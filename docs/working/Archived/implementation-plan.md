# Precept V2 — Collection Types & Scalar `~string` Implementation Plan

**Status:** Draft  
**Scope:** 6 feature areas — scalar `~string`, bag, list, log, log-by, queue-by, lookup  
**Design docs:** `docs/language/primitive-types.md`, `docs/language/collection-types.md`  
**Baseline:** Clean working tree (confirmed `git diff HEAD -- src/` is empty)

---

## Non-Negotiable Architectural Constraints

All implementation in this plan must comply with `docs/contributing/catalog-driven-checklist.md`. Key rules:

- Every new token/keyword/operator has a `GetMeta` entry — no bare values
- `OperatorPrecedence` derives from `Operators.All` — no inline binding power
- `KeywordsValidAsMemberName` derives from `IsValidAsMemberName: true` in `GetMeta` — no manual Parser.cs edits
- No `FrozenSet<TokenKind>` or token arrays maintained outside catalog queries
- No pipeline stage switches on enum member identity for per-member behavior
- All exhaustive switches must remain exhaustive — no `default:` suppressions

These are not guidelines. Violations are blockers, not nits.

---

## Baseline State

### What exists today

| Layer | State |
|---|---|
| Lexer | Handles Set/QueueType/StackType, Tilde (for collection element CI), Into, No, All, Any, Contains, Is |
| Parser | ParseTypeRef handles set/queue/stack + scalar types; ParseActionStatement handles 8 action kinds |
| TypeChecker | **Complete stub** — `throw new NotImplementedException()` on all paths |
| ProofEngine | **Complete stub** — `throw new NotImplementedException()` |
| GraphAnalyzer | **Complete stub** — `throw new NotImplementedException()` |
| Evaluator | **Complete stub** — `throw new NotImplementedException()` on all paths |
| Catalogs | Exhaustive switches on TokenKind, TypeKind, ActionKind, DiagnosticCode — all must stay exhaustive |

All four Phase 3 pipeline stages are stubs. The catalog-first constraint means enum additions **must** come before all metadata entries, which **must** come before parser implementations. TypeChecker, ProofEngine, GraphAnalyzer, and Evaluator implementations are out of scope for this plan — they will be addressed in separate plans.

### Pre-existing regression anchors

| Test region | Location | Coverage |
|---|---|---|
| `set of ~string` parsing | `ParserTests.cs:1677–1719` | GAP-3 (direct anchor for Slice 6/7) |
| Collection action parsing | `ParserTests.cs:1590–1675` | set/queue/stack actions (anchor for Slice 8) |
| Type-ref parsing | `ParserTests.cs:900–1050` | all existing scalar types (anchor for Slice 6) |
| Expression parsing | `ParserTests.cs:1100–1350` | Pratt loop + member access (anchor for Slices 9/10) |

---

## Dependency Graph

```
Slice 1 (TokenKind) ─┬─► Slice 2 (TypeKind/ActionKind)
                     └─► Slice 3 (Tokens.GetMeta)
                         Slice 3 ──► Slice 3b (OperatorKind.LookupAccess / Operators.GetMeta)

Slice 2 ─┬─► Slice 4 (Types.GetMeta)
          └─► Slice 5 (Actions.GetMeta/ActionSyntaxShape)

Slices 1–5 ──► Slice 6 (DiagnosticCode/ExpressionFormKind)

Slices 1–6 ──► Slice 7 (AST nodes)

Slices 1–7 ──► Slices 8–14 (Parser slices)
```

---

## Slice 1 — TokenKind Enum Additions

**Files:** `src/Precept/Language/TokenKind.cs`

### Implementation

Add 16 new token kind enum members after the existing entry at value 123.

```csharp
// New collection type keywords
BagType      = 124,   // "bag"
ListType     = 125,   // "list"
LogType      = 126,   // "log"
LookupType   = 127,   // "lookup"

// New ordering / indexing keywords
By           = 128,   // "by"   — ordering-key connector: log of T by P, queue of T by P
At           = 129,   // "at"   — index connector in action: insert F Expr at N, remove F at N
Ascending    = 130,   // "ascending"  — sort direction on queue-by type ref
Descending   = 131,   // "descending" — sort direction on queue-by type ref

// New action keywords
Append       = 132,   // "append"     — log/list append
Insert       = 133,   // "insert"     — list insert-at
Put          = 134,   // "put"        — lookup upsert

// New quantifier keyword
Each         = 135,   // "each"

// New lookup access operator  
For          = 136,   // "for"   — infix key accessor: F for K

// New member-name tokens (valid as identifiers after dot)
Countof      = 137,   // "countof"  — .countof(E) on all collections
Peekby       = 138,   // "peekby"   — .peekby on queue-by (peek-by key)

// New type-syntax connector
To           = 139,   // "to"   — key-to-value connector in lookup of K to V
```

**Design notes:**
- `by`, `at`, `for`, `ascending`, `descending` are new reserved keywords. They are **not** valid as field names. The language spec intentionally reserves them.
- `remove F at N` uses the existing `Remove` token plus the contextual `At` token — there is no `remove-at` compound keyword. The parser dispatches on `TokenKind.Remove` and checks whether the next token after the field is `At`; if so it parses the positional form, otherwise it falls through to `remove F Expr`.
- `countof` and `peekby` have `IsValidAsMemberName: true` in their `Tokens.GetMeta` entries — `KeywordsValidAsMemberName` derives automatically from `Tokens.All.Where(t => t.IsValidAsMemberName)`; do not add them to a manual set or edit `Parser.cs`.
- `each` is new. `any` (44) and `no` (32) already exist but serve dual roles (state wildcard / quantifier and `no transition` / quantifier respectively). Disambiguation is handled in the parser by context (declaration vs. expression position).

**Tests:**
- `LexerTests.cs`: `TokenizesNewKeywords` — verify `bag`, `list`, `log`, `lookup`, `by`, `at`, `ascending`, `descending`, `append`, `insert`, `put`, `each`, `for`, `countof`, `peekby`, `to` each lex to their new kind.
- `LexerTests.cs`: `NewKeywordsAreNotValidIdentifiers` — verify `bag`, `list`, `log`, `lookup`, `by`, `at`, `ascending`, `descending`, `append`, `insert`, `put`, `each`, `for`, `to` each produce a keyword token, not `Identifier`.

**Regression anchors:** All pre-existing lexer tests must still pass unchanged.

**Depends on:** Nothing (first slice).

---

## Slice 2 — TypeKind & ActionKind Enum Additions

**Files:** `src/Precept/Language/TypeKind.cs`, `src/Precept/Language/ActionKind.cs`, `src/Precept/Language/Action.cs`

### TypeKind additions

```csharp
// Appended after StateRef=26 (preserves Error=25 and StateRef=26 integer values — insertion strategy)
Log      = 27,   // "log of T"             — append-only ordered log
LogBy    = 28,   // "log of T by P"        — append-only ordered log keyed by P
Bag      = 29,   // "bag of T"             — multiset (element + count)
List     = 30,   // "list of T"            — ordered list with index access
QueueBy  = 31,   // "queue of T by P"      — priority queue keyed by P
Lookup   = 32,   // "lookup of K to V"     — key-value map
```

### ActionKind additions

```csharp
// After Clear=8
Append      = 9,    // append F expr           — log/list append (no ordering key)
AppendBy    = 10,   // append F expr by P      — log-by append with explicit key
Insert      = 11,   // insert F Expr at N      — list insert at index
RemoveAt    = 12,   // remove F at N           — list remove at index
Put         = 13,   // put F K = V             — lookup upsert
EnqueueBy   = 14,   // enqueue F expr by P     — queue-by enqueue with key
DequeueBy   = 15,   // dequeue F [into G] [by H] — queue-by dequeue with optional routing
```

### ActionSyntaxShape additions (in `Action.cs`)

```csharp
/// <summary>verb field expr by expr  (append-by, enqueue-by)</summary>
CollectionValueBy   = 5,
/// <summary>verb field expr at expr  (insert at index)</summary>
InsertAt            = 6,
/// <summary>verb field at expr       (remove at index: positional, no element)</summary>
RemoveAtIndex       = 7,
/// <summary>verb field key = value   (put: lookup upsert)</summary>
PutKeyValue         = 8,
/// <summary>verb field [into field] [by key]  (dequeue-by: optional into + optional routing)</summary>
CollectionIntoBy    = 9,
```

**Tests:**
- Unit: `TypesTests.cs` / `ActionsTests.cs` — confirm all enum values are contiguous with no gaps or duplicates (reflection-based assertion).

**Regression anchors:** Existing ActionKind.Set=1 through Clear=8 values unchanged.

**Depends on:** Slice 1 (for contextual clarity only; these enums are independent of TokenKind).

---

## Slice 3 — Tokens Catalog: GetMeta Entries for New Tokens

**Files:** `src/Precept/Language/Tokens.cs`

### Implementation

`Tokens.GetMeta(TokenKind kind)` is an exhaustive switch. Adding new `TokenKind` members without adding switch arms produces a compile error. Add one entry per new token kind from Slice 1.

**Description strings derive from `docs/language/precept-language-spec.md` § Token vocabulary — do not invent descriptions; read the spec.** For each new token, record only the structural metadata:

| Token | `TokenCategory` | `TextMateScope` | `SemanticTokenType` | `IsValidAsMemberName` |
|-------|-----------------|-----------------|---------------------|-----------------------|
| `BagType` | `Cat_Type` | `storage.type.precept` | `type` | false |
| `ListType` | `Cat_Type` | `storage.type.precept` | `type` | false |
| `LogType` | `Cat_Type` | `storage.type.precept` | `type` | false |
| `LookupType` | `Cat_Type` | `storage.type.precept` | `type` | false |
| `By` | `Cat_Prep` | `keyword.control.precept` | `keyword` | false |
| `At` | `Cat_Prep` | `keyword.control.precept` | `keyword` | false |
| `Ascending` | `Cat_Decl` | `keyword.declaration.precept` | `keyword` | false |
| `Descending` | `Cat_Decl` | `keyword.declaration.precept` | `keyword` | false |
| `Append` | `Cat_Act` | `keyword.other.action.precept` | `keyword` | false |
| `Insert` | `Cat_Act` | `keyword.other.action.precept` | `keyword` | false |
| `Put` | `Cat_Act` | `keyword.other.action.precept` | `keyword` | false |
| `Each` | `Cat_Qnt` | `keyword.other.quantifier.precept` | `keyword` | false |
| `For` | `Cat_Prep` | `keyword.control.precept` | `keyword` | false |
| `Countof` | `Cat_Cns` | `keyword.other.precept` | `keyword` | **true** |
| `Peekby` | `Cat_Cns` | `keyword.other.precept` | `keyword` | **true** |
| `To` | `Cat_Kw` | `keyword.control.precept` | `keyword` | false |

`Countof` and `Peekby` have `IsValidAsMemberName: true` — `KeywordsValidAsMemberName` derives automatically from `Tokens.All.Where(t => t.IsValidAsMemberName)`. Do not add them to a manual set or edit `Parser.cs`.

### Sub-section 3b — OperatorKind.LookupAccess

**Files:** `src/Precept/Language/OperatorKind.cs`, `src/Precept/Language/Operators.cs`

`TokenKind.For` is added in Slice 1. `For` participates in the Pratt expression loop as a binary infix operator — it must appear in the `Operators` catalog so `OperatorPrecedence` picks it up automatically. No inline special-casing in `ParseExpression` is correct; the Pratt loop reads binding power from `Operators.All`.

**Add to `OperatorKind.cs`** (after the existing `IsNotSet = 20` entry):

```csharp
// ── Lookup ─────────────────────────────────────────────────
LookupAccess = 21,   // F for K — lookup key access
```

**Add to `Operators.GetMeta` (exhaustive switch in `Operators.cs`):**

```csharp
OperatorKind.LookupAccess => new SingleTokenOp(
    kind, Tokens.GetMeta(TokenKind.For),
    "Lookup key access",
    Arity.Binary, Associativity.Left, Precedence: 40, OperatorFamily.Membership,
    IsKeywordOperator: true),
```

Binding power is **40** (same tier as `contains`; above `and`/`or`, below arithmetic). `OperatorPrecedence` reads from `Operators.All` automatically — do not hardcode binding power anywhere in the parser.

**Tests:**
- `TokensTests.cs`: `AllNewTokenKindsHaveMetaEntries` — reflection over all `TokenKind` values, assert `Tokens.GetMeta(k)` does not throw.
- `TokensTests.cs`: `NewCollectionTypeTokensHaveCorrectCategory` — assert `BagType`, `ListType`, `LogType`, `LookupType` have `Cat_Type`.
- `OperatorsTests.cs`: `LookupAccessHasCorrectBindingPower` — assert `Operators.GetMeta(OperatorKind.LookupAccess).Precedence == 40`.
- `OperatorsTests.cs`: `LookupAccessTokenIsFor` — assert the token for `LookupAccess` is `TokenKind.For`.

**Existing tests that break and must be updated in this slice:**
- `TokenMetaMemberNameTests.cs` `AllOtherKeywords_IsValidAsMemberName_False` — exhaustive negative sweep; breaks when `Countof` and `Peekby` gain `IsValidAsMemberName: true`. Fix: exclude `Countof` and `Peekby` from the negative sweep.
- `TokenMetaMemberNameTests.cs` `ContainsMinAndMax` — currently `HaveCount(2)` (Min, Max); breaks when `Countof` and `Peekby` are added. Fix: update to `HaveCount(4)` (Min, Max, Countof, Peekby). ⚠️ *This count is derived from the catalog at implementation time — it will break if additional `IsValidAsMemberName: true` tokens are added. Prefer `Tokens.All.Where(t => t.IsValidAsMemberName).Count()` over a hardcoded integer where the test framework allows it.*
- `OperatorsTests.cs` `Operators_All_CountIs21` (was `Operators_All_CountIs20`) — update to `HaveCount(21)` (1 new operator: `LookupAccess`).
- `OperatorsTests.cs` `Operators_SingleTokenOp_CountIs18` — rename to `Operators_SingleTokenOp_CountIs19` (retire `CountIs18`) and update the assertion to `HaveCount(19)`.

**Regression anchors:** All pre-existing `GetMeta` entries unchanged. Existing catalog tests pass.

**Depends on:** Slice 1 (TokenKind values). `OperatorKind.LookupAccess` is added alongside `Tokens.GetMeta` in this slice.

---

## Slice 4 — Types Catalog: TypeMeta Entries for New Collection TypeKinds

**Files:** `src/Precept/Language/Types.cs`

### Implementation

`Types.GetMeta(TypeKind kind)` is an exhaustive switch. Add entries for `Log`, `LogBy`, `Bag`, `List`, `QueueBy`, `Lookup`.

**Accessor definitions derive from `docs/language/collection-types.md` — see the accessor matrix table and each type's accessor section. Do not reproduce return types or proof requirements inline; read the spec.**

**New entry shapes:**

```
TypeKind.Bag  → CollectionTypeMeta(
    Token: Tokens.GetMeta(TokenKind.BagType),
    SupportedActions: [Add, Remove, Clear],
    NotemptyApplicable: true)

TypeKind.List → CollectionTypeMeta(
    Token: Tokens.GetMeta(TokenKind.ListType),
    SupportedActions: [Append, Insert, RemoveAt, Clear],
    NotemptyApplicable: true)

TypeKind.Log  → CollectionTypeMeta(
    Token: Tokens.GetMeta(TokenKind.LogType),
    SupportedActions: [Append],
    NotemptyApplicable: true)

TypeKind.LogBy → TwoParamCollectionTypeMeta(
    Token: Tokens.GetMeta(TokenKind.LogType),
    OrderingKeyType: any comparable scalar,
    SupportedActions: [AppendBy],
    NotemptyApplicable: true,
    KeyUniquenessGuardRequired: true)
    // .first / .last / .at(N) return T — see collection-types.md lines 359–361.
    // .countof(P) is NOT in the accessor list — per spec accessor matrix
    // (collection-types.md line 861), .countof(E) is Bag-only. A spec change
    // is required before .countof can be added to LogBy.

TypeKind.QueueBy → TwoParamCollectionTypeMeta(
    Token: Tokens.GetMeta(TokenKind.QueueType),
    OrderingKeyType: any comparable scalar,
    SortDirection: field-level Ascending (default) or Descending,
    SupportedActions: [EnqueueBy, DequeueBy, Clear],
    NotemptyApplicable: true)

TypeKind.Lookup → TwoParamCollectionTypeMeta(
    Token: Tokens.GetMeta(TokenKind.LookupType),
    KeyType: any scalar (optional: ~string for CI keys),
    ValueType: any scalar,
    SupportedActions: [Put, Remove],
    NotemptyApplicable: false)   ← lookup excluded from notempty
    // ⚠️ Spec inconsistency: The Lookup type body section of collection-types.md implies
    // notempty applies, but the authoritative Constraint Catalog table (precept-language-spec.md
    // §3.7 Modifier validation table) defines notempty as a type error on Lookup. This plan
    // follows the Constraint Catalog. This inconsistency should be flagged for resolution in a
    // separate issue.
```

**Notes for two-parameter types:**
- `LogBy`, `QueueBy`, and `Lookup` have two type parameters. Current `CollectionTypeRefNode` carries one element type token. Three distinct AST subtypes are needed: `LogByTypeRefNode`, `QueueByTypeRefNode`, and `LookupTypeRefNode` (added in Slice 8).

**Tests:**
- Unit: `TypesTests.cs` — `AllNewTypeKindsHaveMetaEntries` — reflection, assert no `GetMeta` throws.
- Unit: `TypesTests.cs` — `LookupNotemetptyApplicable` — assert `Lookup` entry has `NotemptyApplicable: false`; all others (`Log`, `LogBy`, `Bag`, `List`, `QueueBy`) have `true`.
- Unit: `TypesTests.cs` — `LogListLogByHaveAtAccessor` — assert `Log`, `List`, and `LogBy` entries all have `.at(N)` in their accessor lists; `Bag` and `QueueBy` do **NOT** have `.at(N)` (assert both positive and negative cases explicitly).
- Unit: `TypesTests.cs` — `BagHasNoFirstOrLast` — assert `Bag` entry has no `.first`/`.last` accessors.
- Unit: `TypesTests.cs` — `QueueByHasPeekNotFirstLast` — assert `QueueBy` entry has `.peek` accessor and does not have `.first`/`.last`.
- Unit: `TypesTests.cs` — `QueueByHasPeekbyAccessor` — (positive) assert `QueueBy` entry has `.peekby(P)` accessor. Source: accessor matrix (collection-types.md line 857).
- Unit: `TypesTests.cs` — `LogHasFirstAndLast` — (positive) assert `Log` entry has `.first` and `.last` accessors. Source: accessor matrix (collection-types.md lines 858–859).
- Unit: `TypesTests.cs` — `ListHasFirstAndLast` — (positive) assert `List` entry has `.first` and `.last` accessors. Source: accessor matrix (collection-types.md lines 858–859).
- Unit: `TypesTests.cs` — `LogByHasFirstAndLast` — (positive) assert `LogBy` entry has `.first` and `.last` accessors. Source: accessor matrix (collection-types.md lines 858–859).
- Unit: `TypesTests.cs` — `BagHasCountofAccessor` — (positive) assert `Bag` entry has `.countof(E)` accessor. Source: accessor matrix (collection-types.md line 861).
- Unit: `TypesTests.cs` — `LogListBagHaveNoPeek` — (negative) assert `Log`, `List`, and `Bag` entries do NOT have `.peek` (Queue/QueueBy only per accessor matrix).
- Unit: `TypesTests.cs` — `CountofDoesNotExistOnQueueQueueByLogList` — (negative) assert `Queue`, `QueueBy`, `Log`, and `List` entries do NOT have `.countof(E)` (Bag-only per accessor matrix line 861).
- Unit: `TypesTests.cs` — `ForKSyntaxOnlyOnLookup` — (negative) assert `Set`, `Queue`, `Stack`, `Log`, `LogBy`, `Bag`, `List`, `QueueBy` entries do NOT have the `F for K` accessor (Lookup-only per accessor matrix line 862).

