## Core Context

- Owns code-level feasibility, parser/runtime implementation detail, and architecture-to-code translation across checker, analyzer, proof, and tooling surfaces.
- Parser and checker changes stay catalog-derived, array-primary where order matters, and hostile to mirrored duplicate state.
- Shared-environment discipline still applies: validate surgically, stage exact paths only, and preserve durable regression anchors when semantics move.

## Live Guidance

- Construction status is semantic: `event ... initial` is the source of truth, while authored rows stay `on <Event> -> ...`.
- Construction handlers do not create graph edges; PRE0081 and `GraphEvent.IsInitial` must stay event-metadata-driven rather than topological.
- Guard narrowing still needs explicit handling whenever arg refs or interpolated typed constants introduce new proof lanes.
- Computed fields remain readable but not writable; any new action surface that mutates fields must preserve the `ComputedFieldNotWritable` lane.

## Historical Summary

- 2026-05-12 through 2026-05-16 closed the major constructor-semantics track: proof/checker slices landed, runtime `Create()` was spiked into the semantic model, construction rows became a DU-backed surface, and Slice 8b removed row-level `initial` syntax in favor of declaration metadata.
- Detailed chronology for the proof-engine, graph-analyzer, runtime, and reject-surface work now lives in `.squad\decisions.md`; this file keeps only durable posture plus the latest closeout.

## Recent Updates

### 2026-05-17T12:46:26Z — Compiler gaps + SyntaxReference closeout recorded

- Commit `57433ce1` closed the PRE0092/PRE0094 Pattern A construction-row gap, the PRE0038 computed-field write gap, and the PRE0010 chained-comparison parser gap; targeted regressions are green.
- The remaining full-suite failures are still the pre-existing `F5TempVerify` `UnsatisfiableInitialState` cases for `samples\parcel-locker-pickup.precept` and `samples\clinic-appointment-scheduling.precept`.
- Commit `0c3018a1` updated SyntaxReference wording, added the `omit`, `entry ensures`, and construction-row self-read guidance entries, and accurately recorded the then-open compiler-gap notes.
- Coordinator marked the compiler-gap and tooling slices done, then launched `george-8` and `george-9` to remove temporary gap notes now that the compiler fix exists.

### 2026-05-17T09:00:00Z — Slice E closes PRE0115 construction-row false positives

- `ProofEngine.Analysis.CheckInitialStateSatisfiability(...)` now short-circuits to a satisfiable result when the precept has a construction handler, with event metadata kept as the fallback source of truth for construction status.
- Added proof regressions for the Pattern A lane (unguarded initial-state ensure + construction row stays clean) and the Pattern B lane (no construction row still emits PRE0115).
- Validation closed green at `dotnet build src\Precept\Precept.csproj --nologo`, focused `ProofEngineConstructionTests|F5TempVerify`, and full `dotnet test test\Precept.Tests\ --no-build --nologo` (`5798/5798`).

## Learnings

- A cached semantic flag like `TypedEventRow.IsConstruction` is a convenience, not the only truth; downstream validation should re-derive from event metadata when the language guarantee depends on it.
- Non-associative operator metadata still needs a parser recovery branch if the goal is a precise user-facing diagnostic instead of a later type error.
- Structural guarantees that already exist in the language surface should be enforced as early as possible; defer only what truly requires later semantic knowledge.
