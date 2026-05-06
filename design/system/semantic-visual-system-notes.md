# Semantic Visual System Notes

Purpose: lightweight working notes for discussion around a consolidated definition of Precept's semantic visual system.

## Captured Intent

- Brainstorm first; no drafting yet.
- This should become a strong consolidated definition of Precept's semantic visual system.
- The final HTML should be beautiful and should dogfood the design system so it embodies Precept.
- Design conviction: rigorous enough to govern the system, expressive enough to prove it.
- Working definition: the semantic visual system defines how Precept means things on screen. It gives structure, states, events, data, explanatory rule language, and runtime verdicts a stable visual grammar so meaning stays recognizable across authoring, topology, preview, diagnostics, and public explanation. The point is not decoration; the point is faster recognition, lower confusion, and a surface language that feels unmistakably Precept.
- Presentation priority: the document itself should be highly visual, beautiful, and inspiring. It should embody the semantic visual system and the concepts and philosophy of Precept rather than reading primarily like governance or change-control documentation.
- Design constraint: this page should feel like an embodiment of the Precept system itself, not merely a page about the system. Beauty, composition, and visual conviction are part of the requirement, not decorative extras.
- Posture decision (confirmed): Option B is the chosen visual posture — the stricter architectural/austere variant. More negative space, harder edges, colder authority, strong sense of governed precision. This is the foundation for the canonical artifact.
- Opening tone preference: the document should not begin by justifying itself. Avoid section framing like "Why This System Exists" for the opening.
- Structure preference: do not force a hard top-level split between core constructs and runtime meaning if most semantic variants only make sense through live behavior. The construct galleries should be allowed to include live semantic variants and modifiers together with the base depiction.
- Reader assumption: this document is not the place to teach Precept from first principles. It can assume the reader already understands Precept at a conceptual and runtime level; readers who need that foundation should use other docs first.
- Document ambition: this should read as a visual canonical artifact and tour de force, not as a product-model primer.
- Section 2 intent: this is not a vocabulary refresher or general Precept recap. It should introduce concepts that are specific to the semantic visual system itself and are not defined elsewhere.
- Construct emphasis preference: keep the five core constructs feeling evenly balanced overall, with only a slight lean toward Data and Rule rather than a strongly data-dominant hierarchy.
- Gallery structure preference: in the System section, all five core constructs should use the same basic presentation template rather than giving Data and Rule a special structural format.

## Open Questions

## Notes

### Proposed semantic model

- Core constructs: Structure, State, Event, Data, Rule.
- Related semantic categories that are also part of the system: Transitions and verdicts.
- Boundary rule: transitions are purposely not a core construct. They participate in the semantic visual system, but remain outside the core construct set.
- Boundary rule: verdicts are also adjacent rather than core. They are typically runtime-applied semantics related to rule execution.
- Relationship note: rules are attached to some of the core constructs as defined in the runtime.
- Surface model: the visual system manifests across surfaces rather than existing as a single surface. Current named surfaces include the DSL text editor, the state diagram, the data form, the timeline, and public-facing documentation.
- Surface implication: the system should define stable semantic meaning that can travel across surfaces while allowing each surface to realize that meaning through its own appropriate form and layout.
- General foundation layer: the visual system also includes shared foundations that are not tied to a single construct or one surface, including typography, the color palette, and other cross-system visual rules.
- Document framing requirement: the document should include an overview that explains both how the system works and why it exists. It should cover operational logic and design philosophy together.

### Proposed semantic signals

- Colour: five families map to the core constructs, plus three verdict colours.
- Typography: bold or italics convey additional meaning, such as constrained status.
- Form: each construct should have a distinct shape or base form on visual surfaces.
- Layout: ordering, sequencing, grouping, and placement also carry meaning.

### Proposed definition pattern for each construct

For each core construct, define:

1. Identifier: the textual way the construct is recognized.
2. Base depiction — Definition: how it appears in authored DSL source (token form — colour, weight, mono).
3. Base depiction — Runtime: how it appears as a rendered component across Diagram, Data Form, and Timeline.
4. Semantic modifiers: additive visual signals layered onto the base form to represent specific meanings.

