using System.Globalization;
using System.Text.Json;
using CsvHelper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechInventory.Api.Common;
using TechInventory.Application.Exports;
using TechInventory.Domain.Enums;

namespace TechInventory.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/exports")]
public sealed class ExportsController(ISender sender, ILogger<ExportsController> logger) : ControllerBase
{
    [HttpGet("devices")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportDevices([FromQuery] ExportDevicesRequest request, CancellationToken cancellationToken)
    {
        var exportRows = (await sender.Send(request.ToQuery(), cancellationToken).ConfigureAwait(false)).GetValueOrThrow();
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture);
        var extension = request.Format == ExportFormat.Json ? "json" : "csv";
        Response.Headers["Content-Disposition"] = $"attachment; filename=\"devices-export-{timestamp}.{extension}\"";

        if (request.Format == ExportFormat.Json)
        {
            Response.ContentType = "application/json";
            var rowCount = await WriteJsonAsync(exportRows, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Exported {RowCount} devices as {Format}.", rowCount, request.Format);
            return new EmptyResult();
        }

        Response.ContentType = "text/csv";
        var csvCount = await WriteCsvAsync(exportRows, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Exported {RowCount} devices as {Format}.", csvCount, request.Format);
        return new EmptyResult();
    }

    private async Task<int> WriteCsvAsync(IAsyncEnumerable<DeviceExportRow> exportRows, CancellationToken cancellationToken)
    {
        await WriteCsvChunkAsync(csvWriter =>
        {
            csvWriter.WriteHeader<DeviceExportRow>();
            csvWriter.NextRecord();
        }, cancellationToken).ConfigureAwait(false);

        var rowCount = 0;
        await foreach (var row in exportRows.WithCancellation(cancellationToken))
        {
            await WriteCsvChunkAsync(csvWriter =>
            {
                csvWriter.WriteRecord(row);
                csvWriter.NextRecord();
            }, cancellationToken).ConfigureAwait(false);
            rowCount++;
        }

        return rowCount;
    }

    private async Task WriteCsvChunkAsync(Action<CsvWriter> writeChunk, CancellationToken cancellationToken)
    {
        using var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
        using var csvWriter = new CsvWriter(stringWriter, CultureInfo.InvariantCulture);
        writeChunk(csvWriter);
        await Response.WriteAsync(stringWriter.ToString(), cancellationToken).ConfigureAwait(false);
    }

    private async Task<int> WriteJsonAsync(IAsyncEnumerable<DeviceExportRow> exportRows, CancellationToken cancellationToken)
    {
        var rowCount = 0;
        using var jsonWriter = new Utf8JsonWriter(Response.BodyWriter);
        jsonWriter.WriteStartArray();

        await foreach (var row in exportRows.WithCancellation(cancellationToken))
        {
            JsonSerializer.Serialize(jsonWriter, row, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            await jsonWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
            rowCount++;
        }

        jsonWriter.WriteEndArray();
        await jsonWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
        return rowCount;
    }

    public sealed record ExportDevicesRequest
    {
        public ExportFormat Format { get; init; } = ExportFormat.Csv;

        public string? Search { get; init; }

        public Guid? BrandId { get; init; }

        public Guid? CategoryId { get; init; }

        public Guid? OwnerId { get; init; }

        public Guid? LocationId { get; init; }

        public Guid? NetworkId { get; init; }

        public DeviceStatus? Status { get; init; }

        public bool IncludeAllStatuses { get; init; }

        public string? Tags { get; init; }

        public int? PurchaseYearFrom { get; init; }

        public int? PurchaseYearTo { get; init; }

        public string? SortBy { get; init; }

        public bool SortDescending { get; init; }

        public ExportDevicesQuery ToQuery()
            => new(
                Format,
                Search,
                BrandId,
                CategoryId,
                OwnerId,
                LocationId,
                NetworkId,
                Status,
                IncludeAllStatuses,
                ParseTags(Tags),
                PurchaseYearFrom,
                PurchaseYearTo,
                SortBy,
                SortDescending);

        private static IReadOnlyCollection<Guid>? ParseTags(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(tagId => Guid.TryParse(tagId, out var parsedTagId) ? parsedTagId : Guid.Empty)
                .ToArray();
        }
    }
}
