# Coverage — proof-engine-prevention

**Rollup:** blocks total 112 · covered 44 · added 4 · no-behavior 64 | cells draft 0 · added 119 · total 119

Scope: the PREVENTION GUARANTEE stated in `docs/compiler/proof-engine.md` — every fault-prone operation is PROVEN safe (accept, proof certificate) or REJECTED (prove-or-reject; unprovable = reject, never skip). Per unit guidance, §6 Architecture / §7 Component Mechanics / §8 Dependencies / §9 Failure Modes are marked NO-BEHAVIOR (implementation-internal HOW) EXCEPT subsections that state a normative prevention guarantee. No draft cells exist for this unit; every cell is net-new (`source:"added"`).

A block marked COVERED is accounted by ≥1 added cell in this unit (the behavior it states is exercised); the cell ids are listed. ADDED blocks are the primary home for their cells. Fault-class reject/accept cells are attributed to the catalog/strategy block that states the behavior most directly.

## Contents / navigation

| block | kind | disposition | cells / reason |
|---|---|---|---|
| Contents TOC (20–87) | section | NO-BEHAVIOR | navigation only |

## §1 Status

| block | kind | disposition | cells / reason |
|---|---|---|---|
| Status table (5–11) | table | NO-BEHAVIOR | implementation status + source pointers |
| Non-Negotiable Rules callout (13–18) | section | COVERED | never-guess-unprovable-rejects (the "proves / rejects / explicit-unresolved, no hidden certainty" rule) |

## §2 Overview

| block | kind | disposition | cells / reason |
|---|---|---|---|
| Overview prose + pipeline diagram (90–102) | section | COVERED | core-prove-safe-accepts, core-proof-fails-rejects (line 92: proven-safe→no runtime check; proof-fails→diagnostic + author must fix) |
| Key design choices list (104–108) | section | NO-BEHAVIOR | design rationale (bounded/no-cross-boundary/catalog-driven) |
| Quantity-normalization precision note (110) | section | ADDED (2) | qty-normalization-exact-supported-units, qty-normalization-log-units-outside-guarantee |

## §3 Responsibilities and Boundaries

| block | kind | disposition | cells / reason |
|---|---|---|---|
| In Scope table (116–126) | table | COVERED | obligation discharge + diagnostic emission restated; behaviors in all fault-class cells |
| Out of Scope table (128–136) | table | NO-BEHAVIOR | boundary/rationale (semantic resolution, topology, evaluation belong elsewhere) |

## §4 Right-Sizing

| block | kind | disposition | cells / reason |
|---|---|---|---|
| Right-sizing table + prose (140–155) | section | NO-BEHAVIOR | bounded-strategy rationale; "determinism 100%" is an engine property |

## §5 Inputs and Outputs

| block | kind | disposition | cells / reason |
|---|---|---|---|
| Input SemanticIndex csharp (161–202) | example | NO-BEHAVIOR | typed-expression data shapes |
| StateGraph / ProofForwardingFact csharp (204–241) | example | NO-BEHAVIOR | data shapes |
| Catalog metadata list (243–247) | section | NO-BEHAVIOR | read-surface list |
| Output ProofLedger csharp (250–268) | example | NO-BEHAVIOR | output data shape |
| ProofObligation + ProofStrategy enum (271–312) | example | NO-BEHAVIOR | disposition/strategy enum (accept-path strategy list); behaviors covered by fault-class cells |
| Resolved CC#6 Site-identity callout (314–331) | section | NO-BEHAVIOR | reference-equality impl |
| FaultSiteLink csharp (333–346) | example | NO-BEHAVIOR | data shape |
| Resolved CC#6 FaultSiteLink callout + elision model (348–363) | section | NO-BEHAVIOR | structural-elision impl (proved→no annotation) |
| ConstraintInfluenceEntry csharp (365–383) | example | NO-BEHAVIOR | AI-tooling output shape |
| InitialStateSatisfiabilityResult csharp (385–400) | example | NO-BEHAVIOR | output data shape |

## §6 Architecture

