## Core Context

- Owns language research, spec wording, and cross-surface documentation for Precept's DSL and architecture.
- Converts owner/design decisions into implementable guidance for parser, catalog, type-checker, and tooling work.
- Durable active baseline: catalogs remain the language truth; generic consumer flow should dispatch by `SlotValue` shape instead of construct identity; the generic `ParsedConstruct` direction remains acceptable for consumers, and any accessor layer stays deferred until a concrete need exists.

## Learnings

- `docs/compiler-and-runtime-design.md` is the narrative overview layer over the 11 canonical stage docs; it inherits open questions and cross-references the stage docs rather than silently resolving them.
- `SemanticIndex` is a flat semantic inventory, not a mirrored tree; any wording that frames it as annotated syntax is architectural drift.
- Catalog-first propagation means “add a catalog entry and keep consumers generic,” not “add an enum member and fill an exhaustive switch.”
- The live generic parser contract is `ParsedConstruct(ConstructMeta, ImmutableArray<SlotValue>, SourceSpan)` plus `SyntaxTree.Diagnostics`; unresolved SlotValue subtype shape mismatches stay explicit until Shane locks them.
- Grammar `Tag(...)` captures are the slot system in the radical parser design; do not reintroduce `ConstructMeta.Slots` as mirrored truth.
- Outcomes need metadata when outcome-level meaning is compositional (`no transition` remains the durable example).
- The remaining explicit catalog-thesis tooling gaps are still the hand-authored TextMate grammar and the hardcoded MCP `firePipeline` array.
- Tree-shaped naming for the flat parser artifact remains suspect; Shane's current preference is `ConstructManifest` if the `SyntaxTree` rename moves forward.

## Recent Updates

### 2026-05-03T14:18:15Z — Scribe post-batch sync recorded
- Merged the three Frank inbox files into `decisions.md`, deduplicating the overview-confirmation notes into the already-recorded compiler-overview sync while separately capturing Shane's `ConstructManifest` preference as the current rename target over Frank's `ParsedSource` recommendation.
- Wrote orchestration records for frank-23, frank-24, and frank-25; frank-25's `to` classification verification remains in flight with no canonical ruling yet.
- Summarized this history file into `history-archive.md` to bring Frank back under the 15 KB gate.

### 2026-05-03T14:02:40Z — Compiler overview and catalog-first wording batch recorded
- Frank's completed doc-sync pass remains the active baseline: the overview is narrative-only, the live parser contract is generic `ParsedConstruct`/`SlotValue`, `TypeKind` resolves in the checker, SemanticIndex back-pointers target `ParsedConstruct`, and the catalog count is 13 including `ExpressionForms`.
- The durable wording correction remains active: Precept extends by adding catalog metadata, not by adding enum members and downstream exhaustive switches; the “Precept Innovations” callout box still needs the same cleanup in a later pass.

### 2026-05-03T09:10:00Z — Catalog-thesis deviation audit baseline retained
- Frank's full 11-doc sweep still stands: the only real deviations from the catalog-driven thesis are the hand-authored TextMate grammar and the hardcoded MCP `firePipeline`, both already called out as tooling gaps rather than silent architectural drift.

### 2026-05-03T05:21:49Z — HandlesCatalog cleanup remains recorded
- The Option F follow-through still stands: consumer-side `[HandlesCatalogExhaustively]` / `[HandlesCatalogMember]` annotations and their reflection tests were removed, while the attribute types themselves remain valid for catalog-side use.
