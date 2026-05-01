# Extended Plan: Technical Design
**Author:** George  
**Date:** 2026-05-01

---

## Work Item A: Option B Full DU for OperatorMeta

### Decision Context

Shane chose **Option B full DU** over Frank's Option C (`Token → IReadOnlyList<TokenMeta>` flat replacement) and George's interim Option A (flat extension with nullable `Tokens`). The catalog-system.md DU rule applies: `SingleTokenOp` and `MultiTokenOp` genuinely need different *shaped* metadata. `LeadToken` on a flat record is an ambiguous accessor that conflates "the one token" with "the first of several tokens." The DU makes the distinction structurally impossible to violate.

---

### Type Definitions

**`src/Precept/Language/Operator.cs`** — full replacement:

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
    Presence    = 5,   // NEW — for is set / is not set and future presence operators
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

**Notes on `OperatorFamily.Presence`**: The existing `OperatorFamily.Membership` covers `contains`. Presence operators (`is set`, `is not set`) test a different semantic concept (whether a value is absent/present vs. whether a value is a member of a collection). Creating a distinct `Presence = 5` family cleanly separates the semantic token scopes for tooling. If this is considered over-engineering, collapse into `Membership` — but then the tmLanguage `keyword.operator.presence.precept` scope can't be distinguished from `keyword.operator.membership.precept` by family alone.

---

### OperatorKind Changes

**`src/Precept/Language/OperatorKind.cs`** — append two members:

```csharp
    // ── Presence (postfix) ─────────────────────────────────────────
    IsSet                     = 19,
    IsNotSet                  = 20,
```

Must be appended at the end to preserve the `All_IsInAscendingOrder` test invariant.

---

### Operators.All and GetMeta Changes

**`src/Precept/Language/Operators.cs`** — three changes:

**1. All 18 existing `GetMeta` arms** change from `new(kind, token, ...)` to `new SingleTokenOp(kind, token, ...)`. This is a mechanical one-for-one rename — no logic changes.

**2. Add two new arms for IsSet and IsNotSet**:

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

Precedence 60 matches the current Pratt loop binding power (`if (minPrecedence > 60) break;`).

