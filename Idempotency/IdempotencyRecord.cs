using RondiTrack.Models;

namespace RondiTrack.Idempotency;

public class IdempotencyRecord
{
    // Identifies the original request.
    public string Payload { get; }

    // Stores the contribution returned by the original request.
    public Contribution Contribution { get; }

    // Records when this idempotency entry expires.
    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public IdempotencyRecord(string payload, Contribution contribution)
    {
        Payload = payload;
        Contribution = contribution;

        // The store sets the actual expiration using TimeProvider.
        ExpiresAtUtc = DateTimeOffset.MaxValue;
    }

    // Sets the expiration time when the record is saved.
    public void SetExpiration(DateTimeOffset expiresAtUtc)
    {
        ExpiresAtUtc = expiresAtUtc;
    }
}