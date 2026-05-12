# Pipeline Audit Fix Plan

**Status:** ✅ All 7 fixes implemented  
**Implemented by:** George  
**Tests:** 4,598/4,598 passing

---

## Build Configuration

### Action: Add `Directory.Build.props` at repo root

Shane's mandate: Release-only builds with PDB symbols. No Debug configuration.

```xml
<Project>
  <PropertyGroup>
    <Configuration Condition="'$(Configuration)' == ''">Release</Configuration>
    <DebugSymbols>true</DebugSymbols>
    <DebugType>portable</DebugType>
  </PropertyGroup>
</Project>
```

**Rationale:** SDK defaults for Release already include portable PDB symbols. The props file makes it explicit and enforced across all projects. All `Debug.Assert`, `Debug.Fail`, `#if DEBUG` must be eliminated — they are stripped in Release builds.

✅ **Implemented:** `Directory.Build.props` created at repo root.

---

## Debug.Assert Conversion: All 7 Sites

All `Debug.Assert` calls in the pipeline must become unconditional `throw new InvalidOperationException(...)`.

| # | File | ~Line | Invariant | Converted |
|---|------|-------|-----------|-----------|
| 1 | `TypeChecker.cs` | 571–575 | D26 — no TypedErrorExpression without diagnostic | ✅ |
| 2 | `TypeChecker.cs` | 591–595 | D26 — same | ✅ |
| 3 | `TypeChecker.cs` | 1211–1214 | D26 — same | ✅ |
| 4 | `TypeChecker.Expressions.cs` | ~940 | D5 — secondary expression not null | ✅ |
| 5 | `TypeChecker.Expressions.cs` | ~959 | D5 — same | ✅ |
| 6 | `TypeChecker.Expressions.cs` | ~995 | D5 — same | ✅ |
| 7 | `Lexer.cs` | ~131 | Mode stack depth | ✅ |

**Doc-comment updates:** Two doc comments in `SemanticIndex.cs` referenced "Debug.Assert" — updated to "unconditional throw".

**Conversion pattern:**
```csharp
// Before
Debug.Assert(someCondition, "message");

// After
if (!someCondition)
    throw new InvalidOperationException("message");
```

---

## P1 Fixes: D26 Diagnostic Gap Closures

**D26 Invariant:** Any `TypedErrorExpression` in `SemanticIndex` must have ≥1 Error-severity diagnostic.

**Canonical pattern (from commit dd1d8e7f):**
```csharp
ctx.Diagnostics.Add(Diagnostics.Create(DiagnosticCode.TypeMismatch, span, expected, actual));
return new TypedErrorExpression(span);
```

**DiagnosticCode choice:** `TypeMismatch` (code 18) for all 4 fixes. These are catalog consistency guards, not user-authored errors. `TypeMismatch` is the established catch-all for type-level errors. A new `UnknownOperator` code was considered and rejected — the errors only surface if catalog metadata is inconsistent, which is a developer error, not a user error.

---

### Fix 1: `ResolveBinaryOp` — Operator Lookup Failure

**File:** `src/Precept/Pipeline/TypeChecker.Expressions.cs`  
**Method:** `ResolveBinaryOp()`  
**Location:** Operator lookup failure path (~line 622)

**Problem:** If `OperatorCatalog.TryGet(op, left, right)` fails, the method returned `TypedErrorExpression` without emitting a diagnostic. Silent D26 violation.

**Fix:** Emit `TypeMismatch` diagnostic before returning `TypedErrorExpression`.

```csharp
ctx.Diagnostics.Add(Diagnostics.Create(DiagnosticCode.TypeMismatch, bin.Span,
    $"{left.Type} {op} {right.Type}", "no matching operator"));
return new TypedErrorExpression(bin.Span);
```

✅ **Implemented.**

---

### Fix 2: `ResolveUnaryOp` — Operator Lookup Failure

**File:** `src/Precept/Pipeline/TypeChecker.Expressions.cs`  
**Method:** `ResolveUnaryOp()`  
**Location:** Operator lookup failure path (~line 763)

**Problem:** Same pattern as Fix 1 — unary operator lookup failure returned `TypedErrorExpression` silently.

**Fix:** Emit `TypeMismatch` diagnostic before returning `TypedErrorExpression`.

✅ **Implemented.**

---

### Fix 3: `Resolve()` Default Case

**File:** `src/Precept/Pipeline/TypeChecker.Expressions.cs`  
**Method:** `Resolve()`  
**Location:** Default case of expression dispatch switch (~line 101)

**Problem:** Unrecognized `ExpressionFormKind` values returned `TypedErrorExpression` without a diagnostic.

**Fix:** Emit `TypeMismatch` diagnostic in the default case.

✅ **Implemented.**

---

### Fix 4: `ResolveAction()` Default + `MalformedAction`

**File:** `src/Precept/Pipeline/TypeChecker.Expressions.cs`  
**Method:** `ResolveAction()`  
**Location:** Default case + `MalformedAction` path (~line 1026)

