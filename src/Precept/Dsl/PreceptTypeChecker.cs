using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Precept;

[Flags]
internal enum StaticValueKind
{
    None = 0,
    String = 1,
    Number = 2,
    Boolean = 4,
    Null = 8,
    Integer = 16,   // #29
    Decimal = 32,   // #27 (scaffold)
}

internal sealed record PreceptValidationDiagnostic(
    LanguageConstraint Constraint,
    string Message,
    int Line,
    int Column = 0,
    string? StateContext = null)
{
    public string DiagnosticCode => DiagnosticCatalog.ToDiagnosticCode(Constraint.Id);
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

internal sealed record TypeCheckResult(
    IReadOnlyList<PreceptValidationDiagnostic> Diagnostics,
    PreceptTypeContext TypeContext)
{
    public bool HasErrors => Diagnostics.Any(static diagnostic => diagnostic.Constraint.Severity == ConstraintSeverity.Error);
}

internal sealed record ValidationResult(
    IReadOnlyList<PreceptValidationDiagnostic> Diagnostics,
    PreceptTypeContext TypeContext)
{
    public bool HasErrors => Diagnostics.Any(static diagnostic => diagnostic.Constraint.Severity == ConstraintSeverity.Error);
}

internal static class PreceptTypeChecker
{
    public static TypeCheckResult Check(PreceptDefinition model)
    {
        var diagnostics = new List<PreceptValidationDiagnostic>();
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
        ValidateStateActions(model, dataFieldKinds, collectionFieldMap, stateAssertNarrowings, diagnostics, expressions, scopes);
        ValidateFieldConstraints(model, diagnostics);
        ValidateRules(model, dataFieldKinds, eventArgKinds, diagnostics, expressions, scopes);

        return new TypeCheckResult(diagnostics, new PreceptTypeContext(expressions, scopes));
    }

    internal static StaticValueKind MapFieldContractKind(PreceptField field) => MapKind(field.Type, field.IsNullable);

    internal static StaticValueKind MapFieldContractKind(PreceptEventArg arg) => MapKind(arg.Type, arg.IsNullable);

    internal static StaticValueKind MapScalarType(PreceptScalarType type) => MapScalarTypeToKind(type);

    internal static bool TryGetLiteralKind(string label, out StaticValueKind kind)
    {
        switch (label)
        {
            case "true":
            case "false":
                kind = StaticValueKind.Boolean;
                return true;
            case "null":
                kind = StaticValueKind.Null;
                return true;
            default:
                kind = StaticValueKind.None;
                return false;
        }
    }

    internal static bool IsAssignableKind(StaticValueKind actual, StaticValueKind expected) => IsAssignable(actual, expected);

    internal static string FormatKinds(StaticValueKind kinds)
    {
        if (kinds == StaticValueKind.None)
            return "unknown";

        var labels = new List<string>(6);
        if (HasFlag(kinds, StaticValueKind.String)) labels.Add("string");
        if (HasFlag(kinds, StaticValueKind.Number)) labels.Add("number");
        if (HasFlag(kinds, StaticValueKind.Integer)) labels.Add("integer");
        if (HasFlag(kinds, StaticValueKind.Decimal)) labels.Add("decimal");
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
        List<PreceptValidationDiagnostic> diagnostics,
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

                // C47: detect identical guard text for the same (state, event) group
                var seenGuards = new Dictionary<string, int>(StringComparer.Ordinal);

                foreach (var item in stateGroup.OrderBy(x => x.Row.SourceLine))
                {
                    var row = item.Row;
                    IReadOnlyDictionary<string, StaticValueKind> setSymbols = branchSymbols;

                    // C47: duplicate guard detection for this (state, event) group
                    if (row.WhenGuard is not null && !string.IsNullOrWhiteSpace(row.WhenText))
                    {
                        var normalizedGuard = Regex.Replace(
                            row.WhenText!.Trim(), @"\s+", " ");
                        if (seenGuards.TryGetValue(normalizedGuard, out var firstLine))
                        {
                            diagnostics.Add(new PreceptValidationDiagnostic(
                                DiagnosticCatalog.C47,
                                DiagnosticCatalog.C47.FormatMessage(
                                    ("guard", normalizedGuard),
                                    ("state", item.State),
                                    ("event", eventName),
                                    ("sourceLine", firstLine)),
                                row.SourceLine,
                                StateContext: item.State));
                        }
                        else
                        {
                            seenGuards[normalizedGuard] = row.SourceLine;
                        }
                    }

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
                            stateContext: item.State,
                            isBooleanRulePosition: true);

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

    private static void ValidateStateActions(
        PreceptDefinition model,
        IReadOnlyDictionary<string, StaticValueKind> dataFieldKinds,
        IReadOnlyDictionary<string, PreceptCollectionField> collectionFieldMap,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, StaticValueKind>> stateAssertNarrowings,
        List<PreceptValidationDiagnostic> diagnostics,
        List<PreceptTypeExpressionInfo> expressions,
        List<PreceptTypeScopeInfo> scopes)
    {
        if (model.StateActions is null || model.StateActions.Count == 0)
            return;

        foreach (var action in model.StateActions)
        {
            // Build data-only symbols with collection accessors, narrowed by state asserts
            var baseSymbols = new Dictionary<string, StaticValueKind>(
                stateAssertNarrowings.TryGetValue(action.State, out var narrowed) ? narrowed : dataFieldKinds,
                StringComparer.Ordinal);

            foreach (var col in model.CollectionFields)
            {
                var innerKind = MapScalarTypeToKind(col.InnerType);
                baseSymbols[$"{col.Name}.count"] = StaticValueKind.Number;

                if (col.CollectionKind == PreceptCollectionKind.Set)
                {
                    baseSymbols[$"{col.Name}.min"] = innerKind;
                    baseSymbols[$"{col.Name}.max"] = innerKind;
                }

                if (col.CollectionKind is PreceptCollectionKind.Queue or PreceptCollectionKind.Stack)
                    baseSymbols[$"{col.Name}.peek"] = innerKind;
            }

            foreach (var field in model.Fields)
            {
                if (field.Type == PreceptScalarType.String)
                    baseSymbols[$"{field.Name}.length"] = StaticValueKind.Number;
            }

            scopes.Add(new PreceptTypeScopeInfo(
                action.SourceLine,
                "state-action",
                new Dictionary<string, StaticValueKind>(baseSymbols, StringComparer.Ordinal),
                action.State));

            foreach (var assignment in action.SetAssignments)
            {
                if (!dataFieldKinds.TryGetValue(assignment.Key, out var targetKind))
                    continue;

                ValidateExpression(
                    assignment.Expression,
                    assignment.ExpressionText,
                    assignment.SourceLine > 0 ? assignment.SourceLine : action.SourceLine,
                    baseSymbols,
                    targetKind,
                    $"set target '{assignment.Key}'",
                    diagnostics,
                    expressions,
                    stateContext: action.State);
            }

            ValidateCollectionMutations(
                action.CollectionMutations,
                baseSymbols,
                dataFieldKinds,
                collectionFieldMap,
                diagnostics,
                expressions,
                action.State,
                action.SourceLine,
                eventName: null);
        }
    }

    /// <summary>
    /// Validates field/arg-level constraint suffixes for type compatibility (C57),
    /// contradiction/duplicate (C58), and default-value violations (C59).
    /// Runs before <see cref="ValidateRules"/> so errors are attributed to constraints,
    /// not to the synthetic invariants they generate.
    /// </summary>
    private static void ValidateFieldConstraints(
        PreceptDefinition model,
        List<PreceptValidationDiagnostic> diagnostics)
    {
        foreach (var field in model.Fields)
        {
            // SYNC:CONSTRAINT:C62
            // SYNC:CONSTRAINT:C63
            // SYNC:CONSTRAINT:C66
            ValidateChoiceField(field.Name, field.Type, field.ChoiceValues, field.IsOrdered, diagnostics);

            // SYNC:CONSTRAINT:C64
            if (field.Type == PreceptScalarType.Choice &&
                field.HasDefaultValue &&
                field.DefaultValue is string defaultStr &&
                field.ChoiceValues is { Count: > 0 } choiceVals &&
                !choiceVals.Contains(defaultStr, StringComparer.Ordinal))
            {
                diagnostics.Add(new PreceptValidationDiagnostic(
                    DiagnosticCatalog.C64,
                    DiagnosticCatalog.C64.FormatMessage(
                        ("value", defaultStr),
                        ("values", string.Join(", ", choiceVals.Select(v => $"\"{v}\""))),
                        ("name", field.Name)),
                    0));
            }

            if (field.Constraints is not { Count: > 0 }) goto ValidateConstraints;
            // SYNC:CONSTRAINT:C57
            ValidateConstraintTypes(field.Name, field.Type, isCollection: false, field.Constraints, diagnostics);
            // SYNC:CONSTRAINT:C58
            ValidateConstraintDuplicates(field.Name, field.Constraints, diagnostics);
            // SYNC:CONSTRAINT:C59
            if (field.HasDefaultValue)
                ValidateConstraintDefault(field.Name, field.DefaultValue, field.Constraints, diagnostics);

            ValidateConstraints: ;
        }

        foreach (var col in model.CollectionFields)
        {
            // SYNC:CONSTRAINT:C62
            // SYNC:CONSTRAINT:C63
            ValidateChoiceField(col.Name, col.InnerType, col.ChoiceValues, isOrdered: false, diagnostics);

            if (col.Constraints is not { Count: > 0 }) continue;
            // SYNC:CONSTRAINT:C57
            ValidateConstraintTypes(col.Name, null, isCollection: true, col.Constraints, diagnostics);
            // SYNC:CONSTRAINT:C58
            ValidateConstraintDuplicates(col.Name, col.Constraints, diagnostics);
        }

        foreach (var evt in model.Events)
        {
            foreach (var arg in evt.Args)
            {
                // SYNC:CONSTRAINT:C62
                // SYNC:CONSTRAINT:C63
                // SYNC:CONSTRAINT:C66
                ValidateChoiceField(arg.Name, arg.Type, arg.ChoiceValues, arg.IsOrdered, diagnostics);

                if (arg.Constraints is not { Count: > 0 }) continue;
                // SYNC:CONSTRAINT:C57
                ValidateConstraintTypes(arg.Name, arg.Type, isCollection: false, arg.Constraints, diagnostics);
                // SYNC:CONSTRAINT:C58
                ValidateConstraintDuplicates(arg.Name, arg.Constraints, diagnostics);
                // SYNC:CONSTRAINT:C59
                if (arg.HasDefaultValue)
                    ValidateConstraintDefault(arg.Name, arg.DefaultValue, arg.Constraints, diagnostics);
            }
        }
    }

    /// <summary>Validates choice type metadata: C62 (empty set), C63 (duplicates), C66 (ordered on non-choice).</summary>
    private static void ValidateChoiceField(
        string name,
        PreceptScalarType type,
        IReadOnlyList<string>? choiceValues,
        bool isOrdered,
        List<PreceptValidationDiagnostic> diagnostics)
    {
        // SYNC:CONSTRAINT:C66
        if (isOrdered && type != PreceptScalarType.Choice)
        {
            diagnostics.Add(new PreceptValidationDiagnostic(
                DiagnosticCatalog.C66,
                DiagnosticCatalog.C66.FormatMessage(
                    ("name", name),
                    ("type", type.ToString().ToLowerInvariant())),
                0));
        }

        if (type != PreceptScalarType.Choice)
            return;

        // SYNC:CONSTRAINT:C62
        if (choiceValues == null || choiceValues.Count == 0)
        {
            diagnostics.Add(new PreceptValidationDiagnostic(
                DiagnosticCatalog.C62,
                DiagnosticCatalog.C62.FormatMessage(("name", name)),
                0));
            return;
        }

        // SYNC:CONSTRAINT:C63
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var val in choiceValues)
        {
            if (!seen.Add(val))
            {
                diagnostics.Add(new PreceptValidationDiagnostic(
                    DiagnosticCatalog.C63,
                    DiagnosticCatalog.C63.FormatMessage(("value", val), ("name", name)),
                    0));
                break; // one diagnostic per field for duplicate
            }
        }
    }

    private static void ValidateConstraintTypes(
        string name,
        PreceptScalarType? scalarType,
        bool isCollection,
        IReadOnlyList<FieldConstraint> constraints,
        List<PreceptValidationDiagnostic> diagnostics)
    {
        foreach (var c in constraints)
        {
            bool valid = (c, isCollection, scalarType) switch
            {
                // Number-only constraints
                (FieldConstraint.Nonnegative, false, PreceptScalarType.Number) => true,
                (FieldConstraint.Positive,    false, PreceptScalarType.Number) => true,
                (FieldConstraint.Min,         false, PreceptScalarType.Number) => true,
                (FieldConstraint.Max,         false, PreceptScalarType.Number) => true,
                // Integer also accepts numeric range constraints
                (FieldConstraint.Nonnegative, false, PreceptScalarType.Integer) => true,
                (FieldConstraint.Positive,    false, PreceptScalarType.Integer) => true,
                (FieldConstraint.Min,         false, PreceptScalarType.Integer) => true,
                (FieldConstraint.Max,         false, PreceptScalarType.Integer) => true,
                // Decimal accepts numeric range constraints and maxplaces
                (FieldConstraint.Nonnegative, false, PreceptScalarType.Decimal) => true,
                (FieldConstraint.Positive,    false, PreceptScalarType.Decimal) => true,
                (FieldConstraint.Min,         false, PreceptScalarType.Decimal) => true,
                (FieldConstraint.Max,         false, PreceptScalarType.Decimal) => true,
                // Maxplaces only valid on Decimal
                (FieldConstraint.Maxplaces,   false, PreceptScalarType.Decimal) => true,
                // String-only constraints
                (FieldConstraint.Notempty,    false, PreceptScalarType.String) => true,
                (FieldConstraint.Minlength,   false, PreceptScalarType.String) => true,
                (FieldConstraint.Maxlength,   false, PreceptScalarType.String) => true,
                // Collection constraints
                (FieldConstraint.Notempty,    true,  _) => true,
                (FieldConstraint.Mincount,    true,  _) => true,
                (FieldConstraint.Maxcount,    true,  _) => true,
                _ => false
            };

            if (!valid)
            {
                var typeLabel = isCollection ? "collection" : scalarType?.ToString().ToLowerInvariant() ?? "unknown";
                var constraintLabel = ConstraintLabel(c);
                // SYNC:CONSTRAINT:C61
                if (c is FieldConstraint.Maxplaces)
                {
                    diagnostics.Add(new PreceptValidationDiagnostic(
                        DiagnosticCatalog.C61,
                        DiagnosticCatalog.C61.MessageTemplate,
                        0));
                }
                // SYNC:CONSTRAINT:C57
                else
                {
                    diagnostics.Add(new PreceptValidationDiagnostic(
                        DiagnosticCatalog.C57,
                        DiagnosticCatalog.C57.FormatMessage(
                            ("constraint", constraintLabel),
                            ("type", typeLabel)),
                        0));
                }
            }
        }
    }

    private static void ValidateConstraintDuplicates(
        string name,
        IReadOnlyList<FieldConstraint> constraints,
        List<PreceptValidationDiagnostic> diagnostics)
    {
        // Detect same-kind duplicates (e.g., two min constraints regardless of value)
        var kindSeen = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (var c in constraints)
        {
            var kind = ConstraintKindKey(c);
            if (kindSeen.ContainsKey(kind))
            {
                // SYNC:CONSTRAINT:C58
                diagnostics.Add(new PreceptValidationDiagnostic(
                    DiagnosticCatalog.C58,
                    DiagnosticCatalog.C58.FormatMessage(
                        ("message", $"Duplicate constraint '{kind}' on field '{name}'.")),
                    0));
            }
            else
            {
                kindSeen[kind] = true;
            }
        }

        // Detect subsumption: positive is strictly stronger than nonnegative;
        // having both means nonnegative can never fire independently — it is dead code.
        bool hasNonneg = constraints.Any(static c => c is FieldConstraint.Nonnegative);
        bool hasPositive = constraints.Any(static c => c is FieldConstraint.Positive);
        if (hasNonneg && hasPositive)
        {
            // SYNC:CONSTRAINT:C58
            diagnostics.Add(new PreceptValidationDiagnostic(
                DiagnosticCatalog.C58,
                DiagnosticCatalog.C58.FormatMessage(
                    ("message", "Constraint 'nonnegative' is subsumed by 'positive'.")) ,
                0));
        }

        // Detect contradictory range constraints
        double? minVal = null, maxVal = null;
        foreach (var c in constraints)
        {
            if (c is FieldConstraint.Min mn) minVal = mn.Value;
            if (c is FieldConstraint.Max mx) maxVal = mx.Value;
            if (c is FieldConstraint.Mincount mc) minVal = mc.Value;
            if (c is FieldConstraint.Maxcount mc2) maxVal = mc2.Value;
        }
        if (minVal.HasValue && maxVal.HasValue && minVal.Value > maxVal.Value)
        {
            var c1 = constraints.FirstOrDefault(c => c is FieldConstraint.Min or FieldConstraint.Mincount);
            var c2 = constraints.FirstOrDefault(c => c is FieldConstraint.Max or FieldConstraint.Maxcount);
            // SYNC:CONSTRAINT:C58
            diagnostics.Add(new PreceptValidationDiagnostic(
                DiagnosticCatalog.C58,
                DiagnosticCatalog.C58.FormatMessage(
                    ("message", $"Contradictory constraints: '{ConstraintLabel(c1!)}' and '{ConstraintLabel(c2!)}' define an empty valid range.")),
                0));
        }
    }

    private static void ValidateConstraintDefault(
        string name,
        object? defaultValue,
        IReadOnlyList<FieldConstraint> constraints,
        List<PreceptValidationDiagnostic> diagnostics)
    {
        foreach (var c in constraints)
        {
            bool violated = (c, defaultValue) switch
            {
                (FieldConstraint.Nonnegative, double d) => d < 0,
                (FieldConstraint.Positive,    double d) => d <= 0,
                (FieldConstraint.Min mn,      double d) => d < mn.Value,
                (FieldConstraint.Max mx,      double d) => d > mx.Value,
                (FieldConstraint.Nonnegative, long l) => l < 0,
                (FieldConstraint.Positive,    long l) => l <= 0,
                (FieldConstraint.Min mn,      long l) => l < mn.Value,
                (FieldConstraint.Max mx,      long l) => l > mx.Value,
                (FieldConstraint.Notempty,    string s) => s.Length == 0,
                (FieldConstraint.Minlength ml, string s) => s.Length < ml.Value,
                (FieldConstraint.Maxlength ml, string s) => s.Length > ml.Value,
                _ => false
            };

            if (violated)
            {
                var valueLabel = defaultValue switch
                {
                    string s => $"\"{s}\"",
                    long l => l.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    double d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    _ => defaultValue?.ToString() ?? "null"
                };
                // SYNC:CONSTRAINT:C59
                diagnostics.Add(new PreceptValidationDiagnostic(
                    DiagnosticCatalog.C59,
                    DiagnosticCatalog.C59.FormatMessage(
                        ("value", valueLabel),
                        ("constraint", ConstraintLabel(c))),
                    0));
            }
        }
    }

    private static string ConstraintKindKey(FieldConstraint c) => c switch
    {
        FieldConstraint.Nonnegative => "nonnegative",
        FieldConstraint.Positive    => "positive",
        FieldConstraint.Notempty    => "notempty",
        FieldConstraint.Min         => "min",
        FieldConstraint.Max         => "max",
        FieldConstraint.Minlength   => "minlength",
        FieldConstraint.Maxlength   => "maxlength",
        FieldConstraint.Mincount    => "mincount",
        FieldConstraint.Maxcount    => "maxcount",
        FieldConstraint.Maxplaces   => "maxplaces",
        _                           => c.GetType().Name.ToLowerInvariant()
    };

    private static string ConstraintLabel(FieldConstraint c) => c switch
    {
        FieldConstraint.Nonnegative => "nonnegative",
        FieldConstraint.Positive    => "positive",
        FieldConstraint.Notempty    => "notempty",
        FieldConstraint.Min mn      => $"min {mn.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
        FieldConstraint.Max mx      => $"max {mx.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
        FieldConstraint.Minlength ml => $"minlength {ml.Value}",
        FieldConstraint.Maxlength ml => $"maxlength {ml.Value}",
        FieldConstraint.Mincount mc  => $"mincount {mc.Value}",
        FieldConstraint.Maxcount mc  => $"maxcount {mc.Value}",
        FieldConstraint.Maxplaces mp => $"maxplaces {mp.Places}",
        _                           => c.GetType().Name.ToLowerInvariant()
    };

    private static void ValidateRules(
        PreceptDefinition model,
        IReadOnlyDictionary<string, StaticValueKind> dataFieldKinds,
        IReadOnlyDictionary<string, Dictionary<string, StaticValueKind>> eventArgKinds,
        List<PreceptValidationDiagnostic> diagnostics,
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

        foreach (var field in model.Fields)
        {
            if (field.Type == PreceptScalarType.String)
                dataSymbols[$"{field.Name}.length"] = StaticValueKind.Number;
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
                    expressions,
                    isBooleanRulePosition: true);
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
                    stateContext: stateAssert.State,
                    isBooleanRulePosition: true);
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
                    eventName: eventAssert.EventName,
                    isBooleanRulePosition: true);
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
            if (HasFlag(pair.Value, StaticValueKind.String))
            {
                symbols[$"{pair.Key}.length"] = StaticValueKind.Number;
                symbols[$"{eventName}.{pair.Key}.length"] = StaticValueKind.Number;
            }
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
            .Where(static stateAssert => stateAssert.Anchor == AssertAnchor.In)
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
                // Only dotted form (EventName.ArgName) is valid in transition-row scope.
                // Bare arg names are valid only in event-assert scope (see BuildEventAssertSymbols).
                symbols[$"{eventName}.{pair.Key}"] = pair.Value;
                if (HasFlag(pair.Value, StaticValueKind.String))
                    symbols[$"{eventName}.{pair.Key}.length"] = StaticValueKind.Number;
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

        foreach (var pair in dataFieldKinds)
        {
            if (HasFlag(pair.Value, StaticValueKind.String))
                symbols[$"{pair.Key}.length"] = StaticValueKind.Number;
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
        List<PreceptValidationDiagnostic> diagnostics,
        List<PreceptTypeExpressionInfo> expressions,
        string? stateContext = null,
        string? eventName = null,
        bool isBooleanRulePosition = false)
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

        LanguageConstraint constraint;
        string message;
        if (isBooleanRulePosition)
        {
            constraint = DiagnosticCatalog.C46;
            message = $"{expectedLabel} must be a boolean expression, but expression produces {FormatKinds(actualKind)}.";
        }
        else
        {
            constraint = HasFlag(actualKind, StaticValueKind.Null) && !HasFlag(expectedKind, StaticValueKind.Null)
                ? DiagnosticCatalog.C42
                // SYNC:CONSTRAINT:C60
                : IsNarrowingToInteger(actualKind, expectedKind)
                    ? DiagnosticCatalog.C60
                    : DiagnosticCatalog.C39;
            message = constraint == DiagnosticCatalog.C60
                ? DiagnosticCatalog.C60.FormatMessage(("actual", FormatKinds(actualKind)), ("name", expectedLabel))
                : $"{expectedLabel} type mismatch: expected {FormatKinds(expectedKind)} but expression produces {FormatKinds(actualKind)}.";
        }

        expressions.Add(new PreceptTypeExpressionInfo(
            sourceLine,
            expressionText,
            actualKind,
            expectedLabel,
            stateContext,
            eventName));

        diagnostics.Add(new PreceptValidationDiagnostic(
            constraint,
            message,
            sourceLine,
            StateContext: stateContext));
    }

    private static bool TryInferKind(
        PreceptExpression expression,
        IReadOnlyDictionary<string, StaticValueKind> symbols,
        out StaticValueKind kind,
        out PreceptValidationDiagnostic? diagnostic)
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
                var key = identifier.SubMember is not null
                    ? $"{identifier.Name}.{identifier.Member}.{identifier.SubMember}"
                    : identifier.Member is null ? identifier.Name : $"{identifier.Name}.{identifier.Member}";
                if (!symbols.TryGetValue(key, out kind))
                {
                    diagnostic = new PreceptValidationDiagnostic(
                        DiagnosticCatalog.C38,
                        $"unknown identifier '{key}'.",
                        0);
                    return false;
                }

                // C56: .length on a nullable string requires an explicit null guard before access.
                // SYNC:CONSTRAINT:C56
                if (identifier.Member == "length" &&
                    symbols.TryGetValue(identifier.Name, out var baseKind) &&
                    HasFlag(baseKind, StaticValueKind.Null))
                {
                    diagnostic = new PreceptValidationDiagnostic(
                        DiagnosticCatalog.C56,
                        DiagnosticCatalog.C56.FormatMessage(("field", identifier.Name)),
                        0);
                    return false;
                }

                // C56 three-level form: EventName.ArgName.length — nullable arg requires null guard.
                // SYNC:CONSTRAINT:C56
                if (identifier.SubMember == "length")
                {
                    var argKey = $"{identifier.Name}.{identifier.Member}";
                    if (symbols.TryGetValue(argKey, out var argKind) && HasFlag(argKind, StaticValueKind.Null))
                    {
                        diagnostic = new PreceptValidationDiagnostic(
                            DiagnosticCatalog.C56,
                            DiagnosticCatalog.C56.FormatMessage(("field", argKey)),
                            0);
                        return false;
                    }
                }

                return true;
            }

            case PreceptParenthesizedExpression parenthesized:
                return TryInferKind(parenthesized.Inner, symbols, out kind, out diagnostic);

            case PreceptUnaryExpression unary:
            {
                if (!TryInferKind(unary.Operand, symbols, out var operandKind, out diagnostic))
                    return false;

                if (unary.Operator == "not")
                {
                    if (!IsExactly(operandKind, StaticValueKind.Boolean))
                    {
                        diagnostic = new PreceptValidationDiagnostic(
                            DiagnosticCatalog.C40,
                            "operator 'not' requires boolean operand.",
                            0);
                        return false;
                    }

                    kind = StaticValueKind.Boolean;
                    return true;
                }

                if (unary.Operator == "-")
                {
                    if (!IsExactly(operandKind, StaticValueKind.Number) &&
                        !IsExactly(operandKind, StaticValueKind.Integer))
                    {
                        diagnostic = new PreceptValidationDiagnostic(
                            DiagnosticCatalog.C40,
                            "unary '-' requires numeric operand.",
                            0);
                        return false;
                    }

                    kind = operandKind; // preserve Integer or Number
                    return true;
                }

                diagnostic = new PreceptValidationDiagnostic(
                    DiagnosticCatalog.C40,
                    $"unsupported unary operator '{unary.Operator}'.",
                    0);
                return false;
            }

            case PreceptBinaryExpression binary:
                return TryInferBinaryKind(binary, symbols, out kind, out diagnostic);

            case PreceptRoundExpression round:
            {
                if (!TryInferKind(round.Value, symbols, out var innerKind, out diagnostic))
                    return false;

                if (!IsNumericKind(innerKind))
                {
                    diagnostic = new PreceptValidationDiagnostic(
                        DiagnosticCatalog.C40,
                        "round() requires a numeric (integer, decimal, or number) argument.",
                        0);
                    return false;
                }

                if (round.Places < 0)
                {
                    diagnostic = new PreceptValidationDiagnostic(
                        DiagnosticCatalog.C40,
                        "round() places argument must be non-negative.",
                        0);
                    return false;
                }

                kind = StaticValueKind.Decimal;
                return true;
            }

            default:
                diagnostic = new PreceptValidationDiagnostic(
                    DiagnosticCatalog.C39,
                    "unsupported expression node.",
                    0);
                return false;
        }
    }

    private static bool TryInferBinaryKind(
        PreceptBinaryExpression binary,
        IReadOnlyDictionary<string, StaticValueKind> symbols,
        out StaticValueKind kind,
        out PreceptValidationDiagnostic? diagnostic)
    {
        kind = StaticValueKind.None;
        diagnostic = null;

        switch (binary.Operator)
        {
            case "and":
            {
                if (!TryInferKind(binary.Left, symbols, out var leftKind, out diagnostic))
                    return false;

                if (!IsExactly(leftKind, StaticValueKind.Boolean))
                {
                    diagnostic = new PreceptValidationDiagnostic(DiagnosticCatalog.C41, "operator 'and' requires boolean operands.", 0);
                    return false;
                }

                var rightSymbols = ApplyNarrowing(binary.Left, symbols, assumeTrue: true);
                if (!TryInferKind(binary.Right, rightSymbols, out var rightKind, out diagnostic))
                    return false;

                if (!IsExactly(rightKind, StaticValueKind.Boolean))
                {
                    diagnostic = new PreceptValidationDiagnostic(DiagnosticCatalog.C41, "operator 'and' requires boolean operands.", 0);
                    return false;
                }

                kind = StaticValueKind.Boolean;
                return true;
            }

            case "or":
            {
                if (!TryInferKind(binary.Left, symbols, out var leftKind, out diagnostic))
                    return false;

                if (!IsExactly(leftKind, StaticValueKind.Boolean))
                {
                    diagnostic = new PreceptValidationDiagnostic(DiagnosticCatalog.C41, "operator 'or' requires boolean operands.", 0);
                    return false;
                }

                var rightSymbols = ApplyNarrowing(binary.Left, symbols, assumeTrue: false);
                if (!TryInferKind(binary.Right, rightSymbols, out var rightKind, out diagnostic))
                    return false;

                if (!IsExactly(rightKind, StaticValueKind.Boolean))
                {
                    diagnostic = new PreceptValidationDiagnostic(DiagnosticCatalog.C41, "operator 'or' requires boolean operands.", 0);
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

                if (stringCandidate)
                {
                    kind = StaticValueKind.String;
                    return true;
                }

                if (IsNumericKind(leftKind) && IsNumericKind(rightKind))
                {
                    kind = ResolveNumericResultKind(leftKind, rightKind);
                    return true;
                }

                diagnostic = new PreceptValidationDiagnostic(DiagnosticCatalog.C41, "operator '+' requires number+number or string+string.", 0);
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

                if (!IsNumericKind(leftKind) || !IsNumericKind(rightKind))
                {
                    diagnostic = new PreceptValidationDiagnostic(
                        DiagnosticCatalog.C41,
                        $"operator '{binary.Operator}' requires numeric operands.",
                        0);
                    return false;
                }

                kind = binary.Operator is ">" or ">=" or "<" or "<="
                    ? StaticValueKind.Boolean
                    : ResolveNumericResultKind(leftKind, rightKind);
                return true;
            }

            case "==":
            case "!=":
            {
                if (!TryInferKind(binary.Left, symbols, out var leftEqKind, out diagnostic) ||
                    !TryInferKind(binary.Right, symbols, out var rightEqKind, out diagnostic))
                    return false;

                var leftEqFamily = leftEqKind & ~StaticValueKind.Null;
                var rightEqFamily = rightEqKind & ~StaticValueKind.Null;
                var leftIsNull = leftEqKind == StaticValueKind.Null;
                var rightIsNull = rightEqKind == StaticValueKind.Null;

                // null literal compared to a non-nullable operand is always wrong
                if (leftIsNull && !HasFlag(rightEqKind, StaticValueKind.Null))
                {
                    diagnostic = new PreceptValidationDiagnostic(DiagnosticCatalog.C41,
                        $"operator '{binary.Operator}' cannot compare non-nullable {FormatKinds(rightEqKind)} with null.", 0);
                    return false;
                }

                if (rightIsNull && !HasFlag(leftEqKind, StaticValueKind.Null))
                {
                    diagnostic = new PreceptValidationDiagnostic(DiagnosticCatalog.C41,
                        $"operator '{binary.Operator}' cannot compare non-nullable {FormatKinds(leftEqKind)} with null.", 0);
                    return false;
                }

                // Cross-type equality: both sides have non-null scalar families that differ
                // Allow Integer <=> Number mix (widening)
                var isIntNumberMix =
                    (leftEqFamily == StaticValueKind.Integer && rightEqFamily == StaticValueKind.Number) ||
                    (leftEqFamily == StaticValueKind.Number  && rightEqFamily == StaticValueKind.Integer);
                // Allow Decimal <=> Integer mix (widening) and Decimal <=> Decimal
                var isDecimalMix =
                    (leftEqFamily == StaticValueKind.Decimal && rightEqFamily == StaticValueKind.Integer) ||
                    (leftEqFamily == StaticValueKind.Integer && rightEqFamily == StaticValueKind.Decimal) ||
                    (leftEqFamily == StaticValueKind.Decimal && rightEqFamily == StaticValueKind.Decimal);
                if (!isIntNumberMix && !isDecimalMix && leftEqFamily != StaticValueKind.None && rightEqFamily != StaticValueKind.None && leftEqFamily != rightEqFamily)
                {
                    diagnostic = new PreceptValidationDiagnostic(DiagnosticCatalog.C41,
                        $"operator '{binary.Operator}' requires operands of the same type, but found {FormatKinds(leftEqKind)} and {FormatKinds(rightEqKind)}.", 0);
                    return false;
                }

                kind = StaticValueKind.Boolean;
                return true;
            }

            case "contains":
            {
                if (binary.Left is not PreceptIdentifierExpression { Member: null } collectionIdentifier)
                {
                    diagnostic = new PreceptValidationDiagnostic(DiagnosticCatalog.C41, "'contains' requires a collection field on the left side.", 0);
                    return false;
                }

                if (!TryInferKind(binary.Right, symbols, out var rightKind, out diagnostic))
                    return false;

                var collectionKey = $"{collectionIdentifier.Name}.count";
                if (!symbols.ContainsKey(collectionKey))
                {
                    diagnostic = new PreceptValidationDiagnostic(DiagnosticCatalog.C38, $"unknown identifier '{collectionIdentifier.Name}'.", 0);
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
                    diagnostic = new PreceptValidationDiagnostic(
                        DiagnosticCatalog.C41,
                        $"operator 'contains' requires RHS of type {FormatKinds(innerKind)} but expression produces {FormatKinds(rightKind)}.",
                        0);
                    return false;
                }

                kind = StaticValueKind.Boolean;
                return true;
            }

            default:
                diagnostic = new PreceptValidationDiagnostic(DiagnosticCatalog.C41, $"unsupported binary operator '{binary.Operator}'.", 0);
                return false;
        }
    }

    private static IReadOnlyDictionary<string, StaticValueKind> ApplyNarrowing(
        PreceptExpression expression,
        IReadOnlyDictionary<string, StaticValueKind> symbols,
        bool assumeTrue)
    {
        expression = StripParentheses(expression);

        if (expression is PreceptUnaryExpression { Operator: "not" } unary)
            return ApplyNarrowing(unary.Operand, symbols, !assumeTrue);

        if (expression is not PreceptBinaryExpression binary)
            return symbols;

        if (binary.Operator == "and")
        {
            if (!assumeTrue)
                return symbols;

            var leftNarrowed = ApplyNarrowing(binary.Left, symbols, assumeTrue: true);
            return ApplyNarrowing(binary.Right, leftNarrowed, assumeTrue: true);
        }

        if (binary.Operator == "or")
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
        List<PreceptValidationDiagnostic> diagnostics,
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

                    diagnostics.Add(new PreceptValidationDiagnostic(
                        DiagnosticCatalog.C43,
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
        PreceptScalarType.Integer => StaticValueKind.Integer,
        PreceptScalarType.Decimal => StaticValueKind.Decimal,
        PreceptScalarType.Choice => StaticValueKind.String,  // choice values are strings at runtime
        _ => StaticValueKind.None
    };

    private static StaticValueKind MapLiteralKind(object? value) => value switch
    {
        null => StaticValueKind.Null,
        string => StaticValueKind.String,
        bool => StaticValueKind.Boolean,
        long => StaticValueKind.Integer,
        byte or sbyte or short or ushort or int or uint or ulong or float or double or decimal => StaticValueKind.Number,
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

        // Integer widens to Number and Decimal (explicit widening rules from #29)
        if (actualNonNull == StaticValueKind.Integer &&
            (expectedNonNull == StaticValueKind.Number || expectedNonNull == StaticValueKind.Decimal))
            return true;

        if ((actualNonNull & ~expectedNonNull) != StaticValueKind.None)
            return false;

        if (actual == StaticValueKind.Null)
            return HasFlag(expected, StaticValueKind.Null);

        return true;
    }

    private static bool HasFlag(StaticValueKind kind, StaticValueKind flag)
        => (kind & flag) == flag;

    /// <summary>Returns true when <paramref name="k"/> is a pure numeric kind (Number, Integer, or Decimal, non-nullable, no other flags).</summary>
    private static bool IsNumericKind(StaticValueKind k)
        => IsExactly(k, StaticValueKind.Number) || IsExactly(k, StaticValueKind.Integer) || IsExactly(k, StaticValueKind.Decimal);

    /// <summary>
    /// Resolves the result kind for a numeric binary operation.
    /// Integer × Integer → Integer; Decimal × Decimal|×Integer → Decimal; anything involving Number → Number.
    /// </summary>
    private static StaticValueKind ResolveNumericResultKind(StaticValueKind left, StaticValueKind right)
    {
        if (IsExactly(left, StaticValueKind.Integer) && IsExactly(right, StaticValueKind.Integer))
            return StaticValueKind.Integer;
        if ((IsExactly(left, StaticValueKind.Decimal) || IsExactly(left, StaticValueKind.Integer)) &&
            (IsExactly(right, StaticValueKind.Decimal) || IsExactly(right, StaticValueKind.Integer)))
            return StaticValueKind.Decimal;
        return StaticValueKind.Number;
    }

    /// <summary>Returns true when assigning a Number or Decimal to an Integer target (narrowing, requires explicit conversion).</summary>
    private static bool IsNarrowingToInteger(StaticValueKind actual, StaticValueKind expected)
    {
        var expectedNonNull = expected & ~StaticValueKind.Null;
        var actualNonNull = actual & ~StaticValueKind.Null;
        return expectedNonNull == StaticValueKind.Integer &&
               (actualNonNull == StaticValueKind.Number || actualNonNull == StaticValueKind.Decimal);
    }
}