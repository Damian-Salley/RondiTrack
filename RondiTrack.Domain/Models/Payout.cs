namespace RondiTrack.Models;

public class Payout
{
    public int Id { get; private set; }
    public int StokvelId { get; private set; }
    public int UserId { get; private set; }
    public int ContributionCycleId { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime PayoutDate { get; private set; }
    public int RotationOrder { get; private set; }

    public Payout(int id, int stokvelId, int userId, int contributionCycleId,
                  decimal amount, DateTime payoutDate, int rotationOrder)
    {
        Id = id;
        StokvelId = stokvelId;
        UserId = userId;
        ContributionCycleId = contributionCycleId;
        Amount = amount;
        PayoutDate = payoutDate;
        RotationOrder = rotationOrder;
    }
}