using FluentAssertions;
using NSubstitute;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Auditing;
using TechInventory.Application.Brands;
using TechInventory.Application.Brands.Commands;
using TechInventory.Application.Brands.Queries;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.UnitTests.Application;

public sealed class T22BrandHandlerScaffoldingTests
{
    [Fact]
    public async Task CreateBrandCommandHandler_WhenValidInput_ReturnsSuccess()
    {
        var deps = CreateDependencies();
        deps.Repository.GetByNormalizedNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<Brand>.Failure(Error.NotFound("missing")));
        deps.Repository.AddAsync(Arg.Any<Brand>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<Brand>.Success(call.Arg<Brand>()));
        deps.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = new CreateBrandCommandHandler(deps.Repository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new CreateBrandCommand("Lenovo", "https://lenovo.com", "Notes"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Lenovo");
        result.Value.Website.Should().Be("https://lenovo.com");
        await deps.Repository.Received(1).AddAsync(
            Arg.Is<Brand>(b => b.Name == "Lenovo" && b.Website == "https://lenovo.com"),
            Arg.Any<CancellationToken>());
        deps.AuditContext.Received(1).Set(Arg.Is<AuditContextEntry>(entry =>
            entry.EntityType == nameof(Brand) && entry.Action == AuditAction.Created));
        await deps.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateBrandCommandHandler_WhenValidationFails_ReturnsValidationFailure()
    {
        var result = await DeviceHandlerTestSupport.ValidateAsync(
            new CreateBrandCommand(string.Empty),
            new CreateBrandCommandValidator(),
            BrandResponse.FromEntity(DeviceHandlerTestSupport.CreateBrand()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
    }

    [Fact]
    public async Task CreateBrandCommandHandler_WhenDuplicateNameExists_ReturnsConflictFailure()
    {
        var deps = CreateDependencies();
        deps.Repository.GetByNormalizedNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<Brand>.Success(DeviceHandlerTestSupport.CreateBrand()));
        var handler = new CreateBrandCommandHandler(deps.Repository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new CreateBrandCommand("Lenovo"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Conflict");
        await deps.Repository.DidNotReceive().AddAsync(Arg.Any<Brand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateBrandCommandHandler_WhenValidInput_ReturnsSuccessAndCapturesBeforeSnapshot()
    {
        var deps = CreateDependencies();
        var brand = new Brand(Guid.NewGuid(), "Lenovo");
        deps.Repository.GetByIdAsync(brand.Id, Arg.Any<CancellationToken>()).Returns(Result<Brand>.Success(brand));
        deps.Repository.GetByNormalizedNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<Brand>.Failure(Error.NotFound("missing")));
        deps.Repository.UpdateAsync(brand, Arg.Any<CancellationToken>()).Returns(Result<Brand>.Success(brand));
        deps.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = new UpdateBrandCommandHandler(deps.Repository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new UpdateBrandCommand(brand.Id, "ThinkPad", "https://thinkpad.com", "Updated"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("ThinkPad");
        brand.Name.Should().Be("ThinkPad");
        deps.AuditContext.Received(1).Set(Arg.Is<AuditContextEntry>(entry =>
            entry.EntityType == nameof(Brand) &&
            entry.Action == AuditAction.Updated &&
            entry.BeforePayload != null &&
            entry.BeforePayload.ToString()!.Contains("Lenovo", StringComparison.Ordinal)));
        await deps.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateBrandCommandHandler_WhenValidationFails_ReturnsValidationFailure()
    {
        var result = await DeviceHandlerTestSupport.ValidateAsync(
            new UpdateBrandCommand(Guid.Empty, string.Empty),
            new UpdateBrandCommandValidator(),
            BrandResponse.FromEntity(DeviceHandlerTestSupport.CreateBrand()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
    }

    [Fact]
    public async Task UpdateBrandCommandHandler_WhenBrandDoesNotExist_ReturnsNotFoundFailure()
    {
        var deps = CreateDependencies();
        var id = Guid.NewGuid();
        deps.Repository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(Result<Brand>.Failure(Error.NotFound("Brand missing.")));
        var handler = new UpdateBrandCommandHandler(deps.Repository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new UpdateBrandCommand(id, "Lenovo"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NotFound");
    }

    [Fact]
    public async Task UpdateBrandCommandHandler_WhenDuplicateNameBelongsToDifferentBrand_ReturnsConflictFailure()
    {
        var deps = CreateDependencies();
        var brand = new Brand(Guid.NewGuid(), "Lenovo");
        var other = new Brand(Guid.NewGuid(), "Apple");
        deps.Repository.GetByIdAsync(brand.Id, Arg.Any<CancellationToken>()).Returns(Result<Brand>.Success(brand));
        deps.Repository.GetByNormalizedNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<Brand>.Success(other));
        var handler = new UpdateBrandCommandHandler(deps.Repository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new UpdateBrandCommand(brand.Id, "Apple"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Conflict");
    }

    [Fact]
    public async Task DeleteBrandCommandHandler_WhenBrandExists_DeactivatesAndCapturesBeforeSnapshot()
    {
        var deps = CreateDependencies();
        var brand = new Brand(Guid.NewGuid(), "Lenovo");
        deps.Repository.GetByIdAsync(brand.Id, Arg.Any<CancellationToken>()).Returns(Result<Brand>.Success(brand));
        deps.Repository.UpdateAsync(brand, Arg.Any<CancellationToken>()).Returns(Result<Brand>.Success(brand));
        deps.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        var handler = new DeleteBrandCommandHandler(deps.Repository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new DeleteBrandCommand(brand.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        brand.IsActive.Should().BeFalse();
        deps.AuditContext.Received(1).Set(Arg.Is<AuditContextEntry>(entry =>
            entry.EntityType == nameof(Brand) &&
            entry.Action == AuditAction.Deleted &&
            entry.BeforePayload != null &&
            entry.BeforePayload.ToString()!.Contains("Lenovo", StringComparison.Ordinal)));
        await deps.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteBrandCommandHandler_WhenValidationFails_ReturnsValidationFailure()
    {
        var result = await DeviceHandlerTestSupport.ValidateAsync(new DeleteBrandCommand(Guid.Empty), new DeleteBrandCommandValidator());

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
    }

    [Fact]
    public async Task DeleteBrandCommandHandler_WhenBrandDoesNotExist_ReturnsNotFoundFailure()
    {
        var deps = CreateDependencies();
        var id = Guid.NewGuid();
        deps.Repository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(Result<Brand>.Failure(Error.NotFound("Brand missing.")));
        var handler = new DeleteBrandCommandHandler(deps.Repository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new DeleteBrandCommand(id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NotFound");
    }

    [Fact]
    public async Task DeleteBrandCommandHandler_WhenBrandAlreadyInactive_ReturnsConflictFailure()
    {
        var deps = CreateDependencies();
        var brand = DeviceHandlerTestSupport.CreateBrand(isActive: false);
        deps.Repository.GetByIdAsync(brand.Id, Arg.Any<CancellationToken>()).Returns(Result<Brand>.Success(brand));
        var handler = new DeleteBrandCommandHandler(deps.Repository, deps.UnitOfWork, deps.AuditContext);

        var result = await handler.Handle(new DeleteBrandCommand(brand.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Conflict");
    }

    [Fact]
    public async Task GetBrandByIdQueryHandler_WhenBrandExists_ReturnsSuccess()
    {
        var deps = CreateDependencies();
        var brand = DeviceHandlerTestSupport.CreateBrand();
        deps.Repository.GetByIdAsync(brand.Id, Arg.Any<CancellationToken>()).Returns(Result<Brand>.Success(brand));
        var handler = new GetBrandByIdQueryHandler(deps.Repository);

        var result = await handler.Handle(new GetBrandByIdQuery(brand.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(brand.Id);
        result.Value.Name.Should().Be(brand.Name);
    }

    [Fact]
    public async Task GetBrandByIdQueryHandler_WhenBrandDoesNotExist_ReturnsNotFoundFailure()
    {
        var deps = CreateDependencies();
        var id = Guid.NewGuid();
        deps.Repository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(Result<Brand>.Failure(Error.NotFound("missing")));
        var handler = new GetBrandByIdQueryHandler(deps.Repository);

        var result = await handler.Handle(new GetBrandByIdQuery(id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NotFound");
    }

    [Fact]
    public async Task ListBrandsQueryHandler_WhenRequested_ReturnsAllItems()
    {
        var deps = CreateDependencies();
        var brands = new[]
        {
            new Brand(Guid.NewGuid(), "Lenovo"),
            new Brand(Guid.NewGuid(), "Apple"),
        };
        deps.Repository.ListAsync(false, Arg.Any<CancellationToken>()).Returns(brands);
        var handler = new ListBrandsQueryHandler(deps.Repository);

        var result = await handler.Handle(new ListBrandsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.Items.Select(b => b.Name).Should().Contain(new[] { "Lenovo", "Apple" });
    }

    private static BrandDeps CreateDependencies() => new(
        Substitute.For<IBrandRepository>(),
        Substitute.For<IUnitOfWork>(),
        Substitute.For<IAuditContext>());

    private sealed record BrandDeps(IBrandRepository Repository, IUnitOfWork UnitOfWork, IAuditContext AuditContext);
}
