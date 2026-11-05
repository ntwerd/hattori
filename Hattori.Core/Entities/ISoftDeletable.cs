// This code is part of the Hattori project. Licensed under the MIT License.
// See the LICENSE file for full license text.

using System;

namespace Hattori.Core.Entities;

/// <summary>
/// Implemented by entities whose rows are never physically deleted: deleting sets <see cref="IsDeleted"/> and <see cref="DeletedAt"/>.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    /// Whether the row has been soft-deleted.
    /// </summary>
    bool IsDeleted { get; set; }

    /// <summary>
    /// The UTC moment the row was soft-deleted, if it was.
    /// </summary>
    DateTime? DeletedAt { get; set; }
}
