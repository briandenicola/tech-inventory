using FluentAssertions;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;
using TechInventory.Domain.ValueObjects;

namespace TechInventory.UnitTests.Domain;

public class DeviceStateTransitionContractTests
{
    [Fact]
    public void Device_Create_RejectsANegativePurchasePrice()
    {
        var household = CreateHousehold();
        var act = () => Device.Create(
            Guid.NewGuid(),
            household,
            "Camera",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            purchasePrice: -1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Device_Create_RejectsAnEmptyOptionalNetworkId()
    {
        var household = CreateHousehold();
        var act = () => Device.Create(
            Guid.NewGuid(),
            household,
            "Camera",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            networkId: Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Device_ActiveStatus_RejectsARetiredDate()
    {
        var act = () => new Device(
            Guid.NewGuid(),
            "Camera",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Currency.From("USD"),
            status: DeviceStatus.Active,
            retiredDate: new DateOnly(2025, 1, 1));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Device_ActiveStatus_RejectsADisposalMethod()
    {
        var act = () => new Device(
            Guid.NewGuid(),
            "Camera",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Currency.From("USD"),
            status: DeviceStatus.Active,
            disposalMethod: "Recycled");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Device_ChangeStatusToRetired_DefaultsTheRetiredDateWhenMissing()
    {
        var device = CreateActiveDevice();

        device.ChangeStatus(DeviceStatus.Retired, modifiedBy: "apone");

        device.Status.Should().Be(DeviceStatus.Retired);
        device.RetiredDate.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));
        device.ModifiedBy.Should().Be("apone");
    }

    [Fact]
    public void Device_CannotLeaveTheRetiredStateThroughChangeStatus()
    {
        var device = new Device(
            Guid.NewGuid(),
            "Camera",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Currency.From("USD"),
            status: DeviceStatus.Retired,
            retiredDate: new DateOnly(2025, 1, 1));
        var act = () => device.ChangeStatus(DeviceStatus.Active, modifiedBy: "apone");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Device_UpdateDisposalMethod_RejectsActiveDevices()
    {
        var device = CreateActiveDevice();
        var act = () => device.UpdateDisposalMethod("Recycled", modifiedBy: "apone");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Device_UpdateDetails_UpdatesCoreFieldsAndAuditMetadata()
    {
        var device = CreateActiveDevice();
        var brandId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var networkId = Guid.NewGuid();

        device.UpdateDetails(
            "  Updated Camera  ",
            brandId,
            categoryId,
            ownerId,
            locationId,
            Currency.From("EUR"),
            model: "  X100  ",
            serialNumber: "  SN-123  ",
            networkId: networkId,
            purchaseDate: new DateOnly(2024, 2, 3),
            purchasePrice: 499.99m,
            modifiedBy: "apone");

        device.Name.Should().Be("Updated Camera");
        device.BrandId.Should().Be(brandId);
        device.CategoryId.Should().Be(categoryId);
        device.OwnerId.Should().Be(ownerId);
        device.LocationId.Should().Be(locationId);
        device.NetworkId.Should().Be(networkId);
        device.Currency.Code.Should().Be("EUR");
        device.Model.Should().Be("X100");
        device.SerialNumber.Should().Be("SN-123");
        device.PurchaseDate.Should().Be(new DateOnly(2024, 2, 3));
        device.PurchasePrice.Should().Be(499.99m);
        device.ModifiedBy.Should().Be("apone");
    }

    [Fact]
    public void Device_UpdateNotes_TrimsWhitespaceToNullAndTouchesAuditMetadata()
    {
        var device = CreateActiveDevice();

        device.UpdateNotes("   ", modifiedBy: "apone");

        device.Notes.Should().BeNull();
        device.ModifiedBy.Should().Be("apone");
    }

    [Fact]
    public void Device_UpdateDisposalMethod_AllowsRetiredDevices()
    {
        var device = new Device(
            Guid.NewGuid(),
            "Camera",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Currency.From("USD"),
            status: DeviceStatus.Retired,
            retiredDate: new DateOnly(2025, 1, 1));

        device.UpdateDisposalMethod("  Recycled  ", modifiedBy: "apone");

        device.DisposalMethod.Should().Be("Recycled");
        device.ModifiedBy.Should().Be("apone");
    }

    private static Household CreateHousehold() => new(Guid.NewGuid(), "Primary Household", Currency.From("USD"));

    private static Device CreateActiveDevice() => Device.Create(
        Guid.NewGuid(),
        CreateHousehold(),
        "Camera",
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid());
}
