namespace Precept.Pipeline;

/// <summary>
/// Classification of a <see cref="TokenKind"/> for tooling surfaces (MCP vocabulary,
/// language server semantic tokens, completions). A token may belong to multiple
/// categories (e.g. <see cref="TokenKind.Set"/> is both Action and Type).
/// </summary>
public enum TokenCategory
{
    Declaration,
    Preposition,
    Control,
    Action,
    Outcome,
    AccessMode,
    LogicalOperator,
    Membership,
    Quantifier,
    StateModifier,
    Constraint,
    Type,
    Literal,
    Operator,
    Punctuation,
    Identifier,
    Structural,
}