Modifier rule: modifiers are additive rather than replacement-based. Examples include an initial-state dot or a terminal-state double border.

### First-pass construct table

| Construct | Identity | Base Depiction — Definition (Code) | Base Depiction — Runtime (Diagram · Data Form · Timeline) | Semantic Modifiers |
|---|---|---|---|---|
| Structure | Not applicable | Indigo syntax tokens: keywords (`precept`, `state`, `event`, `from…on`, `set`, `transition`) in structure colour — grammar rendered as colour | Indigo framing chrome: container borders, section headers, rail, group labels — ambient structural scaffolding | None |
| State | State name | Violet mono token in `state StateName { }` declaration | Hard-rect node, violet border, violet mono label — same shape on all runtime surfaces | initial (dot), terminal (double border), constrained (italic label), reachableFromInitial, reachableFromCurrent, current |
| Event | Event name | Cyan mono token in `on EventName` / `from X on EventName` clause | Directed arc or row with cyan label + directional marker — marker is invariant on all runtime surfaces | transition, self-transition, no-transition, rejected, constraint failure, unmatched |
| Data | Field name | Data-colour mono token for field name; type annotation in data-t; no value shown (values are runtime-assigned, not authored) | Three-part row: name (data, mono) · type (data-t, mono) · value (data-v, mono) — mono throughout, no carve-outs | editable, constrained (italic name label), violated (dashed border on row), required |
| Rule | Rule message text (the `because` / `reject` string) | Gold mono token inline in `because "…"` or `reject "…"` clause | Gold message rendered below field or as verdict overlay when rule fires — the message is the visual interrupt | satisfied, violated |

### Team research synthesis

- The document should stay construct-first rather than surface-first. Surfaces matter, but they should be framed as translations of stable semantic meaning rather than as independent mini-systems.
- Authored meaning and runtime outcome must stay separate. Core construct identity should remain stable, while verdicts act as overlays rather than replacements.
- The system document should not become a second brand spec. Brand owns semantic meaning at the identity level; the system document owns reusable cross-surface operationalization.
- The system document should also not collapse into surface-local implementation detail. It should define shared semantic law first, then show how surfaces realize that law.
- The strongest throughline is user recognition: faster reading, lower confusion, and stable meaning across editor, diagram, form, timeline, diagnostics, and explanation.
- The signal system should be treated as a grammar with precedence and combination rules, not just a palette inventory.
- AI readability and structured inspectability are part of the system contract, especially for runtime and diagnostic surfaces.
- The user wants the main document to privilege visual embodiment and inspiration over governance framing. Governance and authority concerns should not dominate the main narrative flow.

### New context from merged README and philosophy

- The newly merged product framing is governed integrity, not workflow orchestration. The visual system should align with that center of gravity.
- Data and rules are now stated explicitly as the primary concern, with state described as the structural mechanism that makes governance lifecycle-aware when lifecycle exists.
- States are optional in the current philosophy. The semantic visual system cannot imply that every canonical Precept specimen is primarily a state-machine specimen.
- Stateless precepts are now first-class in the product story. The visual system should be able to represent data-only governance clearly and convincingly, not only lifecycle-heavy examples.
- The philosophy now defines validity in terms of configuration: lifecycle position plus field values when state exists, or field values alone when it does not. This is useful framing for how surfaces should present semantic truth.
- README emphasis now strongly reinforces one-file completeness, inspectability, determinism, and AI-first authoring. Those themes should influence which surfaces and examples feel canonical.
- Design implication: the doc should still feel visual and inspiring, but its examples and signal hierarchy should not accidentally make state topology look like the only real form of Precept.

### Boundary mistakes to avoid

- Re-merging brand identity, reusable system guidance, and surface-local realization into one authority layer.
- Letting runtime states or surface specimens harden into new core semantic categories.
- Using verdict colour to replace base construct identity instead of overlaying it.
- Reopening exploratory palette drift or surface-local styling drift inside the canonical system document.
- Letting state-diagram logic dominate the system so heavily that data-only or stateless governance looks secondary.

