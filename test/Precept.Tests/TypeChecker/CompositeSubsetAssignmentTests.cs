using System.Linq;
using FluentAssertions;
using Precept;
using Precept.Language;
using Xunit;

namespace Precept.Tests.TypeChecker;

/// <summary>
/// D14 composite-basis assignment: a computed field <c>field Dst as period in '&lt;target&gt;' &lt;- Src</c>
/// is sound iff the source value's basis component set is a SUBSET of the target field's declared
/// basis. Assigning a <c>{days}</c> value to a <c>'years + months'</c> field would widen the basis with
/// an atom the target does not admit, and is rejected with <see cref="DiagnosticCode.QualifierMismatch"/>.
/// The reverse — a narrower (subset) source into a wider target — is sound.
///
/// Full pipeline via <see cref="Compiler.Compile(string)"/> so the assignment-qualifier check runs.
/// </summary>
public class CompositeSubsetAssignmentTests
{
    private static bool HasQualifierMismatch(string source) =>
        Compiler.Compile(source).Diagnostics.Any(d =>
            d.Code == nameof(DiagnosticCode.QualifierMismatch));

    [Fact]
    public void NonSubsetBasis_DaysIntoYearsMonths_EmitsQualifierMismatch()
    {
        // {days} ⊄ {years, months}: the source basis carries an atom the target does not admit.
        var source = """
            precept Schedule
            field Src as period in 'days'
            field Dst as period in 'years + months' <- Src
            state Open initial
            """;

        HasQualifierMismatch(source)
            .Should().BeTrue("{days} is not a subset of {years, months}");
    }

    [Fact]
    public void SubsetBasis_YearsIntoYearsMonths_NoQualifierMismatch()
    {
        // {years} ⊆ {years, months}: a narrower source into a wider target is sound.
        var source = """
            precept Schedule
            field Src as period in 'years'
            field Dst as period in 'years + months' <- Src
            state Open initial
            """;

        HasQualifierMismatch(source)
            .Should().BeFalse("{years} is a subset of {years, months}");
    }

    [Fact]
    public void ExactBasis_YearsMonthsIntoYearsMonths_NoQualifierMismatch()
    {
        // {years, months} ⊆ {years, months}: an exact-basis assignment is sound.
        var source = """
            precept Schedule
            field Src as period in 'years + months'
            field Dst as period in 'years + months' <- Src
            state Open initial
            """;

        HasQualifierMismatch(source)
            .Should().BeFalse("an exact basis is trivially a subset of itself");
    }

    [Fact]
    public void PartialOverlapBasis_MonthsDaysIntoYearsMonths_EmitsQualifierMismatch()
    {
        // {months, days} ⊄ {years, months}: 'days' is the offending atom — partial overlap is not a subset.
        var source = """
            precept Schedule
            field Src as period in 'months + days'
            field Dst as period in 'years + months' <- Src
            state Open initial
            """;

        HasQualifierMismatch(source)
            .Should().BeTrue("{months, days} is not a subset of {years, months} because of 'days'");
    }
}
