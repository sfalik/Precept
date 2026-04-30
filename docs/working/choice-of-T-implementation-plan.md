# Plan: `choice of T` Implementation

> **Spike branch: `spike/Precept-V2`** — no PR, no branch management. Commit directly to the spike. Build green and tests passing are the only gates.

## Problem

The current `choice(...)` type is string-only, has no type identity beyond its declaration node, and no mechanism for event arg supply. Design decisions locked in the 2026-04-29 session require a full redesign of the choice type grammar, AST, catalogs, and type rules.

## Locked Design Decisions

- `choice(...)` without `of T` → parse error (`ChoiceMissingElementType`) — no implicit string default, no backward compat
- All choice declarations must use `choice of T(...)` explicitly
- Supported element types: `string`, `integer`, `decimal`, `number`, `boolean` — validated at parse time
- Choice values must be literals only (no expressions) — enforced by dedicated `ParseChoiceValue()` in parser
- Subset subtype assignment: `choice(A) <: choice(B)` iff `A ⊆ B`
- Order-preserving subsequence rule for ordered choice comparison
- Declaration-order rank universally for all typed choice
- `ordered` disallowed on `choice of boolean`
- `set ChoiceField = stringVariable` → `NonChoiceAssignedToChoice`
- 6 new diagnostic codes replacing generic `TypeMismatch` for choice violations

## Note on Backward Compatibility

Frank's earlier analysis proposed `choice("a","b")` as shorthand for `choice of string("a","b")`. Shane explicitly overrode this: the explicit `of T` form is required with no fallback. This is a breaking change. All samples and docs using bare `choice(...)` are migrated in Slice 0 before any runtime changes.

## Approach

Vertical slices. Samples/docs migration first (Slice 0), then AST, parser, catalogs, and tests.

---

## Slices

### Slice 0: Sample + Doc Migration (prerequisite)

Migrate all existing `choice(...)` usages to `choice of string(...)` before any runtime changes so the build stays green throughout.

**Files:**
- `samples/customer-profile.precept` line 14: `choice("email","phone","sms")` → `choice of string("email","phone","sms")`
- `samples/it-helpdesk-ticket.precept` line 12: `choice("Low","Medium","High","Critical")` → `choice of string("Low","Medium","High","Critical")`
- `docs/compiler/parser.md` ~lines 634–638: update choice grammar reference
- `docs/language/primitive-types.md` lines 214–215, 224–225: update choice usage examples
- `docs/language/business-domain-types.md` line 1473: update choice reference
- `docs/language/collection-types.md` lines 78, 81, 235, 468, 600, 764, 874: update all `choice(...)` occurrences
- `docs/language/temporal-type-system.md` line 1471: update choice reference

---

### Slice 1: Docs (spec + collection-types)

**Files:**
- `docs/language/precept-language-spec.md`
  - Grammar: replace `ChoiceType := choice "(" StringExpr+ ")"` with:
    ```
    ChoiceType        := choice "of" ChoiceElementType "(" ChoiceValueExpr ("," ChoiceValueExpr)* ")"
    ChoiceElementType := string | integer | decimal | number | boolean
    ChoiceValueExpr   := StringLiteral | IntegerLiteral | DecimalLiteral | NumberLiteral | BooleanLiteral
    ```
  - Operator compatibility table (~line 1127): update choice row for element-type-aware comparison
  - Diagnostics table: add 6 new diagnostic entries (codes 85–90)
  - Usage examples: update all `choice(...)` to `choice of string(...)`
- `docs/language/collection-types.md` — update choice section with new syntax

---

### Slice 2: AST

**File:** `src/Precept/Pipeline/SyntaxNodes/TypeRefNode.cs`

Update `ChoiceTypeRefNode` with required `ElementType` token:

```csharp
/// <summary><c>as choice of string("A", "B", "C")</c></summary>
public sealed record ChoiceTypeRefNode(
    SourceSpan Span,
    Language.Token? ElementType,    // null only on parse-error recovery; otherwise string | integer | decimal | number | boolean
    ImmutableArray<Expression> Options) : TypeRefNode(Span);
```

`ElementType` is `Token?` — null only in the error recovery node. Downstream consumers must null-check. Remove legacy doc comment referencing old space-separated form.

---

### Slice 3: Negative Literal Folding

**Files:** `src/Precept/Pipeline/Parser.cs`, `docs/language/precept-language-spec.md`, `src/Precept/Language/SyntaxReference.cs`, `test/Precept.Tests/ExpressionParserTests.cs`

In `ParseAtom` (lines ~1255–1261), the `Minus` prefix arm currently always produces `UnaryExpression(Negate, operand)`. Add a constant-fold branch: when the operand is a `NumberLiteral`, collapse immediately to a signed `LiteralExpression`:

```csharp
case TokenKind.Minus:
{
    var op = Advance();
    var operand = ParseExpression(65); // negate precedence
    if (operand is LiteralExpression { Value.Kind: TokenKind.NumberLiteral } lit)
    {
        var negText = lit.Value.Text!.StartsWith('-')
            ? lit.Value.Text[1..]        // --1 → "1"
            : "-" + lit.Value.Text;      // -1  → "-1"
        var span = SourceSpan.Covering(op.Span, lit.Span);
        return new LiteralExpression(span, new Token(TokenKind.NumberLiteral, negText, span));
    }
    return new UnaryExpression(SourceSpan.Covering(op.Span, operand.Span), op, operand);
}
```

`x - 1` (binary minus) is unaffected — binary path is a separate Pratt infix loop. `-x`, `-(a+b)` remain `UnaryExpression` — fold only fires on `LiteralExpression(NumberLiteral)` operands.

**`docs/language/precept-language-spec.md` lines 459–466:** Remove the restriction note ("No leading `+` or `-`..."). Update grammar:
```
NumberLiteral  :=  '-'? Digits ('.' Digits)? (('e' | 'E') ('+' | '-')? Digits)?
Examples: 0, 42, -1, 3.14, -3.14, 1.5e2, -1e5, 1e-5
```

**`src/Precept/Language/SyntaxReference.cs` line 23:** Update `NumberLiteralRules` to include `(-1, -0.5)` examples.

**`test/Precept.Tests/ExpressionParserTests.cs`:** Add 5 tests:
- `ParseExpression_NegativeIntegerLiteral` — `-1` → `LiteralExpression("-1")`
- `ParseExpression_NegativeDecimalLiteral` — `-3.14` → `LiteralExpression("-3.14")`
- `ParseExpression_NegativeExponentLiteral` — `-1e5` → `LiteralExpression("-1e5")`
- `ParseExpression_UnaryMinusOnIdentifierRemainsUnary` — `-x` → `UnaryExpression`
- `ParseExpression_BinaryMinusNotAffected` — `x - 1` → `BinaryExpression`

---

### Slice 4: Parser

**File:** `src/Precept/Pipeline/Parser.cs` (lines ~1073–1096)

Add `ChoiceElementTypeKeywords` static set: `{ StringType, IntegerType, DecimalType, NumberType, BooleanType }`.

Replace current choice parsing:

```csharp
if (current.Kind == TokenKind.ChoiceType)
{
    var choiceToken = Advance();

    // 'of' is required — bare choice(...) emits ChoiceMissingElementType
    if (!Match(TokenKind.Of))
    {
        AddDiagnostic(DiagnosticCode.ChoiceMissingElementType, choiceToken.Span);
        // error recovery: consume through matching ')' to avoid cascade diagnostics
        ConsumeThrough(TokenKind.RightParen);
        return new ChoiceTypeRefNode(choiceToken.Span, null, ImmutableArray<Expression>.Empty);
    }

    // element type must be one of the 5 allowed keywords
    var elemToken = Current();
    if (!ChoiceElementTypeKeywords.Contains(elemToken.Kind))
    {
        AddDiagnostic(DiagnosticCode.ExpectedToken, elemToken.Span, "string, integer, decimal, number, or boolean");
        ConsumeThrough(TokenKind.RightParen);
        return new ChoiceTypeRefNode(choiceToken.Span, null, ImmutableArray<Expression>.Empty);
    }
    Advance(); // consume validated element type

    var options = ImmutableArray.CreateBuilder<Expression>();
    Expect(TokenKind.LeftParen);
    if (Current().Kind == TokenKind.RightParen)
    {
        AddDiagnostic(DiagnosticCode.EmptyChoice, Current().Span); // parse-time rejection
    }
    else
    {
        do { options.Add(ParseChoiceValue(elemToken.Kind)); }  // literal-only
        while (Match(TokenKind.Comma));
    }
    Expect(TokenKind.RightParen);
    var lastSpan = options.Count > 0 ? options[^1].Span : elemToken.Span;
    return new ChoiceTypeRefNode(SourceSpan.Covering(choiceToken.Span, lastSpan), elemToken, options.ToImmutable());
}
```

**`ParseChoiceValue(TokenKind elementType)` rules:**
- `string` element type → expects `StringLiteral`
- `integer`, `decimal`, `number` element types → all accept `NumberLiteral` (the lexer does not emit distinct `IntegerLiteral`/`DecimalLiteral` — subtype discrimination is deferred to the type stage)
- `boolean` element type → expects `True` or `False` keyword tokens
- Wrong token kind → emits `ChoiceElementTypeMismatch` (code 88, NOT generic `TypeMismatch`)
- Negative numeric values (e.g., `-1`) require a unary minus expression and are **not supported in v1** — document this in spec

**v1 scope note:** `set of choice of string(...)` (typed choice nested inside a collection type) is out of scope. Collection element types are still `Token`-based; making them `TypeRefNode` is a separate future slice.

Remove space-separated form (`choice "A" "B" "C"`) entirely — no sample or test uses it.

---

### Slice 4: Catalogs

**File:** `src/Precept/Language/DiagnosticCode.cs` — add 6 codes after `SqrtOfNegative = 84`:

```csharp
// ── Type (choice) ──────────────────────────────────────────────
NonChoiceAssignedToChoice          =  85,
ChoiceLiteralNotInSet              =  86,
ChoiceArgOutsideFieldSet           =  87,
ChoiceElementTypeMismatch          =  88,
ChoiceRankConflict                 =  89,
ChoiceMissingElementType           =  90,
```

**File:** `src/Precept/Language/Diagnostics.cs` — add metadata for each:
- Use Elaine's message templates from `.squad/decisions/inbox/elaine-choice-diagnostic-ux.md`
- `ChoiceMissingElementType` → `DiagnosticStage.Parse`
- Others → `DiagnosticStage.Type`

**File:** `src/Precept/Language/Types.cs` (~line 161) — update Choice entry:
- Description: `"Enumerated value set with explicit element type"`
- HoverDescription: updated to reflect `choice of T(...)` form and sealed-type semantics
- UsageExample: `"field Priority as choice of string(\"Low\",\"Medium\",\"High\") ordered default \"Low\""`

---

### Slice 5: Tests

**File:** `test/Precept.Tests/SlotParserTests.cs`
- Update existing test: input `choice of string("A","B")` → assert `ElementType.Kind == TokenKind.StringType`
- Add: `choice of integer(0, 404, 500)` → `ElementType.Kind == IntegerType`, values parsed as `NumberLiteral`
- Add: `choice of decimal(0.0, 0.05)` → `ElementType.Kind == DecimalType`, values parsed as `NumberLiteral`
- Add: `choice of number(1.5, 2.5)` → `ElementType.Kind == NumberType`, values parsed as `NumberLiteral`
- Add: `choice of boolean(true, false)` → `ElementType.Kind == BooleanType`
- Add: bare `choice("A","B")` → exactly one diagnostic `ChoiceMissingElementType`, no cascade diagnostics
- Add: `choice of set("A")` → parse diagnostic (invalid element type), no cascade
- Add: `choice of integer("not-an-int")` → `ChoiceElementTypeMismatch` (not `TypeMismatch`)
- Add: `choice of string()` → `EmptyChoice` diagnostic

**File:** `test/Precept.Tests/DiagnosticsTests.cs`
- 6 new codes exist, have metadata, numbers unique/sequential
- Add `ChoiceMissingElementType` to the `ParseCodes` stage-group list
- Add `NonChoiceAssignedToChoice`, `ChoiceLiteralNotInSet`, `ChoiceArgOutsideFieldSet`, `ChoiceElementTypeMismatch`, `ChoiceRankConflict` to the `TypeCodes` stage-group list

---

## File Inventory

| File | Change |
|---|---|
| `samples/customer-profile.precept` | `choice(...)` → `choice of string(...)` |
| `samples/it-helpdesk-ticket.precept` | `choice(...)` → `choice of string(...)` |
| `docs/compiler/parser.md` | Update choice grammar reference |
| `docs/language/primitive-types.md` | Update choice usage examples (lines 214–215, 224–225) |
| `docs/language/business-domain-types.md` | Update choice reference (line 1473) |
| `docs/language/collection-types.md` | Update all `choice(...)` occurrences (lines 78, 81, 235, 468, 600, 764, 874) |
| `docs/language/temporal-type-system.md` | Update choice reference (line 1471) |
| `docs/language/precept-language-spec.md` | Grammar BNF, operator table, diagnostics table, examples; note v1 negative-literal and collection-nesting limits |
| `src/Precept/Pipeline/SyntaxNodes/TypeRefNode.cs` | `ChoiceTypeRefNode` + `ElementType Token?` field |
| `src/Precept/Pipeline/Parser.cs` | Require `of T`, validate element type, `ParseChoiceValue()`, safe recovery, `EmptyChoice` at parse time |
| `src/Precept/Language/DiagnosticCode.cs` | 6 new codes (85–90) |
| `src/Precept/Language/Diagnostics.cs` | 6 new metadata entries; `ChoiceMissingElementType` = Parse stage |
| `src/Precept/Language/Types.cs` | Updated description/hover/example |
| `test/Precept.Tests/SlotParserTests.cs` | Update existing + add 9 new tests (incl. cascade + empty) |
| `test/Precept.Tests/DiagnosticsTests.cs` | 6 new codes + stage-group list updates |

**Explicitly out of scope (v1):**
- Negative numeric choice values (e.g., `choice of integer(-1, 0, 1)`) — unary minus is an expression, not a literal; document the limitation in the spec
- `set of choice of string(...)` — collection element types are `Token`-based; nesting typed choice inside a collection requires a separate AST/parser slice

---

## Dependency Order

```
Slice 0 (samples migration)
  ├── Slice 1 (docs)
  ├── Slice 2 (AST)
  │     └── Slice 3 (parser)
  └── Slice 4 (catalogs)
        └── Slice 5 (tests) ← also depends on 2, 3
```

Slices 1, 2, 4 can proceed in parallel after Slice 0 lands.

---

## Regression Anchors

- `dotnet build src/Precept/` green after every slice
- `dotnet test test/Precept.Tests/` green after every slice
- No skipped tests added
- All existing choice parser tests updated, not deleted