## Core Context

- Owns code-level feasibility, parser/runtime implementation detail, and architecture-to-code translation across checker, analyzer, and tooling surfaces.
- Parser and checker work stay catalog-derived, array-primary where order matters, and hostile to mirrored duplicate state.
- Shared-environment build discipline matters: targeted build/test commands are safer than full-solution runs when the workspace may have external file locks.

## Learnings

- `CurrencyCatalog` is now the durable ISO 4217 surface: a `FrozenDictionary<string, CurrencyEntry>` keyed case-insensitively by alpha code, with `CurrencyEntry` carrying `AlphaCode`, `NumericCode`, `Name`, and `MinorUnit`.
- Currency validation should derive from `CurrencyCatalog.All.Keys.ToFrozenSet(StringComparer.OrdinalIgnoreCase)` so runtime/type surfaces consume the catalog instead of a mirrored hardcoded set.
- Current catalog scope is ISO 4217 transactional currencies: withdrawn `ANG`, `BGN`, and `ZWL` are removed, while precious metals (`XAU`, `XAG`, `XPT`, `XPD`), placeholders/tests (`XTS`, `XXX`), and fund/accounting-unit codes (`BOV`, `CHE`, `CHW`, `CLF`, `COU`, `MXV`, `USN`, `UYI`, `UYW`, `VED`, `XAD`, `XCG`, `ZWG`) stay excluded.
- `CurrencyCatalogSyncTests` should encode XML-only exclusions with a case-insensitive `IntentionalExclusions` `FrozenSet<string>` and apply it only to `xmlCodesNotInCatalog`; `catalogCodesNotInXml` stays unfiltered so withdrawn catalog entries still fail fast.
- ISO refresh posture is manual and tooling-backed: the workspace task `iso4217: refresh` pulls current XML, while `CurrencyCatalogSyncTests` validates catalog drift when the XML fixture is present.
- `CurrencyCatalog.All.Keys` returns `IEnumerable<string>` (not `FrozenSet<string>`); calling `.ToFrozenSet(StringComparer.OrdinalIgnoreCase)` on it produces the correct case-insensitive set for `ClosedSetValidation.AllowedValues`.
- Final `CurrencyCatalog` exclusion policy: only transactional currencies used in business workflows belong in the catalog; commodities, fund codes/accounting units, and ISO placeholder/testing codes belong in `CurrencyCatalogSyncTests.IntentionalExclusions`.
- ProofEngine binary-op subject resolution must check RHS before LHS when shared `ParameterMeta` instances are reused, otherwise divisor/nonzero requirements can bind to the wrong operand.
- Proof forwarding facts must be consumed before the discharge loop, and discharge must skip already-proved obligations to avoid overwriting proved facts with `Unresolved`.
- Graph terminal-state analysis must ignore `Reject` / `NoTransition` self-edges; a terminal state that only rejects events is honoring the contract, not violating it.
- Error propagation in the TypeChecker should return `TypedErrorExpression` without layering extra parent diagnostics, but the original error source must emit the TC diagnostic so D26 self-containment holds.
- `ParsedConstruct.LeadingTokenKind` is the minimal durable surface for recovering anchor keywords the parser consumed but downstream normalization still needs.
- `count` is a type accessor on collection types, not a `FunctionKind`; guard extraction for `count(X) > 0` should match typed member access, not a synthetic function call.
- `IsMessagePosition` now lives on both `TokenMeta` and `FunctionMeta`; generator/tooling consumers should read the catalog flag instead of maintaining their own token lists.
- Typed-literal validation is now catalog-driven end to end: `TypeMeta.ContentValidation` is the registry, `TypedConstantValidation.Validate(...)` is the sole dispatcher, and domain-specific validators return `TypedConstantParseResult` instead of checker-local tuples.
- `unitofmeasure` is no longer a mirrored closed set; it validates through the UCUM subsystem (`UcumParser`, `UcumAtomCatalog`, `UcumPrefixCatalog`) and returns structured parsed units that can be reused by future runtime measure work.
- Temporal typed constants now share one canonical parser stack under `src/Precept/Language/Time/`; `TemporalParser` owns the formatted temporal forms, `TemporalQuantityParser` owns quantity phrases, and `TemporalValidator` bridges both into typed-constant validation.
- External standards data belongs in embedded XML + lazy loaders, not in Precept catalogs: ISO 4217 and UCUM are authoritative reference data consumed by validators, while `TypeMeta` remains the language-owned metadata boundary.
- `UcumCatalog` Tier 1 browsing cannot assume every curated code is a direct `UcumAtomCatalog.All` key: prefixed units and compact exponent forms like `m2` need synthesized tier entries built from UCUM parsing/normalization while `LookupAtom` continues to expose the full atom catalog.
- Q4 registry alignment should expose canonical `All` registries and keep alias lookups separate: `TemporalUnits.All` is singular-keyed while `TryGet(...)` stays backed by an alias map, and `precept_language` should surface business-domain registries under a dedicated `domains` object instead of scattering ad hoc fields.

