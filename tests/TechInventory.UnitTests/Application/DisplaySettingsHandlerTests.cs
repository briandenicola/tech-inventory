using FluentAssertions;
using NSubstitute;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Results;
using TechInventory.Application.Settings;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.UnitTests.Application;

public sealed class DisplaySettingsHandlerTests
{
    [Fact]
    public async Task GetDisplaySettingsQueryHandler_WhenSettingsMissing_ReturnsDefaultsAndSeedsRows()
    {
        var household = DeviceHandlerTestSupport.CreateHousehold();
        var householdRepository = Substitute.For<IHouseholdRepository>();
        var householdSettingRepository = Substitute.For<IHouseholdSettingRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();

        householdRepository.ListAsync(Arg.Any<CancellationToken>()).Returns([household]);
        householdSettingRepository.ListByHouseholdAsync(household.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<HouseholdSetting>());
        householdSettingRepository.AddAsync(Arg.Any<HouseholdSetting>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<HouseholdSetting>.Success(call.Arg<HouseholdSetting>()));

        var handler = new GetDisplaySettingsQueryHandler(householdRepository, householdSettingRepository, unitOfWork);

        var result = await handler.Handle(new GetDisplaySettingsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(DisplaySettingsCatalog.GetDefaultResponse());
        await householdSettingRepository.Received(2).AddAsync(Arg.Any<HouseholdSetting>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateDisplaySettingsCommandHandler_WhenCalled_UpsertsSettingsAndWritesAuditEntry()
    {
        var household = DeviceHandlerTestSupport.CreateHousehold();
        var existingListSetting = new HouseholdSetting(
            Guid.NewGuid(),
            household.Id,
            DisplaySettingsCatalog.DeviceListColumnsKey,
            DisplaySettingsCatalog.SerializeColumns(DisplaySettingsCatalog.DefaultDeviceListColumns));
        var householdRepository = Substitute.For<IHouseholdRepository>();
        var householdSettingRepository = Substitute.For<IHouseholdSettingRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var auditContext = Substitute.For<IAuditContext>();
        var command = new UpdateDisplaySettingsCommand(
            ["name", "category", "brand", "status"],
            ["brand", "status", "notes"]);

        householdRepository.ListAsync(Arg.Any<CancellationToken>()).Returns([household]);
        householdSettingRepository.ListByHouseholdAsync(household.Id, Arg.Any<CancellationToken>())
            .Returns([existingListSetting]);
        householdSettingRepository.UpdateAsync(Arg.Any<HouseholdSetting>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<HouseholdSetting>.Success(call.Arg<HouseholdSetting>()));
        householdSettingRepository.AddAsync(Arg.Any<HouseholdSetting>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<HouseholdSetting>.Success(call.Arg<HouseholdSetting>()));

        var handler = new UpdateDisplaySettingsCommandHandler(
            householdRepository,
            householdSettingRepository,
            unitOfWork,
            auditContext);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(new DisplaySettingsResponse(command.DeviceListColumns, command.DeviceDetailFields));
        existingListSetting.Value.Should().Be(DisplaySettingsCatalog.SerializeColumns(command.DeviceListColumns));
        await householdSettingRepository.Received(1).UpdateAsync(existingListSetting, Arg.Any<CancellationToken>());
        await householdSettingRepository.Received(1).AddAsync(
            Arg.Is<HouseholdSetting>(setting => setting.Key == DisplaySettingsCatalog.DeviceDetailFieldsKey),
            Arg.Any<CancellationToken>());
        auditContext.Received(1).Set(Arg.Is<AuditContextEntry>(entry =>
            entry.EntityType == nameof(HouseholdSetting)
            && entry.EntityId == household.Id.ToString()
            && entry.Action == AuditAction.Updated));
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
