namespace Precept.Pipeline;

/// <summary>
/// Static lookup table for binary operator type resolution.
/// Arithmetic lanes: integer, decimal, number — with explicit widening rules.
/// Slice 6 will add comparison, logical, and string-comparison operators.
/// </summary>
public static class OperatorTable
{
    /// <summary>
    /// Returns the result type for a binary operation, or null if the operator
    /// does not apply to the given operand types.
    /// </summary>
    /// <remarks>
    /// Arithmetic rules (+, -, *, /, %):
    ///   integer × integer → integer
    ///   decimal × decimal → decimal
    ///   number  × number  → number
    ///   integer × decimal → decimal  (widening, commutative)
    ///   integer × number  → number   (widening, commutative)
    ///   decimal × number  → null     (type error — explicit bridge required)
    ///
    /// String concatenation:
    ///   string + string → string  (+ only)
    ///
    /// ErrorType propagation:
    ///   Either operand is ErrorType → ErrorType
    ///
    /// All other combinations → null (caller emits TypeMismatch).
    /// </remarks>
    public static ResolvedType? ResolveBinary(BinaryOp op, ResolvedType left, ResolvedType right)
    {
        // ErrorType propagation: suppress cascading errors
        if (left is ErrorType || right is ErrorType)
            return new ErrorType();

        return op switch
        {
            BinaryOp.Plus    => ResolveAddition(left, right),
            BinaryOp.Minus   => ResolveArithmetic(left, right),
            BinaryOp.Star    => ResolveArithmetic(left, right),
            BinaryOp.Slash   => ResolveArithmetic(left, right),
            BinaryOp.Percent => ResolveArithmetic(left, right),
            _                => null   // comparison, logical, string-comparison — Slice 6
        };
    }

    /// <summary>
    /// Returns the common numeric type for widening two numeric operands,
    /// or null if they are incompatible (e.g., decimal + number).
    /// ErrorType on either side propagates as ErrorType.
    /// </summary>
    public static ResolvedType? CommonNumericType(ResolvedType a, ResolvedType b)
    {
        if (a is ErrorType || b is ErrorType) return new ErrorType();
        if (a == b)                           return a;

        // integer + decimal → decimal
        if ((a is IntegerType && b is DecimalType) ||
            (a is DecimalType && b is IntegerType))
            return new DecimalType();

        // integer + number → number
        if ((a is IntegerType && b is NumberType) ||
            (a is NumberType  && b is IntegerType))
            return new NumberType();

        // decimal + number → null (incompatible — requires explicit bridge)
        return null;
    }

    // + handles both arithmetic and string concatenation
    private static ResolvedType? ResolveAddition(ResolvedType left, ResolvedType right)
    {
        if (left is StringType && right is StringType)
            return new StringType(false);

        return ResolveArithmetic(left, right);
    }

    // -, *, /, % — numeric only
    private static ResolvedType? ResolveArithmetic(ResolvedType left, ResolvedType right)
    {
        if (left is not (IntegerType or DecimalType or NumberType)) return null;
        if (right is not (IntegerType or DecimalType or NumberType)) return null;
        return CommonNumericType(left, right);
    }
}
