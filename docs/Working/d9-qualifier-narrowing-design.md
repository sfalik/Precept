---
status: Locked 2026-05-29 — re-grounded against current src/ (A1/A2 substrate shipped in BUG-014/015/016) + verbatim-evidence audits; load-bearing gaps (N1/N2/G1/G2/G5/CONCERN-1) resolved. Owner-locked after reviewing the N2 (subset-vs-equality) correction. No open questions; no exploratory/irreversible decisions. Slicing → /lifecycle-3-plan.
promoted-to: Implemented + promoted 2026-05-29 (S1 `4e5f837c`, S2a `fc10db45`, S2b `f662aad3`, S3+(0) `5d071ffa`, S4 `5949de4b`/`04bf5f0e`/`75b83980`). Mechanism promoted to `docs/language/business-domain-types.md § D9` (the `$eq:`/StaticValueKind fiction rewritten to the real proof-engine fact). Completion review → `lifecycle-review-d9-qualifier-narrowing-2026-05-29.md`. The `.basis` *cancellation* half of acceptance criterion #4 (`price × period`, composite single-basis) is deferred to W-C, not D9.
phase-target: Phase 6 (F-LANG-BIZ-07 continuation) / standalone proof-engine work item
comparable-systems-research-status: partial — flow-sensitive narrowing (occurrence/flow typing) cited inline in Language Design Grounding as the broader-field anchor; the load-bearing soundness decisions are grounded in Precept's own numeric discrete-equality narrowing precedent (F-LANG-BIZ-08), cited per-decision
sources-consulted:
  - docs/Working/d9-qualifier-narrowing-analysis-2026-05-28.md — the deep analysis (3 lenses) this design implements; exhaustive requirements matrix, soundness rules, architecture findings
  - docs/language/business-domain-types.md § D9 (lines 1745-1762) + § Mechanism (1207-1216) + accessor table (1218-1233) — the spec promise; § Mechanism describes unbuilt infra (drift)
  - docs/language/business-domain-types.md § dimension (759-792) — partitioned-registry model ("partitioned by parent type"); :787-790 partition table; :792 cross-partition compile error
  - docs/language/business-domain-types.md:1817 (D14 composite acceptance — subset ⊆, the N2 correction); :961 (open-period .basis narrowing remedy); :1226-1227 (.basis/.dimension accessor rows)
  - docs/compiler/literal-system.md:197-205 (context-born-then-content-validated), :223 (dimension ClosedSetValidation row), :228,232,603 (stateref context-dependent-validation precedent)
  - src/Precept/Language/ClosedSetValidator.cs:3-13 (context-free Validate) + TypedConstantValidation.cs:5-20 (dispatcher passes targetType+context; ClosedSet arm drops them)
  - src/Precept/Pipeline/ProofLedger.cs — ProofObligation.ReassignedBefore/CountEstablishedBefore/CountInvalidatedBefore (shipped BUG-014/015/016 substrate the discharge consults)
  - src/Precept/Pipeline/ProofEngine.cs:29 (GuardConstraint, decimal-only) + :624-678 (TryDischarge dispatch) + WalkActions (forward stamping)
  - src/Precept/Pipeline/ProofEngine.Strategies.cs:618-680 (TryGuardInPathProof), :716-748 (ExtractGuardBranchesCore AND/OR), :779-862 (ExtractGuardLeafConstraints), :864-898 (GuardSubsumes/NumericConstraintSubsumes), :227-241 (ResolvePeriodDimension)
  - src/Precept/Pipeline/ProofEngine.Qualifiers.cs:10-35 (TryQualifierCompatibilityProof), :256-435 (ResolveQualifierOnAxis — declaration-only), :441-529 (ResolveQualifierFromExpression)
  - src/Precept/Pipeline/TypeChecker.Expressions.AssignmentQualifiers.cs:120-174 (ResolveAssignmentQualifierAxis), :313 (sole non-hover ReturnsQualifier consumer)
  - src/Precept/Language/Types.cs:480-481,552,590-591,607,637-639,657-658 (accessors + ReturnsQualifier) + :125-130 (DimensionValidation)
  - src/Precept/Language/Ucum/DimensionCatalog.cs:11-21 (UCUM partition; contains 'time')
  - src/Precept/Language/DeclaredQualifierMeta.cs:75-80 (TemporalUnit + DerivedDimension)
  - test/Precept.Tests/ProofEngine/DiscreteEqualityNarrowingTests.cs — numeric narrowing precedent (the structural template)
---

# Open-field discrete-equality qualifier narrowing (spec § D9)

> Build the guard-driven qualifier narrowing the spec § D9 promises but never delivered: `when X.<accessor> == '<value>'` seeds a branch-scoped fact that lets a downstream operation discharge an open field's qualifier obligation — soundly, across currency/unit/dimension/from/to/basis axes — plus the partition-aware dimension-literal validation that makes `period.dimension == 'date'` compile in the first place.

**Read `docs/Working/d9-qualifier-narrowing-analysis-2026-05-28.md` first** — this design implements its conclusions; the exhaustive (type,accessor,axis) matrix, the six discharge sites, the soundness failure-mode catalog, and the architecture findings live there and are not duplicated here (pointer philosophy).

## Goal
When done, the three spec § D9 acceptance examples compile and narrow — `when X.currency == 'USD' -> set UsdField = UsdField + X` (open money), `when X.dimension == 'mass' -> set KgField = X` (open quantity), `when X.dimension == 'date' -> set D = D + X` (open period) — AND a value-mismatched assignment under the same guard (`when X.currency == 'USD' -> set EurField = ... X`) is still rejected; demonstrated by `precept_compile` accepting the matched cases and rejecting the mismatched/cross-partition/OR-collapse cases.

## Scope
- **In scope**: (1) partition-aware validation of `dimension` typed-constants so `period.dimension == 'date'|'time'|'datetime'` compiles and cross-partition is rejected (the folded-in PRE0053 fix); (2) guard→qualifier narrowing-fact production for `X.<accessor> == 'literal'` across Currency/Unit/Dimension/TemporalDimension/From/ToCurrency; (3) a discharge path consulting those facts at the qualifier-compatibility, assignment-validation, dimension-requirement, and dimensional-product sites; (4) `.basis` discrete-string narrowing; (5) period `.dimension` derivation (TemporalUnit→DerivedDimension) on the discharge path; (6) spec-drift correction in § D9.
- **Out of scope**: numeric narrowing (shipped, F-LANG-BIZ-08); the `of`-constraint paths (work today); interpolation-slot narrowing from open fields (`'{X.currency}'`) — deferred unless trivially covered; runtime evaluation (stub).
- **Deferred to future**: field-to-field equality narrowing (`X.currency == Y.currency`) — non-actionable for positive proof; `!=`/exclusion-set narrowing — non-actionable.

