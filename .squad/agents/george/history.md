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

### 2026-05-15T14:55:25Z — Wave 2 follow-ups and the Slice N/M repair lane closed

- `01f255ab` locked regression coverage around the authored-vs-normalized bound split for both `TypedField` and `TypedArg`, and guarded `NormalizePrice` against affine-offset denominator units.
- `0837ad6f` narrowed bare-numeric quantity-bound PRE0018 suppression to the intended qualifier lane and added the missing negative tests for dimension-only and unqualified quantity bounds.
- `70ee2406` extended the same suppression to count-dimension fields through the existing `IsCountDimension` helper; Frank's final re-review approved Slices N and M.

## Learnings

- When a suppression hands off to a more specific diagnostic, re-run every dependent lane before closing the fix: unit-qualified, count-dimension, non-count dimension, and unqualified quantity cases each exercise a different downstream rule.
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
