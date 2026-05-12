# MCP DTO-Free Design

**Owner:** Frank (Lead)
**Status:** Approved for implementation
**Last Updated:** 2026-05-12
**Scope:** `tools/Precept.Mcp/` catalog tools and compile contract simplification
**Supersedes:** `.squad/decisions/inbox/frank-mcp-dto-free-design.md`

---

## Decision

Shane approved **Approach 4 — Hybrid**: render catalog/reference MCP tools as markdown/text,
keep only minimal JSON where structure is genuinely programmatic, and proceed with
implementation.

This decision also locks the following points:

- There are **no known programmatic consumers** of the current catalog-tool JSON payloads.
- **Raw core-type serialization remains rejected.** This design is still a curated MCP contract,
  not a dump of runtime records.
- **No code generation** is introduced. No DTO generator, no source-generated contract layer,
  and no serialization attributes/converters pushed into the core runtime for MCP's sake.
- Implementation may proceed as working design, not proposal.

---

## Contents

1. Problem Statement
2. Architectural Constraints
3. Approaches Evaluated
4. Approved Working Design
5. Per-Tool Contract Breakdown
6. File / LOC Impact
7. Implementation Phases
8. Risks and Open Questions

---

## Problem Statement

The MCP server currently pays a heavy maintenance tax for a DTO layer that exists purely to shape
AI-facing output.

Current burden in `tools/Precept.Mcp/`:

- **63 DTO records** total
  - `LanguageToolDtos.cs` — 36 records / 369 LOC
  - `NewToolDtos.cs` — 14 records / 108 LOC
  - `CompileToolDtos.cs` — 13 records / 125 LOC
- `LanguageTool.cs` — **627 LOC** with a large projection/mapping surface
- `CompileTool.cs` — **387 LOC**, much of it DTO mapping
- `LanguageTool.cs` contains roughly **33 mapping helpers**; `CompileTool.cs` contains a second,
  smaller mapping stack

The sync problem is real: when core model types or catalog metadata change, MCP DTOs and mapping
code must be updated by hand. That drift tax is exactly the recurring "MCP sync" warning that now
shows up in implementation work.

The DTOs do solve real concerns:

| Concern | Why it exists today |
|---------|---------------------|
| Wire-contract shaping | Keep the MCP surface decoupled from internal record evolution |
| Projection/filtering | Expose only author/agent-relevant fields |
| Rendering | Turn enums/flags/runtime structures into legible output |
| Polymorphism | Flatten DU-style metadata into transport-safe shapes |
| Transport safety | Avoid hostile runtime shapes like nested internals and non-JSON-friendly values |

So the question is **not** "should MCP expose raw core types?" That was already rejected, correctly.
The actual question is: **how do we preserve a curated contract without maintaining a parallel DTO
forest?**

---

## Architectural Constraints

This design is bounded by non-negotiable constraints:

1. **Curated projection stays.** Precept's public MCP contract must remain author/AI legible and
   transport-safe. Raw catalog serialization is still architecturally wrong.
2. **AI legibility is a contract concern.** These tools exist primarily so agents can understand
   the language and runtime, not so external systems can deserialize elaborate object graphs.
3. **No code generation.** No T4, no `.g.cs`, no generated DTO layer.
4. **No MCP-specific serialization pollution in the core runtime.** The Precept core model should
   not absorb attributes/converters just to satisfy one transport layer.
5. **Minimal structure only where structure matters.** Diagnostics and runtime orchestration may
   require JSON. Reference material does not.

---

## Approaches Evaluated

| Approach | Sync Burden | Compile Safety | AI Legibility | Disposition |
|----------|-------------|----------------|---------------|-------------|
| 1. Attribute-driven core serialization | moved, not removed | weakens via converter/string writing | preserved | rejected |
| 2. `JsonNode` builder projection | mostly unchanged | stringly-typed | preserved | viable but limited |
| 3. Text / markdown output | eliminated for reference tools | N/A | improved for reference reading | recommended for catalogs |
| 4. Hybrid | eliminated where it matters; minimal remainder | small structured surface only | improved | approved |

## Approach 1: Attribute-Driven Serialization on Core Types

**Mechanism:** annotate core enums and records with `System.Text.Json` attributes and add custom
converters where needed.

### What it gets right

- Removes separate DTO record declarations.
- Can preserve enum-as-string and curated field names if converters are written carefully.

### Why it fails

- It **moves** the sync burden; it does not remove it.
- The core runtime becomes polluted with MCP transport concerns.
- Custom converters are stringly-typed and lose much of the compile-time safety DTOs at least
  provide through record constructors.
- The maintenance burden becomes more scattered and harder to reason about than the current DTO
  files.

### Disposition

**Rejected.** Wrong layer, wrong coupling, same sync problem in a different costume.

---

## Approach 2: `JsonNode` Builder Pattern

**Mechanism:** keep projection logic in MCP, but build `JsonObject` / `JsonArray` payloads directly
instead of instantiating DTO records.

### What it gets right

- Keeps MCP shaping concerns out of the core runtime.
- Deletes DTO type declarations.
- Works cleanly with AOT-friendly `System.Text.Json.Nodes` APIs.

