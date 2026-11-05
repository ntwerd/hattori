// This code is part of the Hattori project. Licensed under the MIT License.
// See the LICENSE file for full license text.

using System;

namespace Hattori.Core.Authorization;

/// <summary>
/// The roles accounts can carry, and the permissions each role grants. Roles are created by Hattori.Tools or on first registration.
/// </summary>
public static class DefaultRoles
{
    /// <summary>
    /// The role every self-registered account receives: full use of one's own todos.
    /// </summary>
    public const string USER = "User";

    /// <summary>
    /// The administrator role, created by the tools: everything, including user management.
    /// </summary>
    public const string ADMIN = "Administrator";

    /// <summary>
    /// The permissions a role grants as permission claims.
    /// </summary>
    /// <param name="roleName">The role's name, one of this class's constants.</param>
    /// <returns>The permission names to attach to the role.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The role name is not one of this class's constants.</exception>
    public static string[] PermissionsFor(string roleName) => roleName switch
    {
        USER => new[]
        {
            Permissions.TodoList.READ,
            Permissions.TodoList.WRITE,
            Permissions.TodoItem.READ,
            Permissions.TodoItem.WRITE,
            Permissions.Account.READ
        },
        ADMIN => Permissions.ALL,
        _ => throw new ArgumentOutOfRangeException(nameof(roleName), roleName, "Unknown default role.")
    };
}
