using System;
using System.Collections.Generic;
using System.Linq;

namespace Precept;

[Flags]
internal enum StaticValueKind
{
    None = 0,
    String = 1,
    Number = 2,
    Boolean = 4,
    Null = 8
}

internal sealed record PreceptTypeDiagnostic(
    LanguageConstraint Constraint,
    string Message,
    int Line,
    int Column = 0,
    string? StateContext = null)
{
    public string DiagnosticCode => ConstraintCatalog.ToDiagnosticCode(Constraint.Id);
}

internal sealed record PreceptTypeExpressionInfo(
    int Line,
    string ExpressionText,
    StaticValueKind Kind,
    string ScopeKind,
    string? StateContext = null,
    string? EventName = null);

internal sealed record PreceptTypeScopeInfo(
    int Line,
    string ScopeKind,
    IReadOnlyDictionary<string, StaticValueKind> Symbols,
    string? StateContext = null,
    string? EventName = null);

internal sealed class PreceptTypeContext(
    IReadOnlyList<PreceptTypeExpressionInfo> expressions,
    IReadOnlyList<PreceptTypeScopeInfo> scopes)
{
    public IReadOnlyList<PreceptTypeExpressionInfo> Expressions { get; } = expressions;

    public IReadOnlyList<PreceptTypeScopeInfo> Scopes { get; } = scopes;

    public PreceptTypeScopeInfo? FindBestScope(int oneBasedLine, string? stateContext = null, string? eventName = null)
    {
        var eligible = Scopes
            .Where(scope => scope.Line <= oneBasedLine)
            .Where(scope => stateContext is null || string.Equals(scope.StateContext, stateContext, StringComparison.Ordinal))
            .Where(scope => eventName is null || string.Equals(scope.EventName, eventName, StringComparison.Ordinal))
            .ToArray();

        if (eligible.Length == 0)
            return null;

        var bestLine = eligible.Max(scope => scope.Line);
        return eligible.LastOrDefault(scope => scope.Line == bestLine);
    }
}

internal sealed record PreceptTypeCheckResult(
    IReadOnlyList<PreceptTypeDiagnostic> Diagnostics,
    PreceptTypeContext TypeContext)
{
    public bool HasErrors => Diagnostics.Count > 0;
}

internal sealed record PreceptCompileValidationResult(
    IReadOnlyList<PreceptTypeDiagnostic> Diagnostics,
    PreceptTypeContext TypeContext)
{
    public bool HasErrors => Diagnostics.Count > 0;
}

internal static class PreceptTypeChecker
{
    public static PreceptTypeCheckResult Check(PreceptDefinition model)
    {
        var diagnostics = new List<PreceptTypeDiagnostic>();
        var expressions = new List<PreceptTypeExpressionInfo>();
        var scopes = new List<PreceptTypeScopeInfo>();

        var dataFieldKinds = model.Fields.ToDictionary(
            field => field.Name,
            MapFieldContractKind,
            StringComparer.Ordinal);

        var eventArgKinds = model.Events.ToDictionary(
            evt => evt.Name,
            evt => evt.Args.ToDictionary(
                arg => arg.Name,
                MapFieldContractKind,
                StringComparer.Ordinal),
            StringComparer.Ordinal);

        var collectionFieldMap = model.CollectionFields.ToDictionary(
            field => field.Name,
            field => field,
            StringComparer.Ordinal);

        var stateAssertNarrowings = BuildStateAssertNarrowings(model, dataFieldKinds);
        ValidateTransitionRows(model, dataFieldKinds, eventArgKinds, collectionFieldMap, stateAssertNarrowings, diagnostics, expressions, scopes);
        ValidateRules(model, dataFieldKinds, eventArgKinds, diagnostics, expressions, scopes);

        return new PreceptTypeCheckResult(diagnostics, new PreceptTypeContext(expressions, scopes));
    }

    internal static StaticValueKind MapFieldContractKind(PreceptField field) => MapKind(field.Type, field.IsNullable);

    internal static StaticValueKind MapFieldContractKind(PreceptEventArg arg) => MapKind(arg.Type, arg.IsNullable);

