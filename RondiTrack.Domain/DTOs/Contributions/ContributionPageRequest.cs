namespace RondiTrack.DTOs.Contributions;

public class ContributionPageRequest
{
    public int? PageSize { get; set; }
    public string? PageToken { get; set; }
    public string SortBy { get; set; } = "userId";
    public int? UserId { get; set; }

    // UTC boundaries: From is inclusive, To is exclusive.
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}