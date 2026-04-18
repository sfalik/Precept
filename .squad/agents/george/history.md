## Core Context

- Owns the core DSL/runtime: parser, type checker, diagnostics, graph analysis, and execution semantics.
- Protects the fire/update/inspect pipeline contract and keeps runtime behavior aligned with docs, tests, and MCP output.
- Historical summary (pre-2026-04-18): led feasibility analysis and implementation shaping for declaration guards, event hooks, computed fields, divisor-safety narrowing, and the temporal/NodaTime runtime model.

## Learnings

- Separate runtime feasibility from product philosophy; owner decisions should stay visible as owner decisions.
- Precision contracts have to be closed at the data-model boundary, not patched later in evaluators or tooling.
- Recompute-style features stay tractable only when insertion points are explicit across Fire, Update, and Inspect.
- Constraint/narrowing proofs should flow through one shared mechanism; special-case proof loops are a smell.
- Design-doc accuracy matters most around pipeline stages, precision boundaries, and where compile-time versus runtime enforcement happens.

## Recent Updates

### 2026-04-18 — Currency/Quantity/UOM runtime feasibility review
- Verdict: FEASIBLE-WITH-CAVEATS.
- Implementation is technically tractable, but blocked behind Issue #107 typed constants, Issue #115 decimal exactness, static registries, and a compound-value transport decision.
- `StaticValueKind` can absorb new kinds, but unit/currency metadata must ride alongside values rather than inside the enum.

### 2026-04-18 — Precision chain for duration-based arithmetic
- `Duration` storage is integer-backed, but `Duration.TotalHours` is `double` and therefore unacceptable for financial paths.
- The safe extraction route is decimal math over integer nanoseconds.
- Any compound arithmetic feature that touches money/quantity/price must treat Issue #115 as a prerequisite.

### 2026-04-18 — Cross-agent batch consolidation
- Team consensus aligns with the runtime gate: lock `maxplaces` behavior, cancellation/accessor semantics, compound-value transport, and Issue #115 before implementation begins.
- Frank's architectural blockers and Soup Nazi's test blockers are the same runtime risks viewed from different angles.
- Newman reduced the AI/MCP question to one contract decision: typed-string versus object transport for compound values. Runtime should not support both implicitly.
- Uncle Leo's design-stage conditions reinforce that normalization order and decimal exactness are part of the runtime contract, not documentation garnish.

### 2026-04-17 — Divisor-safety narrowing work
- Unified numeric-proof extraction across constraints, rules, ensures, and guards.
- Closed the dotted-key consistency gap for event-arg proofs and kept the proof system centered on shared narrowing markers.

### 2026-04-17 — Temporal design-doc review support
- Confirmed the major NodaTime alignment contradictions, DST-sensitive semantics, and serialization-shape questions that later surfaced again in the currency batch.
