// This code is part of the Hattori project. Licensed under the MIT License.
// See the LICENSE file for full license text.

using System;

namespace Hattori.Core.Development;

/// <summary>
/// Answers questions whose answer depends on the build configuration.
/// </summary>
public static class DebugUtils
{
    /// <summary>
    /// Whether the running code was built in the Debug configuration.
    /// </summary>
    public static bool IsDebugBuild
    {
        get
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }
    }
}
