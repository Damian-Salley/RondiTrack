namespace RondiTrack.DTOs.Contributions;

public class RecordCycleContributionRequest
{
    // Member whose contribution is being recorded.
    public int UserId { get; set; }

    // Exact contribution amount in cents.
    public long AmountInCents { get; set; }
}