### 2026-05-09T11:33:49Z — Commit batching: catalog DU + pipeline + analyzers

Committed 6 logical groups from a large multi-session working tree:

1. **`feat: catalog DU metadata enhancements and currency catalog wire-up`** (`b1c95512`) — Added `LeadingToken` to `ConstraintMeta.StateAnchored`, `Constraints.ByToken`, `Modifiers.ByAccessToken`/`ByAnchorToken`, `HasCIVariant`/`CIDiagnosticCode` on `BinaryOperationMeta` and `FunctionMeta`, `[CatalogDU]` on `ProofRequirement`, new `UnprovedPresenceRequirement` (116), removed dual-use `Cat_ActType` from Tokens, wired `Types` to `CurrencyCatalog`.
2. **`feat: catalog-driven pipeline — parser, type checker, proof engine`** (`d27fae6b`) — Parser handles `OutcomeArgumentKind.None` with recovery; TypeChecker uses catalog lookups for constraint/access/anchor resolution; TypeChecker.Validation collapses CI rules to catalog-driven single branch; ProofEngine covers `PresenceProofRequirement` exhaustively and uses `GetNumericRequirementDiagnosticCode` helper.
3. **`feat: PRECEPT0025 hardening + PRECEPT0026 CatalogDU completeness analyzer`** (`a1956961`) — Extracted shared DU helpers to `CatalogAnalysisHelpers`; new PRECEPT0026 analyzer verifies exhaustive switch coverage of `[CatalogDU]` hierarchies.
4. **`docs: sync compiler and spec docs; add working docs batch`** (`9c608dc2`) — diagnostic-system.md, proof-engine.md, precept-language-spec.md updated; 5 new working docs added.
5. **`chore: add iso4217 refresh task, fix LS stub method names, ship refresh script`** (`b9009a2a`) — VS Code task + `refresh-iso4217.js` script; `Handle` → `HandleAsync` in LS stubs.
6. **`chore: update soup nazi agent history`** (`dbfac08e`).

**Grouping observation:** The natural split was: (A) catalog-level metadata DU changes, (B) pipeline consumers of that metadata, (C) analyzers that enforce catalog DU discipline, (D) docs, (E) tooling/scripts, (F) squad state. The CI diagnostic metadata on `BinaryOperationMeta`/`FunctionMeta` and the TypeChecker validation collapse belonged together in A+B respectively — they are producer/consumer pairs across the catalog/pipeline boundary.

## Recent Updates

### 2026-05-09T16:55:27Z — Typed-literal plan execution started on amended scope
- George-7 is now executing the full 12-slice `docs/Working/typed-literal-system-plan.md` plan in background mode.
- Coordinator amendments are part of the execution baseline: `UcumParsedUnit.Annotations`, the Slice 1d two-phase/transitive UCUM loader, and the Slice 10 `(DimensionVector, UcumExactFactor)` interning key note.
- The user directive to stay within plan scope is now durable squad memory for this implementation thread.

### 2026-05-09T15:33:49Z - Six-slice commit batch recorded
- Scribe recorded George-6's six logical commits as one durable implementation batch spanning catalog metadata, pipeline consumers, analyzers, docs, tooling, and squad-state follow-up.
- Commit sequence preserved for future traceability: `b1c95512`, `d27fae6b`, `a1956961`, `9c608dc2`, `b9009a2a`, `dbfac08e`.

