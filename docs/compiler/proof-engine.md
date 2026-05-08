# Proof Engine

## 1. Status

| Property | Value |
|---|---|
| Doc maturity | Full |
| Implementation state | Stub — not yet implemented |
| Source | `src/Precept/Pipeline/ProofEngine.cs`, `src/Precept/Pipeline/ProofLedger.cs` |
| Upstream | SemanticIndex + StateGraph, catalog metadata (Operations, Functions, Types, Modifiers, Actions, Diagnostics, Faults) |
| Downstream | Compilation (proof ledger), Precept Builder (fault backstops, constraint influence map) |

---

## 2. Overview

The proof engine is the fifth and final analysis stage before the Precept Builder — the compile-time half of Precept's structural safety guarantee. Its role: discharge statically preventable runtime hazards using a bounded, four-strategy set. If an operation is proven safe, no runtime check is needed. If proof fails, the compiler emits a diagnostic and the author must fix the source before an executable model is produced.

**Pipeline Position:**

```text
Source Text → Lexer → Parser → Name Binder → Type Checker → Graph Analyzer → [Proof Engine] → Precept Builder
                                    ↓               ↓                ↓
                              SemanticIndex    StateGraph       ProofLedger
```

The proof engine consumes the `SemanticIndex` (typed expressions with attached `ProofRequirement` records) and `StateGraph` (structural analysis facts including `ProofForwardingFact` entries). It produces a `ProofLedger` — the complete obligation inventory with dispositions, fault-site links, constraint influence analysis, and initial-state satisfiability results.

**Key design choices:**

1. **Proof is bounded** — four strategies only, no general SMT solver. Predictable, auditable, zero external dependencies.
2. **Proof ledger does NOT cross the compile-runtime boundary** — only `FaultSiteDescriptor` residue (defense-in-depth backstops) crosses into runtime. The proof engine is purely a compile-time analysis stage.
3. **Catalog-driven obligations** — the proof engine reads `ProofRequirement` records stamped by the type checker from catalog metadata. It does NOT maintain its own list of what needs to be proved.

---

## 3. Responsibilities and Boundaries

### In Scope

| Responsibility | Description |
|---|---|
| **Obligation instantiation** | Create `ProofObligation` records from `ProofRequirement` attachments on typed expressions and actions |
| **Obligation discharge** | Apply four proof strategies in order to resolve each obligation |
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

The proof engine is **intentionally bounded** — four strategies only, no external solver. This is a deliberate scope limit, not a capability gap.

| Metric | Value | Rationale |
|---|---|---|
| Estimated LOC | 400–600 | ~150 obligation instantiation + ~200 strategy dispatch + ~100 influence analysis + ~50 satisfiability |
| Strategy count | 4 | Bounded set covers the DSL's constrained expression language |
| External dependencies | 0 | No SMT solver, no SAT solver, no external libraries |
| Determinism | 100% | Same input always produces same output — no solver timeouts or resource limits |

**Why four strategies, not a general solver:**

General SMT solving (Z3, CVC4/5) would add non-deterministic verification times, external dependencies, and implementation complexity. The surveyed verification systems (SPARK Ada/GNATprove, Dafny, Liquid Haskell, CBMC) all depend on external solvers for general proof discharge. Precept's DSL is intentionally constrained — the expression language is finite, the obligation space is bounded, and the four-strategy set covers realistic programs.

**Boundary condition:** If the four strategies prove insufficient for real programs, a fifth strategy (e.g., relational pair narrowing for cross-field comparisons) would be added — not a general solver. The strategy set is bounded, not extensible by users.

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
    ProofDisposition Disposition,     // Proved | Unresolved
    ProofStrategy? Strategy,          // which strategy discharged it (null if Unresolved)
    DiagnosticCode? EmittedDiagnostic // diagnostic code if Unresolved (null if Proved)
);

public enum ProofDisposition { Proved, Unresolved }

