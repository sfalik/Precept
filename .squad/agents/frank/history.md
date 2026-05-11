## Core Context

- Owns language research, spec wording, and cross-surface architecture documentation for the Precept DSL and runtime.
- Catalog metadata remains the language truth; runtime, tooling, and docs derive from durable catalog shape rather than enum-identity switches or parallel lists.
- Interpolation work must preserve compile-time guarantees first; plans that trade structural certainty for runtime validation are philosophically out of bounds.
- Constraint-propagation scope should stay concrete and bounded: one-hop semantic traces are acceptable, speculative provenance systems are not.

## Live Guidance

- String holes are out of scope for interpolated typed constants; typed-hole composition is the only path that preserves qualifier and proof reasoning.
- Slice 6 is a numeric proof strategy only. Qualifier, dimension, modifier, and presence obligations remain declaration-driven through the existing proof strategies.
- Single-hole whole-value forms (`'{x}'`) are part of Slice 6 scope and should flow through the same helper path as magnitude-slot proofs.
- LOE reviews should call out missing binder/tooling walk updates explicitly so checker and language-server behavior do not drift.

## Historical Summary

- Earlier May review, parser-gap, typed-literal, and catalog-audit detail was archived into `history-archive.md` during the 15 KB summarization pass.
- The full chronology remains in `.squad/decisions.md`; this file now keeps only live architectural guidance and the newest closeout state.

## Recent Updates

### 2026-05-11T20:03:33Z — Slice 6 closeout recorded
- frank-6 confirmed that Slice 6 stays numeric-only and that qualifier/dimension/presence propagation should not be added because S2/S5 already discharge those obligations from field declarations.
- The same review identified the only scoped gap: a single-hole whole-value interpolated constant should inherit numeric constraints from its source field.

### 2026-05-11T20:03:33Z — Plan patch merged
- frank-7 updated the Slice 6 plan to use `GetSlotSource`, raised the estimate to roughly 90 LOC / 10 tests, and documented the no-qualifier-propagation rationale directly in the plan.
- The compile-time guarantee ruling still stands above the plan: simplification-by-runtime-validation is rejected.
