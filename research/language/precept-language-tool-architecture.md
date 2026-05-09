# `precept_language` Tool Architecture Audit

**Date:** 2026-05-09  
**Requested by:** Shane  
**Author:** Newman (MCP/AI Dev)

---

## Scope and sources

Reviewed:

- `tools/Precept.Mcp/Tools/LanguageTool.cs`
- `tools/Precept.Mcp/Tools/CompileTool.cs`
- `tools/Precept.Mcp/Tools/PingTool.cs`
- `tools/Precept.Mcp/Dtos/LanguageToolDtos.cs`
- `docs/tooling/mcp.md` as the current living MCP design/spec doc (the requested `docs/McpServerDesign.md` path does not exist in the repo anymore)
- `.github/skills/precept-authoring/SKILL.md`
- `.github/skills/precept-debugging/SKILL.md`
- `.github/agents/precept-author.agent.md`
- Actual `precept_language` output captured from the MCP tool

Measured on the actual tool payload:

- Compact reserialization size: **196,936 bytes (~192.3 KiB)**
- Rough token cost: **~49k–55k tokens**

---

## Part 1 — Current tool architecture analysis

## 1. What the 192 KB output actually contains

`LanguageTool.Language()` assembles one top-level object in a fixed order at `LanguageTool.cs:24-45`:

1. `tokens`
2. `types`
3. `modifiers`
4. `actions`
5. `constructs`
6. `constraints`
7. `operators`
8. `functions`
9. `diagnostics`
10. `domains`
11. `firePipeline`

The top-level shape is defined by `LanguageReferenceDto` in `LanguageToolDtos.cs:3-15`, and the emitted JSON uses camelCase keys matching the spec example in `docs/tooling/mcp.md:187-221`.

### Size breakdown by top-level section

| Section | Source lines | Count | Bytes | Share | Notes |
|---|---:|---:|---:|---:|---|
| `domains` | `LanguageTool.cs:40-44`, `277-310` | 326 | 57,169 | 29.1% | Mostly UCUM + currencies |
| `tokens` | `LanguageTool.cs:26`, `47-58` | 139 | 46,436 | 23.6% | Includes editor/tokenization metadata |
| `diagnostics` | `LanguageTool.cs:39`, `265-275` | 116 | 36,379 | 18.5% | Large because every rule carries messages/hints/sources |
| `types` | `LanguageTool.cs:27`, `60-111` | 32 | 21,410 | 10.9% | Rich metadata: traits, widening, qualifiers, accessors |
| `functions` | `LanguageTool.cs:38`, `248-263` | 23 | 9,347 | 4.7% | Overloads + parameter/return metadata |
| `modifiers` | `LanguageTool.cs:28-33`, `113-165` | 29 | 8,835 | 4.5% | Grouped by subtype |
| `constructs` | `LanguageTool.cs:35`, `179-205` | 12 | 7,011 | 3.6% | The most authoring-helpful section, but small and buried |
| `actions` | `LanguageTool.cs:34`, `167-177` | 15 | 5,284 | 2.7% | Mutation verbs + applicability |
| `operators` | `LanguageTool.cs:37`, `227-246` | 21 | 4,155 | 2.1% | Precedence + associativity |
| `constraints` | `LanguageTool.cs:36`, `206-225` | 5 | 637 | 0.3% | Scope + trigger tokens |
| `firePipeline` | `LanguageTool.cs:11-20`, `45` | 7 | 132 | 0.1% | Names only |

**Key fact:** four sections dominate the payload:

- `domains` + `tokens` + `diagnostics` + `types`
- **161,394 bytes (~82%)** of the entire reference

### Modifier breakdown

`modifiers` is one of only two grouped sections. `LanguageTool.cs:28-33` groups by catalog subtype instead of returning one flat list.

| Modifier group | Count | Bytes |
|---|---:|---:|
| `field` | 15 | 5,819 |
| `state` | 7 | 1,700 |
| `access` | 3 | 632 |
| `anchor` | 3 | 453 |
| `event` | 1 | 181 |

### Domain breakdown

`domains` is the single biggest block, and it is heavily skewed.