| block | kind | disposition | cells / reason |
|---|---|---|---|
| Two-Pass Design diagram (406–479) | example | COVERED | Pass 1.5 boxes list the satisfiability verdicts → unsatisfiable-guard-rejects, tautological-guard-rejects, unsatisfiable-rule-rejects, vacuous-rule-rejects, contradictory-rule-pair-rejects |
| Satisfiability scan (Pass 1.5) prose (481) | section | COVERED | same satisfiability-scan cells |
| Rule-vs-default fold prose (483) | section | COVERED | rule-vs-default-violating-rejects, rule-vs-default-unknown-safe, rule-vs-default-guarded-skipped-safe, initstate-construction-event-suppressed-safe |
| Rule-level scans two-phase para (485) | section | COVERED | unsatisfiable-rule-rejects, vacuous-rule-rejects, contradictory-rule-pair-rejects |
| Satisfiability-scans-read-bare-interval soundness invariant (487) | section | NO-BEHAVIOR | implementation soundness (no-false-rejection isolation) |
| field-op-field relational not in pre-pass (489) | section | COVERED | div-zero-relational-contradiction-rejects (relational contradiction caught on discharge path) |
| Cross-row interval composition (493) | section | COVERED | count-grow-within-band-safe, overflow-set-action-in-range-safe (sibling-reject / guard narrowing) |
| narrowing sound only single-leaf (495) | section | NO-BEHAVIOR | impl soundness (forfeit on multi-leaf) |
| NegateConstraintToInterval type-dispatch (497) | section | NO-BEHAVIOR | impl interval arithmetic |
| DimensionalProduct (Strategy 6) para (499) | section | COVERED | dim-product-curated-safe, dim-product-cancelling-safe, dim-product-outside-set-rejects |
| Obligation Generation Contract (501–513) | section | ADDED (2) | obligation-completeness-declared-constraint-obligated, obligation-completeness-maxcount-obligated |
| "Why this is a contract" (513) | section | COVERED | same obligation-completeness cells (prevention-vs-detection restated) |
| Catalog-Driven Obligation Instantiation + table (515–527) | table | ADDED | fault-class catalog rows: division→div-zero-*, sqrt→sqrt-neg-*, pow→pow-exp-*, .first/.last/.peek→coll-*, dequeue/pop→coll-mut-* |
| ProofRequirement Catalog DU csharp (529–600) | example | NO-BEHAVIOR | requirement DU data shapes |
| Normalization boundary note (602) | section | NO-BEHAVIOR | UCUM pre-normalization impl |
| Computed-field bound containment note (604) | section | COVERED | overflow-computed-bounded-operands-safe, overflow-computed-unbounded-operands-rejects |
| Flag-modifier lower bounds note (606) | section | NO-BEHAVIOR | sound operand-interval tightening impl (never rejects) |
| ProofSubject csharp (608–620) | example | NO-BEHAVIOR | subject DU data shape |

## §7 Component Mechanics

