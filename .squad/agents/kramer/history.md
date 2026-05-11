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

## Historical Summary

- Early May tooling work established the catalog-driven language-server baseline: semantic-context completions, semantic-token publication, shared symbol-navigation helpers, document version ordering, and VS Code activation coverage.
- The canonical decision ledger in `.squad/decisions.md` carries full batch chronology; this history keeps only the durable tooling rules and the newest live context needed for future implementation runs.

## Recent Updates

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

### 2026-05-11T02:32:33Z — Modifier-token squiggle precision closed the latest span pass
- `ParsedModifier` now owns its own span so modifier diagnostics land on the offending keyword instead of the whole field declaration.
- The broader span program stays intact: identifier-precise semantic ranges upstream, exact-range dedup and invalid-coordinate filtering as downstream safety rails.
