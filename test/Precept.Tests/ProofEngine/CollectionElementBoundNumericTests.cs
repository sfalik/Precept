using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Collection inner-type value modifiers — Phase 2 (numeric / qualified parity).
/// A collection's numeric inner type may carry `min`/`max`/flag modifiers; the bound is
/// validated per element, proven at numeric write-sites and default literals, and carried
/// at numeric read-sites (`.min`/`.max`/`.peek`/…) so an element read into a same-or-wider
/// numeric field discharges. Uses <see cref="Compiler.Compile"/>.
/// </summary>
public class CollectionElementBoundNumericTests
{
    private static ImmutableArray<Diagnostic> Compile(string source)
        => Compiler.Compile(source).Diagnostics;

    private static bool Has(ImmutableArray<Diagnostic> d, DiagnosticCode code)
        => d.Any(x => x.Code == code.ToString());

    // A min/max element-range violation on a set-action reuses the scalar interval-containment
    // path; accept either code that path can surface.
    private static bool HasRangeViolation(ImmutableArray<Diagnostic> d)
        => Has(d, DiagnosticCode.NumericOverflow) || Has(d, DiagnosticCode.OutOfRange);

    // ── Typing ──────────────────────────────────────────────────────────────────

    [Fact]
    public void ElementMinMax_OnIntegerInner_NoModifierError()
    {
        var d = Compile("""
            precept T
            field S as set of integer min 0 max 100 optional
            state Open initial
            """);

        Has(d, DiagnosticCode.InvalidModifierForType).Should().BeFalse(
            because: "min/max are legal on an integer element; they bind to the inner type, not the set field");
    }

    [Fact]
    public void ElementMin_OnStringInner_EmitsInvalidModifierForType()
    {
        var d = Compile("""
            precept T
            field S as set of string min 0 optional
            state Open initial
            """);

        Has(d, DiagnosticCode.InvalidModifierForType).Should().BeTrue(
            because: "min does not apply to a string element");
    }

    [Fact]
    public void ElementNonnegative_OnMoneyInner_NoModifierError()
    {
        var d = Compile("""
            precept T
            field S as set of money in 'USD' nonnegative optional
            state Open initial
            """);

        Has(d, DiagnosticCode.InvalidModifierForType).Should().BeFalse(
            because: "nonnegative is legal on a money element and composes with the currency qualifier");
    }

    // ── Write-site obligation ────────────────────────────────────────────────────

    [Fact]
    public void Add_OutOfRangeSource_EmitsRangeViolation()
    {
        var d = Compile("""
            precept T
            field S as set of integer min 0 max 100 optional
            state Open initial
            state Done terminal
            event Add(V as integer)
            from Open on Add
                -> add S Add.V
                -> transition Done
            """);

        HasRangeViolation(d).Should().BeTrue(
            because: "an unbounded source added into a [0,100] element cannot be proven within the bound");
    }

    [Fact]
    public void Add_InRangeSource_Clean()
    {
        var d = Compile("""
            precept T
            field S as set of integer min 0 max 100 optional
            state Open initial
            state Done terminal
            event Add(V as integer min 0 max 100)
            from Open on Add
                -> add S Add.V
                -> transition Done
            """);

        HasRangeViolation(d).Should().BeFalse(
            because: "a [0,100] source added into a [0,100] element is provably within the bound");
    }

    // ── Read-site reach ──────────────────────────────────────────────────────────

    [Fact]
    public void Max_ElementBoundIntoSameBoundField_Clean()
    {
        var d = Compile("""
            precept T
            field S as set of integer min 0 max 100 optional
            field Dst as integer optional min 0 max 100
            state Open initial
            state Done terminal
            event Take
            from Open on Take when S.count > 0
                -> set Dst = S.max
                -> transition Done
            """);

        HasRangeViolation(d).Should().BeFalse(
            because: "the element carries [0,100], so .max discharges into a [0,100] field");
    }

    [Fact]
    public void Max_ElementBoundIntoNarrowerField_EmitsRangeViolation()
    {
        var d = Compile("""
            precept T
            field S as set of integer min 0 max 100 optional
            field Dst as integer optional min 0 max 50
            state Open initial
            state Done terminal
            event Take
            from Open on Take when S.count > 0
                -> set Dst = S.max
                -> transition Done
            """);

        HasRangeViolation(d).Should().BeTrue(
            because: ".max carries [0,100], which cannot be proven within [0,50]");
    }

    // ── default [...] numeric element-entry path ─────────────────────────────────

    [Fact]
    public void DefaultLiteral_OutOfRangeNumericElement_EmitsRangeViolation()
    {
        var d = Compile("""
            precept T
            field S as set of integer min 0 max 5 default [10]
            state Open initial
            """);

        HasRangeViolation(d).Should().BeTrue(
            because: "the default element 10 exceeds the element max 5");
    }

    [Fact]
    public void DefaultLiteral_InRangeNumericElement_Clean()
    {
        var d = Compile("""
            precept T
            field S as set of integer min 0 max 5 default [3, 5]
            state Open initial
            """);

        HasRangeViolation(d).Should().BeFalse(
            because: "all default elements are within [0,5]");
    }

    // ── Quantifier-binding reach (Design Acceptance 6) ───────────────────────────
    // The binding `x` over a bounded-element collection carries band(m) into the
    // predicate's value-interval context — white-box because the obligations that
    // consume an interval over a rule-predicate binding (overflow/OutOfRange) are not
    // readily generated there, but the seeding is the load-bearing change and is
    // directly observable on the resolved binding reference.

