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

---

## Implementation Plan

This section turns the approved design into the concrete Newman execution plan. Treat the 8 focused
catalog tools plus `precept_compile` as the public contract surface. `LanguageTool.cs` is no longer
the architectural center; it is either a temporary migration aid or a dead file.

### Phase 1 — Catalog Tools (`LanguageTool.cs` exit plan)

#### Mechanical rule for all 8 tools

Change the public signatures first, then rewire the bodies:

- `QuickstartTool.Quickstart()` — `QuickstartDto` -> `string`
- `SyntaxTool.Syntax()` — `SyntaxDto` -> `string`
- `TypesTool.Types()` — `TypesDto` -> `string`
- `OperationsTool.Operations(string? category = null)` — `OperationsResultDto` -> `string`
- `ProofsTool.Proofs()` — `ProofsDto` -> `string`
- `PatternsTool.Patterns()` — `PatternsDto` -> `string`
- `DiagnosticTool.Diagnostic(string code)` — `DiagnosticLookupResultDto` -> `string`
- `DomainsTool.Domains()` — `DomainsDto` -> `string`

Do **not** keep routing these tools through `LanguageTool.Language()`. That preserves the DTO forest
behind a markdown facade and defeats the entire point of the redesign. Each focused tool should read
its own catalog inputs directly and hand them to a formatter.

#### `CatalogFormatters.cs` design

Add a new formatter module at:

- `tools/Precept.Mcp/Formatters/CatalogFormatters.cs`

Use one internal static class with a consistent signature pattern: catalog data in, markdown out.
Keep the tools thin and keep all rendering rules in one place.

```csharp
namespace Precept.Mcp.Formatters;

internal static class CatalogFormatters
{
    public static string FormatQuickstart();

    public static string FormatSyntax(
        IReadOnlyList<ConstructMeta> constructs,
        IReadOnlyList<ActionMeta> actions,
        IReadOnlyList<OutcomeMeta> outcomes,
        IReadOnlyList<OperatorMeta> operators);

    public static string FormatTypes(
        IReadOnlyList<TypeMeta> types,
        IReadOnlyList<ModifierMeta> modifiers,
        IReadOnlyList<FunctionMeta> functions);

    public static string FormatOperations(
        IReadOnlyList<OperationMeta> operations,
        string? category = null);

    public static string FormatProofs(
        IReadOnlyList<ProofRequirementMeta> proofRequirements,
        IReadOnlyList<FaultMeta> runtimeFaults);

    public static string FormatPatterns();
    public static string FormatDiagnostic(DiagnosticMeta diagnostic);
    public static string FormatDiagnosticNotFound(string code);

    public static string FormatDomains(
        IReadOnlyCollection<CurrencyEntry> currencies,
        IReadOnlyList<UcumAtom> units,
        IReadOnlyCollection<UcumPrefix> prefixes,
        IReadOnlyCollection<DimensionCatalog.DimensionAlias> dimensions,
        IReadOnlyList<TemporalUnits.TemporalUnitEntry> temporalUnits);
}
```

Implementation guidance:

- Build strings with `StringBuilder`.
- Standardize headings: `# Tool Title`, then `##` major sections, then `###` per-entry sections.
- Prefer bullet lists for per-entry metadata and tables only for dense comparison sets.
- Add small shared helpers inside the same file for repeated rendering:
  - `AppendSection(StringBuilder sb, string heading)`
  - `RenderQualifierShape(QualifierShape? shape)`
  - `RenderProofRequirements(IReadOnlyList<ProofRequirement> requirements)`
  - `RenderDimensionVector(DimensionVector vector)`
  - `RenderScale(UcumExactFactor factor)`
  - `JoinOrNone(IEnumerable<string> values)`
- Keep catalog ordering stable: use the native declaration order for `*.All`, and only sort where the
  current code already sorts (`CurrencyCatalog.All.Values.OrderBy(...)`, `DimensionCatalog.All.Values.OrderBy(...)`,
  `UcumPrefixCatalog.All.Values.OrderBy(p => p.Order)`).

#### `precept_quickstart`

