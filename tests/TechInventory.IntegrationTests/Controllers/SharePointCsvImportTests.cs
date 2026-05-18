using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TechInventory.Domain.Enums;
using TechInventory.IntegrationTests.Helpers;

namespace TechInventory.IntegrationTests.Controllers;

public sealed class SharePointCsvImportTests(IntegrationTestFactory<SharePointCsvImportTests> factory)
    : ControllerTestBase<SharePointCsvImportTests>(factory), IClassFixture<IntegrationTestFactory<SharePointCsvImportTests>>
{
    private static readonly string ImportArtifactsDirectory = Path.Combine(AppContext.BaseDirectory, "import-artifacts");
    private static readonly string SampleCsvPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "TechInventory.IntegrationTests", "Imports", "SampleData", "devices-sample.csv");

    [Fact]
    public async Task CommitImport_SharePointCsv_ProcessesAllStatusMappingsAndExtendedFields()
    {
        await ResetDatabaseAsync();
        await SeedDeviceReferenceDataAsync();
        using var client = CreateClient();

        if (!File.Exists(SampleCsvPath))
        {
            throw new FileNotFoundException($"Sample CSV not found at {SampleCsvPath}");
        }

        var csvContent = await File.ReadAllTextAsync(SampleCsvPath, Encoding.UTF8);
        using var content = await CreateMultipartFileContentAsync("devices-sample.csv", csvContent);

        var response = await client.PostAsync("/api/v1/imports/commit", content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var payload = await ReadJsonNodeAsync(response);

        CountRows(payload, "totalRows", "total").Should().Be(10);
        CountRows(payload, "importedRows", "imported").Should().Be(9);
        CountRows(payload, "invalidRows", "invalid").Should().Be(1);

        await WithDbContextAsync(async dbContext =>
        {
            var devices = await dbContext.Devices.ToListAsync();
            devices.Count.Should().Be(9);

            var brands = await dbContext.Brands.ToListAsync();
            var categories = await dbContext.Categories.ToListAsync();
            var locations = await dbContext.Locations.ToListAsync();
            var networks = await dbContext.Networks.ToListAsync();

            var disposedDevice = devices.FirstOrDefault(d => d.Name == "Test Router 1");
            disposedDevice.Should().NotBeNull();
            disposedDevice!.Status.Should().Be(DeviceStatus.Disposed);
            disposedDevice.DisposalMethod.Should().Be("sold to neighbor");
            disposedDevice.Purpose.Should().Be("sold to neighbor");
            disposedDevice.IpAddress.Should().Be("192.168.1.1");
            disposedDevice.MacAddress.Should().Be("AA:BB:CC:DD:EE:F1");
            disposedDevice.OperatingSystem.Should().Be("RouterOS");
            disposedDevice.Version.Should().Be("v7.2");
            disposedDevice.ProductUrl.Should().Be("https://eero.com/shop/eero-pro-6");
            disposedDevice.NetworkId.Should().NotBeNull();
            var disposedNetwork = networks.First(n => n.Id == disposedDevice.NetworkId);
            disposedNetwork.Name.Should().Be("Eero");

            var donatedDevice = devices.FirstOrDefault(d => d.Name == "Sample Laptop");
            donatedDevice.Should().NotBeNull();
            donatedDevice!.Status.Should().Be(DeviceStatus.Disposed);
            donatedDevice.DisposalMethod.Should().Be("given to John");
            donatedDevice.MacAddress.Should().Be("00:11:22:33:44:55");

            var retiredDevice = devices.FirstOrDefault(d => d.Name == "Backup Server");
            retiredDevice.Should().NotBeNull();
            retiredDevice!.Status.Should().Be(DeviceStatus.Retired);
            retiredDevice.Purpose.Should().Be("backup spare");
            retiredDevice.RetiredDate.Should().NotBeNull();
            retiredDevice.DisposalMethod.Should().BeNull();
            retiredDevice.MacAddress.Should().Be("AA:BB:CC:DD:EE:FF");

            var activeDevice = devices.FirstOrDefault(d => d.Name == "Smart Display");
            activeDevice.Should().NotBeNull();
            activeDevice!.Status.Should().Be(DeviceStatus.Active);
            activeDevice.ProductUrl.Should().Be("https://amazon.com/echo-show");

            var naNetworkDevice = devices.FirstOrDefault(d => d.Name == "Test Switch");
            naNetworkDevice.Should().NotBeNull();
            naNetworkDevice!.NetworkId.Should().BeNull();

            var zWaveDevice = devices.FirstOrDefault(d => d.Name == "IoT Sensor");
            zWaveDevice.Should().NotBeNull();
            zWaveDevice!.NetworkId.Should().NotBeNull();
            var zWaveNetwork = networks.First(n => n.Id == zWaveDevice.NetworkId);
            zWaveNetwork.Name.Should().Be("z-wave");

            var eeroDevices = devices.Where(d => d.Name.StartsWith("Eero Mesh")).ToList();
            eeroDevices.Count.Should().Be(2);
            var eeroBrand = brands.First(b => b.Name == "Eero");
            eeroDevices.All(d => d.BrandId == eeroBrand.Id).Should().BeTrue();

            var badMacDevice = devices.FirstOrDefault(d => d.Name == "Bad MAC Device");
            badMacDevice.Should().BeNull();

            var whitespaceDevice = devices.FirstOrDefault(d => d.Name == "Whitespace Date");
            whitespaceDevice.Should().NotBeNull();
            whitespaceDevice!.PurchaseDate.Should().Be(new DateOnly(2024, 7, 15));

            var eeroNetwork = networks.FirstOrDefault(n => n.Name == "Eero");
            eeroNetwork.Should().NotBeNull();

            networks.Any(n => n.Name == "z-wave").Should().BeTrue();
        });
    }

    [Fact]
    public async Task PreviewImport_SharePointCsv_ReturnsValidAndInvalidRows()
    {
        await ResetDatabaseAsync();
        await SeedDeviceReferenceDataAsync();
        using var client = CreateClient();

        if (!File.Exists(SampleCsvPath))
        {
            throw new FileNotFoundException($"Sample CSV not found at {SampleCsvPath}");
        }

        var csvContent = await File.ReadAllTextAsync(SampleCsvPath, Encoding.UTF8);
        using var content = await CreateMultipartFileContentAsync("devices-sample.csv", csvContent);

        var response = await client.PostAsync("/api/v1/imports/preview", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadJsonNodeAsync(response);

        CountRows(payload, "totalRows", "total").Should().Be(10);
        CountRows(payload, "validRows", "valid").Should().Be(9);
        CountRows(payload, "invalidRows", "invalid").Should().Be(1);

        var invalidRows = GetArrayProperty(payload, "invalidRows", "invalid");
        invalidRows.Should().NotBeNull();
        invalidRows!.Count.Should().Be(1);

        var lookupsToCreate = GetArrayProperty(payload, "lookupsToCreate", "lookups");
        lookupsToCreate.Should().NotBeNull();
        lookupsToCreate!.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CommitImport_SharePointCsv_IdempotentReimport_CreatesNoDuplicates()
    {
        await ResetDatabaseAsync();
        await SeedDeviceReferenceDataAsync();
        using var client = CreateClient();

        if (!File.Exists(SampleCsvPath))
        {
            throw new FileNotFoundException($"Sample CSV not found at {SampleCsvPath}");
        }

        var csvContent = await File.ReadAllTextAsync(SampleCsvPath, Encoding.UTF8);

        using (var content1 = await CreateMultipartFileContentAsync("devices-sample-1.csv", csvContent))
        {
            var response1 = await client.PostAsync("/api/v1/imports/commit", content1);
            response1.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        var brandCountAfterFirstImport = 0;
        var categoryCountAfterFirstImport = 0;
        var locationCountAfterFirstImport = 0;
        var networkCountAfterFirstImport = 0;

        await WithDbContextAsync(async dbContext =>
        {
            brandCountAfterFirstImport = await dbContext.Brands.CountAsync();
            categoryCountAfterFirstImport = await dbContext.Categories.CountAsync();
            locationCountAfterFirstImport = await dbContext.Locations.CountAsync();
            networkCountAfterFirstImport = await dbContext.Networks.CountAsync();
        });

        using (var content2 = await CreateMultipartFileContentAsync("devices-sample-2.csv", csvContent))
        {
            var response2 = await client.PostAsync("/api/v1/imports/commit", content2);
            response2.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        await WithDbContextAsync(async dbContext =>
        {
            (await dbContext.Brands.CountAsync()).Should().Be(brandCountAfterFirstImport);
            (await dbContext.Categories.CountAsync()).Should().Be(categoryCountAfterFirstImport);
            (await dbContext.Locations.CountAsync()).Should().Be(locationCountAfterFirstImport);
            (await dbContext.Networks.CountAsync()).Should().Be(networkCountAfterFirstImport);

            (await dbContext.Devices.CountAsync()).Should().Be(18);
        });
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
}
