# Precept Brand Decisions

Status: In progress. Decided elements are marked ✅. Open items are marked 🔲.

---

## ✅ Positioning

**Primary frame**: Category creation — "domain integrity engine"

No competitor occupies this frame. Precept defines the category, then becomes synonymous with it. Same pattern as Temporal ("durable execution"), Docker ("containerization"), Terraform ("infrastructure as code").

**Secondary frame**: AI-native — "the contract AI can reason about"

The deterministic semantics + structured tool APIs make Precept uniquely suited for AI authoring and validation. This is the "why now" signal, not the primary identity.

**Combined single-sentence positioning**:
> "Precept is a domain integrity engine for .NET — a single declarative contract that binds state, data, and business rules into an engine where invalid states are structurally impossible."

The AI-native angle lives in supporting copy, not the opening line.

## ✅ Narrative archetype

**Category Creator**: "This is a new kind of thing."

Like Temporal's "durable execution" or Tailwind's "utility-first CSS." Requires category education but earns the strongest position.

README narrative arc:
1. One sentence: what it is (resolution)
2. One sentence: why it's different (implicit conflict)
3. One code sample (proof)
4. Feature list or badge row (comprehensiveness signal)

The name "Precept" carries narrative weight — it means "a strict rule of action." The name IS the story.

## ✅ Voice

**Authoritative with warmth.**

Tone coordinates (NN/g dimensions):
- Formal ↔ Casual: Slightly casual. Contractions fine. Occasional explanatory asides.
- Serious ↔ Funny: Serious. No jokes.
- Respectful ↔ Irreverent: Respectful.
- Matter-of-fact ↔ Enthusiastic: Matter-of-fact with clarifying asides.

The voice states facts. It doesn't hedge. It doesn't oversell. But it occasionally explains *when* something matters — like Workflow Core's "Think: long running processes with multiple tasks that need to track state."

Models: Serilog's professionalism + MediatR's economy + Workflow Core's clarifying warmth.

### Do / Don't

| Do | Don't |
|----|-------|
| "Precept compiles business rules into an engine." | "Precept supercharges your business logic!" |
| "Invalid states are structurally impossible." | "Say goodbye to bugs forever!" |
| "Think: scattered validation across six services — Precept puts it in one file." | "Imagine a world where..." |
| "Same definition + same data = same outcome." | "Precept guarantees 100% reliability!" |
| "One file. Every rule." | "All your rules in one convenient place!" |

## ✅ Brand color

**Primary brand color**: Deep indigo `#6366F1` (Tailwind indigo-500)

The "Precept color." Used for keywords in syntax highlighting, brand mark, NuGet badge, diagnostic codes, initial state nodes in diagrams.

### Semantic palette

Five colors, each with a fixed meaning across all surfaces (syntax, diagrams, diagnostics, preview panel):

| Color | Hex | Role | Syntax HL | Diagram | Diagnostics |
|-------|-----|------|-----------|---------|-------------|
| **Indigo** | `#6366F1` / `#818CF8` / `#A5B4FC` | Brand / Structure | Keywords | Initial state node | PRECEPT codes |
| **Emerald** | `#34D399` | Enabled / Valid | Type names | Enabled edges | Type annotations |
| **Rose** | `#F43F5E` / `#FB7185` | Blocked / Error | Error squiggles | Blocked edges (dashed) | Error severity |
| **Amber** | `#FBBF24` | Constraint / Warning | `because` strings | Guard annotations | Warning severity |
| **Slate** | `#475569` / `#94A3B8` | Structural / Inactive | Operators, punctuation | Intermediate nodes, muted edges | Separators |

### Structural state coloring (compiler-driven)

Three structural roles the compiler determines automatically — no DSL annotation needed:

| Role | Color | Token | Rationale |
|------|-------|-------|-----------|
| **Initial** | Light indigo `#A5B4FC` | `preceptState.initial` | Brand color. The entry point. |
| **Intermediate** | Slate `#94A3B8` | `preceptState.intermediate` | In-progress. Connective tissue. |
| **Terminal** | Lilac `#C4B5FD` | `preceptState.terminal` | Neutral finality. No success/failure implication. |

Terminal states use lilac (light violet from the indigo family) deliberately — "Approved" and "Declined" are both terminal, and the compiler cannot distinguish success from failure. Lilac reads as "finished" without judgment.

## ✅ Typography / Wordmark

**Brand font**: Inconsolata (Raph Levien, SIL Open Font License)

Humanist monospace. Narrower and more elegant than most mono faces. Variable weight 400–900.

**Wordmark treatment**: Inconsolata 700, small caps + 0.1em letter-spacing.

Small caps is the typographic convention for defined terms, legal codes, and axioms — exactly what a precept is. The typography says "this is a defined concept" before you read the word.

**Code font**: Inconsolata 400–600, normal case. Same typeface family — the code literally grows out of the wordmark.

```
Wordmark:  PRECEPT  (Inconsolata 700, font-variant: small-caps, letter-spacing: 0.1em)
Code:      precept LoanApplication  (Inconsolata 400-600, normal case)
```

## ✅ Visual language

**Primary visual surface**: `.precept` DSL syntax — the file format IS the brand. Like Prisma's `.prisma` files, the code is the hero image.

**Secondary visual surface**: State diagrams from the VS Code preview panel. The diagram uses the same semantic palette as the syntax highlighting, creating a visual link between code and visualization.

**Structural semantic coloring**: State names in syntax highlighting are colored by their structural role (initial/intermediate/terminal), matching their diagram node color. This is compiler-driven and automatic.

**The system**: Every color means something. Nothing is decorative. Indigo = "this is Precept." Emerald = "this is allowed." Rose = "this is blocked." Amber = "this is a rule." Slate = "this is structure." The same five-color system works across syntax highlighting, state diagrams, diagnostics, and the preview panel.

### Open exploration

- **Event coloring**: How should event names be colored in the semantic system? Currently classified as `"function"` in the language server. Exploring whether events deserve their own semantic color or role within the palette.