    internal static bool IsAssignableKind(StaticValueKind actual, StaticValueKind expected) => IsAssignable(actual, expected);

    internal static string FormatKinds(StaticValueKind kinds)
    {
        if (kinds == StaticValueKind.None)
            return "unknown";

        var labels = new List<string>(4);
        if (HasFlag(kinds, StaticValueKind.String)) labels.Add("string");
        if (HasFlag(kinds, StaticValueKind.Number)) labels.Add("number");
        if (HasFlag(kinds, StaticValueKind.Boolean)) labels.Add("boolean");
        if (HasFlag(kinds, StaticValueKind.Null)) labels.Add("null");
        return string.Join("|", labels);
    }

    private static void ValidateTransitionRows(
        PreceptDefinition model,
        IReadOnlyDictionary<string, StaticValueKind> dataFieldKinds,
        IReadOnlyDictionary<string, Dictionary<string, StaticValueKind>> eventArgKinds,
        IReadOnlyDictionary<string, PreceptCollectionField> collectionFieldMap,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, StaticValueKind>> stateAssertNarrowings,
        List<PreceptTypeDiagnostic> diagnostics,
        List<PreceptTypeExpressionInfo> expressions,
        List<PreceptTypeScopeInfo> scopes)
    {
        var allStates = model.States.Select(static state => state.Name).ToArray();
        var rows = model.TransitionRows ?? Array.Empty<PreceptTransitionRow>();

        foreach (var group in rows.GroupBy(row => row.EventName, StringComparer.Ordinal))
        {
            var eventName = group.Key;
            var groupedRows = group
                .SelectMany(row => ExpandRowStates(row, allStates)
                    .Select(state => (State: state, Row: row)))
                .GroupBy(x => x.State, StringComparer.Ordinal);

            foreach (var stateGroup in groupedRows)
            {
                var baseSymbols = BuildSymbolKinds(
                    dataFieldKinds,
                    eventArgKinds,
                    eventName,
                    model.CollectionFields,
                    stateAssertNarrowings.TryGetValue(stateGroup.Key, out var stateNarrowing) ? stateNarrowing : null);
                scopes.Add(new PreceptTypeScopeInfo(
                    stateGroup.Min(x => x.Row.SourceLine),
                    "transition-base",
                    new Dictionary<string, StaticValueKind>(baseSymbols, StringComparer.Ordinal),
                    stateGroup.Key,
                    eventName));

                IReadOnlyDictionary<string, StaticValueKind> branchSymbols = baseSymbols;

                foreach (var item in stateGroup.OrderBy(x => x.Row.SourceLine))
                {
                    var row = item.Row;
                    IReadOnlyDictionary<string, StaticValueKind> setSymbols = branchSymbols;

                    if (row.WhenGuard is not null && !string.IsNullOrWhiteSpace(row.WhenText))
                    {
                        scopes.Add(new PreceptTypeScopeInfo(
                            row.SourceLine,
                            "when",
                            new Dictionary<string, StaticValueKind>(branchSymbols, StringComparer.Ordinal),
                            item.State,
                            eventName));
                        ValidateExpression(
                            row.WhenGuard,
                            row.WhenText!,
                            row.SourceLine,
                            branchSymbols,
                            StaticValueKind.Boolean,
                            "when predicate",
                            diagnostics,
                            expressions,
                            stateContext: item.State);

                        setSymbols = ApplyNarrowing(row.WhenGuard, branchSymbols, assumeTrue: true);
                        branchSymbols = ApplyNarrowing(row.WhenGuard, branchSymbols, assumeTrue: false);
                    }

                    scopes.Add(new PreceptTypeScopeInfo(
                        row.SourceLine,
                        "transition-actions",
                        new Dictionary<string, StaticValueKind>(setSymbols, StringComparer.Ordinal),
                        item.State,
                        eventName));

                    foreach (var assignment in row.SetAssignments)
                    {
                        if (!dataFieldKinds.TryGetValue(assignment.Key, out var targetKind))
                            continue;

                        ValidateExpression(
                            assignment.Expression,
                            assignment.ExpressionText,
                            assignment.SourceLine > 0 ? assignment.SourceLine : row.SourceLine,
                            setSymbols,
                            targetKind,
                            $"set target '{assignment.Key}'",
                            diagnostics,
                            expressions,
                            stateContext: item.State);
                    }

                    ValidateCollectionMutations(
                        row.CollectionMutations,
                        setSymbols,
                        dataFieldKinds,
                        collectionFieldMap,
                        diagnostics,
                        expressions,
                        item.State,
                        row.SourceLine,
                        eventName);
                }
            }
        }
    }

