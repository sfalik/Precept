## Core Context

- Owns language research, spec wording, and cross-surface architecture documentation for the Precept DSL and runtime.
- Catalog metadata remains the language truth; runtime, tooling, and docs must derive behavior from durable catalog shape rather than enum-identity switch logic or parallel lists.
- Public API surfaces expose stable CLR/JSON interchange contracts; evaluator internals stay internal.
- Investigation docs can be archived once their outcomes are captured in canonical docs, proposals, or the squad decision ledger.

## Learnings

- `SemanticTokenTypes` is now the approved 14th catalog; `TokenMeta` should carry one `VisualCategory`, and token-surface projections must derive from catalog metadata rather than parallel token fields.
- The production LS gap-closure plan is Phase 2 in `docs/Working/language-server-implementation-plan.md`: expression/default-position completions, catalog-complete hover, navigation, selection/document symbols, semantic-token cleanup, version ordering, and related VS Code polish.
- Outline metadata is settled in catalog form: `ConstructMeta.IsOutlineNode` + `OutlineSymbolTag`, with the LS projecting the string tag to `SymbolKind` instead of pulling LSP protocol types into `src/Precept/`.
- `DiagnosticMeta` enrichment and `QuickstartCatalog`/`SyntaxReference` authoring metadata are already present in source and should be consumed rather than re-described elsewhere.
- Typed-literal work stays inside the 12-slice plan: `ContentValidation` is the metadata hook, compile-time literal validation goes through `TypedConstantValidation.Validate(...)`, and runtime JSON lanes go through `TypeRuntime<T>` / `TypeRuntimeMeta`.
- ISO 4217 and UCUM remain embedded external reference datasets, not Precept catalogs; Precept-owned augmentation lives in source metadata on top of those datasets.
- The focused AI-authoring MCP suite remains the durable authoring direction; `precept_language` is fallback/internal while named tools own discovery.
- The 52-bug audit made the current highest-risk gaps explicit: parser routing/disambiguation still ignores catalog grammar in multiple places, MCP definition/docs DTOs still flatten or omit catalog-derived structure, and several type-checker result types still come from hardcoded operator dispatch instead of `Operations` metadata.
- Highest-leverage prevention layer: catalog-reflection fixture tests plus real-catalog contract tests (especially MCP definition matrices, parser routing/disambiguation, keyword-collision accessors, and hook branches).

## Historical Summary

- Earlier May 2026 work locked the typed-literal boundary, the external-data posture for ISO/UCUM, the catalog-driven parser/checker trajectory, and the requirement that durable rationale live in decisions/research instead of scattered implementation switches.
- Recent batches settled the LS baseline: Slice 0/0b infrastructure and shim cleanup, `TypedField.NameSpan`, `ArgReference` recording, snippet-template metadata, and the Phase 2 production gap-closure plan.
- Use `.squad/decisions.md` for the exact batch chronology and `docs/` / `research/` for the surviving canonical rationale.

## Recent Updates

### 2026-05-10T03:13:51Z — Bug cluster analysis merged and operationalized
- The 52 confirmed toolchain bugs are now durably classified by stage: Parser 17, MCP serialization 15, Type Checker 10, Name Binder 4, Proof Engine 3, MCP docs 3.
- Dominant causes are now locked: parser/catalog drift, MCP DTO projection drift, and hardcoded type-checker operator behavior where catalog metadata should drive the result.
- Scribe merged the analysis with Soup-Nazi's testing verdict into one canonical decision entry, and Kramer's Track 2 status table makes the register executable for follow-up work.

### 2026-05-10T02:50:04Z — Visual taxonomy and LS Phase 2 direction recorded
- `SemanticTokenTypes` is the approved catalog surface for token visual categories, and constrained events stay in the shared italic constraint system.
- LS Phase 2 is the active production gap-closure plan, with `set` in type position called out as the sharpest cross-surface bug to fix contextually.

### 2026-05-09T23:46:43Z — LS docs reconciled and field-span prerequisite closed
- The LS design/plan docs were reconciled to the live source, and `TypedField.NameSpan` landed as the thin-core prerequisite that unblocks name-based editor projections.
- The remaining open LS contract question from that batch is still the `precept/inspect` restore-failure surface.
