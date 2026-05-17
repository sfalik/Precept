namespace Precept.Language;

/// <summary>
/// Typed slot positions within a construct declaration.
/// Used by grammar generation and context-aware completions.
/// </summary>
public enum ConstructSlotKind
{
    IdentifierList    =  1, // one or more user-defined names (e.g. field names, event names)
    TypeExpression    =  2, // "as TypeKeyword Qualifiers" type annotation
    ModifierList      =  3, // field modifiers (nonnegative, positive, notempty, etc.)
    StateEntryList    =  4, // comma-separated (name modifier*) pairs for state declarations
    ArgumentList      =  5, // event parameter list "(name as type, ...)"
    ComputeExpression =  6, // "<- expression" computed value
    GuardClause       =  7, // "when expression"
    ActionChain       =  8, // "-> action -> action" chain
    Outcome           =  9, // "-> transition State | -> no transition | -> reject 'reason'"
    StateTarget       = 10, // state name, "any", or comma-delimited list of state names
    EventTarget       = 11, // event name (or "initial" marker)
    EnsureClause      = 12, // "ensure expression"
    BecauseClause     = 13, // "because message"
    AccessModeKeyword = 14, // readonly | editable (B4 access mode adjectives)
    FieldTarget       = 15, // field name or "all"
    RuleExpression    = 16, // the rule's boolean expression (e.g. amount > 0)
    InitialMarker     = 17, // optional "initial" keyword on event declarations
    RejectClause      = 18, // "reject "message"" refusal outcome
    SuccessOutcome    = 19, // success transition target (transition State | no transition)
    EventEntryList    = 20, // comma-separated (name [(args)] [initial])* pairs for event declarations
}

/// <summary>
/// Declares what completion vocabulary a slot offers.
/// Drives CompletionHandler dispatch once SlotPositionResolver ships (Slice 3).
/// </summary>
public enum SlotVocabulary
{
    /// <summary>No completions (identifier slots, because-clause text, initial marker, argument list).</summary>
    None = 0,
    /// <summary>Declared state names from the semantic index.</summary>
    StateNames = 1,
    /// <summary>Declared event names from the semantic index.</summary>
    EventNames = 2,
    /// <summary>Declared field names from the semantic index.</summary>
    FieldNames = 3,
    /// <summary>Action verb keywords from the Actions catalog.</summary>
    ActionVerbs = 4,
    /// <summary>Type keywords from the Types catalog.</summary>
    TypeKeywords = 5,
    /// <summary>Modifier keywords (context-sensitive to construct's ModifierDomain).</summary>
    Modifiers = 6,
    /// <summary>Expression context: field refs, functions, literals, operators.</summary>
    Expression = 7,
    /// <summary>Top-level construct keywords.</summary>
    TopLevel = 8,
    /// <summary>Outcome keywords (transition, no transition).</summary>
    OutcomeKeywords = 9,
    /// <summary>Access mode keywords (readonly, editable).</summary>
    AccessModes = 10,
    /// <summary>State entry names with optional modifiers (state declaration body).</summary>
    StateEntryNames = 11,
    /// <summary>Reject clause: string literal for refusal reason.</summary>
    RejectReason = 12,
}

/// <summary>
/// A single typed slot in a construct declaration shape.
/// </summary>
public sealed record ConstructSlot(
    ConstructSlotKind Kind,
    bool              IsRequired = true,
    string?           Description = null,
    TokenKind[]?      TerminationTokens = null,
    bool              IsList = false,
    bool              IsChainable = false,
    TokenKind?        ItemIntroducerToken = null,
    SlotVocabulary    Vocabulary = SlotVocabulary.None);
