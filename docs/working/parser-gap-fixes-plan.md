# Parser Gap Fixes — Implementation Plan

> **Branch:** `spike/Precept-V2`  
> **Author:** Frank (Lead/Architect)  
> **Date:** 2026-05-01  
> **Implementer:** George  
> **Baseline:** 2107 tests passing, 0 failing

---

## 1. Overview

This plan addresses 5 functional parser gaps (GAP-1, GAP-2, GAP-3, GAP-6, GAP-7), 1 spec defect (GAP-8), and the highest-priority test coverage gaps identified by the full spec audit and Soup Nazi's 38-gap coverage matrix.

The parser on `spike/Precept-V2` is ~88% spec-complete. The remaining 12% is concentrated in:
- Expression atom handling (typed constants, list literals)
- Pratt left-denotation handling (`is set`, method calls)
- Post-condition guard parsing in `ensure` declarations

All gaps have spec-confirmed grammar productions (§2.1 null-denotation/left-denotation tables, §2.2 declaration grammar). None are aspirational.

## Implementation Status

### Phase 1 — Parser Gap Fixes (ALL COMPLETE ✅)
All 13 slices shipped on `spike/Precept-V2`. Test baseline: 2107 → 2482 (+375 tests, 0 failing).

| Slice | Description | Status |
|-------|-------------|--------|
| 1 | GAP-1: TypedConstant/InterpolatedTypedConstant atom handling | ✅ Done |
| 2 | GAP-2: post-condition ensure guard on event declarations | ✅ Done |
| 3 | GAP-3: `is set` / `is not set` Pratt led handler | ✅ Done |
| 4 | ExpressionFormKind catalog (13th) + annotation bridge + PRECEPT0019 | ✅ Done |
| 5 | GAP-6: list literals | ✅ Done |
| 6 | GAP-7: method calls | ✅ Done |
| 7 | GAP-8: spec fix (removed `?` from `because` in §2.2) | ✅ Done |
| 8 | Comparison operator tests | ✅ Done |
| 9 | `contains` tests | ✅ Done |
| 10 | Collection mutation tests | ✅ Done |
| 11 | Interpolated string tests | ✅ Done |
| 12 | Sample file integration tests (58 tests; GAP-A/B/C sentinel-gated) | ✅ Done |
| 13 | ExpressionFormCoverageTests (reflection + round-trip) | ✅ Done |

### Phase 2 — Extended Gap Resolution ✅ PHASE 2c COMPLETE

**Baseline:** 2247 tests passing (2240 passing + 7 intentional KnownBrokenFiles failures; Phase 1 exit). Post-Phase 2a: 2261 tests, 0 failing. Post-Phase 2b: 2274 tests, 0 failing. Post-Phase 2c: 2300 tests, 0 failing.

#### Phase 2a — Parallel (independent, no catalog changes)

| Slice | Work Item | Description | Status |
|-------|-----------|-------------|--------|
| 14 | D | GAP-A: `when`-guard on `ParseStateEnsure`/`ParseEventEnsure` (method bodies only, no AST changes) | ✅ Done |
| 15 | E | GAP-B: Modifiers after computed (`->`) field expressions (`ParseFieldDeclaration` rewrite) | ✅ Done |
| 16 | F | GAP-C: Keyword-as-member-name (`Min`/`Max`) in `MemberAccess` handler | ✅ Done |
| 17 | G2 | `is set` precedence audit: resolve 60 vs spec 40, sync parser and spec | ✅ Done |
| 18 | G3 | `contains` chaining non-associativity test | ✅ Done |

#### Phase 2b — Sequential (full DU, depends on 2a)

| Slice | Work Item | Description | Status |
|-------|-----------|-------------|--------|
| 19 | A1 | Enum additions: `Arity.Postfix`, `OperatorKind.IsSet/IsNotSet`, `OperatorFamily.Presence` | ✅ Done |
| 20 | A2/B | `OperatorMeta` → DU (`SingleTokenOp`/`MultiTokenOp`) + `ByToken`/`ByTokenSequence` restructure | ✅ Done |
| 21 | A3 | `ExpressionFormKind.PostfixOperation` (11th member) + `[HandlesForm]` on `is`-handler | ✅ Done |
| 22 | A4 | Consumer call site audit — no stragglers; build clean, 2274 tests passing | ✅ Done |

#### Phase 2c — Sequential (PRECEPT0019 promotion, depends on 2b)

| Slice | Work Item | Description | Status |
|-------|-----------|-------------|--------|
| 23 | C1 | Annotate `TypeChecker` with `[HandlesCatalogExhaustively]` + `[HandlesForm]` for all 11 forms | ✅ Done |
| 24 | C2 | Annotate `GraphAnalyzer` similarly | ✅ Done |
| 25 | G1 | Write `ExpressionFormCoverageTests.cs` (Slice 13 makeup) | ✅ Done |
| 26 | C3–C5 | Flip PRECEPT0019 `Warning` → `Error`, remove `WarningsNotAsErrors`, verify build | ✅ Done |

#### Phase 2d — Independent (structural, no functional dependencies)

| Slice | Work Item | Description | Status |
|-------|-----------|-------------|--------|
| 27 | S1 | Split `Parser.cs` (1757 lines) into 3 partial files for AI agent manageability | ⏳ Pending |
| 28 | A5 | PRECEPT0020 — Operators `ByToken` / `OperatorPrecedence` collision analyzer | ⏳ Pending |

#### Phase 2e — Analyzer Gap Closure (Slices 29–32)

| Slice | Work Item | Description | Status |
|-------|-----------|-------------|--------|
| 29 | S2 | `KeywordsValidAsMemberName` → `IsValidAsMemberName` flag on `TokenMeta` (catalog cleanup) | ⏳ Pending |
| 30 | A6 | PRECEPT0021 — Tokens duplicate `Text` check | ⏳ Pending |
| 31 | A7 | PRECEPT0022 — OperatorMeta inline Token reference | ⏳ Pending |
| 32 | A8 | PRECEPT0023 — OperatorMeta DU shape invariants (deferred — depends on Phase 2b completion) | ⏳ Pending (deferred) |

Shane's directive: no deferred items, no holes — all gaps resolved on this spike before type-checker work begins.

---

## 2. Scope

### In Scope
- **GAP-1:** `TypedConstant` / `TypedConstantStart` atom handling in `ParseAtom()`
- **GAP-2:** Post-condition guard (`ensure Cond when Guard because "msg"`) in `ParseStateEnsure()` and `ParseEventEnsure()`
- **GAP-3:** `is set` / `is not set` left-denotation in Pratt loop
- **GAP-6:** `ListLiteralExpression` — new AST node + `LeftBracket` atom case
- **GAP-7:** `MethodCallExpression` — new AST node + `LeftParen` left-denotation
- **GAP-8:** Spec §2.2 `because` optional marker correction (text-only fix)
- **Test Coverage:** Comparison operators, collection mutations, `contains`, interpolated strings, sample file integration tests
- **Catalog:** `ExpressionFormKind` / `ExpressionFormMeta` — new 13th catalog (prerequisite for GAP-6 and GAP-7, Slice 4)
- **MCP Tooling:** `LanguageTool.cs` — add `expression_forms` section to `precept_language` output (Slice 4)

### Explicitly Out of Scope
- Type checker changes (blocked by `NotImplementedException`)
- New diagnostic codes (except where required by GAP-7's invalid-call-target case)
- Evaluator / runtime changes
- Language server changes (LS code: no changes needed; `LanguageTool.cs` MCP update is in scope as part of Slice 4)
- Catalog changes beyond `OperatorKind.IsSet` (Slice 3) and `ExpressionFormKind` (Slice 4) additions — all other required tokens/operators already exist in catalogs

---

## 3. Implementation Slices

### Slice 1: GAP-1 — Typed Constant Atom Handling — ✅ DONE

**Problem:** `ParseAtom()` handles `StringLiteral` but not `TypedConstant` (116), `TypedConstantStart` (117), `TypedConstantMiddle` (118), or `TypedConstantEnd` (119). Single-quoted literals like `'USD'` or `'2026-04-15'` produce `ExpectedToken` diagnostics.

**Files to modify:**

| File | Method/Location | Change |
|------|----------------|--------|
| `src/Precept/Pipeline/Parser.cs` | `ParseAtom()` switch, after `case TokenKind.StringStart` | Add `case TokenKind.TypedConstant` → `LiteralExpression` |
| `src/Precept/Pipeline/Parser.cs` | `ParseAtom()` switch, after above | Add `case TokenKind.TypedConstantStart` → `ParseInterpolatedTypedConstant()` |
| `src/Precept/Pipeline/Parser.cs` | New method after `ParseInterpolatedString()` | Add `ParseInterpolatedTypedConstant()` — same reassembly loop pattern using `TypedConstantMiddle` / `TypedConstantEnd` |
| `src/Precept/Pipeline/SyntaxNodes/Expressions/` | New file `TypedConstantExpression.cs` | Add `TypedConstantExpression(SourceSpan, Token Value) : Expression(Span)` |
| `src/Precept/Pipeline/SyntaxNodes/Expressions/` | New file `InterpolatedTypedConstantExpression.cs` | Add `InterpolatedTypedConstantExpression(SourceSpan, ImmutableArray<InterpolationPart> Parts) : Expression(Span)` |

**What to add/change:**

```csharp
// In ParseAtom() switch:
case TokenKind.TypedConstant:
    return new TypedConstantExpression(current.Span, Advance());

case TokenKind.TypedConstantStart:
    return ParseInterpolatedTypedConstant();
```

`ParseInterpolatedTypedConstant()` mirrors `ParseInterpolatedString()` exactly but uses `TokenKind.TypedConstantMiddle` and `TokenKind.TypedConstantEnd` instead of `StringMiddle`/`StringEnd`.

**Tests to write** (in `test/Precept.Tests/ExpressionParserTests.cs`):

| Test Method | Verifies |
|-------------|----------|
| `ParseExpression_TypedConstant_Simple` | `'USD'` → `TypedConstantExpression` with correct `Value.Text` |
| `ParseExpression_TypedConstant_Date` | `'2026-04-15'` → `TypedConstantExpression` |
| `ParseExpression_TypedConstant_Interpolated` | `'Hello {name}'` → `InterpolatedTypedConstantExpression` with text + expression parts |
| `ParseExpression_TypedConstant_InFieldDefault` | Full declaration `field Amt as money default '100 USD'` → clean parse, no diagnostics |

**Regression anchors:** All existing `ExpressionParserTests` (atoms, binary ops, precedence, boundary termination, negative folding).

---

### Slice 2: GAP-2 — Post-Condition Guard on Ensure — ✅ DONE

**Problem:** `ParseStateEnsure()` and `ParseEventEnsure()` call `ParseExpression(0)` for the condition, but `When` is in `StructuralBoundaryTokens` (line 95 of Parser.cs), so the expression parser terminates the condition early when it hits `when`. Then `Expect(TokenKind.Because)` sees `when` and emits a bogus diagnostic.

The spec (§2.2, post-`50a459c`) defines: `ensure BoolExpr ("when" BoolExpr)? because StringExpr`

**Files to modify:**

| File | Method/Location | Change |
|------|----------------|--------|
| `src/Precept/Pipeline/Parser.cs` | `ParseStateEnsure()` (line ~418) | After parsing condition, check for `When` → parse guard → then `Expect(Because)` |
| `src/Precept/Pipeline/Parser.cs` | `ParseEventEnsure()` (line ~543) | Same pattern as `ParseStateEnsure()` |
| `src/Precept/Pipeline/SyntaxNodes/StateEnsureNode.cs` | Record definition | Add `Expression? PostConditionGuard` parameter (or reuse existing `StashedGuard` field) |
| `src/Precept/Pipeline/SyntaxNodes/EventEnsureNode.cs` | Record definition | Add `Expression? PostConditionGuard` parameter (or reuse existing `StashedGuard` field) |

**What to add/change:**

The key insight: `When` is already a boundary token for expressions — that's correct and desired. The bug is that `ParseStateEnsure()` doesn't check for a trailing `when` after the condition expression terminates.

```csharp
private StateEnsureNode ParseStateEnsure(SourceSpan start, Token preposition, StateTargetNode anchor, Expression? stashedGuard)
{
    Advance(); // consume 'ensure'
    var condition = ParseExpression(0);  // terminates at 'when' — correct
    
    // Post-condition guard (new spec form: ensure Cond when Guard because "msg")
    Expression? postConditionGuard = null;
    if (Current().Kind == TokenKind.When)
    {
        Advance(); // consume 'when'
        postConditionGuard = ParseExpression(0);
    }
    
    var because = Expect(TokenKind.Because);
    var message = ParseExpression(0);
    
    return new StateEnsureNode(
        SourceSpan.Covering(start, message.Span),
        preposition, anchor, stashedGuard, condition, postConditionGuard, message);
}
```

Same pattern applies to `ParseEventEnsure()`.

**Design decision: stashed vs. post-condition guards.** `stashedGuard` is parsed in the disambiguator (pre-condition, before the `ensure` keyword). `postConditionGuard` is parsed after the condition expression (between condition and `because`). Both can coexist on the AST node — downstream passes determine which is active. The stashed form (`in Draft when Cond ensure ...`) is the OLD spec form; the post-condition form (`ensure Cond when Guard`) is the CURRENT spec form. Both remain parseable for migration purposes.

**Tests to write** (in `test/Precept.Tests/ParserTests.cs`):

| Test Method | Verifies |
|-------------|----------|
| `Parse_InStateEnsure_WithPostConditionGuard` | `in Approved ensure DecisionNote != null when FraudFlag because "msg"` → `StateEnsureNode` with non-null `PostConditionGuard` |
| `Parse_ToStateEnsure_WithPostConditionGuard` | `to Submitted ensure Amount > 0 when NotExempt because "msg"` → same pattern for `to` preposition |
| `Parse_OnEventEnsure_WithPostConditionGuard` | `on Submit ensure Valid when Active because "msg"` → `EventEnsureNode` with non-null `PostConditionGuard` |
| `Parse_InStateEnsure_NoGuard_StillWorks` | Existing simple form regression: `in Draft ensure Amount > 0 because "msg"` → `PostConditionGuard` is null |

**Regression anchors:** Existing `ParserTests` Slice 4.2 (`in State ensure` simple form), Slice 4.4 (`on Event ensure` form).

---

### Slice 3: GAP-3 — `is set` / `is not set` Postfix Expression — ✅ DONE

**Problem:** The Pratt left-denotation loop (line ~1327) has no handler for `TokenKind.Is`. When `Is` appears after an expression, the loop falls through `OperatorPrecedence.TryGetValue` (Is is not in the binary operators catalog — it's a multi-token operator) and breaks, leaving `is set` unconsumed.

The spec §2.1 left-denotation: `Is` → `IsSetExpression` — consumes optional `Not`, then `Set`. Precedence 40.

**Files to modify:**

| File | Method/Location | Change |
|------|----------------|--------|
| `src/Precept/Pipeline/Parser.cs` | `ParseExpression()` Pratt loop, after `Dot` handler (line ~1343) | Add `Is` handler before the `OperatorPrecedence` check |
| `src/Precept/Pipeline/SyntaxNodes/Expressions/` | New file `IsSetExpression.cs` | Add `IsSetExpression(SourceSpan, Expression Operand, bool Negated) : Expression(Span)` |
| `src/Precept/Language/Operators.cs` | `OperatorKind` enum, `Arity` enum, `GetMeta()` | Add `Arity.Postfix = 3`, `OperatorKind.IsSet = 19`, `GetMeta()` arm for `IsSet` |

**What to add/change:**

```csharp
// In ParseExpression() Pratt loop, after the Dot handler and before OperatorPrecedence check:
if (current.Kind == TokenKind.Is)
{
    if (minPrecedence > 40) break;
    Advance(); // consume 'is'
    bool negated = Match(TokenKind.Not); // optional 'not'
    var setToken = Expect(TokenKind.Set); // require 'set'
    left = new IsSetExpression(
        SourceSpan.Covering(left.Span, setToken.Span), left, negated);
    continue;
}
```

**`Operators.cs` update:** Add `Arity.Postfix = 3` to the `Arity` enum, `OperatorKind.IsSet = 19` to the `OperatorKind` enum, and a `GetMeta()` arm for `IsSet` (arity `Postfix`, precedence 40, lead token `TokenKind.Is`).

**Tests to write** (in `test/Precept.Tests/ExpressionParserTests.cs`):

| Test Method | Verifies |
|-------------|----------|
| `ParseExpression_IsSet` | `field is set` → `IsSetExpression { Negated = false }` |
| `ParseExpression_IsNotSet` | `field is not set` → `IsSetExpression { Negated = true }` |
| `ParseExpression_IsSet_InLogicalContext` | `field is set and x > 0` → top-level `And`, left is `IsSetExpression` |
| `ParseExpression_IsSet_WithMemberAccess` | `claim.note is set` → `IsSetExpression` with `MemberAccessExpression` operand |
| `ParseExpression_IsSet_Precedence` | Verify precedence 40 — `is set` binds tighter than `and` (20) but looser than `+` (50) |

**Regression anchors:** All existing `ExpressionParserTests`, especially `TerminatesAtWhen` and `TerminatesAtBecause` (boundary behavior unchanged).

---

### Slice 4: `ExpressionFormKind` Catalog — Expression Form Taxonomy — ✅ DONE

**Problem:** GAP-6 (list literals) and GAP-7 (method calls) introduce new expression forms. Per the Completeness Principle in `catalog-system.md`, language-surface vocabulary that consumers need — MCP `precept_language` output, LS expression-context completions, grammar generator expression-pattern passes — must be cataloged. Expression forms are that vocabulary: a machine-readable taxonomy of what shapes the expression parser can produce and what Pratt parser role each form plays. No such catalog currently exists.

**Design decision:** Shane approved creating a separate 13th catalog on 2026-05-01. Option B (extending `ConstructKind`) was rejected: metadata shapes are incompatible (zero overlapping domain-specific fields), and extension would require either inapplicable nullable fields (anti-pattern per `catalog-system.md`) or a DU (reinventing two catalogs with shared plumbing).

**Files to create/modify:**

| File | Method/Location | Change |
|------|----------------|--------|
| `src/Precept/Language/ExpressionForms.cs` | New file | Create `ExpressionFormKind` enum, `ExpressionCategory` enum, `ExpressionFormMeta` record, `ExpressionForms` static class |
| `tools/Precept.Mcp/Tools/LanguageTool.cs` | `precept_language` response assembly | Add `expression_forms` section from `ExpressionForms.All`, grouped by `Category` |
| `src/Precept.Analyzers/CatalogAnalysisHelpers.cs` | `CatalogEnumNames` list | Add `"ExpressionFormKind"` so PRECEPT0007 enforces exhaustive `GetMeta` switches at compile time |
| `src/Precept/HandlesCatalogExhaustivelyAttribute.cs` | Existing file | Already exists — verify the stackable class-level catalog coverage marker shape before modifying if needed |
| `src/Precept/Language/HandlesFormAttribute.cs` | Existing file | Already exists — verify the `[HandlesForm]` annotation attribute shape (`Kind`) before modifying if needed; call-sites still use typed enum literals such as `ExpressionFormKind.Identifier` |
| `src/Precept.Analyzers/Precept0019PipelineCoverageExhaustiveness.cs` | Existing file | Modify PRECEPT0019 — fully generic analyzer: discovers any class marked with `[HandlesCatalogExhaustively(typeof(T))]`, reads enum `T`, and checks every member of `T` has at least one `[HandlesForm(X)]` method in that class; future catalogs require zero analyzer changes |
| `src/Precept/Pipeline/Parser.cs` | ~10 expression handler methods + class declaration | Add `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]` on the class and `[HandlesForm(ExpressionFormKind.X)]` on each existing form handler; Slices 5 and 6 add annotations for the new forms they introduce |
| `src/Precept/Pipeline/TypeChecker.cs` | Expression-handling methods + class declaration | Add `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]` on the class and `[HandlesForm(ExpressionFormKind.X)]` annotations to satisfy PRECEPT0019 |
| `src/Precept/Pipeline/Evaluator.cs` | Expression-handling methods + class declaration | Add `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]` on the class and `[HandlesForm(ExpressionFormKind.X)]` annotations to satisfy PRECEPT0019 |
| `src/Precept/Pipeline/GraphAnalyzer.cs` | Expression-handling methods + class declaration | Add `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]` on the class and `[HandlesForm(ExpressionFormKind.X)]` annotations to satisfy PRECEPT0019 |

**What to create — `src/Precept/Language/ExpressionForms.cs`:**

```csharp
namespace Precept.Language;

public enum ExpressionFormKind
{
    // Atoms — null-denotation (nud): start a new expression
    Literal         = 1,
    Identifier      = 2,
    Grouped         = 3,

    // Composites — infix or prefix structural forms
    BinaryOperation = 4,
    UnaryOperation  = 5,
    MemberAccess    = 6,
    Conditional     = 7,

    // Invocations — call forms
    FunctionCall    = 8,
    MethodCall      = 9,

    // Collections — aggregate literal forms
    ListLiteral     = 10,
}

public enum ExpressionCategory { Atom = 1, Composite = 2, Invocation = 3, Collection = 4 }

public sealed record ExpressionFormMeta(
    ExpressionFormKind        Kind,
    ExpressionCategory        Category,
    bool                      IsLeftDenotation,
    IReadOnlyList<TokenKind>  LeadTokens,
    string                    HoverDocs);

public static class ExpressionForms
{
    public static ExpressionFormMeta GetMeta(ExpressionFormKind kind) => kind switch
    {
        ExpressionFormKind.Literal          => new(kind, ExpressionCategory.Atom,       false, [TokenKind.StringStart, TokenKind.TypedConstant, TokenKind.TypedConstantStart, TokenKind.IntegerLiteral, TokenKind.DecimalLiteral, TokenKind.True, TokenKind.False, TokenKind.Null],   "A literal value: string, number, boolean, typed constant, or null."),
        ExpressionFormKind.Identifier       => new(kind, ExpressionCategory.Atom,       false, [TokenKind.Identifier],                                                                                                                                                               "A bare field or parameter name."),
        ExpressionFormKind.Grouped          => new(kind, ExpressionCategory.Atom,       false, [TokenKind.LeftParen],                                                                                                                                                                 "A parenthesized expression: (expr)."),
        ExpressionFormKind.BinaryOperation  => new(kind, ExpressionCategory.Composite,  true,  [],                                                                                                                                                                                    "An infix binary operation: left op right."),
        ExpressionFormKind.UnaryOperation   => new(kind, ExpressionCategory.Composite,  false, [TokenKind.Not, TokenKind.Minus],                                                                                                                                                      "A prefix unary operation: op expr."),
        ExpressionFormKind.MemberAccess     => new(kind, ExpressionCategory.Composite,  true,  [TokenKind.Dot],                                                                                                                                                                       "Dot-access on an expression: target.member."),
        ExpressionFormKind.Conditional      => new(kind, ExpressionCategory.Composite,  false, [TokenKind.If],                                                                                                                                                                        "A conditional expression: if cond then valueA else valueB."),
        ExpressionFormKind.FunctionCall     => new(kind, ExpressionCategory.Invocation, false, [TokenKind.Identifier],                                                                                                                                                                "A named function call: name(args)."),
        ExpressionFormKind.MethodCall       => new(kind, ExpressionCategory.Invocation, true,  [TokenKind.LeftParen],                                                                                                                                                                 "A method call on an expression: target.method(args)."),
        ExpressionFormKind.ListLiteral      => new(kind, ExpressionCategory.Collection, false, [TokenKind.LeftBracket],                                                                                                                                                               "A list literal: [elem, elem, ...]."),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    public static IReadOnlyList<ExpressionFormMeta> All { get; } =
        Enum.GetValues<ExpressionFormKind>().Select(GetMeta).ToList().AsReadOnly();
}
```

**LanguageTool.cs change:** Add an `"expression_forms"` key to the `precept_language` JSON response body. Serialize `ExpressionForms.All`, grouping entries by `Category`. Each entry includes `kind`, `category`, `is_left_denotation`, `lead_tokens`, and `hover_docs`.

**Analyzer registration (compile-time coverage enforcement):** Add `"ExpressionFormKind"` to `CatalogAnalysisHelpers.CatalogEnumNames` in `src/Precept.Analyzers/`. PRECEPT0007 already enforces exhaustive switches on all registered catalog enum names — once `ExpressionFormKind` is in that list, any future member added without a `GetMeta` switch arm is a compile-time error. No new analyzer infrastructure is needed. This is Layer 1 of the two-layer coverage pattern.

**`HandlesCatalogExhaustivelyAttribute` + `HandlesFormAttribute` (annotation bridge, shipped in Slice 4):** Verify the existing `src/Precept/HandlesCatalogExhaustivelyAttribute.cs` and `src/Precept/Language/HandlesFormAttribute.cs` shapes before modifying them:

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class HandlesCatalogExhaustivelyAttribute : Attribute
{
    public HandlesCatalogExhaustivelyAttribute(Type catalogType) => CatalogType = catalogType;
    public Type CatalogType { get; }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class HandlesFormAttribute : Attribute
{
    public HandlesFormAttribute(object kind) => Kind = kind;
    public object Kind { get; }
}
```

Method annotations still use typed enum literals at the call-site — e.g. `[HandlesForm(ExpressionFormKind.Identifier)]` — but the attribute constructor accepts `object` so the same attribute works for any catalog enum. Class-level coverage markers use `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]`; the attribute is stackable, so a future class handling multiple catalogs declares one attribute per catalog.

**PRECEPT0019 — Pipeline Coverage Analyzer:** Modify `src/Precept.Analyzers/Precept0019PipelineCoverageExhaustiveness.cs`. The analyzer is fully generic: it discovers every class marked with `[HandlesCatalogExhaustively(typeof(T))]`, reads the declared enum `T`, and verifies that every member of `T` has at least one `[HandlesForm(X)]`-decorated method in that same class. For the Slice 4 expression-form pass, that means `Parser`, `TypeChecker`, `Evaluator`, and `GraphAnalyzer` each declare `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]` and must cover every `ExpressionFormKind` member. Fires: `"{CatalogType}.{member} has no [HandlesForm] handler in {ClassName}"` (error). This is distinct from PRECEPT0007, which enforces `GetMeta` switch exhaustiveness. PRECEPT0019 is Layer 2 of the coverage enforcement pattern, and adding a future catalog requires zero analyzer changes.

**Parser method annotations (shipped in Slice 4):** Add `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]` to `Parser`, then annotate the Parser's ~10 existing expression handler methods with `[HandlesForm(ExpressionFormKind.X)]`. Slices 5 and 6 will add `[HandlesForm(ExpressionFormKind.ListLiteral)]` and `[HandlesForm(ExpressionFormKind.MethodCall)]` respectively on the new methods they introduce.

**Downstream stage annotations (shipped in Slice 4):** Add `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]` to `TypeChecker`, `Evaluator`, and `GraphAnalyzer`, then annotate their expression-handling methods with `[HandlesForm(ExpressionFormKind.X)]`. All four classes must be annotated in the same slice — PRECEPT0019 fires on all of them simultaneously.

**Tests to write** (in `test/Precept.Tests/ExpressionFormCatalogTests.cs`, new file):

| Test Method | Verifies |
|-------------|----------|
| `ExpressionForms_All_HasExpectedCount` | `ExpressionForms.All.Count == 10` |
| `ExpressionForms_All_NoneNull` | No entry in `All` has a null or empty `HoverDocs` |
| `ExpressionForms_IsLeftDenotation_CorrectForLedForms` | `BinaryOperation`, `MemberAccess`, `MethodCall` have `IsLeftDenotation = true` |
| `ExpressionForms_IsLeftDenotation_CorrectForNudForms` | All remaining members have `IsLeftDenotation = false` |
| `ExpressionForms_GetMeta_AllMembersHandled` | `Enum.GetValues<ExpressionFormKind>()` — `GetMeta()` does not throw for any member |
| `PRECEPT0019_Fires_WhenFormKindHasNoHandler` | Analyzer emits PRECEPT0019 when a class marked with `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]` is missing a `[HandlesForm]` handler for an `ExpressionFormKind` member |
| `PRECEPT0019_Passes_WhenAllFormKindsAnnotated` | Analyzer passes when each class marked with `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]` covers all `ExpressionFormKind` members with `[HandlesForm]` |

**Regression anchors:** All existing catalog tests must pass unchanged. No modifications to any other catalog file.

---

### Slice 5: GAP-6 — List Literal Expressions — ✅ DONE

**Problem:** `ParseAtom()` has no `case TokenKind.LeftBracket`. The spec (§1.3, §2.1) defines `ListLiteral := '[' (Expr (',' Expr)*)? ']'`. No `ListLiteralExpression` AST node exists.

**Files to modify:**

| File | Method/Location | Change |
|------|----------------|--------|
| `src/Precept/Pipeline/Parser.cs` | `ParseAtom()` switch, before `default` | Add `case TokenKind.LeftBracket` |
| `src/Precept/Pipeline/SyntaxNodes/Expressions/` | New file `ListLiteralExpression.cs` | Add record type |

**What to add/change:**

```csharp
// New AST node:
public sealed record ListLiteralExpression(
    SourceSpan Span,
    ImmutableArray<Expression> Elements) : Expression(Span);
