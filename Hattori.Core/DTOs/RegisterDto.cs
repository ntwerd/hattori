// This code is part of the Hattori project. Licensed under the MIT License.
// See the LICENSE file for full license text.

using System;
using System.ComponentModel.DataAnnotations;

namespace Hattori.Core.DTOs;

/// <summary>
/// A registration request: an email address and a password.
/// </summary>
/// <param name="Email">The new account's email address, also used as the user name.</param>
/// <param name="Password">The new account's password. Needs an uppercase letter, a lowercase letter, a digit and a symbol.</param>
public record RegisterDto(
    [Required]
    [EmailAddress]
    string Email,
    [Required]
    [StringLength(128, MinimumLength = 8)]
    string Password);
