using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Precept;
using Precept.Language;
using Xunit;

namespace Precept.Tests.TypeChecker;

/// <summary>
/// Legal-basis-by-source-operation (business-domain-types.md § D15 composite legality / Gap 4):
/// a temporal subtraction yields a <c>period</c> whose producible component atoms are fixed by the
/// operand dimension — <c>date − date</c> → calendar atoms (years/months/weeks/days),
/// <c>time − time</c> → clock atoms (hours/minutes/seconds), <c>datetime − datetime</c> → either.
/// Declaring the computed result with an atom the source cannot produce emits <c>QualifierMismatch</c>
/// per illegal atom. Full pipeline (Compiler.Compile) so the type stage runs end to end.
/// </summary>
public class TemporalSubtractionBasisTests
{
    private static IReadOnlyList<Diagnostic> Diags(string source) => Compiler.Compile(source).Diagnostics;

    private static bool HasQualifierMismatch(string source) =>
        Diags(source).Any(d => d.Code == nameof(DiagnosticCode.QualifierMismatch));

    private static bool QualifierMismatchNaming(string source, string atom) =>
        Diags(source).Any(d => d.Code == nameof(DiagnosticCode.QualifierMismatch) && d.Message.Contains(atom));

    [Fact]
    public void DateMinusDate_IntoDateTimeComposite_RejectsClockAtom()
    {
        var source = """
            precept Span
            field D1 as date
            field D2 as date
            field S as period in 'days + hours' <- D1 - D2
            state Open initial
            """;

        // 'hours' is a clock atom that date − date (calendar distance) cannot produce.
        QualifierMismatchNaming(source, "hours")
            .Should().BeTrue("date − date cannot produce the clock atom 'hours'");
        // 'days' is a calendar atom date − date can produce — must NOT be flagged.
        QualifierMismatchNaming(source, "days")
            .Should().BeFalse("'days' is a legal calendar atom for date − date");
    }

    [Fact]
    public void TimeMinusTime_IntoCalendarBasis_RejectsCalendarAtom()
    {
        var source = """
            precept Span
            field T1 as time
            field T2 as time
            field S as period in 'days' <- T1 - T2
            state Open initial
            """;

        QualifierMismatchNaming(source, "days")
            .Should().BeTrue("time − time cannot produce the calendar atom 'days'");
    }

    [Fact]
    public void DateMinusDate_IntoAllCalendarComposite_NoMismatch()
    {
        var source = """
            precept Span
            field D1 as date
            field D2 as date
            field S as period in 'months + days' <- D1 - D2
            state Open initial
            """;

        HasQualifierMismatch(source)
            .Should().BeFalse("both 'months' and 'days' are calendar atoms producible by date − date");
    }

    [Fact]
    public void TimeMinusTime_IntoAllClockComposite_NoMismatch()
    {
        var source = """
            precept Span
            field T1 as time
            field T2 as time
            field S as period in 'hours + minutes' <- T1 - T2
            state Open initial
            """;

        HasQualifierMismatch(source)
            .Should().BeFalse("both 'hours' and 'minutes' are clock atoms producible by time − time");
    }

    [Fact]
    public void DateTimeMinusDateTime_IntoSpanningComposite_NoMismatch()
    {
        var source = """
            precept Span
            field A as datetime
            field B as datetime
            field S as period in 'days + hours' <- A - B
            state Open initial
            """;

        HasQualifierMismatch(source)
            .Should().BeFalse("datetime − datetime can produce both calendar and clock atoms");
    }

    [Fact]
    public void DateMinusDate_IntoSingleLegalAtom_NoMismatch()
    {
        var source = """
            precept Span
            field D1 as date
            field D2 as date
            field S as period in 'days' <- D1 - D2
            state Open initial
            """;

        HasQualifierMismatch(source)
            .Should().BeFalse("'days' is a legal calendar atom for date − date");
    }

    [Fact]
    public void DateMinusDate_IntoMultiIllegalComposite_EmitsOnePerIllegalAtom()
    {
        // Both 'hours' and 'minutes' are clock atoms date − date cannot produce → two diagnostics.
        var source = """
            precept Span
            field D1 as date
            field D2 as date
            field S as period in 'hours + minutes' <- D1 - D2
            state Open initial
            """;

        Diags(source).Count(d => d.Code == nameof(DiagnosticCode.QualifierMismatch))
            .Should().Be(2, "one QualifierMismatch per illegal atom (hours, minutes)");
    }

    [Fact]
    public void TimeMinusTime_IntoWeeks_RejectsCalendarAtom()
    {
        var source = """
            precept Span
            field T1 as time
            field T2 as time
            field S as period in 'weeks' <- T1 - T2
            state Open initial
            """;

        QualifierMismatchNaming(source, "weeks")
            .Should().BeTrue("time − time cannot produce the calendar atom 'weeks'");
    }

    [Fact]
    public void DateMinusDate_IntoSeconds_RejectsClockAtom()
    {
        var source = """
            precept Span
            field D1 as date
            field D2 as date
            field S as period in 'seconds' <- D1 - D2
            state Open initial
            """;

        QualifierMismatchNaming(source, "seconds")
            .Should().BeTrue("date − date cannot produce the clock atom 'seconds'");
    }

    [Fact]
    public void DateMinusDate_IntoYearsWeeksComposite_NoMismatch()
    {
        var source = """
            precept Span
            field D1 as date
            field D2 as date
            field S as period in 'years + weeks' <- D1 - D2
            state Open initial
            """;

        HasQualifierMismatch(source)
            .Should().BeFalse("'years' and 'weeks' are calendar atoms producible by date − date");
    }

    [Fact]
    public void InstantMinusInstant_YieldsDurationNotPeriod_NoBasisCheck()
    {
        // instant − instant → duration (not period); the period-result guard excludes it entirely.
        var source = """
            precept Span
            field I1 as instant
            field I2 as instant
            field S as duration <- I1 - I2
            state Open initial
            """;

        HasQualifierMismatch(source)
            .Should().BeFalse("instant − instant yields a duration, which carries no period basis to check");
    }
}