### Current rough document shape

- Intro
- Concepts
- System
- Surfaces

### Posture studies

- `semantic-visual-system-posture-a.html` — baseline editorial codex.
- `semantic-visual-system-posture-b.html` — stricter architectural and more austere.
- `semantic-visual-system-posture-c.html` — richer expressive variant that stays controlled.

This appears to be the user's preferred high-level compression for the document structure.

#### Intro

- Open with "The Semantic World of Precept" or a close variant.
- Present the high-level visual thesis: Precept has a stable semantic world that can be read across surfaces as governed integrity, not merely as workflow motion.
- Introduce the five core constructs as a balanced cast: Structure, State, Event, Data, and Rule.
- Let Data and Rule carry a slight extra pull in the framing language because they express what is being governed, but do not let them eclipse the other three.
- Use a strong hero composition that immediately shows beauty, coherence, and semantic recognizability without implying that topology is the only canonical reading of Precept.
- Keep the opening out of primer territory. It should orient the reader to the semantic world, not teach the whole runtime.

#### Concepts

- Define only concepts that belong specifically to the semantic visual system, not general Precept vocabulary.
- Establish configuration as the thing the system helps users read: what is true of an entity now, whether that includes lifecycle position, field values, or both.
- Define semantic identity, base depiction, additive modifier, overlay, and cross-surface translation.
- Define the four signal channels as a grammar with precedence and combination rules: colour, typography, form, and layout.
- Clarify authored identity versus runtime-applied meaning so verdicts and live state do not replace core construct identity.
- Define the distinction between the five core constructs and adjacent semantics such as transitions and verdicts.
- Clarify lifecycle optionality here as a constraint on the system's honesty, while still treating lifecycle-heavy reading as a common and important mode.

#### System

- Use this as the canonical center of the artifact: define the visual identity of Structure, State, Event, Data, and Rule with roughly even authority across the set.
- Give each construct the same basic explanatory discipline: identity, base depiction, modifier logic, and key semantic variants.
- Let Data and Rule carry slightly richer interpretive weight because they express what is governed, but do not give them so much extra space or ornament that the system feels data-dominant.
- Preserve one shared gallery template across all five constructs so the balance is felt structurally, not just stated rhetorically.
- Keep State and Event visibly indispensable as the mechanisms that make governance legible through time.
- Show how additive modifiers and overlays attach to the base depiction without replacing core identity.
- Show adjacent semantics such as transitions and verdicts as dependent overlays that reveal what happened to core constructs, not as new peers in the system.
- Define the concrete signal behavior of the system through these galleries: colour, typography, form, layout, and their combination rules in practice.

##### Proposed shared gallery template

For each of the five core constructs, use the same five-slot pattern:

1. Role: what this construct is in the semantic world of Precept.
2. Identity: how the construct is recognized in authored form, especially in language and labeling.
3. Base depiction: the stable visual form that carries the construct before runtime modifiers are applied.
4. Modifier logic: the additive signals, overlays, or stateful/runtime conditions that can attach without replacing core identity.
5. Cross-surface translation: how the same construct remains recognizable across editor, diagram, forms, runtime views, and explanatory documentation.

Working implication: Data and Rule can carry slightly richer examples, captions, or modifier nuance inside this shared structure, but they should not receive a different structural format.

Presentation implication: the final HTML should not expose these five slots as a rigid repeated checklist or visibly instructional subheading stack. The shared template should govern composition internally, while the reader experiences a more composed pattern such as semantic role, hero specimen, live variants/overlays, and cross-surface proof.

Visual implication: every structural decision should be judged against whether it increases or reduces the page's power as a visual artifact. If a pattern makes the page clearer but deadens it into a handbook or mechanical reference, it is probably the wrong pattern for this document.

#### Surfaces

