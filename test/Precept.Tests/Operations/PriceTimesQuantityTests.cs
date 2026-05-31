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

        compilation.Diagnostics.Should().Contain(d => d.Code == Pre0114);
    }

    // ---- Known gap: cross-unit (same-dimension, different-unit) -------------

    // Resolving the quantity on the Dimension axis makes same-dimension but
    // different-unit pairs cancel silently, dropping the UCUM conversion factor
    // ('USD/kg' × 'g' is off by 1000×; 'each' × 'box' bypasses the count-unit
    // restriction of business-domain-types.md § quantity). Whether such pairs
    // should reject (exact-unit), auto-convert, or split by unit kind is an
    // undecided policy. This is skipped until that policy is decided — at which
    // point it is un-skipped (reject) or rewritten to assert the converted
    // result (auto-convert). It currently documents that silent cancellation is
    // not the intended end-state.
    [Fact(Skip = "Cross-unit price×quantity cancellation policy is undecided; it currently cancels silently and drops the conversion factor.")]
    public void CrossUnit_SameDimension_MustNotSilentlyCancel()
    {
        var compilation = Compiler.Compile("""
            precept Widget
            field Total as money in 'USD' default '0.00 USD' editable
            state Draft initial
            event Receive(UnitCost as price in 'USD/kg', Qty as quantity in 'g')
            from Draft on Receive -> set Total = Receive.UnitCost * Receive.Qty -> no transition
            """);

        compilation.Diagnostics.Should().Contain(d => d.Code == Pre0114);
    }
}
