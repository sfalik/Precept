# Language Server

## Status

| Property | Value |
|---|---|
| Doc maturity | Stub |
| Implementation state | Bootstrap only (server boots and waits for exit; no LSP features implemented) |
| Source | `tools/Precept.LanguageServer/` |
| Upstream | Compiler (Compilation artifact), Runtime (Precept artifact for preview) |
| Downstream | VS Code extension (via LSP protocol) |

---

## Overview

The language server implements the LSP protocol for Precept `.precept` files. It provides diagnostics, completions, hover, go-to-definition, semantic tokens, and preview (inspect) capabilities to editors. It consumes pipeline artifacts by responsibility — each feature reads from exactly the artifact that owns the information it needs.

---

## Responsibilities and Boundaries

**OWNS:** LSP message handling, per-feature artifact routing, `Compilation` lifecycle (recompile on change via `Interlocked.Exchange`), preview/inspect dispatch.

**Does NOT OWN:** Compilation logic (`src/Precept/`), grammar (generated from catalogs), MCP tooling.

---

## Right-Sizing

The LS is thin — a routing and protocol layer over the compiler and runtime. All intelligence lives in the compiler artifacts and catalogs. The LS does not add semantic knowledge; it projects what the compiler already knows to the editor. An LS feature that reaches into catalog data or re-implements any compiler logic is a design violation.

---

## Inputs and Outputs

**Input:** LSP requests from editor (document open/change, completion request, hover request, etc.)

**Output:** LSP responses (diagnostics, completion items, hover markup, semantic tokens, go-to-definition locations)

**Internal:** Holds one `Compilation` reference per open document, atomically swapped on each change.

---

## In-Process Compilation Model

In-process compilation model: the LS calls `Compiler.Compile(source)` directly in the same process. Full-pipeline recompile on every document change. The resulting `Compilation` is stored atomically and read by concurrent LSP request handlers without locking (deep immutability of `Compilation` enables `Interlocked.Exchange`).

When `!HasErrors`, the LS also holds a `Precept` (built from `Compilation`) for preview/inspect operations.

---

## LSP Feature Routing and Artifact Consumption

### Consumer Artifact Map

| LS Feature | Correct artifact |
|---|---|
| Diagnostics | `Compilation.Diagnostics` |
| Lexical semantic tokens (Pass 1) | `TokenStream` + `TokenMeta` |
| Identifier semantic tokens (Pass 2) | `SemanticIndex` symbol/reference bindings |
| Completions | Catalogs + `SyntaxTree` context + `SemanticIndex` |
| Hover | `SemanticIndex` + catalog documentation |
| Go-to-definition | `SemanticIndex` reference binding + declaration back-pointer |
| Preview/inspect | `Precept` + inspection runtime (only when `!HasErrors`) |
| Outline/folding | `SyntaxTree` |

### Hard Rules

1. Semantic LS features must not walk `SyntaxTree` to answer semantic questions — `SemanticIndex` + back-pointers only.
2. Preview/runtime features must not consume `Compilation` after `Precept` is available.

### Atomic Swap Concurrency

Each document change triggers `Compiler.Compile(newSource)`. The resulting `Compilation` is stored via `Interlocked.Exchange`. Concurrent LSP request handlers read whichever `Compilation` was current when they started — no locking required because `Compilation` is deeply immutable.

---

## Dependencies and Integration Points

- **Compiler** (`src/Precept/`): `Compiler.Compile(source)` — called in-process on every document change
- **Runtime** (`src/Precept/`): `Precept.From(Compilation)` — called when `!HasErrors` for preview
- **Catalogs** (upstream at request time): for completions and hover text
- **VS Code extension** (downstream): hosts the LS process; receives LSP responses

---

## Failure Modes and Recovery

If `Compiler.Compile` throws (engine bug, not authoring error), the LS catches the exception and reports it as a single top-level diagnostic without crashing the server. The previous `Compilation` is retained until the next successful compile.

---

## Contracts and Guarantees

- The LS never holds a stale `Compilation` reference longer than one document change cycle.
- Every LSP diagnostic corresponds 1:1 to a `Diagnostic` in `Compilation.Diagnostics`.
- Preview features are never invoked when `Compilation.HasErrors` is true.

---

## Design Rationale and Decisions

**In-process compilation:** At Precept's scale (64KB ceiling, flat grammar, sub-millisecond lex), in-process compilation is faster, simpler, and more correct than IPC-based compiler integration. No serialization boundary, no version skew between LS and compiler.

**Full-pipeline recompile on change:** Correct at DSL scale; no incremental infrastructure needed. Eliminates an entire class of invalidation bugs.

---

## Innovation

- **Atomic swap concurrency model:** Deep immutability of `Compilation` enables `Interlocked.Exchange` — no locks needed for concurrent LSP requests.
- **Single-process integration:** The LS calls `Compiler.Compile` directly — same process, no IPC, no serialization boundary. This is the dominant pattern at DSL scale.
- **Full-pipeline recompile on every change:** Correct at DSL scale; no incremental infrastructure needed.

---

## Open Questions / Implementation Notes

1. Only server boot is implemented — all LSP feature handlers are not yet written.
2. Implement diagnostics push first (simplest feature, most visible value to users).
3. Implement lexical semantic tokens second (uses `TokenStream`, no `SemanticIndex` dependency).
4. Completions and hover depend on `SemanticIndex` — blocked on TypeChecker implementation.
5. Preview/inspect depends on `Precept` runtime — blocked on Precept Builder and Evaluator implementation.
6. Confirm TextMate grammar two-pass semantic token design: first pass `TokenStream`+`TokenMeta`; second pass `SemanticIndex` bindings.

---

## Deliberate Exclusions

- **No semantic logic in LS code:** All language knowledge lives in compiler artifacts and catalogs.
- **No separate compiler process:** In-process compilation is the right model at this scale.

---

## Cross-References

| Topic | Document |
|---|---|
| LS consumer artifact map and hard rules | `docs/compiler-and-runtime-design.md §15` |
| Immutability + atomic swap model | `docs/compiler-and-runtime-design.md §12` |
| Compiler artifacts the LS consumes | `docs/compiler/` |
| VS Code extension that hosts the LS | `docs/tooling/extension.md` |

---

## Source Files

| File | Purpose |
|---|---|
| `tools/Precept.LanguageServer/` | All language server source files |
