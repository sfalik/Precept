## Core Context

- Owns UX/design across README form, semantic visual system, preview surfaces, and domain-author-facing language/diagnostic wording.
- Keeps visual and wording decisions aligned with runtime semantics rather than tool-implementation jargon.
- Historical summary: shaped README/brand presentation, review method for design docs, and author-centered diagnostic language.

## Learnings

- When the type system already disambiguates, surface keywords should stay short, shared, and author-friendly.
- Design docs should be decision-led and problem-first; tables should stay only where they truly compare alternatives or dimensions.
- Progressive disclosure beats uniform metadata tables for long technical docs.
- Diagnostics must use domain-author language, not compiler jargon.
- The most common first-contact failures deserve specific, fix-oriented diagnostics rather than umbrella error messages.
- Preview concepts and README treatments need validation against realistic sample complexity, not only polished toy examples.
- Verdict cues work best as subtle authored-intent signals layered beneath runtime outcomes.
- Emerald/Amber/Rose remain runtime-semantic colors; Gold stays a restrained brand accent.
- Grammar hierarchy diagrams read best as plain ASCII reference tables (family → construct → syntax), not Mermaid flow graphs: syntax examples are too long for Mermaid node labels, and circled-number callouts (① ②) communicate cross-cutting asymmetries more cleanly than graph edges. Chose a three-column tabular ASCII layout with `─►` family anchors and numbered footnotes for actions and outcomes.
- When a diagram uses the same visual form (encircled digits) for structurally distinct things — grouping headers vs. per-construct badges — readers can't tell them apart at a glance. The fix is to pick visually distinct *classes* of symbol: a solid shape (`◆`) for structural sub-headers, and bracket-enclosed letters (`[A]`, `[O]`) for per-construct annotation badges. The bracket form keeps action and outcome visually in the same family (both are slot badges) while the diamond clearly signals "this is a grouping row, not a construct row."


## Recent Updates

### 2026-05-02 — Grammar hierarchy HTML prototype created
- Built a fully self-contained dark-mode HTML reference at `design/prototypes/grammar-hierarchy.html`.
- Used brand-spec color tokens throughout: gold (Header family), indigo (Direct), purple (StateScoped), cyan (EventScoped), emerald (actions), rose (outcomes).
- TransitionRow is visually featured with an expanded sub-section showing all 15 action verbs (grouped by collection type) and all 3 outcome variants inline.
- Three asymmetry callout cards at the bottom surface the key design facts: actions in 3 constructs, outcomes only in TransitionRow, and `no transition` = 2 tokens · 1 outcome.
- Disambiguation tokens (`ensure`, `modify`, `omit`, `→`) are shown as inline badges on each construct name row, giving parser writers an at-a-glance routing map.
- Key reusable pattern: table-cell layout (display:table) gives reliable column alignment across constructs without flex or grid complexity; TransitionRow breaks out of the table into a block-level expansion for its sub-elements.

### 2026-05-02 — Grammar hierarchy diagram added to parser-radical.md
- Added §0.9 Grammar Hierarchy as a plain ASCII three-column reference table covering all 4 routing families, 12 constructs, 15 action verbs, and 3 outcome variants. No prior HTML prototype existed to clean up.
- Genre and readability recommendations were applied to the combined design document, and the reusable patterns were captured durably for later design work.

### 2026-04-24 — docs.next navigation review approved
- Confirmed the README and pipeline docs form a strong navigation layer with only minor structural nits.

### 2026-04-18 — Proof engine hover UX review
- Approved the direction but blocked on natural-language interval rendering, diagnostic-template cleanup, and hover-attribution formatting.

### 2026-05-03T01:07:30Z — Grammar hierarchy closeout recorded
- Scribe logged Elaine-2's §0.9 Grammar Hierarchy pass: the durable reference stays a plain ASCII table spanning routing families, constructs, action groups, outcome variants, and the ①② asymmetry callouts.
- Durable presentation rule: when grammar examples are long and asymmetries cut across rows, ASCII reference tables communicate more clearly than Mermaid-style node graphs.

### 2026-05-03T01:07:30Z — Grammar-hierarchy icon revision recorded
- Scribe merged Elaine's follow-on §0.9 icon note: `◆` is the structural `ConstructFamily` anchor marker, while `[A]` / `[O]` are per-construct slot badges.
- Durable presentation rule: do not reuse the same visual icon family for grouping rows and annotation badges; distinct semantic roles need distinct visual classes.
