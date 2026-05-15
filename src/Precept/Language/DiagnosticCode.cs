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
    /// <summary>
    /// The assignment token '=' appears inside an expression, where only comparison '==' is valid.
    /// </summary>
    AssignmentInExpressionContext      = 127,

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
    StateListContainsWildcard          = 128,
    DuplicateStateInList               = 129,
    /// <summary>
    /// Reading a field that is <c>omit</c> in the state-anchored expression context
    /// (transition row guard, in-state ensure, from-state ensure, state action guard/RHS).
    /// </summary>
    OmittedFieldReadInState            = 130,
    /// <summary>
    /// A <c>set</c> action in a transition or state hook targets a field that is
    /// <c>omit</c> in the target state.
    /// Grounded in §2.2 rule #6.
    /// </summary>
    OmittedFieldSetInTargetState       = 131,
    /// <summary>
    /// A transition moves a required field from <c>omit</c> (from-state) to non-omit
    /// (to-state) without a <c>set</c> action assigning it.
    /// Structural dual of <see cref="InitialEventMissingAssignments"/> (D94).
    /// </summary>
    RequiredFieldUnassignedOnEntry     = 132,
    UndeclaredEvent                    =  29,
    UndeclaredFunction                 =  30,
    MultipleInitialStates              =  31,
    NoInitialState                     =  32,
    InvalidModifierForType             =  33,
    InvalidModifierBounds              =  34,
    InvalidModifierValue               =  35,
    /// <summary><c>min</c>/<c>max</c> bounds on qualified types require explicit qualifier context.</summary>
    BoundsRequireQualifier             = 133,
    /// <summary>A bound's qualifier (e.g., <c>'100 EUR'</c>) conflicts with the field's declared qualifier (e.g., <c>in 'USD'</c>).</summary>
    BoundsQualifierMismatch            = 134,
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
    /// <summary>
    /// A reachable non-terminal state has no outgoing transitions and is not marked 'terminal'.
    /// Entities that enter it will be permanently stuck with no way to progress.
    /// </summary>
    StructuralSinkState                = 119,
    /// <summary>Two modifiers that are mutually exclusive are declared on the same field or event argument.</summary>
    ConflictingModifiers               = 120,
    /// <summary>The segment sequence of an interpolated typed constant does not match any valid form for the target type.</summary>
    InvalidInterpolatedTypedConstantForm = 121,
    /// <summary>The target type does not support interpolation (formatted temporal types).</summary>
    InterpolationNotSupportedForType   = 122,
    /// <summary>A hole expression type is not valid for the assigned slot in an interpolated typed constant.</summary>
    InterpolatedTypedConstantHoleTypeMismatch = 123,
    /// <summary>A unit-slot hole carries a dimension that conflicts with the target field's declared dimension.</summary>
    DimensionMismatchInUnitSlot        = 124,

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

    /// <summary>A string field assignment cannot be proven to satisfy the declared minlength/maxlength bounds.</summary>
    LengthBoundViolation               = 135,
    /// <summary>A collection field assignment cannot be proven to satisfy the declared mincount/maxcount bounds.</summary>
    CountBoundViolation                = 136,
    /// <summary>A binary/function operation combines business counting units that share dimension family but not unit identity.</summary>
    CrossCountingUnitOperation         = 137,
    /// <summary>Bounds on count-dimension quantity fields are ambiguous without an explicit counting unit.</summary>
    CountDimensionBoundsAmbiguous      = 138,
    /// <summary>
    /// The <c>of</c> qualifier is only valid when the <c>in</c> slot resolves to a currency code.
    /// When <c>in</c> resolves to a unit or compound price, <c>of</c> is not permitted because
    /// the unit already carries dimension information.
    /// </summary>
    InvalidQualifierCoexistence        = 139,
    /// <summary>
    /// A <c>price in '...'</c> value is not a recognized currency code, UCUM unit, or compound form.
    /// </summary>
    InvalidPriceQualifier              = 140,
    /// <summary>
    /// An assignment to a qualified field cannot prove the source satisfies one required qualifier axis.
    /// Distinct from <see cref="UnprovedQualifierCompatibility"/>, which is proof-stage and operand-pair based.
    /// </summary>
    UnprovedAssignmentQualifierCompatibility = 141,

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
    /// <summary>
    /// Every row for this event has a reject outcome — the event can never succeed anywhere.
    /// If the event is not applicable in any state, remove all rows for it.
    /// </summary>
    AlwaysRejecting                    = 125,
    /// <summary>
    /// This event always rejects from the specified state, but has a success path from another state.
    /// If the event has no meaning in this state, remove the row; no row means 'not applicable here'.
    /// </summary>
    StateAlwaysRejects                 = 126,

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
