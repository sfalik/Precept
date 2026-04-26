namespace Precept.Language;

public sealed record FaultMeta(
    string        Code,
    string        MessageTemplate,
    FaultSeverity Severity     = FaultSeverity.Fatal,
    string?       RecoveryHint = null
);

public static class Faults
{
    public static FaultMeta GetMeta(FaultCode code) => code switch
    {
        FaultCode.DivisionByZero                 => new(nameof(FaultCode.DivisionByZero),                 "Divisor evaluated to zero",
            RecoveryHint: "Guard the transition with 'when Divisor != 0', or apply the 'nonzero' or 'positive' modifier to the divisor field"),
        FaultCode.SqrtOfNegative                 => new(nameof(FaultCode.SqrtOfNegative),                 "sqrt() operand evaluated to a negative number",
            RecoveryHint: "Guard the transition with 'when Value >= 0', or apply the 'nonnegative' modifier to the field"),
        FaultCode.TypeMismatch                   => new(nameof(FaultCode.TypeMismatch),                   "Operator applied to incompatible type: {0}",
            RecoveryHint: "Use precept_compile to catch type mismatches at design time before the precept is deployed"),
        FaultCode.UndeclaredField                => new(nameof(FaultCode.UndeclaredField),                "Referenced field does not exist: '{0}'",
            RecoveryHint: "Verify all field names in expressions match declared field names exactly — Precept identifiers are case-sensitive"),
        FaultCode.UnexpectedNull                 => new(nameof(FaultCode.UnexpectedNull),                 "Null value used in non-nullable context",
            RecoveryHint: "Add the 'optional' modifier to allow null values, or guard with 'when Field != null' before use"),
        FaultCode.InvalidMemberAccess            => new(nameof(FaultCode.InvalidMemberAccess),            "Member accessor '{0}' not supported on this type",
            RecoveryHint: "Ensure the member accessor is valid for the field's declared type; the type checker flags invalid accessors at design time"),
        FaultCode.FunctionArityMismatch          => new(nameof(FaultCode.FunctionArityMismatch),          "Function '{0}' called with wrong number of arguments",
            RecoveryHint: "Match the number of arguments to the function's signature; use precept_compile to verify at design time"),
        FaultCode.FunctionArgConstraintViolation => new(nameof(FaultCode.FunctionArgConstraintViolation), "Function argument violates constraint: {0}",
            RecoveryHint: "Guard with a condition that ensures argument values satisfy the declared constraints before calling the function"),
        FaultCode.CollectionEmptyOnAccess        => new(nameof(FaultCode.CollectionEmptyOnAccess),        "Collection was empty when read accessor was called on '{0}'",
            RecoveryHint: "Guard the accessor with 'when CollectionField.count > 0' in the transition or event condition"),
        FaultCode.CollectionEmptyOnMutation      => new(nameof(FaultCode.CollectionEmptyOnMutation),      "Collection was empty when mutation operation was called on '{0}'",
            RecoveryHint: "Guard the mutation action with 'when CollectionField.count > 0' in the transition or event condition"),
        FaultCode.QualifierMismatch              => new(nameof(FaultCode.QualifierMismatch),              "Typed constant qualifier '{0}' does not match the field's declared qualifier '{1}'",
            RecoveryHint: "Ensure typed constant values match the qualifier declared on the target field — e.g., use 'USD' for a 'money in USD' field"),
        FaultCode.NumericOverflow                => new(nameof(FaultCode.NumericOverflow),                "Numeric computation exceeded representable range in '{0}'",
            RecoveryHint: "Reduce the magnitude of operands, or widen the field type to number for larger range"),
        FaultCode.OutOfRange                     => new(nameof(FaultCode.OutOfRange),                     "Value {0} is outside the declared bounds [{1}, {2}]",
            RecoveryHint: "Ensure assigned values satisfy the field's min/max constraints, or widen the bounds in the field declaration"),
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, null),
    };

    public static Fault Create(FaultCode code, params object?[] args)
    {
        var meta = GetMeta(code);
        var message = args is { Length: > 0 }
            ? string.Format(meta.MessageTemplate, args)
            : meta.MessageTemplate;
        return new(code, meta.Code, message);
    }

    public static IReadOnlyList<FaultMeta> All { get; } =
        Enum.GetValues<FaultCode>().Select(GetMeta).ToList();
}
