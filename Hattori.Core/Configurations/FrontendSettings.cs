// This code is part of the Hattori project. Licensed under the MIT License.
// See the LICENSE file for full license text.

using System;
using System.Collections.Generic;

namespace Hattori.Core.Configurations;

/// <summary>
/// The origins of the frontends this API serves, read from HATTORI_FRONTEND_BASE_URL as a comma-separated list.
/// </summary>
public static class FrontendSettings
{
    private const string default_base_urls = "http://localhost:3000";

    private static readonly Lazy<string> base_urls = new Lazy<string>(() => EnvironmentVariables.Required("HATTORI_FRONTEND_BASE_URL", default_base_urls));

    /// <summary>
    /// The frontend origins CORS allows. Set several by separating them with commas in the environment variable.
    /// </summary>
    public static IReadOnlyList<string> BaseUrls => base_urls.Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
}