**Accessor coverage requirement (Soup):** Each new type must have positive tests for every defined accessor (`.count`, `.first`/`.last`, `.at(N)`, `.peek`, `.peekby`, `.countof(E)`, and `F for K`) AND negative tests confirming absent accessors do not exist. Note: `contains` is an **infix operator** (`X contains Y`), not a member accessor — it does not appear in the accessor list. Membership tests use the infix form; see `docs/language/collection-types.md` Accessor Summary for which types support it. Follow the pattern of `QueueByHasPeekNotFirstLast` (positive + negative) for all new accessor/type combinations — covering each accessor against each type.

**Regression anchors:** All pre-existing `Types.GetMeta` entries unchanged.

**Depends on:** Slices 1–2.

---

## Slice 5 — Actions Catalog: ActionMeta Entries for New Action Kinds

**Files:** `src/Precept/Language/Actions.cs`, `src/Precept/Language/Action.cs`

### Implementation

`Actions.GetMeta(ActionKind kind)` is an exhaustive switch. Add entries for `Append`, `AppendBy`, `Insert`, `RemoveAt` (action), `Put`, `EnqueueBy`, `DequeueBy`.

**`ApplicableTo` arrays and `ProofRequirements` derive from `docs/language/collection-types.md` — check each action's section and the supported-actions matrix. Do not reproduce them inline; read the spec.**

**`AllowedIn` derives from the spec's action-context table — follow the same pattern as existing collection actions (`Add`, `Remove`, `Enqueue`, `Dequeue`). Check `Actions.GetMeta` for those entries as the pattern.**

For each new action kind, the structural metadata to record:

```
ActionKind.Append    → SyntaxShape: CollectionValue       // append F Expr
                       Token: Tokens.GetMeta(TokenKind.Append)

ActionKind.AppendBy  → SyntaxShape: CollectionValueBy     // append F Expr by P
                       Token: Tokens.GetMeta(TokenKind.Append)  // shares Append — secondary kind
                       PrimaryActionKind: ActionKind.Append

ActionKind.Insert    → SyntaxShape: InsertAt              // insert F Expr at N
                       Token: Tokens.GetMeta(TokenKind.Insert)

ActionKind.RemoveAt  → SyntaxShape: RemoveAtIndex         // remove F at N
                       Token: Tokens.GetMeta(TokenKind.Remove)  // shares Remove — secondary kind
                       PrimaryActionKind: ActionKind.Remove

ActionKind.Put       → SyntaxShape: PutKeyValue           // put F K = V
                       Token: Tokens.GetMeta(TokenKind.Put)

ActionKind.EnqueueBy → SyntaxShape: CollectionValueBy     // enqueue F Expr by P
                       Token: Tokens.GetMeta(TokenKind.Enqueue) // shares Enqueue — secondary kind
                       PrimaryActionKind: ActionKind.Enqueue

ActionKind.DequeueBy → SyntaxShape: CollectionIntoBy      // dequeue F [into G] [by H]
                       Token: Tokens.GetMeta(TokenKind.Dequeue) // shares Dequeue — secondary kind
                       IntoSupported: true
                       PrimaryActionKind: ActionKind.Dequeue
```

**`ActionKind? PrimaryActionKind` on `ActionMeta`:**

Four action-kind pairs share a leading token:

| Secondary | Primary | Shared token |
|-----------|---------|--------------|
| `AppendBy` | `Append` | `TokenKind.Append` |
| `EnqueueBy` | `Enqueue` | `TokenKind.Enqueue` |
| `DequeueBy` | `Dequeue` | `TokenKind.Dequeue` |
| `RemoveAt` | `Remove` | `TokenKind.Remove` |

For these four, `PrimaryActionKind` holds the canonical base kind. For all other action kinds, `PrimaryActionKind` is `null`.

`PrimaryActionKind` is **metadata for catalog consumers, not parser logic.** The parser uses it to understand the shared-token relationship between paired action kinds. The parser does NOT switch on `PrimaryActionKind` to decide which statement to produce — that decision is made syntactically at parse time (see the G5 note below). `PrimaryActionKind` exists so that catalog consumers (tooling, MCP vocabulary, documentation generators) can identify secondary kinds and their canonical base without hardcoding the pairing.

**Update `Actions.ByTokenKind`:**

`Actions.ByTokenKind` (keyed by `TokenKind`) must exclude secondary action kinds — those where `PrimaryActionKind != null`. This ensures the dictionary maps each token to exactly one canonical (primary) action kind, not multiple:

```csharp
public static FrozenDictionary<TokenKind, ActionMeta> ByTokenKind { get; } =
    All.Where(m => m.PrimaryActionKind == null)
       .ToFrozenDictionary(m => m.Token.Kind);
```

Without this filter, `ByTokenKind` would have duplicate key conflicts for `Append` (both `Append` and `AppendBy` map to `TokenKind.Append`).

**Note on ByTokenKind atomicity (George):** The `ByTokenKind` lookup is built at startup from `Actions.All`. Secondary action kinds (AppendBy, RemoveAt, EnqueueBy, DequeueBy) share tokens with primary kinds — verify the `ByTokenKind` filter correctly handles secondary kinds before the catalog is registered.

**Note on shared tokens and syntactic disambiguation (G5):**

`Append`/`AppendBy`, `Enqueue`/`EnqueueBy`, and `Dequeue`/`DequeueBy` each share a leading token. The parser disambiguates syntactically at parse time — no TypeChecker involvement is needed or correct. After consuming the leading verb and field name, if `TokenKind.By` is present the `*By` AST node is produced; otherwise the base node. `Remove`/`RemoveAt` share `TokenKind.Remove`; the parser checks `TokenKind.At` after the field name to select `RemoveAtStatement` vs. `RemoveStatement`. These are grammar production rules for the `CollectionValueBy`, `CollectionIntoBy`, and `RemoveAtIndex` syntax shapes — not catalog-driven disambiguation. The TypeChecker validates type compatibility post-parse (e.g., `AppendByStatement` requires the field to be `log of T by P`; `AppendStatement` on a `log of T by P` field emits `MissingOrderingKey`), but does not determine which action kind was intended. The `PrimaryActionKind` metadata captures the shared-token relationship for catalog consumers but does not drive parse-time dispatch.

**Exhaustiveness annotations (B2):**

Any existing `switch (actionKind)` statements in the codebase that handle `ActionKind` members must be extended with arms for the 7 new kinds (`Append`, `AppendBy`, `Insert`, `RemoveAt`, `Put`, `EnqueueBy`, `DequeueBy`). CS8509 is load-bearing for centralized switches on `ActionKind`.

For distributed dispatch (classes that handle one action kind per method), add supplementary exhaustiveness guards:
- `[HandlesCatalogExhaustively(typeof(ActionKind))]` on the class
- `[HandlesCatalogMember(ActionKind.X)]` on each dispatch method

This makes it a compile-time error if a new `ActionKind` is added without a corresponding handler.

**Also update existing entries:**

`Add` entry's `ApplicableTo` — currently `SetOnly`. Expand to `[Set, Bag]` (spec Action Summary: `add` applies to Set and Bag). Update `HoverDescription` accordingly.

`Remove` entry's `ApplicableTo` — currently `SetOnly`. Expand to `[Set, Bag, List, Lookup]` (spec §2.3: `remove` applies to bag and list too; `remove K` on lookup removes the entry for key K).

`Dequeue` entry's `ApplicableTo` — currently `[Queue]`. Expand to `[Queue, QueueBy]` — plain `dequeue` applies to both the classic `Queue` type and the new `QueueBy` type when used without a `by` clause.

`Clear` entry's `ApplicableTo` — currently `CollectionsAndOptional` (Set, Queue, Stack + Optional). Expand to `[Set, Queue, Stack, Bag, List, QueueBy, Optional]`. Do NOT include Log, LogBy (append-only — clear is a type error), or Lookup (has per-key `remove`; no bulk-clear). Full correct set from spec Action Summary line `clear F`: Set=✓, Queue=✓, Stack=✓, Log=✗, Bag=✓, List=✓, Queue/P=✓, Lookup=✗.

**Tests:**
- Unit: `ActionsTests.cs` — `AllNewActionKindsHaveMetaEntries`.
- Unit: `ActionsTests.cs` — `RemoveApplicableToExpandedCorrectly` — assert `Remove` entry `ApplicableTo` contains `Bag`, `List`, `Lookup`.
- Unit: `ActionsTests.cs` — `AddApplicableToIncludesBag` — assert `Add` entry `ApplicableTo` contains `Bag`.
- Unit: `ActionsTests.cs` — `DequeueApplicableToIncludesQueueBy` — assert `Dequeue` entry `ApplicableTo` contains both `Queue` and `QueueBy`.
- Unit: `ActionsTests.cs` — `ClearApplicableToIncludesBagListQueueBy` — assert `Clear` entry `ApplicableTo` contains `Bag`, `List`, `QueueBy`; does NOT contain `Log`, `LogBy`, `Lookup`.
- Unit: `ActionsTests.cs` — `SecondaryActionKindsHavePrimaryActionKind` — assert `AppendBy`, `EnqueueBy`, `DequeueBy`, `RemoveAt` all have non-null `PrimaryActionKind`; all other kinds have `null`.
- Unit: `ActionsTests.cs` — `ByTokenKindExcludesSecondaryKinds` — assert `Actions.ByTokenKind` does not contain entries for `AppendBy`, `EnqueueBy`, `DequeueBy`, `RemoveAt`.

