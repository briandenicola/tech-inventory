using Microsoft.OpenApi.Writers;
using Swashbuckle.AspNetCore.Swagger;

namespace TechInventory.Api.OpenApi;

public static class OpenApiDocumentExporter
{
    public static async Task ExportAsync(IServiceProvider serviceProvider, string outputPath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        var swaggerProvider = serviceProvider.GetRequiredService<ISwaggerProvider>();
        var document = swaggerProvider.GetSwagger("v1");
        var fullOutputPath = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath)!);

        await using var fileStream = File.Create(fullOutputPath);
        await using var streamWriter = new StreamWriter(fileStream);
        var yamlWriter = new OpenApiYamlWriter(streamWriter);
        document.SerializeAsV3(yamlWriter);
        await streamWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
