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

    [Fact]
    public async Task GetEras_WhenDataExists_ReturnsSortedDecadeBuckets()
    {
        await ResetDatabaseAsync();
        var references = await SeedDeviceReferenceDataAsync();
        var secondCategory = new Category(Guid.NewGuid(), "Wearables");
        await SeedAsync(entities: [secondCategory]);
        await SeedAsync(
            entities:
            [
                CreateReportDevice(references.Household, references.Brand.Id, references.Category.Id, references.Owner.Id, references.Location.Id, references.Network.Id, "Future Console", DeviceStatus.Active, 1500m, purchaseDate: new DateOnly(2032, 7, 1)),
                CreateReportDevice(references.Household, references.Brand.Id, references.Category.Id, references.Owner.Id, references.Location.Id, references.Network.Id, "Phone 2026", DeviceStatus.Lent, 900m, purchaseDate: new DateOnly(2026, 5, 1)),
                CreateReportDevice(references.Household, references.Brand.Id, references.Category.Id, references.Owner.Id, references.Location.Id, references.Network.Id, "Laptop 2024", DeviceStatus.Active, 1200m, purchaseDate: new DateOnly(2024, 3, 1)),
                CreateReportDevice(references.Household, references.Brand.Id, secondCategory.Id, references.Owner.Id, references.Location.Id, references.Network.Id, "Watch 2021", DeviceStatus.Active, 100m, purchaseDate: new DateOnly(2021, 11, 1)),
                CreateReportDevice(references.Household, references.Brand.Id, references.Category.Id, references.Owner.Id, references.Location.Id, references.Network.Id, "Tablet 2020", DeviceStatus.InRepair, 400m, purchaseDate: new DateOnly(2020, 1, 1)),
                CreateReportDevice(references.Household, references.Brand.Id, references.Category.Id, references.Owner.Id, references.Location.Id, references.Network.Id, "Vintage Stereo", DeviceStatus.Active, 300m, purchaseDate: new DateOnly(1979, 6, 1)),
                CreateReportDevice(references.Household, references.Brand.Id, references.Category.Id, references.Owner.Id, references.Location.Id, references.Network.Id, "No Purchase Date", DeviceStatus.Active, 250m, purchaseDate: null, useDefaultPurchaseDate: false),
                CreateReportDevice(references.Household, references.Brand.Id, references.Category.Id, references.Owner.Id, references.Location.Id, references.Network.Id, "Retired 2022", DeviceStatus.Retired, 999m, purchaseDate: new DateOnly(2022, 4, 1))
            ]);
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/reports/eras");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadJsonAsync<EraReportResponse>(response);
        payload.AppliedCategoryId.Should().BeNull();
        payload.AsOfDate.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));
        payload.Decades.Select(decade => decade.Decade).Should().Equal("2030s", "2020s", "1970s");
        payload.Decades[0].Should().BeEquivalentTo(new EraReportDecade("2030s", 2030, 2039, 1, 1500m, ["Future Console"]));
        payload.Decades[1].Should().BeEquivalentTo(new EraReportDecade("2020s", 2020, 2029, 4, 2600m, ["Phone 2026", "Laptop 2024", "Watch 2021"]));
        payload.Decades[2].Should().BeEquivalentTo(new EraReportDecade("1970s", 1970, 1979, 1, 300m, ["Vintage Stereo"]));
    }

    [Fact]
    public async Task GetEras_WhenFilteredByCategory_ReturnsMatchingDecadesOnly()
    {
        await ResetDatabaseAsync();
        var references = await SeedDeviceReferenceDataAsync();
        var secondCategory = new Category(Guid.NewGuid(), "Phones");
        await SeedAsync(entities: [secondCategory]);
        await SeedAsync(
            entities:
            [
                CreateReportDevice(references.Household, references.Brand.Id, references.Category.Id, references.Owner.Id, references.Location.Id, references.Network.Id, "Included Laptop", DeviceStatus.Active, 1000m, purchaseDate: new DateOnly(2024, 3, 1)),
                CreateReportDevice(references.Household, references.Brand.Id, references.Category.Id, references.Owner.Id, references.Location.Id, references.Network.Id, "Included Tablet", DeviceStatus.Active, 500m, purchaseDate: new DateOnly(2016, 8, 1)),
                CreateReportDevice(references.Household, references.Brand.Id, secondCategory.Id, references.Owner.Id, references.Location.Id, references.Network.Id, "Excluded Phone", DeviceStatus.Active, 700m, purchaseDate: new DateOnly(2025, 1, 1))
            ]);
        using var client = CreateClient();

        var response = await client.GetAsync($"/api/v1/reports/eras?categoryId={references.Category.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadJsonAsync<EraReportResponse>(response);
        payload.AppliedCategoryId.Should().Be(references.Category.Id);
        payload.Decades.Should().BeEquivalentTo(
            [
                new EraReportDecade("2020s", 2020, 2029, 1, 1000m, ["Included Laptop"]),
                new EraReportDecade("2010s", 2010, 2019, 1, 500m, ["Included Tablet"])
            ]);
    }

    [Fact]
    public async Task GetEras_WhenCategoryIdEmpty_Returns400ValidationProblemDetails()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/reports/eras?categoryId=00000000-0000-0000-0000-000000000000");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await ReadValidationProblemDetailsAsync(response);
        problem.Errors.Keys.Should().Contain(key => string.Equals(key, "CategoryId", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetInsurance_WhenFilteredByLocation_ReturnsCsvAttachmentForActiveDevicesOnly()
    {
        await ResetDatabaseAsync();
        var references = await SeedDeviceReferenceDataAsync();
        var secondLocation = new Location(Guid.NewGuid(), "Workshop", LocationType.Home);
        await SeedAsync(
            entities:
            [
                secondLocation,
                CreateReportDevice(references.Household, references.Brand.Id, references.Category.Id, references.Owner.Id, references.Location.Id, references.Network.Id, "Covered", DeviceStatus.Active, 500m, purchaseDate: new DateOnly(2024, 2, 1), warrantyExpiry: new DateOnly(2026, 2, 1)),
                CreateReportDevice(references.Household, references.Brand.Id, references.Category.Id, references.Owner.Id, secondLocation.Id, references.Network.Id, "Other Location", DeviceStatus.Active, 250m, purchaseDate: new DateOnly(2024, 3, 1)),
                CreateReportDevice(references.Household, references.Brand.Id, references.Category.Id, references.Owner.Id, references.Location.Id, references.Network.Id, "Retired", DeviceStatus.Retired, 999m, purchaseDate: new DateOnly(2024, 4, 1))
            ]);
        using var client = CreateClient();

        var response = await client.GetAsync($"/api/v1/reports/insurance?locationId={references.Location.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        response.Content.Headers.ContentDisposition.Should().NotBeNull();
        response.Content.Headers.ContentDisposition!.DispositionType.Should().Be("attachment");
        response.Content.Headers.ContentDisposition.FileName.Should().Contain("insurance-report-");
        var csv = await response.Content.ReadAsStringAsync();
        csv.Should().Contain("# Insurance Inventory Report - Generated ");
        csv.Should().Contain("Name,Brand,Category,Serial Number,Purchase Date,Price,Location,Warranty Expiry");
        csv.Should().Contain("Covered,");
        csv.Should().NotContain("Other Location");
        csv.Should().NotContain("Retired,");
        csv.Should().Contain("TOTAL,,,,,500.00,,");
    }

    [Fact]
    public async Task GetInsurance_WhenLocationIdEmpty_Returns400ValidationProblemDetails()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/reports/insurance?locationId=00000000-0000-0000-0000-000000000000");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await ReadValidationProblemDetailsAsync(response);
        problem.Errors.Keys.Should().Contain(key => string.Equals(key, "LocationId", StringComparison.OrdinalIgnoreCase));
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
        DateOnly? warrantyExpiry = null,
        bool useDefaultPurchaseDate = true)
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
            purchaseDate: purchaseDate ?? (useDefaultPurchaseDate ? new DateOnly(2024, 1, 1) : null),
            purchasePrice: purchasePrice,
            currency: Currency.From("USD"),
            status: status,
            notes: "reporting",
            retiredDate: retiredDate,
            disposalMethod: disposalMethod,
            warrantyExpiry: warrantyExpiry);
    }
}
