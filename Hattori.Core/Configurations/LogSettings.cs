// This code is part of the Hattori project. Licensed under the MIT License.
// See the LICENSE file for full license text.

using System;
using Serilog;

namespace Hattori.Core.Configurations;

/// <summary>
/// The time zone log timestamps are shown in, read from HATTORI_LOG_TIMEZONE as an IANA ID. Unset or invalid means UTC.
/// </summary>
public static class LogSettings
{
    private static readonly Lazy<TimeZoneInfo> time_zone = new Lazy<TimeZoneInfo>(readTimeZone);

    /// <summary>
    /// The time zone log timestamps are shown in. An unset or invalid HATTORI_LOG_TIMEZONE falls back to UTC, with a warning when the ID is invalid.
    /// </summary>
    public static TimeZoneInfo TimeZone => time_zone.Value;

    /// <summary>
    /// Reads and validates HATTORI_LOG_TIMEZONE once.
    /// </summary>
    /// <returns>The configured time zone, or UTC.</returns>
    private static TimeZoneInfo readTimeZone()
    {
        string? zoneId = Environment.GetEnvironmentVariable("HATTORI_LOG_TIMEZONE");

        if (string.IsNullOrWhiteSpace(zoneId))
            return TimeZoneInfo.Utc;

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(zoneId);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            Log.Warning(ex, "HATTORI_LOG_TIMEZONE '{ZoneId}' is not a known time zone ID. Logging in UTC instead.", zoneId);
            return TimeZoneInfo.Utc;
        }
    }
}
