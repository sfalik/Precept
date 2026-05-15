## Core Context

- Owns code-level feasibility, parser/runtime implementation detail, and architecture-to-code translation across checker, analyzer, and tooling surfaces.
- Parser and checker work stay catalog-derived, array-primary where order matters, and hostile to mirrored duplicate state.
- Shared-environment discipline still applies: validate surgically, stage exact paths only, and preserve durable regression anchors when changing semantics.

## Live Guidance

- Interval and proof work must use explicit authored bounds; `nonnegative` is proof-only and does not populate `DeclaredMin`.
- `quantity` remains a reserved DSL keyword in tests and samples; use names like `qty` instead.
- Guard narrowing only handles field-to-constant comparisons today; arg-ref and broader interpolated reasoning still need explicit implementation.
- `TypedInterpolatedTypedConstant` is still not an exhaustive semantic surface: static qualifier-bearing text and whole-value qualifier identity are missing until new slices capture them.
- Do not treat `PreceptValue` as decimal-only when planning quantity runtime work; quantity unit identity can ride the existing reference lane.

## Historical Summary

- 2026-05-12 and 2026-05-13 closed George's major proof/checker work: interval-proof slices 1-4 validated green, D93/D94 constructor guarantees landed, the Tokens/Types static-init crash was fixed, and the comma-list `StateTarget` program closed with re-review approval.
- Same-qualifier proof repairs and hover/proof-surface closeout remain part of George's durable baseline, but detailed batch chronology now lives in `.squad/decisions.md` and `history-archive.md`.

## Recent Updates

### 2026-05-15T23:14:11Z — Frank's N1 qualifier note is closed in commit `f55e283b`

- Implemented Frank's N1 follow-up in `src/Precept/Pipeline/TypeChecker.Expressions.AssignmentQualifiers.cs`: the slot-hole early return now checks axis applicability and returns `Unknown` instead of `Absent` when the hole type can carry the requested qualifier axis.
- Commit `f55e283b` is the durable fix point for the review note; no additional issues were left open for George on this lane.
- Final closeout validation stayed green for the new work: all 26 new N3/N4 follow-up tests passed, and full `dotnet test test/Precept.Tests/` closed at 5689 / 5699 passing with 10 unrelated branch-baseline failures.

### 2026-05-15T22:51:51Z — Slot-hole qualifier fallback now preserves Unknown when applicable

- `ResolveSlotSourceQualifierAxis(...)` no longer hard-returns `Absent` from the early slot-hole bailout when the hole expression's type can carry the requested axis; it now returns `Unknown` for that correctness seam.
- The fix stays surgical inside `TypeChecker.Expressions.AssignmentQualifiers.cs` and matches Frank's deferred-scoping Item 2 contract for unresolved qualifier-bearing slot expressions.
- Focused assignment coverage remains green at 55/55, and full `dotnet test test/Precept.Tests/ --no-restore --nologo --verbosity minimal --tl:off` stayed on the branch's known 9-failure baseline (5655 passed / 9 failed / 5664 total).

### 2026-05-15T22:27:03Z — Deferred qualifier fixes closed across TypeChecker and ProofEngine

- `TypedFunctionCall` now carries nullable `ResultQualifiers`, populated from the first argument's resolved qualifier axes for `QualifierMatch.Same` overloads; assignment validation and proof resolution both consume the new surface.
- `ResolveDirectQualifierAxis(...)` now checks `Types.GetMeta(resultType).ImpliedQualifiers` after declared qualifiers, restoring the missing TypeChecker parity with the proof path.
- Added shared `QualifierUnitHelpers` so TypeChecker and ProofEngine stop maintaining separate compound-unit split/dimension derivation logic.
- Added regression coverage for `round(...)` qualifier preservation in assignment validation and proof checks; `TypeCheckerAssignmentQualifierTests` now pass 55/55.
- Full `dotnet test test/Precept.Tests/ --no-restore` remains on the branch baseline at 5642 passed / 9 failed / 5651 total; focused `ProofEngineTypedArgQualifierTests` still have the same pre-existing `CompoundUnitPositivityProof_ClearsDivisionByZero` failure.

### 2026-05-15T20:40:13Z — Assignment qualifier architecture closeout is recorded with Frank's rule and Soup Nazi's matrix

