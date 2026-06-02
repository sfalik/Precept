---
title: "Compile-Time vs Runtime Guarantee Claims: Documentation Audit"
status: "Audit — 2026-06-02"
author: Frank
scope: "docs/philosophy.md, README.md, docs/runtime/ — claims and framing only"
---

# Compile-Time vs Runtime Guarantee Claims: Documentation Audit

## What the Docs Claim

### Compile-Time Claims

| # | Claim | Source | Exact Text |
|---|-------|--------|------------|
| C1 | Unreachable states caught at compile time | philosophy.md | "Every declared state is reachable — there are no lifecycle positions that exist on paper but that no sequence of operations can reach." |
| C2 | Dead-end states caught at compile time | philosophy.md | "Every non-terminal state has a path forward — there are no dead ends where an entity gets stuck with no way to advance." |
| C3 | Required-state paths proven | philosophy.md | "Required states are guaranteed to lie on every path to completion — no entity can skip a mandatory step." |
| C4 | Division by zero is a compile-time impossibility | philosophy.md | "Division by zero, arithmetic overflow, empty collection access — these are not risks managed at runtime. They are compile-time impossibilities." |
| C5 | Proof of all expressions | philosophy.md | "The compiler does not trust that an expression will succeed — it proves it will." |
| C6 | Clean compile = no faults | philosophy.md | "A definition that compiles without diagnostics has no unproven evaluation faults, no unreachable business process states, and no structural dead ends." |
| C7 | Proof engine catches specific codes | README.md | "Compile-time proof engine — a unified interval + relational inference engine that proves numeric properties at compile time... Catches divisor safety (C92/C93), sqrt safety (C76), assignment constraint violations (C94)..." |
| C8 | Field-state guarantees at compile time | README.md | "Compile-time field-state guarantees — omitted fields cannot be read in state-anchored expressions (D130)..." |
| C9 | No diagnostics = no evaluator faults | evaluator.md | "No diagnostics means no evaluator faults — compiler proofs are intended to eliminate runtime fault paths, leaving evaluator faults as defense-in-depth only." |

### Runtime Claims

| # | Claim | Source | Exact Text |
|---|-------|--------|------------|
| R1 | Invalid configurations are structurally impossible | philosophy.md | "making invalid configurations structurally impossible" |
| R2 | No operation can produce a rule violation | philosophy.md | "No operation can produce a result that violates a declared rule — the invalid configuration is not reachable." |
| R3 | Atomic evaluation | philosophy.md | "Whether the rule is about what the entity *is* or what the operation *brings in*, it evaluates atomically — the change and the check are the same act." |
| R4 | Determinism | philosophy.md | "The engine is deterministic — same definition, same data, same outcome. Nothing is hidden." |
| R5 | Restore bypasses access modes | runtime-api.md | "Access modes are bypassed. Fields that are `readonly` in the restored state were `editable` when previously written." |
| R6 | Restore evaluates constraints | runtime-api.md | "Restore reconstitutes a Version from persisted data... and evaluates constraints against the restored state." |
| R7 | Faults are defense-in-depth only | fault-system.md | "Defense-in-depth, not a primary error path" / "it classifies failures that *should never occur* but must be handled if they do" |
| R8 | Faults may occur from external bypass | fault-system.md | "e.g. data loaded from external sources bypassing the compile-time check, or a proof engine gap" |
| R9 | FromJson bypasses constraint validation | evaluator.md Decision 5 | "FromJson bypasses both access-mode checks and constraint validation. It is a pure hydration operation." |
| R10 | Restore enforces constraint checks | evaluator.md §10 Output Guarantees | "Access mode bypass for Restore — Restore bypasses access-mode checks but enforces constraint checks." |

### System-Level Claims

