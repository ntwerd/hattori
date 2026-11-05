// This code is part of the Hattori project. Licensed under the MIT License.
// See the LICENSE file for full license text.

namespace Hattori.Core.Authorization;

/// <summary>
/// Every permission of the Hattori API as a constant, the claim type they travel as, and the list of them all for policy registration.
/// </summary>
public static class Permissions
{
    /// <summary>
    /// The claim type a role carries its permissions as.
    /// </summary>
    public const string CLAIM_TYPE = "permission";

    /// <summary>
    /// Permissions about todo lists.
    /// </summary>
    public static class TodoList
    {
        /// <summary>
        /// Read the user's todo lists.
        /// </summary>
        public const string READ = "TodoList.Read";

        /// <summary>
        /// Create, change and delete the user's todo lists.
        /// </summary>
        public const string WRITE = "TodoList.Write";
    }

    /// <summary>
    /// Permissions about todo items.
    /// </summary>
    public static class TodoItem
    {
        /// <summary>
        /// Read the user's todo items.
        /// </summary>
        public const string READ = "TodoItem.Read";

        /// <summary>
        /// Create, change and delete the user's todo items.
        /// </summary>
        public const string WRITE = "TodoItem.Write";
    }

    /// <summary>
    /// Permissions about one's own account.
    /// </summary>
    public static class Account
    {
        /// <summary>
        /// Read one's own account.
        /// </summary>
        public const string READ = "Account.Read";
    }

    /// <summary>
    /// Permissions for administrators.
    /// </summary>
    public static class Administration
    {
        /// <summary>
        /// Manage other users' accounts.
        /// </summary>
        public const string USER_MANAGEMENT = "Administration.UserManagement";
    }

    /// <summary>
    /// Every permission, each of which gets an authorization policy in Program.cs.
    /// </summary>
    public static readonly string[] ALL = new[]
    {
        TodoList.READ,
        TodoList.WRITE,
        TodoItem.READ,
        TodoItem.WRITE,
        Account.READ,
        Administration.USER_MANAGEMENT
    };
}
