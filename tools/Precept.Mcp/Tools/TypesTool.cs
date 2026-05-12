using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Precept.Mcp.Tools;

[McpServerToolType]
public static class TypesTool
{
    [McpServerTool(Name = "precept_types")]
    [Description("Return the Precept type-system reference as markdown. Use `scope` to keep the payload small: `types`, `modifiers`, `modifiers:value`, `modifiers:state`, `modifiers:event`, `modifiers:access`, `modifiers:anchor`, or `functions`. Omit `scope` only when you need the full catalog.")]
    public static string Types(string? scope = null)
        => CatalogFormatters.FormatTypes(scope);
}
