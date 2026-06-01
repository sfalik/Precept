using System.Linq;
using FluentAssertions;
using Precept;
using Precept.Language;
using Precept.Pipeline;
using Precept.Tests.TypeChecker;
using Xunit;

namespace Precept.Tests.OperationsSuite;

/// <summary>
/// <c>price × quantity → money</c> dimensional cancellation. The price's
/// denominator dimension must match the quantity's dimension; matched operands
/// cancel to <c>money</c>, mismatched dimensions trip
/// <see cref="DiagnosticCode.UnprovedQualifierCompatibility"/> (PRE0114).
///
/// Covers the <c>in '&lt;unit&gt;'</c> declaration form (e.g. <c>quantity in 'kg'</c>),
/// whose dimension is carried on a <c>Unit</c> qualifier — distinct from the
/// <c>of '&lt;dimension&gt;'</c> form already exercised by
/// <c>ProofEngineTypedArgQualifierTests</c>. See
/// <c>business-domain-types.md</c> § price (the headline
/// <c>price in 'USD/each' × quantity in 'each'</c> example).
/// </summary>
public class PriceTimesQuantityTests
{
    private static ProofLedger Prove(string source)
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean(source);
        var graph = GraphAnalyzer.Analyze(index);
        return ProofEngine.Prove(index, graph);
    }

    private const string Pre0114 = nameof(DiagnosticCode.UnprovedQualifierCompatibility);

    // ---- Must cancel cleanly (no PRE0114) ------------------------------------

    [Fact]
    public void CompoundPrice_UnitQuantity_FieldForm_CancelsCleanly()
    {
        // Computed-field reference path (ResolveFieldQualifier). USD/kg × kg → USD.
        var compilation = Compiler.Compile("""
            precept OrderCost
            field UnitPrice as price in 'USD/kg' default '4.17 USD/kg' editable
            field Weight as quantity in 'kg' default '2 kg' editable
            field TotalCost as money in 'USD' <- UnitPrice * Weight
            state Open initial
            """);

        compilation.Diagnostics.Should().NotContain(d => d.Code == Pre0114);
    }

    [Fact]
    public void CountUnits_HeadlineExample_CancelsCleanly()
    {
        // business-domain-types.md headline example: (USD/each) × each → USD.
        var compilation = Compiler.Compile("""
            precept OrderCost
            field UnitPrice as price in 'USD/each' default '4.17 USD/each' editable
            field OrderQty as quantity in 'each' default '3 each' editable
            field TotalCost as money in 'USD' <- UnitPrice * OrderQty
            state Open initial
            """);

        compilation.Diagnostics.Should().NotContain(d => d.Code == Pre0114);
    }

    [Fact]
    public void CompoundPrice_UnitQuantity_EventArgForm_CancelsCleanly()
    {
        // Event-arg path (TypedArgRef / ResolveQualifierOnAxis arg branch).
        var compilation = Compiler.Compile("""
            precept Widget
            field Total as money in 'USD' default '0.00 USD' editable
            state Draft initial
            event Receive(UnitCost as price in 'USD/kg', Qty as quantity in 'kg')
            from Draft on Receive -> set Total = Receive.UnitCost * Receive.Qty -> no transition
            """);

        compilation.Diagnostics.Should().NotContain(d => d.Code == Pre0114);
    }

    [Fact]
    public void NonCompoundPriceOfDimension_UnitQuantity_CancelsCleanly()
    {
        // price 'in currency of dimension' (Dimension-axis) × quantity 'in unit' (Unit-axis).
        var compilation = Compiler.Compile("""
            precept Widget
            field Total as money in 'USD' default '0.00 USD' editable
            state Draft initial
            event Receive(UnitCost as price in 'USD' of 'mass', Qty as quantity in 'kg')
            from Draft on Receive -> set Total = Receive.UnitCost * Receive.Qty -> no transition
            """);

        compilation.Diagnostics.Should().NotContain(d => d.Code == Pre0114);
    }

    [Fact]
    public void QuantityTimesPrice_Commutative_CancelsCleanly()
    {
        // BidirectionalLookup: quantity × price resolves the same as price × quantity.
        var compilation = Compiler.Compile("""
            precept Widget
            field Total as money in 'USD' default '0.00 USD' editable
            state Draft initial
            event Receive(Qty as quantity in 'kg', UnitCost as price in 'USD/kg')
            from Draft on Receive -> set Total = Receive.Qty * Receive.UnitCost -> no transition
            """);

        compilation.Diagnostics.Should().NotContain(d => d.Code == Pre0114);
    }

    [Fact]
    public void CompoundPrice_UnitQuantity_DischargesQualifierChain()
    {
        // Stronger than "no PRE0114": the QualifierChain obligation actually proves.
        var ledger = Prove("""
            precept Widget
            field Total as money in 'USD' default '0.00 USD' editable
            state Draft initial
            event Receive(UnitCost as price in 'USD/kg', Qty as quantity in 'kg')
            from Draft on Receive -> set Total = Receive.UnitCost * Receive.Qty -> no transition
            """);

        ledger.Obligations
            .Where(o => o.Requirement is QualifierChainProofRequirement)
            .Should().ContainSingle()
            .Which.Disposition.Should().Be(ProofDisposition.Proved);

        ledger.Diagnostics.Should().NotContain(d => d.Code == Pre0114);
    }

    // ---- Must still error (PRE0114) — soundness guard ------------------------

    [Fact]
    public void DifferentDimension_UnitForm_StillErrors()
    {
        // mass (kg) vs length (m): different dimensions must not cancel.
        var compilation = Compiler.Compile("""
            precept Widget
            field Total as money in 'USD' default '0.00 USD' editable
            state Draft initial
            event Receive(UnitCost as price in 'USD/kg', Qty as quantity in 'm')
            from Draft on Receive -> set Total = Receive.UnitCost * Receive.Qty -> no transition
            """);

        var diagnostic = compilation.Diagnostics.Should().ContainSingle(d => d.Code == Pre0114).Which;
        // Domain-targeted PRE0114 names both operands so the mismatch is teachable.
        diagnostic.Message.Should().Contain("UnitCost").And.Contain("Qty");
    }

    [Fact]
    public void CrossCountingUnit_QuantityArithmetic_StillRejects()
    {
        // 'each' and 'box' share the count dimension but have no universal factor.
        // PRE0137 fires for quantity × quantity (both operands quantities) per
        // business-domain-types.md § quantity. (The price × quantity cancellation
        // path does not currently route to PRE0137 — see the count-unit note below.)
        var compilation = Compiler.Compile("""
            precept Widget
            field Total as quantity in 'each' default '0 each' editable
            state Draft initial
            event Receive(A as quantity in 'each', B as quantity in 'box')
            from Draft on Receive -> set Total = Receive.A * Receive.B -> no transition
            """);

        compilation.Diagnostics.Should()
            .Contain(d => d.Code == nameof(DiagnosticCode.CrossCountingUnitOperation));
    }

    // ---- Cross-unit (same-dimension, different-unit) cancels cleanly --------

    [Fact]
    public void CrossUnit_SameDimension_MustNotSilentlyCancel()
    {
        // USD/kg × g: same dimension (mass), different unit. The exact g→kg factor
        // is 1/1000; the cancellation is admitted (no PRE0114) and the conversion is
        // surfaced in hover (HoverHandlerTests). The runtime application of the factor
        // is the documented evaluator obligation (see docs/runtime/evaluator.md).
        var compilation = Compiler.Compile("""
            precept Widget
            field Total as money in 'USD' default '0.00 USD' editable
            state Draft initial
            event Receive(UnitCost as price in 'USD/kg', Qty as quantity in 'g')
            from Draft on Receive -> set Total = Receive.UnitCost * Receive.Qty -> no transition
            """);

        compilation.Diagnostics.Should().NotContain(d => d.Code == Pre0114);
    }

    [Fact]
    public void CrossUnit_InchFoot_CancelsCleanly()
    {
        // USD/[ft_i] × [in_i]: length, exact rational factor 1/12.
        var compilation = Compiler.Compile("""
            precept Widget
            field Total as money in 'USD' default '0.00 USD' editable
            state Draft initial
            event Receive(UnitCost as price in 'USD/[ft_i]', Qty as quantity in '[in_i]')
            from Draft on Receive -> set Total = Receive.UnitCost * Receive.Qty -> no transition
            """);

        compilation.Diagnostics.Should().NotContain(d => d.Code == Pre0114);
    }

    [Fact]
    public void CrossUnit_AffineAmount_CancelsCleanly()
    {
        // USD/Cel × [degF]: temperature amount; the amount-conversion scale (5/9) is an
        // exact rational. The affine offset is an absolute-reading concern, not a scale
        // concern — the cancellation is admitted.
        var compilation = Compiler.Compile("""
            precept Widget
            field Total as money in 'USD' default '0.00 USD' editable
            state Draft initial
            event Receive(UnitCost as price in 'USD/Cel', Qty as quantity in '[degF]')
            from Draft on Receive -> set Total = Receive.UnitCost * Receive.Qty -> no transition
            """);

        compilation.Diagnostics.Should().NotContain(d => d.Code == Pre0114);
    }

    // ---- Exactness guard: ScaleIsRational catalog flag ----------------------

    [Theory]
    [InlineData("kg")]
    [InlineData("[ft_i]")]
    [InlineData("[in_i]")]
    [InlineData("g")]
    [InlineData("Cel")]
    [InlineData("[degF]")]
    public void ScaleIsRational_IsTrue_ForMetricCustomaryAndAffineUnits(string code)
    {
        UcumAtomCatalog.All.TryGetValue(code, out var atom).Should().BeTrue($"'{code}' should be a cataloged atom");
        atom!.ScaleIsRational.Should().BeTrue(
            $"'{code}' has an exact-rational scale-to-base (affine offset is a separate concern)");
    }

    [Theory]
    [InlineData("B")]      // bel (logarithmic) — backs the dB unit
    [InlineData("Np")]     // neper (logarithmic)
    [InlineData("[pH]")]   // pH (logarithmic special function — not just lg/ln)
    [InlineData("deg")]    // degree of arc ([pi]-reducing → irrational)
    [InlineData("rad")]    // radian — angle family
    [InlineData("gon")]    // gon ([pi]-reducing)
    public void ScaleIsRational_IsFalse_ForLogAndAngleUnits(string code)
    {
        UcumAtomCatalog.All.TryGetValue(code, out var atom).Should().BeTrue($"'{code}' should be a cataloged atom");
        atom!.ScaleIsRational.Should().BeFalse(
            $"'{code}' has a non-affine special-function or [pi]-derived scale that is not an exact rational");
    }

    // ---- Characterization: out-of-scope count gap (price-path) ---------------

    [Fact]
    public void CrossCountingUnit_PricePath_CurrentlyCancelsSilently_KnownGap()
    {
        // KNOWN GAP (out of this slice's scope): `price × quantity` across two count
        // units with no universal factor (`each` vs `box`) currently cancels silently —
        // the count-mismatch rejection (PRE0137) is gated on BOTH operands being
        // quantities (TypeChecker), so the price-cancellation path skips it. This test
        // pins the *current* behavior so a regression is noticed; it does NOT endorse it.
        var compilation = Compiler.Compile("""
            precept Widget
            field Total as money in 'USD' default '0.00 USD' editable
            state Draft initial
            event Receive(UnitCost as price in 'USD/each', Qty as quantity in 'box')
            from Draft on Receive -> set Total = Receive.UnitCost * Receive.Qty -> no transition
            """);

        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.CrossCountingUnitOperation));
        compilation.Diagnostics.Should().NotContain(d => d.Code == Pre0114);
    }
}
