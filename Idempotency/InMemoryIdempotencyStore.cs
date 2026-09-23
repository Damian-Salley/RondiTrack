namespace RondiTrack.Idempotency;

public class InMemoryIdempotencyStore : IIdempotencyStore
{
    private readonly Dictionary<string, IdempotencyRecord> _records = new();

    // Retrieves an idempotency record by key.
    public Task<IdempotencyRecord?> GetAsync(string key)
    {
        _records.TryGetValue(key, out var record);

        return Task.FromResult(record);
    }

    // Saves an idempotency record with the specified key.
    public Task SaveAsync(string key, IdempotencyRecord record)
    {
        _records[key] = record;

        return Task.CompletedTask;
    }
}