using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Collection count-containment (BUG-018) under <b>Reading A — obligation / prove-or-reject</b>
/// (design: count-bound-discharge-semantics, Locked 2026-06-03).
///
/// A count-bounded (<c>mincount</c>/<c>maxcount</c>) field's count obligation discharges clean
/// <b>iff</b> the post-mutation count interval is <i>provably</i> in-band. Otherwise — a provable
/// violation <b>or</b> a merely-unprovable case (an unguarded grow whose seed <c>hi</c> was
/// <c>∞</c>-capped at <c>Hi</c>, giving post-<c>hi = Hi+1 &gt; Hi</c>) — the obligation is
/// unresolved and <see cref="DiagnosticCode.CountBoundViolation"/> (PRE0136) <b>emits</b>, naming
/// a guard. This mirrors the committed length sibling (<c>TryLengthContainmentProof</c>: both
/// <c>false</c> and <c>null</c> emit).
///
/// The seed is the governed band <c>[mincount ?? 0 .. maxcount ?? ∞]</c>; a same-context
/// <c>count</c>-comparison guard narrows it (<c>when C.count &lt; N ⇒ hi := N−1</c>;
/// <c>when C.count &gt; M ⇒ lo := M+1</c>), and a routed <c>when C.count &gt;= N -&gt; reject</c>
/// sibling narrows the fall-through add row's seed to the cap.
///
/// PRE0136 wording (locked OQ2): overflow — <c>"Cannot prove `C` stays within `maxcount N` after
/// this add — guard with `when C.count &lt; N`."</c>; underflow — <c>"Cannot prove `C` stays at or
/// above `mincount M` — guard with `when C.count &gt; M`."</c>
///
/// Uses <see cref="Compiler.Compile"/> (full pipeline).
/// </summary>
public class CountContainmentEmissionTests
{
    private static ImmutableArray<Diagnostic> Compile(string source)
        => Compiler.Compile(source).Diagnostics;

    private static bool HasCountViolation(ImmutableArray<Diagnostic> d)
        => d.Any(x => x.Code == nameof(DiagnosticCode.CountBoundViolation));

    private static string? CountViolationMessage(ImmutableArray<Diagnostic> d)
        => d.FirstOrDefault(x => x.Code == nameof(DiagnosticCode.CountBoundViolation)).Message;

    // ════════════════════════════════════════════════════════════════════════════
    //  A. Unguarded grow that can't prove in-band → EMITS, names a `when C.count < N` guard
    //     (the load-bearing Reading-A inversion: the held impl stayed CLEAN here)
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void A_Maxcount_SingleUnguardedAdd_Emits()
    {
        // Reading A: seed [0, ∞] narrowed to band [0, 1]; an unguarded add gives post-hi = 1+1 = 2
        // > maxcount 1 — NOT provably in-band, so the obligation is unresolved and emits.
        // (Held impl: stays clean because lower-after 1 ≤ 1 is not a *provable* violation.)
        var d = Compile("""
            precept T
            field C as set of string maxcount 1 default []
            state Open initial
            event Add(X as string notempty)
            from Open on Add
                -> add C Add.X
                -> no transition
            """);

        HasCountViolation(d).Should().BeTrue(
            because: "Reading A: an unguarded add into a maxcount-from-∞-seed gives post-hi = Hi+1, which is not provably in-band, so the obligation emits");
    }

    [Fact]
    public void A_Maxcount_SingleUnguardedAdd_MessageNamesGuard()
    {
        var d = Compile("""
            precept T
            field C as set of string maxcount 1 default []
            state Open initial
            event Add(X as string notempty)
            from Open on Add
                -> add C Add.X
                -> no transition
            """);

        CountViolationMessage(d).Should().NotBeNull().And.Subject!.ToString()
            .Should().Contain("count <",
                because: "the locked OQ2 overflow message names the guard 'when C.count < N'");
    }