- Show the semantic visual system in action as one law translated across multiple surfaces, not as separate mini-systems.
- Lead with the code window as the hero surface because it best expresses one-file completeness, authored meaning, and inspectable structure.
- Cover the state diagram as the topology-reading surface, but not as the surface that defines the whole artifact's meaning by itself.
- Cover data forms or inspection surfaces as the field-and-constraint-reading surface.
- Cover timeline or event-flow views as runtime-reading surfaces where outcome overlays become legible.
- Cover explanatory/docs as the translational surface that carries the same semantic language into narrative and reference contexts.
- For each surface, state what remains invariant, what adapts to the medium, and what that surface helps the user read fastest.
- Include one deliberate stateless specimen somewhere in this section so the philosophy is visible in the artifact, without forcing equal airtime for stateful and stateless cases.

## Session Decisions — 2026-04-08

### Color System

- Section 06 Colors added to the document: authoring palette strip (8 swatches), comments + verdicts + background strip, six concept cells (one per construct family plus comments), four rule channels.
- Canonical palette CSS vars locked:
  - `--bg: #040506`, `--panel: #090b0e`, `--panel-2: #0d1014`
  - `--structure-d: #4338CA` (semantic, bold), `--structure: #6366F1` (grammar)
  - `--state: #A898F5`, `--event: #30B8E8`
  - `--data: #B0BEC5`, `--data-t: #9AA8B5`, `--data-v: #84929F`
  - `--rule: #FBBF24`, `--comment: #9096A6`
  - `--valid: #34D399`, `--violated: #F87171`, `--warn: #FDE047`
- Token class mapping locked: `.s` (structure grammar), `.sd` (structure semantic, bold), `.st` (state), `.ev` (event), `.da` (data name), `.dt` (data type), `.dv` (data value), `.ru` (rule/message), `.cm` (comment, italic), `.ok` (valid), `.no` (violated), `.so` (data-t tonal).
- Label vocabulary rule: no color-name terms in semantic labels. Terms like "amber tone," "rose mark," "emerald callout," "violated tone" are banned. Labels name semantic roles, not color names.

### Construct Gallery Redesign

- Each construct gallery now shows two specimens side-by-side: **Definition** (authored DSL syntax token, left) and **Runtime** (rendered component, right), separated by a strong hairline seam.
- Cross-surface translation strips removed from all galleries. Cross-surface translation is reserved for the Surfaces section (Section 04).
- Per-construct Runtime specimens:
  - Structure/Runtime: indigo container frame with nested state rects inside — the perimeter relationship made visible.
  - State/Runtime: four hard-rect state nodes showing all role variants (initial dot, active fill, double border for terminal, italic for constrained).
  - Event/Runtime: three transition rows showing outcome variants (plain, guarded/italic, blocked/dashed-violated).
  - Data/Runtime: field-table with semantic tonal classes — `.da` name, `.dt` type, `.dv` value. Four rows covering valid, violated/required (constrained italic), editable, locked.
  - Rule/Runtime: three verdict callouts rendered as actual specimens — satisfied (green), violated (red), unmatched (amber).
- Data table tonal classes corrected: `.dt` for type, `.dv` for value (previously `.so`). Semantically correct; visual output unchanged.
- Modifier sections now visually demonstrate modifier traits in the chips themselves where possible (e.g. dashed border on blocked chip, italic on guarded chip, double border on terminal chip).
- Rule modifiers section replaced chips for callout variants with full-width rendered callout specimens.
- New CSS classes added: `.specimen-pair`, `.specimen-half`, `.specimen-half-bar`, `.specimen-half-label`, `.specimen-half-tag`, `.specimen-half-inner`, `.callout-demo` (+ variants), `.tonal-demo`, `.tonal-cell`, `.tonal-cell-lbl`.
- `.rule-callout` semantic: border, background, and text are violated (red family), not rule (gold). A callout rendered because a rule fired is a verdict signal, not a rule color signal.

### Surface Inventory

- Final surface model: **Code · State Diagram · Data Form · Event Timeline**.
- Docs dropped from the active surface scope. Surface count: four.
- "Stateless Precept" is not a peer surface. Code is code — a stateless precept is just a precept without state declarations. No separate specimen category for it.
- "Inspector" term is banned. The canonical term is **Data Form** everywhere.

### Definition / Runtime Framework

