---
status: Locked 2026-05-26
phase-target: Phase 5
comparable-systems-research-status: partial — GHC `-Woverlapping-patterns` and `-Wredundant-constraints` cited verbatim from the current GHC user guide; CUE lattice/bottom cited from cuelang.org/docs/concept/the-logic-of-cue; Dafny `ProofDependencyWarnings.cs` warning strings cited verbatim from github.com/dafny-lang/dafny master; Liquid Haskell cited via in-tree `research/architecture/compiler/proof-attribution-witness-design-survey.md § Liquid Haskell`. No `Stakes: irreversible` decisions; inline-survey discipline applied per decision.
feature-gates: F-LANG-SPEC-02, F-LANG-SPEC-03, F-LANG-SPEC-04, F-LANG-SPEC-05, F-LANG-SPEC-12
related-bugs: BUG-006
sources-consulted:
  - docs/philosophy.md — prevention/inspectability commitments
  - docs/language/precept-language-spec.md § 0.1 — 11 design principles
  - docs/language/precept-language-spec.md § 0.6 — proof engine design contract; 13 obligations and 7 philosophy commitments; Implementation Status table
  - docs/language/precept-language-spec.md § 0.5 — Graph Analyzer Overapproximation Rule
  - docs/compiler/proof-engine.md § 6 Two-Pass Design
  - docs/compiler/proof-engine.md § 11 Decisions 1 + 3 + 5 — five-strategy bound; type-checker stamps obligations; catalog satisfactions
  - docs/compiler/diagnostic-system.md — severity conventions
  - docs/Working/Archive/index-bounds-proof-design.md — Phase 4 W-E precedent for new requirement + strategy + diagnostic
  - docs/Working/bugs.md § BUG-006 — verbatim cross-row interval composition repro
  - docs/Working/bugs.md § F-LANG-BIZ-08 — discrete narrowing comparator (out of scope per D-7)
  - docs/Working/compiler-readiness-plan-2026-05-24.md § Phase 5 — phase framing and findings list
  - src/Precept/Pipeline/ProofEngine.cs — Prove entry point + TryDischarge dispatch
  - src/Precept/Pipeline/ProofEngine.Strategies.cs — seven strategies; ExtractGuardBranches infrastructure
  - src/Precept/Pipeline/ProofEngine.Intervals.cs — BuildNarrowedIntervals; IntervalOfNarrowed
  - src/Precept/Pipeline/ProofEngine.Diagnostics.cs — diagnostic routing
  - src/Precept/Pipeline/StateGraph.cs — ProofForwardingFact DU shape
  - src/Precept/Pipeline/GraphAnalyzer.cs — DeadEndStateFact production
  - src/Precept/Language/ProofRequirement.cs — DU + 11 ProofRequirementKind values
  - src/Precept/Language/Diagnostics.cs — UnsatisfiableGuard (PRE0082) catalog entry; Severity convention for dead-code-family diagnostics
  - src/Precept/Language/DiagnosticCode.cs — code numbering; current top values
  - research/architecture/compiler/proof-attribution-witness-design-survey.md § Liquid Haskell — Z3 discharge, anti-precedent for opaque solvers
  - research/architecture/compiler/proof-attribution-witness-design-survey.md § Dafny — Boogie/SMT pipeline, anti-precedent
  - research/philosophy/formal-spec-languages-comparators.md § Liquid Haskell + Dafny — verbatim claim about Precept's existing scope
  - GHC user guide `-Woverlapping-patterns` — verbatim "the last pattern match in `f` won't ever be reached"
  - GHC user guide `-Wredundant-constraints` — verbatim "Constraints not used in the code it covers"
  - cuelang.org/docs/concept/the-logic-of-cue — lattice-bottom contradiction model; verbatim "Bottom ... is the result of computing `true & false`. A value cannot be both true and false, so this an error."
  - dafny-lang/dafny ProofDependencyWarnings.cs — verbatim warning strings: "proved using contradictory assumptions", "unnecessary requires clause"
---

# Proof-engine satisfiability cluster

**Findings covered**: F-LANG-SPEC-02 (dead-guard detection / wire `UnsatisfiableGuard`), F-LANG-SPEC-03 (contradictory rule detection), F-LANG-SPEC-04 (vacuous rule detection), F-LANG-SPEC-05 (tautological guard detection), F-LANG-SPEC-12 (sharpened routing diagnostics from proven-dead guards), BUG-006 (cross-row interval composition).

## Goal

When this design lands, the proof engine flags any guard, rule, or row that is provably-always-false (dead) or provably-always-true (tautological/vacuous) under the field's declared constraints, sharpens reachability diagnostics from those proven verdicts, and composes guard-derived narrowing with field-modifier-derived bounds across sibling rows so that the BUG-006 repro from `docs/Working/bugs.md § BUG-006` (`field Counter as integer default 0 nonnegative` + `field MaxCount as integer max 5` + reject-row `when Counter >= MaxCount` followed by `set Counter = Counter + 1`) compiles clean.

Five of the thirteen `precept-language-spec.md § 0.6` obligations move from "Specification-only" to "Implemented" as a result.

## Scope

**In scope.**

- Catalog declaration of three new diagnostic codes: `TautologicalGuard` (PRE0153), `VacuousRule` (PRE0154), `ContradictoryRule` (PRE0155).
- Wire-up of the existing `UnsatisfiableGuard` (PRE0082, currently a catalog entry with no emitter).
- A new "satisfiability scan" pass in the proof engine, placed between `IncorporateForwardingFacts` and the per-obligation discharge loop. Lives in a new partial-class file `ProofEngine.Satisfiability.cs`.
- Extension of `ProofEngine.Intervals.cs` to compose **sibling reject-row** guard-derived intervals with field-modifier-derived bounds. Adds `NumericInterval.Empty`, `IsEmpty`, and a sound `Subtract` operation that falls back when the difference is non-contiguous.
- F-LANG-SPEC-12 integration: a new `ProofForwardingFact` variant (`UnreachableRowFact`) consumed by the existing `IncorporateForwardingFacts` path so that a state whose every outgoing row has a proven-unsatisfiable guard is treated as a DeadEnd (sharpening the existing `DeadEndState` / `UnreachableState` diagnostics).

**Out of scope.**

- F-LANG-BIZ-08 (discrete equality narrowing on `choice of` fields). Different lattice (discrete value sets vs numeric intervals). Captured as D-7 below.
- General SMT-based discharge. § 0.6 #3 holds — no opaque solvers.
- Cross-event relational reasoning ("event A's postcondition implies event B's precondition"). The philosophy comparator survey explicitly disclaims this scope.

**Deferred to future.**

- Quantifier-binding satisfiability (`each i in C (...)` where the predicate body is provably-false for some `i`). The current narrowing infrastructure does not bind quantifier indexes; that is a separate extension surface.
- Hover surface for the new verdicts. Diagnostics ship now; LS / MCP hover enhancement is a follow-up slice.

## Philosophy Alignment

