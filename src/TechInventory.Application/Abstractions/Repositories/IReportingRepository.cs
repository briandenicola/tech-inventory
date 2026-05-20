using TechInventory.Application.Reports;

namespace TechInventory.Application.Abstractions.Repositories;

public interface IReportingRepository
{
    Task<SummaryReportResponse> GetSummaryAsync(int topCategoryCount, CancellationToken cancellationToken);

    Task<IReadOnlyList<WarrantyReportItem>> GetExpiringWarrantiesAsync(DateOnly asOfDate, int expiringWithinDays, CancellationToken cancellationToken);

    Task<IReadOnlyList<EraReportDecade>> GetEraReportAsync(Guid? categoryId, CancellationToken cancellationToken);

    Task<IReadOnlyList<TimelineReportEntry>> GetTimelineReportAsync(Guid? categoryId, TimelineGroupBy groupBy, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken);

    Task<IReadOnlyList<InsuranceReportItem>> GetInsuranceReportItemsAsync(Guid? locationId, CancellationToken cancellationToken);

    Task<IReadOnlyList<SpendingReportPoint>> GetSpendingAsync(SpendingGroupBy groupBy, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken);
}
