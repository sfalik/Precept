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

### 2026-05-14T17:10:32.283-04:00 — Frank's architectural review set the guardrails for implementation

- Frank independently approved the normalization design with conditions and identified the same high-risk areas George's audit hit from the code side: `IntervalOf` scoping, normalized-field bound reads, normalized `StaticMagnitude`, and missing event-arg bound parity.
- Treat the combined George + Frank result as the current architectural baseline before any implementation slices are started.
