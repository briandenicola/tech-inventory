using MediatR;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.Application.Categories.Commands;

public sealed record CreateCategoryCommand(string Name, Guid? ParentId = null, string? Icon = null) : IRequest<Result<CategoryResponse>>, IAuditable;

public sealed class CreateCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    IAuditContext auditContext) : IRequestHandler<CreateCategoryCommand, Result<CategoryResponse>>
{
    public async Task<Result<CategoryResponse>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var depthResult = await ResolveDepthAsync(request.ParentId, cancellationToken).ConfigureAwait(false);
        if (depthResult.IsFailure)
        {
            return Result<CategoryResponse>.Failure(depthResult.Error!);
        }

        var duplicateResult = await categoryRepository.GetByNameWithinParentAsync(request.Name, request.ParentId, cancellationToken).ConfigureAwait(false);
        if (duplicateResult.IsSuccess)
        {
            return Result<CategoryResponse>.Failure(Error.Conflict($"Category with name '{request.Name.Trim()}' already exists under the selected parent."));
        }

        var category = new Category(Guid.NewGuid(), request.Name, request.ParentId, depthResult.Value!, request.Icon);
        var addResult = await categoryRepository.AddAsync(category, cancellationToken).ConfigureAwait(false);
        if (addResult.IsFailure)
        {
            return Result<CategoryResponse>.Failure(addResult.Error!);
        }

        auditContext.Set(new AuditContextEntry(nameof(Category), category.Id.ToString(), AuditAction.Created));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<CategoryResponse>.Success(CategoryResponse.FromEntity(category));
    }

    private async Task<Result<int>> ResolveDepthAsync(Guid? parentId, CancellationToken cancellationToken)
    {
        if (!parentId.HasValue)
        {
            return Result<int>.Success(1);
        }

        var parentResult = await categoryRepository.GetByIdAsync(parentId.Value, cancellationToken).ConfigureAwait(false);
        if (parentResult.IsFailure)
        {
            return Result<int>.Failure(parentResult.Error!);
        }

        if (!parentResult.Value!.IsActive)
        {
            return Result<int>.Failure(Error.Conflict($"Parent category '{parentId.Value}' is inactive."));
        }

        if (parentResult.Value.Depth >= 3)
        {
            return Result<int>.Failure(Error.Conflict("Categories cannot exceed a depth of 3."));
        }

        return Result<int>.Success(parentResult.Value.Depth + 1);
    }
}
