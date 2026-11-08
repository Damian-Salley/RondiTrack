using Microsoft.EntityFrameworkCore;
using RondiTrack.Models;

namespace RondiTrack.Data.Seeding;

public static class Assignment92TestSeeder
{
    public static async Task SeedAsync(RondiTrackDbContext db, string passwordHash)
    {
        if (await db.Users.AnyAsync())
            throw new InvalidOperationException("Assignment 9.2 seed requires an empty test database.");

        var sipho = new User(1, "Sipho", "Nkosi", "sipho@assignment92.example", "0000000000");
        sipho.SetPasswordHash(passwordHash);
        var members = Enumerable.Range(2, 39)
            .Select(id => new User(id, $"Member{id}", "Test", $"member{id}@assignment92.example", "0000000000"))
            .Prepend(sipho).ToArray();
        db.Users.AddRange(members);
        // Each browser test claims a separate untouched stokvel by its fixed fixture ID.
        foreach (var id in Enumerable.Range(1, 12))
        {
            var stokvel = new Stokvel(id, $"Private test stokvel {id} {Guid.NewGuid():N}", 150.50m);
            foreach (var member in members)
                stokvel.AddMember(member, member.Id == 1 && id != 2 ? "Treasurer" : "Member");
            db.Stokvels.Add(stokvel);
            foreach (var month in Enumerable.Range(1, 12))
            {
                var cycleId = id * 100 + month;
                var cycle = new ContributionCycle(cycleId, id, month, 6020m,
                    DateTime.UtcNow.AddYears(2), 150.50m);
                if (month != 12) cycle.Close(DateTime.UtcNow.AddDays(-month * 30));
                db.ContributionCycles.Add(cycle);
                foreach (var member in members.Where(member => month != 12 || member.Id != 1))
                    db.Contributions.Add(new Contribution(member.Id, id, cycleId, 150.50m,
                        DateTime.UtcNow.AddMonths(-12 + month)));
            }
        }
        await db.SaveChangesAsync();
        await db.Database.ExecuteSqlRawAsync("""
            SELECT setval(pg_get_serial_sequence('"Users"', 'Id'), (SELECT MAX("Id") FROM "Users"));
            SELECT setval(pg_get_serial_sequence('"Stokvels"', 'Id'), (SELECT MAX("Id") FROM "Stokvels"));
            SELECT setval(pg_get_serial_sequence('"ContributionCycles"', 'Id'), (SELECT MAX("Id") FROM "ContributionCycles"));
            """);
    }
}
