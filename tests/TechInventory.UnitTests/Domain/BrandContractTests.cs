using FluentAssertions;
using TechInventory.Domain.Entities;

namespace TechInventory.UnitTests.Domain;

public class BrandContractTests
{
    [Fact]
    public void Brand_RejectsAnEmptyName()
    {
        var act = () => new Brand(Guid.NewGuid(), string.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Brand_RejectsANonAbsoluteWebsite()
    {
        var act = () => new Brand(Guid.NewGuid(), "Nintendo", website: "example.com");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Brand_TrimsInputsAndExposesANormalizedName()
    {
        var brand = new Brand(Guid.NewGuid(), "  Nintendo  ", website: "   ", notes: "  family favorite  ");

        brand.Name.Should().Be("Nintendo");
        brand.Website.Should().BeNull();
        brand.Notes.Should().Be("family favorite");
        brand.NormalizedName.Should().Be("NINTENDO");
        brand.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Brand_Rename_UpdatesNameAndAuditMetadata()
    {
        var brand = new Brand(Guid.NewGuid(), "Nintendo");

        brand.Rename("  Sony  ", modifiedBy: "apone");

        brand.Name.Should().Be("Sony");
        brand.NormalizedName.Should().Be("SONY");
        brand.ModifiedBy.Should().Be("apone");
    }

    [Fact]
    public void Brand_UpdateDetails_TrimsWebsiteAndNotes()
    {
        var brand = new Brand(Guid.NewGuid(), "Nintendo");

        brand.UpdateDetails(" https://example.com ", "  updated notes  ", modifiedBy: "apone");

        brand.Website.Should().Be("https://example.com");
        brand.Notes.Should().Be("updated notes");
        brand.ModifiedBy.Should().Be("apone");
    }

    [Fact]
    public void Brand_DeactivateAndReactivate_ToggleTheActiveFlag()
    {
        var brand = new Brand(Guid.NewGuid(), "Nintendo");

        brand.Deactivate("apone");
        brand.IsActive.Should().BeFalse();
        brand.ModifiedBy.Should().Be("apone");

        brand.Reactivate("ripley");
        brand.IsActive.Should().BeTrue();
        brand.ModifiedBy.Should().Be("ripley");
    }
}
