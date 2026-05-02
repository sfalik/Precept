# Radical Parser Design: The Catalog IS the Grammar

**Author:** Frank (Lead/Architect)  
**Date:** 2026-05-03  
**Status:** Design sketch — input for rebuild decision and type-checker radical design  
**Builds on:** `frank-catalog-driven-parser-review.md` §§5.4–5.5, `frank-parser-rebuild-reassessment.md`

---

## 1. The Bet

The existing parser is built on one implicit assumption: that a parser is a collection of methods, one per grammar production. That assumption is correct for languages like C# or TypeScript, where productions have heterogeneous recursive structure. It is wrong for Precept.

Precept's grammar is not recursive and heterogeneous. It is **flat, keyword-anchored, and slot-sequential**. Every top-level construct begins with a fixed leading token. Every construct body is a fixed sequence of typed slots. The entire grammar fits in a table. The catalog already *is* that table.

The bet: replace the ~1,800-line three-file parser with a ~650-line single-file PEG rule interpreter. The catalog describes grammar; the parser executes it. No per-construct parse methods. No per-action-kind switches. No `DisambiguateAndParse` hand-coded dispatch. One interpreter, all constructs.

This is not theoretical. The `ParseConstructSlots` + `InvokeSlotParser` pattern in the current parser already proves the architecture is viable — it handles `state`, `event`, and `rule` declarations generically today. The radical design extends that principle to *everything*.

---

## 2. Core Model: The ParseRule Combinator

The fundamental abstraction is a small, sealed combinator vocabulary called `ParseRule`. Each `ConstructMeta` carries a `ParseRule Grammar` field. The interpreter takes a grammar and a cursor and produces a tagged result bag.

### 2.1 The Combinator Vocabulary

```csharp
// ParseRule is a discriminated union (abstract sealed class hierarchy)
abstract sealed class ParseRule { }

// --- Terminals ---
sealed class ConsumeLeader()       : ParseRule  // consume the construct's leading keyword
sealed class Consume(TokenKind K)  : ParseRule  // consume exactly this token (assert it's there)
sealed class Expect(TokenKind K)   : ParseRule  // expect + error-recover if absent
sealed class ConsumeIdent()        : ParseRule  // consume identifier (with required-guard)

// --- Structure ---
sealed class Seq(ParseRule[] Rules)             : ParseRule  // ordered sequence
sealed class Opt(ParseRule Rule)                : ParseRule  // optional — never fails
sealed class Rep(ParseRule Rule)                : ParseRule  // zero or more
sealed class Alt(ParseRule[] Rules)             : ParseRule  // first match wins

// --- Named Slots (produce tagged nodes in result bag) ---
sealed class Tag(string Name, ParseRule Rule)   : ParseRule  // run Rule, store result under Name

// --- Typed Productions (invoke sub-parsers, produce typed nodes) ---
sealed class TypeRefProd()                      : ParseRule  // → TypeRefNode
sealed class ExprProd()                         : ParseRule  // → Expression
sealed class ModifiersProd()                    : ParseRule  // → ImmutableArray<FieldModifierNode>
sealed class StateEntriesProd()                 : ParseRule  // → ImmutableArray<StateEntryNode>
sealed class ActionChainProd()                  : ParseRule  // → ImmutableArray<Statement>
sealed class OutcomeProd()                      : ParseRule  // → OutcomeNode
sealed class IdentListProd()                    : ParseRule  // → ImmutableArray<Token>
sealed class ArgListProd()                      : ParseRule  // → ImmutableArray<ArgumentNode>
sealed class FieldTargetProd()                  : ParseRule  // → FieldTargetNode
sealed class AccessModeKeywordProd()            : ParseRule  // → Token

// --- Family/Disambiguation ---
sealed class FamilyDispatch(ConstructFamily Family) : ParseRule  // anchor + float + disambiguate
```

The parser has exactly one method that matters: `object? Interpret(ParseRule rule, Cursor cursor, ResultBag bag)`. Everything flows through it. No per-construct code.

### 2.2 Example: FieldDeclaration Grammar

Before the design, `ParseFieldDeclaration` was 40 lines of handwritten sequential logic with special-case split-modifier handling. As a grammar rule it becomes:

```csharp
// ConstructMeta for FieldDeclaration:
Grammar = Seq(
    ConsumeLeader(),                          // 'field'
    Tag("names", IdentListProd()),            // Name, Name2, ...
    Consume(TokenKind.As),
    Tag("type",  TypeRefProd()),              // as Type [qualifiers]
    Tag("mods1", ModifiersProd()),            // pre-expression modifiers (zero or more)
    Opt(Seq(
        Consume(TokenKind.Arrow),
        Tag("expr",  ExprProd()),             // -> ComputedExpression
        Tag("mods2", ModifiersProd())         // post-expression modifiers (zero or more)
    ))
)
```

`BuildNode` for `FieldDeclaration` reads `bag["names"]`, `bag["type"]`, concatenates `bag["mods1"] + bag["mods2"]`, and reads `bag["expr"]`. The split-modifier problem dissolves. The grammar says exactly what the syntax is; the interpreter executes it literally.

### 2.3 Example: StateEnsure Grammar (with stashed guard)

```csharp
// ConstructMeta for StateEnsure — body grammar only (anchor zone handled by FamilyDispatch):
Grammar = Seq(
    Consume(TokenKind.Ensure),
    Tag("condition", ExprProd()),
    Opt(Seq(Consume(TokenKind.When), Tag("guard", ExprProd()))),
    Expect(TokenKind.Because),
    Tag("message",   ExprProd())
)
```

The stashed guard from the anchor zone is injected by the `FamilyDispatch` interpreter before executing this body grammar (§4.1). `BuildNode` checks `bag["guard"]` for the anchor-zone stash first, then the body-level `Opt(When, ...)` — the stash wins if it was set.

---

## 3. Catalog Metadata Expansions

Three additions to the existing catalog. Everything else already exists.

### 3.1 `ParseRule Grammar` on `ConstructMeta`

```csharp
public sealed record ConstructMeta(
    ConstructKind Kind,
    string DisplayName,
    TokenKind LeadingToken,
    ImmutableArray<ConstructSlot> Slots,         // keep — used for documentation/tooling
    ImmutableArray<TokenKind>? DisambiguationTokens,
    ParseRule Grammar                            // NEW — the runtime grammar rule
)
```

The `Slots` list is kept for IDE tooling and documentation. `Grammar` is authoritative for the parser. Both say the same thing; `Grammar` is the executable form.

The grammar rules for all 12 constructs are declared once, inline, in `Constructs.cs`. Total: approximately 80 lines of combinator trees.

### 3.2 `ConstructFamily` — New Catalog Type

`ConstructFamily` replaces the hand-coded `DisambiguateAndParse` logic. One family per leading token that requires disambiguation.

```csharp
public sealed record ConstructFamily(
    TokenKind Leader,
    AnchorKind Anchor,              // StateAnchor | EventAnchor
    ParseRule? FloatingGuard,       // optional guard parseable before disambiguation token
    FrozenDictionary<TokenKind, ConstructKind> DisambiguationMap
)

public enum AnchorKind { StateAnchor, EventAnchor }
```

There are four families: `In` (StateAnchor), `To` (StateAnchor), `From` (StateAnchor), `On` (EventAnchor).

`FloatingGuard` for state families: `Seq(Consume(TokenKind.When), Tag("stashedGuard", ExprProd()))`.  
`FloatingGuard` for the `On` family: `null` — event handlers do not support a pre-event guard.

`DisambiguationMap` is computed from `Constructs.ByLeadingToken`, same as `FindDisambiguatedConstruct` today. No logic changes — just data that lives in the catalog instead of in `DisambiguateAndParse`.

Static initializer:
```csharp
public static class Families
{
    public static readonly ImmutableArray<ConstructFamily> All = [
        new ConstructFamily(
            Leader: TokenKind.In,
            Anchor: AnchorKind.StateAnchor,
            FloatingGuard: Seq(Consume(TokenKind.When), Tag("stashedGuard", ExprProd())),
            DisambiguationMap: FrozenDictionary.ToFrozenDictionary([
                (TokenKind.Modify,  ConstructKind.AccessMode),
                (TokenKind.Omit,    ConstructKind.OmitDeclaration),
                (TokenKind.Ensure,  ConstructKind.StateEnsure),
            ])
        ),
        // ... To, From, On families
    ];
    public static readonly FrozenDictionary<TokenKind, ConstructFamily> ByLeader = ...;
}
```

