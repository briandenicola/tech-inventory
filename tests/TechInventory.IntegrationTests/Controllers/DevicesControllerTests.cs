using System.Net;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TechInventory.Application.Common.Paging;
using TechInventory.Application.Devices;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;
using TechInventory.IntegrationTests.Helpers;

namespace TechInventory.IntegrationTests.Controllers;

public sealed class DevicesControllerTests(IntegrationTestFactory<DevicesControllerTests> factory)
    : ControllerTestBase<DevicesControllerTests>(factory), IClassFixture<IntegrationTestFactory<DevicesControllerTests>>
{
    [Fact]
    public async Task CreateDevice_WhenValid_Returns201WithLocation()
    {
        await ResetDatabaseAsync();
        var references = await SeedDeviceReferenceDataAsync();
        using var client = CreateClient();
        var request = new
        {
            name = $"Device-{Guid.NewGuid():N}",
            brandId = references.Brand.Id,
            categoryId = references.Category.Id,
            ownerId = references.Owner.Id,
            locationId = references.Location.Id,
            model = "Steam Deck",
            serialNumber = "SN-100",
            networkId = references.Network.Id,
            purchaseDate = new DateOnly(2024, 5, 1),
            purchasePrice = 499.99m,
            status = DeviceStatus.Active,
            notes = "Portable"
        };

        var response = await client.PostAsync("/api/v1/devices", CreateJsonContent(request));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await ReadJsonAsync<DeviceResponse>(response);
        created.Name.Should().Be(request.name);
        created.BrandId.Should().Be(references.Brand.Id);
        created.CategoryId.Should().Be(references.Category.Id);
        created.OwnerId.Should().Be(references.Owner.Id);
        created.LocationId.Should().Be(references.Location.Id);
        created.NetworkId.Should().Be(references.Network.Id);
        created.CurrencyCode.Should().Be("USD");
        AssertCreatedLocationHeader(response, "/api/v1/devices", created.Id);
    }

    [Fact]
    public async Task GetDevices_WhenDatabaseEmpty_ReturnsEmptyPagedResponse()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/devices");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var paged = await ReadJsonAsync<PagedResponse<DeviceResponse>>(response);
        paged.Page.Should().Be(1);
        paged.PageSize.Should().Be(25);
        paged.TotalCount.Should().Be(0);
        paged.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDevices_WhenDataExists_ReturnsItemsWithPagination()
    {
        await ResetDatabaseAsync();
        var references = await SeedDeviceReferenceDataAsync();
        var first = CreateDevice(references, $"Device-{Guid.NewGuid():N}");
        var second = CreateDevice(references, $"Device-{Guid.NewGuid():N}");
        await SeedAsync(entities: [first, second]);
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/devices?page=1&pageSize=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var paged = await ReadJsonAsync<PagedResponse<DeviceResponse>>(response);
        paged.TotalCount.Should().Be(2);
        paged.Page.Should().Be(1);
        paged.PageSize.Should().Be(1);
        paged.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task GetDeviceById_WhenFound_ReturnsDevice()
    {
        await ResetDatabaseAsync();
        var references = await SeedDeviceReferenceDataAsync();
        var device = CreateDevice(references, $"Device-{Guid.NewGuid():N}");
        await SeedAsync(entities: [device]);
        using var client = CreateClient();

        var response = await client.GetAsync($"/api/v1/devices/{device.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadJsonAsync<DeviceResponse>(response);
        payload.Id.Should().Be(device.Id);
        payload.Name.Should().Be(device.Name);
        payload.OwnerId.Should().Be(device.OwnerId);
    }

    [Fact]
    public async Task GetDeviceById_WhenMissing_Returns404ProblemDetails()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();

        var response = await client.GetAsync($"/api/v1/devices/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await ReadProblemDetailsAsync(response);
        problem.Status.Should().Be((int)HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateDevice_WhenValid_ReturnsUpdatedDevice()
    {
        await ResetDatabaseAsync();
        var references = await SeedDeviceReferenceDataAsync();
        var device = CreateDevice(references, $"Device-{Guid.NewGuid():N}");
        await SeedAsync(entities: [device]);
        using var client = CreateClient();
        var request = new
        {
            id = device.Id,
            name = $"Updated-{Guid.NewGuid():N}",
            brandId = references.Brand.Id,
            categoryId = references.Category.Id,
            ownerId = references.Owner.Id,
            locationId = references.Location.Id,
            currencyCode = "EUR",
            model = "Steam Deck OLED",
            serialNumber = "SN-200",
            networkId = references.Network.Id,
            purchaseDate = new DateOnly(2024, 6, 1),
            purchasePrice = 649.99m,
            status = DeviceStatus.Active,
            notes = "Updated device"
        };

        var response = await client.PutAsync($"/api/v1/devices/{device.Id}", CreateJsonContent(request));

        await AssertUpdateResponseAsync<DeviceResponse>(
            response,
            () => client.GetAsync($"/api/v1/devices/{device.Id}"),
            updated =>
            {
                updated.Id.Should().Be(device.Id);
                updated.Name.Should().Be(request.name);
                updated.CurrencyCode.Should().Be("EUR");
                updated.Model.Should().Be(request.model);
                updated.SerialNumber.Should().Be(request.serialNumber);
            });
    }

    [Fact]
    public async Task UpdateDevice_WhenValidationFails_Returns400ValidationProblemDetails()
    {
        await ResetDatabaseAsync();
        var references = await SeedDeviceReferenceDataAsync();
        var device = CreateDevice(references, $"Device-{Guid.NewGuid():N}");
        await SeedAsync(entities: [device]);
        using var client = CreateClient();
        var request = new
        {
            id = device.Id,
            name = string.Empty,
            brandId = references.Brand.Id,
            categoryId = references.Category.Id,
            ownerId = references.Owner.Id,
            locationId = references.Location.Id,
            currencyCode = "ZZZ",
            model = "Invalid",
            serialNumber = "SN-200",
            networkId = references.Network.Id,
            purchaseDate = new DateOnly(2024, 6, 1),
            purchasePrice = -1m,
            status = DeviceStatus.Active,
            notes = "Invalid"
        };

        var response = await client.PutAsync($"/api/v1/devices/{device.Id}", CreateJsonContent(request));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await ReadValidationProblemDetailsAsync(response);
        problem.Errors.Keys.Should().Contain(key => string.Equals(key, "Name", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DeleteDevice_WhenFound_Returns204()
    {
        await ResetDatabaseAsync();
        var references = await SeedDeviceReferenceDataAsync();
        var device = CreateDevice(references, $"Device-{Guid.NewGuid():N}");
        await SeedAsync(entities: [device]);
        using var client = CreateClient();

        var response = await client.DeleteAsync($"/api/v1/devices/{device.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var reload = await client.GetAsync($"/api/v1/devices/{device.Id}");
        reload.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadJsonAsync<DeviceResponse>(reload);
        payload.Status.Should().Be(DeviceStatus.Disposed.ToString());
    }

    [Fact]
    public async Task DeleteDevice_WhenMissing_Returns404ProblemDetails()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();

        var response = await client.DeleteAsync($"/api/v1/devices/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await ReadProblemDetailsAsync(response);
        problem.Status.Should().Be((int)HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddDeviceTag_WhenValid_ReturnsDeviceTagResponse()
    {
        await ResetDatabaseAsync();
        var references = await SeedDeviceReferenceDataAsync(includeTag: true);
        var device = CreateDevice(references, $"Device-{Guid.NewGuid():N}");
        await SeedAsync(entities: [device]);
        using var client = CreateClient();
        var request = new { tagId = references.Tag!.Id };

        var response = await client.PostAsync($"/api/v1/devices/{device.Id}/tags", CreateJsonContent(request));

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        var payload = await ReadJsonAsync<DeviceTagResponse>(response);
        payload.DeviceId.Should().Be(device.Id);
        payload.TagId.Should().Be(references.Tag!.Id);
        payload.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveDeviceTag_WhenExisting_Returns204()
    {
        await ResetDatabaseAsync();
        var references = await SeedDeviceReferenceDataAsync(includeTag: true);
        var device = CreateDevice(references, $"Device-{Guid.NewGuid():N}");
        var deviceTag = new DeviceTag(device.Id, references.Tag!.Id);
        await SeedAsync(entities: [device, deviceTag]);
        using var client = CreateClient();

        var response = await client.DeleteAsync($"/api/v1/devices/{device.Id}/tags/{references.Tag!.Id}");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);
        await WithDbContextAsync(async dbContext =>
        {
            var savedTag = await dbContext.DeviceTags.SingleAsync(tag => tag.DeviceId == device.Id && tag.TagId == references.Tag!.Id);
            savedTag.IsActive.Should().BeFalse();
        });
    }

    [Fact]
    public async Task ClaimDeviceOwnership_WhenValid_PersistsAuditEvent()
    {
        await ResetDatabaseAsync();
        var references = await SeedDeviceReferenceDataAsync();
        var replacementOwner = new Owner(Guid.NewGuid(), $"Owner-{Guid.NewGuid():N}", OwnerRole.Admin);
        await SeedAsync(entities: [replacementOwner]);
        var device = CreateDevice(references, $"Device-{Guid.NewGuid():N}");
        await SeedAsync(entities: [device]);
        using var client = CreateClient();
        var request = new { ownerId = replacementOwner.Id };

        var response = await client.PatchAsync($"/api/v1/devices/{device.Id}/owner", CreateJsonContent(request));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var reload = await client.GetAsync($"/api/v1/devices/{device.Id}");
        reload.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadJsonAsync<DeviceResponse>(reload);
        payload.OwnerId.Should().Be(replacementOwner.Id);
        await WithDbContextAsync(async dbContext =>
        {
            var auditEvent = await AuditEventAssertionHelper.AssertExistsAsync(
                dbContext,
                nameof(Device),
                device.Id.ToString(),
                AuditAction.Updated);
            auditEvent.BeforePayload.Should().Contain(device.OwnerId.ToString());
        });
    }
}