    private static void ValidateRules(
        PreceptDefinition model,
        IReadOnlyDictionary<string, StaticValueKind> dataFieldKinds,
        IReadOnlyDictionary<string, Dictionary<string, StaticValueKind>> eventArgKinds,
        List<PreceptTypeDiagnostic> diagnostics,
        List<PreceptTypeExpressionInfo> expressions,
        List<PreceptTypeScopeInfo> scopes)
    {
        var dataSymbols = new Dictionary<string, StaticValueKind>(dataFieldKinds, StringComparer.Ordinal);
        foreach (var col in model.CollectionFields)
        {
            dataSymbols[$"{col.Name}.count"] = StaticValueKind.Number;

            var innerKind = MapScalarTypeToKind(col.InnerType);
            if (col.CollectionKind == PreceptCollectionKind.Set)
            {
                dataSymbols[$"{col.Name}.min"] = innerKind;
                dataSymbols[$"{col.Name}.max"] = innerKind;
            }

            if (col.CollectionKind is PreceptCollectionKind.Queue or PreceptCollectionKind.Stack)
                dataSymbols[$"{col.Name}.peek"] = innerKind;
        }

        scopes.Add(new PreceptTypeScopeInfo(1, "data-rules", new Dictionary<string, StaticValueKind>(dataSymbols, StringComparer.Ordinal)));

        if (model.Invariants is not null)
        {
            foreach (var invariant in model.Invariants)
            {
                ValidateExpression(
                    invariant.Expression,
                    invariant.ExpressionText,
                    invariant.SourceLine,
                    dataSymbols,
                    StaticValueKind.Boolean,
                    "invariant",
                    diagnostics,
                    expressions);
            }
        }

        if (model.StateAsserts is not null)
        {
            foreach (var stateAssert in model.StateAsserts)
            {
                ValidateExpression(
                    stateAssert.Expression,
                    stateAssert.ExpressionText,
                    stateAssert.SourceLine,
                    dataSymbols,
                    StaticValueKind.Boolean,
                    $"state assert on '{stateAssert.State}'",
                    diagnostics,
                    expressions,
                    stateContext: stateAssert.State);
            }
        }

        if (model.EventAsserts is not null)
        {
            foreach (var eventAssert in model.EventAsserts)
            {
                var symbols = BuildEventAssertSymbols(model, eventArgKinds, eventAssert.EventName);
                scopes.Add(new PreceptTypeScopeInfo(
                    eventAssert.SourceLine,
                    "event-assert",
                    new Dictionary<string, StaticValueKind>(symbols, StringComparer.Ordinal),
                    null,
                    eventAssert.EventName));
                ValidateExpression(
                    eventAssert.Expression,
                    eventAssert.ExpressionText,
                    eventAssert.SourceLine,
                    symbols,
                    StaticValueKind.Boolean,
                    $"event assert on '{eventAssert.EventName}'",
                    diagnostics,
                    expressions,
                    eventName: eventAssert.EventName);
            }
        }
    }

    private static IReadOnlyDictionary<string, StaticValueKind> BuildEventAssertSymbols(
        PreceptDefinition model,
        IReadOnlyDictionary<string, Dictionary<string, StaticValueKind>> eventArgKinds,
        string eventName)
    {
        var symbols = new Dictionary<string, StaticValueKind>(StringComparer.Ordinal);
        if (!eventArgKinds.TryGetValue(eventName, out var args))
            return symbols;

        foreach (var pair in args)
        {
            symbols[pair.Key] = pair.Value;
            symbols[$"{eventName}.{pair.Key}"] = pair.Value;
        }

        return symbols;
    }

