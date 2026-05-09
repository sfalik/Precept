## Core Context

- Owns test discipline across parser, type checker, runtime, MCP, language server, and analyzer validation.
- Treats behavioral claims as unproven until executable evidence exists and records gaps as actionable findings.
- Pushes for full-surface coverage matrices, honest red tests when behavior is missing, and regression anchors that match the real AST/runtime shape.

## Learnings

- 2026-05-09T09:02:31.415-04:00 — Filled the 12 requested ProofEngine gaps, raising the filtered `ProofEngineTests` run to 173 passing: added the Strategy 4 positive proof path, real pipeline emission for codes 112/113/114/116, transition-row and state-hook presence proof coverage, real `.count > 0` guard coverage for collection member access, a TypedPostfixOp `is set` regression anchor, the same-type `number / number` RHS-resolution anchor, vacuous-proof diagnostic absence, and a multi-obligation same-site assertion. Two surprise findings: the audit's `count(collection)` branch does not exist in current source because the catalog models count as `.count` member access, and shared-parameter qualifier requirements on same-type binary ops collapse both subjects to the RHS under `ResolveParamInBinaryOp`, so the end-to-end Code 114 test had to use a distinct-parameter binary site to reach the real unresolved path.
- 2026-05-09 ProofEngine exhaustive audit: the 158-test suite has zero positive proof-success tests for Strategy 4 (FlowNarrowing). Every Strategy 4 test asserts the strategy cannot fire. The implementation exists but is untested for the success path.
- Strategy 5 (QualifierCompatibility) tests are entirely metadata-level record equality checks — no test exercises `QualifierCompatibilityProofRequirement` through the full `ProofEngine.Prove(index, graph)` pipeline with actual DSL source.
- Diagnostic codes 112 (UnprovedModifierRequirement), 113 (UnprovedDimensionRequirement), and 114 (UnprovedQualifierCompatibility) are verified only by enum value assertions — no test causes those diagnostics to actually fire.
- `PresenceProofRequirement` end-to-end path (from DSL source through strategy dispatch to diagnostic) is never exercised; all presence tests are metadata-shape assertions on `DeclaredPresenceMeta`.
- The `count(collection) > 0` and `collection.count > 0` guard patterns in `ExtractGuardConstraintsCore` are implemented but entirely untested; `Strategy3_CountGuard_DischargesCollectionNonEmpty` actually tests plain `D > 0`, not a collection count guard.
- The "field is set" `TypedPostfixOp` guard pattern in `ExtractGuardConstraintsCore` is implemented but no test exercises it with an actual `is set` guard expression.
- StateHookContext + guard (Strategy 3) code path is implemented but untested — only TransitionRowContext guards are tested for Strategy 3.
- The RHS-before-LHS fix in `ResolveParamInBinaryOp` has no same-type regression anchor; all division tests use `integer / number` to avoid the ambiguity. A `number / number` test would be the correct anchor.
- Forwarding facts tests verify `obligation.Disposition == Proved` but do not assert `ledger.Diagnostics` is empty for those vacuously proved obligations.

- Sample-file integration tests catch parser and language-surface gaps that isolated unit tests miss.
- Hardcoded enum/count assertions are acceptable only when they are intentionally updated alongside catalog growth.
- Expression diagnostics often need a full parser host (rule/transition row), not the bare expression helper.
- DSL keywords are common identifier traps in tests; use non-keyword names unless the test is explicitly about keyword handling.
- Analyzer suites need partition/exclusion assertions, diagnostic severities, and direct edge/topology checks — not just happy-path end-to-end outcomes.
- `reject` / `no transition` rows can matter structurally as self-edges even when they do not change state.
- 2026-05-08T23:45:00.367-04:00 — ProofEngine test authoring exposed three easy-to-miss proof-surface traps: operand-metadata identity can change subject resolution (`integer / number` behaves differently from `number / number`), missing boolean catalog entries can turn `and` / `or` guard tests red for type-check reasons instead of proof reasons, and forwarding-fact suppression only holds if later discharge passes preserve already-proved obligations.

## Recent Updates

### 2026-05-09T14:04:05Z — LanguageTool coverage review opened
- Started reviewing `LanguageToolTests.cs` against `McpServerDesign.md` after Newman's `precept_language` implementation landed.
- This batch closed with the review still in flight, so no new durable pass/fail verdict is recorded yet.


### 2026-05-09T04:35:00Z — ProofEngine Phase 2 suite validated
- Soup-Nazi's 158-test `ProofEngineTests.cs` matrix for S1-S13 is now fully green after George's follow-up fixes.
- The recorded red cases successfully flushed out forwarding-fact suppression drift and the Strategy 2 null-guard bug before branch closeout.

### 2026-05-08T00:56:00Z — A-series regression batch recorded
- Added four A-series GraphAnalyzer regression tests (`RS-109`, `RS-110`, `RS-111`, and `DEDUP-NoInitialState`) in commit `c0ca3ae`.
- The batch raised the recorded branch baseline to 3389 total tests and is durably logged even though no decision-inbox note was present for merge.

### 2026-05-08T04:26:28Z — GraphAnalyzer R4 test surface closed across both Soup batches
- Commit `7c674bd` added the required GraphAnalyzer R4 coverage for wildcard expansion/suppression, missing-initial recovery, stateless precepts, terminal/back-edge structural violations, positive terminal completeness, and single-state / cycle / diamond / multi-dead-end topologies.
- The strengthened suite also pinned dead-end exclusion, reachability partitioning, warning severities, and the previously under-specified dominance/proof-forwarding boundary where applicable.
- A late-arriving Round 2 inbox note then closed TQ1, EC5, EC6, and Gap 8 with isolated zero-handler / partial-coverage assertions plus explicit `reject` / `no transition` self-edge tests, bringing `GraphAnalyzerTests.cs` to 20 facts and the branch baseline to 3385 passing tests.

### 2026-05-08T03:08:18Z — R3 G1-G4 test pass closed
- Added the G1 determinism, G3 non-null `TypedInputAction.SecondaryRole`, and G4 non-optional event-arg `is set` regression anchors; all pass.
- G2 is durably recorded as dead code at the TypeChecker level until qualifier-aware candidate disambiguation exists. Commit: `fcc9760`.

### 2026-05-07T23:58:00Z — GraphAnalyzer R4 review opened
- Reviewed George's 5-test `GraphAnalyzerTests.cs` against the ~600-line analyzer and the spec.
- Found eight zero-coverage behavioral areas plus five test-quality gaps; that review directly drove the R4 test matrix George and Soup later closed.

## Historical Summary

- 2026-05-07 parser/testing work locked broad parser coverage discipline: sample integration, AST-shape assertions, diagnostic-identity anchors, and the rule that green parser runs can still be checker-hostile when payloads are under-tested.
- 2026-05-03 through 2026-05-07, Soup-Nazi authored the canonical TypeChecker strategy review, ensure/because-clause coverage, NameBinder regression anchors, outcomes coverage, and the parser gap sweep that filled type-ref, action-chain, wildcard, interpolation, event-arg, and negative-expression holes.
- 2026-05-01 review work established the pattern that full reviews must convert missing coverage into shipped tests, not advisory notes; that batch also locked the sample-file gate and multi-source analyzer harness expectations.
