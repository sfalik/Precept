---
name: "textmate-semantic-color-drift"
description: "Diagnose miscoloring caused by TextMate fallback colors drifting from semantic token lanes"
domain: "tooling"
confidence: "high"
source: "earned"
---

## Context
Use this when a Precept token looks visually wrong in VS Code and it is unclear whether the bug lives in catalog metadata, semantic tokens, the generated grammar, or extension theming.

## Patterns
- Check the catalog/semantic-token lane first: confirm `TokenMeta.VisualCategory` and the language-server projection still classify the token as intended.
- Then inspect `tools/Precept.VsCode/package.json` under `contributes.configurationDefaults.editor.tokenColorCustomizations`; a stale TextMate fallback can make correctly classified tokens look wrong before semantic tokens arrive.
- Keep gold reserved for `string.quoted.double.message.precept`; ordinary grammar keywords belong on the shared grammar lane (`keyword.other.grammar.precept` → `#6366F1`).
- Add paired regression coverage: one semantic-token test that locks the token lane, plus one manifest test that locks the fallback scope color.

## Examples
- `as` and `default` were still emitted as `KeywordGrammar` by `SemanticTokensHandler`, but the extension manifest mapped `keyword.other.grammar.precept` to `#FBBF24`, so they rendered gold anyway.

## Anti-Patterns
- Changing token catalog metadata to fix a theme-only bug.
- Hand-editing the generated TextMate grammar when the real drift is in `package.json`.
- Locking only semantic-token behavior and leaving fallback TextMate colors untested.
