## Core Context

- Owns tooling surfaces: language server, VS Code extension, grammar sync, plugin wiring, MCP ergonomics, and executable developer workflows.
- Keeps grammar, completions, semantic tokens, tests, and tooling docs synchronized with the actual DSL and server surface.
- Favors catalog-driven and semantic-model-driven editor behavior over LS-local keyword lists or parser-span guesses.

## Learnings

- **Timezone typed constants should use validation catalogs, not generic examples (2026-05-16T18:29:22.060-04:00):** when a typed-constant type already validates against a real domain catalog (`DateTimeZoneProviders.Tzdb.Ids` for `timezone`), wire completions to that source directly like currency/unit/dimension handlers do. Guard it with an integration test that asserts representative labels and a count far above the fallback-example path so regressions cannot silently drop back to two hardcoded examples.
- **Slot-vocabulary cutover fallback layer (2026-05-16T09:25:00-04:00):** `SlotPositionResolver` is strong enough to drive the main `SlotVocabulary` switch, but live completions still need a small token-level fallback layer for pre-slot and gap positions (`from|in|to` before names, `on` before events, valued-modifier expressions, comma continuations, and trailing `->`). Keep those as explicit micro-context shims in `CompletionHandler` until slot metadata can represent them directly.
- **Trailing transition-row action arrows (2026-05-16T08:44:58.170-04:00):** run structural action-chain continuation recovery before construct-aware slot checks. Parser recovery can extend a `TransitionRow` span through a fresh trailing `->` while the `ActionChainSlot` still ends at the last completed action, so relying on construct presence suppresses valid action-verb completions and incorrectly invites outcome logic to decide.
- **Slot-level comma continuation routing (2026-05-16T08:25:44.026-04:00):** treat `,` as continuation punctuation for the owning name-list slot, not as a cue to fall into the next post-name lane. State targets (`from`/`to`/`in`), state declaration entry lists, and access field targets (`modify`/`omit`) each need continuation recovery that keeps completion on the same semantic list and filters already-listed names before returning results.
- **From-clause comma continuation routing (2026-05-16T08:23:29.087-04:00):** the empty completion list after `from off, ` was a slot-routing failure, not a missing catalog. Because commas are significant punctuation, the resolver stayed on the comma, fell back to `TopLevel`, and then whitespace-gated top-level completions collapsed to nothing; recover `InStateTarget` for comma continuations, suppress `any`, and filter already-listed states from the same `StateTargetSlot`.
- **Completion pass 2 closeout (2026-05-15T18:18:38.540-04:00):** the remaining completion gaps were routing problems, not missing catalogs. Use narrow slot lanes (`AfterStateTarget`, `AfterEventTarget`, `AfterNo`) and derive state-target verbs, `->` outcomes, and expression keywords from catalog metadata; gate `any` and `all` by target family.
- **Completion audit + routing hardening (2026-05-15T18:04:26.860-04:00):** keep raw document text available to completion so top-level constructs can be limited to whitespace-only line starts; route `transition`, valued modifiers, and event-arg modifier domains through the correct contexts.
- **Completion test pattern reminder (2026-05-15T18:04:26.860-04:00):** use a `¦` cursor marker and assert both wanted labels and forbidden noise. Negative assertions catch fallback-routing leaks.
- **Set-assignment operator context (2026-05-13):** `set FieldName ` is its own `SlotContext.InSetAssignment`; completion should offer only `= ` there, never top-level constructs.
- **Action-chain continuation arrow (2026-05-15T18:47:10.829-04:00):** when a fresh `->` line sits outside the parsed construct span, recover `InActionVerb` with a short backward scan over significant tokens; require both a prior action verb and a prior same-or-deeper-indented `->` so continuation recovery stays precise.
- **SlotPositionResolver shadow path (2026-05-16T09:02:56-04:00):** `tools/Precept.LanguageServer/SlotPositionResolver.cs` can classify structural slot ownership by letting parsed slot spans own the gaps between slots, then deriving `InList`, `InChain`, `AfterSlot`, and `InExpression` from slot metadata plus the previous significant token.
- **Shadow-run test shape (2026-05-16T09:02:56-04:00):** `test/Precept.LanguageServer.Tests/SlotPositionResolverTests.cs` works best as two layers: legacy-context shadow comparisons for stable structural anchors, plus direct slot/phase assertions for catalog behaviors that do not yet map one-to-one to legacy `SlotContext` micro-states.

