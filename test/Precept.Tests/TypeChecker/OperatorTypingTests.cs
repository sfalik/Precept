using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.TypeChecker;

public class OperatorTypingTests
{
    [Fact]
    public void Contains_InRuleExpression_CompilesCleanly()
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean("""
            precept Example
            field Tags as set of string
            rule Tags contains "required" because "required tag required"
            """);

        index.Rules.Should().ContainSingle();
        index.Rules.Single().Condition.Should().BeOfType<TypedBinaryOp>();
        ((TypedBinaryOp)index.Rules.Single().Condition).ResolvedOp.Should().Be(OperationKind.CollectionContains);
    }

    [Fact]
    public void Contains_InGuardExpression_CompilesCleanly()
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean("""
            precept Example
            field Tags as set of string
            state Draft initial
            event Remove(Tag as string)
            from Draft on Remove when Tags contains Tag -> no transition
            """);

        index.TransitionRows.Should().ContainSingle();
        index.TransitionRows.Single().Guard.Should().BeOfType<TypedBinaryOp>();
        ((TypedBinaryOp)index.TransitionRows.Single().Guard!).ResolvedOp.Should().Be(OperationKind.CollectionContains);
    }

    [Fact]
    public void Contains_QuantitySetWithSameCountingUnit_CompilesCleanly()
    {
        TypeCheckerTestHelpers.CheckExpectingClean("""
            precept Example
            field QtyEach as quantity in 'each' default '1 each'
            rule [QtyEach] contains QtyEach because "membership check"
            """);
    }

    [Fact]
    public void Contains_QuantitySetWithDifferentCountingUnit_EmitsCrossCountingUnitOperation()
    {
        TypeCheckerTestHelpers.CheckExpectingError("""
            precept Example
            field QtyBox as quantity in 'box' default '1 box'
            field QtyEach as quantity in 'each' default '1 each'
            rule [QtyBox] contains QtyEach because "membership check"
            """, DiagnosticCode.CrossCountingUnitOperation);
    }

    [Fact]
    public void Contains_QuantitySetWithDifferentDimension_EmitsCrossDimensionArithmetic()
    {
        TypeCheckerTestHelpers.CheckExpectingError("""
            precept Example
            field Distance as quantity in 'm' default '1 m'
            field WeightKg as quantity in 'kg' default '1 kg'
            rule [Distance] contains WeightKg because "membership check"
            """, DiagnosticCode.CrossDimensionArithmetic);
    }

    [Fact]
    public void Not_InGuardExpression_CompilesCleanly()
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean("""
            precept Example
            field Urgent as boolean default false
            state Draft initial
            state Done terminal
            event Finish
            from Draft on Finish when not Urgent -> transition Done
            """);

        index.TransitionRows.Single().Guard.Should().BeOfType<TypedUnaryOp>();
        ((TypedUnaryOp)index.TransitionRows.Single().Guard!).ResolvedOp.Should().Be(OperationKind.NotBoolean);
    }

    [Fact]
    public void And_InComputedField_CompilesCleanly()
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean("""
            precept Example
            field A as boolean default false writable
            field B as boolean default false writable
            field Both as boolean <- A and B
            """);

        var expr = index.FieldsByName["Both"].ComputedExpression;
        expr.Should().BeOfType<TypedBinaryOp>();
        var bin = (TypedBinaryOp)expr!;
        bin.ResultType.Should().Be(TypeKind.Boolean);
        bin.ResolvedOp.Should().Be(OperationKind.BooleanAndBoolean);
    }

    [Fact]
    public void Or_InComputedField_CompilesCleanly()
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean("""
            precept Example
            field A as boolean default false writable
            field B as boolean default false writable
            field Either as boolean <- A or B
            """);

        var expr = index.FieldsByName["Either"].ComputedExpression;
        expr.Should().BeOfType<TypedBinaryOp>();
        var bin = (TypedBinaryOp)expr!;
        bin.ResultType.Should().Be(TypeKind.Boolean);
        bin.ResolvedOp.Should().Be(OperationKind.BooleanOrBoolean);
    }

    [Fact]
    public void LookupAccess_ReturnsLookupValueType()
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean("""
            precept Example
            field Prices as lookup of string to number
            field SelectedPrice as number <- Prices for "standard"
            """);

        var expr = index.FieldsByName["SelectedPrice"].ComputedExpression;
        expr.Should().BeOfType<TypedBinaryOp>();
        var bin = (TypedBinaryOp)expr!;
        bin.ResultType.Should().Be(TypeKind.Number);
        bin.ResolvedOp.Should().Be(OperationKind.LookupAccess);
    }

    [Fact]
    public void Arithmetic_BindsTighterThanComparison()
    {
        var index = TypeCheckerTestHelpers.CheckExpectingClean("""
            precept Example
            field X as integer default 0 writable
            field Y as integer default 0 writable
            field Flag as boolean <- X + 1 > Y
            """);

        var expr = index.FieldsByName["Flag"].ComputedExpression;
        expr.Should().BeOfType<TypedBinaryOp>();
        var comparison = (TypedBinaryOp)expr!;
        comparison.Left.Should().BeOfType<TypedBinaryOp>();
        comparison.ResolvedOp.Should().Be(OperationKind.IntegerGreaterThanInteger);
        ((TypedBinaryOp)comparison.Left).ResolvedOp.Should().Be(OperationKind.IntegerPlusInteger);
    }
}
