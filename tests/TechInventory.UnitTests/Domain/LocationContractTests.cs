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
        location.NormalizedName.Should().Be("HALL CLOSET");
    }

    [Fact]
    public void Location_StartsActiveWhenCreated()
    {
        var location = new Location(Guid.NewGuid(), "Hall Closet", LocationType.Storage);

        location.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Location_RenameAndSetType_UpdateStateAndAuditMetadata()
    {
        var location = new Location(Guid.NewGuid(), "Hall Closet", LocationType.Storage);

        location.Rename("  Kitchen Shelf  ", modifiedBy: "apone");
        location.SetType(LocationType.Home, modifiedBy: "ripley");

        location.Name.Should().Be("Kitchen Shelf");
        location.Type.Should().Be(LocationType.Home);
        location.ModifiedBy.Should().Be("ripley");
    }

    [Fact]
    public void Location_DeactivateAndReactivate_ToggleTheActiveFlag()
    {
        var location = new Location(Guid.NewGuid(), "Hall Closet", LocationType.Storage);

        location.Deactivate("apone");
        location.IsActive.Should().BeFalse();
        location.ModifiedBy.Should().Be("apone");

        location.Reactivate("ripley");
        location.IsActive.Should().BeTrue();
        location.ModifiedBy.Should().Be("ripley");
    }
}
