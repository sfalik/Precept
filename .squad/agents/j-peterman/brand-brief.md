# Precept Brand Brief
*J. Peterman — working reference document. Last synthesized: 2026-04-05.*

---

## 1. Product

### What Precept Is

A .NET library that compiles a plain-text `.precept` definition — an entity's states, fields, events, guards, and constraints — into an immutable, deterministic engine. Four operations: `CreateInstance`, `Inspect`, `Fire`, `Update`. Same definition + same data = same outcome. Always.

The engine does not catch invalid states after the fact. It makes them structurally impossible. That is the entire story.

### Who It's For

.NET backend engineers building systems where business rules matter — order processing, loan applications, user onboarding, workflow automation. The target is the developer who currently has guards scattered across six services, validation in the ORM layer, and invariants in handlers, and who feels the cost every time those get out of sync.

### The Single Positioning Sentence (LOCKED)

> "Precept is a domain integrity engine for .NET — a single declarative contract that binds state, data, and business rules into an engine where invalid states are structurally impossible."

The AI-native frame ("the contract AI can reason about") is the *why now*, not the primary identity. It lives in supporting copy, not the opening line.

### The Name

"Precept" means a strict rule or principle of action. It is not a metaphor — it is a description. The name is the story.

### Narrative Arc (README/pitch)

1. One sentence: what it is (resolution)
2. One sentence: why it's different (implicit conflict)
3. One code sample: proof
4. Feature list: comprehensiveness signal

This matches the pattern of Serilog, Polly, and the strongest library-scale narratives. No manifesto. No "imagine a world where."

---

## 2. Voice & Tone

### The Register

**Authoritative with warmth.** The voice of a well-written legal code — clear, binding, no ambiguity — but not cold.

| Dimension | Position | Notes |
|-----------|----------|-------|
| Formal ↔ Casual | Slightly casual | Contractions fine. Brief explanatory asides allowed. |
| Serious ↔ Funny | Serious | No jokes. Developer humor ages badly. |
| Respectful ↔ Irreverent | Respectful | Domain integrity deserves gravity. |
| Matter-of-fact ↔ Enthusiastic | Matter-of-fact | Let the properties speak. Don't oversell. |

### Models

- **Serilog** — professionalism, confidence through plain statement
- **MediatR** — economy (14 words total, "no dependencies" dropped without ceremony)
- **Workflow Core** — clarifying warmth ("Think: long running processes with multiple tasks…")

### Do / Don't (LOCKED)

| ✅ Do | ❌ Don't |
|-------|---------|
| "Precept compiles business rules into an engine." | "Precept supercharges your business logic!" |
| "Invalid states are structurally impossible." | "Say goodbye to bugs forever!" |
| "Think: scattered validation across six services — Precept puts it in one file." | "Imagine a world where…" |
| "Same definition + same data = same outcome." | "Precept guarantees 100% reliability!" |
| "One file. Every rule." | "All your rules in one convenient place!" |
| "Inspect previews the outcome without mutating." | "Inspect lets you safely peek into the future!" |
| "The engine rejects the transition." | "The system prevents bad things from happening." |

### Tone Variation by Context

| Context | Adjustment |
|---------|-----------|
| README opening | Slightly more assertive. First impression. |
| API documentation | Pure matter-of-fact. Technical precision. |
| Error messages / diagnostics | Helpful, direct. Name the problem and the fix. |
| Sample file comments | Brief. Explain the *why*, not the *what*. |
| VS Code marketplace copy | Concise. Scanned, not read. |

### What to Never Say

- "Supercharge," "revolutionize," "game-changing," "next-generation"
- "Imagine a world where…"
- "100% reliable / bulletproof / foolproof"
- Anything that requires the reader to imagine a scenario before making the claim

---

## 3. Visual Language

### Primary Visual Surface: The DSL

The `.precept` file IS the brand. Like Prisma's `.prisma` schema, the code is the hero image. A `.precept` file has a distinctive left-edge pattern — every line starts with `field`, `state`, `event`, `from`, `invariant`, `on` — that is recognizable before you read the content. This is Precept's most powerful visual language asset.

