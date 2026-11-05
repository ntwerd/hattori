// This code is part of the Hattori project. Licensed under the MIT License.
// See the LICENSE file for full license text.

using System.ComponentModel.DataAnnotations;

namespace Hattori.Core.DTOs;

/// <summary>
/// A request to rename or recolor a todo list.
/// </summary>
/// <param name="Name">The new list name. Trimmed and lowercased; unique per user among live lists.</param>
/// <param name="Color">The new optional CSS hex color for the list.</param>
public record UpdateTodoListDto(
    [Required]
    [StringLength(100, MinimumLength = 1)]
    string Name,
    [RegularExpression("^#[0-9a-fA-F]{6}$")]
    string? Color);
