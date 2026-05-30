using System.Linq;
using FluentAssertions;
using Precept;
using Precept.Language;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// D9 open-field qualifier narrowing on the <c>period</c> <c>.basis</c> axis (S3).
/// The D14 assignment constraint generalizes to composite bases: assigning a value to a
/// <c>period in '…'</c> field is accepted iff the value's non-zero component set is a
/// <b>subset</b> of the field's declared basis (`business-domain-types.md` § D14 composite
/// extension). This slice (a) makes that assignment-subset obligation real for periods
/// (today the TemporalUnit axis is unhandled on the assignment-qualifier path), and (b) lets a
/// guard <c>when X.basis == 'hours'</c> narrow an OPEN period so the obligation discharges —
/// soundly: D14 subset (not equality), canonicalized RHS (so `'minutes + hours'` matches
/// canonical `'hours + minutes'`), all-branches (no OR-collapse), negation narrows nothing,
/// reassignment-aware.
///
/// Asserts only on the assignment-qualifier diagnostics (proof-stage
/// <see cref="DiagnosticCode.UnprovedAssignmentQualifierCompatibility"/> for the open-source
/// case; <see cref="DiagnosticCode.QualifierMismatch"/> for a declared mismatch), so the
/// unrelated required-field-init diagnostics in these minimal probes don't interfere. Compiles
/// directly against freshly-built core (never the MCP wrapper) so results can't be stale.
/// </summary>
public class PeriodBasisNarrowingTests
{
    private static bool HasBasisAssignmentError(string source) =>
        Compiler.Compile(source).Diagnostics.Any(d =>
            d.Code == nameof(DiagnosticCode.UnprovedAssignmentQualifierCompatibility)
         || d.Code == nameof(DiagnosticCode.QualifierMismatch));

    // ── Acceptance: guard narrows an open period; subset discharges ────────────

    [Fact]
    public void OpenPeriod_GuardedToSubsetBasis_Discharges()
    {
        HasBasisAssignmentError("""
            precept P
            field Composite as period in 'hours + minutes'
            field X as period
            state S initial
            event E
            from S on E when X.basis == 'hours'
                -> set Composite = X
                -> no transition
            """).Should().BeFalse("{hours} ⊆ {hours, minutes} — the guard-narrowed basis is a subset of the declared basis");
    }

    [Fact]
    public void OpenPeriod_GuardedToExactBasis_Discharges()
    {
        HasBasisAssignmentError("""
            precept P
            field Composite as period in 'hours + minutes'
            field X as period
            state S initial
            event E
            from S on E when X.basis == 'hours + minutes'
                -> set Composite = X
                -> no transition
            """).Should().BeFalse("{hours, minutes} ⊆ {hours, minutes} — equal sets satisfy the subset relation");
    }

    [Fact]
    public void OpenPeriod_GuardedToNonCanonicalBasis_Discharges()
    {
        HasBasisAssignmentError("""
            precept P
            field Composite as period in 'hours + minutes'
            field X as period
            state S initial
            event E
            from S on E when X.basis == 'minutes + hours'
                -> set Composite = X
                -> no transition
            """).Should().BeFalse("the guard RHS canonicalizes to {hours, minutes}; component-set comparison is order-independent");
    }

    // ── Soundness: superset / disjoint must NOT discharge ──────────────────────

    [Fact]
    public void OpenPeriod_GuardedToSupersetBasis_Rejected()
    {
        HasBasisAssignmentError("""
            precept P
            field Single as period in 'hours'
            field X as period
            state S initial
            event E
            from S on E when X.basis == 'hours + minutes'
                -> set Single = X
                -> no transition
            """).Should().BeTrue("{hours, minutes} ⊄ {hours} — a superset basis cannot be assigned to a narrower field");
    }

    [Fact]
    public void OpenPeriod_GuardedToDisjointBasis_Rejected()
    {
        HasBasisAssignmentError("""
            precept P
            field Composite as period in 'hours + minutes'
            field X as period
            state S initial
            event E
            from S on E when X.basis == 'days'
                -> set Composite = X
                -> no transition
            """).Should().BeTrue("{days} ⊄ {hours, minutes} — disjoint component sets do not satisfy the subset relation");
    }

    // ── Soundness: OR-collapse, negation, reassignment narrow nothing usable ───

    [Fact]
    public void OpenPeriod_OrGuard_DoesNotDischarge()
    {
        HasBasisAssignmentError("""
            precept P
            field Composite as period in 'hours + minutes'
            field X as period
            state S initial
            event E
            from S on E when X.basis == 'hours' or X.basis == 'days'
                -> set Composite = X
                -> no transition
            """).Should().BeTrue("the two OR branches narrow X to different bases — no single pinned value, so no discharge");
    }

