namespace Precept.Language;

/// <summary>
/// Typed slot positions within a construct declaration.
/// Used by grammar generation and context-aware completions.
/// </summary>
public enum ConstructSlotKind
{
    IdentifierList,        // one or more user-defined names (e.g. field names, state names, event names)
    TypeExpression,        // "as TypeKeyword Qualifiers" type annotation
    ModifierList,          // field modifiers (nonnegative, positive, notempty, etc.)
    StateModifierList,     // state modifiers (terminal, initial, required, success, warning, error, irreversible)
    ArgumentList,          // event parameter list "(name as type, ...)"
    ComputeExpression,     // "= expression" default or computed value
    GuardClause,           // "when expression"
    ActionChain,           // "-> action -> action" chain
    Outcome,               // "-> transition State | -> no transition | -> reject 'reason'"
    StateTarget,           // state name or quantifier (any)
    EventTarget,           // event name (or "initial" marker)
    EnsureClause,          // "ensure expression because message"
    BecauseClause,         // "because message"
    AccessModeKeyword,     // write | read | omit
    FieldTarget,           // field name or "all"
}

/// <summary>
/// A single typed slot in a construct declaration shape.
/// </summary>
public sealed record ConstructSlot(
    ConstructSlotKind Kind,
    bool              IsRequired = true,
    string?           Description = null);
