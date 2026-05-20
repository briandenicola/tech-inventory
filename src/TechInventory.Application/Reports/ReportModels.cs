namespace TechInventory.Application.Reports;

public sealed record ReportBreakdownItem(string Label, int Count);

public sealed record SummaryReportResponse(
    int TotalActiveDeviceCount,
    decimal TotalEstimatedValue,
    IReadOnlyList<ReportBreakdownItem> DevicesByCategory,
    IReadOnlyList<ReportBreakdownItem> DevicesByLocation,
    IReadOnlyList<ReportBreakdownItem> DevicesByStatus);

public sealed record WarrantyReportItem(
    string DeviceName,
    string? Brand,
    DateOnly? PurchaseDate,
    DateOnly WarrantyExpiry,
    int DaysRemaining);

public sealed record WarrantyReportResponse(
    DateOnly AsOfDate,
    int ExpiringWithinDays,
    IReadOnlyList<WarrantyReportItem> Devices);

public enum SpendingGroupBy
{
    Month = 1,
    Year = 2,
}

public sealed record SpendingReportPoint(string PeriodLabel, decimal TotalSpend, int DeviceCount);

public sealed record SpendingReportResponse(
    SpendingGroupBy GroupBy,
    DateOnly? FromDate,
    DateOnly? ToDate,
    IReadOnlyList<SpendingReportPoint> Periods);
