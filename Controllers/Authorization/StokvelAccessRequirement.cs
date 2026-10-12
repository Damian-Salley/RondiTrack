using Microsoft.AspNetCore.Authorization;

namespace RondiTrack.Authorization;

public class StokvelAccessRequirement : IAuthorizationRequirement
{
    public bool RequireElevatedRole { get; }
    public bool RequireChairperson { get; }

    public StokvelAccessRequirement(
        bool requireElevatedRole = false,
        bool requireChairperson = false)
    {
        RequireElevatedRole = requireElevatedRole;
        RequireChairperson = requireChairperson;
    }
}