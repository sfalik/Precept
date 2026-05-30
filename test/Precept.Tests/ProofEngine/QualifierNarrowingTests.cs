using System.Linq;
using FluentAssertions;
using Precept;
using Precept.Language;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// D9 open-field qualifier narrowing (S1, identity axes) + the Site-A assignment-qualifier
/// discharge (PRE0141 re-staged to the proof stage). A guard `when X.&lt;axis&gt; == 'value'`
/// narrows an open field so a downstream constrained operation/assignment discharges — soundly:
/// value-exact, all-branches (no OR-collapse), and reassignment-aware. Asserts on the specific
/// qualifier diagnostics (PRE0114 operand / PRE0141 assignment) so unrelated structural
/// diagnostics in the minimal probes don't interfere.
/// </summary>
public class QualifierNarrowingTests
{
    private static bool HasQualifierError(string source)
    {
        var diags = Compiler.Compile(source).Diagnostics;
        return diags.Any(d =>
            d.Code == nameof(DiagnosticCode.UnprovedAssignmentQualifierCompatibility)
            || d.Code == nameof(DiagnosticCode.UnprovedQualifierCompatibility));
    }

    // ── Acceptance: open field narrowed by a guard discharges ──────────────────

    [Fact]
    public void OpenMoney_GuardedToUsd_BinaryOp_Discharges()
    {
        HasQualifierError("""
            precept P
            field Bal as money in 'USD'
            field Payment as money
            state S initial
            event E
            from S on E when Payment.currency == 'USD'
                -> set Bal = Bal + Payment
                -> no transition
            """).Should().BeFalse("the guard narrows Payment to USD, matching Bal");
    }

    [Fact]
    public void OpenMoney_GuardedToUsd_BareAssignment_Discharges()
    {
        HasQualifierError("""
            precept P
            field Bal as money in 'USD'
            field Payment as money
            state S initial
            event E
            from S on E when Payment.currency == 'USD'
                -> set Bal = Payment
                -> no transition
            """).Should().BeFalse("the guard narrows the bare open Payment to USD");
    }

    [Fact]
    public void OpenQuantity_GuardedUnit_BareAssignment_Discharges()
    {
        // Bare assignment of a unit-guarded open quantity. (The binary-op form `Total + Reading`
        // depends on quantity+quantity *result*-unit propagation when an operand is open — a
        // pre-existing limitation distinct from this narrowing, and out of S1's scope.)
        HasQualifierError("""
            precept P
            field Total as quantity in 'kg'
            field Reading as quantity
            state S initial
            event E
            from S on E when Reading.unit == 'kg'
                -> set Total = Reading
                -> no transition
            """).Should().BeFalse("the guard narrows Reading's unit to kg, matching the target");
    }

    // ── Soundness: cases that must NOT discharge ───────────────────────────────

    [Fact]
    public void OpenMoney_CurrencyMismatch_Rejected()
    {
        HasQualifierError("""
            precept P
            field Bal as money in 'USD'
            field Payment as money
            state S initial
            event E
            from S on E when Payment.currency == 'EUR'
                -> set Bal = Payment
                -> no transition
            """).Should().BeTrue("the guard narrows Payment to EUR, which does not satisfy the USD target");
    }

    [Fact]
    public void OpenMoney_NoGuard_Rejected()
    {
        HasQualifierError("""
            precept P
            field Bal as money in 'USD'
            field Payment as money
            state S initial
            event E
            from S on E
                -> set Bal = Payment
                -> no transition
            """).Should().BeTrue("an unnarrowed open field cannot discharge the USD requirement");
    }

    [Fact]
    public void OpenMoney_OrGuard_DoesNotDischarge()
    {
        HasQualifierError("""
            precept P
            field Bal as money in 'USD'
            field Payment as money
            state S initial
            event E
            from S on E when Payment.currency == 'USD' or Payment.currency == 'EUR'
                -> set Bal = Payment
                -> no transition
            """).Should().BeTrue("an OR guard pins Payment to {USD,EUR}, not to a single satisfying value");
    }

    [Fact]
    public void OpenMoney_NegatedGuard_DoesNotDischarge()
    {
        HasQualifierError("""
            precept P
            field Bal as money in 'USD'
            field Payment as money
            state S initial
            event E
            from S on E when Payment.currency != 'JPY'
                -> set Bal = Payment
                -> no transition
            """).Should().BeTrue("a `!=` guard narrows nothing to a positive value");
    }

    [Fact]
    public void OpenMoney_ReassignedAfterGuard_DoesNotDischarge()
    {
        // The guard narrows Payment, but `set Payment = Other` reassigns it before the use;
        // the stale narrowing fact must not discharge the assignment to Bal (sequential proof flow).
        HasQualifierError("""
            precept P
            field Bal as money in 'USD'
            field Payment as money
            field Other as money
            state S initial
            event E
            from S on E when Payment.currency == 'USD'
                -> set Payment = Other
                -> set Bal = Payment
                -> no transition
            """).Should().BeTrue("Payment was reassigned after the guard, so its USD narrowing is stale");
    }

    // ── Regression: declared-matching sources still compile (no over-flagging) ──

    [Fact]
    public void DeclaredMatch_NoQualifierError()
    {
        HasQualifierError("""
            precept P
            field Bal as money in 'USD'
            field Other as money in 'USD'
            state S initial
            event E
            from S on E
                -> set Bal = Other
                -> no transition
            """).Should().BeFalse("a declared USD source matches the USD target with no narrowing needed");
    }

    // ── Diagnostic names the narrowed value vs the required value ───────────────

    [Fact]
    public void MismatchedCurrencyGuard_DiagnosticNamesNarrowedAndRequiredValues()
    {
        var message = Compiler.Compile("""
            precept P
            field Payment as money
            field Balance as money in 'USD'
            state S initial
            event E
            from S on E when Payment.currency == 'EUR'
                -> set Balance = Balance + Payment
                -> no transition
            """).Diagnostics
            .First(d => d.Code == nameof(DiagnosticCode.UnprovedQualifierCompatibility))
            .Message;

        message.Should().Contain("EUR", "the message names what the guard narrowed the open operand to")
            .And.Contain("USD", "and the required value it failed to match");
    }
}
