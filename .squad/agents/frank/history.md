## Core Context



- Owns language research, spec wording, and cross-surface architecture documentation for the Precept DSL and runtime.

- Catalog metadata remains the language truth; runtime, tooling, and docs should derive from durable metadata rather than enum-identity switches or parallel lists.

- Proof, qualifier, and field-state design work must stay grounded in the shipped spec and evaluator/runtime surfaces, not inferred intent.



## Live Guidance



- `readonly` / `editable` / guarded access modes govern the Update (patch) surface; they do not restrict event-driven `set` actions unless the spec changes.

- Business-domain magnitude modifier legality is a catalog contract: fix drift in metadata and docs, not with checker-only special cases.

- Required-field constructor enforcement is now an active implementation surface: D93 and D94 are real checker obligations, while stateless construction stays outside the state-entry slice.



## Historical Summary



- 2026-05-12 through 2026-05-13 concentrated Frank's work around hover contract reviews, comma-list `StateTarget` closure, field-state-guarantees design, modifier applicability drift, constructor-gap analysis, and diagnostic coverage enforcement planning.

- Durable batch-by-batch detail now lives in `.squad/decisions.md` and `history-archive.md`; this live file keeps the research posture plus the newest conclusions other agents need immediately.



## Recent Updates

### 2026-05-13T04:32:04Z — Diagnostic coverage enforcement now has two enforced gates

- `docs/working/diagnostic-coverage-enforcement.md` now records Gate 1 emission-site coverage plus Gate 2 emitted-code test coverage as separate convention-test allow-list checks.
- Frank confirmed the current baseline is clean for Gate 2: all 83 emitted diagnostic codes are referenced in tests, so the emitted-but-untested allow-list starts empty while 7 codes remain neither emitted nor tested.




### 2026-05-13 — Field-state guarantees and constructor enforcement are the active baseline



- The v3 field-state design is now canonically D130/D131/D132, with author-language follow-up still needed for D132's shipped name.

- Frank's gap audit established that declaration metadata is not enough: D93/D94 were specified but unenforced, so prerequisite audits must verify real `DiagnosticCode` emission sites before downstream design work assumes behavior exists.

- George's Slice 9 and Slice 10/11 follow-through closed the immediate construction-risk loop: disjunction support landed, D93 now blocks no-initial-event required-field holes, and D94 now enforces required assignments on initial-event construction paths while keeping stateless precepts exempt.

- The diagnostic coverage enforcement recommendation remains a convention-test allow-list model, using literal `DiagnosticCode.{Member}` references in pipeline and catalog-emission files as the minimum coverage bar.



## Learnings

### 2026-05-13T18:17:15Z — Interval-proof design doc authored

- `docs/working/interval-proof-engine-design.md` authored as a complete design + implementation plan for Strategy 7 (`IntervalContainment`) in the proof engine.
- **Key architectural decision:** No new `@bounds` annotation syntax. The interval engine reads bounds from existing `min`/`max` field modifiers via `ValueModifierMeta.ProofSatisfactions`. One source of truth; no migration cost.
- **`integer` fields are exempt.** `TypeKind.Integer` = BigInteger — mathematically unbounded. No `IntervalContainmentProofRequirement` is generated for integer targets, ever.
- **`ProofStrategy.IntervalContainment = 7`** is the new strategy number, extending the existing `ProofStrategy` enum without displacing existing values.
- **`BinaryOperationMeta.IntervalTransfer`** is the catalog-driven extension: each arithmetic operation in the `Operations` catalog declares its own interval transfer function. The proof engine reads it — no hardcoded per-operator arithmetic in the engine body.
- **Gradual adoption is correct.** Fields with no declared bounds generate no obligation. The prevention guarantee is strengthened only for bounded fields; unbounded fields retain runtime fallback. This is honest and consistent with Precept's philosophy.
- **Hover extends, never replaces.** Interval proof results use existing badge vocabulary (`✅`, `⚡`, `⚠️`, `🔬`) and extend the existing proof expression hover card with an interval sub-line. No new card kinds. Elaine's sign-off on §6 of the design doc required before Slice 5.
- **Soundness is guaranteed, not approximated.** Strategy 7 never produces a false positive. Conservatism manifests as false negatives (unresolved obligations on provably-safe expressions with complex guard interactions) — these are addressed by Slice 3 (guard narrowing). False negatives are safe; false proofs are not.
- **Existing precepts with bounded fields may get new `NumericOverflow` errors after implementation.** This is a semantic breaking change that is intentional and correct. Release notes must document it clearly.

### 2026-05-13T18:32:11Z — Test strategy revised per Soup Nazi review; strategy count corrected

