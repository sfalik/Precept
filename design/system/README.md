# Precept Design System

This folder is the beginning of Precept's design system.

It is separate from `design/brand/`.

- `design/brand/` defines identity: positioning, philosophy, voice, wordmark, brand mark, typography intent, and canonical semantic meaning.
- `design/system/` defines reusable product-surface guidance: shared visual semantics, reusable foundations, and surface specs that apply those rules in concrete UI.

Precept's current design-system work is centered on a semantic visual system rather than a generic component library. The goal is to make states, events, data, verdicts, and runtime signals read consistently across product surfaces without collapsing everything into color alone.

## Authority Boundary

- Brand meaning is owned in `design/brand/`.
- Reusable product-surface guidance is owned in `design/system/`.
- Surface-specific realization belongs in `design/system/surfaces/`.

If a rule changes what a semantic lane means, it belongs in `design/brand/` and requires brand review.

If a rule defines how shared meaning is operationalized across product surfaces, it belongs in `design/system/`.

If a rule only applies to one surface, it belongs in that surface spec and must not redefine shared meaning.

## Initial Structure

- `foundations/` holds shared design-system guidance that multiple surfaces inherit.
- `surfaces/` holds canonical surface specs for concrete product surfaces.

This scaffold is intentionally small. It establishes ownership and migration direction first; deeper token, component, and implementation structure should be added only when the system has real consumers.