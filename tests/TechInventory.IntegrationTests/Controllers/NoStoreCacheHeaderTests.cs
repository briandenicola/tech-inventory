using System.Net;
using FluentAssertions;

namespace TechInventory.IntegrationTests.Controllers;

/// <summary>
/// Every <c>/api/v1/*</c> response must carry <c>Cache-Control: no-store</c>.
/// </summary>
/// <remarks>
/// <para>
/// The API used to send no cache directives at all. With neither
/// <c>Cache-Control</c> nor a validator, a browser or intermediary may apply
/// heuristic freshness and hand back a stale device list — the user sees their
/// own edit missing even when the service worker behaves.
/// </para>
/// <para>
/// TAMPER-TESTED: removing <c>app.UseMiddleware&lt;NoStoreCacheHeaderMiddleware&gt;()</c>
/// from <c>Program.cs</c> fails all four <see cref="ApiReads_AreNeverStored"/> cases;
/// restoring it makes them pass again. <see cref="ApiErrors_AreNeverStored"/> passes
/// either way — the error pipeline already emits <c>no-store</c> without this
/// middleware — so it pins a contract this middleware must not regress rather than
/// one it establishes. <see cref="NonApiPaths_KeepTheirOwnCacheHeaders"/> likewise
/// guards the prefix, not the header.
/// </para>
/// </remarks>
public sealed class NoStoreCacheHeaderTests(IntegrationTestFactory<NoStoreCacheHeaderTests> factory)
    : ControllerTestBase<NoStoreCacheHeaderTests>(factory), IClassFixture<IntegrationTestFactory<NoStoreCacheHeaderTests>>
{
    [Theory]
    [InlineData("/api/v1/devices")]
    [InlineData("/api/v1/brands")]
    [InlineData("/api/v1/categories")]
    [InlineData("/api/v1/audit-events")]
    public async Task ApiReads_AreNeverStored(string path)
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();

        var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.CacheControl.Should().NotBeNull();
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
    }

    [Fact]
    public async Task ApiErrors_AreNeverStored()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();

        // A 404 body is as cacheable as a 200 to a heuristic cache, and a stale
        // "not found" for a device that now exists is the same defect.
        var response = await client.GetAsync($"/api/v1/devices/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Headers.CacheControl.Should().NotBeNull();
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
    }

    [Fact]
    public async Task NonApiPaths_KeepTheirOwnCacheHeaders()
    {
        using var client = CreateClient();

        // /health sets its own "no-store, no-cache" (ASP.NET Core's health-check
        // default). This middleware writes a bare "no-store", so the surviving
        // no-cache component proves it did not reach outside /api/v1 and clobber
        // another component's header — the prefix check cannot quietly widen.
        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.CacheControl.Should().NotBeNull();
        response.Headers.CacheControl!.NoCache.Should().BeTrue();
    }
}
