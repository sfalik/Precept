## Core Context

- Owns tooling surfaces: language server, VS Code extension, grammar sync, plugin wiring, MCP ergonomics, and executable developer workflows.
- Keeps grammar, completions, semantic tokens, tests, and tooling docs synchronized with the actual DSL and server surface.
- Favors catalog-driven and semantic-model-driven editor behavior over LS-local keyword lists or parser-span guesses.

## Learnings

- **`[` trigger span guard identical to `{` one-column-back pattern (2026-05-16):** After typing `[`, the cursor lands at `span.EndColumn` (exclusive), which `Contains()` treats as outside the token. Guard by checking `IsInsideTypedConstantToken` at `position with { Character = position.Character - 1 }`. Applies to any trigger character that terminates at the end of a token span.
- **Test assertion thresholds on substring-filtered TZDB sets (2026-05-16):** `"America/tor"` matches only two IANA zones (`America/Toronto`, `America/Tortola`). Never use `BeGreaterThan(N)` for a filtered TZDB result without verifying the actual match count — assert specific known members and absent members instead.
- **{ trigger span guard requires one-column-back check(2026-05-16T20:38:00-04:00):** `IsInsideTypedConstantToken` uses `character < span.EndColumn` (strict less than). When `{` is the LAST character of a `TypedConstantStart` token, the cursor position AFTER typing `{` is at exactly `span.EndColumn` — which `Contains()` treats as outside the span. The fix: guard by checking `IsInsideTypedConstantToken` at `position with { Character = position.Character - 1 }` (the `{` character itself), not at the cursor. This pattern is essential for any trigger-character guard where the trigger char is the last char of a token span.
- **Brace-interpolation snippet insert text: `Name}` not `{Name}` (2026-05-16T20:38:00-04:00):** when the `{` trigger fires, the `{` is already in the document. Completion items must insert `Name}` (just the identifier + closing brace) — NOT `{Name}` which would double the brace. Label is `{Name}` (the visible form) but `snippetTemplate` is `Name}` so `InsertText = "Name}"`. Use `$"{"` for label (C# `$"{{{field.Name}}}"`) and `$"{field.Name}}}"` for the snippet template.
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
- **Dot-trigger event receivers must win before generic receiver typing (2026-05-16T18:32:32.380-04:00):** after `EventName.`, `TryGetReceiverTypeForDotTrigger` can still succeed with `TypeKind.Error` because the incomplete member access parses as an error-typed expression. Check `EventsByName` first in the `"."` trigger branch, then fall back to field/type accessor resolution, and lock both paths with trigger-character integration tests.

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

### 2026-05-16T18:59:08-04:00 — Slice 0 snippet-format prerequisite closed

- Fixed `CompletionHandler.AppendToInsertText` so `'`-trigger typed-constant completions preserve the source `InsertTextFormat` instead of downgrading snippets to plain text.
- Added regression coverage in `CompletionHandlerTests` for both branches: plain-text items still append the closing quote without becoming snippets, and snippet items with `${1:...}` keep `InsertTextFormat.Snippet` after suffix append.
- Validation: targeted `CompletionHandlerTests.Completions_TypedConstant_SingleQuoteTrigger*` passed, then full `dotnet test --nologo` passed. Slice A is unblocked.

### 2026-05-16T18:59:08-04:00 — Slice A premium snippet templates landed

- Added `TypedConstantSnippet = 2` sort group so snippet templates sort before plain examples/reused values (TypedConstant shifted to 3, TypedConstantSegment to 4).
- `GetTemporalDateTimeSnippetItems` dispatches `date`/`time`/`instant`/`datetime`/`zoneddatetime` to per-type snippet helpers, then falls through to existing `GetStructuredExampleItems` so plain examples are preserved.
- `GetTemporalBuilderSnippets` prepends `duration`/`period` builder snippets; when a `TemporalUnit` qualifier is declared, shows a unit-specific single-template instead of the full builder set.
- `GetMoneySnippetItems`: qualifier-aware — prefills currency literal, interpolated field name, or generic `${1:0.00} ${2:USD}` based on `DeclaredQualifierMeta.Currency`.
- `GetQuantitySnippetItems`: qualifier-aware — prefills unit literal, interpolated field name, dimension-filtered `UcumCatalog.BrowseTier1()` items when `quantity of <dimension>`, or generic `${1:0} ${2:each}`. Dimension-filtering reuses the `atom.Vector == dimAlias.Vector` pattern from `GetQuantitySlotItems`.
- 24 new tests covering all types and sort ordering. Full test suite: 6,179 passing. Commit `365674a2`.
- **Frank review needed:** ZDT UTC item uses `[UTC]` bracket; confirm that is valid syntax. Also confirm `quantity — amount + unit` default (`${1:0} ${2:each}`) is the right generic fallback or should be `${2:unit}`.

### 2026-05-16T18:59:08-04:00 — Slice A Frank review conditions addressed

- **Condition 1 (test count):** Actual new test count is 16, not 24 as stated in the task description. Count confirmed, no missing tests.
- **Condition 2 (qualifier-aware duration/period):** `TypeKind.Duration` has no `QualifierShape` in `Types.cs`, so `duration in 'hours'` is not valid DSL — the `unitQualifier is not null` branch for Duration is unreachable via field declarations. `TypeKind.Period` does have `QualifierShape: QS_TemporalUnitOrDimension`, so `period in 'days'` works. Added two tests: one for `period in 'days'` (verifies `period — days` label and suppression of generic set) and one for `period in 'weeks'` (verifies `period — weeks` label, suppression of `period — days` and `period — years + months`).
- **Condition 3 (SourceFieldName interpolation):** Added test for `field Cost as money in '{CatalogCurrency}'`. Verifies label is `money — CatalogCurrency currency` and InsertText contains literal `{CatalogCurrency}` brace interpolation, with no `${2:` tab stop.
- Total LS tests: 354 passing. Commit `343a1fd3`.

### 2026-05-16T20:38:00-04:00 — Slice B: { trigger + span-guarded interpolation completions

- Added `"{"` to `TriggerCharacters` in `GetRegistrationOptions`.
- Guard in `GetCompletions`: checks `IsInsideTypedConstantToken` at `position with { Character = position.Character - 1 }` (one column back), because the cursor after typing `{` lands at the exclusive end of `TypedConstantStart`'s span and `Contains()` uses strict `<`. Stepping back one column to the `{` character itself reliably identifies "was `{` typed inside a typed constant".
- `GetBraceInterpolationItems`: iterates event args (current event, via `CursorSemanticResolver.GetCurrentEventName`) and all precept fields, emitting Snippet items with label `{Name}` and `InsertText = "Name}"` (closing brace appended since `{` is already in the document).
- Updated `ServerCapabilityTests.Initialize_AdvertisesFinalCapabilitySurface` and `GetRegistrationOptions_AdvertisesExpectedTriggerCharacters` (renamed) to include `"{"`.
- 7 new tests: `OutsideTypedConstant_ReturnsEmpty`, `ReturnsInScopeFields`, `ItemsHaveSnippetFormat`, `MultipleFields_AllReturned`, `InsertTextClosesHole`, `WithEventArg_ReturnsArgItem`, `Regression_SingleQuoteTrigger_SnippetFormatPreserved`.
- Total LS tests: 361 passing. Commit `2dc06879`.

### 2026-05-16T20:38:00-04:00 — Slice B Frank review conditions addressed (commit `85718bc4`)

- **Condition 1 (PlainText for brace interpolation items):** Switched `GetBraceInterpolationItems` from `snippetTemplate:` to `insertText:` for both event-arg and field items. Insert text (`Name}`) is unchanged. Format is now `InsertTextFormat.PlainText` — no tab stops exist, so Snippet format was unnecessary and the unescaped `}` violated the LSP snippet grammar. Updated `ItemsHaveSnippetFormat` test to `ItemsHavePlainTextFormat` and corrected its assertion.
- **Condition 2 (column-zero guard test):** The `position.Character == 0` early return was already present in the guard at the top of the `"{"` trigger branch. Added `Completions_BraceTrigger_AtColumnZero_ReturnsEmpty` to lock the boundary — cursor at column 0 of any line, `{` trigger, expects empty list with no exception.
- Total LS tests: 362 passing.

### 2026-05-16T22:39:47-04:00 — Multi-event declaration parity fix (commit `013e08b7`)

- **Root cause:** `EventDeclaration` used a flat `[IdentifierList, ArgumentList, InitialMarker]` slot triple. `ParseIdentifierList` stops at any non-identifier token, so `initial` (a keyword, not an identifier) terminated the name list. The remaining `, start, stop, reset` was silently dropped.
- **Fix:** Introduced `EventEntryList` (ConstructSlotKind = 20), a compound slot mirroring `StateEntryList`. Each `EventEntrySyntax` entry carries its own name, `Args` list, and `IsInitial` flag. Parser method `ParseEventEntryList` loops `identifier → optional '(' args ')' → optional 'initial' → comma continues`.
- **Per-entry initial:** `initial` is per-entry, not global. `event create initial, start` marks only `create` as initial.
- **Updated consumers:** NameBinder.CollectEvent, RichHoverFactory.TryFindQualifierAt (iterates entries by evt.Name), SlotPositionResolver.IsExpressionPhase (EventEntryList case for default-value detection), OutlineSymbolProjector.ExtractName.
- **Tests:** 15 existing tests updated; 3 new tests added (MultipleEventsOnOneLine, InitialWithMultiple, SingleEvent_Regression). Total: 5784 Precept.Tests, 365 LS.Tests, 46 Mcp.Tests — all passing.

