# Cross-Cutting Decisions Register

> Decisions that affect multiple pipeline stages or canonical docs simultaneously.
> These cannot be made in isolation — each decision ripples across the stages listed.
> Resolve these before stage-specific gaps can be closed.

---

## Priority 1 — Must Resolve First (Blocking Multiple Stages)

### 1. Expression Tree Design

**Affects:** Parser, Type Checker, Proof Engine, Evaluator, Precept Builder
**Gap register refs:** #14, #45, #53
**The decision:** What is the structure of expression trees produced by the parser and consumed by all downstream stages?

**Why it's cross-cutting:**
- **Parser** (blocked) — expression-carrying slots (`ComputeExpressionSlot`, `GuardClauseSlot`, `EnsureClauseSlot`, `RuleExpressionSlot`, `OutcomeSlot`) currently carry only `SourceSpan`. Parser needs to know the expression node shape to produce it.
- **Type Checker** (blocked) — §7.2 Expression Resolution Engine is designed but cannot be exercised until parser produces expression trees.
- **Proof Engine** (blocked) — Strategy 3 (guard-in-path) and Strategy 4 (flow-narrowing) require parsing guard expressions into constraint form.
- **Evaluator** — needs to walk typed expressions (currently uses ExecutionPlan opcodes, which are compiled FROM expression trees).
- **Precept Builder** — compiles `TypedExpression` trees into flat opcode arrays.

**Options / known design space:**
1. **Roslyn-style** — Per-expression-kind node types with full fidelity
2. **S-expression** — Uniform `(op, args...)` structure
3. **Span + lazy parse** — Keep span, parse on demand

**Resolution path:** Design decision required before any expression-related implementation can proceed. This is the single most blocking cross-cutting decision.

---

### 2. SlotValue Subtype Shapes

**Affects:** Parser, Type Checker
**Gap register refs:** #6, #7, #40, #41, #42, #43, #44
**The decision:** What are the canonical shapes for `SlotValue` subtypes, and which document is authoritative?

**Why it's cross-cutting:**
- **Parser** produces `SlotValue` subtypes with specific shapes
- **Type Checker** consumes them with expected shapes

Four slot subtypes have conflicting shapes between parser.md and type-checker.md:

| Slot | parser.md says | type-checker.md says |
|------|----------------|----------------------|
| `TypeExpressionSlot` | `TypeMeta` | `SourceSpan` |
| `ModifierListSlot` | `ImmutableArray<ModifierKind>` | `ImmutableArray<TokenKind>` |
| `AccessModeSlot` | `SourceSpan` | `TokenKind` |
| `BecauseClauseSlot` | `string` | `SourceSpan` |

Additionally, parser.md lists 17 subtypes while catalog-system.md shows 15 `ConstructSlotKind` members. `RuleExpressionSlot` and `InitialMarkerSlot` are in parser.md but not in the catalog.

**Options / known design space:**
1. **Parser resolves at parse time** — `TypeMeta`, `ModifierKind` carried in slots (parser.md shapes)
2. **Parser captures spans, type checker resolves** — `SourceSpan`, `TokenKind` in slots (type-checker.md shapes)
3. **Hybrid** — Some slots resolved early, expression slots deferred

**Resolution path:** Owner must declare which doc is authoritative. The mismatch prevents both parser and type checker implementation.

---

### 3. SemanticIndex Reference-Tracking Collections

**Affects:** Type Checker, Language Server, Tooling Surface
**Gap register refs:** #16, #47, #48, #49, #50
**The decision:** Should `SemanticIndex` carry reference-site tracking arrays (`References`, `FieldReferences`, `StateReferences`, `EventReferences`)?

**Why it's cross-cutting:**
- **Type Checker** — Must record span and binding of every identifier use if these collections are added
- **Language Server** — Pass 2 semantic tokens needs reference-site arrays to classify identifier tokens
- **Tooling Surface** — Also references these collections for semantic token Pass 2

**Options / known design space:**
1. **Add reference arrays to SemanticIndex** — Type checker records all reference sites
2. **LS/tooling reconstructs references** — Walk typed declarations, pattern-match on `TypedFieldRef`, `TypedArgRef`, etc.
3. **Hybrid approach** — Core references in SemanticIndex, derived references reconstructed

