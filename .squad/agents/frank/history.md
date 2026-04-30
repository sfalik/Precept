## Core Context

- Owns the core DSL/runtime architecture across parser, type checker, diagnostics, graph analysis, and execution semantics.
- Protects cross-surface contract integrity across runtime, docs, MCP, and contributor workflow changes.
- Historical summary: led the combined compiler/runtime design consolidation, access-mode redesign, parser catalog-shape direction, catalog extensibility hardening, and the spike-mode operating model.

## Learnings

- `list of T` was added to `collection-types.md` as Candidate 3 (Low priority), inserted after `bag of T` and before `deque of T`. The four required touchpoints were: (1) new Candidate 3 section with full evaluation; (2) renumbered existing Candidates 3–6 to 4–7; (3) `log of T` section annotated with a deliberate-overlap note on `.at(N)` / `.first` / `.last`; (4) Priority Summary table and comparison table both extended. The key architectural rationale: `list`'s incremental territory over `log` is narrow — arbitrary positional removal — while the proof cost (position-shift on mutation) is real. Correct sequencing is to evaluate after `log` ships to see whether `.at(N)` on an append-only log satisfies real positional-read needs.

- `list of T` (ordered, duplicates-allowed, index-accessible) was an oversight in the collection-types research — not explicitly evaluated, not explicitly rejected. The comparison table covered 14 capability rows but omitted "Ordered sequence with random access" entirely. Python's `list` was mapped to the LIFO stack row and the append-only log row, but not to its primary role. There was no principled reason for the omission; the research simply never stopped on the base concept.
- On evaluation, `list of T` earns Low priority. The most valuable part — ordered insertion + positional read via `.at(N)` — is already covered by `log of T`'s candidate write-up, which includes `.at(N)`, `.first`, `.last`. The incremental territory `list` adds over `log` is arbitrary positional removal, which is the semantically weaker commitment and the proof-harder operation.
- `.at(N)` on a mutable list introduces a two-sided index-bounds proof obligation (`N >= 0 and N < F.count`) that is tractable via guard patterns. The harder problem is `insert(index, element)` and `remove-at(index)`: these shift subsequent positions, making positional invariants unstable across mutations. The proof engine cannot verify "element at position N is X after this mutation sequence" without full history tracking — a design assumption Precept's proof engine does not make. This is a cost, not a fatal flaw.
- `list` does not render `bag` or `log` redundant. `bag` is about element frequencies (unordered); `log` is about immutable ordered history; `list` is about mutable ordered positions. They address orthogonal domain dimensions.
- Decision: add `list of T` to `collection-types.md` as a Low priority candidate, add the missing "Ordered sequence with random access" row to the comparison table, and defer full evaluation until after `log` ships to see whether `.at(N)` on `log` satisfies real positional-read needs. Filed to `.squad/decisions/inbox/frank-list-candidate.md`.

- The `ordered` modifier on `choice(...)` governs **element-type comparability** (enables `<`/`>` comparison and powers `.min`/`.max` validity), not collection storage order. These are distinct semantic levels: inner-type comparability vs collection storage invariant. A keyword that spans both levels without syntactic disambiguation is a grammar landmine.
- Collection-level `ordered` on `set of T` is ambiguous between sorted storage (`SortedSet<T>`) and stable insertion order (`LinkedHashSet<T>`). Neither interpretation is derivable from the syntax alone. Named types resolve this unambiguously.
- `set of choice(...) ordered` already occupies the trailing-`ordered` grammar position. Permitting a second collection-level `ordered` in the same trailing position requires either positional disambiguation (fragile, invisible to authors) or the nightmare double-modifier form `set of choice(...) ordered ordered`.
- Static type systems are unanimous: named types (`SortedSet<T>`, `TreeSet`, `sortedSetOf()`) carry storage-semantic differences, not modifiers. The modifier approach belongs to query-time ordering (SQL `ORDER BY`), which does not transfer to declaration-time type design.
- Collection-level `ordered` generalizes badly: it creates semantic collision or vocabulary noise on `queue`, `stack`, `priorityqueue`, and `deque`. `sortedset` as a named type has no such bleed.
- Verdict filed: `sortedset` as a named type is correct; `ordered` stays scoped to `choice(...)` inner types. Decision record: `.squad/decisions/inbox/frank-ordered-modifier-vs-sortedset.md`.
- When a grammar collision objection is resolved (by changing the keyword), the remaining structural objections must be re-evaluated on their own merits — not assumed to have been answered. The `sorted` counter-proposal resolved the `ordered` collision cleanly; the category-break and type-discriminator objections survived the keyword swap unchanged.
- A modifier is a storage directive when its removal changes the runtime backing data structure rather than narrowing the value domain or governing access. Storage directives belong in type names, not modifier positions. `sortedset` is the correct vehicle; `sorted` is not.
- The "only valid on one collection kind" test is a reliable heuristic for identifying type discriminators masquerading as modifiers. Modifiers that apply uniformly across collection kinds (`notempty`, `mincount`, `maxcount`) are genuine modifiers. Modifiers that are valid on `set` but semantically incoherent on `queue` and `stack` are revealing type-level information and belong in a named type.
- Shane's modifier-heavy principle is valid when attributes are orthogonal to type identity. It does not apply when the attribute selects an implementation variant (hash-backed vs tree-backed). The principle governs qualification; it does not govern variant selection.
- Decision record filed: `.squad/decisions/inbox/frank-sorted-modifier-proposal.md`.

