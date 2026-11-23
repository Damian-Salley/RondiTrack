namespace RondiTrack.Models;

public sealed class ContributionReceipt
{
    public string Key { get; init; } = "";
    public string Payload { get; init; } = "";
    public int StokvelId { get; init; }
    public int UserId { get; init; }
    public int CycleId { get; init; }
    public decimal Amount { get; init; }
    public DateTime? PaidAtUtc { get; init; }
    public DateTimeOffset ExpiresAtUtc { get; init; }
}
