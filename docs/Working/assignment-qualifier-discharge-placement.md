---
status: Locked 2026-05-29
phase-target: Phase 6 (D9 S1) — resolves the D9 design's G3
comparable-systems-research-status: strong — grounded in two in-tree surveys (flow-sensitive-check-placement, type-proof-stage-contract) that survey 11+ systems with verbatim excerpts; cited per-decision
sources-consulted:
  - docs/compiler/proof-engine.md § Decision 3 (:63,:511,:521) — "Obligations Stamped by Type Checker, Not Identified by Proof Engine"; the locked contract this design regularizes toward
  - docs/compiler/diagnostic-system.md:178 — the documented PRE0141 (type-stage) / PRE0114 (proof-stage) split this design updates
  - src/Precept/Pipeline/TypeChecker.Expressions.AssignmentQualifiers.cs:88-118 — ValidateResolvedQualifierAxes; the Unknown arm that emits PRE0141 today
  - src/Precept/Language/Actions.cs:257 — GenerateIntervalContainmentObligations (the existing dynamic-obligation-on-`set` precedent)
  - research/architecture/compiler/type-proof-stage-contract-survey.md — type-checker↔verifier contract across 11 systems; self-derive vs VC-gen
  - research/architecture/compiler/flow-sensitive-check-placement-survey.md — placement + diagnostic staging of flow-sensitive checks
  - docs/Working/d9-qualifier-narrowing-design.md § G3 — the under-specified gap this resolves
---

# Open-field assignment-qualifier compatibility: placement & diagnostic stage

## Goal
When done, `when X.currency == 'USD' -> set UsdField = X` (open `money` field narrowed by a guard, assigned directly to a constrained field) compiles and proves — because the assignment-qualifier compatibility check is a **stamped proof obligation discharged by the proof engine** (via the D9 narrowing strategy), not a type-checker-immediate emit; demonstrated by `precept_compile` accepting the narrowed assignment and rejecting the unnarrowed / value-mismatched one.

## Scope
- **In scope**: relocate the open-field (`Unknown`-axis) case of assignment-qualifier compatibility from a type-checker-immediate PRE0141 emit to a **stamped obligation** the proof engine discharges; update `diagnostic-system.md:178` to reflect PRE0141 as a proof-stage obligation diagnostic; record the general type↔proof-contract direction (stay catalog/stamp; no VC-gen/IVL).
- **Out of scope**: the narrowing discharge mechanism itself (D9 S1 — built); the `Resolved`-but-mismatched definite-error path (stays a type-immediate error — narrowing cannot rescue a declared mismatch); collection/numeric obligations (already stamped).
- **Deferred to future**: wholesale migration of any *other* type-immediate qualifier checks to obligations — only the open-field assignment case is in scope.

## Philosophy Alignment

| Principle | Affected? | How served (1 sentence + cite) | Tension | Tradeoff |
|---|---|---|---|---|
| 1. Prevention not detection | Y | The guarded open-field assignment is proven safe at compile time (or rejected); narrowing only discharges what the guard establishes (`philosophy.md` Prevention). | N/A | N/A |
| 2. One file, complete rules | N | No cross-file surface. | N/A | N/A |
| 3. Determinism | Y | Discharge is a deterministic function of the typed tree + guard; **no IVL/SMT** is introduced (the determinism reason IVL was rejected — `proof-engine.md` opaque-solver rejection). | N/A | N/A |
| 4. Full inspectability | Y | The assignment check becomes a stamped obligation with proof attribution, like every other obligation (`proof-engine.md` Decision 3). | N/A | N/A |
| 5. Keyword-anchored readability | N | No syntax change. | N/A | N/A |
| 6. Governance not validation | Y | `when X.currency == 'USD'` becomes a governance predicate that gates the assignment, not a no-op. | N/A | N/A |
| 7. Compile-time totality | Y | Open-field assignment gains a compile-time proof or a rejection. | N/A | N/A |
| 8. Honesty about approximation | N | No approximation. | N/A | N/A |
| 9. Mandatory rationale | N | Not a rule/ensure. | N/A | N/A |
| 10. Static semantic checking | Y | The check moves to the proof stage where flow context (guard + reassignment) is available, making it sound (`proof-engine.md § Sequential proof flow`). | A type-immediate check is "earlier"; moving it to proof defers the diagnostic stage. | Accepted: the diagnostic stages slightly later, but the check becomes *sound* (flow-aware) instead of unconditionally-rejecting open fields. |
| 11. Static completeness | Y | A well-typed guarded assignment gains defined compile-time meaning; no new runtime fault path. | N/A | N/A |

Companion commitments: *Stateless-first-class* — the obligation discharges in rules/ensures too (the proof engine consults `ConstraintContext` guards). *Domain-expert-primary-author* — the author writes the guard in domain terms; the stamping/discharge is invisible. The PRE0141 author-facing message and code are unchanged.