    [Fact]
    public void OpenPeriod_NegatedGuard_DoesNotDischarge()
    {
        HasBasisAssignmentError("""
            precept P
            field Composite as period in 'hours + minutes'
            field X as period
            state S initial
            event E
            from S on E when X.basis != 'days'
                -> set Composite = X
                -> no transition
            """).Should().BeTrue("a `!=` guard pins no single basis — negation narrows nothing usable for a positive proof");
    }

    [Fact]
    public void OpenPeriod_ReassignedAfterGuard_DoesNotDischarge()
    {
        HasBasisAssignmentError("""
            precept P
            field Composite as period in 'hours + minutes'
            field Source as period in 'days'
            field X as period
            state S initial
            event E
            from S on E when X.basis == 'hours'
                -> set X = Source
                -> set Composite = X
                -> no transition
            """).Should().BeTrue("X is reassigned after the guard — the narrowing fact is stale and must not discharge");
    }

    // ── No guard: open source is unproven ──────────────────────────────────────

    [Fact]
    public void OpenPeriod_NoGuard_Rejected()
    {
        HasBasisAssignmentError("""
            precept P
            field Composite as period in 'hours + minutes'
            field X as period
            state S initial
            event E
            from S on E
                -> set Composite = X
                -> no transition
            """).Should().BeTrue("an open period with no narrowing guard has no proven basis — assignment is unproven");
    }

    // ── Declared baseline (Resolved path, no guard): subset accepts, mismatch rejects ──

    [Fact]
    public void DeclaredSubsetBasis_NoGuard_Accepted()
    {
        HasBasisAssignmentError("""
            precept P
            field Composite as period in 'hours + minutes'
            field Hourly as period in 'hours'
            state S initial
            event E
            from S on E
                -> set Composite = Hourly
                -> no transition
            """).Should().BeFalse("a declared {hours} basis ⊆ {hours, minutes} satisfies the assignment without any guard");
    }

    [Fact]
    public void DeclaredMismatchBasis_NoGuard_Rejected()
    {
        HasBasisAssignmentError("""
            precept P
            field Composite as period in 'hours + minutes'
            field Daily as period in 'days'
            state S initial
            event E
            from S on E
                -> set Composite = Daily
                -> no transition
            """).Should().BeTrue("a declared {days} basis ⊄ {hours, minutes} — the assignment is a definite mismatch");
    }

    // ── Literal source: a period literal derives its own basis and is subset-checked ──
    // (Exercises DerivePeriodLiteralBasis — the literal-basis exposure that makes the
    //  D14 tightening provable on default/literal assignment paths, not just set-actions.)

    [Fact]
    public void PeriodLiteralDefault_SubsetBasis_Accepted()
    {
        HasBasisAssignmentError("""
            precept P
            field Annual as period in 'years' default '2 years'
            state S initial
            """).Should().BeFalse("the literal '2 years' has basis {years} ⊆ the declared {years}");
    }

    [Fact]
    public void PeriodLiteralDefault_MismatchBasis_Rejected()
    {
        HasBasisAssignmentError("""
            precept P
            field Annual as period in 'years' default '2 months'
            state S initial
            """).Should().BeTrue("the literal '2 months' has basis {months} ⊄ the declared {years}");
    }

    [Fact]
    public void PeriodLiteralDefault_MixedClassBasis_Rejected()
    {
        HasBasisAssignmentError("""
            precept P
            field Annual as period in 'years' default '1 year + 2 hours'
            state S initial
            """).Should().BeTrue("the literal's basis {years, hours} ⊄ the declared {years}");
    }

    // ── Guard-side malformed basis literal surfaces the composite-basis diagnostic ──
    // (The G4 canonicalizer runs on the guard RHS, so a malformed basis is validated there.)

    [Fact]
    public void GuardMalformedBasisLiteral_EmitsCompositeBasisDiagnostic()
    {
        var diagnostics = Compiler.Compile("""
            precept P
            field Composite as period in 'hours + minutes'
            field X as period
            state S initial
            event E
            from S on E when X.basis == 'hours + bogus'
                -> set Composite = X
                -> no transition
            """).Diagnostics;

        diagnostics.Select(d => d.Code)
            .Should().Contain(nameof(DiagnosticCode.UnknownCompositeBasisComponent),
                "canonicalizing the guard RHS validates its components, surfacing the unknown atom 'bogus'");
    }
}
