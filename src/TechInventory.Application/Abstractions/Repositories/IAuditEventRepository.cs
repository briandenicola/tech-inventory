using TechInventory.Application.Common.Paging;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;

namespace TechInventory.Application.Abstractions.Repositories;

public interface IAuditEventRepository
{
    Task<Result<AuditEvent>> AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken);

    Task<Result<AuditEvent>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<AuditEvent>> ListAsync(AuditEventListCriteria criteria, CancellationToken cancellationToken);
}