## Language Design Grounding
Omitted — no language surface change. PRE0141's diagnostic *code* and author-facing message are unchanged; only its emitting *pipeline stage* moves (Type → Proof). No keyword/type/operator/modifier/construct/accessor is added or modified.

## Semantic Rules

**Obligation stamping.** When the type checker resolves a `set F = e` action whose target field `F` carries qualifiers on some axis α, and the source `e`'s qualifier on α resolves to `Unknown` (open — no declared qualifier; the `QualifierResolutionKind.Unknown` arm of `ValidateResolvedQualifierAxes`), it **stamps** a `QualifierCompatibilityProofRequirement`(target=F's declared α-value, source=`e`, axis=α) onto the action node — instead of immediately emitting PRE0141. (The `Resolved`-but-mismatched arm is unchanged: a declared cross-currency assignment is a definite type-stage error; narrowing cannot rescue it.)

```
  F : τ in 'w' (declared qualifier w on axis α)      e : τ, qualifier(e, α) = Unknown
  ─────────────────────────────────────────────────────────────────────────────────  [stamp]
        set F = e   stamps   QualifierCompatibilityProofRequirement(w, e, α)
```

**Discharge.** The proof engine instantiates the stamped obligation and discharges it via the D9 narrowing strategy (`TryQualifierGuardNarrowingProof`): proved iff the enclosing guard narrows `e`'s field on α to a value satisfying `w` (value-exact for identity axes; all-branches; reassignment-aware via `ReassignedBefore`). Unresolved → PRE0141 at the proof stage.

**Composition with the binary-op obligation.** `set F = F + e` already stamps the operation's `QualifierCompatibilityProofRequirement` on the binary node (`Operations.cs`). The assignment obligation is stamped on the *assignment target vs the RHS result*. To avoid double-diagnosis, the assignment obligation is stamped only when the RHS resolves to `Unknown` on the axis (open) — the case the operation obligation does not already cover. (Acceptance tests assert no double PRE0114+PRE0141 on the same site.)

**Soundness preservation (Principles 1, 10, 11).** The relocation admits *no* program the type-immediate check rejected except genuinely-narrowed ones: the proof obligation is discharged only by a sound guard-narrowing (D9 D3), and an unresolved obligation still emits PRE0141. The check moves from "reject all open RHS" (sound but incomplete) to "discharge iff provably narrowed" (sound and more complete). No new runtime fault path.

## Architecture Grounding

### Precept-internal placement
**Layer placement.** The assignment-qualifier check belongs where every other proof obligation lives: **stamped by the type checker, discharged by the proof engine** (`proof-engine.md` Decision 3, `:521`: "The type checker stamps these requirements … The proof engine … reads them, doesn't compute them"). The current type-immediate PRE0141 emit is the *anomaly* — the one qualifier check done inline rather than as a stamped obligation. This design regularizes it. The narrowing discharge requires flow context (guard + reassignment) that exists at the proof stage and not at the type-checker assignment-validation site (`CheckContext` carries neither, and the helper is shared with ~6 guard-less call sites) — so the proof stage is the correct layer.

**Cross-component propagation:**
- Runtime (type checker, proof engine, diagnostics): type checker stamps the obligation on the `Unknown` arm instead of emitting PRE0141; proof engine discharges via the existing narrowing strategy; PRE0141 emits at the proof stage on unresolved (same code, same message, new stage).
- Tooling (hover, completions, semantic tokens): None to hover/completions. Diagnostic *timing*: PRE0141 now surfaces alongside proof-stage diagnostics (like PRE0114) rather than type-stage — LS publishes it in the same diagnostic pass it already publishes PRE0114; no new LS code.
- MCP: `precept_compile` groups diagnostics by stage; PRE0141 moves from the Type group to the Proof group in the output. DTO shape unchanged.

**Breaking changes.** PRE0141's *stage metadata* flips Type → Proof. The diagnostic code (141), severity, and author-facing message are unchanged. `diagnostic-system.md:178` updates its wording. No catalog renames; no public-API signature change.

### External architectural precedent
The two in-tree surveys ground this. `type-proof-stage-contract-survey.md`: general-compiler-kind systems where a proof/flow check is one analysis among many (Rust borrowck on MIR, Roslyn nullable walk, Kotlin K2) **self-derive obligations by walking a typed/flow IR**, and Precept's catalog-stamped variant is a *cleaner* refinement of that well-precedented model — whereas dedicated-verification systems (Dafny, Frama-C, SPARK) front-end-generate VCs, often via an IVL. Precept stays with the self-derive/stamp model: it matches its *kind*, and an **IVL is rejected because it reintroduces the SMT non-determinism `proof-engine.md` forbids**. `flow-sensitive-check-placement-survey.md`: three of four production compilers stage flow-sensitive-check diagnostics into a distinct/deferred phase — so PRE0141 emitting at the proof stage (distinct from the type stage) is the better-precedented staging, consistent with the existing PRE0114 placement.

