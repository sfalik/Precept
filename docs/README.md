# docs/ — Design Documentation

Design documents for the Precept compiler pipeline, language surface, runtime API, and tooling.

## Start here

**[`philosophy.md`](philosophy.md)** — Precept's core commitments. Read before any design decision. Every other doc in this tree is evaluated against it.

**[`language/README.md`](language/README.md)** — the language surface. Precept's design decisions are language decisions. Grammar, spec, canonical types, and the catalog system all live here. The canonical documents in this tree — not the catalogs, and not what the compiler currently accepts — are what completeness is measured against; see [`CLAUDE.md` § Catalog System](../CLAUDE.md) for the measured gaps and what closes them.

**First time in this codebase?** Read [`agent-onboarding.md`](agent-onboarding.md) once — the five organizing concepts (catalogs as the intended machine-readable form of the language spec, the pipeline → Compilation → Precept chain, lifecycle-driven design, pointer-philosophy + doc-sync, required reads vs context-on-demand). Short orientation; everything else assumes you have it.

## Structure

| Area | Entry point | What it covers |
|------|-------------|----------------|
| Philosophy | [`philosophy.md`](philosophy.md) | Core commitments — prevention, determinism, honesty about approximation, governance model |
| **Language surface** | **[`language/README.md`](language/README.md)** | **Grammar, formal spec, canonical type system, catalog system — the primary substance, and what completeness is measured against** |
| Architecture overview | [`compiler-and-runtime-design.md`](compiler-and-runtime-design.md) | Full pipeline + runtime surfaces; read before any pipeline or architecture work. §1 is where *what Precept guarantees and how it's delivered end-to-end* (the prove-or-reject walk from fault-prone site to committed guarantee) is told. |
| Compiler pipeline | [`compiler/README.md`](compiler/README.md) | Stage docs (lexer → parser → type checker → graph analyzer → proof engine), diagnostic system, literal system |
| Runtime API | [`runtime/README.md`](runtime/README.md) | Public API, result types, fault system, Evaluator, Precept Builder |
| Tooling | [`tooling/README.md`](tooling/README.md) | Language server, MCP server, VS Code extension |

## Doc status conventions

Every doc declares a status. The taxonomy:

**Reference docs** (in `docs/` outside Working/):
- **Implemented / Active** — describes shipped code; if it conflicts with code, that's drift — fix the doc in the same pass
- **Canonical design** — architectural reference; grounded in the implementation
- **Full** — complete reference doc; typically a stage doc following the 16-section template
- **Incremental** — grows over time as content lands (e.g., the spec adds sections per stage)
- **Design / Draft** — specification awaiting implementation
- **Partial / Partial stub** — code exists but operations are stubs or incomplete; design is locked
- **Stub** — placeholder; design not yet written
- **Roadmap** — forward-looking; primarily about planning rather than current state
- **Archived** — superseded; reference only, do not update

**Working docs** (in `docs/Working/`):
- **Draft** / **Draft <kind> — YYYY-MM-DD** — work in progress; not yet locked
- **Locked YYYY-MM-DD** — design frozen, awaiting implementation or promotion to canonical
- **Promoted to: `<link>`** — design canonicalized; doc retained as historical artifact in `docs/Working/Archive/`

Status fields may carry an inline annotation (e.g., `Design — public surface locked; entity internals pending`). The leading category must match the taxonomy above.

## Navigate by topic

Each area has a README that maps its documents, status, and reading order. Navigate to what the task actually touches — don't read everything, but use the relevant sub-README before diving into individual docs.

| When working on | Start here |
|-----------------|------------|
| Language surface (keyword, type, operator, modifier, construct, accessor) | [`language/README.md`](language/README.md) |
| Pipeline stage or compiler architecture | [`compiler/README.md`](compiler/README.md) — read `compiler-and-runtime-design.md` first |
| Runtime API or public contracts | [`runtime/README.md`](runtime/README.md) |
| Tooling (LS, MCP, VS Code extension) | [`tooling/README.md`](tooling/README.md) |
| Comparable systems, PLT theory, language precedent | [`research/language/README.md`](../research/language/README.md) |

## Discovery aids

- [`glossary.md`](glossary.md) — one-paragraph definitions for load-bearing terms (catalog, qualifier, descriptor, etc.). Grep here when a term is ambiguous.
- [`agent-onboarding.md`](agent-onboarding.md) — first-session orientation for fresh AI agents.

## In-flight and working docs

| Path | Purpose |
|------|---------|
| [`Working/bugs.md`](Working/bugs.md) | Active bug ledger — file new bugs here |
| [`Working/`](Working/) | In-flight design proposals and execution plans |
| [`Working/Archive/`](Working/Archive/) | Completed design work — reference only, do not update |
| [`archive/`](archive/) | Superseded specs — do not update |

## Relationship to other docs

- [`research/`](../research/) — evidence and precedent that grounds design decisions here; see `research/README.md` for the map
- [`CONTRIBUTING.md`](../CONTRIBUTING.md) — issue and PR workflow, lifecycle skills, doc-sync obligations
