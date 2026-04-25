namespace Precept.Language;

/// <summary>
/// Grammar meta-rules — singular facts about how Precept source text is structured.
/// Not a catalog (no enum, no <c>GetMeta()</c>, no <c>All</c>). Structured metadata
/// about the grammar as a whole, consumed by MCP, LS hover, reference docs, and AI grounding.
/// </summary>
public static class SyntaxReference
{
    public static string GrammarModel       => "line-oriented";
    public static string CommentSyntax      => "# to end of line";
    public static string IdentifierRules    => "Starts with letter, alphanumeric + underscore, case-sensitive";
    public static string StringLiteralRules => "Double-quoted, \\\" escape only, no interpolation";
    public static string NumberLiteralRules => "Integers (42), decimals (3.14), no hex/scientific/underscore separators";
    public static string WhitespaceRules    => "Not significant — indentation is cosmetic, line breaks separate declarations";
    public static string NullNarrowing      => "if Field != null narrows to non-nullable in the then branch";

    public static IReadOnlyList<string> ConventionalOrder { get; } =
    [
        "header",
        "fields",
        "rules",
        "states",
        "ensures",
        "edits",
        "events",
        "eventEnsures",
        "transitions",
        "stateActions",
    ];
}
