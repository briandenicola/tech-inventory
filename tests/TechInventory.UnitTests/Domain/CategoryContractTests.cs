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
    }

    [Fact]
    public void Category_RejectsDepthGreaterThanThree()
    {
        var act = () => new Category(Guid.NewGuid(), "Too Deep", Guid.NewGuid(), 4);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
