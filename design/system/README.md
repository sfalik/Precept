# Precept Design System

This folder is the beginning of Precept's design system.

It is separate from `design/brand/`.

- `design/brand/` defines identity: positioning, philosophy, voice, wordmark, brand mark, typography intent, and brand-owned semantic meaning.
- `design/system/` defines reusable product-surface guidance: shared visual semantics, reusable foundations, and the current surface-application guidance that operationalizes those rules in concrete UI.

The archived combined spec in `design/brand/archive/2026-04-06-brand-spec-legacy.html` is historical source material only. Current reusable visual-system guidance is owned in [foundations/semantic-visual-system.html](foundations/semantic-visual-system.html).

Precept's current design-system work is centered on a semantic visual system rather than a generic component library. The goal is to make states, events, data, verdicts, and runtime signals read consistently across product surfaces without collapsing everything into color alone.

Canonical system output: [foundations/semantic-visual-system.html](foundations/semantic-visual-system.html)

## Authority Boundary

- Brand meaning is owned in `design/brand/brand-spec.html`.
- Reusable product-surface guidance is owned in `foundations/semantic-visual-system.html`.
- Current surface-application guidance is folded into `foundations/semantic-visual-system.html` until a later split is warranted.

Ownership boundary:

- Peterman owns brand meaning.
- Elaine owns reusable system guidance.
- Frank arbitrates boundary questions.

If a rule changes what a semantic lane means, it belongs in `design/brand/` and requires brand review.

If a rule defines how shared meaning is operationalized across product surfaces, it belongs in `design/system/`.

If a rule only applies to one surface, keep it local to that surface definition and do not let it redefine shared meaning.

## Initial Structure

- `foundations/` holds the canonical semantic visual system document, including shared rules and the current surface-application guidance.

Current foundations source of truth:

- [foundations/semantic-visual-system.html](foundations/semantic-visual-system.html) is the canonical output for the shared semantic visual model, reading order, primitives, current surface translation guidance, and cross-surface compliance rules.

Prototype composition experiment:

- [foundations/semantic-visual-system-composed.html](foundations/semantic-visual-system-composed.html) is a non-canonical generated prototype that tests a hybrid markdown plus HTML-islands authoring model.
- [foundations/source/README.md](foundations/source/README.md) documents the prototype source tree under `foundations/source/` and the local builder at `tools/scripts/build-semantic-visual-system.mjs`.

This scaffold is intentionally small. It establishes ownership and migration direction first; a separate surface-spec split can be reintroduced later when concrete surfaces need durable standalone specs.