- **Current -> new:** `QuickstartDto` -> `string`
- **Tool file:** `tools/Precept.Mcp/Tools/QuickstartTool.cs`
- **Read from:** `QuickstartCatalog.WhatIsPrecept`, `QuickstartCatalog.CoreGuarantee`,
  `QuickstartCatalog.CoreConcepts`, `QuickstartCatalog.ToolGuide`, `QuickstartCatalog.MinimalExamples`
- **Formatter output:**
  - `# Precept Quickstart`
  - `## What Precept Is`
  - `## Core Guarantee`
  - `## Core Concepts` with one `###` subsection per concept containing name, summary, example
  - `## Tool Guide` with one bullet per tool: when to call it, what it returns
  - `## Minimal Examples` with one `###` subsection per example and a fenced `precept` code block
- **DTO deletions:** `QuickstartDto`, `CoreConceptDto`, `ToolGuideDto`, `MinimalExampleDto`
  from `tools/Precept.Mcp/Dtos/NewToolDtos.cs`

#### `precept_syntax`

- **Current -> new:** `SyntaxDto` -> `string`
- **Tool file:** `tools/Precept.Mcp/Tools/SyntaxTool.cs`
- **Read from:** `Constructs.All`, `Actions.All`, `Outcomes.All`, `Operators.All`, `SyntaxReference`
- **Formatter output:**
  - `# Precept Syntax Reference`
  - `## Grammar Rules` using `SyntaxReference.GrammarModel`, `CommentSyntax`, `IdentifierRules`,
    `StringLiteralRules`, `NumberLiteralRules`, `WhitespaceRules`, `NullNarrowing`,
    `TypedConstantRules`, `ExpressionRules`
  - `## Operator Precedence` using `SyntaxReference.PrecedenceTable`
  - `## Conventional Order` using `SyntaxReference.ConventionalOrder`
  - `## Constructs` with one `###` subsection per construct: name, kind, description, usage example,
    primary leading token, allowed scopes, slot list, disambiguation entries, routing family,
    modifier domain, snippet template when present
  - `## Actions` with one `###` subsection per action: keyword, description, applicable targets,
    allowed constructs, syntax shape, value requirement, into support, primary action kind,
    proof requirements, hover/usage/snippet metadata
  - `## Outcomes` with one `###` subsection per outcome: leading token, argument kind,
    description, example
  - `## Operators` with one `###` subsection per operator: rendered tokens, arity,
    associativity, precedence, family, keyword flag, description, usage example
- **DTO deletions:** `SyntaxDto` from `NewToolDtos.cs`; `ConstructCatalogEntryDto`,
  `ConstructSlotDto`, `DisambiguationEntryDto`, `ActionCatalogEntryDto`, `OutcomeDto`,
  `OperatorCatalogEntryDto`, `SyntaxReferenceDto`, `CommonPatternDto`, `AntiPatternDto`
  from `tools/Precept.Mcp/Dtos/LanguageToolDtos.cs`

#### `precept_types`

- **Current -> new:** `TypesDto` -> `string`
- **Tool file:** `tools/Precept.Mcp/Tools/TypesTool.cs`
- **Read from:** `Types.All`, `Modifiers.All`, `Functions.All`
- **Formatter output:**
  - `# Precept Type System`
  - `## Types` with one `###` subsection per type: keyword, kind, display name, category,
    description, traits, widening targets, implied modifiers, implied qualifiers, qualifier shape,
    accessors, choice literal tokens, authoring metadata (`HoverDescription`, `UsageExample`),
    `NotemptyApplicable`, content-validation details when present
  - `## Modifiers` split into fixed subsections:
    - `### Value Modifiers`
    - `### State Modifiers`
    - `### Event Modifiers`
    - `### Access Modifiers`
    - `### Anchor Modifiers`
    Each entry should include keyword, kind, description, category, applicability, mutually
    exclusive set, desugaring/proof metadata, and authoring metadata where present.
  - `## Built-in Functions` with one `###` subsection per function: name, category, description,
    overload list, qualifier match, proof requirements, CI variant info, usage/snippet/hover,
    message-position flag
