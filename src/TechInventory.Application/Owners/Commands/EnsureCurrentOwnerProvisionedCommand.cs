using MediatR;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Owners.Commands;

public sealed record EnsureCurrentOwnerProvisionedCommand(Guid EntraObjectId, string? DisplayName, string? RoleClaim)
    : IRequest<Result<OwnerResponse>>, IAuditable;

public sealed class EnsureCurrentOwnerProvisionedCommandHandler(
    IOwnerRepository ownerRepository,
    IUnitOfWork unitOfWork,
    IAuditContext auditContext) : IRequestHandler<EnsureCurrentOwnerProvisionedCommand, Result<OwnerResponse>>
{
    public async Task<Result<OwnerResponse>> Handle(EnsureCurrentOwnerProvisionedCommand request, CancellationToken cancellationToken)
    {
        var existingOwnerResult = await ownerRepository.GetByEntraObjectIdAsync(request.EntraObjectId, cancellationToken).ConfigureAwait(false);
        if (existingOwnerResult.IsSuccess)
        {
            return Result<OwnerResponse>.Success(OwnerResponse.FromEntity(existingOwnerResult.Value!));
        }

        if (!string.Equals(existingOwnerResult.Error?.Code, "NotFound", StringComparison.Ordinal))
        {
            return Result<OwnerResponse>.Failure(existingOwnerResult.Error!);
        }

        var displayName = await ResolveDisplayNameAsync(request, cancellationToken).ConfigureAwait(false);
        var role = ResolveRole(request.RoleClaim);
        var owner = new Owner(Guid.NewGuid(), displayName, role, request.EntraObjectId);

        var addResult = await ownerRepository.AddAsync(owner, cancellationToken).ConfigureAwait(false);
        if (addResult.IsFailure)
        {
            return Result<OwnerResponse>.Failure(addResult.Error!);
        }

        auditContext.Set(new AuditContextEntry(nameof(Owner), owner.Id.ToString(), AuditAction.Created));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<OwnerResponse>.Success(OwnerResponse.FromEntity(owner));
    }

    private async Task<string> ResolveDisplayNameAsync(EnsureCurrentOwnerProvisionedCommand request, CancellationToken cancellationToken)
    {
        var preferredDisplayName = string.IsNullOrWhiteSpace(request.DisplayName)
            ? BuildFallbackDisplayName(request.EntraObjectId)
            : request.DisplayName.Trim();

        if (await IsDisplayNameAvailableAsync(preferredDisplayName, cancellationToken).ConfigureAwait(false))
        {
            return preferredDisplayName;
        }

        var fallbackDisplayName = BuildFallbackDisplayName(request.EntraObjectId);
        if (!string.Equals(preferredDisplayName, fallbackDisplayName, StringComparison.OrdinalIgnoreCase)
            && await IsDisplayNameAvailableAsync(fallbackDisplayName, cancellationToken).ConfigureAwait(false))
        {
            return fallbackDisplayName;
        }

        return $"User {request.EntraObjectId:N}";
    }

    private async Task<bool> IsDisplayNameAvailableAsync(string displayName, CancellationToken cancellationToken)
    {
        var existingOwnerResult = await ownerRepository.GetByNormalizedDisplayNameAsync(displayName, cancellationToken).ConfigureAwait(false);
        return existingOwnerResult.IsFailure && string.Equals(existingOwnerResult.Error?.Code, "NotFound", StringComparison.Ordinal);
    }

    private static string BuildFallbackDisplayName(Guid entraObjectId) => $"User {entraObjectId:N}"[..13];

    private static OwnerRole ResolveRole(string? roleClaim)
        => Enum.TryParse<OwnerRole>(roleClaim, ignoreCase: true, out var role)
            ? role
            : OwnerRole.Member;
}
