// This code is part of the Hattori project. Licensed under the MIT License.
// See the LICENSE file for full license text.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hattori.Core.DTOs;

namespace Hattori.Core.Interfaces;

/// <summary>
/// Reads and writes the todo lists of the users of the Hattori API.
/// </summary>
public interface ITodoListRepository
{
    /// <summary>
    /// Lists every live todo list of one user, ordered by name.
    /// </summary>
    /// <param name="ownerId">The owning user's identifier.</param>
    /// <param name="cancellationToken">A token that cancels the query.</param>
    Task<IReadOnlyList<TodoListDto>> ListAsync(Guid ownerId, CancellationToken cancellationToken);

    /// <summary>
    /// Reads one live todo list, if it belongs to the user.
    /// </summary>
    /// <param name="ownerId">The owning user's identifier.</param>
    /// <param name="id">The list's identifier.</param>
    /// <param name="cancellationToken">A token that cancels the query.</param>
    Task<TodoListDto?> GetAsync(Guid ownerId, Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Whether one user already has a live list with a name.
    /// </summary>
    /// <param name="ownerId">The owning user's identifier.</param>
    /// <param name="name">The list name to check, already normalized.</param>
    /// <param name="cancellationToken">A token that cancels the query.</param>
    Task<bool> ExistsAsync(Guid ownerId, string name, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a todo list for one user.
    /// </summary>
    /// <param name="ownerId">The owning user's identifier.</param>
    /// <param name="name">The list name, already normalized.</param>
    /// <param name="color">An optional CSS hex color.</param>
    /// <param name="cancellationToken">A token that cancels the save.</param>
    Task<TodoListDto> CreateAsync(Guid ownerId, string name, string? color, CancellationToken cancellationToken);

    /// <summary>
    /// Renames or recolors one live todo list, if it belongs to the user.
    /// </summary>
    /// <param name="ownerId">The owning user's identifier.</param>
    /// <param name="id">The list's identifier.</param>
    /// <param name="name">The new list name, already normalized.</param>
    /// <param name="color">The new optional CSS hex color.</param>
    /// <param name="cancellationToken">A token that cancels the save.</param>
    Task<TodoListDto?> UpdateAsync(Guid ownerId, Guid id, string name, string? color, CancellationToken cancellationToken);

    /// <summary>
    /// Soft-deletes one live todo list, if it belongs to the user.
    /// </summary>
    /// <param name="ownerId">The owning user's identifier.</param>
    /// <param name="id">The list's identifier.</param>
    /// <param name="cancellationToken">A token that cancels the save.</param>
    /// <returns>Whether the list was found and deleted.</returns>
    Task<bool> SoftDeleteAsync(Guid ownerId, Guid id, CancellationToken cancellationToken);
}