**Existing tests that break and must be updated in this slice:**
- `ActionsTests.cs` `Total_Count` — currently `HaveCount(8)`, update to `HaveCount(15)` (7 new action kinds added).
- `ActionsTests.cs` `SetCollectionActions_ApplyToSetOnly` — `Add` and `Remove` `ApplicableTo` are expanding; update expected values to match the new sets.
- `ActionsTests.cs` `AllActions_ProofRequirements_DefaultEmpty` — 4 new action kinds (`AppendBy`, `Insert`, `RemoveAt`, `DequeueBy`) have non-empty `ProofRequirements`; update the test to exclude them or assert their requirements explicitly.
- `ActionsTests.cs` `Actions_ByTokenKind_ContainsAllActionKinds` — secondary kinds (`AppendBy`, `RemoveAt`, `EnqueueBy`, `DequeueBy`) share tokens with primary kinds; verify the test's assertion accounts for `ByTokenKind` filtering out secondary kinds.
- `ActionsTests.cs` `Clear_AppliesToCollectionsAndOptional` — currently `HaveCount(4)`, update to `HaveCount(7)` (`Bag`, `List`, `QueueBy` added to `Clear` applicability). ⚠️ *This count is a filtered subset (actions where `Clear` is applicable). It will break if catalog applicability changes. Prefer `Actions.All.Where(a => a.Verb == "clear").ApplicableTo.Count()` over a hardcoded integer where the test framework allows it.*
- `ActionsTests.cs` `QueueActions_ApplyToQueueOnly` — `Dequeue` gains `QueueBy` in `ApplicableTo`; update the assertion for `Dequeue` to include both `Queue` and `QueueBy`. ⚠️ *This is a filtered subset. Prefer deriving the expected set from the catalog rather than hardcoding type names.*
- `ActionsTests.cs` `TwoActions_SupportInto` — `DequeueBy` supports `into`; count grows from 2 to 3. Update `HaveCount(2)` → `HaveCount(3)`. ⚠️ *This count is a filtered subset (actions where `IntoSupported == true`). It will break if `into` support is added to another action. Prefer `Actions.All.Where(a => a.IntoSupported).Count()` over a hardcoded integer where the test framework allows it.*
- `ActionsTests.cs` `FiveActions_RequireValue` — 5 new action kinds require a value expression (`Append`, `AppendBy`, `Insert`, `Put`, `EnqueueBy`); count grows from 5 to 10. Update `HaveCount(5)` → `HaveCount(10)`. ⚠️ *This count is a filtered subset (actions where a value expression is required). It will break if value requirements change. Prefer `Actions.All.Where(a => a.RequiresValue).Count()` over a hardcoded integer where the test framework allows it.*
- `ActionsTests.cs` `AllowedIn_FilteredCount` (any test that filters `Actions.All.Where(a => a.AllowedIn.Contains(...))` and asserts a count) — Search `ActionsTests.cs` for any test that filters `Actions.All.Where(a => a.AllowedIn.Contains(...))` and asserts a count — that count will change when the 7 new action kinds land. Update with the correct count derived from the catalog at implementation time. Follow the same ⚠️ pattern as other filtered-subset count annotations.

**Regression anchors:** `Remove` entry change must not break existing set-remove parser tests.

**Depends on:** Slices 1–3.

---

## Slice 6 — Diagnostic Codes & Diagnostics Catalog Updates

**Files:** `src/Precept/Language/DiagnosticCode.cs`, `src/Precept/Language/Diagnostics.cs`

### DiagnosticCode changes

**Rename (same int value, new name):**
```
66: CaseInsensitiveStringOnNonCollection  →  CaseInsensitiveFieldRequiresTildeEquals
```
This code was never emitted (the parser fell into `ExpectedToken` instead). It is safe to repurpose. Update all `grep` references.

**New codes for CI enforcement (values 95+):**

These four codes cover the CI enforcement trigger conditions defined in `docs/language/precept-language-spec.md` §3.8 `~string` enforcement trigger table (lines 1381–1387 and 1595–1599). `CaseInsensitiveFieldRequiresTildeEquals` (code 66) covers the `==` case via the existing rename; codes 95–98 cover the remaining four conditions.

```
95:  CaseInsensitiveFieldRequiresTildeNotEquals    [Error]  — binary `!=` comparison where either operand is ~string (use !~ instead)
96:  CaseInsensitiveValueInCaseSensitiveContains   [Error]  — ~string value in a CS collection contains expression
97:  CaseInsensitiveFieldRequiresTildeStartsWith   [Error]  — startsWith(s, ...) call where first arg resolves to ~string (use ~startsWith)
98:  CaseInsensitiveFieldRequiresTildeEndsWith     [Error]  — endsWith(s, ...) call where first arg resolves to ~string (use ~endsWith)
```

**Message templates derive from `docs/language/precept-language-spec.md` lines 1595–1601 for CI string diagnostics and the relevant sections for other diagnostics. Do not reproduce templates inline.**

**New codes for collection safety:**

```
99:  KeyPresenceSafety         [Error]    — 'F for K' without preceding 'when F contains K' guard
100: IndexBoundsGuard          [Error]    — insert-at / remove-at-index without index-bounds guard
101: KeyUniquenessGuard        [Error]    — append-by / log-by without 'when not (F contains P)' guard
102: InvalidQuantifierTarget   [Error]    — quantifier binding var resolves to non-collection field
103: BindingShadowsField       [Error]    — quantifier binding var name shadows a precept field  
  ⚠️ Spec inconsistency: spec prose at line 1147 says `Warning: BindingShadowsField`; the diagnostics table at line 1615 says `Error`. The plan follows the diagnostics table (Error). This inconsistency should be resolved in a separate spec fix — flag when opening the TypeChecker plan.
104: MissingOrderingKey        [Error]    — log-by/queue-by type ref missing 'by P' clause
105: CollectionInnerTypeError  [Error]    — mismatched element type in collection operation
106: QuantifierPredicateNotBoolean [Error] — quantifier predicate does not resolve to bool
```

> **Codes 102 and 103 severity:** Both are `[Error]`, matching the spec (precept-language-spec.md lines 1614–1615). Do not downgrade to Warning.

### Diagnostics.cs changes

`Diagnostics.GetMeta(DiagnosticCode code)` is an exhaustive switch. Update code 66 entry text to reflect the new name and intended meaning. Add `DiagnosticMeta` entries for codes 95–106 with `Code`, `Severity`, and a description derived from the spec. Message templates come from the spec — do not invent them here.

**Stage group classification (B):** Code 66 (`CaseInsensitiveFieldRequiresTildeEquals`) belongs in the `Type` stage group — it is a type-aware check and this is correct. Codes 95–106 must also be added to the `Type` stage group in `DiagnosticsTests.cs` `MemberData` test data.

**Message templates for codes 95–106** are in `docs/language/precept-language-spec.md` — derive from there. No inline templates in the plan.

**Tests:**
- Unit: `DiagnosticsTests.cs` — `AllDiagnosticCodesHaveMetaEntries` — reflection, assert no `GetMeta` throws.
- Unit: `DiagnosticsTests.cs` — `Code66RenamedCorrectly` — assert `DiagnosticCode` does not contain a member named `CaseInsensitiveStringOnNonCollection`.
- Unit: `DiagnosticsTests.cs` — `CaseInsensitiveFieldRequiresTildeNotEquals_HasSeverityError` — asserts `DiagnosticsCode.CaseInsensitiveFieldRequiresTildeNotEquals` has `Severity == Error`.
- Unit: `DiagnosticsTests.cs` — `CaseInsensitiveFieldRequiresTildeNotEquals_HasCorrectCode` — asserts the numeric code is 95.
- Unit: `DiagnosticsTests.cs` — `CaseInsensitiveValueInCaseSensitiveContains_HasSeverityError` — asserts `DiagnosticsCode.CaseInsensitiveValueInCaseSensitiveContains` has `Severity == Error`.
- Unit: `DiagnosticsTests.cs` — `CaseInsensitiveValueInCaseSensitiveContains_HasCorrectCode` — asserts the numeric code is 96.
- Unit: `DiagnosticsTests.cs` — `CaseInsensitiveFieldRequiresTildeStartsWith_HasSeverityError` — asserts `DiagnosticsCode.CaseInsensitiveFieldRequiresTildeStartsWith` has `Severity == Error`.
- Unit: `DiagnosticsTests.cs` — `CaseInsensitiveFieldRequiresTildeStartsWith_HasCorrectCode` — asserts the numeric code is 97.
- Unit: `DiagnosticsTests.cs` — `CaseInsensitiveFieldRequiresTildeEndsWith_HasSeverityError` — asserts `DiagnosticsCode.CaseInsensitiveFieldRequiresTildeEndsWith` has `Severity == Error`.
- Unit: `DiagnosticsTests.cs` — `CaseInsensitiveFieldRequiresTildeEndsWith_HasCorrectCode` — asserts the numeric code is 98.

> **Note:** Emission tests (asserting the diagnostic fires for a given `~string` comparison input) belong in the TypeChecker plan, not here. These catalog tests verify that each entry exists with correct metadata (severity and numeric code). TypeChecker-scope emission tests for codes 95–98 are deferred to the TypeChecker implementation plan.

**Existing tests that break and must be updated in this slice:**
- `DiagnosticsTests.cs` currently references `DiagnosticCode.CaseInsensitiveStringOnNonCollection` (code 66). When code 66 is renamed to `CaseInsensitiveFieldRequiresTildeEquals`, this reference breaks with a compile error. Update the reference to `DiagnosticCode.CaseInsensitiveFieldRequiresTildeEquals` in the same commit that renames the enum member.
- `DiagnosticsTests.cs` `Diagnostics_All_CountIs_N` (if such a test exists) — adding 12 new diagnostic codes (95–106) must update the expected total to `N + 12`. Search for any `HaveCount` or `CountIs` assertion on `Diagnostics.All` and update it.

