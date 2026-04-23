namespace Precept.Pipeline;

public enum DiagnosticCode
{
    // ── Lex ──────────────────────────────────────────────
    InputTooLarge,
    UnterminatedStringLiteral,
    UnterminatedTypedConstant,
    UnterminatedInterpolation,
    InvalidCharacter,

    // ── Parse ────────────────────────────────────────────
    ExpectedToken,
    UnexpectedKeyword,

    // ── Type ─────────────────────────────────────────────
    UndeclaredField,
    TypeMismatch,
    NullInNonNullableContext,
    InvalidMemberAccess,
    FunctionArityMismatch,
    FunctionArgConstraintViolation,

    // ── Graph ────────────────────────────────────────────
    UnreachableState,
    UnhandledEvent,

    // ── Proof ────────────────────────────────────────────
    UnsatisfiableGuard,
    DivisionByZero,
    SqrtOfNegative,
}
