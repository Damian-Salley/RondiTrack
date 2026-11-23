using Microsoft.EntityFrameworkCore;
using RondiTrack.Data;
using RondiTrack.Models;

namespace RondiTrack.Idempotency;

public sealed class PostgresIdempotencyStore(
    RondiTrackDbContext db,
    TimeProvider clock) : IIdempotencyStore
{
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            // Serialize payments with cycle closing and other financial writes.
            await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(64001)");
            try
            {
                var result = await operation();
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                db.ChangeTracker.Clear();
                throw;
            }
        });
    }

    public async Task<IdempotencyRecord?> GetAsync(string key)
    {
        var now = clock.GetUtcNow();
        var receipt = await db.ContributionReceipts.AsNoTracking()
            .SingleOrDefaultAsync(receipt => receipt.Key == key && receipt.ExpiresAtUtc > now);
        if (receipt is null)
            return null;

        var record = new IdempotencyRecord(receipt.Payload, new Contribution(
            receipt.UserId, receipt.StokvelId, receipt.CycleId, receipt.Amount, receipt.PaidAtUtc));
        record.SetExpiration(receipt.ExpiresAtUtc);
        return record;
    }

    public async Task SaveAsync(string key, IdempotencyRecord record)
    {
        var now = clock.GetUtcNow();
        await db.ContributionReceipts
            .Where(receipt => receipt.Key == key && receipt.ExpiresAtUtc <= now)
            .ExecuteDeleteAsync();
        var contribution = record.Contribution;
        db.ContributionReceipts.Add(new ContributionReceipt
        {
            Key = key,
            Payload = record.Payload,
            StokvelId = contribution.StokvelId,
            UserId = contribution.UserId,
            CycleId = contribution.Cycle,
            Amount = contribution.ContributionAmount,
            PaidAtUtc = contribution.PaidAtUtc,
            ExpiresAtUtc = now.AddHours(24)
        });
        await db.SaveChangesAsync();
    }

    public Task<int> RemoveExpiredAsync()
    {
        var now = clock.GetUtcNow();
        return db.ContributionReceipts.Where(receipt => receipt.ExpiresAtUtc <= now)
            .ExecuteDeleteAsync();
    }
}
