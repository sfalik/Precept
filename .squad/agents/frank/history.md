## Core Context

- Owns the core DSL/runtime architecture across parser, type checker, diagnostics, graph analysis, and execution semantics.
- Protects cross-surface contract integrity across runtime, docs, MCP, and contributor workflow changes.
- Historical summary: led the combined compiler/runtime design consolidation, access-mode redesign, parser catalog-shape direction, catalog extensibility hardening, and the spike-mode operating model.

## Learnings

- Vision-to-spec migration works best as a substance-preserving transplant with minimal reframing.
- Pre-implementation contracts are clearer when the spec names stubs and responsibilities explicitly.
- Merge overlapping vision sources instead of restating the same rule in parallel sections.
- Catalog metadata should carry consumer-facing distinctions; hardcoded parallel copies drift.
- Parser algorithms may stay hand-written while vocabulary, precedence, and disambiguation data remain catalog-driven.
- Execution consumers read lowered `Precept`; authoring consumers read `CompilationResult`.
- Enum and construct-family changes require cross-surface verification: catalog entries, AST nodes, tests, routing, and regression anchors.
- Analyzer/spec verification must be done by spec ID and code path, not by test count alone.
- Spike mode only sticks when routing, ceremonies, and contributor workflow docs all enforce it together.
- Philosophy and guarantee language can lag implementation/spec reality; when that happens, flag the gap rather than silently rewriting philosophy.
- Collection design docs consolidate best when structured per-kind (set/queue/stack) with shared cross-cutting sections (inner types, emptiness safety, constraints) rather than per-concern, because the per-kind structure mirrors how authors encounter the surface in `.precept` files.

## Recent Updates

### 2026-04-29 — Reconciled duplicate collection-types research sections
- Removed frank-9's `§Proposed Collection Types` section (duplicate of frank-10's `§Proposed Additional Types`).
- Absorbed three rejected candidates (ring buffer, bounded collection, multimap) from frank-9 into frank-10's section as explicit `### Rejected:` subsections with full evaluation rationale.
- Added all three rejects to `### Priority Summary` table and ring buffer + multimap to `## Comparison With Other Collection Systems`.
- Added Open Question #10: temporal/business-domain types as collection inner types.
- Moved `## Cross-References` to document end (after comparison table).
- Updated Open Question #9 section reference from `§Proposed Collection Types` to `§Proposed Additional Types`.

### 2026-04-29 — Ordered-choice gaps closed and collection comparisons added
- Fixed the last three `choice(...) ordered` documentation gaps in `docs/language/collection-types.md`.
- Added `§ Proposed Additional Types` evaluating 6 candidates with priorities: `bag`, `log`, `map` high; `sortedset`, `priorityqueue` medium; `deque` low.
- Added `§ Comparison With Other Collection Systems`, mapping 14 capabilities across 9 ecosystems to ground future collection-surface decisions.
- Scribe merged the resulting decision note into `decisions.md` and cleared the inbox entry.
### 2026-04-29 — Collection research recorded durably
- Scribe logged frank-6 and frank-7, merged both collection research records into `decisions.md`, and summarized this history after the size gate tripped.

### 2026-04-29 — Collection type expansion research and ordered-choice fixes
- Fixed grammar production in collection-types.md: `choice(...)` → `choice(...) ordered?` to reflect the optional `ordered` modifier.
- Surveyed collection types across Java, Python, C#, Kotlin, Scala, Haskell, and domain-specific systems (Drools, DMN, Camunda, event sourcing).
- Evaluated 8 candidate types: priority queue, ordered set, multiset, deque, ring buffer, bounded collection, map, multimap.
- Recommended: reject 4 (sorted set, deque, ring buffer, bounded collection), defer 3 (priority queue, multiset, restricted map), reject 1 (multimap).
- Restricted `map of choice(...) to V` identified as the strongest future candidate — statically known key set enables proof engine reasoning.
- Ring buffer rejected on philosophy grounds — silent eviction violates inspectability principle.
- Added §Proposed Collection Types section and Open Question #9 to `docs/language/collection-types.md`.

### 2026-04-29 — Ordered-choice gap fixed + additional collection types researched
- Fixed three spots in `docs/language/collection-types.md` where `choice(...) ordered` was incorrectly treated as non-orderable: orderable definition, grammar production, and Ordering category assessment.
- Clarified that `ordered` as a field-level modifier on collection fields is a type error, but `ordered` on the inner `choice(...)` type is valid.
- Surveyed collection types across .NET, Java, Python, Rust, SQL, CEL, Zod/Valibot, and functional languages.
- Authored §Proposed Additional Types evaluating 6 candidates: `bag`, `log`, `map` (high priority), `sortedset`, `priorityqueue` (medium), `deque` (low).
- Authored §Comparison With Other Collection Systems reference table mapping 14 capabilities across 9 language ecosystems.
- Decision record filed to inbox for scribe merge.

### 2026-04-29 — Collection types design doc authored
- Created `docs/language/collection-types.md` as the canonical reference for the shipped collection surface (set/queue/stack) and proposed extensions (quantifiers, field constraints).
- Document follows `primitive-types.md` style: per-kind sections, action/accessor/constraint tables, emptiness safety with proof obligations, diagnostic codes, and cross-references.
- Proposed Extensions section synthesizes frank-6 (CEL quantifier research) and frank-7 (6-category collection rules taxonomy) into concrete proposals with 8 explicitly captured open questions for owner decision.
- Updated `docs/language/README.md` Documents table and Reading Order.

### 2026-04-29 — Collection-level rule design direction
- Surveyed 7 external systems, built a 6-category taxonomy, and mapped the categories onto concrete Precept business-rule pressure.
- Recommended a 3-layer rollout: field-level collection modifiers first (`unique`, collection `notempty`, `subset`, `disjoint`), quantifier predicates second, dedicated `check` blocks deferred.
- Provability boundary: cardinality and uniqueness are often static; element-shape and aggregate rules depend on quantifiers; ordering is hardest.

### 2026-04-29 — Collection iteration direction
- Studied CEL, OPA/Rego, and SQL precedents and recommended bounded quantifier predicates (`all`/`any`/`none`) as acceptable, but no general loops or `map`/`filter`/`reduce` yet.
- Key implementation note: `All` and `Any` already exist in the lexer; parser disambiguation can be positional.

### 2026-04-29 — Spec migration closeout
- Migrated the §0 Preamble and §3A Language Semantics into `docs/language/precept-language-spec.md`, archived the old vision doc, and swept cross-references.
- Locked the no-runtime-faults principle as "prove safety or emit diagnostics" and flagged the remaining owner-only philosophy wording gap around evaluation-fault prevention.

### 2026-04-29 — Vision/spec audit completed
- Audited the vision against the live spec, identifying philosophy-bearing content, semantic gaps, and two stale contradictions that informed the migration order.

### 2026-04-29 — PRECEPT0018 gate closed
- George's follow-up commit `e7a643d` added the three missing required PRECEPT0018 regression tests plus the two advisory visibility anchors, closing Frank's only blocking finding.

### 2026-04-28 — Prior closeout summary
- Locked spike mode as first-class squad workflow, closed the parser/catalog extensibility loop, completed the PRECEPT0018 review pass, and defined the vision-to-spec migration boundary for the next day's implementation work.