**3. Update `ByToken` to exclude `MultiTokenOp` entries** (they can't be keyed by a single token without collision):

```csharp
    public static FrozenDictionary<(TokenKind, Arity), OperatorMeta> ByToken { get; } =
        All.OfType<SingleTokenOp>()
           .ToFrozenDictionary(m => (m.Token.Kind, m.Arity));
```

**`Operators.All`** derivation — no change. `Enum.GetValues<OperatorKind>().Select(GetMeta).ToArray()` automatically includes IsSet=19 and IsNotSet=20. Count grows from 18 to 20.

---

### Call Site Migration

Every existing call site that accesses `OperatorMeta.Token` directly will become a **compile error** under the DU because `Token` is only on `SingleTokenOp`, not on `OperatorMeta`. Full list of affected sites:

| File | Line (approx) | Current code | Required change |
|------|--------------|-------------|----------------|
| `src/Precept/Pipeline/Parser.cs` | ~38 | `Operators.All.Where(op => op.Arity == Arity.Binary).ToFrozenDictionary(op => op.Token.Kind, ...)` | Add `.OfType<SingleTokenOp>()` before `.ToFrozenDictionary(...)` — binary ops are always SingleTokenOp |
| `src/Precept/Language/Operators.cs` | ~149 | `All.ToFrozenDictionary(m => (m.Token.Kind, m.Arity))` | Change to `All.OfType<SingleTokenOp>().ToFrozenDictionary(m => (m.Token.Kind, m.Arity))` |
| `test/Precept.Tests/OperatorsTests.cs` | `GetMeta_TokenTextMatchesSymbol` theory | `Operators.GetMeta(kind).Token.Text` | Cast to `SingleTokenOp`: `((SingleTokenOp)Operators.GetMeta(kind)).Token.Text` or pattern-match |
| `test/Precept.Tests/OperatorsTests.cs` | `ByToken_CountMatchesAll` | Asserts `ByToken.Count == All.Count` (18 == 18) | Update to `ByToken.Count == All.OfType<SingleTokenOp>().Count()` (18 == 18 initially, then 18 when 20 total after IsSet/IsNotSet added) |
| `test/Precept.Tests/OperatorsTests.cs` | `ByToken_RoundTrip_AllEntriesRetrievable` | Iterates `Operators.All` and checks `ByToken` for each | Skip `MultiTokenOp` entries: add `.OfType<SingleTokenOp>()` guard |

**Parser.cs lines 1412 and 1417** — `Operators.ByToken.GetValueOrDefault((current.Kind, Arity.Binary))` — these access the ByToken *dictionary*, not `OperatorMeta.Token` directly. No change needed there; the dictionary type stays `FrozenDictionary<(TokenKind, Arity), OperatorMeta>`.

**PRECEPT0017 analyzer**: Not affected. It checks that the first constructor argument in `GetMeta` arms matches the switch pattern constant. Under the DU, `SingleTokenOp`'s first arg is still `Kind` (OperatorKind), and `MultiTokenOp`'s first arg is also `Kind`. The analyzer works unchanged.

---

### New ExpressionFormKind Member

**`src/Precept/Language/ExpressionForms.cs`** — add 11th member and catalog entry:

```csharp
// ExpressionFormKind enum — add after ListLiteral:
PostfixOperation = 11,   // is set / is not set — led (postfix) form
```

```csharp
// ExpressionForms.GetMeta — add arm:
ExpressionFormKind.PostfixOperation => new(
    kind, ExpressionCategory.Composite, true, [TokenKind.Is],
    "A postfix presence-check operation: expr is set / expr is not set."),
```

**`src/Precept/Pipeline/Parser.cs`** — add `[HandlesForm]` annotation to `ParseExpression`:

```csharp
[HandlesForm(ExpressionFormKind.MemberAccess)]
[HandlesForm(ExpressionFormKind.BinaryOperation)]
[HandlesForm(ExpressionFormKind.MethodCall)]
[HandlesForm(ExpressionFormKind.PostfixOperation)]   // NEW
internal Expression ParseExpression(int minPrecedence)
```

Once this annotation is added, PRECEPT0019 is green again. All 11 members covered.

---

## Work Item B: ByToken Multi-Token Key

### Problem

`ByToken: FrozenDictionary<(TokenKind, Arity), OperatorMeta>` cannot hold `IsSet` and `IsNotSet` without collision — both have lead token `TokenKind.Is` and arity `Arity.Postfix`. The existing entry format can't distinguish them.

### Design Options Evaluated

**Option 1 — `(TokenKind, TokenKind?)` tuple key**  
Key `(Is, Set)` → IsSet; `(Is, Not)` → IsNotSet. Works for current operators. Breaks if future `is not empty` also starts with `(Is, Not)` — then `Not` as second key no longer uniquely identifies the operator (both `is not set` and `is not empty` share the same first two tokens).

**Option 2 — `ByToken(params TokenKind[] tokens): OperatorMeta?`**  
Shane's explicit direction. A method that accepts the full token sequence and routes to the right entry. Internal storage can use any scheme. Callers don't need to know the storage key format. Works for sequences of any length.

**Option 3 — `ByLeadToken + ByTokenPair` two-dictionary split**  
More lookups, more code. Unnecessary for the current and near-term operator set.

### Recommendation: Option 2 (method with params), backed by a tuple dictionary

A `ByTokenSequence` lookup method backed by `FrozenDictionary<(TokenKind, TokenKind?, TokenKind?), OperatorMeta>` handles all current operators (max 3 tokens) and any future addition up to 3 tokens (which covers all plausible presence operators: `is empty`, `is not empty`, `has value`, etc.).

```csharp
// Internal storage — only MultiTokenOp entries
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

**How this works with the Pratt parser's token-by-token look-ahead:**

The Pratt loop already handles `is set`/`is not set` through a named special-case block before `OperatorPrecedence.TryGetValue`. The dispatch sequence is:

1. Current token = `Is`
2. Advance past `Is`, peek: is it `Not`?  
   - Yes → advance past `Not`, expect `Set` → `IsNotSetExpression`  
   - No → expect `Set` → `IsSetExpression`

This code remains UNCHANGED. The `ByTokenSequence` lookup is used by **post-parse consumers** (type checker, evaluator, MCP tool, LS hover) that want to retrieve catalog metadata for an operator they've already identified by its AST node type. For example:

```csharp
// Type checker or evaluator looking up IsSet metadata:
var meta = Operators.ByTokenSequence(TokenKind.Is, TokenKind.Set);
// → returns the MultiTokenOp for IsSet

// Or for IsNotSet:
var meta = Operators.ByTokenSequence(TokenKind.Is, TokenKind.Not, TokenKind.Set);
```

The Pratt dispatch loop does **not** need to call `ByTokenSequence` — it already identifies the operator by explicit token consumption. `ByTokenSequence` is a catalog lookup API for consumers downstream of parsing.

**`ByToken` (single-token) stays unchanged in shape** — after the `OfType<SingleTokenOp>()` filter added in Work Item A, it correctly contains only the 18 single-token operators.

---

## Work Item C: PRECEPT0019 → Error

### Prerequisites (must complete first)

1. **`ExpressionFormKind.PostfixOperation = 11` added** (Work Item A) — adds a new catalog member.
2. **`[HandlesForm(ExpressionFormKind.PostfixOperation)]` annotation added to `ParseExpression`** (Work Item A) — covers the new member.
3. **PRECEPT0019 fires zero times on a clean build** — verify: `dotnet build src/Precept/` with no warnings.

### Verify No Other Uncovered Members

Current annotation coverage in `Parser.cs`:

| Form | Annotated on |
|------|-------------|
| Literal | `ParseAtom` |
| Identifier | `ParseAtom` |
| Grouped | `ParseAtom` |
| BinaryOperation | `ParseExpression` |
| UnaryOperation | `ParseAtom` |
| MemberAccess | `ParseExpression` |
| Conditional | `ParseAtom` |
| FunctionCall | `ParseAtom` |
| MethodCall | `ParseExpression` |
| ListLiteral | `ParseAtom` |
| PostfixOperation | `ParseExpression` (added in Work Item A) |

All 11 members covered. No additional gaps.

**Other pipeline classes** (`TypeChecker`, `GraphAnalyzer`, `Evaluator`) are currently NOT annotated with `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]`. They are unimplemented stubs. Do NOT add the class marker to them yet — it would immediately fire PRECEPT0019 for all 11 forms on each stub class. Add the marker to each pipeline class only when that class is being implemented.

### Exact Changes

**`src/Precept.Analyzers/Precept0019PipelineCoverageExhaustiveness.cs`**:  
Line 29 — change severity:
```csharp
// Before:
defaultSeverity: DiagnosticSeverity.Warning,
// After:
defaultSeverity: DiagnosticSeverity.Error,
```

**`src/Precept/Precept.csproj`**:  
Remove the comment and `WarningsNotAsErrors` entry (2 lines):
```xml
<!-- REMOVE these two lines: -->
<!-- PRECEPT0019 fires for known unimplemented expression forms (GAP-6/GAP-7) — informational, not blocking -->
<WarningsNotAsErrors>PRECEPT0019</WarningsNotAsErrors>
```

The `TreatWarningsAsErrors>true</TreatWarningsAsErrors>` remains. The removed `WarningsNotAsErrors` exemption was the safety valve for when PRECEPT0019 was a Warning. Now that it's a native Error, the exemption is moot and the comment is misleading.

### Sequence

1. Complete Work Items A (DU + PostfixOperation + [HandlesForm] annotation).
2. Run `dotnet build src/Precept/` — confirm zero PRECEPT0019 warnings.
3. Apply the two changes above.
4. Run `dotnet build src/Precept/` — confirm zero errors and build passes.
5. Run `dotnet test test/Precept.Tests/` — confirm all tests pass.

---

## Work Item D: GAP-A — when-guard on StateEnsure/EventEnsure

### What the Gap Is

The sample files contain:
```
in Approved ensure DecisionNote is set when FraudFlag because "..."
in UnderReview ensure CreditScore >= 300 when DocumentsVerified because "..."
```

The `when Guard` clause appears **between the ensure condition and the `because` message**. The parser's `ParseStateEnsure` and `ParseEventEnsure` call `Expect(TokenKind.Because)` immediately after `ParseExpression(0)` — no post-condition `when` handling. `when` is in `StructuralBoundaryTokens`, so `ParseExpression` correctly stops at it, then `Expect(TokenKind.Because)` emits `ExpectedBecause` because the current token is `when`.

### AST Nodes — No Change Needed

Both `StateEnsureNode` and `EventEnsureNode` ALREADY carry `Expression? Guard`:

```csharp
// StateEnsureNode — existing shape (no change needed)
public sealed record StateEnsureNode(
    SourceSpan Span, Token Preposition, StateTargetNode State,
    Expression? Guard,     // ← already present
    Expression Condition, Expression Message) : Declaration(Span);

