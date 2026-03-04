# Copilot Instructions for StateMachine

## Documentation Sync Is Mandatory

When making any code, interface, test, or behavior change, keep documentation in sync in the same edit pass.

### Source of Truth

- designs in `docs` are the canonical design decision records.
- `README.md` is the public project narrative and usage guide.
- If they diverge, update both so they agree before completing the task.

## README Must Track Real Implementation

On every meaningful change, review `README.md` and update impacted sections, including:

- API names/signatures and type names
- Behavioral semantics (especially inspect/fire outcomes and exceptions)
- Thread-safety/concurrency statements
- Examples/snippets that reference changed APIs
- Feature claims that no longer match current code
- Sample files that are affected by changes

Do not leave aspirational claims as if implemented. If behavior is planned but not implemented, mark it clearly as design-phase or pending.

## Required Sync Checklist (Run Before Final Response)

1. Did runtime behavior change?
   - If yes, update README behavior descriptions and exception table.
2. Did interfaces or fluent API names change?
   - If yes, update README examples and terminology.
3. Did tests move from skipped to active (or vice versa)?
   - If yes, update README current-status wording.
4. Did any design decision change?
   - If yes, update corresponding design in `docs` and any corresponding README section.
5. Did sample files change?
   - If yes, ensure `README.md` is updated accordingly.

## DSL Syntax Reference Sync (Non-Negotiable)

When any DSL grammar, keyword, rule, or semantics change (for example: `set`, `state <Name> initial`, branch constraints, operators, null rules), update these sections in the same pass:

- `README.md` → `## DSL Syntax Reference`
- `README.md` → `## DSL Cookbook`
- `docs/DesignNotes.md` → `### DSL Syntax Contract (Current)`

These sections must not contradict each other. If one is updated, all relevant sections must be updated before final response.

## Syntax Highlighting Grammar Sync (Non-Negotiable)

The TextMate grammar at `tools/StateMachine.Dsl.VsCode/syntaxes/state-machine-dsl.tmLanguage.json` must stay in sync with the DSL parser at `src/StateMachine/Dsl/StateMachineDslParser.cs`.

When any of the following change, update the grammar file in the same pass:

- New keywords added (control, action, type, or collection)
- New statement or declaration forms (e.g. a new block type like `edit`)
- New expression constructs or operators
- New collection type kinds or inner types
- Changes to identifier naming rules
- Any DSL Syntax Contract change in `docs/DesignNotes.md`

### Grammar Sync Checklist

For every new or changed DSL construct, verify the grammar covers:

1. **Declaration form** — does the keyword appear at the start of a line? Add/update a named declaration pattern with capture groups for the keyword and following identifier.
2. **Keyword** — is it a control keyword (`if/else/from/on/state/event/machine/initial`) or action keyword (`set/transition/reject/rule/add/remove/…`)? Add to the correct `controlKeywords` or `actionKeywords` alternation.
3. **Type token** — is it a type name (`string/number/boolean`) or collection type (`set<T>/queue<T>/stack<T>`)? Add to `typeKeywords`.
4. **Operator** — is it a new operator symbol? Add to `operators` in priority order (multi-char before single-char).
5. **Identifier references** — identifiers in expression positions are caught by the `identifierReference` catch-all; no change needed unless a new dotted form (like `EventName.ArgName`) is introduced, in which case add a dedicated pattern before `identifierReference`.
6. **Pattern ordering** — specific patterns (declarations, dotted refs) must appear before general ones (type keywords, identifier catch-all). Verify the top-level `patterns` array order is still correct after changes.

## Intellisense Sync (Non-Negotiable)

The completions and semantic tokens in `tools/StateMachine.Dsl.LanguageServer/SmDslAnalyzer.cs` and `tools/StateMachine.Dsl.LanguageServer/SmSemanticTokensHandler.cs` must stay in sync with the DSL parser whenever the language surface changes.

When any of the following change, update both files in the same pass:

- New keywords, operators, or type names
- New statement forms or block types with their own context (e.g. a new `from … edit` block)
- New expression positions where identifiers or operators can appear
- New dotted accessor forms (e.g. `Collection.count`, `EventName.ArgName`)
- New collection kinds or inner types

### Completions Sync Checklist (`SmDslAnalyzer.cs`)

For every new or changed DSL construct:

1. **Keyword in `KeywordItems`** — is the new keyword word visible in the global fallback list? Add it.
2. **Context-specific trigger** — does the keyword start or appear within a specific line position (e.g. after `from … on`, after `set =`, at the start of a block body)? Add a regex branch to `GetCompletions` that detects that position and returns the correct item set.
3. **Identifier scope** — are field names, event names, arg names, or state names valid completions in the new context? Reuse `BuildGuardCompletions` or `BuildExpressionCompletions` as appropriate, or build a new dedicated helper.
4. **Dotted member access** — if the new construct allows `Identifier.member` access, add it to the dot-trigger branch and the member suggestion list.
5. **Snippets** — if the construct has a required structure, add a snippet to the relevant snippet list (`GlobalSnippetItems`, `GuardSnippetItems`, `SetSnippetItems`, `TransitionSnippetItems`).

### Semantic Tokens Sync Checklist (`SmSemanticTokensHandler.cs`)

For every new or changed DSL construct:

1. **Keyword token** — add the new keyword to `KeywordTokens` so it receives `keyword` coloring.
2. **Declaration pattern** — if the construct introduces a `keyword Identifier` header line, add a regex to `HighlightNamedSymbols` that pushes the identifier with the correct token type (`type` for states/machines, `function` for events, `variable` for fields).
3. **Expression identifiers** — bare identifiers in expression positions (guards, `set` RHS, `rule` expressions) are covered by the `ExpressionLineRegex` + `IdentifierInExprRegex` pass; update `ExpressionLineRegex` if the new construct introduces a new expression-containing line prefix.
4. **Dotted references** — if a new dotted form is introduced, verify `EventArgRefRegex` (or a new equivalent regex) covers it and pushes both parts with the correct token types.
5. **Operator** — if a new operator symbol is introduced, update `OperatorRegex` with multi-char forms listed before single-char forms.

## Current-Status Hygiene

Maintain a concise "Current Status" section in `README.md` that reflects:

- what is implemented now
- what remains stubbed/pending
- current concurrency model

Update this section whenever those facts change.

## Scope Discipline

- Keep doc updates focused and factual.
- Prefer minimal, accurate wording over broad marketing language.
- If uncertain whether a claim is implemented, verify from code/tests first.

## Design Option Responses

When providing design-option responses, include concrete usage examples to illustrate the implementation and clarify the context of the options presented.

## Deliverable Expectation

Unless explicitly told not to, include documentation synchronization as part of every relevant code change.
