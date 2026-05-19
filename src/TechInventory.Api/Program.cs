using System.Text;
using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using TechInventory.Api.Authentication;
using TechInventory.Api.ExceptionHandling;
using TechInventory.Api.OpenApi;
using TechInventory.Application;
using TechInventory.Application.Abstractions.Services;
using TechInventory.Domain.Entities;
using TechInventory.Domain.ValueObjects;
using TechInventory.Infrastructure;
using TechInventory.Infrastructure.Persistence;
using TechInventory.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/techinventory-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddHttpContextAccessor();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<ICurrentUserService, HttpContextCurrentUserService>();
builder.Services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// Auth registration. Two real bearer schemes — Entra (cloud SSO) and
// F025 Local HS256 (break-glass username/password). If Entra is configured,
// the default scheme is a policy scheme that sniffs the JWT `iss` claim and
// forwards to whichever handler issued the token. If Entra is not configured
// (pure-local-dev or air-gapped deployments), the Local handler IS the
// default and Entra is never registered.
//
// There is no longer any DevBypass / fake-admin shim in the production
// binary. Integration tests install a TestAuthHandler via ConfigureTestServices.
var entraAuthority = builder.Configuration["Auth:Entra:Authority"];
var entraAudiences = builder.Configuration.GetSection("Auth:Entra:Audiences").Get<string[]>();
var entraTenantId = builder.Configuration["Auth:Entra:TenantId"];
var entraConfigured = !string.IsNullOrWhiteSpace(entraAuthority) && entraAudiences is { Length: > 0 };

var authenticationBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = ApiAuthenticationSchemes.DefaultScheme;
    options.DefaultChallengeScheme = ApiAuthenticationSchemes.DefaultScheme;
});

