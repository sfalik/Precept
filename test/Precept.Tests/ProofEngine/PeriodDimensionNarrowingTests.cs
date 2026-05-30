using System.Linq;
using FluentAssertions;
using Precept;
using Precept.Language;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// D9 open-field qualifier narrowing on the <c>period</c> <c>.dimension</c> axis (S2b).
/// A guard <c>when X.dimension == 'date'</c> narrows an open period so a downstream
/// <c>date ± X</c> / <c>time ± X</c> operation discharges its <see cref="DimensionProofRequirement"/>
/// (PRE0113) — soundly: value-exact against the required <see cref="PeriodDimension"/>,
/// <c>datetime</c> inert (admits all components, proves no single class), all-branches
/// (no OR-collapse), negation narrows nothing, and reassignment-aware.
///
/// Asserts on PRE0113 (<see cref="DiagnosticCode.UnprovedDimensionRequirement"/>) only, so the
/// unrelated required-field-init diagnostics in these minimal probes don't interfere. Compiles
/// directly against freshly-built core (never the MCP wrapper) so results can't be stale.
/// </summary>
public class PeriodDimensionNarrowingTests
{
    private static bool HasDimensionError(string source) =>
        Compiler.Compile(source).Diagnostics.Any(d =>
            d.Code == nameof(DiagnosticCode.UnprovedDimensionRequirement));

    // ── Acceptance: open period narrowed to the required dimension discharges ──
    // (These two are RED until the D6 discharge wiring lands.)

    [Fact]
    public void OpenPeriod_GuardedToDate_DatePlusPeriod_Discharges()
    {
        HasDimensionError("""
            precept P
            field Anchor as date
            field X as period
            state S initial
            event E
            from S on E when X.dimension == 'date'
                -> set Anchor = Anchor + X
                -> no transition
            """).Should().BeFalse("the guard narrows the open period X to the date dimension, satisfying date + X");
    }

    [Fact]
    public void OpenPeriod_GuardedToTime_TimePlusPeriod_Discharges()
    {
        HasDimensionError("""
            precept P
            field Clock as time
            field X as period
            state S initial
            event E
            from S on E when X.dimension == 'time'
                -> set Clock = Clock + X
                -> no transition
            """).Should().BeFalse("the guard narrows X to the time dimension, satisfying time + X");
    }

    // ── Soundness: cross-dimension mismatch must NOT discharge ─────────────────

    [Fact]
    public void OpenPeriod_GuardedToTime_DatePlusPeriod_Rejected()
    {
        HasDimensionError("""
            precept P
            field Anchor as date
            field X as period
            state S initial
            event E
            from S on E when X.dimension == 'time'
                -> set Anchor = Anchor + X
                -> no transition
            """).Should().BeTrue("a time-narrowed period does not satisfy a date-level requirement");
    }

    [Fact]
    public void OpenPeriod_GuardedToDate_TimePlusPeriod_Rejected()
    {
        HasDimensionError("""
            precept P
            field Clock as time
            field X as period
            state S initial
            event E
            from S on E when X.dimension == 'date'
                -> set Clock = Clock + X
                -> no transition
            """).Should().BeTrue("a date-narrowed period does not satisfy a time-level requirement");
    }

    // ── Soundness: datetime is compare-but-inert (admits all → proves no class) ─

    [Fact]
    public void OpenPeriod_GuardedToDatetime_DatePlusPeriod_DoesNotDischarge()
    {
        HasDimensionError("""
            precept P
            field Anchor as date
            field X as period
            state S initial
            event E
            from S on E when X.dimension == 'datetime'
                -> set Anchor = Anchor + X
                -> no transition
            """).Should().BeTrue("a datetime period spans date and time components, proving no single date-level class");
    }

    [Fact]
    public void OpenPeriod_GuardedToDatetime_TimePlusPeriod_DoesNotDischarge()
    {
        HasDimensionError("""
            precept P
            field Clock as time
            field X as period
            state S initial
            event E
            from S on E when X.dimension == 'datetime'
                -> set Clock = Clock + X
                -> no transition
            """).Should().BeTrue("datetime proves no single time-level class");
    }

    // ── Soundness: all-branches (OR-collapse does not pin a single dimension) ──

    [Fact]
    public void OpenPeriod_OrGuard_DoesNotDischarge()
    {
        HasDimensionError("""
            precept P
            field Anchor as date
            field X as period
            state S initial
            event E
            from S on E when X.dimension == 'date' or X.dimension == 'time'
                -> set Anchor = Anchor + X
                -> no transition
            """).Should().BeTrue("an OR guard does not pin X to a single dimension on every branch");
    }

    // ── Soundness: negated guard narrows nothing ──────────────────────────────

    [Fact]
    public void OpenPeriod_NegatedGuard_DoesNotDischarge()
    {
        HasDimensionError("""
            precept P
            field Anchor as date
            field X as period
            state S initial
            event E
            from S on E when X.dimension != 'date'
                -> set Anchor = Anchor + X
                -> no transition
            """).Should().BeTrue("a `!=` guard does not establish X's dimension");
    }

    // ── Soundness: reassignment after the guard invalidates the narrowing fact ─

    [Fact]
    public void OpenPeriod_ReassignedAfterGuard_DoesNotDischarge()
    {
        HasDimensionError("""
            precept P
            field Anchor as date
            field X as period
            field Y as period
            state S initial
            event E
            from S on E when X.dimension == 'date'
                -> set X = Y
                -> set Anchor = Anchor + X
                -> no transition
            """).Should().BeTrue("X is reassigned after the guard, so the date narrowing fact is stale");
    }

    // ── Soundness: no guard does not discharge an open period ─────────────────

    [Fact]
    public void OpenPeriod_NoGuard_DatePlusPeriod_Rejected()
    {
        HasDimensionError("""
            precept P
            field Anchor as date
            field X as period
            state S initial
            event E
            from S on E
                -> set Anchor = Anchor + X
                -> no transition
            """).Should().BeTrue("an unguarded open period has no established dimension");
    }

    // ── Composition: AND with an unrelated conjunct still pins the subject ────

    [Fact]
    public void OpenPeriod_AndGuard_RelevantConjunct_Discharges()
    {
        HasDimensionError("""
            precept P
            field Anchor as date
            field X as period
            field Y as period
            state S initial
            event E
            from S on E when X.dimension == 'date' and Y.dimension == 'time'
                -> set Anchor = Anchor + X
                -> no transition
            """).Should().BeFalse("the AND branch pins X to the date dimension; the unrelated Y conjunct does not block discharge");
    }

    // ── Regression: declared period-of-date still discharges (baseline) ───────

    [Fact]
    public void DeclaredDatePeriod_DatePlusPeriod_Discharges()
    {
        HasDimensionError("""
            precept P
            field Anchor as date
            field X as period of 'date'
            state S initial
            event E
            from S on E
                -> set Anchor = Anchor + X
                -> no transition
            """).Should().BeFalse("a declared date-level period satisfies date + X via the declaration path");
    }
}
