# README Restructure Proposal

**Author:** J. Peterman, Brand/DevRel  
**Date:** 2026-04-05 | **Revised:** 2026-04-07 (Shane feedback pass)  
**Status:** Draft — Revised for Shane Feedback  
**Inputs:** `brand/references/readme-research-peterman.md`, `docs/references/readme-research-steinbrenner.md`, `brand/references/readme-research-elaine.md`  
**Reviews:** Frank, George, Uncle Leo — 2026-04-06  
**Scope:** Structural recommendation only — not final README copy

---

## Preamble

Three independent research passes have now completed. Peterman studied brand/copy conventions across 13 comparable library READMEs. Steinbrenner mapped the developer evaluation journey against category-defining tools. Elaine reviewed both outputs through a UX/IA lens and issued explicit structural requirements.

This document synthesizes all three into a single recommended README structure. Elaine's requirements are treated as **hard constraints** — they are not recommendations to weigh against alternatives. The structure below satisfies all of them.

One open thread: Elaine's separate color palette/usage roles pass is still in flight. Where this proposal references color application (badge styling, code block syntax, image treatment), it anchors to the locked indigo-first brand system (`brand/brand-decisions.md`). Specific palette usage roles (which shade applies where in the README document itself) will be refined once that pass lands. Nothing in this proposal requires finalization of the palette pass before the rewrite begins.

---

## Hard Constraints (Elaine's Non-Negotiables)

These constraints are not design opinions. They are prerequisites. The README rewrite does not ship without satisfying every one.

