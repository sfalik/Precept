using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;
using static Precept.Tests.TypeChecker.TypeCheckerTestHelpers;

namespace Precept.Tests.TypeChecker;

/// <summary>
/// Choice as a collection inner type (F-LANG-COLL-02) and the propagation of the
/// `ordered` modifier from <see cref="ChoiceTypeReference"/> through <see cref="TypedChoiceElement"/>
/// into accessor trait-checking and proof discharge (F-LANG-COLL-03).
///
/// `set of choice of T(...)` is the natural shape for sealed-enumeration histories
/// (severity tiers, urgency levels). The `ordered` keyword on the inner type lifts
/// ordinal-comparison accessors (.min/.max/.first/.last) and choice-vs-choice
/// comparison operators.
/// </summary>
public class CollectionInnerChoiceTests
{
    [Theory]
    [InlineData("set of choice of string(\"Low\", \"Medium\", \"High\")")]
    [InlineData("bag of choice of string(\"Low\", \"Medium\", \"High\")")]
    [InlineData("list of choice of string(\"Low\", \"Medium\", \"High\")")]
    [InlineData("queue of choice of string(\"Low\", \"Medium\", \"High\")")]
    [InlineData("log of choice of string(\"Low\", \"Medium\", \"High\")")]
    public void Collection_OfChoice_Unordered_CompilesClean(string innerType)
    {
        var precept = $$"""
            precept Widget
            field Tiers as {{innerType}}
            state Open initial
            """;

        var (_, diagnostics) = Check(precept);
        diagnostics.Should().NotContain(d =>
            d.Code == nameof(DiagnosticCode.CollectionInnerTypeError)
            || d.Code == nameof(DiagnosticCode.InvalidMemberAccess),
            because: $"{innerType} is a valid choice-typed inner per F-LANG-COLL-02");
    }

    [Theory]
    [InlineData("set of choice of string(\"Low\", \"Medium\", \"High\") ordered")]
    [InlineData("list of choice of integer(1, 2, 3) ordered")]
    [InlineData("queue of choice of string(\"P0\", \"P1\", \"P2\") ordered")]
    public void Collection_OfOrderedChoice_CompilesClean(string innerType)
    {
        var precept = $$"""
            precept Widget
            field Tiers as {{innerType}}
            state Open initial
            """;

        var (_, diagnostics) = Check(precept);
        diagnostics.Should().NotContain(d =>
            d.Code == nameof(DiagnosticCode.CollectionInnerTypeError),
            because: $"{innerType} carries an inner-type 'ordered' modifier");
    }

    [Fact]
    public void OrderedChoiceElement_TypedAsOrdered_OnTypedField()
    {
        var precept = """
            precept Widget
            field Tiers as set of choice of string("Low", "Medium", "High") ordered
            state Open initial
            """;

        var (semantics, diagnostics) = Check(precept);
        diagnostics.Should().BeEmpty();
        var tiers = semantics.FieldsByName["Tiers"];
        tiers.ElementType.Should().BeOfType<TypedChoiceElement>();
        ((TypedChoiceElement)tiers.ElementType!).Ordered.Should().BeTrue();
    }

    [Fact]
    public void OrderedChoiceElement_MinAccessor_DischargesOrderableTrait()
    {
        var precept = """
            precept Widget
            field Severities as set of choice of string("Info", "Warn", "Error") ordered
            field Threshold as choice of string("Info", "Warn", "Error") ordered default "Info"
            rule Severities.min >= Threshold
                because "Lowest severity in the set must be at or above Threshold"
            state Open initial
            """;

        var (_, diagnostics) = Check(precept);
        diagnostics.Should().NotContain(
            d => d.Code == nameof(DiagnosticCode.RequiredTraitViolation),
            because: ".min on a set of choice of T(...) ordered is permitted; the ordered bit lifts the Orderable trait");
    }

    [Fact]
    public void UnorderedChoiceElement_MinAccessor_EmitsRequiredTraitViolation()
    {
        var precept = """
            precept Widget
            field Tags as set of choice of string("A", "B", "C")
            field Threshold as choice of string("A", "B", "C") default "A"
            rule Tags.min >= Threshold
                because "demonstrates that .min on an unordered choice set is rejected"
            state Open initial
            """;

        var (_, diagnostics) = Check(precept);
        diagnostics.Should().Contain(
            d => d.Code == nameof(DiagnosticCode.RequiredTraitViolation),
            because: ".min requires Orderable; an unordered choice element does not satisfy it");
    }
}
