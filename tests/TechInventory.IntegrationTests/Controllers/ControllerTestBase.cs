using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;
using TechInventory.Domain.ValueObjects;
using TechInventory.Infrastructure.Persistence;

namespace TechInventory.IntegrationTests.Controllers;

public abstract class ControllerTestBase<TMarker>(IntegrationTestFactory<TMarker> factory)
    where TMarker : class
{
    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    protected IntegrationTestFactory<TMarker> Factory { get; } = factory;

    protected HttpClient CreateClient()
    {
        return Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
    }

    protected static JsonContent CreateJsonContent<T>(T payload)
        => JsonContent.Create(payload, options: JsonOptions);

    protected async Task ResetDatabaseAsync(CancellationToken cancellationToken = default)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.EnsureDeletedAsync(cancellationToken);
        await dbContext.Database.MigrateAsync(cancellationToken);
    }

    protected async Task SeedAsync(CancellationToken cancellationToken = default, params object[] entities)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.AddRangeAsync(entities, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    protected async Task<TResult> WithDbContextAsync<TResult>(Func<AppDbContext, Task<TResult>> action)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await action(dbContext);
    }

    protected async Task WithDbContextAsync(Func<AppDbContext, Task> action, CancellationToken cancellationToken = default)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await action(dbContext);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    protected static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        body.Should().NotBeNull();
        return body!;
    }

    protected static async Task<ProblemDetails> ReadProblemDetailsAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem!.Status.Should().Be((int)response.StatusCode);
        problem.Title.Should().NotBeNullOrWhiteSpace();
        problem.Type.Should().NotBeNullOrWhiteSpace();
        return problem;
    }

    protected static async Task<ValidationProblemDetails> ReadValidationProblemDetailsAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem!.Status.Should().Be((int)HttpStatusCode.BadRequest);
        problem.Errors.Should().NotBeNull();
        return problem;
    }

    protected static async Task<TResponse> AssertUpdateResponseAsync<TResponse>(
        HttpResponseMessage response,
        Func<Task<HttpResponseMessage>> reload,
        Action<TResponse> assertResponse)
    {
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var updated = await ReadJsonAsync<TResponse>(response);
            assertResponse(updated);
            return updated;
        }

        var reloadResponse = await reload();
        reloadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var reloaded = await ReadJsonAsync<TResponse>(reloadResponse);
        assertResponse(reloaded);
        return reloaded;
    }

    protected async Task<DeviceReferenceData> SeedDeviceReferenceDataAsync(bool includeTag = false, CancellationToken cancellationToken = default)
    {
        var household = new Household(Guid.NewGuid(), $"Household-{Guid.NewGuid():N}", Currency.From("USD"));
        var brand = new Brand(Guid.NewGuid(), $"Brand-{Guid.NewGuid():N}");
        var category = new Category(Guid.NewGuid(), $"Category-{Guid.NewGuid():N}");
        var owner = new Owner(Guid.NewGuid(), $"Owner-{Guid.NewGuid():N}", OwnerRole.Member);
        var location = new Location(Guid.NewGuid(), $"Location-{Guid.NewGuid():N}", LocationType.Home);
        var network = new Network(Guid.NewGuid(), $"Network-{Guid.NewGuid():N}", "Primary Wi-Fi");
        var tag = includeTag ? new Tag(Guid.NewGuid(), $"Tag-{Guid.NewGuid():N}", "#112233") : null;

        var entities = new List<object> { household, brand, category, owner, location, network };
        if (tag is not null)
        {
            entities.Add(tag);
        }

        await SeedAsync(cancellationToken, [.. entities]);
        return new DeviceReferenceData(household, brand, category, owner, location, network, tag);
    }

    protected static Device CreateDevice(DeviceReferenceData referenceData, string name, DeviceStatus status = DeviceStatus.Active)
    {
        var device = Device.Create(
            Guid.NewGuid(),
            referenceData.Household,
            name,
            referenceData.Brand.Id,
            referenceData.Category.Id,
            referenceData.Owner.Id,
            referenceData.Location.Id,
            model: "Model 1",
            serialNumber: $"SN-{Guid.NewGuid():N}"[..12],
            networkId: referenceData.Network.Id,
            purchaseDate: new DateOnly(2024, 5, 1),
            purchasePrice: 123.45m,
            currency: Currency.From("USD"),
            status: status,
            notes: "integration-test");

        if (status is DeviceStatus.Retired or DeviceStatus.Disposed)
        {
            device.ChangeStatus(status, new DateOnly(2024, 12, 31), "recycled", "apone");
        }

        return device;
    }

    protected static void AssertCreatedLocationHeader(HttpResponseMessage response, string expectedBasePath, Guid entityId)
    {
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().EndWith($"{expectedBasePath}/{entityId}");
    }

    protected sealed record DeviceReferenceData(
        Household Household,
        Brand Brand,
        Category Category,
        Owner Owner,
        Location Location,
        Network Network,
        Tag? Tag);
}
