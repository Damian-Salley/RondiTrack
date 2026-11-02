using System.Text;
using System.Text.Json;

namespace RondiTrack.DTOs.Pagination;

public class ContributionHistoryToken
{
    public DateTime? LastPaidAtUtc { get; set; }
    public int LastCycle { get; set; }
    public int LastUserId { get; set; }

    public int StokvelId { get; set; }
    public int? UserId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

    public string Encode()
    {
        var json = JsonSerializer.Serialize(this);

        return Convert.ToBase64String(
            Encoding.UTF8.GetBytes(json));
    }

    public static ContributionHistoryToken? Decode(string token)
    {
        try
        {
            var json = Encoding.UTF8.GetString(
                Convert.FromBase64String(token));

            return JsonSerializer.Deserialize<ContributionHistoryToken>(json);
        }
        catch
        {
            return null;
        }
    }
}