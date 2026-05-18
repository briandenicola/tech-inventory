using FluentAssertions;
using TechInventory.Domain.Entities;

namespace TechInventory.UnitTests.Domain;

public class CategoryContractTests
{
    [Fact]
    public void Category_RejectsAnEmptyName()
    {
        var act = () => new Category(Guid.NewGuid(), string.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Category_AllowsAnOptionalParentCategory()
    {
        var parentId = Guid.NewGuid();
        var category = new Category(Guid.NewGuid(), "Routers", parentId, 2, "wifi");

        category.ParentId.Should().Be(parentId);
        category.Depth.Should().Be(2);
        category.Icon.Should().Be("wifi");
        category.IsActive.Should().BeTrue();
        category.NormalizedName.Should().Be("ROUTERS");
    }

    [Fact]
    public void Category_RejectsDepthGreaterThanThree()
    {
        var act = () => new Category(Guid.NewGuid(), "Too Deep", Guid.NewGuid(), 4);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Category_RootNodesMustUseDepthOne()
    {
        var act = () => new Category(Guid.NewGuid(), "Root", depth: 2);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Category_ChildNodesMustUseDepthTwoOrThree()
    {
        var act = () => new Category(Guid.NewGuid(), "Child", Guid.NewGuid(), 1);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Category_Reparent_UpdatesParentDepthAndAuditMetadata()
    {
        var category = new Category(Guid.NewGuid(), "Routers");
        var parentId = Guid.NewGuid();

        category.Reparent(parentId, 2, modifiedBy: "apone");

        category.ParentId.Should().Be(parentId);
        category.Depth.Should().Be(2);
        category.ModifiedBy.Should().Be("apone");
    }

    [Fact]
    public void Category_UpdateIcon_TrimsWhitespaceAndTouchesAuditMetadata()
    {
        var category = new Category(Guid.NewGuid(), "Routers");

        category.UpdateIcon("  wifi  ", modifiedBy: "apone");

        category.Icon.Should().Be("wifi");
        category.ModifiedBy.Should().Be("apone");
    }

    [Fact]
    public void Category_DeactivateAndReactivate_ToggleTheActiveFlag()
    {
        var category = new Category(Guid.NewGuid(), "Routers");

        category.Deactivate("apone");
        category.IsActive.Should().BeFalse();
        category.ModifiedBy.Should().Be("apone");

        category.Reactivate("ripley");
        category.IsActive.Should().BeTrue();
        category.ModifiedBy.Should().Be("ripley");
    }
}
