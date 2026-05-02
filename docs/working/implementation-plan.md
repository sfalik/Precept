# Precept V2 — Collection Types & Scalar `~string` Implementation Plan

**Status:** Draft  
**Scope:** 6 feature areas — scalar `~string`, bag, list, log, log-by, queue-by, lookup  
**Design docs:** `docs/language/primitive-types.md`, `docs/language/collection-types.md`  
**Baseline:** Clean working tree (confirmed `git diff HEAD -- src/` is empty)

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

All four Phase 3 pipeline stages are stubs. Every slice from Slice 11 onward is a first-time implementation of a stage that does not exist yet. The catalog-first constraint means enum additions **must** come before all metadata entries, which **must** come before parser/stage implementations.

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

Slice 2 ─┬─► Slice 4 (Types.GetMeta)
          └─► Slice 5 (Actions.GetMeta/ActionSyntaxShape)

Slices 1–5 ──► Slice 6 (DiagnosticCode/ExpressionFormKind)

Slices 1–6 ──► Slice 7 (AST nodes)

Slices 1–7 ──► Slices 8–12 (Parser slices)

Slices 1–12 ──► Slice 13 (TypeChecker core)

Slice 13 ──► Slices 14–17 (TypeChecker rules)

Slices 13–17 ──► Slice 18 (ProofEngine core)

Slice 18 ──► Slices 19–20 (ProofEngine rules)

Slices 13–20 ──► Slice 21 (GraphAnalyzer)

Slices 13–21 ──► Slices 22–23 (Evaluator)
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
Put          = 135,   // "put"        — lookup upsert

// New quantifier keyword
Each         = 136,   // "each"

// New lookup access operator  
For          = 137,   // "for"   — infix key accessor: F for K

// New member-name tokens (valid as identifiers after dot)
Countof      = 138,   // "countof"  — .countof(E) on all collections
Peekby       = 139,   // "peekby"   — .peekby on queue-by (peek-by key)
```

**Design notes:**
- `by`, `at`, `for`, `ascending`, `descending` are new reserved keywords. They are **not** valid as field names. The language spec intentionally reserves them.
- `remove F at N` uses the existing `Remove` token plus the contextual `At` token — there is no `remove-at` compound keyword. The parser dispatches on `TokenKind.Remove` and checks whether the next token after the field is `At`; if so it parses the positional form, otherwise it falls through to `remove F Expr`.
- `countof` and `peekby` are added to `KeywordsValidAsMemberName` in `Tokens.cs` so they appear in member-access position after `.`.
- `each` is new. `any` (44) and `no` (32) already exist but serve dual roles (state wildcard / quantifier and `no transition` / quantifier respectively). Disambiguation is handled in the parser by context (declaration vs. expression position).

**Tests:**
- `LexerTests.cs`: `TokenizesNewKeywords` — verify `bag`, `list`, `log`, `lookup`, `by`, `at`, `ascending`, `descending`, `append`, `insert`, `put`, `each`, `for`, `countof`, `peekby` each lex to their new kind.
- `LexerTests.cs`: `NewKeywordsAreNotValidIdentifiers` — verify `bag`, `list`, `log`, `lookup`, `by`, `at`, `ascending`, `descending`, `append`, `insert`, `put`, `each`, `for` each produce a keyword token, not `Identifier`.

**Regression anchors:** All pre-existing lexer tests must still pass unchanged.

**Depends on:** Nothing (first slice).

---

## Slice 2 — TypeKind & ActionKind Enum Additions

**Files:** `src/Precept/Language/TypeKind.cs`, `src/Precept/Language/ActionKind.cs`, `src/Precept/Language/Action.cs`

### TypeKind additions

```csharp
// After Stack=24, QueueBy=25 ... (fill in correct next value)
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
- Unit: confirm all enum values are contiguous with no gaps or duplicates (reflection-based assertion).

**Regression anchors:** Existing ActionKind.Set=1 through Clear=8 values unchanged.

**Depends on:** Slice 1 (for contextual clarity only; these enums are independent of TokenKind).

---

## Slice 3 — Tokens Catalog: GetMeta Entries for New Tokens

**Files:** `src/Precept/Language/Tokens.cs`

### Implementation

`Tokens.GetMeta(TokenKind kind)` is an exhaustive switch. Adding new `TokenKind` members without adding switch arms produces a compile error. Add one entry per new token kind from Slice 1.

**New entries (representative — add to exhaustive switch):**

```csharp
TokenKind.BagType     => new(kind, "bag",        Cat_Type,   "Bag (multiset) collection type",
    TextMateScope: "storage.type.precept", SemanticTokenType: "type"),
TokenKind.ListType    => new(kind, "list",        Cat_Type,   "List collection type",
    TextMateScope: "storage.type.precept", SemanticTokenType: "type"),
TokenKind.LogType     => new(kind, "log",         Cat_Type,   "Log (append-only ordered) collection type",
    TextMateScope: "storage.type.precept", SemanticTokenType: "type"),
TokenKind.LookupType  => new(kind, "lookup",      Cat_Type,   "Lookup (key-value map) collection type",
    TextMateScope: "storage.type.precept", SemanticTokenType: "type"),

TokenKind.By          => new(kind, "by",          Cat_Prep,   "Ordering-key connector",
    TextMateScope: "keyword.control.precept", SemanticTokenType: "keyword"),
TokenKind.At          => new(kind, "at",          Cat_Prep,   "Index connector for insert/remove at N",
    TextMateScope: "keyword.control.precept", SemanticTokenType: "keyword"),
TokenKind.Ascending   => new(kind, "ascending",   Cat_Decl,   "Ascending sort direction",
    TextMateScope: "keyword.declaration.precept", SemanticTokenType: "keyword"),
TokenKind.Descending  => new(kind, "descending",  Cat_Decl,   "Descending sort direction",
    TextMateScope: "keyword.declaration.precept", SemanticTokenType: "keyword"),

TokenKind.Append      => new(kind, "append",      Cat_Act,    "Log/list append action",
    TextMateScope: "keyword.other.action.precept", SemanticTokenType: "keyword", ValidAfter: VA_AfterArrow),
TokenKind.Insert      => new(kind, "insert",      Cat_Act,    "List insert-at action",
    TextMateScope: "keyword.other.action.precept", SemanticTokenType: "keyword", ValidAfter: VA_AfterArrow),
TokenKind.Put         => new(kind, "put",         Cat_Act,    "Lookup upsert action",
    TextMateScope: "keyword.other.action.precept", SemanticTokenType: "keyword", ValidAfter: VA_AfterArrow),

TokenKind.Each        => new(kind, "each",        Cat_Qnt,    "Bounded-iteration quantifier",
    TextMateScope: "keyword.other.quantifier.precept", SemanticTokenType: "keyword"),
TokenKind.For         => new(kind, "for",         Cat_Prep,   "Lookup key accessor (F for K)",
    TextMateScope: "keyword.control.precept", SemanticTokenType: "keyword"),

TokenKind.Countof     => new(kind, "countof",     Cat_Mem,    "Collection count-of accessor (.countof(E))",
    TextMateScope: "keyword.other.precept", SemanticTokenType: "keyword"),
TokenKind.Peekby      => new(kind, "peekby",      Cat_Mem,    "Queue-by peek-by-key accessor (.peekby)",
    TextMateScope: "keyword.other.precept", SemanticTokenType: "keyword"),
```

**KeywordsValidAsMemberName** — add `TokenKind.Countof` and `TokenKind.Peekby` to this set so the parser accepts `F.countof(E)` and `F.peekby` in member-access position.

**Tests:**
- Unit: `CatalogTests.cs` — `AllNewTokenKindsHaveMetaEntries` — reflection over all `TokenKind` values, assert `Tokens.GetMeta(k)` does not throw.
- Unit: `CatalogTests.cs` — `NewCollectionTypeTokensHaveCorrectCategory` — assert `BagType`, `ListType`, `LogType`, `LookupType` have `Cat_Type`.

**Regression anchors:** All pre-existing `GetMeta` entries unchanged. Existing catalog tests pass.

**Depends on:** Slice 1 (TokenKind values).

---

## Slice 4 — Types Catalog: TypeMeta Entries for New Collection TypeKinds

**Files:** `src/Precept/Language/Types.cs`

### Implementation

`Types.GetMeta(TypeKind kind)` is an exhaustive switch. Add entries for `Log`, `LogBy`, `Bag`, `List`, `QueueBy`, `Lookup`. Each entry requires: token, description, category, element type parameter, accessor list, proof requirements.

**New entry shapes:**

```
TypeKind.Bag  → CollectionTypeMeta(
    Token: Tokens.GetMeta(TokenKind.BagType),
    ElementType: any scalar type,
    Accessors: [
        .count                   → integer  (no proof requirement)
        .countof(E)              → integer  (no proof requirement)
        .first                   → element  (requires: notempty OR mincount≥1)
        .last                    → element  (requires: notempty OR mincount≥1)
        .contains(E)             → bool     (no proof requirement)
    ],
    SupportedActions: [Add, Remove, Clear],
    NotemptyApplicable: true)

TypeKind.List → CollectionTypeMeta(
    Token: Tokens.GetMeta(TokenKind.ListType),
    ElementType: any scalar type,
    Accessors: [
        .count                   → integer
        .first                   → element  (notempty/mincount≥1)
        .last                    → element  (notempty/mincount≥1)
        .at(N)                   → element  (index-bounds obligation)
        .contains(E)             → bool
    ],
    SupportedActions: [Append, Insert, RemoveAt, Clear],
    NotemptyApplicable: true)

TypeKind.Log  → CollectionTypeMeta(
    Token: Tokens.GetMeta(TokenKind.LogType),
    ElementType: any scalar type,
    Accessors: [
        .count                   → integer
        .first                   → element  (notempty/mincount≥1)
        .last                    → element  (notempty/mincount≥1)
        .contains(E)             → bool
    ],
    SupportedActions: [Append, Clear],
    NotemptyApplicable: true)

TypeKind.LogBy → TwoParamCollectionTypeMeta(
    Token: Tokens.GetMeta(TokenKind.LogType),
    OrderingKeyType: any comparable scalar,
    Accessors: [
        .count                   → integer
        .first                   → {value: T, by: P}  (notempty/mincount≥1)
        .last                    → {value: T, by: P}  (notempty/mincount≥1)
        .contains(P)             → bool  (NOTE: contains tests key P, not value T)
        .countof(P)              → integer  (count elements with this key; always 0 or 1)
    ],
    SupportedActions: [AppendBy, Clear],
    NotemptyApplicable: true,
    KeyUniquenessGuardRequired: true)

TypeKind.QueueBy → TwoParamCollectionTypeMeta(
    Token: Tokens.GetMeta(TokenKind.QueueType),
    OrderingKeyType: any comparable scalar,
    SortDirection: field-level Ascending (default) or Descending,
    Accessors: [
        .count                   → integer
        .first                   → {value: T, by: P}  (notempty/mincount≥1)
        .last                    → {value: T, by: P}  (notempty/mincount≥1)
        .peekby                  → P  (minimum-priority key; notempty)
        .contains(E)             → bool
    ],
    SupportedActions: [EnqueueBy, DequeueBy, Clear],
    NotemptyApplicable: true)

TypeKind.Lookup → TwoParamCollectionTypeMeta(
    Token: Tokens.GetMeta(TokenKind.LookupType),
    KeyType: any scalar (optional: ~string for CI keys),
    ValueType: any scalar,
    Accessors: [
        .count                   → integer
        .contains(K)             → bool
        F for K                  → V  (KeyPresenceSafety obligation: 'F contains K' guard required)
    ],
    SupportedActions: [Put, Remove, Clear],
    NotemptyApplicable: false)   ← lookup excluded from notempty
```