## Philosophy Alignment

| Principle | Affected? | How served (1 sentence + cite) | Tension | Tradeoff |
|---|---|---|---|---|
| 1. Prevention not detection | Y | A value-mismatched assignment under a guard (`X.currency=='USD'` → assign to EUR field) stays a structural compile error; narrowing only proves what the guard genuinely establishes (`philosophy.md` Prevention; analysis § Soundness rule 4). | Soundness-critical — see tradeoff | A false discharge would *violate* Principle 1; the design's value-exact-discharge rule (D3) is the guard against that. |
| 2. One file, complete rules | N | In-definition; no cross-file surface. | N/A | N/A |
| 3. Determinism | Y | Partition selection and narrowing are deterministic functions of static types + the branch's guard (`spec § 0.1 #3`). | N/A | N/A |
| 4. Full inspectability | Y | Narrowed facts surface as discharged obligations (proof attribution); a guard that narrows nothing leaves the obligation visibly unresolved. | N/A | N/A |
| 5. Keyword-anchored readability | N | No syntax change — `when X.accessor == 'v'` is existing guard form. | N/A | N/A |
| 6. Governance not validation | Y | `when X.currency == 'USD'` becomes a usable governance predicate that gates the operation, not a no-op. | N/A | N/A |
| 7. Compile-time-first static checking | Y | Open-field arithmetic gains compile-time proof via the guard, or is rejected (`spec § 0.1 #7`). | N/A | N/A |
| 8. Honesty about approximation | N | No approximation surface. | N/A | N/A |
| 9. Mandatory rationale (`because`) | N | Not a rule/ensure construct. | N/A | N/A |
| 10. Static semantic checking | Y | New statically-checked discharges + cross-partition + OR-collapse rejections (`spec § 0.1 #10`). | N/A | N/A |
| 11. Static completeness | Y | A well-typed guarded operation gains defined compile-time meaning; no new runtime fault path (runtime deferred); the discharge is value-exact so no unsound program is admitted. | Soundness-critical | The whole-design tradeoff: the narrowing must be provably sound (D3) or it breaks the core guarantee — accepted by gating every discharge on value-equality + all-branches + intersection-with-declared. |

**Companion commitments.** *Stateless-first-class*: guard narrowing works in rules/ensures on data-only precepts, not just transitions. *Domain-expert-primary-author*: the author writes `when Payment.currency == 'USD'` in domain terms; the proof plumbing is invisible.

## Language Design Grounding

**General language design.** This is **flow-sensitive narrowing** (a.k.a. occurrence typing / flow typing): a value's statically-known properties are refined within the scope where a predicate provably holds. TypeScript's control-flow analysis narrows a union by `typeof`/`===` guards within the guarded block; Kotlin's smart-casts refine a nullable/sealed type after an `is`/`!= null` check; Ceylon and Typed Racket (occurrence typing, Tobin-Hochstadt & Felleisen, ICFP 2010) formalize the discipline that a predicate in test position refines the type in the then-branch only. Precept's case is the discrete-equality specialization: the "property" is a qualifier-axis value (currency/dimension/…), the "predicate" is `accessor == literal`, and the "scope" is the guarded transition row / rule / ensure branch. Precept diverges from TS/Kotlin in one load-bearing way: **soundness is non-negotiable and checked, not best-effort** — TS narrowing is deliberately unsound at the edges (type assertions, mutation gaps); Precept's narrowing must never admit an invalid configuration (Principle 1), so OR-branches take the union (never one arm), negative guards narrow nothing, and discharge is value-exact. The internal precedent is Precept's own numeric discrete-equality narrowing (F-LANG-BIZ-08, `Strategies.cs:870-898`) — same shape (`==`-only, branch-scoped, value-exact), different fact algebra (decimal interval vs discrete string identity). No `research/language/` study covers flow-typing for qualifier axes (gap noted); the mechanism is a direct application of the established occurrence-typing discipline.

**Precept-specific application.** Implements spec § D9 (the promised-but-unbuilt narrowing) and § dimension (the partitioned registry). Touches Principles 1/7/10/11 (the soundness-critical ones). Conflicts with no locked decision; corrects § D9 Mechanism drift (describes unbuilt `$eq:`/StaticValueKind/ApplyNarrowing infra).

## Audience and Teachability

