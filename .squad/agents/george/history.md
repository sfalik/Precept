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

## Recent Updates

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

## Historical Summary

- 2026-05-08 GraphAnalyzer work locked the dead-end/terminal diagnostic model, emitted the missing structural diagnostics, and synchronized the graph/proof docs back to live behavior.
- Earlier 2026-05-08 TypeChecker work restored computed/default field resolution, normalization for ensures/access/state hooks, quantifier + list literal handling, and the full semantic-index assembly pipeline.
- 2026-05-07 and earlier groundwork established the catalog-driven parser/checker trajectory: `TransitionRowOutcome` naming, metadata-driven parser lookups, grammar/message-position metadata, and the principle that durable design decisions belong in catalogs and the squad ledger rather than scattered switches.
- Use `.squad\decisions\decisions.md` for full chronology when a future task needs exact per-batch provenance.


### 2026-05-09T15:21:46Z — Scribe merged the CurrencyCatalog exclusion policy
- `.squad/decisions.md` now records the ISO-sync reconciliation: transactional currencies stay in `CurrencyCatalog`, while fund/accounting-unit codes and other intentional XML-only codes belong in `CurrencyCatalogSyncTests.IntentionalExclusions`.
- Treat the current exclusion set (`BOV`, `CHE`, `CHW`, `CLF`, `COU`, `MXV`, `USN`, `UYI`, `UYW`, `VED`, `XAD`, `XCG`, `ZWG`, plus metals/placeholders) as the durable runtime boundary until Shane changes policy.
