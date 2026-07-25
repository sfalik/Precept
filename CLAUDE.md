# Claude Code Instructions for Precept

## Architecture

Precept is a domain integrity engine for .NET — a DSL runtime that governs how a business entity's data evolves under business rules across its lifecycle, making invalid configurations structurally impossible.

| Component | Path | Purpose |
|-----------|------|---------|
| Core runtime | `src/Precept/` | Lexer → parser → type checker → graph analyzer → proof engine → runtime evaluator |
| Language server | `tools/Precept.LanguageServer/` | LSP: diagnostics, completions, hover, go-to-definition, semantic tokens, preview |
| MCP server | `tools/Precept.Mcp/` | MCP tools wrapping core APIs |
| VS Code extension | `tools/Precept.VsCode/` | Extension host: syntax highlighting, preview webview, commands |
| Copilot plugin | `tools/Precept.Plugin/` | Shipped agent + skills + MCP launcher for consumers |
| Sample files | `samples/` | `.precept` files — illustrative examples, never a measure of coverage |

## How to write (Non-Negotiable)

Write plain engineering prose in every document, report and agent brief. Not simplified — plain. Expand an idea rather than compressing it into a term nobody outside this project would recognise.

Do not use: *source of truth (as a noun phrase), load-bearing, lens, surface (on its own), signal (meaning evidence), orthogonal, spine, harness, the ask, arm (of a switch or procedure)* — or any coined phrase that would need a glossary. Say the actual thing instead. Not "this claim is load-bearing" but "if this is wrong, everything after it is wrong." Not "the non-linear arm" but "the case that handles multiplying two variables."

**`denominator`** is correct in its arithmetic sense — the bottom of a fraction, the divisor unit in `quantity in 'kg/hour'`, the unit a price divides by. Never use it to mean the thing we measure against: write "we measure completeness against the canonical docs, and nothing else."

**`witness`** is being retired. What a proof produces to demonstrate a violation is a **counterexample**. Use that everywhere, including where the spec still says witness.

**This rule travels.** A writing rule that lives only in a skill binds the document being authored and nothing else. Copy it verbatim into every sub-agent and workflow prompt, or the vocabulary comes straight back in through them.

The owner reads everything this project produces and has said directly that reviewing agent-written documents is exhausting because of this vocabulary. His review speed is the bottleneck for the whole project.

## Documentation Map

Precept is documentation-dense. Many design decisions live in `docs/` and `research/`, not in code comments. **Read the relevant docs before making non-trivial changes** — assume the answer exists in a doc until you've verified it doesn't.

### Read first, every time

- **`docs/philosophy.md`** — Precept's core commitments. Every design and implementation decision is evaluated against these.
- **`docs/README.md`** — the doc landscape and navigation gateway. Know what exists before deciding what to read.
- **`docs/language/README.md`** — the language: spec, canonical types, grammar. Precept's design decisions are language decisions; this is the primary substance. **The docs here are what we measure against — not the catalogs, which are incomplete (see § Catalog System).**

**First time in this codebase?** Read `docs/agent-onboarding.md` once — the five organizing concepts (catalogs as the intended machine-readable form of the language spec, the pipeline → Compilation → Precept chain, lifecycle-driven design, pointer-philosophy + doc-sync, required reads vs context-on-demand). Note that the first of those is a goal, not the current state — see § Catalog System. When a term feels ambiguous, grep `docs/glossary.md`.

### Entry points

Each area has a README that catalogs its documents, status fields, and reading order. Treat these as the canonical maps before diving into individual files:

| Area | Entry point |
|---|---|
| Philosophy | `docs/philosophy.md` — core commitments; read before any design decision |
| Top-level | `docs/README.md` — doc landscape and navigation gateway |
| Architecture overview | `docs/compiler-and-runtime-design.md` — full pipeline + runtime; read before pipeline or architecture work |
| Language surface | `docs/language/README.md` — DSL spec and type system docs |
| Compiler pipeline | `docs/compiler/README.md` — pipeline stage docs and cross-cutting infrastructure |
| Runtime | `docs/runtime/README.md` — runtime API and component docs |
| Tooling | `docs/tooling/README.md` — language server, MCP server, VS Code extension |
| Contributing | `docs/contributing/` — pre-implementation checklists and reviewer protocols |
| In-flight proposals | `docs/Working/` — design proposals awaiting acceptance |
| Archive | `docs/archive/` — superseded specs; reference only, never update |