    [Fact]
    public void QuantifierBinding_OverBoundedElement_CarriesElementBand()
    {
        var manifest = Precept.Pipeline.Parser.Parse(Lexer.Lex("""
            precept T
            field S as set of integer min 0 max 100
            state Open initial
            """));
        var symbols = Precept.Pipeline.NameBinder.Bind(manifest);
        var ctx = Precept.Pipeline.TypeChecker.CreateContext(manifest, symbols);

        var span = new SourceSpan(0, 1, 1, 1, 1, 2);
        // no x in S (x > 100)
        var predicate = new BinaryOperationExpression(
            new IdentifierExpression("x", span),
            TokenKind.GreaterThan,
            new LiteralExpression(TokenKind.NumberLiteral, "100", span),
            span);
        var expr = new QuantifierExpression(
            TokenKind.No, "x", new IdentifierExpression("S", span), predicate, span);

        var resolved = Precept.Pipeline.TypeChecker.ResolveExpression(expr, ctx);

        resolved.Should().BeOfType<TypedQuantifier>();
        var quant = (TypedQuantifier)resolved;
        var bin = quant.Predicate.Should().BeOfType<TypedBinaryOp>().Subject;
        var bindingRef = bin.Left.Should().BeOfType<TypedFieldRef>().Subject;
        bindingRef.FieldName.Should().Be("x");
        bindingRef.ElementBounds.Should().NotBeNull(
            because: "the binding ranges over the collection's bounded elements");
        bindingRef.ElementBounds!.DeclaredMin.Should().Be(0m);
        bindingRef.ElementBounds!.DeclaredMax.Should().Be(100m,
            because: "x carries [0,100] — the element band — into the predicate");
    }

    // ── Qualified watch-cell: money/quantity element bound read into a qualified dest ──
    // The least-tested parity cell. The numeric read-reach feeds IntervalOfNarrowed, which
    // normalizes the element bound to UCUM/currency base units against the destination.

    [Fact]
    public void MoneyElementMax_IntoSameCurrencyField_Clean()
    {
        var d = Compile("""
            precept T
            field S as set of money in 'USD' min '0 USD' max '100 USD' optional
            field Dst as money in 'USD' optional min '0 USD' max '100 USD'
            state Open initial
            state Done terminal
            event Take
            from Open on Take when S.count > 0
                -> set Dst = S.max
                -> transition Done
            """);

        HasRangeViolation(d).Should().BeFalse(
            because: "the money element carries [0,100] USD, so .max discharges into a [0,100] USD field");
    }

    [Fact]
    public void MoneyElementMax_IntoNarrowerCurrencyField_EmitsRangeViolation()
    {
        var d = Compile("""
            precept T
            field S as set of money in 'USD' min '0 USD' max '100 USD' optional
            field Dst as money in 'USD' optional min '0 USD' max '50 USD'
            state Open initial
            state Done terminal
            event Take
            from Open on Take when S.count > 0
                -> set Dst = S.max
                -> transition Done
            """);

        HasRangeViolation(d).Should().BeTrue(
            because: ".max carries [0,100] USD, which cannot be proven within [0,50] USD — currency normalization works");
    }

    // ── Accessor gating: `.count` is cardinality, NOT an element ──────────────────
    // The numeric read-reach band is the ELEMENT's value band; only an element-returning
    // accessor may carry it. `.count` is a FixedReturnAccessor returning the collection's
    // cardinality (Integer in [0, count]), which is unrelated to the element band. It must
    // NOT inherit the element band, or a count well outside the element range falsely
    // discharges — a soundness hole.

    [Fact]
    public void Count_DoesNotInheritElementBand_EmitsRangeViolation()
    {
        var d = Compile("""
            precept T
            field S as set of integer min 50 max 100 optional
            field Dst as integer optional min 50 max 100
            state Open initial
            state Done terminal
            event Take
            from Open on Take
                -> set Dst = S.count
                -> transition Done
            """);

        HasRangeViolation(d).Should().BeTrue(
            because: ".count is the cardinality ([0, ∞)), not an element — it can be 0, outside the element band [50,100], so it must not discharge into a [50,100] field");
    }

    // ── Cross-currency element read into a mismatched-currency destination ───────
    // A money element read via .max into a destination of a DIFFERENT currency must
    // reject on the qualifier axis — currencies are not UCUM-convertible.

    [Fact]
    public void MoneyElementMax_IntoDifferentCurrencyField_EmitsQualifierMismatch()
    {
        var d = Compile("""
            precept T
            field S as set of money in 'EUR' min '0 EUR' max '100 EUR' optional
            field Dst as money in 'USD' optional min '0 USD' max '100 USD'
            state Open initial
            state Done terminal
            event Take
            from Open on Take when S.count > 0
                -> set Dst = S.max
                -> transition Done
            """);

        (Has(d, DiagnosticCode.QualifierMismatch)
            || Has(d, DiagnosticCode.UnprovedAssignmentQualifierCompatibility)
            || Has(d, DiagnosticCode.UnprovedQualifierCompatibility)).Should().BeTrue(
            because: "a EUR element read via .max cannot be assigned into a USD field — currencies are not convertible");
    }
}
