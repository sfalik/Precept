### 2026-05-05T04:25Z: User directive — CLR typed collection projections stay in v1 (overrules frank-138 deferral)

**By:** Shane (via Copilot)

**Status:** Directive recorded. Owner overrule of frank-138 deferral recommendation.

**Merged source:** `copilot-directive-clr-collections-keep-v1.md`.

- `IReadOnlyList<T>`, `IReadOnlyDictionary<K,V>`, `KeyedElement<TValue,TKey>`, `PreceptList<T>`, `PreceptLookup<K,V>` stay in v1 public surface.
- §10 prescribed surface spec changes in collection types investigation doc remain valid and were already applied.
- `FieldDescriptor.ClrType` for collection fields must use constructed generics.
