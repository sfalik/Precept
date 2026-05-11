using System;
using System.Collections.Generic;
using System.Linq;

namespace Precept;

// Partial-class file for PreceptTypeChecker — field-constraint validation.
// Contains self-contained constraint validation methods with no dependencies on
// Narrowing or ProofChecks methods.
internal static partial class PreceptTypeChecker
{
    /// <summary>
    /// Validates field/arg-level constraint suffixes for type compatibility (C57),
    /// contradiction/duplicate (C58), and default-value violations (C59).
    /// Runs before <see cref="ValidateRules"/> so errors are attributed to constraints,
    /// not to the synthetic rules they generate.
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
            ValidateChoiceField(field.Name, field.Type, field.ChoiceValues, field.IsOrdered, field.SourceLine, diagnostics);

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
                    field.SourceLine));
            }

            if (field.Constraints is not { Count: > 0 }) goto ValidateConstraints;
            // SYNC:CONSTRAINT:C57
            ValidateConstraintTypes(field.Name, field.Type, isCollection: false, field.Constraints, field.SourceLine, diagnostics);
            // SYNC:CONSTRAINT:C58
            ValidateConstraintDuplicates(field.Name, field.Constraints, field.SourceLine, diagnostics);
            // SYNC:CONSTRAINT:C59
            if (field.HasDefaultValue)
                ValidateConstraintDefault(field.Name, field.DefaultValue, field.Constraints, field.SourceLine, diagnostics);

            ValidateConstraints: ;
        }

        foreach (var col in model.CollectionFields)
        {
            // SYNC:CONSTRAINT:C62
            // SYNC:CONSTRAINT:C63
            ValidateChoiceField(col.Name, col.InnerType, col.ChoiceValues, isOrdered: false, col.SourceLine, diagnostics);

            if (col.Constraints is not { Count: > 0 }) continue;
            // SYNC:CONSTRAINT:C57
            ValidateConstraintTypes(col.Name, null, isCollection: true, col.Constraints, col.SourceLine, diagnostics);
            // SYNC:CONSTRAINT:C58
            ValidateConstraintDuplicates(col.Name, col.Constraints, col.SourceLine, diagnostics);
        }

        foreach (var evt in model.Events)
        {
            foreach (var arg in evt.Args)
            {
                // SYNC:CONSTRAINT:C62
                // SYNC:CONSTRAINT:C63
                // SYNC:CONSTRAINT:C66
                ValidateChoiceField(arg.Name, arg.Type, arg.ChoiceValues, arg.IsOrdered, arg.SourceLine, diagnostics);

                if (arg.Constraints is not { Count: > 0 }) continue;
                // SYNC:CONSTRAINT:C57
                ValidateConstraintTypes(arg.Name, arg.Type, isCollection: false, arg.Constraints, arg.SourceLine, diagnostics);
                // SYNC:CONSTRAINT:C58
                ValidateConstraintDuplicates(arg.Name, arg.Constraints, arg.SourceLine, diagnostics);
                // SYNC:CONSTRAINT:C59
                if (arg.HasDefaultValue)
                    ValidateConstraintDefault(arg.Name, arg.DefaultValue, arg.Constraints, arg.SourceLine, diagnostics);
            }
        }
    }

    /// <summary>Validates choice type metadata: C62 (empty set), C63 (duplicates), C66 (ordered on non-choice).</summary>
    private static void ValidateChoiceField(
        string name,
        PreceptScalarType type,
        IReadOnlyList<string>? choiceValues,
        bool isOrdered,
        int sourceLine,
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
                sourceLine));
        }

        if (type != PreceptScalarType.Choice)
            return;

        // SYNC:CONSTRAINT:C62
        if (choiceValues == null || choiceValues.Count == 0)
        {
            diagnostics.Add(new PreceptValidationDiagnostic(
                DiagnosticCatalog.C62,
                DiagnosticCatalog.C62.FormatMessage(("name", name)),
                sourceLine));
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
                    sourceLine));
                break; // one diagnostic per field for duplicate
            }
        }
    }

    private static void ValidateConstraintTypes(
        string name,
        PreceptScalarType? scalarType,
        bool isCollection,
        IReadOnlyList<FieldConstraint> constraints,
        int sourceLine,
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
                        sourceLine));
                }
                // SYNC:CONSTRAINT:C57
                else
                {
                    diagnostics.Add(new PreceptValidationDiagnostic(
                        DiagnosticCatalog.C57,
                        DiagnosticCatalog.C57.FormatMessage(
                            ("constraint", constraintLabel),
                            ("type", typeLabel)),
                        sourceLine));
                }
            }
        }
    }

    private static void ValidateConstraintDuplicates(
        string name,
        IReadOnlyList<FieldConstraint> constraints,
        int sourceLine,
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
                    sourceLine));
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
                sourceLine));
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
                sourceLine));
        }
    }

    private static void ValidateConstraintDefault(
        string name,
        object? defaultValue,
        IReadOnlyList<FieldConstraint> constraints,
        int sourceLine,
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
                    sourceLine));
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
}
