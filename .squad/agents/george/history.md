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

### 2026-05-17T18:06:33Z — Constructor semantics finish line recorded

- Commit `6635ec95` removed row-level `initial`, keeping construction classification on event metadata and type checking.
- Commit `e2b1c375` removed the stale PRE0092 gap notes from `SyntaxReference.cs`.
- Commit `ce16e69b` closed Slice E by exempting construction rows from PRE0115; proof regressions and the full `Precept.Tests` suite finished green at `5798/5798`.

## Learnings

- A cached semantic flag like `TypedEventRow.IsConstruction` is a convenience, not the only truth; downstream validation should re-derive from event metadata when the language guarantee depends on it.
- Non-associative operator metadata still needs a parser recovery branch if the goal is a precise user-facing diagnostic instead of a later type error.
- Structural guarantees that already exist in the language surface should be enforced as early as possible; defer only what truly requires later semantic knowledge.

### 2026-05-17T18:12:43Z — Event declaration regression closeout

- Verified the parser/source build accepts `event create initial, start, stop, reset` as four event entries with `initial` bound only to `create`; the remaining repo drift was the sample/test workaround rather than a fresh parser delta in `Parser.cs`.
- Kept `samples\Test.precept` on the canonical single-line declaration, strengthened parser regression coverage to the exact four-event form, and added a sample-level parser assertion so the restored syntax stays locked.
- Re-enabled `samples\Test.precept` in `F5TempVerify` so the temporary full-sample compiler sweep now exercises this case instead of silently excluding it.

### 2026-07-06T20:00:00Z — Band-enforcement operation: runtime-canon findings (Slice D, brief-D)

## Learnings

- **`ConstraintsFailed` scope is rules + ensures ONLY — never declared bands.** `ConstraintKind` (`ConstraintKind.cs:9-24`) has exactly five members: `Invariant` (rule), `StateResident`/`StateEntry`/`StateExit` (ensures), `EventPrecondition` (event ensure). A declared band (`min`/`max`/`minlength`/`maxlength`/`mincount`/`maxcount`) is NOT a `ConstraintKind`, never becomes a `ConstraintDescriptor` (which requires an `ExpressionText`+`Because` a band lacks, `SharedTypes.cs:42`), and therefore never enters the §7.6 runtime constraint sweep (`evaluator.md:1326`, `:1695-1698` "Every ConstraintDescriptor appears in exactly one bucket"). `result-types.md:114/:121` confirm `ConstraintsFailed` covers "global rules, state ensures, AND event ensures" — undifferentiated, no band channel, no provenance tag (Q4a: there is one governance path and bands aren't on it).
- **Internal-band deferral is NOT spec'd anywhere in the runtime canon.** Declared bands are compile-time prove-or-reject: a computation result exceeding a bound is `NumericOverflow` at the Proof stage (`diagnostic-system.md:163`); the proof ledger explicitly does NOT cross the compile-runtime boundary — "only `FaultSiteDescriptor` residue (defense-in-depth backstops) crosses into runtime" (`proof-engine.md:107`). The only runtime residue of a band is `FaultCode.OutOfRange` (`FaultCode.cs:47-48`), a `[StaticallyPreventable]` backstop the canon says is "reachable only for out-of-contract data … a fault on contract data would indicate a proof-engine gap — a defect to fix, not a condition to design around" (`fault-system.md:280,316`).
- **The "deferred" seam in evaluator.md is lazy collection loading, not band deferral.** `evaluator.md:1280` `ICollectionBacking` is explicitly "DEFERRED — do NOT implement … Ship PreceptValue[] first" for large-collection materialization. Unrelated to band enforcement. No band-deferral job exists in the canon.
- **The commit pipeline is fully stubbed at HEAD.** `Version.Fire` returns `UndefinedEvent()` (`Version.cs:81`), `Version.Update` throws `NotImplementedException` (`Version.cs:85`), `Precept.Constraints` throws `NotImplementedException` (`Precept.cs:158`), `Evaluator.cs:46` "TODO: implement Fire/Update once the executable model is designed" (D8/R4). §7.6 `EvaluateFireConstraints` is design-doc pseudocode, not shipped.
- **Hard finding (Q4):** The runtime CANNOT reliably enforce a deferred internal declared band — no mechanism, designed or shipped. Model A's "defer to runtime" escape hatch is a false promise at the enforcement layer → leans B/hybrid. The shipped/designed reality (compile-time prove-or-reject for bands; runtime governance for rules/ensures + §0.7:268 ingress) IS already the hybrid boundary. §0.7:266 "no deferral" is consistent with the runtime canon and contradicts any assumption that D2's "deferred to runtime" has a landing zone.

### 2026-07-14T22:35:00Z — Posture-v2 independent reconstruction and sign-off check

- Produced `docs/Working/posture-v2-support/blind-reconstruction-2026-07-14.md`, rebuilding the posture from raw sources only and broadly converging with Frank's v1.
- Returned in a same-session follow-up to author `docs/Working/posture-v2-support/independent-check-2026-07-14.md`, checking Frank's v2 against all five gathering-phase inputs.
- Final verdict: `docs/Working/frank-go-forward-posture-replay-2026-07-14-v2.md` is ready for Shane's sign-off except for owner-gated item M3, where the separate canonical-capture recommendation still needs Shane's correction on the overruled “proof-carrying vs. not” framing.