| block | kind | disposition | cells / reason |
|---|---|---|---|
| Proof Strategies intro (626–628) | section | NO-BEHAVIOR | strategy dispatch mechanics |
| Subject Resolution Utilities (630–708) | section | NO-BEHAVIOR | impl helpers (ResolveSubject/GetFieldName) |
| Strategy 1: Literal Proof + examples + pseudocode (710–765) | section | COVERED | div-zero-literal-nonzero-safe, div-zero-literal-zero-rejects, sqrt-neg-literal-nonneg-safe, sqrt-neg-literal-negative-rejects, coll-first-literal-nonempty-safe, coll-first-empty-literal-rejects |
| Strategy 2: Declaration Attribute + modifier→satisfaction table + examples + pseudocode (767–969) | section | COVERED | div-zero-modifier-positive-safe, div-zero-modifier-nonzero-safe, sqrt-neg-modifier-nonnegative-safe, sqrt-neg-abs-return-nonneg-safe, mod-req-satisfied-safe |
| ProofSatisfaction DU csharp (830–965) | example | NO-BEHAVIOR | carrier-fact data shapes |
| Resolved CC#5 callout (967–969) | section | NO-BEHAVIOR | rename/redesign note |
| Carrier: DeclaredPresenceMeta (975–985) | section | COVERED | presence-required-field-safe, presence-notempty-optional-still-rejects (notempty does NOT satisfy presence) |
| Carrier: DeclaredQualifierMeta (987–1010) | section | NO-BEHAVIOR | qualifier carrier data + normalization rules |
| Carrier: ValueModifierMeta.ProofSatisfactions table (1012–1031) | table | NO-BEHAVIOR | modifier satisfaction data (duplicate of §6 table) |
| Strategy 2 → Carrier Dispatch summary tables (1033–1052) | table | NO-BEHAVIOR | requirement-kind → carrier dispatch impl |
| Strategy 3: Guard-in-Path + examples + guard-pattern table + subsumption (1054–1090) | section | COVERED | div-zero-guard-nonzero-safe, div-zero-guard-positive-safe, sqrt-neg-guard-nonneg-safe, coll-first-guard-count-fn-safe, coll-first-guard-count-member-safe, presence-guard-is-set-safe |
| ExtractGuardConstraints spec + decomposition/inversion tables (1091–1245) | section | NO-BEHAVIOR | guard-parsing impl tables (AND/OR decomposition, operator inversion) |
| Sequential proof flow (reassignment / forward-propagation) (1247–1256) | section | COVERED | div-zero-reassignment-invalidates-guard, presence-reassign-clear-invalidates, coll-access-after-grow-safe, coll-dequeue-after-grow-safe, coll-dequeue-after-shrink-invalidates |
| Count-band tracking para (1257) | section | COVERED | count-grow-overflow-rejects, count-shrink-underflow-rejects |
| Strategy 4: Flow Narrowing + example + pseudocode (1259–1345) | section | COVERED | div-zero-subtraction-divisor-relational-safe, div-zero-barefield-divisor-relational-rejects |
| GuardRelationImpliesObligation triple table (1347–1414) | table | COVERED | div-zero-subtraction-divisor-relational-safe (row A>B, Z/(A-B)), div-zero-barefield-divisor-relational-rejects (row A>B, B/A) |
| Strategy 3 vs 4 boundary callout (1416–1419) | section | NO-BEHAVIOR | dispatch-boundary rationale |
| Strategy 4: rule-sourced relations / subject resolution / contradiction guard (1421–1429) | section | COVERED | div-zero-relational-contradiction-rejects |
| Strategy 5: Qualifier Compatibility + examples + pseudocode (1431–1466) | section | COVERED | qual-currency-match-safe, qual-currency-mismatch-rejects, qual-unit-match-safe, qual-unit-mismatch-rejects, qual-unqualified-unprovable-rejects |
| Strategy 6: Dimensional Product + pseudocode (1468–1482) | section | COVERED | dim-product-curated-safe, dim-product-cancelling-safe, dim-product-outside-set-rejects |
| Strategy 7: Compositional Constraint + examples + sign inference + pseudocode (1484–1697) | section | NO-BEHAVIOR | compositional sign-inference accept-path mechanics; discharge failures fall through to the fault-class rejects already enumerated |
| Strategy 8: Interval Containment (1699–1707) | section | COVERED | overflow-set-action-in-range-safe, overflow-set-action-out-of-range-rejects, overflow-collection-element-write-*, oor-default-* |
| Strategy 9: Length Containment (1709–1725) | section | COVERED | length-* cells (carried-bound, literal-in/violating, unbounded-source, concat-*, collection-element-write, string-default) |
| Strategy 10: Count Containment + delta table (1727–1747) | section | COVERED | count-* cells (grow-within-band, grow-overflow, unguarded-grow-unbounded, shrink-underflow, dedup-add, default-literal-over) |
| Qualifier Resolution 7.1 dispatch (1753–1778) | section | NO-BEHAVIOR | ResolveQualifierFromExpression impl |
| Qualifier Resolution 7.2 axis-fallback + Known-gap cross-unit (1780–1792) | section | ADDED (1) | qual-cross-unit-cancellation-known-gap (line 1790 unsound cross-unit cancellation) |
| Qualifier Resolution 7.3 currency translation (1794–1803) | section | NO-BEHAVIOR | TranslateCurrencyAxis impl |
| Qualifier Resolution 7.4 subsumption/satisfaction tables (1805–1841) | table | NO-BEHAVIOR | NumericConstraintSubsumes / SatisfactionCovers impl tables |
| Qualifier Resolution 7.5 two subsystems (1843–1862) | section | NO-BEHAVIOR | architectural rationale |
| Qualifier Resolution 7.6 ResultQualifiers propagation (1864–1878) | section | NO-BEHAVIOR | function-call qualifier propagation impl |
| Qualifier Resolution 7.7 constant-folder zero-denominator guard (1880–1892) | section | COVERED | mod-zero-literal-zero-rejects, mod-zero-literal-nonzero-safe (folder refuses to evaluate through /0 and %0) |
| Proof/Fault Chain diagram + invariants (1894–1940) | example | COVERED | invariant 3 (every unresolved obligation → diagnostic + FaultSiteLink) exercised by every reject cell |
| Constraint Influence Analysis (1942–1981) | section | NO-BEHAVIOR | AI-tooling causal-reasoning output; not a prevention guarantee |
| Initial-State Satisfiability algorithm (1983–2115) | section | COVERED | initstate-satisfiable-safe, initstate-unsatisfiable-rejects, initstate-unfoldable-conservative-safe, initstate-guarded-ensure-skipped-safe |
| Collection Non-Empty Proof + sources table (2117–2135) | section | COVERED | coll-last-unguarded-rejects, coll-peek-unguarded-rejects, coll-dequeue-unguarded-rejects, coll-pop-unguarded-rejects, coll-access-mincount-modifier-safe, coll-dequeue-guard-count-safe |
| ProofForwardingFact Consumption Contract + table (2137–2147) | table | COVERED | unreachable-state-suppresses-obligation (unreachable-state obligations vacuously satisfied) |
| Stateless Precept Handling + tables (2149–2176) | section | COVERED | stateless-precept-proof-runs-rejects |

