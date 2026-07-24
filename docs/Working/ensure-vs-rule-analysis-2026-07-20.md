---
title: Is an `ensure` just another `rule` spelling? — a constraint-form analysis
status: Draft analysis — 2026-07-20 — question answered, NO language change proposed. One narrow internal-representation consolidation flagged for a future separate /design pass with owner sign-off.
author: Frank (Lead/Architect & Language Designer)
requested-by: Shane (project owner)
scope: Semantic-equivalence + implementation-state analysis ONLY. Does NOT propose new syntax, does NOT imply a locked design decision, does NOT change any language surface, timing, or diagnostic.
grounding: docs/language/precept-language-spec.md §3A.1 :1868-1887, §3A.3 :1952-1958, :584, :2108; src/Precept catalog Constraint.cs; SharedTypes.cs; SemanticIndex.cs; TypeChecker.Normalization.cs; ProofEngine.cs / ProofEngine.Analysis.cs / ProofEngine.Satisfiability.cs; EventOutcome.cs; samples/refund-request.precept, samples/loan-application.precept, samples/insurance-underwriting.precept, samples/it-helpdesk-ticket.precept; empirical precept_compile on both forms.
---

# Is an `ensure` just another `rule` spelling?

## Verdict

**`ensure` is not sugar for `rule`.** The proposed rewrite
`rule Balance >= 0 when state is Frozen` (offered as equivalent to
`in Frozen { ensure Balance >= 0 }`) is **not even expressible** in the current
language. `state` is a reserved keyword, not referenceable in guard expressions,
and there is no `is <StateName>` state-test operator in the expression grammar.

Empirically confirmed via `precept_compile`: the rewrite throws **PRE0009** parse
errors (4 diagnostics), while `in Frozen ensure Balance >= 0 because "..."`
compiles clean (1 ensure / 0 rules). `ensure` and `rule` are **peer constraint
forms on orthogonal axes**, not two spellings of one construct.

**No language surface should change on the basis of this analysis.** One narrow
*internal-representation* consolidation opportunity is flagged at the end — it is a
future, separate `/design` question requiring owner sign-off, not actioned here.

---

## Key findings

### 1. Semantic-equivalence check — the rewrite is inexpressible

`state` is a reserved keyword (spec `precept-language-spec.md:584`), not an
identifier; no `is <StateName>` operator exists in the expression grammar. Spec
`:1868` — "Rules operate in field scope" — guards read **fields only**, not state.
`in Frozen` is a **routing preposition** resolved into structural metadata
(`ConstraintKind.StateResident` + `AnchorState`), not a runtime predicate. There is
no legal expression form that turns "resident in state S" into a boolean guard.

### 2. Activation timing — five distinct `ConstraintKind`s with different timings

Per `SharedTypes.cs:17-30` and spec §3A.1 `:1884-1887`:

| Surface form | ConstraintKind | Activation |
|---|---|---|
| `rule` | `Invariant` | after every mutation, always (level-triggered, global) |
| `in S ensure` | `StateResident` | while resident in `S` (level-triggered, state-scoped) |
| `to S ensure` | `StateEntry` | only on transition **into** `S` (edge-triggered) |
| `from S ensure` | `StateExit` | only on transition **out of** `S` (edge-triggered) |
| `on E ensure` | `EventPrecondition` | against caller args **before** `E`'s mutations (ingress) |

`to`/`from` are edge-triggered — NOT evaluated in Tier 2, only in Tier 3 at fire
time (`SharedTypes.cs:22-27`). A level-triggered guard rewrite **cannot express**
`to`/`from`'s edge-triggered truth. Spec `:2108` confirms stateless precepts have
`to`/`in` ensures **structurally absent**, not conditionally skipped.

### 3. Proof-engine treatment — shared mechanism, distinct diagnostics

- `ScanRulesAgainstDefaults` (`ProofEngine.Satisfiability.cs:82`) →
  `DefaultViolatesRule`.
