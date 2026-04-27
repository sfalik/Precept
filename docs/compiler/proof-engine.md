# Proof Engine

## Status

| Property | Value |
|---|---|
| Doc maturity | Stub |
| Implementation state | Diagnostics-only stub (catalog-side proof vocabulary exists; ProofEngine.Prove not implemented) |
| Source | `src/Precept/Pipeline/ProofEngine.cs`, `src/Precept/Pipeline/ProofLedger.cs` |
| Upstream | SemanticIndex + StateGraph, catalog metadata (Operations, Functions, Types, Diagnostics, Faults) |
| Downstream | Compilation (proof ledger), Precept Builder (fault backstops) |

---

## Overview

The proof engine is the last analysis stage before the Precept Builder. It discharges statically preventable runtime hazards using a bounded, four-strategy set. If it proves an operation is safe, no runtime check is needed. If it cannot, the compiler emits a diagnostic. Its key design choice: proof is bounded — four strategies only, no general SMT solver — and the proof ledger does NOT cross the compile-runtime boundary (only fault-site residue crosses).

---

## Responsibilities and Boundaries

**OWNS:** Proof obligation instantiation from catalog `ProofRequirements`, obligation discharge via four strategy set, diagnostic emission for unresolvable obligations, initial-state satisfiability checking, `FaultSiteLink` production, `ConstraintInfluenceMap` production.

**Does NOT OWN:** Semantic resolution (TypeChecker), lifecycle topology (GraphAnalyzer), runtime execution (Precept Builder / Evaluator).

---

## Right-Sizing

The proof engine is bounded by design — four strategies only, no external solver. This is a deliberate scope limit: general SMT solving would add non-deterministic verification times and external dependencies. The obligation space is bounded because the DSL expression language is intentionally constrained. If the four strategies prove insufficient for real programs, a fifth strategy (relational pair narrowing) would be added — not a general solver.

---

## Inputs and Outputs

**Input:**
- `SemanticIndex` — typed expressions with resolved `ProofRequirement` attachments
- `StateGraph` — `ProofForwardingFact` entries from structural violations
- Catalog metadata: Operations, Functions, Types, Diagnostics, Faults

**Output:**
- `ProofLedger` — `ProofObligation` entries with dispositions, `FaultSiteLink`s, `ConstraintInfluenceEntry` records, `InitialStateSatisfiabilityResult`s, `ObligationCoverageRecord`s, diagnostics

---

## Architecture

The proof engine operates in two passes:

1. **Obligation instantiation:** For each semantic site in the `SemanticIndex` with attached `ProofRequirement` catalog entries, instantiate a `ProofObligation` carrying the site, requirement, and available evidence (guard-in-scope, literal values, modifier chain).
2. **Obligation discharge:** For each obligation, try the four strategies in order. The first strategy that succeeds marks the obligation as `Proved`. An obligation that fails all four strategies emits the `DiagnosticCode` from the `ProofRequirement` and marks the obligation as `Unresolved`.

---

## Component Mechanics

### Four Proof Strategies

| Strategy | How it works |
|---|---|
| **1. Literal proof** | The value at the proof site is a known compile-time literal; outcome is directly knowable |
| **2. Modifier proof** | Value flows through a catalog-defined modifier chain with statically bounded output |
| **3. Guard-in-path proof** | An enclosing guard expression statically establishes a sufficient range or type constraint |
| **4. Straightforward flow narrowing** | A guard in the same transition row establishes a constraint on a field available to action-chain expressions |

### Proof/Fault Chain

```
catalog metadata → ProofRequirement → ProofObligation → DiagnosticCode → FaultCode → FaultSiteDescriptor
```

- `ProofRequirement` — declared in catalog: what must be proved at each usage site
- `ProofObligation` — instantiated by proof engine: specific site, specific requirement
- `DiagnosticCode` — emitted if obligation unresolved: authoring-time diagnostic
- `FaultCode` — the runtime counterpart: what fault would occur if this site reached runtime
- `FaultSiteDescriptor` — planted by Precept Builder: defense-in-depth backstop

### Initial-State Satisfiability

The proof engine verifies that the initial state's declared constraint expressions are satisfiable given the initial field values. An unsatisfiable initial state means the entity could never be created — a compile-time error.

### ConstraintInfluenceMap

For each constraint, the proof engine produces a `ConstraintInfluenceEntry` recording which fields contribute to the constraint expression. The Precept Builder organizes these into a `ConstraintInfluenceMap` — a precomputed artifact that enables AI agents to reason causally about constraint satisfaction.

---

## Dependencies and Integration Points

