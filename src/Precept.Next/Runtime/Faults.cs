namespace Precept.Runtime;

public sealed record FaultMeta(string MessageTemplate);

public static class Faults
{
    public static FaultMeta GetMeta(FaultCode code) => code switch
    {
        FaultCode.DivisionByZero                 => new("Divisor evaluated to zero"),
        FaultCode.SqrtOfNegative                 => new("sqrt() operand evaluated to a negative number"),
        FaultCode.TypeMismatch                   => new("Operator applied to incompatible type"),
        FaultCode.UndeclaredField                => new("Referenced field does not exist"),
        FaultCode.UnexpectedNull                 => new("Null value used in non-nullable context"),
        FaultCode.InvalidMemberAccess            => new("Member accessor not supported on this type"),
        FaultCode.FunctionArityMismatch          => new("Function called with wrong number of arguments"),
        FaultCode.FunctionArgConstraintViolation => new("Function argument violates constraint"),
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, null),
    };

    public static Fault Create(FaultCode code, params object?[] args)
    {
        var meta = GetMeta(code);
        return new(code, code.ToString(), string.Format(meta.MessageTemplate, args));
    }

    public static IReadOnlyList<FaultMeta> All { get; } =
        Enum.GetValues<FaultCode>().Select(GetMeta).ToList();
}