public enum ProofStrategy
{
    Literal,         // value is a compile-time constant
    Modifier,        // field modifier establishes the bound
    GuardInPath,     // enclosing guard establishes the constraint
    FlowNarrowing    // same-row guard narrows the type state
}
```

> **Resolved (CC#6):** `ProofObligation.Site` structural identity
> The builder resolves structural binding during Pass 4 (expression compilation). When the builder visits a `TypedExpression` to emit an opcode, it matches against `ProofLedger.FaultSiteLinks` by `ProofObligation.Site` identity. The structural binding is the opcode itself — a nullable `FaultSiteAnnotation?` is stamped directly on the emitted opcode. No separate structural reference or opcode-offset lookup is needed; the annotation lives on the opcode, not on a side table. See `precept-builder.md §Pass 4` for the planting contract.

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
public sealed record RuleIdentity(string RuleName, int Index) : ConstraintIdentity;
public sealed record EnsureIdentity(ConstraintKind Kind, string? AnchorState, string? AnchorEvent, int Index) : ConstraintIdentity;

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
│  • Walk SemanticIndex typed expressions                              │
│  • For each TypedBinaryOp/TypedFunctionCall/TypedMemberAccess/       │
│    TypedAction with non-empty ProofRequirements:                     │
│    - Create ProofObligation for each requirement                     │
│    - Attach semantic context (enclosing guard, field modifiers)      │
│  • Incorporate ProofForwardingFacts from StateGraph                  │
└─────────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────┐
│  Pass 2: Obligation Discharge                                        │
│  • For each ProofObligation:                                         │
│    - Try Strategy 1 (Literal) → if success, mark Proved              │
│    - Try Strategy 2 (Modifier) → if success, mark Proved             │
│    - Try Strategy 3 (GuardInPath) → if success, mark Proved          │
│    - Try Strategy 4 (FlowNarrowing) → if success, mark Proved        │
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

// 4. Modifier check (field must declare required modifier)
public sealed record ModifierRequirement(
    ProofSubject Subject,
    ModifierKind Required,
    string Description
) : ProofRequirement(ProofRequirementKind.Modifier, Description);

// 5. Qualifier compatibility (two operands must share a qualifier value)
public sealed record QualifierCompatibilityProofRequirement(
    ProofSubject LeftSubject,
    ProofSubject RightSubject,
    QualifierAxis Axis,
    string Description
) : ProofRequirement(ProofRequirementKind.QualifierCompatibility, Description);
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

### Four Proof Strategies

Each strategy is a simple predicate function — not a solver. The first strategy that succeeds marks the obligation as `Proved`. Strategies are tried in order (1 → 2 → 3 → 4).

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

#### Strategy 2: Modifier Proof

**When it applies:** The subject expression resolves to a field with a modifier that statically bounds the value.

**How it works:** Walk the subject field's `Modifiers` array; check whether any modifier discharges the requirement.

**Modifier → Proof Discharge Mapping:**

| Modifier | Discharges | Rationale |
|---|---|---|
| `positive` | `NumericProofRequirement(>, 0)`, `NumericProofRequirement(!=, 0)` | Field is strictly > 0 |
| `nonnegative` | `NumericProofRequirement(>=, 0)` | Field is ≥ 0 |
| `nonzero` | `NumericProofRequirement(!=, 0)` | Field is ≠ 0 |
| `notempty` | `NumericProofRequirement(count > 0)` | Collection is never empty |
| `min(N)` | `NumericProofRequirement(>=, N)` | Field is ≥ N |
| `max(N)` | `NumericProofRequirement(<=, N)` | Field is ≤ N |

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

**Catalog metadata design:** The `FieldModifierMeta` record gains a `ProofDischarges` property:

```csharp
public sealed record FieldModifierMeta(
    ModifierKind Kind,
    TokenMeta Token,
    string Description,
    ModifierCategory Category,
    TypeTarget[] ApplicableTo,
    bool HasValue = false,
    ModifierKind[] Subsumes = default!,
    ProofDischarge[] ProofDischarges = default!,  // ← NEW
    // ... other properties
) : ModifierMeta(...);

