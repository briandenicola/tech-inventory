using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
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

var devBypassEnabled = builder.Configuration.GetValue<bool>("Auth:DevBypass");
if (devBypassEnabled && !builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException("Auth:DevBypass may only be enabled in Development.");
}

var authenticationBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = ApiAuthenticationSchemes.DefaultScheme;
    options.DefaultChallengeScheme = ApiAuthenticationSchemes.DefaultScheme;
});

if (devBypassEnabled)
{
    authenticationBuilder.AddScheme<AuthenticationSchemeOptions, DevBypassAuthenticationHandler>(
        ApiAuthenticationSchemes.DefaultScheme,
        _ => { });
}
else
{
    var authority = builder.Configuration["Auth:Entra:Authority"]
        ?? throw new InvalidOperationException("Auth:Entra:Authority is required when Auth:DevBypass=false");
    var audiences = builder.Configuration.GetSection("Auth:Entra:Audiences").Get<string[]>()
        ?? throw new InvalidOperationException("Auth:Entra:Audiences is required when Auth:DevBypass=false");

    authenticationBuilder.AddJwtBearer(ApiAuthenticationSchemes.DefaultScheme, options =>
    {
        options.Authority = authority;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidAudiences = audiences,
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

                var rolesClaim = principal.FindFirst("roles");
                if (rolesClaim == null)
                {
                    context.Fail("JWT missing 'roles' claim");
                    return Task.CompletedTask;
                }

                var roles = System.Text.Json.JsonSerializer.Deserialize<string[]>(rolesClaim.Value);
                if (roles == null || roles.Length == 0)
                {
                    context.Fail("JWT 'roles' claim is empty");
                    return Task.CompletedTask;
                }

                var identity = (System.Security.Claims.ClaimsIdentity)principal.Identity!;
                foreach (var role in roles)
                {
                    identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role));
                }

                return Task.CompletedTask;
            }
        };
    });
}

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

const string DevBypassWarningMessage = "⚠️ Auth:DevBypass" + "=true — all requests authenticated as 'dev-admin'. NEVER enable outside Development.";
const string JwtBearerEnabledMessage = "🔐 Auth mode: ENTRA JWT BEARER";

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

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");
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

    if (devBypassEnabled)
    {
        app.Logger.LogWarning(DevBypassWarningMessage);
    }
    else
    {
        app.Logger.LogInformation(JwtBearerEnabledMessage);
    }

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
