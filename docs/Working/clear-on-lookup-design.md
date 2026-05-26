---
status: Locked 2026-05-26 (owner ratifications: D-1 confirmed — lift spec exclusion; allow `clear MyLookup`. BUNDLED LIFT: `notempty` on lookup also lifted in same slice (`Types.cs:711 NotemptyApplicable: false → true`), restoring catalog uniformity with other collection kinds. Comparator survey stays inline in this doc; no promote to `research/architecture/compiler/`. `catalog-system.md` requires no edit; doc updates land in `collection-types.md` (`:85`, `:756`, `:902`) and `precept-language-spec.md` (`:1624`, `:1632`, `:1662`).)
authored: 2026-05-26
author: Claude (/lifecycle-2-design — F-LANG-COLL-13)
comparable-systems-research-status: surveyed in-line — Java `Map.clear()`, C# `IDictionary<K,V>.Clear()`, Python `dict.clear()`, Rust `HashMap::clear()`, Swift `Dictionary.removeAll()`, F# `Dictionary.Clear()`, Go `clear(map)` (Go 1.21+), Kotlin `MutableMap.clear()`, SQL `TRUNCATE TABLE` / unqualified `DELETE` — no surveyed language with a per-key remove forbids bulk clear. The "per-key remove implies no bulk clear" framing in the v3 spec appears to be unique to Precept. No full comparator survey artifact was written; if this design advances to Locked, promote the inline comparator notes to `research/language/collections/bulk-clear-on-lookup-comparator-survey.md`.
sources-consulted:
  - `docs/language/collection-types.md:85` — common-surface clause excluding `clear` on `lookup` ("has per-key `remove`")
  - `docs/language/collection-types.md:902` — cross-kind action matrix `clear F` row
  - `docs/language/collection-types.md:1280-1349` — `lookup of K to V` section: declaration, actions, accessors, constraints, proof obligations
  - `docs/language/collection-types.md:1335` — current lookup constraint surface (`notempty`, `mincount`, `maxcount`, `optional`; no `default [...]`)
  - `docs/language/precept-language-spec.md:1662` — action-statement validation row for `clear F` ("Not valid on `log of T`, `log of T by P`, or `lookup of K to V` (v3)")
  - `docs/language/precept-language-spec.md:157, :266` — no-iteration principle (§ 0.4.1) and its bounded-predicate carve-out
  - `src/Precept/Language/Actions.cs:50-60` — `ClearApplicable` array (currently 7 entries: Set, Queue, Stack, Bag, List, QueueBy, plus the `optional` modifier carve-out)
  - `src/Precept/Language/Actions.cs:129-134` — `ActionKind.Clear` metadata
  - `src/Precept/Pipeline/TypeChecker.Expressions.Callables.cs:400-440` — `ScalarOperationOnCollection` / `CollectionOperationOnScalar` enforcement site (Phase 4 W-A)
  - `samples/shopping-cart.precept:160-187, :256-269` — the affected sample; soft-clear workaround for `ClearCart` and `Cancel`
  - `docs/Working/bugs.md` § F-LANG-COLL-13 — bug entry as filed
  - `docs/Working/compiler-readiness-plan-2026-05-24.md § Phase 4` — collection-completeness phase, current Phase 4 finding list
  - `docs/Working/compiler-readiness-plan-2026-05-24.md § F-LANG-COLL-08` — `ApplicableTo` enforcement (the W-A work that surfaced this gap)
  - `docs/Working/f-lang-biz-10-currency-derived-maxplaces.md` — template for the structure and four-leg rationale conventions of this doc
  - Java SE Platform — `java.util.Map.clear()` (Java SE 21 API docs)
  - .NET API Reference — `System.Collections.Generic.IDictionary<TKey,TValue>.Clear()`
  - Python Language Reference — `dict.clear()` (CPython 3.13 docs)
  - Go release notes — `clear(map)` built-in (Go 1.21, August 2023)
  - SQL:2016 — `TRUNCATE TABLE` and bulk-`DELETE` semantics (partial — no verbatim standards excerpt without ISO access)
