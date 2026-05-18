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
}
