# Precept Guarantee Specification — Strawman

**Status:** SUPERSEDED — biased first draft (authored inline during conversation, carries the author's conversational framing). Replaced by a fresh, unbiased synthesis built directly from the research + grounded Precept docs/philosophy. Retained for reference only; do **not** treat as the active artifact and do **not** read it as input to the fresh version.

> One sentence: enumerate Precept's compile-time/runtime guarantees precisely, scope each, and map each to the stage that enforces it and the severity it carries — so "what the proof engine checks, what the graph analyzer checks, error or warning" becomes *derivable* rather than case-by-case.

## Why this exists

The principles are real but **scattered and informal** on exactly the distinctions that matter for the proof engine and graph analyzer:

- `precept-language-spec.md` §0.1 Principle 11 ("no diagnostics ⟹ no runtime faults"), §0.7 (the fault-prevention-vs-governance contract), Principles 1/6/7/10 — substantive, but the *boundary* between them isn't drawn crisply.
- `docs/Working/compile-time-vs-runtime-contract-clarity-2026-06-01.md` already flagged the gap: *the canon never states explicitly whether structural prevention is scoped to definition-internal values vs externally-sourced inputs.*
- **Liveness is stated *structurally*; the *semantic* layer is the frontier (and the philosophy phrasing runs ahead of it).** `philosophy.md` already claims liveness (line 51 "every non-terminal state has a path forward… no dead ends where an entity gets stuck with no way to advance"; line 57 "no structural dead ends"; line 61 "dead-end states… unsatisfiable guard combinations"), and the graph analyzer enforces the *structural* version (reachability, dead-end sinks, `AlwaysRejecting`). But spec **§0.5 explicitly over-approximates** — *"structural graph analysis treats all edges as traversable regardless of `when` guards… 'all paths' means all structurally declared paths, not all guard-satisfiable paths"* — so **semantic completability** (an entity stuck because every exit's guard/constraints are unsatisfiable) is **not** checked. Philosophy line 51's absolute phrasing ("an entity gets stuck") reaches past §0.5's honest structural-only scoping. **Resolving that gap is the liveness leg's central question — and it is philosophy-adjacent (flag to owner, do not self-resolve).**
- The **error-vs-warning** choice is made case-by-case, not derived from a guarantee class.
- The **fault-vs-rule** division (compile-time fault prevention vs runtime governance) is the spine of all of it and is nowhere written as a single table.

Evidence the imprecision causes real defects: the **BUG-017 family** root cause — *"a detection-tool mindset (skip-to-avoid-false-positives) leaked into a prevention contract"* — happened because the prove-or-reject contract wasn't crisp per-obligation.

## The spine: fault vs rule-violation (two different failure kinds, two mechanisms)

| | **Fault** | **Rule violation** |
|---|---|---|
| What | evaluator performs an *undefined* op (÷0, overflow, empty-access) — a crash | an input fails a declared predicate (`x > y`) |
| When caught | **must** be prevented *before* the value is used — a crash can't be undone | at the **post-mutation sweep**, against the working copy |
| Mechanism | **compile-time**, structural: per-operand requirement back-propagated to the source input; reject the definition if unmet (§0.7) | **runtime governance**: evaluate the rule, reject the operation atomically (entity stays valid) |
| Principle | 7 / 10 / 11 | 6 |
| Runtime outcome | `Faulted` — unreachable for contract data; a fire = compiler defect | `ConstraintsFailed` — expected, typed, recoverable |
| Evaluation? | **No** — structural / interval | **Yes**, but the *runtime* does it; the compiler only evaluates the one input it can see (defaults) |

Key consequence: relational rules are **inherently dynamically enforced** — satisfaction depends on runtime values the compiler can't see, so they cannot be made un-violatable by static input constraints (and need not be — governance rejects violators atomically; no committed state ever violates a rule).

## The enumerated guarantees (precise statements)

| # | Guarantee | Precise statement | Scope |
|---|---|---|---|
| G0 | **Structural/type correctness** | Well-typed, exhaustively-routed, well-formed. | all definitions |
| G1 | **Fault-freedom** | For data entering through the contract, the evaluator never performs an undefined operation; `Faulted` is unreachable. | contract data only (restored/host-injected is out-of-contract, backstopped by traps — §0.7) |
| G2 | **Default validity** | A definition whose default field values violate a declared rule/ensure is rejected at compile time; `Create`-from-defaults always succeeds (C59/C86). | the definition's own static values |
| G3 | **Soundness of outcomes** | Every outcome/inspection verdict the runtime reports is accurate; a *committed* state satisfies every declared rule. | all operations |
| G4 | **Determinism** | Same definition + same data ⇒ same outcome/verdict. | all operations |
| G5a | **Liveness — structural** | Every declared state is reachable; every non-terminal state has a *structurally declared* exit; required states dominate; no structural dead-ends. **Claimed (philosophy 51/57/61) and enforced** (graph analyzer, over-approximate per §0.5). | structural graph |
| G5b | **Liveness — semantic completability** | Every operation offered in a reachable state is completable by some valid input (`Possible` ⟹ ∃ input reaching a determinate authored outcome); no reachable non-terminal state is *semantically* frozen; construction always succeeds. **Decidable solver-free in Precept's interval + single-pass difference-bound fragment** (DBM emptiness, polynomial — the existing PRE0154/0155 contradiction machinery); must **under-approximate** (witness-or-decline). **Hard guarantee in-fragment, warning out-of-fragment.** [grounded: `liveness-completability-verification-survey`] | reachable states |

Note the unification we reached: **a value is a singleton bound** (CUE), so G2's "default fold" is not a second mechanism — it is constraint-validation run on the one input known at compile time. And `Possible` (G5) is best read as a **liveness-carrying verdict** ("a completion exists"), under which G5 folds into G3 (a sound `Possible` *is* a witness) — *except* the state-level "no frozen state" part, which is an existential over events per state, not a single verdict's soundness.

## Responsibility matrix (the payoff)

| Guarantee | Enforcing stage(s) | Reasoning mode | Evaluates? | Severity |
|---|---|---|---|---|
| G0 structural | type checker | typing / structural | no | error |
| G1 fault-freedom | proof engine (fault obligations) | **universal** (∀ inputs safe): structural + interval/relational narrowing | no | error |
| G2 default validity | proof engine (the fold — PRE0164/PRE0115) | **concrete** check of known values | **yes** (defaults only) | error |
| — rule consistency | proof engine (satisfiability scans — PRE0154/0155/0159) | **abstract** consistency | no | error (mostly) |
| G3 soundness | proof engine + runtime governance | — (correctness of the above + sweep completeness) | runtime: yes | error |
| G4 determinism | whole pipeline (no hidden state; `decimal` not `double`) | — | — | — |
| G5a liveness — structural | graph analyzer (reachability PRE0080, dead-end sinks, `AlwaysRejecting` 125 / `StateAlwaysRejects` 126) | structural; over-approximate (§0.5 — all edges traversable) | no | **error** for construction-path; **warning** otherwise (today) |
| G5b liveness — semantic completability | proof engine (DBM emptiness — same machinery as the PRE0154/0155 contradiction scan) | **existential, under-approximating** (witness-or-decline) — solver-free in-fragment; out-of-fragment stays out | no (feasibility, not value-eval) | **error** in-fragment (witness-carrying) / **warning** out-of-fragment |

The matrix makes the open work visible: the proof engine today runs **four** reasoning modes (universal fault obligations, concrete default fold, abstract satisfiability, relational narrowing); the **fifth** — existential completability — is unassigned, and whether the proof engine should take it on depends on whether it's decidable in Precept's fragment without an opaque solver.

## Open decisions (for the design lock)

- **D1 — Proof-engine evaluation scope / shared core.** Confirm the proof engine's job is *not* "only faults": it also does bounded evaluation over known (default) values. **Lock** that this fold shares the runtime's `BinaryExecutors`/`UnaryExecutors` — one expression-evaluation core, invoked over a partial (compile-time) vs full (runtime) environment — so the compile-time prediction can't drift from runtime (G3/G4 soundness depends on this; grounded by `static-vs-runtime-expression-evaluation-survey`). *Evidence-complete; needs the decision.*
- **D2 — Liveness severity. RESOLVED by survey (owner to ratify).** Under-approximating, witness-carrying check: **error** for the in-fragment slice (a real, sound guarantee), **warning** out-of-fragment (best-effort — matches SPARK/Liquid Haskell/itemis; none of them promotes semantic completability to a hard *complete* gate). `Prospect {Certain, Possible, Impossible}` is the runtime surface.
- **D3 — Severity policy (general).** Derive error-vs-warning from the guarantee *class*: a **soundness/fault** violation (G1–G3) is an error (prevention contract — prove-or-reject, never defer); a **best-effort/approximate** check is a warning. Make this a stated rule, not case-by-case.
- **D4 — Existential completability decidable without a solver? RESOLVED: YES, in-fragment.** The guard ∧ post-constraint feasibility test is **DBM emptiness — polynomial negative-cycle detection, solver-free** — the *same machinery* as the existing contradiction scan (PRE0154/0155). SPARK/Liquid Haskell reach for SMT only because their theories are *richer*; Precept's interval + single-pass difference-bound data theory **is** the cheap fragment, so it does solver-free what they need SMT for. Hard boundary (must stay OUT to preserve §0.4 single-pass / §0.6 #3 no-SMT): transitive 3-field chains, full linear arithmetic, congruence/disequality (SDBM → NP-hard), regex/string-shape. (Classical workflow-net soundness is EXPSPACE-complete, but that cost is from *unbounded Petri tokens*, which Precept's single-marking lifecycle graph lacks.) The proof engine *does* take on the fifth reasoning mode — but as **feasibility (emptiness), not value-evaluation**.
- **D5 — Philosophy phrasing + approximation direction (flag, owner-only).** The survey sharpens this into a real choice. Today's structural reachability **over-approximates** (§0.5) — sound for *safety*, but it **falsely certifies** "path forward" through an unsatisfiable-guarded exit (the BUG-017 shape, applied to a *liveness* claim). So line 51's strong reading ("an entity cannot get stuck") is **not backed today.** The survey shows the honest fix is an **under-approximating** semantic check (certify completable only with an exhibited witness — the existential mirror of locked Proof-philosophy #1/#2), and that it's **feasible solver-free in-fragment.** So the two honest options: **(a)** scope line 51 to structural (match §0.5 as-is), or **(b)** adopt the under-approximating completability check and back the strong reading for the in-fragment slice, warning out-of-fragment. The survey makes (b) achievable. **Philosophy decision — surface, do not self-resolve.**

## Evidence base

- Session surveys: `research/architecture/compiler/bounds-only-constraint-enforcement-2026-06-05.md`, `interval-vs-value-evaluation-prior-art-2026-06-05.md`, `static-vs-runtime-expression-evaluation-survey.md`, `liveness-completability-verification-survey.md` (✅ Cited — grounds G5b / D2 / D4 / D5).
- `docs/Working/compile-time-vs-runtime-contract-clarity-2026-06-01.md` (the prior clarity flag — build on it).
- Spec/philosophy: §0.1 Principles 1/6/7/10/11, §0.7; `docs/philosophy.md`.
- Implementation ground truth: `FaultCode.cs` (15 codes, each `[StaticallyPreventable]`), `GraphAnalyzer.cs` (reachability / dead-ends / reject-classification), `ProofEngine.*.cs` (the four reasoning modes), `result-types.md` (the 8 `EventOutcome` variants + inspection `Prospect`/`ConstraintStatus`).

## What this drives next

1. **Completeness audit** uses this matrix as its rubric — verify each row is enforced, live, sound, total (the fault-surface ↔ obligation audit; G1's 15 codes each live & prove-or-reject; the BUG-017 family is the cautionary precedent).
2. **The liveness survey** populates G5 / D2 / D4.
3. **A `/lifecycle-2-design`** locks the matrix (with owner sign-off, given philosophy-adjacency) once D1–D4 are resolved.
