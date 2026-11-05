// This code is part of the Hattori project. Licensed under the MIT License.
// See the LICENSE file for full license text.

using System;
using System.ComponentModel.DataAnnotations;

namespace Hattori.Core.DTOs;

/// <summary>
/// A request to create a todo list.
/// </summary>
/// <param name="Name">The list's name. Trimmed and lowercased; unique per user among live lists.</param>
/// <param name="Color">An optional CSS hex color for the list, e.g. <c>#3A7BD5</c>.</param>
public record CreateTodoListDto(
    [Required]
    [StringLength(100, MinimumLength = 1)]
    string Name,
    [RegularExpression("^#[0-9a-fA-F]{6}$")]
    string? Color);