### 3.3 `ImmutableArray<ActionVariant> Variants` on `ActionMeta`

This resolves the variant-action detection gap without any mid-parse `meta.Kind` checks.

```csharp
public sealed record ActionVariant(
    VariantPeekPosition Position,  // BeforeValue or AfterValue
    TokenKind TriggerToken,        // the token that triggers the variant shape
    ActionSyntaxShape VariantShape // the shape to use if triggered
)

public enum VariantPeekPosition { BeforeValue, AfterValue }
```

**Variant declarations in `Actions.cs`:**

```
remove:   Variants = [ new(BeforeValue, TokenKind.At,  ActionSyntaxShape.RemoveAtIndex) ]
append:   Variants = [ new(AfterValue,  TokenKind.By,  ActionSyntaxShape.CollectionValueBy) ]
enqueue:  Variants = [ new(AfterValue,  TokenKind.By,  ActionSyntaxShape.CollectionValueBy) ]
dequeue:  Variants = [ new(AfterValue,  TokenKind.By,  ActionSyntaxShape.CollectionIntoBy) ]
```

All other actions: `Variants = []`.

The action shape interpreter reads `Variants` from the catalog — no per-kind branching anywhere in the parser.

### 3.4 `TypeParseShape` on `TypeMeta` (from review §3.3)

Already proposed in `frank-catalog-driven-parser-review.md`. Carried forward unchanged. Shrinks `ParseTypeRef` from 190 lines to ~45 lines of generic dispatch.

```csharp
public abstract sealed class TypeParseShape { }
sealed class ScalarShape()              : TypeParseShape
sealed class CISensitiveShape()         : TypeParseShape   // ~string
sealed class CollectionShape()          : TypeParseShape   // keyword of Element [qualifiers] [by P [dir]]
sealed class LookupShape()              : TypeParseShape   // lookup of K to V
sealed class ChoiceShape()              : TypeParseShape   // choice of ElemType (options...)
```

The per-type parse dispatch table is computed at startup:
```csharp
FrozenDictionary<TokenKind, TypeParseShape> TypeParseDispatch =
    Types.ByToken.ToFrozenDictionary(kvp => kvp.Key, kvp => kvp.Value.ParseShape);
```

---

## 4. The Three Design Gaps: Resolved

### 4.1 Gap 1: Stashed-Guard Pattern

**The problem**: In `DisambiguateAndParse`, the optional `when Guard` is consumed *before* the disambiguation token is seen. The grammar isn't left-to-right from the perspective of a flat slot sequence — the guard floats between the anchor and the disambiguation token. Per-construct slot iteration can't express this without knowing which construct it's in.

**The resolution: `FamilyDispatch` with `FloatingGuard`**.

The `FamilyDispatch` combinator is the only non-trivial part of the interpreter. Its execution sequence:

```
1. Consume leading token (in/to/from/on)
2. Parse anchor target per Family.Anchor:
     StateAnchor → ConsumeIdent or Consume(Any) → StateTargetNode → bag["anchor"]
     EventAnchor → ConsumeIdent → bag["anchor"]
3. If Family.FloatingGuard is non-null:
     Speculatively try to parse FloatingGuard:
       - Save cursor position
       - Attempt to parse Seq(Consume(When), ExprProd())
       - If the current token is When AND parsing succeeds: stash result in bag["stashedGuard"]
       - If no When token: skip (guard is absent), do not restore cursor (nothing was consumed)
4. Read Current().Kind → look up in Family.DisambiguationMap → ConstructKind
5. Retrieve body Grammar from ConstructMeta for that kind
6. Execute body Grammar with current bag (stash available under "stashedGuard")
```

Step 3 is deterministic: `When` is never a disambiguation token, so peeking for `When` has no ambiguity. If `When` is present, parse the guard expression. If not, skip. No speculative backtracking required — it's a one-token peek.

In `BuildNode` for `StateEnsure`, the guard is resolved as:
```
guard = bag.Get("stashedGuard") ?? bag.Get("guard")
```

The body grammar for `StateEnsure` attempts `Opt(When, guard)` for post-condition guards. The stash from `FamilyDispatch` pre-empts this. Both guard positions are expressed cleanly — one in the family definition, one in the body grammar — and assembly logic picks the stash if present.

