using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RondiTrack.Data;
using RondiTrack.Models;
using RondiTrack.Exceptions;

namespace RondiTrack.Services;

public record InvitePreview(int StokvelId, string Name, long ContributionAmountCents,
    string Schedule, string Rules, int MemberCount, bool AlreadyMember);
public record CloseMember(int UserId, string Name, long AmountCents);
public record ClosePreview(int CycleId, string CycleName, string AsOfDate, string Version,
    IReadOnlyList<CloseMember> Members, long TotalCents);

public class JoinPaymentClosingService(RondiTrackDbContext db, TimeProvider clock)
{
    public const int ProofLimit = 5 * 1024 * 1024;
    public async Task RequireTreasurer(int stokvelId, int userId)
    {
        if (!await db.StokvelMembers.AnyAsync(m => m.StokvelId == stokvelId &&
            m.UserId == userId && m.Role == "Treasurer"))
            throw new WorkflowException(403, "Only this stokvel's treasurers can do this.");
    }

    // Short database transactions serialize these infrequent financial mutations across instances.
    // The advisory lock is also used by the existing monthly closing job.
    private async Task<T> Mutate<T>(Func<Task<T>> action) =>
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var tx = await db.Database.BeginTransactionAsync();
            await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(64001)");
            await db.Database.ExecuteSqlRawAsync("LOCK TABLE \"ContributionCycles\", \"Contributions\", \"StokvelMembers\" IN SHARE ROW EXCLUSIVE MODE");
            var result = await action();
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return result;
        });

    public async Task<JoinInvite> CreateInvite(int stokvelId, int userId, string rules, string schedule)
    {
        await RequireTreasurer(stokvelId, userId);
        if (string.IsNullOrWhiteSpace(rules) || rules.Length > 4000 ||
            string.IsNullOrWhiteSpace(schedule) || schedule.Length > 200)
            throw new WorkflowException(400, "Enter rules (up to 4000 characters) and a schedule (up to 200 characters).");
        var invite = new JoinInvite { Code = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)),
            StokvelId = stokvelId, Rules = rules.Trim(), Schedule = schedule.Trim(),
            ExpiresAtUtc = clock.GetUtcNow().UtcDateTime.AddDays(7) };
        db.JoinInvites.Add(invite);
        await db.SaveChangesAsync();
        return invite;
    }

    private async Task<JoinInvite> ValidInvite(string code)
    {
        var invite = await db.JoinInvites.FindAsync(code);
        if (invite is null) throw new WorkflowException(404, "Check the code and try again.");
        if (invite.ExpiresAtUtc <= clock.GetUtcNow().UtcDateTime)
            throw new WorkflowException(410, "Ask for a new invite.");
        return invite;
    }

    public async Task<InvitePreview> PreviewInvite(string code, int userId)
    {
        var invite = await ValidInvite(code);
        var stokvel = await db.Stokvels.SingleAsync(s => s.Id == invite.StokvelId);
        return new(stokvel.Id, stokvel.Name, checked((long)(stokvel.ContributionAmount * 100m)),
            invite.Schedule, invite.Rules,
            await db.StokvelMembers.CountAsync(m => m.StokvelId == stokvel.Id),
            await db.StokvelMembers.AnyAsync(m => m.StokvelId == stokvel.Id && m.UserId == userId));
    }

    public Task<int> Join(string code, int userId) => Mutate(async () =>
    {
        var invite = await ValidInvite(code);
        // The composite membership key plus transaction makes concurrent retries harmless.
        if (!await db.StokvelMembers.AnyAsync(m => m.StokvelId == invite.StokvelId && m.UserId == userId))
            db.StokvelMembers.Add(new(invite.StokvelId, userId, "Member", clock.GetUtcNow().UtcDateTime));
        return invite.StokvelId;
    });

    // Ignore the filename and supplied MIME type. Only these byte signatures are supported.
    public static string? ProofType(byte[] bytes)
    {
        if (bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10})) return "image/png";
        if (bytes.Length >= 3 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255) return "image/jpeg";
        if (bytes.Length >= 5 && bytes.AsSpan(0, 5).SequenceEqual("%PDF-"u8)) return "application/pdf";
        return null;
    }

    private async Task<PaymentProof?> AccessibleProof(int stokvelId, int cycleId, int memberId, int userId)
    {
        var proof = await db.PaymentProofs.FindAsync(stokvelId, memberId, cycleId);
        if (proof is null || (proof.UploaderId != userId && !await db.StokvelMembers.AnyAsync(m =>
            m.StokvelId == stokvelId && m.UserId == userId && m.Role == "Treasurer")))
            throw new WorkflowException(404, "Proof not found.");
        return proof;
    }
    public async Task<object> ProofDetails(int stokvelId, int cycleId, int memberId, int userId)
    {
        if (!await db.Contributions.AnyAsync(c => c.StokvelId == stokvelId && c.Cycle == cycleId && c.UserId == memberId))
            throw new WorkflowException(404, "Contribution not found.");
        var proof = await db.PaymentProofs.FindAsync(stokvelId, memberId, cycleId);
        var treasurer = await db.StokvelMembers.AnyAsync(m => m.StokvelId == stokvelId && m.UserId == userId && m.Role == "Treasurer");
        var owner = memberId == userId && await db.StokvelMembers.AnyAsync(m => m.StokvelId == stokvelId && m.UserId == userId);
        if (!owner && !treasurer && proof?.UploaderId != userId)
            throw new WorkflowException(404, "Contribution not found.");
        var cycle = await db.ContributionCycles.FindAsync(cycleId);
        return new { hasProof = proof is not null, canUpload = owner && cycle!.Status == "Active", locked = cycle!.Status != "Active" };
    }

    public Task<PaymentProof?> Download(int stokvelId, int cycleId, int memberId, int userId) =>
        AccessibleProof(stokvelId, cycleId, memberId, userId);

    public Task<bool> SaveProof(int stokvelId, int cycleId, int memberId, int userId, byte[]? bytes) => Mutate(async () =>
    {
        var contribution = await db.Contributions.FindAsync(stokvelId, memberId, cycleId);
        // A member uploads their own proof; treasurers may inspect, but not impersonate the uploader.
        if (contribution is null || userId != memberId || !await db.StokvelMembers.AnyAsync(m =>
            m.StokvelId == stokvelId && m.UserId == userId))
            throw new WorkflowException(404, "Contribution not found.");
        var cycle = await db.ContributionCycles.FindAsync(cycleId);
        if (cycle!.Status != "Active") throw new WorkflowException(409, "The cycle is closed. Proof is locked.");
        var existing = await db.PaymentProofs.FindAsync(stokvelId, memberId, cycleId);
        if (bytes is null)
        {
            if (existing is not null) db.PaymentProofs.Remove(existing);
            return true;
        }
        if (bytes.Length == 0 || bytes.Length > ProofLimit)
            throw new WorkflowException(400, "Choose a file up to 5 MiB.");
        var type = ProofType(bytes) ?? throw new WorkflowException(400, "Choose a real JPEG, PNG or PDF file.");
        if (existing is null)
        {
            existing = new() { StokvelId = stokvelId, Cycle = cycleId, UserId = memberId, UploaderId = userId };
            db.PaymentProofs.Add(existing);
        }
        existing.Bytes = bytes;
        existing.ContentType = type;
        return true;
    });

    private async Task<ClosePreview> Calculate(int cycleId)
    {
        var cycle = await db.ContributionCycles.FindAsync(cycleId)
            ?? throw new WorkflowException(404, "Cycle not found.");
        if (cycle.Status != "Active") throw new WorkflowException(409, "This cycle is already closed or paid.");
        var members = await db.StokvelMembers.Where(m => m.StokvelId == cycle.StokvelId)
            .Include(m => m.User).OrderBy(m => m.UserId).ToListAsync();
        var contributions = await db.Contributions.Where(c => c.Cycle == cycleId).OrderBy(c => c.UserId).ToListAsync();
        var asOfDate = TimeZoneInfo.ConvertTime(clock.GetUtcNow(),
            TimeZoneInfo.FindSystemTimeZoneById("Africa/Johannesburg")).ToString("yyyy-MM-dd");
        var rows = members.Select(m => new CloseMember(m.UserId, $"{m.User.FirstName} {m.User.LastName}",
            CycleClosing.OutstandingCents(cycle, m.UserId, contributions))).Where(m => m.AmountCents > 0).ToList();
        // Include all relevant inputs, even payments that do not change total arrears.
        var inputs = JsonSerializer.Serialize(new { cycle.Id, cycle.Status, cycle.RequiredContributionAmount,
            cycle.EndsAtUtc, asOfDate,
            Members = members.Select(m => new { m.UserId, m.User.FirstName, m.User.LastName }),
            Payments = contributions.Select(c => new { c.UserId, c.ContributionAmount, c.Status, c.PaidAtUtc }) });
        var version = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(inputs)));
        return new(cycleId, $"Cycle {cycle.Number}", asOfDate, version, rows, rows.Sum(m => m.AmountCents));
    }

    public async Task<ClosePreview> PreviewClose(int cycleId, int userId)
    {
        var cycle = await db.ContributionCycles.FindAsync(cycleId) ?? throw new WorkflowException(404, "Cycle not found.");
        await RequireTreasurer(cycle.StokvelId, userId);
        return await Calculate(cycleId);
    }

    public Task<ClosePreview> Close(int cycleId, int userId, string version, string key) => Mutate(async () =>
    {
        var cycle = await db.ContributionCycles.FindAsync(cycleId) ?? throw new WorkflowException(404, "Cycle not found.");
        await RequireTreasurer(cycle.StokvelId, userId);
        if (string.IsNullOrWhiteSpace(key) || key.Length > 100 || string.IsNullOrWhiteSpace(version))
            throw new WorkflowException(400, "A preview version and Idempotency-Key are required.");
        var receipt = await db.CycleCloseReceipts.FindAsync(cycleId);
        if (receipt is not null)
        {
            if (receipt.UserId == userId && receipt.Key == key && receipt.Version == version)
                return JsonSerializer.Deserialize<ClosePreview>(receipt.ResultJson)!;
            throw new WorkflowException(409, "This cycle has already been closed.");
        }
        if (await db.CycleCloseReceipts.AnyAsync(r => r.UserId == userId && r.Key == key))
            throw new WorkflowException(409, "This key was used for a different close.");
        var preview = await Calculate(cycleId);
        if (preview.Version != version) throw new WorkflowException(409, "Payments or membership changed. Review the new preview and confirm again.");
        foreach (var member in preview.Members)
            db.CycleArrears.Add(new(cycleId, member.UserId, member.AmountCents));
        cycle.Close(clock.GetUtcNow().UtcDateTime);
        db.CycleCloseReceipts.Add(new() { CycleId = cycleId, UserId = userId, Key = key,
            Version = version, ResultJson = JsonSerializer.Serialize(preview) });
        return preview;
    });
}