**Notes for two-parameter types:**
- `LogBy`, `QueueBy`, and `Lookup` have two type parameters. Current `CollectionTypeRefNode` carries one element type token. A `TwoParamCollectionTypeRefNode` with `ElementType`, `OrderingKeyType` (or `KeyType`/`ValueType`) fields is needed (added in Slice 7).
- The `.first`/`.last` accessors on `LogBy`/`QueueBy` project a *tuple-like* binding — in expression position this materializes as `.first.value` and `.first.by`. This is **not** an anonymous record type in the catalog; it is a named accessor pair. The TypeChecker (Slice 13) resolves member access chains.

**Tests:**
- Unit: `CatalogTests.cs` — `AllNewTypeKindsHaveMetaEntries` — reflection, assert no `GetMeta` throws.
- Unit: `CatalogTests.cs` — `LookupNotemetptyApplicable` — assert `Lookup` entry has `NotemptyApplicable: false`; all others (`Log`, `LogBy`, `Bag`, `List`, `QueueBy`) have `true`.

**Regression anchors:** All pre-existing `Types.GetMeta` entries unchanged.

**Depends on:** Slices 1–2.

---

## Slice 5 — Actions Catalog: ActionMeta Entries for New Action Kinds

**Files:** `src/Precept/Language/Actions.cs`, `src/Precept/Language/Action.cs`

### Implementation

`Actions.GetMeta(ActionKind kind)` is an exhaustive switch. Add entries for `Append`, `AppendBy`, `Insert`, `RemoveAt` (action), `Put`, `EnqueueBy`, `DequeueBy`.

```
ActionKind.Append   → ActionMeta(
    Token: Tokens.GetMeta(TokenKind.Append),
    ApplicableTo: [Log, List],
    SyntaxShape: CollectionValue,             // append F Expr
    ProofRequirements: none)

ActionKind.AppendBy → ActionMeta(
    Token: Tokens.GetMeta(TokenKind.Append),  // same keyword — "append"
    ApplicableTo: [LogBy],
    SyntaxShape: CollectionValueBy,           // append F Expr by P
    ProofRequirements: [KeyUniquenessGuard])  // 'when not (F contains P)' required

ActionKind.Insert   → ActionMeta(
    Token: Tokens.GetMeta(TokenKind.Insert),
    ApplicableTo: [List],
    SyntaxShape: InsertAt,                    // insert F Expr at N
    ProofRequirements: [IndexBoundsGuard])    // 'when N >= 0 and N <= F.count' required

ActionKind.RemoveAt → ActionMeta(
    Token: Tokens.GetMeta(TokenKind.Remove),  // reuses Remove token; 'at N' is trailing disambiguator
    ApplicableTo: [List],
    SyntaxShape: RemoveAtIndex,               // remove F at N
    ProofRequirements: [IndexBoundsGuard])    // 'when N >= 0 and N < F.count' required

ActionKind.Put      → ActionMeta(
    Token: Tokens.GetMeta(TokenKind.Put),
    ApplicableTo: [Lookup],
    SyntaxShape: PutKeyValue,                 // put F K = V
    ProofRequirements: none)                  // upsert — no guard required

ActionKind.EnqueueBy → ActionMeta(
    Token: Tokens.GetMeta(TokenKind.Enqueue), // same keyword — "enqueue"
    ApplicableTo: [QueueBy],
    SyntaxShape: CollectionValueBy,           // enqueue F Expr by P
    ProofRequirements: none)

ActionKind.DequeueBy → ActionMeta(
    Token: Tokens.GetMeta(TokenKind.Dequeue), // same keyword — "dequeue"
    ApplicableTo: [QueueBy],
    SyntaxShape: CollectionIntoBy,            // dequeue F [into G] [by H]
    IntoSupported: true,
    ProofRequirements: [NotemptyGuard])       // 'when F.count > 0' or notempty
```

**Note on shared tokens:** `Append/AppendBy` share the `Append` token; `Enqueue/EnqueueBy` share `Enqueue`; `Dequeue/DequeueBy` share `Dequeue`. The parser resolves which `ActionKind` applies based on the field's resolved type (TypeChecker) — in the parser phase, a single `AppendStatement` AST node is produced and resolved to `Append` vs. `AppendBy` by the TypeChecker. Document this in a comment on the `ActionMeta` entries.

**Also update:** `Remove` entry's `ApplicableTo` — currently `SetOnly`. Expand to `[Set, Bag, List, Lookup]` (design spec §2.3: `remove` applies to bag and list too; `remove K` on lookup removes the entry for key K).

**Tests:**
- Unit: `CatalogTests.cs` — `AllNewActionKindsHaveMetaEntries`.
- Unit: `CatalogTests.cs` — `RemoveApplicableToExpandedCorrectly` — assert `Remove` entry `ApplicableTo` contains `Bag`, `List`, `Lookup`.

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
```
95:  CaseInsensitiveRequiresEquality        — ~string compared with != must use ~!= or !=  (CI vs. CS)
96:  CaseSensitiveContainsCIValue           — CS collection.contains(~string expr) rejected
97:  CIFunctionOnNonString                  — ~startsWith / ~endsWith applied to non-~string
98:  CIResultInCSContext                    — CI operation result used where CS string expected
```

**New codes for collection safety:**
```
99:  KeyPresenceSafety                      — 'F for K' without preceding 'when F contains K' guard
100: IndexBoundsGuard                       — insert-at / remove-at-index without index-bounds guard
101: KeyUniquenessGuard                     — append-by / log-by without 'when not (F contains P)' guard
102: InvalidQuantifierTarget                — quantifier binding var resolves to non-collection field
103: BindingShadowsField                    — quantifier binding var name shadows a precept field (warning)
104: MissingOrderingKey                     — log-by/queue-by type ref missing 'by P' clause
105: CollectionInnerTypeError               — mismatched element type in collection operation
106: QuantifierPredicateNotBoolean          — quantifier predicate does not resolve to bool
```

### Diagnostics.cs changes

`Diagnostics.GetMeta(DiagnosticCode code)` is an exhaustive switch. Update code 66 entry text to reflect the new name and intended meaning. Add `DiagnosticMeta` entries for codes 95–106 with:
- `Code`
- `Message` (template)
- `Severity` (Error for 95–101, 104–106; Warning for 102–103)
- `HoverDescription`

**Tests:**
- Unit: `CatalogTests.cs` — `AllDiagnosticCodesHaveMetaEntries` — reflection, assert no `GetMeta` throws.
- Unit: `CatalogTests.cs` — `Code66RenamedCorrectly` — assert `DiagnosticCode` does not contain a member named `CaseInsensitiveStringOnNonCollection`.

**Regression anchors:** No existing code emits code 66 (confirmed). All other existing diagnostic code values and entries unchanged.

**Depends on:** Slices 1–5.

---

## Slice 7 — ExpressionFormKind: New Expression Forms

**Files:** `src/Precept/Language/ExpressionForms.cs`

### New ExpressionFormKind members

```csharp
Quantifier       = 12,   // each/any/no Binding in Field (Predicate) — null-denotation
CIFunctionCall   = 13,   // ~startsWith(E) / ~endsWith(E) — null-denotation, Tilde prefix
LookupAccess     = 14,   // F for K — left-denotation infix on identifier/member-access
```

### ExpressionForms.GetMeta entries

```csharp
ExpressionFormKind.Quantifier    => new(kind, ExpressionCategory.Composite, false,
    [TokenKind.Each, TokenKind.Any, TokenKind.No],
    "A bounded quantifier: each/any/no binding in collection (predicate)."),
ExpressionFormKind.CIFunctionCall => new(kind, ExpressionCategory.Invocation, false,
    [TokenKind.Tilde],
    "A case-insensitive function call: ~startsWith(expr) or ~endsWith(expr)."),
ExpressionFormKind.LookupAccess  => new(kind, ExpressionCategory.Composite, true,
    [],
    "A lookup key access: field for key."),
```

**Also add `[HandlesCatalogMember]` annotations** to `GraphAnalyzer.AnalyzeExpression` for the three new forms. This is required for `[Precept.HandlesCatalogExhaustively(typeof(ExpressionFormKind))]` to compile.

**Tests:**
- Unit: `CatalogTests.cs` — `AllExpressionFormKindsHaveMetaEntries`.
- Unit: `GraphAnalyzerTests.cs` — verifying the `[HandlesCatalogMember]` annotations exist (static analysis / reflection test).

**Depends on:** Slice 1.

---

## Slice 8 — AST Node Extensions & New Nodes

**Files:** `src/Precept/Pipeline/SyntaxNodes/TypeRefNode.cs`, `src/Precept/Pipeline/SyntaxNodes/ActionStatements.cs`, `src/Precept/Pipeline/SyntaxNodes/` (new files)

### TypeRefNode.cs changes

**1. Add `CaseInsensitive: bool` to `ScalarTypeRefNode`:**
```csharp
// Before:
public sealed record ScalarTypeRefNode(Span Span, Token TypeName, QualifierList Qualifiers) : TypeRefNode(Span);

// After:
public sealed record ScalarTypeRefNode(Span Span, Token TypeName, QualifierList Qualifiers, bool CaseInsensitive = false) : TypeRefNode(Span);
```

