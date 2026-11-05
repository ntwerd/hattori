// This code is part of the Hattori project. Licensed under the MIT License.
// See the LICENSE file for full license text.

using Microsoft.AspNetCore.Authorization;

namespace Hattori.Core.Authorization;

/// <summary>
/// Requires that the signed-in user carries one named permission as a claim. Checked by the permission authorization handler in Hattori.Web.
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Creates a requirement for one permission.
    /// </summary>
    /// <param name="permission">The permission the user must carry, one of the <see cref="Permissions"/> constants.</param>
    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }

    /// <summary>
    /// The permission the user must carry.
    /// </summary>
    public string Permission { get; }
}
