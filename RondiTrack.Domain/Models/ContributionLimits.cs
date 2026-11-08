namespace RondiTrack.Models;

// Defines the maximum contribution amount accepted by the API.
public static class ContributionLimits
{
    // Maximum safe integer supported by JavaScript.
    //
    // Contributions are stored and transferred as integer cents.
    // Keeping amounts within this limit prevents precision loss
    // when the Next.js frontend sends values to the .NET backend.
    //
    // Maximum: 9,007,199,254,740,991 cents.
    public const long MaximumCents = 9_007_199_254_740_991;
}