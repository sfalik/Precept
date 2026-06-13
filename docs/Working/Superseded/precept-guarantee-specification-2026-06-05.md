> **SUPERSEDED 2026-06-11** — replaced by [`compiler-readiness-plan-2026-06-11.md`](../compiler-readiness-plan-2026-06-11.md). Retained for history; all still-valid obligations were mined into that plan (see its `-appendices/working-docs-triage.md`). Do **not** treat as current strategy.

# Precept Guarantee Specification

**Status:** Draft — strawman for owner reaction, **NOT locked.** Philosophy-adjacent (it makes the *core contract* precise); requires owner sign-off, does not itself change any guarantee, and must not edit `docs/philosophy.md` / `precept-language-spec.md`. Authored 2026-06-05.

> What Precept guarantees, **why its constraints make those guarantees decidable and complete where a general-purpose language cannot**, and the honest line between what is promised, what is delivered today, and what is bounded out.

---

## 1. The thesis — Precept trades generality for complete, solver-free proof

General-purpose verification (SPARK, Dafny, Frama-C) needs SMT solvers, abstract interpretation with widening, and *still* can't prove everything — because the hard part of program verification is **loops, user functions, and aliasing**. Those are what make the problem undecidable.

**Precept deliberately has none of them.** It is not Turing-complete; there are no loops or recursion, no user-defined functions, no pointers or aliasing, and mutation happens only through declared, named field assignments. The undecidability that caps general verifiers does not apply.

The consequence is the product thesis: **Precept can symbolically reason about the complete behavior of every operation, and decide — prove-or-reject, completely, without a solver — whether that operation keeps every rule true.** "Invalid configurations are structurally impossible" (philosophy) is not aspirational; it is *what the constraints make decidable.*

## 2. What the constraints buy (the mechanism)

| Precept constraint | What it eliminates | What it enables |
|---|---|---|
| **No loops / recursion** (not Turing-complete) | the need for loop invariants + fixpoint + solver | every operation is **straight-line**; the exact post-state is a *finite expression* computed by substitution |
| **No user functions** (catalog operators only) | opaque black-box calls | every expression's behavior is **fully visible** to the compiler |
| **Controlled mutation** (`set <field> = <expr>`, no aliasing) | the frame problem; unknown side effects | the frame is **trivial** — everything not assigned is unchanged, declaratively; the compiler knows *exactly* what each operation touches |
| **Determinism + finite structure** | nondeterministic / dynamic control flow | the post-state is a pure function of (pre-state, args); the whole definition is **finitely enumerable** |

Put together, the verification question collapses to something cheap:

> **"Does operation E preserve rule R?"** = substitute E's post-state expressions into R, and check R still holds given (R held before) ∧ (the guard) ∧ (arg constraints).

That is a **symbolic substitution + a feasibility check in the data fragment** — and the data fragment (intervals + difference-bounds) is **decidable solver-free** (DBM emptiness, polynomial — see `liveness-completability-verification-survey`). No loop-invariant to infer, no SMT, no widening. The "honesty about approximation" caveat largely *dissolves*: there is nothing to approximate when there are no loops to unroll. And because each operation is checked *exactly*, "R holds in every reachable state" follows by induction over the **finite set of operations** — not by guessing a loop invariant.

## 3. What is actually guaranteed (if your precept compiles clean)

The structure unlocks these — stated as the author experiences them:

1. **Calculations cannot fault.** No divide-by-zero, overflow, empty/absent access, or type error — proven for all inputs, or the definition is rejected naming the constraint that would make it safe.
2. **Every rule is either provably kept true, rejected with the fix named, or visibly marked *governed* — never quietly unchecked.** A withdrawal can't drive a balance negative because the compiler *proved* the guarded operation can't — the same way it proves a divisor non-zero. Not "checked after," **proven before** — and where a rule is inherently beyond proof (a product), it's *labelled* as runtime-governed, not faked. (The three-way sort is the authoring contract, §4.)
3. **The entity is born valid and can always make progress.** Defaults satisfy the rules; no reachable non-terminal state is frozen; a valid next input always exists (completability).
4. **Therefore the entire reachable state space is valid and navigable** — invalid states are not reachable, dead-ends are impossible, and the valid moves are inspectable at every step.
5. **Deterministic, fully inspectable, no opaque solver** — same data → same outcome; preview any operation's full reasoning; every verdict is a legible witness, never an SMT trace.

## 4. The authoring contract — prevented, rejected, or governed

The earlier framing called rule-enforcement *governance* (check the result at runtime, refuse violations) — and that, on its own, is just validation; a runtime exception achieves the same outcome. The strengthening dissolves that critique by sorting every rule into one of three dispositions the author always sees — and **never silently skipping one** (silent-accept of a violable rule is the soundness lie the engine must not tell).

