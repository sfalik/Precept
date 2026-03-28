Voice and tone research — how developer tools and open-source libraries communicate.

Purpose:
- Study voice and tone strategies from GitHub libraries through platforms.
- Define frameworks for a consistent Precept voice.
- Ground in real README copy and documentation patterns, not abstract guidelines.

Sources referenced:
- Nielsen Norman Group, "The Four Dimensions of Tone of Voice" (nngroup.com/articles/tone-of-voice-dimensions)
- Linear Method principles (linear.app/method/introduction)
- Observed README copy from: Serilog, Polly, Dapper, FluentValidation, MediatR, AutoMapper, Newtonsoft.Json, xUnit
- Observed documentation voice from: Stripe, Vercel, Tailwind CSS, Prisma

---

## The four dimensions of tone (NN/g framework)

Kate Moran at Nielsen Norman Group identifies four measurable dimensions of voice:

1. **Formal ↔ Casual** — "We apologize for the inconvenience" vs. "Oops! Something broke."
2. **Serious ↔ Funny** — Attempting humor or not.
3. **Respectful ↔ Irreverent** — Treating the subject with gravity vs. playful dismissal.
4. **Matter-of-fact ↔ Enthusiastic** — Dry and direct vs. excited and energetic.

Key findings from NN/g research:
- Users *do* notice tone differences — statistically significant at p < 0.05.
- Different tones produce measurable differences in perceived friendliness and formality.
- The tone should match the user's emotional state and context. Error messages need a different tone than welcome screens.

For developer tools, the most effective quadrant is: **casual-ish, serious, respectful, matter-of-fact.** This is where Stripe, Linear, and most successful dev tools land.

---

## How libraries and tools actually sound

### Tier 1: GitHub libraries (our actual scale)

| Library | Opening line (README) | Voice character |
|---------|----------------------|----------------|
| Serilog | "Serilog is a diagnostic logging library for .NET applications. It is easy to set up, has a clean API, and runs on all recent .NET platforms." | **Matter-of-fact, professional, confident.** Three short claims. No exclamation marks. No "awesome" or "powerful." Trust through understatement. |
| Polly | "Polly is a .NET resilience and transient-fault-handling library that allows developers to express policies such as Retry, Circuit Breaker, Timeout, Rate-limiting, Hedging and Fallback in a fluent and thread-safe manner." | **Technical, precise, comprehensive.** One long sentence that names every capability. The completeness IS the confidence. |
| Dapper | "Dapper is a simple micro-ORM used to simplify working with ADO.NET; if you like SQL but dislike the boilerplate of ADO.NET: Dapper is for you!" | **Casual, direct, opinionated.** "If you like X but dislike Y" — positions the reader immediately. The exclamation mark is unusual at library scale but matches Dapper's personality (fast, unpretentious). |
| FluentValidation | "FluentValidation is validation library for .NET that uses a fluent interface and lambda expressions for building strongly-typed validation rules." | **Dry, technical, precise.** No personality. No opinion. Just the facts. This works because the library is so widely used it doesn't need to sell. |
| Newtonsoft.Json | "Json.NET is a popular high-performance JSON framework for .NET" | **Minimal.** Eleven words. "Popular" and "high-performance" do the positioning. No fluff. At 7.9 billion downloads, the number speaks louder than copy. |
| AutoMapper | "AutoMapper is a simple little library built to solve a deceptively complex problem — getting rid of code that mapped one object to another. This type of code is rather dreary and boring to write, so why not invent a tool to do it for us?" | **Conversational, self-aware, slightly playful.** "Simple little library," "dreary and boring" — this is a voice with personality. It acknowledges the problem is unglamorous and doesn't pretend otherwise. |
| MediatR | "Simple mediator implementation in .NET. In-process messaging with no dependencies." | **Austere. Zero waste.** Two sentence fragments. Fourteen words total. "No dependencies" is the value bomb dropped without ceremony. |
| xUnit | "xUnit.net is a free, open source, community-focused unit testing tool for the .NET Framework. Written by the original inventor of NUnit v2..." | **Credential-forward.** The second sentence establishes authority through lineage. "Written by the original inventor" is the most powerful thing in the paragraph. |

