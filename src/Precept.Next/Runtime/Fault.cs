namespace Precept.Runtime;

// TODO: Stub — shape will be finalized when the evaluator result type is designed.
// Expected additions: expression context (what was being evaluated), input values that
// triggered the fault, and linkage back to the DiagnosticCode that should have prevented it.
public readonly record struct Fault(
    FaultCode Code,
    string    CodeName,  // code.ToString() — stable identity for logging / MCP
    string    Message    // pre-formatted, final English string
);
