## Core Context

- Owns language research, spec wording, and cross-surface architecture documentation for the Precept DSL and runtime.
- Catalog metadata remains the language truth; parser, analyzer, evaluator, tooling, and docs should derive from durable metadata rather than enum-identity switches or parallel keyword lists.
- Constructor-semantics work stays complete only when docs, diagnostics, samples, and downstream tooling surfaces match shipped behavior.

## Live Guidance

- Quantity normalization still has two durable lanes: compile-time normalization for declarations/literals and runtime normalization for ingress values; both should stay on shared normalizer logic.
- `TypedField` remains the normalization handshake between analysis and execution: authored bounds stay available for display, normalized bounds feed proof/comparison surfaces.
- Comparison/equality checking must stay as strict about explicit counting-unit identity as assignment is about constrained qualifier axes.
- When the grammar can make an invalid form impossible, do that instead of inventing a later semantic ban.
- Documentation updates for a shipped feature must verify against the actual source and validation run; stale tooling builds are ops drift, not spec truth.

## Durable Learnings

- Any claim that work happens "only at compile time" must be stress-tested against Fire/Update/Restore ingress paths.
- Construction row syntax is now declaration-driven: `initial` lives only on event declarations, while authored rows are bare `on <Event>` and the type checker classifies construction from event metadata.
- Graph analysis for construction must stay semantic, not topological: construction handlers do not generate graph edges, PRE0081 must consult construction handlers, and `GraphEvent.IsInitial` must come from event metadata.
- Hollow-entity validation should be shared across all pre-materialization expression lanes, not re-added slot by slot.
- Formal grammar production rules must reflect structural exclusion decisions immediately; the grammar doc is a design deliverable, not follow-up cleanup.
- Constructor semantics (Pattern A) and parameterless construction with governed initial state (Pattern B) are complementary first-class idioms — neither is a gap pattern. The domain shape dictates the choice: existential identity requirements → constructor; progressive enrichment lifecycle → parameterless + governed draft state.

## Historical Summary

- 2026-05-12 through 2026-05-16 concentrated Frank's work around hover contract reviews, field-state guarantees, constructor semantics, reject-surface structure, interval-proof design, quantity normalization, diagnostic-enforcement architecture, and counting-unit comparison gaps.
- The constructor/reject track settled three durable ideas: `on <Event>` is the honest construction surface, fallback `reject` is valid authored refusal rather than misuse, and grammar-level structural exclusion is preferred whenever the language already knows a path is impossible.
- Detailed batch-by-batch chronology now lives in `.squad/decisions.md` and `history-archive.md`; this file keeps only the guidance and latest durable closeout.

## Recent Updates

### 2026-05-17T18:06:33Z — Constructor semantics fully closed

- `frank-30` traced the `F5TempVerify` `UnsatisfiableInitialState` failures to a PRE0115 ProofEngine false positive and added Slice E to the plan.
- Commit `ce16e69b` then exempted construction-row precepts from PRE0115 and restored the full `test\Precept.Tests` suite to `5798/5798` green.
- Commit `8a2bae37` finished the docs/samples closeout, syncing `samples\Test.precept` plus the language-spec PRE0092 and PRE0115 exemption notes.
- Constructor semantics is now end-to-end closed across syntax, checker, proof, tooling, docs, and samples.

### 2026-05-17T18:06:33Z — Constructor semantics docs/samples closeout (slice-docs-samples)

- Fixed `samples/Test.precept`: split `event create initial, start, stop, reset` into two declarations — `initial` modifier terminates the event declaration line.
- Added explicit PRE0092 construction-row exemption note to language spec §3.8 (Stateless/stateful cross-validation).
- Added PRE0115 construction exemption for initial-state satisfiability to language spec §5 (Proof Engine).
- `docs/runtime/runtime-api.md` and `CHANGELOG.md` already reflected final semantics from prior closeout — no changes needed.
- Pattern A samples (`loan-application`, `parcel-locker-pickup`, `clinic-appointment-scheduling`) confirmed syntactically correct; MCP PRE0092 is stale deployment drift.
- Commit: `8a2bae37`. Constructor semantics implementation fully closed across all 12 slices.
- Decision: `.squad/decisions/inbox/frank-docs-samples-complete.md`.

## Learnings