**No `TryParseStashedGuard` method. No stash parameter passed through method signatures. The catalog describes where the guard goes; the interpreter handles the rest.**

### 4.2 Gap 2: Split-Modifier Problem

**The problem**: `ParseFieldDeclaration` runs `ParseFieldModifierNodes()` twice — once before `->` and once after. A pure left-to-right slot sequence produces two separate modifier arrays that must be concatenated. The split can't be expressed in a flat `Slots` list without a `SplitAroundSlot` meta-relationship.

**The resolution: PEG nesting makes it trivial.**

The field grammar (shown in §2.2) wraps the compute expression and post-modifiers in an `Opt(Seq(...))`. The two modifier positions are two separate `Tag("mods1", ...)` and `Tag("mods2", ...)` in the same grammar tree. The interpreter doesn't see a "split" — it sees a sequence with an optional tail that happens to contain another modifier run.

`BuildNode` for `FieldDeclaration`:
```csharp
var mods = bag.GetArray<FieldModifierNode>("mods1")
              .AddRange(bag.GetArray<FieldModifierNode>("mods2"));
```

That's the entire solution. The grammar expresses the syntax; the assembly step concatenates what it finds. **No special metadata relationship needed. No slot position tags. The problem disappears in the PEG model because nested structure is the native currency.**

### 4.3 Gap 3: Variant-Action Detection

**The problem**: `ParseCollectionValueStatement` checks `meta.Kind == ActionKind.Remove && Current().Kind == TokenKind.At` to branch to `RemoveAtStatement`. Similarly for `Append`+`By` and `Enqueue`+`By`. These are mid-parse routing decisions that depend on both the action kind and a subsequent token — neither piece alone is sufficient. The current code is correct but puts domain routing knowledge in the parser.

**The resolution: `ActionVariant` in `ActionMeta` (§3.3).**

The action shape interpreter becomes a pure catalog reader:

```
ParseActionStatement():
  1. meta = Actions.ByTokenKind[Current().Kind]
  2. kw = Advance()                                    // consume action keyword
  3. field = ConsumeIdent()                            // consume field name

  4. // BeforeValue variants (e.g. 'remove at')
     for each v in meta.Variants where v.Position == BeforeValue:
       if Current().Kind == v.TriggerToken:
         return ExecuteVariantShape(v.VariantShape, kw, field)

  5. // Parse base value(s) per meta.SyntaxShape
     result = ExecuteBaseShape(meta.SyntaxShape, kw, field)
     if result is not null: return result     // for FieldOnly and CollectionInto shapes

  6. // AfterValue variants (e.g. 'append by', 'enqueue by')
     for each v in meta.Variants where v.Position == AfterValue:
       if Current().Kind == v.TriggerToken:
         return ExecuteVariantShape(v.VariantShape, kw, field, stashedValue)

  7. return BuildActionStatement(meta, kw, field, stashedValue)
```

`ExecuteBaseShape` returns the parsed value and stashes it for step 6. `ExecuteVariantShape` reads the variant shape and constructs the appropriate result.

**The parser contains zero `meta.Kind` checks.** All branching decisions are driven by `ActionVariant.TriggerToken` — a datum in the catalog. Adding `insertAt`-style variants to new actions in the future requires only a catalog entry, not a parser change.

---

## 5. The Interpreter Loop

The core interpreter is a single recursive method. This is the entire generic machinery:

```
Interpret(ParseRule rule, Cursor cur, ResultBag bag):

  Seq(rules):
    for each r in rules: Interpret(r, cur, bag)

  Opt(rule):
    snapshot = cur.Position
    try: Interpret(rule, cur, bag)
    on ParseFailed: cur.Restore(snapshot)   // backtrack to snapshot

  Rep(rule):
    while true:
      snapshot = cur.Position
      try: Interpret(rule, cur, bag)
      on ParseFailed: cur.Restore(snapshot); break

  Alt(rules):
    for each r in rules:
      snapshot = cur.Position
      try: Interpret(r, cur, bag); return
      on ParseFailed: cur.Restore(snapshot)
    // all alternatives failed — emit diagnostic

  Consume(k):
    if cur.Current().Kind == k: cur.Advance()
    else: emit error (ExpectedToken)           // do NOT throw — just emit diagnostic and advance

  ConsumeLeader():
    cur.Advance()   // already know it's the right token — dispatch loop verified it

  ConsumeIdent():
    if cur.Current().Kind == Identifier: cur.Advance()
    else: emit error; synthetic token

  Tag(name, rule):
    inner = new ResultBag()
    Interpret(rule, cur, inner)
    bag[name] = inner.SingleResult()  // typed production rules produce a single node

  TypeRefProd():   return ParseTypeRef(cur)          // dedicated sub-parser (§6.3)
  ExprProd():      return ParseExpression(cur, 0)    // Pratt loop (§6)
  ModifiersProd(): return ParseModifiers(cur)         // small loop over ModifierKeywords
  // ... other typed productions

  FamilyDispatch(family):
    // as described in §4.1
```