| Situation | Disposition | Author experience |
|---|---|---|
| In-fragment, provable as written | **Prevented** (compile-time proof) | nothing — it just holds |
| In-fragment, **not** provable as written | **Rejected** — names the guard/constraint | add a guard or bound the input |
| Out-of-fragment but **reducible** | **Rejected** — suggests reducing arity | restructure (often a *better* model) |
| Inherently out-of-fragment (products, strings) | **Governed** — marked, enforced at runtime | optionally restructure for prevention |

"Force a rewrite" happens only in the two middle rows. The bottom row is the honest limit: not everything is provable, and the engine says so plainly (per "honesty about approximation") rather than faking exactness — the rule is still enforced on every operation at runtime, just *governed*, not *proven*.

**Tier 1 — add a guard (in-fragment).** A decrement that can underflow `nonnegative`:
```precept
from Open on Withdraw -> set Balance = Balance - Withdraw.Amount   # rejected: not provably ≥ 0
from Open on Withdraw when Balance >= Withdraw.Amount              # prevented: guard ⟹ Balance − Amount ≥ 0
    -> set Balance = Balance - Withdraw.Amount
```
A rule *tighter than the field* is the same shape: `field DiscountPercent min 0 max 100` + `rule DiscountPercent <= 50` rejects a `set` from a `[0,100]` input until the input is bounded `max 50` or guarded.

**Tier 2 — reduce arity (out → in).** Three independent fields summed (`rule A + B + C <= 1`) is three-variable — out. The fix is structural: derive the third as the residual, leaving a two-variable rule:
```precept
rule StateRate + LocalRate <= 1                                   # two-variable — in fragment
field SurchargeRate as decimal nonnegative <- 1 - StateRate - LocalRate
```
The combined rate is now `1` *by construction* — prevented structurally, and a more correct model than three free fields constrained after the fact.

