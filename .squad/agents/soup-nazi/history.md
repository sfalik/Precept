## Core Context

- Owns test discipline across parser, type checker, runtime, MCP, language server, and analyzer validation.
- Treats behavioral claims as unproven until executable evidence exists and records gaps as actionable findings.
- Pushes for full-surface coverage matrices, honest red tests when behavior is missing, and regression anchors that match the real AST/runtime shape.

## Learnings

- Sample-file integration tests catch parser and language-surface gaps that isolated unit tests miss.
- Hardcoded enum/count assertions are acceptable only when they are intentionally updated alongside catalog growth.
- Expression diagnostics often need a full parser host (rule/transition row), not the bare expression helper.
- DSL keywords are common identifier traps in tests; use non-keyword names unless the test is explicitly about keyword handling.
- Analyzer suites need partition/exclusion assertions, diagnostic severities, and direct edge/topology checks — not just happy-path end-to-end outcomes.
- `reject` / `no transition` rows can matter structurally as self-edges even when they do not change state.

## Recent Updates

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
