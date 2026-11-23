namespace RondiTrack.DTOs.Contributions;

public class ContributionHistoryRequest : ContributionPageRequest
{
    public int? CycleId { get; set; }
    public string? Status { get; set; }
}
