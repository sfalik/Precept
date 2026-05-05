### 2026-05-05T04:11Z: User directive — Raw lane = JSON lane; `PreceptValue` never leaks public API

**By:** Shane (via Copilot)

**Status:** Directive recorded. Resolves B2 conflict between surface spec and CC#25 Q7.

**Merged source:** `copilot-directive-2026-05-05T00-11-43.md`.

- The raw lane is the JSON lane. Raw lane public indexer (`version["fieldName"]`) returns `JsonElement`, not `PreceptValue`.
- `PreceptValue` is a strictly internal type and must NEVER appear in any public method signature, return type, property type, or generic constraint.
- This overrides any prior inbox reference to `PreceptValue` as the raw lane return type (including the stale Fix 3a applied to the collection types investigation).
