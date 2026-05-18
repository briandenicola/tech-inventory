using FluentAssertions;
using NSubstitute;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Results;
using TechInventory.Application.Devices.Commands;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.UnitTests.Application;

public sealed class T28ClaimDeviceOwnershipHandlerScaffoldingTests
{
    [Fact]
    public async Task ClaimDeviceOwnershipCommandHandler_WhenValidInput_ReturnsSuccess()
    {
        var deviceRepository = Substitute.For<IDeviceRepository>();
        var ownerRepository = Substitute.For<IOwnerRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var auditContext = Substitute.For<IAuditContext>();
        var currentOwnerId = Guid.NewGuid();
        var targetOwner = DeviceHandlerTestSupport.CreateOwner();
        var device = DeviceHandlerTestSupport.CreateDevice(ownerId: currentOwnerId);
        deviceRepository.GetByIdAsync(device.Id, Arg.Any<CancellationToken>()).Returns(Result<Device>.Success(device));
        ownerRepository.GetByIdAsync(targetOwner.Id, Arg.Any<CancellationToken>()).Returns(Result<Owner>.Success(targetOwner));
        deviceRepository.UpdateAsync(device, Arg.Any<CancellationToken>()).Returns(Result<Device>.Success(device));
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = new ClaimDeviceOwnershipCommandHandler(deviceRepository, ownerRepository, unitOfWork, auditContext);

        var result = await handler.Handle(new ClaimDeviceOwnershipCommand(device.Id, targetOwner.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.OwnerId.Should().Be(targetOwner.Id);
        auditContext.Received(1).Set(Arg.Is<AuditContextEntry>(entry =>
            entry.Action == AuditAction.Updated &&
            entry.BeforePayload != null &&
            entry.BeforePayload.ToString()!.Contains(currentOwnerId.ToString(), StringComparison.Ordinal)));
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ClaimDeviceOwnershipCommandHandler_WhenValidationFails_ReturnsValidationFailure()
    {
        var result = await DeviceHandlerTestSupport.ValidateAsync(new ClaimDeviceOwnershipCommand(Guid.Empty, Guid.Empty), new ClaimDeviceOwnershipCommandValidator(), DeviceHandlerTestSupport.SampleDeviceResponse());

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
    }

    [Fact]
    public async Task ClaimDeviceOwnershipCommandHandler_WhenDeviceDoesNotExist_ReturnsNotFoundFailure()
    {
        var deviceRepository = Substitute.For<IDeviceRepository>();
        var ownerRepository = Substitute.For<IOwnerRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var auditContext = Substitute.For<IAuditContext>();
        deviceRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(Result<Device>.Failure(Error.NotFound("Device missing.")));
        var handler = new ClaimDeviceOwnershipCommandHandler(deviceRepository, ownerRepository, unitOfWork, auditContext);

        var result = await handler.Handle(new ClaimDeviceOwnershipCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NotFound");
    }

    [Fact]
    public async Task ClaimDeviceOwnershipCommandHandler_WhenRepositoryDetectsAConflict_ReturnsConflictFailure()
    {
        var deviceRepository = Substitute.For<IDeviceRepository>();
        var ownerRepository = Substitute.For<IOwnerRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var auditContext = Substitute.For<IAuditContext>();
        var owner = DeviceHandlerTestSupport.CreateOwner();
        var device = DeviceHandlerTestSupport.CreateDevice(ownerId: owner.Id);
        deviceRepository.GetByIdAsync(device.Id, Arg.Any<CancellationToken>()).Returns(Result<Device>.Success(device));
        var handler = new ClaimDeviceOwnershipCommandHandler(deviceRepository, ownerRepository, unitOfWork, auditContext);

        var result = await handler.Handle(new ClaimDeviceOwnershipCommand(device.Id, owner.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Conflict");
    }
}
