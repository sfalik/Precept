using Precept.Pipeline;
using Precept.Runtime;

namespace Precept.Language;

/// <summary>
/// The severity level of a runtime fault. Currently all faults are fatal
/// (the transition is aborted and no state changes are applied).
/// </summary>
public enum FaultSeverity
{
    /// <summary>The transition fails immediately; no state changes are committed.</summary>
    Fatal = 1,
}

// ExpressionContext and InputValues are optional — attach via `with` expressions at call sites
// that have the relevant context. The constructor defaults keep all existing Faults.Create()
// call sites unchanged. See D8/R4 revisit note in Evaluator.cs for the planned evaluator-side usage.
public readonly record struct Fault(
    FaultCode                                    Code,
    string                                       CodeName,      // nameof-derived via Faults.Create() — stable identity for logging / MCP
    string                                       Message,       // pre-formatted, final English string
    SourceSpan?                                  ExpressionContext = null,  // structured source location of the failing expression
    IReadOnlyDictionary<string, PreceptValue>?   InputValues      = null   // field/arg values at fault time (API boundary type)
);