### Color Palette (LOCKED — 2026-04-03)

**Background:** `#0c0c0f` — near-black with slight warm undertone. All contrast ratios computed against this.

**8-shade authoring system:**

| Family | Hex | Typography | Role |
|--------|-----|------------|------|
| Structure · Semantic | `#4338CA` | **bold** | `precept`, `field`, `state`, `event`, `invariant`, `from`, `on`, `in`, `to`, `set`, `transition`, `edit`, `assert`, `reject`, `when`, `no` |
| Structure · Grammar | `#6366F1` | normal | `as`, `with`, `default`, `nullable`, operators, punctuation glue, `->`, `=` |
| States | `#A898F5` | normal / *italic if constrained* | All state names; italic = participates in `in/to/from X assert` |
| Events | `#30B8E8` | normal / *italic if constrained* | All event names; italic = has `on X assert` |
| Data · Names | `#B0BEC5` | normal / *italic if guarded* | Field and argument names; italic = referenced by `invariant` |
| Data · Types | `#9AA8B5` | normal | `string`, `number`, `boolean`, collection types |
| Data · Values | `#84929F` | normal | Literals: `true`, `false`, `null`, strings, numbers |
| Rules · Messages | `#FBBF24` | normal | Message strings in `because` / `reject` |

**Comments:** `#9096A6` italic — outside the semantic palette by design.

**Runtime verdicts (never in syntax highlighting):**

| | Hex | Role |
|--|-----|------|
| Enabled | `#34D399` | Valid, success |
| Blocked | `#F87171` | Rejected, violated |
| Warning | `#FDE047` | Unmatched, warning |

### The Two Design Principles Behind the Color System

**1. Semantic color as brand.** Every color exists because the compiler knows something about the code and needs to say it. Nothing is decorative. The system has two dimensions: *what kind of thing* (Structure / States / Events / Data / Rules each get a lane) and *constraint signal* (italic means "this token is under rule pressure").

**2. Typography as a second channel.** Bold = structural semantic keywords (behavioral drivers). Italic = constrained or guarded. Normal = everything else. Color + weight together carry more information than either alone.

**The hue map:**
```
45° Gold — 195° Cyan — 215° Slate — 239° Indigo — 260° Violet
```
Gold is the only warm hue. It is reserved exclusively for human-readable message strings — the visual interrupt that says "this is what the rule says to the user."

### Brand Color

`#6366F1` (Tailwind indigo-500) — "the Precept color." Used in the brand mark, NuGet badge, diagnostic code prefixes, and diagram framing.

### Typography (LOCKED)

**Brand font:** Cascadia Cove (Cascadia Code fallback). Monospace. Editor-native.

**Wordmark:** Cascadia Cove 700, `font-variant: small-caps`, `letter-spacing: 0.1em`

Small caps is the typographic convention for defined terms, legal codes, and axioms. The wordmark says "defined concept" before you read the word.

**Code font:** Cascadia Cove 400–700, normal case. Same family — the code grows out of the wordmark.

```
PRECEPT           ← Cascadia Cove 700, small-caps, 0.1em tracking
precept LoanApplication  ← Cascadia Cove 400–700, normal case
```

### Secondary Visual Surface: State Diagrams

The VS Code preview panel generates state diagrams using the same hue families as syntax highlighting. Shape (not lifecycle tint) carries initial/final structure. Node borders use Indigo. State names use Violet. Transition arrows use Indigo Grammar. Event labels use Cyan.

### What Makes Precept Visually Recognizable

1. The left-edge keyword pattern of `.precept` files
2. The indigo-dominant syntax palette with cyan event names and gold rule messages
3. The small-caps wordmark in Cascadia Cove
4. Italic as constraint signal — not color, not decoration

---

## 4. Competitive Positioning

### Category

**Domain integrity engine.** No competitor occupies this frame. Category-creation play: define the category, become synonymous with it. Analogues: Temporal ("durable execution"), Docker ("containerization"), Terraform ("infrastructure as code").

### The Adjacent Landscape

