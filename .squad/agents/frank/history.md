## Core Context

- Owns architecture, language design, and final review gates across runtime, tooling, and documentation.
- Co-owns language research with George and keeps the product philosophy and DSL surface aligned with actual implementation and documentation.
- Historical summary (pre-2026-04-18): shaped declaration guards, event hooks, computed fields, verdict modifiers, modifier taxonomy, and the temporal/NodaTime design arc; consistently enforced same-pass doc sync and philosophy-gap escalation.

## Learnings

- Product-identity gaps belong to Shane; technical analysis should surface them explicitly rather than resolve them silently.
- DSL-surface changes must stay synchronized across docs, grammar, completions, MCP descriptions, and tests.
- Computed-field semantics are only coherent with one recomputation pass after mutations and before constraint evaluation.
- Temporal and compound-domain types must expose backing-library semantics honestly; hidden coercions and implied units create design debt immediately.
- Mixed-type arithmetic is where ambiguity surfaces first. Operator tables, accessor surfaces, and cancellation rules must be explicit before implementation starts.

## Recent Updates

### 2026-04-18 — Currency/Quantity/UOM design review
- Verdict: BLOCKED. Four blockers and ten good catches.
- Strongest confirmed decisions: seven-type taxonomy, Level A/B/C split, D11 no auto-convert across currencies, and the fixed-length intent behind D15.
- Blockers: D3/D14 period qualification reconciliation, multi-basis `period` × single-basis `price` cancellation, `maxplaces` auto-default contradiction, and undefined `.basis` / `.component` accessor semantics.

### 2026-04-18 — Shift-pay temporal type choice
- For actual-hours compensation and billing, `instant` is the correct field type. `localdatetime` subtraction underpays on DST fall-back nights and creates awkward mixed-component arithmetic.
- `zoneddatetime` remains a richer but heavier model; `instant` plus separate timezone data preserves correctness without adding a larger surface.

### 2026-04-18 — Duration cancellation guidance
- `duration` can participate in compound cancellation only for fixed-length denominators such as `hours`, `minutes`, and `seconds`.
- `days` and larger calendar-relative units must remain outside duration-based cancellation.
- Decimal-exact nanosecond extraction is mandatory; any `double` path is rejected for financial results.

### 2026-04-18 — Cross-agent batch consolidation
- Team outcome: do not start Issue #95 implementation until four contracts are closed: `maxplaces` default behavior, period accessor/cancellation semantics, compound-value serialization form, and Issue #115 decimal exactness.
- Newman's open contract is deliberately narrow: prefer a typed-string transport unless runtime object ingestion is intentionally redesigned.
- Soup Nazi's estimate remains the planning anchor: roughly 310 tests, with duration/days and chained cancellation as explicit blockers.
- Uncle Leo's conditions make validator normalization order and Issue #115 framing required design-doc fixes, not optional cleanup.

### 2026-04-17 — Temporal design-doc review
- Approved the overall temporal direction with one blocker on stale decision text and a clear grammar/completions/doc-sync checklist for implementation.
