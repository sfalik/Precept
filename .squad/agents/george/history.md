## Core Context

- Owns code-level feasibility, parser/runtime implementation detail, and architecture-to-code translation across checker, analyzer, and tooling surfaces.
- Parser and checker work stay catalog-derived, array-primary where order matters, and hostile to mirrored duplicate state.
- Shared-environment discipline still applies: validate surgically, stage exact paths only, and preserve durable regression anchors when changing semantics.

## Live Guidance

- Interval and proof work must use explicit authored bounds; `nonnegative` is proof-only and does not populate `DeclaredMin`.
- `quantity` remains a reserved DSL keyword in tests and samples; use names like `qty` instead.
- Guard narrowing only handles field-to-constant comparisons today; arg-ref and broader interpolated reasoning still need explicit implementation.
- `TypedInterpolatedTypedConstant` still needs careful treatment whenever qualifiers or static text are split across dynamic slots.
- Do not treat `PreceptValue` as decimal-only when planning quantity runtime work; quantity unit identity can ride the existing reference lane.

## Historical Summary

- 2026-05-12 and 2026-05-13 closed George's major proof/checker work: interval-proof slices 1–4 validated green, D93/D94 constructor guarantees landed, the Tokens/Types static-init crash was fixed, and the comma-list `StateTarget` program closed with re-review approval.
- 2026-05-15 concentrated on qualifier enforcement, construction-guarantee follow-ups, and targeted completion/proof seams. Detailed slice-by-slice chronology now lives in `.squad/decisions.md` and `history-archive.md`; this file keeps only the guidance and latest durable outcomes.

## Recent Updates

### 2026-07-18 — Slice 8b: Removed `initial` from construction row syntax

- **Design:** `on <name> initial -> ...` → `on <name> -> ...`. Construction classification moves from parser-time to type-check-time via `resolvedEvent?.IsInitial ?? false`.
- **Catalog change:** `EventRow` gains `SlotPreVerbGuardArrow` (now 3 slots). `ConstructionRow` and `ConstructionRowReject` get `Entries: []` — no longer produced by direct parser dispatch. `ResolveRejectVariant` extended: `EventRow → ConstructionRowReject`.
- **PRE0014 retired:** Guard gate for `EventRow` removed from Parser.cs — guards are now valid on all `on`-rows. Added to Gate1 allow-list.
- **GraphAnalyzer PRE0081 false positive fixed:** Initial events handled via `EventHandlers` (construction rows) were falsely emitting `UnhandledEvent` because they don't generate graph edges. Added EventHandlers guard in the loop.
- **GraphEvent.IsInitial fixed:** Changed from edge-based topology detection to `evt.IsInitial` semantic flag. An event is initial iff it has the `initial` modifier — not based on graph reachability from the initial state.
- **Diagnostics.cs:** Updated PRE0145–PRE0148 messages to remove `initial` from construction row syntax examples.
- **Test sweep:** 6 test files (DSL string sweep) + structural changes in 5 parser/catalog/analyzer test files. New test: `PRE0081_NotEmitted_InitialEventWithConstructionRow`.
- All 5,781 Precept.Tests + 364 LS Tests + 44 MCP Tests + 291 Analyzer Tests green.



- **Discovery:** `Precept.From()` was completely hollow (`new Precept()`) — storing no compilation data. Updated to store `SemanticIndex` so the runtime can read the event/state topology.
- **`EventOutcome.Created`** — added as `sealed record Created(Version Result, FiredArgs Args)` following the existing DU pattern (`Transitioned`, `Applied`, etc.).
- **`FiredArgs.Empty`** — added `public static FiredArgs Empty { get; } = new();` sentinel for no-arg construction events (needed since `FiredArgs` has a private constructor).
- **`Precept.Create()` spike-level** — iterates `EventHandlers` for rows where `IsConstruction && EventName == initialEvent.Name`; skips guarded rows (R4 deferred); returns `Created` on success row, `Rejected` on reject row, `Created` directly on the no-initial-event path.
- **Key DSL distinction**: construction rows (`on Event initial -> ...`) go into `EventHandlers` as `TypedEventRow{IsConstruction=true}`. Regular transition rows from the initial state (`from Draft on Event -> ...`) go into `TransitionRows`. Current spike only covers `EventHandlers`. The `from State on InitialEvent -> ...` form is valid but not yet runtime-evaluated (TODO R4).
- **`Precept.Events` / `InitialEvent` / `InitialState`** — implemented using `SemanticIndex`; `BuildEventDescriptor` converts `TypedEvent` to `EventDescriptor` setting `ModifierKind.InitialEvent` modifier.
- **`Version.AvailableEvents`** — filters `Precept.Events` to exclude `ModifierKind.InitialEvent` entries; delegates to `Precept.Events` rather than duplicating lookup.
- **`Version.Fire()` fire-once** — checks `Precept.IsInitialEvent(eventName)`; returns `Rejected` to block post-construction firing of initial events.
- **Test DSL gotcha**: an unconditional construction reject row (`on E initial -> reject "msg"`) alone triggers `AlwaysRejecting` error. The reject test needs a guarded success row first to satisfy graph analysis, then an unguarded reject row which the spike picks up (guards are skipped).
- Created `test/Precept.Tests/Runtime/RuntimeConstructionTests.cs` (9 tests): all 8 named spec tests + `EventOutcome_Created_IsPatternMatchable`.
- Baseline 6,421 (pre-Slice 7 context) → 5,757 was the Precept.Tests count pre-Slice 8 → **5,764 after (+7 new)**. All green. Commit `d95fff84`.

