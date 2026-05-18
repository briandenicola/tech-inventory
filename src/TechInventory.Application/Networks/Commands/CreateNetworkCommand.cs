using MediatR;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Networks.Commands;

public sealed record CreateNetworkCommand(string Name, string? Description = null) : IRequest<Result<NetworkResponse>>, IAuditable;

public sealed class CreateNetworkCommandHandler(
    INetworkRepository networkRepository,
    IUnitOfWork unitOfWork,
    IAuditContext auditContext) : IRequestHandler<CreateNetworkCommand, Result<NetworkResponse>>
{
    public async Task<Result<NetworkResponse>> Handle(CreateNetworkCommand request, CancellationToken cancellationToken)
    {
        var duplicateResult = await networkRepository.GetByNormalizedNameAsync(request.Name, cancellationToken).ConfigureAwait(false);
        if (duplicateResult.IsSuccess)
        {
            return Result<NetworkResponse>.Failure(Error.Conflict($"Network with name '{request.Name.Trim()}' already exists."));
        }

        try
        {
            var entity = new Network(Guid.NewGuid(), request.Name, request.Description);
            var addResult = await networkRepository.AddAsync(entity, cancellationToken).ConfigureAwait(false);
            if (addResult.IsFailure)
            {
                return Result<NetworkResponse>.Failure(addResult.Error!);
            }

            auditContext.Set(new AuditContextEntry(nameof(Network), entity.Id.ToString(), AuditAction.Created));
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<NetworkResponse>.Success(NetworkResponse.FromEntity(entity));
        }
        catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
        {
            return Result<NetworkResponse>.Failure(Error.Conflict(exception.Message));
        }
    }
}
