namespace RondiTrack.DTOs.Stokvels;

// The server owns the order, amount and next-recipient decision.
public sealed record PayoutOrderEntryResponse(
    int UserId,
    string MemberName,
    int Position,
    long AmountCents,
    DateTime? PaidAtUtc,
    string Status);