## Inventory of what will be built
- `TypeChecker.Expressions.AssignmentQualifiers.cs` — the `QualifierResolutionKind.Unknown` arm of `ValidateResolvedQualifierAxes` stamps a `QualifierCompatibilityProofRequirement` onto the action's value node instead of emitting PRE0141. (Definite-mismatch arm unchanged.)
- Proof-engine obligation instantiation — picks up the stamped assignment obligation (no new strategy; `TryQualifierGuardNarrowingProof` from D9 S1 discharges it).
- PRE0141 diagnostic factory — re-staged to the proof stage (emitted on unresolved obligation, like PRE0114).
- Tests: open-assignment narrowed → proves; unnarrowed → PRE0141 (proof stage); value-mismatch → rejected; no double PRE0114+PRE0141; the `Resolved`-mismatch definite error still type-stage.

## Decisions

### Decision 1: Regularize open-field assignment-qualifier compatibility to a stamped obligation discharged at the proof stage
**Stakes**: high
- **Rationale**: `proof-engine.md` Decision 3 makes "type checker stamps the catalog obligation, proof engine discharges" the locked contract for *every* obligation; the assignment-qualifier check is the lone inline exception. Stamping it (a) makes the guarded open-field assignment provable via the D9 narrowing strategy with no new architecture, (b) gives it the flow context (guard + `ReassignedBefore`) that only exists at the proof stage, and (c) aligns it with its sibling PRE0114. The narrowing logic stays in the proof engine (no duplication into the type checker).
- **Tradeoff accepted**: PRE0141 stages later (proof, not type). Accepted: the staging-survey shows distinct/deferred diagnostic staging is the production majority, and PRE0114 already stages there; the check becomes sound-and-complete rather than open-field-rejecting.
- **Alternatives considered**: (A) *narrow inline at the type checker* — rejected: the type checker has no flow substrate (`CheckContext` lacks guard/chain; the helper is shared with guard-less default-value sites), it duplicates the proof engine's narrowing, and it violates Decision 3 + the type/proof separation; the placement survey found no system narrows-for-compatibility without a flow substrate. (B) *VC-handoff / IVL as a new general pattern* — rejected: Precept already has the stamping contract (this needs no new pattern), and an IVL reintroduces SMT non-determinism `proof-engine.md` rejects.
- **Precedent**: `proof-engine.md` Decision 3 (the stamping contract); `Actions.cs:257` `GenerateIntervalContainmentObligations` (Precept already generates obligations on `set` actions); Rust borrowck / Roslyn nullable self-derivation (`type-proof-stage-contract-survey.md`); the staging majority (`flow-sensitive-check-placement-survey.md`).
- **Sources consulted for this decision**: `proof-engine.md:521` — "The type checker stamps these requirements onto TypedExpression and TypedAction nodes … The proof engine … reads them, doesn't compute them"; `diagnostic-system.md:178` — "Use PRE0141 when the type checker cannot prove a required assignment qualifier axis; keep PRE0114 for operand-pair proof obligations"; `AssignmentQualifiers.cs:108-115` — the `Unknown` arm emitting PRE0141; `type-proof-stage-contract-survey.md` § Conclusions (self-derivation well-precedented for Precept's kind; IVL reintroduces non-determinism); grepped `precept-language-spec.md`/`business-domain-types.md` — spec is silent on the *pipeline placement* of the assignment check (it's an impl-architecture choice); the only settled statement is `diagnostic-system.md:178`'s stage wording, which this design updates with owner authorization.
- **Strongest counter-evidence**: `diagnostic-system.md:178` deliberately placed PRE0141 at the type stage. Response: that text describes the current inline anomaly; the owner authorized regularizing it (this session), and the staging survey shows proof-stage emission is better-precedented. The PRE0141/PRE0114 *distinction* (assignment vs operand) is preserved — only the stage changes.
- **Reversibility**: `Hard` — re-staging a diagnostic and the stamping wiring touch a few sites; not irreversible (code unchanged, no author surface).
- **Blast radius**: `AssignmentQualifiers.cs` (stamp vs emit), PRE0141 factory (stage), proof-engine obligation instantiation; docs: `diagnostic-system.md`, `proof-engine.md`, `type-checker.md`; tests: the assignment-narrowing + no-double-diagnosis set. No external consumers beyond the PRE0141 stage-grouping in `precept_compile`/LS output.

