# Catalog-Driven Parser Design Review

**Author:** Frank (Lead/Architect)
**Date:** 2026-05-02
**Subject:** Creative review of the parser design from a catalog-first perspective
**Request:** Shane asked for outside-the-box thinking on catalog-driven parsing — explicitly countering massive model training bias from traditional compiler codebases.

---

## 1. Traditional Compiler Bias Audit

The parser is *already* remarkably catalog-driven compared to anything from the Roslyn/TypeScript/ANTLR world. The vocabulary frozen sets at the top of `Parser.cs` derive from catalogs, not hand-maintained lists. The top-level dispatch loop uses `Constructs.ByLeadingToken` for routing. The Pratt loop reads `OperatorPrecedence` from catalog metadata. This is well ahead of traditional baseline.

But residual patterns remain where the *shape* of the design unconsciously replicates compiler-internal conventions that don't apply here:

### 1.1 The "Per-Construct Parse Method" Pattern

`ParseDirectConstruct` (Parser.cs:321–328) is a manual 4-arm switch dispatching to `ParseFieldDeclaration()`, `ParseStateDeclaration()`, `ParseEventDeclaration()`, `ParseRuleDeclaration()`. Similarly, `DisambiguateAndParse` (Parser.cs:330–404) has two multi-arm switches for state-scoped and event-scoped constructs.

This is the recursive descent tradition: every grammar production gets its own dedicated method. Traditional compilers *must* do this because productions have radically different recursive structure (nested blocks, arbitrary statement nesting, type declarations containing members containing expressions).

**In Precept, constructs are flat sequences of typed slots.** The `ParseConstructSlots` method (Parser.Declarations.cs:661–670) already proves this — it generically iterates a construct's slot metadata and invokes slot parsers. Yet this generic infrastructure coexists alongside dedicated per-construct methods (`ParseFieldDeclaration`, `ParseAccessMode`, etc.) that do the *same thing manually*. Two parsing paradigms occupy the same codebase.

**Bias:** Traditional compilers have heterogeneous productions where generic slot-iteration would be meaningless. Precept's grammar is homogeneous enough that one generic slot-iteration path could handle *all* constructs — the catalog already describes their slot structure completely.

### 1.2 The "Exhaustive Kind Switch for Node Construction" Pattern

`BuildNode` (Parser.cs:482–547) and the per-shape action-statement parsers (`ParseAssignValueStatement`, `ParseCollectionValueStatement`, `ParseFieldOnlyStatement`, `ParseCollectionIntoStatement`) each contain exhaustive `ActionKind` switches where 12+ arms throw `InvalidOperationException` — they exist solely to satisfy CS8524.

This is the Roslyn pattern: express compile-time exhaustiveness as runtime unreachable-arm noise. Traditional compilers have genuinely complex polymorphic node creation where the switch carries real logic.

**In Precept, the action statement switch is already catalog-driven at the routing level** — `Actions.ByTokenKind` + `meta.SyntaxShape` dispatch correctly routes to the right parser method. The *construction* step then re-switches on `ActionKind` to pick the concrete `Statement` subtype, producing pages of `throw new InvalidOperationException`. This is ceremony, not logic.

**Bias:** Traditional compilers need per-kind construction because node types carry genuinely different field structures. Precept's action statements within a shape share the *same* field structure (kw, field, value) — it's only the C# type name that differs.

### 1.3 The "Postfix/Infix Special-Casing in the Pratt Loop" Pattern

The Pratt loop in `ParseExpression` (Parser.Expressions.cs:33–138) has explicit branches for:
- Member access (`.`) — hardcoded precedence 80
- `is set` / `is not set` — multi-token postfix special case
- Method call `(` — hardcoded precedence 90
- Binary operators — catalog-driven via `OperatorPrecedence`

Only the binary operator path is truly catalog-driven. The other three are hand-written branches with hardcoded precedence values. The `Operators` catalog *already* carries precedence metadata for `is set`, `is not set` (precedence 60 as `MultiTokenOp`), member access (implicit), and method call (implicit). But the Pratt loop doesn't read it — it hardcodes `80` and `90`.

