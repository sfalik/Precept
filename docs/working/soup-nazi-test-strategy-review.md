# Type Checker Test Strategy Review

**Reviewer:** Soup Nazi (Tester)
**Date:** 2026-05-02
**Subject:** `docs/compiler/type-checker.md` (817 lines)
**Current state:** Stub — `TypeChecker.Check()` returns empty `SemanticIndex`, `SemanticIndex` is a single-field record with `Diagnostics` only.

---

## TEST STRATEGY

### Overall Approach

The type checker test suite will follow the same vertical-slice structure as the implementation plan. Each slice gets its own test class (or logical region). Tests are organized:

1. **Unit tests** — individual `Resolve()` arms, `IsAssignable()`, qualifier disambiguation, cycle detection
2. **Declaration-level tests** — full declaration nodes through normalization (transition rows, rules, ensures, access modes)
3. **Integration tests** — complete `.precept` files through the full pipeline, asserting `SemanticIndex` shape

**Estimated total test count: ~450–550 tests** across all slices.

### Per-Slice Estimates

| Slice | Description | Est. Tests | Primary Categories | Density |
|-------|-------------|-----------|-------------------|---------|
| Pre-Slice 0 | Shape commit | 15–20 | Build verification, record construction, DU exhaustiveness | Low |
| Slice 1 | Symbol tables (Pass 1) | 35–45 | Positive registration, duplicate detection, initial/terminal validation | Medium |
| Slice 2 | Binary & Unary ops | 80–100 | Op resolution, type mismatch, qualifier disambiguation, ErrorType propagation | **HIGHEST** |
| Slice 3 | Functions, Accessors, MethodCall, InterpolatedString | 60–75 | Overload matching, CI enforcement, member access, string holes | High |
| Slice 4 | Typed constants + context literals | 30–40 | Content validation, context propagation, numeric ambiguity | Medium-High |
| Slice 5 | Transition row normalization | 40–50 | Guard/action resolution, scope management, partial results | High |
| Slice 6 | Structural validation | 45–55 | Cycles, forward refs, choice validation, IsSet/IsNotSet | High |
| Slice 7 | Modifier validation | 35–45 | Applicability, conflicts, subsumption, bounds | Medium |
| Slice 8 | CI enforcement | 15–20 | ~string enforcement, CI function lookup, non-~string rejection | Low |
| Slice 9 | Quantifiers + List literals | 25–35 | Binding scope, predicate typing, element unification | Medium |
| Slice 10 | Final assembly + integration | 30–40 | Full-file compilation, golden snapshots, anti-mirroring | Medium |

**Highest test density:** Slice 2 (expression core). The `Resolve()` function's 16+ arms × type combinations × error paths create a combinatorial surface that dwarfs every other slice.

### File Organization

```
test/Precept.Tests/
├── TypeCheckerRegistrationTests.cs       # Slice 1
├── TypeCheckerExpressionTests.cs         # Slice 2 (may split into Binary/Unary sub-files)
├── TypeCheckerFunctionTests.cs           # Slice 3
├── TypeCheckerTypedConstantTests.cs      # Slice 4
├── TypeCheckerTransitionRowTests.cs      # Slice 5
├── TypeCheckerStructuralTests.cs         # Slice 6
├── TypeCheckerModifierTests.cs           # Slice 7
├── TypeCheckerCIEnforcementTests.cs      # Slice 8
├── TypeCheckerQuantifierTests.cs         # Slice 9
├── TypeCheckerIntegrationTests.cs        # Slice 10
└── TypeCheckerErrorRecoveryTests.cs      # Cross-cutting error recovery
```

---

## HIGH-RISK AREAS

### 1. Qualifier Disambiguation (Slice 2) — CRITICAL

The ~15-line qualifier-match logic after `FindCandidates` returns multiple candidates is the single most failure-prone path. Test matrix:

