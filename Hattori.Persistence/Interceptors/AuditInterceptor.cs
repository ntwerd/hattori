// This code is part of the Hattori project. Licensed under the MIT License.
// See the LICENSE file for full license text.

using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Hattori.Core.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Hattori.Persistence.Interceptors;

/// <summary>
/// Stamps the audit fields of every changed <see cref="IAuditable"/> entity with the current user and the UTC moment. Registered as a singleton, added to the DbContext in <see cref="HattoriDatabaseRegistration"/>.
/// </summary>
public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public AuditInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        stampAuditFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        stampAuditFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// Stamps added entities as created and modified, and modified entities as updated. Nothing happens outside a request: a tools run leaves the user columns null.
    /// </summary>
    /// <param name="context">The context whose changes are about to be saved, if any.</param>
    private void stampAuditFields(DbContext? context)
    {
        if (context == null)
            return;

        Guid? currentUserId = getCurrentUserId();

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedById = currentUserId;
                entry.Entity.UpdatedById = currentUserId;
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedById = currentUserId;
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }

    /// <summary>
    /// Reads the signed-in user's ID from the current request, if there is one.
    /// </summary>
    /// <returns>The user's ID, or null outside a request or signed out.</returns>
    private Guid? getCurrentUserId()
    {
        string? userId = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(userId, out Guid parsed) ? parsed : null;
    }
}