- Frank's decision is now canonical: assignment qualifier enforcement is per-axis proof, `PRE0068` / `PRE0069` stay for definite mismatches, and `PRE0141` is the assignment-stage signal for required-axis uncertainty.
- Soup Nazi's expanded `TypeCheckerAssignmentQualifierTests` matrix is the durable regression guard for the new resolver across price, money, quantity, and exchangerate assignment surfaces.
- Scribe merged the batch into `.squad/decisions.md`, cleared the inbox, and wrote the orchestration/session records for downstream consumers.

### 2026-05-15T14:55:25Z — Wave 2 follow-ups and the Slice N/M repair lane closed

- `01f255ab` locked regression coverage around the authored-vs-normalized bound split for both `TypedField` and `TypedArg`, and guarded `NormalizePrice` against affine-offset denominator units.
- `0837ad6f` narrowed bare-numeric quantity-bound PRE0018 suppression to the intended qualifier lane and added the missing negative tests for dimension-only and unqualified quantity bounds.
- `70ee2406` extended the same suppression to count-dimension fields through the existing `IsCountDimension` helper; Frank's final re-review approved Slices N and M.

## Learnings

- When a qualifier-preserving expression can know **some** axes but not all, store the partial resolved set instead of collapsing to all-or-nothing. `quantity of 'mass'` flowing through `round(...)` should preserve the known dimension while still leaving exact-unit checks unresolved.
- When a task's described implementation is "adding handling for X," verify whether the code is already generic/correct before restructuring. Slice 24's money/price handling was already working via the generic single-slot path from Slice 19. The real Slice 24 contribution was (a) making the Price+Magnitude+no-qualifier defensive Unbounded return explicit, (b) adding explanatory comments for each type path, and (c) adding the missing static-denominator price tests.
- For Price intervals with a magnitude slot, the split of responsibilities matters: `IntervalOfNarrowed` extracts the raw slot interval; `ApplyStaticUnitScaling` applies the `1/denominatorScale` factor. Documenting this contract explicitly in the code prevents future attempts to "fix" the lack of scaling in `IntervalOfNarrowed`.
- Stale `.msCoverageSourceRootsMapping_Precept.Tests` and `CoverletSourceRootsMapping_Precept.Tests` files in `test/Precept.Tests/bin/Release/net10.0/` cause transient build failures on first run after a rebuild. Clear them and re-run — they regenerate cleanly on the second attempt.

- Warning follow-ups should first verify whether the intended invariant is already present in shipped code and only missing regression locks.
- `IntervalContainmentProofRequirement.DeclaredMin/Max` carry **normalized** (proof-math) bounds — they are not authored values despite the name. Always add a parallel authored pair for display. Never use proof-math bounds in diagnostic messages.
- When adding fields to a proof requirement record, update every direct constructor call site in tests — grep for `new IntervalContainmentProofRequirement` before shipping.
- The WholeValue double-normalization guard in `ApplyStaticUnitScaling` is implicit: it relies on `HasSingleMagnitudeSlot` excluding `WholeValue`. Document this so future changes to that helper don't silently corrupt interval math.

### 2026-05-15T15:35:00Z — Slice 18: Display contract implemented

- `IntervalContainmentProofRequirement` gains `AuthoredMin`/`AuthoredMax` — raw authored values from `TypedField.DeclaredMin/Max`, used for diagnostic display only.
- Existing `DeclaredMin`/`DeclaredMax` on the requirement retain their normalized proof-math semantics unchanged.
- `NumericOverflow` diagnostic now uses `AuthoredMin ?? DeclaredMin` for the bound display string — humans see their authored values, not UCUM base-unit numbers.
- `CompileProofObligationDto` gains `NormalizedDeclaredMin`/`NormalizedDeclaredMax`; `DeclaredMin`/`DeclaredMax` now project authored values so MCP consumers get display-ready bounds by default.
- WholeValue double-normalization risk noted and written to `.squad/decisions/inbox/george-s18-wholeval-doublenorm.md` for Soup Nazi's Slice 17 test pass.
- 9 pre-existing branch failures unchanged; 44 MCP tests fully green; 5524 Precept.Tests pass.

### 2026-05-15T15:37:42Z — Slice 18 display contract approved