`ParseFailed` is a lightweight sentinel — not an exception. `Opt` and `Alt` use cursor snapshots. Since Precept's grammar is LL(1) at the construct level, speculative parsing only occurs inside `Opt` clauses and `FamilyDispatch`'s floating-guard step. The grammar never requires deep backtracking.

**No per-construct code anywhere in this loop.** It reads grammar rules from metadata and executes them. The per-construct behavior lives in the grammar trees declared in `Constructs.cs`.

---

## 6. Expression Parsing

### 6.1 The Pratt Loop Survives

The Pratt loop in `ParseExpression` is already correctly designed and already mostly catalog-driven. It survives in the radical model. Its residence moves from `Parser.Expressions.cs` to the single `Parser.cs`, because the file split was organizational habit, not structural necessity. The code is essentially unchanged.

Minor cleanup: the three hardcoded branches for `.`, `is set`, and `()` can be unified with catalog entries for `MemberAccess` (precedence 80) and `MethodCall` (precedence 90) operators with `LeftDenotation` parse behavior metadata. This is §5.2 of the review. It's P2 — correct but not load-bearing. The current Pratt loop code is fine; carrying it forward as-is loses nothing.

### 6.2 `ExpressionBoundaryTokens` Becomes Fully Derived

`StructuralBoundaryTokens` is currently hand-listed. In the radical model it's derived from the combinator trees: any `Consume(k)` or `Expect(k)` that appears as the first rule after an `ExprProd()` in any construct grammar is a boundary token. This is computable at startup from the grammar trees. Hand-listing is eliminated.

### 6.3 Type Parsing via `TypeParseShape` Dispatch

`ParseTypeRef` shrinks from 190 lines to a 5-arm switch dispatching on `TypeMeta.ParseShape`:

```
ParseTypeRef(cur):
  current = cur.Current()
  if current.Kind == Tilde: return ParseCISensitive(cur)        // ~string
  if !TypeParseDispatch.TryGetValue(current.Kind, out shape): error
  return shape switch
    ScalarShape     → ParseScalarType(cur)       // keyword [qualifiers]
    CollectionShape → ParseCollectionType(cur)   // keyword 'of' elem [qualifiers] [by P [dir]]
    LookupShape     → ParseLookupType(cur)       // 'lookup of' K 'to' V
    ChoiceShape     → ParseChoiceType(cur)       // 'choice of' ElemType '(' options ')'
```

Each branch is 8–15 lines. `TryPeekQualifierKeyword` is unchanged and shared by `ParseScalarType` and `ParseCollectionType`.

---

## 7. AST Shape

The AST is what the type checker builds on. These are the concrete changes from current types.

### 7.1 Action Statements: Shape-Based, Not Kind-Based

Current design: one C# type per `ActionKind` — 14 statement types with parallel field structures.

New design: **one C# type per `ActionSyntaxShape`** — 7 types, each carrying `ActionMeta` so downstream stages can read the kind.

