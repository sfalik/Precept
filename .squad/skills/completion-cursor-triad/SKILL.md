---
name: "completion-cursor-triad"
description: "Diagnose completion-routing bugs by separating trigger state, slot context, and raw token/span evidence"
domain: "language-server"
confidence: "high"
source: "earned — fixing empty typed-constant Ctrl+Space regressions in the Precept language server"
---

## Context

Apply this when completion results differ between automatic trigger and manual invoke, or when parser context and token reality disagree around incomplete literals.

## Patterns

- Trace three axes independently before changing code:
  1. request trigger shape (`TriggerKind`, `TriggerCharacter`, null vs empty)
  2. slot context (`SlotContextResolver.GetCursorContext`)
  3. raw token/span under the caret (`FindTokenAtOrBeforeCursor` + span containment)
- Prove the lexer output for empty/incomplete literals first; do not assume the parse tree owns the cursor just because completion fell back to outer grammar.
- Reproduce both no-context tests and invoked-completion client variants with empty trigger characters.
- If tokenization/span math is correct but the wrong completion surface appears, normalize invoked-completion handling before touching lexer math.
- For local syntax inference reopened inside an existing literal, skip the active literal token before walking left for operators, commas, or call boundaries.

## Examples

- `field ApprovedOn as date default '¦'` → raw token is `TypedConstant`, slot context may still be `TopLevel`, so routing must trust the token and normalize invoked completion.
- `rule ApprovedAmount > '¦'` → raw token is `TypedConstant`, slot context is `InExpression`, and peer-operand inference must step left past the literal to reach `>` and recover `money`.

## Anti-Patterns

- Treating `TriggerCharacter == null` as the only manual-invoke shape
- Assuming a bad completion list proves lexer/token failure without checking the raw token span
- Walking left from the caret and stopping on the active typed-constant token when the goal is to recover surrounding expression syntax
