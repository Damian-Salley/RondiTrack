namespace RondiTrack.Idempotency;

public interface IIdempotencyStore
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation) => operation();

    // Retrieves a record if it exists and has not expired.
    Task<IdempotencyRecord?> GetAsync(string key);

    // Saves a record with an expiration time.
    Task SaveAsync(string key, IdempotencyRecord record);

    // Removes expired records.
    Task<int> RemoveExpiredAsync();
}