## Core Context

- Owns the core DSL/runtime architecture across parser, type checker, diagnostics, graph analysis, and execution semantics.
- Protects cross-surface contract integrity across runtime, docs, MCP, and contributor workflow changes.
- Historical summary: led the combined compiler/runtime design consolidation, proof/fault boundary hardening, catalog-consumer drift analysis, and canonical design-doc promotion work.

## Learnings

- D3's closed-world access default is a structural safety property. `write` opens exceptions; inverting to write-by-default weakens auditability and computed-field clarity.
- Runtime boundaries should be described by dependency direction, not by pretending no analysis knowledge crosses lowering. Lowered runtime-native descriptors may carry selected compile-time residue.
- Catalog completeness is no longer the main bottleneck; consumer drift is. Highest-value follow-up is removing hardcoded language knowledge from checker, LS, MCP, and tooling consumers.
- Parser and lexer algorithms should stay hand-written, but vocabulary tables, precedence data, and classification sets should derive from catalog metadata wherever the catalog is the language truth.
- Authoring consumers read `CompilationResult`; execution and preview consumers read lowered `Precept`. Constraint plans and proof/fault behavior are sibling contracts, not one blended validation layer.
- Runtime resolved-value enums (for example `FieldAccessMode { Read, Write }`) are outputs of compilation, not declaration-surface vocabulary. Do not collapse modifier docs into runtime descriptor docs.
- For language-surface doc audits, the update set is predictable: token catalog, modifier catalog, parser/type-checker docs, diagnostic catalog, language spec/vision, and evaluator prose; historical working docs and runtime result-shape docs change only when their own contract changes.
- Clean-slate parser work changes the calculus: richer catalog-driven dispatch and routing are viable when the parser is still a stub, but semantic meaning and downstream type safety remain irreducibly hand-authored.

## Recent Updates

### 2026-04-27 — `writable` field modifier audit and review
- Audited all 32 docs files for the `writable` language change. Locked the two-layer access model: field-level `writable` baseline plus state-scoped `write|read|omit` overrides, with state-level rules winning per field/state pair.
- Confirmed compile-time-only `WritableOnEventArg`, preserved root `write all` for stateless precepts, and recorded which documentation surfaces must change when modifiers are added.
- Review verdict stayed blocked on one real catalog issue (`AccessMode.LeadingToken`) plus a few stale doc references.

### 2026-04-27 — Catalog-driven parser design loop
- Round 1 established the full-vision parser shape: `DisambiguationEntry`, generic disambiguation, generic slot iteration, and generator-ready architecture.
- Round 3 resolved George's six flagged items: accept `LeadingTokenSlot`, keep `BuildNode` as an exhaustive switch, apply peek-before-consume for `ActionChain`, allow both `when` guard positions, keep disambiguation tokens explicit, and sequence the catalog migration behind a `PrimaryLeadingToken` bridge.
- Extensibility outcome: catalog-driven parsing removes most parser-layer glue for new constructs, but generic AST and AST-as-catalog-tree were rejected; source generation stays deferred until construct count or consumers justify the infrastructure.

### 2026-04-28 — Combined design boundary/philosophy revision
- Corrected the overclaim that "nothing crosses the boundary" and recentered the main design doc on Precept's philosophy and guarantee.
- Locked the real rule: type dependency direction stays one-way, while lowered runtime-native shapes may intentionally preserve selected analysis knowledge.