```csharp
// Replaces: SetStatement, AddStatement, RemoveStatement, EnqueueStatement, PushStatement, AppendStatement
sealed record ActionStatement(SourceSpan Span, ActionMeta Meta, Token Field, Expression Value)
    : Statement(Span);

// Replaces: AppendByStatement, EnqueueByStatement
sealed record ActionByStatement(SourceSpan Span, ActionMeta Meta, Token Field, Expression Value, Expression Key)
    : Statement(Span);

// Replaces: DequeueStatement, PopStatement  
sealed record ActionIntoStatement(SourceSpan Span, ActionMeta Meta, Token Field, Token? IntoField)
    : Statement(Span);

// Replaces: DequeueByStatement
sealed record ActionIntoByStatement(SourceSpan Span, ActionMeta Meta, Token Field, Token? IntoField)
    : Statement(Span);

// Replaces: ClearStatement
sealed record ActionFieldOnlyStatement(SourceSpan Span, ActionMeta Meta, Token Field)
    : Statement(Span);

// Unchanged shapes (already unique — no ambiguity about what they are):
sealed record InsertStatement(SourceSpan Span, Token Field, Expression Value, Expression Index)
    : Statement(Span);
sealed record PutStatement(SourceSpan Span, Token Field, Expression Key, Expression Value)
    : Statement(Span);
sealed record RemoveAtStatement(SourceSpan Span, Token Field, Expression Index)
    : Statement(Span);
```

**Type-checker impact**: The type checker pattern-matches on the shape-based types and reads `Meta.Kind` for kind-specific semantics (e.g., validating that `add` is applied to a collection field). This is identical to the current pattern where the TC reads `ActionMeta`. The C# type now carries the shape; the metadata carries the kind. No breaking change to TC logic — a `SetStatement` was already read as a shape (`AssignValue`) by the TC.

### 7.2 Declaration Nodes: Unchanged

All 12 declaration node types (`FieldDeclarationNode`, `StateDeclarationNode`, `TransitionRowNode`, etc.) keep their current record shapes and property names. The radical design changes *how they're assembled* (from a `ResultBag` rather than hand-coded slot arrays), not *what they contain*.

The type checker sees exactly the same node types it sees today. The only thing that changes from the TC's perspective is action statement types (§7.1) and a cleaner assembly path.

### 7.3 Expression Nodes: Unchanged

All expression node types survive unchanged. The Pratt loop produces the same output as today. This is entirely transparent to the type checker.

### 7.4 Type Reference Nodes: Unchanged

`ScalarTypeRefNode`, `CollectionTypeRefNode`, `LookupTypeRefNode`, `ChoiceTypeRefNode`, etc. — all unchanged. The type parser outputs the same nodes; it just arrives there via `TypeParseShape` dispatch instead of manual if/else branches.

### 7.5 Summary for Type Checker Designer

The type checker can assume:
- **All 12 declaration node types are identical** — same properties, same record shapes
- **All expression node types are identical**
- **All TypeRef node types are identical**
- **Action statement types change**: match on the 8 shape-based types above; read `Meta.Kind` for kind routing; the meta is always attached
- `ActionMeta` is richer: `Variants` is new but the TC doesn't need it — that's parser-only metadata

---

## 8. Disambiguation

Disambiguation survives in the radical model, but it moves from hand-coded method logic to catalog-declared `ConstructFamily` data.

`Constructs.ByLeadingToken` dispatch is **unchanged and correct**. It was always right. The catalog-driven dispatch loop continues to:
1. Look up the leading token in `Constructs.ByLeadingToken`
2. If single unambiguous candidate: execute `ConstructMeta.Grammar` directly
3. If multiple candidates: look up in `Families.ByLeader`, execute `FamilyDispatch`

`FamilyDispatch` is 40–50 lines of interpreter code that replaces the entire `DisambiguateAndParse` method (75 lines of hand-coded switch logic, plus `TryParseStashedGuard`, `ParseStateTargetDirect`, `ParseEventTargetDirect`, and two 15-arm construct switch bodies).

The disambiguation *correctness* is unchanged — the same token sequences disambiguate the same constructs. What changes is that the routing table (`DisambiguationMap`) is declared in `Families.cs` rather than embedded in a method's switch arms. Adding a new disambiguated construct under `in` is a one-line entry in `Families.ByLeader[TokenKind.In].DisambiguationMap`. Today it requires editing `DisambiguateAndParse`.

---

## 9. Error Recovery

The current recovery model is minimal and correct:
- `SyncToNextDeclaration`: skip until `Constructs.LeadingTokens`
- `ConsumeThrough(k)`: skip until closing delimiter
- `TryParseActionStatementWithRecovery`: skip until next `->` or construct boundary

These survive unchanged in the radical model. Recovery is a property of the *cursor infrastructure*, not of per-construct code. The interpreter calls `SyncToNextDeclaration` when a top-level dispatch fails; it calls `ConsumeThrough` when a typed production (like `ChoiceType`) needs bracket-recovery.

