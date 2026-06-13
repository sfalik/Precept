---
status: Proposal / Draft
authored: 2026-06-06
author: proposal (horizon-groundwork)
topic: Strengthening Precept's compile-time prevention — (Part 1) how much more proof coverage is reachable solver-free within the per-keystroke budget, and (Part 2) forcing authors to declare governed-vs-prevention intent at the grammar level
governing-constraints:
  - "R1 — no opaque solver; every verdict carries a legible witness (spec §0.6 #3)."
  - "R2 — per-keystroke LS performance: predictable, bounded worst case (no data-dependent blowup). Baseline corpus ~44ms, worst file ~3ms."
research-backbone: research/architecture/compiler/solver-free-static-analysis-techniques-survey.md
---

> **SUPERSEDED 2026-06-11** — replaced by [`compiler-readiness-plan-2026-06-11.md`](../compiler-readiness-plan-2026-06-11.md). Retained for history; all still-valid obligations were mined into that plan (see its `-appendices/working-docs-triage.md`). Do **not** treat as current strategy.


# Strengthening compile-time prevention in Precept

> **Altitude.** This is a *proposal*, not a locked design. It presents candidate features, their tradeoffs, and a recommended ordering, so an owner conversation and a later `/design` pass have a verified starting point. It is **not** a four-leg-per-decision spec and does not lock anything. Every Precept construct shown — existing or proposed — was verified against the catalog source and/or `precept_compile` (evidence inline). Proposed *new* surface is marked **PROPOSED (illustrative syntax)** wherever it appears.

## What this proposal rests on (verified)

Two facts about today's engine ground everything below. Both were verified live against `precept_compile` (MCP, `precept_ping` → `ok`, current working tree) during authoring.

**Fact A — the relational fragment boundary is real and observable.** A single precept with two rules and two divisors:

```precept
field X as integer default 10
field Y as integer default 3
field A as integer default 4
field B as integer default 6
field Total as integer default 10
field R1 as integer default 1
field R2 as integer default 1

rule X > Y because "X must exceed Y"
rule A + B == Total because "A plus B equals total"

state Open initial
event Go
from Open on Go
  -> set R1 = 100 / (X - Y)        # PROVED   (FlowNarrowing, from rule X > Y)
  -> set R2 = 100 / (Total - A)    # UNRESOLVED → PRE0083
  -> no transition
```

