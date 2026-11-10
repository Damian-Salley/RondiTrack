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

    // Decimal request fields must fit the signed 64-bit cents used in responses.
    // The existing JavaScript-safe contribution limit above remains unchanged.
    public const decimal MaximumDecimalAmount = long.MaxValue / 100m;

    public static bool HasAtMostTwoDecimalPlaces(decimal amount)
    {
        // Decimal stores its number of fractional digits in bits 16-23.
        var scale = (decimal.GetBits(amount)[3] >> 16) & 0xff;
        return scale <= 2;
    }

    public static long ToCents(decimal amount)
    {
        // Zero is valid for a legacy cycle with no required amount configured.
        // Create/update validators separately require strictly positive amounts.
        if (amount < 0 || amount > MaximumDecimalAmount || !HasAtMostTwoDecimalPlaces(amount))
            throw new ArgumentOutOfRangeException(nameof(amount),
                "Amount must be nonnegative, have at most two decimal places and fit whole cents.");

        return checked((long)(amount * 100m));
    }
}