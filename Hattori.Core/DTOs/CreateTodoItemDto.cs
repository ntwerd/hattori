// This code is part of the Hattori project. Licensed under the MIT License.
// See the LICENSE file for full license text.

using System;
using System.ComponentModel.DataAnnotations;

namespace Hattori.Core.DTOs;

/// <summary>
/// A request to create a todo item.
/// </summary>
/// <param name="TodoListId">The list to create the item in. Must be one of the signed-in user's live lists.</param>
/// <param name="Title">What there is to do. Trimmed and stored as entered.</param>
/// <param name="Notes">Optional details about the item.</param>
/// <param name="DueDate">The optional due date, with no time and no time zone.</param>
public record CreateTodoItemDto(
    [Required]
    Guid TodoListId,
    [Required]
    [StringLength(200, MinimumLength = 1)]
    string Title,
    [StringLength(2000)]
    string? Notes,
    DateOnly? DueDate);