| Product | Frame | How Precept Differs |
|---------|-------|---------------------|
| Stately / XState | "Visual logic that runs" | Visual-first, state transitions only, no constraint enforcement |
| Temporal | "Durable execution" | Workflow orchestration, infrastructure-grade, long-running; Precept is entity-level, structural |
| Camunda | "Process orchestration" | Enterprise BPM, low-code, visual-first |
| FluentValidation | "Validation library for .NET" | Constraints only — no state management, no transition enforcement |
| Stateless | "State machines for .NET" | State transitions only — no constraint enforcement, no compile-time checking |
| NRules | "Production rules engine" | Rules only — no state machine, no data binding |
| MassTransit / Hangfire | Message/job orchestration | Infrastructure-level, not entity-level |
| Custom code | Switch statements, scattered if-else | The status quo Precept replaces |

**The gap Precept fills:** No product combines state machines + constraint enforcement + compile-time checking + full inspectability in a single file. Every adjacent tool does one or two of these. Precept does all of them, deterministically, in plain text.

### The Five Differentiators

1. **Prevention, not detection.** Invalid states don't get caught — they are structurally impossible.
2. **One file, complete rules.** Guards, constraints, invariants, transitions in one `.precept` definition.
3. **Full inspectability.** `Inspect` previews every possible action and outcome without executing anything.
4. **Compile-time checking.** Unreachable states, dead ends, type mismatches, null violations — all caught before runtime.
5. **AI-native.** Deterministic semantics + structured MCP tools = AI can author and validate without human feedback.

### Positioning Against the Status Quo

Precept's competitive set is "custom code" and "scattered logic," not other libraries. Position against the universal pain, not specific products. That expands the market.

---

## 5. Open Questions

### Icon (Active — No Lock)

85+ candidates have been generated and reviewed in `brand/icon-prototyping-loop/`. No candidate has been locked. Active exploration directions include: proof marks, boundary stones, gated forms, letterform "P" variants, seal/stamp forms, and transition-arrow code patterns.

**The brief for icon work:** The icon must make you feel that something is governed, visible, and sound. It should not depict a workflow, a graph, or a state machine. It needs to represent the *guarantee* — not the mechanism. See `candidate-evaluation-rubric.md` for scoring criteria (minimum 4.0 average, hard fail below 3 on conceptual bridge / legibility / structural soundness).

### Hero Snippet

Which `.precept` example leads everywhere — README, VS Code marketplace, NuGet listing, marketing materials. Not yet locked. Requirements:
- Readable in 30 seconds
- Domain that makes rules obvious (something with clear states and clear consequences)
- Must show constraint signaling (an invariant or guard that rejects something meaningful)
- The gold rule message must earn its visual weight
- Should feel like something real, not a toy

See `.squad/decisions/inbox/steinbrenner-hero-example-spec.md` for active spec on this.

### Documentation Site

Does not exist. When it does, it will be the first surface where typography fully applies (Cascadia Cove for body, not just wordmark).

---

## 6. Key Tensions

### 1. Category Education vs. Instant Comprehension

"Domain integrity engine" is precise but not self-explanatory. The category-creator play requires educating the market on what the category *is* before they understand what Precept *does*. The tension: library-scale README copy must work in 10 seconds, but category creation requires more context.

**Current resolution:** Lead with "domain integrity engine" as the category, then immediately follow with the mechanism sentence ("binds state, data, and business rules…"). The second sentence is the education. This is working — don't over-correct toward plain description.

### 2. AI-Native as Primary vs. Secondary Frame

The "contract AI can reason about" angle is genuinely differentiating and timely. But leading with it risks alienating the core audience (developers who want to write rules by hand) and anchoring the product to a trend rather than a property.

**Current resolution:** AI-native lives in supporting copy, not the opening line. This is correct. Do not let enthusiasm for the AI story pull it forward before the core value proposition has landed.

### 3. Prevention vs. Inspectability

Two strong brand claims compete for primacy: "invalid states are impossible" (prevention) and "you can always see inside" (inspectability). Prevention is the power claim. Inspectability is the trust claim. Both are real.

**Current resolution:** Prevention is the headline. Inspectability is the second beat. This order is right — prevention is rarer and stronger. Don't invert them.

### 4. Precision vs. Warmth

