using System.Net;
using FluentAssertions;
using TechInventory.Application.Common.Paging;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.IntegrationTests.Controllers;

public sealed class AuditEventsControllerTests(IntegrationTestFactory<AuditEventsControllerTests> factory)
    : ControllerTestBase<AuditEventsControllerTests>(factory), IClassFixture<IntegrationTestFactory<AuditEventsControllerTests>>
{
    [Fact]
    public async Task GetAuditEvents_WhenDatabaseEmpty_ReturnsEmptyPagedResponse()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/audit-events");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var paged = await ReadJsonAsync<PagedResponse<AuditEventResponseContract>>(response);
        paged.Page.Should().Be(1);
        paged.PageSize.Should().Be(25);
        paged.TotalCount.Should().Be(0);
        paged.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAuditEvents_WhenFilteredByEntityTypeAndEntityId_ReturnsMatchingEvents()
    {
        await ResetDatabaseAsync();
        var deviceId = Guid.NewGuid();
        var matching = new AuditEvent(Guid.NewGuid(), "apone", nameof(Device), deviceId.ToString(), AuditAction.Updated, "{\"before\":1}", "{\"after\":2}");
        var other = new AuditEvent(Guid.NewGuid(), "hudson", nameof(Brand), Guid.NewGuid().ToString(), AuditAction.Created, "null", "{\"after\":true}");
        await SeedAsync(entities: [matching, other]);
        using var client = CreateClient();

        var response = await client.GetAsync($"/api/v1/audit-events?entityType={nameof(Device)}&entityId={deviceId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var paged = await ReadJsonAsync<PagedResponse<AuditEventResponseContract>>(response);
        paged.TotalCount.Should().Be(1);
        paged.Items.Should().ContainSingle();
        paged.Items[0].Id.Should().Be(matching.Id);
    }

    [Fact]
    public async Task GetAuditEvents_WhenPageSizeSpecified_ReturnsPagedResults()
    {
        await ResetDatabaseAsync();
        var first = new AuditEvent(Guid.NewGuid(), "apone", nameof(Device), Guid.NewGuid().ToString(), AuditAction.Created, "null", "{\"after\":1}");
        var second = new AuditEvent(Guid.NewGuid(), "apone", nameof(Device), Guid.NewGuid().ToString(), AuditAction.Updated, "{\"before\":1}", "{\"after\":2}");
        await SeedAsync(entities: [first, second]);
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/audit-events?page=1&pageSize=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var paged = await ReadJsonAsync<PagedResponse<AuditEventResponseContract>>(response);
        paged.TotalCount.Should().Be(2);
        paged.Page.Should().Be(1);
        paged.PageSize.Should().Be(1);
        paged.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task CreateAuditEvent_WhenPostInvoked_Returns405MethodNotAllowed()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();

        var response = await client.PostAsync("/api/v1/audit-events", CreateJsonContent(new { }));

        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task UpdateAuditEvent_WhenPutInvoked_Returns405MethodNotAllowed()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();

        var response = await client.PutAsync("/api/v1/audit-events", CreateJsonContent(new { }));

        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task DeleteAuditEvent_WhenDeleteInvoked_Returns405MethodNotAllowed()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();

        var response = await client.DeleteAsync("/api/v1/audit-events");

        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }
}
