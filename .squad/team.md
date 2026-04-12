# Squad Team

> Precept

## Coordinator

| Name | Role | Notes |
|------|------|-------|
| Squad | Coordinator | Routes work, enforces handoffs and reviewer gates. |

## Members

| Name | Role | Charter | Status |
|------|------|---------|--------|
| Frank | Lead/Architect & Language Designer | `.squad/agents/frank/charter.md` | 🟢 Active |
| George | Runtime Dev | `.squad/agents/george/charter.md` | 🟢 Active |
| Kramer | Tooling Dev | `.squad/agents/kramer/charter.md` | 🟢 Active |
| Newman | MCP/AI Dev | `.squad/agents/newman/charter.md` | 🟢 Active |
| Soup Nazi | Tester | `.squad/agents/soup-nazi/charter.md` | 🟢 Active |
| Uncle Leo | Security Champion | `.squad/agents/uncle-leo/charter.md` | 🟢 Active |
| J. Peterman | Brand/DevRel | `.squad/agents/j-peterman/charter.md` | 🟢 Active |
| Steinbrenner | PM | `.squad/agents/steinbrenner/charter.md` | 🟢 Active |
| Elaine | UX Designer | `.squad/agents/elaine/charter.md` | 🟢 Active |
| Scribe | Session Logger | `.squad/agents/scribe/charter.md` | 🟢 Active |
| Ralph | Work Monitor | — | 🔄 Monitor |

## Coding Agent

<!-- copilot-auto-assign: true -->

| Name | Role | Charter | Status |
|------|------|---------|--------|
| @copilot | Coding Agent | — | 🤖 Coding Agent |

### Capabilities

**🟢 Good fit — auto-route when enabled:**
- Bug fixes with clear reproduction steps
- Test coverage (adding missing tests, fixing flaky tests)
- Lint/format fixes and code style cleanup
- Dependency updates and version bumps
- Small isolated features with clear specs and established implementation patterns
- Boilerplate/scaffolding generation
- Documentation fixes and README updates
- Narrow tooling or workflow fixes that stay within an existing pattern

**🟡 Needs review — route to @copilot but flag for squad member PR review:**
- Medium features with clear specs and acceptance criteria
- Refactoring with existing test coverage
- API endpoint additions following established patterns
- Migration scripts with well-defined schemas
- Changes that touch multiple files but remain low-ambiguity and pattern-following

**🔴 Not suitable — route to squad member instead:**
- Architecture decisions and system design
- Multi-system integration requiring coordination
- Ambiguous requirements needing clarification
- Security-critical changes (auth, encryption, access control)
- Performance-critical paths requiring benchmarking
- Changes requiring cross-team discussion
- Product philosophy or positioning changes
- DSL surface, parser, runtime semantics, or constraint-model changes
- Syntax grammar, language-server completion, or MCP contract changes
- Work that needs new design direction instead of following an established pattern

### Workflow

- Small explicit chore issues that clearly fit the coding-agent profile may be created directly with the `squad:copilot` label.
- Use direct `squad:copilot` routing only for bounded, low-ambiguity work with clear acceptance criteria and an existing pattern to follow.
- Keep philosophy, language-surface, runtime-semantics, MCP-contract, security-sensitive, and cross-cutting design work on the normal squad triage path.
- `@copilot` picks up those issues automatically, opens the PR, and keeps ownership through normal review feedback.
- When the PR is marked ready for review, the squad PR Review ceremony runs.
- Shane handles final approval and merge.

## Human Members

| Name | Role | Badge | Status | Notes |
|------|------|-------|--------|-------|
| Shane | Reviewer | 👤 Human | 🟢 Active | Final human review/sign-off when the team explicitly routes work for review. |

## Project Context

