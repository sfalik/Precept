# PE-G3 Analysis — ProofLedger Shape Gap

**Date:** 2026-05-08
**Author:** Frank (Lead/Architect)
**Status:** Analysis complete — full design, no deferrals

---

## 1. What Is the Gap?

The spec (`docs/compiler/proof-engine.md` §5 "Output") defines `ProofLedger` with five fields:

```csharp
ProofLedger(
    ImmutableArray<ProofObligation> Obligations,
    ImmutableArray<FaultSiteLink> FaultSiteLinks,
    ImmutableArray<ConstraintInfluenceEntry> ConstraintInfluence,
    ImmutableArray<InitialStateSatisfiabilityResult> InitialStateResults,
    ImmutableArray<Diagnostic> Diagnostics
)
```

The source stub at `src/Precept/Pipeline/ProofLedger.cs` defines:

```csharp
ProofLedger(ImmutableArray<Diagnostic> Diagnostics)
```

**Nine types** referenced by the spec's ProofLedger do not exist anywhere in source:

1. `ProofObligation`
2. `ProofDisposition` (enum)
3. `ProofStrategy` (enum)
4. `FaultSiteLink`
5. `ConstraintInfluenceEntry`
6. `EventArgReference`
7. `InitialStateSatisfiabilityResult`
8. `UnsatisfiedConstraint`
9. `FaultSiteAnnotation`

The `Compilation` record consumes `ProofLedger` but only reads `Diagnostics` — it has no field for `FaultSiteLinks` or `ConstraintInfluence`. The `Compiler.cs` pipeline compiles cleanly because the stub is structurally valid.

---

## 2. What Does the Spec Say ProofLedger Should Be?

The spec defines the full output contract in §5 (lines 172–287). Each supporting type has a defined shape.

---

## 3. What Exists Today?

| Type | Source Status |
|------|-------------|
| `ProofLedger` | Stub — `ImmutableArray<Diagnostic>` only |
| `ConstraintIdentity` | EXISTS in `SemanticIndex.cs` (line 398) |
| `RuleIdentity` | EXISTS — `(int RuleIndex)` |
| `EnsureIdentity` | EXISTS — `(ConstraintKind Kind, string? AnchorName, int EnsureIndex)` |
| `ConstraintFieldRefs` | EXISTS in `SemanticIndex.cs` (line 384) |
| `FaultSiteDescriptor` | EXISTS in `Runtime/Descriptors.cs` — runtime-side type |
| `FaultCode` | EXISTS with `[StaticallyPreventable]` attributes |
| Everything else | DOES NOT EXIST |

---

## 4. Full Shape Definitions — No Deferrals

### 4.1 `ProofObligation`

**File:** `src/Precept/Pipeline/ProofLedger.cs`

```csharp
public sealed record ProofObligation(
    ProofRequirement Requirement,
    TypedExpression Site,
    ProofDisposition Disposition,
    ProofStrategy? Strategy,
    DiagnosticCode? EmittedDiagnostic
);
```

- `Requirement` — the catalog-declared requirement stamped by the type checker
- `Site` — the `TypedExpression` node where the obligation was instantiated (same object reference as in the SemanticIndex — no copies)
- `Disposition` — `Proved` or `Unresolved`
- `Strategy` — which strategy discharged it (`null` if `Unresolved`)
- `EmittedDiagnostic` — diagnostic code if `Unresolved` (`null` if `Proved`)

**Mutability note:** `ProofObligation` is created with `Disposition = Unresolved` in Pass 1 and then resolved with `with { ... }` in Pass 2. This is standard C# record immutability — a new instance is created.

### 4.2 `ProofDisposition`

**File:** `src/Precept/Pipeline/ProofLedger.cs`

```csharp
public enum ProofDisposition
{
    Proved,
    Unresolved
}
```

Two values only. This is not extensible. A `Justified` disposition is a future consideration (see PE spec §13) but is not implemented now.

### 4.3 `ProofStrategy`

**File:** `src/Precept/Pipeline/ProofLedger.cs`

```csharp
public enum ProofStrategy
{
    Literal = 1,
    DeclarationAttribute = 2,
    GuardInPath = 3,
    FlowNarrowing = 4,
    QualifierCompatibility = 5
}
```

Five values — one per strategy. The names are updated to reflect the PE-G2 rename: Strategy 2 is `DeclarationAttribute` (not just `Modifier`), and Strategy 5 is `QualifierCompatibility`.

**Why the name change for Strategy 2:** The original spec used `Modifier` but Strategy 2 now reads three carrier surfaces (`FieldModifierMeta.ProofSatisfactions`, `DeclaredPresenceMeta`, `DeclaredQualifierMeta`) plus direct modifier membership. `DeclarationAttribute` is the correct generalization — it discharges obligations from declaration-site attributes, not just modifiers.

### 4.4 `FaultSiteLink`

**File:** `src/Precept/Pipeline/ProofLedger.cs`

```csharp
public sealed record FaultSiteLink(
    ProofObligation Obligation,
    FaultCode FaultCode,
    DiagnosticCode DiagnosticCode,
    SourceSpan Site
);
```

