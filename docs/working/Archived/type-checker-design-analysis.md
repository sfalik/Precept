# Type Checker Design Analysis

**By:** Frank (Lead/Architect)
**Date:** 2025-07-14T12:00:00Z
**Status:** Design proposal for owner review

---

## 1. Catalog Coverage Audit

This section maps every type-checking responsibility from spec §3 to catalog metadata, classifying each check as either a **pure catalog lookup** (the checker is just machinery) or **genuine checker logic** (structural validation that can't be metadata-driven).

### §3.2 Type Widening Rules

| Check | Catalog Source | Lookup vs Logic | What the checker does |
|-------|---------------|-----------------|----------------------|
| Implicit widening acceptance | `TypeMeta.WidensTo` | **Pure lookup** | Query `Types.GetMeta(sourceType).WidensTo.Contains(targetType)` |
| Widening context applicability | `TypeMeta.WidensTo` | **Pure lookup** | Same check applies uniformly in assignment, binary ops, function args, defaults, comparisons |
| Decimal→Number rejection | `TypeMeta.WidensTo` (absence) | **Pure lookup** | `Types.GetMeta(TypeKind.Decimal).WidensTo` does NOT contain `Number` → emit `TypeMismatch` |
| Common numeric type resolution | `Operations` catalog (BinaryOperationMeta) | **Pure lookup** | The operation entry already declares the result type — no separate widening logic needed |

**Insight:** Widening isn't a separate phase; it's embedded in operation/function resolution. When the checker looks up `(integer, +, decimal)` in Operations, the catalog already says "result = decimal." The WidensTo field serves as the validation authority for *assignment* contexts and *function arg matching*, not for expression typing (which the Operations catalog handles directly).

### §3.3 Context-Sensitive Type Resolution (Numeric Literals, Typed Constants)

| Check | Catalog Source | Lookup vs Logic | What the checker does |
|-------|---------------|-----------------|----------------------|
| Numeric literal resolution | **Types catalog** (TypeCategory) | **Checker logic** | Top-down context propagation — determine expected type from parent (assignment target, binary peer, function param position), resolve literal form against it |
| Typed constant resolution | **Types catalog** (TypeCategory) | **Checker logic** | Same context propagation + content validation against the context-determined type |
| No-context diagnostic | `Diagnostics` catalog | **Pure lookup** (emit) | If propagated context is absent → emit diagnostic |
| Content validation patterns | **GAP — not in any catalog** | **Checker logic** | Per-type validation rules (date = YYYY-MM-DD, money = `<number> <currency>`, etc.) |

**Gap identified:** Typed constant content validation patterns (the table in §3.3) have no catalog representation. The checker would need to hardcode per-TypeKind validation patterns — violating the catalog-first principle. See §2 Gap Analysis.

### §3.4 Name Resolution

| Check | Catalog Source | Lookup vs Logic | What the checker does |
|-------|---------------|-----------------|----------------------|
| Duplicate field/state/event/arg names | N/A (symbol table) | **Checker logic** | Hash-set collision during registration pass |
| Undeclared field reference | N/A (symbol table) | **Checker logic** | Identifier not in field symbol table (or arg scope) |
| Undeclared state reference | N/A (symbol table) | **Checker logic** | State name not in state symbol table |
| Undeclared event reference | N/A (symbol table) | **Checker logic** | Event name not in event symbol table |
| Multiple/no initial states | `Modifiers` catalog (StateModifierMeta) | **Mixed** | Count states with `InitialState` modifier — modifier identity from catalog, counting is logic |

**Assessment:** Name resolution is inherently structural logic (building and querying symbol tables). Catalogs don't help here — this is the ~20% that's genuine checker work.

### §3.5 Scope Rules

| Check | Catalog Source | Lookup vs Logic | What the checker does |
|-------|---------------|-----------------|----------------------|
| Global scope (fields/states/events visible everywhere) | N/A | **Checker logic** | Registration pass populates global scope — trivial |
| Expression scope (what's visible where) | **GAP — partially missing** | **Checker logic** | Scope determination per context (rule body, transition guard, computed expr, etc.) |
| Quantifier binding scope | N/A | **Checker logic** | Push/pop binding variable for quantifier predicate |
| Event arg access (EventName.ArgName) | N/A (symbol table) | **Checker logic** | Resolve MemberAccess on event identifier to arg symbol |
| Default expr forward-reference prohibition | N/A | **Checker logic** | Only field names before current field are in scope for defaults |
| Binding shadows field | `Diagnostics` | **Checker logic** (emit) | Name collision between binding var and field name |

**Assessment:** Scope rules are inherently structural. The scope model is small and fixed — no catalog can meaningfully drive it because scope is about *where* in the program you are, not *what kind* of thing you're checking.

### §3.6 Expression Typing Rules

| Check | Catalog Source | Lookup vs Logic | What the checker does |
|-------|---------------|-----------------|----------------------|
| Binary operator resolution | `Operations` catalog (`BinaryOperationMeta`) | **Pure lookup** | Query `Operations.ByOperatorAndTypes(op, lhsType, rhsType)` → get result TypeKind and OperationKind |
| Unary operator resolution | `Operations` catalog (`UnaryOperationMeta`) | **Pure lookup** | Query `Operations.ByOperatorAndType(op, operandType)` → result |
| `contains` resolution | `Operations` catalog | **Pure lookup** | Contains is an operation entry: `(collection-type, Contains, element-type) → boolean` |
| `is set` / `is not set` | `Operations` catalog | **Pure lookup** | `(optional-field, IsSet) → boolean`; type error if non-optional |
| Conditional type unification | `TypeMeta.WidensTo` + **checker logic** | **Mixed** | Find common type between branches — catalog-aware but logic-driven (LCA in widening graph) |
| Member access (`.`) | `TypeMeta.Accessors` | **Pure lookup** | Query `Types.GetMeta(objectType).Accessors.Find(memberName)` → result type |
| Function call resolution | `Functions` catalog (`FunctionOverload`) | **Pure lookup** | Query `Functions.FindByName(name)` → match overload by arg types |
| Parenthesized expression | N/A | **Trivial** | Result type = inner expression type |
| `~string` enforcement | **Operations/Functions** + `TypeMeta` | **Mixed** | CI flag propagates through expression typing; enforcement fires at comparison/call sites based on operand carrying `~string` |
| Quantifier predicate type | N/A | **Checker logic** | Predicate expression must resolve to boolean |
| String interpolation | `TypeMeta.Category` | **Pure lookup** | Each `{expr}` must resolve to a scalar (category != Collection) |
| QualifierMatch enforcement | `BinaryOperationMeta.Match` / `FunctionOverload.Match` | **Pure lookup** | When `Match = Same`, verify operand qualifiers are equal |
| ProofRequirement recording | `BinaryOperationMeta.ProofRequirements` / `TypeAccessor.ProofRequirements` / `FunctionOverload.ProofRequirements` | **Pure lookup** | Record proof obligations from catalog entries into SemanticIndex |

**Key insight:** Expression typing is ~85% pure catalog lookup. The Operations catalog with ~200+ entries encodes the entire type algebra. The checker's expression resolution is: "given (operator, operand types), look up the catalog entry → if found, use its result type and OperationKind; if not found, emit TypeMismatch." Functions work the same way via overload matching. Accessors work via TypeMeta.Accessors. This is a metadata interpreter, not a type checker.

### §3.7 Built-in Functions

| Check | Catalog Source | Lookup vs Logic | What the checker does |
|-------|---------------|-----------------|----------------------|
| Function name validation | `Functions.ByName` | **Pure lookup** | Name not in ByName → `UndeclaredFunction` |
| Arity validation | `FunctionMeta.Overloads[].Parameters.Count` | **Pure lookup** | No overload matches arg count → `FunctionArityMismatch` |
| Arg type matching | `FunctionOverload.Parameters[].Kind` | **Pure lookup** | Walk parameters, check assignability (with widening) |
| Overload selection | `FunctionMeta.Overloads` | **Pure lookup** (algorithm) | Find best-matching overload by arg types — small-scale overload resolution |
| CI variant enforcement | `FunctionMeta.HasCIVariant` | **Pure lookup** | If first arg is `~string` and `HasCIVariant` is true → emit enforcement diagnostic |

**Assessment:** 100% catalog-driven. The checker is a generic overload resolution algorithm parameterized by catalog data.

### §3.8 Semantic Checks (Modifier Validation)

| Check | Catalog Source | Lookup vs Logic | What the checker does |
|-------|---------------|-----------------|----------------------|
| Modifier type applicability | `FieldModifierMeta.ApplicableTo` | **Pure lookup** | Field's TypeKind in ApplicableTo array? |
| Mutual exclusivity | `ModifierMeta.MutuallyExclusiveWith` | **Pure lookup** | Any modifier in set also present on same field? |
| Subsumption (redundancy) | `FieldModifierMeta.Subsumes` | **Pure lookup** | Any subsumed modifier also present? → warning |
| min > max bounds check | N/A | **Checker logic** | Compare literal values from modifier value expressions |
| Negative count/length/places | N/A | **Checker logic** | Validate modifier value is non-negative |
| `writable` on computed | N/A | **Checker logic** | Field has computed expression + writable → error |
| `writable` on event arg | N/A | **Checker logic** | Arg carries writable → error |
| Duplicate modifier | N/A | **Checker logic** | Same ModifierKind appears twice on one field |

**Assessment:** Applicability checking (~60% of modifier work) is pure catalog lookup. Bounds validation and structural constraints are genuine checker logic — but they're small and uniform.

### §3.9 Action Statement Validation

| Check | Catalog Source | Lookup vs Logic | What the checker does |
|-------|---------------|-----------------|----------------------|
| Action-field type compatibility | `ActionMeta.ApplicableTo` | **Pure lookup** | Field's TypeKind in ApplicableTo? |
| Value expression type | `ActionMeta.ValueRequired` + element type | **Mixed** | If ValueRequired, resolve value expr, check assignability to collection element / field type |
| `into` target validation | `ActionMeta.IntoSupported` | **Pure lookup** | Action supports `into`? Target field type compatible? |
| ProofRequirement recording | `ActionMeta.ProofRequirements` | **Pure lookup** | Record proof obligations |
| `AllowedIn` context check | `ActionMeta.AllowedIn` | **Pure lookup** | Current construct kind in AllowedIn array? |
| Computed field write prevention | N/A | **Checker logic** | `set` targets a computed field → error |

**Assessment:** ~70% catalog-driven. ActionMeta.ApplicableTo handles the majority. Value type checking requires expression resolution but uses the standard generic path.

### §3.10 Access Mode Validation

| Check | Catalog Source | Lookup vs Logic | What the checker does |
|-------|---------------|-----------------|----------------------|
| Field/state existence | N/A (symbol table) | **Checker logic** | Name lookup |
| Computed field in editable mode | N/A | **Checker logic** | Computed + editable = error |
| Conflicting access modes | `AccessModifierMeta.MutuallyExclusiveWith` | **Pure lookup** | Same field has mutually exclusive modes |
| Redundant access modes | N/A | **Checker logic** | Field baseline writable/readonly vs declared mode |

### §3.11 Computed Field Validation

| Check | Catalog Source | Lookup vs Logic | What the checker does |
|-------|---------------|-----------------|----------------------|
| Self-reference cycle | N/A | **Checker logic** | Build dependency graph, detect cycles (DFS) |
| Expression type mismatch | Generic expression resolution | **Mixed** | Standard assignability check |
| Computed with default | N/A | **Checker logic** | Both `->` and `default` present |

### §3.12 Choice Type Validation

| Check | Catalog Source | Lookup vs Logic | What the checker does |
|-------|---------------|-----------------|----------------------|
| Missing element type | N/A | **Checker logic** (parser should catch) | Structural validation |
| Empty choice | N/A | **Checker logic** | No values declared |
| Duplicate choice value | N/A | **Checker logic** | Hash-set collision in declared values |
| Choice literal validation | N/A | **Checker logic** | Assigned literal in declared set? |
| Choice arg subset validation | N/A | **Checker logic** | Event arg choice values ⊆ field choice values |
| Rank conflict (ordered) | N/A | **Checker logic** | Order-preservation check between arg and field values |

**Assessment:** Choice validation is genuine structural logic. No catalog encodes choice-value sets — they're user-declared per-field. This is inherently checker logic.

### §3.13 Transition Outcome Validation

| Check | Catalog Source | Lookup vs Logic | What the checker does |
|-------|---------------|-----------------|----------------------|
| Undeclared target state | N/A (symbol table) | **Checker logic** | Name lookup |
| Reject message type | Generic expression resolution | **Mixed** | Standard "must be string" check |

### §3.14 Error Recovery

| Check | Catalog Source | Lookup vs Logic | What the checker does |
|-------|---------------|-----------------|----------------------|
| ErrorType propagation | N/A | **Checker logic** | Any op with ErrorType → ErrorType; suppress downstream diagnostics |
| IsMissing node handling | N/A | **Checker logic** | Skip/assign ErrorType per node category |
| One diagnostic per root cause | N/A | **Checker logic** | Suppress when ErrorType is flowing |

---

### Coverage Summary

| Category | % Catalog-Driven | % Checker Logic |
|----------|-----------------|-----------------|
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
| **Overall weighted estimate** | **~70-75%** | **~25-30%** |

The type checker is ~70-75% metadata interpreter and ~25-30% structural logic. The structural logic clusters in name resolution (symbol tables), scope management, dependency graph cycles, and choice-specific validation. Expression typing — the heart of what people think of as "type checking" — is almost entirely catalog-driven.

---

## 2. Catalog Gap Analysis

### Gap 1: Typed Constant Content Validation Patterns

**Belongs in:** `Types` catalog (`TypeMeta`)

**Metadata shape:**
```csharp
// New field on TypeMeta
ContentValidation? ContentValidation = null

// Shape:
public sealed record ContentValidation(
    string Pattern,           // Regex or structural pattern
    string[] Examples,        // Valid examples for diagnostics
    string FormatDescription  // Human-readable description, e.g. "YYYY-MM-DD"
);
```

**Needed by:** Context-sensitive resolution (§3.3) — validating typed constant content against expected type.

**Why it can't be hardcoded:** Without this, the checker would need `typeKind switch { Date => validateDate(), Money => validateMoney(), ... }` — a per-member switch encoding domain knowledge. With this metadata, the checker generically validates content against the pattern declared in the catalog.

**Types that need it:** `Date`, `Time`, `Instant`, `DateTime`, `ZonedDateTime`, `Timezone`, `Duration`, `Period`, `Money`, `Quantity`, `Price`, `ExchangeRate`, `Currency`, `UnitOfMeasure`, `Dimension`.

**Note:** The actual *parsing* of typed constant content (NodaTime, ISO 4217, etc.) is a runtime concern. The catalog declares the *format pattern* and *validation rule identity*; the checker dispatches to a format validator identified by catalog metadata rather than a per-type switch.

### Gap 2: Scope Visibility Rules (per Construct/Context)

**Belongs in:** `Constructs` catalog (`ConstructMeta`) or new metadata

**Metadata shape:**
```csharp
// New field on ConstructMeta (or a dedicated record)
public sealed record ScopeRule(
    bool FieldsInScope,
    bool EventArgsInScope,
    bool DefaultFieldsOnlyPrior,  // for default value expressions
    bool QuantifierBindingIntroduced
);
```

**Needed by:** Scope rules (§3.5) — the checker currently needs per-construct knowledge of what's in scope.

**Why it can't be hardcoded:** Without this, the checker needs `constructKind switch { TransitionRow => fieldsAndArgs, Rule => fieldsOnly, ComputedExpr => priorFieldsOnly, ... }`. The scope rules per construct ARE language knowledge.

**Assessment:** This is a **borderline** gap. Scope rules are so few (7 contexts from §3.5) and so structurally interleaved with symbol-table mechanics that metadata-driving them may over-engineer. **Recommendation:** This one stays as checker logic — the scope model is fundamentally structural and tiny. It's the kind of "20% genuine logic" that belongs in the checker, not in metadata. The scope model won't grow (no nested scopes, no imports, no modules by design).

### Gap 3: Action Typed-Shape Classification

**Belongs in:** `Actions` catalog (`ActionMeta`)

**Metadata shape:**
```csharp
// New field on ActionMeta
TypedActionShape TypedShape  // = Base | Input | Binding
```

Where:
```csharp
public enum TypedActionShape
{
    Base,     // TypedAction — no operand (clear)
    Input,    // TypedInputAction — carries expression value (set, add, remove, enqueue, push, append, insert, put)
    Binding,  // TypedBindingAction — carries binding target (dequeue, pop)
}
```

**Needed by:** Action resolution — the checker must produce one of three TypedAction shapes. Currently, the classification is implicit in `ActionSyntaxShape` (FieldOnly→Base, CollectionValue/AssignValue→Input, CollectionInto→Binding) but not declarative.

**Why it can't be hardcoded:** Without this, the checker would derive the typed shape from `ActionSyntaxShape` via a switch — which encodes the mapping between syntax shape and semantic shape in checker logic. With this field, the catalog explicitly declares which typed output shape each action resolves to.

**Note:** `ActionSyntaxShape` already correlates loosely, but the mapping isn't 1:1 (e.g., `InsertAt` and `PutKeyValue` are multi-expression Input shapes, not simple CollectionValue). Making this explicit prevents future misclassification bugs.

### Gap 4: `~string` CI Flag Propagation Metadata

**Belongs in:** `Types` catalog or `Operations` catalog

**What's needed:** The `~string` enforcement rules fire at specific operator/function call sites when an operand carries the CI qualifier. The checker needs to know:
1. Which operators trigger CI enforcement (currently `==`, `!=`)
2. Which functions trigger CI enforcement (currently `startsWith`, `endsWith`)
3. What diagnostic each site emits

**Current state:** The `Operations` catalog entries for `==`/`!=` on string types don't carry metadata about CI enforcement. The `Functions` catalog has `HasCIVariant` but doesn't explicitly declare *which diagnostic to emit*.

**Metadata shape:**
```csharp
// On BinaryOperationMeta — new optional field
DiagnosticCode? CIEnforcementDiagnostic = null
// e.g., EqualString entries get CaseInsensitiveFieldRequiresTildeEquals
// EqualCIString entries get null (they ARE the CI variant)

// On FunctionMeta — already has HasCIVariant; could add:
DiagnosticCode? CIEnforcementDiagnostic = null
```

**Assessment:** The checker CAN derive this today by checking `HasCIVariant` + operand type, but the mapping from "has CI variant" to "which diagnostic" would be a small switch. Making it explicit eliminates the switch. **Moderate priority** — the enforcement rules are small and stable (5 diagnostics total), so this gap is acceptable if we prefer to keep the checker's CI enforcement as a focused structural concern.

### Gap 5: Missing `pow` Integer-Exponent ProofRequirement

**Already identified as GAP-032.** The `pow(integer, integer)` overload in Functions.cs is missing its `ProofRequirement` for `exp >= 0`. This is a catalog data completeness bug, not a shape gap. Fix: add `NumericProofRequirement(PPowExp, GreaterThanOrEqual, 0m, ...)` to the Integer^Integer overload.

---

### Gap Summary

| # | Gap | Priority | Recommendation |
|---|-----|----------|----------------|
| 1 | TypedConstant content validation patterns | **High** | Add `ContentValidation?` to TypeMeta. Without it, the checker hardcodes per-type validation. |
| 2 | Scope visibility rules | **Skip** | Keep as checker logic. Scope model is tiny, structural, won't grow. |
| 3 | Action typed-shape classification | **Medium** | Add `TypedActionShape` to ActionMeta. Explicit > derived. |
| 4 | ~string CI enforcement diagnostics | **Low** | Acceptable as checker logic given the tiny surface (5 rules). Could catalog later. |
| 5 | pow ProofRequirement | **High** | Existing GAP-032 — pure data fix. |

---

## 3. SemanticIndex Shape Design

### Design Principles

1. **Flat by role, structured within** — top-level collections keyed by semantic identity, not source position.
2. **Dual access** — both keyed (dictionary) and ordered (array) access where consumers need both. Solve with `ImmutableDictionary` as primary + derived `ImmutableArray` views, or a custom `SemanticTable<TKey, TValue>` wrapper.
3. **Back-pointers as direct object references** — cheap, same-heap, full-recompile model.
4. **No nullable semantic identity** — every entry is either fully resolved or carries ErrorType marker.

### Concrete Record Types

#### Symbols

```csharp
/// Resolved field with full semantic identity.
public sealed record TypedField(
    string Name,
    TypeKind ResolvedType,
    TypeKind? ElementType,            // for collections: the inner type
    TypeKind? KeyType,                // for lookup/logBy/queueBy: the key type
    ImmutableArray<ModifierKind> Modifiers,
    ImmutableArray<ModifierKind> ImpliedModifiers,  // from TypeMeta.ImpliedModifiers
    TypedExpression? DefaultExpression,
    TypedExpression? ComputedExpression,
    QualifierBinding? Qualifier,      // resolved qualifier values (currency, unit, etc.)
    bool IsComputed,
    bool IsOptional,
    bool IsWritable,                  // baseline writable from modifier
    FieldDeclarationNode Syntax       // back-pointer
);

/// Resolved state.
public sealed record TypedState(
    string Name,
    ImmutableArray<ModifierKind> Modifiers,  // initial, terminal, required, irreversible, etc.
    StateDeclarationNode Syntax
);

/// Resolved event.
public sealed record TypedEvent(
    string Name,
    ImmutableArray<TypedArg> Args,
    bool IsInitial,
    EventDeclarationNode Syntax
);

/// Resolved event argument.
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

/// Resolved qualifier binding on a field.
public sealed record QualifierBinding(
    ImmutableArray<QualifierValue> Values
);

public sealed record QualifierValue(QualifierAxis Axis, string Value);
```

#### Normalized Declarations

```csharp
/// Resolved transition row with typed guard and action chain.
public sealed record TypedTransitionRow(
    string FromState,          // "any" encoded as a sentinel or null
    string EventName,
    string? TargetState,       // null for "no transition" / reject
    TypedExpression? Guard,
    ImmutableArray<TypedAction> Actions,
    TransitionOutcome Outcome, // Transition | NoTransition | Reject
    TransitionRowNode Syntax
);

public enum TransitionOutcome { Transition, NoTransition, Reject }

/// Resolved rule.
public sealed record TypedRule(
    TypedExpression Condition,
    TypedExpression? Guard,
    TypedExpression Message,
    ImmutableArray<string> SemanticSubjects,  // field names referenced
    RuleDeclarationNode Syntax
);

/// Resolved ensure (state-anchored or event-anchored).
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

/// Resolved access mode declaration.
public sealed record TypedAccessMode(
    string StateName,
    string FieldName,
    ModifierKind Mode,         // Write, Read, or Omit
    TypedExpression? Guard,
    AccessModeNode Syntax
);

/// Resolved state action (entry/exit hook with actions).
public sealed record TypedStateHook(
    AnchorScope Scope,         // OnEntry or OnExit
    string StateName,
    TypedExpression? Guard,
    ImmutableArray<TypedAction> Actions,
    StateActionNode Syntax
);

/// Resolved stateless event handler.
public sealed record TypedEventHandler(
    string EventName,
    ImmutableArray<TypedAction> Actions,
    EventHandlerNode Syntax
);
```

#### Typed Actions (3-shape DU)

```csharp
/// Base typed action — no operand (clear).
public record TypedAction(
    ActionKind Kind,
    string FieldName,
    TypeKind FieldType,
    ImmutableArray<ProofRequirement> ProofRequirements,
    Statement Syntax
);

/// Input action — carries resolved value expression (set, add, remove, enqueue, push, append, insert, put).
public sealed record TypedInputAction(
    ActionKind Kind,
    string FieldName,
    TypeKind FieldType,
    TypedExpression InputExpression,
    TypedExpression? SecondaryExpression,  // for insert (index), put (key), appendBy (key), enqueueBy (priority)
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
```

#### Typed Expressions

```csharp
/// Base typed expression — every expression node resolves to this.
public abstract record TypedExpression(
    TypeKind ResultType,
    Expression Syntax          // back-pointer to source expression node
);

/// Field reference.
public sealed record TypedFieldRef(
    TypeKind ResultType,
    string FieldName,
    bool IsCaseInsensitive,    // carries ~string flag for enforcement
    Expression Syntax
) : TypedExpression(ResultType, Syntax);

/// Event arg reference.
public sealed record TypedArgRef(
    TypeKind ResultType,
    string EventName,
    string ArgName,
    Expression Syntax
) : TypedExpression(ResultType, Syntax);

/// Literal (resolved from context).
public sealed record TypedLiteral(
    TypeKind ResultType,
    object? Value,             // parsed literal value
    Expression Syntax
) : TypedExpression(ResultType, Syntax);

/// Binary operation.
public sealed record TypedBinaryOp(
    TypeKind ResultType,
    OperationKind ResolvedOp,
    TypedExpression Left,
    TypedExpression Right,
    ImmutableArray<ProofRequirement> ProofRequirements,
    Expression Syntax
) : TypedExpression(ResultType, Syntax);

/// Unary operation.
public sealed record TypedUnaryOp(
    TypeKind ResultType,
    OperationKind ResolvedOp,
    TypedExpression Operand,
    Expression Syntax
) : TypedExpression(ResultType, Syntax);

/// Function call.
public sealed record TypedFunctionCall(
    TypeKind ResultType,
    FunctionKind ResolvedFunction,
    ImmutableArray<TypedExpression> Arguments,
    ImmutableArray<ProofRequirement> ProofRequirements,
    Expression Syntax
) : TypedExpression(ResultType, Syntax);

/// Member access (.count, .peek, .currency, etc.)
public sealed record TypedMemberAccess(
    TypeKind ResultType,
    TypedExpression Object,
    TypeAccessor ResolvedAccessor,
    ImmutableArray<ProofRequirement> ProofRequirements,
    Expression Syntax
) : TypedExpression(ResultType, Syntax);

/// Conditional (if/then/else).
public sealed record TypedConditional(
    TypeKind ResultType,
    TypedExpression Condition,
    TypedExpression ThenBranch,
    TypedExpression ElseBranch,
    Expression Syntax
) : TypedExpression(ResultType, Syntax);

/// Quantifier (each/any/no).
public sealed record TypedQuantifier(
    TypeKind ResultType,       // always Boolean
    string BindingName,
    TypeKind BindingType,
    TypedExpression Collection,
    TypedExpression Predicate,
    Expression Syntax
) : TypedExpression(ResultType, Syntax);

/// Error expression — propagates ErrorType.
public sealed record TypedError(
    Expression Syntax
) : TypedExpression(TypeKind.Error, Syntax);
```

#### Dependency Facts

```csharp
/// Computed field dependency edge.
public sealed record ComputedFieldDep(
    string FieldName,
    ImmutableArray<string> DependsOn
);

/// Constraint referenced-field set (for semantic subject attribution).
public sealed record ConstraintFieldRefs(
    object ConstraintIdentity,  // TypedRule or TypedEnsure reference
    ImmutableArray<string> ReferencedFields,
    ImmutableArray<string> ReferencedArgs
);
```

#### The SemanticIndex Itself

```csharp
public sealed record SemanticIndex(
    // Symbol tables — keyed access
    ImmutableDictionary<string, TypedField> Fields,
    ImmutableDictionary<string, TypedState> States,
    ImmutableDictionary<string, TypedEvent> Events,

    // Normalized declarations — ordered arrays
    ImmutableArray<TypedTransitionRow> TransitionRows,
    ImmutableArray<TypedRule> Rules,
    ImmutableArray<TypedEnsure> Ensures,
    ImmutableArray<TypedAccessMode> AccessModes,
    ImmutableArray<TypedStateHook> StateHooks,
    ImmutableArray<TypedEventHandler> EventHandlers,

    // Dependency facts
    ImmutableArray<ComputedFieldDep> ComputedDeps,
    ImmutableArray<ConstraintFieldRefs> ConstraintRefs,

    // Diagnostics
    ImmutableArray<Diagnostic> Diagnostics
);
```

**Collection type rationale:**
- **Symbols** use `ImmutableDictionary` — consumers need O(1) lookup by name (LS hover/go-to-def, expression resolution, graph analysis by state identity).
- **Normalized declarations** use `ImmutableArray` — consumers need ordered iteration (transition rows are order-sensitive for first-match semantics; rules/ensures are collect-all but order matters for diagnostic stability). Graph analysis indexes them by (state, event) at its own layer.
- **Dependency facts** use `ImmutableArray` — computed deps are small, iterated once by graph analysis to build the full dependency DAG.

---

## 4. Resolution Architecture

### The Thesis: Metadata Interpreter, Not Type Checker

Traditional type checkers have hundreds of per-node visitor methods. Precept's catalogs encode the complete type algebra: every legal (operator, type, type) → result triple, every function overload, every accessor, every widening path, every modifier applicability rule. The checker's job isn't to *know* the type system — the catalogs know it. The checker's job is to *apply* it: look things up, report when lookups fail, and organize results into the SemanticIndex.

This reframes the architecture: **the type checker is a two-pass metadata resolution engine with a small kernel of structural validation logic.**

### Pass 1: Registration (Symbol Table Construction)

**Input:** `ConstructManifest.Declarations`
**Output:** Mutable symbol tables (field, state, event)
**No expression checking.** No diagnostics beyond duplicates and structural errors.

The registration pass walks all declarations once:

```
for each declaration in manifest.Declarations:
    match declaration:
        FieldDeclarationNode → register field names + resolve TypeRef → TypeKind
        StateDeclarationNode → register state names + resolve modifiers
        EventDeclarationNode → register event names + resolve arg types
        (all others) → skip (processed in Pass 2)
```

**What "resolve TypeRef → TypeKind" means:** Query `Types.ByTokenKind` for the keyword token → get TypeMeta → stamp TypeKind. For collections, extract element type. For choice, extract the choice definition. This is pure catalog lookup — no expression resolution needed.

**Initial state / terminal / required validation** also fires here (counting modifiers).

### Pass 2: Checking (Expression Resolution + Normalization)

**Input:** Symbol tables (from Pass 1) + `ConstructManifest.Declarations`
**Output:** `SemanticIndex`

Pass 2 has **three generic sub-passes** rather than per-construct methods:

#### Sub-pass 2a: Expression Resolution Engine

The core of the checker. A single recursive function that resolves any `Expression` node to a `TypedExpression`:

```
TypedExpression Resolve(Expression expr, TypeKind? expectedType):
    match expr:
        LiteralExpression     → resolve via context (expectedType)
        IdentifierExpression  → look up in symbol table → TypedFieldRef or TypedArgRef
        BinaryExpression      → resolve left, resolve right,
                                query Operations catalog for (op, leftType, rightType),
                                if found → TypedBinaryOp with catalog's result type + OperationKind
                                if not → emit TypeMismatch, return TypedError
        UnaryExpression       → resolve operand,
                                query Operations catalog for (op, operandType),
                                if found → TypedUnaryOp
        FunctionCallExpr      → resolve args,
                                query Functions.FindByName(name),
                                match overload by arg types (with widening),
                                if found → TypedFunctionCall
        MemberAccessExpr      → resolve object,
                                query Types.GetMeta(objectType).Accessors for member,
                                if found → TypedMemberAccess
        ConditionalExpr       → resolve condition (expect boolean),
                                resolve then/else, unify branch types
        QuantifierExpr        → resolve collection, push binding, resolve predicate (expect boolean), pop
        GroupedExpr           → resolve inner
        ListLiteralExpr       → resolve elements, check against expected element type
```

**This is the metadata interpreter.** The critical insight: this function has no per-type-kind branching for operators or functions. It doesn't know what `+` means for money vs integers — it asks the Operations catalog. It doesn't know what `min` accepts — it asks the Functions catalog. It doesn't know what `.count` returns — it asks the Types catalog. The function is ~100 lines of structural pattern matching + catalog queries.

#### Sub-pass 2b: Declaration Normalization

Walks each declaration kind and resolves contained expressions via Sub-pass 2a, producing typed inventory entries:

- **TransitionRowNode** → Resolve guard + actions + outcome → `TypedTransitionRow`
- **RuleDeclarationNode** → Resolve condition + guard + message → `TypedRule`
- **StateEnsureNode / EventEnsureNode** → Resolve condition + guard + message → `TypedEnsure`
- **AccessModeNode** → Validate field/state names, resolve guard → `TypedAccessMode`
- **StateActionNode** → Resolve guard + actions → `TypedStateHook`
- **EventHandlerNode** → Resolve actions → `TypedEventHandler`
- **FieldDeclarationNode** (computed) → Resolve computed expression → populate `TypedField.ComputedExpression`

This IS per-construct dispatch, but it's thin — each case is 5-10 lines of "resolve the expressions this construct contains, validate structural constraints, produce a typed entry." The construct-specific logic is minimal: check that guards are boolean, check that messages are strings, validate action-field compatibility.

#### Sub-pass 2c: Structural Validation

After all expressions are resolved:
- **Computed field cycle detection** — build dep graph from `ComputedExpression` references, DFS for cycles
- **Choice validation** — validate choice value sets, subset relationships, ordering
- **Stateless/stateful cross-validation** — check for EventHandlerNode + states conflict
- **Initial event field assignment completeness** — if initial event exists, verify required fields are assigned

### Catalog Query API

The resolution engine needs efficient catalog queries. Current catalogs provide:
- `Operations.GetMeta(OperationKind)` — exhaustive switch
- `Functions.FindByName(string)` → overload array
- `Types.GetMeta(TypeKind)` → TypeMeta with Accessors and WidensTo

**Missing for type checker efficiency:**

1. **Operations lookup by (OperatorKind, TypeKind, TypeKind) → OperationMeta?** — Currently the checker would need to scan all operations. Needed: a frozen index `FrozenDictionary<(OperatorKind, TypeKind, TypeKind), BinaryOperationMeta>`.

2. **Operations lookup by (OperatorKind, TypeKind) → UnaryOperationMeta?** — Same issue.

3. **Widening-aware operation lookup** — When `(+, integer, decimal)` isn't found directly, the checker should try `(+, decimal, decimal)` via `integer.WidensTo.Contains(decimal)`. This is a small algorithm wrapping the lookup: try direct match first, then try widened variants.

**Proposed additions to `Operations` class:**

```csharp
// Binary operation lookup: (op, lhs, rhs) → BinaryOperationMeta?
public static FrozenDictionary<(OperatorKind, TypeKind, TypeKind), BinaryOperationMeta> BinaryBySignature { get; }

// Unary operation lookup: (op, operand) → UnaryOperationMeta?
public static FrozenDictionary<(OperatorKind, TypeKind), UnaryOperationMeta> UnaryBySignature { get; }
```

These are derived indexes (built at startup from the catalog's `All` property), following the exact pattern of `Functions.ByName` and `Actions.ByTokenKind`.

### How "Earliest-Knowable Kind Assignment" Works

| What | Where stamped | How |
|------|---------------|-----|
| `TypeKind` on TypeRef | Parser (Pass 1 validates) | Parser resolves keyword token → TypeKind via `Types.ByTokenKind` |
| `OperatorKind` | Parser | Parser stamps from token |
| `ActionKind` | Parser | Parser stamps from token via `Actions.ByTokenKind` |
| `ModifierKind` | Parser | Parser stamps from token |
| `ConstructKind` | Parser | Parser stamps from leading token via `Constructs.ByLeadingToken` |
| `OperationKind` | **Type Checker** | Requires knowing both operand types → catalog lookup |
| `FunctionKind` | **Type Checker** | Requires knowing arg types → overload resolution |
| `TypeAccessor` | **Type Checker** | Requires knowing object type → catalog lookup |
| Result `TypeKind` on expressions | **Type Checker** | Derived from the above resolutions |

### Type Widening Integration

Widening is NOT a separate phase. It's a helper function used inside resolution:

```csharp
bool IsAssignable(TypeKind source, TypeKind target)
{
    if (source == target) return true;
    if (source == TypeKind.Error || target == TypeKind.Error) return true; // ErrorType compat
    return Types.GetMeta(source).WidensTo.Contains(target);
}
```

Used in:
- Assignment validation (`set Field = Expr`)
- Function overload matching (arg type ← param type)
- Binary operation lookup fallback (try widened variants)
- Default value validation
- Conditional branch unification

The function is 5 lines. The catalog owns the data; the checker owns the algorithm.

---

## 5. Vertical Slice Decomposition (Preliminary)

### Slice 1: Registration Pass + Symbol Tables

- Build field, state, event symbol tables from declarations
- Resolve TypeRef → TypeKind
- Detect duplicates (names)
- Validate initial state constraints (multiple/none)
- **Output:** Working symbol tables; foundation for everything else
- **Tests:** Name resolution tests — duplicate detection, unknown references, initial state validation

### Slice 2: Expression Resolution Core (Scalars Only)

- Implement the recursive expression resolver for: identifiers, numeric/boolean/string literals, binary operators (arithmetic, comparison, logical), unary operators (not, negate)
- Build Operations catalog lookup indexes (`BinaryBySignature`, `UnaryBySignature`)
- Type widening in IsAssignable + operation lookup fallback
- ErrorType propagation
- **Output:** Typed expressions for the scalar subset; TypeMismatch diagnostics
- **Tests:** Binary/unary expression typing, widening, ErrorType behavior

### Slice 3: Functions + Member Access

- Overload resolution against Functions catalog
- Member access resolution against TypeMeta.Accessors
- ProofRequirement recording from overload/accessor entries
- **Output:** TypedFunctionCall, TypedMemberAccess with resolved identities
- **Tests:** Function arity, arg type mismatch, accessor resolution, proof obligation recording

### Slice 4: Context-Sensitive Literals + Typed Constants

- Top-down context propagation for numeric literal resolution
- Typed constant content validation (requires Gap 1 resolution or temporary hardcode)
- **Output:** TypedLiteral with context-resolved TypeKind
- **Tests:** Numeric literal in integer/decimal/number context, typed constant validation, no-context errors

### Slice 5: Transition Rows + Actions

- Normalize TransitionRowNodes to TypedTransitionRow
- Resolve guards (must be boolean)
- Resolve action chains → TypedAction/TypedInputAction/TypedBindingAction
- Action-field applicability validation from ActionMeta
- Scope: event args in scope for guards and action values
- **Output:** Complete transition row inventory
- **Tests:** Action type checking, field type mismatch, computed write prevention, into-target validation

### Slice 6: Rules + Ensures + Constraints

- Normalize RuleDeclarationNode → TypedRule
- Normalize StateEnsureNode/EventEnsureNode → TypedEnsure
- Validate condition = boolean, message = string
- Compute semantic subjects (referenced fields)
- **Output:** Complete constraint inventories
- **Tests:** Constraint typing, scope validation (rules can't reference args), ensure anchor resolution

### Slice 7: Modifier Validation

- All modifier checks from §3.8: applicability, mutual exclusivity, subsumption, bounds checking
- writable on computed/arg prevention
- Value validation (min > max, negative counts)
- **Output:** Modifier diagnostics
- **Tests:** Every modifier × applicable type, redundancy warnings, bounds errors

### Slice 8: Collections + Quantifiers + `~string`

- `contains` operator resolution
- Collection action type checking (element type matching)
- Quantifier expression resolution (binding variable scope, predicate typing)
- `~string` CI enforcement at operator/function sites
- **Output:** Complete collection/CI enforcement coverage
- **Tests:** Contains typing, CI enforcement diagnostics, quantifier scope/type tests

### Slice 9: Computed Fields + Access Modes + Choices

- Computed field dependency graph + cycle detection
- Computed expression type validation
- Access mode validation (redundant, conflicting)
- Choice type validation (full §3.12 suite)
- Stateless/stateful cross-validation
- **Output:** Complete SemanticIndex with all structural validations
- **Tests:** Cycle detection, choice validation suite, access mode conflicts

### Slice 10: SemanticIndex Sealing + Consumer Integration

- Final SemanticIndex construction and immutable sealing
- Verify anti-mirroring rules (structural test: graph/proof/builder cannot access Syntax properties)
- Wire type checker into pipeline (`TypeChecker.Check` returns complete SemanticIndex)
- Integration tests with full .precept files from `samples/`
- **Output:** Production-ready type checker
- **Tests:** End-to-end integration tests, anti-mirroring enforcement

---

### Dependency Graph

```
Slice 1 (Registration)
   ↓
Slice 2 (Expression Core)
   ↓
Slice 3 (Functions + Accessors) ← depends on Slice 2
   ↓
Slice 4 (Literals + Typed Constants) ← depends on Slice 2
   ↓
Slice 5 (Transitions + Actions) ← depends on Slices 2-4
Slice 6 (Rules + Ensures) ← depends on Slices 2-4
Slice 7 (Modifiers) ← depends on Slice 1
   ↓
Slice 8 (Collections + Quantifiers + CI) ← depends on Slices 2-3, 5
   ↓
Slice 9 (Computed + Access + Choice) ← depends on Slices 1-6
   ↓
Slice 10 (Sealing + Integration) ← depends on all
```

Slices 5, 6, and 7 can be developed in parallel after Slice 4. Slice 7 only depends on Slice 1 (modifier validation doesn't need expression resolution).

---

## 6. Open Questions

These are unresolved design decisions that must be answered before the relevant TypeChecker slice is implemented. They are documented here so they surface at implementation time rather than mid-slice.

### OQ-1: DiagnosticCode for `~startsWith`/`~endsWith` with non-`~string` first argument

**Context:** The spec §3.7 says: *"First argument must be a `~string` field; compile error otherwise."* The existing diagnostics 97/98 (`CaseInsensitiveFieldRequiresTildeStartsWith`, `CaseInsensitiveFieldRequiresTildeEndsWith`) cover the **inverse** direction: plain `startsWith`/`endsWith` called with a `~string` first argument — pointing the user toward the CI form.

No `DiagnosticCode` exists for the **forward direction**: `~startsWith`/`~endsWith` called with a plain `string` first argument (the caller used the CI form but passed a non-CI-qualified field).

**Decision required before Slice 8 (`~string` CI enforcement):**

| Option | Description | Consequence |
|--------|-------------|-------------|
| **A — Error** | Add a new `DiagnosticCode` (e.g., `TildeStartsWithRequiresCIString` / `TildeEndsWithRequiresCIString`). The TypeChecker emits it when the first arg resolves to plain `string`. | Fully enforces §3.7 spec text. Requires two new `DiagnosticCode` members and catalog/doc entries. |
| **B — Warning** | Change §3.7 spec text from "compile error" to "compiler warning." Emit as a warning-level diagnostic. | Softer enforcement — callers aren't blocked. May be more ergonomic for prototyping. Requires spec update. |
| **C — Skip enforcement** | Remove the §3.7 "compile error" language entirely. CI functions work the same regardless of whether the first arg is `~string`. | Simplifies the checker but removes a protection the spec currently promises. Requires spec update. |

**Flagged by:** Frank (GAP-046 design brief, §10 Open Design Note). Shane sign-off required before implementing.

**Owned by:** George (Slice 8 implementation).

---

## Key Architectural Insight

> **The type checker is a metadata resolution engine with structural scaffolding, not a structural validator with metadata lookups.**

The traditional mental model is wrong for Precept. In a traditional compiler, the type checker "knows" the type system and implements it. In Precept, the catalogs "know" the type system — the checker just applies it. 70-75% of the checker's work is asking catalogs questions and recording answers. The remaining 25-30% is structural: symbol tables, scope management, cycle detection, choice set logic.

This has a profound implication: **new language features (operations, functions, types, modifiers, actions) should require zero type checker code changes.** Add the catalog entry → the checker's generic resolution engine automatically handles it. Only genuinely new structural patterns (a new scope rule, a new validation shape) require checker changes.

The Operations catalog with its ~200 entries IS the type checker for expression typing. The Functions catalog IS the type checker for function calls. The Modifiers catalog IS the type checker for modifier validation. The checker is the machinery that reads them.
