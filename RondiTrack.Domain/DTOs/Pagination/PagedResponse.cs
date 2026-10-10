namespace RondiTrack.DTOs.Pagination;

public class PagedResponse<T>
{
    public IReadOnlyCollection<T> Items { get; set; } = [];
    public string? NextPageToken { get; set; }
}