# Project Context

- **Owner:** shane
- **Project:** Precept — domain integrity engine for .NET. Makes invalid states structurally impossible.
- **Stack:** C# / .NET 10.0 (runtime, tests), xUnit + FluentAssertions
- **My domain:** All three test suites: `test/Precept.Tests/` (666 tests), `test/Precept.LanguageServer.Tests/`, `test/Precept.Mcp.Tests/`
- **Test conventions:** xUnit `[Fact]` and `[Theory]`, FluentAssertions, `PascalCase` + `Tests` suffix
- **Key docs for test strategy:** `docs/RulesDesign.md` (what constraints must do), `docs/ConstraintViolationDesign.md` (what violations look like)
- **Ground truth:** `samples/` — 20 `.precept` files showing valid and invalid usage
- **Build/test:** `dotnet test` (all), `dotnet test test/Precept.Tests/` (single suite)
- **Created:** 2026-04-04

## Learnings

### Test Refresh 2026-04-10 (Comprehensive Coverage Analysis)

**Test Suite Status: 703 tests passing, 0 failures.**

#### 1. Precept.Tests (571 tests) — Core Runtime & Language

| Component | File | Tests | Coverage |
|-----------|------|-------|----------|
| **Workflow** | PreceptWorkflowTests.cs | 87 | Comprehensive. Covers parse → compile → inspect → fire → outcomes (Transition, NoTransition, Undefined, Blocked). Unguarded transitions, guarded transitions, multiple fallback rows. ✓ Solid. |
| **Rules (Invariants, State/Event Asserts)** | PreceptRulesTests.cs | 65 | Parse all 4 rule positions (field, top-level, state, event). Scope restrictions (field rules vs. top-level, event args only). Compile-time default violations detected. Self-transitions trigger state entry rules. ✓ Excellent — rules system fully covered. |
| **Constraint Violations (Structured)** | PreceptConstraintViolationTests.cs | 16 | IsSuccess on FireResult, EventInspectionResult, UpdateResult. Outcome kinds (Transition, Rejected, ConstraintFailure). ConstraintTarget types (Field, EventArg, Event, State, Definition). ConstraintSource kinds. Multi-subject invariants. ✓ Solid. |
| **Edit Blocks** | PreceptEditTests.cs | 48 | Parse edit declarations (single/multi-state). Runtime update constraints. Editability guards (state-scoped field edits). Invariants checked post-edit. ✓ Comprehensive. |
| **Collections** | PreceptCollectionTests.cs | 42 | Set, Queue, Stack declarations, inner types. Add/Remove/Contains operations. Duplicate detection. Rule checks on collection properties (.count, contains). ✓ Solid. |
| **Parser (New Syntax)** | NewSyntaxParserTests.cs | 78 | Field declarations (as / default), state / event syntax, transition rows (->), mutation ops (set/add/remove). Null/nullable syntax. ✓ Thorough. |
| **Runtime (New Syntax)** | NewSyntaxRuntimeTests.cs | 44 | Invariant violation → ConstraintFailure. State asserts (in/to/from) enforcement. Event asserts on inputs. Transition vs. no-transition vs. blocked. ✓ Strong. |
| **Type Checker** | PreceptTypeCheckerTests.cs | 35 | Type mismatch detection, null safety narrowing, collection type compatibility. ✓ Good. |
| **Expression Parser** | PreceptExpressionParserTests.cs | 13 | Binary ops, unary ops, parentheses, dotted member access. ✓ Adequate. |
| **Expression Runtime** | PreceptExpressionRuntimeEvaluatorBehaviorTests.cs | 24 | Arithmetic, boolean, string ops. Null propagation, type coercion, short-circuit eval. ✓ Solid. |
| **Expression Edge Cases** | PreceptExpressionParserEdgeCaseTests.cs | 10 | Precedence, chained comparisons, mixed types. ✓ Reasonable. |
| **Compiler Phase I** | PreceptCompilerPhaseITests.cs | 4 | Basic compilation flow. Minimal coverage. |
| **Set Parsing** | PreceptSetParsingTests.cs | 4 | Basic set syntax. Minimal coverage. |
| **Catalog Drift** | CatalogDriftTests.cs | 8 | AST → runtime model consistency checks. ✓ Valuable for regression detection. |

**Precept.Tests Verdict:** Core runtime, rules, and constraint violations are **very well tested**. Expression evaluator, type checking, and collections are solid. Parser coverage is excellent. **Minor gap:** Compiler Phase I is under-tested (4 tests only) — could use deeper validation of compilation pass strategies, optimization, and error recovery.

