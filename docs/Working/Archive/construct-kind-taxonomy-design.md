---
status: Locked 2026-05-26 (owner ratifications: Phase 4 target confirmed; test files renamed to `*InitialEvent*` naming — `ParserConstructionRowTests → ParserInitialEventRowTests`, `GraphAnalyzerConstructionTests → GraphAnalyzerInitialEventTests`, `ProofEngineConstructionTests → ProofEngineInitialEventTests`, `TypeCheckerConstructionStructuralTests → TypeCheckerInitialEventStructuralTests`; comment-level "construction" as concept name retained)
type: language-surface design
scope: ConstructKind taxonomy reorganization
findings: F-LANG-GRAM-01, F-LANG-GRAM-02
upstream-research: docs/Working/compiler-readiness-review-2026-05-24-appendices/audit-collections-grammar-docs.md
target-phase: Phase 4 (or doc-only portion absorbable into a Phase 1 follow-up)
---

# ConstructKind taxonomy reorganization

## 1. Goal

Resolve the three-way incoherence in the `ConstructKind` enum, the `Constructs` catalog, and `docs/language/precept-grammar.md` such that (a) the parser-side names match the typed-side names match the grammar-doc names, (b) the stateless `on Event -> Actions` form and the stateful construction-row form share a single parsed kind whose construction-vs-handler classification is performed by the type checker (the post-Slice-8b reality), and (c) the catalog no longer ships a documented-vestigial enum value. The end-state is one parse kind per grammar form, one canonical name across all surfaces, and a typed-side promotion that splits success vs reject — exactly the model the type checker already implements but the parser-side catalog still obscures.

## 2. Scope

**In-scope**
- `ConstructKind` enum membership and ordinal values.
- `Constructs.cs` metadata entries (descriptions, examples, slot shapes, `RoutingFamily`).
- Parser dispatch identifiers (`ResolveRejectVariant`, `ParseEventHandler`).
- `TypeChecker.Normalization.cs` manifest-iteration sites.
- `GraphAnalyzer.cs` and `TypeChecker.Validation.Structural.cs` pattern-match sites on `TypedEventRow*`.
- `docs/language/precept-grammar.md` naming and count.
- Test fixtures naming the renamed kinds.

**Out-of-scope**
- The `TypedEventRow` discriminated union and its `Success` / `Reject` subtypes — already correctly named, not part of this design.
- Construction semantics (initial event, fire-once, `ZeroConstructionRows`) — owned by `constructor-semantics.md` (archived) and shipped.
- Grammar-doc count drift outside the construct table (`ConstructSlotKind`, expression-form, catalog count) — separate findings (F-LANG-GRAM-03 / -04 / -05).

**Deferred**
- Renaming `EventRow` to anything more specific (`StatelessEventRow` / `EventHandlerRow`). The current name is dual-purpose by construction (see Decision 3) and a further rename has higher cost than benefit. Revisit only if a third dispatch path emerges.

## 3. Inventory of what will be built

**Code**
- `src/Precept/Language/ConstructKind.cs` — drop `ConstructionRow = 19`; rename `ConstructionRowReject = 20` → `EventRowReject` (keep ordinal 20). XML docs updated.
- `src/Precept/Language/Constructs.cs` — drop the `ConstructionRow` `GetMeta` arm; rename the `ConstructionRowReject` arm to `EventRowReject`. Update the `EventRow` description to acknowledge dual-use explicitly. Drop the now-stale "Slice 8b: no longer produced by parser" disclaimer in the `EventRow` description (the catalog is the spec; the disclaimer was a transitional bridge).
- `src/Precept/Pipeline/Parser.cs:299` — `ResolveRejectVariant` returns `ConstructKind.EventRowReject` (was `ConstructionRowReject`).
- `src/Precept/Pipeline/TypeChecker.Normalization.cs:45-67` — iterate `ConstructKind.EventRowReject` (was `ConstructionRowReject`); update doc-comment to drop "Slice 8b removed parser production of `ConstructionRow`" backreference (the kind no longer exists).
- `src/Precept/Pipeline/GraphAnalyzer.cs:762` — no rename needed (already pattern-matches on `TypedEventRowReject`, typed side).
- No runtime API surface changes.

