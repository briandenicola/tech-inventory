# SKILL: SQLite IntegrationTestFactory

**Purpose:** Reuse a `WebApplicationFactory<Program>` pattern that gives each integration test class its own SQLite database file, applies migrations when available, and cleans up after itself.

## When to use
- ASP.NET Core integration tests in this repo need a real database but the app architecture is SQLite-only
- You want `WebApplicationFactory` realism without Testcontainers overhead
- The suite should be ready before EF Core migrations fully land

## Inputs
- A test class name to use as the marker type (`IntegrationTestFactory<TMarker>`)
- `ConnectionStrings:Default` support in the API host
- Optional EF Core `DbContext` registration and migrations

## Outputs
- One SQLite file per test class under the integration test output directory
- Automatic `Database.Migrate()` when a registered `DbContext` with migrations is present
- Cleanup of `.sqlite`, `-wal`, and `-shm` files on factory disposal

## Pattern
1. Create `IntegrationTestFactory<TMarker> : WebApplicationFactory<Program>`.
2. Override app configuration to inject `ConnectionStrings:Default` with a unique file path.
3. In `ConfigureServices`, record any registered `DbContext` types.
4. In `CreateHost`, resolve those contexts and call `Database.Migrate()` only when migrations exist.
5. In `Dispose` / `DisposeAsync`, delete the SQLite database files.
6. In tests, implement `IClassFixture<IntegrationTestFactory<MyTests>>` and build clients from the injected factory.

## Handoff hook
- If Hicks lands `AppDbContext` without using `ConnectionStrings:Default`, update the TODO in `IntegrationTestFactory.ConfigureServices(...)` to replace that registration explicitly.
- Apone should keep new integration test classes on this fixture so each class gets its own isolated DB.

## Example
```csharp
public sealed class DevicesApiTests(IntegrationTestFactory<DevicesApiTests> factory)
    : IClassFixture<IntegrationTestFactory<DevicesApiTests>>
{
    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
    }
}
```

## Anti-patterns
- Do **not** add a SQLite container just to satisfy the word "integration"
- Do **not** use `EnsureCreated()` once real migrations exist; keep schema changes migration-driven
- Do **not** share one database file across the whole suite unless a specific test collection needs it
