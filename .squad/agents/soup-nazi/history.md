## Core Context

- Owns test discipline across parser, type checker, runtime, MCP, language server, and analyzer validation.
- Treats behavioral claims as unproven until executable evidence exists and records gaps as actionable findings, not just counts.
- Historical summary (pre-2026-04-13): led broad verification for declaration guards, including parser/type-checker/runtime/LS/MCP coverage and test-matrix planning for guarded editability.

## Learnings

- Multi-source analyzer coverage needs only one structural test-helper upgrade: `AnalyzerTestHelper` must accept multiple source strings. The rest of the analyzer harness already scales.
- Cross-catalog analyzer stubs must stay minimal and must not pull in `FrozenDictionary`, `ImmutableArray`, or other real-catalog BCL-heavy surfaces.
- Dual-surface MCP validation is blind unless the config artifact and directly related documentation land together.
- Numeric-literal and arithmetic tests are heavily shaped by context flow: binary-expression operands resolve under null expected-type, so some mismatch paths are only observable at the unit-table layer.
- Hardcoded enum counts in infrastructure tests (e.g. `InvokeSlotParser_SwitchIsExhaustive`) must be updated whenever a `ConstructSlotKind` member is added. The pattern is safe and intentional, but the count must track reality. Pattern: keep the count, update the message to name the added member and new count.
- Catalog slot-structure tests must exist for every construct that has a non-trivial or recently-changed slot sequence. When R3/R4 change a construct's slots, the old assumption in a `KeyConstructs_HaveMinimumSlotCount` theory becomes wrong. Replace broad theories with targeted `HasExactly*Slot` facts that document the rationale.
- `NonAssociativeComparison` diagnostic cannot be tested via the bare `ParseExpr()` helper (no diagnostics surface). Wrap in `Parser.Parse(Lexer.Lex("rule a < b < c because \"msg\""))` to get the diagnostics list. Pattern: when expression-level diagnostics must be verified, use a full rule/transition-row declaration as the host.
- DSL keyword collision in action tests: `queue`, `stack` lex as type keywords (`QueueType`, `StackType`), not identifiers. Use PascalCase field names (`ItemQueue`, `ItemStack`) to stay in identifier territory. Same principle applies to event names — never use a DSL keyword as an event name in tests.
- `Token?` (nullable struct) from `DequeueStatement.IntoField` / `PopStatement.IntoField`: FluentAssertions `.Should().BeNull()` / `.Should().NotBeNull()` work correctly; follow with `!.Value.Text` to access the unwrapped struct after asserting HasValue.
- The existing `ExpressionParserTests.cs` file had grown beyond what an initial read showed (TypedConstant tests were appended). Always check the actual last bytes of a test file before writing an edit target — `Get-Content | Select-Object -Last N` is the safe probe.

## Learnings (continued)

- Sample file integration testing reveals parser gaps that unit tests miss. The promotion-gate pattern works: `KnownBrokenSampleFile_StillHasParserErrors` tests fail when a gap is fixed, forcing explicit promotion to the clean set. This prevents silent regressions in both directions.
- Slice 2 (GAP-2) implemented `EventHandlerNode.PostConditionGuard` (action post-condition: `on Submit -> set x = v ensure x == v`) but NOT the `StateEnsureNode`/`EventEnsureNode` post-condition guard form (`in State ensure Cond when Guard because "msg"`). They are distinct AST nodes — don't conflate them.
- Field modifiers after computed expressions (`field X -> expr modifier`) are not supported by the current parser. The DSL authoring style of trailing modifiers is used in multiple sample files but is unimplemented. This is GAP-B.
- Reserved keywords (e.g., `min`, `max`) are rejected as member names in member access (`RequestedFloors.min`). The parser's `Expect(Identifier)` in `MemberAccessExpression` needs an explicit keyword-as-identifier exception. This is GAP-C.

## Recent Updates

### 2026-05-01 — Slice 12: SampleFileIntegrationTests

- Created `test/Precept.Tests/SampleFileIntegrationTests.cs` with 58 new tests across four groups.
- **Group 1 (zero-error gate):** 21-case `[Theory]` covering clean sample files — asserts no error-severity diagnostics. Files confirmed clean by the lex + parse pipeline.
- **Group 2 (gap sentinels):** 7-case `[Theory]` covering known-broken files — asserts they STILL produce errors. Fails when a gap is fixed, prompting promotion of the file to the clean set.
- **Group 3 (structural smoke):** 28-case `[Theory]` verifying all sample files produce a non-null `precept` header (parser recovery works even on broken files).
- **Group 4 (coverage facts):** `AllSampleFiles_TotalCountIs28` and `KnownBrokenFiles_AccountForExactly7OfThe28Samples` maintain accurate coverage accounting.
- **Known pre-existing gaps documented (7 files):**
  - GAP-A (2 files): `StateEnsureNode`/`EventEnsureNode` missing post-condition when-guard — `insurance-claim.precept`, `loan-application.precept`
  - GAP-B (4 files): field modifiers trailing computed expression — `sum-on-rhs-rule.precept`, `invoice-line-item.precept`, `transitive-ordering.precept`, `travel-reimbursement.precept`
  - GAP-C (1 file): reserved keyword (`.min`, `.max`) as collection member name — `building-access-badge-request.precept`
