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

15 shades in 6 families. Background `#0c0c0f`. Every color has a fixed meaning across all surfaces (syntax, diagrams, inspector). Nothing is decorative.

**Semantic color as brand.** Most tools pick colors for aesthetics, then assign meanings later. Precept's palette is the reverse — every color exists because the compiler knows something about the code and needs a way to say it. The system is organized along two dimensions:

- **Dimension 1 — what kind of thing:** States (nouns — where you are), Events (verbs — what happens), Data (variables — what you know). Each gets its own hue family so a line of code is instantly scannable without reading it.
- **Dimension 2 — runtime evaluation:** Verdicts (enabled / blocked / warning) are reserved for runtime outcomes. They never appear in syntax highlighting, so green and red can't be confused with authoring-time colors.
- **Rules sit at the intersection** — they aren't a category of *thing*. They produce *verdicts about* categories. Gold is the only warm hue in a cool-toned palette, so constraint syntax interrupts visually.
- **Structure is scaffolding** — intentionally quiet, rendered in semibold. Declaration, action, and glue keywords fade behind the semantic content they frame.

**Typography as a second channel.** Color alone doesn't carry enough information when five hue families cluster in the cool hemisphere. Typography adds a second axis:

- **Semibold** — structure keywords. Weight makes scaffolding feel load-bearing without adding brightness.
- **Italic** — data (field names, types, operators, values). Italic recedes visually, letting data read as content rather than chrome.
- **Normal weight** — states, events, rules. The primary semantic actors stand upright at default weight.

#### Structure · Indigo · 239–245° · Semibold

Scaffolding — intentionally sub-AA contrast, rendered in semibold to feel load-bearing. Three tiers create hierarchy: declarations define the schema, actions drive the machine, glue is connective tissue.

| Shade | Hex | CR | Typography | Role | Keywords |
|-------|-----|----|------------|------|----------|
| Declaration | `#4338CA` | 2.5 | semibold | Top-level declarations | precept, field, state, event, invariant |
| Action | `#4F46E5` | 3.1 | semibold | Imperative actions | from, on, in, set, transition, edit, to, add, remove, enqueue, dequeue, push, pop, clear |
| Glue | `#6366F1` | 4.4 | normal | Connective tissue | as, with, when, nullable, default, no, into, of, any, ->, = |

#### States · Violet · 260°

Nouns — where you are. Three lifecycle shades let the compiler show origin → active → final without annotation. Violet at 260° provides 21° of hue separation from indigo structure while keeping the cool blueprint family cohesion.

| Shade | Hex | CR | Role |
|-------|-----|----|------|
| Origin | `#8B7AED` | 7.0 | Initial state + `initial` keyword |
| Active | `#A898F5` | 8.5 | Non-initial, non-terminal states |
| Final | `#C0B4F8` | 10.0 | Terminal states |

#### Events · Cyan · 195°

Verbs — what happens. Toned cyan gives events visual primacy on action lines, where they're the most important word. Single shade — all events are the same color regardless of transition behavior.

| Shade | Hex | CR | Role |
|-------|-----|----|------|
| All events | `#30B8E8` | 9.5 | Event names in all contexts |

#### Data · Bright Slate · 215° · Italic

Variables — what you know. Bright slate in italic recedes behind the colored actors (states, events, rules) while remaining comfortably readable. The italic typography creates a distinct visual register even though the hue is close to structure.

| Shade | Hex | CR | Typography | Role |
|-------|-----|----|------------|------|
| Names | `#B0BEC5` | 8.0 | italic | Field names, arg names |
| Types + Operators | `#9AA8B5` | 6.5 | italic | string, number, boolean, set, queue, stack + &&, >=, !=, ==, contains |
| Values | `#84929F` | 5.5 | italic | Literals, non-rule strings, arithmetic |

#### Rules · Gold · 45°

The only warm hue in a cool-toned palette — gold interrupts visually, signaling "this is a constraint." Bright messages outrank muted keywords: the human-readable explanation is the hero text.

| Shade | Hex | CR | Typography | Role |
|-------|-----|----|------------|------|
| Keywords | `#B8860B` | 6.0 | semibold | assert, because, reject |
| Messages | `#FBBF24` | 11.7 | normal | because/reject string content |

#### Verdicts · Runtime only

Never in syntax highlighting — reserved entirely for runtime outcomes. Green/red/yellow can't be confused with authoring-time colors because they only appear in the inspector and diagrams.

| Shade | Hex | CR | Role |
|-------|-----|----|------|
| Enabled | `#34D399` | 10.2 | Enabled, valid, success |
| Blocked | `#F87171` | 7.1 | Blocked, rejected, violated |
| Warning | `#FDE047` | 14.8 | Unmatched, warning |

### Hue map

```
45° Gold — 195° Cyan — 215° Slate — 239° Indigo — 260° Violet
        150°        20°         24°          21°
```

All five authoring-time families cluster in two zones: gold alone at 45° (the warm outlier), then four cool families spanning 195°–260° (65° arc). Within the cool cluster, families separate via three independent channels: hue (20–24° gaps), saturation (low slate vs high violet), and typography (italic data vs semibold structure vs normal states/events).

### Structural state coloring (compiler-driven)

Three structural roles the compiler determines automatically — no DSL annotation needed:

| Role | Color | Token | Rationale |
|------|-------|-------|-----------|
| **Origin** | Violet `#8B7AED` | `preceptState.initial` | Entry point. Deepest violet shade. |
| **Active** | Violet `#A898F5` | `preceptState.intermediate` | In-progress. Mid-tone violet. |
| **Final** | Violet `#C0B4F8` | `preceptState.terminal` | Terminal. Lightest violet shade. |

All three use the Violet 260° family at different lightness tiers. "Approved" and "Declined" get the same Final shade — the compiler can't judge success vs failure.

### State diagram coloring

State diagrams use the same semantic palette, reinforcing the code ↔ diagram visual link:

| Element | Color | Rationale |
|---------|-------|-----------|
| Node borders | Indigo `#4338CA`/`#4F46E5`/`#6366F1` | Structure colors — `state` is an indigo keyword, so node containers are indigo |
| State names (text) | Violet `#8B7AED`/`#A898F5`/`#C0B4F8` | Matches syntax highlighting state colors by lifecycle tier |
| Transition arrows | Gold `#B8860B` | Transitions are "rules of motion" — gold is the rule channel |
| Event labels | Cyan `#30B8E8` | Matches syntax highlighting event color |

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

**The system**: Every color means something. Nothing is decorative. 6 families, 15 shades:
- **Indigo** = structure (keywords, actions, glue) — semibold
- **Violet** = states (origin, active, final)
- **Cyan** = events (single shade)
- **Slate** = data (names, types, values) — italic
- **Gold** = rules (assert/because/reject keywords + message strings)
- **Emerald / Coral / Yellow** = verdicts (enabled, blocked, warning — runtime only)

The same system works across syntax highlighting, state diagrams, diagnostics, and the inspector panel.