- MCP `precept_compile` tool has three known compiler gaps that affect pattern/anti-pattern verification: (1) PRE0092 not exempting initial-event construction rows, (2) PRE0038 (ComputedFieldNotWritable) not firing on `set` in transition rows, (3) PRE0010 (NonAssociativeComparison) not firing in parser for chained comparisons. Patterns are correct per spec — don't "fix" the DSL; file compiler bugs.
- PRE0115 (UnsatisfiableInitialState) is the ProofEngine sibling of PRE0092. Both are pipeline stages that don't understand construction rows as a valid mechanism. The pattern: "if a construction row exists, the entity never inhabits the initial state with default values, so default-value satisfiability checks are inapplicable." Always check ALL pipeline stages when introducing a new semantic mechanism — each stage that reasons about state entry may need an exemption.
- When a Pattern A sample has GUARDED ensures on the initial state, it accidentally avoids PRE0115 (guarded ensures are skipped). This masked the bug in loan-application.precept. Lesson: canonical samples should exercise the hardest paths — unguarded ensures on construction-populated states are the real stress test.
- When auditing fragment snippets, always verify in the TYPE CONTEXT they were designed for (e.g. `rule X <= Y * 3.0` only compiles if X/Y are decimal/money, not `number`). Fragment correctness is context-dependent.
- The `remove` collection action requires presence proof on the value operand. Any optional field used as a `remove` target must have `is set` in the guard path.
- When a spec section already explains mechanics but the language supports multiple valid domain idioms, the spec must name those idioms explicitly and give a selection rubric. For construction, the durable pair is **Constructor with existential fields** versus **Free construction + governed draft state**; the distinction is existential-at-birth data versus lifecycle-appropriate progressive enrichment.
- When a typed-constant domain has a validation data source (like NodaTime TZDB) but no completion handler, the fix is always CompletionHandler dispatch — never a catalog or SlotVocabulary change. The slot infrastructure correctly doesn't model typed-constant content domains.
- Test quality for catalog-backed completions should always assert a count threshold that distinguishes "full catalog" from "hardcoded examples" — `BeGreaterThan(100)` against a ~590-entry catalog is the right shape.
- The dot trigger is a separate dispatch path from Ctrl+Space expression completions — both must be checked independently. A passing Ctrl+Space test does NOT prove the trigger-character path works. Always test trigger-character paths with the `GetCompletionsAsync(source, triggerChar)` overload.
- When a receiver identifier can resolve to multiple semantic categories (fields, events, future: states?), each category needs its own resolution branch in the dot trigger. The resolver's return shape (`TypeKind`) only works for type-accessor dispatch — event-arg dispatch needs a different return shape (`TypedEvent`).
- `AppendToInsertText` is a hidden gate for snippet-based completions in the `'` trigger path — it unconditionally strips `InsertTextFormat.Snippet` to `PlainText` (line 1024). Any proposal that adds snippet templates to typed-constant completions must fix this first. Always check the post-processing pipeline, not just the item generation, when adding new `InsertTextFormat` expectations.
- Dimension-filtered quantity starters already have a proven pattern in `GetQuantitySlotItems` (lines 1377–1381): `UcumCatalog.BrowseTier1().Where(atom => atom.Vector == dimAlias.Vector)`. Reuse this for initial-items generation rather than inventing a parallel filtering mechanism.
- When writing implementation plans for completion handler work, verify the `AppendToInsertText` / `appendClosingQuote` post-processing path — it applies to all items in the `'` trigger branch and can silently break format expectations that look correct at the item-generation level.
- `writable` and `editable` are **two distinct concepts at two distinct layers** — no naming inconsistency. `writable` is a Value Modifier (field-level baseline: "editable everywhere by default"). `editable` is an Access Modifier (state-scoped override: "editable in this specific state"). They occupy different grammatical positions, different catalog categories (`ModifierKind.Writable` vs `ModifierKind.Write`), and different lifecycle scopes. The names are deliberately different because the semantics are different.
- The two-layer access mode composition model: Layer 1 = `writable` on field declaration sets the universal baseline; Layer 2 = `in State modify Field editable|readonly` overrides per (field, state) pair. State-level always wins.
- For `FieldNeverSet` analysis: `modify Field editable` (state-scoped) is clearly a write site (grants caller mutation in that state). The `writable` baseline (grants caller mutation in ALL states) requires an explicit design decision — it's semantically equivalent to "editable everywhere" but lives at a different abstraction layer. Recommendation: treat it as a write site to avoid false positives.

