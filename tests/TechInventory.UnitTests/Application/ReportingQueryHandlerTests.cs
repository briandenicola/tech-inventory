using System.Text;
using FluentAssertions;
using NSubstitute;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Results;
using TechInventory.Application.Reports;
using TechInventory.Application.Reports.Queries;

namespace TechInventory.UnitTests.Application;

public sealed class ReportingQueryHandlerTests
{
    [Fact]
    public async Task GetSummaryReportQueryHandler_WhenCalled_ReturnsRepositoryResponse()
    {
        var repository = Substitute.For<IReportingRepository>();
        var response = new SummaryReportResponse(
            4,
            1234.56m,
            [new ReportBreakdownItem("Computers", 3)],
            [new ReportBreakdownItem("Office", 4)],
            [new ReportBreakdownItem("Active", 4), new ReportBreakdownItem("Retired", 0), new ReportBreakdownItem("Disposed", 0)]);
        repository.GetSummaryAsync(5, Arg.Any<CancellationToken>()).Returns(response);
        var handler = new GetSummaryReportQueryHandler(repository);

        var result = await handler.Handle(new GetSummaryReportQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(response);
        await repository.Received(1).GetSummaryAsync(5, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetWarrantyReportQueryHandler_WhenCalled_ReturnsResponseWithWindowMetadata()
    {
        var repository = Substitute.For<IReportingRepository>();
        var items = new[]
        {
            new WarrantyReportItem("Laptop", "Lenovo", new DateOnly(2024, 1, 15), new DateOnly(2026, 6, 1), 12),
        };
        repository.GetExpiringWarrantiesAsync(Arg.Any<DateOnly>(), 30, Arg.Any<CancellationToken>()).Returns(items);
        var handler = new GetWarrantyReportQueryHandler(repository);

        var result = await handler.Handle(new GetWarrantyReportQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ExpiringWithinDays.Should().Be(30);
        result.Value.Devices.Should().BeEquivalentTo(items);
        await repository.Received(1).GetExpiringWarrantiesAsync(Arg.Any<DateOnly>(), 30, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetWarrantyReportQuery_WhenValidationFails_ReturnsValidationFailure()
    {
        var query = new GetWarrantyReportQuery(366);

        var result = await DeviceHandlerTestSupport.ValidateAsync(
            query,
            new GetWarrantyReportQueryValidator(),
            new WarrantyReportResponse(DateOnly.FromDateTime(DateTime.UtcNow), 30, []));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
    }

    [Fact]
    public async Task GetSpendingReportQueryHandler_WhenCalled_ReturnsRepositoryPeriods()
    {
        var repository = Substitute.For<IReportingRepository>();
        var periods = new[]
        {
            new SpendingReportPoint("2024-01", 250m, 2),
            new SpendingReportPoint("2024-02", 125m, 1),
        };
        repository.GetSpendingAsync(SpendingGroupBy.Month, new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31), Arg.Any<CancellationToken>())
            .Returns(periods);
        var handler = new GetSpendingReportQueryHandler(repository);
        var query = new GetSpendingReportQuery(SpendingGroupBy.Month, new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31));

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Periods.Should().BeEquivalentTo(periods);
    }

    [Fact]
    public async Task GetEraReportQueryHandler_WhenCalled_ReturnsRepositoryResponseWithMetadata()
    {
        var repository = Substitute.For<IReportingRepository>();
        var now = new DateTimeOffset(2026, 5, 20, 14, 30, 0, TimeSpan.Zero);
        var categoryId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var decades = new[]
        {
            new EraReportDecade("2020s", 2020, 2029, 2, 3450m, ["iPhone 14 Pro", "MacBook Pro 14"])
        };
        repository.GetEraReportAsync(categoryId, Arg.Any<CancellationToken>()).Returns(decades);
        var handler = new GetEraReportQueryHandler(repository, new FixedTimeProvider(now));
        var query = new GetEraReportQuery(categoryId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(new EraReportResponse(decades, new DateOnly(2026, 5, 20), categoryId));
        await repository.Received(1).GetEraReportAsync(query.CategoryId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetTimelineReportQueryHandler_WhenCalled_ReturnsRepositoryResponseWithMetadata()
    {
        var repository = Substitute.For<IReportingRepository>();
        var now = new DateTimeOffset(2026, 5, 20, 14, 30, 0, TimeSpan.Zero);
        var categoryId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var entries = new[]
        {
            new TimelineReportEntry("Laptop", "Lenovo", new DateOnly(2020, 4, 1), null, "Brian", 1200m),
            new TimelineReportEntry("Phone", "Apple", new DateOnly(2022, 9, 16), new DateOnly(2024, 12, 31), "Casey", 999m),
        };
        repository.GetTimelineReportAsync(categoryId, TimelineGroupBy.Owner, new DateOnly(2020, 1, 1), new DateOnly(2024, 12, 31), Arg.Any<CancellationToken>())
            .Returns(entries);
        var handler = new GetTimelineReportQueryHandler(repository, new FixedTimeProvider(now));
        var query = new GetTimelineReportQuery(categoryId, "Owner", new DateOnly(2020, 1, 1), new DateOnly(2024, 12, 31));

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(new TimelineReportResponse(entries, new DateOnly(2026, 5, 20), TimelineGroupBy.Owner, categoryId));
        await repository.Received(1).GetTimelineReportAsync(query.CategoryId, TimelineGroupBy.Owner, query.FromDate, query.ToDate, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetTimelineReportQueryHandler_WhenCategoryIdNull_ReturnsResponseWithNullAppliedCategoryId()
    {
        var repository = Substitute.For<IReportingRepository>();
        var now = new DateTimeOffset(2026, 5, 20, 14, 30, 0, TimeSpan.Zero);
        var entries = new[]
        {
            new TimelineReportEntry("Tablet", null, new DateOnly(2018, 6, 1), null, "Computers", 499m),
        };
        repository.GetTimelineReportAsync(null, TimelineGroupBy.Category, null, null, Arg.Any<CancellationToken>())
            .Returns(entries);
        var handler = new GetTimelineReportQueryHandler(repository, new FixedTimeProvider(now));

        var result = await handler.Handle(new GetTimelineReportQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AppliedCategoryId.Should().BeNull();
        result.Value.GroupBy.Should().Be(TimelineGroupBy.Category);
        result.Value.Entries.Should().BeEquivalentTo(entries);
    }

    [Fact]
    public async Task GetTimelineReportQueryHandler_WhenRepositoryReturnsNoEntries_ReturnsEmptyResponse()
    {
        var repository = Substitute.For<IReportingRepository>();
        var now = new DateTimeOffset(2026, 5, 20, 14, 30, 0, TimeSpan.Zero);
        repository.GetTimelineReportAsync(null, TimelineGroupBy.Category, null, null, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TimelineReportEntry>());
        var handler = new GetTimelineReportQueryHandler(repository, new FixedTimeProvider(now));

        var result = await handler.Handle(new GetTimelineReportQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Entries.Should().BeEmpty();
        result.Value.AsOfDate.Should().Be(new DateOnly(2026, 5, 20));
    }

    [Fact]
    public async Task GetInsuranceReportQueryHandler_WhenCalled_ReturnsCsvWithHeaderAndFooter()
    {
        var repository = Substitute.For<IReportingRepository>();
        var now = new DateTimeOffset(2026, 5, 20, 14, 30, 0, TimeSpan.Zero);
        repository.GetInsuranceReportItemsAsync(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), Arg.Any<CancellationToken>())
            .Returns(
            [
                new InsuranceReportItem("Laptop", "Lenovo", "Computers", "SN-001", new DateOnly(2024, 1, 15), 1200m, "Office", new DateOnly(2027, 1, 15)),
                new InsuranceReportItem("Speaker", null, "Audio", null, null, null, "Living Room", null)
            ]);
        var handler = new GetInsuranceReportQueryHandler(
            repository,
            new FixedTimeProvider(now));
        var query = new GetInsuranceReportQuery(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.FileName.Should().Be("insurance-report-2026-05-20.csv");
        var csv = Encoding.UTF8.GetString(result.Value.Content);
        csv.Should().Contain("# Insurance Inventory Report - Generated 2026-05-20 14:30:00 UTC");
        csv.Should().Contain("Name,Brand,Category,Serial Number,Purchase Date,Price,Location,Warranty Expiry");
        csv.Should().Contain("Laptop,Lenovo,Computers,SN-001,2024-01-15,1200.00,Office,2027-01-15");
        csv.Should().Contain("Speaker,,Audio,,,,Living Room,");
        csv.Should().Contain("TOTAL,,,,,1200.00,,");
        await repository.Received(1).GetInsuranceReportItemsAsync(query.LocationId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetInsuranceReportQuery_WhenLocationIdEmpty_ReturnsValidationFailure()
    {
        var query = new GetInsuranceReportQuery(Guid.Empty);

        var result = await DeviceHandlerTestSupport.ValidateAsync(
            query,
            new GetInsuranceReportQueryValidator(),
            new InsuranceReportFileResponse("insurance-report-2026-05-20.csv", []));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
    }

    [Fact]
    public async Task GetEraReportQuery_WhenCategoryIdEmpty_ReturnsValidationFailure()
    {
        var query = new GetEraReportQuery(Guid.Empty);

        var result = await DeviceHandlerTestSupport.ValidateAsync(
            query,
            new GetEraReportQueryValidator(),
            new EraReportResponse([], new DateOnly(2026, 5, 20), null));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
    }

    [Fact]
    public async Task GetTimelineReportQuery_WhenGroupByInvalid_ReturnsValidationFailure()
    {
        var query = new GetTimelineReportQuery(null, "Household", null, null);

        var result = await DeviceHandlerTestSupport.ValidateAsync(
            query,
            new GetTimelineReportQueryValidator(),
            new TimelineReportResponse([], new DateOnly(2026, 5, 20), TimelineGroupBy.Category, null));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
    }

    [Fact]
    public async Task GetSpendingReportQuery_WhenDateRangeInvalid_ReturnsValidationFailure()
    {
        var query = new GetSpendingReportQuery(SpendingGroupBy.Year, new DateOnly(2025, 1, 1), new DateOnly(2024, 12, 31));

        var result = await DeviceHandlerTestSupport.ValidateAsync(
            query,
            new GetSpendingReportQueryValidator(),
            new SpendingReportResponse(SpendingGroupBy.Year, query.FromDate, query.ToDate, []));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
