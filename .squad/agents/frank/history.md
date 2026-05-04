## Core Context

- Owns language research, spec wording, and cross-surface architecture documentation for Precept's DSL and runtime.
- Durable baseline: catalogs remain the language truth; consumers derive behavior from metadata/shape instead of hardcoded enum identity or parallel lookup tables.
- Active runtime baseline: execution stays on PreceptValue, slot arrays, eager execution plans, and catalog-owned dispatch; public ingress/egress should expose stable CLR/JSON types rather than evaluator internals.
- Canonical docs state what the system is; squad records carry provenance, decision lineage, and working-status detail.

## Learnings

- Discovery answers belong on the descriptor or metadata object the consumer already holds, not behind a second lookup surface.
- Stable public API surfaces should expose CLR/JSON interchange types; leaking internal evaluation machinery into public signatures turns internal refactors into breaking changes.
- When a design locks, immediately replace provisional wording in canonical docs and move the rationale/provenance back into squad records.
- Audit-gap reports go stale quickly in a fast-moving doc set; re-verify the canonical docs before trusting a rollup.
- Registration or dispatch behavior that varies by language member is metadata, not incidental code structure, and should live in the catalog system.
- Philosophy prose is strongest when it opens with ontological certainty about the compiled artifact rather than with mechanism descriptions.

## Recent Activity

### Historical summary through 2026-05-04T09:26:32Z
- Consolidated the late-April and early-May architecture corpus: parser/catalog remediation, runtime/evaluator CC#25 design closure, gap-register migration, canonical doc sync across runtime/proof/catalog surfaces, and the rule that canonical docs stay factual while squad records preserve decision provenance.

### 2026-05-04T10:15:18Z — Public API surface mini-spec authored
- Wrote docs/working/runtime-api-public-surface-spec.md to lock the redesigned public API surface around typed descriptors, TryGet<T>() on FiredArgs, FieldSnapshot.ClrType, and a pre-1.0 atomic break away from public PreceptValue leakage.

### 2026-05-04T15:15:33Z — Philosophy review notes landed in the locked v6 copy
- Frank's v4 philosophy review was approved with notes and is now durably reflected in the locked v6 text path: prevention is framed as structural impossibility, Precept's own nouns replace stray implementation jargon, and the final document addresses developers as adopters/builders rather than speaking directly to domain experts.

### 2026-05-04T15:32:34Z — Unit-type investigation and mini-spec OQ closeout recorded
- Captured the UCUM-backed quantity-system recommendation set in `docs/working/unit-type-system-investigation.md`, including database-driven metadata, tiered discovery, and the currency-separation rule for `MoneyValue`.
- Recorded the runtime API mini-spec closeout: `integer` = `long`, `duration` = NodaTime `Duration`, descriptor-driven collection wrapping, and `PreceptList<T> : IReadOnlyList<T>` as the collection projection surface.

### 2026-05-04T16:20:24Z — Persistence API naming lock recorded
- Naming pass on `docs/working/runtime-api-public-surface-spec.md` finalized the public persistence pair as `Version.ToJson()` / `Precept.FromJson(JsonElement document)`.
- Scribe merged 7 Frank inbox files into 4 canonical decisions covering the final names, envelope shape, direct `FromJson` return contract, and read-path semantics, then cleared the inbox.
