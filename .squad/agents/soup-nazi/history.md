## Core Context



- Owns test discipline across parser, type checker, runtime, MCP, language server, and analyzer validation.

- Treats behavioral claims as unproven until executable evidence exists and records gaps as actionable findings.

- Historical summary: established the sample-file promotion gate, large parser coverage matrices, analyzer-harness expectations, and the rule that spec/catalog changes are not complete until regression anchors exist.



## Learnings



- Sample-file integration tests catch parser and language-surface gaps that isolated unit tests miss.

- Hardcoded enum/count assertions are acceptable regression anchors only when they are intentionally updated alongside catalog growth.

- Expression-level diagnostics often need a full parser host (rule/transition row) instead of the bare expression helper.

- DSL keywords are common identifier traps in tests; use non-keyword names for fields/events unless the test is explicitly about keyword handling.

- Multi-source analyzer tests only need a small harness extension; the rest of the analyzer test shape can stay minimal.

- For operation analyzers, use `ctx.Operation.SemanticModel` (nullable) rather than APIs available only on `SyntaxNodeAnalysisContext`.



## Recent Updates



### 2026-05-01T20:36:28Z — Full review gaps closed and branch green

- The full coverage review is now durably recorded end-to-end: the initial review found 6 missing tests, and Soup-Nazi-4 landed all 6 (M1-M6) plus the RS1030 follow-on fix.

- Validation closed green at 2687 passing tests across the branch; no skipped tests were introduced.

- Treat the coverage review as resolved, not merely advisory: the missing test matrix is now implemented.



### 2026-05-01 — 6 coverage gaps from full review implemented (M1-M6)

- Wrote the six missing tests for postfix arity, multi-token operator token sequences, postfix round-trip coverage, PRECEPT0020 MultiTokenOp skip behavior, chained `is set` behavior, and postfix precedence.

- Fixed the PRECEPT0013 follow-on issue by switching to `ctx.Operation.SemanticModel` with a null-guard.

- Branch validation finished green with no new failures.



### 2026-05-01 — Full-surface review summary compressed

- Historical branch review work already established the sample integration gate, parser remediation anchors, whitespace-insensitivity regression layer, and the broad gap matrix that drove the spike's later test additions.

- Earlier detailed notes were summarized here to keep the active history compact while preserving durable testing patterns and the branch's executed outcomes.



### 2026-05-02T21:58:21Z — Canonical type checker batch closed

- Soup-Nazi's checker test-strategy review is now part of the implementation baseline: expect roughly 450-550 tests, 3 non-negotiable validation gates, and 4 high-risk areas to anchor the slice-by-slice rollout.

- The canonical checker design is now implementation-ready after Frank's response to George; treat the test review as required follow-through, not advisory commentary.

- Future checker slices should keep error recovery, scope/binding behavior, qualifier-sensitive operations, and cross-consumer regression coverage explicitly pinned in tests.



### 2026-05-03T11:11:04Z — BecauseClause slot split tests written

- Added 35 tests in `EnsureBecauseClauseSlotTests.cs` covering the catalog defect fix that splits `BecauseClause` out of `EnsureClause` for `StateEnsure` and `EventEnsure`.

- George had already applied the 3-slot structure by the time tests ran: `HasThreeSlots`, `SlotAtIndex2_IsBecauseClause`, and `SlotAtIndex0/1` tests all pass green.

- A real defect was surfaced: `BecauseClause` slot is `IsRequired: true` on both constructs but must be `IsRequired: false` (ensures without `because` are valid DSL). Two RED-C tests document this.

- 7 RED-P tests document expected parser behavior (stub returns empty manifest); 4 RED-R tests document expected runtime behavior (not yet implemented). All 13 red tests are intentionally honest — no skips.

- Finding: When George uses the shared `SlotBecauseClause` instance (required=true) for StateEnsure/EventEnsure, a new optional variant (`SlotOptBecauseClause = new(ConstructSlotKind.BecauseClause, IsRequired: false)`) is required to match the design intent.



### 2026-05-03T15:18:05Z — Ensure-slot test matrix closed over George's fix

- The because-clause test batch is now durably closed at the catalog layer: 35 tests exist, George cleared the 2 RED-C optional-slot defects, and the remaining 11 red tests stay as honest parser/runtime stubs.

- Cross-team correction to preserve in future diagram-related testing/docs work: schema diagrams should count 13 catalogs including `ExpressionForms`; `ConstructSlotKind` is helper schema only.

- User routing directive: Elaine owns diagram authoring across both ASCII and Mermaid, so future diagram polish or anatomy rendering requests should route there rather than treating diagrams as Frank-only follow-through.



### 2026-05-07T08:22:32Z — BackArrow parser coverage added

- Added `ParserBackArrowTests.cs` to lock the new `<-` computed-field delimiter and the `->` regression boundary for transition rows and event handlers.

- Coverage includes happy-path computed fields, multi-field coexistence, span anchoring at `<-`, token catalog registration, and two honest negative cases for wrong-position `<-` usage.

- Current high-value defect anchor: `field amount as number <-` should raise a parse diagnostic instead of silently accepting a placeholder expression.



### 2026-05-07T08:40:33Z — BackArrow test red case closed

- Soup-Nazi wrote 11 `ParserBackArrowTests` and correctly left the bare `<-` case red instead of masking it with a fake parser placeholder.

- George-5 closed the defect and added the narrow exhaustiveness annotations; the full parser batch now sits inside the 2810/2810 green run.

- Durable lesson: new syntax work needs both happy-path/regression coverage and at least one honest negative case that forces recovery behavior.



### 2026-05-07T09:04:34Z — Parser coverage audit flagged critical gaps

- Parser-focused run is green at 459 passed, 0 failed, 0 skipped across the 6 files under `test/Precept.Tests/Parser/`.

- The strongest gap cluster is type-checker-facing parser surface: full `TypeRef` syntax, collection/lookup/choice shapes, wildcard/shorthand routing (`from any`, `modify all`, `omit all`), rich event args, and stateless handler trailing `ensure` all lack meaningful parser coverage.

- Action parsing is especially under-anchored: parser tests prove `ActionChain` slot presence, but not action kind/operand detail for the collection mutation surface.

- Catalog drift protection is strong and distributed (`TokensTests`, `OperatorsTests`, `ActionsTests`, `ConstructsTests`, `ExpressionForm*`, `SlotOrderingDriftTests`), but parser diagnostic-code coverage is near-zero — many tests still stop at `NotBeEmpty()` or `Stage == Parse`.

- Durable lesson: a green parser suite can still be checker-hostile when it does not pin the actual AST payloads and diagnostic identities that later stages consume.

### 2026-05-07T15:18:42Z — NameBinder suite complete

- Added `test/Precept.Tests/NameBinder/NameBinderTests.cs` with 40 tests spanning 9 behavioral groups for declarations, duplicates, references, shadowing, forward references, and manifest integration.
- Branch validation closed green at 2929 total passing tests.
- Frank's companion doc-sync batch eliminated stale NameBinder references, so the implementation and its regression anchors are now durably documented together.
