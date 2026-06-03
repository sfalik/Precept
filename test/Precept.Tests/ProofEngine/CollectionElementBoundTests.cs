using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Collection inner-type value modifiers — Phase 1 (string-length half).
/// A collection's string inner type may carry `maxlength`/`minlength`; the bound is
/// validated per element, proven at string write-sites, and carried at string read-sites
/// (`.peek`/`.first`/`.last`) so an element read into a same-or-wider-bounded field
/// discharges (closes BUG-019 residual gap 1). Uses <see cref="Compiler.Compile"/>.
/// </summary>
public class CollectionElementBoundTests
{
    private static ImmutableArray<Diagnostic> Compile(string source)
        => Compiler.Compile(source).Diagnostics;

    private static bool Has(ImmutableArray<Diagnostic> d, DiagnosticCode code)
        => d.Any(x => x.Code == code.ToString());

    // ── Typing ──────────────────────────────────────────────────────────────────

    [Fact]
    public void ElementMaxlength_OnStringInner_NoModifierError()
    {
        // maxlength binds to the string ELEMENT, not the queue field — no PRE0033.
        var d = Compile("""
            precept T
            field Q as queue of string maxlength 200 optional
            state Open initial
            """);

        Has(d, DiagnosticCode.InvalidModifierForType).Should().BeFalse(
            because: "maxlength is legal on a string element; it binds to the inner type, not the queue field");
    }

    [Fact]
    public void ElementMaxlength_OnIntegerInner_EmitsInvalidModifierForType()
    {
        var d = Compile("""
            precept T
            field S as set of integer maxlength 5 optional
            state Open initial
            """);

        Has(d, DiagnosticCode.InvalidModifierForType).Should().BeTrue(
            because: "maxlength does not apply to an integer element");
    }

    // ── Read-site reach (the gap closer) ─────────────────────────────────────────

    [Fact]
    public void Peek_ElementBoundIntoSameBoundField_Clean()
    {
        var d = Compile("""
            precept T
            field Q as queue of string maxlength 200 optional
            field Dst as string optional maxlength 200
            state Open initial
            state Done terminal
            event Take
            from Open on Take when Q.count > 0
                -> set Dst = Q.peek
                -> transition Done
            """);

        Has(d, DiagnosticCode.LengthBoundViolation).Should().BeFalse(
            because: "the element carries maxlength 200, so .peek discharges into a maxlength 200 field");
    }

    [Fact]
    public void Peek_ElementBoundIntoNarrowerField_EmitsLengthViolation()
    {
        var d = Compile("""
            precept T
            field Q as queue of string maxlength 200 optional
            field Dst as string optional maxlength 100
            state Open initial
            state Done terminal
            event Take
            from Open on Take when Q.count > 0
                -> set Dst = Q.peek
                -> transition Done
            """);

        Has(d, DiagnosticCode.LengthBoundViolation).Should().BeTrue(
            because: ".peek carries (0,200), which cannot be proven within maxlength 100");
    }

    [Fact]
    public void Peek_NoElementBound_StillUnbounded_EmitsLengthViolation()
    {
        // Regression / monotonicity: a collection with no element modifier behaves as
        // before — the element read is unbounded, so it cannot prove into a capped field.
        var d = Compile("""
            precept T
            field Q as queue of string optional
            field Dst as string optional maxlength 200
            state Open initial
            state Done terminal
            event Take
            from Open on Take when Q.count > 0
                -> set Dst = Q.peek
                -> transition Done
            """);

        Has(d, DiagnosticCode.LengthBoundViolation).Should().BeTrue(
            because: "with no element bound the read is unbounded — unchanged pre-feature behavior");
    }

    // ── Write-site obligation ────────────────────────────────────────────────────

    [Fact]
    public void Enqueue_UnboundedSourceIntoBoundedElement_EmitsLengthViolation()
    {
        var d = Compile("""
            precept T
            field Q as queue of string maxlength 5 optional
            state Open initial
            state Done terminal
            event Add(Name as string)
            from Open on Add
                -> enqueue Q Add.Name
                -> transition Done
            """);

        Has(d, DiagnosticCode.LengthBoundViolation).Should().BeTrue(
            because: "an unbounded source enqueued into a maxlength 5 element cannot be proven within the bound");
    }

    [Fact]
    public void Enqueue_BoundedSourceIntoBoundedElement_Clean()
    {
        var d = Compile("""
            precept T
            field Q as queue of string maxlength 5 optional
            state Open initial
            state Done terminal
            event Add(Name as string maxlength 5)
            from Open on Add
                -> enqueue Q Add.Name
                -> transition Done
            """);

        Has(d, DiagnosticCode.LengthBoundViolation).Should().BeFalse(
            because: "a maxlength 5 source enqueued into a maxlength 5 element is provably within the bound");
    }

    // ── default [...] literal element-entry path (matched-pair: must be checked too) ──

    [Fact]
    public void DefaultLiteral_OverBoundElement_EmitsLengthViolation()
    {
        // A default list literal is an element-entry path; an over-bound element must reject,
        // or the read-reach would carry a bound the default element violates.
        var d = Compile("""
            precept T
            field L as list of string maxlength 5 default ["abcdefghij"]
            state Open initial
            """);

        Has(d, DiagnosticCode.LengthBoundViolation).Should().BeTrue(
            because: "the 10-char default element exceeds the element maxlength 5");
    }

    [Fact]
    public void DefaultLiteral_WithinBoundElement_Clean()
    {
        var d = Compile("""
            precept T
            field L as list of string maxlength 5 default ["abc", "de"]
            state Open initial
            """);

        Has(d, DiagnosticCode.LengthBoundViolation).Should().BeFalse(
            because: "all default elements are within the element maxlength 5");
    }
}
