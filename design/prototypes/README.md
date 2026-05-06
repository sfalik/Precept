# Prototypes

This folder is for durable design prototypes and explorations that should live outside code-local tool folders.

Use it for prototypes that are referenced repeatedly, compared across iterations, or preserved as design artifacts.

The current historical prototype set now lives under `archive/`. Keep this root focused on actively referenced prototype work only.

This root is intentionally quiet. It may contain only this README plus `archive/` for stretches of time. That is the expected state when there is no active durable prototype set that needs top-level visibility.

Use the split this way:

- `design/prototypes/` root: active durable prototypes, orientation files, and small comparison sets that are still part of current design work.
- `design/prototypes/archive/`: preserved reference artifacts that should remain available but are no longer active top-level work.

Do not move hot, implementation-near prototypes here prematurely. While a prototype is tightly coupled to an active code surface, it may stay near that tool. Promote it here once it becomes a durable design reference.