Created only for `Unresolved` obligations. The Precept Builder consumes these to plant `FaultSiteDescriptor` runtime backstops.

**Relationship to `FaultSiteDescriptor`:** `FaultSiteLink` is the compile-time artifact (references the full obligation, carries `SourceSpan`). `FaultSiteDescriptor` is the runtime artifact (carries only `FaultCode`, `DiagnosticCode PreventedBy`, `int SourceLine`). The Precept Builder transforms the former into the latter during Pass 4 (expression compilation), collapsing `SourceSpan` to `int SourceLine`.

### 4.5 `ConstraintInfluenceEntry`

**File:** `src/Precept/Pipeline/ProofLedger.cs`

```csharp
public sealed record ConstraintInfluenceEntry(
    ConstraintIdentity Constraint,
    ImmutableArray<string> ReferencedFields,
    ImmutableArray<EventArgReference> ReferencedArgs
);

public sealed record EventArgReference(string EventName, string ArgName);
```

- `Constraint` — reuses the existing `ConstraintIdentity` DU from `SemanticIndex.cs`
- `ReferencedFields` — field names the constraint expression reads
- `ReferencedArgs` — event-qualified arg references

**Source of truth:** The proof engine projects `SemanticIndex.ConstraintRefs` (type `ConstraintFieldRefs`) into this richer shape by enriching bare arg names with their owning event via `SemanticIndex.EventsByName`.

**Shape discrepancy with spec (PE-G5):** The spec defines `ConstraintIdentity` with different parameter names than the source:
- Spec: `RuleIdentity(string RuleName, int Index)` — source: `RuleIdentity(int RuleIndex)` — no `RuleName`
- Spec: `EnsureIdentity(ConstraintKind, string? AnchorState, string? AnchorEvent, int Index)` — source: `EnsureIdentity(ConstraintKind, string? AnchorName, int EnsureIndex)` — single `AnchorName`

**Decision:** The source shapes are canonical. The spec must be updated to match. The source was created during TypeChecker implementation and has tests. `ConstraintInfluenceEntry` must use the source `ConstraintIdentity` shapes, not the spec's hypothetical shapes.

### 4.6 `InitialStateSatisfiabilityResult`

**File:** `src/Precept/Pipeline/ProofLedger.cs`

```csharp
public sealed record InitialStateSatisfiabilityResult(
    string StateName,
    bool IsSatisfiable,
    ImmutableArray<UnsatisfiedConstraint> Violations
);

public sealed record UnsatisfiedConstraint(
    ConstraintIdentity Constraint,
    string Reason
);
```

- `StateName` — the name of the initial state being checked
- `IsSatisfiable` — whether default field values satisfy all initial-scope constraints
- `Violations` — the specific constraints that are not satisfiable, with human-readable reasons

**Scope note (from Decision 3):** This check is bounded constant folding — substitute `TypedField.DefaultExpression` into `TypedEnsure.Condition`, then fold. If the expression cannot be fully reduced, the result is `IsSatisfiable = true` (conservative — no false negatives).

### 4.7 `FaultSiteAnnotation`

**File:** `src/Precept/Pipeline/ProofLedger.cs` (compile-time shape; the builder stamps this onto opcodes)

```csharp
public sealed record FaultSiteAnnotation(
    FaultCode Code,
    DiagnosticCode PreventedBy,
    SourceSpan Site
);
```

This is the builder-side annotation, NOT the runtime `FaultSiteDescriptor`. The builder stamps `FaultSiteAnnotation?` on each opcode during Pass 4:
- `null` = proven safe, no runtime check needed
- non-null = unresolved obligation, defense-in-depth backstop

The builder later collapses `FaultSiteAnnotation` into `FaultSiteDescriptor(FaultCode, DiagnosticCode, int SourceLine)` for the runtime model.

---

## 5. How ProofLedger Connects to ProofSatisfaction (PE-G2)

The `ProofSatisfaction` DU (PE-G2) feeds into `ProofLedger` through the strategy dispatch:

```text
ProofRequirement (on TypedExpression)
    → ProofObligation (instantiated in Pass 1)
    → Strategy dispatch (Pass 2):
        Strategy 1: checks literal value against requirement
        Strategy 2: reads ProofSatisfactions from FieldModifierMeta,
                    DeclaredPresenceMeta, DeclaredQualifierMeta
        Strategy 3: reads guard constraints
        Strategy 4: reads guard field-to-field constraints
        Strategy 5: reads DeclaredQualifierMeta for qualifier comparison
    → ProofObligation.Disposition = Proved | Unresolved
    → If Unresolved: FaultSiteLink created
    → All obligations → ProofLedger.Obligations
    → All fault links → ProofLedger.FaultSiteLinks
```

The `ProofSatisfaction` entries on carriers are the _metadata the strategies read_. The `ProofObligation` records in the ledger are the _output the strategies produce_.

---

## 6. Full ProofLedger Shape — Final Definition