Pipeline stage docs follow a canonical 16-section template — see existing stage docs for the pattern when adding a new one.

### Doc status conventions

Every doc declares a status. Trust it for routing; verify against code when status is "Implemented":

- **Implemented / Active** — describes shipped code
- **Canonical design** — architectural reference; grounded in the implementation
- **Full** — complete reference doc; typically a stage doc following the 16-section template
- **Incremental** — grows over time as content lands (e.g., the spec adds sections per stage)
- **Design / Draft** — specification awaiting implementation
- **Partial / Partial stub** — code exists but operations are stubs or incomplete; design is locked
- **Stub** — placeholder; design not yet fully written
- **Roadmap** — forward-looking; primarily about planning rather than current state
- **Archived** — superseded; reference only, do not update

Working-doc statuses (in `docs/Working/`): `Draft`, `Draft <kind> — YYYY-MM-DD`, `Locked YYYY-MM-DD`, `Promoted to: <link>`. Status fields may carry an inline annotation; the leading category must match the taxonomy.

If a doc says "Implemented" but the code disagrees, that's drift — fix the doc in the same pass and note the drift to the user.

### Research

`research/` houses evidence and precedent that grounds the design decisions in `docs/`. Always check before fresh investigation — much of the comparator and feasibility work is already done. Subfolders: `language/`, `architecture/`, `philosophy/`, `product/`, `security/`, `archive/`.

Brand research lives in `design/brand/research/`; UX/design-system research in `design/system/research/`.

**Use the `/research` skill for new investigations** — it enforces folder discipline, citation requirements, and the promote-or-cite rule. See `research/README.md` for the canonical map.

### `.squad/` is live Squad state, not Precept product truth

`.squad/` is live Squad operational state: roster/routing, casting, decisions, agent histories, logs, and memory. It is **not** the source of truth for Precept product design, language semantics, or implementation behavior — those live in `docs/`, `README.md`, `CONTRIBUTING.md`, and this `CLAUDE.md`. Ignore `.squad/` when reasoning about Precept itself; read it when operating, auditing, or repairing Squad.

## Catalog System (Non-Negotiable)

Precept uses a metadata-driven architecture. The goal is that catalogs become the language specification in machine-readable form — domain knowledge declared once as structured metadata, with pipeline stages, tooling and consumers deriving from it rather than keeping parallel copies.

**They are not that yet, and must not be worked as if they were.** Measured 2026-07-25: the `CertificateSteps` catalog does not exist at all, though `precept-language-spec.md:225` makes emitting a certificate drawn from it a condition of a proof strategy being admissible. `ProofRequirementKind` declares thirteen members and neither establishment nor preservation is among them, though the induction model rests on both. Searching `src/Precept/Language/` for *premise*, *certificate*, *verdict* or *occasion* returns nothing. And `precept-language-spec.md:1992` states in canon's own voice that five of the places data can change have no catalog entry.

Until further notice:

- **The canonical docs win.** Where a catalog and a canonical doc disagree, the doc is right and the catalog has drifted. Fix the catalog.
- **Never cite a catalog as evidence that something is complete.** The completeness tests prove that every member a catalog *declares* carries metadata. They cannot detect a decision that was never written into the catalog at all.
- **Never scope work by what the catalog happens to declare.**
- **Catalog before code still stands.** It is the discipline that gets us there.

**What ends this suspension:** the catalogs become authoritative when adding a decision without teaching the catalog fails the build — every place data can change catalogued, the certificate vocabulary created, establishment and preservation declared, and the exhaustiveness analyzer applied across all of it. When that lands, delete this notice and restore the original sentence: *"Catalogs are the language specification in machine-readable form."*

