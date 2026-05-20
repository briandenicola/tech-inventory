using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Reports;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;
using TechInventory.Domain.ValueObjects;
using TechInventory.IntegrationTests.Support;
using TechInventory.Infrastructure.Persistence;

namespace TechInventory.IntegrationTests.Repositories;

public sealed class ReportingRepositoryIntegrationTests(IntegrationTestFactory<ReportingRepositoryIntegrationTests> factory)
    : IClassFixture<IntegrationTestFactory<ReportingRepositoryIntegrationTests>>
{
    private readonly RepositoryIntegrationTestHost<ReportingRepositoryIntegrationTests> _host = new(factory);

    [Fact]
    public async Task ReportingRepository_GetEraReportAsync_GroupsPurchasesIntoSortedDecades()
    {
        await using var dbContext = await _host.CreateDbContextAsync();
        await ResetDatabaseAsync(dbContext);
        var repository = _host.CreateRepository<IReportingRepository>(dbContext, "ReportingRepository");
        var references = await SeedDeviceReferencesAsync(dbContext);

        dbContext.Devices.AddRange(
            CreateReportDevice(references, "Future Console", DeviceStatus.Active, new DateOnly(2032, 7, 1), 1500m),
            CreateReportDevice(references, "Phone 2026", DeviceStatus.Lent, new DateOnly(2026, 5, 1), 900m),
            CreateReportDevice(references, "Laptop 2024", DeviceStatus.Active, new DateOnly(2024, 3, 1), 1200m),
            CreateReportDevice(references, "Watch 2021", DeviceStatus.InRepair, new DateOnly(2021, 11, 1), 100m),
            CreateReportDevice(references, "Tablet 2020", DeviceStatus.Active, new DateOnly(2020, 1, 1), 400m),
            CreateReportDevice(references, "Vintage Stereo", DeviceStatus.Active, new DateOnly(1979, 6, 1), 300m),
            CreateReportDevice(references, "No Purchase Date", DeviceStatus.Active, null, 250m),
            CreateReportDevice(references, "Retired 2022", DeviceStatus.Retired, new DateOnly(2022, 4, 1), 999m));
        await dbContext.SaveChangesAsync();

        var report = await repository.GetEraReportAsync(null, CancellationToken.None);

        report.Should().BeEquivalentTo(
            [
                new EraReportDecade("2030s", 2030, 2039, 1, 1500m, ["Future Console"]),
                new EraReportDecade("2020s", 2020, 2029, 4, 2600m, ["Phone 2026", "Laptop 2024", "Watch 2021"]),
                new EraReportDecade("1970s", 1970, 1979, 1, 300m, ["Vintage Stereo"])
            ],
            options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task ReportingRepository_GetEraReportAsync_WhenCategoryFiltered_ReturnsMatchingDevicesOnly()
    {
        await using var dbContext = await _host.CreateDbContextAsync();
        await ResetDatabaseAsync(dbContext);
        var repository = _host.CreateRepository<IReportingRepository>(dbContext, "ReportingRepository");
        var references = await SeedDeviceReferencesAsync(dbContext);
        var secondCategory = new Category(Guid.NewGuid(), $"Category-{Guid.NewGuid():N}");
        dbContext.Categories.Add(secondCategory);
        await dbContext.SaveChangesAsync();

        dbContext.Devices.AddRange(
            CreateReportDevice(references, "Included Laptop", DeviceStatus.Active, new DateOnly(2024, 3, 1), 1000m),
            CreateReportDevice(references, "Included Tablet", DeviceStatus.Active, new DateOnly(2016, 8, 1), 500m),
            CreateReportDevice(references with { Category = secondCategory }, "Excluded Phone", DeviceStatus.Active, new DateOnly(2025, 1, 1), 700m));
        await dbContext.SaveChangesAsync();

        var report = await repository.GetEraReportAsync(references.Category.Id, CancellationToken.None);

        report.Should().BeEquivalentTo(
            [
                new EraReportDecade("2020s", 2020, 2029, 1, 1000m, ["Included Laptop"]),
                new EraReportDecade("2010s", 2010, 2019, 1, 500m, ["Included Tablet"])
            ],
            options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task ReportingRepository_GetTimelineReportAsync_FiltersAndSortsHistoricalEntries()
    {
        await using var dbContext = await _host.CreateDbContextAsync();
        await ResetDatabaseAsync(dbContext);
        var repository = _host.CreateRepository<IReportingRepository>(dbContext, "ReportingRepository");
        var references = await SeedDeviceReferencesAsync(dbContext);
        var secondCategory = new Category(Guid.NewGuid(), $"Category-{Guid.NewGuid():N}");
        var secondOwner = new Owner(Guid.NewGuid(), "Casey", OwnerRole.Member);
        dbContext.AddRange(secondCategory, secondOwner);
        await dbContext.SaveChangesAsync();

        dbContext.Devices.AddRange(
            CreateReportDevice(references, "Too Early", DeviceStatus.Active, new DateOnly(2017, 1, 1), 200m),
            CreateReportDevice(references, "Included Retired", DeviceStatus.Retired, new DateOnly(2020, 2, 1), 500m),
            CreateReportDevice(references with { Owner = secondOwner }, "Included Disposed", DeviceStatus.Disposed, new DateOnly(2021, 4, 1), 700m),
            CreateReportDevice(references with { Category = secondCategory }, "Wrong Category", DeviceStatus.Active, new DateOnly(2021, 6, 1), 800m),
            CreateReportDevice(references, "Too Late", DeviceStatus.Active, new DateOnly(2023, 1, 1), 900m),
            CreateReportDevice(references, "Missing Purchase Date", DeviceStatus.Active, null, 100m));
        await dbContext.SaveChangesAsync();

        var report = await repository.GetTimelineReportAsync(
            references.Category.Id,
            TimelineGroupBy.Owner,
            new DateOnly(2020, 1, 1),
            new DateOnly(2021, 12, 31),
            CancellationToken.None);

        report.Should().BeEquivalentTo(
            [
                new TimelineReportEntry("Included Retired", references.Brand.Name, new DateOnly(2020, 2, 1), new DateOnly(2024, 12, 31), references.Owner.DisplayName, 500m),
                new TimelineReportEntry("Included Disposed", references.Brand.Name, new DateOnly(2021, 4, 1), new DateOnly(2024, 12, 31), secondOwner.DisplayName, 700m)
            ],
            options => options.WithStrictOrdering());
    }

    private static async Task ResetDatabaseAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await dbContext.Database.EnsureDeletedAsync(cancellationToken);
        await dbContext.Database.MigrateAsync(cancellationToken);
    }

    private static async Task<DeviceReferenceData> SeedDeviceReferencesAsync(AppDbContext dbContext)
    {
        var household = new Household(Guid.NewGuid(), $"Household-{Guid.NewGuid():N}", Currency.From("USD"));
        var brand = new Brand(Guid.NewGuid(), $"Brand-{Guid.NewGuid():N}");
        var category = new Category(Guid.NewGuid(), $"Category-{Guid.NewGuid():N}");
        var owner = new Owner(Guid.NewGuid(), $"Owner-{Guid.NewGuid():N}", OwnerRole.Member);
        var location = new Location(Guid.NewGuid(), $"Location-{Guid.NewGuid():N}", LocationType.Home);
        var network = new Network(Guid.NewGuid(), $"Network-{Guid.NewGuid():N}", "Primary Wi-Fi");

        dbContext.AddRange(household, brand, category, owner, location, network);
        await dbContext.SaveChangesAsync();

        return new DeviceReferenceData(household, brand, category, owner, location, network);
    }

    private static Device CreateReportDevice(
        DeviceReferenceData references,
        string name,
        DeviceStatus status,
        DateOnly? purchaseDate,
        decimal? purchasePrice)
    {
        var retiredDate = status is DeviceStatus.Retired or DeviceStatus.Disposed
            ? new DateOnly(2024, 12, 31)
            : (DateOnly?)null;
        var disposalMethod = status == DeviceStatus.Disposed ? "Recycle" : null;

        return Device.Create(
            Guid.NewGuid(),
            references.Household,
            name,
            references.Brand.Id,
            references.Category.Id,
            references.Owner.Id,
            references.Location.Id,
            model: "Report Model",
            serialNumber: $"SN-{Guid.NewGuid():N}"[..12],
            networkId: references.Network.Id,
            purchaseDate: purchaseDate,
            purchasePrice: purchasePrice,
            currency: Currency.From("USD"),
            status: status,
            notes: "reporting",
            retiredDate: retiredDate,
            disposalMethod: disposalMethod);
    }

    private sealed record DeviceReferenceData(
        Household Household,
        Brand Brand,
        Category Category,
        Owner Owner,
        Location Location,
        Network Network);
}
