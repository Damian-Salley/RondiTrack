using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using RondiTrack.Repositories;

namespace RondiTrack.Authorization;

// Checks the user's stokvel membership and role against the database.
public class StokvelAccessHandler
    : AuthorizationHandler<StokvelAccessRequirement, int>
{
    private readonly IRondiTrackRepository _repository;

    public StokvelAccessHandler(IRondiTrackRepository repository)
    {
        _repository = repository;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        StokvelAccessRequirement requirement,
        int stokvelId)
    {
        // Get the authenticated user's ID.
        var userIdClaim =
            context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");

        if (!int.TryParse(userIdClaim, out int userId))
            return;

        // Check membership in PostgreSQL.
        var member = await _repository.GetMemberForContributionAsync(
            stokvelId, userId);

        if (member is null)
            return;

        // Chairperson-only operations.
        if (requirement.RequireChairperson)
        {
            if (member.Role == "Chairperson")
                context.Succeed(requirement);

            return;
        }

        // Treasurer or Chairperson operations.
        if (requirement.RequireElevatedRole)
        {
            if (member.Role is "Treasurer" or "Chairperson")
                context.Succeed(requirement);

            return;
        }

        // Any stokvel member can perform basic operations.
        context.Succeed(requirement);
    }
}