    private static IEnumerable<string> ExpandRowStates(PreceptTransitionRow row, IReadOnlyList<string> allStates)
    {
        if (string.Equals(row.FromState, "any", StringComparison.OrdinalIgnoreCase))
            return allStates;

        return [row.FromState];
    }

    private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, StaticValueKind>> BuildStateAssertNarrowings(
        PreceptDefinition model,
        IReadOnlyDictionary<string, StaticValueKind> dataFieldKinds)
    {
        var result = new Dictionary<string, IReadOnlyDictionary<string, StaticValueKind>>(StringComparer.Ordinal);
        if (model.StateAsserts is null || model.StateAsserts.Count == 0)
            return result;

        foreach (var group in model.StateAsserts
            .Where(static stateAssert => stateAssert.Preposition == PreceptAssertPreposition.In)
            .GroupBy(static stateAssert => stateAssert.State, StringComparer.Ordinal))
        {
            IReadOnlyDictionary<string, StaticValueKind> narrowed = new Dictionary<string, StaticValueKind>(dataFieldKinds, StringComparer.Ordinal);

            foreach (var stateAssert in group)
                narrowed = ApplyNarrowing(stateAssert.Expression, narrowed, assumeTrue: true);

            result[group.Key] = narrowed;
        }

        return result;
    }

    private static Dictionary<string, StaticValueKind> BuildSymbolKinds(
        IReadOnlyDictionary<string, StaticValueKind> dataFieldKinds,
        IReadOnlyDictionary<string, Dictionary<string, StaticValueKind>> eventArgKinds,
        string eventName,
        IReadOnlyList<PreceptCollectionField> collectionFields,
        IReadOnlyDictionary<string, StaticValueKind>? stateSymbols)
    {
        var symbols = stateSymbols is not null
            ? new Dictionary<string, StaticValueKind>(stateSymbols, StringComparer.Ordinal)
            : new Dictionary<string, StaticValueKind>(dataFieldKinds, StringComparer.Ordinal);

        if (eventArgKinds.TryGetValue(eventName, out var eventArgs))
        {
            foreach (var pair in eventArgs)
            {
                symbols[pair.Key] = pair.Value;
                symbols[$"{eventName}.{pair.Key}"] = pair.Value;
            }
        }

        foreach (var col in collectionFields)
        {
            var innerKind = MapScalarTypeToKind(col.InnerType);
            symbols[$"{col.Name}.count"] = StaticValueKind.Number;

            if (col.CollectionKind == PreceptCollectionKind.Set)
            {
                symbols[$"{col.Name}.min"] = innerKind;
                symbols[$"{col.Name}.max"] = innerKind;
            }

            if (col.CollectionKind is PreceptCollectionKind.Queue or PreceptCollectionKind.Stack)
                symbols[$"{col.Name}.peek"] = innerKind;
        }

        return symbols;
    }

    private static void ValidateExpression(
        PreceptExpression expression,
        string expressionText,
        int sourceLine,
        IReadOnlyDictionary<string, StaticValueKind> symbols,
        StaticValueKind expectedKind,
        string expectedLabel,
        List<PreceptTypeDiagnostic> diagnostics,
        List<PreceptTypeExpressionInfo> expressions,
        string? stateContext = null,
        string? eventName = null)
    {
        if (!TryInferKind(expression, symbols, out var actualKind, out var diagnostic))
        {
            diagnostics.Add(diagnostic! with
            {
                Line = sourceLine > 0 ? sourceLine : diagnostic.Line,
                StateContext = stateContext
            });
            return;
        }

        if (IsAssignable(actualKind, expectedKind))
        {
            expressions.Add(new PreceptTypeExpressionInfo(
                sourceLine,
                expressionText,
                actualKind,
                expectedLabel,
                stateContext,
                eventName));
            return;
        }

        var constraint = HasFlag(actualKind, StaticValueKind.Null) && !HasFlag(expectedKind, StaticValueKind.Null)
            ? ConstraintCatalog.C42
            : ConstraintCatalog.C39;

        expressions.Add(new PreceptTypeExpressionInfo(
            sourceLine,
            expressionText,
            actualKind,
            expectedLabel,
            stateContext,
            eventName));

        diagnostics.Add(new PreceptTypeDiagnostic(
            constraint,
            $"{expectedLabel} type mismatch: expected {FormatKinds(expectedKind)} but expression produces {FormatKinds(actualKind)}.",
            sourceLine,
            StateContext: stateContext));
    }

