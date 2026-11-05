// This code is part of the Hattori project. Licensed under the MIT License.
// See the LICENSE file for full license text.

using System;

namespace Hattori.Core.Configurations;

/// <summary>
/// Whether an OpenTelemetry collector is configured through OTEL_EXPORTER_OTLP_ENDPOINT, which is a standard variable and keeps its own name.
/// </summary>
public static class TelemetrySettings
{
    private static readonly Lazy<bool> otlp_endpoint_set = new Lazy<bool>(() => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")));

    /// <summary>
    /// Whether traces and metrics should be exported, which they only are when a collector endpoint is set.
    /// </summary>
    public static bool OtlpEndpointSet => otlp_endpoint_set.Value;
}
