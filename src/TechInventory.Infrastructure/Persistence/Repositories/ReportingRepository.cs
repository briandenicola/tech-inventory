using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Reports;
using TechInventory.Domain.Enums;

namespace TechInventory.Infrastructure.Persistence.Repositories;

public sealed class ReportingRepository(AppDbContext dbContext) : IReportingRepository
{
    private const string OthersLabel = "Others";
    private readonly AppDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task<SummaryReportResponse> GetSummaryAsync(int topCategoryCount, CancellationToken cancellationToken)
    {
        var activeDevices = _dbContext.Devices
            .AsNoTracking()
            .Where(device => device.Status != DeviceStatus.Retired && device.Status != DeviceStatus.Disposed);

        var totalActiveDeviceCount = await activeDevices.CountAsync(cancellationToken).ConfigureAwait(false);
        var totalEstimatedValue = (await activeDevices
            .SumAsync(device => device.PurchasePrice, cancellationToken)
            .ConfigureAwait(false)) ?? 0m;

        var categoryCounts = await (
                from device in activeDevices
                join category in _dbContext.Categories.AsNoTracking() on device.CategoryId equals category.Id
                group device by category.Name into grouped
                orderby grouped.Count() descending, grouped.Key
                select new ReportBreakdownItem(grouped.Key, grouped.Count()))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var locationCounts = await (
                from device in activeDevices
                join location in _dbContext.Locations.AsNoTracking() on device.LocationId equals location.Id
                group device by location.Name into grouped
                orderby grouped.Count() descending, grouped.Key
                select new ReportBreakdownItem(grouped.Key, grouped.Count()))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var groupedStatuses = await _dbContext.Devices
            .AsNoTracking()
            .GroupBy(device => device.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var statusCounts = new[]
        {
            new ReportBreakdownItem(
                DeviceStatus.Active.ToString(),
                groupedStatuses.Where(item => item.Status != DeviceStatus.Retired && item.Status != DeviceStatus.Disposed).Sum(item => item.Count)),
            new ReportBreakdownItem(
                DeviceStatus.Retired.ToString(),
                groupedStatuses.Where(item => item.Status == DeviceStatus.Retired).Sum(item => item.Count)),
            new ReportBreakdownItem(
                DeviceStatus.Disposed.ToString(),
                groupedStatuses.Where(item => item.Status == DeviceStatus.Disposed).Sum(item => item.Count)),
        };

        return new SummaryReportResponse(
            totalActiveDeviceCount,
            totalEstimatedValue,
            SummarizeTopCounts(categoryCounts, topCategoryCount),
            locationCounts,
            statusCounts);
    }

    public async Task<IReadOnlyList<WarrantyReportItem>> GetExpiringWarrantiesAsync(DateOnly asOfDate, int expiringWithinDays, CancellationToken cancellationToken)
    {
        var cutoffDate = asOfDate.AddDays(expiringWithinDays);

        var devices = await (
                from device in _dbContext.Devices.AsNoTracking()
                where device.Status != DeviceStatus.Retired
                      && device.Status != DeviceStatus.Disposed
                      && device.WarrantyExpiry.HasValue
                      && device.WarrantyExpiry.Value >= asOfDate
                      && device.WarrantyExpiry.Value <= cutoffDate
                join brand in _dbContext.Brands.AsNoTracking() on device.BrandId equals brand.Id into brandGroup
                from brand in brandGroup.DefaultIfEmpty()
                orderby device.WarrantyExpiry, device.Name
                select new
                {
                    device.Name,
                    Brand = brand != null ? brand.Name : null,
                    device.PurchaseDate,
                    WarrantyExpiry = device.WarrantyExpiry!.Value,
                })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return devices
            .Select(device => new WarrantyReportItem(
                device.Name,
                device.Brand,
                device.PurchaseDate,
                device.WarrantyExpiry,
                device.WarrantyExpiry.DayNumber - asOfDate.DayNumber))
            .ToArray();
    }

    public async Task<IReadOnlyList<EraReportDecade>> GetEraReportAsync(Guid? categoryId, CancellationToken cancellationToken)
    {
        var query = _dbContext.Devices
            .AsNoTracking()
            .Where(device => device.Status != DeviceStatus.Retired
                             && device.Status != DeviceStatus.Disposed
                             && device.PurchaseDate.HasValue);

        if (categoryId.HasValue)
        {
            query = query.Where(device => device.CategoryId == categoryId.Value);
        }

        var purchasedDevices = await query
            .Select(device => new EraReportDeviceProjection(
                device.Name,
                device.PurchaseDate!.Value.Year,
                device.PurchasePrice))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return purchasedDevices
            .GroupBy(device => GetDecadeStartYear(device.PurchaseYear))
            .OrderByDescending(group => group.Key)
            .Select(group => new EraReportDecade(
                $"{group.Key}s",
                group.Key,
                group.Key + 9,
                group.Count(),
                group.Sum(device => device.PurchasePrice ?? 0m),
                group.OrderByDescending(device => device.PurchaseYear)
                    .ThenBy(device => device.Name)
                    .Select(device => device.Name)
                    .Take(3)
                    .ToArray()))
            .ToArray();
    }

    public async Task<IReadOnlyList<InsuranceReportItem>> GetInsuranceReportItemsAsync(Guid? locationId, CancellationToken cancellationToken)
    {
        var activeDevices = _dbContext.Devices
            .AsNoTracking()
            .Where(device => device.Status != DeviceStatus.Retired && device.Status != DeviceStatus.Disposed);

        if (locationId.HasValue)
        {
            activeDevices = activeDevices.Where(device => device.LocationId == locationId.Value);
        }

        return await (
                from device in activeDevices
                join category in _dbContext.Categories.AsNoTracking() on device.CategoryId equals category.Id
                join location in _dbContext.Locations.AsNoTracking() on device.LocationId equals location.Id
                join brand in _dbContext.Brands.AsNoTracking() on device.BrandId equals brand.Id into brandGroup
                from brand in brandGroup.DefaultIfEmpty()
                orderby location.Name, device.Name
                select new InsuranceReportItem(
                    device.Name,
                    brand != null ? brand.Name : null,
                    category.Name,
                    device.SerialNumber,
                    device.PurchaseDate,
                    device.PurchasePrice,
                    location.Name,
                    device.WarrantyExpiry))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<SpendingReportPoint>> GetSpendingAsync(SpendingGroupBy groupBy, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken)
    {
        var query = _dbContext.Devices
            .AsNoTracking()
            .Where(device => device.PurchaseDate.HasValue && device.PurchasePrice.HasValue);

        if (fromDate.HasValue)
        {
            query = query.Where(device => device.PurchaseDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(device => device.PurchaseDate <= toDate.Value);
        }

        var purchases = await query
            .Select(device => new
            {
                PurchaseDate = device.PurchaseDate!.Value,
                PurchasePrice = device.PurchasePrice!.Value,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return groupBy switch
        {
            SpendingGroupBy.Year => purchases
                .GroupBy(purchase => purchase.PurchaseDate.Year)
                .OrderBy(group => group.Key)
                .Select(group => new SpendingReportPoint(
                    group.Key.ToString(CultureInfo.InvariantCulture),
                    group.Sum(item => item.PurchasePrice),
                    group.Count()))
                .ToArray(),
            _ => purchases
                .GroupBy(purchase => new { purchase.PurchaseDate.Year, purchase.PurchaseDate.Month })
                .OrderBy(group => group.Key.Year)
                .ThenBy(group => group.Key.Month)
                .Select(group => new SpendingReportPoint(
                    FormattableString.Invariant($"{group.Key.Year:D4}-{group.Key.Month:D2}"),
                    group.Sum(item => item.PurchasePrice),
                    group.Count()))
                .ToArray(),
        };
    }

    private static IReadOnlyList<ReportBreakdownItem> SummarizeTopCounts(IReadOnlyList<ReportBreakdownItem> counts, int topCategoryCount)
    {
        if (counts.Count <= topCategoryCount)
        {
            return counts;
        }

        var topCounts = counts.Take(topCategoryCount).ToList();
        var otherCount = counts.Skip(topCategoryCount).Sum(item => item.Count);
        if (otherCount > 0)
        {
            topCounts.Add(new ReportBreakdownItem(OthersLabel, otherCount));
        }

        return topCounts;
    }

    private static int GetDecadeStartYear(int year) => year / 10 * 10;

    private sealed record EraReportDeviceProjection(string Name, int PurchaseYear, decimal? PurchasePrice);
}
