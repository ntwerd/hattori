// This code is part of the Hattori project. Licensed under the MIT License.
// See the LICENSE file for full license text.

using Hattori.Core.Configurations;
using Hattori.Core.Interfaces;
using Hattori.Persistence.Interceptors;
using Hattori.Persistence.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hattori.Persistence;

/// <summary>
/// One call from Program.cs that wires up the database, its interceptors and repositories, and the database health check.
/// </summary>
public static class HattoriDatabaseRegistration
{
    /// <summary>
    /// Registers the DbContext with the audit interceptor, the repositories and the database health check.
    /// </summary>
    /// <param name="services">The service collection to register in.</param>
    /// <param name="target">Which database to connect to.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddHattoriPersistence(this IServiceCollection services, DatabaseConnectionTarget target)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<AuditInterceptor>();
        services.AddDbContext<HattoriDbContext>((serviceProvider, options) => options
            .UseNpgsql(DatabaseSettings.GetConnectionString(target))
            .AddInterceptors(serviceProvider.GetRequiredService<AuditInterceptor>()));
        services.AddScoped<ITodoListRepository, TodoListRepository>();
        services.AddScoped<ITodoItemRepository, TodoItemRepository>();
        services.AddHealthChecks().AddDbContextCheck<HattoriDbContext>("database");

        return services;
    }

    /// <summary>
    /// Builds DbContext options for the production or the test database, with the audit interceptor but no user, for tests and tools.
    /// </summary>
    /// <param name="target">Which database to connect to.</param>
    /// <returns>Options a HattoriDbContext can be constructed with.</returns>
    public static DbContextOptions<HattoriDbContext> BuildOptions(DatabaseConnectionTarget target) =>
        new DbContextOptionsBuilder<HattoriDbContext>()
            .UseNpgsql(DatabaseSettings.GetConnectionString(target))
            .AddInterceptors(new AuditInterceptor(new HttpContextAccessor()))
            .Options;
}
