# README Research — PM/Product Adoption Perspective (Steinbrenner)

**Date:** 2026-04-04  
**Researcher:** Steinbrenner (PM)  
**Context:** Parallel research to Peterman's brand/copy research. This document focuses on developer evaluation journey, adoption drivers, and product positioning.

---

## Developer Evaluation Journey

Based on analysis of category-defining tools, developers move through four stages:

### 1. **Awareness** (First 5 seconds)
- **Trigger:** Problem recognition + tool name visibility
- **Decision point:** "Is this relevant to me?"
- **What works:** Problem statement in first line + visual logo/brand

### 2. **Evaluation** (30-60 seconds)
- **Trigger:** Quick scan for capability match
- **Decision point:** "Does this solve my specific problem?"
- **What works:** Feature list, quick example, social proof badges

### 3. **Trial Decision** (2-5 minutes)
- **Trigger:** Reading installation + quickstart
- **Decision point:** "Can I get this running quickly?"
- **What works:** One-command install, simple first example, clear next steps

### 4. **Adoption** (Post-trial)
- **Trigger:** Successful trial experience + seeing value
- **Decision point:** "Should I invest time learning this deeply?"
- **What works:** Comprehensive docs link, community signals, clear roadmap

---

## Projects Studied

### xstate (State management library)

- **Problem statement:** "Actor-based state management & orchestration for complex app logic."
- **Time-to-value:** Immediate — code example in section 2 (Super quick start)
- **Section map:**
  - Logo/tagline → Hook
  - 1-sentence description → Problem positioning
  - Templates grid → Proof (framework support)
  - Super quick start → Quickstart
  - Stately Studio section → Differentiation (visual tooling)
  - "Why?" section → Deeper problem explanation
  - Packages table → Reference
  - Code examples (FSM, hierarchical, parallel, history) → Education
  - SemVer policy → Trust signal
- **Comparison handling:** Implicit — doesn't mention competitors, but "Why?" section explains the formalism (statecharts) that differentiates it
- **Social proof:** Sponsors section (OpenCollective badge), Discord badge, templates across 4 frameworks
- **Primary CTA:** "Create state machines visually in Stately Studio → state.new" (drives to tooling ecosystem)

**Key insight:** xstate leads with the **conceptual hook** (actor model, statecharts), not implementation details. The README teaches you *why* you need state machines before showing you *how*.

---

### Polly (Resilience library for .NET)

- **Problem statement:** "Polly is a .NET resilience and transient-fault-handling library that allows developers to express resilience strategies such as Retry, Circuit Breaker, Hedging, Timeout, Rate Limiter and Fallback in a fluent and thread-safe manner."
- **Time-to-value:** 2 sections down (Quick start)
- **Section map:**
  - Logo + badges → Trust signals
  - Problem statement → Problem positioning
  - .NET Foundation badge → Social proof
  - NuGet packages table → Installation clarity
  - Documentation link → Reference
  - Quick start → Quickstart
  - Dependency injection example → Advanced quickstart
  - Resilience strategies (reactive/proactive) → Feature categorization
  - Strategy-specific code examples → Education
- **Comparison handling:** None — establishes "resilience strategies" as the category, no competitors mentioned
- **Social proof:** .NET Foundation membership, NuGet download badges, build status, code coverage
- **Primary CTA:** "pollydocs.org" for comprehensive docs

**Key insight:** Polly invents vocabulary ("resilience strategies", "reactive vs proactive"). The README is a **taxonomy** that teaches developers a mental model, not just a feature list.

---

### Temporal (Durable execution platform)

- **Problem statement:** "Temporal is a durable execution platform that enables developers to build scalable applications without sacrificing productivity or reliability. The Temporal server executes units of application logic called Workflows in a resilient manner that automatically handles intermittent failures, and retries failed operations."
- **Time-to-value:** Immediate — video embed in intro, then "Getting Started" is section 2
- **Section map:**
  - Logo + intro paragraph → Problem positioning
  - Video embed → Hook (visual explanation)
  - Getting Started (install + run samples) → Quickstart
  - Use CLI → Tool discovery
  - Use Web UI → Tool discovery
  - Repository context → Developer onboarding
  - Contributing section → Community CTA
- **Comparison handling:** "originated as a fork of Uber's Cadence" — direct lineage statement, positions as successor
- **Social proof:** "mature technology", "by the creators of Cadence", links to Temporal Technologies company
- **Primary CTA:** Install CLI, run samples

**Key insight:** Temporal leads with **category definition** ("durable execution platform") and uses a **video** to explain the concept. The README assumes you don't know what durable execution is.

---