    private static bool TryInferKind(
        PreceptExpression expression,
        IReadOnlyDictionary<string, StaticValueKind> symbols,
        out StaticValueKind kind,
        out PreceptTypeDiagnostic? diagnostic)
    {
        kind = StaticValueKind.None;
        diagnostic = null;

        switch (expression)
        {
            case PreceptLiteralExpression literal:
                kind = MapLiteralKind(literal.Value);
                return true;

            case PreceptIdentifierExpression identifier:
            {
                var key = identifier.Member is null ? identifier.Name : $"{identifier.Name}.{identifier.Member}";
                if (!symbols.TryGetValue(key, out kind))
                {
                    diagnostic = new PreceptTypeDiagnostic(
                        ConstraintCatalog.C38,
                        $"unknown identifier '{key}'.",
                        0);
                    return false;
                }

                return true;
            }

            case PreceptParenthesizedExpression parenthesized:
                return TryInferKind(parenthesized.Inner, symbols, out kind, out diagnostic);

            case PreceptUnaryExpression unary:
            {
                if (!TryInferKind(unary.Operand, symbols, out var operandKind, out diagnostic))
                    return false;

                if (unary.Operator == "!")
                {
                    if (!IsExactly(operandKind, StaticValueKind.Boolean))
                    {
                        diagnostic = new PreceptTypeDiagnostic(
                            ConstraintCatalog.C40,
                            "operator '!' requires boolean operand.",
                            0);
                        return false;
                    }

                    kind = StaticValueKind.Boolean;
                    return true;
                }

                if (unary.Operator == "-")
                {
                    if (!IsExactly(operandKind, StaticValueKind.Number))
                    {
                        diagnostic = new PreceptTypeDiagnostic(
                            ConstraintCatalog.C40,
                            "unary '-' requires numeric operand.",
                            0);
                        return false;
                    }

                    kind = StaticValueKind.Number;
                    return true;
                }

                diagnostic = new PreceptTypeDiagnostic(
                    ConstraintCatalog.C40,
                    $"unsupported unary operator '{unary.Operator}'.",
                    0);
                return false;
            }

            case PreceptBinaryExpression binary:
                return TryInferBinaryKind(binary, symbols, out kind, out diagnostic);

            default:
                diagnostic = new PreceptTypeDiagnostic(
                    ConstraintCatalog.C39,
                    "unsupported expression node.",
                    0);
                return false;
        }
    }

