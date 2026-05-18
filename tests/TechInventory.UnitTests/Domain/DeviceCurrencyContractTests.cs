using FluentAssertions;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;
using TechInventory.Domain.ValueObjects;

namespace TechInventory.UnitTests.Domain;

public class DeviceCurrencyContractTests
{
    [Fact]
    public void DeviceStatus_ContainsTheExpectedLifecycleValues()
    {
        Enum.GetNames<DeviceStatus>().Should().BeEquivalentTo(["Active", "Retired", "Disposed", "InRepair", "Lent"]);
    }

    [Fact]
    public void Household_ExposesADefaultCurrency()
    {
        var household = new Household(Guid.NewGuid(), "Primary Household", Currency.From("USD"));

        household.DefaultCurrency.Code.Should().Be("USD");
    }

    [Fact]
    public void Device_UsesHouseholdDefaultCurrencyWhenNoOverrideIsProvided()
    {
        var household = new Household(Guid.NewGuid(), "Primary Household", Currency.From("USD"));

        var device = Device.Create(
            Guid.NewGuid(),
            household,
            "Family Laptop",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid());

        device.Currency.Code.Should().Be("USD");
    }

    [Fact]
    public void Device_AllowsAnExplicitCurrencyOverride()
    {
        var household = new Household(Guid.NewGuid(), "Primary Household", Currency.From("USD"));

        var device = Device.Create(
            Guid.NewGuid(),
            household,
            "Family Laptop",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            currency: Currency.From("EUR"));

        device.Currency.Code.Should().Be("EUR");
    }

    [Fact]
    public void Device_RemainsValidWhenItsCurrencyDiffersFromTheParentHouseholdDefault()
    {
        var household = new Household(Guid.NewGuid(), "Primary Household", Currency.From("USD"));

        var device = Device.Create(
            Guid.NewGuid(),
            household,
            "Travel Camera",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            currency: Currency.From("JPY"));

        device.Currency.Code.Should().Be("JPY");
        household.DefaultCurrency.Code.Should().Be("USD");
    }
}
