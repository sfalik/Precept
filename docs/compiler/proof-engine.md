# Proof Engine

## 1. Status

| Property | Value |
|---|---|
| Doc maturity | Full |
| Implementation state | Implemented |
| Source | `src/Precept/Pipeline/ProofEngine.cs`, `src/Precept/Pipeline/ProofLedger.cs` |
| Upstream | SemanticIndex + StateGraph, catalog metadata (Operations, Functions, Types, Modifiers, Actions, Diagnostics, Faults) |
| Downstream | Compilation (proof ledger), Precept Builder (fault backstops, constraint influence map) |

---

## 2. Overview

The proof engine is the fifth and final analysis stage before the Precept Builder — the compile-time half of Precept's structural safety guarantee. Its role: discharge statically preventable runtime hazards using a bounded, five-strategy set. If an operation is proven safe, no runtime check is needed. If proof fails, the compiler emits a diagnostic and the author must fix the source before an executable model is produced.

**Pipeline Position:**

```text
Source Text → Lexer → Parser → Name Binder → Type Checker → Graph Analyzer → [Proof Engine] → Precept Builder
                                    ↓               ↓                ↓
                              SemanticIndex    StateGraph       ProofLedger
```

The proof engine consumes the `SemanticIndex` (typed expressions with attached `ProofRequirement` records) and `StateGraph` (structural analysis facts including `ProofForwardingFact` entries). It produces a `ProofLedger` — the complete obligation inventory with dispositions, fault-site links, constraint influence analysis, and initial-state satisfiability results.

**Key design choices:**

1. **Proof is bounded** — five strategies only, no general SMT solver. Predictable, auditable, zero external dependencies.
2. **Proof ledger does NOT cross the compile-runtime boundary** — only `FaultSiteDescriptor` residue (defense-in-depth backstops) crosses into runtime. The proof engine is purely a compile-time analysis stage.
3. **Catalog-driven obligations** — the proof engine reads `ProofRequirement` records stamped by the type checker from catalog metadata. It does NOT maintain its own list of what needs to be proved.

---

## 3. Responsibilities and Boundaries

### In Scope

| Responsibility | Description |
|---|---|
| **Obligation instantiation** | Create `ProofObligation` records from `ProofRequirement` attachments on typed expressions and actions |
| **Obligation discharge** | Apply five proof strategies in order to resolve each obligation |
| **Diagnostic emission** | Emit diagnostics for unresolved obligations with semantic site attribution |
| **FaultSiteLink production** | Link unresolved obligations to their corresponding `FaultCode` for Precept Builder backstops |
| **Constraint influence analysis** | Traverse constraint expressions to record field/arg dependencies for causal reasoning |
| **Initial-state satisfiability** | Verify that declared initial-state constraints are satisfiable given default field values |
| **ProofForwardingFact consumption** | Incorporate structural violations forwarded from graph analysis into proof obligations |

### Out of Scope

| Exclusion | Rationale |
|---|---|
| **Semantic resolution** | Names, types, and overloads are already resolved by the type checker. The proof engine consumes resolved `TypedExpression` nodes. |
| **Graph topology** | Reachability, dominance, and edge sets are the graph analyzer's responsibility. The proof engine receives structural facts, not the topology itself. |
| **Expression evaluation** | The evaluator handles runtime values. The proof engine performs static analysis only. |
| **Constraint enforcement** | Runtime constraint checking is the evaluator's responsibility. The proof engine verifies satisfiability statically. |
| **Runtime fault handling** | The proof engine identifies what *could* fault; the evaluator and `FaultSiteDescriptor` backstops handle actual runtime failures. |

---

## 4. Right-Sizing

The proof engine is **intentionally bounded** — five strategies only, no external solver. This is a deliberate scope limit, not a capability gap.

| Metric | Value | Rationale |
|---|---|---|
| Estimated LOC | 400–600 | ~150 obligation instantiation + ~200 strategy dispatch + ~100 influence analysis + ~50 satisfiability |
| Strategy count | 5 | Bounded set covers the DSL's constrained expression language |
| External dependencies | 0 | No SMT solver, no SAT solver, no external libraries |
| Determinism | 100% | Same input always produces same output — no solver timeouts or resource limits |

**Why five strategies, not a general solver:**

General SMT solving (Z3, CVC4/5) would add non-deterministic verification times, external dependencies, and implementation complexity. The surveyed verification systems (SPARK Ada/GNATprove, Dafny, Liquid Haskell, CBMC) all depend on external solvers for general proof discharge. Precept's DSL is intentionally constrained — the expression language is finite, the obligation space is bounded, and the five-strategy set covers realistic programs.

**Boundary condition:** If the five strategies prove insufficient for real programs, a sixth bounded strategy would be added — not a general solver. The strategy set is bounded, not extensible by users.

---

## 5. Inputs and Outputs

### Input

**`SemanticIndex`** — the type checker's output containing typed expressions with resolved `ProofRequirement` attachments:

```csharp
// Typed expressions carry proof requirements from catalog metadata
public sealed record TypedBinaryOp(
    TypeKind ResultType,
    OperationKind ResolvedOp,
    TypedExpression Left,
    TypedExpression Right,
    QualifierBinding? ResultQualifier,
    ImmutableArray<ProofRequirement> ProofRequirements,  // ← from BinaryOperationMeta
    SourceSpan Span
) : TypedExpression(ResultType, Span);

public sealed record TypedFunctionCall(
    TypeKind ResultType,
    FunctionKind ResolvedFunction,
    ImmutableArray<TypedExpression> Arguments,
    ImmutableArray<ProofRequirement> ProofRequirements,  // ← from FunctionOverload
    SourceSpan Span
) : TypedExpression(ResultType, Span);

public sealed record TypedMemberAccess(
    TypeKind ResultType,
    TypedExpression Object,
    TypeAccessor ResolvedAccessor,
    ImmutableArray<ProofRequirement> ProofRequirements,  // ← from TypeAccessor
    SourceSpan Span
) : TypedExpression(ResultType, Span);

// Typed actions also carry proof requirements
public record TypedAction(
    ActionKind Kind,
    string FieldName,
    TypeKind FieldType,
    ImmutableArray<ProofRequirement> ProofRequirements,  // ← from ActionMeta
    SourceSpan Span
);
```

**`StateGraph`** — the graph analyzer's output containing `ProofForwardingFact` entries:

```csharp
public sealed record StateGraph(
    // ... topology and structural analysis ...
    ImmutableArray<ProofForwardingFact> ProofFacts,
    ImmutableArray<Diagnostic> Diagnostics
);

// Proof forwarding facts from graph analysis
public abstract record ProofForwardingFact;

public sealed record ReachabilityFact(
    string StateName,
    bool IsReachable,
    ImmutableArray<string>? PathFromInitial
) : ProofForwardingFact;

public sealed record DominancePathFact(
    string RequiredState,
    ImmutableArray<string> DominatedTerminals
) : ProofForwardingFact;

public sealed record EventCoverageFact(
    string EventName,
    ImmutableArray<string> UnhandledReachableStates
) : ProofForwardingFact;

public sealed record TerminalCompletenessFact(
    bool AllTerminalsReachable,
    ImmutableArray<string> UnreachableTerminals
) : ProofForwardingFact;

public sealed record DeadEndStateFact(
    ImmutableArray<string> DeadEndStates,
    int DeadEndCount
) : ProofForwardingFact;
```

**Catalog metadata** — read at proof time for FaultCode↔DiagnosticCode correspondence:

- `Faults.GetMeta(code)` — fault metadata including `RecoveryHint`
- `FaultCode` enum values with `[StaticallyPreventable(DiagnosticCode)]` attributes

### Output

**`ProofLedger`** — the complete obligation ledger:

```csharp
public sealed record ProofLedger(
    // All obligations with their dispositions
    ImmutableArray<ProofObligation> Obligations,
    
    // Fault sites for unresolved obligations — consumed by Precept Builder
    ImmutableArray<FaultSiteLink> FaultSiteLinks,
    
    // Constraint influence analysis — consumed by Precept Builder
    ImmutableArray<ConstraintInfluenceEntry> ConstraintInfluence,
    
    // Initial-state satisfiability results
    ImmutableArray<InitialStateSatisfiabilityResult> InitialStateResults,
    
    // Diagnostics
    ImmutableArray<Diagnostic> Diagnostics
);
```

#### ProofObligation

Each obligation tracks its source, requirement, and resolution:

```csharp
public sealed record ProofObligation(
    ProofRequirement Requirement,     // the catalog-declared requirement
    TypedExpression Site,             // the expression node where obligation was instantiated
    ObligationContext Context,        // the enclosing scope that owns this expression (set at instantiation)
    ProofDisposition Disposition,     // Proved | Unresolved
    ProofStrategy? Strategy,          // which strategy discharged it (null if Unresolved)
    DiagnosticCode? EmittedDiagnostic // diagnostic code if Unresolved (null if Proved)
);

/// Discriminated union: the enclosing scope in which an obligation's expression lives.
/// Attached at Pass 1 instantiation time — the walk already knows the context.
/// Strategies 3/4 read this to find the enclosing guard (O(1) lookup, not post-hoc search).
public abstract record ObligationContext;

public sealed record TransitionRowContext(TypedTransitionRow Row) : ObligationContext;
public sealed record ConstraintContext(ConstraintIdentity Constraint) : ObligationContext;
public sealed record StateHookContext(TypedStateHook Hook) : ObligationContext;
public sealed record EventHandlerContext(TypedEventHandler Handler) : ObligationContext;
public sealed record FieldExpressionContext(TypedField Field) : ObligationContext;

public enum ProofDisposition { Proved = 1, Unresolved = 2 }

public enum ProofStrategy
{
    Literal = 1,                 // value is a compile-time constant
    DeclarationAttribute = 2,    // declaration-site attributes establish the proof
    GuardInPath = 3,             // enclosing guard establishes the constraint
    FlowNarrowing = 4,           // same-row guard narrows the type state
    QualifierCompatibility = 5   // qualifier values are provably compatible
}
```