**2. Add `TwoParamCollectionTypeRefNode` (for log-by, queue-by, lookup):**
```csharp
/// <summary>
/// Type ref node for two-parameter collection types: log of T by P, queue of T by P, lookup of K to V.
/// </summary>
public sealed record TwoParamCollectionTypeRefNode(
    Span          Span,
    Token         CollectionKind,   // log / queue / lookup token
    Token         ElementType,      // T (log-by, queue-by) or K (lookup)
    Token         SecondType,       // P (log-by, queue-by) or V (lookup)
    bool          CaseInsensitive,  // true if ElementType or SecondType has ~ prefix
    SortDirection SortDirection,    // ascending/descending (queue-by only; default = Ascending)
    QualifierList Qualifiers
) : TypeRefNode(Span);

public enum SortDirection { Ascending = 1, Descending = 2 }
```

### ActionStatements.cs new nodes

Append the following records to `ActionStatements.cs`:

```csharp
// append F Expr  (log of T, list of T)
public sealed record AppendStatement(Span Span, Token Field, Expression Value) : ActionStatement(Span);

// append F Expr by P  (log of T by P)
public sealed record AppendByStatement(Span Span, Token Field, Expression Value, Expression Key) : ActionStatement(Span);

// insert F Expr at N  (list of T)
public sealed record InsertAtStatement(Span Span, Token Field, Expression Value, Expression Index) : ActionStatement(Span);

// remove F at N  (list of T)
public sealed record RemoveAtStatement(Span Span, Token Field, Expression Index) : ActionStatement(Span);

// put F K = V  (lookup of K to V)
public sealed record PutStatement(Span Span, Token Field, Expression Key, Expression Value) : ActionStatement(Span);

// enqueue F Expr by P  (queue of T by P)
public sealed record EnqueueByStatement(Span Span, Token Field, Expression Value, Expression Key) : ActionStatement(Span);

// dequeue F [into G] [by H]  (queue of T by P)
public sealed record DequeueByStatement(Span Span, Token Field, Token? Into, Token? ByBinding) : ActionStatement(Span);
```

### New expression AST nodes (new file: `src/Precept/Pipeline/SyntaxNodes/QuantifierExpression.cs`)

```csharp
/// <summary>
/// A quantifier expression: each/any/no Binding in CollectionField (Predicate).
/// </summary>
public sealed record QuantifierExpression(
    Span       Span,
    Token      Quantifier,     // each / any / no token
    Token      BindingVar,     // identifier declared as iteration variable
    Token      CollectionField,// the field being iterated
    Expression Predicate       // boolean expression; BindingVar in scope
) : Expression(Span);

/// <summary>
/// A case-insensitive function call: ~startsWith(Arg) or ~endsWith(Arg).
/// The Tilde prefix signals case-insensitive matching.
/// </summary>
public sealed record CIFunctionCallExpression(
    Span        Span,
    Token       FunctionName,  // "startsWith" or "endsWith" identifier
    Expression  Argument       // the string expression to match against
) : Expression(Span);
```

### ExpressionFormKind `[HandlesCatalogMember]` annotation update

In `GraphAnalyzer.cs`, add:
```csharp
[HandlesCatalogMember(ExpressionFormKind.Quantifier)]
[HandlesCatalogMember(ExpressionFormKind.CIFunctionCall)]
[HandlesCatalogMember(ExpressionFormKind.LookupAccess)]
```
to the existing `AnalyzeExpression` method stub.

**Tests:**
- Unit: `AstTests.cs` — `ScalarTypeRefNodeCaseInsensitiveDefaultsFalse` — construct `ScalarTypeRefNode` with no explicit CI flag, verify `CaseInsensitive == false`.
- Unit: `AstTests.cs` — `TwoParamCollectionTypeRefNodeRoundTrip` — construct and assert property equality.
- Unit: `AstTests.cs` — `AppendByStatementFields` — construct and assert Key/Value/Field accessible.

**Regression anchors:** All existing `ScalarTypeRefNode` construction sites now pass `CaseInsensitive: false` implicitly (default). If any site passes positional args, verify arg count doesn't break. Search `new ScalarTypeRefNode(` in the parser and update any positional constructors.

**Depends on:** Slices 1–7.

---

## Slice 9 — Lexer: Keyword Recognition & Compound Token

**Files:** `src/Precept/Language/Lexer.cs`

### Implementation

**9a. New keyword recognition** — in the keyword-lookup table (the `FrozenDictionary<string, TokenKind>` or equivalent), add entries for all 16 new tokens from Slice 1:

```
"bag"        → TokenKind.BagType
"list"       → TokenKind.ListType
"log"        → TokenKind.LogType
"lookup"     → TokenKind.LookupType
"by"         → TokenKind.By
"at"         → TokenKind.At
"ascending"  → TokenKind.Ascending
"descending" → TokenKind.Descending
"append"     → TokenKind.Append
"insert"     → TokenKind.Insert
"put"        → TokenKind.Put
"each"       → TokenKind.Each
"for"        → TokenKind.For
"countof"    → TokenKind.Countof
"peekby"     → TokenKind.Peekby
```

**9b. No compound token needed** — `remove F at N` uses the existing `Remove` keyword followed by `At`. No special lexer lookahead is required. The parser dispatches on `TokenKind.Remove` and peeks at the token after the field; if it is `At`, it produces `RemoveAtStatement`, otherwise `RemoveStatement`. This is entirely a parser-level disambiguation — the lexer needs no change.

**9c. `Peekby` lexing note** — `"peekby"` contains a hyphen in the design doc notation (`peek-by`) but the token text registered in `Tokens.GetMeta` is `"peekby"` (no hyphen). Verify the design doc uses `peekby` (one word) as the accessor name. If the spec uses `peek-by`, a compound-token pattern would be needed. *(Check `docs/language/collection-types.md` §queue-by accessors before finalizing.)*

**Tests:**
- `LexerTests.cs`: `RemoveWithoutAtIsSimple` — `"remove F E"` → token sequence `[Remove, Identifier, Identifier]`.
- `LexerTests.cs`: `EachTokenized` — `"each"` → `TokenKind.Each`.
- `LexerTests.cs`: `ForTokenized` — `"for"` → `TokenKind.For`.

**Regression anchors:** All existing lexer tests pass. `"remove"` without `-at` must not produce `RemoveAt`. All pre-existing keyword tokens must produce same kinds as before.

**Depends on:** Slice 1 (token kind values), Slice 3 (Tokens.GetMeta has entries for new kinds).

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

```csharp
// Scalar ~string: field F as ~string
if (Current.Kind == TokenKind.Tilde)
{
    var tildeSpan = Current.Span;
    Advance(); // consume ~
    if (Current.Kind != TokenKind.StringType)
    {
        // ~<non-string> is invalid: emit CaseInsensitiveFieldRequiresTildeEquals (code 66)
        // then recover by treating Current as the type token
        Emit(DiagnosticCode.CaseInsensitiveFieldRequiresTildeEquals, tildeSpan);
    }
    var typeTok = Consume(); // consume 'string' (or recovered token)
    var qualifiers = ParseQualifierList();
    return new ScalarTypeRefNode(tildeSpan.To(qualifiers.Span), typeTok, qualifiers, CaseInsensitive: true);
}
```

**Invariant:** `CaseInsensitive: true` on `ScalarTypeRefNode` means the field uses case-insensitive equality/membership. The stored value is case-preserving; comparison behavior is altered.

**Tests:**
- `ParserTests.cs`: `ScalarTildeString_Parses` — `"field Email as ~string"` → `ScalarTypeRefNode` with `TypeName.Kind == StringType` and `CaseInsensitive == true`.
- `ParserTests.cs`: `ScalarTildeNonString_EmitsDiagnostic` — `"field X as ~integer"` → diagnostic code 66.
- `ParserTests.cs`: `ScalarNoTilde_CaseInsensitiveFalse` — `"field Email as string"` → `ScalarTypeRefNode` with `CaseInsensitive == false`.

**Regression anchors:** All GAP-3 tests at lines 1677–1719 (set of ~string collection) must still pass. The Tilde branch in `ParseTypeRef` must only fire for Tilde in *scalar* position — the existing collection branch (`Set | QueueType | StackType` with inner-type Tilde) must not be affected.

**Depends on:** Slices 1–8.

---

## Slice 11 — Parser: New Collection Type Refs in ParseTypeRef

**Files:** `src/Precept/Pipeline/Parser.Declarations.cs`

### Implementation

Extend `ParseTypeRef()` to handle `BagType`, `ListType`, `LogType`, and `LookupType`, and extend the existing `QueueType` branch to handle the `by P` clause.

**Current `Set|QueueType|StackType` branch — extend to include `BagType`, `ListType`, `LogType`:**

```
ParseCollectionTypeRef(collectionToken):
  Expect TokenKind.Of
  if Current == Tilde: consume it, setCI = true
  elementTypeTok = Consume(TypeKeywords)  // bag/list/log inner type
  qualifiers = ParseQualifierList()
  // For LogType: check for optional 'by P'
  if collectionToken.Kind == LogType && Current.Kind == TokenKind.By:
      Consume(By)
      keyTypeTok = Consume(TypeKeywords)   // log-by ordering key type
      return new TwoParamCollectionTypeRefNode(..., SortDirection.Ascending, ...)
  return new CollectionTypeRefNode(..., CaseInsensitive: setCI, ...)
```

**New `QueueType + by` branch** (within or after the existing QueueType arm):
```
if collectionToken.Kind == QueueType && Current.Kind == TokenKind.By:
    Consume(By)
    keyTypeTok = Consume(TypeKeywords)   // queue-by ordering key type
    // Optional sort direction
    var dir = SortDirection.Ascending;
    if (Current.Kind == TokenKind.Ascending) { Advance(); dir = SortDirection.Ascending; }
    else if (Current.Kind == TokenKind.Descending) { Advance(); dir = SortDirection.Descending; }
    return new TwoParamCollectionTypeRefNode(..., dir, ...)
```

**New `LookupType` branch:**
```
if current == LookupType:
    Consume(LookupType)
    Expect(Of)           // "lookup of K to V"
    keyTypeTok = Consume(TypeKeywords)   // K (may be preceded by ~ for CI keys)
    Expect(To)           // "to"
    if Current == Tilde: consume it, setCI = true  // ~string value type
    valueTypeTok = Consume(TypeKeywords) // V
    return new TwoParamCollectionTypeRefNode(..., CI on key or value)
```