// EventEnsureNode — existing shape (no change needed)
public sealed record EventEnsureNode(
    SourceSpan Span, Token EventName,
    Expression? Guard,     // ← already present
    Expression Condition, Expression Message) : Declaration(Span);
```

The AST is already shaped for guards. Only the parser methods need updating.

### Parser Location and Fix

**`src/Precept/Pipeline/Parser.cs` — `ParseStateEnsure` (~line 419)**

```csharp
private StateEnsureNode ParseStateEnsure(SourceSpan start, Token preposition, StateTargetNode anchor, Expression? stashedGuard)
{
    Advance(); // consume 'ensure'
    var condition = ParseExpression(0);

    // Post-condition when-guard: `ensure Cond when Guard because "msg"`
    // stashedGuard is a pre-ensure guard (parsed before the 'ensure' keyword in the
    // dispatch flow). If no stashed guard exists, consume a post-condition when-guard here.
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

**`src/Precept/Pipeline/Parser.cs` — `ParseEventEnsure` (~line 544)**

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

**Risk: `BuildNode` dead-code arms**  
`BuildNode` has a `StateEnsure` arm and an `EventEnsure` arm (lines 1677-1704). Both construct the nodes with `null` for the guard. These dead-code arms have `Guard: null` hardcoded — this is NOT a compilation problem (null is already valid for `Expression?`). No change needed to `BuildNode`. The guard is wired from the live parse path (`ParseStateEnsure`/`ParseEventEnsure`), not from the slot-based dead path.

**Spec update**: Update `docs/language/precept-language-spec.md` to document:
- `(in|to|from) State ensure Condition [when Guard] because "msg"`
- `on Event ensure Condition [when Guard] because "msg"`

### Test Coverage

Add to `test/Precept.Tests/ParserTests.cs` (new section or existing StateEnsure section):

```csharp
[Fact]
public void Parse_StateEnsure_WithPostConditionWhenGuard()
// Input: "precept X\nstate Active initial\nin Active ensure amount > 0 when flagged because \"msg\""
// Assert: StateEnsureNode.Guard = IdentifierExpression("flagged")
// Assert: StateEnsureNode.Condition = BinaryExpression(amount > 0)
// Assert: zero error-severity diagnostics

[Fact]
public void Parse_StateEnsure_WithoutWhenGuard_Regression()
// Input: "precept X\nstate Active initial\nin Active ensure amount > 0 because \"msg\""
// Assert: StateEnsureNode.Guard = null
// Assert: zero diagnostics

[Fact]
public void Parse_EventEnsure_WithPostConditionWhenGuard()
// Input: "precept X\nevent Submit(Amount as number)\non Submit ensure Submit.Amount > 0 when active because \"msg\""
// Assert: EventEnsureNode.Guard = IdentifierExpression("active")
// Assert: zero diagnostics

[Fact]
public void Parse_EventEnsure_WithoutWhenGuard_Regression()
// Same as existing test — Guard = null, zero diagnostics
```

**SampleFileIntegrationTests.cs update**: Remove `insurance-claim.precept` and `loan-application.precept` from `KnownBrokenFiles`. Update count assertions: 7 → 5 known broken files.

---

## Work Item E: GAP-B — Modifiers after computed expressions

### What the Gap Is

```
field LineTotal as number -> TaxableAmount + TaxAmount nonnegative
field Net as number -> Total - Tax - Fee positive
```

The field syntax `field Name as Type -> Expr Modifier` has modifiers AFTER the `->` expression. The slot system for `FieldDeclaration` processes slots in fixed order: `[0] IdentifierList → [1] TypeExpression → [2] ModifierList → [3] ComputeExpression`. The modifier slot runs BEFORE the expression slot. After `ParseComputeExpression` returns, any trailing modifier tokens are unconsumed, and the top-level dispatch loop emits `ExpectedDeclarationKeyword` for each one.

### Fix: Dedicated ParseFieldDeclaration

Override `ParseFieldDeclaration` to bypass the generic slot system and collect modifiers both before and after the `->` expression:

**`src/Precept/Pipeline/Parser.cs` — replace `ParseFieldDeclaration`**:

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

    var lastSpan = postModifiers.Length > 0 ? postModifiers[^1].Span
                 : computed?.Span
                 : preModifiers.Length > 0 ? preModifiers[^1].Span
                 : type.Span;

    return new FieldDeclarationNode(
        SourceSpan.Covering(start, lastSpan),
        nameTokens, type, allModifiers, computed);
}
```

**Important**: The existing slot-based code calls `Expect(TokenKind.As)` implicitly through `ParseTypeExpression`. The dedicated implementation must call `Expect(TokenKind.As)` explicitly before `ParseTypeRef()`. Verified: `ParseTypeRef()` does NOT consume `As` — `ParseTypeExpression` does that. The dedicated method owns that step.

**`FieldDeclarationNode`** — no structural change. The record already has `ImmutableArray<FieldModifierNode> Modifiers` and `Expression? ComputedExpression`. Order of modifiers in the combined array is: pre-expression modifiers first, post-expression modifiers appended — this matches the lexical order in the source text.

**`BuildNode` dead-code arm** — still uses `slots[2]?.AsFieldModifiers()` which returns `[]` when slot[2] is null. The dead path won't be reached by live parses after this change (ParseFieldDeclaration now builds directly). No change needed.

### Test Coverage

Add to `test/Precept.Tests/ParserTests.cs` (FieldDeclaration section):

```csharp
[Fact]
public void Parse_FieldDeclaration_ModifierAfterComputedExpression()
// Input: "precept X\nfield Net as number -> Total - Tax positive"
// Assert: FieldDeclarationNode.ComputedExpression = BinaryExpression(...)
// Assert: FieldDeclarationNode.Modifiers.Single() = FlagModifierNode(positive)
// Assert: zero error-severity diagnostics