public sealed record ProofDischarge(
    ProofRequirementKind RequirementKind,
    OperatorKind? Comparison,    // for Numeric requirements
    decimal? Threshold           // for Numeric requirements
);
```

**Discharge predicate pseudocode:**

```csharp
// Strategy 2: Modifier Proof — Discharge Predicate
// Input: ProofObligation (from Pass 1)
// Reads: SemanticIndex.FieldsByName (to get TypedField.Modifiers),
//        FieldModifierMeta.ProofDischarges (from Modifiers catalog)
// Scope: Any obligation whose subject resolves to a declared field with modifiers

bool TryModifierProof(ProofObligation obligation, SemanticIndex semantics)
{
    // 1. Resolve the proof subject to a field name
    var fieldName = GetFieldName(obligation.Requirement.Subject, obligation.Site);
    if (fieldName is null) return false;

    // 2. Look up the field's modifiers from SemanticIndex
    if (!semantics.FieldsByName.TryGetValue(fieldName, out var field))
        return false;

    // 3. For each modifier on the field, check catalog-declared ProofDischarges
    foreach (var modifier in field.Modifiers)
    {
        var meta = Modifiers.GetMeta(modifier.Kind);

        foreach (var discharge in meta.ProofDischarges)
        {
            // 4. Check if this discharge covers the obligation's requirement
            if (DischargeCovers(discharge, obligation.Requirement))
                return true;  // modifier statically establishes the bound
        }
    }

    return false;  // no modifier discharges this requirement
}

// DischargeCovers: checks whether a ProofDischarge entry covers a requirement.
// For NumericProofRequirement: discharge.RequirementKind == Numeric
//   AND discharge.Comparison subsumes requirement.Comparison at discharge.Threshold.
// For PresenceProofRequirement: discharge.RequirementKind == Presence.
// Subsumption: positive (>, 0) covers both (!=, 0) and (>=, 0).
// The subsumption logic mirrors GuardSubsumes (Strategy 3) but reads from catalog
// metadata rather than from a guard expression.
```

> **✅ Resolved (CC#5) — FieldModifierMeta.ProofDischarges is now canonical**
> `ProofDischarge[]` has been added to `FieldModifierMeta` in `catalog-system.md`. The `ProofDischarge` record is also defined there. Strategy 2 can now be implemented by reading `modifier.ProofDischarges` from the catalog — no per-modifier switch in the engine.
> *Resolved: 2026-05-06 — CC#5*

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
    // 1. Find the enclosing transition row for this obligation's expression site
    var row = FindEnclosingTransitionRow(obligation.Site, semantics);
    if (row?.Guard is null) return false;  // no guard → cannot prove

    // 2. Decompose the guard into simple constraint forms
    //    Supported forms: field OP literal, count(collection) > 0, field is set
    var guardConstraints = ExtractGuardConstraints(row.Guard);

    // 3. For each guard constraint, check if it covers the requirement
    foreach (var guard in guardConstraints)
    {
        if (obligation.Requirement is NumericProofRequirement numeric)
        {
            if (GuardSubsumes(guard, numeric))
                return true;
        }
        else if (obligation.Requirement is PresenceProofRequirement presence)
        {
            // "field is set" guard discharges presence requirements
            // "count(collection) > 0" guard discharges collection non-empty
            if (guard.Field == GetFieldName(presence.Subject)
                && guard.IsPresenceCheck)
                return true;
        }
    }

    return false;  // no guard constraint covers the requirement
}

bool GuardSubsumes(GuardConstraint guard, NumericProofRequirement requirement)
{
    if (guard.Field != GetFieldName(requirement.Subject)) return false;

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
    // 1. Find the enclosing transition row
    var row = FindEnclosingTransitionRow(obligation.Site, semantics);
    if (row?.Guard is null) return false;

    // 2. Decompose the guard — look for field-vs-field comparisons only
    //    e.g., "when Quantity > ReorderPoint" → (Quantity, >, ReorderPoint)
    var relationalGuards = ExtractFieldToFieldConstraints(row.Guard);
    if (relationalGuards.IsEmpty) return false;  // no field-vs-field guards

    // 3. Check if the obligation's expression site uses both fields from a guard
    if (obligation.Site is not TypedBinaryOp binaryOp) return false;

    var leftField = GetFieldName(binaryOp.Left);
    var rightField = GetFieldName(binaryOp.Right);
    if (leftField is null || rightField is null) return false;

    // 4. For each relational guard, check if it establishes a constraint
    //    that makes the binary operation safe
    foreach (var guard in relationalGuards)
    {
        // Guard: A > B (or A >= B, A < B, etc.)
        // Obligation site: expression involving A and B (e.g., A - B, B / A)
        if (!InvolvesFields(guard, leftField, rightField)) continue;

        // 5. Check if the guard's established relation implies the obligation
        //    Example: guard "Quantity > ReorderPoint" + site "ReorderPoint - Quantity"
        //    → guard proves Quantity > ReorderPoint
        //    → therefore (ReorderPoint - Quantity) < 0 — result range is known
        //    → if obligation is "result won't overflow" or "divisor != 0" for
        //      an expression involving both fields, the guard covers it
        if (GuardRelationImpliesObligation(guard, binaryOp, obligation.Requirement))
            return true;
    }

    return false;
}

// GuardRelationImpliesObligation: given guard "A op B" and obligation on
// an expression f(A, B), check whether the guard's established relation
// implies the obligation predicate. This is a simple pattern match on
// (guard.Op, expression.Op, requirement.Comparison) triples — not a solver.
// Example triples:
//   guard A > B  + expr A - B  + req result >= 0  → true (A-B > 0 > requirement)
//   guard A > B  + expr B / A  + req divisor != 0 → true (A > B and both fields,
//                                                          but need A != 0 separately)
//   guard A >= B + expr A - B  + req result >= 0  → true (A-B >= 0)
```

