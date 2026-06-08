using System.Linq;
using FluentAssertions;
using Precept;
using Precept.Language;
using Precept.Pipeline;
using Precept.Tests.TypeChecker;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Slice 2c-ii — field-reference modifier-bound ENFORCEMENT (the enforcement half of
/// BUG-020), grounded in
/// <c>docs/Working/field-reference-bound-enforcement-2026-06-04.md</c> (Locked,
/// amended/re-locked 2026-06-05).
///
/// A bound modifier whose value is a field reference — numeric <c>min Floor</c> /
/// <c>max Ceil</c>; length <c>minlength Y</c> / <c>maxlength Y</c> (Y an INTEGER field
/// whose VALUE is the required length); count <c>mincount Y</c> / <c>maxcount Y</c>
/// (Y an INTEGER field whose VALUE is the required count) — parses and type-checks
/// today but is silently DROPPED: the bound does nothing (Principle-10/11 soundness
/// hole). This matrix pins the intended POST-BUILD behavior so the enforcement build
/// runs test-first.
///
/// FAILING-FIRST (TDD): the enforcement cells encode a reject/clean the engine does
/// NOT yet produce (the bound is inert), so they FAIL today; the regression-guard
/// cells (literal byte-identical, undeclared-name, self/mutual termination, the
/// already-correct cross-lane / cross-currency type errors) already match and stay
/// green.
///
/// Idiom: ground truth is <c>Compiler.Compile(source)</c> (the freshly-built core
/// pipeline, NOT the MCP — the MCP serves its last-spawn build). "Rejects" =
/// <c>result.HasErrors</c> (any Error-severity diagnostic). "Clean" = no Error-severity
/// diagnostic (PRE0158 "no write site" / PRE0119 "no outgoing transitions" are
/// WARNINGS and are deliberately tolerated — these fields-only stateless fixtures
/// hold their declared defaults). <c>Diagnostic.Code</c> is the string
/// <c>nameof(DiagnosticCode.X)</c>. A few cells inspect the <c>ProofLedger</c> via
/// the type-checker-driven <c>Prove</c> helper for strategy/disposition attribution.
/// </summary>
public class FieldRefBoundEnforcementTests
{
    // ── helpers ───────────────────────────────────────────────────────────────

    private static Compilation Compile(string source) => Compiler.Compile(source);

    /// <summary>True iff the compilation has any Error-severity diagnostic.</summary>
    private static bool Rejects(Compilation c) =>
        c.Diagnostics.Any(d => d.Severity == Severity.Error);

    private static void ShouldReject(Compilation c, string because) =>
        Rejects(c).Should().BeTrue(because);

    private static void ShouldBeClean(Compilation c, string because) =>
        c.Diagnostics.Where(d => d.Severity == Severity.Error).Should().BeEmpty(because);

    private static bool HasCode(Compilation c, DiagnosticCode code) =>
        c.Diagnostics.Any(d => d.Code == code.ToString() && d.Severity == Severity.Error);

    private static ProofLedger Prove(string source)
    {
        var (index, _) = TypeCheckerTestHelpers.Check(source);
        var graph = GraphAnalyzer.Analyze(index);
        return ProofEngine.Prove(index, graph);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  NUMERIC  (min / max)
    // ════════════════════════════════════════════════════════════════════════

    // ── #1 — numeric min, provable default → CLEAN (regression-ish; currently
    //         clean only because the bound is inert, but the asserted outcome is the
    //         same: a satisfied default must compile clean) ──────────────────────
    [Fact]
    public void Numeric_MinFieldRef_ProvableDefault_CompilesClean()
    {
        // Floor default 5 (bounded [0,10]); Amount min Floor default 5.
        // Desugared rule Amount >= Floor folded on defaults: 5 >= 5 → true → clean.
        var c = Compile("""
            precept NumMinProvable
            field Floor as integer min 0 max 10 default 5
            field Amount as integer min Floor default 5
            state Active initial terminal
            """);

        ShouldBeClean(c,
            because: "Amount's default 5 satisfies the desugared rule Amount >= Floor (5 >= Floor's default 5)");
    }

    // ── #2 — numeric min, UNPROVABLE default (the headline BUG-020 regression;
    //         rides BUG-027's rule-vs-default fold) → REJECT ─────────────────────
    [Fact]
    public void Numeric_MinFieldRef_UnprovableDefault_Rejects()
    {
        // Floor default 10 (NO declared min — unbounded for narrowing);
        // Amount min Floor default 5. Desugared rule Amount >= Floor folded on
        // defaults: 5 >= 10 → FALSE → reject via DefaultViolatesRule (BUG-027 fold).
        // RED now: the field-ref bound is inert, so no rule is folded and it is clean.
        var c = Compile("""
            precept NumMinUnprovable
            field Floor as integer default 10
            field Amount as integer min Floor default 5
            state Active initial terminal
            """);

        ShouldReject(c,
            because: "Amount's default 5 violates the desugared rule Amount >= Floor (5 >= Floor's default 10 is false)");
        c.Diagnostics.Should().Contain(
            d => d.Code == nameof(DiagnosticCode.DefaultViolatesRule) && d.Severity == Severity.Error,
            because: "the desugared rule Amount >= Floor folds false on the defaults — DefaultViolatesRule (BUG-027 fold), NOT a narrowed-interval check (Floor is unbounded, narrowing identity-degrades)");
    }

    // ── #3 — numeric min, unbounded-ref + dependent op → REJECT (identity-degrade,
    //         2c-i path) ──────────────────────────────────────────────────────────
    [Fact]
    public void Numeric_MinFieldRef_UnboundedRef_DependentOp_Rejects()
    {
        // Floor unbounded; Amount min Floor; a divisor 100/Amount. The desugared
        // rule Amount >= Floor cannot give Amount a positive floor (Floor unbounded),
        // so the divisor stays unsafe → DivisionByZero. RED now (bound inert → no
        // relational fact → divisor already rejects, but for the wrong reason: the
        // post-build path is the desugared relation identity-degrading. Today it
        // rejects because Amount is simply unbounded; the assertion is the reject).
        var c = Compile("""
            precept NumMinUnboundedDep
            field Floor as integer editable
            field Amount as integer min Floor default 1 editable
            field Q as integer <- 100 / Amount
            state Active initial terminal
            """);

        ShouldReject(c,
            because: "Floor is unbounded, so min Floor cannot exclude Amount == 0 — the divisor 100/Amount stays unsafe (identity-degradation)");
        HasCode(c, DiagnosticCode.DivisionByZero).Should().BeTrue(
            because: "the unbounded field-ref bound contributes no discharge fact for the divisor");
    }

    // ── #4 — numeric min, contradiction with the field's own bound → REJECT ──────
    [Fact]
    public void Numeric_MinFieldRef_ContradictsOwnBound_Rejects()
    {
        // Amount max 5; min Floor with Floor min 10. The desugared rule Amount >= Floor
        // narrows Amount's lower bound to >= 10, contradicting Amount max 5 → empty
        // intersection → reject (contradiction guard). Also defaults: Amount default 5,
        // Floor default 10 → 5 >= 10 false → DefaultViolatesRule. RED now (inert).
        var c = Compile("""
            precept NumMinContra
            field Floor as integer min 10 max 20 default 10
            field Amount as integer max 5 min Floor default 5
            state Active initial terminal
            """);

        ShouldReject(c,
            because: "min Floor (Floor >= 10) contradicts Amount's own max 5 — no value is both >= 10 and <= 5 (empty intersection)");
    }

    // ── #5a — numeric max, provable default → CLEAN ─────────────────────────────
    [Fact]
    public void Numeric_MaxFieldRef_ProvableDefault_CompilesClean()
    {
        // Ceiling default 10 (bounded [0,20]); Amount max Ceiling default 10.
        // Desugared rule Amount <= Ceiling on defaults: 10 <= 10 → true → clean.
        var c = Compile("""
            precept NumMaxProvable
            field Ceiling as integer min 0 max 20 default 10
            field Amount as integer max Ceiling default 10
            state Active initial terminal
            """);

        ShouldBeClean(c,
            because: "Amount's default 10 satisfies the desugared rule Amount <= Ceiling (10 <= Ceiling's default 10)");
    }

    // ── #5b — numeric max, UNPROVABLE default → REJECT (symmetric to #2) ─────────
    [Fact]
    public void Numeric_MaxFieldRef_UnprovableDefault_Rejects()
    {
        // Ceiling default 3; Amount max Ceiling default 7. Desugared rule
        // Amount <= Ceiling on defaults: 7 <= 3 → FALSE → reject (DefaultViolatesRule).
        // RED now (inert).
        var c = Compile("""
            precept NumMaxUnprovable
            field Ceiling as integer default 3
            field Amount as integer max Ceiling default 7
            state Active initial terminal
            """);

        ShouldReject(c,
            because: "Amount's default 7 violates the desugared rule Amount <= Ceiling (7 <= Ceiling's default 3 is false)");
        c.Diagnostics.Should().Contain(
            d => d.Code == nameof(DiagnosticCode.DefaultViolatesRule) && d.Severity == Severity.Error,
            because: "the desugared rule Amount <= Ceiling folds false on the defaults (BUG-027 fold)");
    }

    // ── #6 — the RICH MESSAGE: the unprovable diagnostic names the referenced field ─
    [Fact]
    public void Numeric_MinFieldRef_UnprovableDefault_MessageNamesReferencedField()
    {
        // Same shape as #2 — assert the rejecting diagnostic's message attributes the
        // referenced field 'Floor' (the rich author-facing message, Decision 4).
        var c = Compile("""
            precept NumMinRichMsg
            field Floor as integer default 10
            field Amount as integer min Floor default 5
            state Active initial terminal
            """);

        var reject = c.Diagnostics.FirstOrDefault(d => d.Severity == Severity.Error);
        reject.Should().NotBeNull(because: "the unprovable field-ref bound must reject");
        reject!.Message.Should().Contain("Floor",
            because: "the rich message must name the referenced field so the author sees the real cause (Decision 4 / S5)");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  LENGTH  (minlength / maxlength) — Y is an INTEGER field; Y's VALUE is the
    //  required length; band edge resolves from Y's NumericInterval (value interval)
    // ════════════════════════════════════════════════════════════════════════

    // ── #7 — minlength, provable default → CLEAN ────────────────────────────────
    [Fact]
    public void Length_MinlengthFieldRef_ProvableDefault_CompilesClean()
    {
        // MinLen integer [2,8]; the required minimum length is MinLen's MAX value (8).
        // Name default "abcdefgh" (length 8) clears the required min 8 → clean.
        var c = Compile("""
            precept LenMinProvable
            field MinLen as integer min 2 max 8 default 8
            field Name as string minlength MinLen default "abcdefgh"
            state Active initial terminal
            """);

        ShouldBeClean(c,
            because: "Name's default length 8 clears the required minimum length = MinLen's max value (8)");
    }

    // ── #8 — minlength, UNPROVABLE default (too-short) → REJECT ──────────────────
    [Fact]
    public void Length_MinlengthFieldRef_TooShortDefault_Rejects()
    {
        // MinLen integer [2,8]; required min length = MinLen's max value (8).
        // Name default "ab" (length 2) < 8 → reject (LengthBoundViolation). RED now
        // (the field-ref length bound is inert → no length obligation on Name).
        var c = Compile("""
            precept LenMinTooShort
            field MinLen as integer min 2 max 8 default 8
            field Name as string minlength MinLen default "ab"
            state Active initial terminal
            """);

        ShouldReject(c,
            because: "Name's default length 2 is below the required minimum length = MinLen's max value (8)");
        HasCode(c, DiagnosticCode.LengthBoundViolation).Should().BeTrue(
            because: "a too-short value against a field-ref minlength bound emits LengthBoundViolation");
    }

    // ── #9 — minlength, unbounded-ref (no max on Y) → REJECT (reqMin = +inf) ─────
    [Fact]
    public void Length_MinlengthFieldRef_UnboundedRef_Rejects()
    {
        // MinLen has min 2 but NO max → required min length = ceiling(MinLen) = +inf →
        // no assigned value can clear it → reject (identity-degradation). RED now (inert).
        var c = Compile("""
            precept LenMinUnbounded
            field MinLen as integer min 2 default 2
            field Name as string minlength MinLen default "abcdefghij"
            state Active initial terminal
            """);

        ShouldReject(c,
            because: "MinLen has no declared max value, so the required minimum length is +inf → unprovable → reject (identity-degradation)");
    }

    // ── #10 — minlength contradicts the field's own maxlength → REJECT ───────────
    [Fact]
    public void Length_MinlengthFieldRef_ContradictsOwnMaxlength_Rejects()
    {
        // Name maxlength 3; minlength MinLen with MinLen min 5 (required min = MinLen's
        // max). Give MinLen max 5 so required-min = 5 > Name's own maxlength 3 → empty
        // band → reject (contradiction guard). RED now (inert).
        var c = Compile("""
            precept LenMinContra
            field MinLen as integer min 5 max 5 default 5
            field Name as string maxlength 3 minlength MinLen default "ab"
            state Active initial terminal
            """);

        ShouldReject(c,
            because: "the required minimum length (MinLen's max value 5) exceeds Name's own maxlength 3 — empty band, proven contradiction");
    }

    // ── #11a — maxlength, provable default (Y's MIN value as the allowed max) → CLEAN ─
    [Fact]
    public void Length_MaxlengthFieldRef_ProvableDefault_CompilesClean()
    {
        // MaxLen integer [4,10]; allowed maximum length = MaxLen's MIN value (4).
        // Name default "abcd" (length 4) <= 4 → clean.
        var c = Compile("""
            precept LenMaxProvable
            field MaxLen as integer min 4 max 10 default 4
            field Name as string maxlength MaxLen default "abcd"
            state Active initial terminal
            """);

        ShouldBeClean(c,
            because: "Name's default length 4 is within the allowed maximum length = MaxLen's min value (4)");
    }

    // ── #11b — maxlength, UNPROVABLE default (too-long) → REJECT ─────────────────
    [Fact]
    public void Length_MaxlengthFieldRef_TooLongDefault_Rejects()
    {
        // MaxLen integer [4,10]; allowed maximum length = MaxLen's min value (4).
        // Name default "abcdefgh" (length 8) > 4 → reject (LengthBoundViolation).
        // RED now (inert).
        var c = Compile("""
            precept LenMaxTooLong
            field MaxLen as integer min 4 max 10 default 4
            field Name as string maxlength MaxLen default "abcdefgh"
            state Active initial terminal
            """);

        ShouldReject(c,
            because: "Name's default length 8 exceeds the allowed maximum length = MaxLen's min value (4)");
        HasCode(c, DiagnosticCode.LengthBoundViolation).Should().BeTrue(
            because: "a too-long value against a field-ref maxlength bound emits LengthBoundViolation");
    }

    // ── #12 — minlength Y with Y as STRING → TYPE ERROR ─────────────────────────
    [Fact]
    public void Length_MinlengthStringRef_IsTypeError()
    {
        // minlength requires a non-negative INTEGER value; referencing a string field S
        // is a type error (a string is not a valid integer length value; S1 type-validity).
        // Current behavior captured in the report — the value-type validator
        // (TypeChecker.Validation.Modifiers.cs:570-589) checks only literals/unary-minus,
        // so the field-ref-to-string path may surface a DIFFERENT code today; asserted
        // post-build outcome is HasErrors (a type error is emitted).
        var c = Compile("""
            precept LenMinStringRef
            field S as string default "hi"
            field Name as string minlength S default "world"
            state Active initial terminal
            """);

        ShouldReject(c,
            because: "minlength requires an integer length value; referencing a string field S is a type error (S1 type-validity)");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  COUNT  (mincount / maxcount) — Y is an INTEGER field on a collection.
    //  Count bounds are checked against the count effect of grow (`append`) and
    //  shrink (`remove`) actions — the canonical count idiom (see
    //  CountContainmentEmissionTests). NOTE on collection-literal quirks (probed
    //  against the freshly-built compiler, not the MCP):
    //    - the element-literal form `['a','b']` is NOT supported (PRE0052);
    //    - `default []` is a LIST literal, so a `set` field with `default []`
    //      itself emits a TypeMismatch ("Expected a set value here, but got
    //      'list'") — list/log fields accept `default []` cleanly, so the grow
    //      cells use `list of string ... default []`;
    //    - a `set` is seeded via an `initial` Seed event for the shrink cell.
    // ════════════════════════════════════════════════════════════════════════

    // ── #13a — maxcount, provable (guarded) grow → CLEAN ────────────────────────
    [Fact]
    public void Count_MaxcountFieldRef_GuardedGrow_CompilesClean()
    {
        // MaxLimit integer [3,9]; allowed maximum count = MaxLimit's MIN value (3).
        // A single guarded append (when Tags.count < 3) keeps post-count <= 3 → clean.
        var c = Compile("""
            precept CntMaxProvable
            field MaxLimit as integer min 3 max 9 default 3
            field Tags as list of string maxcount MaxLimit default []
            state Open initial
            event Add(X as string notempty)
            from Open on Add when Tags.count < 3
                -> append Tags Add.X
                -> no transition
            """);

        ShouldBeClean(c,
            because: "the guard `when Tags.count < 3` keeps post-count <= the allowed maximum = MaxLimit's min value (3)");
    }

    // ── #13b — maxcount, UNPROVABLE (unguarded) grow (overflow) → REJECT ─────────
    [Fact]
    public void Count_MaxcountFieldRef_UnguardedGrow_Rejects()
    {
        // MaxLimit integer [1,5]; allowed maximum count = MaxLimit's MIN value (1).
        // An unguarded append into a maxcount-MaxLimit list cannot prove in-band
        // against the relational cap → reject (CountBoundViolation). RED now (the
        // field-ref count bound is inert → no relational cap → grow compiles clean).
        var c = Compile("""
            precept CntMaxOverflow
            field MaxLimit as integer min 1 max 5 default 1
            field Tags as list of string maxcount MaxLimit default []
            state Open initial
            event Add(X as string notempty)
            from Open on Add
                -> append Tags Add.X
                -> no transition
            """);

        ShouldReject(c,
            because: "an unguarded append cannot prove the count stays within the allowed maximum = MaxLimit's min value (1)");
        HasCode(c, DiagnosticCode.CountBoundViolation).Should().BeTrue(
            because: "an overflow grow against a field-ref maxcount bound emits CountBoundViolation");
    }

    // ── #13c — maxcount, unbounded-ref (no min on Y) → REJECT ────────────────────
    [Fact]
    public void Count_MaxcountFieldRef_UnboundedRef_Rejects()
    {
        // MaxLimit has max 5 but NO min → allowed maximum count = floor(⟦MaxLimit⟧) =
        // MaxLimit's min value, which is unbounded below → the cap cannot be
        // established → an unguarded grow cannot prove in-band → reject
        // (identity-degradation). RED now (inert → clean).
        var c = Compile("""
            precept CntMaxUnbounded
            field MaxLimit as integer max 5 default 1
            field Tags as list of string maxcount MaxLimit default []
            state Open initial
            event Add(X as string notempty)
            from Open on Add
                -> append Tags Add.X
                -> no transition
            """);

        ShouldReject(c,
            because: "MaxLimit has no declared min value, so the allowed maximum count cannot be established → unprovable grow → reject");
    }

    // ── #13d — maxcount contradicts the field's own mincount → REJECT ────────────
    [Fact]
    public void Count_MaxcountFieldRef_ContradictsOwnMincount_Rejects()
    {
        // Tags mincount 5; maxcount MaxLimit with MaxLimit [2,2] → allowed max count =
        // MaxLimit's min value (2) < Tags' own mincount 5 → empty band → reject
        // (contradiction guard: no count is both >= 5 and <= 2). NOTE: today this also
        // rejects because the empty list (default []) already violates the literal
        // mincount 5 — the contradiction-specific reject is the POST-BUILD distinct
        // cause; the cell asserts CountBoundViolation, which fires either way.
        var c = Compile("""
            precept CntMaxContra
            field MaxLimit as integer min 2 max 2 default 2
            field Tags as list of string mincount 5 maxcount MaxLimit default []
            state Open initial
            event Add(X as string notempty)
            from Open on Add
                -> append Tags Add.X
                -> no transition
            """);

        ShouldReject(c,
            because: "the allowed maximum count (MaxLimit's min value 2) is below Tags' own mincount 5 — empty band, proven contradiction");
        HasCode(c, DiagnosticCode.CountBoundViolation).Should().BeTrue(
            because: "the contradictory count band emits CountBoundViolation");
    }

    // ── #13e — mincount, UNPROVABLE (unguarded) shrink (underflow) → REJECT ──────
    [Fact]
    public void Count_MincountFieldRef_UnguardedShrink_Rejects()
    {
        // MinLimit integer [1,3]; required minimum count = MinLimit's MAX value (3).
        // A `set` seeded by an initial Seed event, then an unguarded value-remove that
        // cannot prove the count stays at or above the relational floor → reject
        // (CountBoundViolation). RED now (inert → clean). The set+Seed idiom avoids the
        // empty-default underflow and the index-bounds guards of `remove ... at`.
        var c = Compile("""
            precept CntMinUnderflow
            field MinLimit as integer min 1 max 3 default 3
            field Tags as set of string mincount MinLimit
            state Open initial
            event Seed(S as set of string) initial
            event Drop(X as string notempty)
            on Seed -> set Tags = Seed.S
            from Open on Drop
                -> remove Tags Drop.X
                -> no transition
            """);

        ShouldReject(c,
            because: "an unguarded remove cannot prove the count stays at or above the required minimum = MinLimit's max value (3)");
        HasCode(c, DiagnosticCode.CountBoundViolation).Should().BeTrue(
            because: "an underflow shrink against a field-ref mincount bound emits CountBoundViolation");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  CROSS-DOMAIN EDGE (the adversarial S3-dual): numeric and length over the
    //  SAME bounded integer Y read DIFFERENT edges of Y's value interval.
    //  numeric min Y → narrows X's lower bound to Y's MIN (2);
    //  length minlength Y → required min length = Y's MAX (8).
    // ════════════════════════════════════════════════════════════════════════

    // ── #14a — cross-domain edge, both genuinely UNPROVABLE → REJECT ─────────────
    [Fact]
    public void FieldRefBound_CrossDomainEdge_BothUnprovable_Reject()
    {
        // Y integer [2,8]. NUMERIC: Amount min Y default 1 — desugared Amount >= Y on
        // defaults reads Y's default; LENGTH: Name minlength Y default "abc" (length 3)
        // < Y's max value 8 → length unprovable. Both must reject.
        var c = Compile("""
            precept CrossEdgeUnprovable
            field Y as integer min 2 max 8 default 8
            field Amount as integer min Y default 1
            field Name as string minlength Y default "abc"
            state Active initial terminal
            """);

        ShouldReject(c,
            because: "numeric: Amount default 1 < Y's default 8 (Amount >= Y false); length: Name length 3 < Y's max value 8 — both reject reading different edges of the same Y");
    }

    // ── #14b — cross-domain edge, both genuinely PROVABLE → CLEAN ────────────────
    [Fact]
    public void FieldRefBound_CrossDomainEdge_BothProvable_CompilesClean()
    {
        // Y integer [2,8] default 8. NUMERIC: Amount min Y default 8 (8 >= 8 → true).
        // LENGTH: Name minlength Y default "abcdefgh" (length 8 >= Y's max 8 → true).
        // Both clear.
        var c = Compile("""
            precept CrossEdgeProvable
            field Y as integer min 2 max 8 default 8
            field Amount as integer min Y default 8
            field Name as string minlength Y default "abcdefgh"
            state Active initial terminal
            """);

        ShouldBeClean(c,
            because: "numeric: Amount default 8 satisfies Amount >= Y (8 >= 8); length: Name length 8 clears Y's max value 8 — both provable");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  D5 / D6 / D7  (cross-lane / qualifier / computed-field)
    // ════════════════════════════════════════════════════════════════════════

    // ── #15 — D5 cross-lane: decimal min number → TypeMismatch (GREEN today) ─────
    [Fact]
    public void Numeric_CrossLane_DecimalMinNumber_EmitsTypeMismatch()
    {
        // field D as decimal min N where N is a `number` field. §3.6: decimal-vs-number
        // is a type error. Current behavior fires TypeMismatch (PRE0018) at the bound-
        // value layer; the design's D5 expects the comparison lane rule to fire. GREEN.
        var c = Compile("""
            precept CrossLane
            field N as number min 0 default 0
            field D as decimal min N default 0
            state Active initial terminal
            """);

        ShouldReject(c,
            because: "decimal min number crosses the §3.6 comparison lane (decimal-vs-number is a type error)");
        c.Diagnostics.Should().Contain(
            d => d.Code == nameof(DiagnosticCode.TypeMismatch) && d.Severity == Severity.Error,
            because: "the cross-lane field-ref bound emits TypeMismatch (D5 inherits §3.6 lane rules)");
    }

    // ── #16a — D6 qualifier: same-currency min → enforced (provable) → CLEAN ─────
    [Fact]
    public void Numeric_Qualifier_SameCurrencyMin_Provable_CompilesClean()
    {
        // Other money USD; M money USD min Other. Same currency → no qualifier error;
        // the desugared rule M >= Other participates. Defaults M=10 >= Other=5 → clean.
        var c = Compile("""
            precept QualSame
            field Other as money in 'USD' default '5.00 USD'
            field M as money in 'USD' min Other default '10.00 USD'
            state Active initial terminal
            """);

        ShouldBeClean(c,
            because: "same-currency min Other is well-qualified and the default 10 USD satisfies M >= Other (>= 5 USD)");
    }

    // ── #16b — D6 qualifier: cross-currency min → QualifierMismatch (GREEN today) ─
    [Fact]
    public void Numeric_Qualifier_CrossCurrencyMin_EmitsQualifierMismatch()
    {
        // Other money EUR; M money USD min Other. Cross-currency bound → QualifierMismatch
        // (PRE0068). GREEN today.
        var c = Compile("""
            precept QualCross
            field Other as money in 'EUR' default '5.00 EUR'
            field M as money in 'USD' min Other default '10.00 USD'
            state Active initial terminal
            """);

        ShouldReject(c,
            because: "a cross-currency field-ref bound mismatches M's USD qualifier (D6 inherits §428 qualifier rules)");
        c.Diagnostics.Should().Contain(
            d => d.Code == nameof(DiagnosticCode.QualifierMismatch) && d.Severity == Severity.Error,
            because: "cross-currency min Other emits QualifierMismatch");
    }

    // ── #17a — D7 computed-field bound, provable → CLEAN ─────────────────────────
    [Fact]
    public void Numeric_ComputedFieldRef_Provable_CompilesClean()
    {
        // Computed <- Base + 1, Base [0,5] → Computed in [1,6]. Amount min Computed
        // narrows Amount's lower bound from IntervalOf(Computed). Defaults: Base 2 →
        // Computed 3; Amount default 6 >= 3 → clean (and Amount >= Computed's range
        // upper 6 is cleared by Amount default 6).
        var c = Compile("""
            precept ComputedProvable
            field Base as integer min 0 max 5 default 2
            field Computed as integer <- Base + 1
            field Amount as integer min Computed default 6
            state Active initial terminal
            """);

        ShouldBeClean(c,
            because: "Amount's default 6 satisfies min Computed (Computed in [1,6] from Base+1)");
    }

    // ── #17b — D7 computed-field bound, unprovable default → REJECT ──────────────
    [Fact]
    public void Numeric_ComputedFieldRef_UnprovableDefault_Rejects()
    {
        // Computed <- Base + 1, Base default 5 → Computed default 6. Amount min Computed
        // default 2 → 2 >= 6 false → reject. RED now (inert). NOTE: a computed field is
        // non-foldable for the rule-vs-default fold (BUG-027 cell #4 marks computed
        // defaults unfoldable) — so the default-fold may NOT reject this; the reject is
        // expected via the relational-narrowing path reading IntervalOf(Computed).
        // Captured in the report if current behavior differs.
        var c = Compile("""
            precept ComputedUnprovable
            field Base as integer min 5 max 5 default 5
            field Computed as integer <- Base + 1
            field Amount as integer min Computed default 2
            state Active initial terminal
            """);

        ShouldReject(c,
            because: "Amount's default 2 is below min Computed (Computed = Base+1 in [6,6]) — must reject");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  TERMINATION + REGRESSION GUARDS
    // ════════════════════════════════════════════════════════════════════════

    // ── #18 — self-reference (numeric) → compiles, vacuous, terminates ──────────
    [Fact]
    public void SelfFieldRefBound_Numeric_CompilesAndTerminates()
    {
        // min X on X → desugared X >= X, vacuous, dropped at desugar — no error, no hang.
        var c = Compile("""
            precept SelfRef
            field X as integer min X default 3
            state Active initial terminal
            """);

        ShouldBeClean(c,
            because: "min X is self-referential (X >= X), vacuous, dropped at desugar (§3.5 line 1352)");
    }

    // ── #19 — mutual reference → compiles, terminates (A == B) ───────────────────
    [Fact]
    public void MutualFieldRefBound_Numeric_CompilesAndTerminates()
    {
        // A min B + B min A → A == B; each reads the OTHER's non-relational interval
        // (depth-1, no fixpoint) → terminates, compiles clean.
        var c = Compile("""
            precept MutualRef
            field A as integer min B default 5
            field B as integer min A default 5
            state Active initial terminal
            """);

        ShouldBeClean(c,
            because: "mutual references conjoin to A == B without fixpoint re-entry (§3.5 line 1352)");
    }

    // ── #20 — undeclared reference → UndeclaredField (regression guard, GREEN) ───
    [Fact]
    public void UndeclaredFieldRefBound_EmitsUndeclaredField()
    {
        // min Nonexistent → the binder must resolve the name and emit UndeclaredField
        // (PRE0017). Already fixed — regression guard (GREEN today).
        var c = Compile("""
            precept UndeclaredRef
            field X as integer min Nonexistent default 5
            state Active initial terminal
            """);

        ShouldReject(c,
            because: "min Nonexistent references no declared field — the name must not be silently swallowed");
        c.Diagnostics.Should().Contain(
            d => d.Code == nameof(DiagnosticCode.UndeclaredField) && d.Severity == Severity.Error,
            because: "an undeclared field-ref bound emits UndeclaredField (PRE0017)");
    }

    // ── #21a — literal numeric bounds, known-clean → byte-identical (regression) ─
    [Fact]
    public void LiteralBound_Numeric_KnownClean_CompilesClean()
    {
        // Pure literal bounds, no field-refs — must compile exactly as today.
        // (mincount is deliberately omitted: an empty `default []` has count 0, which a
        // literal `mincount 1` would itself reject — unrelated to the field-ref path.)
        var c = Compile("""
            precept LiteralClean
            field A as integer min 0 max 10 default 5
            field B as string minlength 2 maxlength 8 default "abcd"
            field C as list of string maxcount 5 default []
            state Open initial
            event Add(X as string notempty)
            from Open on Add when C.count < 5
                -> append C Add.X
                -> no transition
            """);

        ShouldBeClean(c,
            because: "literal-bound-only precept must remain byte-identical-clean (the field-ref path adds nothing to the literal path)");
    }

    // ── #21b — literal numeric bound, known-rejecting → still rejects (regression) ─
    [Fact]
    public void LiteralBound_Numeric_KnownRejecting_StillRejects()
    {
        // A literal min that the default violates — must reject exactly as today.
        // field N min 5 default 2 → 2 < 5 → OutOfRange (the literal default-bound check).
        var c = Compile("""
            precept LiteralReject
            field N as integer min 5 max 10 default 2
            state Active initial terminal
            """);

        ShouldReject(c,
            because: "a literal min 5 with default 2 rejects (2 < 5) exactly as today — the literal path is unchanged");
        HasCode(c, DiagnosticCode.OutOfRange).Should().BeTrue(
            because: "the literal default-bound violation emits OutOfRange (PRE0079)");
    }
}
