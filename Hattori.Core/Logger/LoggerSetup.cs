// This code is part of the Hattori project. Licensed under the MIT License.
// See the LICENSE file for full license text.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Hattori.Core.Configurations;
using Serilog;
using Serilog.Events;

namespace Hattori.Core.Logger;

/// <summary>
/// Configures Serilog once: console and a rolling file, OpenTelemetry when a collector endpoint is set, and a Fatal record for unhandled exceptions.
/// </summary>
public static class LoggerSetup
{
    private const string output_template = "{LogTimestamp} [{Level:u3}]: {Message:lj}{NewLine}{Exception}";

    /// <summary>
    /// Configures Serilog: console, rolling file, and OpenTelemetry when an OTLP endpoint is set.
    /// </summary>
    /// <param name="isDevelopment">Whether the app runs in development, which logs at Debug level.</param>
    /// <param name="serviceName">The service name telemetry and the startup header report.</param>
    public static void Configure(bool isDevelopment, string serviceName)
    {
        var configuration = new LoggerConfiguration()
            .MinimumLevel.Is(isDevelopment ? LogEventLevel.Debug : LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .Filter.ByExcluding(e => e.Properties.TryGetValue("SourceContext", out var source) && source.ToString().Contains("Microsoft.EntityFrameworkCore"))
            .Enrich.With(new LabelledTimestampEnricher(LogSettings.TimeZone))
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: output_template)
            .WriteTo.File("logs/log.txt",
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true,
                fileSizeLimitBytes: 50L * 1024 * 1024,
                retainedFileTimeLimit: TimeSpan.FromDays(14),
                outputTemplate: output_template);

        if (TelemetrySettings.OtlpEndpointSet)
            configuration.WriteTo.OpenTelemetry(options => options.ResourceAttributes["service.name"] = serviceName);

        Log.Logger = configuration.CreateLogger();

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            Log.Fatal(e.ExceptionObject as Exception, "An unhandled exception occurred");
            Log.CloseAndFlush();
        };
    }

    /// <summary>
    /// Logs a short header that says what wrote this log: app, version, environment, machine, run time and zone, and the log file's location.
    /// </summary>
    /// <param name="serviceName">The service name to report.</param>
    /// <param name="environment">The environment name, e.g. Development.</param>
    /// <param name="version">The app's version.</param>
    public static void LogStartupHeader(string serviceName, string environment, string version)
    {
        TimeZoneInfo zone = LogSettings.TimeZone;

        Log.Information("{ServiceName} starting: version {Version}, environment {Environment}, .NET {DotNetVersion}, {OSDescription} with {ProcessorCount} CPU(s)",
            serviceName, version, environment, Environment.Version, RuntimeInformation.OSDescription, Environment.ProcessorCount);
        Log.Information("Log timestamps are in {ZoneId}; log files are written to {LogFolder}", zone.Id, Path.GetFullPath("logs"));
        Log.Information("Run started at {StartTime:yyyy-MM-dd HH:mm:ss} GMT", DateTime.UtcNow);
    }
}
