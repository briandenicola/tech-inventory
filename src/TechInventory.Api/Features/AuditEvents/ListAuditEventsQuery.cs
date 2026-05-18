using FluentValidation;
using MediatR;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Paging;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Api.Features.AuditEvents;

public sealed record ListAuditEventsQuery(
    int Page = 1,
    int PageSize = 25,
    string? EntityType = null,
    string? EntityId = null,
    AuditAction? Action = null,
    string? Actor = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null) : IRequest<Result<PagedResponse<AuditEventResponse>>>;

public sealed class ListAuditEventsQueryValidator : AbstractValidator<ListAuditEventsQuery>
{
    public ListAuditEventsQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThan(0);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 200);

        RuleFor(query => query)
            .Must(query => !query.From.HasValue || !query.To.HasValue || query.From <= query.To)
            .WithMessage("From cannot be later than To.");
    }
}

public sealed class ListAuditEventsQueryHandler(IAuditEventRepository auditEventRepository) : IRequestHandler<ListAuditEventsQuery, Result<PagedResponse<AuditEventResponse>>>
{
    public async Task<Result<PagedResponse<AuditEventResponse>>> Handle(ListAuditEventsQuery request, CancellationToken cancellationToken)
    {
        var pagedResult = await auditEventRepository.ListAsync(
            new AuditEventListCriteria(
                new PageRequest(request.Page, request.PageSize),
                request.EntityType,
                request.EntityId,
                request.Action,
                request.Actor,
                request.From,
                request.To),
            cancellationToken);

        var response = new PagedResponse<AuditEventResponse>(
            pagedResult.Items.Select(AuditEventResponse.FromEntity).ToArray(),
            pagedResult.TotalCount,
            pagedResult.Page,
            pagedResult.PageSize);

        return Result<PagedResponse<AuditEventResponse>>.Success(response);
    }
}

public sealed record AuditEventResponse(
    Guid Id,
    string Actor,
    string EntityType,
    string EntityId,
    string Action,
    DateTimeOffset Timestamp,
    string BeforePayload,
    string AfterPayload)
{
    public static AuditEventResponse FromEntity(AuditEvent auditEvent)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

        return new AuditEventResponse(
            auditEvent.Id,
            auditEvent.Actor,
            auditEvent.EntityType,
            auditEvent.EntityId,
            auditEvent.Action.ToString(),
            auditEvent.Timestamp,
            auditEvent.BeforePayload,
            auditEvent.AfterPayload);
    }
}
