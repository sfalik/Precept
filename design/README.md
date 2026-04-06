# Design

This folder contains Precept's living design source material.

It sits beside `docs/`, not under it.

- `design/brand/` defines Precept's identity: philosophy, narrative, voice, mark logic, typography intent, and canonical semantic meaning.
- `design/system/` defines reusable product-facing visual guidance: shared visual semantics, foundations, and surface specs.
- `design/prototypes/` holds durable design prototypes and experiments that are important enough to preserve outside code-local workspaces.

Use this rule when deciding where something belongs:

- If it defines identity or brand meaning, it belongs in `design/brand/`.
- If it defines reusable visual or interaction rules across product surfaces, it belongs in `design/system/`.
- If it is exploratory but worth preserving, it belongs in `design/prototypes/`.
- If it explains, plans, or justifies design decisions in prose, it belongs in `docs/`.

This structure keeps source-of-truth design material separate from technical documentation while reducing top-level clutter.