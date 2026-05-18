using MediatR;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Devices.Commands;

public sealed record AddTagToDeviceCommand(Guid DeviceId, Guid TagId) : IRequest<Result<DeviceTagResponse>>, IAuditable;

public sealed class AddTagToDeviceCommandHandler(
    IDeviceRepository deviceRepository,
    ITagRepository tagRepository,
    IUnitOfWork unitOfWork,
    IAuditContext auditContext) : IRequestHandler<AddTagToDeviceCommand, Result<DeviceTagResponse>>
{
    public async Task<Result<DeviceTagResponse>> Handle(AddTagToDeviceCommand request, CancellationToken cancellationToken)
    {
        var deviceResult = await deviceRepository.GetByIdAsync(request.DeviceId, cancellationToken).ConfigureAwait(false);
        if (deviceResult.IsFailure)
        {
            return Result<DeviceTagResponse>.Failure(deviceResult.Error!);
        }

        if (deviceResult.Value!.Status == DeviceStatus.Disposed)
        {
            return Result<DeviceTagResponse>.Failure(Error.Conflict($"Device '{request.DeviceId}' is disposed."));
        }

        var tagResult = await tagRepository.GetByIdAsync(request.TagId, cancellationToken).ConfigureAwait(false);
        if (tagResult.IsFailure)
        {
            return Result<DeviceTagResponse>.Failure(tagResult.Error!);
        }

        if (!tagResult.Value!.IsActive)
        {
            return Result<DeviceTagResponse>.Failure(Error.Conflict($"Tag '{request.TagId}' is inactive."));
        }

        var upsertResult = await deviceRepository.UpsertTagAsync(new DeviceTag(request.DeviceId, request.TagId), cancellationToken).ConfigureAwait(false);
        if (upsertResult.IsFailure)
        {
            return Result<DeviceTagResponse>.Failure(upsertResult.Error!);
        }

        auditContext.Set(new AuditContextEntry(nameof(DeviceTag), $"{request.DeviceId}:{request.TagId}", AuditAction.Created));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<DeviceTagResponse>.Success(DeviceTagResponse.FromEntity(upsertResult.Value!));
    }
}