---

# F-LANG-COLL-13 — `clear` on `lookup of K to V`: a language-surface gap

## Goal

When done: an author who needs "empty this lookup" can express it directly. The canonical spec, the action catalog, the type checker, and the sample corpus all agree on what idiom realises the operation. The soft-clear workaround in `shopping-cart.precept` (lines 173-187, 256-269) is removed; the comment block citing F-LANG-COLL-13 deletes; `ItemQuantities` and `CartPromotions` are emptied in lockstep with `LineItems` rather than being left orphaned.

The decision below takes **Position A — lift the spec exclusion** (allow `clear MyLookup` with explicit "drop all keys" semantics). The doc enumerates the alternatives and their tradeoffs in Decision 1.

## Scope

- **In scope**:
  - Catalog change: add `TypeKind.Lookup` to `Actions.ClearApplicable` (`src/Precept/Language/Actions.cs:50-60`).
  - Spec amendments: `docs/language/collection-types.md:85, :902, :1293-1300` and `docs/language/precept-language-spec.md:1662` — replace the exclusion language with the lift, document the semantics, update the action matrix.
  - Type-checker test coverage: `clear MyLookup` compiles clean on every legal lookup shape (`lookup of K to V`, `lookup of ~string to V`, `lookup of K to V` with `notempty`/`mincount`/`maxcount` modifiers).
  - Falsifier coverage: scalar-on-lookup and lookup-on-scalar misuses continue to emit their existing diagnostics (`CollectionOperationOnScalar` / `ScalarOperationOnCollection`) — only the lookup-specific exclusion is lifted, not the broader cross-kind enforcement that W-A wired.
  - Sample restore: revert the soft-clear workaround in `samples/shopping-cart.precept`; add explicit `clear ItemQuantities` and `clear CartPromotions` to both the `ClearCart` and `Cancel` rows; remove the F-LANG-COLL-13 citation comments.
  - `bugs.md` entry transitions from Active to Fixed with a "Fixed by" cite.

- **Out of scope**:
  - `log of T` and `log of T by P`. See Decision 2 — the log exclusion stays; this design addresses lookup only.
  - A lookup-iteration primitive (`for each K in Lookup -> remove Lookup K`). See Decision 3 — rejected.
  - Any new accessor surface on lookup (`.keys`, `.values`, `.entries`). Not implicated by clear-on-lookup; deferred to future designs if and when an iteration consumer appears.
  - `default [...]` for lookup. The spec already forbids list-literal defaults for lookup because key-value pairs need explicit key syntax (`docs/language/collection-types.md:760, :1335`). This design does not touch defaults.
  - `notempty` on lookup. Tracked separately as F-LANG-COLL-10 (`compiler-readiness-plan-2026-05-24.md:230`). Lifting `clear` does interact with `notempty` (an authored `notempty` lookup that gets cleared in a transition becomes a proof-engine question — see Decision 5).