## §8 Dependencies and Integration Points

| block | kind | disposition | cells / reason |
|---|---|---|---|
| Upstream Dependencies table (2184–2196) | table | NO-BEHAVIOR | catalog-source list (evidence for fault classes) |
| Downstream Consumers table (2198–2204) | table | NO-BEHAVIOR | consumer list |
| Builder Proof-Consumption Contract PE-G11 (2206–2248) | section | COVERED | initstate-unsatisfiable-rejects (InitialStateResults gate blocks runtime model when unsatisfiable) |

## §9 Failure Modes and Recovery

| block | kind | disposition | cells / reason |
|---|---|---|---|
| Error Accumulation table (2254–2264) | table | COVERED | unresolved-obligation→emit-diagnostic path exercised by all reject cells |
| Diagnostic Message Formatting PE-G12 table (2266–2285) | table | NO-BEHAVIOR | message templates (evidence for diagnostic codes) |
| Partial Results Prove() pseudocode (2287–2352) | example | NO-BEHAVIOR | pipeline-completeness impl |
| Upstream Error Handling PE-G13 (2354–2379) | section | COVERED | error-tainted-obligation-suppressed |

## §10 Contracts and Guarantees

| block | kind | disposition | cells / reason |
|---|---|---|---|
| Obligation Completeness (2385–2389) | section | COVERED | obligation-completeness-declared-constraint-obligated, obligation-completeness-maxcount-obligated |
| Disposition Exhaustiveness (2391–2395) | section | COVERED | every obligation Proved-or-Unresolved exercised by every accept/reject cell |
| Fault Chain Integrity (2397–2404) | section | COVERED | reject cells (each unresolved obligation ⇒ 1:1 FaultSiteLink to a [StaticallyPreventable] fault) |
| Determinism (2406–2410) | section | NO-BEHAVIOR | engine property; not a per-definition accept/reject behavior |
| Catalog Correspondence (2412–2416) | section | NO-BEHAVIOR | architecture (engine reads, never constructs, requirements) |

## §11 Design Rationale and Decisions

| block | kind | disposition | cells / reason |
|---|---|---|---|
| Decision 1: Admissibility by Certificate (2422–2437) | section | NO-BEHAVIOR | rationale (bounded strategy / certificate) |
| Decision 2: ProofLedger not cross boundary (2439–2456) | section | NO-BEHAVIOR | rationale |
| Decision 3: Obligations stamped by TC (2457–2466) | section | NO-BEHAVIOR | rationale |
| Decision 4: Constraint Influence output (2468–2478) | section | NO-BEHAVIOR | rationale |
| Decision 5: Modifier-proof via catalog + table (2480–2525) | table | NO-BEHAVIOR | rationale (modifier proofsatisfaction table = evidence) |
| Decision 6: Relational facts reuse field-only record + 3 invariants (2527–2539) | section | COVERED | div-zero-relational-contradiction-rejects (invariant 3 contradiction guard) |

## §12 Innovation

| block | kind | disposition | cells / reason |
|---|---|---|---|
| Catalog-Declared Proof Obligations (2545–2549) | section | NO-BEHAVIOR | contrast/rationale |
| Roslyn-Enforced FaultCode↔DiagnosticCode (2551–2576) | section | NO-BEHAVIOR | build-time invariant impl |
| Bounded, Non-Extensible Strategy Set (2578–2585) | section | NO-BEHAVIOR | rationale |
| Compile-Time Satisfiability (2587–2591) | section | COVERED | initstate-unsatisfiable-rejects (X default 0 / ensure X>5 → compile error) |
| Constraint Influence Analysis (2593–2597) | section | NO-BEHAVIOR | AI-tooling output |

