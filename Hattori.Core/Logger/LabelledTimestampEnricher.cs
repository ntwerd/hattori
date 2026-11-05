// This code is part of the Hattori project. Licensed under the MIT License.
// See the LICENSE file for full license text.

using System;
using Serilog.Core;
using Serilog.Events;

namespace Hattori.Core.Logger;

/// <summary>
/// Adds a <c>LogTimestamp</c> property with the event time in the log time zone, e.g. "2026-11-05 12:00:00 GMT", so every line says which zone it is in.
/// </summary>
public class LabelledTimestampEnricher : ILogEventEnricher
{
    private readonly TimeZoneInfo zone;

    public LabelledTimestampEnricher(TimeZoneInfo zone)
    {
        this.zone = zone;
    }

    /// <inheritdoc />
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        DateTime localTime = TimeZoneInfo.ConvertTimeFromUtc(logEvent.Timestamp.UtcDateTime, zone);
        string label = zone.Id == "UTC" ? "GMT" : zone.Id;
        logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("LogTimestamp", $"{localTime:yyyy-MM-dd HH:mm:ss} {label}"));
    }
}
