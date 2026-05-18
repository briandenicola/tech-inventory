using MediatR;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Paging;
using TechInventory.Application.Common.Results;

namespace TechInventory.Application.Devices.Queries;

public sealed record ListDevicesQuery(
    int Page = 1,
    int PageSize = 25,
    string? Search = null,
    Guid? BrandId = null,
    Guid? CategoryId = null,
    Guid? OwnerId = null,
    Guid? LocationId = null,
    Guid? NetworkId = null,
    TechInventory.Domain.Enums.DeviceStatus? Status = null,
    IReadOnlyCollection<Guid>? TagIds = null,
    int? PurchaseYearFrom = null,
    int? PurchaseYearTo = null,
    string? SortBy = null,
    bool SortDescending = false) : IRequest<Result<PagedResponse<DeviceResponse>>>;

public sealed class ListDevicesQueryHandler(IDeviceRepository deviceRepository) : IRequestHandler<ListDevicesQuery, Result<PagedResponse<DeviceResponse>>>
{
    public async Task<Result<PagedResponse<DeviceResponse>>> Handle(ListDevicesQuery request, CancellationToken cancellationToken)
    {
        var pagedResult = await deviceRepository.ListAsync(
            new DeviceListCriteria(
                new PageRequest(request.Page, request.PageSize),
                request.Search,
                request.BrandId,
                request.CategoryId,
                request.OwnerId,
                request.LocationId,
                request.NetworkId,
                request.Status,
                request.TagIds,
                ToStartDate(request.PurchaseYearFrom),
                ToEndDate(request.PurchaseYearTo),
                request.SortBy,
                request.SortDescending),
            cancellationToken).ConfigureAwait(false);

        return Result<PagedResponse<DeviceResponse>>.Success(
            new PagedResponse<DeviceResponse>(
                pagedResult.Items.Select(DeviceResponse.FromEntity).ToArray(),
                pagedResult.TotalCount,
                pagedResult.Page,
                pagedResult.PageSize));
    }

    private static DateOnly? ToStartDate(int? year)
        => year.HasValue ? new DateOnly(year.Value, 1, 1) : null;

    private static DateOnly? ToEndDate(int? year)
        => year.HasValue ? new DateOnly(year.Value, 12, 31) : null;
}
