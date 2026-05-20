using FluentValidation;
using MediatR;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Settings;

public sealed record UpdateDisplaySettingsCommand(
    IReadOnlyList<string> DeviceListColumns,
    IReadOnlyList<string> DeviceDetailFields) : IRequest<Result<DisplaySettingsResponse>>, IAuditable;

public sealed class UpdateDisplaySettingsCommandHandler(
    IHouseholdRepository householdRepository,
    IHouseholdSettingRepository householdSettingRepository,
    IUnitOfWork unitOfWork,
    IAuditContext auditContext) : IRequestHandler<UpdateDisplaySettingsCommand, Result<DisplaySettingsResponse>>
{
    public async Task<Result<DisplaySettingsResponse>> Handle(UpdateDisplaySettingsCommand request, CancellationToken cancellationToken)
    {
        var householdResult = await GetCurrentHouseholdAsync(cancellationToken).ConfigureAwait(false);
        if (householdResult.IsFailure)
        {
            return Result<DisplaySettingsResponse>.Failure(householdResult.Error!);
        }

        var household = householdResult.Value!;
        Dictionary<string, HouseholdSetting> settingByKey;

        try
        {
            settingByKey = await GetSettingDictionaryAsync(household.Id, cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException exception)
        {
            return Result<DisplaySettingsResponse>.Failure(Error.Conflict(exception.Message));
        }

        DisplaySettingsResponse beforeResponse;

        try
        {
            beforeResponse = DisplaySettingsCatalog.ToResponse(settingByKey);
        }
        catch (InvalidOperationException exception)
        {
            return Result<DisplaySettingsResponse>.Failure(Error.Conflict(exception.Message));
        }

        var deviceListColumns = DisplaySettingsCatalog.NormalizeColumns(request.DeviceListColumns);
        var deviceDetailFields = DisplaySettingsCatalog.NormalizeColumns(request.DeviceDetailFields);
        var afterResponse = new DisplaySettingsResponse(deviceListColumns, deviceDetailFields);

        var upsertListResult = await UpsertSettingAsync(
                settingByKey,
                household.Id,
                DisplaySettingsCatalog.DeviceListColumnsKey,
                deviceListColumns,
                cancellationToken)
            .ConfigureAwait(false);
        if (upsertListResult.IsFailure)
        {
            return Result<DisplaySettingsResponse>.Failure(upsertListResult.Error!);
        }

        var upsertDetailResult = await UpsertSettingAsync(
                settingByKey,
                household.Id,
                DisplaySettingsCatalog.DeviceDetailFieldsKey,
                deviceDetailFields,
                cancellationToken)
            .ConfigureAwait(false);
        if (upsertDetailResult.IsFailure)
        {
            return Result<DisplaySettingsResponse>.Failure(upsertDetailResult.Error!);
        }

        if (!upsertListResult.Value! && !upsertDetailResult.Value!)
        {
            return Result<DisplaySettingsResponse>.Success(beforeResponse);
        }

        auditContext.Set(new AuditContextEntry(
            nameof(HouseholdSetting),
            household.Id.ToString(),
            AuditAction.Updated,
            beforePayload: beforeResponse,
            afterPayload: afterResponse));

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<DisplaySettingsResponse>.Success(afterResponse);
    }

    private async Task<Result<bool>> UpsertSettingAsync(
        IDictionary<string, HouseholdSetting> settingByKey,
        Guid householdId,
        string settingKey,
        IReadOnlyList<string> columns,
        CancellationToken cancellationToken)
    {
        var serializedColumns = DisplaySettingsCatalog.SerializeColumns(columns);
        if (settingByKey.TryGetValue(settingKey, out var existingSetting))
        {
            if (string.Equals(existingSetting.Value, serializedColumns, StringComparison.Ordinal))
            {
                return Result<bool>.Success(false);
            }

            existingSetting.UpdateValue(serializedColumns);
            var updateResult = await householdSettingRepository.UpdateAsync(existingSetting, cancellationToken).ConfigureAwait(false);
            return updateResult.IsFailure
                ? Result<bool>.Failure(updateResult.Error!)
                : Result<bool>.Success(true);
        }

        var addResult = await householdSettingRepository.AddAsync(
                new HouseholdSetting(Guid.NewGuid(), householdId, settingKey, serializedColumns),
                cancellationToken)
            .ConfigureAwait(false);
        if (addResult.IsFailure)
        {
            return Result<bool>.Failure(addResult.Error!);
        }

        settingByKey[settingKey] = addResult.Value!;
        return Result<bool>.Success(true);
    }

    private async Task<Result<Household>> GetCurrentHouseholdAsync(CancellationToken cancellationToken)
    {
        var households = await householdRepository.ListAsync(cancellationToken).ConfigureAwait(false);
        var household = households.FirstOrDefault();

        return household is null
            ? Result<Household>.Failure(Error.NotFound("Household was not found."))
            : Result<Household>.Success(household);
    }

    private async Task<Dictionary<string, HouseholdSetting>> GetSettingDictionaryAsync(Guid householdId, CancellationToken cancellationToken)
    {
        var settings = await householdSettingRepository.ListByHouseholdAsync(householdId, cancellationToken).ConfigureAwait(false);
        var settingByKey = new Dictionary<string, HouseholdSetting>(StringComparer.OrdinalIgnoreCase);

        foreach (var setting in settings)
        {
            if (!settingByKey.TryAdd(setting.Key, setting))
            {
                throw new InvalidOperationException($"Duplicate household setting key '{setting.Key}' was found.");
            }
        }

        return settingByKey;
    }
}

public sealed class UpdateDisplaySettingsCommandValidator : AbstractValidator<UpdateDisplaySettingsCommand>
{
    public UpdateDisplaySettingsCommandValidator()
    {
        RuleFor(command => command.DeviceListColumns)
            .NotEmpty()
            .WithMessage("DeviceListColumns must contain at least one column identifier.")
            .Must(columns => !DisplaySettingsCatalog.HasDuplicates(columns))
            .WithMessage("DeviceListColumns cannot contain duplicate column identifiers.")
            .Must(DisplaySettingsCatalog.ContainsRequiredListColumn)
            .WithMessage("DeviceListColumns must include 'name'.");

        RuleForEach(command => command.DeviceListColumns)
            .Must(DisplaySettingsCatalog.IsAllowedDeviceListColumn)
            .WithMessage("DeviceListColumns contains an unknown column identifier.");

        RuleFor(command => command.DeviceDetailFields)
            .NotEmpty()
            .WithMessage("DeviceDetailFields must contain at least one column identifier.")
            .Must(columns => !DisplaySettingsCatalog.HasDuplicates(columns))
            .WithMessage("DeviceDetailFields cannot contain duplicate column identifiers.");

        RuleForEach(command => command.DeviceDetailFields)
            .Must(DisplaySettingsCatalog.IsAllowedDeviceDetailField)
            .WithMessage("DeviceDetailFields contains an unknown column identifier.");
    }
}
