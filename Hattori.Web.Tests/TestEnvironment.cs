// This code is part of the Hattori project. Licensed under the MIT License.
// See the LICENSE file for full license text.

using System;
using NUnit.Framework;

namespace Hattori.Web.Tests;

/// <summary>
/// Points both database settings at the test database before any test reads them, so every fixture in this assembly hits port 5433 and never the dev database.
/// </summary>
[SetUpFixture]
public class TestEnvironment
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        Environment.SetEnvironmentVariable("HATTORI_DATABASE_CONNECTION_STRING", "Host=localhost;Database=hattori_test;Username=postgres;Password=mysecretpassword;Port=5433");
        Environment.SetEnvironmentVariable("HATTORI_TEST_DATABASE_CONNECTION_STRING", "Host=localhost;Database=hattori_test;Username=postgres;Password=mysecretpassword;Port=5433");
    }
}
