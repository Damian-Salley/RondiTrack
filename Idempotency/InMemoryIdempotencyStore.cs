using System.Collections.Concurrent;

namespace RondiTrack.Idempotency;

public class InMemoryIdempotencyStore : IIdempotencyStore
{
    // ConcurrentDictionary supports access from multiple requests.
    private readonly ConcurrentDictionary<string, IdempotencyRecord> _records = new();

    // TimeProvider lets us control time during automated tests.
    private readonly TimeProvider _timeProvider;

    // Idempotency records remain valid for 24 hours.
    private static readonly TimeSpan RecordLifetime = TimeSpan.FromHours(24);

    public InMemoryIdempotencyStore(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    // Retrieves a record only while it is valid.
    public Task<IdempotencyRecord?> GetAsync(string key)
    {
        if (!_records.TryGetValue(key, out var record))
        {
            return Task.FromResult<IdempotencyRecord?>(null);
        }

        // Expired records must not be returned to callers.
        if (record.ExpiresAtUtc <= _timeProvider.GetUtcNow())
        {
            _records.TryRemove(key, out _);

            return Task.FromResult<IdempotencyRecord?>(null);
        }

        return Task.FromResult<IdempotencyRecord?>(record);
    }

    // Saves a record and assigns its expiration time.
    public Task SaveAsync(string key, IdempotencyRecord record)
    {
        record.SetExpiration(_timeProvider.GetUtcNow().Add(RecordLifetime));

        _records[key] = record;

        return Task.CompletedTask;
    }

    // Removes expired records and returns the number removed.
    public Task<int> RemoveExpiredAsync()
    {
        var now = _timeProvider.GetUtcNow();
        var removedCount = 0;

        foreach (var entry in _records)
        {
            if (entry.Value.ExpiresAtUtc <= now &&
                _records.TryRemove(entry.Key, out _))
            {
                removedCount++;
            }
        }

        return Task.FromResult(removedCount);
    }
}