[Fact]
public void Parse_FieldDeclaration_MultipleTrailingModifiers()
// Input: "precept X\nfield X as number -> expr nonnegative writable"
// Assert: Modifiers.Length == 2 (nonnegative, writable)
// Assert: zero diagnostics

[Fact]
public void Parse_FieldDeclaration_PreAndPostModifiers()
// Input: "precept X\nfield X as number optional -> expr positive"
// Assert: Modifiers = [optional, positive] in order
// Assert: zero diagnostics

[Fact]
public void Parse_FieldDeclaration_PreModifiersOnly_Regression()
// Input: "precept X\nfield X as number nonnegative writable"
// Assert: Modifiers = [nonnegative, writable], ComputedExpression = null
// Assert: zero diagnostics
```

**SampleFileIntegrationTests.cs update**: Remove `sum-on-rhs-rule.precept`, `invoice-line-item.precept`, `transitive-ordering.precept`, `travel-reimbursement.precept` from `KnownBrokenFiles`. Update count: 5 → 1 known broken (after GAP-A also applied), or 7 → 3 (if applied before GAP-A).

---

## Work Item F: GAP-C — Keyword-as-member-name

### What the Gap Is

```
-> set LowestRequestedFloor = RequestedFloors.min
-> set HighestRequestedFloor = RequestedFloors.max
```

`min` and `max` are tokenized as `TokenKind.Min` (55) and `TokenKind.Max` (56) — constraint keywords. The `Dot` handler in `ParseExpression` calls:

```csharp
var member = Expect(TokenKind.Identifier);
```

`Expect(TokenKind.Identifier)` emits `ExpectedToken("identifier", "min")` because the current token is `TokenKind.Min`, not `TokenKind.Identifier`.

**Scope check**: Other member accessors in the sample files — `.count`, `.length`, `.sum` — are NOT keywords in `TokenKind`. They lex as `TokenKind.Identifier` and work correctly. Only `TokenKind.Min` and `TokenKind.Max` are affected.

**Scope of fix**: Member access position only (`Dot` handler in `ParseExpression`). No change to field declarations, argument lists, or other identifier positions — those don't use keywords as names in current samples. This is additive; no existing behavior changes.

### Fix: Helper Method in ParseSession

**`src/Precept/Pipeline/Parser.cs`** — add to `ParseSession`:

```csharp
/// <summary>
/// Token kinds that are keywords but are also valid as collection accessor member names
/// (e.g. <c>.min</c>, <c>.max</c>). These appear after <c>.</c> in member-access position
/// and must be accepted where the grammar specifies an identifier.
/// </summary>
private static readonly FrozenSet<TokenKind> KeywordsValidAsMemberName =
    new[] { TokenKind.Min, TokenKind.Max }.ToFrozenSet();
