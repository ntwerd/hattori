// This code is part of the Hattori project. Licensed under the MIT License.
// See the LICENSE file for full license text.

using System;
using System.Collections.Generic;

namespace Hattori.Core.Entities;

/// <summary>
/// A named group of todo items owned by one user. Deleting is soft: the row stays, with <see cref="ISoftDeletable.IsDeleted"/> set.
/// </summary>
public class TodoList : ISoftDeletable, IAuditable
{
    /// <summary>
    /// The list's identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The owning user's identifier. Scoped by the repositories' queries rather than a foreign key, so repository tests need no identity rows.
    /// </summary>
    public Guid OwnerId { get; set; }

    /// <summary>
    /// The list's name, unique per user among live lists, stored in lowercase.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// An optional CSS hex color for the list, e.g. <c>#3A7BD5</c>.
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// The todo items in this list.
    /// </summary>
    public List<TodoItem> Items { get; set; } = new List<TodoItem>();

    /// <inheritdoc />
    public bool IsDeleted { get; set; }

    /// <inheritdoc />
    public DateTime? DeletedAt { get; set; }

    /// <inheritdoc />
    public Guid? CreatedById { get; set; }

    /// <inheritdoc />
    public Guid? UpdatedById { get; set; }

    /// <inheritdoc />
    public DateTime? UpdatedAt { get; set; }
}