### 2026-05-09T15:26:09Z — CurrencyCatalog transactional-surface policy recorded
- Scribe merged the XML mismatch inventory plus George's currency-catalog follow-up into the canonical ledger.
- Durable policy: fund/accounting codes remain out of `CurrencyCatalog` and live in sync-test `IntentionalExclusions`; withdrawn `ANG`, `BGN`, and `ZWL` are the catalog-side drift to remove.


### 2026-05-09T14:47:06Z — CurrencyCatalog implementation recorded
- George-3 shipped `src/Precept/Language/CurrencyCatalog.cs` with 162 ISO 4217 entries and removed the mirrored `Iso4217CurrencyCodes` set from `Types.cs`.
- `CurrencyValidation` now derives allowed values from `CurrencyCatalog.All.Keys`; build stayed green and `test/Precept.Tests` closed at 3646 passed, 1 skipped (`CurrencyCatalogSyncTests` awaiting XML).
- Frank's follow-up doc sync locked the operational sync story: use the VS Code `iso4217: refresh` task, then let the optional sync test verify catalog drift.

### 2026-05-09T09:49:38Z — TypeChecker catalog-fix batch recorded
- `george-5` removed four hardcoded TypeChecker dispatch sites by adding CI diagnostic metadata plus `Constraints.ByToken`, `Modifiers.ByAccessToken`, and `Modifiers.ByAnchorToken`.
- Validation closed green at 3646/3646 targeted runtime tests.

### 2026-05-08T23:45:00Z — ProofEngine Phase 2 closeout recorded
- George landed the full ProofEngine body, then closed the last post-commit failures; proof strategies, forwarding-fact handling, and bounded constant folding are now operational.
- Branch validation stabilized with green builds and only the previously-known non-runtime test gaps remaining at that time.

### 2026-05-09T17:41:32.9988470Z — Typed-literal system plan completed
- George completed all 12 slices of `typed-literal-system-plan`: embedded ISO/UCUM sources, XML-backed currency + UCUM loaders, temporal and UCUM parsers, typed-literal validator framework, `TypeMeta.ContentValidation` wiring, TypeChecker migration, canonical doc sync, and retirement of the superseded working docs.
- Validation closed green at `dotnet build src\\Precept\\Precept.csproj` plus `dotnet test test\\Precept.Tests\\Precept.Tests.csproj` with 3721 passing tests after the TypeChecker migrated fully to `TypedConstantValidation.Validate(...)`.
- Durable follow-up boundary: runtime measure types in `src/Precept/Runtime/Measures/` remain explicit stubs; compile-time parsing/validation is complete, while runtime arithmetic integration is future work.

## Historical Summary

- 2026-05-08 GraphAnalyzer work locked the dead-end/terminal diagnostic model, emitted the missing structural diagnostics, and synchronized the graph/proof docs back to live behavior.
- Earlier 2026-05-08 TypeChecker work restored computed/default field resolution, normalization for ensures/access/state hooks, quantifier + list literal handling, and the full semantic-index assembly pipeline.
- 2026-05-07 and earlier groundwork established the catalog-driven parser/checker trajectory: `TransitionRowOutcome` naming, metadata-driven parser lookups, grammar/message-position metadata, and the principle that durable design decisions belong in catalogs and the squad ledger rather than scattered switches.
- Use `.squad\decisions\decisions.md` for full chronology when a future task needs exact per-batch provenance.

### 2026-05-09T15:21:46Z — Scribe merged the CurrencyCatalog exclusion policy
- `.squad/decisions.md` now records the ISO-sync reconciliation: transactional currencies stay in `CurrencyCatalog`, while fund/accounting-unit codes and other intentional XML-only codes belong in `CurrencyCatalogSyncTests.IntentionalExclusions`.
- Treat the current exclusion set (`BOV`, `CHE`, `CHW`, `CLF`, `COU`, `MXV`, `USN`, `UYI`, `UYW`, `VED`, `XAD`, `XCG`, `ZWG`, plus metals/placeholders) as the durable runtime boundary until Shane changes policy.