```

Add helper method:

```csharp
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

**Update the `Dot` handler in `ParseExpression`**:

```csharp
if (current.Kind == TokenKind.Dot)
{
    if (minPrecedence > 80) break;
    Advance(); // consume '.'
    var member = ExpectIdentifierOrKeywordAsMemberName();  // ← was: Expect(TokenKind.Identifier)
    left = new MemberAccessExpression(
        SourceSpan.Covering(left.Span, member.Span), left, member);
    continue;
}
```

**Why `FrozenSet` not inline token list**: The `KeywordsValidAsMemberName` set is catalog-derived knowledge — it could be driven by a `TokenTrait.ValidAsMemberName` flag on `TokenMeta` in the future. The `FrozenSet` here is a parser-level structural decision, not a hardcoded list scattered through parse logic. This follows the catalog-system.md principle: derive vocabulary; don't hardcode.

**Why not expand globally**: The fix is intentionally scoped to member-access position. Allowing keywords as field names in declarations would open a different can of worms (field named `min` could shadow the built-in). Keep it surgical.

### Test Coverage

Add to `test/Precept.Tests/ExpressionParserTests.cs`:

```csharp
[Fact]
public void Parse_MemberAccess_MinKeywordAsMemberName()
// Input: "items.min"
// Assert: MemberAccessExpression with Object = IdentifierExpression("items")
//         Member.Kind == TokenKind.Identifier, Member.Text == "min"
// Assert: zero diagnostics

[Fact]
public void Parse_MemberAccess_MaxKeywordAsMemberName()
// Input: "items.max"
// Assert: MemberAccessExpression, Member.Text == "max"
// Assert: zero diagnostics
```

