using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Domain.Entities;

namespace TechInventory.IntegrationTests.Persistence;

public sealed class RepositoryAndAuditStampTests(IntegrationTestFactory<RepositoryAndAuditStampTests> factory)
    : IClassFixture<IntegrationTestFactory<RepositoryAndAuditStampTests>>
{
    [Fact]
    public async Task BrandRepository_RoundTripsAgainstSqliteAndStampsAuditColumns()
    {
        var brandId = Guid.NewGuid();
        DateTimeOffset createdAt;
        DateTimeOffset modifiedAt;

        using (var createScope = factory.Services.CreateScope())
        {
            var repository = createScope.ServiceProvider.GetRequiredService<IBrandRepository>();
            var unitOfWork = createScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var brand = new Brand(brandId, "Nintendo");

            var addResult = await repository.AddAsync(brand, CancellationToken.None);
            addResult.IsSuccess.Should().BeTrue();

            await unitOfWork.SaveChangesAsync(CancellationToken.None);

            brand.CreatedBy.Should().Be("system");
            brand.ModifiedBy.Should().Be("system");
            brand.CreatedAt.Offset.Should().Be(TimeSpan.Zero);
            brand.ModifiedAt.Offset.Should().Be(TimeSpan.Zero);

            createdAt = brand.CreatedAt;
            modifiedAt = brand.ModifiedAt;
        }

        using (var updateScope = factory.Services.CreateScope())
        {
            var repository = updateScope.ServiceProvider.GetRequiredService<IBrandRepository>();
            var unitOfWork = updateScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var readResult = await repository.GetByIdAsync(brandId, CancellationToken.None);
            var brand = readResult.Value!;

            readResult.IsSuccess.Should().BeTrue();
            brand.CreatedAt.Should().Be(createdAt);
            brand.ModifiedAt.Should().Be(modifiedAt);

            brand.Rename("Nintendo Updated");
            var updateResult = await repository.UpdateAsync(brand, CancellationToken.None);
            updateResult.IsSuccess.Should().BeTrue();

            await unitOfWork.SaveChangesAsync(CancellationToken.None);

            brand.CreatedAt.Should().Be(createdAt);
            brand.ModifiedAt.Should().BeAfter(modifiedAt);
            brand.ModifiedBy.Should().Be("system");
        }

        using (var deactivateScope = factory.Services.CreateScope())
        {
            var repository = deactivateScope.ServiceProvider.GetRequiredService<IBrandRepository>();
            var unitOfWork = deactivateScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var readResult = await repository.GetByIdAsync(brandId, CancellationToken.None);
            var brand = readResult.Value!;

            readResult.IsSuccess.Should().BeTrue();
            brand.Name.Should().Be("Nintendo Updated");

            brand.Deactivate();
            var updateResult = await repository.UpdateAsync(brand, CancellationToken.None);
            updateResult.IsSuccess.Should().BeTrue();

            await unitOfWork.SaveChangesAsync(CancellationToken.None);
        }

        using var verifyScope = factory.Services.CreateScope();
        var verifyRepository = verifyScope.ServiceProvider.GetRequiredService<IBrandRepository>();

        var inactiveResult = await verifyRepository.GetByIdAsync(brandId, CancellationToken.None);
        var activeList = await verifyRepository.ListAsync(includeInactive: false, CancellationToken.None);
        var allList = await verifyRepository.ListAsync(includeInactive: true, CancellationToken.None);

        inactiveResult.IsSuccess.Should().BeTrue();
        inactiveResult.Value!.IsActive.Should().BeFalse();
        activeList.Should().NotContain(brand => brand.Id == brandId);
        allList.Should().Contain(brand => brand.Id == brandId && !brand.IsActive);
    }
}