    private static bool TryInferBinaryKind(
        PreceptBinaryExpression binary,
        IReadOnlyDictionary<string, StaticValueKind> symbols,
        out StaticValueKind kind,
        out PreceptTypeDiagnostic? diagnostic)
    {
        kind = StaticValueKind.None;
        diagnostic = null;

        switch (binary.Operator)
        {
            case "&&":
            {
                if (!TryInferKind(binary.Left, symbols, out var leftKind, out diagnostic))
                    return false;

                if (!IsExactly(leftKind, StaticValueKind.Boolean))
                {
                    diagnostic = new PreceptTypeDiagnostic(ConstraintCatalog.C41, "operator '&&' requires boolean operands.", 0);
                    return false;
                }

                var rightSymbols = ApplyNarrowing(binary.Left, symbols, assumeTrue: true);
                if (!TryInferKind(binary.Right, rightSymbols, out var rightKind, out diagnostic))
                    return false;

                if (!IsExactly(rightKind, StaticValueKind.Boolean))
                {
                    diagnostic = new PreceptTypeDiagnostic(ConstraintCatalog.C41, "operator '&&' requires boolean operands.", 0);
                    return false;
                }

                kind = StaticValueKind.Boolean;
                return true;
            }

            case "||":
            {
                if (!TryInferKind(binary.Left, symbols, out var leftKind, out diagnostic))
                    return false;

                if (!IsExactly(leftKind, StaticValueKind.Boolean))
                {
                    diagnostic = new PreceptTypeDiagnostic(ConstraintCatalog.C41, "operator '||' requires boolean operands.", 0);
                    return false;
                }

                var rightSymbols = ApplyNarrowing(binary.Left, symbols, assumeTrue: false);
                if (!TryInferKind(binary.Right, rightSymbols, out var rightKind, out diagnostic))
                    return false;

                if (!IsExactly(rightKind, StaticValueKind.Boolean))
                {
                    diagnostic = new PreceptTypeDiagnostic(ConstraintCatalog.C41, "operator '||' requires boolean operands.", 0);
                    return false;
                }

                kind = StaticValueKind.Boolean;
                return true;
            }

            case "+":
            {
                if (!TryInferKind(binary.Left, symbols, out var leftKind, out diagnostic) ||
                    !TryInferKind(binary.Right, symbols, out var rightKind, out diagnostic))
                    return false;

                var stringCandidate = IsExactly(leftKind, StaticValueKind.String) && IsExactly(rightKind, StaticValueKind.String);
                var numberCandidate = IsExactly(leftKind, StaticValueKind.Number) && IsExactly(rightKind, StaticValueKind.Number);

                if (stringCandidate)
                {
                    kind = StaticValueKind.String;
                    return true;
                }

                if (numberCandidate)
                {
                    kind = StaticValueKind.Number;
                    return true;
                }

                diagnostic = new PreceptTypeDiagnostic(ConstraintCatalog.C41, "operator '+' requires number+number or string+string.", 0);
                return false;
            }

            case "-":
            case "*":
            case "/":
            case "%":
            case ">":
            case ">=":
            case "<":
            case "<=":
            {
                if (!TryInferKind(binary.Left, symbols, out var leftKind, out diagnostic) ||
                    !TryInferKind(binary.Right, symbols, out var rightKind, out diagnostic))
                    return false;

                if (!IsExactly(leftKind, StaticValueKind.Number) || !IsExactly(rightKind, StaticValueKind.Number))
                {
                    diagnostic = new PreceptTypeDiagnostic(
                        ConstraintCatalog.C41,
                        $"operator '{binary.Operator}' requires numeric operands.",
                        0);
                    return false;
                }

                kind = binary.Operator is ">" or ">=" or "<" or "<="
                    ? StaticValueKind.Boolean
                    : StaticValueKind.Number;
                return true;
            }

            case "==":
            case "!=":
                if (!TryInferKind(binary.Left, symbols, out _, out diagnostic) ||
                    !TryInferKind(binary.Right, symbols, out _, out diagnostic))
                    return false;

                kind = StaticValueKind.Boolean;
                return true;

            case "contains":
            {
                if (binary.Left is not PreceptIdentifierExpression { Member: null } collectionIdentifier)
                {
                    diagnostic = new PreceptTypeDiagnostic(ConstraintCatalog.C41, "'contains' requires a collection field on the left side.", 0);
                    return false;
                }

                if (!TryInferKind(binary.Right, symbols, out var rightKind, out diagnostic))
                    return false;

                var collectionKey = $"{collectionIdentifier.Name}.count";
                if (!symbols.ContainsKey(collectionKey))
                {
                    diagnostic = new PreceptTypeDiagnostic(ConstraintCatalog.C38, $"unknown identifier '{collectionIdentifier.Name}'.", 0);
                    return false;
                }

                var innerKeyCandidates = new[]
                {
                    $"{collectionIdentifier.Name}.min",
                    $"{collectionIdentifier.Name}.peek",
                    $"{collectionIdentifier.Name}.max"
                };

                var innerKind = innerKeyCandidates
                    .Where(symbols.ContainsKey)
                    .Select(key => symbols[key])
                    .DefaultIfEmpty(StaticValueKind.None)
                    .First();

                if (innerKind != StaticValueKind.None && !IsAssignable(rightKind, innerKind))
                {
                    diagnostic = new PreceptTypeDiagnostic(
                        ConstraintCatalog.C41,
                        $"operator 'contains' requires RHS of type {FormatKinds(innerKind)} but expression produces {FormatKinds(rightKind)}.",
                        0);
                    return false;
                }

                kind = StaticValueKind.Boolean;
                return true;
            }

            default:
                diagnostic = new PreceptTypeDiagnostic(ConstraintCatalog.C41, $"unsupported binary operator '{binary.Operator}'.", 0);
                return false;
        }
    }

