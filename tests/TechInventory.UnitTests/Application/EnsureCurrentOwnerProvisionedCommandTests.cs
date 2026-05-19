using FluentAssertions;
using NSubstitute;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Results;
using TechInventory.Application.Owners;
using TechInventory.Application.Owners.Commands;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.UnitTests.Application;

public sealed class EnsureCurrentOwnerProvisionedCommandTests
{
    [Fact]
    public async Task Handle_WhenOwnerAlreadyExists_ReturnsExistingOwnerWithoutWriting()
    {
        var deps = CreateDependencies();
        var entraObjectId = Guid.NewGuid();
        var existingOwner = new Owner(Guid.NewGuid(), "Existing Owner", OwnerRole.Viewer, entraObjectId);
        deps.OwnerRepository.GetByEntraObjectIdAsync(entraObjectId, Arg.Any<CancellationToken>())
            .Returns(Result<Owner>.Success(existingOwner));
        var handler = new EnsureCurrentOwnerProvisionedCommandHandler(deps.OwnerRepository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(
            new EnsureCurrentOwnerProvisionedCommand(entraObjectId, "Ignored Name", "Admin"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(existingOwner.Id);
        result.Value.DisplayName.Should().Be(existingOwner.DisplayName);
        result.Value.Role.Should().Be(existingOwner.Role.ToString());
        await deps.OwnerRepository.DidNotReceive().AddAsync(Arg.Any<Owner>(), Arg.Any<CancellationToken>());
        deps.AuditContext.DidNotReceive().Set(Arg.Any<AuditContextEntry>());
        await deps.UnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenOwnerMissing_ProvisionsOwnerFromClaims()
    {
        var deps = CreateDependencies();
        var entraObjectId = Guid.NewGuid();
        deps.OwnerRepository.GetByEntraObjectIdAsync(entraObjectId, Arg.Any<CancellationToken>())
            .Returns(Result<Owner>.Failure(Error.NotFound("missing")));
        deps.OwnerRepository.GetByNormalizedDisplayNameAsync("dev-admin", Arg.Any<CancellationToken>())
            .Returns(Result<Owner>.Failure(Error.NotFound("missing")));
        deps.OwnerRepository.AddAsync(Arg.Any<Owner>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<Owner>.Success(call.Arg<Owner>()));
        deps.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = new EnsureCurrentOwnerProvisionedCommandHandler(deps.OwnerRepository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(
            new EnsureCurrentOwnerProvisionedCommand(entraObjectId, "dev-admin", "Admin"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.EntraObjectId.Should().Be(entraObjectId);
        result.Value.DisplayName.Should().Be("dev-admin");
        result.Value.Role.Should().Be(OwnerRole.Admin.ToString());
        await deps.OwnerRepository.Received(1).AddAsync(
            Arg.Is<Owner>(owner =>
                owner.EntraObjectId == entraObjectId &&
                owner.DisplayName == "dev-admin" &&
                owner.Role == OwnerRole.Admin),
            Arg.Any<CancellationToken>());
        deps.AuditContext.Received(1).Set(Arg.Is<AuditContextEntry>(entry =>
            entry.EntityType == nameof(Owner) &&
            entry.Action == AuditAction.Created));
        await deps.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenClaimsMissing_UsesFallbackDisplayNameAndMemberRole()
    {
        var deps = CreateDependencies();
        var entraObjectId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        deps.OwnerRepository.GetByEntraObjectIdAsync(entraObjectId, Arg.Any<CancellationToken>())
            .Returns(Result<Owner>.Failure(Error.NotFound("missing")));
        deps.OwnerRepository.GetByNormalizedDisplayNameAsync("User 11111111", Arg.Any<CancellationToken>())
            .Returns(Result<Owner>.Failure(Error.NotFound("missing")));
        deps.OwnerRepository.AddAsync(Arg.Any<Owner>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<Owner>.Success(call.Arg<Owner>()));
        deps.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = new EnsureCurrentOwnerProvisionedCommandHandler(deps.OwnerRepository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(
            new EnsureCurrentOwnerProvisionedCommand(entraObjectId, null, "not-a-role"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DisplayName.Should().Be("User 11111111");
        result.Value.Role.Should().Be(OwnerRole.Member.ToString());
    }

    [Fact]
    public async Task Validate_WhenEntraObjectIdMissing_ReturnsValidationFailure()
    {
        var result = await DeviceHandlerTestSupport.ValidateAsync(
            new EnsureCurrentOwnerProvisionedCommand(Guid.Empty, "dev-admin", "Admin"),
            new EnsureCurrentOwnerProvisionedCommandValidator(),
            OwnerResponse.FromEntity(DeviceHandlerTestSupport.CreateOwner()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
    }

    private static ProvisioningDeps CreateDependencies() => new(
        Substitute.For<IOwnerRepository>(),
        Substitute.For<IUnitOfWork>(),
        Substitute.For<IAuditContext>());

    private sealed record ProvisioningDeps(
        IOwnerRepository OwnerRepository,
        IUnitOfWork UnitOfWork,
        IAuditContext AuditContext);
}
