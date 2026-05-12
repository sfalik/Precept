---
name: "proof-hover-routing"
description: "Route proof diagnostics and proof-bearing expressions ahead of generic hover help so VS Code shows obligation evidence instead of operator trivia."
domain: "language-server"
confidence: "high"
source: "earned — generalized from implementing hover-design v4 proof routing in the language server"
---

## When to Apply

Use this when a language-server hover surface has both:

1. generic catalog hover candidates (operator, type, accessor, function, symbol), and
2. proof-aware candidates backed by diagnostics or semantic obligations.

## Pattern

1. In the top-level hover handler, ask a proof-specific router first.
2. First return a proof-diagnostic card when the cursor is inside a proof diagnostic span.
3. Otherwise return the smallest proof-bearing expression card at the cursor.
4. Only after proof-specific checks fail should generic operator/type/accessor/function hover run.
5. Keep proof-card content driven by compile-time artifacts already on `Compilation` (`Proof.Obligations`, `FaultSiteLinks`, diagnostics, semantic spans).

## Why

- Prevents generic operator help from hiding the actual teachable moment.
- Keeps Elaine-style proof hover honest: verdict first, evidence second, fix third.
- Preserves thin tooling behavior by composing existing semantic/proof data instead of inventing a parallel model.

## Key Files

- `tools/Precept.LanguageServer/Handlers/HoverHandler.cs`
- `tools/Precept.LanguageServer/Handlers/RichHoverFactory.cs`
- `src/Precept/Pipeline/ProofLedger.cs`

## Verification Pattern

Add tests that prove both routing outcomes:

- the proof diagnostic hover beats generic operator hover on an unresolved proof span
- a proved binary expression hover beats generic operator hover on the operator token
- qualified field hover still shows declaration truth plus live proof-use summary
