## Core Context

- Owns UX/design across README form, semantic visual system, preview surfaces, and domain-author-facing language/diagnostic wording.
- Keeps visual and wording decisions aligned with runtime semantics rather than tool-implementation jargon.
- Historical summary: shaped README/brand presentation, review method for design docs, and author-centered diagnostic language.

## Learnings

- Grammar design reference documents for a DSL need to lead with what the grammar is NOT before explaining what it is — this immediately orients the reader who may arrive with assumptions from general-purpose language design. The "not expression-based / not indentation-sensitive / not context-dependent" framing prevents the most common misreadings.
- The flat-construct / keyword-anchored / named-slot triad is best understood as three mutually reinforcing properties, not three independent features. Each one enables the other two: flatness makes keyword anchoring tractable; keyword anchoring makes named slots necessary; named slots make flatness readable. Framing them together as "the core design choices" is clearer than listing them separately.
- The linguistic-model section (nouns/verbs/adjectives/prepositions) is the most powerful bridge between the formal grammar description and the business-domain audience claim. Grounding each linguistic role in concrete Precept examples makes the claim testable: readers can verify by reading a `.precept` file aloud.
- Grammar invariants belong in a dedicated section for language designers — not scattered through the philosophy or the construct descriptions. Collocating all six invariants with their "what breaks if violated" rationale makes the document actionable for the next person proposing a language change.
- The catalog-as-grammar section should emphasize the architectural inversion explicitly ("the catalog IS the grammar — not a reflection of it") before showing the mechanics. Without the inversion framed clearly, the catalog table reads as documentation rather than as the central design claim.
- A quick-reference appendix at the end (construct-to-family mapping, slot-kind to expression-type mapping, invariants at a glance) doubles the document's utility for people who already know the material and just need a lookup surface.
- Target audience framing for a design reference is critical: writing for "people building the language" vs. "people using the language" produces very different tone and emphasis. The document should assume familiarity with compiler concepts (Pratt parser, disambiguation, catalog-driven dispatch) and not explain them from scratch.

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

### 2026-05-03T14:28:59Z — Grammar reference fixes recorded
- Elaine-6 fixed the ASCII box right-wall alignment in `docs/language/precept-grammar.md`; all diagram rows now hold a 78-character width contract. Commit `9a3b657`.
- Elaine-7 replaced the “five constructs” section with six principled examples spanning the major slot/routing shapes and added a computed-field `-> expression` example. Commit `fc54bac`.
- Elaine-8 remains in flight verifying that the grammar doc's `in` disambiguation tokens (`readonly` / `editable`) still match the Constructs catalog.

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

### 2026-05-03T09:55:27Z — Grammar design reference created

- Created `docs/language/precept-grammar.md` as the language design reference for Precept developers and language designers.
- Document covers: grammar philosophy (flat/keyword-anchored/named-slots), the four-level hierarchy (File → Constructs → Slots → Expressions), the 12 construct kinds, the four routing families and disambiguation protocol, the 15 slot kinds, expression placement constraints, the linguistic model, the six grammar invariants, and the catalog-as-grammar-spec principle.
- Used ASCII hierarchy trees, annotated construct anatomy diagrams, and slot-sequence tables throughout. Quick-reference appendix added for lookup use cases.

### 2026-05-03T09:59:25Z — Grammar design reference confirmed complete

- Reviewed `docs/language/precept-grammar.md` against full task brief; confirmed all required sections are present and authoritative.
- Design principles (flat/combinator-friendly, IntelliSense-friendly, AI-authoring-friendly, noun-verb-adjective grammar, consistency as a value) are effectively distributed across §1, §7, §8, and §9 rather than collected in one explicit "Grammar Design Principles" section. The content is complete; future maintainers may wish to consolidate into a dedicated principles section if the document is extended.
- Decisions inbox (`elaine-grammar-doc.md`) and learnings already captured from prior session; no duplicate entries needed.

### 2026-05-03T14:02:40Z — Grammar reference batch logged

- Elaine-4's grammar-doc pass is now durably recorded: `docs/language/precept-grammar.md` is the active grammar design reference, with negative-first orientation, the flat/keyword-anchored/named-slot spine, dedicated linguistic-model + invariants sections, and a quick-reference appendix.
- Elaine-5's duplicate verification adds no new work item; treat the current grammar doc as complete unless a later scope expansion justifies consolidating the distributed design-principles content into a dedicated section.
