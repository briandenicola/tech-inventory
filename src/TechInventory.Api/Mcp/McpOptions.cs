using System.ComponentModel.DataAnnotations;

namespace TechInventory.Api.Mcp;

public sealed class McpOptions
{
    public const string SectionName = "Mcp";

    [Range(1024, 1048576)]
    public long MaxRequestBodyBytes { get; init; } = 131072;
}
