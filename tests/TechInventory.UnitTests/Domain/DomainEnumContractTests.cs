using FluentAssertions;
using TechInventory.Domain.Enums;

namespace TechInventory.UnitTests.Domain;

public class DomainEnumContractTests
{
    [Fact]
    public void AuditAction_ContainsTheExpectedValues()
    {
        Enum.GetNames<AuditAction>().Should().BeEquivalentTo(["Created", "Updated", "Deleted"]);
    }

    [Fact]
    public void ImportStatus_ContainsTheExpectedValues()
    {
        Enum.GetNames<ImportStatus>().Should().BeEquivalentTo(["Pending", "Completed", "PartialSuccess", "Failed"]);
    }
}
