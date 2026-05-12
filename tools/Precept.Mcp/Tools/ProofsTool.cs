using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Precept.Mcp.Tools;

[McpServerToolType]
public static class ProofsTool
{
    [McpServerTool(Name = "precept_proofs")]
    [Description("Return the proof-requirement and runtime-fault catalogs as markdown. Call when guards, ensures, or runtime safety behavior are unclear.")]
    public static string Proofs()
        => CatalogFormatters.FormatProofs();
}