The voice model ("authoritative with warmth") is a tension by definition. Too far toward authoritative and the copy feels cold, bureaucratic, antiseptic. Too far toward warmth and it loses the structural confidence that makes the brand credible.

**Watch for:** any sentence that explains *why you should care* (warmth overreach) rather than stating *what it does* (authoritative default). The clarifying aside is allowed. The motivation speech is not.

### 5. Library-Scale Humility vs. Platform Ambitions

The brand materials draw on Temporal, Docker, Stripe — platform-scale analogues. But the product is a GitHub library. Overreaching the analogy in copy creates cognitive dissonance with a NuGet listing.

**Resolution:** Borrow the *pattern* (category creation), not the *posture* (platform manifesto). Stay at library voice. The category ambition is in the framing, not the register.

---

## 7. For Hero Work Specifically

### What a Hero Snippet Must Do

1. **Show prevention, not just structure.** Include at least one guard or invariant that rejects something. The engine saying "no" is the product's core guarantee — if the snippet doesn't show it, the brand claim isn't proven.

2. **Make the domain real.** Pick something with genuine stakes: a loan, an order, an approval workflow. Toy examples (traffic lights, doors) don't carry the "business rules that hold" weight.

3. **Use constraint signaling.** At least one state or event should be italic (constrained), and at least one field should be italic (guarded by invariant). This makes the semantic palette do its job visually.

4. **Use gold.** At least one `because` or `reject` message should appear. Gold is the only warm hue — it earns its place by being the human-readable explanation of why a rule fired. No gold means no gold message means the visual interrupt is missing.

5. **Stay readable in 30 seconds.** If a developer can't skim the snippet and understand the domain, the rules, and one rejection scenario in 30 seconds, it's too complex.

6. **Show the `->` pipeline visually.** The `from X on Y -> set Z = ... -> transition W` pattern is Precept's most distinctive syntactic form. Any hero snippet that doesn't show the pipeline is showing the wrong surface.

### What to Avoid in a Hero Snippet

- States that are positional but not meaningful (Draft → Active → Archived with no interesting rules)
- Guards that are trivially obvious (`amount > 0`) with no domain weight
- An invariant without a clear consequence (what happens when it fires?)
- More than 30–35 lines — this is a hero, not a spec

### Voice in Hero Snippet Comments

If the snippet has comments, they explain *why*, not *what*. "# Prevents re-submission after review" not "# This is the Submit event handler." The comment is the brand voice rendered in code.

---

## Raw Observations

*Direct. Not softened.*

**The icon situation is a problem.** 85+ candidates and no lock. This isn't creative exploration anymore — it's decision avoidance. The rubric is solid. The conceptual brief is solid. What's missing is a committed choice. At some point the right move is to pick the strongest candidate that scores ≥4.0 across all criteria and ship it, knowing it can evolve.

**`brand-narrative.md` has a typo at line 2** — "please re-read teh" — that suggests it was edited mid-thought and never cleaned up. Not a brand crisis, but the research files should be clean source material.

**The reference files are archival, not active.** The research in `color-systems.md`, `typography.md`, `adjacent-products.md`, and `brand-positioning.md` represents the reasoning *behind* decisions that are now locked in `brand-decisions.md`. Most of that material is no longer actionable — it's the *why*, not the *what*. This creates a navigation problem: if someone reads the reference files first, they'll encounter a lot of "here are four options" framing for things that have already been decided. Consider adding a `STATUS: SUPERSEDED BY brand-decisions.md` header to those files so future team members don't re-litigate closed questions.

**The AI-native frame is undersold in all current materials.** "The contract AI can reason about" appears as a secondary positioning note, but the actual implementation — five MCP tools, a deterministic engine, structured tool APIs — is genuinely compelling and differentiated. No other tool in this space has a first-class AI authoring story. This doesn't mean making it primary, but the current materials treat it as almost a footnote. It deserves a dedicated paragraph somewhere in the README.

**The hero snippet question is the most consequential unresolved brand decision.** The icon matters for recognition. The hero snippet matters for conversion. The right `.precept` example will do more brand work than any other single asset — it will be in every screenshot, every blog post, every VS Code listing. Closing the `steinbrenner-hero-example-spec.md` item is higher priority than closing the icon.

