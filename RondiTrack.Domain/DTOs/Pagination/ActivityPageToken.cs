using System.Text;
using System.Text.Json;

namespace RondiTrack.DTOs.Pagination;

// Stores the position of the last contribution returned by the activity feed.
public class ActivityPageToken
{
    public DateTime LastPaidAtUtc { get; set; }
    public int LastStokvelId { get; set; }
    public int LastCycle { get; set; }
    public int LastUserId { get; set; }

    // Ensures a token belongs to the authenticated user.
    public int UserId { get; set; }

    // Convert the cursor into an opaque string for the frontend.
    public string Encode()
    {
        var json = JsonSerializer.Serialize(this);

        return Convert.ToBase64String(
            Encoding.UTF8.GetBytes(json));
    }

    // Decode the cursor. Invalid tokens return null.
    public static ActivityPageToken? Decode(string token)
    {
        try
        {
            var json = Encoding.UTF8.GetString(
                Convert.FromBase64String(token));

            return JsonSerializer.Deserialize<ActivityPageToken>(json);
        }
        catch
        {
            return null;
        }
    }
}