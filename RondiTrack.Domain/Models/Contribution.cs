namespace RondiTrack.Models;

// Represents a member's contribution for a specific stokvel cycle.
public class Contribution
{
    // EF Core uses the composite key:
    // StokvelId + UserId + Cycle.
    // We are preserving this existing database identity.
    public int UserId { get; private set; }
    public int StokvelId { get; private set; }
    public int Cycle { get; private set; }

    // Keep decimal amounts internally for compatibility
    // with the existing business logic and repositories.
    public decimal ContributionAmount { get; private set; }

    // Indicates the payment's current state.
    public string Status { get; private set; } = "Paid";

    // The UTC timestamp when the contribution was recorded as paid.
    public DateTime? PaidAtUtc { get; private set; }

    // Existing relationship to the stokvel member.
    public StokvelMember Member { get; private set; } = null!;

    // Parameterless constructor required by EF Core.
    private Contribution()
    {
    }

    // Creates a contribution using a supplied UTC payment timestamp.
    // The optional parameter preserves compatibility with existing callers.
    public Contribution(
        int userId,
        int stokvelId,
        int cycle,
        decimal contributionAmount,
        DateTime? paidAtUtc = null)
    {
        if (userId <= 0)
        {
            throw new ArgumentException(
                "User ID must be greater than zero.");
        }

        if (stokvelId <= 0)
        {
            throw new ArgumentException(
                "Stokvel ID must be greater than zero.");
        }

        if (cycle <= 0)
        {
            throw new ArgumentException(
                "Cycle must be greater than zero.");
        }

        if (contributionAmount <= 0)
        {
            throw new ArgumentException(
                "Contribution amount must be greater than zero.");
        }

        UserId = userId;
        StokvelId = stokvelId;
        Cycle = cycle;
        ContributionAmount = contributionAmount;

        // The existing RecordContribution operation represents
        // a contribution that has already been paid.
        Status = "Paid";

        // The recording service supplies the actual payment time.
        // Older callers can omit the timestamp without inventing a payment date.
        PaidAtUtc = paidAtUtc;
    }

    // Marks a contribution as pending payment.
    // A pending contribution must not have a payment timestamp.
    public void MarkAsPending()
    {
        if (Status != "Paid")
        {
            throw new InvalidOperationException(
                "Only a paid contribution can be marked as pending.");
        }

        if (PaidAtUtc.HasValue)
        {
            throw new InvalidOperationException(
                "A recorded payment cannot be marked as pending.");
        }

        Status = "Pending";
    }

    // Marks an existing paid contribution as confirmed.
    // This method does not automatically confirm payments.
    public void Confirm()
    {
        if (Status != "Paid")
        {
            throw new InvalidOperationException(
                "Only paid contributions can be confirmed.");
        }

        Status = "Confirmed";
    }
}