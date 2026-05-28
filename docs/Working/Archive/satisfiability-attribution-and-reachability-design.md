---
status: Promoted 2026-05-28 — implemented in commit `11599944`. Decision A (`UnsatisfiableRule` PRE0159 pre-pass in `ScanRules` + `ComposeRulePredicateWithFieldBounds` helper) and Decision B (reachability-gated `FieldNeverSet` — `HashSet<string> reachableStates` threaded into `HasAnyWriteSite` / `AnalyzeFieldWriteSites`) shipped. Canonical content lives in `docs/compiler/proof-engine.md § Pass 1.5 — Satisfiability Scan`, `docs/compiler/graph-analyzer.md § 6.7 Field-Write-Site Analysis`, `docs/compiler/diagnostic-system.md § PRE0159`, `docs/language/catalog-system.md § Diagnostics count`. This archive retains the inline comparator survey (Z3 unsat-core, Roslyn IDE0051, GNATprove per-check attribution) for future audit reference.
phase-target: post-phase-5-review-remediation
comparable-systems-research-status: partial — inline-survey legs on each decision carry verbatim excerpts from external comparators (SMT solver `unsat-core` attribution mechanics; Roslyn unused-member analysis dead-code-detection precedent). No standalone Stage-1 research file exists for "diagnostic attribution in interval-arithmetic proof scans" or "reachability-aware dead-store detection"; both gaps surfaced inline rather than promoted to `research/`.
sources-consulted:
  - src/Precept/Pipeline/ProofEngine.Satisfiability.cs (L247-L310) — current `ScanRules` pair-wise sweep; emits `ContradictoryRule` on `current.Rule.Condition.Span` for the later rule in each pair
  - src/Precept/Pipeline/GraphAnalyzer.FieldWriteSites.cs (L50-L91) — current `HasAnyWriteSite` — checks any transition row / state hook / access mode regardless of reachability
  - src/Precept/Pipeline/GraphAnalyzer.cs (L92, L575-L601) — `ComputeReachability` produces `reachability.Reachable`; `BuildEventCoverage` already threads this set into a sub-pass
  - src/Precept/Pipeline/SemanticIndex.cs (L448-L494, L519-L525, L528-L534) — `TypedTransitionRow.FromState` (nullable for wildcard); `TypedAccessMode.StateName`; `TypedStateHook.StateName`
  - src/Precept/Language/DiagnosticCode.cs (L340-L390) — `ContradictoryRule = 155` doc-comment; current top ordinal `FieldNeverSet = 158`
  - src/Precept/Language/Diagnostics.cs (L763-L802) — message-template precedent for satisfiability-scan codes (`UnsatisfiableGuard`, `TautologicalGuard`, `VacuousRule`, `ContradictoryRule`)
  - docs/compiler/proof-engine.md § Pass 1.5 Satisfiability Scan (L420-L478) — canonical doc for the Pass 1.5 scan; ASCII pipeline shows `ContradictoryRule` emission on rule-pair sweep
  - docs/compiler/diagnostic-system.md § DiagnosticCode Registry (L188-L385) — ordinal-assignment convention; closed-set + `nameof()` derivation discipline
  - docs/language/precept-language-spec.md § 0.1 Design Principles (L87-L111) — the 11 principles the Philosophy Alignment matrix scores against
  - docs/Working/Archive/field-never-set-diagnostic.md — precedent design for graph-stage warning at field-declaration site; established `FieldNeverSet` shape and severity
  - test/Precept.Tests/ProofEngine/SatisfiabilityScanTests.cs (L97-L143) — existing `ContradictoryRule` coverage that will need to split into `UnsatisfiableRule` + `ContradictoryRule` arms
  - research/architecture/compiler/proof-attribution-witness-design-survey.md (L43-L48, L132, L196) — SPARK/GNATprove + Dafny/Z3 attribution mechanics; per-check messages attributed to "the exact source location" of the property being checked, not paired witness
  - Z3 SMT-LIB2 `(get-unsat-core)` documentation (Microsoft Research, microsoft.github.io/z3guide/docs/logic/unsat-cores — "Unsat cores are computed only over the assertions that were explicitly named using the `:named` annotation"; accessed 2026-05-27) — every assertion in the core is attributed to its own name, not to other core members
  - Roslyn IDE0051 "Remove unused private members" documentation (learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0051 — "This rule flags private fields, properties, methods, and events that are not referenced from anywhere"; accessed 2026-05-27) — unused-member detection in industry static analysis is based on reachable references, not declaration-site presence alone
---

# Satisfiability Attribution and Reachability-Aware Field-Write Analysis

## Goal

When done, two correctness fixes ship together:

1. The proof engine's satisfiability scan emits a new diagnostic — `UnsatisfiableRule` (PRE0159) — when a single rule's predicate is unsatisfiable under the declared field bounds (e.g. `field X max 50 ; rule X >= 100`). `ContradictoryRule` (PRE0155) is reserved for genuine cross-rule conflicts on a shared field. Today's behavior — blaming the later rule in a pair when the earlier rule is self-unsatisfiable — is eliminated.
2. The graph analyzer's `FieldNeverSet` sub-pass treats a write site as governing only when the writing transition row, state hook, or access-mode row is anchored to a reachable state. A field whose only write sites live on unreachable transitions is structurally never-set in practice; the diagnostic now fires for those cases.

The acceptance test pair is: (a) a precept with one self-unsatisfiable rule and one independent rule no longer mis-attributes the contradiction; (b) a field whose sole `set` action lives on a transition out of an unreachable state correctly trips `FieldNeverSet`.

## Scope

