# Work Routing

How to decide who handles what.

## Routing Table

| Work Type | Route To | Examples |
|-----------|----------|----------|
| Architecture, design decisions, cross-cutting concerns | Frank | API surface changes, breaking changes, architectural proposals |
| DSL engine: parser, type checker, evaluator, compiler, runtime | George | Tokenizer bugs, type inference, state machine logic, constraint evaluation |
| VS Code extension, language server, LSP, TypeScript, TextMate grammar | Kramer | Completions, hover, semantic tokens, syntax highlighting, preview webview |
| MCP server, Copilot plugin, AI integration, agent/skills content | Elaine | MCP tool DTOs, plugin prompts, skill files, AI-native features |
| Testing, xUnit, edge cases, quality gates, regression | Soup Nazi | Test coverage, failing tests, edge case identification, test strategy |
| Code review, PR review, code quality, standards enforcement | Uncle Leo | PR reviews, code smells, style violations, quality audits |
| README, docs, brand, marketplace listings, DevRel | J. Peterman | README updates, brand copy, NuGet/VS Code/Claude marketplace content |
| Roadmap, priorities, releases, @ToDo.md, issue triage | Steinbrenner | Feature prioritization, milestone planning, release sequencing |
| Session logging | Scribe | Automatic — never needs routing |

## Issue Routing

| Label | Action | Who |
|-------|--------|-----|
| `squad` | Triage: analyze issue, assign `squad:{member}` label | Lead |
| `squad:{name}` | Pick up issue and complete the work | Named member |

### How Issue Assignment Works

1. When a GitHub issue gets the `squad` label, the **Lead** triages it — analyzing content, assigning the right `squad:{member}` label, and commenting with triage notes.
2. When a `squad:{member}` label is applied, that member picks up the issue in their next session.
3. Members can reassign by removing their label and adding another member's label.
4. The `squad` label is the "inbox" — untriaged issues waiting for Lead review.

## Rules

1. **Eager by default** — spawn all agents who could usefully start work, including anticipatory downstream work.
2. **Scribe always runs** after substantial work, always as `mode: "background"`. Never blocks.
3. **Quick facts → coordinator answers directly.** Don't spawn an agent for "what port does the server run on?"
4. **When two agents could handle it**, pick the one whose domain is the primary concern.
5. **"Team, ..." → fan-out.** Spawn all relevant agents in parallel as `mode: "background"`.
6. **Anticipate downstream work.** If a feature is being built, spawn the tester to write test cases from requirements simultaneously.
7. **Issue-labeled work** — when a `squad:{member}` label is applied to an issue, route to that member. The Lead handles all `squad` (base label) triage.
