using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Precept.Mcp.Tools;

[McpServerToolType]
public static class DomainsTool
{
    [McpServerTool(Name = "precept_domains")]
    [Description("Return the Precept domain catalog as markdown. Use `scope` to limit size: `currencies`, `units`, `prefixes`, `dimensions`, or `temporal`. Omit `scope` only when you need the full domain bundle.")]
    public static string Domains(string? scope = null)
        => CatalogFormatters.FormatDomains(scope);
}
