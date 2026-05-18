using Microsoft.EntityFrameworkCore;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Paging;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;

namespace TechInventory.Infrastructure.Persistence.Repositories;

public sealed class ImportBatchRepository(AppDbContext dbContext) : IImportBatchRepository
{
    private readonly AppDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task<Result<ImportBatch>> AddAsync(ImportBatch importBatch, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(importBatch);

        await _dbContext.ImportBatches.AddAsync(importBatch, cancellationToken).ConfigureAwait(false);
        return Result<ImportBatch>.Success(importBatch);
    }

    public async Task<Result<ImportBatch>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var importBatch = _dbContext.ImportBatches.Local.FirstOrDefault(entity => entity.Id == id)
            ?? await _dbContext.ImportBatches
                .SingleOrDefaultAsync(entity => entity.Id == id, cancellationToken)
                .ConfigureAwait(false);

        return importBatch is null
            ? Result<ImportBatch>.Failure(new Error("NotFound", $"ImportBatch '{id}' was not found."))
            : Result<ImportBatch>.Success(importBatch);
    }

    public async Task<PagedResult<ImportBatch>> ListAsync(ImportBatchListCriteria criteria, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        IQueryable<ImportBatch> query = _dbContext.ImportBatches.AsNoTracking();

        if (criteria.Status.HasValue)
        {
            query = query.Where(entity => entity.Status == criteria.Status.Value);
        }

        if (criteria.CreatedAfter.HasValue)
        {
            query = query.Where(entity => entity.CreatedAt >= criteria.CreatedAfter.Value);
        }

        if (criteria.CreatedBefore.HasValue)
        {
            query = query.Where(entity => entity.CreatedAt <= criteria.CreatedBefore.Value);
        }

        var merged = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        var filteredLocal = _dbContext.ImportBatches.Local.Where(entity => MatchesCriteria(entity, criteria));
        var items = merged
            .Concat(filteredLocal)
            .GroupBy(entity => entity.Id)
            .Select(group => group.Last())
            .OrderByDescending(entity => entity.CreatedAt)
            .ThenByDescending(entity => entity.Id)
            .ToArray();

        var pagedItems = items
            .Skip((criteria.PageRequest.Page - 1) * criteria.PageRequest.PageSize)
            .Take(criteria.PageRequest.PageSize)
            .ToArray();

        return new PagedResult<ImportBatch>(pagedItems, items.Length, criteria.PageRequest.Page, criteria.PageRequest.PageSize);
    }

    private static bool MatchesCriteria(ImportBatch importBatch, ImportBatchListCriteria criteria)
    {
        return (!criteria.Status.HasValue || importBatch.Status == criteria.Status.Value)
            && (!criteria.CreatedAfter.HasValue || importBatch.CreatedAt >= criteria.CreatedAfter.Value)
            && (!criteria.CreatedBefore.HasValue || importBatch.CreatedAt <= criteria.CreatedBefore.Value);
    }
}
