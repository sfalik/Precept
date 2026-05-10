---
name: "incomplete-declaration-completions"
description: "Detect incomplete declaration-head completion contexts from neighboring significant tokens"
domain: "language-server"
confidence: "high"
source: "earned — fixing `field Name ` completion fallback in the Precept language server"
---

## Context

Apply this when completion is requested inside a partially written declaration and the parser has not consumed the next grammar token yet, such as `field Amount ` or `event Submit(Amount )`.

## Patterns

- Do not trust incomplete `ParsedConstruct.Span` as the only routing signal; parser recovery can collapse the construct span to its leading keyword.
- Do not trust declaration-slot end spans as exact name boundaries; trailing trivia can extend them past the identifier.
- Detect declaration-head positions from neighboring significant tokens instead:
  - field names: previous significant token is `field` or `,`, and the next significant token is `as`, EOF, or a construct-leading token from `Constructs.LeadingTokens`
  - event arg names: previous significant token is `(` or `,`, and the next significant token is `as`, `,`, `)`, or EOF
- Return a dedicated slot context so completion can offer the grammar token (`as`) instead of broad top-level constructs.

## Examples

- `field Amount ` → declaration-head context → suggest `as`
- `event Submit(Amount )` → declaration-head context → suggest `as`

## Anti-Patterns

- Routing incomplete declarations solely from `ParsedConstruct.Span`
- Using slot spans with trailing trivia as proof of the exact identifier boundary
- Falling back to top-level construct completions when the local token neighborhood already proves the next grammar step