**Docs**
- `docs/language/precept-grammar.md` — six existing `EventRowReject` references become correct (no change). The two `ConstructionRowReject` references (table on line 228, paragraph on line 229) become `EventRowReject`. The vestigial-`ConstructionRow` note at line 231 is deleted. Construct count "14" → 14 (no change; the enum drops one and gains none — see Decision 1 math: 15 → 14).
- `docs/compiler/type-checker.md:452` — already correctly describes the post-Slice-8b classification model; no change required (verify).
- `docs/Working/compiler-readiness-plan-2026-05-24.md` Phase-4 entry for F-LANG-GRAM-01/-02 — mark as superseded by this design; the new "rename + delete" combo lands together.
- No spec/README/runtime-doc impact (the surface grammar is unchanged; only internal identifiers move).

**Tests**
- `test/Precept.Tests/ConstructsTests.cs:418-419, 438-439, 593` — `ConstructionRow` removed from the parser-internal-kinds set; `ConstructionRowReject` renamed to `EventRowReject`. The "On has 2 candidates" theory case (line 593) stays at 2 — `EventRow` and `EventEnsure` are the two direct-dispatch candidates (`EventRowReject` is produced by `ResolveRejectVariant`, not dispatch — same as before the rename).
- `test/Precept.Tests/Parser/ParserConstructionRowTests.cs` — fact name `ConstructionRowReject_EmitsCorrectKind` becomes `EventRowReject_EmitsCorrectKind`; assertions update; class XML doc updated. Optionally rename file to `ParserEventRowTests.cs` to reflect that all on-row routing now lives in one place; defer the file rename to keep this PR focused on identifier renames (file rename is a separate diff slice).
- `test/Precept.Tests/GraphAnalyzer/GraphAnalyzerConstructionTests.cs` — test names and comments referencing `ConstructionRow*` remain valid as **conceptual** labels (the tests describe construction semantics, not parse kinds). Comment on line 20 (`"A single ConstructionRowReject with a guard..."`) updates to `EventRowReject`.
- `test/Precept.Tests/TypeChecker/TypeCheckerConstructionStructuralTests.cs` and `test/Precept.Tests/ProofEngine/ProofEngineConstructionTests.cs` — no rename required at the test level (they exercise construction *semantics*, which remain a thing — Decision 3 does not abolish the concept, only collapses the parse-kind for it). Comments verified.

## 4. Decisions

### Decision 1 — Delete `ConstructKind.ConstructionRow = 19`

**Stakes**: Medium. The enum value is part of the public catalog surface; downstream consumers may have switched on it. However, both the catalog `GetMeta` arm and the grammar-doc note explicitly tell readers it is vestigial and "new code should not produce it" — the public contract is already "do not depend on this."

**Decision**: Delete the enum value entirely. No retain-as-vestigial-stub, no rename-to-`Deprecated*`. The ordinal `19` is retired and not reused (skip it; next new kind takes the next free ordinal).

**Rationale**:
- Verified-unused-by-parser. Grep confirms no production site emits `ConstructKind.ConstructionRow`. `Parser.cs:194` matches on it only as a possible *input* to `ResolveRejectVariant` reject-mapping, where it falls through the `_ => baseKind` arm because the parser never actually arrives there (the construction routing was removed in Slice 8b; the parser only produces `EventRow` for `on Event -> ...`).
- Verified-unused-by-type-checker. `TypeChecker.Normalization.cs:55-67` iterates `ConstructKind.EventRow` and `ConstructKind.ConstructionRowReject`. There is no iteration of `ConstructKind.ConstructionRow`. The doc comment on line 48 explicitly states "Slice 8b removed [it]; success-path construction rows now arrive as `EventRow`."
- Verified-unused-by-downstream. The only references in `tools/` and `test/` are in catalog-membership tests (`ConstructsTests.cs:418, 438`) that exclude it precisely because it has no disambiguation entries, and in the `Constructs.GetMeta` switch arm itself. No tooling pattern-matches on it. No MCP DTO references it.
- The grammar-doc note (line 231) and the catalog description (`Constructs.cs:190`) both telegraph "do not produce this." Anyone who took those notes seriously is already not depending on it. Keeping it ships a footgun (a downstream consumer could pattern-match on the DU, hit the never-emitted arm, and never know).
- Catalog discipline (per `CLAUDE.md` "Catalog System (Non-Negotiable)"): the catalog *is* the language spec. A documented-vestigial entry pollutes the spec. If the entry doesn't describe a grammar form the parser produces, it shouldn't be in the catalog.

