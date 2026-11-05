// This code is part of the Hattori project. Licensed under the MIT License.
// See the LICENSE file for full license text.

using System;

namespace Hattori.Core.DTOs;

/// <summary>
/// The signed-in user's own account.
/// </summary>
/// <param name="Id">The user's identifier.</param>
/// <param name="Email">The user's email address.</param>
public record AccountDto(Guid Id, string Email);