- In the quantifier family `each`/`any`/`no`, all three words are English determiners — the same word class, the same syntactic role before a bare noun. `none` is a pronoun and does not fit this grammatical family. Word class consistency matters for readability and is the primary test for this kind of keyword choice.
- The `No` token's dual role (`no transition` outcome and quantifier `no`) is not a parsing ambiguity. A single token of lookahead after `No` resolves the production: `Transition` keyword → outcome; identifier → quantifier. Dual-role tokens are acceptable when the grammar productions are unambiguous.
- Alloy (the canonical formal specification language for set/relation predicates) uses `no` as the negated existential quantifier. It faced the same design choice and made the same decision.
- When quantifier keywords advance to implementation, the spec keyword table for `No` under Keywords: Outcomes must have its context column expanded to document both roles.

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
- When a design decision is pending owner sign-off, use an inline blockquote callout in the doc rather than leaving the old notation or silently adopting the new one; the callout keeps readers honest about what is and isn't settled.

- The "only valid on exactly one collection kind" test was stated too strongly as the kill-shot for `sorted`. Shane correctly identified that `sorted` generalizes to `list` (sorted bag — sorted, duplicates allowed). The precise test is: "does the modifier change the operation surface or behavioral contract of the target type, rather than constraining the values it accepts?" `sorted` on `list` removes `insert at index` and `remove-at` (position is determined by value, not author). `sorted` on `set` changes from hash-backed to tree-backed with different iteration semantics. Both fail the correct test. Verdict unchanged; argument sharpened.
- The distinction between `ordered` on `choice(...)` (semantic enrichment) and `sorted` on `set of T` (storage directive) is real and defensible when stated precisely. `ordered` ADDS a comparison capability that did not exist — before `ordered`, `"low" < "medium"` is a type error. `sorted` USES a comparison capability that already exists — before `sorted`, `set of integer` can already call `.min`/`.max`. `ordered` changes what the type system KNOWS; `sorted` changes how the runtime STORES. The prior explanation was correct but too imprecisely stated to withstand Shane's challenge.
- `sorted list` = sorted bag (sorted, duplicates allowed). Python's `sortedcontainers.SortedList` is this type. It is coherent and exists in practice. It would require a named type if and when the need arises — not a modifier on `list`.
- The unified-bag model (`set = bag of T unique`) is rejected for these reasons: (1) `set` is shipped and canonical — you don't retroactively make a shipped type a modifier-qualified variant of an unshipped proposal; (2) the common case (`set`-style membership) would require a modifier in the unified model, which is backwards ergonomics; (3) `bag`'s primary accessor `.countof(element)` is degenerate on `bag of T unique` (always 0 or 1), signaling a design mismatch; (4) `unique` on `bag` changes the operation surface of `add` (from increment-count to no-op-if-present), making it a type-level distinction rather than a value constraint.
- `bag` is the correct keyword for the multiset/frequency-tracking collection type. Alternatives (`multiset`, `tally`, `inventory`, `counter`, `quantities`) are all worse — either more jargon, domain-specific, grammatically awkward, or already claimed. The documentation must explain `bag`'s business-facing meaning on first use: "bag = an unordered collection that tracks how many of each thing you have."
- The `set`/`bag`/`list` as "base types" / `queue`/`stack`/`priorityqueue`/`log`/`map` as "semantic variants" framing is partially right but wrong in spirit. In Precept, the type name IS the contract. `queue` is not "list with FIFO constraint" — FIFO is the identity of the type, not a restriction applied to a more general type. Calling sequenced-access types "variants of list" would create pressure for modifier-based variants (`list of T fifo`) that is exactly what named types prevent. Correct Precept taxonomy: membership (`set`, `bag`), sequenced access (`queue`, `stack`, `log`, `priorityqueue`), sorted membership (`sortedset`), indexed mutable sequence (`list`), associative (`map`).
- Decision record: `.squad/decisions/inbox/frank-collection-surface-reeval.md`.

