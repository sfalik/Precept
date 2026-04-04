# Test Coverage Snapshot — Precept (2026-04-10)

**Total Tests:** 703 passing (571 + 84 + 48)
**Status:** All green. No failures.

## Suite Breakdown

### Precept.Tests (571 tests) — Core Runtime
- **Parser:** 78 tests (NewSyntaxParserTests) — Excellent
- **Runtime:** 44 tests (NewSyntaxRuntimeTests) — Excellent
- **Workflow:** 87 tests (PreceptWorkflowTests) — Comprehensive
- **Rules:** 65 tests (PreceptRulesTests) — **Best in class**, all 4 positions, scope restrictions, defaults
- **Constraints:** 16 tests (PreceptConstraintViolationTests) — All target types + source kinds
- **Edits:** 48 tests (PreceptEditTests) — Editability, state-scoped guards
- **Collections:** 42 tests (PreceptCollectionTests) — Set/Queue/Stack ops
- **Type Checking:** 35 tests (PreceptTypeCheckerTests)
- **Expressions:** 37 tests combined (parser + runtime)
- **Other:** 12 tests (catalog drift, phase I, set parsing)

### Precept.LanguageServer.Tests (84 tests) — IDE Integration
- **Completions:** 34 tests — Excellent scope awareness
- **Null Narrowing:** 9 tests — Solid type safety
- **Preview Rules:** 13 tests — Good
- **Semantic Tokens:** 10 tests (classification + constraints)
- **Rule Warnings:** 1 test — **CRITICAL GAP** (see decisions/inbox/soup-nazi-rule-analyzer-gaps.md)
- **Other:** 17 tests (nav, surface, code actions)

### Precept.Mcp.Tests (48 tests) — Tool Interface
- **Compile Tool:** 15 tests — Diagnostics, errors
- **Inspect Tool:** 11 tests — Outcomes, violations
- **Fire Tool:** 7 tests — Execution, targets
- **Update Tool:** 6 tests — Edit validation
- **Language Tool:** 9 tests — Vocabulary export

## Risk Assessment

### Solid (No Gaps)
✓ Rules system (all 4 positions, scope validation, compile-time defaults)
✓ Constraint violations (all source/target combinations)
✓ State machine transitions (outcomes, guards, fallbacks)
✓ Expression evaluation (operators, null handling, type safety)
✓ Completions and intellisense (scope-aware)
✓ Collections (basic add/remove/contains)

### Medium Risk
⚠️ Rule analyzer diagnostics (1/8 warning cases tested)
⚠️ Compiler phase I (4 tests, forward ref validation sparse)
⚠️ Queue/stack-specific semantics (collection edge cases)

### Low Risk
✓ MCP tools (unit tests solid, no integration tests needed — thin wrappers)
✓ Code actions (minimal, but 2 tests present)

## Critical Dependency

**PreceptRulesTests.cs is foundational.** 65 tests ensure all rule positions work correctly. If this suite regresses, the entire rules system is at risk. Conversely, if PreceptRulesTests passes, the odds of a rules bug are low.

## How to Use This

1. **Before submitting code:** Run `dotnet test` locally. All 703 should pass.
2. **After feature changes:** Check which test file changed. If rules changed → PreceptRulesTests must still pass. If analyzer changed → PreceptAnalyzerRuleWarningTests should grow.
3. **If a production bug found:** Add a regression test to the relevant file, make it fail, fix the bug, confirm it passes.
4. **If a new feature added:** Write tests first (TDD), then implement. Follow naming convention: `PascalCase` + `Tests`.

## Known Gaps (No Soup for You)

See `.squad/decisions/inbox/soup-nazi-rule-analyzer-gaps.md` for the rule analyzer diagnostic audit. Seven warning/error scenarios untested. George, Kramer, or Newman should add these tests when they touch the analyzer.
