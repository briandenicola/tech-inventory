using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TechInventory.Application.Abstractions.Services;
using TechInventory.Infrastructure.Persistence;
using TechInventory.Infrastructure.Persistence.Interceptors;
using TechInventory.Infrastructure.Services;
using Xunit.Sdk;

namespace TechInventory.IntegrationTests.Support;

public sealed class RepositoryIntegrationTestHost<TMarker>(IntegrationTestFactory<TMarker> factory)
    where TMarker : class
{
    public async Task<AppDbContext> CreateDbContextAsync(bool requireSaveChangesInterceptor = false, CancellationToken cancellationToken = default)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(factory.ConnectionString);

        var auditInterceptor = ResolveAuditSaveChangesInterceptor(requireSaveChangesInterceptor);
        var dbContext = new AppDbContext(optionsBuilder.Options, auditInterceptor);
        await dbContext.Database.MigrateAsync(cancellationToken);
        return dbContext;
    }

    public TRepository CreateRepository<TRepository>(AppDbContext dbContext, string implementationName)
        where TRepository : class
    {
        var implementationType = typeof(AppDbContext).Assembly.GetType($"TechInventory.Infrastructure.Persistence.Repositories.{implementationName}");
        if (implementationType is null)
        {
            throw SkipException.ForSkip("awaiting Hicks T16");
        }

        if (!typeof(TRepository).IsAssignableFrom(implementationType))
        {
            throw new XunitException($"{implementationType.FullName} should implement {typeof(TRepository).FullName}.");
        }

        foreach (var constructor in implementationType.GetConstructors().OrderByDescending(candidate => candidate.GetParameters().Length))
        {
            if (!TryCreateConstructorArguments(constructor, dbContext, out var arguments))
            {
                continue;
            }

            return (TRepository)constructor.Invoke(arguments);
        }

        throw SkipException.ForSkip($"awaiting Hicks T16 ({implementationType.Name} constructor wiring)");
    }

    private AuditSaveChangesInterceptor ResolveAuditSaveChangesInterceptor(bool requireSaveChangesInterceptor)
    {
        using var scope = factory.Services.CreateScope();
        if (scope.ServiceProvider.GetService(typeof(ICurrentUserService)) is ICurrentUserService currentUserService)
        {
            return new AuditSaveChangesInterceptor(currentUserService);
        }

        if (requireSaveChangesInterceptor)
        {
            throw SkipException.ForSkip("awaiting Hicks T17");
        }

        return new AuditSaveChangesInterceptor(new SystemCurrentUserService());
    }

    private bool TryCreateConstructorArguments(System.Reflection.ConstructorInfo constructor, AppDbContext dbContext, out object?[] arguments)
    {
        arguments = new object?[constructor.GetParameters().Length];

        for (var index = 0; index < constructor.GetParameters().Length; index++)
        {
            var parameter = constructor.GetParameters()[index];
            if (parameter.ParameterType == typeof(AppDbContext) || parameter.ParameterType.IsAssignableFrom(typeof(AppDbContext)))
            {
                arguments[index] = dbContext;
                continue;
            }

            if (TryCreateService(parameter.ParameterType, service => service, out var service))
            {
                arguments[index] = service;
                continue;
            }

            return false;
        }

        return true;
    }

    private bool TryCreateService<T>(Type serviceType, Func<object, T> projection, out T? value)
    {
        using var scope = factory.Services.CreateScope();
        if (scope.ServiceProvider.GetService(serviceType) is { } registered)
        {
            value = projection(registered);
            return true;
        }

        if (serviceType == typeof(TimeProvider))
        {
            value = projection(TimeProvider.System);
            return true;
        }

        if (serviceType.GetConstructor(Type.EmptyTypes) is not null)
        {
            value = projection(Activator.CreateInstance(serviceType)!);
            return true;
        }

        value = default;
        return false;
    }
}
