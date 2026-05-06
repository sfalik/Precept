# Core Context

- Owns language research, spec wording, and cross-surface architecture documentation for Precept's DSL and runtime.
- Catalogs remain the language truth; runtime, tooling, and docs derive behavior from metadata and shape rather than hardcoded enum identity or parallel lists.
- Public API surfaces must expose stable CLR/JSON interchange types; evaluator internals stay internal and never leak into the durable surface.
- Operation legality lives in `Language.Operations`; computation lives in `TypeRuntime` plus the runtime dispatch registry. The evaluator stays zero-knowledge.
- Identity-type work follows the dual-shape rule: enriched internal entities when metadata/lifetime demands it, lightweight API-boundary code/value shapes when callers need stable interchange.
- Collection internals are settled around universal `PreceptValue[]` backing, stride-2 pair storage, static `CollectionActions` helpers, and evaluator-owned copy-on-write.
- Collection CLR adapters are lazy at the `Version` level and eager on first materialization, not per-index lazy.
- Working docs drift quickly during heavy deliberation; canonical docs and squad records must be synchronized as soon as a decision locks.
- Investigation docs may be archived once their outcomes are captured in canonical docs, proposals, or the squad decision ledger.

## Learnings

- Mini-spec track complete (2026-05-05): `docs/working/runtime-api-public-surface-spec.md` §3.4 was corrected to align with the locked value-type surface (`currency`, `money`, and `quantity` fixed; internal `Unit` removed; `unitofmeasure`, `dimension`, `price`, and `exchangerate` added), and §12 OQ-3d/OQ-3e now reflect the resolved shapes.
- Completed mini-spec track: wrote the full inbox drop covering all 17 locked §11 runtime-API decisions plus the OQ resolutions, type contracts, visibility decisions, and supersession markers needed to retire stale `RestoreOutcome`, `ConstraintViolation.FailingValue`, and raw-indexer guidance.
- Inbox completeness audit (2026-05-05): the existing inbox batch targeted the value-types investigation rather than the runtime API mini-spec. Treat wrong-session inbox drift as a real failure mode and verify the target artifact before assuming the inbox is complete.
- Key rule: spec tables must sync to `docs/language/business-domain-types.md` immediately when value-types decisions lock. Do not defer CLR-shape table cleanup to a later reconciliation pass.
- Value-types investigation archival is complete: `docs/working/precept-value-types-investigation.md` can be retired once its outcomes are migrated, and the archived research copy under `research/archive/` must be preserved in commits for provenance.
- `PreceptValue` stays internal-only. Public raw lanes use `JsonElement`, and `Get<string>()` is the universal canonical-string lane for supported value types.
- Next up: apply 41 canonical doc fixes — `runtime-api.md` (18 findings), `result-types.md` (11), `descriptor-types.md` (4), `evaluator.md` (4), `precept-builder.md` (5), and `business-domain-types.md` (5).

## Recent Updates

### 2026-05-05 - Severity audit of 41 canonical doc gaps

- Cross-referenced all six canonical runtime/language docs against `docs/working/runtime-api-public-surface-spec.md` §§1–10 (post frank-152) and the 17 locked §11 design decisions.
- Result: **22 MAJOR, 12 MINOR, 13 RESOLVED** across 41+ atomic findings (F-001–F-029, some slots multi-gap).
- `runtime-api.md` is the highest-urgency target: 10 MAJOR gaps including the missing `FromJson` migration, stale `Fields`/`Events`/`AvailableEvents` return types, absent Typed Lane and CLR Discovery sections, and four wrong variant names from decisions #12–#17.
- `result-types.md` is second: 7 MAJOR gaps — all DU variants shown top-level (not nested), three Axiom 1 `PreceptValue` leaks, and all four UpdateOutcome/EventOutcome renames unapplied.
- `descriptor-types.md`: 2 MAJOR (both `ClrType` additions missing on `FieldDescriptor` and `ArgDescriptor`); small doc, fast fix.
- `evaluator.md`: 2 MAJOR (stale variant names in §5 output definitions, `RestoreOutcome` still defined); 2 MINOR (OQ blocks, `BecauseClause` rename).
- `precept-builder.md`: 1 MAJOR (Pass 1 descriptors missing `ClrType`); OQ blocks and naming are MINOR.
- `business-domain-types.md`: All 5 findings resolved — doc was correct, frank-152 fixed the spec to match.
- Full structured report delivered to `.squad/decisions/inbox/frank-severity-audit-2026-05-05.md`.

### 2026-05-05 - Mini-spec remediation closed out

- Fixed the runtime API mini-spec's stale CLR table and OQ references, then produced `frank-minispec-decisions-2026-05-05.md` so Scribe could merge a complete ledger record for the runtime API surface.
- The follow-on completeness audit (`frank-inbox-completeness-audit-2026-05-05.md`) documented 27 gaps, highlighted the three stale decisions that needed supersession, and confirmed the inbox/session mismatch that caused the drift.

### 2026-05-05 - Value-types investigation archived after migration

- Audited `docs/working/precept-value-types-investigation.md` section by section against canonical docs, migrated the remaining durable content, and moved the investigation to `research/archive/precept-value-types-investigation.md`.
- Canonical value-type guidance now uses CLR-shape language, retains the internal/public dual-shape boundary, and records the locked currency/unit/dimension/price/exchange-rate contracts.

### 2026-05-04 - Runtime API surface locks

- Public persistence naming is `Version.ToJson()` / `Precept.FromJson(JsonElement)`.
- `FromJson` returns `Version` directly; restore is not a business-outcome DU.
- Raw public value lanes use JSON (`JsonElement`), never `PreceptValue`.

### 2026-05-05 - Canonical runtime doc completion pass

- Completed the five-doc canonical runtime documentation completion pass across `docs/runtime/result-types.md`, `docs/runtime/descriptor-types.md`, `docs/runtime/runtime-api.md`, `docs/runtime/evaluator.md`, and `docs/runtime/precept-builder.md`.
- Applied all 34 severity-audit findings assigned to the pass, including locked decisions #9-#17.
- Archived the mini-spec into `docs/working/archive/` after the canonical sync landed.
- Commit: `01bfbd0`.

### Historical summary through 2026-05-05

- 2026-05-04 established the execution/runtime baseline: catalogs describe legality, `TypeRuntime` plus runtime registries own computation, and the evaluator remains type-agnostic.
- Collection API design converged on CLR-friendly adapters and declared-direction storage while keeping internal storage on `PreceptValue[]`.
- Currency, unit, and dimension work converged on catalog-backed identity types with clear public/internal shape boundaries.
- Use `.squad/decisions/decisions.md` for full per-decision provenance; keep `history.md` focused on durable operating context and the newest closures.
