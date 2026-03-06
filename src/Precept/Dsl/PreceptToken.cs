using System;
using System.Collections.Generic;
using System.Linq;

namespace Precept;

// ── Tier 1: Token Attributes ─────────────────────────────────────────

/// <summary>Semantic category for a <see cref="PreceptToken"/> member.</summary>
public enum TokenCategory
{
    Control,
    Declaration,
    Action,
    Outcome,
    Type,
    Literal,
    Operator,
    Punctuation,
    Structure,
    Value
}

/// <summary>Classifies a <see cref="PreceptToken"/> member into a semantic group.</summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class TokenCategoryAttribute(TokenCategory category) : Attribute
{
    public TokenCategory Category { get; } = category;
}

/// <summary>Human-readable purpose of a <see cref="PreceptToken"/> member.</summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class TokenDescriptionAttribute(string description) : Attribute
{
    public string Description { get; } = description;
}

/// <summary>The source-text representation of a <see cref="PreceptToken"/> member.</summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class TokenSymbolAttribute(string symbol) : Attribute
{
    public string Symbol { get; } = symbol;
}

// ── Token Enum ───────────────────────────────────────────────────────

/// <summary>
/// Every token produced by the Precept tokenizer.
/// Each member carries <see cref="TokenCategoryAttribute"/>,
/// <see cref="TokenDescriptionAttribute"/>, and (for keywords/operators/punctuation)
/// <see cref="TokenSymbolAttribute"/> metadata used by the tokenizer keyword dictionary,
/// language server, and future MCP reflection.
/// </summary>
public enum PreceptToken
{
    // ═══ Keywords: declarations ═══

    [TokenCategory(TokenCategory.Declaration)]
    [TokenDescription("Top-level precept declaration")]
    [TokenSymbol("precept")]
    Precept,

    [TokenCategory(TokenCategory.Declaration)]
    [TokenDescription("Declares a data field")]
    [TokenSymbol("field")]
    Field,

    [TokenCategory(TokenCategory.Declaration)]
    [TokenDescription("Type annotation separator")]
    [TokenSymbol("as")]
    As,

    [TokenCategory(TokenCategory.Declaration)]
    [TokenDescription("Marks a field as nullable")]
    [TokenSymbol("nullable")]
    Nullable,

    [TokenCategory(TokenCategory.Declaration)]
    [TokenDescription("Specifies a default value")]
    [TokenSymbol("default")]
    Default,

    [TokenCategory(TokenCategory.Declaration)]
    [TokenDescription("Global data invariant")]
    [TokenSymbol("invariant")]
    Invariant,

    [TokenCategory(TokenCategory.Declaration)]
    [TokenDescription("Reason sentinel for constraints")]
    [TokenSymbol("because")]
    Because,

    [TokenCategory(TokenCategory.Control)]
    [TokenDescription("Declares a state")]
    [TokenSymbol("state")]
    State,

    [TokenCategory(TokenCategory.Control)]
    [TokenDescription("Marks the initial state")]
    [TokenSymbol("initial")]
    Initial,

    [TokenCategory(TokenCategory.Declaration)]
    [TokenDescription("Declares an event")]
    [TokenSymbol("event")]
    Event,

    [TokenCategory(TokenCategory.Declaration)]
    [TokenDescription("Introduces event arguments")]
    [TokenSymbol("with")]
    With,

    [TokenCategory(TokenCategory.Declaration)]
    [TokenDescription("Movement constraint")]
    [TokenSymbol("assert")]
    Assert,

    [TokenCategory(TokenCategory.Declaration)]
    [TokenDescription("Editable field declaration")]
    [TokenSymbol("edit")]
    Edit,

    // ═══ Keywords: prepositions + modifiers ═══

    [TokenCategory(TokenCategory.Control)]
    [TokenDescription("While residing in a state")]
    [TokenSymbol("in")]
    In,

    [TokenCategory(TokenCategory.Control)]
    [TokenDescription("Crossing into a state")]
    [TokenSymbol("to")]
    To,

    [TokenCategory(TokenCategory.Control)]
    [TokenDescription("Crossing out of a state")]
    [TokenSymbol("from")]
    From,

    [TokenCategory(TokenCategory.Control)]
    [TokenDescription("When an event fires")]
    [TokenSymbol("on")]
    On,

    [TokenCategory(TokenCategory.Control)]
    [TokenDescription("Guard condition for transition rows")]
    [TokenSymbol("when")]
    When,

    [TokenCategory(TokenCategory.Control)]
    [TokenDescription("Wildcard for all declared states")]
    [TokenSymbol("any")]
    Any,

    [TokenCategory(TokenCategory.Control)]
    [TokenDescription("Collection inner-type separator")]
    [TokenSymbol("of")]
    Of,

    // ═══ Keywords: actions ═══

    [TokenCategory(TokenCategory.Action)]
    [TokenDescription("Assigns a value to a field")]
    [TokenSymbol("set")]
    Set,

    [TokenCategory(TokenCategory.Action)]
    [TokenDescription("Adds an element to a set")]
    [TokenSymbol("add")]
    Add,