**Alternatives considered + rejected**:
- *Retain as vestigial DU member.* Rejected. The retention rationale ("avoid breaking downstream consumers that pattern-match on it") is moot — there are no such consumers in this repo, and we ship no public NuGet that locks the surface. Continuing to ship it perpetuates the documentation-vs-code drift this design exists to eliminate.
- *Rename to `LegacyConstructionRow` or `Deprecated_ConstructionRow`.* Rejected. Renaming an unused enum value doesn't change its uselessness; it just adds a search-term for archaeologists. Cleaner to remove.
- *Reuse ordinal 19 for the next new ConstructKind.* Rejected. Enum-ordinal reuse is a footgun if any future external consumer ever cached the old mapping. Skipping `19` is cheap.

**Precedent**:
- C#/Roslyn `SyntaxKind` and `SymbolKind`: Roslyn does retain deprecated values in these enums for binary compatibility (e.g., `SymbolKind.Discard` was added late). Precept's catalog is *not yet* under that binary-compatibility constraint — we have not shipped the catalog as a public surface to non-Precept consumers, so this constraint does not apply.
- TypeScript compiler (`SyntaxKind`): historically very stable, but the team has on multiple occasions removed values when the corresponding grammar form was retired (e.g., older JSX edge-case kinds). The pattern is "retire when you can."
- Prior Precept decision: Slice 8b removed `ConstructionRow` from the parser surface but kept the enum value "for safety." Two slices later there are still zero consumers. The safety reservation has expired.

**Tradeoff accepted**: A hypothetical out-of-tree consumer who switched on `ConstructionRow` will hit a compile error after this change. Mitigation: this is a deliberate breaking change called out in the migration note. Anyone who took the doc-warning seriously won't be affected.

**Sources consulted**:
- `ConstructKind.cs:51`: `/// <summary><c>on Event [when Guard] -> Actions</c> (construction success path)</summary>` → `ConstructionRow = 19,`
- `Constructs.cs:190`: `"Construction success path (Slice 8b: no longer produced by parser; all on-rows parse as EventRow and are promoted by the type checker via resolvedEvent.IsInitial)"`
- `docs/language/precept-grammar.md:231`: `"The catalog still defines ConstructionRow for historical compatibility, but Slice 8b removed it from the parser surface […] retained as a vestigial DU member to avoid breaking downstream consumers […] new code should not produce it."`
- `TypeChecker.Normalization.cs:45-50` doc-comment: `"Slice 8b removed parser production of ConstructKind.ConstructionRow — success-path construction rows now arrive as ConstructKind.EventRow and are classified semantically via the bound event's IsInitial flag […]"`

### Decision 2 — Canonical name for the reject variant is `EventRowReject`

**Stakes**: Low-to-Medium. Pure rename; no semantic shift. Touches one enum value, one catalog arm, one parser line, one normalization site, and ~6 test/doc references.

**Decision**: Rename `ConstructKind.ConstructionRowReject` → `ConstructKind.EventRowReject`. Keep ordinal `20`.

