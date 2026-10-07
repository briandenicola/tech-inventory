using System.ComponentModel.DataAnnotations;

namespace TechInventory.Api.Mcp;

public sealed class McpOptions
{
    public const string SectionName = "Mcp";

    public bool Enabled { get; init; }

    [Range(1024, 1048576)]
    public long MaxRequestBodyBytes { get; init; } = 131072;
}
