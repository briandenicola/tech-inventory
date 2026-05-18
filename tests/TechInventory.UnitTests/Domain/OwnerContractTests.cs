using FluentAssertions;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.UnitTests.Domain;

public class OwnerContractTests
{
    [Fact]
    public void OwnerRole_ContainsTheExpectedValues()
    {
        Enum.GetNames<OwnerRole>().Should().BeEquivalentTo(["Admin", "Member", "Viewer"]);
    }

    [Fact]
    public void Owner_RequiresADisplayName()
    {
        var act = () => new Owner(Guid.NewGuid(), null!, OwnerRole.Admin);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Owner_CapturesTheProvidedRoleAndOptionalEntraObjectId()
    {
        var entraObjectId = Guid.NewGuid();
        var owner = new Owner(Guid.NewGuid(), "Brian", OwnerRole.Admin, entraObjectId);

        owner.Role.Should().Be(OwnerRole.Admin);
        owner.EntraObjectId.Should().Be(entraObjectId);
    }

    [Fact]
    public void Owner_StartsActiveWhenCreated()
    {
        var owner = new Owner(Guid.NewGuid(), "Brian");

        owner.IsActive.Should().BeTrue();
    }
}
