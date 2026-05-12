using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Precept.Mcp.Tools;

[McpServerToolType]
public static class DiagnosticTool
{
    [McpServerTool(Name = "precept_diagnostic")]
    [Description("Look up a Precept diagnostic by code name (for example `UndeclaredField`) or PRE number (`PRE0017`) and return a markdown explanation with trigger, recovery steps, fix hint, related codes, and examples.")]
    public static string Diagnostic(string code)
        => CatalogFormatters.FormatDiagnostic(code);
}
