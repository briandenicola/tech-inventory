using System.Text.Json;
using MediatR;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Abstractions.Services;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;

namespace TechInventory.Application.Behaviors;

public sealed class AuditBehavior<TRequest, TResponse>(
    IAuditEventRepository auditEventRepository,
    IUnitOfWork unitOfWork,
    IAuditContext auditContext,
    ICurrentUserService currentUserService)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IAuditEventRepository _auditEventRepository = auditEventRepository ?? throw new ArgumentNullException(nameof(auditEventRepository));
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly IAuditContext _auditContext = auditContext ?? throw new ArgumentNullException(nameof(auditContext));
    private readonly ICurrentUserService _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not IAuditable)
        {
            return await next().ConfigureAwait(false);
        }

        try
        {
            var response = await next().ConfigureAwait(false);
            if (response.IsFailure)
            {
                return response;
            }

            foreach (var entry in _auditContext.Entries)
            {
                var auditEvent = new AuditEvent(
                    Guid.NewGuid(),
                    string.IsNullOrWhiteSpace(entry.Actor) ? _currentUserService.GetCurrentUserId() : entry.Actor,
                    entry.EntityType,
                    entry.EntityId,
                    entry.Action,
                    JsonSerializer.Serialize(entry.BeforePayload, SerializerOptions),
                    JsonSerializer.Serialize(entry.AfterPayload ?? request, SerializerOptions));

                var appendResult = await _auditEventRepository.AppendAsync(auditEvent, cancellationToken).ConfigureAwait(false);
                if (appendResult.IsFailure)
                {
                    return ResultFactory.CreateFailure<TResponse>(appendResult.Error!);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return response;
        }
        finally
        {
            _auditContext.Clear();
        }
    }
}