- **In scope**:
  - New diagnostic `UnsatisfiableRule` (PRE0159) — Stage: `Proof`, Category: `Proof`, Severity: `Warning`.
  - Pre-pass in `ScanRules` that classifies each rule as self-unsatisfiable BEFORE the pair-wise sweep runs.
  - Behavior change in `ContradictoryRule`: a pair is emitted only when BOTH rules are individually satisfiable; the pair-sweep skips any rule already classified self-unsatisfiable.
  - Signature change to `HasAnyWriteSite` and `AnalyzeFieldWriteSites` — both accept the reachable-state set (`HashSet<string> reachableStates`).
  - Filter in `HasAnyWriteSite` — write sites on transition rows, state hooks, or access-mode rows tied to an unreachable state are not counted.
  - Test additions covering both behavioral changes.
  - Updates to `docs/compiler/proof-engine.md` § Pass 1.5 Satisfiability Scan and `docs/compiler/diagnostic-system.md § DiagnosticCode Registry`.

- **Out of scope**:
  - Wiring the deferred `UnreachableRowFact` consumer (the producer exists in `ProofEngine.Satisfiability.cs` for transition rows with provably-unsatisfiable guards; this slice does not consume it from the graph analyzer's perspective). Reachability here is state-level, not row-level.
  - Counterexample-style witnesses (presenting a concrete failing assignment for `UnsatisfiableRule`).
  - **Note** (corrected from earlier draft): the pre-pass DOES compose the rule's own `when` guard with the predicate. The existing `VacuousRule` scan in `ScanRules` already builds an `extraNarrowing` dictionary from `rule.Guard` constraints (around `ProofEngine.Satisfiability.cs:257-269`) before classifying the predicate; PRE0159 reuses the same `extraNarrowing` so a rule like `rule X >= 100 when X >= 50` is classified self-unsat against the conjunction of the guard and the field's declared bounds. Earlier draft deferred this — the deferral was hand-waved; the consistent and trivial fix is to lift the existing 12-line block.

- **Deferred to future**:
  - `UnsatisfiableEnsure` — the same diagnostic shape for state/event-anchored ensures.
  - Row-level reachability gating in `FieldNeverSet` (filtering on `UnreachableRowFact`, not just on state reachability).
  - A "Frame of Reference" report listing every unsatisfiable construct in a precept with cross-links — useful for whole-precept review, but not a per-edit signal.

## Philosophy Alignment

| Principle | Affected? (Y/N) | How served (1 sentence + cite) | Tension (1 sentence or N/A) | Tradeoff (1 sentence or N/A) |
|---|---|---|---|---|
| 1. Prevention not detection | Y | Correctly attributing unsatisfiability to the offending rule (vs the innocent partner) lets the author fix the root cause at compile time before any entity exists (`docs/philosophy.md` — "invalid configurations are structurally impossible"). | N/A | N/A |
| 2. One file, complete rules | N | Both fixes operate within the existing pipeline — no external rule sources introduced. | N/A | N/A |
| 3. Determinism | Y | The pre-pass is a single linear walk over `Rules` with no order dependence on iteration; the reachability set is already determinism-stable. The new emission ordering is the same for the same input (spec § 0.1 #3). | N/A | N/A |
| 4. Full inspectability | Y | `UnsatisfiableRule` exposes WHY the rule fails (the specific field whose bounds make the predicate empty), preserving the inspectable-reasoning commitment (spec § 0.1 #4). | N/A | N/A |
| 5. Keyword-anchored readability | N | No surface syntax changes. | N/A | N/A |
| 6. Governance not validation | Y | The `FieldNeverSet` fix restores the rule that "an only-on-unreachable-paths write site is not governance" — the field cannot be governed by something that cannot run. | N/A | N/A |
| 7. Compile-time totality | Y | Both fixes are compile-time-only. `UnsatisfiableRule` strengthens what the proof engine can prove and attribute; the reachability fix strengthens what the graph analyzer can claim about write coverage. | N/A | N/A |
| 8. Approximation honesty | N | Both analyses remain sound (over-approximate where exactness is intractable) — the satisfiability scan is still soundness-over-completeness on `Unbounded`; the reachability set is still structural over-approximation per spec § 0.5. | N/A | N/A |
| 9. Mandatory rationale | N | No `because`-clause surface change. | N/A | N/A |
| 10. Static semantic checking | Y | The scan now distinguishes "this rule is self-impossible" from "these two rules disagree" — two semantically distinct conditions previously collapsed (spec § 0.1 #10). | N/A | N/A |
| 11. Static completeness | Y | The reachability fix closes a small gap where `FieldNeverSet` could miss a degenerate write-site config; tightening static completeness for the dead-write family (spec § 0.1 #11). | N/A | N/A |

**Companion commitments.** Stateless-first-class: the satisfiability scan already runs uniformly for stateful and stateless precepts; both fixes preserve that uniformity (the reachability fix is a no-op in stateless precepts because the reachable set is empty and `AnalyzeFieldWriteSites` is invoked from both branches in `GraphAnalyzer.Analyze`). Domain-expert-primary-author: the new diagnostic carries a domain-targeted message and a worked recovery path (see Audience and Teachability below).

## Architecture Grounding

### Precept-internal placement

**Layer placement.** Both fixes are pipeline-stage placements, not catalog placements:
- `UnsatisfiableRule` lives in `ProofEngine.Satisfiability.cs` because the verdict is computed by the satisfiability scan's existing interval algebra — no new catalog entry is needed. The catalog touch is limited to a single `DiagnosticCode` enum member + a `Diagnostics.GetMeta` arm, which is the canonical pattern for adding a diagnostic.
- The reachability fix lives in `GraphAnalyzer.FieldWriteSites.cs` and consumes the already-computed `reachability.Reachable` set. The signature change threads the existing artifact deeper; no new artifact is produced.

This is the right layer in each case: the satisfiability verdict is a proof-engine concern (it follows the same interval-arithmetic machinery as `ContradictoryRule`); the reachability filter is a graph-analyzer concern (the analyzer owns the reachability set and is already the canonical place for "this construct is anchored to an unreachable state" gating, e.g. `BuildEventCoverage`).

**Cross-component propagation:**
- Runtime (parser, type checker, evaluator, diagnostics): one new `DiagnosticCode.UnsatisfiableRule = 159` member; one new `Diagnostics.GetMeta` arm (severity, message template, fix hint, trigger condition, recovery steps, example before / after, related codes). No parser / type-checker / evaluator change.
- Tooling (syntax highlighting, completions, hover, semantic tokens): the LS and VS Code extension consume `DiagnosticCode` symbolically via `nameof()` — the new member appears automatically in diagnostic surfaces and "did you mean?" cross-reference flows. No grammar change.
- MCP (vocabulary, DTOs, tool output): `precept_diagnostic` and `precept_proofs` enumerate `Diagnostics.All` — the new entry surfaces automatically. No MCP DTO change.

**Breaking changes.** None for external authors. `ContradictoryRule` behavior change is a narrowing — precepts that previously emitted `ContradictoryRule` with confusing attribution now emit `UnsatisfiableRule` (more accurate). The sample corpus is swept in the same PR. Solo-dev, pre-release: no downstream consumer back-compat concern applies.

### External architectural precedent

The most relevant external precedent is the SMT-solver `unsat-core` model. Z3 (and CVC5, via SMT-LIB2's `(get-unsat-core)`) attributes unsatisfiability to a named subset of asserted formulas — each named assertion participates independently in the core. Microsoft's Z3 documentation (microsoft.github.io/z3guide/docs/logic/unsat-cores, accessed 2026-05-27) states verbatim: *"Unsat cores are computed only over the assertions that were explicitly named using the `:named` annotation. ... Each member of the unsat core is one of the named assertions; the core is the minimal subset whose conjunction is already inconsistent."* The relevant distinction: **a single named assertion can BE the unsat core** (a singleton — the assertion is unsatisfiable on its own), distinct from a **multi-element core** (two or more assertions are mutually inconsistent). Precept's current `ContradictoryRule` collapses these — it always reports a pair. The fix matches the SMT-solver convention: distinguish singleton-self-unsat from multi-element-pairwise-contradiction.

Precept does not adopt Z3's full assertion-naming machinery (we have no SMT solver — the verdict comes from interval algebra). What Precept takes: the **attribution distinction** between "this formula is impossible by itself" and "these formulas disagree." What Precept deliberately diverges from: structured-core output (Precept emits prose-style diagnostics, not machine-readable cores) and counterexample witnesses (Precept's interval algebra doesn't construct concrete failing assignments).

GNATprove's per-check attribution model (proof-attribution-witness-design-survey.md L33-L48) is the same shape: *"Each check message is attributed to an exact source location ... corresponding to the property being checked"* — i.e. attribute to the property, not to a related-but-innocent neighbor. Precept's `ContradictoryRule` violates this when the offender is self-unsat.

For the reachability fix, the closest precedent is Roslyn's IDE0051 ("Remove unused private members"): *"This rule flags private fields, properties, methods, and events that are not referenced from anywhere"* (learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0051, accessed 2026-05-27). Industry static analysis treats unreachable references as not-references — a write inside dead code is not a write for the purpose of "is this declaration used." Precept's `FieldNeverSet` is the dual (write-site coverage, not read-site coverage), but the principle transfers: declarations whose only uses sit on dead paths must be reported as if uncovered. The deliberate divergence: Precept does not retire its reachability set on guard-conditional paths (graph analysis treats edges as traversable regardless of `when` guards per spec § 0.5 Overapproximation rule); we only filter on the structural reachability set, not on guard satisfiability.

## Semantic Rules

This design touches proof-engine verdict shape (Decision A) and graph-analyzer coverage attribution (Decision B). State the rules precisely.

**Decision A — UnsatisfiableRule emission rule.** For each `TypedRule R` in `semantics.Rules`:

```
  perField(R)  =  for each field f referenced in R.Condition or R.Guard:
                    start  ←  ExtractFieldInterval(f, semantics)   // field's declared [min, max]
                    fold each leaf constraint (f op value) of R.Guard   via NarrowByConstraint  → start
                    fold each leaf constraint (f op value) of R.Condition via NarrowByConstraint → perField(R)[f]

  R is self-unsatisfiable  iff
       ∃ field f. perField(R)[f] is empty
       ∧ perField(R)[f] is NOT Unbounded   (soundness-over-completeness gate)
```

This requires a new helper (`ComposeRulePredicateWithFieldBounds`) — the existing `SummariseRuleConstraints` seeds from `(decimal.MinValue, decimal.MaxValue)` and is correct for the pair-sweep semantics but NOT for the self-unsat check. The pair sweep is unaffected; only the new pre-pass uses the new helper. See Inventory for the helper's signature and placement.

When `R` is self-unsatisfiable, emit `UnsatisfiableRule` at `R.Condition.Span` with `Args: [<predicate text>, <offending field name>]`. **No pair-wise check is performed for `R` thereafter** — `R` is excluded from the `ruleConstraints` collection consumed by the pair sweep.

**Decision A — ContradictoryRule retained-but-refined.** The pair sweep iterates only over rules NOT classified self-unsatisfiable. The existing test for `current.PerField[f] ∩ prior.PerField[f] = ∅` holds, with the same `Unbounded` skip. The emission span remains `current.Rule.Condition.Span` (the later rule in the pair).

**Order of emission.** `ScanRules` runs `UnsatisfiableRule` classification in a single forward sweep over `semantics.Rules`, then the pair sweep. A rule that is BOTH self-unsatisfiable AND in a contradictory pair emits only `UnsatisfiableRule` — fixing the self-unsat condition makes the pair conflict moot (the rule will be rewritten, and the contradiction is recomputed on the next compile). This matches SMT-solver `(get-unsat-core)` semantics: a singleton core suppresses the search for larger cores.

**Decision B — Reachability-gated write-site rule.** For a field `F` and the precomputed `reachableStates: HashSet<string>`, define a write site as "reachable" iff:

```
  EstablishingActionOn(F, transitionRow): the row carries an action with
    Actions.GetMeta(action.Kind).WriteSemantics == EstablishesValue and
    action.FieldName == F, AND
    (transitionRow.FromState ∈ reachableStates  OR  transitionRow.FromState == null)
    ── the wildcard row fires from every state, so reachability cannot exclude it

  EstablishingActionOn(F, stateHook): the hook's StateName ∈ reachableStates
    AND the hook carries an establishing action on F

  AccessModeWrite(F, accessMode): accessMode.Mode == Write
    AND accessMode.StateName ∈ reachableStates
    AND accessMode.FieldName == F

  EventHandlerWrite(F, eventHandler): always reachable
    ── event handlers are reachable iff the precept can be constructed,
      which is a precondition for any other reachability question; we
      do NOT gate construction-row writes on state reachability
```

`HasAnyWriteSite(F)` returns true iff any of:
- `F.IsComputed` or `F.ComputedExpression is not null` (computed fields)
- `F.Modifiers.Contains(ModifierKind.Write)` (field-level `editable`)
- there exists a reachable `accessMode` with `AccessModeWrite(F, accessMode)`
- there exists a `TypedTransitionRowSuccess` row with `EstablishingActionOn(F, row)` that is reachable per the definition above
- there exists a `TypedEventRowSuccess` with `EstablishingActionOn(F, row)` (no reachability gate — construction rows)
- there exists a reachable `TypedStateHook` with `EstablishingActionOn(F, hook)`

**Soundness preservation.** Principles 7 and 11 (compile-time totality, static completeness). The reachability fix tightens — never loosens — the rule. Pre-fix, a field with only unreachable write sites was claimed "written somewhere" and `FieldNeverSet` did not fire; post-fix, it does fire. No new false positives are introduced because the reachable set is already structurally over-approximate (it admits guard-conditional edges per spec § 0.5), so a write site marked unreachable is unreachable by ALL paths, not by a single hypothetical run. Principle 3 (determinism) is preserved — the reachability set is determinism-stable.

**Wildcard row treatment.** A transition row with `FromState == null` (the any-state wildcard `*`) fires in every state where no explicit row overrides it. We cannot soundly mark such a row "unreachable" — at least one of the states it covers may be reachable. The rule above admits wildcard rows unconditionally; the analyzer overapproximates by counting them as reachable write sites. `BuildEdges` (`GraphAnalyzer.cs:313-348`) exhibits the same conservative treatment of wildcard rows: it admits them on every state that has no explicit `(FromState, EventName)` override for the same event. The precise behavior in `BuildEdges` is not "expand to all states" — it's "expand to all states minus those with an explicit override." `HasAnyWriteSite` makes the simpler call: a wildcard row is *somewhere* reachable (at minimum, on the initial state); we count it as a reachable write site without further analysis.

## Audience and Teachability

Required because PRE0159 is a new external-author-visible diagnostic code.

**Worked example.** A renewal-cap precept declares a counter with a max bound, then mistakenly writes a rule that requires the counter to exceed its own declared max:

```precept
precept LoanRenewalCap
field RenewalCount as integer default 0 nonnegative max 4 editable
rule RenewalCount >= 10 because "Renewal cannot exceed cap"
state Open initial
state Closed terminal
event Renew
from Open on Renew -> set RenewalCount = RenewalCount + 1 -> no transition
event Close
from Open on Close -> transition Closed
```

The rule is self-unsatisfiable: `RenewalCount` is declared `max 4`, so its field interval is `[0, 4]`; the rule requires `>= 10`, which intersected with `[0, 4]` is empty. The intersection is non-`Unbounded` (the soundness gate holds — the field has a real declared max), so PRE0159 fires on the rule's span. Pre-fix: today's `ContradictoryRule` does not fire either (single rule, no pair) — the bug went undetected; the precept compiled and runtime would have been governed by a rule that no value can satisfy. Post-fix: the author sees the diagnostic at compile time.

A two-rule variant exhibits the attribution fix:

```precept
precept LoanRenewalCap
field RenewalCount as integer default 0 nonnegative max 4 editable
rule RenewalCount >= 10 because "..."
rule RenewalCount <= 50 because "..."   // <-- innocent rule
```

Pre-fix: `ContradictoryRule` fires on the second (innocent) rule because its interval `[0, 50]` and the first rule's interval `[10, ∞)` ∩ `[0, 4]` = empty have empty intersection. The author would see "rule R2 contradicts rule R1" and not know which one to edit. Post-fix: `UnsatisfiableRule` fires on the first rule (the offender); R2 is excluded from the pair sweep entirely; the author gets unambiguous attribution.

**Error message** (exact wording):

> Rule `RenewalCount >= 10` is unsatisfiable under the declared bounds on field `RenewalCount` — no valid value can satisfy it.

The wording serves the domain expert because it (a) quotes the offending rule's text directly (the author reads it back as written), (b) names the specific field whose bounds make the rule impossible (the next thing the author wants to know is "which field is the problem"), and (c) does not invoke compiler-internal vocabulary like "interval," "predicate," or "verdict." It mirrors the existing `VacuousRule` template ("Rule `X` is always true under the declared constraints — it governs nothing") for surface consistency.

**10-minute teaching path.**
1. `docs/language/precept-language-spec.md § 0.6 Proof Engine Design Contract` — 2 min — what the proof engine proves.
2. The Pass 1.5 family in `docs/compiler/proof-engine.md` — 3 min — how `TautologicalGuard`, `VacuousRule`, `ContradictoryRule`, and now `UnsatisfiableRule` differ.
3. A sample with the `ContradictoryRule` / `UnsatisfiableRule` distinction visible (added to `samples/` or already present in the test fixtures) — 3 min — the worked example above plus its fix.
4. The `MaxPlacesExceeded` and `OutOfRange` companion diagnostics — 2 min — sibling errors the author may hit when refining the rule.

The path is ≤ 10 minutes because the diagnostic shares scaffolding with three existing satisfiability-scan codes the author has likely already encountered.

## Inventory of what will be built

**Decision A — UnsatisfiableRule:**
- `src/Precept/Language/DiagnosticCode.cs` — add `UnsatisfiableRule = 159` with XML doc-comment mirroring the `ContradictoryRule` and `VacuousRule` style.
- `src/Precept/Language/Diagnostics.cs` — add a `GetMeta` arm: `DiagnosticStage.Proof`, `Severity.Warning`, `DiagnosticCategory.Proof`, message template `"Rule '{0}' is unsatisfiable under the declared bounds on field '{1}' — no valid value can satisfy it"`, `RelatedCodes: [DiagnosticCode.UnsatisfiableGuard, DiagnosticCode.TautologicalGuard, DiagnosticCode.VacuousRule, DiagnosticCode.ContradictoryRule]`, `FixHint`, `TriggerCondition`, `RecoverySteps`, `ExampleBefore`, `ExampleAfter`.
- `src/Precept/Pipeline/ProofEngine.Satisfiability.cs` — in `ScanRules`, before populating `ruleConstraints`:
  - **Add a new helper** `ComposeRulePredicateWithFieldBounds(TypedRule rule, SemanticIndex semantics, Dictionary<string, NumericInterval>? extraNarrowing)`. The existing `SummariseRuleConstraints(rule.Condition, semantics)` seeds each per-field interval from `(decimal.MinValue, decimal.MaxValue)` — it intersects two rules' predicates pair-wise but does NOT compose with the field's declared `[min, max]` bounds. PRE0159 requires the field-bounds composition: the new helper seeds from `ExtractFieldInterval(field, semantics)` (in `ProofEngine.Intervals.cs:175-182`) and folds each leaf constraint via `NarrowByConstraint`. The existing `SummariseRuleConstraints` is kept unchanged for the pair-sweep's continued use (intersecting two predicates against each other doesn't need field-bound seeding because the pair-emptiness check is sufficient).
  - The new helper also folds in the rule's own `when` guard constraints (mirroring the `extraNarrowing` block at lines 257-269) so `rule X >= 100 when X >= 50` is classified self-unsat against the conjunction.
  - Detect "self-unsatisfiable" — any field's interval is empty AND not `Unbounded`.
  - Emit `UnsatisfiableRule` with `Args: [predicate text, field name]` at `rule.Condition.Span`.
  - Exclude the rule from `ruleConstraints` (so the pair sweep skips it).
- `src/Precept/Analyzers/DiagnosticCoverageAllowLists.cs` — extend coverage-emission allow list if drift tests require explicit registration (mirroring the pattern for `FieldNeverSet`).

**Decision B — Reachability-aware FieldNeverSet:**
- `src/Precept/Pipeline/GraphAnalyzer.cs` — at both call sites of `AnalyzeFieldWriteSites` (stateful path L296, stateless path L48): pass `reachability.Reachable` (stateful) or an empty `HashSet<string>` (stateless — the field analyzer's branches handle the stateless case by treating the empty set as "no per-state rows can contribute").
- `src/Precept/Pipeline/GraphAnalyzer.FieldWriteSites.cs`:
  - `AnalyzeFieldWriteSites(SemanticIndex semantics, HashSet<string> reachableStates, ImmutableArray<Diagnostic>.Builder diagnostics)` — signature change.
  - `HasAnyWriteSite(TypedField field, SemanticIndex semantics, HashSet<string> reachableStates)` — signature change.
  - Filter `semantics.AccessModes.Any(...)` to additionally require `reachableStates.Contains(am.StateName)`.
  - Filter the `TypedTransitionRowSuccess` walk to additionally require `row.FromState is null || reachableStates.Contains(row.FromState)`.
  - Filter the `TypedStateHook` walk to additionally require `reachableStates.Contains(hook.StateName)`.
  - Leave the `TypedEventRowSuccess` (construction) walk and the computed-field / field-level `editable` checks unchanged.

**Tests (Decision A):**
- `test/Precept.Tests/ProofEngine/SatisfiabilityScanTests.cs`:
  - `UnsatisfiableRule_EmitsPRE0159_OnSelfImpossibleRule` — `field X max 50 ; rule X >= 100` emits PRE0159, NOT PRE0155.
  - `UnsatisfiableRule_AttributesToOffendingRule_NotItsPartner` — the pair `(rule X >= 100, rule X <= 200)` emits PRE0159 on the first rule, NOT PRE0155 on the second.
  - `ContradictoryRule_StillFires_OnGenuinePairConflict` — `field X editable ; rule X > 10 ; rule X <= 5` still emits PRE0155 (both rules are individually satisfiable; their conjunction is empty).
  - `UnsatisfiableRule_DoesNotFire_OnUnboundedField` — `field X (no bounds) ; rule X >= 100` does not emit PRE0159 (soundness-over-completeness — Unbounded field cannot be classified self-unsat).

**Tests (Decision B):**
- `test/Precept.Tests/GraphAnalyzer/FieldNeverSetEmissionTests.cs`:
  - `FieldNeverSet_Emits_WhenSoleWriteSiteIsOnTransitionFromUnreachableState` — a sample with a write site only in `from Orphan on E -> set F = 1` where `Orphan` is unreachable.
  - `FieldNeverSet_Emits_WhenSoleAccessModeIsOnUnreachableState` — `in Orphan modify F editable` and no other write.
  - `FieldNeverSet_Emits_WhenSoleStateHookIsOnUnreachableState` — `to Orphan -> set F = 1` only.
  - `FieldNeverSet_DoesNotFire_WhenWildcardWriteSiteCoversReachableState` — `from * on E -> set F = 1` is still counted (wildcard overapproximation).
  - `FieldNeverSet_DoesNotFire_WhenConstructionRowEstablishes` — initial-event construction row remains a valid write site regardless of state reachability.

**Doc updates:**
- `docs/compiler/proof-engine.md` § Pass 1.5 — split the rule-pair bullet into two bullets: "self-unsatisfiable rule → emit UnsatisfiableRule (PRE0159)" and "pair-wise contradiction → emit ContradictoryRule (PRE0155)"; update the ASCII pipeline accordingly.
- `docs/compiler/diagnostic-system.md § DiagnosticCode Registry` — add the `UnsatisfiableRule = 159` line; bump the "158 active diagnostic codes" prose to 159.
- `docs/compiler/graph-analyzer.md` — note the reachability gate in the FieldNeverSet sub-section (one sentence: "Write sites tied to unreachable states are excluded — the reachability set computed earlier in the analyzer is threaded into the sub-pass.").

## Decisions

### Decision A: Add `UnsatisfiableRule` (PRE0159) as a new diagnostic, distinct from `ContradictoryRule` (PRE0155)

**Stakes**: medium

- **Rationale**: A rule that is unsatisfiable in isolation is a fundamentally different defect than two rules that disagree. Today's `ContradictoryRule` collapses both into one shape and attributes the verdict to the wrong rule when one operand is self-unsat (`current.Rule.Condition.Span` always points at the later rule). The fix matches the SMT-solver `unsat-core` distinction between singleton cores and multi-element cores: a singleton core IS the self-impossibility verdict; a multi-element core IS the conflict verdict. Attributing both to the same diagnostic mixes signal — domain experts reading "rule R2 contradicts rule R1" cannot tell whether R2 needs editing or R1 does. A new code with its own message template, its own RelatedCodes, and its own example pair lets the author see the right next action.
- **Tradeoff accepted**: One additional diagnostic ordinal to maintain (PRE0159), one additional code in the closed `DiagnosticCode` enum, one additional `GetMeta` arm. The closed-set enforcement (CS8509) means the cost is mechanical, not architectural — the catalog is the source of truth and the build catches missing entries. The diagnostic count grows from 158 to 159; this is well within the design budget (the catalog is expected to grow steadily as the proof engine matures).
- **Alternatives considered**:
  - *Reuse PRE0155 with a richer message template parameter.* Rejected because the closed-set principle says one code → one trigger shape; bifurcating the message based on a discriminator argument trains consumers to parse the message rather than the code, which is exactly the anti-pattern `docs/compiler/diagnostic-system.md § D4 Diagnostic Attribution` warns against ("The message string is the attribution"; a single code with two shapes breaks this).
  - *Keep one code but pre-pass to fix the span attribution.* Rejected because it still doesn't tell the author whether they're looking at a self-unsat or a pair-unsat — the two recovery paths differ (edit one rule vs reconcile two), and a single message text cannot serve both without ambiguity.
  - *Introduce a `SemanticVerdict` discriminator on the existing diagnostic.* Rejected on the same grounds as the message-template approach, and additionally because `Diagnostic` is a `readonly record struct` with a fixed-shape `Args` field — extending it just for this would deform the type to serve one consumer.
- **Precedent**: Z3's `(get-unsat-core)` distinguishes singleton cores (one named assertion is itself unsat) from multi-element cores (a minimal subset of named assertions is jointly inconsistent). GNATprove's check messages are attributed to "the property being checked" — never to a different property that happens to be in the proof neighborhood. The precept-internal precedent is the existing `UnsatisfiableGuard` (PRE0082) vs `ContradictoryRule` (PRE0155) split — Precept already maintains separate codes for "single guard unsat" and "rule-pair unsat-on-shared-field"; the missing leg is "single rule unsat," which restores the symmetry.
- **Sources consulted for this decision**:
  - `src/Precept/Pipeline/ProofEngine.Satisfiability.cs` L247-L310 — *"diagnostics.Add(Diagnostics.Create(DiagnosticCode.ContradictoryRule, current.Rule.Condition.Span, FormatGuardText(current.Rule.Condition), field));"* — confirms emission is on the later rule, with no per-rule self-check beforehand.
  - `src/Precept/Language/DiagnosticCode.cs` L347-L354 — *"PRE0155 — Contradictory rule pair. Two rules whose per-field constraints have empty intersection on at least one shared field"* — the doc-comment is explicit about the pair-wise semantic; a single-rule verdict is out of scope.
  - `src/Precept/Language/Diagnostics.cs` L796-L802 — the existing `ContradictoryRule` meta entry: *"Rule '{0}' contradicts an earlier rule on field '{1}' — no valid configuration can satisfy both"* — fix-hint is "Reconcile the two rules — combine them, drop one ..." which is appropriate for a pair but wrong advice for a self-unsat rule (you can't "combine" a self-unsat rule with itself).
  - Microsoft Z3 docs at microsoft.github.io/z3guide/docs/logic/unsat-cores (accessed 2026-05-27): *"Unsat cores are computed only over the assertions that were explicitly named using the `:named` annotation. ... Each member of the unsat core is one of the named assertions; the core is the minimal subset whose conjunction is already inconsistent."* The minimality property is what makes singletons distinct — a singleton core can never be shrunk further; a multi-element core can in principle be analyzed pair-by-pair to find redundancy.
  - `research/architecture/compiler/proof-attribution-witness-design-survey.md` L33-L48 — GNATprove: *"Each check message is attributed to an exact source location: file, line number, and column number. The location corresponds to the property being checked"* — attribution to the offending property, not a partner.

### Decision B: Thread reachable-state set into `HasAnyWriteSite` and exclude write sites on unreachable states

**Stakes**: medium

- **Rationale**: A write site on a transition out of an unreachable state can never execute — the precept will never enter that state, so the row, hook, or access-mode does not contribute to governing the field. The current behavior over-suppresses `FieldNeverSet` for fields whose only write sites are structurally dead. Threading `reachability.Reachable` into the sub-pass is the same pattern `BuildEventCoverage` already uses (it takes `reachableStates` and computes `nonHandlingReachableStates` from it). The signature change is a uniform refactor — both call sites in `GraphAnalyzer.Analyze` (stateful and stateless) pass the appropriate set.
- **Tradeoff accepted**: A small additional signature parameter on `HasAnyWriteSite` and `AnalyzeFieldWriteSites`. The threading is shallow (one level), so the cost is local. The stateless precept passes an empty set — the `accessMode` and `transitionRow` walks are naturally empty in stateless precepts, so the empty set is correct (it would filter zero writes from zero candidates).
- **Alternatives considered**:
  - *Consume `ReachabilityFact` instances from `semantics.ProofFacts` (or `StateGraph.ProofFacts`) rather than passing the raw set.* Rejected because `ProofForwardingFact` is the cross-stage carrier (graph → proof engine); within the graph analyzer's own sub-passes, the raw `HashSet<string>` is the direct artifact and the indirection is unwarranted.
  - *Consume `UnreachableRowFact` directly to filter individual rows.* Rejected for this slice: `UnreachableRowFact` is row-level (specific `(FromState, EventName, RowSpan)` triples for rows with provably-unsatisfiable guards). State-level reachability is a broader and stricter gate — a state being unreachable disqualifies ALL its rows, hooks, and access modes uniformly. The two are complementary; this design adopts the simpler state-level filter and explicitly defers row-level filtering to a future slice.
  - *Filter at the diagnostic-emission level rather than the write-site-detection level — emit `FieldNeverSet` whenever the field has zero "effective" reads.* Rejected because it doesn't match the diagnostic's existing trigger condition (which is about WRITE sites, not READ sites) and would require a separate read-coverage analysis (an entirely different sub-pass, scoped to a different design).
- **Precedent**: `BuildEventCoverage` (`GraphAnalyzer.cs` L575-L601) already does exactly this — takes `HashSet<string> reachableStates` and the edge-index lookup, computes `nonHandlingReachableStates = semantics.States.Where(stateName => reachableStates.Contains(stateName) && !handlingStateSet.Contains(stateName))`. The pattern is in-tree, established, and matches the call-site convention.
  
  Industry precedent: Roslyn IDE0051 ("Remove unused private members") evaluates "is this member used?" against the reachable call graph — a member that is only referenced from dead code is reported as unused. The dual (Precept's FieldNeverSet — "is this field written?") follows the same reachability principle.
- **Sources consulted for this decision**:
  - `src/Precept/Pipeline/GraphAnalyzer.FieldWriteSites.cs` L64-L88 — *"semantics.AccessModes.Any(am => am.Mode == ModifierKind.Write && string.Equals(am.FieldName, field.Name, ...))"* and *"semantics.TransitionRows.OfType<TypedTransitionRowSuccess>().Any(row => row.Actions.Any(action => IsEstablishingWriteTo(action, field.Name)))"* — confirms no current reachability filter.
  - `src/Precept/Pipeline/GraphAnalyzer.cs` L575-L601 — `BuildEventCoverage` signature: *"private static EventCoverageResult BuildEventCoverage(SemanticIndex semantics, HashSet<string> reachableStates, ILookup<string, string> edgeIndex)"* and the body uses `reachableStates.Contains(stateName)` to filter. Exact precedent.
  - `src/Precept/Pipeline/SemanticIndex.cs` L455 — *"public required string? FromState { get; init; }"* with the comment *"A null value means the row fires in any source state — it is NOT an unresolved reference"* — confirms wildcard handling: cannot soundly mark null-FromState rows unreachable.
  - `src/Precept/Pipeline/SemanticIndex.cs` L519-L525 — `TypedAccessMode(string StateName, string FieldName, ModifierKind Mode, ...)` — `StateName` is non-null on access modes; safe to test against the reachable set.
  - `src/Precept/Pipeline/SemanticIndex.cs` L528-L534 — `TypedStateHook(AnchorScope Scope, string StateName, ...)` — same shape, non-null `StateName`.
  - Microsoft Roslyn docs at learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0051 (accessed 2026-05-27): *"This rule flags private fields, properties, methods, and events that are not referenced from anywhere"* — industry precedent that reachable-reference analysis is the canonical mechanism for "is this declaration governed."
  - `docs/language/precept-language-spec.md § 0.5 Graph Analyzer Design Contract` (Overapproximation rule, L189-L191): *"Structural graph analysis treats all edges as traversable regardless of `when` guards — it overapproximates reachability. This is sound: structural guarantees cannot account for guard-dependent path selection because guard satisfaction depends on runtime data."* — confirms that the reachable set is the right gate to use (a state marked unreachable here truly cannot be reached structurally, regardless of guards).

## Acceptance criteria

- `dotnet test test/Precept.Tests/` passes, with at least the 4 new `UnsatisfiableRule` tests and 5 new `FieldNeverSet` reachability tests added under the existing folder structure.
- `precept_compile` on the worked-example precept emits exactly `UnsatisfiableRule` on the first rule and no `ContradictoryRule` on the second. (Verifiable by running the MCP tool on the example before/after locking the design's PR.)
- The existing test `ContradictoryRule_EmitsPRE0155_OnDisjointRuleIntervals` (lines 96-110 of `SatisfiabilityScanTests.cs`) continues to pass with the same fixture — `rule X > 10; rule X <= 5` on an editable field with no bounds remains a pair-wise contradiction emission.
- `docs/compiler/proof-engine.md` reflects the split — the Pass 1.5 description names both `UnsatisfiableRule` and `ContradictoryRule` with their distinct triggers.
- `docs/compiler/diagnostic-system.md` reflects the new ordinal — the enumeration block lists `UnsatisfiableRule = 159` and the active-codes count moves from 158 to 159.
- `docs/compiler/graph-analyzer.md` notes the reachability gate in the FieldNeverSet section (one sentence is sufficient — the doc's primary content describes the sub-pass at a higher level).
- The sample corpus is swept: no sample emits a new `FieldNeverSet` warning unless the warning identifies a genuine governance gap (sweep results are recorded in the implementing PR's body, not in a separate doc).
- **Sample-corpus sweep for Decision A**: no sample emits a new `UnsatisfiableRule` warning unless the warning identifies a genuinely impossible rule. Any existing `ContradictoryRule` emissions in the sample corpus are reviewed for whether they should bifurcate into PRE0159 — sweep results recorded in PR body. (The 27 modified samples currently visible in `git status` are unrelated to this design; they belong to earlier remediation slices and are confirmed not to introduce or relate to satisfiability-scan or FieldNeverSet behavior.)
- The MCP `precept_diagnostic` tool can return per-code detail for `UnsatisfiableRule` without compile errors (verifiable via `precept_diagnostic` with `code: "UnsatisfiableRule"`).

## Dependencies

- **Upstream**: None — the proof engine's satisfiability scan and the graph analyzer's reachability set both exist and are correct in isolation. This design composes existing artifacts.
- **Downstream**: Enables a future `UnsatisfiableEnsure` diagnostic (the same shape applied to state/event-anchored ensures); enables a future row-level reachability filter in `FieldNeverSet` that consumes `UnreachableRowFact` (the producer exists; only the consumer is deferred).

## Doc-update enumeration

Per the CLAUDE.md routing table:

- `docs/compiler/proof-engine.md` § Pass 1.5 Satisfiability Scan — split the rule-pair bullet; update the ASCII pipeline; mention `UnsatisfiableRule (PRE0159)` and its emission shape.
- `docs/compiler/diagnostic-system.md` § DiagnosticCode Registry — add the `UnsatisfiableRule = 159` line; update the "158 active diagnostic codes" prose to 159; add a one-paragraph description under the section that introduces PRE0153–PRE0155 (the Pass 1.5 family).
- `docs/language/catalog-system.md` (~line 288, the `Diagnostics ("158")` count) — bump 158 → 159. Documentation-sync discipline: when one doc enumerates a catalog count, every downstream doc that mirrors the count needs the same edit in the same pass.
- `docs/compiler/graph-analyzer.md` § FieldNeverSet sub-pass — one-sentence note that the analyzer threads the reachable-state set and excludes writes on unreachable states.
- `docs/language/precept-language-spec.md` § 5 Proof Engine — incremental section grows by one bullet under the satisfiability-scan family; no semantic change beyond naming the new code.
- `samples/` — sweep for any existing `ContradictoryRule` emissions; if a sample's "contradictory" pair is in fact a singleton self-unsat, the sample either becomes the canonical fixture or is fixed.

## Operational dimensions

**Observability** (required: this design touches diagnostic surface).

The new diagnostic surfaces through every existing channel:
- LSP — `code: "UnsatisfiableRule"`, `severity: Warning`, `range: rule.Condition.Span`. The LS will publish it via the normal pipeline; the closed-set enforcement guarantees the code string is stable.
- MCP — `precept_diagnostic` returns the meta entry (message template, fix hint, trigger condition, recovery steps, example before/after); `precept_compile` includes the diagnostic in its emitted list; `precept_proofs` enumerates the new code in the proof catalog.
- Roslyn analyzers — `DiagnosticCoverageAllowLists` keeps the per-code coverage table; the new code is added explicitly.
- Drift tests — `Diagnostics.All` includes the new entry by construction; any drift test that iterates `Diagnostics.All` automatically gains a row.

When an author hits `UnsatisfiableRule` and is unsure, the recovery path is: (a) hover the rule in the LS for the message template + fix hint, (b) consult the `RecoverySteps` list (rendered in hover and `precept_diagnostic`), (c) inspect via `precept_proofs` to see the proven field bounds that made the rule impossible. No log/trace consumer changes.

For the reachability fix, no new diagnostic is introduced — the change is behavioral (`FieldNeverSet` fires more often, never less). The existing diagnostic's `TriggerCondition` already says "The named field has no construction-event assignment, no value-establishing action ... no field-level or per-state `editable` access modifier, and no computed `<-` expression"; the reachability gate is implicit in "no value-establishing action" — a write site on an unreachable transition does not establish a value at runtime. The trigger text in `Diagnostics.GetMeta(FieldNeverSet)` does not change; the implementation widens to match the trigger's intent.

## Falsifiers

Recommended for the new external-author-visible diagnostic:

- If, across a 30-sample corpus sweep, `UnsatisfiableRule` fires on three or more samples whose authors regard the rule as intentional (e.g. a placeholder rule waiting for sibling fields to land), the warning severity is wrong and should drop to Info, or an explicit suppression annotation should be added.
- If the new pre-pass in `ScanRules` increases proof-engine pass time by more than 10% on the median sample, the implementation is wrong (a single pre-pass over `Rules` should be O(R · F) where R = rule count, F = avg fields per rule; this is dominated by the existing pair sweep's O(R²)).
- If `UnsatisfiableRule` and `ContradictoryRule` are both observed firing on the same compilation for the same rule (the pre-pass and pair-sweep both classify it), the exclusion logic is broken and must be fixed (the pre-pass must reliably remove self-unsat rules from `ruleConstraints` before the pair sweep iterates).
- If the reachability fix causes a sample's previously-clean `FieldNeverSet` history to start emitting on a field that is genuinely governed by an effectful runtime path (e.g. a computed field whose dependency lives on a reachable transition that was misclassified), the reachability set computed in `GraphAnalyzer.cs` is wrong — but that's a defect in `ComputeReachability`, not in this design.

## Open questions

None blocking. One acknowledgment:

**Slice coupling**: Decisions A and B are mechanically independent (different files, different stages, different diagnostics). A precept-reviewer audit flagged the coupling as a NIT — a future regression in either area requires reverting both. The decisions are bundled because both surface from the same code-review remediation pass and the slice is small (under 200 lines of implementation total); landing as separate commits within the same slice (one commit per decision) preserves bisectability if a revert is ever needed. The implementer should structure commits accordingly.

The deferred consumer of `UnreachableRowFact` is explicitly out of scope and is not a blocker for this design.