**Rationale**:
- *Symmetry with the typed side already in production.* `SemanticIndex.cs:473, 491, 499` already names the typed records `TypedEventRow` (base), `TypedEventRowSuccess`, `TypedEventRowReject`. A reader who follows the pipeline finds `ConstructKind.ConstructionRowReject` becoming `TypedEventRowReject` with no explanation. Aligning the parse-side name removes that asymmetry.
- *Symmetry with `TransitionRowReject`.* The transition-row family already uses the unmarked-success / explicit-reject naming convention: `TransitionRow` + `TransitionRowReject`. The event-row family should follow the same convention: `EventRow` + `EventRowReject`. Naming the reject variant after a different concept (construction-row) breaks the family pattern.
- *Doc usage already favors `EventRowReject`.* Grep shows the grammar doc uses `EventRowReject` six times (lines 119, 432, 433, 480, 483, 919, 920) and `ConstructionRowReject` twice (lines 228, 229). The doc has already migrated; only the table-row at line 228 and its caption at line 229 lag. The code is the laggard, not the doc.
- *Single-pipeline-name principle.* A construct should have one name from parse through type-check through diagnostic emission. Today the same construct is `ConstructionRowReject` (parser/catalog) and `TypedEventRowReject` (typed). One name, one concept.

**Alternatives considered + rejected**:
- *Rename the typed side instead* (`TypedEventRowReject` → `TypedConstructionRowReject`). Rejected. The typed-side naming is correct — these rows are not exclusively construction-related (they can also be stateless event-handler rejects in stateless precepts; see Decision 3 below). The parse-side naming is what's wrong.
- *Pick a new name for both sides* (e.g., `EventHandlerReject` + `TypedEventHandlerReject`). Rejected. Renaming all three (parse, typed, doc) costs more than renaming one (parse). The "EventRow" name is established (six grammar-doc uses, an entire `TypedEventRow` DU in production); changing it has no payoff.
- *Keep `ConstructionRowReject` and fix the doc to match.* Rejected. (a) The doc has already converged on `EventRowReject`; backsliding the doc is the wrong direction. (b) The name `ConstructionRowReject` is inaccurate post-Slice-8b — the parser does not know whether the row is a construction row; only the type checker classifies that via `resolvedEvent.IsInitial`. Naming the parse kind after a semantic concept the parser cannot determine is misleading.

**Precedent**:
- `TransitionRow` / `TransitionRowReject` — the in-family naming convention is already "base + Reject suffix." `EventRow` + `EventRowReject` matches.
- `TypedEventRow` / `TypedEventRowSuccess` / `TypedEventRowReject` — the typed-side DU naming is established and stable; the parse side should mirror it.
- F# discriminated unions and Rust enums conventionally name a "success vs failure" pair as `Foo` / `FooFailure` or `Foo` / `FooError`, not by introducing an unrelated noun in the failure-side label. The "ConstructionRowReject" name violates this convention by importing the `Construction` qualifier from a different concept (initial-event classification).

**Tradeoff accepted**: Six test-file and comment references must move. Mechanical, IDE-supported. The other rename targets (`Parser.cs:299`, `TypeChecker.Normalization.cs:61-63`, `Constructs.cs:197`) are equally mechanical.

**Sources consulted**:
- `ConstructKind.cs:53-54`: `/// <summary><c>on Event [when Guard] -> reject "reason"</c> (construction reject path)</summary>` → `ConstructionRowReject = 20,`
- `Constructs.cs:197-205`: `ConstructKind.ConstructionRowReject => new(kind, "construction row reject", "Reject path for on-rows: refuses event with a reason (produced by ResolveRejectVariant from EventRow, not via direct disambiguation)", […])`
- `Parser.cs:299`: `ConstructKind.EventRow => ConstructKind.ConstructionRowReject,`
- `SemanticIndex.cs:499`: `public sealed record TypedEventRowReject : TypedEventRow`
- `docs/language/precept-grammar.md:480`: ``on  EventName  ->  reject  "reason"                                    → EventRowReject``

### Decision 3 — Resolve the `EventRow = 12` namespace concern by *not renaming* — `EventRow` is the single parse kind for **all** `on Event -> Actions` forms, with the type checker deciding construction-vs-handler

**Stakes**: Medium. This codifies the post-Slice-8b reality but locks it in catalog metadata; reversing it later would require re-introducing a parse-time fork.

**Decision**: `ConstructKind.EventRow = 12` is the unified parse kind for both stateless-precept event handlers (`on Event -> Actions` with no states) and stateful-precept construction-success rows (`on InitialEvent -> Actions`). Classification — handler vs construction — is performed by the type checker via `resolvedEvent.IsInitial`. The catalog description is updated to acknowledge this dual role explicitly.