### Why it falls short

- The projection logic still exists almost unchanged.
- Property names become raw strings.
- The sync burden remains effectively intact: core-shape changes still require touching the same
  projection sites.
- The only reliable savings is the DTO declaration files themselves.

### Disposition

**Viable but limited.** Better than polluting core types; still not enough benefit to justify the
same mapping burden.

---

## Approach 3: Text / Markdown Output

**Mechanism:** abandon structured JSON for AI-facing catalog/reference tools and emit well-formed
markdown or plain text directly.

### What it gets right

- Eliminates structural contract churn for reference tools.
- Produces output in the form the real consumer — the language model — actually reads best.
- Makes formatter code materially simpler than deep DTO projections.

### Limitation

- Pure text is excellent for reference/catalog tools, but diagnostics and runtime workflows still
  benefit from structured fields.

### Disposition

**Recommended for catalog/reference tools.** This is the right direction for language-reference
surfaces.

---

## Approach 4: Hybrid — Text for Catalogs, Minimal JSON for Compile/Runtime

**Mechanism:** use markdown/text for catalog/reference tools and reserve small JSON contracts only
for genuinely programmatic surfaces.

### Why this is the approved design

1. It removes the sync burden where it is currently worst: the catalog/reference tools.
2. It preserves structure where it genuinely matters: diagnostics and future runtime workflows.
3. It improves AI legibility instead of merely preserving it.
4. It keeps projection logic in MCP, where it belongs, without a giant DTO hierarchy.
5. It honors the earlier architectural ruling against raw catalog serialization.

### Disposition

**Approved.** This is the working design.

---

## Approved Working Design

The MCP surface is split by consumer need, not by historical implementation convenience.

### Contract rule

- **Reference/catalog tools** return **markdown/text**.
- **Compile diagnostics** return **minimal JSON** plus a text summary.
- **Future runtime orchestration tools** return **minimal JSON**.
- `precept_ping` stays trivial and is not a DTO driver either way.

### Why markdown is the correct contract for catalog tools

Catalog tools are read so an agent can understand the language:

- what `money` means
- which modifiers are valid
- how a proof obligation works
- what a diagnostic code means
- what domain atoms exist

That is reference reading, not structured data exchange. JSON was merely an indirect route to an
AI-readable projection. Markdown is the direct route.

### Output conventions for catalog formatters

The formatter layer should standardize on:

- `##` major sections, `###` subsections
- `**bold**` for named entities
- `` `code` `` for DSL syntax, tokens, and examples
- tables where comparison actually helps
- terminal-readable wrapping (target: under 120 columns)
- stable ordering derived from catalogs so repeated calls are predictable

### Compile contract rule

`precept_compile` does **not** keep the full `PreceptDefinitionDto` object graph.
It returns only:

- `hasErrors`
- `diagnostics[]`
- `summary` (text)

The diagnostics stay structured because navigation is programmatic. The definition summary becomes
text because agents read it to understand the precept, not to rebuild an AST.

A representative shape is:

```json
{
  "hasErrors": false,
  "diagnostics": [],
  "summary": "Precept: LoanApplication\nFields: 8\nStates: 4\nEvents: 5\nRules: 6\n..."
}
```

Diagnostic entries should stay intentionally small and stable:

- `code`
- `severity`
- `message`
- `line`
- `column`
- `length`

No source snippet is required; callers already have the submitted source.

---

## Per-Tool Contract Breakdown

## Current 10-tool surface

| Tool | Contract | Notes |
|------|----------|-------|
| `precept_ping` | trivial text/JSON-safe scalar | No DTO design pressure here |
| `precept_quickstart` | markdown/text | onboarding reference |
| `precept_syntax` | markdown/text | grammar/reference surface |
| `precept_types` | markdown/text | types, modifiers, built-ins |
| `precept_operations` | markdown/text | preserve category filter parameter |
| `precept_proofs` | markdown/text | proof requirements and runtime faults |
| `precept_patterns` | markdown/text | common patterns / anti-patterns |
| `precept_diagnostic` | markdown/text | keep diagnostic-code lookup parameter |
| `precept_domains` | markdown/text | currencies, UCUM, dimensions, temporal domains |
| `precept_compile` | minimal JSON + text summary | diagnostics stay structured |

### Internal aggregation surface

`LanguageTool.cs` is no longer the public contract center. The focused tools should format directly
from the catalogs they expose.

The old internal `Language()` aggregation method may be:

1. **deleted entirely** if no longer useful, or
2. **retained only for internal/debug use** after its DTO projection helpers are stripped away.

The approved design does **not** preserve a giant internal DTO projection stack merely because it
already exists.

### Future runtime tools

If runtime MCP tools such as `precept_inspect`, `precept_fire`, or `precept_update` are added, they
should follow the same principle from day one:

- use **minimal JSON** for state-machine workflow data
- avoid deep DTO trees
- keep rich narrative/explanatory content in text fields where appropriate

---

## File / LOC Impact

The expected reduction is substantial even before any deeper cleanup.

### DTO deletions

