using RondiTrack.DTOs.Contributions;
using RondiTrack.Models;
using RondiTrack.Validators;

namespace RondiTrack.Tests;

public class Assignment92MoneyTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(1999, true)]
    [InlineData(ContributionLimits.MaximumCents, true)]
    [InlineData(ContributionLimits.MaximumCents + 1, false)]
    public void Contribution_cents_must_fit_the_exact_browser_contract(long cents, bool valid)
    {
        var result = new RecordCycleContributionRequestValidator().Validate(
            new RecordCycleContributionRequest { UserId = 1, AmountInCents = cents });
        Assert.Equal(valid, result.IsValid);
    }

    [Fact]
    public void Collected_totals_above_the_browser_integer_range_serialize_without_losing_a_cent()
    {
        var response = new RondiTrack.DTOs.Me.HomeStokvelResponse
        {
            TotalCollectedCents = ContributionLimits.MaximumCents + 2
        };
        var json = System.Text.Json.JsonSerializer.Serialize(response,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        using var document = System.Text.Json.JsonDocument.Parse(json);
        Assert.Equal("9007199254740993", document.RootElement.GetProperty("totalCollectedCents").GetString());
    }
}
