# Type Checker

## 1. Status

| Property | Value |
|---|---|
| Doc maturity | Full |
| Implementation state | Stub — not yet implemented |
| Source | `src/Precept/Pipeline/TypeChecker.cs`, `src/Precept/Pipeline/SemanticIndex.cs` |
| Upstream | `ConstructManifest` (from Parser) + `SymbolTable` (from NameBinder) |
| Downstream | GraphAnalyzer, ProofEngine, PreceptBuilder, LS semantic features |

---

## 2. Overview

The type checker is a **metadata resolution engine with structural scaffolding** — not a structural validator with metadata lookups. Traditional type checkers "know" the type system and implement it; Precept's catalogs know the type system and the checker merely applies it. ~70–75% of the checker's work is asking catalogs questions and recording answers. The remaining ~25–30% is structural: symbol tables, scope management, cycle detection, choice-set logic.

This reframing has a profound implication: **new language features (operations, functions, types, modifiers, actions) require zero type-checker code changes.** Add the catalog entry → the generic resolution engine handles it automatically. Only genuinely new structural patterns (a new scope rule, a new validation shape) require checker changes.

The type checker transforms `ConstructManifest` into `SemanticIndex` — a flat semantic inventory of resolved symbols, typed expressions, normalized declarations, and dependency facts. The inventory is organized by semantic role, not by source position, because downstream consumers need declarations indexed by role, not by parser nesting.

### Input Shape

The parser produces:

```csharp
public sealed record ConstructManifest(
    ImmutableArray<ParsedConstruct> Constructs,
    ImmutableArray<Diagnostic> Diagnostics);
```

Each construct is a generic `ParsedConstruct`:

```csharp
public sealed record ParsedConstruct(
    ConstructMeta Meta,
    ImmutableArray<SlotValue> Slots,
    SourceSpan Span);
```

There are no per-construct AST node types. The type checker dispatches on `ConstructKind` (via `Meta.Kind`), not on C# type. Slot values are read via typed pattern matching on the 17-subtype `SlotValue` discriminated union.

### SlotValue Subtypes

The parser produces these slot value types:

| SlotValue Subtype | Contents | Expression Status |
|---|---|---|
| `IdentifierListSlot` | `ImmutableArray<string> Names` | N/A — resolved names |
| `TypeExpressionSlot` | `TypeMeta Type` | N/A — resolved catalog type |
| `ModifierListSlot` | `ImmutableArray<ModifierKind> Modifiers` | N/A — resolved modifiers |
| `StateEntryListSlot` | `ImmutableArray<(string Name, ImmutableArray<ModifierKind> Modifiers)> Entries` | N/A — resolved names + modifiers |
| `ArgumentListSlot` | `ImmutableArray<(string Name, TypeMeta Type)> Args` | N/A — resolved names + types |
| `ComputeExpressionSlot` | `ParsedExpression Expression` | Parser-owned expression DU |
| `GuardClauseSlot` | `ParsedExpression Expression` | Parser-owned expression DU |
| `ActionChainSlot` | `ImmutableArray<ActionKind> Actions` | N/A — resolved actions |
| `OutcomeSlot` | `ParsedOutcome Outcome` | Parser-owned outcome DU |
| `StateTargetSlot` | `string? StateName` | N/A — resolved name |
| `EventTargetSlot` | `string? EventName` | N/A — resolved name |
| `EnsureClauseSlot` | `ParsedExpression Expression` | Parser-owned expression DU |
| `BecauseClauseSlot` | `string Message` | N/A — extracted literal text |
| `AccessModeSlot` | `TokenKind AccessMode` | N/A — resolved token |
| `FieldTargetSlot` | `string? FieldName` | N/A — resolved name |
| `RuleExpressionSlot` | `ParsedExpression Expression` | Parser-owned expression DU |
| `InitialMarkerSlot` | `bool IsPresent` | N/A — keyword presence |

### Blocking Dependency: Expression Trees (RESOLVED)

Expression-carrying slots (`ComputeExpressionSlot`, `GuardClauseSlot`, `EnsureClauseSlot`, `RuleExpressionSlot`) now carry `ParsedExpression` — a sealed abstract record DU with 13 per-form sealed subtypes, one for each `ExpressionFormKind` member. The parser produces these; the type checker's expression resolution sub-engine consumes them and produces `TypedExpression`. Note: `OutcomeSlot` carries `ParsedOutcome`, not `ParsedExpression` — outcomes are a separate 4-member DU (TransitionOutcome, NoTransitionOutcome, RejectOutcome, MalformedOutcome).

The expression tree is a closed, strongly-typed DU. `ParsedExpression` is the parser-side counterpart to `TypedExpression`. The set is closed by design — new expression form requires C# code change. Exhaustiveness is enforced via: (1) sealed class hierarchy (CS8509/CS8524 on switch expressions); (2) `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]` + PRECEPT0019 for multi-method consumers.

---

## 3. Responsibilities and Boundaries

**OWNS:** name resolution, type resolution, expression typing, overload selection, modifier combination legality, action shape classification, semantic identity stamping, qualifier disambiguation, error-type propagation, SemanticIndex production.

**Does NOT OWN:** source structure (Parser), graph topology (GraphAnalyzer), proof obligation discharge (ProofEngine), execution planning (Precept Builder), qualifier runtime identity (Evaluator).

---

## 4. Right-Sizing

The type checker has **medium complexity** — more involved than the lexer or tokenizer, but less structural than the graph analyzer:

| Metric | Value | Rationale |
|---|---|---|
| Estimated LOC | 800–1200 | ~350 expression resolution + ~300 declaration normalization + ~200 structural validation |
| Catalog dependency | ~70% | Most logic is "look up catalog, record result" |
| Structural logic | ~30% | Symbol tables, scope management, cycles, choice sets |
| Expression forms | 13 | One arm per `ExpressionFormKind` + error stub |
| Construct kinds | ~20 | One dispatch arm per `ConstructKind` that matters to the checker |

The checker is NOT a general-purpose compiler component — it's purpose-built for Precept's closed type system and finite construct vocabulary.

---

## 5. Inputs and Outputs

### Input

`ConstructManifest` containing `ImmutableArray<ParsedConstruct>` from the parser, plus `SymbolTable` from the NameBinder containing pre-resolved declarations and references. The type checker dispatches on `ConstructKind` for semantic validation and expression resolution. It does not perform name lookup — all names are pre-resolved in the `SymbolTable`.

### Output

`SemanticIndex` — a flat semantic inventory with:

- **Symbol tables:** `TypedField`, `TypedState`, `TypedEvent` arrays with derived name lookups
- **Typed declarations:** `TypedTransitionRow`, `TypedRule`, `TypedEnsure`, etc.
- **Dependency facts:** Computed field dependencies, constraint field references
- **Accumulated diagnostics**

---

## 6. Architecture: Catalog-Driven 2-Pass Design

### The Dispatch Model

The type checker dispatches on `ConstructKind` (an enum), NOT on C# type. There are no per-construct AST node classes — only generic `ParsedConstruct`:

