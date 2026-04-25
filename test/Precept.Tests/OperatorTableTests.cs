using FluentAssertions;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests;

public class OperatorTableTests
{
    // ════════════════════════════════════════════════════════════════════════════
    //  ResolveBinary — same-lane arithmetic
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Resolve_IntegerPlusInteger_ReturnsInteger()
    {
        var result = OperatorTable.ResolveBinary(BinaryOp.Plus, new IntegerType(), new IntegerType());
        result.Should().BeOfType<IntegerType>();
    }

    [Fact]
    public void Resolve_DecimalPlusDecimal_ReturnsDecimal()
    {
        var result = OperatorTable.ResolveBinary(BinaryOp.Plus, new DecimalType(), new DecimalType());
        result.Should().BeOfType<DecimalType>();
    }

    [Fact]
    public void Resolve_NumberPlusNumber_ReturnsNumber()
    {
        var result = OperatorTable.ResolveBinary(BinaryOp.Plus, new NumberType(), new NumberType());
        result.Should().BeOfType<NumberType>();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  ResolveBinary — widening (commutative)
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Resolve_IntegerPlusDecimal_ReturnsDecimal()
    {
        var result = OperatorTable.ResolveBinary(BinaryOp.Plus, new IntegerType(), new DecimalType());
        result.Should().BeOfType<DecimalType>();
    }

    [Fact]
    public void Resolve_DecimalPlusInteger_ReturnsDecimal()
    {
        // Widening is commutative: order must not matter.
        var result = OperatorTable.ResolveBinary(BinaryOp.Plus, new DecimalType(), new IntegerType());
        result.Should().BeOfType<DecimalType>();
    }

    [Fact]
    public void Resolve_IntegerPlusNumber_ReturnsNumber()
    {
        var result = OperatorTable.ResolveBinary(BinaryOp.Plus, new IntegerType(), new NumberType());
        result.Should().BeOfType<NumberType>();
    }

    [Fact]
    public void Resolve_NumberPlusInteger_ReturnsNumber()
    {
        // Commutative widening.
        var result = OperatorTable.ResolveBinary(BinaryOp.Plus, new NumberType(), new IntegerType());
        result.Should().BeOfType<NumberType>();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  ResolveBinary — cross-lane errors
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Resolve_DecimalPlusNumber_ReturnsNull()
    {
        // decimal + number is a type error — explicit bridge (approximate()) required.
        var result = OperatorTable.ResolveBinary(BinaryOp.Plus, new DecimalType(), new NumberType());
        result.Should().BeNull();
    }

    [Fact]
    public void Resolve_NumberPlusDecimal_ReturnsNull()
    {
        // Commutative check: order must not matter for cross-lane rejection.
        var result = OperatorTable.ResolveBinary(BinaryOp.Plus, new NumberType(), new DecimalType());
        result.Should().BeNull();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  ResolveBinary — string concatenation
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Resolve_StringPlusString_ReturnsString()
    {
        var result = OperatorTable.ResolveBinary(BinaryOp.Plus, new StringType(), new StringType());
        result.Should().BeOfType<StringType>();
    }

    [Fact]
    public void Resolve_StringMinusString_ReturnsNull()
    {
        // String concatenation is + only; minus on strings is not defined.
        var result = OperatorTable.ResolveBinary(BinaryOp.Minus, new StringType(), new StringType());
        result.Should().BeNull();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  ResolveBinary — all arithmetic operators for integer pair
    // ════════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(BinaryOp.Plus)]
    [InlineData(BinaryOp.Minus)]
    [InlineData(BinaryOp.Star)]
    [InlineData(BinaryOp.Slash)]
    [InlineData(BinaryOp.Percent)]
    public void Resolve_AllArithmeticOps_IntegerPair_ReturnsInteger(BinaryOp op)
    {
        var result = OperatorTable.ResolveBinary(op, new IntegerType(), new IntegerType());
        result.Should().BeOfType<IntegerType>();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  ResolveBinary — ErrorType propagation
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Resolve_LeftErrorType_PropagatesErrorType()
    {
        var result = OperatorTable.ResolveBinary(BinaryOp.Plus, new ErrorType(), new IntegerType());
        result.Should().BeOfType<ErrorType>();
    }

    [Fact]
    public void Resolve_RightErrorType_PropagatesErrorType()
    {
        var result = OperatorTable.ResolveBinary(BinaryOp.Plus, new IntegerType(), new ErrorType());
        result.Should().BeOfType<ErrorType>();
    }

    [Fact]
    public void Resolve_BothErrorType_PropagatesErrorType()
    {
        // Both sides ErrorType → still ErrorType (no null, no cascade).
        var result = OperatorTable.ResolveBinary(BinaryOp.Plus, new ErrorType(), new ErrorType());
        result.Should().BeOfType<ErrorType>();
    }

    [Fact]
    public void Resolve_ErrorType_DoesNotReturnNull()
    {
        // ErrorType propagation must produce ErrorType, never null, to prevent false TypeMismatch cascades.
        var resultLeft  = OperatorTable.ResolveBinary(BinaryOp.Plus, new ErrorType(), new BooleanType());
        var resultRight = OperatorTable.ResolveBinary(BinaryOp.Plus, new BooleanType(), new ErrorType());
        resultLeft.Should().NotBeNull();
        resultRight.Should().NotBeNull();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  ResolveBinary — non-numeric types
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Resolve_BooleanPlusBoolean_ReturnsNull()
    {
        // Booleans are not numeric and not strings; + is not valid.
        var result = OperatorTable.ResolveBinary(BinaryOp.Plus, new BooleanType(), new BooleanType());
        result.Should().BeNull();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  ResolveBinary — comparison and logical operators (deferred to Slice 6)
    // ════════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(BinaryOp.Equal)]
    [InlineData(BinaryOp.NotEqual)]
    [InlineData(BinaryOp.Less)]
    [InlineData(BinaryOp.Greater)]
    [InlineData(BinaryOp.LessOrEqual)]
    [InlineData(BinaryOp.GreaterOrEqual)]
    public void Resolve_ComparisonOp_ReturnsNull(BinaryOp op)
    {
        // Comparison operators are deferred to Slice 6; all return null.
        var result = OperatorTable.ResolveBinary(op, new IntegerType(), new IntegerType());
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(BinaryOp.And)]
    [InlineData(BinaryOp.Or)]
    public void Resolve_LogicalOp_ReturnsNull(BinaryOp op)
    {
        // Logical operators deferred to Slice 6; must return null, not throw.
        var result = OperatorTable.ResolveBinary(op, new BooleanType(), new BooleanType());
        result.Should().BeNull();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  CommonNumericType — same type identity
    // ════════════════════════════════════════════════════════════════════════════

    [Theory]
    [MemberData(nameof(NumericTypeSingletons))]
    public void CommonNumericType_SameType_ReturnsSameType(ResolvedType type)
    {
        var result = OperatorTable.CommonNumericType(type, type);
        result.Should().BeOfType(type.GetType());
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  CommonNumericType — widening (commutative)
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void CommonNumericType_IntegerDecimal_ReturnsDecimal()
    {
        var result = OperatorTable.CommonNumericType(new IntegerType(), new DecimalType());
        result.Should().BeOfType<DecimalType>();
    }

    [Fact]
    public void CommonNumericType_DecimalInteger_ReturnsDecimal()
    {
        // Commutative: same result regardless of operand order.
        var result = OperatorTable.CommonNumericType(new DecimalType(), new IntegerType());
        result.Should().BeOfType<DecimalType>();
    }

    [Fact]
    public void CommonNumericType_IntegerNumber_ReturnsNumber()
    {
        var result = OperatorTable.CommonNumericType(new IntegerType(), new NumberType());
        result.Should().BeOfType<NumberType>();
    }

    [Fact]
    public void CommonNumericType_NumberInteger_ReturnsNumber()
    {
        // Commutative.
        var result = OperatorTable.CommonNumericType(new NumberType(), new IntegerType());
        result.Should().BeOfType<NumberType>();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  CommonNumericType — incompatible lane
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void CommonNumericType_DecimalNumber_ReturnsNull()
    {
        // decimal + number has no common type — explicit bridge required.
        var result = OperatorTable.CommonNumericType(new DecimalType(), new NumberType());
        result.Should().BeNull();
    }

    [Fact]
    public void CommonNumericType_NumberDecimal_ReturnsNull()
    {
        // Commutative — order must not matter for incompatibility.
        var result = OperatorTable.CommonNumericType(new NumberType(), new DecimalType());
        result.Should().BeNull();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  CommonNumericType — ErrorType propagation
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void CommonNumericType_LeftErrorType_ReturnsErrorType()
    {
        var result = OperatorTable.CommonNumericType(new ErrorType(), new IntegerType());
        result.Should().BeOfType<ErrorType>();
    }

    [Fact]
    public void CommonNumericType_RightErrorType_ReturnsErrorType()
    {
        var result = OperatorTable.CommonNumericType(new IntegerType(), new ErrorType());
        result.Should().BeOfType<ErrorType>();
    }

    [Fact]
    public void CommonNumericType_BothErrorType_ReturnsErrorType()
    {
        var result = OperatorTable.CommonNumericType(new ErrorType(), new ErrorType());
        result.Should().BeOfType<ErrorType>();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  TheoryData helpers
    // ════════════════════════════════════════════════════════════════════════════

    public static TheoryData<ResolvedType> NumericTypeSingletons => new()
    {
        new IntegerType(),
        new DecimalType(),
        new NumberType()
    };
}