**File:** `src/Precept/Pipeline/ProofLedger.cs`

```csharp
using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;

public sealed record ProofLedger(
    ImmutableArray<ProofObligation> Obligations,
    ImmutableArray<FaultSiteLink> FaultSiteLinks,
    ImmutableArray<ConstraintInfluenceEntry> ConstraintInfluence,
    ImmutableArray<InitialStateSatisfiabilityResult> InitialStateResults,
    ImmutableArray<Diagnostic> Diagnostics
);

public sealed record ProofObligation(
    ProofRequirement Requirement,
    TypedExpression Site,
    ProofDisposition Disposition,
    ProofStrategy? Strategy,
    DiagnosticCode? EmittedDiagnostic
);

public enum ProofDisposition { Proved, Unresolved }

public enum ProofStrategy
{
    Literal = 1,
    DeclarationAttribute = 2,
    GuardInPath = 3,
    FlowNarrowing = 4,
    QualifierCompatibility = 5
}

public sealed record FaultSiteLink(
    ProofObligation Obligation,
    FaultCode FaultCode,
    DiagnosticCode DiagnosticCode,
    SourceSpan Site
);

public sealed record FaultSiteAnnotation(
    FaultCode Code,
    DiagnosticCode PreventedBy,
    SourceSpan Site
);

public sealed record ConstraintInfluenceEntry(
    ConstraintIdentity Constraint,
    ImmutableArray<string> ReferencedFields,
    ImmutableArray<EventArgReference> ReferencedArgs
);

public sealed record EventArgReference(string EventName, string ArgName);

public sealed record InitialStateSatisfiabilityResult(
    string StateName,
    bool IsSatisfiable,
    ImmutableArray<UnsatisfiedConstraint> Violations
);

public sealed record UnsatisfiedConstraint(
    ConstraintIdentity Constraint,
    string Reason
);
```

---

## 7. Downstream Impact — Compilation Record

The `Compilation` record in `src/Precept/Pipeline/Compilation.cs` already has `ProofLedger Proof`. No signature change is needed — the record consumes whatever shape `ProofLedger` has. However, when the Precept Builder is implemented, it must read:
- `Proof.FaultSiteLinks` → plant `FaultSiteDescriptor` backstops
- `Proof.ConstraintInfluence` → build `ConstraintInfluenceMap`

The `ProofEngine.Prove()` stub must be updated to return the expanded shape with empty arrays:

```csharp
public static ProofLedger Prove(SemanticIndex semantics, StateGraph graph) =>
    new(
        ImmutableArray<ProofObligation>.Empty,
        ImmutableArray<FaultSiteLink>.Empty,
        ImmutableArray<ConstraintInfluenceEntry>.Empty,
        ImmutableArray<InitialStateSatisfiabilityResult>.Empty,
        ImmutableArray<Diagnostic>.Empty
    );
```

---

## 8. Implementation Checklist — Dependency Order

1. **`src/Precept/Pipeline/ProofLedger.cs`** — Expand from stub to full shape. Add all 9 supporting types. This is Slice 0 — pure shape declarations, no logic.

2. **`src/Precept/Pipeline/ProofEngine.cs`** — Update stub to return the expanded `ProofLedger` with empty arrays. Build stays green.

3. **`docs/compiler/proof-engine.md` §5** — Update `ConstraintIdentity` shapes in spec to match source (`RuleIdentity(int RuleIndex)`, `EnsureIdentity(ConstraintKind, string? AnchorName, int EnsureIndex)`).

**Dependencies:** PE-G3 has no dependency on PE-G2. The ProofLedger shape is independent of the ProofSatisfaction DU — one is the output contract, the other is the input metadata vocabulary. Both must exist before the ProofEngine implementation can begin, but they can be implemented in any order.

---

## 9. Spec Corrections Required

### 9.1 ConstraintIdentity shapes (PE-G5)

The spec must be updated to match source:

| Field | Spec | Source (canonical) |
|-------|------|-------------------|
| `RuleIdentity` | `(string RuleName, int Index)` | `(int RuleIndex)` |
| `EnsureIdentity` | `(ConstraintKind, string? AnchorState, string? AnchorEvent, int Index)` | `(ConstraintKind, string? AnchorName, int EnsureIndex)` |

The source shapes are correct — they were implemented during TypeChecker work and have passing tests.

### 9.2 ProofStrategy enum member names

The spec uses `Modifier` for Strategy 2 and does not name Strategy 5. With PE-G2 locked:
- Strategy 2 → `DeclarationAttribute` (reads modifiers, presence, and qualifier carriers)
- Strategy 5 → `QualifierCompatibility`

---

## 10. Risk Assessment

**Low risk.** PE-G3 is entirely about shape declarations — no algorithmic decisions, no design ambiguity, no carrier-versus-carrier trade-offs. The spec already defines every type. The work is mechanical: declare the types, update the stub, verify the build.

Estimated effort: 1 implementation slice (Slice 0 shape). Same pattern as TypeChecker and GraphAnalyzer initial shape slices.
