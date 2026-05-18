using FluentAssertions;
using TechInventory.Domain.ValueObjects;

namespace TechInventory.UnitTests.Domain;

public class CurrencyContractTests
{
    [Fact]
    public void Currency_AcceptsValidIso4217UppercaseCode()
    {
        var currency = Currency.From("USD");

        currency.Code.Should().Be("USD");
    }

    [Fact]
    public void Currency_NormalizesLowercaseCodesToUppercase()
    {
        var currency = Currency.From("usd");

        currency.Code.Should().Be("USD");
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDD")]
    public void Currency_RejectsCodesThatAreNotExactlyThreeCharacters(string invalidCode)
    {
        var act = () => Currency.From(invalidCode);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Currency_RejectsCodesOutsideTheIso4217Allowlist()
    {
        var act = () => Currency.From("ZZZ");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
