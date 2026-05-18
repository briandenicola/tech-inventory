using FluentAssertions;
using MediatR;
using NSubstitute;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Abstractions.Services;
using TechInventory.Application.Auditing;
using TechInventory.Application.Behaviors;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.UnitTests.Application.Behaviors;

public sealed class AuditBehaviorTests
{
    [Fact]
    public async Task Handle_WhenAuditableRequestSucceeds_AppendsAuditEventAndSavesChanges()
    {
        var auditRepository = Substitute.For<IAuditEventRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var currentUserService = Substitute.For<ICurrentUserService>();
        var auditContext = new AuditContext();
        var behavior = new AuditBehavior<AuditedCommand, Result<Guid>>(auditRepository, unitOfWork, auditContext, currentUserService);
        var entityId = Guid.NewGuid();
        var request = new AuditedCommand(entityId, "Family Laptop");

        currentUserService.GetCurrentUserId().Returns("system");
        auditRepository
            .AppendAsync(Arg.Any<AuditEvent>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Result<AuditEvent>.Success(callInfo.Arg<AuditEvent>()));
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await behavior.Handle(
            request,
            _ =>
            {
                auditContext.Set(new AuditContextEntry("Device", entityId.ToString(), AuditAction.Created));
                return Task.FromResult(Result<Guid>.Success(entityId));
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await auditRepository.Received(1).AppendAsync(
            Arg.Is<AuditEvent>(auditEvent =>
                auditEvent.Actor == "system" &&
                auditEvent.EntityType == "Device" &&
                auditEvent.EntityId == entityId.ToString() &&
                auditEvent.Action == AuditAction.Created &&
                auditEvent.BeforePayload == "null" &&
                auditEvent.AfterPayload.Contains("\"name\":\"Family Laptop\"", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        auditContext.Current.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenAuditableRequestFails_DoesNotAppendAuditEvent()
    {
        var auditRepository = Substitute.For<IAuditEventRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var currentUserService = Substitute.For<ICurrentUserService>();
        var auditContext = new AuditContext();
        var behavior = new AuditBehavior<AuditedCommand, Result<Guid>>(auditRepository, unitOfWork, auditContext, currentUserService);
        var failure = Result<Guid>.Failure(new Error("Conflict", "The device could not be updated."));

        var result = await behavior.Handle(
            new AuditedCommand(Guid.NewGuid(), "Conflict Device"),
            _ => Task.FromResult(failure),
            CancellationToken.None);

        result.Should().BeSameAs(failure);
        await auditRepository.DidNotReceiveWithAnyArgs().AppendAsync(default!, default);
        await unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
        auditContext.Current.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenRequestIsNotAuditable_IsANoOp()
    {
        var auditRepository = Substitute.For<IAuditEventRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var currentUserService = Substitute.For<ICurrentUserService>();
        var auditContext = new AuditContext();
        var behavior = new AuditBehavior<NonAuditableCommand, Result<Guid>>(auditRepository, unitOfWork, auditContext, currentUserService);
        var expected = Result<Guid>.Success(Guid.NewGuid());

        var result = await behavior.Handle(
            new NonAuditableCommand("Ripley"),
            _ => Task.FromResult(expected),
            CancellationToken.None);

        result.Should().BeSameAs(expected);
        await auditRepository.DidNotReceiveWithAnyArgs().AppendAsync(default!, default);
        await unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    private sealed record AuditedCommand(Guid Id, string Name) : IRequest<Result<Guid>>, IAuditable;

    private sealed record NonAuditableCommand(string Name) : IRequest<Result<Guid>>;
}