**Problem:** Two paths returned without emitting diagnostics.

**Fix:** Emit `TypeMismatch` for both paths.

✅ **Implemented.**

---

## P2 Fixes

### Fix 5: `Fault.cs` — `ExpressionContext` and `InputValues` Fields

**File:** `src/Precept/Language/Fault.cs`

**Problem:** `Fault` struct had a TODO comment deferring all context fields to "when the evaluator is designed". This was incorrect — `EventOutcome`, `UpdateOutcome`, and `PreceptValue` are already fully designed.

**Design (approved by Shane):** Add fields now using existing types. Leave open question in `Evaluator.cs` for D8/R4 follow-up on whether `ExpressionContext` should become a richer descriptor type.

**Field additions:**
- `SourceSpan? ExpressionContext = null` — structured source location of the failing expression
- `IReadOnlyDictionary<string, PreceptValue>? InputValues = null` — field/arg values at fault time

**Nullable defaults** keep all existing `Faults.Create()` call sites unchanged. Callers attach context via `with` expressions post-construction.

```csharp
public readonly record struct Fault(
    FaultCode                                    Code,
    string                                       CodeName,
    string                                       Message,
    SourceSpan?                                  ExpressionContext = null,
    IReadOnlyDictionary<string, PreceptValue>?   InputValues      = null
);
```

**Open question placed in `Evaluator.cs`:** `// TODO D8/R4: revisit whether ExpressionContext should become a typed descriptor rather than a raw SourceSpan when the evaluator is implemented.`

✅ **Implemented.** Regression tests added in `FaultsTests.cs`.

---

### Fix 6: `ParsedExpression.cs` — `CIFunctionCall` TODO Comment

**File:** `src/Precept/Pipeline/ParsedExpression.cs`  
**Location:** `CIFunctionCallExpression` record (~line 62)

**Problem:** A TODO comment suggested the `FunctionKind` should be resolved in the parser. This is wrong — `FunctionKind` resolution belongs in the type checker (`ResolveCIFunctionCall`), not the parser.

**Fix:** Replace TODO with rationale comment explaining the correct ownership.

```csharp
/// FunctionKind is resolved by the type checker (ResolveCIFunctionCall),
/// not stamped here.
```

✅ **Implemented.**

---

### Fix 7: `GraphAnalyzer.cs` — Gap1 Event Modifier Dispatch Loop

**File:** `src/Precept/Pipeline/GraphAnalyzer.cs`  
**Location:** Event modifier analysis loop (~line 173)

**Problem:** A `// TODO: Gap1` comment existed where event modifier analysis should dispatch on `EventModifierMeta.RequiredAnalysis`. The missing dispatch meant new `GraphAnalysisKind` members could be silently ignored.

**Design decision:** Unconditional `throw new InvalidOperationException(...)` as the default case. Mirrors the `StateModifierMeta` dispatch pattern in `GetStateFlags()`. The `InitialEventCompatibility` case is a no-op (handled implicitly by `initialState` logic) but structurally present.

**Fix 7 vs. PRECEPT019:** Frank-22 evaluated whether PRECEPT019 (Roslyn analyzer from PR #133) supersedes this fix. Verdict: it does not. PRECEPT019 enforces method-level coverage via annotations — it has zero visibility into an inline switch inside a loop. Fix 7's unconditional throw is proportionate for a two-member enum with one dispatch site. See `.squad/decisions/inbox/frank-fix7-precept019.md`.

```csharp
foreach (var modifier in evt.Modifiers)
{
    switch (modifier.Meta.RequiredAnalysis)
    {
        case GraphAnalysisKind.InitialEventCompatibility:
            // handled implicitly by initialState logic
            break;
        case GraphAnalysisKind.TransitionReachability:
            AnalyzeTransitionReachability(evt, modifier, diagnostics);
            break;
        default:
            throw new InvalidOperationException(
                $"Unhandled GraphAnalysisKind: {modifier.Meta.RequiredAnalysis}");
    }
}
```

✅ **Implemented.**

---

## Implementation Order (followed)

1. `Directory.Build.props` — build config
2. Debug.Assert → throw conversion (all 7 sites) + SemanticIndex doc comments
3. Fix 1 & 2 (binary/unary operator lookup failures)
4. Fix 3 (`Resolve()` default case)
5. Fix 4 (`ResolveAction()` default + `MalformedAction`)
6. Fix 7 (GraphAnalyzer Gap1 dispatch loop)
7. Fix 6 (ParsedExpression TODO → rationale comment)
8. Fix 5 (Fault.cs field additions + evaluator open question)

---

## Tests Added

| Test file | Tests | Coverage |
|-----------|-------|----------|
| `test/Precept.Tests/FaultsTests.cs` | 3 | Fix 5 — Fault struct with ExpressionContext and InputValues |
| `test/Precept.Tests/TypeChecker/TypeCheckerExpressionTests.cs` | 5 | Fixes 1–4 D26 defensive paths |

All 4,598 tests passing after implementation.