- **Same qualifier, one candidate:** money + money → same currency, selects `QualifierMatch.Same`
- **Different qualifier, selects alternate:** money / money with different currencies → selects `QualifierMatch.Different` (returns scalar)
- **No qualifier info available:** falls through to error diagnostic
- **Mixed operations:** money * integer (no qualifier ambiguity — single candidate)
- **Chained qualifier expressions:** `(a + b) - c` where all three are money — propagation through sub-expressions
- **ErrorType operand with qualifier:** must short-circuit, not crash on qualifier lookup

### 2. Numeric Literal Context Resolution (Slice 4) — HIGH

The `expectedType` top-down propagation for numeric literals creates ambiguity:

- `field x as integer default 5` → literal resolves as integer
- `field x as decimal default 5` → same literal resolves as decimal
- `field x as money default 5` → same literal resolves as money (via widening? or context?)
- Binary expression with mixed literals: `5 + field_that_is_decimal` → what type is `5`?
- Nested context loss: `min(5, some_decimal_field)` → does context propagate into function args?
- Absence of context: bare `5` in a guard position (no expected type) — must fall back to integer

### 3. ErrorType Propagation Chains — HIGH

Every binary/unary/function node must short-circuit on ErrorType:

- `ErrorType + integer` → ErrorType (no "incompatible operand" diagnostic)
- `min(ErrorType, integer)` → ErrorType (no "wrong argument type" diagnostic)
- `ErrorType.count` → ErrorType (no "no member 'count' on Error")
- Deeply nested: `a + (b * (c / ErrorType))` → error propagates outward without 3 diagnostics
- Conditional: `if ErrorType then x else y` → result is ErrorType
- Quantifier: collection is ErrorType → still emits `TypedQuantifier` with ErrorType predicate

### 4. Scope Boundary Violations (Slices 1, 5, 6) — HIGH

- **Event args out of scope:** Reference `Submit.Amount` inside a rule (not a transition row for Submit) → error
- **Event args IN scope:** Same reference inside `Draft -> Submit -> Submitted` row → resolves
- **Forward references in defaults:** `field b as integer default a` where `a` is declared after `b` → error
- **Forward references in computed:** `field b -> a + 1` where `a` is after `b` → different validation (cycle detection handles this)
- **Computed field self-reference:** `field x -> x + 1` → cycle detection must catch
- **Binding variable shadows field:** `any(items, item => item > 0)` where `item` is also a field name → binding takes precedence

### 5. `Resolve()` Arm Coverage Gaps — MEDIUM

The 16+ arms each need positive + negative + error paths. Highest risk of "forgotten arm":

- `InterpolatedTypedConstantExpression` — rare in samples, easy to under-test
- `CIFunctionCallExpression` — narrow surface (`~startsWith`, `~endsWith`)
- `MethodCallExpression` — currently only collection accessors (`queue.peek()`)
- `ListLiteralExpression` — element type unification with mixed types

### 6. Type Widening Edge Cases — MEDIUM

`IsAssignable(source, target)` is simple but its callers are many:

- `integer` widens to `number` — assignment context
- `integer` widens to `decimal` — binary op context (what does `5 + 3.14` resolve to?)
- Widening is NOT symmetric: `number` does NOT widen to `integer`
- Chained widening: does `integer` → `number` → ??? exist? (Likely not, but must verify)
- ErrorType passes `IsAssignable` with anything — both directions

---

## CATALOG DRIFT TESTING

### The 70/30 Problem

~70% of the checker's behavior comes from catalog lookups. This means tests that hardcode specific type/operation relationships are fragile to catalog growth. Strategy:

### Parameterized Catalog-Driven Tests

YES — write parameterized tests driven by catalog entries for:

1. **Every `BinaryOperationMeta` entry → positive resolution test:**
   ```csharp
   [Theory]
   [MemberData(nameof(AllBinaryOperations))]
   public void Resolve_BinaryOp_CatalogEntry_ProducesExpectedResult(BinaryOperationMeta meta)
   ```
   Feed `Resolve()` with operands matching `meta.LeftType` and `meta.RightType`, assert result is `meta.ResultType`.

2. **Every `FunctionMeta` overload → positive call test:**
   Feed matching argument types, assert return type matches overload.