**Resolution path:** The type-checker.md §7.1 canonical `SemanticIndex` shape has no reference collections, but language-server.md explicitly expects them.

---

### 4. Compilation.Tokens Field

**Affects:** Precept Builder, Language Server, Tooling Surface
**Gap register refs:** #61
**The decision:** Should `Compilation` carry a `Tokens` field containing the lexer's token stream?

**Why it's cross-cutting:**
- **Language Server** — Pass 1 lexical semantic tokens needs the token stream
- **Tooling Surface** — References `Compilation.Tokens` for lexical tokens
- **Precept Builder** — The `Compilation` record in precept-builder.md doesn't include `Tokens`

**Options / known design space:**
1. **Add `ImmutableArray<Token> Tokens` to `Compilation`** — Direct access
2. **Separate token stream access path** — LS re-lexes or retrieves from lexer cache
3. **Compilation variant** — `FullCompilation` with tokens for tooling, `SlimCompilation` for runtime

**Resolution path:** The precept-builder.md `Compilation` record shape is the canonical definition; it needs explicit decision on whether to add `Tokens`.

---

### 5. FieldModifierMeta.ProofDischarges

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

## Priority 2 — Significant Cross-Stage Impact

### 6. FaultSiteLink to FaultSiteDescriptor Transformation

**Affects:** Proof Engine, Precept Builder, Evaluator
**Gap register refs:** #11, #23, #56, #57, #60
**The decision:** How does the proof engine's `FaultSiteLink` (compile-time span) transform into the Precept Builder's `FaultSiteDescriptor` (runtime location)?

**Why it's cross-cutting:**
- **Proof Engine** — Produces `FaultSiteLink.Site` as `SourceSpan`
- **Precept Builder** — Needs to plant `FaultSiteDescriptor` at specific `ExecutionRow` or opcode offset
- **Evaluator** — Must locate fault sites for defense-in-depth routing

**Current gap:** Neither `ExecutionRow` nor `ConstraintDescriptor` carries a fault site field. `Precept.FaultBackstops` is a flat array with no mechanism to associate backstops with specific opcodes.

**Options / known design space:**
1. **Opcode annotation** — Each opcode carries optional fault site reference
2. **Span-to-opcode map** — Precept Builder builds span→opcode index, evaluator looks up
3. **Inline fault checks** — Compiler injects guard opcodes at fault sites

**Resolution path:** The transformation mechanism is unspecified across all three stages.

---

### 7. ConstraintMeta DU Subtype Count

**Affects:** Type Checker, Precept Builder
**Gap register refs:** #22, #62
**The decision:** How many subtypes does the `ConstraintMeta` discriminated union have?

**Why it's cross-cutting:**
- **Precept Builder** — Uses 5 concrete subtypes for constraint bucket routing (`Invariant`, `StateResident`, `StateEntry`, `StateExit`, `EventPrecondition`)
- **Catalog-system.md** — Shows only 4 subtypes in the DU hierarchy

**Current discrepancy:** Precept-builder.md's bucket dispatch switch uses 5 subtypes. Are `StateEntry` and `StateExit` separate top-level subtypes, or subtypes of `StateAnchored`?

**Resolution path:** Catalog-system.md must specify the exact DU hierarchy.

---

### 8. EventInspection Shape

**Affects:** Evaluator, Language Server, MCP
**Gap register refs:** #33, #64
**The decision:** What is the canonical shape of `EventInspection`?

**Why it's cross-cutting:**
- **Evaluator** (evaluator.md) — Uses `EventEnsures`, `ConstraintResult`, `RowInspection`
- **Language Server** (language-server.md) — Uses `BeforeFields`, `AfterFields`, `TransitionRowInspection`

These are different shapes for the same concept. Downstream consumers cannot implement until the shape is resolved.

**Resolution path:** Evaluator.md should be authoritative for runtime shapes; other docs should reference it.

---

### 9. ConstraintFieldRefs.ConstraintIdentity Type

**Affects:** Type Checker, Proof Engine
**Gap register refs:** #15, #51
**The decision:** Should `ConstraintFieldRefs.ConstraintIdentity` be typed `object` or use the proof-engine.md's `ConstraintIdentity` DU?

