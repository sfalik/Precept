# Analyzer Recommendations — Catalog Structural Consistency

> **Author:** Frank (Lead Architect)
> **Date:** 2026-05-12
> **Audience:** George (implementer), Shane (owner review)

---

## 1. Coverage Map — What's Already Enforced

Before identifying gaps, here is the full picture of what the 19 existing analyzers already lock down:

| ID | Scope | Invariant |
|----|-------|-----------|
| PRECEPT0001–0004 | `Fail`/`Diagnostic`/`Fault` call sites | Fault codes and diagnostic creation conventions |
| PRECEPT0005–0006 | Proof system | `ParamSubject` and `ProofSubject` placement |
| PRECEPT0007 | All catalog `GetMeta` switches | Exhaustive switch expression — every enum member must have an explicit arm |
| PRECEPT0008a–c | `Types.GetMeta` | `WidensTo` self-ref, `ImpliedModifiers` duplicates, inline `Token` creation |
| PRECEPT0009a–b | `Operations.GetMeta` | Duplicate `(Op, Lhs, Rhs, Match)` or `(Op, Operand)` keys — FrozenDict startup safety |
| PRECEPT0010a–d | `Types` ↔ `Operations` cross-catalog | `EqualityComparable`/`Orderable` trait ↔ operations symmetry |
| PRECEPT0011a–e | `Modifiers.GetMeta` | Self-subsumption, self-exclusivity, asymmetric mutex, circular subsumption, inline Token |
| PRECEPT0012a–b | `Functions.GetMeta` | Arity collisions across same-name functions, empty `Overloads` |
| PRECEPT0013a–b | `Actions.GetMeta` | Inline Token, empty `AllowedIn` |
| PRECEPT0014a–b | `Constructs.GetMeta` | `AllowedIn` self-reference, duplicate leading-token + identical slot-sequences |
| PRECEPT0015a–c | `Diagnostics.GetMeta` | `Code` identity mismatch, `RelatedCodes` self-reference, `nameof()` required |
| PRECEPT0016a–b | `Faults.GetMeta` | `Code` identity mismatch, `nameof()` required |
| PRECEPT0017 | ALL `GetMeta` switches (generic) | `Kind` argument must match switch arm pattern — no copy-paste mismatches |
| PRECEPT0018 | All `Precept.*` enums | Semantic enums must not put a real member at value 0 — guards against `default(T)` silent routing |
| PRECEPT0019 | `[HandlesCatalogExhaustively]` classes | Pipeline stage coverage — every form must have a `[HandlesCatalogMember]` method handler |

---

## 2. Identified Gaps

### Gap A — No Operators ByToken / OperatorPrecedence Collision Check

**The invariant that's missing:**

`Operators.cs` builds two FrozenDictionary indexes at startup:

```csharp
// Operators.ByToken — keyed by (TokenKind, Arity)
public static FrozenDictionary<(TokenKind, Arity), OperatorMeta> ByToken { get; } =
    All.ToFrozenDictionary(m => (m.Token.Kind, m.Arity));

// Parser.OperatorPrecedence — keyed by TokenKind (binary operators only)
internal static readonly FrozenDictionary<TokenKind, (int Precedence, bool RightAssociative)> OperatorPrecedence =
    Operators.All
        .Where(op => op.Arity == Arity.Binary)
        .ToFrozenDictionary(op => op.Token.Kind, ...);
```

If two `OperatorMeta` arms share the same `(Token.Kind, Arity)` pair, `ByToken` throws at startup. If two *binary* operators share the same `Token.Kind`, `OperatorPrecedence` also throws. Neither collision is currently caught at compile time.

PRECEPT0009 already catches exactly this problem for the Operations catalog (`ByBinaryOp` and `ByUnaryOp` FrozenDictionary collisions). **There is no equivalent check for Operators.**

**Triggering scenario:**

The `OperatorKind.Minus` / `OperatorKind.Negate` pair deliberately shares `TokenKind.Minus` but differs on `Arity`. This is fine for `ByToken` — the `(Minus, Binary)` and `(Minus, Unary)` keys are distinct. However, if Phase 2b introduces a new `Postfix` arity and someone adds `OperatorKind.PostfixMinus` pointing at `TokenKind.Minus` with `Arity.Unary`, the `ByToken` key would collide with `Negate`. Without an analyzer, this is a startup throw caught only at runtime.

Also: Phase 2b is restructuring `OperatorMeta` into `SingleTokenOp`/`MultiTokenOp`. The new shape will introduce a `ByTokenSequence` index alongside `ByToken`. Adding the analyzer now — against the current flat `OperatorMeta` shape — is the right time to lock down the ByToken invariant before the DU complicates the analysis.