The canonical catalog inventory lives in [`docs/language/catalog-system.md`](docs/language/catalog-system.md). Other docs reference catalogs by name, not by count — the enumeration is the source of truth.

This is the inverse of traditional compilers (Roslyn, GCC, TypeScript), where domain knowledge is scattered across pipeline stages and enums are internal classification axes. In Precept the catalog drives everything downstream: grammar, completions, hover, semantic tokens, MCP vocabulary, diagnostics.

**Rules:**

- **Catalog before code.** A new keyword, type, operator, modifier, or construct goes in the appropriate catalog entry first. Downstream artifacts derive from it.
- **Never maintain parallel keyword lists.** If a parser/LS/MCP consumer hardcodes what a catalog already knows, that's a violation. `Constructs.ByLeadingToken`, `DisambiguationEntry.DisambiguationTokens`, `Modifiers`, `Actions`, `Types`, `Operators` cover their respective surfaces — derive, don't duplicate.
- **Never hand-edit `tmLanguage.json`.** It's generated from `Tokens`, `Types`, and `Constructs` by the grammar generator.
- **Never switch on `*Kind` enum identity to dispatch per-member behavior.** The smell is `kind switch { FooKind.Bar => …, FooKind.Baz => … }` where each arm exists "because the language says so." That behavior belongs in catalog metadata. Switching on a DU **subtype** is correct (the subtype IS the metadata shape); switching on enum identity to apply per-member behavior is the violation.
- **Use discriminated unions for varying shapes.** Don't paper over shape differences with nullable fields on a flat record — use a DU base + sealed subtypes.

## The samples are examples, not a specification (Non-Negotiable)

`samples/` holds hand-written `.precept` files that demonstrate the language. They were written to illustrate, not to cover. They are not a measure of anything.

- **Never count sample files to establish coverage.** "64 of 78 files do X" tells you about the examples somebody happened to write. It tells you nothing about the language, and nothing about whether a rule is right. Measure against `docs/language/` and the spec.
- **Absence in the samples is not evidence.** No sample declaring `mincount` does not mean collections have no count constraints — it means nobody wrote that example. The riskiest parts of the language are precisely the ones with no sample, because nothing has ever exercised them.
- **Never compile a `.precept` file and treat the result as the specification.** The compiler is unfinished. What it accepts or rejects today tells you what got built, never what should be. This one feels like evidence, which is why it keeps happening.

The samples are good for one thing: showing whether a proposed change would break something a person actually wrote. That is real information about cost. It is never a statement about correctness or completeness.

## Product Philosophy (Non-Negotiable)

`docs/philosophy.md` is the grounding document for Precept's identity — what the product is, what it governs, how it's positioned, and why. Read it before making design decisions, writing public-facing copy, or proposing language changes.

### Core principles

The durable commitments — `docs/philosophy.md` is canonical:

- **Prevention, not detection.** Invalid configurations are structurally impossible — not caught after the fact, not bypassable.
- **One file, complete rules.** Every field, rule, ensure, and transition lives in the `.precept` definition. No scattered logic across validators, handlers, or service code.
- **Data and rules are primary; states are the mechanism.** Precept governs an entity's data integrity. Lifecycle is the coordinate system, not the point. **States are optional** — stateless precepts are first-class.
- **Governance, not validation.** Rules are declarations bound to the entity, structurally enforced on every operation. Not validators called at boundaries that can be bypassed.
- **Determinism.** Same definition + same data = same outcome. Nothing hidden.
- **Honesty about approximation.** Exact and approximate behavior must be visible in the type system and public surface. Precept does not present approximation as exactness.
- **Compile-time structural checking.** Unreachable states, dead-ends, type mismatches, contradictions, unsatisfiable guards, division-by-zero, overflow — all compile-time impossibilities.
- **Primary author is the domain expert**, not the developer. The language optimizes for someone who reasons in terms of "what is this data allowed to become."

### Rules

**Do not edit `docs/philosophy.md` without explicit owner approval.** Philosophy changes require deliberation — they are never auto-synced, never incidental updates, and never bundled into implementation PRs without discussion.

