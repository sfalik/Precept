# Field and Arg Semantic Colors — Proposal

## Status
**Final** — Shane decision locked (2026-05-09). Field and arg added to Data family. Spec updated.

## The Model

Shane articulated a two-axis reading of precept structure that cuts across the existing five-construct model:

- **Structure axis:** States + Fields — "what a precept IS." States name where the entity sits in its lifecycle; fields name what data the entity carries. Together they define the entity's shape.
- **Behaviour axis:** Events + Args — "what a precept DOES." Events name what can happen; args parameterize those happenings. Together they define how the entity transitions.

Colors should reinforce these two conceptual clusters. Each pair should share a hue neighbourhood without collapsing into one color.

## Field Color

**Recommendation:** Formalize `#A5B4FC` as the field-name color. Introduce a new CSS custom property `--field` at this value.

### Why this color

`#A5B4FC` is the existing hero identifier tone in the Structure family (indigo, ~239°). It currently carries only the precept name. Shane applied it temporarily to fields and found it reads correctly — and the reason it reads correctly is structural: fields define what the entity IS, just as the precept name identifies what the entity IS. They're both structural identifiers.

The pairing with State (`#A898F5`, violet, ~260°) works visually:

| Token | Hex | Hue | Role |
|-------|-----|-----|------|
| State | `#A898F5` | 260° violet | lifecycle coordinate |
| Field | `#A5B4FC` | 239° indigo | data shape identifier |

Both sit in the 239–260° blue-violet band. A reader scanning precept source sees a coherent "structural definition" zone — field names and state names live in the same colour neighbourhood without being identical.

### Why the existing Data color (`#B0BEC5`) is wrong for fields

The current spec places field names under the Data construct at `#B0BEC5` (bright slate, ~215°). This makes fields visually indistinguishable from type annotations and literal values. The slate family is factual and quiet — correct for `number` and `false`, but not for `tenureMonths` or `acceptedOffer`, which are *structural declarations*, not incidental data.

The hero identifier color promotes field names to the same visual register as the precept name and state names: these are the things you *name* when you *define* a precept, not the values you pass through it.

### Formal spec treatment

- New CSS custom property: `--field: #A5B4FC`
- New semantic token role: **Field identifier** — structural data shape declaration
- The hero identifier (`--structure-h`) and field identifier (`--field`) share the same hex value. This is intentional — they share a conceptual axis. They get distinct tokens because they are semantically different roles that could diverge in future if needed.
- The Data construct gallery's colour caption (currently: "Slate marks field and argument names") should update to reflect that field names have moved to the structural axis. Data retains types (`--data-t: #9AA8B5`) and values (`--data-v: #84929F`) plus arg names until the arg color ships.

### Practical disambiguation from precept name

Both the precept name and field names would share `#A5B4FC`. This is acceptable because:

1. The precept name appears exactly once, on the first line.
2. Naming conventions differ: `LoanApplication` (PascalCase) vs. `tenureMonths` (camelCase).
3. Syntactic context is unambiguous: `precept LoanApplication` vs. `field tenureMonths as number`.
4. If differentiation ever becomes necessary, the separate tokens allow it without system-wide changes.

## Arg Color

**Recommendation:** Introduce `#9AD8E8` as the arg-name color. New CSS custom property `--arg`.

### Why this color

Args parameterize events. They sit on the behavioural axis. The color must read as "event-adjacent" — clearly in the cyan neighbourhood — while being visibly distinct from the event color itself.

The hero identifier (`#A5B4FC`) relates to mid-structure (`#6366F1`) by lifting lightness and softening saturation. Applying the same tonal shift to events:

| Token | Hex | Hue | Saturation | Lightness | Role |
|-------|-----|-----|------------|-----------|------|
| Event | `#30B8E8` | 195° | ~80% | ~55% | event trigger |
| Arg   | `#9AD8E8` | 195° | ~55% | ~76% | event parameter |

`#9AD8E8` is a lifted, softened cyan that:

- Keeps the same hue (195°) as events — unmistakably the same family
- Reduces saturation and raises lightness — clearly subordinate to the event name
- Mirrors the structure→field tonal relationship (anchor→lifted companion)
- Reads cleanly on dark backgrounds without washing out
- Is distinct from Data-slate (`#B0BEC5`) — `#9AD8E8` has visible cyan saturation where slate is neutral
- Is distinct from Comment (`#9096A6`) — comment is gray-blue and italic; arg is cyan and upright