### Gap B — No Tokens Duplicate-Text Check

**The invariant that's missing:**

Every keyword `TokenMeta` has a `Text` field — the lexer keyword string (e.g., `"field"`, `"state"`, `"=="`). If two keyword tokens share the same `Text`, the lexer's internal keyword→TokenKind lookup would silently produce the first-registered token for all matches, making the second token unreachable. This is silent data corruption, not a startup throw.

The Tokens catalog is the largest catalog (50+ members) and the most likely target for new keyword additions. Duplicate text is an easy copy-paste error.

**Triggering scenario:**

```csharp
// Typo: "all" used twice — first arm's entry is silently preferred
TokenKind.All => new(kind, "all", Cat_Qnt, ...),
...
TokenKind.Any => new(kind, "all", Cat_Qnt, ...),  // copy-paste error: should be "any"
```

The lexer would tokenize `all` and `any` identically. No existing analyzer catches this.

**Feasibility note:** Only keyword tokens carry meaningful `Text` values. Tokens like `Identifier`, `NumberLiteral`, `StringLiteral`, `Comment`, `NewLine`, `EndOfSource` have null, empty, or variable text — those must be excluded from the duplicate check. The filter is: non-null, non-empty `Text` values only.

### Gap C — No OperatorMeta Token Cross-Reference Check

**The invariant that's missing:**

PRECEPT0008c (Types), PRECEPT0011e (Modifiers), and PRECEPT0013a (Actions) all enforce that `Token` references in catalog entries use `Tokens.GetMeta(TokenKind.X)` rather than inline `new TokenMeta(...)`. No equivalent enforcement exists for `Operators.GetMeta`.

**Assessment:** All current `Operators.GetMeta` arms already use `Tokens.GetMeta(TokenKind.X)` correctly. This is a low-risk gap but would complete the "no inline Token" invariant across all catalogs uniformly.

### Gap D — OperatorMeta DU Shape Invariants (Phase 2b)

**The invariant that's missing (future):**

After Phase 2b restructures `OperatorMeta` → `SingleTokenOp`/`MultiTokenOp`:

- `MultiTokenOp.ContinuationTokens` must have at least one element (a sequence without a continuation is a `SingleTokenOp`)
- No `SingleTokenOp` token may be a prefix of any `MultiTokenOp` lead token (lexer ambiguity: the parser would consume the single-token operator before seeing the continuation)
- No two `MultiTokenOp` entries may share the same `(LeadToken, ContinuationTokens[0])` initial prefix

These invariants are analogous to PRECEPT0009 but operate on token sequences instead of enum pairs. **Not immediately actionable** — depends on Phase 2b completing the DU restructure.

### Gap E — `CatalogEnumNames` Staleness in `CatalogAnalysisHelpers`

**Not an analyzer — a process gap:**

`CatalogAnalysisHelpers.CatalogEnumNames` is a hardcoded `HashSet<string>` that `TryGetCatalogSwitchKind` uses to determine whether a switch is a catalog switch. When `ExpressionFormKind` was added as the 13th catalog in Phase 1, `CatalogEnumNames` had to be manually updated to include it.