- Final test count: 2247 passing in Precept.Tests. Baseline 2189 + 58 new tests. Zero regressions.
- Commit: `61ac744` on `spike/Precept-V2`.

### 2026-05-01 — Slice 13: ExpressionFormCoverageTests

- Created `test/Precept.Tests/ExpressionFormCoverageTests.cs` with 17 new tests across three groups.
- **Group 1 (catalog completeness):** `ContainsAllEnumMembers` (each enum member appears exactly once), `NoDuplicateKinds`, `NoNullHoverDocs`, `NoNullCategory` (validates defined ExpressionCategory value).
- **Group 2 (reflection-based annotation coverage):** `Parser_HasHandlesCatalogExhaustivelyForExpressionFormKind` finds the single type carrying the class marker; `Parser_HandlesFormAnnotations_CoverAllExpressionFormKinds` collects all `[HandlesForm]` annotations via GetMethods and asserts every ExpressionFormKind member is covered. This is the xUnit mirror of PRECEPT0019 — a new unhandled enum member will fail both the Roslyn analyzer and these tests.
- **Group 3 (parse round-trip):** `[Theory]` with 8 InlineData cases covering Literal, Identifier, Grouped/Parenthesized, BinaryOperation, UnaryOperation, MemberAccess, ListLiteral; plus separate `[Fact]` tests for Conditional, FunctionCall (→ CallExpression), MethodCall.
- Final test count: 2185 (Precept.Tests) + 235 (Analyzers.Tests) = 2420. Baseline was 2403. +17.
- No gaps found in the catalog itself; all 10 ExpressionFormKind members are covered. The reflection tests confirmed ParseSession carries the annotation and all 10 forms have at least one handler.
- Learning: `ref struct` types are returned by `Assembly.GetTypes()` and support `GetCustomAttributes()` and `GetMethods()` normally under .NET 10 — reflection-based annotation tests work cleanly for `ref struct` parser types.


### 2026-07 — Full-surface coverage gap matrix completed
- Performed exhaustive audit of all ~75 parser-level constructs in `docs/language/precept-language-spec.md` against all 23 test files in `test/Precept.Tests/` and all 28 sample files in `samples/`.
- **38 total gaps identified**: 2 Critical (known), 15 High, 14 Medium, 7 Low/Blocked.
- **23 of 28 sample files have zero integration test coverage** — only `crosswalk-signal`, `trafficlight`, and `hiring-pipeline` are clean; `insurance-claim` and `loan-application` are partial (known GAP-2/3 blocks).
- Key new findings beyond the 3 known gaps:
  - GAP-4: `contains` infix operator — no expression-parser test (blast radius: 2+ sample files)
  - GAP-10: Interpolated string expression — no expression-parser test (lexer-only coverage)
  - GAP-12: List literal `[a, b, c]` — no expression-parser test
  - GAP-13: `NonAssociativeComparison` diagnostic — no test producing it
  - GAP-15: `~=` and `!~` case-insensitive operators — no expression-parser test
  - GAP-16: `%` modulo — no expression-parser test
  - GAP-17: `<`, `<=`, `>=`, `==`, `!=` — all five missing from expression-parser tests (only `>` is tested)
  - GAP-5/6/7/8: `remove`, `enqueue`, `dequeue (into)`, `push`, `pop (into)` — no parser-level action tests
  - GAP-18–22: All value-bearing modifiers (`default`, `min`, `max`, `minlength`, `maxlength`, `mincount`, `maxcount`, `maxplaces`) — catalog only, no parse-tree assertions
  - GAP-23–25: `ordered`, `optional`, `writable` flag modifiers — catalog only, no parse assertions
  - GAP-31: `required`, `irreversible`, `success`, `warning`, `error` state modifiers — no parse tests
- Full matrix written to `.squad/decisions/inbox/soup-nazi-full-coverage-review.md`.
- Learnings: GAP-17 is the highest-surprise gap — `<`, `<=`, `>=`, `==`, `!=` look tested because `>` is tested, but the other four have zero coverage. One theory covers them all.

### 2026-05-01 — WSI verification batch recorded
- Added 27 parser-focused whitespace-insensitivity tests and finished green at 2107 passing tests.
- Durable gaps are now logged in `decisions.md`: `TypedConstant` atom handling, `StateEnsure when`, membership-expression completeness, missing `TypeChecker.Check()`, and missing MCP `precept_compile` support.
- Future MCP smoke inputs were preserved for the day the compile tool ships.

