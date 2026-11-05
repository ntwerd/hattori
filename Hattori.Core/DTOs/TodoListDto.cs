// This code is part of the Hattori project. Licensed under the MIT License.
// See the LICENSE file for full license text.

using System;

namespace Hattori.Core.DTOs;

/// <summary>
/// A todo list as the API returns it.
/// </summary>
/// <param name="Id">The list's identifier.</param>
/// <param name="Name">The list's name, unique per user among live lists, stored in lowercase.</param>
/// <param name="Color">The list's optional CSS hex color.</param>
/// <param name="UpdatedAt">The UTC moment the list was last changed.</param>
public record TodoListDto(Guid Id, string Name, string? Color, DateTime? UpdatedAt);
