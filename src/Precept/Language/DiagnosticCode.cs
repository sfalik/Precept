namespace Precept.Language;

public enum DiagnosticCode
{
    // ── Lex ──────────────────────────────────────────────
    InputTooLarge                      =   1,
    UnterminatedStringLiteral          =   2,
    UnterminatedTypedConstant          =   3,
    UnterminatedInterpolation          =   4,
    InvalidCharacter                   =   5,
    UnrecognizedStringEscape           =   6,
    UnrecognizedTypedConstantEscape    =   7,
    UnescapedBraceInLiteral            =   8,

    // ── Parse ────────────────────────────────────────────
    ExpectedToken                      =   9,
    NonAssociativeComparison           =  10,
    UnexpectedKeyword                  =  11, // reserved — not currently emitted by the parser
    InvalidCallTarget                  =  12, // reserved — not currently emitted by the parser
    OmitDoesNotSupportGuard            =  13,
    EventHandlerDoesNotSupportGuard    =  14,
    PreEventGuardNotAllowed            =  15,
    ExpectedOutcome                    =  16,

    // ── Type ─────────────────────────────────────────────
    UndeclaredField                    =  17,
    TypeMismatch                       =  18,
    NullInNonNullableContext           =  19,
    InvalidMemberAccess                =  20,
    FunctionArityMismatch              =  21,
    FunctionArgConstraintViolation     =  22,
    MutuallyExclusiveQualifiers        =  23,
    DuplicateFieldName                 =  24,
    DuplicateStateName                 =  25,
    DuplicateEventName                 =  26,
    DuplicateArgName                   =  27,
    UndeclaredState                    =  28,
    UndeclaredEvent                    =  29,
    UndeclaredFunction                 =  30,
    MultipleInitialStates              =  31,
    NoInitialState                     =  32,
    InvalidModifierForType             =  33,
    InvalidModifierBounds              =  34,
    InvalidModifierValue               =  35,
    DuplicateModifier                  =  36,
    RedundantModifier                  =  37,
    ComputedFieldNotWritable           =  38,
    ComputedFieldWithDefault           =  39,
    CircularComputedField              =  40,
    WritableOnEventArg                 =  41,
    ConflictingAccessModes             =  42,
    RedundantAccessMode                =  43,
    ListLiteralOutsideDefault          =  44,
    DuplicateChoiceValue               =  45,
    EmptyChoice                        =  46,
    CollectionOperationOnScalar        =  47,
    ScalarOperationOnCollection        =  48,
    IsSetOnNonOptional                 =  49,
    EventArgOutOfScope                 =  50,
    InvalidInterpolationCoercion       =  51,
    UnresolvedTypedConstant            =  52,
    InvalidTypedConstantContent        =  53,
    DefaultForwardReference            =  54,

    // ── Type (temporal) ──────────────────────────────────
    InvalidDateValue                   =  55,
    InvalidDateFormat                  =  56,
    InvalidTimeValue                   =  57,
    InvalidInstantFormat               =  58,
    InvalidTimezoneId                  =  59,
    UnqualifiedPeriodArithmetic        =  60,
    MissingTemporalUnit                =  61,
    FractionalUnitValue                =  62,

    // ── Type (collection safety) ─────────────────────────
    UnguardedCollectionAccess          =  63,
    UnguardedCollectionMutation        =  64,
    NonOrderableCollectionExtreme      =  65,
    CaseInsensitiveStringOnNonCollection = 66,

    // ── Type (business-domain) ───────────────────────────
    MaxPlacesExceeded                  =  67,
    QualifierMismatch                  =  68,
    DimensionCategoryMismatch          =  69,
    CrossCurrencyArithmetic            =  70,
    CrossDimensionArithmetic           =  71,
    DenominatorUnitMismatch            =  72,
    DurationDenominatorMismatch        =  73,
    CompoundPeriodDenominator          =  74,
    InvalidUnitString                  =  75,
    InvalidCurrencyCode                =  76,
    InvalidDimensionString             =  77,

    // ── Runtime / value safety ────────────────────────────
    NumericOverflow                    =  78,
    OutOfRange                         =  79,

    // ── Graph ────────────────────────────────────────────
    UnreachableState                   =  80,
    UnhandledEvent                     =  81,

    // ── Proof ────────────────────────────────────────────
    UnsatisfiableGuard                 =  82,
    DivisionByZero                     =  83,
    SqrtOfNegative                     =  84,

    // ── Type (choice) ────────────────────────────────────
    NonChoiceAssignedToChoice          =  85,
    ChoiceLiteralNotInSet              =  86,
    ChoiceArgOutsideFieldSet           =  87,
    ChoiceElementTypeMismatch          =  88,
    ChoiceRankConflict                 =  89,
    ChoiceMissingElementType           =  90,
}
