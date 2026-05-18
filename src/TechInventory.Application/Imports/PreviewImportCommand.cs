using FluentValidation;
using MediatR;
using TechInventory.Application.Common.Results;

namespace TechInventory.Application.Imports;

public sealed record PreviewImportCommand(
    byte[] FileContents,
    string? FileName = null,
    IReadOnlyDictionary<string, string>? ColumnMapping = null) : IRequest<Result<PreviewImportResult>>;

public sealed class PreviewImportCommandHandler(IDeviceImportProcessingService importProcessingService)
    : IRequestHandler<PreviewImportCommand, Result<PreviewImportResult>>
{
    public async Task<Result<PreviewImportResult>> Handle(PreviewImportCommand request, CancellationToken cancellationToken)
    {
        var preview = await importProcessingService.ProcessAsync(request.FileContents, request.ColumnMapping, cancellationToken).ConfigureAwait(false);
        return Result<PreviewImportResult>.Success(
            new PreviewImportResult(
                preview.TotalRows,
                preview.ValidRows
                    .Select(row => new ImportRowPreview(
                        row.RowNumber,
                        row.Device,
                        row.BrandId,
                        row.CategoryId,
                        row.OwnerId,
                        row.LocationId,
                        row.NetworkId))
                    .ToArray(),
                preview.InvalidRows,
                preview.LookupsToCreate));
    }
}

public sealed class PreviewImportCommandValidator : AbstractValidator<PreviewImportCommand>
{
    public PreviewImportCommandValidator()
    {
        RuleFor(command => command.FileContents)
            .Must(fileContents => fileContents is { Length: > 0 })
            .WithMessage("FileContents must not be empty.");

        RuleFor(command => command.FileName)
            .MaximumLength(512);

        When(command => command.ColumnMapping is not null, () =>
        {
            RuleForEach(command => command.ColumnMapping!)
                .Must(mapping => !string.IsNullOrWhiteSpace(mapping.Key) && !string.IsNullOrWhiteSpace(mapping.Value))
                .WithMessage("Column mappings must include both a CSV column name and a target field.");

            RuleFor(command => command.ColumnMapping!)
                .Must(AllMappingsAreSupported)
                .WithMessage($"Column mappings must target supported fields: {string.Join(", ", ImportFieldNames.SupportedFields)}.");
        });
    }

    private static bool AllMappingsAreSupported(IReadOnlyDictionary<string, string> mapping)
        => mapping.Values.All(value => ImportFieldNames.TryNormalize(value, out _));
}
