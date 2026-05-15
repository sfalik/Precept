## Core Context

- Owns test discipline across parser, type checker, runtime, MCP, language server, and analyzer validation.
- Treats behavioral claims as unproven until executable evidence exists and records gaps as actionable findings.
- Pushes for full-surface coverage matrices, honest red/green pressure, and regression anchors that match the real AST/runtime shape.

## Learnings

- Real catalog metadata is the executable language contract; prefer tiny synthetic fixtures around it over mocked metadata.
- Sample-file integration tests catch parser and language-surface gaps that isolated unit tests miss.
- Span-heavy regressions should derive expected spans from compilation artifacts instead of hand-counted columns.
- Exhaustiveness-style diagnostic fixture tests must supply enough placeholder args for the highest indexed template slot in the catalog.
- When design behavior is still open (diagnostic multiplicity, wildcard fan-out, broadcast identity contracts), wait for the decision before locking tests.
- Quantity-normalization coverage must separate raw typed literals, WholeValue interpolation, and interpolated magnitude-with-static-unit paths; otherwise the dangerous double-normalization and unbounded-interval branches stay invisible.
- When implementation types are still landing, reflection-based tests are the honest way to pin the contract without turning the suite into compile errors or skipped placeholders.
- For `InterpolatedTypedConstant` proof coverage: only `Magnitude` and `WholeValue` slot kinds recurse in `IntervalOfNarrowed`; all other slot kinds (Unit, NumeratorUnit, DenominatorUnit) return Unbounded. Tests must cover both the conservative-overflow case (Unbounded slot) and the proved case (single bounded slot) — the two branches are orthogonal and both need anchors.
- The double-normalization guard for WholeValue slots is `HasSingleMagnitudeSlot`, which returns false for WholeValue → `ApplyStaticUnitScaling` skips re-scaling. To expose a false-safety double-normalization bug via a NumericOverflow test, the source field's normalized bound must straddle the target max when half-normalized but stay below it when doubly-normalized (or vice versa). A same-base-unit source (kg→kg, scale=1) silently passes both correct and incorrect implementations — always use a cross-unit source field for this regression.
- For WholeValue cross-unit (lb_av source → kg target), the false-safety variant needs a target max between the normalized bound (3 lb = 1.36 kg) and the doubly-normalized bound (0.617 kg): e.g., `max '1 kg'` would flip the test between false-safe (0.617 < 1, no overflow) and correct (1.36 > 1, overflow). The task-specified `max '5 kg'` target is a regression anchor for the happy path, not a double-normalization detector; add a tight-bound variant if that specific bug resurfaces.

## Historical Summary

- Earlier 2026-05 work established Soup Nazi's durable posture: convert review findings into shipped tests, keep sample-file gates live, and use real-catalog fixtures for parser/type-checker/analyzer coverage.
- The full chronology now lives in `.squad/decisions.md`; this history keeps only lasting testing rules plus the newest high-value review and coverage outcomes.

## Recent Updates

### 2026-05-15T23:14:11Z — N3/N4 qualifier follow-up test closure landed green in commit `3468dec0`

- Added 26 regression tests across `test/Precept.Tests/TypeChecker/TypeCheckerAssignmentQualifierTests.cs` and `test/Precept.Tests/ProofEngineTypedArgQualifierTests.cs`, covering Frank's N3 implied-qualifier path, compound-cancellation helper parity, and N4 function-call qualifier preservation for `min`, `max`, and `round`.
- Final closeout verification confirmed all 26 new tests pass; full `dotnet test test/Precept.Tests/` finished 5689 / 5699 passing with 10 unrelated pre-existing failures.
- A pre-existing detached HEAD artifact that surfaced as a duplicate variable in `FieldState.cs` blocked an intermediate rebuild, but it was resolved before the final run and did not survive the session closeout.

### 2026-05-15T18:09:58.927-04:00 — Quantity qualifier gap sweep added coverage, but the lane is already green

- Added 8 quantity expression-lane tests to `test/Precept.Tests/TypeChecker/TypeCheckerAssignmentQualifierTests.cs`: bare refs to constrained quantity targets, WholeValue interpolation, bare `.unit` interpolation, binary addition, conditional selection, plus the matching direct/unit-slot pass controls.
- The requested red PRE0141 pressure is no longer honest: the shared assignment resolver already emits `PRE0141` for the bare-ref, WholeValue, unit-slot, binary, and conditional quantity gaps. The binary case also still emits proof-stage `PRE0114`, and the conditional case emits 2 `PRE0141` diagnostics (one per branch).
- Validation: targeted run finished `52 passed / 0 failed`; full `test\\Precept.Tests` still shows `9` unrelated baseline failures in existing proof/quantity tests.

### 2026-05-15T20:40:13Z — The qualifier enforcement regression matrix is now tied to the shipped axis-aware model