**Bias:** Traditional Pratt parsers special-case infix and postfix operators by kind because each may have unique parsing semantics (ternary operator needs two sub-expressions, array indexing needs bracket matching). Precept's non-standard operators (`.`, `is set`, method call) all follow simple, uniform patterns — there's no reason they can't be catalog-described and generically dispatched.

### 1.4 The "Grammar-as-Code" Mindset for Type References

`ParseTypeRef` (Parser.Declarations.cs:901–1091) is 190 lines with five distinct branches: `~string`, `lookup of K to V`, `SimpleCollectionTypeLeaders` (set/queue/stack/log/list/bag of T), `choice of T(...)`, and scalar types with qualifiers.

This is classic grammar-as-code: one method, multiple if/else branches per production alternative. Traditional compilers need this because type syntax is open-ended (generics, arrays, tuples, function types, pointer types, nullable reference types...).

**In Precept, the Types catalog already classifies every type into categories** (`Scalar`, `Temporal`, `BusinessDomain`, `Collection`). The `TypeMeta` carries `QualifierShape`, `Token`, and `TypeCategory`. Yet `ParseTypeRef` doesn't use category-driven dispatch — it manually encodes which types are collections, which support `by P`, which need `of K to V`, etc.

**Bias:** Traditional compilers have no type catalog describing parse syntax per type. Precept *has* one but doesn't exploit it for parse-path routing.

---

## 2. What the Parser Already Gets Right (Catalog-Driven)

Credit where it's due — this parser is *far* ahead of traditional compiler practice on catalog derivation:

### 2.1 Vocabulary FrozenSets (Lines 26–207)

The 16+ frozen sets at the top of `Parser.cs` are derived from catalog metadata: `OperatorPrecedence`, `TypeKeywords`, `ModifierKeywords`, `StateModifierKeywords`, `ActionKeywords`, `OutcomeKeywords`, `ChoiceElementTypeKeywords`, `KeywordsValidAsMemberName`, `AllKeywordKinds`, `CICapableFunctionNames`, `KeywordsUsableAsFunctionNames`, `SimpleCollectionTypeLeaders`, `QualifierPrepositionTokens`, `AmbiguousQualifierPrepositions`. None are hand-maintained.

This is **exemplary**. Adding a new modifier, action, or type keyword to the catalog propagates to the parser automatically. Traditional compilers maintain these as parallel hand-edited sets.

### 2.2 Top-Level Dispatch via `Constructs.ByLeadingToken`

The main dispatch loop (Parser.cs:296–317) uses `Constructs.ByLeadingToken.TryGetValue(token.Kind, ...)` to route to construct parsing. This is purely catalog-driven — adding a new construct with a new leading token just works.

### 2.3 Disambiguation via `FindDisambiguatedConstruct`

The disambiguation logic (Parser.cs:406–416) is catalog-driven: it looks up candidates by leading token, then checks `entry.DisambiguationTokens.Contains(disambToken)`. The parser doesn't hardcode "in + ensure = StateEnsure" — it reads it from catalog entries.

### 2.4 Qualifier Parsing via `TryPeekQualifierKeyword`

The qualifier lookahead (Parser.Declarations.cs:875–898) is entirely catalog-driven: `QualifierPrepositionTokens` from `TypeMeta.QualifierShape`, `AmbiguousQualifierPrepositions` from `Constructs.ByLeadingToken` intersection. This is a genuinely creative piece of catalog derivation that traditional parsers would hand-code.

### 2.5 Action Parsing via `Actions.ByTokenKind` + `ActionSyntaxShape`

`ParseActionStatement` (Parser.Declarations.cs:291–317) looks up the current token in `Actions.ByTokenKind`, then dispatches by `meta.SyntaxShape`. This is catalog-driven dispatch — new actions with existing shapes parse automatically.

### 2.6 Modifier Parsing via `Modifiers.ByFieldToken`

`ParseFieldModifierNodes` (Parser.Declarations.cs:1168–1188) uses `Modifiers.ByFieldToken` for O(1) lookup and `modMeta.HasValue` to decide flag vs. value modifier. Entirely catalog-driven.

