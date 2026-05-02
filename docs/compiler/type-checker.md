# Type Checker

## Status

| Property | Value |
|---|---|
| Doc maturity | Full design spec |
| Implementation state | Pre-implementation (stub with `[HandlesCatalogMember]` annotations) |
| Source | `src/Precept/Pipeline/TypeChecker.cs`, `src/Precept/Pipeline/SemanticIndex.cs` |
| Upstream | SyntaxTree (from Parser), catalog metadata |
| Downstream | GraphAnalyzer, ProofEngine, Precept Builder, LS semantic features |

---

## Overview

The type checker is a **metadata resolution engine with structural scaffolding** — not a structural validator with metadata lookups. Traditional type checkers "know" the type system and implement it; Precept's catalogs know the type system and the checker merely applies it. ~70–75% of the checker's work is asking catalogs questions and recording answers. The remaining ~25–30% is structural: symbol tables, scope management, cycle detection, choice-set logic.

This reframing has a profound implication: **new language features (operations, functions, types, modifiers, actions) require zero type-checker code changes.** Add the catalog entry → the generic resolution engine handles it automatically. Only genuinely new structural patterns (a new scope rule, a new validation shape) require checker changes.

The type checker transforms `SyntaxTree` into `SemanticIndex` — a flat semantic inventory of resolved symbols, typed expressions, normalized declarations, and dependency facts. The inventory is organized by semantic role, not by source position, because downstream consumers need declarations indexed by role, not by parser nesting.

---

## Responsibilities and Boundaries

**OWNS:** name resolution, type resolution, expression typing, overload selection, modifier combination legality, action shape classification, semantic identity stamping, qualifier disambiguation, error-type propagation, SemanticIndex production.

**Does NOT OWN:** source structure (Parser), graph topology (GraphAnalyzer), proof obligation discharge (ProofEngine), execution planning (Precept Builder), qualifier runtime identity (Evaluator).

---

## Architecture: 2-Pass / 3-Sub-Pass Design

### Pass 1: Registration (Symbol Table Construction)

**Input:** `SyntaxTree.Declarations`
**Output:** Mutable symbol tables (field, state, event) in `CheckContext`

No expression checking. No diagnostics beyond duplicates and structural errors.

```
for each declaration in syntaxTree.Declarations:
    match declaration:
        FieldDeclarationNode → register field name + resolve TypeRef → TypeKind
        StateDeclarationNode → register state name + resolve modifiers
        EventDeclarationNode → register event name + resolve arg types
        (all others) → skip (processed in Pass 2)
```

**TypeRef resolution:** Query `Types.ByTokenKind` for the keyword token → get `TypeMeta` → stamp `TypeKind`. For collections, extract element type. For choice, extract the choice definition. Pure catalog lookup — no expression resolution needed.

**Initial state / terminal / required validation** fires here (counting state modifiers).

### Pass 2: Checking (Expression Resolution + Normalization + Structural Validation)

**Input:** Symbol tables (from Pass 1) + `SyntaxTree.Declarations`
**Output:** `SemanticIndex`

Pass 2 has three generic sub-passes:

#### Sub-pass 2a: Expression Resolution Engine

The core of the checker. A single recursive function (~250–350 lines) that resolves any `Expression` node to a `TypedExpression`:

```
TypedExpression Resolve(Expression expr, TypeKind? expectedType):
    match expr:
        LiteralExpression                       → resolve via context (expectedType)
        IdentifierExpression                    → symbol table lookup → TypedFieldRef or TypedArgRef
        BinaryExpression                        → resolve left/right, FindCandidates(op, L, R), disambiguate
        UnaryExpression                         → resolve operand, FindUnary(op, type)
        CallExpression                          → resolve args, Functions.ByName, overload match
        MemberAccessExpression                  → resolve object, TypeMeta.Accessors lookup
        MethodCallExpression                    → resolve receiver, TypeMeta accessor dispatch
        ConditionalExpression                   → resolve condition (boolean), unify branch types
        QuantifierExpression                    → resolve collection, push binding, resolve predicate (boolean)
        ParenthesizedExpression                 → resolve inner
        ListLiteralExpression                   → resolve elements, check element type
        IsSetExpression                         → operand must be optional, result = boolean
        IsNotSetExpression                      → same
        CIFunctionCallExpression                → ~string enforcement + function lookup
        InterpolatedStringExpression            → resolve holes (must be scalar), result = string
        InterpolatedTypedConstantExpression     → same + context-typed result
        TypedConstantExpression                 → context type propagation + content validation
```

