---
status: Locked 2026-07-06
phase-target: Phase 1 (compiler-code — the ConstantFold rewire lands as a proof-engine refactor); the two-implementation conformance differential aligns with Phase 7's conformance program; runtime adoption remains a runtime-initiative obligation. /plan finalizes placement.
comparable-systems-research-status: strong — grounded in `research/architecture/compiler/static-vs-runtime-expression-evaluation-survey.md` (Primary-sourced Stage-1 survey), with sibling surveys cited.
sources-consulted:
  - research/architecture/compiler/static-vs-runtime-expression-evaluation-survey.md: the load-bearing survey — five-point spectrum, the drift-pole evidence (GCC/LH), Rust/Zig/Dhall shared-core, C++ equivalence-by-mandate, CompCert/WASM two-implementations-one-semantics; Spec-first check (Step 1b)
  - research/architecture/compiler/interval-vs-value-evaluation-prior-art-2026-06-05.md: sibling — value-fold is a point-1/2 system, interval layer is point-3 (sound-by-approximation); CUE value-as-singleton-bound
  - research/architecture/compiler/bounds-only-constraint-enforcement-2026-06-05.md: sibling — the value-fold catches 7/8 broken non-interval defaults bounds-only misses (Q-B necessity)
  - src/Precept/Pipeline/ProofEngine.Analysis.cs: FoldValue + EvaluateBinaryOp (the actual proof fold arithmetic) — feasibility check
  - src/Precept/Language/Operations.cs: the OperationMeta catalog — carries IntervalTransfer + ProofRequirements, no value-executor delegate today
  - src/Precept/Language/Operation.cs: OperationMeta DU (Unary/Binary), IntervalTransfer fields
  - src/Precept/Runtime/Evaluator.cs: the stub (all operation bodies throw); CATALOG-DRIVEN IMPLEMENTATION GUIDE ("Do NOT claim catalog-driven execution dispatch until delegate fields exist")
  - docs/compiler/proof-engine.md: :110 decimal mandate; §7.7 constant-folder zero-denominator guard; § Initial-State Satisfiability shared-ConstantFold/BuildDefaultEnvironment
  - docs/runtime/evaluator.md: Status=Stub; §7.6 Constraint Evaluation; PreceptValue 32-byte struct (decimal payload); :281 BinaryOp.Executor : Func<PreceptValue,PreceptValue,PreceptValue>
  - docs/runtime/precept-builder.md: §"No Evaluation"; :536 builder embeds TypeRuntimeMeta.BinaryExecutors/UnaryExecutors executor delegate in opcodes
  - docs/compiler-and-runtime-design.md: §2 catalog-driven grounding (CEL/OPA/CUE comparative standard)
  - docs/philosophy.md: determinism, prevention, static completeness (§ core principles)
  - docs/language/precept-language-spec.md: §0.1 the eleven design principles (P3 determinism, P10 totality, P11 static completeness)
  - docs/Working/compiler-readiness-plan-2026-06-16.md: OD-1 (Slice 0.3 record; Phase-7 confirm); phase table
---

# Shared Expression-Evaluation Core (fold ↔ runtime)

## Goal

When done, the compile-time proof fold and the (future) runtime evaluator compute every arithmetic/comparison/boolean/string operation through **one** catalog-attached evaluation core — established now as compiler-scope work — so that a definition's proved-against value and its runtime-computed value are the same by construction, demonstrated by the proof fold delegating to that core (no inline arithmetic left in `ProofEngine.Analysis.cs`) with the sample-corpus diagnostics unchanged.

## Scope

- **In scope**: Extracting the proof fold's value arithmetic (`EvaluateBinaryOp` and the inline unary arithmetic in `FoldValue`) into a single pure evaluation core reachable from the `Operations` catalog metadata; rewiring `FoldValue` to consume it; a characterization test matrix pinning that core's outputs; the number-model pin that binds fold and runtime to one `decimal`-based value representation; amending the readiness plan's OD-1 from a recorded runtime-build note to compiler-scope build work.
- **Out of scope**: Building the runtime evaluator (`Fire`/`Update`/`Inspect`/`Restore` stay stubbed); the executable-model design (`PreceptValue` layout, opcode dispatch); the singleton-interval reframing of the value-fold (a sibling-survey escape hatch, deliberately not pursued here); any change to the interval/octagon proof layer (it is sound-by-approximation and carries no drift obligation).
- **Deferred to future**: The runtime's *adoption* of the shared core (a runtime-initiative obligation — the runtime, when built, must dispatch through the same core, not a second implementation); the two-implementation conformance differential (only becomes runnable once a second executor exists); overflow-boundary enforcement (D1 overflow is parked per the readiness plan).

## Philosophy Alignment

Row labels follow the design skill's eleven-principle matrix; each cell cites the corresponding `precept-language-spec.md §0.1` principle. (The skill's paraphrase labels and the spec's §0.1 wording differ slightly in a few rows — the spec is the authority; the mapping is noted inline where it matters.)

