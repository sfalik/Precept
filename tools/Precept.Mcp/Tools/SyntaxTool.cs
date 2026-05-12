using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Precept.Mcp.Tools;

[McpServerToolType]
public static class SyntaxTool
{
    [McpServerTool(Name = "precept_syntax")]
    [Description("Return the Precept syntax reference as markdown: grammar rules, precedence, conventional order, constructs, actions, outcomes, and operators.")]
    public static string Syntax()
        => CatalogFormatters.FormatSyntax();
}
