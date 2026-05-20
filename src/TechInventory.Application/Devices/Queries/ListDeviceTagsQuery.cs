using MediatR;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Results;
using TechInventory.Application.Tags;

namespace TechInventory.Application.Devices.Queries;

/// <summary>
/// F030: returns the active tags attached to a single device, projected as
/// <see cref="TagResponse"/> so the frontend can render the same shape it
/// already uses for the global tag list.
/// </summary>
public sealed record ListDeviceTagsQuery(Guid DeviceId) : IRequest<Result<IReadOnlyList<TagResponse>>>;

public sealed class ListDeviceTagsQueryHandler(
    IDeviceRepository deviceRepository,
    ITagRepository tagRepository) : IRequestHandler<ListDeviceTagsQuery, Result<IReadOnlyList<TagResponse>>>
{
    public async Task<Result<IReadOnlyList<TagResponse>>> Handle(ListDeviceTagsQuery request, CancellationToken cancellationToken)
    {
        var deviceResult = await deviceRepository.GetByIdAsync(request.DeviceId, cancellationToken).ConfigureAwait(false);
        if (deviceResult.IsFailure)
        {
            return Result<IReadOnlyList<TagResponse>>.Failure(deviceResult.Error!);
        }

        var deviceTags = await deviceRepository.ListTagsAsync(request.DeviceId, cancellationToken).ConfigureAwait(false);
        if (deviceTags.Count == 0)
        {
            return Result<IReadOnlyList<TagResponse>>.Success(Array.Empty<TagResponse>());
        }

        // Tag-set size is bounded (~hundreds household-wide) so loading the
        // entire active tag catalog once and filtering in memory is cheaper
        // than N round-trips through GetByIdAsync.
        var allActiveTags = await tagRepository.ListAsync(includeInactive: false, cancellationToken).ConfigureAwait(false);
        var tagsById = allActiveTags.ToDictionary(tag => tag.Id);

        var responses = deviceTags
            .Select(deviceTag => tagsById.TryGetValue(deviceTag.TagId, out var tag) ? tag : null)
            .Where(tag => tag is not null)
            .Select(tag => TagResponse.FromEntity(tag!))
            .OrderBy(tag => tag.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Result<IReadOnlyList<TagResponse>>.Success(responses);
    }
}
