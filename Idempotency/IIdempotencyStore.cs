namespace RondiTrack.Idempotency;

public interface IIdempotencyStore
{
    // Retrieves a record if it exists and has not expired.
    Task<IdempotencyRecord?> GetAsync(string key);

    // Saves a record with an expiration time.
    Task SaveAsync(string key, IdempotencyRecord record);

    // Removes expired records from memory.
    Task<int> RemoveExpiredAsync();
}