## §13 Open Questions / Implementation Notes

| block | kind | disposition | cells / reason |
|---|---|---|---|
| Implementation Status (2603–2605) | section | NO-BEHAVIOR | status note |
| Prove-or-reject MVP — designed not built (2607–2613) | section | COVERED | prove-or-reject-no-runtime-deferral (three-way verdict; unprovable computed bound rejects, no runtime defer) |
| Deferred multi-fact §1b (2615–2619) | section | COVERED | no-multi-hop-fact-chaining-rejects (single-hop only; combined-bound stays unproven) |
| Validation Required (2621–2625) | section | NO-BEHAVIOR | validation TODO |
| Blocking Dependencies (2627–2633) | section | NO-BEHAVIOR | dependency notes |
| Catalog Metadata Needed (2635–2637) | section | NO-BEHAVIOR | resolved catalog note |
| Future Considerations (2639–2645) | section | NO-BEHAVIOR | future ideas |

## §14 Deliberate Exclusions

| block | kind | disposition | cells / reason |
|---|---|---|---|
| No Opaque Solver (2651–2659) | section | COVERED | reject-names-remediating-fix (undischargeable ⇒ reject AND name what makes it safe) |
| No Runtime Obligation Checking (2661–2668) | section | NO-BEHAVIOR | boundary (ledger doesn't cross to runtime) |
| No Constraint Evaluation (2670–2674) | section | NO-BEHAVIOR | boundary (proof = satisfiability, not enforcement) |
| No General Dataflow Analysis (2676–2685) | section | COVERED | no-cross-transition-dataflow-rejects (cross-transition value not tracked ⇒ dependent op rejects) |
| No Inductive Proof (2687–2689) | section | NO-BEHAVIOR | N/A — no loops/recursion in the DSL |
| No User-Defined Proof Hints (2691–2693) | section | NO-BEHAVIOR | boundary (no pragma/assert) |

## §15 Cross-References / §16 Source Files

| block | kind | disposition | cells / reason |
|---|---|---|---|
| Cross-References table (2697–2711) | table | NO-BEHAVIOR | doc pointers |
| Source Files table (2715–2728) | table | NO-BEHAVIOR | source-file map |

## Notes / dropped draft cells

- No draft cells existed for this unit; nothing was dropped. All 119 cells are `source:"added"`.
- Expectation-vocabulary caveats carried into Phase 2:
  - The Pass 1.5 whole-construct verdicts (UnsatisfiableGuard, TautologicalGuard, UnsatisfiableRule, VacuousRule, ContradictoryRule) are structural-soundness diagnostics the doc classifies as `Warning`-severity "reports" (§0.6 items 7/8), not hard `Error` rejections. They are recorded as `reject:<Name>` meaning "emits that diagnostic"; Phase 2 should treat severity as secondary to emission.
  - `qty-normalization-log-units-outside-guarantee` uses `reject:any` as the doc only states dB/[pH] are "outside the proof guarantee" (line 110) without naming a code or a definite accept/reject; the true expectation is "not provably exact." Flagged for Phase 2 disambiguation.
  - Codes taken from the §9 Diagnostic Message Formatting table and the strategy prose: DivisionByZero(83), SqrtOfNegative(84), UnsatisfiableGuard(82), UnprovedModifierRequirement(112), UnprovedDimensionRequirement(113), UnprovedQualifierCompatibility(114), UnsatisfiableInitialState(115), UnprovedPresenceRequirement(116), NumericOverflow(PRE0078), OutOfRange(PRE0079), LengthBoundViolation(PRE0135), CountBoundViolation(PRE0136), IncompatibleDimensionalProduct(PRE0157), UnsatisfiableRule(PRE0159), VacuousRule(PRE0154), ContradictoryRule(PRE0155), TautologicalGuard(PRE0153), DefaultViolatesRule(PRE0164), UnguardedCollectionAccess / UnguardedCollectionMutation (fault-shaped, no explicit numeric code in doc). KeyPresence and IndexBounds carry no explicit diagnostic code in this doc (reported under GuardInPath; code "by absence flag"), so those cells use `reject:any`.
  - `pow` negative-exponent rejection carries no explicit code in the doc → `reject:any`.