| Principle | Affected? | How served | Tension | Tradeoff |
|---|---|---|---|---|
| 1. Prevention not detection | Y | Provably-false guards, contradictory rules, and vacuous rules are flagged at compile time (`philosophy.md § What makes it different — "Two guarantees make this absolute"`); after the warning fires, the author either fixes the predicate or deletes the construct before any instance exists | N/A | N/A |
| 2. One file, complete rules | Y | All facts derive from the single `.precept` file; no external oracle or solver call (per `precept-language-spec.md § 0.6 #4 — "All proof facts derive from the .precept definition"`) | N/A | N/A |
| 3. Determinism | Y | The scan uses the existing `NumericInterval` algebra and `ExtractGuardBranches` traversal — no solver search, no timeout, no "unknown" verdict. Same definition ⇒ same verdict | N/A | N/A |
| 4. Full inspectability | Y | New diagnostics carry structured attribution (field intervals, guard span, conflicting clause); the proof ledger surfaces the verdict for `precept_proofs` MCP consumers per § 0.6 #6 — "Proof results flow as structured data, not parsed prose" | N/A | N/A |
| 5. Keyword-anchored readability | N/A | No new keyword / grammar surface — diagnostic surface only | N/A | N/A |
| 6. Governance not validation | Y | A rule proven vacuous fails to govern anything; a rule proven contradictory with another governs an empty set. Flagging them protects the "the rule holds because configurations that violate it cannot exist" claim by ensuring the rule is meaningful | N/A | N/A |
| 7. Compile-time totality | Y | Closes five of the thirteen § 0.6 obligations that are currently "Specification-only" per § 0.6 Implementation Status | Author-visible behavior change: existing precepts with decorative-but-vacuous rules will see a Warning | One-time corpus sweep during execute; new diagnostics are Severity.Warning so they don't block ship |
| 8. Honesty about approximation | N/A | No approximation involved; interval composition is exact decimal arithmetic | N/A | N/A |
| 9. Mandatory rationale (`because`) | N/A | Diagnostic surface, not rule authoring | N/A | N/A |
| 10. Static semantic checking | Y | The satisfiability scan is a static semantic pass; direct fulfillment of § 0.1 #10 | N/A | N/A |
| 11. Static completeness | Y | A precept that ships today with a contradictory rule pair compiles clean and the rule "holds" vacuously at runtime — a static-completeness gap. The new diagnostics close it | The new diagnostics are Warnings, not Errors, so a precept with a vacuous rule still compiles and produces an engine | Accepted: the verdict is an author-intent gap, not a runtime safety hole. Severity escalation is reversible (Warning → Error is easy; Error → Warning is build-breaking) — start conservative |

**Tradeoff accepted (Principle 7 / 11).** The new diagnostics expand the proof scope. A precept with a tautological guard like `when Count >= 0` (where `Count` is `nonnegative`) compiles today and will compile after this ships — only the Warning is new. Authors who relied on these forms as documentation will see new diagnostic noise. The mitigation is per § 0.6 #1 "Soundness over completeness": the scan emits ONLY on provably-determined verdicts, never heuristic ones, so the Warning is always actionable.

**Companion commitments.** Stateless-first-class: unaffected; satisfiability detection works identically on stateless precepts (rules are checked the same way; the only difference is the absence of state-scoped guards). Domain-expert-primary-author: served — diagnostic messages target domain vocabulary ("rule X is always true given Counter's declared bounds"), not solver internals; the recovery hint always names the conflicting field constraint or the redundant predicate.

## Language Design Grounding

### General language design — compile-time detection of provably-dead, contradictory, and tautological constructs

The field has well-developed precedent. The architectural question is *how* the verdict is computed (structural pattern subsumption, lattice unification, SMT discharge), not *whether* the detection is desirable.

**Structural pattern subsumption (GHC).** GHC's `-Woverlapping-patterns` (default-on) detects unreachable pattern clauses by structural subsumption — no SMT. Verbatim from the current GHC user guide (`https://downloads.haskell.org/ghc/latest/docs/users_guide/using-warnings.html`, accessed 2026-05-26):

> "the last pattern match in `f` won't ever be reached, as the second pattern overlaps it"

The mechanism is Maranget's algorithm — pattern matrix subsumption over algebraic-data-type constructors. GHC also ships `-Wredundant-constraints`:

> "Constraints not used in the code it covers"

These are the closest analogues to F-LANG-SPEC-02 (dead guard) and F-LANG-SPEC-04 (vacuous rule). GHC implements both as default-on Warnings, not Errors.

**Lattice unification with explicit bottom (CUE).** CUE is a constraint language — closer to Precept's surface than a programming language. From `cuelang.org/docs/concept/the-logic-of-cue` (accessed 2026-05-26):

> "Bottom, in this example, is the result of computing `true & false`. A value cannot be both true and false, so this an error."

CUE detects contradictions at unification time on any value. No SMT. Order-independent. The lattice-bottom model is the structural precedent for F-LANG-SPEC-03 (contradictory rules) on a constraint-language surface.

**SMT-based discharge (Dafny, Liquid Haskell — anti-precedent).** Dafny delegates these verdicts to Z3 via Boogie. Verbatim warning strings from `github.com/dafny-lang/dafny/blob/master/Source/DafnyCore/ProofDependencyWarnings.cs` (accessed 2026-05-26):

> `"proved using contradictory assumptions: {obligation.Description}"`
>
> `"ensures clause proved using contradictory assumptions"`
>
> `"unnecessary requires clause"`
>
> `"unnecessary (or partly unnecessary) {assumption.Description}"`

Same diagnostic *content* as F-LANG-SPEC-03 and F-LANG-SPEC-04. Different *mechanism*: an opaque solver. Liquid Haskell uses the same Z3 path (`research/architecture/compiler/proof-attribution-witness-design-survey.md § Liquid Haskell:185-188`). Precept's `precept-language-spec.md § 0.6 #3` explicitly rejects this:

> "Opaque solvers are rejected on principle. The language's proof reasoning must be legible — to authors, to tooling, and to AI agents. ... This is why SMT/Z3 solvers are excluded even when they could prove more"

Precept therefore takes the GHC + CUE *judgments* (dead clause, lattice-bottom contradiction) and the GHC *severity convention* (Warning, default-on), and rejects the Dafny / Liquid Haskell *mechanism* (Z3). The structural-interval-composition path is the synthesis: CUE's lattice algebra at the constraint-language surface, GHC's structural traversal as the implementation discipline, no SMT.

### Precept-specific application

Touches:

- `precept-language-spec.md § 0.6 #7` (Contradictory rule detection) — F-LANG-SPEC-03
- `precept-language-spec.md § 0.6 #8` (Vacuous rule detection) — F-LANG-SPEC-04
- `precept-language-spec.md § 0.6 #9` (Dead guard detection) — F-LANG-SPEC-02
- `precept-language-spec.md § 0.6 #10` (Tautological guard detection) — F-LANG-SPEC-05
- `precept-language-spec.md § 0.6 #12` (Sharpening of reachability and routing diagnostics from proven-dead guards) — F-LANG-SPEC-12
- `precept-language-spec.md § 0.6 #1` (Numeric interval reasoning) — extended by BUG-006 cross-row composition

## Audience and Teachability

Touches author-visible surface (three new diagnostic codes; the wording is read by domain experts). Required.

### Worked example

A loan-renewal-cap precept that exercises both BUG-006 (cross-row composition) and F-LANG-SPEC-05 (tautological guard):

```precept
precept LoanRenewalCap because "Loans cap at MaxRenewals to bound roll-over risk"
field RenewalCount as integer default 0 nonnegative
field MaxRenewals as integer max 5 nonnegative
state Active initial
state Cancelled terminal

event RequestRenewal
from Active on RequestRenewal
    when RenewalCount >= MaxRenewals
    -> reject "Renewal cap reached"
from Active on RequestRenewal
    -> set RenewalCount = RenewalCount + 1
    -> no transition

event Cancel
from Active on Cancel -> transition Cancelled
```

Before this design: the second `RequestRenewal` row emits `PRE0078 NumericOverflow` on `RenewalCount + 1` (the proof engine does not narrow `RenewalCount`'s interval by the preceding reject-row).

After this design: the second row's narrowed `RenewalCount` interval is `[0, MaxRenewals-1] = [0, 4]` (`field MaxRenewals max 5` gives the upper bound; the preceding reject row's `RenewalCount >= MaxRenewals` subtracts `[5, ∞)`). So `RenewalCount + 1 ∈ [1, 5]`, which fits the field's bounds. Clean compile.

A second snippet shows the new tautological-guard diagnostic:

```precept
event Submit
from Active on Submit when RenewalCount >= 0 -> transition Cancelled
# After this design: PRE0153 TautologicalGuard
# "Guard 'RenewalCount >= 0' is always true given the declared
#  constraints on 'RenewalCount' — the `when` clause has no effect"
```

A third snippet shows the contradictory-rule diagnostic:

