using FluentAssertions;
using Precept.Language;
using Xunit;
using static Precept.Tests.TypeChecker.TypeCheckerTestHelpers;

namespace Precept.Tests.TypeChecker;

/// <summary>
/// Enforces the catalog-declared <see cref="ActionMeta.ApplicableTo"/> contract for every
/// state-machine action verb. Verifies PRE0047 (CollectionOperationOnScalar) fires on
/// scalar targets where the action expects a collection, and PRE0048 (ScalarOperationOnCollection)
/// fires on wrong-kind collection targets.
/// </summary>
public class ActionApplicabilityTests
{
    // ── Positive cases: target matches Applicability ─────────────────────────

    [Theory]
    [InlineData("set of string",          "add", "string")]
    [InlineData("bag of string",          "add", "string")]
    [InlineData("queue of string",        "enqueue", "string")]
    [InlineData("stack of string",        "push", "string")]
    [InlineData("log of string",          "append", "string")]
    [InlineData("list of string",         "append", "string")]
    public void Action_AppliesToTarget_NoApplicabilityDiagnostic(
        string fieldType, string actionVerb, string argType)
    {
        var precept = $$"""
            precept Widget
            field MyField as {{fieldType}}
            state Open initial
            state Done terminal
            event Trigger(Value as {{argType}})
            from Open on Trigger -> {{actionVerb}} MyField Trigger.Value -> transition Done
            """;

        var (_, diagnostics) = Check(precept);
        diagnostics.Should().NotContain(
            d => d.Code == nameof(DiagnosticCode.CollectionOperationOnScalar)
              || d.Code == nameof(DiagnosticCode.ScalarOperationOnCollection),
            because: $"'{actionVerb}' on a {fieldType} is catalog-applicable");
    }

    // ── Negative cases: collection action on scalar field → PRE0047 ──────────

    [Theory]
    [InlineData("string optional", "add",     "string")]
    [InlineData("integer",         "enqueue", "integer")]
    [InlineData("string optional", "push",    "string")]
    [InlineData("string optional", "append",  "string")]
    public void CollectionAction_OnScalarField_EmitsPRE0047(
        string fieldType, string actionVerb, string argType)
    {
        var precept = $$"""
            precept Widget
            field MyField as {{fieldType}}
            state Open initial
            state Done terminal
            event Trigger(Value as {{argType}})
            from Open on Trigger -> {{actionVerb}} MyField Trigger.Value -> transition Done
            """;

        var (_, diagnostics) = Check(precept);
        diagnostics.Should().Contain(
            d => d.Code == nameof(DiagnosticCode.CollectionOperationOnScalar),
            because: $"'{actionVerb}' requires a collection but {fieldType} is scalar");
    }

    // ── Negative cases: wrong-kind collection target → PRE0048 ───────────────

    [Theory]
    [InlineData("set of string",     "enqueue", "string")]  // enqueue wants Queue, got Set
    [InlineData("set of string",     "append",  "string")]  // append wants Log/List, got Set
    [InlineData("set of string",     "push",    "string")]  // push wants Stack, got Set
    [InlineData("set of string",     "insert",  "string")]  // insert wants List, got Set
    [InlineData("queue of string",   "add",     "string")]  // add wants Set/Bag, got Queue
    [InlineData("queue of string",   "append",  "string")]  // append wants Log/List, got Queue
    [InlineData("queue of string",   "push",    "string")]  // push wants Stack, got Queue
    [InlineData("list of string",    "add",     "string")]  // add wants Set/Bag, got List
    [InlineData("list of string",    "enqueue", "string")]  // enqueue wants Queue, got List
    public void Action_OnWrongCollectionKind_EmitsPRE0048(
        string fieldType, string actionVerb, string argType)
    {
        var precept = $$"""
            precept Widget
            field MyField as {{fieldType}}
            state Open initial
            state Done terminal
            event Trigger(Value as {{argType}})
            from Open on Trigger -> {{actionVerb}} MyField Trigger.Value -> transition Done
            """;

        var (_, diagnostics) = Check(precept);
        diagnostics.Should().Contain(
            d => d.Code == nameof(DiagnosticCode.ScalarOperationOnCollection),
            because: $"'{actionVerb}' cannot be used with collection field of kind {fieldType}");
    }

    // ── Clear ApplicableTo coverage ──────────────────────────────────────────

