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

The "Precept color." Used across structure grammar in syntax highlighting, the brand mark, NuGet badge, diagnostic codes, and diagram framing.

### Semantic palette

8 authoring-time shades in 5 families, plus 3 runtime verdict colors. Background `#0c0c0f`. Every color has a fixed meaning. Nothing is decorative.

**Locked direction (2026-04-03).**

For `.precept` editor highlighting, the locked direction is a dark-mode-only 8-shade system with no user-facing palette overrides.

| Family | Hex | Typography | Locked role |
|--------|-----|------------|-------------|
| Structure · Semantic | `#4338CA` | bold | `precept`, `field`, `state`, `event`, `invariant`, `from`, `on`, `in`, `to`, `set`, `transition`, `edit`, `assert`, `reject`, `when`, `no` |
| Structure · Grammar | `#6366F1` | normal | `as`, `with`, `default`, `nullable`, `any`, `of`, `into`, `because`, `=`, `->`, operators, punctuation glue |
| States | `#A898F5` | normal / italic if constrained | State names; italic means the state participates in `in/to/from X assert` |
| Events | `#30B8E8` | normal / italic if constrained | Event names; italic means the event has `on X assert` |
| Data · Names | `#B0BEC5` | normal / italic if guarded | Field and argument names; italic means referenced by an `invariant` |
| Data · Types | `#9AA8B5` | normal | `string`, `number`, `boolean`, collection types |
| Data · Values | `#84929F` | normal | literals such as `true`, `false`, `null`, strings, and numbers |
| Rules · Messages | `#FBBF24` | normal | human-readable message strings in `because` / `reject` |

Rule keywords do not use a separate gold keyword shade. They join Structure · Semantic. The only gold authoring-time syntax tokens are rule message strings.

**Semantic color as brand.** Most tools pick colors for aesthetics, then assign meanings later. Precept's palette is the reverse — every color exists because the compiler knows something about the code and needs a way to say it. The system is organized along two dimensions:

- **Dimension 1 — what kind of thing:** Structure, States, Events, and Data each get their own visual lane, so a line is scannable before it is read.
- **Dimension 2 — constraint signal:** Italic means "this token is under rule pressure" — constrained states, constrained events, and invariant-guarded data names all use typography, not extra hues.
- **Rules are mostly absorbed into structure** — `assert`, `reject`, `when`, and `no` are treated as semantic structure keywords. Gold is reserved for the human-readable message payload.
- **Verdicts remain runtime-only** — enabled / blocked / warning stay outside authoring-time syntax so green and red cannot be confused with static code meaning.

**Typography as a second channel.** Color alone doesn't carry enough information when five hue families cluster in the cool hemisphere. Typography adds a second axis:

- **Bold** — structure semantic keywords. Weight marks the DSL words that drive behavior.
- **Italic** — constrained states, constrained events, and invariant-guarded data names.
- **Normal weight** — structure grammar, unconstrained actors, data types, data values, and rule messages.

#### Structure · Indigo · 239–245°

Scaffolding and control. Two indigo shades split the words that drive behavior from the grammar that connects them.

| Shade | Hex | CR | Typography | Role | Keywords |
|-------|-----|----|------------|------|----------|
| Semantic | `#4338CA` | 2.5 | bold | Behavioral drivers | precept, field, state, event, invariant, from, on, in, to, set, transition, edit, assert, reject, when, no |
| Grammar | `#6366F1` | 4.4 | normal | Connective tissue | as, with, default, nullable, any, of, into, because, operators, punctuation glue, `->`, `=` |

#### States · Violet · 260°

Nouns — where you are. States now use one violet. Constraint presence is shown with italic, not lifecycle tint.

| Shade | Hex | CR | Typography | Role |
|-------|-----|----|------------|------|
| All states | `#A898F5` | 8.5 | normal / italic if constrained | State names in all contexts |

#### Events · Cyan · 195°

Verbs — what happens. Events keep a single cyan hue. Constraint presence is shown with italic.

| Shade | Hex | CR | Typography | Role |
|-------|-----|----|------------|------|
| All events | `#30B8E8` | 9.5 | normal / italic if constrained | Event names in all contexts |

#### Data · Bright Slate · 215°

Variables — what you know. Data splits into names, types, and values. Only names use italic, and only when guarded by invariants.

