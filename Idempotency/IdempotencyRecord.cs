using RondiTrack.Models;

namespace RondiTrack.Idempotency;

public class IdempotencyRecord
{
    public string Payload { get; }
    public Contribution Contribution { get; }

    public IdempotencyRecord(string payload, Contribution contribution)
    {
        Payload = payload;
        Contribution = contribution;
    }
}