**Rationale**:
- *This is already the implementation.* `TypeChecker.Normalization.cs:55` reads `manifest.ByKind[ConstructKind.EventRow]` and normalizes each into a `TypedEventRow`; the per-handler classification (`Is this a construction row?`) happens via `NormalizeEventHandler` consulting `resolvedEvent.IsInitial`. The catalog description on `Constructs.cs:180` already says: *"event row […] serves both stateless handlers and construction rows; type checker classifies construction via resolvedEvent.IsInitial."* The design simply makes the catalog and the docs consistent with the code.
- *Construction is a semantic property, not a syntactic one.* The grammar form `on Event -> Actions` is identical regardless of whether the event happens to be `initial`. Forking a parse kind on a property the parser cannot independently determine (the parser does not bind event references) would require the parser to either look ahead into the event-declaration table — violating the streaming-parser invariant — or commit a wrong kind and have a later stage rewrite it. Slice 8b removed the latter; this design ratifies the removal.
- *Catalog-driven philosophy* (`CLAUDE.md` "Catalog System"): metadata drives behavior; pipeline stages do not switch on enum identity to dispatch per-member behavior. Forking `EventRow` → `StatelessEventRow` / `ConstructionEventRow` would invite exactly that anti-pattern downstream — every consumer would have to switch on which one. The unified `EventRow` + typed-side `TypedEventRowSuccess` (carrying `IsConstruction` if needed) keeps the dispatch in the typed-side DU subtype, where it belongs.
- *Stateless precepts are first-class* (per `docs/philosophy.md`: "States are optional — stateless precepts are first-class"). The naming should not privilege the stateful-construction interpretation. `EventRow` is neutral; `ConstructionEventRow` would not be.

