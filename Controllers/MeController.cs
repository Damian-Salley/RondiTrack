using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RondiTrack.Constants;
using RondiTrack.DTOs.Me;
using RondiTrack.Repositories;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace RondiTrack.Controllers;

// Require authentication for all home-screen endpoints.
[ApiController]
[Authorize]
[Route(ApiRoutes.Prefix + "/me")]
public class MeController : ControllerBase
{
    private readonly IRondiTrackRepository _repository;
    private readonly TimeProvider _timeProvider;

    // Inject the repository and time provider.
    public MeController(
        IRondiTrackRepository repository,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    // Return the authenticated user's home-screen information.
    [HttpGet("home")]
    public async Task<ActionResult<List<HomeStokvelResponse>>> GetHome()
    {
        // Retrieve the authenticated user's ID from their JWT.
        var claim = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Reject requests without a valid user ID.
        if (!int.TryParse(claim, out var userId))
            return Unauthorized();

        // Retrieve stokvels, contribution cycles, and payouts.
        var stokvels = await _repository.GetStokvelsAsync();
        var cycles = await _repository.GetContributionCyclesAsync();
        var payouts = await _repository.GetPayoutsAsync();

        // Get the current UTC date and time.
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        // Store the home-screen information for each stokvel.
        var result = new List<HomeStokvelResponse>();

        foreach (var stokvel in stokvels)
        {
            // Check whether the authenticated user belongs to this stokvel.
            var membership = stokvel.Members
                .FirstOrDefault(member => member.UserId == userId);

            // Do not return stokvels the user does not belong to.
            if (membership is null)
                continue;

            // Find the latest active contribution cycle.
            var cycle = cycles
                .Where(c => c.StokvelId == stokvel.Id &&
                            c.Status == "Active")
                .OrderByDescending(c => c.Number)
                .FirstOrDefault();

            // Retrieve only this user's contribution for the active cycle.
            // The repository expects stokvel ID, user ID, then cycle ID.
            var contribution = cycle is null
                ? null
                : await _repository.GetContributionAsync(
                    stokvel.Id,
                    userId,
                    cycle.Id);

            // Calculate how much the user has already contributed.
            var paid = contribution?.ContributionAmount ?? 0m;

            // Find the next scheduled payout that has not passed.
            var nextPayout = payouts
                .Where(p => p.StokvelId == stokvel.Id &&
                            p.PayoutDate >= now)
                .OrderBy(p => p.PayoutDate)
                .FirstOrDefault();

            // Add the user's home-screen information for this stokvel.
            result.Add(new HomeStokvelResponse
            {
                StokvelId = stokvel.Id,
                StokvelName = stokvel.Name,

                // Return the user's role within this stokvel.
                Role = membership.Role,

                // Return the active contribution cycle.
                CurrentCycle = cycle?.Number,
                CycleStatus = cycle?.Status,

                // Return money as decimal strings for JavaScript safety.
                PaidThisCycle = paid.ToString(
                    "0.00",
                    CultureInfo.InvariantCulture),

                // Calculate the remaining contribution amount.
                StillOwes = Math.Max(
                    0m,
                    stokvel.ContributionAmount - paid)
                    .ToString(
                        "0.00",
                        CultureInfo.InvariantCulture),

                // Return the next payout date, if one exists.
                NextPayoutDate = nextPayout?.PayoutDate
            });
        }

        // Return only stokvels belonging to the authenticated user.
        return Ok(result);
    }
}