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

### 2026-05-14T17:10:32.283-04:00 — Exhaustive interpolated typed-constant audit widened the normalization track

- Audited `TypedInterpolatedTypedConstant` coverage against quantity-normalization Slices 19-21 and confirmed those slices fix the motivating bug but are **not** exhaustive.
- Confirmed five gap categories: bounded interpolated `set` false positives, qualifier-proof false positives, silent qualifier-mismatch acceptance in assignment/defaults, missing interpolated field-default proofing, and completely unwired event-arg defaults.
- Durable severity callout: three false-positive classes and one silent-wrong acceptance are already real; only fully dynamic qualifier text (`'{n} {u}'`-style forms) should remain conservative/unproved by design.
- Recommended the next planning set as slices 22-26: capture static interpolated qualifier metadata, route it through qualifier consumers, extend interval extraction beyond quantity, add field-default proof coverage, and decide event-arg default resolution.

### 2026-05-14T22:00:00Z — PRE0027 diagnosis: Test.precept revert recommended

- Frank investigated suspected PRE0027 (`DuplicateArgName`) errors. Result: **none exist anywhere** in the repository.
- The only error in `samples/Test.precept` is **PRE0078** (pre-existing), present before George's edit. George's change (`'6 [lb_av]'` → `'{test2} [lb_av]'`) changed proof shape but not error category.
- **Recommendation from Frank:** revert `samples/Test.precept` via `git checkout samples/Test.precept`. If interpolated-quantity test coverage is needed for normalization work, create a new sample file with satisfiable bounds.
- `test/Precept.Analyzers.Tests/AnalyzerTestHelper.cs` addition (`AnalyzeWithFilePathsAsync<TAnalyzer>()`) is clean, legitimate C# test infrastructure.

### 2026-05-14T22:00:00Z — Frank's conditions resolution + Slice 15b confirmed: event-arg bound normalization approved

- Frank resolved all six §5.5.6 conditions — the implementation gate for Slices 14–21 is cleared (pending Shane's sign-off).
- Key outcome for George: **Slice 15b** adds `NormalizedDeclaredMin/Max` to `TypedEventArg` (Option a). This is now a design-locked requirement, architecturally parallel to `TypedField`.
- Slices 22–26 have full §5.6 detail entries; George can reference those for implementation planning.



- Frank independently approved the normalization design with conditions and identified the same high-risk areas George's audit hit from the code side: `IntervalOf` scoping, normalized-field bound reads, normalized `StaticMagnitude`, and missing event-arg bound parity.
- Treat the combined George + Frank result as the current architectural baseline before any implementation slices are started.

## Learnings

- Typed interpolated typed-constant holes were bypassing presence-proof generation entirely; the fix is to recurse `TypedInterpolatedTypedConstant.Slots` through `WalkExpression` so optional field reads inside holes emit PRE0116 unless a guard proves presence. Verified with new proof-engine tests and with `samples/Test.precept`, which now reports `UnprovedPresenceRequirement` on line 14.
