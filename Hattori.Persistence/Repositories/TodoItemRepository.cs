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
/// Reads and writes todo items with EF Core, projected into DTOs, scoped to one owning user per query through the item's list.
/// </summary>
public class TodoItemRepository : ITodoItemRepository
{
    private readonly HattoriDbContext context;

    public TodoItemRepository(HattoriDbContext context)
    {
        this.context = context;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TodoItemDto>> ListAsync(Guid ownerId, Guid? todoListId, CancellationToken cancellationToken)
    {
        List<TodoItemDto> items = await context.Set<TodoItem>()
            .AsNoTracking()
            .Where(item => item.TodoList!.OwnerId == ownerId && (todoListId == null || item.TodoListId == todoListId))
            .OrderBy(item => item.Title)
            .Select(item => new TodoItemDto(item.Id, item.TodoListId, item.Title, item.Notes, item.IsCompleted, item.CompletedAt, item.DueDate, item.UpdatedAt))
            .ToListAsync(cancellationToken);

        return items;
    }

    /// <inheritdoc />
    public Task<TodoItemDto?> GetAsync(Guid ownerId, Guid id, CancellationToken cancellationToken) =>
        context.Set<TodoItem>()
            .AsNoTracking()
            .Where(item => item.Id == id && item.TodoList!.OwnerId == ownerId)
            .Select(item => new TodoItemDto(item.Id, item.TodoListId, item.Title, item.Notes, item.IsCompleted, item.CompletedAt, item.DueDate, item.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<TodoItemDto> CreateAsync(Guid ownerId, Guid todoListId, string title, string? notes, DateOnly? dueDate, CancellationToken cancellationToken)
    {
        var item = new TodoItem
        {
            TodoListId = todoListId,
            Title = title,
            Notes = notes,
            DueDate = dueDate
        };

        context.Set<TodoItem>().Add(item);
        await context.SaveChangesAsync(cancellationToken);

        return new TodoItemDto(item.Id, item.TodoListId, item.Title, item.Notes, item.IsCompleted, item.CompletedAt, item.DueDate, item.UpdatedAt);
    }

    /// <inheritdoc />
    public async Task<TodoItemDto?> UpdateAsync(Guid ownerId, Guid id, string title, string? notes, bool isCompleted, DateOnly? dueDate, CancellationToken cancellationToken)
    {
        TodoItem? item = await context.Set<TodoItem>()
            .FirstOrDefaultAsync(i => i.Id == id && i.TodoList!.OwnerId == ownerId, cancellationToken);

        if (item == null)
            return null;

        item.Title = title;
        item.Notes = notes;
        item.DueDate = dueDate;
        item.IsCompleted = isCompleted;
        // The first completion time is kept; reopening and completing again stamps a new one only after the old was cleared.
        item.CompletedAt = isCompleted ? item.CompletedAt ?? DateTime.UtcNow : null;
        await context.SaveChangesAsync(cancellationToken);

        return new TodoItemDto(item.Id, item.TodoListId, item.Title, item.Notes, item.IsCompleted, item.CompletedAt, item.DueDate, item.UpdatedAt);
    }

    /// <inheritdoc />
    public async Task<bool> SoftDeleteAsync(Guid ownerId, Guid id, CancellationToken cancellationToken)
    {
        TodoItem? item = await context.Set<TodoItem>()
            .FirstOrDefaultAsync(i => i.Id == id && i.TodoList!.OwnerId == ownerId, cancellationToken);

        if (item == null)
            return false;

        item.IsDeleted = true;
        item.DeletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
