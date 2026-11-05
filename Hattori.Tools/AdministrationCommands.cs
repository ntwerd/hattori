// This code is part of the Hattori project. Licensed under the MIT License.
// See the LICENSE file for full license text.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Hattori.Core.Authorization;
using Hattori.Core.Entities;
using Hattori.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Hattori.Tools;

/// <summary>
/// The operations behind the tools commands: accounts, roles and demo data.
/// </summary>
internal static class AdministrationCommands
{
    /// <summary>
    /// Creates an account in the Administrator role, creating the role first if needed. Fails loudly on weak passwords or existing accounts.
    /// </summary>
    /// <param name="userManager">The account manager.</param>
    /// <param name="roleManager">The role manager.</param>
    /// <param name="email">The account's email address.</param>
    /// <param name="password">The account's password.</param>
    public static async Task CreateAdministratorAsync(UserManager<HattoriUser> userManager, RoleManager<IdentityRole<Guid>> roleManager, string email, string password)
    {
        await EnsureRoleExistsAsync(roleManager, DefaultRoles.ADMIN);

        var user = new HattoriUser
        {
            UserName = email,
            Email = email
        };

        IdentityResult created = await userManager.CreateAsync(user, password);

        if (!created.Succeeded)
        {
            foreach (IdentityError error in created.Errors)
                Console.WriteLine($"{error.Code}: {error.Description}");

            return;
        }

        await userManager.AddToRoleAsync(user, DefaultRoles.ADMIN);
        Console.WriteLine($"Administrator '{email}' created.");
    }

    /// <summary>
    /// Creates both default roles with their permission claims if they do not exist yet.
    /// </summary>
    /// <param name="roleManager">The role manager.</param>
    public static async Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> roleManager)
    {
        await EnsureRoleExistsAsync(roleManager, DefaultRoles.USER);
        await EnsureRoleExistsAsync(roleManager, DefaultRoles.ADMIN);
        Console.WriteLine("Default roles are in place.");
    }

    /// <summary>
    /// Reports whether the database answers and which PostgreSQL version it runs.
    /// </summary>
    /// <param name="context">The Hattori database context.</param>
    public static async Task CheckDatabaseAsync(HattoriDbContext context)
    {
        if (!await context.Database.CanConnectAsync())
        {
            Console.WriteLine("The database is unreachable.");
            return;
        }

        string version = await context.Database.SqlQuery<string>($"SELECT version() AS \"Value\"").FirstAsync();
        Console.WriteLine($"The database answers: {version}");
    }

    /// <summary>
    /// Creates two demo todo lists with items for one account, creating the account in the default user role if it does not exist. Idempotent per account.
    /// </summary>
    /// <param name="userManager">The account manager.</param>
    /// <param name="roleManager">The role manager.</param>
    /// <param name="context">The Hattori database context.</param>
    /// <param name="email">The account's email address.</param>
    /// <param name="password">The account's password, used only when the account is created.</param>
    public static async Task SeedDemoDataAsync(UserManager<HattoriUser> userManager, RoleManager<IdentityRole<Guid>> roleManager, HattoriDbContext context, string email, string password)
    {
        await EnsureRoleExistsAsync(roleManager, DefaultRoles.USER);

        HattoriUser? user = await userManager.FindByEmailAsync(email);

        if (user == null)
        {
            user = new HattoriUser
            {
                UserName = email,
                Email = email
            };

            IdentityResult created = await userManager.CreateAsync(user, password);

            if (!created.Succeeded)
            {
                foreach (IdentityError error in created.Errors)
                    Console.WriteLine($"{error.Code}: {error.Description}");

                return;
            }

            await userManager.AddToRoleAsync(user, DefaultRoles.USER);
            Console.WriteLine($"Account '{email}' created.");
        }

        if (await context.Set<TodoList>().AnyAsync(list => list.OwnerId == user.Id))
        {
            Console.WriteLine($"Account '{email}' already has todo lists; nothing seeded.");
            return;
        }

        var chores = new TodoList
        {
            OwnerId = user.Id,
            Name = "chores",
            Color = "#3A7BD5",
            Items = new List<TodoItem>
            {
                new TodoItem
                {
                    Title = "Take out the trash",
                    DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))
                },
                new TodoItem
                {
                    Title = "Water the plants"
                }
            }
        };

        var shopping = new TodoList
        {
            OwnerId = user.Id,
            Name = "shopping",
            Items = new List<TodoItem>
            {
                new TodoItem
                {
                    Title = "Rice",
                    Notes = "The jasmine kind"
                }
            }
        };

        context.Set<TodoList>().AddRange(chores, shopping);
        await context.SaveChangesAsync();
        Console.WriteLine($"Demo lists seeded for '{email}'.");
    }

    /// <summary>
    /// Creates one default role with its permission claims if it does not exist yet.
    /// </summary>
    /// <param name="roleManager">The role manager.</param>
    /// <param name="roleName">The role's name, one of the <see cref="DefaultRoles"/> constants.</param>
    private static async Task EnsureRoleExistsAsync(RoleManager<IdentityRole<Guid>> roleManager, string roleName)
    {
        if (await roleManager.RoleExistsAsync(roleName))
            return;

        var role = new IdentityRole<Guid>(roleName);
        await roleManager.CreateAsync(role);

        foreach (string permission in DefaultRoles.PermissionsFor(roleName))
            await roleManager.AddClaimAsync(role, new Claim(Permissions.CLAIM_TYPE, permission));
    }
}
