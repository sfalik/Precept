# Decision: Language Server Design Finalization

> **Author:** Frank · **Date:** 2026-05-09 · **Scope:** Language Server architecture

## Decisions Made

### D1: Honor the Stub API Contract

**Decision:** The 173 ported LS tests define the public API contract via `LanguageServerStubs.cs`. Implementations replace `throw NotImplementedException` with real logic, preserving the same method signatures. No test refactoring required.

**Rationale:** The stubs were designed to let tests compile ahead of implementation. Changing signatures now would invalidate 173 tests and introduce unnecessary churn.

**Alternative rejected:** Redesign the test-facing API to match the design doc's handler-per-file structure. Rejected because it adds test migration work with no behavioral benefit — both approaches route to the same underlying logic.

**Tradeoff:** The LS has two parallel API surfaces — the OmniSharp handler classes (used by the editor) and the stub classes (used by tests). Both delegate to shared internal logic. Acceptable for a component this thin.

### D2: Temporary ConstructKind Switch for Document Outline

**Decision:** Until `ConstructMeta.IsOutlineNode` and `LspSymbolKind` are added to the catalog, the Document Outline handler uses a private `ConstructKind → SymbolKind` mapping for four construct kinds (PreceptHeader, FieldDeclaration, StateDeclaration, EventDeclaration).

**Rationale:** The catalog gap (CC#18 resolved in design but not in code) should not block the outline feature. The mapping is isolated in one private method (~10 lines), trivially replaceable.

**Alternative rejected:** Add `IsOutlineNode` and `LspSymbolKind` to `ConstructMeta` as part of this work. Rejected because catalog changes require their own exhaustive-switch updates and test coverage — mixing them into the LS implementation adds scope and risk.

**Tradeoff:** Temporary violation of the "no ConstructKind switch" principle. Deliberately scoped to one private method with a `// TODO: Replace with c.Meta.IsOutlineNode when catalog fields land` comment.

### D3: Levenshtein in LS, Not Core

**Decision:** The Levenshtein distance algorithm lives in `tools/Precept.LanguageServer/LevenshteinDistance.cs`, not in `src/Precept/`.

**Rationale:** Fuzzy matching is a tooling concern — the compiler never needs it. Placing it in the LS keeps `src/Precept/` focused on compilation and keeps the LS dependency minimal.

**Alternative rejected:** Add to core pipeline as a general utility. Rejected because no other pipeline stage or consumer needs edit distance.

### D4: IsUserFacing Workaround via Token Null Check

**Decision:** Instead of waiting for `TypeMeta.IsUserFacing`, completion filtering uses `t.Token != null` to exclude internal types (`Error`, `StateRef`).

**Rationale:** Internal types have `Token = null` because they have no surface keyword. This is a structural invariant, not a heuristic. When `IsUserFacing` lands, the filter can be updated, but behavior is identical either way.

### D5: Preview Handler Ships as Shell

**Decision:** Slice 10 (Preview/Inspect) ships the OmniSharp handler registration and request routing, but actual execution depends on the runtime evaluator (Phase 3), which is entirely stubbed. Tests remain red.

**Rationale:** The handler structure and request/response types are known. Shipping the shell ensures the LS capability declaration is complete and the VS Code extension can discover the custom method. Blocking on the evaluator would delay the entire LS.

## Open Items for Shane

1. **ConstructMeta catalog change:** `IsOutlineNode` and `LspSymbolKind` should be added as a separate slice (not part of this LS work). Recommend George handles this alongside other catalog additions.
2. **George-15's DiagnosticMeta additions:** When `TriggerCondition`/`RecoverySteps`/`ExampleBefore`/`ExampleAfter` land, Slice 5 (hover) and Slice 8 (code actions) should be enriched with this metadata. This is an additive enhancement, not a redesign.
3. **VS Code `precept.showFixHint` command:** Slice 8 (Code Actions) will register tooltip-only code actions that invoke this command. The command must be registered in the VS Code extension's `package.json`. This is a one-line contribution to the extension.