**Regression anchors:** No existing code emits code 66 (confirmed). All other existing diagnostic code values and entries unchanged.

**Depends on:** Slices 1–5.

---

## Slice 7 — ExpressionFormKind: New Expression Forms

**Files:** `src/Precept/Language/ExpressionForms.cs`, `src/Precept/Pipeline/Parser.Expressions.cs` (stub annotations only), `src/Precept/Pipeline/GraphAnalyzer.cs` (stub annotations only)

### New ExpressionFormKind members

```csharp
Quantifier       = 12,   // each/any/no Binding in Field (Predicate) — null-denotation
CIFunctionCall   = 13,   // ~startsWith(E) / ~endsWith(E) — null-denotation, Tilde prefix
```

> **Note (D):** `F for K` is a binary operator fully captured by `OperatorKind.LookupAccess` (Slice 3b). It does not require a separate `ExpressionFormKind` member — the Pratt loop handles it automatically via the `Operators` catalog.

### ExpressionForms.GetMeta entries

```csharp
ExpressionFormKind.Quantifier    => new(kind, ExpressionCategory.Composite, false,
    [TokenKind.Each, TokenKind.Any, TokenKind.No],
    "A bounded quantifier: each/any/no binding in collection (predicate)."),
ExpressionFormKind.CIFunctionCall => new(kind, ExpressionCategory.Invocation, false,
    [TokenKind.Tilde],
    "A case-insensitive function call: ~startsWith(subject, prefix) or ~endsWith(subject, suffix). Both arguments are required."),
```

### ParseSession stub annotations (build gate — required before Slices 8–14)

`ParseSession` in `Parser.cs` carries `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]`. Adding the 2 new `ExpressionFormKind` members (`Quantifier`, `CIFunctionCall`) without corresponding `[HandlesCatalogMember]` stub annotations in `ParseAtom`/`ParseExpression` is a **build-breaking change** (CS8509).

**This slice must add stub `[HandlesCatalogMember]` annotations for both new members** in `ParseAtom`/`ParseExpression` before any parser implementation in Slices 8–14 begins. The stubs do not need to produce correct AST nodes — they just need to compile. The real implementations follow in Slices 13–14.

Similarly, add `[HandlesCatalogMember]` annotations to `GraphAnalyzer.AnalyzeExpression` for both new forms (`Quantifier`, `CIFunctionCall`) — required for `[Precept.HandlesCatalogExhaustively(typeof(ExpressionFormKind))]` on that method to compile.

**Tests:**
- Unit: `ExpressionFormCatalogTests.cs` — `AllExpressionFormKindsHaveMetaEntries`.
- Unit: `ExpressionFormCatalogTests.cs` — `QuantifierIsLeftDenotationFalse` — assert `Quantifier` form has `IsLeftDenotation == false`.
- Unit: `ExpressionFormCatalogTests.cs` — `CIFunctionCallIsLeftDenotationFalse` — assert `CIFunctionCall` form has `IsLeftDenotation == false`.
- Unit: `ExpressionFormCatalogTests.cs` — `QuantifierCategory` — assert `Category == ExpressionCategory.Composite`.
- Unit: `ExpressionFormCatalogTests.cs` — `CIFunctionCallCategory` — assert `Category == ExpressionCategory.Invocation`.
- Compile-time: the `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]` attribute on `ParseSession` and `GraphAnalyzer.AnalyzeExpression` will fail to compile if any `ExpressionFormKind` member lacks a `[HandlesCatalogMember]` annotation. Adding the two annotations in this slice keeps the build green. No separate reflection test needed — the build is the test.

**Existing test that breaks and must be updated in this slice:**
- `ExpressionFormCatalogTests.cs` `ExpressionForms_All_HasExpectedCount` currently asserts `HaveCount(11)` — update to `HaveCount(13)` (2 new members: `Quantifier`, `CIFunctionCall`).

**Depends on:** Slice 1.

---

## Slice 7b — Functions Catalog: `HasCIVariant` Flag

**Files:** `src/Precept/Language/Functions.cs`

### Implementation

Add `HasCIVariant: bool` to `FunctionMeta` in `Functions.cs`. Set `HasCIVariant: true` on `FunctionKind.StartsWith` and `FunctionKind.EndsWith`. All other function kinds have `HasCIVariant: false`.

This flag allows parser and type-checker consumers to derive the set of CI-capable functions from `Functions.All.Where(f => f.HasCIVariant)` — no hardcoded list anywhere in the pipeline.

**Tests:**
- Unit: `FunctionsTests.cs` — `StartsWith_HasCIVariant_True` — assert `HasCIVariant == true` for `StartsWith`.
- Unit: `FunctionsTests.cs` — `EndsWith_HasCIVariant_True` — assert `HasCIVariant == true` for `EndsWith`.
- Unit: `FunctionsTests.cs` — `AllOtherFunctions_HasCIVariant_False` — assert all other `FunctionKind` members have `HasCIVariant: false`.

**Regression anchors:** All pre-existing function metadata entries unchanged.

**Depends on:** No direct dependency — purely a catalog metadata field addition.

---

## Slice 8 — AST Node Extensions & New Nodes

**Files:** `src/Precept/Pipeline/SyntaxNodes/TypeRefNode.cs`, `src/Precept/Pipeline/SyntaxNodes/ActionStatements.cs`, `src/Precept/Pipeline/SyntaxNodes/` (new files)

### TypeRefNode.cs changes

**1. Add `CaseInsensitive: bool` to `ScalarTypeRefNode`:**
```csharp
// Before:
public sealed record ScalarTypeRefNode(SourceSpan Span, Language.Token TypeName, ImmutableArray<TypeQualifierNode> Qualifiers) : TypeRefNode(Span);

// After:
public sealed record ScalarTypeRefNode(SourceSpan Span, Language.Token TypeName, ImmutableArray<TypeQualifierNode> Qualifiers, bool CaseInsensitive = false) : TypeRefNode(Span);
```

**2. Add DU subtypes for two-parameter collection type refs (for log-by, queue-by, lookup):**

Three distinct subtypes — each carries exactly the fields its consumers need. This follows the DU principle for varying metadata shapes.

```csharp
// LogByTypeRefNode: no SortDirection (log-by has no ordering direction)
public sealed record LogByTypeRefNode(
    SourceSpan     Span,
    Language.Token ElementType,       // T in "log of T by P"
    Language.Token OrderingKeyType,   // P (any comparable scalar)
    bool           CaseInsensitive,
    ImmutableArray<TypeQualifierNode> Qualifiers
) : TypeRefNode(Span);

// QueueByTypeRefNode: carries SortDirection (queue-by is a priority queue with direction)
public sealed record QueueByTypeRefNode(
    SourceSpan     Span,
    Language.Token ElementType,       // T in "queue of T by P"
    Language.Token OrderingKeyType,   // P (any comparable scalar)
    SortDirection  SortDirection,     // ascending/descending
    bool           CaseInsensitive,
    ImmutableArray<TypeQualifierNode> Qualifiers
) : TypeRefNode(Span);

// LookupTypeRefNode: key/value types, no SortDirection
public sealed record LookupTypeRefNode(
    SourceSpan     Span,
    Language.Token KeyType,           // K in "lookup of K to V"
    Language.Token ValueType,         // V
    bool           CaseInsensitive,   // K only — Lookup keys may be ~string, values are always typed normally
    ImmutableArray<TypeQualifierNode> Qualifiers
) : TypeRefNode(Span);

public enum SortDirection { Ascending = 1, Descending = 2 }
```

### ActionStatements.cs new nodes

Append the following records to `ActionStatements.cs`:

```csharp
// append F Expr  (log of T, list of T)
public sealed record AppendStatement(SourceSpan Span, Language.Token Field, Expression Value) : Statement(Span);

// append F Expr by P  (log of T by P)
public sealed record AppendByStatement(SourceSpan Span, Language.Token Field, Expression Value, Expression Key) : Statement(Span);

// insert F Expr at N  (list of T)
public sealed record InsertAtStatement(SourceSpan Span, Language.Token Field, Expression Value, Expression Index) : Statement(Span);

// remove F at N  (list of T)
public sealed record RemoveAtStatement(SourceSpan Span, Language.Token Field, Expression Index) : Statement(Span);

// put F K = V  (lookup of K to V)
public sealed record PutStatement(SourceSpan Span, Language.Token Field, Expression Key, Expression Value) : Statement(Span);

// enqueue F Expr by P  (queue of T by P)
public sealed record EnqueueByStatement(SourceSpan Span, Language.Token Field, Expression Value, Expression Key) : Statement(Span);

// dequeue F [into G] [by H]  (queue of T by P)
public sealed record DequeueByStatement(SourceSpan Span, Language.Token Field, Language.Token? Into, Language.Token? ByBinding) : Statement(Span);
```

### New expression AST nodes (new file: `src/Precept/Pipeline/SyntaxNodes/QuantifierExpression.cs`)

```csharp
/// <summary>
/// A quantifier expression: each/any/no Binding in CollectionField (Predicate).
/// </summary>
public sealed record QuantifierExpression(
    SourceSpan     Span,
    Language.Token Quantifier,      // each / any / no token
    Language.Token BindingVar,      // identifier declared as iteration variable
    Language.Token CollectionField, // the field being iterated
    Expression     Predicate        // boolean expression; BindingVar in scope
) : Expression(Span);

/// <summary>
/// A case-insensitive function call: ~startsWith(Subject, Argument) or ~endsWith(Subject, Argument).
/// Both Subject and Argument are required, non-nullable. Return type: boolean.
/// The Tilde prefix signals case-insensitive matching; the first arg must be ~string (type-checker enforces).
/// </summary>
public sealed record CIFunctionCallExpression(
    SourceSpan     Span,
    FunctionKind   Kind,            // FunctionKind.StartsWith or FunctionKind.EndsWith
    Expression     Subject,         // first argument — the ~string field/expression being tested
    Expression     Argument         // second argument — the prefix or suffix pattern string
) : Expression(Span);
```

