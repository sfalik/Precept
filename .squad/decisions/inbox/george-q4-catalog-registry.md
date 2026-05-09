# George Q4 — catalog registry alignment

- Treat curated business-domain surfaces as registry-pattern APIs: expose `All: FrozenDictionary<string, TEntry>` for canonical entries and keep plural/alias lookup in separate compatibility helpers.
- `TemporalUnits.All` is keyed by singular unit name, while `TryGet(...)` continues to resolve singular + plural spellings through an alias map.
- `precept_language` now surfaces domain registries in a dedicated `domains` object (`currencies`, `ucumTier1Units`, `dimensions`, `temporalUnits`) mapped directly from runtime registries instead of mirrored MCP-side lists.
- Curated UCUM Tier 1 entries may need synthesized registry entries for parse-only forms (for example compact exponent codes like `m2` and unity `1`); `LookupAtom` remains backed by the full `UcumAtomCatalog`.