- Frank's full analysis is now the canonical rule-set behind the suite: constrained qualifier axes require compile-time proof, and unproved constrained axes fail on the new assignment-stage diagnostic instead of slipping through.
- George's architecture replacement closed the red matrix by replacing the partial-array seam with per-axis resolution and `PRE0141`, while preserving `PRE0068` / `PRE0069` for definite disagreements.
- Scribe merged the matrix and implementation notes into `.squad/decisions.md` and recorded the batch as the durable assignment-enforcement baseline.

### 2026-05-15T16:40:13.267-04:00 — Full assignment-qualifier regression matrix landed with honest 26/14 signal

- Added 19 regression tests to `test/Precept.Tests/TypeChecker/TypeCheckerAssignmentQualifierTests.cs`, covering Frank's required price, money, exchangerate, and shared-surface gaps: bare-source refs, whole-value interpolation, conditional selection, unit-slot axis precision, currency-slot interpolation, plus field-default and event-arg-default call sites.
- Used `Compiler.Compile(...)` plus direct `result.Diagnostics` assertions for every new test; the expected future diagnostic name `UnprovedAssignmentQualifierCompatibility` is pinned as a string literal so the suite compiles before George's enum member lands.
- Focused validation run finished `40 total / 26 passed / 14 failed`; the 14 failures are the intended red cases where the current checker still compiles silent gaps clean.
- Corrected the event-arg fixture to use `amount as money ...` syntax so the red signal stays on qualifier enforcement instead of parser noise.

### 2026-05-15T15:56:59-04:00 — Price typed-constant qualifier regressions added with honest red signal

- Added 4 regression tests to `test/Precept.Tests/TypeChecker/TypeCheckerAssignmentQualifierTests.cs` covering set-assignment count-unit mismatch, matching count-unit success, dimension-qualified currency mismatch, and the shared field-default seam for plain price typed constants.
- Read Frank's bug analysis first and kept the assertions pinned to `DiagnosticCode.QualifierMismatch` / Error severity, matching the existing assignment-qualifier helper conventions in the suite.
- `dotnet test test\Precept.Tests\ --filter "TypeCheckerAssignmentQualifier" --no-restore` finished `15 passed / 3 failed / 18 total`; the three red cases are the intended regression pressure awaiting George's `TryGetAssignmentSourceQualifiers(...)` fix, while the matching control test already passes.

### 2026-05-12T23:50:08Z — Modifier-gap regression suite closed green after coordinator follow-up

- Landed 22 regression tests across `PriceExchangeRateModifierTests.cs`, `IdentityTypeModifierTests.cs`, and `MoneyQuantityModifierRegressionTests.cs`, covering price/exchangerate legality, business-magnitude `maxplaces`, and identity-type redundancy behavior.
- Coordinator corrected the price qualifier fixture and updated `ModifiersTests` drift theories for the split `ZeroBound` vs `Ranged` catalog groups, bringing final repo-wide validation to `4995/4995`.

### 2026-05-12T23:20:42Z — George blocker closeout and redesign re-review approved

- Re-review of commits `53d68d51` and `cf3c6a81` found `0 blockers / 2 good findings`; the explicit-wildcard `ResolvedStateTarget` redesign is structurally correct and ready to merge.

### 2026-05-12T23:02:04Z — Comma-list spike review kept parser/test closure blocked until redesign landed

- Recorded the remaining review gate on commit `a63d88b4`: parser AST coverage for 2-name / 3+-name / whitespace / trailing-comma cases, semantic-clone assertions on expanded rows, and explicit multi-unknown-state diagnostic fan-out.
- Locked the count-integrity rule for this spike: published core-suite totals must match real `dotnet test` output.

### 2026-05-12T19:50:08Z — Modifier catalog gap tests added for price / exchangerate / identity types

- Added 17 price/exchangerate legality tests, 3 identity-type redundancy tests, and 2 money/quantity regression anchors.
- Preserved honest red/green posture: price tests stayed red until implementation landed; exchangerate and identity-type expectations were already green.

### 2026-05-12T18:55:39Z — v2 field-state guarantees test plan review recorded the missing coverage map

- Revised the expected test inventory from ~43 to ~74 and flagged the top blockers: broadcast identity contract, diagnostic exhaustiveness traps, and the lack of event-handler validation coverage.
- Logged the open design questions that must close before affected tests should be written.

### 2026-05-15T03:43:11Z — Quantity-normalization skeleton batch committed with honest 19/12 signal

- Commit `58e498fa` added slice 17, 21, and 37 skeleton coverage across type-checker, proof, numeric-interval, and language-normalizer surfaces.
- `dotnet build test\\Precept.Tests\\Precept.Tests.csproj` passed, and the targeted 31-test run finished `19 passed / 12 failed`.
- The red cases are correctly pinned to still-missing implementation seams: affine metadata/offset behavior, raw-magnitude compare paths, interpolated interval extraction plus static-unit scaling, denominator normalization for cross-unit price comparisons, and the intended cross-dimension diagnostic.
- Keep the failures red until the implementation lands; they are contract pressure, not suite noise.

