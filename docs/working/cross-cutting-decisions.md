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
| CC#5 | `FieldModifierMeta.ProofDischarges` | ✅ Resolved — `ProofDischarge[]` added to `FieldModifierMeta`; each entry carries `ProofRequirementKind`, optional `OperatorKind? Comparison`, and `decimal? Threshold`, enabling catalog-driven Strategy 2 modifier proof discharge. Canonical: `docs/language/catalog-system.md §FieldModifierMeta`. | Proof Engine Strategy 2 |
| CC#6 | FaultSiteLink to FaultSiteDescriptor Transformation | ✅ Resolved — Option A: nullable `FaultSiteAnnotation?` on each opcode. Builder stamps annotation at compile time from unresolved `FaultSiteLink`s. `null` = proven safe (structural absence = elision). Canonical: `docs/compiler/proof-engine.md §2`, `docs/runtime/precept-builder.md`, `docs/runtime/evaluator.md`. | Proof-to-runtime backstop planting |
| CC#7 | ConstraintMeta DU Subtype Count | ✅ Resolved — Option B (hierarchical, StateAnchored grouping node). Approved 2026-05-06. | Builder constraint routing, catalog-system alignment |
| CC#8 | EventInspection Shape | ✅ Resolved — EventInspection shape adopted; OQ-2 (ArgError = string Reason only) and OQ-3 (RowEffect DU) closed. OQ-4 (EventEnsures timing) pending. Canonical: `result-types.md` + `evaluator.md`. Approved 2026-05-06. | Evaluator inspection contract, LS preview, MCP DTO shape |
| CC#9 | `ConstraintFieldRefs.ConstraintIdentity` Type | ✅ Resolved — uses proof-engine ConstraintIdentity DU. Resolved 2026-05-06. | Type Checker and Proof Engine constraint identity alignment |
| CC#10 | GraphState Modifier Representation | ✅ Resolved — Flat boolean flags (`IsInitial`, `IsTerminal`, `IsRequired`, `IsIrreversible`, `IsReachable`) retained. Graph analyzer derives them catalog-driven from `StateModifierMeta` fields at construction time. `GraphState` carries derived structural conclusions, not the raw modifier list (which remains on `TypedState.Modifiers` in `SemanticIndex`). Canonical: `docs/compiler/graph-analyzer.md §4`. | Graph output shape, modifier-derived facts |
| CC#11 | `ExecutionRow.RejectReason` Field | ✅ Resolved — string? RejectReason added to TypedTransitionRow and ExecutionRow. Resolved 2026-05-06. | Reject-row lowering, evaluator rejection outcomes |
| CC#12 | `Faulted(Fault)` as EventOutcome Variant | ✅ Resolved — `Faulted(Fault)` added as 8th variant. Canonical: `result-types.md`. | Evaluator outcome DU, MCP fire result serialization |
| CC#13 | `FaultCode.AmbiguousDispatch` | ✅ Resolved — `FaultCode.AmbiguousDispatch` confirmed with `[StaticallyPreventable(DiagnosticCode.AmbiguousDispatch)]`. `DiagnosticCode.AmbiguousDispatch` added (Proof stage, Error severity). Evaluator fires it on the impossible-path multi-candidate backstop (`candidates.Count > 1`). Canonical: `docs/runtime/evaluator.md §7.1`, `docs/compiler/diagnostic-system.md`. | Evaluator impossible-path faulting, diagnostics linkage |
| CC#14 | SlotContext vs SlotKind Enum Naming | ✅ Resolved — `SlotContext` is the canonical cursor-context enum name. The mapping switch uses `ConstructSlotKind` members (catalog type name), not a `SlotKind` alias. Two distinct concepts: `SlotContext` = where is the cursor; `ConstructSlotKind` = what schema slot is this. Canonical: `docs/tooling/language-server.md §7.3`. | LS/tooling completion context contract |
| CC#15 | `precept/inspect` vs `precept/preview` Naming | ✅ Resolved — `precept/inspect` is canonical. Aligns with MCP tool name `precept_inspect` — "inspect" names the operation semantically, "preview" was a UX alias. Canonical: `docs/tooling/language-server.md §7.6`. | LS custom method name, tooling preview routing |
| CC#16 | `TypeMeta.IsUserFacing` for Completions | ✅ Resolved — `bool IsUserFacing = true` added to `TypeMeta` as first-class catalog field. Default `true` (most types). `Error` and `StateRef` have `IsUserFacing = false`. Derived filtering is insufficient — the distinction is domain knowledge about each type. Canonical: `docs/language/catalog-system.md §TypeMeta`. | Completion filtering |
| CC#17 | `TypedArg.EventName` Back-Reference | ✅ Resolved — `TypedArg.EventName` back-reference already exists in the canonical `type-checker.md §7.1` shape. LS hover accesses `a.EventName` directly. No additional field needed. Canonical: `docs/compiler/type-checker.md §7.1`. | Arg hover/navigation |
| CC#18 | ConstructMeta Outline Properties | ✅ Resolved — `bool IsOutlineNode = false` and `string? LspSymbolKind = null` added to `ConstructMeta`. Matches `TokenMeta.SemanticTokenType` pattern. LS reads these instead of hardcoding `ConstructKind` switches. Canonical: `docs/language/catalog-system.md §ConstructMeta`, `docs/tooling/language-server.md §7.7`. | Document symbols / outline |
| CC#19 | `TokenMeta.HoverDescription` Strategy | ✅ Resolved — Option A: `string? HoverDescription` added to `TokenMeta` as first-class catalog field. Matches existing pattern on `FieldModifierMeta`, `FunctionMeta`, `OperatorMeta`. Hover descriptions are domain knowledge about language elements — they belong in the catalog. Canonical: `docs/language/catalog-system.md §Tokens`. | Hover/completion documentation |
| CC#20 | Diagnostic Related Locations | ✅ Resolved — `ImmutableArray<SourceSpan> RelatedLocations = default` added to `Diagnostic`. Default empty — existing diagnostics unaffected. LS mapper emits LSP `relatedInformation` entries when non-empty. Canonical: `docs/compiler/diagnostic-system.md`. | Multi-span diagnostics, LSP relatedInformation |
| CC#21 | Event `optional` Modifier | ✅ Resolved — old `UnhandledEvent` removed (violates §0.6 principle 2); `UnhandledEvent` recycled with tighter definition: zero handlers in ANY state = provably dead. `optional` modifier moot. | Graph Analyzer, Diagnostics |
| CC#22 | `SemanticIndex.EnsuresByState` | ✅ Resolved — `FrozenDictionary<string, ImmutableArray<TypedEnsure>> EnsuresByState` added to `SemanticIndex`. Follows CC#3 primary-array + secondary-index pattern. `SemanticIndex.Ensures` remains the primary ordered array; `EnsuresByState` is the derived O(1) lookup for LS/MCP ensure navigation. Canonical: `docs/compiler/type-checker.md §7.1`. | LS/MCP ensure navigation and indexing |
| CC#23 | `EventOutcome.mutations` Payload | ✅ Resolved — Option A: `ImmutableArray<FieldMutation> Mutations` attached to `Transitioned` and `Applied`. Canonical: `result-types.md`. | Evaluator outcome contract, MCP fire payload |
| CC#24 | Unmatched Guard Trace Enrichment | ✅ Resolved — Option A: `Unmatched(ImmutableArray<TransitionInspection> EvaluatedRows)` — same type as inspect path. Canonical: `result-types.md`. | Evaluator unmatched contract, MCP diagnostics |
| CC#25 | Execution Dispatch Delegate Design | [Decided] | Evaluator, runtime value model, builder plan lowering, catalog runtime metadata |
| CC#26 | Stateless Precepts `CreateInitialVersion` Semantics | ✅ Resolved 2026-05-06 — Option 1: null-state initial version. `Version.State = null`, state-entry semantics skipped, all other construction machinery runs normally. Graph analyzer exempt from initial-state/dead-end/unreachable-state checks for stateless precepts. Canonical: `docs/runtime/runtime-api.md §Stateless Precepts`. | Runtime API, Evaluator, Graph Analyzer |

