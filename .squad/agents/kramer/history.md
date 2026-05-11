## Core Context

- Owns tooling surfaces: language server, VS Code extension, grammar sync, plugin wiring, MCP ergonomics, and executable developer workflows.
- Keeps grammar, completions, semantic tokens, tests, and tooling docs synchronized with the actual DSL and server surface.
- Favors catalog-driven and semantic-model-driven editor behavior over LS-local keyword lists or parser-span guesses.

## Learnings

- Semantic-token delta stability depends on exact identifier spans; when typed-constant token layouts shift, invalidate the cached `SemanticTokensDocument` for that URI instead of swallowing exceptions or disabling deltas globally.
- Versioned document updates must reject stale recompiles while preserving the unversioned fallback path for clients that omit version data.
- Declaration and slot completions should derive from catalog metadata plus nearby semantic context rather than parser recovery spans alone.
- Typed-constant completion needs local expected-type inference plus qualifier metadata; `TypedConstantContext` is the durable carrier for `(TypeKind, DeclaredQualifiers)`.
- Space and Ctrl+Space inside typed constants must intercept before the outer context switch; invoked completion should treat both null and empty trigger characters as the same recovery path.
- Quantity slot completions should use `UcumCatalog.BrowseTier1()` for the open catalog, pin exact `DeclaredQualifierMeta.Unit` values directly, and filter `DeclaredQualifierMeta.Dimension` through `DimensionVector` equality.
- Quote-close starter insertion belongs in a postprocess step on returned `CompletionItem`s; temporal segment items remain the exception because the caret must stay inside the quote for `+` continuation.
- Singular vs plural temporal unit preference comes from the last numeric segment, including compound literals after the most recent `+`.
- Semantic-token, definition, highlight, references, and rename flows all depend on the same identifier-precise span story; container spans are acceptable only when the semantic site contract explicitly requires them.
- B4/B5 are not expression-site bugs: qualifier declaration literals must resolve the active qualifier slot from parsed qualifier metadata before any enclosing-field expression fallback runs.
- B9-B12 Slice 1 touched 6 `TypedArgRef`/`TypedFieldRef` construction sites end-to-end: 5 explicit constructors in `src/` plus the target-typed `MakeFieldRef(...)` helper in `test/Precept.Tests/ProofEngineTests.cs`.
- Surprise from the Slice 1 audit: no positional `TypedArgRef(...)`/`TypedFieldRef(...)` pattern matches needed updates; the only non-obvious break was the target-typed test helper constructor.
- For qualifier propagation coverage, `test/Precept.Tests/TypeChecker/TypeCheckerSymbolTests.cs` can validate full-pipeline expression nodes by asserting against `TypedInputAction.InputExpression` from `index.EventHandlers` and `TypedField.ComputedExpression` from `index.FieldsByName`.
- B9-B12 Slice 2 modified `src/Precept/Pipeline/TypeChecker.Expressions.cs` at current lines 812-824, inserting the post-resolution `IsAssignable` gate directly in `ResolveAction`'s `AssignAction` arm before `TypedInputAction` construction.
- `test/Precept.Tests/TypeChecker/MoneyQuantityModifierRegressionTests.cs` did not need updates for Slice 2; its gap anchors cover modifier/default validation, while the new check only affects assignment actions.
- Surprise from Slice 2: the assignability helper to reuse is the local `TypeChecker.Expressions.cs` implementation (`private static bool IsAssignable` at current line 1172), not a shared helper in `TypeChecker.cs`.
- Slice 3 qualifier comparison uses the `DeclaredQualifierMeta` DU from `src/Precept/Language/DeclaredQualifierMeta.cs`: `Dimension.DimensionName`, `Unit.UnitCode`, `Unit.DimensionName`, and `Currency.CurrencyCode` are the relevant properties for assignment checks.
- `Diagnostics.Create(DiagnosticCode.DimensionCategoryMismatch, span, sourceDimension, targetDimension, fieldName)` matches PRE0069's `{0}=source dimension`, `{1}=declared category`, `{2}=field name`; `Diagnostics.Create(DiagnosticCode.QualifierMismatch, span, targetQualifier, fieldName)` matches PRE0068's `{0}=declared qualifier`, `{1}=field name`.
- `MoneyQuantityModifierRegressionTests` still needed no updates for Slice 3 because the new helper only runs in `ResolveAction` assignment paths, not modifier/default validation paths.
- Slice 4 extracted `DeriveUnitDimensionName` plus the count/non-count dimensionless UCUM sets into `src/Precept/Language/UnitDimensionHelper.cs` as `internal` shared runtime helpers so both `TypeChecker` and `QuantityValidator` can derive qualifier dimensions from the same metadata.
- Slice 4 threaded `DeclaredQualifiers` through 4 resolve-chain touchpoints (`TypeChecker.ResolveExpression`, private `Resolve`, `ResolveLiteral`, and `ResolveTypedConstant`) plus the `ResolveAction` assign call site and 3 `ResolveFieldExpressions` modifier/default call sites.
- `QuantityValidator.Validate(...)` uses `TypedConstantParseResult.Failed(formatDescription, params TypedConstantDiagnostic[] diagnostics)`; the only surprise was that validator-specific `DimensionCategoryMismatch` details ride inside the typed-constant diagnostic payload, while the surfaced pipeline diagnostic remains `InvalidTypedConstantContent`.

