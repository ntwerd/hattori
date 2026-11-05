// This code is part of the Hattori project. Licensed under the MIT License.
// See the LICENSE file for full license text.

using System;

namespace Hattori.Core.Entities;

/// <summary>
/// Implemented by entities whose change history is stamped by the audit interceptor in <c>Hattori.Persistence</c>. Never set these fields by hand.
/// </summary>
public interface IAuditable
{
    /// <summary>
    /// The ID of the user who created the row, if a signed-in user was involved.
    /// </summary>
    Guid? CreatedById { get; set; }

    /// <summary>
    /// The ID of the user who last changed the row, if a signed-in user was involved.
    /// </summary>
    Guid? UpdatedById { get; set; }

    /// <summary>
    /// The UTC moment the row was last changed.
    /// </summary>
    DateTime? UpdatedAt { get; set; }
}
