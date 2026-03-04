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