### 2.7 Error Recovery via `Constructs.LeadingTokens`

`SyncToNextDeclaration` (Parser.cs:462–466) skips tokens until `Constructs.LeadingTokens.Contains(Current().Kind)` — catalog-derived recovery points.

### 2.8 Generic Slot Iteration (`ParseConstructSlots`)

The `ParseConstructSlots` + `InvokeSlotParser` pattern (Parser.Declarations.cs:655–700) is a genuinely catalog-driven parser architecture. It reads a construct's `Slots` metadata and iterates generically. This is the foundation that proves the architecture is viable.

---

## 3. Catalog-Driven Opportunities (New or Remaining Gaps)

### 3.1 Unified Slot-Based Parsing for All Constructs

**What it is:** `ParseFieldDeclaration` (Parser.Declarations.cs:510–552) manually parses identifiers, 'as', type, modifiers, optional compute expression, then more modifiers. Meanwhile, `ParseStateDeclaration` and `ParseEventDeclaration` and `ParseRuleDeclaration` use the generic `ParseConstructSlots` path. `ParseAccessMode`, `ParseOmitDeclaration`, `ParseStateEnsure`, `ParseStateAction`, `ParseTransitionRow`, `ParseEventEnsure`, `ParseEventHandler` — these *all* have hand-written parse methods.

**What catalog metadata drives it:** `ConstructMeta.Slots` already declares the slot sequence for every construct. `InvokeSlotParser` maps each `ConstructSlotKind` to its parser.

**Why traditional compilers produce this pattern:** In traditional compilers, each production has unique recursive structure — nested blocks, optional clauses in different positions, context-dependent syntax. Per-production methods are the only option. In Precept, *every construct is a flat sequence of typed slots*. The catalog already knows this.

**Concrete proposal:** Eliminate the dedicated parse methods for non-disambiguated constructs. The dispatch loop calls `ParseConstructSlots(meta)` for *all* constructs, using the catalog's slot sequence. `ParseFieldDeclaration`'s special post-expression-modifier logic would be encoded as metadata (a `SlotPosition` field on the modifier slot, or a `SplitAroundSlot` relationship in `ConstructMeta`). The per-construct methods dissolve into the generic slot iterator.

For disambiguated constructs (state-scoped, event-scoped), the disambiguation layer parses the anchor (state/event target + optional guard), then calls the same `ParseConstructSlots` for the remaining slots. The only per-construct code is the disambiguation routing — which is already catalog-driven.

### 3.2 Pratt Loop Operators as Uniform Catalog Entries

**What it is:** The Pratt loop hardcodes precedence `80` for member access and `90` for method call. It handles `is set` / `is not set` as a special multi-token branch. The binary operator path is the only one that reads `OperatorPrecedence` from the catalog.

**What catalog metadata drives it:** `Operators.All` already includes `MultiTokenOp` entries for `is set` and `is not set` (with precedence and associativity). It includes `SingleTokenOp` entries for all binary operators. What's missing: catalog entries for `.` (member access) and `()` (method call) as operators with precedence metadata.

**Why traditional compilers produce this pattern:** Roslyn, TypeScript, and GCC all special-case member access and method call in the Pratt loop because these are not "operators" in the classic BNF sense — they have unique right-hand-side semantics (the `.` expects an identifier, `()` expects a parenthesized argument list). But in a metadata-driven system, "right-hand side semantics" can be described by a `ParseBehavior` field on the operator metadata.

**Concrete proposal:** Add `OperatorKind.MemberAccess` and `OperatorKind.MethodCall` to the Operators catalog with precedence 80 and 90 respectively. Give `OperatorMeta` a new DU field `LeftDenotationParsing`:

```
Sealed subtypes:
  StandardBinary(int nextMinPrec)          — parse right operand
  MemberAccess()                           — expect identifier
  MethodCall()                             — expect paren-delimited args
  MultiToken(TokenKind[] sequence)         — consume token sequence (is set, is not set)
```

