using System.Net;
using FluentAssertions;
using TechInventory.Application.Common.Paging;
using TechInventory.Application.Locations;
using TechInventory.Application.Merges;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.IntegrationTests.Controllers;

public sealed class LocationsControllerTests(IntegrationTestFactory<LocationsControllerTests> factory)
    : ControllerTestBase<LocationsControllerTests>(factory), IClassFixture<IntegrationTestFactory<LocationsControllerTests>>
{
    [Fact]
    public async Task CreateLocation_WhenValid_Returns201WithLocation()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();
        var request = new { name = $"Location-{Guid.NewGuid():N}", type = LocationType.Home };

        var response = await client.PostAsync("/api/v1/locations", CreateJsonContent(request));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await ReadJsonAsync<LocationResponse>(response);
        created.Name.Should().Be(request.name);
        created.Type.Should().Be(LocationType.Home.ToString());
        created.IsActive.Should().BeTrue();
        AssertCreatedLocationHeader(response, "/api/v1/locations", created.Id);
    }

    [Fact]
    public async Task GetLocations_WhenDatabaseEmpty_ReturnsEmptyPagedResponse()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/locations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var paged = await ReadJsonAsync<PagedResponse<LocationResponse>>(response);
        paged.Page.Should().Be(1);
        paged.PageSize.Should().Be(25);
        paged.TotalCount.Should().Be(0);
        paged.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLocations_WhenDataExists_ReturnsItemsWithPagination()
    {
        await ResetDatabaseAsync();
        var first = new Location(Guid.NewGuid(), $"Location-{Guid.NewGuid():N}", LocationType.Home);
        var second = new Location(Guid.NewGuid(), $"Location-{Guid.NewGuid():N}", LocationType.Storage);
        await SeedAsync(entities: [first, second]);
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/locations?page=1&pageSize=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var paged = await ReadJsonAsync<PagedResponse<LocationResponse>>(response);
        paged.TotalCount.Should().Be(2);
        paged.Page.Should().Be(1);
        paged.PageSize.Should().Be(1);
        paged.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task GetLocationById_WhenFound_ReturnsLocation()
    {
        await ResetDatabaseAsync();
        var location = new Location(Guid.NewGuid(), $"Location-{Guid.NewGuid():N}", LocationType.Home);
        await SeedAsync(entities: [location]);
        using var client = CreateClient();

        var response = await client.GetAsync($"/api/v1/locations/{location.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadJsonAsync<LocationResponse>(response);
        payload.Id.Should().Be(location.Id);
        payload.Name.Should().Be(location.Name);
        payload.Type.Should().Be(location.Type.ToString());
    }

    [Fact]
    public async Task GetLocationById_WhenMissing_Returns404ProblemDetails()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();

        var response = await client.GetAsync($"/api/v1/locations/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await ReadProblemDetailsAsync(response);
        problem.Status.Should().Be((int)HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateLocation_WhenValid_ReturnsUpdatedLocation()
    {
        await ResetDatabaseAsync();
        var location = new Location(Guid.NewGuid(), $"Location-{Guid.NewGuid():N}", LocationType.Home);
        await SeedAsync(entities: [location]);
        using var client = CreateClient();
        var request = new { id = location.Id, name = $"Updated-{Guid.NewGuid():N}", type = LocationType.Storage };

        var response = await client.PutAsync($"/api/v1/locations/{location.Id}", CreateJsonContent(request));

        await AssertUpdateResponseAsync<LocationResponse>(
            response,
            () => client.GetAsync($"/api/v1/locations/{location.Id}"),
            updated =>
            {
                updated.Id.Should().Be(location.Id);
                updated.Name.Should().Be(request.name);
                updated.Type.Should().Be(LocationType.Storage.ToString());
            });
    }

    [Fact]
    public async Task UpdateLocation_WhenValidationFails_Returns400ValidationProblemDetails()
    {
        await ResetDatabaseAsync();
        var location = new Location(Guid.NewGuid(), $"Location-{Guid.NewGuid():N}", LocationType.Home);
        await SeedAsync(entities: [location]);
        using var client = CreateClient();
        var request = new { id = location.Id, name = string.Empty, type = (LocationType)999 };

        var response = await client.PutAsync($"/api/v1/locations/{location.Id}", CreateJsonContent(request));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await ReadValidationProblemDetailsAsync(response);
        problem.Errors.Keys.Should().Contain(key => string.Equals(key, "Name", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DeleteLocation_WhenFound_Returns204()
    {
        await ResetDatabaseAsync();
        var location = new Location(Guid.NewGuid(), $"Location-{Guid.NewGuid():N}", LocationType.Home);
        await SeedAsync(entities: [location]);
        using var client = CreateClient();

        var response = await client.DeleteAsync($"/api/v1/locations/{location.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var reload = await client.GetAsync($"/api/v1/locations/{location.Id}");
        reload.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadJsonAsync<LocationResponse>(reload);
        payload.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteLocation_WhenMissing_Returns404ProblemDetails()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();

        var response = await client.DeleteAsync($"/api/v1/locations/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await ReadProblemDetailsAsync(response);
        problem.Status.Should().Be((int)HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MergeLocation_WhenSourceHasNoDevices_ReturnsZeroMergedCountAndDeactivatesSource()
    {
        await ResetDatabaseAsync();
        var references = await SeedDeviceReferenceDataAsync();
        var target = new Location(Guid.NewGuid(), $"Target-{Guid.NewGuid():N}", LocationType.Home);
        await SeedAsync(entities: [target]);
        using var client = CreateClient();

        var response = await client.PostAsync(
            "/api/v1/locations/merge",
            CreateJsonContent(new { sourceId = references.Location.Id, targetId = target.Id }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadJsonAsync<MergeReferenceEntityResponse>(response);
        payload.Should().BeEquivalentTo(new MergeReferenceEntityResponse(0, references.Location.Id, target.Id));

        var sourceResponse = await client.GetAsync($"/api/v1/locations/{references.Location.Id}");
        var sourcePayload = await ReadJsonAsync<LocationResponse>(sourceResponse);
        sourcePayload.IsActive.Should().BeFalse();
    }
}