**Alternatives considered + rejected**:
- *Split into `StatelessEventRow` and `ConstructionEventRow`.* Rejected. (a) The parser cannot distinguish them — same grammar form, same tokens, only differs on a semantic property (the event's `initial` flag) the parser does not bind. Forcing the split would require a parser look-back into the event table. (b) It would multiply downstream pattern-matches and re-introduce the `kind switch { … }` anti-pattern.
- *Merge into a single kind disambiguated by metadata `IsInitial` flag on the construct.* Rejected as already the de-facto state — there is no separate `IsInitial` flag on the construct, but the *event* carries it, and that's what the type checker reads. Adding a redundant flag on the construct would be parallel-state with the event declaration, a catalog violation.
- *Rename `EventRow = 12` to `OnEventRow` or `EventHandlerOrConstructionRow` for clarity.* Rejected. The name `EventRow` is fine and matches the established `TypedEventRow` typed-side. A more-descriptive name (`EventHandlerOrConstructionRow`) is unwieldy and still misleading (it's not "or" — the row IS an event row; the construction-ness is a property of the bound event, not the row).

**Precedent**:
- TypeScript's `BinaryExpression`: a single AST node serves `+`, `-`, `<`, `&&`, etc. Operator-specific semantics are determined post-parse via the bound operator. The parser does not fork the AST kind per operator. Precept's `EventRow` is the same shape — one parse kind, semantic interpretation downstream.
- Roslyn's `InvocationExpressionSyntax`: serves method invocations, delegate invocations, and dynamic invocations. Classification happens in binding, not parsing. Same model.
- Prior Precept decision (Slice 8b): explicitly removed the parse-time fork between `EventRow` and `ConstructionRow`. This design ratifies that.

**Tradeoff accepted**: A reader inspecting only `ConstructKind` cannot tell whether a given `EventRow` is a stateless handler or a construction row — they must follow the type-checker output. Mitigation: the catalog description on `EventRow` now says so explicitly, and the type-checker doc (`type-checker.md:452`) already documents the classification model. The catalog comment will be expanded to drop the lingering "Slice 8b: no longer produced by parser" disclaimer (now historical) and lead with the dual-role description.

**Sources consulted**:
- `Constructs.cs:177-185`: `ConstructKind.EventRow => new(kind, "event row", "Event handler with optional guard and actions — serves both stateless handlers and construction rows; type checker classifies construction via resolvedEvent.IsInitial", "on UpdateName -> set name = newName", [], [SlotEventTarget, SlotPreVerbGuardArrow, SlotActionChain], [new(TokenKind.On, [TokenKind.Arrow])], RoutingFamily.EventScoped),`
- `TypeChecker.Normalization.cs:44-50`: `"Iterate all ConstructKind.EventRow and ConstructKind.ConstructionRowReject constructs from the manifest, resolve each to a TypedEventRow, and accumulate into CheckContext.EventHandlers. Slice 8b removed ConstructKind.ConstructionRow — success-path construction rows now arrive as ConstructKind.EventRow and are classified semantically via the bound event's IsInitial flag in NormalizeEventHandler."`
- `docs/compiler/type-checker.md:452`: *"The parser produces the same `EventRow` construct for both — there is no separate `ConstructionRow` parse kind. Classification happens in the type checker, by reading `resolvedEvent.IsInitial` on the bound event."*
- `docs/philosophy.md` (paraphrased; verbatim quote not in current grep): *"States are optional — stateless precepts are first-class."*

## 5. Acceptance criteria

1. `grep -r "ConstructionRow" src/ tools/ test/` returns zero matches (excepting `docs/Working/Archive/` and the audit appendix files, which are historical).
2. `grep -r "ConstructionRow" docs/language/` returns zero matches.
3. The full test suite passes (`dotnet test`), including the `ConstructsTests` membership theories that previously excluded the two parser-internal kinds.
4. The `precept_compile` MCP tool returns the same diagnostic set for every sample file before and after the change (verified on `samples/*.precept`).
5. The grammar-doc construct table at `precept-grammar.md:211-229` lists exactly 14 ConstructKinds (15 − 1 deletion = 14). The "14 construct kinds" doc claim becomes accurate without doc-side adjustment.
6. `ConstructKind` enum has values `1..12, 19 unused, 20, 21` (ordinal 19 is skipped, not reused).
7. No public `Precept.dll` API surface change beyond the renamed enum value (which is a deliberate breaking change for any out-of-tree consumer).

## 6. Dependencies

**Upstream**: None. This design depends only on the post-Slice-8b state of the parser, which is shipped.

**Downstream**:
- F-LANG-CAT-03 (the catalog-count drift finding) — this change reduces `ConstructKind` count from 15 to 14, closing one of the count discrepancies that finding cites.
- F-LANG-GRAM-03 / -04 / -05 (other grammar-doc count drifts) — independent; do not block.
- The Phase-4 plan entry for F-LANG-GRAM-01 / -02 — this design consolidates the two findings into one execution slice.

**No coupling to**: runtime API, MCP DTO surface, language-server behavior (no LSP feature consumes the renamed kind directly; all LSP code pattern-matches on the typed side).

## 7. Doc-update enumeration

Per the `CLAUDE.md` routing table:

| Change kind | Doc to update |
|---|---|
| Catalog entry removed (`ConstructionRow`) and renamed (`ConstructionRowReject` → `EventRowReject`) | `docs/language/precept-grammar.md` — construct table (lines 211-229), `on`-family routing diagram (line 119), disambiguation pseudo-table (lines 432-433), `on` family section (lines 480, 483), final routing block (lines 919-920), delete the vestigial-`ConstructionRow` note (line 231) |
| Implementation state of catalog | `docs/language/catalog-system.md` — verify it does not enumerate ConstructKind members (it references catalogs by name, not by count, per the doc's own self-description); confirm no count update needed |
| Diagnostic surface | No change. `ZeroConstructionRows` diagnostic name stays; it names a *semantic* property (construction rows are an emergent concept from `EventRow + IsInitial`), not the parse kind. |
| Type-checker doc | `docs/compiler/type-checker.md:452` — already correct; verify the post-rename text reads coherently (no edits expected). |
| Pipeline parser doc | `docs/compiler/parser.md` (or equivalent stage doc) — verify references to `ConstructionRow*` are gone; update §Implementation State if it lists the post-Slice-8b state. |
| Working plan entry | `docs/Working/compiler-readiness-plan-2026-05-24.md:692-693` — when execution ships, mark F-LANG-GRAM-01 and -02 as resolved together. |

Audit appendices (`docs/Working/compiler-readiness-review-2026-05-24-appendices/`) are historical artifacts — do **not** update.

## 8. Open questions

- **Phase targeting.** F-LANG-GRAM-01 and -02 are currently listed as Phase 4 findings (`compiler-readiness-plan-2026-05-24.md:692-693`). The change is small (one rename + one delete + ~8 file touches) and doc-only-coupled. Owner ratification needed: ship in Phase 4 as planned, or absorb into a small Phase 3.5 / Phase 1 follow-up slice? Recommendation in §10.
- **Test-file rename.** Should `test/Precept.Tests/Parser/ParserConstructionRowTests.cs` be renamed to `ParserEventRowTests.cs` in the same slice, or deferred? Recommendation: defer (file rename adds review noise; the test contents and class XML doc convey the post-rename semantics adequately).
- **Comment-level usage of "construction row" as a concept name.** Tests like `AlwaysRejecting_ConstructionRow_IsError` and `ConstructionRow_IncludedInReachability` use "ConstructionRow" as a *concept* name (the semantic notion of a row that constructs the entity), not the parse-kind name. These should remain unchanged — the concept persists even though the parse-kind for it doesn't. **Confirm with owner that this interpretation is intended** before shipping.

## 9. Falsifiers

This design is wrong if:
- A downstream consumer (in `tools/Precept.Mcp`, `tools/Precept.LanguageServer`, or `tools/Precept.VsCode`) is found to pattern-match on `ConstructKind.ConstructionRow`. Verified by exhaustive grep on the date of this draft: **no such consumer exists.** Re-verify at execution time.
- The parser is found to emit `ConstructKind.ConstructionRow` from any path. Verified by grep on `Parser.cs`, `NameBinder.cs`, `TypeChecker.cs`: **no production site exists.** The fall-through arm in `ResolveRejectVariant:301` (`_ => baseKind`) is the only mention, and it is never reached for the `EventRow` input (which is special-cased on line 299).
- The construction-row semantic concept turns out to need a *parse-time* discrimination (e.g., a future language feature that constrains parser grammar for construction rows differently from event rows). If such a feature emerges, Decision 3 would need revisiting — but it is not in the current plan, and stateless precepts being first-class makes this unlikely.
- A public API contract (NuGet, MCP schema) is found to expose `ConstructionRow` by ordinal or by name to external consumers. **Verify before execution.** The MCP DTOs in `tools/Precept.Mcp/Dtos/` should be reviewed at execution time.

## 10. Phase-target rationale

**Recommendation**: Land this in **Phase 4** as currently planned, alongside the other grammar-related findings (F-LANG-GRAM-03 / -04 / -05) and the collection-grammar work that Phase 4 already groups. The combined diff is small, the rename mechanics are mechanical, and bundling avoids two doc-touch cycles on `precept-grammar.md`.

**Why not a smaller / earlier slice**:
- Phase 1 (doc foundation) is doc-only; the code rename component does not fit.
- A standalone Phase 1.5 / Phase 3.5 slice would require its own PR ceremony for a ~10-line code change.
- The risk profile is low — no semantic shift, no runtime impact, fully tested by the existing construction-row test suite.

**Why not deferred to Phase 5+**:
- The drift surfaces every time a reader follows a stack trace, a diagnostic, or a doc cross-link through this corner of the catalog. Each day it lingers is one more day of "what does the doc mean, exactly?" friction. Phase 4 is the next natural landing slot; do not push further.

**Phase 4 plan integration**: When execution starts, update `compiler-readiness-plan-2026-05-24.md:692-693` to point at this design doc as the authoritative spec, then ship the rename in a single PR alongside the F-LANG-GRAM-03 / -04 / -05 doc updates so the grammar-doc lands once with all four findings resolved.