The Pratt loop becomes a single unified path: look up the current token in a combined `LeftDenotationTable` → read precedence and parse-behavior → invoke the generic handler. No if/else branches for `.`, `is`, or `(`.

### 3.3 Type Parsing via TypeCategory Dispatch

**What it is:** `ParseTypeRef` has 5 branches — one for `~string`, one for `lookup`, one for collections, one for `choice`, one for scalars. Each branch has its own parsing logic for qualifiers, sub-components, and error recovery.

**What catalog metadata drives it:** `TypeMeta` already has `TypeCategory` (Scalar, Collection, etc.), `QualifierShape`, and `Token`. What's missing: a parse-shape descriptor on `TypeMeta` that tells the parser what syntax this type expects.

**Why traditional compilers produce this pattern:** Traditional compilers have unbounded type syntax (generics, arrays, pointers, function types). Each form requires unique recursive parsing. Precept has exactly 5 type syntax patterns, and they're fixed by the catalog.

**Concrete proposal:** Add a `TypeParseShape` DU to `TypeMeta`:

```
ScalarParse()                              — keyword [qualifiers]
CollectionParse(CollectionVariant)          — keyword 'of' element [qualifiers] ['by' P]
LookupParse()                              — 'lookup of' K 'to' V
ChoiceParse()                              — 'choice of' elemType '(' options ')'
CISensitiveParse(TypeKind inner)           — '~' innerType
```

`ParseTypeRef` shrinks from 190 lines to ~40 lines of generic dispatch that reads the type's parse shape.

### 3.4 Action Node Construction Without Kind-Switch Ceremony

**What it is:** `ParseAssignValueStatement`, `ParseCollectionValueStatement`, `ParseFieldOnlyStatement`, `ParseCollectionIntoStatement` each end with exhaustive `meta.Kind switch` to pick the concrete `Statement` subtype — 12–15 arms each, all but 1–3 throwing `InvalidOperationException`.

**What catalog metadata drives it:** `ActionMeta.SyntaxShape` already routes to the correct parser method. The *construction* switch exists only because each `ActionKind` maps to a different C# `Statement` subclass. If the parser produced a uniform `ActionStatement` node (with the `ActionMeta` attached as metadata), the switch disappears.

**Why traditional compilers produce this pattern:** Roslyn produces one AST node type per syntactic form because downstream phases need structural guarantees. But Precept's action statements *within a shape* are semantically identical at the parse level — the differentiation happens in the type checker, which already reads `ActionMeta`.

**Concrete proposal:** For each `ActionSyntaxShape`, define a single `Statement` subtype that carries `ActionMeta` + the parsed fields. `ParseAssignValueStatement` returns `ActionStatement.Assign(meta, field, value)` without knowing or switching on `meta.Kind`. The type checker and evaluator, which already read `ActionMeta`, get the kind from the metadata — not from the C# type.

Impact: Eliminates ~120 lines of throw-arms across 4 methods. The parser becomes truly shape-driven: it parses the *shape*, attaches the *metadata*, and lets downstream stages interpret the kind.

### 3.5 ExpressionBoundaryTokens as Catalog-Derived

**What it is:** `StructuralBoundaryTokens` (Parser.cs:104–108) is a hand-written array: `When, Because, Arrow, Ensure, EndOfSource`. These are tokens that terminate expression parsing.

**What catalog metadata drives it:** These tokens have a structural role: they begin clause boundaries in constructs. The `ConstructSlotKind` enum and construct metadata *imply* which tokens terminate expressions (they're the "leading keywords" of subsequent slots in a construct's slot sequence).

**Why traditional compilers produce this pattern:** Traditional parsers maintain expression-termination sets by hand because their grammar is too complex to derive them mechanically. Precept's grammar is simple enough that "tokens that begin a new slot in any construct" is derivable from construct metadata.

**Concrete proposal:** Derive `StructuralBoundaryTokens` from `ConstructSlot` metadata — any token that introduces a subsequent slot (the "leader" of each slot kind: `When` leads `GuardClause`, `Because` leads `BecauseClause`, `Arrow` leads `ActionChain`/`ComputeExpression`/`Outcome`, `Ensure` leads `EnsureClause`) should be computed from slot metadata rather than hand-listed.