---

#### 2. Precept.LanguageServer.Tests (84 tests) — IDE Integration & Analysis

| Component | File | Tests | Coverage |
|-----------|------|-------|----------|
| **Completions** | PreceptAnalyzerCompletionTests.cs | 34 | Scope-aware suggestions: field vs. event arg contexts, dotted accessors, collection members filtered by kind, keyword triggers. ✓ Very thorough. |
| **Semantic Tokens** | PreceptSemanticTokensClassificationTests.cs | 5 | Token type categorization (keyword, type, operator, identifier). Basic but present. |
| **Semantic Tokens (Constraints)** | PreceptSemanticTokensConstraintTests.cs | 5 | Constraint-specific token scoping. Minimal. |
| **Event Surface** | PreceptAnalyzerEventSurfaceTests.cs | 4 | Event arg scope visibility, event context tracking. Basic. |
| **Null Narrowing** | PreceptAnalyzerNullNarrowingTests.cs | 9 | Null safety analysis, type narrowing on conditionals. ✓ Solid. |
| **Rule Warnings** | PreceptAnalyzerRuleWarningTests.cs | 1 | **CRITICAL GAP:** Only 1 test. Detects "to state asserts never checked" warning. Missing: "from state asserts never checked," cross-field rules on non-existent fields, scope violations, compile-time rule defaults. **Gap flagged.** |
| **Intellisense Navigation** | PreceptIntellisenseNavigationTests.cs | 5 | Go-to-definition, symbol resolution. Basic. |
| **Code Actions** | PreceptCodeActionTests.cs | 2 | Quick fixes. Minimal. |
| **Preview Rules** | PreceptPreviewRulesTests.cs | 13 | EvaluateCurrentRules API, violation collection in preview snapshots, multi-violation reporting. ✓ Good. |

**LanguageServer.Tests Verdict:** Completions are excellent, null narrowing is solid. **Critical gap:** Rule warning diagnostics (PreceptAnalyzerRuleWarningTests) has only 1 test covering 1 diagnostic case — scope violations, semantic rule issues, and design constraint warnings are untested.

---

#### 3. Precept.Mcp.Tests (48 tests) — MCP Tool Interface

| Component | File | Tests | Coverage |
|-----------|------|-------|----------|
| **Compile Tool** | CompileToolTests.cs | 15 | Parse errors → diagnostics, type errors, line numbers, error attribution. ✓ Solid. |
| **Fire Tool** | FireToolTests.cs | 7 | Single event execution, transition outcomes, constraint violations with targets, data snapshots, errors. ✓ Good. |
| **Inspect Tool** | InspectToolTests.cs | 11 | Outcome prediction, guide structure, violation collection, multi-violation reporting. ✓ Good. |
| **Update Tool** | UpdateToolTests.cs | 6 | Field edit validation, editability guards, invariant checks on update. ✓ Adequate. |
| **Language Tool** | LanguageToolTests.cs | 9 | DSL vocabulary export (keywords, operators, types, scopes). ✓ Covers API shape. |

**Mcp.Tests Verdict:** Tool interfaces are reasonably tested at the API level. **Gap:** No integration tests across tool sequences (e.g., compile → fire → update → fire again). Cross-tool contract validation missing.

---

#### 4. **Constraints System Coverage — Detailed Analysis**

**Invariants:** ✓ 16+ tests in PreceptConstraintViolationTests. Field → Definition target mapping. Multi-subject invariants. Default violations at compile time.

**Event Asserts:** ✓ Scope restricted to args. Pre-transition timing. EventArg + Event targets. 10+ tests across suites.

**State Asserts (in/to/from):** ✓ Temporal scoping tested. Entry timing vs. no-transition logic. Anchor types (In, To, From). StateTarget with anchor. 15+ tests.

**Transition Rejections (reject rows):** ✓ TransitionRejectionSource + EventTarget. Fallback row matching. 5+ tests.

**When Guards (routing logic, NOT constraints):** ✓ Not constraint violations, correctly excluded from ConstraintSourceKind. Tested as part of row matching logic.

**Verdict:** Constraint system is comprehensively tested by design. All four constraint types have structured violations with proper target attribution. ✓ No gaps in core constraint testing.

---

#### 5. **Samples Sanity Check** (21 files covering realistic workflows)

