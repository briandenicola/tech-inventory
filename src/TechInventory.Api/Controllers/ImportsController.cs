using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechInventory.Api.Common;
using TechInventory.Api.ExceptionHandling;
using TechInventory.Application.Common.Paging;
using TechInventory.Application.Common.Results;
using TechInventory.Application.Imports;
using TechInventory.Domain.Enums;

namespace TechInventory.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/imports")]
public sealed class ImportsController(ISender sender, IConfiguration configuration) : ControllerBase
{
    private const long DefaultMaxFileSizeBytes = 10_485_760;

    [HttpPost("preview")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(DefaultMaxFileSizeBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = DefaultMaxFileSizeBytes)]
    [ProducesResponseType(typeof(PreviewImportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status413PayloadTooLarge)]
    public async Task<ActionResult<PreviewImportResult>> PreviewImport([FromForm] ImportUploadRequest request, CancellationToken cancellationToken)
    {
        var fileBytes = await ReadFileBytesAsync(request, cancellationToken).ConfigureAwait(false);
        var mapping = ParseMapping(request.Mapping);
        return this.OkResult(await sender.Send(new PreviewImportCommand(fileBytes, request.File.FileName, mapping), cancellationToken));
    }

    [HttpPost("commit")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(DefaultMaxFileSizeBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = DefaultMaxFileSizeBytes)]
    [ProducesResponseType(typeof(CommitImportResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CommitImportResult>> CommitImport([FromForm] ImportUploadRequest request, CancellationToken cancellationToken)
    {
        var fileBytes = await ReadFileBytesAsync(request, cancellationToken).ConfigureAwait(false);
        var mapping = ParseMapping(request.Mapping);
        var result = await sender.Send(new CommitImportCommand(fileBytes, request.File.FileName, mapping), cancellationToken).ConfigureAwait(false);
        var value = result.GetValueOrThrow();
        return CreatedAtAction(nameof(GetImportBatchById), new { id = value.BatchId }, value);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ImportBatchSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<ImportBatchSummaryResponse>>> GetImportBatches(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] ImportStatus? status = null,
        [FromQuery] DateTimeOffset? createdAfter = null,
        [FromQuery] DateTimeOffset? createdBefore = null,
        CancellationToken cancellationToken = default)
        => this.OkResult(await sender.Send(new ListImportBatchesQuery(page, pageSize, status, createdAfter, createdBefore), cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ImportBatchDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ImportBatchDetailResponse>> GetImportBatchById(Guid id, CancellationToken cancellationToken)
        => this.OkResult(await sender.Send(new GetImportBatchByIdQuery(id), cancellationToken));

    private async Task<byte[]> ReadFileBytesAsync(ImportUploadRequest request, CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length == 0)
        {
            throw new ResultFailureException(Error.Validation(new Dictionary<string, string[]> { ["file"] = ["A CSV file is required."] }));
        }

        var maxFileSizeBytes = configuration.GetValue<long?>("Imports:MaxFileSizeBytes") ?? DefaultMaxFileSizeBytes;
        if (request.File.Length > maxFileSizeBytes)
        {
            throw new ResultFailureException(new Error("PayloadTooLarge", $"Import files may not exceed {maxFileSizeBytes} bytes."));
        }

        await using var memoryStream = new MemoryStream();
        await request.File.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
        return memoryStream.ToArray();
    }

    private IReadOnlyDictionary<string, string>? ParseMapping(string? mapping)
    {
        if (string.IsNullOrWhiteSpace(mapping))
        {
            return null;
        }

        try
        {
            var parsedMapping = JsonSerializer.Deserialize<Dictionary<string, string>>(mapping, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return parsedMapping;
        }
        catch (JsonException exception)
        {
            throw new ResultFailureException(Error.Validation(new Dictionary<string, string[]> { ["mapping"] = [$"Mapping must be valid JSON: {exception.Message}"] }));
        }
    }

    public sealed record ImportUploadRequest(IFormFile File, string? Mapping = null);
}