- **Project:** Precept — a domain integrity engine for .NET. A single declarative contract for modelling business entities, governing how their data evolves under business rules across a lifecycle. Makes invalid configurations structurally impossible.
- **Stack:** C# / .NET 10.0 (core runtime, language server, MCP server), TypeScript (VS Code extension), xUnit + FluentAssertions (tests), LSP (language server protocol)
- **Components:** Core DSL runtime (`src/Precept/`), Language Server (`tools/Precept.LanguageServer/`), MCP Server (`tools/Precept.Mcp/`), VS Code Extension (`tools/Precept.VsCode/`), Copilot Plugin (`tools/Precept.Plugin/`)
- **Design Structure:** `design/brand/` is the source of truth for brand identity and canonical meaning. `design/system/` is the source of truth for reusable product-facing visual-system guidance and surface specs. `design/prototypes/` holds durable design prototypes. `docs/` remains the source of truth for technical and explanatory documentation.
- **Philosophy:** `docs/philosophy.md` — grounding document for product identity, positioning, and conceptual hierarchy. All agents must read before design decisions, positioning work, or language proposals. Keep in sync with the implementation.
- **Distribution:** NuGet (core library), VS Code Marketplace (extension), Claude Marketplace (plugin)
- **Owner:** shane
- **Universe:** Seinfeld
- **Created:** 2026-04-04

## Ownership Boundaries

- **Peterman owns `design/brand/`** — brand identity, philosophy, voice, mark logic, typography intent, and canonical brand meaning.
- **Elaine owns `design/system/`** — semantic visual system work, reusable product-surface guidance, and canonical surface specs.
- **Elaine creates and maintains the two canonical design HTML artifacts** — `design/brand/brand-spec.html` and `design/system/foundations/semantic-visual-system.html` are Elaine-owned design executions and should be treated as a visually coordinated pair.
- **Peterman remains brand owner for the brand artifact** — he supplies brand meaning, research, semantic guidance, and review input to Elaine for `design/brand/brand-spec.html` and for any design-system choices that affect brand meaning.
- **Frank governs boundary decisions** — promotions between local surface rules, reusable system rules, and brand-level meaning.
- **`docs/` is not a design bucket** — it holds technical design docs, implementation plans, research, and explanatory material rather than canonical visual-system rules.
- **Shared review is required** when a change affects both brand meaning and reusable product-surface guidance.

## Review Policy

**All PR reviews follow a conversational lifecycle that mirrors how humans use GitHub.** See `.squad/skills/pr-review/SKILL.md` for the full spec.

### Lifecycle

1. **Initial review** — Reviewers post `REQUEST_CHANGES` with inline comments on specific files and lines.
2. **Dev fix** — Devs fix the code, push, and **reply** to each review thread explaining the fix.
3. **Re-review** — Reviewers verify by **replying** to the same threads, **resolve** satisfied threads, and submit `APPROVE`.

### Rules

- Reviews are posted as `squad-reviewer[bot]` via the Squad Reviewer GitHub App.
- Reviewers output **structured JSON** (not Markdown) with inline comments on specific files and lines.
- Every blocker MUST have an inline comment. Top-level-only blockers are incomplete reviews.
- Dev fix replies and reviewer verifications happen **in the same thread** — no new duplicate threads.
- Reviewers resolve threads only after verifying the fix in re-review.
- The decisions inbox is still used for team-relevant decisions — review findings belong on the PR.

### Script

```bash
# Initial review
node tools/scripts/squad-review.js <pr> <review.json>

# Dev fix reply (reply to existing threads)
node tools/scripts/squad-review.js <pr> <fix-reply.json>

# Re-review (reply + resolve + APPROVE)
node tools/scripts/squad-review.js <pr> <rereview.json>

# List threads (for mapping)
node tools/scripts/squad-review.js <pr> threads [--unresolved]
```

## Review Governance

- **Authors can fix their own rejected code.** They know it best — routing fixes to a different agent who isn't familiar with the code creates worse problems than it solves.
- **Authors cannot review their own code.** The review gate requires fresh eyes. A different agent (or Shane) must review every PR — the original author never marks their own work as approved.

## Issue Source

- **Provider:** GitHub
- **Repository:** `sfalik/Precept`
- **Connected:** 2026-04-05T19:00:10Z
- **Filters:** Open issues and open pull requests