| Domain group | Count | Bytes | Share of total |
|---|---:|---:|---:|
| `ucumTier1Units` | 150 | 40,530 | 20.6% |
| `currencies` | 159 | 14,238 | 7.2% |
| `dimensions` | 10 | 1,660 | 0.8% |
| `temporalUnits` | 7 | 677 | 0.3% |

### Shape details that matter

The output is not just “vocabulary.” It is a mixed bundle of:

- **Authoring-critical syntax** (`constructs`, `actions`, `operators`, `constraints`)
- **Type-system semantics** (`types`, `modifiers`, `functions`)
- **Domain registries** (`domains`)
- **Diagnostic catalog** (`diagnostics`)
- **Editor/tooling metadata** inside `tokens`:
  - `textMateScope`
  - `semanticTokenType`
  - `validAfter`
  - `isMessagePosition`

That last point matters: the second-largest section is not purely authoring guidance. A writing agent pays for VS Code / grammar-facing metadata whether it needs it or not.

## 2. Is the information organized for AI consumption or human reference?

**Verdict: reference-first, not AI-task-first.**

What is good:

- The shape is deterministic and DTO-backed.
- Modifiers and domains are at least grouped.
- Constructs include `usageExample` and `snippetTemplate` (`LanguageTool.cs:183-191`), which are authoring-friendly.
- Types include qualifier/accessor metadata, which is essential for correctness.

What is not AI-optimized:

- Most sections are **flat arrays**, not lookup maps.
- Cross-references are **plain strings**, not object references or keyed indexes:
  - `widensTo`
  - `impliedModifiers`
  - `allowedIn`
  - `mutuallyExclusiveWith`
  - `relatedCodes`
- The agent must do repeated **linear scans** to answer simple questions like:
  - “What does `from` start?”
  - “Which modifiers apply to `money`?”
  - “Which operators are valid here?”
- The bundle is grouped by **catalog ownership**, not by **authoring task**.

In practice the current tool answers: “What is everything the catalogs know?”

It does **not** answer: “I need to write a field declaration right now; show me the minimum reliable contract.”

## 3. Token cost

Measured compact payload: **196,936 bytes / 196,685 characters**.

Reasonable estimate:

- **~49,171 tokens** at ~4 chars/token
- **~54,635 tokens** at ~3.6 chars/token
- Practical planning range: **~50k tokens**

Context-window impact:

- **128k window:** ~38%–43%
- **200k window:** ~25%–27%
- **256k window:** ~19%–21%

For a tool that is supposed to be the first grounding step for AI authoring, that is expensive enough to distort the whole session. One call can consume a third of the working budget before the agent has written a line of DSL or seen diagnostics.

## 4. How an AI agent currently uses this tool, and where it hurts

The intended workflow in docs is:

- `precept_language` → `precept_compile` → `precept_inspect` → `precept_fire` (`docs/tooling/mcp.md:690-693`)

The current authoring skill says the same thing more strongly:

- `precept-authoring/SKILL.md:16-19` tells the agent to call `precept_language` first
- `precept-debugging/SKILL.md:16` also points to `precept_language` before guessing syntax/semantics

### Current agent experience

1. Call one tool.
2. Receive a ~192 KB JSON blob.
3. Internally search that blob for syntax, type, diagnostics, and domain rules.
4. Try to write Precept from memory of that blob.

### Friction points

- **All-or-nothing download.** There is no cheap “write-now” reference.
- **Syntax is buried under non-syntax.** Domains + diagnostics + tokens dominate the payload.
- **No lookup form.** The agent has to scan arrays instead of jumping by key.
- **No tool guide.** The tool does not explain when to switch to `precept_compile`.
- **No authoring loop.** The compile/fix cycle lives in skills/docs, not in the tool contract.
- **No compact examples section.** Some examples exist inside constructs, but they are sparse and distributed.
- **Runtime guidance is incomplete.** `firePipeline` is only stage names; the tool does not explain first-match row semantics, compile-before-runtime gating, or when runtime tools add value.

---

## Part 2 — Splitting options analysis

## Option A — Section-filtered single tool

