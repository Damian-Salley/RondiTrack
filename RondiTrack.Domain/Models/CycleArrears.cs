namespace RondiTrack.Models;

public class CycleArrears
{
    public int Id { get; private set; }

    public int ContributionCycleId { get; private set; }
    public int UserId { get; private set; }

    public long AmountCents { get; private set; }

    private CycleArrears()
    {
    }

    public CycleArrears(
        int contributionCycleId,
        int userId,
        long amountCents)
    {
        if (amountCents < 0)
            throw new ArgumentOutOfRangeException(nameof(amountCents));

        ContributionCycleId = contributionCycleId;
        UserId = userId;
        AmountCents = amountCents;
    }
}