- Surfaces are organized as: **Definition** (Code) and **Runtime** (State Diagram · Data Form · Event Timeline).
- The State Diagram is a Runtime surface — it is rendered from a compiled `PreceptDefinition`, not from DSL text directly.
- Definition and Runtime are the two organizing categories. No "authoring/presentation" or "textual/visual" split.
- The three runtime surfaces are peer surfaces. No surface among Diagram, Data Form, and Timeline is primary over the others.

### Channel Consistency Rules (Runtime Surfaces)

- "Visually consistent" across runtime surfaces means all four channels are consistent: colour, typography, form, and layout.
- **Colour**: runtime construct families map to their canonical colour tokens on all surfaces. No surface gets a local palette exception.
- **Typography**:
  - Mono for everything the runtime reasons about: state names, event names, field names, field values, rule messages.
  - Sans for ambient context: labels, captions, nav, structural UI text.
  - Italic = constraint pressure on identity label only, all surfaces (no other italic use).
- **Form**:
  - States = hard-rect (square corners) on all runtime surfaces. No rounded states anywhere.
  - Blocked/violated = dashed border. This is the exclusive form signal for that meaning — reserved, no other use.
  - Events carry a directional marker (arrow) on all runtime surfaces.
- **Layout**: layout grammars differ by surface purpose but share the same principles (grouping by meaning, consistent spacing rhythm). No surface invents private layout semantics.
- **Field values = mono** on all runtime surfaces. No carve-outs for long strings — truncation handles overflow.

### Construct Table

- "Visual Depiction" column split into two: **Base Depiction — Definition (Code)** and **Base Depiction — Runtime (Diagram · Data Form · Timeline)**.
- Rationale: Definition and Runtime are now the two organizing categories for surfaces. Base depiction differs between them — in Code constructs appear as syntax tokens (colour + mono); in Runtime they appear as rendered components (shape, row, arc, chrome).
- Definition pattern updated to match: now defines four things per construct — identity, base depiction (Definition), base depiction (Runtime), and semantic modifiers.
- Key per-construct notes from the split:
  - Structure/Definition: indigo syntax tokens (grammar keywords). Structure/Runtime: indigo framing chrome (borders, headers, rail).
  - State/Definition: violet mono token in declaration. State/Runtime: hard-rect node, violet, mono label — same shape on all runtime surfaces.
  - Event/Definition: cyan mono token in clause. Event/Runtime: directed arc or row with cyan label + directional marker (marker invariant on all runtime surfaces).
  - Data/Definition: name in data colour, type in data-t, no value (values are runtime-assigned). Data/Runtime: three-part row — name (data) · type (data-t) · value (data-v) — mono throughout.
  - Rule/Definition: gold mono token in `because`/`reject` clause. Rule/Runtime: gold message as verdict overlay below field when rule fires.

### Concepts Section Redesign

- Concepts section redesigned as an architectural block diagram — not a specimen matrix. Level of detail is overview/orientation only. No specimens of any kind in this section.
- Three-layer composition top-to-bottom: Constructs (foundation row) → Registers + Surfaces (split layer) → Channels (full-width spanning band). Visual hierarchy encodes the dependency chain.
- Register column ratio 1:3 (Definition : Runtime) encodes the 1-surface (Code) vs 3-surface (Diagram · Data Form · Timeline) structure in layout.
- Channels band placed full-width, spanning both registers — communicates that channels are cross-cutting, not surface-local. Signal precedence language locked in channels header: "colour answers first, form second, layout third, typography throughout."
- Four concept annotation cells restored as prose below the diagram: Semantic Identity, Authored vs Runtime Meaning, Additive Modifiers & Runtime Overlays, Cross-Surface Translation.
- Docs surface: appears in the Concepts section matrix (5 surfaces total), but does not get a full specimen in Section 04 Surfaces. Distinction: Concepts shows all 5 surfaces in the overview; Surfaces section covers the four primary runtime+definition surfaces in depth.
- All old `smx-*` and `sys-matrix`/`sys-channels` CSS classes removed. New prefix: `cov-`.
- Specimens live exclusively in Section 03 (System). Concepts section earns orientation without stealing Section 03's impact.