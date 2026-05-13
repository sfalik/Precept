## Core Context

- Owns test discipline across parser, type checker, runtime, MCP, language server, and analyzer validation.
- Treats behavioral claims as unproven until executable evidence exists and records gaps as actionable findings.
- Pushes for full-surface coverage matrices, honest red/green pressure, and regression anchors that match the real AST/runtime shape.

## Learnings

- Real catalog metadata is the executable language contract; prefer tiny synthetic fixtures around it over mocked metadata.
- Sample-file integration tests catch parser and language-surface gaps that isolated unit tests miss.
- Span-heavy regressions should derive expected spans from compilation artifacts instead of hand-counted columns.
- Exhaustiveness-style diagnostic fixture tests must supply enough placeholder args for the highest indexed template slot in the catalog.
- When design behavior is still open (diagnostic multiplicity, wildcard fan-out, broadcast identity contracts), wait for the decision before locking tests.

## Historical Summary

- Earlier 2026-05 work established Soup Nazi's durable posture: convert review findings into shipped tests, keep sample-file gates live, and use real-catalog fixtures for parser/type-checker/analyzer coverage.
- The full chronology now lives in `.squad/decisions.md`; this history keeps only lasting testing rules plus the newest high-value review and coverage outcomes.

## Recent Updates

### 2026-05-12T23:50:08Z — Modifier-gap regression suite closed green after coordinator follow-up

- Landed 22 regression tests across `PriceExchangeRateModifierTests.cs`, `IdentityTypeModifierTests.cs`, and `MoneyQuantityModifierRegressionTests.cs`, covering price/exchangerate legality, business-magnitude `maxplaces`, and identity-type redundancy behavior.
- Coordinator corrected the price qualifier fixture and updated `ModifiersTests` drift theories for the split `ZeroBound` vs `Ranged` catalog groups, bringing final repo-wide validation to `4995/4995`.

### 2026-05-12T23:20:42Z — George blocker closeout and redesign re-review approved

- Re-review of commits `53d68d51` and `cf3c6a81` found `0 blockers / 2 good findings`; the explicit-wildcard `ResolvedStateTarget` redesign is structurally correct and ready to merge.

### 2026-05-12T23:02:04Z — Comma-list spike review kept parser/test closure blocked until redesign landed

- Recorded the remaining review gate on commit `a63d88b4`: parser AST coverage for 2-name / 3+-name / whitespace / trailing-comma cases, semantic-clone assertions on expanded rows, and explicit multi-unknown-state diagnostic fan-out.
- Locked the count-integrity rule for this spike: published core-suite totals must match real `dotnet test` output.

### 2026-05-12T19:50:08Z — Modifier catalog gap tests added for price / exchangerate / identity types

- Added 17 price/exchangerate legality tests, 3 identity-type redundancy tests, and 2 money/quantity regression anchors.
- Preserved honest red/green posture: price tests stayed red until implementation landed; exchangerate and identity-type expectations were already green.

### 2026-05-12T18:55:39Z — v2 field-state guarantees test plan review recorded the missing coverage map

- Revised the expected test inventory from ~43 to ~74 and flagged the top blockers: broadcast identity contract, diagnostic exhaustiveness traps, and the lack of event-handler validation coverage.
- Logged the open design questions that must close before affected tests should be written.
