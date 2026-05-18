using FluentAssertions;
using TechInventory.Domain.Entities;
using TechInventory.Domain.ValueObjects;

namespace TechInventory.UnitTests.Domain;

public class HouseholdContractTests
{
    [Fact]
    public void Household_Rename_TrimsTheNameAndUpdatesAuditMetadata()
    {
        var household = new Household(Guid.NewGuid(), "Primary Household", Currency.From("USD"));

        household.Rename("  Family Home  ", modifiedBy: "apone");

        household.Name.Should().Be("Family Home");
        household.ModifiedBy.Should().Be("apone");
    }

    [Fact]
    public void Household_SetDefaultCurrency_UpdatesTheCurrencyAndAuditMetadata()
    {
        var household = new Household(Guid.NewGuid(), "Primary Household", Currency.From("USD"));

        household.SetDefaultCurrency(Currency.From("EUR"), modifiedBy: "apone");

        household.DefaultCurrency.Code.Should().Be("EUR");
        household.ModifiedBy.Should().Be("apone");
    }

    [Fact]
    public void Household_SetDefaultCurrency_RejectsNull()
    {
        var household = new Household(Guid.NewGuid(), "Primary Household", Currency.From("USD"));
        var act = () => household.SetDefaultCurrency(null!, modifiedBy: "apone");

        act.Should().Throw<ArgumentNullException>();
    }
}
