# Typed Declaration Spans

## When to Apply

When a downstream pipeline stage needs source locations for diagnostics or tooling, but PRECEPT0024 or the stage-boundary rules forbid reading `.Syntax` back-pointers from `Typed*` records.

## Pattern

1. Identify the exact source location the downstream stage needs (usually a declaration-name span).
2. Hoist that `SourceSpan` onto the typed semantic record produced by the owning stage.
3. Populate the span while still inside the owning stage (typically TypeChecker from SymbolTable data).
4. Consume the hoisted span downstream; never bypass the stage boundary by reading `.Syntax`.

## Example

- Add `TypedState.NameSpan` and `TypedEvent.NameSpan` in `SemanticIndex.cs`.
- Populate them from `DeclaredState.NameSpan` / `DeclaredEvent.NameSpan` inside `TypeChecker`.
- Let `GraphAnalyzer` emit `UnreachableState`, `DeadEndState`, and `UnhandledEvent` from those spans.

## Why

This preserves anti-mirroring discipline, keeps diagnostics precisely located, and avoids dragging parse-tree concerns back into later semantic stages.
