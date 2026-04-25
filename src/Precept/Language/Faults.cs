namespace Precept.Language;

public sealed record FaultMeta(
    string Code,
    string MessageTemplate
);

public static class Faults
{
    public static FaultMeta GetMeta(FaultCode code) => code switch
    {
        FaultCode.DivisionByZero                 => new(nameof(FaultCode.DivisionByZero),                 "Divisor evaluated to zero"),
        FaultCode.SqrtOfNegative                 => new(nameof(FaultCode.SqrtOfNegative),                 "sqrt() operand evaluated to a negative number"),
        FaultCode.TypeMismatch                   => new(nameof(FaultCode.TypeMismatch),                   "Operator applied to incompatible type"),
        FaultCode.UndeclaredField                => new(nameof(FaultCode.UndeclaredField),                "Referenced field does not exist"),
        FaultCode.UnexpectedNull                 => new(nameof(FaultCode.UnexpectedNull),                 "Null value used in non-nullable context"),
        FaultCode.InvalidMemberAccess            => new(nameof(FaultCode.InvalidMemberAccess),            "Member accessor not supported on this type"),
        FaultCode.FunctionArityMismatch          => new(nameof(FaultCode.FunctionArityMismatch),          "Function called with wrong number of arguments"),
        FaultCode.FunctionArgConstraintViolation => new(nameof(FaultCode.FunctionArgConstraintViolation), "Function argument violates constraint"),
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, null),
    };

    public static Fault Create(FaultCode code, params object?[] args)
    {
        var meta = GetMeta(code);
        return new(code, meta.Code, string.Format(meta.MessageTemplate, args));
    }

    public static IReadOnlyList<FaultMeta> All { get; } =
        Enum.GetValues<FaultCode>().Select(GetMeta).ToList();
}