```precept
rule RenewalCount > 10 because "Bonus renewals beyond standard cap"
rule RenewalCount <= 5 because "Hard cap on renewals"
# After this design: PRE0155 ContradictoryRule on the first rule
# "Rule 'RenewalCount > 10' (range (10, ∞)) conflicts with rule
#  'RenewalCount <= 5' (range (-∞, 5]) — no field configuration satisfies both"
```

### Error messages

Each new diagnostic targets domain-expert vocabulary, not compiler internals.

- **PRE0082 `UnsatisfiableGuard`** (existing catalog entry, message unchanged from `Diagnostics.cs:764`): *"Guard '{0}' on event '{1}' is unsatisfiable under the declared constraints{2} — this row can never fire"*. Recovery: *"Revise the guard so at least one valid configuration can satisfy it. Delete the row if the event should never be allowed from this path."*

- **PRE0153 `TautologicalGuard`** (new): *"Guard '{0}' is always true given the declared constraints on '{1}' — the `when` clause has no effect"*. Recovery: *"Remove the guard if the row is intended to fire unconditionally, or tighten the predicate to express the intended business condition."*

- **PRE0154 `VacuousRule`** (new): *"Rule '{0}' is always true given the declared field constraints on '{1}' — it has no enforcement effect"*. Recovery: *"Tighten the rule's predicate, or delete the rule if the field-level constraint already captures the intent."*

- **PRE0155 `ContradictoryRule`** (new): *"Rule '{0}' (range {1}) conflicts with rule '{2}' (range {3}) on field '{4}' — no configuration satisfies both"*. Recovery: *"Rewrite one rule's predicate so it does not conflict with the other, or delete the rule that does not reflect the domain."*

These follow `precept-language-spec.md § 0.7 #3 — "Compile-time error messages target domain vocabulary, not compiler internals. A message that says 'monthly price cannot be negative' speaks to the author; a message that says 'type mismatch: expected `decimal{nonnegative}`, got `decimal`' does not."*

### 10-minute teaching path

1. `docs/language/precept-language-spec.md § 0.6` (5 min) — read the 13-obligation contract, especially items 7-10 and 12.
2. The worked example above (2 min) — read the LoanRenewalCap precept to see the new diagnostics in their domain context.
3. `docs/compiler/diagnostic-system.md § PRE0153 / PRE0154 / PRE0155` entries (3 min) — RecoverySteps and ExampleBefore/After give the canonical fix shape.

## Semantic Rules

Required (touches proof obligations and interval reasoning).

### Detection rule — dead guard (F-LANG-SPEC-02)

For a guard expression G on a transition row, event handler, or state hook:

```
  ConjoinedInterval(G under field bounds + ImpliedModifiers) ≡ Empty
  ─────────────────────────────────────────────────────────────────
  emit UnsatisfiableGuard at G.Span
```

Where `ConjoinedInterval(G)` walks each branch of `ExtractGuardBranches(G)` (the existing infrastructure produces disjunctive branches), narrows each field's interval per branch, and conjoins. A branch is unsatisfiable when any field's narrowed interval is empty. The guard is unsatisfiable when **every** branch is unsatisfiable (disjunctive — one branch true is enough to fire).

### Detection rule — tautological guard (F-LANG-SPEC-05)

```
  ConjoinedInterval(¬G under field bounds + ImpliedModifiers) ≡ Empty
  ─────────────────────────────────────────────────────────────────
  emit TautologicalGuard at G.Span
```

Operationally: negate the guard via the existing `NegateOp` in `ProofEngine.Strategies.cs:859`, apply De Morgan to AND/OR, and check whether the negation is unsatisfiable. If `¬G` cannot be satisfied, then `G` is always true.

### Detection rule — vacuous rule (F-LANG-SPEC-04)

A rule `rule P because "..."` (or guarded `rule P when Q because "..."`) is vacuous when:

```
  ConjoinedInterval(¬P under field bounds + ImpliedModifiers + Q) ≡ Empty
  ─────────────────────────────────────────────────────────────────────
  emit VacuousRule at P.Span
```

(Same shape as TautologicalGuard, applied to the rule predicate, with the rule's own `when` guard included in the narrowing context if present.)

### Detection rule — contradictory rules (F-LANG-SPEC-03)

For a pair of rules `(R1, R2)`:

```
  For some field F appearing in both predicates:
    IntervalOf(R1's constraint on F) ∩ IntervalOf(R2's constraint on F) ≡ Empty
  ─────────────────────────────────────────────────────────────────────────
  emit ContradictoryRule at R2.Span, with R1 as the related-rule witness
```

Scope-cut: only fires when F appears in **both** predicates and the conjoined-interval check is exact (no `Unbounded` operand contributes). Soundness-over-completeness per § 0.6 #1 — when one interval is `Unbounded`, the verdict is "cannot decide," and no diagnostic emits.

### Detection rule — cross-row interval composition (BUG-006)

When `BuildNarrowedIntervals` computes the narrowed interval for a field F at site S in row R_current:

```
  narrowedF := IntervalOf(F under R_current.Guard)
  For each sibling row R_i ABOVE R_current on the same (state, event):
    If R_i is a reject-row AND R_i.Guard narrows F to interval I_reject:
      narrowedF := narrowedF ∩ (FieldBounds(F) ∖ I_reject)