| # | Claim | Source | Exact Text |
|---|-------|--------|------------|
| S1 | Three-layer enforcement model | runtime-api.md | "Precept enforces field constraints across three layers: 1. Compile-time diagnostics... 2. Ingress validation... 3. Defense-in-depth evaluator faults..." |
| S2 | Prevention not detection | philosophy.md | "Prevention, not detection. No errors. No bugs. Business logic cannot produce a wrong answer." |
| S3 | Governance not validation | philosophy.md | "This is not validation — it is governance." |
| S4 | StaticallyPreventable chain | fault-system.md | "for every way the evaluator can fail at runtime, there exists a compile-time diagnostic that prevents it." |
| S5 | Inspection–commit agreement | evaluator.md §10 | "InspectFire and Fire execute the same plans. A row that InspectFire marks Prospect.Certain will be the winning row when Fire is called with the same inputs." |

---

## Consistent Claims

### Determinism (C1 consistent across all docs)

- philosophy.md: "same definition, same data, same outcome"
- evaluator.md §10: "Same Precept + same Version + same operation + same inputs → same outcome. No randomness, no external state."
- evaluator.md §4: "Determinism: 100%"

Precise because it names all four inputs (definition, entity state, operation type, inputs) and excludes all external dependencies.

### Immutability / No In-Place Mutation (consistent)

- evaluator.md: "Input Version is never mutated. Output Version (in success outcomes) is a fresh instance."
- runtime-api.md: "Operations return new instances."

Precise because the structural guarantee (sealed record, no mutation methods) is named explicitly.

### Inspection–Commit Agreement (consistent)

- runtime-api.md §Correctness Invariant: "InspectFire runs the same pipeline as Fire — same guard evaluation, same action chain, same constraint checking."
- evaluator.md §10: same claim with identical precision.
- evaluator.md Decision 3: rationale explicitly rejects alternative (separate inspection evaluator).

### StaticallyPreventable Chain (consistent across fault-system.md and evaluator.md)

Both docs describe the same enforcement chain with the same five-layer mechanism. fault-system.md provides the detailed walkthrough; evaluator.md references it. No contradiction.

---

## Ambiguous Claims

### A1. "No diagnostics means no evaluator faults"

**Source:** evaluator.md Non-Negotiable Rules box (line 19)

**Exact text:** "No diagnostics means no evaluator faults — compiler proofs are intended to eliminate runtime fault paths, leaving evaluator faults as defense-in-depth only."

**Ambiguity:** The sentence uses "intended to" as a qualifier, then the surrounding box presents this as a "Non-Negotiable Rule." A reader cannot determine whether:
1. This is an absolute structural guarantee (clean compile = zero faults), or
2. This is the design intent with acknowledged gaps (faults are "should never" rather than "cannot").

The same doc later acknowledges the Restore path accepts external data that bypasses compilation — and fault-system.md explicitly says faults handle "data loaded from external sources bypassing the compile-time check, or a proof engine gap." So the clean-compile guarantee has two exceptions the headline statement does not mention.

### A2. "Invalid configurations are structurally impossible"

**Source:** philosophy.md (multiple instances), README.md line 8

**Ambiguity:** Does "structurally impossible" mean:
1. The type system prevents them (compile-time), or
2. The runtime rejects them before they persist (runtime enforcement), or
3. Both together (the three-layer model)?

philosophy.md uses this phrase without explaining which layer provides it. The README uses it in the opening paragraph. The three-layer enforcement model (only in runtime-api.md) clarifies that layers 2 and 3 are also needed — but a reader of philosophy.md alone would infer this is purely compile-time.

### A3. "Prevention, not detection"

**Source:** philosophy.md

**Exact text:** "No errors. No bugs. Business logic cannot produce a wrong answer."

**Ambiguity:** This is marketing copy that conflates compile-time structural proofs (provably preventing certain fault classes) with runtime enforcement (rejecting operations that would violate constraints). The runtime enforcement IS detection — it detects that a proposed mutation would violate a constraint and rejects it. Philosophy.md frames both as "prevention" without distinguishing the mechanism. A technically precise reader would note that runtime `ConstraintsFailed` is literally detecting an invalid proposed state and rejecting it — which is detection.