- **DTO deletions:** `TypesDto` from `NewToolDtos.cs`; `TypeCatalogEntryDto`, `QualifierShapeDto`,
  `QualifierSlotDto`, `TypeAccessorDto`, `ContentValidationDto`, `ModifierCatalogDto`,
  `ModifierTargetDto`, `ValueModifierCatalogEntryDto`, `StateModifierCatalogEntryDto`,
  `EventModifierCatalogEntryDto`, `AccessModifierCatalogEntryDto`, `AnchorModifierCatalogEntryDto`,
  `FunctionCatalogEntryDto`, `FunctionOverloadDto`, `FunctionParameterDto`
  from `LanguageToolDtos.cs`

#### `precept_operations`

- **Current -> new:** `OperationsResultDto` -> `string`
- **Tool file:** `tools/Precept.Mcp/Tools/OperationsTool.cs`
- **Read from:** `Operations.All`
- **Formatter output:**
  - `# Precept Operations`
  - If a filter was supplied, print `Filtered by: \`Money\`` (or whatever was passed)
  - `## Available Categories` listing distinct LHS type names in the current order used by the tool
  - `## Matching Operations` with one `###` subsection per operation: kind, rendered signature
    (`lhs operator rhs -> result`), description, qualifier match, proof requirements,
    CI-variant diagnostic if present, bidirectional lookup flag
  - `## Count` as a final single-line section so tests can assert the filtered total cleanly
- **DTO deletions:** `OperationsResultDto` from `NewToolDtos.cs`; `OperationDto`
  from `LanguageToolDtos.cs`

#### `precept_proofs`

- **Current -> new:** `ProofsDto` -> `string`
- **Tool file:** `tools/Precept.Mcp/Tools/ProofsTool.cs`
- **Read from:** `ProofRequirements.All`, `Faults.All`
- **Formatter output:**
  - `# Precept Proofs and Runtime Faults`
  - `## Proof Requirements` with one `###` subsection per requirement: kind, description,
    whether it is dual-subject, and any short explanatory line needed for authoring
  - `## Runtime Faults` with one `###` subsection per fault: code, severity, message template,
    recovery hint
- **DTO deletions:** `ProofsDto`, `ProofRequirementMetaDto`, `FaultMetaDto`
  from `NewToolDtos.cs`

#### `precept_patterns`

- **Current -> new:** `PatternsDto` -> `string`
- **Tool file:** `tools/Precept.Mcp/Tools/PatternsTool.cs`
- **Read from:** `SyntaxReference.CommonPatterns`, `SyntaxReference.AntiPatterns`
- **Formatter output:**
  - `# Precept Patterns`
  - `## Common Patterns` with one `###` subsection per pattern: name, description,
    fenced `precept` snippet
  - `## Anti-Patterns` with one `###` subsection per anti-pattern: name, description,
    `Bad` snippet, `Good` snippet, `Why it fails`
- **DTO deletions:** `PatternsDto` from `NewToolDtos.cs`; `CommonPatternDto`, `AntiPatternDto`
  from `LanguageToolDtos.cs`

#### `precept_diagnostic`

- **Current -> new:** `DiagnosticLookupResultDto` -> `string`
- **Tool file:** `tools/Precept.Mcp/Tools/DiagnosticTool.cs`
- **Read from:** `Diagnostics.All`; `Diagnostics.GetMeta((DiagnosticCode)numericValue)` for PRE lookups
- **Tool logic to keep:** name lookup, PRE#### lookup, and not-found branching stay in
  `DiagnosticTool.cs`; only rendering moves into `CatalogFormatters`
- **Formatter output when found:**
  - `# Diagnostic UndeclaredField (PRE0017)` style heading
  - `## Summary` with stage, severity, category, message template
  - `## Trigger`
  - `## Recovery Steps`
  - `## Fix Hint`
  - `## Related Codes`
  - `## Prevents Fault`
  - `## Examples` with before/after blocks when present
- **Formatter output when missing:** a short markdown/plain-text block that echoes the requested code,
  states the lookup failed, and reminds the caller of the two accepted formats
- **DTO deletions:** `DiagnosticLookupResultDto` from `NewToolDtos.cs`; `DiagnosticCatalogEntryDto`
  from `LanguageToolDtos.cs`

#### `precept_domains`

- **Current -> new:** `DomainsDto` -> `string`
- **Tool file:** `tools/Precept.Mcp/Tools/DomainsTool.cs`
- **Read from:**
  - `CurrencyCatalog.All.Values.OrderBy(entry => entry.AlphaCode)`
  - `UcumAtomCatalog.BrowseTier1()`
  - `UcumPrefixCatalog.All.Values.OrderBy(p => p.Order)`
  - `DimensionCatalog.All.Values.OrderBy(entry => entry.Name)`
  - `TemporalUnits.AllEntries`