- **SemanticIndex** (upstream): typed expressions with ProofRequirement attachments
- **StateGraph** (upstream): ProofForwardingFacts from structural violations
- **Catalog metadata** (upstream): Operations.ProofRequirements, Functions.ProofRequirements, TypeAccessors.ProofRequirements, Faults (FaultCode↔DiagnosticCode correspondence)
- **Compilation** (downstream): receives ProofLedger as one of the sealed pipeline artifacts
- **Precept Builder** (downstream): reads FaultSiteLinks to plant FaultSiteDescriptor backstops in the executable model

---

## Failure Modes and Recovery

The proof engine accumulates all diagnostics. Unresolved obligations produce diagnostics without aborting; the full obligation set is retained in the `ProofLedger` regardless of disposition. An empty `SemanticIndex` (no typed expressions with proof requirements) produces an empty obligation ledger with no diagnostics.

---

## Contracts and Guarantees

- Every `ProofRequirement` declared in any catalog entry for any used operation/function/accessor has a corresponding `ProofObligation` in the `ProofLedger`.
- Every `ProofObligation` has a disposition: `Proved` (one of four strategies succeeded) or `Unresolved` (emitted diagnostic).
- Every `FaultSiteLink` in the `ProofLedger` has a 1:1 correspondence with an `Unresolved` obligation and a `FaultCode` that carries `[StaticallyPreventable(DiagnosticCode)]`.

---

## Design Rationale and Decisions

TBD — design rationale section to be populated during implementation. Key decision: four-strategy bounded set rather than general SMT solver, chosen for predictability, auditability, and zero external dependencies.

---

## Innovation

- **Catalog-declared proof obligations:** Operations, functions, accessors, and actions declare safety requirements as catalog metadata. The proof engine reads these — it maintains no hardcoded obligation lists. Adding an operation with a division-by-zero hazard requires only a `NumericProofRequirement` catalog attachment.
- **Roslyn-enforced `FaultCode`↔`DiagnosticCode` correspondence:** Every `FaultCode` carries a `[StaticallyPreventable(DiagnosticCode)]` attribute. Roslyn analyzers (PRECEPT0001, PRECEPT0002) enforce that every evaluator failure path routes through a classified `FaultCode`, and every `FaultCode` links to its prevention `DiagnosticCode`. This makes fault–diagnostic correspondence a build-time invariant.
- **Bounded, non-extensible strategy set:** Four strategies only, each a simple predicate — not a solver framework. This makes proof predictable, auditable, and implementable without external dependencies.
- **Compile-time satisfiability:** The proof engine guarantees initial-state configurations are satisfiable at compile time — no state machine library or rules engine in this category provides this.

---

## Open Questions / Implementation Notes

1. `ProofEngine.Prove` throws `NotImplementedException` — catalog-side proof vocabulary exists (`ProofRequirements`, `Constraints` catalogs), but `ProofObligation` instantiation and strategy evaluation not implemented.
2. Validate four-strategy coverage against all 20 sample files in `samples/` before committing to no fifth strategy. Cross-field comparison obligations (e.g., `ApprovedAmount <= RequestedAmount`) are the highest-risk case — confirm guard-in-path covers them or identify the gap.
3. Define `ProofObligation`, `FaultSiteLink`, `ConstraintInfluenceEntry`, `ObligationCoverageRecord` shapes as sealed records before implementing.
4. Confirm PRECEPT0001/PRECEPT0002 Roslyn analyzer scope: do they run on the Precept source itself, or on host-application code?
5. Initial-state satisfiability: confirm this check is blocked on `TypeChecker.Check` being implemented (needs resolved initial field values and initial-state constraint expressions).
6. `ConstraintInfluenceMap`: confirm this is produced by the ProofEngine (influence analysis) or by the Precept Builder (reorganization). Current design doc places it under `ProofLedger`.

---

## Deliberate Exclusions

- **No SMT solver:** The bounded strategy set is intentional. General proof is not a goal.
- **No runtime obligation checking:** The `ProofLedger` does NOT cross the compile-runtime boundary. Only `FaultSiteDescriptors` (defense-in-depth residue) cross into the runtime model.
- **No constraint evaluation:** That is the evaluator's domain. ProofEngine checks statically; the evaluator enforces dynamically.

---

## Cross-References

| Topic | Document |
|---|---|
| Full ProofLedger design, proof/fault chain, strategy set | `docs/compiler-and-runtime-design.md §8` |
| SemanticIndex input | `docs/compiler/type-checker.md` |
| StateGraph input (ProofForwardingFacts) | `docs/compiler/graph-analyzer.md` |
| FaultSiteLinks consumer (fault backstops) | `docs/runtime/precept-builder.md` |
| ProofRequirements and Constraints catalogs | `docs/language/catalog-system.md` |

---

## Source Files

| File | Purpose |
|---|---|
| `src/Precept/Pipeline/ProofEngine.cs` | Proof engine implementation — `ProofEngine` static class with `Prove(SemanticIndex, StateGraph)` entry point |
| `src/Precept/Pipeline/ProofLedger.cs` | `ProofLedger` — obligation ledger artifact |
