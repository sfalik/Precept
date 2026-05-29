using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;

public sealed record ProofLedger(
    ImmutableArray<ProofObligation> Obligations,
    ImmutableArray<FaultSiteLink> FaultSiteLinks,
    ImmutableArray<ConstraintInfluenceEntry> ConstraintInfluence,
    ImmutableArray<InitialStateSatisfiabilityResult> InitialStateResults,
    ImmutableArray<Diagnostic> Diagnostics,
    /// <summary>
    /// Proof-engine-produced facts about reachability. Currently carries
    /// <see cref="UnreachableRowFact"/> entries for transition rows whose
    /// guards are unsatisfiable under field bounds. Future consumers (LS
    /// hover, MCP `precept_proofs`) can read these structured verdicts; the
    /// graph-level routing diagnostics are NOT re-emitted from these facts
    /// (the proof engine emits its own UnsatisfiableGuard diagnostics
    /// directly via the satisfiability scan).
    /// </summary>
    ImmutableArray<ProofForwardingFact> ProducedFacts
);

public sealed record ProofObligation(
    ProofRequirement Requirement,
    TypedExpression Site,
    ObligationContext Context,
    ProofDisposition Disposition,
    ProofStrategy? Strategy,
    DiagnosticCode? EmittedDiagnostic,
    NumericInterval? ComputedInterval = null
)
{
    private readonly ImmutableArray<string> _reassignedBefore = ImmutableArray<string>.Empty;
    private readonly ImmutableArray<string> _countEstablishedBefore = ImmutableArray<string>.Empty;
    private readonly ImmutableArray<string> _countInvalidatedBefore = ImmutableArray<string>.Empty;

    /// <summary>
    /// Fields fully replaced (written by a <c>set</c>/<c>clear</c> action — see
    /// <see cref="Precept.Language.ActionEffectClass.ReplacesValue"/> /
    /// <see cref="Precept.Language.ActionEffectClass.Empties"/>) earlier in the same action chain,
    /// before this obligation's site. Per <c>precept-language-spec.md § 0.6</c> item 7 (Sequential
    /// proof flow) — "When a field is reassigned, prior proof facts about that field are invalidated
    /// before the new assignment's facts are stored" — a guard fact about such a field must NOT
    /// discharge this obligation. In-place collection grow/shrink is tracked separately
    /// (<see cref="CountEstablishedBefore"/> / <see cref="CountInvalidatedBefore"/>), since those
    /// invalidate or establish only the non-empty (<c>count &gt; 0</c>) fact, not every fact about
    /// the field. Empty for obligations outside an action chain (rules, ensures, field/arg
    /// defaults). IsDefault-safe (reads coalesce to Empty).
    /// </summary>
    public ImmutableArray<string> ReassignedBefore
    {
        get => _reassignedBefore.IsDefault ? ImmutableArray<string>.Empty : _reassignedBefore;
        init => _reassignedBefore = value;
    }

    /// <summary>
    /// Collections proven non-empty by a grow action (<see cref="Precept.Language.ActionEffectClass.Grows"/>:
    /// <c>add</c>/<c>append</c>/<c>insert</c>/<c>enqueue</c>/<c>push</c>/<c>put</c> and <c>*-by</c>
    /// variants) earlier in the same chain, before this obligation's site. A grow adds at least one
    /// element, so <c>count &gt; 0</c> holds afterward regardless of prior contents — a <c>count &gt; 0</c>
    /// obligation about such a field discharges directly (forward-propagation, spec § 0.6 item 7).
    /// IsDefault-safe.
    /// </summary>
    public ImmutableArray<string> CountEstablishedBefore
    {
        get => _countEstablishedBefore.IsDefault ? ImmutableArray<string>.Empty : _countEstablishedBefore;
        init => _countEstablishedBefore = value;
    }

    /// <summary>
    /// Collections whose non-empty fact is stale because a shrink action
    /// (<see cref="Precept.Language.ActionEffectClass.Shrinks"/>: <c>remove</c>/<c>removeAt</c>/<c>pop</c>/<c>dequeue</c>
    /// and <c>*-by</c> variants) earlier in the same chain may have emptied them. A pre-shrink
    /// <c>count &gt; 0</c> guard must NOT discharge a <c>count &gt; 0</c> obligation after the shrink
    /// (the shrink could have reduced count to 0). IsDefault-safe.
    /// </summary>
    public ImmutableArray<string> CountInvalidatedBefore
    {
        get => _countInvalidatedBefore.IsDefault ? ImmutableArray<string>.Empty : _countInvalidatedBefore;
        init => _countInvalidatedBefore = value;
    }
};

public abstract record ObligationContext;
public sealed record TransitionRowContext(TypedTransitionRow Row) : ObligationContext;
public sealed record ConstraintContext(ConstraintIdentity Constraint) : ObligationContext;
public sealed record StateHookContext(TypedStateHook Hook) : ObligationContext;
public sealed record EventHandlerContext(TypedEventRow Handler) : ObligationContext;
public sealed record FieldExpressionContext(TypedField Field) : ObligationContext;
public sealed record FieldDefaultContext(TypedField Field) : ObligationContext;
public sealed record ArgDefaultContext(TypedArg Arg) : ObligationContext;

public enum ProofDisposition
{
    Proved = 1,
    Unresolved = 2
}

public enum ProofStrategy
{
    Literal = 1,
    DeclarationAttribute = 2,
    GuardInPath = 3,
    FlowNarrowing = 4,
    QualifierCompatibility = 5,
    CompositionalConstraint = 6,
    IntervalContainment = 7,
    LengthContainment   = 8,
    CountContainment    = 9,
    /// <summary>Dimensional product of two quantity operands lands in the curated business-domain dimension set.</summary>
    DimensionalProduct  = 10,
    /// <summary>A <c>count &gt; 0</c> obligation discharged by a prior grow action in the same chain (forward-propagation; spec § 0.6 item 7).</summary>
    CollectionGrowth    = 11,
}

public sealed record FaultSiteLink(
    ProofObligation Obligation,
    FaultCode FaultCode,
    DiagnosticCode DiagnosticCode,
    SourceSpan Site
);

public sealed record FaultSiteAnnotation(
    FaultCode Code,
    DiagnosticCode PreventedBy,
    SourceSpan Site
);

public sealed record ConstraintInfluenceEntry(
    ConstraintIdentity Constraint,
    ImmutableArray<string> ReferencedFields,
    ImmutableArray<EventArgReference> ReferencedArgs
);

public sealed record EventArgReference(string EventName, string ArgName);

public sealed record InitialStateSatisfiabilityResult(
    string StateName,
    bool IsSatisfiable,
    ImmutableArray<UnsatisfiedConstraint> Violations
);

public sealed record UnsatisfiedConstraint(
    ConstraintIdentity Constraint,
    string Reason
);
