using System.Net;
using FluentAssertions;
using TechInventory.Application.Common.Paging;
using TechInventory.Application.Tags;
using TechInventory.Domain.Entities;

namespace TechInventory.IntegrationTests.Controllers;

public sealed class TagsControllerTests(IntegrationTestFactory<TagsControllerTests> factory)
    : ControllerTestBase<TagsControllerTests>(factory), IClassFixture<IntegrationTestFactory<TagsControllerTests>>
{
    [Fact]
    public async Task CreateTag_WhenValid_Returns201WithLocation()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();
        var request = new { name = $"Tag-{Guid.NewGuid():N}", color = "#112233" };

        var response = await client.PostAsync("/api/v1/tags", CreateJsonContent(request));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await ReadJsonAsync<TagResponse>(response);
        created.Name.Should().Be(request.name);
        created.Color.Should().Be(request.color);
        created.IsActive.Should().BeTrue();
        AssertCreatedLocationHeader(response, "/api/v1/tags", created.Id);
    }

    [Fact]
    public async Task GetTags_WhenDatabaseEmpty_ReturnsEmptyPagedResponse()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/tags");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var paged = await ReadJsonAsync<PagedResponse<TagResponse>>(response);
        paged.Page.Should().Be(1);
        paged.PageSize.Should().Be(25);
        paged.TotalCount.Should().Be(0);
        paged.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTags_WhenDataExists_ReturnsItemsWithPagination()
    {
        await ResetDatabaseAsync();
        var first = new Tag(Guid.NewGuid(), $"Tag-{Guid.NewGuid():N}", "#112233");
        var second = new Tag(Guid.NewGuid(), $"Tag-{Guid.NewGuid():N}", "#445566");
        await SeedAsync(entities: [first, second]);
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/tags?page=1&pageSize=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var paged = await ReadJsonAsync<PagedResponse<TagResponse>>(response);
        paged.TotalCount.Should().Be(2);
        paged.Page.Should().Be(1);
        paged.PageSize.Should().Be(1);
        paged.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task GetTagById_WhenFound_ReturnsTag()
    {
        await ResetDatabaseAsync();
        var tag = new Tag(Guid.NewGuid(), $"Tag-{Guid.NewGuid():N}", "#112233");
        await SeedAsync(entities: [tag]);
        using var client = CreateClient();

        var response = await client.GetAsync($"/api/v1/tags/{tag.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadJsonAsync<TagResponse>(response);
        payload.Id.Should().Be(tag.Id);
        payload.Name.Should().Be(tag.Name);
        payload.Color.Should().Be(tag.Color);
    }

    [Fact]
    public async Task GetTagById_WhenMissing_Returns404ProblemDetails()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();

        var response = await client.GetAsync($"/api/v1/tags/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await ReadProblemDetailsAsync(response);
        problem.Status.Should().Be((int)HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateTag_WhenValid_ReturnsUpdatedTag()
    {
        await ResetDatabaseAsync();
        var tag = new Tag(Guid.NewGuid(), $"Tag-{Guid.NewGuid():N}", "#112233");
        await SeedAsync(entities: [tag]);
        using var client = CreateClient();
        var request = new { id = tag.Id, name = $"Updated-{Guid.NewGuid():N}", color = "#445566" };

        var response = await client.PutAsync($"/api/v1/tags/{tag.Id}", CreateJsonContent(request));

        await AssertUpdateResponseAsync<TagResponse>(
            response,
            () => client.GetAsync($"/api/v1/tags/{tag.Id}"),
            updated =>
            {
                updated.Id.Should().Be(tag.Id);
                updated.Name.Should().Be(request.name);
                updated.Color.Should().Be(request.color);
            });
    }

    [Fact]
    public async Task UpdateTag_WhenValidationFails_Returns400ValidationProblemDetails()
    {
        await ResetDatabaseAsync();
        var tag = new Tag(Guid.NewGuid(), $"Tag-{Guid.NewGuid():N}", "#112233");
        await SeedAsync(entities: [tag]);
        using var client = CreateClient();
        var request = new { id = tag.Id, name = string.Empty, color = new string('c', 33) };

        var response = await client.PutAsync($"/api/v1/tags/{tag.Id}", CreateJsonContent(request));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await ReadValidationProblemDetailsAsync(response);
        problem.Errors.Keys.Should().Contain(key => string.Equals(key, "Name", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DeleteTag_WhenFound_Returns204()
    {
        await ResetDatabaseAsync();
        var tag = new Tag(Guid.NewGuid(), $"Tag-{Guid.NewGuid():N}", "#112233");
        await SeedAsync(entities: [tag]);
        using var client = CreateClient();

        var response = await client.DeleteAsync($"/api/v1/tags/{tag.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var reload = await client.GetAsync($"/api/v1/tags/{tag.Id}");
        reload.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadJsonAsync<TagResponse>(reload);
        payload.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteTag_WhenMissing_Returns404ProblemDetails()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();

        var response = await client.DeleteAsync($"/api/v1/tags/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await ReadProblemDetailsAsync(response);
        problem.Status.Should().Be((int)HttpStatusCode.NotFound);
    }
}
