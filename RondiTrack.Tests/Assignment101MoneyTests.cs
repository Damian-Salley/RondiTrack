using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using RondiTrack.DTOs.ContributionCycles;
using RondiTrack.DTOs.Stokvels;
using RondiTrack.Mappers;
using RondiTrack.Models;
using RondiTrack.Validators;

namespace RondiTrack.Tests;

public class Assignment101MoneyTests
{
    [Theory]
    [InlineData("0.01", 1L)]
    [InlineData("0.10", 10L)]
    [InlineData("150", 15000L)]
    [InlineData("150.50", 15050L)]
    [InlineData("92233720368547758.06", long.MaxValue - 1)]
    [InlineData("92233720368547758.07", long.MaxValue)]
    public void ToCents_ConvertsExactly(string input, long expected)
    {
        var amount = decimal.Parse(input, CultureInfo.InvariantCulture);
        Assert.Equal(expected, ContributionLimits.ToCents(amount));
        Assert.Equal(expected, new StokvelResponse { ContributionAmount = amount }.ContributionAmountCents);
        var cycle = new ContributionCycle(1, 1, 1, amount, requiredContributionAmount: amount);
        var response = ContributionCycleMapper.ToResponse(cycle);
        Assert.Equal(expected, response.TargetAmountCents);
        Assert.Equal(expected, response.RequiredContributionCents);
    }

    [Theory]
    [InlineData("0.001")]
    [InlineData("150.505")]
    [InlineData("150.500")]
    [InlineData("-0.01")]
    [InlineData("92233720368547758.08")]
    [InlineData("79228162514264337593543950335")]
    public void ToCents_RejectsRatherThanTruncatingRoundingOrOverflowing(string input)
    {
        var amount = decimal.Parse(input, CultureInfo.InvariantCulture);
        Assert.Throws<ArgumentOutOfRangeException>(() => ContributionLimits.ToCents(amount));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new StokvelResponse { ContributionAmount = amount }.ContributionAmountCents);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ContributionCycleMapper.ToResponse(new ContributionCycle(1, 1, 1, amount)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ContributionCycleMapper.ToResponse(new ContributionCycle(1, 1, 1, 1m,
                requiredContributionAmount: amount)));
    }

    [Theory]
    [InlineData("0.01", true)]
    [InlineData("150.50", true)]
    [InlineData("92233720368547758.07", true)]
    [InlineData("0.001", false)]
    [InlineData("150.505", false)]
    [InlineData("150.500", false)]
    [InlineData("0", false)]
    [InlineData("-0.01", false)]
    [InlineData("92233720368547758.08", false)]
    [InlineData("79228162514264337593543950335", false)]
    public void AllDecimalRequestValidators_EnforcePositiveExactRepresentableCents(string input, bool valid)
    {
        var amount = decimal.Parse(input, CultureInfo.InvariantCulture);
        Assert.Equal(valid, new CreateStokvelRequestValidator().Validate(
            new CreateStokvelRequest { Id = 1, Name = "Savings", ContributionAmount = amount }).IsValid);
        Assert.Equal(valid, new UpdateStokvelRequestValidator().Validate(
            new UpdateStokvelRequest { Name = "Savings", ContributionAmount = amount }).IsValid);
        Assert.Equal(valid, new CreateContributionCycleRequestValidator().Validate(
            new CreateContributionCycleRequest { Id = 1, StokvelId = 1, Number = 1, TargetAmount = amount }).IsValid);
        Assert.Equal(valid, new UpdateContributionCycleRequestValidator().Validate(
            new UpdateContributionCycleRequest { Number = 1, TargetAmount = amount }).IsValid);
    }

    [Fact]
    public void LegacyCycleWithoutRequiredAmount_StillReturnsZero()
    {
        Assert.Equal(0, ContributionLimits.ToCents(0m));
        Assert.Equal(0, ContributionCycleMapper.ToResponse(new ContributionCycle(1, 1, 1, 1m))
            .RequiredContributionCents);
    }

    [Fact]
    public async Task DecimalRequestEndpoints_RejectInvalidAmountsBeforePersistence()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.InitializeAsync();
        using var client = factory.CreateClient();
        foreach (var amount in new[] { 0.001m, 150.505m, 150.500m, 0m, -0.01m,
            92233720368547758.08m, decimal.MaxValue })
        {
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/stokvels",
                new { id = 10101, name = "Exact cents", contributionAmount = amount })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/v1/stokvels/1",
                new { name = "Exact cents", contributionAmount = amount })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/contributioncycles",
                new { id = 10101, stokvelId = 1, number = 1, targetAmount = amount })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/v1/contributioncycles/10101",
                new { number = 1, targetAmount = amount })).StatusCode);
        }
    }
}
