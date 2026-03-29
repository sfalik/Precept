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

17 shades in 6 families. Background `#0c0c0f`. Every color has a fixed meaning across all surfaces (syntax, diagrams, inspector). Nothing is decorative.

**Semantic color as brand.** Most tools pick colors for aesthetics, then assign meanings later. Precept's palette is the reverse — every color exists because the compiler knows something about the code and needs a way to say it. The system is organized along two dimensions:

- **Dimension 1 — what kind of thing:** States (nouns — where you are), Events (verbs — what happens), Data (variables — what you know). Each gets its own hue family so a line of code is instantly scannable without reading it.
- **Dimension 2 — runtime evaluation:** Verdicts (enabled / blocked / warning) are reserved for runtime outcomes. They never appear in syntax highlighting, so green and red can't be confused with authoring-time colors.
- **Rules sit at the intersection** — they aren't a category of *thing*. They produce *verdicts about* categories. Gold is the only warm hue in a cool-toned palette, so constraint syntax interrupts visually.
- **Structure is scaffolding** — intentionally quiet. Declaration, action, and glue keywords fade behind the semantic content they frame.

#### Structure · Indigo · 239–245°

Scaffolding — intentionally sub-AA contrast, fades behind the content it frames. Three tiers create hierarchy: declarations define the schema, actions drive the machine, glue is connective tissue.

| Shade | Hex | CR | Role | Keywords |
|-------|-----|----|------|----------|
| Declaration | `#4338CA` | 2.5 | Top-level declarations | precept, field, state, event, invariant |
| Action | `#4F46E5` | 3.1 | Imperative actions | from, on, in, set, transition, edit, to, add, remove, enqueue, dequeue, push, pop, clear |
| Glue | `#6366F1` | 4.4 | Connective tissue | as, with, when, nullable, default, no, into, of, any, ->, = |

Structure is intentionally sub-AA contrast — it's scaffolding that fades behind the content it frames.

#### States · Azure · 220°

Nouns — where you are. Three lifecycle shades let the compiler show origin → active → final without annotation.

| Shade | Hex | CR | Role |
|-------|-----|----|------|
| Origin | `#638EE3` | 6.1 | Initial state + `initial` keyword |
| Active | `#80A5EF` | 7.9 | Non-initial, non-terminal states |
| Final | `#98B7F6` | 9.7 | Terminal states |

#### Events · Sky · 195°

Verbs — what happens. Bright cyan gives events visual primacy on action lines, where they're the most important word.

| Shade | Hex | CR | Role |
|-------|-----|----|------|
| Transition | `#3AC8F8` | 10.0 | Always moves state |
| Conditional | `#7DDAFC` | 12.4 | May transition or reject |
| Stationary | `#0FB0EB` | 7.8 | Data-only, no transition |

Sky shifted from 199° to 195° to widen the gap to Azure from 21° to 25°.

#### Data · Slate · 215–223°

Variables — what you know. Neutral slate recedes behind the colored actors (states, events, rules), letting field names read as content rather than chrome.

| Shade | Hex | CR | Role |
|-------|-----|----|------|
| Names | `#94A3B8` | 7.6 | Field names, arg names |
| Types + Operators | `#78829B` | 5.1 | string, number, boolean, set, queue, stack + &&, >=, !=, ==, contains |
| Values | `#64748B` | 4.1 | Literals, non-rule strings, arithmetic |

#### Rules · Gold · 43°

The only warm hue in a cool-toned palette — gold interrupts visually, signaling "this is a constraint." Bright messages outrank muted keywords: the human-readable explanation is the hero text.

| Shade | Hex | CR | Role |
|-------|-----|----|------|
| Keywords | `#B8860B` | 6.0 | assert, because, reject |
| Messages | `#FBBF24` | 11.7 | because/reject string content |

#### Verdicts · Runtime only

Never in syntax highlighting — reserved entirely for runtime outcomes. Emerald is purely "enabled" (no longer double-duty with types). Green/red/yellow can't be confused with authoring-time colors because they only appear in the inspector and diagrams.

| Shade | Hex | CR | Role |
|-------|-----|----|------|
| Enabled | `#34D399` | 10.2 | Enabled, valid, success |
| Blocked | `#F87171` | 7.1 | Blocked, rejected, violated |
| Warning | `#FDE047` | 14.8 | Unmatched, warning |

### Hue map

```
43° Gold — 50° Warning — 158° Emerald — 195° Sky — 215° Slate — 220° Azure — 239° Indigo
         7°           108°            37°         20°          5°          19°
```

Minimum gap: 5° (Slate↔Azure) — separated by saturation (15-20% vs 65-78%) and lightness tier.

### Structural state coloring (compiler-driven)

Three structural roles the compiler determines automatically — no DSL annotation needed:

| Role | Color | Token | Rationale |
|------|-------|-------|-----------|
| **Origin** | Azure `#638EE3` | `preceptState.initial` | Entry point. Deepest Azure shade. |
| **Active** | Azure `#80A5EF` | `preceptState.intermediate` | In-progress. Mid-tone Azure. |
| **Final** | Azure `#98B7F6` | `preceptState.terminal` | Terminal. Lightest Azure shade. |

All three use the Azure 220° family at different lightness tiers. "Approved" and "Declined" get the same Final shade — the compiler can't judge success vs failure.

### Event structural coloring (compiler-driven)

Three structural roles the compiler determines by analyzing transition behavior:

| Role | Color | Token | Determination |
|------|-------|-------|---------------|
| **Transition** | Sky `#3AC8F8` | `preceptEvent.transition` | Every `from` block for this event contains a `transition` action |
| **Conditional** | Sky `#7DDAFC` | `preceptEvent.conditional` | Some `from` blocks transition, others `reject` or have `when` guards |
| **Stationary** | Sky `#0FB0EB` | `preceptEvent.stationary` | No `from` block for this event contains a `transition` action |

Parallel to state structural coloring — the compiler classifies events automatically, no DSL annotation needed.

### Background

`#0c0c0f` — near-black with a slight warm undertone. All contrast ratios in the palette are computed against this background.

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

**Structural semantic coloring**: Both state names and event names in syntax highlighting are colored by their compiler-determined structural role, matching their diagram node/edge color. This is automatic — no DSL annotation needed.

**The system**: Every color means something. Nothing is decorative. 6 families, 17 shades:
- **Indigo** = structure (keywords, actions, glue)
- **Azure** = states (origin, active, final)
- **Sky** = events (transition, conditional, stationary)
- **Slate** = data (names, types, values)
- **Gold** = rules (assert/because/reject keywords + message strings)
- **Emerald / Coral / Yellow** = verdicts (enabled, blocked, warning — runtime only)

The same system works across syntax highlighting, state diagrams, diagnostics, and the inspector panel.
