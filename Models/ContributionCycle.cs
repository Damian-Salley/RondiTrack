namespace RondiTrack.Models;

public class ContributionCycle
{
    public int Id { get; private set; }
    public int StokvelId { get; private set; }
    public int Number { get; private set; }
    public decimal TargetAmount { get; private set; }

    public ContributionCycle(int id, int stokvelId, int number, decimal targetAmount)
    {
        Id = id;
        StokvelId = stokvelId;
        Number = number;
        TargetAmount = targetAmount;
    }

    public void Update(int number, decimal targetAmount)
    {
        Number = number;
        TargetAmount = targetAmount;
    }
}