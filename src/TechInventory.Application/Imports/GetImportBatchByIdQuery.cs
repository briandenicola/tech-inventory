using FluentValidation;
using MediatR;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Results;

namespace TechInventory.Application.Imports;

public sealed record GetImportBatchByIdQuery(Guid Id) : IRequest<Result<ImportBatchDetailResponse>>;

public sealed class GetImportBatchByIdQueryHandler(IImportBatchRepository importBatchRepository)
    : IRequestHandler<GetImportBatchByIdQuery, Result<ImportBatchDetailResponse>>
{
    public async Task<Result<ImportBatchDetailResponse>> Handle(GetImportBatchByIdQuery request, CancellationToken cancellationToken)
    {
        var batchResult = await importBatchRepository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        return batchResult.IsFailure
            ? Result<ImportBatchDetailResponse>.Failure(batchResult.Error!)
            : Result<ImportBatchDetailResponse>.Success(ImportBatchDetailResponse.FromEntity(batchResult.Value!));
    }
}

public sealed class GetImportBatchByIdQueryValidator : AbstractValidator<GetImportBatchByIdQuery>
{
    public GetImportBatchByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty();
    }
}