    [Theory]
    [InlineData("set of string")]
    [InlineData("queue of string")]
    [InlineData("stack of string")]
    [InlineData("bag of string")]
    [InlineData("list of string")]
    [InlineData("queue of string by integer")]
    [InlineData("lookup of string to integer")]
    [InlineData("string optional")]
    [InlineData("integer optional")]
    public void Clear_OnApplicableTarget_NoApplicabilityDiagnostic(string fieldType)
    {
        var precept = $$"""
            precept Widget
            field MyField as {{fieldType}}
            state Open initial
            state Done terminal
            event Trigger
            from Open on Trigger -> clear MyField -> transition Done
            """;

        var (_, diagnostics) = Check(precept);
        diagnostics.Should().NotContain(
            d => d.Code == nameof(DiagnosticCode.CollectionOperationOnScalar)
              || d.Code == nameof(DiagnosticCode.ScalarOperationOnCollection),
            because: $"clear on a {fieldType} is catalog-applicable");
    }

    [Fact]
    public void Clear_OnLookup_CompilesClean()
    {
        // `clear MyLookup` drops all key-value entries — comparable to Java Map.clear(),
        // C# IDictionary.Clear(), Python dict.clear(), Go clear(map). The spec previously
        // forbade this with no idiomatic alternative; the lift restores catalog uniformity.
        var precept = """
            precept Widget
            field MyMap as lookup of string to integer
            state Open initial
            state Done terminal
            event Trigger
            from Open on Trigger -> clear MyMap -> transition Done
            """;

        var (_, diagnostics) = Check(precept);
        diagnostics.Should().NotContain(
            d => d.Code == nameof(DiagnosticCode.CollectionOperationOnScalar)
              || d.Code == nameof(DiagnosticCode.ScalarOperationOnCollection),
            because: "clear is now valid on lookup");
    }

    [Theory]
    [InlineData("lookup of string to integer")]
    [InlineData("lookup of string to string")]
    [InlineData("lookup of integer to integer")]
    public void Mincount_OnLookup_CompilesClean(string fieldType)
    {
        // Collection cardinality ("the lookup has ≥1 entry") is `mincount 1`, valid on every
        // collection kind including lookup. (`notempty` is now string-only — it constrains the
        // value element, not the collection; see Notempty_OnLookup_ValueType below.)
        var precept = $$"""
            precept Widget
            field MyMap as {{fieldType}} mincount 1
            state Open initial
            """;

        var (_, diagnostics) = Check(precept);
        diagnostics.Should().NotContain(
            d => d.Code == nameof(DiagnosticCode.InvalidModifierForType),
            because: $"mincount applies to every collection kind, including {fieldType}");
    }

    [Theory]
    [InlineData("lookup of string to string", false)]   // value is string → notempty applies
    [InlineData("lookup of string to integer", true)]   // value is integer → type mismatch
    [InlineData("lookup of integer to integer", true)]  // value is integer → type mismatch
    public void Notempty_OnLookup_AppliesToValueType(string fieldType, bool expectMismatch)
    {
        // `notempty` is string-only; on a lookup it constrains the value element. A non-string
        // value type is therefore an InvalidModifierForType mismatch.
        var precept = $$"""
            precept Widget
            field MyMap as {{fieldType}} notempty
            state Open initial
            """;

        var (_, diagnostics) = Check(precept);
        if (expectMismatch)
            diagnostics.Should().Contain(
                d => d.Code == nameof(DiagnosticCode.InvalidModifierForType),
                because: $"notempty on {fieldType} mismatches the non-string value type");
        else
            diagnostics.Should().NotContain(
                d => d.Code == nameof(DiagnosticCode.InvalidModifierForType),
                because: $"notempty on {fieldType} applies to the string value type");
    }

    [Theory]
    [InlineData("string")]
    [InlineData("integer")]
    [InlineData("boolean")]
    public void Clear_OnNonOptionalScalar_EmitsPRE0047(string fieldType)
    {
        var precept = $$"""
            precept Widget
            field MyField as {{fieldType}} default {{Default(fieldType)}}
            state Open initial
            state Done terminal
            event Trigger
            from Open on Trigger -> clear MyField -> transition Done
            """;

        var (_, diagnostics) = Check(precept);
        diagnostics.Should().Contain(
            d => d.Code == nameof(DiagnosticCode.CollectionOperationOnScalar),
            because: $"clear requires a collection or optional field; a non-optional {fieldType} is neither");
    }

    private static string Default(string fieldType) => fieldType switch
    {
        "string" => "\"x\"",
        "integer" => "0",
        "boolean" => "false",
        _ => "0",
    };
}