- `CheckInitialStateSatisfiability` (`ProofEngine.cs:187`,
  `ProofEngine.Analysis.cs:57`) → `UnsatisfiableInitialState`, folding **only**
  `StateResident` ensures for the initial state (`to`/`from` excluded — they are
  edge-triggered and defaults are not their activation environment).

The rule-fold code comment confirms it "mirrors the initial-state ensure fold…
reusing the same default environment and the same `ConstantFold` evaluator."
**Mechanism shared; obligations distinct.**

### 4. Failure mode / attribution differ

Both surface as `EventOutcome.ConstraintsFailed` (`EventOutcome.cs:28`) — the same
fault channel — but **attribution differs** (spec §3A.3 `:1952-1958`):

- `rule` → referenced fields + definition scope
- state `ensure` → fields + state scope with anchor
- event `ensure` → event args + event scope

`ConstraintDescriptor.ScopeTarget` (`SharedTypes.cs:47`) carries the state/event
name for ensures, `null` for invariants. **A rewrite to `rule` would erase anchor
provenance from violations** — a regression against the inspectability guarantee.

---

## Sample evidence

- **`refund-request.precept`** — global `rule ApprovedAmount <= RequestedAmount`
  (`:27`) alongside residency ensures `in AwaitingReturn ensure ApprovedAmount >
  '0.00 USD'` and `in Refunded ensure ReturnReceived` (`:44-45`). The rule is
  genuinely global; the ensures are lifecycle-local.
- **`loan-application.precept`** — `rule ExistingDebt <= AnnualIncome * 3.0 when
  DocumentsVerified` (`:38`), a **field-guarded conditional rule**, alongside
  `in Funded ensure ApprovedAmount > '0.00 USD'` (`:48`). Clean contrast: `when`
  guards on a **field**; `in Funded` anchors on a **state**. The language already
  HAS a "conditional rule" (field-guarded) and it visibly **isn't** what a state
  ensure is.
- **`it-helpdesk-ticket.precept`** — `in Assigned ensure AssignedAgent is set`
  (`:52`) reads spatially ("while in this state"), not conditionally.
- **`insurance-underwriting.precept`** — seven `in <State> ensure` residency checks
  (`:86-94`).
- Recurring idiom across ~65 sample files: **"invariant while in this state"** —
  `in S ensure` is the direct spelling.

---

## The three questions, answered

### Q1 — Is `ensure` semantically just a conditional rule?

**No.** A rule-when-state rewrite loses: (a) the ability to anchor on the state
coordinate at all; (b) edge-triggered entry/exit semantics for `to`/`from`;
(c) anchor-scoped attribution + distinct proof obligation
(`UnsatisfiableInitialState` vs `DefaultViolatesRule`). A hypothetical
`rule … when state …` form would collapse five `ConstraintKind`s into one lossy
form.

### Q2 — Is it redundant? Confusion or value?

**Not redundant.** `when` guards on **fields**; `in`/`to`/`from`/`on` anchor on
**states/events** — orthogonal axes, not two spellings of one thing. The
distinctness adds value: the domain-expert reading matches the domain concept,
anchor provenance is preserved for inspection, and the compiler picks the correct
proof obligation per anchor. The only legitimate "two ways to say something
similar" is global `rule` vs. an in-every-state `ensure`, and even those differ in
**fold behavior** and **attribution**. Tradeoff: authors learn five constraint
anchors instead of one — paid back by readability and per-anchor reasoning.

### Q3 — If it were sugar, should the compiler desugar it?

The premise fails (it is not sugar), but the implementation state is worth naming
precisely:

- **Catalog layer — already unified.** `Constraint.cs` has one catalog of five
  `ConstraintKind`s with `StateAnchored` as a shared abstract DU subtype. Correct
  DU-as-identity shape.
