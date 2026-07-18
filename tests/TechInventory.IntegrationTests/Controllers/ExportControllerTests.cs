using System.Globalization;
using System.Net;
using CsvHelper;
using FluentAssertions;
using TechInventory.Application.Common.Paging;
using TechInventory.Application.Devices;
using TechInventory.Application.Exports;
using TechInventory.Domain.Enums;

namespace TechInventory.IntegrationTests.Controllers;

public sealed class ExportControllerTests(IntegrationTestFactory<ExportControllerTests> factory)
    : ControllerTestBase<ExportControllerTests>(factory), IClassFixture<IntegrationTestFactory<ExportControllerTests>>
{
    [Fact]
    public async Task ExportDevicesAsCsv_WhenRequested_ReturnsAttachmentWithAllRows()
    {
        await ResetDatabaseAsync();
        var references = await SeedDeviceReferenceDataAsync();
        await SeedAsync(
            entities:
            [
                CreateDevice(references, $"Csv-{Guid.NewGuid():N}"),
                CreateDevice(references, $"Csv-{Guid.NewGuid():N}"),
                CreateDevice(references, $"Csv-{Guid.NewGuid():N}")
            ]);
        using var client = CreateClient();

        var exportResponse = await client.GetAsync("/api/v1/exports/devices?format=csv", HttpCompletionOption.ResponseHeadersRead);
        var devicesResponse = await client.GetAsync("/api/v1/devices?page=1&pageSize=200");

        exportResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        exportResponse.Content.Headers.ContentType?.MediaType.Should().Be("text/csv");
        var contentDisposition = exportResponse.Headers.TryGetValues("Content-Disposition", out var responseHeaderValues)
            ? string.Join(';', responseHeaderValues)
            : exportResponse.Content.Headers.ContentDisposition?.ToString();
        contentDisposition.Should().NotBeNullOrWhiteSpace();
        contentDisposition.Should().Contain("attachment");
        contentDisposition.Should().Contain("devices-export-");

        var csvBody = await exportResponse.Content.ReadAsStringAsync();
        var csvRows = ReadCsvRows(csvBody);
        var pagedDevices = await ReadJsonAsync<PagedResponse<DeviceResponse>>(devicesResponse);
        csvRows.Should().HaveCount(pagedDevices.TotalCount);
    }

    [Fact]
    public async Task ExportDevicesAsJson_WhenRequested_ReturnsJsonArray()
    {
        await ResetDatabaseAsync();
        var references = await SeedDeviceReferenceDataAsync();
        await SeedAsync(entities: [CreateDevice(references, $"Json-{Guid.NewGuid():N}"), CreateDevice(references, $"Json-{Guid.NewGuid():N}")]);
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/exports/devices?format=json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        var payload = await ReadJsonAsync<DeviceExportRow[]>(response);
        payload.Should().HaveCount(2);
    }

    [Fact]
    public async Task ExportDevices_WhenFilteredByActiveStatus_ReturnsOnlyActiveRows()
    {
        await ResetDatabaseAsync();
        var references = await SeedDeviceReferenceDataAsync();
        await SeedAsync(
            entities:
            [
                CreateDevice(references, $"Active-{Guid.NewGuid():N}", DeviceStatus.Active),
                CreateDevice(references, $"Disposed-{Guid.NewGuid():N}", DeviceStatus.Disposed)
            ]);
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/exports/devices?format=json&status=Active");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadJsonAsync<DeviceExportRow[]>(response);
        payload.Should().NotBeEmpty();
        payload.Should().OnlyContain(row => string.Equals(row.Status, DeviceStatus.Active.ToString(), StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ExportDevices_WhenLargeDatasetRequested_ReturnsStreamedSuccessfulResponse()
    {
        await ResetDatabaseAsync();
        var references = await SeedDeviceReferenceDataAsync();
        var devices = Enumerable.Range(1, 125)
            .Select(index => CreateDevice(references, $"Stream-{index:D3}"))
            .Cast<object>()
            .ToArray();
        await SeedAsync(entities: devices);
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/exports/devices?format=csv", HttpCompletionOption.ResponseHeadersRead);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/csv");
        var csvBody = await response.Content.ReadAsStringAsync();
        ReadCsvRows(csvBody).Should().HaveCount(125);
    }

    [Fact]
    public async Task ExportDevicesAsCsv_WhenNameStartsWithFormulaTrigger_IsNeutralizedWithLeadingQuote()
    {
        await ResetDatabaseAsync();
        var references = await SeedDeviceReferenceDataAsync();
        await SeedAsync(entities: [CreateDevice(references, "=1+1")]);
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/exports/devices?format=csv", HttpCompletionOption.ResponseHeadersRead);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var csvRows = ReadCsvRows(await response.Content.ReadAsStringAsync());
        csvRows.Should().ContainSingle();
        csvRows[0]["Name"].Should().Be("'=1+1");
    }

    private static List<Dictionary<string, string>> ReadCsvRows(string csvBody)
    {
        using var stringReader = new StringReader(csvBody);
        using var csvReader = new CsvReader(stringReader, CultureInfo.InvariantCulture);

        csvReader.Read();
        csvReader.ReadHeader();
        var headerRecord = csvReader.HeaderRecord ?? [];
        var rows = new List<Dictionary<string, string>>();

        while (csvReader.Read())
        {
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in headerRecord)
            {
                row[header] = csvReader.GetField(header) ?? string.Empty;
            }

            rows.Add(row);
        }

        return rows;
    }

}
