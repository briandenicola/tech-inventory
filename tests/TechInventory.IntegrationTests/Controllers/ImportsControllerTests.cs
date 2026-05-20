using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TechInventory.Application.Imports;
using TechInventory.Domain.Entities;
using TechInventory.Domain.Enums;
using TechInventory.Domain.ValueObjects;
using TechInventory.IntegrationTests.Helpers;

namespace TechInventory.IntegrationTests.Controllers;

public sealed class ImportsControllerTests(IntegrationTestFactory<ImportsControllerTests> factory)
    : ControllerTestBase<ImportsControllerTests>(factory), IClassFixture<IntegrationTestFactory<ImportsControllerTests>>
{
    private static readonly string ImportArtifactsDirectory = Path.Combine(AppContext.BaseDirectory, "import-artifacts");

    [Fact]
    public async Task PreviewImport_WhenCsvIsValid_ReturnsPreviewWithoutPersisting()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();
        using var content = await CreateMultipartFileContentAsync("devices-valid.csv", CreateValidImportCsv());

        var response = await client.PostAsync("/api/v1/imports/preview", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadJsonNodeAsync(response);
        CountRows(payload, "validRows", "valid").Should().BeGreaterThan(0);
        CountRows(payload, "invalidRows", "invalid").Should().Be(0);
        GetArrayProperty(payload, "lookupsToCreate", "lookups").Should().NotBeNull();

        await WithDbContextAsync(async dbContext =>
        {
            (await dbContext.Devices.CountAsync()).Should().Be(0);
            (await dbContext.ImportBatches.CountAsync()).Should().Be(0);
        });
    }

    [Fact]
    public async Task PreviewImport_WhenCsvIsMalformed_ReturnsInvalidRows()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();
        using var content = await CreateMultipartFileContentAsync("devices-malformed.csv", CreateMalformedImportCsv());

        var response = await client.PostAsync("/api/v1/imports/preview", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadJsonNodeAsync(response);
        CountRows(payload, "invalidRows", "invalid").Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task PreviewImport_WhenCsvReferencesMissingLookups_ReturnsLookupsToCreate()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();
        using var content = await CreateMultipartFileContentAsync("devices-missing-lookups.csv", CreateMissingLookupCsv());

        var response = await client.PostAsync("/api/v1/imports/preview", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadJsonNodeAsync(response);
        var lookupsToCreate = GetArrayProperty(payload, "lookupsToCreate", "lookups");
        lookupsToCreate.Should().NotBeNull();
        lookupsToCreate!.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task PreviewImport_WhenFileExceedsTenMegabytes_Returns413ProblemDetails()
    {
        await using var limitedFactory = new ImportSizeLimitFactory();
        using var client = limitedFactory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
        using var content = CreateMultipartContentFromBytes(new byte[513], "oversized.csv");

        var response = await client.PostAsync("/api/v1/imports/preview", content);

        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
        var problem = await ReadProblemDetailsAsync(response);
        problem.Status.Should().Be((int)HttpStatusCode.RequestEntityTooLarge);
    }

    [Fact]
    public async Task CommitImport_WhenCsvIsValid_Returns201AndPersistsDevicesBatchAndAuditEvents()
    {
        await ResetDatabaseAsync();
        var references = await SeedDeviceReferenceDataAsync();
        using var client = CreateClient();
        using var content = await CreateMultipartFileContentAsync("devices-valid.csv", CreateValidImportCsv(
            references.Brand.Name,
            references.Category.Name,
            references.Owner.DisplayName,
            references.Location.Name));

        var response = await client.PostAsync("/api/v1/imports/commit", content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var batchId = ExtractBatchId(response);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().EndWith($"/api/v1/imports/{batchId}");

        await WithDbContextAsync(async dbContext =>
        {
            var devices = await dbContext.Devices.OrderBy(device => device.Name).ToListAsync();
            devices.Should().HaveCount(3);

            var importBatch = await dbContext.ImportBatches.SingleAsync(batch => batch.Id == batchId);
            importBatch.RowCount.Should().Be(3);
            importBatch.SuccessCount.Should().Be(3);
            importBatch.ErrorCount.Should().Be(0);

            foreach (var device in devices)
            {
                await AuditEventAssertionHelper.AssertExistsAsync(
                    dbContext,
                    nameof(Device),
                    device.Id.ToString(),
                    AuditAction.Created,
                    cancellationToken: default);
            }
        });
    }

    [Fact]
    public async Task CommitImport_WhenOwnerColumnIsMissing_DefaultsToCurrentImporter()
    {
        await ResetDatabaseAsync();
        var references = await SeedDeviceReferenceDataAsync();
        using var client = CreateClient();

        var provisionResponse = await client.GetAsync("/api/v1/owners/me");
        provisionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var currentOwner = await ReadJsonAsync<TechInventory.Application.Owners.OwnerResponse>(provisionResponse);

        using var content = await CreateMultipartFileContentAsync(
            "devices-owner-omitted.csv",
            CreateCsvWithoutOwnerColumn(references.Brand.Name, references.Category.Name, references.Location.Name));

        var response = await client.PostAsync("/api/v1/imports/commit", content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var payload = await ReadJsonAsync<CommitImportResult>(response);
        payload.ImportedRows.Should().Be(2);
        payload.InvalidRows.Should().Be(0);

        await WithDbContextAsync(async dbContext =>
        {
            var imported = await dbContext.Devices
                .Where(device => device.Name == "Owner-Less Device A" || device.Name == "Owner-Less Device B")
                .ToListAsync();
            imported.Should().HaveCount(2);
            imported.Should().OnlyContain(device => device.OwnerId == currentOwner.Id);
        });
    }

    [Fact]
    public async Task CommitImport_WhenCsvReferencesMissingLookups_AutoCreatesLookups()
    {
        await ResetDatabaseAsync();
        await SeedAsync(entities: [new Household(Guid.NewGuid(), $"Household-{Guid.NewGuid():N}", Currency.From("USD"))]);
        using var client = CreateClient();
        using var content = await CreateMultipartFileContentAsync("devices-missing-lookups.csv", CreateMissingLookupCsv());

        var response = await client.PostAsync("/api/v1/imports/commit", content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var payload = await ReadJsonAsync<CommitImportResult>(response);
        payload.ImportedRows.Should().Be(1);
        payload.InvalidRows.Should().Be(0);

        await WithDbContextAsync(async dbContext =>
        {
            (await dbContext.Brands.CountAsync()).Should().BeGreaterThan(0);
            (await dbContext.Categories.CountAsync()).Should().BeGreaterThan(0);
            (await dbContext.Owners.CountAsync()).Should().BeGreaterThan(0);
            (await dbContext.Locations.CountAsync()).Should().BeGreaterThan(0);
            (await dbContext.Devices.CountAsync()).Should().Be(1);
        });
    }

    [Fact]
    public async Task CommitImport_WhenCsvIsMalformed_ReturnsProblemDetailsOrInvalidRowSummary()
    {
        await ResetDatabaseAsync();
        await SeedAsync(entities: [new Household(Guid.NewGuid(), $"Household-{Guid.NewGuid():N}", Currency.From("USD"))]);
        using var client = CreateClient();
        using var content = await CreateMultipartFileContentAsync("devices-malformed.csv", CreateMalformedImportCsv());

        var response = await client.PostAsync("/api/v1/imports/commit", content);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Created);

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problem = await ReadProblemDetailsAsync(response);
            problem.Status.Should().Be((int)HttpStatusCode.BadRequest);
            return;
        }

        var payload = await ReadJsonAsync<CommitImportResult>(response);
        payload.InvalidRows.Should().BeGreaterThan(0);
        payload.FailedRows.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetImports_WhenBatchesExist_ReturnsPagedResultsOrderedDescendingByTimestamp()
    {
        await ResetDatabaseAsync();
        var older = new ImportBatch(Guid.NewGuid(), "older.csv", 2, 2, 0, ImportStatus.Completed, DateTimeOffset.UtcNow.AddMinutes(-5), "apone");
        var newer = new ImportBatch(Guid.NewGuid(), "newer.csv", 4, 3, 1, ImportStatus.PartialSuccess, DateTimeOffset.UtcNow, "apone");
        await SeedAsync(entities: [older, newer]);
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/imports?page=1&pageSize=25");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadJsonNodeAsync(response);
        var items = GetArrayProperty(payload, "items");
        items.Should().NotBeNull();
        items!.Count.Should().Be(2);
        GetGuidProperty(items[0]!, "id").Should().Be(newer.Id);
        GetGuidProperty(items[1]!, "id").Should().Be(older.Id);
    }

    [Fact]
    public async Task GetImportById_WhenFound_ReturnsBatchDetail()
    {
        await ResetDatabaseAsync();
        var batch = new ImportBatch(Guid.NewGuid(), "detail.csv", 3, 2, 1, ImportStatus.PartialSuccess, DateTimeOffset.UtcNow, "apone", "[]");
        await SeedAsync(entities: [batch]);
        using var client = CreateClient();

        var response = await client.GetAsync($"/api/v1/imports/{batch.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadJsonNodeAsync(response);
        GetGuidProperty(payload, "id").Should().Be(batch.Id);
        GetStringProperty(payload, "fileName").Should().Be(batch.FileName);
    }

    [Fact]
    public async Task GetImportById_WhenMissing_Returns404ProblemDetails()
    {
        await ResetDatabaseAsync();
        using var client = CreateClient();

        var response = await client.GetAsync($"/api/v1/imports/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await ReadProblemDetailsAsync(response);
        problem.Status.Should().Be((int)HttpStatusCode.NotFound);
    }

    private static async Task<MultipartFormDataContent> CreateMultipartFileContentAsync(string fileName, string csvContent)
    {
        Directory.CreateDirectory(ImportArtifactsDirectory);
        var path = Path.Combine(ImportArtifactsDirectory, fileName);
        await File.WriteAllTextAsync(path, csvContent, Encoding.UTF8);

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(path));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", fileName);
        return content;
    }

    private static MultipartFormDataContent CreateMultipartContentFromBytes(byte[] fileBytes, string fileName)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", fileName);
        return content;
    }

    private static string CreateValidImportCsv(
        string brand = "Existing Brand",
        string category = "Existing Category",
        string owner = "Existing Owner",
        string location = "Existing Location")
        => string.Join(
            Environment.NewLine,
            "Title,Brand,Model,Serial Number,Category,Owner,Location,Purchase Date,Purchase Price,Status,Notes",
            $"Steam Deck,{brand},OLED,SN-100,{category},{owner},{location},2024-05-01,549.99,Active,Portable gaming",
            $"Family iPad,{brand},Mini,SN-101,{category},{owner},{location},2023-08-20,399.99,Active,Shared tablet",
            $"Office Laptop,{brand},X1 Carbon,SN-102,{category},{owner},{location},2022-11-10,1499.00,Active,Work device");

    private static string CreateMalformedImportCsv()
        => string.Join(
            Environment.NewLine,
            "Title,Brand,Model,Serial Number,Category,Owner,Location,Purchase Date,Purchase Price,Status,Notes",
            "Broken Row,Brand X,Model X,SN-200,Category X,Owner X",
            "Another Broken Row");

    private static string CreateMissingLookupCsv()
        => string.Join(
            Environment.NewLine,
            "Title,Brand,Model,Serial Number,Category,Owner,Location,Purchase Date,Purchase Price,Status,Notes",
            "Mystery Device,Unknown Brand,Model Z,SN-300,Unknown Category,Unknown Owner,Unknown Location,2024-01-01,123.45,Active,Needs lookup creation");

    private static string CreateCsvWithoutOwnerColumn(string brand, string category, string location)
        => string.Join(
            Environment.NewLine,
            "Title,Brand,Model,Serial Number,Category,Location,Purchase Date,Purchase Price,Status,Notes",
            $"Owner-Less Device A,{brand},Air,SN-A1,{category},{location},2024-03-15,799.00,Active,Imported without owner",
            $"Owner-Less Device B,{brand},Pro,SN-A2,{category},{location},2024-04-22,1099.00,Active,Imported without owner");

    private static async Task<JsonNode> ReadJsonNodeAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        JsonNode.Parse(body).Should().NotBeNull();
        return JsonNode.Parse(body)!;
    }

    private static int CountRows(JsonNode node, params string[] candidatePropertyNames)
    {
        foreach (var propertyName in candidatePropertyNames)
        {
            if (TryGetProperty(node, propertyName, out var value))
            {
                if (value is JsonArray array)
                {
                    return array.Count;
                }

                if (value is JsonValue scalar && scalar.TryGetValue<int>(out var count))
                {
                    return count;
                }
            }
        }

        return 0;
    }

    private static JsonArray? GetArrayProperty(JsonNode node, params string[] candidatePropertyNames)
    {
        foreach (var propertyName in candidatePropertyNames)
        {
            if (TryGetProperty(node, propertyName, out var value) && value is JsonArray array)
            {
                return array;
            }
        }

        return null;
    }

    private static Guid ExtractBatchId(HttpResponseMessage response)
    {
        response.Headers.Location.Should().NotBeNull();
        var segments = response.Headers.Location!.OriginalString.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Guid.TryParse(segments.Last(), out var batchId).Should().BeTrue();
        return batchId;
    }

    private static Guid GetGuidProperty(JsonNode node, params string[] candidatePropertyNames)
    {
        var rawValue = GetStringProperty(node, candidatePropertyNames);
        Guid.TryParse(rawValue, out var value).Should().BeTrue();
        return value;
    }

    private static string GetStringProperty(JsonNode node, params string[] candidatePropertyNames)
    {
        foreach (var propertyName in candidatePropertyNames)
        {
            if (TryGetProperty(node, propertyName, out var value) && value is not null)
            {
                return value.ToString();
            }
        }

        throw new InvalidOperationException($"None of the expected properties were present: {string.Join(", ", candidatePropertyNames)}.");
    }

    private static bool TryGetProperty(JsonNode node, string propertyName, out JsonNode? value)
    {
        value = null;

        if (node is not JsonObject jsonObject)
        {
            return false;
        }

        foreach (var candidate in jsonObject)
        {
            if (string.Equals(candidate.Key, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = candidate.Value;
                return true;
            }
        }

        return false;
    }

    private sealed class ImportSizeLimitFactory : IntegrationTestFactory<ImportSizeLimitFactory>
    {
        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Imports:MaxFileSizeBytes"] = "512"
                });
            });
        }
    }
}