### ExpressionFormKind `[HandlesCatalogMember]` annotation

GraphAnalyzer stub annotations for `Quantifier` and `CIFunctionCall` are specified in Slice 7 — no duplicate annotation block needed here. Follow the pattern in `GraphAnalyzer.cs` for the existing `AnalyzeExpression` method stub.

**Tests:**
- Unit: `AstNodeTests.cs` — `ScalarTypeRefNodeCaseInsensitiveDefaultsFalse` — construct `ScalarTypeRefNode` with no explicit CI flag, verify `CaseInsensitive == false`.
- Unit: `AstNodeTests.cs` — `LogByTypeRefNodeRoundTrip` — construct and assert `ElementType`/`OrderingKeyType` accessible; verify no `SortDirection` field.
- Unit: `AstNodeTests.cs` — `QueueByTypeRefNodeRoundTrip` — construct and assert `SortDirection` accessible alongside `ElementType`/`OrderingKeyType`.
- Unit: `AstNodeTests.cs` — `LookupTypeRefNodeRoundTrip` — construct and assert `KeyType`/`ValueType` accessible.
- Unit: `AstNodeTests.cs` — `AppendByStatementFields` — construct and assert Key/Value/Field accessible.

**Regression anchors:** All existing `ScalarTypeRefNode` construction sites now pass `CaseInsensitive: false` implicitly (default). If any site passes positional args, verify arg count doesn't break. Search `new ScalarTypeRefNode(` in the parser and update any positional constructors.

**Depends on:** Slices 1–7.

---

## Slice 9 — Lexer: Verify Keyword Recognition (Test-Only Slice)

**Files:** `src/Precept/Pipeline/Lexer.cs` — **no edits required**

### Implementation

**No `Lexer.cs` edits needed for keyword recognition.** `Tokens.Keywords` is a computed `FrozenDictionary` derived automatically from `Tokens.All` — it is not a hand-maintained table. The lexer constructor binds to it directly:

```csharp
// Catalog-driven: Tokens.Keywords is the single source of truth for keyword
// recognition. Do not add parallel keyword arrays here — add to the Tokens catalog.
_keywordLookup = Tokens.Keywords.GetAlternateLookup<ReadOnlySpan<char>>();
```

Once Slice 3 adds `TokenMeta` entries with the correct `TokenCategory` values (e.g., `Cat_Type`, `Cat_Act`, `Cat_Prep`), every new token automatically appears in `Tokens.Keywords` and the lexer picks it up with no further change. The same catalog-driven pattern applies to `KeywordsValidAsMemberName` (relevant for `countof` and `peekby`).

This slice exists solely to confirm that Slice 3's catalog entries produce correct lexer output end-to-end. If the tests pass, keyword recognition is working correctly.

**9a. `remove`/`at` disambiguation** — `remove F at N` uses the existing `Remove` keyword followed by `At`. No special lexer lookahead is required. The parser dispatches on `TokenKind.Remove` and peeks at the token after the field; if it is `At`, it produces `RemoveAtStatement`, otherwise `RemoveStatement`. This is entirely a parser-level disambiguation — the lexer needs no change.

**9b. `Peekby` lexing note** — The token text is `"peekby"` (one word, no hyphen), matching the spec. `docs/language/collection-types.md` uses `.peekby` consistently throughout the accessor matrix, usage examples, and rationale. No compound-token pattern is needed.

**Tests:**
- `LexerTests.cs`: `RemoveWithoutAtIsSimple` — `"remove F E"` → token sequence `[Remove, Identifier, Identifier]`.
- `LexerTests.cs`: `EachTokenized` — `"each"` → `TokenKind.Each`.
- `LexerTests.cs`: `ForTokenized` — `"for"` → `TokenKind.For`.
- `LexerTests.cs`: `ToTokenized` — `"to"` → `TokenKind.To`.
- `LexerTests.cs`: `ToInsideIdentifierNotSpurious` — `"total"` → a single `Identifier` token, not a `To` token followed by `Identifier("tal")`. Verifies the keyword recognizer only matches on word boundaries.

**Regression anchors:** All existing lexer tests pass. `"remove"` without `at` must not produce `RemoveAt`. All pre-existing keyword tokens must produce same kinds as before.

**Depends on:** Slice 1 (token kind values), Slice 3 (Tokens.GetMeta has entries for new kinds — this slice verifies those entries lex correctly).

---

## Slice 10 — Parser: Scalar `~string` in ParseTypeRef

**Files:** `src/Precept/Pipeline/Parser.Declarations.cs`

### Implementation

`ParseTypeRef()` currently starts at line 805. The current dispatch:
1. `Set | QueueType | StackType` → `CollectionTypeRefNode`
2. `ChoiceType` → `ChoiceTypeRefNode`
3. `TypeKeywords` → `ScalarTypeRefNode`
4. else → `Diagnostic.UnknownType` error recovery

`Tilde` is not in `TypeKeywords`, so `field Email as ~string` currently falls to path 4 and emits a parser error.

**Add new branch before the TypeKeywords check:**

Add a branch in `ParseTypeRef()`: if the current token is `Tilde`, consume it, then expect `TokenKind.StringType` (emitting `ExpectedToken` on mismatch with error recovery — `~` as a type prefix is only valid before `string`; any other type is a syntax error at the `~` position), consume the type token, parse qualifiers, and return a `ScalarTypeRefNode` with `CaseInsensitive: true`. Follow existing parser method conventions — read `Parser.cs` for actual API names (`Current()`, `EmitDiagnostic()`, `Advance()`/`Expect()`, `ParseExpression(0)`).

**Invariant:** `CaseInsensitive: true` on `ScalarTypeRefNode` means the field uses case-insensitive equality/membership. The stored value is case-preserving; comparison behavior is altered.

**Tests:**
- `ParserTests.cs`: `ScalarTildeString_Parses` — `"field Email as ~string"` → `ScalarTypeRefNode` with `TypeName.Kind == StringType` and `CaseInsensitive == true`.
- `ParserTests.cs`: `ScalarTildeNonString_EmitsDiagnostic` — `"field X as ~integer"` → `ExpectedToken` diagnostic (or the appropriate syntax-error diagnostic code). `~` is only valid before `string` in type-ref position; `~integer` is a parse error at the non-string type token. Do **not** assert code 66 here — code 66 (`CaseInsensitiveFieldRequiresTildeEquals`) is a type-checker enforcement message about comparison operators on CI fields, not a type-declaration syntax error.
- `ParserTests.cs`: `ScalarNoTilde_CaseInsensitiveFalse` — `"field Email as string"` → `ScalarTypeRefNode` with `CaseInsensitive == false`.

**Regression anchors:** All GAP-3 tests at lines 1677–1719 (set of ~string collection) must still pass. The Tilde branch in `ParseTypeRef` must only fire for Tilde in *scalar* position — the existing collection branch (`Set | QueueType | StackType` with inner-type Tilde) must not be affected.

**Depends on:** Slices 1–8.

---

## Slice 11 — Parser: New Collection Type Refs in ParseTypeRef

**Files:** `src/Precept/Pipeline/Parser.Declarations.cs`

### Implementation

Extend `ParseTypeRef()` to handle `BagType`, `ListType`, `LogType`, and `LookupType`, and extend the existing `QueueType` branch to handle the `by P` clause.

**Current `Set|QueueType|StackType` branch — extend to include `BagType`, `ListType`, `LogType`:**

Extend the existing collection branch to match `BagType`, `ListType`, and `LogType` in addition to the existing set/queue/stack tokens. For each: expect `Of`, optionally consume `Tilde` (set CI flag), parse the element type token, parse qualifiers. For `LogType`, additionally check for an optional `By` followed by the ordering key type token — if present, return a `LogByTypeRefNode`; otherwise return a `CollectionTypeRefNode`. Follow existing parser method conventions — read `Parser.cs` for actual API names.

**New `QueueType + by` branch** (within or after the existing QueueType arm):

Extend the existing `QueueType` arm: after parsing the element type, check for `By`. If present, parse the ordering key type token, then check for optional `Ascending`/`Descending` to determine `SortDirection` (default `Ascending`). Return a `QueueByTypeRefNode`. If no `By`, fall through to the existing `CollectionTypeRefNode` path. Follow existing parser method conventions.

**New `LookupType` branch:**

Add a `LookupType` case: expect `Of`, then parse the key type using `ParseTypeRef()` — which already handles `~string` from the scalar CI string work (Slice 10). No special-casing needed for `lookup of ~string to V`. Then expect `To`, parse the value type using `ParseTypeRef()`, return a `LookupTypeRefNode`. Follow existing parser method conventions.

**Missing `by` clause on log-by — emit `MissingOrderingKey` (code 104):** If `LogType` type ref appears in a field context but has no `by` clause, the field gets `TypeKind.Log` (plain log). If `QueueType` has `by` clause → `QueueBy`. If `QueueType` without `by` → `Queue` (pre-existing behavior). `MissingOrderingKey` is only emitted by the TypeChecker, not the parser — the parser produces a `CollectionTypeRefNode` with kind `Log` and the TypeChecker checks whether the field's use is consistent.

