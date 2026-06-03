using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Non-literal string-length containment (BUG-019): a `set`-action assigning a
/// non-literal string (concatenation, field/arg reference, conditional) into a
/// length-bounded field generates a <see cref="LengthContainmentProofRequirement"/>
/// unconditionally and discharges it against a string-length interval — provably
/// within bounds ⇒ clean; provably violating or not provable ⇒ emit
/// <see cref="DiagnosticCode.LengthBoundViolation"/> (§0.7 prove-or-reject).
///
/// The "clean" cases mirror the idiomatic sample-corpus shapes (arg-ref within
/// bound, bounded concat, if-of-literals) — they are the over-rejection Falsifier:
/// the length-interval domain must be rich enough to prove them, or the corpus
/// goes red. Uses <see cref="Compiler.Compile"/> (full pipeline).
/// </summary>
public class LengthContainmentEmissionTests
{
    private static ImmutableArray<Diagnostic> Compile(string source)
        => Compiler.Compile(source).Diagnostics;

    private static bool HasLengthViolation(ImmutableArray<Diagnostic> diagnostics)
        => diagnostics.Any(d => d.Code == nameof(DiagnosticCode.LengthBoundViolation));

    // ── Emit cases (RED against current literal-only gate) ──────────────────────

    [Fact]
    public void Concat_UnboundedArgs_EmitsLengthViolation()
    {
        // E.First / E.Last are unbounded-length args; their concat cannot be proven
        // <= maxlength 10. This is the BUG-019 repro.
        var diagnostics = Compile("""
            precept T
            field Name as string maxlength 10 default ""
            state Open initial
            state Done terminal
            event E(First as string, Last as string)
            from Open on E
                -> set Name = E.First + E.Last
                -> transition Done
            """);

        HasLengthViolation(diagnostics).Should().BeTrue(
            because: "E.First + E.Last is unbounded-length, so it cannot be proven within maxlength 10");
    }

    [Fact]
    public void Concat_BoundedButExceeds_EmitsLengthViolation()
    {
        // First maxlength 8 + Last maxlength 8 = up to 16 > maxlength 10.
        var diagnostics = Compile("""
            precept T
            field Name as string maxlength 10 default ""
            state Open initial
            state Done terminal
            event E(First as string maxlength 8, Last as string maxlength 8)
            from Open on E
                -> set Name = E.First + E.Last
                -> transition Done
            """);

        HasLengthViolation(diagnostics).Should().BeTrue(
            because: "First[..8] + Last[..8] can reach 16, exceeding maxlength 10");
    }

    [Fact]
    public void ArgRef_ExceedsTargetBound_EmitsLengthViolation()
    {
        // Source arg maxlength 50 assigned to a maxlength 10 field — can violate.
        var diagnostics = Compile("""
            precept T
            field Name as string maxlength 10 default ""
            state Open initial
            state Done terminal
            event E(Src as string maxlength 50)
            from Open on E
                -> set Name = E.Src
                -> transition Done
            """);

        HasLengthViolation(diagnostics).Should().BeTrue(
            because: "an arg bounded to 50 chars can exceed the target's maxlength 10");
    }

    // ── Must-stay-clean cases (the over-rejection Falsifier: corpus idioms) ──────

    [Fact]
    public void ArgRef_WithinTargetBound_Clean()
    {
        // Mirrors `set PatientName = Schedule.Name` (arg maxlength <= field maxlength).
        var diagnostics = Compile("""
            precept T
            field Name as string maxlength 200 default ""
            state Open initial
            state Done terminal
            event E(Src as string notempty maxlength 200)
            from Open on E
                -> set Name = E.Src
                -> transition Done
            """);

        HasLengthViolation(diagnostics).Should().BeFalse(
            because: "an arg bounded to <= the target's maxlength is provably within bounds");
    }

