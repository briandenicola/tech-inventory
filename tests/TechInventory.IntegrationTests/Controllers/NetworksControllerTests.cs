using System.Net;
using FluentAssertions;
using TechInventory.Application.Common.Paging;
using TechInventory.Application.Networks;
using TechInventory.Domain.Entities;

namespace TechInventory.IntegrationTests.Controllers;

public sealed class NetworksControllerTests(IntegrationTestFactory<NetworksControllerTests> factory)
    : ControllerTestBase<NetworksControllerTests>(factory), IClassFixture<IntegrationTestFactory<NetworksControllerTests>>
{
    [Fact]
    public async Task CreateNetwork_WhenValid_Returns201WithLocation()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();
        var request = new { name = $"Network-{Guid.NewGuid():N}", description = "Guest Wi-Fi" };

        var response = await client.PostAsync("/api/v1/networks", CreateJsonContent(request));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await ReadJsonAsync<NetworkResponse>(response);
        created.Name.Should().Be(request.name);
        created.Description.Should().Be(request.description);
        created.IsActive.Should().BeTrue();
        AssertCreatedLocationHeader(response, "/api/v1/networks", created.Id);
    }

    [Fact]
    public async Task GetNetworks_WhenDatabaseEmpty_ReturnsEmptyPagedResponse()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/networks");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var paged = await ReadJsonAsync<PagedResponse<NetworkResponse>>(response);
        paged.Page.Should().Be(1);
        paged.PageSize.Should().Be(25);
        paged.TotalCount.Should().Be(0);
        paged.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetNetworks_WhenDataExists_ReturnsItemsWithPagination()
    {
        await ResetDatabaseAsync();
        var first = new Network(Guid.NewGuid(), $"Network-{Guid.NewGuid():N}", "Primary");
        var second = new Network(Guid.NewGuid(), $"Network-{Guid.NewGuid():N}", "Guest");
        await SeedAsync(entities: [first, second]);
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/networks?page=1&pageSize=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var paged = await ReadJsonAsync<PagedResponse<NetworkResponse>>(response);
        paged.TotalCount.Should().Be(2);
        paged.Page.Should().Be(1);
        paged.PageSize.Should().Be(1);
        paged.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task GetNetworkById_WhenFound_ReturnsNetwork()
    {
        await ResetDatabaseAsync();
        var network = new Network(Guid.NewGuid(), $"Network-{Guid.NewGuid():N}", "Primary");
        await SeedAsync(entities: [network]);
        using var client = CreateClient();

        var response = await client.GetAsync($"/api/v1/networks/{network.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadJsonAsync<NetworkResponse>(response);
        payload.Id.Should().Be(network.Id);
        payload.Name.Should().Be(network.Name);
        payload.Description.Should().Be(network.Description);
    }

    [Fact]
    public async Task GetNetworkById_WhenMissing_Returns404ProblemDetails()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();

        var response = await client.GetAsync($"/api/v1/networks/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await ReadProblemDetailsAsync(response);
        problem.Status.Should().Be((int)HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateNetwork_WhenValid_ReturnsUpdatedNetwork()
    {
        await ResetDatabaseAsync();
        var network = new Network(Guid.NewGuid(), $"Network-{Guid.NewGuid():N}", "Primary");
        await SeedAsync(entities: [network]);
        using var client = CreateClient();
        var request = new { id = network.Id, name = $"Updated-{Guid.NewGuid():N}", description = "Updated description" };

        var response = await client.PutAsync($"/api/v1/networks/{network.Id}", CreateJsonContent(request));

        await AssertUpdateResponseAsync<NetworkResponse>(
            response,
            () => client.GetAsync($"/api/v1/networks/{network.Id}"),
            updated =>
            {
                updated.Id.Should().Be(network.Id);
                updated.Name.Should().Be(request.name);
                updated.Description.Should().Be(request.description);
            });
    }

    [Fact]
    public async Task UpdateNetwork_WhenValidationFails_Returns400ValidationProblemDetails()
    {
        await ResetDatabaseAsync();
        var network = new Network(Guid.NewGuid(), $"Network-{Guid.NewGuid():N}", "Primary");
        await SeedAsync(entities: [network]);
        using var client = CreateClient();
        var request = new { id = network.Id, name = string.Empty, description = new string('d', 1001) };

        var response = await client.PutAsync($"/api/v1/networks/{network.Id}", CreateJsonContent(request));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await ReadValidationProblemDetailsAsync(response);
        problem.Errors.Keys.Should().Contain(key => string.Equals(key, "Name", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DeleteNetwork_WhenFound_Returns204()
    {
        await ResetDatabaseAsync();
        var network = new Network(Guid.NewGuid(), $"Network-{Guid.NewGuid():N}", "Primary");
        await SeedAsync(entities: [network]);
        using var client = CreateClient();

        var response = await client.DeleteAsync($"/api/v1/networks/{network.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var reload = await client.GetAsync($"/api/v1/networks/{network.Id}");
        reload.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadJsonAsync<NetworkResponse>(reload);
        payload.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteNetwork_WhenMissing_Returns404ProblemDetails()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();

        var response = await client.DeleteAsync($"/api/v1/networks/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await ReadProblemDetailsAsync(response);
        problem.Status.Should().Be((int)HttpStatusCode.NotFound);
    }
}