### What it's not

- Not teal — no hue shift toward green, which would imply a new colour family
- Not a muted cyan — muted reads as disabled or de-emphasized; args are active parameters
- Not a lighter event — it shares the hue but shifts along saturation/lightness, not just brightness

### Formal spec treatment

- New CSS custom property: `--arg: #9AD8E8`
- New semantic token role: **Arg identifier** — event parameter declaration, behavioural axis
- The Event construct remains single-shade. `--arg` does not live inside the Event family card; it sits alongside it as a companion token, the same way `--field` sits alongside `--structure-h`.

## Pairing Logic

The two pairings create a readable visual geometry:

```
STRUCTURE AXIS (indigo–violet, 239–260°)
  ┌─────────────┐     ┌─────────────┐
  │ Field       │     │ State       │
  │ #A5B4FC     │     │ #A898F5     │
  │ data shape  │     │ lifecycle   │
  └─────────────┘     └─────────────┘

BEHAVIOUR AXIS (cyan, 195°)
  ┌─────────────┐     ┌─────────────┐
  │ Arg         │     │ Event       │
  │ #9AD8E8     │     │ #30B8E8     │
  │ parameter   │     │ trigger     │
  └─────────────┘     └─────────────┘
```

Within each axis, the companion (field, arg) is lighter and softer than the anchor (state, event). The companion names the parts; the anchor names the whole.

Across axes, the two zones are spectrally separated: blue-violet vs. cyan. A reader who understands "blue-violet = what it IS, cyan = what it DOES" can orient instantly.

## Proposed Spec Additions

### 1. CSS custom properties (`:root` block in `semantic-visual-system.html`)

```css
--field: #A5B4FC;
--arg: #9AD8E8;
```

Add after `--data-v: #84929F;` in the existing `:root` block.

### 2. Semantic Colors section (§ 04, `#color-families`)

Add two new entries to the semantic color grid:

**Field** — as a companion swatch inside the Structure family card, or as a new mini-card bridging Structure and Data:

```
Field identifier

---

## Data Family Audit — Slate Hues

### The 3 slate hues

| Token | Hex | Spec role |
|-------|-----|-----------|
| `--data` | `#B0BEC5` | Primary anchor — "data identifier baseline" |
| `--data-t` | `#9AA8B5` | Type tone — type names (`number`, `string`, etc.) |
| `--data-v` | `#84929F` | Value tone — literals, constants, quoted strings |

### Where each is actually used in product surfaces