3. **Every `TypeMeta.Accessors` entry → member access test:**
   Construct `field.accessor` expressions for each registered accessor.

4. **Every `ModifierMeta.ApplicableTo` entry → positive modifier test:**
   Apply modifier to each applicable target, assert no diagnostic.

### Detecting Hardcoded Behavior

To catch "checker hardcodes behavior that should come from catalog":

1. **Reflection scan test:** Assert that `TypeChecker.cs` does NOT contain `switch` statements on `TypeKind`, `OperationKind`, or `FunctionKind` enum values (except the `Resolve()` arm dispatch on expression node types, which is structural).

2. **Catalog growth regression:** If adding a new catalog entry doesn't automatically work in the checker (when it should), that's a test gap. Maintain a "last catalog count" assertion that forces review on catalog growth.

3. **Negative exhaustiveness:** Every `TypeKind` × `TypeKind` pair NOT in `FindCandidates` for a given operator → must produce a type mismatch diagnostic. Parameterize this from the catalog's negative space.

---

## ERROR RECOVERY TESTING

### Per-Declaration Partial Result Tests

For every declaration type, test that the typed entry is emitted even when sub-expressions fail:

| Declaration | Error Injection | Assert |
|-------------|----------------|--------|
| `TypedField` (computed) | Invalid expression in `->` | `Fields` contains entry; `ComputedExpression` is `TypedErrorExpression` |
| `TypedField` (default) | Invalid default expr | `Fields` contains entry; `DefaultExpression` is `TypedErrorExpression` |
| `TypedTransitionRow` | Invalid guard | `TransitionRows` contains entry; `Guard` is `TypedErrorExpression` |
| `TypedTransitionRow` | Invalid action expr | Row emitted; action carries `TypedErrorExpression` |
| `TypedRule` | Invalid condition | `Rules` contains entry; `Condition` is `TypedErrorExpression` |
| `TypedRule` | Invalid message | `Rules` contains entry; `Message` is `TypedErrorExpression` |
| `TypedEnsure` | Invalid condition | `Ensures` contains entry; `Condition` is `TypedErrorExpression` |
| `TypedAccessMode` | Invalid guard | `AccessModes` contains entry; `Guard` is `TypedErrorExpression` |

### ErrorType Propagation (No Cascade) Tests

Each test deliberately introduces ONE error and asserts exactly ONE diagnostic:

- Binary op with one bad operand → 1 diagnostic (from the bad operand), not 2 (no "incompatible types" cascade)
- Function call with one bad arg → 1 diagnostic (bad arg), not 2 (no "wrong param type")
- Chained expression: `bad_ref + 1 > 0` → 1 diagnostic (unresolved `bad_ref`), not 3

### Downstream Consumer Graceful Handling

Tests that feed `TypedErrorExpression` to downstream code paths:

- GraphAnalyzer receives a `SemanticIndex` with `TypedErrorExpression` in transition rows → does not crash
- ProofEngine receives proof requirements from a `TypedBinaryOp` where one operand is `TypedErrorExpression` → skips gracefully
- MCP compile output handles `TypedErrorExpression` in serialization → no crash, appropriate placeholder

---

## INTEGRATION TEST STRATEGY (Slice 10)

### Sample File → Slice Coverage Map

| Sample File | Key Constructs Tested | Primary Slices |
|-------------|----------------------|----------------|
| `computed-tax-net.precept` | Computed fields, dependency graph, numeric ops | 2, 6 |
| `loan-application.precept` | Rules, ensures, event args, functions (`min`, `round`, `clamp`) | 1, 2, 3, 5 |
| `insurance-claim.precept` | `is set`, choice fields, guards, multiple events | 5, 6 |
| `hiring-pipeline.precept` | Complex state machine, access modes, state hooks | 1, 5, 7 |
| `transitive-ordering.precept` | Quantifiers (if present), complex guards | 2, 9 |
| `fee-schedule.precept` | Money/decimal ops, qualifier scenarios | 2, 4 |
| `library-book-checkout.precept` | Temporal types (date, datetime) | 3, 4 |
| `customer-profile.precept` | String operations, CI functions | 3, 8 |
| `restaurant-waitlist.precept` | List literals, collection access | 3, 9 |
| `crosswalk-signal.precept` | Simple stateless, minimal expression surface | 1, 2 |
| `trafficlight.precept` | Minimal state machine, base coverage | 1, 5 |