When any of the following change, **flag the potential philosophy gap to the user** — do not resolve it yourself:

- The category of entities Precept can govern (e.g. stateless precepts shipping)
- The core guarantee (prevention, determinism, inspectability)
- The positioning relative to adjacent tools
- The constraint model or operation surface

If the runtime can do something the philosophy doesn't describe, or the philosophy claims something the runtime can't do, **surface the gap and wait for direction.** The philosophy governs the product — the product does not silently rewrite the philosophy.

## Documentation Sync (Non-Negotiable)

**See also**: `CONTRIBUTING.md` § Doc Lifecycle — the 7-stage lifecycle and the lifecycle skills (`/research` through `/audit`) that automate doc-sync at each transition. The routing table below is consumed by `/design` (populates doc-update enumeration in design docs) and `/promote` (verifies obligations at promotion).

When making any code, interface, test, or behavior change, keep documentation in sync in the same edit pass. Unless explicitly told not to, include documentation synchronization as part of every relevant code change. Keep updates focused and factual; if uncertain whether a claim is implemented, verify from code/tests first.

### Where to update for which change

| Kind of change | Update |
|---|---|
| Pipeline stage behavior | `docs/compiler/<stage>.md` § Implementation State / § Open Questions |
| Runtime API | `docs/runtime/runtime-api.md` + affected `descriptor-types.md` / `result-types.md` / `fault-system.md` / `precept-builder.md` / `evaluator.md` |
| Language surface (new keyword/type/operator/modifier/construct) | Add catalog entry first; then update `docs/language/precept-language-spec.md` + relevant type doc (`primitive-types.md`, `temporal-type-system.md`, `business-domain-types.md`, `collection-types.md`) |
| Diagnostic added/changed | `docs/compiler/diagnostic-system.md` |
| MCP tool surface | `docs/tooling/mcp.md` + relevant DTO/formatter in `tools/Precept.Mcp/` |
| Language server feature | `docs/tooling/language-server.md` |
| Doc status changing (Stub → Design → Implemented) | Update the doc's own Status field AND any cross-referencing tables (e.g., `docs/compiler/README.md`) |
| README claim invalidated | `README.md` — never let aspirational claims sit as if implemented |

### Source of Truth

- `README.md` — public project narrative and usage guide; must track real implementation. Never leave aspirational claims as if implemented.
- `docs/` — canonical technical design decision records, architecture notes, project philosophy. Per-area READMEs are the canonical maps.
- `research/` — evidence and precedent; cite, don't duplicate. See `/research` skill.
- `design/brand/` — brand identity and brand-level semantic meaning.
- `design/system/` — reusable product-facing visual-system guidance and surface specs.
- `design/prototypes/` — durable design prototypes. Hot, code-near prototypes may live near their owning tool surface but should be promoted here when durable.
- `docs/archive/` holds superseded specs; reference only, never update.

### Transient vs Canonical references (Non-Negotiable)

**Code comments must reference only canonical, stable material.** Project-state references (the current task, workstream labels, phase numbers, finding IDs, design-doc Decision numbers, working-doc paths, audit dates) rot as the codebase evolves and belong in commit messages / PR descriptions, not in code that survives the project state.

**Canonical (OK to reference from code)**:
- `docs/language/*.md`, `docs/compiler/*.md`, `docs/runtime/*.md`, `docs/tooling/*.md` — canonical specs and per-stage docs
- `docs/philosophy.md` — locked philosophy
- `CLAUDE.md` — the project rules everything else depends on
- Catalog and source files (e.g. `Modifiers.cs`, `Tokens.cs`, `Operations.cs`)
- Spec line numbers when stable (e.g. `precept-language-spec.md:1662`)

**Transient (do NOT reference from code)**:
- `docs/Working/` — in-flight design proposals; promote-or-archive lifecycle means content moves
- `bugs.md` — tracking surface; entries move from Active to Fixed and eventually archive
- Design-doc internal structure (`Decision 2`, `D-3`, `Option A/B/C`)
- Workstream labels (`W-A`, `W-G`), phase labels (`Phase 4`), slice labels (`Slice 8`, `Slice 12`)
- Finding IDs (`F-LANG-COLL-06`, `F-LANG-BIZ-10`)
- Audit/review references (`precept-reviewer audit (2026-MM-DD)`), date stamps

