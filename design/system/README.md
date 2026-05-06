# Precept Design System

This folder is the beginning of Precept's design system.

It is separate from `design/brand/`.

- `design/brand/` defines identity: positioning, philosophy, voice, wordmark, brand mark, typography intent, and brand-owned semantic meaning.
- `design/system/` defines reusable product-surface guidance: shared visual semantics, reusable foundations, and the current surface-application guidance that operationalizes those rules in concrete UI.

The archived combined spec in `design/brand/archive/2026-04-06-brand-spec-legacy.html` is historical source material only. Current reusable visual-system guidance is owned in [semantic-visual-system.html](semantic-visual-system.html).

Precept's current design-system work is centered on a semantic visual system rather than a generic component library. The goal is to make states, events, data, verdicts, and runtime signals read consistently across product surfaces without collapsing everything into color alone.

Canonical system output: [semantic-visual-system.html](semantic-visual-system.html)

Canonical artifacts in this folder stay here even if related prototypes exist elsewhere. `design/prototypes/` is for exploration and preserved specimen sets; it should not become a second source of truth for the system.

## Authority Boundary

- Brand meaning is owned in `design/brand/brand-spec.html`.
- Reusable product-surface guidance is owned in `semantic-visual-system.html`.
- Current surface-application guidance is folded into `semantic-visual-system.html` until a later split is warranted.

Ownership boundary:

- Peterman owns brand meaning.
- Elaine owns reusable system guidance.
- Frank arbitrates boundary questions.

If a rule changes what a semantic lane means, it belongs in `design/brand/` and requires brand review.

If a rule defines how shared meaning is operationalized across product surfaces, it belongs in `design/system/`.

If a rule only applies to one surface, keep it local to that surface definition and do not let it redefine shared meaning.

## Current Structure

- The canonical semantic visual system artifacts now live directly under `design/system/`.
- The current canonical set is `semantic-visual-system.html`, `semant-visual-system-canonical.precept`, and `semantic-visual-system-notes.md`.
- Prototype specimens may reference these files, but the canonical files themselves stay in `design/system/`.

Current source of truth:

- [semantic-visual-system.html](semantic-visual-system.html) is the canonical output for the shared semantic visual model, reading order, primitives, current surface translation guidance, and cross-surface compliance rules.

This scaffold is intentionally small. It establishes ownership and migration direction first; a separate surface-spec split can be reintroduced later when concrete surfaces need durable standalone specs.