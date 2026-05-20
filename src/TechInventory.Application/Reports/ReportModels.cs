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

public sealed record InsuranceReportItem(
    string Name,
    string? Brand,
    string Category,
    string? SerialNumber,
    DateOnly? PurchaseDate,
    decimal? Price,
    string Location,
    DateOnly? WarrantyExpiry);

public sealed record InsuranceReportFileResponse(string FileName, byte[] Content);

public sealed record EraReportDecade(
    string Decade,
    int StartYear,
    int EndYear,
    int DeviceCount,
    decimal TotalValue,
    IReadOnlyList<string> SampleDevices);

public sealed record EraReportResponse(
    IReadOnlyList<EraReportDecade> Decades,
    DateOnly AsOfDate,
    Guid? AppliedCategoryId);

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
