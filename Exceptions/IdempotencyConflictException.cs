namespace RondiTrack.Exceptions;

public class IdempotencyConflictException : RondiTrackException
{
    public IdempotencyConflictException(string message)
        : base(message)
    {
        
    }
}