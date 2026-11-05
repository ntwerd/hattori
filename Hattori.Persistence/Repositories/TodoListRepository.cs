// This code is part of the Hattori project. Licensed under the MIT License.
// See the LICENSE file for full license text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hattori.Core.DTOs;
using Hattori.Core.Entities;
using Hattori.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hattori.Persistence.Repositories;

/// <summary>
/// Reads and writes todo lists with EF Core, projected into DTOs, scoped to one owning user per query.
/// </summary>
public class TodoListRepository : ITodoListRepository
{
    private readonly HattoriDbContext context;

    public TodoListRepository(HattoriDbContext context)
    {
        this.context = context;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TodoListDto>> ListAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        List<TodoListDto> lists = await context.Set<TodoList>()
            .AsNoTracking()
            .Where(list => list.OwnerId == ownerId)
            .OrderBy(list => list.Name)
            .Select(list => new TodoListDto(list.Id, list.Name, list.Color, list.UpdatedAt))
            .ToListAsync(cancellationToken);

        return lists;
    }

    /// <inheritdoc />
    public Task<TodoListDto?> GetAsync(Guid ownerId, Guid id, CancellationToken cancellationToken) =>
        context.Set<TodoList>()
            .AsNoTracking()
            .Where(list => list.Id == id && list.OwnerId == ownerId)
            .Select(list => new TodoListDto(list.Id, list.Name, list.Color, list.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExistsAsync(Guid ownerId, string name, CancellationToken cancellationToken) =>
        context.Set<TodoList>().AnyAsync(list => list.OwnerId == ownerId && list.Name == name, cancellationToken);

    /// <inheritdoc />
    public async Task<TodoListDto> CreateAsync(Guid ownerId, string name, string? color, CancellationToken cancellationToken)
    {
        var list = new TodoList
        {
            OwnerId = ownerId,
            Name = name,
            Color = color
        };

        context.Set<TodoList>().Add(list);
        await context.SaveChangesAsync(cancellationToken);

        return new TodoListDto(list.Id, list.Name, list.Color, list.UpdatedAt);
    }

    /// <inheritdoc />
    public async Task<TodoListDto?> UpdateAsync(Guid ownerId, Guid id, string name, string? color, CancellationToken cancellationToken)
    {
        TodoList? list = await context.Set<TodoList>()
            .FirstOrDefaultAsync(l => l.Id == id && l.OwnerId == ownerId, cancellationToken);

        if (list == null)
            return null;

        list.Name = name;
        list.Color = color;
        await context.SaveChangesAsync(cancellationToken);

        return new TodoListDto(list.Id, list.Name, list.Color, list.UpdatedAt);
    }

    /// <inheritdoc />
    public async Task<bool> SoftDeleteAsync(Guid ownerId, Guid id, CancellationToken cancellationToken)
    {
        TodoList? list = await context.Set<TodoList>()
            .FirstOrDefaultAsync(l => l.Id == id && l.OwnerId == ownerId, cancellationToken);

        if (list == null)
            return false;

        list.IsDeleted = true;
        list.DeletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
