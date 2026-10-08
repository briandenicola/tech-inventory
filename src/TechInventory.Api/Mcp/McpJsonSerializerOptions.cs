using System.Text.Json;
using System.Text.Json.Serialization;
using ModelContextProtocol;

namespace TechInventory.Api.Mcp;

public static class McpJsonSerializerOptions
{
    public static JsonSerializerOptions Create()
        => new(McpJsonUtilities.DefaultOptions)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        };
}
