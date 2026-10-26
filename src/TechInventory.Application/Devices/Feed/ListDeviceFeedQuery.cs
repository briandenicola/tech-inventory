using System.Globalization;
using FluentValidation;
using MediatR;
using TechInventory.Application.Common.Paging;
using TechInventory.Application.Common.Results;

namespace TechInventory.Application.Devices.Feed;

/// <summary>
/// A feed page. <paramref name="Page"/> is the <c>next</c> token of the previous page,
/// or empty for the first; it is a page number, kept opaque to the reader.
/// </summary>
public sealed record ListDeviceFeedQuery(string? Page = null, int Limit = 25) : IRequest<Result<DeviceFeedResponse>>;

public sealed class ListDeviceFeedQueryHandler(IDeviceFeedReader reader) : IRequestHandler<ListDeviceFeedQuery, Result<DeviceFeedResponse>>
{
    public async Task<Result<DeviceFeedResponse>> Handle(ListDeviceFeedQuery request, CancellationToken cancellationToken)
    {
        var page = ListDeviceFeedQueryValidator.ParsePage(request.Page) ?? 1;
        var result = await reader.ReadPageAsync(new PageRequest(page, request.Limit), cancellationToken).ConfigureAwait(false);

        var records = result.Items
            .Select(item => new DeviceFeedRecord("device", item.Id.ToString(), item.ModifiedAt, item))
            .ToArray();
        var next = (long)page * request.Limit < result.TotalCount
            ? (page + 1).ToString(CultureInfo.InvariantCulture)
            : null;

        return Result<DeviceFeedResponse>.Success(new DeviceFeedResponse(DeviceFeedResponse.SchemaName, records, next));
    }
}

public sealed class ListDeviceFeedQueryValidator : AbstractValidator<ListDeviceFeedQuery>
{
    public ListDeviceFeedQueryValidator()
    {
        RuleFor(query => query.Page)
            .Must(page => string.IsNullOrEmpty(page) || ParsePage(page) is not null)
            .WithMessage("Page must be the 'next' value of a previous page. Leave it out to start from the beginning.");

        RuleFor(query => query.Limit)
            .InclusiveBetween(1, 200);
    }

    internal static int? ParsePage(string? page)
        => int.TryParse(page, NumberStyles.None, CultureInfo.InvariantCulture, out var number) && number >= 1 ? number : null;
}
