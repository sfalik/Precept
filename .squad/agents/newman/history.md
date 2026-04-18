## Core Context

- Owns MCP server and plugin distribution surfaces, including DTO shape, tool contracts, and plugin/package correctness.
- Enforces the thin-wrapper rule: MCP should expose core behavior, not duplicate domain logic.
- Historical summary (pre-2026-04-13): drove MCP vocabulary/spec sync for new types and constraints, declaration-guard DTO design, and Squad automation/docs cleanup around the retired `squad:copilot` lane.

## Learnings

- MCP contract changes should be additive when possible and preserve existing consumer shapes.
- Catalog-driven vocabulary is the preferred path for exposing DSL keywords/types; non-token constructs need explicit catalog registration.
- Label/automation removals should be staged through sync workflows and disabled notices, not silent deletion.
- `typeKeywords` in `precept_language` is a flat string list — zero type metadata. Any new type family needs a companion `typeReference` section in `LanguageResult` or AI cannot self-service.
- `JsonConvert.ToNative` currently loses compound objects (`JsonValueKind.Object → GetRawText()`). Must resolve string-vs-object form contract before implementing compound-typed fields in runtime tools. String form (typed constant syntax) is preferred: simpler, AI-natural, no object deserialization complexity.
- `FieldDto` and `EventArgDto` have no qualifier properties — `in`/`of` constraints are invisible to AI compile output consumers. Add additive optional `UnitQualifier`/`DimensionQualifier` props.
- Interpolation syntax (`'{Amount} USD'`) belongs to DSL authoring context only. MCP runtime data values are literal strings or objects. Tool descriptions must explicitly distinguish — conflation is a predictable AI failure mode.

## Recent Updates

### 2026-04-18 — Currency/Quantity/UOM MCP Impact Review

- **Key file reviewed:** `docs/CurrencyQuantityUomDesign.md` (canonical design for 7 business-domain types)
- **Current DTO gap:** `FieldDto` and `EventArgDto` have no `unitQualifier`/`dimensionQualifier` props — `in 'kg'` and `of 'mass'` qualifiers are invisible to AI consumers. Need additive optional props.
- **Structural blocker:** `JsonConvert.ToNative` maps `JsonValueKind.Object → je.GetRawText()` — compound type values (`{ "amount": 4.17, "currency": "USD" }`) cannot be ingested. Must resolve string-vs-object form contract (string form recommended) before implementation.
- **Language tool gap:** `typeKeywords` is a flat string list — no type metadata. New `typeReference` section needed in `LanguageResult` for AI self-service.
- **AI legibility risk:** Interpolation syntax (`'{Amount} USD'`) is DSL authoring, NOT runtime data format. Tool descriptions must explicitly distinguish.
- **Precision note:** `GetDouble()` in JsonConvert loses decimal precision for new decimal-backed types; mitigated if string form is adopted for compound types.
- **Verdict:** MINOR UPDATE for compile/language tools; BREAKING CHANGE for runtime JsonConvert (structural gap, not behavioral regression).

### 2026-04-18 — Currency review batch consolidation

- Frank's design review blocked implementation until the `maxplaces` default, multi-basis cancellation semantics, and missing period accessor contracts are resolved.
- George aligned the runtime prerequisites behind the same gate: Issue #107 typed constants, Issue #115 precision, embedded registries, and the compound-value transport contract.
- Soup Nazi put the testing cost at roughly 310 tests and elevated duration/days plus chained cancellation to explicit blockers.
- Uncle Leo approved the direction with conditions but treated validator normalization order and Issue #115 framing as hard design-doc fixes.
- Net effect for MCP/AI: keep the additive DTO work queued, but do not commit to object-shaped compound values until the transport contract is explicitly decided.

### 2026-04-12 — Squad `squad:copilot` retirement cleanup
- Tracked the workflow/template/doc blast radius and kept `squad:chore` distinct from the retired coding-agent label.

### 2026-04-11 — Declaration guard DTO contract
- Locked the additive `precept_compile` expansion for invariants, state asserts, event asserts, and edit blocks with nullable `when`.
