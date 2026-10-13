using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using RondiTrack.Repositories;
using RondiTrack.Telemetry;

namespace RondiTrack.Authorization;

// Checks stokvel membership and role using the database.
public class StokvelAccessHandler
    : AuthorizationHandler<StokvelAccessRequirement, int>
{
    private readonly IRondiTrackRepository _repository;
    private readonly ILogger<StokvelAccessHandler> _logger;

    public StokvelAccessHandler(
        IRondiTrackRepository repository,
        ILogger<StokvelAccessHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        StokvelAccessRequirement requirement,
        int stokvelId)
    {
        // Retrieve the authenticated user's ID.
        var userIdClaim =
            context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");

        if (!int.TryParse(userIdClaim, out int userId))
        {
            // Missing or invalid identity.
            // Do not log the invalid claim value.
            return;
        }

        // Check stokvel membership in PostgreSQL.
        var member = await _repository.GetMemberForContributionAsync(
            stokvelId, userId);

        if (member is null)
        {
            // Non-members receive 404 to hide stokvel existence.
            RecordRefusal("not_found", userId);
            return;
        }

        // Chairperson-only operations.
        if (requirement.RequireChairperson)
        {
            if (member.Role == "Chairperson")
            {
                context.Succeed(requirement);
            }
            else
            {
                RecordRefusal("forbidden", userId);
            }

            return;
        }

        // Treasurer or Chairperson operations.
        if (requirement.RequireElevatedRole)
        {
            if (member.Role is "Treasurer" or "Chairperson")
            {
                context.Succeed(requirement);
            }
            else
            {
                RecordRefusal("forbidden", userId);
            }

            return;
        }

        // Any stokvel member can perform basic operations.
        context.Succeed(requirement);
    }

    // Record a refusal without personal or financial information.
    private void RecordRefusal(string result, int userId)
    {
        // Only fixed values: not_found or forbidden.
        RondiTrackTelemetry.AuthorizationRefusals.Add(
            1,
            new KeyValuePair<string, object?>("result", result));

        // Log only the internal user ID and trace ID.
        // Never log stokvel names, amounts, emails or tokens.
        _logger.LogInformation(
            "Stokvel authorization refused. UserId={UserId}, Result={Result}, TraceId={TraceId}",
            userId,
            result,
            Activity.Current?.TraceId.ToString());
    }
}