**Missing `by` clause on log-by — emit `MissingOrderingKey` (code 104):** If `LogType` type ref appears in a field context but has no `by` clause, the field gets `TypeKind.Log` (plain log). If `QueueType` has `by` clause → `QueueBy`. If `QueueType` without `by` → `Queue` (pre-existing behavior). `MissingOrderingKey` is only emitted by the TypeChecker, not the parser — the parser produces a `CollectionTypeRefNode` with kind `Log` and the TypeChecker checks whether the field's use is consistent.

**Tests:**
- `ParserTests.cs`: `BagOf_Parses` — `"field T as bag of string"` → `CollectionTypeRefNode` with `CollectionKind.Kind == BagType`.
- `ParserTests.cs`: `ListOf_Parses` — similar for list.
- `ParserTests.cs`: `LogOf_Parses` — `"field L as log of string"` → `CollectionTypeRefNode(LogType, StringType)`.
- `ParserTests.cs`: `LogOfBy_Parses` — `"field L as log of string by integer"` → `TwoParamCollectionTypeRefNode(LogType, StringType, IntegerType, ...)`.
- `ParserTests.cs`: `QueueByAscending_Parses` — `"field Q as queue of string by integer ascending"` → `TwoParamCollectionTypeRefNode(QueueType, SortDirection.Ascending)`.
- `ParserTests.cs`: `QueueByDescending_Parses` — same with descending.
- `ParserTests.cs`: `LookupOf_Parses` — `"field M as lookup of string to integer"` → `TwoParamCollectionTypeRefNode(LookupType, StringType, IntegerType)`.
- `ParserTests.cs`: `LookupOfCIKey_Parses` — `"field M as lookup of ~string to integer"` → `TwoParamCollectionTypeRefNode` with `CaseInsensitive: true`.
- `ParserTests.cs`: `BagOfTildeString_Parses` — `"field T as bag of ~string"` → `CollectionTypeRefNode` with `CaseInsensitive: true`.

**Regression anchors:** Existing `set of ~string` GAP-3 tests unchanged. Existing queue/stack type ref tests unchanged.

**Depends on:** Slices 1–9 (particularly Slice 8 for new AST nodes).

---

## Slice 12 — Parser: New Action Statements

**Files:** `src/Precept/Pipeline/Parser.Declarations.cs`

### Implementation

`ParseActionStatement()` is a dispatch on the current token kind. The set of valid action-leading tokens is derived from `Actions.All` (the `ActionKeywords` FrozenSet). New action tokens (`Append`, `Insert`, `RemoveAt`, `Put`) are **automatically included** in `ActionKeywords` once their `ActionMeta` entries are added to `Actions.GetMeta()` (Slice 5) because the parser derives the set from the catalog.

The parser needs **parsing logic** for each new shape:

**`CollectionValue` (Append — log/list):** Identical to `Add` shape.
```csharp
case TokenKind.Append:
    verbTok = Consume();
    fieldTok = Consume(Identifier);
    value = ParseExpression();
    // Check for optional 'by P' — if present, produce AppendByStatement; else AppendStatement
    if (Current.Kind == TokenKind.By)
    {
        Advance();
        key = ParseExpression();
        return new AppendByStatement(verbTok.Span.To(key.Span), fieldTok, value, key);
    }
    return new AppendStatement(verbTok.Span.To(value.Span), fieldTok, value);
```

**`InsertAt` (Insert):**
```csharp
case TokenKind.Insert:
    verbTok = Consume();
    fieldTok = Consume(Identifier);
    value = ParseExpression();
    Expect(TokenKind.At);
    index = ParseExpression();
    return new InsertAtStatement(verbTok.Span.To(index.Span), fieldTok, value, index);
```

**`RemoveAtIndex` (Remove — trailing `at` disambiguator):**
```csharp
case TokenKind.Remove:
    verbTok = Consume();
    fieldTok = Consume(Identifier);
    if (Current.Kind == TokenKind.At)
    {
        Advance();
        index = ParseExpression();
        return new RemoveAtStatement(verbTok.Span.To(index.Span), fieldTok, index);
    }
    value = ParseExpression();
    return new RemoveStatement(verbTok.Span.To(value.Span), fieldTok, value);  // existing
```

**`PutKeyValue` (Put):**
```csharp
case TokenKind.Put:
    verbTok = Consume();
    fieldTok = Consume(Identifier);
    key = ParseExpression();
    Expect(TokenKind.Equals);
    value = ParseExpression();
    return new PutStatement(verbTok.Span.To(value.Span), fieldTok, key, value);
```

**Extend `Enqueue` case to detect `by` and produce `EnqueueByStatement`:**
```csharp
case TokenKind.Enqueue:
    verbTok = Consume();
    fieldTok = Consume(Identifier);
    value = ParseExpression();
    if (Current.Kind == TokenKind.By)
    {
        Advance();
        key = ParseExpression();
        return new EnqueueByStatement(verbTok.Span.To(key.Span), fieldTok, value, key);
    }
    return new EnqueueStatement(verbTok.Span.To(value.Span), fieldTok, value);  // existing
```

**Extend `Dequeue` case to detect `by` and produce `DequeueByStatement`:**
```csharp
case TokenKind.Dequeue:
    verbTok = Consume();
    fieldTok = Consume(Identifier);
    Token? into = null;
    if (Current.Kind == TokenKind.Into) { Advance(); into = Consume(Identifier); }
    if (Current.Kind == TokenKind.By)
    {
        Advance();
        byBinding = Consume(Identifier);
        return new DequeueByStatement(verbTok.Span.To(byBinding.Span), fieldTok, into, byBinding);
    }
    if (into != null) return new DequeueStatement(verbTok.Span.To(into.Span), fieldTok, into); // existing
    return new DequeueStatement(verbTok.Span.To(fieldTok.Span), fieldTok, null);               // existing
```

**Tests:**
- `ParserTests.cs`: `AppendStatement_Parses` — `"→ append Items \"x\""` → `AppendStatement`.
- `ParserTests.cs`: `AppendByStatement_Parses` — `"→ append Events e by e.ts"` → `AppendByStatement`.
- `ParserTests.cs`: `InsertAtStatement_Parses` — `"→ insert Items \"x\" at 0"` → `InsertAtStatement` with `Index` of `0`.
- `ParserTests.cs`: `RemoveAtStatement_Parses` — `"→ remove Items at 2"` → `RemoveAtStatement`.
- `ParserTests.cs`: `PutStatement_Parses` — `"→ put Cache \"key\" = 42"` → `PutStatement`.
- `ParserTests.cs`: `EnqueueByStatement_Parses` — `"→ enqueue Q task by task.priority"` → `EnqueueByStatement`.
- `ParserTests.cs`: `DequeueByStatement_With_Into_And_By_Parses` — test optional routing.
- `ParserTests.cs`: `DequeueStatement_WithoutBy_StillProducesDequeueStatement` — regression: plain dequeue unchanged.
- `ParserTests.cs`: `EnqueueStatement_WithoutBy_StillProducesEnqueueStatement` — regression.

**Regression anchors:** All existing `Enqueue`, `Dequeue`, `Add`, `Remove`, `Push`, `Pop`, `Clear` action statement tests must pass unchanged (lines 1590–1675).

**Depends on:** Slices 1–10.

---

## Slice 13 — Parser: `~startsWith`/`~endsWith` in Expression Position

**Files:** `src/Precept/Pipeline/Parser.Expressions.cs`

### Implementation

