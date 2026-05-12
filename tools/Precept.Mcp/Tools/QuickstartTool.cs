using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Precept.Mcp.Tools;

[McpServerToolType]
public static class QuickstartTool
{
    [McpServerTool(Name = "precept_quickstart")]
    [Description("Return a compact markdown quickstart for Precept authoring: what Precept is, the core guarantee, core concepts, tool guidance, and minimal verified examples.")]
    public static string Quickstart()
        => CatalogFormatters.FormatQuickstart();
}