    private static IReadOnlyDictionary<string, StaticValueKind> ApplyNarrowing(
        PreceptExpression expression,
        IReadOnlyDictionary<string, StaticValueKind> symbols,
        bool assumeTrue)
    {
        expression = StripParentheses(expression);

        if (expression is PreceptUnaryExpression { Operator: "!" } unary)
            return ApplyNarrowing(unary.Operand, symbols, !assumeTrue);

        if (expression is not PreceptBinaryExpression binary)
            return symbols;

        if (binary.Operator == "&&")
        {
            if (!assumeTrue)
                return symbols;

            var leftNarrowed = ApplyNarrowing(binary.Left, symbols, assumeTrue: true);
            return ApplyNarrowing(binary.Right, leftNarrowed, assumeTrue: true);
        }

        if (binary.Operator == "||")
        {
            if (assumeTrue)
                return symbols;

            var leftNarrowed = ApplyNarrowing(binary.Left, symbols, assumeTrue: false);
            return ApplyNarrowing(binary.Right, leftNarrowed, assumeTrue: false);
        }

        return binary.Operator is "==" or "!=" &&
               TryApplyNullComparisonNarrowing(binary, symbols, assumeTrue, out var narrowed)
            ? narrowed
            : symbols;
    }

    private static bool TryApplyNullComparisonNarrowing(
        PreceptBinaryExpression binary,
        IReadOnlyDictionary<string, StaticValueKind> symbols,
        bool assumeTrue,
        out IReadOnlyDictionary<string, StaticValueKind> narrowed)
    {
        narrowed = symbols;

        var leftIsNull = IsNullLiteral(binary.Left);
        var rightIsNull = IsNullLiteral(binary.Right);
        if (!leftIsNull && !rightIsNull)
            return false;

        string key;
        if (leftIsNull)
        {
            if (!TryGetIdentifierKey(binary.Right, out key))
                return false;
        }
        else
        {
            if (!TryGetIdentifierKey(binary.Left, out key))
                return false;
        }

        if (!symbols.TryGetValue(key, out var existingKind))
            return false;

        var expectsNull = binary.Operator switch
        {
            "==" => assumeTrue,
            "!=" => !assumeTrue,
            _ => false
        };

        var updatedKind = expectsNull
            ? StaticValueKind.Null
            : (existingKind & ~StaticValueKind.Null);

        narrowed = new Dictionary<string, StaticValueKind>(symbols, StringComparer.Ordinal)
        {
            [key] = updatedKind
        };
        return true;
    }

