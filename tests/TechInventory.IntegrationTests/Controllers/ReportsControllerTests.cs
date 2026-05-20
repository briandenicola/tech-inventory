using System.Net;
using FluentAssertions;
using TechInventory.Application.Reports;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;
using TechInventory.Domain.ValueObjects;

namespace TechInventory.IntegrationTests.Controllers;

public sealed class ReportsControllerTests(IntegrationTestFactory<ReportsControllerTests> factory)
    : ControllerTestBase<ReportsControllerTests>(factory), IClassFixture<IntegrationTestFactory<ReportsControllerTests>>
{
    [Fact]
    public async Task GetSummary_WhenDataExists_ReturnsActiveInventoryAggregates()
    {
        await ResetDatabaseAsync();
        var references = await SeedDeviceReferenceDataAsync();
        var secondLocation = new Location(Guid.NewGuid(), "Workshop", LocationType.Home);
        var categories = Enumerable.Range(1, 6)
            .Select(index => new Category(Guid.NewGuid(), $"Category-{index}"))
            .ToArray();
        await SeedAsync(entities: [secondLocation, .. categories]);

        var activeDevices = new[]
        {
            CreateReportDevice(references.Household, references.Brand.Id, categories[0].Id, references.Owner.Id, references.Location.Id, references.Network.Id, "Alpha", DeviceStatus.Active, 100m),
            CreateReportDevice(references.Household, references.Brand.Id, categories[1].Id, references.Owner.Id, references.Location.Id, references.Network.Id, "Bravo", DeviceStatus.Active, 200m),
            CreateReportDevice(references.Household, references.Brand.Id, categories[2].Id, references.Owner.Id, references.Location.Id, references.Network.Id, "Charlie", DeviceStatus.Active, 300m),
            CreateReportDevice(references.Household, references.Brand.Id, categories[3].Id, references.Owner.Id, references.Location.Id, references.Network.Id, "Delta", DeviceStatus.Active, 400m),
            CreateReportDevice(references.Household, references.Brand.Id, categories[4].Id, references.Owner.Id, secondLocation.Id, references.Network.Id, "Echo", DeviceStatus.Lent, 500m),
            CreateReportDevice(references.Household, references.Brand.Id, categories[5].Id, references.Owner.Id, secondLocation.Id, references.Network.Id, "Foxtrot", DeviceStatus.InRepair, 600m),
            CreateReportDevice(references.Household, references.Brand.Id, categories[0].Id, references.Owner.Id, references.Location.Id, references.Network.Id, "Retired", DeviceStatus.Retired, 700m),
            CreateReportDevice(references.Household, references.Brand.Id, categories[1].Id, references.Owner.Id, secondLocation.Id, references.Network.Id, "Disposed", DeviceStatus.Disposed, 800m),
        };
        await SeedAsync(entities: activeDevices.Cast<object>().ToArray());
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/reports/summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadJsonAsync<SummaryReportResponse>(response);
        payload.TotalActiveDeviceCount.Should().Be(6);
        payload.TotalEstimatedValue.Should().Be(2100m);
        payload.DevicesByCategory.Should().HaveCount(6);
        payload.DevicesByCategory.Should().Contain(item => item.Label == "Others" && item.Count == 1);
        payload.DevicesByLocation.Should().ContainEquivalentOf(new ReportBreakdownItem(references.Location.Name, 4));
        payload.DevicesByLocation.Should().ContainEquivalentOf(new ReportBreakdownItem(secondLocation.Name, 2));
        payload.DevicesByStatus.Should().ContainEquivalentOf(new ReportBreakdownItem(DeviceStatus.Active.ToString(), 6));
        payload.DevicesByStatus.Should().ContainEquivalentOf(new ReportBreakdownItem(DeviceStatus.Retired.ToString(), 1));
        payload.DevicesByStatus.Should().ContainEquivalentOf(new ReportBreakdownItem(DeviceStatus.Disposed.ToString(), 1));
    }

    [Fact]
    public async Task GetWarranties_WhenCalledWithoutParameter_ReturnsUpcomingDevicesSortedByUrgency()
    {
        await ResetDatabaseAsync();
        var references = await SeedDeviceReferenceDataAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await SeedAsync(
            entities:
            [
                CreateReportDevice(references.Household, references.Brand.Id, references.Category.Id, references.Owner.Id, references.Location.Id, references.Network.Id, "Soonest", DeviceStatus.Active, 100m, warrantyExpiry: today.AddDays(10)),
                CreateReportDevice(references.Household, references.Brand.Id, references.Category.Id, references.Owner.Id, references.Location.Id, references.Network.Id, "Later", DeviceStatus.Active, 150m, warrantyExpiry: today.AddDays(20)),
                CreateReportDevice(references.Household, references.Brand.Id, references.Category.Id, references.Owner.Id, references.Location.Id, references.Network.Id, "Outside Window", DeviceStatus.Active, 200m, warrantyExpiry: today.AddDays(45)),
                CreateReportDevice(references.Household, references.Brand.Id, references.Category.Id, references.Owner.Id, references.Location.Id, references.Network.Id, "Retired", DeviceStatus.Retired, 250m, warrantyExpiry: today.AddDays(5))
            ]);
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/reports/warranties");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadJsonAsync<WarrantyReportResponse>(response);
        payload.ExpiringWithinDays.Should().Be(30);
        payload.Devices.Select(device => device.DeviceName).Should().Equal("Soonest", "Later");
        payload.Devices.Select(device => device.DaysRemaining).Should().Equal(10, 20);
    }

    [Fact]
    public async Task GetWarranties_WhenParameterInvalid_Returns400ValidationProblemDetails()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/reports/warranties?expiringWithinDays=366");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await ReadValidationProblemDetailsAsync(response);
        problem.Errors.Keys.Should().Contain(key => string.Equals(key, "ExpiringWithinDays", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetSpending_WhenGroupedByMonth_ReturnsFilteredMonthlyTotals()
    {
        await ResetDatabaseAsync();
        var references = await SeedDeviceReferenceDataAsync();
        await SeedAsync(
            entities:
            [
                CreateReportDevice(references.Household, references.Brand.Id, references.Category.Id, references.Owner.Id, references.Location.Id, references.Network.Id, "Jan-1", DeviceStatus.Active, 100m, purchaseDate: new DateOnly(2024, 1, 5)),
                CreateReportDevice(references.Household, references.Brand.Id, references.Category.Id, references.Owner.Id, references.Location.Id, references.Network.Id, "Jan-2", DeviceStatus.Active, 150m, purchaseDate: new DateOnly(2024, 1, 20)),
                CreateReportDevice(references.Household, references.Brand.Id, references.Category.Id, references.Owner.Id, references.Location.Id, references.Network.Id, "Feb", DeviceStatus.Active, 200m, purchaseDate: new DateOnly(2024, 2, 10)),
                CreateReportDevice(references.Household, references.Brand.Id, references.Category.Id, references.Owner.Id, references.Location.Id, references.Network.Id, "NextYear", DeviceStatus.Active, 300m, purchaseDate: new DateOnly(2025, 3, 1)),
                CreateReportDevice(references.Household, references.Brand.Id, references.Category.Id, references.Owner.Id, references.Location.Id, references.Network.Id, "NoPrice", DeviceStatus.Active, null, purchaseDate: new DateOnly(2024, 4, 1))
            ]);
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/reports/spending?groupBy=month&fromDate=2024-01-01&toDate=2024-12-31");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadJsonAsync<SpendingReportResponse>(response);
        payload.GroupBy.Should().Be(SpendingGroupBy.Month);
        payload.Periods.Should().BeEquivalentTo(
            [
                new SpendingReportPoint("2024-01", 250m, 2),
                new SpendingReportPoint("2024-02", 200m, 1),
            ]);
    }

    [Fact]
    public async Task GetSpending_WhenGroupedByYear_ReturnsYearlyTotals()
    {
        await ResetDatabaseAsync();
        var references = await SeedDeviceReferenceDataAsync();
        await SeedAsync(
            entities:
            [
                CreateReportDevice(references.Household, references.Brand.Id, references.Category.Id, references.Owner.Id, references.Location.Id, references.Network.Id, "First", DeviceStatus.Active, 100m, purchaseDate: new DateOnly(2024, 1, 5)),
                CreateReportDevice(references.Household, references.Brand.Id, references.Category.Id, references.Owner.Id, references.Location.Id, references.Network.Id, "Second", DeviceStatus.Disposed, 200m, purchaseDate: new DateOnly(2024, 6, 1)),
                CreateReportDevice(references.Household, references.Brand.Id, references.Category.Id, references.Owner.Id, references.Location.Id, references.Network.Id, "Third", DeviceStatus.Active, 300m, purchaseDate: new DateOnly(2025, 3, 1))
            ]);
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/reports/spending?groupBy=year");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadJsonAsync<SpendingReportResponse>(response);
        payload.GroupBy.Should().Be(SpendingGroupBy.Year);
        payload.Periods.Should().BeEquivalentTo(
            [
                new SpendingReportPoint("2024", 300m, 2),
                new SpendingReportPoint("2025", 300m, 1),
            ]);
    }

    private static Device CreateReportDevice(
        Household household,
        Guid? brandId,
        Guid categoryId,
        Guid ownerId,
        Guid locationId,
        Guid? networkId,
        string name,
        DeviceStatus status,
        decimal? purchasePrice,
        DateOnly? purchaseDate = null,
        DateOnly? warrantyExpiry = null)
    {
        var retiredDate = status is DeviceStatus.Retired or DeviceStatus.Disposed
            ? new DateOnly(2024, 12, 31)
            : (DateOnly?)null;
        var disposalMethod = status == DeviceStatus.Disposed ? "Recycle" : null;

        return Device.Create(
            Guid.NewGuid(),
            household,
            name,
            brandId,
            categoryId,
            ownerId,
            locationId,
            model: "Report Model",
            serialNumber: $"SN-{Guid.NewGuid():N}"[..12],
            networkId: networkId,
            purchaseDate: purchaseDate ?? new DateOnly(2024, 1, 1),
            purchasePrice: purchasePrice,
            currency: Currency.From("USD"),
            status: status,
            notes: "reporting",
            retiredDate: retiredDate,
            disposalMethod: disposalMethod,
            warrantyExpiry: warrantyExpiry);
    }
}