```

```csharp
// In ParseAtom() switch:
case TokenKind.LeftBracket:
{
    var open = Advance();
    var elements = ImmutableArray.CreateBuilder<Expression>();
    if (Current().Kind != TokenKind.RightBracket)
    {
        do
        {
            elements.Add(ParseExpression(0));
        }
        while (Match(TokenKind.Comma));
    }
    var close = Expect(TokenKind.RightBracket);
    return new ListLiteralExpression(
        SourceSpan.Covering(open.Span, close.Span), elements.ToImmutable());
}
```

**Tests to write** (in `test/Precept.Tests/ExpressionParserTests.cs`):

| Test Method | Verifies |
|-------------|----------|
| `ParseExpression_ListLiteral_Empty` | `[]` → `ListLiteralExpression` with 0 elements |
| `ParseExpression_ListLiteral_SingleElement` | `[1]` → 1 element |
| `ParseExpression_ListLiteral_MultipleElements` | `[1, 2, 3]` → 3 number literal elements |
| `ParseExpression_ListLiteral_Strings` | `["a", "b"]` → 2 string literal elements |
| `ParseExpression_ListLiteral_NestedExpressions` | `[a + 1, b * 2]` → elements are `BinaryExpression` |
| `ParseExpression_ListLiteral_InDefaultClause` | Full `field X as set of integer default [1, 2, 3]` → clean parse |

**Regression anchors:** All existing `ExpressionParserTests`, existing `ParserTests` field declaration tests.

---

### Slice 6: GAP-7 — Method Call on Member Access — ✅ DONE

**Problem:** After dot-access produces `MemberAccessExpression`, if `(` follows, the Pratt loop has no handler. The `(` is unrecognized and the loop breaks, leaving `(` unconsumed.

The spec §2.1 left-denotation: `(` (LeftParen) → If left is `MemberAccessExpression` → `MethodCallExpression`; if `IdentifierExpression` → `CallExpression`; else → diagnostic.

**Files to modify:**

| File | Method/Location | Change |
|------|----------------|--------|
| `src/Precept/Pipeline/Parser.cs` | `ParseExpression()` Pratt loop, after the `Is` handler (Slice 3) and before `OperatorPrecedence` | Add `LeftParen` handler at precedence 80 |
| `src/Precept/Pipeline/SyntaxNodes/Expressions/` | New file `MethodCallExpression.cs` | Add record type |

**What to add/change:**

```csharp
// New AST node:
public sealed record MethodCallExpression(
    SourceSpan Span,
    Expression Target,
    ImmutableArray<Expression> Arguments) : Expression(Span);
```

```csharp
// In ParseExpression() Pratt loop, after Is handler:
if (current.Kind == TokenKind.LeftParen)
{
    if (minPrecedence > 80) break;
    // Method/function call on expression
    if (left is MemberAccessExpression || left is IdentifierExpression)
    {
        Advance(); // consume '('
        var args = ImmutableArray.CreateBuilder<Expression>();
        if (Current().Kind != TokenKind.RightParen)
        {
            do
            {
                args.Add(ParseExpression(0));
            }
            while (Match(TokenKind.Comma));
        }
        var close = Expect(TokenKind.RightParen);
        
        if (left is MemberAccessExpression)
            left = new MethodCallExpression(
                SourceSpan.Covering(left.Span, close.Span), left, args.ToImmutable());
        else
            left = new CallExpression(
                SourceSpan.Covering(left.Span, close.Span),
                ((IdentifierExpression)left).Name, args.ToImmutable());
        continue;
    }
    break; // Cannot call non-member, non-identifier — let downstream handle error
}
```

**Important note:** The existing `CallExpression` handling in `ParseAtom()` (identifier followed by `(`) already works for simple function calls. The left-denotation handler covers chained calls like `obj.method(args)` and also handles identifiers that get `(` after other Pratt operations. The `ParseAtom()` path should be preserved as an optimization for the common case (avoids going through the Pratt loop for simple calls).

**Tests to write** (in `test/Precept.Tests/ExpressionParserTests.cs`):

| Test Method | Verifies |
|-------------|----------|
| `ParseExpression_MethodCall_NoArgs` | `instant.inZone()` → `MethodCallExpression` with 0 args |
| `ParseExpression_MethodCall_SingleArg` | `instant.inZone(tz)` → `MethodCallExpression` with 1 arg |
| `ParseExpression_MethodCall_MultipleArgs` | `obj.method(a, b)` → `MethodCallExpression` with 2 args |
| `ParseExpression_MethodCall_Chained` | `a.b.c()` → `MethodCallExpression` where Target is `MemberAccessExpression` |
| `ParseExpression_FunctionCall_StillWorks` | `min(a, b)` still → `CallExpression` (regression) |

**Regression anchors:** `ParseExpression_FunctionCall`, `ParseExpression_MemberAccess_Identifiers`.

---

### Slice 7: GAP-8 — Spec §2.2 `because` Optional Correction — ✅ DONE

**Problem:** Spec §2.2 grammar shows `("because" StringExpr)?` — the `?` is wrong. Parser correctly requires `because`. Design principle 9 says `because` is mandatory.

**Files to modify:**

| File | Location | Change |
|------|----------|--------|
| `docs/language/precept-language-spec.md` | §2.2 "State/event ensure" grammar (line ~810) | Remove `?` from `because` in both grammar lines |

**What to change:**

```
// Before:
(in|to|from) StateTarget ensure BoolExpr ("when" BoolExpr)? ("because" StringExpr)?
on Identifier ensure BoolExpr ("when" BoolExpr)? ("because" StringExpr)?

// After:
(in|to|from) StateTarget ensure BoolExpr ("when" BoolExpr)? because StringExpr
on Identifier ensure BoolExpr ("when" BoolExpr)? because StringExpr
```

**Tests:** None needed — this is a documentation fix. Parser behavior is already correct.

---

### Slice 8: Test Coverage — Comparison Operators — ✅ DONE

**Problem:** Only `>` (GreaterThan) is tested in `ExpressionParserTests`. All other comparison operators (`<`, `<=`, `>=`, `==`, `!=`) have zero expression-parser tests.

**Files to modify:**

| File | Location | Change |
|------|----------|--------|
| `test/Precept.Tests/ExpressionParserTests.cs` | After existing `ParseExpression_BinaryComparison` | Add Theory for all comparison operators |

**Tests to write:**

| Test Method | Verifies |
|-------------|----------|
| `ParseExpression_ComparisonLessThan` | `a < b` → `BinaryExpression` with `TokenKind.LessThan` |
| `ParseExpression_ComparisonLessThanOrEqual` | `a <= b` → `TokenKind.LessThanOrEqual` |
| `ParseExpression_ComparisonGreaterThanOrEqual` | `a >= b` → `TokenKind.GreaterThanOrEqual` |
| `ParseExpression_ComparisonEquals` | `a == b` → `TokenKind.DoubleEquals` |
| `ParseExpression_ComparisonNotEquals` | `a != b` → `TokenKind.NotEquals` |
| `ParseExpression_ComparisonNonAssociative_EmitsDiagnostic` | `a < b < c` → emits `NonAssociativeComparison` diagnostic |

---

### Slice 9: Test Coverage — `contains` Operator — ✅ DONE

**Problem:** `contains` is in `Operators.All` with `Arity.Binary`, precedence 40. It flows through the standard Pratt binary path. Zero expression-parser tests exist.

**Tests to write** (in `test/Precept.Tests/ExpressionParserTests.cs`):

| Test Method | Verifies |
|-------------|----------|
| `ParseExpression_Contains` | `tags contains "urgent"` → `BinaryExpression` with `TokenKind.Contains` |
| `ParseExpression_Contains_Precedence` | `tags contains "a" and x > 0` → top is `And`, left is `BinaryExpression(Contains)` |

---

### Slice 10: Test Coverage — Collection Mutation Actions — ✅ DONE

**Problem:** No parser-level tests for `remove`, `enqueue`, `dequeue into`, `push`, `pop into`, `clear` action statements. Only `set` and `add` are tested.

**Tests to write** (in `test/Precept.Tests/ParserTests.cs`):

| Test Method | Verifies |
|-------------|----------|
| `Parse_ActionRemove` | `from Draft on Remove -> remove items "x" -> no transition` → `RemoveStatement` |
| `Parse_ActionEnqueue` | `-> enqueue queue "item"` → `EnqueueStatement` |
| `Parse_ActionDequeue` | `-> dequeue queue` → `DequeueStatement` with null `IntoField` |
| `Parse_ActionDequeueInto` | `-> dequeue queue into target` → `DequeueStatement` with `IntoField` |
| `Parse_ActionPush` | `-> push stack "item"` → `PushStatement` |
| `Parse_ActionPop` | `-> pop stack` → `PopStatement` with null `IntoField` |
| `Parse_ActionPopInto` | `-> pop stack into target` → `PopStatement` with `IntoField` |
| `Parse_ActionClear` | `-> clear items` → `ClearStatement` |

---

### Slice 11: Test Coverage — Interpolated Strings in Expression Context — ✅ DONE

**Problem:** `ParseInterpolatedString()` is implemented but has zero expression-parser-level tests. Integration tests may cover it indirectly but the expression parser path is not explicitly verified.

**Tests to write** (in `test/Precept.Tests/ExpressionParserTests.cs`):

| Test Method | Verifies |
|-------------|----------|
| `ParseExpression_InterpolatedString_SingleHole` | `"Hello {name}"` → `InterpolatedStringExpression` with 3 parts (Text, Expr, Text) |
| `ParseExpression_InterpolatedString_MultipleHoles` | `"{a} and {b}"` → 5 parts |
| `ParseExpression_InterpolatedString_ExpressionInHole` | `"Total: {a + b}"` → hole contains `BinaryExpression` |

---

### Slice 12: Test Coverage — Sample File Integration Tests — ✅ DONE

**Problem:** 23 of 28 sample files are never loaded by any test. Adding sample file parse tests ensures real-world syntax combinations are validated.

**Files to modify:**

| File | Location | Change |
|------|----------|--------|
| `test/Precept.Tests/` | New file `SampleFileParserTests.cs` | Integration tests loading each sample file |

**Design:** Use `[Theory]` with `[MemberData]` that enumerates `samples/*.precept` files. For files affected by GAP-2/GAP-3, mark expected diagnostics until those slices land. After all slices complete, all 28 samples should parse with 0 diagnostics.

**Tests to write:**

| Test Method | Verifies |
|-------------|----------|
| `Parse_SampleFile_NoDiagnostics(string filePath)` | Theory over all 28 samples — `Lexer.Lex()` + `Parser.Parse()` → diagnostics count is 0 |

**Note:** This slice depends on Slices 1–6 (GAP fixes and ExpressionForms catalog). Without them, 13 files will emit diagnostics. Write the test with the expectation that all prior slices are complete.

---

### Slice 13: ExpressionForm Coverage Assertion (Layer 2 — Test-Time) — ✅ DONE

**Depends on:** Slice 4 (ExpressionForms catalog), Slice 5 (GAP-6 list literals), Slice 6 (GAP-7 method calls)

**Why:** With Slices 5 and 6 landed, all expression forms the catalog declares are handled by the parser. This slice adds the xUnit test that verifies that invariant — so any future gap is caught immediately at test time. PRECEPT0007 (Layer 1, Slice 4) enforces exhaustiveness at compile time; this test (Layer 2) verifies the parser actually routes those token kinds.

**Files to create:**

| File | Location | Change |
|------|----------|--------|
| `test/Precept.Tests/Language/ExpressionFormCoverageTests.cs` | New file | Coverage assertion test class |

**Implementation:**

New test class `ExpressionFormCoverageTests` in `test/Precept.Tests/Language/`:

| Test Method | Verifies |
|-------------|----------|
| `AllExpressionFormKinds_HaveParserHandler` | Iterates `ExpressionForms.All`; for each form, reads `meta.LeadTokens`; for nud forms (`!meta.IsLeftDenotation`): asserts `ParseAtom()` handles those token kinds; for led forms (`meta.IsLeftDenotation`): asserts the Pratt led loop handles those token kinds |

**Note:** PRECEPT0007 (Layer 1, compile-time) fires at build time if a new `ExpressionFormKind` member is added without a `GetMeta` arm. This test (Layer 2, test-time) verifies the parser actually handles those tokens — real enforcement that adapts to any parser refactoring.

**Regression anchor:** Test count increases by at least 1. All prior tests remain green.

---

| File | Slice | Change Type |
|------|-------|-------------|
| `src/Precept/Language/ExpressionForms.cs` | 4 | **Create** — new 13th catalog |
| `src/Precept/HandlesCatalogExhaustivelyAttribute.cs` | 4 | Modify — existing stackable class-level catalog coverage marker |
| `src/Precept/Language/HandlesFormAttribute.cs` | 4 | Modify — existing `[HandlesForm]` annotation attribute |
| `src/Precept.Analyzers/CatalogAnalysisHelpers.cs` | 4 | Modify — add `"ExpressionFormKind"` to `CatalogEnumNames` |
| `src/Precept.Analyzers/Precept0019PipelineCoverageExhaustiveness.cs` | 4 | Modify — existing fully generic PRECEPT0019 catalog coverage analyzer |
| `src/Precept/Pipeline/Parser.cs` | 1,2,3,4,5,6 | Modify — add atom cases, left-denotation handlers, ensure guard parsing, `[HandlesForm]` annotations |
| `src/Precept/Pipeline/TypeChecker.cs` | 4 | Modify — add `[HandlesForm]` annotations |
| `src/Precept/Pipeline/Evaluator.cs` | 4 | Modify — add `[HandlesForm]` annotations |
| `src/Precept/Pipeline/GraphAnalyzer.cs` | 4 | Modify — add `[HandlesForm]` annotations |
| `src/Precept/Pipeline/SyntaxNodes/Expressions/TypedConstantExpression.cs` | 1 | **Create** |
| `src/Precept/Pipeline/SyntaxNodes/Expressions/InterpolatedTypedConstantExpression.cs` | 1 | **Create** |
| `src/Precept/Language/Operators.cs` | 3 | Modify — add `Arity.Postfix = 3`, `OperatorKind.IsSet = 19`, `GetMeta()` arm for `IsSet` |
| `src/Precept/Pipeline/SyntaxNodes/Expressions/IsSetExpression.cs` | 3 | **Create** |
| `src/Precept/Pipeline/SyntaxNodes/Expressions/ListLiteralExpression.cs` | 5 | **Create** |
| `src/Precept/Pipeline/SyntaxNodes/Expressions/MethodCallExpression.cs` | 6 | **Create** |
| `src/Precept/Pipeline/SyntaxNodes/StateEnsureNode.cs` | 2 | Modify — add `PostConditionGuard` parameter |
| `src/Precept/Pipeline/SyntaxNodes/EventEnsureNode.cs` | 2 | Modify — add `PostConditionGuard` parameter |
| `docs/language/precept-language-spec.md` | 7 | Modify — remove `?` from `because` |
| `tools/Precept.Mcp/Tools/LanguageTool.cs` | 4 | Modify — add `expression_forms` to `precept_language` output |
| `test/Precept.Tests/ExpressionFormCatalogTests.cs` | 4 | **Create** |
| `test/Precept.Tests/ExpressionParserTests.cs` | 1,3,5,6,8,9,11 | Modify — add tests |
| `test/Precept.Tests/ParserTests.cs` | 2,10 | Modify — add tests |
| `test/Precept.Tests/SampleFileParserTests.cs` | 12 | **Create** |
| `test/Precept.Tests/Language/ExpressionFormCoverageTests.cs` | 13 | **Create** |

---

## 5. Tooling / MCP Sync Assessment

**One MCP tooling change is required (Slice 4). All other tooling is unaffected.**

| Change | File | Slice | Reason |
|--------|------|-------|--------|
| Add `expression_forms` section to `precept_language` output | `tools/Precept.Mcp/Tools/LanguageTool.cs` | 4 | New `ExpressionForms` catalog must be surfaced to MCP consumers |

Remaining rationale (unchanged for all other tools):
- The MCP `precept_compile` tool wraps `Compiler.Compile()`, which calls through Lexer → Parser → TypeChecker. Since TypeChecker throws `NotImplementedException` on this branch, MCP tools won't surface these parser fixes regardless.
- The language server diagnostics flow through the same pipeline. Parser improvements will automatically surface in LSP diagnostics when the language server binary is rebuilt — no manual LS code changes needed.
- TextMate grammar is not affected (no new keywords being added — `is`, `set`, `[`, `]`, `'` are all already in the grammar).
- No new `DiagnosticCode` entries are needed (existing `ExpectedToken` covers error cases; `IsSetOnNonOptional` already exists for type-checker stage).

---

## 6. Ordering — Dependency Graph

```
Slice 7 (spec fix)              ─── no dependencies, do first
Slice 8 (comparison tests)      ─── no dependencies, can parallelize
Slice 9 (contains tests)        ─── no dependencies, can parallelize

Slice 1 (typed constants)       ─── no dependencies
Slice 3 (is set)                ─── no dependencies
Slice 4 (ExpressionForms catalog + annotation bridge) ─── no dependencies; prerequisite for Slices 5 and 6
                                    (HandlesFormAttribute, PRECEPT0019, and annotation pass ship in this slice)
Slice 5 (list literals)         ─── DEPENDS ON Slice 4 (ExpressionForms catalog must exist)
Slice 6 (method calls)          ─── DEPENDS ON Slice 4 (ExpressionForms catalog must exist)

Slice 2 (ensure guard)          ─── no code deps, but affects StateEnsureNode/EventEnsureNode
                                    which Slices 1/3/5/6 don't touch. No ordering conflict.

Slice 10 (action tests)         ─── no dependencies (tests existing code)
Slice 11 (interp tests)         ─── no dependencies (tests existing code)

Slice 12 (sample files)         ─── DEPENDS ON Slices 1–6 all complete
                                    (otherwise 13/28 files emit diagnostics)

Slice 13 (coverage assertion)   ─── DEPENDS ON Slices 4, 5, 6
                                    (catalog + list literals + method calls must all be landed;
                                    PRECEPT0019 from Slice 4 enforces Layer 1 compile-time coverage)
```

**Recommended execution order:**

1. Slice 7 (trivial spec fix — commit alone)
2. Slices 8, 9, 10, 11 (test-only, no code changes — can batch into single commit)
3. Slice 1 (typed constants)
4. Slice 3 (is set)
5. Slice 4 (ExpressionForms catalog — prerequisite for Slices 5 and 6)
6. Slice 5 (list literals)
7. Slice 6 (method calls)
8. Slice 2 (ensure guard — touches AST nodes, do after expression-level slices)
9. Slice 12 (sample file integration — must be last, depends on all fixes)
10. Slice 13 (coverage assertion — after Slices 4, 5, 6 are all landed)

---

## 7. Reviewer Notes

> **📋 Historical record** — This section contains the pre-implementation plan review. Phase 1 is complete. Open items are tracked in Phase 2.

> **Reviewer:** George (Runtime Dev)  
> **Date:** 2026-05-01  
> **Status:** APPROVED WITH CONCERNS

---

### Overall Verdict: APPROVED WITH CONCERNS

Plan is well-structured and spec-confirmed. Method signatures, line numbers, token IDs, and Pratt loop topology are all accurate. One critical compilation blocker that will surface the moment GAP-2's record changes compile; several coverage gaps in the test plan. Specific items below.

---

### Slice-by-Slice Notes

**Slice 1 (GAP-1 — Typed Constants)**  
✅ Accurate. Token IDs 116–119 confirmed against `TokenKind.cs`. `ParseAtom()` switch location correct. The `case TokenKind.TypedConstant: return new TypedConstantExpression(current.Span, Advance())` pattern mirrors the existing `NumberLiteral`/`StringLiteral` arms exactly — safe copy. `ParseInterpolatedTypedConstant()` logic mirrors `ParseInterpolatedString()` correctly; the loop condition change from `StringEnd` → `TypedConstantEnd` and the middle-segment check are the only deltas needed.  

_One edge: `'Hello {name}'` needs the `TextInterpolationPart` for the opening text prefix to be non-empty in `startToken.Text`. The lexer handles this (mode stack ensures `TypedConstantStart.Text` carries the content before `{`). No parser risk, but the interpolated test should assert `Parts[0]` is a non-empty `TextInterpolationPart` to pin that contract._

**Slice 2 (GAP-2 — Post-Condition Guard on Ensure)**  
⚠️ **Critical miss. See Risk #1 below.** The parse logic is correct. Signature shapes for `ParseStateEnsure()` and `ParseEventEnsure()` match exactly (lines 418 and 543 confirmed). The stash vs. post-condition guard distinction is correct. But `BuildNode` has two dead-code arms that construct these nodes directly — both will fail to compile when the record signatures change. See Risk #1.

Also: `WSI_Integration_InsuranceClaim_HasExpectedDeclarationCounts` and `WSI_Integration_LoanApplication_HasExpectedDeclarationCounts` in `ParserTests.cs` currently accept diagnostics because of GAP-2/GAP-3. After this slice lands, those tests must be updated to also assert zero diagnostics. They are not mentioned in the test plan.

**Slice 3 (GAP-3 — `is set` / `is not set`)**  
✅ Accurate. Pratt loop position confirmed (after Dot handler at line 1344, before `OperatorPrecedence.TryGetValue` at line 1347). Precedence 40 confirmed in spec §2.1 and operators catalog. `Match(TokenKind.Not)` is the right idiom for the optional negation. `Expect(TokenKind.Set)` is load-bearing — if `Set` is absent after `Is`, this emits `ExpectedToken`, which is the correct error recovery.  

_Minor: no test for `field is foo` (i.e., `Is` followed by something other than `Not` or `Set`). `Expect(TokenKind.Set)` will produce a diagnostic but the parse loop should recover. Should add a test for malformed `is` to pin recovery behavior._

**Slice 4 (GAP-6 — List Literals)**  
✅ Accurate. `LeftBracket = 109`, `RightBracket = 110` confirmed. The `do { ParseExpression(0) } while (Match(Comma))` pattern handles the non-empty case correctly. The empty-list guard (`if (Current().Kind != TokenKind.RightBracket)`) is present and correct.  

_One concern: no test for a trailing comma — `[1, 2,]` — which would currently parse the trailing `,` as a separator, then `ParseExpression(0)` would see `]` and emit `ExpectedToken("expression", "]")`. The list would contain a garbage `IdentifierExpression(empty)`. Whether this should be a distinct diagnostic or just fall through is a design question, but the behavior should be pinned with a test._

**Slice 5 (GAP-7 — Method Call)**  
⚠️ **Significant concern. See Risk #2 below.** The `MemberAccessExpression` branch is correct and needed. The `IdentifierExpression` branch is spec-literal dead code — `ParseAtom()` already eagerly consumes `identifier(args)` before the Pratt loop ever sees `(`. Including it causes no harm but should be explicitly acknowledged so future maintainers don't wonder why it's never hit in tests.  

_Error path concern: Frank's plan says `break` when `left` is neither `MemberAccessExpression` nor `IdentifierExpression`. The spec says "else → diagnostic". `break` leaves `(` unconsumed on the stream, which may cause cascading errors in the caller. Should at minimum emit a diagnostic; ideally consume the argument list with recovery so the caller sees a clean token stream. No test covers this case._

**Slice 6 (GAP-8 — Spec `because` fix)**  
✅ Accurate. Lines 810–811 of the spec confirmed: both show `("because" StringExpr)?` — the `?` is wrong. Remove it from both lines. Text-only change. No tests needed. Agree.

**Slice 7 (Test Coverage — Comparison Operators)**  
✅ Accurate. All six comparison token kinds are in `Operators.All` with `Associativity.NonAssociative, Precedence: 30`. The `NonAssociativeComparison` diagnostic is emitted by the existing Pratt loop at line 1354–1367. Tests as specified will exercise the existing code path cleanly.

**Slice 8 (Test Coverage — `contains`)**  
✅ Mostly correct, with one gap. `Contains` is `Arity.Binary, NonAssociative, Precedence 40` — it flows through `OperatorPrecedence.TryGetValue` as a `BinaryExpression`. Frank's test plan correctly verifies `BinaryExpression(Contains)`.  

_Gap: No test for `tags contains "a" contains "b"` → `NonAssociativeComparison` diagnostic. `contains` is `NonAssociative` so chaining should emit a diagnostic via the same path comparisons use. Should add this test to pin that behavior._  

_FYI (not a Frank bug): the spec left-denotation table says `ContainsExpression` (line 707) but no such type exists — it's a `BinaryExpression`. Silent spec divergence; worth flagging for a spec cleanup pass._

**Slice 9 (Test Coverage — Collection Actions)**  
✅ Acceptable. The actions are already implemented; these tests exercise existing parser behavior. The patterns (`-> dequeue queue`, `-> dequeue queue into target`, etc.) are correct.

**Slice 10 (Test Coverage — Interpolated Strings)**  
✅ The three cases are adequate for the happy path. `ParseInterpolatedString()` is already implemented; these tests would catch any future regressions.

**Slice 11 (Sample File Integration Tests)**  
⚠️ **Structural concern.** `SamplesDir` is already defined in `ParserTests.cs` (line 931) and sample-file tests already exist in that file (Slice 5.3 section). Creating a new `SampleFileParserTests.cs` duplicates the path infrastructure constant. Better to add a new `§ Sample File Coverage` section directly in `ParserTests.cs` — same file, same `SamplesDir`, no duplication. If a separate file is preferred, extract `SamplesDir` into a shared `TestConstants.cs` rather than copying the path.  

Also: `hiring-pipeline.precept` is already in `Parse_SampleFile_ParsesWithNoErrors` (line 702). `crosswalk-signal.precept` and `trafficlight.precept` are also currently passing. The `[MemberData]` approach enumerating all 28 files is correct for covering the 23 untested ones, but the plan should note which files are already covered to avoid duplicate coverage.

---

### Cross-Cutting Concerns

**Concern A: `ParseAtom()` switch position for new cases**  
Frank's plan adds `TypedConstant` and `TypedConstantStart` "after `case TokenKind.StringStart`" and `LeftBracket` "before `default`". Both positions are fine but for clarity I'd put all three adjacent to the literal cases (after `False`, before `Identifier`). No functional difference — just readability.

**Concern B: `is set` precedence vs. `contains` precedence (both 40)**  
Both sit at 40. `Is` is checked first (explicit handler), then `OperatorPrecedence` (which has `contains`). For `x contains "a" is set` — `contains` produces `BinaryExpression`, then `is` fires on that result. This is semantically nonsense but parse-legal. The type checker will catch it. No parser action needed, but worth noting that the two 40-level operators don't interact badly at parse time.

**Concern C: No TypeChecker exhaustiveness sweep needed yet**  
All new node types (`TypedConstantExpression`, `InterpolatedTypedConstantExpression`, `IsSetExpression`, `ListLiteralExpression`, `MethodCallExpression`) will be unrecognized by the TypeChecker since it throws `NotImplementedException`. Since type-checker changes are explicitly out of scope, these nodes just won't be type-checked yet. However, the TypeChecker's node-dispatch switch should be audited once per slice to verify it doesn't hard-fault on encountering the new node type (as opposed to gracefully throwing `NotImplementedException`). This isn't in Frank's plan.

---

### Risks

**Risk #1 (CRITICAL — GAP-2 compilation blocker): `BuildNode` arms for `StateEnsure` and `EventEnsure` not updated**

`BuildNode` in `Parser.cs` (lines 1511–1586) is a static exhaustive switch over all `ConstructKind` values. The `StateEnsure` arm (lines 1553–1558) constructs `StateEnsureNode` with 6 positional arguments; the `EventEnsure` arm (lines 1576–1580) constructs `EventEnsureNode` with 5. These arms are dead code — they are never called in the live parse path (live parse goes through `ParseStateEnsure()` / `ParseEventEnsure()` directly) — but they must still compile.

When GAP-2 adds `Expression? PostConditionGuard` to both records:
- `StateEnsureNode` gains a 7th parameter. The `BuildNode` arm passes 6 → **compile error**.
- `EventEnsureNode` gains a 6th parameter. The `BuildNode` arm passes 5 → **compile error**.

**Fix required:** Add `null` for `PostConditionGuard` in both `BuildNode` arms in the same commit that changes the record definitions. Exact change:

```csharp
// BuildNode StateEnsure arm — add PostConditionGuard as null:
ConstructKind.StateEnsure => new StateEnsureNode(span,
    default,
    (StateTargetNode)slots[0]!,
    null,
    (Expression)((SyntaxNode)slots[1]!),
    null,        // PostConditionGuard — always null in slot-based path (dead code)
    default!),

// BuildNode EventEnsure arm — add PostConditionGuard as null:
ConstructKind.EventEnsure => new EventEnsureNode(span,
    ((SyntaxNode)slots[0]!).AsToken(),
    null,
    (Expression)((SyntaxNode)slots[1]!),
    null,        // PostConditionGuard — always null in slot-based path (dead code)
    default!),
```

This must be added to the **File Inventory** for Slice 2: `src/Precept/Pipeline/Parser.cs` → `BuildNode` — update `StateEnsure` and `EventEnsure` arms.

**Risk #2 (Significant — GAP-7 dead code in `IdentifierExpression` branch)**

The `left is IdentifierExpression` check in the Pratt `LeftParen` handler will never fire. When `ParseAtom()` sees `Identifier` at position N and `LeftParen` at position N+1, it consumes both and returns `CallExpression` before the Pratt loop runs. By the time the Pratt loop begins, `left` is already `CallExpression` — never `IdentifierExpression` with `(` as the next token.

Including this branch is spec-literal compliant (spec line 712 says `IdentifierExpression → CallExpression`) but it is unreachable code. Options:
1. Keep it as a dead spec-compliance stub and add a comment explaining why it's unreachable.
2. Remove it and add a comment that `ParseAtom()` handles this case preemptively.

Either is fine. But the test plan should NOT include a test case for `IdentifierExpression` via the left-denotation path — it will never exercise that branch. The `ParseExpression_FunctionCall_StillWorks` regression test in the plan is correct and does exercise the `ParseAtom()` path.

**Risk #3 (Moderate — GAP-7 error path is a spec deviation)**

Frank's plan says: `break; // Cannot call non-member, non-identifier — let downstream handle error`. The spec (line 712) says "else → diagnostic". A `break` leaves `(` unconsumed, potentially causing cascade errors in the caller. This is a deliberate simplification but deviates from the spec. Recommend either:
1. Emit `ExpectedToken` diagnostic and consume through `RightParen` for recovery.
2. Or keep `break` but explicitly document it as a known spec deviation with a comment, and add a regression test that verifies the behavior (diagnostic + recovery).

**Risk #4 (Minor — existing GAP-2-related tests need retrofit)**

After GAP-2 lands, two existing tests that currently ACCEPT diagnostics must be updated:
- `WSI_Integration_InsuranceClaim_HasExpectedDeclarationCounts` (ParserTests.cs line ~1391): currently passes because it doesn't check `Diagnostics`. After the fix, `insurance-claim.precept` should parse cleanly — add `tree.Diagnostics.Should().BeEmpty()`.
- `WSI_Integration_LoanApplication_HasExpectedDeclarationCounts` (line ~1412): same pattern. The comment at line 1415 explicitly notes this test currently accepts the `when` diagnostic.

These retrofits are regression anchors for GAP-2 — they prove the old broken behavior is gone.

---

### Suggested Changes to the Plan

1. **Slice 2 — Add to File Inventory:** `src/Precept/Pipeline/Parser.cs` → `BuildNode` — update `StateEnsure` and `EventEnsure` arms (add `null` for `PostConditionGuard`). Add to the "What to add/change" section.

2. **Slice 2 — Add to Tests:** Retrofit `WSI_Integration_InsuranceClaim_HasExpectedDeclarationCounts` and `WSI_Integration_LoanApplication_HasExpectedDeclarationCounts` to assert zero diagnostics after the fix.

3. **Slice 5 — Acknowledge dead code:** Add a comment in the plan noting that `left is IdentifierExpression` is spec-literal but unreachable due to `ParseAtom()` preemption. Remove the implied expectation that this branch would be hit in tests.

4. **Slice 5 — Add error-path test OR align to spec:** Either add a test for the non-member, non-identifier `(` case with an expected diagnostic, or acknowledge the spec deviation explicitly with a comment in the implementation.

5. **Slice 8 — Add chaining test:** Add `ParseExpression_Contains_ChainedNonAssociative` — `tags contains "a" contains "b"` → `NonAssociativeComparison` diagnostic.

6. **Slice 11 — Extend `ParserTests.cs` instead of new file:** Move sample-file tests into `ParserTests.cs` under a new section header, reusing the existing `SamplesDir` constant. If a separate file is strongly preferred, extract `SamplesDir` into `TestConstants.cs`.

---

_Review complete. Risk #1 is a compilation blocker — the `BuildNode` arms must be in the same commit as the record changes. Everything else is correctness or coverage hardening. Implementation can proceed on all other slices in parallel while Risk #1 is confirmed._

---

## Appendix: Key Architectural Constraints

1. **No hardcoded keyword lists.** The parser derives vocabulary from catalog metadata. All tokens referenced in this plan (`Is`, `Set`, `Contains`, `TypedConstant*`, `LeftBracket`, etc.) already exist in catalogs/TokenKind. No new enum values are needed.

2. **`When` remains a boundary token.** GAP-2 does NOT require removing `When` from `StructuralBoundaryTokens`. The fix is to check for `When` AFTER `ParseExpression()` returns in the ensure-parsing methods. The expression parser correctly stops at `when`; the ensure parser then consumes it.

3. **AST nodes are value-typed records.** All new expression nodes follow the existing pattern: `sealed record Foo(...) : Expression(Span)`. No mutation, no inheritance hierarchies.

4. **`ParseSession` is a `ref struct`.** All new parsing methods must be instance methods on `ParseSession`, not static helpers.

5. **`InterpolationPart` reuse.** `InterpolatedTypedConstantExpression` reuses the existing `InterpolationPart` / `TextInterpolationPart` / `ExpressionInterpolationPart` types — they are delimiter-agnostic.

---

## Catalog Compliance Audit — Revised

> **Auditor:** Frank (Lead/Architect)  
> **Original date:** 2026-05-01  
> **Revised:** 2026-05-01 — in response to Shane's direct challenge on catalog compliance  
> **Scope:** All 11 slices re-examined against `docs/language/catalog-system.md` § Architectural Identity, § Completeness Principle, and the 12 catalog definitions in `src/Precept/Language/`.

### Revision Context

Shane challenged my original verdict ("implementation may proceed — no catalog changes needed") and asked me to defend it with rigor or revise it. He specifically questioned whether GAP-6, GAP-7, GAP-3, and GAP-1 require catalog additions, not just parser changes.

After re-examining the actual catalog source files, the catalog-system.md decision framework, and the parser code, **I was partially wrong.** One gap (GAP-3) requires a catalog addition before implementation. The others are genuinely compliant. Here's the rigorous case for each.

### The Architectural Boundary — Stated Precisely

The catalog-system.md § Completeness Principle says: *"If something is part of the Precept language, it gets cataloged."* The decision framework asks: *"Is it language surface? Does it appear in `.precept` files, carry semantics that consumers need, or represent a concept that would appear in a complete description of the Precept language?"*

The twelve catalogs cover **vocabulary and semantics**: what tokens exist, what types exist, what operators exist with what precedences, what operations are legal, what constructs the grammar supports, etc.

What catalogs do NOT cover: **expression-level AST node shapes.** No expression node in the current system has a catalog entry:

| Expression node | Catalog entry | Why not |
|---|---|---|
| `LiteralExpression` | None | Parser output — grammar shape, not vocabulary |
| `BinaryExpression` | None | The *operators* are cataloged (`Operators`), the *node* is grammar output |
| `UnaryExpression` | None | Same — `Not` and `Negate` are in `Operators`, but the node isn't |
| `CallExpression` | None | Functions are cataloged (`Functions`), the call *form* is grammar shape |
| `MemberAccessExpression` | None | Grammar shape — dot-access is structural, not a vocabulary entry |
| `ParenthesizedExpression` | None | Grammar shape — grouping syntax |
| `ConditionalExpression` | None | Grammar shape — `if/then/else` expression form |
| `InterpolatedStringExpression` | None | Grammar shape — interpolation mechanics |

The `Constructs` catalog covers **declaration-level grammar forms** (field, state, event, rule, transition, ensure, access mode, etc.) — things that structure a `.precept` file at the top level. Expression nodes live one layer below, inside the expressions that declarations contain.

This means: **adding a new expression AST node type does not inherently require a catalog entry.** The node is grammar output. What DOES require a catalog entry is the *vocabulary* the node uses — tokens, operators, types.

However: **if a new language feature is an operator (something with defined precedence, arity, and semantics that consumers like MCP vocabulary, LS hover, and AI grounding need to know about), it must be in the Operators catalog.** The Completeness Principle demands it. An operator's *existence* is vocabulary, even if its *parsing* is structural.

This is the line the original audit failed to draw clearly.

---

### GAP-1: Typed Constants (`'USD'`)

**Verdict: ✅ Catalog-compliant — no changes needed**

**Tokens:** `TypedConstant` (116), `TypedConstantStart` (117), `TypedConstantMiddle` (118), `TypedConstantEnd` (119) — all exist in `TokenKind` with full `Tokens.GetMeta()` entries. Categories, descriptions, TextMate scopes all populated.

**Diagnostics:** `UnterminatedTypedConstant` (3), `UnrecognizedTypedConstantEscape` (7), `UnresolvedTypedConstant` (52), `InvalidTypedConstantContent` (53) — all cataloged.

**Should TypedConstants have their own catalog?** No. TypedConstants are not a separate vocabulary axis — they are a literal form. The *tokens* are cataloged (the lexer vocabulary for `'...'` delimiters). The *AST nodes* (`TypedConstantExpression`, `InterpolatedTypedConstantExpression`) are parser output, same as `LiteralExpression` and `InterpolatedStringExpression`. The Types catalog covers the type system; the Tokens catalog covers the lexical vocabulary; there is nothing in between that TypedConstants occupy that isn't already covered.

**Is adding `TokenKind.TypedConstant` branches in `ParseAtom()` encoding domain knowledge?** No. The parser already handles `StringLiteral`, `StringStart`, `NumberLiteral`, `True`, `False` as atom cases. These are structural grammar — the parser recognizes literal token kinds and wraps them in AST nodes. The token kinds themselves are cataloged vocabulary; the switch cases are grammar shape. Adding `TypedConstant`/`TypedConstantStart` follows the identical pattern.

---

### GAP-2: Post-Condition Guard on Ensure

**Verdict: ✅ Catalog-compliant — no changes needed**

Unchanged from original audit. The fix extends an existing cataloged construct (`StateEnsure`, `EventEnsure`) with a structural grammar refinement (checking for `when` after condition parsing). All tokens (`When`, `Ensure`, `Because`) are already cataloged. No new vocabulary.

---

### GAP-3: `is set` / `is not set`

**Verdict: ❌ NOT catalog-compliant — catalog addition required**

**I was wrong.** The original audit called this "partially compliant" and recommended a spec-reference comment as sufficient. After Shane's challenge, I re-examined the Completeness Principle and the consumer table, and the original verdict was incorrect.

**The problem:** `is set` / `is not set` is an **operator** in the Precept language. It has:
- Defined precedence (40) in the language spec §2.1
- Defined arity (postfix)
- Defined semantics (presence test on optional fields)
- A result type (boolean)

The Completeness Principle asks: *"If I enumerated every catalog's `All` property, would I have a complete description of Precept?"* Right now, enumerating `Operators.All` produces 18 operators. `is set` is not among them. An AI grounding on Precept from MCP `precept_language` output would see every binary and unary operator but would NOT see `is set`. That's a completeness failure.

The `TokenKind.Is` token IS in the Tokens catalog with description *"Multi-token operator prefix (is set, is not set)"* — so the token vocabulary is covered. But the **operator-level metadata** (precedence, arity, family, hover description) is missing from the Operators catalog. The Tokens catalog tells consumers "the keyword `is` exists." The Operators catalog should tell consumers "`is set` is a postfix presence-test operator at precedence 40."

**Why the original audit was wrong:** I conflated two separate questions:
1. "Does the parser need to read from the catalog?" — No, the parser can hardcode precedence for structural grammar handlers (it already does for `Not` at 25 and `Negate` at 65 in `ParseAtom()`).
2. "Does the operator need to exist in the catalog?" — **Yes**, because the catalog is the language specification in machine-readable form, and `is set` is part of the language.

The fact that the parser doesn't *read* the catalog entry for a given operator doesn't mean the entry shouldn't *exist*. `Not` and `Negate` both exist in `Operators.GetMeta()` even though the parser hardcodes their precedence in `ParseAtom()`. The catalog serves MCP vocabulary, LS hover, AI grounding, and documentation generation — not just the parser.

**What needs to change:**

1. **Add `Arity.Postfix` to the `Arity` enum** (currently `Unary = 1, Binary = 2`; add `Postfix = 3`). This is a one-line addition.

2. **Add `OperatorKind.IsSet` to the enum** (value 19). One new member.

3. **Add the `Operators.GetMeta()` entry:**
```csharp
OperatorKind.IsSet => new(
    kind, Tokens.GetMeta(TokenKind.Is),
    "Presence test (is set / is not set)",
    Arity.Postfix, Associativity.NonAssociative, Precedence: 40,
    OperatorFamily.Membership, IsKeywordOperator: true,
    HoverDescription: "Tests whether an optional field has a value. 'field is set' returns true if the field is non-null. 'field is not set' returns true if the field is null."),
```

4. **The parser's hardcoded `40` becomes consistent with existing practice:** `Not` (25) and `Negate` (65) are both in the Operators catalog, and the parser hardcodes their precedence in `ParseAtom()`. The catalog entry exists for consumers; the parser uses the structural handler for the grammar shape. Same pattern applies to `IsSet`.

**What does NOT need to change:**
- `OperatorMeta` does NOT need a `MultiTokenSequence` field. The `Token` field points to `TokenKind.Is` (the leading token), same as `Not` points to `TokenKind.Not`. The multi-token parsing (`is [not] set`) is grammar shape, handled by the parser's left-denotation handler.
- The `Operations` catalog does NOT need `IsSet` entries. `is set` is a structural presence test on optional fields, not a typed operator combination like `IntegerPlusDecimal`. The type checker verifies optionality, not type-specific operation legality.
- The `OperatorPrecedence` frozen dictionary (which feeds the Pratt loop's binary operator path) does NOT need to include `IsSet`, because `IsSet` is postfix, not binary. The parser handles it in a dedicated left-denotation handler, same as `Dot`.

**Impact on Slice 3:** The catalog addition (steps 1–3 above) should be the FIRST commit in Slice 3, before the parser changes. This is a small, focused change — one enum value, one `Arity` member, one `GetMeta()` arm. It does not change the parser implementation; it ensures the operator exists in the catalog for downstream consumers.

---

### GAP-6: List Literals (`[1, 2, 3]`)

**Verdict: ✅ Catalog-compliant — no changes needed**

Shane asked: *"Should `ListLiteralExpression` have a corresponding catalog entry — in `Constructs`, `Types`, or somewhere else?"*

No. Here's the rigorous case:

**Not in `Constructs`:** The Constructs catalog covers declaration-level grammar forms. Every `ConstructKind` member is a top-level or scope-anchored declaration: `field`, `state`, `event`, `rule`, `from...on`, `in...ensure`, etc. A list literal `[1, 2, 3]` is not a declaration — it's an expression that appears *inside* declarations (specifically inside `default` clauses). Adding it to `Constructs` would violate the catalog's architectural boundary. No existing expression form is in `Constructs`.

**Not in `Types`:** `TypeKind.Set`, `TypeKind.Queue`, `TypeKind.Stack` are collection *types* — they describe the type system's taxonomy. A list literal is a syntactic form for writing collection values, not a type. The type checker will infer the element type from context and validate it against the field's declared type. The literal's existence is grammar, not a type declaration.

**Not a new catalog:** There is no "expression forms" catalog, and creating one would not serve the architecture. Expression nodes are parser output consumed by the type checker, evaluator, and graph analyzer. These consumers work from AST node types (`ListLiteralExpression`, `BinaryExpression`, etc.) via pattern matching — they don't need a catalog to know what expression forms exist. The catalog system's value is propagation to external consumers (MCP, LS, TextMate, AI grounding). Expression AST nodes are internal pipeline types, not external vocabulary.

**Is adding a `LeftBracket` branch in `ParseAtom()` encoding domain knowledge?** No. The `LeftBracket` case follows the same structural pattern as `LeftParen` → `ParenthesizedExpression`: recognize a delimiter token, parse contents, consume the closing delimiter, produce an AST node. The domain knowledge here — that `[` begins a list literal — is a grammar shape decision, not a vocabulary decision. The tokens (`LeftBracket`, `RightBracket`, `Comma`) are all in the Tokens catalog. The parser assembles them into grammar; the catalog defines them as vocabulary.

**What would a "fully catalog-driven" implementation look like?** It would require a new catalog axis: an "expression forms" catalog with entries like `ListLiteral`, `ParenthesizedGroup`, `Conditional`, `InterpolatedString`, etc. The parser would read this catalog to know what `ParseAtom()` cases to handle. This would be architectural overreach — expression grammar is inherently structural. You can't usefully data-drive the difference between `[elements]` and `(inner)` and `if/then/else` because each has unique parsing mechanics. The catalog would degenerate into a registry of parse methods, not reusable metadata.

---

### GAP-7: Method Calls (`obj.method(args)`)

**Verdict: ✅ Catalog-compliant — no changes needed**

Shane asked: *"Should method call syntax be in the `Constructs` catalog? Should the Pratt parser's left-denotation for `LeftParen` be data-driven?"*

**Not in `Constructs`:** Same reasoning as GAP-6. Method calls are expression-level grammar, not declaration-level grammar. `Constructs` covers declarations.

**Should the `LeftParen` left-denotation be data-driven?** No. The left-denotation handler for `LeftParen` is structurally identical to the existing `Dot` handler — both are bespoke Pratt loop handlers for high-precedence expression grammar. The `Dot` handler (line 1336–1343) hardcodes `if (minPrecedence > 80) break;` and manually constructs a `MemberAccessExpression`. It has never been in any catalog, and correctly so — member access is structural grammar, not an operator.

Method call is the same: it's syntactic structure for applying arguments to a callable target. The tokens (`LeftParen`, `RightParen`, `Comma`) are cataloged vocabulary. The parsing mechanics are grammar shape. There is no operator-level metadata (precedence independent of dot-access, associativity, type-specific behavior) that consumers need.

**The hardcoded `80`:** Both `Dot` and `LeftParen` left-denotation handlers share precedence 80. This is a pre-existing pattern — the `Dot` handler has hardcoded 80 since the Pratt loop was written. `80` is not operator domain knowledge in the way that `is set`'s `40` is — there is no `DotAccess` or `MethodCall` entry in the Operators catalog and there shouldn't be, because these aren't operators. They're syntactic structure for member access and invocation. The precedence value is a grammar shape decision: "dot and call bind tighter than any operator." A spec-reference comment is appropriate.

---

### Slices 6–11: Unchanged Verdicts

| Slice | Verdict | Reason |
|-------|---------|--------|
| 6 (GAP-8 spec fix) | ✅ N/A — documentation only | No parser code, no catalog interaction |
| 7 (comparison ops) | ✅ Compliant | Test-only; operators already in catalog; Pratt loop reads from `OperatorPrecedence` |
| 8 (`contains`) | ✅ Compliant | Test-only; `OperatorKind.Contains` fully cataloged |
| 9 (collection actions) | ✅ Compliant | Test-only; `Actions.ByTokenKind` dispatch is textbook catalog-driven |
| 10 (interpolated strings) | ✅ Compliant | Test-only; all tokens cataloged |
| 11 (sample integration) | ✅ Compliant | Integration tests, no catalog interaction |

---

### Revised Overall Assessment

**The plan requires one catalog addition before implementation of Slice 3. All other slices are genuinely catalog-compliant.**

| Gap | Catalog change needed? | What and why |
|-----|----------------------|--------------|
| GAP-1 (typed constants) | No | Tokens already cataloged. AST nodes are grammar output, not vocabulary. |
| GAP-2 (ensure guard) | No | All tokens cataloged. Structural extension of existing cataloged construct. |
| **GAP-3 (`is set`)** | **Yes** | `is set` is a language-surface operator. Must exist in `Operators` catalog for MCP vocabulary, LS hover, AI grounding completeness. Add `Arity.Postfix`, `OperatorKind.IsSet`, `Operators.GetMeta()` entry. |
| GAP-6 (list literals) | No | Expression node — grammar output, not declaration-level construct or vocabulary. |
| GAP-7 (method calls) | No | Expression grammar structure, same as dot-access. Not an operator. |

**Why the original audit got GAP-3 wrong:** I treated the question "does the parser need to read from it?" as equivalent to "does it need to exist?" Those are different questions. The parser handles prefix and postfix operators structurally (same as it handles `Not` and `Negate` in `ParseAtom()` with hardcoded precedence). But the catalog's job is not just to feed the parser — it's to be the language specification in machine-readable form. An operator that exists in the language but not in `Operators.All` is a completeness violation, regardless of whether the parser reads from it.

**Why GAP-6 and GAP-7 genuinely don't need catalog entries:** Because they are expression grammar shapes, not vocabulary. The *tokens* they use are cataloged. The *AST nodes* they produce are internal pipeline types. No existing expression node has a catalog entry. A "fully catalog-driven" expression parser would require a new catalog axis (expression forms) that would add no consumer value — the consumers that need to know about expression forms (type checker, evaluator, graph analyzer) already work from AST node types via pattern matching.

> **⚠️ Pre-implementation reviewer notes** — These action items are from the design-phase plan review, before Phase 1 implementation. See status annotations below each item. Items superseded by Phase 2 are tracked in the Phase 2 scope.

**Action items for implementer:**
1. **Slice 3, first commit:** Add `Arity.Postfix = 3` to the `Arity` enum. Add `OperatorKind.IsSet = 19` to the `OperatorKind` enum. Add the corresponding `Operators.GetMeta()` arm with `Arity.Postfix`, `Precedence: 40`, `OperatorFamily.Membership`, `IsKeywordOperator: true`, `Token: Tokens.GetMeta(TokenKind.Is)`. Verify `Operators.All` count increases to 19.
   > **Status: Superseded by Phase 2 — Option B full DU covers this and more.**
2. **Slice 3, parser commit:** The `40` in the parser's `is` handler is now backed by `Operators.GetMeta(OperatorKind.IsSet).Precedence`. Add a comment: `// Precedence 40 — from Operators catalog (IsSet). Structural postfix handler.`
   > **Status: Parser handler done (Slice 3 ✅). Comment may need verification — tracked in Phase 2.**
3. **Slice 5:** Add a comment on the `80` constant: `// Precedence 80 — matches dot-access. Spec §2.1, structural grammar, not an operator.`
   > **Status: MethodCallExpression handler done (Slice 6 ✅). Comment may need verification — tracked in Phase 2.**
4. **§2 Scope update:** Change "Catalog changes: none" to "Catalog changes: `OperatorKind.IsSet` addition (Slice 3)."
   > **Status: Superseded — Phase 2 status section covers this.**

---

## Phase 2 — Extended Gap Resolution

> **Phase 1 is historical record — do not edit Slices 1–13.**  
> **Authors:** Frank (Lead Architect/Language Designer) + George (Runtime Dev)  
> **Date:** 2026-05-01  
> **Baseline:** 2482 tests passing (Phase 1 exit). Known-broken sample files: 7 (GAP-A: 2, GAP-B: 4, GAP-C: 1).

---

### Phase 2 Overview

**Goal:** Resolve all remaining parser gaps and catalog incompleteness before type-checker work begins. Shane's directive: no deferrals, no holes.

> **Explicit deferral:** `LanguageTool.cs` (MCP vocabulary tool) — explicitly deferred to post-type-checker by Shane (2026-05-01). This was originally planned in Phase 1 Slice 4 and is still pending. It is out of scope for Phase 2 by owner decision.

**Structure:**

| Phase | Description | Slices |
|-------|-------------|--------|
| 2a | Parallel gap fixes — independent, no catalog changes | 14–18 |
| 2b | Operator DU — sequential, catalog restructure | 19–22 |
| 2c | PRECEPT0019 promotion — sequential, depends on 2b | 23–26 |

**Dependency graph:**

```
   ┌──────────────────────────────────────────────────────────────┐
   │                    PHASE 2a: PARALLEL                        │
   │  Slice 14 (GAP-A: when-guard on StateEnsure/EventEnsure)    │
   │  Slice 15 (GAP-B: modifiers after computed expressions)     │
   │  Slice 16 (GAP-C: keyword-as-member-name)                   │
   │  Slice 17 (G2: is set precedence audit)                     │
   │  Slice 18 (G3: contains chaining test)                      │
   └──────────────────────────────────────────────────────────────┘
                            │
                            ▼
   ┌──────────────────────────────────────────────────────────────┐
   │                    PHASE 2b: SEQUENTIAL                      │
   │  Slice 19 (A1: enum additions)                               │
   │  Slice 20 (A2/B: OperatorMeta DU + ByToken restructure)     │
   │  Slice 21 (A3: PostfixOperation + [HandlesForm])             │
   │  Slice 22 (A4: migrate consumer call sites)                  │
   └──────────────────────────────────────────────────────────────┘
                            │
                            ▼
   ┌──────────────────────────────────────────────────────────────┐
   │                    PHASE 2c: SEQUENTIAL                      │
   │  Slice 23 (C1: annotate TypeChecker)                         │
   │  Slice 24 (C2: annotate GraphAnalyzer)                       │
   │  Slice 25 (G1: ExpressionFormCoverageTests)                  │
   │  Slice 26 (C3–C5: flip PRECEPT0019 → Error)                  │
   └──────────────────────────────────────────────────────────────┘
```

**Known-broken sample file tracker:**

| File | Gap | Fixed in Slice |
|------|-----|----------------|
| `insurance-claim.precept` | GAP-A (`when`-guard) | 14 |
| `loan-application.precept` | GAP-A (`when`-guard) | 14 |
| `sum-on-rhs-rule.precept` | GAP-B (modifier after `->`) | 15 |
| `invoice-line-item.precept` | GAP-B (modifier after `->`) | 15 |
| `transitive-ordering.precept` | GAP-B (modifier after `->`) | 15 |
| `travel-reimbursement.precept` | GAP-B (modifier after `->`) | 15 |
| `building-access-badge-request.precept` | GAP-C (keyword member name) | 16 |

After Slice 16: `KnownBrokenFiles` in `SampleFileIntegrationTests.cs` must be empty — 28 clean files.

---

### Slice 14: Work Item D — GAP-A: `when`-guard on `ParseStateEnsure`/`ParseEventEnsure` ✅ DONE

**Goal:** Fix the two broken sample files that use `ensure Cond when Guard because "msg"` syntax.

**Rationale:** `ParseStateEnsure` and `ParseEventEnsure` call `Expect(TokenKind.Because)` immediately after `ParseExpression(0)`. Since `When` is in `StructuralBoundaryTokens`, the expression parser correctly stops at `when`. The next call is `Expect(Because)` — which sees `when` and emits `ExpectedBecause`. The `Guard: Expression?` field already exists on both AST nodes; only the method bodies need updating.

**Key reconciliation:** Frank's review (Finding 2) listed this as requiring `PostConditionGuard` added as a new AST field. George's source inspection found `Guard: Expression?` is ALREADY on both `StateEnsureNode` and `EventEnsureNode`. **No AST changes are needed** — the fix is method bodies only. `BuildNode` dead-code arms do not require changes either.

**Files to modify:**

| File | Method/Location | Change |
|------|----------------|--------|
| `src/Precept/Pipeline/Parser.cs` | `ParseStateEnsure` (~line 419) | Add post-condition `when`-guard check after `ParseExpression(0)` |
| `src/Precept/Pipeline/Parser.cs` | `ParseEventEnsure` (~line 544) | Same pattern |
| `test/Precept.Tests/ParserTests.cs` | StateEnsure / EventEnsure section | Add 4 tests |
| `test/Precept.Tests/SampleFileIntegrationTests.cs` | `KnownBrokenFiles` | Remove 2 files, update count assertions |
| `docs/language/precept-language-spec.md` | §2.2 ensure grammar | Document `[when Guard]` in state/event ensure grammar |

**Existing AST node shapes — no change needed:**

```csharp
// StateEnsureNode — existing shape, unchanged
public sealed record StateEnsureNode(
    SourceSpan Span, Token Preposition, StateTargetNode State,
    Expression? Guard,     // ← already present — reused for post-condition guard
    Expression Condition, Expression Message) : Declaration(Span);

// EventEnsureNode — existing shape, unchanged
public sealed record EventEnsureNode(
    SourceSpan Span, Token EventName,
    Expression? Guard,     // ← already present — reused for post-condition guard
    Expression Condition, Expression Message) : Declaration(Span);
```

**`ParseStateEnsure` — before/after:**

Before (~line 419):
```csharp
private StateEnsureNode ParseStateEnsure(SourceSpan start, Token preposition, StateTargetNode anchor, Expression? stashedGuard)
{
    Advance(); // consume 'ensure'
    var condition = ParseExpression(0);
    Expect(TokenKind.Because);
    var message = ParseExpression(0);
    return new StateEnsureNode(
        SourceSpan.Covering(start, message.Span),
        preposition, anchor, stashedGuard, condition, message);
}
```

After:
```csharp
private StateEnsureNode ParseStateEnsure(SourceSpan start, Token preposition, StateTargetNode anchor, Expression? stashedGuard)
{
    Advance(); // consume 'ensure'
    var condition = ParseExpression(0);

    // Post-condition when-guard: `ensure Cond when Guard because "msg"`
    // stashedGuard is a pre-ensure guard parsed before the 'ensure' keyword in the
    // dispatch flow. If no stashed guard exists, consume a post-condition when-guard here.
    Expression? guard = stashedGuard;
    if (guard is null && Current().Kind == TokenKind.When)
    {
        Advance(); // consume 'when'
        guard = ParseExpression(0);
    }

    Expect(TokenKind.Because);
    var message = ParseExpression(0);

    return new StateEnsureNode(
        SourceSpan.Covering(start, message.Span),
        preposition, anchor, guard, condition, message);
}
```

**`ParseEventEnsure` — before/after:**

Before (~line 544):
```csharp
private EventEnsureNode ParseEventEnsure(SourceSpan start, Token eventName, Expression? stashedGuard)
{
    Advance(); // consume 'ensure'
    var condition = ParseExpression(0);
    Expect(TokenKind.Because);
    var message = ParseExpression(0);
    return new EventEnsureNode(
        SourceSpan.Covering(start, message.Span),
        eventName, stashedGuard, condition, message);
}
```

After:
```csharp
private EventEnsureNode ParseEventEnsure(SourceSpan start, Token eventName, Expression? stashedGuard)
{
    Advance(); // consume 'ensure'
    var condition = ParseExpression(0);

    // Post-condition when-guard: `on Event ensure Cond when Guard because "msg"`
    Expression? guard = stashedGuard;
    if (guard is null && Current().Kind == TokenKind.When)
    {
        Advance(); // consume 'when'
        guard = ParseExpression(0);
    }

    Expect(TokenKind.Because);
    var message = ParseExpression(0);

    return new EventEnsureNode(
        SourceSpan.Covering(start, message.Span),
        eventName, guard, condition, message);
}
```

**Note on `BuildNode` dead-code arms:** `BuildNode` has `StateEnsure` and `EventEnsure` arms that construct nodes with `Guard: null`. Because the AST record shapes are **unchanged** (no new parameters), these dead-code arms compile cleanly without modification. No `BuildNode` changes needed.

**Spec update** (`docs/language/precept-language-spec.md` §2.2):
```
// Before:
(in|to|from) StateTarget ensure BoolExpr because StringExpr
on Identifier ensure BoolExpr because StringExpr

// After:
(in|to|from) StateTarget ensure BoolExpr ["when" BoolExpr] because StringExpr
on Identifier ensure BoolExpr ["when" BoolExpr] because StringExpr
```

**Tests to add** (in `test/Precept.Tests/ParserTests.cs`):

| Test Method | Verifies |
|-------------|----------|
| `Parse_StateEnsure_WithPostConditionWhenGuard` | Input: `in Active ensure amount > 0 when flagged because "msg"` → `StateEnsureNode.Guard = IdentifierExpression("flagged")`, `Condition = BinaryExpression(amount > 0)`, zero error-severity diagnostics |
| `Parse_StateEnsure_WithoutWhenGuard_Regression` | Input: `in Active ensure amount > 0 because "msg"` → `Guard = null`, zero diagnostics |
| `Parse_EventEnsure_WithPostConditionWhenGuard` | Input: `on Submit ensure Submit.Amount > 0 when active because "msg"` → `EventEnsureNode.Guard = IdentifierExpression("active")`, zero diagnostics |
| `Parse_EventEnsure_WithoutWhenGuard_Regression` | Existing form without `when` → `Guard = null`, zero diagnostics |

**`SampleFileIntegrationTests.cs` updates:**
- Remove `insurance-claim.precept` and `loan-application.precept` from `KnownBrokenFiles`
- Update count assertions: known broken 7 → 5, clean 21 → 23

**Acceptance criteria:**
- All 4 new tests pass
- All existing StateEnsure/EventEnsure tests still pass (no regressions)
- `insurance-claim.precept` and `loan-application.precept` absent from `KnownBrokenFiles`
- Build green, zero new diagnostics

---

### Slice 15: Work Item E — GAP-B: Modifiers after Computed (`->`) Expressions ✅ DONE

**Goal:** Fix the 4 broken sample files that use `field Name as Type -> expr modifier` syntax.

**Rationale:** The slot-based `ParseFieldDeclaration` processes `ModifierList` (slot [2]) before `ComputeExpression` (slot [3]). Any modifier tokens appearing after the `->` expression are unconsumed, causing `ExpectedDeclarationKeyword` errors. The fix is a dedicated `ParseFieldDeclaration` override that collects modifiers both before and after the `->` expression and bypasses the generic slot system.

**Broken sample syntax:**
```
field LineTotal as number -> TaxableAmount + TaxAmount nonnegative
field Net as number -> Total - Tax - Fee positive
```

**Files to modify:**

| File | Method/Location | Change |
|------|----------------|--------|
| `src/Precept/Pipeline/Parser.cs` | `ParseFieldDeclaration` | Replace slot-based dispatch with dedicated method collecting pre- and post-`->` modifiers |
| `test/Precept.Tests/ParserTests.cs` | FieldDeclaration section | Add 4 tests |
| `test/Precept.Tests/SampleFileIntegrationTests.cs` | `KnownBrokenFiles` | Remove 4 files, update count assertions |

**`ParseFieldDeclaration` — replacement:**

```csharp
private FieldDeclarationNode ParseFieldDeclaration()
{
    var start = Current().Span;
    Advance(); // consume 'field'

    // [0] Name(s)
    var nameTokens = ParseIdentifierListTokens();

    // [1] Type — 'as Type'
    Expect(TokenKind.As);
    var type = ParseTypeRef();

    // [2a] Pre-expression modifiers (e.g. optional, default N, min N, max N)
    var preModifiers = ParseFieldModifierNodes();

    // [3] Optional computed expression: '-> Expr'
    Expression? computed = null;
    ImmutableArray<FieldModifierNode> postModifiers = [];
    if (Current().Kind == TokenKind.Arrow)
    {
        Advance(); // consume '->'
        computed = ParseExpression(0);

        // [2b] Post-expression modifiers (GAP-B fix — e.g. nonnegative, positive after ->)
        postModifiers = ParseFieldModifierNodes();
    }

    var allModifiers = preModifiers.AddRange(postModifiers);

    SourceSpan lastSpan;
    if (postModifiers.Length > 0)
        lastSpan = postModifiers[^1].Span;
    else if (computed is not null)
        lastSpan = computed.Span;
    else if (preModifiers.Length > 0)
        lastSpan = preModifiers[^1].Span;
    else
        lastSpan = type.Span;

    return new FieldDeclarationNode(
        SourceSpan.Covering(start, lastSpan),
        nameTokens, type, allModifiers, computed);
}
```

**Important notes:**
- Must call `Expect(TokenKind.As)` explicitly before `ParseTypeRef()`. `ParseTypeRef()` does NOT consume `As` — the slot system's `ParseTypeExpression` does that; the dedicated method owns this step.
- `FieldDeclarationNode` has no structural change: `ImmutableArray<FieldModifierNode> Modifiers` and `Expression? ComputedExpression` are already present. Pre-expression modifiers come first in `allModifiers`, post-expression modifiers appended — matches lexical source order.
- `BuildNode` dead-code arm for `FieldDeclaration` still uses `slots[2]?.AsFieldModifiers()`. The dead path is unreachable after this change. No `BuildNode` changes needed.

**Tests to add** (in `test/Precept.Tests/ParserTests.cs`):

| Test Method | Verifies |
|-------------|----------|
| `Parse_FieldDeclaration_ModifierAfterComputedExpression` | Input: `field Net as number -> Total - Tax positive` → `ComputedExpression = BinaryExpression(...)`, `Modifiers.Single() = FlagModifierNode(positive)`, zero error-severity diagnostics |
| `Parse_FieldDeclaration_MultipleTrailingModifiers` | Input: `field X as number -> expr nonnegative writable` → `Modifiers.Length == 2`, zero diagnostics |
| `Parse_FieldDeclaration_PreAndPostModifiers` | Input: `field X as number optional -> expr positive` → `Modifiers = [optional, positive]` in order, zero diagnostics |
| `Parse_FieldDeclaration_PreModifiersOnly_Regression` | Input: `field X as number nonnegative writable` → `Modifiers = [nonnegative, writable]`, `ComputedExpression = null`, zero diagnostics |

**`SampleFileIntegrationTests.cs` updates:**
- Remove `sum-on-rhs-rule.precept`, `invoice-line-item.precept`, `transitive-ordering.precept`, `travel-reimbursement.precept` from `KnownBrokenFiles`
- Update count assertions: known broken 5 → 1, clean 23 → 27 (if Slice 14 has already landed)

**Regression anchors:** All existing `ParserTests` field declaration tests, especially `default`-clause and `min`/`max` modifier tests.

**Acceptance criteria:**
- All 4 new field declaration tests pass
- All existing field declaration tests pass (no regressions)
- All 4 GAP-B sample files removed from `KnownBrokenFiles`
- Build green

---

### Slice 16: Work Item F — GAP-C: Keyword-as-Member-Name in `MemberAccess` ✅ DONE

**Goal:** Fix the 1 broken sample file that uses `.min` and `.max` member access on collections.

**Rationale:** The `Dot` handler in `ParseExpression` calls `Expect(TokenKind.Identifier)`. `min` and `max` are tokenized as `TokenKind.Min` (55) and `TokenKind.Max` (56) — constraint keywords. `Expect(TokenKind.Identifier)` emits `ExpectedToken("identifier", "min")` because the current token is a keyword, not `Identifier`. Other collection accessors (`.count`, `.length`, `.sum`) lex as `Identifier` and work correctly — only `Min` and `Max` are affected.

**Broken sample syntax:**
```
-> set LowestRequestedFloor = RequestedFloors.min
-> set HighestRequestedFloor = RequestedFloors.max
```

**Files to modify:**

| File | Method/Location | Change |
|------|----------------|--------|
| `src/Precept/Pipeline/Parser.cs` | `ParseSession` static fields | Add `KeywordsValidAsMemberName: FrozenSet<TokenKind>` |
| `src/Precept/Pipeline/Parser.cs` | `ParseSession` instance methods | Add `ExpectIdentifierOrKeywordAsMemberName()` helper |
| `src/Precept/Pipeline/Parser.cs` | `ParseExpression()` `Dot` handler (~line 1344) | Replace `Expect(TokenKind.Identifier)` with `ExpectIdentifierOrKeywordAsMemberName()` |
| `test/Precept.Tests/ExpressionParserTests.cs` | MemberAccess section | Add 2 tests |
| `test/Precept.Tests/SampleFileIntegrationTests.cs` | `KnownBrokenFiles` + empty-broken-files sentinel tests | Remove 1 file, update count assertions to 0, convert the empty `KnownBrokenSampleFile_StillHasParserErrors` theory into a fact |

**What to add:**

```csharp
// Static field — keyword kinds valid in member-name position (e.g. after '.')
private static readonly FrozenSet<TokenKind> KeywordsValidAsMemberName =
    new[] { TokenKind.Min, TokenKind.Max }.ToFrozenSet();

/// <summary>
/// Expects an identifier or a keyword that is valid in member-name position (e.g. after '.').
/// Returns a synthetic Identifier token with the keyword's text if a valid keyword is consumed.
/// </summary>
private Token ExpectIdentifierOrKeywordAsMemberName()
{
    var cur = Current();
    if (cur.Kind == TokenKind.Identifier)
        return Advance();
    if (KeywordsValidAsMemberName.Contains(cur.Kind))
    {
        Advance();
        // Reinterpret the keyword as an identifier: preserve text and span, change Kind.
        return new Token(TokenKind.Identifier, cur.Text, cur.Span);
    }
    // Not an identifier and not a known keyword-as-member-name — emit diagnostic.
    _diagnostics.Add(Diagnostics.Create(DiagnosticCode.ExpectedToken, cur.Span, "identifier", cur.Text));
    return new Token(TokenKind.Identifier, string.Empty, cur.Span);
}
```

**`Dot` handler change (in `ParseExpression`, ~line 1344):**

```csharp
// Before:
if (current.Kind == TokenKind.Dot)
{
    if (minPrecedence > 80) break;
    Advance(); // consume '.'
    var member = Expect(TokenKind.Identifier);
    left = new MemberAccessExpression(
        SourceSpan.Covering(left.Span, member.Span), left, member);
    continue;
}

// After:
if (current.Kind == TokenKind.Dot)
{
    if (minPrecedence > 80) break;
    Advance(); // consume '.'
    var member = ExpectIdentifierOrKeywordAsMemberName();  // accepts Identifier or Min/Max
    left = new MemberAccessExpression(
        SourceSpan.Covering(left.Span, member.Span), left, member);
    continue;
}
```

**Design note:** `KeywordsValidAsMemberName` is intentionally scoped to member-access position only. This fix does NOT allow keywords as field names in declarations, event parameters, or other identifier positions. The `FrozenSet` is catalog-derived knowledge — it could be driven by a `TokenTrait.ValidAsMemberName` flag on `TokenMeta` in the future. This follows `catalog-system.md`: derive vocabulary; don't scatter hardcoded lists through parse logic.

**Tests to add** (in `test/Precept.Tests/ExpressionParserTests.cs`):

| Test Method | Verifies |
|-------------|----------|
| `Parse_MemberAccess_MinKeywordAsMemberName` | Input: `items.min` → `MemberAccessExpression` with `Object = IdentifierExpression("items")`, `Member.Kind == TokenKind.Identifier`, `Member.Text == "min"`, zero diagnostics |
| `Parse_MemberAccess_MaxKeywordAsMemberName` | Input: `items.max` → `MemberAccessExpression`, `Member.Text == "max"`, zero diagnostics |

**`SampleFileIntegrationTests.cs` updates:**
- Convert `KnownBrokenSampleFile_StillHasParserErrors` theory to a fact: after `KnownBrokenFiles` reaches zero entries, the `[Theory]` with an empty `MemberData` source produces zero test cases (xUnit warning or silent skip). Convert the theory to a `[Fact]` that asserts `KnownBrokenFiles.Count == 0`. Remove the `[MemberData]` source.
- Remove `building-access-badge-request.precept` from `KnownBrokenFiles`
- Update the count-assertion test that currently asserts the broken-file count is 7 — change the expected value to 0.
- After this slice (assuming Slices 14 and 15 also landed): `KnownBrokenFiles` is empty, count = 0, clean file count = 28

**Regression anchors:** All existing `ExpressionParserTests` member access tests.

**Acceptance criteria:**
- Both new tests pass
- `building-access-badge-request.precept` absent from `KnownBrokenFiles`
- `KnownBrokenFiles` is empty — all 28 sample files clean
- `SampleFileIntegrationTests` fully green
- Build green

---

### Slice 17: Work Item G2 — `is set` Precedence Audit ✅ DONE

**Goal:** Resolve the discrepancy between the current implementation precedence (60) and spec §2.1 (40) for `is set`/`is not set`. Produce a definitive resolution and sync parser and spec.

**Rationale:** Frank's review (Finding 1) flagged this as a moderate risk. Current Pratt loop: `if (minPrecedence > 60) break;` (line ~1360). Spec §2.1 says precedence 40.

- At precedence **60**: `x + y is set` → `x + (y is set)` — `is set` binds tighter than `+`/`-` (50). Semantically unusual but well-typed only if `y` is optional.
- At precedence **40**: `x + y is set` → `(x + y) is set` — arithmetic result checked for presence. Also unusual.
- Real-world usage: `field is set`, `obj.field is set`, `field is set and other > 0`. In all practical cases the operand is a plain field or member-access expression, so the distinction is moot at the parser level.

**Recommendation:** **Confirm 60 as correct** to match existing parser behavior and the `GetMeta` catalog entry in Slice 19 (which uses `Precedence: 60`). Update spec §2.1 to show 60.

**Files to modify (spec-update path, recommended):**

| File | Location | Change |
|------|----------|--------|
| `docs/language/precept-language-spec.md` | §2.1 left-denotation table, `is set`/`is not set` row | Update precedence value from 40 to 60 |
| `src/Precept/Pipeline/Parser.cs` | `ParseExpression` `Is` + `LeftParen` handlers | Add spec-reference comments documenting `is set` binding power 60 and method-call binding power 90 (higher than dot-access at 80), matching the Pratt table in spec §2.1 |

**If parser-update path chosen instead (precedence 40):**

| File | Method | Change |
|------|--------|--------|
| `src/Precept/Pipeline/Parser.cs` | `ParseExpression` `Is` handler (~line 1360) | Change `if (minPrecedence > 60) break;` to `if (minPrecedence > 40) break;` |

**Tests to add (if parser changes to 40):**

| Test Method | Verifies |
|-------------|----------|
| `ParseExpression_IsSet_BindsLooserThan_Arithmetic` | `x + y is set` → top is `IsSetExpression` with operand `BinaryExpression(x + y)` |

**Acceptance criteria:**
- `docs/language/precept-language-spec.md` §2.1 `is set`/`is not set` precedence is consistent with parser implementation
- A comment in `Parser.cs` `Is` handler references the catalog: `// Precedence 60 — matches Operators.GetMeta(OperatorKind.IsSet).Precedence. Spec §2.1.`
- A comment at the `Parser.cs` `LeftParen` handler binding-power check documents method-call binding power 90 and that it binds tighter than dot-access at 80, matching the Pratt table in spec §2.1.
- Build green, no test regressions

---

### Slice 18: Work Item G3 — `contains` Chaining Non-Associativity Test ✅ DONE

**Goal:** Add the missing `contains` chaining test that was identified in George's Phase 1 review.

**Rationale:** `contains` is `Arity.Binary, Associativity.NonAssociative, Precedence: 40`. The Pratt loop emits a non-associativity diagnostic when a non-associative operator is chained. George's Phase 1 review (Slice 9 note) flagged this test as missing — Slice 9 only added `ParseExpression_Contains` and `ParseExpression_Contains_Precedence`.

**Files to modify:**

| File | Location | Change |
|------|----------|--------|
| `test/Precept.Tests/ExpressionParserTests.cs` | `contains` test section | Add 1 test |

**Test to add:**

| Test Method | Verifies |
|-------------|----------|
| `ParseExpression_Contains_ChainedNonAssociative` | Input: `tags contains "a" contains "b"` → emits `NonAssociativeComparison` (or equivalent non-associativity diagnostic); parse result is a `BinaryExpression` for the first `contains` |

**Acceptance criteria:**
- `ParseExpression_Contains_ChainedNonAssociative` passes
- No regressions on existing `contains` tests

---

### Slice 19: Work Item A1 — Enum Additions (`Arity.Postfix`, `OperatorKind.IsSet/IsNotSet`, `OperatorFamily.Presence`) ⏳ PENDING

**Goal:** Add the three new enum members required for `is set`/`is not set` catalog representation. Purely additive — no structural changes.

**Rationale:** `is set` and `is not set` are language-surface operators. They must exist in `Operators.All` for MCP vocabulary, LS hover, and AI grounding completeness (`catalog-system.md` Completeness Principle). The current implementation uses a hardcoded Pratt handler with no catalog entry — a known completeness violation from Phase 1 (Frank's Finding 3). The full `GetMeta` arms with `MultiTokenOp` constructors are added in Slice 20 once that subtype exists.

**Dependencies:** Slices 14–18 complete. This is the first Phase 2b slice.

**Files to modify:**

| File | Location | Change |
|------|----------|--------|
| `src/Precept/Language/Operator.cs` | `Arity` enum | Add `Postfix = 3` |
| `src/Precept/Language/Operator.cs` | `OperatorFamily` enum | Add `Presence = 5` |
| `src/Precept/Language/OperatorKind.cs` | End of `OperatorKind` enum | Append `IsSet = 19`, `IsNotSet = 20` |

**`Arity` enum addition:**

```csharp
// In Operator.cs — Arity enum
public enum Arity { Unary = 1, Binary = 2, Postfix = 3 }
```

**`OperatorFamily` enum addition:**

```csharp
// In Operator.cs — OperatorFamily enum
public enum OperatorFamily
{
    Arithmetic  = 1,
    Comparison  = 2,
    Logical     = 3,
    Membership  = 4,
    Presence    = 5,   // is set / is not set — presence-check operators
}
```

**`OperatorKind` additions** (append at end to preserve `All_IsInAscendingOrder` test invariant):

```csharp
    // ── Presence (postfix) ─────────────────────────────────────────
    IsSet                     = 19,
    IsNotSet                  = 20,
```

**Note:** The `GetMeta` arms for `IsSet` and `IsNotSet` are NOT added in this slice — `MultiTokenOp` does not exist yet. They are added in Slice 20. Until then, the `GetMeta` exhaustive switch will produce a build error for the two new enum members. Slices 19 and 20 must be implemented as a single atomic commit (or Slice 19 enum additions committed together with the Slice 20 DU in one pass).

**Existing tests to verify pass:**

| Test | Assertion |
|------|-----------|
| `OperatorKind_All_IsInAscendingOrder` (existing) | `IsSet = 19`, `IsNotSet = 20` maintain ascending order — passes if appended at end |
| `Operators_All_CountMatchesEnumValues` (existing) | Count grows 18 → 20 — passes once Slice 20 adds `GetMeta` arms |

**Acceptance criteria:**
- `Arity.Postfix = 3`, `OperatorKind.IsSet = 19`, `OperatorKind.IsNotSet = 20`, `OperatorFamily.Presence = 5` exist
- Build green (implement atomically with Slice 20)

---

### Slice 20: Work Item A2/B — `OperatorMeta` → DU + `ByToken`/`ByTokenSequence` Restructure ⏳ PENDING

**Goal:** Refactor `OperatorMeta` from a flat record to an abstract base + `SingleTokenOp`/`MultiTokenOp` discriminated union. Add `ByTokenSequence` lookup for multi-token operators. Update all 18 existing `GetMeta` arms and add 2 new arms for `IsSet`/`IsNotSet`.

**Rationale:** `SingleTokenOp` and `MultiTokenOp` need genuinely different metadata shapes — `Token: TokenMeta` vs. `Tokens: IReadOnlyList<TokenMeta>`. The DU makes the distinction structurally impossible to violate (`catalog-system.md` DU rule). `ByToken` cannot hold `IsSet` and `IsNotSet` without collision (both have lead token `TokenKind.Is`, arity `Arity.Postfix`). `ByTokenSequence` resolves this for multi-token operators.

**Dependencies:** Slice 19 (`Arity.Postfix`, `OperatorKind.IsSet/IsNotSet`, `OperatorFamily.Presence` exist). Implement atomically with Slice 19 (single commit).

**Files to modify:**

| File | Location | Change |
|------|----------|--------|
| `src/Precept/Language/Operator.cs` | Full file | Replace flat `OperatorMeta` with abstract base + two sealed subtypes |
| `src/Precept/Language/Operators.cs` | `GetMeta()` switch | Update all 18 existing arms to `SingleTokenOp`; add 2 new `MultiTokenOp` arms |
| `src/Precept/Language/Operators.cs` | `ByToken` static property | Add `.OfType<SingleTokenOp>()` filter |
| `src/Precept/Language/Operators.cs` | New additions | Add `_byTokenSequence` field + `BuildSequenceKey` helper + `ByTokenSequence` method |
| `src/Precept/Pipeline/Parser.cs` | ~line 38 (`OperatorPrecedence` construction) | Add `.OfType<SingleTokenOp>()` before `.Where(...)` |
| `test/Precept.Tests/OperatorsTests.cs` | 3 tests | Update for DU (see call-site migration table) |

**`Operator.cs` — full DU replacement:**

```csharp
namespace Precept.Language;

/// <summary>Unary (prefix), Binary (infix), or Postfix (suffix, no right operand).</summary>
public enum Arity { Unary = 1, Binary = 2, Postfix = 3 }

/// <summary>
/// Broad operator family — used by the grammar generator, LS semantic tokens,
/// and MCP vocabulary to assign different scopes to operator groups.
/// </summary>
public enum OperatorFamily
{
    Arithmetic  = 1,
    Comparison  = 2,
    Logical     = 3,
    Membership  = 4,
    Presence    = 5,   // is set / is not set and future presence operators
}

/// <summary>Binding direction for the Pratt parser.</summary>
public enum Associativity { Left = 1, Right = 2, NonAssociative = 3 }

/// <summary>
/// Shared metadata carried by every operator regardless of token cardinality.
/// Use <see cref="SingleTokenOp"/> or <see cref="MultiTokenOp"/> — never
/// instantiate this abstract base directly.
/// </summary>
public abstract record OperatorMeta(
    OperatorKind  Kind,
    string        Description,
    Arity         Arity,
    Associativity Associativity,
    int           Precedence,
    OperatorFamily Family,
    bool          IsKeywordOperator = false,
    string?       HoverDescription  = null,
    string?       UsageExample      = null);

/// <summary>
/// An operator expressed by exactly one token (all current binary and unary operators).
/// </summary>
public sealed record SingleTokenOp(
    OperatorKind  Kind,
    TokenMeta     Token,
    string        Description,
    Arity         Arity,
    Associativity Associativity,
    int           Precedence,
    OperatorFamily Family,
    bool          IsKeywordOperator = false,
    string?       HoverDescription  = null,
    string?       UsageExample      = null)
    : OperatorMeta(Kind, Description, Arity, Associativity, Precedence, Family,
                   IsKeywordOperator, HoverDescription, UsageExample);

/// <summary>
/// An operator expressed by a fixed sequence of two or more tokens
/// (e.g. <c>is set</c>, <c>is not set</c>).
/// </summary>
public sealed record MultiTokenOp(
    OperatorKind              Kind,
    IReadOnlyList<TokenMeta>  Tokens,
    string                    Description,
    Arity                     Arity,
    Associativity             Associativity,
    int                       Precedence,
    OperatorFamily            Family,
    bool                      IsKeywordOperator = false,
    string?                   HoverDescription  = null,
    string?                   UsageExample      = null)
    : OperatorMeta(Kind, Description, Arity, Associativity, Precedence, Family,
                   IsKeywordOperator, HoverDescription, UsageExample)
{
    /// <summary>First token in the sequence — the parser's dispatch key.</summary>
    public TokenMeta LeadToken => Tokens[0];
}
```

**`Operators.cs` — `GetMeta()` arm changes:**

All 18 existing arms change from `new(kind, token, ...)` to `new SingleTokenOp(kind, token, ...)`. Mechanical one-for-one rename, no logic changes. Representative example:

```csharp
// Before (representative — one of 18 arms):
OperatorKind.Plus => new(kind, Tokens.GetMeta(TokenKind.Plus),
    "Addition", Arity.Binary, Associativity.Left, 50, OperatorFamily.Arithmetic),

// After:
OperatorKind.Plus => new SingleTokenOp(kind, Tokens.GetMeta(TokenKind.Plus),
    "Addition", Arity.Binary, Associativity.Left, 50, OperatorFamily.Arithmetic),
```

Add two new `MultiTokenOp` arms:

```csharp
        // ── Presence (postfix) ─────────────────────────────────────────────
        OperatorKind.IsSet => new MultiTokenOp(
            kind,
            [Tokens.GetMeta(TokenKind.Is), Tokens.GetMeta(TokenKind.Set)],
            "Presence check — field is set",
            Arity.Postfix, Associativity.NonAssociative, Precedence: 60, OperatorFamily.Presence,
            IsKeywordOperator: true,
            HoverDescription: "Tests whether an optional field has been assigned a value. True if the field is not absent.",
            UsageExample: "field is set"),

        OperatorKind.IsNotSet => new MultiTokenOp(
            kind,
            [Tokens.GetMeta(TokenKind.Is), Tokens.GetMeta(TokenKind.Not), Tokens.GetMeta(TokenKind.Set)],
            "Absence check — field is not set",
            Arity.Postfix, Associativity.NonAssociative, Precedence: 60, OperatorFamily.Presence,
            IsKeywordOperator: true,
            HoverDescription: "Tests whether an optional field has not been assigned a value. True if the field is absent.",
            UsageExample: "field is not set"),
```

**`Operators.cs` — `ByToken` update:**

```csharp
// Before:
public static FrozenDictionary<(TokenKind, Arity), OperatorMeta> ByToken { get; } =
    All.ToFrozenDictionary(m => (m.Token.Kind, m.Arity));

// After:
public static FrozenDictionary<(TokenKind, Arity), OperatorMeta> ByToken { get; } =
    All.OfType<SingleTokenOp>()
       .ToFrozenDictionary(m => (m.Token.Kind, m.Arity));
```

**`Operators.cs` — add `ByTokenSequence`:**

```csharp
// Internal storage — only MultiTokenOp entries, keyed by (lead, second?, third?) token sequence
private static readonly FrozenDictionary<(TokenKind, TokenKind?, TokenKind?), OperatorMeta>
    _byTokenSequence =
        All.OfType<MultiTokenOp>()
           .ToFrozenDictionary(m => BuildSequenceKey(m.Tokens));

private static (TokenKind, TokenKind?, TokenKind?) BuildSequenceKey(IReadOnlyList<TokenMeta> tokens) =>
(
    tokens.Count > 0 ? tokens[0].Kind : throw new InvalidOperationException("Empty token sequence"),
    tokens.Count > 1 ? tokens[1].Kind : (TokenKind?)null,
    tokens.Count > 2 ? tokens[2].Kind : (TokenKind?)null
);

/// <summary>
/// Looks up a multi-token operator by its full token sequence.
/// Returns null if no operator matches the sequence.
/// For single-token operators, use <see cref="ByToken"/>.
/// </summary>
public static OperatorMeta? ByTokenSequence(params TokenKind[] tokens)
{
    var key = (
        tokens.Length > 0 ? tokens[0] : throw new ArgumentException("Empty token sequence"),
        tokens.Length > 1 ? (TokenKind?)tokens[1] : null,
        tokens.Length > 2 ? (TokenKind?)tokens[2] : null
    );
    return _byTokenSequence.GetValueOrDefault(key);
}
```

Usage by post-parse consumers (type checker, MCP tool, LS hover):
```csharp
var meta = Operators.ByTokenSequence(TokenKind.Is, TokenKind.Set);
// → returns MultiTokenOp for IsSet

var meta2 = Operators.ByTokenSequence(TokenKind.Is, TokenKind.Not, TokenKind.Set);
// → returns MultiTokenOp for IsNotSet
```

**`Parser.cs` `OperatorPrecedence` construction (~line 38):**

```csharp
// Before:
private static readonly FrozenDictionary<TokenKind, int> OperatorPrecedence =
    Operators.All.Where(op => op.Arity == Arity.Binary)
                 .ToFrozenDictionary(op => op.Token.Kind, op => op.Precedence);

// After:
private static readonly FrozenDictionary<TokenKind, int> OperatorPrecedence =
    Operators.All.OfType<SingleTokenOp>()
                 .Where(op => op.Arity == Arity.Binary)
                 .ToFrozenDictionary(op => op.Token.Kind, op => op.Precedence);
```

**Call-site migration — full list:**

| File | Line (approx) | Current code | Required change |
|------|--------------|-------------|----------------|
| `src/Precept/Pipeline/Parser.cs` | ~38 | `op.Token.Kind` in `OperatorPrecedence` construction | Add `.OfType<SingleTokenOp>()` before `.Where(...)` — see above |
| `src/Precept/Language/Operators.cs` | `ByToken` property | `All.ToFrozenDictionary(m => (m.Token.Kind, m.Arity))` | `All.OfType<SingleTokenOp>().ToFrozenDictionary(...)` |
| `test/Precept.Tests/OperatorsTests.cs` | `GetMeta_TokenTextMatchesSymbol` theory | `Operators.GetMeta(kind).Token.Text` | Cast to `SingleTokenOp`: `((SingleTokenOp)Operators.GetMeta(kind)).Token.Text`; skip `MultiTokenOp` entries via `[MemberData]` filter |
| `test/Precept.Tests/OperatorsTests.cs` | `ByToken_CountMatchesAll` | `ByToken.Count == All.Count` (18 == 18) | `ByToken.Count == All.OfType<SingleTokenOp>().Count()` (18 out of 20 total) |
| `test/Precept.Tests/OperatorsTests.cs` | `ByToken_RoundTrip_AllEntriesRetrievable` | Iterates `Operators.All`, checks `ByToken` for each | Add `.OfType<SingleTokenOp>()` guard to skip `MultiTokenOp` entries |

**Note on `Parser.cs` lines 1412/1417:** `Operators.ByToken.GetValueOrDefault((current.Kind, Arity.Binary))` accesses the `ByToken` dictionary, not `OperatorMeta.Token` directly. No change needed — the dictionary type stays `FrozenDictionary<(TokenKind, Arity), OperatorMeta>`.

**Note on PRECEPT0017 analyzer:** Not affected. It checks that the first constructor argument in `GetMeta` arms matches the switch pattern constant. `SingleTokenOp`'s first arg is `Kind` (OperatorKind); `MultiTokenOp`'s first arg is also `Kind`. Works unchanged.

**Note on `OperatorPrecedence` filter:** `Arity.Postfix` operators are excluded from `OperatorPrecedence` because `.Where(op => op.Arity == Arity.Binary)` already excludes them. Do NOT add an explicit `!= Arity.Postfix` clause.

**Tests to add:**

| Test Method | Verifies |
|-------------|----------|
| `OperatorMeta_IsSet_IsMultiTokenOp` | `Operators.GetMeta(OperatorKind.IsSet) is MultiTokenOp` |
| `OperatorMeta_IsNotSet_IsMultiTokenOp` | `Operators.GetMeta(OperatorKind.IsNotSet) is MultiTokenOp` |
| `ByTokenSequence_IsSet_Resolves` | `Operators.ByTokenSequence(TokenKind.Is, TokenKind.Set)?.Kind == OperatorKind.IsSet` |
| `ByTokenSequence_IsNotSet_Resolves` | `Operators.ByTokenSequence(TokenKind.Is, TokenKind.Not, TokenKind.Set)?.Kind == OperatorKind.IsNotSet` |
| `ByTokenSequence_Unknown_ReturnsNull` | `Operators.ByTokenSequence(TokenKind.Is, TokenKind.And)` → `null` |
| `Operators_All_CountIs20` | `Operators.All.Count == 20` (was 18) |
| `Operators_SingleTokenOp_CountIs18` | `Operators.All.OfType<SingleTokenOp>().Count() == 18` |
| `Operators_MultiTokenOp_CountIs2` | `Operators.All.OfType<MultiTokenOp>().Count() == 2` |

**Acceptance criteria:**
- `OperatorMeta` is abstract; `SingleTokenOp` and `MultiTokenOp` are sealed subtypes
- All 18 existing `GetMeta` arms construct `SingleTokenOp`
- `IsSet` and `IsNotSet` construct `MultiTokenOp` with correct token sequences
- `ByToken` contains 18 entries (`SingleTokenOp` only)
- `ByTokenSequence(Is, Set)` and `ByTokenSequence(Is, Not, Set)` resolve correctly
- All operator tests pass (including updated `ByToken_CountMatchesAll` and `GetMeta_TokenTextMatchesSymbol`)
- Build green

---

### Slice 21: Work Item A3 — `ExpressionFormKind.PostfixOperation` (11th Member) + `[HandlesForm]` ⏳ PENDING

**Goal:** Add the 11th `ExpressionFormKind` member (`PostfixOperation`) and annotate `ParseExpression`'s `is`-handler with `[HandlesForm(ExpressionFormKind.PostfixOperation)]`.

**Rationale:** The `is set`/`is not set` handler in `ParseExpression` is a left-denotation (Pratt led) handler — a postfix operation form. Without this catalog member, `ExpressionForms` is missing a form and PRECEPT0019 cannot achieve green-with-zero-warnings (it currently fires for the missing `PostfixOperation` coverage). Once this member is added and annotated, all 11 forms are covered on `Parser`.

**Dependencies:** Slice 20 (`MultiTokenOp` exists, `OperatorKind.IsSet/IsNotSet` are in catalog).

**Files to modify:**

| File | Location | Change |
|------|----------|--------|
| `src/Precept/Language/ExpressionForms.cs` | `ExpressionFormKind` enum | Add `PostfixOperation = 11` after `ListLiteral` |
| `src/Precept/Language/ExpressionForms.cs` | `ExpressionForms.GetMeta()` switch | Add arm for `PostfixOperation` |
| `src/Precept/Pipeline/Parser.cs` | `ParseExpression` method annotations | Add `[HandlesForm(ExpressionFormKind.PostfixOperation)]` |
| `test/Precept.Tests/ExpressionFormCatalogTests.cs` | `ExpressionForms_All_HasExpectedCount` | Update count assertion: 10 → 11 |

**`ExpressionFormKind` enum addition:**

```csharp
// In ExpressionForms.cs — append after ListLiteral:
PostfixOperation = 11,   // is set / is not set — led (postfix) form
```

**`ExpressionForms.GetMeta()` new arm:**

```csharp
ExpressionFormKind.PostfixOperation => new(
    kind, ExpressionCategory.Composite, true, [TokenKind.Is],
    "A postfix presence-check operation: expr is set / expr is not set."),
```

**`ParseExpression` annotation update:**

```csharp
// Before:
[HandlesForm(ExpressionFormKind.MemberAccess)]
[HandlesForm(ExpressionFormKind.BinaryOperation)]
[HandlesForm(ExpressionFormKind.MethodCall)]
internal Expression ParseExpression(int minPrecedence)

// After:
[HandlesForm(ExpressionFormKind.MemberAccess)]
[HandlesForm(ExpressionFormKind.BinaryOperation)]
[HandlesForm(ExpressionFormKind.MethodCall)]
[HandlesForm(ExpressionFormKind.PostfixOperation)]   // is set / is not set
internal Expression ParseExpression(int minPrecedence)
```

Once this annotation is added, all 11 `ExpressionFormKind` members are covered on `Parser` and PRECEPT0019 fires zero times for Parser.

**Tests to update:**

| Test | Change |
|------|--------|
| `ExpressionForms_All_HasExpectedCount` (existing in `ExpressionFormCatalogTests.cs`) | Update assertion: `ExpressionForms.All.Count == 11` (was 10) |

**Tests to add:**

| Test Method | Verifies |
|-------------|----------|
| `ExpressionForms_PostfixOperation_IsLeftDenotation` | `ExpressionForms.GetMeta(ExpressionFormKind.PostfixOperation).IsLeftDenotation == true` |
| `ExpressionForms_PostfixOperation_LeadTokenIsIs` | `GetMeta(PostfixOperation).LeadTokens.Single() == TokenKind.Is` |
| `ExpressionForms_PostfixOperation_CategoryIsComposite` | `GetMeta(PostfixOperation).Category == ExpressionCategory.Composite` |

**Acceptance criteria:**
- `ExpressionFormKind.PostfixOperation = 11` exists
- `ExpressionForms.All.Count == 11`
- `ParseExpression` has `[HandlesForm(ExpressionFormKind.PostfixOperation)]`
- PRECEPT0019 fires zero times for `Parser` — verify: `dotnet build src/Precept/ -v q` shows no PRECEPT0019
- All existing `ExpressionFormCatalogTests` pass; count assertion updated to 11

---

### Slice 22: Work Item A4 — Migrate All `OperatorMeta.Token` Consumer Call Sites ⏳ PENDING

**Goal:** Confirm all call sites that accessed `OperatorMeta.Token` (now only on `SingleTokenOp`) have been migrated. Verify the build compiles clean with zero errors and zero PRECEPT0019 warnings.

**Rationale:** After Slice 20, `OperatorMeta.Token` no longer exists — `Token` is only on `SingleTokenOp`. Any remaining `OperatorMeta`-typed variable accessing `.Token` is a compile error. Most migration work was done in Slice 20; this slice is the verification and cleanup pass.

**Dependencies:** Slices 20 and 21 complete.

**Expected call sites (from George's source inspection — all should already be fixed in Slice 20):**

| File | Line (approx) | Current code | Required change |
|------|--------------|-------------|----------------|
| `src/Precept/Pipeline/Parser.cs` | ~38 | `op.Token.Kind` in `OperatorPrecedence` | Fixed in Slice 20: `.OfType<SingleTokenOp>()` added |
| `src/Precept/Language/Operators.cs` | `ByToken` property | `m.Token.Kind` | Fixed in Slice 20 |
| `test/Precept.Tests/OperatorsTests.cs` | `GetMeta_TokenTextMatchesSymbol` | `Operators.GetMeta(kind).Token.Text` | Cast to `SingleTokenOp`; skip `MultiTokenOp` entries |
| `test/Precept.Tests/OperatorsTests.cs` | `ByToken_CountMatchesAll` | `ByToken.Count == All.Count` | Updated in Slice 20: `All.OfType<SingleTokenOp>().Count()` |
| `test/Precept.Tests/OperatorsTests.cs` | `ByToken_RoundTrip_AllEntriesRetrievable` | Iterates all, checks `ByToken` | Fixed in Slice 20: `.OfType<SingleTokenOp>()` guard |

**Migration pattern for any remaining sites:**

```csharp
// Option A — cast (when type is known to be SingleTokenOp):
((SingleTokenOp)meta).Token.X

// Option B — pattern match (when type may be either):
if (meta is SingleTokenOp singleToken)
    singleToken.Token.X
```

**Build verification:**
1. `dotnet build src/Precept/ -v q` — must show zero errors and zero warnings
2. `dotnet build test/Precept.Tests/ -v q` — must show zero errors
3. `dotnet test test/Precept.Tests/ --no-build` — all tests pass

**Acceptance criteria:**
- Build compiles with zero errors, zero warnings across all projects
- All `OperatorsTests` pass (including updated assertions)
- `Operators.ByToken.Count == 18` (SingleTokenOp only)
- `Operators.All.Count == 20`

---

### Slice 23: Work Item C1 — Annotate `TypeChecker` with `[HandlesCatalogExhaustively]` + `[HandlesForm]` ⏳ PENDING

**Goal:** Add `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]` to `TypeChecker` and `[HandlesForm]` annotations for all 11 `ExpressionFormKind` members.

**Rationale:** PRECEPT0019 is currently a Warning suppressed by `<WarningsNotAsErrors>PRECEPT0019</WarningsNotAsErrors>`. Before flipping to Error (Slice 26), all pipeline classes that declare `[HandlesCatalogExhaustively]` must have full `[HandlesForm]` coverage. `TypeChecker` currently has neither marker — adding the class marker without full coverage would immediately fire PRECEPT0019 for all 11 forms.

`TypeChecker` is currently a stub that throws `NotImplementedException`. The `[HandlesForm]` annotations point to the stub method(s) that throw — accurately signaling "this form is designated for handling here (Phase 3 implementation pending)." This is not annotation abuse; it is the correct declaration of intent.

**Dependencies:** Slice 21 (`PostfixOperation` exists — all 11 forms defined).

**Files to modify:**

| File | Location | Change |
|------|----------|--------|
| `src/Precept/Pipeline/TypeChecker.cs` | Class declaration | Add `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]` |
| `src/Precept/Pipeline/TypeChecker.cs` | Expression-handling method(s) | Add `[HandlesForm(ExpressionFormKind.X)]` for all 11 forms |

**Annotation pattern:**

```csharp
[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]
internal sealed class TypeChecker
{
    // ... existing code ...

    [HandlesForm(ExpressionFormKind.Literal)]
    [HandlesForm(ExpressionFormKind.Identifier)]
    [HandlesForm(ExpressionFormKind.Grouped)]
    [HandlesForm(ExpressionFormKind.BinaryOperation)]
    [HandlesForm(ExpressionFormKind.UnaryOperation)]
    [HandlesForm(ExpressionFormKind.MemberAccess)]
    [HandlesForm(ExpressionFormKind.Conditional)]
    [HandlesForm(ExpressionFormKind.FunctionCall)]
    [HandlesForm(ExpressionFormKind.MethodCall)]
    [HandlesForm(ExpressionFormKind.ListLiteral)]
    [HandlesForm(ExpressionFormKind.PostfixOperation)]
    private TypeResult CheckExpression(Expression expression)
    {
        throw new NotImplementedException("TypeChecker expression handling — Phase 3 implementation");
    }
}
```

**Verification:** After this slice, `dotnet build src/Precept/ -v q` should still show PRECEPT0019 as a Warning (not yet Error) with zero PRECEPT0019 instances for `TypeChecker`.

**Acceptance criteria:**
- `TypeChecker` has `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]`
- All 11 `ExpressionFormKind` members covered by `[HandlesForm]` on `TypeChecker`
- PRECEPT0019 fires zero times for `TypeChecker`
- No regressions

---

### Slice 24: Work Item C2 — Annotate `GraphAnalyzer` with `[HandlesCatalogExhaustively]` + `[HandlesForm]` ⏳ PENDING

**Goal:** Same pattern as Slice 23, applied to `GraphAnalyzer`.

**Rationale:** `GraphAnalyzer` is the other unimplemented pipeline stub. Until it has `[HandlesCatalogExhaustively]` and full `[HandlesForm]` coverage, flipping PRECEPT0019 to Error would fire for all 11 forms on `GraphAnalyzer`. Slices 23 and 24 can be committed in the same pass.

**Dependencies:** Slice 21 (`PostfixOperation` exists). Can be implemented in the same commit as Slice 23.

**Files to modify:**

| File | Location | Change |
|------|----------|--------|
| `src/Precept/Pipeline/GraphAnalyzer.cs` | Class declaration | Add `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]` |
| `src/Precept/Pipeline/GraphAnalyzer.cs` | Expression-handling method(s) | Add `[HandlesForm(ExpressionFormKind.X)]` for all 11 forms |

**Annotation pattern:**

```csharp
[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]
internal sealed class GraphAnalyzer
{
    [HandlesForm(ExpressionFormKind.Literal)]
    [HandlesForm(ExpressionFormKind.Identifier)]
    [HandlesForm(ExpressionFormKind.Grouped)]
    [HandlesForm(ExpressionFormKind.BinaryOperation)]
    [HandlesForm(ExpressionFormKind.UnaryOperation)]
    [HandlesForm(ExpressionFormKind.MemberAccess)]
    [HandlesForm(ExpressionFormKind.Conditional)]
    [HandlesForm(ExpressionFormKind.FunctionCall)]
    [HandlesForm(ExpressionFormKind.MethodCall)]
    [HandlesForm(ExpressionFormKind.ListLiteral)]
    [HandlesForm(ExpressionFormKind.PostfixOperation)]
    private GraphResult AnalyzeExpression(Expression expression)
    {
        throw new NotImplementedException("GraphAnalyzer expression handling — Phase 3 implementation");
    }
}
```

**Verification:** After Slices 23 and 24 complete, `dotnet build src/Precept/ -v q` must show zero PRECEPT0019 warnings across ALL pipeline classes. This is the pre-condition for Slice 26.

**Acceptance criteria:**
- `GraphAnalyzer` has `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]`
- All 11 `ExpressionFormKind` members covered by `[HandlesForm]` on `GraphAnalyzer`
- PRECEPT0019 fires zero times for `GraphAnalyzer`
- `dotnet build src/Precept/ -v q` — zero PRECEPT0019 warnings across all classes
- No regressions

---

### Slice 25: Work Item G1 — `ExpressionFormCoverageTests.cs` (Slice 13 Makeup) ⏳ PENDING

**Goal:** Create `test/Precept.Tests/Language/ExpressionFormCoverageTests.cs` — the Layer 2 (test-time) coverage assertion for the `ExpressionForms` catalog.

**Rationale:** Slice 13 was marked "NOT IMPLEMENTED" in Frank's Phase 1 review (Finding 5). PRECEPT0007 (Layer 1, compile-time) enforces exhaustive `GetMeta` switches. This test file (Layer 2, test-time) verifies the parser actually routes each form's lead tokens — a safety net that survives parser refactoring and cannot be bypassed by a compile-only check.

**Dependencies:** Slice 21 (`PostfixOperation` = 11th member exists). All 11 forms must be in the catalog before writing coverage tests.

**Files to create:**

| File | Change |
|------|--------|
| `test/Precept.Tests/Language/ExpressionFormCoverageTests.cs` | **Create** — new test class (create `Language/` subdirectory if it does not exist) |

**Test class:**

```csharp
namespace Precept.Tests.Language;

public class ExpressionFormCoverageTests
{
    [Fact]
    public void ExpressionForms_All_HasExpectedCount()
    {
        ExpressionForms.All.Count.Should().Be(11);
    }

    [Theory]
    [MemberData(nameof(AllFormKindData))]
    public void ExpressionForms_GetMeta_DoesNotThrow(ExpressionFormKind kind)
    {
        // Verifies PRECEPT0007 enforcement: GetMeta handles every catalog member
        var meta = ExpressionForms.GetMeta(kind);
        meta.Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(AllFormKindData))]
    public void ExpressionForms_HoverDocs_NonEmpty(ExpressionFormKind kind)
    {
        var meta = ExpressionForms.GetMeta(kind);
        meta.HoverDocs.Should().NotBeNullOrWhiteSpace(
            $"form {kind} must have non-empty HoverDocs");
    }

    [Fact]
    public void ExpressionForms_LedForms_IsLeftDenotation_IsTrue()
    {
        var ledForms = new[]
        {
            ExpressionFormKind.BinaryOperation,
            ExpressionFormKind.MemberAccess,
            ExpressionFormKind.MethodCall,
            ExpressionFormKind.PostfixOperation
        };
        foreach (var kind in ledForms)
        {
            ExpressionForms.GetMeta(kind).IsLeftDenotation.Should().BeTrue(
                $"form {kind} is a led (left-denotation) form");
        }
    }

    [Fact]
    public void ExpressionForms_NudForms_IsLeftDenotation_IsFalse()
    {
        var nudForms = new[]
        {
            ExpressionFormKind.Literal,
            ExpressionFormKind.Identifier,
            ExpressionFormKind.Grouped,
            ExpressionFormKind.UnaryOperation,
            ExpressionFormKind.Conditional,
            ExpressionFormKind.FunctionCall,
            ExpressionFormKind.ListLiteral
        };
        foreach (var kind in nudForms)
        {
            ExpressionForms.GetMeta(kind).IsLeftDenotation.Should().BeFalse(
                $"form {kind} is a nud (null-denotation) form");
        }
    }

    [Fact]
    public void AllExpressionFormKinds_DeclareLeadTokens_OrAreCompositeInfix()
    {
        // Every nud form must have at least one LeadToken declared (the parser dispatches on it).
        // Led forms (IsLeftDenotation = true) dispatch on the token that follows the left expression
        // rather than a fixed prefix — they may have empty LeadTokens only when context-determined.
        foreach (var meta in ExpressionForms.All)
        {
            if (!meta.IsLeftDenotation)
            {
                meta.LeadTokens.Should().NotBeEmpty(
                    $"nud form {meta.Kind} must declare at least one lead token for parser dispatch");
            }
        }
    }

    public static IEnumerable<object[]> AllFormKindData =>
        Enum.GetValues<ExpressionFormKind>().Select(k => new object[] { k });
}
```

**Acceptance criteria:**
- `test/Precept.Tests/Language/ExpressionFormCoverageTests.cs` exists and all tests pass
- Test count increases by at least 5 new tests
- All prior tests remain green

---

### Slice 26: Work Item C3–C5 — Flip PRECEPT0019 `Warning` → `Error`, Remove `WarningsNotAsErrors`, Verify Build ⏳ PENDING

**Goal:** Promote PRECEPT0019 from an informational warning to a build error. Remove the suppression. Verify the build is green with full enforcement active.

**Rationale:** PRECEPT0019 was intentionally left as `Warning` + `WarningsNotAsErrors` suppression in Phase 1 because `TypeChecker` and `GraphAnalyzer` lacked annotations. After Slices 23 and 24, all pipeline classes have full `[HandlesForm]` coverage. The suppression is now a semantic lie — it implies there are known acceptable uncovered forms, which is false. Remove it and make enforcement real.

**Prerequisites — must ALL be verified before applying changes:**

1. `ExpressionFormKind.PostfixOperation` exists (Slice 21) ✓
2. `[HandlesForm(ExpressionFormKind.PostfixOperation)]` on `ParseExpression` (Slice 21) ✓
3. `TypeChecker` annotated with full `[HandlesForm]` coverage (Slice 23) ✓
4. `GraphAnalyzer` annotated with full `[HandlesForm]` coverage (Slice 24) ✓
5. `ExpressionFormCoverageTests.cs` all tests passing (Slice 25) ✓
6. `dotnet build src/Precept/ -v q` — **zero** PRECEPT0019 warnings ✓

**Files to modify:**

| File | Location | Change |
|------|----------|--------|
| `src/Precept.Analyzers/Precept0019PipelineCoverageExhaustiveness.cs` | `defaultSeverity` (~line 29) | `DiagnosticSeverity.Warning` → `DiagnosticSeverity.Error` |
| `src/Precept/Precept.csproj` | `WarningsNotAsErrors` entry | Remove the 2-line comment + `WarningsNotAsErrors` element |
| `test/Precept.Tests.Analyzers/Precept0019Tests.cs` | Existing severity assertions (~lines 61 and 89) | Change the two `DiagnosticSeverity.Warning` assertions to `DiagnosticSeverity.Error`; these assertions will fail at compile or test time once the analyzer's `defaultSeverity` is flipped |

**`Precept0019PipelineCoverageExhaustiveness.cs` change:**

```csharp
// Before:
defaultSeverity: DiagnosticSeverity.Warning,

// After:
defaultSeverity: DiagnosticSeverity.Error,
```

**`Precept.csproj` change:**

```xml
<!-- Remove these two lines: -->
<!-- PRECEPT0019 fires for known unimplemented expression forms (GAP-6/GAP-7) — informational, not blocking -->
<WarningsNotAsErrors>PRECEPT0019</WarningsNotAsErrors>
```

The `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` line remains. The `WarningsNotAsErrors` exemption was the safety valve when PRECEPT0019 was a Warning. Now that it is a native Error, the exemption is moot and the comment is misleading.

**Verification sequence:**

1. Apply the two changes above.
2. `dotnet build src/Precept/ -v q` — confirm zero errors and zero warnings.
3. `dotnet test test/Precept.Tests/ --no-build` — confirm all tests pass.
4. Confirm no `PRECEPT0019` appears in build output at all.

**Acceptance criteria:**
- `DiagnosticSeverity.Error` in `Precept0019PipelineCoverageExhaustiveness.cs`
- `<WarningsNotAsErrors>PRECEPT0019</WarningsNotAsErrors>` removed from `Precept.csproj`
- `dotnet build src/Precept/` — zero errors, zero warnings
- `dotnet test` — all tests pass
- PRECEPT0019 is now a build gate: any future unannotated pipeline class or missing `[HandlesForm]` causes a build failure

---

### Phase 2 — Acceptance Gate (13 Points)

**"Done" means ALL of the following are true simultaneously:**

1. **`dotnet build` — zero errors, zero warnings** — no suppressions, no `WarningsNotAsErrors` of any kind
2. **`dotnet test` — all tests pass** — Phase 1 baseline: 2482; Phase 2 target: 2510–2530 (see Test Count Projection below)
3. **PRECEPT0019 severity = `DiagnosticSeverity.Error`** in `src/Precept.Analyzers/Precept0019PipelineCoverageExhaustiveness.cs`
4. **`<WarningsNotAsErrors>PRECEPT0019</WarningsNotAsErrors>` removed** from `src/Precept/Precept.csproj`
5. **`KnownBrokenFiles` in `SampleFileIntegrationTests.cs` is empty** — all 28 sample files parse clean with zero diagnostics
6. **`KnownBrokenSampleFile_StillHasParserErrors` test removed or adapted** — when `KnownBrokenFiles` is empty the sentinel theory is obsolete; convert to an assertion that the list is empty, or remove
7. **`OperatorKind.IsSet = 19` and `OperatorKind.IsNotSet = 20` exist** in catalog with `MultiTokenOp` metadata, correct token sequences (`[Is, Set]` and `[Is, Not, Set]`), `Arity.Postfix`, `OperatorFamily.Presence`, `Precedence: 60`
8. **`Arity.Postfix = 3` exists** in the `Arity` enum
9. **`ExpressionFormKind.PostfixOperation = 11` exists** with correct catalog metadata: `IsLeftDenotation = true`, `LeadTokens = [TokenKind.Is]`, `Category = ExpressionCategory.Composite`
10. **`ExpressionFormCoverageTests.cs` exists and all tests pass** — Layer 2 (test-time) coverage assertion is in place alongside Layer 1 (compile-time) PRECEPT0007 enforcement
11. **`TypeChecker` and `GraphAnalyzer` annotated with `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]`** and full `[HandlesForm]` coverage for all 11 forms
12. **Spec §2.1 `is set`/`is not set` precedence matches implementation** — either spec updated to 60 (recommended) or parser updated to 40, with a comment in `Parser.cs` linking the implementation binding power to the catalog entry
13. **No deferred items, no holes** — `CONTRIBUTING.md` "done" definition met; spike branch is clear for type-checker work to begin
14. **Phase 2e complete** — `TokenMeta.IsValidAsMemberName` flag added and `KeywordsValidAsMemberName` derived from catalog (Slice 29); PRECEPT0021 and PRECEPT0022 analyzers implemented, tested, and producing zero diagnostics on the current codebase (Slices 30–31); PRECEPT0023 tracked and ready to implement once Phase 2b ships (Slice 32)

---

### Phase 2 — Test Count Projection

| Checkpoint | Passing Tests | Delta | Source |
|------------|--------------|-------|--------|
| Phase 1 exit (baseline) | 2482 | +375 | Slices 1–13 |
| After Phase 2a (Slices 14–18) | ~2498 | +16 | 4 StateEnsure/EventEnsure (S14), 4 FieldDeclaration (S15), 2 MemberAccess (S16), 1 contains chaining (S18), ~5 precedence/misc (S17) |
| After Phase 2b (Slices 19–22) | ~2514 | +16 | 8 operator DU/ByToken/ByTokenSequence tests (S20), 3 ExpressionForms PostfixOp tests (S21), 3 updated + 2 new count-assertion tests (S22) |
| After Phase 2c (Slices 23–26) | ~2525–2530 | +11–16 | 6+ ExpressionFormCoverageTests (S25), misc annotation verification tests |

**Target:** Phase 2 adds **30–50 new tests**. Exact count depends on theory granularity and whether annotation-verification tests are added for Slices 23–24.

---

### Slice 27: Work Item S1 — Parser.cs Structural Split (Partial Class/Struct) ⏳ PENDING

**Goal:** Split `src/Precept/Pipeline/Parser.cs` (1757 lines) into three partial files so AI agents can read and reason about each section without loading the entire file.

**Rationale:** At 1757 lines, `Parser.cs` exceeds the comfortable context window for agent-driven work. The expression parser (Pratt loop + atoms) is the most actively edited area in Phase 2 and is currently buried on line 1382. Splitting by logical grain — dispatch core, declaration grammar, expression grammar — gives each file a single, coherent responsibility and a manageable line count (~460 / ~1040 / ~310). This is a **zero-behavior-change structural refactor**: the compiler merges partial files into one type, so all behavior, tests, and API surface are identical.

**Why `partial class/struct` and not a different mechanism:** `ParseSession` is a `ref struct`. All logic must remain as instance methods on the struct — you cannot extract it into separate classes without threading `ref ParseSession` parameters through every call chain (60+ methods). `partial struct` is the only viable split mechanism. See `.squad/decisions/inbox/frank-parser-split.md` for full rationale and structural rules.

**Dependency:** Independent of all Phase 2a–2c slices. Can be implemented in any order relative to Slices 14–26. Should be done before type-checker work begins.

---

**Files to create / modify:**

| File | Change |
|------|--------|
| `src/Precept/Pipeline/Parser.cs` | Modify — add `partial` to class and struct declarations; keep core shell (vocabulary, dispatch, BuildNode) |
| `src/Precept/Pipeline/Parser.Declarations.cs` | **Create** — all declaration-level parsers (scoped constructs, slot system, type ref, field modifiers) |
| `src/Precept/Pipeline/Parser.Expressions.cs` | **Create** — Pratt loop and atom parsers |

---

**Method inventory — `Parser.cs` (keep / primary declaration):**

*Outer `Parser` static class (stays in `Parser.cs` — not on `ParseSession`):*

| Symbol | Kind | Notes |
|--------|------|-------|
| `OperatorPrecedence` | static field | FrozenDictionary — catalog-derived |
| `TypeKeywords` | static field | FrozenSet — catalog-derived |
| `ModifierKeywords` | static field | FrozenSet — catalog-derived |
| `StateModifierKeywords` | static field | FrozenSet — catalog-derived |
| `ActionKeywords` | static field | FrozenSet — catalog-derived |
| `StructuralBoundaryTokens` | static field | FrozenSet — boundary set |
| `ExpressionBoundaryTokens` | static field | FrozenSet — derived |
| `ChoiceElementTypeKeywords` | static field | FrozenSet — catalog-derived |
| `AmbiguousQualifierPrepositions` | static field | FrozenDictionary — catalog-derived |
| `Parse(TokenStream)` | static method | Public entry point |
| `BuildNode(ConstructKind, SyntaxNode?[], SourceSpan)` | static method | Exhaustive ConstructKind switch |

*`ParseSession` primary declaration (struct definition + core machinery):*

| Method | Notes |
|--------|-------|
| `ParseSession(ImmutableArray<Token>)` | Constructor — primary declaration; keeps `[HandlesCatalogExhaustively]` attribute |
| `Current()` | Token navigation |
| `Peek(int)` | Token navigation |
| `Advance()` | Token navigation |
| `Match(TokenKind)` | Token navigation |
| `Expect(TokenKind)` | Token navigation |
| `IsAtEnd()` | Token navigation |
| `EmitDiagnostic(DiagnosticCode, SourceSpan, params object?[])` | Diagnostic emission |
| `ParseAll()` | Top-level dispatch loop |
| `ParseDirectConstruct(ConstructKind)` | Dispatch helper |
| `DisambiguateAndParse(Token)` | Dispatch helper |
| `FindDisambiguatedConstruct(TokenKind, TokenKind)` | `private static` dispatch helper |
| `EmitAmbiguityAndSync(Token)` | Dispatch helper |
| `TryParseStashedGuard()` | Dispatch helper — calls `ParseExpression(0)` across file boundary (fine) |
| `ParseStateTargetDirect()` | Called by `DisambiguateAndParse` |
| `ParseEventTargetDirect()` | Called by `DisambiguateAndParse` |
| `SyncToNextDeclaration()` | Called by `DisambiguateAndParse` and error-recovery paths |
| `IsOutcomeAhead()` | Called by `DisambiguateAndParse` dispatch path |

---

**Method inventory — `Parser.Declarations.cs` (new file — partial class + partial struct):**

*In-scoped construct parsers:*

| Method |
|--------|
| `ParseAccessMode(SourceSpan, StateTargetNode, Expression?)` |
| `ParseOmitDeclaration(SourceSpan, StateTargetNode, Expression?)` |
| `ParseStateEnsure(SourceSpan, Token, StateTargetNode, Expression?)` |

*To-scoped construct parsers:*

| Method |
|--------|
| `ParseStateAction(SourceSpan, Token, StateTargetNode, Expression?)` |

*From-scoped construct parsers:*

| Method |
|--------|
| `ParseTransitionRow(SourceSpan, StateTargetNode, Expression?)` |
| `ParseOutcomeNode()` |
| `TryParseActionStatementWithRecovery()` |

*On-scoped construct parsers:*

| Method |
|--------|
| `ParseEventEnsure(SourceSpan, Token, Expression?)` |
| `ParseEventHandler(SourceSpan, Token)` |
| `ParseEventHandlerWithGuardCheck(SourceSpan, Token, Expression?)` |

*Shared action helpers:*

| Method |
|--------|
| `ParseFieldTargetDirect()` |
| `ParseAccessModeKeywordDirect()` |
| `ParseActionStatement()` |
| `ParseAssignValueStatement(ActionMeta)` |
| `ParseCollectionValueStatement(ActionMeta)` |
| `ParseCollectionIntoStatement(ActionMeta)` |
| `ParseFieldOnlyStatement(ActionMeta)` |

*Non-disambiguated construct parsers:*

| Method |
|--------|
| `ParsePreceptHeaderDeclaration()` |
| `ParseFieldDeclaration()` |
| `ParseStateDeclaration()` |
| `ParseEventDeclaration()` |
| `ParseRuleDeclaration()` |

*Entry / list helpers:*

| Method |
|--------|
| `ParseStateEntries()` |
| `ParseIdentifierListTokens()` |
| `ParseArgumentListInner()` |

*Slot system:*

| Method |
|--------|
| `ParseConstructSlots(ConstructMeta)` |
| `InvokeSlotParser(ConstructSlotKind, bool)` |
| `ParseIdentifierList(bool)` |
| `ParseTypeExpression(bool)` |
| `ParseModifierList(bool)` |
| `ParseStateEntryList(bool)` |
| `ParseArgumentList(bool)` |
| `ParseComputeExpression(bool)` |
| `ParseGuardClause(bool)` |
| `ParseBecauseClause(bool)` |
| `ParseRuleExpression(bool)` |
| `ParseInitialMarker(bool)` |
| `ParseActionChain(bool)` |
| `ParseOutcome(bool)` |
| `ParseStateTarget(bool)` |
| `ParseEventTarget(bool)` |
| `ParseEnsureClause(bool)` |
| `ParseAccessModeKeyword(bool)` |
| `ParseFieldTarget(bool)` |

*Type reference and qualifier parsing:*

| Method |
|--------|
| `TryPeekQualifierKeyword()` |
| `ParseTypeRef()` |

*Choice helpers:*

| Method |
|--------|
| `ParseChoiceValue(Token)` |
| `ConsumeThrough(TokenKind)` |

*Field modifier parsing:*

| Method |
|--------|
| `ParseFieldModifierNodes()` |

*Utility:*

| Method |
|--------|
| `GetLastSlotSpan(SyntaxNode?[], SourceSpan)` (`private static`) |

---

**Method inventory — `Parser.Expressions.cs` (new file — partial class + partial struct):**

| Method | Notes |
|--------|-------|
| `ParseExpression(int)` | Pratt loop — carries `[HandlesForm]` attributes for MemberAccess, BinaryOperation, MethodCall, PostfixOperation |
| `ParseAtom()` | Null-denotation dispatcher — carries `[HandlesForm]` attributes for Literal, Identifier, Grouped, UnaryOperation, Conditional, FunctionCall, ListLiteral |
| `ParseInterpolatedString()` | Called by `ParseAtom` |
| `ParseInterpolatedTypedConstant()` | Called by `ParseAtom` |
| `ParseListLiteral()` | Called by `ParseAtom` |

---

**Implementation notes:**

1. **Every partial file needs the full `partial` chain.** Each of the three files must declare both levels:
   ```csharp
   public static partial class Parser
   {
       internal ref partial struct ParseSession
       {
           // methods for this file
       }
   }
   ```
   The `partial` keyword must appear on both `Parser` and `ParseSession` in every file that contributes members.

2. **Usings go in every file.** `using System.Collections.Frozen;`, `using System.Collections.Immutable;`, `using Precept.Language;`, `using Precept.Pipeline.SyntaxNodes;` must be repeated in each file.

3. **`[HandlesCatalogExhaustively]` stays on the primary declaration only.** `[Precept.HandlesCatalogExhaustively(typeof(ExpressionFormKind))]` is on `ParseSession`'s primary declaration in `Parser.cs`. Do NOT duplicate it in the other partial files. Duplicating a non-`AllowMultiple` attribute produces a compiler error or incorrect analyzer behavior.

4. **`[HandlesForm]` attributes move with their methods.** `ParseExpression` and `ParseAtom` move to `Parser.Expressions.cs`. Their `[HandlesForm(...)]` attributes move with them — no action needed beyond the cut.

5. **`BuildNode` stays outside `ParseSession` in `Parser.cs`.** It is a `static` method on the outer `Parser` class and does not move.

6. **Slice 16 `KeywordsValidAsMemberName` correction.** The Slice 16 plan describes this as a `ParseSession` static field. That is wrong — `ref struct` cannot have static fields. `KeywordsValidAsMemberName` must be a static field on the outer `Parser` class (alongside `OperatorPrecedence`, `TypeKeywords`, etc.) in `Parser.cs`. The instance method `ExpectIdentifierOrKeywordAsMemberName()` is on `ParseSession` and goes in `Parser.Expressions.cs` (its caller, the `Dot` handler in `ParseExpression`, lives there). If Slice 16 lands before Slice 27, this correction is made during Slice 27. If Slice 27 lands first, apply the correction when Slice 16 is implemented.

7. **Approximate line counts** (final will vary slightly by whitespace/comments):
   - `Parser.cs`: ~460 lines
   - `Parser.Declarations.cs`: ~1040 lines
   - `Parser.Expressions.cs`: ~310 lines

---

**Tests:**

No new tests. This is a purely structural refactor — the compiler sees one type from all three files, so no existing test can distinguish the single-file from the three-file form.

**Verification:** Run the full `Precept.Tests` suite:

```
dotnet test test/Precept.Tests/
```

All tests must pass with the **same count as the baseline before this slice**. If any test fails, the split introduced a behavior change (e.g., a method was accidentally omitted or duplicated, or a `partial` declaration was mis-formed). Fix the split until the test count is identical.

Also verify build is clean:

```
dotnet build src/Precept/
```

Zero errors, zero warnings.

---

**Acceptance criteria:**

- `Parser.cs`, `Parser.Declarations.cs`, and `Parser.Expressions.cs` all exist in `src/Precept/Pipeline/`
- All three files carry `public static partial class Parser` and `internal ref partial struct ParseSession` declarations
- `[Precept.HandlesCatalogExhaustively(typeof(ExpressionFormKind))]` appears exactly once (in `Parser.cs`)
- `dotnet build src/Precept/` — zero errors, zero warnings
- `dotnet test test/Precept.Tests/` — all tests pass, count unchanged
- Zero changes to any test file
- Zero changes to any non-Parser production file
- `git diff --stat` shows only: `Parser.cs` modified, `Parser.Declarations.cs` added, `Parser.Expressions.cs` added

**Assignee:** George

---

### Slice 28: PRECEPT0020 — Operators ByToken / OperatorPrecedence Collision ⏳ PENDING

**Goal:** Add a new Roslyn analyzer that catches duplicate `(Token.Kind, Arity)` keys in the `Operators.GetMeta` switch at compile time, before they become startup-throw failures in `Operators.ByToken` or `Parser.OperatorPrecedence`.

**Rationale:** PRECEPT0009 already enforces uniqueness of composite keys in the Operations catalog (`ByBinaryOp`, `ByUnaryOp` FrozenDictionaries). The Operators catalog has an exactly parallel problem: `Operators.ByToken` is a `FrozenDictionary<(TokenKind, Arity), OperatorMeta>` and `Parser.OperatorPrecedence` is a `FrozenDictionary<TokenKind, (int, bool)>` keyed on binary operators only. A key collision in either causes a `ArgumentException` (duplicate key) thrown inside a static field initializer — extremely hard to diagnose from the stack trace. Phase 2b is about to restructure `OperatorMeta` into a DU and will introduce new token-lookup indexes; locking down the existing `ByToken` invariant before that work is the right safety-first sequencing. See `docs/working/analyzer-recommendations.md` § 3 PRECEPT0020 for full rationale.

**Dependency:** Independent of all Phase 2b–2c slices. Can be implemented at any point after Slice 27.

---

**Files to create / modify:**

| File | Change |
|------|--------|
| `src/Precept.Analyzers/Precept0020OperatorsTokenCollision.cs` | **Create** — new `[DiagnosticAnalyzer]` implementing PRECEPT0020a and PRECEPT0020b |
| `test/Precept.Analyzers.Tests/Precept0020OperatorsTokenCollisionTests.cs` | **Create** — unit tests |

---

**Invariants enforced:**

| Sub-rule | ID | Invariant | Severity |
|----------|----|-----------|----------|
| ByToken collision | PRECEPT0020a | No two `OperatorMeta` arms may share `(Token.Kind, Arity)` composite key | Error |
| OperatorPrecedence collision | PRECEPT0020b | No two *binary* `OperatorMeta` arms may share `Token.Kind` | Error |

---

**Method inventory — `Precept0020OperatorsTokenCollision.cs`:**

| Symbol | Kind | Notes |
|--------|------|-------|
| `DiagnosticId_ByTokenCollision` | const string | `"PRECEPT0020a"` |
| `DiagnosticId_PrecedenceCollision` | const string | `"PRECEPT0020b"` |
| `ByTokenCollisionRule` | static field | `DiagnosticDescriptor`, category `"Precept.Language"`, severity `Error` |
| `PrecedenceCollisionRule` | static field | `DiagnosticDescriptor`, category `"Precept.Language"`, severity `Error` |
| `SupportedDiagnostics` | override property | `ImmutableArray.Create(ByTokenCollisionRule, PrecedenceCollisionRule)` |
| `Initialize(AnalysisContext)` | override method | `RegisterCompilationStartAction` → registers per-switch `OperationKind.SwitchExpression` action + `RegisterCompilationEndAction` |
| `Collect(OperationAnalysisContext, ConcurrentBag<OperatorArmInfo>)` | private static | Scopes to `GetMeta(OperatorKind)` via `TryGetCatalogSwitchKind`; per arm: calls `ExtractTokenKindName` + `ExtractArityName`; adds to bag |
| `CrossCheck(CompilationAnalysisContext, ConcurrentBag<OperatorArmInfo>)` | private static | Builds `byTokenKey` dict `"(tokenKind, arity)" → (armCase, location)` and `byPrecedenceKey` dict `"tokenKind" → (armCase, location)` for binary arms; reports first-conflict per key |
| `ExtractTokenKindName(IObjectCreationOperation)` | private static | Finds the `Token` constructor argument; confirms it's an `IInvocationOperation` (i.e., `Tokens.GetMeta(TokenKind.X)` call); extracts the first argument of that invocation via `ResolveEnumFieldName`; returns null if argument shape is unexpected (inline Token — already PRECEPT0022 territory) |
| `ExtractArityName(IObjectCreationOperation)` | private static | Finds the `Arity` constructor argument (by parameter name); returns `ResolveEnumFieldName` result |
| `OperatorArmInfo` | private sealed class | `ArmCase: string`, `TokenKindName: string`, `ArityName: string`, `IsBinary: bool`, `Location: Location` |
| `FindObjectCreation(IOperation)` | private static | Reuse pattern from PRECEPT0009 — `UnwrapConversions` then walk children |

**Key implementation note — `ExtractTokenKindName`:**

The `Token` parameter of `OperatorMeta(OperatorKind kind, TokenMeta token, ...)` receives an `IInvocationOperation` from `Tokens.GetMeta(TokenKind.X)`. To extract `TokenKind.X`:

```csharp
// 1. Find the 'token' constructor argument (named "Token" or second positional arg with type TokenMeta).
// 2. Unwrap conversions — result should be IInvocationOperation.
// 3. Check invocation target method name is "GetMeta" and containing type name is "Tokens".
// 4. The first argument of the invocation is IFieldReferenceOperation on TokenKind.
// 5. Return that field's Name.
```

If the unwrapped value is *not* an `IInvocationOperation` (e.g., someone passed an inline `new TokenMeta(...)`), return `null` to skip this arm — PRECEPT0022 handles the inline-Token case.

---

**Tests — `Precept0020OperatorsTokenCollisionTests.cs`:**

| Test | What it verifies |
|------|-----------------|
| `GivenOperatorsWithAllDistinctTokenArityPairs_NoDiagnostic` | Baseline — current `Operators.GetMeta` produces zero diagnostics |
| `GivenTwoUnaryAndOneBinaryOnSameToken_NoDiagnostic` | Minus/Negate pattern: `(Minus, Binary)` and `(Minus, Unary)` are distinct — no PRECEPT0020a |
| `GivenTwoArmsWithSameTokenAndSameArity_ReportsPRECEPT0020a` | Injected collision: two `Unary` arms both pointing at `TokenKind.Minus` — reports ByToken collision on the second arm |
| `GivenTwoBinaryArmsWithSameToken_ReportsPRECEPT0020b` | Injected collision: two `Binary` arms both pointing at `TokenKind.Plus` — reports OperatorPrecedence collision on the second arm |
| `GivenTwoBinaryArmsWithSameToken_ReportsBoth0020a_And_0020b` | Same scenario as above also triggers PRECEPT0020a (since `(Plus, Binary)` collides) — confirms both rules fire |
| `GivenOperatorWithInlineToken_DoesNotCrash` | If `Token` arg is inline `new TokenMeta(...)`, skip gracefully — PRECEPT0020 skips, no crash |

**Regression anchors:**

Run `dotnet test test/Precept.Analyzers.Tests/` — all existing analyzer tests must remain green. Run the analyzer against `src/Precept/Language/Operators.cs` — zero diagnostics emitted (no real collisions currently exist).

---

**Acceptance criteria:**

- `Precept0020OperatorsTokenCollision.cs` exists and compiles
- PRECEPT0020a fires when two arms share `(Token.Kind, Arity)` — confirmed by unit tests
- PRECEPT0020b fires when two binary arms share `Token.Kind` — confirmed by unit tests
- Running against `src/Precept/Language/Operators.cs` produces zero diagnostics (current catalog is clean)
- All existing analyzer tests pass (`dotnet test test/Precept.Analyzers.Tests/`)
- `dotnet build` — zero errors, zero warnings

**Assignee:** George

---

## Phase 2e — Analyzer Gap Closure (Slices 29–32)

---

### Slice 29: `KeywordsValidAsMemberName` → `IsValidAsMemberName` Flag on `TokenMeta` ⏳ PENDING

**Goal:** Move per-token domain knowledge out of a hardcoded `FrozenSet` in `Parser.cs` and into a `TokenMeta` flag, so that future keywords valid as member names are declared at the catalog level.

**Rationale:** `Parser.KeywordsValidAsMemberName` is a hardcoded `FrozenSet<TokenKind> { Min, Max }`. The fact that `Min` and `Max` are valid in member-name position is per-token domain knowledge — it belongs in `TokenMeta`, not scattered through parse logic. If a future keyword (e.g., `Count`, `First`, `Last`) should also be valid as a member name, a developer must know to find and update this set; the catalog gives no hint. Adding `IsValidAsMemberName: bool` to `TokenMeta` and deriving the set from `Tokens.All` makes the catalog self-describing and makes `KeywordsValidAsMemberName` a zero-maintenance derivation. This follows the metadata-driven architecture: derive vocabulary from catalog; do not scatter hardcoded lists. Existing behavior is correct — this is a catalog consistency improvement, not a bug fix.

**Dependency:** Independent. Can run before or after Phase 2b. Slice 16 has already landed and `KeywordsValidAsMemberName` is in use; this slice updates the derivation without touching any consumer.

---

**Files to create / modify:**

| File | Change |
|------|--------|
| `src/Precept/Language/Tokens.cs` | Add `IsValidAsMemberName: bool` property (default `false`) to `TokenMeta`; set to `true` on `Min` and `Max` arms in `GetMeta()` |
| `src/Precept/Pipeline/Parser.cs` | Replace hardcoded `new[] { TokenKind.Min, TokenKind.Max }.ToFrozenSet()` with `Tokens.All.Where(t => t.IsValidAsMemberName).Select(t => t.Kind).ToFrozenSet()` |
| `test/Precept.Tests/` (existing `TokensTests.cs` or equivalent) | Update any `TokenMeta` construction tests that assert on parameter count; add new flag-verification tests |

---

**`TokenMeta` change (in `Tokens.cs`):**

```csharp
// Add IsValidAsMemberName with default false:
public sealed record TokenMeta(TokenKind Kind, string? Text, TokenCategory Category, ..., bool IsValidAsMemberName = false);
```

**`GetMeta` arm changes (two arms only):**

```csharp
TokenKind.Min => new(kind, "min", Cat_Qnt, ..., IsValidAsMemberName: true),
TokenKind.Max => new(kind, "max", Cat_Qnt, ..., IsValidAsMemberName: true),
```

**`Parser.cs` derivation change:**

```csharp
// Before:
internal static readonly FrozenSet<TokenKind> KeywordsValidAsMemberName =
    new[] { TokenKind.Min, TokenKind.Max }.ToFrozenSet();

// After:
internal static readonly FrozenSet<TokenKind> KeywordsValidAsMemberName =
    Tokens.All.Where(t => t.IsValidAsMemberName).Select(t => t.Kind).ToFrozenSet();
```

---

**Tests to add:**

| Test Method | Verifies |
|-------------|----------|
| `TokenMeta_Min_IsValidAsMemberName_True` | `Tokens.GetMeta(TokenKind.Min).IsValidAsMemberName == true` |
| `TokenMeta_Max_IsValidAsMemberName_True` | `Tokens.GetMeta(TokenKind.Max).IsValidAsMemberName == true` |
| `TokenMeta_AllOtherKeywords_IsValidAsMemberName_False` | All `TokenKind` members except `Min` and `Max` have `IsValidAsMemberName == false` (theory test) |
| `Parser_KeywordsValidAsMemberName_ContainsMinAndMax` | `Parser.KeywordsValidAsMemberName.SetEquals(new[] { TokenKind.Min, TokenKind.Max })` — derivation produces correct set |

**Regression anchors:** Slice 16 acceptance tests (`Parse_MemberAccess_MinKeywordAsMemberName`, `Parse_MemberAccess_MaxKeywordAsMemberName`) must pass unchanged. All existing `ExpressionParserTests` member-access tests.

---

**Acceptance criteria:**

- `TokenMeta.IsValidAsMemberName` property exists with default `false`
- `Tokens.GetMeta(TokenKind.Min).IsValidAsMemberName == true`
- `Tokens.GetMeta(TokenKind.Max).IsValidAsMemberName == true`
- All other `TokenMeta` entries have `IsValidAsMemberName == false`
- `Parser.KeywordsValidAsMemberName` is derived from `Tokens.All.Where(t => t.IsValidAsMemberName)` — no hardcoded `{ Min, Max }` array
- All Slice 16 member-access tests pass unchanged
- `dotnet build` — zero errors, zero warnings
- `dotnet test` — all tests pass

**Assignee:** George

---

### Slice 30: PRECEPT0021 — Tokens Duplicate Text ⏳ PENDING

**Goal:** Add a Roslyn analyzer that catches duplicate `Text` values across `TokenMeta` arms in `Tokens.GetMeta` at compile time, before they silently corrupt the lexer's keyword-to-token mapping.

**Rationale:** Every keyword `TokenMeta` has a `Text` field — the lexer's keyword string (e.g., `"field"`, `"state"`, `"=="`). If two keyword tokens share the same `Text`, the lexer's internal keyword→TokenKind lookup silently returns the first-registered token for all matches, making the second token permanently unreachable. Unlike `ByToken` collisions (which throw at startup), this failure mode is **silent** — parse results are wrong with no diagnostic and no startup exception. The Tokens catalog is the largest catalog (50+ members) and the most frequent target for new keyword additions; duplicate text is an easy copy-paste error with severe consequences. Detection uses `ResolveStringConstant` (already in `CatalogAnalysisHelpers`) and follows the same per-arm collection + compilation-end reporting pattern established by PRECEPT0020. See `docs/working/analyzer-recommendations.md` § Gap B and § PRECEPT0021 for full rationale.

**Dependency:** Slice 28 (establishes the per-arm collection + compilation-end pattern; not a compile dependency, but Slice 30 is a direct extrapolation of PRECEPT0020's analysis shape). Can be implemented in parallel once the pattern is understood.

---

**Files to create / modify:**

| File | Change |
|------|--------|
| `src/Precept.Analyzers/Precept0021TokensDuplicateText.cs` | **Create** — new `[DiagnosticAnalyzer]` implementing PRECEPT0021 |
| `test/Precept.Analyzers.Tests/Precept0021TokensDuplicateTextTests.cs` | **Create** — unit tests |

---

**Invariant enforced:**

| Sub-rule | ID | Invariant | Severity |
|----------|----|-----------|----------|
| Duplicate keyword text | PRECEPT0021 | No two `TokenMeta` arms may have the same non-empty `Text` value | Error |

**Exclusion rule:** Arms where `Text` is `null` or `""` are skipped — non-keyword tokens (`Identifier`, `NumberLiteral`, `StringLiteral`, `Comment`, `NewLine`, `EndOfSource`) carry null or empty text and must not be flagged.

---

**Method inventory — `Precept0021TokensDuplicateText.cs`:**

| Symbol | Kind | Notes |
|--------|------|-------|
| `DiagnosticId` | const string | `"PRECEPT0021"` |
| `Rule` | static field | `DiagnosticDescriptor`, category `"Precept.Language"`, severity `Error` |
| `SupportedDiagnostics` | override property | `ImmutableArray.Create(Rule)` |
| `Initialize(AnalysisContext)` | override method | `RegisterCompilationStartAction` → registers `SwitchExpression` action + `RegisterCompilationEndAction` |
| `Collect(OperationAnalysisContext, ConcurrentBag<TokenTextArmInfo>)` | private static | Scopes to `GetMeta(TokenKind)` via `TryGetCatalogSwitchKind`; per arm: calls `ResolveStringConstant` on the `Text` positional arg (second arg); skips null/empty; adds to bag |
| `CrossCheck(CompilationAnalysisContext, ConcurrentBag<TokenTextArmInfo>)` | private static | Groups by text value; reports the second (and subsequent) occurrence for each duplicated text string |
| `TokenTextArmInfo` | private sealed class | `ArmCase: string`, `Text: string`, `Location: Location` |

**Key implementation note — `Text` argument extraction:**

The `Text` parameter is the second positional argument to `TokenMeta(TokenKind kind, string? text, ...)`. Use `ResolveStringConstant` (from `CatalogAnalysisHelpers`) on this argument. If the result is `null` or `""`, skip the arm.

---

**Tests — `Precept0021TokensDuplicateTextTests.cs`:**

| Test | What it verifies |
|------|-----------------|
| `GivenTokensWithAllDistinctText_NoDiagnostic` | Baseline — current `Tokens.GetMeta` produces zero diagnostics |
| `GivenArmWithNullText_NoDiagnostic` | Null `Text` arms (e.g., `Identifier`) are skipped cleanly |
| `GivenArmWithEmptyText_NoDiagnostic` | Empty string `Text` arms are skipped cleanly |
| `GivenTwoArmsWithSameText_ReportsPRECEPT0021` | Injected collision: two arms with `"all"` — reports PRECEPT0021 on the second arm |
| `GivenThreeArmsWithSameText_ReportsTwice` | Three arms sharing the same text — reports PRECEPT0021 on second and third arms |

**Regression anchors:** Run `dotnet test test/Precept.Analyzers.Tests/` — all existing analyzer tests must remain green. Run against `src/Precept/Language/Tokens.cs` — zero diagnostics (current catalog has no duplicate text values).

---

**Acceptance criteria:**

- `Precept0021TokensDuplicateText.cs` exists and compiles
- PRECEPT0021 fires when two arms share the same non-empty `Text` — confirmed by unit tests
- Null/empty `Text` arms are skipped cleanly — confirmed by unit tests
- Running against `src/Precept/Language/Tokens.cs` produces zero diagnostics (current catalog is clean)
- All existing analyzer tests pass (`dotnet test test/Precept.Analyzers.Tests/`)
- `dotnet build` — zero errors, zero warnings

**Assignee:** George

---

### Slice 31: PRECEPT0022 — OperatorMeta Inline Token Reference ⏳ PENDING

**Goal:** Add a Roslyn analyzer that enforces `Operators.GetMeta` arms reference their `Token` field via `Tokens.GetMeta(TokenKind.X)` rather than inline `new TokenMeta(...)` construction, completing the "no inline Token" invariant uniformly across all catalogs.

**Rationale:** PRECEPT0008c (Types), PRECEPT0011e (Modifiers), and PRECEPT0013a (Actions) all enforce that `Token` references in catalog entries use `Tokens.GetMeta(TokenKind.X)` rather than inline `new TokenMeta(...)`. The Operators catalog has no equivalent enforcement. All current `Operators.GetMeta` arms already use `Tokens.GetMeta(TokenKind.X)` correctly, so this is low risk — but it completes the invariant uniformly across all catalogs. If a future arm introduced an inline `Token`, semantic token coloring or hover behavior could silently diverge from the canonical `Tokens` catalog entry without a compile-time diagnostic. See `docs/working/analyzer-recommendations.md` § Gap C and § PRECEPT0022 for full rationale.

**Priority:** Low / opportunistic — the existing Operators catalog is clean; this closes a consistency gap.

**Dependency:** Slice 28 (same analysis infrastructure pattern). After Phase 2b ships, review the `Token` field extraction logic — `OperatorMeta`'s `Token` field will be restructured into the `SingleTokenOp`/`MultiTokenOp` DU — and update the analyzer's extraction path if needed.

---

**Files to create / modify:**

| File | Change |
|------|--------|
| `src/Precept.Analyzers/Precept0022OperatorMetaInlineToken.cs` | **Create** — new `[DiagnosticAnalyzer]` implementing PRECEPT0022 |
| `test/Precept.Analyzers.Tests/Precept0022OperatorMetaInlineTokenTests.cs` | **Create** — unit tests |

---

**Invariant enforced:**

| Sub-rule | ID | Invariant | Severity |
|----------|----|-----------|----------|
| Inline Token reference | PRECEPT0022 | Every `OperatorMeta` arm's `Token` argument must be `Tokens.GetMeta(TokenKind.X)`, not `new TokenMeta(...)` | Warning |

**Severity rationale:** Warning — consistent with PRECEPT0008c, PRECEPT0011e, and PRECEPT0013a (the existing inline-Token rules for Types, Modifiers, and Actions all use Warning).

---

**Method inventory — `Precept0022OperatorMetaInlineToken.cs`:**

| Symbol | Kind | Notes |
|--------|------|-------|
| `DiagnosticId` | const string | `"PRECEPT0022"` |
| `Rule` | static field | `DiagnosticDescriptor`, category `"Precept.Language"`, severity `Warning` |
| `SupportedDiagnostics` | override property | `ImmutableArray.Create(Rule)` |
| `Initialize(AnalysisContext)` | override method | `RegisterOperationAction` on `OperationKind.ObjectCreation` |
| `Analyze(OperationAnalysisContext)` | private static | Scopes to `GetMeta(OperatorKind)` switch arms via `TryGetCatalogSwitchKind`; for each `IObjectCreationOperation` constructing `OperatorMeta`: checks whether the `Token` constructor argument is `IObjectCreationOperation` (inline — report PRECEPT0022) or `IInvocationOperation` (catalog call — clean) |

**Implementation analog:** PRECEPT0008c, PRECEPT0011e, PRECEPT0013a — direct extrapolation of the existing inline-Token detection pattern to the Operators catalog.

---

**Tests — `Precept0022OperatorMetaInlineTokenTests.cs`:**

| Test | What it verifies |
|------|-----------------|
| `GivenOperatorsWithAllCatalogTokenRefs_NoDiagnostic` | Baseline — current `Operators.GetMeta` produces zero diagnostics |
| `GivenOperatorArmWithInlineToken_ReportsPRECEPT0022` | Arm with `new TokenMeta(TokenKind.And, "and", ...)` instead of `Tokens.GetMeta(TokenKind.And)` — reports PRECEPT0022 |
| `GivenOperatorArmWithCatalogTokenRef_NoDiagnostic` | Arm using `Tokens.GetMeta(TokenKind.And)` — no warning |

**Regression anchors:** Run `dotnet test test/Precept.Analyzers.Tests/` — all existing analyzer tests must remain green. Run against `src/Precept/Language/Operators.cs` — zero diagnostics (current catalog uses catalog calls only).

---

**Acceptance criteria:**

- `Precept0022OperatorMetaInlineToken.cs` exists and compiles
- PRECEPT0022 fires when an `OperatorMeta` arm uses `new TokenMeta(...)` — confirmed by unit tests
- Running against `src/Precept/Language/Operators.cs` produces zero diagnostics (current catalog is clean)
- All existing analyzer tests pass (`dotnet test test/Precept.Analyzers.Tests/`)
- `dotnet build` — zero errors, zero warnings

**Assignee:** George

---

### Slice 32: PRECEPT0023 — OperatorMeta DU Shape Invariants ⏳ PENDING (deferred — depends on Phase 2b completion)

> ⚠️ **Deferred.** This slice is tracked here for completeness but **must not be implemented until Phase 2b (Slices 19–22) is complete.** The `SingleTokenOp`/`MultiTokenOp` DU must exist before this analyzer can be written. Do not begin until George confirms Slices 19–22 are done.

**Goal:** Add a Roslyn analyzer that enforces structural invariants on the `SingleTokenOp`/`MultiTokenOp` DU introduced by Phase 2b — preventing multi-token operator definitions that would cause `ByTokenSequence` startup failures or lexer prefix ambiguity at compile time.

**Rationale:** After Phase 2b restructures `OperatorMeta` into `SingleTokenOp`/`MultiTokenOp`, the `ByTokenSequence` dictionary indexes operators by lead token. If two `MultiTokenOp` entries share the same lead token, `ByTokenSequence` throws at startup. If a `MultiTokenOp.Tokens` sequence has only one element, that arm should have been a `SingleTokenOp`. If a `SingleTokenOp` lead token equals any `MultiTokenOp` lead token, the parser cannot determine which lookup to apply. These structural invariants should be caught at compile time, not at runtime. See `docs/working/analyzer-recommendations.md` § Gap D and § PRECEPT0023 for full rationale.

**Dependency:** **Phase 2b (Slices 19–22) MUST be complete.** The `SingleTokenOp`/`MultiTokenOp` DU must exist and be the canonical `OperatorMeta` shape before this analyzer is meaningful.

---

**Files to create / modify:**

| File | Change |
|------|--------|
| `src/Precept.Analyzers/Precept0023OperatorMetaDuShape.cs` | **Create** — new `[DiagnosticAnalyzer]` implementing PRECEPT0023a, PRECEPT0023b, PRECEPT0023c |
| `test/Precept.Analyzers.Tests/Precept0023OperatorMetaDuShapeTests.cs` | **Create** — unit tests |

---

**Invariants enforced:**

| Sub-rule | ID | Invariant | Severity |
|----------|----|-----------|----------|
| Under-populated multi-token | PRECEPT0023a | `MultiTokenOp.Tokens` must have at least 2 elements — a 1-token sequence is a `SingleTokenOp` | Error |
| Lead token prefix ambiguity | PRECEPT0023b | No `SingleTokenOp` lead token may equal any `MultiTokenOp` lead token — causes parser prefix ambiguity | Error |
| Duplicate multi-token lead | PRECEPT0023c | No two `MultiTokenOp` entries may share the same lead token — would make `ByTokenSequence` throw at startup | Error |

---

**Method inventory — `Precept0023OperatorMetaDuShape.cs` (sketch — exact shape depends on Phase 2b DU API):**

| Symbol | Kind | Notes |
|--------|------|-------|
| `DiagnosticId_UnderPopulatedMulti` | const string | `"PRECEPT0023a"` |
| `DiagnosticId_LeadTokenAmbiguity` | const string | `"PRECEPT0023b"` |
| `DiagnosticId_DuplicateMultiLead` | const string | `"PRECEPT0023c"` |
| `Rule_a`, `Rule_b`, `Rule_c` | static fields | `DiagnosticDescriptor`, category `"Precept.Language"`, severity `Error` |
| `SupportedDiagnostics` | override property | All three rules |
| `Initialize(AnalysisContext)` | override method | `RegisterCompilationStartAction` + `RegisterCompilationEndAction` (same pattern as PRECEPT0020) |
| `Collect(...)` | private static | Per arm: detect `SingleTokenOp` vs. `MultiTokenOp` DU subtype; extract lead token; collect into two bags |
| `CrossCheck(...)` | private static | Apply PRECEPT0023a–c logic and report at compilation end |

**Review note after Phase 2b ships:** Verify the DU subtype names, constructor shapes, and token extraction methods match what Phase 2b actually produces. The sketch above uses anticipated names; the final analyzer must use the actual Phase 2b API surface.

---

**Tests — `Precept0023OperatorMetaDuShapeTests.cs` (sketch — finalize after Phase 2b):**

| Test | What it verifies |
|------|-----------------|
| `GivenCleanOperatorsDuShape_NoDiagnostic` | Baseline — post-Phase-2b catalog produces zero diagnostics |
| `GivenMultiTokenOpWithOneToken_ReportsPRECEPT0023a` | `MultiTokenOp` with `Tokens.Length == 1` — reports under-populated error |
| `GivenSingleTokenOpMatchingMultiTokenLeadToken_ReportsPRECEPT0023b` | Lead-token collision between `SingleTokenOp` and `MultiTokenOp` — reports ambiguity error |
| `GivenTwoMultiTokenOpsWithSameLeadToken_ReportsPRECEPT0023c` | Two `MultiTokenOp` entries sharing lead token — reports ByTokenSequence collision error |

**Regression anchors:** All existing analyzer tests pass. Run against Phase-2b-updated `Operators.cs` — zero diagnostics on the clean catalog.

---

**Acceptance criteria:**

- `Precept0023OperatorMetaDuShape.cs` exists and compiles against the Phase 2b DU shape
- PRECEPT0023a fires on a 1-element `MultiTokenOp.Tokens` — confirmed by unit tests
- PRECEPT0023b fires on `SingleTokenOp`/`MultiTokenOp` lead-token collision — confirmed by unit tests
- PRECEPT0023c fires on two `MultiTokenOp` entries sharing lead token — confirmed by unit tests
- Running against post-Phase-2b `Operators.cs` produces zero diagnostics
- All existing analyzer tests pass
- `dotnet build` — zero errors, zero warnings

**Assignee:** George *(start only after Phase 2b — Slices 19–22 — is confirmed complete)*

---

### 🔧 Process Note: `CatalogAnalysisHelpers.CatalogEnumNames` — Manual Maintenance Required

> This is a **process discipline item**, not a numbered slice. No implementation is required beyond adding an inline comment.

**Location:** `src/Precept.Analyzers/CatalogAnalysisHelpers.cs`

**What it is:** `CatalogAnalysisHelpers.CatalogEnumNames` is a hardcoded `HashSet<string>` that `TryGetCatalogSwitchKind` uses to identify catalog switch expressions:

```csharp
private static readonly HashSet<string> CatalogEnumNames = new()
{
    "TypeKind", "TokenKind", "OperatorKind", "OperationKind",
    "ModifierKind", "FunctionKind", "ActionKind", "ConstructKind",
    "DiagnosticCode", "FaultCode", "ExpressionFormKind",
    // Keep in sync with docs/language/catalog-system.md catalog table
};
```

**The gap:** This list must be updated manually whenever a new catalog enum is added. `ExpressionFormKind` was the 13th catalog added in Phase 1 and required a manual update to this set. A future 14th catalog will require the same step. An analyzer cannot enforce its own constants (meta-circularity), so the fix is process-level.

**Required action — new-catalog checklist item:**

Every time a new catalog enum is added, the following step **must** be completed:

> Add the new enum's type name (string) to `CatalogAnalysisHelpers.CatalogEnumNames` in `src/Precept.Analyzers/CatalogAnalysisHelpers.cs`.

**Concrete step for Phase 2e:** When implementing Phase 2e slices, add the inline comment shown above to `CatalogEnumNames` if it is not already present. No other code change is needed — the existing list is current.

**Ownership:** George (runtime dev) adds the comment during Phase 2e. Future catalog additions: whoever adds the new catalog is responsible for updating this set.