- The authored-vs-normalized bound split is now explicit: IntervalContainmentProofRequirement carries AuthoredMin/AuthoredMax for display while DeclaredMin/DeclaredMax remain normalized proof-math values.
- NumericOverflow now renders authored bounds, and MCP compile output adds NormalizedDeclaredMin/NormalizedDeclaredMax while keeping authored DeclaredMin/DeclaredMax consumer-facing by default.
- Frank approved commits 9c98d37b and 45b9dba2; remaining follow-ups are Slice 19 MCP cross-unit coverage and the deferred §5.2(b) hover/preview presentation work.

## Learnings

### 2026-05-15 — Slice 23: StaticQualifier routing

- `StaticQualifier` is only set on `InterpolatedTypedConstant` when no dynamic slot for that axis is present — `ResolveStaticQualifier` returns null if any `WholeValue` slot is present and returns null for an axis if a dynamic slot already covers it. This means early-returning from `StaticQualifier` is safe: it never shadows a runtime-field-sourced qualifier.
- `ResolveQualifierFromInterpolatedConstant` takes a single `QualifierAxis` and returns a single `DeclaredQualifierMeta?`. For `StaticCurrencyAndUnitQualifier`, the same static qualifier answers multiple axes — handle each axis in a separate switch arm.
- `TryGetAssignmentSourceQualifiers` returns `ImmutableArray<DeclaredQualifierMeta>` (a multi-qualifier set). The new `BuildQualifiersFromStaticInterpolated` helper maps all static qualifier subtypes to their full qualifier arrays.
- `IsDefaultOrEmpty` returns true for both uninitialized and empty arrays. The `_ => []` fallback in `BuildQualifiersFromStaticInterpolated` produces an empty (not default) array, so `!IsDefaultOrEmpty` correctly evaluates to false and falls through to the existing `default: return false` path.
- Pre-existing baseline was 9 failures before this slice; it remains 9 after. All 5 new tests pass. The `"Question build FAILED"` MSBuild error is a transient file-locking issue in the incremental build cache — a second `dotnet build` call always succeeds.
- QS-1 for price qualifiers is model-only: `QualifierAxis.PriceIn`, `QualifierShape.OfRequiresCurrencyIn`, and `DeclaredQualifierMeta.CompoundPrice` can land without changing `QS_CurrencyAndDimension` yet. Keeping the catalog slot on `Currency` until QS-2 avoids breaking existing price-field mapping before the new handler exists.
- Existing qualifier-axis consumers already fail soft through `_`/`default` fallbacks. For QS-1, no switch-exhaustion stubs were needed in `TypeChecker`, `CompletionHandler`, `RichHoverFactory`, or MCP formatting surfaces.
- Baseline validation for this lane remains the known 9 failing `Precept.Tests`; QS-1 preserved that count exactly (5561 passed / 9 failed / 5570 total).

### 2026-05-15T19:56:59Z — Price typed-constant qualifier enforcement fixed

- `TryGetAssignmentSourceQualifiers(...)` now trusts non-empty `TypedTypedConstant.DeclaredQualifiers` instead of re-deriving a money-only special case, so plain resolved `price` literals participate in assignment qualifier validation.
- Added regression coverage for price typed-constant set assignments covering count-unit mismatch, count-unit match, currency mismatch, plus a shared-seam field-default mismatch anchor.
- `dotnet build src/Precept/Precept.csproj` succeeded; focused `TypeCheckerAssignmentQualifierTests` passed 18/18.
- Full `dotnet test test/Precept.Tests/` stayed on the same branch baseline failure count while absorbing the new coverage: before 5594 passed / 9 failed / 5603 total, after 5598 passed / 9 failed / 5607 total.

### 2026-05-15T20:06:56Z — Interpolated unit-slot price qualifier enforcement fixed

