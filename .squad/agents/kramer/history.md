## Core Context

- Owns tooling surfaces: language server, VS Code extension, grammar sync, plugin wiring, MCP ergonomics, and executable developer workflows.
- Keeps grammar, completions, semantic tokens, tests, and tooling docs synchronized with the actual DSL and server surface.
- Favors catalog-driven and semantic-model-driven editor behavior over LS-local keyword lists or parser-span guesses.

## Learnings

- `TypedConstantContext` remains the durable carrier for expected typed-constant slot context; declaration-site qualifier recovery must consult parsed qualifier metadata before enclosing-expression fallback.
- Semantic-token delta stability depends on exact identifier spans and cache invalidation of both `_documents` and `_latestResults` when token layouts change.
- Hover, definition, highlight, references, and rename all depend on the same precise semantic span contracts; container spans are only acceptable when the consumer explicitly wants them.
- Keyword semantic tokens should stay out of grammar-owned context-sensitive positions; the extension manifest and grammar must stay aligned on the fallback scopes they actually emit.
- Qualifier hover V3 should derive its status detail from resolved qualifier metadata: simple interpolated templates like `{StockingUnit.dimension}` should collapse to the owning symbol for `qualifier resolves from ...`, and reject rows must keep precedence over generic transition hover because both come from the same `TransitionRows` projection.
- Proof hover routing works best as a two-pass check in `HoverHandler.cs`: first ask `RichHoverFactory.TryCreateProofHover(...)` for proof diagnostics and proof-bearing expressions, then fall back to generic operator/type/accessor hover so PRE0114/PRE0116 explanation wins over catalog help.
- `tools/Precept.LanguageServer/Handlers/RichHoverFactory.cs` is now the compile-time proof-hover composition point: field proof summaries come from `Compilation.Proof.Obligations`, diagnostic cards join proof diagnostics back to `ProofObligation` via `FaultSiteLinks`, and expression cards resolve qualifier evidence from typed expressions rather than raw token text.
- For hover evidence, reuse typed spans plus qualifier metadata together: `FormatSnippet(...)` gives the authored expression text, while `ResolveDeclarationQualifier(...)` / `ResolveQualifierFromExpression(...)` recover resolved qualifier values, sources, and proof-chain fields without reaching for parser back-pointers.

## Historical Summary

- Early May through 2026-05-11 established the current tooling baseline: typed-constant completion and semantic-token fixes, delta-baseline guards, UCUM tier-1 completion curation, modifier-span precision, and catalog-driven hover/completion behavior.
- The hover/color audit cycle already confirmed no open Elaine-listed color implementation gaps; remaining risk is test depth and hover-surface parity rather than missing shipping behavior.

## Recent Updates

### 2026-05-12T07:12:56Z — Hover markdown line-break fix landed
- Changed all 11 `Create*Markdown` builders in `RichHoverFactory.cs` from `string.Join("\n", lines)` to `string.Join("\n\n", lines)` so VS Code renders paragraph breaks instead of a single run-on line.
- The change shipped in commit `af6e563c`; no hover assertions needed updates because tests already match by substring.
- Validation snapshot remained fully green: **5471/5471 tests passing**.

### 2026-05-12T07:12:56Z — Hover/color audit narrowed the remaining follow-up work
- Real hover gaps are now explicit: qualifier hover still hides the resolved-source meaning line, and field/state/event still use the generic symbol path instead of explicit construct entrypoints.
- No active Elaine-listed color gap remains in-tree; only Gap 1 field-vs-arg split evidence is partially unverified, and grammar-level regression depth could still improve.
- Recommended follow-up stays focused on extra hover regression coverage, qualifier resolved-source rendering, optional construct-parity refactors, and explicit field/arg plus grammar-scope tests.
