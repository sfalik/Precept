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

## Historical Summary

- 2026-05-12 through 2026-05-16 concentrated Frank's work around hover contract reviews, field-state guarantees, constructor semantics, reject-surface structure, interval-proof design, quantity normalization, diagnostic-enforcement architecture, and counting-unit comparison gaps.
- The constructor/reject track settled three durable ideas: `on <Event>` is the honest construction surface, fallback `reject` is valid authored refusal rather than misuse, and grammar-level structural exclusion is preferred whenever the language already knows a path is impossible.
- Detailed batch-by-batch chronology now lives in `.squad/decisions.md` and `history-archive.md`; this file keeps only the guidance and latest durable closeout.

## Recent Updates

### 2026-05-16T18:25:58Z — Timezone completions bug diagnosed

- Root cause: `TypeKind.Timezone` in `GetTypedConstantItems` routes to generic `GetStructuredExampleItems`, which only yields 2 hardcoded examples (`America/New_York`, `UTC`) plus reused file values.
- Unlike currencies/dimensions/units, timezones have no dedicated completion handler that queries the full IANA zone database (`DateTimeZoneProviders.Tzdb.Ids`).
- The slot infrastructure (SlotPositionResolver, SlotVocabulary, trigger detection) all work correctly — this is purely a CompletionHandler dispatch gap.
- Fix: add `GetTimezoneItems` method using `DateTimeZoneProviders.Tzdb.Ids` and route `TypeKind.Timezone` to it instead of the generic handler.
- Assigned to Kramer (tooling). No runtime catalog change needed.
- Diagnosis written to `.squad/decisions/inbox/frank-timezone-completions-bug.md`.

### 2026-05-16T13:08:43Z — Constructor semantics batch closed end-to-end

- Frank's graph-analyzer passes locked the durable analyzer model: construction handlers live in `EventHandlers`, do not create topology edges, and require semantic handling for PRE0081 and `GraphEvent.IsInitial`.
- George completed Slice 8b at commit `c72db9b0`, removing row-level `initial` and making construction classification metadata/type-check driven.
- Kramer completed Slices 9+10 at commits `ec5525d2` and `e19736f6`, aligning hover and grammar generation with declaration-level `initial` semantics.
- Newman completed Slice 11, adding `isConstruction` to the MCP compile event-handler DTO surface without duplicating core logic.
- Frank completed Slice 12 docs/sample closeout: the language spec and constructor-semantics tracker are current, `CHANGELOG.md` records the shipped surface, and `samples/Test.precept` was locally verified while the stale MCP result was correctly treated as deployment drift.

### 2026-05-16T18:32:32Z — Timezone completions fix reviewed and APPROVED

- Reviewed Kramer's commit `c35e6032`. Implementation follows the diagnosis exactly: `GetTimezoneItems` queries `DateTimeZoneProviders.Tzdb.Ids`, routing cleanly split from `ZonedDateTime`, test proves full TZDB exposure (>100 zones, specific IANA IDs asserted). 328/328 tests pass. No catalog or runtime changes — CompletionHandler-only, as prescribed.

### 2026-05-16T18:30:48Z — Dot-accessor completions bug diagnosed

- Root cause: the `.` trigger handler (CompletionHandler.cs:108–119) has one code path: resolve receiver TYPE → show type accessors. When the receiver is an EVENT NAME, `TryGetReceiverTypeForDotTrigger` fails because `EventsByName` is never checked (only `FieldsByName`), and the handler returns empty completions.
- Two-part gap: (1) `CursorSemanticResolver` doesn't resolve event names for single-dot access, (2) `CompletionHandler` dot trigger only dispatches to type accessors, never event args.
- The Ctrl+Space path (`GetExpressionItems`) has the same blind spot — falls through to generic expression items instead of contextual event-arg completions.
- No existing test covers the `.` trigger character for dot access — the passing test `Completions_MemberAccess_UsesTypeAccessors` uses Ctrl+Space, not the dot trigger.
- Fix: add `TryGetEventForDotTrigger` to `CursorSemanticResolver`, add event-arg branch to dot trigger handler, add 4 tests.
- Assigned to Kramer (tooling). No runtime catalog change needed — `EventsByName` and `TypedEvent.Args` already exist in `Compilation.Semantics`.
- Diagnosis written to `.squad/decisions/inbox/frank-dot-accessor-completions-bug.md`.

## Learnings

- When a typed-constant domain has a validation data source (like NodaTime TZDB) but no completion handler, the fix is always CompletionHandler dispatch — never a catalog or SlotVocabulary change. The slot infrastructure correctly doesn't model typed-constant content domains.
- Test quality for catalog-backed completions should always assert a count threshold that distinguishes "full catalog" from "hardcoded examples" — `BeGreaterThan(100)` against a ~590-entry catalog is the right shape.
- The dot trigger is a separate dispatch path from Ctrl+Space expression completions — both must be checked independently. A passing Ctrl+Space test does NOT prove the trigger-character path works. Always test trigger-character paths with the `GetCompletionsAsync(source, triggerChar)` overload.
- When a receiver identifier can resolve to multiple semantic categories (fields, events, future: states?), each category needs its own resolution branch in the dot trigger. The resolver's return shape (`TypeKind`) only works for type-accessor dispatch — event-arg dispatch needs a different return shape (`TypedEvent`).
- `AppendToInsertText` is a hidden gate for snippet-based completions in the `'` trigger path — it unconditionally strips `InsertTextFormat.Snippet` to `PlainText` (line 1024). Any proposal that adds snippet templates to typed-constant completions must fix this first. Always check the post-processing pipeline, not just the item generation, when adding new `InsertTextFormat` expectations.
- Dimension-filtered quantity starters already have a proven pattern in `GetQuantitySlotItems` (lines 1377–1381): `UcumCatalog.BrowseTier1().Where(atom => atom.Vector == dimAlias.Vector)`. Reuse this for initial-items generation rather than inventing a parallel filtering mechanism.
- When writing implementation plans for completion handler work, verify the `AppendToInsertText` / `appendClosingQuote` post-processing path — it applies to all items in the `'` trigger branch and can silently break format expectations that look correct at the item-generation level.


