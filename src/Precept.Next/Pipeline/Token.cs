namespace Precept.Pipeline;

/// <summary>
/// Metadata for a single <see cref="TokenKind"/> value. Consumed by the MCP
/// vocabulary builder, language server semantic tokens, and completions.
/// </summary>
public sealed record TokenMeta(
    TokenKind                      Kind,
    string?                        Text,        // keyword/operator text (null for synthetic tokens like Identifier, NumberLiteral)
    IReadOnlyList<TokenCategory>   Categories,
    string                         Description
);

/// <summary>
/// A single token produced by the lexer.
/// </summary>
public readonly record struct Token(
    TokenKind   Kind,
    string      Text,
    int         Line,
    int         Column,
    int         Offset,
    int         Length
);
