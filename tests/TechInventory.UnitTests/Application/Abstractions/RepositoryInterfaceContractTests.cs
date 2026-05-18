using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using TechInventory.UnitTests.Support;

namespace TechInventory.UnitTests.Application.Abstractions;

public class RepositoryInterfaceContractTests
{
    public static TheoryData<string> RepositoryInterfaces => new()
    {
        "IDeviceRepository",
        "IBrandRepository",
        "ICategoryRepository",
        "IHouseholdRepository",
        "IOwnerRepository",
        "ILocationRepository",
        "INetworkRepository",
        "ITagRepository",
        "IAuditEventRepository",
        "IImportBatchRepository"
    };

    [Theory]
    [MemberData(nameof(RepositoryInterfaces))]
    public void RepositoryInterfaces_AreMockableAndExposeOnlyAsyncContractShapes(string interfaceName)
    {
        var interfaceType = ContractReflectionAssertions.RequireApplicationType($"TechInventory.Application.Abstractions.Repositories.{interfaceName}", "awaiting Hicks T15");
        var substitute = ContractReflectionAssertions.CreateSubstitute(interfaceType);
        var methods = interfaceType.GetMethods(BindingFlags.Instance | BindingFlags.Public);

        substitute.Should().NotBeNull();
        interfaceType.IsAssignableFrom(substitute.GetType()).Should().BeTrue();
        methods.Should().NotBeEmpty();
        methods.Should().OnlyContain(method => ContractReflectionAssertions.HasCancellationToken(method));
        methods.Should().OnlyContain(method => ContractReflectionAssertions.ReturnsAllowedRepositoryShape(method.ReturnType));
        methods.Should().OnlyContain(method => !ContractReflectionAssertions.ContainsQueryable(method.ReturnType));
        methods.Should().OnlyContain(method => method.GetParameters().All(parameter => !ContractReflectionAssertions.ContainsQueryable(parameter.ParameterType)));
        interfaceType.GetProperties().Should().NotContain(property => ContractReflectionAssertions.ContainsQueryable(property.PropertyType));
    }

    [Fact]
    public void AuditEventRepository_ExposesAppendAndQueryMethodsOnly()
    {
        var interfaceType = ContractReflectionAssertions.RequireApplicationType("TechInventory.Application.Abstractions.Repositories.IAuditEventRepository", "awaiting Hicks T15");
        var methodNames = interfaceType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Select(method => method.Name)
            .ToArray();

        methodNames.Should().Contain("AppendAsync");
        methodNames.Should().NotContain(name => Regex.IsMatch(name, "Update|Delete|Remove", RegexOptions.IgnoreCase));
    }
}
