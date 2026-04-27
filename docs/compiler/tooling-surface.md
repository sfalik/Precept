# Tooling Surface

## Status

| Property | Value |
|---|---|
| Doc maturity | Stub |
| Implementation state | Grammar generator implemented; semantic token two-pass designed but not implemented |
| Source | `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json` (generated), grammar generator (path TBD) |
| Upstream | Catalog metadata (Tokens, Types, Constructs, Operators) |
| Downstream | VS Code syntax highlighting, LS semantic tokens, LS completions |

---

## Overview

The tooling surface is the layer that projects catalog metadata into editor-facing artifacts — TextMate grammar, semantic token types, completion candidates, hover text. All these artifacts are GENERATED from catalogs, not hand-edited. The tooling surface is not a pipeline stage — it runs at build time (grammar generation) and at LS request time (completions, hover, semantic tokens).

---

## Responsibilities and Boundaries

**OWNS:** TextMate grammar generation from catalog metadata, semantic token type assignment, completion candidate derivation from catalogs + `SyntaxTree` context, hover text assembly from catalog documentation.

**Does NOT OWN:** Semantic resolution (TypeChecker), diagnostic production (pipeline stages), preview/inspect (runtime).

---

## Right-Sizing

The tooling surface is intentionally thin — a projection layer, not a reasoning layer. All intelligence lives in the catalogs. The grammar generator is a build-time tool; the LS features are request-time catalog projections. No editor-specific logic should encode language knowledge. If a new token is added to the Tokens catalog, it automatically appears in syntax highlighting, completions, and the MCP vocabulary with no tooling code change.

---

## Inputs and Outputs

**Grammar generation (build time):**
- Input: `Tokens`, `Types`, `Constructs`, `Operators` catalog metadata
- Output: `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json`

**LS features (request time):**
- Input: Catalogs + `Compilation` artifacts (`TokenStream`, `SemanticIndex`)
- Output: Completion items, hover content, semantic token classifications

---

## Architecture

Two surfaces with different execution times:

1. **Grammar generation (build time):** A generator reads `Tokens.All`, `Types.All`, `Constructs.All`, and `Operators.All` to produce the TextMate grammar JSON. Each `TokenMeta.TextMateScope` becomes a grammar pattern. The grammar is a build output — the VS Code extension registers it at activation.

2. **Semantic tokens (request time — two passes):**
   - **Pass 1 (lexical):** Walk `TokenStream` + `TokenMeta` for all non-identifier tokens. Each token's `SemanticTokenType` drives LSP classification.
   - **Pass 2 (semantic):** Walk `SemanticIndex` symbol/reference bindings for identifier tokens. Resolved symbols receive their semantic class (field reference, state reference, event reference, etc.).

3. **Completions (request time):** Catalog items filtered by `TokenMeta.ValidAfter` predecessor set. Position context narrows candidates using `SyntaxTree` + `SemanticIndex`.

---

## Component Mechanics

### TextMate Grammar Generation

The grammar generator reads catalog entries and emits pattern rules in TextMate JSON format. Each catalog entry with a `TextMateScope` contributes a pattern. Keyword patterns are grouped by `TokenCategory`. The generator runs as part of the build pipeline and overwrites `precept.tmLanguage.json` — no manual editing.

### Semantic Token Two-Pass

Pass 1 uses `TokenStream` + `TokenMeta.SemanticTokenType` to classify structural tokens (keywords, operators, punctuation, literals). Pass 2 uses `SemanticIndex` reference bindings to classify identifiers as field/state/event/arg references. The two-pass design separates lexical classification (no semantic analysis needed) from semantic classification (requires resolved index).

### Completion Filtering via TokenMeta.ValidAfter

`TokenMeta.ValidAfter` carries the set of token kinds after which this token is a valid completion. The completion provider filters catalog candidates to those valid after the predecessor token at the cursor position. Additional narrowing uses syntactic context (inside field declaration, inside guard expression, etc.) from the `SyntaxTree`.

