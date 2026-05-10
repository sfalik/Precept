## Core Context

- Owns tooling surfaces: language server, VS Code extension, grammar sync, plugin wiring, MCP ergonomics, and executable developer workflows.
- Keeps grammar, completions, semantic tokens, tests, and tooling docs synchronized with the actual DSL and server surface.
- The durable tooling rule: prefer precise, runnable guidance and avoid claims the extension, language server, or generators cannot currently support.

## Learnings

- Tooling trust depends on accurate status language: metadata readiness is not the same thing as implemented handlers.
- VS Code extension packaging can bundle `src/extension.ts` into `out/extension.js` with esbuild while keeping `npm run compile` as the dev/type-check path; using `vscode:prepublish` makes every VSIX build pick up the bundle without touching the shipped `server/` payload.
- Grammar and completion changes are safest when specific patterns land before generic catch-alls and are backed by regression tests.
- Proof diagnostics become brittle when they depend on prose instead of structured metadata; keep publication paths but prefer durable data contracts.
- For custom DSL documentation, truthful code-fence labels and accurate path/build guidance matter more than cosmetic approximations.
- `IsMessagePosition` now drives `messageStrings` generation in `Precept.GrammarGen`: token flags emit keyword-plus-gold-string patterns, and function flags reserve the future trailing-argument path.
- No built-in functions currently opt into `IsMessagePosition`; the generator intentionally emits only token-derived message patterns today while keeping the function wiring live.
- Added the VS Code task label `grammar: regenerate`, which runs `dotnet run --project tools/Precept.GrammarGen -- --output tools/Precept.VsCode/syntaxes/precept.tmLanguage.json` so developers can refresh the generated TextMate grammar after catalog changes.
- `ModifierMeta.DesugarsToRule` now wires into `Precept.GrammarGen` through a derived `ruleDesugaringModifiers` repository entry built from `Modifiers.All.Where(m => m.DesugarsToRule)` rather than a hand-maintained modifier list.
- The gold keyword scope for rule-desugaring modifier highlights is `keyword.other.grammar.precept`; it must stay ahead of `#constraintKeywords` anywhere modifier patterns are composed so first-match TextMate ordering preserves the gold scope.
- ProofEngine presence diagnostics were missing because `docs/compiler/proof-engine.md` §9 assigned codes 112–115 for four proof requirement families but omitted `PresenceProofRequirement`; fixing the spec gap required adding `UnprovedPresenceRequirement = 116`, wiring the diagnostics catalog and `ProofEngine.cs`, and updating `test/Precept.Tests/DiagnosticsTests.cs` plus `test/Precept.Tests/ProofEngineTests.cs`.
- Workspace maintenance commands in the VS Code extension can stay dependency-free: built-in `https` plus `withProgress` is enough to fetch official source artifacts into repo-relative paths with clear success/error UX.
- ISO 4217 refresh belongs in `.vscode/tasks.json`, not the extension command surface; keep one-off workspace maintenance flows as explicit tasks backed by repo-local scripts.
- The historical SIX Group `iso-4217/lists/list-one.xml` URL now returns 404; the live XML currently resolves under SIX's `iso-currrency/lists/list-one.xml` path, so repo-local refresh tooling should handle source drift explicitly rather than baking brittle editor commands into the extension.
- ISO 4217 reference data now lives in source control at `src/Precept/Data/Iso4217/list-one.xml`, and `CurrencyCatalogSyncTests` now always executes with plain `[Fact]` so catalog drift surfaces as a real failure instead of a discovery-time skip.
- The current VS Code highlighting split is TextMate-first for field/arg differentiation: fields already land on `variable.other.field.precept` plus generic `variable.other.precept` with `#A5B4FC`, while event arg declarations use `variable.parameter.precept` and now take `#9AD8E8`; the language-server semantic surface still only exposes the shared `preceptFieldName` token type for field/argument names.
- 2026-05-09T11:07:24.986-04:00 color audit locked the current VS Code mapping: `#4338CA` → `keyword.other.semantic.precept`, `keyword.control.precept`, `keyword.other.precept`, `keyword.declaration.precept`, `keyword.other.action.precept`, `keyword.other.assertion.precept`, `keyword.other.outcome.precept`, `keyword.other.access-mode.precept`, `keyword.other.quantifier.precept`; `#6366F1` → `keyword.other.constraint.precept`, `keyword.other.connective.precept`, `keyword.operator.comparison.precept`, `keyword.operator.logical.precept`, `keyword.operator.arithmetic.precept`, `keyword.operator.assignment.precept`, `keyword.operator.arrow.precept`, `keyword.operator.membership.precept`, `keyword.operator.precept`, `punctuation.separator.arrow.precept`, `punctuation.separator.comma.precept`, `punctuation.accessor.precept`, `punctuation.section.group.begin.precept`, `punctuation.section.group.end.precept`, `punctuation.precept`; `#A898F5` → `entity.name.type.state.precept`, `entity.name.type.state.constrained.precept`; `#30B8E8` → `entity.name.function.event.precept`, `entity.name.function.event.constrained.precept`; `#A5B4FC` → `variable.other.field.precept`, `variable.other.field.constrained.precept`, `variable.other.property.precept`, `variable.other.precept`, `entity.name.type.precept.precept`; `#9AD8E8` → `variable.parameter.precept`; `#9AA8B5` → `storage.type.precept`, `storage.modifier.state.precept`; `#84929F` → `constant.other.value.precept`, `constant.language.precept`, `constant.numeric.precept`, `constant.language.boolean.precept`, `string.quoted.double.precept`, `string.quoted.single.precept`; `#FBBF24` → `string.quoted.double.message.precept`, `keyword.other.grammar.precept`; `#9096A6` → `comment.line.number-sign.precept`. Semantic-token customizations remain `preceptComment`/`comment:precept` → `#9096A6`, `operator:precept` → `#6366F1`, `preceptFieldName`/`preceptFieldName.preceptConstrained`/`preceptName` → `#A5B4FC`, and `preceptMessage`/`preceptKeywordGrammar` → `#FBBF24`.