### A4. "Compile-time impossibilities"

**Source:** philosophy.md

**Exact text:** "Division by zero, arithmetic overflow, empty collection access — these are not risks managed at runtime. They are compile-time impossibilities."

**Ambiguity:** This is stated as absolute. But the fault system explicitly has `FaultCode.DivisionByZero`, `FaultCode.NumericOverflow`, and `FaultCode.CollectionEmptyOnAccess` — runtime codes for these exact failures. The existence of runtime fault codes for "compile-time impossibilities" is explained by the defense-in-depth framing in fault-system.md, but philosophy.md states the absolute without the qualifier. A reader of philosophy.md alone would conclude these literally cannot happen at runtime. The reality is they *should not* happen — and the system has backstops in case they do.

---

## Contradictory Claims

### CONTRADICTION 1: Restore Constraint Validation

**Doc A — runtime-api.md (§Restoration, line 266):**
> "Restore reconstitutes a Version from persisted data... and evaluates constraints against the restored state."

**Also runtime-api.md (§Output Guarantees, line 1682):**
> "Access mode bypass for Restore — Restore bypasses access-mode checks but enforces constraint checks."

**Doc B — evaluator.md (Decision 5, line 1766-1770):**
> "FromJson bypasses both access-mode checks and constraint validation. It is a pure hydration operation — the inverse of ToJson."

**Also evaluator.md (Decision 6, line 1783-1784):**
> "No constraint evaluation pipeline runs."

**The contradiction:** runtime-api.md says Restore enforces constraints (producing `RestoreConstraintsFailed` on violation). evaluator.md says Restore/FromJson bypasses constraint validation entirely.

