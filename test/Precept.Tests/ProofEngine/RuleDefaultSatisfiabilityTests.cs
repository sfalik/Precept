using System.Linq;
using FluentAssertions;
using Precept;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// BUG-027 — global rules must be folded against default field values
/// (spec §0.1 Principle 11). A definition whose default values provably violate
/// an unguarded global <c>rule</c> must reject.
///
/// Today only initial-state <c>ensure</c>s are folded against defaults
/// (<c>CheckInitialStateSatisfiability</c> → <c>UnsatisfiableInitialState</c>/PRE0115);
/// global rules are NOT. This matrix pins the intended behavior:
///   - REJECT cells (#1, #5, #8): a rule that provably folds to false on the
///     defaults must reject. These are RED until the rule-vs-default fold lands.
///   - GREEN guards (#2, #3, #4, #6, #7): provably-true, guarded, unfoldable,
///     vacuous, and the unchanged ensure-fold path must NOT over-reject — these
///     are GREEN today and must stay GREEN once the fix lands (never over-reject).
///
/// Idiom: ground truth is <c>Compiler.Compile(source)</c>. "Rejects" =
/// <c>compilation.HasErrors</c> (any <see cref="Severity.Error"/> diagnostic).
/// The specific diagnostic code for a default-violating rule is an OPEN gate
/// decision (reuse PRE0115 vs a new <c>DefaultViolatesRule</c>), so the reject
/// cells assert <c>HasErrors</c> only — see the per-cell TODO(gate).
/// </summary>
public class RuleDefaultSatisfiabilityTests
{
    // ── #1 — BUG-027 repro: unguarded rule, defaults provably violate ──────────
    // Amount default 5, Floor default 10; rule Amount >= Floor → 5 >= 10 → false.
    // RED now: rule-vs-default fold does not yet exist, so this compiles clean.
    [Fact]
    public void Rule_DefaultsProvablyViolate_Rejects()
    {
        const string precept = """
            precept RuleDefaultViolation
            field Amount as number default 5
            field Floor as number default 10
            rule Amount >= Floor because "Amount must reach the floor"
            state Active initial
            """;

        var result = Compiler.Compile(precept);

        result.HasErrors.Should().BeTrue(
            because: "defaults Amount=5, Floor=10 provably violate the unguarded rule Amount >= Floor (5 >= 10 is false)");
        result.Diagnostics.Should().Contain(
            d => d.Code == nameof(DiagnosticCode.DefaultViolatesRule),
            because: "a global rule provably violated by the defaults emits DefaultViolatesRule");
    }

    // ── #2 — Provably-true on defaults: no rejection ───────────────────────────
    // Amount default 10, Floor default 5; rule Amount >= Floor → 10 >= 5 → true.
    // GREEN guard: a rule satisfied by the defaults must never reject.
    [Fact]
    public void Rule_DefaultsProvablySatisfy_CompilesClean()
    {
        const string precept = """
            precept RuleDefaultSatisfied
            field Amount as number default 10
            field Floor as number default 5
            rule Amount >= Floor because "Amount must reach the floor"
            state Active initial
            """;

        var result = Compiler.Compile(precept);

        result.HasErrors.Should().BeFalse(
            because: "defaults Amount=10, Floor=5 satisfy the rule Amount >= Floor (10 >= 5 holds)");
    }

    // ── #3 — Guarded rule with default-violating condition: skipped ────────────
    // The rule only holds under `when SomeFlag`; a global default-fold must skip
    // guarded rules (the rule is not an unconditional invariant of the defaults).
    // GREEN guard (never-over-reject): guarded rules must not reject on defaults.
    [Fact]
    public void Rule_GuardedConditionViolatedByDefaults_CompilesClean()
    {
        const string precept = """
            precept GuardedRuleDefault
            field Amount as number default 5
            field Floor as number default 10
            field SomeFlag as boolean default false
            rule Amount >= Floor when SomeFlag because "Amount must reach the floor when flagged"
            state Active initial
            """;

        var result = Compiler.Compile(precept);

        result.HasErrors.Should().BeFalse(
            because: "the rule is guarded by `when SomeFlag` — it only holds under the guard, so the unguarded defaults must not reject it");
    }

    // ── #4 — Unfoldable / unknown default: no rejection ────────────────────────
    // Computed field (<-): CheckInitialStateSatisfiability marks computed fields
    // unfoldable, so ConstantFold over a rule referencing it returns null (unknown).
    // GREEN guard (the load-bearing never-over-reject cell): unknown ⇒ no rejection.
    [Fact]
    public void Rule_ReferencesUnfoldableComputedDefault_CompilesClean()
    {
        const string precept = """
            precept UnfoldableRuleDefault
            field Seed as number default 3
            field Computed as number <- Seed * Seed
            rule Computed >= 100 because "Computed must reach the threshold"
            state Active initial
            """;

        var result = Compiler.Compile(precept);

        // Computed is a <- field → marked unfoldable → the rule's condition folds
        // to UNKNOWN (not provably false). A prove-or-reject scan reports only
        // PROVEN violations, so an unknown fold must never reject.
        result.HasErrors.Should().BeFalse(
            because: "Computed is a computed (<-) field whose value is not constant-foldable — the rule folds to unknown, and unknown must not reject");
    }

