using FluentAssertions;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.UnitTests.Domain;

public class LocationContractTests
{
    [Fact]
    public void LocationType_ContainsTheExpectedValues()
    {
        Enum.GetNames<LocationType>().Should().BeEquivalentTo(["Home", "Storage", "External"]);
    }

    [Fact]
    public void Location_RejectsAnEmptyName()
    {
        var act = () => new Location(Guid.NewGuid(), string.Empty, LocationType.Home);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Location_PersistsTheProvidedLocationType()
    {
        var location = new Location(Guid.NewGuid(), "Hall Closet", LocationType.Storage);

        location.Type.Should().Be(LocationType.Storage);
    }

    [Fact]
    public void Location_StartsActiveWhenCreated()
    {
        var location = new Location(Guid.NewGuid(), "Hall Closet", LocationType.Storage);

        location.IsActive.Should().BeTrue();
    }
}