```csharp
foreach (var construct in manifest.Constructs)
{
    switch (construct.Meta.Kind)
    {
        case ConstructKind.FieldDeclaration:
            RegisterField(construct);
            break;
        case ConstructKind.StateDeclaration:
            RegisterState(construct);
            break;
        case ConstructKind.EventDeclaration:
            RegisterEvent(construct);
            break;
        // ... remaining construct kinds
    }
}
```

Slot values are accessed by index and pattern-matched:

```csharp
void RegisterField(ParsedConstruct construct)
{
    // Field declaration slots: [0]=Identifiers, [1]=Type, [2]=Modifiers, [3]=Default?, [4]=Compute?
    var names = construct.Slots[0] as IdentifierListSlot 
        ?? throw new InvalidOperationException("Expected identifier list in slot 0");
    var typeSlot = construct.Slots[1] as TypeExpressionSlot
        ?? throw new InvalidOperationException("Expected type expression in slot 1");
    var modifiers = construct.Slots[2] as ModifierListSlot;
    
    // Type resolution via catalog lookup
    var typeKind = ResolveTypeSpan(typeSlot.Span);
    
    foreach (var name in names.Identifiers)
    {
        RegisterFieldSymbol(name, typeKind, modifiers?.Modifiers ?? []);
    }
}
```

The slot array layout for each `ConstructKind` is defined by the `ConstructMeta.Slots` property in the Constructs catalog.

### Symbol Table (from NameBinder)

> **Note:** Symbol table construction — collecting field/state/event/arg declarations, building name lookup dictionaries, detecting duplicate names, and resolving identifier references — is performed by the **NameBinder** stage, which runs before the TypeChecker. See [name-binder.md](./name-binder.md) for the full design.

The TypeChecker receives a pre-resolved `SymbolTable` via its `Check(ConstructManifest, SymbolTable)` signature. It trusts that all names are collected, all references are resolved (or marked `UnresolvedTarget`), and all naming diagnostics are already emitted. The TypeChecker never performs name lookup or duplicate detection.

**TypeRef resolution:** Query `Types.ByTokenKind` for the keyword token → get `TypeMeta` → stamp `TypeKind`. For collections, extract element type. For choice, extract the choice definition. Pure catalog lookup — no expression resolution needed.

**Initial state / terminal / required validation** fires here (counting state modifiers from the `SymbolTable`).

### Checking (Expression Resolution + Normalization + Structural Validation)

**Input:** `SymbolTable` (from NameBinder) + `ConstructManifest.Constructs`
**Output:** `SemanticIndex`

Pass 2 has three generic sub-passes.

#### Sub-pass 2a: Expression Resolution Engine

**Unblocked.** Expression-carrying slots now carry `ParsedExpression` — the expression resolution engine can proceed.

The core of the checker will be a single recursive function (~250–350 lines) that resolves any `ParsedExpression` node to a `TypedExpression`. The function dispatches on expression form and delegates to catalog lookups for operator semantics, function signatures, and type accessors.

This function has no per-type-kind branching for operators or functions. It doesn't know what `+` means for money vs integers — it asks the Operations catalog. It doesn't know what `min` accepts — it asks the Functions catalog. It doesn't know what `.count` returns — it asks the Types catalog.

#### Sub-pass 2b: Declaration Normalization

Walks each construct kind and resolves contained expressions via 2a, producing typed inventory entries. Dispatches on `ConstructKind`:

| ConstructKind | Resolution | Output |
|---|---|---|
| `TransitionRow` | Resolve guard + actions + outcome | `TypedTransitionRow` |
| `RuleDeclaration` | Resolve condition + guard + message | `TypedRule` |
| `StateEnsure` | Resolve condition + guard + message | `TypedEnsure` |
| `EventEnsure` | Resolve condition + guard + message | `TypedEnsure` |
| `AccessMode` | Validate field/state names, resolve guard | `TypedAccessMode` |
| `StateAction` | Resolve guard + actions | `TypedStateHook` |
| `EventHandler` | Resolve actions | `TypedEventHandler` |
| `FieldDeclaration` (computed) | Resolve computed expression | Populate `TypedField.ComputedExpression` |

Each case is 5–10 lines: resolve the expressions this construct contains, validate structural constraints (guards must be boolean, messages must be string), produce a typed entry.

#### Sub-pass 2c: Structural Validation

After all expressions are resolved:

- **Computed field cycle detection** — build dependency graph from `ComputedExpression` references, DFS for cycles
- **Choice validation** — validate choice value sets, subset relationships, ordering constraints
- **Forward-reference prohibition** — default expressions may only reference fields declared before the current field (structural check; forward-reference *detection* in computed fields is already handled by the NameBinder)
- **Stateless/stateful cross-validation** — states present + event handlers conflict
- **Initial event field assignment completeness** — if initial event exists, verify required fields are assigned

---

## 7. Component Mechanics

### 7.1 SemanticIndex Shape

The SemanticIndex is governed by `docs/compiler-and-runtime-design.md §6`. This section specifies type-checker-specific record type details. See the governing doc for anti-mirroring rules, back-pointer discipline, and inventory organization principles.

### Collection Type Decision

**Array-primary + frozen dictionary secondary.** The `Functions.ByName` pattern:

```csharp
// Primary: ordered array (preserves declaration order)
ImmutableArray<TypedField> Fields { get; }

// Secondary: derived frozen lookup (O(1) by name)
FrozenDictionary<string, TypedField> FieldsByName { get; }
```

Same pattern for states, events, args. `ImmutableDictionary` is **not used** as primary storage anywhere in SemanticIndex.

**Rationale:**
- Declaration order matters for "prior fields only" scope (§3.5 default value forward-reference prohibition)
- Declaration order matters for LS hover and MCP compile output (users expect source order)
- `FrozenDictionary` gives O(1) lookup for name resolution
- Follows the established pattern (`Functions.ByName`: `FrozenDictionary<string, FunctionMeta[]>` derived from the ordered `All` array)

### Record Types

#### Symbols

```csharp
public sealed record TypedField(
    string Name,
    TypeKind ResolvedType,
    TypeKind? ElementType,            // for collections: the inner type
    TypeKind? KeyType,                // for lookup/logBy/queueBy: the key type
    ImmutableArray<ModifierKind> Modifiers,
    ImmutableArray<ModifierKind> ImpliedModifiers,  // from TypeMeta.ImpliedModifiers
    TypedExpression? DefaultExpression,
    TypedExpression? ComputedExpression,
    QualifierBinding? Qualifier,      // resolved qualifier values
    bool IsComputed,
    bool IsOptional,
    bool IsWritable,                  // baseline writable from modifier
    ParsedConstruct Syntax            // back-pointer to source construct
);
```



```csharp
public sealed record TypedState(
    string Name,
    ImmutableArray<ModifierKind> Modifiers,  // initial, terminal, required, irreversible, etc.
    ParsedConstruct Syntax
);

public sealed record TypedEvent(
    string Name,
    ImmutableArray<TypedArg> Args,
    bool IsInitial,
    ParsedConstruct Syntax
);

public sealed record TypedArg(
    string Name,
    string EventName,
    TypeKind ResolvedType,
    TypeKind? ElementType,
    ImmutableArray<ModifierKind> Modifiers,
    TypedExpression? DefaultExpression,
    bool IsOptional,
    SourceSpan Span                   // no separate construct — arg is part of event construct
);
```

