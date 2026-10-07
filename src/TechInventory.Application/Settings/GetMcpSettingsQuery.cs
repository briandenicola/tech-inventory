using MediatR;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;

namespace TechInventory.Application.Settings;

public sealed record GetMcpSettingsQuery : IRequest<Result<McpSettingsResponse>>;

public sealed class GetMcpSettingsQueryHandler(
    IHouseholdRepository householdRepository,
    IHouseholdSettingRepository householdSettingRepository)
    : IRequestHandler<GetMcpSettingsQuery, Result<McpSettingsResponse>>
{
    public async Task<Result<McpSettingsResponse>> Handle(
        GetMcpSettingsQuery request,
        CancellationToken cancellationToken)
    {
        var household = (await householdRepository.ListAsync(cancellationToken).ConfigureAwait(false)).FirstOrDefault();
        if (household is null)
        {
            return Result<McpSettingsResponse>.Failure(Error.NotFound("Household was not found."));
        }

        var settings = await householdSettingRepository
            .ListByHouseholdAsync(household.Id, cancellationToken)
            .ConfigureAwait(false);
        var setting = settings.SingleOrDefault(candidate =>
            string.Equals(candidate.Key, McpSettingsCatalog.EnabledKey, StringComparison.OrdinalIgnoreCase));

        try
        {
            return Result<McpSettingsResponse>.Success(McpSettingsCatalog.ToResponse(setting));
        }
        catch (InvalidOperationException exception)
        {
            return Result<McpSettingsResponse>.Failure(Error.Conflict(exception.Message));
        }
    }
}