**SampleFileIntegrationTests.cs update**: Remove `building-access-badge-request.precept` from `KnownBrokenFiles`. After all three gap fixes (A + B + C), `KnownBrokenFiles` is empty. Update count assertions: 7 → 0 known broken, 21 → 28 clean files.

---

## Additional Gaps Discovered During Review

### AG-1: `because` return value unused in ParseStateEnsure/ParseEventEnsure

Both `ParseStateEnsure` and `ParseEventEnsure` (original code) have:
```csharp
var because = Expect(TokenKind.Because);
```
The `because` variable is never used — only the side-effect of advancing past the token matters. C# issues a warning for unused locals in strict mode. The GAP-A fix removes `var because =` (just calls `Expect(TokenKind.Because)` directly). Minor cleanup, not a gap.

### AG-2: `OperatorPrecedence` construction after DU — `Arity.Postfix` exclusion

Current filter: `.Where(op => op.Arity == Arity.Binary)`. After adding `Arity.Postfix`, this filter still works correctly — postfix ops are excluded from `OperatorPrecedence` because they aren't binary. No change needed, but worth noting explicitly so no one "fixes" it by adding a `!= Arity.Postfix` clause.

### AG-3: `Tokens.GetMeta(TokenKind.Not)` needed for `IsNotSet` MultiTokenOp entry

