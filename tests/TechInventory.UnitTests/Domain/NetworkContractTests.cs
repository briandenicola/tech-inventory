using FluentAssertions;
using TechInventory.Domain.Entities;

namespace TechInventory.UnitTests.Domain;

public class NetworkContractTests
{
    [Fact]
    public void Network_RejectsAnEmptyName()
    {
        var act = () => new Network(Guid.NewGuid(), string.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Network_AllowsAnOptionalDescription()
    {
        var network = new Network(Guid.NewGuid(), "Guest WiFi", "Visitors only");

        network.Description.Should().Be("Visitors only");
        network.NormalizedName.Should().Be("GUEST WIFI");
    }

    [Fact]
    public void Network_StartsActiveWhenCreated()
    {
        var network = new Network(Guid.NewGuid(), "Guest WiFi");

        network.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Network_RenameAndUpdateDescription_TrimInputsAndTouchAuditMetadata()
    {
        var network = new Network(Guid.NewGuid(), "Guest WiFi");

        network.Rename("  Main LAN  ", modifiedBy: "apone");
        network.UpdateDescription("  Household devices only  ", modifiedBy: "ripley");

        network.Name.Should().Be("Main LAN");
        network.Description.Should().Be("Household devices only");
        network.ModifiedBy.Should().Be("ripley");
    }

    [Fact]
    public void Network_DeactivateAndReactivate_ToggleTheActiveFlag()
    {
        var network = new Network(Guid.NewGuid(), "Guest WiFi");

        network.Deactivate("apone");
        network.IsActive.Should().BeFalse();
        network.ModifiedBy.Should().Be("apone");

        network.Reactivate("ripley");
        network.IsActive.Should().BeTrue();
        network.ModifiedBy.Should().Be("ripley");
    }
}