> **Strategy 3 vs Strategy 4 boundary (resolved):**
> - **Strategy 3 (guard-in-path):** The `when <guard>` clause protects all actions in the row. The guard must directly constrain the proof subject — the field (or collection) being checked for the required property appears as the operand in the guard comparison (e.g., `when Divisor != 0` discharges a `divisor != 0` proof obligation for `A / Divisor`). Strategy 3 applies when the subject is a *named field or arg* and the guard is a *simple comparison or presence check* on that subject.
> - **Strategy 4 (flow-narrowing):** The guard establishes a *relational invariant between two or more fields* (e.g., `when Quantity > ReorderPoint` establishes `Quantity > ReorderPoint`). Strategy 4 discharges obligations where the proof site involves an expression over both constrained fields and the established relation implies the obligation (e.g., `ReorderPoint - Quantity` is safe because the guard proves `Quantity > ReorderPoint`, so the result is negative, avoiding a specific overflow/underflow concern). Strategy 4 applies only when the guard is a *binary comparison between two non-literal operands* and the obligation is an arithmetic result-range obligation on an expression involving those operands.
> - **Key discriminator:** If the guard directly names the proof subject (`when X > 0` for an obligation about `X`), use Strategy 3. If the guard names two fields in relation (`when A > B`) and the obligation is about an expression using both, use Strategy 4. Strategy 4 does not fire for simple per-field presence or range checks — those are Strategy 3.

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
3. Every unresolved obligation produces both a diagnostic (for the author) and a `FaultSiteLink` (for the Precept Builder)

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

The proof engine verifies that a precept can be instantiated: given the initial state and any field values provided at creation time, are all `initial`-scope constraints satisfiable?

**What is checked:**

1. For the state marked `initial`, collect all `ensure` constraints with `in` anchor
2. For each constraint condition, check whether default field values satisfy it
3. If any constraint is unsatisfiable, emit a diagnostic and record the violation

**Example of unsatisfiable initial state:**

