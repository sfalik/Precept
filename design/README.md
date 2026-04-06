# Design

This folder contains Precept's living design source material.

It sits beside `docs/`, not under it.

The archived combined spec in `design/brand/archive/2026-04-06-brand-spec-legacy.html` is historical source material only. It is not the current authority for brand meaning or reusable visual-system guidance.

- `design/brand/` defines Precept's identity: philosophy, narrative, voice, mark logic, typography intent, and canonical semantic meaning.
- `design/system/` defines reusable product-facing visual guidance: shared visual semantics, foundations, and the current surface-application guidance.
- `design/prototypes/` holds durable design prototypes and experiments that are important enough to preserve outside code-local workspaces.

## Authority Path

- `design/brand/brand-spec.html` is the canonical brand-side output and owns brand identity, canonical semantic meaning, why the semantic visual system matters to Precept as a brand, and the brand/system ownership boundary.
- `design/system/foundations/semantic-visual-system.html` is the canonical system-side output and owns reusable visual-system guidance plus the current surface-application guidance.

Ownership boundary:

- Peterman owns brand meaning.
- Elaine owns reusable system guidance.
- Frank arbitrates boundary questions.

## Research Storage

Research belongs with the domain that owns the decision.

- Brand research goes in `design/brand/research/`.
- Brand precedent and source captures go in `design/brand/references/`.
- Critiques of specific brand artifacts go in `design/brand/reviews/`.
- Design-system and UX research goes in `design/system/research/`.
- Design-system precedent and source captures go in `design/system/references/`.
- Critiques of specific system artifacts or surface drafts go in `design/system/reviews/`.

Use this distinction consistently:

- Research = evidence and synthesis that ends in a recommendation.
- References = source material, precedents, captures, and examples.
- Reviews = critique of a specific artifact or draft.
- Specs = approved rules builders should follow.

Use this rule when deciding where something belongs:

- If it defines identity or brand meaning, it belongs in `design/brand/`.
- If it defines reusable visual or interaction rules across product surfaces, it belongs in `design/system/`.
- If it is exploratory but worth preserving, it belongs in `design/prototypes/`.
- If it explains, plans, or justifies design decisions in prose, it belongs in `docs/`.

If the document is evidence for a design decision, keep it in the owning design domain rather than in a global research pile.

This structure keeps source-of-truth design material separate from technical documentation while reducing top-level clutter.