| File | Records | LOC | Expected action |
|------|---------|-----|-----------------|
| `tools/Precept.Mcp/Dtos/LanguageToolDtos.cs` | 36 | 369 | delete |
| `tools/Precept.Mcp/Dtos/NewToolDtos.cs` | 14 | 108 | delete |
| `tools/Precept.Mcp/Dtos/CompileToolDtos.cs` | 13 | 125 | delete |
| **Total** | **63** | **602** | **delete** |

### Mapping/projection reduction

| File | Current LOC | Expected result |
|------|-------------|-----------------|
| `tools/Precept.Mcp/Tools/LanguageTool.cs` | 627 | most or all DTO projection code removed; file may disappear entirely |
| `tools/Precept.Mcp/Tools/CompileTool.cs` | 387 | reduce to a small JSON diagnostic formatter + text summary formatter |

### Net effect

- **63 DTO records deleted**
- **602 LOC of DTO declarations deleted immediately**
- `CompileTool.cs` mapping path expected to drop from roughly **388 LOC to ~80 LOC**
- the 627-line `LanguageTool.cs` DTO projection layer is expected to collapse dramatically, and may
  be removed outright if no internal aggregation path remains necessary
- the current mapper forest (roughly **33 helpers** in `LanguageTool.cs` alone) disappears from the
  catalog-tool path

A new shared formatter layer is expected, but it is much smaller and flatter than the current DTO
projection stack.

---

## Implementation Phases

## Phase 1 — Catalog Tools to Markdown

Convert the eight reference/catalog tools to formatter-driven markdown/text output:

- `QuickstartTool.cs`
- `SyntaxTool.cs`
- `TypesTool.cs`
- `OperationsTool.cs`
- `ProofsTool.cs`
- `PatternsTool.cs`
- `DiagnosticTool.cs`
- `DomainsTool.cs`

### Expected structural changes

- add a shared formatter module (for example `tools/Precept.Mcp/Formatters/CatalogFormatters.cs`)
- delete `LanguageToolDtos.cs`
- delete `NewToolDtos.cs`
- remove catalog DTO mapping helpers from `LanguageTool.cs`
- update any MCP docs/examples that currently promise JSON for these tools

### Phase 1 success criteria

- catalog tools return readable, stable markdown
- no DTOs remain for the catalog/reference path
- output remains curated and catalog-derived

---

## Phase 2 — Compile Tool Simplification

Replace the full compile DTO graph with minimal JSON diagnostics plus a text summary.

### Expected structural changes

- simplify `CompileTool.cs`
- delete `CompileToolDtos.cs`
- keep only a tiny JSON formatter for diagnostics and location data
- move human-readable definition rendering into a summary formatter

### Phase 2 success criteria

- diagnostic navigation data remains intact
- compile success output is easier for agents to read
- `PreceptDefinitionDto` and child records are gone

---

## Phase 3 — LanguageTool Cleanup

After the public tools no longer depend on DTO projections, decide the fate of `LanguageTool.cs`.

### Decision rule

- if it serves no remaining internal value, delete it
- if retained, strip it down so it is no longer a second public-contract implementation hiding
  behind the focused tools

### Phase 3 success criteria

- no dead DTO projection path remains in the MCP project
- the surviving code clearly reflects the approved contract model

---

## Phase 4 — Future Runtime Tools

This phase is guidance, not immediate scope.

If runtime tools are introduced later, design them DTO-free from the start:

- minimal JSON for state, actions, and results
- text fields for explanatory summaries
- no deep mirror of internal runtime types

This prevents recreating the exact maintenance problem this design removes.

---

## Risks and Open Questions

These are implementation concerns, not decision gates.

### 1. MCP SDK string-return verification

**Risk:** tool discovery/serialization must be confirmed to handle direct `string` returns cleanly.

**Direction:** expected to work; verify during implementation before broad conversion lands.

### 2. Markdown contract consistency

**Risk:** independently written formatters drift in style and ordering.

**Direction:** centralize shared helpers and keep a single formatting convention for headings,
ordering, tables, and code rendering.

### 3. Agent regression from JSON to markdown

**Risk:** some callers may have implicitly learned the old JSON shape.

**Direction:** this is expected to be a net improvement for actual LLM use. Validate on the real
skill/agent workflows that consume these tools.

### 4. Diagnostic JSON scope creep

**Risk:** `precept_compile` slowly regrows a rich object graph.

**Direction:** hold the line. Diagnostics keep only navigation fields plus the message. The summary
remains text.

### 5. Programmatic consumers

**Status:** closed as a decision input for this design. **No known programmatic consumers** of the
catalog-tool JSON exist today.

If that assumption changes later, add an explicit compatibility plan rather than silently keeping a
DTO architecture nobody needs.

---

## Final Ruling

The DTO-heavy MCP design solved a real shaping problem, but it solved it with the wrong level of
ceremony for an AI-first tool surface. The approved architecture keeps the curated projection,
throws away the DTO forest, renders catalog/reference material as markdown, and reserves small JSON
contracts only for diagnostics and future runtime workflow data.

That is the durable working design.
