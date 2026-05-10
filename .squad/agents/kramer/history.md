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

## Recent Updates

### 2026-05-10T00:23:31Z — Slice 0b legacy LS cleanup committed
- Commit `51d93dc2` deleted `tools/Precept.LanguageServer/LanguageServerStubs.cs`, `PreceptPreviewProtocol.cs`, and `LegacyHandlerCompat.cs`; the compat shim had to go because it still referenced the removed stub types.
- Deleted 13 legacy shim-facing files from `test/Precept.LanguageServer.Tests/`, removing 173 compiler-redundant tests; only `LspTestHost.cs` and `GlobalUsings.cs` remain, and the LS test project now discovers 0 tests while still building cleanly.
- Validation for the cleanup gate: `dotnet build` succeeds for the language-server and LS test projects, and `dotnet test test/Precept.Tests/` stays green at 3737/3737.

### 2026-05-10T00:11:05Z — Slice 0 language-server infrastructure committed
- Commit `9f6b1fd7` added `tools/Precept.LanguageServer/DocumentState.cs`, `DocumentStore.cs`, `DiagnosticProjector.cs`, `Handlers/TextDocumentSyncHandler.cs`, and `test/Precept.LanguageServer.Tests/LspTestHost.cs`, with `DocumentState` using a volatile `Compilation` field plus `Interlocked.Exchange` and `DocumentStore` using `ConcurrentDictionary<DocumentUri, DocumentState>`.
- `tools/Precept.LanguageServer/Program.cs` now registers `DocumentStore` and `TextDocumentSyncHandler`; the language server builds, and the remaining legacy stub test failures stay expected until Slice 0b deletes the old stub layer.

### 2026-05-09T23:46:43Z — Tooling-lens LS review reconciled
- Kramer corrected the objective handler-registration, capability, semantic-token, and extension-wiring mismatches in the LS docs, and confirmed `TypedField.NameSpan` is the right projection-only fix for field declaration spans.
- The only unresolved tooling decision left from the batch is how `precept/inspect` should surface restore failures (`RestoreInvalidInput` / `RestoreConstraintsFailed`).


### 2026-05-09T15:26:09Z — Color/scope tooling closeout recorded
- Scribe merged Kramer's field/arg color wiring, retired-anchor cleanup, and event-arg member scope implementation into the canonical ledger.
- Durable tooling state: `variable.parameter.property.precept` supersedes the compound-selector workaround, and the VS Code theme no longer relies on the retired `#B0BEC5` anchor for field/arg highlighting.


### 2026-05-09T11:20:45Z — Event-arg member ref scope promoted to dedicated scope (Frank's design)
- Replaced compound-selector workaround (`meta.event-arg-ref.precept variable.other.property.precept`) with proper dedicated scope `variable.parameter.property.precept` on the parameter axis.
- Generator change: `eventArgReference` capture group 3 in `tools/Precept.GrammarGen/Program.cs` (line ~780) changed from `variable.other.property.precept` to `variable.parameter.property.precept`. The `collectionMemberAccess` pattern was intentionally left unchanged — both patterns used the old scope, only `eventArgReference` gets the new one.
- `precept.tmLanguage.json` regenerated; `eventArgReference` pattern now emits `variable.parameter.property.precept` at line 910.
- `package.json` theme: removed dead compound selector; added simple `variable.parameter.property.precept → #9AD8E8` rule immediately after the `variable.parameter.precept` rule.
- Lesson: structural pattern scopes (like `meta.*` wrappers and capture group scopes in `eventArgReference`) live in the generator's hardcoded structural section — not in `TokenMeta.TextMateScope`. When Frank says "no catalog change required," believe it; the generator already has a clear structural section to target.
- Lesson: compound selectors in themes are always temporary hacks. The grammar scope is the right permanent home for semantic distinctions.



### 2026-05-09T14:41:11Z — ISO 4217 refresh converted to task workflow
- `kramer-2` removed the `precept.refreshIso4217` extension command path, added `tools/scripts/refresh-iso4217.js`, and wired the workspace task label `iso4217: refresh`.
- The refresh now follows SIX's live `iso-currrency/lists/list-one.xml` endpoint because the older `iso-4217/lists/list-one.xml` URL returns 404.
- Downloaded XML stays under gitignored `src/Precept/Data/`, with parity validation handled by an optional discovery-time-skipped xUnit test rather than committed fixtures.

