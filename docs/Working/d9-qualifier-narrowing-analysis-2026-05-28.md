# Deep Analysis — D9 Open-Field Discrete-Equality Qualifier Narrowing

**Date**: 2026-05-28
**Status**: Analysis (feeds a forthcoming `/lifecycle-2-design`)
**Origin**: Surfaced during Phase 6 W-B (composite period basis). Began as "fix PRE0053 so `period.dimension == 'date'` compiles + narrows." Deep analysis (3 independent lenses: requirements / soundness / architecture) revealed the scope is far larger.

## THE HEADLINE FINDING (all three lenses, independently)

**There is no working guard-based qualifier-narrowing path. The entire spec § D9 surface is unbuilt — for every axis, including currency.**

The premise this work started from — "currency narrowing works, so `.dimension` just needs wiring in" — is **false**, verified three ways:

- **Empirical** (Lens A, via `precept_compile`): the spec's OWN D9 acceptance example fails today:
  ```
  field Payment as money
  field AccountBalance as money in 'USD'
  when Payment.currency == 'USD' -> set AccountBalance = AccountBalance + Payment
  → PRE0141 (currency qualifier unproven)
  ```
  The open-quantity example fails identically (PRE0141); the open-period example fails earlier (PRE0053).
- **Structural** (Lens C): `ReturnsQualifier` is consumed in exactly two places — interpolation-slot resolution (`AssignmentQualifiers.cs:313`) and hover (`RichHoverFactory.cs:995`). **No `ProofEngine.*` file reads it.** Qualifier proving (`ResolveQualifierOnAxis`, `Qualifiers.cs:256-435`) resolves purely from *declared* field qualifiers — it takes no guard context and never consults in-scope guards. `TryGuardInPathProof` (the guard path) only matches `Numeric`/`Presence` requirements — a `QualifierChainProofRequirement` cannot enter it.
- **Spec-drift** (Lens A): § D9 "Mechanism" (`business-domain-types.md:1207-1216`) describes `$eq:` markers, a `StaticValueKind` symbol table, `ApplyNarrowing`/`ApplyAssignmentNarrowing` "from #106" — **none of these symbols exist in the codebase** (grep returns zero). `:762` "the registry is partitioned by parent type — the type checker knows which partition to validate against" is **false in code** (the dimension validator is partition-blind). These are doc claims for infrastructure that was never built.

**What actually works today** is a different mechanism: static `in`-qualifier seeding (a `money in 'USD'` field carries its qualifier) + interpolation propagation. Guard-driven discrete-equality narrowing is **greenfield**.

**Consequence**: "implement everything" = build a soundness-critical proof-engine narrowing layer across all 11 (type, accessor) tuples that the spec promised but never delivered. This is **not** a W-B sub-fix. See § Scope/Stakes/Phase.

## Requirements (Lens A)

### The full (type, accessor, axis) matrix — all broken today
| Type | Accessor | Axis | `ReturnsQualifier`? | Works (guard-narrow)? |
|---|---|---|---|---|
| money | `.currency` | Currency | yes (Types.cs:552) | **No** |
| quantity | `.unit` | Unit | yes (:590) | **No** |
| quantity | `.dimension` | Dimension | **no** (:591) | **No** |
| period | `.basis` | (String — no axis) | **no** (:480) | **No** |
| period | `.dimension` | TemporalDimension | **no** (:481) | **No** (+ PRE0053) |
| price | `.currency`/`.unit`/`.dimension` | 3 axes on one field | partial (:637/638/639) | **No** |
| exchangerate | `.from`/`.to` | From/ToCurrency | yes (:657/658) | **No** |
| unitofmeasure | `.dimension` | Dimension | **no** (:607) | **No** (scope OQ) |

### Six discharge sites that must consult a narrowing fact
A — assignment qualifier validation (`AssignmentQualifiers.cs:120-174`, PRE0141); B — arithmetic qualifier-compat/chain proof (`Qualifiers.cs:10-35`, `:256-435`, PRE0114); C — `DimensionProofRequirement` via `ResolvePeriodDimension` (`Strategies.cs:44-49,227-241`, PRE0113); D — dimensional-product vector resolution (`Qualifiers.cs:62-99`); E — the guard producer itself (`ExtractGuardLeafConstraints`, `Strategies.cs:779-862` — no member-access-equality arm exists); F — interpolation slot resolution (open-field `'{X.currency}'` also fails today).

### Bundled PRE0053 partition fix
The dimension typed-constant validator (`DimensionValidation`, one `ClosedSetValidation` over UCUM `DimensionCatalog`) must become **partition-aware**: period→{date,time,datetime}, quantity/uom/price→UCUM. The `'time'` collision (UCUM `time` at `DimensionCatalog.cs:13` vs temporal `time`) is resolved ONLY by partition selection from the LHS parent type — never by widening the shared set. `'datetime'` is compare/return-only (not a declarable `of` constraint).

### Acceptance anchors (spec examples that MUST compile after)
The three § D9 examples (open money+currency `:1237`, open quantity+dimension `:1250`, open period+dimension `:1261`) plus the dead-end `when Gap.basis == 'hours'` enabling `price/period` cancellation (`:961`).

## Soundness (Lens B) — the non-negotiable rules

The numeric discrete-equality narrowing path (F-LANG-BIZ-08) is the **only** structural precedent; replicate its soundness discipline:

1. **Context-scoped sourcing** — facts come ONLY from `obligation.Context`'s own guard (`Intervals.cs:390-396` pattern), never a global guard index. This is what confines a fact to its branch/row/state.
2. **All-branches discharge** — an OR guard discharges a positive obligation only if *every* branch independently establishes the *same* value (`Strategies.cs:660-677`). Never collapse OR to one arm (`// OR: do NOT decompose`, `Strategies.cs:766`).
3. **Subject-identity + axis match** — fact `(field, axis, value)` discharges only when both field and axis match the obligation's subject.
4. **Value-exact discharge (THE #1 TRAP)** — `X.currency == 'USD'` then assign to `money in 'EUR'` must STILL FAIL. The discharge compares the narrowed *value* against the *required* value (mirror `ValueSatisfiesRequirement`, `Strategies.cs:881`), never short-circuits on "X is narrowed." A naive "has-a-narrowing → proven" implementation passes the happy path and silently admits every mismatch — a direct Principle-1 violation.
5. **Literal RHS only** — `X.currency == Y.currency` (field-to-field) seeds no positive value fact.
6. **Negative equality (`!=`, `is not set`) narrows nothing usable** for a positive proof (open complement, not a singleton).
7. **Compose-by-intersection with declared qualifiers** — a guard may refine an open axis but never contradict-and-win over a declared one; a contradiction branch is unsatisfiable, not a discharge.
8. **Period `.dimension == 'date'` is sound as the runtime value's dimension** but must discharge only dimension-axis obligations, never unit/currency; `datetime` (composite) discharges nothing for Date/Time obligations; `PeriodDimension.Any` never satisfies a concrete obligation.

Full 11-item failure-mode→required-test catalog in Lens B output; #1 (value-mismatch discharge) and #2 (OR-arm collapse) are the highest-risk.

## Architecture (Lens C) — recommended shape

- **Fact representation**: a new `QualifierNarrowingConstraint(string Field, QualifierAxis Axis, string Value)` (DU/parallel record) — NOT extending the decimal-only `GuardConstraint` (`ProofEngine.cs:29`; mixing numeric subsumption with discrete string identity is the nullable-field-on-flat-record smell CLAUDE.md forbids). Parallel branch extraction mirroring `ExtractGuardBranchesCore`'s AND/OR logic (so OR-union / AND-cross-product soundness comes for free).
- **Seed**: new arm in `ExtractGuardLeafConstraints` matching `TypedMemberAccess{ReturnsQualifier != None} == TypedTypedConstant`, `Equals`-only.
- **Discharge**: a **dedicated new strategy** `TryQualifierGuardNarrowingProof` that pulls the guard from `obligation.Context` (as `TryGuardInPathProof` does) and discharges qualifier obligations whose subject+axis+value match — keeping `ResolveQualifierOnAxis` declaration-pure (avoids threading a guard param through its many callers).
- **`.basis`**: do NOT fake a `ReturnsQualifier` on a String-returning accessor — model as discrete-string-accessor narrowing; reuse the W-A canonicalizer on the RHS (else non-canonical RHS silently never matches); confirm a real consumer obligation (D15 cancellation) exists or defer.
- **Period `.dimension` derivation gap**: `ResolvePeriodDimension` (TemporalUnit→DerivedDimension) exists but is reachable ONLY from the `of`-path. The new discharge must call it (or a shared helper) — the derivation is missing on the qualifier-discharge path.
- **PRE0053 partition fix**: thread LHS parent-type into dimension typed-constant validation; unify the `of`-path and `==`-path on one partition-aware validator to avoid drift.

## Scope / Stakes / Phase (Lens C, unanimous)

- **Stakes: HIGH, soundness-critical.** A false discharge = an invalid configuration declared structurally impossible when it isn't — the exact guarantee Precept exists to provide (`philosophy.md`).
- **Its own phase, NOT a rider on F-LANG-BIZ-07.** F-LANG-BIZ-07/PRE0053 is type-checker/validation surface; this is proof-engine discharge surface. The two should not share a review. The composite-basis work (W-A done; W-B `.dimension` value/return done) stands on its own.
- **Separable layers**, smallest-to-largest:
  1. **PRE0053 partition-aware dimension-literal validation** — makes `period.dimension == 'date'/'datetime'` *compile* (and rejects cross-partition). Type-checker-only, medium stakes, no soundness surface. Unblocks the W-B guard from compiling.
  2. **The D9 guard-narrowing layer** — makes those guards *narrow* (discharge downstream obligations). Proof-engine, HIGH stakes, greenfield, all axes.
- **Spec drift to correct regardless**: § D9 Mechanism (`:1207-1216`) describes non-existent infra; `:762` partition claim is false. The spec oversells D9 as implemented.

## What the design must resolve (open questions)
Fact vehicle (new record vs DU base); where facts live so both type-checker (Site A) and proof engine (Sites B/C/D) see them (currently separate passes); dimension-validator threading; `.basis` representation (no axis); non-canonical/composite basis RHS handling; `!=`/else-branch (non-actionable); unitofmeasure `.dimension` scope; `datetime` accept-but-inert; interpolation uniformity.

## Recommendation to owner (see chat)
Split into two designs/phases: (1) the PRE0053 partition fix (small, the original W-B unblock — `period.dimension` guards compile); (2) the D9 guard-narrowing layer (large, HIGH-stakes, own phase, all axes — the spec's unbuilt promise). Both are real and should be built ("implement everything"), but as separate scoped efforts, not one design — because mixing a validation fix with a soundness-critical proof-engine layer under one review is exactly the conflation prior reviews caught.
