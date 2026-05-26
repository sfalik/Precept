using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.TypeChecker;

/// <summary>
/// Falsifier and regression-guard tests for the default-value-vs-numeric-modifier
/// enforcement path (PRE0067 MaxPlacesExceeded + PRE0079 OutOfRange). Closes
/// F-LANG-BIZ-02 Position 3 (explicit maxplaces opt-in on business-magnitude
/// types), F-LANG-BIZ-06 (ExchangeRate Positive implied), F-LANG-TEMP-03
/// (Duration/Period in ZeroBoundNumericTypes), and the parallel pre-existing
/// silent gap on bare decimal modifiers.
/// </summary>
public class DefaultValueModifierEnforcementTests
{
    // ── PRE0067 MaxPlacesExceeded × business-magnitude (F-LANG-BIZ-02 Position 3) ──

    [Fact]
    public void Money_MaxplacesExceeded_DefaultEmitsDiagnostic() =>
        TypeCheckerTestHelpers.CheckExpectingError("""
            precept Example
            field A as money in 'USD' maxplaces 2 default '1.999 USD'
            """, DiagnosticCode.MaxPlacesExceeded);

    [Fact]
    public void Quantity_MaxplacesExceeded_DefaultEmitsDiagnostic() =>
        TypeCheckerTestHelpers.CheckExpectingError("""
            precept Example
            field A as quantity of 'length' maxplaces 2 default '1.999 m'
            """, DiagnosticCode.MaxPlacesExceeded);

    [Fact]
    public void Price_MaxplacesExceeded_DefaultEmitsDiagnostic() =>
        TypeCheckerTestHelpers.CheckExpectingError("""
            precept Example
            field A as price in 'USD' of 'length' maxplaces 2 default '1.999 USD/m'
            """, DiagnosticCode.MaxPlacesExceeded);

    [Fact]
    public void ExchangeRate_MaxplacesExceeded_DefaultEmitsDiagnostic() =>
        TypeCheckerTestHelpers.CheckExpectingError("""
            precept Example
            field A as exchangerate maxplaces 4 default '1.99999 USD/EUR'
            """, DiagnosticCode.MaxPlacesExceeded);

    // ── PRE0079 OutOfRange × Money/Quantity/Price (F-LANG-BIZ-06 + symmetric) ────

    [Fact]
    public void Money_NonnegativeViolatedByNegativeDefault_EmitsOutOfRange() =>
        TypeCheckerTestHelpers.CheckExpectingError("""
            precept Example
            field A as money in 'USD' nonnegative default '-1.00 USD'
            """, DiagnosticCode.OutOfRange);

    [Fact]
    public void Money_PositiveViolatedByZeroDefault_EmitsOutOfRange() =>
        TypeCheckerTestHelpers.CheckExpectingError("""
            precept Example
            field A as money in 'USD' positive default '0.00 USD'
            """, DiagnosticCode.OutOfRange);

    [Fact]
    public void Money_NonzeroViolatedByZeroDefault_EmitsOutOfRange() =>
        TypeCheckerTestHelpers.CheckExpectingError("""
            precept Example
            field A as money in 'USD' nonzero default '0.00 USD'
            """, DiagnosticCode.OutOfRange);

    [Fact]
    public void Quantity_PositiveViolatedByNegativeDefault_EmitsOutOfRange() =>
        TypeCheckerTestHelpers.CheckExpectingError("""
            precept Example
            field A as quantity of 'length' positive default '-1 m'
            """, DiagnosticCode.OutOfRange);

    [Fact]
    public void Price_PositiveViolatedByZeroDefault_EmitsOutOfRange() =>
        TypeCheckerTestHelpers.CheckExpectingError("""
            precept Example
            field A as price in 'USD' of 'length' positive default '0 USD/m'
            """, DiagnosticCode.OutOfRange);

    // ── PRE0079 OutOfRange × ExchangeRate implied Positive (F-LANG-BIZ-06) ───────

    [Fact]
    public void ExchangeRate_ImpliedPositiveViolatedByZeroDefault_EmitsOutOfRange() =>
        TypeCheckerTestHelpers.CheckExpectingError("""
            precept Example
            field A as exchangerate default '0 USD/EUR'
            """, DiagnosticCode.OutOfRange);

