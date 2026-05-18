namespace TechInventory.IntegrationTests;

/// <summary>
/// Integration smoke test that will verify GET /health returns 200 OK once Hicks's API is wired.
/// Uses WebApplicationFactory to boot the API in-proc.
/// </summary>
public class ApiSmokeTests
{
    [Fact(Skip = "Awaiting Hicks's /health endpoint wiring in TechInventory.Api")]
    public async Task HealthEndpoint_Returns200Ok()
    {
        // Arrange
        // TODO: var factory = new WebApplicationFactory<Program>();
        // var client = factory.CreateClient();

        // Act
        // var response = await client.GetAsync("/health");

        // Assert
        // response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