- **Formatter output:**
  - `# Precept Domain Catalog`
  - `## Currencies` table or subsections with alpha code, numeric code, name, minor unit, symbol
  - `## UCUM Tier-1 Units` with code, name, dimension vector, resolved dimension name,
    exact scale, prefixable flag, annotation class
  - `## UCUM Prefixes` with code, name, numerator, denominator, base-10 exponent
  - `## Dimensions` with name, dimension vector, description
  - `## Temporal Units` with singular, plural, calendar-based flag, period flag, duration flag
- **DTO deletions:** `DomainsDto`, `UcumPrefixDto` from `NewToolDtos.cs`; `DomainCatalogDto`,
  `CurrencyDomainEntryDto`, `UcumTier1UnitDto`, `DimensionDomainEntryDto`,
  `TemporalUnitDomainEntryDto`, `DimensionVectorDto`, `UcumExactFactorDto`
  from `LanguageToolDtos.cs`

#### Fate of `LanguageTool.cs`

- Do not add new formatter code to `tools/Precept.Mcp/Tools/LanguageTool.cs`.
- During migration Newman may leave it compiling temporarily while focused tools are rewired.
- Once the focused tools no longer call `LanguageTool.Language()`, delete the file rather than
  preserving an internal DTO serializer that nobody should use.
- Do **not** carry forward `Constraints`, `FirePipeline`, or the giant aggregate `LanguageReferenceDto`
  surface just to keep old tests alive. That was the old architecture.

### Phase 2 — Compile Tool (`tools/Precept.Mcp/Tools/CompileTool.cs`)

#### Surviving DTO records

Keep only the minimal diagnostic JSON contract. Reduce
`tools/Precept.Mcp/Dtos/CompileToolDtos.cs` to exactly this shape:

```csharp
namespace Precept.Mcp.Dtos;

public sealed record CompileResultDto(
    bool HasErrors,
    DiagnosticDto[] Diagnostics,
    string? ProjectDefinition);

public sealed record DiagnosticDto(
    string Code,
    string Severity,
    string Message,
    DiagnosticLocationDto Location);

public sealed record DiagnosticLocationDto(
    int Line,
    int Column,
    int Length);
```

With ASP.NET/System.Text.Json web defaults, this serializes as:

```json
{
  "hasErrors": false,
  "diagnostics": [],
  "projectDefinition": "# Compiled Precept\n..."
}
```

#### DTO records to delete from `CompileToolDtos.cs`

Delete the entire definition graph:

- `PreceptDefinitionDto`
- `PreceptFieldDto`
- `PreceptStateDto`
- `PreceptEventDto`
- `EventArgDto`
- `TransitionRowDto`
- `PreceptRuleDto`
- `EnsureDto`
- `AccessModeDto`
- `StateHookDto`

Also remove `using System.Text.Json.Serialization;` because the `JsonPropertyName` attributes go away
with the definition graph.

#### `ProjectDefinition` structure

Replace `MapDefinition(...)` with a text renderer, not another object model. Add a private helper in
`CompileTool.cs` (or extract later if it grows too large):

```csharp
private static string BuildProjectDefinition(string source, Compilation compilation)
```

Structure the rendered text in a fixed order so agents and tests can rely on it:

1. `# Compiled Precept`
2. `## Overview`
   - Name
   - Stateful/stateless
   - Field count
   - State count
   - Event count
   - Rule count
   - State hook count
3. `## Fields`
   - one `###` subsection per field with type, optionality, writability, modifiers, default,
     computed expression, qualifier, choice metadata when present
4. `## States`
   - one `###` subsection per state with modifiers, constraints, omitted fields, access modes
5. `## Events`
   - one `###` subsection per event with args, rows, ensures
   - each row should include from-state(s), guard, actions, outcome, target state,
     reject message when applicable
6. `## Rules`
   - one bullet or `###` subsection per rule with expression, because, when
7. `## State Hooks`
   - one `###` subsection per hook with state name, kind (`entry`/`exit`), actions