    [Fact]
    public void ExchangeRate_ImpliedPositiveViolatedByNegativeDefault_EmitsOutOfRange() =>
        TypeCheckerTestHelpers.CheckExpectingError("""
            precept Example
            field A as exchangerate default '-1.0 USD/EUR'
            """, DiagnosticCode.OutOfRange);

    // ── PRE0079 OutOfRange × Duration/Period (F-LANG-TEMP-03) ────────────────────

    [Fact]
    public void Duration_NonnegativeViolatedByNegativeDefault_EmitsOutOfRange() =>
        TypeCheckerTestHelpers.CheckExpectingError("""
            precept Example
            field A as duration nonnegative default '-14 hours'
            """, DiagnosticCode.OutOfRange);

    [Fact]
    public void Duration_NonzeroViolatedByZeroDefault_EmitsOutOfRange() =>
        TypeCheckerTestHelpers.CheckExpectingError("""
            precept Example
            field A as duration nonzero default '0 seconds'
            """, DiagnosticCode.OutOfRange);

    [Fact]
    public void Period_NonnegativeViolatedByNegativeDefault_EmitsOutOfRange() =>
        TypeCheckerTestHelpers.CheckExpectingError("""
            precept Example
            field A as period nonnegative default '-1 month'
            """, DiagnosticCode.OutOfRange);

    // ── PRE0079 OutOfRange × Min/Max × Money (previously uncovered) ──────────────

    [Fact]
    public void Money_MinViolatedByDefault_EmitsOutOfRange() =>
        TypeCheckerTestHelpers.CheckExpectingError("""
            precept Example
            field A as money in 'USD' min '100.00 USD' default '50.00 USD'
            """, DiagnosticCode.OutOfRange);

    [Fact]
    public void Money_MaxViolatedByDefault_EmitsOutOfRange() =>
        TypeCheckerTestHelpers.CheckExpectingError("""
            precept Example
            field A as money in 'USD' max '500.00 USD' default '600.00 USD'
            """, DiagnosticCode.OutOfRange);

    // ── PRE0079 OutOfRange × bare decimal (pre-existing silent gap) ──────────────

    [Fact]
    public void Decimal_NonnegativeViolatedByNegativeDefault_EmitsOutOfRange() =>
        TypeCheckerTestHelpers.CheckExpectingError("""
            precept Example
            field A as decimal nonnegative default -1
            """, DiagnosticCode.OutOfRange);

    [Fact]
    public void Decimal_PositiveViolatedByZeroDefault_EmitsOutOfRange() =>
        TypeCheckerTestHelpers.CheckExpectingError("""
            precept Example
            field A as decimal positive default 0
            """, DiagnosticCode.OutOfRange);

    // ── Event-arg parity ─────────────────────────────────────────────────────────

    [Fact]
    public void EventArg_NonnegativeViolatedByNegativeDefault_EmitsOutOfRange() =>
        TypeCheckerTestHelpers.CheckExpectingError("""
            precept Example
            state Open initial
            event Begin(Amount as money in 'USD' nonnegative default '-1.00 USD')
            from Open on Begin -> transition Open
            """, DiagnosticCode.OutOfRange);

    // ── Clean-compile boundary regression guard ──────────────────────────────────

    [Fact]
    public void Money_NonnegativeSatisfiedByZeroDefault_CompilesClean() =>
        TypeCheckerTestHelpers.CheckExpectingClean("""
            precept Example
            field A as money in 'USD' nonnegative default '0.00 USD'
            """);

    [Fact]
    public void Duration_NonnegativeSatisfiedByZeroDefault_CompilesClean() =>
        TypeCheckerTestHelpers.CheckExpectingClean("""
            precept Example
            field A as duration nonnegative default '0 seconds'
            """);

    [Fact]
    public void ExchangeRate_ImpliedPositiveSatisfiedByPositiveDefault_CompilesClean() =>
        TypeCheckerTestHelpers.CheckExpectingClean("""
            precept Example
            field A as exchangerate default '1.0 USD/EUR'
            """);

    [Fact]
    public void Money_MaxplacesSatisfiedByDefault_CompilesClean() =>
        TypeCheckerTestHelpers.CheckExpectingClean("""
            precept Example
            field A as money in 'USD' maxplaces 2 default '1.99 USD'
            """);
}