#### QualifierBinding DU

```csharp
public abstract record QualifierBinding;
public sealed record InheritedQualifier(string FieldName) : QualifierBinding;
public sealed record SameQualifierRequired : QualifierBinding;
```

- `InheritedQualifier` — result inherits qualifier identity from the named field
- `SameQualifierRequired` — both operands must have the same qualifier; result inherits

Qualifier propagation is a type-checker concern for structural validation only. The actual qualifier *value* (`"USD"`, `"kg"`) is a runtime concern — the checker can't know it at compile time. The checker validates qualifier *compatibility* when `FindCandidates` returns multiple entries disambiguated by `QualifierMatch`. The **ProofEngine** handles deeper obligations (e.g., "prove these two money values have the same currency").

#### Normalized Declarations

```csharp
public sealed record TypedTransitionRow(
    string? FromState,         // null = "any-state wildcard" (fires in any source state)
    string EventName,
    string? TargetState,       // null for "no transition" / reject
    TypedExpression? Guard,
    ImmutableArray<TypedAction> Actions,
    TransitionOutcome Outcome, // Transition | NoTransition | Reject
    string? RejectReason,      // non-null iff Outcome == Reject; authored "because" text (CC#11)
    QualifierBinding? ResultQualifier,
    ParsedConstruct Syntax
);

public enum TransitionOutcome { Transition, NoTransition, Reject }

public sealed record TypedRule(
    TypedExpression Condition,
    TypedExpression? Guard,
    TypedExpression Message,
    ImmutableArray<string> SemanticSubjects,  // field names referenced
    ParsedConstruct Syntax
);

public sealed record TypedEnsure(
    ConstraintKind Kind,
    string? AnchorState,       // for state-anchored
    string? AnchorEvent,       // for event-anchored
    TypedExpression Condition,
    TypedExpression? Guard,
    TypedExpression Message,
    ImmutableArray<string> SemanticSubjects,
    ParsedConstruct Syntax
);

public sealed record TypedAccessMode(
    string StateName,
    string FieldName,
    ModifierKind Mode,         // Write, Read, or Omit
    TypedExpression? Guard,
    ParsedConstruct Syntax
);

public sealed record TypedStateHook(
    AnchorScope Scope,         // OnEntry or OnExit
    string StateName,
    TypedExpression? Guard,
    ImmutableArray<TypedAction> Actions,
    ParsedConstruct Syntax
);

public sealed record TypedEventHandler(
    string EventName,
    ImmutableArray<TypedAction> Actions,
    ParsedConstruct Syntax
);

/// Placeholder for stateless-precept edit declarations (edit all / edit Field1, Field2).
public sealed record TypedEditDeclaration(
    ImmutableArray<string> EditableFields,  // empty = "all"
    bool IsEditAll,
    ParsedConstruct Syntax
);
```

**`TypedTransitionRow.FromState` convention:** `null` means "any-state wildcard" — the row fires in any source state. This is a binary discriminator (named state vs wildcard) that will never gain a third case; a full DU would be over-abstraction. GraphAnalyzer filters "any-state rows" with `== null`.

#### Typed Actions (3-Shape DU)

```csharp
/// Base typed action — no operand (clear).
public record TypedAction(
    ActionKind Kind,
    string FieldName,
    TypeKind FieldType,
    ImmutableArray<ProofRequirement> ProofRequirements,
    SourceSpan Span                   // action is part of containing construct
);

/// Input action — carries resolved value expression.
public sealed record TypedInputAction(
    ActionKind Kind,
    string FieldName,
    TypeKind FieldType,
    TypedExpression InputExpression,
    TypedExpression? SecondaryExpression,
    ActionSecondaryRole? SecondaryRole,  // null iff SecondaryExpression is null
    ImmutableArray<ProofRequirement> ProofRequirements,
    SourceSpan Span
) : TypedAction(Kind, FieldName, FieldType, ProofRequirements, Span);

/// Binding action — carries target binding (dequeue into, pop into).
public sealed record TypedBindingAction(
    ActionKind Kind,
    string FieldName,
    TypeKind FieldType,
    string? Binding,           // "into" target field name, null if no into
    ImmutableArray<ProofRequirement> ProofRequirements,
    SourceSpan Span
) : TypedAction(Kind, FieldName, FieldType, ProofRequirements, Span);

public enum ActionSecondaryRole
{
    Index,     // insert ... at <index>
    Key,       // put ... key <key>, appendBy <key>, enqueueBy <key>
}
```

**`ActionSecondaryRole` rationale:** A single nullable `SecondaryExpression` without a discriminator forces the Evaluator to back-reference `ActionKind` to determine dispatch — defeating the DU's purpose. The enum carries role semantics; the Evaluator switches on it. Invariant: `SecondaryRole.HasValue == (SecondaryExpression != null)`, enforced at construction time. Start with `Index` and `Key` only; `Priority` can be added if the Evaluator genuinely distinguishes it from `Key`.

#### Typed Expressions (DU)

`TypedExpression` is a sealed abstract record DU — the type checker's output for expressions. Its parser-side counterpart is `ParsedExpression` (same closed DU pattern, unresolved types). The set is closed by design: adding a new expression form requires a new catalog entry + new DU subtype + updating all consumer switch arms. Exhaustiveness is enforced via: (1) sealed class hierarchy (CS8509/CS8524 on switch expressions); (2) `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]` + PRECEPT0019 for multi-method consumers.

```csharp
public abstract record TypedExpression(
    TypeKind ResultType,
    SourceSpan Span               // expressions don't have separate ParsedConstruct
);

public sealed record TypedFieldRef(
    TypeKind ResultType,
    string FieldName,
    bool IsCaseInsensitive,    // carries ~string flag
    SourceSpan Span
) : TypedExpression(ResultType, Span);

public sealed record TypedArgRef(
    TypeKind ResultType,
    string EventName,
    string ArgName,
    SourceSpan Span
) : TypedExpression(ResultType, Span);

public sealed record TypedLiteral(
    TypeKind ResultType,
    object? Value,             // parsed literal value
    SourceSpan Span
) : TypedExpression(ResultType, Span);

public sealed record TypedBinaryOp(
    TypeKind ResultType,
    OperationKind ResolvedOp,
    TypedExpression Left,
    TypedExpression Right,
    QualifierBinding? ResultQualifier,
    ImmutableArray<ProofRequirement> ProofRequirements,
    SourceSpan Span
) : TypedExpression(ResultType, Span);

public sealed record TypedUnaryOp(
    TypeKind ResultType,
    OperationKind ResolvedOp,
    TypedExpression Operand,
    SourceSpan Span
) : TypedExpression(ResultType, Span);

public sealed record TypedFunctionCall(
    TypeKind ResultType,
    FunctionKind ResolvedFunction,
    ImmutableArray<TypedExpression> Arguments,
    ImmutableArray<ProofRequirement> ProofRequirements,
    SourceSpan Span
) : TypedExpression(ResultType, Span);

public sealed record TypedMemberAccess(
    TypeKind ResultType,
    TypedExpression Object,
    TypeAccessor ResolvedAccessor,
    ImmutableArray<ProofRequirement> ProofRequirements,
    SourceSpan Span
) : TypedExpression(ResultType, Span);

public sealed record TypedConditional(
    TypeKind ResultType,
    TypedExpression Condition,
    TypedExpression ThenBranch,
    TypedExpression ElseBranch,
    SourceSpan Span
) : TypedExpression(ResultType, Span);

public sealed record TypedQuantifier(
    TypeKind ResultType,       // always Boolean
    string BindingName,
    TypeKind BindingType,
    TypedExpression Collection,
    TypedExpression Predicate,
    SourceSpan Span
) : TypedExpression(ResultType, Span);

/// Error expression — propagates ErrorType, replaces failed sub-expressions.
public sealed record TypedErrorExpression(
    SourceSpan Span
) : TypedExpression(TypeKind.Error, Span);
```

