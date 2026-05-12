using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Precept.Mcp.Tools;

[McpServerToolType]
public static class OperationsTool
{
    [McpServerTool(Name = "precept_operations")]
    [Description("Return the Precept operations catalog as markdown. Use the `category` filter (for example `Money`, `Integer`, or `Quantity`) as the normal path; omitting it returns the full catalog plus the available categories.")]
    public static string Operations(string? category = null)
        => CatalogFormatters.FormatOperations(category);
}