### 2026-05-16 — Slice 7 Proof Engine construction row context complete

- The proof engine already had `EventHandlerContext(TypedEventRow Handler)` in `ProofLedger.cs` — no new context type was needed.
- Extended three guard-extraction switch expressions to handle `EventHandlerContext h => h.Handler.Guard`:
  - `TryGuardInPathProof` (Strategy 3) in `ProofEngine.Strategies.cs`
  - `TryFlowNarrowingProof` (Strategy 4) in `ProofEngine.Strategies.cs`
  - `BuildNarrowedIntervals` in `ProofEngine.Intervals.cs`
- Key discovery: construction row guards cannot reference regular fields — `ValidateConstructionGuardFieldAccess` in `TypeChecker.Validation.FieldState.cs` emits `ConstructionGuardReadsUninitializedField` for any field ref in a construction guard (fields are uninitialized at construction time). Test 2 uses `ProveAllowingDiagnostics` to test interval narrowing in isolation of this TypeChecker constraint.
- Guard narrowing for event arg refs in construction guards still requires explicit extension of `ExtractGuardLeafConstraints` (currently only handles `TypedFieldRef`, not `TypedArgRef` for numeric comparisons).
- Created `test/Precept.Tests/ProofEngine/ProofEngineConstructionTests.cs` (4 tests): guard extracts constraints, guard intervals correct, no-guard baseline, end-to-end integration.
- All 6,421 tests green (+4 new). Commit `f1eb3cab`.



- Added `RowSpan` (`required SourceSpan`) to `TypedEventRow` abstract record, mirroring the `TypedTransitionRow` pattern.
- Set `RowSpan = construct.Span` in `TypeChecker.NormalizeEventHandler` for both `TypedEventRowSuccess` and `TypedEventRowReject`.
- Extended `GraphAnalyzer.EmitAlwaysRejecting` with a second loop over `EventHandlers` filtered to `IsConstruction == true`, grouping by event name; if ALL rows for an event are `TypedEventRowReject`, emits `AlwaysRejecting` with `Severity.Error` (overriding catalog default via `with { Severity = Severity.Error }`).
- Created `test/Precept.Tests/GraphAnalyzer/GraphAnalyzerConstructionTests.cs` (4 tests): all-reject construction path → Error, mixed path → no diagnostic, transition row → Warning, reachability inclusion.
- Fixed `ProofLedgerTests.cs` `CreateEventHandler` to supply `RowSpan = SourceSpan.Missing` for new required property.
- All 6,407 tests green. Commit `1e1d109a`.

### 2026-07-17 — Slice 3 Semantic Model DU complete

- Converted `TypedTransitionRow` and `TypedEventHandler` to discriminated unions (abstract base + Success + Reject subtypes).
- Renamed `TypedEventHandler` → `TypedEventRow` for naming consistency.
- TypeChecker now emits correct subtypes based on `ConstructKind` (TransitionRowReject, ConstructionRow, ConstructionRowReject).
- Fixed 5 parser tests broken by Slice 2's reject routing — `TransitionRowReject` constructs use `RejectClauseSlot`, not `OutcomeSlot`.
- All 6,360 tests green. Commit `7c49f9c7`.

## Learnings