**Note:** `ExpressionFormKind` enum members (catalog names) differ from AST class names. The catalog classifies the *form*; the AST names the *syntax node*:

| ExpressionFormKind | AST Class |
|---|---|
| `Literal` | `LiteralExpression`, `TypedConstantExpression`, `InterpolatedStringExpression`, `InterpolatedTypedConstantExpression` |
| `Identifier` | `IdentifierExpression` |
| `Grouped` | `ParenthesizedExpression` |
| `BinaryOperation` | `BinaryExpression` |
| `UnaryOperation` | `UnaryExpression` |
| `MemberAccess` | `MemberAccessExpression` |
| `Conditional` | `ConditionalExpression` |
| `FunctionCall` | `CallExpression` |
| `MethodCall` | `MethodCallExpression` |
| `ListLiteral` | `ListLiteralExpression` |
| `PostfixOperation` | `IsSetExpression` / `IsNotSetExpression` |
| `Quantifier` | `QuantifierExpression` |
| `CIFunctionCall` | `CIFunctionCallExpression` |

This function has no per-type-kind branching for operators or functions. It doesn't know what `+` means for money vs integers — it asks the Operations catalog. It doesn't know what `min` accepts — it asks the Functions catalog. It doesn't know what `.count` returns — it asks the Types catalog.

#### Sub-pass 2b: Declaration Normalization

Walks each declaration kind and resolves contained expressions via 2a, producing typed inventory entries:

- **TransitionRowNode** → resolve guard + actions + outcome → `TypedTransitionRow`
- **RuleDeclarationNode** → resolve condition + guard + message → `TypedRule`
- **StateEnsureNode / EventEnsureNode** → resolve condition + guard + message → `TypedEnsure`
- **AccessModeNode** → validate field/state names, resolve guard → `TypedAccessMode`
- **StateActionNode** → resolve guard + actions → `TypedStateHook`
- **EventHandlerNode** → resolve actions → `TypedEventHandler`
- **FieldDeclarationNode** (computed) → resolve computed expression → populate `TypedField.ComputedExpression`

Each case is 5–10 lines: resolve the expressions this construct contains, validate structural constraints (guards must be boolean, messages must be string), produce a typed entry.

#### Sub-pass 2c: Structural Validation

After all expressions are resolved:

- **Computed field cycle detection** — build dependency graph from `ComputedExpression` references, DFS for cycles
- **Choice validation** — validate choice value sets, subset relationships, ordering constraints
- **Forward-reference prohibition** — default expressions may only reference fields declared before the current field
- **Stateless/stateful cross-validation** — EventHandlerNode + states conflict
- **Initial event field assignment completeness** — if initial event exists, verify required fields are assigned

---

## SemanticIndex Shape

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
    FieldDeclarationNode Syntax       // back-pointer
);

public sealed record TypedState(
    string Name,
    ImmutableArray<ModifierKind> Modifiers,  // initial, terminal, required, irreversible, etc.
    StateDeclarationNode Syntax
);

public sealed record TypedEvent(
    string Name,
    ImmutableArray<TypedArg> Args,
    bool IsInitial,
    EventDeclarationNode Syntax
);

public sealed record TypedArg(
    string Name,
    string EventName,
    TypeKind ResolvedType,
    TypeKind? ElementType,
    ImmutableArray<ModifierKind> Modifiers,
    TypedExpression? DefaultExpression,
    bool IsOptional,
    ArgumentNode Syntax
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
    QualifierBinding? ResultQualifier,
    TransitionRowNode Syntax
);

public enum TransitionOutcome { Transition, NoTransition, Reject }

public sealed record TypedRule(
    TypedExpression Condition,
    TypedExpression? Guard,
    TypedExpression Message,
    ImmutableArray<string> SemanticSubjects,  // field names referenced
    RuleDeclarationNode Syntax
);

public sealed record TypedEnsure(
    ConstraintKind Kind,       // Invariant, StateResident, StateEntry, StateExit, EventPrecondition
    string? AnchorState,       // for state-anchored
    string? AnchorEvent,       // for event-anchored
    TypedExpression Condition,
    TypedExpression? Guard,
    TypedExpression Message,
    ImmutableArray<string> SemanticSubjects,
    SyntaxNode Syntax          // StateEnsureNode or EventEnsureNode
);

