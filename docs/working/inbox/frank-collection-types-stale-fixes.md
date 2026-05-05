### 2026-05-05T04:05Z: Collection types investigation — stale fixes applied

**By:** Frank

**Status:** Applied.

**Merged source:** `frank-collection-types-stale-fixes.md`.

- **Fix 1 — §5 namespace:** `namespace Precept.Runtime` → `namespace Precept.Types` for `KeyedElement<TValue, TKey>` declaration. Authority: `frank-type-library-assembly.md`.
- **Fix 3a — §15 raw lane return type:** `version["fieldName"]` return type changed from `JsonElement` to `PreceptValue`. *(Note: this was subsequently overruled by Shane's raw lane = JsonElement directive — see entry below. The investigation doc will need a further correction.)*
- **Fix 3b — §15 `IReadOnlyLog<T>` removal:** Speculative option removed; replaced with settled decision text (`log` → `IReadOnlyList<TElement>`).
- Post-edit grep confirmed zero remaining `IReadOnlyLog`, `JsonElement`, or `Precept.Runtime` references in the document.
