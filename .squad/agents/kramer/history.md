## 2026-04-04T20:28:43Z — Orchestration: Elaine Palette Mapping Polish

Elaine completed beautification and unification of palette mapping visual treatments in \rand\brand-spec.html\ §2.1 (Syntax Editor) and §2.2 (State Diagram). Created \.spm-*\ CSS component system (~70 lines) to match polished §1.4 color system design. All locked semantic colors, mappings, and tokens preserved. System is general-purpose and applicable to future surface sections (Inspector, Docs, CLI).

**Decisions merged to decisions.md:** 35 inbox items (palette structure, color roles, semantic reframes, surfaces, README reviews, corrections, final verdicts)

**Status:** Complete. Ready for integration.

# Project Context

- **Owner:** shane
- **Project:** Precept — domain integrity engine for .NET. DSL that makes invalid states structurally impossible.
- **Stack:** C# / .NET 10.0 (language server), TypeScript (VS Code extension), xUnit
- **My domain:** `tools/Precept.LanguageServer/` (C# LSP server) and `tools/Precept.VsCode/` (TypeScript extension)
- **Key files:** `PreceptAnalyzer.cs` (completions/hover), `PreceptSemanticTokensHandler.cs`, `syntaxes/precept.tmLanguage.json` (grammar)
- **Critical sync rule:** When George changes the DSL parser, I must update both `tmLanguage.json` AND `PreceptAnalyzer.cs` in the same pass
- **Build:** `dotnet build tools/Precept.LanguageServer/...` for LS; `npm run compile` in `tools/Precept.VsCode/` for extension
- **Tests:** `test/Precept.LanguageServer.Tests/`
- **Created:** 2026-04-04

## Learnings

### 2026-04-05: Comprehensive Tooling Knowledge Refresh

#### LSP Feature Set (5 handlers + analyzer)

1. **Completions (PreceptCompletionHandler)**
   - Trigger chars: space, dot
   - ~370 lines in PreceptAnalyzer.cs — heavy regex-based context detection
   - Suppresses keywords during identifier invention (e.g. "precept Name", "field Name", "state Name")
   - Context-aware: invariant scope ≠ event assert scope ≠ guard expression scope ≠ action/outcome pipeline
   - Collection member access (Floors.count, Floors.min, Floors.max, Floors.peek)
   - Event arg member access (EventName.ArgName)
   - Typed completions for `set`, `add/remove/enqueue/dequeue/push/pop`, guard expressions
   - Snippet generation for `reject "reason"`, `because "reason"`, field/event/state declarations
   - Arrow pipeline suggestions (after `->` or after guarded transition expression)
   - 18 static completion item lists (KeywordItems, ArrowItems, TypeItems, ScalarTypeItems, LiteralItems, etc.)

2. **Semantic Tokens (PreceptSemanticTokensHandler)**
   - 17 token types: keyword, type, function, variable, number, string, operator, comment, preceptComment, preceptKeywordSemantic, preceptKeywordGrammar, preceptState, preceptEvent, preceptFieldName, preceptType, preceptValue, preceptMessage
   - 1 modifier: preceptConstrained (planned, not yet emitted)
   - Comments scanned from raw text (tokenizer strips them)
   - Token stream classified via `ClassifyTokens()` using context-aware rules (StateContextTokens, EventContextTokens, FieldContextTokens)
   - Catalog-driven: `PreceptTokenMeta.GetCategory()` maps token enum → semantic type (eliminates manual handler edits when new tokens added)
   - Tests: PreceptSemanticTokensClassificationTests (6+ tests covering comments, message strings, constrained modifiers)

3. **Diagnostics (PreceptAnalyzer.GetDiagnostics)**
   - Parse diagnostics (position info, line/column tracking) via `PreceptParser.ParseWithDiagnostics()`
   - Validation diagnostics via `PreceptCompiler.Validate()`
   - Semantic diagnostics via `GetSemanticDiagnostics()` (warns on reject-only events, orphaned events, unsupported collection mutations)

4. **Hover (PreceptHoverHandler)**
   - Delegates to `PreceptDocumentIntellisense.CreateHover()`
   - Provides contextual help (field types, event args, state names, etc.)

5. **Go-to-Definition (PreceptDefinitionHandler)**
   - Delegates to `PreceptDocumentIntellisense.CreateDefinition()`
   - Resolves symbols (states, events, fields, event args) to their declaration locations

#### TextMate Grammar (`precept.tmLanguage.json` — 407 lines)

**Patterns in top-level array** (order matters):
1. comment, messageStrings (capture `because`/`reject` reason strings), strings
2. machineDeclaration (precept Name), stateDeclaration, eventWithArgsDeclaration, eventDeclaration
3. fieldCollectionDeclaration, fieldScalarDeclaration
4. invariantStatement, assertStatement
5. fromOnHeader (from State on Event), transitionTarget (transition State), eventArgReference (Event.Arg), collectionMemberAccess (Col.count, Col.min, etc.)
6. controlKeywords, declarationKeywords, typeKeywords, actionKeywords, outcomeKeywords
7. arrowOperator (->), operators, booleanNull, numbers, identifierReference (catch-all)

**Repository patterns** (named pattern library):
- controlKeywords: precept, state, event, from, on, if, else, initial, when, in, to, any, of
- declarationKeywords: field, as, with, assert, because, nullable, default, invariant, edit
- typeKeywords: string, number, boolean, set, queue, stack
- actionKeywords: set, add, remove, enqueue, dequeue, push, pop, clear, into, contains
- outcomeKeywords: transition, reject, no transition

**Key scopes**:
- keyword.control.precept (control keywords, "initial")
- keyword.other.precept (declaration, action, outcome keywords)
- storage.type.precept (type keywords)
- entity.name.type.state.precept (state names after "state", "from", "on", "transition", "in", "to")
- entity.name.function.event.precept (event names after "event", "on")
- variable.other.field.precept (field names after "field", "set", "add", "remove", etc.)
- variable.other.property.precept (event arg refs and collection members)
- string.quoted.double.message.precept (strings after "because", "reject")

**Known patterns**: Multi-name declarations (state S1, S2; event E1, E2; field F1, F2) are parsed via capture group iteration on comma-separated identifiers.

#### Extension Architecture (`tools/Precept.VsCode/`)

**Entry point: extension.ts (470+ lines)**
- Language client setup: LS discovery (dev build or bundled), launch configuration, restart on file changes
- Commands: precept.openPreview (inspector webview), precept.togglePreviewLocking, precept.showLanguageServerMode
- Status bar item showing LS status
- Dev mode: watches build output folder, schedules automatic LS restart on change
- Preview panel: webview + ELK graph layout engine integration

**Key files**:
- package.json: manifest, activation events, commands, keybindings, grammar reference, semantic highlight config
- src/extension.ts: activation, LS startup, command registration, preview handler
- webview/: preview UI (not yet fully documented)
- syntaxes/precept.tmLanguage.json: grammar
- language-configuration.json: bracket pairs, comments, word patterns

#### Document Intelligence (`PreceptDocumentIntellisense.cs`)

**Fallback parsing** (when main parser fails or returns null):
- Regex extraction of states, events, fields, collection fields, event args
- Handles incomplete syntax (e.g. cursor in middle of declaration)
- Builds collection kinds, inner types, field type kinds for completion context

**Symbol resolution**:
- Declaration lookup via line/column position
- Dotted symbol resolution (Event.Arg, Field.count)
- Event assert context detection (args available, not fields)

#### Known Gaps & Drift Risks

1. **Grammar ↔ Parser Drift**: TextMate grammar uses hardcoded identifier patterns (`[A-Za-z_][A-Za-z0-9_]*`). Parser lives in C# (PreceptParser.cs). If identifier naming rules change in parser, grammar won't catch it automatically. **Mitigation**: PR description must call this out.

2. **Completions ↔ Parser Drift**: PreceptAnalyzer.cs has 7+ large regexes mirroring parser syntax (FromOnRegex, NewFieldDeclRegex, NewEventWithArgsRegex, etc.). If parser syntax changes, completions become stale. **Mitigation**: CRITICAL SYNC RULE — when George changes the parser, Kramer must update both grammar and PreceptAnalyzer in the same pass.

3. **Semantic Tokens Modifier**: `preceptConstrained` is registered in legend but never emitted. Phase 7 of the implementation plan will emit it when a state/event/field has constraint relationships detected. **Status**: Awaiting Phase 7.

4. **Uncovered completions**: 
   - No type-checking validation for assignment expressions (e.g. "set Balance = true" when Balance is number — completion happens, but validator must catch this)
   - No type narrowing after guards (e.g. "when Balance > 0 → assume Balance is positive" — not tracked, user gets full expression list)
   - Typed dequeue/pop "into" completions only filter by collection inner type, not by var scope (no local var tracking)

5. **Hover/Definition limitations**:
   - No quick info for built-in collection members (Floors.count shows no tooltip)
   - Hover on EventName.ArgName resolves correctly, but precept name (top-level identifier) shows no definition location

6. **Grammar coverage gaps**:
   - No syntax highlighting for "rule" keyword (if planned in future DSL)
   - "contains" keyword appears in actionKeywords but is rarely used in practice
   - No special pattern for numeric literal edge cases (NaN, Infinity)

#### Test Coverage

**PreceptAnalyzerCompletionTests** (~100+ facts):
- Completion suppression in name-invention positions
- Scope isolation (invariant ≠ event assert ≠ guard expression)
- Collection member access, event arg access
- Arrow pipeline completions
- Typed set/mutation expression completions
- Event arg declaration completions with nullable/default modifiers

**PreceptSemanticTokensClassificationTests** (~10+ facts):
- Comment classification
- Message strings (because, reject)
- Precept name coloring
- Constrained state modifier detection

**PreceptIntellisenseNavigationTests**:
- Symbol resolution via ResolveSymbol()
- Document symbol list (goto symbol)

#### Design Decisions (from SyntaxHighlightingDesign.md)

- 8-shade semantic palette locked (dark-mode only)
- Two information axes: color (category) + typography (constraint pressure via italic)
- Semantic tokens layer overrides TextMate layer
- semanticTokenScopes + Precept TextMate fallback ensures color stability
- Implementation plan spans 7 phases (Phase 0: Grammar refactor, Phase 1-2: Token types, Phase 3-5: Color binding, Phase 6-7: Modifiers + extension)

#### Critical Sync Requirements (Summarized)

1. **Parser syntax change** → Update `precept.tmLanguage.json` + `PreceptAnalyzer.cs` regex patterns
2. **New keywords/operators** → Update grammar, analyzer (KeywordItems lists), semantic tokens legend if new category
3. **New token categories** → Update `BuildSemanticTypeMap()`, `BuildKeywordItems()`, and consumers
4. **Event args / field types** → Cross-check `PreceptDocumentIntellisense.cs` regex patterns
5. **Completions behavior change** → Update tests in `PreceptAnalyzerCompletionTests.cs`

### 2026-04-05: README Badge Cleanup + Sample Count Fix

**Requested by:** Shane

#### Badge fixes
- **Removed** broken Build Status badge — no `build.yml` CI workflow exists in `.github/workflows/` (only squad/sync workflows). Badge would have permanently shown "unknown".
- **Removed** broken VS Code Extension marketplace badge — `package.json` publisher is `"local"` (unpublished). Badge would permanently fail against marketplace API.
- **NuGet badge** already correct — package ID is `Precept`, matching `src/Precept/Precept.csproj` (no explicit `<PackageId>`, defaults to project name).
- **License badge** already correct.
- **Fixed** placeholder `AuthorName.precept-vscode` → `sfalik.precept-vscode` in Quick Start install command (derived from `git remote get-url origin` → `https://github.com/sfalik/Precept.git`).

#### Sample count fix
- README claimed "20 fully commented workflows" but `samples/` contained 21 `.precept` files.
- Missing entry: `crosswalk-signal.precept` (Simple complexity — boolean fields, `from any on`, `edit`, `in <State> assert`, countdown arithmetic).
- Updated count to 21, added row to sample catalog table after `trafficlight.precept`, and added to 3 feature coverage matrix rows (`in <State> assert`, `edit`, `from any on`).

#### No catalog/constructs count claim found
- Searched README for numeric claims about constructs, catalog items, or language features. None found.