```precept
field Amount as decimal default 0
state Draft initial
ensure in Draft: Amount > 5, "Amount must exceed 5"
```

This precept cannot be instantiated — the default value `0` violates `Amount > 5`. The proof engine emits `DiagnosticCode.UnsatisfiableGuard`.

**Blocking dependency:** This check requires resolved initial field values and initial-state constraint expressions. Implementation is blocked pending the type checker's expression resolution engine being fully operational.

### Collection Non-Empty Proof

Collection non-empty obligations arise from several sources:

| Source | Obligation |
|---|---|
| `first(collection)` | Collection must be non-empty |
| `last(collection)` | Collection must be non-empty |
| `collection.peek` | Queue/stack must be non-empty |
| `dequeue Collection` action | Queue must be non-empty |
| `pop Collection` action | Stack must be non-empty |

**Modifier-proof strategy:** The `notempty` modifier discharges collection non-empty requirements.

**Guard-in-path strategy:** `count(collection) > 0` or `collection.count > 0` guards discharge the requirement.

### ProofForwardingFact Consumption Contract

The proof engine consumes `ProofForwardingFact` entries from the `StateGraph` during Pass 1 (Obligation Instantiation). Each fact subtype has a specific consumption pattern:

| Fact Subtype | Consumption |
|---|---|
| `ReachabilityFact` | Unreachable-state facts suppress proof obligations on transitions originating from unreachable states — those transitions can never fire, so their proof requirements are vacuously satisfied. Reachable paths feed causal diagnostic explanations. |
| `DominancePathFact` | Verifies the required-state guarantee. If `DominatedTerminals` is empty, records a structural violation in the proof ledger (the graph analyzer already emitted `RequiredStateDoesNotDominateTerminal`). |
| `EventCoverageFact` | Uses coverage gaps to reason about guard completeness: in states where an event is handled, are the guards sufficient? Coverage gaps are structural facts; guard satisfiability is the proof engine's domain. |
| `TerminalCompletenessFact` | Records terminal completeness in the proof ledger for structural completeness reasoning. No additional diagnostics — the graph analyzer emits `UnreachableState` for each unreachable terminal. |
| `DeadEndStateFact` | Records dead-end states in the proof ledger as structural completeness failures. Dead-end states represent lifecycle traps — entities entering these states can never reach completion. The proof engine suppresses proof obligations on transitions originating FROM dead-end states to other dead-end states (those paths are already structurally broken). Transitions INTO dead-end states retain their obligations — those transitions are the ones the author likely needs to fix. No additional diagnostics — the graph analyzer emits `DeadEndState` (Warning, code 108) for each dead-end state. |

---

## 8. Dependencies and Integration Points

### Upstream Dependencies

| Component | What ProofEngine Consumes |
|---|---|
| **SemanticIndex** | `TypedBinaryOp`, `TypedFunctionCall`, `TypedMemberAccess` with `ProofRequirements` arrays; `TypedRule`, `TypedEnsure` for constraint influence; `TypedField` with `Modifiers` for modifier-proof |
| **StateGraph** | `ProofForwardingFact` entries for structural violations that have proof-level implications |
| **Catalog: Operations** | `BinaryOperationMeta.ProofRequirements` — division-by-zero, overflow hazards |
| **Catalog: Functions** | `FunctionOverload.ProofRequirements` — `sqrt()` non-negativity, `pow()` exponent constraints |
| **Catalog: Types** | `TypeAccessor.ProofRequirements` — collection `.first`, `.last`, `.peek` non-empty requirements |
| **Catalog: Actions** | `ActionMeta.ProofRequirements` — `dequeue`, `pop` non-empty requirements |
| **Catalog: Modifiers** | `FieldModifierMeta.ProofDischarges` — which modifiers discharge which proof requirements |
| **Catalog: Faults** | `FaultCode` ↔ `DiagnosticCode` correspondence via `[StaticallyPreventable]` attribute |

### Downstream Consumers