#### Expanded library voice catalog (24 additional)

**Similar scope to Precept** (state machines, workflows, rules engines):

| Library | Opening line (README) | Voice character |
|---------|----------------------|----------------|
| Stateless (6.1k ⭐) | "Create state machines and lightweight state machine-based workflows directly in .NET code." | **Imperative, practical, zero preamble.** Starts with a verb — "Create" — not "Stateless is." No self-description at all. Jumps straight to a code sample of a phone call state machine. The README IS the documentation. |
| Elsa Workflows (7.7k ⭐) | "Elsa is a powerful workflow library that enables workflow execution within any .NET application." | **Ambitious, platform-pitched.** "Powerful" is rare at library scale — most avoid superlatives. The README reads like a product page: Docker quickstart, screenshots, feature matrix, roadmap, professional support. This is a library with platform aspirations. |
| Workflow Core (5.8k ⭐) | "Workflow Core is a light weight embeddable workflow engine targeting .NET Standard. Think: long running processes with multiple tasks that need to track state." | **Practical, explanatory.** "Think: long running processes..." is a rare mid-sentence reframe. It tells you *when* you need it, not just *what* it is. The colon-based aside adds conversational warmth to an otherwise technical opening. |
| NRules (1.6k ⭐) | "NRules is an open source production rules engine for .NET, based on the Rete matching algorithm. Rules are authored in C# using internal DSL." | **Academic, precise.** "Rete matching algorithm" — names the formal CS foundation immediately. "Internal DSL" is jargon aimed at the right audience. This voice says: "if you know what Rete is, this is for you." |
| MassTransit (7.7k ⭐) | "MassTransit is a free, open-source distributed application framework for .NET. MassTransit makes it easy to create applications and services that leverage message-based, loosely-coupled asynchronous communication for higher availability, reliability, and scalability." | **Aspirational-technical.** Two sentences that escalate: identity → value proposition. "Higher availability, reliability, and scalability" is enterprise vocabulary. The voice is too big for "library" but earned by its ecosystem (30+ NuGet packages). |

**General .NET libraries** (broader ecosystem):

