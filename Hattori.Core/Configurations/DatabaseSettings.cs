// This code is part of the Hattori project. Licensed under the MIT License.
// See the LICENSE file for full license text.

using System;

namespace Hattori.Core.Configurations;

/// <summary>
/// Which Hattori database a connection is for: the one the app serves, or the one the tests run against.
/// </summary>
public enum DatabaseConnectionTarget
{
    /// <summary>
    /// The database the running app serves.
    /// </summary>
    Production,

    /// <summary>
    /// The separate database the tests run against.
    /// </summary>
    Test
}

/// <summary>
/// The PostgreSQL connection strings, read once from HATTORI_DATABASE_CONNECTION_STRING and HATTORI_TEST_DATABASE_CONNECTION_STRING.
/// </summary>
public static class DatabaseSettings
{
    private const string default_connection_string = "Host=localhost;Database=hattori;Username=postgres;Password=mysecretpassword";
    private const string default_test_connection_string = "Host=localhost;Database=hattori_test;Username=postgres;Password=mysecretpassword;Port=5433";

    // Lazy keeps the first result, so each variable is read once per process.
    private static readonly Lazy<string> production_connection_string = new Lazy<string>(() => EnvironmentVariables.Required("HATTORI_DATABASE_CONNECTION_STRING", default_connection_string));
    private static readonly Lazy<string> test_connection_string = new Lazy<string>(() => EnvironmentVariables.Required("HATTORI_TEST_DATABASE_CONNECTION_STRING", default_test_connection_string));

    /// <summary>
    /// The connection string of the database the running app serves.
    /// </summary>
    public static string DatabaseConnectionString => production_connection_string.Value;

    /// <summary>
    /// The connection string of the database the tests run against.
    /// </summary>
    public static string TestDatabaseConnectionString => test_connection_string.Value;

    /// <summary>
    /// Reads the connection string of one of the two databases.
    /// </summary>
    /// <param name="target">Which database the connection is for.</param>
    /// <returns>The connection string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The target is not one of the two known databases.</exception>
    public static string GetConnectionString(DatabaseConnectionTarget target) => target switch
    {
        DatabaseConnectionTarget.Production => production_connection_string.Value,
        DatabaseConnectionTarget.Test => test_connection_string.Value,
        _ => throw new ArgumentOutOfRangeException(nameof(target), target, "Unsupported database connection target.")
    };
}
