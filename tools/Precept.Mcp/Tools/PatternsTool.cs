using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Precept.Mcp.Tools;

[McpServerToolType]
public static class PatternsTool
{
    [McpServerTool(Name = "precept_patterns")]
    [Description("Return compile-verified common patterns and anti-patterns as markdown, including corrected alternatives and ready-to-read Precept snippets.")]
    public static string Patterns()
        => CatalogFormatters.FormatPatterns();
}
