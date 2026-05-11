Brand positioning research — how developer tools define and defend their market position.

Purpose:
- Study how strong developer-tool brands articulate what they are, who they're for, and why they win.
- Extract positioning frameworks applicable to Precept.
- Ground recommendations in real product behavior, not aspirational claims.

Sources referenced:
- April Dunford, "Obviously Awesome" positioning methodology (aprildunford.com)
- Sequoia Capital business plan framework (sequoiacap.com/article/writing-a-business-plan)
- Linear Method principles (linear.app/method/introduction)
- Vercel, Stripe, HashiCorp, Temporal — observed positioning in the wild

---

## Positioning frameworks

### April Dunford's 5-component model (Obviously Awesome)

Dunford's methodology, used by hundreds of B2B tech companies, decomposes positioning into five elements that must be defined in sequence:

1. **Competitive alternatives** — What would customers do if your product didn't exist? Not just direct competitors, but the status quo: spreadsheets, custom code, manual processes.
2. **Unique attributes** — What features or capabilities do you have that alternatives lack?
3. **Value** — What benefit do those unique attributes enable for the customer?
4. **Target customer** — Who cares the most about that value? The best-fit customer.
5. **Market category** — What market frame makes your value obvious to the target customer?

The critical insight: positioning is not messaging. It's the *context* that makes messaging possible. You can't write good copy until you know what frame the product sits in.

### Sequoia Capital's positioning distillation

Sequoia's pitch framework demands:
- **Company purpose**: Define the company in a single declarative sentence.
- **Problem**: Describe the customer's pain and how it's addressed today.
- **Solution**: The eureka moment — what's unique and compelling.
- **Why now?**: What changed in the world that makes this the right time.

The emphasis is on *one sentence* for the company purpose. If you can't say it in one sentence, you haven't found it.

---

## How developer tools position themselves

### By category frame

**Platforms and tools (aspirational)**:

| Brand | Category frame | Why it works |
|-------|---------------|-------------|
| Stripe | "Financial infrastructure for the internet" | Names a category that didn't exist. Stripe isn't a "payment processor" — it's the rails everything runs on. |
| Vercel | "The frontend cloud" | Two words. Positions against both AWS (too complex) and Netlify (too simple). Owns the intersection of frontend + infrastructure. |
| Linear | "The issue tracking tool you'll enjoy using" | Reframes the category around developer experience. Positions against Jira (powerful but painful). |
| Temporal | "Durable execution" | Created a category term. "Durable execution" didn't exist before Temporal named it. Now competitors define themselves against it. |
| Terraform | "Infrastructure as code" | HashiCorp named the category then became synonymous with it. The three-word phrase IS the positioning. |
| Docker | "Containerization" | Same pattern — named the mechanism, became the category. |

**Open-source libraries (our actual scale)**:

| Library | Category frame | Why it works |
|---------|---------------|-------------|
| Serilog | "Structured logging for .NET" | Didn't say "logging library." Said "structured logging" — named the differentiator right in the tagline. Every competitor now defines themselves against structured vs. unstructured. |
| Polly | ".NET resilience and transient-fault-handling library" | Long but precise. Names two capabilities. "Resilience" is the aspirational word; "transient-fault-handling" is the practical one. |
| Dapper | "Simple micro-ORM" | Two adjectives that ARE the positioning: "simple" (vs. Entity Framework's complexity) and "micro" (vs. full ORM weight). The tagline is the value proposition. |
| FluentValidation | "Validation library for .NET that uses a fluent interface" | Names the mechanism ("fluent interface") as the differentiator. The name itself IS the positioning — "Fluent" + "Validation." |
| Newtonsoft.Json | "Popular high-performance JSON framework for .NET" | "Popular" and "high-performance" — social proof + technical claim in three words. Became so dominant the name was synonymous with JSON in .NET. |
| xUnit | "Free, open source, community-focused unit testing tool" | Three positioning words: free (vs. commercial), open source (vs. proprietary), community-focused (vs. vendor-driven). Written by NUnit's original author — lineage as positioning. |
| AutoMapper | "Getting rid of code that mapped one object to another" | Positions against a *task*, not a category. Doesn't say "mapping library" — says "the thing that kills boring mapping code." |
| MediatR | "Simple mediator implementation in .NET — in-process messaging with no dependencies" | "No dependencies" is the killer positioning phrase. In a world of bloated packages, zero-dependency is a trust signal. |

**Pattern at library scale**: The name often IS the positioning. Serilog = "Seri(ous) log(ging)." FluentValidation = "Fluent + Validation." Dapper = fast and neat. The strongest library brands pick a name that communicates the value proposition without needing a tagline.

**Pattern across all scales**: The strongest developer brands either *name a new category* or *redefine an existing one* by changing the evaluation criteria. At library scale, the name and the one-line description do all the positioning work — there's no brand page or marketing site to fall back on.

### By positioning archetype

Dunford identifies several category strategies:

1. **Head-to-head**: Compete directly in an existing category. Works when you're clearly better on the dimension customers already care about. (Example: Linear vs. Jira — same category, better experience.)

2. **Big fish, small pond**: Position in a subcategory you can dominate. (Example: Prisma — not "an ORM" but "the type-safe database toolkit for TypeScript.")

3. **Category creation**: Define a new category entirely. Highest risk, highest payoff. Requires educating the market. (Example: Temporal — "durable execution" needed explanation but now dominates.)

---

## Adjacent product positioning analysis

### Products that compete in Precept's neighborhood

| Product | How they position | Market frame |
|---------|------------------|-------------|
| Stately/XState | "Visual logic that runs" | State-machine-as-diagram. Visual-first. |
| Temporal | "Durable execution" | Workflow orchestration. Code-first. Infrastructure-grade. |
| Camunda | "Process orchestration" | Enterprise BPM. Low-code. |
| Prefect | "Workflow orchestration for data teams" | Data pipeline automation. Python-first. |
| FluentValidation | "Validation library for .NET" | Library. Technical utility. No workflow ambitions. |
| MediatR | "Simple mediator for .NET" | Library. In-process message routing. |

**Key observation**: No product occupies the "domain integrity engine" frame. The closest alternatives are:
- Custom code (switch statements, scattered validation)
- BPM engines (Camunda, etc.) — too heavy, visual-first
- State machine libraries (Stateless, XState) — state transitions only, no constraint enforcement
- Validation libraries (FluentValidation) — constraints only, no state management

### What Precept does differently than all of them

1. **Prevention, not detection.** Invalid states don't get caught — they're structurally impossible.
2. **One file, complete rules.** Guards, constraints, invariants, transitions in a single .precept file.
3. **Full inspectability.** Preview every possible action and its outcome without executing anything.
4. **Compile-time checking.** Unreachable states, dead ends, type mismatches caught at compile time.
5. **Designed for AI.** Deterministic semantics + structured tool APIs = AI can author and validate without human feedback.

---

## Positioning options for Precept

### Option A: Category creation — "Domain integrity engine"

- **Frame**: A new category for binding entity state, data, and business rules into a single executable contract.
- **Tagline direction**: "Business rules that hold."
- **Advantage**: No competitors in this exact frame. Owns the conversation.
- **Risk**: Requires category education. "Domain integrity engine" isn't self-explanatory.

### Option B: Subcategory — "The state machine that enforces your business rules"

- **Frame**: Positions against state-machine libraries (Stateless, XState) but adds constraint enforcement as the differentiator.
- **Tagline direction**: "State machines with teeth."
- **Advantage**: Familiar starting point. Developers understand state machines.
- **Risk**: Undersells — Precept is more than a state machine. May attract the wrong audience.

### Option C: Reframe existing pain — "The end of scattered business logic"

- **Frame**: Positions against the status quo (custom code scattered across services, ORMs, handlers).
- **Tagline direction**: "One file. Every rule. Structurally enforced."
- **Advantage**: Speaks to the pain point developers already feel. No category education needed.
- **Risk**: Diagnostic framing ("this thing you hate") vs. aspirational framing ("this thing you want").

### Option D: AI-native framing — "The contract AI can reason about"

- **Frame**: Positions around the AI-authoring workflow that makes Precept unique.
- **Tagline direction**: "Domain rules that AI can write, validate, and execute."
- **Advantage**: Timely. AI-native positioning is a strong "why now" signal.
- **Risk**: May alienate developers who want to write rules by hand.

---

## Sequoia-style single-sentence positioning

Following Sequoia's "define the company in a single declarative sentence":

Candidates:
1. "Precept is a .NET library that compiles business rules into an engine where invalid states are structurally impossible."
2. "Precept binds an entity's states, fields, and constraints into a single executable contract that prevents illegal mutations."
3. "Precept turns a plain-text business rule definition into a deterministic engine that AI can author and users can inspect."

The README currently uses: "Precept is a domain integrity engine for .NET."

---

## Positioning principles for Precept

Distilled from research:

1. **Name the mechanism, not the category.** "Domain integrity engine" is a mechanism, not a market category. Consider whether the brand leads with the mechanism (what it does) or the outcome (what you get).

2. **Anchor to the pain.** Developer positioning works best when it names a specific, recognizable pain. "Your business rules are scattered across six files" is more compelling than "we provide domain integrity."

3. **One sentence, then stop.** If the positioning takes a paragraph, it's not positioning — it's a pitch. The single sentence must survive being overheard at a conference.

4. **Let the word do work.** "Precept" literally means "a strict rule of action." This is a positioning asset. The name itself carries the brand promise.

5. **Position against the status quo, not competitors.** Precept's competitive set is "custom code" and "scattered logic," not other libraries. Positioning against specific products limits the market. Positioning against a universal pain expands it.