In `ParseAtom()` (the Pratt parser's null-denotation dispatch), add a case for `TokenKind.Tilde`:

```csharp
case TokenKind.Tilde:
{
    var tildeTok = Consume();
    // Must be followed by 'startsWith' or 'endsWith' identifier
    if (Current.Kind != TokenKind.Identifier ||
        (Current.Text != "startsWith" && Current.Text != "endsWith"))
    {
        Emit(DiagnosticCode.CIFunctionOnNonString, tildeTok.Span);
        return ErrorExpression(tildeTok.Span);
    }
    var funcName = Consume();  // "startsWith" or "endsWith"
    Expect(TokenKind.LeftParen);
    var arg = ParseExpression();
    Expect(TokenKind.RightParen);
    return new CIFunctionCallExpression(tildeTok.Span.To(Current.Span), funcName, arg);
}
```

**Disambiguation note:** `Tilde` currently also appears in *type-ref position* (inside `ParseTypeRef()`). In `ParseAtom()`, `Tilde` is only reachable in expression context (after a guard `when`, inside an `if`/`then`/`else`, or inside a quantifier predicate). There is no ambiguity because `ParseAtom` is never called from `ParseTypeRef`.

**Tests:**
- `ParserTests.cs`: `TildeStartsWith_InWhenGuard` — `"when Email ~startsWith(\"@\")"` → expression containing `CIFunctionCallExpression(funcName: "startsWith")`.
- `ParserTests.cs`: `TildeEndsWith_InExpression` — similar for `~endsWith`.
- `ParserTests.cs`: `TildeOnNonFunction_EmitsDiagnostic` — `"when ~42"` → diagnostic emitted.

**Regression anchors:** The Tilde in collection type-ref position (`set of ~string`) must not be affected. Verify GAP-3 tests at lines 1677–1719 still pass.

**Depends on:** Slices 1–8.

---

## Slice 14 — Parser: Quantifier Expressions

**Files:** `src/Precept/Pipeline/Parser.Expressions.cs`

### Implementation

In `ParseAtom()`, add cases for `TokenKind.Each`, `TokenKind.Any` (dual-role), and `TokenKind.No` (dual-role):

**Design:** `any` and `no` serve two roles:
- As state wildcards / `no transition` outcome: these appear in *declaration position* (parsed by `Parser.Declarations.cs`), never in expression position.
- As quantifiers: these appear only in *expression position* (after `when`, inside `if cond`, inside predicates).

Therefore the Pratt parser's `ParseAtom()` safely handles `any` and `no` as quantifiers without ambiguity. Declarations never reach `ParseAtom()`.

```csharp
case TokenKind.Each:
case TokenKind.Any when IsInExpressionContext():   // always true in ParseAtom
case TokenKind.No when IsInExpressionContext():
{
    var quantTok = Consume();
    var bindingVar = Consume(TokenKind.Identifier);
    Expect(TokenKind.In);
    var collectionField = Consume(TokenKind.Identifier);  // field name
    Expect(TokenKind.LeftParen);
    var predicate = ParseExpression();  // binding var is in scope
    Expect(TokenKind.RightParen);
    return new QuantifierExpression(
        quantTok.Span.To(Current.Span),
        quantTok, bindingVar, collectionField, predicate);
}
```

**`For` infix in the Pratt loop (left-denotation):**
In the Pratt loop's `ParseInfix()` / led dispatch, add `TokenKind.For` as a binary infix at a specific binding power (higher than `and/or`, lower than member access). When the current left-hand expression is an identifier and the next token is `For`:

```csharp
case TokenKind.For:
{
    var forTok = Consume();
    var key = ParseExpression(bindingPower: BP_Comparison);
    return new BinaryOperationExpression(left.Span.To(key.Span), forTok, left, key);
    // TypeChecker later validates that left resolves to a Lookup field.
}
```

`F for K` is parsed as `BinaryOperation(left=F, op=For, right=K)`. The TypeChecker resolves it to lookup key access.

**Tests:**
- `ParserTests.cs`: `EachQuantifier_Parses` — `"each item in Items (item > 0)"` → `QuantifierExpression(Each, ...)`.
- `ParserTests.cs`: `AnyQuantifier_InExpression` — `"any item in Tags (item ~startsWith \"@\")"` → `QuantifierExpression(Any, ...)` with `CIFunctionCallExpression` predicate.
- `ParserTests.cs`: `NoQuantifier_InExpression` — `"no x in Errors (x is not set)"`.
- `ParserTests.cs`: `ForInfix_Parses` — `"Cache for \"key\""` → `BinaryOperationExpression(op=For)`.
- `ParserTests.cs`: `AnyAsStateWildcard_NotAffected` — regression: existing `any` in state declaration position still parses correctly.
- `ParserTests.cs`: `NoTransitionOutcome_NotAffected` — regression: `"→ no transition"` still parses correctly.

**Regression anchors:** All existing expression tests (lines 1100–1350). `no transition` outcome, `any` state wildcard — these are in declaration context; confirm they are unaffected.

**Depends on:** Slices 1–13.

---

## Slice 15 — TypeChecker Phase 3: Core Infrastructure

**Files:** `src/Precept/Pipeline/TypeChecker.cs`

### Context

The TypeChecker is currently a complete stub:
```csharp
public static SemanticIndex Check(ParseTree parseTree) => throw new NotImplementedException();
```

Phase 3 implements it from scratch. The catalog-driven implementation guide in `TypeChecker.cs` applies: every operator, function, accessor, and action has proof obligations in catalog metadata. The TypeChecker reads them; it does not hardcode per-member logic.

### Design

**Input:** `ParseTree` (produced by the parser)  
**Output:** `SemanticIndex` — resolved types, scope bindings, proof obligation ledger

**`SemanticIndex` structure (define in `src/Precept/Pipeline/SemanticIndex.cs`):**
```csharp
public sealed record SemanticIndex(
    IReadOnlyDictionary<FieldDeclarationNode, ResolvedFieldType>  FieldTypes,
    IReadOnlyDictionary<Expression, ResolvedType>                  ExpressionTypes,
    IReadOnlyDictionary<QuantifierExpression, QuantifierBinding>   QuantifierBindings,
    ImmutableArray<ProofObligation>                                ProofObligations,
    ImmutableArray<Diagnostic>                                     Diagnostics
);
```

**Pass structure:**

```
Pass 1: Field declaration resolution
  For each FieldDeclarationNode in the parse tree:
    - Resolve TypeRefNode → ResolvedFieldType
    - Record CaseInsensitive flag (from ScalarTypeRefNode or TwoParamCollectionTypeRefNode)
    - Validate modifier applicability: FieldModifierMeta.ApplicableTo must include the resolved type category
    - For Notempty on collections: verify TypeKind is not Lookup (Slice 16 extends this)

Pass 2: Expression type inference
  For each expression in guard/rule/ensure/action value positions:
    - Walk the expression tree bottom-up
    - Assign a ResolvedType to each Expression node (stored in SemanticIndex.ExpressionTypes)
    - For BinaryOperation: look up OperationMeta → validate types, record result type
    - For MemberAccess: look up TypeAccessor on the left-hand type → record return type
    - For FunctionCall: look up FunctionMeta → validate argument types, record result type
    - For Conditional (if/then/else): unify then-type and else-type; apply CI unification rules
    - For Identifier: resolve against field declarations or quantifier bindings in scope

Pass 3: Action statement type checking
  For each ActionStatement:
    - Resolve the target field's type
    - Look up ActionMeta.ApplicableTo → validate field type is in the applicable set
    - For CollectionValue/By shapes: validate value expression type matches ElementType
    - For InsertAt/RemoveAtIndex: validate index type is integer; add IndexBoundsGuard obligation
    - For PutKeyValue: validate key/value types match Lookup K and V
    - For AppendBy/EnqueueBy: validate ordering key type matches P; add KeyUniquenessGuard obligation if log-by

Pass 4: Rule/ensure checking
  For each rule/ensure expression: verify it resolves to bool

Pass 5: Transition-row checking
  For each transition row: verify source/target states exist
```

**`CheckTypeRef(TypeRefNode node) → ResolvedFieldType`:**
```csharp
private static ResolvedFieldType CheckTypeRef(TypeRefNode node) => node switch
{
    ScalarTypeRefNode scalar     => ResolvedScalarType(Types.GetMeta(scalar.TypeName.Kind), scalar.CaseInsensitive),
    CollectionTypeRefNode coll   => ResolvedCollectionType(Types.GetMeta(ToTypeKind(coll.CollectionKind.Kind)), ...),
    TwoParamCollectionTypeRefNode tp => ResolvedTwoParamType(Types.GetMeta(ToTypeKind(tp.CollectionKind.Kind)), ...),
    ChoiceTypeRefNode choice      => ResolvedChoiceType(...),
    _                             => throw new UnreachableException(),
};
```

**Tests (TypeCheckerTests.cs — all new):**
- `Check_FieldDeclarations_ResolveTypes` — parse `"field Email as ~string"`, run Check, assert `SemanticIndex.FieldTypes[emailField].CaseInsensitive == true`.
- `Check_StringField_TypeResolvesToString` — `"field Name as string"` → `CaseInsensitive == false`.
- `Check_BagField_TypeResolves` — `"field Tags as bag of string"` → `ResolvedCollectionType(Bag, ...)`.
- `Check_EmptyPrecept_ProducesEmptySemanticIndex` — baseline: no diagnostics.
- `Check_UnknownFieldRef_EmitsDiagnostic` — expression references a field name that does not exist.

**Regression anchors:** `TypeChecker.Check()` currently throws; this is the first implementation. All parser tests that test only the parse tree (not semantic analysis) are unaffected.

**Depends on:** Slices 1–14.

---

## Slice 16 — TypeChecker: `~string` Enforcement Rules

**Files:** `src/Precept/Pipeline/TypeChecker.cs`

### Implementation

Three enforcement rules for `~string`, applied during Pass 2 (expression type inference):

**Rule 1: CI equality — `~string` equality requires `~=` or `=` (bidirectional assignment)**

From `docs/language/primitive-types.md`: assignment is bidirectional — `string` values are assignable to `~string` fields and vice versa. The type checker records `CaseInsensitive` on the expression's `ResolvedType`. It does NOT reject assignment of `string` to `~string`.

For **comparison** (`==`): if either operand is `~string`, the comparison is CI. This is correct and requires no diagnostic. The comparison result is `bool`.

**Rule 2: `~startsWith` / `~endsWith` require `~string` subject**

When checking `CIFunctionCallExpression`:
- The expression is synthesized (it appears in member context: `Email ~startsWith "@"`)
- The *implicit receiver* (the field being tested) must be `~string`. If the field is plain `string`, emit `CIFunctionOnNonString` (code 97).
- The argument type must be `string` or `~string`.
- Return type: `bool`.

**Rule 3: CS collection.contains(~string expr) rejected**

When checking `BinaryOperation(Contains)`:
- If the collection field's ElementType is NOT `~string` (i.e., it is `string`),
  and the argument expression has `CaseInsensitive == true`,
  emit `CaseSensitiveContainsCIValue` (code 96).

**CI type unification rules:**

During conditional expression `if C then A else B`:
- If both A and B are `~string` → result is `~string`.
- If one is `~string` and one is `string` → result is `~string` (CI is preserved; the string is *upcast* to CI context).
- If both are `string` → result is `string`.

During concatenation (`string + string`):
- If either operand is `~string` → result is `string` (CI qualifier is destroyed; per spec §string-concat).
- This means `CI + CS = string` (not `~string`). The author must rephrase to preserve CI.

**Tests (TypeCheckerTests.cs):**
- `Check_CIFieldEquals_NoDiagnostic` — `"when Email = \"user@example.com\""` on `~string Email` → no CI diagnostic.
- `Check_TildeStartsWith_OnCIField_NoDiagnostic`.
- `Check_TildeStartsWith_OnCSField_EmitsDiagnostic` — `~startsWith` on plain `string` → code 97.
- `Check_CSContains_CIValue_EmitsDiagnostic` — `set of string` `.contains(~string expr)` → code 96.
- `Check_CIConcat_ResultIsCS` — `field1 + field2` where field1 is `~string` → result type is `string` (not `~string`).
- `Check_Conditional_BothCI_ResultIsCI` — `if C then ciField else ciField` → result is `~string`.

**Depends on:** Slice 15.

---

## Slice 17 — TypeChecker: New Collection Operations & Accessors

**Files:** `src/Precept/Pipeline/TypeChecker.cs`

### Implementation

Pass 3 extensions for new action kinds (AppendBy, Insert, RemoveAt, Put, EnqueueBy, DequeueBy) and Pass 2 extensions for new accessor forms.

**New action type checks (Pass 3):**

```
AppendStatement on Log field:
  - field.ElementType must match value.ExpressionType
  - No proof obligation (log is always-append-able)

AppendByStatement on LogBy field:
  - field.ElementType must match value.ExpressionType
  - field.OrderingKeyType must match key.ExpressionType
  - Add ProofObligation(KeyUniquenessGuard, field, key)

InsertAtStatement on List field:
  - field.ElementType must match value.ExpressionType
  - index.ExpressionType must be integer
  - Add ProofObligation(IndexBoundsGuard, field, index, InsertKind)

RemoveAtStatement on List field:
  - index.ExpressionType must be integer
  - Add ProofObligation(IndexBoundsGuard, field, index, RemoveKind)

PutStatement on Lookup field:
  - key.ExpressionType must match field.KeyType (with CI rules if key is ~string)
  - value.ExpressionType must match field.ValueType
  - No proof obligation (put is always safe — upsert semantics)

EnqueueByStatement on QueueBy field:
  - field.ElementType must match value.ExpressionType
  - field.OrderingKeyType must match key.ExpressionType

DequeueByStatement on QueueBy field:
  - If Into is not null: verify Into field is compatible with field.ElementType
  - If ByBinding is not null: record binding in scope (ByBinding is a local identifier)
  - Add ProofObligation(NotemptyOrMincount, field) if not statically discharged
```

**New accessor type checks (Pass 2):**

```
.first / .last on Log, LogBy, Bag, List, QueueBy:
  - Return type: field.ElementType (for Log/Bag/List)
  - For LogBy/QueueBy: return type is a projection: { value: T, by: P }
    - Subsequent .value → T, .by → P (handled as chained MemberAccess)
  - Proof obligation: NotemptyOrMincount (unless statically discharged)

.at(N) on List:
  - N must be integer
  - Proof obligation: IndexBoundsGuard

.contains(E) on all collections:
  - For Lookup: E must match KeyType
  - For others: E must match ElementType
  - CI rule (Slice 16, Rule 3) applies here

.countof(E) on all collections:
  - E must match ElementType (or KeyType for Lookup)
  - Return type: integer

.peekby on QueueBy:
  - Return type: field.OrderingKeyType (the minimum-priority key)
  - Proof obligation: NotemptyOrMincount

F for K (LookupAccess):
  - Left-hand F must resolve to Lookup field
  - K.ExpressionType must match field.KeyType (with CI rules)
  - Return type: field.ValueType
  - Proof obligation: KeyPresenceSafety (code 99) — 'F contains K' guard required
```

**Tests:**
- `Check_AppendOnLogField_NoDiagnostic`.
- `Check_AppendOnNonLog_EmitsDiagnostic` — append on `set` field → `CollectionInnerTypeError` (code 105).
- `Check_InsertAtRequiresIntIndex` — `insert F at "x" E` → type error.
- `Check_PutStatement_KeyValueTypesChecked`.
- `Check_FirstAccessor_OnLogWithNotempty_NoDiagnostic`.
- `Check_FirstAccessor_OnLogWithoutNotempty_EmitsObligation`.
- `Check_ForAccessor_OnLookup_AddsSafetyObligation`.
- `Check_PeekbyOnNonQueueBy_EmitsDiagnostic`.

**Depends on:** Slices 15–16.

---

## Slice 18 — TypeChecker: Quantifier Type Checking

**Files:** `src/Precept/Pipeline/TypeChecker.cs`

### Implementation

**Quantifier expression type checking (Pass 2 extension):**

When checking `QuantifierExpression(quantifier, bindingVar, collectionField, predicate)`:

1. **Resolve `collectionField`** — must resolve to a field with a collection type. If not, emit `InvalidQuantifierTarget` (code 102).

2. **Determine binding variable type:**
   - For single-type collections (Bag, List, Log, Set, Queue, Stack): binding var type = `ElementType`.
   - For two-type collections (LogBy, QueueBy): binding var type = projection `{ value: T, by: P }`. Accessed as `bindingVar.value` and `bindingVar.by` inside the predicate.
   - For Lookup: binding var type = `{ key: K, value: V }`. Accessed as `bindingVar.key` and `bindingVar.value`.

3. **Push binding scope** — add `bindingVar → ResolvedType` to the local scope for predicate checking. Emit `BindingShadowsField` (code 103, Warning) if `bindingVar.Text` matches an existing field name.

4. **Check predicate** with binding var in scope — `predicate.ResolvedType` must be `bool`. If not, emit `QuantifierPredicateNotBoolean` (code 106).

5. **Pop binding scope**.

6. **Result type of `QuantifierExpression`:** `bool` for all three quantifiers (`each`/`any`/`no`).

**CI propagation inside predicates:**
- If `collectionField.ElementType` is `~string`, then inside the predicate, `bindingVar` has type `~string`. CI rules (Slice 16) apply normally. This is automatic because the binding var's type carries the CI flag.

**Tests:**
- `Check_EachQuantifier_PredicateChecked` — verify predicate scope has binding var.
- `Check_AnyQuantifier_OnCICollection_BindingIsCIString` — binding var on `bag of ~string` → binding type is `~string`.
- `Check_Quantifier_OnNonCollection_EmitsDiagnostic` — `each x in ScalarField (...)` → code 102.
- `Check_Quantifier_PredicateNotBool_EmitsDiagnostic` — predicate returns string → code 106.
- `Check_Quantifier_BindingVarShadowsField_EmitsWarning` — binding var name equals an existing field name → code 103 (warning, not error).
- `Check_Quantifier_OnLookup_BindingHasKeyValue` — `each entry in Cache (entry.key ~startsWith "@")` → binding var has `.key` and `.value` accessors.

**Depends on:** Slices 15–17.

---

## Slice 19 — ProofEngine Phase 3: Core + Notempty/Collection Obligations

**Files:** `src/Precept/Pipeline/ProofEngine.cs`, `src/Precept/Pipeline/ProofLedger.cs` (new)

### Context

`ProofEngine.Prove()` is a complete stub. Phase 3 implements it from scratch. The catalog-driven implementation guide in `ProofEngine.cs` applies: proof obligations come from catalog metadata (`ActionMeta.ProofRequirements`, `TypeAccessor.ProofRequirements`, etc.). The engine reads them; it does not hardcode per-action logic.

### Design

**Input:** `SemanticIndex` (from TypeChecker), `StateGraph` (from GraphAnalyzer — initially pass a stub)  
**Output:** `ProofLedger` — obligation discharge results

**`ProofLedger` structure:**
```csharp
public sealed record ProofLedger(
    ImmutableArray<ProofObligation>          Outstanding,   // unresolved
    ImmutableArray<DischargedObligation>     Discharged,    // proved safe
    ImmutableArray<Diagnostic>               Violations     // could not prove safe
);
```

**Proof obligation kinds (from catalog metadata):**

```
NotemptyOrMincount:
  Subject: a collection field accessor (.first/.last/.peekby)
  Discharge condition: (a) field has 'notempty' modifier, OR (b) field has 'mincount >= 1' modifier,
                       OR (c) the accessor is inside a guard that has 'when F.count > 0' or equivalent
  If not dischargeable: emit UnguardedCollectionAccess diagnostic (use existing code or add new)

KeyPresenceSafety (code 99):
  Subject: 'F for K' expression
  Discharge condition: surrounding guard includes 'when F contains K' or 'when F.count > 0'
                       where K matches the access key
  If not dischargeable: emit KeyPresenceSafety diagnostic

IndexBoundsGuard (code 100):
  Subject: insert-at / remove at N / .at(N) accessor
  Discharge condition:
    - For insert: guard includes '0 <= N and N <= F.count' (or equivalent)
    - For remove at N / .at: guard includes '0 <= N and N < F.count'
  If not dischargeable: emit IndexBoundsGuard diagnostic

KeyUniquenessGuard (code 101):
  Subject: append-by action on log-by
  Discharge condition: guard includes 'when not (F contains P)' where P is the ordering key
  If not dischargeable: emit KeyUniquenessGuard diagnostic
```

**`notempty` on all non-lookup collections (Slice 4 change to Modifiers catalog)**

In `Modifiers.cs`, update `ModifierKind.Notempty`'s `ApplicableTo` from `StringOnly` to the union of `StringOnly` union `{Set, Queue, Stack, Bag, List, Log, LogBy, QueueBy}`. **Exclude Lookup** (as specified in the design doc and Slice 4).

```csharp
// Modifiers.cs — before:
ModifierKind.Notempty => new FieldModifierMeta(
    kind, Tokens.GetMeta(TokenKind.Notempty),
    "String is non-empty",
    ModifierCategory.Structural, StringOnly, ...)

// After:
ModifierKind.Notempty => new FieldModifierMeta(
    kind, Tokens.GetMeta(TokenKind.Notempty),
    "Field is non-empty: string has content, collection has at least one element",
    ModifierCategory.Structural, StringAndNonLookupCollections, ...)
```

where `StringAndNonLookupCollections` is a new `TypeTarget[]` constant that includes `string`, `~string`, `Set`, `Queue`, `Stack`, `Bag`, `List`, `Log`, `LogBy`, `QueueBy`.

**Tests (ProofEngineTests.cs — all new):**
- `Prove_NotemptyDischarges_FirstAccess` — field has `notempty`, `.first` accessor has no outstanding obligation.
- `Prove_NoNotempty_FirstAccess_EmitsViolation` — field has no `notempty`, `.first` → `ProofLedger.Violations` has one entry.
- `Prove_MincountDischarges_FirstAccess` — field has `mincount 1`, `.first` → discharged.
- `Prove_KeyPresenceSafety_WithGuard_Discharged` — `when F contains K` guard followed by `F for K` → discharged.
- `Prove_KeyPresenceSafety_WithoutGuard_Violation` — `F for K` without guard → violation code 99.
- `Prove_IndexBoundsGuard_WithGuard_Discharged`.
- `Prove_IndexBoundsGuard_WithoutGuard_Violation` — code 100.
- `Prove_KeyUniquenessGuard_LogBy_WithGuard_Discharged`.
- `Prove_Notempty_OnLookup_EmitsModifierError` — regression: `notempty` on `lookup of K to V` → invalid modifier diagnostic.

**Regression anchors:** Existing string `notempty` behavior unchanged. The TypeChecker modifier applicability check (Pass 1) verifies `notempty` on Lookup → diagnostic (this already fires in Pass 1 from the ApplicableTo metadata; ProofEngine doesn't need to re-emit it).

**Depends on:** Slices 15–18.

---

## Slice 20 — ProofEngine: Quantifier Termination Proof

**Files:** `src/Precept/Pipeline/ProofEngine.cs`

### Implementation

The proof engine must verify that every quantifier expression (`each`/`any`/`no`) terminates:

**Quantifier termination proof strategy:**

A quantifier `each x in F (predicate)` is provably terminating if and only if `F` is a **finite, statically-bounded collection field** declared in the precept. Because all Precept collection fields are immutable values (structurally pure — the evaluator never mutates them in place), and all collections are finite at any given point-in-time, ALL quantifiers over precept collection fields are provably terminating.

The proof obligation reduces to:
1. `F` resolves to a field with a collection type (checked in TypeChecker Slice 18 — emits `InvalidQuantifierTarget` if not).
2. The collection field is not infinite (all Precept collections are finite by construction — no stream or lazy-list types exist).

Therefore the ProofEngine's quantifier obligation is trivially discharged for any valid quantifier. The ProofEngine records `DischargedObligation(QuantifierTermination, field)` for all valid quantifiers.

**Predicate safety (nested mutations):**
- Predicates in quantifiers are **read-only** — they are expressions, not action bodies. No mutations can appear inside a predicate. This is enforced by the TypeChecker (expressions cannot contain action statements). No additional proof work needed.

**Tests:**
- `Prove_QuantifierTermination_AlwaysDischarged` — any valid quantifier expression → no outstanding obligations for termination.
- `Prove_QuantifierOnCollection_Trivially_Safe` — `each x in Bag (x > 0)` → `Discharged` contains `QuantifierTermination`.

**Depends on:** Slice 19.

---

## Slice 21 — GraphAnalyzer Phase 3

**Files:** `src/Precept/Pipeline/GraphAnalyzer.cs`

### Context

`GraphAnalyzer.Analyze()` is a complete stub. Phase 3 implements it. The `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]` attribute means the method must handle all 14 `ExpressionFormKind` values (including the 3 added in Slice 7).

### Design

The `GraphAnalyzer` is responsible for:
1. Building the **state graph** (states + transitions as directed edges).
2. Verifying **structural modifiers**: `terminal` (no outgoing edges), `required` (dominator check), `irreversible` (no back-edge).
3. **Reachability** of all declared states from the `initial` state.
4. **Expression analysis for proof relevance** — traversing expressions to find guard patterns that discharge proof obligations (consumed by `ProofEngine`).

**`StateGraph` structure:**
```csharp
public sealed record StateGraph(
    IReadOnlyList<StateNode>           States,
    IReadOnlyList<TransitionEdge>      Transitions,
    IReadOnlyDictionary<StateNode, bool> Reachable,
    IReadOnlyList<GuardPattern>         GuardPatterns   // for ProofEngine consumption
);
```

**New expression form handling:**

```
ExpressionFormKind.Quantifier:
  - Walk the collection field reference — record as a field-access in the guard context.
  - Walk the predicate — extract any guard patterns (e.g., 'x > 0' type patterns).
  - Quantifiers do NOT affect state reachability.

ExpressionFormKind.CIFunctionCall:
  - Walk the argument expression.
  - Does not affect state reachability.

ExpressionFormKind.LookupAccess:
  - The 'F for K' expression resolves to a value — not a guard.
  - Walk both operands.
  - Does not affect state reachability.
```

**Guard pattern extraction (for ProofEngine `GuardPattern` set):**

The graph analyzer walks guard expressions and extracts:
- `F contains K` → `GuardPattern(ContainsGuard, field=F, subject=K)`
- `F.count > 0` / `not F.count == 0` → `GuardPattern(NonemptyGuard, field=F)`
- `0 <= N and N < F.count` → `GuardPattern(IndexBoundsGuard, field=F, index=N)`
- `not (F contains P)` → `GuardPattern(KeyAbsenceGuard, field=F, subject=P)`

These patterns are passed into `ProofEngine.Prove()` as pre-extracted data for obligation discharge.

**Tests (GraphAnalyzerTests.cs — all new):**
- `Analyze_MinimalPrecept_ReturnsStateGraph` — one state → graph has one state, no transitions.
- `Analyze_TwoStates_TransitionEdge_Added`.
- `Analyze_TerminalState_HasNoOutgoing`.
- `Analyze_RequiredState_DominatorCheck`.
- `Analyze_UnreachableState_MarkedUnreachable`.
- `Analyze_GuardExtraction_ContainsPattern` — `when F contains K` → `GuardPattern(ContainsGuard, ...)` in `StateGraph.GuardPatterns`.
- `Analyze_QuantifierExpression_WalksCollection`.

**Regression anchors:** GraphAnalyzer is a first-time implementation; no existing behavior to regress.

**Depends on:** Slices 7, 15–20.

---

## Slice 22 — Evaluator: Core + `ImmutableLog<T>` Backing Type

**Files:** `src/Precept/Runtime/Evaluator.cs`, `src/Precept/Runtime/ImmutableLog.cs` (new)

### Context

The Evaluator is a complete stub. Phase 3 implements `Fire`, `Update`, `InspectFire`, `InspectUpdate`. The executable model design (D8/R4) governs the API surface — when D8/R4 ships, the string-keyed dictionaries become descriptor-keyed. This slice implements the evaluator against the current string-keyed API and leaves D8/R4 migration to a later ticket.

### `ImmutableLog<T>` implementation

The design doc specifies `log of T` uses an Okasaki functional queue (pair-of-stacks) for O(1) append, O(1) last, amortized O(1) first:

```csharp
/// <summary>
/// Immutable append-only log. Okasaki functional queue: two ImmutableStack{T} halves.
/// append: O(1). count: O(1). first: amortized O(1) (rebalance on demand). last: O(1).
/// </summary>
public sealed class ImmutableLog<T>
{
    public static readonly ImmutableLog<T> Empty = new(ImmutableStack<T>.Empty, ImmutableStack<T>.Empty, 0);

    private readonly ImmutableStack<T> _front;  // oldest elements (reversed during rebalance)
    private readonly ImmutableStack<T> _back;   // newest elements (prepend-order)
    private readonly int _count;

    private ImmutableLog(ImmutableStack<T> front, ImmutableStack<T> back, int count)
    {
        _front = front; _back = back; _count = count;
    }

    public int Count => _count;

    public ImmutableLog<T> Append(T element) =>
        new(_front, _back.Push(element), _count + 1);

    public T First() {
        if (_count == 0) throw new InvalidOperationException("Log is empty");
        if (!_front.IsEmpty) return _front.Peek();
        return Rebalanced().First();
    }

    public T Last() {
        if (_count == 0) throw new InvalidOperationException("Log is empty");
        return _back.IsEmpty ? _front.Peek() : _back.Peek();  // back has newest
    }

    private ImmutableLog<T> Rebalanced() {
        var newFront = _back.Reverse();  // ImmutableStack extension: O(n)
        return new(newFront, ImmutableStack<T>.Empty, _count);
    }

    public bool Contains(T element) => _front.Contains(element) || _back.Contains(element); // O(n)
}
```

**Note:** `_back.Peek()` is the *most recently appended* element (last), which is correct because `Append` pushes to `_back`. `_front` holds oldest elements after rebalancing. `Last()` returns `_back.Peek()` when back is non-empty, else scans `_front`.

### Backing types summary for all new collection kinds

| TypeKind | Backing type | Notes |
|---|---|---|
| `Bag` | `ImmutableDictionary<T, int>` | element → count |
| `List` | `ImmutableList<T>` | .NET BCL |
| `Log` | `ImmutableLog<T>` | custom (this slice) |
| `LogBy` | `ImmutableSortedDictionary<P, T>` | key → value; uniqueness enforced by guard |
| `QueueBy` | Custom sorted queue | `SortedDictionary<P, Queue<T>>` + element counter; `IEnumerator`-based min-key dequeue |
| `Lookup` | `ImmutableDictionary<K, V>` (or `OrdinalIgnoreCase` variant if K is `~string`) | CI key semantics |

**Tests (EvaluatorTests.cs — all new):**
- `ImmutableLog_Append_CountIncrements`.
- `ImmutableLog_First_ReturnsOldest` — append "a", "b", "c" → `First() == "a"`.
- `ImmutableLog_Last_ReturnsNewest` — append "a", "b", "c" → `Last() == "c"`.
- `ImmutableLog_Empty_FirstThrows`.
- `ImmutableLog_Contains_ExistingElement_True`.
- `ImmutableLog_Append_IsNonDestructive` — original log unchanged after append.

**Depends on:** Slices 15–21.

---

## Slice 23 — Evaluator: New Collections + CI + Quantifiers

**Files:** `src/Precept/Runtime/Evaluator.cs`

### Implementation

Extends the Evaluator core (Slice 22) with evaluation logic for all new collections, CI semantics, and quantifier expressions.

**New collection action evaluation:**

```
AppendStatement on Log field:
  var newLog = (ImmutableLog<T>)state[field];
  newLog = newLog.Append(EvalExpr(value));
  return state.With(field, newLog);

AppendByStatement on LogBy field:
  var key = EvalExpr(keyExpr);
  var val = EvalExpr(valueExpr);
  var newLog = ((ImmutableSortedDictionary<P,T>)state[field]).Add(key, val);
  return state.With(field, newLog);

InsertAtStatement on List field:
  var idx = (int)EvalExpr(index);
  var val = EvalExpr(value);
  var newList = ((ImmutableList<T>)state[field]).Insert(idx, val);
  return state.With(field, newList);

RemoveAtStatement on List field:
  var idx = (int)EvalExpr(index);
  var newList = ((ImmutableList<T>)state[field]).RemoveAt(idx);
  return state.With(field, newList);

PutStatement on Lookup field:
  var key = EvalExpr(keyExpr);   // if key is ~string: stored case-preserved
  var val = EvalExpr(valueExpr);
  var newMap = ((ImmutableDictionary<K,V>)state[field]).SetItem(key, val);
  return state.With(field, newMap);

EnqueueByStatement on QueueBy field:
  var key = EvalExpr(keyExpr);
  var val = EvalExpr(valueExpr);
  // Insert into SortedDictionary bucket keyed by priority
  ...

DequeueByStatement on QueueBy field:
  // Dequeue from minimum-priority bucket (ascending) or maximum-priority bucket (descending)
  ...
```

**Accessor evaluation:**

```
.first on Log:    ((ImmutableLog<T>)val).First()
.last  on Log:    ((ImmutableLog<T>)val).Last()
.first on LogBy:  ((ImmutableSortedDictionary<P,T>)val).First()  → { value, by }
.peekby on QueueBy: minimum-priority key from sorted queue
F for K on Lookup: ((ImmutableDictionary<K,V>)val)[key]  (key: case-preserved string, OrdinalIgnoreCase dict)
.at(N) on List:   ((ImmutableList<T>)val)[N]
.countof(E) on Bag: ((ImmutableDictionary<T,int>)val).GetValueOrDefault(E, 0)
```

**`~string` CI evaluation:**

```
Field storage: all strings stored case-preserved.
Equality comparison (== and !=): if either operand type is ~string, use OrdinalIgnoreCase.
~startsWith(arg): StringComparer.OrdinalIgnoreCase.Equals(subject[..arg.Length], arg)
  — or: subject.StartsWith(arg, StringComparison.OrdinalIgnoreCase)
~endsWith(arg): subject.EndsWith(arg, StringComparison.OrdinalIgnoreCase)
Lookup with ~string key: dict constructed with StringComparer.OrdinalIgnoreCase
```

**Quantifier evaluation:**

```
QuantifierExpression(Each, bindingVar, collectionField, predicate):
  var collection = (IEnumerable<T>)state[collectionField];
  foreach (var element in collection):
    var localScope = scope.With(bindingVar.Text, element);
    if (!EvalBool(predicate, localScope)) return false;
  return true;

QuantifierExpression(Any, ...):
  foreach (var element in collection):
    var localScope = scope.With(bindingVar.Text, element);
    if (EvalBool(predicate, localScope)) return true;
  return false;

QuantifierExpression(No, ...):
  foreach (var element in collection):
    var localScope = scope.With(bindingVar.Text, element);
    if (EvalBool(predicate, localScope)) return false;
  return true;
```

For `LogBy`/`QueueBy` element projections (binding var has `.value` and `.by`): wrap element as `TwoProjection { Value = v, By = p }` and resolve member access.

**Tests (EvaluatorTests.cs):**
- `Eval_AppendOnLog_ElementAppended`.
- `Eval_FirstOnLog_AfterTwoAppends_ReturnsFirst`.
- `Eval_InsertAtList_ElementInserted`.
- `Eval_RemoveAtList_ElementRemoved`.
- `Eval_PutLookup_KeyAdded`.
- `Eval_ForAccessor_ReturnsValue`.
- `Eval_CILookup_TildeStringKey_CaseInsensitive` — `put L "KEY" = 1`, then `L for "key"` → 1.
- `Eval_CIField_Equality_CaseInsensitive` — field `Email` is `~string`, `Email = "USER@EXAMPLE.COM"` when stored `"user@example.com"` → true.
- `Eval_TildeStartsWith_MatchesCaseInsensitive`.
- `Eval_EachQuantifier_AllMatch_True`.
- `Eval_EachQuantifier_OneMismatch_False`.
- `Eval_AnyQuantifier_NoneMatch_False`.
- `Eval_NoQuantifier_AllMatch_False`.
- `Eval_AnyQuantifier_OnCICollection_UsesCI`.

**Regression anchors:** All existing `set`/`queue`/`stack` evaluator behaviors unchanged. CI behavior on existing `set of ~string` collection — verify `add` / `remove` / `contains` still use OrdinalIgnoreCase.

**Depends on:** Slices 15–22.

---

## File Inventory

| File | Status | Slices |
|---|---|---|
| `src/Precept/Language/TokenKind.cs` | Modify | 1 |
| `src/Precept/Language/TypeKind.cs` | Modify | 2 |
| `src/Precept/Language/ActionKind.cs` | Modify | 2 |
| `src/Precept/Language/Action.cs` | Modify | 2 |
| `src/Precept/Language/Tokens.cs` | Modify | 3 |
| `src/Precept/Language/Types.cs` | Modify | 4 |
| `src/Precept/Language/Actions.cs` | Modify | 5 |
| `src/Precept/Language/Modifiers.cs` | Modify | 19 |
| `src/Precept/Language/DiagnosticCode.cs` | Modify | 6 |
| `src/Precept/Language/Diagnostics.cs` | Modify | 6 |
| `src/Precept/Language/ExpressionForms.cs` | Modify | 7 |
| `src/Precept/Pipeline/SyntaxNodes/TypeRefNode.cs` | Modify | 8 |
| `src/Precept/Pipeline/SyntaxNodes/ActionStatements.cs` | Modify | 8 |
| `src/Precept/Pipeline/SyntaxNodes/QuantifierExpression.cs` | **Create** | 8 |
| `src/Precept/Pipeline/GraphAnalyzer.cs` | Modify (annotations) | 7, 21 |
| `src/Precept/Language/Lexer.cs` | Modify | 9 |
| `src/Precept/Pipeline/Parser.Declarations.cs` | Modify | 10, 11, 12 |
| `src/Precept/Pipeline/Parser.Expressions.cs` | Modify | 13, 14 |
| `src/Precept/Pipeline/TypeChecker.cs` | Implement from stub | 15–18 |
| `src/Precept/Pipeline/SemanticIndex.cs` | **Create** | 15 |
| `src/Precept/Pipeline/ProofEngine.cs` | Implement from stub | 19, 20 |
| `src/Precept/Pipeline/ProofLedger.cs` | **Create** | 19 |
| `src/Precept/Pipeline/GraphAnalyzer.cs` | Implement from stub | 21 |
| `src/Precept/Runtime/Evaluator.cs` | Implement from stub | 22, 23 |
| `src/Precept/Runtime/ImmutableLog.cs` | **Create** | 22 |
| `test/Precept.Tests/LexerTests.cs` | Modify/create | 1, 9 |
| `test/Precept.Tests/CatalogTests.cs` | Modify/create | 3–7 |
| `test/Precept.Tests/AstTests.cs` | Modify/create | 8 |
| `test/Precept.Tests/ParserTests.cs` | Modify | 10–14 |
| `test/Precept.Tests/TypeCheckerTests.cs` | **Create** | 15–18 |
| `test/Precept.Tests/ProofEngineTests.cs` | **Create** | 19–20 |
| `test/Precept.Tests/GraphAnalyzerTests.cs` | **Create** | 21 |
| `test/Precept.Tests/EvaluatorTests.cs` | **Create** | 22–23 |

---

## Implementation Checklist

### Phase I: Catalog Infrastructure

- [ ] Slice 1 — TokenKind additions (16 new tokens)
- [ ] Slice 2 — TypeKind (6) + ActionKind (7) + ActionSyntaxShape (5) additions
- [ ] Slice 3 — Tokens.GetMeta entries for all new TokenKinds
- [ ] Slice 4 — Types.GetMeta entries for Log, LogBy, Bag, List, QueueBy, Lookup
- [ ] Slice 5 — Actions.GetMeta entries + Remove ApplicableTo expansion
- [ ] Slice 6 — DiagnosticCode rename + new codes 95–106 + Diagnostics.GetMeta entries
- [ ] Slice 7 — ExpressionFormKind additions (Quantifier, CIFunctionCall, LookupAccess)

### Phase II: AST + Parser

- [ ] Slice 8 — AST: ScalarTypeRefNode.CaseInsensitive, TwoParamCollectionTypeRefNode, new action nodes, QuantifierExpression, CIFunctionCallExpression
- [ ] Slice 9 — Lexer: keyword table entries (`at` as index connector for both insert and remove)
- [ ] Slice 10 — Parser: scalar `~string` in ParseTypeRef
- [ ] Slice 11 — Parser: new collection type refs (bag, list, log, log-by, queue-by, lookup)
- [ ] Slice 12 — Parser: new action statements (append, append-by, insert-at, remove-at-index, put, enqueue-by, dequeue-by)
- [ ] Slice 13 — Parser: `~startsWith`/`~endsWith` in expression position
- [ ] Slice 14 — Parser: quantifier expressions + `for` infix

### Phase III: TypeChecker

- [ ] Slice 15 — TypeChecker Phase 3 core (5-pass framework, field resolution, expression type inference, action checks, rule/ensure/transition checks)
- [ ] Slice 16 — TypeChecker `~string` enforcement (CI equality, ~startsWith/~endsWith, CI-in-CS-contains, concatenation destroys CI, conditional CI unification)
- [ ] Slice 17 — TypeChecker new collection operations + accessor type checking
- [ ] Slice 18 — TypeChecker quantifier type checking (binding scope, CI propagation)

### Phase IV: Proof Engine + Graph Analyzer

- [ ] Slice 19 — ProofEngine Phase 3 core + NotemptyOrMincount + KeyPresenceSafety + IndexBoundsGuard + KeyUniquenessGuard + Modifiers.Notempty applicability lift
- [ ] Slice 20 — ProofEngine quantifier termination (trivially discharged for all valid quantifiers)
- [ ] Slice 21 — GraphAnalyzer Phase 3 (state graph, structural modifier checks, reachability, guard pattern extraction for all 14 ExpressionFormKinds)

### Phase V: Evaluator

- [ ] Slice 22 — Evaluator core + `ImmutableLog<T>` backing type + all new collection backing types
- [ ] Slice 23 — Evaluator new collection actions, accessors, `~string` CI semantics, quantifier evaluation

---

## Critical Risk Factors

1. **TypeChecker/ProofEngine/GraphAnalyzer/Evaluator are all stubs** — Slices 15–23 are *first-time implementations*, not extensions. Each requires careful design of the internal data structures before implementation begins. Budget significantly more time than the parser slices.

2. **Exhaustive switch breaks** — every addition to `TokenKind`, `TypeKind`, `ActionKind`, `DiagnosticCode`, or `ExpressionFormKind` will break compilation until the corresponding `GetMeta` switch arm is added. Slices 1–7 must be landed together (or Slices 1, then 3; 2, then 4,5; etc.) to keep the project compilable.

3. **`ScalarTypeRefNode` positional constructor** — adding `CaseInsensitive` as a trailing optional parameter is backward-compatible only if all call sites use named or positional args that don't break. Search all `new ScalarTypeRefNode(` usages before landing Slice 8.

4. **`remove F at N` parser disambiguation** — the `Remove` case in the action parser must peek at the token after the field: if `At`, produce `RemoveAtStatement`; otherwise fall through to `RemoveStatement`. The lookahead is one token and unambiguous, but the existing `Remove` switch arm must be extended without breaking `remove F Expr`. Regression tests for plain `remove` are critical.

5. **`any` and `no` dual roles** — parser disambiguation by context. If any existing test feeds `any` or `no` in expression position through the parser, those tests need to verify behavior hasn't changed after Slice 14 adds quantifier handling.

6. **`SemanticIndex` and `ProofLedger` types don't exist yet** — the TypeChecker and ProofEngine stubs reference them but they're not defined. Slice 15 must create `SemanticIndex.cs` and Slice 19 must create `ProofLedger.cs` before the respective implementations land.

7. **MCP tools** — `precept_compile`, `precept_inspect`, `precept_fire`, and `precept_update` all route through the TypeChecker/ProofEngine/Evaluator stubs. Once those are implemented, the MCP tool DTOs in `tools/Precept.Mcp/Tools/` may need updates if result types change shape. Assess during Slice 15.