| Library | Opening line (README) | Voice character |
|---------|----------------------|----------------|
| Moq (6.4k ⭐) | "The most popular and friendly mocking library for .NET." | **Bold claim, warm personality.** "Most popular" is a superlative most libraries avoid. "Friendly" is the personality — unusual and deliberate. The full README doubles down with "WOW! No record/replay weirdness?! :)" in a code comment. This is a library with attitude. |
| Bogus (9.6k ⭐) | "Hello. I'm your host Brian Chavez. Bogus is a simple fake data generator for .NET languages like C#, F# and VB.NET." | **Personal, warm, host-mode.** "Hello. I'm your host" — no other library opens this way. The README is written as personal address from the author, with emoji (⭐😄💫💪), tip jars, and a featured-in section. Maximum personality at library scale. |
| Refit (9.4k ⭐) | "Refit is a library heavily inspired by Square's Retrofit library, and it turns your REST API into a live interface." | **Heritage-forward, then a hook.** "Heavily inspired by Square's Retrofit" — credits the ancestor immediately. "Turns your REST API into a live interface" is a memorable one-line pitch. The voice respects lineage while claiming a new territory (.NET). |
| Humanizer (9.6k ⭐) | "Humanizer meets all your .NET needs for manipulating and displaying strings, enums, dates, times, timespans, numbers and quantities." | **Comprehensive-list, single sentence.** Like Polly, the completeness IS the value claim — six data types enumerated. "Meets all your needs" is confident but earned: the library really does handle all of those types. The README then demonstrates each with inline examples. |
| Spectre.Console (11.3k ⭐) | "A .NET library that makes it easier to create beautiful, cross platform, console applications." | **Aesthetic-forward.** "Beautiful" is the operative word — unusual in .NET library copy. The voice signals that this library cares about appearance, not just function. "Cross platform" is understated, tucked in as a comma-separated aside. |
| CsvHelper (5.2k ⭐) | "A library for reading and writing CSV files. Extremely fast, flexible, and easy to use." | **Classic three-adjective formula.** "Extremely fast, flexible, and easy to use" — the same pattern you'd find on a product box. No library name in the opening line. The indefinite article "A library" rather than "CsvHelper is" creates a subtle humility. |
| RestSharp (9.8k ⭐) | "RestSharp is a lightweight HTTP API client library. It's a wrapper around HttpClient, not a full-fledged client on its own." | **Honest self-limitation.** "Not a full-fledged client on its own" — actively constrains expectations. This is radical honesty at library scale. The voice says: "I'm a wrapper. I know I'm a wrapper. Here's what I add." |
| Quartz.NET (7k ⭐) | "Please visit https://www.quartz-scheduler.net/ for up to date news and documentation." | **Redirect, no pitch.** The README contains almost no descriptive copy — just badges, a compatibility note, and a docs link. The voice is: "we don't sell ourselves here; go to the docs." This is the extreme end of the anti-marketing spectrum. |
| Mapster (5.1k ⭐) | "Writing mapping methods is a machine job. Do not waste your time, let Mapster do it." | **Imperative, opinionated.** "Do not waste your time" — commands the reader. This is the Auto Mapper challenger voice: confident it's better, not diplomatic about it. The subtitle "The Mapper of Your Domain" reinforces the challenger positioning. |
| Hangfire (9.3k ⭐) | "An easy way to perform background processing in .NET and .NET Core applications. No Windows Service or separate process required." | **Problem-solution in two sentences.** The first is the value; the second removes the objection ("No Windows Service"). The voice anticipates the reader's doubt and answers it immediately. |
| BenchmarkDotNet (10.6k ⭐) | "Powerful .NET library for benchmarking." | **Five words.** The shortest opening in this entire catalog. "Powerful" is the only adjective. At 10.6k stars, the brevity is the confidence. The README then shifts entirely to output examples — the benchmark tables ARE the voice. |
| FluentAssertions (3.8k ⭐) | "A very extensive set of extension methods that allow you to more naturally specify the expected outcome of a TDD or BDD-style unit tests." | **Methodological, precise.** "More naturally specify" — the word "naturally" is doing the brand work. It implies the alternatives are unnatural. "TDD or BDD-style" respects both camps without choosing. |
| Shouldly (2.1k ⭐) | "Shouldly is an assertion framework which focuses on giving great error messages when assertions fail while being simple to use." | **Benefit-first.** "Great error messages when assertions fail" — names the specific differentiator before anything else. Most assertion libraries describe what they do; Shouldly describes what goes wrong (better) when tests fail. |
| NSubstitute (2.7k ⭐) | "NSubstitute is a friendly substitute for .NET mocking libraries." | **Pun-as-positioning.** "Friendly substitute" — the word "substitute" is both the technical term (test double) and the competitive claim (replaces other mocking libraries). The double meaning is deliberate and memorable. |
| Scrutor (3.8k ⭐) | "Assembly scanning and decoration extensions for Microsoft.Extensions.DependencyInjection." | **Pure technical descriptor.** No personality, no opinion, no claims. Just: what it is, for what framework. This is the voice of a utility that knows it's a utility. |
| Verify (2.8k ⭐) | "Verify is a snapshot tool that simplifies the assertion of complex data models and documents." | **Precise, tool-positioned.** "Snapshot tool" is the category; "simplifies the assertion of complex data" is the value. Understated and specific. The README then shifts to extensive usage examples. |
| ErrorOr (3.2k ⭐) | "A simple, fluent discriminated union of an error or a result." | **Academic-simple hybrid.** "Discriminated union" is a computer science term; "simple" and "fluent" soften it. The voice bridges functional programming theory with .NET pragmatism. |
| OneOf (3.6k ⭐) | "Easy to use F#-like discriminated unions for C# with exhaustive compile time matching." | **Cross-language positioning.** "F#-like" is the key phrase — it positions OneOf as bringing a feature from one language to another. "Exhaustive compile time matching" is the technical differentiator that appeals to type-safety fans. |
| Ardalis.GuardClauses (3.1k ⭐) | "A simple extensible Guard Clause library." | **Five words, maximum efficiency.** Like BenchmarkDotNet, the brevity is the confidence. "Simple" and "extensible" are the two selling points. The voice trusts that "guard clause" is self-explanatory to the target audience. |
| StronglyTypedId (1.5k ⭐) | "An easy way to avoid mixing up Ids." | **Problem-statement-as-pitch.** Eight words that describe the problem, not the solution. The voice assumes you've experienced the bug (passing a CustomerId where an OrderId was expected) and speaks directly to that pain. |

