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
- 2026-05-09T10:34:23.923-04:00 — For xUnit 2.9.3 under the current VSTest adapter, discovery-time skip via a custom `FactAttribute` setting `Skip` is reliable for optional developer-downloaded files; runtime `SkipException` surfaced as a failed test. Resolve the repo root from `AppContext.BaseDirectory`, and keep the download target (`src/Precept/Data/`) gitignored so CI only validates parity after a local refresh has produced the XML.

- Sample-file integration tests catch parser and language-surface gaps that isolated unit tests miss.
- Hardcoded enum/count assertions are acceptable only when they are intentionally updated alongside catalog growth.
- Expression diagnostics often need a full parser host (rule/transition row), not the bare expression helper.
- DSL keywords are common identifier traps in tests; use non-keyword names unless the test is explicitly about keyword handling.
- Analyzer suites need partition/exclusion assertions, diagnostic severities, and direct edge/topology checks — not just happy-path end-to-end outcomes.
- `reject` / `no transition` rows can matter structurally as self-edges even when they do not change state.
- 2026-05-08T23:45:00.367-04:00 — ProofEngine test authoring exposed three easy-to-miss proof-surface traps: operand-metadata identity can change subject resolution (`integer / number` behaves differently from `number / number`), missing boolean catalog entries can turn `and` / `or` guard tests red for type-check reasons instead of proof reasons, and forwarding-fact suppression only holds if later discharge passes preserve already-proved obligations.

- 2026-05-09T17:01:02.062-04:00 — Q8 UCUM drift coverage is now anchored on the embedded `ucum-essence.xml` source of truth plus the curated Tier 1 contract: new tests lock XML-backed universe coverage, SI base/vector invariants, dimensionless `rad`/`each`, Tier 1's exact 150-code curation and exclusions, shim forwarding, and parse-synthesized Tier 1 vectors (`m2`, `km2`, `m/s`, `km/h`, `kW.h`). Important finding: the requested `All >= 500` floor was a false-positive threshold for the current implementation because the shipped UCUM essence snapshot exposes roughly 300 distinct atom codes, so drift coverage must anchor to the embedded XML universe rather than an aspirational atom-count narrative.
- 2026-05-09T23:11:32.953-04:00 — Test-strategy gap audit across `test/` showed the biggest remaining weakness is boundary choice, not raw test count: catalog suites mostly assert structure, parser/type-checker suites mostly assert hand-picked DSL scenarios, and MCP coverage is thin on definition/docs contracts. Recommendation recorded in `.squad/decisions/inbox/soup-nazi-test-strategy.md`: do not mock the static catalogs; use the real catalogs with synthetic AST/semantic fixtures, starting with an MCP definition-surface matrix plus parser/type-checker catalog-consumer tests for routing, keyword collisions, and hook branches.

## Recent Updates

### 2026-05-10T03:13:51Z — Toolchain bug test-strategy verdict merged
- The 52-bug gap audit is now a durable team decision: keep the real static catalogs as the executable language contract and build small synthetic stage fixtures around them instead of mocking metadata.
- Priority coverage layers are now explicit — MCP definition-surface matrices, parser routing/disambiguation tests from `Constructs.Entries`, keyword-collision/accessor tests from real catalog names, TypeChecker catalog-consumer tests, and hook-specific pipeline tests.
- Kramer also added a Track 2 status table to `docs/Working/precept-toolchain-bugs.md`, so the bug register now has an execution surface for the follow-up fixes this strategy is meant to guard.

### 2026-05-09T14:14:17Z — LanguageTool coverage review closed
- Reviewed Newman's `precept_language` batch against `docs/tooling/mcp.md` and found 7 concrete test gaps in `LanguageToolTests.cs`.
- The batch was blocked until remediation landed; after expanding the suite from 12 to 19 tests, `test/Precept.Mcp.Tests` passed clean at 19/19 and the review was recorded as remediated.
- The remaining `dotnet test --no-build -q -m:1 /nr:false` failures stayed isolated to 194 pre-existing language-server stub failures and did not implicate `LanguageTool`.

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

### 2026-05-09T21:01:02.5602711-04:00 — LS slice audit gap fill (Slices 0–2, 4, 5, 9)
- Added 14 tests across `DiagnosticProjectorTests`, `SemanticTokensHandlerTests`, `CompletionHandlerTests`, `HoverHandlerTests`, `FoldingRangeHandlerTests`, and `DiagnosticPublishIntegrationTests`.
- Slice 0 verdict: gaps found. Added coverage for `DocumentStore.Remove`, concurrent `DocumentState.Update`, and `DidClose` publishing empty diagnostics for the closed URI.
- Slice 1 verdict: gaps found. Added explicit `Severity.Info` -> LSP Information projection, mixed error+warning batch projection, multi-line `ToRange` zero-basing, and reinforced `Source == "precept"` across a mixed batch.
- Slice 2 verdict: gaps found. Added legend coverage for all distinct non-null lexical semantic token types and same-line multi-keyword character-position coverage.
- Slice 4 verdict: gaps found. Added field-target (`modify` / `omit`) and event-target (`on` / `when`) completion coverage. Existing top-level empty-source and no-document coverage was already present.
- Slice 5 verdict: critical gap. Added whitespace, newline, end-of-source, and event-argument declaration hover coverage. While probing the requested declared-state/event hover path, I hit a real production bug in `HoverHandler.TryFindUniqueByName`: when the hovered identifier belongs to another symbol kind, `TryFindField`/`TryFindState`/`TryFindEvent` can return `true` with a null primary symbol, leading to `CreateFieldMarkdown(null)` and a `NullReferenceException`. I did **not** fix production; Kramer needs that dispatch before a positive declared-state hover assertion can ship.
- Slice 9 verdict: gaps found. Added exact one-range-per-construct coverage for multiple multi-line construct spans with explicit 0-based line assertions.
- Final counts: scoped LS validation excluding concurrent in-flight Slice 8 `CodeActionHandlerTests` passed at 56/56; `Precept.Tests` passed at 3740/3740. The exact unfiltered LS project run is currently contaminated by concurrent Slice 8 work (3 `CodeActionHandlerTests` failures outside this batch).
