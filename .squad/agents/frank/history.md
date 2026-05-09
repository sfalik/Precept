## Core Context

- Owns language research, spec wording, and cross-surface architecture documentation for the Precept DSL and runtime.
- Catalog metadata remains the language truth; runtime, tooling, and docs must derive behavior from durable catalog shape rather than enum-identity switch logic or parallel lists.
- Public API surfaces expose stable CLR/JSON interchange contracts; evaluator internals stay internal.
- Investigation docs can be archived once their outcomes are captured in canonical docs, proposals, or the squad decision ledger.

## Learnings

- Typed-literal pre-work is governed by `docs/Working/typed-literal-system-plan.md`, a 12-slice execution plan covering data loaders, parsers, validation framework updates, runtime stubs, and canonical doc sync. Work should not expand beyond that plan.
- Durable typed-literal boundaries are locked: `ContentValidation` remains the catalog hook; compile-time literal validation goes through `TypedConstantValidation.Validate(...)`; runtime Fire/Update/Restore JSON lanes go through `TypeRuntime<T>` / `TypeRuntimeMeta`; format-only validation stays in constraints, not typed literals.
- ISO 4217 and UCUM are external reference datasets, not Precept catalog metadata. They ship as embedded XML resources loaded into typed records, while Precept-owned augmentations like currency symbols stay in source.
- The UCUM evaluator-facing plan fixes are now explicit: `UcumParsedUnit` preserves annotations for display only, Slice 1d uses a two-phase/transitive loader for defined units, and Slice 10 interns runtime `Unit` instances by `(DimensionVector, UcumExactFactor)`.

## Recent Updates

### 2026-05-09T16:55:27Z — UCUM evaluator gap analysis durably merged
- Scribe merged Frank's eight-area UCUM evaluator review into `.squad/decisions.md` and deleted the inbox copy.
- The coordinator-amended plan plus the user directive to stay within the 12-slice plan are now durable squad state.

### 2026-05-09T15:33:49Z - Typed-literal runtime boundary and format-validation scope recorded
- Scribe merged Frank's runtime-arg parsing decision: typed-literal args are parsed through `TypeRuntime<T>` / `TypeRuntimeMeta`, while `TypedConstantValidation.Validate(...)` remains compile-time-only.
- Scribe also recorded Frank's format-validation extensibility ruling: user-defined email/phone/document validation is out of scope for typed literals and should return later as a constraint-level `matches` feature.

### 2026-05-09T15:26:09Z — Typed-literal and UCUM architecture durably recorded
- Scribe merged Frank's typed-literal framework and UCUM parser architecture into `.squad/decisions.md` as the canonical implementation direction.
- Durable boundary: `ContentValidation` stays the metadata hook, `TypedConstantValidation.Validate(...)` stays the static dispatcher, and both temporal + UCUM parsing now have one approved shared-language architecture.

## Historical Summary

- Earlier 2026-05-09 work established the typed-literal direction: runtime JSON ingress reuses `TypeRuntimeMeta.ReadJson`, the unified content-validation framework replaced ad-hoc checker validation, user-defined format validation was explicitly kept out of typed literals, and the comprehensive 12-slice plan became the execution hub.
- The same day also clarified the external-data boundary: ISO 4217 and UCUM are consumed reference datasets rather than language catalogs, `CurrencyCatalog` owns Precept-specific symbol augmentation, and the UCUM parser/data-layer direction moved from placeholder closed sets to a real parser + loader architecture.
- Prior May work locked the catalog-first parser/checker/proof trajectory, required durable rationale capture in research + decisions, and reinforced that canonical docs must track live implementation direction.
- Use `.squad/decisions.md` for the exact per-batch chronology and `docs/` / `research/` for the surviving canonical rationale.
