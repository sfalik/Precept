# Extended Plan: Implementation Review + Phase 2 Outline
**Author:** Frank  
**Date:** 2026-05-01

---

## Part 1: Implementation Review

### Slice-by-Slice Verdict

---

#### Slice 1: GAP-1 — Typed Constant Atom Handling
**Verdict: CLEAN**

- `ParseAtom()` correctly handles `TokenKind.TypedConstant` → `TypedConstantExpression` and `TokenKind.TypedConstantStart` → `ParseInterpolatedTypedConstant()`.
- `ParseInterpolatedTypedConstant()` mirrors `ParseInterpolatedString()` exactly with the correct middle/end token kinds.
- Both AST nodes exist: `TypedConstantExpression(SourceSpan, Token)` and `InterpolatedTypedConstantExpression(SourceSpan, ImmutableArray<InterpolationPart>)`.
- Namespace is `Precept.Pipeline.SyntaxNodes` (correct — no `.Expressions` sub-namespace).
- Tests: `ParseExpression_TypedConstant_Simple`, `_Date`, `_Interpolated`, `_InFieldDefault` — all present.
- Minor plan vs. impl divergence: plan referenced `IntegerLiteral`/`DecimalLiteral`/`Null` in the catalog LeadTokens — implementation correctly uses `NumberLiteral` and omits `Null` (which doesn't exist as a token kind). This is a plan error, not an impl error.

---

#### Slice 2: GAP-2 — Post-Condition Guard on Ensure
**Verdict: NOT IMPLEMENTED — DEFERRED TO GAP-A**

- `ParseStateEnsure()` (line 419) goes directly from `ParseExpression(0)` to `Expect(TokenKind.Because)` — no `when`-guard check.
- `ParseEventEnsure()` (line 544) — same: no `when`-guard check.
- `StateEnsureNode` record has NO `PostConditionGuard` parameter — it's 6 fields (Span, Preposition, State, Guard, Condition, Message).
- `EventEnsureNode` record has NO `PostConditionGuard` parameter — it's 5 fields (Span, EventName, Guard, Condition, Message).
- `BuildNode` arms for `StateEnsure` and `EventEnsure` compile cleanly because no record change was made.
- The sample file integration tests correctly identify this as GAP-A with 2 files broken (`insurance-claim.precept`, `loan-application.precept`).

**Assessment:** This was intentionally deferred during implementation. The plan specified it as Slice 2 but the implementer left it unimplemented and tracked it as a known gap sentinel. This is the primary remaining parser gap.

---

#### Slice 3: GAP-3 — `is set` / `is not set` Postfix Expression
**Verdict: CLEAN — with design deviation (acceptable)**

- Implementation present and correct (Parser.cs line 1358–1374).
- Design deviation from plan: **Two separate AST nodes** (`IsSetExpression`, `IsNotSetExpression`) instead of one `IsSetExpression(Negated: bool)`. This is a reasonable improvement — avoids boolean parameters and makes downstream pattern matching cleaner. Acceptable.
- Precedence deviation: **60** in implementation vs. **40** in plan. The spec §2.1 says precedence 40 for `is set`. Implementation uses 60. This places `is set` above arithmetic (`+`/`-` at 50) which may cause subtle precedence issues. **Flagged for review** — see Critical Findings.
- `Operators.cs` does NOT have `OperatorKind.IsSet` or `Arity.Postfix`. Plan specified adding these. This is a known gap — the `is set` operator is parsed via hardcoded handler rather than catalog-driven dispatch. **This is the multi-token operator gap that Phase 2 addresses.**
- Tests: `ParseExpression_IsSet`, `_IsNotSet`, `_IsSet_InCondition`, `_IsNotSet_InCondition` — present and passing.
- Risk #2 comment correctly handled: dead `IdentifierExpression` branch removed, comment added at line 1400.

---

#### Slice 4: ExpressionFormKind Catalog
**Verdict: CLEAN**

- `ExpressionForms.cs` created with 10 members — correct enum, categories, metadata, and `GetMeta()` exhaustive switch.
- `HandlesCatalogExhaustivelyAttribute` exists with correct shape (`Type catalogEnum`).
- `HandlesFormAttribute` exists with correct shape (`object kind`).
- `PRECEPT0019` analyzer is fully generic — discovers `[HandlesCatalogExhaustively(typeof(T))]`, reads enum T, checks `[HandlesForm]` coverage. Severity = Warning (intentional).
- `CatalogAnalysisHelpers.cs` includes `"ExpressionFormKind"` — PRECEPT0007 enforces `GetMeta` exhaustiveness.
- Parser has `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]` on class (line 151) and `[HandlesForm]` annotations on `ParseExpression` (3 led forms) and `ParseAtom` (7 nud forms). All 10 forms covered.
- TypeChecker and GraphAnalyzer do NOT have `[HandlesCatalogExhaustively]` or `[HandlesForm]` annotations. This is why PRECEPT0019 is suppressed via `<WarningsNotAsErrors>PRECEPT0019</WarningsNotAsErrors>` in `Precept.csproj`. Plan specified annotating all four pipeline classes — only Parser was annotated.
- No Evaluator.cs file exists at all.
- `ExpressionFormCatalogTests.cs` present with completeness, GetMeta exhaustiveness, IsLeftDenotation, and category shape tests.
- `Precept0019Tests.cs` present with TP1 (missing handlers), TP2 (struct), TN1 (all handled), TN2 (multi-annotation), TN3 (no class marker).
- MCP `LanguageTool.cs` does not exist — the `expression_forms` section was not added to MCP output. The plan specified this, but the MCP tool infrastructure doesn't exist yet (only `PingTool.cs`). Not a plan violation — the prerequisite tool doesn't exist.

**Minor gaps:**
- Downstream stage annotations deferred (TypeChecker, GraphAnalyzer, Evaluator). This is why PRECEPT0019 is Warning + suppressed.
- Slice 13 (coverage assertion test `ExpressionFormCoverageTests.cs`) not implemented — no `test/Precept.Tests/Language/` directory exists.

---

#### Slice 5: GAP-6 — List Literal Expressions
**Verdict: CLEAN**

- `ParseAtom()` has `case TokenKind.LeftBracket: return ParseListLiteral()` (line 1540-1541).
- `ParseListLiteral()` method exists (line 1604) with correct structure.
- `ListLiteralExpression(SourceSpan, ImmutableArray<Expression>)` node exists.
- Tests: `_Empty`, `_SingleElement`, `_MultipleElements`, `_StringElements`, `_NestedExpressions` — all present.
- `[HandlesForm(ExpressionFormKind.ListLiteral)]` annotation on `ParseAtom` — present.

---

#### Slice 6: GAP-7 — Method Call on Member Access
**Verdict: CLEAN — with design improvement**

- LeftParen handler in Pratt loop (line 1377–1402) correctly handles `MemberAccessExpression`.
- Design improvement over plan: `MethodCallExpression` decomposes the target into `Receiver` + `MethodName` (string) instead of storing the full `MemberAccessExpression` as `Target`. This is cleaner for downstream consumers. Good deviation.
- Dead `IdentifierExpression` branch: correctly removed with comment (line 1400: "unreachable: identifiers resolve as FunctionCall in ParseAtom"). Risk #2 handled per option 2.
- Risk #3 (error path): `break` is used when `left` is not `MemberAccessExpression`. Per reviewer concern, this deviates from spec ("else → diagnostic"). Acceptable simplification — but noted.
- Precedence 90 for method call (plan said 80). Consistent with dot-access at 80 — method call binds tighter, which is correct.
- Tests: `_NoArgs`, `_SingleArg`, `_MultipleArgs`, `_ChainedAccess` — present.
- `[HandlesForm(ExpressionFormKind.MethodCall)]` annotation on `ParseExpression` — present.

---

#### Slice 7: GAP-8 — Spec `because` Optional Correction
**Verdict: CLEAN**

- `precept-language-spec.md` lines 810-811 now read `because StringExpr` (no `?`).
- Line 762 (rule declaration) also shows `because StringExpr` without `?`.
- Spec is consistent with parser behavior.

---

#### Slice 8: Test Coverage — Comparison Operators
**Verdict: CLEAN**

- All 6 tests present: `_LessThan`, `_LessThanOrEqual`, `_GreaterThanOrEqual`, `_Equals`, `_NotEquals`, `_NonAssociative_EmitsDiagnostic`.
- Each asserts correct `TokenKind` on `BinaryExpression`.

---

#### Slice 9: Test Coverage — `contains` Operator
**Verdict: CLEAN**

- `ParseExpression_Contains` and `ParseExpression_Contains_Precedence` present.
- Reviewer's suggestion to add chaining test was NOT implemented (minor gap).

---

#### Slice 10: Test Coverage — Collection Mutation Actions
**Verdict: CLEAN**

- All 8 tests present: `_ActionRemove`, `_ActionEnqueue`, `_ActionDequeue`, `_ActionDequeueInto`, `_ActionPush`, `_ActionPop`, `_ActionPopInto`, `_ActionClear`.

---

#### Slice 11: Test Coverage — Interpolated Strings
**Verdict: CLEAN**

- All 3 tests present: `_SingleHole`, `_MultipleHoles`, `_ExpressionInHole`.

---

#### Slice 12: Test Coverage — Sample File Integration Tests
**Verdict: CLEAN — with gap sentinels properly tracked**

- `SampleFileIntegrationTests.cs` exists (separate file, not in `ParserTests.cs` — reviewer suggested extending ParserTests, implementation chose separate file — acceptable).
- 21 clean / 7 known-broken split properly documented with GAP-A/B/C sentinels.
- Regression theory `KnownBrokenSampleFile_StillHasParserErrors` ensures gaps are tracked.
- Coverage count assertions (28 total, 7 broken) lock the expectation.

---

#### Slice 13: ExpressionForm Coverage Assertion
**Verdict: NOT IMPLEMENTED**

- `test/Precept.Tests/Language/ExpressionFormCoverageTests.cs` does not exist.
- The xUnit Layer 2 coverage test (iterate `ExpressionForms.All`, verify parser handles each form's lead tokens) was not written.
- The compile-time Layer 1 coverage (PRECEPT0007 + PRECEPT0019) is in place. The test-time Layer 2 was deferred.

---

### Critical Findings

#### Finding 1: `is set` Precedence Mismatch (Moderate)
- **What's wrong:** Plan and spec specify precedence 40 for `is set`. Implementation uses 60.
- **Impact:** At precedence 60, `is set` binds tighter than `+`/`-` (50). `x + y is set` would parse as `x + (y is set)` which is likely correct for field nullability checks, but differs from spec.
- **Spec reference:** §2.1 left-denotation table.
- **Proposed fix:** Verify intended semantics. If 60 is correct behavior (field.member is set should bind tight), update spec §2.1. If 40 is correct, change Parser.cs line 1360.

#### Finding 2: Slice 2 / GAP-2 Entirely Unimplemented
- **What's wrong:** `ParseStateEnsure()` and `ParseEventEnsure()` lack the `when`-guard post-condition parsing. The `PostConditionGuard` parameter was never added to either AST node. `BuildNode` was never updated.
- **Impact:** 2 sample files broken (insurance-claim, loan-application).
- **Spec reference:** §2.2 — `ensure BoolExpr ("when" BoolExpr)? because StringExpr`.
- **Proposed fix:** Part 2, Work Item D (GAP-A).

#### Finding 3: `OperatorKind.IsSet` and `Arity.Postfix` Not Added to Catalog
- **What's wrong:** Plan specified adding `OperatorKind.IsSet = 19` and `Arity.Postfix = 3` to `Operators.cs`. Neither was added. The `is set` operator is parsed via hardcoded handler with no catalog representation.
- **Impact:** `is set`/`is not set` are invisible to MCP vocabulary, LS hover, and grammar generation for operators. The catalog is incomplete per the Completeness Principle.
- **Spec reference:** `catalog-system.md` § Completeness Principle.
- **Proposed fix:** Part 2, Work Item A (Full DU Option B).

#### Finding 4: PRECEPT0019 Severity and Downstream Annotations
- **What's wrong:** PRECEPT0019 is Warning (not Error), and `<WarningsNotAsErrors>PRECEPT0019</WarningsNotAsErrors>` suppresses it. TypeChecker and GraphAnalyzer lack annotations. No Evaluator exists.
- **Impact:** Pipeline coverage enforcement is effectively disabled.
- **Proposed fix:** Part 2, Work Item C.

#### Finding 5: Slice 13 Not Delivered
- **What's wrong:** The xUnit coverage assertion test was not written.
- **Impact:** No automated test-time verification that the parser handles all declared expression forms.
- **Proposed fix:** Include in Phase 2 as a dependency of Work Item C (must pass before PRECEPT0019 → Error).

---

## Part 2: Extended Plan Outline

---

### Work Item A: Full DU Option B (Multi-Token Operators)

**Goal:** Represent `is set` and `is not set` in the Operators catalog using a discriminated union that distinguishes single-token and multi-token operators.

**Design:**

```csharp
// Operator.cs — replace flat OperatorMeta with DU:

public abstract record OperatorMeta(
    OperatorKind Kind,
    string Description,
    Arity Arity,
    Associativity Associativity,
    int Precedence,
    OperatorFamily Family,
    bool IsKeywordOperator = false,
    string? HoverDescription = null,
    string? UsageExample = null);

public sealed record SingleTokenOp(
    OperatorKind Kind,
    TokenMeta Token,
    string Description,
    Arity Arity,
    Associativity Associativity,
    int Precedence,
    OperatorFamily Family,
    bool IsKeywordOperator = false,
    string? HoverDescription = null,
    string? UsageExample = null)
    : OperatorMeta(Kind, Description, Arity, Associativity, Precedence, Family, IsKeywordOperator, HoverDescription, UsageExample);

public sealed record MultiTokenOp(
    OperatorKind Kind,
    IReadOnlyList<TokenMeta> Tokens,
    string Description,
    Arity Arity,
    Associativity Associativity,
    int Precedence,
    OperatorFamily Family,
    bool IsKeywordOperator = false,
    string? HoverDescription = null,
    string? UsageExample = null)
    : OperatorMeta(Kind, Description, Arity, Associativity, Precedence, Family, IsKeywordOperator, HoverDescription, UsageExample);
```

**New enum members:**
- `Arity.Postfix = 3`
- `OperatorKind.IsSet = 19`
- `OperatorKind.IsNotSet = 20`
- `OperatorFamily.Nullability = 5` (new family for null-check operators)

**New ExpressionFormKind member:**
- `ExpressionFormKind.PostfixOperation = 11` (11th catalog member)

**Slices:**
1. **A1:** Add `Arity.Postfix` + `OperatorKind.IsSet` + `OperatorKind.IsNotSet` + `OperatorFamily.Nullability` enum values. Add `GetMeta()` arms with `MultiTokenOp` subtype.
2. **A2:** Refactor `OperatorMeta` from flat record → abstract base + `SingleTokenOp`/`MultiTokenOp` sealed subtypes. Update all 18 existing `GetMeta()` arms to construct `SingleTokenOp`. Update `ByToken` (see Work Item B).
3. **A3:** Add `ExpressionFormKind.PostfixOperation = 11` to catalog. Add `GetMeta()` arm. Update `ExpressionForms.All` count tests. Add `[HandlesForm(ExpressionFormKind.PostfixOperation)]` to Parser's `ParseExpression` (the `is` handler).
4. **A4:** Update all consumers of `OperatorMeta.Token` (singular) to handle the DU correctly — grep for `.Token` access on `OperatorMeta` and switch to pattern matching where needed.

**Dependencies:** A1 → A2 → A3 → A4 (strict sequence). Work Item B is part of A2.

---

### Work Item B: ByToken Multi-Token Key

**Design recommendation: Lead-token lookup returning candidates list**

The current `ByToken: FrozenDictionary<(TokenKind, Arity), OperatorMeta>` can't handle `is set` and `is not set` both keyed by `(TokenKind.Is, Arity.Postfix)` — collision.

**Recommended design:**

```csharp
// Replace single ByToken with:

/// Single-token operator lookup (existing pattern, still fast for 18 single-token ops)
public static FrozenDictionary<(TokenKind, Arity), SingleTokenOp> BySingleToken { get; } =
    All.OfType<SingleTokenOp>().ToFrozenDictionary(m => (m.Token.Kind, m.Arity));

/// Multi-token operators keyed by lead token — returns candidates for Pratt look-ahead
public static FrozenDictionary<TokenKind, IReadOnlyList<MultiTokenOp>> ByLeadToken { get; } =
    All.OfType<MultiTokenOp>()
       .GroupBy(m => m.Tokens[0].Kind)
       .ToFrozenDictionary(g => g.Key, g => (IReadOnlyList<MultiTokenOp>)g.ToList().AsReadOnly());
```

**Rationale:**
- `BySingleToken` preserves the existing fast-path for the 18 single-token operators.
- `ByLeadToken` returns candidates for a given lead token. The Pratt parser checks `ByLeadToken[TokenKind.Is]` → gets `[IsSet, IsNotSet]`, then disambiguates by looking ahead for `Not`/`Set`.
- No params overload needed — the lookup is two-step (lead token → candidates → look-ahead), which maps naturally to how the parser already works.
- Alternative rejected: `ByTokenSequence(params TokenKind[])` — requires the caller to already know the full sequence, which defeats the purpose of a discovery lookup.

**Slice:** Part of A2 (DU refactor includes the lookup restructure).

---

### Work Item C: PRECEPT0019 → Error

**Prerequisites (must complete in order):**
1. `ExpressionFormKind.PostfixOperation` exists (Work Item A3)
2. `[HandlesForm(ExpressionFormKind.PostfixOperation)]` on Parser's `is` handler (Work Item A3)
3. TypeChecker annotated with `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]` + `[HandlesForm]` for all 11 members
4. GraphAnalyzer annotated similarly
5. All PRECEPT0019 warnings resolve to zero

**Slices:**
1. **C1:** Annotate TypeChecker — add class marker + method annotations for all 11 ExpressionFormKind members. (TypeChecker currently has `NotImplementedException` — annotations can point to the method that throws, signaling "handled by throwing".)
2. **C2:** Annotate GraphAnalyzer — same pattern.
3. **C3:** Flip PRECEPT0019 severity: `DiagnosticSeverity.Warning` → `DiagnosticSeverity.Error` in `Precept0019PipelineCoverageExhaustiveness.cs`.
4. **C4:** Remove `<WarningsNotAsErrors>PRECEPT0019</WarningsNotAsErrors>` from `Precept.csproj`.
5. **C5:** Verify build green (no PRECEPT0019 errors).

**Dependencies:** A3 → C1 → C2 → C3 → C4 → C5 (strict).

---

### Work Item D: GAP-A (when-guard on StateEnsure/EventEnsure)

**Problem:** 2 sample files broken. `ParseStateEnsure()` and `ParseEventEnsure()` don't check for `when` between condition and `because`.

**Parser fix design:**

```csharp
// In ParseStateEnsure(), after var condition = ParseExpression(0):
Expression? postConditionGuard = null;
if (Current().Kind == TokenKind.When)
{
    Advance(); // consume 'when'
    postConditionGuard = ParseExpression(0);
}
var because = Expect(TokenKind.Because);
// ... rest unchanged
```

Same pattern in `ParseEventEnsure()`.

**AST node changes:**
- `StateEnsureNode` gains `Expression? PostConditionGuard` (7th parameter)
- `EventEnsureNode` gains `Expression? PostConditionGuard` (6th parameter)
- `BuildNode` arms for both: add `null` for `PostConditionGuard` (Risk #1 from plan)

**Slices:**
1. **D1:** Add `PostConditionGuard` to both records + update `BuildNode` arms (null) + parse logic in both methods.
2. **D2:** Tests — `Parse_InStateEnsure_WithPostConditionGuard`, `Parse_ToStateEnsure_WithPostConditionGuard`, `Parse_OnEventEnsure_WithPostConditionGuard`, `Parse_InStateEnsure_NoGuard_StillWorks`.
3. **D3:** Remove `insurance-claim.precept` and `loan-application.precept` from `KnownBrokenFiles` in `SampleFileIntegrationTests.cs`. Verify they parse clean.

**Dependencies:** None — can start immediately.

---

### Work Item E: GAP-B (Modifiers after computed expressions)

**Problem:** 4 sample files broken. Syntax: `field X -> expr modifier` (e.g., `field Total -> sum(items.amount) readonly`). Parser handles modifiers only before `->`, not after the computed expression.

**Scope:** The field-declaration parser consumes modifiers before `default` or `->`, but not after the RHS expression.

**Slices:**
1. **E1:** In Parser.cs field-declaration logic, after parsing the computed expression (`->`), check for trailing modifiers (same modifier-parse loop used before `default`). Append them to the field's modifier list.
2. **E2:** Tests — `Parse_ComputedField_WithTrailingReadonly`, `Parse_ComputedField_WithTrailingHidden`.
3. **E3:** Remove 4 files from `KnownBrokenFiles`. Verify all pass.

**Dependencies:** None — can start immediately.

---

### Work Item F: GAP-C (Keyword-as-member-name)

**Problem:** 1 sample file broken (`building-access-badge-request.precept`). `.min` and `.max` after dot-access are rejected because `Min`/`Max` are token kinds (keywords), not `Identifier`.

**Scope:** The MemberAccess handler at Parser.cs line 1351 does `Expect(TokenKind.Identifier)`. When the token after `.` is a keyword like `Min`/`Max`, this emits `ExpectedToken`.

**Approach:** After dot, accept `Identifier` OR any contextual keyword that is valid as a member name. The safest approach:

```csharp
// Instead of: var member = Expect(TokenKind.Identifier);
// Use: var member = ExpectMemberName(); // accepts Identifier or keyword-as-member
```

`ExpectMemberName()` accepts the current token if it's `Identifier` or if it's in a whitelist of keywords that can serve as member names in dot-access position (Min, Max, Set, Contains, etc. — effectively any non-structural keyword).

**Slices:**
1. **F1:** Add `ExpectMemberName()` helper. Define the whitelist (all keywords except structural ones like `precept`, `field`, `state`, `event`, `from`, `to`, `in`, `on`, `ensure`, `because`, `rule`, `when`, `access`, `omit`). Replace `Expect(TokenKind.Identifier)` in MemberAccess handler.
2. **F2:** Tests — `Parse_MemberAccess_KeywordAsFieldName_Min`, `_Max`, `_Set`, `_Contains`.
3. **F3:** Remove `building-access-badge-request.precept` from `KnownBrokenFiles`. Verify passes.

**Dependencies:** None — can start immediately.

---

### Work Item G: Remediations from Review

#### G1: Slice 13 — ExpressionForm Coverage Assertion Test
- Create `test/Precept.Tests/Language/ExpressionFormCoverageTests.cs`
- Test iterates `ExpressionForms.All`; for each nud form, verifies parser handles its lead tokens; for led forms, verifies the Pratt loop handles them.
- **Dependency:** After Work Item A3 (PostfixOperation must exist).

#### G2: `is set` Precedence Audit
- Verify whether precedence 60 (current) or 40 (plan/spec) is semantically correct.
- If 60 is correct: update spec §2.1 left-denotation table.
- If 40 is correct: change Parser.cs line 1360.
- **Dependency:** None.

#### G3: Missing `contains` chaining test
- Add `ParseExpression_Contains_ChainedNonAssociative` test: `tags contains "a" contains "b"` → `NonAssociativeComparison` diagnostic.
- **Dependency:** None.

---

### Dependency Graph

```
   ┌──────────────────────────────────────────────────────────────┐
   │                    INDEPENDENT (can start now)                │
   ├──────────────────────────────────────────────────────────────┤
   │  D1-D3 (GAP-A: when-guard)                                  │
   │  E1-E3 (GAP-B: modifiers after computed)                    │
   │  F1-F3 (GAP-C: keyword-as-member-name)                      │
   │  G2    (is set precedence audit)                             │
   │  G3    (contains chaining test)                              │
   └──────────────────────────────────────────────────────────────┘
                            │
                            ▼
   ┌──────────────────────────────────────────────────────────────┐
   │                    OPERATOR DU (sequential)                   │
   ├──────────────────────────────────────────────────────────────┤
   │  A1: Add Postfix arity + IsSet/IsNotSet enum + Nullability   │
   │  A2: Refactor OperatorMeta → DU + ByToken restructure (B)   │
   │  A3: Add PostfixOperation to ExpressionFormKind              │
   │  A4: Fix all OperatorMeta.Token consumers                    │
   └──────────────────────────────────────────────────────────────┘
                            │
                            ▼
   ┌──────────────────────────────────────────────────────────────┐
   │                PRECEPT0019 → Error (sequential)              │
   ├──────────────────────────────────────────────────────────────┤
   │  C1: Annotate TypeChecker                                    │
   │  C2: Annotate GraphAnalyzer                                  │
   │  G1: Coverage assertion test (Slice 13)                      │
   │  C3: Flip severity → Error                                   │
   │  C4: Remove WarningsNotAsErrors                              │
   │  C5: Build green verification                                │
   └──────────────────────────────────────────────────────────────┘
```

**Critical path:** D/E/F → A1 → A2 → A3 → A4 → C1 → C2 → G1 → C3 → C4 → C5

**Parallelism:** D, E, F, G2, G3 are fully independent and can execute in parallel with each other and with A1.

---

### Recommended Execution Order

1. **Phase 2a (parallel):** D1-D3, E1-E3, F1-F3, G2, G3 — fixes all 7 broken sample files
2. **Phase 2b (sequential):** A1 → A2/B → A3 → A4 — full DU + multi-token lookup
3. **Phase 2c (sequential):** C1 → C2 → G1 → C3 → C4 → C5 — PRECEPT0019 promotion

After Phase 2c completes: all 28 sample files clean, PRECEPT0019 is Error, no WarningsNotAsErrors, full catalog coverage enforced.

---

### Acceptance Gate

**"Done" means ALL of the following are true:**

1. `dotnet build` — zero errors, zero warnings (no suppressions)
2. `dotnet test` — all tests pass
3. PRECEPT0019 severity = `DiagnosticSeverity.Error`
4. `<WarningsNotAsErrors>` removed from `Precept.csproj`
5. `KnownBrokenFiles` in `SampleFileIntegrationTests.cs` is empty (all 28 samples clean)
6. `KnownBrokenSampleFile_StillHasParserErrors` test removed or adapted
7. `OperatorKind.IsSet` and `OperatorKind.IsNotSet` exist in catalog with `MultiTokenOp` metadata
8. `Arity.Postfix` exists
9. `ExpressionFormKind.PostfixOperation` exists with correct metadata
10. `ExpressionFormCoverageTests.cs` exists and passes (Layer 2 assertion)
11. TypeChecker, GraphAnalyzer annotated with `[HandlesCatalogExhaustively]` + full `[HandlesForm]` coverage
12. Spec §2.1 precedence table matches implementation (is set precedence resolved)
13. No GitHub issues deferred. No holes.
