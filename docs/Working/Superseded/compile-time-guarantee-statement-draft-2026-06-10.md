---
status: Draft for discussion — NOT locked
authored: 2026-06-10
topic: A precise statement of what Precept's compile-time guarantee promises — the scope (faults + rule preservation), grounded in already-locked canon, with mechanism marked design-intent-pending-runtime
grounds-in: precept-language-spec.md §0.7 / §3A.4 / §2.4; philosophy.md
supersedes-when-locked: the guarantee-thesis halves of precept-guarantee-specification-2026-06-05 (strawman) + proven-core-governed-boundary-2026-06-05 (exploration)
---

> **SUPERSEDED 2026-06-11** — replaced by [`compiler-readiness-plan-2026-06-11.md`](../compiler-readiness-plan-2026-06-11.md). Retained for history; all still-valid obligations were mined into that plan (see its `-appendices/working-docs-triage.md`). Do **not** treat as current strategy.


# The Precept Compile-Time Guarantee — Draft Statement

> **Altitude.** This states the *scope* — what "if it compiles, X" promises. It introduces **no new language surface** and grounds every clause in already-locked canon (§0.7, §3A.4, §2.4, `philosophy.md`). Two elements are explicitly **not settled** and flagged inline: the *certain/possible/impossible* vocabulary (needs `/design`) and the *prove-or-reject-vs-govern* line for statically-decidable-but-unproven rules. Intended, once locked, to consolidate the guarantee-thesis halves of the two 2026-06-05 strawmen.

## The guarantee, in one sentence

**If a precept compiles without diagnostics, then no contract operation (Create / Fire / Update) can commit a result that violates any declared rule, and no operation can fault at runtime — proven where statically decidable, and governed-at-the-result (atomic, with discard) for whatever depends on input the compiler never saw.**

## It guarantees two distinct things

### 1. No faults — operation totality
No division by zero, overflow, sqrt-of-negative, empty/out-of-bounds collection access, null dereference, or unit mismatch. These are intrinsic to an expression *being well-defined* — they are **not** declared rules; you cannot write them as a `rule`. They are **prevention-only**: a fault happens *during* evaluation, before any post-mutation check could run, so it must be **proven at compile time or the definition is rejected** — never deferred to runtime (§0.7, fault-prevention). The runtime fault traps exist only as defense-in-depth.

### 2. No rule violation persists — rule preservation
Every declared rule stays true of the committed entity. **Bound modifiers (`min`/`max`/`maxlength`/`maxcount`/…) are sugar for rules** (§2.4:1135), so this is *one* obligation, not a separate "bounds" guarantee. The canonical formulation (philosophy.md:51):

> "Whether the rule is about what the entity is or what the operation brings in, it evaluates atomically — the change and the check are the same act. No operation can produce a result that violates a declared rule."

*How* each rule is upheld depends on whether the compiler can decide it:

- **Statically decidable and proven** → the rule cannot be violated by construction. The runtime check is pure defense-in-depth.
- **Statically decidable in shape, but not proven as written** (e.g. `set Balance = Balance - amount` with no guard, in-fragment but unconstrained) → **[DISCUSSION POINT A — unresolved]** either *rejected with a fix* (strict; mirrors how faults work) or *governed at runtime* (lenient). Not settled here.
- **Depends on runtime input** → **governed at the result.** The operation runs on a working copy; the full rule set is re-checked against the completed copy; the copy is **discarded on any violation** (§3A.4), so nothing invalid ever commits. This is **prevention, not detection** — the change and the check are one atomic act — and it is the *necessary* handling for input the compiler never saw, **not a weak fallback**: §0.7 Composition makes governance the *discharged precondition* of the compile-time proof, and philosophy.md:55 names it "not a second line of defense."

The deep point (canon, §0.7 Composition): you **cannot statically prove a property of input not yet received.** So governing that input at ingress is not a concession — it is intrinsic. The compiler proves the *structural* fact that a value carries its constraint; governance makes that constraint *true of the concrete value* when it arrives.

## Mechanism — **DESIGN INTENT, runtime is stubbed**

> Everything in this section is *designed*, not built: `Evaluator.Fire/Update/Inspect/Create` all throw `NotImplementedException`. It cannot be cited as observed behavior; it is the contract the runtime must implement.

- Two and only two contract input channels are gated: **event arguments** (Create/Fire) and **direct field edits** (Update). No third bypass lane.
- Ingress validation is **per-field and operation-blind** — each incoming value is checked against *its own field's* contract.
- **Relational / multi-field / computed-field rules** (e.g. `A + B == Total`) are **not** applied to the input; they are enforced by the **post-mutation sweep on the working copy** (§3A.4), discarded on failure. Ordinary rules **do not reverse-map to arguments** — only event `ensure`s target args (§3A.3). *(So "rules applied to inputs at the edge" is imprecise; "governed at the result, atomically" is exact.)*
- **Inspect mirrors Fire/Update structurally** — same guards, same sweep, differing only in commit-vs-discard — so the edge can honestly answer "would this input be rejected?" (§3A.6). This equality is asserted by design; it is stubbed, not demonstrated.

## The boundary — what is **not** covered

- **Restore / host-injection.** Restore is trusted hydration from storage and is **not re-validated** (§0.7:272) — "trusted as valid at the time it was persisted." So the precise claim is **"no *contract* input path can commit an invalid result,"** not "no input path ever." Restored stale data can sit invalid until the next contract operation's sweep re-governs it.
- **Out-of-fragment rules** (variable×variable products, unbounded aggregates like `sum`). These are **governed only**, never proven — honestly labelled, never faked as proven.

## Proposed framing — certain / possible / impossible **(NEEDS `/design` — not a settled decision)**

A clean way to *talk about* the above:
- **Impossible** → rejected statically (the operation can never succeed).
- **Certain** → proven at compile time (the structural fact holds).
- **Possible** → governed at the result when the concrete input arrives.

**Honest caveat (do not cite as prior decision):** this tri-state exists today only as the runtime `Prospect` enum (`Inspection.cs:6` — "row-level certainty in first-match routing"), an *inspection* annotation under partial args. The clean static↔runtime mapping above is a *new synthesis*, and it conflates runtime `Prospect.Impossible` (a verdict over a concrete pre-state) with compile-time graph/proof rejection (a different stage with a different soundness regime — note the graph analyzer is currently guard-blind, so its "impossible" is conservative). Adopting this as guarantee vocabulary must go through `/design` with four-leg rationale; it must not be asserted as "already settled." **[DISCUSSION POINT B]**

## Built vs promised — honesty

This statement is the **target**, not a description of current behavior. The gap, tracked separately:
- **Proven today:** faults within the bounded proof fragment (divisor non-zero from narrowing, overflow on set-actions, count bounds, single-field bounds partially).
- **Not built:** the **runtime evaluator / sweep is stubbed** — so the *entire governance half is currently design intent*. **Relational rule preservation** is marked "Not built." And **obligation-position completeness has 10 live holes** (guards, messages, defaults, member-args) — so even the prevention half does not yet cover every position it claims.

## Open discussion points
- **A.** Prove-or-reject vs govern for statically-decidable-but-unproven-as-written rules.
- **B.** Adopt certain/possible/impossible as guarantee vocabulary? If so, reconcile the two distinct "impossible" surfaces.
- **C.** Where a value-level external violation actually lands — `InvalidArgs` vs `ConstraintsFailed` vs a `Faulted` FaultCode (the 2026-06-01 clarity assessment flagged this; still unspecified).