public sealed record TypedAccessMode(
    string StateName,
    string FieldName,
    ModifierKind Mode,         // Write, Read, or Omit
    TypedExpression? Guard,
    AccessModeNode Syntax
);

public sealed record TypedStateHook(
    AnchorScope Scope,         // OnEntry or OnExit
    string StateName,
    TypedExpression? Guard,
    ImmutableArray<TypedAction> Actions,
    StateActionNode Syntax
);

public sealed record TypedEventHandler(
    string EventName,
    ImmutableArray<TypedAction> Actions,
    EventHandlerNode Syntax
);

/// Placeholder for stateless-precept edit declarations (edit all / edit Field1, Field2).
public sealed record TypedEditDeclaration(
    ImmutableArray<string> EditableFields,  // empty = "all"
    bool IsEditAll,
    SyntaxNode Syntax
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
    Statement Syntax
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
    Statement Syntax
) : TypedAction(Kind, FieldName, FieldType, ProofRequirements, Syntax);

/// Binding action — carries target binding (dequeue into, pop into).
public sealed record TypedBindingAction(
    ActionKind Kind,
    string FieldName,
    TypeKind FieldType,
    string? Binding,           // "into" target field name, null if no into
    ImmutableArray<ProofRequirement> ProofRequirements,
    Statement Syntax
) : TypedAction(Kind, FieldName, FieldType, ProofRequirements, Syntax);

public enum ActionSecondaryRole
{
    Index,     // insert ... at <index>
    Key,       // put ... key <key>, appendBy <key>, enqueueBy <key>
}
```

**`ActionSecondaryRole` rationale:** A single nullable `SecondaryExpression` without a discriminator forces the Evaluator to back-reference `ActionKind` to determine dispatch — defeating the DU's purpose. The enum carries role semantics; the Evaluator switches on it. Invariant: `SecondaryRole.HasValue == (SecondaryExpression != null)`, enforced at construction time. Start with `Index` and `Key` only; `Priority` can be added if the Evaluator genuinely distinguishes it from `Key`.

#### Typed Expressions (DU)

```csharp
public abstract record TypedExpression(
    TypeKind ResultType,
    Expression Syntax
);

public sealed record TypedFieldRef(
    TypeKind ResultType,
    string FieldName,
    bool IsCaseInsensitive,    // carries ~string flag
    Expression Syntax
) : TypedExpression(ResultType, Syntax);

public sealed record TypedArgRef(
    TypeKind ResultType,
    string EventName,
    string ArgName,
    Expression Syntax
) : TypedExpression(ResultType, Syntax);

public sealed record TypedLiteral(
    TypeKind ResultType,
    object? Value,             // parsed literal value
    Expression Syntax
) : TypedExpression(ResultType, Syntax);

public sealed record TypedBinaryOp(
    TypeKind ResultType,
    OperationKind ResolvedOp,
    TypedExpression Left,
    TypedExpression Right,
    QualifierBinding? ResultQualifier,
    ImmutableArray<ProofRequirement> ProofRequirements,
    Expression Syntax
) : TypedExpression(ResultType, Syntax);

public sealed record TypedUnaryOp(
    TypeKind ResultType,
    OperationKind ResolvedOp,
    TypedExpression Operand,
    Expression Syntax
) : TypedExpression(ResultType, Syntax);

public sealed record TypedFunctionCall(
    TypeKind ResultType,
    FunctionKind ResolvedFunction,
    ImmutableArray<TypedExpression> Arguments,
    ImmutableArray<ProofRequirement> ProofRequirements,
    Expression Syntax
) : TypedExpression(ResultType, Syntax);

public sealed record TypedMemberAccess(
    TypeKind ResultType,
    TypedExpression Object,
    TypeAccessor ResolvedAccessor,
    ImmutableArray<ProofRequirement> ProofRequirements,
    Expression Syntax
) : TypedExpression(ResultType, Syntax);

public sealed record TypedConditional(
    TypeKind ResultType,
    TypedExpression Condition,
    TypedExpression ThenBranch,
    TypedExpression ElseBranch,
    Expression Syntax
) : TypedExpression(ResultType, Syntax);

public sealed record TypedQuantifier(
    TypeKind ResultType,       // always Boolean
    string BindingName,
    TypeKind BindingType,
    TypedExpression Collection,
    TypedExpression Predicate,
    Expression Syntax
) : TypedExpression(ResultType, Syntax);

