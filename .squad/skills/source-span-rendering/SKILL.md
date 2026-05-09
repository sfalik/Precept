---
name: "source-span-rendering"
description: "Render authored DSL fragments for AI/tool DTOs by slicing the original source with SourceSpan offsets instead of rebuilding text from semantic nodes."
domain: "mcp-contracts"
confidence: "high"
source: "earned — generalized from implementing precept_compile over Compilation/SemanticIndex"
---

## When to Apply

Use this when an MCP tool, language-server payload, or diagnostic DTO needs the user's original expression/action text, but the semantic model intentionally avoids storing a second string copy.

## Pattern

1. Prefer the authoritative semantic node for structure and validation.
2. When the outbound contract needs authored text, slice the original source with `SourceSpan.Offset` and `SourceSpan.Length`.
3. Trim the slice for DTO output, but keep the semantic node as the source of truth for meaning.
4. Reconstruct only the tiny pieces that are not actually stored in the semantic node text (for example qualifier display from explicit `DeclaredQualifiers`).
5. Guard against `SourceSpan.Missing` / zero-length spans before slicing.

## Why

- Preserves thin-wrapper discipline in MCP layers.
- Avoids lossy pretty-printing or hand-maintained renderers.
- Keeps AI-facing contracts aligned with exactly what the author wrote.
- Respects stage boundaries: the semantic pipeline owns meaning; the source span owns presentation.

## Example

`precept_compile` maps:
- rule expressions from `TypedRule.Condition.Span`
- because messages from `TypedRule.Message.Span`
- transition guards from `TypedTransitionRow.Guard.Span`
- action text from each `TypedAction.Span`

All of them render by slicing the original `text` argument rather than rebuilding syntax in the MCP layer.
