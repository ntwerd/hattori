// This code is part of the Hattori project. Licensed under the MIT License.
// See the LICENSE file for full license text.

using System;
using Hattori.Core.Development;
using Serilog;

namespace Hattori.Core.Configurations;

/// <summary>
/// The one place that reads environment variables, so every settings class follows the same rule: read once, cache, debug default or fail.
/// </summary>
public static class EnvironmentVariables
{
    /// <summary>
    /// Reads a variable the app can't run without. A debug build falls back to <paramref name="debugDefault"/> with a warning; a release build throws.
    /// </summary>
    /// <param name="name">The variable's name, e.g. <c>HATTORI_DATABASE_CONNECTION_STRING</c>.</param>
    /// <param name="debugDefault">The value a debug build falls back to.</param>
    /// <returns>The variable's value.</returns>
    /// <exception cref="InvalidOperationException">The variable is unset in a release build.</exception>
    public static string Required(string name, string debugDefault)
    {
        string? value = Environment.GetEnvironmentVariable(name);

        if (!string.IsNullOrWhiteSpace(value))
            return value;

        if (!DebugUtils.IsDebugBuild)
            throw new InvalidOperationException($"{name} environment variable is not set.");

        Log.Warning("{EnvironmentVariableName} environment variable is not set. Using default value. (Default: {DefaultValue})", name, debugDefault);
        return debugDefault;
    }
}
