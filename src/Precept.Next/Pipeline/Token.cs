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
/// <param name="Kind">The token classification.</param>
/// <param name="Text">
/// The token's semantic text content. For keywords and operators, the literal text (e.g. <c>"from"</c>, <c>"=="</c>).
/// For identifiers, the identifier name. For numeric literals, the digit sequence as written in source.
/// For quoted literals (<see cref="TokenKind.StringLiteral"/>, <see cref="TokenKind.StringStart"/>,
/// <see cref="TokenKind.StringMiddle"/>, <see cref="TokenKind.StringEnd"/>,
/// <see cref="TokenKind.TypedConstant"/>, <see cref="TokenKind.TypedConstantStart"/>,
/// <see cref="TokenKind.TypedConstantMiddle"/>, <see cref="TokenKind.TypedConstantEnd"/>),
/// delimiters are stripped and escape sequences are resolved. An empty segment produces <see cref="string.Empty"/>.
/// For <see cref="TokenKind.Comment"/>, the full text including the leading <c>#</c>.
/// For <see cref="TokenKind.NewLine"/> and <see cref="TokenKind.EndOfSource"/>, an empty string.
/// </param>
/// <param name="Line">1-based line number of the token's first character.</param>
/// <param name="Column">1-based column number of the token's first character.</param>
/// <param name="Offset">0-based character offset from the start of the source string.</param>
/// <param name="Length">Number of characters in the source text spanned by this token (including delimiters for quoted literals).</param>
public readonly record struct Token(
    TokenKind   Kind,
    string      Text,
    int         Line,
    int         Column,
    int         Offset,
    int         Length
);