- **Soup Nazi blocked the design on 5 issues.** All 5 resolved in the design doc revision:
  1. `ParseAndGetSemanticIndex(string preceptSource)` helper defined in `IntervalTestHelpers.cs` — real compile pass on inline string, not a stub. All B/C/D tests use it.
  2. Hover tests expanded from 6 to ≥15 in §9.1 F and §8.2 Slice 5. Covers: all 6 template variants (4.1–4.6 including combined-gap 4.6), routing priority (squiggle beats field hover), interval notation format (`[lo .. hi]`, thin-space thousands separator), badge distinction (`⚖️` declared vs. `🔬` inferred), V1 boundary (no expanded view).
  3. Currency/unit × interval routing: **both diagnostics fire independently** — S5 and S7 are orthogonal obligations. Neither suppresses the other. Hover shows first-match per existing routing rules; both appear in diagnostics list.
  4. Sample file references replaced with inline precept const strings. `invoice-line-item.precept` uses computed fields (no `set` actions); `loan-application.precept` uses `money` not `decimal` — neither was usable for the integration scenarios.
  5. Multi-`set` action edge case added to §9.3: 3 bounded `set` targets in one transition row → 3 independent `IntervalContainmentProofRequirement` obligations, each discharged independently.
- **Strategy count was wrong in §1.** Design doc said "five strategies" but the actual `TryDischarge` method in `ProofEngine.cs` calls six: Literal, DeclarationAttribute, GuardInPath, FlowNarrowing, QualifierCompatibility, **CompositionalConstraint** (the 6th, missed in §1's narrative). The enum in §3.4 was already correct. Corrected §1; `IntervalContainment = 7` numbering unchanged.
- **Test file partitioning adopted** (Soup Nazi MAJOR, not BLOCKER): one file per layer — `NumericIntervalArithmeticTests.cs`, `IntervalOfTraversalTests.cs`, `IntervalContainmentStrategyTests.cs`, `GuardNarrowingTests.cs`. 178 tests in one file was a merge conflict risk across 6 parallel slices.
- **Catalog count assertion is non-negotiable.** `ProofRequirements.All.Length.Should().Be(7)` must be added to catalog tests at Slice 1 completion gate. Named anchor #11 in §9.4.
- **`SemanticIndex` fixture lesson:** For proof engine unit tests that need `SemanticIndex`, the right approach is a real parse on a minimal inline precept string, not a stub. Stubs replicate internal invariants incorrectly. A 10-line inline precept parses in milliseconds. Document the helper clearly — multiple developers will use it across 6 slices.

## Historical Summary



- If a design depends on an existing diagnostic, confirm the emitting pipeline stage instead of trusting `DiagnosticMeta` declarations or spec prose.

- `set` into an `omit` target-state field is the decisive field-state rule; Update access modes do not constrain Fire semantics.

- D132 is the structural complement to D94: both exist to prevent required fields from becoming present without a valid value.

- Catalog static initialization may not reach downstream catalog statics during cctor execution; reverse references must defer through `Lazy<T>`.

- **Two-gate enforcement is the right model for diagnostic coverage.** Gate 1 (emission-site exists) and Gate 2 (test reference exists for every emitted code) enforce orthogonal properties and need separate allow-lists. Gate 1 catches "declared but never emitted" gaps; Gate 2 catches "emitted but never tested" gaps — the more dangerous class because the code ships to users untested.
- **Current test suite is in excellent shape.** Source scan found zero emitted-but-untested diagnostic codes — all 83 codes with emission sites are referenced in test files. Gate 2 starts with an empty allow-list. The 7 codes with neither emission nor test are: `AmbiguousTypedConstant`, `EventHandlerDoesNotSupportGuard`, `EventHandlerInStatefulPrecept`, `OmitDoesNotSupportGuard`, `OutOfRange`, `PreEventGuardNotAllowed`, `RedundantAccessMode`.
- **D94 false positive was a row-scoping bug, not a design gap.** The construction guarantee (D94) applies only to transition rows whose `FromState` is an initial state — lifecycle rows for the initial event are not construction paths. The fix (`4c567cdc`) is correct and matches the v3 design spec. Two defense layers exist: (1) `NeedsInitialEvent` excludes fields omitted in all initial states, (2) the row filter excludes non-initial-state rows. The D94/D132 boundary is clean: D94 covers construction-time field population; D132 covers lifecycle omit→non-omit crossings.
- **Stateless precepts with initial events are a latent D94 gap.** `ValidateConstructionGuarantees` bails out when `ctx.States.Count == 0`, leaving stateless precepts with `event X initial` and required fields unchecked by both D93 and D94. Low priority (rare pattern) but should be tracked.
- **MCP "stale build" is a restart issue, not behavioral divergence.** The MCP launch script (`start-precept-mcp.js`) rebuilds from source on launch. Apparent discrepancies between MCP and LS are always stale cached runtimes — restarting resolves them. No code-level divergence exists.
- **Gate 2 is a tripwire, not a proof of correctness.** It verifies `DiagnosticCode.X` literal presence in test files but cannot distinguish behavioral assertions (`CheckExpectingError`) from catalog-only references (`DiagnosticsTests` metadata checks) or enum iterations. All 83 emitted codes pass Gate 2 today, but the limitation must be documented so implementers know the minimum bar is higher than what the gate enforces.
- **Negative tests are non-negotiable for gap closure.** A diagnostic that fires on valid input is a false positive — worse than a missing diagnostic in a domain-integrity product. Every gap closure test must include at least one `CheckExpectingClean` negative case alongside the `CheckExpectingError` positive case. The CI tests (`TypeCheckerCITests.cs`) set the standard: every violation test has a companion no-diagnostic test.
- **Test file placement follows domain clustering, not pipeline stage.** New gap tests go in domain-specific files (`TypeCheckerCurrencyUnitTests.cs`, `TypeCheckerCollectionSafetyTests.cs`) or extend existing domain files (`TypeCheckerTypedConstantTests.cs` for temporal, `TypeCheckerStructuralTests.cs` for choice). The naming convention is `{Condition}_{InputDescription}_{ExpectedOutcome}` — PascalCase, no `Test_` or `Should_` prefix.

### 2026-05-13T04:52:18Z — Diagnostic gap docs now lock the negative-test quality bar

- `docs/Working/diagnostic-gap-analysis.md` now records the D94 row-scoping review, targeted regression coverage recommendations, and the low-priority stateless-initial-event enforcement gap.
- `docs/Working/diagnostic-coverage-enforcement.md` now adds the team quality bar: each gap closure needs both `CheckExpectingError` and `CheckExpectingClean`, because Gate 2 alone cannot prove behavioral coverage.

### 2026-05-13T09:38:04Z — Catalog-mediated emission expansion scope documented

- `docs/Working/diagnostic-enforcement.md` §9 now includes a "Catalog-mediated emission: expansion scope" subsection with: governing policy (direct emission default, three criteria for mediated pattern), prioritized top-3 expansion candidates (modifier constraints, typed-constant families, proof obligations), and an explicit do-not-apply list for areas that must retain direct emission.
- Key learning: the three-criteria test (stable 1:1 mapping, uniform logic, membership check on resolved artifacts) is the right filter for when catalog mediation applies. The majority of diagnostic emission rightly stays direct.

### 2026-05-13T09:48:49Z — Expansion scope promoted to concrete implementation slices

- `docs/Working/diagnostic-enforcement.md` now has Slices 9A/9B/9C as full implementation slices (objective, target files, completion gate, test/regression anchors) under a new Priority 4 tier in the tracker.
- Ordering constraints updated: 9A depends on Slice 8, 9B depends on or subsumes Slice 5, 9C is independent.
- Policy remains explicit in-doc: direct emission is default; catalog-mediated is selective (three-criteria test).
- Numbering rationale documented inline: "9" prefix = § 9 origin, letter suffix = avoid disrupting 0–8 gap-closure numbering.
- Decision recorded in `.squad/decisions/inbox/frank-expansion-slices-in-plan.md`.
- Key learning: **Mechanism-migration slices need a different completion gate than gap-closure slices.** Gap closure removes codes from the allow-list (net-new coverage). Mechanism migration proves behavioral equivalence (same diagnostics, different dispatch path). Both need Gate 1 analyzer recognition, but the success criteria are fundamentally different.

### 2026-05-13T09:55:48Z — Expansion slices promoted from long-term to active execution scope

- `docs/Working/diagnostic-enforcement.md` § 9 renamed from "Long-Term Evolution" to "Architectural Evolution" — removed all framing that implied deferral.
- Priority 4 tracker tier annotated ★ Active and explicitly states "in-scope for the current execution plan."
- Slice 9A–9C numbering note updated: references "§ 9 architectural evolution" not "long-term."
- Selective-adoption policy preserved unchanged (direct emission default; catalog-mediated selective via three-criteria test).
- Key learning: **Plan framing creates execution expectations.** If slices have full implementation detail, dependency ordering, and tracker entries, calling the section "Long-Term" signals deferral regardless of content quality. Section titles and tier annotations are scope signals — keep them aligned with actual execution intent.