### Hover Text Assembly

Hover text is assembled from catalog documentation fields on the resolved `SemanticIndex` entry for the hovered symbol. For keywords, hover text comes from `TokenMeta` documentation. For identifiers, hover text comes from the resolved `TypedField`, `TypedState`, `TypedEvent`, or `TypedArg` entry.

---

## Dependencies and Integration Points

- **Catalogs** (upstream): Tokens, Types, Constructs, Operators — drive all generated and request-time artifacts
- **TokenStream** (upstream at request time): lexical token sequence for Pass 1 semantic tokens
- **SemanticIndex** (upstream at request time): resolved symbol/reference bindings for Pass 2 and completions
- **VS Code extension** (downstream): registers `precept.tmLanguage.json`; hosts the LS that produces semantic tokens and completions
- **LS** (downstream): sends semantic token and completion responses over LSP

---

## Failure Modes and Recovery

Grammar generation is a build-time tool — failures are build failures, not runtime failures. LS feature failures (e.g., SemanticIndex not available when TypeChecker is not yet implemented) degrade gracefully: Pass 1 still classifies all structural tokens; Pass 2 is skipped with no error.

---

## Contracts and Guarantees

- `precept.tmLanguage.json` is always a function of catalog metadata — no patterns can be present that are not in a catalog entry.
- Every token kind in `Tokens.All` that has a `TextMateScope` appears in the grammar.
- Semantic token classifications for identifiers are always backed by a resolved `SemanticIndex` entry — no speculative classifications.

---

## Design Rationale and Decisions

The single-source-of-truth principle: grammar, completions, hover, semantic tokens, and MCP vocabulary all derive from the same catalog definitions. This eliminates an entire class of drift bugs common in hand-maintained language tooling. The grammar cannot disagree with the parser because both derive from the same metadata.

---

## Innovation

- **Grammar generation from catalogs:** The TextMate grammar is a build output, not a hand-edited file. Drift between syntax highlighting and actual grammar is structurally impossible.
- **Single source for all editor surfaces:** Grammar, completions, hover, semantic tokens, and MCP vocabulary all derive from the same catalog definitions. No other DSL tooling in this category has this level of surface coherence.
- **Zero-drift guarantee:** Because the grammar derives from the same metadata the parser and type checker consume, syntax highlighting cannot disagree with actual parse behavior.

---

## Open Questions / Implementation Notes

1. Confirm grammar generator source file location — it is referenced in the build pipeline but path not confirmed in `tools/Precept.VsCode/` or `tools/scripts/`.
2. Semantic token two-pass design: first pass uses `TokenStream` + `TokenMeta` for lexical tokens; second pass reads `SemanticIndex` symbol/reference bindings for identifier semantic tokens. Not yet implemented.
3. Completion filtering via `TokenMeta.ValidAfter`: predecessor-set filtering is designed but not confirmed as fully implemented. Verify against current LS code.
4. Anti-pattern enforcement: confirm no patterns are hand-edited in `tmLanguage.json` — add a CI check or comment header to the generated file.
5. Define the build step that regenerates `tmLanguage.json` and how it fits into the `dotnet build` pipeline.

---

## Deliberate Exclusions

- **No language semantics in editor code:** All intelligence is in catalogs. Editor code is projection only.
- **No hand-editing of generated files:** `tmLanguage.json` is a build output.

---

## Cross-References

| Topic | Document |
|---|---|
| TextMate grammar generation design | `docs/compiler-and-runtime-design.md §13` |
| Tokens, Types, Constructs, Operators catalogs | `docs/language/catalog-system.md` |
| VS Code extension that hosts the grammar | `docs/tooling/extension.md` |
| Language server that uses semantic tokens and completions | `docs/tooling/language-server.md` |

---

## Source Files

| File | Purpose |
|---|---|
| `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json` | Generated grammar artifact — do not hand-edit |
| Grammar generator source | Path TBD — confirm in `tools/Precept.VsCode/` or `tools/scripts/` |
