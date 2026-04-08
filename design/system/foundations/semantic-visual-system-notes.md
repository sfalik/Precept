# Semantic Visual System Notes

Purpose: lightweight working notes for discussion around a consolidated definition of Precept's semantic visual system.

## Captured Intent

- Brainstorm first; no drafting yet.
- This should become a strong consolidated definition of Precept's semantic visual system.
- The final HTML should be beautiful and should dogfood the design system so it embodies Precept.
- Design conviction: rigorous enough to govern the system, expressive enough to prove it.
- Working definition: the semantic visual system defines how Precept means things on screen. It gives structure, states, events, data, explanatory rule language, and runtime verdicts a stable visual grammar so meaning stays recognizable across authoring, topology, preview, diagnostics, and public explanation. The point is not decoration; the point is faster recognition, lower confusion, and a surface language that feels unmistakably Precept.
- Presentation priority: the document itself should be highly visual, beautiful, and inspiring. It should embody the semantic visual system and the concepts and philosophy of Precept rather than reading primarily like governance or change-control documentation.
- Opening tone preference: the document should not begin by justifying itself. Avoid section framing like "Why This System Exists" for the opening.
- Structure preference: do not force a hard top-level split between core constructs and runtime meaning if most semantic variants only make sense through live behavior. The construct galleries should be allowed to include live semantic variants and modifiers together with the base depiction.
- Reader assumption: this document is not the place to teach Precept from first principles. It can assume the reader already understands Precept at a conceptual and runtime level; readers who need that foundation should use other docs first.
- Document ambition: this should read as a visual canonical artifact and tour de force, not as a product-model primer.
- Section 2 intent: this is not a vocabulary refresher or general Precept recap. It should introduce concepts that are specific to the semantic visual system itself and are not defined elsewhere.

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
2. Visual depiction: the consistent base form used across surfaces.
3. Semantic modifiers: additive visual signals layered onto the base form to represent specific meanings.

Modifier rule: modifiers are additive rather than replacement-based. Examples include an initial-state dot or a terminal-state double border.

### First-pass construct table

| Construct | Identity | Visual Depiction | Semantic Modifiers |
|---|---|---|---|
| Structure | Not applicable | Indigo family | None |
| State | State name | Violet family | initial, terminal, constrained, reachableFromInitial, reachableFromCurrent, current |
| Event | Event name | Cyan family | transition, self-transition, no-transition, rejected, constraint failure, unmatched |
| Data | Field name | Three tonal shades: name / type / value | editable, constrained, violated, required |
| Rule | Rule message text (the because / reject string) | Gold `#FBBF24` — the message is the visual interrupt | satisfied, violated |

### Team research synthesis

- The document should stay construct-first rather than surface-first. Surfaces matter, but they should be framed as translations of stable semantic meaning rather than as independent mini-systems.
- Authored meaning and runtime outcome must stay separate. Core construct identity should remain stable, while verdicts act as overlays rather than replacements.
- The system document should not become a second brand spec. Brand owns semantic meaning at the identity level; the system document owns reusable cross-surface operationalization.
- The system document should also not collapse into surface-local implementation detail. It should define shared semantic law first, then show how surfaces realize that law.
- The strongest throughline is user recognition: faster reading, lower confusion, and stable meaning across editor, diagram, form, timeline, diagnostics, and explanation.
- The signal system should be treated as a grammar with precedence and combination rules, not just a palette inventory.
- AI readability and structured inspectability are part of the system contract, especially for runtime and diagnostic surfaces.
- The user wants the main document to privilege visual embodiment and inspiration over governance framing. Governance and authority concerns should not dominate the main narrative flow.

### Boundary mistakes to avoid

- Re-merging brand identity, reusable system guidance, and surface-local realization into one authority layer.
- Letting runtime states or surface specimens harden into new core semantic categories.
- Using verdict colour to replace base construct identity instead of overlaying it.
- Reopening exploratory palette drift or surface-local styling drift inside the canonical system document.

### Current rough document shape

- Intro
- Concepts
- System
- Surfaces

This appears to be the user's preferred high-level compression for the document structure.

#### Intro

- Open with "The Semantic World of Precept" or a close variant.
- Present the high-level visual thesis: Precept has a stable semantic world that can be read across surfaces.
- Introduce the semantic cast at a very high level only, without teaching general Precept mechanics.
- Use a strong hero composition that immediately shows the system's beauty and coherence.

#### Concepts

- Define concepts that belong specifically to the semantic visual system and are not defined elsewhere.
- Define the distinction between core constructs and adjacent semantic categories.
- Define semantic identity, base depiction, additive modifier, overlay, and cross-surface translation.
- Define the four signal channels as concepts of the semantic visual system: colour, typography, form, and layout.
- Define cross-surface invariance: the same meaning persists even when its realization changes by medium.
- Clarify the conceptual distinction between authored identity and runtime-applied meaning.
- Define surfaces as translations of shared meaning rather than separate semantic authorities.

#### System

- Define the canonical visual identity of each core construct: Structure, State, Event, Data, Rule.
- For each construct, show its base depiction and a curated gallery of meaningful semantic variants.
- Show how additive modifiers and overlays attach to the base depiction without replacing core identity.
- Include live variants where they are necessary to understand the construct honestly.
- Show how adjacent semantics such as transitions and verdicts relate to the core constructs without becoming core constructs themselves.
- Define the concrete signal behavior of the system through the construct galleries: colour, typography, form, layout, and their combination rules in practice.
- Make clear what remains stable, what is additive, and what must never replace base identity.

#### Surfaces

- Show the semantic visual system in action across the main surfaces.
- Lead with the code window as the hero surface.
- Cover the state diagram as the topology and flow-reading surface.
- Cover data forms as the field and constraint-reading surface.
- Cover timeline or event-flow views as the event progression and runtime-reading surface.
- Cover explanatory/docs as the translational and communicative surface.
- For each surface, show what remains invariant, what adapts to the medium, and how the same semantic language remains recognizable.