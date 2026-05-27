using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;
using static Precept.Tests.TypeChecker.TypeCheckerTestHelpers;

namespace Precept.Tests.TypeChecker;

/// <summary>
/// F-LANG-TEMP-04: when both operands of a period `==` / `!=` are static literal
/// periods with disjoint non-zero components, the comparison is statically
/// knowable. Per `docs/language/temporal-type-system.md § Compiler warning —
/// always-false literal comparisons`, the type checker emits a Warning.
/// </summary>
public class AlwaysFalsePeriodComparisonTests
{
    [Fact]
    public void MonthsVsDays_EmitsAlwaysFalseWarning()
    {
        // Direct literal-vs-literal comparison — the canonical F-LANG-TEMP-04 case
        // from temporal-type-system.md § Compiler warning — always-false literal
        // comparisons. The warning fires when both operands are constant literal
        // periods with disjoint non-zero components.
        var (_, diagnostics) = Check("""
            precept Repro
            field AlwaysFalse as boolean <- '1 month' == '30 days'
            state Open initial
            """);

        diagnostics.Should().Contain(
            d => d.Code == nameof(DiagnosticCode.AlwaysFalsePeriodComparison) && d.Severity == Severity.Warning,
            because: "'1 month' and '30 days' have disjoint non-zero components (months vs days); == is statically false");
    }

    [Fact]
    public void NotEquals_DisjointComponents_EmitsAlwaysTrueWarning()
    {
        var (_, diagnostics) = Check("""
            precept Repro
            field AlwaysTrue as boolean <- '1 year' != '7 days'
            state Open initial
            """);

        diagnostics.Should().Contain(
            d => d.Code == nameof(DiagnosticCode.AlwaysFalsePeriodComparison) && d.Severity == Severity.Warning,
            because: "'1 year' and '7 days' have disjoint components; != is statically true");
    }

    [Fact]
    public void OverlappingComponents_NoWarning()
    {
        var (_, diagnostics) = Check("""
            precept Repro
            field Maybe as boolean <- '1 year 6 months' == '1 year'
            state Open initial
            """);

        diagnostics.Should().NotContain(
            d => d.Code == nameof(DiagnosticCode.AlwaysFalsePeriodComparison),
            because: "both periods have a non-zero years component, so equality is not statically false");
    }

    [Fact]
    public void StructurallyEqual_NoWarning()
    {
        var (_, diagnostics) = Check("""
            precept Repro
            field AlwaysTrueButNotFlagged as boolean <- '30 days' == '30 days'
            state Open initial
            """);

        diagnostics.Should().NotContain(
            d => d.Code == nameof(DiagnosticCode.AlwaysFalsePeriodComparison),
            because: "structurally equal periods are always-true, not always-false; this warning is for disjoint-component literals only");
    }

    [Fact]
    public void FieldRefComparison_NoWarning()
    {
        // The warning is scoped to constant-vs-constant. A field reference whose
        // default happens to be a literal is NOT a constant-folded operand at the
        // comparison site; the runtime can update the field, so the comparison is
        // not statically knowable.
        var (_, diagnostics) = Check("""
            precept Repro
            field A as period default '1 month'
            field B as period default '30 days'
            field NotConstant as boolean <- A == B
            state Open initial
            """);

        diagnostics.Should().NotContain(
            d => d.Code == nameof(DiagnosticCode.AlwaysFalsePeriodComparison),
            because: "field-vs-field comparison is not constant-folded; the warning fires only on direct literal operands");
    }
}
