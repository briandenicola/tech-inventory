using Microsoft.EntityFrameworkCore;
using TechInventory.Application.Common.Paging;
using TechInventory.Application.Common.Results;

namespace TechInventory.Infrastructure.Persistence.Repositories;

public abstract class Repository<TEntity, TKey>(AppDbContext dbContext)
    where TEntity : class
    where TKey : notnull
{
    protected AppDbContext DbContext { get; } = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    protected DbSet<TEntity> Entities => DbContext.Set<TEntity>();

    protected virtual IQueryable<TEntity> DefaultQuery => Entities;

    protected virtual IQueryable<TEntity> AllQuery => Entities;

    protected abstract string EntityName { get; }

    protected abstract TKey GetKey(TEntity entity);

    protected async Task<Result<TEntity>> AddEntityAsync(TEntity entity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entity);

        await Entities.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        return Result<TEntity>.Success(entity);
    }

    protected async Task<Result<TEntity>> GetEntityByIdAsync(TKey id, CancellationToken cancellationToken)
    {
        var trackedEntity = FindTrackedEntity(entity => EqualityComparer<TKey>.Default.Equals(GetKey(entity), id));
        if (trackedEntity is not null)
        {
            return Result<TEntity>.Success(trackedEntity);
        }

        var entity = await Entities.FindAsync([id], cancellationToken).ConfigureAwait(false);
        return entity is null
            ? Result<TEntity>.Failure(CreateNotFoundError(id))
            : Result<TEntity>.Success(entity);
    }

    protected async Task<IReadOnlyList<TEntity>> MergeTrackedAsync(
        IQueryable<TEntity> databaseQuery,
        Func<TEntity, bool> localPredicate,
        Func<IEnumerable<TEntity>, IOrderedEnumerable<TEntity>> orderBy,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(databaseQuery);
        ArgumentNullException.ThrowIfNull(localPredicate);
        ArgumentNullException.ThrowIfNull(orderBy);

        var merged = new Dictionary<TKey, TEntity>();

        foreach (var entity in await databaseQuery.ToListAsync(cancellationToken).ConfigureAwait(false))
        {
            merged[GetKey(entity)] = entity;
        }

        foreach (var entity in Entities.Local.Where(localPredicate).Where(IsNotDeleted))
        {
            merged[GetKey(entity)] = entity;
        }

        return orderBy(merged.Values).ToArray();
    }

    protected async Task<PagedResult<TEntity>> ToPagedResultAsync(
        IQueryable<TEntity> databaseQuery,
        Func<TEntity, bool> localPredicate,
        Func<IEnumerable<TEntity>, IOrderedEnumerable<TEntity>> orderBy,
        PageRequest pageRequest,
        CancellationToken cancellationToken)
    {
        var merged = await MergeTrackedAsync(databaseQuery, localPredicate, orderBy, cancellationToken).ConfigureAwait(false);
        var totalCount = merged.Count;
        var items = merged
            .Skip((pageRequest.Page - 1) * pageRequest.PageSize)
            .Take(pageRequest.PageSize)
            .ToArray();

        return new PagedResult<TEntity>(items, totalCount, pageRequest.Page, pageRequest.PageSize);
    }

    protected TEntity? FindTrackedEntity(Func<TEntity, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return Entities.Local
            .Where(IsNotDeleted)
            .FirstOrDefault(predicate);
    }

    protected async Task<Result<TEntity>> UpdateEntityAsync(TEntity entity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var key = GetKey(entity);
        var exists = FindTrackedEntity(candidate => EqualityComparer<TKey>.Default.Equals(GetKey(candidate), key)) is not null
            || await Entities.FindAsync([key], cancellationToken).ConfigureAwait(false) is not null;
        if (!exists)
        {
            return Result<TEntity>.Failure(CreateNotFoundError(key));
        }

        var entry = DbContext.Entry(entity);
        if (entry.State == EntityState.Detached)
        {
            Entities.Attach(entity);
            entry = DbContext.Entry(entity);
        }

        entry.State = EntityState.Modified;
        return Result<TEntity>.Success(entity);
    }

    protected Result<TEntity> ToLookupResult(TEntity? entity, object lookupValue)
    {
        return entity is null
            ? Result<TEntity>.Failure(CreateNotFoundError(lookupValue))
            : Result<TEntity>.Success(entity);
    }

    protected static string NormalizeLookupValue(string value, string paramName)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException($"{paramName} is required.", paramName)
            : value.Trim();
    }

    protected Error CreateNotFoundError(object lookupValue)
    {
        return new Error("NotFound", $"{EntityName} '{lookupValue}' was not found.");
    }

    private bool IsNotDeleted(TEntity entity)
    {
        return DbContext.Entry(entity).State != EntityState.Deleted;
    }
}
