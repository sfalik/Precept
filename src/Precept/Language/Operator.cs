namespace Precept.Language;

/// <summary>Unary or binary.</summary>
public enum Arity { Unary, Binary }

/// <summary>
/// Broad operator family — used by the grammar generator, LS semantic tokens,
/// and MCP vocabulary to assign different scopes to operator groups.
/// </summary>
public enum OperatorFamily
{
    Arithmetic,
    Comparison,
    Logical,
    Membership,
}

/// <summary>Binding direction for the Pratt parser.</summary>
public enum Associativity { Left, Right, NonAssociative }

/// <summary>
/// Metadata for a single operator. Token is the lexer token that produces this
/// operator; for <see cref="OperatorKind.Negate"/> this is the same
/// <see cref="TokenKind.Minus"/> token used by <see cref="OperatorKind.Minus"/>.
/// </summary>
public record OperatorMeta(
    OperatorKind Kind,
    TokenMeta Token,
    string Description,
    Arity Arity,
    Associativity Associativity,
    int Precedence,
    OperatorFamily Family,
    bool IsKeywordOperator = false,
    string? HoverDescription = null,
    string? UsageExample = null);