    // ── #5 — Type-zero default violates rule: rejects ──────────────────────────
    // `field N as integer default 0` pins the integer type-zero value as the
    // default; rule N >= 5 → 0 >= 5 → false. The explicit `default 0` is load-
    // bearing: a no-default integer field is REQUIRED and would error with
    // RequiredFieldsNeedInitialEvent (an unrelated cause) rather than via the
    // rule-vs-default fold. RED now (no rule-vs-default fold yet).
    [Fact]
    public void Rule_TypeZeroDefaultViolates_Rejects()
    {
        const string precept = """
            precept TypeZeroRuleDefault
            field N as integer default 0
            rule N >= 5 because "N must be at least 5"
            state Active initial
            """;

        var result = Compiler.Compile(precept);

        result.HasErrors.Should().BeTrue(
            because: "N's default is the integer type-zero value 0, which violates the rule N >= 5 (0 >= 5 is false)");
        result.Diagnostics.Should().Contain(
            d => d.Code == nameof(DiagnosticCode.DefaultViolatesRule),
            because: "a global rule provably violated by the type-zero default emits DefaultViolatesRule");
    }

    // ── #6 — Existing ensure-fold UNCHANGED (regression) ───────────────────────
    // A default-violating initial-state `ensure` still emits the exact
    // UnsatisfiableInitialState/PRE0115 code. Confirms the rule-fold work does not
    // disturb the established ensure path. GREEN (unchanged today).
    [Fact]
    public void EnsureFold_DefaultViolatingEnsure_StillEmitsUnsatisfiableInitialState()
    {
        const string precept = """
            precept EnsureDefaultViolation
            field Amount as number default 5
            field Floor as number default 10
            state Active initial
            in Active ensure Amount >= Floor because "Amount must reach the floor in the initial state"
            """;

        var result = Compiler.Compile(precept);

        result.Diagnostics.Should().Contain(
            d => d.Code == nameof(DiagnosticCode.UnsatisfiableInitialState),
            because: "the initial-state ensure folds against defaults (5 >= 10 is false) — the established ensure path is unchanged");
    }

    // ── #7 — Self / vacuous rule: no false reject ──────────────────────────────
    // `rule X >= X` is vacuously true for any default. GREEN guard.
    [Fact]
    public void Rule_VacuousSelfComparison_CompilesClean()
    {
        const string precept = """
            precept VacuousRuleDefault
            field X as number default 7
            rule X >= X because "trivially true — guards against false reject"
            state Active initial
            """;

        var result = Compiler.Compile(precept);

        result.HasErrors.Should().BeFalse(
            because: "X >= X is vacuously true under any default — it must never reject");
    }

    // ── #8 — Multi-field compound condition provably false: rejects ────────────
    // A=1, B=1, C=5; rule A + B >= C → 1 + 1 >= 5 → 2 >= 5 → false.
    // Exercises the fold evaluator on a compound (binary-arithmetic) condition.
    // RED now (no rule-vs-default fold yet).
    [Fact]
    public void Rule_CompoundConditionProvablyViolated_Rejects()
    {
        const string precept = """
            precept CompoundRuleDefault
            field A as number default 1
            field B as number default 1
            field C as number default 5
            rule A + B >= C because "the sum must reach C"
            state Active initial
            """;

        var result = Compiler.Compile(precept);

        result.HasErrors.Should().BeTrue(
            because: "defaults A=1, B=1, C=5 make A + B >= C false (2 >= 5), provably violating the unguarded rule");
        result.Diagnostics.Should().Contain(
            d => d.Code == nameof(DiagnosticCode.DefaultViolatesRule),
            because: "a global rule whose compound condition provably folds false on the defaults emits DefaultViolatesRule");
    }

    // ── #9 — Construction (initial) event seeds the field: no rejection ─────────
    // When a precept has a construction (`initial`) event, the field defaults are
    // PLACEHOLDERS — the construction event sets the real initial values, so the
    // defaults are NOT the initial state. The ensure-fold path already skips this
    // case (CheckInitialStateSatisfiability early-returns clean when
    // HasConstructionHandler is true); the rule-vs-default fold must mirror it.
    // GREEN guard (never-over-reject): a construction-seeded field whose
    // placeholder default would violate the rule must NOT reject.
    [Fact]
    public void Rule_ConstructionEventSeedsField_CompilesClean()
    {
        const string precept = """
            precept Ctor
            field Amount as integer nonnegative default 0 editable
            field Floor as integer nonnegative default 10 editable
            rule Amount >= Floor because "amount must reach floor"
            state Active initial
            event Submit(Seed as integer min 10 max 100) initial
            on Submit -> set Amount = Submit.Seed
                      -> set Floor = 10
            """;

        var result = Compiler.Compile(precept);

        result.HasErrors.Should().BeFalse(
            because: "a construction (initial) event sets the real initial values — the placeholder default Amount=0 is not the initial state, so the rule-vs-default fold must skip (mirroring the ensure-fold construction-handler skip)");
        result.Diagnostics.Should().NotContain(
            d => d.Code == nameof(DiagnosticCode.DefaultViolatesRule),
            because: "the construction event seeds Amount/Floor, so the placeholder defaults must not trigger DefaultViolatesRule");
    }
}
