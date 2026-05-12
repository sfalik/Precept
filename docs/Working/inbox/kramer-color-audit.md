# Kramer color audit

Date: 2026-05-12

## Applied fixes

1. **Rule-desugaring modifiers now use the rule/constraint lane.**
   - Changed `tools/Precept.GrammarGen/Program.cs` so `ruleDesugaringModifiers` emits `keyword.other.constraint.precept` instead of `keyword.other.grammar.precept`.
   - Regenerated `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json` from the generator.
   - Changed `tools/Precept.VsCode/package.json` so `keyword.other.constraint.precept` is `#FBBF24`.
2. **Type-keyword TextMate fallback now has an exact package rule.**
   - Added `entity.name.type.precept` → `#9AA8B5` to `tools/Precept.VsCode/package.json`.
   - This closes a real grammar/package gap: the generated grammar uses `entity.name.type.precept`, but the package only had `storage.type.precept` before this pass.
3. **Regression coverage added.**
   - `test/Precept.Tests/Language/TextMateGrammarTests.cs` now asserts `ruleDesugaringModifiers` uses `keyword.other.constraint.precept`.
   - `test/Precept.LanguageServer.Tests/ExtensionManifestTests.cs` now asserts the gold constraint color and the exact type-keyword fallback color.
4. **Docs synced.**
   - Updated `docs/compiler/grammar-generator.md` to describe the new `ruleDesugaringModifiers` scope correctly.

## Validation

- `dotnet run --project tools\Precept.GrammarGen\Precept.GrammarGen.csproj -- --output tools\Precept.VsCode\syntaxes\precept.tmLanguage.json`
- `dotnet test test\Precept.Tests\Precept.Tests.csproj --filter TextMateGrammarTests`
- `dotnet test test\Precept.LanguageServer.Tests\Precept.LanguageServer.Tests.csproj --filter ExtensionManifestTests`
- `npm run compile` (from `tools\Precept.VsCode`)

All passed.

## Full grammar scope inventory (post-fix)

| Scope | Current color | Style | Package match | Match mode |
|---|---|---|---|---|
| comment.line.number-sign.precept | #9096A6 | italic | comment.line.number-sign.precept | exact |
| constant.character.escape.precept | theme/default | normal | — | none |
| constant.language.precept | #84929F | normal | constant.language.precept | exact |
| constant.numeric.precept | #84929F | normal | constant.numeric.precept | exact |
| entity.name.function.event.precept | #30B8E8 | normal | entity.name.function.event.precept | exact |
| entity.name.type.precept | #9AA8B5 | normal | entity.name.type.precept | exact |
| entity.name.type.precept.precept | #A5B4FC | normal | entity.name.type.precept.precept | exact |
| entity.name.type.state.precept | #A898F5 | normal | entity.name.type.state.precept | exact |
| keyword.control.precept | #4338CA | normal | keyword.control.precept | exact |
| keyword.declaration.precept | #4338CA | normal | keyword.declaration.precept | exact |
| keyword.operator.arrow.precept | #6366F1 | normal | keyword.operator.arrow.precept | exact |
| keyword.operator.membership.precept | #6366F1 | normal | keyword.operator.membership.precept | exact |
| keyword.operator.precept | #6366F1 | normal | keyword.operator.precept | exact |
| keyword.other.access-mode.precept | #4338CA | bold | keyword.other.access-mode.precept | exact |
| keyword.other.assertion.precept | #4338CA | bold | keyword.other.assertion.precept | exact |
| keyword.other.connective.precept | #6366F1 | normal | keyword.other.connective.precept | exact |
| keyword.other.constraint.precept | #FBBF24 | normal | keyword.other.constraint.precept | exact |
| keyword.other.grammar.precept | #6366F1 | normal | keyword.other.grammar.precept | exact |
| keyword.other.outcome.precept | #4338CA | bold | keyword.other.outcome.precept | exact |
| keyword.other.quantifier.precept | #4338CA | normal | keyword.other.quantifier.precept | exact |
| keyword.other.semantic.precept | #4338CA | bold | keyword.other.semantic.precept | exact |
| meta.access-mode.precept | theme/default | normal | — | none |
| meta.action.state.precept | theme/default | normal | — | none |
| meta.collection-member.precept | theme/default | normal | — | none |
| meta.declaration.event.precept | theme/default | normal | — | none |
| meta.declaration.precept.precept | theme/default | normal | — | none |
| meta.declaration.state.precept | theme/default | normal | — | none |
| meta.ensure.event.precept | theme/default | normal | — | none |
| meta.ensure.state.precept | theme/default | normal | — | none |
| meta.event-arg-ref.precept | theme/default | normal | — | none |
| meta.field-declaration.precept | theme/default | normal | — | none |
| meta.handler.event.precept | theme/default | normal | — | none |
| meta.message.because.precept | theme/default | normal | — | none |
| meta.message.reject.precept | theme/default | normal | — | none |
| meta.omit.precept | theme/default | normal | — | none |
| meta.rule.precept | theme/default | normal | — | none |
| meta.transition.header.precept | theme/default | normal | — | none |
| meta.transition.target.precept | theme/default | normal | — | none |
| punctuation.accessor.precept | #6366F1 | normal | punctuation.accessor.precept | exact |
| punctuation.precept | #6366F1 | normal | punctuation.precept | exact |
| punctuation.separator.comma.precept | #6366F1 | normal | punctuation.separator.comma.precept | exact |
| storage.modifier.state.precept | #9AA8B5 | normal | storage.modifier.state.precept | exact |
| storage.type.precept | #9AA8B5 | normal | storage.type.precept | exact |
| string.quoted.double.message.precept | #FBBF24 | normal | string.quoted.double.message.precept | exact |
| string.quoted.double.precept | #84929F | normal | string.quoted.double.precept | exact |
| string.quoted.single.precept | #84929F | normal | string.quoted.single.precept | exact |
| support.function.precept | theme/default | normal | — | none |
| variable.other.field.precept | #A5B4FC | normal | variable.other.field.precept | exact |
| variable.other.precept | #A5B4FC | normal | variable.other.precept | exact |
| variable.other.property.precept | #A5B4FC | normal | variable.other.property.precept | exact |
| variable.parameter.precept | #9AD8E8 | normal | variable.parameter.precept | exact |
| variable.parameter.property.precept | #9AD8E8 | normal | variable.parameter.property.precept | exact |