- `TryGetAssignmentSourceQualifiers(...)` now has a slot-backed interpolated path for `InterpolatedTypedConstant` price forms with `StaticQualifier = null`, so `.unit` member access holes can surface their underlying `DeclaredQualifierMeta.Unit` during assignment validation.
- `InterpolatedTypedConstant` now retains concatenated static text, letting mixed static/dynamic price forms like `'4.17 USD/{qty.unit}'` preserve their static currency while the slot contributes the denominator unit.
- `ValidateUnitSlotDimensionConsistency(...)` now reuses the same helper that reaches through `TypedMemberAccess -> TypedFieldRef|TypedArgRef`, keeping the unit-slot structural seam single-sourced.
- Added regression coverage for interpolated count-unit mismatch, interpolated count-unit match, and interpolated currency mismatch; focused `TypeCheckerAssignmentQualifierTests` now pass 21/21.
- Full `dotnet test test/Precept.Tests/ --no-restore` remains on the same known branch baseline: 5601 passed / 9 failed / 5610 total.

### 2026-05-15T21:40:00Z — Assignment qualifier enforcement moved to the new axis-aware architecture

- Added `PRE0141 UnprovedAssignmentQualifierCompatibility` as the type-stage assignment companion to proof-stage `PRE0114`, and documented the split in `docs/compiler/diagnostic-system.md`.
- Replaced the old assignment-source bool/partial-array seam with `QualifierResolutionKind` (`Resolved` / `Unknown` / `Absent`) plus a dedicated per-axis resolver in `TypeChecker.Expressions.AssignmentQualifiers.cs`.
- The new assignment path now handles direct refs, typed constants, whole-value interpolation, qualifier slots, conditionals, and binary result policies, and it recovers raw-text qualifier facts for bare `money`, `price`, and `exchangerate` typed constants when resolved metadata is absent.
- Suppressed redundant assignment-time uncertainty on definite non-cancelling compound-unit multiplication so `CrossDimensionArithmetic` remains the primary error instead of piling on `PRE0141`.
- Updated stale regression expectations around the new architecture (`InterpolatedTypedConstant_SourceNoDimension_EmitsUnprovedAssignmentQualifierCompatibility`, `SetAction_NonCancellingCompoundUnitMultiplication_EmitsCrossDimensionArithmetic`) and synced analyzer/language-server tests that depended on old clean-compilation snippets.
- Validation closed green everywhere except the known branch baseline: `TypeCheckerAssignmentQualifierTests` 44/44, `Precept.Analyzers.Tests` 291/291, `Precept.LanguageServer.Tests` 305/305, `Precept.Tests` back to 5630 passed / 9 failed / 5639 total, and full-repo `dotnet test` at 6270 passed / 9 failed / 6279 total.

### 2026-05-15T22:09:58Z — Deferred qualifier fixes are now the canonical shipped contract

- Scribe merged George's deferred qualifier closeout into .squad/decisions.md and the batch logs.
- The durable surfaces are TypedFunctionCall.ResultQualifiers, TypeChecker implied-qualifier parity, shared QualifierUnitHelpers, and the explicit quantity-slot Unknown contract for assignment validation.
- Frank's earlier review still approves the PRE0141 architecture, and the focused assignment suite is locked green at 55/55.

### 2026-05-15T18:30:16-04:00 — D94 stateless gap and PRE0142 are now closed

- Stateless construction validation cannot piggyback on stateful transition-row logic alone; the current runtime represents stateless initial work in event handlers, so gap closures must inspect both stateless handlers and null-from-state rows when present.
- `PRE0094` and `PRE0142` solve different failure modes and both matter: one catches missing construction writes, the other catches self-referential reads of a value that still does not exist.
- The diagnostic coverage analyzers still need Gate 2 allow-list entries for newly emitted codes referenced only from `test/Precept.Tests/`; otherwise PRECEPT0028 fires even when runtime tests exist.
- Focused construction/diagnostic suites are green; full `Precept.Tests` remains at the existing 9 unrelated proof/quantity failures.

### 2026-05-15T18:51:51.086-04:00 — Construction audit follow-up closed

- Stateful construction validation has to carry the set of covered initial states per action chain. Wildcard `from any` rows are not state-less shortcuts; they owe assignment coverage for every initial state they cover, while explicit rows only owe fields that are present in their concrete source state.
- Initial-event undefined-read validation is an ordering problem, not just a self-match problem. The read set must include `SecondaryExpression`, and cross-field reads must compare against first-assignment order in the same action chain.
- Entry materialization self-reference needs transitive dependency inspection through computed fields. Direct field-ref scans are insufficient because helper computed expressions can route back to the field being materialized even when the RHS never names it directly.