if (entraConfigured)
{
    // Composite policy scheme. We sniff the JWT `iss` claim and forward to
    // either the Entra JwtBearer handler or our local HS256 handler. Both
    // produce a ClaimsPrincipal with the same `role` shape, so existing
    // `[Authorize(Roles=...)]` attributes Just Work regardless of issuer.
    authenticationBuilder.AddPolicyScheme(ApiAuthenticationSchemes.DefaultScheme, ApiAuthenticationSchemes.DefaultScheme, options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            var header = context.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrEmpty(header) && header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = header["Bearer ".Length..].Trim();
                try
                {
                    var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(token);
                    if (string.Equals(jwt.Issuer, LocalJwtOptions.Issuer, StringComparison.Ordinal))
                    {
                        return ApiAuthenticationSchemes.LocalScheme;
                    }
                }
                catch (Exception)
                {
                    // Fall through to Entra so the JwtBearer handler can emit the
                    // proper 401 ProblemDetails.
                }
            }
            return ApiAuthenticationSchemes.EntraScheme;
        };
    });

    authenticationBuilder.AddJwtBearer(ApiAuthenticationSchemes.EntraScheme, options =>
    {
        options.Authority = entraAuthority;

        // Accept BOTH the v1 (`sts.windows.net/<tenant>/`) and v2
        // (`login.microsoftonline.com/<tenant>/v2.0`) issuer formats from the
        // same tenant. Which form Entra emits depends on the app
        // registration's `accessTokenAcceptedVersion` manifest setting
        // (null/1 → v1, 2 → v2). MSAL.js requests v2 by default but the
        // resulting access token still uses the v1 issuer if the manifest
        // hasn't been flipped. Rather than silently 401, accept either —
        // the audience + signing-key + lifetime checks remain authoritative.
        var resolvedTenantId = entraTenantId;
        if (string.IsNullOrWhiteSpace(resolvedTenantId))
        {
            // Fall back to parsing it from the Authority URL: the path
            // segment immediately after the host is the tenant GUID.
            if (Uri.TryCreate(entraAuthority, UriKind.Absolute, out var authorityUri))
            {
                var segments = authorityUri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length > 0)
                {
                    resolvedTenantId = segments[0];
                }
            }
        }

        string[]? validIssuers = null;
        if (!string.IsNullOrWhiteSpace(resolvedTenantId))
        {
            validIssuers = new[]
            {
                $"https://login.microsoftonline.com/{resolvedTenantId}/v2.0",
                $"https://sts.windows.net/{resolvedTenantId}/"
            };
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidAudiences = entraAudiences,
            ValidIssuers = validIssuers,
            ClockSkew = TimeSpan.FromMinutes(2)
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var principal = context.Principal;
                if (principal == null)
                {
                    context.Fail("No principal in JWT");
                    return Task.CompletedTask;
                }

                // Entra emits `roles` as a JSON array. JwtSecurityTokenHandler
                // expands that into one Claim per array element (each with
                // Value being the bare string, NOT a JSON-encoded array), and
                // by default `MapInboundClaims = true` also re-types them as
                // ClaimTypes.Role. Accept both shapes so [Authorize(Roles=...)]
                // works regardless of mapping configuration.
                var roleClaims = principal.FindAll(System.Security.Claims.ClaimTypes.Role)
                    .Concat(principal.FindAll("roles"))
                    .ToList();

                if (roleClaims.Count == 0)
                {
                    context.Fail("JWT missing 'roles' claim");
                    return Task.CompletedTask;
                }

                // Make sure every role value is present under ClaimTypes.Role,
                // so downstream policy/role checks find them even if the
                // inbound map didn't run for some reason.
                var identity = (System.Security.Claims.ClaimsIdentity)principal.Identity!;
                foreach (var rolesClaim in principal.FindAll("roles"))
                {
                    if (!identity.HasClaim(System.Security.Claims.ClaimTypes.Role, rolesClaim.Value))
                    {
                        identity.AddClaim(new System.Security.Claims.Claim(
                            System.Security.Claims.ClaimTypes.Role, rolesClaim.Value));
                    }
                }

                return Task.CompletedTask;
            }
        };
    });
}
else
{
    // Pure-local mode: no Entra config present. Forward the default scheme
    // straight to the Local HS256 handler so the rest of the pipeline
    // (controllers, fallback policy, audit logging) is unaffected.
    authenticationBuilder.AddPolicyScheme(ApiAuthenticationSchemes.DefaultScheme, ApiAuthenticationSchemes.DefaultScheme, options =>
    {
        options.ForwardDefaultSelector = _ => ApiAuthenticationSchemes.LocalScheme;
    });
}

// F025 — local HS256 bearer. Always registered (it is the only auth source
// when Entra is absent, and the break-glass admin when Entra is present).
// We register the JwtBearer scheme up front and wire its
// TokenValidationParameters via a PostConfigure that pulls from
// IOptions<LocalJwtOptions>. This is critical for WebApplicationFactory test
// hosts: their ConfigureAppConfiguration callbacks land AFTER Program.cs
// reads builder.Configuration, so any direct .Get<LocalJwtOptions>() here
// would miss the test's signing-key override.
authenticationBuilder.AddJwtBearer(ApiAuthenticationSchemes.LocalScheme, _ => { });
builder.Services.AddOptions<JwtBearerOptions>(ApiAuthenticationSchemes.LocalScheme)
    .Configure<IOptions<LocalJwtOptions>>((options, localOptions) =>
    {
        var localJwt = localOptions.Value;
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();

        SymmetricSecurityKey? signingKey = null;
        if (!string.IsNullOrWhiteSpace(localJwt.SigningKey))
        {
            var keyBytes = Encoding.UTF8.GetBytes(localJwt.SigningKey);
            if (keyBytes.Length < 32)
            {
                throw new InvalidOperationException("Auth:Local:SigningKey must be at least 32 bytes (256 bits).");
            }
            signingKey = new SymmetricSecurityKey(keyBytes);
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = LocalJwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = localJwt.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = signingKey is not null,
            IssuerSigningKey = signingKey,
            IssuerSigningKeys = signingKey is not null ? new[] { signingKey } : null,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build())
    .AddPolicy(AuthorizationPolicies.Admin, policy => policy
        .RequireAuthenticatedUser()
        .RequireRole("Admin"));

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("ApiCorsPolicy", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Tech Inventory API",
        Version = "v1",
    });
});
builder.Services.AddHealthChecks();