Verified result: `100 / (X - Y)` discharges **`{"disposition":"Proved","strategy":"FlowNarrowing"}`** — the two-field, unit-coefficient relation `X > Y ⟹ X − Y > 0` is exactly the shipped relational core (spec §0.6 item 2). The sibling `100 / (Total - A)` returns **`{"disposition":"Unresolved"}`** and emits **`PRE0083` "Division is unsafe: '(Total - A)' can be zero"`** — even though `rule A + B == Total` makes `Total − A == B` and `B`'s interval is `[6,6]` (or, in the general case, `B ≥ 0`). The engine does not thread the three-field equality. **This is the single recurring out-of-fragment numeric invariant in the corpus** — the same shape appears as `rule PaymentsMade + PaymentsMissed <= PaymentsScheduled` (`samples/equipment-lease-agreement.precept:79`, verified present) and across the balance-bearing samples.

**Fact B — enforcement intent is invisible to the author.** There is no grammar slot, keyword, or modifier today that lets an author say "this rule must be compile-time *proven*" versus "this rule is accepted as runtime-*governed*." Verified: `rule Q >= 0 prevented because "..."` and `rule Q >= 0 because "..." prevented` both fail to parse — **`PRE0009` "Expected because here, but found 'prevented'"** / **"Expected declaration keyword here, but found 'prevented'"**. The rule grammar (`precept-language-spec.md:935`) is exactly `rule BoolExpr ("when" BoolExpr)? because StringExpr` — no intent annotation exists. Whether any given rule lands in the proven set or the governed set is decided silently by whether the proof engine *happens* to discharge a downstream obligation that references it. That is a direct tension with **Principle 8, approximation honesty** ("the line between exact and approximate behavior must be visible in the type system and the language surface") and **§0.6 #6, proof attribution is required**.

Part 1 attacks Fact A (get more prevention). Part 2 attacks Fact B (make the prevented/governed line visible and author-controlled).

---

# Part 1 — How much more compile-time prevention can we get?

## 1.1 The prevented-vs-governed split today

Every declared rule **is enforced** — that is never in question. The runtime post-mutation sweep (§3A.4) re-checks every constraint against the completed working copy and discards it on any violation, and ingress governance (§0.7) validates external input the moment it enters. So `rule A + B == Total` is *governed*: if a `Fire`/`Update` produces `A + B ≠ Total`, the operation is rejected. The invalid configuration never persists.

What the engine does *not* do for that rule is **prevent** — i.e., use it as a proof fact to discharge a downstream fault-prone obligation, or prove a peer constraint can never be violated by construction. The governance/prevention split is precisely the **§0.7 contract**: *governance* (runtime, every external value) versus *fault prevention* (compile-time, prove-or-reject, never deferred). A rule that participates in proof moves work from the runtime sweep into the compiler; a rule that doesn't stays a pure runtime check.

The gap Part 1 closes is **not** "rules go unenforced" — it is "rules authors already write could *prevent* downstream faults but currently only *govern*." Most of Part 1 is therefore **no new author surface** — teach the proof engine to discharge rules authors already write.

## 1.2 The ranked techniques, each tied to a real rule it would newly prevent

Ordered by value-per-cost within R1+R2, following the research backbone's C1–C4 ranking (`solver-free-static-analysis-techniques-survey.md`). Each row states: the **existing rule** that compiles-clean-but-only-governs today (verified), what the technique newly **prevents**, the **witness** (R1), and the **R2 fit**.

### Feature 1 — Karr affine-equality discharge (the #1 win)

**Classification: no author surface — new proof capability over rules authors already write.**

- **Real rule that only governs today.** `rule A + B == Total` and `rule PaymentsMade + PaymentsMissed <= PaymentsScheduled` (`samples/equipment-lease-agreement.precept:79`). Verified: a divisor `100 / (Total - A)` under `rule A + B == Total` is **Unresolved → PRE0083** (Fact A above). The three-field linear identity is provably outside the octagon fragment (octagons represent only `±x ± y ≤ c`) but provably *inside* Karr's affine-equality domain.
- **What it newly prevents.** Karr's domain tracks linear equalities `a₀ + Σaᵢxᵢ = 0` of any arity. With it, `Total − A` reduces to `B` along the equality basis, so `100 / (Total − A)` discharges whenever `B`'s interval excludes 0 — *without* the author rewriting the divisor over `B` by hand. It also lets a balance identity *contribute a bound*: from `A + B == Total` with `A, B ≥ 0`, the engine learns `A ≤ Total` and `B ≤ Total`, sharpening every downstream interval that reads `A` or `B`.
- **Witness (R1).** A Gaussian-eliminated equality basis — a finite system of linear equalities, structured data, exactly the legible artifact §0.6 #3 demands. "`Total − A` is provably `B` because `rule A + B == Total` (line N)" is a domain-readable attribution, not a solver trace.
- **R2 fit.** Müller-Olm/Seidl: *linear in program size, polynomial in variable count* (`O(program · k³)`, k = fields). Deterministic, no fixpoint (Precept has no loops, §0.4), no widening. Per-precept k is small (<~50 fields in the corpus). Safely within the ~3ms worst-file budget.
- **Adoption shape.** As a *reduced-product component* alongside the shipped interval+octagon core (Apron's architecture), not a replacement — each domain keeps its own witness; the reduction is a bounded fact-exchange pass. The research flags one open question to settle in design: whether the existing single-pass `narrowed`-dictionary architecture admits an equality component without a fixpoint (`ProofEngine.Intervals.cs` probe required before locking).

### Feature 2 — Incremental numeric aggregates (extend the shipped count-delta walk)

**Classification: no author surface — new proof capability, reusing shipped machinery.**

- **Real rule that only governs today.** `rule TotalPaid <= TotalScheduledValue` (`equipment-lease-agreement.precept:80`), maintained across `set TotalPaid = TotalPaid + ...` mutation chains; and the cardinality-adjacent monotonic rule `rule ItemCount <= 1000` (`samples/shopping-cart.precept:53`). The engine already proves the *count* axis: verified, `add Items a -> add Items b` into `maxcount 1` rejects with **`PRE0136` "Cannot prove `Items` stays within `maxcount 1` after this add — guard with `when Items.count < 1`."** That is the Strategy-10 per-kind/per-action count-delta walk (spec §0.6, "count interval `[lo,hi]` … advanced by each mutation's sound per-kind/per-action delta").
- **What it newly prevents.** Extend the same delta machinery from cardinality to **numeric magnitude** bounds: a `set X = X + e` where `e ≥ 0` can only *increase* `X` (monotonicity), so a `maxcount`-style upper-bound rule on a running total becomes carrier-discharged the same way count bounds are. Closes the "running total stays within its declared band across a mutation chain" class without an author guard on every step.
- **Witness (R1).** The delta chain — "after `set TotalPaid = TotalPaid + P` with `P ≥ 0`, the interval moved from `[lo,hi]` to `[lo, hi+max(P)]`" — the same per-mutation delta the count walk already emits.
- **R2 fit.** Per-mutation O(1) delta, identical cost profile to the shipped count interval. Near-zero marginal cost.
- **Honest scope.** Sound only where the per-mutation delta is sign-determined (monotone). A `set X = arbitrary-expr` reassignment invalidates the running interval (sequential proof flow, §0.6 #7) — the same conservative direction the count walk already takes.

### Feature 3 — Dominator-based fact propagation (reuse the §0.5 dominators)

**Classification: no author surface — new proof capability, reusing shipped machinery.**

- **Real rule that only governs today.** A constraint established by a `required` state's ensure that a downstream state's expression depends on — e.g., `in Approved ensure DocumentsVerified` (`samples/loan-application.precept:46`) where a later `Funded`-state expression reads `DocumentsVerified`. Today the later site cannot reuse the earlier ensure as a proof fact across the transition.
- **What it newly prevents.** The graph analyzer already computes dominators (Lengauer–Tarjan, §0.5 #4, used for the `required` modifier). A fact that holds on *every* path dominating a use-site is true at that use-site, so a guard/ensure fact can be propagated forward to dominated rows without re-proving — sharpening cross-row proof reuse (e.g., a non-zero or presence fact established before a dominator carries into the dominated divisor/accessor).
- **Witness (R1).** The dominating row + the dominance path — "`DocumentsVerified` holds here because state `Approved` (which establishes it) dominates every path to this site."
- **R2 fit.** Dominators already computed; the propagation is a graph walk over an existing structure. Sound by §0.5's overapproximation rule (structural dominance, the conservative direction).

### Feature 4 — DBM-emptiness guard-feasibility → completability/liveness

**Classification: no author surface — new proof capability, reusing the PRE0154/0155 contradiction scan.**

- **Real gap that stays governed today.** The engine already detects whole-construct contradictions: verified earlier, an always-true rule emits **`PRE0154` "… is always true under the declared constraints — it governs nothing"** (vacuous-rule detection), and the Pass-1.5 scan emits PRE0155 for contradictory rule *pairs*. What it does not yet do is the *existential* mirror: "is this (state, event, guard, post-state) combination satisfiable at all?" — the completability/liveness question (can the entity always reach a terminal/required state under its data constraints, not just structurally).
- **What it newly prevents.** The "is guard ∧ post-state satisfiable" test is **DBM emptiness** (negative-cycle detection) — the same polynomial machinery as the existing PRE0154/0155 interval-contradiction scan. It under-approximates liveness *with a witness*: it certifies a path is takeable by exhibiting a satisfying constraint set, never certifies one it can't exhibit.
- **Witness (R1).** The negative cycle = the contradicting constraint set (for the infeasible direction); the satisfying corner (for the feasible direction).
- **R2 fit.** O(n³) closure in small n, same bound as octagons. Safe. (Established in `liveness-completability-verification-survey.md`; cited forward.)

### Feature 5 (optional tier) — TVPI-rational for non-unit-coefficient two-variable relations

**Classification: no author surface — new proof capability; lower priority (thinner demand).**

- **Real rule that only governs today.** `rule SecurityDeposit <= MonthlyPayment * 3` (`equipment-lease-agreement.precept:78`), `rule ExistingDebt <= AnnualIncome * 3.0 when DocumentsVerified` (`samples/loan-application.precept:38`), `rule ApprovedAmount * 2 <= ClaimAmount when FraudFlag` (`samples/insurance-claim.precept:49`). These are `X ≤ c·Y` with a non-unit coefficient — provably outside the octagon fragment (octagons are unit-coefficient only).
- **What it newly prevents.** TVPI expresses `a·x + b·y ≤ c` with arbitrary coefficients over ≤2 variables, strongly polynomial on the rational path — letting these scaled relations contribute a provable bound to a downstream obligation, the same way `X > Y` does today.
- **Witness (R1).** A planar-polyhedra inequality list per variable pair.
- **R2 fit / caveat.** Strongly polynomial over rationals (Precept's fields are decimal-backed → the rational path is the relevant one). The **integer-hull** tightening is NP-complete and must be the documented *approximation*, never the exact algorithm — which is why the research rates TVPI **borderline** and ranks it *after* Karr. Adopt only if a design pass confirms the demand (the corpus demand for scaled relations is real but thinner than for the balance identity).

### Feature 6 (optional tier) — Congruence closure for equality/disequality

**Classification: no author surface — new proof capability; lowest priority (scattered demand).**

- **What it would prevent.** Equality/disequality reasoning over field references (e.g., structural `X == Y` facts feeding a sign/divisor decision), via a union-find computation. O(n log n) with a *proof-producing* k-step explanation witness — a decision procedure that is explicitly **not** an opaque solver (the union-find path is the witness). Lowest priority because corpus demand is scattered.

### What we explicitly decline (the R2/R1 traps)

- **Convex polyhedra — declined.** Would prove arbitrary N-variable linear relations, but the double-description method is **exponential worst-case**. PPL itself mitigates with a timeout that falls back to zones — and a *timeout makes verdict latency data-dependent*, which a per-keystroke recompile (R2) cannot tolerate, and which also breaks determinism (Principle 3 / §0.6 #1). This is the line R2 draws, and the research confirms it is the *right* ceiling, not a regrettable limit.
- **Predicate abstraction — declined.** Its classical abstraction step uses a theorem prover (fails R1). The solver-free degenerate case adds nothing over the engine's existing guard narrowing.

## 1.3 How far it goes — the honest ceiling

Even with all of the above, a class of rules stays **irreducibly governed** — provable only by the runtime sweep, never at compile time — and that is the polyhedra/solver line, not a failure:

- **Genuine products and ratios as constrained rules.** `rule TotalInventoryCost == AverageCost * QuantityOnHand` is nonlinear (variable × variable). Karr (equalities) and TVPI (≤2-var affine) both stop at affine; a constrained product needs a fixed-degree polynomial domain whose cost is `O(k^{3d})` — a new R2 exponent the corpus doesn't currently justify. These stay governed.
- **Unbounded dynamic aggregates.** `rule Total == sum(Fees)` over a `lookup` cannot even be *expressed* — verified: `sum` returns **`PRE0030` "'sum' is not a recognized function"** (the locked rejection in `collection-types.md:860`, where `sum`/`reduce` are "deliberately held back … would cross the line from predicate into computation"). The real samples maintain these identities by *paired actions* keeping a `set`/`lookup` and a running-total field in sync (the AddOn pattern in `samples/event-venue-booking.precept`), governed by the sweep, never proven. An aggregate over a runtime-variable-cardinality collection is the canonical dynamic-aggregate gap; it stays governed by design.
- **Distributional / statistical constraints.** Anything over a distribution (variance, percentile) is outside every solver-free numeric domain surveyed.

The boundary is exactly the **convex-polyhedra / solver line**: weakly-relational inequalities (octagons, shipped) + affine equalities (Karr, Feature 1) + scaled two-variable inequalities (TVPI, Feature 5) is the reachable frontier under R1+R2. Everything past it is either nonlinear or unbounded-dynamic, and stays honestly governed.

**Crucially, the ceiling does not weaken the guarantee — it relocates it.** A governed-but-not-prevented rule is still enforced on every operation by the sweep; the invalid configuration still cannot persist. What Part 2 ensures is that the author *knows which side of the line each rule is on*.

## 1.4 Part 1 recommendation and ordering

1. **Feature 1 (Karr affine equalities)** — highest value-per-cost; closes the one recurring out-of-fragment invariant; gate on the `ProofEngine.Intervals.cs` architecture probe.
2. **Features 2–4 (incremental aggregates, dominator propagation, DBM-emptiness)** — cheap, machinery-reusing, independent of the Karr layering; ship next.
3. **Features 5–6 (TVPI-rational, congruence closure)** — optional tiers, adopt only if a design pass confirms demand.
4. **Decline** convex polyhedra and predicate abstraction permanently on the interactive path.

All of Part 1 is **no new author surface** — the `.precept` files don't change; the engine simply prevents more of what authors already wrote. Which is exactly why Part 2 is needed: the author can't see that their rule moved from governed to prevented (or silently degraded the other way).

---

# Part 2 — Forcing explicit governed-vs-prevention at the grammar level

## 2.1 The problem, restated as a language-honesty gap

Part 1 makes the prevented set *larger*, but it does not make the prevented/governed boundary *visible or stable*. Today (Fact B, verified): an author writes `rule A + B == Total because "..."` and has **no way to know, and no way to require**, whether the compiler proves anything from it or merely governs it at runtime. Worse, the boundary is *fragile*: a one-character edit (reassigning a field the rule references, which drops the proof fact per §0.6 #7) can silently move a rule from prevented to governed with no diagnostic. That is a Principle-8 (approximation honesty) violation hiding in plain sight: the exact/approximate line is real but invisible.

## 2.2 What exists today (verified)

- **No enforcement-intent modifier or keyword.** `Modifiers.cs` (read in full) declares every modifier as `ModifierCategory.Structural` or `.Semantic`; **none** carries a "must-be-proven" vs "accept-as-governed" axis. The value modifiers (`min`, `max`, `nonnegative`, …) carry `ProofSatisfactions` — they describe *what* a constraint means to the proof engine, never *whether the author requires* it to be proven.
- **No rule-level annotation slot.** Verified: `prevented` is a parse error in every rule position (`PRE0009`). The grammar is `rule BoolExpr ("when" BoolExpr)? because StringExpr` with no trailing or leading intent token.
- **The proof outcome is observable, but only out-of-band.** `precept_proofs` and LS hover surface obligation dispositions (`Proved`/`Unresolved`) — §0.8 #4. But that is *inspection*, not *declaration*: the author can look up what happened, but cannot *require* an outcome and have the compiler enforce the requirement. There is no construct that says "reject this definition if you can't prove this."

So the raw material exists (the proof engine already classifies every obligation `Proved`/`Unresolved`, and `PRE0154`/`PRE0083` already speak in those terms) — what's missing is an author-facing **declaration** of intent and a compiler rule that **enforces** it.

## 2.3 PROPOSED mechanism — a rule-level enforcement-intent annotation

**Classification: genuinely new author surface (a new rule-level modifier). Does not touch a locked rejection** — there is no prior locked decision on enforcement-intent annotation in `precept-language-spec.md`, `business-domain-types.md`, or `collection-types.md` (checked; the locked rejections there concern `sum`/`reduce`, `subset`/`disjoint`, `units {}`, multimap — none is about prevention-intent).

The shape under consideration: an optional **enforcement-intent adjective** on a rule (and, symmetrically, on an `ensure`), occupying a new grammar slot. Two illustrative spellings are sketched; the choice between them is a `/design` question, not settled here.

### PROPOSED (illustrative syntax) — spelling A: trailing adjective

```precept
# PROPOSED — not current grammar
rule A + B == Total prevented because "Available plus allocated must equal total capacity"
rule Total == sum(Fees) governed because "Total is maintained in sync with the fee lookup by paired actions"
```

- `prevented` = "I require the compiler to *prove* this is upheld by construction (or used to discharge the faults that depend on it). If it cannot, **reject the definition** and name the fix."
- `governed` = "I accept this as a runtime-enforced constraint; do not require a compile-time proof." (The honest escape hatch for the irreducible ceiling — `sum`, products, etc.)

Grammar delta (illustrative): `rule BoolExpr Intent? ("when" BoolExpr)? because StringExpr`, where `Intent := "prevented" | "governed"`. The trailing-before-`because` slot mirrors how `initial` trails the event parameter list (§3A.5) — a known precedent for a post-construct adjective.

### PROPOSED (illustrative syntax) — spelling B: a modifier-style prefix

```precept
# PROPOSED — not current grammar
prevented rule A + B == Total because "..."
governed  rule Total == sum(Fees) because "..."
```

Reads as an adjective on the construct, consistent with state modifiers (`terminal state X`). Tradeoff vs spelling A: prefix is more scannable but competes visually with the `rule`/`ensure` leading-keyword dispatch the parser relies on (§0.8 #1). Spelling A keeps the leading keyword clean. Lean: **spelling A**, pending design.

### Default behavior (the load-bearing decision)

Three candidate defaults; this proposal **recommends candidate (ii)** and flags it as the central owner conversation:

- **(i) Default `governed`.** Back-compatible (every existing rule keeps compiling), but preserves the invisibility problem for unannotated rules — the honesty win only applies to rules the author bothered to mark. Weak.
- **(ii) Default `prevented` where the engine *can* decide, with a mandatory annotation only when it *cannot*.** The compiler attempts proof on every rule; if it proves or refutes, fine; if it lands `Unresolved` AND the rule is of a *provable shape* (linear, in-fragment), it emits a new diagnostic **"this rule is governed-only; mark it `governed` to accept that, or supply the constraint that makes it provable."** This forces the choice *exactly at the boundary* — the author can't accidentally ship a rule that silently degraded, because the degradation now produces a diagnostic demanding an explicit `governed` acknowledgment. Out-of-fragment shapes (`sum`, products) require `governed` and the compiler tells the author so. **This is the strongest fit for Principle 8** — the line becomes visible precisely where it matters, and nowhere else as noise.
- **(iii) Mandatory annotation on every rule.** Maximally explicit but maximally verbose; fights the domain-expert audience (§0.8) and the constraint-modifier-shorthand ethos (§2.4). Rejected as too heavy.

### How `prevented` is enforced

A `prevented` rule whose proof lands `Unresolved` becomes a **hard rejection** with a new diagnostic (PROPOSED code, e.g. `PRE0XXX` "Rule marked `prevented` cannot be proven — the engine could not discharge it from the declared constraints; supply a bound on `B`, or mark the rule `governed`."). This reuses the *existing* `Proved`/`Unresolved` disposition the proof engine already computes for every obligation — the annotation just promotes an `Unresolved` from "silent runtime fallback" to "named compile error," and names the fix, exactly as §0.7 fault-prevention already does for divisor/overflow obligations.

### Interaction with the runtime sweep (§0.7 / §3A.4)

This is purely additive to enforcement and changes **nothing** about runtime behavior:

- A `governed` rule is enforced by the sweep exactly as today. The annotation only suppresses the "you could have proven this" diagnostic — it is an author *acknowledgment*, not a behavior change.
- A `prevented` rule that *does* prove is still *also* enforced by the sweep (defense-in-depth, §0.7 — the runtime fault traps remain as "defensive redundancy for paths the compiler has already proven unreachable," Principle 10). `prevented` does not strip the runtime check; it adds a compile-time *requirement* on top.
- The sweep stays the single runtime enforcement point; the annotation lives entirely at compile time. No new runtime construct, no new evaluator path.

### How it surfaces in diagnostics and inspection

- `precept_proofs` / LS hover already expose `Proved`/`Unresolved` per obligation (§0.8 #4). The annotation makes the author's *intent* a first-class field on the rule, so inspection can show **intent vs outcome** side by side ("declared `prevented`, outcome `Proved` ✓" / "declared `prevented`, outcome `Unresolved` → rejected" / "declared `governed`, outcome `Unresolved`, runtime-enforced").
- A new MCP/LS surface could list, per precept, the prevented set and the governed set — making the §0.7 split *legible at the file level*, which it currently is not.

### How it prevents accidental silent degradation

The degradation scenario today: an author edits a transition (e.g., adds `set X = ...`), which drops the proof fact per §0.6 #7, silently moving a previously-prevented rule into governed-only — with **no signal**. Under candidate-(ii) default: the moment that rule's downstream obligation goes `Unresolved` for a provable-shape rule, the compiler emits the "mark `governed` or fix" diagnostic. The author must *consciously* either restore provability or write `governed` — the silent degradation becomes a loud, blocking decision. That is the core honesty guarantee Part 2 delivers.

## 2.4 Part 2 recommendation

- Adopt a **rule-level (and ensure-level) enforcement-intent annotation** — recommend **spelling A** (`prevented`/`governed` adjective before `because`), **default candidate (ii)** (proven-where-decidable; force an explicit `governed` acknowledgment exactly at the fragment boundary).
- Reuse the proof engine's existing `Proved`/`Unresolved` disposition — no new proof machinery, only a new declaration + a new diagnostic that promotes a boundary `Unresolved` to a named, fixable rejection.
- Net effect: the §0.7 prevented/governed split — today an invisible, fragile, engine-internal accident — becomes an **author-declared, compiler-enforced, inspectable** property, satisfying Principle 8 (approximation honesty) and §0.6 #6 (proof attribution) at the language surface.

**Owner-conversation flags (this is Tier 2 new surface):** (1) the default-behavior choice (i/ii/iii) is the load-bearing decision and should be settled in conversation before `/design`; (2) whether the annotation applies to `ensure` symmetrically or to `rule` only; (3) spelling A vs B. Per the pre-design consultation gate, this proposal opens that conversation; it does not settle it.

---

## Verification log (every construct cited)

| Construct shown | Existing / Proposed | Evidence |
|---|---|---|
| `rule X > Y` divisor `(X-Y)` discharges | Existing | `precept_compile` → `{"disposition":"Proved","strategy":"FlowNarrowing"}` |
| `rule A + B == Total` divisor `(Total-A)` does NOT discharge | Existing (the gap) | `precept_compile` → `Unresolved` + `PRE0083` |
| `rule PaymentsMade + PaymentsMissed <= PaymentsScheduled` | Existing | `samples/equipment-lease-agreement.precept:79` (grep-verified) |
| `add … add` into `maxcount 1` rejects | Existing | `precept_compile` → `PRE0136` |
| `rule SecurityDeposit <= MonthlyPayment * 3` (scaled 2-var) | Existing | `equipment-lease-agreement.precept:78` |
| `rule Total == sum(Fees)` unexpressible | Existing rejection | `precept_compile` → `PRE0030` "'sum' is not a recognized function"; `collection-types.md:860` |
| vacuous-rule detection wording | Existing | `precept_compile` → `PRE0154` "is always true … it governs nothing" |
| `rule … prevented …` / `rule … because … prevented` | PROPOSED — currently a parse error | `precept_compile` → `PRE0009` (both positions) |
| rule grammar `rule BoolExpr ("when" BoolExpr)? because StringExpr` | Existing | `precept-language-spec.md:935` |
| no enforcement-intent modifier in catalog | Existing (the gap) | `src/Precept/Language/Modifiers.cs` (read in full) |
| `sum`/`reduce`, `subset`/`disjoint` locked rejections | Existing | `collection-types.md:860`, `:1540`; `units {}` `business-domain-types.md` D6 — none about enforcement intent |

## Cross-references

- Research backbone: `research/architecture/compiler/solver-free-static-analysis-techniques-survey.md` (C1 Karr, C2 structural wins, C3 TVPI/congruence, C4 declines).
- Demand inventory: `research/architecture/compiler/fragment-boundary-corpus-validation-2026-06-05.md`; `research/architecture/compiler/liveness-completability-verification-survey.md`.
- Canon: `docs/language/precept-language-spec.md` §0.4, §0.6 (#1/#2/#3/#6/#7), §0.7, §3A.1, §3A.4; `docs/compiler/proof-engine.md` (Strategies, Pass 1.5); `docs/philosophy.md` (Principles 1, 8, 10, 11).