    [Fact]
    public void Concat_BoundedWithinTarget_Clean()
    {
        // First[..3] + Last[..4] = up to 7 <= maxlength 10.
        var diagnostics = Compile("""
            precept T
            field Name as string maxlength 10 default ""
            state Open initial
            state Done terminal
            event E(First as string maxlength 3, Last as string maxlength 4)
            from Open on E
                -> set Name = E.First + E.Last
                -> transition Done
            """);

        HasLengthViolation(diagnostics).Should().BeFalse(
            because: "First[..3] + Last[..4] is at most 7, within maxlength 10");
    }

    [Fact]
    public void FieldRef_WithinTargetBound_Clean()
    {
        var diagnostics = Compile("""
            precept T
            field Other as string maxlength 5 default ""
            field Name as string maxlength 10 default ""
            state Open initial
            state Done terminal
            event E
            from Open on E
                -> set Name = Other
                -> transition Done
            """);

        HasLengthViolation(diagnostics).Should().BeFalse(
            because: "a field bounded to maxlength 5 is provably within the target's maxlength 10");
    }

    [Fact]
    public void Conditional_OfLiterals_WithinTarget_Clean()
    {
        // Mirrors `set FinalNote = if G then "Strong Hire" else "Standard Hire"`.
        var diagnostics = Compile("""
            precept T
            field Flag as boolean default false
            field Note as string maxlength 20 default ""
            state Open initial
            state Done terminal
            event E
            from Open on E
                -> set Note = if Flag then "Strong Hire" else "Standard Hire"
                -> transition Done
            """);

        HasLengthViolation(diagnostics).Should().BeFalse(
            because: "both branches are literals of length <= 13, within maxlength 20");
    }

    [Fact]
    public void NonLiteral_NoTargetLengthBound_NoObligation_Clean()
    {
        // Target has no length bound — no length obligation regardless of RHS.
        var diagnostics = Compile("""
            precept T
            field Name as string default ""
            state Open initial
            state Done terminal
            event E(First as string, Last as string)
            from Open on E
                -> set Name = E.First + E.Last
                -> transition Done
            """);

        HasLengthViolation(diagnostics).Should().BeFalse(
            because: "no maxlength/minlength on the target ⇒ no length-containment obligation");
    }

    // ── Literal regression (the existing live path must be unchanged) ───────────

    [Fact]
    public void Literal_WithinBound_Clean()
    {
        var diagnostics = Compile("""
            precept T
            field Name as string maxlength 10 default ""
            state Open initial
            state Done terminal
            event E
            from Open on E
                -> set Name = "short"
                -> transition Done
            """);

        HasLengthViolation(diagnostics).Should().BeFalse(
            because: "literal 'short' (5 chars) is within maxlength 10");
    }

    [Fact]
    public void Literal_ExceedsBound_EmitsLengthViolation()
    {
        var diagnostics = Compile("""
            precept T
            field Name as string maxlength 10 default ""
            state Open initial
            state Done terminal
            event E
            from Open on E
                -> set Name = "this is far too long"
                -> transition Done
            """);

        HasLengthViolation(diagnostics).Should().BeTrue(
            because: "literal of 20 chars exceeds maxlength 10 (existing literal path)");
    }

    [Fact]
    public void Interpolation_UnboundedQuantityHole_EmitsLengthViolation()
    {
        // The statistical-process-control shape: a maxlength field assembled by
        // interpolating an open-ended quantity measurement (no magnitude bound) →
        // unbounded length → cannot be proven within the cap, so Precept rejects.
        var diagnostics = Compile("""
            precept T
            field Reason as string maxlength 20 optional
            state Open initial
            state Done terminal
            event Record(Value as quantity of 'mass')
            from Open on Record
                -> set Reason = "Sample {Record.Value} is out of range"
                -> transition Done
            """);

        HasLengthViolation(diagnostics).Should().BeTrue(
            because: "an open-ended quantity interpolated into a maxlength field cannot be proven within the cap");
    }
}
