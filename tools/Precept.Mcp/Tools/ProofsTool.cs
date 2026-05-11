using System.ComponentModel;
using ModelContextProtocol.Server;
using Precept.Language;
using Precept.Mcp.Dtos;

namespace Precept.Mcp.Tools;

[McpServerToolType]
public static class ProofsTool
{
    [McpServerTool(Name = "precept_proofs")]
    [Description("Return the proof obligation catalog and runtime fault catalog. Call when writing guards (when clauses) or ensure constraints to understand what the proof engine verifies and what runtime faults can occur.")]
    public static ProofsDto Proofs()
        => new(
            ProofRequirements.All.Select(MapProofRequirementMeta).ToArray(),
            Faults.All.Select(MapFaultMeta).ToArray()
        );

    private static ProofRequirementMetaDto MapProofRequirementMeta(ProofRequirementMeta meta)
        => new(
            meta.Kind.ToString(),
            meta.Description,
            meta is ProofRequirementMeta.QualifierCompatibility
                or ProofRequirementMeta.QualifierChain
        );

    private static FaultMetaDto MapFaultMeta(FaultMeta fault)
        => new(
            fault.Code,
            fault.MessageTemplate,
            fault.Severity.ToString(),
            fault.RecoveryHint
        );
}