**The brand-spec.html is the authoritative reference, but it's not easy to link to.** It's a local HTML file. If this team grows, there's going to be a "where do I find the brand spec" problem. A plain Markdown version of the locked decisions (not the research, just the outcomes) would make brand compliance easier for contributors.

**The wordmark's conceptual rationale is strong but not documented anywhere easily findable.** "Small caps is the typographic convention for defined terms, legal codes, and axioms" is a great line — it should appear somewhere in the public README or docs, not just in internal brand files. That kind of reasoning builds brand credibility with developers who care about intentionality.

---

## Philosophy
*Source: `brand/philosophy.md` — synthesized for brand use.*

### What It Feels Like to Use It

You write a definition. The engine holds it. From that point forward, there is no question you cannot ask and no outcome that surprises you.

`Inspect` answers "what would happen?" for every event, from any state, without touching the data. `Fire` executes — and if the guard fails or a constraint is violated, the invalid state simply never exists. Not caught. Not rolled back. Never instantiated. The boundary between possible and impossible is explicit, structural, and permanent.

This is the feeling the brand must reproduce in every surface: not vigilance, but certainty. You don't watch for failure. You define the rules, and the rules hold.

### The Four Differentiators (Brand Use)

These are not feature bullets. They are claims about the nature of the product — and they should read that way.

1. **Prevention, not detection.** Invalid states are structurally impossible. The contract prevents them before they exist. This is the headline claim and the hardest one to steal. Do not soften it.

2. **One file, complete rules.** Every guard, constraint, invariant, and transition lives in the `.precept` definition. Scattered logic across services is the status quo this product replaces. The single-file nature is not a convenience — it is a design statement about where authority lives.

3. **Full inspectability.** At any point, from any state, you can preview every possible action and its outcome. Nothing is hidden. This is the trust claim: the system is not a black box.

4. **Compile-time checking.** Unreachable states, dead ends, type mismatches, null-safety violations, structural contradictions — all caught before runtime. The compiler is part of the contract.

These four work in order. Prevention is why you adopt it. One file is how it's organized. Inspectability is what you trust about it. Compile-time checking is what makes you confident in the first three.

### The Word Itself

"Precept" means a strict rule or principle of action. It is not a metaphor. It is not evocative branding in search of a meaning. It describes the product exactly.

The name carries an inherent register — legal, principled, enduring. It sounds like something that cannot be argued away. For a product whose central claim is that business rules are unbreakable, the name is the first proof. Every other name in this category (Stateless, Temporal, Hangfire) describes a mechanism or a mode. Precept describes an obligation.

This is why the small-caps wordmark is correct. Small caps is the typographic convention for defined terms, legal codes, and axioms. Before you read the word, the letterform says: *this has been decided.*

### What the Icon Must Evoke

The icon does not depict a workflow, a graph, or a state machine. Those are the mechanism. The icon represents the *guarantee*.

Four qualities must be present, at minimum as feeling:

- **Governance.** Something is in charge. The rules are not suggestions.
- **Visibility.** You can see inside. The system is not opaque.
- **Soundness.** The structure holds. It will not shift under load.
- **Boundedness.** Some paths are open. Others are closed. The line between them is explicit and drawn.

The last quality is the most specific direction from `philosophy.md`: **open and closed paths, and the explicit boundary between them.** This is not a metaphor the icon should gesture at — it is the geometric fact of what the engine does. The icon should make the viewer feel that they are looking at something with a defined interior and a defined exterior, and that the difference between the two is not approximate.

### Visual Metaphors to Explore

From the philosophy directly:

- **Open/closed paths** — the spatial expression of possible vs. impossible states
- **Containment** — a bounded form that is self-contained and exact; not sprawling, not approximate
- **A strong mark in monochrome** — the concept must survive stripping the palette

Explicitly ruled out: anything that reads as a generic SaaS badge, vague concepts disconnected from what the product actually does.

Color is free — any palette that serves the concept. Semantic color (one hue for open, another for closed) is permitted but not required. Dark background preferred. The mark must be strong in monochrome first.
