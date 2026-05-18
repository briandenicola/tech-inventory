using FluentAssertions;
using NSubstitute;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Categories;
using TechInventory.Application.Categories.Commands;
using TechInventory.Application.Categories.Queries;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.UnitTests.Application;

public sealed class T23CategoryHandlerScaffoldingTests
{
    [Fact]
    public async Task CreateCategoryCommandHandler_WhenValidRootInput_ReturnsSuccess()
    {
        var deps = CreateDependencies();
        deps.Repository.GetByNameWithinParentAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Result<Category>.Failure(Error.NotFound("missing")));
        deps.Repository.AddAsync(Arg.Any<Category>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<Category>.Success(call.Arg<Category>()));
        deps.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = new CreateCategoryCommandHandler(deps.Repository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new CreateCategoryCommand("Computers"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Computers");
        result.Value.Depth.Should().Be(1);
        result.Value.ParentId.Should().BeNull();
        deps.AuditContext.Received(1).Set(Arg.Is<AuditContextEntry>(entry =>
            entry.EntityType == nameof(Category) && entry.Action == AuditAction.Created));
        await deps.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateCategoryCommandHandler_WhenValidationFails_ReturnsValidationFailure()
    {
        var result = await DeviceHandlerTestSupport.ValidateAsync(
            new CreateCategoryCommand(string.Empty),
            new CreateCategoryCommandValidator(),
            CategoryResponse.FromEntity(DeviceHandlerTestSupport.CreateCategory()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
    }

    [Fact]
    public async Task CreateCategoryCommandHandler_WhenDuplicateNameInParent_ReturnsConflictFailure()
    {
        var deps = CreateDependencies();
        var existing = DeviceHandlerTestSupport.CreateCategory();
        deps.Repository.GetByNameWithinParentAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Result<Category>.Success(existing));
        var handler = new CreateCategoryCommandHandler(deps.Repository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new CreateCategoryCommand("Computers"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Conflict");
    }

    [Fact]
    public async Task UpdateCategoryCommandHandler_WhenValidInput_ReturnsSuccessAndCapturesBeforeSnapshot()
    {
        var deps = CreateDependencies();
        var category = new Category(Guid.NewGuid(), "Computers");
        deps.Repository.GetByIdAsync(category.Id, Arg.Any<CancellationToken>()).Returns(Result<Category>.Success(category));
        deps.Repository.ListAsync(true, Arg.Any<CancellationToken>()).Returns(new[] { category });
        deps.Repository.GetByNameWithinParentAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Result<Category>.Failure(Error.NotFound("missing")));
        deps.Repository.UpdateAsync(category, Arg.Any<CancellationToken>()).Returns(Result<Category>.Success(category));
        deps.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = new UpdateCategoryCommandHandler(deps.Repository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new UpdateCategoryCommand(category.Id, "Laptops"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Laptops");
        category.Name.Should().Be("Laptops");
        deps.AuditContext.Received(1).Set(Arg.Is<AuditContextEntry>(entry =>
            entry.EntityType == nameof(Category) &&
            entry.Action == AuditAction.Updated &&
            entry.BeforePayload != null &&
            entry.BeforePayload.ToString()!.Contains("Computers", StringComparison.Ordinal)));
        await deps.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateCategoryCommandHandler_WhenValidationFails_ReturnsValidationFailure()
    {
        var result = await DeviceHandlerTestSupport.ValidateAsync(
            new UpdateCategoryCommand(Guid.Empty, string.Empty),
            new UpdateCategoryCommandValidator(),
            CategoryResponse.FromEntity(DeviceHandlerTestSupport.CreateCategory()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
    }

    [Fact]
    public async Task UpdateCategoryCommandHandler_WhenCategoryDoesNotExist_ReturnsNotFoundFailure()
    {
        var deps = CreateDependencies();
        var id = Guid.NewGuid();
        deps.Repository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(Result<Category>.Failure(Error.NotFound("missing")));
        var handler = new UpdateCategoryCommandHandler(deps.Repository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new UpdateCategoryCommand(id, "Laptops"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NotFound");
    }

    [Fact]
    public async Task UpdateCategoryCommandHandler_WhenCategoryReferencesItselfAsParent_ReturnsConflictFailure()
    {
        var deps = CreateDependencies();
        var category = new Category(Guid.NewGuid(), "Computers");
        deps.Repository.GetByIdAsync(category.Id, Arg.Any<CancellationToken>()).Returns(Result<Category>.Success(category));
        deps.Repository.ListAsync(true, Arg.Any<CancellationToken>()).Returns(new[] { category });
        var handler = new UpdateCategoryCommandHandler(deps.Repository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new UpdateCategoryCommand(category.Id, "Computers", ParentId: category.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Conflict");
    }

    [Fact]
    public async Task DeleteCategoryCommandHandler_WhenCategoryExists_DeactivatesAndCapturesBeforeSnapshot()
    {
        var deps = CreateDependencies();
        var category = new Category(Guid.NewGuid(), "Computers");
        deps.Repository.GetByIdAsync(category.Id, Arg.Any<CancellationToken>()).Returns(Result<Category>.Success(category));
        deps.Repository.ListAsync(true, Arg.Any<CancellationToken>()).Returns(new[] { category });
        deps.Repository.UpdateAsync(category, Arg.Any<CancellationToken>()).Returns(Result<Category>.Success(category));
        deps.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = new DeleteCategoryCommandHandler(deps.Repository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new DeleteCategoryCommand(category.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        category.IsActive.Should().BeFalse();
        deps.AuditContext.Received(1).Set(Arg.Is<AuditContextEntry>(entry =>
            entry.EntityType == nameof(Category) &&
            entry.Action == AuditAction.Deleted &&
            entry.BeforePayload != null &&
            entry.BeforePayload.ToString()!.Contains("Computers", StringComparison.Ordinal)));
        await deps.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteCategoryCommandHandler_WhenValidationFails_ReturnsValidationFailure()
    {
        var result = await DeviceHandlerTestSupport.ValidateAsync(new DeleteCategoryCommand(Guid.Empty), new DeleteCategoryCommandValidator());

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
    }

    [Fact]
    public async Task DeleteCategoryCommandHandler_WhenCategoryDoesNotExist_ReturnsNotFoundFailure()
    {
        var deps = CreateDependencies();
        var id = Guid.NewGuid();
        deps.Repository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(Result<Category>.Failure(Error.NotFound("missing")));
        var handler = new DeleteCategoryCommandHandler(deps.Repository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new DeleteCategoryCommand(id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NotFound");
    }

    [Fact]
    public async Task DeleteCategoryCommandHandler_WhenCategoryAlreadyInactive_ReturnsConflictFailure()
    {
        var deps = CreateDependencies();
        var category = DeviceHandlerTestSupport.CreateCategory(isActive: false);
        deps.Repository.GetByIdAsync(category.Id, Arg.Any<CancellationToken>()).Returns(Result<Category>.Success(category));
        var handler = new DeleteCategoryCommandHandler(deps.Repository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new DeleteCategoryCommand(category.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Conflict");
    }

    [Fact]
    public async Task GetCategoryByIdQueryHandler_WhenCategoryExists_ReturnsResponseWithChildren()
    {
        var deps = CreateDependencies();
        var root = new Category(Guid.NewGuid(), "Computers");
        var child = new Category(Guid.NewGuid(), "Laptops", root.Id, 2);
        deps.Repository.GetByIdAsync(root.Id, Arg.Any<CancellationToken>()).Returns(Result<Category>.Success(root));
        deps.Repository.ListAsync(true, Arg.Any<CancellationToken>()).Returns(new[] { root, child });
        var handler = new GetCategoryByIdQueryHandler(deps.Repository);

        var result = await handler.Handle(new GetCategoryByIdQuery(root.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(root.Id);
        result.Value.Children.Should().HaveCount(1);
        result.Value.Children[0].Id.Should().Be(child.Id);
        result.Value.Children[0].ParentId.Should().Be(root.Id);
    }

    [Fact]
    public async Task GetCategoryByIdQueryHandler_WhenCategoryDoesNotExist_ReturnsNotFoundFailure()
    {
        var deps = CreateDependencies();
        var id = Guid.NewGuid();
        deps.Repository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(Result<Category>.Failure(Error.NotFound("missing")));
        var handler = new GetCategoryByIdQueryHandler(deps.Repository);

        var result = await handler.Handle(new GetCategoryByIdQuery(id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NotFound");
    }

    [Fact]
    public async Task ListCategoriesQueryHandler_WhenRequested_ReturnsRootsWithNestedChildren()
    {
        var deps = CreateDependencies();
        var rootA = new Category(Guid.NewGuid(), "Computers");
        var rootB = new Category(Guid.NewGuid(), "Networking");
        var child = new Category(Guid.NewGuid(), "Laptops", rootA.Id, 2);
        var grandchild = new Category(Guid.NewGuid(), "Ultrabooks", child.Id, 3);
        deps.Repository.ListAsync(false, Arg.Any<CancellationToken>())
            .Returns(new[] { rootA, rootB, child, grandchild });
        var handler = new ListCategoriesQueryHandler(deps.Repository);

        var result = await handler.Handle(new ListCategoriesQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(2, because: "only roots count toward TotalCount");
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items.Select(item => item.Name).Should().Equal("Computers", "Networking");

        var computersNode = result.Value.Items.Single(item => item.Id == rootA.Id);
        computersNode.Children.Should().HaveCount(1);
        computersNode.Children[0].Id.Should().Be(child.Id);
        computersNode.Children[0].Children.Should().HaveCount(1);
        computersNode.Children[0].Children[0].Id.Should().Be(grandchild.Id);

        var networkingNode = result.Value.Items.Single(item => item.Id == rootB.Id);
        networkingNode.Children.Should().BeEmpty();
    }

    private static CategoryDeps CreateDependencies() => new(
        Substitute.For<ICategoryRepository>(),
        Substitute.For<IUnitOfWork>(),
        Substitute.For<IAuditContext>());

    private sealed record CategoryDeps(ICategoryRepository Repository, IUnitOfWork UnitOfWork, IAuditContext AuditContext);
}
