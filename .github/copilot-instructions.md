# Copilot Instructions for StateMachine

## Documentation Sync Is Mandatory

When making any code, interface, test, or behavior change, keep documentation in sync in the same edit pass.

### Source of Truth

- `docs/DesignNotes.md` is the canonical design decision record.
- `README.md` is the public project narrative and usage guide.
- If they diverge, update both so they agree before completing the task.

## README Must Track Real Implementation

On every meaningful change, review `README.md` and update impacted sections, including:

- API names/signatures and type names
- Behavioral semantics (especially inspect/fire outcomes and exceptions)
- Thread-safety/concurrency statements
- Examples/snippets that reference changed APIs
- Feature claims that no longer match current code

Do not leave aspirational claims as if implemented. If behavior is planned but not implemented, mark it clearly as design-phase or pending.

## Required Sync Checklist (Run Before Final Response)

1. Did runtime behavior change?
   - If yes, update README behavior descriptions and exception table.
2. Did interfaces or fluent API names change?
   - If yes, update README examples and terminology.
3. Did tests move from skipped to active (or vice versa)?
   - If yes, update README current-status wording.
4. Did any design decision change?
   - If yes, update `docs/DesignNotes.md` and any corresponding README section.

## Current-Status Hygiene

Maintain a concise "Current Status" section in `README.md` that reflects:

- what is implemented now
- what remains stubbed/pending
- current concurrency model
- role/status of legacy `FiniteStateMachine`

Update this section whenever those facts change.

## Scope Discipline

- Keep doc updates focused and factual.
- Prefer minimal, accurate wording over broad marketing language.
- If uncertain whether a claim is implemented, verify from code/tests first.

## Design Option Responses

When providing design-option responses, include concrete usage examples to illustrate the implementation and clarify the context of the options presented.

## Deliverable Expectation

Unless explicitly told not to, include documentation synchronization as part of every relevant code change.
