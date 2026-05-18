using System.Diagnostics;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using TechInventory.Application;
using TechInventory.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/techinventory-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();

// Problem Details for RFC 7807 error responses
builder.Services.AddProblemDetails();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// OpenAPI / Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Healthchecks
builder.Services.AddHealthChecks();

// OpenTelemetry (optional via configuration)
var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"];
if (!string.IsNullOrEmpty(otlpEndpoint))
{
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(res => res.AddService("TechInventory.Api"))
        .WithTracing(tracing =>
        {
            tracing
                .AddAspNetCoreInstrumentation()
                .AddOtlpExporter(opts => opts.Endpoint = new Uri(otlpEndpoint));
        });
}

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseExceptionHandler();
app.UseStatusCodePages();

// Swagger in Development and optionally in Production
if (app.Environment.IsDevelopment() ||
    bool.TryParse(app.Configuration["Features:SwaggerInProduction"], out var enableSwagger) && enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

// Healthchecks
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");

app.MapControllers();

try
{
    Log.Information("Starting Tech Inventory API");
    app.Run();
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