**Worked example** (open-payment governance — the spec's own § D9 case, made real):
```precept
precept PaymentApplication
field Payment as money
field AccountBalance as money in 'USD'
state Pending initial
state Applied
event ApplyPayment
from Pending on ApplyPayment when Payment.currency == 'USD'
    -> set AccountBalance = AccountBalance + Payment
    -> transition Applied
from Pending on ApplyPayment
    -> reject "Only USD payments can be applied to a USD account"
```
The author guards the open `Payment` to USD, then adds it to a USD balance — today this fails to compile (`PRE0141`); after this design it narrows and proves.

**Error message** (the misuse: guard value ≠ assignment requirement — the soundness headline):
```
PRE0141: cannot prove Payment's currency satisfies 'AccountBalance' (USD) — the guard narrows Payment to 'EUR', which does not match. Narrow to the required currency, or convert via an exchangerate.
```
Names the *narrowed* value and the *required* value so the author sees exactly why the guard didn't help — serving the domain expert who guarded the wrong value, not a compiler-internals reader.

**10-minute teaching path:**
1. `business-domain-types.md § D9` — the open-field-narrowing pattern + table (3 min).
2. The worked guard example above / `samples` once added (2 min).
3. `business-domain-types.md § dimension` partition table — for the `.dimension` case (2 min).

## Semantic Rules

**Narrowing-fact production.** A guard leaf of shape `X.acc == 'v'` where `X.acc` has `ReturnsQualifier = α` (or `acc = basis`) and `'v'` resolves to a concrete axis value `v` produces a branch fact:
```
  X.acc == 'v'   acc ⟶ axis α   'v' ⇓ v   (literal, statically known)
  ─────────────────────────────────────────────────────────────────  [seed]
        branch ⊢ narrowed(X, α, v)
```
OR/AND compose per the existing branch algebra (`ExtractGuardBranchesCore`, `Strategies.cs:716-748`): `A or B` ⟶ branch-set union (each disjunct its own branch); `A and B` ⟶ one branch carrying both facts.

**Discharge (axis-appropriate relation).** An open-field qualifier obligation `requires(X, α, w)` discharges iff **every** branch reachable at the obligation's site carries `narrowed(X, α, v)` with `v` satisfying `w`:
```
  ∀ branch ∈ branches(obligation.Context):  narrowed(X, α, v) ∈ branch   ∧   satisfies_α(v, w)
  ──────────────────────────────────────────────────────────────────────────────────────────  [discharge]
                              requires(X, α, w)  proved
```
`satisfies_α(v, w)` is **axis-dependent** (this is the N2 correction — see D3):
- **Identity axes** (Currency, Unit, Dimension, TemporalDimension, From, ToCurrency): value-**equality** (mirrors `QualifiersAreCompatible`, `Qualifiers.cs:118-173`) — never "X is narrowed." `PeriodDimension.Any` never satisfies a concrete `w`.
- **Period basis** (the `.basis` axis): D14 **subset** (`business-domain-types.md:1817` — "a value is accepted iff its non-zero component set is a subset of the declared basis"). The narrowed basis `v` (component set from the guard literal) satisfies a required basis `w` iff `components(v) ⊆ components(w)`. Equality is the wrong relation here: a value narrowed to `'hours'` (`{hours}`) satisfies an obligation requiring `'hours + minutes'` (`{hours, minutes}`) because `{hours} ⊆ {hours, minutes}`. The reverse does not hold.

**Period `.dimension` derivation.** For an open period, `narrowed(X, TemporalDimension, d)` from `X.dimension == 'd'` carries the runtime value's derived dimension; discharge of a `DimensionProofRequirement(req)` consults this fact via the shared `ResolvePeriodDimension` helper (which already derives `TemporalUnit.DerivedDimension`, `Strategies.cs:227-241`) — extended to read the guard fact, not only declared qualifiers.

**Soundness preservation.** Principle 1/11: the discharge admits a program iff the guard *provably* establishes the required value on every reachable branch — strictly narrower than "accepted." The 11 failure modes that would break soundness (value-mismatch discharge, OR-arm collapse, wrong-axis, negative-equality false-positive, field-to-field false value, cross-branch leak, **cross-step reassignment leak**, declared-qualifier override, negated-conjunction over-credit, composite-datetime over-specification, Any-discharge) are each closed by a stated rule (D3) and paired with a required fail-before/pass-after test in the **`analysis § Failure-mode → rule → required-test catalog`** (materialized as an 11-row table). Principle 7: cross-partition and unprovable cases are rejected at compile time.

**Reassignment invalidation (cross-step) — now rides a shipped substrate.** The guard is row-scoped (`obligation.Context` carries the whole `TransitionRowContext.Row`, so `t.Row.Guard` is pulled for every body obligation). A narrowing fact `narrowed(X, α, v)` must be invalidated by any `set X = …` earlier in the same body. This is **no longer an open question**: sequential-proof-flow invalidation (spec § 0.6 item 7) is now built — `WalkActions` stamps each obligation with `ReassignedBefore` (fields fully replaced by `set`/`clear`, classified via `ActionMeta.Effect`/`ActionEffectClass`), and every guard-narrowing strategy already drops facts about reassigned subjects. The qualifier discharge strategy consults `obligation.ReassignedBefore` exactly as `TryGuardInPathProof` does (`if (obligation.ReassignedBefore.Contains(X)) ` → do not discharge). No new mechanism is required — the substrate was completed in the BUG-014/015/016 work that landed before this design; the qualifier path is one more consumer of it.

## Architecture Grounding

### Precept-internal placement
**Layer placement.** (a) Narrowing-**fact production** is a proof-engine guard-decomposition concern (mirrors numeric `ExtractGuardLeafConstraints`). (b) **Discharge** is a new proof-engine strategy — kept *out* of `ResolveQualifierOnAxis` (which stays declaration-pure) to avoid threading guard context through its many callers. (c) **Partition membership** is catalog metadata (UCUM `DimensionCatalog` + a temporal-partition set); **partition selection** is type-checker context (needs the LHS accessor's parent type). The new fact type is a DU/parallel record, not a nullable extension of the decimal-only `GuardConstraint` (CLAUDE.md DU-over-nullable-fields rule).

**Cross-component propagation:**
- Runtime (parser, type checker, evaluator, diagnostics): type checker — partition-aware dimension-literal validation, `ReturnsQualifier` on `.dimension` accessors, assignment-site discharge consultation; proof engine — new fact record, seed arm, discharge strategy, period-dimension derivation on the discharge path; diagnostics — PRE0141/0114/0113 messages gain narrowed-vs-required wording; partition-mismatch message. Parser: None. Evaluator: None (runtime deferred).
- Tooling (completions, hover, semantic tokens): completions after `X.dimension == '` offer the partition-correct set; after `X.currency == '` offer ISO 4217. **Hover behavior changes** (not "none"): adding `ReturnsQualifier` to the `.dimension` accessors flips `RichHoverFactory.cs:995`'s `when returnsAxis != None` arm on for `.dimension` — hovering `X.dimension` now resolves the Dimension/TemporalDimension axis on the owner instead of falling to the generic arm. Reviewed as intended/benign (showing the dimension axis is correct), but it IS a change to an existing consumer, enumerated here.
- **Interpolation-slot behavior changes** (not "none"): `SlotAccessorCanResolveAxis` (`AssignmentQualifiers.cs:694`) keys on `ReturnsQualifier`; today `.dimension`=None so `'{X.dimension}'` slots never resolve via the accessor. After the change a Dimension-targeting slot resolves. Low-risk/inert (interpolating a dimension into a qualifier slot is rare), but enumerated rather than claimed "none." The design must add a test asserting this does not mis-resolve a non-dimension slot.
- MCP (vocabulary, DTOs, tool output): None.

**Breaking changes.** None to public contracts. Previously-failing programs (PRE0141/0113/0053 on valid guarded operations) now compile (relaxation). `quantity.dimension == 'date'` stays rejected. No catalog renames. **Not purely additive**: adding `ReturnsQualifier` to the `.dimension` accessors changes two *existing* `ReturnsQualifier` consumers (hover + interpolation-slot resolution — enumerated in Cross-component propagation above); both reviewed as benign/intended, but the change must be tested, not assumed inert.

### External architectural precedent
TypeScript's control-flow analysis is the closest architectural analogue: a separate analysis pass computes per-block narrowed types consulted by later type queries — Precept mirrors this with branch-scoped facts sourced from `obligation.Context` and consulted by a dedicated discharge strategy, rather than mutating the field's declared type. Precept diverges by making the discharge *sound-by-construction* (all-branches, value-exact) where TS accepts unsoundness for ergonomics (`compiler-and-runtime-design.md § 2` grounds the catalog-first/soundness-first stance). The numeric narrowing path (F-LANG-BIZ-08) is the in-tree structural template.

## Inventory of what will be built
- `QualifierNarrowingConstraint(string Field, QualifierAxis Axis, string Value)` record + a parallel branch extractor mirroring `ExtractGuardBranchesCore` AND/OR.
- Seed arm in `ExtractGuardLeafConstraints` (`Strategies.cs:779`) for `TypedMemberAccess{ReturnsQualifier≠None} == TypedTypedConstant` (Equals-only) + the `.basis` string-accessor case.
- `TryQualifierGuardNarrowingProof` discharge strategy inserted in `TryDischarge` (`ProofEngine.cs:624`), pulling the guard from `obligation.Context`.
- `ReturnsQualifier` added: quantity `.dimension`→Dimension (Types.cs:591), period `.dimension`→TemporalDimension (:481), price `.dimension`→Dimension (:639), unitofmeasure `.dimension`→Dimension (:607, per D7).
- Partition-aware dimension typed-constant validation (the PRE0053 fix) — thread LHS parent-type into `dimension` content validation; temporal partition set {date,time,datetime}.
- Period `.dimension` derivation reachable from the discharge (share `ResolvePeriodDimension`).
- Assignment-site (`AssignmentQualifiers.cs`) consults the discharge for open-field sources.
- Diagnostics: narrowed-vs-required wording on PRE0141/0114/0113; partition-mismatch message/code.
- Tests: the full failure-mode→test catalog (analysis § Soundness) + the 3 acceptance examples + the `.basis`/period-dimension cases + cross-partition + OR-collapse + value-mismatch.

## Decisions

### Decision 1: A dedicated `QualifierNarrowingConstraint` fact + parallel branch extraction (not extending `GuardConstraint`)
**Stakes**: high
- **Rationale**: `GuardConstraint.Value` is `decimal?` (`ProofEngine.cs:29`); qualifier identities ('USD'/'date') are discrete strings with no numeric subsumption algebra. A separate record keeps the two fact algebras clean and leaves the live numeric path untouched (lowest regression surface). A parallel extractor mirroring the AND/OR walk inherits OR-union / AND-cross-product soundness for free.
- **Tradeoff accepted**: duplicates the (short) AND/OR walk rather than unifying numeric+qualifier under a DU base. Accepted: unification (Option B) touches the live numeric branch types — higher regression risk on a soundness-critical path — for modest dedup.
- **Alternatives considered**: (a) extend `GuardConstraint` with nullable qualifier fields — rejected: the nullable-field-on-flat-record smell CLAUDE.md forbids; forces every numeric consumer to null-check. (b) shared `GuardFact` DU base with numeric+qualifier subtypes — rejected for now: touches live numeric branch plumbing (regression risk) for modest gain; revisit if a third fact kind appears.
- **Precedent**: the engine already runs *two* parallel branch extractors — `GuardConstraint` (numeric, `Strategies.cs:870-898`) and `FieldToFieldConstraint` via `TryFlowNarrowingProof`/`ExtractFieldToFieldBranchesCore` (`Strategies.cs:948-1010`). `QualifierNarrowingConstraint` is the third established instance of the same pattern, not a novel duplication.
- **Sources consulted for this decision**: `ProofEngine.cs:29-33` — `record GuardConstraint(string Field, OperatorKind Comparison, decimal? Value, bool IsPresenceCheck)`; `Strategies.cs:716-748` `ExtractGuardBranchesCore` AND/OR walk typed to `GuardConstraint`.
- **Strongest counter-evidence**: Option B (shared DU) would avoid duplicating the AND/OR walk. Looked at `Strategies.cs:687-748`. Response: the walk is ~30 lines; duplicating it is cheaper than de-risking every numeric-narrowing test against a retyped branch structure.
- **Reversibility**: `Hard` — the fact type threads through seed+discharge; changing it later touches both. Not irreversible (internal, no author surface).
- **Blast radius**: `ProofEngine.cs` (new record), `ProofEngine.Strategies.cs` (extractor + seed), `ProofEngine.Qualifiers.cs` (discharge consult); no docs/samples; no external consumers.

### Decision 2: Discharge via a dedicated `TryQualifierGuardNarrowingProof` strategy; keep `ResolveQualifierOnAxis` declaration-pure
**Stakes**: high
- **Rationale**: `ResolveQualifierOnAxis`/`ResolveQualifierFromExpression` are called from many sites and take no guard context; threading one in touches all callers. A dedicated strategy that pulls the guard from `obligation.Context` (as `TryGuardInPathProof` does) isolates the new behavior and keeps the declaration-resolution path unchanged.
- **Tradeoff accepted**: two resolution paths for a qualifier (declared vs guard-narrowed) that must be kept consistent. Accepted: isolation is worth it; the discharge strategy composes (declared first, then guard) rather than forking resolution.
- **Alternatives considered**: thread an in-scope-narrowing parameter into `ResolveQualifierOnAxis` — rejected: high blast radius across all its call sites (`Qualifiers.cs:25-26,64-65`, recursive binary-op arms) on a soundness-critical function.
- **Precedent**: `TryGuardInPathProof` (`Strategies.cs:618-680`) already pulls the guard from `obligation.Context` for numeric/presence — the new strategy is its qualifier sibling.
- **Sources consulted for this decision**: `Qualifiers.cs:256-435` `ResolveQualifierOnAxis(subject, axis, site, semantics)` — no guard param, resolves `field.DeclaredQualifiers`; `ProofEngine.cs:624-678` `TryDischarge` dispatch; `Strategies.cs:620-632` guard-from-Context pattern.
- **Strongest counter-evidence**: a dedicated strategy risks dispatch-ordering bugs vs `TryQualifierCompatibilityProof`. Looked at `ProofEngine.cs:624-678`. Response: ordering is explicit and testable; place the new strategy adjacent and assert order in tests.
- **Reversibility**: `Hard`. **Blast radius**: `ProofEngine.cs` (dispatch), `ProofEngine.Strategies.cs`/`Qualifiers.cs` (new strategy); plus the assignment-validation site (`AssignmentQualifiers.cs`) which must consult the same fact for Site A.

### Decision 3: Soundness rules are hard gates — axis-appropriate discharge, all-branches-OR, intersection-with-declared, literal-RHS-only, negation-narrows-nothing, reassignment-invalidation
**Stakes**: high
- **Rationale**: Each rule closes a concrete unsoundness in the **`analysis § Failure-mode → rule → required-test catalog`** (11 rows, each with a fail-before/pass-after test). **Axis-appropriate discharge** (N2 correction): value-**equality** for identity axes (currency/unit/dimension/from/to) closes the #1 trap; **subset** (`⊆`) for the period `.basis` axis matches D14's accept-iff-non-zero-components-⊆-declared rule (`business-domain-types.md:1817`) — using equality there would *wrongly reject* a sound `'hours'`-narrowed value against an `'hours + minutes'` obligation. all-branches-OR closes OR-arm collapse; intersection-with-declared closes contradiction-override; literal-RHS-only and negation-narrows-nothing close field-to-field and `!=` false positives; **reassignment-invalidation now consults the shipped `ReassignedBefore` set** (spec § 0.6 item 7, built in BUG-014/015) — a `set X`/`clear X` mid-body invalidates X's narrowing fact for later obligations, with no new mechanism. Without these, the feature violates Principle 1.
- **Tradeoff accepted**: more conservative than authors might expect — `when X.currency=='USD' or =='EUR'` does NOT let you assign to a USD-only field; `X.currency != 'JPY'` narrows nothing. Accepted: soundness over convenience is the whole point (and matches the numeric path's stance).
- **Alternatives considered**: best-effort narrowing (TS-style, accept some unsoundness) — rejected: violates Principle 1, non-negotiable for a prevention-first engine.
- **Precedent**: the numeric path's `ValueSatisfiesRequirement` value-exactness (`Strategies.cs:881`) and its all-branches discharge loop over the *branch* extractor `ExtractGuardBranchesCore` (`:716-748` + `:660-677`) — this is the all-branches-OR precedent we mirror. (Note: the `// OR: do NOT decompose` line at `:766` is a *different*, conservative handler in the FLAT extractor `ExtractGuardConstraintsCore` — it discards OR entirely; we mirror the branch extractor, not the flat one.)
- **Sources consulted for this decision**: `Strategies.cs:870-898` `NumericConstraintSubsumes`/`ValueSatisfiesRequirement`; `:660-677` all-branches discharge; `:766-768` OR-no-decompose; `Qualifiers.cs:126-133` `Any`-rejection in `QualifiersAreCompatible`; `business-domain-types.md:1817` (N2) — "a value is accepted iff its non-zero component set is a subset of the declared basis"; `ProofLedger.cs` `ProofObligation.ReassignedBefore` (init-prop, IsDefault-safe) + `ProofEngine.cs WalkActions` stamping — the shipped reassignment-invalidation substrate the discharge consults.
- **Strongest counter-evidence**: authors will hit "my OR-guard didn't narrow" friction. Looked at the numeric path — it has the same friction and accepts it. Response: a teachable diagnostic ("guard narrows to a set {USD,EUR}; the field requires exactly USD") mitigates; soundness is not negotiable. **Pre-recorded resolution for the OR-friction falsifier**: if the falsifier fires in real authoring (a sample genuinely needs OR-discharge), the fix is the teachable diagnostic + author guidance to split the branches — **never** relaxing the all-branches rule, which is the soundness guarantee. A future maintainer must not reach for the unsound shortcut under friction pressure.
- **Reversibility**: `Effectively-irreversible-post-ship` in spirit — relaxing a soundness rule later could admit programs that were rejected, but *tightening* is always safe; we ship the sound (strict) version.
- **Blast radius**: the discharge predicate (`Qualifiers.cs`/new strategy); the test suite (the full failure-mode catalog); diagnostics wording.

### Decision 4: Partition-aware `dimension` typed-constant validation, catalog-driven (folded-in PRE0053 fix)
**Stakes**: medium
- **Rationale**: spec § dimension already locks "partitioned by parent type — the type checker knows which partition to validate against" (`business-domain-types.md:762`); the validator is partition-blind (one `ClosedSetValidation` over `DimensionCatalog.All.Keys`, UCUM-only — `Types.cs:125-130`). The clean mechanism, established by the re-grounding evidence: the dimension validation becomes **context-aware**, selecting the partition from a **catalog-declared partition tag on the dimension accessor** (period's `.dimension` → temporal partition `{date,time,datetime}`; quantity/uom/price `.dimension` → UCUM partition). This is **not** a `TypeKind` switch (G2) — the accessor carries the partition, the type checker reads it. It is **not** a contract change to `ClosedSetValidator` (G1): the outer dispatcher `TypedConstantValidation.Validate(validation, rawText, targetType, context)` **already** passes `targetType`+`context`, and the NodaTime/Ucum/Quantity arms already consume them — only the `ClosedSet` arm currently drops them. The `stateref` exception (`literal-system.md:232` — validated against declared state names, not a global closed set) is the documented precedent that context-dependent content validation is already permitted. Resolves the `'time'` collision (UCUM physical-time vs temporal time) by partition.
- **Tradeoff accepted**: dimension validation gains a partition-selection step keyed on the comparison peer's accessor metadata. Worth it: a flat union would make `quantity.dimension == 'date'` wrongly pass and leave `'time'` ambiguous (the UCUM catalog has a physical `'time'` at `DimensionCatalog.cs:13`).
- **Alternatives considered**: flat union (accept date/time/datetime globally) — rejected (unsound cross-partition + `'time'` ambiguity); separate `temporaldimension` type — rejected (spec models one partitioned `dimension`); a partition arg threaded into `ClosedSetValidator.Validate` itself — rejected (changes the context-free closed-set contract; instead route through the already-context-aware dispatcher arm).
- **Precedent**: `MapTemporalDimensionQualifier` (`TypeChecker.cs:377-391`) already does period-context temporal validation for the `of` path; D4 extends the same awareness to the comparison path. `stateref` name-binding (`literal-system.md:232`) is the in-tree precedent for context-dependent (non-global-closed-set) validation.
- **Sources consulted for this decision**: `business-domain-types.md:762` — "The registry is partitioned by parent type — the type checker knows which partition to validate against"; `:787-790` partition table (UCUM vs Temporal); `:792` cross-partition is a compile error; `Types.cs:125-130` `DimensionValidation` (single UCUM `ClosedSetValidation`); `DimensionCatalog.cs:9-22` (UCUM keys incl. physical `'time'`, no `date`/`datetime`); `TypedConstantValidation.cs:5-20` — dispatcher passes `targetType`+`context`, ClosedSet arm drops them; `ClosedSetValidator.cs:3-13` — context-free `Validate(rawText, validation)`; `literal-system.md:228,232` — `stateref` context-dependent-validation precedent.

### Decision 5: `.basis` narrowing is discrete-string-accessor narrowing, not a faked `ReturnsQualifier`
**Stakes**: medium
- **Rationale**: `.basis` returns `TypeKind.String` (`Types.cs:480`); assigning a `ReturnsQualifier` to a String accessor would corrupt the interpolation/hover consumers that assume the return type carries the axis. Model basis narrowing as a discrete component-set fact on the accessor, canonicalizing the guard-literal RHS via the W-A canonicalizer (else `'minutes + hours'` silently never matches canonical `'hours + minutes'`), then discharging by **D14 subset** (N2: `components(narrowed) ⊆ components(required)`), not string equality. Note the open-period case is exactly what the guard is *for* (N1): an open period has no static basis, so `when X.basis == 'hours'` is the author's only way to give it one — the guard literal supplies the component set the discharge tests.
- **Tradeoff accepted**: basis is a third narrowing shape (string-accessor) alongside qualifier-axis narrowing. Accepted: it's genuinely a string comparison; faking an axis is worse.
- **Alternatives considered**: give `.basis` `ReturnsQualifier: TemporalUnit` — rejected (incoherent on a String-returning accessor; corrupts slot resolver `AssignmentQualifiers.cs:694`).
- **Precedent**: W-A canonicalization (`MapTemporalUnitQualifier`, commit `3e116ced`); D15 cancellation rule (`business-domain-types.md` § D15).
- **Sources consulted for this decision**: `Types.cs:480` `new FixedReturnAccessor("basis", TypeKind.String, ...)`; `AssignmentQualifiers.cs:313,694` `ReturnsQualifier` consumers.

### Decision 6: Period `.dimension` derivation (TemporalUnit→DerivedDimension) wired onto the discharge path
**Stakes**: medium
- **Rationale**: a period's dimension derives from `TemporalUnit.DerivedDimension`, not a stored TemporalDimension qualifier. `ResolvePeriodDimension` (`Strategies.cs:227-241`) does this derivation but is reachable only from the `of`-path `DimensionProofRequirement`. The new discharge must call it (or a shared helper) or period `.dimension` narrowing resolves nothing.
- **Tradeoff accepted**: the discharge strategy gains a period-specific derivation step. Accepted: it's a small shared-helper extraction.
- **Alternatives considered**: duplicate the derivation in the discharge — rejected (drift risk; share the helper).
- **Precedent**: `ResolvePeriodDimension` (`Strategies.cs:227-241`) already encodes the derivation.
- **Sources consulted for this decision**: `Strategies.cs:227-241` `ResolvePeriodDimension` (TemporalDimension → value; TemporalUnit → DerivedDimension); `DeclaredQualifierMeta.cs:75-80` `TemporalUnit(UnitName, DerivedDimension, …)`.

### Decision 7: `datetime` compare-but-inert; `!=`/else non-actionable; unitofmeasure `.dimension` in scope
**Stakes**: low
- **Rationale**: `period.dimension == 'datetime'` is a legal compare (RHS value-space includes datetime per the partition table) but discharges nothing for a Date/Time `DimensionProofRequirement` (datetime admits all components — proves no single class), mirroring the `of 'datetime'` rejection at the compare level. `!=`/else narrows nothing usable (analysis § soundness rule 6). unitofmeasure `.dimension` is included for uniformity (the accessor table lists it).
- **Tradeoff accepted**: an author writing `== 'datetime'` expecting it to enable `date ± period` gets no discharge. Accepted: it's semantically correct (datetime proves nothing for single-class arithmetic); a teachable message explains.
- **Alternatives considered**: reject `== 'datetime'` entirely — rejected (it's a valid observation of a both-spanning period's dimension; only its *discharge power* is nil).
- **Precedent**: the W-B amendment's `of 'datetime'` rejection (`business-domain-types.md § dimension`, commit `6f12706a`).
- **Sources consulted for this decision**: `business-domain-types.md § dimension` partition table — "'datetime' (return-only)".

## Falsifiers
- If wiring the discharge strategy reproduces a numeric-narrowing test failure (the new fact leaking into the numeric path), the parallel-record isolation (D1) failed — revisit.
- If any of the 11 rows in the `analysis § Failure-mode → rule → required-test catalog` cannot be made to fail-before / pass-after, the soundness model (D3) is incompletely implemented.
- If the spec's three § D9 acceptance examples cannot all compile-and-prove after, the discharge wiring (D2/D6) is incomplete.
- If a sample needs `X.currency == 'USD' or == 'EUR'` to discharge a single-currency assignment, the all-branches rule (D3) is too strict and the OR semantics need reconsidering.
- If threading LHS-type into dimension validation (D4) changes resolution for non-dimension typed constants, the partition selection isn't scoped tightly enough.

## Acceptance criteria
- `precept_compile`: the three § D9 examples (open money+currency, open quantity+dimension, open period+dimension) compile AND the guarded assignment proves (no PRE0141/0114/0113).
- `precept_compile`: value-mismatch (`X.currency=='USD'` → assign to `money in 'EUR'`) → rejected; OR-collapse (`=='USD' or =='EUR'` → assign to USD-only) → rejected; `!=`/field-to-field guards → no discharge; cross-partition (`quantity.dimension == 'date'`) → partition-mismatch; `period.dimension == 'datetime'` → compiles but does not discharge a Date obligation.
- `period.dimension == 'date'|'time'|'datetime'` compiles; `period of 'datetime'` still rejected.
- `.basis == 'hours'` narrows an open period for **D14 assignment** — `set <period in 'a + b'> = openPeriod` discharges by subset. ✅ delivered (S3). **Correction:** the original wording conflated this with `price in 'USD/hours' × period` *cancellation* and composite single-basis *rejection* — those are dimension-/basis-level **cancellation** (W-C), not D9 basis-assignment narrowing. The `price × period` site is dimension-level (a declared single basis does not yet even resolve its dimension there — see the W-C exit criteria); the composite-rejection is D15. **Both deferred to W-C** (wired into its exit criteria), not satisfied by D9.
- All 11 rows of the `analysis § Failure-mode → rule → required-test catalog` have a fail-before/pass-after test (incl. cross-step reassignment, row 7).
- Adding `ReturnsQualifier` to `.dimension` does not regress hover or interpolation-slot resolution (CONCERN-2 tests).
- `dotnet test` green (incl. untouched numeric-narrowing + W-A composite tests); `dotnet build` 0 warnings; diagnostic-coverage analyzer clean.

## Dependencies
- Upstream: W-A composite basis (`DerivedDimension`, `PeriodDimension.Datetime`, canonicalizer) — shipped `3e116ced`. Numeric narrowing (F-LANG-BIZ-08) — shipped, the structural template.
- Downstream: closes the W-B premise (composite `.dimension` guards both compile and narrow); delivers the spec § D9 promise repo-wide; enables open-field governance patterns.

## Doc-update enumeration
- `docs/language/business-domain-types.md` § D9 — replace the unbuilt-infra "Mechanism" (`$eq:`/StaticValueKind) with the real fact/discharge mechanism; correct the `:762` partition claim to describe the implemented partition-aware validation; reconcile accessor-table vs prose on unitofmeasure `.dimension`.
- `docs/compiler/proof-engine.md` — new `TryQualifierGuardNarrowingProof` strategy + `QualifierNarrowingConstraint` fact; the discharge rules.
- `docs/compiler/type-checker.md` — partition-aware dimension-literal validation; `ReturnsQualifier` on `.dimension` accessors.
- `docs/compiler/literal-system.md` — the `dimension` content-validation row gains partition awareness (temporal partition `{date,time,datetime}` alongside the UCUM partition), routed through the dispatcher's existing `targetType`/`context` pass; document it as a second context-dependent case alongside `stateref`.
- `docs/compiler/diagnostic-system.md` — narrowed-vs-required wording on PRE0141/0114/0113; partition-mismatch message/code.
- `docs/language/catalog-system.md` — if a partition accessor or new diagnostic code lands.

## Operational dimensions
- **Security** (source ingestion): N/A beyond existing guard/typed-constant parsing.
- **Observability** (diagnostic surface): the narrowed-vs-required diagnostics are the operator signal; a guard that narrows nothing leaves the obligation visibly unresolved (inspectability).
- **Evolvability** (external standard): N/A — internal proof mechanism; the partition sets are Precept's own.

## Open questions
**None blocking lock.** The draft's sole owner-facing question (CONCERN-1, cross-step reassignment) is **resolved by re-grounding**: sequential-proof-flow invalidation shipped in BUG-014/015/016 (`ReassignedBefore` + the count-fact forward walk), so the qualifier discharge consults an existing substrate rather than introducing or deferring a mechanism — option (a), already built. The other load-bearing questions are resolved inline: N1 (open-period `.basis` — the guard supplies the basis, D5), N2 (basis discharge is D14 subset not equality, D3/D5), G1/G2/G5 (partition validation is catalog-driven via accessor-declared partition + the dispatcher's existing `targetType`/`context` pass, precedented by `stateref`; D4). Out of scope (not open): A2 forward marker-propagation (`set X = Y` copying qualifier markers — not needed for the § D9 acceptance examples, which narrow from the guard); N3/N5 (W-C composite-legal-by-operation ordering — belong to the W-C plan, not this discharge design).

Remaining before `Locked`: owner confirmation of the **slicing/sequencing** (see the gaps appendix) and the lock itself.

## Spec-coverage gaps — resolution log (audits 2026-05-28; re-grounded 2026-05-29)

The 2026-05-28 audits flagged the gaps below. A 2026-05-29 re-grounding pass (verbatim-evidence agents over `literal-system.md`, `business-domain-types.md` §§ period/dimension/D14, and the current proof-engine `src/`) dispositioned each. **Resolution summary:**

| Gap cluster | Disposition |
|---|---|
| **A1** (reassignment-invalidation absent) | ✅ **Resolved by re-grounding** — built in BUG-014/015; `ReassignedBefore` ships and all guard strategies consult it. The discharge rides it (D3, Semantic Rules). |
| **A2** (forward marker propagation) | ⏸️ **Out of scope** — `set X = Y` copying qualifier markers is not needed for the § D9 acceptance examples (they narrow from the guard, not a prior set). Count-fact forward walk shipped (BUG-016); qualifier-marker propagation is a later completeness extension, not this cut. |
| **N1** (open-period `.basis` no static string) | ✅ **Resolved** — the guard *is* the basis source for an open period (`when X.basis == 'hours'`, the spec's own remedy `:961`); D5 amended. |
| **N2** (D14 subset vs equality) | ✅ **Resolved (was a real error)** — D14 acceptance is `⊆` (`business-domain-types.md:1817`); D3/D5/Semantic-Rules now use axis-appropriate `satisfies_α` (equality for identity axes, subset for basis). |
| **G1/G2/G5** (partition validation / literal-system contract) | ✅ **Resolved in principle** — partition selection is catalog-driven (accessor-declared partition + the dispatcher's existing `targetType`/`context` pass, which the NodaTime/Ucum/Quantity arms already use; `stateref` is the documented context-dependent-validation precedent). No `TypeKind`-switch; no contract change to `ClosedSetValidator`. `literal-system.md` added to doc-update enumeration. D4 carries the mechanism. |
| **G3** (Site A vs declaration-pure D2) | ✅ **Resolved by a dedicated design** — `docs/Working/assignment-qualifier-discharge-placement.md` (Locked 2026-05-29). The assignment-qualifier check is **stamped as a proof obligation** (per `proof-engine.md` Decision 3) and discharged by the narrowing strategy — *not* narrowed inline at the type checker (ruled out: no flow substrate there) and *not* a new VC-handoff. PRE0141 re-stages Type→Proof as a regularization. `ResolveAssignmentQualifierAxis` stays declaration-pure (D2 preserved). Grounded in two 2026-05-29 surveys. |
| **G6** (`ReturnsQualifier` contract undocumented) | 📋 Doc obligation — `type-checker.md` update enumerated; design introduces the meaning + consumers (hover, slot resolver). |
| **A8/A9** (diagnostic-code attribution) | 📋 Reuse existing codes (`InvalidDimensionString` 77 for cross-partition); no new partition-mismatch code unless a probe shows a gap. |
| **N3/N5** (W-C composite-legal-by-operation seam) | ⏸️ **Belongs to the W-C plan**, not this discharge design — flagged for the W-C coordinator. |
| **A4/A5/A6** (Tier-2 dynamic narrowing; field-to-field dimension; partial-quantity unit narrowing) | ⏸️ **Deferred** — out of this cut (open-field member-access narrowing); A5 cross-partition field-to-field already errors per D4. |

The original audit text is retained below as provenance. Items not in the table above (A3 attribution payload, A7 stateless-guard check, A10–A14, G7/G8/G9) are implementation-level and fold into `/lifecycle-3-plan` slices; none blocks the design's correctness now that the load-bearing soundness questions (N2, A1) are settled.

### Original audit text (provenance — 2026-05-28)

Two spec-completeness audits (the first authored the design with insufficient spec reading; these audits read the relevant docs in full and cross-checked).

**Audit-2 (type-checker.md + literal-system.md):**
- **G1 [contract violation]** — partition-by-LHS (D4) contradicts `literal-system.md`'s context-born-*then*-content-validated model: `ClosedSetValidator.Validate(rawText, validation)` takes no context arg; the `ContentValidation` DU *is* the closed registry. Either express partition as catalog metadata keyed by the already-known type, or change (and document) the content-validation contract. **Add `literal-system.md` to doc-update enumeration (currently omitted).**
- **G2 [catalog violation]** — partition *selection* keyed on LHS `TypeKind` identity reintroduces the TypeKind-switch F-TC-04 removed (`type-checker.md:930`). Must be catalog-metadata-driven.
- **G3 [tension]** — Site A (type-checker assignment-qualifier validation consulting the narrowing fact) conflicts with D2 (declaration-pure resolution, discharge in ProofEngine). The closed `ResolveAssignmentQualifierAxis` switch takes `(value, axis)` only — no guard context. Specify the mechanism or relocate.
- **G4 [contract violation]** — `.basis` canonicalization at discharge (D5) violates the type-checker normalization-boundary contract; canonicalize the RHS at the checker, ProofEngine compares already-canonical strings.
- **G5** — `dimension` content-validation table (`literal-system.md:223`) has no door for `date`/`time`/`datetime`; the "make `period.dimension == 'date'` compile" prerequisite is unreconciled (needs a partitioned closed set or a documented second exception alongside `stateref`).
- **G6** — `ReturnsQualifier` accessor-metadata contract is undocumented in `type-checker.md`; the design must *introduce* it (meaning + consumers: hover, slot resolver), not just note four edits.
- **G7** — error-recovery / ErrorType-propagation interaction for a partition-mismatch RHS (`TypedErrorExpression` vs diagnostic-with-valid-RHS) unspecified — affects whether a rejected RHS suppresses the narrowing seed.
- **G8/G9** — dual seed predicates (`ReturnsQualifier≠None` axis case vs `.basis` String case) not crisply separated; doc-update omits `literal-system.md`.

**Audit-1 (proof-contract + business-domain), the load-bearing ones:**
- **A1 [contract]** — sequential proof flow / reassignment-invalidation (spec § 0.6 item 7) is mandated + claimed-implemented but absent in `src/` (BUG-014). Not an owner decision — implement it; correct the impl-status drift.
- **A2 [contract]** — forward narrowing propagation (`set X = Y` copies Y's markers, § D9 Mechanism #4) entirely missing from the design.
- **A3 [missing]** — proof attribution payload (§ 0.6 item 6): the discharge must produce a structured `ProofSatisfaction` projection (guard leaf + axis + value); design asserts attribution without a payload.
- **A4 [missing]** — Tier-2 dynamic narrowing on identity-typed fields (`when ActiveCurrency == 'USD'`, § D9/D14 worked examples) — design's seed matches only member-access; deferred as out-of-scope but spec presents it first-class.
- **A5 [missing]** — field-to-field dimension comparison (`Reading.dimension == AllowedDimension`, § dimension Patterns 1-3) both unsupported and not rejected; cross-partition field-to-field must error (§ dimension:792).
- **A6 [missing]** — narrowing the unit axis of a declared-but-partial `quantity of 'mass'` field (§ D9 contract row 3); design scopes to open fields only.
- **A7 [tension]** — stateless-precept guard-narrowing claim vs `proof-engine.md:2084` (Strategy 3 N/A for stateless — verify `EventHandlerContext.Handler.Guard` exists).
- **A8/A9** — diagnostic-code attribution (spec names `QualifierMismatch` 68 / `DimensionCategoryMismatch` 69, not proof-stage PRE0141/0114/0113; new "partition-mismatch code" likely unwarranted — reuse `InvalidDimensionString` 77).
- **A10–A14** — price three-axis disambiguation unstated; else-branch negation vs negation-narrows-nothing conflict; strategy ordinal not pinned; contradiction-as-unsatisfiable depends on unbuilt PRE0082; normalization-at-boundary (dup of G4).

**Audit-3 (temporal-type-system.md full + spec §1-3 + parser.md + catalog-discipline) — new gaps beyond the above:**
- **N1** — open-period `.basis` has no static canonical string to compare against (`.basis` on an open period is the *runtime* non-zero decomposition, `business-domain-types.md:1226`); the design's `.basis == 'hours'` narrowing (D5) assumes a *declared* basis. The open-period case — the one narrowing is *for* — is unaddressed (distinct from G4, which is only about where canonicalization runs).
- **N2** — D14 acceptance is a `⊆` subset relation (`business-domain-types.md:922`), but the design's discharge `satisfies(v,w)` is value-*equality*. A declared `period in 'hours + minutes'` used where an `hours` obligation needs satisfying is a subset question equality can't express. Unspecified.
- **N3** — composite legal-by-source-operation (W-C) and D9 narrowing both emit/suppress on the same `period` QualifierMismatch surface; ordering unspecified.
- **N4** — CONCERN-2's hover/interpolation tests must include the `Datetime` case (`'{X.dimension}'` / hover on a composite period now resolves `datetime`); the enumeration only named currency/dimension.
- **N5** — the W-A↔W-C seam: plan W-C NIT-1 (`Count > 1` / `== Date` dead heuristics) and the design's Decision 6/7 touch the *same* predicate; uncoordinated, one could re-introduce the false-green the plan warns of.

**Audit-3 verdict on shipped work**: W-A (commit `3e116ced`) is **consistent** with temporal-type-system.md — no Decision #26, cancellation, or dimension-contract violation; the "no parser change" claim is confirmed against `parser.md`. The new gaps are all D9-design / W-C-seam, not in W-A's committed code.

The full audit reports (with verbatim spec citations) are in the conversation record; this appendix is the actionable summary the rework consumes.