## Historical Summary

- Early May through 2026-05-15 established the tooling baseline: hover-card compaction, grammar/completion routing, semantic-token precision, and catalog-driven language-server behavior.
- Older hover-specific batch detail now lives in `history-archive.md` and `.squad/decisions.md`; this file stays focused on the durable completion contract and current guidance.

## Recent Updates

### 2026-05-15T22:18:38Z — Completion provider audit fully closed

- Pass 1 and Pass 2 closed the top-level keyword leak and the six spec-driven completion gaps: state/event-target continuations, `any`/`all` gating, `no -> transition`, `->` outcome keywords, and expanded expression operator/keyword vocabulary.
- All fixes remained catalog-sourced through `Constructs`, `Modifiers`, `Actions`, `Operators`, `Outcomes`, `ExpressionForms`, and token metadata rather than language-server-local keyword arrays.
- Validation endpoint: `dotnet test test\Precept.LanguageServer.Tests\ --nologo` → 319/319 passing.

### 2026-05-16T09:02:56Z — Catalog-driven completions Slice 1 metadata landed

- Frank landed Slice 1 on `spike/Precept-V2-Radical` at commit `069145da`.
- `ConstructSlot` now carries `IsList`, `IsChainable`, `ItemIntroducerToken`, and `Vocabulary` metadata, backed by a 13-value `SlotVocabulary` enum.
- All 24 slot instances were populated with the new metadata contract.
- Validation stayed clean: 16 new tests passed, and there were zero regressions against 6,157 existing tests.
- Slice 2 can now assume the slot metadata baseline exists when building the resolver path.

### 2026-05-16 — Slices 9+10: hover/grammar for `initial` on event declarations

- Updated `InitialEvent` modifier description from stale "Auto-fire entry point event" to "Construction mechanism — fires once at entity creation".
- Added `TryCreateEventModifierHover` to `HoverHandler.cs`: checks token is an `EventModifierMeta` token AND enclosing construct is `EventDeclaration`; returns context-specific hover. Implemented generically over the full `EventModifierMeta` set — new event modifiers get hover automatically.
- Grammar generator (`Program.cs`) now derives event modifier alternation from `Modifiers.All.OfType<EventModifierMeta>()` and adds an explicit group 5 capture `(\s+(?:initial)\b)?` before the args group (renumbered to 6). Group 5 uses nested `"patterns"` (not flat `"name"`) so whitespace is not scoped. Regenerated `precept.tmLanguage.json`.
- 3 new tests added: `Hover_InitialModifier_ReturnsText`, `SemanticTokens_InitialModifier_Classified`, `Grammar_EventInitial_Highlighted`.
- Completions were already working via `TryGetModifierFallbackItems` — no code change needed there.
- Validation: 327 LS tests passing, 5782 Precept.Tests passing. Commit `ec5525d2`.

### 2026-05-16T13:08:43Z — Team closeout recorded for constructor semantics tooling lane

- George's Slice 8b semantic cutover (`c72db9b0`) established the declaration-level `initial` baseline that Slices 9+10 carried through hover and grammar.
- Newman's MCP DTO follow-through and Frank's docs/sample closeout completed the remaining downstream surfaces, so the tooling lane now ships as part of a full vertical slice rather than an isolated editor change.
- Scribe merged the Slices 9+10 completion note into `.squad/decisions.md` and recorded the batch in squad logs.

