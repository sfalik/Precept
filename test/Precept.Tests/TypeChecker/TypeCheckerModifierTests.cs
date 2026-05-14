using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.TypeChecker;

/// <summary>
/// Slice 7 — Modifier Validation.
/// Covers type-applicability (catalog-driven), duplicate detection, mutual exclusivity,
/// subsumption redundancy, implied-modifier redundancy, writable-on-event-arg,
/// and computed-field-not-writable diagnostics.
/// </summary>
public class TypeCheckerModifierTests
{
    // ════════════════════════════════════════════════════════════════════════
    //  Category 1: Valid modifiers — no diagnostic
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void OptionalModifier_OnStringField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Name as string optional
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void NotemptyModifier_OnStringField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Name as string notempty
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void NonnegativeModifier_OnIntegerField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Count as integer nonnegative
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void WritableModifier_OnStringField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Name as string writable
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void PositiveModifier_OnDecimalField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Price as decimal positive
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void OrderedModifier_OnChoiceField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Priority as choice of string("Low","Medium","High") ordered
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void NotemptyModifier_OnSetOfStringField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Tags as set of string notempty
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Category 2: Invalid modifier for type
    // ════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("boolean", "nonnegative")]
    [InlineData("string", "nonnegative")]
    [InlineData("date", "positive")]
    [InlineData("boolean", "nonzero")]
    [InlineData("integer", "ordered")]
    public void Modifier_NotApplicableToType_EmitsInvalidModifierForType(string typeName, string modifier)
    {
        var precept = $"""
            precept Widget
            field MyField as {typeName} {modifier}
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.InvalidModifierForType);
    }

    [Fact]
    public void NotemptyModifier_OnBooleanField_EmitsInvalidModifierForType()
    {
        var precept = """
            precept Widget
            field Flag as boolean notempty
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.InvalidModifierForType);
    }

    [Fact]
    public void MinlengthModifier_OnIntegerField_EmitsInvalidModifierForType()
    {
        var precept = """
            precept Widget
            field Count as integer minlength 3
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.InvalidModifierForType);
    }

    [Fact]
    public void MaxplacesModifier_OnIntegerField_EmitsInvalidModifierForType()
    {
        var precept = """
            precept Widget
            field Count as integer maxplaces 2
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.InvalidModifierForType);
    }

    [Fact]
    public void MincountModifier_OnStringField_EmitsInvalidModifierForType()
    {
        var precept = """
            precept Widget
            field Name as string mincount 1
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.InvalidModifierForType);
    }

    [Fact]
    public void EventArg_WithModifierNotApplicableToArgType_EmitsInvalidModifierForType()
    {
        var precept = """
            precept Widget
            field Status as string
            state Open initial
            state Closed
            event Close(Reason as boolean nonnegative)
            from Open on Close -> set Status = "done" -> Closed
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.InvalidModifierForType);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Category 3: Duplicate modifier + mutual exclusivity
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void SameModifierTwice_EmitsDuplicateModifier()
    {
        var precept = """
            precept Widget
            field Count as integer nonnegative nonnegative
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.DuplicateModifier);
    }

    [Fact]
    public void OptionalModifierTwice_EmitsDuplicateModifier()
    {
        var precept = """
            precept Widget
            field Name as string optional optional
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.DuplicateModifier);
    }

    [Fact]
    public void NonnegativeAndPositive_MutuallyExclusive_EmitsRedundantModifier()
    {
        var precept = """
            precept Widget
            field Count as integer nonnegative positive
            state Open initial
            """;

        var (_, diagnostics) = TypeCheckerTestHelpers.Check(precept);

        diagnostics.Should().ContainSingle(d => d.Code == nameof(DiagnosticCode.RedundantModifier));
        diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.InvalidModifierForType));
    }

    [Fact]
    public void PositiveAndNonnegative_ReversedOrder_EmitsRedundantModifier()
    {
        var precept = """
            precept Widget
            field Count as integer positive nonnegative
            state Open initial
            """;

        var (_, diagnostics) = TypeCheckerTestHelpers.Check(precept);

        diagnostics.Should().ContainSingle(d => d.Code == nameof(DiagnosticCode.RedundantModifier));
        diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.InvalidModifierForType));
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Category 3b: Conflicting modifiers (optional + notempty — error)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Field_OptionalAndNotempty_EmitsConflictingModifiers()
    {
        // optional permits absence; notempty asserts content — logically contradictory
        var precept = """
            precept Widget
            field Note as string optional notempty
            state Open initial
            """;

        var (_, diagnostics) = TypeCheckerTestHelpers.Check(precept);

        diagnostics.Should().ContainSingle(d => d.Code == nameof(DiagnosticCode.ConflictingModifiers));
    }

    [Fact]
    public void EventArg_OptionalAndNotempty_EmitsConflictingModifiers()
    {
        var precept = """
            precept Widget
            field Status as string
            state Open initial
            state Closed
            event Close(Note as string optional notempty)
            from Open on Close -> set Status = "done" -> Closed
            """;

        var (_, diagnostics) = TypeCheckerTestHelpers.Check(precept);

        diagnostics.Should().ContainSingle(d => d.Code == nameof(DiagnosticCode.ConflictingModifiers));
    }

    [Fact]
    public void Field_CollectionOptionalAndNotempty_EmitsConflictingModifiers()
    {
        // conflict applies to collection fields as well as scalar fields
        var precept = """
            precept Widget
            field Tags as set of string optional notempty
            state Open initial
            """;

        var (_, diagnostics) = TypeCheckerTestHelpers.Check(precept);

        diagnostics.Should().ContainSingle(d => d.Code == nameof(DiagnosticCode.ConflictingModifiers));
    }

    [Fact]
    public void Field_NotemptyAlone_CompilesClean()
    {
        // regression: ConflictingModifiers must not fire when notempty appears without optional
        var precept = """
            precept Widget
            field Note as string notempty
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Category 4: Redundant modifier (subsumption + implied)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void PositiveSubsumesNonnegative_EmitsRedundantModifier()
    {
        // positive.Subsumes = [Nonnegative, Nonzero]
        // Order: positive first → nonnegative second → redundant
        var precept = """
            precept Widget
            field Count as integer positive nonnegative
            state Open initial
            """;

        var (_, diagnostics) = TypeCheckerTestHelpers.Check(precept);

        diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.RedundantModifier))
            .Should().NotBeEmpty(
                because: "'nonnegative' is subsumed by 'positive'");
    }

    [Fact]
    public void PositiveSubsumesNonzero_EmitsRedundantModifier()
    {
        var precept = """
            precept Widget
            field Count as integer positive nonzero
            state Open initial
            """;

        var (_, diagnostics) = TypeCheckerTestHelpers.Check(precept);

        diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.RedundantModifier))
            .Should().NotBeEmpty(
                because: "'nonzero' is subsumed by 'positive'");
    }

    [Fact]
    public void NotemptyOnTimezoneField_ImpliedModifier_EmitsRedundantModifier()
    {
        // timezone type has ImpliedModifiers: [Notempty]
        var precept = """
            precept Widget
            field Tz as timezone notempty
            state Open initial
            """;

        var (_, diagnostics) = TypeCheckerTestHelpers.Check(precept);

        diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.RedundantModifier))
            .Should().NotBeEmpty(
                because: "'notempty' is already implied by the timezone type");
    }

    [Fact]
    public void NotemptyOnCurrencyField_ImpliedModifier_EmitsRedundantModifier()
    {
        // currency type has ImpliedModifiers: [Notempty]
        var precept = """
            precept Widget
            field Cur as currency notempty
            state Open initial
            """;

        var (_, diagnostics) = TypeCheckerTestHelpers.Check(precept);

        diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.RedundantModifier))
            .Should().NotBeEmpty(
                because: "'notempty' is already implied by the currency type");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Category 5: Event arg modifier validation
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void EventArg_WithValidModifier_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Name as string
            state Open initial
            event Submit(Label as string optional)
            from Open on Submit -> set Name = Submit.Label -> no transition
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void EventArg_WithWritableModifier_EmitsExpectedToken()
    {
        var precept = """
            precept Widget
            field Status as string
            state Open initial
            state Closed
            event Close(Reason as string writable)
            from Open on Close -> set Status = "done" -> Closed
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.ExpectedToken);
    }

    [Fact]
    public void ComputedField_WithWritableModifier_EmitsComputedFieldNotWritable()
    {
        var precept = """
            precept Widget
            field Price as number writable
            field Tax as number writable <- Price
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.ComputedFieldNotWritable);
    }

    [Fact]
    public void ComputedField_WithoutWritable_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Price as number writable
            field Tax as number <- Price
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Category 7: Numeric range modifiers on money and quantity fields
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void NonnegativeModifier_OnMoneyField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Balance as money in 'USD' nonnegative
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void PositiveModifier_OnMoneyField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Balance as money in 'USD' positive
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void NonzeroModifier_OnMoneyField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Balance as money in 'USD' nonzero
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void NonnegativeModifier_OnQuantityField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Weight as quantity in 'kg' nonnegative
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void PositiveModifier_OnQuantityField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Weight as quantity in 'kg' positive
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void MinModifier_OnMoneyField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Balance as money in 'USD' min '100.00 USD'
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void MaxModifier_OnMoneyField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Balance as money in 'USD' max '500.00 USD'
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void MinAndMaxModifiers_OnMoneyField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Balance as money in 'USD' min '100.00 USD' max '500.00 USD'
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void MinModifier_OnQuantityField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Weight as quantity in 'kg' min '1.0 kg'
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void BoundsRequireQualifier_MoneyWithoutIn_EmitsDiagnostic()
    {
        var precept = """
            precept Widget
            field Balance as money max '500.00 USD'
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.BoundsRequireQualifier);
    }

    [Fact]
    public void BoundsRequireQualifier_MoneyWithIn_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Balance as money in 'USD' max '500.00 USD'
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void BoundsRequireQualifier_QuantityWithoutInOrOf_EmitsDiagnostic()
    {
        var precept = """
            precept Widget
            field Weight as quantity max '5 kg'
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.BoundsRequireQualifier);
    }

    [Fact]
    public void BoundsRequireQualifier_QuantityWithIn_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Weight as quantity in 'kg' max '5 kg'
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void BoundsRequireQualifier_QuantityWithOf_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Weight as quantity of 'mass' max '5 kg'
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void BoundsRequireQualifier_DecimalWithBoundsNoQualifier_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Balance as decimal min 0 max 1000
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    [Fact]
    public void MinModifier_OnMoneyField_InvalidCurrency_EmitsInvalidTypedConstantContent()
    {
        var precept = """
            precept Widget
            field Balance as money in 'USD' min 'not-valid-currency'
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.InvalidTypedConstantContent);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Slice 8: ConflictingAccessModes (PRE0042)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void AccessMode_ConflictingModesOnSameFieldState_EmitsConflictingAccessModes()
    {
        var precept = """
            precept Widget
            field Name as string default "x"
            state Open initial
            state Done
            in Open modify Name editable
            in Open modify Name readonly
            event Submit()
            from Open on Submit -> transition Done
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.ConflictingAccessModes);
    }

    [Fact]
    public void AccessMode_DifferentStates_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Name as string writable default "x"
            state Open initial
            state Done
            in Done modify Name readonly
            event Submit()
            from Open on Submit -> transition Done
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Slice 8: RedundantAccessMode (PRE0043)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void AccessMode_EditableOnWritableField_EmitsRedundantAccessMode()
    {
        var precept = """
            precept Widget
            field Name as string writable default "x"
            state Open initial
            in Open modify Name editable
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.RedundantAccessMode);
    }

    [Fact]
    public void AccessMode_ReadonlyOnWritableField_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Name as string writable default "x"
            state Open initial
            state Done
            in Done modify Name readonly
            event Submit()
            from Open on Submit -> transition Done
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Slice 8: InvalidModifierValue (PRE0035)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Modifier_NegativeMaxPlaces_EmitsInvalidModifierValue()
    {
        var precept = """
            precept Widget
            field Price as decimal maxplaces -1 default 10
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.InvalidModifierValue);
    }

    [Fact]
    public void Modifier_ValidMaxPlaces_NoDiagnostic()
    {
        var precept = """
            precept Widget
            field Price as decimal maxplaces 2 default 10
            state Open initial
            """;

        TypeCheckerTestHelpers.CheckExpectingClean(precept);
    }
}
