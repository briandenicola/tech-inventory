using System.Net;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TechInventory.Application.BulkOperations;
using TechInventory.Application.Categories;
using TechInventory.Application.Common.Paging;
using TechInventory.Application.Merges;
using TechInventory.Domain.Entities;

namespace TechInventory.IntegrationTests.Controllers;

public sealed class CategoriesControllerTests(IntegrationTestFactory<CategoriesControllerTests> factory)
    : ControllerTestBase<CategoriesControllerTests>(factory), IClassFixture<IntegrationTestFactory<CategoriesControllerTests>>
{
    [Fact]
    public async Task CreateCategory_WhenValid_Returns201WithLocation()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();
        var request = new { name = $"Category-{Guid.NewGuid():N}", parentId = (Guid?)null, icon = "desktop" };

        var response = await client.PostAsync("/api/v1/categories", CreateJsonContent(request));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await ReadJsonAsync<CategoryResponse>(response);
        created.Name.Should().Be(request.name);
        created.ParentId.Should().BeNull();
        created.Depth.Should().Be(1);
        created.Icon.Should().Be(request.icon);
        created.IsActive.Should().BeTrue();
        AssertCreatedLocationHeader(response, "/api/v1/categories", created.Id);
    }

    [Fact]
    public async Task GetCategories_WhenDatabaseEmpty_ReturnsEmptyPagedResponse()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/categories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var paged = await ReadJsonAsync<PagedResponse<CategoryResponse>>(response);
        paged.Page.Should().Be(1);
        paged.PageSize.Should().Be(25);
        paged.TotalCount.Should().Be(0);
        paged.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCategories_WhenDataExists_ReturnsItemsWithPagination()
    {
        await ResetDatabaseAsync();
        var first = new Category(Guid.NewGuid(), $"Category-{Guid.NewGuid():N}");
        var second = new Category(Guid.NewGuid(), $"Category-{Guid.NewGuid():N}");
        await SeedAsync(entities: [first, second]);
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/categories?page=1&pageSize=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var paged = await ReadJsonAsync<PagedResponse<CategoryResponse>>(response);
        paged.TotalCount.Should().Be(2);
        paged.Page.Should().Be(1);
        paged.PageSize.Should().Be(1);
        paged.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task GetCategoryById_WhenFound_ReturnsCategory()
    {
        await ResetDatabaseAsync();
        var category = new Category(Guid.NewGuid(), $"Category-{Guid.NewGuid():N}", icon: "desktop");
        await SeedAsync(entities: [category]);
        using var client = CreateClient();

        var response = await client.GetAsync($"/api/v1/categories/{category.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadJsonAsync<CategoryResponse>(response);
        payload.Id.Should().Be(category.Id);
        payload.Name.Should().Be(category.Name);
        payload.Icon.Should().Be(category.Icon);
        payload.Depth.Should().Be(1);
    }

    [Fact]
    public async Task GetCategoryById_WhenMissing_Returns404ProblemDetails()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();

        var response = await client.GetAsync($"/api/v1/categories/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await ReadProblemDetailsAsync(response);
        problem.Status.Should().Be((int)HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateCategory_WhenValid_ReturnsUpdatedCategory()
    {
        await ResetDatabaseAsync();
        var category = new Category(Guid.NewGuid(), $"Category-{Guid.NewGuid():N}", icon: "desktop");
        await SeedAsync(entities: [category]);
        using var client = CreateClient();
        var request = new { id = category.Id, name = $"Updated-{Guid.NewGuid():N}", parentId = (Guid?)null, icon = "laptop" };

        var response = await client.PutAsync($"/api/v1/categories/{category.Id}", CreateJsonContent(request));

        await AssertUpdateResponseAsync<CategoryResponse>(
            response,
            () => client.GetAsync($"/api/v1/categories/{category.Id}"),
            updated =>
            {
                updated.Id.Should().Be(category.Id);
                updated.Name.Should().Be(request.name);
                updated.Icon.Should().Be(request.icon);
            });
    }

    [Fact]
    public async Task UpdateCategory_WhenValidationFails_Returns400ValidationProblemDetails()
    {
        await ResetDatabaseAsync();
        var category = new Category(Guid.NewGuid(), $"Category-{Guid.NewGuid():N}");
        await SeedAsync(entities: [category]);
        using var client = CreateClient();
        var request = new { id = category.Id, name = string.Empty, parentId = Guid.Empty, icon = new string('i', 101) };

        var response = await client.PutAsync($"/api/v1/categories/{category.Id}", CreateJsonContent(request));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await ReadValidationProblemDetailsAsync(response);
        problem.Errors.Keys.Should().Contain(key => string.Equals(key, "Name", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DeleteCategory_WhenFound_Returns204()
    {
        await ResetDatabaseAsync();
        var category = new Category(Guid.NewGuid(), $"Category-{Guid.NewGuid():N}");
        await SeedAsync(entities: [category]);
        using var client = CreateClient();

        var response = await client.DeleteAsync($"/api/v1/categories/{category.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var reload = await client.GetAsync($"/api/v1/categories/{category.Id}");
        reload.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadJsonAsync<CategoryResponse>(reload);
        payload.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteCategory_WhenMissing_Returns404ProblemDetails()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();

        var response = await client.DeleteAsync($"/api/v1/categories/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await ReadProblemDetailsAsync(response);
        problem.Status.Should().Be((int)HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCategoryTree_WhenHierarchyExists_ReturnsNestedChildren()
    {
        await ResetDatabaseAsync();
        var root = new Category(Guid.NewGuid(), $"Root-{Guid.NewGuid():N}");
        var child = new Category(Guid.NewGuid(), $"Child-{Guid.NewGuid():N}", root.Id, 2, "laptop");
        await SeedAsync(entities: [root, child]);
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/categories/tree");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var roots = await ReadJsonAsync<CategoryResponse[]>(response);
        roots.Should().ContainSingle(category => category.Id == root.Id);
        roots.Single(category => category.Id == root.Id).Children.Should().ContainSingle(category => category.Id == child.Id);
    }

    [Fact]
    public async Task DeleteCategory_WhenArchivingParent_CascadesArchiveToChildren()
    {
        await ResetDatabaseAsync();
        var root = new Category(Guid.NewGuid(), $"Root-{Guid.NewGuid():N}");
        var child = new Category(Guid.NewGuid(), $"Child-{Guid.NewGuid():N}", root.Id, 2, "laptop");
        await SeedAsync(entities: [root, child]);
        using var client = CreateClient();

        var response = await client.DeleteAsync($"/api/v1/categories/{root.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var rootResponse = await client.GetAsync($"/api/v1/categories/{root.Id}");
        var childResponse = await client.GetAsync($"/api/v1/categories/{child.Id}");
        var archivedRoot = await ReadJsonAsync<CategoryResponse>(rootResponse);
        var archivedChild = await ReadJsonAsync<CategoryResponse>(childResponse);
        archivedRoot.IsActive.Should().BeFalse();
        archivedChild.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task MergeCategory_WhenValid_ReassignsDevicesAndReparentsChildren()
    {
        await ResetDatabaseAsync();
        var references = await SeedDeviceReferenceDataAsync();
        var target = new Category(Guid.NewGuid(), $"Target-{Guid.NewGuid():N}");
        var child = new Category(Guid.NewGuid(), $"Child-{Guid.NewGuid():N}", references.Category.Id, 2, "speaker");
        var device = CreateDevice(references, "Merged Device");
        await SeedAsync(entities: [target, child, device]);
        using var client = CreateClient();

        var response = await client.PostAsync(
            "/api/v1/categories/merge",
            CreateJsonContent(new { sourceId = references.Category.Id, targetId = target.Id }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadJsonAsync<MergeReferenceEntityResponse>(response);
        payload.Should().BeEquivalentTo(new MergeReferenceEntityResponse(1, references.Category.Id, target.Id));

        var mergedDevice = await WithDbContextAsync(dbContext => dbContext.Devices.AsNoTracking().SingleAsync(entity => entity.Id == device.Id));
        mergedDevice.CategoryId.Should().Be(target.Id);

        var reparentedChild = await WithDbContextAsync(dbContext => dbContext.Categories.AsNoTracking().SingleAsync(entity => entity.Id == child.Id));
        reparentedChild.ParentId.Should().Be(target.Id);
        reparentedChild.Depth.Should().Be(2);

        var sourceResponse = await client.GetAsync($"/api/v1/categories/{references.Category.Id}");
        var sourcePayload = await ReadJsonAsync<CategoryResponse>(sourceResponse);
        sourcePayload.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task BulkDeleteCategories_WhenValid_DeletesSelectedCategoriesAndTheirChildren()
    {
        await ResetDatabaseAsync();
        var root = new Category(Guid.NewGuid(), $"Root-{Guid.NewGuid():N}");
        var child = new Category(Guid.NewGuid(), $"Child-{Guid.NewGuid():N}", root.Id, 2, "speaker");
        var sibling = new Category(Guid.NewGuid(), $"Sibling-{Guid.NewGuid():N}");
        await SeedAsync(entities: [root, child, sibling]);
        using var client = CreateClient();

        var response = await client.PostAsync(
            "/api/v1/categories/bulk/delete",
            CreateJsonContent(new { categoryIds = new[] { root.Id, sibling.Id } }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadJsonAsync<BulkOperationResponse>(response);
        payload.AffectedCount.Should().Be(2);

        var rootResponse = await client.GetAsync($"/api/v1/categories/{root.Id}");
        var childResponse = await client.GetAsync($"/api/v1/categories/{child.Id}");
        var siblingResponse = await client.GetAsync($"/api/v1/categories/{sibling.Id}");
        (await ReadJsonAsync<CategoryResponse>(rootResponse)).IsActive.Should().BeFalse();
        (await ReadJsonAsync<CategoryResponse>(childResponse)).IsActive.Should().BeFalse();
        (await ReadJsonAsync<CategoryResponse>(siblingResponse)).IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task BulkDeleteCategories_WhenAnyCategoryIsInactive_ReturnsConflictWithoutMutatingOthers()
    {
        await ResetDatabaseAsync();
        var active = new Category(Guid.NewGuid(), $"Category-{Guid.NewGuid():N}");
        var inactive = new Category(Guid.NewGuid(), $"Category-{Guid.NewGuid():N}");
        inactive.Deactivate();
        await SeedAsync(entities: [active, inactive]);
        using var client = CreateClient();

        var response = await client.PostAsync(
            "/api/v1/categories/bulk/delete",
            CreateJsonContent(new { categoryIds = new[] { active.Id, inactive.Id } }));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await ReadProblemDetailsAsync(response);
        problem.Detail.Should().Contain(inactive.Id.ToString());

        var reloadedActive = await client.GetAsync($"/api/v1/categories/{active.Id}");
        (await ReadJsonAsync<CategoryResponse>(reloadedActive)).IsActive.Should().BeTrue();
    }
}