| Shade | Hex | CR | Typography | Role |
|-------|-----|----|------------|------|
| Names | `#B0BEC5` | 8.0 | normal / italic if guarded | Field names, arg names |
| Types | `#9AA8B5` | 6.5 | normal | string, number, boolean, set, queue, stack |
| Values | `#84929F` | 5.5 | normal | Literals and literal-like values |

#### Rules · Gold · 45°

The only warm hue in the authoring-time palette. Gold is reserved for message payloads, so the human explanation is the visual interrupt.

| Shade | Hex | CR | Typography | Role |
|-------|-----|----|------------|------|
| Messages | `#FBBF24` | 11.7 | normal | because/reject string content |

#### Comments · Editorial outside the semantic palette

Comments are not part of the executable semantic system, so they intentionally sit outside the 8-shade authoring palette. Use muted steel `#7A8599` in italic for `#` comments: visible enough to read, quiet enough to stay out of the semantic lanes.

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

The 8-shade authoring system uses five hue families. Within the cool cluster, families separate by hue and by typography: violet and cyan carry actor identity; slate carries data depth; indigo carries DSL control; italic carries constraint pressure.

### Constraint signaling (token-aware)

Constraint visibility is no longer expressed with extra hues. It is expressed through token-aware typography:

| Target | Signal | Detection |
|--------|--------|-----------|
| State names | Violet italic | State participates in `in/to/from X assert` |
| Event names | Cyan italic | Event has `on X assert` |
| Field / arg names | Slate italic | Name is referenced by an `invariant` |

The rule is uniform: same hue means same category; italic means constrained.

### State diagram coloring

State diagrams align to the same hue families, but they do not use lifecycle-tier syntax colors. Shape carries origin/final structure; hue carries category.

| Element | Color | Rationale |
|---------|-------|-----------|
| Node borders | Indigo `#4338CA` / `#6366F1` | Structure family frames the diagram |
| State names (text) | Violet `#A898F5` | One state hue; origin/final is shown by shape, not tint |
| Transition arrows | Indigo `#6366F1` | Flow belongs to structure grammar |
| Event labels | Cyan `#30B8E8` | Matches syntax highlighting event hue |
| Runtime outcomes | Emerald / Coral / Yellow | Reserved verdict colors for enabled, blocked, warning |

### Background

`#0c0c0f` — near-black with a slight warm undertone. All contrast ratios in the palette are computed against this background.

## ✅ Typography / Wordmark

**Brand font**: Cascadia Cove (with Cascadia Code fallback)

Monospace with a slightly more engineered, editor-native feel than Inconsolata. The Cove variant keeps the family recognizable while giving the wordmark and code samples a sharper technical posture.

**Wordmark treatment**: Cascadia Cove 700, small caps + 0.1em letter-spacing.

Small caps is the typographic convention for defined terms, legal codes, and axioms — exactly what a precept is. The typography says "this is a defined concept" before you read the word.

**Code font**: Cascadia Cove 400–700, normal case. Same typeface family — the code literally grows out of the wordmark.

```
Wordmark:  PRECEPT  (Cascadia Cove 700, font-variant: small-caps, letter-spacing: 0.1em)
Code:      precept LoanApplication  (Cascadia Cove 400-700, normal case)
```

## ✅ Visual language

**Primary visual surface**: `.precept` DSL syntax — the file format IS the brand. Like Prisma's `.prisma` files, the code is the hero image.

**Secondary visual surface**: State diagrams from the VS Code preview panel. The diagram uses the same hue families as syntax highlighting, but shape — not lifecycle tint — carries initial/final structure.

**Constraint-aware semantic coloring**: Syntax highlighting uses one hue per category, with italic indicating constrained states/events and invariant-guarded names. This is automatic — no DSL annotation needed.

**The system**: Every color means something. Nothing is decorative. 8 authoring-time shades plus runtime verdict colors:
- **Indigo** = structure semantic + structure grammar
- **Violet** = states; italic means constrained
- **Cyan** = events; italic means constrained
- **Slate** = data names, data types, data values; names italic when guarded
- **Gold** = rule messages only
- **Emerald / Coral / Yellow** = verdicts (enabled, blocked, warning — runtime only)

The same semantic vocabulary works across syntax highlighting, state diagrams, diagnostics, and the inspector panel.