### 2026-04-28 — Parser remediation coverage audit (R1–R6)
- Audited all 6 remediation slices. Behavioral coverage was complete; two tests were broken by the remediation itself (not by the audit).
- B1: `InvokeSlotParser_SwitchIsExhaustive` had stale count (16) after R4 added `InitialMarker`. Fixed to 17.
- B2: `KeyConstructs_HaveMinimumSlotCount(StateDeclaration)` expected ≥2 slots; R3 correctly collapsed StateDeclaration to 1 compound `StateEntryList` slot. Removed from theory, added `StateDeclaration_HasExactlyOneSlot_StateEntryList`.
- Added `EventDeclaration_HasInitialMarkerSlot` to pin the R4 catalog shape.
- Final: 2034 tests, 0 failing. Verdict: APPROVED.
- The pre-landing blocked run is now superseded. Root `.mcp.json` exists, parses cleanly, and correctly uses the CLI `mcpServers` schema with only the local `precept` server.
- `.vscode/mcp.json` remains the VS Code/workspace `servers` config with both `precept` and `github`, and `tools/Precept.Plugin/.mcp.json` remains the unchanged shipped payload.
- Directly related docs now describe the three-surface boundary precisely, and no stale live operating-model reference remains.
- Durable testing pattern: dual-surface changes should land config + doc updates together in the same PR.

### 2026-04-26 — PRECEPT0005/PRECEPT0006 shipped; PRECEPT0007 remains follow-up
- Implemented PRECEPT0005 and PRECEPT0006 with focused analyzer coverage and caught the real sqrt `ParameterMeta` reference-identity bug in production catalog code.
- Kept PRECEPT0007 as the next-step proposal: flag `Enum.GetValues<CatalogEnum>()` outside the owning `All` getter.

### 2026-04-26 — Analyzer infrastructure and full test-plan bar
- Defined the minimal harness change for cross-catalog analyzer tests and the stub rules needed to keep them reliable.
- Set the testing bar for the analyzer expansion at roughly 298 cases across helper coverage, analyzer-specific suites, and regression anchors.
- Accepted the only notable blind spot: spread elements inside shared static arrays, with declaration-site validation/regression coverage as the backstop.

### 2026-04-25 — Catalog-driven metadata test strategy review
- Flagged the operations matrix as the highest-value generated test surface and required snapshot/golden coverage per catalog.
- Framed catalog drift testing as non-negotiable once catalog-owned behavior starts replacing hand-written tables.

### 2026-04-24 — Precept.Next coverage audit and slice support
- Identified the compile-time blockers that prevented deeper TypeChecker test work: hollow model shapes and missing diagnostic codes.
- Added targeted Faults and OperatorTable/binary-expression coverage while documenting what remained untestable until scaffolding was fixed.

### 2026-04-30 — WSI Slices 1–5 test coverage added

- Added 27 new tests to `test/Precept.Tests/ParserTests.cs` across 9 categories: multi-line whitespace, comment filtering, multi-qualifier parsing, qualifier disambiguation, collection qualifiers, negative cases, token stream regression, integration sample files, and ChoiceElementTypeKeywords catalog regression.
- Final test count: 2107 passing, 0 failing.
- Key DSL quirks discovered:
  - DSL comments use `#` (hash), NOT `//`. The lexer only recognizes `#`; `//` lexes as two `Slash` operators.
  - Single-quoted strings (`'USD'`) lex as `TypedConstant` (not `StringLiteral`). `ParseAtom()` does not handle `TypedConstant` — qualifier values must use double-quoted strings (`"USD"`).
  - `Compiler.Compile()` is unusable in tests — `TypeChecker.Check()` throws `NotImplementedException`. Tests must use `Lexer.Lex()` + `Parser.Parse()` directly.
- Key coverage gaps filed to inbox (`soup-nazi-wsi-tests.md`):
  - `ParseAtom()` can't handle `TypedConstant` (GAP-1)
  - `StateEnsure` with `when` guard clause not implemented — blocks insurance-claim and loan-application sample file parse (GAP-2)
  - `is set`/`is not set` membership expressions may be incomplete (GAP-3)
  - `TypeChecker` not implemented — zero type-checking test coverage system-wide (GAP-4)
  - MCP `precept_compile` not implemented — blocks regression protocol (GAP-5)
- MCP regression skipped: only `precept_ping` exists in `tools/Precept.Mcp/Tools/`. The 4 regression inputs are documented in the gap report as smoke tests for when `precept_compile` ships.

### 2026-04-29 — Parser remediation coverage audit recorded
- Coverage audit for parser remediation slices R1-R6 is now recorded as approved with 2034/2034 tests passing.
- Durable regression anchors now include the updated ConstructSlotKind count, the exact StateDeclaration slot-shape fact, and the EventDeclaration initial-marker slot fact.
