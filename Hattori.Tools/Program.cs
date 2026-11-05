// This code is part of the Hattori project. Licensed under the MIT License.
// See the LICENSE file for full license text.

using System;
using System.CommandLine;
using System.Threading.Tasks;
using Hattori.Core.Configurations;
using Hattori.Core.Entities;
using Hattori.Persistence;
using Hattori.Persistence.Interceptors;
using Hattori.Tools;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection();
services.AddLogging(options => options.AddConsole());
services.AddDataProtection();
services.AddHttpContextAccessor();
services.AddSingleton<AuditInterceptor>();
services.AddDbContext<HattoriDbContext>((serviceProvider, options) => options
    .UseNpgsql(DatabaseSettings.GetConnectionString(DatabaseConnectionTarget.Production))
    .AddInterceptors(serviceProvider.GetRequiredService<AuditInterceptor>()));
services.AddIdentityCore<HattoriUser>()
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<HattoriDbContext>()
    .AddDefaultTokenProviders();

var provider = services.BuildServiceProvider();

var emailOption = new Option<string>("--email") { Required = true, Description = "The account's email address." };
var passwordOption = new Option<string>("--password") { Required = true, Description = "The account's password." };

var createAdminCommand = new Command("create-admin", "Create an account in the Administrator role.")
{
    emailOption,
    passwordOption
};
createAdminCommand.SetAction(async (parseResult, cancellationToken) =>
{
    await AdministrationCommands.CreateAdministratorAsync(
        provider.GetRequiredService<UserManager<HattoriUser>>(),
        provider.GetRequiredService<RoleManager<IdentityRole<Guid>>>(),
        parseResult.GetValue(emailOption)!,
        parseResult.GetValue(passwordOption)!);
});

var usersCommand = new Command("users", "Account maintenance.")
{
    createAdminCommand
};

var rolesSeedCommand = new Command("seed", "Create the default roles with their permission claims. Idempotent.");
rolesSeedCommand.SetAction(async (parseResult, cancellationToken) => await AdministrationCommands.SeedRolesAsync(provider.GetRequiredService<RoleManager<IdentityRole<Guid>>>()));

var rolesCommand = new Command("roles", "Role maintenance.")
{
    rolesSeedCommand
};

var dbCheckCommand = new Command("check", "Try to connect to the database and report its version.");
dbCheckCommand.SetAction(async (parseResult, cancellationToken) => await AdministrationCommands.CheckDatabaseAsync(provider.GetRequiredService<HattoriDbContext>()));

var dbSeedDemoCommand = new Command("seed-demo", "Create two demo todo lists with items for one account, creating the account if needed.")
{
    emailOption,
    passwordOption
};
dbSeedDemoCommand.SetAction(async (parseResult, cancellationToken) =>
{
    await AdministrationCommands.SeedDemoDataAsync(
        provider.GetRequiredService<UserManager<HattoriUser>>(),
        provider.GetRequiredService<RoleManager<IdentityRole<Guid>>>(),
        provider.GetRequiredService<HattoriDbContext>(),
        parseResult.GetValue(emailOption)!,
        parseResult.GetValue(passwordOption)!);
});

var dbCommand = new Command("db", "Database maintenance.")
{
    dbCheckCommand,
    dbSeedDemoCommand
};

var rootCommand = new RootCommand("Maintenance tools for the Hattori todo API. Run with no arguments for an interactive menu.")
{
    usersCommand,
    rolesCommand,
    dbCommand
};

return args.Length == 0
    ? await Menu.RunAsync(rootCommand)
    : await rootCommand.Parse(args).InvokeAsync();

/// <summary>
/// The no-argument interactive menu: pick a command by number, which runs the same command tree.
/// </summary>
internal static class Menu
{
    /// <summary>
    /// Shows the menu until quit is chosen.
    /// </summary>
    /// <param name="rootCommand">The command tree the menu entries run.</param>
    /// <returns>The process exit code.</returns>
    public static async Task<int> RunAsync(RootCommand rootCommand)
    {
        string[][] entries = new[]
        {
            new[] { "users", "create-admin" },
            new[] { "roles", "seed" },
            new[] { "db", "check" },
            new[] { "db", "seed-demo" }
        };

        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("Hattori tools");
            Console.WriteLine("  1) users create-admin");
            Console.WriteLine("  2) roles seed");
            Console.WriteLine("  3) db check");
            Console.WriteLine("  4) db seed-demo");
            Console.WriteLine("  q) quit");
            Console.Write("Choose: ");
            string? choice = Console.ReadLine();

            if (choice is null or "q")
                return 0;

            if (!int.TryParse(choice, out int number) || number < 1 || number > entries.Length)
            {
                Console.WriteLine($"Unknown choice '{choice}'.");
                continue;
            }

            int exitCode = await rootCommand.Parse(entries[number - 1]).InvokeAsync();

            if (exitCode != 0)
                return exitCode;
        }
    }
}