### Docker (Deprecated repo, but historically instructive)

- **Problem statement:** N/A (repo is deprecated/archived)
- **Note:** This repo now redirects to moby/moby and docker/cli. Original Docker README (when it was category-defining) led with "An open platform for distributed applications for developers and sysadmins" and a diagram showing containers vs VMs.

**Key insight:** Docker's original README was famous for the **visual diagram** explaining containers. When establishing a new category, visuals > text.

---

### Terraform (Infrastructure as Code)

- **Problem statement:** "Terraform is a tool for building, changing, and versioning infrastructure safely and efficiently. Terraform can manage existing and popular service providers as well as custom in-house solutions."
- **Time-to-value:** Below feature list (no immediate code example in main README)
- **Section map:**
  - Logo → Brand
  - Links (website, forums, docs, tutorials, certification) → Navigation
  - Problem statement → Problem positioning
  - Key features (Infrastructure as Code, Execution Plans, Resource Graph, Change Automation) → Feature categorization
  - "What is Terraform?" link → Deeper education
  - Getting Started & Documentation → Reference
  - Developing Terraform → Contributor onboarding
- **Comparison handling:** None — establishes "Infrastructure as Code" category, no comparisons
- **Social proof:** Certification exam, HashiCorp Learn platform, commercial backing (HashiCorp)
- **Primary CTA:** Visit documentation site

**Key insight:** Terraform README is **extremely minimal** — it's a gateway to external docs, not the docs itself. Feature list uses **conceptual names** ("Execution Plans", "Resource Graph") that require explanation.

---

### FastEndpoints (ASP.NET alternative)

- **Problem statement:** "FastEndpoints is a developer friendly alternative to Minimal APIs & MVC"
- **Time-to-value:** Immediate — positions against known solutions in tagline
- **Section map:**
  - Badges (license, version, downloads, Discord) → Trust signals
  - Tagline "ASP.NET Minimal APIs Made Easy..." → Hook
  - Problem statement → Positioning
  - REPR pattern link → Education
  - Performance claim + benchmark link → Proof
  - Documentation link → Reference
  - Sponsors section → Social proof
- **Comparison handling:** **Direct and explicit** — "alternative to Minimal APIs & MVC", performance benchmarks against both
- **Social proof:** Download count, Discord, sponsors
- **Primary CTA:** "Visit the official website for detailed documentation"

**Key insight:** FastEndpoints uses **direct competitive positioning** successfully. When you're competing in an established space, explicit comparison works. The README is lean because it assumes you already understand ASP.NET.

---

### Bun (Node.js alternative)

- **Problem statement:** "Bun is an all-in-one toolkit for JavaScript and TypeScript apps. It ships as a single executable called `bun`. At its core is the *Bun runtime*, a fast JavaScript runtime designed as **a drop-in replacement for Node.js**."
- **Time-to-value:** Immediate — code example in section 2 (install), example commands in section 3
- **Section map:**
  - Logo → Brand
  - Discord/stars/speed badges → Social proof
  - "Read the docs" link → Reference
  - "What is Bun?" → Problem positioning
  - Install section with multiple options → Quickstart
  - Quick links (extensive bulleted list) → Navigation
  - Guides section → Education
- **Comparison handling:** **Direct replacement claim** — "drop-in replacement for Node.js", speed badge, performance positioning
- **Social proof:** Star count, Discord, speed claim
- **Primary CTA:** Install and run

**Key insight:** Bun doesn't apologize for being a Node competitor — it **leads with the speed claim** and "drop-in replacement" positioning. The README is organized as a **table of contents** to massive external docs.

---

### Deno (Node.js alternative)

- **Problem statement:** "Deno (/ˈdiːnoʊ/, pronounced `dee-no`) is a JavaScript, TypeScript, and WebAssembly runtime with secure defaults and a great developer experience. It's built on V8, Rust, and Tokio."
- **Time-to-value:** Immediate — install in section 1, first program in section 2
- **Section map:**
  - Logo + pronunciation guide → Brand
  - Badges (crates.io, Twitter, Discord, YouTube) → Social proof
  - Problem statement → Positioning
  - Installation (6 different methods) → Quickstart
  - "Your first Deno program" → Education
  - Additional resources → Reference
  - Contributing → Community CTA
- **Comparison handling:** Implicit — doesn't mention Node.js, but "secure defaults" and "great developer experience" are known Node pain points
- **Social proof:** Social media badges, community size signals
- **Primary CTA:** Install and run first program

**Key insight:** Deno **never mentions Node.js** in the README, but the positioning ("secure defaults", "great developer experience") is anti-Node without saying it. This is positioning through **problem correction**, not comparison.