    [Theory]
    [InlineData("set", "add C Add.X")]
    [InlineData("bag", "add C Add.X")]
    [InlineData("list", "append C Add.X")]
    [InlineData("log", "append C Add.X")]
    [InlineData("queue", "enqueue C Add.X")]
    [InlineData("stack", "push C Add.X")]
    public void A_Maxcount_UnguardedGrow_AllKindsAndVerbs_Emits(string kind, string growAction)
    {
        // Every grow verb against every collection kind: an unguarded grow into a maxcount-from-∞
        // seed is unprovable-in-band ⇒ emits (Reading A is kind-agnostic for the unguarded case).
        var d = Compile($"""
            precept T
            field C as {kind} of string maxcount 1 default []
            state Open initial
            event Add(X as string notempty)
            from Open on Add
                -> {growAction}
                -> no transition
            """);

        HasCountViolation(d).Should().BeTrue(
            because: $"an unguarded {growAction} into a maxcount 1 {kind} cannot prove in-band ⇒ emits");
    }

    [Fact]
    public void A_Maxcount_TwoDistinctUnguardedAdds_Emits()
    {
        // The classic BUG-018 repro: two distinct adds into maxcount 1 reach count 2.
        // Already provably-over under the held impl; also emits under Reading A.
        var d = Compile("""
            precept T
            field C as set of string maxcount 1 default []
            state Open initial
            state Done terminal
            event Fill(X as string notempty, Y as string notempty)
            from Open on Fill
                -> add C Fill.X
                -> add C Fill.Y
                -> transition Done
            """);

        HasCountViolation(d).Should().BeTrue(
            because: "two distinct adds into a maxcount 1 set reach count 2 > 1");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  B. Guarded grow (`when C.count < N -> add`) → clean
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void B_Maxcount_GuardedAdd_Clean()
    {
        // The guard narrows seed hi := N−1 = 0; post-hi = 0+1 = 1 ≤ maxcount 1 ⇒ provably in-band.
        var d = Compile("""
            precept T
            field C as set of string maxcount 1 default []
            state Open initial
            event Add(X as string notempty)
            from Open on Add when C.count < 1
                -> add C Add.X
                -> no transition
            """);

        HasCountViolation(d).Should().BeFalse(
            because: "the guard `when C.count < 1` narrows the seed so post-hi = 1 ≤ maxcount 1 — provably in-band");
    }

    [Fact]
    public void B_Maxcount_GuardedAdd_LargerCap_Clean()
    {
        var d = Compile("""
            precept T
            field C as set of string maxcount 5 default []
            state Open initial
            event Add(X as string notempty)
            from Open on Add when C.count < 5
                -> add C Add.X
                -> no transition
            """);

        HasCountViolation(d).Should().BeFalse(
            because: "the guard `when C.count < 5` proves the add stays within maxcount 5");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  C. Routed reject-row sibling (`when C.count >= N -> reject` + unguarded add row)
    //     → clean on the add row
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void C_Maxcount_RoutedRejectSibling_AddRowClean()
    {
        // A sibling row rejects when at the cap; the fall-through add row therefore runs only
        // when count < N, so its seed is narrowed to the cap and the add discharges.
        var d = Compile("""
            precept T
            field C as set of string maxcount 1 default []
            state Open initial
            event Add(X as string notempty)
            from Open on Add when C.count >= 1
                -> reject "Already at capacity"
            from Open on Add
                -> add C Add.X
                -> no transition
            """);

        HasCountViolation(d).Should().BeFalse(
            because: "the `when C.count >= 1 -> reject` sibling narrows the fall-through add row's seed to the cap, so the add is provably in-band");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  D. Shrink provably below `mincount` → emits (names `when C.count > M`);
    //     guarded shrink → clean
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void D_Mincount_ShrinkProvablyUnderflows_Emits()
    {
        // Field fully determined to [1,1] (mincount 1 maxcount 1 default [1]); a remove gives
        // post-[0,0], below mincount 1 ⇒ emits.
        var d = Compile("""
            precept T
            field C as set of string mincount 1 maxcount 1 default ["a"]
            state Open initial
            event E(X as string notempty)
            from Open on E
                -> remove C E.X
                -> no transition
            """);

        HasCountViolation(d).Should().BeTrue(
            because: "removing from a [1,1] collection drops count to 0, below mincount 1");
    }

    [Fact]
    public void D_Mincount_ShrinkUnderflow_MessageNamesGuard()
    {
        var d = Compile("""
            precept T
            field C as set of string mincount 1 maxcount 1 default ["a"]
            state Open initial
            event E(X as string notempty)
            from Open on E
                -> remove C E.X
                -> no transition
            """);

        CountViolationMessage(d).Should().NotBeNull().And.Subject!.ToString()
            .Should().Contain("count >",
                because: "the locked OQ2 underflow message names the guard 'when C.count > M'");
    }

    [Fact]
    public void D_Mincount_GuardedShrink_Clean()
    {
        // The guard `when C.count > 1` narrows seed lo := 2; a remove gives post-lo = 1 ≥ mincount 1.
        var d = Compile("""
            precept T
            field C as set of string mincount 1 default ["a"]
            state Open initial
            event E(X as string notempty)
            from Open on E when C.count > 1
                -> remove C E.X
                -> no transition
            """);

        HasCountViolation(d).Should().BeFalse(
            because: "the guard `when C.count > 1` proves the remove leaves count ≥ mincount 1");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  E. `clear` against `mincount` → emits
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void E_Mincount_Clear_Emits()
    {
        // clear forces post-[0,0]; with mincount 1 that is below the floor ⇒ emits.
        var d = Compile("""
            precept T
            field C as set of string mincount 1 default ["a"]
            state Open initial
            event E
            from Open on E
                -> clear C
                -> no transition
            """);

        HasCountViolation(d).Should().BeTrue(
            because: "clear forces count to 0, below mincount 1");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  F. `set C = [literal]` is type-rejected (list literal outside `default`) — no count
    //     obligation on an already-invalid construct.
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void F_SetLiteral_IsTypeRejected_NoCountViolation()
    {
        // A list literal is legal only in `default` (PriorFieldsOnly scope). In a handler it is
        // type-rejected: PRE0044 ListLiteralOutsideDefault plus a set↔list TypeMismatch. There is no
        // count obligation on this already-invalid construct, so CountBoundViolation does NOT emit.
        var d = Compile("""
            precept T
            field C as set of string maxcount 2 default []
            state Open initial
            state Done terminal
            event E
            from Open on E
                -> set C = ["a", "b", "c"]
                -> transition Done
            """);

        d.Any(x => x.Code == nameof(DiagnosticCode.ListLiteralOutsideDefault)).Should().BeTrue(
            because: "a list literal in a handler is rejected as PRE0044 ListLiteralOutsideDefault");
        d.Any(x => x.Code == nameof(DiagnosticCode.TypeMismatch)).Should().BeTrue(
            because: "`set C = [literal]` is a set↔list mismatch");
        HasCountViolation(d).Should().BeFalse(
            because: "the count path is correctly absent on an already-invalid construct — no CountBoundViolation");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  G. `default [...]` underflowing `mincount` → emits
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void G_Mincount_DefaultUnderflows_Emits()
    {
        // An empty default is an exact point [0,0], below mincount 1.
        var d = Compile("""
            precept T
            field C as set of string mincount 1 default []
            state Open initial
            """);

        HasCountViolation(d).Should().BeTrue(
            because: "the default [] is exactly [0,0], below mincount 1");
    }

    [Fact]
    public void G_Mincount_DefaultWithinBand_Clean()
    {
        var d = Compile("""
            precept T
            field C as set of string mincount 1 default ["a"]
            state Open initial
            """);

        HasCountViolation(d).Should().BeFalse(
            because: "the default [\"a\"] is exactly [1,1], at the mincount 1 floor");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  H. Dedup-set floor (Decision 3) — Option 2 is OUT; no special same-literal +0
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void H_DedupSet_UnguardedRepeatAdd_Emits()
    {
        // An unguarded `add x; add x` into a maxcount 1 set: Option 2 is OUT, so the same-element
        // `+0` is NOT specially recognized — the obligation is unproven and emits (Reading A).
        var d = Compile("""
            precept T
            field C as set of string maxcount 1 default []
            state Open initial
            event Add(X as string notempty)
            from Open on Add
                -> add C Add.X
                -> add C Add.X
                -> no transition
            """);

        HasCountViolation(d).Should().BeTrue(
            because: "Option 2 is out — an unguarded `add x; add x` is not recognized as +0, so the unproven obligation emits");
    }

    [Fact]
    public void H_DedupSet_GuardedSingleAdd_NotOverRejected()
    {
        // The dedup floor's soundness role: a *guarded* single add into a maxcount 1 set is
        // provably in-band and must NOT be over-rejected via a spurious lower-bound claim.
        var d = Compile("""
            precept T
            field C as set of string maxcount 1 default []
            state Open initial
            event Add(X as string notempty)
            from Open on Add when C.count < 1
                -> add C Add.X
                -> no transition
            """);

        HasCountViolation(d).Should().BeFalse(
            because: "a guarded add into a dedup set is provably in-band; the dedup floor must not over-reject it");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Soundness of the per-kind/per-action delta model (pins the sound delta vs ±1-both)
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Soundness_DedupAddThenRemove_MincountUnderflow_Emits()
    {
        // A dedup `add` leaves the LOWER bound unchanged (the add may be a duplicate no-op), so the
        // following `remove` can drop the count below mincount 2. The sound model: seed [2,5];
        // add → [2,6] (dedup: lo unchanged); remove → [1,6] (lo −1) — lo 1 < mincount 2 ⇒ emits.
        // A naive +1-on-both for the add would give add → [3,6], remove → [2,6], lo 2 ≥ 2 — MISSED.
        var d = Compile("""
            precept T
            field C as set of string mincount 2 maxcount 5 default ["a", "b"]
            state Open initial
            event E(X as string notempty, Y as string notempty)
            from Open on E when C.count < 5
                -> add C E.X
                -> remove C E.Y
                -> no transition
            """);

        HasCountViolation(d).Should().BeTrue(
            because: "a dedup add leaves the lower bound unchanged, so the following remove can underflow mincount 2 — the sound model catches it, naive +1-on-both would miss it");
    }

    [Fact]
    public void Soundness_RemoveThenAdd_MaxcountOverflow_Emits()
    {
        // A `remove`-by-value may be a no-op (element absent), so it leaves the UPPER bound unchanged;
        // the following dedup `add` can then push the count past maxcount 3. The sound model: seed [0,3];
        // remove → [0,3] (hi unchanged, conditional); add → [0,4] (hi +1) — hi 4 > maxcount 3 ⇒ emits.
        // A naive −1-on-both for the remove would give remove → [0,2], add → [0,3], hi 3 ≤ 3 — MISSED.
        var d = Compile("""
            precept T
            field C as set of string maxcount 3 default ["a", "b", "c"]
            state Open initial
            event E(X as string notempty, Y as string notempty)
            from Open on E
                -> remove C E.Y
                -> add C E.X
                -> no transition
            """);

        HasCountViolation(d).Should().BeTrue(
            because: "a remove-by-value may be a no-op (upper unchanged), so the following dedup add can overflow maxcount 3 — the sound model catches it, naive −1-on-both would miss it");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Regression: no count bound ⇒ no obligation
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void NoCountBound_UnguardedAdds_Clean()
    {
        var d = Compile("""
            precept T
            field C as set of string default []
            state Open initial
            state Done terminal
            event Fill(X as string notempty, Y as string notempty)
            from Open on Fill
                -> add C Fill.X
                -> add C Fill.Y
                -> transition Done
            """);

        HasCountViolation(d).Should().BeFalse(
            because: "no mincount/maxcount ⇒ no count-containment obligation");
    }

    [Fact]
    public void SetToNonLiteral_NoObligation_Clean()
    {
        // `set C = <non-literal>` drops the interval (unknown count) and emits nothing per the
        // delta model — the whole-collection assignment from an event arg is not a count obligation.
        var d = Compile("""
            precept T
            field C as set of string maxcount 2 default []
            state Open initial
            state Done terminal
            event E(Src as set of string)
            from Open on E
                -> set C = E.Src
                -> transition Done
            """);

        HasCountViolation(d).Should().BeFalse(
            because: "`set C = <non-literal>` drops the count interval and emits nothing (the count is unknown)");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  I. Corpus: the one count-bounded sample stays clean (event-arg, not in-transition)
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void I_Corpus_ShoppingCart_NoCountViolation()
    {
        // The one count-bounded field in the corpus (`CatalogItems ... mincount 1`) is an EVENT
        // ARGUMENT governed at ingress, set whole via `set Catalog = Create.CatalogItems`
        // (a non-literal assignment) — not an in-transition count mutation, so no PRE0136.
        var samplesRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "samples"));
        var path = Path.Combine(samplesRoot, "shopping-cart.precept");
        var d = Compile(File.ReadAllText(path));

        HasCountViolation(d).Should().BeFalse(
            because: "the count-bounded CatalogItems is an event-arg governed at ingress, not an in-transition mutation");
    }
}
