using Precept.Pipeline;

namespace Precept.Language;

/// <summary>
/// Classification of a <see cref="TokenKind"/> for tooling surfaces (MCP vocabulary,
/// language server semantic tokens, completions). A token may belong to multiple
/// categories (e.g. <see cref="TokenKind.Set"/> is both Action and Type).
/// </summary>
public enum TokenCategory
{
    Declaration     =  1,
    Preposition     =  2,
    Control         =  3,
    Action          =  4,
    Outcome         =  5,
    AccessMode      =  6,
    LogicalOperator =  7,
    Membership      =  8,
    Quantifier      =  9,
    StateModifier   = 10,
    Constraint      = 11,
    Type            = 12,
    Literal         = 13,
    Operator        = 14,
    Punctuation     = 15,
    Identifier      = 16,
    Structural      = 17,
}

/// <summary>
/// Metadata for a single <see cref="TokenKind"/> value. Consumed by the MCP
/// vocabulary builder, language server semantic tokens, and completions.
/// </summary>
public sealed record TokenMeta(
    TokenKind                      Kind,
    string?                        Text,        // keyword/operator text (null for synthetic tokens like Identifier, NumberLiteral)
    IReadOnlyList<TokenCategory>   Categories,
    string                         Description,
    /// <summary>
    /// TextMate grammar scope assigned to this token. Used by the grammar generator
    /// to emit scope rules from catalog metadata rather than hardcoding scopes in tmLanguage.json.
    /// Null for structural/synthetic tokens that carry no scope (EndOfSource, NewLine).
    /// </summary>
    string?                        TextMateScope,
    /// <summary>
    /// LSP semantic token type for this token. Used by the language server semantic token provider.
    /// Null for structural tokens that have no tooling representation (EndOfSource, NewLine).
    /// </summary>
    string?                        SemanticTokenType,
    /// <summary>
    /// Token kinds that may immediately precede this token in a valid program. Null means
    /// unbounded context — the token may appear after any token. Populated for tokens where
    /// context-sensitive completions are valuable. Use this metadata to filter completion
    /// candidates without full parse-state analysis.
    /// </summary>
    TokenKind[]?                   ValidAfter = null,
    bool                           IsAccessModeAdjective = false,
    /// <summary>
    /// True if this keyword token may appear as a member name after <c>.</c> (e.g., <c>min</c>
    /// and <c>max</c> are DSL aggregation keywords but also idiomatic member-accessor names).
    /// Drives <see cref="Pipeline.Parser.KeywordsValidAsMemberName"/>.
    /// </summary>
    bool                           IsValidAsMemberName = false
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
/// <param name="Span">Source location span covering the token's full extent in the original source text.</param>
public readonly record struct Token(
    TokenKind   Kind,
    string      Text,
    SourceSpan  Span);
