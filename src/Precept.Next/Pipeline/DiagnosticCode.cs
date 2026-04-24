namespace Precept.Pipeline;

public enum DiagnosticCode
{
    // ── Lex ──────────────────────────────────────────────
    InputTooLarge,
    UnterminatedStringLiteral,
    UnterminatedTypedConstant,
    UnterminatedInterpolation,
    InvalidCharacter,
    UnrecognizedStringEscape,
    UnrecognizedTypedConstantEscape,
    UnescapedBraceInLiteral,

    // ── Parse ────────────────────────────────────────────
    ExpectedToken,
    UnexpectedKeyword,
    NonAssociativeComparison,
    InvalidCallTarget,
    MutuallyExclusiveQualifiers,

    // ── Type ─────────────────────────────────────────────
    UndeclaredField,
    TypeMismatch,
    NullInNonNullableContext,
    InvalidMemberAccess,
    FunctionArityMismatch,
    FunctionArgConstraintViolation,
    DuplicateFieldName,
    DuplicateStateName,
    DuplicateEventName,
    DuplicateArgName,
    UndeclaredState,
    UndeclaredEvent,
    UndeclaredFunction,
    MultipleInitialStates,
    NoInitialState,
    InvalidModifierForType,
    InvalidModifierBounds,
    InvalidModifierValue,
    DuplicateModifier,
    RedundantModifier,
    ComputedFieldNotWritable,
    ComputedFieldWithDefault,
    CircularComputedField,
    ConflictingAccessModes,
    ListLiteralOutsideDefault,
    DuplicateChoiceValue,
    EmptyChoice,
    CollectionOperationOnScalar,
    ScalarOperationOnCollection,
    IsSetOnNonOptional,
    EventArgOutOfScope,
    InvalidInterpolationCoercion,
    UnresolvedTypedConstant,
    InvalidTypedConstantContent,
    DefaultForwardReference,

    // ── Type (business-domain) ───────────────────────────
    QualifierMismatch,
    DimensionCategoryMismatch,
    CrossCurrencyArithmetic,
    CrossDimensionArithmetic,
    DenominatorUnitMismatch,
    DurationDenominatorMismatch,
    CompoundPeriodDenominator,
    InvalidUnitString,
    InvalidCurrencyCode,
    InvalidDimensionString,

    // ── Graph ────────────────────────────────────────────
    UnreachableState,
    UnhandledEvent,

    // ── Proof ────────────────────────────────────────────
    UnsatisfiableGuard,
    DivisionByZero,
    SqrtOfNegative,
}
