using FluentAssertions;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Results;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;
using TechInventory.Domain.Primitives;
using TechInventory.IntegrationTests.Support;

namespace TechInventory.IntegrationTests.Repositories;

public class ReferenceRepositoryIntegrationTests(IntegrationTestFactory<ReferenceRepositoryIntegrationTests> factory)
    : IClassFixture<IntegrationTestFactory<ReferenceRepositoryIntegrationTests>>
{
    private readonly RepositoryIntegrationTestHost<ReferenceRepositoryIntegrationTests> _host = new(factory);

    public static TheoryData<IActiveRepositoryCase> ActiveRepositoryCases => new()
    {
        CreateBrandCase(),
        CreateCategoryCase(),
        CreateOwnerCase(),
        CreateLocationCase(),
        CreateNetworkCase(),
        CreateTagCase()
    };

    [Theory]
    [MemberData(nameof(ActiveRepositoryCases))]
    public async Task ConcreteReferenceRepositories_AddFindUpdateAndSoftDeleteRoundTrip(IActiveRepositoryCase repositoryCase)
    {
        var snapshot = await repositoryCase.ExecuteCrudRoundTripAsync(_host);

        snapshot.CreatedAt.Offset.Should().Be(TimeSpan.Zero);
        snapshot.ModifiedAt.Offset.Should().Be(TimeSpan.Zero);
        snapshot.ModifiedAt.Should().BeOnOrAfter(snapshot.CreatedAt);
        snapshot.IsInactive.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(ActiveRepositoryCases))]
    public async Task ConcreteReferenceRepositories_ListAsyncReturnsActiveOnlyByDefault(IActiveRepositoryCase repositoryCase)
    {
        var snapshot = await repositoryCase.ExecuteActiveFilterAsync(_host);

        snapshot.DefaultIds.Should().Contain(snapshot.ActiveId);
        snapshot.DefaultIds.Should().NotContain(snapshot.InactiveId);
        snapshot.IncludeInactiveIds.Should().Contain(snapshot.ActiveId);
        snapshot.IncludeInactiveIds.Should().Contain(snapshot.InactiveId);
    }

    [Theory]
    [MemberData(nameof(ActiveRepositoryCases))]
    public async Task ConcreteReferenceRepositories_AuditColumnsAreStampedInUtc(IActiveRepositoryCase repositoryCase)
    {
        var snapshot = await repositoryCase.ExecuteAuditStampAsync(_host);

        snapshot.AddedCreatedAt.Should().NotBe(snapshot.AddSentinel);
        snapshot.AddedModifiedAt.Should().NotBe(snapshot.AddSentinel);
        snapshot.UpdatedModifiedAt.Should().NotBe(snapshot.UpdateSentinel);
        snapshot.AddedCreatedAt.Offset.Should().Be(TimeSpan.Zero);
        snapshot.AddedModifiedAt.Offset.Should().Be(TimeSpan.Zero);
        snapshot.UpdatedModifiedAt.Offset.Should().Be(TimeSpan.Zero);
        snapshot.UpdatedModifiedAt.Should().BeOnOrAfter(snapshot.AddedModifiedAt);
    }

    public interface IActiveRepositoryCase
    {
        Task<CrudRoundTripSnapshot> ExecuteCrudRoundTripAsync(RepositoryIntegrationTestHost<ReferenceRepositoryIntegrationTests> host);

        Task<StatusFilterSnapshot> ExecuteActiveFilterAsync(RepositoryIntegrationTestHost<ReferenceRepositoryIntegrationTests> host);

        Task<AuditStampSnapshot> ExecuteAuditStampAsync(RepositoryIntegrationTestHost<ReferenceRepositoryIntegrationTests> host);
    }

    public sealed record CrudRoundTripSnapshot(DateTimeOffset CreatedAt, DateTimeOffset ModifiedAt, bool IsInactive);

    public sealed record StatusFilterSnapshot(Guid ActiveId, Guid InactiveId, IReadOnlyCollection<Guid> DefaultIds, IReadOnlyCollection<Guid> IncludeInactiveIds);

    public sealed record AuditStampSnapshot(
        DateTimeOffset AddSentinel,
        DateTimeOffset UpdateSentinel,
        DateTimeOffset AddedCreatedAt,
        DateTimeOffset AddedModifiedAt,
        DateTimeOffset UpdatedModifiedAt);

    private sealed class ActiveRepositoryCase<TRepository, TEntity>(
        string name,
        string implementationName,
        Func<TEntity> createEntity,
        Action<TEntity> mutateEntity,
        Action<TEntity> softDeleteEntity,
        Action<TEntity> assertUpdated,
        Func<TRepository, TEntity, CancellationToken, Task<Result<TEntity>>> addAsync,
        Func<TRepository, Guid, CancellationToken, Task<Result<TEntity>>> getByIdAsync,
        Func<TRepository, TEntity, CancellationToken, Task<Result<TEntity>>> updateAsync,
        Func<TRepository, bool, CancellationToken, Task<IReadOnlyList<TEntity>>> listAsync)
        : IActiveRepositoryCase
        where TRepository : class
        where TEntity : Entity
    {
        public async Task<CrudRoundTripSnapshot> ExecuteCrudRoundTripAsync(RepositoryIntegrationTestHost<ReferenceRepositoryIntegrationTests> host)
        {
            await using var dbContext = await host.CreateDbContextAsync();
            var repository = host.CreateRepository<TRepository>(dbContext, implementationName);
            var entity = createEntity();

            (await addAsync(repository, entity, CancellationToken.None)).IsSuccess.Should().BeTrue();
            await dbContext.SaveChangesAsync();

            var added = await getByIdAsync(repository, entity.Id, CancellationToken.None);
            added.IsSuccess.Should().BeTrue();

            mutateEntity(entity);
            (await updateAsync(repository, entity, CancellationToken.None)).IsSuccess.Should().BeTrue();
            await dbContext.SaveChangesAsync();

            var updated = await getByIdAsync(repository, entity.Id, CancellationToken.None);
            updated.IsSuccess.Should().BeTrue();
            assertUpdated(updated.Value!);

            softDeleteEntity(entity);
            (await updateAsync(repository, entity, CancellationToken.None)).IsSuccess.Should().BeTrue();
            await dbContext.SaveChangesAsync();

            var softDeleted = await getByIdAsync(repository, entity.Id, CancellationToken.None);
            softDeleted.IsSuccess.Should().BeTrue();

            return new CrudRoundTripSnapshot(
                softDeleted.Value!.CreatedAt,
                softDeleted.Value.ModifiedAt,
                !IsEntityActive(softDeleted.Value));
        }

        public async Task<StatusFilterSnapshot> ExecuteActiveFilterAsync(RepositoryIntegrationTestHost<ReferenceRepositoryIntegrationTests> host)
        {
            await using var dbContext = await host.CreateDbContextAsync();
            var repository = host.CreateRepository<TRepository>(dbContext, implementationName);
            var active = createEntity();
            var inactive = createEntity();

            (await addAsync(repository, active, CancellationToken.None)).IsSuccess.Should().BeTrue();
            (await addAsync(repository, inactive, CancellationToken.None)).IsSuccess.Should().BeTrue();
            await dbContext.SaveChangesAsync();
            softDeleteEntity(inactive);
            (await updateAsync(repository, inactive, CancellationToken.None)).IsSuccess.Should().BeTrue();
            await dbContext.SaveChangesAsync();

            var defaultList = await listAsync(repository, false, CancellationToken.None);
            var includeInactiveList = await listAsync(repository, true, CancellationToken.None);

            return new StatusFilterSnapshot(
                active.Id,
                inactive.Id,
                defaultList.Select(item => item.Id).ToArray(),
                includeInactiveList.Select(item => item.Id).ToArray());
        }

        public async Task<AuditStampSnapshot> ExecuteAuditStampAsync(RepositoryIntegrationTestHost<ReferenceRepositoryIntegrationTests> host)
        {
            await using var dbContext = await host.CreateDbContextAsync(requireSaveChangesInterceptor: true);
            var repository = host.CreateRepository<TRepository>(dbContext, implementationName);
            var entity = createEntity();
            var addSentinel = new DateTimeOffset(2001, 1, 1, 0, 0, 0, TimeSpan.Zero);

            entity.SetAuditMetadata(addSentinel, addSentinel, "legacy", "legacy");
            (await addAsync(repository, entity, CancellationToken.None)).IsSuccess.Should().BeTrue();
            await dbContext.SaveChangesAsync();

            var added = await getByIdAsync(repository, entity.Id, CancellationToken.None);
            added.IsSuccess.Should().BeTrue();

            var updateSentinel = added.Value!.CreatedAt.AddDays(1);
            mutateEntity(entity);
            entity.SetAuditMetadata(added.Value.CreatedAt, updateSentinel, "legacy", "legacy");
            (await updateAsync(repository, entity, CancellationToken.None)).IsSuccess.Should().BeTrue();
            await dbContext.SaveChangesAsync();

            var updated = await getByIdAsync(repository, entity.Id, CancellationToken.None);
            updated.IsSuccess.Should().BeTrue();

            return new AuditStampSnapshot(
                addSentinel,
                updateSentinel,
                added.Value.CreatedAt,
                added.Value.ModifiedAt,
                updated.Value!.ModifiedAt);
        }

        public override string ToString() => name;

        private static bool IsEntityActive(TEntity entity)
        {
            if (entity.GetType().GetProperty("IsActive")?.GetValue(entity) is bool isActive)
            {
                return isActive;
            }

            throw new InvalidOperationException($"{typeof(TEntity).Name} should expose IsActive for the reference-repository contract.");
        }
    }

    private static IActiveRepositoryCase CreateBrandCase()
    {
        return new ActiveRepositoryCase<IBrandRepository, Brand>(
            "BrandRepository",
            "BrandRepository",
            () => new Brand(Guid.NewGuid(), $"Brand-{Guid.NewGuid():N}", "https://example.com", "notes"),
            entity =>
            {
                entity.Rename($"Updated-{Guid.NewGuid():N}", "apone");
                entity.UpdateDetails("https://updated.example.com", "updated notes", "apone");
            },
            entity => entity.Deactivate("apone"),
            entity =>
            {
                entity.Name.Should().StartWith("Updated-");
                entity.Website.Should().Be("https://updated.example.com");
                entity.Notes.Should().Be("updated notes");
            },
            (repository, entity, cancellationToken) => repository.AddAsync(entity, cancellationToken),
            (repository, id, cancellationToken) => repository.GetByIdAsync(id, cancellationToken),
            (repository, entity, cancellationToken) => repository.UpdateAsync(entity, cancellationToken),
            (repository, includeInactive, cancellationToken) => repository.ListAsync(includeInactive, cancellationToken));
    }

    private static IActiveRepositoryCase CreateCategoryCase()
    {
        return new ActiveRepositoryCase<ICategoryRepository, Category>(
            "CategoryRepository",
            "CategoryRepository",
            () => new Category(Guid.NewGuid(), $"Category-{Guid.NewGuid():N}"),
            entity =>
            {
                entity.Rename($"Updated-{Guid.NewGuid():N}", "apone");
                entity.UpdateIcon("laptop", "apone");
            },
            entity => entity.Deactivate("apone"),
            entity =>
            {
                entity.Name.Should().StartWith("Updated-");
                entity.Icon.Should().Be("laptop");
            },
            (repository, entity, cancellationToken) => repository.AddAsync(entity, cancellationToken),
            (repository, id, cancellationToken) => repository.GetByIdAsync(id, cancellationToken),
            (repository, entity, cancellationToken) => repository.UpdateAsync(entity, cancellationToken),
            (repository, includeInactive, cancellationToken) => repository.ListAsync(includeInactive, cancellationToken));
    }

    private static IActiveRepositoryCase CreateOwnerCase()
    {
        return new ActiveRepositoryCase<IOwnerRepository, Owner>(
            "OwnerRepository",
            "OwnerRepository",
            () => new Owner(Guid.NewGuid(), $"Owner-{Guid.NewGuid():N}"),
            entity =>
            {
                entity.Rename($"Updated-{Guid.NewGuid():N}", "apone");
                entity.SetRole(OwnerRole.Admin, "apone");
                entity.LinkEntraIdentity(Guid.NewGuid(), "apone");
            },
            entity => entity.Deactivate("apone"),
            entity =>
            {
                entity.DisplayName.Should().StartWith("Updated-");
                entity.Role.Should().Be(OwnerRole.Admin);
                entity.EntraObjectId.Should().NotBeNull();
            },
            (repository, entity, cancellationToken) => repository.AddAsync(entity, cancellationToken),
            (repository, id, cancellationToken) => repository.GetByIdAsync(id, cancellationToken),
            (repository, entity, cancellationToken) => repository.UpdateAsync(entity, cancellationToken),
            (repository, includeInactive, cancellationToken) => repository.ListAsync(includeInactive, cancellationToken));
    }

    private static IActiveRepositoryCase CreateLocationCase()
    {
        return new ActiveRepositoryCase<ILocationRepository, Location>(
            "LocationRepository",
            "LocationRepository",
            () => new Location(Guid.NewGuid(), $"Location-{Guid.NewGuid():N}", LocationType.Home),
            entity =>
            {
                entity.Rename($"Updated-{Guid.NewGuid():N}", "apone");
                entity.SetType(LocationType.Storage, "apone");
            },
            entity => entity.Deactivate("apone"),
            entity =>
            {
                entity.Name.Should().StartWith("Updated-");
                entity.Type.Should().Be(LocationType.Storage);
            },
            (repository, entity, cancellationToken) => repository.AddAsync(entity, cancellationToken),
            (repository, id, cancellationToken) => repository.GetByIdAsync(id, cancellationToken),
            (repository, entity, cancellationToken) => repository.UpdateAsync(entity, cancellationToken),
            (repository, includeInactive, cancellationToken) => repository.ListAsync(includeInactive, cancellationToken));
    }

    private static IActiveRepositoryCase CreateNetworkCase()
    {
        return new ActiveRepositoryCase<INetworkRepository, Network>(
            "NetworkRepository",
            "NetworkRepository",
            () => new Network(Guid.NewGuid(), $"Network-{Guid.NewGuid():N}", "initial"),
            entity =>
            {
                entity.Rename($"Updated-{Guid.NewGuid():N}", "apone");
                entity.UpdateDescription("updated", "apone");
            },
            entity => entity.Deactivate("apone"),
            entity =>
            {
                entity.Name.Should().StartWith("Updated-");
                entity.Description.Should().Be("updated");
            },
            (repository, entity, cancellationToken) => repository.AddAsync(entity, cancellationToken),
            (repository, id, cancellationToken) => repository.GetByIdAsync(id, cancellationToken),
            (repository, entity, cancellationToken) => repository.UpdateAsync(entity, cancellationToken),
            (repository, includeInactive, cancellationToken) => repository.ListAsync(includeInactive, cancellationToken));
    }

    private static IActiveRepositoryCase CreateTagCase()
    {
        return new ActiveRepositoryCase<ITagRepository, Tag>(
            "TagRepository",
            "TagRepository",
            () => new Tag(Guid.NewGuid(), $"Tag-{Guid.NewGuid():N}", "#112233"),
            entity =>
            {
                entity.Rename($"Updated-{Guid.NewGuid():N}", "apone");
                entity.UpdateColor("#445566", "apone");
            },
            entity => entity.Deactivate("apone"),
            entity =>
            {
                entity.Name.Should().StartWith("Updated-");
                entity.Color.Should().Be("#445566");
            },
            (repository, entity, cancellationToken) => repository.AddAsync(entity, cancellationToken),
            (repository, id, cancellationToken) => repository.GetByIdAsync(id, cancellationToken),
            (repository, entity, cancellationToken) => repository.UpdateAsync(entity, cancellationToken),
            (repository, includeInactive, cancellationToken) => repository.ListAsync(includeInactive, cancellationToken));
    }
}
