namespace Precept.Language;

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
    OmitDoesNotSupportGuard,
    PreEventGuardNotAllowed,
    ExpectedOutcome,

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
    WritableOnEventArg,
    ConflictingAccessModes,
    RedundantAccessMode,
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

    // ── Type (temporal) ──────────────────────────────────
    InvalidDateValue,
    InvalidDateFormat,
    InvalidTimeValue,
    InvalidInstantFormat,
    InvalidTimezoneId,
    UnqualifiedPeriodArithmetic,
    MissingTemporalUnit,
    FractionalUnitValue,

    // ── Type (collection safety) ─────────────────────────
    UnguardedCollectionAccess,
    UnguardedCollectionMutation,
    NonOrderableCollectionExtreme,
    CaseInsensitiveStringOnNonCollection,

    // ── Type (business-domain) ───────────────────────────
    MaxPlacesExceeded,
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

    // ── Runtime / value safety ────────────────────────────
    NumericOverflow,
    OutOfRange,

    // ── Graph ────────────────────────────────────────────
    UnreachableState,
    UnhandledEvent,

    // ── Proof ────────────────────────────────────────────
    UnsatisfiableGuard,
    DivisionByZero,
    SqrtOfNegative,
}