/// Error expression — propagates ErrorType, replaces failed sub-expressions.
public sealed record TypedErrorExpression(
    Expression Syntax
) : TypedExpression(TypeKind.Error, Syntax);
```

#### Dependency Facts

```csharp
public sealed record ComputedFieldDep(
    string FieldName,
    ImmutableArray<string> DependsOn
);

public sealed record ConstraintFieldRefs(
    object ConstraintIdentity,  // TypedRule or TypedEnsure reference
    ImmutableArray<string> ReferencedFields,
    ImmutableArray<string> ReferencedArgs
);
```

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

    // Dependency facts
    ImmutableArray<ComputedFieldDep> ComputedDeps,
    ImmutableArray<ConstraintFieldRefs> ConstraintRefs,

    // Diagnostics
    ImmutableArray<Diagnostic> Diagnostics
);
```

---

## CheckContext: Mutable Working State

`CheckContext` is the internal mutable state used during the check pass. It is **not** part of the public `SemanticIndex` contract.

```csharp
internal sealed class CheckContext
{
    // Symbol tables (populated in Pass 1)
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

## Expression Resolution

### The Core Resolve Function

The `Resolve(Expression expr, TypeKind? expectedType)` function is the metadata interpreter core. The `expectedType` parameter enables top-down context propagation for numeric literal resolution and typed constants.

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

When resolving `MemberAccessExpression` or `MethodCallExpression`, the return type and parameter type depend on the accessor DU subtype:

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

For `MethodCallExpression` (accessor with parameters), validate the argument type against `paramType`. If `paramType` is null, the accessor is property-style — a call syntax `field.accessor()` emits a diagnostic.

### Identifier Resolution Priority

When resolving an `IdentifierExpression`, check scopes in this order:

1. **Quantifier bindings** (top of stack first — innermost binding wins)
2. **Event args** (`CurrentEventArgs` if set)
3. **Fields** (`FieldLookup`, gated by `CurrentScope` and `CurrentFieldIndex`)
4. **Error:** emit "UnresolvedIdentifier" diagnostic, return `TypedErrorExpression`

For step 3 with `CurrentScope == PriorFieldsOnly`: if the resolved field's index >= `CurrentFieldIndex`, emit "ForwardReferenceProhibited" diagnostic instead.

### Stub Strategy for Unimplemented Arms

Every expression node type that won't be implemented in its slice has an explicit stub arm returning `TypedErrorExpression` with a `NotYetImplemented` marker. No switch fallthrough, no crash. This is required from Slice 2 onward to prevent test failures when expressions contain forms not yet handled.

---

## Catalog Integration

### What the Checker Reads

| Catalog | What the checker uses | Section |
|---|---|---|
| **Types** | `WidensTo`, `Accessors`, `TypeCategory`, `ImpliedModifiers`, `ByTokenKind` | Widening, member access, literal classification |
| **Operations** | `FindCandidates`, `FindUnary`, `BinaryOperationMeta.QualifierMatch`, `.ProofRequirements` | Binary/unary expression resolution |
| **Functions** | `ByName`, `FunctionOverload.Parameters`, `.ProofRequirements`, `.HasCIVariant` | Function call resolution, CI enforcement |
| **Modifiers** | `ApplicableTo`, `MutuallyExclusiveWith`, `Subsumes` | Modifier validation |
| **Actions** | `ApplicableTo`, `AllowedIn`, `ValueRequired`, `ActionSyntaxShape` | Action resolution and classification |

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

## Catalog Gaps

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

Five stable rules, `FunctionMeta.HasCIVariant` already exists. The 5-rule enforcement surface is small enough that checker logic is acceptable without catalog metadata for the diagnostic dispatch.

---

## Key Design Decisions

| # | Decision | Original Proposal | Revised (Locked) | Rationale |
|---|---|---|---|---|
| 1 | Catalog lookup API | New `BinaryBySignature` / `UnaryBySignature` | Use existing `FindCandidates()` / `FindUnary()` | APIs already exist with correct semantics; no duplication |
| 2 | BinaryIndex disambiguation | Not addressed | ~15 lines qualifier-match logic after multi-candidate return | Qualifier identity is a runtime value the catalog can't know |
| 3 | SemanticIndex record placement | Slice 10 | Pre-Slice 0 commit | Can't write Slice 2 tests without type definitions to compile against |
| 4 | Field storage | `ImmutableDictionary<string, TypedField>` | `ImmutableArray<TypedField>` primary + `FrozenDictionary` secondary | Declaration order matters for scope and display; lookup needs O(1) |
| 5 | TypedInputAction secondary | Single nullable, no discriminator | Single nullable + `ActionSecondaryRole?` enum | Evaluator can't dispatch without knowing the role |
| 6 | HandlesCatalogMember stubs | Not addressed | Per-slice migration: remove from stub, add to real handler | PRECEPT0019 enforces single-coverage; duplicate annotations = CI failure |
| 7 | Resolve line count | ~100 lines | ~250–350 lines (16+ arms) | 7 missing AST nodes identified; each needs explicit handling |
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
| 18 | EventHandler scope | Not addressed | Has event arg scope (same `CurrentEventArgs` pattern as transition rows) | `EventHandlerNode.EventName` names the event; args naturally in scope |
| 19 | Forward-reference gate | Implicit | `FieldScopeMode` enum in CheckContext; check in identifier resolution | Generalizable scope restriction; fires at resolution time, not as separate validation |
| 20 | Identifier resolution priority | Not addressed | Quantifier bindings > event args > fields | Innermost scope wins; shadowing is predictable and standard |
| 21 | Function overload resolution | Not addressed | Arity filter → exact → widened → context retry for literals | Single deterministic algorithm; no ambiguity with current catalog |
| 22 | Slice 6 split | George suggestion (6a/6b) | Rejected — keep as single slice | IsSet/IsNotSet is 10 lines; splitting adds overhead with no parallelism gain |
| 23 | TypedTransitionRow.ResolvedArgs | Kramer R3 | Rejected — single dict lookup doesn't justify cached copies | Anti-mirroring: data already in `EventsByName[row.EventName].Args` |
| 24 | TypedEditDeclaration | Kramer R4 | Placeholder record in Pre-Slice 0; full implementation deferred | Correct eventual shape for stateless-precept edit support |
| 25 | ExpressionFormKind.Literal ownership | Not addressed | Migrates in Slice 2; Slices 3–4 add arms within handler | Single annotation unit; sub-form stubs live inside the real handler |
| 26 | ErrorGuaranteed debug assertion | LOW (research cross-reference) | In-scope for Slice 10: debug/test-time assertion validates any SemanticIndex containing a `TypedErrorExpression` also contains ≥1 Error-severity Diagnostic | Zero production cost; catches orphaned error expressions where we produce `TypedErrorExpression` without emitting the corresponding diagnostic |

---

## Error Recovery

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

## HandlesCatalogMember Migration

The current `TypeChecker.cs` (lines 18–31) has 13 `[HandlesCatalogMember]` annotations on a dead-letter `CheckExpression()` stub. PRECEPT0019 enforces that each `ExpressionFormKind` member is covered by exactly one handler method.

**Per-slice migration protocol:**

1. Remove the relevant `[HandlesCatalogMember(ExpressionFormKind.X)]` from the stub method
2. Add it to the real handler method implementing that form
3. Verify PRECEPT0019 doesn't fire (no duplicate-coverage error)

When the last annotation leaves the stub, delete `CheckExpression()` entirely.

This is mechanical but mandatory per-slice to prevent CI failures.

---

## Vertical Slice Plan

### Pre-Slice 0: Shape Commit (unblocks everything)

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

### Slice 1: Symbol Tables (Pass 1)

- Field/state/event/arg registration into `CheckContext`
- Duplicate-name detection (emit diagnostic, retain first)
- Initial/terminal state counting and validation
- `[HandlesCatalogMember]` migration: none needed (registration doesn't cover expression forms)

### Slice 2: Scalar Expression Resolution — Binary & Unary Ops

- `Resolve()` function with arms: `LiteralExpression`, `IdentifierExpression`, `BinaryExpression`, `UnaryExpression`, `ParenthesizedExpression`
- `Operations.FindCandidates()` + qualifier disambiguation logic + widening fallback
- `Operations.FindUnary()` integration
- ErrorType propagation
- `FieldScopeMode` check in identifier resolution (forward-reference gate)
- Stub arms for all other expression forms → `TypedErrorExpression`
- **Stub migration:** Remove `Literal`, `Identifier`, `BinaryOperation`, `UnaryOperation`, `Grouped` from `CheckExpression` stub. Note: `Literal` ownership moves here; Slices 3–4 add arms *within* this handler for interpolated strings and typed constants.

### Slice 3: Functions, Accessors, Method Calls, Interpolated Strings

- `CallExpression` → `Functions.ByName` lookup + overload resolution (arity → exact → widened → context retry)
- `MemberAccessExpression` → field ref or TypeAccessor lookup + return-type resolution via accessor DU
- `MethodCallExpression` → resolve receiver, TypeMeta accessor dispatch, parameter-type validation
- `InterpolatedStringExpression` → hole resolution, scalar check, result = `TypeKind.String` (add arm within `Literal` handler — no separate stub migration)
- ProofRequirement recording from overload/accessor entries
- **Stub migration:** Remove `FunctionCall`, `MethodCall`, `MemberAccess` from stub. `InterpolatedString` is already under the `Literal` handler from Slice 2.

### Slice 4: Typed Constants + Context-Sensitive Resolution

- `TypedConstantExpression` → context type propagation + content validation (hardcoded dispatch if ContentValidation DU not yet landed). Add arm within `Literal` handler — no separate stub migration.
- `InterpolatedTypedConstantExpression` → same with interpolation holes (add arm within `Literal` handler)
- Numeric literal context retry mechanism (`expectedType` propagation into binary ops and function calls)
- **Out-of-range literal checking:** After resolving a numeric literal's type via `expectedType`, validate the literal's value against the type's representable range (`integer` → Int64 bounds, `decimal` → Decimal.MaxValue/MinValue, `number` → Double range with precision loss warning for integers > 2^53). ~10 lines of logic sourced from `TypeMeta` range metadata. Universal compiler practice per research validation.
- **Stub migration:** None — `TypedConstant` and `InterpolatedTypedConstant` are sub-forms of `Literal`, which migrated in Slice 2. These slices add arms within the already-migrated handler.

### Slice 5: Transition Row + EventHandler Normalization

- Guard expression resolution (boolean result required)
- Action chain resolution per `ActionSyntaxShape` → TypedAction DU
- `SecondaryExpression` + `SecondaryRole` assignment
- Transition target validation (state name lookup in symbol table)
- Partial result policy: failed guard/action → `TypedErrorExpression`, row still emitted
- Scope: set `CurrentEventArgs` when entering transition row OR event handler (both name their event)
- EventHandler scope: `EventHandlerNode.EventName` → lookup event args → set `CurrentEventArgs` → resolve actions + post-condition guard → clear `CurrentEventArgs`

### Slice 6: Structural Validation

- `IsSetExpression` / `IsNotSetExpression` → operand must be optional field, result = boolean
- Computed field dependency graph + cycle detection (DFS)
- Choice field validation (values valid for type, no duplicates, subset/ordering)
- Forward-reference prohibition (default expressions reference only prior fields — belt-and-suspenders validation; the primary gate fires in identifier resolution via `FieldScopeMode`)
- **Stub migration:** Remove `PostfixOperation` from stub (covers both `IsSetExpression` and `IsNotSetExpression` AST nodes). If this is the last annotation, delete `CheckExpression()` stub entirely.

### Slice 7: Modifier Validation

- Per-field modifier applicability (`ModifierMeta.ApplicableTo`)
- Modifier conflicts (`MutuallyExclusiveWith`)
- Subsumption warnings (`Subsumes`)
- Bounds validation (min > max, negative counts)
- `writable` on computed/arg prevention
- State modifier validation (exactly one initial, at least one terminal)
- **Independent of expression resolution** — depends only on Slice 1

### Slice 8: CI Enforcement + CIFunctionCall

- `CIFunctionCallExpression` resolution → subject must be `~string`, lookup CI variant
- CI operator validation (case-insensitive comparison on `~string` operands)
- Diagnostic for CI function on non-`~string` operand
- **Stub migration:** Remove `CIFunctionCall` from stub

### Slice 9: Quantifiers + List Literals

- `QuantifierExpression` → push `(bindingName, bindingType)` onto `QuantifierBindings` stack, resolve predicate (must be boolean), resolve collection operand (must be collection type), pop binding on exit
- `ListLiteralExpression` → element type unification, result = inferred collection type
- Binding shadowing: quantifier bindings shadow event args and fields (resolution priority: bindings > args > fields)
- **Stub migration:** Remove `Quantifier`, `ListLiteral` from stub

### Slice 10: Final Assembly

- `CheckContext` → immutable `SemanticIndex` transformation
- Array-to-frozen-dict derivation for lookup indexes
- Dependency fact extraction (computed-field deps, constraint-field refs)
- ErrorGuaranteed debug assertion: validate that any SemanticIndex containing a `TypedErrorExpression` anywhere in its trees also contains ≥1 Error-severity Diagnostic (debug/test-time only — zero production cost) [D-26]
- Integration tests: full `.precept` files from `samples/` → complete `SemanticIndex` with all inventories populated
- Anti-mirroring rule enforcement test

### Dependency Graph

```
Pre-Slice 0 (Shape Commit)
   ↓
