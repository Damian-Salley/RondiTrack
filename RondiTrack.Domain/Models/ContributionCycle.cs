namespace RondiTrack.Models;

// Represents one monthly contribution cycle belonging to a stokvel.
public class ContributionCycle
{
    // Used by Entity Framework Core when loading contribution cycles
    // from the database. Application code should use the public constructor.
    private ContributionCycle()
    {
        // EF Core will populate these properties from the database.
        Status = null!;
    }

    // Unique identifier for the contribution cycle.
    public int Id { get; private set; }

    // Identifies the stokvel that owns this cycle.
    public int StokvelId { get; private set; }

    // Navigation property used by Entity Framework Core.
    public Stokvel Stokvel { get; private set; } = null!;

    // Sequence number of the contribution cycle.
    public int Number { get; private set; }

    // Total amount the stokvel aims to collect during this cycle.
    public decimal TargetAmount { get; private set; }

    // Fixed amount each member must contribute during this specific cycle.
    public decimal RequiredContributionAmount { get; private set; }

    // Indicates whether the cycle is active, paid, or closed.
    public string Status { get; private set; }

    // UTC date and time when the cycle is scheduled to close.
    public DateTime EndsAtUtc { get; private set; }

    // UTC date and time when the cycle was actually closed.
    public DateTime? ClosedAtUtc { get; private set; }

    // Constructor used when creating a contribution cycle.
    // Optional parameters preserve compatibility with existing tests and callers.
    public ContributionCycle(
        int id,
        int stokvelId,
        int number,
        decimal targetAmount,
        DateTime? endsAtUtc = null,
        decimal? requiredContributionAmount = null)

    {
        Id = id;
        StokvelId = stokvelId;
        Number = number;
        TargetAmount = targetAmount;

        // Store the per-member amount when it is supplied.
        // Existing test code can still construct cycles without this argument.
        RequiredContributionAmount = requiredContributionAmount ?? 0m;

        Status = "Active";

        // Calculate the next Johannesburg month-end unless a date was supplied.
        EndsAtUtc = endsAtUtc ??
            GetMonthEndUtc(DateTimeOffset.UtcNow);


    }

    // Updates the cycle number and total collection target.
    public void Update(int number, decimal targetAmount)
    {
        Number = number;
        TargetAmount = targetAmount;
    }

    // Marks a cycle as paid after payout processing.
    public void MarkAsPaid()
    {
        Status = "Paid";
    }

    // Marks the cycle as closed without closing it twice.
    public void Close(DateTime closedAtUtc)
    {
        if (ClosedAtUtc.HasValue)
            return;

        ClosedAtUtc = closedAtUtc;
        Status = "Closed";
    }

    // Finds the start of the next month in Johannesburg time,
    // then converts that moment to UTC.
    public static DateTime GetMonthEndUtc(DateTimeOffset currentTime)
    {
        var timeZone =
            TimeZoneInfo.FindSystemTimeZoneById("Africa/Johannesburg");

        var localTime =
            TimeZoneInfo.ConvertTime(currentTime, timeZone);

        var firstDayOfNextMonth = new DateTime(
            localTime.Year,
            localTime.Month,
            1).AddMonths(1);

        return TimeZoneInfo.ConvertTimeToUtc(
            firstDayOfNextMonth,
            timeZone);
    }
}