// F025 — seed a local Admin from env vars if Auth:Local:SeedEnabled is set.
builder.Services.AddHostedService<LocalAdminSeedHostedService>();

var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"];
if (!string.IsNullOrEmpty(otlpEndpoint))
{
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService("TechInventory.Api"))
        .WithTracing(tracing =>
        {
            tracing
                .AddAspNetCoreInstrumentation()
                .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
        });
}

const string EntraPlusLocalAuthMessage = "🔐 Auth mode: ENTRA JWT BEARER (+ local F025 fallback)";
const string LocalOnlyAuthMessage = "🔐 Auth mode: LOCAL F025 ONLY (Auth:Entra:Authority not set)";

var app = builder.Build();

if (args.Length > 0 && string.Equals(args[0], "export-openapi", StringComparison.OrdinalIgnoreCase))
{
    var outputPath = args.Length > 1
        ? args[1]
        : Path.Combine(app.Environment.ContentRootPath, "..", "..", "openapi.yaml");
    await OpenApiDocumentExporter.ExportAsync(app.Services, outputPath).ConfigureAwait(false);
    return;
}

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment() ||
    bool.TryParse(app.Configuration["Features:SwaggerInProduction"], out var enableSwagger) && enableSwagger)
{
    app.UseSwagger(options => options.RouteTemplate = "openapi/{documentName}.json");
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = "swagger";
        options.SwaggerEndpoint("/openapi/v1.json", "Tech Inventory API v1");
    });
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("ApiCorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

// F025 — must-change-password gate. While a local-auth principal carries
// must_change_password=true, allow only the change-password endpoint (plus
// anonymous routes like swagger/health). Everything else returns 403 so the UI
// can route them through the rotation flow.
app.Use(async (httpContext, next) =>
{
    var user = httpContext.User;
    if (user?.Identity?.IsAuthenticated == true
        && string.Equals(user.FindFirst("auth_method")?.Value, "local", StringComparison.Ordinal)
        && string.Equals(user.FindFirst("must_change_password")?.Value, "true", StringComparison.OrdinalIgnoreCase))
    {
        var path = httpContext.Request.Path.Value ?? string.Empty;
        if (!path.StartsWith("/api/v1/auth/local/change-password", StringComparison.OrdinalIgnoreCase))
        {
            httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
            httpContext.Response.ContentType = "application/problem+json";
            await httpContext.Response.WriteAsJsonAsync(new
            {
                type = "https://tools.ietf.org/html/rfc9110#name-403-forbidden",
                title = "Password change required",
                status = 403,
                code = "PasswordChangeRequired",
                detail = "Your local account must change its password before using the API."
            });
            return;
        }
    }
    await next().ConfigureAwait(false);
});

app.MapHealthChecks("/health").AllowAnonymous();
app.MapHealthChecks("/health/ready").AllowAnonymous();
app.MapControllers();

try
{
    await using (var scope = app.Services.CreateAsyncScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();

        if (!await dbContext.Households.AnyAsync())
        {
            await dbContext.Households.AddAsync(new Household(Guid.NewGuid(), "Primary Household", Currency.From("USD")));
            await dbContext.SaveChangesAsync();
            app.Logger.LogInformation("Seeded the default single-household record.");
        }
    }

    app.Logger.LogInformation(entraConfigured ? EntraPlusLocalAuthMessage : LocalOnlyAuthMessage);

    Log.Information("Starting Tech Inventory API");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