| Consumer | What It Receives |
|---|---|
| **Compilation** | `ProofLedger` as one of the sealed pipeline artifacts; `Diagnostics` merged into final diagnostic stream |
| **Precept Builder** | `FaultSiteLinks` → plants `FaultSiteDescriptor` backstops; `ConstraintInfluence` → builds `ConstraintInfluenceMap` |
| **Language Server** | Proof diagnostics with semantic site attribution for Problems panel |
| **MCP compile output** | Obligation dispositions and constraint influence for AI tooling |

---

## 9. Failure Modes and Recovery

### Error Accumulation

The proof engine follows Precept's "accumulate diagnostics without abandoning" principle:

| Condition | Behavior |
|---|---|
| Unresolved obligation | Emit diagnostic, mark as `Unresolved`, continue to next obligation |
| Invalid ProofRequirement | Should not occur — catalog validation prevents malformed requirements |
| Missing catalog metadata | Should not occur — type checker stamps requirements from validated catalogs |
| Empty SemanticIndex | Produce empty `ProofLedger` with no obligations |
| All obligations proved | Produce `ProofLedger` with all dispositions `Proved`, no diagnostics |

### Partial Results

The proof engine always produces a complete `ProofLedger`, regardless of how many obligations fail:

```csharp
public static ProofLedger Prove(SemanticIndex semantics, StateGraph graph)
{
    var obligations = new List<ProofObligation>();
    var faultSiteLinks = new List<FaultSiteLink>();
    var diagnostics = new List<Diagnostic>();
    
    // Pass 1: Instantiate all obligations
    foreach (var expr in semantics.AllTypedExpressions)
    {
        foreach (var req in expr.ProofRequirements)
            obligations.Add(InstantiateObligation(req, expr));
    }
    
    // Pass 2: Discharge each obligation
    foreach (var obligation in obligations)
    {
        var (disposition, strategy) = TryDischarge(obligation, semantics);
        obligation = obligation with { Disposition = disposition, Strategy = strategy };
        
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
        AnalyzeConstraintInfluence(semantics).ToImmutableArray(),  // reads SemanticIndex.ConstraintRefs
        CheckInitialStateSatisfiability(semantics).ToImmutableArray(),
        diagnostics.ToImmutableArray()
    );
}
```

### Upstream Error Handling

If the `SemanticIndex` contains `TypedErrorExpression` nodes (from type checker error recovery), the proof engine skips them — they have no `ProofRequirements` to process. The error was already reported by the type checker.

---

## 10. Contracts and Guarantees

### Obligation Completeness

**Contract:** Every `ProofRequirement` declared in any catalog entry for any used operation/function/accessor/action has a corresponding `ProofObligation` in the `ProofLedger`.

**Enforcement:** The type checker stamps requirements onto typed expressions; the proof engine iterates over all typed expressions with non-empty `ProofRequirements` arrays.

### Disposition Exhaustiveness

**Contract:** Every `ProofObligation` has a disposition: `Proved` (one of four strategies succeeded) or `Unresolved` (emitted diagnostic).

**Enforcement:** The strategy dispatch loop is exhaustive — after trying all four strategies, the obligation is marked `Unresolved` and a diagnostic is emitted.

### Fault Chain Integrity

**Contract:** Every `FaultSiteLink` in the `ProofLedger` has a 1:1 correspondence with an `Unresolved` obligation and a `FaultCode` that carries `[StaticallyPreventable(DiagnosticCode)]`.

**Enforcement:**
1. `FaultSiteLink` is created only for `Unresolved` obligations
2. The `FaultCode` lookup uses the Roslyn-enforced `[StaticallyPreventable]` attribute
3. Roslyn analyzers PRECEPT0001/PRECEPT0002 enforce attribute presence at build time

### Determinism

**Contract:** Same input (`SemanticIndex` + `StateGraph`) always produces same output (`ProofLedger`).

**Enforcement:** No external solver, no non-deterministic algorithms, no timeouts. The four strategies are pure predicate functions.

### Catalog Correspondence

**Contract:** The proof engine does NOT maintain its own list of proof obligations. All obligations are declared in catalog metadata.