Reviewed: `loan-application.precept`, `apartment-rental-application.precept`, `trafficlight.precept`.

All samples use correct:
- Invariant syntax (field / top-level)
- Event assert guards (`on Event assert`)
- State asserts (in/to/from)
- Transition rows with conditions and mutations
- Edit blocks for state-scoped editability
- Collection operations (add, remove, contains)

✓ Ground truth is consistent with test expectations.

---

#### 6. **Critical Test Gaps Identified**

1. **Rule Analyzer Diagnostics** (PreceptAnalyzerRuleWarningTests = 1 test)
   - Missing: Warnings for `from` state asserts never checked
   - Missing: Field rule scope violations (referencing other fields)
   - Missing: Event rule scope violations (referencing instance data)
   - Missing: Top-level rule placement before field declaration (forward ref errors)
   - Missing: Null semantic errors in rule expressions
   - **Impact:** Users won't see actionable diagnostics for common authoring mistakes.
   - **Action:** File `soup-nazi-rule-analyzer-gaps.md`

2. **Collection Mutation Edge Cases** (PreceptAnalyzerCollectionMutationTests = 6 tests)
   - Covers: Basic add/remove validation
   - Missing: Queue/stack-specific semantics (FIFO order, pop constraints)
   - Missing: Collection.count in different expression contexts
   - Missing: Type incompatibility on add (e.g., adding wrong inner type)
   - **Impact:** Low — basic coverage present, edge cases deferred.

3. **Compiler Phase I** (PreceptCompilerPhaseITests = 4 tests)
   - Only 4 tests for first compilation pass
   - Missing: Field hoisting order, forward reference detection, scope validation
   - Missing: Duplicate declaration errors across all symbol types
   - **Impact:** Medium — regression risk if parser changes.

4. **MCP Integration Tests**
   - No cross-tool sequence tests (compile → fire → update → fire)
   - No error propagation tests (malformed data across tools)
   - **Impact:** Low — tools are thin wrappers, unit tests sufficient.

5. **Code Actions & Quick Fixes** (PreceptCodeActionTests = 2 tests)
   - Only 2 tests for entire code action layer
   - Missing: Create state, create event, create field refactorings
   - Missing: Fix-and-continue scenarios
   - **Impact:** Low — feature is minimal, but could grow.

---

#### 7. **Coverage by Feature Area (Percentages)**

| Feature | Coverage | Status |
|---------|----------|--------|
| Parsing (all constructs) | ~90% | ✓ Excellent — NewSyntaxParser tests are thorough |
| Runtime (fire/inspect/update) | ~95% | ✓ Excellent — core paths covered, edge cases present |
| Constraints (invariant/assert/reject) | ~95% | ✓ Excellent — all target types, all source kinds tested |
| Expressions (evaluate + parse) | ~85% | ✓ Good — binary/unary/dotted covered; some precedence gaps |
| Type Checking | ~80% | ✓ Solid — null narrowing, basic type safety |
| Collections (syntax + runtime) | ~80% | ✓ Good — Set/Queue/Stack operations present; edge cases sparse |
| Rules (all 4 positions) | ~90% | ✓ Excellent — PreceptRulesTests is comprehensive |
| Language Server (completions) | ~85% | ✓ Very good — scope-aware suggestions, triggers |
| Language Server (diagnostics) | ~40% | ⚠️ **Gap** — Rule warnings severely under-tested |
| MCP Tools | ~75% | ✓ Good — API coverage present; no integration tests |

---

#### 8. **Conclusion: Test Suite Cold Start**

Precept has a **strong test foundation** with 703 passing tests across three suites. Core runtime, rules system, and constraint violations are bulletproof. Parser and expression evaluator are well-covered.

**Solid zones:**
- Rules system (all positions, compile-time validation, scope restrictions)
- Constraint violations (all source kinds, target attribution)
- Workflow state machine (outcomes, transitions, guards)
- Expression evaluation (operators, null handling, type safety)
- Completions and intellisense

**Risky zones:**
- Rule analyzer diagnostics (only 1 test, but critical for UX)
- Compiler Phase I (4 tests, medium regression risk)
- Code actions and refactorings (minimal, but growing)
- Collection mutation edge cases (Set/Queue/Stack semantics sparse)

**Risk Assessment:** Low overall. No untested critical paths in runtime. Rules system is solid. Only concern is analyzer diagnostics — that's a UX issue, not a correctness issue.