    private static void ValidateCollectionMutations(
        IReadOnlyList<PreceptCollectionMutation>? mutations,
        IReadOnlyDictionary<string, StaticValueKind> symbols,
        IReadOnlyDictionary<string, StaticValueKind> dataFieldKinds,
        IReadOnlyDictionary<string, PreceptCollectionField> collectionFieldMap,
        List<PreceptTypeDiagnostic> diagnostics,
        List<PreceptTypeExpressionInfo> expressions,
        string stateContext,
        int fallbackLine,
        string? eventName)
    {
        if (mutations is null || mutations.Count == 0)
            return;

        foreach (var mutation in mutations)
        {
            if (!collectionFieldMap.TryGetValue(mutation.TargetField, out var collectionField))
                continue;

            var innerKind = MapScalarTypeToKind(collectionField.InnerType);
            var line = mutation.SourceLine > 0 ? mutation.SourceLine : fallbackLine;

            switch (mutation.Verb)
            {
                case PreceptCollectionMutationVerb.Add:
                case PreceptCollectionMutationVerb.Remove:
                case PreceptCollectionMutationVerb.Enqueue:
                case PreceptCollectionMutationVerb.Push:
                    if (mutation.Expression is null || string.IsNullOrWhiteSpace(mutation.ExpressionText))
                        break;

                    ValidateExpression(
                        mutation.Expression,
                        mutation.ExpressionText,
                        line,
                        symbols,
                        innerKind,
                        $"'{mutation.Verb.ToString().ToLowerInvariant()} {mutation.TargetField}' value",
                        diagnostics,
                        expressions,
                        stateContext,
                        eventName);
                    break;

                case PreceptCollectionMutationVerb.Dequeue:
                case PreceptCollectionMutationVerb.Pop:
                    if (string.IsNullOrWhiteSpace(mutation.IntoField) || !dataFieldKinds.TryGetValue(mutation.IntoField!, out var intoKind))
                        break;

                    if (IsAssignable(innerKind, intoKind))
                        break;

                    diagnostics.Add(new PreceptTypeDiagnostic(
                        ConstraintCatalog.C43,
                        $"'{mutation.Verb.ToString().ToLowerInvariant()} {mutation.TargetField} into {mutation.IntoField}': cannot assign {FormatKinds(innerKind)} to target '{mutation.IntoField}' of type {FormatKinds(intoKind)}.",
                        line,
                        StateContext: stateContext));
                    break;
            }
        }
    }

    private static StaticValueKind MapKind(PreceptScalarType type, bool isNullable)
    {
        var kind = MapScalarTypeToKind(type);
        if (isNullable)
            kind |= StaticValueKind.Null;

        return kind;
    }

    private static StaticValueKind MapScalarTypeToKind(PreceptScalarType type) => type switch
    {
        PreceptScalarType.String => StaticValueKind.String,
        PreceptScalarType.Number => StaticValueKind.Number,
        PreceptScalarType.Boolean => StaticValueKind.Boolean,
        PreceptScalarType.Null => StaticValueKind.Null,
        _ => StaticValueKind.None
    };

    private static StaticValueKind MapLiteralKind(object? value) => value switch
    {
        null => StaticValueKind.Null,
        string => StaticValueKind.String,
        bool => StaticValueKind.Boolean,
        byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal => StaticValueKind.Number,
        _ => StaticValueKind.None
    };

    private static bool TryGetIdentifierKey(PreceptExpression expression, out string key)
    {
        var stripped = StripParentheses(expression);
        if (stripped is PreceptIdentifierExpression identifier)
        {
            key = identifier.Member is null
                ? identifier.Name
                : $"{identifier.Name}.{identifier.Member}";
            return true;
        }

        key = string.Empty;
        return false;
    }

    private static bool IsNullLiteral(PreceptExpression expression)
        => StripParentheses(expression) is PreceptLiteralExpression { Value: null };

    private static PreceptExpression StripParentheses(PreceptExpression expression)
    {
        while (expression is PreceptParenthesizedExpression parenthesized)
            expression = parenthesized.Inner;

        return expression;
    }

    private static bool IsExactly(StaticValueKind kind, StaticValueKind expected)
        => kind == expected;

    private static bool IsAssignable(StaticValueKind actual, StaticValueKind expected)
    {
        var actualNonNull = actual & ~StaticValueKind.Null;
        var expectedNonNull = expected & ~StaticValueKind.Null;

        if (!HasFlag(expected, StaticValueKind.Null) && HasFlag(actual, StaticValueKind.Null))
            return false;

        if ((actualNonNull & ~expectedNonNull) != StaticValueKind.None)
            return false;

        if (actual == StaticValueKind.Null)
            return HasFlag(expected, StaticValueKind.Null);

        return true;
    }

    private static bool HasFlag(StaticValueKind kind, StaticValueKind flag)
        => (kind & flag) == flag;
}