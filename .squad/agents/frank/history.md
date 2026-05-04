## Core Context

- Owns language research, spec wording, and cross-surface architecture documentation for Precept's DSL and runtime.
- Durable baseline: catalogs remain the language truth; consumers derive behavior from metadata/shape instead of hardcoded enum identity or parallel lookup tables.
- Active runtime baseline: execution stays on PreceptValue, slot arrays, eager execution plans, and catalog-owned dispatch; public ingress/egress should expose stable CLR/JSON types rather than evaluator internals.
- Canonical docs state what the system is; squad records carry provenance, decision lineage, and working-status detail.

## Learnings

- When a prior patch pass reports "all N replacements applied, scan confirmed zero remaining," trust that scan and verify with grep rather than re-reading 859 lines. The review findings may predate the patch pass — always cross-reference the "already applied" record before acting on review items.

- The evaluator's slot-level working-copy model (clone once, mutate freely, donate on commit) applies fractally to collection backing arrays within slots. After first mutation, a collection backing is UNSHARED — subsequent mutations can go in-place. The CoW boundary belongs in the evaluator (ReferenceEquals check against original), not in CollectionActions. This refines "array in, array out" to "span in, count out" without breaking the architectural principles (no wrappers, no ownership in helpers, evaluator owns lifecycle).
- When evaluating alternative backing types for internal data structures, ArrayPool compatibility and type uniformity across the evaluation pipeline are architectural constraints that override ergonomics concerns in isolated internal methods. A `ref` helper accessor pattern (`static ref T Key(T[] arr, int i) => ref arr[i * 2]`) gives named-field readability without introducing a type boundary.
- Jagged arrays (`T[][]`) are never acceptable for hot-path paired data — N+1 heap objects, no spatial locality, no pooling. This is a "dead on arrival" pattern for evaluator internals.
- Wrapper types around dumb backing storage are an anti-pattern when the evaluator owns the mutation lifecycle and pool lifecycle. The "intelligence" belongs in the plan executor (static helpers), not in the data container. Wrapping and unwrapping for every mutation adds code without reducing complexity. Pool provenance tracking (rented vs committed) is incompatible with wrapper ownership.
- The consistency test for "should this logic live in a type?" is: does the evaluator use wrapper types for ANY other execution concern (guards, constraints, computed fields, scalar assignment)? If no, collections aren't special enough to break the pattern.
- Discovery answers belong on the descriptor or metadata object the consumer already holds, not behind a second lookup surface.
- For hotpath value types: struct wins when the type is short-lived, ≤ ~40 bytes, has no polymorphism requirement, and the API surface doesn't force boxing. The copy-cost crossover where heap allocation becomes cheaper is ~64–128 bytes, not 24. `QuantityValue` at 24 bytes (decimal + ref) is firmly struct territory.
- The dual-shape pattern (internal `PreceptValue` class for slot storage, external struct for API materialization) is correct for runtimes with a hard internal/external boundary. Different lifetimes require different shapes — don't collapse them.
- "Value or entity?" is the first question before choosing struct vs class. `QuantityValue` (an amount) is a value; `Unit` (an identity with catalog lifetime and interning) is an entity. Ontological classification precedes performance analysis.
- Stable public API surfaces should expose CLR/JSON interchange types; leaking internal evaluation machinery into public signatures turns internal refactors into breaking changes.
- When a design locks, immediately replace provisional wording in canonical docs and move the rationale/provenance back into squad records.
- Audit-gap reports go stale quickly in a fast-moving doc set; re-verify the canonical docs before trusting a rollup.
- Registration or dispatch behavior that varies by language member is metadata, not incidental code structure, and should live in the catalog system.
- Philosophy prose is strongest when it opens with ontological certainty about the compiled artifact rather than with mechanism descriptions.
- Documentation for runtime API tradeoffs must explicitly name the affected audience when the DSL itself still enforces the invariant at language-check time.

## Recent Activity

### 2026-05-04T22:33:30Z — Collection CoW and doc sync closeout
- Locked the collection multi-mutation protocol as **Option C-2**: the evaluator clones on first mutation only, then mutates the private backing in place; `CollectionActions` stays as static span-in/count-out helpers and the evaluator uses `ReferenceEquals` to detect whether the backing is still shared.
- Confirmed `docs/working/precept-collection-types-investigation.md` now carries the full collection design surface through §15, covering internal representation, scalability/lazy-load seams, static action helpers, the CoW protocol, and CLR API direction.
- Confirmed `docs/working/precept-value-types-investigation.md` is canonically current; only the ExchangeRate size note needed clarification that currency strings are interned ISO 4217 references.

### Historical summary through 2026-05-04T18:11:29-04:00
- Early 2026-05-04 work locked the collection architecture baseline: flat `PreceptValue[]` backings for all nine kinds, stride-2 layout for pair collections, no collection wrapper types, and a future `ICollectionBacking` seam only if scale ever forces lazy materialization.
- The same workstream also tightened the public CLR/API direction: stable external CLR/JSON shapes stay separate from evaluator internals, and canonical docs are patched immediately once a design stops being provisional.
- Use `.squad/decisions.md` for full per-decision provenance; keep `history.md` focused on durable operating context plus the newest closures.
