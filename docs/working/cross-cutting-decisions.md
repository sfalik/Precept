# Cross-Cutting Decisions — Execution Driver

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

**Status:** 🔴 Pending Shane decision

**Affects:** Evaluator, Operations Catalog, Functions Catalog, Actions Catalog
**Gap register refs:** Audit item C
**The decision:** What catalog-defined execution delegate contract should drive operation, function, and action dispatch at runtime?

**Why it's cross-cutting:**
- **Evaluator** — needs a canonical invocation contract for expression execution and action application.
- **Operations Catalog** — must declare how unary/binary operator implementations are surfaced to the evaluator.
- **Functions Catalog** — must declare callable signatures, argument binding, and execution delegates.
- **Actions Catalog** — must declare how action payloads are executed without evaluator-owned per-action switches.

**Options / known design space:**
1. **Per-catalog delegate properties** — each catalog family exposes its own strongly typed execution delegate shape.
2. **Shared invocation descriptor** — catalogs expose a common execution-contract DU or descriptor that the evaluator interprets generically.
3. **Evaluator adapter layer** — catalogs stay descriptive only and the evaluator owns the dispatch mapping (least aligned with the catalog-first architecture).

**Resolution path:** Lock the dispatch contract before evaluator execution work proceeds further; this is the highest-priority uncaptured catalog-system decision from the audit.

---

## Wave 1 — Shape-Defining (Shane Required for Most)

> Once Wave 0 is locked, these set the type shapes everything else conforms to. Most require Shane decisions; CC#9 follows mechanically from CC#7.

---

### CC#3. SemanticIndex Reference-Tracking Collections

**Status:** 🔴 Pending Shane decision

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

### CC#4. Compilation.Tokens Field

**Status:** 🔴 Pending Shane decision

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

### CC#6. FaultSiteLink to FaultSiteDescriptor Transformation

**Status:** 🔴 Pending Shane decision

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

### CC#7. ConstraintMeta DU Subtype Count

**Status:** 🔴 Pending Shane decision

**Affects:** Type Checker, Precept Builder
**Gap register refs:** #22, #62
**The decision:** How many subtypes does the `ConstraintMeta` discriminated union have?

**Why it's cross-cutting:**
- **Precept Builder** — Uses 5 concrete subtypes for constraint bucket routing (`Invariant`, `StateResident`, `StateEntry`, `StateExit`, `EventPrecondition`)
- **Catalog-system.md** — Shows only 4 subtypes in the DU hierarchy

**Current discrepancy:** Precept-builder.md's bucket dispatch switch uses 5 subtypes. Are `StateEntry` and `StateExit` separate top-level subtypes, or subtypes of `StateAnchored`?

**Resolution path:** Catalog-system.md must specify the exact DU hierarchy.

---

### CC#9. ConstraintFieldRefs.ConstraintIdentity Type

**Status:** 🔵 Pending team resolution (follows from CC#7)

**Affects:** Type Checker, Proof Engine
**Gap register refs:** #15, #51
**The decision:** Should `ConstraintFieldRefs.ConstraintIdentity` be typed `object` or use the proof-engine.md's `ConstraintIdentity` DU?

**Why it's cross-cutting:**
- **Type Checker** — Produces `ConstraintFieldRefs` with identity field typed as `object`
- **Proof Engine** — Defines a proper `ConstraintIdentity` DU with `RuleIdentity` and `EnsureIdentity` subtypes

**Resolution path:** Align on the proof-engine.md DU shape for type safety.

---

## Wave 2 — Outcome & Evaluator (Shane Required for Initial Direction)

> These form a cluster around evaluator output shapes. Lock CC#8 first (defines the outcome DU shape), then CC#12, CC#23, CC#24 follow. CC#11 is independent but simple.

---

### CC#8. EventInspection Shape

**Status:** 🔴 Pending Shane decision

**Affects:** Evaluator, Language Server, MCP
**Gap register refs:** #33, #64
**The decision:** What is the canonical shape of `EventInspection`?

**Why it's cross-cutting:**
- **Evaluator** (evaluator.md) — Uses `EventEnsures`, `ConstraintResult`, `RowInspection`
- **Language Server** (language-server.md) — Uses `BeforeFields`, `AfterFields`, `TransitionRowInspection`

These are different shapes for the same concept. Downstream consumers cannot implement until the shape is resolved.

**Resolution path:** Evaluator.md should be authoritative for runtime shapes; other docs should reference it.

---

### CC#11. ExecutionRow.RejectReason Field

**Status:** 🔵 Pending team resolution (obvious add — no design ambiguity)

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

### CC#12. Faulted(Fault) as EventOutcome Variant

**Status:** 🔵 Pending team resolution (follows from CC#8)

**Affects:** Evaluator, MCP
**Gap register refs:** #21, #63
**The decision:** Should `Faulted(Fault)` be added as an `EventOutcome` DU variant?

**Why it's cross-cutting:**
- **Evaluator** — `Fail()` returns a `Fault`, but `Fault` is not currently a subtype of `EventOutcome`
- **MCP** — `precept_fire` needs to serialize fault outcomes

**Resolution path:** Add `Faulted(Fault)` variant to the `EventOutcome` DU.

---

### CC#23. `EventOutcome.mutations` Payload

**Status:** 🔴 Pending Shane decision

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

**Resolution path:** Lock the ownership and shape of mutation diffs before evolving the `EventOutcome` discriminated union.

---

### CC#24. Unmatched Guard Trace Enrichment

**Status:** 🔴 Pending Shane decision

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