- **Deferred to future**:
  - Generalising the lift to `log of T` / `log of T by P` if the append-only philosophy is later relaxed for a specific consumer (e.g., reset-on-cycle audit logs).
  - Promoting the inline comparator notes (Java/C#/Python/Go/Rust/Swift/F#/SQL) to a durable research artifact under `research/language/collections/` if reviewer feedback requests it.

## Inventory of what will be built

### Code changes

**1. Catalog — extend `ClearApplicable`**

`src/Precept/Language/Actions.cs:50-60` — add `new(TypeKind.Lookup)` to the `ClearApplicable` array. After the change, the array reads (semantic shape):

```
Set, Queue, Stack, Bag, List, QueueBy, Lookup, ModifiedTypeTarget(null, [Optional])
```

No change to `ActionKind.Clear`'s `ActionMeta` (line 129) — `ClearApplicable` is the single source of truth and metadata derives from it.

**2. Type checker — no change**

`TypeChecker.Expressions.Callables.cs` validates action target types against `ActionMeta.ApplicableTo` (which is `ClearApplicable` for `Clear`). With Lookup added to the array, `clear MyLookup` becomes a clean validation pass. The `ScalarOperationOnCollection` enforcement that W-A wired (line 424) continues to fire on genuine scalar/collection mismatches; this design is additive.

**3. Runtime evaluator — small extension**

The runtime evaluator already implements `clear` for every other collection kind by replacing the backing `ImmutableXxx` with `Empty`. For `lookup of K to V`, the backing is `ImmutableDictionary<K, V>`; the implementation is `ImmutableDictionary<K, V>.Empty` (preserving the original key comparer for `~string` keys — `StringComparer.OrdinalIgnoreCase`). Mirror what already exists for the other collection kinds.

  - For `lookup of ~string to V`, the comparer carried on the original instance (case-insensitive) must be preserved on the cleared instance, otherwise subsequent `put`s would create case-sensitive keys. Specifically: use `ImmutableDictionary.Create<string, V>(StringComparer.OrdinalIgnoreCase)` rather than `ImmutableDictionary<string, V>.Empty`, matching the construction path documented at `collection-types.md:512`.

**4. No new diagnostic code**

The lift is a permission grant — it removes a current "this collection kind doesn't support `clear`" rejection rather than adding new validation. No new `DiagnosticCode` entry; no new `RecoverySteps`.

**5. Language-server completion + hover**

`tools/Precept.LanguageServer/Handlers/CompletionHandler.cs` — `clear` completion is gated by `ClearApplicable` and so automatically appears for lookup fields once the catalog changes. Hover for `clear F` where `F : lookup of K to V` should describe semantics: "Removes all key-value pairs from the lookup. The lookup becomes empty (`.count == 0`); subsequent `contains` queries return `false` for every key." No code change beyond the hover-string update if any catalog-derived hover descriptions hardcode the applicable-kinds list (none currently do — verify before close).

### Doc changes

- `docs/language/collection-types.md:85` — replace `"clear applies to set, queue, stack, bag, and list only — not log types (append-only) and not lookup (has per-key remove)"` with `"clear applies to set, queue, stack, bag, list, queue of T by P, and lookup — not log types (append-only)"`.
- `docs/language/collection-types.md:902` (action matrix row for `clear F`) — change the `Lookup` column from `✗` to `✓`.
- `docs/language/collection-types.md:1280-1300` (the `lookup of K to V` section's Actions table) — add a row: `clear` / `clear F` / "Remove all key-value pairs. The lookup becomes empty; subsequent `contains` returns `false`. Always safe."
- `docs/language/precept-language-spec.md:1662` — update the action-statement validation row for `clear F`. New text: `"`clear F` | `set of T`, `queue of T`, `stack of T`, `bag of T`, `list of T`, `queue of T by P`, `lookup of K to V`; any `optional` field | — | On `optional` fields, resets the field to "not set" (see §1.2). Not valid on `log of T` or `log of T by P` (v3)"`.
- `docs/language/catalog-system.md § Actions § Clear` (if such a section exists; verify) — surface the lookup addition alongside the existing six collection kinds.
- `docs/compiler-and-runtime-design.md` — no expected change; the pipeline contract is unchanged.

### Test stubs

`test/Precept.Tests/TypeChecker/ClearOnLookupTests.cs` (new file, or extend the existing collection-action validation tests):

- `[Fact] ClearOnLookup_StringToInteger_CompilesClean` — `field F as lookup of string to integer` + `clear F` in a transition row → no diagnostic.
- `[Fact] ClearOnLookup_CaseInsensitiveKey_PreservesComparer` — `field F as lookup of ~string to decimal` + `put F "USD" = 100.0` + `clear F` + `put F "usd" = 200.0` then `F contains "USD"` returns `true` post-second-`put` (case comparison preserved across clear).
- `[Fact] ClearOnLookup_AfterPuts_EmptiesAndCountZeroes` — runtime test asserting `.count == 0` after a sequence of `put` + `clear`.
- `[Fact] ClearOnLookup_ContainsReturnsFalseForAllKeys` — runtime test asserting `contains K` returns false for every previously-`put` key after `clear`.
- `[Fact] ClearOnLookup_WithMincountModifier_ProofObligationFires` — `field F as lookup of string to integer mincount 1` + `clear F` in a row → proof engine emits an `UnprovedCardinalityRequirement`-family diagnostic for the post-action state (or equivalent — see Decision 5).
- `[Fact] ClearOnLookup_ChainedPutAfterClear_CompilesClean` — `clear F -> put F "K" = 1` → no diagnostic; `F contains "K"` is `true` after the sequence.
- `[Fact] ClearOnLog_StillRejected` — `field F as log of string` + `clear F` → continues to emit `ScalarOperationOnCollection` (regression guard: only lookup was lifted).

### Sample restore

`samples/shopping-cart.precept`:
- Lines 173-179: delete the F-LANG-COLL-13 comment block.
- Lines 180-187: add `-> clear ItemQuantities` and `-> clear CartPromotions` to the `ClearCart` row.
- Lines 256-259: delete the F-LANG-COLL-13 comment block on `Cancel`.
- Lines 260-269: add `-> clear ItemQuantities` and `-> clear CartPromotions` to the `Cancel` row (or as many of those two lookups as exist in the row's reachable read paths).

`docs/Working/bugs.md` § F-LANG-COLL-13 — transition from Active to Fixed with `Fixed by:` cite to the implementing commit. Add a Post-fix cleanup sweep instruction analogous to BUG-002's: `grep -rn "F-LANG-COLL-13" samples/` to find any other cite sites.

## Decisions

### Decision 1 — Position A (lift the exclusion) over Position B (iteration primitive) or Position C (accept the gap)

- **Stakes**: This is the only language-surface decision. Position A keeps Precept's flat-statement philosophy intact and matches every surveyed comparator language. Position B would introduce a new construct (key iteration) that violates the no-iteration principle (§ 0.4.1, `precept-language-spec.md:157, :266`). Position C ships a permanent gap that the soft-clear workaround does not fully bridge (orphaned entries are semantically incorrect, even if read paths shield them — `bugs.md` notes "semantically incomplete").
- **Rationale**: `clear` is a bulk-cardinality operation, not an iteration. Every other surveyed language with both per-key remove and a map type provides bulk-clear directly; Precept is unique in withholding it. The original "has per-key `remove`" rationale conflated "every-element operation must be expressible by iteration" with "every-element operation must be available as a primitive" — but `clear` on set/bag/list is already a bulk primitive in Precept, not derived from per-element remove. Lookup is the same pattern.
- **Alternatives considered**:
  - **Position B (lookup-iteration primitive)** — rejected. Introducing `for each K in Lookup -> remove Lookup K` would either (i) require general iteration (forbidden by § 0.4.1) or (ii) require a new bounded-quantifier-shaped mutation construct that no existing collection kind has. The catalog has no precedent for "bounded mutation quantifier." Quantifier predicates (`each`/`any`/`no`) are explicitly carved out from the no-iteration rule because they produce a single boolean and mutate nothing (`precept-language-spec.md:159`); a mutation quantifier would lose that property and require new fixpoint-style reasoning machinery in the proof engine. Larger language addition with no other beneficiary.
  - **Position C (accept the gap, ship soft-clear as idiom)** — rejected. The soft-clear pattern is documented in shopping-cart.precept as a workaround, not an idiom. It relies on (a) the lookup having a controlling set/list that tracks "what keys exist authoritatively," and (b) every read path flowing through a membership guard on that controlling collection. Lookups that don't have a natural controlling collection (a fee schedule, a config table, a per-category settings map) can't use it. And even when the pattern applies, orphaned entries are real state that occupies memory and survives serialization — the "harmless" framing depends entirely on the consumer not introspecting the lookup directly, which Precept can't structurally guarantee.
- **Precedent**:
  - Java `java.util.Map.clear()` — bulk; no equivalent restriction.
  - C# `IDictionary<TKey,TValue>.Clear()` (and `Dictionary<K,V>.Clear()`) — bulk; no restriction.
  - Python `dict.clear()` — bulk; no restriction.
  - Go `clear(map)` (built-in since Go 1.21, August 2023) — explicitly added as bulk clear for maps; the language designers added it after the per-key `delete` had been in place for ~13 years, signaling that per-key remove is not considered a substitute.
  - Rust `HashMap::clear()`, Swift `Dictionary.removeAll()`, F# `System.Collections.Generic.Dictionary.Clear()`, Kotlin `MutableMap.clear()` — same pattern.
  - SQL `TRUNCATE TABLE` (or unqualified `DELETE FROM T`) — bulk clear with per-row delete also available; SQL doesn't forbid one because the other exists.
  - No surveyed language with a per-key remove forbids the bulk operation. Precept's exclusion was unique.
- **Tradeoff accepted**:
  - **Catalog surface grows by one entry** — minor; `ClearApplicable` becomes 8 entries instead of 7. This is exactly the kind of "metadata drives the language" change the catalog system was designed for.
  - **Proof-engine implication for `notempty` lookups** — a `notempty` lookup that gets cleared in a transition row body triggers a cardinality contradiction. This is handled by the existing proof-engine machinery for cardinality constraints (see Decision 5). No new infrastructure needed.
  - **One philosophical claim refined** — the v3 spec line ("has per-key remove") is rewritten as "previously excluded; lifted in vN+1 to match comparator-language norms and remove the soft-clear workaround dependency." The exclusion was design-by-omission rather than design-by-principle, so the lift does not contradict philosophy.
- **Sources**: as enumerated in `sources-consulted` frontmatter.

### Decision 2 — Address `log of T` and `log of T by P` exclusion: out of scope

- **Stakes**: The spec excludes `clear` on log types with the same prose ("append-only") that some readers might conflate with the lookup exclusion. If log clear is also desired, scoping that here would consolidate the spec amendment.
- **Rationale**: The log exclusion is principled in a way the lookup exclusion was not. `log of T` is the language's append-only chronicle type — its identity is "an immutable, ordered record of what happened." `clear` would destroy that identity. The shopping-cart sample is also explicit: the author needed lookup clear, not log clear; the soft-clear workaround in F-LANG-COLL-13 cites only lookups. There is no current consuming use case for log clear.
- **Alternatives considered**:
  - **Bundle log clear into this design** — rejected. Different philosophical weight (append-only is a principled identity; per-key-remove was incidental). Bundling would obscure the lookup-specific decision and force the log decision to inherit reasoning that doesn't apply to it.
  - **File a separate F-LANG-COLL-NN for log clear** — not now. No author has needed it; the soft-clear workaround was lookup-only. If a future consumer surfaces (e.g., reset-on-cycle audit logs), file then with that use case as grounding.
- **Precedent**: SQL `TRUNCATE` works on append-only tables in some dialects but not others; Java has no "log" type in the standard library; the "append-only chronicle" pattern in event-sourced systems (Kafka, EventStore) generally does not support bulk clear by design — the chronicle's identity is its immutability.
- **Tradeoff accepted**: The spec retains an asymmetry — lookup is lifted, logs are not. The asymmetry is documented in the amended action-statement table at `precept-language-spec.md:1662`; readers see the surviving exclusion and its rationale ("append-only") side by side with the previously-excluded-now-lifted lookup. Cleaner than papering over the asymmetry.
- **Sources**: `docs/language/precept-language-spec.md:1662`; current shopping-cart usage (no log fields touched by the workaround); `bugs.md` § F-LANG-COLL-13 (lookup-only scope).

### Decision 3 — No lookup-iteration primitive

- **Stakes**: The most ambitious alternative position would introduce a `for each K in Lookup -> ...` shape or a related bounded-mutation construct. This would address clear-on-lookup as a special case of a more general capability and could enable future operations (e.g., bulk transform).
- **Rationale**: Such a construct violates the no-iteration principle (`precept-language-spec.md:157`, restated at line 266). Quantifier predicates (`each`/`any`/`no`) are explicitly carved out because they produce a single boolean and mutate nothing — a mutation quantifier loses both properties. The proof engine has no machinery for fixpoint-style reasoning over mutations; adding it is an order of magnitude more work than lifting one catalog entry, and the user demand is zero (no other workaround in `bugs.md` cites iteration as the missing capability).
- **Alternatives considered**:
  - **`for each K in Lookup -> remove Lookup K`** — rejected. Loop-shaped construct; the parser, type checker, and proof engine would all need new constructs; the runtime evaluator would need iteration semantics it currently lacks.
  - **`apply F = (K, V) -> ...` shape with bulk-application semantics** — rejected as too speculative; same philosophical objection plus an order-of-evaluation question that doesn't have a current answer.
  - **`drop F` as a parallel verb to `clear F`** distinct semantics — rejected. The catalog principle is fewer-verbs-more-applicable, not more-verbs-disambiguated. `clear` already means "drop everything"; a new verb would split the surface.
- **Precedent**: Go's `clear(map)` is the relevant precedent — they added a bulk primitive specifically to avoid forcing authors to write a `for k := range m { delete(m, k) }` loop. The Go authors explicitly chose primitive over iteration. Precept's no-iteration commitment is stronger; the same reasoning applies a fortiori.
- **Tradeoff accepted**: If a future consumer demands key-transform or per-key-update semantics, those will need their own design pass. This design does not foreclose iteration entirely — it declines to introduce iteration as a vehicle for a single bulk operation.
- **Sources**: `precept-language-spec.md:157-159, :266` (no-iteration commitment); Go 1.21 release notes (precedent for "add bulk primitive rather than force iteration").

### Decision 4 — Backwards compatibility: no migration cost

- **Stakes**: A spec-level change to which forms compile cleanly is in principle a compatibility break. The Phase 4 W-A enforcement is recent (2026-05-26); samples haven't accumulated `clear MyLookup` usage because the diagnostic blocked it.
- **Rationale**: Every existing sample either (a) doesn't use `clear` on a lookup (so the lift is invisible) or (b) uses the soft-clear workaround that the lift renders unnecessary. Restoring the workaround sites is a one-pass sample edit, not a migration.
- **Alternatives considered**:
  - **Treat as a breaking change requiring a version bump** — rejected. No author-visible behavior changes for code that compiles today; the lift only newly accepts code that the v3 enforcement rejected.
  - **Ship behind a flag** — rejected. The catalog is the single source of truth; flags fragment the catalog. The lift is small enough to ship directly.
- **Precedent**: Standard practice for "narrowing a rejection set" in compiler language extensions — newly accepted programs don't break previously accepted programs.
- **Tradeoff accepted**: None of note. The compatibility surface is genuinely empty.
- **Sources**: sample corpus inspection (only shopping-cart cites F-LANG-COLL-13; no `clear MyLookup` appears anywhere in `samples/`); the W-A enforcement is the only barrier.

### Decision 5 — Proof-engine interaction with `notempty` and `mincount`

- **Stakes**: A `lookup notempty` (or `mincount N` with `N >= 1`) that gets cleared in a transition row body produces a state where the lookup violates its own cardinality constraint. The proof engine must catch this.
- **Rationale**: The existing infrastructure handles this case for the other collection kinds (set, bag, etc.) — `clear F` on a `notempty` field produces an `UnprovedCardinalityRequirement` diagnostic at the post-action state if the row doesn't subsequently re-populate the field. No new proof-engine code is required; the same machinery applies once Lookup is in `ClearApplicable`.
- **Alternatives considered**:
  - **Suppress the cardinality check for lookup specifically** — rejected. Inconsistent with the other collection kinds; would silently allow `notempty` to be violated.
  - **Add a new "clearance contradicts cardinality" diagnostic** — rejected. The existing diagnostic covers the case; new code would be parallel infrastructure for no gain.
- **Precedent**: Internal — `samples/` includes cases where the proof engine catches `clear F` on a `notempty` set (e.g., the relevant test in `test/Precept.Tests/ProofEngine*`). Lookup inherits the same behavior.
- **Tradeoff accepted**: An author who writes `field F as lookup of K to V notempty` and then `clear F` will see a diagnostic. This is the same experience the author would see for `set notempty` + `clear F`; consistency is the goal.
- **Sources**: F-LANG-COLL-10 (`compiler-readiness-plan-2026-05-24.md:230`) — `notempty` on lookup is the subject of a separate finding; this design assumes that finding lands with `notempty` continuing to apply to lookup (the "remove from doc" alternative would obviate Decision 5 by removing the relevant authoring shape; in that case this decision is a no-op).

## Acceptance criteria

1. `clear MyLookup` compiles clean in every test in `test/Precept.Tests/TypeChecker/ClearOnLookupTests.cs`.
2. `clear MyLog`, `clear MyLogBy` continue to emit `ScalarOperationOnCollection` (regression guard).
3. `samples/shopping-cart.precept` compiles clean with `clear ItemQuantities` and `clear CartPromotions` restored to both `ClearCart` and `Cancel` rows.
4. The F-LANG-COLL-13 citation comments in `shopping-cart.precept` (lines 173-179, 256-259) are deleted.
5. `docs/language/collection-types.md:85, :902` and the `lookup of K to V` Actions table at line 1293-1300 reflect the lift.
6. `docs/language/precept-language-spec.md:1662` reflects the lift; the surviving log exclusion is documented.
7. `Actions.ClearApplicable` array contains `TypeKind.Lookup`.
8. Runtime evaluator clears the backing `ImmutableDictionary<K, V>` and preserves the original key comparer for `~string`-keyed lookups; round-trip test asserts comparer preservation.
9. A `notempty` lookup cleared without re-population emits a cardinality diagnostic (same behavior as other collection kinds).
10. `docs/Working/bugs.md` § F-LANG-COLL-13 entry moves from Active to Fixed with a "Fixed by" cite.

## Dependencies

- **Upstream**: none. The Phase 4 W-A enforcement (2026-05-26, `ScalarOperationOnCollection` wired) is already in place; this design extends what W-A built rather than blocking on it. F-LANG-COLL-08 (`ApplicableTo` enforcement) is the architectural parent — this design is one applicability adjustment within that surface.
- **Downstream**: F-LANG-COLL-10 (`notempty` on lookup) — Decision 5's resolution depends on whichever way -10 lands. If -10 removes `notempty` applicability from lookup, Decision 5 becomes a no-op. If -10 keeps `notempty` on lookup, Decision 5 ships as described. Either resolution is compatible with the lift itself.
- **Sample restore window**: shopping-cart.precept's restore is part of the implementing slice; no separate sample-restore commit needed.

## Doc-update enumeration

Per `CLAUDE.md` § Doc Sync:

| File | Change |
|---|---|
| `docs/language/collection-types.md:85` | Replace common-surface exclusion text |
| `docs/language/collection-types.md:902` | Flip `clear F` × `Lookup` cell from `✗` to `✓` |
| `docs/language/collection-types.md:1293-1300` | Add `clear` row to lookup Actions table |
| `docs/language/precept-language-spec.md:1662` | Amend action-statement validation row for `clear F` |
| `docs/language/catalog-system.md § Actions § Clear` | If catalog-system surfaces per-action applicability lists, add Lookup; verify before edit |
| `docs/Working/bugs.md` § F-LANG-COLL-13 | Active → Fixed with "Fixed by" cite |
| `samples/shopping-cart.precept:173-187, :256-269` | Restore `clear ItemQuantities` / `clear CartPromotions`; remove citation comments |
| `docs/Working/compiler-readiness-plan-2026-05-24.md § Phase 4` | Add F-LANG-COLL-13 to the in-scope findings list with this design as its plan |

## Open questions

- **Open Question 1 — Lookup-applicability for `clear` while `notempty` is still ambiguous (needs owner ratification)**: If F-LANG-COLL-10 resolves "remove `notempty` from lookup," Decision 5 simplifies to "the situation can't arise." If F-LANG-COLL-10 keeps `notempty` on lookup, Decision 5 ships as described. Either is workable; the design does not gate on the resolution.
- **Open Question 2 — Comparator survey promotion (needs owner ratification at promote-or-cite time)**: The inline comparator notes (Java, C#, Python, Go, Rust, Swift, F#, SQL) are sufficient for this lift. If reviewer feedback requests a durable artifact, promote to `research/language/collections/bulk-clear-on-lookup-comparator-survey.md` at promote time. Listed in the frontmatter as `comparable-systems-research-status: surveyed in-line`.
- **Open Question 3 — Should the design also touch `docs/language/catalog-system.md`?** That doc enumerates catalogs as the language spec in machine-readable form. If it surfaces per-action applicability lists (verify before edit), the Lookup addition needs to land there too. If it stays at the inventory level only, no edit. The implementing slice should resolve this by reading the actual doc.

## Falsifiers

This design is falsifiable. If any of the following hold, reopen:

1. **A future construct genuinely requires "clear means iterate-and-delete" semantics** — e.g., per-entry side effects on clear (cache invalidation hooks, etc.). Precept does not currently support side-effects-on-mutation, so this falsifier is hypothetical, but if it lands, the bulk-primitive vs iteration tradeoff would re-open.
2. **A surveyed comparator language is found that has both per-key remove AND forbids bulk clear** — would weaken the precedent leg. Surveyed eight languages plus SQL; if a ninth surfaces that contradicts, reopen Decision 1.
3. **A real-world sample needs key-transform or per-key-update semantics** — would surface a real demand for Position B (iteration primitive). This design does not foreclose that work; it only declines to introduce iteration as a vehicle for clear-on-lookup.
4. **The proof-engine `notempty` + `clear F` interaction does NOT carry over to lookup automatically** — would surface a gap in Decision 5's "no new code needed" claim. Acceptance Criterion 9's test asserts the carry-over; if that test cannot pass without new code, this design is incomplete and needs an additional decision.
5. **The runtime evaluator's comparer preservation for `~string`-keyed lookups is harder than described** — would surface a runtime-side gap. Acceptance Criterion 8's test asserts comparer preservation; if it cannot pass without invasive runtime work, this design needs an additional Inventory entry.

## Phase-target rationale

**Target phase: Phase 4 (collection completeness).**

- **Fit with Phase 4 goal**: "Every documented capability of the 9 collection types works as specified. The catalog's action-applicability metadata is actually enforced." This design is one applicability change within that surface. F-LANG-COLL-08 (catalog action-applicability enforcement) is the architectural parent; this design ships an adjustment within the same surface that -08 built.
- **Fit with adjacent findings**: BUG-002 (lookup `remove` expects key, not value type) shipped in Phase 4 W-B (2026-05-26). The lookup action surface is being actively expanded; lifting `clear` lands in the same phase by topical adjacency.
- **Effort**: XS-to-S. One catalog-array entry + one runtime arm + four doc edits + seven tests + one sample restore. No new diagnostic, no new construct, no new proof-engine machinery. Order of effort: hours, not days.
- **Not Phase 5 (proof engine satisfiability)**: nothing in this design is a proof-engine extension. Decision 5's proof-engine interaction reuses existing infrastructure.
- **Not new phase**: too small to warrant its own phase; folds cleanly into Phase 4's existing collection-completeness scope.

**Recommended sequencing within Phase 4**: land alongside or immediately after F-LANG-COLL-08's `ApplicableTo` enforcement is fully in. If F-LANG-COLL-08 has already shipped (as W-A indicates), this can land as a follow-up slice in the same Phase 4 work stream with no rebasing concerns.