**Tests:**
- `ParserTests.cs`: `BagOf_Parses` — `"field T as bag of string"` → `CollectionTypeRefNode` with `CollectionKind.Kind == BagType`.
- `ParserTests.cs`: `ListOf_Parses` — similar for list.
- `ParserTests.cs`: `LogOf_Parses` — `"field L as log of string"` → `CollectionTypeRefNode(LogType, StringType)`.
- `ParserTests.cs`: `LogOfBy_Parses` — `"field L as log of string by integer"` → `LogByTypeRefNode` with `ElementType.Kind == StringType` and `OrderingKeyType.Kind == IntegerType`.
- `ParserTests.cs`: `QueueByAscending_Parses` — `"field Q as queue of string by integer ascending"` → `QueueByTypeRefNode` with `SortDirection.Ascending`.
- `ParserTests.cs`: `QueueByDescending_Parses` — same with descending.
- `ParserTests.cs`: `LookupOf_Parses` — `"field M as lookup of string to integer"` → `LookupTypeRefNode` with `KeyType.Kind == StringType` and `ValueType.Kind == IntegerType`.
- `ParserTests.cs`: `LookupOfCIKey_Parses` — `"field M as lookup of ~string to integer"` → `LookupTypeRefNode` with `CaseInsensitive: true`.
- `ParserTests.cs`: `BagOfTildeString_Parses` — `"field T as bag of ~string"` → `CollectionTypeRefNode` with `CaseInsensitive: true`.
- `ParserTests.cs`: `LookupMissingToClause_EmitsDiagnostic` — `"field M as lookup of string"` (missing `to V`) → diagnostic emitted.
- `ParserTests.cs`: `LogByMissingKeyType_EmitsDiagnostic` — `"field L as log of string by"` (missing key type after `by`) → diagnostic emitted (parse error — `by` without a following type token).
- `ParserTests.cs`: `QueueByMissingKeyType_EmitsDiagnostic` — `"field Q as queue of string by"` (missing key type after `by`) → diagnostic emitted (parse error — `by` without a following type token).
- `ParserTests.cs`: `LookupAccessMissingFor_EmitsDiagnostic` — expression `"Cache \"key\""` (lookup field followed by key, `for` keyword omitted) wrapped in a transition row → diagnostic emitted (the `for` keyword is required for key access syntax; its absence is a parse error or produces an unexpected token diagnostic).

**Regression anchors:** Existing `set of ~string` GAP-3 tests unchanged. Existing queue/stack type ref tests unchanged.

**Depends on:** Slices 1–9 (particularly Slice 8 for new AST nodes).

---

## Slice 12 — Parser: New Action Statements

**Files:** `src/Precept/Pipeline/Parser.Declarations.cs`

### Implementation

`ParseActionStatement()` is a dispatch on the current token kind. The set of valid action-leading tokens is derived from `Actions.All` (the `ActionKeywords` FrozenSet). New action tokens (`Append`, `Insert`, `RemoveAt`, `Put`) are **automatically included** in `ActionKeywords` once their `ActionMeta` entries are added to `Actions.GetMeta()` (Slice 5) because the parser derives the set from the catalog.

The parser needs **parsing logic** for each new shape. Follow existing parser method conventions — read `Parser.cs` for actual API names (`Current()`, `EmitDiagnostic()`, `Advance()`/`Expect()`, `ParseExpression(0)`). Structural intent for each shape:

**`CollectionValue` (Append — log/list):** Add a case for `TokenKind.Append`: consume verb, consume field identifier, parse the value expression, then check for optional `By` — if present, consume it, parse the key expression, and return an `AppendByStatement`; otherwise return an `AppendStatement`.

**`InsertAt` (Insert):** Add a case for `TokenKind.Insert`: consume verb, consume field, parse value expression, expect `At`, parse index expression, return `InsertAtStatement`.

**`RemoveAtIndex` (Remove — trailing `at` disambiguator):** Extend the existing `TokenKind.Remove` case: after consuming verb and field, check if the next token is `At` — if so, consume it, parse the index expression, and return `RemoveAtStatement`; otherwise parse the value expression and return `RemoveStatement` (existing path).

**`PutKeyValue` (Put):** Add a case for `TokenKind.Put`: consume verb, consume field, parse key expression, expect `Assign`, parse value expression, return `PutStatement`.

**Extend `Enqueue` case:** After parsing the value expression, check for optional `By` — if present, parse the key expression and return `EnqueueByStatement`; otherwise return `EnqueueStatement` (existing path).

**Extend `Dequeue` case:** After consuming verb and field, check for optional `Into` (consume and save the field token), then check for optional `By` (consume and save the binding identifier) — if `By` was present, return `DequeueByStatement`; otherwise return `DequeueStatement` (existing path).

**Tests:**
- `ParserTests.cs`: `AppendStatement_Parses` — `"from S on E -> append Items \"x\""` → `AppendStatement`.
- `ParserTests.cs`: `AppendByStatement_Parses` — `"from S on E -> append Events e by e.ts"` → `AppendByStatement`.
- `ParserTests.cs`: `InsertAtStatement_Parses` — `"from S on E -> insert Items \"x\" at 0"` → `InsertAtStatement` with `Index` of `0`.
- `ParserTests.cs`: `RemoveAtStatement_Parses` — `"from S on E -> remove Items at 2"` → `RemoveAtStatement`.
- `ParserTests.cs`: `PutStatement_Parses` — `"from S on E -> put Cache \"key\" = 42"` → `PutStatement`.
- `ParserTests.cs`: `EnqueueByStatement_Parses` — `"from S on E -> enqueue Q task by task.priority"` → `EnqueueByStatement`.
- `ParserTests.cs`: `DequeueByStatement_With_Into_And_By_Parses` — `"from S on E -> dequeue Q into G by H"` — test optional `into`/`by` routing.
- `ParserTests.cs`: `DequeueStatement_WithoutBy_StillProducesDequeueStatement` — `"from S on E -> dequeue Q"` — regression: plain dequeue unchanged.
- `ParserTests.cs`: `EnqueueStatement_WithoutBy_StillProducesEnqueueStatement` — `"from S on E -> enqueue Q item"` — regression.
- `ParserTests.cs`: `InsertAtMissingAt_EmitsDiagnostic` — `"from S on E -> insert Items \"x\""` (missing `at N`) → diagnostic emitted.
- `ParserTests.cs`: `PutMissingValue_EmitsDiagnostic` — `"from S on E -> put Cache \"key\""` (missing `= V`) → diagnostic emitted.
- `ParserTests.cs`: `AppendMissingValue_EmitsDiagnostic` — `"from S on E -> append Items"` (missing value expression after field name) → diagnostic emitted (parse error — `append` requires a value expression).

**Regression anchors:** All existing `Enqueue`, `Dequeue`, `Add`, `Remove`, `Push`, `Pop`, `Clear` action statement tests must pass unchanged (lines 1590–1675).

**Depends on:** Slices 1–10.

---

## Slice 13 — Parser: `~startsWith`/`~endsWith` in Expression Position

**Files:** `src/Precept/Pipeline/Parser.Expressions.cs`

### Implementation

In `ParseAtom()` (the Pratt parser's null-denotation dispatch), add a case for `TokenKind.Tilde`: consume the tilde, then validate that the next token is an identifier whose text matches a CI-capable function. **Derive the valid set from `Functions.All.Where(f => f.HasCIVariant)` — do not hardcode `"startsWith"`/`"endsWith"`.** If not valid, emit the appropriate parser-level error (use the existing `ExpectedToken` diagnostic or equivalent — no dedicated code is defined in the spec for this error position; follow the existing parser's error recovery conventions for "expected X, got Y") and return an error expression. Otherwise, consume the function name identifier (resolving it to a `FunctionKind`), then expect `(`, parse the first expression (Subject), expect `,` (missing `,` or second arg is a parse error), parse the second expression (Argument), expect `)`, and return a `CIFunctionCallExpression(Kind, Subject, Argument)`. Both `Subject` and `Argument` are required and non-nullable. Follow existing parser method conventions — read `Parser.cs` for actual API names (`Current()`, `EmitDiagnostic()`, `Advance()`/`Expect()`, `ParseExpression(0)`).

**Disambiguation note:** `Tilde` currently also appears in *type-ref position* (inside `ParseTypeRef()`). In `ParseAtom()`, `Tilde` is only reachable in expression context (after a guard `when`, inside an `if`/`then`/`else`, or inside a quantifier predicate). There is no ambiguity because `ParseAtom` is never called from `ParseTypeRef`.

**Tests:**
- `ParserTests.cs`: `TildeStartsWith_InWhenGuard` — `"when ~startsWith(Email, \"@\")"` wrapped in a full transition row → expression contains `CIFunctionCallExpression` with `Kind == FunctionKind.StartsWith`, `Subject` populated (the `Email` identifier expression), and `Argument` populated (the `"@"` string literal).
- `ParserTests.cs`: `TildeEndsWith_InExpression` — `"when ~endsWith(Email, \".com\")"` wrapped in a full transition row → `CIFunctionCallExpression` with `Kind == FunctionKind.EndsWith`, both `Subject` and `Argument` fields populated.
- `ParserTests.cs`: `TildeStartsWith_BothFieldsPopulated` — assert AST test: construct or parse a CI function call and assert both `Subject` and `Argument` are non-null and of correct expression types.
- `ParserTests.cs`: `TildeStartsWith_OneArgOnly_EmitsDiagnostic` — `"when ~startsWith(Email)"` (missing `,` and second argument) → parse error diagnostic emitted (missing second argument is a syntax error).
- `ParserTests.cs`: `TildeOnNonFunction_EmitsDiagnostic` — tilde followed by a non-CI-function identifier → diagnostic emitted.

**Test wrapping note (Soup):** Expression tests must be wrapped in a full transition row context (`from S on E -> ...`) — bare expressions do not parse standalone in `ParserTests.cs`. Use `ExpressionParserTests.cs` if that file exists, or wrap in transition rows.

**Regression anchors:** The Tilde in collection type-ref position (`set of ~string`) must not be affected. Verify GAP-3 tests at lines 1677–1719 still pass.

**Depends on:** Slices 1–8, 7b (for `Functions.HasCIVariant` — must be available when the parser resolves CI-function names).

---

## Slice 14 — Parser: Quantifier Expressions

**Files:** `src/Precept/Pipeline/Parser.Expressions.cs`

### Implementation

In `ParseAtom()`, add cases for `TokenKind.Each`, `TokenKind.Any` (dual-role), and `TokenKind.No` (dual-role):

**Design:** `any` and `no` serve two roles:
- As state wildcards / `no transition` outcome: these appear in *declaration position* (parsed by `Parser.Declarations.cs`), never in expression position.
- As quantifiers: these appear only in *expression position* (after `when`, inside `if cond`, inside predicates).

