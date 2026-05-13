using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.TypeChecker;

/// <summary>
/// Regression anchors for the double-error fix on identity-type fields with notempty.
///
/// Before the fix: currency/unitofmeasure/dimension + notempty emitted BOTH
/// InvalidModifierForType AND RedundantModifier — a confusing double diagnostic
/// because notempty is implied by those types (ImpliedModifiers catalog entry),
/// which means it is simultaneously "redundant" and "invalid" by two different
/// checker rules.
///
/// After the fix: only RedundantModifier is emitted. The type-applicability check
/// is suppressed when the modifier is in the type's ImpliedModifiers list, so the
/// implied-modifier rule wins and the invalid-for-type rule is silent.
/// </summary>
public class IdentityTypeModifierTests
{
    // ════════════════════════════════════════════════════════════════════════
    //  currency notempty — must emit ONLY RedundantModifier, not both
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Notempty_OnCurrencyField_EmitsOnlyRedundantModifier()
    {
        var precept = """
            precept Widget
            field Cur as currency notempty
            state Open initial
            """;

        var (_, diagnostics) = TypeCheckerTestHelpers.Check(precept);

        diagnostics.Should().ContainSingle(
            d => d.Code == nameof(DiagnosticCode.RedundantModifier),
            because: "'notempty' is implied by currency — only RedundantModifier should fire");

        diagnostics.Should().NotContain(
            d => d.Code == nameof(DiagnosticCode.InvalidModifierForType),
            because: "when a modifier is in ImpliedModifiers, InvalidModifierForType must be suppressed");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  unitofmeasure notempty — must emit ONLY RedundantModifier, not both
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Notempty_OnUnitOfMeasureField_EmitsOnlyRedundantModifier()
    {
        var precept = """
            precept Widget
            field U as unitofmeasure notempty
            state Open initial
            """;

        var (_, diagnostics) = TypeCheckerTestHelpers.Check(precept);

        diagnostics.Should().ContainSingle(
            d => d.Code == nameof(DiagnosticCode.RedundantModifier),
            because: "'notempty' is implied by unitofmeasure — only RedundantModifier should fire");

        diagnostics.Should().NotContain(
            d => d.Code == nameof(DiagnosticCode.InvalidModifierForType),
            because: "when a modifier is in ImpliedModifiers, InvalidModifierForType must be suppressed");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  dimension notempty — must emit ONLY RedundantModifier, not both
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Notempty_OnDimensionField_EmitsOnlyRedundantModifier()
    {
        var precept = """
            precept Widget
            field D as dimension notempty
            state Open initial
            """;

        var (_, diagnostics) = TypeCheckerTestHelpers.Check(precept);

        diagnostics.Should().ContainSingle(
            d => d.Code == nameof(DiagnosticCode.RedundantModifier),
            because: "'notempty' is implied by dimension — only RedundantModifier should fire");

        diagnostics.Should().NotContain(
            d => d.Code == nameof(DiagnosticCode.InvalidModifierForType),
            because: "when a modifier is in ImpliedModifiers, InvalidModifierForType must be suppressed");
    }
}