- `sortedset of T` is rejected. The challenge was Shane's: "Why would iteration order matter? There are no loops in Precept." The analysis confirms this completely. Every Precept construct — quantifiers (`each`/`any`/`no`), `.min`/`.max`, `contains`, `.count` — is order-independent. No consumer can observe whether elements are stored in a hash or a tree. The `.min`/`.max` "always-safe" argument in the original candidate write-up was wrong about its own mechanism: `notempty` discharges the proof obligation, not sorted storage. `set of T notempty` is proof-identical to `sortedset of T notempty`. Declaration intent without an observable behavioral contract is noise, not signal. A type that is indistinguishable from another type by any language construct is an implementation detail, not a type. `sortedset` has been moved to Rejected in `collection-types.md` alongside `ringbuffer`, `bounded collection`, and `multimap`. The "Sorted membership" family in the collection taxonomy becomes an empty family — if an ordered-iteration construct ever arrives and makes sorted order observable, `sortedset` can be re-evaluated at that time. Decision record: `.squad/decisions/inbox/frank-sortedset-value-assessment.md`.
- Prior decisions about `sortedset` argued for it as a named type (vs `ordered`/`sorted` modifier). Those decisions were correct on the named-type-vs-modifier question but never challenged whether `sortedset` had observable value in the language at all. Shane's challenge went deeper. The modifier debate was a valid syntactic argument; the value argument exposes that the underlying semantic premise was wrong.

- The restriction "Collections of temporal or business-domain types are not currently supported" (collection-types.md line 238) is incidental, not principled. It is an incremental build artifact: collections shipped against the primitive vocabulary; temporal and business-domain types were designed later; `ScalarType` was never updated. No design decision or architecture note supports the exclusion. Every temporal and business-domain type satisfies the three collection inner type requirements: (1) single value — not a collection itself, (2) well-defined equality semantics, (3) not a nested collection. The restriction should be removed.
- Of the eight temporal types, five are fully orderable (`date`, `time`, `instant`, `duration`, `datetime`) — `<`/`>` supported, `.min`/`.max` valid. Three are equality-only: `period` (NodaTime deliberately omits `IComparable<Period>` because month length varies — structural equality only; `'1 month' != '30 days'`), `timezone` (identity type — IANA code), and `zoneddatetime` (NodaTime deliberately omits `IComparable<ZonedDateTime>` — ordering semantics are ambiguous by instant vs wall clock). All three equality-only temporal types follow the same pattern as `boolean` in the current `ScalarType` — valid inner type, `.min`/`.max` is a type error.
- Of the seven business-domain types, three are orderable within their qualifier scope: `money` (same currency), `quantity` (same dimension, auto-converts), `price` (same currency + unit). Four are equality-only: `currency` (identity type), `unitofmeasure` (identity type), `dimension` (identity type), `exchangerate` (explicitly: "exchange rates have no meaningful ordering outside their time context").
- The `~string` modifier has no parallel for temporal or business-domain types. `~` addresses case-insensitive string comparison. Temporal types have no concept of case. Business-domain types are case-normalized by the parsing layer before entering the type system (ISO 4217 enforces uppercase for currency codes; UCUM enforces lowercase for units). The `~` modifier is not applicable to any non-string type. This is correct and expected.
- Open `money`/`quantity`/`price` inner types (no `in`/`of` qualifier) in a set raise a design question: what does `.min`/`.max` mean on `set of money` when elements have mixed currencies? The answer is that `.min`/`.max` are type errors on open typed sets — same rule as cross-currency `money + money`. Using qualified inner types (`set of money in 'USD'`) restores orderability. This is a documentation constraint, not a barrier.
- `period` structural equality in a set is a documentation concern: authors may be surprised that `'1 month'` and `'30 days'` are different elements. This must be called out explicitly when the doc is updated.
- Open Question 8 in collection-types.md mentions `percentage` as a business-domain type. No such type exists in `business-domain-types.md`. This reference is either stale or refers to a type not yet designed. Flagged to Shane for resolution.
- Grammar for `in`/`of` in inner type position needs parser disambiguation design: `set of money in 'USD'` must parse as `set of (money in 'USD')`. The precedent from `set of choice(...) ordered` applies — the qualifier attaches to the inner type keyword, not to the collection kind. This is mechanical grammar design, not a semantic barrier.
- Decision record: `.squad/decisions/inbox/frank-scalar-type-extension.md`.

## Recent Updates

### 2026-04-29 — Map access syntax updated to infix `for`; open-question callout added

- Replaced all `.get(key)` occurrences in `docs/language/collection-types.md` with the infix `for` form (`CoverageLimits for CheckCoverage.CoverageType`) — code block, proof-engine prose, action surface bullet in the `map` candidate section, and the multimap rejection section.
- Added an inline blockquote open-question callout after the grammar fit block in the `map` section, explicitly stating that `for` is the working syntax but has not been locked in. Both `for` and `at` are on the table; owner sign-off required.
- Filed decision record to `.squad/decisions/inbox/frank-map-access-for-open.md`.


- Replaced the C#-ish `.all(item, pred)` method-call form with the approved keyword-first quantifier syntax: `each`/`any`/`no` `binding in Collection (predicate)`.
- Updated `docs/language/collection-types.md`: new syntax examples, grammar sketch, lexer note (`each` and `no` need new entries; `all` superseded by `each`).
- Closed Open Questions Q2 (three keywords — decided: `each`/`any`/`no`) and Q3 (named binding — decided: author-named).
- Re-numbered remaining open questions (10 → 8).
- Updated inline `.all(x, ...)` example in Ordering category to `each x in Items (...)` form.
- Filed decision record to `.squad/decisions/inbox/frank-quantifier-syntax.md`.

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

