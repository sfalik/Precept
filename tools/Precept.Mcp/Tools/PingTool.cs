using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Precept.Mcp.Tools;

[McpServerToolType]
public static class PingTool
{
    [McpServerTool(Name = "precept_ping")]
    [Description("Connectivity check — returns ok.")]
    public static string Ping() => "ok";
}