## Dependency Map

| Canonical doc / stage | Blocking CC decisions | Why this matters |
|---|---|---|
| `docs/compiler/parser.md` | CC#1, CC#2 | Expression nodes and slot shapes must be fixed before parser text can stabilize. |
| `docs/compiler/type-checker.md` | CC#1, CC#2, CC#3, CC#7, CC#9, CC#22 | SemanticIndex shape and constraint identity are the type-checker contracts other stages read. |
| `docs/compiler/proof-engine.md` | CC#1, CC#5, CC#6, CC#9 | Proof discharge and proof-to-runtime fault-site identity define what residue crosses downstream. |
| `docs/compiler/graph-analyzer.md` | CC#10, CC#26 | Graph facts depend on modifier representation and stateless creation rules. |
| `docs/runtime/precept-builder.md` | CC#4, CC#6, CC#7, CC#11, CC#25 | The compile-to-runtime boundary owns compilation aggregate shape, fault-site planting, constraint routing, reject-row lowering, and execution-plan dispatch. |
| `docs/runtime/evaluator.md` | CC#8, CC#11, CC#12, CC#13, CC#23, CC#24, CC#25, CC#26 | Runtime result shapes, impossible-path faults, mutation reporting, trace richness, and stateless creation semantics converge here. |
| `docs/runtime/runtime-api.md` | CC#25, CC#26 | Public API wording must match the locked runtime baseline and stateless initialization contract. |
| `docs/tooling/language-server.md` | CC#3, CC#8, CC#14, CC#15, CC#17, CC#18, CC#19, CC#22 | LS contracts mirror the semantic index, preview method naming, outline metadata, and hover/documentation metadata. |
| `docs/tooling/mcp.md` | CC#8, CC#12, CC#22, CC#23, CC#24 | MCP DTOs are thin wrappers; they cannot drift from evaluator and SemanticIndex contracts. |
| `docs/language/catalog-system.md` | CC#5, CC#7, CC#16, CC#19, CC#25 | Catalog metadata is the machine-readable language spec; these decisions decide which metadata must exist. |
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