Slice 1 (Registration / Symbol Tables)
   ↓
Slice 2 (Expression Core: Binary + Unary)
   ↓
Slice 3 (Functions + Accessors + MethodCall + InterpolatedString)  ← depends on Slice 2
   ↓
Slice 4 (Typed Constants + Context Literals)  ← depends on Slice 2
   ↓
Slice 5 (Transition Rows + Actions)  ← depends on Slices 2–4
Slice 6 (Structural Validation)      ← depends on Slices 2–4
Slice 7 (Modifiers)                  ← depends on Slice 1 ONLY
   ↓
Slice 8 (CI Enforcement)  ← depends on Slices 2–3
   ↓
Slice 9 (Quantifiers + List Literals)  ← depends on Slices 2–3
   ↓
Slice 10 (Final Assembly + Integration)  ← depends on all
```

**Parallelism:** Slices 5, 6, and 7 can be developed in parallel after Slice 4. Slice 7 only depends on Slice 1. Slices 8 and 9 can proceed in parallel after Slice 3.

---

## Open Questions

1. **Anti-mirroring enforcement mechanism** — structural test that graph analysis, proof, and builder code paths do NOT call back-pointer `.Syntax` properties. How to enforce: Roslyn analyzer, runtime reflection test, or code review convention?

2. **ContentValidation DU landing timeline** — if the catalog shape change is significantly delayed, the hardcoded dispatch table in Slice 4 becomes long-lived debt. Track as a blocking dependency for post-Slice-4 cleanup.

3. **`~string` CI enforcement cataloging** — currently acceptable as 5-rule checker logic. If the CI surface grows beyond 5 rules, revisit whether `CIEnforcementDiagnostic?` belongs on `BinaryOperationMeta`.

4. **Precision Propagation Awareness for Decimal Arithmetic** — MEDIUM PRIORITY (future). Research finding: The exact decimal arithmetic survey documents critical precision behaviors: division can produce inexact results (silently rounded in .NET `Decimal`); multiplication accumulates scale (`scale_a + scale_b`), which can overflow the 96-bit mantissa; trailing zeros may be lost when mantissa reduction is needed; division by zero throws `OverflowException`, not a special value. The type checker currently resolves `decimal / decimal → decimal` and `decimal * decimal → decimal` without noting that these operations have different precision characteristics than addition. The ProofEngine handles overflow obligations, but the type checker has no mechanism to warn about precision-lossy operations. **Resolution:** This is a ProofEngine concern, not a type checker concern — and the design correctly places it there. The type checker spec explicitly states that precision propagation is NOT its responsibility (see § Deliberate Exclusions: "No proof obligation discharge"). If we ever add `ProofRequirement.PrecisionWarning` to the catalog metadata for division operations, the type checker would record it automatically through the existing `BinaryOperationMeta.ProofRequirements` mechanism. No design change needed — just document the boundary.

5. **CandidateTypes on TypedErrorExpression** — LOW PRIORITY (post-v1). Consider adding an optional `ImmutableArray<TypeKind>? CandidateTypes` to `TypedErrorExpression` in a future enhancement. When `FindCandidates` returns entries but none matches qualifier/widening constraints, recording what *would* have matched enables the language server to provide "did you mean?" suggestions. This is not needed for Slices 2–10 (the LS can fall back to context-based completions), but is a known future lane for richer error recovery and diagnostic UX.

---

## Deliberate Exclusions

- **No graph topology:** Reachability, dominance, and edge sets are the GraphAnalyzer's responsibility.
- **No proof obligation discharge:** ProofRequirement *recording* happens here (from catalog entries); ProofRequirement *discharge* is the ProofEngine's responsibility.
- **No runtime planning:** Descriptor production and execution plan compilation are the Precept Builder's responsibility.
- **No qualifier runtime identity:** The checker validates qualifier *compatibility* structurally; the Evaluator handles qualifier *values* at runtime.

---

## Research Validation

**Full analysis:** [`research/language/type-checker-research-validation.md`](../../research/language/type-checker-research-validation.md)

Validated 2026-05-02 against 16 compiler architecture surveys and 11 language theory references.

### Well-Grounded Decisions (8)

| Decision | Precedent |
|----------|-----------|
| 2-pass architecture (registration → checking) | Roslyn, TypeScript, Go, Kotlin K2 |
| ErrorType propagation + always-produce-partial-results | Roslyn, TypeScript, Rust, Kotlin K2 |
| Diagnostic accumulation without abandoning | Roslyn, TypeScript, Go, Dafny |
| Context-sensitive literal typing via `expectedType` | Kotlin, Rust, Swift, GHC |
| Immutable output artifact (SemanticIndex) | Roslyn, TypeScript, Rust |
| Proof requirement recording separated from discharge | SPARK Ada, Dafny, Liquid Haskell |
| Widening/subtyping via `TypeMeta.WidensTo` | C# platform alignment, Kotlin literal subtypes |
| Array-primary + FrozenDictionary secondary | Roslyn, Go, TypeScript dual-representation |

### Justified Divergences (5)

| Divergence | Rationale |
|------------|-----------|
| Flat semantic inventory (no per-tree SemanticModel) | Single-file DSL — lazy resolution adds complexity without benefit at this scale |
| No query system / no on-demand resolution | <500 declarations — query overhead exceeds computation cost |
| Catalog-driven checker (~70% catalog / ~30% structural) | Core architectural identity — closed type system fully described by metadata |
| Qualifier disambiguation as ~15 lines | Qualifiers are runtime values, not type-level parameters — full unit algebra inappropriate |
| No numeric literal defaulting | Every expression resolves within a typed declaration context — no untyped positions exist |

### Research Gaps Incorporated

| Priority | Gap | Action |
|----------|-----|--------|
| **HIGH** | Out-of-range literal checking | Added to Slice 4 — validate numeric literal value against resolved type's representable range |
| Lower | Precision propagation for decimal arithmetic | Confirmed as ProofEngine concern — no type checker change needed |
| Lower | Temporal type cross-validation | Catalog entries handle temporal safety generically — cross-reference note only |
| Lower | Structured error recovery (CandidateTypes on TypedErrorExpression) | Post-v1 LS enhancement |
| **IN-SCOPE** | ErrorGuaranteed pattern (debug assertion) | Promoted to Slice 10 — debug/test-time invariant validation, zero production cost (D-26) |

---

## Cross-References

| Topic | Document |
|---|---|
| SemanticIndex governance, anti-mirroring rules, inventory shape | `docs/compiler-and-runtime-design.md §6` |
| All catalogs the type checker consumes | `docs/language/catalog-system.md` |
| SyntaxTree input contract | `docs/compiler/parser.md` |
| SemanticIndex consumer (graph) | `docs/compiler/graph-analyzer.md` |
| Design discussion record (original analysis) | `docs/working/type-checker-design-analysis.md` |
| Design discussion record (implementer review) | `docs/working/george-type-checker-review.md` |
| Design discussion record (response) | `docs/working/frank-response-to-george-review.md` |
| Design discussion record (canonical review) | `docs/working/george-canonical-design-review.md` |
| Design discussion record (canonical response) | `docs/working/frank-response-to-george-canonical-review.md` |

---

## Source Files

| File | Purpose |
|---|---|
| `src/Precept/Pipeline/TypeChecker.cs` | Type checker implementation — `TypeChecker` static class with `Check(SyntaxTree)` entry point |
| `src/Precept/Pipeline/SemanticIndex.cs` | `SemanticIndex` — flat semantic inventory artifact |
| `src/Precept/Language/Operations.cs` | `FindCandidates()` at line 1153, `FindUnary()` at line 1145, `BinaryIndex` at line 1111 |
| `src/Precept/Language/Functions.cs` | `ByName` frozen dictionary at line 298 for function overload resolution |
| `src/Precept/Language/Types.cs` | `GetMeta()` → `TypeMeta` with `.Accessors`, `.WidensTo` |
| `src/Precept/Language/Modifiers.cs` | Modifier applicability, mutual exclusivity, subsumption metadata |
| `src/Precept/Language/ExpressionForms.cs` | `ExpressionFormKind` enum — form classification axis for `[HandlesCatalogMember]` |
