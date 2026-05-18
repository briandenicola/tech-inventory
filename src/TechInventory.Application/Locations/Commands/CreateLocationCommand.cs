using MediatR;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Locations.Commands;

public sealed record CreateLocationCommand(string Name, TechInventory.Domain.Enums.LocationType Type) : IRequest<Result<LocationResponse>>, IAuditable;

public sealed class CreateLocationCommandHandler(
    ILocationRepository locationRepository,
    IUnitOfWork unitOfWork,
    IAuditContext auditContext) : IRequestHandler<CreateLocationCommand, Result<LocationResponse>>
{
    public async Task<Result<LocationResponse>> Handle(CreateLocationCommand request, CancellationToken cancellationToken)
    {
        var duplicateResult = await locationRepository.GetByNormalizedNameAsync(request.Name, cancellationToken).ConfigureAwait(false);
        if (duplicateResult.IsSuccess)
        {
            return Result<LocationResponse>.Failure(Error.Conflict($"Location with name '{request.Name.Trim()}' already exists."));
        }

        try
        {
            var entity = new Location(Guid.NewGuid(), request.Name, request.Type);
            var addResult = await locationRepository.AddAsync(entity, cancellationToken).ConfigureAwait(false);
            if (addResult.IsFailure)
            {
                return Result<LocationResponse>.Failure(addResult.Error!);
            }

            auditContext.Set(new AuditContextEntry(nameof(Location), entity.Id.ToString(), AuditAction.Created));
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<LocationResponse>.Success(LocationResponse.FromEntity(entity));
        }
        catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
        {
            return Result<LocationResponse>.Failure(Error.Conflict(exception.Message));
        }
    }
}
