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

**Patterns at library scale**:
- **Lead with what it is, not what it does for you.** Libraries don't write marketing copy. They write technical descriptions.
- **Confidence through brevity.** The most downloaded libraries use the fewest words. Newtonsoft: 11 words. MediatR: 14 words.
- **One personality trait, max.** Dapper is casual. AutoMapper is self-aware. Serilog is professional. None try to be everything.
- **No buzzwords.** No library says "revolutionary," "game-changing," or "next-generation." They say "simple," "fast," "no dependencies."

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
