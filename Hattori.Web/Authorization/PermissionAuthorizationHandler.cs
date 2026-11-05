// This code is part of the Hattori project. Licensed under the MIT License.
// See the LICENSE file for full license text.

using System.Threading.Tasks;
using Hattori.Core.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace Hattori.Web.Authorization;

/// <summary>
/// Succeeds when the signed-in user carries the required permission as a claim, which the claims factory put there from the user's roles.
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    /// <inheritdoc />
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.HasClaim(Permissions.CLAIM_TYPE, requirement.Permission))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
