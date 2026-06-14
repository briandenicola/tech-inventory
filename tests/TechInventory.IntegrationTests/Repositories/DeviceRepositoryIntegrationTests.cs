using FluentAssertions;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Paging;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;
using TechInventory.Domain.ValueObjects;
using TechInventory.IntegrationTests.Support;
using TechInventory.Infrastructure.Persistence;

namespace TechInventory.IntegrationTests.Repositories;

public class DeviceRepositoryIntegrationTests(IntegrationTestFactory<DeviceRepositoryIntegrationTests> factory)
    : IClassFixture<IntegrationTestFactory<DeviceRepositoryIntegrationTests>>
{
    private readonly RepositoryIntegrationTestHost<DeviceRepositoryIntegrationTests> _host = new(factory);

    [Fact]
    public async Task DeviceRepository_AddFindUpdateAndSoftDeleteRoundTrip()
    {
        await using var dbContext = await _host.CreateDbContextAsync();
        var repository = _host.CreateRepository<IDeviceRepository>(dbContext, "DeviceRepository");
        var refs = await SeedDeviceReferencesAsync(dbContext);
        var device = CreateDevice(refs, $"Device-{Guid.NewGuid():N}");

        var addResult = await repository.AddAsync(device, CancellationToken.None);
        addResult.IsSuccess.Should().BeTrue();
        await dbContext.SaveChangesAsync();

        var added = await repository.GetByIdAsync(device.Id, CancellationToken.None);
        added.IsSuccess.Should().BeTrue();
        added.Value!.Name.Should().Be(device.Name);

        device.UpdateDetails(
            $"Updated-{Guid.NewGuid():N}",
            refs.BrandId,
            refs.CategoryId,
            refs.OwnerId,
            refs.LocationId,
            Currency.From("EUR"),
            model: "Model 2",
            serialNumber: "SN-200",
            networkId: refs.NetworkId,
            purchaseDate: new DateOnly(2024, 6, 1),
            purchasePrice: 456.78m,
            modifiedBy: "apone");

        var updateResult = await repository.UpdateAsync(device, CancellationToken.None);
        updateResult.IsSuccess.Should().BeTrue();
        await dbContext.SaveChangesAsync();

        var updated = await repository.GetByIdAsync(device.Id, CancellationToken.None);
        updated.IsSuccess.Should().BeTrue();
        updated.Value!.Name.Should().StartWith("Updated-");
        updated.Value.Currency.Code.Should().Be("EUR");
        updated.Value.Model.Should().Be("Model 2");
        updated.Value.SerialNumber.Should().Be("SN-200");
        updated.Value.PurchasePrice.Should().Be(456.78m);

        device.ChangeStatus(DeviceStatus.Disposed, new DateOnly(2025, 1, 1), "Recycled", "apone");
        var softDeleteResult = await repository.UpdateAsync(device, CancellationToken.None);
        softDeleteResult.IsSuccess.Should().BeTrue();
        await dbContext.SaveChangesAsync();

        var softDeleted = await repository.GetByIdAsync(device.Id, CancellationToken.None);
        softDeleted.IsSuccess.Should().BeTrue();
        softDeleted.Value!.Status.Should().Be(DeviceStatus.Disposed);
        softDeleted.Value.DisposalMethod.Should().Be("Recycled");
    }

    [Fact]
    public async Task DeviceRepository_ListAsyncReturnsActiveOnlyByDefault()
    {
        await using var dbContext = await _host.CreateDbContextAsync();
        var repository = _host.CreateRepository<IDeviceRepository>(dbContext, "DeviceRepository");
        var refs = await SeedDeviceReferencesAsync(dbContext);
        var activeDevice = CreateDevice(refs, $"Active-{Guid.NewGuid():N}");
        var retiredDevice = CreateDevice(refs, $"Retired-{Guid.NewGuid():N}");
        retiredDevice.ChangeStatus(DeviceStatus.Retired, new DateOnly(2025, 1, 15), null, "apone");
        var disposedDevice = CreateDevice(refs, $"Disposed-{Guid.NewGuid():N}");
        disposedDevice.ChangeStatus(DeviceStatus.Disposed, new DateOnly(2025, 2, 1), "Recycled", "apone");

        (await repository.AddAsync(activeDevice, CancellationToken.None)).IsSuccess.Should().BeTrue();
        (await repository.AddAsync(retiredDevice, CancellationToken.None)).IsSuccess.Should().BeTrue();
        (await repository.AddAsync(disposedDevice, CancellationToken.None)).IsSuccess.Should().BeTrue();
        await dbContext.SaveChangesAsync();

        var defaultList = await repository.ListAsync(new DeviceListCriteria(new PageRequest()), CancellationToken.None);

        defaultList.Items.Select(item => item.Id).Should().Contain(activeDevice.Id);
        defaultList.Items.Select(item => item.Id).Should().NotContain(retiredDevice.Id);
        defaultList.Items.Select(item => item.Id).Should().NotContain(disposedDevice.Id);
    }

    [Fact]
    public async Task DeviceRepository_ListAsyncCanFilterByExplicitStatus()
    {
        await using var dbContext = await _host.CreateDbContextAsync();
        var repository = _host.CreateRepository<IDeviceRepository>(dbContext, "DeviceRepository");
        var refs = await SeedDeviceReferencesAsync(dbContext);
        var activeDevice = CreateDevice(refs, $"Active-{Guid.NewGuid():N}");
        var disposedDevice = CreateDevice(refs, $"Disposed-{Guid.NewGuid():N}");
        disposedDevice.ChangeStatus(DeviceStatus.Disposed, new DateOnly(2025, 2, 1), "Recycled", "apone");

        (await repository.AddAsync(activeDevice, CancellationToken.None)).IsSuccess.Should().BeTrue();
        (await repository.AddAsync(disposedDevice, CancellationToken.None)).IsSuccess.Should().BeTrue();
        await dbContext.SaveChangesAsync();

        var disposedList = await repository.ListAsync(new DeviceListCriteria(new PageRequest(), status: DeviceStatus.Disposed), CancellationToken.None);

        disposedList.Items.Select(item => item.Id).Should().Contain(disposedDevice.Id);
        disposedList.Items.Select(item => item.Id).Should().NotContain(activeDevice.Id);
    }

    [Fact]
    public async Task DeviceRepository_ListAsyncIncludesAllStatusesWhenRequested()
    {
        await using var dbContext = await _host.CreateDbContextAsync();
        var repository = _host.CreateRepository<IDeviceRepository>(dbContext, "DeviceRepository");
        var refs = await SeedDeviceReferencesAsync(dbContext);
        var activeDevice = CreateDevice(refs, $"Active-{Guid.NewGuid():N}");
        var retiredDevice = CreateDevice(refs, $"Retired-{Guid.NewGuid():N}");
        retiredDevice.ChangeStatus(DeviceStatus.Retired, new DateOnly(2025, 1, 15), null, "apone");
        var disposedDevice = CreateDevice(refs, $"Disposed-{Guid.NewGuid():N}");
        disposedDevice.ChangeStatus(DeviceStatus.Disposed, new DateOnly(2025, 2, 1), "Recycled", "apone");
        var inRepairDevice = CreateDevice(refs, $"InRepair-{Guid.NewGuid():N}");
        inRepairDevice.ChangeStatus(DeviceStatus.InRepair, null, null, "apone");
        var lentDevice = CreateDevice(refs, $"Lent-{Guid.NewGuid():N}");
        lentDevice.ChangeStatus(DeviceStatus.Lent, null, null, "apone");

        (await repository.AddAsync(activeDevice, CancellationToken.None)).IsSuccess.Should().BeTrue();
        (await repository.AddAsync(retiredDevice, CancellationToken.None)).IsSuccess.Should().BeTrue();
        (await repository.AddAsync(disposedDevice, CancellationToken.None)).IsSuccess.Should().BeTrue();
        (await repository.AddAsync(inRepairDevice, CancellationToken.None)).IsSuccess.Should().BeTrue();
        (await repository.AddAsync(lentDevice, CancellationToken.None)).IsSuccess.Should().BeTrue();
        await dbContext.SaveChangesAsync();

        var allStatusList = await repository.ListAsync(new DeviceListCriteria(new PageRequest(), includeAllStatuses: true), CancellationToken.None);

        allStatusList.Items.Select(item => item.Id).Should().Contain(activeDevice.Id);
        allStatusList.Items.Select(item => item.Id).Should().Contain(retiredDevice.Id);
        allStatusList.Items.Select(item => item.Id).Should().Contain(disposedDevice.Id);
        allStatusList.Items.Select(item => item.Id).Should().Contain(inRepairDevice.Id);
        allStatusList.Items.Select(item => item.Id).Should().Contain(lentDevice.Id);
    }

    [Fact]
    public async Task DeviceRepository_AuditColumnsAreStampedInUtc()
    {
        await using var dbContext = await _host.CreateDbContextAsync(requireSaveChangesInterceptor: true);
        var repository = _host.CreateRepository<IDeviceRepository>(dbContext, "DeviceRepository");
        var refs = await SeedDeviceReferencesAsync(dbContext);
        var device = CreateDevice(refs, $"Stamped-{Guid.NewGuid():N}");
        var addSentinel = new DateTimeOffset(2001, 1, 1, 0, 0, 0, TimeSpan.Zero);

        device.SetAuditMetadata(addSentinel, addSentinel, "legacy", "legacy");
        (await repository.AddAsync(device, CancellationToken.None)).IsSuccess.Should().BeTrue();
        await dbContext.SaveChangesAsync();

        var added = await repository.GetByIdAsync(device.Id, CancellationToken.None);
        added.IsSuccess.Should().BeTrue();
        added.Value!.CreatedAt.Should().NotBe(addSentinel);
        added.Value.ModifiedAt.Should().NotBe(addSentinel);
        added.Value.CreatedAt.Offset.Should().Be(TimeSpan.Zero);
        added.Value.ModifiedAt.Offset.Should().Be(TimeSpan.Zero);

        var updateSentinel = added.Value.CreatedAt.AddDays(1);
        device.UpdateDetails(
            $"Stamped-Updated-{Guid.NewGuid():N}",
            refs.BrandId,
            refs.CategoryId,
            refs.OwnerId,
            refs.LocationId,
            Currency.From("USD"),
            model: "Model 3",
            serialNumber: "SN-300",
            networkId: refs.NetworkId,
            purchaseDate: new DateOnly(2024, 7, 1),
            purchasePrice: 789.01m,
            modifiedBy: "apone");
        device.SetAuditMetadata(added.Value.CreatedAt, updateSentinel, "legacy", "legacy");

        (await repository.UpdateAsync(device, CancellationToken.None)).IsSuccess.Should().BeTrue();
        await dbContext.SaveChangesAsync();

        var updated = await repository.GetByIdAsync(device.Id, CancellationToken.None);
        updated.IsSuccess.Should().BeTrue();
        updated.Value!.ModifiedAt.Should().NotBe(updateSentinel);
        updated.Value.ModifiedAt.Offset.Should().Be(TimeSpan.Zero);
        updated.Value.ModifiedAt.Should().BeOnOrAfter(added.Value.ModifiedAt);
    }

    private static async Task<DeviceReferenceIds> SeedDeviceReferencesAsync(AppDbContext dbContext)
    {
        var household = new Household(Guid.NewGuid(), $"Household-{Guid.NewGuid():N}", Currency.From("USD"));
        var brand = new Brand(Guid.NewGuid(), $"Brand-{Guid.NewGuid():N}");
        var category = new Category(Guid.NewGuid(), $"Category-{Guid.NewGuid():N}");
        var owner = new Owner(Guid.NewGuid(), $"Owner-{Guid.NewGuid():N}");
        var location = new Location(Guid.NewGuid(), $"Location-{Guid.NewGuid():N}", LocationType.Home);
        var network = new Network(Guid.NewGuid(), $"Network-{Guid.NewGuid():N}");

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
            model: "Model 1",
            serialNumber: "SN-100",
            networkId: refs.NetworkId,
            purchaseDate: new DateOnly(2024, 5, 1),
            purchasePrice: 123.45m,
            currency: Currency.From("USD"));
    }

    private sealed record DeviceReferenceIds(Guid HouseholdId, Guid BrandId, Guid CategoryId, Guid OwnerId, Guid LocationId, Guid NetworkId);
}
