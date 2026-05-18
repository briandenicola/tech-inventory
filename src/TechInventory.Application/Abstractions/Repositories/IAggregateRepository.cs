using TechInventory.Application.Common.Results;

namespace TechInventory.Application.Abstractions.Repositories;

public interface IAggregateRepository<TAggregate>
    where TAggregate : class
{
    Task<Result<TAggregate>> AddAsync(TAggregate aggregate, CancellationToken cancellationToken);

    Task<Result<TAggregate>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Result<TAggregate>> UpdateAsync(TAggregate aggregate, CancellationToken cancellationToken);
}
