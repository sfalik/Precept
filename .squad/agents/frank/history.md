## Core Context

- Owns language research, spec wording, and cross-surface architecture documentation for the Precept DSL and runtime.
- Catalog metadata remains the language truth; runtime, tooling, and docs must derive behavior from durable catalog shape rather than enum-identity switch logic or parallel lists.
- Public API surfaces expose stable CLR/JSON interchange contracts; evaluator internals stay internal.
- Investigation docs can be archived once their outcomes are captured in canonical docs, proposals, or the squad decision ledger.

## Learnings

- Language Server design at `docs/tooling/language-server.md` is implementation-ready across all 10 feature areas (diagnostics, two-pass semantic tokens, catalog-driven completions, hover, go-to-definition, preview/inspect, document outline, folding, diagnostic enrichment, code actions). Finalized implementation plan at `docs/Working/language-server-implementation-plan.md` — 13 slices (0a + 0–11) with method-level specificity, 173 existing red tests as the acceptance contract.
- `ConstructMeta.IsOutlineNode` + `OutlineSymbolTag` resolved as concrete Slice 0a (no longer deferred). Architectural decision: catalog stores `string? OutlineSymbolTag` (plain tag), LS projects via `Enum.Parse<SymbolKind>(tag)` — follows the `TokenMeta.SemanticTokenType` pattern. No LSP protocol dependency in `src/Precept/`. Field named `OutlineSymbolTag` not `LspSymbolKind` to avoid protocol coupling in catalog vocabulary.
- George-15's `DiagnosticMeta` enrichments (`TriggerCondition`, `RecoverySteps`, `ExampleBefore`, `ExampleAfter`) have landed in `src/Precept/Language/Diagnostics.cs` — no longer a prerequisite, Slices 5 and 8 consume immediately.
- `TypeMeta.IsUserFacing` gap resolved permanently: `Token != null` is structurally equivalent and is the permanent filter — no catalog change needed. Reframed from "workaround" to "permanent solution."
- No deferrals remain in the LS implementation plan. All "future work", "temporary", "may need", "when…land" language eliminated or resolved with concrete decisions (2026-05-09).
- The stub API in `LanguageServerStubs.cs` defines the test-facing contract for 173 ported tests. Implementations must satisfy these exact signatures — the OmniSharp handler layer delegates to the same classes. Two parallel API surfaces (OmniSharp handlers + stub classes) are acceptable for a component this thin (~500-700 LOC).
- `SemanticIndex` already carries `FieldReferences`, `StateReferences`, `EventReferences` (CC#3) — the reference site records needed for Pass 2 semantic tokens and go-to-definition. No reconstruction from typed expression trees needed; the type checker populates them at resolution time.
- `TypeMeta.IsUserFacing` is not on the record but `Token != null` is a structural equivalent for filtering internal types from completions. No catalog change required.
- Levenshtein distance for "did you mean?" enrichment belongs in the LS (`tools/Precept.LanguageServer/`), not in `src/Precept/`. Fuzzy matching is a tooling concern.
- Preview/Inspect (Slice 10) is blocked on the runtime evaluator (Phase 3 — entirely stubbed). Handler shell ships to complete the capability declaration; tests remain red until evaluator lands.
- Typed-literal pre-work is governed by`docs/Working/typed-literal-system-plan.md`, a 12-slice execution plan covering data loaders, parsers, validation framework updates, runtime stubs, and canonical doc sync. Work should not expand beyond that plan.
- Durable typed-literal boundaries are locked: `ContentValidation` remains the catalog hook; compile-time literal validation goes through `TypedConstantValidation.Validate(...)`; runtime Fire/Update/Restore JSON lanes go through `TypeRuntime<T>` / `TypeRuntimeMeta`; format-only validation stays in constraints, not typed literals.
- ISO 4217 and UCUM are external reference datasets, not Precept catalog metadata. They ship as embedded XML resources loaded into typed records, while Precept-owned augmentations like currency symbols stay in source.
- The UCUM evaluator-facing plan fixes are now explicit: `UcumParsedUnit` preserves annotations for display only, Slice 1d uses a two-phase/transitive loader for defined units, and Slice 10 interns runtime `Unit` instances by `(DimensionVector, UcumExactFactor)`.
- `precept_language` currently exposes only 9 language catalogs plus domains and a static `firePipeline` array (`tokens`, `types`, `modifiers`, `actions`, `constructs`, `constraints`, `operators`, `functions`, `diagnostics`). It does **not** yet expose `Operations`, `ExpressionForms`, `ProofRequirements`, `Outcomes`, `Faults`, or `SyntaxReference`, despite architecture docs claiming all 13 catalogs plus `SyntaxReference` should ground AI authoring.
- The biggest AI-authoring gaps are not missing keywords but missing legality and guidance: `TypeMeta.ContentValidation`, type/action/function/modifier examples, proof requirements, outcome forms, and operation compatibility are already present in source metadata but dropped by `LanguageTool.cs`. Payload size is also dominated by low-authoring-value sections (`ucumTier1Units`, tokens, diagnostics).
- AI authoring tool suite designed: 8 new tools (`precept_quickstart`, `precept_syntax`, `precept_types`, `precept_operations`, `precept_proofs`, `precept_patterns`, `precept_diagnostic`, `precept_domains`) plus existing `precept_language` and `precept_compile`. Named tools, not parameterized sections — tool names are the discoverability surface. Full design at `research/language/precept-ai-authoring-tool-suite.md`.
- Newman's 3-tool proposal was good architecture but insufficient coverage: missing operator/type legality (Operations), guard/proof obligations, outcome vocabulary, expression forms, diagnostic recovery, and common patterns. Those gaps cause first-try authoring failures in concrete scenarios (money arithmetic, collection guards, outcome syntax).
- `SyntaxReference.CommonPatterns` "Computed field" example is compile-invalid — it applies `nonnegative` to a computed field, which the parser rejects. Must be fixed before exposing patterns through MCP.
- Typical cold-start authoring session with the new suite costs ~57 KB (quickstart + syntax + patterns + types) vs. ~192 KB for `precept_language` alone — every byte is authoring-relevant.

## Recent Updates

### 2026-05-09T18:53:05-04:00 — LS plan no-deferrals amendment
- Converted `ConstructMeta.IsOutlineNode` + `OutlineSymbolTag` from catalog gap to concrete Slice 0a with method-level specificity, entry-by-entry values, and 8 named tests.
- Resolved architectural question: catalog stores `string? OutlineSymbolTag`, LS projects to `SymbolKind` — no LSP dependency in core.
- Confirmed George-15's `DiagnosticMeta` enrichments already landed — upgraded from "prerequisite" to "available now" in Slices 5 and 8.
- Eliminated all deferred/temporary/may-need language from the plan. Decision record at `.squad/decisions/inbox/frank-ls-plan-no-deferrals.md`.

### 2026-05-09T19:55:00Z — Full design gap audit for typed literal / business domain types
- Completed comprehensive audit against `docs/language/business-domain-types.md` (D1–D18), research doc, language spec, catalog system doc, and runtime API doc.
- **Critical gap (P0):** Qualifier values (`in 'USD'`, `of 'mass'`) never flow from parser → type checker → semantic model. `TypeChecker.cs:128,240` hardcodes `DeclaredQualifiers: ImmutableArray<DeclaredQualifierMeta>.Empty`. The proof engine reads qualifiers correctly but always sees empty. This blocks all qualifier-dependent features: `in`/`of` enforcement, dimension checks, operator preconditions, narrowing, implicit `maxplaces`.
- **Fully implemented:** CurrencyCatalog (159 currencies + symbols), UcumAtomCatalog (≥300 atoms), UcumParser (full grammar), DimensionCatalog (10 categories), all 7 type definitions + qualifier shapes, 198 operation kinds, 12 diagnostic codes, content validation dispatch, TextMate grammar, MCP type vocabulary.
- **Not implemented:** Runtime evaluator (entire layer stubbed — Phase 3), runtime value types (Money/Quantity/Price/ExchangeRate records don't exist), language server (31 stubs), all IntelliSense completions, discrete equality narrowing, unit conversion, compound cancellation, sample files, drift tests.
- **UCUM Tier 1:** BrowseTier1() returns 10 units; design says ~150. No canonical enumeration exists in any doc.
- **DimensionCatalog:** Has 10 categories (design says 7 + post-v1). `speed`, `force`, `count` added beyond v1 spec — needs ratification.
- Full report at `.squad/decisions/inbox/frank-design-gap-audit.md`.


### 2026-05-09T16:55:27Z — UCUM evaluator gap analysis durably merged
- Scribe merged Frank's eight-area UCUM evaluator review into `.squad/decisions.md` and deleted the inbox copy.
- The coordinator-amended plan plus the user directive to stay within the 12-slice plan are now durable squad state.

### 2026-05-09T15:33:49Z - Typed-literal runtime boundary and format-validation scope recorded
- Scribe merged Frank's runtime-arg parsing decision: typed-literal args are parsed through `TypeRuntime<T>` / `TypeRuntimeMeta`, while `TypedConstantValidation.Validate(...)` remains compile-time-only.
- Scribe also recorded Frank's format-validation extensibility ruling: user-defined email/phone/document validation is out of scope for typed literals and should return later as a constraint-level `matches` feature.

### 2026-05-09T15:26:09Z — Typed-literal and UCUM architecture durably recorded
- Scribe merged Frank's typed-literal framework and UCUM parser architecture into `.squad/decisions.md` as the canonical implementation direction.
- Durable boundary: `ContentValidation` stays the metadata hook, `TypedConstantValidation.Validate(...)` stays the static dispatcher, and both temporal + UCUM parsing now have one approved shared-language architecture.

## Historical Summary

- Earlier 2026-05-09 work established the typed-literal direction: runtime JSON ingress reuses `TypeRuntimeMeta.ReadJson`, the unified content-validation framework replaced ad-hoc checker validation, user-defined format validation was explicitly kept out of typed literals, and the comprehensive 12-slice plan became the execution hub.
- The same day also clarified the external-data boundary: ISO 4217 and UCUM are consumed reference datasets rather than language catalogs, `CurrencyCatalog` owns Precept-specific symbol augmentation, and the UCUM parser/data-layer direction moved from placeholder closed sets to a real parser + loader architecture.
- Prior May work locked the catalog-first parser/checker/proof trajectory, required durable rationale capture in research + decisions, and reinforced that canonical docs must track live implementation direction.
- Use `.squad/decisions.md` for the exact per-batch chronology and `docs/` / `research/` for the surviving canonical rationale.