---

## 4. Right-Sizing Assessment

### 4.1 Per-Construct Parse Methods: Over-Engineered

10+ dedicated parse methods for 12 constructs when `ParseConstructSlots` already proves the generic path works. The dedicated methods exist because they were written before the slot infrastructure matured — they're legacy, not necessity.

**Right-sized alternative:** One generic `ParseConstruct(ConstructMeta meta)` method that iterates slots. Disambiguated constructs add a pre-phase (parse anchor + guard + disambiguate), then call the same generic path for remaining slots. Total construct-parsing code: ~80 lines instead of ~400.

### 4.2 Action Statement Construction Switches: Over-Engineered

~120 lines of exhaustive `ActionKind` switches where 80%+ of arms throw. This is ceremony — the shape dispatch already happened. The parser knows the shape; the kind is metadata attached to the node, not a structural branching point.

**Right-sized alternative:** One `Statement` type per `ActionSyntaxShape` (6 shapes × ~15 lines each = ~90 lines total). No kind switch anywhere in the parser.

### 4.3 ParseTypeRef: Over-Sized for 5 Syntax Patterns

190 lines to parse 5 type syntax patterns. Traditional compilers need this for unbounded type syntax. Precept's 5 patterns are fixed and catalog-described.

**Right-sized alternative:** A 5-arm match on `TypeParseShape` driving 5 small helper methods (~20 lines each) = ~100 lines. With shared qualifier-parsing extracted (already done via `TryPeekQualifierKeyword`), probably closer to ~80.

### 4.4 The Pratt Loop: Appropriately-Sized

The Pratt loop at ~105 lines is correctly sized. Expression parsing genuinely needs recursive precedence handling. The hardcoded branches for `.`, `is`, and `(` are mild over-engineering but not severe — they're functionally correct and performance-critical.

### 4.5 Error Recovery: Appropriately-Sized

`SyncToNextDeclaration`, `TryParseActionStatementWithRecovery`, `ConsumeThrough` — this is minimal, correct error recovery. Not over-engineered.

### 4.6 Interpolated String/TypedConstant Parsing: Appropriately-Sized

These are legitimately complex multi-token parse sequences that can't be further simplified. Correctly sized.

### 4.7 Slot Parser Infrastructure: Appropriately-Sized

The `InvokeSlotParser` switch + individual slot methods is the right architecture. Each `ConstructSlotKind` maps to a small, focused parser. Adding a new slot kind is one switch arm + one method.

---

## 5. Creative Proposals

### 5.1 The "Declarative Grammar Machine" — Parser as Catalog Interpreter

**Radical proposal:** What if the parser has *no parse methods at all* — only a generic grammar interpreter that reads construct metadata as its "instruction set"?

Each `ConstructMeta` already declares its `Slots` — an ordered sequence of `ConstructSlot` entries describing what the parser should expect. Extend this with `ParseHint` metadata on each slot:

```csharp
public sealed record ConstructSlot(
    ConstructSlotKind Kind,
    bool IsRequired = true,
    string? Description = null,
    TokenKind? LeaderToken = null,    // token that introduces this slot (e.g. 'when', 'because')
    bool ConsumesLeader = true        // whether the leader is consumed vs. just peeked
);
```

The *entire parser* becomes:
1. Top-level loop: dispatch by leading token (already catalog-driven)
2. Disambiguation: read `DisambiguationTokens` from catalog (already catalog-driven)
3. Slot iteration: for each slot in `ConstructMeta.Slots`, check for leader token → invoke `InvokeSlotParser(slot.Kind)` → collect results
4. Node construction: `BuildNode(kind, slots, span)`

No `ParseFieldDeclaration`. No `ParseTransitionRow`. No `ParseAccessMode`. The catalog IS the parser. Each `ConstructMeta` is a tiny program describing how to parse its construct.

This is something a traditional parser engineer would *never* propose because traditional grammars are too recursive, too context-sensitive, and too polymorphic for declarative description. Precept's grammar is flat, keyword-anchored, and slot-sequential — it's *perfectly* suited to declarative interpretation.

