// This code is part of the Hattori project. Licensed under the MIT License.
// See the LICENSE file for full license text.

using System;
using System.Linq;
using System.Reflection;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Hattori.Core.Authorization;
using Hattori.Core.Configurations;
using Hattori.Core.Entities;
using Hattori.Core.Logger;
using Hattori.Persistence;
using Hattori.Web.Authorization;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using Serilog;

const string service_name = "Hattori.Web";
string appVersion = typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion?.Split('+')[0] ?? "0.0.0";

var builder = WebApplication.CreateBuilder(args);

LoggerSetup.Configure(builder.Environment.IsDevelopment(), service_name);
LoggerSetup.LogStartupHeader(service_name, builder.Environment.EnvironmentName, appVersion);
builder.Host.UseSerilog();

// Read every required setting before anything else, so a missing one stops startup instead of failing the first request that needs it.
_ = DatabaseSettings.DatabaseConnectionString;
_ = FrontendSettings.BaseUrls;

builder.Services.AddProblemDetails();
builder.Services.AddHattoriPersistence(DatabaseConnectionTarget.Production);

builder.Services.AddIdentityApiEndpoints<HattoriUser>()
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<HattoriDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders()
    .AddClaimsPrincipalFactory<HattoriUserClaimsPrincipalFactory>();

// One policy per permission constant; the handler checks the permission claims the cookie carries.
var authBuilder = builder.Services.AddAuthorizationBuilder();

foreach (string permission in Permissions.ALL)
    authBuilder.AddPolicy(permission, policy => policy.AddRequirements(new PermissionRequirement(permission)));

builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

// A cookie for a frontend on another origin: no redirect to a login page, an API answers 401/403.
builder.Services.ConfigureApplicationCookie(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    }
    else
    {
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    }

    options.Cookie.HttpOnly = true;
    options.Cookie.Name = ".Hattori.Auth";

    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

// Stops the cookie policy middleware from downgrading SameSite=None on plain HTTP.
builder.Services.AddCookiePolicy(options => options.MinimumSameSitePolicy = SameSiteMode.None);

const string front_end_cors_policy = "Frontend";

builder.Services.AddCors(options =>
{
    options.AddPolicy(front_end_cors_policy, policy => policy
        .WithOrigins(FrontendSettings.BaseUrls.ToArray())
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

// Without a limit on the sign-in endpoints, passwords can be guessed without limit.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1)
        }));
});

builder.Services.AddOpenApi();
builder.Services.AddControllers();

if (TelemetrySettings.OtlpEndpointSet)
{
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService(service_name, serviceVersion: appVersion))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddOtlpExporter())
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter());
}

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors(front_end_cors_policy);

// Self-registration goes through api/account/register, which also assigns the default user role; the identity API's own register is answered as if it did not exist.
app.Use(async (context, next) =>
{
    if (HttpMethods.IsPost(context.Request.Method) && context.Request.Path == "/account/register")
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    await next();
});

app.UseHttpsRedirection();
app.UseCookiePolicy();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();
app.MapGroup("/account").MapIdentityApi<HattoriUser>().RequireRateLimiting("login");
app.MapHealthChecks("/health").AllowAnonymous();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.Run();

/// <summary>
/// The entry point, named so tests can boot the whole app with WebApplicationFactory.
/// </summary>
internal partial class Program;