- [x] [Shane] Resolve CC#3 `SemanticIndex` reference-collection contract. [Blocks: `docs/compiler/type-checker.md §7.1`, `docs/tooling/language-server.md §7.3`, `docs/compiler/tooling-surface.md`]
- [x] [Shane] Resolve CC#4 `Compilation.Tokens` access path. [Blocks: `docs/runtime/precept-builder.md §2`, lexical semantic-token flow]
- [x] [Shane] Resolve CC#6 proof-to-runtime `FaultSiteLink` lowering. [Blocks: `docs/compiler/proof-engine.md §2`, `docs/runtime/precept-builder.md`, evaluator fault routing]
- [x] [Shane] Resolve CC#7 `ConstraintMeta` DU hierarchy. [Blocks: builder constraint buckets, catalog-system constraint metadata]
- [x] [Shane] Resolve CC#8 canonical `EventInspection` shape. [Blocks: evaluator inspection contract, LS preview, MCP DTOs]
- [x] [Team] Apply the CC#7 ruling to CC#9 `ConstraintFieldRefs.ConstraintIdentity`. [Blocked by: CC#7] [Blocks: type-checker/proof-engine shared identity data]
- [x] [Team] Add canonical storage for reject-row `because` text (CC#11). [Blocked by: none] [Blocks: `docs/runtime/precept-builder.md §ExecutionRow`, `docs/runtime/evaluator.md` rejection outcomes]
- [x] [Team] Apply the CC#8 ruling to CC#12 `Faulted(Fault)` outcome handling. [Blocks: evaluator/MCP outcome parity]
- [x] [Shane] Resolve CC#23 `EventOutcome.mutations` ownership. [Blocks: evaluator result contract, `docs/tooling/mcp.md §precept_fire`]
- [x] [Shane] Resolve CC#24 unmatched-guard trace richness. [Blocks: evaluator unmatched contract, `docs/tooling/mcp.md §precept_fire`]

### Exit Criteria

Wave 1 is done when the cross-stage shape questions stop living only in this driver and each affected canonical doc can state a single contract without cross-doc disagreement.

## Wave 2 — Stage-Local Resolutions

### Execution Checklist

- [x] [Team] Close CC#5 `FieldModifierMeta.ProofDischarges` using the already-drafted catalog shape. [Blocks: `docs/language/catalog-system.md`, proof-engine Strategy 2]
- [x] [Team] Close CC#10 `GraphState` modifier representation. [Blocks: `docs/compiler/graph-analyzer.md` graph output shape]
- [x] [Team] Add CC#13 `FaultCode.AmbiguousDispatch` plus linked diagnostic metadata. [Blocks: evaluator impossible-path faulting, diagnostics linkage]
- [x] [Team] Unify CC#14 `SlotContext` vs `SlotKind` naming. [Blocks: LS/tooling completion context docs]
- [x] [Team] Lock CC#15 `precept/inspect` vs `precept/preview` naming and apply it everywhere. [Blocks: LS preview command, tooling surface docs]
- [x] [Team] Decide whether CC#16 `TypeMeta.IsUserFacing` becomes first-class catalog metadata. [Blocks: completion filtering docs]
- [x] [Team] Close CC#17 `TypedArg.EventName` back-reference routing. [Blocks: arg hover/navigation docs]
- [x] [Team] Close CC#18 outline metadata on `ConstructMeta`. [Blocks: document-symbol / outline docs]
- [x] [Team] Standardize CC#19 hover/snippet documentation metadata strategy. [Blocks: catalog-system hover/completion docs]
- [x] [Team] Decide CC#20 diagnostic related-location support. [Blocks: `docs/compiler/diagnostic-system.md`, LS `relatedInformation` mapping]
- [x] [Shane] Lock CC#21 end-to-end semantics for the event `optional` modifier. ✅ Resolved 2026-05-06: old `UnhandledEvent` removed, `UnhandledEvent` recycled (tighter: zero handlers in ANY state), `optional` moot. Name approved by Shane 2026-05-06.
- [x] [Team] Apply the CC#3 ruling to CC#22 `SemanticIndex.EnsuresByState`. [Blocked by: CC#3] [Blocks: LS/MCP ensure navigation]
- [x] [Shane] Lock CC#26 stateless `CreateInitialVersion` semantics. ✅ Resolved 2026-05-06: Option 1 — null-state initial version. `Version.State = null`, state-entry semantics skipped, all other construction machinery runs normally. Graph analyzer exempt from initial-state/dead-end/unreachable-state checks for stateless precepts.

### Exit Criteria

Wave 2 is done when the remaining single-stage or lightly coupled questions become mechanical documentation and implementation work instead of architecture debates.

## Wave 3 — Open Question Resolution ✅ COMPLETE

### Execution Checklist

- [x] [Team] Use `docs/working/Archived/structural-gap-register-migrated.md` as a routing index only; burn down each migrated structural open question in its owning canonical doc. [Blocked by: Waves 1–2]
- [x] [Team] Use `docs/working/Archived/catalog-gap-register-migrated.md` as a routing index only; burn down each migrated catalog open question in `docs/language/catalog-system.md` and dependent docs. [Blocked by: Waves 1–2]
- [x] [Team] Sweep `docs/compiler/type-checker.md`, `docs/compiler/proof-engine.md`, `docs/runtime/precept-builder.md`, `docs/runtime/evaluator.md`, `docs/tooling/language-server.md`, `docs/tooling/mcp.md`, `docs/language/catalog-system.md`, `docs/compiler/graph-analyzer.md`, and `docs/compiler/diagnostic-system.md` so the canonical docs become the only live trackers. [Blocked by: Waves 1–2]
- [x] [Shane] Review any item that remains a real product choice after the team closes the mechanical migrations. — Reviewed; 6 follow-up gaps were preserved and resolved in Wave 4 (all team-autonomous).

### Canonical-Doc Burn-Down Order

1. `docs/compiler/type-checker.md` — CC#3, CC#9, CC#22
2. `docs/compiler/proof-engine.md` + `docs/runtime/precept-builder.md` — CC#6, CC#7, CC#11
3. `docs/runtime/evaluator.md` + `docs/tooling/language-server.md` + `docs/tooling/mcp.md` — CC#8, CC#12, CC#23, CC#24
4. `docs/language/catalog-system.md` + `docs/compiler/graph-analyzer.md` + `docs/compiler/diagnostic-system.md` — CC#5, CC#10, CC#13, CC#16–CC#21, CC#26

### Outcome

Wave 3 is complete. 33 open question markers closed across 9 canonical docs in two rounds (Round 1: type-checker, proof-engine, precept-builder; Round 2: evaluator, language-server, mcp, catalog-system, graph-analyzer, diagnostic-system). Six genuine follow-up gaps deferred to Wave 4 for triage.

## Wave 4 — Doc Finalization ✅ COMPLETE

### Execution Checklist

- [x] [Team] Run a final consistency pass across compiler, runtime, tooling, and catalog docs after Wave 3 closes. [Blocked by: Wave 3]
- [x] [Team] Replace stale `pending`, `provisional`, and cross-doc disagreement language for any CC decision already locked. [Blocked by: Wave 3]
- [x] [Team] Update `docs/compiler/README.md` and any navigation tables so canonical docs are the first and only destination. [Blocked by: Wave 3]
- [x] [Shane] Sign off any owner-only open question — no owner-required gaps found; all 6 Wave 3 follow-up gaps were team-autonomous.

### Outcome

Wave 4 is complete. All 6 Wave 3 follow-up gaps were resolved as team-autonomous (no product-semantic decisions required):
1. **`TokenMeta.SemanticTokenModifiers`** — No field added; Precept tokens carry zero modifier bits. LS hardcodes `tokenModifiers: 0`.
2. **`EventCoverageEntry` granularity** — Stays at event-level; guard-conditioned coverage is the proof engine's domain.
3. **Back-edge definition** — BFS-ancestor is canonical; DFS back-edges not used.
4. **`GraphEvent.IsInitial` derivation** — Derived from outgoing edges of the initial state (structural, no metadata lookup).
5. **TBD structural diagnostic codes** — Assigned: 82=`TerminalStateHasOutgoingEdges`, 83=`IrreversibleStateHasBackEdge`, 84=`RequiredStateDoesNotDominateTerminal`, 85=`NoInitialState`. Proof engine codes start at 86.
6. **`ActionMeta` LS/MCP alignment** — `Description` surfaces in both LS hover and MCP vocabulary; `SyntaxShape` is internal; `SnippetTemplate` is a deferred implementation milestone.

Stale language cleaned: 3 graph-analyzer OQ blocks closed, 2 catalog-system OQs closed (SemanticTokenModifiers + ConstructSlotKind count), LS tokenModifiers comment updated, proof-engine source file note fixed, 6 `precept/preview` occurrences in tooling-surface.md corrected to `precept/inspect`. `docs/compiler/README.md` updated with superseded-doc entries.

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

**Status:** ✅ Resolved (2026-05-06)

**Affects:** Proof Engine, Modifiers Catalog
**Gap register refs:** #1

**Ruling:**

Add `ProofDischarge[] ProofDischarges` to `FieldModifierMeta`. The field is defaulted to `[]` for modifiers with no discharge implications.

```csharp
public sealed record ProofDischarge(
    ProofRequirementKind RequirementKind,
    OperatorKind? Comparison,   // non-null for Numeric requirements
    decimal? Threshold          // non-null for Numeric requirements
);
```

Discharge table (locked):

| Modifier | ProofDischarges |
|----------|-----------------|
| `positive` | `[Numeric(>, 0), Numeric(!=, 0)]` — strictly positive implies nonzero |
| `nonnegative` | `[Numeric(>=, 0)]` |
| `nonzero` | `[Numeric(!=, 0)]` |
| `notempty` | `[Numeric(count, >, 0)]` — collection count is positive |
| `min(N)` | `[Numeric(>=, N)]` |
| `max(N)` | `[Numeric(<=, N)]` |
| all others | `[]` |

The proof engine Strategy 2 walks the field's `Modifiers` array, calls `Modifiers.GetMeta(kind)`, casts to `FieldModifierMeta`, and checks `ProofDischarges` against the `NumericProofRequirement` being discharged. No per-modifier switch in the proof engine — the discharge mapping lives entirely in catalog metadata.

**Rationale:** The draft shape was already captured in `proof-engine.md §Strategy 2`. This decision locks the catalog shape and discharge table so Strategy 2 is implementable. "Modifier discharges obligation" is domain knowledge — it belongs in the modifier's metadata, not in engine code.

**Canonical:** `docs/language/catalog-system.md §FieldModifierMeta`, `docs/compiler/proof-engine.md §Strategy 2`.

---

### CC#10. GraphState Modifier Representation

**Status:** ✅ Resolved (2026-05-06)

**Affects:** Graph Analyzer, Modifiers Catalog
**Gap register refs:** #9, #54

**Ruling:** Retain flat boolean flags (`IsInitial`, `IsTerminal`, `IsRequired`, `IsIrreversible`, `IsReachable`) on `GraphState`. Do **not** add a redundant `ImmutableArray<ModifierKind> Modifiers` field.

**Rationale:**

`GraphState` is a structural analysis *output* — a record of derived conclusions, not a source model. The booleans are the graph analyzer's answers to structural questions: "is this state initial?", "is it terminal?", etc. These are correct as derived boolean conclusions.

- The graph analyzer's §5.2 already shows catalog-driven modifier derivation *inside* the analyzer — it reads `StateModifierMeta.IsInitial`, `IsTerminal`, etc. from the catalog to populate the booleans. This is exactly the right architecture.
- `IsReachable` is purely topological (computed from graph traversal), not modifier-derived at all — confirming `GraphState` is a "derived facts record", not a modifier passthrough.
- `SemanticIndex.TypedState.Modifiers` already carries the raw `ImmutableArray<ModifierKind>` for consumers that need the modifier list. Duplicating it on `GraphState` would be redundant and coupling.

The open question in `graph-analyzer.md §4` is closed. The flat boolean shape is canonical.

**Canonical:** `docs/compiler/graph-analyzer.md §4`.

---

### CC#13. FaultCode.AmbiguousDispatch

**Status:** ✅ Resolved (2026-05-06)

**Affects:** Evaluator, Faults Catalog, Diagnostics Catalog
**Gap register refs:** #4

**Ruling:**

Confirm `FaultCode.AmbiguousDispatch` with the full `[StaticallyPreventable]` linkage chain:

- **`DiagnosticCode.AmbiguousDispatch`** — Stage: `Proof`, Severity: `Error`, message template: `"Guard expressions are provably ambiguous — multiple rows can simultaneously match in state '{0}' on event '{1}'"`.
- **`FaultCode.AmbiguousDispatch`** — `[StaticallyPreventable(DiagnosticCode.AmbiguousDispatch)]`. The evaluator fires this fault on the impossible-path backstop: `if (candidates.Count > 1) return Fail(FaultCode.AmbiguousDispatch)`.

The evaluator's `§7.1 Fire` already references `FaultCode.AmbiguousDispatch` — this closes the gap by making the fault code and its diagnostic counterpart fully specified.

The proof engine emits `DiagnosticCode.AmbiguousDispatch` when two guard expressions in the same `(state, event)` pair are simultaneously satisfiable.

**Rationale:** The proof engine statically prevents this by analyzing guard satisfiability. The runtime backstop exists as the last-resort defense for proofs that could not run (e.g., dynamic types). Both sides of the chain must name the same fault concept — this ruling supplies the formal definitions.

**Canonical:** `docs/compiler/diagnostic-system.md`, `docs/runtime/evaluator.md §7.1`.

---

### CC#14. SlotContext vs SlotKind Enum Naming

**Status:** ✅ Resolved (2026-05-06)

**Affects:** Language Server, Tooling Surface
**Gap register refs:** #38, #69

**Ruling:** `SlotContext` is the canonical name for the cursor-context enum. `SlotKind` is not a real type and must not be used.

The mapping function `GetCursorContext()` in `language-server.md §7.3` maps `ConstructSlotKind` (the catalog type name for a construct's slot schema) → `SlotContext` (what kind of completion context the cursor is in). The two names represent two different concepts:

- `ConstructSlotKind` — where in a construct schema this slot appears (e.g., `TypeExpression`, `FieldName`). Lives in the catalog. Read-only during parsing.
- `SlotContext` — what the cursor is currently positioned inside, for completion routing. Lives in the LS. Produced by `GetCursorContext()`.

Any reference to `SlotKind.X` in `language-server.md §7.3` should read `ConstructSlotKind.X` in the switch arms.

**Canonical:** `docs/tooling/language-server.md §7.3`.

---

### CC#15. precept/inspect vs precept/preview Naming

**Status:** ✅ Resolved (2026-05-06)

**Affects:** Language Server, Tooling Surface
**Gap register refs:** #32, #68

**Ruling:** `precept/inspect` is the canonical custom LSP method name. `precept/preview` was a UX alias that must be removed.

Rationale: `precept/inspect` aligns with the MCP tool `precept_inspect`. The semantics are "inspect the precept's current state and event outcomes" — this is inspection, not preview. The §4 overview table already uses `precept/inspect`; the §7.6 trigger line was inconsistently left as `precept/preview`. That inconsistency is closed by this ruling.

**Canonical:** `docs/tooling/language-server.md §7.6`.

---

### CC#16. TypeMeta.IsUserFacing for Completions

**Status:** ✅ Resolved (2026-05-06)

**Affects:** Language Server, Types Catalog
**Gap register refs:** #30

**Ruling:** Add `bool IsUserFacing = true` to `TypeMeta` as a first-class catalog field. Default is `true`. `Error` and `StateRef` have `IsUserFacing = false`.

**Rationale:** Whether a type appears in user-facing completion lists is domain knowledge about each type — it cannot be reliably derived from `Token == null` (`StateRef` has no token but is meaningfully different from `Error`; both should be non-user-facing for different reasons). Multiple consumers (LS completions, MCP vocabulary output) need this filter. Following the catalog-driven architecture principle: if the behavior varies per type member and the reason is "the language says so," the behavior belongs in the catalog.

**Alternatives rejected:**
- *Derive from `Token == null`*: Incorrect — `StateRef` has no token in the typical sense but is not an error type.
- *Let each consumer hardcode exceptions*: Each consumer maintains parallel per-member knowledge that belongs in the catalog.

**Canonical:** `docs/language/catalog-system.md §TypeMeta`.

---

### CC#17. TypedArg.EventName Back-Reference

**Status:** ✅ Resolved (2026-05-06)

**Affects:** Type Checker, Language Server
**Gap register refs:** #31, #52

**Ruling:** `TypedArg.EventName` already exists as a first-class field in the canonical `type-checker.md §7.1` shape:

```csharp
public sealed record TypedArg(
    string Name,
    string EventName,   // back-reference to the owning event
    TypeKind ResolvedType,
    ...
);
```

The LS hover at `§7.4` accesses `a.EventName` directly. No additional reconstruction via `ArgReference` traversal is needed. No new field addition is required — the field already exists.

The open question in `language-server.md §7.4` is closed: the back-reference routing is resolved by the existing `TypedArg.EventName` field.

**Canonical:** `docs/compiler/type-checker.md §7.1`, `docs/tooling/language-server.md §7.4`.

---

### CC#18. ConstructMeta Outline Properties

**Status:** ✅ Resolved (2026-05-06)

**Affects:** Language Server, Constructs Catalog
**Gap register refs:** #34, #70, #71

**Ruling:** Add `bool IsOutlineNode = false` and `string? LspSymbolKind = null` to `ConstructMeta`.

`LspSymbolKind` is a string constant matching the LSP `SymbolKind` value names (e.g., `"Class"`, `"Property"`, `"Enum"`, `"Function"`) — same pattern as `TokenMeta.SemanticTokenType`.

The LS `textDocument/documentSymbol` handler reads `ConstructMeta.IsOutlineNode` and `ConstructMeta.LspSymbolKind` from the catalog instead of maintaining a hardcoded `ConstructKind` switch. Adding a new outline-able construct only requires updating the catalog entry.

**Rationale:** "Is this construct an outline node?" and "what LSP symbol kind does it map to?" are domain knowledge about each construct — they vary per member for language-structural reasons. The hardcoded `IsOutlineConstruct()` and `MapSymbolKind()` switch functions in the LS are exactly the per-member dispatch that the catalog-driven architecture prohibits. Follows the `TokenMeta.SemanticTokenType` precedent.

**Alternatives rejected:**
- *Keep hardcoded switches*: Violates the catalog-driven principle; requires LS changes for every new construct.
- *Single `string? LspOutlineKind` field only*: `IsOutlineNode` is a distinct boolean for constructs that are outline nodes but have no LSP symbol kind — the separation is clean.

**Canonical:** `docs/language/catalog-system.md §ConstructMeta`, `docs/tooling/language-server.md §7.7`.

---

### CC#19. TokenMeta.HoverDescription Strategy

**Status:** ✅ Resolved (2026-05-06)

**Affects:** Language Server, Tooling Surface, Tokens Catalog
**Gap register refs:** #5, #35

**Ruling:** Option A — add `string? HoverDescription` to `TokenMeta` as a first-class catalog field.

**Rationale:** `FieldModifierMeta`, `FunctionMeta`, and `OperatorMeta` all already carry a `string? HoverDescription` field. The inconsistency is that `TokenMeta` (keywords, type names, modifiers) lacks one. Hover descriptions for keywords and operators are domain knowledge about those language elements — they belong in the catalog, exactly as they do for modifiers, functions, and operators.

**Alternatives rejected:**
- *Derive from C# XML doc comments*: Couples tooling metadata to source structure. Doc comments exist for developer-facing API documentation, not for user-facing hover text.
- *Separate "documentation catalog"*: A 14th catalog serving only one consumer with one field type is architectural overhead. The pattern already exists on the type — add the field.
- *Let LS hardcode per-token hover strings*: Per-member string knowledge that the catalog should own.

**Canonical:** `docs/language/catalog-system.md §Tokens`.

---

### CC#20. Diagnostic Related Locations

**Status:** ✅ Resolved (2026-05-06)

**Affects:** Diagnostic System, Language Server
**Gap register refs:** #39

**Ruling:** Add `ImmutableArray<SourceSpan> RelatedLocations = default` to the `Diagnostic` `readonly record struct`.

- `default` means `ImmutableArray<SourceSpan>.Empty` — existing diagnostics (and their construction sites) are unaffected.
- The LS mapper emits LSP `relatedInformation` entries when `RelatedLocations.Length > 0`, using the parent diagnostic's `Message` as each span's message (the relationship is implied by context, not repeated per-span).
- No per-span message field is added — Precept's diagnostic scale doesn't warrant it.

**Rationale:** Multi-span diagnostics are real (e.g., "field declared at line 12, constraint referencing it at line 31"). Without `RelatedLocations`, the LS can only show a single span per diagnostic. The Roslyn model uses `DiagnosticDescriptor.AdditionalLocations` for the same purpose; the concept is established.

The `readonly record struct` can gain a field with a default without breaking existing construction — C# `with` expression compatibility is maintained. All existing `new Diagnostic(...)` call sites continue to compile.

**Canonical:** `docs/compiler/diagnostic-system.md`.

---

### CC#21. Event "optional" Modifier

**Status:** ✅ Resolved (2026-05-06)

**Affects:** Graph Analyzer, Diagnostics
**Gap register refs:** #10

**Ruling:**

1. **Remove the old `UnhandledEvent` diagnostic entirely.** A partial event-handler matrix (event handled in some states but not others) is valid, intentional authoring — not broken. Flagging it violates §0.6 principle 2 ("only flag proven violations"). The check is removed from the graph analyzer; the diagnostic code is removed from the catalog.

2. **Add `UnhandledEvent` diagnostic (recycled name, tighter definition, Warning).** An event declared with zero transition rows handling it in *any* state is a provably dead declaration. Emit `DiagnosticCode.UnhandledEvent` (Warning, Structure category). Algorithm: if `handlingStates` is empty for an event → emit diagnostic.

   **Name note:** The name `UnhandledEvent` was chosen by Elaine and approved by Shane. It names the structural cause (no handlers exist) rather than implying parentage ("orphan"). It also distinguishes cleanly from `EventNeverSucceeds` — the event isn't merely blocked in some execution path; it has *no* handler rows anywhere and therefore cannot fire successfully under any condition. The old `UnhandledEvent` meant "event not handled in some states" (partial coverage). The new `UnhandledEvent` means "event not handled in ANY state" — a strictly narrower, provably-broken case. Ordinal 81 is retained for stability.

3. **`optional` event modifier: moot.** With the old broad `UnhandledEvent` gone, there is nothing to suppress — no modifier needed.

**Rationale:** The distinction is partial coverage (intentional — author's prerogative) vs. zero coverage (provably dead — definitively broken). Only the latter warrants a diagnostic.

---

### CC#22. `SemanticIndex.EnsuresByState`

**Status:** ✅ Resolved (2026-05-06)

**Affects:** Type Checker, Language Server, MCP
**Gap register refs:** #26, #73

**Ruling:** Option 1 — add `FrozenDictionary<string, ImmutableArray<TypedEnsure>> EnsuresByState` to `SemanticIndex`, following the CC#3 primary-array + secondary-index pattern.

- `SemanticIndex.Ensures` remains the primary ordered array.
- `EnsuresByState` is the derived secondary index, built at `SemanticIndex` construction time.
- Only state-anchored ensures (Kind ∈ `{StateResident, StateEntry, StateExit}` where `AnchorState != null`) appear as values. Event-anchored ensures and invariants are NOT included.
- The key is the state name string.

**Rationale:** The type checker already walks all ensures during `SemanticIndex` construction — building the index there is zero additional work at call time. LS ensure navigation and MCP inspection need "what ensures exist for state X?" without O(n) scan of the primary array. One producer, many consumers. Follows CC#3: the dual-index approach (primary ordered + secondary keyed) is the resolved contract for `SemanticIndex` secondary lookups.

**Alternatives rejected:**
- *Keep SemanticIndex minimal; let consumers derive*: Two consumers (LS + MCP) would implement duplicate derivations, which the CC#3 ruling explicitly prevents.
- *Lazy/optional computed index*: Adds surface complexity for marginal benefit — the type checker owns the data and should populate both.

**Canonical:** `docs/compiler/type-checker.md §7.1`.

---

### CC#26. Stateless Precepts `CreateInitialVersion` Semantics

**Status:** ✅ Resolved 2026-05-06 — Option 1: Null-state initial version

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

---

**Ruling — Option 1 locked by Shane (2026-05-06):**

`CreateInitialVersion` for stateless precepts (no states declared) returns a `Version` with `State = null`. No separate API path, no hidden sentinel state.

**Per-component behavior:**

- **Runtime API:** `Version.State` is `null` for stateless precepts. This is the honest representation of "no state machine" — not an error, not a sentinel, the natural degenerate case. `CreateInitialVersion` returns this version when construction succeeds.
- **Evaluator:** The state-set step (building the hollow version with an initial state) is omitted. State-entry semantics do not fire because there is no state to enter: no `to <InitialState> ensure` guards, no `in <InitialState> ensure` residency checks, no omit-on-entry clearing. All other construction machinery runs normally — the initial event fires if declared, arg ensures are evaluated, field constraints are checked, global rules (`always`) are evaluated, computed fields are recomputed, and the working copy promotion/discard protocol applies. Construction succeeds if the initial event succeeds.
- **Graph Analyzer:** Stateless precepts are exempt from initial-state reachability checks, dead-end-state checks, and unreachable-state checks. These checks require a state machine to operate on. A precept with no states declared has no topology to validate.

**Rationale:**
- The spec (§0.2) defines stateless configuration as "current field values alone" — `null` state is the honest representation of this. Anything else adds hidden behavior inconsistent with the metadata-driven, no-implicit-behavior principle.
- Option 2 (synthetic sentinel state) would introduce a state that the author never declared, violating the principle that pipeline stages are generic machinery reading catalog metadata — not machinery that invents undeclared structure.
- Option 3 (separate API path) adds public API surface with no language justification — the spec already handles parameterless construction when no initial event is declared, and the null-state contract extends this cleanly.

**Canonical doc locations:**
- Full contract: `docs/runtime/runtime-api.md §Stateless Precepts — CreateInitialVersion`
- Evaluator behavior: `docs/runtime/evaluator.md §Create`
- Graph analyzer exemptions: `docs/compiler/graph-analyzer.md §8.1`
- Language spec: `docs/language/precept-language-spec.md §3A.5`

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