Therefore the Pratt parser's `ParseAtom()` safely handles `any` and `no` as quantifiers without ambiguity. Declarations never reach `ParseAtom()`.

> **Spec cite:** The dual-role disambiguation for `any` and `no` (quantifier vs. state-wildcard/transition-outcome) is defined in spec §2.1 Null-denotation table (`docs/language/precept-language-spec.md` line 709): "if the token after `any`/`no` is `Identifier` followed by `in` followed by `CollectionRef` followed by `(`, parse as quantifier; otherwise `any` continues as type/state modifier, `no` continues as transition keyword." Follow that disambiguation logic exactly. See also the token vocabulary notes at lines 273 and 453.

In `ParseAtom()`, add cases for `TokenKind.Each`, `TokenKind.Any`, and `TokenKind.No`: consume the quantifier token, consume the binding variable identifier, expect `In`, consume the collection field identifier, parse a parenthesized predicate expression (binding var in scope), and return a `QuantifierExpression`. Follow existing parser method conventions — read `Parser.cs` for actual API names (`Current()`, `EmitDiagnostic()`, `Advance()`/`Expect()`, `ParseExpression(0)`).

**`For` infix in the Pratt loop (left-denotation):**

Once `OperatorKind.LookupAccess` is in the `Operators` catalog (added in Slice 3b), the Pratt loop handles `F for K` **automatically** — `Operators.ByToken[(TokenKind.For, Arity.Binary)]` resolves to `LookupAccess` with `Precedence: 40`, and the generic infix handler produces a `BinaryExpression`. **No explicit `case TokenKind.For:` is needed in `ParseInfix`** — adding one would duplicate catalog-driven behavior and violate the non-negotiable architectural constraints.

The TypeChecker (separate plan) validates that the left operand resolves to a `Lookup` field and enforces `KeyPresenceSafety`.

**Tests:**
- `ParserTests.cs`: `EachQuantifier_Parses` — quantifier expression wrapped in a full transition row → `QuantifierExpression(Each, ...)`.
- `ParserTests.cs`: `AnyQuantifier_InExpression` — `any` quantifier with CI-function predicate wrapped in a transition row → `QuantifierExpression(Any, ...)` with `CIFunctionCallExpression` predicate.
- `ParserTests.cs`: `NoQuantifier_InExpression` — `no` quantifier wrapped in a transition row.
- `ParserTests.cs`: `ForInfix_Parses` — `"Cache for \"key\""` wrapped in a transition row → `BinaryExpression(op=For)`.
- `ParserTests.cs`: `QuantifierMissingIn_EmitsDiagnostic` — quantifier without `in` keyword → diagnostic emitted.
- `ParserTests.cs`: `AnyAsStateWildcard_NotAffected` — regression: existing `any` in state declaration position still parses correctly.
- `ParserTests.cs`: `NoTransitionOutcome_NotAffected` — regression: `"→ no transition"` still parses correctly.

**Test wrapping note (Soup):** Expression tests must be wrapped in a full transition row context (`from S on E -> ...`) — bare expressions do not parse standalone in `ParserTests.cs`. Use `ExpressionParserTests.cs` if that file exists, or wrap in transition rows.

**Regression anchors:** All existing expression tests (lines 1100–1350). `no transition` outcome, `any` state wildcard — these are in declaration context; confirm they are unaffected.

**Depends on:** Slices 1–13.

---

## File Inventory

| File | Status | Slices |
|---|---|---|
| `src/Precept/Language/TokenKind.cs` | Modify | 1 |
| `src/Precept/Language/TypeKind.cs` | Modify | 2 |
| `src/Precept/Language/ActionKind.cs` | Modify | 2 |
| `src/Precept/Language/Action.cs` | Modify | 2, 5 |
| `src/Precept/Language/Tokens.cs` | Modify | 3 |
| `src/Precept/Language/OperatorKind.cs` | Modify | 3b |
| `src/Precept/Language/Operators.cs` | Modify | 3b |
| `src/Precept/Language/Types.cs` | Modify | 4 |
| `src/Precept/Language/Actions.cs` | Modify | 5 |
| `src/Precept/Language/DiagnosticCode.cs` | Modify | 6 |
| `src/Precept/Language/Diagnostics.cs` | Modify | 6 |
| `src/Precept/Language/ExpressionForms.cs` | Modify | 7 |
| `src/Precept/Language/Functions.cs` | Modify | 7b |
| `src/Precept/Pipeline/SyntaxNodes/TypeRefNode.cs` | Modify | 8 |
| `src/Precept/Pipeline/SyntaxNodes/ActionStatements.cs` | Modify | 8 |
| `src/Precept/Pipeline/SyntaxNodes/QuantifierExpression.cs` | **Create** | 8 |
| `src/Precept/Pipeline/GraphAnalyzer.cs` | Modify (annotations only) | 7 |
| `src/Precept/Pipeline/Lexer.cs` | No edits | 9 |
| `src/Precept/Pipeline/Parser.Declarations.cs` | Modify | 10, 11, 12 |
| `src/Precept/Pipeline/Parser.Expressions.cs` | Modify | 7 (stubs), 13, 14 |
| `test/Precept.Tests/LexerTests.cs` | Modify | 1, 9 |
| `test/Precept.Tests/TokensTests.cs` | Modify | 3 |
| `test/Precept.Tests/TokenMetaMemberNameTests.cs` | Modify | 3 |
| `test/Precept.Tests/OperatorsTests.cs` | Modify | 3b |
| `test/Precept.Tests/TypesTests.cs` | Modify | 4 |
| `test/Precept.Tests/ActionsTests.cs` | Modify | 5 |
| `test/Precept.Tests/DiagnosticsTests.cs` | Modify | 6 |
| `test/Precept.Tests/ExpressionFormCatalogTests.cs` | Modify | 7 |
| `test/Precept.Tests/FunctionsTests.cs` | Modify | 7b |
| `test/Precept.Tests/AstNodeTests.cs` | Modify | 8 |
| `test/Precept.Tests/ParserTests.cs` | Modify | 10–14 |

---

## Implementation Checklist

### Phase I: Catalog Infrastructure

- [ ] Slice 1 — TokenKind additions (16 new tokens)
- [ ] Slice 2 — TypeKind (6) + ActionKind (7) + ActionSyntaxShape (5) additions
- [ ] Slice 3 — Tokens.GetMeta entries for all new TokenKinds (`Countof`/`Peekby` category: `Cat_Cns`)
- [ ] Slice 3b — OperatorKind.LookupAccess + Operators.GetMeta entry (token=For, BP=40, left, binary)
- [ ] Slice 4 — Types.GetMeta entries for Log, LogBy, Bag, List, QueueBy, Lookup
- [ ] Slice 5 — Actions.GetMeta entries + AllowedIn + PrimaryActionKind + Remove/Add/Dequeue/Clear ApplicableTo expansion
- [ ] Slice 6 — DiagnosticCode rename + new codes 95–106 + Diagnostics.GetMeta entries (codes 95–106 in Type stage group in DiagnosticsTests.cs)
- [ ] Slice 7 — ExpressionFormKind additions (Quantifier, CIFunctionCall) + ParseSession + GraphAnalyzer stub annotations
- [ ] Slice 7b — Functions.cs: `HasCIVariant: bool` on FunctionMeta; `StartsWith`/`EndsWith` set to true

### Phase II: AST + Parser

- [ ] Slice 8 — AST: ScalarTypeRefNode.CaseInsensitive, DU subtypes (LogByTypeRefNode / QueueByTypeRefNode / LookupTypeRefNode), new action nodes, QuantifierExpression, CIFunctionCallExpression
- [ ] Slice 9 — Lexer: verify keyword recognition (test-only; no `Lexer.cs` edits — catalog-driven via Slice 3)
- [ ] Slice 10 — Parser: scalar `~string` in ParseTypeRef
- [ ] Slice 11 — Parser: new collection type refs (bag, list, log, log-by, queue-by, lookup)
- [ ] Slice 12 — Parser: new action statements (append, append-by, insert-at, remove-at-index, put, enqueue-by, dequeue-by)
- [ ] Slice 13 — Parser: `~startsWith`/`~endsWith` in expression position
- [ ] Slice 14 — Parser: quantifier expressions + `for` infix

---

## Critical Risk Factors

1. **Exhaustive switch breaks** — every addition to `TokenKind`, `TypeKind`, `ActionKind`, `DiagnosticCode`, or `ExpressionFormKind` will break compilation until the corresponding `GetMeta` switch arm is added. Slices 1–7 must be landed together (or Slices 1, then 3; 2, then 4,5; etc.) to keep the project compilable.

2. **`ScalarTypeRefNode` positional constructor** — adding `CaseInsensitive` as a trailing optional parameter is backward-compatible only if all call sites use named or positional args that don't break. Search all `new ScalarTypeRefNode(` usages before landing Slice 8.

3. **`remove F at N` parser disambiguation** — the `Remove` case in the action parser must peek at the token after the field: if `At`, produce `RemoveAtStatement`; otherwise fall through to `RemoveStatement`. The lookahead is one token and unambiguous, but the existing `Remove` switch arm must be extended without breaking `remove F Expr`. Regression tests for plain `remove` are critical.

4. **`any` and `no` dual roles** — parser disambiguation by context. If any existing test feeds `any` or `no` in expression position through the parser, those tests need to verify behavior hasn't changed after Slice 14 adds quantifier handling.

5. **Tooling sync** — This plan adds new catalog entries (new TypeKinds, ActionKinds, TokenKinds). The TextMate grammar generator, completions provider, semantic tokens emitter, and hover provider derive directly from catalog metadata. Once catalog entries are landed (Slices 1–5), run the grammar generator to regenerate `precept.tmLanguage.json` and verify completions/hover against the new keywords. MCP sync is N/A — the MCP server is not yet built. TypeChecker sync is N/A — TypeChecker is being built in a separate plan.
