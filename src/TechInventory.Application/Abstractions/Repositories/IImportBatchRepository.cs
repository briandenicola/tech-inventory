using TechInventory.Application.Common.Paging;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;

namespace TechInventory.Application.Abstractions.Repositories;

public interface IImportBatchRepository
{
    Task<Result<ImportBatch>> AddAsync(ImportBatch importBatch, CancellationToken cancellationToken);

    Task<Result<ImportBatch>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<ImportBatch>> ListAsync(ImportBatchListCriteria criteria, CancellationToken cancellationToken);
}
