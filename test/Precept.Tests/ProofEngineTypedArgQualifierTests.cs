using System;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Precept;
using Precept.Language;
using Precept.Pipeline;
using Precept.Tests.TypeChecker;
using Xunit;

namespace Precept.Tests;

public class ProofEngineTypedArgQualifierTests
{
    private static string SamplesRoot =>
        Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "samples"));

    private static ProofLedger Prove(string source)
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean(source);
        var graph = GraphAnalyzer.Analyze(index);
        return ProofEngine.Prove(index, graph);
    }

    private static DeclaredQualifierMeta? ResolveInterpolatedConstantQualifier(
        InterpolatedTypedConstant constant,
        QualifierAxis axis)
    {
        var method = typeof(ProofEngine).GetMethod(
            "ResolveQualifierFromInterpolatedConstant",
            BindingFlags.NonPublic | BindingFlags.Static);

        method.Should().NotBeNull();
        return (DeclaredQualifierMeta?)method!.Invoke(null, new object?[] { constant, axis });
    }

    private static InterpolatedTypedConstant GetRuleRightConstant(SemanticIndex index)
    {
        index.Rules.Should().ContainSingle();
        index.Rules[0].Condition.Should().BeOfType<TypedBinaryOp>();

        var comparison = (TypedBinaryOp)index.Rules[0].Condition;
        comparison.Right.Should().BeOfType<InterpolatedTypedConstant>();
        return (InterpolatedTypedConstant)comparison.Right;
    }

    private static DeclaredQualifierMeta? ResolveExpressionQualifier(
        TypedExpression expression,
        QualifierAxis axis,
        SemanticIndex semantics)
    {
        var method = typeof(ProofEngine).GetMethod(
            "ResolveQualifierFromExpression",
            BindingFlags.NonPublic | BindingFlags.Static);

        method.Should().NotBeNull();
        return (DeclaredQualifierMeta?)method!.Invoke(null, new object?[] { expression, axis, semantics });
    }

    private static TypedField CreateField(string name, TypeKind type, params DeclaredQualifierMeta[] qualifiers)
        => new(
            name,
            type,
            null,
            null,
            ImmutableArray<ModifierKind>.Empty,
            ImmutableArray<ModifierKind>.Empty,
            null,
            null,
            null,
            false,
            false,
            true,
            new DeclaredPresenceMeta.Guaranteed(),
            ImmutableArray.Create(qualifiers),
            SourceSpan.Missing,
            null!);

    [Fact]
    public void CompoundUnitInterpolatedConstant_ResolvesCompoundUnitQualifier()
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean("""
            precept Widget
            field StockingUnit as unitofmeasure default 'each'
            field SaleUnit as unitofmeasure default 'case'
            field Ratio as quantity in '{StockingUnit}/{SaleUnit}' default '1 {StockingUnit}/{SaleUnit}'
            rule Ratio > '0 {StockingUnit}/{SaleUnit}' because "positive"
            """);

        var qualifier = ResolveInterpolatedConstantQualifier(GetRuleRightConstant(index), QualifierAxis.Unit);

        qualifier.Should().BeEquivalentTo(new DeclaredQualifierMeta.Unit("{StockingUnit}/{SaleUnit}", "{StockingUnit}/{SaleUnit}"));
    }

    [Fact]
    public void SingleUnitInterpolatedConstant_StillResolvesSingleUnitQualifier()
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean("""
            precept Widget
            field SaleUnit as unitofmeasure default 'case'
            field Qty as quantity in '{SaleUnit}' default '1 {SaleUnit}'
            rule Qty > '0 {SaleUnit}' because "positive"
            """);

        var qualifier = ResolveInterpolatedConstantQualifier(GetRuleRightConstant(index), QualifierAxis.Unit);

        qualifier.Should().BeEquivalentTo(new DeclaredQualifierMeta.Unit("{SaleUnit}", "{SaleUnit}", SourceFieldName: "SaleUnit"));
    }

    [Fact]
    public void CompoundUnitRule_DoesNotEmit_PRE0114()
    {
        var compilation = Compiler.Compile("""
            precept Widget
            field StockingUnit as unitofmeasure default 'each'
            field SaleUnit as unitofmeasure default 'case'
            field Ratio as quantity in '{StockingUnit}/{SaleUnit}' default '1 {StockingUnit}/{SaleUnit}'
            rule Ratio > '0 {StockingUnit}/{SaleUnit}' because "positive"
            """);

        compilation.HasErrors.Should().BeFalse();
        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UnprovedQualifierCompatibility));
    }

    [Fact]
    public void CompoundUnitPositivityProof_ClearsDivisionByZero()
    {
        var compilation = Compiler.Compile("""
            precept Widget
            field CatalogCurrency as currency default 'USD'
            field StockingUnit as unitofmeasure default 'each'
            field SaleUnit as unitofmeasure default 'case'
            field ListPrice as price in '{CatalogCurrency}' of '{SaleUnit.dimension}' default '24 {CatalogCurrency}/{SaleUnit}'
            field StockingUnitsPerSaleUnit as quantity in '{StockingUnit}/{SaleUnit}' default '12 {StockingUnit}/{SaleUnit}'
            field UnitPrice as price in '{CatalogCurrency}' of '{StockingUnit.dimension}' <- ListPrice / StockingUnitsPerSaleUnit
            rule StockingUnitsPerSaleUnit > '0 {StockingUnit}/{SaleUnit}' because "positive"
            """);

        compilation.HasErrors.Should().BeFalse();
        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UnprovedQualifierCompatibility));
        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.DivisionByZero));
    }

    [Fact]
    public void CompoundCancellationResolver_LengthPerTimeAndTime_ProducesLengthForProofEngine()
    {
        var speedField = CreateField(
            "Speed",
            TypeKind.Quantity,
            new DeclaredQualifierMeta.Unit("m/s", "length/time"));
        var elapsedField = CreateField(
            "Elapsed",
            TypeKind.Quantity,
            new DeclaredQualifierMeta.Unit("s", "time"));
        var semantics = SemanticIndex.Empty with
        {
            Fields = ImmutableArray.Create(speedField, elapsedField),
            FieldsByName = new[] { speedField, elapsedField }.ToFrozenDictionary(field => field.Name),
        };
        var expression = new TypedBinaryOp(
            TypeKind.Quantity,
            OperationKind.QuantityTimesQuantity,
            new TypedFieldRef(TypeKind.Quantity, "Speed", false, speedField.DeclaredQualifiers, SourceSpan.Missing),
            new TypedFieldRef(TypeKind.Quantity, "Elapsed", false, elapsedField.DeclaredQualifiers, SourceSpan.Missing),
            new CompoundUnitCancellationRequired(),
            ImmutableArray<ProofRequirement>.Empty,
            SourceSpan.Missing);

        var unit = ResolveExpressionQualifier(expression, QualifierAxis.Unit, semantics)
            .Should().BeOfType<DeclaredQualifierMeta.Unit>().Which;
        unit.UnitCode.Should().Be("m");
        unit.DimensionName.Should().Be("length");
        unit.Origin.Should().Be(QualifierOrigin.Derived);

        var dimension = ResolveExpressionQualifier(expression, QualifierAxis.Dimension, semantics)
            .Should().BeOfType<DeclaredQualifierMeta.Dimension>().Which;
        dimension.DimensionName.Should().Be("length");
        dimension.Origin.Should().Be(QualifierOrigin.Derived);
    }

    [Fact]
    public void TypedArgRefs_Discharge_QualifierChainProofs()
    {
        var ledger = Prove("""
            precept Widget
            field Total as money in 'USD' default '0.00 USD' writable
            state Draft initial
            event Receive(UnitCost as price in 'USD' of 'mass', Qty as quantity of 'mass')
            from Draft on Receive -> set Total = Receive.UnitCost * Receive.Qty -> no transition
            """);

        ledger.Obligations
            .Where(o => o.Requirement is QualifierChainProofRequirement)
            .Should().ContainSingle()
            .Which.Disposition.Should().Be(ProofDisposition.Proved);

        ledger.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UnprovedQualifierCompatibility));
    }

    [Fact]
    public void TypedArgRefs_Use_ArgNames_In_QualifierDiagnostics()
    {
        var compilation = Compiler.Compile("""
            precept Widget
            field Total as money in 'USD' default '0.00 USD' writable
            state Draft initial
            event Receive(UnitCost as price in 'USD' of 'mass', Qty as quantity of 'length')
            from Draft on Receive -> set Total = Receive.UnitCost * Receive.Qty -> no transition
            """);

        var diagnostic = compilation.Diagnostics
            .Single(d => d.Code == nameof(DiagnosticCode.UnprovedQualifierCompatibility));

        diagnostic.Message.Should().Contain("UnitCost");
        diagnostic.Message.Should().Contain("Qty");
        diagnostic.Message.Should().NotContain("<unknown>");
    }

    [Fact]
    public void InventoryItem_Sample_Clears_G1_Diagnostics()
    {
        var source = File.ReadAllText(Path.Combine(SamplesRoot, "inventory-item.precept"));
        var compilation = Compiler.Compile(source);

        compilation.Diagnostics.Should().NotContain(d =>
            (d.Code == nameof(DiagnosticCode.UnprovedQualifierCompatibility)
                && (d.Span.StartLine == 122 || d.Span.StartLine == 123))
            || (d.Code == nameof(DiagnosticCode.DivisionByZero)
                && (d.Span.StartLine == 137 || d.Span.StartLine == 142)));
    }

    [Fact]
    public void InventoryItem_Sample_PRE0114_Count_Drops_Below_Baseline()
    {
        var source = File.ReadAllText(Path.Combine(SamplesRoot, "inventory-item.precept"));
        var compilation = Compiler.Compile(source);

        compilation.Diagnostics
            .Count(d => d.Code == nameof(DiagnosticCode.UnprovedQualifierCompatibility))
            .Should().BeLessThan(73, because: "C4 should reduce the inventory-item PRE0114 baseline");
    }

    [Fact]
    public void InventoryItem_Sample_Clears_G2_DivisionByZero_Diagnostics()
    {
        var source = File.ReadAllText(Path.Combine(SamplesRoot, "inventory-item.precept"));
        var compilation = Compiler.Compile(source);

        compilation.Diagnostics.Should().NotContain(d =>
            d.Code == nameof(DiagnosticCode.DivisionByZero)
            && (d.Span.StartLine == 214 || d.Span.StartLine == 220 || d.Span.StartLine == 225));
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Slice 23 — Static qualifier routing: proof-engine PRE0114 suppression
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void StaticUnitInterpolated_Rule_DoesNotEmit_PRE0114()
    {
        // '{Min} kg' is InterpolatedTypedConstant with StaticQualifier = StaticUnitQualifier(kg).
        // ProofEngine should resolve the unit qualifier from StaticQualifier, not from slots,
        // so it discharges the qualifier-compatibility obligation without PRE0114.
        var compilation = Compiler.Compile("""
            precept Widget
            field Min as decimal default '0'
            field Qty as quantity in 'kg' default '0 kg' writable
            rule Qty > '{Min} kg' because "positive"
            """);

        compilation.HasErrors.Should().BeFalse();
        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UnprovedQualifierCompatibility));
    }

    [Fact]
    public void StaticCurrencyInterpolated_Rule_DoesNotEmit_PRE0114()
    {
        // '{n} USD' has StaticQualifier = StaticCurrencyQualifier(USD).
        // ProofEngine should resolve the currency qualifier from StaticQualifier and discharge the proof.
        var compilation = Compiler.Compile("""
            precept Widget
            field MinAmount as decimal default '0'
            field Balance as money in 'USD' default '0.00 USD' writable
            rule Balance > '{MinAmount} USD' because "positive"
            """);

        compilation.HasErrors.Should().BeFalse();
        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UnprovedQualifierCompatibility));
    }

    [Fact]
    public void SymbolicMoneyRoundFunctionCall_Rule_DoesNotEmit_PRE0114()
    {
        var compilation = Compiler.Compile("""
            precept Widget
            field CatalogCurrency as currency default 'USD'
            field Limit as money in '{CatalogCurrency}' default '10.00 {CatalogCurrency}'
            field Balance as money in '{CatalogCurrency}' default '5.12 {CatalogCurrency}'
            rule Limit >= round(Balance, 2) because "rounded balance keeps the symbolic currency"
            """);

        compilation.HasErrors.Should().BeFalse();
        compilation.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.UnprovedQualifierCompatibility));
    }
}