**Assessment:** This appears to be a naming/design-evolution issue. runtime-api.md uses `Restore` (the current API name) with `RestoreOutcome` (including `RestoreConstraintsFailed`). evaluator.md uses `FromJson` (an earlier/internal name) and says it bypasses constraints. These may describe two different operations (public `Restore` which checks constraints vs. internal `FromJson` which doesn't), OR one doc is outdated. Either way, the docs contradict each other on whether restored entities are constraint-checked.

### CONTRADICTION 2: "No errors. No bugs." vs. Structured Error Outcomes

**philosophy.md:** "No errors. No bugs. Business logic cannot produce a wrong answer."

**runtime-api.md / evaluator.md:** Nine `EventOutcome` variants, four `UpdateOutcome` variants — multiple of which represent "the operation was rejected" (Rejected, ConstraintsFailed, Unmatched, FieldNotEditable).

**The contradiction is semantic:** philosophy.md says "no errors" but the runtime is architected around producing structured rejection outcomes. These are explicitly described as "not errors — they are expected business outcomes" in evaluator.md (§9). The contradiction is in framing: philosophy.md uses absolutist consumer-facing language ("no errors") that conflicts with the technical reality that operations frequently produce non-success outcomes that callers must handle. A reader coming from philosophy.md would not expect to pattern-match on 9 failure variants.

---

## Missing Qualifiers

### MQ1. Clean Compile Guarantee — Missing Restore Exception

**Unqualified claim (evaluator.md):** "No diagnostics means no evaluator faults"

**Missing qualifier:** This guarantee holds only for operations where all data entered through the compile-time-checked contract (Create, Fire, Update). It does NOT hold for Restore, which accepts externally-persisted data that may have been written under a different definition version, or data injected by a system that bypassed the Precept contract entirely. fault-system.md acknowledges this ("data loaded from external sources bypassing the compile-time check") but the headline guarantee in evaluator.md does not carry the qualifier.

### MQ2. Clean Compile Guarantee — Missing Proof Engine Gap Qualifier

**Unqualified claim (philosophy.md):** "A definition that compiles without diagnostics has no unproven evaluation faults"

**Missing qualifier:** fault-system.md explicitly lists "a proof engine gap" as a scenario where faults may occur despite a clean compile. The proof engine may have coverage gaps (not all possible fault paths are proven). philosophy.md states the guarantee as absolute; the implementation docs acknowledge it's aspirational within current proof coverage.

### MQ3. "Structurally impossible" — Missing Ingress Validation Layer

**Unqualified claim (philosophy.md, README.md):** "invalid configurations are structurally impossible"

**Missing qualifier:** runtime-api.md's three-layer model reveals that Layer 2 (ingress validation at `TypeRuntimeMeta`/`TypeRuntime`) is needed for cases the proof engine cannot prove statically. This means some "impossibilities" are actually enforced at runtime ingress — not compile-time. The consumer-facing docs (philosophy.md, README.md) don't mention this layer at all. A reader would not know that some enforcement is runtime-based.

### MQ4. Scope of "no code path outside the contract"

**Unqualified claim (philosophy.md):** "no code path outside the contract, no window where an invalid configuration can exist"

**Missing qualifier:** The Restore path is explicitly designed to bypass some contract enforcement (access modes always, constraints per the unresolved contradiction above). If Restore bypasses constraints (per evaluator.md), then there IS a code path that can produce a Version with field values that don't satisfy current rules — that's what the "schema drift" scenario in runtime-api.md is acknowledging. The absolutist claim doesn't qualify this.

---

## Recommended Corrections

### philosophy.md

1. **Line ~53 (compile-time impossibilities):** Change:
   > "Division by zero, arithmetic overflow, empty collection access — these are not risks managed at runtime. They are compile-time impossibilities."

   To:
   > "Division by zero, arithmetic overflow, empty collection access — these are statically proven at compile time. The runtime carries defense-in-depth backstops (see fault-system.md) for data that bypasses the compiler, but for definitions that compile without diagnostics, these paths should never execute."

2. **Line ~49 ("No errors. No bugs."):** This is marketing copy that creates a mismatch with the technical docs. Consider either:
   - Qualifying: "No errors in the contract itself. No bugs in the rule enforcement. Business logic declared in the precept cannot produce a wrong answer — the engine rejects any operation that would violate the declared rules."
   - Or add a sentence: "The engine expresses business rejection as structured outcomes, not silent failures."

3. **Line ~53 ("A definition that compiles without diagnostics..."):** Add qualifier:
   > "A definition that compiles without diagnostics has no unproven evaluation faults within the proof engine's current coverage, no unreachable business process states, and no structural dead ends."

### README.md

4. **Line 8 ("invalid configurations are structurally impossible"):** No change needed — this is a summary statement. But the "Learn More" table should route readers to the three-layer model. Consider adding after line 104:
   > "Precept's guarantees operate across three enforcement layers — see the [Runtime API](docs/runtime/runtime-api.md#three-layer-enforcement-model) for the full model."

### runtime-api.md

5. **§Restoration vs. evaluator.md Decision 5/6:** Resolve the Restore/FromJson contradiction. Either:
   - Confirm that `Restore` checks constraints (per runtime-api.md and the `RestoreConstraintsFailed` outcome variant), and clarify that evaluator.md Decision 5/6 describe a *different* internal operation (`FromJson` as a raw hydration bypass), or
   - Confirm that `Restore` does NOT check constraints, and remove `RestoreConstraintsFailed` from the `RestoreOutcome` type and the §Output Guarantees table.

   **This is a decision item — see inbox.**

### evaluator.md

6. **Non-Negotiable Rules box, "No diagnostics means no evaluator faults":** Add qualifier:
   > "No diagnostics means no evaluator faults *for operations where all data entered through the compile-time-checked contract* (Create, Fire, Update with validated ingress). Data restored from external persistence (Restore) may trigger defense-in-depth faults if the stored values predate a definition change."

### fault-system.md

7. **No changes needed.** fault-system.md is the most precisely qualified of all five docs. It correctly frames faults as defense-in-depth, names the scenarios where they may occur despite clean compilation, and documents the structural chain. It is the doc that all other docs should reference.
