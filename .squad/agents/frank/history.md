## Core Context

- Owns language research, spec wording, and cross-surface documentation for Precept's DSL and architecture.
- Converts owner/design decisions into implementable guidance for parser, catalog, type-checker, and tooling work.
- Durable active baseline: catalogs remain the language truth; generic consumer flow should dispatch by `SlotValue` shape instead of construct identity; the generic `ParsedConstruct` direction remains acceptable for consumers, and any accessor layer stays deferred until a concrete need exists.

## Learnings

- LS enrichment features (did you mean? / code actions) require three catalog structure changes before LS implementation: (1) `Diagnostic.Args: ImmutableArray<string>` to carry raw template args through the compiler artifact; (2) `DiagnosticMeta.SuggestionSources: SuggestionSource[]?` to bind naming diagnostics to their fuzzy-match sources without per-code switches in the LS; (3) `ConstructMeta.ModifierDomain: ModifierDomain` to bind construct kinds to modifier DU subtypes without per-kind switches in the LS code action provider. Both `SuggestionSource` and `ModifierDomain` stay bare enums — no per-member metadata, classification axes only. Naming-error "did you mean?" candidates: `UndeclaredField` → `SemanticIndex.Fields`; `UndeclaredState` → `SemanticIndex.States`; `UndeclaredEvent` → `SemanticIndex.Events`; `UndeclaredFunction` → `Functions.All`. `SemanticIndex` is unavailable for Lex/Parse-stage diagnostics; LS must guard accordingly.

- The `tree` variable name sweep (2026-05-03) found stale references in 7 files: `Compiler.cs`, `CompileRunner/Program.cs`, `ConstructsTests.cs`, `compiler-and-runtime-design.md`, `precept-language-spec.md`, `tooling-surface.md`, and `language-server.md`. All Roslyn `SyntaxTree` usages in analyzer tests/code are legitimate and left alone. Archived docs are not updated. The `docs/compiler/type-checker.md` still has many `SyntaxTree` type-name references (not caught by `\btree\b` word boundary) that will need a separate pass.

- `docs/compiler-and-runtime-design.md` is the narrative overview layer over the 11 canonical stage docs; it inherits open questions and cross-references the stage docs rather than silently resolving them.
- `SemanticIndex` is a flat semantic inventory, not a mirrored tree; any wording that frames it as annotated syntax is architectural drift.
- Catalog-first propagation means “add a catalog entry and keep consumers generic,” not “add an enum member and fill an exhaustive switch.”
- The live generic parser contract is `ParsedConstruct(ConstructMeta, ImmutableArray<SlotValue>, SourceSpan)` plus `SyntaxTree.Diagnostics`; unresolved SlotValue subtype shape mismatches stay explicit until Shane locks them.
- Grammar `Tag(...)` captures are the slot system in the radical parser design; do not reintroduce `ConstructMeta.Slots` as mirrored truth.
- Outcomes need metadata when outcome-level meaning is compositional (`no transition` remains the durable example).
- The remaining explicit catalog-thesis tooling gaps are still the hand-authored TextMate grammar and the hardcoded MCP `firePipeline` array.
- Tree-shaped naming for the flat parser artifact remains suspect; Shane's current preference is `ConstructManifest` if the `SyntaxTree` rename moves forward.

- The 2026-05-03 `SyntaxTree` doc sweep confirmed the missed type-name drift in `docs/compiler/type-checker.md` and `docs/compiler/README.md`, and also cleaned stale internal references in `tooling-surface.md`, `language-server.md`, `compiler-and-runtime-design.md`, `precept-builder.md`, `fault-system.md`, and multiple archived design notes. The only remaining `SyntaxTree` mention under `docs/` is the intentional Roslyn reference in `docs/working/Archived/type-checker-research-crossref.md`; `dotnet build` stayed green after the sweep.
- Grammar anatomy for `StateEnsure` / `EventEnsure` must model `EnsureClause` and `BecauseClause` as separate slots, mirroring `RuleDeclaration`; the `because` reason remains mandatory even though it is no longer described as embedded inside `EnsureClause`.

## Recent Updates

### 2026-05-03T14:28:59Z — ConstructManifest rename shipped
- Frank-26 completed the `SyntaxTree` → `ConstructManifest` rename across 5 source files and 2 docs.
- Build succeeded after the rename and no test changes were needed for the batch.

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

### 2026-05-03T14:37:24Z — Grammar doc accuracy confirmed against catalog
- Frank-27 completed a full review of docs/language/precept-grammar.md and corrected 9 material errors across slot-bearing examples, slot-kind totals, and invariant references.
- Durable baseline: the grammar doc now matches catalog reality for StateEntryList, InitialMarker, GuardClause, and the distinct ActionChain + Outcome slot shape in TransitionRow.
- The active grammar reference should now be treated as accurate on the reviewed slot/routing details unless a later catalog change reopens them.



### 2026-05-03T14:59:24Z — ConstructManifest doc cleanup and slot rulings recorded
- Frank-29 swept stale `SyntaxTree` type-name references from the requested compiler docs and adjacent surfaces; build stayed clean. Commit `8baca9f`.
- Frank-30 locked `because` as a separate `BecauseClause` slot for ensure syntax; `RuleDeclaration` is the correct reference shape and `StateEnsure` / `EventEnsure` are the defect sites.
- Frank-31 locked the event-modifier shape to an individual `InitialMarker` slot and confirmed `terminal` remains a state modifier, not an event modifier.

### 2026-05-03T15:18:05Z — Catalog diagram baseline and ownership routing recorded
- Frank-34's research memo is now the durable baseline for schema-diagram work: the live catalog system is 13 catalogs because `ExpressionForms` is in scope, and `ConstructSlotKind` is supporting schema rather than a catalog.
- User routing directive updated: Elaine owns both Mermaid and ASCII diagram rendering. Frank remains the architectural analyst/decision source for what the diagrams should communicate.
- The because-clause ledger closeout is also recorded: grammar docs already match the separate `EnsureClause` + `BecauseClause` slot anatomy, and George's optional-slot follow-up closed the last catalog-red defect.