### 2026-05-09T09:49:38Z — Token/action parser cleanup batch recorded
- `kramer-3` closed the `TokenKind.Set` dual-category bug: token metadata is now action-only, language-server type completions derive vocabulary from `Types.All`, and the language spec reflects the `Set`/`SetType` split.
- `kramer-4` split `ParseActionByShape` into nine named handlers and enrolled `ActionSyntaxShape` in PRECEPT0019 while preserving malformed-action recovery; targeted analyzer/parser validation stayed green aside from the pre-existing 2 `TokensTests` baseline failures.

### 2026-05-08T22:36:50Z — Message-position generator gap closed
- The grammar generator now derives `messageStrings` patterns from `Tokens.All.Where(m => m.IsMessagePosition)` and the parallel `FunctionMeta` path.
- Current output stays token-only because no built-ins opt in yet; `precept.tmLanguage.json` regenerated with zero diff on commit `7f3842fd`.
- Future trailing message-string built-ins now have catalog support with no new hardcoded grammar logic required.

### 2026-05-08T05:27:37Z — Grammar generator implementation durably recorded
- Scribe merged Kramer's PR #139 implementation note into `.squad/decisions.md`, capturing the 16 must-fix closures, 42 repository patterns, stale pattern and keyword removals, and the remaining function-argument message-string metadata block.
- Active follow-up stays narrow: the generator cannot gold-scope function-argument message strings until message-position metadata exists, so promotion to the canonical grammar remains gated.

### 2026-05-08T03:29:02Z — Wave 2 tooling closeout recorded
- `kramer-invest` confirmed the Precept Language Server test corpus still exists on `main`; the v2 branch is intentionally stubbed rather than accidentally regressed.
- All six Wave 2 design gates D1–D6 are now closed in `.squad/decisions.md`, and Kramer's active continuation work remains the grammar-generator scaffold plus LS test-port follow-through.

### 2026-05-08T03:08:18Z — Comprehensive tooling doc review recorded
- Kramer corrected `docs/tooling/extension.md`, `docs/tooling/language-server.md`, and `docs/compiler/tooling-surface.md` to match the branch's actual tooling state.
- Durable follow-ups now logged in `.squad/decisions.md`: clarify design-spec vs. implementation-status docs, recover the missing LS test corpus, and decide the grammar-generator ownership path.

### Historical summary through 2026-05-07
- Prior active work covered grammar and completion sync for guards, conditional expressions, `and` / `or` / `not`, stateless edit forms, semantic-token metadata propagation, README/tooling accuracy passes, and the C93 divisor-safety tooling fixes.
- The standing tooling baseline is unchanged: docs must reflect reality, tests are the safest spec anchor for language-server behavior, and future tooling derivation should come from catalog metadata rather than hand-maintained lists.

### 2026-05-09T11:15:46.104-04:00 — Event-arg member ref color override fixed
- Added a TextMate compound selector override in `tools/Precept.VsCode/package.json`: `meta.event-arg-ref.precept variable.other.property.precept` now maps to `#9AD8E8` before the general `variable.other.property.precept` field-color rule.
- This closes the precision gap where `LoadParcel.Recipient`-style event arg member references inherited the field color `#A5B4FC` even though they semantically belong to the arg color family.
- Lesson: `variable.other.property.precept` is reused across both field references and event-arg member references, so context-sensitive color intent must be expressed with compound selectors rather than assuming the base scope is unique.

### 2026-05-09T15:21:46Z — Scribe merged the event-arg scope batch
- `.squad/decisions.md` now records the durable outcome from `kramer-8`: the grammar emits `variable.parameter.property.precept`, so the earlier compound-selector workaround is officially superseded rather than left as the permanent fix.
- The merged ledger entry also preserves the related field/arg color cleanup as part of the same semantic-token realignment.

### 2026-05-09T20:00:24.839-04:00 — Slice 0 LS infrastructure landed
- Slice 0 complete: `DocumentState`, `DocumentStore`, `DiagnosticProjector`, and `Handlers/TextDocumentSyncHandler` created.
- `test/Precept.LanguageServer.Tests/LspTestHost.cs` created as the reusable in-process LSP harness for later protocol-layer slices.
- `tools/Precept.LanguageServer/Program.cs` now registers `DocumentStore` and `TextDocumentSyncHandler` through OmniSharp DI.
- OmniSharp 0.19.9 quirks observed: text-sync registration uses `TextSynchronizationCapability` in `CreateRegistrationOptions`, server/client in-process startup uses `LanguageServer.PreInit(...)` + `LanguageClient.PreInit(...)`, and the test harness needs the separate `OmniSharp.Extensions.LanguageClient` package.
- Added a temporary `LegacyHandlerCompat` shim so the legacy LS test project compiles without touching `LanguageServerStubs.cs`; Slice 0b should delete the whole shim layer.
