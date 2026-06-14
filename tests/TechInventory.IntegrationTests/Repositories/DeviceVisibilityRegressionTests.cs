using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Paging;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;
using TechInventory.Domain.ValueObjects;
using TechInventory.Infrastructure.Persistence;
using TechInventory.IntegrationTests.Support;

namespace TechInventory.IntegrationTests.Repositories;

/// <summary>
/// Regression tests for device visibility bug:
/// Context: "/devices defaults to Active only. Expected household scale is 500-1000 devices.
/// Grouped view must not silently truncate at 200. Retired/sold/disposed are explicit filters/secondary views."
///
/// Addresses:
/// 1. Backend integration/repository tests: default list returns Active only; explicit all-status request includes Disposed/Retired; explicit status filter returns that status.
/// 2. Brand filter + status filter combined scenarios
/// 3. Large dataset pagination scenarios (>200 devices)
/// </summary>
public class DeviceVisibilityRegressionTests(IntegrationTestFactory<DeviceVisibilityRegressionTests> factory)
    : IClassFixture<IntegrationTestFactory<DeviceVisibilityRegressionTests>>
{
    private readonly RepositoryIntegrationTestHost<DeviceVisibilityRegressionTests> _host = new(factory);

    [Fact]
    public async Task DefaultList_ExcludesDisposedDevices_ButIncludesActiveRetiredInRepairLent()
    {
        // Arrange
        await using var dbContext = await CreateCleanDbContextAsync();
        var repository = _host.CreateRepository<IDeviceRepository>(dbContext, "DeviceRepository");
        var refs = await SeedDeviceReferencesAsync(dbContext);

        var activeDevice = CreateDevice(refs, $"Active-{Guid.NewGuid():N}");
        var retiredDevice = CreateDevice(refs, $"Retired-{Guid.NewGuid():N}");
        retiredDevice.ChangeStatus(DeviceStatus.Retired, new DateOnly(2025, 1, 1), null, "apone");
        var inRepairDevice = CreateDevice(refs, $"InRepair-{Guid.NewGuid():N}");
        inRepairDevice.ChangeStatus(DeviceStatus.InRepair, null, null, "apone");
        var lentDevice = CreateDevice(refs, $"Lent-{Guid.NewGuid():N}");
        lentDevice.ChangeStatus(DeviceStatus.Lent, null, null, "apone");
        var disposedDevice = CreateDevice(refs, $"Disposed-{Guid.NewGuid():N}");
        disposedDevice.ChangeStatus(DeviceStatus.Disposed, new DateOnly(2025, 2, 1), "Recycled", "apone");

        await repository.AddAsync(activeDevice, CancellationToken.None);
        await repository.AddAsync(retiredDevice, CancellationToken.None);
        await repository.AddAsync(inRepairDevice, CancellationToken.None);
        await repository.AddAsync(lentDevice, CancellationToken.None);
        await repository.AddAsync(disposedDevice, CancellationToken.None);
        await dbContext.SaveChangesAsync();

        // Act: List without status filter (default behavior)
        var defaultList = await repository.ListAsync(new DeviceListCriteria(new PageRequest()), CancellationToken.None);

        // Assert: Should include Active only (new default behavior)
        defaultList.TotalCount.Should().Be(1);
        defaultList.Items.Should().Contain(d => d.Id == activeDevice.Id);
        defaultList.Items.Should().NotContain(d => d.Id == retiredDevice.Id);
        defaultList.Items.Should().NotContain(d => d.Id == inRepairDevice.Id);
        defaultList.Items.Should().NotContain(d => d.Id == lentDevice.Id);
        defaultList.Items.Should().NotContain(d => d.Id == disposedDevice.Id);

        var allStatusList = await repository.ListAsync(
            new DeviceListCriteria(new PageRequest(page: 1, pageSize: 10), includeAllStatuses: true),
            CancellationToken.None);

        allStatusList.TotalCount.Should().Be(5);
        allStatusList.Items.Select(device => device.Id).Should().Contain(
            [activeDevice.Id, retiredDevice.Id, inRepairDevice.Id, lentDevice.Id, disposedDevice.Id]);
    }

    [Fact]
    public async Task ExplicitStatusFilter_ReturnsOnlyMatchingStatus()
    {
        // Arrange
        await using var dbContext = await CreateCleanDbContextAsync();
        var repository = _host.CreateRepository<IDeviceRepository>(dbContext, "DeviceRepository");
        var refs = await SeedDeviceReferencesAsync(dbContext);

        var activeDevice = CreateDevice(refs, $"Active-{Guid.NewGuid():N}");
        var retiredDevice = CreateDevice(refs, $"Retired-{Guid.NewGuid():N}");
        retiredDevice.ChangeStatus(DeviceStatus.Retired, new DateOnly(2025, 1, 1), null, "apone");
        var disposedDevice = CreateDevice(refs, $"Disposed-{Guid.NewGuid():N}");
        disposedDevice.ChangeStatus(DeviceStatus.Disposed, new DateOnly(2025, 2, 1), "Recycled", "apone");

        await repository.AddAsync(activeDevice, CancellationToken.None);
        await repository.AddAsync(retiredDevice, CancellationToken.None);
        await repository.AddAsync(disposedDevice, CancellationToken.None);
        await dbContext.SaveChangesAsync();

        // Act & Assert: Each status filter returns only matching devices
        var activeList = await repository.ListAsync(
            new DeviceListCriteria(new PageRequest(), status: DeviceStatus.Active), CancellationToken.None);
        activeList.TotalCount.Should().Be(1);
        activeList.Items.Should().ContainSingle(d => d.Id == activeDevice.Id);

        var retiredList = await repository.ListAsync(
            new DeviceListCriteria(new PageRequest(), status: DeviceStatus.Retired), CancellationToken.None);
        retiredList.TotalCount.Should().Be(1);
        retiredList.Items.Should().ContainSingle(d => d.Id == retiredDevice.Id);

        var disposedList = await repository.ListAsync(
            new DeviceListCriteria(new PageRequest(), status: DeviceStatus.Disposed), CancellationToken.None);
        disposedList.TotalCount.Should().Be(1);
        disposedList.Items.Should().ContainSingle(d => d.Id == disposedDevice.Id);
    }

    [Fact]
    public async Task BrandFilter_ReturnsOnlyMatchingBrand()
    {
        // Arrange
        await using var dbContext = await CreateCleanDbContextAsync();
        var repository = _host.CreateRepository<IDeviceRepository>(dbContext, "DeviceRepository");
        var household = new Household(Guid.NewGuid(), $"TestHH-BrandFilter-{Guid.NewGuid():N}", Currency.From("USD"));
        var brandDell = new Brand(Guid.NewGuid(), $"Dell-{Guid.NewGuid():N}");
        var brandHP = new Brand(Guid.NewGuid(), $"HP-{Guid.NewGuid():N}");
        var category = new Category(Guid.NewGuid(), $"Laptops-{Guid.NewGuid():N}");
        var owner = new Owner(Guid.NewGuid(), $"TestOwner-{Guid.NewGuid():N}");
        var location = new Location(Guid.NewGuid(), $"Home-{Guid.NewGuid():N}", LocationType.Home);

        dbContext.AddRange(household, brandDell, brandHP, category, owner, location);
        await dbContext.SaveChangesAsync();

        var dellDevice1 = Device.Create(
            Guid.NewGuid(), household, "Dell XPS 13", brandDell.Id, category.Id, owner.Id, location.Id,
            model: "XPS 13", serialNumber: "D1", networkId: null, purchaseDate: new DateOnly(2024, 1, 1),
            purchasePrice: 1200m, currency: Currency.From("USD"));
        var dellDevice2 = Device.Create(
            Guid.NewGuid(), household, "Dell XPS 15", brandDell.Id, category.Id, owner.Id, location.Id,
            model: "XPS 15", serialNumber: "D2", networkId: null, purchaseDate: new DateOnly(2024, 2, 1),
            purchasePrice: 1500m, currency: Currency.From("USD"));
        var hpDevice = Device.Create(
            Guid.NewGuid(), household, "HP Pavilion", brandHP.Id, category.Id, owner.Id, location.Id,
            model: "Pavilion", serialNumber: "H1", networkId: null, purchaseDate: new DateOnly(2024, 3, 1),
            purchasePrice: 800m, currency: Currency.From("USD"));

        await repository.AddAsync(dellDevice1, CancellationToken.None);
        await repository.AddAsync(dellDevice2, CancellationToken.None);
        await repository.AddAsync(hpDevice, CancellationToken.None);
        await dbContext.SaveChangesAsync();

        // Act: Filter by Dell brand
        var dellList = await repository.ListAsync(
            new DeviceListCriteria(new PageRequest(), brandId: brandDell.Id), CancellationToken.None);

        // Assert: Only Dell devices returned
        dellList.TotalCount.Should().Be(2);
        dellList.Items.Should().HaveCount(2);
        dellList.Items.Should().OnlyContain(d => d.BrandId == brandDell.Id);
        dellList.Items.Should().Contain(d => d.Id == dellDevice1.Id);
        dellList.Items.Should().Contain(d => d.Id == dellDevice2.Id);
        dellList.Items.Should().NotContain(d => d.Id == hpDevice.Id);
    }

    [Fact]
    public async Task BrandAndStatusFilter_Combined_ReturnsOnlyMatchingBoth()
    {
        // Arrange
        await using var dbContext = await CreateCleanDbContextAsync();
        var repository = _host.CreateRepository<IDeviceRepository>(dbContext, "DeviceRepository");
        var household = new Household(Guid.NewGuid(), $"TestHH-BrandStatus-{Guid.NewGuid():N}", Currency.From("USD"));
        var brandDell = new Brand(Guid.NewGuid(), $"Dell-{Guid.NewGuid():N}");
        var category = new Category(Guid.NewGuid(), $"Monitors-{Guid.NewGuid():N}");
        var owner = new Owner(Guid.NewGuid(), $"TestOwner-{Guid.NewGuid():N}");
        var location = new Location(Guid.NewGuid(), $"Office-{Guid.NewGuid():N}", LocationType.External);

        dbContext.AddRange(household, brandDell, category, owner, location);
        await dbContext.SaveChangesAsync();

        var dellActiveDevice = Device.Create(
            Guid.NewGuid(), household, "Dell Monitor Active", brandDell.Id, category.Id, owner.Id, location.Id,
            model: "U2720Q", serialNumber: "DA1", networkId: null, purchaseDate: new DateOnly(2024, 1, 1),
            purchasePrice: 600m, currency: Currency.From("USD"));
        var dellRetiredDevice = Device.Create(
            Guid.NewGuid(), household, "Dell Monitor Retired", brandDell.Id, category.Id, owner.Id, location.Id,
            model: "P2419H", serialNumber: "DR1", networkId: null, purchaseDate: new DateOnly(2020, 1, 1),
            purchasePrice: 200m, currency: Currency.From("USD"));
        dellRetiredDevice.ChangeStatus(DeviceStatus.Retired, new DateOnly(2023, 12, 31), null, "apone");
        var dellDisposedDevice = Device.Create(
            Guid.NewGuid(), household, "Dell Monitor Disposed", brandDell.Id, category.Id, owner.Id, location.Id,
            model: "E2014H", serialNumber: "DD1", networkId: null, purchaseDate: new DateOnly(2018, 1, 1),
            purchasePrice: 150m, currency: Currency.From("USD"));
        dellDisposedDevice.ChangeStatus(DeviceStatus.Disposed, new DateOnly(2024, 1, 1), "Recycled", "apone");

        await repository.AddAsync(dellActiveDevice, CancellationToken.None);
        await repository.AddAsync(dellRetiredDevice, CancellationToken.None);
        await repository.AddAsync(dellDisposedDevice, CancellationToken.None);
        await dbContext.SaveChangesAsync();

        // Act: Filter by brand=Dell and status=Active
        var dellActiveList = await repository.ListAsync(
            new DeviceListCriteria(new PageRequest(), brandId: brandDell.Id, status: DeviceStatus.Active),
            CancellationToken.None);

        // Assert: Only Dell Active device returned
        dellActiveList.TotalCount.Should().Be(1);
        dellActiveList.Items.Should().ContainSingle(d => d.Id == dellActiveDevice.Id && d.BrandId == brandDell.Id && d.Status == DeviceStatus.Active);

        // Act: Filter by brand=Dell and status=Retired
        var dellRetiredList = await repository.ListAsync(
            new DeviceListCriteria(new PageRequest(), brandId: brandDell.Id, status: DeviceStatus.Retired),
            CancellationToken.None);

        // Assert: Only Dell Retired device returned
        dellRetiredList.TotalCount.Should().Be(1);
        dellRetiredList.Items.Should().ContainSingle(d => d.Id == dellRetiredDevice.Id && d.BrandId == brandDell.Id && d.Status == DeviceStatus.Retired);

        // Act: Filter by brand=Dell and status=Disposed
        var dellDisposedList = await repository.ListAsync(
            new DeviceListCriteria(new PageRequest(), brandId: brandDell.Id, status: DeviceStatus.Disposed),
            CancellationToken.None);

        // Assert: Only Dell Disposed device returned
        dellDisposedList.TotalCount.Should().Be(1);
        dellDisposedList.Items.Should().ContainSingle(d => d.Id == dellDisposedDevice.Id && d.BrandId == brandDell.Id && d.Status == DeviceStatus.Disposed);
    }

    [Fact]
    public async Task LargeDataset_DefaultPageSize25_ReturnsCorrectTotalCount()
    {
        // Arrange: Seed 50 active devices
        await using var dbContext = await CreateCleanDbContextAsync();
        var repository = _host.CreateRepository<IDeviceRepository>(dbContext, "DeviceRepository");
        var refs = await SeedDeviceReferencesAsync(dbContext);

        for (int i = 1; i <= 50; i++)
        {
            var device = CreateDevice(refs, $"Device {i:D3}");
            await repository.AddAsync(device, CancellationToken.None);
        }
        await dbContext.SaveChangesAsync();

        // Act: Get first page with default page size (25)
        var firstPage = await repository.ListAsync(
            new DeviceListCriteria(new PageRequest(page: 1, pageSize: 25)), CancellationToken.None);

        // Assert: Total count should be 50, items returned should be 25
        firstPage.TotalCount.Should().Be(50);
        firstPage.Items.Should().HaveCount(25);
        firstPage.Page.Should().Be(1);
        firstPage.PageSize.Should().Be(25);

        // Act: Get second page
        var secondPage = await repository.ListAsync(
            new DeviceListCriteria(new PageRequest(page: 2, pageSize: 25)), CancellationToken.None);

        // Assert: Second page should also have 25 items
        secondPage.TotalCount.Should().Be(50);
        secondPage.Items.Should().HaveCount(25);
        secondPage.Items.Should().NotIntersectWith(firstPage.Items);
    }

    [Fact]
    public async Task LargeDataset_PageSize200_ReturnsAllItemsInSinglePage()
    {
        // Arrange: Seed 150 active devices (below 200 threshold)
        await using var dbContext = await CreateCleanDbContextAsync();
        var repository = _host.CreateRepository<IDeviceRepository>(dbContext, "DeviceRepository");
        var refs = await SeedDeviceReferencesAsync(dbContext);

        for (int i = 1; i <= 150; i++)
        {
            var device = CreateDevice(refs, $"Device {i:D3}");
            await repository.AddAsync(device, CancellationToken.None);
        }
        await dbContext.SaveChangesAsync();

        // Act: Request with pageSize=200 (typical grouped view scenario)
        var result = await repository.ListAsync(
            new DeviceListCriteria(new PageRequest(page: 1, pageSize: 200)), CancellationToken.None);

        // Assert: All 150 devices should be returned in single page
        result.TotalCount.Should().Be(150);
        result.Items.Should().HaveCount(150);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(200);
    }

    [Fact]
    public async Task LargeDataset_Over200Devices_RequiresMultiplePages()
    {
        // Arrange: Seed 250 active devices (over 200 threshold)
        await using var dbContext = await CreateCleanDbContextAsync();
        var repository = _host.CreateRepository<IDeviceRepository>(dbContext, "DeviceRepository");
        var refs = await SeedDeviceReferencesAsync(dbContext);

        for (int i = 1; i <= 250; i++)
        {
            var device = CreateDevice(refs, $"Device {i:D3}");
            await repository.AddAsync(device, CancellationToken.None);
        }
        await dbContext.SaveChangesAsync();

        // Act: Request first page with pageSize=200
        var firstPage = await repository.ListAsync(
            new DeviceListCriteria(new PageRequest(page: 1, pageSize: 200)), CancellationToken.None);

        // Assert: First page returns 200 items, total count is 250
        firstPage.TotalCount.Should().Be(250);
        firstPage.Items.Should().HaveCount(200);

        // Act: Request second page
        var secondPage = await repository.ListAsync(
            new DeviceListCriteria(new PageRequest(page: 2, pageSize: 200)), CancellationToken.None);

        // Assert: Second page returns remaining 50 items
        secondPage.TotalCount.Should().Be(250);
        secondPage.Items.Should().HaveCount(50);
        secondPage.Items.Should().NotIntersectWith(firstPage.Items);
    }

    [Fact]
    public async Task BrandFilter_WithLargeDataset_ReturnsCorrectSubset()
    {
        // Arrange: Seed 100 Dell devices and 100 HP devices
        await using var dbContext = await CreateCleanDbContextAsync();
        var repository = _host.CreateRepository<IDeviceRepository>(dbContext, "DeviceRepository");
        var household = new Household(Guid.NewGuid(), $"TestHH-LargeDataset-{Guid.NewGuid():N}", Currency.From("USD"));
        var brandDell = new Brand(Guid.NewGuid(), $"Dell-{Guid.NewGuid():N}");
        var brandHP = new Brand(Guid.NewGuid(), $"HP-{Guid.NewGuid():N}");
        var category = new Category(Guid.NewGuid(), $"Computers-{Guid.NewGuid():N}");
        var owner = new Owner(Guid.NewGuid(), $"TestOwner-{Guid.NewGuid():N}");
        var location = new Location(Guid.NewGuid(), $"HQ-{Guid.NewGuid():N}", LocationType.External);

        dbContext.AddRange(household, brandDell, brandHP, category, owner, location);
        await dbContext.SaveChangesAsync();

        for (int i = 1; i <= 100; i++)
        {
            var dellDevice = Device.Create(
                Guid.NewGuid(), household, $"Dell Device {i:D3}", brandDell.Id, category.Id, owner.Id, location.Id,
                model: "Model D", serialNumber: $"D{i}", networkId: null, purchaseDate: new DateOnly(2024, 1, 1),
                purchasePrice: 1000m, currency: Currency.From("USD"));
            await repository.AddAsync(dellDevice, CancellationToken.None);

            var hpDevice = Device.Create(
                Guid.NewGuid(), household, $"HP Device {i:D3}", brandHP.Id, category.Id, owner.Id, location.Id,
                model: "Model H", serialNumber: $"H{i}", networkId: null, purchaseDate: new DateOnly(2024, 1, 1),
                purchasePrice: 800m, currency: Currency.From("USD"));
            await repository.AddAsync(hpDevice, CancellationToken.None);
        }
        await dbContext.SaveChangesAsync();

        // Act: Filter by Dell brand with pageSize=200
        var dellList = await repository.ListAsync(
            new DeviceListCriteria(new PageRequest(page: 1, pageSize: 200), brandId: brandDell.Id),
            CancellationToken.None);

        // Assert: Only Dell devices returned, count is 100
        dellList.TotalCount.Should().Be(100);
        dellList.Items.Should().HaveCount(100);
        dellList.Items.Should().OnlyContain(d => d.BrandId == brandDell.Id);
    }

    private async Task<AppDbContext> CreateCleanDbContextAsync()
    {
        var dbContext = await _host.CreateDbContextAsync();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();
        return dbContext;
    }

    private static async Task<DeviceReferenceIds> SeedDeviceReferencesAsync(AppDbContext dbContext)
    {
        var household = new Household(Guid.NewGuid(), $"TestHH-{Guid.NewGuid():N}", Currency.From("USD"));
        var brand = new Brand(Guid.NewGuid(), $"TestBrand-{Guid.NewGuid():N}");
        var category = new Category(Guid.NewGuid(), $"TestCategory-{Guid.NewGuid():N}");
        var owner = new Owner(Guid.NewGuid(), $"TestOwner-{Guid.NewGuid():N}");
        var location = new Location(Guid.NewGuid(), $"TestLocation-{Guid.NewGuid():N}", LocationType.Home);
        var network = new Network(Guid.NewGuid(), $"TestNetwork-{Guid.NewGuid():N}");

        dbContext.AddRange(household, brand, category, owner, location, network);
        await dbContext.SaveChangesAsync();

        return new DeviceReferenceIds(household.Id, brand.Id, category.Id, owner.Id, location.Id, network.Id);
    }

    private static Device CreateDevice(DeviceReferenceIds refs, string name)
    {
        return Device.Create(
            Guid.NewGuid(),
            new Household(refs.HouseholdId, "Household", Currency.From("USD")),
            name,
            refs.BrandId,
            refs.CategoryId,
            refs.OwnerId,
            refs.LocationId,
            model: "Test Model",
            serialNumber: $"SN-{Guid.NewGuid():N}",
            networkId: refs.NetworkId,
            purchaseDate: new DateOnly(2024, 1, 1),
            purchasePrice: 100m,
            currency: Currency.From("USD"));
    }

    private sealed record DeviceReferenceIds(Guid HouseholdId, Guid BrandId, Guid CategoryId, Guid OwnerId, Guid LocationId, Guid NetworkId);
}
