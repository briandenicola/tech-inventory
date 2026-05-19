using System.Net;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TechInventory.Application.Common.Paging;
using TechInventory.Application.Owners;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;

namespace TechInventory.IntegrationTests.Controllers;

public sealed class OwnersControllerTests(IntegrationTestFactory<OwnersControllerTests> factory)
    : ControllerTestBase<OwnersControllerTests>(factory), IClassFixture<IntegrationTestFactory<OwnersControllerTests>>
{
    [Fact]
    public async Task CreateOwner_WhenValid_Returns201WithLocation()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();
        var entraObjectId = Guid.NewGuid();
        var request = new { displayName = $"Owner-{Guid.NewGuid():N}", role = OwnerRole.Admin, entraObjectId };

        var response = await client.PostAsync("/api/v1/owners", CreateJsonContent(request));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await ReadJsonAsync<OwnerResponse>(response);
        created.DisplayName.Should().Be(request.displayName);
        created.Role.Should().Be(OwnerRole.Admin.ToString());
        created.EntraObjectId.Should().Be(entraObjectId);
        created.IsActive.Should().BeTrue();
        AssertCreatedLocationHeader(response, "/api/v1/owners", created.Id);
    }

    [Fact]
    public async Task GetOwners_WhenDatabaseEmpty_ReturnsEmptyPagedResponse()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/owners");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var paged = await ReadJsonAsync<PagedResponse<OwnerResponse>>(response);
        paged.Page.Should().Be(1);
        paged.PageSize.Should().Be(25);
        paged.TotalCount.Should().Be(0);
        paged.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOwners_WhenDataExists_ReturnsItemsWithPagination()
    {
        await ResetDatabaseAsync();
        var first = new Owner(Guid.NewGuid(), $"Owner-{Guid.NewGuid():N}", OwnerRole.Member);
        var second = new Owner(Guid.NewGuid(), $"Owner-{Guid.NewGuid():N}", OwnerRole.Admin);
        await SeedAsync(entities: [first, second]);
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/owners?page=1&pageSize=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var paged = await ReadJsonAsync<PagedResponse<OwnerResponse>>(response);
        paged.TotalCount.Should().Be(2);
        paged.Page.Should().Be(1);
        paged.PageSize.Should().Be(1);
        paged.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task GetOwnerById_WhenFound_ReturnsOwner()
    {
        await ResetDatabaseAsync();
        var owner = new Owner(Guid.NewGuid(), $"Owner-{Guid.NewGuid():N}", OwnerRole.Member, Guid.NewGuid());
        await SeedAsync(entities: [owner]);
        using var client = CreateClient();

        var response = await client.GetAsync($"/api/v1/owners/{owner.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadJsonAsync<OwnerResponse>(response);
        payload.Id.Should().Be(owner.Id);
        payload.DisplayName.Should().Be(owner.DisplayName);
        payload.Role.Should().Be(owner.Role.ToString());
        payload.EntraObjectId.Should().Be(owner.EntraObjectId);
    }

    [Fact]
    public async Task GetOwnerById_WhenMissing_Returns404ProblemDetails()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();

        var response = await client.GetAsync($"/api/v1/owners/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await ReadProblemDetailsAsync(response);
        problem.Status.Should().Be((int)HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateOwner_WhenValid_ReturnsUpdatedOwner()
    {
        await ResetDatabaseAsync();
        var owner = new Owner(Guid.NewGuid(), $"Owner-{Guid.NewGuid():N}", OwnerRole.Member);
        await SeedAsync(entities: [owner]);
        using var client = CreateClient();
        var entraObjectId = Guid.NewGuid();
        var request = new { id = owner.Id, displayName = $"Updated-{Guid.NewGuid():N}", role = OwnerRole.Admin, entraObjectId };

        var response = await client.PutAsync($"/api/v1/owners/{owner.Id}", CreateJsonContent(request));

        await AssertUpdateResponseAsync<OwnerResponse>(
            response,
            () => client.GetAsync($"/api/v1/owners/{owner.Id}"),
            updated =>
            {
                updated.Id.Should().Be(owner.Id);
                updated.DisplayName.Should().Be(request.displayName);
                updated.Role.Should().Be(OwnerRole.Admin.ToString());
                updated.EntraObjectId.Should().Be(entraObjectId);
            });
    }

    [Fact]
    public async Task UpdateOwner_WhenValidationFails_Returns400ValidationProblemDetails()
    {
        await ResetDatabaseAsync();
        var owner = new Owner(Guid.NewGuid(), $"Owner-{Guid.NewGuid():N}", OwnerRole.Member);
        await SeedAsync(entities: [owner]);
        using var client = CreateClient();
        var request = new { id = owner.Id, displayName = string.Empty, role = (OwnerRole)999, entraObjectId = Guid.Empty };

        var response = await client.PutAsync($"/api/v1/owners/{owner.Id}", CreateJsonContent(request));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await ReadValidationProblemDetailsAsync(response);
        problem.Errors.Keys.Should().Contain(key => string.Equals(key, "DisplayName", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DeleteOwner_WhenReferencedByDevice_Returns409ConflictProblemDetails()
    {
        await ResetDatabaseAsync();
        var references = await SeedDeviceReferenceDataAsync();
        var device = CreateDevice(references, $"Device-{Guid.NewGuid():N}");
        await SeedAsync(entities: [device]);
        using var client = CreateClient();

        var response = await client.DeleteAsync($"/api/v1/owners/{references.Owner.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await ReadProblemDetailsAsync(response);
        problem.Status.Should().Be((int)HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeleteOwner_WhenMissing_Returns404ProblemDetails()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();

        var response = await client.DeleteAsync($"/api/v1/owners/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await ReadProblemDetailsAsync(response);
        problem.Status.Should().Be((int)HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCurrentOwner_WithDevBypass_AutoProvisionsOnFirstCall_AndReturnsSameOwnerOnSecondCall()
    {
        await ResetDatabaseAsync();
        var devEntraObjectId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        using var client = CreateClient();

        var firstResponse = await client.GetAsync("/api/v1/owners/me");
        var secondResponse = await client.GetAsync("/api/v1/owners/me");

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var firstPayload = await ReadJsonAsync<OwnerResponse>(firstResponse);
        var secondPayload = await ReadJsonAsync<OwnerResponse>(secondResponse);

        firstPayload.EntraObjectId.Should().Be(devEntraObjectId);
        firstPayload.DisplayName.Should().Be("dev-admin");
        firstPayload.Role.Should().Be(OwnerRole.Admin.ToString());

        secondPayload.Id.Should().Be(firstPayload.Id);
        secondPayload.EntraObjectId.Should().Be(devEntraObjectId);
        secondPayload.DisplayName.Should().Be("dev-admin");
        secondPayload.Role.Should().Be(OwnerRole.Admin.ToString());

        await WithDbContextAsync(async dbContext =>
        {
            var owners = await dbContext.Owners
                .Where(owner => owner.EntraObjectId == devEntraObjectId)
                .ToListAsync();

            owners.Should().ContainSingle();
            owners[0].Id.Should().Be(firstPayload.Id);
            owners[0].Role.Should().Be(OwnerRole.Admin);
        });
    }
}