Render empty sections explicitly as `None.` rather than omitting them. The point is stable readable
output, not compactness.

#### `CompileTool.cs` method-level rewrite

Keep the outer compile flow; remove the DTO projection tree.

- Keep:
  - `Compile(string text)`
  - `MapDiagnostic(Diagnostic diagnostic)`
  - `FormatDiagnosticCode(...)`
  - `FormatSeverity(...)`
  - low-level render helpers that still serve text rendering (`RenderTypeName`, `RenderQualifier`,
    `RenderExpression`, `RenderSpan`, etc.)
- Delete or replace:
  - `MapDefinition`
  - `MapField`
  - `MapState`
  - `MapEvent`
  - `MapArg`
  - `MapTransitionRow`
  - `MapEventHandlerRow`
  - `MapRule`
  - `MapEnsure`
  - `MapAccessMode`
  - `MapStateHook`
- Add focused summary helpers so the file reads as a formatter, not a DTO mapper:

```csharp
private static string BuildProjectDefinition(string source, Compilation compilation);
private static void AppendFields(StringBuilder sb, SemanticIndex semantics, string source);
private static void AppendStates(
    StringBuilder sb,
    SemanticIndex semantics,
    IReadOnlyDictionary<string, string[]> omittedFieldsByState,
    string source);
private static void AppendEvents(StringBuilder sb, SemanticIndex semantics, string source);
private static void AppendRules(StringBuilder sb, SemanticIndex semantics, string source);
private static void AppendStateHooks(StringBuilder sb, SemanticIndex semantics, string source);
```

#### End-to-end `precept_compile` contract

The tool should now return only three top-level fields:

```json
{
  "hasErrors": true,
  "diagnostics": [
    {
      "code": "PRE0017",
      "severity": "Error",
      "message": "Field 'Amount' is not declared.",
      "location": {
        "line": 2,
        "column": 7,
        "length": 6
      }
    }
  ],
  "projectDefinition": null
}
```

and on success:

```json
{
  "hasErrors": false,
  "diagnostics": [],
  "projectDefinition": "# Compiled Precept\n## Overview\n- Name: `LoanApplication`\n..."
}
```

### Phase 3 — Cleanup

#### Files to delete

- `tools/Precept.Mcp/Dtos/LanguageToolDtos.cs`
- `tools/Precept.Mcp/Dtos/NewToolDtos.cs`
- `tools/Precept.Mcp/Tools/LanguageTool.cs` (after the focused tools stop calling it)
- `test/Precept.Mcp.Tests/LanguageToolTests.cs` (delete rather than preserving tests for a dead internal aggregate tool)

#### Files to modify

- `tools/Precept.Mcp/Tools/QuickstartTool.cs` — return `string`; call `CatalogFormatters.FormatQuickstart()`
- `tools/Precept.Mcp/Tools/SyntaxTool.cs` — return `string`; call `CatalogFormatters.FormatSyntax(...)`
- `tools/Precept.Mcp/Tools/TypesTool.cs` — return `string`; call `CatalogFormatters.FormatTypes(...)`
- `tools/Precept.Mcp/Tools/OperationsTool.cs` — return `string`; preserve `category` filter, then format text
- `tools/Precept.Mcp/Tools/ProofsTool.cs` — return `string`; move mapping/rendering to formatter
- `tools/Precept.Mcp/Tools/PatternsTool.cs` — return `string`; format `SyntaxReference` patterns directly
- `tools/Precept.Mcp/Tools/DiagnosticTool.cs` — return `string`; keep lookup logic, replace DTO return
- `tools/Precept.Mcp/Tools/DomainsTool.cs` — return `string`; read catalogs directly, include temporal units
- `tools/Precept.Mcp/Tools/CompileTool.cs` — keep minimal diagnostic JSON; replace definition DTO graph with `ProjectDefinition` text
- `tools/Precept.Mcp/Dtos/CompileToolDtos.cs` — reduce to 3 records only
- `tools/Precept.Mcp/Formatters/CatalogFormatters.cs` — new file containing all catalog markdown rendering
- `test/Precept.Mcp.Tests/NewToolTests.cs` — rewrite from JSON-shape assertions to markdown content assertions
- `test/Precept.Mcp.Tests/RecoveryHintTests.cs` — assert proof/diagnostic markdown content instead of DTO property access
- `test/Precept.Mcp.Tests/CompileToolTests.cs` — assert `ProjectDefinition` presence/absence instead of `Definition`
- `test/Precept.Mcp.Tests/CompileToolDefinitionProjectionTests.cs` — replace DTO graph assertions with summary-section assertions
- `test/Precept.Mcp.Tests/DefinitionProjectionTests.cs` — replace DTO field/property assertions with summary text assertions or fold into a new compile-summary test file
- `test/Precept.Mcp.Tests/OutcomeKindProjectionTests.cs` — keep catalog `SerializedKind` assertions; replace compile DTO assertions with summary text assertions

