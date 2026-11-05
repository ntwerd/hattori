// This code is part of the Hattori project. Licensed under the MIT License.
// See the LICENSE file for full license text.

using System;
using Microsoft.AspNetCore.Identity;

namespace Hattori.Core.Entities;

/// <summary>
/// A person with an account in the Hattori API.
/// </summary>
public class HattoriUser : IdentityUser<Guid>
{
}
