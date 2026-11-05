// This code is part of the Hattori project. Licensed under the MIT License.
// See the LICENSE file for full license text.

using System;

namespace Hattori.Core.Entities;

/// <summary>
/// A single todo item in a <see cref="TodoList"/>. Deleting is soft: the row stays, with <see cref="ISoftDeletable.IsDeleted"/> set.
/// </summary>
public class TodoItem : ISoftDeletable, IAuditable
{
    /// <summary>
    /// The item's identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The identifier of the list the item belongs to. Items of a deleted list become invisible with it.
    /// </summary>
    public Guid TodoListId { get; set; }

    /// <summary>
    /// The owning list.
    /// </summary>
    public TodoList TodoList { get; set; } = null!;

    /// <summary>
    /// What there is to do, stored as entered.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Optional details about the item.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Whether the item is done.
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// The UTC moment the item was completed, kept from the first completion.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// The date the item is due, with no time and no time zone. Never converted; display is the viewer's choice.
    /// </summary>
    public DateOnly? DueDate { get; set; }

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