#### Expanded patterns at library scale

From the full 32-library catalog, the original four patterns hold and five new ones emerge:

**Patterns at library scale** (original four):
- **Lead with what it is, not what it does for you.** Libraries don't write marketing copy. They write technical descriptions.
- **Confidence through brevity.** The most downloaded libraries use the fewest words. Newtonsoft: 11 words. MediatR: 14 words. BenchmarkDotNet: 5 words.
- **One personality trait, max.** Dapper is casual. AutoMapper is self-aware. Serilog is professional. None try to be everything.
- **No buzzwords.** No library says "revolutionary," "game-changing," or "next-generation." They say "simple," "fast," "no dependencies."

**New patterns from the expanded catalog**:
- **The honest self-limitation.** RestSharp ("not a full-fledged client"), MediatR ("unambitious"), Ardalis.GuardClauses ("simple") — constraining expectations builds trust faster than inflating them.
- **Problem-as-pitch.** StronglyTypedId ("avoid mixing up Ids") and Hangfire ("No Windows Service required") — naming the pain is more memorable than naming the feature.
- **Heritage-forward.** Refit credits Retrofit. xUnit credits NUnit v2's inventor. Quartz.NET is a .NET port of Java's Quartz. Naming ancestors signals maturity and proven ideas, not novel risk.
- **Code-as-voice.** Stateless, Bogus, Moq, and Spectre.Console let their README code samples do the talking. The voice IS the API — a well-designed fluent API is more persuasive than any description.
- **The challenger.** Mapster ("Do not waste your time"), OneOf ("F#-like for C#"), NSubstitute ("friendly substitute") — these are explicit or implicit replacements for established alternatives. The voice is confident without being hostile.

**Cross-cutting observation: Precept's closest peers**

Among the 32 libraries, Precept's closest voice peers are:
- **Stateless** — same domain (state machines), similar scope, lets code lead
- **NRules** — same DSL-driven approach, academic-precise voice
- **Workflow Core** — same "long running processes" framing, practical explainer voice
- **FluentValidation** — same "rules as code" concept, dry technical precision

Precept's current voice ("A domain integrity engine for .NET") is most aligned with **Serilog's professional confidence** combined with **NRules' precision** and **Stateless' code-first approach**. This is a strong combination.

### Tier 2: Developer tools (more brand investment)

| Tool | Voice character | Example |
|------|----------------|---------|
| Tailwind CSS | Practical, opinionated, slightly warm. Confident in its approach but not arrogant. | "A utility-first CSS framework packed with classes like flex, pt-4, text-center and rotate-90 that can be composed to build any design, directly in your markup." |
| Prisma | Clear, methodical, developer-empathetic. | "Prisma provides the best experience for your team to work and interact with databases." |
| Next.js | Terse, authoritative, no-frills. | "The React Framework for the Web. Used by some of the world's largest companies, Next.js enables you to create high-quality web applications with the power of React components." |
| Astro | Friendly, enthusiastic, slightly informal. | "Astro is the web framework for building content-driven websites like blogs, marketing, and e-commerce." |

**Pattern at tool scale**: Slightly more personality than libraries, but still technical-first. The voice matches the product personality — Tailwind is practical, Next.js is austere, Astro is friendly.

### Tier 3: Platforms (aspirational)

