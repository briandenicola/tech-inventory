using FluentAssertions;
using TechInventory.Domain.Entities;

namespace TechInventory.UnitTests.Domain;

public class TagContractTests
{
    [Fact]
    public void Tag_RejectsAnEmptyName()
    {
        var act = () => new Tag(Guid.NewGuid(), string.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Tag_AllowsAnOptionalColor()
    {
        var tag = new Tag(Guid.NewGuid(), "Network", "#0EA5E9");

        tag.Color.Should().Be("#0EA5E9");
        tag.NormalizedName.Should().Be("NETWORK");
        tag.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Tag_RenameAndUpdateColor_TrimInputsAndTouchAuditMetadata()
    {
        var tag = new Tag(Guid.NewGuid(), "Network");

        tag.Rename("  Wireless  ", modifiedBy: "apone");
        tag.UpdateColor("  #22C55E  ", modifiedBy: "ripley");

        tag.Name.Should().Be("Wireless");
        tag.Color.Should().Be("#22C55E");
        tag.ModifiedBy.Should().Be("ripley");
    }

    [Fact]
    public void Tag_DeactivateAndReactivate_ToggleTheActiveFlag()
    {
        var tag = new Tag(Guid.NewGuid(), "Network");

        tag.Deactivate("apone");
        tag.IsActive.Should().BeFalse();
        tag.ModifiedBy.Should().Be("apone");

        tag.Reactivate("ripley");
        tag.IsActive.Should().BeTrue();
        tag.ModifiedBy.Should().Be("ripley");
    }

    [Fact]
    public void DeviceTag_RequiresANonDefaultDeviceId()
    {
        var act = () => new DeviceTag(Guid.Empty, Guid.NewGuid());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DeviceTag_RequiresANonDefaultTagId()
    {
        var act = () => new DeviceTag(Guid.NewGuid(), Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DeviceTag_PairsDeviceAndTagForCompositeIdentity()
    {
        var deviceId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        var deviceTag = new DeviceTag(deviceId, tagId);

        deviceTag.DeviceId.Should().Be(deviceId);
        deviceTag.TagId.Should().Be(tagId);
        deviceTag.IsActive.Should().BeTrue();
    }

    [Fact]
    public void DeviceTag_DeactivateAndReactivate_ToggleTheActiveFlag()
    {
        var deviceTag = new DeviceTag(Guid.NewGuid(), Guid.NewGuid());

        deviceTag.Deactivate();
        deviceTag.IsActive.Should().BeFalse();

        deviceTag.Reactivate();
        deviceTag.IsActive.Should().BeTrue();
    }
}
