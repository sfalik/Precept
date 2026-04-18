namespace Precept;

/// <summary>
/// Renders proof-backed diagnostic messages from <see cref="ProofAssessment"/> records.
/// Shared by C76, C92, C93 to eliminate ad hoc message branching.
/// </summary>
internal static class ProofDiagnosticRenderer
{
    /// <summary>
    /// Renders a human-readable diagnostic message for the given assessment.
    /// </summary>
    public static string Render(ProofAssessment assessment) => assessment switch
    {
        { Requirement: ProofRequirement.NonzeroDivisor, Outcome: ProofOutcome.Contradiction } a =>
            $"Division by zero: divisor {a.SubjectDescription} is provably zero.",

        { Requirement: ProofRequirement.NonzeroDivisor, Outcome: ProofOutcome.Obligation } a when a.StrongestFact.IsNonnegative =>
            $"Divisor {a.SubjectDescription} is nonnegative but not nonzero — 'nonnegative' allows zero. Consider 'positive' instead.",

        { Requirement: ProofRequirement.NonzeroDivisor, Outcome: ProofOutcome.Obligation } a when !a.StrongestFact.IsUnknown =>
            $"Divisor {a.SubjectDescription} interval [{a.StrongestFact.Lower}, {a.StrongestFact.Upper}] may include zero. " +
            "Consider restructuring with a 'positive' constraint or 'rule != 0'.",

        { Requirement: ProofRequirement.NonzeroDivisor, Outcome: ProofOutcome.Obligation } a =>
            $"Divisor {a.SubjectDescription} has no compile-time nonzero proof. " +
            $"Consider adding a 'positive' constraint, 'rule {a.SubjectDescription} != 0', or 'when {a.SubjectDescription} != 0' guard.",

        { Requirement: ProofRequirement.NonnegativeArgument, Outcome: ProofOutcome.Obligation } a =>
            $"sqrt() requires a non-negative argument. '{a.SubjectDescription}' may be negative. " +
            $"Add a 'nonnegative' constraint, 'rule {a.SubjectDescription} >= 0', state/event 'ensure', or guard with '{a.SubjectDescription} >= 0'.",

        { Requirement: ProofRequirement.NonnegativeArgument, Outcome: ProofOutcome.Contradiction } a =>
            $"sqrt() argument '{a.SubjectDescription}' is provably negative.",

        _ => $"Proof diagnostic: {assessment.Requirement}/{assessment.Outcome} for {assessment.SubjectDescription}",
    };
}