### 5.2 The "Zero-Branch Type Parser"

**Radical proposal:** Instead of `ParseTypeRef` branching on the current token's type category, register each type-leading token in a `FrozenDictionary<TokenKind, TypeParseStrategy>` computed from catalog metadata at startup:

```csharp
FrozenDictionary<TokenKind, Func<Token, TypeRefNode>> TypeParsers;
```

Where the function for each token kind is selected based on `TypeMeta.TypeCategory` and `TypeMeta.QualifierShape`:
- Scalar types → generic `ParseScalarType(token, qualifierShape)`
- Collection types → generic `ParseCollectionType(token, qualifierShape)`
- Lookup → specific `ParseLookupType`
- Choice → specific `ParseChoiceType`
- `~` prefix → specific `ParseCIType`

The key insight: the *token itself* already tells you which parse strategy applies, because the Types catalog maps tokens to type metadata, and type metadata carries the parse-shape information. `ParseTypeRef` doesn't need conditional logic — it needs a precomputed dispatch table. The current if/else chain is a runtime search for information the catalog already knows at startup.

### 5.3 The "Self-Describing Error Recovery"

**Radical proposal:** Instead of hand-written recovery logic (`SyncToNextDeclaration`, `ConsumeThrough`), each `ConstructSlot` could carry recovery metadata:

```csharp
public sealed record ConstructSlot(
    ...,
    RecoveryStrategy Recovery = RecoveryStrategy.SyncToNextConstruct
);

public enum RecoveryStrategy
{
    SyncToNextConstruct,    // skip until Constructs.LeadingTokens
    SkipToClosingDelimiter, // consume until matching ), ], }
    SkipToSlotLeader,       // skip until next slot's LeaderToken in this construct
    EmitPlaceholder,        // produce a placeholder node and continue
}
```

The parser's recovery behavior becomes *declared* per slot rather than coded per construct. When `InvokeSlotParser` fails, it reads the slot's `Recovery` field and applies the appropriate generic strategy. No per-method recovery code.

### 5.4 The "Construct as Grammar Rule" — Metadata-Driven PEG

**Most radical proposal:** What if each `ConstructMeta` IS a PEG rule, written in a tiny internal DSL that the generic parser interprets?

```csharp
// Instead of ConstructSlot lists, a construct carries a ParseGrammar:
public sealed record ConstructMeta(
    ...,
    ParseRule Grammar   // e.g. Seq(Keyword("field"), Names, Keyword("as"), TypeRef, OptRepeat(Modifier), Opt(Seq(Arrow, Expr)))
);
```

This takes the slot-iteration pattern to its logical extreme: the catalog doesn't just list slot *kinds* — it describes their *syntactic relationship* (sequence, repetition, optionality, alternation). The parser becomes a PEG interpreter that reads grammar rules from construct metadata.

For Precept's 12 constructs, each grammar rule would be ~3–8 combinators long. The total grammar description would be ~60 lines of metadata — replacing ~400 lines of hand-written parse methods.

Traditional compilers would never do this because their grammars require left-recursion elimination, complex precedence handling, and ambiguity resolution that PEG interpreters handle poorly. Precept's grammar is LL(1) with trivial lookahead (one disambiguation token past the anchor) — it's *trivially* PEG-interpretable.

### 5.5 The "Structural Boundary Dissolution"

**Insight proposal:** The distinction between "Parser.cs", "Parser.Expressions.cs", and "Parser.Declarations.cs" replicates the traditional compiler's code-organization model: expressions are fundamentally different from declarations, so they live in different files with different architects.

In a catalog-driven system, this distinction dissolves. Declarations are slot sequences interpreted from `ConstructMeta`. Expressions are operator-precedence sequences interpreted from `Operators.All`. Both are "interpret catalog metadata to produce AST nodes." The organizational boundary between them is an artifact of traditional thinking, not a structural necessity.

A right-sized parser for a 20-keyword, 10-construct DSL designed catalog-first would be **one file, ~300 lines**: a generic slot interpreter (constructs), a Pratt loop (expressions), and the shared infrastructure (cursor, recovery, diagnostics). That's it. The rest — all the per-construct methods, per-type branches, per-action switches — dissolves into catalog metadata.

