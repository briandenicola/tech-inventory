using System.Net;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TechInventory.Application.Brands;
using TechInventory.Application.BulkOperations;
using TechInventory.Application.Common.Paging;
using TechInventory.Application.Merges;
using TechInventory.Domain.Entities;

namespace TechInventory.IntegrationTests.Controllers;

public sealed class BrandsControllerTests(IntegrationTestFactory<BrandsControllerTests> factory)
    : ControllerTestBase<BrandsControllerTests>(factory), IClassFixture<IntegrationTestFactory<BrandsControllerTests>>
{
    [Fact]
    public async Task CreateBrand_WhenValid_Returns201WithLocation()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();

        var request = new { name = $"Brand-{Guid.NewGuid():N}", website = "https://example.com", notes = "Primary brand" };

        var response = await client.PostAsync("/api/v1/brands", CreateJsonContent(request));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await ReadJsonAsync<BrandResponse>(response);
        created.Name.Should().Be(request.name);
        created.Website.Should().Be(request.website);
        created.Notes.Should().Be(request.notes);
        created.IsActive.Should().BeTrue();
        AssertCreatedLocationHeader(response, "/api/v1/brands", created.Id);
    }

    [Fact]
    public async Task GetBrands_WhenDatabaseEmpty_ReturnsEmptyPagedResponse()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/brands");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var paged = await ReadJsonAsync<PagedResponse<BrandResponse>>(response);
        paged.Page.Should().Be(1);
        paged.PageSize.Should().Be(25);
        paged.TotalCount.Should().Be(0);
        paged.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBrands_WhenDataExists_ReturnsItemsWithPagination()
    {
        await ResetDatabaseAsync();
        var first = new Brand(Guid.NewGuid(), $"Brand-{Guid.NewGuid():N}");
        var second = new Brand(Guid.NewGuid(), $"Brand-{Guid.NewGuid():N}");
        await SeedAsync(entities: [first, second]);
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/brands?page=1&pageSize=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var paged = await ReadJsonAsync<PagedResponse<BrandResponse>>(response);
        paged.Page.Should().Be(1);
        paged.PageSize.Should().Be(1);
        paged.TotalCount.Should().Be(2);
        paged.Items.Should().ContainSingle();
        paged.Items.Select(item => item.Id).Should().Contain(paged.Items[0].Id);
    }

    [Fact]
    public async Task GetBrandById_WhenFound_ReturnsBrand()
    {
        await ResetDatabaseAsync();
        var brand = new Brand(Guid.NewGuid(), $"Brand-{Guid.NewGuid():N}", "https://example.com", "Existing");
        await SeedAsync(entities: [brand]);
        using var client = CreateClient();

        var response = await client.GetAsync($"/api/v1/brands/{brand.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadJsonAsync<BrandResponse>(response);
        payload.Id.Should().Be(brand.Id);
        payload.Name.Should().Be(brand.Name);
        payload.Website.Should().Be(brand.Website);
    }

    [Fact]
    public async Task GetBrandById_WhenMissing_Returns404ProblemDetails()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();

        var response = await client.GetAsync($"/api/v1/brands/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await ReadProblemDetailsAsync(response);
        problem.Status.Should().Be((int)HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateBrand_WhenValid_ReturnsUpdatedBrand()
    {
        await ResetDatabaseAsync();
        var brand = new Brand(Guid.NewGuid(), $"Brand-{Guid.NewGuid():N}", "https://example.com", "Original");
        await SeedAsync(entities: [brand]);
        using var client = CreateClient();
        var request = new { id = brand.Id, name = $"Updated-{Guid.NewGuid():N}", website = "https://updated.example.com", notes = "Updated" };

        var response = await client.PutAsync($"/api/v1/brands/{brand.Id}", CreateJsonContent(request));

        await AssertUpdateResponseAsync<BrandResponse>(
            response,
            () => client.GetAsync($"/api/v1/brands/{brand.Id}"),
            updated =>
            {
                updated.Id.Should().Be(brand.Id);
                updated.Name.Should().Be(request.name);
                updated.Website.Should().Be(request.website);
                updated.Notes.Should().Be(request.notes);
            });
    }

    [Fact]
    public async Task UpdateBrand_WhenValidationFails_Returns400ValidationProblemDetails()
    {
        await ResetDatabaseAsync();
        var brand = new Brand(Guid.NewGuid(), $"Brand-{Guid.NewGuid():N}");
        await SeedAsync(entities: [brand]);
        using var client = CreateClient();
        var request = new { id = brand.Id, name = string.Empty, website = "not-a-uri", notes = new string('n', 4001) };

        var response = await client.PutAsync($"/api/v1/brands/{brand.Id}", CreateJsonContent(request));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await ReadValidationProblemDetailsAsync(response);
        problem.Errors.Keys.Should().Contain(key => string.Equals(key, "Name", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DeleteBrand_WhenFound_Returns204()
    {
        await ResetDatabaseAsync();
        var brand = new Brand(Guid.NewGuid(), $"Brand-{Guid.NewGuid():N}");
        await SeedAsync(entities: [brand]);
        using var client = CreateClient();

        var response = await client.DeleteAsync($"/api/v1/brands/{brand.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var reload = await client.GetAsync($"/api/v1/brands/{brand.Id}");
        reload.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadJsonAsync<BrandResponse>(reload);
        payload.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteBrand_WhenMissing_Returns404ProblemDetails()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();

        var response = await client.DeleteAsync($"/api/v1/brands/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await ReadProblemDetailsAsync(response);
        problem.Status.Should().Be((int)HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MergeBrand_WhenValid_ReassignsDevicesDeactivatesSourceAndWritesAuditEvents()
    {
        await ResetDatabaseAsync();
        var references = await SeedDeviceReferenceDataAsync();
        var target = new Brand(Guid.NewGuid(), $"Target-{Guid.NewGuid():N}");
        var device = CreateDevice(references, "Merged Device");
        await SeedAsync(entities: [target, device]);
        using var client = CreateClient();

        var response = await client.PostAsync(
            "/api/v1/brands/merge",
            CreateJsonContent(new { sourceId = references.Brand.Id, targetId = target.Id }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadJsonAsync<MergeReferenceEntityResponse>(response);
        payload.Should().BeEquivalentTo(new MergeReferenceEntityResponse(1, references.Brand.Id, target.Id));

        var mergedDevice = await WithDbContextAsync(dbContext => dbContext.Devices.AsNoTracking().SingleAsync(entity => entity.Id == device.Id));
        mergedDevice.BrandId.Should().Be(target.Id);

        var reloadedSource = await client.GetAsync($"/api/v1/brands/{references.Brand.Id}");
        var sourcePayload = await ReadJsonAsync<BrandResponse>(reloadedSource);
        sourcePayload.IsActive.Should().BeFalse();

        var auditEvents = await WithDbContextAsync(dbContext => dbContext.AuditEvents
            .AsNoTracking()
            .Where(entity => entity.EntityType == nameof(Brand)
                && (entity.EntityId == references.Brand.Id.ToString() || entity.EntityId == target.Id.ToString()))
            .ToListAsync());
        auditEvents.Should().HaveCount(2);
        auditEvents.Should().Contain(entity => entity.EntityId == references.Brand.Id.ToString() && entity.Action == Domain.Enums.AuditAction.Deleted);
        auditEvents.Should().Contain(entity => entity.EntityId == target.Id.ToString() && entity.Action == Domain.Enums.AuditAction.Updated);
        auditEvents.Should().OnlyContain(entity => entity.AfterPayload.Contains("\"mergedCount\":1", StringComparison.Ordinal));
    }

    [Fact]
    public async Task BulkDeleteBrands_WhenValid_DeactivatesAllBrandsAndWritesCorrelatedAuditEvents()
    {
        await ResetDatabaseAsync();
        var first = new Brand(Guid.NewGuid(), $"Brand-{Guid.NewGuid():N}");
        var second = new Brand(Guid.NewGuid(), $"Brand-{Guid.NewGuid():N}");
        await SeedAsync(entities: [first, second]);
        using var client = CreateClient();

        var response = await client.PostAsync(
            "/api/v1/brands/bulk/delete",
            CreateJsonContent(new { brandIds = new[] { first.Id, second.Id } }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadJsonAsync<BulkOperationResponse>(response);
        payload.AffectedCount.Should().Be(2);

        var firstResponse = await client.GetAsync($"/api/v1/brands/{first.Id}");
        var secondResponse = await client.GetAsync($"/api/v1/brands/{second.Id}");
        (await ReadJsonAsync<BrandResponse>(firstResponse)).IsActive.Should().BeFalse();
        (await ReadJsonAsync<BrandResponse>(secondResponse)).IsActive.Should().BeFalse();

        var auditEvents = await WithDbContextAsync(dbContext => dbContext.AuditEvents
            .AsNoTracking()
            .Where(entity => entity.EntityType == nameof(Brand)
                && (entity.EntityId == first.Id.ToString() || entity.EntityId == second.Id.ToString()))
            .ToListAsync());
        auditEvents.Should().HaveCount(2);
        auditEvents.Should().OnlyContain(entity => entity.Action == Domain.Enums.AuditAction.Deleted);
        auditEvents.Should().AllSatisfy(entity => entity.AfterPayload.Should().Contain(payload.CorrelationId.ToString()));
    }

    [Fact]
    public async Task BulkDeleteBrands_WhenAnyBrandIsInactive_ReturnsConflictWithoutChangingActiveRows()
    {
        await ResetDatabaseAsync();
        var active = new Brand(Guid.NewGuid(), $"Brand-{Guid.NewGuid():N}");
        var inactive = new Brand(Guid.NewGuid(), $"Brand-{Guid.NewGuid():N}");
        inactive.Deactivate();
        await SeedAsync(entities: [active, inactive]);
        using var client = CreateClient();

        var response = await client.PostAsync(
            "/api/v1/brands/bulk/delete",
            CreateJsonContent(new { brandIds = new[] { active.Id, inactive.Id } }));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await ReadProblemDetailsAsync(response);
        problem.Detail.Should().Contain(inactive.Id.ToString());

        var reloadedActive = await client.GetAsync($"/api/v1/brands/{active.Id}");
        (await ReadJsonAsync<BrandResponse>(reloadedActive)).IsActive.Should().BeTrue();
    }
}
