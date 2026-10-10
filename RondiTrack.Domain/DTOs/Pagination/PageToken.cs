using System.Text;
using System.Text.Json;

namespace RondiTrack.DTOs.Pagination;

public class PageToken
{
    public int LastId { get; set; }
    public decimal? LastAmount { get; set; }
    public string SortBy { get; set; } = string.Empty;
    public string? Filter { get; set; }

    public string Encode()
    {
        var json = JsonSerializer.Serialize(this);

        return Convert.ToBase64String(
            Encoding.UTF8.GetBytes(json));
    }

    public static PageToken? Decode(string token)
    {
        try
        {
            var json = Encoding.UTF8.GetString(
                Convert.FromBase64String(token));

            return JsonSerializer.Deserialize<PageToken>(json);
        }
        catch
        {
            return null;
        }
    }
}