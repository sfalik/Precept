namespace Precept.Language;

/// <summary>
/// Typed slot positions within a construct declaration.
/// Used by grammar generation and context-aware completions.
/// </summary>
public enum ConstructSlotKind
{
    IdentifierList,        // one or more user-defined names (e.g. field names, event names)
    TypeExpression,        // "as TypeKeyword Qualifiers" type annotation
    ModifierList,          // field modifiers (nonnegative, positive, notempty, etc.)
    StateEntryList,        // comma-separated (name modifier*) pairs for state declarations
    ArgumentList,          // event parameter list "(name as type, ...)"
    ComputeExpression,     // "-> expression" computed value
    GuardClause,           // "when expression"
    ActionChain,           // "-> action -> action" chain
    Outcome,               // "-> transition State | -> no transition | -> reject 'reason'"
    StateTarget,           // state name or quantifier (any)
    EventTarget,           // event name (or "initial" marker)
    EnsureClause,          // "ensure expression because message"
    BecauseClause,         // "because message"
    AccessModeKeyword,     // readonly | editable (B4 access mode adjectives)
    FieldTarget,           // field name or "all"
    RuleExpression,        // the rule's boolean expression (e.g. amount > 0)
}

/// <summary>
/// A single typed slot in a construct declaration shape.
/// </summary>
public sealed record ConstructSlot(
    ConstructSlotKind Kind,
    bool              IsRequired = true,
    string?           Description = null);
