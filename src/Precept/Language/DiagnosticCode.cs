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
    /// <summary>
    /// A keyword token appears in an expression/value slot where an operand is expected.
    /// Emitted by the parser when a reserved keyword is encountered as an atom in expression parsing.
    /// </summary>
    UnexpectedKeyword                  =  11,
    /// <summary>
    /// A well-formed expression is used as a call target but is not invocable.
    /// Emitted by the parser when a non-callable expression (e.g., a literal or grouped expression)
    /// is followed by <c>(</c> in infix position.
    /// </summary>
    InvalidCallTarget                  =  12,
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
    /// <summary>The string in a <c>period of '...'</c> qualifier is not 'date' or 'time'.</summary>
    InvalidTemporalDimensionString     = 117,
    /// <summary>The string in a <c>period in '...'</c> qualifier is not a recognized temporal unit name.</summary>
    InvalidTemporalUnitString          = 118,

    // ── Type (collection safety) ─────────────────────────
    UnguardedCollectionAccess          =  63,
    UnguardedCollectionMutation        =  64,
    NonOrderableCollectionExtreme      =  65,
    /// <summary>
    /// Ordinal 66 was originally <c>CaseInsensitiveStringOnNonCollection</c> (reserved for
    /// scalar ~string, never emitted). When scalar ~string ships this ordinal is reassigned to
    /// <c>CaseInsensitiveFieldRequiresTildeEquals</c>. Update any source references accordingly.
    /// </summary>
    CaseInsensitiveFieldRequiresTildeEquals = 66,

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
    /// <summary>
    /// A reachable non-terminal state has no structural path to any terminal state.
    /// The entity can enter this state but can never reach completion from it.
    /// </summary>
    DeadEndState                       = 108,
    /// <summary>
    /// A terminal state has outgoing transitions to other states, violating the
    /// terminal modifier contract that no further transitions are allowed.
    /// </summary>
    TerminalStateHasOutgoingEdges      = 109,
    /// <summary>
    /// An irreversible state has a transition returning to a BFS ancestor,
    /// violating the irreversible modifier contract that prevents back-edges.
    /// </summary>
    IrreversibleStateHasBackEdge       = 110,
    /// <summary>
    /// A required state does not dominate any terminal state, meaning there exist
    /// complete execution paths that bypass the required state entirely.
    /// </summary>
    RequiredStateDoesNotDominateTerminal = 111,

    // ── Proof (new codes) ───────────────────────────────────
    /// <summary>A field referenced in a proof obligation does not declare the required modifier.</summary>
    UnprovedModifierRequirement            = 112,
    /// <summary>An operand requires a specific temporal dimension but the resolved dimension does not match.</summary>
    UnprovedDimensionRequirement           = 113,
    /// <summary>Two operands require matching qualifiers on an axis but their qualifier values differ or are unresolved.</summary>
    UnprovedQualifierCompatibility         = 114,
    /// <summary>An initial state's constraints cannot be satisfied with default field values.</summary>
    UnsatisfiableInitialState              = 115,
    /// <summary>An optional field must be present for a proof obligation, but no strategy can guarantee it is set.</summary>
    UnprovedPresenceRequirement            = 116,

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

    // ── Type (lifecycle validation) ──────────────────────
    /// <summary>A typed constant is ambiguous between two candidate types.</summary>
    AmbiguousTypedConstant             =  91,
    /// <summary>An event handler appears in a stateful precept context where it is not valid.</summary>
    EventHandlerInStatefulPrecept      =  92,
    /// <summary>Required fields exist but no initial event is defined to assign them.</summary>
    RequiredFieldsNeedInitialEvent     =  93,
    /// <summary>An initial event does not assign all required fields.</summary>
    InitialEventMissingAssignments     =  94,

    // ── Type (CI enforcement) ─────────────────────────────────────────────
    CaseInsensitiveFieldRequiresTildeNotEquals  =  95,
    CaseInsensitiveValueInCaseSensitiveContains =  96,
    CaseInsensitiveFieldRequiresTildeStartsWith =  97,
    CaseInsensitiveFieldRequiresTildeEndsWith   =  98,

    // ── Type (collection safety — new) ────────────────────────────────────
    KeyPresenceSafety               =  99,
    IndexBoundsGuard                = 100,
    KeyUniquenessGuard              = 101,
    InvalidQuantifierTarget         = 102,
    BindingShadowsField             = 103,
    MissingOrderingKey              = 104,
    CollectionInnerTypeError        = 105,
    QuantifierPredicateNotBoolean   = 106,

    // ── NameBinder ───────────────────────────────────────────────
    /// <summary>Event argument reference not found in scope.</summary>
    UndeclaredArg                   = 107,
}
