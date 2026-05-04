# Structural Gap Register

> **Migration completed:** 2026-05-04
> **Total entries:** 46
> **Breakdown:** 44 migrated to canonical docs; 2 already resolved by recorded decisions (#45, #53).
> **Canonical docs updated:** `docs/compiler/parser.md`, `docs/compiler/type-checker.md`, `docs/compiler/graph-analyzer.md`, `docs/compiler/proof-engine.md`, `docs/runtime/precept-builder.md`, `docs/runtime/evaluator.md`, `docs/tooling/language-server.md`, `docs/tooling/mcp.md`, `docs/compiler/literal-system.md`.

> Companion to `docs/working/catalog-gap-register.md`.
> Covers gaps in the structural shapes of data flowing between pipeline stages and adjacent interfaces.
> Gaps are promoted from here into the relevant canonical doc once decided.

## How to Use This Register

A **structural gap** is a place where:
- A stage's output type has a field referenced downstream that doesn't exist
- Two docs describe the same type with different shapes
- A type carries `SourceSpan` as a placeholder where a richer structure is needed
- A downstream stage re-derives data the upstream stage should have included
- A result type's DU is missing a variant consumers expect
- A stage interface is described inconsistently across docs

Gaps move through this lifecycle:

1. **Pending Decision** — identified, needs owner ruling before shape fix
2. **Decided** — owner has ruled; implementation can proceed
3. **Updated** — shape fixed in canonical doc

Items are organized by the **producing stage** — parser output, type checker output, etc.

## Status Key

| Status | Meaning |
|--------|---------|
| Shape Mismatch | Two docs describe the same type with conflicting shapes — canonical shape must be decided |
| Missing Field | A field referenced by a downstream consumer doesn't exist on the type |
| Placeholder | `SourceSpan` or `object` used where a richer type is needed — full type design pending |
| Missing Variant | A DU is missing a variant that downstream consumers expect |
| Interface Gap | Stage interface (in/out contract) is undefined or inconsistent |
| Pending Decision | Needs owner ruling before canonical shape can be established |

## Gap Register

Organized by producing stage. **Numbering continues from catalog-gap-register.md (#40+).**

### Parser Output

| # | Gap | Priority | Status | Source Doc(s) | Notes |
|---|-----|----------|--------|---------------|-------|
| 40 | `SlotValue` subtype count mismatch (15 vs 17) | Medium | MIGRATED | parser.md line 36, catalog-system.md | Migrated to `docs/compiler/parser.md` §2.2 SlotValue: 17-Subtype Discriminated Union |
| 41 | `TypeExpressionSlot` shape conflict | High | MIGRATED | parser.md line 51, type-checker.md line 51 | Migrated to `docs/compiler/parser.md` §2.2 SlotValue: 17-Subtype Discriminated Union |
| 42 | `ModifierListSlot` shape conflict | High | MIGRATED | parser.md line 42, type-checker.md line 52 | Migrated to `docs/compiler/parser.md` §2.2 SlotValue: 17-Subtype Discriminated Union |
| 43 | `AccessModeSlot` shape conflict | High | MIGRATED | parser.md line 53, type-checker.md line 63 | Migrated to `docs/compiler/parser.md` §2.2 SlotValue: 17-Subtype Discriminated Union |
| 44 | `BecauseClauseSlot` shape conflict | Medium | MIGRATED | parser.md line 52, type-checker.md line 62 | Migrated to `docs/compiler/parser.md` §2.2 SlotValue: 17-Subtype Discriminated Union |
| 45 | Expression-carrying slots hold `SourceSpan` only | High | RESOLVED | parser.md line 60, type-checker.md line 70 | Resolved by the 2026-05-04 `ParsedExpression` / `TypedExpression` decision; canonical docs now document typed expression DU output |
| 46 | `ConstructManifest` missing from graph analyzer inputs | Low | MIGRATED | parser.md line 283 | Migrated to `docs/compiler/parser.md` §7 Dependencies and Integration Points |

### Type Checker Output

| # | Gap | Priority | Status | Source Doc(s) | Notes |
|---|-----|----------|--------|---------------|-------|
| 47 | `SemanticIndex.References` collection missing | High | MIGRATED | type-checker.md line 562, language-server.md line 305 | Migrated to `docs/compiler/type-checker.md` §7.1 SemanticIndex |
| 48 | `SemanticIndex.FieldReferences` collection missing | High | MIGRATED | language-server.md line 305-318 | Migrated to `docs/compiler/type-checker.md` §7.1 SemanticIndex |
| 49 | `SemanticIndex.StateReferences` collection missing | Medium | MIGRATED | language-server.md line 305-318 | Migrated to `docs/compiler/type-checker.md` §7.1 SemanticIndex |
| 50 | `SemanticIndex.EventReferences` collection missing | Medium | MIGRATED | language-server.md line 305-318 | Migrated to `docs/compiler/type-checker.md` §7.1 SemanticIndex |
| 51 | `ConstraintFieldRefs.ConstraintIdentity` typed as `object` | Medium | MIGRATED | type-checker.md line 528, proof-engine.md | Migrated to `docs/compiler/type-checker.md` §7.1 SemanticIndex |
| 52 | `TypedArg.EventName` back-reference missing | Low | MIGRATED | language-server.md line 543 | Migrated to `docs/tooling/language-server.md` §7.4 Hover |
| 53 | `TypedExpression` DU incomplete for proof engine | Medium | RESOLVED | type-checker.md, proof-engine.md | Resolved in canonical docs: `docs/compiler/type-checker.md` now defines the closed `TypedExpression` DU and `docs/compiler/proof-engine.md` consumes typed expressions directly |

### Graph Analyzer Output

| # | Gap | Priority | Status | Source Doc(s) | Notes |
|---|-----|----------|--------|---------------|-------|
| 54 | `GraphState.Modifiers` vs explicit booleans | Medium | MIGRATED | graph-analyzer.md line 120 | Migrated to `docs/compiler/graph-analyzer.md` §2 Output Shape |
| 55 | `GraphEvent.IsInitial` derivation unclear | Low | MIGRATED | graph-analyzer.md line 125 | Migrated to `docs/compiler/graph-analyzer.md` §12 Open Questions / Implementation Notes |

### Proof Engine Output

| # | Gap | Priority | Status | Status | Notes |
|---|-----|----------|--------|--------|-------|
| 56 | `FaultSiteLink.Site` to `FaultSiteDescriptor` transformation | Medium | MIGRATED | proof-engine.md line 222, precept-builder.md line 467 | Migrated to `docs/compiler/proof-engine.md` §2 Output Shape |
| 57 | `ProofObligation.Site` carries `TypedExpression`, not structural ref | Medium | MIGRATED | proof-engine.md line 188 | Migrated to `docs/compiler/proof-engine.md` §2 Output Shape |
| 58 | Strategy 3 vs Strategy 4 boundary undefined | Medium | MIGRATED | proof-engine.md line 568 | Migrated to `docs/compiler/proof-engine.md` §7 Strategy 4 |

### Precept Builder Output (Descriptors)

| # | Gap | Priority | Status | Source Doc(s) | Notes |
|---|-----|----------|--------|---------------|-------|
| 59 | `ExecutionRow.RejectReason` field missing | Medium | MIGRATED | evaluator.md line 434, precept-builder.md | Migrated to `docs/runtime/precept-builder.md` §7 Component Mechanics / ExecutionRow |
| 60 | `FaultSiteDescriptor` planting mechanism unspecified | Medium | MIGRATED | precept-builder.md line 467 | Migrated to `docs/runtime/precept-builder.md` §7 Component Mechanics / Pass 6 |
| 61 | `Compilation.Tokens` field missing | Medium | MIGRATED | precept-builder.md line 116, language-server.md line 222 | Migrated to `docs/runtime/precept-builder.md` §2 Inputs and Outputs |
| 62 | `ConstraintDescriptor` bucket routing to anchor unclear | Low | MIGRATED | precept-builder.md line 201-205 | Migrated to `docs/runtime/precept-builder.md` §7 Component Mechanics / Pass 4 |

### Evaluator Output

| # | Gap | Priority | Status | Source Doc(s) | Notes |
|---|-----|----------|--------|---------------|-------|
| 63 | `Faulted(Fault)` missing from `EventOutcome` DU | Medium | MIGRATED | evaluator.md line 156, line 885 | Migrated to `docs/runtime/evaluator.md` §7.6 Constraint Evaluation |
| 64 | `EventInspection` shape mismatch | Medium | MIGRATED | language-server.md line 655-663, evaluator.md line 183-189 | Migrated to `docs/tooling/language-server.md` §7.6 Preview/Inspect |
| 65 | `EventOutcome.mutations` payload missing | Low | MIGRATED | mcp.md line 629 | Migrated to `docs/tooling/mcp.md` § `precept_fire` |
| 66 | `Unmatched` guard trace enrichment | Low | MIGRATED | mcp.md line 684 | Migrated to `docs/tooling/mcp.md` § `precept_fire` |
| 67 | `Version.Slots` array vs `ImmutableArray` | Low | MIGRATED | evaluator.md line 131 | Migrated to `docs/runtime/evaluator.md` §13 Open Questions / Implementation Notes |

### Compilation / Tooling Interface

| # | Gap | Priority | Status | Source Doc(s) | Notes |
|---|-----|----------|--------|---------------|-------|
| 68 | `precept/inspect` vs `precept/preview` naming | Low | MIGRATED | language-server.md line 610, tooling-surface.md | Migrated to `docs/tooling/language-server.md` §7.6 Preview/Inspect |
| 69 | `SlotContext` vs `SlotKind` enum naming | Low | MIGRATED | language-server.md line 354, tooling-surface.md line 512 | Migrated to `docs/tooling/language-server.md` §7.3 Catalog-Driven Completions |
| 70 | `ConstructMeta.IsOutlineNode` missing | Medium | MIGRATED | language-server.md line 702 | Migrated to `docs/tooling/language-server.md` §7.7 Document Outline |
| 71 | `ConstructMeta.LspSymbolKind` missing | Medium | MIGRATED | language-server.md line 712 | Migrated to `docs/tooling/language-server.md` §7.7 Document Outline |
| 72 | `precept_inspect` N+1 API calls | Low | MIGRATED | mcp.md line 509 | Migrated to `docs/tooling/mcp.md` § `precept_inspect` |
| 73 | `SemanticIndex.EnsuresByState` index missing | Low | MIGRATED | mcp.md line 443 | Migrated to `docs/tooling/mcp.md` § `precept_compile` |

## Summary by Status

| Status | Count |
|--------|-------|
| Shape Mismatch | 6 |
| Missing Field | 11 |
| Placeholder | 2 |
| Missing Variant | 1 |
| Interface Gap | 9 |
| Pending Decision | 17 |
| **Total** | **46** |

## High-Priority Gaps (Blocking)

These require owner decision before dependent implementation can proceed:

### #41–43 — SlotValue Subtype Shape Conflicts (Shape Mismatch)

Four slot subtypes have conflicting shapes between parser.md and type-checker.md: `TypeExpressionSlot`, `ModifierListSlot`, `AccessModeSlot`, `BecauseClauseSlot`. The parser cannot be implemented until canonical shapes are decided. **Decision needed:** Which document's shapes are authoritative? parser.md shows "resolved at parse time" (TypeMeta, ModifierKind); type-checker.md shows "spans resolved later" (SourceSpan, TokenKind).

### #45 — Expression Tree Design (Placeholder)

Expression-carrying slots (`ComputeExpressionSlot`, `GuardClauseSlot`, `EnsureClauseSlot`, `RuleExpressionSlot`, `OutcomeSlot`) currently carry only `SourceSpan`. This blocks:
- Type checker expression resolution (§7.2)
- Proof engine guard analysis (Strategy 3, Strategy 4)
- Evaluator expression evaluation (currently uses spans for error messages only)

**Decision needed:** Expression tree design — Roslyn-style per-kind nodes, S-expression uniform structure, or span + lazy parse.

### #47–50 — SemanticIndex Reference Collections (Missing Field)

`SemanticIndex` lacks `References`, `FieldReferences`, `StateReferences`, `EventReferences` collections. Language server Pass 2 semantic tokens depends on these for identifier classification. **Decision needed:** Add reference-site arrays to `SemanticIndex`, or define an alternative mechanism (walk typed declarations and pattern-match on `TypedFieldRef`, `TypedArgRef`, etc.)?

### #61 — Compilation.Tokens (Missing Field)

`Compilation` record doesn't include `Tokens` field, but LS Pass 1 lexical semantic tokens needs the token stream. **Decision needed:** Add `Tokens` to `Compilation`, or define separate access path?

## Cross-Stage Dependencies

These gaps show where one stage's output shape directly blocks another stage's implementation:

### Expression Tree → Type Checker → Proof Engine

Gap #45 (expression tree placeholder) creates a three-stage dependency chain:
1. **Parser** produces `SourceSpan` for expression slots
2. **Type Checker** cannot resolve expression types without expression trees
3. **Proof Engine** cannot analyze guards or constraints without typed expressions

Until expression tree design completes, the following are blocked:
- Type checker §7.2 (Expression Resolution Engine)
- Proof engine Strategy 3 (guard-in-path) and Strategy 4 (flow-narrowing)
- Evaluator expression evaluation beyond error message spans

### SlotValue Shape → Parser → Type Checker

Gaps #41–44 (slot subtype shape mismatches) block:
1. **Parser** cannot be implemented without canonical slot shapes
2. **Type Checker** expects specific shapes for each slot — misaligned shapes cause runtime failures

### SemanticIndex Shape → Language Server

Gaps #47–50 (missing reference collections) block:
1. **LS Pass 2** semantic token generation — cannot classify identifier tokens without reference bindings
2. **LS hover** for references — cannot show "references to this field" without reference index

### Proof-to-Runtime Bridge

Gaps #56–57 (FaultSiteLink transformation) block:
1. **Precept Builder** fault backstop pass — cannot plant `FaultSiteDescriptor` without transformation spec
2. **Evaluator** fault routing — cannot locate fault sites without row/opcode references

## Newly Found Gaps (Frank Coverage Review)

### #74 — InspectFire multiple-candidate handling

**Status:** MIGRATED  
**Source:** `evaluator.md` line ~567–568  
**The Gap:** `InspectFire` documents a multiple-candidate branch but does not settle whether inspection should return a `Fault`, mirror runtime dispatch rejection semantics, or surface ambiguity some other way.  
**Why It Matters:** Inspection and execution need aligned semantics for ambiguous dispatch or tooling will preview a different outcome than runtime fire.  
**Options:** Return `Faulted(Fault)`; return an inspection-specific ambiguity result; or specify that inspection never faults and instead reports multiple candidates explicitly.
**Migration:** `docs/runtime/evaluator.md` §7.2 Inspection Operations

### #75 — `InspectFire` skips event-level ensures

**Status:** MIGRATED  
**Source:** `evaluator.md` line ~568  
**The Gap:** `InspectFire` hardcodes `EventEnsures` to `[]`, leaving event-level constraint evaluation unspecified during inspection.  
**Why It Matters:** Tooling and MCP inspection results can under-report failing ensures compared to actual event execution.  
**Options:** Evaluate event ensures during inspection; explicitly declare inspection as transition-only; or add a partial-inspection contract that marks event ensures as omitted.
**Migration:** `docs/runtime/evaluator.md` §7.2 Inspection Operations

### #76 — Opcode executor behavior details unresolved

**Status:** MIGRATED  
**Source:** `evaluator.md` line ~707  
**The Gap:** Four executor semantics are still open: `LoadArg` null handling, whether `BranchFalse` treats `0` as falsy, whether `Return` can fall through, and whether the evaluation stack should be pooled.  
**Why It Matters:** These choices affect runtime determinism, compiled-plan compatibility, and the exact behavior the builder must target.  
**Options:** No options listed in the source; owner decision is needed on each executor rule.
**Migration:** `docs/runtime/evaluator.md` §7.3 Opcode Execution Engine

### #77 — `FieldDescriptor.AccessModes` structural shape

**Status:** MIGRATED  
**Source:** `evaluator.md` line ~805  
**The Gap:** `FieldDescriptor.AccessModes` is not settled between an `ImmutableDictionary` keyed by state and a denser `ImmutableArray`-style representation.  
**Why It Matters:** This shape affects evaluator lookup cost, builder emission shape, and alignment with the catalog-side `FieldDescriptor.AccessModes` question.  
**Options:** `ImmutableDictionary<StateId, AccessMode>` for direct lookup, or `ImmutableArray`/indexed storage for denser compiled descriptors.
**Migration:** `docs/runtime/evaluator.md` §7.5 Access Mode Enforcement

### #78 — `Version.Slots` storage representation

**Status:** MIGRATED  
**Source:** `evaluator.md` line ~131  
**The Gap:** `Version.Slots` is still undecided between immutable-array semantics and an `object?[]` copy-on-write backing store.  
**Why It Matters:** The choice affects mutation cost, snapshot semantics, and the concrete versioning contract exposed across evaluator internals.  
**Options:** `ImmutableArray<object?>` for explicit immutability, or `object?[]` with copy-on-write for lower allocation overhead.
**Migration:** `docs/runtime/evaluator.md` §13 Open Questions / Implementation Notes

### #79 — Wildcard expansion ordering

**Status:** MIGRATED  
**Source:** `graph-analyzer.md` line ~300  
**The Gap:** Wildcard transition expansion does not specify whether expansion iterates all declared states or only states already reachable in the graph.  
**Why It Matters:** Expansion order and reachability scope change graph shape, event coverage results, and the downstream runtime rows the builder will receive.  
**Options:** Expand against all declared states, or restrict expansion to currently reachable states only.
**Migration:** `docs/compiler/graph-analyzer.md` §6 Wildcard expansion

### #80 — `EventCoverageEntry` granularity

**Status:** MIGRATED  
**Source:** `graph-analyzer.md` line ~600  
**The Gap:** The graph analyzer does not settle whether `EventCoverageEntry` distinguishes guarded and unguarded transitions separately or folds them together.  
**Why It Matters:** Coverage reporting, diagnostics, and later tooling views depend on whether guard-conditioned coverage is preserved as first-class structure.  
**Options:** Track guarded versus unguarded entries separately, or keep a single coarser event-coverage entry.
**Migration:** `docs/compiler/graph-analyzer.md` §12 Open Questions / Implementation Notes

### #81 — Back-edge definition

**Status:** MIGRATED  
**Source:** `graph-analyzer.md` line ~601  
**The Gap:** The doc leaves "back-edge" undefined between a BFS-ancestor notion and classic DFS back-edge semantics.  
**Why It Matters:** Cycle detection, irreversibility reasoning, and graph diagnostics can differ materially depending on which graph-theory definition is canonical.  
**Options:** Define back-edges relative to BFS ancestry, or use standard DFS back-edge classification.
**Migration:** `docs/compiler/graph-analyzer.md` §12 Open Questions / Implementation Notes

### #82 — Initial event with null data

**Status:** MIGRATED  
**Source:** `mcp.md` line ~98  
**The Gap:** The MCP design does not define what gets passed to `Restore` when the initial event is fired with null data.  
**Why It Matters:** Tool callers need a deterministic initialization contract, and runtime/MCP parity breaks if null bootstrap data is interpreted differently.  
**Options:** Pass `null`, pass an empty slot/value container, or require callers to provide initial data explicitly.
**Migration:** `docs/tooling/mcp.md` §5 Internal Flow

### #83 — `ITypedConstantValidator` registration API

**Status:** MIGRATED  
**Source:** `literal-system.md` §Open Questions #1  
**The Gap:** The literal system leaves the exact registration API surface for `ITypedConstantValidator` unsettled.  
**Why It Matters:** Consumers cannot implement extension registration or dependency injection wiring until the contract is nailed down.  
**Options:** No options listed in the source; API shape remains open.
**Migration:** `docs/compiler/literal-system.md` §Open Questions / Implementation Notes

### #84 — Interpolated typed-constant validation timing

**Status:** MIGRATED  
**Source:** `literal-system.md` §Open Questions #2  
**The Gap:** The doc does not decide whether interpolated typed constants validate entirely at compile time or defer some validation to runtime.  
**Why It Matters:** This changes diagnostics timing, what the type checker guarantees, and whether runtime fault paths must remain for typed constants.  
**Options:** Compile-time validation, runtime validation, or a split model where structural checks compile-time and value checks runtime.
**Migration:** `docs/compiler/literal-system.md` §Open Questions / Implementation Notes

### #85 — Structural validation fallback for `'...'`

**Status:** MIGRATED  
**Source:** `literal-system.md` §Open Questions #3  
**The Gap:** Structural validation fallback semantics for `'...'` are not defined.  
**Why It Matters:** Literal-system consumers need a deterministic rule for how incomplete or deferred structural validation behaves when a fully typed validator is unavailable.  
**Options:** No options listed in the source; fallback behavior remains open.
**Migration:** `docs/compiler/literal-system.md` §Open Questions / Implementation Notes
