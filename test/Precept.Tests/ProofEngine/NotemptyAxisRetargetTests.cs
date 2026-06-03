using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// `notempty` axis retarget (Slice 1): `notempty` becomes string-only and routes to the
/// element ("each element non-empty", ≡ `minlength 1`); collection cardinality is `mincount 1`,
/// which now discharges `.peek`/`.first`/`.last` access safety the way collection-`notempty`
/// used to. The element-bound binding (`BuildElementValueBounds`) is genericized off the
/// catalog's `ProofSatisfactions`, so `notempty` binds with no per-`ModifierKind` arm.
/// Uses <see cref="Compiler.Compile"/> (fresh-build ground truth). RED until the retarget lands.
/// </summary>
public class NotemptyAxisRetargetTests
{
    private static ImmutableArray<Diagnostic> Compile(string source)
        => Compiler.Compile(source).Diagnostics;

    private static bool Has(ImmutableArray<Diagnostic> d, DiagnosticCode code)
        => d.Any(x => x.Code == code.ToString());

    // ── notempty is now an element (string) modifier ────────────────────────────

    [Fact]
    public void SetOfStringNotempty_EmptyElementAdded_EmitsLengthViolation()
    {
        // After the retarget, `notempty` binds per-element (≡ minlength 1), so adding the
        // empty string violates the element bound. (Today notempty means collection-≥1 and
        // the empty element is unchecked — clean — so this is RED until the retarget.)
        var d = Compile("""
            precept T
            field Tags as list of string notempty default ["seed"]
            state Open initial
            state Done terminal
            event Add
            from Open on Add
                -> add Tags ""
                -> transition Done
            """);

        Has(d, DiagnosticCode.LengthBoundViolation).Should().BeTrue(
            because: "per-element notempty rejects the empty-string element added to the set");
    }

    [Fact]
    public void Notempty_OnIntegerElement_EmitsInvalidModifierForType()
    {
        // notempty is string-only after the retarget, so on an integer element it is a type
        // mismatch. (Today notempty applies to the set as a collection — no PRE0033 — RED.)
        var d = Compile("""
            precept T
            field Nums as set of integer notempty
            state Open initial
            """);

        Has(d, DiagnosticCode.InvalidModifierForType).Should().BeTrue(
            because: "notempty does not apply to an integer element");
    }

    // ── mincount 1 now discharges element-access safety ─────────────────────────

    [Fact]
    public void Mincount1_DischargesFirstAccess_Clean()
    {
        // `mincount 1` guarantees ≥1 element, so `.first` is safe with no `count > 0` guard —
        // the discharge collection-`notempty` used to provide. (Today mincount does not
        // discharge — its bound resolves conservatively to null — so `.first` errors: RED.)
        var c = Compiler.Compile("""
            precept T
            field L as list of string mincount 1 default ["seed"]
            field Dst as string optional editable
            state Open initial
            state Done terminal
            event Take
            from Open on Take
                -> set Dst = L.first
                -> transition Done
            """);

        c.HasErrors.Should().BeFalse(
            because: "mincount 1 discharges .first access safety with no count > 0 guard");
    }

    [Fact]
    public void NoMincount_FirstAccess_StillNeedsGuard()
    {
        // Regression guard: the discharge is mincount-gated. With no mincount (and no guard),
        // `.first` is unsafe (the list could be empty) — must still require a guard. Holds
        // today and after.
        var c = Compiler.Compile("""
            precept T
            field L as list of string optional editable
            field Dst as string optional editable
            state Open initial
            state Done terminal
            event Take
            from Open on Take
                -> set Dst = L.first
                -> transition Done
            """);

        c.HasErrors.Should().BeTrue(
            because: "with no mincount and no count > 0 guard, .first is not provably safe");
    }
}