- **Typed model layer — deliberately split.** The TypeChecker normalizes `rule`
  into `TypedRule{Condition, Guard, Message}` and every `ensure` into
  `TypedEnsure{Kind, AnchorState, AnchorEvent, …}` (`SemanticIndex.cs:569,577`;
  `TypeChecker.Normalization.cs:114,196`). **Asymmetry:** the catalog says `rule` is
  `ConstraintKind.Invariant`, but the typed model keeps `rule` as a **separate
  record type**, not `TypedEnsure{Kind=Invariant}`.
- **Proof / runtime layer — shared fold evaluator + default environment + fault
  channel**, but two separate scan entry points emitting two diagnostics.

Honest state: **mechanism is already unified where it should be** (fold engine,
fault channel, catalog); **surface identity is kept distinct where it must be**
(typed model, obligations, attribution). Fully collapsing `TypedRule` into
`TypedEnsure{Kind=Invariant}` is feasible and would remove the asymmetry, but it is
**REPRESENTATION consolidation, not desugaring** — `rule` would become the
degenerate anchor-less `ensure`, not the other way around. That is a genuine design
question requiring its own `/design` pass, **not implemented here.**

---

## Four-leg rationale

- **Rationale** — `ensure` is a peer constraint form, not sugar for `rule`.
  State-anchoring is structural metadata resolved by the parser into a
  `ConstraintKind`, not a boolean guard, and it encodes activation timings the guard
  sublanguage cannot express.

- **Alternatives rejected** —
  - *"`ensure` = `rule when state==X`"* — rejected: `state` is unreferenceable and
    edge-triggered `to`/`from` are inexpressible by a level-triggered guard.
  - *"Collapse into one runtime path"* — partially already true (shared fold
    engine / fault channel) but must **not** collapse five timings or two diagnostic
    codes; the full `TypedRule`→`TypedEnsure` merge is a separate design question.
  - *"They're redundant, drop one"* — rejected: orthogonal axes; dropping ensures
    makes lifecycle-local truth inexpressible and erases anchor provenance.

- **Precedent** — spec §3A.1 `:1872-1887`, §3A.3 `:1952-1958`, `:1868`, `:2108`;
  catalog `Constraint.cs` (five `ConstraintKind`s, `StateAnchored` DU); typed model
  `SemanticIndex.cs:569,577`; normalization `TypeChecker.Normalization.cs:114-266`;
  proof engine `ProofEngine.Analysis.cs:57`, `ProofEngine.Satisfiability.cs:82`,
  `ProofEngine.cs:187`; runtime tiers `SharedTypes.cs:17-56`; samples
  `refund-request:27,44-45`, `loan-application:38,48`,
  `insurance-underwriting:86-94`; empirical `precept_compile` on both forms
  (rewrite → 4× PRE0009; real form → compiles clean, 1 ensure / 0 rules).

- **Tradeoff accepted** — five constraint anchors instead of one uniform "guarded
  rule" is more surface to learn; the catalog/typed-model asymmetry (`Invariant`
  exists as a kind but `rule` isn't stored as `TypedEnsure`) is a small
  architectural wart. Both worth it — anchors buy domain-expert readability,
  per-anchor proof obligations, and anchor-scoped inspectability, all first-order
  philosophy commitments.

---

## Recommendation (owner-gated, NOT actioned)

The catalog declares `ConstraintKind.Invariant` as a peer of the four anchored
kinds, but the typed model keeps `rule` as a separate `TypedRule` record rather than
`TypedEnsure{Kind=Invariant}`. Unifying the typed model — making `rule` the
anchor-less member of the ensure family — would tighten catalog↔code alignment and
let the two proof-scan paths share more structure, **without changing any language
surface, timing, or diagnostic**.

This is an **internal representation consolidation, not a language change**, but it
touches the proof engine and the diagnostic split, so it needs a `/design` pass and
Shane's sign-off before anyone opens an editor. **Not proposed for action now — only
named for future consideration.** No language surface should change on the basis of
this analysis.
