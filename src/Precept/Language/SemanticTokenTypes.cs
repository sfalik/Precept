using System.Collections.Frozen;

namespace Precept.Language;

public enum SemanticTokenTypeKind
{
    Name = 1,
    KeywordSemantic = 2,
    KeywordGrammar = 3,
    Operator = 4,
    State = 5,
    Event = 6,
    Type = 7,
    Value = 8,
    FieldName = 9,
    ArgName = 10,
    Message = 11,
    Comment = 12,
}

public sealed record SemanticTokenTypeMeta(
    SemanticTokenTypeKind Kind,
    string CustomType,
    string TextMateScope,
    string Description,
    string ForegroundHex,
    bool Bold = false,
    bool Italic = false,
    bool SupportsConstrainedModifier = false);

public static class SemanticTokenTypes
{
    private static readonly FrozenDictionary<SemanticTokenTypeKind, SemanticTokenTypeMeta> _byKind =
        new SemanticTokenTypeMeta[]
        {
            new(SemanticTokenTypeKind.Name, "preceptName", "entity.name.type.precept.precept", "Precept declaration name", "#A5B4FC"),
            new(SemanticTokenTypeKind.KeywordSemantic, "preceptKeywordSemantic", "keyword.other.semantic.precept", "Precept behavioral structure keyword", "#4338CA", Bold: true),
            new(SemanticTokenTypeKind.KeywordGrammar, "preceptKeywordGrammar", "keyword.other.grammar.precept", "Precept grammar connective keyword", "#6366F1"),
            new(SemanticTokenTypeKind.Operator, "preceptOperator", "keyword.operator.precept", "Precept operator", "#6366F1"),
            new(SemanticTokenTypeKind.State, "preceptState", "entity.name.type.state.precept", "Precept state name", "#A898F5", SupportsConstrainedModifier: true),
            new(SemanticTokenTypeKind.Event, "preceptEvent", "entity.name.function.event.precept", "Precept event name", "#30B8E8", SupportsConstrainedModifier: true),
            new(SemanticTokenTypeKind.Type, "preceptType", "entity.name.type.precept", "Precept type keyword", "#9AA8B5"),
            new(SemanticTokenTypeKind.Value, "preceptValue", "constant.language.precept", "Precept literal value", "#84929F"),
            new(SemanticTokenTypeKind.FieldName, "preceptFieldName", "variable.other.field.precept", "Precept field name", "#A5B4FC", SupportsConstrainedModifier: true),
            new(SemanticTokenTypeKind.ArgName, "preceptArgName", "variable.parameter.precept", "Precept argument name", "#9AD8E8", SupportsConstrainedModifier: true),
            new(SemanticTokenTypeKind.Message, "preceptMessage", "string.other.message.precept", "Precept message string", "#FBBF24"),
            new(SemanticTokenTypeKind.Comment, "preceptComment", "comment.line.precept", "Precept comment", "#9096A6", Italic: true),
        }.ToFrozenDictionary(m => m.Kind);

    public static SemanticTokenTypeMeta GetMeta(SemanticTokenTypeKind kind) =>
        _byKind.TryGetValue(kind, out var meta)
            ? meta
            : throw new ArgumentOutOfRangeException(nameof(kind), kind, null);

    public static IReadOnlyList<SemanticTokenTypeMeta> All { get; } =
        Enum.GetValues<SemanticTokenTypeKind>().Select(GetMeta).ToArray();
}