```

Where `∖` is interval set difference. When the difference would produce two disjoint intervals (e.g., `[0, 10] ∖ [3, 5] = [0, 3) ∪ (5, 10]`), the implementation falls back to `narrowedF := IntervalOf(F under R_current.Guard)` (the original single-row narrowing) — sound, less precise. The contiguous-difference case (e.g., `[0, 10] ∖ [5, ∞) = [0, 5)`) is the common one for reject-row guards on the upper end.

### Soundness preservation claim

- **Principle 7 (compile-time totality)**: each new diagnostic emits ONLY on provably-determined verdicts (interval ≡ ∅ for dead; interval ≡ ∅ on the negation for tautological/vacuous; intersection ≡ ∅ for contradictory). All decidable on the existing `NumericInterval` algebra. False positives are structurally impossible — the narrowing path itself is sound.
- **Principle 10 (static semantic checking)**: the satisfiability scan is a static semantic pass. The algebra is the same one `IntervalOfNarrowed` already uses for interval-containment proof; no new lattice, no new search.
- **Principle 11 (static completeness)**: a precept that ships clean after this design has no provably-dead guards, no provably-contradictory rule pairs (under the scope-cut), and BUG-006's previously-rejected `Counter + 1` is now proved. Five of the thirteen § 0.6 obligations move from Specification-only to Implemented.

## Architecture Grounding

### Precept-internal placement

**Layer placement.** Three additions, each in its natural layer:

1. **Catalog metadata layer** — three new entries in `DiagnosticCode.cs` and `Diagnostics.cs` `GetMeta`. Severity.Warning, DiagnosticStage.Proof, DiagnosticCategory.Proof. Consistent with the existing `UnsatisfiableGuard` (PRE0082) and the graph-stage dead-code family (`UnreachableState`, `DeadEndState`, `UnhandledEvent`, `AlwaysRejecting`, `StateAlwaysRejects`) which are all Severity.Warning per `Diagnostics.cs:701-794`.

2. **Proof engine pipeline stage** — a new "satisfiability scan" pass placed between `IncorporateForwardingFacts` (line 100 of `ProofEngine.cs`) and the per-obligation discharge loop (line 104). Lives in a new partial-class file `ProofEngine.Satisfiability.cs`. The scan walks transition rows / event handlers / state hooks / rules and emits the new diagnostics directly; it does NOT use the obligation discharge channel (per D-3 below).

3. **`ProofEngine.Intervals.cs`** — extend `BuildNarrowedIntervals` to compose sibling reject-row intervals (BUG-006). Add `NumericInterval.Empty` static + `IsEmpty` accessor + `Subtract(NumericInterval other)` returning the at-most-single-interval result (with fallback to original bounds for non-contiguous differences).

**Why proof engine and not graph analyzer?** `precept-language-spec.md § 0.5` locks the graph analyzer to **structural** reasoning: *"Structural graph analysis treats all edges as traversable regardless of `when` guards — it overapproximates reachability. ... A modifier that claims 'all paths visit this state' means all *structurally declared* paths, not all guard-satisfiable paths."* Proving a guard unsatisfiable requires reasoning about field-value intervals — that's the proof engine's job, not the graph analyzer's. F-LANG-SPEC-12 then *feeds* the proof-engine verdicts back to the graph-style routing diagnostics via the existing `ProofForwardingFact` channel — see the new `UnreachableRowFact` variant below.

**Cross-component propagation.**

- **Runtime** (parser, type checker, evaluator, diagnostics): parser unchanged. Type checker unchanged. Evaluator unchanged — no new `ProofRequirement` subtype, no per-expression obligation, no new runtime path. Diagnostics: three new codes added.
- **Tooling** (syntax highlighting, completions, hover, semantic tokens): no changes in this slice. Hover surface for satisfiability verdicts on guards/rules is a future enhancement.
- **MCP** (vocabulary, DTOs, tool output): `precept_compile` output gains the new diagnostic codes via the normal channel — no new MCP tool, no new DTO. `precept_diagnostic` lookup for the three new codes works automatically (catalog-driven). `precept_proofs` may want to surface the new verdict kinds at a later slice — out of scope here.

**Breaking changes.** Yes — author-visible. Three new Warning-severity diagnostics will fire on existing precepts that contain tautological guards, vacuous rules, or contradictory rule pairs. The implementation slice will include a sample-corpus sweep: each new diagnostic trip is either a decorative-noise rule that gets deleted, or a real predicate bug that gets fixed. Existing samples that compile clean today and have no such constructs are unaffected.

### External architectural precedent

The architectural question: *how does a constraint-language compiler report dead clauses, contradictions, and tautologies without an opaque solver?*

Three solutions surveyed:

- **GHC `-Woverlapping-patterns`** (structural pattern subsumption, no SMT). From the current GHC user guide (`https://downloads.haskell.org/ghc/latest/docs/users_guide/using-warnings.html`, accessed 2026-05-26): *"the last pattern match in `f` won't ever be reached, as the second pattern overlaps it ... More often than not, redundant patterns is a programmer mistake/error, so this option is enabled by default."* GHC uses Maranget's algorithm — pattern matrix subsumption over ADT constructors. Closest direct analogue to F-LANG-SPEC-02 (dead guard).

  Precept takes: the default-on Warning discipline; the principle that dead-clause detection is structural, not solver-based. Precept diverges: pattern matrices → interval lattices (Precept's surface is numeric guards, not ADT patterns).

- **CUE lattice unification** with explicit `⊥` (bottom). From `cuelang.org/docs/concept/the-logic-of-cue` (accessed 2026-05-26): *"Bottom, in this example, is the result of computing `true & false`. A value cannot be both true and false, so this an error."* CUE is a constraint language; detection is order-independent unification at constraint-conjoin time. No SMT. Closest direct analogue to F-LANG-SPEC-03 (contradictory rules) on a constraint-language surface.

  Precept takes: the lattice-bottom semantics. `NumericInterval.Empty` after conjunction maps to CUE's `⊥`. Precept diverges: CUE's lattice is full-structural-value (any CUE value type); Precept's lattice is bounded-numeric-interval — narrower scope, same architectural shape.

- **Dafny / Liquid Haskell** (SMT-based discharge — explicit anti-precedent). Dafny's `ProofDependencyWarnings.cs` ships these verbatim strings (from `github.com/dafny-lang/dafny/blob/master/Source/DafnyCore/ProofDependencyWarnings.cs`, accessed 2026-05-26):

  > `"proved using contradictory assumptions: {obligation.Description}"`
  >
  > `"ensures clause proved using contradictory assumptions"`
  >
  > `"unnecessary requires clause"`

  Same diagnostic *content* as F-LANG-SPEC-03 and F-LANG-SPEC-04. Different *mechanism* — Z3 via Boogie. `precept-language-spec.md § 0.6 #3` explicitly rejects this: *"opaque proof witnesses violate the inspectability commitment. This is why SMT/Z3 solvers are excluded even when they could prove more."* Liquid Haskell shares the same Z3 path per `research/architecture/compiler/proof-attribution-witness-design-survey.md § Liquid Haskell:185-188`.

**Precept's positioning.** Take GHC's *discipline* (default-on Warning, structural detection), CUE's *semantics* (lattice-bottom on constraint conjunction), reject Dafny / Liquid Haskell's *mechanism* (Z3). The synthesis is a structural-interval-composition scan over the existing `NumericInterval` algebra — no solver, no search.

## Inventory of what will be built

### Catalog additions (`src/Precept/Language/`)

- `DiagnosticCode.cs`: add three entries — `TautologicalGuard = 153`, `VacuousRule = 154`, `ContradictoryRule = 155`. (Current top values: 150, 151, 152 are taken per `DiagnosticCode.cs:209,319,328`; 153-155 are the next consecutive free codes.)
- `Diagnostics.cs` `GetMeta`: three new entries. Severity.Warning, DiagnosticStage.Proof, DiagnosticCategory.Proof. Each carries FixHint, TriggerCondition, RecoverySteps, ExampleBefore, ExampleAfter per the catalog template. RelatedCodes link back to `UnsatisfiableGuard`, `UnreachableState`, `DeadEndState` to cycle the dead-code-family link graph.
- `Diagnostics.cs` PRE0082 entry: refresh `RelatedCodes` to include `TautologicalGuard`, `VacuousRule`, `ContradictoryRule`. Single-line edit at `Diagnostics.cs:765`.

### Proof engine pipeline (`src/Precept/Pipeline/`)

- New file `ProofEngine.Satisfiability.cs` — partial class:
  - `internal static void ScanSatisfiability(SemanticIndex semantics, List<Diagnostic> diagnostics)` — entry point.
  - `private static bool IsGuardUnsatisfiable(TypedExpression guard, SemanticIndex semantics, IReadOnlyDictionary<string, NumericInterval>? rowSiblingExclusions)` — narrows each field's interval per branch; returns true iff every branch has at least one empty narrowed interval.
  - `private static bool IsGuardTautological(TypedExpression guard, SemanticIndex semantics)` — negates G (via existing `NegateOp` + De Morgan walk), runs `IsGuardUnsatisfiable` on the negation.
  - `private static bool IsRulePredicateVacuous(TypedExpression predicate, TypedExpression? ruleGuard, SemanticIndex semantics)` — same shape as `IsGuardTautological` with the rule's own `when` guard included in the narrowing context.
  - `private static IEnumerable<(int R1Index, int R2Index, string FieldName, NumericInterval R1Range, NumericInterval R2Range)> FindContradictoryRulePairs(SemanticIndex semantics)` — quadratic scan over `semantics.Rules`. For each pair, find fields common to both predicates; check interval intersection per field. Soundness scope-cut: skip when either operand is `Unbounded`.
- `ProofEngine.Intervals.cs`:
  - Add `public static NumericInterval Empty { get; } = new(0, -1)` (any min > max). Add `public bool IsEmpty => Min > Max`. (Verify exact API shape against the existing struct at write time.)
  - Add `public NumericInterval Subtract(NumericInterval other)` returning a single contiguous interval when the difference is contiguous, or `this` (unchanged) when the difference would split into two disjoint intervals — sound fallback.
  - Extend `BuildNarrowedIntervals` to walk **sibling reject-rows** preceding the current row on the same `(state, event)` pair. For each sibling, compute the narrowed interval(s) that the sibling's guard rejects, and subtract them from the current row's per-field narrowed intervals.
- `ProofEngine.cs Prove(...)`: insert `ScanSatisfiability(semantics, diagnostics)` between `IncorporateForwardingFacts(graph.ProofFacts, obligations, semantics)` (line 100) and the obligation-discharge loop (line 104).
- F-LANG-SPEC-12 routing:
  - New `ProofForwardingFact` variant in `StateGraph.cs:91`: `public sealed record UnreachableRowFact(string FromState, string EventName, ImmutableArray<int> UnreachableRowIndices) : ProofForwardingFact`.
  - The satisfiability scan produces these facts as a side output. (Note: `ProofForwardingFact` is conventionally produced by the graph analyzer per `StateGraph.cs`, but the type is a public DU base — extending it from the proof engine is a clean continuation, and the consumption path is already inside the proof engine via `IncorporateForwardingFacts`.)
  - `IncorporateForwardingFacts` extended at `ProofEngine.cs:679` to consume `UnreachableRowFact`. When a state's outgoing-row set has every row tagged unreachable, the existing dead-end propagation already kicks in via `DeadEndStateFact` (no new emission path needed at the graph layer — the proof engine adds the dead-end Warning directly when the conditions are met).

### Tests (`test/Precept.Tests/ProofEngine/`)

- `SatisfiabilityScanTests.cs` (new):
  - `UnsatisfiableGuard_EmitsPRE0082_OnContradictoryGuard` — `when X > 100 and X < 50` → PRE0082.
  - `TautologicalGuard_EmitsPRE0153_OnNonnegativeFieldNonnegativeGuard` — `field X nonnegative; when X >= 0` → PRE0153.
  - `ContradictoryRule_EmitsPRE0155_OnDisjointRuleRanges` — `rule X > 10; rule X <= 5` → PRE0155 on the second.
  - `VacuousRule_EmitsPRE0154_WhenPredicateAlwaysTrue` — `field X nonnegative; rule X >= 0` → PRE0154.
  - `ScopeCut_DoesNotEmit_OnUnboundedOperand` — `field X integer; rule X > 0` (no upper bound) does NOT emit (soundness scope-cut).
  - `Disjunctive_BothBranchesMustBeUnsatisfiable` — `when (X > 100 and X < 50) or (X == 7)` does NOT emit PRE0082 (second branch is satisfiable).
- `CrossRowIntervalCompositionTests.cs` (new):
  - `BUG006_LoanRenewalCap_CompilesCleanAfterFix` — the verbatim BUG-006 repro compiles clean.
  - `RejectRowAbove_SubtractsFromNarrowedInterval` — minimal isolated cross-row composition test.
  - `NonContiguousDifference_FallsBackSoundly` — the case where the subtraction would split the interval; verifies the fallback path doesn't produce false-positive PRE0078.
- `SharpenedRoutingTests.cs` (new):
  - `F_LANG_SPEC_12_StateWithAllUnsatisfiableRows_EmitsDeadEnd` — a state whose every outgoing row has an unsatisfiable guard is reported as DeadEnd.

## Decisions

### Decision 1: Add a new "satisfiability scan" pass — not a sixth (eighth) strategy

**Stakes**: high

- **Rationale**: The existing strategies in `ProofEngine.Strategies.cs` are obligation-discharge-shaped — each takes a `ProofObligation` and returns "did this discharge?". Satisfiability is a *dual* question: "does ANY data configuration satisfy this guard / make this rule non-trivial?" — there is no obligation site, and the verdict is whole-construct, not per-expression. Folding satisfiability into `TryDischarge` would deform the dispatch shape. A dedicated lateral pass keeps the existing strategies intact (a structural property that `proof-engine.md § 11 Decision 1` deliberately bounded) and adds a clean second surface.
- **Tradeoff accepted**: A new top-level surface in `ProofEngine` requires a doc update at `docs/compiler/proof-engine.md § 6 Two-Pass Design` (rename to three-pass, or carve out "Satisfiability Scan" as a sub-pass of the two-pass shape). Manageable; the readiness plan explicitly frames Phase 5 as documentation-heavy.
- **Alternatives considered**:
  - **Eighth strategy** (extending the `TryDischarge` chain). Rejected — strategies are obligation-shaped and satisfiability has no obligation; the shape mismatch would force a `null`-site placeholder obligation that exists only to fit the dispatcher. Category error.
  - **Fold into `IncorporateForwardingFacts`**. Rejected — that function *consumes* facts from the graph analyzer; the satisfiability scan *produces* new facts (specifically the `UnreachableRowFact` variant). Conflating consumer and producer paths obscures the data flow.
  - **Run in the Graph Analyzer**. Rejected — `precept-language-spec.md § 0.5` explicitly excludes guard-dependent reasoning from graph analysis: *"Structural graph analysis treats all edges as traversable regardless of `when` guards"*. Satisfiability depends on guard reasoning, so it belongs in the proof engine.
- **Precedent**:
  - `docs/compiler/proof-engine.md § 11 Decision 1 — "Five strategies only, each a simple predicate — not a solver framework. ... New strategies are language changes, not tooling extensions. Adding a sixth strategy requires design review and documentation, not just implementation."* — the strategy set is structurally bounded; lateral additions are the precedented path.
  - `src/Precept/Pipeline/ProofEngine.cs:93-150` — the existing `Prove` body already has a clear "after collection, before discharge" insertion point at line 100-104 (between `IncorporateForwardingFacts` and the for-loop).
  - GHC `-Woverlapping-patterns` is implemented as a dedicated pattern-matrix analysis in GHC's source tree (`compiler/GHC/HsToCore/Match.hs`), not folded into the type-checker. Cited from the GHC user guide passage above.
- **Sources consulted for this decision**:
  - `docs/compiler/proof-engine.md § 11 Decision 1` — *"Five strategies only, each a simple predicate — not a solver framework."* Verbatim. This is the architectural floor that bounds the strategy set; lateral additions are the precedented way to expand scope without violating it.
  - `src/Precept/Pipeline/ProofEngine.cs:93-104` — *"`IncorporateForwardingFacts(graph.ProofFacts, obligations, semantics); var suppressDiagnostics = new bool[obligations.Count]; // Pass 2: Obligation Discharge for (int i = 0; i < obligations.Count; i++) {`"* — the clean insertion point between consumption and discharge.
  - `precept-language-spec.md § 0.5` — *"Structural graph analysis treats all edges as traversable regardless of `when` guards — it overapproximates reachability."* This is why the satisfiability scan cannot live in the graph analyzer.
  - GHC user guide (accessed 2026-05-26): *"the last pattern match in `f` won't ever be reached, as the second pattern overlaps it ... More often than not, redundant patterns is a programmer mistake/error, so this option is enabled by default."* GHC's analogue is a dedicated pass.
- **Strongest counter-evidence**: `precept-language-spec.md § 0.6 #6 — "Proof attribution is required, not optional. ... Proof results flow as structured data, not parsed prose — tooling and agents consume the proof model directly, never by parsing diagnostic message text."* could be read as requiring every verdict to flow through the obligation-discharge channel (which produces the proof ledger). Response: the structured-witness requirement is a *format* requirement on the diagnostic's payload, not a *channel* requirement on how the verdict is produced. The new pass emits `Diagnostic` records with the same structured attribution (field name, narrowed interval, conflicting rule index) via the existing `Diagnostic.Substitutions` channel. No counter-evidence found in `docs/compiler/proof-engine.md § 6 / § 7 / § 11` or `docs/compiler/diagnostic-system.md` after a walk.
- **Reversibility**: `Hard` (architecturally). Once the new pass is in the dispatch order, refactoring to fold it back into a strategy would require unifying obligation-shaped and verdict-shaped dispatch — a substantial refactor. From an author-facing perspective the placement is invisible — only architects see it — so the reversibility cost is internal-only.
- **Blast radius**: catalogs touched: `DiagnosticCode.cs` (3 new entries), `Diagnostics.cs` (3 new GetMeta entries + 1 entry's RelatedCodes refresh). Docs touched: `proof-engine.md § 6` (architecture), `proof-engine.md § 11` (new Decision lifted at promote), `precept-language-spec.md § 0.6` (5 obligations move from Specification-only to Implemented), `catalog-system.md` (diagnostic count bump). Samples touched: estimated 1-3 trip-tests during the sweep slice — verify in implementation.

### Decision 2: Witness representation — reuse `Diagnostic.Substitutions`; no new `ProofWitness` carrier type

**Stakes**: medium

- **Rationale**: The structured-attribution requirement (`precept-language-spec.md § 0.6 #6`) is about *format*, not *carrier type*. The existing `Diagnostic` record carries substitution arguments for message formatting; downstream consumers (LS hover, MCP `precept_diagnostic`, `precept_proofs`) read those substitutions as the structured payload. The existing `UnsatisfiableGuard` (PRE0082) catalog entry already uses this shape — substitution slots `{0}=guard text`, `{1}=event name`, `{2}=context suffix`. The three new codes mirror this. Introducing a parallel `ProofWitness` carrier type for the new verdicts would create two ways to do the same thing.
- **Tradeoff accepted**: The new diagnostic's structured payload (field name, narrowed interval as formatted text, conflicting rule index) lives in the substitution dict, not in a typed witness record. Downstream typed consumers parse the dict. Bounded cost — three new codes use this shape; the shape is identical to the existing PRE0082.
- **Alternatives considered**:
  - **New `SatisfiabilityWitness` carrier record** attached to the diagnostic. More type-safe; consumers get a strongly-typed witness. Rejected — adds a record type that no other proof verdict uses; introduces a per-diagnostic-shape carrier surface; inconsistent with PRE0082's already-shipped substitution-only shape.
  - **JSON blob in diagnostic message text**. Rejected — explicitly violates `precept-language-spec.md § 0.6 #6 — "never by parsing diagnostic message text."*
- **Precedent**:
  - `src/Precept/Language/Diagnostics.cs:764` — PRE0082 catalog entry uses substitution slots for structured payload.
  - `precept-language-spec.md § 0.6 #6` — structured proof attribution is required, but the channel is unspecified.
- **Sources consulted for this decision**:
  - `src/Precept/Language/Diagnostics.cs:764` — *"`DiagnosticCode.UnsatisfiableGuard => new(nameof(DiagnosticCode.UnsatisfiableGuard), DiagnosticStage.Proof, Severity.Warning, "Guard '{0}' on event '{1}' is unsatisfiable under the declared constraints{2} — this row can never fire", DiagnosticCategory.Proof, RelatedCodes: [DiagnosticCode.DivisionByZero, DiagnosticCode.SqrtOfNegative], ...)`"* — the substitution-slot shape the new codes follow.
  - `precept-language-spec.md § 0.6 #6` — *"Proof results flow as structured data, not parsed prose — tooling and agents consume the proof model directly, never by parsing diagnostic message text."* The channel is open; the format must be structured.

### Decision 3: No new `ProofRequirementKind` for satisfiability — these are not per-expression obligations

**Stakes**: medium

- **Rationale**: The `ProofRequirement` DU in `ProofRequirement.cs:42` attaches to a `TypedExpression` site and asks "is this expression safe in this context?" Satisfiability is a *whole-construct* verdict ("is the row's guard ever satisfiable? Are these two rules contradictory?"). Reusing the per-expression obligation channel for whole-construct verdicts is a category error. `proof-engine.md § 11 Decision 3` makes the typing-side stamping explicit: *"The proof engine reads `ProofRequirement` arrays stamped onto typed expressions by the type checker. It does NOT identify what needs to be proved."* Per-expression obligations are stamped by the type checker from catalog metadata; whole-construct satisfiability is identified by the proof engine itself, with no type-checker stamp.
- **Tradeoff accepted**: The `ProofRequirementKind` enum count stays at 11. The new verdicts use a separate code path. Downstream MCP `precept_proofs` may want a parallel surface to expose the new verdict kinds — a small additive DTO that does not affect the obligation surface.
- **Alternatives considered**:
  - **New `SatisfiabilityRequirement` ProofRequirement subtype**. Rejected for the category-error reason: there is no per-expression site to attach it to.
- **Precedent**:
  - `docs/compiler/proof-engine.md § 11 Decision 3` — *"Obligations Stamped by Type Checker, Not Identified by Proof Engine."*
  - `src/Precept/Language/ProofRequirement.cs:42 ProofRequirement(ProofRequirementKind Kind, string Description)` — confirms the per-expression base shape (Subject + Description per subtype; subtype carries the per-expression safety claim).
- **Sources consulted for this decision**:
  - `docs/compiler/proof-engine.md § 11 Decision 3` — *"The proof engine reads `ProofRequirement` arrays stamped onto typed expressions by the type checker. It does NOT identify what needs to be proved. ... Implementation consequence: The proof engine never imports `Operations`, `Functions`, `Types`, or `Actions` catalogs directly. It reads only the `ProofRequirements` arrays on typed expressions."* — the structural boundary at play.
  - `src/Precept/Language/ProofRequirement.cs:11-30` — *"`public abstract record ProofSubject; public sealed record ParamSubject(ParameterMeta Parameter) : ProofSubject; public sealed record SelfSubject(TypeAccessor? Accessor = null) : ProofSubject; ... [CatalogDU] public abstract record ProofRequirement(ProofRequirementKind Kind, string Description);`"* — the DU is shaped around per-expression subjects (ParamSubject / SelfSubject); a whole-row "subject" doesn't fit.

### Decision 4: All new diagnostics ship as Severity.Warning (not Error)

**Stakes**: high (author-visible)

- **Rationale**: Three converging reasons:
  1. **Precedent within Precept**: every existing dead-code-family diagnostic is Severity.Warning — `UnreachableState`, `DeadEndState`, `UnhandledEvent`, `AlwaysRejecting`, `StateAlwaysRejects` per `Diagnostics.cs:701-794`. The existing `UnsatisfiableGuard` (PRE0082) is **already** Severity.Warning. The new codes inherit the same family convention.
  2. **Author signal vs safety hole**: a dead guard / vacuous rule is an author *intent* mismatch (the predicate doesn't do what the author probably meant), not an unsafe operation. Safety holes (`DivisionByZero`, `SqrtOfNegative`, `NumericOverflow`) are Errors because the runtime would fault. The satisfiability findings produce no runtime fault — the precept's engine is operationally sound.
  3. **External precedent**: GHC `-Woverlapping-patterns` is a Warning (default-on). Dafny's `ProofDependencyWarnings.cs` writes these as warnings, not errors. Standard discipline for compile-time "this looks wrong" detection.
- **Tradeoff accepted**: A precept with a tautological guard compiles and produces an engine; the author gets a Warning but the precept ships. Promoting these to Error would block ship on a non-safety issue, which conflicts with the "Proven violations only" principle (`precept-language-spec.md § 0.6 #2`) — there is no proof of operational danger.
- **Alternatives considered**:
  - **All Error**. Rejected — promotes intent-noise to safety-block; inconsistent with the `UnreachableState`/`DeadEndState`/`PRE0082` precedent; conflicts with § 0.6 #2.
  - **Mixed: ContradictoryRule = Error, others = Warning**. ContradictoryRule is the most semantically dangerous case (the conjunction governs an empty set). Rejected anyway — the engine still ships and the runtime is sound (vacuously); the author signal is the same as the others. Mixed severities create reviewer-burden inconsistency within the same diagnostic family.
  - **All Info**. Rejected — too quiet; authors won't see them; defeats the purpose.
- **Precedent**:
  - `src/Precept/Language/Diagnostics.cs:701-794` — UnreachableState, DeadEndState, UnhandledEvent, AlwaysRejecting, StateAlwaysRejects, UnsatisfiableGuard all `Severity.Warning`.
  - GHC user guide `-Woverlapping-patterns`: Warning, default-on (cited verbatim above).
  - Dafny `ProofDependencyWarnings.cs`: ships these as warnings (cited verbatim above).
- **Sources consulted for this decision**:
  - `src/Precept/Language/Diagnostics.cs:701 — "DiagnosticCode.UnreachableState => new(nameof(DiagnosticCode.UnreachableState), DiagnosticStage.Graph, Severity.Warning, ...)"* — verbatim severity assignment for the graph-stage dead-code family.
  - `src/Precept/Language/Diagnostics.cs:764` — *"DiagnosticCode.UnsatisfiableGuard => new(nameof(DiagnosticCode.UnsatisfiableGuard), DiagnosticStage.Proof, Severity.Warning, ...)"* — verbatim severity assignment for the existing proof-stage entry this design wires up.
  - `precept-language-spec.md § 0.6 #2 — "Proven violations only. The language reports what is definitively broken, not what might be broken. ... when it speaks, it is right."* — confirms the proven-verdict bar; is silent on Warning vs Error. The Warning choice fits "definitively-proven authoring-intent gap" without overstepping into "definitively-proven safety violation."
  - GHC user guide (accessed 2026-05-26): *"More often than not, redundant patterns is a programmer mistake/error, so this option is enabled by default."* — Warning, default-on.
- **Strongest counter-evidence**: a reader could argue that `precept-language-spec.md § 0.1 #1 — "Prevention, not detection. Invalid configurations ... cannot exist. They are structurally prevented before any change is committed, not caught after the fact"* should promote any provably-broken construct to Error so the precept cannot even build. Response: "invalid configurations" refers to *runtime data* configurations, not *author intent gaps*. A precept with a vacuous rule still prevents every invalid runtime configuration the field bounds describe — the rule just adds nothing. The Warning signals the gap without blocking ship. No counter-evidence found in `docs/philosophy.md` after walking the "What makes it different" section.
- **Reversibility**: `Hard`. Severity escalation (Warning → Error) breaks builds for downstream authors who already shipped clean precepts but had a tautology in a rule. Severity de-escalation (Error → Warning) is harmless. Starting at Warning is the conservative direction.
- **Blast radius**: 3 new codes' severity, author-visible. CI gating tools that key on severity (e.g., `--treat-warnings-as-errors` flags) will treat these as failures if so configured; that's the consumer's choice, not Precept's contract.

### Decision 5: BUG-006 — extend `BuildNarrowedIntervals` with sibling-row reject composition; not a new strategy

**Stakes**: medium

- **Rationale**: The narrowing infrastructure already exists at `ProofEngine.Intervals.cs:386-441 BuildNarrowedIntervals`. BUG-006 is precisely the gap that this function reads the *current row's* guard but ignores **sibling reject-rows** preceding it on the same `(state, event)`. The minimal sound extension is to extend the same function: walk preceding reject-rows, compute the interval each reject-row excludes, and subtract from the current row's per-field narrowed intervals. No new strategy, no new requirement subtype — the existing `IntervalContainmentProofRequirement` obligation discharge gets a tighter narrowed input.
- **Tradeoff accepted**: `NumericInterval` needs `Empty`, `IsEmpty`, and `Subtract` — three small API additions. The `Subtract` operation can produce a non-contiguous result (`[0, 10] ∖ [3, 5] = [0, 3) ∪ (5, 10]`); the implementation returns the original interval unchanged in that case — sound fallback (the proof is conservative; never wrong). The contiguous-difference case (`[0, 10] ∖ [5, ∞) = [0, 5)`) is the common one for reject-row guards on the upper end, which is exactly the BUG-006 shape.
- **Alternatives considered**:
  - **New `TryCrossRowIntervalProof` strategy**. Rejected — cross-row composition is a narrowing input improvement, not a new discharge mechanism. The discharge stays `TryIntervalContainmentProofNarrowed`; only the narrowing context is richer.
  - **Generalize to all preceding sibling rows (not just reject-rows)**. Tempting but unsound: a non-reject sibling `when G -> set X = ...` doesn't *exclude* the case where `¬G` from the current row's reachability; both rows may fire on different runs. Only reject-rows structurally exclude a region from reachability. Out of scope for V1.
  - **Add a `IntervalSet` carrier type that handles non-contiguous regions**. More precise (no fallback). Rejected for cost — the carrier type would need to propagate through `IntervalOfNarrowed`, `IntervalTransfer` for every binary op, and the existing `IntervalContainmentProofRequirement` check. A substantial refactor for the rare case. Re-evaluate if usage shows the contiguous-fallback losing common cases.
- **Precedent**:
  - `src/Precept/Pipeline/ProofEngine.Intervals.cs:386-441 BuildNarrowedIntervals` — the existing single-row narrowing path.
  - `docs/Working/bugs.md § BUG-006` verbatim — *"same architectural class as BUG-001 / BUG-004 — the proof engine's interval-narrowing strategy doesn't compose guard-derived field bounds with field-modifier-derived bounds across rows. ... probably fixable by extending the same narrowing infrastructure"*.
- **Sources consulted for this decision**:
  - `src/Precept/Pipeline/ProofEngine.Intervals.cs:399-413 BuildNarrowedIntervals` — *"`var guard = obligation.Context switch { TransitionRowContext t => t.Row.Guard, ... }; if (guard is null) return null; var branches = ExtractGuardBranches(guard); if (branches.IsEmpty) return null; // Use the first branch only — single-branch guards. ... foreach (var branch in branches) {`"* — verbatim: the function reads ONLY the current row's guard, never walks siblings. That's the BUG-006 gap.
  - `docs/Working/bugs.md § BUG-006` — verbatim: *"a precept with `field Counter as integer default 0 nonnegative` and `field MaxCount as integer max 5`, plus rows `from S on Inc when Counter >= MaxCount -> reject "..."` then `from S on Inc -> set Counter = Counter + 1 -> no transition`. The unguarded second row fails proof on `Counter + 1` containment."* — and *"same architectural family as the new dead-guard / contradictory-rule machinery"* (from `compiler-readiness-plan-2026-05-24.md § Phase 5`).

### Decision 6: Ship sequencing — F-LANG-SPEC-02 + -05 + -12 as Slice 1; -03 + -04 + BUG-006 as Slice 2

**Stakes**: low

- **Rationale**: F-LANG-SPEC-12 depends on F-LANG-SPEC-02 (the dead-guard verdicts feed the `UnreachableRowFact` propagation). F-LANG-SPEC-05 is the negation case of F-LANG-SPEC-02 — same `IsGuardUnsatisfiable` primitive, reused on the negated guard. Bundle all three. F-LANG-SPEC-03 (contradictory rules) and F-LANG-SPEC-04 (vacuous rules) target the *rule* surface, not the *guard* surface — distinct traversal (the rule list vs row guards). BUG-006 needs the `NumericInterval.Subtract` infrastructure that Slice 2 will add. Splitting the work reduces per-slice blast radius and lets Slice 1 ship with a smaller diagnostic-trip sweep surface.
- **Tradeoff accepted**: Two slices instead of one. Slight per-slice overhead; matches the readiness plan's "highest-variance phase" framing for Phase 5.
- **Alternatives considered**:
  - **All-at-once**. Rejected — larger diagnostic-trip surface in `samples/` per slice; one consolidated sweep means a single point of variance for the high-variance phase.
- **Precedent**:
  - `precept-language-spec.md § 0.6 #12 — "Sharpening of reachability and routing diagnostics from proven-dead guards"* — explicit dependency on dead-guard verdicts.
- **Sources consulted for this decision**:
  - `precept-language-spec.md § 0.6 Implementation Status` table — items 9, 10, 12 grouped (guard-side) vs items 7, 8 grouped (rule-side); the spec's own grouping aligns with this sequencing.

### Decision 7: Out-of-scope — F-LANG-BIZ-08 (discrete equality narrowing on choice fields)

**Stakes**: low

- **Rationale**: BIZ-08 is satisfiability-shaped (it asks "given `when Priority == 'High'`, can the proof engine narrow `Priority`'s value set to `{'High'}`?"), but operates on *choice value sets* — a discrete lattice — not the bounded-numeric-interval lattice this design extends. Per `docs/Working/bugs.md § F-LANG-BIZ-08`: *"requires (a) a new proof strategy that recognizes `choiceField == literal` guards and narrows the field's discrete value set, and (b) a discrete-value interval representation (analogous to `BuildNarrowedIntervals` for scalar types)."* Two distinct extension surfaces.
- **Tradeoff accepted**: BIZ-08 stays in `bugs.md` Active for a future design pass. Phase 5 ships without it. The five obligations § 0.6 calls out are the immediate target; discrete narrowing is a follow-up.
- **Alternatives considered**:
  - **Include BIZ-08 in this design**. Rejected — different lattice (discrete value set vs numeric interval); different traversal target (choice equality vs numeric comparison); doubles the design surface and the falsifier list. Cleaner to ship the numeric surface first, then design the discrete surface against the same architectural shape (lateral pass + new diagnostic codes).
- **Precedent**:
  - `docs/Working/bugs.md § F-LANG-BIZ-08` — explicit scope-out reasoning.
- **Sources consulted for this decision**:
  - `docs/Working/bugs.md § F-LANG-BIZ-08` — *"`ProofEngine.Strategies.cs` narrowing infrastructure handles numeric interval narrowing via `BuildNarrowedIntervals`. There is no corresponding mechanism to recognize `when X == 'literal'` for a choice field and narrow `X`'s set of possible values to `{'literal'}` in the guard-true branch."* — confirms the distinct extension surface.

## Acceptance criteria

1. The BUG-006 minimal repro from `docs/Working/bugs.md § BUG-006` compiles clean (proof-ledger snapshot test).
2. A precept with `rule X > 10 because "..."` and `rule X <= 5 because "..."` emits `PRE0155 ContradictoryRule` (diagnostic snapshot test).
3. A precept with `field X nonnegative` and `rule X >= 0 because "..."` emits `PRE0154 VacuousRule`.
4. A precept with `field X nonnegative` and `when X >= 0 -> ...` on a transition row emits `PRE0153 TautologicalGuard`.
5. A precept with `when X > 100 and X < 50 -> ...` emits `PRE0082 UnsatisfiableGuard` (the existing PRE0082 is finally wired).
6. A precept with a state whose every outgoing event row has a proven-unsatisfiable guard emits `PRE0108 DeadEndState` (F-LANG-SPEC-12 sharpened routing).
7. `precept_diagnostic("TautologicalGuard")`, `precept_diagnostic("VacuousRule")`, `precept_diagnostic("ContradictoryRule")` MCP tool calls return full structured metadata (catalog-driven; works automatically when `Diagnostics.cs` `GetMeta` entries are present).
8. Soundness scope-cut verified: `field X integer` (no upper bound) + `rule X > 0 because "..."` does NOT emit `VacuousRule` (the `Unbounded` operand suppresses the verdict).
9. Disjunctive guard `when (X > 100 and X < 50) or (X == 7) -> ...` does NOT emit `UnsatisfiableGuard` (second branch is satisfiable).
10. Sample corpus: every sample in `samples/` either compiles clean OR has its noise diagnostic resolved by the sweep slice (per-slice variance, captured in the execute stage).
11. All four test projects green: `Precept.Tests`, `Precept.LanguageServer.Tests`, `Precept.Mcp.Tests`, `Precept.Analyzers.Tests`.

## Dependencies

- **Upstream**: Phase 4 W-G (TypedMemberAccess `ChoiceMetadata` slot, F-LANG-COLL-06) and W-E (ParamSubject framework for accessors/actions, F-LANG-COLL-04/09) already shipped. No additional upstream design work needed.
- **Downstream**: F-LANG-BIZ-08 (discrete narrowing) will consume the satisfiability-scan pass surface if it eventually ships. Future quantifier-binding satisfiability would extend the same surface.

## Doc-update enumeration

Per CLAUDE.md routing table:

| Change | Doc |
|---|---|
| 5 of 13 § 0.6 obligations move from Specification-only → Implemented | `docs/language/precept-language-spec.md § 0.6 Implementation Status` — remove items 7, 8, 9, 10, 12 from the Specification-only table; add the corresponding test reference |
| New ProofEngine pass design rationale | `docs/compiler/proof-engine.md § 11 Decision N (new)` — lift D-1 from this design at promote-time |
| New pass narrative | `docs/compiler/proof-engine.md § 6 Two-Pass Design` — rename to "Two-Pass Design + Satisfiability Scan" (or "Three-Pass Design"); add narrative for the new pass between obligation collection and discharge |
| Sibling-row interval composition | `docs/compiler/proof-engine.md § 7 Subject Resolution Utilities / Interval Composition` (new sub-section) — describe the cross-row reject-guard composition for BUG-006 |
| 3 new diagnostic codes | `docs/compiler/diagnostic-system.md` — add `TautologicalGuard` (PRE0153), `VacuousRule` (PRE0154), `ContradictoryRule` (PRE0155) entries with full template |
| PRE0082 status update | `docs/compiler/diagnostic-system.md § PRE0082 UnsatisfiableGuard` — update Status from "catalog-only" to "Implemented"; cross-reference the three new codes via RelatedCodes |
| Diagnostic count bump | `docs/language/catalog-system.md` — bump from the post-FieldNeverSet count (150 if W-D Slice 4 has shipped) to that count + 3 |
| Phase 5 marker | `docs/Working/compiler-readiness-plan-2026-05-24.md § Phase 5` — mark cluster complete; archive scaffolding state |
| BUG-006 closure | `docs/Working/bugs.md § BUG-006` — mark Fixed by Phase 5 Slice 2; run post-fix cleanup per the existing checklist |

## Operational dimensions

- **Security**: N/A — does not touch source-text ingestion. The satisfiability scan is bounded by guard-expression depth × number of fields × number of rules; the lexer's 64KB hard ceiling already bounds the input.
- **Observability**: Affected. New verdicts surface in `precept_compile` diagnostic output via the normal channel. The proof ledger SHOULD include the new verdict kinds so `precept_proofs` consumers can list them — small additive DTO change tracked in the execute slice, not gated by this design. Hover surface for guard/rule satisfiability verdicts is a future enhancement.
- **Evolvability**: N/A — no dependency on external standards (NodaTime, ICU, UCUM, ISO 4217, TZDB). The interval algebra is self-contained.

## Falsifiers

External-author-visible change → 2-5 specific observations that would force redesign post-ship.

- **If three or more samples need their existing rules deleted as `VacuousRule`-flagged decoration**: the vacuity detection is too broad; tighten so the rule's predicate must be a SYNTACTIC match against the field's modifier-derived bound, not just a semantically-equivalent one. (Bound: 3 samples = signal; 1-2 = expected migration cost.)
- **If `precept_compile` p99 latency exceeds 100ms on the median sample after the satisfiability scan ships**: the new pass is too expensive — likely the contradictory-rule O(N²) pair scan or repeated narrowing computation. Optimize via per-field rule indexing (group rules by referenced field; only check pairs within a group) and cache per-rule narrowing.
- **If a single domain expert in a usability test cannot interpret a `TautologicalGuard` or `ContradictoryRule` diagnostic message within 60 seconds**: the audience-fit claim is falsified; rewrite the message templates with concrete domain-vocabulary examples (e.g., "Counter >= 0 — Counter is already nonnegative") rather than abstract field-and-range pairs.
- **If BUG-006's fix proves to be unsound** — i.e., the cross-row reject composition allows an actual interval-containment violation through (a precept compiles but `Counter + 1` overflows at runtime): back out D-5 and route BUG-006 through a documented runtime-rule workaround. The `CrossRowIntervalCompositionTests` in CI must catch this; if they pass and the field still reports it, the test corpus is incomplete and needs expansion.
- **If F-LANG-SPEC-12's sharpened routing produces a `DeadEndState` Warning on a state that the author considers reachable** (e.g., the state IS reachable via dynamic data not visible at compile time): the proof scope is overreaching. Tighten the routing dependency to only sharpen when ALL outgoing edges are unsatisfiable under the **conservative** narrowing (already the rule; verify in tests). If still overreaching, demote the integration to opt-in.

## Open Questions

None. PRE codes 153, 154, 155 verified free against `DiagnosticCode.cs` (current top: 152 = `MaxplacesCurrencyQualifierNotStatic`). `ProofForwardingFact` DU extension verified open at `StateGraph.cs:91` (already four sealed variants; adding a fifth follows the pattern). All decisions carry stakes classifications, four legs, and source citations with verbatim excerpts. Falsifiers section present (5 entries). Doc-update enumeration present (9 entries). No `Stakes: irreversible` decisions; no cooling-off required.
