using MediatR;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Results;

namespace TechInventory.Application.Tags.Queries;

public sealed record GetTagByIdQuery(Guid Id) : IRequest<Result<TagResponse>>;

public sealed class GetTagByIdQueryHandler(ITagRepository tagRepository) : IRequestHandler<GetTagByIdQuery, Result<TagResponse>>
{
    public async Task<Result<TagResponse>> Handle(GetTagByIdQuery request, CancellationToken cancellationToken)
    {
        var entityResult = await tagRepository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        return entityResult.IsFailure
            ? Result<TagResponse>.Failure(entityResult.Error!)
            : Result<TagResponse>.Success(TagResponse.FromEntity(entityResult.Value!));
    }
}
