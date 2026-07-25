---
status: Active
purpose: First-session orientation for AI agents working on Precept — the five organizing concepts that ground every decision
audience: AI agents new to this codebase (also useful for new human contributors)
---

# Agent Onboarding

If you've never worked on Precept before — or you're a fresh session that doesn't remember a prior one — read this once. It is the conceptual on-ramp the rest of the corpus assumes you already have.

This doc is **not** a substitute for the always-required reads ([`philosophy.md`](philosophy.md), [`README.md`](README.md), [`language/README.md`](language/README.md)). It is the *orientation layer* — what those docs are organized around. After this, the rest of the corpus becomes navigable.

---

## What Precept is, in one paragraph

Precept is a **domain integrity engine** for .NET — a DSL runtime that governs how a business entity's data evolves under business rules across its lifecycle, making invalid configurations structurally impossible. Authors write `.precept` definitions; the compiler proves the definition's invariants hold; the runtime guarantees no Configuration can ever violate them. The intended primary author is the **domain expert**, not the developer — a business analyst who reasons in terms of "what is this data allowed to become." Read [philosophy.md](philosophy.md) for the full positioning.

---

## The five organizing concepts

Every doc in the corpus is structured around one or more of these. If you understand these five, the rest of the corpus has a place to land.

### Concept 1: Catalogs as the intended machine-readable form of the language spec

Precept is **metadata-driven**. Every language element — tokens, types, operators, modifiers, actions, constructs, expression forms, constraints, proof requirements, diagnostics, faults, semantic-token categories — is declared as a structured catalog entry. The goal is that the catalogs together become the language specification in machine-readable form rather than commentary about it.

**They are not that yet.** Measured 2026-07-25: the `CertificateSteps` catalog that `precept-language-spec.md:225` makes a condition of a proof strategy being admissible does not exist at all; `ProofRequirementKind` has thirteen members and neither establishment nor preservation is among them; and `precept-language-spec.md:1992` states in the spec's own voice that five of the places data can change have no catalog entry. So: the canonical docs win where they disagree with a catalog, never cite a catalog as evidence that something is complete, and never scope work by what the catalogs happen to declare. See `CLAUDE.md` § Catalog System, which records what ends this.

**Implication for agents:** When you add a new language feature, the catalog entry comes first. The parser, type checker, language server, MCP server, TextMate grammar generator, and docs all **derive** from catalog metadata. You never maintain parallel knowledge in pipeline code.

**The smell to recognize:** `kind switch { FooKind.Bar => …, FooKind.Baz => … }` where each arm exists "because the language says so" — that's per-member behavior leaking out of metadata into code. Switching on a DU **subtype** is correct; switching on a catalog member's enum identity to apply per-member behavior is the violation.

**Read next:** [`docs/language/catalog-system.md § Architectural Identity`](language/catalog-system.md) — the architectural keystone. Plus [`docs/contributing/catalog-driven-checklist.md`](contributing/catalog-driven-checklist.md) before writing any pipeline code.

### Concept 2: The pipeline → Compilation → Precept → Version chain

Source text flows through a six-stage compiler pipeline (Lexer → Parser → NameBinder → TypeChecker → GraphAnalyzer → ProofEngine), producing a **Compilation** artifact. The PreceptBuilder transforms the Compilation into a **Precept** (a runtime-ready execution model). The Evaluator operates Precepts to produce **Versions** (snapshots of an instance's data + state). Each stage boundary is a type contract — stages consume artifact types and produce artifact types, and those contracts are the architecture.

**Implication for agents:** When working on a pipeline change, locate the stage boundary you're touching and respect the artifact contract on both sides. `SemanticIndex` (TypeChecker's output) is a **flat semantic inventory**, not a structural mirror of the parse tree — downstream stages consume typed semantic data, not parser shape. Anti-mirroring is a non-negotiable rule.

**Read next:** [`docs/compiler-and-runtime-design.md`](compiler-and-runtime-design.md) — the architectural spine, full pipeline + runtime + tooling integration. Then drill into the relevant per-stage doc in [`docs/compiler/`](compiler/README.md) or per-component doc in [`docs/runtime/`](runtime/README.md).

### Concept 3: Lifecycle-driven design

Precept work flows through a seven-stage engineering lifecycle. Six of those stages are backed by a skill that exists in `.claude/skills/` — `/research`, `/design`, `/plan`, `/execute`, `/promote`, `/review`. The seventh stage's `/audit` skill has not been built yet. Research feeds Design; Design feeds Plan; Plan feeds Execute; Execute feeds Promote; Promote feeds Review. Each stage has explicit artifacts and discipline:

