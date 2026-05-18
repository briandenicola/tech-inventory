using NSubstitute;
using TechInventory.Application.Abstractions.Persistence;
using TechInventory.Application.Auditing;
using TechInventory.Application.Common.Results;

namespace TechInventory.UnitTests.Application;

internal sealed record HandlerDependencies<TRepository>(TRepository Repository, IUnitOfWork UnitOfWork, IAuditContext? AuditContext)
    where TRepository : class;

internal static class HandlerScaffoldingSupport
{
    public static HandlerDependencies<TRepository> CreateAuditable<TRepository>()
        where TRepository : class
        => new(Substitute.For<TRepository>(), Substitute.For<IUnitOfWork>(), Substitute.For<IAuditContext>());

    public static HandlerDependencies<TRepository> CreateQuery<TRepository>()
        where TRepository : class
        => new(Substitute.For<TRepository>(), Substitute.For<IUnitOfWork>(), null);

    public static Error ValidationError() => new("Validation", "The request is invalid.");

    public static Error NotFoundError(string entityName) => new("NotFound", $"{entityName} was not found.");

    public static Error ConflictError(string entityName) => new("Conflict", $"{entityName} already exists.");
}

internal static class HandlerSkipReasons
{
    public const string T20 = "Waiting on Hicks T20 handler";
    public const string T21 = "Waiting on Hicks T21 handler";
    public const string T22 = "Waiting on Hicks T22 handler";
    public const string T23 = "Waiting on Hicks T23 handler";
    public const string T24 = "Waiting on Hicks T24 handler";
    public const string T25 = "Waiting on Hicks T25 handler";
    public const string T26 = "Waiting on Hicks T26 handler";
    public const string T27 = "Waiting on Hicks T27 handler";
    public const string T28 = "Waiting on Hicks T28 handler";
}