**Tier 3 — governed (inherently out).** `rule UnitPrice * Quantity <= Budget` is a product — nonlinear, no rewrite brings a genuine product into the fragment. The engine **accepts it, marked governed**, and says so: enforced at runtime, not compile-time-prevented. Forcing a rewrite here would be wrong (you'd be unable to express a legitimate rule); silently accepting it as if proven is the lie. Prevent what's provable, **govern the rest and say so.**

So a shipped precept has the property that *every rule is either provably preserved or visibly marked governed* — never quietly unchecked. For the prevented rules the runtime sweep is **defense-in-depth** (like the fault traps); for the governed rules it is the primary mechanism, honestly labelled. That is the answer to "governance is hollow": proof is the primary mechanism wherever the fragment reaches, and where it doesn't, the gap is *visible*, not hidden.

## 5. Delivered today vs target (honesty — verified against the compiler)

This section is the bar; do not let §3 read as shipped.

| Guarantee | State today (verified via `precept_compile`) |
|---|---|
| Faults — prove-or-reject | **Partly built.** `rule X > Y` discharges a `100/(X−Y)` divisor (`disposition: Proved, strategy: FlowNarrowing`). Holes remain: **BUG-021** (a `nonnegative` field with no `max` creates *no* lower-bound obligation — `set Balance = Balance − Amount` compiles clean unproven), and the fault-surface completeness is unaudited (the BUG-017 family). |
| Rule preservation (#2 above) | **Not built.** `FlowNarrowing` reaches the divisor obligation but **not** the `IntervalContainment` (assignment-range) obligation. Verified: `set Balance = Balance − Withdraw.Amount` into `min 0 max N` **rejects** (PRE0078, interval `[−∞..+∞]`, Unresolved), and adding the correct guard `when Balance >= Withdraw.Amount` — field-vs-arg, field-vs-field, or post-value — **does not discharge it.** The author is stuck (rejects) or falls into the BUG-021 silent-pass. The *structure* supports proving it; the discharge path is unwired. |
| Completability / liveness | **Structural only** (graph analyzer, over-approximate per §0.5). Semantic completability is **decidable solver-free** (the survey) but **unbuilt**; over-approximation is the *dishonest* direction for it. |
| Born-valid (defaults) | **Just built** (BUG-027 — `DefaultViolatesRule`/PRE0164); computed/equality/choice default cases remain (OD-3/OD-4). |
| Runtime governance + evaluator | **Stub** — the defense-in-depth layer isn't implemented. |

## 6. The proof-engine extension that delivers the thesis

All of it is the *same* machinery the divisor case already uses, extended — solver-free, fits §0.4 (single-pass; substitute through the *finite* action list) and §0.6 #3 (no SMT):

1. **Wire `FlowNarrowing` into the `IntervalContainment` obligation** — so a guard/relation discharges an assignment-range/rule-preservation obligation the same way it discharges a divisor (`when Balance >= Amount` ⟹ `Balance − Amount ≥ 0`).
2. **Close BUG-021** — create the lower-bound obligation even without a `max`, so the unproven decrement rejects instead of silently passing.
3. **Add semantic completability** — the DBM feasibility check per offered operation (`∃ input firing it`), under-approximating (witness-or-decline).
4. **Generalize preservation to multi-action bodies and derived fields** — sequential substitution through the finite action list and the derived-field DAG (both finite → exact).

## 7. The boundary (honest)

The *only* real limit is the **expressiveness of the data-relation fragment**, not program structure:
- **In-fragment (decidable, cheap, solver-free):** intervals + **two-variable octagon relations** `±X ±Y ≤ c` — order (`X ≥ Y`), difference (`X − Y ≤ c`), and two-field sums (`X + Y ≤ c`); plus a nonlinear subterm **atomized** when it recurs identically on both sides.
- **Out-of-fragment:** **3+-variable** linear (`A + B ≥ C`), non-unit-coefficient (`2X + Y`), nonlinear (products/quotients of variables), congruence/disequality (NP-hard), regex/string-shape. These cross into the polyhedra/SMT territory §0.6 #3 forbids.

Out-of-fragment relations are **not** silently skipped — they are handled by the §4 authoring contract: *reducible* ones are **rejected** with a restructure suggested (reduce arity, derive the residual); *inherently* out-of-fragment ones (products) are **accepted but marked governed**. So "honesty about approximation" narrows from the sprawling incompleteness of general verification to a single, namable question: *which data relations can we represent.*

**Corpus validation (verdict: ACCEPTABLE).** A 77-sample audit found **~89% of real invariants are in-fragment** as written (82% of `rule`s, 91% of `ensure`s), and the corpus's heavy 3+-term/nonlinear arithmetic lives almost entirely in **derived `<-` fields** — computed, not constrained, so they carry no preservation obligation and the boundary doesn't bite them. The one recurring out-of-fragment *invariant* is the **3-field balance identity** (`A + B == C`, 4 precepts) — the named candidate for a bounded extension (fixed-arity, plausibly still solver-free), with non-unit-coefficient two-variable as the cheap adjacent win. (Source: `research/architecture/compiler/fragment-boundary-corpus-validation-2026-06-05.md`. One gate before locking: confirm `ProofEngine.*.cs` decides `X − Y ≤ c` directly — the in-fragment fraction hinges on that attribution.)

## 8. Open decisions (neutral — for a `/design` lock)

- **OD-1 — Shared evaluation core.** The runtime defense-in-depth must compute the *same* values the compiler proved about, or the guarantee can drift. Lock that the runtime evaluator and the compile-time fold share one expression-evaluation core (per `static-vs-runtime-expression-evaluation-survey`).
- **OD-2 — The data-fragment boundary.** How far to push relations (difference-bounds only? a bounded 3-field case?) before declaring the edge — the trade between completeness and staying solver-free/single-pass.
- **OD-3 — Severity policy.** Derive error-vs-warning from the guarantee class: a soundness/preservation failure that *can* be proven → error; an out-of-fragment case the engine can't decide → reject (ask for a constraint) vs warn. State the rule, don't decide case-by-case.
- **OD-4 — Philosophy phrasing (owner-only).** Reconcile the absolute claims (line 51 "no dead ends where an entity gets stuck"; "the invalid configuration is not reachable") with the delivered/over-approximate state — keep the strong phrasing only where the thesis is built. Surface, do not self-resolve.

## 9. Evidence base

- Research: `research/architecture/compiler/` — `liveness-completability-verification-survey.md` (DBM feasibility solver-free; the data-fragment boundary), `static-vs-runtime-expression-evaluation-survey.md` (shared core / drift), `interval-vs-value-evaluation-prior-art-2026-06-05.md`, `bounds-only-constraint-enforcement-2026-06-05.md`.
- Spec/philosophy: §0.1 Principles 1/6/7/10/11, §0.4 (single-pass/depth-bounded), §0.5 (over-approximation), §0.6 (proof contract; #3 no-SMT), §0.7 (fault-prevention vs governance); `docs/philosophy.md` (not Turing-complete, controlled mutation, no user functions, determinism, inspectability).
- Implementation ground truth: `FaultCode.cs` (15 codes), `GraphAnalyzer.cs` (structural reachability/dead-end), `ProofEngine.*.cs` (`FlowNarrowing`); verified compiler behavior via `precept_compile` (divisor discharges; assignment-range guard does **not**; BUG-021 silent-pass).
- `docs/Working/bugs.md` — BUG-021 (lower-bound obligation not created without `max`), BUG-017 family (obligation-not-created soundness holes).
