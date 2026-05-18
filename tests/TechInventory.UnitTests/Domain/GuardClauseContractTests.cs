using FluentAssertions;
using TechInventory.Domain.Primitives;

namespace TechInventory.UnitTests.Domain;

public class GuardClauseContractTests
{
    [Fact]
    public void AgainstDefault_ReturnsTheGuidWhenItIsNotEmpty()
    {
        var value = Guid.NewGuid();

        Guard.AgainstDefault(value, "id").Should().Be(value);
    }

    [Fact]
    public void AgainstDefault_RejectsAnEmptyGuid()
    {
        var act = () => Guard.AgainstDefault(Guid.Empty, "id");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AgainstOptionalDefault_ReturnsNullWhenNoGuidIsProvided()
    {
        Guard.AgainstOptionalDefault(null, "networkId").Should().BeNull();
    }

    [Fact]
    public void AgainstOptionalDefault_ReturnsTheGuidWhenItIsNotEmpty()
    {
        var value = Guid.NewGuid();

        Guard.AgainstOptionalDefault(value, "networkId").Should().Be(value);
    }

    [Fact]
    public void AgainstOptionalDefault_RejectsAnEmptyGuid()
    {
        var act = () => Guard.AgainstOptionalDefault(Guid.Empty, "networkId");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AgainstNullOrWhiteSpace_TrimsValidValues()
    {
        Guard.AgainstNullOrWhiteSpace("  valid  ", "name", 10).Should().Be("valid");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AgainstNullOrWhiteSpace_RejectsNullOrWhitespace(string? value)
    {
        var act = () => Guard.AgainstNullOrWhiteSpace(value, "name", 10);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AgainstNullOrWhiteSpace_RejectsValuesOverTheMaxLength()
    {
        var act = () => Guard.AgainstNullOrWhiteSpace("toolong", "name", 3);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AgainstMaxLength_ReturnsNullForMissingValues(string? value)
    {
        Guard.AgainstMaxLength(value, "notes", 10).Should().BeNull();
    }

    [Fact]
    public void AgainstMaxLength_TrimsValidValues()
    {
        Guard.AgainstMaxLength("  notes  ", "notes", 10).Should().Be("notes");
    }

    [Fact]
    public void AgainstMaxLength_RejectsValuesOverTheMaxLength()
    {
        var act = () => Guard.AgainstMaxLength("toolong", "notes", 3);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AgainstNegative_AllowsNullValues()
    {
        Guard.AgainstNegative(null, "purchasePrice").Should().BeNull();
    }

    [Fact]
    public void AgainstNegative_AllowsZero()
    {
        Guard.AgainstNegative(0m, "purchasePrice").Should().Be(0m);
    }

    [Fact]
    public void AgainstNegative_AllowsPositiveValues()
    {
        Guard.AgainstNegative(42.5m, "purchasePrice").Should().Be(42.5m);
    }

    [Fact]
    public void AgainstNegative_RejectsNegativeValues()
    {
        var act = () => Guard.AgainstNegative(-1, "purchasePrice");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