---

## 6. Recommendations

Prioritized by impact and alignment with the catalog-driven principle:

| # | Recommendation | Impact | Effort | Priority |
|---|---|---|---|---|
| 1 | **Unify all non-disambiguated constructs through `ParseConstructSlots`** — eliminate `ParseFieldDeclaration` as a dedicated method; express its post-expression-modifier logic as slot metadata. | HIGH — removes ~100 lines of redundant per-construct code; proves the generic path handles everything | MEDIUM — needs slot metadata for field-declaration's split modifier position | **P1** |
| 2 | **Eliminate action-kind switches in shape parsers** — produce a uniform `Statement` per shape carrying `ActionMeta`, or use a factory method indexed by `ActionKind` without exhaustive throw-arms | HIGH — removes ~120 lines of pure ceremony | LOW — straightforward refactor | **P1** |
| 3 | **Derive `StructuralBoundaryTokens` from slot metadata** — compute expression-terminating tokens from "which tokens are slot leaders" rather than hand-listing | MEDIUM — eliminates a hand-maintained set that drifts when new slots are added | LOW — simple derivation at startup | **P1** |
| 4 | **Add `TypeParseShape` to `TypeMeta`** — let `ParseTypeRef` dispatch on catalog-declared type parse shapes instead of 5 if/else branches | MEDIUM — shrinks ParseTypeRef from 190 to ~80 lines | MEDIUM — needs DU design on TypeMeta | **P2** |
| 5 | **Catalog-ize Pratt loop special branches** — add operator entries for `.` and method call with parse-behavior metadata; unify the Pratt loop into a single table-driven path | LOW-MEDIUM — aesthetic improvement, removes 3 hardcoded branches | MEDIUM — needs LeftDenotation DU on OperatorMeta | **P2** |
| 6 | **Consider the Declarative Grammar Machine** (§5.1) — extend slot metadata with leader tokens and recovery strategies so the parser becomes a pure construct-metadata interpreter | HIGH (long-term) — dissolves per-construct code entirely | HIGH — needs careful metadata design and full regression coverage | **P3** |
| 7 | **Consider uniform action-shape `Statement` types** (§3.4) — one type per shape, metadata-attached, no kind-switch in parser | MEDIUM — cleaner AST, simpler parser | MEDIUM — downstream stages need adjustment | **P3** |

### What NOT to change:

- **Pratt loop core** — correctly designed, precedence-climbing is the right algorithm
- **`Constructs.ByLeadingToken` dispatch** — already catalog-driven, working perfectly
- **Disambiguation routing** — already catalog-driven, keep as-is
- **Qualifier lookahead (`TryPeekQualifierKeyword`)** — creative catalog-driven logic, keep as-is
- **`InvokeSlotParser` infrastructure** — the right foundation, extend it don't replace it
- **Error recovery** — minimal and correct, don't over-engineer

### Summary principle:

The recurring theme is: **Precept's grammar is flat and slot-sequential, not recursive and heterogeneous.** Traditional compilers need per-production methods because productions have genuinely different recursive structure. Precept's constructs are all the same shape: a leading keyword, an anchor (optional), then a sequence of typed slots. The parser should reflect this uniformity — one generic path that reads construct metadata, not 10+ methods that each manually implement a flat slot sequence.

The key insight for the parser (analogous to precomputed lookup tables for the type checker): **the catalog already IS the grammar.** Each `ConstructMeta` with its slot list IS a grammar rule. The parser's job is to *interpret* that rule — not to *reimplement* it in C# methods. The more the parser reads from catalog metadata at runtime, the less code it needs. And unlike traditional compilers where "less code" means "less capability," in Precept "less parser code" means "more catalog-driven" — which is strictly superior.

---

*This review does not propose code generation. It proposes that the parser's design lean harder into the implications of a flat, keyword-anchored, slot-sequential grammar — which traditional compiler training bias systematically underweights because no mainstream compiler has one.*
