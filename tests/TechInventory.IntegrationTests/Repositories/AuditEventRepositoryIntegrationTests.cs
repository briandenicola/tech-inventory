using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TechInventory.Application.Abstractions.Repositories;
using TechInventory.Application.Common.Paging;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;
using TechInventory.IntegrationTests.Support;
using TechInventory.Infrastructure.Persistence;

namespace TechInventory.IntegrationTests.Repositories;

public class AuditEventRepositoryIntegrationTests(IntegrationTestFactory<AuditEventRepositoryIntegrationTests> factory)
    : IClassFixture<IntegrationTestFactory<AuditEventRepositoryIntegrationTests>>
{
    private readonly RepositoryIntegrationTestHost<AuditEventRepositoryIntegrationTests> _host = new(factory);

    [Fact]
    public async Task AppendAsync_PersistsAuditEvent()
    {
        await using var dbContext = await _host.CreateDbContextAsync();
        var repository = _host.CreateRepository<IAuditEventRepository>(dbContext, "AuditEventRepository");
        var auditEvent = new AuditEvent(
            Guid.NewGuid(),
            "apone",
            "Device",
            Guid.NewGuid().ToString(),
            AuditAction.Updated,
            "{\"before\":true}",
            "{\"after\":true}");

        var appendResult = await repository.AppendAsync(auditEvent, CancellationToken.None);
        appendResult.IsSuccess.Should().BeTrue();
        await dbContext.SaveChangesAsync();

        var loaded = await repository.GetByIdAsync(auditEvent.Id, CancellationToken.None);
        loaded.IsSuccess.Should().BeTrue();
        loaded.Value.Should().NotBeNull();
        loaded.Value!.Actor.Should().Be("apone");
        loaded.Value.Action.Should().Be(AuditAction.Updated);

        var paged = await repository.ListAsync(new AuditEventListCriteria(new PageRequest(), entityType: "Device"), CancellationToken.None);
        paged.Items.Select(item => item.Id).Should().Contain(auditEvent.Id);
    }

    [Fact]
    public async Task AuditEvents_CannotBeUpdatedEndToEnd()
    {
        await using var dbContext = await _host.CreateDbContextAsync();
        var auditEvent = await SeedAuditEventAsync(dbContext);

        dbContext.Entry(auditEvent).Property(nameof(AuditEvent.Actor)).CurrentValue = "hudson";
        dbContext.Entry(auditEvent).State = EntityState.Modified;

        var act = () => dbContext.SaveChangesAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*append-only*");
    }

    [Fact]
    public async Task AuditEvents_CannotBeDeletedEndToEnd()
    {
        await using var dbContext = await _host.CreateDbContextAsync();
        var auditEvent = await SeedAuditEventAsync(dbContext);

        dbContext.Remove(auditEvent);

        var act = () => dbContext.SaveChangesAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*append-only*");
    }

    private static async Task<AuditEvent> SeedAuditEventAsync(AppDbContext dbContext)
    {
        var auditEvent = new AuditEvent(
            Guid.NewGuid(),
            "apone",
            "Device",
            Guid.NewGuid().ToString(),
            AuditAction.Created,
            "{\"before\":null}",
            "{\"after\":{\"name\":\"Nostromo\"}}");

        dbContext.AuditEvents.Add(auditEvent);
        await dbContext.SaveChangesAsync();

        return auditEvent;
    }
}