### 2026-05-15T11:37:42Z — Slice 21 interpolated quantity overflow test coverage completed

- Added 8 new tests to `test/Precept.Tests/TypeChecker/TypeCheckerInterpolatedQuantityTests.cs` covering all required Slice 21 cases: unbounded magnitude (conservative overflow), WholeValue within-bound (Proved), WholeValue overflow, dynamic Unit slot (conservative overflow), dynamic price denominator (conservative overflow), money magnitude within-bound, money magnitude overflow, and WholeValue cross-unit (3 lb → 1.36 kg < 5 kg, no double-normalization).
- All 10 tests in the class pass; suite goes from 5535 → 5543 total, 5526 → 5534 passed, 9 baseline failures unchanged.
- Added `CompileGeneral` helper alongside the existing `CompileAssignment` to support tests with configurable target field types (price, money, quantity) and multiple extra declarations.
- Key finding: the double-normalization guard (`HasSingleMagnitudeSlot`) correctly prevents `ApplyStaticUnitScaling` from re-scaling WholeValue intervals. The test 9 fixture (3 lb / 5 kg target) is a happy-path anchor; a tighter target (e.g., `max '1 kg'`) would distinguish correct (overflow at 1.36 > 1) from doubly-normalized (no overflow at 0.617 < 1) — noted in Learnings for future reference if the bug resurfaces.

- Added 2 missing Slice 17 tests: `PriceBound_CrossUnitDenominatorNormalization_WithinBound_DoesNotEmitNumericOverflow` (test 4b: 3 USD/lb ≈ 6.61 USD/kg < 10 USD/kg → no overflow) and `QuantityBound_WholeValueInterpolation_SourceExceedsMax_EmitsNumericOverflow` (test 8: qtyField max '8 kg' assigned to weight max '5 kg' → [0..8] ⊄ [0..5] → NumericOverflow).
- Both new tests pass immediately; WholeValue interval extraction via the `InterpolationSlotKind.WholeValue` branch in `ProofEngine.Intervals.cs` works correctly when the source field uses the SI base unit (kg). Double-normalization risk (§5.5.2) is dormant for kg-to-kg because scale = 1.0 — Slice 19 must add a non-base-unit WholeValue test to expose any actual double-scaling bug.
- Test 6 (`QuantityBound_CrossDimensionAssignment_IsBlockedByDimensionCheck`) remains intentionally red: (a) bare `m` is not recognized UCUM notation in the current quantity parser, producing `InvalidTypedConstantContent` before the dimension check fires; (b) `ValidateAssignmentQualifiers` early-returns for any `TypedTypedConstant { ResultType: Quantity }`, bypassing `DimensionCategoryMismatch` entirely. Both gaps must be fixed before this test can go green.
- Final suite: 9 failures (unchanged baseline) / 5526 passed (+2) / 5535 total. Commit: `9c98d37b`.

### 2026-05-15T15:37:42Z — Slice 17 cross-unit normalization tests approved

- Added the last two Slice 17 tests: price denominator no-overflow (test 4b) and WholeValue overflow (test 8), closing the planned 9-test matrix.
- Final branch totals moved to 5526 passing (+2) with 9 pre-existing failures; the cross-dimension test remains intentional debt pressure, not a new regression.
- Frank approved the slice and carried two obligations forward: add a true cross-unit WholeValue regression in Slice 19 and track the Test 6 root causes as explicit debt.

### 2026-05-15T22:09:58Z — Quantity qualifier gap sweep is now recorded as green regression closure

- The 8-test quantity qualifier sweep is now merged into .squad/decisions.md as confirmation that bare refs, whole-value interpolation, unit slots, binary results, and conditionals were already enforcing PRE0141 honestly.
- George's later quantity-slot hardening now sits on top of an already-green lane rather than rescuing a still-red gap.

### 2026-05-15T19:02:24.3919248-04:00 — N3/N4 follow-up tests are now pinned directly in the suite

- Added direct TypeChecker coverage for the `duration` implied-qualifier path and synthetic compound-cancellation resolution, so the assignment resolver now has explicit regression anchors for `TemporalDimension(Time)` and `m/s × s -> m` / `length`.
- Added the ProofEngine companion resolver test plus function-call qualifier preservation checks for `min`, `max`, and `round`, closing the review gap on `TypedFunctionCall.ResultQualifiers`.
- Validation is partially blocked by existing rebuild failures in `TypeCheckerFieldStateTests.cs` (`AssertSingleD143` / `AssertNoD143` missing), but the edited files are clean in IDE diagnostics and the baseline 9-failure branch count was unchanged before this slice.