### Decision 2: Stay with the catalog/stamp type↔proof contract; do not adopt VC-generation or an IVL
**Stakes**: high
- **Rationale**: the survey shows catalog/self-derivation is well-precedented for Precept's *kind* (general DSL pipeline, proof as one stage), and Precept's catalog-metadata sourcing is cleaner than the Rust/Roslyn precedents. VC-gen/IVL is the dedicated-verifier pattern; adopting it wholesale is unwarranted and an IVL specifically reintroduces SMT non-determinism `proof-engine.md` rejects for determinism.
- **Tradeoff accepted**: Precept forgoes the off-the-shelf prover ecosystem (Why3/Boogie back-ends) an IVL would unlock. Accepted: determinism + inspectability (Principle 3, opaque-solver rejection) outweigh prover reuse; Precept's proof obligations are bounded and catalog-shaped, not general SMT.
- **Alternatives considered**: VC-generation to an IVL (Dafny/Frama-C model) — rejected (determinism + over-engineering for Precept's bounded obligation set).
- **Precedent**: `type-proof-stage-contract-survey.md` § Conclusions (bimodal split by system purpose; self-derivation well-precedented for general-compiler-kind); `proof-engine.md` opaque-solver / determinism rejection.
- **Sources consulted for this decision**: `type-proof-stage-contract-survey.md` § Implications — "catalog-driven self-derivation is not unusual for Precept's kind … what's thinly-precedented is sourcing derived obligations from catalog metadata … cleaner than the Rust/Roslyn precedents"; `proof-engine.md` § (opaque-solver rejection) — SMT/Z3 excluded for inspectability/determinism.
- **Strongest counter-evidence**: Precept's prevention-as-structural-guarantee *ambition* matches the dedicated-verifier kind (Whiley/Dafny), which favor VC-gen/IVL. Response: ambition ≠ architecture; the stamping contract already delivers structural prevention deterministically without an external solver, and the survey flags this as a tension to monitor, not a mandate to switch.
- **Reversibility**: `Hard` — a future shift to an IVL would be a large re-architecture; but this decision *preserves the status quo*, so it locks in no new cost.
- **Blast radius**: documentation only (records the direction); no code change. `research/architecture/README.md` open-questions #1/#2 updated.

## Falsifiers
- If re-staging PRE0141 to the proof stage causes a diagnostic-ordering or LS-publish regression (PRE0141 lost or duplicated on real samples), the re-stage is wrong and must be revisited.
- If the stamped assignment obligation double-diagnoses (`PRE0114` + `PRE0141` on the same `set F = F + e` site), the stamping condition (RHS-`Unknown`-only) is mis-scoped.
- If a future obligation class genuinely needs an external solver Precept's catalog model can't express, Decision 2's "no IVL" needs revisiting (the survey's monitored tension fired).

## Acceptance criteria
- `precept_compile`: `when X.currency == 'USD' -> set UsdField = X` (open money → USD field) compiles and proves; without the guard → PRE0141 (proof stage); `when X.currency == 'EUR' -> set UsdField = X` → PRE0141 (mismatch, unresolved).
- `set UsdField = UsdField + X` under the USD guard → proves with **no** double PRE0114+PRE0141.
- A declared cross-currency assignment (`EurField` value → `set UsdField = EurField`) → still a definite type-stage mismatch error (unchanged).
- PRE0141 appears in the proof-stage diagnostic group; `diagnostic-system.md:178` reflects the proof-stage wording.
- `dotnet test` green (existing PRE0141 type-stage tests migrated to proof-stage expectations); 0 warnings; analyzer clean.

## Dependencies
- Upstream: D9 S1 narrowing strategy (`TryQualifierGuardNarrowingProof`) — built (uncommitted). `proof-engine.md` Decision 3 stamping contract — shipped.
- Downstream: unblocks D9 S1 completion (the guarded assignment compiles); the D9 plan S1 row expands to include the stamp+re-stage wiring.

## Doc-update enumeration
- `docs/compiler/diagnostic-system.md:178` — PRE0141 re-described as a proof-stage stamped-obligation diagnostic (sibling of PRE0114), distinction preserved.
- `docs/compiler/proof-engine.md` — the assignment-qualifier obligation joins the stamped-obligation inventory.
- `docs/compiler/type-checker.md` — the `Unknown`-arm assignment check stamps an obligation rather than emitting.
- `docs/Working/d9-qualifier-narrowing-design.md` § G3 — reference this design as the resolution.
- `research/architecture/README.md` open-questions #1/#2 — record the stay-with-catalog/stamp direction (cite `type-proof-stage-contract-survey.md`).

## Operational dimensions
- **Observability** (diagnostic surface): PRE0141 moves to the proof-stage group; an unresolved assignment obligation surfaces with proof attribution (the guard leaf it needed). No loss of author signal — same code/message.

## Open questions
None.