**One improvement**: The `Consume` combinator in the interpreter already handles absent-but-expected tokens gracefully (emit diagnostic, produce synthetic token, continue). This means the interpreter naturally recovers at the slot level — if a `because` keyword is missing from a `rule` declaration, the interpreter emits the diagnostic and continues from the next token, rather than aborting the parse. This is slightly *better* than the current behavior in the slot-iteration path, and costs nothing.

There is no slot-level `RecoveryStrategy` metadata (proposed in review §5.3) in this design. The combinator model's implicit recovery (emit + continue) is sufficient for Precept's flat grammar. DSL-specific recovery strategies are over-engineering for a grammar with no deeply nested structures.

---

## 10. What Survives From the Current Design

The radical redesign is surgical. Several pieces of the current parser are exactly right and survive intact:

| Piece | Fate | Reason |
|---|---|---|
| `Constructs.ByLeadingToken` dispatch loop | **Survives** | Already catalog-driven, working perfectly |
| `OperatorPrecedence` FrozenDictionary | **Survives** | Derived correctly from `Operators.All` |
| All 16+ vocabulary FrozenSets | **Survives** | Exemplary catalog derivation, never duplicate |
| `TryPeekQualifierKeyword` | **Survives unchanged** | Best piece of catalog-driven logic in the codebase |
| Pratt loop core | **Survives** | Correct algorithm, already mostly catalog-driven |
| `ParseAtom` | **Survives** | ~165 lines of legitimately heterogeneous atom forms |
| `ParseModifiers` | **Survives** | Already catalog-driven via `Modifiers.ByFieldToken` |
| `ParseInterpolatedString/TypedConstant` | **Survives** | Legitimately complex multi-token sequences |
| `ParseListLiteral` | **Survives** | Correctly sized |
| `SyncToNextDeclaration` | **Survives** | Minimal, correct |
| `InvokeSlotParser` approach | **Superseded** | The PEG interpreter replaces this, but the per-slot-kind typed productions (`TypeRefProd`, `ExprProd`, etc.) are its direct descendants |

| Piece | Fate | Reason |
|---|---|---|
| `ParseFieldDeclaration` (dedicated method) | **Dissolves** | Replaced by `FieldDeclaration.Grammar` interpretation |
| `DisambiguateAndParse` (hand-coded) | **Dissolves** | Replaced by `FamilyDispatch` + `ConstructFamily` data |
| `TryParseStashedGuard` | **Dissolves** | Replaced by `Family.FloatingGuard` combinator |
| `ParseDirectConstruct` switch | **Dissolves** | `ConstructMeta.Grammar` interpretation handles all cases |
| Per-action-kind throw switches (~120 lines) | **Dissolves** | Replaced by `ActionVariant` data + shape interpreter |
| `BuildNode` exhaustive switch | **Shrinks** | Becomes tag-based assembly per kind (~40 lines) |
| `Parser.Declarations.cs` (650 lines) | **Dissolves** | Absorbed into single `Parser.cs` |
| `Parser.Expressions.cs` (406 lines) | **Merges** | Pratt + Atom become sections of single `Parser.cs` |

---

## 11. Size Estimate

| Section | Lines |
|---|---|
| `ParseSession` cursor infrastructure (unchanged) | 80 |
| `ParseAll` top-level dispatch loop | 30 |
| `Interpret` core — full PEG interpreter | 110 |
| `FamilyDispatch` interpreter (anchor + float + disambiguate) | 50 |
| Typed production sub-parsers (TypeRef, Modifiers, etc.) | 100 |
| Action shape interpreter (`ParseActionStatement` + helpers) | 70 |
| Pratt loop (`ParseExpression`) | 105 |
| `ParseAtom` | 165 |
| `BuildNode` (tag-based assembly) | 60 |
| Error recovery helpers | 25 |
| **Total** | **~795 lines** |

Catalog additions (in `Constructs.cs`, `Actions.cs`, `Types.cs`):

| Section | Lines |
|---|---|
| `ParseRule` combinator type hierarchy | 50 |
| `ConstructMeta.Grammar` fields for 12 constructs | 80 |
| `ConstructFamily` type + `Families` static class | 60 |
| `ActionVariant` type + 4 variant declarations | 30 |
| `TypeParseShape` type + 5 shape declarations on `TypeMeta` | 40 |
| **Total** | **~260 lines** |