| # | Constraint | Source |
|---|-----------|--------|
| 1 | Mobile-first above-the-fold: logo, hook, and primary CTA visible within 550px vertical viewport | Elaine §IA Requirements |
| 2 | No horizontal scrolling at 400px viewport width | Elaine §Viewport |
| 3 | Single primary CTA in Getting Started — secondary CTAs clearly subordinated | Elaine §CTA |
| 4 | H1 → H2 → H3 semantic heading hierarchy with no level skips | Elaine §Heading |
| 5 | Headings are descriptive, not clever — no emoji at heading start | Elaine §Scannability |
| 6 | Prose paragraphs max 2-3 sentences; feature lists use bullets, not prose | Elaine §Scannability |
| 7 | Visual separators (horizontal rules) between major sections | Elaine §Scannability |
| 8 | Hero code block: DSL only, ≤20 lines, lines ≤60 characters | Elaine §Progressive Disclosure |
| 9 | Runtime usage shown immediately after hero as a separate 5-line C# block | Elaine §Hero Refinement |
| 10 | Progressive disclosure: What → Can I read → Can I use → Why different → Learn more | Elaine §Progressive Disclosure |
| 11 | All code blocks tagged with language (` ```precept `, ` ```csharp `, ` ```bash `) | Elaine §AI Parseability |
| 12 | Descriptive link text — no "click here" or bare "here" | Elaine §AI Parseability |
| 13 | All images have descriptive alt text | Elaine §AI Parseability |
| 14 | Badge alt text includes version/status value | Elaine §Accessibility |
| 15 | Sample catalog table removed from main README — linked externally | Elaine §Viewport |
| 16 | Section order must not front-load differentiation before installation | Elaine §Progressive Disclosure |

---

## Recommended Section Order

```
# Precept
[Logo + wordmark]
[Badges: NuGet version, License, Build status]

> Definition block (one-line dictionary entry)

[One-sentence hook: positioning + mechanism phrase]
[One-sentence clarifier: unification claim]

---

## Quick Example
[bold lead-in: The Contract / The Execution — see Hero Treatment section]
[18-20 line DSL hero]
[5-line C# runtime block]

---

## Getting Started
### 1. Install the VS Code Extension
### 2. Create Your First Precept File
### 3. Add the NuGet Package

---

## What Makes Precept Different
### AI-Native Tooling
### Unified Domain Integrity
### Live Editor Experience

---

## Learn More
[Bulleted links: Docs, Language Reference, Samples, MCP Server, Contributing]

---

## Contributing
[Contributor-facing developer loop: build, extension install, test commands]

---

## License
```

---

## Section-by-Section Rationale

### Section 0: Title Block (Logo, Wordmark, Badges)

**Content:**
- Brand mark (SVG logo, full variant at 64px)
- Project name: `PRECEPT` in small-cap wordmark treatment where supported
- Badge row: NuGet version, MIT license, build status — in that order

**Rationale:** Follows the F-pattern entry point. Top-left is where developers form the first impression. Badge alt text must include the actual version/status value (constraint #14). Keep badge count to three — additional badges add visual noise without adding signal at the awareness stage.

**Above-the-fold requirement:** The entire title block plus the opening hook paragraph must clear 550px. This means: logo at 48px (not 64px SVG full), one badge row, one blockquote, and the two-sentence hook. Test at 550px before finalizing.

**Color:** Brand mark uses locked indigo `#6366F1`. Badge styling follows standard shields.io rendering; no custom badge colors needed here. Specific palette usage roles for the README document surface will be confirmed when Elaine's palette/usage pass lands.

---

### Section 1: Hook

**Content:**

```
> **pre·​cept** *(noun)*: A general rule intended to regulate behavior or thought.

**Precept is a domain integrity engine for .NET.** By treating your business constraints
as unbreakable precepts, it binds state, data, and rules into a single executable contract
where invalid states are structurally impossible.

It eliminates the fragmentation that comes from managing state, validation, and business rules separately.
```

**Rationale:** Two sentences. First positions the new category ("domain integrity engine for .NET") — the locked positioning statement from `brand/brand-decisions.md` — and carries the mechanism phrase that names the core idea: treating business constraints as unbreakable. Second is the clarifying unification claim that answers the question every developer will ask: "Wait, is this a state machine library? A validator? A rules engine?" Answer: all three, unified.

The definition blockquote is retained from the current README. It does meaningful work: it grounds the brand name in dictionary authority before the technical claim lands. It also serves AI agents — an AI parsing this README for the first time extracts the definition before the technical positioning.

**The "unbreakable precepts" phrase:** The current README's third sentence ("By treating your business constraints as unbreakable *precepts*, the engine ensures...") was previously flagged as redundant and cut in earlier drafts. On reflection, this phrase does distinct work: it connects the brand name to the mechanism in one beat, making the name feel earned rather than invented. Rather than cutting it, integrate it as the mechanism half of the positioning sentence. The key is fold, not append — added as a participial phrase, it strengthens without bloating.

**Voice check:** "domain integrity engine" is authoritative, not hyperbolic. "unbreakable precepts" earns the brand name without explaining it. "structurally impossible" is precise. All three conform to brand voice (authoritative, matter-of-fact, no hedging).

---

### Section 2: Quick Example

**Content:** Two code blocks — DSL hero, then C# runtime usage.

**Rationale:** The hero section answers the two most important reader questions in sequence:
1. "Can I read this DSL?" (answered by the precept block)
2. "How do I use this in my C# app?" (answered by the execution block)

Peterman's research established that hero code should show a complete round trip (definition → usage → result). Elaine refined this: split the two concerns into separate labeled blocks rather than one 49-line combined block (which is what the current README has). Both are right. The split format serves progressive disclosure *and* AI parseability — an AI agent with context-window constraints can extract the DSL block independently from the C# block.

**Hero code treatment:**

```precept
[18-20 lines, DSL only]
```

Headed with: `**The Contract** (\`filename.precept\`):`

**Runtime usage treatment:**

```csharp
[5 lines: parse → compile → create instance → fire → check result]
```

Headed with: `**The Execution** (C#):`

The C# block must show the minimal round trip. Five lines is the target. The current README's execution block runs to 45+ lines with detailed comments and multiple API calls — all of that belongs in the quickstart guide docs, not the hero.

**Hero sample selection:** Peterman recommended Subscription Billing (18 DSL statements). Elaine noted that Order Fulfillment may be more universally relatable. Both are valid. This proposal defers sample selection to Shane's judgment — the structural commitment here is the form (18-20 lines, DSL only, real-world domain), not the specific domain. A/B testing is the right validation method, but it requires a published README. Ship with one, adjust based on feedback.

**Time Machine vs. new sample:** The current README uses Time Machine. It's memorable and playful, which fits the brand's slight warmth. The question is whether "88 mph and 1.21 gigawatts" as a guard condition reads as serious DSL or as a toy demo. For a category-creation play, the hero domain should feel like real business logic. Recommendation: use a business domain (Order, SubscriptionBilling, LoanApplication) for the hero — keep Time Machine in the samples catalog where its playfulness is an asset.

**Viewport check (constraint #8):** Lines must stay under 60 characters. Current hero code includes lines like:
```
from Accelerating on FloorIt when FloorIt.TargetMph >= 88 && FloorIt.Gigawatts >= 1.21 -> set Speed = FloorIt.TargetMph -> transition TimeTraveling
```
That is 140 characters — it will require horizontal scrolling at 400px. The new hero must be designed for narrow viewports from the start.

---

### Section 3: Getting Started

**Content:** Numbered sequence, three steps.

```markdown
## Getting Started

Precept is a domain integrity engine for .NET. Install the VS Code extension to start authoring `.precept` files with live feedback.

> **Prerequisite:** The language server requires the [.NET 10 SDK](https://dotnet.microsoft.com/download). Install it before step 1 if you don't have it already.

### 1. Install the VS Code Extension

Search for **Precept DSL** in the VS Code marketplace, or run:

\```bash
code --install-extension sfalik.precept-vscode
\```

You'll see syntax highlighting and live diagnostics as you type.

### 2. Create Your First Precept File

Open VS Code, create `Order.precept`, and type along with the hero example above.
The language server provides completions, hover documentation, and error detection in real time.

### 3. Add the NuGet Package

When you're ready to integrate with your C# project:

\```bash
dotnet add package Precept
\```

See the [Quickstart Guide](link) for a complete walkthrough including runtime integration.
```

**Rationale:** Elaine's primary CTA requirement (constraint #3) is satisfied by the numbered sequence. Developers know exactly what to do first (VS Code extension), what comes second (author a file), and what comes third (integrate). There is no decision paralysis because there is no branch.

**Primary CTA:** Install VS Code extension. It is the lowest-friction entry point — no C# project needed, immediate visual feedback, proves tooling works before any code is written. This is the adoption path Steinbrenner's research identified as "tooling-first, DSL-second."

**Secondary CTA:** Add NuGet package (step 3). Clearly subordinated by sequence and by the qualifier "when you're ready."

**Tertiary CTA (deferred):** Copilot plugin installation is removed from Getting Started entirely. It belongs in the "AI-Native Tooling" section of "What Makes Precept Different," visible only to developers who have already committed to Precept. This resolves the **three-CTA problem** in the current README: the current Getting Started presents VS Code extension, NuGet package, and Copilot plugin as three simultaneously equal next steps with no hierarchy. The problem is not having three CTAs — it is having three primary CTAs at the same decision point. The numbered sequence here enforces hierarchy: one step at a time, one decision at a time.

**Section-level context reminder (Elaine):** Each section after the hero includes a one-sentence context reminder in the subheading or opening line so non-linear readers don't lose orientation. The context reminder appears as the opening sentence of the Getting Started template above: "Precept is a domain integrity engine for .NET. Install the VS Code extension to start authoring \.precept\ files with live feedback." This satisfies Elaine's requirement for non-linear readers who arrive at Getting Started without reading the hook.

---

### Section 4: What Makes Precept Different

**Content:** Three subsections, each using bulleted lists.

**Rationale:** Differentiation content comes *after* the trial decision, not before it. This is the structural correction both Steinbrenner and Elaine flagged: the current README front-loads tooling description (section `🛠️ World-Class Tooling` appears before the user has been told how to install anything). Under progressive disclosure (constraint #10), the reader arrives here only after they've answered "Can I use this?" — at which point they are receptive to the "why this over alternatives" argument.

**Subsection 4a: AI-Native Tooling**

Content:
- MCP server: 5 tools (`precept_compile`, `precept_fire`, `precept_inspect`, `precept_update`, `precept_language`)
- GitHub Copilot plugin: agent definition + 2 skills for DSL authoring and debugging
- Language server: diagnostics, completions, hover, semantic tokens, live preview

Close with: "AI agents can validate, inspect, and iterate on `.precept` files through structured tool APIs."

**Rationale:** Precept is the only tool in the category that ships a dedicated MCP server, Copilot plugin, and full LSP as a unified AI tooling story. Of the 13 projects in Peterman's study, only NRules mentioned AI tooling — and it was a single link to a custom GPT in the Getting Started section. Precept should *lead* with the AI tooling story in its differentiation section, not bury it. This is also the Claude Marketplace and Copilot Marketplace audience: when AI agents evaluate Precept for a user, this section is what convinces them.

**AI-audience note:** This section is read by AI agents evaluating Precept on behalf of developers. The bulleted list format (not prose) is specifically designed for AI parseability — bullet points extract cleanly into structured context.

**Subsection 4b: Unified Domain Integrity**

Content:

Opening (1–2 sentences of "why this approach" — kept in the README, not deferred):
> State machines enforce transitions. Validators check fields. Rules engines evaluate conditions. When they live in separate libraries, they disagree: a validator allows a value the state machine would reject; a rules engine fires on a state that was never legal. Precept unifies all three so the constraints are physically co-located with the state they govern.

Followed by bullets:
- Prevention, not detection — invalid states are structurally impossible
- One file, complete rules — every guard, constraint, rule, and transition in one DSL definition
- Full inspectability — preview any action and its outcome without mutation
- Compile-time checking — unreachable states, dead ends, type mismatches, null-safety violations caught before runtime

**Rationale:** This subsection carries the "why this approach" argument that belongs in the README, not deferred to docs. The opening 2-sentence rationale is not philosophy — it is the concrete diagnosis developers need to recognize their own problem. Without it, the bullets read as a feature list. With it, they read as a solution. Keep this prose in the README. The deep construct reference (how the engine pipeline works, what the 6-stage fire produces) lives in the docs. The core reasoning — *why* co-location prevents disagreement — stays here.

**Voice check:** Every bullet uses declarative, present tense with no hedging. "Structurally impossible" is precise. "One file" is concrete. These are facts, not promises.

**Subsection 4c: Live Editor Experience**

Content (bulleted):
- Context-aware IntelliSense: completions respect DSL scope and grammar position
- Semantic syntax highlighting: one color per semantic category; italic signals constraint pressure
- Live state diagram: VS Code preview panel generates the diagram from the DSL definition
- Inline diagnostics: unreachable states, dead-end events, and type errors flagged as you type

**Rationale:** This section was titled "World-Class Tooling" in the current README and buried 150 words of prose. The transformation to bulleted format is required by constraint #5. Each bullet is one concrete capability. The section name is "Live Editor Experience" — descriptive (constraint #4), not clever. "Language Server and Preview" (the earlier proposed title) is an implementation label; developers don't think in LSP protocol terms, they think in experience terms.

**Color note:** The semantic syntax highlighting claim ("one color per semantic category; italic signals constraint pressure") is a factual description of the locked 8-shade system in `brand/brand-decisions.md`. When Elaine's palette/usage pass lands, she may specify how colors are rendered in the README itself (e.g., if the docs site uses code highlighting). Until then, DSL code blocks in the README render with GitHub's default styling — the brand color system is not expressible in standard GitHub Markdown. No blocking dependency.

---

### Section 6: Contributing

**Content:** Contributor-facing section. Kept in the README but positioned at the bottom — after Learn More and before License — so it does not interrupt the user onboarding flow.

```markdown
## Contributing

Precept is built with .NET 10.0 and TypeScript. The VS Code extension, language server,
MCP server, and runtime are all in this repository.

### Build

```bash
dotnet build
```

### Language Server

```bash
dotnet build tools/Precept.LanguageServer/Precept.LanguageServer.csproj \
  --artifacts-path temp/dev-language-server
```

The extension auto-detects a new build — no reload needed.

### VS Code Extension

```bash
cd tools/Precept.VsCode
npm run compile     # build TypeScript
npm run watch       # watch mode
npm run loop:local  # package and install locally
```

### Tests

```bash
dotnet test
```

See [Contributing Guide](link) for full contribution workflow, branch conventions,
and how to add sample files.
```

**Rationale:** The developer loop — build language server → extension auto-detects → edit `.precept` file → see diagnostics update — is the contributor workflow that enables productive iteration on the runtime and tooling. This content belongs in the README (not only in the contributing guide) because it lets a new contributor get oriented without leaving GitHub. It is not user onboarding. Users of Precept don't need to build the language server; contributors do. Positioning it at the bottom, after the primary user flow is complete, satisfies both goals: users see what they need without noise; contributors find what they need without hunting.

**What this is not:** This section is not a replacement for a full `CONTRIBUTING.md`. It is the "get me running in 5 minutes" entry point for contributors, with a link to the full guide for everything else.

---

### Section 5: Learn More

**Content:** Bulleted link list.

```markdown
## Learn More

- [Language Reference](link) — full DSL syntax and construct reference
- [Sample Catalog](link) — 20+ domain models in `.precept`
- [Quickstart Guide](link) — step-by-step integration walkthrough
- [MCP Server Documentation](link) — tool reference for AI agent integration
- [Contributing](link) — how to contribute to Precept
```

**Rationale:** This section replaces the current README's Sample Catalog table (21 rows × 3 columns — fails constraint #15) and the scattered documentation links. All links use descriptive text (constraint #12). The sample catalog moves here as a link, not an embedded table. Complex reference material belongs in external docs; the README's job is to create the desire to go there.

**CTA note:** The "Learn More" section contains no competing actions — it is a reference navigation list, not a decision point. This is consistent with constraint #3 (single primary CTA in Getting Started).

---

### Section 6: License

**Content:** One-line MIT statement with badge.

Rationale: Legal hygiene. Keep it minimal. The current README handles this correctly.

---

## Hero Treatment (Detailed)

### Form

Two labeled blocks, separated by a blank line, under the `## Quick Example` heading:

```
**The Contract** (`filename.precept`):
```precept
[DSL — 18-20 lines]
```

**The Execution** (C#):
```csharp
[5 lines: parse → compile → create → fire → check]
```
```

### DSL Block Specification

- **Length:** 18-20 lines (18 is the target; 20 is the ceiling)
- **Line length:** ≤60 characters per line (viewport constraint #8)
- **Contents:** `precept` declaration, 2-3 fields, 3 states (`initial` marked), 2 events, 3-4 `from/on` transition rows including one guard, one `rule` or `ensure`
- **Domain:** Business-relevant (Order, Subscription, LoanApplication) — not a toy or pop-culture reference
- **Language tag:** ` ```precept ` — required for AI parseability (constraint #11)

### C# Block Specification

- **Length:** 5 lines maximum
- **Contents:** `PreceptParser.Parse(...)`, `PreceptCompiler.Compile(...)`, `engine.CreateInstance(savedState, savedData)` (or `engine.CreateInstance()` for a new entity), `engine.Fire(...)`, result check
- **Style:** No comments, no imports, minimal variable names — this is a sketch, not a tutorial
- **Language tag:** ` ```csharp ` — required (constraint #11)

### What the Hero Must Not Include

- Comments explaining the DSL constructs (that's the language reference's job)
- Full C# startup boilerplate (`using`, namespace, class declaration)
- JSON state snapshots or fire result verbose output
- More than one `precept` declaration
- Any line exceeding 60 characters

---

## CTA Strategy

Three audiences, three CTAs, one primary path. The problem in the current README is not that it has three CTAs — it is that all three appear at the same decision point with equal visual weight and no hierarchy. A developer arriving at Getting Started faces: install the VS Code extension, add the NuGet package, and install the Copilot plugin — simultaneously, with no signal about which to do first. That is the **three-CTA problem**: not the count, but the equal weight. This proposal resolves it by distributing the three CTAs across three adoption stages.

### Primary CTA — VS Code Extension (Awareness stage → Trial stage)

**Location:** Getting Started, step 1  
**Action:** `code --install-extension sfalik.precept-vscode`  
**Why primary:** Lowest friction. No C# project required. Immediate visual reward (syntax highlighting, diagnostics). Proves tooling works before any integration commitment.

### Secondary CTA — NuGet Package (Trial stage → Adoption stage)

**Location:** Getting Started, step 3  
**Action:** `dotnet add package Precept`  
**Why secondary:** Requires a C# project. Assumes developer has already decided to try Precept. The "when you're ready" qualifier signals this is a progression, not a simultaneous decision.

### Tertiary CTA — Copilot Plugin (Adoption stage → Power use)

**Location:** What Makes Precept Different → AI-Native Tooling  
**Action:** Link to plugin installation docs  
**Why tertiary:** Optional capability for developers already using Precept with Copilot. Placing it in Getting Started alongside the VS Code extension and NuGet package (its current position) creates the three equal-weight CTA problem described above. Placing it after the trial decision removes it from the primary path entirely — the developer encounters it only after they've already committed to Precept.

### CTA Hierarchy Enforcement

The numbered sequence in Getting Started (1, 2, 3) enforces hierarchy visually without requiring any copy explaining it. Developers read numbered lists as sequential. No additional CTA framing is needed.

---

## Serving Human and AI Readers

Both audiences matter. The README serves them simultaneously by satisfying the same structural requirements that serve each:

### Human Reader Needs → Structural Choice

| Human need | Structural choice |
|-----------|------------------|
| F-pattern scanning (left edge = headings) | Descriptive H2/H3 headings that communicate content before the reader enters the section |
| Z-pattern exit (bottom-right = CTA) | Learn More section at bottom with clear link list |
| Cognitive load: 5-7 lines per scan | ≤20-line hero; bullets over prose; 2-3 sentence paragraphs |
| Progressive disclosure | Section order: What → Read → Use → Why → Learn |
| Mobile viewport | 60-char line limit; no embedded tables; responsive images |

### AI Reader Needs → Structural Choice

| AI need | Structural choice |
|---------|------------------|
| Document outline extraction | Semantic H1 → H2 → H3 hierarchy |
| DSL vs. host language disambiguation | Language tags on all code blocks (` ```precept `, ` ```csharp `) |
| Link context (relative paths don't resolve) | Descriptive link text; absolute URLs for external links |
| Image content (agents can't see images) | Descriptive alt text on all images; state diagram described in surrounding prose |
| Hero extraction without truncation | ≤20-line hero fits in any standard context window |
| Tool inventory for agent use | AI-Native Tooling section as structured bullet list — extracts cleanly into agent context |

### The Dual-Audience Principle

Every structural decision in this proposal was made against both checklists. There are no structural elements that serve humans but hurt AI readability, or vice versa. The most important single decision is **language tags on all code blocks** — this is the highest-leverage change for AI parseability and costs nothing for human readers.

---

## Explicit Constraints the README Rewrite Must Respect

The following are not suggestions. They are gates.

### From Elaine (Hard Constraints — see full list above)

All 16 constraints in the table at the top of this document must be satisfied before the README ships.

### From Brand Decisions (Locked)

1. **Positioning:** Opening hook uses "domain integrity engine for .NET" — no substitutions
2. **Voice:** Declarative, present tense, no hedging, no superlatives
3. **Category-creation framing:** README educates on the category; does not compare to state machine libraries, validators, or rules engines by name
4. **Wordmark:** If the README renders the project name as a styled element (e.g., on the docs site), it uses Cascadia Cove 700, small-caps, 0.1em letter-spacing

### From Peterman Research

5. **Hybrid model:** README is enough to understand and trial Precept; deep construct documentation lives in docs, not README
6. **Hero domain is real business logic** — not a toy demo (Time Machine is appropriate for samples; not appropriate for the category-creation hero)
7. **No "Why Precept vs. X?" section** — implicit differentiation only; no competitor names
8. **Concrete claims over abstract:** Use specific numbers where possible ("5 MCP tools", "3 runtime APIs", "20+ sample domain models")

### From Steinbrenner Research

9. **Comparison handling:** Pattern #1 only (implicit differentiation via category naming) — no explicit competitive comparisons in the README
10. **Minimum path to first working file:** VS Code extension → create file → NuGet — linear, no branches

### Color Application Deferral

11. **Palette usage roles pending:** Where the README doc surface itself uses color (badge styling, syntax highlighting in rendered code blocks), anchor to the locked indigo-first system. Specific role assignments for the README surface will be specified in Elaine's palette/usage pass. No color decisions in the rewrite should override or anticipate that pass.

---

## What This Proposal Does Not Decide

- **Final hero sample:** Domain is deferred to Shane's judgment (Order vs. Subscription Billing vs. LoanApplication). The structural specification is locked.
- **Logo/brand mark finalization:** The brand mark appears in the title block; its final SVG form is Shane's call.
- **Docs site links:** All "link" placeholders in this document represent real URLs that must exist before the README ships.
- **Badge values:** NuGet version and build status badges require a published package and CI pipeline.
- **Light mode support:** Elaine flagged this as an open question for the VS Code extension; the README itself uses standard GitHub Markdown rendering which handles light/dark automatically.

---

## Summary: Why This Structure

The current README is a well-intentioned engineering document. It answers the questions engineers ask when they already know what Precept is. The restructured README answers the questions developers ask when they've never heard of it.

The difference: **the current README teaches before it proves. The restructured README proves before it teaches.**

Hero code comes before installation. Installation comes before differentiation. Differentiation comes before deep philosophy. The core reasoning — why treating business constraints as unbreakable precepts prevents disagreement between separately-managed rules — stays in the README. The deep construct reference belongs in the docs.

*(Proposed README tagline — confirm or substitute during rewrite):* One file. Every rule. Prove it in 30 seconds.

---

## What the Restructure Trims, Compresses, Defers, or Removes

A cost-at-a-glance summary for Shane. Every item below exists in the current `README.md` and does **not** survive the restructure in its present form.

---

### Removed Entirely

| What | Current form | Fate |
|------|-------------|------|
| "By treating" phrase in hook | "By treating your business constraints as unbreakable *precepts*, the engine ensures..." — standalone third sentence | Integrated as the mechanism half of the positioning sentence — not cut |
| Full C# boilerplate in the hero | `using` statements, namespace, class declaration, detailed comments | Cut — hero is a sketch, not a tutorial |
| Three-CTA Getting Started | VS Code extension + NuGet package + Copilot plugin all presented as equal-weight concurrent choices at the same decision point | Copilot plugin CTA removed from Getting Started; distributed to differentiation section — three CTAs remain but are now sequenced across adoption stages, not stacked at the entry point |
| Badge wall risk | Any badge beyond the current two | Hard cap at three: NuGet version, license, build status |

---

### Compressed

| What | Current form | New form |
|------|-------------|----------|
| Hero code block | 49-line combined DSL + C# block | 18-20 line DSL only + separate 5-line C# block |
| C# execution block | 45+ lines with comments, imports, multiple API calls | 5 lines maximum: parse → compile → create → fire → check |
| "World-Class Tooling" | 150+ word prose section | 4 bullets under "Language Server and Preview" |
| Differentiation sections (Problem It Solves, Designed for AI, Pillars of Precept) | Front-loaded prose before installation | Collapsed into three bulleted subsections under "What Makes Precept Different" — which now appears *after* Getting Started |
| Documentation links | Scattered throughout the README | Consolidated into a single "Learn More" bulleted link list |

---

### Repositioned (Same Content, Lower Prominence)

| What | Current position | New position |
|------|-----------------|-------------|
| AI tooling / Copilot plugin | Getting Started (primary-level CTA, equal weight with VS Code and NuGet) | What Makes Precept Different → AI-Native Tooling (tertiary CTA, adoption stage) |
| Differentiation and philosophy content | Before installation — the reader encounters "Why Precept?" before "How do I install it?" | After Getting Started — the reader has trialed Precept before encountering the "Why" argument |
| Time Machine hero sample | Primary hero in Quick Example | Samples catalog only — replaced by a business domain example (Order / Subscription Billing / LoanApplication) |
| Developer loop / contributor build workflow | Absent or buried | New `## Contributing` section at bottom — retained for contributors, not in the user onboarding flow |

---

### Deferred to External Docs

| What | Deferred to |
|------|------------|
| Sample Catalog (21-row embedded table) | External link in Learn More → `[Sample Catalog](link)` |
| Complete runtime integration walkthrough | External link in Learn More → `[Quickstart Guide](link)` |
| Full DSL construct reference | External link in Learn More → `[Language Reference](link)` |
| MCP server tool documentation | External link in Learn More → `[MCP Server Documentation](link)` |
| Deep construct reference and extended philosophy | Docs site — the core "why this approach" reasoning stays in README (§ Unified Domain Integrity); the full pipeline mechanics, construct catalog, and extended design rationale live in docs |

---

### Net-New Content (Additions)

The restructure is not purely subtractive. These items are added:

| What | Why |
|------|-----|
| .NET 10 SDK prerequisite note in Getting Started | George (G2): without .NET, the language server silently fails — no diagnostics, no completions, no errors |
| One-sentence context reminder before Getting Started step 1 | Uncle Leo (RC-2): Elaine required this for non-linear readers who jump directly to Getting Started |
| Build status badge (third badge) | Rounds out the signal set without crossing the three-badge cap |
| Language tags on all code blocks (` ```precept `, ` ```csharp `, ` ```bash `) | Currently absent; required for AI parseability (constraint #11) |
| `## Contributing` section at bottom | Shane feedback: retain developer loop and contributor build workflow; position as contributor content, not user onboarding |
| 1–2 sentence "why this approach" opening in § Unified Domain Integrity | Shane feedback: keep core reasoning in the README — not all philosophy deferred to docs |

---

*Trim summary updated 2026-04-07 (review gap pass, J. Peterman). Required changes from Frank/George/Uncle Leo reviews now addressed: AI capability overclaim corrected (Frank RC-4 / George G3 complementary); `RestoreInstance` removed from C# block spec (George G1); `.NET 10 SDK` prerequisite added to Getting Started template (George G2); explicit context reminder added before Getting Started step 1 (Uncle Leo RC-2); "maintenance anxiety" phrasing replaced (Uncle Leo WC-1); hook "replaces" softened to "eliminates fragmentation" (George G3); closing tagline labeled as proposed copy (Uncle Leo WC-3). Items from Shane's feedback pass remain: "by treating" phrase retained; contributor section added; "why this approach" reasoning retained in README; three-CTA problem clarified.*