`precept_language(section?: string)`

### Pros

- Smallest migration from the current tool.
- Keeps one canonical tool name.
- Full-output behavior can remain unchanged for backward compatibility.
- Easy to implement because the source data already exists in `LanguageTool.cs`.

### Cons

- The contract becomes **stringly typed**: agents must guess valid section names.
- Return shape becomes a **union** unless every section is wrapped in a consistent envelope.
- Discoverability is weak: tool list still shows one big “language” tool.
- Some sections are still large:
  - `syntax` would still be roughly token/construct/operator heavy
  - `domains` is still ~57 KB unless split again

### Implementation complexity

**Low to medium.** Mostly projection and schema work.

### AI ergonomics

**Medium.** Better than today, but the tool-selection problem remains implicit.

## Option B — Dedicated companion tools

Keep `precept_language` as the exhaustive reference and add focused tools.

### Pros

- **Best explicit contract.** Tool names themselves tell the agent what to call.
- Each tool can have a **stable, purpose-built schema**.
- Great first-call ergonomics for authoring: `precept_syntax_reference` can be tiny and high-signal.
- Keeps `precept_language` intact as the “full dump” fallback.
- Tool descriptions can steer AI behavior directly.

### Cons

- More tool surface area.
- Some content will be projected twice (full reference + focused reference).
- Docs, skills, and the plugin agent instructions must be updated together.

### Implementation complexity

**Medium.** More DTOs and docs, but still straightforward projection work.

### AI ergonomics

**High.** This is the cleanest contract for a model deciding which tool to call.

## Option C — Intent-based tools

`precept_how_to_write(topic: string)` / `precept_examples(construct: string)`

### Pros

- Closest to the agent’s immediate task.
- Can be very compact.
- Can produce highly actionable examples.

### Cons

- `topic` is ambiguous and hard to stabilize.
- Strong risk of fuzzy, prose-heavy responses instead of crisp structured output.
- Requires maintaining a topic taxonomy that is not already present in the catalogs.
- Easiest option to drift into “AI advice layer” instead of clean contract.

### Implementation complexity

**High.** Requires authored content design, topic routing, and stronger maintenance discipline.

### AI ergonomics

**High when it works, brittle when it doesn’t.**

## Option D — Streaming/progressive disclosure

`precept_quick_reference` + current `precept_language`

### Pros

- Excellent first-call size.
- Minimal conceptual overhead.
- Easy to explain: quick start first, full reference second.

### Cons

- Only solves the first call.
- Does not create focused deep references for types or domains.
- Agents still fall off a cliff from ~5 KB to ~192 KB when quick reference is insufficient.

### Implementation complexity

**Low.**

### AI ergonomics

**Medium-high.** Good first step, weak mid-tier story.

## Option E — No split, just reorganize

### Pros

- Preserves one tool and one payload.
- Could improve navigability with better grouping and examples.
- No tool proliferation.

### Cons

- Does **not** solve the core problem: the payload still costs ~50k tokens.
- Still one atomic response (`docs/tooling/mcp.md:1028-1032`).
- AI still pays for everything up front.

### Implementation complexity

**Medium.**

### AI ergonomics

**Medium-low.** Better shaped, still too big.

---

## Part 3 — What is structurally missing for AI writing agents

## 1. Does the tool tell the agent how to use the other MCP tools?

**No.**

`precept_language` returns vocabulary only. It does not include:

- what `precept_compile` is for
- when `precept_inspect` becomes useful
- when `precept_fire` is worth calling
- when `precept_update` is the right lane

This is currently documented outside the tool (`docs/tooling/mcp.md:690-693`) and implied by skills, not carried by the AI-facing contract itself.

**Recommendation:** add a compact `toolGuide` section somewhere in the authoring-oriented reference surface.

One important caveat: today the repo only implements `precept_ping`, `precept_language`, and `precept_compile` in `tools/Precept.Mcp/Tools/`. Any tool-guide content must reflect **actual availability**, not the aspirational 5-tool surface.

## 2. Does it explain the authoring workflow?

**No.**

The compile/fix loop exists in:

- `precept-authoring/SKILL.md:71-90`
- `precept-debugging/SKILL.md:20-67`

That workflow is absent from `precept_language` itself. The tool gives raw language data but not the operating procedure for using it effectively.

**Recommendation:** include an `authoringWorkflow` or `recommendedSequence` block in the focused authoring reference.

## 3. Are the MCP description fields optimized for AI tool selection?

**Not yet.**

Current descriptions:

- `precept_language`: “Return the complete Precept DSL vocabulary derived from language catalogs.” (`LanguageTool.cs:22-24`)
- `precept_compile`: “Parse, type-check, and analyze a precept definition. Returns diagnostics and a typed definition structure on success.” (`CompileTool.cs:12-14`)
- `precept_ping`: “Connectivity check — returns ok.” (`PingTool.cs:9-11`)

They are accurate, but they are not optimized for the moment of choice.

Example problem: `precept_language` does not warn the agent that it is the **largest** tool and should be used as the exhaustive fallback, not necessarily the first call for every authoring task.

## 4. Is the output format predictable enough for reliable AI parsing?

**Mostly yes at the serialization level, no at the retrieval level.**

Good:

- DTO-backed
- Stable section order
- Consistent record shapes within each section
- Predictable camelCase JSON

Weaknesses:

- No `schemaVersion`
- No `section` / `kind` index maps
- No counts/summaries up front
- Cross-links are raw strings instead of keyed references
- No consistent “query surface” for jumping from one concept to related concepts

So: the format is **parseable**, but not **navigable**.

## 5. Does it include enough execution-model context for correct code, not just valid code?

**Partially, but not enough.**

What it does include:

- `firePipeline` stage names (`LanguageTool.cs:11-20`, `45`)

What it does not include:

- first-match transition-row semantics
- row ordering importance
- compile-before-runtime gating
- when event preconditions run relative to transitions
- when state/event ensures fire relative to mutation
- how to use compile diagnostics as the main feedback loop

A writing agent can infer syntax from the current tool. It cannot reliably infer the **authoring method** or the **full behavioral model** it needs for good code generation.

---

## Part 4 — Primary recommendation

## Recommend **Option B: dedicated companion tools**

This is the cleanest AI contract.

Why this wins:

- It fixes the first-call problem without breaking the exhaustive reference.
- It avoids the stringly-typed ambiguity of `precept_language(section?: string)`.
- It gives each tool a stable schema and a clear description.
- It lets the plugin/skills teach a concrete tool-selection sequence.

## Proposed tool set

### 1. Keep `precept_language()` as the exhaustive reference

**Signature:**

```text
precept_language()
```

**Role:** full catalog/registry dump for exhaustive grounding and rare deep dives.

**Return shape:** keep current shape for compatibility:

- `tokens`
- `types`
- `modifiers`
- `actions`
- `constructs`
- `constraints`
- `operators`
- `functions`
- `diagnostics`
- `domains`
- `firePipeline`

**Estimated size:** stays **~192.3 KiB / ~50k tokens**.

### 2. Add `precept_syntax_reference()`

**Signature:**

```text
precept_syntax_reference()
```

**Purpose:** the default first call for “I need to write Precept now.”

**Return shape outline:**

```json
{
  "toolGuide": {
    "startWith": "precept_syntax_reference",
    "next": ["precept_compile"],
    "then": ["precept_inspect", "precept_fire", "precept_update"]
  },
  "authoringWorkflow": [ ... ],
  "declarationOrder": [ ... ],
  "keywordsByRole": {
    "declarations": [ ... ],
    "control": [ ... ],
    "outcomes": [ ... ],
    "actions": [ ... ]
  },
  "constructs": [ ... ],
  "operators": [ ... ],
  "actionPatterns": [ ... ],
  "canonicalExamples": [ ... ]
}
```

**Source material:** a curated projection of current tokens/constructs/operators/actions/constraints, plus new authored workflow/examples.

**Estimated size:** **~8-12 KiB**.

### 3. Add `precept_type_reference()`

**Signature:**

```text
precept_type_reference()
```

**Purpose:** pull type-system detail only when needed.

**Return shape outline:**

