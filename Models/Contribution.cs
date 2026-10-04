namespace RondiTrack.Models;

public class Contribution
{
    public int UserId { get; }
    public int StokvelId { get; }
    public int Cycle { get; }
    public decimal ContributionAmount { get; private set; }

    public StokvelMember Member { get; private set; } = null!;

    public Contribution(
        int userId,
        int stokvelId,
        int cycle,
        decimal contributionAmount)
    {
        // Validate the user ID
        if (userId <= 0)
        {
            throw new ArgumentException(
                "User ID must be greater than zero.");
        }

        // Validate the stokvel ID
        if (stokvelId <= 0)
        {
            throw new ArgumentException(
                "Stokvel ID must be greater than zero.");
        }

        // Validate the cycle
        if (cycle <= 0)
        {
            throw new ArgumentException(
                "Cycle must be greater than zero.");
        }

        // Validate the contribution amount
        if (contributionAmount <= 0)
        {
            throw new ArgumentException(
                "Contribution amount must be greater than zero.");
        }

        UserId = userId;
        StokvelId = stokvelId;
        Cycle = cycle;
        ContributionAmount = contributionAmount;
    }
}