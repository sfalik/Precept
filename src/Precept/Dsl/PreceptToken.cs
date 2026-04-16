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
    Grammar,
    Constraint,
    Type,
    Literal,
    Operator,
    Punctuation,
    Structure,
    Value
}

/// <summary>Classifies a <see cref="PreceptToken"/> member into a semantic group.
/// A token may carry multiple categories (e.g. <c>set</c> is both Action and Type).</summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
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

    [TokenCategory(TokenCategory.Grammar)]
    [TokenDescription("Global data rule — a truth that must always hold")]
    [TokenSymbol("rule")]
    Rule,

    [TokenCategory(TokenCategory.Declaration)]
    [TokenDescription("Reason sentinel for constraints")]
    [TokenSymbol("because")]
    Because,

    [TokenCategory(TokenCategory.Declaration)]
    [TokenDescription("Declares a state")]
    [TokenSymbol("state")]
    State,

    [TokenCategory(TokenCategory.Declaration)]
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

    [TokenCategory(TokenCategory.Grammar)]
    [TokenDescription("Temporal enforcement — checked at a specific lifecycle moment")]
    [TokenSymbol("ensure")]
    Ensure,

    [TokenCategory(TokenCategory.Declaration)]
    [TokenDescription("Editable field declaration")]
    [TokenSymbol("edit")]
    Edit,

    // ═══ Keywords: prepositions + modifiers ═══

    [TokenCategory(TokenCategory.Declaration)]
    [TokenDescription("While residing in a state")]
    [TokenSymbol("in")]
    In,

    [TokenCategory(TokenCategory.Declaration)]
    [TokenDescription("Crossing into a state")]
    [TokenSymbol("to")]
    To,

    [TokenCategory(TokenCategory.Declaration)]
    [TokenDescription("Crossing out of a state")]
    [TokenSymbol("from")]
    From,

    [TokenCategory(TokenCategory.Declaration)]
    [TokenDescription("When an event fires")]
    [TokenSymbol("on")]
    On,

    [TokenCategory(TokenCategory.Control)]
    [TokenDescription("Guard condition for transition rows")]
    [TokenSymbol("when")]
    When,

    [TokenCategory(TokenCategory.Control)]
    [TokenDescription("Conditional expression — selects between two values")]
    [TokenSymbol("if")]
    If,

    [TokenCategory(TokenCategory.Control)]
    [TokenDescription("Conditional expression — introduces the true branch")]
    [TokenSymbol("then")]
    Then,

    [TokenCategory(TokenCategory.Control)]
    [TokenDescription("Conditional expression — introduces the false branch")]
    [TokenSymbol("else")]
    Else,

    [TokenCategory(TokenCategory.Declaration)]
    [TokenDescription("Wildcard for all declared states")]
    [TokenSymbol("any")]
    Any,

    [TokenCategory(TokenCategory.Declaration)]
    [TokenDescription("Quantifier for all declared fields")]
    [TokenSymbol("all")]
    All,

    [TokenCategory(TokenCategory.Declaration)]
    [TokenDescription("Collection inner-type separator")]
    [TokenSymbol("of")]
    Of,

    // ═══ Keywords: field-level constraints ═══

    [TokenCategory(TokenCategory.Constraint)]
    [TokenDescription("Constraint: value must be >= 0")]
    [TokenSymbol("nonnegative")]
    Nonnegative,

    [TokenCategory(TokenCategory.Constraint)]
    [TokenDescription("Constraint: value must be > 0")]
    [TokenSymbol("positive")]
    Positive,

    [TokenCategory(TokenCategory.Constraint)]
    [TokenDescription("Constraint: minimum value (number) or minimum count (collection)")]
    [TokenSymbol("min")]
    Min,

    [TokenCategory(TokenCategory.Constraint)]
    [TokenDescription("Constraint: maximum value (number) or maximum count (collection)")]
    [TokenSymbol("max")]
    Max,

    [TokenCategory(TokenCategory.Constraint)]
    [TokenDescription("Constraint: string or collection must not be empty")]
    [TokenSymbol("notempty")]
    Notempty,

    [TokenCategory(TokenCategory.Constraint)]
    [TokenDescription("Constraint: minimum string length")]
    [TokenSymbol("minlength")]
    Minlength,

    [TokenCategory(TokenCategory.Constraint)]
    [TokenDescription("Constraint: maximum string length")]
    [TokenSymbol("maxlength")]
    Maxlength,

    [TokenCategory(TokenCategory.Constraint)]
    [TokenDescription("Constraint: minimum collection element count")]
    [TokenSymbol("mincount")]
    Mincount,

    [TokenCategory(TokenCategory.Constraint)]
    [TokenDescription("Constraint: maximum collection element count")]
    [TokenSymbol("maxcount")]
    Maxcount,

    [TokenCategory(TokenCategory.Constraint)]
    [TokenDescription("Constraint: maximum decimal places (decimal fields only)")]
    [TokenSymbol("maxplaces")]
    Maxplaces,

    [TokenCategory(TokenCategory.Constraint)]
    [TokenDescription("Constraint: ordinal ordering for choice fields")]
    [TokenSymbol("ordered")]
    Ordered,

    // ═══ Keywords: actions ═══

    [TokenCategory(TokenCategory.Action)]
    [TokenCategory(TokenCategory.Type)]
    [TokenDescription("Assigns a value to a field; also a collection type")]
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

    [TokenCategory(TokenCategory.Declaration)]
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
    [TokenDescription("Integer scalar type")]
    [TokenSymbol("integer")]
    IntegerType,

    [TokenCategory(TokenCategory.Type)]
    [TokenDescription("Decimal scalar type — exact base-10 arithmetic")]
    [TokenSymbol("decimal")]
    DecimalType,

    [TokenCategory(TokenCategory.Type)]
    [TokenDescription("Choice type — constrained value set")]
    [TokenSymbol("choice")]
    ChoiceType,

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
    [TokenSymbol("and")]
    And,

    [TokenCategory(TokenCategory.Operator)]
    [TokenDescription("Logical OR")]
    [TokenSymbol("or")]
    Or,

    [TokenCategory(TokenCategory.Operator)]
    [TokenDescription("Logical NOT")]
    [TokenSymbol("not")]
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

    [TokenCategory(TokenCategory.Operator)]
    [TokenDescription("Modulo")]
    [TokenSymbol("%")]
    Percent,

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
    private static readonly Dictionary<PreceptToken, TokenCategoryAttribute[]> _categories = [];
    private static readonly Dictionary<PreceptToken, TokenDescriptionAttribute?> _descriptions = [];
    private static readonly Dictionary<PreceptToken, TokenSymbolAttribute?> _symbols = [];

    static PreceptTokenMeta()
    {
        foreach (var member in Enum.GetValues<PreceptToken>())
        {
            var fi = typeof(PreceptToken).GetField(member.ToString())!;
            _categories[member] = fi.GetCustomAttributes(typeof(TokenCategoryAttribute), false)
                .Cast<TokenCategoryAttribute>().ToArray();
            _descriptions[member] = fi.GetCustomAttributes(typeof(TokenDescriptionAttribute), false)
                .Cast<TokenDescriptionAttribute>().FirstOrDefault();
            _symbols[member] = fi.GetCustomAttributes(typeof(TokenSymbolAttribute), false)
                .Cast<TokenSymbolAttribute>().FirstOrDefault();
        }
    }

    /// <summary>Returns the primary (first declared) category, or null if none.</summary>
    public static TokenCategory? GetCategory(PreceptToken token)
        => _categories.GetValueOrDefault(token) is { Length: > 0 } cats ? cats[0].Category : null;

    /// <summary>Returns all categories for a token (supports dual-role tokens like <c>set</c>).</summary>
    public static IReadOnlyList<TokenCategory> GetCategories(PreceptToken token)
        => _categories.GetValueOrDefault(token) is { } cats
            ? cats.Select(c => c.Category).ToArray()
            : [];

    public static string? GetDescription(PreceptToken token)
        => _descriptions.GetValueOrDefault(token)?.Description;

    public static string? GetSymbol(PreceptToken token)
        => _symbols.GetValueOrDefault(token)?.Symbol;

    /// <summary>
    /// Returns all tokens that have the specified category (primary or secondary).
    /// </summary>
    public static IEnumerable<PreceptToken> GetByCategory(TokenCategory category)
        => _categories.Where(kv => kv.Value.Any(c => c.Category == category)).Select(kv => kv.Key);

    /// <summary>
    /// Returns a dictionary mapping lowercase word-token symbol text to token kind.
    /// Built by reflecting <see cref="TokenSymbolAttribute"/> — adding a keyword to the enum
    /// automatically adds it to this dictionary (zero drift).
    /// Includes all tokens with purely alphabetic symbols (keywords + keyword-operators
    /// like <c>contains</c>). Non-alphabetic symbols (operators, punctuation) are excluded.
    /// Dual-role tokens like <c>set</c> (Action + Type) appear once — the first
    /// enum member wins via <see cref="Dictionary{TKey,TValue}.TryAdd"/>.
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