    [TokenCategory(TokenCategory.Action)]
    [TokenDescription("Removes an element from a set")]
    [TokenSymbol("remove")]
    Remove,

    [TokenCategory(TokenCategory.Action)]
    [TokenDescription("Enqueues an element to a queue")]
    [TokenSymbol("enqueue")]
    Enqueue,

    [TokenCategory(TokenCategory.Action)]
    [TokenDescription("Dequeues an element from a queue")]
    [TokenSymbol("dequeue")]
    Dequeue,

    [TokenCategory(TokenCategory.Action)]
    [TokenDescription("Pushes an element onto a stack")]
    [TokenSymbol("push")]
    Push,

    [TokenCategory(TokenCategory.Action)]
    [TokenDescription("Pops an element from a stack")]
    [TokenSymbol("pop")]
    Pop,

    [TokenCategory(TokenCategory.Action)]
    [TokenDescription("Clears a collection")]
    [TokenSymbol("clear")]
    Clear,

    [TokenCategory(TokenCategory.Action)]
    [TokenDescription("Captures dequeue/pop result into a field")]
    [TokenSymbol("into")]
    Into,

    // ═══ Keywords: outcomes ═══

    [TokenCategory(TokenCategory.Outcome)]
    [TokenDescription("Moves to a new state")]
    [TokenSymbol("transition")]
    Transition,

    [TokenCategory(TokenCategory.Outcome)]
    [TokenDescription("Negation keyword for 'no transition'")]
    [TokenSymbol("no")]
    No,

    [TokenCategory(TokenCategory.Outcome)]
    [TokenDescription("Rejects the event")]
    [TokenSymbol("reject")]
    Reject,

    // ═══ Type keywords ═══

    [TokenCategory(TokenCategory.Type)]
    [TokenDescription("String scalar type")]
    [TokenSymbol("string")]
    StringType,

    [TokenCategory(TokenCategory.Type)]
    [TokenDescription("Number scalar type")]
    [TokenSymbol("number")]
    NumberType,

    [TokenCategory(TokenCategory.Type)]
    [TokenDescription("Boolean scalar type")]
    [TokenSymbol("boolean")]
    BooleanType,

    [TokenCategory(TokenCategory.Type)]
    [TokenDescription("Queue collection type")]
    [TokenSymbol("queue")]
    Queue,

    [TokenCategory(TokenCategory.Type)]
    [TokenDescription("Stack collection type")]
    [TokenSymbol("stack")]
    Stack,

    // ═══ Literal keywords ═══

    [TokenCategory(TokenCategory.Literal)]
    [TokenDescription("Boolean true literal")]
    [TokenSymbol("true")]
    True,

    [TokenCategory(TokenCategory.Literal)]
    [TokenDescription("Boolean false literal")]
    [TokenSymbol("false")]
    False,

    [TokenCategory(TokenCategory.Literal)]
    [TokenDescription("Null literal")]
    [TokenSymbol("null")]
    Null,

    // ═══ Operators ═══

    [TokenCategory(TokenCategory.Operator)]
    [TokenDescription("Equality comparison")]
    [TokenSymbol("==")]
    DoubleEquals,

    [TokenCategory(TokenCategory.Operator)]
    [TokenDescription("Inequality comparison")]
    [TokenSymbol("!=")]
    NotEquals,

    [TokenCategory(TokenCategory.Operator)]
    [TokenDescription("Greater-than-or-equal comparison")]
    [TokenSymbol(">=")]
    GreaterThanOrEqual,

    [TokenCategory(TokenCategory.Operator)]
    [TokenDescription("Less-than-or-equal comparison")]
    [TokenSymbol("<=")]
    LessThanOrEqual,

    [TokenCategory(TokenCategory.Operator)]
    [TokenDescription("Greater-than comparison")]
    [TokenSymbol(">")]
    GreaterThan,

    [TokenCategory(TokenCategory.Operator)]
    [TokenDescription("Less-than comparison")]
    [TokenSymbol("<")]
    LessThan,

    [TokenCategory(TokenCategory.Operator)]
    [TokenDescription("Logical AND")]
    [TokenSymbol("&&")]
    And,

    [TokenCategory(TokenCategory.Operator)]
    [TokenDescription("Logical OR")]
    [TokenSymbol("||")]
    Or,

    [TokenCategory(TokenCategory.Operator)]
    [TokenDescription("Logical NOT")]
    [TokenSymbol("!")]
    Not,

    [TokenCategory(TokenCategory.Operator)]
    [TokenDescription("Assignment operator")]
    [TokenSymbol("=")]
    Assign,

    [TokenCategory(TokenCategory.Operator)]
    [TokenDescription("Membership test")]
    [TokenSymbol("contains")]
    Contains,

    [TokenCategory(TokenCategory.Operator)]
    [TokenDescription("Addition")]
    [TokenSymbol("+")]
    Plus,

    [TokenCategory(TokenCategory.Operator)]
    [TokenDescription("Subtraction")]
    [TokenSymbol("-")]
    Minus,

