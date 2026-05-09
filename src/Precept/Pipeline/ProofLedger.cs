using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;

public sealed record ProofLedger(
    ImmutableArray<ProofObligation> Obligations,
    ImmutableArray<FaultSiteLink> FaultSiteLinks,
    ImmutableArray<ConstraintInfluenceEntry> ConstraintInfluence,
    ImmutableArray<InitialStateSatisfiabilityResult> InitialStateResults,
    ImmutableArray<Diagnostic> Diagnostics
);

public sealed record ProofObligation(
    ProofRequirement Requirement,
    TypedExpression Site,
    ObligationContext Context,
    ProofDisposition Disposition,
    ProofStrategy? Strategy,
    DiagnosticCode? EmittedDiagnostic
);

public abstract record ObligationContext;
public sealed record TransitionRowContext(TypedTransitionRow Row) : ObligationContext;
public sealed record ConstraintContext(ConstraintIdentity Constraint) : ObligationContext;
public sealed record StateHookContext(TypedStateHook Hook) : ObligationContext;
public sealed record EventHandlerContext(TypedEventHandler Handler) : ObligationContext;
public sealed record FieldExpressionContext(TypedField Field) : ObligationContext;

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
    QualifierCompatibility = 5
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
