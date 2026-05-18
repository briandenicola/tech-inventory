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
        owner.NormalizedDisplayName.Should().Be("BRIAN");
    }

    [Fact]
    public void Owner_StartsActiveWhenCreated()
    {
        var owner = new Owner(Guid.NewGuid(), "Brian");

        owner.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Owner_Rename_TrimsWhitespaceAndTouchesAuditMetadata()
    {
        var owner = new Owner(Guid.NewGuid(), "Brian");

        owner.Rename("  Brian D  ", modifiedBy: "apone");

        owner.DisplayName.Should().Be("Brian D");
        owner.NormalizedDisplayName.Should().Be("BRIAN D");
        owner.ModifiedBy.Should().Be("apone");
    }

    [Fact]
    public void Owner_SetRole_UpdatesTheRoleAndAuditMetadata()
    {
        var owner = new Owner(Guid.NewGuid(), "Brian");

        owner.SetRole(OwnerRole.Viewer, modifiedBy: "apone");

        owner.Role.Should().Be(OwnerRole.Viewer);
        owner.ModifiedBy.Should().Be("apone");
    }

    [Fact]
    public void Owner_LinkEntraIdentity_RejectsAnEmptyGuid()
    {
        var owner = new Owner(Guid.NewGuid(), "Brian");
        var act = () => owner.LinkEntraIdentity(Guid.Empty, modifiedBy: "apone");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Owner_DeactivateAndReactivate_ToggleTheActiveFlag()
    {
        var owner = new Owner(Guid.NewGuid(), "Brian");

        owner.Deactivate("apone");
        owner.IsActive.Should().BeFalse();
        owner.ModifiedBy.Should().Be("apone");

        owner.Reactivate("ripley");
        owner.IsActive.Should().BeTrue();
        owner.ModifiedBy.Should().Be("ripley");
    }
}