> **Resolved (CC#6):** `ProofObligation.Site` identity — reference equality
> The proof engine stores the same `TypedExpression` object reference that exists in the `SemanticIndex`. No copies are made. The builder resolves structural binding during Pass 4 (expression compilation). When the builder visits a `TypedExpression` to emit an opcode, it matches against `ProofLedger.FaultSiteLinks` by `ProofObligation.Site` identity using `ReferenceEqualityComparer.Instance`. The structural binding is the opcode itself — a nullable `FaultSiteAnnotation?` is stamped directly on the emitted opcode.
>
> **Matching mechanism:**
> ```csharp
> // Builder internals:
> var faultSites = new HashSet<TypedExpression>(
>     proof.FaultSiteLinks.Select(l => l.Obligation.Site),
>     ReferenceEqualityComparer.Instance);
>
> // During expression walk:
> if (faultSites.TryGetValue(expr, out var matched))
> {
>     // Stamp FaultSiteDescriptor on this opcode
> }
> ```
>
> **Invariant:** The proof engine MUST NOT use `with { ... }` on `obligation.Site` or create new `TypedExpression` instances — it must preserve the original object reference. The `ProofObligation` itself can use `with { ... }` (it's a separate record), but the `Site` field must point to the original `SemanticIndex` expression. Reference identity prevents false positives from structural equality matching identical-but-distinct expressions in different transition rows.

#### FaultSiteLink

Links unresolved obligations to their runtime fault codes:

```csharp
public sealed record FaultSiteLink(
    ProofObligation Obligation,  // the unresolved obligation
    FaultCode FaultCode,         // the runtime fault that would fire
    DiagnosticCode DiagnosticCode, // the authoring-time diagnostic
    SourceSpan Site              // source location for the backstop
);
```

The Precept Builder consumes these to plant `FaultSiteAnnotation` backstops — defense-in-depth runtime checks for operations that could not be proven safe.

> **Resolved (CC#6):** `FaultSiteLink.Site` to runtime binding
> The builder resolves `FaultSiteLink` to a structural binding during Pass 4 (expression compilation). When the builder compiles a `TypedExpression` into an opcode, it matches `ProofObligation.Site` (the `TypedExpression`) against the expression being compiled. If a matching `FaultSiteLink` exists, the builder stamps a `FaultSiteAnnotation` on the opcode. The `FaultSiteLink.Site` (`SourceSpan`) is carried forward in the annotation for diagnostics/logging; the structural binding is the opcode itself.
>
> **Canonical annotation shape:**
>
> ```csharp
> public sealed record FaultSiteAnnotation(
>     FaultCode Code,           // Runtime fault to fire if reached
>     DiagnosticCode PreventedBy, // Authoring-time diagnostic that would prevent this
>     SourceSpan Site           // Source location for diagnostics/logging
> );
> // On each Opcode — null = proven safe
> FaultSiteAnnotation? FaultSite
> ```
>
> **Structural elision model:** Proved obligations produce no `FaultSiteLink` → no annotation → zero evaluator overhead. This matches the SPARK Ada model (strip proven checks) realized through Precept's gate architecture. See `precept-builder.md §Pass 4` for the planting contract and `evaluator.md §7.3` for the consumption contract.

#### ConstraintInfluenceEntry

Records which fields and event args influence each constraint:

```csharp
public sealed record ConstraintInfluenceEntry(
    ConstraintIdentity Constraint,     // which rule or ensure
    ImmutableArray<string> ReferencedFields,
    ImmutableArray<EventArgReference> ReferencedArgs
);

public abstract record ConstraintIdentity;
public sealed record RuleIdentity(int RuleIndex) : ConstraintIdentity;
public sealed record EnsureIdentity(ConstraintKind Kind, string? AnchorName, int EnsureIndex) : ConstraintIdentity;

public sealed record EventArgReference(string EventName, string ArgName);
```

The Precept Builder reorganizes these into a `ConstraintInfluenceMap` for AI agents to reason causally: "which fields affect which constraints?"

#### InitialStateSatisfiabilityResult

Reports whether an initial state's constraints can be satisfied:

```csharp
public sealed record InitialStateSatisfiabilityResult(
    string StateName,
    bool IsSatisfiable,
    ImmutableArray<UnsatisfiedConstraint> Violations
);

public sealed record UnsatisfiedConstraint(
    ConstraintIdentity Constraint,
    string Reason  // human-readable explanation
);
```

---

## 6. Architecture

### Two-Pass Design

The proof engine operates in two sequential passes:

```text
┌─────────────────────────────────────────────────────────────────────┐
│                    SemanticIndex + StateGraph                        │
└─────────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────┐
│  Pass 1: Obligation Instantiation                                    │
│  • Walk explicit SemanticIndex members (PE-G4):                      │
│    - TransitionRows[].Actions[] (+ nested InputExpression)           │
│    - EventHandlers[].Actions[]                                       │
│    - StateHooks[].Actions[]                                          │
│    - Rules[].Condition                                               │
│    - Ensures[].Condition                                             │
│    - Fields[].ComputedExpression                                     │
│  • For each TypedBinaryOp/TypedFunctionCall/TypedMemberAccess/       │
│    TypedAction with non-empty ProofRequirements:                     │
│    - Create ProofObligation with ObligationContext (PE-G6)           │
│  • Incorporate ProofForwardingFacts from StateGraph                  │
└─────────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────┐
│  Pass 2: Obligation Discharge                                        │
│  • For each ProofObligation:                                         │
│    - Try Strategy 1 (Literal) → if success, mark Proved              │
│    - Try Strategy 2 (DeclarationAttribute) → if success, mark Proved │
│    - Try Strategy 3 (GuardInPath) → if success, mark Proved          │
│    - Try Strategy 4 (FlowNarrowing) → if success, mark Proved        │
│    - Try Strategy 5 (QualifierCompatibility) → if success, mark Proved │
│    - If all fail → mark Unresolved, emit diagnostic                  │
│  • Build FaultSiteLinks for unresolved obligations                   │
│  • Run constraint influence analysis                                 │
│  • Run initial-state satisfiability check                            │
└─────────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────┐
│                            ProofLedger                               │
└─────────────────────────────────────────────────────────────────────┘
```

### Catalog-Driven Obligation Instantiation

**The proof engine does NOT maintain its own list of what needs to be proved.** Obligations are declared in catalog metadata and stamped onto typed expressions by the type checker:

| Catalog Entry | Metadata Property | Example |
|---|---|---|
| `BinaryOperationMeta` | `ProofRequirements` | Division operations declare `NumericProofRequirement(Subject, !=, 0)` |
| `FunctionOverload` | `ProofRequirements` | `sqrt()` declares `NumericProofRequirement(Subject, >=, 0)` |
| `FunctionOverload` | `ProofRequirements` | `pow()` declares `NumericProofRequirement(exp >= 0)` for integer exponents |
| `TypeAccessor` | `ProofRequirements` | `.first`, `.last`, `.peek` declare `NumericProofRequirement(count > 0)` |
| `ActionMeta` | `ProofRequirements` | `dequeue`, `pop` declare `NumericProofRequirement(count > 0)` |

The type checker stamps these requirements onto `TypedExpression` and `TypedAction` nodes when it resolves them. The proof engine instantiates `ProofObligation` records from the stamped requirements — it reads them, doesn't compute them.

### ProofRequirement Catalog DU

The `ProofRequirement` discriminated union has five subtypes (one per `ProofRequirementKind`):

```csharp
// Abstract base — discriminated union
public abstract record ProofRequirement(ProofRequirementKind Kind, string Description);

// 1. Numeric interval check
public sealed record NumericProofRequirement(
    ProofSubject Subject,
    OperatorKind Comparison,  // !=, >=, >, etc.
    decimal Threshold,        // 0 for non-zero, 0 for non-negative
    string Description
) : ProofRequirement(ProofRequirementKind.Numeric, Description);

// 2. Presence check (optional field must be set before access)
public sealed record PresenceProofRequirement(
    ProofSubject Subject,
    string Description
) : ProofRequirement(ProofRequirementKind.Presence, Description);

// 3. Dimension check (period operand must have required time dimension)
public sealed record DimensionProofRequirement(
    ProofSubject Subject,
    PeriodDimension RequiredDimension,
    string Description
) : ProofRequirement(ProofRequirementKind.Dimension, Description);
// Discharged by Strategy 2 (Declaration Attribute Proof). Reads the subject's
// resolved period dimension — from field qualifier for field references, from
// literal temporal unit for literals. `PeriodDimension.Any` permissively
// satisfies any dimension requirement.

// 4. Modifier check (field must declare required modifier)
public sealed record ModifierRequirement(
    ProofSubject Subject,
    ModifierKind Required,
    string Description
) : ProofRequirement(ProofRequirementKind.Modifier, Description);
// Discharged by Strategy 2 (Declaration Attribute Proof). Checks direct
// modifier membership: `field.Modifiers.Contains(requirement.Required)`.
// Does not use the `ProofSatisfactions` table — this is a presence check, not a
// satisfaction mapping.

// 5. Qualifier compatibility (two operands must share a qualifier value)
public sealed record QualifierCompatibilityProofRequirement(
    ProofSubject LeftSubject,
    ProofSubject RightSubject,
    QualifierAxis Axis,
    string Description
) : ProofRequirement(ProofRequirementKind.QualifierCompatibility, Description);
// Discharged by Strategy 5 (Qualifier Compatibility Proof). The only
// dual-subject requirement kind — requires a dedicated strategy that compares
// two subjects' qualifier bindings. Depends on qualifier resolution in the
// TypeChecker.
```

#### ProofSubject

Identifies what a proof obligation targets:

```csharp
public abstract record ProofSubject;

// References a parameter by object identity
public sealed record ParamSubject(ParameterMeta Parameter) : ProofSubject;

// References the receiver of an accessor or action
public sealed record SelfSubject(TypeAccessor? Accessor = null) : ProofSubject;
```

---

## 7. Component Mechanics

### Five Proof Strategies

Each strategy is a simple predicate function — not a solver. The first strategy that succeeds marks the obligation as `Proved`. Strategies are tried in order (1 → 2 → 3 → 4 → 5).

### Subject Resolution Utilities

Private helper methods in `ProofEngine.cs` for resolving abstract `ProofSubject` identities to concrete expression nodes. Used by all strategies.

#### `ResolveSubject(ProofSubject subject, TypedExpression site) → TypedExpression?`

Resolves a `ProofSubject` to the concrete `TypedExpression` node within the obligation's expression site.

```csharp
static TypedExpression? ResolveSubject(ProofSubject subject, TypedExpression site)
{
    return subject switch
    {
        ParamSubject param => site switch
        {
            TypedBinaryOp bin => ResolveParamInBinaryOp(param.Parameter, bin),
            TypedFunctionCall call => ResolveParamInFunctionCall(param.Parameter, call),
            TypedMemberAccess access => ResolveParamInMemberAccess(param.Parameter, access),
            _ => null
        },

        SelfSubject self => site switch
        {
            TypedMemberAccess access => access.Object,
            TypedAction action => ResolveSelfInAction(self, action),
            _ => null
        },

        _ => null
    };
}
```

**Binary op parameter resolution:** The `BinaryOperationMeta` declares parameters with names like `PInteger`, `PDecimal`. The `ProofRequirement` on a `TypedBinaryOp` carries a `ParamSubject` whose `Parameter` is reference-equal to one of the operation's declared parameters:

```csharp
static TypedExpression? ResolveParamInBinaryOp(ParameterMeta param, TypedBinaryOp bin)
{
    var opMeta = Operations.GetMeta(bin.ResolvedOp);
    if (ReferenceEquals(param, opMeta.Left)) return bin.Left;
    if (ReferenceEquals(param, opMeta.Right)) return bin.Right;
    return null;
}
```

**Function call parameter resolution:**

```csharp
static TypedExpression? ResolveParamInFunctionCall(ParameterMeta param, TypedFunctionCall call)
{
    var overloads = Functions.GetOverloads(call.ResolvedFunction);
    foreach (var overload in overloads)
    {
        for (int i = 0; i < overload.Parameters.Length; i++)
        {
            if (ReferenceEquals(param, overload.Parameters[i]))
                return i < call.Arguments.Length ? call.Arguments[i] : null;
        }
    }
    return null;
}
```

#### `GetFieldName(ProofSubject subject, TypedExpression site) → string?`

Extracts the field name from a resolved subject expression. Used by Strategies 3/4 to match guard fields against obligation subjects.

```csharp
static string? GetFieldName(ProofSubject subject, TypedExpression site)
{
    var resolved = ResolveSubject(subject, site);
    return resolved switch
    {
        TypedFieldRef fieldRef => fieldRef.FieldName,
        TypedMemberAccess { Object: TypedFieldRef fieldRef } => fieldRef.FieldName,
        _ => null
    };
}
```

#### Strategy 1: Literal Proof

**When it applies:** The expression site's subject value is a compile-time literal.

**How it works:** Pattern-match on `TypedLiteral`, extract the value, check against the requirement predicate.

**Examples:**
- `x / 2` — divisor `2` is a non-zero literal → discharge `NumericProofRequirement(!=, 0)`
- `first([1, 2, 3])` — collection literal is non-empty → discharge non-empty requirement
- `sqrt(4)` — operand `4` is non-negative → discharge `NumericProofRequirement(>=, 0)`

**Edge cases:**
- `first([])` — empty collection literal → strategy **fails** (obligation unresolved, diagnostic emitted)
- `x / 0` — divisor `0` → strategy **fails**
- Negative literals for `sqrt()` → strategy **fails**

**Discharge predicate pseudocode:**

```csharp
// Strategy 1: Literal Proof — Discharge Predicate
// Input: ProofObligation (from Pass 1)
// Reads: obligation.Site (TypedExpression), obligation.Requirement
// Scope: NumericProofRequirement ONLY — PresenceProofRequirement is Strategy 2/3

bool TryLiteralProof(ProofObligation obligation)
{
    // 1. Gate: only numeric requirements are literal-provable
    if (obligation.Requirement is not NumericProofRequirement numeric)
        return false;

    // 2. Resolve the proof subject to a concrete expression node
    var subject = ResolveSubject(numeric.Subject, obligation.Site);
    if (subject is not TypedLiteral literal)
        return false;  // subject is not a compile-time constant — cannot prove

    // 3. Extract numeric value (int or decimal)
    var value = literal.Value as decimal? ?? (literal.Value as int?)?.ToDecimal();
    if (value is null) return false;

    // 4. Evaluate the requirement predicate against the literal value
    return numeric.Comparison switch
    {
        OperatorKind.NotEquals          => value != numeric.Threshold,
        OperatorKind.GreaterThan        => value >  numeric.Threshold,
        OperatorKind.GreaterThanOrEqual => value >= numeric.Threshold,
        OperatorKind.LessThan           => value <  numeric.Threshold,
        OperatorKind.LessThanOrEqual    => value <= numeric.Threshold,
        _ => false
    };
    // Passes if the literal value satisfies the comparison against the threshold.
    // Fails (returns false) if the subject isn't a literal, isn't numeric, or
    // the literal violates the requirement (e.g., x / 0, sqrt(-1)).
}
```

> **Intentional scope:** `TryLiteralProof` covers `NumericProofRequirement` only. `PresenceProofRequirement` obligations (null/empty checks) are discharged by Strategy 2 (field modifiers, e.g. `notempty`) or Strategy 3 (guard-in-path, e.g. `when count(x) > 0`). Literal values never statically establish presence — routing `PresenceProofRequirement` to literal proof would be incorrect. The bounded scope is deliberate.

#### Strategy 2: Declaration Attribute Proof

**When it applies:** The obligation can be discharged from declaration-site attributes of the subject — field modifiers, modifier-implied metadata, resolved period dimension, or resolved result metadata on the subject expression/accessor.

**How it works:** Resolve the subject, read the relevant declaration attribute, then either check the requirement directly (dimension, required modifier, known non-negative return metadata) or consult modifier-declared `ProofSatisfactions` metadata for bound-establishing modifiers.

**Non-negative result metadata (two discharge paths):**

- **`FunctionReturnSatisfies`** — when the proof site resolves to a `TypedFunctionCall` and the resolved `FunctionOverload` has `ReturnNonnegative = true`, Strategy 2 discharges `NumericProofRequirement(>=, 0)` directly from the catalog metadata. Example: `abs(X)` is non-negative regardless of `X`, so `sqrt(abs(X))` does not require a user-declared `nonnegative` modifier on an intermediate field.
- **`FixedReturnAccessor.ReturnNonnegative`** — when the obligation subject is `SelfSubject` with a `FixedReturnAccessor` whose `ReturnNonnegative = true`, Strategy 2 discharges `NumericProofRequirement(>=, 0)` trivially. `CollectionCountAccessor` uses this path because collection counts can never be negative. This handles `insert` / `insert-at` proof requirements on plain collection fields without requiring user-declared `notempty`.

**Modifier → ProofSatisfaction Mapping:**

| Modifier | ProofSatisfaction entries |
|---|---|
| `positive` | `Numeric(SelfValue, GreaterThan, Constant(0))` |
| `nonnegative` | `Numeric(SelfValue, GreaterThanOrEqual, Constant(0))` |
| `nonzero` | `Numeric(SelfValue, NotEquals, Constant(0))` |
| `notempty` | `Numeric(Accessor("length"), GreaterThan, Constant(0))`, `Numeric(Accessor("count"), GreaterThan, Constant(0))` |
| `min(N)` | `Numeric(SelfValue, GreaterThanOrEqual, DeclarationValue)` |
| `max(N)` | `Numeric(SelfValue, LessThanOrEqual, DeclarationValue)` |
| `minlength(N)` | `Numeric(Accessor("length"), GreaterThanOrEqual, DeclarationValue)` |
| `maxlength(N)` | `Numeric(Accessor("length"), LessThanOrEqual, DeclarationValue)` |
| `mincount(N)` | `Numeric(Accessor("count"), GreaterThanOrEqual, DeclarationValue)` |
| `maxcount(N)` | `Numeric(Accessor("count"), LessThanOrEqual, DeclarationValue)` |

**Example:**

```precept
field Quantity as nonnegative integer

on Submitted when Quantity > 0
    set Rate = Amount / Quantity   // ← divisor is Quantity
    // nonzero not declared, but guard establishes > 0 (Strategy 3)
```

```precept
field Divisor as positive integer

on Calculate
    set Result = Total / Divisor   // ← Divisor has 'positive' modifier → discharge
```

**Catalog metadata design:** The `ValueModifierMeta` record carries a `ProofSatisfactions` property:

```csharp
public sealed record ValueModifierMeta(
    ModifierKind Kind,
    TokenMeta Token,
    string Description,
    ModifierCategory Category,
    TypeTarget[] ApplicableTo,
    bool HasValue = false,
    ValueModifierDeclarationSite ApplicableDeclarationSites =
        ValueModifierDeclarationSite.FieldDeclaration | ValueModifierDeclarationSite.EventArgDeclaration,
    ModifierKind[] Subsumes = default!,
    ProofSatisfaction[]? ProofSatisfactions = null,  // ← carries proof metadata
    // ... other properties
) : ModifierMeta(...);
```

### ProofSatisfaction DU

The `ProofSatisfaction` discriminated union represents a positive carrier fact that can satisfy a `ProofRequirement`. One per `ProofRequirementKind`:

```csharp
abstract record ProofSatisfaction(ProofRequirementKind RequirementKind)
    sealed record Numeric(SatisfactionProjection Projection, OperatorKind Comparison, NumericBoundSource Bound)
    sealed record Presence()
    sealed record Dimension(DimensionSource Source)
    sealed record Modifier(ModifierKind RequiredModifier)
    sealed record QualifierCompatibility(QualifierAxis Axis)

// Supporting DUs:
abstract record SatisfactionProjection
    sealed record SelfValue()
    sealed record Accessor(string Name)         // "count", "length"

abstract record NumericBoundSource
    sealed record Constant(decimal Value)
    sealed record DeclarationValue()            // for min(N), max(N)

abstract record DimensionSource
    sealed record Constant(PeriodDimension Value)
    sealed record DeclaredTemporalDimension()
```

`ProofSatisfaction.Modifier` exists in the DU for vocabulary completeness but is not currently populated — `ModifierRequirement` uses direct `Contains()` membership, not metadata rows.

**Discharge predicate pseudocode:**

```csharp
// Strategy 2: Declaration Attribute Proof — Discharge Predicate
// Input: ProofObligation (from Pass 1)
// Reads: SemanticIndex.FieldsByName (to get TypedField modifiers and declaration carriers),
//        ValueModifierMeta.ProofSatisfactions (from Modifiers catalog),
//        TypedField.Presence (DeclaredPresenceMeta carrier),
//        TypedField.DeclaredQualifiers (DeclaredQualifierMeta carrier),
//        resolved period dimension metadata for period-typed subjects
// Scope: Single-subject obligations discharged by declaration-site attributes

bool TryDeclarationAttributeProof(ProofObligation obligation, SemanticIndex semantics)
{
    // DimensionProofRequirement: check if subject's period dimension satisfies the requirement
    if (obligation.Requirement is DimensionProofRequirement dimReq)
    {
        var subject = ResolveSubject(dimReq.Subject, obligation.Site);
        var dimension = ResolvePeriodDimension(subject, semantics);
        // PeriodDimension.Any always satisfies (permissive unqualified periods — locked decision)
        return dimension == PeriodDimension.Any || dimension == dimReq.RequiredDimension;
    }

    // ModifierRequirement: check if subject field declares the required modifier
    if (obligation.Requirement is ModifierRequirement modReq)
    {
        var fieldName = GetFieldName(modReq.Subject, obligation.Site);
        if (fieldName is null) return false;
        if (!semantics.FieldsByName.TryGetValue(fieldName, out var field)) return false;

        // Direct modifier check — does the field's declaration include the required modifier?
        return field.Modifiers.Contains(modReq.Required);
    }

    // Numeric/Presence requirements: declaration modifiers may imply the needed bound
    ProofSubject? subject = obligation.Requirement switch
    {
        NumericProofRequirement numericReq => numericReq.Subject,
        PresenceProofRequirement presenceReq => presenceReq.Subject,
        _ => null
    };
    if (subject is null) return false;

    var resolvedSubject = ResolveSubject(subject, obligation.Site);

    if (resolvedSubject is TypedFunctionCall functionCall
        && FunctionReturnSatisfies(functionCall, obligation.Requirement))
    {
        return true;
    }

    var attributeFieldName = GetFieldName(resolvedSubject);
    if (attributeFieldName is null) return false;

    if (!semantics.FieldsByName.TryGetValue(attributeFieldName, out var attributeField))
        return false;

    if (subject is SelfSubject { Accessor: FixedReturnAccessor { ReturnNonnegative: true } }
        && obligation.Requirement is NumericProofRequirement {
            Comparison: OperatorKind.GreaterThanOrEqual,
            Threshold: 0m
        })
    {
        return true;
    }

    foreach (var modifier in attributeField.Modifiers.Concat(attributeField.ImpliedModifiers))
    {
        var meta = Modifiers.GetMeta(modifier);

        foreach (var satisfaction in meta.ProofSatisfactions)
        {
            if (SatisfactionCovers(satisfaction, obligation.Requirement))
                return true;
        }
    }

    // PresenceProofRequirement: check declaration-attached presence carrier
    if (obligation.Requirement is PresenceProofRequirement)
    {
        if (attributeField.Presence is DeclaredPresenceMeta.Guaranteed guaranteed)
        {
            return guaranteed.ProofSatisfactions
                .Any(s => s.RequirementKind == ProofRequirementKind.Presence);
        }
    }

    return false;
}

// FunctionReturnSatisfies: for NumericProofRequirement(>=, 0), resolve the matched
// overload and return overload.ReturnNonnegative.
// FixedReturnAccessor.ReturnNonnegative: for SelfSubject(accessor) obligations,
// a true flag discharges NumericProofRequirement(>=, 0) before modifier lookup.
// SatisfactionCovers: checks whether a ProofSatisfaction entry covers a requirement.
// For NumericProofRequirement: satisfaction.RequirementKind == Numeric
//   AND satisfaction.Comparison subsumes requirement.Comparison at satisfaction.Bound.
// For PresenceProofRequirement: satisfaction.RequirementKind == Presence.
// Subsumption: positive (>, 0) covers both (!=, 0) and (>=, 0).
// The subsumption logic mirrors GuardSubsumes (Strategy 3) but reads from catalog
// metadata rather than from a guard expression.
```

> **✅ Resolved (CC#5 → PE-G2) — ValueModifierMeta.ProofSatisfactions is now canonical**
> `ProofDischarge` has been renamed to `ProofSatisfaction` — a full DU with 5 subtypes (Numeric, Presence, Dimension, Modifier, QualifierCompatibility) plus 3 supporting DUs (SatisfactionProjection, NumericBoundSource, DimensionSource). `ProofSatisfaction[]` is carried on `ValueModifierMeta` for numeric modifiers. Presence proof reads `TypedField.Presence` (new `DeclaredPresenceMeta` carrier). Dimension and qualifier-compatibility proof read `TypedField.DeclaredQualifiers` (new `DeclaredQualifierMeta` carrier). Strategy 2 consumes all three carrier surfaces.
> *Resolved: 2026-05-06 (CC#5), redesigned 2026-05-08 (PE-G2 locked)*

### Carrier Types

The proof engine reads positive carrier facts from three declaration-attached metadata surfaces. Each carrier is populated by the type checker at compile time — the proof engine reads them, never computes them.

#### DeclaredPresenceMeta

**Location:** `src/Precept/Language/DeclaredPresence.cs`

Normalizes every `TypedField` and `TypedArg` into one of:
- `DeclaredPresenceMeta.Guaranteed` — carries `ProofSatisfaction.Presence()`
- `DeclaredPresenceMeta.Optional` — carries no satisfactions

The type checker writes this at compile time based on whether the `optional` modifier is present. The proof engine reads `field.Presence` and sees a POSITIVE fact — never absence-checks modifiers directly.

**Note:** `notempty` does NOT satisfy presence. An optional `notempty` field may still be absent.

#### DeclaredQualifierMeta

**Location:** `src/Precept/Language/DeclaredQualifierMeta.cs`

A DU with 8 subtypes representing all qualifier axes:
- `Currency(string CurrencyCode, QualifierOrigin Origin)`
- `Unit(string UnitCode, string DimensionName, QualifierOrigin Origin)`
- `Dimension(string DimensionName, QualifierOrigin Origin)`
- `FromCurrency(string CurrencyCode, QualifierOrigin Origin)`
- `ToCurrency(string CurrencyCode, QualifierOrigin Origin)`
- `Timezone(string TimezoneId, QualifierOrigin Origin)`
- `TemporalDimension(PeriodDimension Value, QualifierOrigin Origin)`
- `TemporalUnit(string UnitName, PeriodDimension DerivedDimension, QualifierOrigin Origin)`

`QualifierOrigin` enum: `Explicit` | `Derived` | `Baseline`

**Normalization rules:**
- `period in 'days'` → `TemporalUnit("days", Date)` + derived `TemporalDimension(Date, Origin: Derived)`
- Unqualified `period` → baseline `TemporalDimension(Any, Origin: Baseline)`
- `TemporalDimension(Any)` satisfies Dimension proof but NOT QualifierCompatibility

**Strategy integration:**
- Strategy 2 reads `field.DeclaredQualifiers` for `DimensionProofRequirement` — finds a `TemporalDimension` entry and checks dimension compatibility.
- Strategy 5 reads `field.DeclaredQualifiers` for `QualifierCompatibilityProofRequirement` — compares two fields' qualifier entries on the requested axis.

#### ValueModifierMeta.ProofSatisfactions

**Location:** `src/Precept/Language/Modifier.cs` (property on `ValueModifierMeta`)

`ProofSatisfaction[]` array populated on 10 modifier catalog entries:

| Modifier | ProofSatisfaction entries |
|---|---|
| `notempty` | `Numeric(Accessor("length"), GreaterThan, Constant(0))`, `Numeric(Accessor("count"), GreaterThan, Constant(0))` |
| `min(N)` | `Numeric(SelfValue, GreaterThanOrEqual, DeclarationValue)` |
| `max(N)` | `Numeric(SelfValue, LessThanOrEqual, DeclarationValue)` |
| `positive` | `Numeric(SelfValue, GreaterThan, Constant(0))` |
| `nonnegative` | `Numeric(SelfValue, GreaterThanOrEqual, Constant(0))` |
| `nonzero` | `Numeric(SelfValue, NotEquals, Constant(0))` |
| `minlength(N)` | `Numeric(Accessor("length"), GreaterThanOrEqual, DeclarationValue)` |
| `maxlength(N)` | `Numeric(Accessor("length"), LessThanOrEqual, DeclarationValue)` |
| `mincount(N)` | `Numeric(Accessor("count"), GreaterThanOrEqual, DeclarationValue)` |
| `maxcount(N)` | `Numeric(Accessor("count"), LessThanOrEqual, DeclarationValue)` |

**Effective modifiers note:** The proof engine must read effective modifiers = declared modifiers + `TypeMeta.ImpliedModifiers`. That is how `timezone`, `currency`, `unitofmeasure`, and `dimension` types inherit `notempty` proof facts without duplicating them on `TypeMeta`.

#### Strategy 2 → Carrier Dispatch Summary

Strategy 2 dispatches across three carrier surfaces by requirement kind:

| Requirement Kind | Carrier | Read Path |
|---|---|---|
| `NumericProofRequirement` | `FunctionOverload.ReturnNonnegative` | If the resolved subject is a `TypedFunctionCall`, check the matched overload's `ReturnNonnegative` flag |
| `NumericProofRequirement` | `FixedReturnAccessor.ReturnNonnegative` | If the requirement subject is `SelfSubject(accessor)` and the accessor flag is true, discharge `>= 0` trivially |
| `NumericProofRequirement` | `ValueModifierMeta.ProofSatisfactions` | Otherwise walk the field's effective modifiers and check each satisfaction entry |
| `PresenceProofRequirement` | `DeclaredPresenceMeta` | Read `field.Presence`, check for `Guaranteed` subtype |
| `DimensionProofRequirement` | `DeclaredQualifierMeta` | Read `field.DeclaredQualifiers`, find `TemporalDimension` entry |
| `ModifierRequirement` | Direct membership | `field.Modifiers.Contains(required)` — no carrier metadata |

#### Strategy 5 → Carrier Dispatch

Strategy 5 reads `DeclaredQualifierMeta` entries from both subjects:

| Requirement Kind | Carrier | Read Path |
|---|---|---|
| `QualifierCompatibilityProofRequirement` | `DeclaredQualifierMeta` | Read both subjects' `DeclaredQualifiers`, find entries on requested axis, compare values |

#### Strategy 3: Guard-in-Path Proof

**When it applies:** An enclosing guard expression in the same transition row establishes a sufficient constraint.

**How it works:** Parse the guard expression into a simple constraint form (field comparisons, presence checks); check whether the constraint implies the required condition for the subject.

**Examples:**

```precept
on Submitted when count(Items) > 0
    set FirstItem = first(Items)   // ← guard discharges non-empty requirement
```

```precept
on Calculate when Divisor != 0
    set Rate = Total / Divisor     // ← guard discharges division-by-zero
```

```precept
on Process when Value >= 0
    set Root = sqrt(Value)         // ← guard discharges non-negative requirement
```

**Guard pattern recognition:**

| Guard Pattern | Discharges |
|---|---|
| `field > 0` | `NumericProofRequirement(field, >, 0)` and `NumericProofRequirement(field, !=, 0)` |
| `field >= 0` | `NumericProofRequirement(field, >=, 0)` |
| `field != 0` | `NumericProofRequirement(field, !=, 0)` |
| `field < 0` | `NumericProofRequirement(field, <, 0)` (also implies `!= 0`) |
| `count(collection) > 0` | Collection non-empty requirement |
| `collection.count > 0` | Collection non-empty requirement |
| `field is set` | `PresenceProofRequirement(field)` |

**Constraint subsumption:** `> 0` subsumes `!= 0` and `>= 0`. `< 0` subsumes `!= 0`. The proof engine checks subsumption, not just exact matches.

**Discharge predicate pseudocode:**

```csharp
// Strategy 3: Guard-in-Path Proof — Discharge Predicate
// Input: ProofObligation (from Pass 1)
// Reads: TypedTransitionRow.Guard (the enclosing when clause),
//        obligation.Requirement (NumericProofRequirement or PresenceProofRequirement)
// Scope: Obligations where the enclosing guard directly constrains the proof subject
//        (field or collection named in the guard matches the obligation's subject)

bool TryGuardInPathProof(ProofObligation obligation, SemanticIndex semantics)
{
    // Read context from obligation — set at instantiation time (O(1), not post-hoc search)
    if (obligation.Context is not TransitionRowContext trc and not StateHookContext shc)
        return false;  // only transition rows and state hooks have guards
    var guard = obligation.Context switch
    {
        TransitionRowContext t => t.Row.Guard,
        StateHookContext s => s.Hook.Guard,
        _ => null
    };
    if (guard is null) return false;

    // 2. Decompose the guard into simple constraint forms
    var guardConstraints = ExtractGuardConstraints(guard);

    // 3. For each guard constraint, check if it covers the requirement
    foreach (var guardConstraint in guardConstraints)
    {
        if (obligation.Requirement is NumericProofRequirement numeric)
        {
            if (GuardSubsumes(guardConstraint, numeric, obligation.Site))
                return true;
        }
        else if (obligation.Requirement is PresenceProofRequirement presence)
        {
            if (guardConstraint.Field == GetFieldName(presence.Subject, obligation.Site)
                && guardConstraint.IsPresenceCheck)
                return true;
        }
    }

    return false;  // no guard constraint covers the requirement
}

bool GuardSubsumes(GuardConstraint guard, NumericProofRequirement requirement, TypedExpression site)
{
    if (guard.Field != GetFieldName(requirement.Subject, site)) return false;

    return (guard.Comparison, requirement.Comparison) switch
    {
        // > 0 guard subsumes != 0 and >= 0 requirements
        (OperatorKind.GreaterThan, OperatorKind.NotEquals)
            when guard.Value == 0 && requirement.Threshold == 0 => true,
        (OperatorKind.GreaterThan, OperatorKind.GreaterThanOrEqual)
            when guard.Value >= requirement.Threshold => true,

        // >= N guard subsumes >= M when N >= M
        (OperatorKind.GreaterThanOrEqual, OperatorKind.GreaterThanOrEqual)
            when guard.Value >= requirement.Threshold => true,

        // < 0 subsumes != 0
        (OperatorKind.LessThan, OperatorKind.NotEquals)
            when guard.Value == 0 && requirement.Threshold == 0 => true,

        // Exact match
        _ when guard.Comparison == requirement.Comparison
            && guard.Value == requirement.Threshold => true,

        _ => false
    };
    // Passes if the guard establishes a range that implies the requirement.
    // Key: the guard must name the SAME field as the obligation's subject.
}
```

#### `ExtractGuardConstraints` Specification (PE-G10)

```csharp
// Internal to ProofEngine — not part of public API
record GuardConstraint(
    string Field,                    // field name
    OperatorKind Comparison,         // comparison operator
    decimal? Value,                  // literal threshold (null for presence checks)
    bool IsPresenceCheck             // true for "field is set" patterns
);
```

**Decomposition rules:**

| Guard Pattern | Decomposition |
|---|---|
| `TypedBinaryOp(And, left, right)` | Recurse into both `left` and `right` — each produces independent constraints. All AND-conjuncts contribute constraints because ALL conjuncts must be true when the guard passes. |
| `TypedBinaryOp(Or, left, right)` | **Do NOT decompose.** Neither disjunct is guaranteed true when the guard passes — the guard only guarantees one of them. Return empty for this branch. |
| `TypedBinaryOp(comparison, TypedFieldRef, TypedLiteral)` | Yield one `GuardConstraint(field.FieldName, comparison, literal.Value)` |
| `TypedBinaryOp(comparison, TypedLiteral, TypedFieldRef)` | Yield one `GuardConstraint(field.FieldName, InvertOp(comparison), literal.Value)`. Example: `0 < X` → `GuardConstraint(X, GreaterThan, 0)` |
| `TypedBinaryOp(NotEquals, TypedFieldRef, TypedLiteral)` | Yield `GuardConstraint(field, NotEquals, literal.Value)` |
| `TypedPostfixOp(TypedFieldRef, IsNegated: false)` | Yield `GuardConstraint(field, _, null, IsPresenceCheck: true)` — "`field is set`" |
| `TypedPostfixOp(TypedFieldRef, IsNegated: true)` | **Do NOT yield.** "`field is not set`" does not establish a positive constraint usable for proof. |
| `TypedFunctionCall(Count, [TypedFieldRef])` in comparison | Recognize `count(collection) > 0` pattern: yield `GuardConstraint(field, GreaterThan, 0)` |
| `TypedMemberAccess(TypedFieldRef, CountAccessor)` in comparison | Recognize `collection.count > 0` pattern: same as above |
| `TypedUnaryOp(Not, inner)` | Attempt to invert simple comparisons: `not (X == 0)` → `GuardConstraint(X, NotEquals, 0)`. For complex inner expressions, return empty. |
| `TypedConditional` | **Do NOT decompose.** Conditional expressions in guards are too complex for pattern matching. |
| `TypedQuantifier` | **Do NOT decompose.** Quantifiers are beyond the bounded strategy scope. |
| Any other expression | Return empty — not a recognized constraint form. |

**Operator inversion table** (for `InvertOp`, used when the literal is on the left):

| Original | Inverted |
|---|---|
| `GreaterThan` | `LessThan` |
| `LessThan` | `GreaterThan` |
| `GreaterThanOrEqual` | `LessThanOrEqual` |
| `LessThanOrEqual` | `GreaterThanOrEqual` |
| `Equals` | `Equals` (symmetric) |
| `NotEquals` | `NotEquals` (symmetric) |

**Negation inversion table** (for `not (X op Y)` → `X inverted_op Y`):

| Original | Negated |
|---|---|
| `Equals` | `NotEquals` |
| `NotEquals` | `Equals` |
| `GreaterThan` | `LessThanOrEqual` |
| `LessThanOrEqual` | `GreaterThan` |
| `LessThan` | `GreaterThanOrEqual` |
| `GreaterThanOrEqual` | `LessThan` |

**Rationale:** AND-decomposition is safe because all conjuncts are true when the guard passes. OR-decomposition is unsafe because only one disjunct is guaranteed. Negation inversion is safe for simple comparisons but not for complex expressions.

#### Strategy 4: Straightforward Flow Narrowing

**When it applies:** A guard earlier in the same transition row establishes a field value range via a comparison or range check. More constrained than Strategy 3 — applies only to simple relational constraints in the same row.

**How it differs from Strategy 3:** Strategy 3 handles enclosing guards that protect the entire expression scope. Strategy 4 handles guards that narrow a field's value range for subsequent expressions in the same transition row's action chain.

**Example:**

```precept
on AdjustInventory when Quantity > ReorderPoint
    set Deficit = ReorderPoint - Quantity   // ← Strategy 4: guard establishes Quantity > ReorderPoint
```

This strategy handles the case where a guard establishes a *relative* constraint between two fields, and the action chain uses that relationship.

**Implementation note:** Strategy 4 is the narrowest and most specialized. It applies only when:
1. The guard is in the same transition row
2. The guard establishes a simple relational constraint
3. The proof site references the constrained field

**Discharge predicate pseudocode:**

```csharp
// Strategy 4: Straightforward Flow Narrowing — Discharge Predicate
// Input: ProofObligation (from Pass 1)
// Reads: TypedTransitionRow.Guard (same-row guard),
//        obligation.Site (TypedBinaryOp expression involving two fields)
// Scope: Obligations where the guard establishes a relational invariant between
//        two non-literal operands, and the obligation involves an expression
//        over both of those operands.
// Key discriminator vs Strategy 3: Strategy 3 fires when the guard names the
// proof subject directly (field vs literal). Strategy 4 fires when the guard
// names TWO fields in relation (field vs field) and the obligation is about
// an expression combining both.

bool TryFlowNarrowingProof(ProofObligation obligation, SemanticIndex semantics)
{
    // Read context from obligation — set at instantiation time (O(1), not post-hoc search)
    if (obligation.Context is not TransitionRowContext trc and not StateHookContext shc)
        return false;  // only transition rows and state hooks have guards
    var guard = obligation.Context switch
    {
        TransitionRowContext t => t.Row.Guard,
        StateHookContext s => s.Hook.Guard,
        _ => null
    };
    if (guard is null) return false;

    // 2. Decompose the guard — look for field-vs-field comparisons only
    var relationalGuards = ExtractFieldToFieldConstraints(guard);
    if (relationalGuards.IsEmpty) return false;

    // 3. Check if the obligation's expression site uses both fields from a guard
    if (obligation.Site is not TypedBinaryOp binaryOp) return false;
    if (obligation.Requirement is not NumericProofRequirement numeric) return false;

    var leftField = GetFieldName(binaryOp.Left);
    var rightField = GetFieldName(binaryOp.Right);
    if (leftField is null || rightField is null) return false;

    // 4. For each relational guard, check if it establishes a constraint
    //    that makes the binary operation safe
    foreach (var relationalGuard in relationalGuards)
    {
        if (!InvolvesFields(relationalGuard, leftField, rightField)) continue;

        // 5. Check if the guard's established relation implies the obligation
        if (GuardRelationImpliesObligation(relationalGuard, binaryOp, numeric))
            return true;
    }

    return false;
}
```

#### Complete `GuardRelationImpliesObligation` Triple Table (PE-G14)

The function takes `(guard.Op, expr.Op, requirement)` and returns whether the guard's established relation implies the obligation. **Scope: subtraction expressions only (`A - B`).** Division is NOT covered — proving `A != 0` from `A > B` requires knowledge of B's sign, beyond bounded flow narrowing.

**Guard form:** `A guard.Op B` (two field references, no literals)
**Expression form:** `A expr.Op B` (subtraction only)
**Requirement form:** `NumericProofRequirement(subject, req.Op, req.Threshold)`

| Guard | Expression | Requirement | Result | Reasoning |
|---|---|---|---|---|
| `A > B` | `A - B` | `result > 0` | ✅ | A > B → A - B > 0 |
| `A > B` | `A - B` | `result >= 0` | ✅ | A > B → A - B > 0 ≥ 0 |
| `A > B` | `A - B` | `result != 0` | ✅ | A > B → A - B > 0 ≠ 0 |
| `A >= B` | `A - B` | `result >= 0` | ✅ | A >= B → A - B >= 0 |
| `A >= B` | `A - B` | `result != 0` | ❌ | A >= B allows A == B → result == 0 |
| `A > B` | `B / A` | `divisor != 0` | ❌ | A > B doesn't prove A != 0 (both could be negative) |
| `A != B` | `A - B` | `result != 0` | ✅ | A != B → A - B != 0 |
| `A < B` | `B - A` | `result > 0` | ✅ | A < B → B - A > 0 |
| `A < B` | `B - A` | `result >= 0` | ✅ | A < B → B - A > 0 ≥ 0 |
| `A < B` | `B - A` | `result != 0` | ✅ | A < B → B - A > 0 ≠ 0 |
| `A <= B` | `B - A` | `result >= 0` | ✅ | A <= B → B - A >= 0 |
| `A > 0` | — | — | ❌ | **Strategy 3 case** — A > literal, not A > field |

**Supporting types:**

```csharp
record FieldToFieldConstraint(string LeftField, OperatorKind Comparison, string RightField);
```

**Implementation pattern:**

```csharp
static bool GuardRelationImpliesObligation(
    FieldToFieldConstraint guard,
    TypedBinaryOp expr,
    NumericProofRequirement requirement)
{
    var (gLeft, gOp, gRight) = (guard.LeftField, guard.Comparison, guard.RightField);
    var exprLeft = GetFieldName(expr.Left);
    var exprRight = GetFieldName(expr.Right);

    if (exprLeft is null || exprRight is null) return false;

    bool sameOrder = exprLeft == gLeft && exprRight == gRight;
    bool reversed = exprLeft == gRight && exprRight == gLeft;
    if (!sameOrder && !reversed) return false;

    if (expr.ResolvedOp is not OperationKind subtraction
        || !IsSubtractionOp(subtraction)) return false;

    var effectiveOp = sameOrder ? gOp : InvertOp(gOp);

    return (effectiveOp, requirement.Comparison) switch
    {
        (OperatorKind.GreaterThan, OperatorKind.GreaterThan) when requirement.Threshold == 0 => true,
        (OperatorKind.GreaterThan, OperatorKind.GreaterThanOrEqual) when requirement.Threshold <= 0 => true,
        (OperatorKind.GreaterThan, OperatorKind.NotEquals) when requirement.Threshold == 0 => true,
        (OperatorKind.GreaterThanOrEqual, OperatorKind.GreaterThanOrEqual) when requirement.Threshold <= 0 => true,
        (OperatorKind.LessThan, OperatorKind.LessThan) when requirement.Threshold == 0 => true,
        (OperatorKind.LessThan, OperatorKind.NotEquals) when requirement.Threshold == 0 => true,
        (OperatorKind.LessThanOrEqual, OperatorKind.LessThanOrEqual) when requirement.Threshold <= 0 => true,
        (OperatorKind.NotEquals, OperatorKind.NotEquals) when requirement.Threshold == 0 => true,
        _ => false
    };
}
```

> **Strategy 3 vs Strategy 4 boundary (resolved):**
> - **Strategy 3 (guard-in-path):** The `when <guard>` clause protects all actions in the row. The guard must directly constrain the proof subject — the field (or collection) being checked for the required property appears as the operand in the guard comparison (e.g., `when Divisor != 0` discharges a `divisor != 0` proof obligation for `A / Divisor`). Strategy 3 applies when the subject is a *named field or arg* and the guard is a *simple comparison or presence check* on that subject.
> - **Strategy 4 (flow-narrowing):** The guard establishes a *relational invariant between two or more fields* (e.g., `when Quantity > ReorderPoint` establishes `Quantity > ReorderPoint`). Strategy 4 discharges obligations where the proof site involves an expression over both constrained fields and the established relation implies the obligation (e.g., `ReorderPoint - Quantity` is safe because the guard proves `Quantity > ReorderPoint`, so the result is negative, avoiding a specific overflow/underflow concern). Strategy 4 applies only when the guard is a *binary comparison between two non-literal operands* and the obligation is an arithmetic result-range obligation on an expression involving those operands.
> - **Key discriminator:** If the guard directly names the proof subject (`when X > 0` for an obligation about `X`), use Strategy 3. If the guard names two fields in relation (`when A > B`) and the obligation is about an expression using both, use Strategy 4. Strategy 4 does not fire for simple per-field presence or range checks — those are Strategy 3.

#### Strategy 5: Qualifier Compatibility Proof

**When it applies:** The obligation is a `QualifierCompatibilityProofRequirement` — the only dual-subject requirement kind.

**How it works:** Resolve both subjects to their `DeclaredQualifierMeta` entries on the specified `QualifierAxis`. If both resolve to the same qualifier value, discharge. If either is unqualified or the values differ, the obligation remains `Unresolved`.

**Examples:**
- `quantity of 'kg' + quantity of 'kg'` → both Unit qualifiers match → discharged
- `quantity of 'kg' + quantity of 'miles'` → Unit qualifiers differ → `Unresolved` → diagnostic
- `quantity + quantity` (unqualified) → cannot prove → `Unresolved` → diagnostic
- `TemporalDimension(Any)` satisfies Dimension proof but does NOT satisfy QualifierCompatibility — two `Any` fields are not provably compatible

**Declaration carrier dependency:** Strategy 5 reads `TypedField.DeclaredQualifiers` (new `DeclaredQualifierMeta` carrier entries populated by the type checker). Each `DeclaredQualifierMeta` subtype carries its axis and concrete value. The proof engine compares two fields' qualifier entries on the requested axis — it never inspects raw type annotations or parser output.

**Discharge predicate pseudocode:**

```csharp
// Strategy 5: Qualifier Compatibility Proof — Discharge Predicate
// Input: ProofObligation with QualifierCompatibilityProofRequirement
// Reads: Both subjects' resolved qualifier bindings from SemanticIndex
// Scope: Dual-subject obligations where two operands must share a qualifier value

bool TryQualifierCompatibilityProof(ProofObligation obligation, SemanticIndex semantics)
{
    if (obligation.Requirement is not QualifierCompatibilityProofRequirement qcReq)
        return false;

    // 1. Resolve both subjects to their qualifier bindings
    var leftQualifier = ResolveQualifierOnAxis(qcReq.LeftSubject, qcReq.Axis, obligation.Site, semantics);
    var rightQualifier = ResolveQualifierOnAxis(qcReq.RightSubject, qcReq.Axis, obligation.Site, semantics);

    // 2. If either qualifier is unresolved (unqualified field), cannot prove — Unresolved
    if (leftQualifier is null || rightQualifier is null)
        return false;

    // 3. Compare: both must have the same qualifier value on the specified axis
    return leftQualifier == rightQualifier;
}
```

**Dependency:** Requires qualifier resolution in the TypeChecker (currently Slice 2+ future work). Until qualifier resolution ships, all `QualifierCompatibilityProofRequirement` obligations produce `Unresolved` — the correct conservative behavior.

### Proof/Fault Chain

The proof engine threads obligations through a chain that connects compile-time analysis to runtime defense:

```text
┌──────────────────┐      ┌────────────────────┐      ┌────────────────────┐
│ Catalog metadata │  →   │  ProofRequirement  │  →   │   ProofObligation  │
│ (BinaryOpMeta)   │      │  (stamped by TC)   │      │  (instantiated)    │
└──────────────────┘      └────────────────────┘      └────────────────────┘
                                                                │
                                    ┌───────────────────────────┴───────────────────────────┐
                                    ▼                                                       ▼
                          ┌─────────────────┐                                    ┌─────────────────┐
                          │   Disposition:  │                                    │   Disposition:  │
                          │     Proved      │                                    │   Unresolved    │
                          └─────────────────┘                                    └─────────────────┘
                                    │                                                       │
                                    ▼                                                       ▼
                            No runtime check                                     ┌─────────────────┐
                               needed                                            │ DiagnosticCode  │
                                                                                 │ (authoring-time)│
                                                                                 └─────────────────┘
                                                                                           │
                                                                                           ▼
                                                                                 ┌─────────────────┐
                                                                                 │   FaultCode     │
                                                                                 │ (runtime)       │
                                                                                 └─────────────────┘
                                                                                           │
                                                                                           ▼
                                                                                 ┌─────────────────┐
                                                                                 │ FaultSiteLink   │
                                                                                 │ → Precept Builder│
                                                                                 └─────────────────┘
                                                                                           │
                                                                                           ▼
                                                                                 ┌─────────────────┐
                                                                                 │FaultSiteDescriptor│
                                                                                 │ (defense-in-depth)│
                                                                                 └─────────────────┘
```

**Key invariants:**

1. Every `FaultCode` carries a `[StaticallyPreventable(DiagnosticCode)]` attribute linking it to its prevention diagnostic
2. Roslyn analyzers (PRECEPT0001, PRECEPT0002) enforce that every evaluator failure path routes through a classified `FaultCode`
3. Every unresolved, non-error-tainted obligation produces both a diagnostic (for the author) and a `FaultSiteLink` (for the Precept Builder)

### Constraint Influence Analysis

For each rule and ensure constraint, the proof engine records which fields and event args the constraint expression references. **The ProofEngine reads `SemanticIndex.ConstraintRefs` (populated by the TypeChecker) to build `ConstraintInfluenceEntry` records. It does NOT re-walk expression trees.** The TypeChecker already traverses constraint expressions during type checking and records field/arg references in `ConstraintFieldRefs` entries. The proof engine projects these into the richer `ConstraintInfluenceEntry` shape, enriching arg references with event-qualified identity.

```csharp
// The proof engine reads SemanticIndex.ConstraintRefs — NOT expression trees.
// ConstraintFieldRefs (from TypeChecker):
//   ConstraintIdentity, ImmutableArray<string> ReferencedFields, ImmutableArray<string> ReferencedArgs
// ConstraintInfluenceEntry (proof engine output):
//   ConstraintIdentity, ImmutableArray<string> ReferencedFields, ImmutableArray<EventArgReference> ReferencedArgs
//
// The shape difference: ConstraintFieldRefs carries arg names as bare strings;
// ConstraintInfluenceEntry carries EventArgReference(EventName, ArgName).
// The proof engine resolves arg names to event-qualified references using
// SemanticIndex.EventsByName to find the owning event for each arg.

ImmutableArray<ConstraintInfluenceEntry> ProjectConstraintInfluence(SemanticIndex semantics)
{
    var entries = new List<ConstraintInfluenceEntry>();

    foreach (var cfr in semantics.ConstraintRefs)
    {
        // Enrich bare arg names → EventArgReference using the event index
        var qualifiedArgs = cfr.ReferencedArgs
            .Select(argName => ResolveArgToEvent(argName, cfr.ConstraintIdentity, semantics))
            .ToImmutableArray();

        entries.Add(new ConstraintInfluenceEntry(
            cfr.ConstraintIdentity,
            cfr.ReferencedFields,
            qualifiedArgs));
    }

    return entries.ToImmutableArray();
}
```

> **Design note:** `SemanticIndex.ConstraintRefs` carries `ImmutableArray<string> ReferencedArgs` (bare arg names), while `ConstraintInfluenceEntry.ReferencedArgs` carries `ImmutableArray<EventArgReference>` (event-qualified). The proof engine resolves this by looking up which event owns each arg name via `SemanticIndex.EventsByName`. If a future TypeChecker change populates `ConstraintFieldRefs` with event-qualified arg references directly, the proof engine projection simplifies to a 1:1 copy.

The Precept Builder consumes these entries and reorganizes them into a `ConstraintInfluenceMap` — a precomputed artifact that enables AI agents to reason causally: "which fields affect which constraints?"

### Initial-State Satisfiability

The proof engine verifies structural initial-state satisfiability using bounded constant folding over declared defaults and initial-state ensures.

**Algorithm: `CheckInitialStateSatisfiability`**

**Input:** `SemanticIndex semantics`

**Output:** `ImmutableArray<InitialStateSatisfiabilityResult>`

**Step 1 — Find the initial state:**
```csharp
var initialState = semantics.States.FirstOrDefault(s =>
    s.Modifiers.Contains(ModifierKind.Initial));
if (initialState is null)
    return ImmutableArray<InitialStateSatisfiabilityResult>.Empty;
    // Stateless precepts have no initial state — nothing to check.
```

**Step 2 — Collect applicable constraints:**

Collect all `TypedEnsure` entries anchored to the initial state with `ConstraintKind.StateResident` (the `in` anchor). These are the constraints that must hold while the entity is in the initial state.

```csharp
var initialEnsures = semantics.EnsuresByState.TryGetValue(initialState.Name, out var ensures)
    ? ensures.Where(e => e.Kind == ConstraintKind.StateResident)
    : Enumerable.Empty<TypedEnsure>();
```

**Why only `StateResident` (`in`)?** `StateEntry` constraints (`to`) fire on transition into the state — but the initial state is entered at creation time, not via a transition. `StateExit` constraints (`from`) are irrelevant at creation. `StateResident` constraints must hold for the entire duration of residency in a state — including the initial moment.

**Step 3 — Build the default value environment:**

For each field in `semantics.Fields`, determine its default value:

| Field Condition | Default Value |
|---|---|
| Has `DefaultExpression` that is `TypedLiteral` | The literal's `Value` |
| Has `DefaultExpression` that is NOT `TypedLiteral` | **Unfoldable** — mark as unknown |
| Has `ComputedExpression` (`IsComputed = true`) | **Unfoldable** — computed fields depend on other fields |
| Is `IsOptional = true` with no default | `null` (not set) |
| Is numeric type with no default | `0` (the CLR default for numeric types) |
| Is `string` type with no default | `""` (empty string) |
| Is `boolean` type with no default | `false` |
| Is collection type with no default | Empty collection (count = 0) |
| Is `date`/`datetime`/`period`/`duration`/`money`/`price`/`quantity` with no default | **Unfoldable** — no meaningful zero value |

```csharp
Dictionary<string, object?> defaults = new();
HashSet<string> unfoldable = new();

foreach (var field in semantics.Fields)
{
    if (field.DefaultExpression is TypedLiteral lit)
        defaults[field.Name] = lit.Value;
    else if (field.DefaultExpression is not null || field.IsComputed)
        unfoldable.Add(field.Name);
    else if (field.IsOptional)
        defaults[field.Name] = null;
    else
        defaults[field.Name] = GetTypeDefault(field.ResolvedType);
        // Returns 0m for numeric, "" for string, false for boolean,
        // empty for collections, or marks unfoldable for complex types.
}
```

**Step 4 — Fold each constraint condition:**

For each initial-state ensure, substitute `TypedFieldRef` nodes in the condition with their default values, then constant-fold.

```csharp
foreach (var ensure in initialEnsures)
{
    if (ensure.Guard is not null)
        continue; // Guarded ensures are skipped for initial-state satisfiability.

    var foldResult = ConstantFold(ensure.Condition, defaults, unfoldable);

    if (foldResult == FoldResult.False)
    {
        // The constraint is definitely unsatisfiable with defaults.
        // Emit DiagnosticCode.UnsatisfiableInitialState (115).
        violations.Add(new UnsatisfiedConstraint(
            EnsureIdentity(ensure.Kind, ensure.AnchorName, ensureIndex),
            FormatViolationReason(ensure, defaults)));
    }
    else if (foldResult == FoldResult.Unknown)
    {
        // Cannot determine — expression references unfoldable fields or uses
        // non-foldable operations. Conservative: treat as satisfiable.
    }
    // FoldResult.True → constraint is satisfied by defaults, no violation.
}
```

**Step 5 — `ConstantFold` specification:**

Bounded constant folding over `TypedExpression` trees. Supported operations:

| Expression Type | Fold Behavior |
|---|---|
| `TypedLiteral` | Return the literal value |
| `TypedFieldRef` | Substitute from `defaults` map; if field is in `unfoldable`, return `Unknown` |
| `TypedBinaryOp` | Fold both operands; if both are known values, evaluate the operation; otherwise `Unknown` |
| `TypedUnaryOp` | Fold operand; if known, evaluate; otherwise `Unknown` |
| `TypedFunctionCall` | **Not folded** — return `Unknown`. Function calls involve catalog-defined semantics too complex for constant folding. |
| `TypedConditional` | Fold condition; if `true`, fold `ThenBranch`; if `false`, fold `ElseBranch`; otherwise `Unknown` |
| `TypedMemberAccess` | **Not folded** — return `Unknown` |
| `TypedPostfixOp` | Fold operand; if operand is `null`, `is set` = `false`, `is not set` = `true`; otherwise `is set` = `true` |
| Any other | `Unknown` |

**Fold result type:**
```csharp
enum FoldResult { True, False, Unknown }
```

The constant folder operates on `object?` values and uses the same comparison semantics as the Precept expression language (decimal arithmetic, string equality, boolean logic).

**Step 6 — Initial event arguments are NOT considered.**

Initial event arguments are runtime values — they vary per instantiation. The satisfiability check verifies structural satisfiability: "given only declared defaults, can the entity be created in a valid state?" If the answer is "no," the precept has a structural defect regardless of what arguments are provided.

If the initial event's `set` actions assign fields that satisfy constraints, that's a runtime guarantee, not a compile-time one. The proof engine checks compile-time defaults only.

**Step 7 — Guarded ensures are skipped.**

If `ensure.Guard` is not null, the ensure is conditionally applied. The satisfiability check does not evaluate guards — they're runtime conditions. Guarded ensures are treated as vacuously satisfiable for initial-state checking purposes.

**Diagnostic:** If any initial-state ensure folds to `False`, the proof engine emits `DiagnosticCode.UnsatisfiableInitialState` (115).

**Rationale:** This algorithm is deliberately conservative — it only reports violations it can prove statically. Unfoldable fields, non-literal defaults, function calls, and guarded ensures all resolve to `Unknown` (conservative pass). This ensures zero false positives at the cost of potentially missing some true violations that would require deeper analysis.

### Collection Non-Empty Proof

Collection non-empty obligations arise from several sources:

| Source | Obligation |
|---|---|
| `first(collection)` | Collection must be non-empty |
| `last(collection)` | Collection must be non-empty |
| `collection.peek` | Queue/stack must be non-empty |
| `dequeue Collection` action | Queue must be non-empty |
| `pop Collection` action | Stack must be non-empty |

**Ownership (PE-G9):** The type checker owns collection safety diagnostics (`UnguardedCollectionAccess` code 63, `UnguardedCollectionMutation` code 64). Collection non-empty requirements are encoded as `NumericProofRequirement(SelfSubject(CollectionCountAccessor), GreaterThan, 0)` in the catalog. The proof engine processes them as ordinary numeric obligations through Strategies 1/2/3 — no special collection-specific logic or diagnostic codes are needed.

If the obligation is unresolved after all strategies, the `FaultSiteLink` maps to `FaultCode.CollectionEmptyOnAccess` or `CollectionEmptyOnMutation`, which are already linked to the type checker's diagnostics via `[StaticallyPreventable]`.

**Modifier-proof strategy:** The `notempty` modifier discharges collection non-empty requirements.

**Guard-in-path strategy:** `count(collection) > 0` or `collection.count > 0` guards discharge the requirement.

### ProofForwardingFact Consumption Contract

The proof engine consumes `ProofForwardingFact` entries from the `StateGraph` during Pass 1 (Obligation Instantiation). Each fact subtype has a specific consumption pattern:

| Fact Subtype | Consumption |
|---|---|
| `ReachabilityFact` | Unreachable-state facts suppress proof obligations on transitions originating from unreachable states — those transitions can never fire, so their proof requirements are vacuously satisfied. Reachable paths feed causal diagnostic explanations. |
| `DominancePathFact` | Records in the proof ledger for structural completeness. No additional diagnostic — the graph analyzer emits `RequiredStateDoesNotDominateTerminal` (111). Redundant diagnostics are suppressed. |
| `EventCoverageFact` | Records structural coverage facts for downstream consumption. The proof engine does NOT analyze guard completeness (value-space partitioning). Guard completeness would require solver-grade analysis; coverage facts are structural metadata only. |
| `TerminalCompletenessFact` | Records terminal completeness in the proof ledger for structural completeness reasoning. No additional diagnostics — the graph analyzer emits `UnreachableState` for each unreachable terminal. |
| `DeadEndStateFact` | Records dead-end states in the proof ledger as structural completeness failures. Dead-end states represent lifecycle traps — entities entering these states can never reach completion. The proof engine suppresses proof obligations on transitions originating FROM dead-end states to other dead-end states (those paths are already structurally broken). Transitions INTO dead-end states retain their obligations — those transitions are the ones the author likely needs to fix. No additional diagnostics — the graph analyzer emits `DeadEndState` (Warning, code 108) for each dead-end state. |

### Stateless Precept Handling (PE-G15)

The proof engine runs for ALL precepts, including stateless ones.

**Detection:** A stateless precept has `semantics.States.IsEmpty` (no state declarations). It uses `EventHandlers` instead of `TransitionRows`.

**Walk targets for stateless precepts:**

| Walk Target | Present? | Notes |
|---|---|---|
| `TransitionRows` | Empty | No state machine → no transition rows |
| `EventHandlers` | Non-empty | Actions carry `ProofRequirements` |
| `StateHooks` | Empty | No states → no state hooks |
| `Rules` | May be non-empty | Global invariant constraints apply |
| `Ensures` | Empty | No states → no state-anchored constraints |
| `Fields` | Non-empty | Fields exist; defaults relevant for computed expressions |

**Strategy applicability for stateless precepts:**

| Strategy | Applies? | Reason |
|---|---|---|
| Strategy 1 (Literal) | ✅ | Literal arguments in event handler actions |
| Strategy 2 (Declaration Attribute) | ✅ | Field modifiers apply regardless of state machine |
| Strategy 3 (Guard-in-Path) | ❌ | `TypedEventHandler` has no `Guard` field |
| Strategy 4 (Flow Narrowing) | ❌ | No guards → no relational constraints |
| Strategy 5 (Qualifier Compatibility) | ✅ | Qualifier comparison is field-level, not state-dependent |

**Initial-state satisfiability:** Skipped — no initial state. `CheckInitialStateSatisfiability` returns empty when `States.IsEmpty`.

**Constraint influence:** Rules (if any) are processed normally.

---

## 8. Dependencies and Integration Points

### Upstream Dependencies

| Component | What ProofEngine Consumes |
|---|---|
| **SemanticIndex** | `TypedBinaryOp`, `TypedFunctionCall`, `TypedMemberAccess` with `ProofRequirements` arrays; `TypedRule`, `TypedEnsure` for constraint influence; `TypedField` with `Modifiers`, `Presence` (`DeclaredPresenceMeta`), and `DeclaredQualifiers` (`DeclaredQualifierMeta[]`) for declaration-attribute proof |
| **StateGraph** | `ProofForwardingFact` entries for structural violations that have proof-level implications |
| **Catalog: Operations** | `BinaryOperationMeta.ProofRequirements` — division-by-zero, overflow hazards |
| **Catalog: Functions** | `FunctionOverload.ProofRequirements` — `sqrt()` non-negativity, `pow()` exponent constraints |
| **Catalog: Types** | `TypeAccessor.ProofRequirements` — collection `.first`, `.last`, `.peek` non-empty requirements |
| **Catalog: Actions** | `ActionMeta.ProofRequirements` — `dequeue`, `pop` non-empty requirements |
| **Catalog: Modifiers** | `ValueModifierMeta.ProofSatisfactions` — which modifiers satisfy which proof requirements |
| **Catalog: Faults** | `FaultCode` ↔ `DiagnosticCode` correspondence via `[StaticallyPreventable]` attribute |

### Downstream Consumers

| Consumer | What It Receives |
|---|---|
| **Compilation** | `ProofLedger` as one of the sealed pipeline artifacts; `Diagnostics` merged into final diagnostic stream |
| **Precept Builder** | `FaultSiteLinks` → plants `FaultSiteDescriptor` backstops; `ConstraintInfluence` → builds `ConstraintInfluenceMap`; `InitialStateResults` → gates runtime model production |
| **Language Server** | Proof diagnostics with semantic site attribution for Problems panel |
| **MCP compile output** | Obligation dispositions and constraint influence for AI tooling |

#### Builder Proof-Consumption Contract (PE-G11)

The Precept Builder (pipeline stage 7, after ProofEngine) consumes `ProofLedger` to produce runtime backstops and indices, and to enforce the initial-state gate:

**1. FaultSiteDescriptor Backstops (from `ProofLedger.FaultSiteLinks`)**

For each `FaultSiteLink` (one per unresolved, non-error-tainted obligation):

```csharp
// Builder transforms:
//   FaultSiteLink(obligation, faultCode, diagnosticCode, SourceSpan site)
// Into:
//   FaultSiteDescriptor(faultCode, diagnosticCode, site.StartLine)
```

**Builder rule:** Every `FaultSiteLink` produced for an unresolved, non-error-tainted obligation MUST produce a `FaultSiteDescriptor`. Error-tainted unresolved obligations are suppressed earlier in `ProofEngine` and therefore never reach the builder.

**2. ConstraintInfluenceMap (from `ProofLedger.ConstraintInfluence`)**

```csharp
public sealed record ConstraintInfluenceMap(
    FrozenDictionary<string, ImmutableArray<ConstraintIdentity>> FieldToConstraints,
    FrozenDictionary<(string EventName, string ArgName), ImmutableArray<ConstraintIdentity>> ArgToConstraints
);
```

Inverted index: given a field or arg that changed, which constraints might be affected? Used by the evaluator for constraint re-check and by AI agents for causal reasoning.

**3. InitialStateSatisfiabilityResults (from `ProofLedger.InitialStateResults`)**

The builder reads `InitialStateResults` and, if any `IsSatisfiable == false`, prevents the production of a runtime `Precept` model. An unsatisfiable initial state is a structural defect.

**4. ProofLedger.Obligations (diagnostic-only, not consumed by builder)**

| ProofLedger Field | Builder Consumption | Runtime Artifact |
|---|---|---|
| `Obligations` | Not consumed by builder | Diagnostic-only (LS, MCP) |
| `FaultSiteLinks` | Transforms to `FaultSiteDescriptor` per opcode | Runtime backstops |
| `ConstraintInfluence` | Inverts to `ConstraintInfluenceMap` | Runtime constraint re-check index |
| `InitialStateResults` | Gate: blocks runtime model if unsatisfiable | Compile-time gate |
| `Diagnostics` | Merged into `Compilation.Diagnostics` | Authoring-time feedback |

> **Note:** `docs/runtime/precept-builder.md` is the downstream builder spec and should treat this section as the canonical proof-consumption contract.

---

## 9. Failure Modes and Recovery

### Error Accumulation

The proof engine follows Precept's error-accumulation pipeline contract (see `docs/compiler/diagnostic-system.md`): every stage runs unconditionally, diagnostics accumulate without abandoning the analysis pass. See also `src/Precept/Compiler.cs` for the unconditional pipeline execution pattern and `docs/compiler/diagnostic-system.md § FaultCode → DiagnosticCode Chain` for the `[StaticallyPreventable]` contract.

| Condition | Behavior |
|---|---|
| Unresolved obligation (non-error-tainted) | Emit diagnostic, mark as `Unresolved`, continue to next obligation |
| Invalid ProofRequirement | Should not occur — catalog validation prevents malformed requirements |
| Missing catalog metadata | Should not occur — type checker stamps requirements from validated catalogs |
| Empty SemanticIndex | Produce empty `ProofLedger` with no obligations |
| All obligations proved | Produce `ProofLedger` with all dispositions `Proved`, no diagnostics |

### Diagnostic Message Formatting (PE-G12)

Template parameter population for proof-stage diagnostics:

| DiagnosticCode | Template | `{0}` | `{1}` | `{2}` | `{3}` |
|---|---|---|---|---|---|
| `DivisionByZero` (83) | `"Division by zero: '{0}' can be zero when {1}"` | Field name (from `GetFieldName`) | Context description | — | — |
| `SqrtOfNegative` (84) | `"sqrt() requires a non-negative value, but '{0}' can be negative when {1}"` | Field name | Context description | — | — |
| `UnsatisfiableGuard` (82) | `"The condition '{0}' on event '{1}' can never be true when {2}"` | Guard expression text | Event name | State name or `"any state"` | — |
| `UnprovedModifierRequirement` (112) | `"Field '{0}' must have modifier '{1}' but it is not declared{2}"` | Field name | Required modifier name | Context description | — |
| `UnprovedDimensionRequirement` (113) | `"Operand '{0}' requires {1} dimension but has {2}{3}"` | Operand field name | Required dimension | Actual dimension | Context |
| `UnprovedQualifierCompatibility` (114) | `"Operands '{0}' and '{1}' have incompatible {2} qualifiers{3}"` | Left operand | Right operand | Qualifier axis | Context |
| `UnsatisfiableInitialState` (115) | `"Initial state '{0}' cannot be satisfied: constraint '{1}' fails with default values"` | Initial state name | Constraint description | — | — |
| `UnprovedPresenceRequirement` (116) | `"Field '{0}' must be present but its presence cannot be guaranteed{1}"` | Field name | Context description | — | — |

**Context description format:** `"event '{EventName}' in state '{FromState}'"` for transition rows. `"state hook '{Scope}' on '{StateName}'"` for state hooks. `"event handler '{EventName}'"` for event handlers. `"rule at index {RuleIndex}"` for rules. `"ensure in {AnchorName}"` for ensures.

**Field name resolution:** Use `GetFieldName(requirement.Subject, obligation.Site)`. If the subject resolves to a field, use the field name. If resolution fails, use `"<unknown>"`.

### Partial Results

The proof engine always produces a complete `ProofLedger`, regardless of how many obligations fail:

```csharp
public static ProofLedger Prove(SemanticIndex semantics, StateGraph graph)
{
    var obligations = new List<ProofObligation>();
    var faultSiteLinks = new List<FaultSiteLink>();
    var diagnostics = new List<Diagnostic>();
    
    // Pass 1: Obligation Instantiation — explicit walk-target enumeration (PE-G4)
    // Walk each SemanticIndex member that carries proof-relevant expressions.
    // For each node with non-empty ProofRequirements, instantiate one ProofObligation
    // per requirement, tagging it with its ObligationContext (PE-G6).
    
    // Walk targets:
    // - TransitionRows[].Actions[] → TransitionRowContext(row)
    // - TransitionRows[].Actions[].InputExpression (recursive) → TransitionRowContext(row)
    // - EventHandlers[].Actions[] → EventHandlerContext(handler)
    // - StateHooks[].Actions[] → StateHookContext(hook)
    // - Rules[].Condition → ConstraintContext(RuleIdentity(i))
    // - Ensures[].Condition → ConstraintContext(EnsureIdentity(...))
    // - Fields[].DefaultExpression → FieldExpressionContext(field) [for satisfiability, not obligation walk]
    // - Fields[].ComputedExpression → FieldExpressionContext(field)
    
    var allObligations = CollectObligations(semantics);
    obligations.AddRange(allObligations);
    
    // Incorporate ProofForwardingFacts from StateGraph
    IncorporateForwardingFacts(graph.ProofFacts, obligations, semantics);
    
    // Pass 2: Obligation Discharge
    for (int i = 0; i < obligations.Count; i++)
    {
        var obligation = obligations[i];
        
        // Error-tainted obligation suppression (PE-G13):
        // If the obligation's site contains TypedErrorExpression, suppress proof
        // diagnostic — the type checker already reported the root cause.
        if (ContainsErrorExpression(obligation.Site))
        {
            obligations[i] = obligation with { Disposition = ProofDisposition.Unresolved };
            continue;  // No diagnostic, no fault link — error already reported upstream
        }
        
        var (disposition, strategy) = TryDischarge(obligation, semantics);
        obligations[i] = obligation with { Disposition = disposition, Strategy = strategy };
        
        if (disposition == ProofDisposition.Unresolved)
        {
            diagnostics.Add(CreateDiagnostic(obligation));
            faultSiteLinks.Add(CreateFaultSiteLink(obligation));
        }
    }
    
    // Always return complete ledger
    return new ProofLedger(
        obligations.ToImmutableArray(),
        faultSiteLinks.ToImmutableArray(),
        ProjectConstraintInfluence(semantics),
        CheckInitialStateSatisfiability(semantics),
        diagnostics.ToImmutableArray()
    );
}
```

### Upstream Error Handling (PE-G13)

The proof engine handles upstream errors with three rules:

1. **`TypedErrorExpression` nodes:** Skipped during Pass 1 obligation walk — `TypedErrorExpression` has `ResultType = TypeKind.Error` and no `ProofRequirements`. If encountered as a child of an obligation-bearing node (e.g., `TypedBinaryOp` with one `TypedErrorExpression` operand), the obligation is still instantiated but subject resolution will fail (returning `null`), causing all strategies to return `false`.

2. **Error-tainted obligation suppression:** No proof diagnostic is emitted for obligations whose site or resolved subject contains `TypedErrorExpression`. The type checker already emitted the root-cause diagnostic. Suppression prevents cascading diagnostics.

3. **Unconditional execution:** The proof engine runs regardless of upstream errors (matches `Compiler.cs` pattern — every stage runs unconditionally). It processes whatever is available and produces a complete `ProofLedger`.

**`ContainsErrorExpression` helper:**

```csharp
static bool ContainsErrorExpression(TypedExpression expr) => expr switch
{
    TypedErrorExpression => true,
    TypedBinaryOp bin => ContainsErrorExpression(bin.Left) || ContainsErrorExpression(bin.Right),
    TypedUnaryOp un => ContainsErrorExpression(un.Operand),
    TypedFunctionCall call => call.Arguments.Any(ContainsErrorExpression),
    TypedMemberAccess ma => ContainsErrorExpression(ma.Object),
    TypedConditional cond => ContainsErrorExpression(cond.Condition) ||
                             ContainsErrorExpression(cond.ThenBranch) ||
                             ContainsErrorExpression(cond.ElseBranch),
    _ => false
};
```

---

## 10. Contracts and Guarantees

### Obligation Completeness

**Contract:** Every `ProofRequirement` declared in any catalog entry for any used operation/function/accessor/action has a corresponding `ProofObligation` in the `ProofLedger`.

**Enforcement:** The type checker stamps requirements onto typed expressions and actions; the proof engine walks explicit `SemanticIndex` targets (`TransitionRows`, `EventHandlers`, `StateHooks`, `Rules`, `Ensures`, `Fields[].ComputedExpression`) and instantiates one obligation per non-empty `ProofRequirements` entry.

### Disposition Exhaustiveness

**Contract:** Every `ProofObligation` has a disposition: `Proved` (one of five strategies succeeded) or `Unresolved`.

**Enforcement:** The strategy dispatch loop is exhaustive — after trying all five strategies, the obligation is marked `Unresolved`; non-error-tainted obligations emit diagnostics, while error-tainted obligations are suppressed per PE-G13.

### Fault Chain Integrity

**Contract:** Every `FaultSiteLink` in the `ProofLedger` has a 1:1 correspondence with an `Unresolved` obligation and a `FaultCode` that carries `[StaticallyPreventable(DiagnosticCode)]`.

**Enforcement:**
1. `FaultSiteLink` is created only for `Unresolved` obligations
2. The `FaultCode` lookup uses the Roslyn-enforced `[StaticallyPreventable]` attribute
3. Roslyn analyzers PRECEPT0001/PRECEPT0002 enforce attribute presence at build time

### Determinism

**Contract:** Same input (`SemanticIndex` + `StateGraph`) always produces same output (`ProofLedger`).

**Enforcement:** No external solver, no non-deterministic algorithms, no timeouts. The five strategies are pure predicate functions.

### Catalog Correspondence

**Contract:** The proof engine does NOT maintain its own list of proof obligations. All obligations are declared in catalog metadata.

**Enforcement:** The proof engine reads `ProofRequirements` from typed expressions — it never constructs `ProofRequirement` instances directly.

---

## 11. Design Rationale and Decisions

### Decision 1: Five-Strategy Bounded Set vs. SMT Solver

**Decision:** The proof engine uses exactly five proof strategies — no general SMT solver.

**Rationale:**
- **Predictability:** Every proof attempt completes in bounded, deterministic time. No solver timeouts, no "unknown" results, no resource exhaustion.
- **Auditability:** Each strategy is a simple predicate function (~10–30 lines). Authors can understand exactly why an obligation was proved or not.
- **Zero external dependencies:** No Z3, no CVC5, no SAT solver. The proof engine is self-contained within the Precept runtime.
- **Coverage sufficiency:** The DSL expression language is intentionally constrained. Precept does not support arbitrary arithmetic, unbounded loops, or recursive definitions. The five strategies cover the realistic obligation space, including qualifier compatibility as a dedicated bounded case.

**Trade-off accepted:** The proof engine cannot discharge complex cross-field value relationships or inductive properties. This is acceptable because:
1. Such relationships are rare in business state machines
2. Authors can add guards to make obligations statically dischargeable
3. Defense-in-depth backstops catch any runtime failures

**Precedent:** The surveyed verification systems (SPARK Ada/GNATprove, Dafny, Liquid Haskell, CBMC) all depend on external solvers. Precept deliberately chooses bounded proof over general verification.

### Decision 2: ProofLedger Does NOT Cross Compile-Runtime Boundary

**Decision:** The `ProofLedger` is a compile-time artifact. Only `FaultSiteAnnotation` records (stamped on opcodes by the Precept Builder per CC#6) cross into runtime.

**Rationale:**
- **Separation of concerns:** Proof is analysis; runtime is execution. The runtime model should contain only what's needed for execution.
- **Memory efficiency:** Runtime instances don't carry proof baggage. A deployed precept includes only field descriptors, transition tables, and backstops.
- **Defense-in-depth:** `FaultSiteAnnotation` backstops are defense-in-depth — they should never fire if proof is correct. They exist for belt-and-suspenders safety, not as a proof continuation mechanism.

**What crosses:**
- `FaultSiteAnnotation` — nullable annotation on each opcode: fault code + preventing diagnostic code + source location (CC#6)
- Constraint execution plans (from evaluator, not proof engine)

**What does not cross:**
- `ProofObligation` records
- Strategy evidence
- Obligation coverage statistics

### Decision 3: Obligations Stamped by Type Checker, Not Identified by Proof Engine

**Decision:** The proof engine reads `ProofRequirement` arrays stamped onto typed expressions by the type checker. It does NOT identify what needs to be proved.

**Rationale:**
- **Catalog metadata determines obligations:** Adding a new operation with a division-by-zero hazard requires only a `NumericProofRequirement` catalog attachment — no proof engine code changes.
- **Type checker has the resolution context:** The type checker knows which `BinaryOperationMeta`, `FunctionOverload`, `TypeAccessor`, or `ActionMeta` was resolved. It stamps the requirements from that catalog entry.
- **Proof engine stays focused:** The proof engine's job is to *discharge* obligations, not *discover* them.

**Implementation consequence:** The proof engine never imports `Operations`, `Functions`, `Types`, or `Actions` catalogs directly. It reads only the `ProofRequirements` arrays on typed expressions.

### Decision 4: Constraint Influence as Proof Engine Output

**Decision:** The proof engine produces `ConstraintInfluenceEntry` records as part of its output, by reading `SemanticIndex.ConstraintRefs` (populated by the TypeChecker) and projecting them into the richer `ConstraintInfluenceEntry` shape.

**Rationale:**
- **No duplicate traversal:** The TypeChecker already walks constraint expressions during type checking and populates `SemanticIndex.ConstraintRefs` with field/arg references. The proof engine reads these — it does NOT re-walk expression trees.
- **Enrichment responsibility:** `ConstraintFieldRefs` carries bare arg name strings; `ConstraintInfluenceEntry` carries event-qualified `EventArgReference` records. The proof engine enriches arg names with event identity using `SemanticIndex.EventsByName`.
- **Semantic completeness:** Constraint influence is part of the static analysis picture. The proof engine is the last analysis stage — it has full visibility to produce the final enriched shape.
- **Consumer convenience:** The Precept Builder can reorganize influence entries into whatever map structure runtime needs without duplicating the traversal.

**Alternative considered:** Having the type checker produce the final `ConstraintInfluenceEntry` shape directly. Deferred because the TypeChecker's `ConstraintFieldRefs` is already a correct intermediate representation, and the enrichment step (arg name → EventArgReference) is trivial.

### Decision 5: Modifier-Proof via Catalog Metadata

**Decision:** Modifier proof satisfaction mappings are declared in `ValueModifierMeta.ProofSatisfactions`, not hardcoded in the proof engine.

**Rationale:**
- **Catalog-driven architecture consistency:** Precept's modifiers are catalog-declared. Their proof implications should be too.
- **Extensibility:** Adding a new modifier that satisfies a proof requirement requires only a catalog entry update.
- **Single source of truth:** The modifier catalog already knows what each modifier means. Adding satisfaction metadata keeps all modifier knowledge in one place.

**New metadata:**

```csharp
public abstract record ProofSatisfaction(ProofRequirementKind RequirementKind)
{
    public sealed record Numeric(
        SatisfactionProjection Projection,
        OperatorKind Comparison,
        NumericBoundSource Bound)
        : ProofSatisfaction(ProofRequirementKind.Numeric);

    public sealed record Presence()
        : ProofSatisfaction(ProofRequirementKind.Presence);

    public sealed record Dimension(DimensionSource Source)
        : ProofSatisfaction(ProofRequirementKind.Dimension);

    public sealed record Modifier(ModifierKind RequiredModifier)
        : ProofSatisfaction(ProofRequirementKind.Modifier);

    public sealed record QualifierCompatibility(QualifierAxis Axis)
        : ProofSatisfaction(ProofRequirementKind.QualifierCompatibility);
}
```

| Modifier | ProofSatisfactions |
|---|---|
| `positive` | `[Numeric(SelfValue, >, 0)]` |
| `nonnegative` | `[Numeric(SelfValue, >=, 0)]` |
| `nonzero` | `[Numeric(SelfValue, !=, 0)]` |
| `notempty` | `[Numeric(Accessor("length"), >, 0), Numeric(Accessor("count"), >, 0)]` |
| `min(N)` | `[Numeric(SelfValue, >=, DeclarationValue)]` |
| `max(N)` | `[Numeric(SelfValue, <=, DeclarationValue)]` |
| `minlength(N)` | `[Numeric(Accessor("length"), >=, DeclarationValue)]` |
| `maxlength(N)` | `[Numeric(Accessor("length"), <=, DeclarationValue)]` |
| `mincount(N)` | `[Numeric(Accessor("count"), >=, DeclarationValue)]` |
| `maxcount(N)` | `[Numeric(Accessor("count"), <=, DeclarationValue)]` |

---

## 12. Innovation

### Catalog-Declared Proof Obligations

Operations, functions, accessors, and actions declare safety requirements as catalog metadata. The proof engine reads these — it maintains no hardcoded obligation lists. Adding an operation with a division-by-zero hazard requires only a `NumericProofRequirement` catalog attachment.

**Contrast with traditional compilers:** Traditional compilers hardcode division-by-zero checks in the code generator or optimizer. Adding a new arithmetic operation requires modifying the checker. In Precept, the catalog entry carries the obligation declaration.

### Roslyn-Enforced FaultCode↔DiagnosticCode Correspondence

Every `FaultCode` carries a `[StaticallyPreventable(DiagnosticCode)]` attribute:

```csharp
public enum FaultCode
{
    [StaticallyPreventable(DiagnosticCode.DivisionByZero)]
    DivisionByZero = 1,
    
    [StaticallyPreventable(DiagnosticCode.SqrtOfNegative)]
    SqrtOfNegative = 2,
    
    [StaticallyPreventable(DiagnosticCode.UnguardedCollectionAccess)]
    CollectionEmptyOnAccess = 9,
    // ...
}
```

Roslyn analyzers PRECEPT0001 and PRECEPT0002 enforce:
- Every evaluator failure path routes through a classified `FaultCode`
- Every `FaultCode` links to its prevention `DiagnosticCode`

This makes fault–diagnostic correspondence a **build-time invariant**. A fault cannot be added without declaring its prevention diagnostic. A diagnostic cannot be removed while its fault still exists.

### Bounded, Non-Extensible Strategy Set

Five strategies only, each a simple predicate — not a solver framework. This makes proof:
- **Predictable:** Same input → same output, every time
- **Auditable:** Strategy logic is inspectable, not hidden in solver internals
- **Implementable:** No external dependencies, no complex algorithm implementations

New strategies are language changes, not tooling extensions. Adding a sixth strategy requires design review and documentation, not just implementation.

### Compile-Time Satisfiability

The proof engine guarantees initial-state configurations are satisfiable at compile time. If `field X default 0` and `ensure in Draft: X > 5`, the author gets a compile-time error, not a runtime failure on create.

**Contrast with rules engines:** Most rules engines discover unsatisfiable configurations at runtime when the first instance fails to instantiate. Precept catches this at compile time, before deployment.

### Constraint Influence Analysis

The proof engine produces a `ConstraintInfluenceMap` that enables AI agents to reason causally: "which fields affect which constraints?" This is a first-class output, not a debugging afterthought.

**Use case:** An AI assistant determining what to change to fix a constraint violation can consult the influence map to identify the relevant fields, rather than parsing constraint expressions at runtime.

---

## 13. Open Questions / Implementation Notes

### Implementation Status

Items 1 and 2 are resolved — the full ProofEngine body is implemented with all five strategies, obligation collection, diagnostic emission, constraint influence analysis, initial-state satisfiability, and ProofForwardingFact consumption.

### Validation Required

3. **Five-strategy coverage validation** — validate five-strategy coverage against all 20 sample files in `samples/` before committing to no sixth strategy. Qualifier-compatibility obligations are now an explicit Strategy 5 case; cross-field comparison obligations remain the highest-risk residual category.

4. **Roslyn analyzer scope confirmation** — confirm PRECEPT0001/PRECEPT0002 run on the Precept source itself (not on host-application code consuming Precept).

### Blocking Dependencies

5. **Initial-state satisfiability** — the algorithm is fully specified (PE-G8): bounded constant folding over `TypedField.DefaultExpression` and `TypedEnsure.Condition`. Implementation requires the type checker's expression resolution engine to be operational. No design questions remain — only implementation work.

6. **~~Expression tree parsing blocked~~** — **RESOLVED.** Guard-in-path (Strategy 3) and flow-narrowing (Strategy 4) are now unblocked. Parser produces `ParsedExpression` DU nodes for `GuardClauseSlot`; the type checker resolves these into `TypedExpression` for proof engine consumption.

> **Implementation note:** Item 5 above is an implementation dependency, not a design gap — the expression resolution engine must be built before initial-state satisfiability can run. No open design questions remain; CC#1 closed the expression tree design.

### Catalog Metadata Needed

7. **`ValueModifierMeta.ProofSatisfactions`** — ✅ resolved (CC#5 → PE-G2). `ProofSatisfaction[]` replaces the original `ProofDischarge[]` with a full DU covering all five requirement kinds. Strategy 2 reads `modifier.ProofSatisfactions` for numeric obligations; presence and qualifier-compatibility obligations read their respective declaration carriers (`DeclaredPresenceMeta`, `DeclaredQualifierMeta`). See §7 Strategy 2 for the catalog-driven dispatch pattern.

### Future Considerations

8. **Precision propagation awareness** — MEDIUM PRIORITY. The exact decimal arithmetic survey documents precision behaviors (division rounding, scale accumulation, mantissa overflow). If `ProofRequirement.PrecisionWarning` is added to `BinaryOperationMeta` for division/multiplication operations, consider whether this warrants a sixth proof strategy or fits within the existing five-strategy framework.

9. **Justification mechanism** — SPARK GNATprove provides a `Justified` disposition for checks that cannot be proved but have been manually annotated as acceptable. If the five-strategy boundary proves too restrictive, a justification mechanism would be the precedented response. Currently not planned.

10. **OperatorKind enum names verified (PE-G17)** — all `OperatorKind` member names used in spec pseudocode (`NotEquals`, `GreaterThan`, `GreaterThanOrEqual`, `LessThan`, `LessThanOrEqual`) match `src/Precept/Language/OperatorKind.cs` exactly.

---

## 14. Deliberate Exclusions

### No SMT Solver

The bounded strategy set is intentional. General proof is not a goal. Adding an SMT solver would:
- Introduce non-deterministic verification times
- Add external dependencies (Z3, CVC5)
- Complicate the build and deployment story
- Provide marginal benefit for the constrained DSL expression language

If an obligation cannot be discharged by the five strategies, the author adds a guard or modifier to make it statically provable, or accepts the defense-in-depth backstop.

### No Runtime Obligation Checking

The `ProofLedger` does NOT cross the compile-runtime boundary. Only `FaultSiteAnnotation` records (stamped on opcodes by the Precept Builder per CC#6) cross into runtime.

**Why not pass obligations to runtime?**
- Runtime instances would carry proof baggage that's never used in execution
- Defense-in-depth backstops already provide runtime safety
- Proof is a compile-time activity — mixing it with runtime would blur responsibilities

### No Constraint Evaluation

Constraint evaluation is the evaluator's domain. The proof engine checks satisfiability statically; the evaluator enforces dynamically. These are separate concerns:
- **Proof engine:** "Can this constraint ever be satisfied given the structure?"
- **Evaluator:** "Is this constraint satisfied right now given these values?"

### No General Dataflow Analysis

The five strategies are local and shallow:
- Literal proof: single expression
- Declaration attribute proof: single subject's declaration metadata
- Guard-in-path: enclosing guard in same transition row
- Flow narrowing: guard constraint in same transition row
- Qualifier compatibility proof: dual-subject qualifier comparison on one axis

General dataflow (tracking values across multiple transitions, interprocedural analysis) is out of scope. The DSL's event-driven model makes general dataflow less meaningful than in imperative languages.

### No Inductive Proof

The proof engine does not handle loop invariants, recursive definitions, or inductive properties. Precept does not have loops or recursion — the DSL is finite and event-driven.

### No User-Defined Proof Hints

Unlike SPARK Ada's `pragma Annotate` or Dafny's `assert`/`assume`, Precept does not support author-provided proof hints. The five-strategy set is sufficient for the DSL's constrained expression language. If hints become necessary, they would be a language addition, not a proof engine extension.

---

## 15. Cross-References

| Topic | Document |
|---|---|
| Full pipeline architecture, artifact flow | `docs/compiler-and-runtime-design.md` |
| Proof engine overview (§8) | `docs/compiler-and-runtime-design.md §8` |
| SemanticIndex shape, typed expression records | `docs/compiler/type-checker.md §7.1` |
| StateGraph output, ProofForwardingFact shapes | `docs/compiler/graph-analyzer.md §4` |
| FaultSiteDescriptor planting | `docs/runtime/precept-builder.md` |
| ProofRequirement catalog and DU structure | `docs/language/catalog-system.md` |
| Modifier catalog (ValueModifierMeta) | `docs/language/catalog-system.md` |
| Diagnostic infrastructure and error-accumulation contract | `docs/compiler/diagnostic-system.md` |
| FaultCode → DiagnosticCode chain, `[StaticallyPreventable]` | `docs/compiler/diagnostic-system.md § FaultCode → DiagnosticCode Chain` |
| FaultCode ↔ DiagnosticCode correspondence | `src/Precept/Language/FaultCode.cs` |
| Roslyn analyzer enforcement | `analyzers/Precept.Analyzers/` |

---

## 16. Source Files

| File | Purpose |
|---|---|
| `src/Precept/Pipeline/ProofEngine.cs` | Proof engine implementation — `ProofEngine` static class with `Prove(SemanticIndex, StateGraph)` entry point |
| `src/Precept/Pipeline/ProofLedger.cs` | `ProofLedger` — obligation ledger artifact (currently minimal, needs expansion) |
| `src/Precept/Language/ProofRequirement.cs` | `ProofRequirement` DU base and subtypes, `ProofSubject` DU |
| `src/Precept/Language/ProofRequirementKind.cs` | `ProofRequirementKind` enum (5 members) |
| `src/Precept/Language/ProofRequirements.cs` | `ProofRequirements` catalog with `GetMeta()` and `All` |
| `src/Precept/Language/FaultCode.cs` | `FaultCode` enum with `[StaticallyPreventable]` attributes |
| `src/Precept/Language/Faults.cs` | `Faults` catalog with `GetMeta()` and fault message templates |
| `src/Precept/Language/Modifier.cs` | `ModifierMeta` DU including `ValueModifierMeta` with `ProofSatisfactions[]` (PE-G2 locked) |
| `src/Precept/Language/Modifiers.cs` | `Modifiers` catalog |
| `src/Precept/Language/DiagnosticCode.cs` | `DiagnosticCode` enum including proof-related codes (graph analyzer uses codes 80–85; proof engine codes start at 86) |