### Golden File / Snapshot Strategy

**YES — use golden-file snapshots, but carefully:**

1. **Serialize `SemanticIndex` to a normalized JSON/text format** (field count, state count, event count, diagnostic codes, expression types per declaration).
2. **Store golden files as `.approved` files** alongside tests (not in samples/).
3. **Diff-based assertion:** Use ApprovalTests or a custom comparator that ignores SourceSpan positions (fragile) but asserts type structure.
4. **Update protocol:** Golden files are regenerated explicitly (never auto-updated). A mismatch is a test failure until reviewed.

### Fragility Prevention

- **Do NOT assert on SourceSpan values** — parser changes shift these constantly.
- **Do NOT assert on exact diagnostic message text** — wording changes are non-breaking.
- **DO assert on:** TypeKind of resolved expressions, diagnostic codes (not messages), declaration counts, field/state/event names, resolved operation kinds.
- **DO use semantic equality:** Compare `TypedField.ResolvedType` and `TypedField.Modifiers`, not `.Syntax` back-pointer equality.
- **Isolate from catalog growth:** If a new type/operation is added to catalogs, integration tests should not break unless sample files are affected. Pin sample files as test fixtures — don't modify them for unrelated features.

---

## INFRASTRUCTURE NEEDS

### Test Helpers Required Before Slice 2

1. **`TypeCheck(string source)` helper** — Lex → Parse → Check in one call, returns `SemanticIndex`. Equivalent to `Parse()` helper in `ParserTests.cs`.
   ```csharp
   private static SemanticIndex TypeCheck(string source)
   {
       var tokens = Lexer.Lex(source);
       var tree = Parser.Parse(tokens);
       return TypeChecker.Check(tree);
   }
   ```

2. **`TypeCheckExpr(string fieldDecl, string exprContext)` helper** — wraps an expression in a minimal precept body and returns the resolved `TypedExpression` for that specific expression. Essential for Slice 2+ unit tests.
   ```csharp
   private static TypedExpression TypeCheckExpr(string body)
   {
       var index = TypeCheck($"precept Test\n{body}");
       // Extract the relevant typed expression from the index
       ...
   }
   ```

3. **`AssertNoDiagnostics(SemanticIndex index)` assertion** — commonly needed positive test assertion.

4. **`AssertSingleDiagnostic(SemanticIndex index, DiagnosticCode expected)` assertion** — for negative tests asserting exactly one error.

5. **`AssertErrorType(TypedExpression expr)` assertion** — verifies `expr is TypedErrorExpression` with clear failure message.

### Test Fixtures

1. **Minimal precept bodies:** A set of reusable DSL fragments that declare fields of specific types, to avoid repeating boilerplate in every test:
   ```csharp
   private const string IntegerFieldSetup = "precept Test\nfield x as integer\nfield y as integer\n";
   private const string MoneyFieldSetup = "precept Test\nfield a as money\nfield b as money\n";
   ```

2. **Catalog data providers:** `MemberData` methods that yield test cases from catalog entries:
   ```csharp
   public static IEnumerable<object[]> AllBinaryOps() =>
       Operations.All.OfType<BinaryOperationMeta>().Select(m => new object[] { m });
   ```

### Builder Pattern (If Needed)

For complex test scenarios in Slice 5+ (transition rows with guards, actions, and scope), consider a fluent builder:
```csharp
var index = new TypeCheckBuilder()
    .WithField("amount", TypeKind.Decimal)
    .WithState("Draft", ModifierKind.Initial)
    .WithState("Submitted")
    .WithEvent("Submit", ("value", TypeKind.Decimal))
    .WithTransition("Draft", "Submit", "Submitted", guard: "Submit.value > 0")
    .Build();
```