| Principle | Affected? | How served (cite) | Tension | Tradeoff |
|---|---|---|---|---|
| 1. Prevention not detection | Y | Prevention is only as sound as the fold's prediction; one core makes the fold's proved-against value equal the runtime's value, closing the gap where a "prevented" configuration could still fault (`philosophy.md § What makes it different`; survey Conclusions) | The fold must use the runtime's exact number model — no host-native shortcut | Forgoes any faster fold arithmetic; same trade GCC makes emulating target FP (survey §5a) |
| 2. One file, complete rules | N | The core evaluates expressions already wholly inside the `.precept` file; no new cross-file surface (`spec §0.1 P2`) | N/A | N/A |
| 3. Determinism | Y | `spec §0.1 P3` "same definition, same data, same outcome": one evaluation function makes fold≡runtime a reflexive identity, not a hoped-for coincidence (survey §1 Rust one-VM) | N/A | N/A |
| 4. Full inspectability | Y | The fold's proved values and the runtime's computed values are now the *same* values, so hover/proof-witness and runtime traces cannot disagree (`spec §0.1 P4`) | N/A | N/A |
| 5. Keyword-anchored readability | N | No syntax, keyword, or layout change (`spec §0.1 P5`) | N/A | N/A |
| 6. Governance not validation | N | Maps to `spec §0.1 P6` (explicit domain meaning); the operation set and its typed result rules are unchanged — only the executor's implementation location moves | N/A | N/A |
| 7. Compile-time totality | Y | `spec §0.1 P7`/P10 totality: the fold's zero-denominator conservative guard (proof-engine.md §7.7) is preserved inside the shared core — divide/modulo-by-zero still folds to unknown, never a proved value | The core must keep the "refuse to evaluate through a fault" posture, not adopt a runtime "throw" posture | The core is the *proof* posture (unknown-on-unsafe), and the runtime adapts to it — not vice-versa |
| 8. Honesty about approximation | Y | `spec §0.1 P8`: pinning `decimal` (not `double`) as the one representation keeps the exact lane exact and refuses the C++ FP carve-out (survey §2) | N/A | `Number`/IEEE-754 operations remain the declared-approximate lane; the core does not fold them to a claimed-exact value |
| 9. Mandatory rationale (`because`) | N | No constraint surface change; `because` semantics untouched (`spec §0.1 P9`) | N/A | N/A |
| 10. Static semantic checking | Y | Maps to `spec §0.1 P7`/P10: type checking and obligation stamping are unchanged; the core is invoked only after the type checker has resolved each `OperationKind` | N/A | N/A |
| 11. Static completeness | Y | `spec §0.1 P11` "no error ⇒ no fault" is sound *only if* fold-predicted value = runtime-computed value; one core makes that structural (survey Implications #2; `fault-system.md:280` "a fold-vs-runtime mismatch is a proof-engine gap") | N/A | N/A |

Rows with an accepted tradeoff (Principles 1, 7, 8): the shared core forgoes any phase-specialized fast arithmetic (Principle 1/7) and keeps `Number`/IEEE-754 in the declared-approximate lane rather than folding it to a claimed-exact result (Principle 8). Each is justified because the alternative — a second, faster, or laxer arithmetic implementation — *is* the empirically-unsound drift pole (survey §5): GCC's target-FP-emulation rule and Liquid Haskell's SMT-int-vs-machine-int gap both reduce to "static and runtime used different number models." Paying the shared-core cost is the mainstream, shippable posture (C++ mandates constexpr≡runtime except FP; Precept removes even the FP exception by keeping `double` out of the fold).

**Companion commitments.** *Stateless-first-class*: the core is a pure function over values with no lifecycle/state dependency, so it serves stateless and stateful precepts identically. *Domain-expert-primary-author*: this is an internal implementation-unification with no authoring-surface change — the domain expert writes the same `.precept` and sees the same diagnostics; the commitment is respected by being invisible to them.

## Language Design Grounding

Omitted — this design introduces no token, keyword, construct, modifier, type, operator, accessor, or expression form. The operation set (`Operations` catalog) and its typed evaluation semantics are unchanged; only the *implementation* of the existing operations' value computation is unified across two pipeline phases. Language-surface grounding is therefore not applicable.

## Audience and Teachability

Omitted — no language surface changes, so there is nothing new for a domain expert to learn or misuse. The `.precept` an author writes, the diagnostics they see, and the operator vocabulary are all identical before and after. Per the skill, this section is explicitly N/A for a non-language-surface design.

## Semantic Rules

This design changes an *implementation*, not the *semantics* — so it introduces **no new reduction rule and no new typing rule**. The evaluation and typing relations are exactly as they are today. What changes is that the single denotation those relations already presuppose is now realized by one function instead of two.

**The one-denotation statement.** Let `⟦·⟧ : Expr × Env → Value` be the meaning of a Precept expression. The type checker resolves each binary/unary node to an `OperationKind` (`bin.ResolvedOp`), and `⟦·⟧` is defined by the catalog's operation semantics. Today `⟦·⟧` is realized *twice*: by `EvaluateBinaryOp` for the compile-time fold, and (in the executable-model design) by the runtime `BinaryOp.Executor` (`evaluator.md:281`). This design collapses both realizations to one function `E` derived from `OperationMeta`:

```
fold(e)     = E⟦e⟧         (proof engine, over the default environment)
runtime(e)  = E⟦e⟧         (evaluator, over entity data)   [when the runtime is built]
```

**Soundness-preservation claim.** The principles most at risk from expression evaluation are `spec §0.1` P3 (determinism), P10 (totality), P11 (static completeness).

- **P11 (static completeness)** holds — and is *strengthened*. Its soundness condition is `∀ e. fold(e) = runtime(e)` (survey Background: "sound only if the compiler's prediction of an expression's value matches what the runtime computes"). With a single `E`, this becomes `E⟦e⟧ = E⟦e⟧` — reflexivity — true by construction rather than by test or hope. Today, with two realizations, the condition is a standing obligation that `fault-system.md:280` classifies as a proof-engine gap if violated; the shared core discharges that obligation structurally.
- **P3 (determinism)** holds — and is strengthened for the same reason: two evaluators are two opportunities to diverge on the same `(definition, data)`; one evaluator has none.
- **P10 (totality)** holds because the shared core preserves the fold's conservative posture verbatim: divide/modulo by a zero denominator returns the unknown sentinel, never a proved value (`proof-engine.md §7.7`). The core does not introduce any expression form whose evaluation is newly defined or newly undefined; the proof obligations the type checker stamps (`ProofRequirement` records) are unchanged, and the proof engine still discharges divisor-safety and bounds before any value is folded.

**Proof obligations.** No new `ProofRequirement` catalog entry. The obligations remain exactly those `Operations.cs` already declares per `OperationKind` (e.g. `NumericProofRequirement(... NotEquals, 0m ...)` on every division). The shared core is invoked *after* those obligations are collected and only computes values for already-type-resolved operations.

## Architecture Grounding

### Precept-internal placement

**Layer placement.** The shared core belongs in **catalog metadata** — reachable from `OperationMeta` (`src/Precept/Language/Operations.cs` / `Operation.cs`), consumed by both the proof engine and the future runtime. It does **not** belong in pipeline code. Today it is mis-placed: `EvaluateBinaryOp` lives *inside* `src/Precept/Pipeline/ProofEngine.Analysis.cs` (:351–405), a pipeline stage. That is precisely the placement the catalog-first rule forbids ("Never maintain parallel keyword lists… derive, don't duplicate" — CLAUDE.md), and it is the placement that makes a second implementation (the runtime's) able to drift. `Operations.cs` already declares, per `OperationKind`, the operator, operand types, result type, `IntervalTransfer` function, and `ProofRequirement`s — the value executor is the one missing metadata facet, and it belongs alongside the interval transfer function it mirrors. Moving the arithmetic from `ProofEngine.Analysis.cs` into a catalog-attached executor is the catalog-first correction, not a new placement invention.

**Feasibility (survey Open Question 1, resolved against code).** The survey framed "two arithmetic implementations exist." Reading the actual code sharpens this favorably:

- The proof fold's `EvaluateBinaryOp` (`ProofEngine.Analysis.cs:351-405`) is a **pure static function** over `decimal`/`string`/`bool`, dispatched on the coarse `OperatorKind`, taking `object? left, object? right → object?`. It has **zero** dependency on entity state, opcode dispatch, or the runtime `Evaluator`. Confirmed independent of the stubbed `Fire`/`Update`.
- The catalog value-executors the survey names (`BinaryExecutors`/`UnaryExecutors`) **do not exist in code today**: `grep -rn "BinaryExecutor\|UnaryExecutor" src/` returns nothing. They are a *documented runtime design* (`precept-builder.md:536` "fetches the executor delegate from … `TypeRuntimeMeta.BinaryExecutors[localIndex]`"; `evaluator.md:281` `BinaryOp.Executor : Func<PreceptValue,PreceptValue,PreceptValue>`) for the not-yet-built runtime. `Evaluator.cs` explicitly warns: "Do NOT claim catalog-driven execution dispatch until delegate fields exist in catalog metadata."

So **today there is exactly ONE value-arithmetic implementation** (the fold's, pure and runtime-independent). The verdict is **cheap-now**: no reconciliation of two existing implementations is needed — the refactor lifts the single existing pure kernel into the catalog seam and points the fold at it. Because the second implementation has not yet been written, establishing the shared core *now* is the moment of minimum cost and zero drift risk — it preempts the hazard rather than repairing it. The one real refactor cost is a value-domain bridge: the fold operates on boxed `object?`, the runtime design operates on the 32-byte `PreceptValue` struct (`evaluator.md:230/281`). The extracted kernel must be defined over a domain both phases adapt to (one arithmetic kernel with two thin value adapters, or an executor generic over a value abstraction) — scoped in the Inventory, not left open.

**Cross-component propagation:**
- **Runtime (parser, type checker, evaluator, diagnostics):** *Proof engine* — fold rewired now to consume the core (this design). *Evaluator* — future adopter: when built it MUST dispatch through the same core (the designed `BinaryOp.Executor` *is* that core), not a fresh implementation. *Type checker* — **None** (it stamps `ProofRequirement`s and resolves `OperationKind`; it does not fold values). *Parser* — **None**. *Diagnostics* — **None** (no code added, changed, or removed; fold verdicts identical).
- **Tooling (syntax highlighting, completions, hover, semantic tokens):** **None** — the operator vocabulary and typed operations are unchanged; hover/proof-witness values are the same values, now guaranteed to match runtime.
- **MCP (vocabulary, DTOs, tool output):** **None** — `precept_operations` already surfaces `OperationMeta`; the executor delegate is internal behavior, not serialized vocabulary. No DTO or formatter change.

**Breaking changes:** **None to any public contract.** No diagnostic-code change, no catalog member rename that flows to grammar/completions/MCP. Internal only: `EvaluateBinaryOp` is deleted from `ProofEngine.Analysis.cs`; `Operations`/`OperationMeta` gains an internal executor accessor (additive).

### External architectural precedent

**Rust — one MIR interpreter (CTFE = Miri).** The sharpest precedent for "share one core so drift is structurally impossible" [survey §1, Primary, access 2026-06-05]:

> "The interpreter is a virtual machine for executing MIR without compiling to machine code. … The interpreter is shared between the compiler (for compile-time function evaluation, CTFE) and the tool Miri, which uses the same virtual machine…"
> — Rust Compiler Development Guide, *Interpreter*

**What Precept takes:** the core insight — one evaluation implementation means "a fold-vs-runtime mismatch is not a bug to chase, it is structurally unrepresentable" (survey §1). **What Precept deliberately diverges from:** Rust shares an entire *MIR virtual machine* (a loop-executing interpreter). Precept's branch-free, loop-free expression model (`spec §0.1 P5`; survey Threats-to-Validity "no loop interpreter to share") means Precept shares only the far smaller **pure operation-executor layer** — which makes the shared-core option *easier* for Precept than for a general compiler, not harder. The divergence is warranted: sharing the minimal pure kernel gets Rust's structural guarantee without adopting Rust's whole interpreter.

**Weaker fallbacks, deliberately not chosen.** C++ `constexpr` makes equivalence a *normative mandate* rather than sharing an implementation — "produces the same result as an invocation of an equivalent non-constexpr function" — but carves out floating point as "unspecified" (survey §2). CompCert/WASM keep two implementations bound by one formal semantics + a conformance oracle, and CompCert's own lesson is that "test-based conformance is *weaker* than sharing one implementation" (survey §4). Precept chooses the strongest posture available (share one core) precisely because, unlike a multi-vendor standard (C++) or a legacy two-implementation system (CompCert), Precept has no second implementation yet to reconcile — the cheap path and the soundest path coincide.

## Inventory of what will be built

- **Shared evaluation core (catalog layer).**
  - `src/Precept/Language/Operation.cs` — add an executor facet to the `OperationMeta` DU (mirroring the existing `IntervalTransfer` field): a pure delegate that computes the operation's value. Keyed on `OperationKind` (the catalog key), so both the fold and the runtime dispatch through the identical entry. Defined over a value abstraction both phases adapt to (see value-domain bridge below).
  - `src/Precept/Language/Operations.cs` (or a new partial `Operations.Executors.cs`) — populate the executor for each value-producing `OperationKind`, lifting the arithmetic currently inlined in `EvaluateBinaryOp`. Preserve the zero-denominator conservative guard (unknown-on-unsafe) verbatim for divide/modulo.
- **Value-domain bridge.** The kernel must be callable from the fold's boxed `object?` domain and (later) the runtime's `PreceptValue` domain (`evaluator.md:281`). Chosen shape: one arithmetic kernel over `decimal`/`string`/`bool`, with a thin fold-side adapter (boxed `object?` ↔ kernel) now and a runtime-side adapter (`PreceptValue` ↔ kernel) when the runtime is built. No open question — the fold already holds `bin.ResolvedOp` (the `OperationKind`), so keying the kernel on `OperationKind` is strictly available and *more precise* than the fold's current coarsening to `OperatorKind`.
- **Proof-engine rewire.**
  - `src/Precept/Pipeline/ProofEngine.Analysis.cs` — `FoldValue`'s `TypedBinaryOp` (:261-270) and `TypedUnaryOp` (:272-284) cases call the shared core via the resolved `OperationKind`; **delete** the inline `EvaluateBinaryOp` (:351-405) and the inline unary arithmetic. `ConstantFold`/`FoldValue`/`BuildDefaultEnvironment` structure and the two-scans-share-one-fold invariant (`proof-engine.md:483/1987`) are preserved.
- **Tests.** `test/Precept.Tests/` —
  - a characterization matrix: for every value-producing `OperationKind` × representative operand values, assert the shared core's output (pins behavior so the future runtime adoption is guarded);
  - a fold-verdict-preservation test: `Compiler.Compile` on every `samples/*.precept` yields byte-identical diagnostics before/after the rewire (uses `Compiler.Compile`, not the type-checker-only helpers, so PRE0115/PRE0164/PRE0079-on-default proof verdicts are actually exercised).
- **Docs.** Enumerated in § Doc-update enumeration.
- **Readiness-plan amendment.** OD-1 reclassified from recorded runtime-build note to compiler-scope build work (a `/plan` amendment to a Working doc).

## Decisions

### Decision 1: Share one evaluation core, compiler-scope, established now

**Stakes**: high — establishes an architectural seam (catalog-attached executor) the runtime will depend on; reversal is costly but nothing ships to external authors and no public contract locks.

- **Rationale**: `spec §0.1 P11` ("no error ⇒ no fault") is sound only if the fold's predicted value equals the runtime's computed value. Sharing one core makes that a reflexive identity — the drift the survey documents (GCC target-FP, LH overflow) becomes structurally unrepresentable. Doing it *now*, while only one implementation exists, is the moment of minimum cost; the readiness plan itself calls this "cheap to establish now and expensive to retrofit later" (OD-1). Deferring it to the runtime initiative (the status quo) means the second implementation gets written first and the seam is retrofitted onto two divergent code paths.
- **Tradeoff accepted**: Couples the proof engine to the catalog executor model and forfeits any phase-specialized fast fold arithmetic — the same trade Rust accepts (CTFE has no separate fast const-folder) and GCC accepts (emulate the target, never the host's native FP).
- **Alternatives considered**:
  - *Defer to the runtime initiative (current plan — OD-1 as a recorded note)* — rejected: it writes the second implementation before the seam exists, converting a zero-cost preemption into a two-implementation retrofit; the survey's whole point is that the two-unrelated-evaluators state is the empirically-unsound pole.
  - *Keep two implementations, bind them with a conformance differential (CompCert/WASM point 4)* — rejected as the *primary* choice: CompCert's own lesson is that test-based conformance is strictly weaker than sharing one implementation (survey §4); it is retained only as the fallback if Decision 2's pure-function assumption fails (see Falsifiers).
  - *Equivalence-by-mandate (C++ constexpr)* — rejected: a normative "must be equal" obligation with no shared implementation still permits drift (C++ carves out FP as "unspecified"); a mandate is weaker than a single implementation.
- **Precedent**: Rust one-MIR-interpreter (CTFE = Miri); Zig comptime = ordinary Zig at semantic-analysis time; Dhall single standardized normalizer (survey §1). The synthesis: "concrete static evaluation and a shared/mandated-equal runtime semantics travel together; no surveyed system concretely evaluates statically while leaving an unrelated runtime evaluator free to disagree" (survey Conclusions).
- **Sources consulted for this decision**:
  - `research/architecture/compiler/static-vs-runtime-expression-evaluation-survey.md` §1 — "The interpreter is shared between the compiler (for compile-time function evaluation, CTFE) and the tool Miri, which uses the same virtual machine…"; Conclusions — "no surveyed system concretely evaluates an expression at compile time while leaving an unrelated runtime evaluator free to disagree — that combination is exactly the empirically-demonstrated drift pole."
  - `docs/Working/compiler-readiness-plan-2026-06-16.md:303` — "the runtime evaluator and the compile-time `ConstantFold` must share **ONE** expression-evaluation core. This constraint is cheap to establish now and expensive to retrofit later."
  - Spec-first (guard 17): survey § "Spec-first check (Step 1b)" — "the spec locks that the *two compile-time scans* … share 'one default environment and one constant-fold evaluator' (`precept-language-spec.md:213`; `proof-engine.md:1987` — 'no fork'). It does **not** decide whether that compile-time fold shares an engine with the *runtime* evaluator — the evaluator is a stub … So Q-A is genuinely open, not spec-decided." → legitimate design decision, not implementation-against-locked-spec.
- **Strongest counter-evidence**: The survey's own "What would change this conclusion" — "If Precept's runtime executor model turns out to make a shared core impossible (e.g. the executable model's opcode/delegate dispatch is fundamentally incompatible with the proof engine's `TypedExpression` walk), then option 1 falls." Response: the feasibility check shows the fold is a pure `object?`→`object?` kernel and the runtime's designed executor is a pure `Func<PreceptValue,PreceptValue,PreceptValue>` (`evaluator.md:281`) — both are pure operation-level functions with a mechanical value-domain bridge, so no fundamental incompatibility exists; the risk is a value-adapter, not an architecture wall. No further counter-evidence found after checking the survey, the three sibling surveys, and `precept-builder.md`/`evaluator.md`.
- **Reversibility**: `Hard` — once the runtime is built against the shared core, unwinding it means re-introducing a second implementation (the exact drift the design prevents). But nothing external ships, so it is not effectively-irreversible.
- **Blast radius**: Catalogs — `Operations`/`OperationMeta` (executor facet added). Docs — `proof-engine.md`, `evaluator.md`, `compiler-and-runtime-design.md`, the readiness plan. Samples — none (verdicts must be identical). External consumers — none.

### Decision 2: The shareable unit is the catalog operation executors (pure arithmetic), independent of the stubbed Fire/Update

**Stakes**: high — fixes where the shared core lives (catalog seam) and what shape it has (pure per-`OperationKind` executor), which the runtime's dispatch will depend on.

- **Rationale**: The value the fold and runtime must agree on is the *result of one operation on operands*. The catalog already keys every operation by `OperationKind` with its typed operands, result, interval transfer, and proof requirements — the value executor is the missing facet and belongs there. The feasibility check confirms the arithmetic is pure and has no entity-state dependency, so it is shareable without the runtime existing.
- **Tradeoff accepted**: A value-domain bridge is required (fold's `object?` vs runtime's `PreceptValue`), adding one adapter layer; and the executor facet slightly enlarges `OperationMeta`.
- **Alternatives considered**:
  - *Share a higher-level unit (the whole `TypedExpression` walker)* — rejected: the walker mixes environment lookup, unfoldable-field handling, and conditional/interpolation folding with the pure operation step; only the operation step needs to be shared for the drift guarantee, and the environments differ (fold: default env; runtime: entity data).
  - *Share a lower-level unit (raw `OperatorKind` over decimal)* — rejected: that is the fold's *current* coarsening, which discards the `OperationKind` distinctions the catalog and the runtime dispatch on; keying on `OperationKind` is strictly more precise and matches both consumers.
- **Precedent**: `Operations.cs` already attaches per-`OperationKind` behavior facets (`IntervalTransfer`, `ProofRequirements`) to the metadata; the executor mirrors that established pattern. Externally, Dhall's single normalizer and CUE's single unification engine both attach evaluation to the operation/type definition, not to a pipeline stage (survey Q-B table).
- **Sources consulted for this decision**:
  - `src/Precept/Language/Operations.cs:107-113` — `IntegerPlusInteger => new BinaryOperationMeta(kind, OperatorKind.Plus, PInteger, PInteger, TypeKind.Integer, "Integer addition") { IntervalTransfer = AddTransfer }` (behavior facets already live on the meta).
  - `src/Precept/Pipeline/ProofEngine.Analysis.cs:351-405` — `EvaluateBinaryOp(OperatorKind op, object? left, object? right)`: pure, decimal/string/bool, no entity state.
  - `src/Precept/Runtime/Evaluator.cs:39-40` — "Do NOT use `Operations.Resolve()` … Do NOT claim catalog-driven execution dispatch until delegate fields exist in catalog metadata" (the executor facet is exactly the delegate field this design adds).
  - `docs/runtime/precept-builder.md:536` — the runtime design already plans to "fetch the executor delegate … and embed it directly in the opcode," confirming the runtime intends to consume a catalog executor, not hand-roll its own.
  - Spec-first (guard 17): grepped `precept-language-spec.md`/`proof-engine.md`/`business-domain-types.md` — the spec locks the operation *set* and typed results but is silent on *where the value executor lives*; this placement is a genuine architecture decision, not a spec re-litigation.
- **Strongest counter-evidence**: `evaluator.md:281` designs the runtime executor as `Func<PreceptValue,PreceptValue,PreceptValue>` — a *different* value domain than the fold's `object?`, which could be read as "the units are not actually shareable." Response: the domains differ but the arithmetic is identical; a single kernel with two value adapters shares the arithmetic (the thing that can drift) while letting each phase keep its representation — the bridge is mechanical, not a semantic fork. Looked in `evaluator.md`, `precept-builder.md`, and the fold code; found no state dependency that would break purity.
- **Reversibility**: `Hard` — the runtime's opcode dispatch would be built against this executor shape.
- **Blast radius**: Catalogs — `OperationMeta` (executor facet). Docs — `catalog-system.md` (Evaluator-catalog integration pattern), `proof-engine.md`, `evaluator.md`. Samples — none. External consumers — none.

### Decision 3: Number-model pin — the shared core fixes one `decimal`-based value representation

**Stakes**: medium — reversible with bounded effort; and largely *ratifies* an already-locked posture rather than choosing afresh.

- **Rationale**: The two documented drift cases both reduce to "static and runtime used different number models" (GCC FP; LH overflow — survey §5). The shared core, by being one function, makes the fold's and the runtime's numeric representation *the same object*, not two independently-chosen `decimal`s that could later diverge (e.g. if the runtime picked a different overflow/rounding boundary). This is the LH lesson applied preemptively.
- **Tradeoff accepted**: The fold may not use a faster host-native numeric type; `Number`/IEEE-754 stays in the declared-approximate lane and is not folded to a claimed-exact value.
- **Alternatives considered**:
  - *Let fold and runtime each pick their own numeric type, reconciled by test* — rejected: this is exactly the LH SMT-int-vs-machine-int gap (survey §5b), unsound at the boundary where the models diverge.
  - *Introduce a new number model for the core* — rejected and unnecessary: `decimal` is already mandated for the fold (`proof-engine.md:110`) and is already the runtime `PreceptValue` payload (`evaluator.md:230/254` "the largest union payload (`decimal`, which is 16 bytes)").
- **Precedent**: C++ carves out FP as the one place constexpr≡runtime is abandoned (survey §2); Precept's `decimal`-not-`double` rule sidesteps that carve-out entirely — the pin ratifies that choice as structural rather than per-phase.
- **Sources consulted for this decision**:
  - Spec-first (guard 17): `docs/compiler/proof-engine.md:110` — "the proof engine operates on `decimal` magnitudes specifically to avoid binary-floating-point rounding drift." The `decimal` *choice itself is already settled* — this decision is therefore **not** a fresh number-model choice; it is the decision that the shared core makes the fold's already-locked `decimal` and the runtime's already-`decimal` `PreceptValue` **one** representation instead of two coincidentally-equal ones.
  - `docs/runtime/evaluator.md:254` — "union payload (decimal, long, reference region)" — the runtime's designed value struct is decimal-based, corroborating that no new choice is introduced.
  - `research/architecture/compiler/static-vs-runtime-expression-evaluation-survey.md` §5b — "LiquidHaskell … assumes that machine addition behaves like logical addition … by default this is a soundness gap."
- **Strongest counter-evidence**: The survey's Open Question 3 — "The executable-model design has not fixed the runtime numeric representation; if it differs from the fold's `decimal`, the LH-style gap is live." Response: this is precisely why the pin is made structural now (one core ⇒ one representation) rather than left to the executable-model design to re-decide; if that design later fixes a non-`decimal` representation, this decision (and the whole design) is falsified — see Falsifiers. (Not counter-evidence against the decision; it is the reason for it.)
- **Reversibility**: `Hard` — a later non-`decimal` runtime representation would force re-deriving fold↔runtime equivalence, but no external surface locks.
- **Blast radius**: Catalogs — none (representation, not membership). Docs — `proof-engine.md`, `evaluator.md`. Samples — none. External consumers — none.

### Decision 4: Reclassify OD-1 from a deferred runtime-build note to compiler-scope build work

**Stakes**: medium — amends a Working-doc plan item across the compiler/runtime scope boundary; reversible, bounded, affects `/plan` sequencing only.

- **Rationale**: OD-1 today is *recorded* as a runtime-build constraint (readiness plan Slice 0.3: "Record the constraint…," with "No runtime code lands"). But the shareable unit is a compiler artifact (the proof fold) and the extraction is landable *while the runtime stays stubbed*. Leaving OD-1 as a deferred note lets the runtime initiative build the second implementation first — the retrofit the plan itself calls "expensive." Pulling it into compiler scope lands the seam now, when it is cheap.
- **Tradeoff accepted**: Adds a compiler-code slice to an already-large readiness plan; the fold refactor must be sequenced without disturbing the Phase-1/Phase-2 hard ordering.
- **Alternatives considered**:
  - *Keep OD-1 deferred (status quo)* — rejected per the rationale (retrofit cost; second implementation written first).
  - *Do the extraction outside the readiness plan entirely* — rejected: the plan is the single sequencing surface for compiler-scope work; an out-of-plan refactor would fork the coordination the plan exists to hold.
- **Precedent**: The readiness plan already reclassifies deferred items into landable compiler edits when they are found to be compiler-scope — e.g. the `RestoreConstraintsFailed` removal is "recorded here … Phase 7 lands the edit" (readiness plan Slice 0.3); OD-1 gets the same record-here / land-in-a-compiler-phase treatment.
- **Sources consulted for this decision**:
  - `docs/Working/compiler-readiness-plan-2026-06-16.md:303` — "OD-1 — shared expression-evaluation core. Record the constraint … This constraint is cheap to establish now and expensive to retrofit later."; and Slice 0.3 exit — "OD-1 … recorded as runtime-build constraints, each with a named capture pointer. … No runtime code lands; the `Evaluator.cs` stubs remain stubs."
  - `docs/Working/compiler-readiness-plan-2026-06-16.md:63` — "OD-1 = the constraint that the runtime evaluator and compile-time constant folding must share one expression-evaluation core."
  - The plan's scope boundary (`:status`) — "COMPILER code + ALL CANONICAL DESIGN … Runtime CODING is OUT." The fold rewire is COMPILER code, so it is *in* scope; only the runtime's adoption is out.
- **Strongest counter-evidence**: The plan's Slice 0.3 exit criterion "No runtime code lands; the `Evaluator.cs` stubs remain stubs" could be read as "OD-1 is a runtime concern, keep it deferred." Response: the criterion constrains *runtime* code, and this design lands *proof-engine* (compiler) code while honoring that exact criterion — the stubs stay stubs; only the fold changes. So the reclassification is consistent with, not contrary to, the plan's boundary.
- **Reversibility**: `Easy` — it is a plan-treatment decision; `/plan` can re-sequence or re-defer with no code cost until the slice is scheduled.
- **Blast radius**: Docs — `docs/Working/compiler-readiness-plan-2026-06-16.md` (OD-1 row). Catalogs — none. Samples — none. External consumers — none.

## Falsifiers

1. **Non-`decimal` runtime representation.** If the runtime executable-model design fixes a non-`decimal` numeric representation (e.g. `Int128`, a rational, or IEEE-754 for the exact lane), the "shared core fixes the number model" claim (Decision 3) is falsified and the fold↔runtime equivalence must be re-derived over the new representation — likely reopening the CompCert/WASM differential fallback.
2. **Executors need entity state.** If lifting the arithmetic reveals that any value executor requires entity state / slot context the fold cannot supply, Decision 2's cheap-now/pure-function claim falls and the design collapses to the conformance-differential fallback (keep two implementations, bind by a test oracle) — the *weaker* posture the survey warns against.
3. **Extraction changes a verdict.** If the fold-verdict-preservation test shows any `samples/*.precept` diagnostic appears or disappears after the rewire, the extraction is not behavior-preserving and must be redone; a changed verdict means the shared core is not semantically identical to the fold it replaced.
4. **A second implementation reappears.** If, after this ships, any pipeline stage or the runtime grows a second inline arithmetic switch (a new `EvaluateBinaryOp`-shaped block outside the catalog core), the single-core invariant is broken and the design's "no differential needed because there is one implementation" assumption is void — a build-time guard (Roslyn analyzer forbidding arithmetic outside the core) would then be required.

## Acceptance criteria

All criteria are verifiable **with the runtime still stubbed**:

1. **No inline fold arithmetic.** `EvaluateBinaryOp` and the inline unary arithmetic are gone from `src/Precept/Pipeline/ProofEngine.Analysis.cs`; `grep -n "EvaluateBinaryOp" src/Precept/Pipeline/` returns nothing. `FoldValue`'s binary/unary cases dispatch to the catalog core.
2. **Core reachable from the catalog.** A single evaluation entry, keyed on `OperationKind`, is reachable from `Operations`/`OperationMeta`; the fold obtains it via `bin.ResolvedOp`/`un.ResolvedOp`.
3. **Characterization matrix green.** A test in `test/Precept.Tests/` asserts the core's output for every value-producing `OperationKind` × representative operand values (including the divide/modulo-by-zero → unknown guard). `dotnet test` green.
4. **Fold verdicts unchanged.** `Compiler.Compile` (not the type-checker-only helpers) on every `samples/*.precept` yields identical diagnostics before and after; `precept_compile` behavior on the sample corpus is unchanged.
5. **Runtime untouched.** No `using Precept.Runtime` and no reference to `Runtime/Evaluator.cs` from the proof engine (`grep`); `Evaluator.cs` operation bodies remain `NotImplementedException`.
6. **Differential specified, not run.** The two-implementation differential (`fold-executor result == runtime-executor result` for every op × operand-type) is documented as a conformance-suite row that becomes live only once the runtime executors exist — explicitly *not* runnable today (one implementation), and recorded as the future guard (Phase 7 conformance / runtime initiative).

## Dependencies

- **Upstream**: The `Operations` catalog and `OperationKind` resolution (present); the readiness plan's Phase-1 compiler-code phase existing as the landing site (present, Draft). No owner decision blocks — this is compiler-scope implementation against an open (non-spec-settled) architecture question, per the survey's Spec-first check.
- **Downstream**: Enables the runtime initiative to build `Fire`/`Update` dispatch against a ready shared core (no retrofit); enables the Phase-7 conformance program's fold-vs-executor differential row; strengthens the `spec §0.1 P11` guarantee the whole prevention story rests on.

## Doc-update enumeration

- `docs/compiler/proof-engine.md` § 7.7 (Constant Folder) and § Initial-State Satisfiability — the fold now delegates binary/unary value computation to the catalog shared core; `EvaluateBinaryOp` is no longer inline; the `ConstantFold`/`FoldValue`/`BuildDefaultEnvironment` shared-fold invariant is preserved.
- `docs/runtime/evaluator.md` § number model (`PreceptValue`) and a new shared-core-seam note — the runtime MUST dispatch through the catalog executor core; the designed `BinaryOp.Executor` *is* that core, not a second implementation.
- `docs/compiler-and-runtime-design.md` — a short subsection near the catalog-driven grounding (§2) describing the `ConstantFold` ↔ evaluator shared-core seam, held to the same CEL/OPA/CUE comparative standard the section already uses.
- `docs/language/catalog-system.md` § (OperationMeta / Evaluator-catalog integration pattern) — record the executor facet as catalog metadata if the delegate-field route is taken.
- `docs/Working/compiler-readiness-plan-2026-06-16.md` — OD-1 amendment: reclassify from recorded runtime-build note (Slice 0.3 / Phase-7 confirm) to compiler-scope build work. This is a plan amendment `/plan` handles (the plan is a Working doc).

## Operational dimensions

- **Security** — N/A: the design does not touch source-text ingestion (lexer/parser/MCP input); it relocates value computation for already-parsed, already-type-checked expressions.
- **Observability** — Addressed: the shared core is a compile-time analysis path; when it folds to the unknown sentinel (e.g. divide-by-zero denominator), the existing proof-engine diagnostics and `DescribeExpression` witness rendering are unchanged, so authors diagnose exactly as they do today. No new runtime trace/log surface is introduced (runtime stays stubbed).
- **Evolvability** — N/A: the design depends on no external standard (NodaTime/ICU/UCUM/ISO-4217/TZDB); `decimal` is a .NET primitive, not a versioned upstream.

## Open questions

None. The one open question the grounding survey left — "Is the runtime executor delegate set usable *as* the proof fold's arithmetic?" — is resolved by the feasibility check: the value-executors do not yet exist in code (only the fold's pure `EvaluateBinaryOp` does), so the design lifts that single pure kernel into the catalog seam and points the fold at it, with a mechanical value-domain adapter scoped in the Inventory. No decision remains `Stakes: exploratory`; no `TBD` markers remain.
