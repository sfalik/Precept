---
title: "Frank's Direction Review — Compiler Conformance Gap Ledger (2026-07-13)"
status: Draft — 2026-07-14 (Frank's assessment; nothing ratified)
author: Frank (Lead/Architect & Language Designer)
owner: Shane
reviews:
  - docs/Working/gap-ledger-2026-07-13.md
context: >
  Shane asked Frank to review the gap ledger's overall direction and
  methodology soundness — not to duplicate the independent cell-by-cell
  detailed pass running concurrently — and to give his opinion on the
  ledger's five owner-fork open questions (OF1-OF5) plus any misclassified
  dispositions in the doc-drift / out-of-scope-with-trigger buckets.
note: >
  This is Frank's raw, verbatim assessment — read-only analysis, no files
  edited by Frank. Nothing here is ratified: the G03 under-scoping finding,
  the G31 probe request, and the five OF rulings (especially OF1 and OF5)
  are recommendations pending Shane's sign-off. OF5 in particular is flagged
  by Frank as philosophy-adjacent (integer overflow posture) and should not
  be treated as a mere doc-drift fix without explicit ratification.
---

# Frank — Direction review: gap-ledger-2026-07-13

## 1. Methodology & disposition taxonomy — sound, with one named blind spot

The five-angle + independent re-extraction + structural-floor + v1-reconciliation frame is the right shape, and the **§1.3 honesty statement is accurate, not overclaiming.** "Agreement + lint + structural-floor backstop, NOT a totality proof" is exactly what it is. If anything it *underclaims* on two axes it should take credit for: the catalog no-orphan-member property (§1.2 Angle 2) is a genuine *structural* completeness guarantee, not mere agreement — every catalog member is reachable by construction (exhaustive `GetMeta` switch + `Enum.GetValues` + reflection test). That's stronger than "two readers agreed." Likewise the 144/164 build-verified emission split is a real lint floor. Say so.

The disposition taxonomy (**implement-against-locked-spec / owner-fork / out-of-scope-with-trigger / new-surface-park**) matches how I triage conformance gaps. No complaints about the buckets themselves.

**But there is a coverage hole the honesty section does not name, and I found it by walking the evidence:**

> **No angle systematically enumerates severity-correctness.** Angle 3 ("by diagnostic code") checks *does it emit?* — not *does it emit at the correct severity per the ruled model?* Severity drift only surfaced where Angle 1 happened to probe (G03/G04). That's a blind spot, and it *demonstrably* let something through — see §2 below. §1.3's residual-honesty paragraph names "a claim mis-stated identically in both extractions" as the escape hatch; it should *also* name "an axis of conformance (severity assignment) that no enumeration angle walks." That's not hypothetical — it's a live miss in this very ledger.

---

## 2. The miss I'd gate on: G03 is under-scoped — four sibling codes dropped

G03 (uninhabitable definitions accepted at Warning, should reject at Error) is correctly dispositioned **implement-against-locked-spec**. But the ledger frames it as a fresh two-code discovery (ContradictoryRule / UnsatisfiableRule) grounded only on spec §0.6 item 7, and **misses that the canonical doc already tracks this exact drift — for six codes, not two.**

`diagnostic-system.md:191` (verbatim):

> "…where a flipped code (`UnreachableState`, `StructuralSinkState`, `DeadEndState`, `RequiredStateDoesNotDominateTerminal`, `ContradictoryRule`, `UnsatisfiableRule`) still emits `Severity.Warning` in `Diagnostics.cs`, that is tracked drift — the code severity flip lands in a later implementation slice; this section states the target severity."

I confirmed all six are `Severity.Warning` today (`Diagnostics.cs:707, :720, :726, :747, :802, :814`). The doc's own block-vs-report model (`:187–189`) explicitly puts these six in the **block/Error** set and keeps `AlwaysRejecting`/`UnhandledEvent`/`FieldNeverSet` in the **report/Warning** set. So the target severity for the four Graph state-structural codes is Error, they currently emit Warning, and **the ledger's G03 enrolls only the two Proof codes.** The four state codes appear nowhere — not in G03's scope note (which lists the report-class codes that *correctly* stay Warning: PRE0082/0153/0154/0080/0111), not as their own gap.

This is load-bearing because the philosophy explicitly claims *"unreachable states, dead-ends … all compile-time impossibilities"* (CLAUDE.md core principles; philosophy §compile-time structural checking). A dead-end state emitting a mere Warning is a philosophy conformance gap, and `diagnostic-system.md:191` already committed the target. **Fold the four Graph codes into WU-STRUCT-SEV, or state explicitly why they're excluded.** Right now they're silently dropped, and the reason they were dropped is the severity blind spot in §1 — the angles probed rule-satisfiability but never walked graph-diagnostic severity.

(Secondary, minor: G03/G18 should cite `diagnostic-system.md:182/187/191` as corroboration. The ledger treats §0.6 item 7 as the sole anchor, but item 7 *defers* severity to `diagnostic-system.md` explicitly — "Severity model … are in diagnostic-system.md" — and that doc is honest and precise about the drift. So G18's framing that "§Status/§5 *understate*" is slightly unfair to the diagnostic doc, which is the opposite of understating.)

---

## 3. The five owner-forks — my rulings

I checked each against the actual doc lines and, for OF5, the runtime backing type. Here's where I land.

### OF1 — message interpolation holes — **SPLIT (value-faults enroll, presence exempt)**

This is genuinely two-sided and genuinely consequential — agree it's near the top. My ruling is neither pure A nor pure B:

- **Value-faults (div-by-zero, overflow, sqrt-of-negative) MUST enroll.** `because "bad {10/0}"` renders on constraint violation and throws `DivideByZero` at render time. A message that crashes the evaluator when it fires is a determinism/inspectability breach — §0.7's "no deferral" applies verbatim, and this is precisely the prevention guarantee Precept sells. Read A wins here.
- **Presence obligations on optional fields MUST NOT enroll.** Read B's argument is decisive *for presence only*: interpolating the possibly-absent field is the message's whole job ("field {A} failed"). Forcing a guard on the field you're reporting is self-defeating.

The clean resolution isn't "pick a side" — it's a **language guarantee that message rendering is presence-tolerant** (an unset optional interpolates to empty/"unset", never throws), which makes the presence obligation unnecessary *by construction* while value-faults still enroll. That's the architecturally honest answer: the boundary of prove-or-reject is "things that crash the evaluator," and presence-in-a-message shouldn't be one of them. Route the value-fault arm into WU-OBLIG-CREATE; the presence arm becomes a runtime null-tolerance spec note, not an obligation.

### OF2 — `FunctionArgConstraintViolation` (PRE0022) — **REMOVE the scaffolding now; re-open via /design only on concrete demand**

Two reasons, and my own gate discipline forces the answer:

1. **The "build" arm is new catalog/language surface with zero locked-spec grounding** (the ledger's own finding: `diagnostic-system.md` merely re-lists the code; nothing defines the semantics). Per my Design Gate + the Tier-2/3 Pre-Design Owner Consultation gate, "build" cannot even be *proposed inline* — it routes to `/design` and needs owner alignment on *whether it's the right problem* first. There is no demonstrated demand. The canonical example (`round(X,-1)`) is already better served by the existing modifier-value path (`InvalidModifierValue`, `primitive-types.md:701`) — a different, live code.
2. **The dead `[StaticallyPreventable]` scaffolding is actively harmful.** It makes `Precept0002` pass green while the guarantee is hollow — the exact G17 false-green pathology. Every day it sits there, the fault-prevention invariant is a lie by one code. Removing it *restores the honesty of the invariant.*

So: delete `FaultCode 8` + `DiagnosticCode.FunctionArgConstraintViolation` + metadata. If a real per-argument constraint need surfaces later, it comes back as a deliberate feature through `/design`, not as resurrected zombie scaffolding. This also closes half of OS2's PRE0022 overlap cleanly.

### OF3 — `notempty` on collection fields — **string-only; `primitive-types.md:582` is the drift**

Not really a fork once you apply catalog architecture. I verified: `primitive-types.md:582` says notempty applies to collections "equivalent to `mincount 1`"; **three** other locked surfaces say string-only (`precept-language-spec.md:1150, :1671, :435`; `collection-types.md:794`). The spec and collection-types.md are the authoritative language-surface docs; primitive-types.md overreached out of its lane (it's describing a primitive and wandered into collection cardinality).

Architecturally decisive: collection cardinality vocabulary is deliberately `mincount`/`maxcount` — single-source in the Constraints catalog. Making `notempty` a *second spelling* of `mincount 1` creates two ways to say one thing, muddies the carefully-drawn inner-type-vs-field-level distinction (`set of string notempty` already means *each element* non-empty, per `:794`), and forces the Constraints-catalog applicability metadata to carry an ambiguity. **Fix `primitive-types.md:582` to string-only.** I'd downgrade this from owner-fork to doc-drift-with-forced-resolution — but keep it surfaced because it touches Constraints-catalog applicability metadata, which is language surface.

### OF4 — `in`-qualified periods orderable — **YES, orderable when unit-pinned; D14 needs the carve-out**

Verified both sides. `collection-types.md:533` grants `.min`/`.max` on `period in 'days'` *with reasoning* ("all elements are single-basis periods; ordering is by single component value"). Temporal D14 (`:1476`) says "equality-only (unqualified)" and the general period row explains *why* — `'1 month'` has no fixed length, so multi-basis periods are genuinely incomparable.

These aren't in conflict once you read D14's own qualifier: **the `in <basis>` qualifier is exactly the mechanism that removes the incomparability.** Pin to one basis and the single component is a total order — that's the same honesty-about-approximation move the qualifier system exists for. collection-types.md already did the reasoning; D14 just never carved out the exception. **Resolution: orderable when pinned to a single unit basis; update D14 to reference the `in`-qualified exception** (it currently reads as an absolute "no ordering," which is the drift). Doc fix, not a behavioral fork.

### OF5 — `integer` overflow posture — **fixed-width, checked overflow; `:81` is wrong against the runtime**

Comparably consequential to OF1, and I settled it by reading the code rather than the doc. The type checker parses integer literals with **`long.TryParse`** (`TypeChecker.Expressions.cs:180, :203`; `TypedConstants.cs:115`; modifier-value checks `Validation.Modifiers.cs:548/578/599`). Integer is backed by **Int64 — fixed-width, 64-bit.** `PreceptValue` is still a stub (`NotImplementedException`), so there is no arbitrary-precision type anywhere in the boundary. The proof engine's whole `NumericOverflow` obligation family (`diagnostic-system.md:182/187`) exists precisely to prevent Int64 overflow at compile time.

Therefore the three postures aren't co-equal:
- `:197` "Overflow is a type error" and `:205` "Checked overflow" **match the implemented reality.**
- `:81` "Arbitrary-precision … no overflow" is **aspirational copy that contradicts the runtime** — it would make the entire integer-lane `NumericOverflow` family dead scaffolding.

**Ruling: `integer` is fixed-width Int64 with checked overflow — provable overflow is a compile-time Error, unprovable is an obligation (also Error), consistent with `diagnostic-system.md:182`. Fix `primitive-types.md:81` to drop "no overflow / arbitrary-precision."**

⚠️ **Philosophy flag (per my charter — I surface, I don't resolve):** "Arbitrary-precision, no overflow" is a *core-guarantee / type-honesty* claim (`docs/philosophy.md` "Honesty about approximation"). Deciding integer is fixed-width Int64 is a philosophy-adjacent posture, not a mere doc typo — it changes what the language promises about `integer`. Shane should ratify the posture explicitly before the doc is edited, even though the code has already decided the question de facto.

**Downstream consequence the ledger got right but should escalate:** OS5 (unary-op `ProofRequirements` slot) is gated on OF5. With integer confirmed fixed-width, `-integer` where the operand can be `long.MinValue` **is** an independently faultable overflow (`-long.MinValue` overflows). OS5's "empty slot defensible-by-construction" rests on the assumption that MinValue-negation is always caught by an enclosing containment site — that assumption is now testable and should be *verified, not assumed*, as part of resolving OF5. **OS5's trigger is effectively already active** — don't leave it in the deferred bucket once OF5 rules "fixed-width."

---

## 4. Mis-scoping in the OS / doc-drift buckets

**The whole G18–G33 table is probe-free.** Those rows are doc-vs-doc contradictions from the re-extractor, dispositioned "no spec-behavior change" *by assertion*. That's fine for genuine doc-vs-doc rows — but it means **if any row's "correct" side is itself wrong against code, the row is misclassified as hygiene when it's behavioral.** One row trips that wire:

- **G31** — `log of T by P` missing-uniqueness-guard on an *append* raises `UnguardedCollectionAccess` (an **access** code) on a **mutation**. The ledger files this as doc-drift (`collection-types.md:384/:392 vs :724-729`). But *if the code actually emits an access-code on a mutation*, that's a diagnostic-quality behavioral defect (wrong code → wrong author guidance → AI-legibility hit), belonging with G12–G16, not in the hygiene bucket. **It needs a probe before you trust the "doc-drift" label.** This is exactly the "doc-drift classification hiding a behavioral gap" risk — G31 is where I'd look.

Everything else in OS1–OS7 I agree is correctly deferred:
- **OS2** (nine dead-specific codes, generic code runs) — soundness bar met, specificity only. Correct. Just don't lose PRE0022 to it — it's OF2's, and OF2 says remove.
- **OS4** (runtime `FaultSiteLink` identity collapse) — correctly deferred to runtime-surfacing; compile-time prevention is live. Agree.
- **OS7** (cell-by-cell type-doc matrices) — the honest coverage boundary. Agree, and it's the right trigger.

**One I would *not* let sit:** nothing in the implement-against-locked-spec behavioral set (G01–G11) is mis-scoped *upward* — G05 (KeyPresence fail-open, P7/P11 breach) and G06/G07 (guard/arg positions never enrolled) are correctly "implement," and they're the real prevention holes. If anything, G06 (six guard positions producing `success:true` on a fault that rejects in a rule body) is the most alarming single finding in the whole ledger and deserves top billing over the diagnostic-message cosmetics — it's a silent prevention bypass, not a message defect.

---

## Bottom line

The ledger's **direction is sound** and its **self-assessment is honest.** Dispositions are applied consistently. Three concrete corrections before WF-2 builds on it:

1. **G03 is under-scoped** — enroll the four Graph state-structural codes (`diagnostic-system.md:191`) or justify their exclusion. Root cause: **severity-correctness is an unwalked conformance axis** — name that blind spot in §1.3.
2. **G31 needs a probe** — its "doc-drift" label is unverified and it smells behavioral.
3. **My OF rulings:** OF1 = split (value-faults enroll, presence exempt via render-tolerance guarantee); OF2 = remove scaffolding now; OF3 = string-only (fix primitive-types.md); OF4 = orderable when unit-pinned (fix D14); OF5 = fixed-width checked overflow (code-confirmed Int64) — **philosophy-flag to Shane before editing `:81`**, and re-activate OS5.

The framework earns confidence. Fix the severity blind spot and the one unverified doc-drift row, get Shane's ruling on OF1 and OF5 (the two with real teeth), and this is a solid foundation.