The `IsNotSet` catalog entry needs `Tokens.GetMeta(TokenKind.Not)` as the second element. Verify that `Tokens.GetMeta` has a `Not` entry (it does — `TokenKind.Not` = 40 is a keyword in the token catalog). No gap, just a pre-flight check before writing the arm.

### AG-4: `ByToken_CountMatchesAll` test will need updating after Work Item A

This test currently asserts `ByToken.Count == All.Count` (18 == 18). After Work Item A:
- `All.Count` = 20 (adds IsSet, IsNotSet)
- `ByToken.Count` = 18 (MultiTokenOp entries excluded by `.OfType<SingleTokenOp>()` filter)
The test must be updated to: `ByToken.Count == All.OfType<SingleTokenOp>().Count()` or equivalently `ByToken.Count == 18`.

### AG-5: `All_CountMatchesEnumValues` test — passes automatically

Adding IsSet=19 and IsNotSet=20 to `OperatorKind` auto-grows `Enum.GetValues<OperatorKind>()`. The test asserts that `All.Count == Enum.GetValues<OperatorKind>().Length`. This invariant is preserved automatically. No change needed.

---

## Implementation Cost Summary

| Item | Complexity | Files Changed | Key Risk |
|------|-----------|--------------|---------|
| **A: Option B Full DU** | Medium | `Operator.cs` (rewrite), `OperatorKind.cs` (+2 members), `Operators.cs` (18 arm changes + 2 new + ByToken filter), `Parser.cs` (~line 38: +OfType, ~line 1334: +HandlesForm), `ExpressionForms.cs` (+1 enum member + GetMeta arm), `OperatorsTests.cs` (3 test updates) | 18 GetMeta arm changes — mechanical but high surface area; BuildNode dead-code arms don't need changes |
| **B: ByToken Multi-Token Key** | Low | `Operators.cs` (+`_byTokenSequence` field + `BuildSequenceKey` helper + `ByTokenSequence` method) — same file as A | None; the method is purely additive |
| **C: PRECEPT0019 → Error** | Low | `Precept0019PipelineCoverageExhaustiveness.cs` (1 line), `Precept.csproj` (remove 2 lines) | Must complete A first — flip with any missing [HandlesForm] → build error |
| **D: GAP-A** | Low | `Parser.cs` (2 method bodies), `ParserTests.cs` (+4 tests), `SampleFileIntegrationTests.cs` (remove 2 files from KnownBrokenFiles, update 2 count assertions) | Verify both `ParseStateEnsure` AND `ParseEventEnsure` are updated |
| **E: GAP-B** | Medium | `Parser.cs` (rewrite `ParseFieldDeclaration`), `ParserTests.cs` (+4 tests), `SampleFileIntegrationTests.cs` (remove 4 files, update counts) | Must call `Expect(TokenKind.As)` explicitly in dedicated method; BuildNode dead arm still references slots but is unreachable — leave it |
| **F: GAP-C** | Low | `Parser.cs` (+1 FrozenSet, +1 helper method, 1 line in Dot handler), `ExpressionParserTests.cs` (+2 tests), `SampleFileIntegrationTests.cs` (remove 1 file, update counts) | Keep scope surgical — member-access only, not all identifier positions |

**Total file changes across all items**: 9 source files, 3 test files (some modified multiple times across items).

**Recommended slice order**:
1. D (GAP-A) — independent of catalog changes, zero-risk parser fix, unblocks 2 sample files
2. E (GAP-B) — independent of catalog changes, dedicated method, unblocks 4 sample files
3. F (GAP-C) — trivial additive change, unblocks 1 sample file. After F: `KnownBrokenFiles` is empty, `SampleFileIntegrationTests` fully green
4. A+B — DU refactor, new catalog entries. Large surface but all mechanical
5. C — flip severity, the final seal after A+B confirm PRECEPT0019 is green
