using System.Collections.Generic;
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
    {
        var semantics = compilation.Semantics;
        var omittedFieldsByState = BuildOmittedFieldsByState(compilation.ConstructManifest);

        return new PreceptDefinitionDto(
            GetDefinitionName(compilation.ConstructManifest),
            semantics.States.IsEmpty,
            semantics.Fields.Select(field => MapField(field, source)).ToArray(),
            semantics.States.Select(state => MapState(state, semantics, omittedFieldsByState, source)).ToArray(),
            semantics.Events.Select(@event => MapEvent(@event, semantics, source)).ToArray(),
            semantics.Rules.Select(rule => MapRule(rule, source)).ToArray(),
            semantics.StateHooks.Select(hook => MapStateHook(hook, source)).ToArray());
    }

    private static PreceptFieldDto MapField(TypedField field, string source)
    {
        var typeRef = GetTypeReference(field.Syntax);
        var (choiceElementType, choiceValues) = GetChoiceMetadata(typeRef);

        return new PreceptFieldDto(
            field.Name,
            RenderTypeName(field, typeRef),
            field.IsOptional,
            field.IsWritable,
            RenderFieldModifiers(field, source),
            RenderDefaultValue(field.DefaultExpression, source),
            RenderExpression(field.ComputedExpression, source),
            RenderQualifier(field),
            choiceElementType,
            choiceValues);
    }

    private static PreceptStateDto MapState(
        TypedState state,
        SemanticIndex semantics,
        IReadOnlyDictionary<string, string[]> omittedFieldsByState,
        string source)
    {
        var constraints = semantics.EnsuresByState.TryGetValue(state.Name, out var ensures)
            ? ensures.Select(ensure => MapEnsure(ensure, source)).ToArray()
            : [];

        var stateAccessModes = semantics.AccessModes
            .Where(mode => string.Equals(mode.StateName, state.Name, StringComparison.Ordinal))
            .ToArray();

        var explicitOmits = omittedFieldsByState.TryGetValue(state.Name, out var omitted)
            ? omitted
            : [];

        var omittedFields = explicitOmits
            .Concat(stateAccessModes.Where(mode => mode.Mode == ModifierKind.Omit).Select(mode => mode.FieldName))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var accessModes = stateAccessModes
            .Where(mode => mode.Mode != ModifierKind.Omit)
            .Select(MapAccessMode)
            .ToArray();

        return new PreceptStateDto(
            state.Name,
            state.Modifiers.Select(RenderModifier).ToArray(),
            constraints,
            omittedFields.Length == 0 ? null : omittedFields,
            accessModes.Length == 0 ? null : accessModes);
    }

    private static PreceptEventDto MapEvent(TypedEvent @event, SemanticIndex semantics, string source)
    {
        var transitionRows = semantics.TransitionRows
            .Where(row => string.Equals(row.EventName, @event.Name, StringComparison.Ordinal))
            .Select(row => MapTransitionRow(row, source));

        var handlerRows = semantics.EventHandlers
            .Where(handler => string.Equals(handler.EventName, @event.Name, StringComparison.Ordinal))
            .Select(handler => MapEventHandlerRow(handler, source));

        var constraints = semantics.Ensures
            .Where(ensure => string.Equals(ensure.AnchorEvent, @event.Name, StringComparison.Ordinal))
            .Select(ensure => MapEnsure(ensure, source))
            .ToArray();

        return new PreceptEventDto(
            @event.Name,
            @event.Args.Select(MapArg).ToArray(),
            transitionRows.Concat(handlerRows).ToArray(),
            constraints.Length == 0 ? null : constraints);
    }

    private static EventArgDto MapArg(TypedArg arg)
        => new(arg.Name, RenderTypeName(arg.ResolvedType), arg.IsOptional);

    private static TransitionRowDto MapTransitionRow(TypedTransitionRow row, string source)
        => new(
            row.FromState is null ? ["*"] : [row.FromState],
            RenderExpression(row.Guard, source),
            row.Actions
                .Select(action => RenderSpan(action.Span, source))
                .Where(action => !string.IsNullOrWhiteSpace(action))
                .ToArray()!,
            row.TargetState,
            Outcomes.GetMeta(ToOutcomeKind(row.Outcome)).SerializedKind,
            row.RejectReason);

    private static TransitionRowDto MapEventHandlerRow(TypedEventHandler handler, string source)
        => new(
            ["*"],
            null,
            handler.Actions
                .Select(action => RenderSpan(action.Span, source))
                .Where(action => !string.IsNullOrWhiteSpace(action))
                .ToArray()!,
            null,
            Outcomes.GetMeta(OutcomeKind.NoTransition).SerializedKind,
            null);

    private static PreceptRuleDto MapRule(TypedRule rule, string source)
        => new(
            RenderExpression(rule.Condition, source) ?? string.Empty,
            RenderMessage(rule.Message, source),
            RenderExpression(rule.Guard, source));

    private static EnsureDto MapEnsure(TypedEnsure ensure, string source)
        => new(
            ensure.Kind.ToString(),
            ensure.AnchorState ?? ensure.AnchorEvent ?? "global",
            RenderExpression(ensure.Condition, source) ?? string.Empty,
            RenderMessage(ensure.Message, source),
            RenderExpression(ensure.Guard, source));

    private static AccessModeDto MapAccessMode(TypedAccessMode accessMode)
        => new(accessMode.StateName, accessMode.FieldName, RenderModifier(accessMode.Mode));

    private static StateHookDto MapStateHook(TypedStateHook hook, string source)
        => new(
            hook.StateName,
            RenderStateHookKind(hook.Scope),
            hook.Actions
                .Select(action => RenderSpan(action.Span, source))
                .Where(action => !string.IsNullOrWhiteSpace(action))
                .ToArray()!);

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

    private static string[] RenderFieldModifiers(TypedField field, string source)
    {
        var modifierSlot = field.Syntax.GetSlot<ModifierListSlot>(ConstructSlotKind.ModifierList);
        if (modifierSlot is null)
        {
            return field.Modifiers.Select(RenderModifier).ToArray();
        }

        return modifierSlot.Modifiers
            .Select(modifier => RenderModifier(modifier, source))
            .ToArray();
    }

    private static string RenderModifier(ParsedModifier modifier, string source)
    {
        var name = RenderModifier(modifier.Kind);
        if (!HasBoundValue(modifier.Kind) || modifier.Value is null)
        {
            return name;
        }

        var value = RenderParsedExpression(modifier.Value, source);
        return string.IsNullOrWhiteSpace(value)
            ? name
            : $"{name} {value}";
    }

    private static bool HasBoundValue(ModifierKind kind)
        => kind is ModifierKind.Min
            or ModifierKind.Max
            or ModifierKind.Minlength
            or ModifierKind.Maxlength
            or ModifierKind.Mincount
            or ModifierKind.Maxcount
            or ModifierKind.Maxplaces;

    private static string RenderModifier(ModifierKind kind)
        => Modifiers.GetMeta(kind).Token.Text ?? kind.ToString();

    private static string RenderTypeName(TypedField field, ParsedTypeReference? typeRef)
        => typeRef is null ? RenderTypeName(field.ResolvedType) : RenderTypeName(typeRef);

    private static string RenderTypeName(ParsedTypeReference typeRef)
        => typeRef switch
        {
            QualifiedTypeReference qualified => RenderTypeName(qualified.InnerType),
            CITypeReference ci => $"~{RenderTypeName(ci.Type.Kind)}",
            CollectionTypeReference collection when collection.CollectionType.Kind == TypeKind.Lookup && collection.KeyType is not null
                => $"{RenderTypeName(collection.CollectionType.Kind)} of {RenderTypeName(collection.KeyType)} to {RenderTypeName(collection.ElementType)}",
            CollectionTypeReference collection when collection.KeyType is not null
                => $"{RenderTypeName(collection.CollectionType.Kind)} of {RenderTypeName(collection.ElementType)} by {RenderTypeName(collection.KeyType)}",
            CollectionTypeReference collection
                => $"{RenderTypeName(collection.CollectionType.Kind)} of {RenderTypeName(collection.ElementType)}",
            ChoiceTypeReference choice => RenderTypeName(choice.Type.Kind),
            SimpleTypeReference simple => RenderTypeName(simple.Type.Kind),
            _ => string.Empty,
        };

    private static string RenderTypeName(TypeKind kind)
    {
        var meta = Types.GetMeta(kind);
        return meta.Token?.Text ?? meta.DisplayName;
    }

    private static (string? ChoiceElementType, string[]? ChoiceValues) GetChoiceMetadata(ParsedTypeReference? typeRef)
    {
        if (typeRef is QualifiedTypeReference qualified)
        {
            return GetChoiceMetadata(qualified.InnerType);
        }

        if (typeRef is not ChoiceTypeReference choice)
        {
            return (null, null);
        }

        var values = choice.Domain.ToArray();
        return (
            choice.ElementType is null ? null : RenderTypeName(choice.ElementType.Kind),
            values.Length == 0 ? null : values);
    }

    private static ParsedTypeReference? GetTypeReference(ParsedConstruct syntax)
        => syntax.GetSlot<TypeExpressionSlot>(ConstructSlotKind.TypeExpression)?.TypeRef;

    private static IReadOnlyDictionary<string, string[]> BuildOmittedFieldsByState(ConstructManifest manifest)
    {
        if (!manifest.ByKind.Contains(ConstructKind.OmitDeclaration))
        {
            return new Dictionary<string, string[]>(StringComparer.Ordinal);
        }

        return manifest.ByKind[ConstructKind.OmitDeclaration]
            .Select(construct => new
            {
                StateName = construct.GetSlot<StateTargetSlot>(ConstructSlotKind.StateTarget)?.StateName,
                FieldName = construct.GetSlot<FieldTargetSlot>(ConstructSlotKind.FieldTarget)?.FieldName,
            })
            .Where(entry => !string.IsNullOrWhiteSpace(entry.StateName) && !string.IsNullOrWhiteSpace(entry.FieldName))
            .GroupBy(entry => entry.StateName!, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(entry => entry.FieldName!).Distinct(StringComparer.Ordinal).ToArray(),
                StringComparer.Ordinal);
    }

    private static OutcomeKind ToOutcomeKind(TransitionRowOutcome outcome)
        => outcome switch
        {
            TransitionRowOutcome.Transition => OutcomeKind.Transition,
            TransitionRowOutcome.NoTransition => OutcomeKind.NoTransition,
            TransitionRowOutcome.Reject => OutcomeKind.Reject,
            _ => OutcomeKind.NoTransition,
        };

    private static string RenderStateHookKind(AnchorScope scope)
        => scope switch
        {
            AnchorScope.OnEntry => "entry",
            AnchorScope.OnExit => "exit",
            _ => scope.ToString(),
        };

    private static string? RenderDefaultValue(TypedExpression? expression, string source)
        => expression is TypedLiteral { Value: string text } ? text : RenderExpression(expression, source);

    private static string? RenderMessage(TypedExpression? expression, string source)
        => expression is TypedLiteral { Value: string text } ? text : RenderExpression(expression, source);

    private static string? RenderExpression(TypedExpression? expression, string source)
        => expression is null ? null : RenderSpan(expression.Span, source);

    private static string? RenderParsedExpression(ParsedExpression? expression, string source)
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
