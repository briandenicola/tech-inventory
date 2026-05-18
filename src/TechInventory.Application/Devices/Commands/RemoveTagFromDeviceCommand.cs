using MediatR;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Devices.Commands;

public sealed record RemoveTagFromDeviceCommand(Guid DeviceId, Guid TagId) : IRequest<Result>, IAuditable;

public sealed class RemoveTagFromDeviceCommandHandler(
    IDeviceRepository deviceRepository,
    IUnitOfWork unitOfWork,
    IAuditContext auditContext) : IRequestHandler<RemoveTagFromDeviceCommand, Result>
{
    public async Task<Result> Handle(RemoveTagFromDeviceCommand request, CancellationToken cancellationToken)
    {
        var deviceResult = await deviceRepository.GetByIdAsync(request.DeviceId, cancellationToken).ConfigureAwait(false);
        if (deviceResult.IsFailure)
        {
            return Result.Failure(deviceResult.Error!);
        }

        if (deviceResult.Value!.Status == DeviceStatus.Disposed)
        {
            return Result.Failure(Error.Conflict($"Device '{request.DeviceId}' is disposed."));
        }

        var existingTag = (await deviceRepository.ListTagsAsync(request.DeviceId, cancellationToken).ConfigureAwait(false))
            .FirstOrDefault(deviceTag => deviceTag.TagId == request.TagId);
        if (existingTag is null)
        {
            return Result.Failure(Error.NotFound($"Device tag '{request.DeviceId}:{request.TagId}' was not found."));
        }

        var removeResult = await deviceRepository.RemoveTagAsync(request.DeviceId, request.TagId, cancellationToken).ConfigureAwait(false);
        if (removeResult.IsFailure)
        {
            return Result.Failure(removeResult.Error!);
        }

        auditContext.Set(new AuditContextEntry(nameof(DeviceTag), $"{request.DeviceId}:{request.TagId}", AuditAction.Deleted, beforePayload: DeviceTagResponse.FromEntity(existingTag)));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
