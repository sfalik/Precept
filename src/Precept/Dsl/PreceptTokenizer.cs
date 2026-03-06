using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using Superpower.Tokenizers;

namespace Precept;

/// <summary>
/// Superpower tokenizer for the Precept DSL.
/// Converts raw source text into a <see cref="TokenList{TKind}"/> of <see cref="PreceptToken"/>.
/// The keyword dictionary is built from <see cref="TokenSymbolAttribute"/> via reflection —
/// adding a keyword to the enum automatically adds it to the tokenizer (zero drift).
/// </summary>
public static class PreceptTokenizerBuilder
{
    // ── Text parsers for complex token patterns ──────────────────────
    // NOTE: These must be declared BEFORE Instance so they are initialized
    // before Build() runs (C# static fields initialize in textual order).

    /// <summary>Comment: # followed by any non-newline characters to end of line.</summary>
    static readonly TextParser<Unit> CommentParser =
        Character.EqualTo('#')
            .IgnoreThen(Character.Matching(c => c != '\n' && c != '\r', "non-newline").Many())
            .Value(Unit.Value);

    /// <summary>Number literal: integer or decimal (e.g. 123, 3.14).</summary>
    static readonly TextParser<TextSpan> DecimalNumber =
        Span.MatchedBy(
            Character.Digit.AtLeastOnce()
                .Then(_ =>
                    Character.EqualTo('.')
                        .IgnoreThen(Character.Digit.AtLeastOnce())
                        .Try()
                        .OptionalOrDefault([])));

    /// <summary>Singleton tokenizer instance, thread-safe after initialization.</summary>
    public static Tokenizer<PreceptToken> Instance { get; } = Build();

    // ── Builder ──────────────────────────────────────────────────────

    static Tokenizer<PreceptToken> Build()
    {
        var keywords = PreceptTokenMeta.BuildKeywordDictionary();

        var builder = new TokenizerBuilder<PreceptToken>();

        // 1. Comments — strip from token stream so the parser never sees them
        builder.Ignore(CommentParser);

        // 2. String literals ("..." with C-style escapes)
        builder.Match(QuotedString.CStyle, PreceptToken.StringLiteral);

        // 3. Number literals (123 or 123.45)
        builder.Match(DecimalNumber, PreceptToken.NumberLiteral);

        // 4. Multi-char operators — must come before single-char variants
        builder.Match(Span.EqualTo("=="), PreceptToken.DoubleEquals);
        builder.Match(Span.EqualTo("!="), PreceptToken.NotEquals);
        builder.Match(Span.EqualTo(">="), PreceptToken.GreaterThanOrEqual);
        builder.Match(Span.EqualTo("<="), PreceptToken.LessThanOrEqual);
        builder.Match(Span.EqualTo("&&"), PreceptToken.And);
        builder.Match(Span.EqualTo("||"), PreceptToken.Or);
        builder.Match(Span.EqualTo("->"), PreceptToken.Arrow);

        // 5. Single-char operators
        builder.Match(Character.EqualTo('>'), PreceptToken.GreaterThan);
        builder.Match(Character.EqualTo('<'), PreceptToken.LessThan);
        builder.Match(Character.EqualTo('='), PreceptToken.Assign);
        builder.Match(Character.EqualTo('!'), PreceptToken.Not);
        builder.Match(Character.EqualTo('+'), PreceptToken.Plus);
        builder.Match(Character.EqualTo('-'), PreceptToken.Minus);
        builder.Match(Character.EqualTo('*'), PreceptToken.Star);
        builder.Match(Character.EqualTo('/'), PreceptToken.Slash);

        // 6. Punctuation
        builder.Match(Character.EqualTo(','), PreceptToken.Comma);
        builder.Match(Character.EqualTo('.'), PreceptToken.Dot);
        builder.Match(Character.EqualTo('('), PreceptToken.LeftParen);
        builder.Match(Character.EqualTo(')'), PreceptToken.RightParen);
        builder.Match(Character.EqualTo('['), PreceptToken.LeftBracket);
        builder.Match(Character.EqualTo(']'), PreceptToken.RightBracket);

        // 7. Keywords — attribute-driven, registered with requireDelimiters
        //    so that "fromState" stays as Identifier, while "from" becomes From.
        //    Keywords are strictly lowercase (design doc).
        foreach (var (symbol, token) in keywords)
            builder.Match(Span.EqualTo(symbol), token, requireDelimiters: true);

        // 8. Identifier catch-all (must come AFTER all keywords)
        builder.Match(Identifier.CStyle, PreceptToken.Identifier, requireDelimiters: true);

        // 9. Skip whitespace (spaces, tabs, newlines, carriage returns)
        builder.Ignore(Span.WhiteSpace);

        return builder.Build();
    }
}
