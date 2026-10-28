namespace RondiTrack.Exceptions;

public class DuplicateContributionException : RondiTrackException
{
    public DuplicateContributionException(string message)
        : base(message)
    {
    }
}