| Platform | Voice character | What makes it work |
|----------|----------------|-------------------|
| Stripe | Precise but warm. Explains complex concepts without condescension. Every paragraph is edited to within an inch of its life. | Stripe's docs are the gold standard because they respect the reader's intelligence while leaving nothing ambiguous. The voice says "you're smart, but this is complex, so here's exactly what you need." |
| Linear | Sparse, confident, opinionated. Principles are stated as facts, not suggestions. | "Productivity software needs to be designed for purpose." Not "we believe that..." — just states it. The absence of hedging IS the brand voice. |
| Vercel | Developer-native. Assumes competence. Gets to the code fast. | Geist docs: "Learn how to work with our color system." Five words of preamble, then the content. No warm-up. |

---

## Voice and tone principles for Precept

### The Precept voice (proposed)

Based on what the product actually does and how the best libraries at our scale communicate:

**Tone coordinates** (on the NN/g dimensions):
- **Formal ↔ Casual**: Slightly casual. Not stiff, not slangy. Contractions are fine ("it's," "you'll," "doesn't").
- **Serious ↔ Funny**: Serious. No jokes. Humor in developer documentation is high-risk and ages badly.
- **Respectful ↔ Irreverent**: Respectful. The subject (business rules, domain integrity) deserves gravity.
- **Matter-of-fact ↔ Enthusiastic**: Matter-of-fact. Let the product's properties speak. Don't oversell.

**One-word voice**: **Authoritative.**

Not aggressive, not cold — authoritative the way a well-written legal code is authoritative. It states facts. It doesn't hedge. It doesn't oversell. It respects the reader's time and intelligence.

### Voice guidelines

1. **Lead with what it IS.** "Precept is a domain integrity engine for .NET." Not "Precept helps you build better software" or "Imagine a world where..."

2. **State, don't claim.** "Invalid states are structurally impossible" (verifiable) vs. "Precept makes your code bulletproof" (marketing).

3. **Use concrete nouns and verbs.** "The engine rejects the transition" vs. "The system prevents bad things from happening."

4. **Respect the word's meaning.** "Precept" means a strict rule of action. The voice should feel like a precept — clear, binding, no ambiguity.

5. **Match MediatR's economy.** If you can say it in 14 words, don't use 40. Brevity is confidence.

6. **Match Serilog's professionalism.** Serilog sounds like the kind of library you'd trust in production. That's the target.

7. **Match AutoMapper's self-awareness — but only in the right context.** It's OK to acknowledge that "scattered business logic" is a universal pain. But don't be cute about it.

### Voice do's and don'ts

| Do | Don't |
|----|-------|
| "Precept compiles business rules into an engine." | "Precept supercharges your business logic!" |
| "Invalid states are structurally impossible." | "Say goodbye to bugs forever!" |
| "The guard passes, and the state changes." | "Like magic, your entity transforms!" |
| "Same definition + same data = same outcome." | "Precept guarantees 100% reliability!" |
| "One file. Every rule." | "All your rules in one convenient place!" |
| "Inspect previews the outcome without mutating." | "Inspect lets you safely peek into the future!" |

### Tone variation by context

| Context | Tone adjustment |
|---------|----------------|
| README opening | Slightly more assertive. This is the first impression. |
| API documentation | Pure matter-of-fact. Technical precision. |
| Error messages / diagnostics | Helpful, direct. Name the problem and the fix. |
| Sample file comments | Brief. Explain the "why," not the "what." |
| VS Code extension descriptions | Concise. Marketplace copy is read fast. |
| Blog / announcement (if ever) | Slightly warmer. Can be more conversational. |

---

## What the README already gets right

The current README opening is strong:

> "Precept is a domain integrity engine for .NET. It binds an entity's state, data, and business rules into a single, executable contract. By treating your business constraints as unbreakable precepts, the engine ensures that invalid states and illegal data mutations are fundamentally impossible."

This follows the patterns of the most successful libraries:
- Leads with what it is (Serilog pattern)
- Uses precise technical language (Polly pattern)
- Makes a bold claim backed by the mechanism (MediatR's "no dependencies" pattern)
- Three sentences, each doing distinct work: identity → mechanism → guarantee

The word "unbreakable" is the one risk — it's a marketing word in a technical paragraph. "Enforced" or "structural" might hold up better under scrutiny.
