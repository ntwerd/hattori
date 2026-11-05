// This code is part of the Hattori project. Licensed under the MIT License.
// See the LICENSE file for full license text.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Hattori.Core.Authorization;
using Hattori.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Hattori.Persistence;

/// <summary>
/// Adds the permission claims of the user's roles to the cookie when it is issued, so <c>[Authorize(Policy = …)]</c> can check them on every request.
/// </summary>
public class HattoriUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<HattoriUser, IdentityRole<Guid>>
{
    public HattoriUserClaimsPrincipalFactory(UserManager<HattoriUser> userManager, RoleManager<IdentityRole<Guid>> roleManager, IOptions<IdentityOptions> optionsAccessor)
        : base(userManager, roleManager, optionsAccessor)
    {
    }

    /// <inheritdoc />
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(HattoriUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        foreach (string roleName in await UserManager.GetRolesAsync(user))
        {
            IdentityRole<Guid>? role = await RoleManager.FindByNameAsync(roleName);

            if (role == null)
                continue;

            foreach (Claim permission in await RoleManager.GetClaimsAsync(role))
            {
                if (permission.Type == Permissions.CLAIM_TYPE && !identity.HasClaim(permission.Type, permission.Value))
                    identity.AddClaim(permission);
            }
        }

        return identity;
    }
}
