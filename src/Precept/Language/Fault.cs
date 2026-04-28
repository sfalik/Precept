namespace Precept.Language;

/// <summary>
/// The severity level of a runtime fault. Currently all faults are fatal
/// (the transition is aborted and no state changes are applied).
/// </summary>
public enum FaultSeverity
{
    /// <summary>The transition fails immediately; no state changes are committed.</summary>
    Fatal,
}

// TODO: Stub — shape will be finalized when the evaluator result type is designed.
// Expected additions: expression context (what was being evaluated), input values that
// triggered the fault, and linkage back to the DiagnosticCode that should have prevented it.
public readonly record struct Fault(
    FaultCode Code,
    string    CodeName,  // nameof-derived via Faults.Create() — stable identity for logging / MCP
    string    Message    // pre-formatted, final English string
);
