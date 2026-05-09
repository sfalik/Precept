using System.ComponentModel;
using ModelContextProtocol.Server;
using Precept.Language;
using Precept.Mcp.Dtos;
using Precept.Pipeline;

namespace Precept.Mcp.Tools;

[McpServerToolType]
public static class CompileTool
{
    [McpServerTool(Name = "precept_compile")]
    [Description("Parse, type-check, and analyze a precept definition. Returns diagnostics and a typed definition structure on success.")]
    public static CompileResultDto Compile(string text)
    {
        var compilation = Compiler.Compile(text);
        var diagnostics = compilation.Diagnostics.Select(MapDiagnostic).ToArray();

        if (compilation.HasErrors)
        {
            return new CompileResultDto(true, diagnostics, null);
        }

        return new CompileResultDto(false, diagnostics, MapDefinition(text, compilation));
    }

    private static DiagnosticDto MapDiagnostic(Diagnostic diagnostic)
        => new(
            FormatDiagnosticCode(diagnostic),
            FormatSeverity(diagnostic.Severity),
            diagnostic.Message,
            new DiagnosticLocationDto(
                diagnostic.Span.StartLine,
                diagnostic.Span.StartColumn,
                diagnostic.Span.Length));

    private static PreceptDefinitionDto MapDefinition(string source, Compilation compilation)
        => new(
            GetDefinitionName(compilation.ConstructManifest),
            compilation.Semantics.States.IsEmpty,
            compilation.Semantics.Fields.Select(field => MapField(field, source)).ToArray(),
            compilation.Semantics.States.Select(state => MapState(state, compilation.Semantics, source)).ToArray(),
            compilation.Semantics.Events.Select(@event => MapEvent(@event, compilation.Semantics, source)).ToArray(),
            compilation.Semantics.Rules.Select(rule => MapRule(rule, source)).ToArray());

    private static PreceptFieldDto MapField(TypedField field, string source)
        => new(
            field.Name,
            RenderTypeName(field.ResolvedType),
            field.IsOptional,
            field.IsWritable,
            field.Modifiers.Select(RenderModifier).ToArray(),
            RenderExpression(field.DefaultExpression, source),
            RenderExpression(field.ComputedExpression, source),
            RenderQualifier(field));

    private static PreceptStateDto MapState(TypedState state, SemanticIndex semantics, string source)
        => new(
            state.Name,
            state.Modifiers.Select(RenderModifier).ToArray(),
            semantics.EnsuresByState.TryGetValue(state.Name, out var ensures)
                ? ensures.Select(ensure => MapEnsure(ensure, source)).ToArray()
                : []);

    private static PreceptEventDto MapEvent(TypedEvent @event, SemanticIndex semantics, string source)
        => new(
            @event.Name,
            @event.Args.Select(MapArg).ToArray(),
            semantics.TransitionRows
                .Where(row => string.Equals(row.EventName, @event.Name, StringComparison.Ordinal))
                .Select(row => MapTransitionRow(row, source))
                .ToArray());

    private static EventArgDto MapArg(TypedArg arg)
        => new(arg.Name, RenderTypeName(arg.ResolvedType));

    private static TransitionRowDto MapTransitionRow(TypedTransitionRow row, string source)
        => new(
            row.FromState is null ? ["*"] : [row.FromState],
            RenderExpression(row.Guard, source),
            row.Actions
                .Select(action => RenderSpan(action.Span, source))
                .Where(action => !string.IsNullOrWhiteSpace(action))
                .ToArray()!,
            row.TargetState);

    private static PreceptRuleDto MapRule(TypedRule rule, string source)
        => new(
            RenderExpression(rule.Condition, source) ?? string.Empty,
            RenderExpression(rule.Message, source));

    private static EnsureDto MapEnsure(TypedEnsure ensure, string source)
        => new(
            ensure.Kind.ToString(),
            ensure.AnchorState ?? ensure.AnchorEvent ?? "global",
            RenderExpression(ensure.Condition, source) ?? string.Empty,
            RenderExpression(ensure.Message, source),
            RenderExpression(ensure.Guard, source));

    private static string GetDefinitionName(ConstructManifest manifest)
    {
        var header = manifest.Constructs.FirstOrDefault(construct => construct.Meta.Kind == ConstructKind.PreceptHeader);
        var names = header?.GetSlot<IdentifierListSlot>(ConstructSlotKind.IdentifierList)?.Names;

        return names is { } value && value.Length > 0
            ? value[0]
            : string.Empty;
    }

    private static string FormatDiagnosticCode(Diagnostic diagnostic)
        => diagnostic.Code.StartsWith("PRE", StringComparison.Ordinal)
            ? diagnostic.Code
            : Enum.TryParse<DiagnosticCode>(diagnostic.Code, out var code)
                ? $"PRE{(int)code:D4}"
                : diagnostic.Code;

    private static string FormatSeverity(Severity severity)
        => severity switch
        {
            Severity.Info => "Hint",
            Severity.Warning => nameof(Severity.Warning),
            Severity.Error => nameof(Severity.Error),
            _ => severity.ToString(),
        };

    private static string RenderModifier(ModifierKind kind)
        => Modifiers.GetMeta(kind).Token.Text ?? kind.ToString();

    private static string RenderTypeName(TypeKind kind)
    {
        var meta = Types.GetMeta(kind);
        return meta.Token?.Text ?? meta.DisplayName;
    }

    private static string? RenderExpression(TypedExpression? expression, string source)
        => expression is null ? null : RenderSpan(expression.Span, source);

    private static string? RenderQualifier(TypedField field)
    {
        var qualifiers = field.DeclaredQualifiers
            .Where(qualifier => qualifier.Origin == QualifierOrigin.Explicit)
            .Select(RenderQualifierPart)
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToArray();

        return qualifiers.Length == 0 ? null : string.Join(" ", qualifiers);
    }

    private static string RenderQualifierPart(DeclaredQualifierMeta qualifier)
    {
        var preposition = qualifier.Preposition is { } tokenKind
            ? Tokens.GetMeta(tokenKind).Text
            : string.Empty;

        var value = qualifier switch
        {
            DeclaredQualifierMeta.Currency currency => currency.CurrencyCode,
            DeclaredQualifierMeta.Unit unit => unit.UnitCode,
            DeclaredQualifierMeta.Dimension dimension => dimension.DimensionName,
            DeclaredQualifierMeta.FromCurrency fromCurrency => fromCurrency.CurrencyCode,
            DeclaredQualifierMeta.ToCurrency toCurrency => toCurrency.CurrencyCode,
            DeclaredQualifierMeta.Timezone timezone => timezone.TimezoneId,
            DeclaredQualifierMeta.TemporalDimension temporalDimension => temporalDimension.Value.ToString(),
            DeclaredQualifierMeta.TemporalUnit temporalUnit => temporalUnit.UnitName,
            _ => string.Empty,
        };

        return string.IsNullOrWhiteSpace(preposition)
            ? value
            : $"{preposition} '{value}'";
    }

    private static string? RenderSpan(SourceSpan span, string source)
    {
        if (span.Length <= 0 || span.Offset < 0 || span.End > source.Length)
        {
            return null;
        }

        return source.Substring(span.Offset, span.Length).Trim();
    }
}