**Why it's cross-cutting:**
- **Type Checker** — Produces `ConstraintFieldRefs` with identity field typed as `object`
- **Proof Engine** — Defines a proper `ConstraintIdentity` DU with `RuleIdentity` and `EnsureIdentity` subtypes

**Resolution path:** Align on the proof-engine.md DU shape for type safety.

---

### 10. GraphState Modifier Representation

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

### 11. ExecutionRow.RejectReason Field

**Affects:** Type Checker, Precept Builder, Evaluator
**Gap register refs:** #20, #59
**The decision:** Where is the `because` clause from a `reject` transition row stored?

**Why it's cross-cutting:**
- **Type Checker** — Produces `TypedTransitionRow` (needs field for reject reason)
- **Precept Builder** — Transforms to `ExecutionRow` (needs field)
- **Evaluator** — Returns `Rejected(reason)` outcome (reads field)

**Current gap:** Evaluator.md references `winningRow.RejectReason` but no such field is defined on `ExecutionRow`.

**Resolution path:** Add `string? RejectReason` to both `TypedTransitionRow` and `ExecutionRow`.

---

### 12. Faulted(Fault) as EventOutcome Variant

**Affects:** Evaluator, MCP
**Gap register refs:** #21, #63
**The decision:** Should `Faulted(Fault)` be added as an `EventOutcome` DU variant?

**Why it's cross-cutting:**
- **Evaluator** — `Fail()` returns a `Fault`, but `Fault` is not currently a subtype of `EventOutcome`
- **MCP** — `precept_fire` needs to serialize fault outcomes

**Resolution path:** Add `Faulted(Fault)` variant to the `EventOutcome` DU.

---

## Priority 3 — Minor Cross-Stage Coordination

### 13. FaultCode.AmbiguousDispatch

**Affects:** Evaluator, Faults Catalog, Diagnostics Catalog
**Gap register refs:** #4
**The decision:** Add `FaultCode.AmbiguousDispatch` with corresponding `DiagnosticCode` and `[StaticallyPreventable]` attribute.

**Current gap:** Evaluator.md references `Fail(FaultCode.AmbiguousDispatch)` but the fault code doesn't exist.

---

### 14. SlotContext vs SlotKind Enum Naming

**Affects:** Language Server, Tooling Surface
**Gap register refs:** #38, #69
**The decision:** Is `SlotContext` (language-server.md) the same as `SlotKind` (tooling-surface.md)? Which is canonical?

---

### 15. precept/inspect vs precept/preview Naming

**Affects:** Language Server, Tooling Surface
**Gap register refs:** #32, #68
**The decision:** What is the canonical name for the custom LSP preview method?

---

### 16. TypeMeta.IsUserFacing for Completions

**Affects:** Language Server, Types Catalog
**Gap register refs:** #30
**The decision:** Add `IsUserFacing` property to `TypeMeta` for filtering completion suggestions.

---

### 17. TypedArg.EventName Back-Reference

**Affects:** Type Checker, Language Server
**Gap register refs:** #31, #52
**The decision:** Should `TypedArg` carry an `EventName` back-reference for hover lookup?

---

### 18. ConstructMeta Outline Properties

**Affects:** Language Server, Constructs Catalog
**Gap register refs:** #34, #70, #71
**The decision:** Add `IsOutlineNode` and `LspSymbolKind` properties to `ConstructMeta` for catalog-driven document outline.

---

### 19. TokenMeta.HoverDescription Strategy

**Affects:** Language Server, Tooling Surface, Tokens Catalog
**Gap register refs:** #5, #35
**The decision:** Comprehensive strategy for documentation strings in `TokenMeta` beyond partial capture.

---

### 20. Diagnostic Related Locations

**Affects:** Diagnostic System, Language Server
**Gap register refs:** #39
**The decision:** Add `AdditionalLocations` to `Diagnostic` for multi-span diagnostics (e.g., "field declared at line X, constraint at line Y").

---

## Coverage Assessment

### Gaps NOT in Either Register (Newly Found)

The following gaps were found during this review that are not captured in either the catalog-gap-register.md or pipeline-output-gap-register.md:

