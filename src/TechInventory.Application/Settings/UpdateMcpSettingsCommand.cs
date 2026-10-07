using MediatR;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Settings;

public sealed record UpdateMcpSettingsCommand(bool Enabled)
    : IRequest<Result<McpSettingsResponse>>, IAuditable;

public sealed class UpdateMcpSettingsCommandHandler(
    IHouseholdRepository householdRepository,
    IHouseholdSettingRepository householdSettingRepository,
    IUnitOfWork unitOfWork,
    IAuditContext auditContext)
    : IRequestHandler<UpdateMcpSettingsCommand, Result<McpSettingsResponse>>
{
    public async Task<Result<McpSettingsResponse>> Handle(
        UpdateMcpSettingsCommand request,
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

        McpSettingsResponse beforeResponse;
        try
        {
            beforeResponse = McpSettingsCatalog.ToResponse(setting);
        }
        catch (InvalidOperationException exception)
        {
            return Result<McpSettingsResponse>.Failure(Error.Conflict(exception.Message));
        }

        var afterResponse = new McpSettingsResponse(request.Enabled);
        if (beforeResponse == afterResponse)
        {
            return Result<McpSettingsResponse>.Success(afterResponse);
        }

        if (setting is null)
        {
            var addResult = await householdSettingRepository.AddAsync(
                    new HouseholdSetting(
                        Guid.NewGuid(),
                        household.Id,
                        McpSettingsCatalog.EnabledKey,
                        McpSettingsCatalog.Serialize(request.Enabled)),
                    cancellationToken)
                .ConfigureAwait(false);
            if (addResult.IsFailure)
            {
                return Result<McpSettingsResponse>.Failure(addResult.Error!);
            }
        }
        else
        {
            setting.UpdateValue(McpSettingsCatalog.Serialize(request.Enabled));
            var updateResult = await householdSettingRepository.UpdateAsync(setting, cancellationToken).ConfigureAwait(false);
            if (updateResult.IsFailure)
            {
                return Result<McpSettingsResponse>.Failure(updateResult.Error!);
            }
        }

        auditContext.Set(new AuditContextEntry(
            nameof(HouseholdSetting),
            household.Id.ToString(),
            AuditAction.Updated,
            beforePayload: beforeResponse,
            afterPayload: afterResponse));

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<McpSettingsResponse>.Success(afterResponse);
    }
}