#### Build verification

Minimum verification after the MCP rewrite:

```bash
dotnet build tools/Precept.Mcp/Precept.Mcp.csproj
dotnet test test/Precept.Mcp.Tests/Precept.Mcp.Tests.csproj
```

If Newman touches any shared docs describing the MCP contract, run the same MCP test suite after the
doc sync so the final commit closes green.

### Tests to write

Add or update MCP tests to prove the new contract, not merely the new signatures.

#### Catalog-tool tests

Rewrite `test/Precept.Mcp.Tests/NewToolTests.cs` around markdown/text behavior:

- `Quickstart_ReturnsMarkdownSections_ForOverviewConceptsToolGuideExamples`
- `Syntax_ReturnsMarkdown_WithGrammarConstructActionOutcomeOperatorSections`
- `Types_ReturnsMarkdown_WithTypesModifierBucketsAndFunctions`
- `Operations_FilteredCall_RendersFilterHeaderAndCorrectCount`
- `Proofs_ReturnsMarkdown_WithProofRequirementsAndRuntimeFaults`
- `Patterns_ReturnsMarkdown_WithCommonAndAntiPatternBlocks`
- `Diagnostic_LookupByName_RendersDiagnosticHeaderTriggerRecoveryAndExamples`
- `Diagnostic_MissingCode_RendersHelpfulNotFoundMessage`
- `Domains_ReturnsMarkdown_WithCurrenciesUnitsPrefixesDimensionsTemporalUnits`

Do **not** assert exact whitespace or full-table formatting. Assert stable headings, representative
lines, and key substrings.

#### Compile-tool tests

Replace DTO-shape tests with compile-contract tests:

- success -> `HasErrors == false`, diagnostics empty or non-error, `ProjectDefinition` non-null
- failure -> `HasErrors == true`, diagnostics populated, `ProjectDefinition == null`
- summary contains all fixed major sections: `## Overview`, `## Fields`, `## States`, `## Events`,
  `## Rules`, `## State Hooks`
- representative projection coverage still matters; keep the old behavioral scenarios, but assert
  against summary lines instead of object properties:
  - choice field metadata
  - modifier bound values
  - omitted fields per state
  - state access modes
  - event ensures
  - reject rows / outcome text
  - entry/exit hooks

#### Regression tests that will break and must be updated

These existing files are DTO-contract tests and will fail immediately once the rewrite lands:

- `test/Precept.Mcp.Tests/NewToolTests.cs`
- `test/Precept.Mcp.Tests/RecoveryHintTests.cs`
- `test/Precept.Mcp.Tests/CompileToolTests.cs`
- `test/Precept.Mcp.Tests/CompileToolDefinitionProjectionTests.cs`
- `test/Precept.Mcp.Tests/DefinitionProjectionTests.cs`
- `test/Precept.Mcp.Tests/OutcomeKindProjectionTests.cs`
- `test/Precept.Mcp.Tests/LanguageToolTests.cs` (delete with the file under test)
- `test/Precept.Mcp.Tests/ValueModifierDtoTestAccess.cs` (delete once modifier DTO reflection is gone)

### Dependency note

- **Runtime changes:** none. This is strictly `tools/Precept.Mcp/` contract/rendering work.
  `src/Precept/` stays untouched.
- **Language server / VS Code extension:** no code changes required. MCP contract reshaping is
  independent of LSP and the VS Code extension.
- **Architectural boundary:** keep using catalog metadata as the source of truth. This plan removes
  DTOs; it does **not** authorize hand-maintained prose or tool-local vocabulary tables.
