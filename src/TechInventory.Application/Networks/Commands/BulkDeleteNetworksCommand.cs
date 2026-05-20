using MediatR;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.BulkOperations;
using TechInventory.Application.Common.Results;
using TechInventory.Application.Networks;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Networks.Commands;

public sealed record BulkDeleteNetworksCommand(IReadOnlyList<Guid> NetworkIds)
    : IRequest<Result<BulkOperationResponse>>, IAuditable, IBulkDeleteReferenceEntityCommand
{
    public IReadOnlyList<Guid> Ids => NetworkIds;
}

public sealed class BulkDeleteNetworksCommandHandler(
    INetworkRepository networkRepository,
    IUnitOfWork unitOfWork,
    IAuditContext auditContext) : IRequestHandler<BulkDeleteNetworksCommand, Result<BulkOperationResponse>>
{
    public async Task<Result<BulkOperationResponse>> Handle(BulkDeleteNetworksCommand request, CancellationToken cancellationToken)
    {
        var uniqueIds = request.NetworkIds.Distinct().ToArray();
        var networks = new List<Network>(uniqueIds.Length);

        foreach (var networkId in uniqueIds)
        {
            var networkResult = await networkRepository.GetByIdAsync(networkId, cancellationToken).ConfigureAwait(false);
            if (networkResult.IsFailure)
            {
                return Result<BulkOperationResponse>.Failure(networkResult.Error!);
            }

            var network = networkResult.Value!;
            if (!network.IsActive)
            {
                return Result<BulkOperationResponse>.Failure(Error.Conflict($"Network '{networkId}' is already inactive."));
            }

            networks.Add(network);
        }

        var correlationId = Guid.NewGuid();
        foreach (var network in networks)
        {
            var beforeSnapshot = NetworkResponse.FromEntity(network);
            network.Deactivate();

            var updateResult = await networkRepository.UpdateAsync(network, cancellationToken).ConfigureAwait(false);
            if (updateResult.IsFailure)
            {
                return Result<BulkOperationResponse>.Failure(updateResult.Error!);
            }

            auditContext.Add(new AuditContextEntry(
                nameof(Network),
                network.Id.ToString(),
                AuditAction.Deleted,
                beforePayload: new BulkAuditEnvelope(correlationId, beforeSnapshot),
                afterPayload: new BulkAuditEnvelope(correlationId, NetworkResponse.FromEntity(network))));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<BulkOperationResponse>.Success(new BulkOperationResponse(correlationId, networks.Count));
    }
}
