using FluentValidation;
using MediatR;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Paging;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Imports;

public sealed record ListImportBatchesQuery(
    int Page = 1,
    int PageSize = 25,
    ImportStatus? Status = null,
    DateTimeOffset? CreatedAfter = null,
    DateTimeOffset? CreatedBefore = null) : IRequest<Result<PagedResponse<ImportBatchSummaryResponse>>>;

public sealed class ListImportBatchesQueryHandler(IImportBatchRepository importBatchRepository)
    : IRequestHandler<ListImportBatchesQuery, Result<PagedResponse<ImportBatchSummaryResponse>>>
{
    public async Task<Result<PagedResponse<ImportBatchSummaryResponse>>> Handle(ListImportBatchesQuery request, CancellationToken cancellationToken)
    {
        var batches = await importBatchRepository.ListAsync(
            new ImportBatchListCriteria(
                new PageRequest(request.Page, request.PageSize),
                request.Status,
                request.CreatedAfter,
                request.CreatedBefore),
            cancellationToken).ConfigureAwait(false);

        return Result<PagedResponse<ImportBatchSummaryResponse>>.Success(
            new PagedResponse<ImportBatchSummaryResponse>(
                batches.Items.Select(ImportBatchSummaryResponse.FromEntity).ToArray(),
                batches.TotalCount,
                batches.Page,
                batches.PageSize));
    }
}

public sealed class ListImportBatchesQueryValidator : AbstractValidator<ListImportBatchesQuery>
{
    public ListImportBatchesQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThan(0);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 200);

        RuleFor(query => query)
            .Must(query => !query.CreatedAfter.HasValue || !query.CreatedBefore.HasValue || query.CreatedAfter <= query.CreatedBefore)
            .WithMessage("CreatedAfter cannot be later than CreatedBefore.");
    }
}
