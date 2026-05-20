using MediatR;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;

namespace TechInventory.Application.Settings;

public sealed record GetDisplaySettingsQuery : IRequest<Result<DisplaySettingsResponse>>;

public sealed class GetDisplaySettingsQueryHandler(
    IHouseholdRepository householdRepository,
    IHouseholdSettingRepository householdSettingRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<GetDisplaySettingsQuery, Result<DisplaySettingsResponse>>
{
    public async Task<Result<DisplaySettingsResponse>> Handle(GetDisplaySettingsQuery request, CancellationToken cancellationToken)
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

        var seededDefaults = false;
        if (!settingByKey.TryGetValue(DisplaySettingsCatalog.DeviceListColumnsKey, out _))
        {
            var addResult = await householdSettingRepository.AddAsync(
                    DisplaySettingsCatalog.CreateDefaultSetting(household.Id, DisplaySettingsCatalog.DeviceListColumnsKey),
                    cancellationToken)
                .ConfigureAwait(false);
            if (addResult.IsFailure)
            {
                return Result<DisplaySettingsResponse>.Failure(addResult.Error!);
            }

            settingByKey[DisplaySettingsCatalog.DeviceListColumnsKey] = addResult.Value!;
            seededDefaults = true;
        }

        if (!settingByKey.TryGetValue(DisplaySettingsCatalog.DeviceDetailFieldsKey, out _))
        {
            var addResult = await householdSettingRepository.AddAsync(
                    DisplaySettingsCatalog.CreateDefaultSetting(household.Id, DisplaySettingsCatalog.DeviceDetailFieldsKey),
                    cancellationToken)
                .ConfigureAwait(false);
            if (addResult.IsFailure)
            {
                return Result<DisplaySettingsResponse>.Failure(addResult.Error!);
            }

            settingByKey[DisplaySettingsCatalog.DeviceDetailFieldsKey] = addResult.Value!;
            seededDefaults = true;
        }

        if (seededDefaults)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        try
        {
            return Result<DisplaySettingsResponse>.Success(DisplaySettingsCatalog.ToResponse(settingByKey));
        }
        catch (InvalidOperationException exception)
        {
            return Result<DisplaySettingsResponse>.Failure(Error.Conflict(exception.Message));
        }
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
