// This code is part of the Hattori project. Licensed under the MIT License.
// See the LICENSE file for full license text.

using System;
using System.ComponentModel.DataAnnotations;

namespace Hattori.Core.DTOs;

/// <summary>
/// A request to change a todo item. The whole item is replaced, including its completion state.
/// </summary>
/// <param name="Title">The new item title. Trimmed and stored as entered.</param>
/// <param name="Notes">The new optional details.</param>
/// <param name="IsCompleted">Whether the item is done. Completing stamps the completion time once; reopening clears it.</param>
/// <param name="DueDate">The new optional due date, with no time.</param>
public record UpdateTodoItemDto(
    [Required]
    [StringLength(200, MinimumLength = 1)]
    string Title,
    [StringLength(2000)]
    string? Notes,
    bool IsCompleted,
    DateOnly? DueDate);