#### Dependency Facts

```csharp
public sealed record ComputedFieldDep(
    string FieldName,
    ImmutableArray<string> DependsOn
);

public sealed record ConstraintFieldRefs(
    ConstraintIdentity ConstraintIdentity,  // typed DU: RuleIdentity or EnsureIdentity (proof-engine.md §2)
    ImmutableArray<string> ReferencedFields,
    ImmutableArray<string> ReferencedArgs
);
```

> **✅ Resolved (CC#9):** `ConstraintFieldRefs.ConstraintIdentity` uses the proof-engine `ConstraintIdentity` DU — the same `abstract record ConstraintIdentity` with `RuleIdentity` and `EnsureIdentity` subtypes defined in `proof-engine.md §2`. `object` is replaced; compile-time exhaustiveness is guaranteed. The DU crosses from `ProofEngine` into `SemanticIndex` via `ConstraintInfluenceEntry`, so both stages share the identical type.
> *Closed: 2026-05-06. CC#9 resolved.*

#### SemanticIndex Record

```csharp
public sealed record SemanticIndex(
    // Symbol tables — ordered arrays (primary)
    ImmutableArray<TypedField> Fields,
    ImmutableArray<TypedState> States,
    ImmutableArray<TypedEvent> Events,

    // Derived lookup indexes (secondary)
    FrozenDictionary<string, TypedField> FieldsByName,
    FrozenDictionary<string, TypedState> StatesByName,
    FrozenDictionary<string, TypedEvent> EventsByName,

    // Normalized declarations — ordered arrays
    ImmutableArray<TypedTransitionRow> TransitionRows,
    ImmutableArray<TypedRule> Rules,
    ImmutableArray<TypedEnsure> Ensures,
    ImmutableArray<TypedAccessMode> AccessModes,
    ImmutableArray<TypedStateHook> StateHooks,
    ImmutableArray<TypedEventHandler> EventHandlers,
    ImmutableArray<TypedEditDeclaration> EditDeclarations,

    // Secondary derived indexes over normalized declarations
    FrozenDictionary<string, ImmutableArray<TypedEnsure>> EnsuresByState,   // CC#22: state-anchored ensures by state name; follows CC#3 pattern

    // Dependency facts
    ImmutableArray<ComputedFieldDep> ComputedDeps,
    ImmutableArray<ConstraintFieldRefs> ConstraintRefs,

    // Reference sites — recorded at resolution time for LS semantic tokens and navigation
    ImmutableArray<FieldReference> FieldReferences,
    ImmutableArray<StateReference> StateReferences,
    ImmutableArray<EventReference> EventReferences,

    // Diagnostics
    ImmutableArray<Diagnostic> Diagnostics
);
```

> **Resolved (CC#3):** `SemanticIndex` reference collections — Option A adopted. Three typed per-category arrays (`FieldReferences`, `StateReferences`, `EventReferences`) are added. The type checker records every reference site (span + resolved binding) at resolution time. No general heterogeneous `References` array — per-type arrays are sufficient for LS Pass 2 and navigation. Reference types: `FieldReference(TypedField Field, SourceSpan Site)`, `StateReference(TypedState State, SourceSpan Site)`, `EventReference(TypedEvent Event, SourceSpan Site)`.
> *Closed: 2026-05-06. CC#3 resolved.*

> **Resolved (CC#3):** `SemanticIndex.FieldReferences` — first-class output. See ruling above.
> *Closed: 2026-05-06. CC#3 resolved.*

> **Resolved (CC#3):** `SemanticIndex.StateReferences` — first-class output. See ruling above.
> *Closed: 2026-05-06. CC#3 resolved.*

> **Resolved (CC#3):** `SemanticIndex.EventReferences` — first-class output. See ruling above.
> *Closed: 2026-05-06. CC#3 resolved.*

> **Resolved (CC#22):** `SemanticIndex.EnsuresByState` — `FrozenDictionary<string, ImmutableArray<TypedEnsure>>` added. Follows the CC#3 primary-array + secondary-index pattern. `Ensures` remains the primary ordered array; `EnsuresByState` is the derived O(1) secondary index built at type-checker construction time. Only state-anchored ensures (Kind ∈ `{StateResident, StateEntry, StateExit}` where `AnchorState != null`) are included. Key is the state name string.
> *Closed: 2026-05-06. CC#22 resolved.*

---

### 7.2 CheckContext: Mutable Working State

`CheckContext` is the internal mutable state used during the check pass. It is **not** part of the public `SemanticIndex` contract.

```csharp
internal sealed class CheckContext
{
    // Symbol tables (pre-resolved by NameBinder — CheckContext reads from SymbolTable)
    // TypeChecker populates these typed versions from SymbolTable declarations
    public List<TypedField> Fields { get; } = [];
    public Dictionary<string, TypedField> FieldLookup { get; } = new();
    public List<TypedState> States { get; } = [];
    public Dictionary<string, TypedState> StateLookup { get; } = new();
    public List<TypedEvent> Events { get; } = [];
    public Dictionary<string, TypedEvent> EventLookup { get; } = new();

    // Current scope (for Pass 2)
    public IReadOnlyDictionary<string, TypedArg>? CurrentEventArgs { get; set; }
    public int CurrentFieldIndex { get; set; } = -1;  // for "prior fields only" scope
    public FieldScopeMode CurrentScope { get; set; } = FieldScopeMode.AllFields;

    // Quantifier binding stack (for nested quantifiers)
    public Stack<(string Name, TypeKind Type)> QuantifierBindings { get; } = new();

    // Diagnostics accumulator
    public List<Diagnostic> Diagnostics { get; } = [];
}

public enum FieldScopeMode { AllFields, PriorFieldsOnly }
```

Scope is managed by setting `CurrentEventArgs` when entering a transition row, event handler, or event-anchored ensure, and clearing it on exit. `CurrentFieldIndex` tracks the position in the field array for forward-reference prohibition in default value expressions. `CurrentScope` controls whether identifier resolution enforces "prior fields only" (for computed/default expressions) or allows all fields (for guards, actions, rules). `QuantifierBindings` is a stack for nested quantifier variable scoping.

---

### 7.3 Expression Resolution (UNBLOCKED)

Parser now produces `ParsedExpression` DU nodes. The design below is complete and ready for implementation.

#### The Core Resolve Function

The `Resolve(ParsedExpression expr, TypeKind? expectedType)` function is the metadata interpreter core. The `expectedType` parameter enables top-down context propagation for numeric literal resolution and typed constants.

### Catalog Lookup Strategy

The checker uses existing catalog APIs directly — no new indexes:

| API | Location | Returns |
|---|---|---|
| `Operations.FindCandidates(op, lhs, rhs)` | `Operations.cs:1153` | `ReadOnlySpan<BinaryOperationMeta>` |
| `Operations.FindUnary(op, operand)` | `Operations.cs` | `UnaryOperationMeta?` |
| `Functions.ByName` | `Functions.cs` | `FrozenDictionary<string, FunctionMeta[]>` |
| `Types.GetMeta(typeKind)` | `Types.cs` | `TypeMeta` with `.Accessors`, `.WidensTo` |
| `Modifiers` catalog entries | `Modifiers.cs` | `.ApplicableTo`, `.MutuallyExclusiveWith`, `.Subsumes` |

**No new `BinaryBySignature` / `UnaryBySignature` indexes.** The original proposal for these is withdrawn — `FindCandidates` and `FindUnary` are the correct APIs. They already exist as frozen indexes with convenience wrappers.

### Qualifier Disambiguation Logic

`FindCandidates` returns `BinaryOperationMeta[]`, not a single entry. For operations like money/money division, there are entries with both `QualifierMatch.Same` and `QualifierMatch.Different` (e.g., `Operations.cs` lines 425/435, 504/514). The checker applies ~15 lines of structural logic after a multi-candidate return:

```csharp
// After FindCandidates returns > 1 entry:
var candidates = Operations.FindCandidates(op, leftType, rightType);
if (candidates.Length == 1) return candidates[0];

// Qualifier disambiguation:
// 1. Check whether operand qualifiers are known to match
// 2. If qualifiers match → select the QualifierMatch.Same entry
// 3. If qualifiers differ → select the QualifierMatch.Different entry (if present)
// 4. If no match → emit QualifierMismatch diagnostic, return error
```

This is the one point where "pure catalog lookup" has a structural-logic layer on top. The catalog can't do this because qualifier identity requires knowing the actual field qualifiers at the expression site.

### Type Widening Integration

Widening is NOT a separate phase. It's a helper function used inside resolution:

```csharp
bool IsAssignable(TypeKind source, TypeKind target)
{
    if (source == target) return true;
    if (source == TypeKind.Error || target == TypeKind.Error) return true;
    return Types.GetMeta(source).WidensTo.Contains(target);
}
```

**Widening is single-hop only.** `WidensTo` arrays are designed to be complete for each type (e.g., `IntegerWidens = [Decimal, Number]` — integer reaches both directly). No transitive resolution.

Used in: assignment validation, function overload matching, binary operation lookup fallback (try widened variants), default value validation, conditional branch unification.

### Binary Operation Widening Fallback

When `FindCandidates(op, leftType, rightType)` returns empty, the checker tries widened combinations in deterministic priority order:

```
ResolveOp(op, leftType, rightType):
  1. candidates = FindCandidates(op, leftType, rightType)
  2. if candidates.Length >= 1 → disambiguate (qualifier or single), done
  3. Try LEFT widening only:
     for each wt in Types.GetMeta(leftType).WidensTo:
       candidates = FindCandidates(op, wt, rightType)
       if candidates.Length >= 1 → disambiguate, done
  4. Try RIGHT widening only:
     for each wt in Types.GetMeta(rightType).WidensTo:
       candidates = FindCandidates(op, leftType, wt)
       if candidates.Length >= 1 → disambiguate, done
  5. Try BOTH widening:
     for each lwt in Types.GetMeta(leftType).WidensTo:
       for each rwt in Types.GetMeta(rightType).WidensTo:
         candidates = FindCandidates(op, lwt, rwt)
         if candidates.Length >= 1 → disambiguate, done
  6. Emit "NoMatchingOperation" diagnostic, return TypedErrorExpression
```

Priority: left-first → right-first → both. `WidensTo` array order is the tiebreaker (narrowest-first by convention).

### Numeric Literal Context Resolution

Bare numeric literals resolve to `integer` by default (bottom-up). When binary operation or function call resolution fails with a literal operand:

1. Resolve both operands bottom-up (literal → integer)
2. Try FindCandidates + widening fallback
3. If failure AND one operand is a bare `LiteralExpression` → retry that operand with `expectedType` from the other side's resolved type
4. If failure AND both are bare literals → both remain integer; emit diagnostic

Context retry is the mechanism that makes `amount > 100` (where `amount: money`) work: initial resolution produces `(>, money, integer)` → no match → retry `100` with expectedType=money → `(>, money, money)` → match.

**Implementation timing:** Slices 2–3 use bottom-up only. Slice 4 adds the context retry mechanism (part of `expectedType` propagation).

### Function Overload Resolution

```
ResolveFunctionCall(name, resolvedArgs[]):
  1. allOverloads = Functions.ByName[name].SelectMany(fm => fm.Overloads)
  2. Filter by arity: keep only overloads where Parameters.Length == resolvedArgs.Length
  3. For each remaining overload, score:
     a. EXACT match:  all arg types == parameter types → score 0 (best)
     b. WIDENED match: all args IsAssignable to params → score = count of widened args
     c. NO match:     skip
  4. If exactly one score-0 entry → select it.
  5. If multiple score-0 → ambiguity error
  6. If no score-0 but one or more widened → select lowest score
  7. If no match → retry with context propagation for literal args, then:
  8. If still no match → emit "NoMatchingOverload" diagnostic, return TypedErrorExpression
```

For context retry (step 7): if an argument is a bare `LiteralExpression`, re-resolve it with `expectedType` = each candidate's parameter type at that position. This handles `min(amount, 100)` where `amount: money` and `100` must resolve as money.

### Accessor Return-Type Resolution

When resolving member access or method call expressions, the return type and parameter type depend on the accessor DU subtype:

```csharp
(TypeKind returnType, TypeKind? paramType) = resolvedAccessor switch
{
    // Base TypeAccessor (peek, dequeue, pop): returns element type of owning collection
    TypeAccessor a when a is not FixedReturnAccessor and a is not ElementParameterAccessor
        => (owningField.ElementType!.Value, null),

    // FixedReturnAccessor (date.year, date.month): returns accessor.Returns directly
    FixedReturnAccessor f
        => (f.Returns, f.ParameterType),

    // ElementParameterAccessor (bag.countof(x)): return = integer, param = element type
    ElementParameterAccessor e
        => (TypeKind.Integer, owningField.ElementType!.Value),
};
```

For method call expressions (accessor with parameters), validate the argument type against `paramType`. If `paramType` is null, the accessor is property-style — a call syntax `field.accessor()` emits a diagnostic.

### Identifier Resolution Priority

When resolving an identifier expression, check scopes in this order:

1. **Quantifier bindings** (top of stack first — innermost binding wins)
2. **Event args** (`CurrentEventArgs` if set)
3. **Fields** (`FieldLookup`, gated by `CurrentScope` and `CurrentFieldIndex`)
4. **Error:** emit "UnresolvedIdentifier" diagnostic, return `TypedErrorExpression`

For step 3 with `CurrentScope == PriorFieldsOnly`: if the resolved field's index >= `CurrentFieldIndex`, emit "ForwardReferenceProhibited" diagnostic instead.

### Stub Strategy for Unimplemented Arms

Every expression node type that won't be implemented in its slice has an explicit stub arm returning `TypedErrorExpression` with a `NotYetImplemented` marker. No switch fallthrough, no crash. This is required from Slice 2 onward to prevent test failures when expressions contain forms not yet handled.

---

## Catalog Integration (part of §7)

### What the Checker Reads

| Catalog | What the checker uses | Section |
|---|---|---|
| **Types** | `WidensTo`, `Accessors`, `TypeCategory`, `ImpliedModifiers`, `ByTokenKind` | Widening, member access, literal classification |
| **Operations** | `FindCandidates`, `FindUnary`, `BinaryOperationMeta.QualifierMatch`, `.ProofRequirements` | Binary/unary expression resolution |
| **Functions** | `ByName`, `FunctionOverload.Parameters`, `.ProofRequirements`, `.HasCIVariant` | Function call resolution, CI enforcement |
| **Modifiers** | `ApplicableTo`, `MutuallyExclusiveWith`, `Subsumes` | Modifier validation |
| **Actions** | `ApplicableTo`, `AllowedIn`, `ValueRequired`, `ActionSyntaxShape` | Action resolution and classification |

> **✅ Resolved in Source — ActionMeta.SyntaxShape:** `Action.cs` already carries a `SyntaxShape` property (`ActionSyntaxShape`). Update `catalog-system.md` to include it in the canonical `ActionMeta` shape and remove this as an open question.

### Catalog-Driven vs Structural Logic (~70/30 Split)

| Category | % Catalog | % Structural |
|---|---|---|
| Type widening | 100% | 0% |
| Expression typing (binary/unary/accessor/function) | ~90% | ~10% |
| Function validation | ~95% | ~5% |
| Modifier validation | ~65% | ~35% |
| Action validation | ~70% | ~30% |
| Name resolution | 0% | 100% |
| Scope rules | 0% | 100% |
| Computed field validation | ~20% | ~80% |
| Choice validation | 0% | 100% |
| Error recovery | 0% | 100% |

The structural logic clusters in: name resolution (symbol tables), scope management, dependency graph cycles, and choice-specific validation.

---

## Catalog Gaps (part of §13)

### Gap 1: ContentValidation DU on TypeMeta — HIGH

**Status:** Design locked, implementation pending (separate PR)

The typed constant content validation patterns (date = YYYY-MM-DD, money = `<number> <currency>`, etc.) need catalog representation to avoid a per-`TypeKind` switch in the checker.

**Locked shape:**

```csharp
// New field on TypeMeta
ContentValidation? ContentValidation = null

// DU shape:
public abstract record ContentValidation(string FormatDescription, string[] Examples);
public sealed record RegexValidation(string Pattern, string FormatDescription, string[] Examples) : ContentValidation(...);
public sealed record NodaTimeValidation(string NodaTimePattern, string FormatDescription, string[] Examples) : ContentValidation(...);
public sealed record ClosedSetValidation(string SetName, string FormatDescription, string[] Examples) : ContentValidation(...);
```

- `RegexValidation` — freeform patterns
- `NodaTimeValidation` — date, time, datetime, period types (delegates to NodaTime parser)
- `ClosedSetValidation` — currency (ISO 4217), unit (UCUM) (membership check)

**Dependency:** Slice 4 (Typed Constants). If not landed before Slice 4, use a hardcoded per-TypeKind dispatch table with a TODO referencing this gap.

### Gap 3: TypedActionShape on ActionMeta — LOW (deprioritized)

**Status:** Acceptable as checker logic

The mapping from `ActionSyntaxShape` → typed DU shape is a stable 3-arm switch (`FieldOnly` → Base, `CollectionInto/CollectionIntoBy` → Binding, all others → Input). The DU's semantic meaning reflects structural ownership (no operand / carries expression / carries binding), not surface syntax — so new `ActionSyntaxShape` values naturally fall into existing categories. An explicit `TypedActionShape` field can be added later but is not a blocker.

### Gap 4: ~string CI Enforcement — LOW (acceptable as checker logic)

Five stable rules. The 5-rule enforcement surface is small enough that checker logic is acceptable without catalog metadata for the diagnostic dispatch.

> **✅ Resolved in Source — FunctionMeta.HasCIVariant:** `Function.cs` already carries `HasCIVariant` and `CIVariantOf` properties. Update `catalog-system.md` to include them in the canonical `FunctionMeta` shape.

---

## 8. Dependencies and Integration Points

### Upstream

| Component | What it provides |
|---|---|
| **Parser** | `ConstructManifest` containing `ImmutableArray<ParsedConstruct>` — the generic construct nodes with typed slots |
| **Constructs catalog** | `ConstructMeta` with slot layouts — defines what slots each `ConstructKind` has |
| **Types catalog** | `TypeMeta` with `WidensTo`, `Accessors`, `ImpliedModifiers` |
| **Operations catalog** | `FindCandidates()`, `FindUnary()` — operator lookup |
| **Functions catalog** | `ByName` — function overload lookup |
| **Modifiers catalog** | `ApplicableTo`, `MutuallyExclusiveWith`, `Subsumes` |
| **Actions catalog** | `ApplicableTo`, `AllowedIn`, `ValueRequired`, `ActionSyntaxShape` |

### Downstream

| Component | What it consumes |
|---|---|
| **GraphAnalyzer** | `SemanticIndex.States`, `SemanticIndex.TransitionRows`, `SemanticIndex.StateHooks` — builds state-transition graph |
| **ProofEngine** | `ProofRequirement` arrays on typed expressions — discharges proof obligations |
| **PreceptBuilder** | Full `SemanticIndex` — produces runtime descriptor |
| **Language Server** | `SemanticIndex` for hover, go-to-definition, semantic tokens, completions |

---

## 9. Failure Modes and Recovery

**Policy:** Always produce partial results. The type checker accumulates diagnostics without abandoning any pass.

**Sub-expression failure handling:**
- Any sub-expression that fails resolution is replaced with `TypedErrorExpression` (carrying the diagnostic and source span)
- The containing declaration is still emitted to the SemanticIndex
- Downstream stages (GraphAnalyzer, ProofEngine) must handle `TypedErrorExpression` gracefully — typically by skipping proof obligations on that expression but still analyzing structural topology

**Per-declaration behavior:**

| Declaration | Error in sub-expression | Result |
|---|---|---|
| `TypedField` | Failed default expression | Emitted; `DefaultExpression = TypedErrorExpression` |
| `TypedTransitionRow` | Failed guard | Emitted; `Guard = TypedErrorExpression` |
| `TypedTransitionRow` | Failed action expression | Emitted; action carries `TypedErrorExpression` |
| `TypedRule` | Failed condition | Emitted; `Condition = TypedErrorExpression` |
| `TypedEnsure` | Failed body | Emitted; `Condition = TypedErrorExpression` |

No declaration type is ever skipped due to sub-expression errors.

**ErrorType propagation:** Any operation with an `ErrorType` operand produces `ErrorType` result — suppresses downstream diagnostics for the same expression tree, preventing diagnostic cascades.

---

## 10. Contracts and Guarantees

| Guarantee | Description |
|---|---|
| **Total function** | Every `ConstructManifest` produces a `SemanticIndex`, even with errors |
| **Diagnostic completeness** | Every `TypedErrorExpression` has a corresponding `Diagnostic` |
| **Declaration preservation** | Every parseable declaration appears in the output, even if sub-expressions failed |
| **No cascading errors** | `ErrorType` propagation prevents duplicate diagnostics for the same failure |
| **Determinism** | Same input always produces same output (no random, no clock) |

---

## 11. Design Rationale and Decisions

| # | Decision | Original Proposal | Revised (Locked) | Rationale |
|---|---|---|---|---|
| 1 | Catalog lookup API | New `BinaryBySignature` / `UnaryBySignature` | Use existing `FindCandidates()` / `FindUnary()` | APIs already exist with correct semantics; no duplication |
| 2 | BinaryIndex disambiguation | Not addressed | ~15 lines qualifier-match logic after multi-candidate return | Qualifier identity is a runtime value the catalog can't know |
| 3 | SemanticIndex record placement | Slice 10 | Pre-Slice 0 commit | Can't write Slice 2 tests without type definitions to compile against |
| 4 | Field storage | `ImmutableDictionary<string, TypedField>` | `ImmutableArray<TypedField>` primary + `FrozenDictionary` secondary | Declaration order matters for scope and display; lookup needs O(1) |
| 5 | TypedInputAction secondary | Single nullable, no discriminator | Single nullable + `ActionSecondaryRole?` enum | Evaluator can't dispatch without knowing the role |
| 6 | Expression form coverage | Not addressed | CS8509 exhaustive switch + xUnit coverage tests | Compiler + tests ensure all forms handled; no annotation overhead |
| 7 | Resolve line count | ~100 lines | ~250–350 lines (16+ arms) | 7 missing expression forms identified; each needs explicit handling |
| 8 | Gap 5 (pow) | Active blocker | Closed — `NumericProofRequirement` already in `Functions.cs` | GAP-032 fixed 2026-05-02 |
| 9 | ContentValidation shape | Flat record | DU: Regex / NodaTime / ClosedSet subtypes | Flat record still requires a hidden per-type switch |
| 10 | TypedTransitionRow.FromState | Unspecified | `string?` with null = "any" convention (XML doc mandatory) | Binary discriminator; DU for a never-grows-third-case is over-abstraction |
| 11 | Qualifier propagation | Not addressed | `QualifierBinding?` on `TypedBinaryOp`; proof obligations for compatibility | Type checker validates structure; ProofEngine handles runtime identity |
| 12 | Error recovery | Implicit | Always produce partial result; `TypedErrorExpression` replaces failed sub-exprs | Consistent with "accumulate diagnostics without abandoning" principle |
| 13 | Interpolated string | No slice | `InterpolatedStringExpression` → Slice 3; `InterpolatedTypedConstantExpression` → Slice 4 | Not CI/string operations; belongs with general expression machinery |
| 14 | MethodCallExpression | Not addressed | Accessor-style lookup via TypeMeta; Slice 3 | Current surface only has collection accessors (`queue.peek()`, etc.) |
| 15 | Widening transitivity | Not addressed | Single-hop only; `WidensTo` arrays are complete per type | Transitive adds complexity and confusing errors; catalog arrays encode all reachable targets directly |
| 16 | Binary op widening fallback | Not addressed | Left-first → right-first → both; `WidensTo` order is priority | Deterministic; narrowest-widen-first by array convention |
| 17 | Numeric literal default | Not addressed | Integer default + one-retry context propagation (Slice 4) | Hybrid simplicity: bottom-up works for most cases, retry for context-sensitive |
| 18 | EventHandler scope | Not addressed | Has event arg scope (same `CurrentEventArgs` pattern as transition rows) | Event handler construct names the event; args naturally in scope |
| 19 | Forward-reference gate | Implicit | `FieldScopeMode` enum in CheckContext; check in identifier resolution | Generalizable scope restriction; fires at resolution time, not as separate validation |
| 20 | Identifier resolution priority | Not addressed | Quantifier bindings > event args > fields | Innermost scope wins; shadowing is predictable and standard |
| 21 | Function overload resolution | Not addressed | Arity filter → exact → widened → context retry for literals | Single deterministic algorithm; no ambiguity with current catalog |
| 22 | Slice 6 split | George suggestion (6a/6b) | Rejected — keep as single slice | IsSet/IsNotSet is 10 lines; splitting adds overhead with no parallelism gain |
| 23 | TypedTransitionRow.ResolvedArgs | Kramer R3 | Rejected — single dict lookup doesn't justify cached copies | Anti-mirroring: data already in `EventsByName[row.EventName].Args` |
| 24 | TypedEditDeclaration | Kramer R4 | Placeholder record in Pre-Slice 0; full implementation deferred | Correct eventual shape for stateless-precept edit support |
| 25 | ExpressionFormKind.Literal ownership | Not addressed | Implemented in early slices; sub-forms handled within single switch arm | Single switch arm handles all literal sub-forms; later slices add arms within the literal handler |
| 26 | ErrorGuaranteed debug assertion | LOW (research cross-reference) | In-scope for Slice 10: debug/test-time assertion validates any SemanticIndex containing a `TypedErrorExpression` also contains ≥1 Error-severity Diagnostic | Zero production cost; catches orphaned error expressions where we produce `TypedErrorExpression` without emitting the corresponding diagnostic |

---

## 12. Innovation

### Catalog-Driven Expression Resolution

The type checker's expression resolution engine is **~70% catalog-driven**. For binary operations, unary operations, function calls, and member accessors, the checker:

1. Asks the catalog for candidates
2. Applies disambiguation logic (~15 lines for qualifier matching, ~20 lines for widening fallback)
3. Records the result

This means new operators, functions, and accessors added to the catalogs work automatically — no type checker changes needed.

### Generic Construct Dispatch

The type checker dispatches on `ConstructKind` enum values via exhaustive switch, not on C# AST node types. This aligns with the catalog-driven architecture where construct layouts are metadata, not hard-coded type hierarchies.

---

## 13. Open Questions / Implementation Notes

### Implementation Plan (UNBLOCKED)

Implementation unblocked. Parser now produces `ParsedExpression` DU nodes. The following slices can proceed:

**Pre-Slice 0: Shape Commit (unblocks everything)**
- All `TypedField`, `TypedState`, `TypedEvent`, `TypedArg` record definitions
- `TypedExpression` DU (all subtypes including `TypedErrorExpression`)
- `TypedAction` DU (3 shapes + `ActionSecondaryRole` enum)
- `TypedTransitionRow`, `TypedEnsure`, `TypedRule`, `TypedAccessMode`, `TypedStateHook`, `TypedEventHandler`
- `TypedEditDeclaration` placeholder (for future stateless-precept edit support)
- `QualifierBinding` DU
- `CheckContext` internal class (including `FieldScopeMode`, `QuantifierBindings` stack)
- `SemanticIndex` expanded with typed inventories and derived lookup indexes
- Test infrastructure: `TypeCheck(string source) → SemanticIndex` and `TypeCheckExpr(string source) → TypedExpression` helpers in test project
- **No logic, no behavioral tests** — build verification only
- This commit unblocks all numbered slices

**Slice 1: Typed Symbol Population**
- Populate `CheckContext` typed fields/states/events from `SymbolTable` declarations
- Type resolution via `TypeMeta` → `TypeKind` mapping
- Initial/terminal state counting and validation

**Slice 2: Scalar Expression Resolution — Binary & Unary Ops**
- `Resolve()` function with arms for literal, identifier, binary, unary, parenthesized expressions
- `Operations.FindCandidates()` + qualifier disambiguation logic + widening fallback
- `Operations.FindUnary()` integration
- ErrorType propagation
- `FieldScopeMode` check in identifier resolution (forward-reference gate)
- Stub arms for all other expression forms → `TypedErrorExpression`

**Slice 3: Functions, Accessors, Method Calls, Interpolated Strings**
- Function call → `Functions.ByName` lookup + overload resolution (arity → exact → widened → context retry)
- Member access → field ref or TypeAccessor lookup + return-type resolution via accessor DU
- Method call → resolve receiver, TypeMeta accessor dispatch, parameter-type validation
- Interpolated string → hole resolution, scalar check, result = `TypeKind.String`
- ProofRequirement recording from overload/accessor entries

**Slice 4: Typed Constants + Context-Sensitive Resolution**
- Typed constant → context type propagation + content validation
- Interpolated typed constant → same with interpolation holes
- Numeric literal context retry mechanism (`expectedType` propagation into binary ops and function calls)
- **Out-of-range literal checking:** validate numeric literal value against resolved type's representable range

**Slice 5: Transition Row + EventHandler Normalization**
- Guard expression resolution (boolean result required)
- Action chain resolution per `ActionSyntaxShape` → TypedAction DU
- `SecondaryExpression` + `SecondaryRole` assignment
- Transition target validation (state name lookup in symbol table)
- Partial result policy: failed guard/action → `TypedErrorExpression`, row still emitted
- Scope: set `CurrentEventArgs` when entering transition row OR event handler

**Slice 6: Structural Validation**
- IsSet/IsNotSet → operand must be optional field, result = boolean
- Computed field dependency graph + cycle detection (DFS)
- Choice field validation (values valid for type, no duplicates, subset/ordering)
- Forward-reference prohibition belt-and-suspenders validation

**Slice 7: Modifier Validation**
- Per-field modifier applicability (`ModifierMeta.ApplicableTo`)
- Modifier conflicts (`MutuallyExclusiveWith`)
- Subsumption warnings (`Subsumes`)
- Bounds validation (min > max, negative counts)
- `writable` on computed/arg prevention
- State modifier validation (exactly one initial, at least one terminal)
- **Independent of expression resolution** — depends only on Slice 1

**Slice 8: CI Enforcement**
- CI function call resolution → subject must be `~string`, lookup CI variant
- CI operator validation (case-insensitive comparison on `~string` operands)
- Diagnostic for CI function on non-`~string` operand

**Slice 9: Quantifiers + List Literals**
- Quantifier → push `(bindingName, bindingType)` onto `QuantifierBindings` stack, resolve predicate (must be boolean), resolve collection operand (must be collection type), pop binding on exit
- List literal → element type unification, result = inferred collection type
- Binding shadowing: quantifier bindings shadow event args and fields

**Slice 10: Final Assembly**
- `CheckContext` → immutable `SemanticIndex` transformation
- Array-to-frozen-dict derivation for lookup indexes
- Dependency fact extraction (computed-field deps, constraint-field refs)
- ErrorGuaranteed debug assertion
- Integration tests: full `.precept` files from `samples/` → complete `SemanticIndex`

### Open Questions

1. **Anti-mirroring enforcement mechanism** — structural test that graph analysis, proof, and builder code paths do NOT call back-pointer `.Syntax` properties. How to enforce: Roslyn analyzer, runtime reflection test, or code review convention?

2. **ContentValidation DU landing timeline** — if the catalog shape change is significantly delayed, the hardcoded dispatch table becomes long-lived debt.

3. **`~string` CI enforcement cataloging** — currently acceptable as 5-rule checker logic. If the CI surface grows beyond 5 rules, revisit whether `CIEnforcementDiagnostic?` belongs on `BinaryOperationMeta`.

4. **CandidateTypes on TypedErrorExpression** — LOW PRIORITY (post-v1). Consider adding an optional `ImmutableArray<TypeKind>? CandidateTypes` to `TypedErrorExpression` for "did you mean?" suggestions.

---

## 14. Deliberate Exclusions

- **No graph topology:** Reachability, dominance, and edge sets are the GraphAnalyzer's responsibility.
- **No proof obligation discharge:** ProofRequirement *recording* happens here (from catalog entries); ProofRequirement *discharge* is the ProofEngine's responsibility.
- **No runtime planning:** Descriptor production and execution plan compilation are the Precept Builder's responsibility.
- **No qualifier runtime identity:** The checker validates qualifier *compatibility* structurally; the Evaluator handles qualifier *values* at runtime.
- **No expression tree parsing:** The type checker resolves `ParsedExpression` DU nodes (produced by the parser) into `TypedExpression`. Expression tree design was previously blocked; resolved by CC#1.

---

## 15. Cross-References

| Topic | Document |
|---|---|
| SemanticIndex governance, anti-mirroring rules, inventory shape | `docs/compiler-and-runtime-design.md §6` |
| All catalogs the type checker consumes | `docs/language/catalog-system.md` |
| ConstructManifest input contract | `docs/compiler/parser.md` |
| SemanticIndex consumer (graph) | `docs/compiler/graph-analyzer.md` |
| ParsedConstruct and SlotValue definitions | `src/Precept/Pipeline/ParsedConstruct.cs`, `src/Precept/Pipeline/SlotValue.cs` |
| ConstructMeta slot layouts | `src/Precept/Language/Constructs.cs` |

---

## 16. Source Files

| File | Purpose |
|---|---|
| `src/Precept/Pipeline/TypeChecker.cs` | Type checker implementation — `TypeChecker` static class with `Check(ConstructManifest, SymbolTable)` entry point |
| `src/Precept/Pipeline/SemanticIndex.cs` | `SemanticIndex` — flat semantic inventory artifact |
| `src/Precept/Pipeline/ParsedConstruct.cs` | `ParsedConstruct` — generic input node type with `ConstructMeta` and `SlotValue[]` |
| `src/Precept/Pipeline/SlotValue.cs` | 17 `SlotValue` subtypes — the typed slot discriminated union |
| `src/Precept/Language/Constructs.cs` | `ConstructMeta`, `ConstructKind` — construct layouts and dispatch axis |
| `src/Precept/Language/Operations.cs` | `FindCandidates()`, `FindUnary()`, `BinaryIndex` — operator lookup |
| `src/Precept/Language/Functions.cs` | `ByName` frozen dictionary — function overload resolution |
| `src/Precept/Language/Types.cs` | `GetMeta()` → `TypeMeta` with `.Accessors`, `.WidensTo` |
| `src/Precept/Language/Modifiers.cs` | Modifier applicability, mutual exclusivity, subsumption metadata |
