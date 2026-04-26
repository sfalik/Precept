---
name: "architecture"
description: "Domain knowledge skill for Precept system architecture. Covers where architecture design docs live, how to capture new decisions, how to use them in implementation, and how to keep them current."
domain: "architecture"
confidence: "high"
source: "earned — generalized from language-design audit; ensures architecture work is grounded in existing design docs, not assumptions"
---

## Context

This skill governs how agents work with the **architecture design corpus** — the design documents, implementation plans, and API specs that define how Precept's components are built and how they interact.

**Applies to:** Any agent doing architecture-related work — component design, API changes, cross-component integration, implementation planning, or evaluating structural proposals. Primary users: Frank (Lead/Architect), George (Runtime), Newman (MCP), Kramer (Tooling/LS), Elaine (preview surfaces).

## Research Location

### Core design documents

| Document | Path | Governs |
|----------|------|---------|
| Language design | `docs/PreceptLanguageDesign.md` | DSL syntax, semantics, type system — what EXISTS in the runtime |
| Runtime API | `docs/RuntimeApiDesign.md` | C# public API surface — Parse, Compile, Fire, Inspect, Update |
| MCP server | `docs/McpServerDesign.md` | 5-tool surface, tool philosophy, tiers, distribution split |
| Catalog infrastructure | `docs/CatalogInfrastructureDesign.md` | Token metadata registry, construct catalog, diagnostic catalog |
| Constraint violations | `docs/ConstraintViolationDesign.md` | Constraint violation reporting model |
| Editable fields | `docs/EditableFieldsDesign.md` | Edit declaration semantics and enforcement |
| Rules | `docs/RulesDesign.md` | Rule execution model |
| Syntax highlighting | `docs/SyntaxHighlightingDesign.md` | TextMate grammar design, token categorization |
| Preview/inspector | `docs/PreviewInspectorRedesignPrd.md` | Preview panel product requirements |
| Diagram layout | `docs/DiagramLayoutRedesign.md` | State diagram rendering |
| CLI | `docs/CliDesign.md` | CLI surface design |

### Implementation plans (archived — all complete)

| Plan | Path |
|------|------|
| Language features | `docs/archive/PreceptLanguageImplementationPlan.md` |
| MCP server | `docs/archive/McpServerImplementationPlan.md` |
| Syntax highlighting | `docs/archive/SyntaxHighlightingImplementationPlan.md` |
| Constraint violations | `docs/archive/ConstraintViolationImplementationPlan.md` |

### Cross-cutting

| Source | Path | When to read |
|--------|------|-------------|
| Product philosophy | `docs/philosophy.md` | Any change touching core guarantees |
| Artifact operating model | `tools/Precept.Plugin/README.md` | Changes to how artifacts are structured or distributed |
| Project history | `docs/HowWeGotHere.md` | Understanding prior architectural decisions |

## Using Research in Work

### Before any architecture decision

1. Read the **design document** for the component being changed
2. Check the **implementation plan** if one exists — know what's been sequenced (archived plans in `docs/archive/` for completed features)
3. Read `docs/philosophy.md` when the change touches core guarantees (prevention, determinism, inspectability)
4. **Cite specific design decisions** — section references, boundary rules, tier classifications

### Citation standard

| Acceptable | NOT acceptable |
|---|---|
| "Per McpServerDesign.md §Tool Philosophy: each tool owns exactly one concern, no overlap" | "Each tool has its own purpose" |
| "Per RuntimeApiDesign.md: Fire returns a result with NewState, AppliedActions, and Violations" | "Fire returns the new state" |
| "Per CatalogInfrastructureDesign.md: [TokenCategory] attributes drive semantic tokens automatically" | "Semantic tokens are automatic" |
| "Per McpServerDesign.md: >30 lines non-serialization code means logic belongs in src/Precept/" | "Keep tools simple" |

**Rule:** If a claim could be made by any architect without reading the design doc, it is not a citation.

## Capturing New Research

### Where to put it

| Type of finding | Location |
|-----------------|----------|
| New component design | `docs/{ComponentName}Design.md` |
| Implementation plan for new feature | `docs/{Feature}ImplementationPlan.md` (archive to `docs/archive/` when complete) |
| Architecture decision that affects existing design | Update the relevant design doc |
| Cross-component integration concern | Update both affected design docs |

### Design doc conventions

Design documents should include:
- **Status line** with dates of significant changes
- **Purpose** section — what problem this solves
- **Project location** — where the code lives
- Clear section structure that supports § references

### After creating or updating a design doc

1. Check if implementation plans need updating
2. Check if `CONTRIBUTING.md` workflow references are still accurate
3. If adding a new component, verify the project structure in `README.md`

## Maintaining Existing Research

- **When a feature ships:** Update the design doc to reflect what was actually built (design docs track what EXISTS, not what's planned)
- **When an implementation plan completes:** Note completion; don't delete the plan (it's history)
- **When APIs change:** Update `RuntimeApiDesign.md` and check MCP wrapper alignment
- **When the redesign history changes:** Add a status line entry with the date
