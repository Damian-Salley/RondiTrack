namespace RondiTrack.Models;

public static class CycleClosing
{
    // Shared by previews, manual closes and the monthly job.
    public static long OutstandingCents(ContributionCycle cycle, int userId,
        IEnumerable<Contribution> contributions) => checked((long)(100m * Math.Max(0m,
            cycle.RequiredContributionAmount - contributions.Where(c => c.UserId == userId)
                .Sum(c => c.ContributionAmount))));
}