This is lower priority — start with raw string helpers and only build this if Slice 5+ tests become unwieldy.

---

## REGRESSION RISKS

### What Breaks When the Stub Becomes Real

The current stub returns `new SemanticIndex(ImmutableArray<Diagnostic>.Empty)`. The moment it starts returning real diagnostics or a populated `SemanticIndex`, these things will break:

| Risk | Current Behavior | Post-Implementation Behavior | Mitigation |
|------|-----------------|-------|------------|
| `Compiler.Compile()` | Throws `NotImplementedException` (per `WritableSurfaceTests`) | Returns actual result OR throws different exception | **Anchor:** Update `WritableSurfaceTests` to expect real compilation behavior as each slice lands |
| `SampleFileIntegrationTests` | Only tests Lex+Parse (zero errors at parse stage) | Type-check diagnostics would appear if pipeline runs through | **Anchor:** Keep integration test on parse-only path until Slice 10; then upgrade to full-pipeline test |
| `ExpressionFormCoverageTests` | Tests `[HandlesCatalogMember]` on stub | Stub annotations migrate per slice | **Anchor:** These tests will break slice-by-slice as annotations move — update in lockstep |
| Downstream MCP/LS tests | May mock or bypass `TypeChecker.Check()` | Real `SemanticIndex` shape changes parameter types | **Anchor:** Review MCP test mocks after Pre-Slice 0 shape commit |

### Per-Slice Regression Anchors

| Slice | Key Regression Risk | Anchor Strategy |
|-------|--------------------|--------------------|
| Pre-Slice 0 | `SemanticIndex` record gains 14+ new fields — all existing references to `SemanticIndex` break | Landing commit must update ALL downstream references. Test: `SemanticIndex` construction with default values compiles. |
| Slice 1 | Duplicate field/state/event names that currently silently pass will now emit diagnostics | Run all 28 sample files through Slice 1 — none should have duplicates (if they do, fix samples). |
| Slice 2 | Expressions that currently are un-typed will get type errors for real mismatches | The stub-arm strategy (`TypedErrorExpression` for unimplemented arms) prevents premature failures. Test: expressions outside Slice 2 scope return `TypedErrorExpression`, not crash. |
| Slice 5 | Transition rows with bad guards currently "work" (no checking) — will now emit guard-type errors | Test: every sample file transition row has a boolean guard (audit samples pre-implementation). |
| Slice 7 | Invalid modifier combinations currently pass — will now emit diagnostics | Audit: ensure no sample file uses invalid modifier combos. If any do, they're spec bugs not test bugs. |
| Slice 10 | Full pipeline integration — any accumulated drift surfaces here | This is the "moment of truth" slice. Budget extra test time. |

### HandlesCatalogMember Migration Risk

Each slice removes annotations from the stub. If the test (`ExpressionFormCoverageTests`) asserts specific stub method annotations, it will break. Strategy:

- **Do NOT assert which method has the annotation** — only assert that every `ExpressionFormKind` has exactly one handler (the invariant PRECEPT0019 enforces).
- The existing `ExpressionFormCoverageTests` already does this correctly — it checks exhaustiveness, not placement.
- Risk is LOW here, but verify after first slice migration.

---

## VERDICT

The design doc is testable. The vertical slice structure maps cleanly to test classes. The ~70% catalog-driven architecture means parameterized tests will cover enormous ground efficiently. The highest-risk area is Slice 2's `Resolve()` function — that single function will account for ~20% of all type checker tests.

**Three non-negotiable gates before implementation begins:**

1. Pre-Slice 0 shape commit must land with build-verification tests (no behavioral tests, just "it compiles and constructs").
2. The `TypeCheck()` and `TypeCheckExpr()` helpers must exist before Slice 2 starts.
3. A sample-file audit must verify no sample violates rules that will be enforced by the checker (zero false-positive regressions on day one).

No test for you if these gates aren't met.