## Historical Summary

- Early May tooling work established the catalog-driven language-server baseline: semantic-context completions, semantic-token publication, shared symbol-navigation helpers, document version ordering, and VS Code activation coverage.
- The canonical decision ledger in `.squad/decisions.md` carries full batch chronology; this history keeps only the durable tooling rules and the newest live context needed for future implementation runs.

## Recent Updates

### 2026-05-14 — kramer-10 UCUM display labels and tier-1 pruning
- Added `UcumAtom.PrintSymbol`, parsed UCUM XML `printSymbol` metadata, and surfaced print-symbol labels with name details in quantity completions.
- Pruned troy/apothecary mass units from tier-1 while keeping `[gr]` because the embedded UCUM XML marks it `class="avoirdupois"`.
- Quantity hover now shows resolved unit metadata (`code`, `printSymbol`, `name`); validation closed green at 4567/4567 core tests and 221/221 language-server tests.

### 2026-05-11T05:34:40Z — Frank retriage reopened B4/B5
- The earlier apostrophe-trigger normalization correctly fixed B1/default-site recovery, but it does **not** fix declaration-side qualifier literals.
- At `field q as quantity in '` and `field q as quantity of '`, coercing the site to `InExpression` makes `TryGetTypedConstantContext(...)` fall back to the enclosing field and recover `Quantity`, so completion incorrectly routes to quantity-literal examples instead of the active qualifier slot.
- Next safe implementation path: detect declaration-side qualifier literal sites before `TryGetEnclosingField(...)`, resolve the active qualifier axis from parsed qualifier metadata/shape, return `UnitOfMeasure` / `Dimension` slot context (and the analogous currency / temporal qualifier slots), and strengthen tests to assert concrete unit or dimension labels rather than non-empty lists.

### 2026-05-11 — B2 quantity slot now returns the full UCUM tier-1 catalog
- `GetQuantitySlotItems` replaced the 3-example fallback with `UcumCatalog.BrowseTier1()` and dimension-vector filtering, while preserving exact pinned-unit behavior for `DeclaredQualifierMeta.Unit`.
- Regression coverage now asserts representative UCUM codes, dimension exclusions, and pinned-unit exactness; language-server validation closed green at 220/220 tests.

### 2026-05-11 — B7 semantic-tokens delta crash fixed
- Typed-constant span changes now invalidate the cached semantic-token document per URI so OmniSharp falls back to a full refresh only when the typed-constant token layout actually changed.
- Durable rule: targeted cache invalidation beats global full-refresh mode and beats exception swallowing.

### 2026-05-11T06:00:00Z — B14 semantic-token delta baseline guard
- Root cause was not UCUM display metadata: OmniSharp's `SemanticTokensDocument` keeps one framework `Id` per document, so stale client `PreviousResultId` values were indistinguishable once `_prevData` had been primed by an earlier delta.
- `SemanticTokensHandler` now stamps its own per-response client result IDs, tracks the latest `(resultId, document.Id)` per URI, and returns a full semantic-tokens payload whenever the client baseline is stale or typed-constant invalidation swapped the backing document.
- Regression coverage now proves both stale-result fallback and typed-constant-span fallback; validation closed green at 223/223 language-server tests. Commit: `ef7374dd`.

### 2026-05-11T02:32:33Z — Modifier-token squiggle precision closed the latest span pass
- `ParsedModifier` now owns its own span so modifier diagnostics land on the offending keyword instead of the whole field declaration.
- The broader span program stays intact: identifier-precise semantic ranges upstream, exact-range dedup and invalid-coordinate filtering as downstream safety rails.