**Enforcement:** The proof engine reads `ProofRequirements` from typed expressions — it never constructs `ProofRequirement` instances directly.

---

## 11. Design Rationale and Decisions

### Decision 1: Four-Strategy Bounded Set vs. SMT Solver

**Decision:** The proof engine uses exactly four proof strategies — no general SMT solver.

**Rationale:**
- **Predictability:** Every proof attempt completes in bounded, deterministic time. No solver timeouts, no "unknown" results, no resource exhaustion.
- **Auditability:** Each strategy is a simple predicate function (~10–30 lines). Authors can understand exactly why an obligation was proved or not.
- **Zero external dependencies:** No Z3, no CVC5, no SAT solver. The proof engine is self-contained within the Precept runtime.
- **Coverage sufficiency:** The DSL expression language is intentionally constrained. Precept does not support arbitrary arithmetic, unbounded loops, or recursive definitions. The four strategies cover the realistic obligation space.

**Trade-off accepted:** The proof engine cannot discharge complex cross-field relationships or inductive properties. This is acceptable because:
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

**Decision:** Modifier proof discharge mappings are declared in `FieldModifierMeta.ProofDischarges`, not hardcoded in the proof engine.

**Rationale:**
- **Catalog-driven architecture consistency:** Precept's modifiers are catalog-declared. Their proof implications should be too.
- **Extensibility:** Adding a new modifier that discharges a proof requirement requires only a catalog entry update.
- **Single source of truth:** The modifier catalog already knows what each modifier means. Adding discharge metadata keeps all modifier knowledge in one place.

**New metadata:**

```csharp
public sealed record ProofDischarge(
    ProofRequirementKind RequirementKind,
    OperatorKind? Comparison,
    decimal? Threshold
);
```

| Modifier | ProofDischarges |
|---|---|
| `positive` | `[Numeric(>, 0), Numeric(!=, 0)]` |
| `nonnegative` | `[Numeric(>=, 0)]` |
| `nonzero` | `[Numeric(!=, 0)]` |
| `notempty` | `[Numeric(>, 0)]` for collection count |
| `min(N)` | `[Numeric(>=, N)]` |
| `max(N)` | `[Numeric(<=, N)]` |

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

Four strategies only, each a simple predicate — not a solver framework. This makes proof:
- **Predictable:** Same input → same output, every time
- **Auditable:** Strategy logic is inspectable, not hidden in solver internals
- **Implementable:** No external dependencies, no complex algorithm implementations

New strategies are language changes, not tooling extensions. Adding a fifth strategy requires design review and documentation, not just implementation.

### Compile-Time Satisfiability

The proof engine guarantees initial-state configurations are satisfiable at compile time. If `field X default 0` and `ensure in Draft: X > 5`, the author gets a compile-time error, not a runtime failure on create.

**Contrast with rules engines:** Most rules engines discover unsatisfiable configurations at runtime when the first instance fails to instantiate. Precept catches this at compile time, before deployment.

### Constraint Influence Analysis

The proof engine produces a `ConstraintInfluenceMap` that enables AI agents to reason causally: "which fields affect which constraints?" This is a first-class output, not a debugging afterthought.

**Use case:** An AI assistant determining what to change to fix a constraint violation can consult the influence map to identify the relevant fields, rather than parsing constraint expressions at runtime.

---

## 13. Open Questions / Implementation Notes

### Implementation Status

1. **`ProofEngine.Prove` throws `NotImplementedException`** — catalog-side proof vocabulary exists (`ProofRequirements` catalog, `BinaryOperationMeta.ProofRequirements`, `FunctionOverload.ProofRequirements`), but `ProofObligation` instantiation and strategy evaluation are not implemented.

2. **`ProofLedger` is minimal** — currently only holds `Diagnostics`. The full shape designed in this document needs to be implemented.

### Validation Required