- **Stage 1 (Research)** — produces a doc in `research/` with comparable systems, prior art, citations
- **Stage 2 (Design)** — locks a design in `docs/Working/<topic>.md` with **four-leg rationale** (Rationale, Alternatives, Precedent, Tradeoff) per decision, **citation-with-excerpt** for every source, Philosophy Alignment + Language Design Grounding + Architecture Grounding sections
- **Stage 3 (Plan)** — produces a phased execution plan with exit criteria
- **Stage 4 (Execute)** — implements via vertical slices against a live plan artifact. On a branch destined for `main` that artifact is the draft pull-request body. On a `spike/*` branch no pull request is opened at all, commits land directly on the branch, and the plan document produced by `/plan` is the tracker. **The current branch is a spike branch**, so the plan document is where work is tracked; the `/execute` skill refuses to open a pull request here. See `CONTRIBUTING.md` § Spike Workflow.
- **Stage 5 (Promote)** — lifts content from `docs/Working/` to canonical docs; archives the design
- **Stage 6 (Review)** — end-of-lifecycle verification a work item passed all 5 earlier stages
- **Stage 7 (Maintain)** — periodic drift detection over canonical docs. The seventh stage's `/audit` skill has not been built yet, so do not route work to it. Until it exists, drift is caught by `/review` and by the doc-sync obligations in `CLAUDE.md`.

**Implication for agents:** Never propose a specific language-surface design in direct chat. Route to `/design`. Casual chat suggestions are brainstorming; they must not harden into "the" design without the four-leg rationale + grounding sections + citation discipline that the skill enforces. The risk the skill exists to prevent: the first thing written down becomes "the" answer.

**Read next:** [`CONTRIBUTING.md`](../CONTRIBUTING.md) for the workflow, and the relevant skill in [`.claude/skills/`](../.claude/skills/) when you start a stage.

### Concept 4: Pointer-philosophy and doc-sync

Canonical content is **pointed-to**, not duplicated. The catalog system enforces this in code; the doc corpus follows the same discipline. When two docs both describe the SemanticIndex, one of them is canonical and the other points to it. Drift between docs is a real failure mode and the corpus has had episodes of it ([the doc-corpus evaluation](Working/Archive/doc-corpus-evaluation-opus.md) catalogued them).

**Doc-sync rule:** code, interface, test, or behavior changes update docs **in the same pass**. Implementations and the docs that describe them ship together; aspirational claims in a `Implemented` doc are drift that must be fixed in the same edit. The CLAUDE.md § Documentation Sync routing table tells you which doc to update for which kind of change.

**Implication for agents:** Before writing prose that restates a concept, check whether the concept is already canonically described somewhere. If yes, link rather than restate. When changing code, scan the routing table and update the affected docs in the same commit.

**Read next:** [`CLAUDE.md` § Documentation Sync](../CLAUDE.md) for the routing table.

### Concept 5: Required reads vs context-on-demand

The session-level discipline:

- **Always-required reads** (every session): [`philosophy.md`](philosophy.md), [`README.md`](README.md), [`language/README.md`](language/README.md). About 200 lines combined — short enough that the cost-per-session is trivial.
- **Topic-gated reads** (when the work touches them): per-area READMEs ([`compiler/`](compiler/README.md), [`runtime/`](runtime/README.md), [`tooling/`](tooling/README.md)), the spec, catalog-system.md, per-stage docs, per-component docs.
- **Discovery-on-demand**: the [glossary](glossary.md) (grep when a term is ambiguous), [`docs/Working/`](Working/) (in-flight plans, bugs, designs), [`docs/Working/Archive/`](Working/Archive/) (historical decisions; reference only).

The READMEs in each sub-area are the **navigation layer** — they map their area's docs, give a reading order, and point to cross-cutting concerns. Use them before diving into individual docs.

**Implication for agents:** Don't try to read everything. Use the always-required reads to anchor; navigate via the sub-area READMEs to what your task actually touches. Context budget matters — every doc you read is context another task needs.

**Read next:** the relevant sub-area README when you have a task in hand.

---

## What an agent looks like working in Precept well

You read the three required docs. You identify the area(s) your task touches via the navigation table in `docs/README.md`. You read the sub-area README. You read the canonical doc for the specific concept. You write code or design that respects the catalog discipline (no parallel knowledge, no per-member kind switches, no aspirational claims). When you change code, you update the relevant docs in the same pass. You use the lifecycle skills for design and review work — never settle a language-surface decision inline.

When unsure: grep the glossary, then grep the canonical doc the glossary points to, then read.

---

## What an agent looks like working poorly

You don't read philosophy.md and propose features that violate "prevention not detection." You skip catalog-system.md and write a parser that hardcodes a token set the catalog already encodes. You discuss a new syntax in chat and let the suggestion harden into a design without going through `/design`. You change code without updating the doc that describes it. You read every doc in the corpus before starting any work and exhaust your context budget on background. You assume an `Implemented` status means the code matches without verifying.

All of these are recoverable. The discipline is "notice when you're doing one of them, and stop."

---

## Where to go next

- New to the codebase entirely → read [`philosophy.md`](philosophy.md), then [`docs/README.md`](README.md), then [`docs/language/README.md`](language/README.md), then come back here.
- Starting a specific task → identify the area in [`docs/README.md`](README.md)'s navigation table → open the sub-area README → drill from there.
- Confused about a term → grep [`docs/glossary.md`](glossary.md), then follow the canonical pointer.
- Doing design work → start with [`/design`](../.claude/skills/design/SKILL.md).
- Doing review work → start with the [`precept-reviewer`](../.claude/agents/precept-reviewer.md) agent.

The corpus is dense but navigable. Trust the README system; it routes you to depth on demand.
