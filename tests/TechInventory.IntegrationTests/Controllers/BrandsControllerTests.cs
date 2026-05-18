using System.Net;
using FluentAssertions;
using TechInventory.Application.Brands;
using TechInventory.Application.Common.Paging;
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
}
