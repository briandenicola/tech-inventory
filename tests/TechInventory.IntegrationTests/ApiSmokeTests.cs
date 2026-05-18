using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TechInventory.IntegrationTests;

public sealed class ApiSmokeTests(IntegrationTestFactory<ApiSmokeTests> factory)
    : IClassFixture<IntegrationTestFactory<ApiSmokeTests>>
{
    [Fact]
    public async Task HealthEndpoint_Returns200Ok()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
