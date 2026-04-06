# Work Routing

How to decide who handles what.

## Routing Table

| Work Type | Route To | Examples |
|-----------|----------|----------|
| Architecture, design decisions, cross-cutting concerns | Frank | API surface changes, breaking changes, architectural proposals |
| Language design, grammar evolution, new DSL constructs, Superpower parser strategy | Frank | New keywords, syntax proposals, grammar philosophy, parser-combinator feasibility, `PreceptLanguageDesign.md` updates |
| DSL engine: parser, type checker, evaluator, compiler, runtime | George | Tokenizer bugs, type inference, state machine logic, constraint evaluation |
| VS Code extension, language server, LSP, TypeScript, TextMate grammar | Kramer | Completions, hover, semantic tokens, syntax highlighting, preview webview |
| MCP server, Copilot plugin, AI integration, agent/skills content | Newman | MCP tool DTOs, plugin prompts, skill files, AI-native features |
| Testing, xUnit, edge cases, quality gates, regression | Soup Nazi | Test coverage, failing tests, edge case identification, test strategy |
| Code review, PR review, code quality, standards enforcement | Uncle Leo | PR reviews, code smells, style violations, quality audits |
| README, docs, brand, marketplace listings, DevRel | J. Peterman | README updates, brand copy, NuGet/VS Code/Claude marketplace content |
| UX design, VS Code extension UI, diagram layout, interaction design, accessibility | Elaine | Preview webview design, hover UI, state diagram visual layout, UX specs for Kramer to implement |
| Roadmap, priorities, releases, @ToDo.md, issue triage | Steinbrenner | Feature prioritization, milestone planning, release sequencing |
| Final human review, sign-off, approval requests | Shane | PR approval, design sign-off, go/no-go review when the team needs a human verdict |
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
4. The `squad` label marks the shared backlog entry point — issues waiting for Lead triage and board placement.

## Rules

1. **Eager by default** — spawn all agents who could usefully start work, including anticipatory downstream work.
2. **Scribe always runs** after substantial work, always as `mode: "background"`. Never blocks.
3. **Quick facts → coordinator answers directly.** Don't spawn an agent for "what port does the server run on?"
4. **When two agents could handle it**, pick the one whose domain is the primary concern.
5. **"Team, ..." → fan-out.** Spawn all relevant agents in parallel as `mode: "background"`.
6. **Anticipate downstream work.** If a feature is being built, spawn the tester to write test cases from requirements simultaneously.
7. **Issue-labeled work** — when a `squad:{member}` label is applied to an issue, route to that member. The Lead handles all `squad` (base label) triage.
8. **Human review routing** — when Shane is requested as reviewer, present the review item to him and wait for his verdict instead of spawning an agent.
9. **Coordinator never writes domain content.** If the work involves writing or rewriting a proposal, design doc, code artifact, research document, or any domain-specific content, it MUST be spawned to the appropriate agent. The coordinator routes and synthesizes — it does not author. This applies even when the task feels small or the coordinator "already knows the answer."
