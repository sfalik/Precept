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

### 2026-05-12T22:25:28Z — Frank review approved B4’s shape but blocked the next placement step

- Frank’s review of B4 approved `EdgeProofStatus` as the right `StateGraph` projection and approved the shipped regression coverage, so the data shape itself is now durable context.
- The next fix is architectural, not hover-surface: move proof-status enrichment/domain logic out of `Compiler.cs` orchestration and avoid duplicating edge-expansion knowledge already owned by `GraphAnalyzer`.
- Elaine’s doc sync also landed, so `docs/Working/hover-design.md` now records the shipped B4 badge vocabulary and the fact that the proof narrative appends to rich state hover instead of introducing a standalone hover kind.

### 2026-05-12T07:12:56Z — Hover markdown line-break fix landed
- Changed all 11 `Create*Markdown` builders in `RichHoverFactory.cs` from `string.Join("\n", lines)` to `string.Join("\n\n", lines)` so VS Code renders paragraph breaks instead of a single run-on line.
- The change shipped in commit `af6e563c`; no hover assertions needed updates because tests already match by substring.
- Validation snapshot remained fully green: **5471/5471 tests passing**.

### 2026-05-12T07:12:56Z — Hover/color audit narrowed the remaining follow-up work
- Real hover gaps are now explicit: qualifier hover still hides the resolved-source meaning line, and field/state/event still use the generic symbol path instead of explicit construct entrypoints.
- No active Elaine-listed color gap remains in-tree; only Gap 1 field-vs-arg split evidence is partially unverified, and grammar-level regression depth could still improve.
- Recommended follow-up stays focused on extra hover regression coverage, qualifier resolved-source rendering, optional construct-parity refactors, and explicit field/arg plus grammar-scope tests.

### 2026-05-12T15:15:10Z — George shipped G1 compound-unit qualifier repair
- George fixed `ResolveQualifierFromInterpolatedConstant` in `ProofEngine.cs` so interpolated compound-unit constants resolve the full `{A}/{B}` qualifier string before the denominator fallback path.
- Commit `cb4fbf57` plus docs/history follow-up `1ee54bdb` cleared the RC1 PRE0114 and cascading DivisionByZero fallout in `samples/inventory-item.precept`, leaving only BUG-C / later proof work outside Kramers hover scope.

### 2026-05-12T17:45:51-04:00 — B1 compact proof-gap cards landed
- Reworked the proof diagnostic/expression builders in `tools/Precept.LanguageServer/Handlers/RichHoverFactory.cs` to emit the badge-first compact cards from `docs/Working/hover-design.md` instead of verbose `Status:` / `Reason:` blocks.
- Added compact evidence formatting so qualifier gaps now read inline (`Left ... has no known ... · right ... carries ...`), presence cards render the optional/access reason on one line, and proved expression cards collapse to clean 3-line summaries.
- Updated `test/Precept.LanguageServer.Tests/HoverHandlerTests.cs` for the new PRE0114 / PRE0116 / proved-expression card text, including direct formatter coverage for the presence card path; validation passed with `dotnet test test\\Precept.LanguageServer.Tests\\Precept.LanguageServer.Tests.csproj` and `dotnet test test\\Precept.Tests\\Precept.Tests.csproj`.

### 2026-05-12T18:01:17.648-04:00 — Hover B1 landed; B2/B3 follow-up active

- B1 compact proof-gap cards are shipped in `RichHoverFactory.cs`; hover proof diagnostics and proof-expression cards now use the badge-first compact format from `docs/Working/hover-design.md`, with updated `HoverHandlerTests` and green LS/core suites (272/272, 4938/4938).
- Frank’s follow-up ruling is now the active V1 contract for the next pass: fix construct routing before generic token help, keep guarded access out of mutability counts/state lists, and defer the state-card missing-path narrative.
- Kramer-2 is currently applying the B2/B3 routing + mutability honesty changes in `HoverHandler.cs` and `RichHoverFactory.cs`.

### 2026-05-12T19:26:05.9065969-04:00 — B2 construct routing + B3 mutability honesty landed

- `HoverHandler.cs` now routes state symbols to the rich state card before generic construct rows can steal them, and it evaluates rich construct hovers before generic operator/function/accessor fallbacks so rule/ensure/transition/reject/access/omit cards win where the spec requires.
- `RichHoverFactory.cs` now exposes shared rich symbol-card builders for field/state/event/arg paths and filters guarded access declarations out of V1 writable summaries, producing honest field mutability lines with `✏️` unconditional states and `🔒` locked-or-omitted states.
- `HoverHandlerTests.cs` now covers state-reference routing, reject-over-transition precedence, qualifier-over-symbol routing, guarded-access omission from mutability summaries, and the updated routing expectations; validation passed with `dotnet test test\Precept.LanguageServer.Tests\ --nologo` (271/271) and `dotnet build tools\Precept.LanguageServer\Precept.LanguageServer.csproj --artifacts-path temp/dev-language-server --nologo`.