3. **Four-strategy coverage validation** — validate four-strategy coverage against all 20 sample files in `samples/` before committing to no fifth strategy. Cross-field comparison obligations (e.g., `ApprovedAmount <= RequestedAmount`) are the highest-risk case — confirm guard-in-path covers them or identify the gap.

4. **Roslyn analyzer scope confirmation** — confirm PRECEPT0001/PRECEPT0002 run on the Precept source itself (not on host-application code consuming Precept).

### Blocking Dependencies

5. **Initial-state satisfiability blocked on implementation** — this check requires the type checker's expression resolution engine to be operational. The expression tree design is resolved (CC#1); what remains is the type checker implementation itself. Once the type checker's `Resolve()` pass is implemented, initial-state satisfiability can proceed without design changes.

6. **~~Expression tree parsing blocked~~** — **RESOLVED.** Guard-in-path (Strategy 3) and flow-narrowing (Strategy 4) are now unblocked. Parser produces `ParsedExpression` DU nodes for `GuardClauseSlot`; the type checker resolves these into `TypedExpression` for proof engine consumption.

> **Implementation note:** Item 5 above is an implementation dependency, not a design gap — the expression resolution engine must be built before initial-state satisfiability can run. No open design questions remain; CC#1 closed the expression tree design.

### Catalog Metadata Needed

7. **`FieldModifierMeta.ProofDischarges`** — ✅ resolved (CC#5). `ProofDischarge[]` is in `catalog-system.md §FieldModifierMeta`. Strategy 2 implementation reads `modifier.ProofDischarges` — no per-modifier switch. See §7 Strategy 2 for the catalog-driven dispatch pattern.

### Future Considerations

8. **Precision propagation awareness** — MEDIUM PRIORITY. The exact decimal arithmetic survey documents precision behaviors (division rounding, scale accumulation, mantissa overflow). If `ProofRequirement.PrecisionWarning` is added to `BinaryOperationMeta` for division/multiplication operations, consider whether this warrants a fifth proof strategy or fits within the existing four-strategy framework.

9. **Justification mechanism** — SPARK GNATprove provides a `Justified` disposition for checks that cannot be proved but have been manually annotated as acceptable. If the four-strategy boundary proves too restrictive, a justification mechanism would be the precedented response. Currently not planned.

---

## 14. Deliberate Exclusions

### No SMT Solver

The bounded strategy set is intentional. General proof is not a goal. Adding an SMT solver would:
- Introduce non-deterministic verification times
- Add external dependencies (Z3, CVC5)
- Complicate the build and deployment story
- Provide marginal benefit for the constrained DSL expression language

If an obligation cannot be discharged by the four strategies, the author adds a guard or modifier to make it statically provable, or accepts the defense-in-depth backstop.

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

The four strategies are local and shallow:
- Literal proof: single expression
- Modifier proof: single field declaration
- Guard-in-path: enclosing guard in same transition row
- Flow narrowing: guard constraint in same transition row

General dataflow (tracking values across multiple transitions, interprocedural analysis) is out of scope. The DSL's event-driven model makes general dataflow less meaningful than in imperative languages.

### No Inductive Proof

The proof engine does not handle loop invariants, recursive definitions, or inductive properties. Precept does not have loops or recursion — the DSL is finite and event-driven.

### No User-Defined Proof Hints

Unlike SPARK Ada's `pragma Annotate` or Dafny's `assert`/`assume`, Precept does not support author-provided proof hints. The four-strategy set is sufficient for the DSL's constrained expression language. If hints become necessary, they would be a language addition, not a proof engine extension.

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
| Modifier catalog (FieldModifierMeta) | `docs/language/catalog-system.md` |
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
| `src/Precept/Language/Modifier.cs` | `ModifierMeta` DU including `FieldModifierMeta` with `ProofDischarges[]` (CC#5 resolved) |
| `src/Precept/Language/Modifiers.cs` | `Modifiers` catalog |
| `src/Precept/Language/DiagnosticCode.cs` | `DiagnosticCode` enum including proof-related codes (graph analyzer uses codes 80–85; proof engine codes start at 86) |
