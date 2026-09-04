using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Abstractions.Services;
using TechInventory.Application.Exports;
using TechInventory.Infrastructure.Persistence;
using TechInventory.Infrastructure.Persistence.Interceptors;
using TechInventory.Infrastructure.Persistence.Repositories;
using TechInventory.Infrastructure.Services;

namespace TechInventory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddScoped<ICurrentUserService, SystemCurrentUserService>();
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            var connectionString = configuration.GetConnectionString("Default") ?? "Data Source=techinventory.db";
            options.UseSqlite(connectionString);
        });
        services.AddScoped<IUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<AppDbContext>());

        services.AddScoped<IBrandRepository, BrandRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<DeviceRepository>();
        services.AddScoped<IDeviceRepository>(serviceProvider => serviceProvider.GetRequiredService<DeviceRepository>());
        services.AddScoped<IDeviceExportService>(serviceProvider => serviceProvider.GetRequiredService<DeviceRepository>());
        services.AddScoped<IReportingRepository, ReportingRepository>();
        services.AddScoped<IHouseholdRepository, HouseholdRepository>();
        services.AddScoped<IHouseholdSettingRepository, HouseholdSettingRepository>();
        services.AddScoped<IOwnerRepository, OwnerRepository>();
        services.AddScoped<ILocationRepository, LocationRepository>();
        services.AddScoped<INetworkRepository, NetworkRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<IAuditEventRepository, AuditEventRepository>();
        services.AddScoped<IImportBatchRepository, ImportBatchRepository>();
        services.AddScoped<ILocalUserRepository, LocalUserRepository>();
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();

        // #149 / ADR 0003 — API key verification. The pepper is a required deployment
        // secret: ValidateOnStart turns a missing, malformed, short, or reused value into
        // a startup failure with a specific message, rather than a 500 on the first
        // authentication attempt. Existing deployments must add ApiKeys:HmacPepper before
        // upgrading — see docs/operations.md, "API Key Administration".
        services.AddOptions<ApiKeyOptions>()
            .Bind(configuration.GetSection(ApiKeyOptions.SectionPath))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<ApiKeyOptions>, ApiKeyOptionsValidator>();
        services.AddSingleton<IApiKeyHasher, HmacApiKeyHasher>();

        // F025 — local-account fallback wiring. Options bind even when the
        // feature is disabled so test configs can still touch the values.
        services.Configure<LocalJwtOptions>(configuration.GetSection(LocalJwtOptions.SectionPath));
        services.Configure<Argon2idOptions>(configuration.GetSection(Argon2idOptions.SectionPath));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IPasswordHasher, Argon2idPasswordHasher>();
        services.AddSingleton<ILocalTokenIssuer, HmacJwtLocalTokenIssuer>();
        services.AddSingleton<ILocalLoginLockoutPolicy, LocalLoginLockoutPolicy>();

        return services;
    }
}
