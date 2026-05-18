using FluentAssertions;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;
using TechInventory.Domain.ValueObjects;

namespace TechInventory.UnitTests.Domain;

public class DeviceInvariantContractTests
{
    [Fact]
    public void Device_RejectsAnEmptyName()
    {
        var household = new Household(Guid.NewGuid(), "Primary Household", Currency.From("USD"));
        var act = () => Device.Create(
            Guid.NewGuid(),
            household,
            string.Empty,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RetiredDevice_IsReadOnlyExceptForNotesAndDisposalMethod()
    {
        var device = new Device(
            Guid.NewGuid(),
            "Family Laptop",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Currency.From("USD"),
            status: DeviceStatus.Retired,
            retiredDate: new DateOnly(2025, 1, 1));

        var updateDetails = () => device.UpdateDetails(
            "Updated Name",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Currency.From("USD"));
        var updateNotes = () => device.UpdateNotes("still editable");
        var updateDisposal = () => device.UpdateDisposalMethod("Recycled");

        updateDetails.Should().Throw<InvalidOperationException>();
        updateNotes.Should().NotThrow();
        updateDisposal.Should().NotThrow();
        device.Notes.Should().Be("still editable");
        device.DisposalMethod.Should().Be("Recycled");
    }
}