## Historical Summary

- Earlier May 2026 tooling work established the standing rules that tooling docs must stay truthful, grammar/scope/semantic-token behavior must remain catalog-driven, and one-off maintenance flows like ISO refreshes belong in explicit workspace tasks rather than extension commands.
- The 2026-05-09 closeout wave already recorded the event-arg parameter-property scope promotion, the tooling-lens LS review reconciliation, Slice 0 infrastructure plus 0b shim deletion, and the first handler batch for diagnostics, semantic tokens, completions, hover, and folding.
- Use `.squad/decisions.md` for the canonical per-batch detail; this history now keeps only the live tooling baseline and the most recent active updates.

## Recent Updates

### 2026-05-10T02:50:04Z — Team update: visual taxonomy and LS prerequisites locked
- 📌 Team update (2026-05-10T02:50:04Z): `SemanticTokenTypes` is now the approved 14th catalog, `TokenMeta` keeps one `VisualCategory`, and token-surface projections (custom type, TextMate scope, base styles, constrained-modifier support) now belong to catalog metadata instead of parallel token fields or manifest copies — decided by Frank and Shane Falik.
- 📌 Team update (2026-05-10T02:50:04Z): George's outline metadata plus snippet-template catalog work are now durably recorded as the completion/outline prerequisite surface that your LS slices consume on top of Slice 0's protocol foundation — decided by George and captured by Scribe.

### 2026-05-10T01:55:00Z — Slice 12 committed: trigger characters + dual-use `set` type context
- Commit `d962d0cb` fixed `CompletionHandler` trigger characters to `[" ", "'", ".", ">", "~"]`, added `SlotContextResolver.IsSetInTypePosition`, and wired the dual-use `set` reclassification into hover and semantic-token projection.
- Durable lessons from the slice stay locked: token-stream lookups should use `StartLine` + `StartColumn` rather than `SourceSpan.Offset`, and type-position `set` must project the type semantic token path rather than the action keyword path.

### 2026-05-10T00:41:09Z — Slices 1/2/4/5/9 handler batch recorded
- The LS now has concrete diagnostics, semantic-token, completion, hover, and folding handlers, with the matching tests that locked the publication, projection, and projection-only semantic contracts.
- Validation stayed green except for the pre-existing `SemanticTokensHandler.CreateRegistrationOptions` access-modifier mismatch in the shared LS baseline.

### 2026-05-10T00:23:31Z — Slice 0b legacy LS cleanup committed
- Commit `51d93dc2` deleted the stub layer (`LanguageServerStubs.cs`, `PreceptPreviewProtocol.cs`, `LegacyHandlerCompat.cs`) and removed 173 legacy shim-facing tests so the LS project keeps only the real harness surface.
- The cleanup gate remained `dotnet build` for the LS projects plus `dotnet test test/Precept.Tests/`, all green in the validated slice run.

