# Design

This folder contains Precept's living design source material.

It sits beside `docs/`, not under it.

- `design/brand/` defines Precept's identity: philosophy, narrative, voice, mark logic, typography intent, and canonical semantic meaning.
- `design/system/` defines reusable product-facing visual guidance: shared visual semantics, foundations, and surface specs.
- `design/prototypes/` holds durable design prototypes and experiments that are important enough to preserve outside code-local workspaces.

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