namespace Precept.Language;

[AttributeUsage(AttributeTargets.Field)]
public sealed class StaticallyPreventableAttribute(DiagnosticCode code) : Attribute
{
    public DiagnosticCode Code { get; } = code;
}

public enum FaultCode
{
    [StaticallyPreventable(DiagnosticCode.DivisionByZero)]
    DivisionByZero             =  1,

    [StaticallyPreventable(DiagnosticCode.SqrtOfNegative)]
    SqrtOfNegative             =  2,

    [StaticallyPreventable(DiagnosticCode.TypeMismatch)]
    TypeMismatch               =  3,

    [StaticallyPreventable(DiagnosticCode.UndeclaredField)]
    UndeclaredField            =  4,

    [StaticallyPreventable(DiagnosticCode.NullInNonNullableContext)]
    UnexpectedNull             =  5,

    [StaticallyPreventable(DiagnosticCode.InvalidMemberAccess)]
    InvalidMemberAccess        =  6,

    [StaticallyPreventable(DiagnosticCode.FunctionArityMismatch)]
    FunctionArityMismatch      =  7,

    [StaticallyPreventable(DiagnosticCode.FunctionArgConstraintViolation)]
    FunctionArgConstraintViolation =  8,

    [StaticallyPreventable(DiagnosticCode.UnguardedCollectionAccess)]
    CollectionEmptyOnAccess    =  9,

    [StaticallyPreventable(DiagnosticCode.UnguardedCollectionMutation)]
    CollectionEmptyOnMutation  = 10,

    [StaticallyPreventable(DiagnosticCode.QualifierMismatch)]
    QualifierMismatch          = 11,

    [StaticallyPreventable(DiagnosticCode.NumericOverflow)]
    NumericOverflow            = 12,

    [StaticallyPreventable(DiagnosticCode.OutOfRange)]
    OutOfRange                 = 13,
}