```json
{
  "types": [ ... ],
  "modifiers": { ... },
  "qualifierAxes": [ ... ],
  "wideningRules": [ ... ],
  "accessorRules": [ ... ],
  "constraintNotes": [ ... ],
  "functionSignatures": [ ... ]
}
```

**Source material:** primarily current `types`, `modifiers`, relevant `constraints`, relevant `functions`.

**Estimated size:** **~20-30 KiB**.

### 4. Add `precept_domain_reference()`

**Signature:**

```text
precept_domain_reference()
```

**Purpose:** on-demand lookup for domain vocabularies.

**Return shape outline:**

```json
{
  "currencies": [ ... ],
  "ucumTier1Units": [ ... ],
  "dimensions": [ ... ],
  "temporalUnits": [ ... ],
  "qualifierUsage": [ ... ]
}
```

**Source material:** current `domains`, with a tiny authored `qualifierUsage` guide.

**Estimated size:** **~55-60 KiB** if full; the UCUM block remains the dominant cost.

If the team wants to reduce this further, this is the one companion tool that should later gain a subgroup filter (`currencies`, `ucumTier1Units`, `dimensions`, `temporalUnits`).

## What stays vs. moves

### Stays in `precept_language`

Everything. It remains the canonical exhaustive reference.

### Moves into focused companion tools as curated projections

- `tokens` → relevant subsets into `precept_syntax_reference`
- `constructs` → `precept_syntax_reference`
- `operators` → `precept_syntax_reference`
- `actions` / `constraints` → `precept_syntax_reference`
- `types` / `modifiers` / relevant `functions` → `precept_type_reference`
- `domains` → `precept_domain_reference`

This is projection, not deletion.

## New authored content that should be added

The current tool surface is missing authored AI-operating guidance. Add this to the focused tools, not the full dump:

1. **`toolGuide`** — which MCP tool to call next and why
2. **`authoringWorkflow`** — write → compile → fix → only then runtime tools
3. **`canonicalExamples`** — small, validated snippets for:
   - field declaration
   - rule
   - state
   - event
   - `on <Event> ensure`
   - `from/on/when` transition row
   - `reject`
   - money/quantity/temporal examples where relevant
4. **`declarationOrder`** — the canonical file layout

This content is exactly what the current skill files are compensating for outside the tool contract.

## Migration path

### Existing callers

- `precept_language()` remains valid and unchanged.
- No breaking change for current clients.

### New preferred sequence for AI agents

1. `precept_syntax_reference()`
2. `precept_compile(text)`
3. `precept_type_reference()` if the issue is about types/modifiers/qualifiers/functions
4. `precept_domain_reference()` if the issue is about currencies/units/dimensions/temporal values
5. `precept_language()` only when the focused tools are insufficient

## Required skill / agent updates

### `.github/skills/precept-authoring/SKILL.md`

**Yes, update required.**

Current Step 1 (`precept-authoring/SKILL.md:16-19`) tells the agent to call `precept_language` first. That should change to:

- call `precept_syntax_reference` first
- call `precept_type_reference` only when the model needs type-system detail
- call `precept_domain_reference` only when working with domain vocabularies
- use `precept_language` as exhaustive fallback

### `.github/agents/precept-author.agent.md`

**Yes, small update recommended.**

The agent definition is still broadly correct, but it should teach the preferred tool-selection order explicitly. Right now it only says “treat the Precept MCP tools as the primary source of truth” (`precept-author.agent.md:17-18`). That is too generic once the surface grows.

### `.github/skills/precept-debugging/SKILL.md`

Not explicitly requested, but it should be updated too, because it currently tells the agent to call `precept_language` before guessing syntax/semantics (`precept-debugging/SKILL.md:16`).

---

## Bottom line

The current `precept_language` tool is a strong **catalog dump** and a weak **AI authoring contract**.

Its main structural problem is not correctness. It is that the first call is too expensive, too flat, and too reference-shaped for the job AI agents actually need to do.

**Ship dedicated companion tools, keep `precept_language` as the exhaustive fallback, and move workflow/examples/tool-selection guidance into the focused authoring surface.**