| Doc | Location | Description |
|-----|----------|-------------|
| literal-system.md | §Open Questions #1 | `ITypedConstantValidator` registration API — exact API surface not settled |
| literal-system.md | §Open Questions #2 | Interpolated typed constant validation timing — compile-time vs runtime |
| literal-system.md | §Open Questions #3 | Structural validation fallback definition for `'...'` |
| tooling-surface.md | line ~144 | Grammar input catalog list may be incomplete — do `Modifiers.All` and `Actions.All` feed grammar via their `Token` references? |
| evaluator.md | line ~567–568 | InspectFire multiple candidates handling — should inspection return `Fault`? |
| evaluator.md | line ~568 | `EventEnsures` hardcoded to `[]` in InspectFire — event-level constraints not evaluated |
| evaluator.md | line ~707 | Four opcode executor implementation questions: LoadArg null handling, BranchFalse 0 as falsy, Return fallthrough, Stack pooling |
| evaluator.md | line ~805 | `FieldDescriptor.AccessModes` shape — `ImmutableDictionary` vs `ImmutableArray` |
| evaluator.md | line ~131 | `Version.Slots` — `ImmutableArray` vs `object?[]` with copy-on-write |
| graph-analyzer.md | line ~300 | Wildcard expansion ordering — all declared states vs reachable states only |
| graph-analyzer.md | line ~600 | EventCoverageEntry granularity — guarded vs unguarded transitions |
| graph-analyzer.md | line ~601 | Back-edge definition — BFS ancestor vs DFS back-edges |
| mcp.md | line ~98 | Initial event with null data — what is passed to `Restore`? |

### Gaps in Registers That Are Isolated (Single-Doc Only)

The following registered gaps are truly isolated to one stage and don't need cross-cutting treatment:

| # | Gap | Why Isolated |
|---|-----|--------------|
| #8 | `DisambiguationEntry.Offset` | Parser-only concern |
| #10 | Event "optional" modifier | Language surface change, not pipeline gap |
| #12 | `TryLiteralProof` requirement coverage | Proof engine strategy scope |
| #19 | `Compilation.Tokens` field | *(Actually cross-cutting — see Priority 1 #4)* |
| #24 | `ModifierMeta.ModifierCategory` | Already resolved in source |
| #25 | `FirePipeline` catalog | MCP output design only |
| #26 | `SemanticIndex.EnsuresByState` | MCP convenience index only |
| #27 | `precept_inspect` composite view exemption | MCP API design only |
| #28 | `EventOutcome.mutations` payload | MCP-to-evaluator interface |
| #29 | Unmatched guard trace enrichment | MCP diagnostic enrichment |
| #36 | Grammar generator implementation path | Tooling implementation only |
| #37 | Complex TextMate pattern representation | Tooling-surface only |
| #46 | `ConstructManifest` in graph analyzer | Documentation clarification |
| #55 | `GraphEvent.IsInitial` derivation | Graph analyzer internal |
| #65 | `EventOutcome.mutations` payload | MCP interface only |
| #66 | `Unmatched` guard trace enrichment | MCP diagnostic enrichment |
| #67 | `Version.Slots` array type | Evaluator internal |
| #72 | `precept_inspect` N+1 API calls | MCP API design only |
| #73 | `SemanticIndex.EnsuresByState` index | MCP convenience only |

### Coverage Verdict

**Coverage confidence: ~90%**

The two gap registers together provide strong coverage of the significant gaps in the 11 canonical docs. The registers successfully capture:

- All major type shape mismatches
- All missing fields blocking downstream consumers
- All interface gaps between stages
- Most placeholder types needing design

**Gaps in coverage:**

1. **Literal system implementation questions** — The literal-system.md open questions (#1–5) are not in either register
2. **Evaluator implementation details** — Several evaluator-internal questions around opcode execution semantics
3. **Graph analyzer implementation questions** — Back-edge definition, wildcard expansion ordering
4. **MCP edge cases** — Initial event null data handling

**Recommendation:** Add the 13 newly-found gaps to the appropriate register, then mark this review complete.

The cross-cutting decisions identified here (Priority 1 especially) represent the critical path for implementation. **Expression tree design** is the single most blocking decision — it affects 5 pipeline stages and cannot be worked around.