---

### TypeScript (Language)

- **Problem statement:** "TypeScript is a language for application-scale JavaScript. TypeScript adds optional types to JavaScript that support tools for large-scale JavaScript applications for any browser, for any host, on any OS. TypeScript compiles to readable, standards-based JavaScript."
- **Time-to-value:** Below problem statement (Installation section)
- **Section map:**
  - Badges → Trust signals
  - Problem statement → Positioning
  - Links (playground, blog, Twitter) → Engagement
  - Community link → Social proof
  - Installing → Quickstart
  - Contribute → Community CTA
  - Documentation → Reference
  - Roadmap → Transparency
- **Comparison handling:** None — positions as "JavaScript with types", not vs. JavaScript
- **Social proof:** Download badges, community page, Microsoft backing (implicit)
- **Primary CTA:** Try the playground, then install

**Key insight:** TypeScript README is **minimal** because TypeScript is mature and well-known. New tools can't replicate this — it's earned through adoption.

---

### Axios (HTTP client)

- **Problem statement:** "Promise based HTTP client for the browser and node.js"
- **Time-to-value:** Immediate — installation options, then example code in same section
- **Section map:**
  - Sponsor sections (Platinum, Gold) → Funding/sustainability signals
  - Logo + tagline → Brand
  - Badges (extensive) → Trust signals
  - Table of Contents → Navigation
  - Features list → Capability communication
  - Browser support matrix → Compatibility
  - Installing → Quickstart
  - Example → Education
  - Extensive API reference (in README) → Reference
- **Comparison handling:** None — established as de facto standard
- **Social proof:** Download count, install size, bundle size, contributors, sponsors
- **Primary CTA:** Install and use

**Key insight:** Axios README is a **complete API reference**. This works because Axios is simple — the entire API fits in one README. Complexity requires external docs.

---

## Synthesis

### Optimal Section Order for Adoption

Based on the evaluation journey mapping:

1. **Logo + one-sentence hook** (awareness)
2. **Problem statement** (evaluation — match to developer's mental model)
3. **Social proof badges** (evaluation — credibility check)
4. **Installation** (trial decision)
5. **Quickstart code** (trial decision — must be <20 lines)
6. **Key differentiators** (evaluation — why this over alternatives)
7. **Feature overview or categorization** (evaluation — capability check)
8. **Link to comprehensive docs** (adoption)
9. **Community/contributing** (adoption)

**Anti-pattern:** Leading with badges, then installation, then problem statement. Developers can't evaluate installation relevance without understanding the problem first.

---

### Above-the-Fold Requirements

Must be visible without scrolling:

1. **Logo**
2. **One-sentence hook** (what is this?)
3. **Problem statement** (what problem does it solve?)
4. **Primary badge cluster** (downloads, version, build status)
5. **Installation command** (if one-liner exists)

**Nice-to-have:** Quick example (if ≤10 lines)

**Anti-pattern:** Long sponsor lists, extensive badge walls, or navigation before problem statement.

---

### Handling Comparisons

Three successful patterns observed:

#### 1. **Implicit differentiation** (xstate, Polly, Temporal)
- **When to use:** You're establishing a new category
- **How:** Lead with category name ("durable execution", "resilience strategies", "statecharts"), then educate on the category
- **Risk:** Developers don't recognize the problem you solve
- **Mitigation:** "Why?" section that explains the problem space

#### 2. **Direct positioning** (FastEndpoints, Bun)
- **When to use:** You're competing in an established space with clear incumbents
- **How:** Name the competitor in the tagline, provide benchmarks/comparisons
- **Risk:** Seen as derivative or confrontational
- **Mitigation:** Back claims with data, focus on measurable differences

#### 3. **Problem-correction positioning** (Deno)
- **When to use:** You're solving known pain points of an incumbent
- **How:** Describe your strengths in terms that highlight competitor weaknesses without naming them
- **Risk:** Too subtle, developers miss the positioning
- **Mitigation:** Use vocabulary that clearly maps to known problems

**For Precept:** Pattern #1 (implicit differentiation) is correct. Precept establishes "domain integrity" as a category. The README should educate on what domain integrity is, not list features xstate/FluentValidation/Stateless don't have.

**Comparison table placement:** If needed, put it **below** the quickstart, never above. Let developers understand the tool first, then compare.

---

### Minimum Path to First Working File

Observed patterns from successful tools:

**Single-command install + single-file example:**
- xstate: `npm install xstate` → 15-line machine definition
- Deno: `curl install | bash` → 3-line server
- Bun: `curl install | bash` → `bun run index.tsx`

**For Precept:**
1. `dotnet add package Precept` (if published) **OR** NuGet install
2. Create `Order.precept` with 10-line state machine
3. Execute validation in 3 lines of C# **OR** use CLI: `precept validate Order.precept`

**Critical:** The first example must be **domain-relevant**, not "Hello World". Developers evaluate tools by imagining their use case mapped to the example.

**Anti-pattern:** Multi-step setup (install CLI, install extension, configure workspace, create project, add reference, configure build). Every step is a drop-off point.

---

### What Precept Needs That Others Don't Have

Category-defining tools in our study had:

1. **Educational content** (xstate "Why?", Temporal video, Terraform "What is...")
2. **Conceptual vocabulary** (Polly's reactive/proactive, xstate's actor model)
3. **Visual aids** (xstate statechart diagrams, Docker container diagrams)

**What Precept has that no comparable tool mentions:**

1. **AI-native tooling** (MCP server, Claude integration, Copilot plugin)
2. **Language server / IDE integration** (diagnostics, hover, completion)
3. **Live preview** (VS Code extension preview pane)
4. **Catalog infrastructure** (centralized precept registry — future)

**README opportunity:** Most tools bury their tooling story. Precept can **lead with tooling** as a differentiator:

> "Precept is a domain integrity DSL with AI-native tooling. Write state machines and constraints in `.precept` files, get real-time diagnostics in VS Code, and let Claude reason about your domain model through MCP."

This positions Precept as **tooling-first, DSL-second** — which is actually the adoption path (developers try the extension, discover the DSL).

---

## Recommendations for Precept README Revamp

### 1. **Problem Statement**
Current README lacks a clear problem statement. Study Polly's approach:

**Before:** "Precept is a DSL for..."
**After:** "Precept is a domain integrity engine that binds an entity's state, data, and business rules into a single executable contract."

### 2. **Category Definition**
We're establishing "domain integrity" as a category. The README must teach this concept:

- Add a "Why?" or "What is domain integrity?" section
- Explain the problem: scattered validation, split state machines, fragmented business rules
- Position Precept as the unified solution

### 3. **Tooling as Differentiation**
Lead with tooling story, not DSL syntax:

- Screenshot of VS Code extension with hover/diagnostics
- MCP tool demonstration (Claude using precept_fire)
- Live preview pane showing state machine visualization

### 4. **Quickstart Path**
Current README has no quickstart. Needs:

```markdown
## Quick Start

Install the VS Code extension:
1. Search "Precept" in Extensions
2. Create `Order.precept`
3. Start typing — you'll get completions and diagnostics

Or use the NuGet package:
```csharp
dotnet add package Precept
```

See [Getting Started](docs/getting-started.md) for full walkthrough.
```

### 5. **Comparison Strategy**
Use **implicit differentiation**. Don't mention xstate, FluentValidation, or Stateless by name. Instead:

- "Unlike separate state machine and validation libraries, Precept unifies state, data, and rules"
- Feature comparison table (if needed) goes **below** quickstart, labeled "How Precept Compares"

### 6. **Social Proof**
Precept is new — can't compete on download counts. Use:

- GitHub stars (once published)
- "Featured in Claude Marketplace" (once available)
- Sample precept count (e.g., "20+ sample domain models included")
- Test count (666 tests — shows maturity)

### 7. **Section Order**
Proposed structure:

1. Logo + tagline
2. Problem statement (1-2 sentences)
3. Key differentiators (tooling + unification)
4. Installation/Quick Start
5. Simple example (10-line Order.precept)
6. "What is domain integrity?" (educational)
7. Feature overview (DSL + runtime + tooling)
8. Documentation link
9. Contributing

---

## Appendix: Badge Strategy

Observed badge patterns:

**Trust signals:**
- Build status (CI passing)
- Version (latest release)
- Downloads (if high)
- Code coverage (if >80%)

**Community signals:**
- Stars (if >100)
- Discord/Slack (if active)
- Contributors (if >5)

**Quality signals:**
- OpenSSF Scorecard
- License
- Test count

**For Precept (immediate):**
- Build status
- License (MIT)
- Latest version (once published)

**For Precept (future):**
- Download count (once >1k)
- VS Code extension rating
- Claude Marketplace badge (custom)

---

## Final Thoughts

The most successful category-defining READMEs **teach a mental model first, show code second**. Precept's README should:

1. Define "domain integrity" as a problem
2. Position Precept as the solution
3. Show tooling as the adoption path
4. Provide quickstart that feels immediate
5. Link to deep docs for the invested

The README is not documentation — it's a **product landing page**. Every section should answer: "Why should I try this?"

