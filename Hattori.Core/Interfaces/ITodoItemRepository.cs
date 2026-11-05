// This code is part of the Hattori project. Licensed under the MIT License.
// See the LICENSE file for full license text.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hattori.Core.DTOs;

namespace Hattori.Core.Interfaces;

/// <summary>
/// Reads and writes the todo items of the users of the Hattori API.
/// </summary>
public interface ITodoItemRepository
{
    /// <summary>
    /// Lists the live todo items of one user, all of them or one list's, ordered by title.
    /// </summary>
    /// <param name="ownerId">The owning user's identifier.</param>
    /// <param name="todoListId">Restricts the result to one list's items, or null for all of them.</param>
    /// <param name="cancellationToken">A token that cancels the query.</param>
    Task<IReadOnlyList<TodoItemDto>> ListAsync(Guid ownerId, Guid? todoListId, CancellationToken cancellationToken);

    /// <summary>
    /// Reads one live todo item, if its list belongs to the user.
    /// </summary>
    /// <param name="ownerId">The owning user's identifier.</param>
    /// <param name="id">The item's identifier.</param>
    /// <param name="cancellationToken">A token that cancels the query.</param>
    Task<TodoItemDto?> GetAsync(Guid ownerId, Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a todo item in one of the user's lists.
    /// </summary>
    /// <param name="ownerId">The owning user's identifier.</param>
    /// <param name="todoListId">The list's identifier.</param>
    /// <param name="title">The item title, already trimmed.</param>
    /// <param name="notes">Optional details.</param>
    /// <param name="dueDate">The optional due date, with no time.</param>
    /// <param name="cancellationToken">A token that cancels the save.</param>
    Task<TodoItemDto> CreateAsync(Guid ownerId, Guid todoListId, string title, string? notes, DateOnly? dueDate, CancellationToken cancellationToken);

    /// <summary>
    /// Changes one live todo item, if its list belongs to the user. Completing stamps <c>CompletedAt</c> once; reopening clears it.
    /// </summary>
    /// <param name="ownerId">The owning user's identifier.</param>
    /// <param name="id">The item's identifier.</param>
    /// <param name="title">The new item title, already trimmed.</param>
    /// <param name="notes">The new optional details.</param>
    /// <param name="isCompleted">Whether the item is done.</param>
    /// <param name="dueDate">The new optional due date.</param>
    /// <param name="cancellationToken">A token that cancels the save.</param>
    Task<TodoItemDto?> UpdateAsync(Guid ownerId, Guid id, string title, string? notes, bool isCompleted, DateOnly? dueDate, CancellationToken cancellationToken);

    /// <summary>
    /// Soft-deletes one live todo item, if its list belongs to the user.
    /// </summary>
    /// <param name="ownerId">The owning user's identifier.</param>
    /// <param name="id">The item's identifier.</param>
    /// <param name="cancellationToken">A token that cancels the save.</param>
    /// <returns>Whether the item was found and deleted.</returns>
    Task<bool> SoftDeleteAsync(Guid ownerId, Guid id, CancellationToken cancellationToken);
}