- DU refactors that touch abstract base properties (`Outcome`) need careful downstream mapping — keeping computed properties on the base (dispatching to subtypes) minimizes callsite churn.
- Slice 2 parser routing changes (reject → `TransitionRowReject` construct kind) had test-level consequences that weren't caught at Slice 2 time. Test fixups belong to the slice that surfaces the failure.
- `required` keyword on record properties is the right pattern when abstract bases can't use positional syntax.
- Slice 4 structural checks must recognize construction rows without breaking the still-shipped declared-initial + transition-row path used by D93/D94 coverage; construction-specific diagnostics need to key off the new `IsConstruction` lane rather than blanket-banning legacy initial-event flows.

### 2026-05-15T23:59:59Z — Deferred test-9 closeout reported green

- George closed the remaining quantity-bound red test after Frank's spec, recorded implementation commit `d68eb6bc`, and reported `5699/5699` passing across the validation run.
- Durable seam: typed-constant validator codes must survive emission when the qualifier text is concrete, and quantity assignment qualifier checks remain regression-sensitive whenever typed constants are special-cased.


### 2026-05-15T23:26:25Z — Pairwise qualifier repair lane reduced the branch from 10 failing tests to 1

- Commit `a03fcf4e` implemented Frank's three-root-cause fix spec for the remaining pre-existing failure cluster.
- Shipped fixes: added the missing `time` dimension alias, narrowed compound-operation pairwise qualifier suppression so real cancellation/elevation paths skip eager PRE0070/PRE0071 checks without hiding definite non-cancelling errors, limited same-qualifier proof deferral to non-field-expression contexts, and taught the proof path to treat zero-magnitude interpolated typed constants with dynamic unit slots as a usable numeric zero for positivity clearing.
- Validation moved `test/Precept.Tests` from 10 failures to 1 remaining intentional red test: `QuantityBound_CrossDimensionAssignment_IsBlockedByDimensionCheck`.

### 2026-05-15T23:14:11Z — Deferred qualifier follow-up lane is durably closed

- George's follow-up commits closed the PRE0141 review notes: `TypedFunctionCall.ResultQualifiers`, implied-qualifier parity in `ResolveDirectQualifierAxis`, shared `QualifierUnitHelpers`, and the slot-hole `Unknown` fallback now form the canonical shipped seam.
- Frank kept the architecture APPROVED after review; Soup Nazi's N3/N4 regression coverage locked the follow-up behavior into direct tests.

### 2026-05-15T18:51:51Z — Construction guarantee follow-up closed

- Stateful construction validation must account for every initial state covered by a wildcard `from any` row, not just explicit source-state rows.
- Ordered undefined-read checking now covers `SecondaryExpression`, cross-field reads, and omit→present materialization paths that recurse through computed-field dependencies.
- The resulting shipped diagnostics are `PRE0142`, `PRE0143`, and `PRE0144`, each guarding a distinct "read before the language established a value" seam.

## Learnings

- When a qualifier-preserving expression can know some axes but not all, store the partial resolved set instead of collapsing to all-or-nothing.
- Warning follow-ups should first verify whether the intended invariant is already present in shipped code and only missing regression locks.
- `IntervalContainmentProofRequirement.DeclaredMin/Max` remain normalized proof-math bounds even though their names read like authored values; display surfaces need parallel authored fields.
- For compound quantity operations, eager pairwise mismatch diagnostics must respect whether the operation's catalog semantics resolve qualifiers structurally or defer them to proof requirements.
- Typed-constant validator codes can safely promote to `DiagnosticCode` only when the declared qualifiers are concrete; interpolated qualifier text must keep the generic catalog fallback to avoid false `DimensionCategoryMismatch` emissions.
- Slice 5 field-state work must keep the stateless initial-event handler lane (`event Start initial` + `on Start -> ...`) in the same construction-chain helper as stateful construction rows, or D94/D142/D144 regress immediately.
- New emitted diagnostics still need a `Precept.Analyzers` Gate 2 allow-list entry even when the real coverage lives in `test/Precept.Tests/`; PRECEPT0028 cannot see cross-project test references.

### 2026-05-16T13:08:43Z — Constructor semantics downstream closeout recorded

- Scribe recorded George's Slice 8b completion (commit `c72db9b0`) as the semantic cutover that made `initial` declaration-only and construction rows uniformly `on <Event> -> ...`.
- Kramer finished the downstream language-server and grammar work at `ec5525d2` and `e19736f6`, Newman finished the MCP `isConstruction` surface, and Frank closed docs/sample verification.
- Durable batch outcome: construction semantics are now aligned across parser/checker, graph analysis, tooling, MCP, docs, and the canonical sample.

