using System.Linq;
using FluentAssertions;
using Precept;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.TypeChecker;

/// <summary>
/// Slice 26 — event arg default resolution and proof coverage.
/// Verifies that <c>TypedArg.DefaultExpression</c> is resolved, type-checked against
/// the arg's declared type and bounds, and participates in interval-containment proof coverage.
/// </summary>
public class TypeCheckerEventArgDefaultTests
{
    // ── Test 1: quantity default exceeds max → NumericOverflow ───────────────────────

    [Fact]
    public void EventArgDefault_QuantityExceedsMax_EmitsNumericOverflow()
    {
        // '15 kg' → normalized 15000 g. max '10 kg' → 10000 g. 15000 > 10000 → NumericOverflow.
        const string precept = """
            precept ArgDefaultExceedsMax
            event Load(weight: quantity of 'mass' max '10 kg' default '15 kg')
            state Active initial
            """;

        var result = Compiler.Compile(precept);

        result.Diagnostics.Should().Contain(
            d => d.Code == nameof(DiagnosticCode.NumericOverflow),
            because: "default '15 kg' exceeds the declared max of '10 kg'");

        result.Proof.Obligations
            .Where(o => o.Requirement is IntervalContainmentProofRequirement { TargetField: "Load.weight" }
                     && o.Context is ArgDefaultContext)
            .Should().ContainSingle()
            .Which.Disposition.Should().Be(ProofDisposition.Unresolved,
                because: "15000 g > 10000 g — default exceeds bound");
    }

    // ── Test 2: quantity default within max → compiles clean ─────────────────────────

    [Fact]
    public void EventArgDefault_QuantityWithinMax_CompilesClean()
    {
        // '5 kg' → normalized 5000 g. max '10 kg' → 10000 g. 5000 ≤ 10000 → Proved.
        const string precept = """
            precept ArgDefaultWithinMax
            event Load(weight: quantity of 'mass' max '10 kg' default '5 kg')
            state Active initial
            """;

        var result = Compiler.Compile(precept);

        result.Diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.NumericOverflow))
            .Should().BeEmpty(because: "default '5 kg' is within the declared max of '10 kg'");

        result.Proof.Obligations
            .Where(o => o.Requirement is IntervalContainmentProofRequirement { TargetField: "Load.weight" }
                     && o.Context is ArgDefaultContext)
            .Should().ContainSingle()
            .Which.Disposition.Should().Be(ProofDisposition.Proved,
                because: "5000 g ≤ 10000 g — default is within bound");
    }

    // ── Test 3: cross-unit default within bounds → compiles clean ────────────────────

    [Fact]
    public void EventArgDefault_CrossUnit_WithinBounds_CompilesClean()
    {
        // 6 lb_av × 453.59237 g/lb ≈ 2721.55 g. max '5 kg' → 5000 g. 2721.55 ≤ 5000 → Proved.
        const string precept = """
            precept ArgDefaultCrossUnitOk
            event Load(weight: quantity of 'mass' max '5 kg' default '6 [lb_av]')
            state Active initial
            """;

        var result = Compiler.Compile(precept);

        result.Diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.NumericOverflow))
            .Should().BeEmpty(because: "6 lb ≈ 2722 g is within the 5 kg (5000 g) max");

        result.Proof.Obligations
            .Where(o => o.Requirement is IntervalContainmentProofRequirement { TargetField: "Load.weight" }
                     && o.Context is ArgDefaultContext)
            .Should().ContainSingle()
            .Which.Disposition.Should().Be(ProofDisposition.Proved,
                because: "6 lb ≈ 2722 g fits inside the 5 kg bound");
    }

    // ── Test 4: cross-unit default exceeds bounds → NumericOverflow ──────────────────

    [Fact]
    public void EventArgDefault_CrossUnit_ExceedsBounds_EmitsNumericOverflow()
    {
        // 6 lb_av × 453.59237 g/lb ≈ 2721.55 g. max '2 kg' → 2000 g. 2721.55 > 2000 → NumericOverflow.
        const string precept = """
            precept ArgDefaultCrossUnitExceeded
            event Load(weight: quantity of 'mass' max '2 kg' default '6 [lb_av]')
            state Active initial
            """;

        var result = Compiler.Compile(precept);

        result.Diagnostics.Should().Contain(
            d => d.Code == nameof(DiagnosticCode.NumericOverflow),
            because: "6 lb ≈ 2722 g exceeds the 2 kg (2000 g) max");

        result.Proof.Obligations
            .Where(o => o.Requirement is IntervalContainmentProofRequirement { TargetField: "Load.weight" }
                     && o.Context is ArgDefaultContext)
            .Should().ContainSingle()
            .Which.Disposition.Should().Be(ProofDisposition.Unresolved,
                because: "6 lb ≈ 2722 g > 2000 g — default exceeds bound");
    }

    // ── Test 5: money default within max → compiles clean ────────────────────────────

    [Fact]
    public void EventArgDefault_MoneyWithinMax_CompilesClean()
    {
        // '50 USD' ≤ '100 USD' → Proved.
        const string precept = """
            precept ArgDefaultMoneyOk
            event Load(cost: money in 'USD' max '100 USD' default '50 USD')
            state Active initial
            """;

        var result = Compiler.Compile(precept);

        result.Diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.NumericOverflow))
            .Should().BeEmpty(because: "default '50 USD' is within the declared max of '100 USD'");

        result.Proof.Obligations
            .Where(o => o.Requirement is IntervalContainmentProofRequirement { TargetField: "Load.cost" }
                     && o.Context is ArgDefaultContext)
            .Should().ContainSingle()
            .Which.Disposition.Should().Be(ProofDisposition.Proved,
                because: "50 USD ≤ 100 USD — default is within bound");
    }

    // ── Test 6: no bounds declared → no obligation, compiles clean ───────────────────

    [Fact]
    public void EventArgDefault_NoBounds_CompilesClean()
    {
        // No min/max on arg → CollectArgDefaultObligations skips it → no ArgDefaultContext obligation.
        const string precept = """
            precept ArgDefaultNoBounds
            event Load(weight: quantity of 'mass' default '5 kg')
            state Active initial
            """;

        var result = Compiler.Compile(precept);

        result.Diagnostics
            .Where(d => d.Code == nameof(DiagnosticCode.NumericOverflow))
            .Should().BeEmpty(because: "no bounds declared — no interval obligation to violate");

        result.Proof.Obligations
            .Where(o => o.Context is ArgDefaultContext adc && adc.Arg.Name == "weight")
            .Should().BeEmpty(because: "unbounded args generate no default-proof obligations");
    }

    // ── Test 7: type mismatch on arg default → TypeMismatch ──────────────────────────

    [Fact]
    public void EventArgDefault_TypeMismatch_EmitsTypeMismatch()
    {
        // Integer literal 5 used as default for a quantity arg → TypeMismatch
        // (same pattern as field default: default 5 on a quantity field).
        const string precept = """
            precept ArgDefaultTypeMismatch
            event Load(weight: quantity of 'mass' default 5)
            state Active initial
            """;

        var (_, diagnostics) = TypeCheckerTestHelpers.Check(precept);

        diagnostics
            .Where(d => d.Severity == Severity.Error)
            .Select(d => d.Code)
            .Should().Contain(DiagnosticCode.TypeMismatch.ToString(),
                because: "integer literal 5 is not assignable to a quantity arg");
    }
}
