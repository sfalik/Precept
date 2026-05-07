# Cross-Cutting Decisions — Execution Driver

> This document is the coordination artifact for all multi-stage decisions.
> It drives Wave 1–5 execution. Wave 0 (Shane's foundational decisions) is complete.

## Status Summary

| CC# | Decision | Current status | Blocks |
|---|---|---|---|
| CC#1 | Expression Tree Design | [Decided] | Parser, Type Checker, Proof Engine, Precept Builder, Evaluator |
| CC#2 | SlotValue Subtype Shapes | [Decided] | Parser, Type Checker |
| CC#3 | SemanticIndex Reference-Tracking Collections | ✅ Resolved — Option A: typed reference arrays (`FieldReferences`, `StateReferences`, `EventReferences`) added to `SemanticIndex`. No general heterogeneous `References` array. Canonical: `docs/compiler/type-checker.md §7.1`. | Language Server semantic tokens, tooling surface reference routing |
| CC#4 | `Compilation.Tokens` Field | ✅ Resolved — already present in code stub as `TokenStream Tokens` (first field). `TokenStream` wraps `ImmutableArray<Token>` + lex-level diagnostics. No doc change needed; canonical: `src/Precept/Pipeline/Compilation.cs`. | Builder-facing compilation contract, lexical token tooling |
| CC#5 | `FieldModifierMeta.ProofDischarges` | [Open Question — canonical doc: `docs/language/catalog-system.md §FieldModifierMeta.ProofDischarges`] | Proof Engine Strategy 2 |
| CC#6 | FaultSiteLink to FaultSiteDescriptor Transformation | ✅ Resolved — Option A: nullable `FaultSiteAnnotation?` on each opcode. Builder stamps annotation at compile time from unresolved `FaultSiteLink`s. `null` = proven safe (structural absence = elision). Canonical: `docs/compiler/proof-engine.md §2`, `docs/runtime/precept-builder.md`, `docs/runtime/evaluator.md`. | Proof-to-runtime backstop planting |
| CC#7 | ConstraintMeta DU Subtype Count | ✅ Resolved — Option B (hierarchical, StateAnchored grouping node). Approved 2026-05-06. | Builder constraint routing, catalog-system alignment |
| CC#8 | EventInspection Shape | ✅ Resolved — EventInspection shape adopted; OQ-2 (ArgError = string Reason only) and OQ-3 (RowEffect DU) closed. OQ-4 (EventEnsures timing) pending. Canonical: `result-types.md` + `evaluator.md`. Approved 2026-05-06. | Evaluator inspection contract, LS preview, MCP DTO shape |
| CC#9 | `ConstraintFieldRefs.ConstraintIdentity` Type | ✅ Resolved — uses proof-engine ConstraintIdentity DU. Resolved 2026-05-06. | Type Checker and Proof Engine constraint identity alignment |
| CC#10 | GraphState Modifier Representation | [Open Question — canonical doc: `docs/compiler/graph-analyzer.md §GraphState shape`] | Graph output shape, modifier-derived facts |
| CC#11 | `ExecutionRow.RejectReason` Field | ✅ Resolved — string? RejectReason added to TypedTransitionRow and ExecutionRow. Resolved 2026-05-06. | Reject-row lowering, evaluator rejection outcomes |
| CC#12 | `Faulted(Fault)` as EventOutcome Variant | ✅ Resolved — `Faulted(Fault)` added as 8th variant. Canonical: `result-types.md`. | Evaluator outcome DU, MCP fire result serialization |
| CC#13 | `FaultCode.AmbiguousDispatch` | [Open Question — canonical doc: `docs/runtime/evaluator.md §7 Fire dispatch`] | Evaluator impossible-path faulting, diagnostics linkage |
| CC#14 | SlotContext vs SlotKind Enum Naming | [Open Question — canonical doc: `docs/tooling/language-server.md §7.3 Completions`] | LS/tooling completion context contract |
| CC#15 | `precept/inspect` vs `precept/preview` Naming | [Open Question — canonical doc: `docs/tooling/language-server.md §7.6 Preview/Inspect`] | LS custom method name, tooling preview routing |
| CC#16 | `TypeMeta.IsUserFacing` for Completions | [Open Question — canonical doc: `docs/language/catalog-system.md §TypeMeta`] | Completion filtering |
| CC#17 | `TypedArg.EventName` Back-Reference | [Open Question — canonical doc: `docs/tooling/language-server.md §7.5 Go To Definition`] | Arg hover/navigation |
| CC#18 | ConstructMeta Outline Properties | [Open Question — canonical doc: `docs/tooling/language-server.md §7.7 Document Outline`] | Document symbols / outline |
| CC#19 | `TokenMeta.HoverDescription` Strategy | [Open Question — canonical doc: `docs/language/catalog-system.md §Catalog documentation strings`] | Hover/completion documentation |
| CC#20 | Diagnostic Related Locations | [Open Question — canonical doc: `docs/compiler/diagnostic-system.md §Open Questions / Implementation Notes`] | Multi-span diagnostics, LSP relatedInformation |
| CC#21 | Event `optional` Modifier | [Open Question — canonical doc: `docs/compiler/graph-analyzer.md §Event coverage open questions`] | Parser, Type Checker, Graph Analyzer, Evaluator, grammar, completions, hover, MCP |
| CC#22 | `SemanticIndex.EnsuresByState` | [Blocked by: CC#3] | LS/MCP ensure navigation and indexing |
| CC#23 | `EventOutcome.mutations` Payload | ✅ Resolved — Option A: `ImmutableArray<FieldMutation> Mutations` attached to `Transitioned` and `Applied`. Canonical: `result-types.md`. | Evaluator outcome contract, MCP fire payload |
| CC#24 | Unmatched Guard Trace Enrichment | ✅ Resolved — Option A: `Unmatched(ImmutableArray<TransitionInspection> EvaluatedRows)` — same type as inspect path. Canonical: `result-types.md`. | Evaluator unmatched contract, MCP diagnostics |
| CC#25 | Execution Dispatch Delegate Design | [Decided] | Evaluator, runtime value model, builder plan lowering, catalog runtime metadata |
| CC#26 | Stateless Precepts `CreateInitialVersion` Semantics | [Open Question — canonical doc: `docs/runtime/runtime-api.md §Stateless Precepts — CreateInitialVersion`] | Runtime API, Evaluator, Graph Analyzer |

## Dependency Map

| Canonical doc / stage | Blocking CC decisions | Why this matters |
|---|---|---|
| `docs/compiler/parser.md` | CC#1, CC#2, CC#21 | Expression nodes, slot shapes, and any event-surface modifier semantics must be fixed before parser text can stabilize. |
| `docs/compiler/type-checker.md` | CC#1, CC#2, CC#3, CC#7, CC#9, CC#22 | SemanticIndex shape and constraint identity are the type-checker contracts other stages read. |
| `docs/compiler/proof-engine.md` | CC#1, CC#5, CC#6, CC#9 | Proof discharge and proof-to-runtime fault-site identity define what residue crosses downstream. |
| `docs/compiler/graph-analyzer.md` | CC#10, CC#21, CC#26 | Graph facts depend on modifier representation, optional-event semantics, and stateless creation rules. |
| `docs/runtime/precept-builder.md` | CC#4, CC#6, CC#7, CC#11, CC#25 | The compile-to-runtime boundary owns compilation aggregate shape, fault-site planting, constraint routing, reject-row lowering, and execution-plan dispatch. |
| `docs/runtime/evaluator.md` | CC#8, CC#11, CC#12, CC#13, CC#23, CC#24, CC#25, CC#26 | Runtime result shapes, impossible-path faults, mutation reporting, trace richness, and stateless creation semantics converge here. |
| `docs/runtime/runtime-api.md` | CC#25, CC#26 | Public API wording must match the locked runtime baseline and stateless initialization contract. |
| `docs/tooling/language-server.md` | CC#3, CC#8, CC#14, CC#15, CC#17, CC#18, CC#19, CC#22 | LS contracts mirror the semantic index, preview method naming, outline metadata, and hover/documentation metadata. |
| `docs/tooling/mcp.md` | CC#8, CC#12, CC#22, CC#23, CC#24 | MCP DTOs are thin wrappers; they cannot drift from evaluator and SemanticIndex contracts. |
| `docs/language/catalog-system.md` | CC#5, CC#7, CC#16, CC#19, CC#21, CC#25 | Catalog metadata is the machine-readable language spec; these decisions decide which metadata must exist. |
| `docs/compiler/diagnostic-system.md` | CC#13, CC#20 | Impossible-path fault linkage and related locations must stay aligned between diagnostics and runtime fault codes. |

## Wave 0 — Foundational Decisions ✅ COMPLETE

### Execution Checklist

- [x] [Shane] Lock CC#1 Expression Tree Design. [Blocks: `docs/compiler/parser.md`, `docs/compiler/type-checker.md`, `docs/compiler/proof-engine.md`, `docs/runtime/precept-builder.md`, `docs/runtime/evaluator.md`]
- [x] [Shane] Lock CC#2 SlotValue subtype ownership and expression-slot contract. [Blocks: `docs/compiler/parser.md`, `docs/compiler/type-checker.md`]
- [x] [Shane] Lock CC#25 execution dispatch/runtime value architecture. [Blocks: `docs/runtime/evaluator.md`, `docs/runtime/precept-builder.md`, `docs/runtime/runtime-api.md`, `docs/language/catalog-system.md`]

### Outcome

Wave 0 is closed. Wave 1 now owns the remaining cross-stage shape decisions; Waves 2–5 should treat CC#1, CC#2, and CC#25 as fixed architecture, not open design space.

## Wave 1 — Cross-Stage Structural Decisions

### Execution Checklist

- [ ] [Shane] Resolve CC#3 `SemanticIndex` reference-collection contract. [Blocks: `docs/compiler/type-checker.md §7.1`, `docs/tooling/language-server.md §7.3`, `docs/compiler/tooling-surface.md`]
- [ ] [Shane] Resolve CC#4 `Compilation.Tokens` access path. [Blocks: `docs/runtime/precept-builder.md §2`, lexical semantic-token flow]
- [ ] [Shane] Resolve CC#6 proof-to-runtime `FaultSiteLink` lowering. [Blocks: `docs/compiler/proof-engine.md §2`, `docs/runtime/precept-builder.md`, evaluator fault routing]
- [x] [Shane] Resolve CC#7 `ConstraintMeta` DU hierarchy. [Blocks: builder constraint buckets, catalog-system constraint metadata]
- [x] [Shane] Resolve CC#8 canonical `EventInspection` shape. [Blocks: evaluator inspection contract, LS preview, MCP DTOs]
- [x] [Team] Apply the CC#7 ruling to CC#9 `ConstraintFieldRefs.ConstraintIdentity`. [Blocked by: CC#7] [Blocks: type-checker/proof-engine shared identity data]
- [x] [Team] Add canonical storage for reject-row `because` text (CC#11). [Blocked by: none] [Blocks: `docs/runtime/precept-builder.md §ExecutionRow`, `docs/runtime/evaluator.md` rejection outcomes]
- [ ] [Team] Apply the CC#8 ruling to CC#12 `Faulted(Fault)` outcome handling. [Blocks: evaluator/MCP outcome parity]
- [ ] [Shane] Resolve CC#23 `EventOutcome.mutations` ownership. [Blocks: evaluator result contract, `docs/tooling/mcp.md §precept_fire`]
- [ ] [Shane] Resolve CC#24 unmatched-guard trace richness. [Blocks: evaluator unmatched contract, `docs/tooling/mcp.md §precept_fire`]

### Exit Criteria

Wave 1 is done when the cross-stage shape questions stop living only in this driver and each affected canonical doc can state a single contract without cross-doc disagreement.

## Wave 2 — Stage-Local Resolutions

### Execution Checklist

- [ ] [Team] Close CC#5 `FieldModifierMeta.ProofDischarges` using the already-drafted catalog shape. [Blocks: `docs/language/catalog-system.md`, proof-engine Strategy 2]
- [ ] [Team] Close CC#10 `GraphState` modifier representation. [Blocks: `docs/compiler/graph-analyzer.md` graph output shape]
- [ ] [Team] Add CC#13 `FaultCode.AmbiguousDispatch` plus linked diagnostic metadata. [Blocks: evaluator impossible-path faulting, diagnostics linkage]
- [ ] [Team] Unify CC#14 `SlotContext` vs `SlotKind` naming. [Blocks: LS/tooling completion context docs]
- [ ] [Team] Lock CC#15 `precept/inspect` vs `precept/preview` naming and apply it everywhere. [Blocks: LS preview command, tooling surface docs]
- [ ] [Team] Decide whether CC#16 `TypeMeta.IsUserFacing` becomes first-class catalog metadata. [Blocks: completion filtering docs]
- [ ] [Team] Close CC#17 `TypedArg.EventName` back-reference routing. [Blocks: arg hover/navigation docs]
- [ ] [Team] Close CC#18 outline metadata on `ConstructMeta`. [Blocks: document-symbol / outline docs]
- [ ] [Team] Standardize CC#19 hover/snippet documentation metadata strategy. [Blocks: catalog-system hover/completion docs]
- [ ] [Team] Decide CC#20 diagnostic related-location support. [Blocks: `docs/compiler/diagnostic-system.md`, LS `relatedInformation` mapping]
- [ ] [Shane] Lock CC#21 end-to-end semantics for the event `optional` modifier. [Blocks: parser, type checker, graph analyzer, evaluator, grammar, completions, hover, MCP]
- [ ] [Team] Apply the CC#3 ruling to CC#22 `SemanticIndex.EnsuresByState`. [Blocked by: CC#3] [Blocks: LS/MCP ensure navigation]
- [ ] [Shane] Lock CC#26 stateless `CreateInitialVersion` semantics. [Blocks: `docs/runtime/runtime-api.md`, evaluator, graph analyzer]

### Exit Criteria

Wave 2 is done when the remaining single-stage or lightly coupled questions become mechanical documentation and implementation work instead of architecture debates.

## Wave 3 — Open Question Resolution

### Execution Checklist

- [ ] [Team] Use `docs/working/Archived/structural-gap-register-migrated.md` as a routing index only; burn down each migrated structural open question in its owning canonical doc. [Blocked by: Waves 1–2]
- [ ] [Team] Use `docs/working/Archived/catalog-gap-register-migrated.md` as a routing index only; burn down each migrated catalog open question in `docs/language/catalog-system.md` and dependent docs. [Blocked by: Waves 1–2]
- [ ] [Team] Sweep `docs/compiler/type-checker.md`, `docs/compiler/proof-engine.md`, `docs/runtime/precept-builder.md`, `docs/runtime/evaluator.md`, `docs/tooling/language-server.md`, `docs/tooling/mcp.md`, `docs/language/catalog-system.md`, `docs/compiler/graph-analyzer.md`, and `docs/compiler/diagnostic-system.md` so the canonical docs become the only live trackers. [Blocked by: Waves 1–2]
- [ ] [Shane] Review any item that remains a real product choice after the team closes the mechanical migrations. [Blocked by: Team sweep]

### Canonical-Doc Burn-Down Order

1. `docs/compiler/type-checker.md` — CC#3, CC#9, CC#22
2. `docs/compiler/proof-engine.md` + `docs/runtime/precept-builder.md` — CC#6, CC#7, CC#11
3. `docs/runtime/evaluator.md` + `docs/tooling/language-server.md` + `docs/tooling/mcp.md` — CC#8, CC#12, CC#23, CC#24
4. `docs/language/catalog-system.md` + `docs/compiler/graph-analyzer.md` + `docs/compiler/diagnostic-system.md` — CC#5, CC#10, CC#13, CC#16–CC#21, CC#26

## Wave 4 — Doc Finalization

### Execution Checklist

- [ ] [Team] Run a final consistency pass across compiler, runtime, tooling, and catalog docs after Wave 3 closes. [Blocked by: Wave 3]
- [ ] [Team] Replace stale `pending`, `provisional`, and cross-doc disagreement language for any CC decision already locked. [Blocked by: Wave 3]
- [ ] [Team] Update `docs/compiler/README.md` and any navigation tables so canonical docs are the first and only destination. [Blocked by: Wave 3]
- [ ] [Shane] Sign off any owner-only open question that still prevents a doc from reaching Full status. [Blocked by: Team final pass]

## Wave 5 — Archive

### Execution Checklist

- [ ] [Team] Delete `docs/working/` only after Wave 4 confirms the canonical docs carry all live open questions and decision outcomes. [Blocked by: Wave 4]
- [ ] [Team] Remove superseded `docs/compiler/parser-radical.md` and `docs/compiler/type-checker-radical.md` once their content is fully absorbed or explicitly retired. [Blocked by: Wave 4]
- [ ] [Team] Clean README and doc links that still point at retired working artifacts. [Blocked by: Wave 4]
- [ ] [Team] Verify no unresolved decision exists only in an archived working file before cleanup closes. [Blocked by: Wave 4]

## Detailed Decision Entries (Retained Source Material)

> Primary execution driver for gap resolution. Organized by dependency wave per Frank's gap-sequencing analysis (2026-05-03).
> Cross-cutting decisions gate everything — resolve waves 0–2 before any catalog/structural gap resolution.

**Legend:** 🔴 Shane Decision Required | 🔵 Team-Autonomous | ✅ Resolved

---

## Wave 0 — Foundational (Shane Required — Must Resolve First)

> These 3 decisions block everything downstream. Every catalog shape, every structural type, every stage implementation cascades from these. Shane must lock these before any other work proceeds.

---

### CC#1. Expression Tree Design

**Status:** ✅ Resolved

**Affects:** Parser, Type Checker, Proof Engine, Evaluator, Precept Builder
**Gap register refs:** #14, #45, #53
**The decision:** What is the structure of expression trees produced by the parser and consumed by all downstream stages?

**Why it's cross-cutting:**
- **Parser** (blocked) — expression-carrying slots (`ComputeExpressionSlot`, `GuardClauseSlot`, `EnsureClauseSlot`, `RuleExpressionSlot`, `OutcomeSlot`) currently carry only `SourceSpan`. Parser needs to know the expression node shape to produce it.
- **Type Checker** (blocked) — §7.2 Expression Resolution Engine is designed but cannot be exercised until parser produces expression trees.
- **Proof Engine** (blocked) — Strategy 3 (guard-in-path) and Strategy 4 (flow-narrowing) require parsing guard expressions into constraint form.
- **Evaluator** — needs to walk typed expressions (currently uses ExecutionPlan opcodes, which are compiled FROM expression trees).
- **Precept Builder** — compiles `TypedExpression` trees into flat opcode arrays.

**Ruling: Option A — Roslyn-style typed nodes. Approved 2026-05-03.**

**Design:**
- `ParsedExpression` — sealed abstract record base + per-kind sealed subtypes (~10 forms). Parser produces these.
- `TypedExpression` — sealed abstract record base + per-kind sealed subtypes with resolved types. Type checker produces these.
- Expression tree is the strongly-typed layer. The rest of the parser AST (constructs, declarations, statements) stays generic.
- Closed set by design: new expression form requires C# code change. This is intentional — the DU IS the enforcement boundary.
- **Exhaustiveness enforcement:**
  - Sealed class hierarchy → compiler-level (CS8509/CS8524): non-exhaustive `switch` expressions over the DU base type are compile errors
  - `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]` + PRECEPT0019 → annotation-bridge: multi-method consumers (Parser, Builder) must annotate each handler method with `[HandlesCatalogMember(ExpressionFormKind.X)]`; PRECEPT0019 fires if any enum member lacks a handler
  - Convention (enforced by test): each `ExpressionFormKind` member maps 1:1 to a sealed DU subtype in both `ParsedExpression` and `TypedExpression`

**Blocked items now unblocked:** Parser expression slots, TC §7.2, Proof Engine strategies 3 & 4, Builder compilation.

---

### CC#2. SlotValue Subtype Shapes

**Status:** ✅ Resolved — Option C (Hybrid), 2026-05-03T23:39:16Z

**Affects:** Parser, Type Checker
**Gap register refs:** #6, #7, #40, #41, #42, #43, #44
**Decision:** Parser stamps `ParsedExpression` into expression-carrying slots at parse time. Type checker consumes `ParsedExpression` and produces `TypedExpression` into the `SemanticIndex` — no re-parsing. Single parse pass. Clean syntactic/semantic boundary. SlotValue DU shape stable at 17 subtypes. `ParsedExpression` is unresolved (operator + operands, unresolved names); `TypedExpression` is resolved (identities, inferred types). The Pratt expression parser uses `Operators.GetMeta()` for precedence/associativity — no hardcoded table.

**Blocked items now unblocked:** Parser expression slots (now `ComputeExpressionSlot`, `GuardClauseSlot`, `OutcomeSlot`, `EnsureClauseSlot`, `RuleExpressionSlot` all carry `ParsedExpression`), TC §7.2 semantic resolution, Proof Engine strategies 3 & 4, Builder compilation.

**Remaining open (non-expression slots):** The `ModifierListSlot`, `AccessModeSlot`, and `BecauseClauseSlot` shape conflicts between parser.md and type-checker.md are unresolved. These are non-expression slots and are not blocked by CC#2.

---

### CC#25. Execution Dispatch Delegate Design

**Status:** ✅ Resolved — Option A+G (typed-opcode interpreter + catalog-owned delegate dispatch), 2026-05-03/04.

**Affects:** Evaluator, Operations Catalog, Functions Catalog, Actions Catalog
**Gap register refs:** Audit item C

**Resolution:**
- `PreceptValue` is a 32-byte tagged struct (`[StructLayout(LayoutKind.Explicit, Size = 32)]`) — the internal evaluation currency on the A+G opcode stack.
- Catalog-owned executor arrays: `BinaryExecutors` and `UnaryExecutors` live on `TypeRuntimeMeta`, indexed by `OperationKind` ordinal. The builder fetches the delegate at compile time and embeds it directly in the `BinaryOp`/`UnaryOp` opcode record (`Func<PreceptValue, PreceptValue, PreceptValue> Executor` field). The evaluator calls `opcode.Executor(l, r)` — no global aggregation, no catalog lookup at evaluation time. `Kind` is preserved on the opcode for diagnostics and trace mode.
- `LOAD_ARG` carries pre-resolved `ArgSlotIndex` (not arg name string). All name resolution happens at build time in Pass 1.
- `IArgBuilder` materializes `PreceptValue[]` arg slot array + `bool[]` presence mask at the Fire boundary. Required-arg faults happen before the opcode loop.
- `TypeRuntime<T>` naming is final: `FromClr` / `ToClr` / `FromJson` / `ToJson`.
- `TypeRuntimeMeta` active surface: `ReadJson` / `WriteJson` / `ParseString` / `FormatString` / `BinaryExecutors` / `UnaryExecutors`. `ExtractValue`, `StoreValue`, `ParseValue` excluded from hot paths.
- Single interpreter with diagnostic trace — no dual-path. The A+G opcode executor serves production Fire/Inspect/Update AND LS/MCP authoring feedback. Trace mode emits per-step diagnostic records; no separate tree-walk interpreter.
- `System.Linq.Expressions` compilation is a designed-in future seam — not a v1 dual-path. TypeBuilder rejected for SaaS (cold-start incompatibility + inspectability guarantee).

**Blocked items now unblocked:** Evaluator opcode dispatch, TypeRuntime registration, builder execution plan compilation, IArgBuilder/IFieldBuilder implementation.

**Canonical docs:** `docs/runtime/evaluator.md` (§5 PreceptValue, §7 opcode dispatch, §11 Decision 8), `docs/runtime/runtime-api.md` (§Value Types), `docs/runtime/precept-builder.md` (§Pass 5, §Arg Slot Invariants).

---

## Wave 1 — Shape-Defining (Shane Required for Most)

> Once Wave 0 is locked, these set the type shapes everything else conforms to. Most require Shane decisions; CC#9 follows mechanically from CC#7.

---

### CC#3. SemanticIndex Reference-Tracking Collections

**Status:** ✅ Resolved — Option A: typed reference arrays on `SemanticIndex`. Canonical: `docs/compiler/type-checker.md §7.1`.

**Affects:** Type Checker, Language Server, Tooling Surface
**Gap register refs:** #16, #47, #48, #49, #50
**The decision:** Should `SemanticIndex` carry reference-site tracking arrays (`References`, `FieldReferences`, `StateReferences`, `EventReferences`)?

**Why it's cross-cutting:**
- **Type Checker** — Must record span and binding of every identifier use if these collections are added
- **Language Server** — Pass 2 semantic tokens needs reference-site arrays to classify identifier tokens
- **Tooling Surface** — Also references these collections for semantic token Pass 2

**Ruling:** Option A — typed per-category arrays added to `SemanticIndex`. The type checker is already resolving every identifier; recording the span + binding at that point is zero extra work. Reconstructing from typed declarations at the LS layer would duplicate work done at resolution time. No general heterogeneous `References` array — the per-type arrays are sufficient and avoid a DU or `object`-typed collection.

```csharp
public sealed record FieldReference(TypedField Field, SourceSpan Site);
public sealed record StateReference(TypedState State, SourceSpan Site);
public sealed record EventReference(TypedEvent Event, SourceSpan Site);
```

Added to `SemanticIndex` as:
```csharp
ImmutableArray<FieldReference> FieldReferences,
ImmutableArray<StateReference> StateReferences,
ImmutableArray<EventReference> EventReferences,
```

**Resolution path:** The type-checker.md §7.1 canonical `SemanticIndex` shape has no reference collections, but language-server.md explicitly expects them.

---

### CC#4. Compilation.Tokens Field

**Status:** ✅ Resolved — already present in code stub as `TokenStream Tokens`. No decision needed.

**Affects:** Precept Builder, Language Server, Tooling Surface
**Gap register refs:** #61
**The decision:** Should `Compilation` carry a `Tokens` field containing the lexer's token stream?

**Why it's cross-cutting:**
- **Language Server** — Pass 1 lexical semantic tokens needs the token stream
- **Tooling Surface** — References `Compilation.Tokens` for lexical tokens
- **Precept Builder** — The `Compilation` record in precept-builder.md doesn't include `Tokens`

**Ruling:** The code stub (`src/Precept/Pipeline/Compilation.cs`) already carries `TokenStream Tokens` as its first field. `TokenStream` wraps `ImmutableArray<Token> Tokens` plus `ImmutableArray<Diagnostic> Diagnostics` (lex-level diagnostics). The doc gap (precept-builder.md not mentioning Tokens) is a documentation lag, not a design gap. The canonical truth is in the code stub.

**Resolution path:** The precept-builder.md `Compilation` record shape is the canonical definition; it needs explicit decision on whether to add `Tokens`.

---

### CC#6. FaultSiteLink to FaultSiteDescriptor Transformation

**Status:** ✅ Resolved — Option A: nullable `FaultSiteAnnotation?` on each opcode. Canonical: `docs/compiler/proof-engine.md §2`, `docs/runtime/precept-builder.md`, `docs/runtime/evaluator.md`.

**Affects:** Proof Engine, Precept Builder, Evaluator
**Gap register refs:** #11, #23, #56, #57, #60
**The decision:** How does the proof engine's `FaultSiteLink` (compile-time span) transform into the Precept Builder's `FaultSiteDescriptor` (runtime location)?

**Why it's cross-cutting:**
- **Proof Engine** — Produces `FaultSiteLink.Site` as `SourceSpan`
- **Precept Builder** — Needs to plant `FaultSiteDescriptor` at specific `ExecutionRow` or opcode offset
- **Evaluator** — Must locate fault sites for defense-in-depth routing

**Current gap:** Neither `ExecutionRow` nor `ConstraintDescriptor` carries a fault site field. `Precept.FaultBackstops` is a flat array with no mechanism to associate backstops with specific opcodes.

**Ruling:** Option A — nullable `FaultSiteAnnotation?` field on each opcode.

Key design insight (confirmed by proof-engine.md §2): `FaultSiteLink` is produced *only* for `Unresolved` obligations. Unresolved proof is a hard compilation error — the author must fix the source before a `Precept` can be built. Therefore the runtime backstop is defense-in-depth against:
- Force-build / tooling-mode builds with errors present
- Catalog evolution (new `ProofRequirement` added to an existing `FaultCode` after a precept was compiled)
- Compiler bugs that produce a `Precept` with an undetected hazard

Elision is structural absence: proved obligations produce no `FaultSiteLink`, no annotation is planted, and the evaluator performs zero backstop check. This is the SPARK Ada model (strip proven checks) realized through Precept's gate architecture — no skip flag needed.

```csharp
public sealed record FaultSiteAnnotation(
    FaultCode Code,           // Runtime fault to fire if reached
    DiagnosticCode PreventedBy, // Authoring-time diagnostic that would prevent this
    SourceSpan Site           // Source location for diagnostics/logging
);

// On each opcode — null = proven safe (structural absence = elision)
FaultSiteAnnotation? FaultSite
```

Builder matches `ProofObligation.Site` (TypedExpression) against the expression being compiled and stamps the annotation on the resulting opcode. Evaluator checks `op.FaultSite` after each dispatch; `null` = no check.

**Resolution path:** The transformation mechanism is unspecified across all three stages.

---

### CC#7. ConstraintMeta DU Subtype Count

**Status:** ✅ Resolved — Option B (hierarchical, StateAnchored grouping node). Approved 2026-05-06.

**Affects:** Type Checker, Precept Builder
**Gap register refs:** #22, #62
**The decision:** How many subtypes does the `ConstraintMeta` discriminated union have?

**Why it's cross-cutting:**
- **Precept Builder** — Uses 5 concrete subtypes for constraint bucket routing (`Invariant`, `StateResident`, `StateEntry`, `StateExit`, `EventPrecondition`)
- **Catalog-system.md** — Shows only 4 subtypes in the DU hierarchy

**Current discrepancy:** Precept-builder.md's bucket dispatch switch uses 5 subtypes. Are `StateEntry` and `StateExit` separate top-level subtypes, or subtypes of `StateAnchored`?

**Ruling — Option B (hierarchical with StateAnchored grouping node):**

```csharp
public abstract record ConstraintMeta(...)
{
    public sealed record Invariant()         : ConstraintMeta(...);
    public abstract record StateAnchored()   : ConstraintMeta(...);
        public sealed record StateResident() : StateAnchored(...);
        public sealed record StateEntry()    : StateAnchored(...);
        public sealed record StateExit()     : StateAnchored(...);
    public sealed record EventPrecondition() : ConstraintMeta(...);
}
```

- Builder routing: 5-way switch on concrete types — correct for execution bucket dispatch.
- Type checker and proof engine use `meta is ConstraintMeta.StateAnchored` for grouping checks — correct for "is this state-scoped?" questions.
- The catalog already specifies this shape; this ruling locks it as canonical.

**Resolution path:** Catalog-system.md must specify the exact DU hierarchy.

---

### CC#9. ConstraintFieldRefs.ConstraintIdentity Type

**Status:** ✅ Resolved — ConstraintFieldRefs.ConstraintIdentity uses the proof-engine ConstraintIdentity DU. Resolved 2026-05-06.

**Affects:** Type Checker, Proof Engine
**Gap register refs:** #15, #51
**The decision:** Should `ConstraintFieldRefs.ConstraintIdentity` be typed `object` or use the proof-engine.md's `ConstraintIdentity` DU?

**Why it's cross-cutting:**
- **Type Checker** — Produces `ConstraintFieldRefs` with identity field typed as `object`
- **Proof Engine** — Defines a proper `ConstraintIdentity` DU with `RuleIdentity` and `EnsureIdentity` subtypes

**Ruling:** Use the proof-engine `ConstraintIdentity` DU:

```csharp
public abstract record ConstraintIdentity
{
    public sealed record RuleIdentity(string StateName, string RuleName) : ConstraintIdentity;
    public sealed record EnsureIdentity(string EventName, string FieldName) : ConstraintIdentity;
}
```

`object` is indefensible now that the DU is locked by CC#7. The DU provides type safety and exhaustive matching; `object` would require downstream casts with no compile-time exhaustiveness guarantee. The proof-engine already defines the correct shape — align `ConstraintFieldRefs.ConstraintIdentity` to it.

**Resolution path:** Align on the proof-engine.md DU shape for type safety.

---

## Wave 2 — Outcome & Evaluator (Shane Required for Initial Direction)

> These form a cluster around evaluator output shapes. Lock CC#8 first (defines the outcome DU shape), then CC#12, CC#23, CC#24 follow. CC#11 is independent but simple.

---

### CC#8. EventInspection Shape

**Status:** ✅ Resolved

**Affects:** Evaluator, Language Server, MCP
**Gap register refs:** #33, #64
**The decision:** What is the canonical shape of `EventInspection`?

**Why it's cross-cutting:**
- **Evaluator** (evaluator.md) — Uses `EventEnsures`, `ConstraintResult`, `RowInspection`
- **Language Server** (language-server.md) — Uses `BeforeFields`, `AfterFields`, `TransitionRowInspection`

These are different shapes for the same concept. Downstream consumers cannot implement until the shape is resolved.

**Resolution path:** Evaluator.md should be authoritative for runtime shapes; other docs should reference it.

**Resolution (2026-05-06):** EventInspection shape adopted per `docs/working/event-inspection-proposal.md` with OQ-2 (ArgError = string Reason only, no ArgErrorKind — matches field edit error pattern) and OQ-3 (RowEffect DU, not TransitionKind enum — `RowEffect { TransitionTo(TargetState), NoTransition, Rejection(Reason) }`) closed. Canonical shape now lives in `docs/runtime/result-types.md` (type definitions) and `docs/runtime/evaluator.md` (implementation contract). OQ-4 (EventEnsures timing) remains pending. CC#12 unblocked.

---

### CC#11. ExecutionRow.RejectReason Field

**Status:** ✅ Resolved — string? RejectReason added to TypedTransitionRow and ExecutionRow. Resolved 2026-05-06.

**Affects:** Type Checker, Precept Builder, Evaluator
**Gap register refs:** #20, #59
**The decision:** Where is the `because` clause from a `reject` transition row stored?

**Why it's cross-cutting:**
- **Type Checker** — Produces `TypedTransitionRow` (needs field for reject reason)
- **Precept Builder** — Transforms to `ExecutionRow` (needs field)
- **Evaluator** — Returns `Rejected(reason)` outcome (reads field)

**Ruling:** Add `string? RejectReason` to both `TypedTransitionRow` and `ExecutionRow`. No design ambiguity — the evaluator already references `winningRow.RejectReason`; the field simply wasn't declared. `null` for non-reject rows; populated from the `because` clause text for reject rows.

**Resolution path:** Add `string? RejectReason` to both `TypedTransitionRow` and `ExecutionRow`.

---

### CC#12. Faulted(Fault) as EventOutcome Variant

**Status:** ✅ Resolved — `Faulted(Fault)` added as 8th `EventOutcome` variant. Canonical: `result-types.md`.

**Affects:** Evaluator, MCP
**Gap register refs:** #21, #63
**The decision:** Should `Faulted(Fault)` be added as an `EventOutcome` DU variant?

**Why it's cross-cutting:**
- **Evaluator** — `Fail()` returns a `Fault`, but `Fault` is not currently a subtype of `EventOutcome`
- **MCP** — `precept_fire` needs to serialize fault outcomes

**Ruling:** `Faulted(Fault fault)` added as the 8th variant of `EventOutcome`. The evaluator's `Fail()` path now surfaces its `Fault` as a structured outcome rather than an unhandled exception at the runtime boundary. MCP `precept_fire` serializes `Faulted` as `{ "outcome": "Faulted", "fault": { ... } }`.

**Resolution path:** Add `Faulted(Fault)` variant to the `EventOutcome` DU.

---

### CC#23. `EventOutcome.mutations` Payload

**Status:** ✅ Resolved — Option A: `ImmutableArray<FieldMutation> Mutations` attached to `Transitioned` and `Applied`. Canonical: `result-types.md`.

**Affects:** Evaluator, MCP
**Gap register refs:** #28, #65
**The decision:** Should `EventOutcome` carry a canonical `mutations` payload describing the field/state changes produced by execution?

**Why it's cross-cutting:**
- **Evaluator** — must compute and attach mutation details as part of outcome production.
- **MCP** — must serialize the payload into `precept_fire`/inspection responses for tooling consumers.

**Options / known design space:**
1. **Shared outcome payload** — attach `mutations` to the relevant successful `EventOutcome` variants.
2. **Dedicated mutation wrapper/result shape** — separate execution result data from the existing DU variants.
3. **Consumer reconstruction** — keep evaluator output lean and let MCP/tooling compute diffs independently.

**Ruling:** Option A — `ImmutableArray<FieldMutation> Mutations` is attached directly to the `Transitioned` and `Applied` variants. The evaluator already maintains the working copy during execution; computing the diff is zero-cost at that point and eliminates any N+1 re-inspection need at call sites. `FieldMutation` is a sealed record: `FieldMutation(string FieldName, JsonElement? Before, JsonElement? After)`. `Rejected`, `InvalidArgs`, `ConstraintsFailed`, `Unmatched`, `UndefinedEvent`, and `Faulted` carry no mutation payload because no mutations were committed.

**Resolution path:** Lock the ownership and shape of mutation diffs before evolving the `EventOutcome` discriminated union.

---

### CC#24. Unmatched Guard Trace Enrichment

**Status:** ✅ Resolved — Option A: `Unmatched(ImmutableArray<TransitionInspection> EvaluatedRows)` — same type as inspect path. Canonical: `result-types.md`.

**Affects:** Evaluator, MCP
**Gap register refs:** #29, #66
**The decision:** How much evaluated guard-trace detail should an `Unmatched` event outcome carry for tooling consumers?

**Why it's cross-cutting:**
- **Evaluator** — must capture candidate-row guard evaluations instead of returning a minimal unmatched result.
- **MCP** — must serialize that trace data for debugging, preview, and inspection callers.

**Options / known design space:**
1. **Full per-candidate trace** — every evaluated guard and its result is attached to `Unmatched`.
2. **Best-explanation summary** — only the most relevant failed guard or row explanation is carried.
3. **Consumer reconstruction** — evaluator stays minimal and tooling derives explanations elsewhere.

**Ruling:** Option A — `Unmatched(ImmutableArray<TransitionInspection> EvaluatedRows)`. Using the same `TransitionInspection` type already defined by CC#8 makes the inspect and commit paths type-consistent. Callers who want to understand *why* no row matched get the full per-candidate trace. The evaluator on the commit path is already running guard evaluation; retaining the per-row results is no additional work. MCP `precept_fire` serializes `EvaluatedRows` using the same DTO as `precept_inspect` transition rows.

**Resolution path:** Decide the required diagnostic richness before changing the `Unmatched` outcome shape and its MCP serialization.

**Navigation note:** The audit considered a separate umbrella decision for evaluator-output richness. I did not add one here because #22–#24 already form a tight, directly actionable cluster, and an umbrella entry would add indirection without introducing a separate design choice.

---

## Wave 3 — Catalog + Structural Resolution (Team-Autonomous — Run After Waves 0–2)

> Wave 3 items are not tracked here — they are the catalog and structural gaps that become mechanically resolvable once Waves 0–2 lock the upstream shapes. They live in the canonical pipeline docs as Open Questions. See the pipeline docs in `docs/pipeline/` for specifics.

---

## Wave 4 — Tooling & Minor Decisions (Team-Autonomous — No Upstream Blockers)

> These have no upstream blockers. Can proceed in parallel with anything. Some require Shane decisions (CC#21, CC#26) but are non-blocking for the core pipeline.

---

### CC#5. FieldModifierMeta.ProofDischarges

**Status:** 🔵 Pending team resolution (already designed — implement when ready)

**Affects:** Proof Engine, Modifiers Catalog
**Gap register refs:** #1
**The decision:** Add `ProofDischarges` property to `FieldModifierMeta` to enable catalog-driven proof Strategy 2 (Modifier Proof).

**Why it's cross-cutting:**
- **Modifiers Catalog** — Needs new `ProofDischarge[]` property on `FieldModifierMeta`
- **Proof Engine** — Strategy 2 reads this property to discharge obligations

This is already captured in catalog-system.md § Open Questions with a proposed shape, but implementation is pending.

**Proposed shape (from proof-engine.md):**
```csharp
public sealed record ProofDischarge(
    ProofRequirementKind RequirementKind,
    OperatorKind? Comparison,
    decimal? Threshold
);
```

| Modifier | ProofDischarges |
|----------|-----------------|
| `positive` | `[Numeric(>, 0), Numeric(!=, 0)]` |
| `nonnegative` | `[Numeric(>=, 0)]` |
| `nonzero` | `[Numeric(!=, 0)]` |
| `notempty` | `[Numeric(>, 0)]` for collection count |

**Resolution path:** Already designed, awaiting implementation decision.

---

### CC#10. GraphState Modifier Representation

**Status:** 🔵 Pending team resolution

**Affects:** Graph Analyzer, Modifiers Catalog
**Gap register refs:** #9, #54
**The decision:** Should `GraphState` carry explicit boolean flags (`IsInitial`, `IsTerminal`, `IsRequired`, `IsIrreversible`) or `ImmutableArray<ModifierKind> Modifiers` with catalog-derived flags?

**Why it's cross-cutting:**
- **Graph Analyzer** — Uses `GraphState` with modifier-derived properties
- **Modifiers Catalog** — Contains `StateModifierMeta` with structural constraint flags

**Options / known design space:**
1. **Explicit booleans** — Current design, easy to consume
2. **Modifier array + derivation** — Catalog-driven, easier to extend

**Resolution path:** Decide whether catalog-driven architecture applies to `GraphState` modifier representation.

---

### CC#13. FaultCode.AmbiguousDispatch

**Status:** 🔵 Pending team resolution

**Affects:** Evaluator, Faults Catalog, Diagnostics Catalog
**Gap register refs:** #4
**The decision:** Add `FaultCode.AmbiguousDispatch` with corresponding `DiagnosticCode` and `[StaticallyPreventable]` attribute.

**Current gap:** Evaluator.md references `Fail(FaultCode.AmbiguousDispatch)` but the fault code doesn't exist.

---

### CC#14. SlotContext vs SlotKind Enum Naming

**Status:** 🔵 Pending team resolution

**Affects:** Language Server, Tooling Surface
**Gap register refs:** #38, #69
**The decision:** Is `SlotContext` (language-server.md) the same as `SlotKind` (tooling-surface.md)? Which is canonical?

---

### CC#15. precept/inspect vs precept/preview Naming

**Status:** 🔵 Pending team resolution

**Affects:** Language Server, Tooling Surface
**Gap register refs:** #32, #68
**The decision:** What is the canonical name for the custom LSP preview method?

---

### CC#16. TypeMeta.IsUserFacing for Completions

**Status:** 🔵 Pending team resolution

**Affects:** Language Server, Types Catalog
**Gap register refs:** #30
**The decision:** Add `IsUserFacing` property to `TypeMeta` for filtering completion suggestions.

---

### CC#17. TypedArg.EventName Back-Reference

**Status:** 🔵 Pending team resolution

**Affects:** Type Checker, Language Server
**Gap register refs:** #31, #52
**The decision:** Should `TypedArg` carry an `EventName` back-reference for hover lookup?

---

### CC#18. ConstructMeta Outline Properties

**Status:** 🔵 Pending team resolution

**Affects:** Language Server, Constructs Catalog
**Gap register refs:** #34, #70, #71
**The decision:** Add `IsOutlineNode` and `LspSymbolKind` properties to `ConstructMeta` for catalog-driven document outline.

---

### CC#19. TokenMeta.HoverDescription Strategy

**Status:** 🔵 Pending team resolution

**Affects:** Language Server, Tooling Surface, Tokens Catalog
**Gap register refs:** #5, #35
**The decision:** Comprehensive strategy for documentation strings in `TokenMeta` beyond partial capture.

---

### CC#20. Diagnostic Related Locations

**Status:** 🔵 Pending team resolution

**Affects:** Diagnostic System, Language Server
**Gap register refs:** #39
**The decision:** Add `AdditionalLocations` to `Diagnostic` for multi-span diagnostics (e.g., "field declared at line X, constraint at line Y").

---

### CC#21. Event "optional" Modifier

**Status:** 🔴 Pending Shane decision (non-blocking for core pipeline)

**Affects:** Parser, Type Checker, Graph Analyzer, Evaluator, Grammar, Completions, Hover, MCP
**Gap register refs:** #10
**The decision:** What are the end-to-end semantics of an `optional` modifier on events?

**Why it's cross-cutting:**
- **Parser** — must recognize `optional` in event modifier position.
- **Type Checker** — must validate modifier legality and produce the correct typed metadata.
- **Graph Analyzer** — must decide whether `optional` suppresses `UnhandledEvent`-style diagnostics or changes reachability expectations.
- **Evaluator** — must define runtime behavior when an optional event has no applicable transition.
- **Grammar / Completions / Hover / MCP** — must surface the new keyword, documentation, and vocabulary consistently.

**Options / known design space:**
1. **Compile-time optionality only** — affects diagnostics and tooling surface, but runtime dispatch semantics stay unchanged.
2. **Full runtime optionality** — missing handlers become a benign outcome with explicit evaluator/tooling semantics.
3. **Documentation-only marker** — allowed on the language surface but carries no behavioral meaning beyond author intent.

**Resolution path:** Owner must lock whether `optional` is purely descriptive, diagnostic-affecting, or runtime-affecting before any stage implements it.

---

### CC#22. `SemanticIndex.EnsuresByState`

**Status:** 🔵 Pending team resolution (follows from CC#3)

**Affects:** Type Checker, Language Server, MCP
**Gap register refs:** #26, #73
**The decision:** Should `SemanticIndex` expose an `EnsuresByState` index, and if so what canonical shape should it use?

**Why it's cross-cutting:**
- **Type Checker** — would own construction of the index during semantic binding.
- **Language Server** — can consume the index directly for ensure-aware navigation and tooling features.
- **MCP** — can serialize or query the index instead of reconstructing state/ensure relationships ad hoc.

**Options / known design space:**
1. **Add canonical index to `SemanticIndex`** — one producer, many consumers.
2. **Keep `SemanticIndex` minimal** — LS and MCP each derive the view from existing arrays.
3. **Hybrid** — core emits a lazily computed or optional derived index used only by tooling consumers.

**Resolution path:** Decide whether this is a first-class semantic artifact or a consumer-side convenience view before LS and MCP implement duplicate derivations.

---

### CC#26. Stateless Precepts `CreateInitialVersion` Semantics

**Status:** 🔴 Pending Shane decision (non-blocking for core pipeline)

**Affects:** Runtime API, Evaluator, Graph Analyzer
**Gap register refs:** Audit item E
**The decision:** What should `CreateInitialVersion` do for stateless precepts whose initial `Version.State` is `null`?

**Why it's cross-cutting:**
- **Runtime API** — must define the public contract for creating the first version of a stateless precept.
- **Evaluator** — must know whether to skip state-entry actions, omit-on-entry clearing, and any state-based initialization work.
- **Graph Analyzer** — must know whether stateless precepts are exempt from initial-state validation rules.

**Options / known design space:**
1. **Null-state initial version** — return a version with `State = null` and explicitly skip state-entry semantics.
2. **Synthetic pseudo-state** — represent stateless initialization through an internal sentinel state.
3. **Disallow API path** — require a separate creation path for stateless precepts instead of `CreateInitialVersion`.

**Resolution path:** Lock the stateless creation contract before runtime API, evaluator, and analyzer behavior diverge.

---

## Wave 5 — Doc Sync & Stale Cleanup (Team-Autonomous)

> Wave 5 is pure documentation work — updating canonical docs for already-resolved items and closing out stale gap entries. Tracked in canonical docs, not here.

---

## Coverage Assessment

### Coverage-Review Gaps Now Registered

The previously unregistered coverage-review findings are now tracked in the working registers:

- `docs/working/structural-gap-register.md` now captures structural gaps **#74–85** from `evaluator.md`, `graph-analyzer.md`, `mcp.md`, and `literal-system.md`.
- `docs/working/catalog-gap-register.md` now captures catalog gap **#40** from `tooling-surface.md` covering grammar input catalog coverage.
- This register now captures the audit promotions and new decisions as **#21–#26**, covering the remaining cross-stage items found in `frank-cross-cutting-audit.md`.

### Gaps in Registers That Are Isolated (Single-Doc Only)

The following registered gaps are truly isolated to one stage and don't need cross-cutting treatment:

| # | Gap | Why Isolated |
|---|-----|--------------|
| #8 | `DisambiguationEntry.Offset` | Parser-only concern |
| #12 | `TryLiteralProof` requirement coverage | Proof engine strategy scope |
| #24 | `ModifierMeta.ModifierCategory` | Already resolved in source |
| #25 | `FirePipeline` catalog | MCP output design only |
| #27 | `precept_inspect` composite view exemption | MCP API design only |
| #36 | Grammar generator implementation path | Tooling implementation only |
| #37 | Complex TextMate pattern representation | Tooling-surface only |
| #46 | `ConstructManifest` in graph analyzer | Documentation clarification |
| #55 | `GraphEvent.IsInitial` derivation | Graph analyzer internal |
| #67 | `Version.Slots` array type | Evaluator internal |
| #72 | `precept_inspect` N+1 API calls | MCP API design only |

### Coverage Verdict

**Coverage confidence: ~97%**

The three working registers now provide strong coverage of the significant gaps in the canonical docs. They successfully capture:

- All major type shape mismatches
- All missing fields blocking downstream consumers
- All interface gaps between stages
- The evaluator/tooling interface enrichments identified in the audit
- Most placeholder types needing design

The remaining uncaptured items are implementation-level details that can emerge during coding rather than advance cross-cutting design decisions.

The cross-cutting decisions identified here (Priority 1 especially) represent the critical path for implementation. **Expression tree design** is the single most blocking decision — it affects 5 pipeline stages and cannot be worked around.
