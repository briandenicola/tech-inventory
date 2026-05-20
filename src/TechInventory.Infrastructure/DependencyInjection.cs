using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        // F025 — local-account fallback wiring. Options bind even when the
        // feature is disabled so test configs can still touch the values.
        services.Configure<LocalJwtOptions>(configuration.GetSection(LocalJwtOptions.SectionPath));
        services.Configure<Argon2idOptions>(configuration.GetSection(Argon2idOptions.SectionPath));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IPasswordHasher, Argon2idPasswordHasher>();
        services.AddSingleton<ILocalTokenIssuer, HmacJwtLocalTokenIssuer>();

        return services;
    }
}
