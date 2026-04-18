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
    /// <summary>Assignment must fall within the target field's constraint interval.</summary>
    AssignmentConstraint,
    /// <summary>Rule must be satisfiable given field constraints and other rules.</summary>
    RuleSatisfiability,
    /// <summary>Rule is vacuous — already guaranteed by field constraints.</summary>
    RuleVacuity,
    /// <summary>Guard is provably always false (dead code).</summary>
    GuardSatisfiability,
    /// <summary>Guard is provably always true (tautological — no effect).</summary>
    GuardTautology,
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
        (ProofRequirement.AssignmentConstraint, ProofOutcome.Contradiction) => DiagnosticCatalog.C94,
        (ProofRequirement.RuleSatisfiability, ProofOutcome.Contradiction) => DiagnosticCatalog.C95,
        (ProofRequirement.RuleVacuity, ProofOutcome.Satisfied) => DiagnosticCatalog.C96,
        (ProofRequirement.GuardSatisfiability, ProofOutcome.Contradiction) => DiagnosticCatalog.C97,
        (ProofRequirement.GuardTautology, ProofOutcome.Satisfied) => DiagnosticCatalog.C98,
        _ => throw new InvalidOperationException($"Unexpected assessment: {Requirement}/{Outcome}"),
    };
}
