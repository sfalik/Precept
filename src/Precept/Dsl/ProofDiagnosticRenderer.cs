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
            $"Division by zero: divisor '{a.SubjectDescription}' is provably zero.",

        { Requirement: ProofRequirement.NonzeroDivisor, Outcome: ProofOutcome.Obligation } a when a.StrongestFact.IsNonnegative =>
            $"Divisor '{a.SubjectDescription}' is nonnegative but not nonzero — 'nonnegative' allows zero. Consider 'positive' instead.",

        { Requirement: ProofRequirement.NonzeroDivisor, Outcome: ProofOutcome.Obligation } a when !a.StrongestFact.IsUnknown =>
            $"Divisor '{a.SubjectDescription}' interval [{a.StrongestFact.Lower}, {a.StrongestFact.Upper}] may include zero. " +
            "Consider restructuring with a 'positive' constraint or 'rule != 0'.",

        { Requirement: ProofRequirement.NonzeroDivisor, Outcome: ProofOutcome.Obligation } a =>
            $"Divisor '{a.SubjectDescription}' has no compile-time nonzero proof. " +
            $"Consider adding a 'positive' constraint, 'rule {a.SubjectDescription} != 0', or 'when {a.SubjectDescription} != 0' guard.",

        { Requirement: ProofRequirement.NonnegativeArgument, Outcome: ProofOutcome.Obligation } a =>
            $"sqrt() requires a non-negative argument. '{a.SubjectDescription}' may be negative. " +
            $"Add a 'nonnegative' constraint, 'rule {a.SubjectDescription} >= 0', state/event 'ensure', or guard with '{a.SubjectDescription} >= 0'.",

        { Requirement: ProofRequirement.NonnegativeArgument, Outcome: ProofOutcome.Contradiction } a =>
            $"sqrt() argument '{a.SubjectDescription}' is provably negative.",

        { Requirement: ProofRequirement.AssignmentConstraint, Outcome: ProofOutcome.Contradiction } a
            when a.ConstraintInterval is not null =>
            $"Assignment to '{a.SubjectDescription}' is provably outside the field's constraint range. " +
            $"Expression produces {FormatInterval(a.StrongestFact)}, but field requires {FormatInterval(a.ConstraintInterval.Value)}.",

        { Requirement: ProofRequirement.RuleSatisfiability, Outcome: ProofOutcome.Contradiction } a
            when a.ConstraintInterval is not null && a.ConstraintDescription is not null =>
            $"Rule '{a.ConstraintDescription}' contradicts the constraints on '{a.SubjectDescription}'. " +
            $"Rule requires {FormatInterval(a.StrongestFact)}, but field is constrained to {FormatInterval(a.ConstraintInterval.Value)}.",

        { Requirement: ProofRequirement.RuleSatisfiability, Outcome: ProofOutcome.Contradiction } a =>
            $"Rules for '{a.SubjectDescription}' are contradictory — no value can satisfy all simultaneously.",

        _ => $"Proof diagnostic: {assessment.Requirement}/{assessment.Outcome} for {assessment.SubjectDescription}",
    };

    /// <summary>
    /// Formats a <see cref="NumericInterval"/> as standard mathematical notation.
    /// </summary>
    internal static string FormatInterval(NumericInterval ival)
    {
        var left = ival.LowerInclusive ? "[" : "(";
        var right = ival.UpperInclusive ? "]" : ")";
        var lo = double.IsNegativeInfinity(ival.Lower) ? "-∞" : ival.Lower.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var hi = double.IsPositiveInfinity(ival.Upper) ? "+∞" : ival.Upper.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return $"{left}{lo}, {hi}{right}";
    }
}