This is an analyzer maintenance issue, not a production code issue. It cannot be enforced by a Roslyn analyzer without meta-circularity (an analyzer checking another analyzer's constants). The correct fix is process discipline: the "new catalog" checklist must include "add to `CatalogEnumNames`."

---

## 3. Candidate Analyzers — Prioritized Recommendations

### PRECEPT0020 — Operators ByToken and OperatorPrecedence Collision

**Priority: High | Cost: Medium | Feasibility: Practical**

| Attribute | Value |
|-----------|-------|
| Invariant | No two `OperatorMeta` arms share `(Token.Kind, Arity)` composite key; no two *binary* arms share `Token.Kind` |
| Detection | In `GetMeta(OperatorKind)` switch, extract `(tokenKindName, arityName)` per arm; report any duplicate pairs at compilation end |
| Violation example | `OperatorKind.Negate` with `Arity.Unary` AND a new `OperatorKind.PostfixMinus` with `Arity.Unary` both pointing at `TokenKind.Minus` — ByToken collision |
| Failure mode if missed | `FrozenDictionary` startup throw; also corrupts Pratt loop precedence lookups |
| Existing analog | PRECEPT0009 (duplicate binary/unary operation keys) — implementation is directly parallel |
| Dependencies | None — scoped to `GetMeta(OperatorKind)`, independent of Phase 2b DU restructure |
| DiagnosticSeverity | Error (both sub-checks) |

**Implementation sketch:**

```csharp
// PRECEPT0020a — ByToken collision: (Token.Kind, Arity) must be unique
// PRECEPT0020b — OperatorPrecedence collision: Token.Kind must be unique among binary arms
```

The key extraction challenge: `OperatorMeta.Token` is not a direct enum reference — it's an `IInvocationOperation` (`Tokens.GetMeta(TokenKind.X)`). The analyzer must:
1. Find the constructor argument named `Token` (or by position — it's the second arg in `OperatorMeta`)
2. Confirm it's an `IInvocationOperation` (i.e., a `Tokens.GetMeta()` call)
3. Extract the first argument of that invocation — an `IFieldReferenceOperation` on `TokenKind` — using `ResolveEnumFieldName`
4. Also extract `Arity` directly as a field reference from the constructor arg named `Arity`
5. Use a compilation-end action to report collisions across all collected arms

**Required new file:** `src/Precept.Analyzers/Precept0020OperatorsTokenCollision.cs`

**Required test file:** `test/Precept.Analyzers.Tests/Precept0020OperatorsTokenCollisionTests.cs`

**Required update:** `CatalogAnalysisHelpers.CatalogEnumNames` does not need updating (PRECEPT0020 uses `TryGetCatalogSwitchKind` which already includes `"OperatorKind"`).

---

### PRECEPT0021 — Tokens Duplicate Text

**Priority: Medium | Cost: Medium | Feasibility: Practical**

| Attribute | Value |
|-----------|-------|
| Invariant | No two `TokenMeta` arms have the same non-empty `Text` value |
| Detection | In `GetMeta(TokenKind)` switch, extract `Text` string per arm (keyword tokens have fixed text); report any duplicate at compilation end |
| Exclusions | Null/empty `Text` values (non-keyword tokens: `Identifier`, `NumberLiteral`, etc.) — skip those |
| Violation example | `TokenKind.Any => new(kind, "all", ...)` — copy-paste error from `TokenKind.All` above it |
| Failure mode if missed | Silent lexer non-determinism — `any` tokens are silently re-classified as `all`, corrupting parse results without any diagnostic |
| DiagnosticSeverity | Error |

**Implementation sketch:**

```csharp
// PRECEPT0021 — Tokens duplicate text
// Scoped to GetMeta(TokenKind) in Precept.Language.
// Collect (armCase, text, location) per arm where text != null && text != "".
// Report any pair sharing the same text string.
```

The `Text` argument is the second positional argument to `TokenMeta`. Use `ResolveStringConstant` (already in `CatalogAnalysisHelpers`) to extract it. Skip arms where text is null or empty.

**Required new file:** `src/Precept.Analyzers/Precept0021TokensDuplicateText.cs`

**Required test file:** `test/Precept.Analyzers.Tests/Precept0021TokensDuplicateTextTests.cs`

---

### PRECEPT0022 — OperatorMeta Inline Token Reference

**Priority: Low | Cost: Low | Feasibility: Practical**

| Attribute | Value |
|-----------|-------|
| Invariant | `OperatorMeta.Token` in every arm must be `Tokens.GetMeta(TokenKind.X)`, not inline `new TokenMeta(...)` |
| Detection | In `GetMeta(OperatorKind)` switch, check the `Token` argument; report if it's `IObjectCreationOperation` (inline) rather than `IInvocationOperation` (catalog call) |
| Violation example | `OperatorKind.And => new(kind, new TokenMeta(TokenKind.And, "and", ...), ...)` |
| Failure mode if missed | Token metadata is inconsistent with the canonical `Tokens` catalog entry — could cause semantic token coloring or hover mismatches without a runtime error |
| Existing analogs | PRECEPT0008c (Types), PRECEPT0011e (Modifiers), PRECEPT0013a (Actions) |
| DiagnosticSeverity | Warning (consistent with PRECEPT0008c, PRECEPT0011e) |

Can be folded into PRECEPT0020's implementation file as a third sub-rule (PRECEPT0020c), or added separately. Given it's a different invariant class (reference integrity vs. collision), a separate file is cleaner.

---

### PRECEPT0023 — OperatorMeta DU Shape Invariants (Phase 2b)

**Priority: Medium (Phase 2b) | Cost: Medium-High | Feasibility: Practical post-Phase-2b**

Not actionable until Phase 2b (`SingleTokenOp`/`MultiTokenOp` DU) ships. When it does, add:

| Sub-rule | Invariant |
|----------|-----------|
| PRECEPT0023a | `MultiTokenOp.ContinuationTokens` must be non-empty |
| PRECEPT0023b | No `SingleTokenOp` lead token may equal any `MultiTokenOp` lead token (prefix ambiguity) |
| PRECEPT0023c | No two `MultiTokenOp` entries share the same lead token (would make ByTokenSequence throw) |

Defer until Phase 2b design is final.

---

## 4. Existing Hardcoded Code — Highest-Value Targets

### 4.1 `Parser.KeywordsValidAsMemberName` — Should Be a `TokenMeta` Flag

**Location:** `src/Precept/Pipeline/Parser.cs`, line ~135

```csharp
internal static readonly FrozenSet<TokenKind> KeywordsValidAsMemberName =
    new[] { TokenKind.Min, TokenKind.Max }.ToFrozenSet();
```

**Problem:** `Min` and `Max` are documented as "DSL aggregation keywords but also idiomatic member-accessor names." This is per-token domain knowledge that belongs in `TokenMeta`. If a future keyword (e.g., `Count`, `First`, `Last`) should also be valid as a member name, it will require finding and updating this hardcoded set rather than setting a flag on the new token's `TokenMeta`.

**Fix:** Add `IsValidAsMemberName: bool` (default `false`) to `TokenMeta` (or as a `TokenCategory` flag). Set it to `true` on `TokenKind.Min` and `TokenKind.Max`. Derive `KeywordsValidAsMemberName` from `Tokens.All.Where(t => t.IsValidAsMemberName)`.

**Impact:** Minor catalog change + one `Parser.cs` derivation change + PRECEPT0007 pick up the new field automatically (no analyzer needed, but consistency improves). Not a blocker — existing behavior is correct.

### 4.2 `CatalogAnalysisHelpers.CatalogEnumNames` — Manual Maintenance Required

**Location:** `src/Precept.Analyzers/CatalogAnalysisHelpers.cs`, line ~57

```csharp
private static readonly HashSet<string> CatalogEnumNames = new()
{
    "TypeKind", "TokenKind", "OperatorKind", "OperationKind",
    "ModifierKind", "FunctionKind", "ActionKind", "ConstructKind",
    "DiagnosticCode", "FaultCode", "ExpressionFormKind",
};
```

**Problem:** This list must be updated manually whenever a new catalog enum is added. `ExpressionFormKind` was the 13th catalog added in Phase 1 — someone had to remember to update this. A future 14th catalog will need the same manual step.

**Fix:** Not an analyzer — process discipline. The new-catalog checklist should include: "Add `CatalogEnumTypeName` to `CatalogAnalysisHelpers.CatalogEnumNames`." Consider adding an inline comment to this list that says "keep in sync with catalog doc table" for discoverability.

---

## 5. Recommendation Summary

| Rank | ID | Invariant | Cost | Priority | Ship When |
|------|----|-----------|------|----------|-----------|
| 1 | PRECEPT0020 | Operators `ByToken` / `OperatorPrecedence` collision | Medium | **High** | Before Phase 2b |
| 2 | PRECEPT0021 | Tokens duplicate `Text` | Medium | **Medium** | After PRECEPT0020 |
| 3 | PRECEPT0022 | OperatorMeta inline `Token` reference | Low | **Low** | Opportunistic |
| 4 | PRECEPT0023 | OperatorMeta DU shape (Phase 2b) | Medium-High | **Medium** | After Phase 2b ships |

**Process action (no analyzer):** Add `CatalogEnumNames` update step to new-catalog checklist.

**Catalog change (no analyzer):** Add `IsValidAsMemberName` flag to `TokenMeta`, derive `KeywordsValidAsMemberName` from catalog.

---

## 6. Why PRECEPT0020 is Slice 28

PRECEPT0020 is the right next slice because:

1. **It closes a real gap** — PRECEPT0009 covers Operations; there's nothing for Operators. The gap is asymmetric and visible.
2. **The failure mode is a startup throw** — not a silent corruption. Startup failures in the Language static field initializers are hard to debug because the stack trace goes through `FrozenDictionary` internals, not through the offending catalog arm.
3. **The timing is right** — Phase 2b is about to restructure `OperatorMeta` into a DU. Locking down the existing `(Token.Kind, Arity)` invariant *before* that work makes Phase 2b safer.
4. **Implementation is a direct extrapolation** — PRECEPT0009 is the template. The new element is token-kind extraction from `Tokens.GetMeta()` invocations, which is a small, well-scoped addition to the established analysis pattern.

