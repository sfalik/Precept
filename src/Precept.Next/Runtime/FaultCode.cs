using Precept.Pipeline;

namespace Precept.Runtime;

[AttributeUsage(AttributeTargets.Field)]
public sealed class StaticallyPreventableAttribute(DiagnosticCode code) : Attribute
{
    public DiagnosticCode Code { get; } = code;
}

public enum FaultCode
{
    [StaticallyPreventable(DiagnosticCode.DivisionByZero)]
    DivisionByZero,

    [StaticallyPreventable(DiagnosticCode.SqrtOfNegative)]
    SqrtOfNegative,

    [StaticallyPreventable(DiagnosticCode.TypeMismatch)]
    TypeMismatch,

    [StaticallyPreventable(DiagnosticCode.UndeclaredField)]
    UndeclaredField,

    [StaticallyPreventable(DiagnosticCode.NullInNonNullableContext)]
    UnexpectedNull,

    [StaticallyPreventable(DiagnosticCode.InvalidMemberAccess)]
    InvalidMemberAccess,

    [StaticallyPreventable(DiagnosticCode.FunctionArityMismatch)]
    FunctionArityMismatch,

    [StaticallyPreventable(DiagnosticCode.FunctionArgConstraintViolation)]
    FunctionArgConstraintViolation,
}
