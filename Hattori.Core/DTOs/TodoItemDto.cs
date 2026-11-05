// This code is part of the Hattori project. Licensed under the MIT License.
// See the LICENSE file for full license text.

using System;

namespace Hattori.Core.DTOs;

/// <summary>
/// A todo item as the API returns it.
/// </summary>
/// <param name="Id">The item's identifier.</param>
/// <param name="TodoListId">The identifier of the list the item belongs to.</param>
/// <param name="Title">What there is to do.</param>
/// <param name="Notes">Optional details about the item.</param>
/// <param name="IsCompleted">Whether the item is done.</param>
/// <param name="CompletedAt">The UTC moment the item was completed, if it is.</param>
/// <param name="DueDate">The date the item is due, with no time and no time zone.</param>
/// <param name="UpdatedAt">The UTC moment the item was last changed.</param>
public record TodoItemDto(Guid Id, Guid TodoListId, string Title, string? Notes, bool IsCompleted, DateTime? CompletedAt, DateOnly? DueDate, DateTime? UpdatedAt);
