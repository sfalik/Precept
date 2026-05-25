---
status: Active
purpose: The canonical template for sub-area README files in `docs/` — applied uniformly across language/, compiler/, runtime/, tooling/
---

# Sub-area README Template

Every sub-area README in `docs/<area>/README.md` follows this structure. The template is designed so an AI agent navigating to the area gets consistent routing — Documents → Reading Order → Cross-references — regardless of which area they land in.

The template is descriptive, not literal — sub-areas may add sections (e.g., a "Pipeline Order" section in `compiler/README.md`) but the four canonical sections below are required.

## Canonical structure

```markdown
# <area>/ — <Area Name>

> [!IMPORTANT] (optional — only if the area has non-negotiable rules)
> Brief callout. Key rules, with pointers to the canonical source.

Brief paragraph stating what this area covers and its boundary against
adjacent areas.

## Documents

| Document | Purpose | Status |
|----------|---------|--------|
| [doc-1.md](doc-1.md) | One-sentence purpose | Status from canonical taxonomy |
| [doc-2.md](doc-2.md) | One-sentence purpose | Status |

## Reading Order

For an agent new to the area, recommended sequence:

1. [doc-1.md](doc-1.md) — Why this first; what it grounds.
2. [doc-2.md](doc-2.md) — What it builds on.
3. ...

If the area splits naturally (e.g., per-stage docs + cross-cutting infrastructure),
provide separate reading orders for each split.

## Relationship to Other Docs

- `../<other-area>/<doc>.md` — How this area depends on or feeds into the other.
- `research/<topic>/<doc>` — Where research that grounds this area's decisions lives.
- `docs/Working/<plan-or-design>.md` — Active in-flight work touching this area.

## Cross-cutting concerns (optional)

If the area has cross-cutting topics that span multiple docs (e.g., diagnostic
system spans every compiler stage), list them here with pointers.
```

## Section purposes

### Documents table
The lookup surface. Each row: file path, one-sentence purpose, status. Status values come from the [canonical taxonomy](../README.md). An agent skimming this table should be able to identify which doc to open without reading the docs themselves.

### Reading Order
A numbered sequence that routes a fresh agent through the area in dependency order. Not every doc must appear in the reading order — some are pure reference and appear only in the Documents table. The Reading Order is the "onboarding path through this area."

### Relationship to Other Docs
Cross-references to adjacent areas, research, and active working documents. This is how an agent navigating one area discovers the next area their work touches.

### Cross-cutting concerns
For areas where a topic spans multiple docs (e.g., catalog discipline in `compiler/`, diagnostic-system in `compiler/`, qualifier propagation in `language/`), list the topic and the docs that touch it. Optional — only if the area genuinely has cross-cutting topics.

## Style conventions

- **Headings:** `# area/ — Area Name` (mirror the folder path), `## Documents`, `## Reading Order`, `## Relationship to Other Docs`, `## Cross-cutting concerns`.
- **Documents table column order:** Document, Purpose, Status. Always this order.
- **Status values:** must match the [canonical taxonomy](../README.md). Drift is a Phase-1-style truth-up violation.
- **Reading Order numbering:** uses `1.`, `2.`, ... not bullets. Order matters.
- **Cross-references:** relative paths from the README's location. No absolute paths.

## Why this template exists

Before this template, sub-area READMEs varied substantially. `language/README.md` had a five-step Reading Order; `tooling/README.md` was 19 lines with no Reading Order; `compiler/README.md` had pipeline order but no cross-cutting reading guidance. The variance was structural — an agent navigating couldn't predict what each README would provide. The template gives the corpus a uniform routing layer.

## Maintenance

When adding a new doc to a sub-area, update the area's README in the same pass. When a doc's status changes, update the README's table. When an area gains a new cross-cutting concern, add it to the README's Cross-cutting section. The README is part of the area, not separate from it.