    [TokenCategory(TokenCategory.Operator)]
    [TokenDescription("Multiplication")]
    [TokenSymbol("*")]
    Star,

    [TokenCategory(TokenCategory.Operator)]
    [TokenDescription("Division")]
    [TokenSymbol("/")]
    Slash,

    // ═══ Punctuation ═══

    [TokenCategory(TokenCategory.Punctuation)]
    [TokenDescription("Action/outcome pipeline separator")]
    [TokenSymbol("->")]
    Arrow,

    [TokenCategory(TokenCategory.Punctuation)]
    [TokenDescription("List separator")]
    [TokenSymbol(",")]
    Comma,

    [TokenCategory(TokenCategory.Punctuation)]
    [TokenDescription("Member access")]
    [TokenSymbol(".")]
    Dot,

    [TokenCategory(TokenCategory.Punctuation)]
    [TokenDescription("Left parenthesis")]
    [TokenSymbol("(")]
    LeftParen,

    [TokenCategory(TokenCategory.Punctuation)]
    [TokenDescription("Right parenthesis")]
    [TokenSymbol(")")]
    RightParen,

    [TokenCategory(TokenCategory.Punctuation)]
    [TokenDescription("Left bracket")]
    [TokenSymbol("[")]
    LeftBracket,

    [TokenCategory(TokenCategory.Punctuation)]
    [TokenDescription("Right bracket")]
    [TokenSymbol("]")]
    RightBracket,

    // ═══ Identifiers + literals ═══

    [TokenCategory(TokenCategory.Value)]
    [TokenDescription("User-defined name")]
    Identifier,

    [TokenCategory(TokenCategory.Value)]
    [TokenDescription("Quoted string value")]
    StringLiteral,

    [TokenCategory(TokenCategory.Value)]
    [TokenDescription("Numeric value")]
    NumberLiteral,

    // ═══ Structure ═══

    [TokenCategory(TokenCategory.Structure)]
    [TokenDescription("Comment line")]
    [TokenSymbol("#")]
    Comment,

    [TokenCategory(TokenCategory.Structure)]
    [TokenDescription("Line terminator")]
    NewLine
}

// ── Reflection helpers ───────────────────────────────────────────────

/// <summary>
/// Cached reflection utilities for reading token attributes.
/// </summary>
public static class PreceptTokenMeta
{
    private static readonly Dictionary<PreceptToken, TokenCategoryAttribute?> _categories = [];
    private static readonly Dictionary<PreceptToken, TokenDescriptionAttribute?> _descriptions = [];
    private static readonly Dictionary<PreceptToken, TokenSymbolAttribute?> _symbols = [];

    static PreceptTokenMeta()
    {
        foreach (var member in Enum.GetValues<PreceptToken>())
        {
            var fi = typeof(PreceptToken).GetField(member.ToString())!;
            _categories[member] = fi.GetCustomAttributes(typeof(TokenCategoryAttribute), false)
                .Cast<TokenCategoryAttribute>().FirstOrDefault();
            _descriptions[member] = fi.GetCustomAttributes(typeof(TokenDescriptionAttribute), false)
                .Cast<TokenDescriptionAttribute>().FirstOrDefault();
            _symbols[member] = fi.GetCustomAttributes(typeof(TokenSymbolAttribute), false)
                .Cast<TokenSymbolAttribute>().FirstOrDefault();
        }
    }

    public static TokenCategory? GetCategory(PreceptToken token)
        => _categories.GetValueOrDefault(token)?.Category;

    public static string? GetDescription(PreceptToken token)
        => _descriptions.GetValueOrDefault(token)?.Description;

    public static string? GetSymbol(PreceptToken token)
        => _symbols.GetValueOrDefault(token)?.Symbol;

    /// <summary>
    /// Returns all tokens that have the specified category.
    /// </summary>
    public static IEnumerable<PreceptToken> GetByCategory(TokenCategory category)
        => _categories.Where(kv => kv.Value?.Category == category).Select(kv => kv.Key);

    /// <summary>
    /// Returns a dictionary mapping lowercase word-token symbol text to token kind.
    /// Built by reflecting <see cref="TokenSymbolAttribute"/> — adding a keyword to the enum
    /// automatically adds it to this dictionary (zero drift).
    /// Includes all tokens with purely alphabetic symbols (keywords + keyword-operators
    /// like <c>contains</c>). Non-alphabetic symbols (operators, punctuation) are excluded.
    /// For the dual-use <c>set</c> keyword (action + type), the first enum member wins
    /// via <see cref="Dictionary{TKey,TValue}.TryAdd"/>.
    /// </summary>
    public static IReadOnlyDictionary<string, PreceptToken> BuildKeywordDictionary()
    {
        var dict = new Dictionary<string, PreceptToken>(StringComparer.Ordinal);
        foreach (var (token, attr) in _symbols)
        {
            if (attr?.Symbol is not { Length: > 0 } symbol) continue;
            // Only alphabetic symbols are word-tokens (not operators like ==, punctuation like ->)
            if (!symbol.All(char.IsLetter)) continue;
            dict.TryAdd(symbol, token);
        }
        return dict;
    }
}
