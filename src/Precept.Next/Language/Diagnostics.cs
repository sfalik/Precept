using Precept.Pipeline;

namespace Precept.Language;

public sealed record DiagnosticMeta(
    string          Code,
    DiagnosticStage Stage,
    Severity        Severity,
    string          MessageTemplate
);

public static class Diagnostics
{
    public static DiagnosticMeta GetMeta(DiagnosticCode code) => code switch
    {
        DiagnosticCode.InputTooLarge                 => new(nameof(DiagnosticCode.InputTooLarge),                 DiagnosticStage.Lex,   Severity.Error,   "This definition exceeds the 65,536-character security limit and cannot be processed"),
        DiagnosticCode.UnterminatedStringLiteral      => new(nameof(DiagnosticCode.UnterminatedStringLiteral),      DiagnosticStage.Lex,   Severity.Error,   "Text value opened with \" is missing its closing quote — every \" must have a matching \""),
        DiagnosticCode.UnterminatedTypedConstant       => new(nameof(DiagnosticCode.UnterminatedTypedConstant),       DiagnosticStage.Lex,   Severity.Error,   "Value opened with ' is missing its closing quote — every ' must have a matching '"),
        DiagnosticCode.UnterminatedInterpolation       => new(nameof(DiagnosticCode.UnterminatedInterpolation),       DiagnosticStage.Lex,   Severity.Error,   "The {{ }} section is not closed — add a closing }} on the same line"),
        DiagnosticCode.InvalidCharacter                  => new(nameof(DiagnosticCode.InvalidCharacter),                  DiagnosticStage.Lex,   Severity.Error,   "'{0}' is not a valid character in a precept definition — remove or replace it"),
        DiagnosticCode.UnrecognizedStringEscape          => new(nameof(DiagnosticCode.UnrecognizedStringEscape),          DiagnosticStage.Lex,   Severity.Error,   "'\\{0}' is not a valid escape in a text value — use \\\" for a quote, \\\\ for a backslash, \\n for a newline, or \\t for a tab"),
        DiagnosticCode.UnrecognizedTypedConstantEscape   => new(nameof(DiagnosticCode.UnrecognizedTypedConstantEscape),   DiagnosticStage.Lex,   Severity.Error,   "'\\{0}' is not a valid escape in a single-quoted value — use \\' for a quote, or \\\\ for a backslash"),
        DiagnosticCode.UnescapedBraceInLiteral           => new(nameof(DiagnosticCode.UnescapedBraceInLiteral),           DiagnosticStage.Lex,   Severity.Error,   "Use '}}}}' to include a literal }} in this value — a single }} starts a field reference"),
        DiagnosticCode.ExpectedToken                  => new(nameof(DiagnosticCode.ExpectedToken),                  DiagnosticStage.Parse, Severity.Error,   "Expected {0} here, but found '{1}'"),
        DiagnosticCode.UnexpectedKeyword              => new(nameof(DiagnosticCode.UnexpectedKeyword),              DiagnosticStage.Parse, Severity.Error,   "'{0}' cannot appear inside a {1}"),
        DiagnosticCode.NonAssociativeComparison         => new(nameof(DiagnosticCode.NonAssociativeComparison),         DiagnosticStage.Parse, Severity.Error,   "Comparisons like == and < cannot be chained — {0}"),
        DiagnosticCode.InvalidCallTarget                => new(nameof(DiagnosticCode.InvalidCallTarget),                DiagnosticStage.Parse, Severity.Error,   "Only built-in functions can be called this way — '{0}' is not a function name"),
        DiagnosticCode.MutuallyExclusiveQualifiers       => new(nameof(DiagnosticCode.MutuallyExclusiveQualifiers),       DiagnosticStage.Parse, Severity.Error,   "'in' and 'of' cannot both appear on the same field declaration"),
        DiagnosticCode.UndeclaredField                => new(nameof(DiagnosticCode.UndeclaredField),                DiagnosticStage.Type,  Severity.Error,   "Field '{0}' is not declared"),
        DiagnosticCode.TypeMismatch                   => new(nameof(DiagnosticCode.TypeMismatch),                   DiagnosticStage.Type,  Severity.Error,   "Expected a {0} value here, but got '{1}'"),
        DiagnosticCode.NullInNonNullableContext       => new(nameof(DiagnosticCode.NullInNonNullableContext),       DiagnosticStage.Type,  Severity.Error,   "'{0}' requires a value and cannot be empty here"),
        DiagnosticCode.InvalidMemberAccess            => new(nameof(DiagnosticCode.InvalidMemberAccess),            DiagnosticStage.Type,  Severity.Error,   "'.{0}' is not available on {1} fields"),
        DiagnosticCode.FunctionArityMismatch          => new(nameof(DiagnosticCode.FunctionArityMismatch),          DiagnosticStage.Type,  Severity.Error,   "'{0}' takes {1} inputs, but {2} were provided"),
        DiagnosticCode.FunctionArgConstraintViolation => new(nameof(DiagnosticCode.FunctionArgConstraintViolation), DiagnosticStage.Type,  Severity.Error,   "Value {0} for '{1}' is not valid: {2}"),
        DiagnosticCode.DuplicateFieldName             => new(nameof(DiagnosticCode.DuplicateFieldName),             DiagnosticStage.Type,  Severity.Error,   "Field '{0}' is already declared"),
        DiagnosticCode.DuplicateStateName             => new(nameof(DiagnosticCode.DuplicateStateName),             DiagnosticStage.Type,  Severity.Error,   "State '{0}' is already declared"),
        DiagnosticCode.DuplicateEventName             => new(nameof(DiagnosticCode.DuplicateEventName),             DiagnosticStage.Type,  Severity.Error,   "Event '{0}' is already declared"),
        DiagnosticCode.DuplicateArgName               => new(nameof(DiagnosticCode.DuplicateArgName),               DiagnosticStage.Type,  Severity.Error,   "Argument '{0}' is already declared on event '{1}'"),
        DiagnosticCode.UndeclaredState                => new(nameof(DiagnosticCode.UndeclaredState),                DiagnosticStage.Type,  Severity.Error,   "State '{0}' is not declared"),
        DiagnosticCode.UndeclaredEvent                => new(nameof(DiagnosticCode.UndeclaredEvent),                DiagnosticStage.Type,  Severity.Error,   "Event '{0}' is not declared"),
        DiagnosticCode.UndeclaredFunction             => new(nameof(DiagnosticCode.UndeclaredFunction),             DiagnosticStage.Type,  Severity.Error,   "'{0}' is not a recognized function"),
        DiagnosticCode.MultipleInitialStates          => new(nameof(DiagnosticCode.MultipleInitialStates),          DiagnosticStage.Type,  Severity.Error,   "Only one state can be marked 'initial' — '{0}' and '{1}' both are"),
        DiagnosticCode.NoInitialState                 => new(nameof(DiagnosticCode.NoInitialState),                 DiagnosticStage.Type,  Severity.Error,   "This precept has states but none is marked 'initial'"),
        DiagnosticCode.InvalidModifierForType         => new(nameof(DiagnosticCode.InvalidModifierForType),         DiagnosticStage.Type,  Severity.Error,   "The '{0}' constraint does not apply to {1} fields"),
        DiagnosticCode.InvalidModifierBounds          => new(nameof(DiagnosticCode.InvalidModifierBounds),          DiagnosticStage.Type,  Severity.Error,   "{0} ({1}) cannot exceed {2} ({3})"),
        DiagnosticCode.InvalidModifierValue           => new(nameof(DiagnosticCode.InvalidModifierValue),           DiagnosticStage.Type,  Severity.Error,   "The value for '{0}' must be {1}"),
        DiagnosticCode.DuplicateModifier              => new(nameof(DiagnosticCode.DuplicateModifier),              DiagnosticStage.Type,  Severity.Error,   "The '{0}' constraint is already applied to this field"),
        DiagnosticCode.RedundantModifier              => new(nameof(DiagnosticCode.RedundantModifier),              DiagnosticStage.Type,  Severity.Warning, "'{0}' is unnecessary — '{1}' already implies it"),
        DiagnosticCode.ComputedFieldNotWritable       => new(nameof(DiagnosticCode.ComputedFieldNotWritable),       DiagnosticStage.Type,  Severity.Error,   "Field '{0}' is computed and cannot be assigned"),
        DiagnosticCode.ComputedFieldWithDefault       => new(nameof(DiagnosticCode.ComputedFieldWithDefault),       DiagnosticStage.Type,  Severity.Error,   "Field '{0}' is computed and cannot have a default value"),
        DiagnosticCode.CircularComputedField          => new(nameof(DiagnosticCode.CircularComputedField),          DiagnosticStage.Type,  Severity.Error,   "Computed field '{0}' has a circular dependency: {1}"),
        DiagnosticCode.ConflictingAccessModes         => new(nameof(DiagnosticCode.ConflictingAccessModes),         DiagnosticStage.Type,  Severity.Error,   "Field '{0}' has conflicting access modes in state '{1}'"),
        DiagnosticCode.ListLiteralOutsideDefault      => new(nameof(DiagnosticCode.ListLiteralOutsideDefault),      DiagnosticStage.Type,  Severity.Error,   "List values can only appear in default clauses"),
        DiagnosticCode.DuplicateChoiceValue           => new(nameof(DiagnosticCode.DuplicateChoiceValue),           DiagnosticStage.Type,  Severity.Error,   "Choice value '{0}' is duplicated"),
        DiagnosticCode.EmptyChoice                    => new(nameof(DiagnosticCode.EmptyChoice),                    DiagnosticStage.Type,  Severity.Error,   "A choice type must have at least one value"),
        DiagnosticCode.CollectionOperationOnScalar    => new(nameof(DiagnosticCode.CollectionOperationOnScalar),    DiagnosticStage.Type,  Severity.Error,   "'{0}' is a {1} operation, but '{2}' is not a {1}"),
        DiagnosticCode.ScalarOperationOnCollection    => new(nameof(DiagnosticCode.ScalarOperationOnCollection),    DiagnosticStage.Type,  Severity.Error,   "'{0}' cannot be used with collection field '{1}'"),
        DiagnosticCode.IsSetOnNonOptional             => new(nameof(DiagnosticCode.IsSetOnNonOptional),             DiagnosticStage.Type,  Severity.Error,   "'{0}' always has a value — 'is set' only works on optional fields"),
        DiagnosticCode.EventArgOutOfScope             => new(nameof(DiagnosticCode.EventArgOutOfScope),             DiagnosticStage.Type,  Severity.Error,   "Event '{0}' arguments are not accessible here"),
        DiagnosticCode.InvalidInterpolationCoercion   => new(nameof(DiagnosticCode.InvalidInterpolationCoercion),   DiagnosticStage.Type,  Severity.Error,   "A {0} value cannot appear inside a text interpolation"),
        DiagnosticCode.UnresolvedTypedConstant        => new(nameof(DiagnosticCode.UnresolvedTypedConstant),        DiagnosticStage.Type,  Severity.Error,   "Cannot determine the type of '{0}' — no type context available"),
        DiagnosticCode.InvalidTypedConstantContent    => new(nameof(DiagnosticCode.InvalidTypedConstantContent),    DiagnosticStage.Type,  Severity.Error,   "'{0}' is not a valid {1} value"),
        DiagnosticCode.DefaultForwardReference        => new(nameof(DiagnosticCode.DefaultForwardReference),        DiagnosticStage.Type,  Severity.Error,   "Default value for '{0}' cannot reference '{1}', which is declared later"),
        DiagnosticCode.InvalidDateValue               => new(nameof(DiagnosticCode.InvalidDateValue),               DiagnosticStage.Type,  Severity.Error,   "Invalid date: {0} does not exist"),
        DiagnosticCode.InvalidDateFormat              => new(nameof(DiagnosticCode.InvalidDateFormat),              DiagnosticStage.Type,  Severity.Error,   "Dates must be written as YYYY-MM-DD. Use '{0}'"),
        DiagnosticCode.InvalidTimeValue               => new(nameof(DiagnosticCode.InvalidTimeValue),               DiagnosticStage.Type,  Severity.Error,   "Invalid time: {0} must be 0–23 for hours, 0–59 for minutes and seconds"),
        DiagnosticCode.InvalidInstantFormat           => new(nameof(DiagnosticCode.InvalidInstantFormat),           DiagnosticStage.Type,  Severity.Error,   "Instants must end with Z to indicate UTC. Use '{0}Z'"),
        DiagnosticCode.InvalidTimezoneId              => new(nameof(DiagnosticCode.InvalidTimezoneId),              DiagnosticStage.Type,  Severity.Error,   "'{0}' is not a recognized timezone — use canonical IANA form like 'America/New_York'"),
        DiagnosticCode.UnqualifiedPeriodArithmetic    => new(nameof(DiagnosticCode.UnqualifiedPeriodArithmetic),    DiagnosticStage.Type,  Severity.Error,   "Period field '{0}' may contain {1} components — use period of '{2}' to constrain it"),
        DiagnosticCode.MissingTemporalUnit            => new(nameof(DiagnosticCode.MissingTemporalUnit),            DiagnosticStage.Type,  Severity.Error,   "A bare number doesn't specify a unit. Use '{0}' + '{1}' to add {1}"),
        DiagnosticCode.FractionalUnitValue            => new(nameof(DiagnosticCode.FractionalUnitValue),            DiagnosticStage.Type,  Severity.Error,   "Unit values must be whole numbers. Use smaller units for fractions: '{0}'"),
        DiagnosticCode.UnguardedCollectionAccess      => new(nameof(DiagnosticCode.UnguardedCollectionAccess),      DiagnosticStage.Type,  Severity.Error,   "'{0}' may be empty — guard with if {0}.count > 0 before accessing .{1}"),
        DiagnosticCode.UnguardedCollectionMutation    => new(nameof(DiagnosticCode.UnguardedCollectionMutation),    DiagnosticStage.Type,  Severity.Error,   "'{0}' may be empty — guard with if {0}.count > 0 before {1}"),
        DiagnosticCode.NonOrderableCollectionExtreme  => new(nameof(DiagnosticCode.NonOrderableCollectionExtreme),  DiagnosticStage.Type,  Severity.Error,   "'.{1}' requires an orderable element type — '{0}' elements have no natural ordering"),
        DiagnosticCode.CaseInsensitiveStringOnNonCollection => new(nameof(DiagnosticCode.CaseInsensitiveStringOnNonCollection), DiagnosticStage.Type, Severity.Error, "~string is only valid as a collection inner type — use string for field declarations"),
        DiagnosticCode.MaxPlacesExceeded              => new(nameof(DiagnosticCode.MaxPlacesExceeded),              DiagnosticStage.Type,  Severity.Error,   "Value has {0} decimal places, but field '{1}' allows at most {2}"),
        DiagnosticCode.QualifierMismatch              => new(nameof(DiagnosticCode.QualifierMismatch),              DiagnosticStage.Type,  Severity.Error,   "Value does not match the '{0}' qualifier on field '{1}'"),
        DiagnosticCode.DimensionCategoryMismatch      => new(nameof(DiagnosticCode.DimensionCategoryMismatch),      DiagnosticStage.Type,  Severity.Error,   "Dimension '{0}' does not match the declared category '{1}' on field '{2}'"),
        DiagnosticCode.CrossCurrencyArithmetic        => new(nameof(DiagnosticCode.CrossCurrencyArithmetic),        DiagnosticStage.Type,  Severity.Error,   "Cannot combine '{0}' ({1}) with '{2}' ({3}) — different currencies"),
        DiagnosticCode.CrossDimensionArithmetic       => new(nameof(DiagnosticCode.CrossDimensionArithmetic),       DiagnosticStage.Type,  Severity.Error,   "Cannot combine '{0}' ({1}) with '{2}' ({3}) — incompatible dimensions"),
        DiagnosticCode.DenominatorUnitMismatch        => new(nameof(DiagnosticCode.DenominatorUnitMismatch),        DiagnosticStage.Type,  Severity.Error,   "Denominator unit '{0}' does not match operand unit '{1}'"),
        DiagnosticCode.DurationDenominatorMismatch    => new(nameof(DiagnosticCode.DurationDenominatorMismatch),    DiagnosticStage.Type,  Severity.Error,   "Duration cannot cancel '{0}' denominator — days, weeks, months, and years have variable length"),
        DiagnosticCode.CompoundPeriodDenominator      => new(nameof(DiagnosticCode.CompoundPeriodDenominator),      DiagnosticStage.Type,  Severity.Error,   "Compound period '{0}' cannot cancel single-unit denominator '{1}'"),
        DiagnosticCode.InvalidUnitString              => new(nameof(DiagnosticCode.InvalidUnitString),              DiagnosticStage.Type,  Severity.Error,   "'{0}' is not a valid unit"),
        DiagnosticCode.InvalidCurrencyCode            => new(nameof(DiagnosticCode.InvalidCurrencyCode),            DiagnosticStage.Type,  Severity.Error,   "'{0}' is not a recognized ISO 4217 currency code"),
        DiagnosticCode.InvalidDimensionString         => new(nameof(DiagnosticCode.InvalidDimensionString),         DiagnosticStage.Type,  Severity.Error,   "'{0}' is not a recognized dimension"),
        DiagnosticCode.UnreachableState               => new(nameof(DiagnosticCode.UnreachableState),               DiagnosticStage.Graph, Severity.Warning, "State '{0}' is unreachable from initial state '{1}'"),
        DiagnosticCode.UnhandledEvent                 => new(nameof(DiagnosticCode.UnhandledEvent),                 DiagnosticStage.Graph, Severity.Warning, "No transition handles event '{0}' in state '{1}' — firing it will always be rejected"),
        DiagnosticCode.UnsatisfiableGuard             => new(nameof(DiagnosticCode.UnsatisfiableGuard),             DiagnosticStage.Proof, Severity.Warning, "The condition '{0}' on event '{1}' can never be true when {2} — this transition will never fire"),
        DiagnosticCode.DivisionByZero                 => new(nameof(DiagnosticCode.DivisionByZero),                 DiagnosticStage.Proof, Severity.Error,   "Division by zero: '{0}' can be zero when {1}"),
        DiagnosticCode.SqrtOfNegative                 => new(nameof(DiagnosticCode.SqrtOfNegative),                 DiagnosticStage.Proof, Severity.Error,   "sqrt() requires a non-negative value, but '{0}' can be negative when {1}"),
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, null),
    };

    public static Diagnostic Create(DiagnosticCode code, SourceSpan span, params object?[] args)
    {
        var meta = GetMeta(code);
        return new(meta.Severity, meta.Stage, meta.Code, string.Format(meta.MessageTemplate, args), span);
    }

    public static IReadOnlyList<DiagnosticMeta> All { get; } =
        Enum.GetValues<DiagnosticCode>().Select(GetMeta).ToList();
}
