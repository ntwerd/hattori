// This code is part of the Hattori project. Licensed under the MIT License.
// See the LICENSE file for full license text.

using System;
using Hattori.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Hattori.Persistence;

/// <summary>
/// The Hattori database: identity tables plus todo lists and their items. All EF Core types live in this project only.
/// </summary>
public class HattoriDbContext : IdentityDbContext<HattoriUser, IdentityRole<Guid>, Guid>
{
    public HattoriDbContext(DbContextOptions<HattoriDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Maps the entities with PostgreSQL-native column types, and hides soft-deleted rows from every query.
    /// </summary>
    /// <param name="builder">The model builder.</param>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<TodoList>(entity =>
        {
            entity.HasIndex(list => list.OwnerId);
            // Unique among the user's live lists only: a soft-deleted list frees its name for reuse.
            entity.HasIndex(list => new { list.OwnerId, list.Name }).IsUnique().HasFilter("\"IsDeleted\" = false");
            entity.Property(list => list.Name).HasColumnType("character varying(100)");
            entity.Property(list => list.Color).HasColumnType("character varying(7)");
            entity.Property(list => list.DeletedAt).HasColumnType("timestamp with time zone");
            entity.Property(list => list.UpdatedAt).HasColumnType("timestamp with time zone");
            entity.HasMany(list => list.Items)
                .WithOne(item => item.TodoList)
                .HasForeignKey(item => item.TodoListId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(list => !list.IsDeleted);
        });

        builder.Entity<TodoItem>(entity =>
        {
            entity.HasIndex(item => item.TodoListId);
            entity.Property(item => item.Title).HasColumnType("character varying(200)");
            entity.Property(item => item.Notes).HasColumnType("character varying(2000)");
            entity.Property(item => item.CompletedAt).HasColumnType("timestamp with time zone");
            entity.Property(item => item.DueDate).HasColumnType("date");
            entity.Property(item => item.DeletedAt).HasColumnType("timestamp with time zone");
            entity.Property(item => item.UpdatedAt).HasColumnType("timestamp with time zone");
            entity.HasQueryFilter(item => !item.IsDeleted);
        });

        // OwnerId is scoped by the repositories' queries rather than a foreign key, so repository tests need no identity rows.
    }
}