## Mapping against the canonical visual-system notes

Canonical locked palette from `design/system/semantic-visual-system-notes.md`:

- Structure semantic: `#4338CA`
- Structure grammar: `#6366F1`
- State: `#A898F5`
- Event: `#30B8E8`
- Data name: `#B0BEC5`
- Data type: `#9AA8B5`
- Data value: `#84929F`
- Rule/message: `#FBBF24`
- Comment: `#9096A6`

### Aligned after this pass

- Structure grammar lane: `keyword.other.grammar.precept`, operator scopes, punctuation scopes
- Structure semantic lane: `keyword.other.semantic.precept`, `keyword.declaration.precept`, `keyword.control.precept`, `keyword.other.access-mode.precept`, `keyword.other.assertion.precept`, `keyword.other.outcome.precept`, `keyword.other.quantifier.precept`
- State lane: `entity.name.type.state.precept`
- Event lane: `entity.name.function.event.precept`
- Data type lane: `entity.name.type.precept`, `storage.type.precept`, `storage.modifier.state.precept`
- Data value lane: `constant.language.precept`, `constant.numeric.precept`, `string.quoted.double.precept`, `string.quoted.single.precept`
- Rule/message lane: `string.quoted.double.message.precept`, `keyword.other.constraint.precept`
- Comment lane: `comment.line.number-sign.precept`

## Gaps and mismatches still present

1. **Data-name lane drift remains in package colors.**
   - The canonical visual-system doc locks data names to `#B0BEC5`.
   - The extension still uses non-canonical hues for these scopes:
     - `variable.other.field.precept` → `#A5B4FC`
     - `variable.other.property.precept` → `#A5B4FC`
     - `variable.other.precept` → `#A5B4FC`
     - `variable.parameter.precept` → `#9AD8E8`
     - `variable.parameter.property.precept` → `#9AD8E8`
     - `entity.name.type.precept.precept` → `#A5B4FC`
   - The corresponding constrained semantic-token fallback scopes in `package.json` (`*.constrained.precept`) still inherit that same non-canonical split.
   - I did **not** change these in this pass because the semantic-token metadata in `src/Precept/Language/SemanticTokenTypes.cs` still encodes the same colors, and Shane explicitly scoped semantic-token work out of this audit. Changing only TextMate would create a startup/steady-state color disagreement.

2. **`support.function.precept` has no explicit TextMate rule.**
   - Current rendering: theme/default.
   - Likely intended semantic lane: data-name, but the canonical visual-system note does not explicitly call built-in functions out.
   - Not fixed in this pass.

3. **`constant.character.escape.precept` has no explicit TextMate rule.**
   - Current rendering: theme/default.
   - Likely intended semantic lane: data-value (`#84929F`) because it lives inside string literals.
   - Not fixed in this pass.

4. **Meta scopes are intentionally uncolored wrappers.**
   - All `meta.*.precept` scopes still resolve to theme/default, which is acceptable because they are structural wrapper scopes, not author-facing semantic lanes.

## Typed literals finding

- In the grammar, a typed literal such as `'5 {USD}'` is emitted as one single TextMate token: `string.quoted.single.precept`.
- Regular literals land on:
  - `constant.numeric.precept` for `5`
  - `string.quoted.double.precept` for `"x"`
  - `constant.language.precept` for `true` / `false`
- In the **TextMate grammar/package layer**, typed literals are already on the same semantic lane as other data values: `string.quoted.single.precept` is `#84929F`, matching the canonical data-value color.
- So the typed-literal visual difference Shane noticed is **not** a TextMate-scope gap.
- The remaining likely source is the semantic-token layer: the language server emits the standard semantic token type `"string"` for typed constants (documented in `docs/Working/syntax-coloring-fix-design.md`), which can pick up theme string colors and diverge from the TextMate fallback. That is a real drift, but it is outside this pass because semantic-token work was explicitly excluded.
- If/when that semantic-token drift is fixed, typed literals should stay on the canonical **data-value** color: `#84929F`.
