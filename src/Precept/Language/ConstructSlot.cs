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
    ComputeExpression =  6, // "-> expression" computed value
    GuardClause       =  7, // "when expression"
    ActionChain       =  8, // "-> action -> action" chain
    Outcome           =  9, // "-> transition State | -> no transition | -> reject 'reason'"
    StateTarget       = 10, // state name or quantifier (any)
    EventTarget       = 11, // event name (or "initial" marker)
    EnsureClause      = 12, // "ensure expression because message"
    BecauseClause     = 13, // "because message"
    AccessModeKeyword = 14, // readonly | editable (B4 access mode adjectives)
    FieldTarget       = 15, // field name or "all"
    RuleExpression    = 16, // the rule's boolean expression (e.g. amount > 0)
    InitialMarker     = 17, // optional "initial" keyword on event declarations
}

/// <summary>
/// A single typed slot in a construct declaration shape.
/// </summary>
public sealed record ConstructSlot(
    ConstructSlotKind Kind,
    bool              IsRequired = true,
    string?           Description = null);
