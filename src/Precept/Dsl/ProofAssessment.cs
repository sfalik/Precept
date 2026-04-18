using System;

namespace Precept;

/// <summary>The outcome kind of a proof-backed diagnostic assessment.</summary>
internal enum ProofOutcome
{
    /// <summary>The safety requirement is provably satisfied — no diagnostic.</summary>
    Satisfied,
    /// <summary>The safety requirement is provably violated — contradiction (e.g., literal zero divisor).</summary>
    Contradiction,
    /// <summary>The safety requirement cannot be proven — unresolved obligation.</summary>
    Obligation,
}

/// <summary>What kind of safety requirement is being assessed.</summary>
internal enum ProofRequirement
{
    /// <summary>Divisor must be nonzero.</summary>
    NonzeroDivisor,
    /// <summary>sqrt() argument must be non-negative.</summary>
    NonnegativeArgument,
    // Planned: AssignmentConstraint (C94), RuleSatisfiability (C95),
    // RuleVacuity (C96), GuardSatisfiability (C97), GuardTautology (C98)
}

/// <summary>
/// A structured proof-backed diagnostic assessment. Captures the safety requirement,
/// the proof outcome, the subject expression, and the strongest known fact.
/// </summary>
internal sealed record ProofAssessment(
    ProofRequirement Requirement,
    ProofOutcome Outcome,
    string SubjectDescription,
    NumericInterval StrongestFact,
    ProofAttribution Attribution,
    NumericInterval? ConstraintInterval = null,
    string? ConstraintDescription = null)
{
    /// <summary>Maps to the appropriate diagnostic code.</summary>
    public LanguageConstraint DiagnosticCode => (Requirement, Outcome) switch
    {
        (ProofRequirement.NonzeroDivisor, ProofOutcome.Contradiction) => DiagnosticCatalog.C92,
        (ProofRequirement.NonzeroDivisor, ProofOutcome.Obligation) => DiagnosticCatalog.C93,
        (ProofRequirement.NonnegativeArgument, ProofOutcome.Contradiction) => DiagnosticCatalog.C76,
        (ProofRequirement.NonnegativeArgument, ProofOutcome.Obligation) => DiagnosticCatalog.C76,
        _ => throw new InvalidOperationException($"Unexpected assessment: {Requirement}/{Outcome}"),
    };
}