`BUG-NNN` cites are a deliberate exception: the bugs.md convention pairs each inline cite with a planned removal (the workaround comes out when the bug closes). Add a new `BUG-NNN` cite only when there's a matching workaround in the code or sample.

**Rewriting rule**: if a comment had WHY content worth keeping mixed with transient refs, preserve the WHY in terms of the language/architecture; drop the project-task scaffolding.

- Bad: `// Slice 8 wires PRE0048 emission for action-applicability mismatches per F-LANG-COLL-08`
- Good: `// PRE0048 emission for action-applicability mismatches`
- Bad: `// TODO(W-G): widen return per COLL-02/03 design D-3 Option A (docs/Working/...).`
- Good: `// TODO: return TypedElementType? so accessor return-type propagation can flow qualifier metadata.`

This rule supplements the generic "don't reference the current task" comment-discipline rule with the project-specific list of which surfaces are stable vs transient.

### When research is involved

When locking a decision that started as research:
- Reference the research file from the consuming proposal/decision/spec.
- Update the issue map in `research/language/README.md` (or the relevant subfolder README) so the research connects forward.
- **Do not let research stand alone as policy.** Promote conclusions to a spec or decision; archive what didn't ship. (See `/research` skill — Promote-or-Cite rule.)

## DSL Authoring (Non-Negotiable)

Delegate `.precept` file work to the **`precept-author` sub-agent** (`.claude/agents/precept-author.md`). It carries the precept MCP tools and the canonical authoring/debugging workflows — spawn it rather than authoring DSL inline.

For inline snippets (a single line in an explanation, a short example in a comment), still: read at least one representative sample file from `samples/` first. Do not rely on memory or inference — read first, then write.

## Pre-Design Owner Consultation (Non-Negotiable)

A language-surface proposal must surface to the owner for **conversation** before any of: invoking `/research`, `/design`, or `/plan` on it; expanding a readiness-plan phase row from stub to populated workstreams; or spawning a sub-agent on language-surface work.

The gate scales with risk. The *purpose* is to bring the owner into the **what should we do** question — not to bottleneck on yes/no approval. Push-back, redirection, "let's sketch a different shape," "is this even the right problem," "let's defer" are all expected responses. The fast path through the gate is **alignment, not approval**.

### Tier 1 — No gate

Mechanical work without language-surface change: direct bug fixes against locked spec, refactors, doc sweeps, test-fixture restores, pipeline-internal changes, tooling/MCP/LS internals, finding-ID work where the canonical doc has no prior locked decision to read against. Proceed normally. No consultation required.

### Tier 2 — Conversation opener (new language surface, no spec conflict)

Any proposal touching language surface (keyword, type, operator, modifier, construct, expression form, syntax) where the canonical-doc area has no prior locked decision. Before delegating, surface the proposal in plain text:

- Name the finding/gap being addressed
- Cite the canonical-doc area checked (e.g., "I read `business-domain-types.md § quantity`; no prior decision on cross-precept unit visibility")
- Sketch the shape under consideration — framed as **opening a conversation**, not as a settled proposal: "I'm thinking X — does that match your intent? What else should I consider? Are there shapes I'm missing?"
- **Encourage exploration**: invite alternative shapes, scope tightening, "should this even be a Phase N item" pushback. The owner may sketch ideas that didn't occur to you; you may sketch ideas that didn't occur to them. The conversation might converge in one turn or run for several.

Then wait. No agent spawn, no design write, no readiness-plan expansion until there's alignment on the shape.

### Tier 3 — Heavier conversation (spec conflict)

Any proposal touching an area where the canonical doc has a locked prior decision — particularly `## Alternatives rejected` sections, locked Decision blocks, explicit "no X" statements, or `business-domain-types.md` / `precept-language-spec.md` decisions. Same as Tier 2, plus:

- **Quote the prior locked decision verbatim** with section reference
- Surface the conflict honestly: "the spec already answers this; here's how. Do we want to extend / override / close the finding as already-answered / something else?"
- Frame the override cost explicitly: a locked spec decision cannot be overridden inside a design pass. The owner authorizes the override (or doesn't); the design pass implements it.
- **Same conversational framing**: pushback, "let's revisit the rejection," "the spec is right, close the finding" are all expected responses.

Then wait. Other work rests on locked decisions; overriding one requires explicit owner direction.

### When the gate is satisfied

When proceeding past Tier 2 or Tier 3 into research/design/plan/agent-spawn, the assistant's response should make the consultation evidence visible — what was checked, what was found in the canonical area, what the owner authorized. This is post-hoc verifiable (a reviewer can grep the conversation for the consultation record).

### Why this exists

Broad delegation ("research, design, and plan Phase N") authorizes proceeding *after* the *what to do* question is settled with the owner. It does **not** authorize the assistant to settle that question unilaterally. A previous design pass conflated the two, introduced a `units { }` block construct that contradicted a locked rejection in `business-domain-types.md § D6`, and overrode the rejection inside the design pass with no owner consultation. This gate prevents that conflation by making the conversation a structural prerequisite, not an optional politeness.

### Honest exits

- **Mechanical work** (Tier 1) — proceed without consultation. The gate is for novel surface and spec conflicts, not execution of authorized work.
- **Owner has already authorized the specific shape** in this session — proceed; do not re-consult on details within the authorized scope.
- **Genuinely unsure whether a proposal touches language surface or not** — surface the ambiguity to the owner ("I'm not sure if X counts as language surface; how do you want me to treat it?"). When in doubt, surface.

## Language Surface Design (Non-Negotiable)

New language surface — syntax, keywords, types, operators, modifiers, constructs, expression forms — must go through `/design`. Never propose or settle on a specific syntax approach in direct chat.

If a user asks about syntax options, discuss tradeoffs briefly but **do not propose a specific design inline**. Route to the skill: "Let's run `/design` to work through this properly." A suggestion made in chat is brainstorming; it must not harden into a decision without the four-leg rationale, Language Design Grounding (broader field, not just Precept-internal), and Architecture Grounding the skill enforces.

The risk: a casual inline suggestion — made without reading the spec, comparable systems, or the catalog — can become "the" design simply by being the first thing written down. The `/design` skill exists precisely to prevent this.

## Per-Decision Rationale (Non-Negotiable)

Locked design decisions — in proposals, design docs, or research conclusions — must include all four:

- **Rationale** — why this choice, not just what it is
- **Alternatives considered and rejected** — with reasons for rejection
- **Precedent** — the evidence that grounds the decision
- **Tradeoff accepted** — the known downside being taken on

A decision that states WHAT without WHY is incomplete. Flag it before it advances.

For research that grounds decisions, use the `/research` skill — it codifies the methodology, folder discipline, and promote-or-cite rule. See `CONTRIBUTING.md` for the proposal lifecycle (research → issue → decision → spec).

## Build & Test

```bash
# Build everything
dotnet build

# Build language server only (default build task — Ctrl+Shift+B)
dotnet build tools/Precept.LanguageServer/Precept.LanguageServer.csproj --artifacts-path temp/dev-language-server

# Run all tests (xUnit + FluentAssertions, ~3600 tests across 4 projects)
dotnet test

# Run a single test project
dotnet test test/Precept.Tests/
dotnet test test/Precept.LanguageServer.Tests/
dotnet test test/Precept.Mcp.Tests/
dotnet test test/Precept.Analyzers.Tests/

# VS Code extension (from tools/Precept.VsCode/)
npm run compile        # Build TypeScript
npm run watch          # Watch mode
npm run loop:local     # Package + install locally (also a VS Code task)
```

**VS Code tasks** (Run Task menu): `build`, `extension: install`, `extension: uninstall`, `plugin: sync payload`.

## Development Workflow

- **Runtime / language server changes** → edit `src/Precept/` or `tools/Precept.LanguageServer/` → run Build task → extension auto-detects new build, no reload needed.
- **Extension UI / grammar / TypeScript** → edit `tools/Precept.VsCode/` → run `extension: install` task → reload window.
- **MCP server** → edit `tools/Precept.Mcp/` → reload window → rebuild happens lazily on next tool invocation from source.
- **Claude Code agents / skills** → edit `.claude/agents/` and `.claude/skills/` → changes appear on next session or sub-agent spawn.
- **Shipped Copilot plugin (agents/skills markdown)** → edit workspace-native copies in `.github/agents/` and `.github/skills/` → reload window → changes appear immediately. Run `plugin: sync payload` only when updating the shipped plugin payload under `tools/Precept.Plugin/` for explicit validation. *(The shipped plugin targets Copilot consumers of Precept — it's a product artifact, not your dev tooling.)*

Four distinct MCP config surfaces exist and must stay distinct:

- **`.vscode/mcp.json`** — VS Code/workspace-local source-first config. Uses the VS Code `servers` schema. Points `servers.precept` at `tools/scripts/start-precept-mcp.js`.
- **`.mcp.json` (repo root)** — repo-local config shared by Claude Code and Copilot CLI. Uses the `mcpServers` schema. Points `mcpServers.precept` at the same `tools/scripts/start-precept-mcp.js`. Claude Code enables it via `.claude/settings.local.json` (`enabledMcpjsonServers: ["precept"]`); Copilot CLI uses it natively.
- **`.claude/settings.local.json`** — Claude Code's per-user, per-project settings (enabled MCP servers, permissions). Not the MCP server definition itself — that lives in `.mcp.json`.
- **`tools/Precept.Plugin/.mcp.json`** — shipped/distribution payload. Uses `mcpServers` with `dotnet tool run precept-mcp`. Do not use this surface for local development.

## Use the MCP Tools First

This project ships a Precept MCP server. **Use its tools as your primary research surface** before reading source code or making assumptions about the DSL. Call `precept_ping` to confirm connectivity, then discover what's available from your session's tool list. Fall back to source code only for implementation details the tools don't cover.

## MCP Tool Sync

The MCP server tools in `tools/Precept.Mcp/Tools/` are **thin wrappers** around core APIs. If a tool method exceeds ~30 lines of non-serialization code, the logic belongs in `src/Precept/`.

- Core model / compile-result changes → check `tools/Precept.Mcp/Dtos/CompileToolDtos.cs` and any affected tool projections
- Catalog / diagnostic / domain metadata changes → verify `tools/Precept.Mcp/CatalogFormatters.cs` and `docs/tooling/mcp.md` still match

## Issue Implementation Workflow

Read `CONTRIBUTING.md` for the full workflow. The rules that matter most:

- Open the draft PR immediately; it's the execution hub. Body structure: `## Summary`, `## Linked Issue` (`Closes #N`), `## Why`, `## Implementation Plan`.
- **Design review gate.** `## Implementation Plan` stays "Pending design review" until the review ceremony completes with owner sign-off. Track B (introducing a new canonical design doc) also requires all inline review comments on the design doc resolved. See `CONTRIBUTING.md` § 3 for full Track A / Track B details.
- Work in vertical slices — commit, push, and update the PR-body summary/checklist after each.
- The PR body **is** the implementation plan. Never create a separate implementation-plan markdown file.

## DSL Sample Files (.precept)

**Read § The samples are examples, not a specification before using anything in `samples/` as evidence.**

`.precept` files are interpreted by the runtime — **not** compiled by the C# build pipeline. Never run `dotnet build` or `dotnet run` to validate a `.precept` file.

To check a `.precept` file for errors:
1. Use the `precept_compile` MCP tool on the `.precept` file contents — it returns diagnostics from the full compiler pipeline.
2. Cross-check against sample files in `samples/`.

## Test Conventions

- Framework: **xUnit** with **FluentAssertions**
- Naming: `PascalCase` + `Tests` suffix (e.g. `PreceptParserTests.cs`, `PreceptRuntimeTests.cs`)
- Tests typically use `[Fact]` and `[Theory]` attributes