**`--data` (#B0BEC5)** — Used in the VS Code TextMate theme (`package.json`) for:
- `variable.other.property.precept` — field names referenced as data
- `variable.parameter.precept` — event argument parameters

Also used in the visual-system spec itself for code examples, form labels, modifier tokens, and doc-ref/pill styling. Not referenced directly in the Language Server or VS Code TypeScript/CSS source.

**`--data-t` (#9AA8B5)** — Used in the VS Code TextMate theme for:
- `storage.type.precept` — type keywords (`number`, `string`, `boolean`, etc.)
- `storage.modifier.state.precept` — state modifiers

Also used in the visual-system spec for mini-data-type displays and the `.so` scope class. Not referenced in the Language Server.

**`--data-v` (#84929F)** — Used in the VS Code TextMate theme for:
- `constant.other.value.precept` — literal values
- `constant.language.precept` — language constants (`null`, etc.)
- `constant.numeric.precept` — numeric literals
- `string.quoted.double.precept` — double-quoted strings (non-message)
- `constant.language.boolean.precept` — boolean literals
- `string.quoted.single.precept` — single-quoted strings

Most heavily used of the three — 6 TextMate scopes. Also used in the spec for mini-data-value displays and code examples. Not referenced in the Language Server.

**No surface spec directory exists** (`design/system/surfaces/` does not exist), so there are no standalone surface specs referencing these tokens.

### Impact of `--field` and `--arg` on the anchor

Here's the key finding: **`--data` (#B0BEC5) currently colors the two scopes that should now be `--field` and `--arg`.**

- `variable.other.property.precept` (field names) → should become `--field` (#A5B4FC)
- `variable.parameter.precept` (arg names) → should become `--arg` (#9AD8E8)

Once those scopes migrate to their new hues, `--data` (#B0BEC5) loses its only two TextMate consumers. It would have no remaining product-surface use outside the spec document itself.

### Recommendation: simplify to 2 slate tones

**Drop `--data` (#B0BEC5).** After field/arg migration, it has no consumers. The Data family becomes:

| Token | Hex | Role |
|-------|-----|------|
| `--data-t` | `#9AA8B5` | Type tone — type names |
| `--data-v` | `#84929F` | Value tone — literals and constants |
| `--field` | `#A5B4FC` | Field identifier (indigo hue) |
| `--arg` | `#9AD8E8` | Arg identifier (cyan hue) |

This is a 4-token family with 2 slate tones + 2 cross-hue members. The anchor was acting as a catch-all for "data-ish things that aren't types or values" — but those things turned out to be fields and args, which now have their own semantic identity.

If a future need arises for a generic data baseline color (e.g., a Data Form section header), `--data-t` can serve that role as the lighter of the two remaining slates, or a new token can be introduced with clear intent. Keeping a dead anchor "just in case" adds visual-system debt.

**Keep `--data-t` and `--data-v`.** Both have clear, distinct, active consumers in the TextMate theme. Types and values are genuinely different things — the 2-tone split carries real information.
#A5B4FC
field names — structural data shape
```

**Arg** — as a companion swatch inside the Event family card, or as a new mini-card bridging Event and Data:

```
Arg identifier
#9AD8E8
arg names — event parameters
```

### 3. Data construct gallery update (§ 05, `#data-c`)

Update the colour caption from:

> Slate marks field and argument names, with separate tones for type and value.

To:

> Slate marks type annotations and literal values. Field names use the structural identifier tone; arg names use the behavioural identifier tone. See Structure and Event companion tokens.

### 4. Token role summary table

If the spec gains a consolidated token table, include:

| Token | Property | Value | Semantic role |
|-------|----------|-------|---------------|
| `--field` | Field identifier | `#A5B4FC` | Field name in declarations and references |
| `--arg` | Arg identifier | `#9AD8E8` | Event argument name in declarations and references |

## Questions for Shane

1. **Cross-surface scope:** This proposal is scoped to syntax highlighting. Should field names also appear as `#A5B4FC` in the Data Form and Event Timeline surfaces? If yes, the Data Form's field-name treatment shifts from slate to indigo — a visible change.

2. **Data construct identity:** Pulling field and arg names out of Data-slate leaves the Data construct carrying only types and values. Is that the right reduction, or does Data need to retain some ownership of field names in non-syntax surfaces?

3. **Spec placement:** Should `--field` and `--arg` be formalized as companions inside the Structure and Event family cards respectively, or as a new "Structural/Behavioural Identifiers" sub-section? The companion-card approach keeps them visually nested; a separate section makes the two-axis model explicit.

4. **Light-theme validation:** `#A5B4FC` and `#9AD8E8` are both medium-light values that read well on dark backgrounds. For light VS Code themes, they may need darker variants. Should we define light-theme alternates now, or defer until light-theme work is in scope?

## Color Family Paradigm Recommendation

> **Revision history:**
> - **V1** (Elaine-42): Named "axis layer" — Structure Axis, Behaviour Axis, Grounding Axis. Rejected by Shane: states are already structural, making the axis grouping circular.
> - **V2** (Elaine-43): Standalone companion tokens — field and arg outside all families. Rejected by Shane: "fields and args ARE data. If we move them out, then what is Data really defining?"
> - **V3 (Final)**: Field and arg into Data family. Spec family definition updated to allow semantically-grouped families with distinct hues.

### Shane's decision

Fields and args are data tokens in the Precept DSL — they carry data values bound to entity states and event parameters. Moving them to a standalone layer would hollow out the Data family's identity. The family definition was too restrictive: it assumed families must be tonal variants of a single hue. Shane overruled that constraint.

### What shipped

1. **Field (`#A5B4FC`) and arg (`#9AD8E8`) are Data family members.** The Data family now has 5 tones: anchor (`#B0BEC5`), type (`#9AA8B5`), value (`#84929F`), field (`#A5B4FC`), arg (`#9AD8E8`).

2. **The spec's family definition was updated.** Families may contain semantically related members with distinct hues — the family name defines the semantic category, not a hue constraint. Hue proximity to neighboring families signals conceptual relationships (field's indigo signals entity structure; arg's cyan signals event behaviour).

3. **CSS custom properties added:** `--field: #A5B4FC` and `--arg: #9AD8E8` in the `:root` block.

4. **Data family card updated** from "Bright slate · 215°" / 3 tones to "Slate · Indigo · Cyan — data-tier tokens" / 5 tones.

5. **Construct gallery caption updated** to reflect field and arg as distinct Data family tones.

### What was dropped

- The axis layer (V1) — restated what hue proximity already communicates.
- Standalone companion tokens (V2) — hollowed out Data's semantic identity.
- The restriction that families must be hue-coherent at a single hue band.

### Downstream concerns (carried forward)

1. **Light theme.** `#A5B4FC` and `#9AD8E8` will need darker variants for light backgrounds. Defer until light-theme work is in scope.
2. **Cross-surface propagation.** Field names should appear as `#A5B4FC` in the Data Form; arg names as `#9AD8E8` in Event Timeline arg dialogs. Follow-on task.
3. **Semantic token implementation.** `--field` and `--arg` need corresponding TextMate/semantic token scopes in the VS Code extension.

## Anchor Drop — Literal Safety Check

**Question:** If we remove `--data` (#B0BEC5) from the Data family, do literals lose their color?

**Answer: Literals are completely safe.** No literal scope references the anchor. Every literal/value scope is wired exclusively to `--data-v` (#84929F).

### Evidence: VS Code TextMate theme (package.json)

All 6 literal/value scopes use `#84929F` directly — hardcoded hex, no indirection through `--data`:

| TextMate scope | Hex | Token |
|----------------|-----|-------|
| `constant.other.value.precept` | `#84929F` | `--data-v` |
| `constant.language.precept` | `#84929F` | `--data-v` |
| `constant.numeric.precept` | `#84929F` | `--data-v` |
| `string.quoted.double.precept` | `#84929F` | `--data-v` |
| `constant.language.boolean.precept` | `#84929F` | `--data-v` |
| `string.quoted.single.precept` | `#84929F` | `--data-v` |

`#B0BEC5` (the anchor) appears in only one remaining TextMate rule: `variable.other.property.precept` (field names) — already scheduled to migrate to `--field` (#A5B4FC).

### Evidence: Design spec (semantic-visual-system.html)

- `--data-v` is defined at `:root` as `#84929F` — a standalone value, not derived from `--data`. No CSS fallback chain exists.
- Code examples in the spec use `var(--data-v)` for literal values (`0`, `null`) and `var(--data)` for field names (`Amount`, `ApplicantName`). The two tokens are fully independent.
- The `.dv` utility class uses `var(--data-v)` directly. No `.dv` consumer falls through to `var(--data)`.

### What happens when `--data` is dropped

**TextMate theme:** Zero impact. The one scope using `#B0BEC5` (`variable.other.property.precept`) will have already migrated to `#A5B4FC` (`--field`). All literal scopes continue at `#84929F` unchanged.

**Design spec HTML:** The `--data` CSS custom property is referenced in ~25 places in the spec — but all of those are for field names in code examples, form labels, modifier tokens, doc-ref/pill styling, and the Data family card's anchor swatch. None are for literals. After migration, these references should update to `var(--field)` or be removed.

### Post-drop Data family: complete and gap-free

| Token | Hex | Role | Scope count |
|-------|-----|------|-------------|
| `--data-t` | `#9AA8B5` | Type tone — type names | 2 scopes |
| `--data-v` | `#84929F` | Value tone — literals, constants, strings | 6 scopes |
| `--field` | `#A5B4FC` | Field identifier — entity data shape | 1 scope (+ constrained variant) |
| `--arg` | `#9AD8E8` | Arg identifier — event parameters | 1 scope |

**No scope loses color.** The anchor was a semantic pass-through — it colored things that now have their own identity. Removing it leaves a cleaner family with every member purposefully named.

### Spec cleanup required (follow-on)

The ~25 `var(--data)` references in `semantic-visual-system.html` will need updating when the anchor is formally removed. Most should become `var(--field)` (field names in code examples, form labels). The Data family card should drop the anchor swatch and update from "5 tones" to "4 tones." This is a mechanical pass, not a design decision.