**Combined new code: ~1,055 lines.**  
**Replaced code: ~1,800 lines across 3 parser files.**  
Net reduction: ~745 lines. And the surviving code is structurally simpler — no 12-arm switches, no 14-arm kind switches, no `#pragma disable CS8524` blocks.

More importantly: every construct-specific behavior has moved from code to data. A new construct requires a grammar rule and a `ConstructMeta` entry. The parser is untouched.

---

## 12. Appendix: Full Grammar Rules (Sketch)

For reference, the approximate grammar combinator trees for all 12 constructs. These become the `Grammar` field of each `ConstructMeta`.

```
PreceptHeader:
  Seq(ConsumeLeader, Tag("name", ConsumeIdent))

FieldDeclaration:
  Seq(ConsumeLeader,
      Tag("names", IdentListProd),
      Consume(As),
      Tag("type",  TypeRefProd),
      Tag("mods1", ModifiersProd),
      Opt(Seq(Consume(Arrow), Tag("expr", ExprProd), Tag("mods2", ModifiersProd))))

StateDeclaration:
  Seq(ConsumeLeader, Tag("entries", StateEntriesProd))

EventDeclaration:
  Seq(ConsumeLeader,
      Tag("names", IdentListProd),
      Opt(Seq(Consume(LeftParen), Tag("args", ArgListProd), Consume(RightParen))),
      Opt(Tag("initial", Consume(Initial))))

RuleDeclaration:
  Seq(ConsumeLeader,
      Tag("guard",     Opt(Seq(Consume(When), ExprProd))),
      Tag("condition", ExprProd),
      Expect(Because),
      Tag("message",   ExprProd))

--- Disambiguated constructs (body grammar only; anchor zone via FamilyDispatch) ---

AccessMode (in ... modify):
  Seq(Consume(Modify),
      Tag("fields", FieldTargetProd),
      Tag("mode",   AccessModeKeywordProd),
      Opt(Seq(Consume(When), Tag("guard", ExprProd))))

OmitDeclaration (in ... omit):
  Seq(Consume(Omit),
      Tag("fields", FieldTargetProd))

StateEnsure (in ... ensure):
  Seq(Consume(Ensure),
      Tag("condition", ExprProd),
      Opt(Seq(Consume(When), Tag("guard", ExprProd))),
      Expect(Because),
      Tag("message", ExprProd))

StateAction (to ... ->):
  Seq(Consume(Arrow),
      Tag("actions", ActionChainProd))

TransitionRow (from ... on):
  Seq(Consume(On),
      Tag("event",   ConsumeIdent),
      Opt(Seq(Consume(When), Tag("guard", ExprProd))),
      Tag("actions", ActionChainProd),
      Tag("outcome", OutcomeProd))

EventEnsure (on ... ensure):
  Seq(Consume(Ensure),
      Tag("condition", ExprProd),
      Opt(Seq(Consume(When), Tag("guard", ExprProd))),
      Expect(Because),
      Tag("message", ExprProd))

EventHandler (on ... ->):
  Seq(Consume(Arrow),
      Tag("actions", ActionChainProd),
      Opt(Seq(Consume(Ensure), Tag("postCondition", ExprProd))))
```

These 12 grammar trees, plus the 4 family definitions, constitute the complete parse description of Precept's grammar. The parser interprets them. No construct-specific code anywhere.

---

## 13. The Key Insight, Restated

Traditional compilers need per-production methods because their grammars are recursive, context-sensitive, and heterogeneous. Precept's grammar is none of those things. It is flat, keyword-anchored, LL(1) at the construct level, and fully described by a table of ~80 combinator expressions.

The current parser has two paradigms living in the same codebase: the generic slot-iteration path (which handles `state`, `event`, and `rule` correctly), and the hand-written per-construct path (which handles everything else redundantly). The radical design has one paradigm: the generic PEG interpreter path, handling all 12 constructs identically.

The catalog already is the grammar. The parser's only job is to interpret it.

---

*Next: type-checker radical design. Build on §7 (AST Shape) — the declaration and expression node types are unchanged; the action statement types are the only delta to account for.*
