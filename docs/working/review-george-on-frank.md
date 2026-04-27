# Architectural Review: Frank's "Metadata Everywhere" Design

**Reviewer:** George, Runtime Dev  
**Document under review:** `docs/working/metadata-everywhere-frank.md`  
**Date:** 2026-04-27  
**Review type:** Cross-review for synthesis  
**Reference documents cited:**  
- `docs/philosophy.md` (philosophy)  
- `docs/language/catalog-system.md` (catalog architecture)  
- `docs/language/precept-language-spec.md` (language spec)  
- `docs/runtime/runtime-api.md` (runtime API)  
- `docs/compiler-and-runtime-design.md` (pipeline and runtime surfaces)  
- `docs/compiler/parser.md`, `docs/compiler/lexer.md`, `docs/compiler/diagnostic-system.md`  
- Source: `ActionKind.cs`, `Operations.cs`, `Functions.cs`, `Modifiers.cs`, `Types.cs`

---

## 1. Full Agreement

These are the things Frank gets exactly right. I would implement them as described.

### 1.1 The philosophical grounding (§1) is the strongest framing of the catalog principle I've seen

Frank's §1 draws the line precisely: "The pipeline is only metadata-driven if **every decision point reads kind, not C# type.**" This is the principle I was applying selectively to the evaluator, stated as a universal architectural invariant. He's right — my design applied the principle where I felt the most pain (evaluator dispatch) but did not enforce it uniformly across all node families. His framing is tighter and more defensible.

### 1.2 OperatorKind on parse tree, OperationKind on typed model (§3.3)

This is the correct boundary. The parser knows the lexical symbol (`OperatorKind.Plus`); it does not know the operand types. The type checker resolves the actual operation (`OperationKind.IntegerPlusInteger`) and annotates the typed model. Frank's code examples (§3.3, lines 118-133) show both sides cleanly. This matches my design exactly — we independently arrived at the same boundary, which is a strong confidence signal.

### 1.3 TypeRef nodes carrying TypeKind at parse time (§3.4)

Frank correctly identifies that `field Amount as integer` resolves `integer` → `TypeKind.Integer` at parse time via the `TypesByToken` frozen index from `Types.All`. This is a pure lexical mapping — the parser has all the information it needs. I omitted this from my design, and Frank's review (§3.2 in his review of me) correctly called it a gap. He's right. TypeRef resolution at parse time is catalog-driven dispatch that I should have specified.

### 1.4 Modifier node story with abstract base + ModifierKind (§3.5)

Three concrete shapes — `FieldModifier`, `StateModifier`, `AnchorModifier` — with `ModifierKind Kind` on the base. This mirrors the DU pattern already implemented in `Modifiers.cs` (where `ModifierMeta` has subtypes `FieldModifierMeta`, `StateModifierMeta`, `AccessModifierMeta`, `AnchorModifierMeta`). The node hierarchy should reflect the catalog hierarchy. This is the modifier story I failed to provide.

### 1.5 TypedModel as parallel tree, type checker as pure function (§4, §9.1)

`SyntaxTree → TypedModel` as a pure transformation, parse tree discarded after type checking. We agree completely. Frank's recommendation in §9.1 matches my design in §5 and the pipeline contract in `docs/compiler-and-runtime-design.md` lines 16-28.

### 1.6 FunctionKind: one per family, not per overload (§9.2)

`FunctionKind.Min` + `OverloadIndex` is the right answer. The alternative — `FunctionKind.MinIntegerInteger`, `FunctionKind.MinDecimalDecimal`, etc. — produces a combinatorial explosion. The `Functions.cs` catalog already uses this pattern: `FunctionMeta` carries an `Overloads` array, and `FunctionKind` identifies the family. Frank's recommendation aligns with the existing catalog design.

### 1.7 Detailed proof obligations, not aggregate status (§9.4)

Detailed obligations (`proven + unproven + diagnostics`) are necessary for inspectability. The philosophy says: "You see not only what would happen, but why." If the proof model only carries "complete" vs "incomplete," tooling and MCP lose the ability to explain *which* obligations were proven and *how*. Frank's recommendation is correct.

### 1.8 The stage → kind dispatch table (§10)

The summary table mapping each pipeline stage to its primary dispatch key is an excellent reference artifact. Parser dispatches on `ConstructKind` and `OperatorKind`, type checker resolves `OperationKind` and `FunctionKind`, graph analyzer reads `ModifierKind`, proof engine reads `ProofRequirement[]`, evaluator uses `ActionKind`/`OperationKind`/`FunctionKind` dispatch tables. This is the complete picture of how metadata flows through the pipeline.

---

## 2. Pushback (with citations)

These are design decisions in Frank's document that are wrong, incomplete, or where I think the runtime reality is more nuanced than the architecture suggests.

### 2.1 "Kind is assigned at parse time" is overstated as a universal constraint (§2)

Frank's unified principle states: "Kind is assigned once, at the earliest stage where it is determined, and carried forward as a declared property." Good. But the table in §2 then says: "Kind is assigned at parse time (or the earliest stage where identity is determined)."

The parenthetical weakens the constraint into a truism. The critical design question is: **for each kind, which stage is the earliest?** Frank correctly identifies that `OperationKind` is a type-checker responsibility (§3.3), but the unified principle table still says "parse time" as the primary case. This creates a misleading expectation that *most* kinds are parser-assigned.

In reality, the split is:
- **Parser-assigned:** `ConstructKind`, `OperatorKind`, `ActionKind`, `TypeKind` (on TypeRef), `ModifierKind`
- **Type-checker-assigned:** `OperationKind`, `FunctionKind`, `TypeAccessor`

That's roughly even. The principle should state: "Kind is assigned at the earliest stage where identity is determined" — full stop, without the "parse time" primary case that implies parse-time is the norm.

### 2.2 The proof engine producing the executable model (§9.3) conflates analysis and lowering

Frank recommends Option A: `TypedModel → ProofEngine → ExecutableModel`. His rationale: "The proof engine already knows all the metadata; assembling the executable model as part of proving soundness is natural."

I disagree. The proof engine's job is verification — collecting obligations, attempting proofs, emitting diagnostics. Lowering the typed model into a runtime-optimized executable form (dispatch table assembly, slot indexing, constraint scope pre-computation) is a separate concern. Bundling them makes the proof engine responsible for two fundamentally different operations: "is this model sound?" and "produce a runtime artifact."

**Reference:** `docs/compiler-and-runtime-design.md` lines 78-84 describe `Precept.From(compilation)` as constructing "dispatch tables, slot-indexed expression trees, scope-indexed constraints." This is lowering — taking the analysis-oriented tree and building a runtime-optimized form. `docs/runtime/runtime-api.md` lines 40-45 describe the same step: "`Precept.From(CompilationResult)` lowers the analysis-oriented typed model into a runtime-optimized executable form."

The lowering step reads from `TypedModel` + `GraphResult` + `ProofModel` — it needs the proof model as an *input*, not as a *co-product*. If the proof engine produces the executable model, then proof and lowering are coupled — a change to runtime optimization strategy requires modifying the proof engine. That's wrong.

**My recommendation:** Option B with a clarification. The pipeline remains five stages (`Lexer → Parser → TypeChecker → GraphAnalyzer → ProofEngine`). `Precept.From(compilation)` is the lowering step — it reads the complete `CompilationResult` and produces the executable model. The proof engine is pure verification.

Shane's answer #5 ("The ExecutableModel/proof boundary is already specified in `docs/compiler-and-runtime-design.md`") supports this: the boundary is *already* specified, and that spec puts lowering in `Precept.From()`, not in the proof engine.

### 2.3 The evaluator dispatch table sketch (§6) under-specifies the exhaustiveness enforcement mechanism

Frank writes: "Every `ActionKind`, `OperationKind`, and `FunctionKind` must have an entry — the compiler enforces exhaustiveness." But the code shown uses `FrozenDictionary<ActionKind, ActionExecutor>` — a runtime dictionary, not a compile-time switch. The compiler does **not** enforce exhaustiveness on dictionary population. If you forget to add `ActionKind.Clear` to the dictionary, the compiler is perfectly happy; the error surfaces at runtime as a `KeyNotFoundException`.

In my design (§1, lines 72-91), I showed the exhaustive switch inside `BuildActionDispatchTable()` — the switch on `ActionKind` triggers CS8509 if a member is missing, and the dictionary is populated from that switch. Frank's description is architecturally correct but the code sketch elides the critical enforcement mechanism. The synthesis document should be explicit: the frozen dictionary is the runtime dispatch structure, but the exhaustive switch inside the builder is the compile-time enforcement.

### 2.4 TypedAction as a single record (Frank's review §2.2) loses type safety on Value presence

Frank's review of my design recommends collapsing eight `TypedXxxStatement` types into one:

```csharp
public sealed record TypedAction(
    ActionKind Kind,
    FieldReference Target,
    TypedExpression? Value,  // null iff ActionMeta.ValueRequired == false
    SourceSpan Span);
```

I take Frank's point about Roslyn bias — eight structurally identical types are waste. But the unified record introduces a nullable `Value` where five of eight actions *always* have a value and three *never* do. The type checker validates this, but the evaluator must re-check nullability at runtime — `Value!` assertions on every value-required path, or defensive null checks that duplicate the type checker's work.

**The catalog already has the answer:** `ActionMeta.ValueRequired` is a boolean. Two shapes, not eight types and not one type. I concede the collapse from eight to fewer, but I'd prefer two:

```csharp
public sealed record TypedValueAction(
    ActionKind Kind, FieldReference Target,
    TypedExpression Value, SourceSpan Span);

public sealed record TypedNoValueAction(
    ActionKind Kind, FieldReference Target,
    SourceSpan Span);
```

This is the DU-shaped approach the catalog system doc recommends (§ Decision Framework, step 3): "Do members have varying metadata by kind? → DU." `Set`/`Add`/`Remove`/`Enqueue`/`Push` are value actions; `Dequeue`/`Pop`/`Clear` are no-value actions. The evaluator dispatches on `Kind` (from the base), and value-action handlers access `Value` without null assertions. Two shapes, catalog-aligned, zero runtime null checks.

That said — if the team decides the single `TypedAction` is simpler and the null-forgiving operator is acceptable, I won't die on this hill. The kind-based dispatch is the important part. But I want the tradeoff acknowledged: one record = runtime null-forgiving operators on a non-nullable semantic path.

### 2.5 The proof engine sketch (§5) is too tidy — it hides the hardest problem

Frank's proof engine code (§5, lines 231-246) walks the tree, reads `ProofRequirements` from metadata, and produces obligations. This is correct as far as it goes. But the hard part of the proof engine is not collecting obligations — it's *discharging* them. Frank's sketch doesn't address how the proof engine determines whether a guard in the execution path satisfies an obligation.

For example: if a transition row has `when Amount > 0` and then `set Rate = 100 / Amount`, the proof engine must recognize that the guard establishes `Amount != 0` (actually `Amount > 0`, which is stronger), and therefore the division's `NonZeroDivisor` obligation is discharged. This requires:

1. Path-sensitive analysis — the guard only holds inside the transition's scope
2. Expression matching — recognizing that `Amount > 0` implies `Amount != 0`
3. Modifier-based discharge — recognizing that `nonnegative` + `nonzero` on a field implies the value can be used as a divisor without a guard

Frank's §5 acknowledges this obliquely: "Conservative. The proof engine handles literal constants and field modifiers... Unknown expressions emit a diagnostic." But "conservative" needs specification. **Which discharge strategies are in scope?** My design (§3, lines 253-285) showed the literal-constant prover explicitly. The guard-based prover and modifier-based prover need at least as much specificity, because they determine the user experience — how many "add a guard" diagnostics the author sees.

This isn't a design flaw — it's an under-specification. The synthesis should enumerate the proof strategies: literal, modifier, guard-in-path, and leave an explicit expansion seam for future strategies.

---

## 3. Gaps and Missed Opportunities

### 3.1 No constraint evaluation architecture — the biggest gap in the document

Frank's design covers how metadata flows through the pipeline: parse tree → typed model → proof engine → evaluator dispatch tables. But it never addresses how `rule` and `ensure` declarations are represented, scoped, and evaluated at runtime.

The philosophy is unambiguous (`docs/philosophy.md` lines 33-36): "Rules are the broadest enforcement surface — they hold regardless of lifecycle position." And: "State-conditional ensures layer lifecycle-aware data constraints on top." These are architecturally distinct enforcement paths:

- **Rules:** Global. Checked after every fire and every update. Unconditional or guarded.
- **State ensures:** Checked when entering, occupying, or leaving a state (per anchor: `in`, `to`, `from`).
- **Event ensures:** Checked when firing a specific event (per anchor: `on`).

A metadata-everywhere design must address: How are constraints indexed by scope? How does the evaluator know which constraints apply after a transition that changes state? How are `EnsureAnchor` values from the Modifiers catalog (`InState`, `ToState`, `FromState`, `OnEvent`) used to build scope-indexed constraint tables?

This is the core of Precept's guarantee. I hand-waved it in my design too (§7, line 493: `EvaluateConstraints(model, workingCopy)`), and Frank correctly called me out. But Frank's design also hand-waves it — there is no `TypedRuleDeclaration`, no `TypedEnsureDeclaration`, no scope-indexing strategy, no constraint-table design.

**What the synthesis needs:** Pre-computed constraint tables indexed by (state × anchor), built during `Precept.From()` lowering. Global rules in an always-check list. State ensures in a `FrozenDictionary<(string state, EnsureAnchor anchor), ImmutableArray<TypedConstraint>>`. Event ensures similarly. The evaluator reads the current state and anchor, concatenates the applicable constraints, and evaluates them in order.

### 3.2 No executable model contract — the boundary between analysis and runtime

Frank asks the right question in §9.3 but doesn't answer it. `docs/runtime/runtime-api.md` lines 1-4 show that D8/R4 ("executable model contract") is open. `docs/compiler-and-runtime-design.md` lines 78-84 describe `Precept.From(compilation)` as the lowering step but don't specify what the lowered form contains.

The TypedModel is the analysis-oriented representation — it carries source spans, diagnostic-friendly structures, and the full AST shape. The executable model should be a runtime-optimized form: flat constraint arrays indexed by scope, slot-indexed field arrays, pre-resolved dispatch keys. These are different representations optimized for different consumers.

Frank's design puts `TypedModel` as the evaluator's input (§6: evaluator dispatch tables read from typed expressions). My design (§7, line 507) put `TypedModel` directly on the `Precept` class. We both conflate analysis and execution representations. The synthesis should define the boundary: what does `Precept` hold internally? Is it the `TypedModel` directly, or a lowered form?

Shane's answer #5 says the boundary "is already specified in `docs/compiler-and-runtime-design.md`." That doc says `Precept.From()` constructs "dispatch tables, slot-indexed expression trees, scope-indexed constraints." This implies a lowered form — not the TypedModel itself. The synthesis should respect this.

### 3.3 No story for how metadata reaches the runtime fault bridge

Frank's §7 (Runtime Fault Bridge) describes faults carrying `ActionKind`, `OperationKind`, `FunctionKind`, `FieldName`, `EventName`, and `FailedValue`. Good. But how does this context get assembled? The evaluator is executing through dispatch tables. When a fault fires, what is in scope?

My design (§4, lines 293-314) showed the `FaultContext` record but didn't show how the evaluator populates it during dispatch. The dispatch table handlers are static delegates — they don't have ambient context. The evaluator needs to thread execution context through the dispatch path so that a fault handler can capture the current action, operation, field, and event.

This is a design decision: is the execution context threaded explicitly (parameter on every handler), or captured via a mutable context object that the evaluator sets before each dispatch call? The latter is simpler but introduces mutable state in an otherwise functional dispatch path. The former is cleaner but makes every handler signature wider.

The synthesis should specify this. It affects every dispatch handler's signature.

### 3.4 Metadata-driven diagnostics — Frank's §5 stops at "emit a diagnostic"

When the proof engine fails to discharge an obligation, it emits a diagnostic. But which diagnostic? The proof engine needs to know, for each `ProofRequirement`, which `DiagnosticCode` to emit. This is a catalog bridge: `ProofRequirement` → `DiagnosticCode`.

The `FaultCode → DiagnosticCode` chain is well-specified (`docs/compiler/diagnostic-system.md` lines 36-38, catalog-system.md § Roslyn Enforcement Layer, PRECEPT0002). But the `ProofRequirement → DiagnosticCode` bridge is not. When the proof engine discovers an unprovable `NonZeroDivisor` obligation, does it emit `DiagnosticCode.PotentialDivisionByZero`? That code must exist in the `DiagnosticCode` enum and be connected to `FaultCode.DivisionByZero` via `[StaticallyPreventable]`.

Shane's answer #7 says "the proof/fault contract still needs a clear explanation." This is part of that gap. The synthesis should specify the chain: `ProofRequirement` (catalog metadata) → `DiagnosticCode` (compile-time) → `FaultCode` (runtime) → `[StaticallyPreventable]` back-reference. All metadata-driven, all enforced by PRECEPT0001/0002.

### 3.5 Shane's answer #1 ("Kinds should appear as soon as possible") is not reflected in the design

Shane says kinds should appear as soon as possible. Frank's design agrees in principle (§2: "Kind is assigned once, at the earliest stage where it is determined"). But neither design specifies what "as soon as possible" means for `ActionKind` on the parse tree.

Currently, the parse tree has `SetStatement`, `AddStatement`, etc. — distinct C# types with no kind. Frank's §3.2 says: "Fix: `ActionKind Kind` on the `Statement` base." But the parse tree currently uses separate types. The fix requires changing the parse tree's `Statement` base to carry `ActionKind`, which is a structural change to the parser's output.

This is straightforward — the parser already knows the action kind when it sees the keyword — but it should be stated explicitly: the parse tree's `Statement` base gets `ActionKind Kind`, assigned by the parser from the keyword token via the Actions catalog. This is the "as soon as possible" answer for actions.

### 3.6 Missing: how Shane's answer #6 ("all of it") changes the MCP/tooling scope

Shane says metadata everywhere means **all of it** — runtime, tooling, MCP, diagnostics, proof, faults, tests. Frank's §8 (Additional Unlocks) sketches the language server and MCP surface in three bullet points each. This is too thin for "all of it."

For MCP specifically: `precept_compile` currently returns a typed structure. With metadata-saturated nodes, that structure now carries `OperationKind`, `FunctionKind`, `ActionKind`, `ModifierKind[]`, `SlotIndex`, `TypeAccessor` references. The MCP DTOs must serialize all of this. An AI agent reading `precept_compile` output should be able to see, for every expression, what operation it represents and what proof requirements it carries — without calling `precept_language` to look up the metadata separately.

For tests: Shane says tests are in scope. The synthesis should specify: every catalog entry gets a theory test verifying the evaluator dispatch table has a handler for it. `Operations.All.Select(o => o.Kind)` → each has a `BinaryOperationTable` entry. `Actions.All.Select(a => a.Kind)` → each has an `ActionDispatchTable` entry. This is metadata-driven test generation — the tests derive from the catalog, not from ad-hoc test lists.

---

## 4. Questions I'd Want Answered

### 4.1 Does the executable model hold TypedModel nodes or lowered representations?

Frank's evaluator (§6) operates on typed expressions with `OperationKind` and `FunctionKind`. My evaluator (§7) does the same. Both treat the TypedModel as the evaluator's input. But `docs/compiler-and-runtime-design.md` says `Precept.From()` constructs "dispatch tables, slot-indexed expression trees, scope-indexed constraints." Is "slot-indexed expression trees" a rewrite of the TypedModel, or is it the TypedModel with slot indices already on FieldReference nodes?

If it's the TypedModel directly: `Precept` holds `TypedModel` + pre-computed dispatch tables + pre-computed constraint scopes.

If it's a lowered form: there's a `RuntimeExpression` hierarchy (or similar) optimized for evaluation — no source spans, no diagnostic-friendly metadata, just the execution-relevant fields.

The first is simpler. The second is cleaner architecturally and enables future optimizations (e.g., pre-compiled expression trees). I lean toward the first for now, with the lowering boundary available as a future optimization.

### 4.2 How are guards modeled in the typed tree for proof-engine consumption?

A guard like `when Amount > 0` needs to be visible to the proof engine as establishing a fact (`Amount > 0`) within the transition's scope. How is this represented? Is the guard just a `TypedExpression` on the transition row, and the proof engine pattern-matches on it to extract facts? Or is there a `GuardFact` abstraction that the type checker produces?

This matters because the proof engine's ability to discharge obligations depends on recognizing what guards establish. If guards are opaque expressions, the proof engine must do its own expression analysis. If guards are structured facts, the proof engine reads them as metadata.

### 4.3 Where do `ConstructKind` values for rule, ensure, access, and state declarations appear in the TypedModel?

Frank's §3.1 notes that `ConstructKind` is already on the parse tree's `Declaration` base. Good. Frank's §4 shows `TypedDeclaration(ConstructKind Kind, SourceSpan Span)` as the typed base. Good. But neither design shows the full set of `TypedDeclaration` subtypes.

My design (§5, lines 330-353) showed `TypedFieldDeclaration`, `TypedEventDeclaration`, and `TypedTransitionRow`. Frank's review (§3.4 in his review of me) correctly noted the absence of `TypedRuleDeclaration`, `TypedEnsureDeclaration`, `TypedStateDeclaration`, and `TypedAccessModeDeclaration`. Frank's own design doesn't show them either.

The synthesis needs the complete `TypedDeclaration` hierarchy — every `ConstructKind` that the parser can emit needs a corresponding `TypedDeclaration` subtype.

### 4.4 What is Frank's position on defensive checks in the evaluator?

Frank's review of my design (§2.5) recommends `Debug.Assert` over `Fail()` for proof-guaranteed paths. I understand the argument: `Fail()` says "this is a valid runtime fault path," while `Debug.Assert` says "this is a proof-engine bug." But `Debug.Assert` vanishes in release builds — it provides zero defense-in-depth in production.

My preference: a dedicated `ProofViolation` internal exception (not a `Fault`) that fires in all builds. It's not user-facing — it's an internal integrity violation that indicates a proof-engine bug. This preserves defense-in-depth without muddying the fault contract.

Is Frank amenable to this middle ground?

---

## Summary Assessment

Frank's design is **the correct architectural vision for metadata everywhere.** The philosophical grounding (§1) is precise and defensible. The unified principle (§2) is the right constraint. The node design (§3) covers every node family I missed — TypeRef, modifier, declaration — and correctly identifies the parse/typed boundary for each kind. The stage dispatch table (§10) is the complete reference.

Where the design falls short is **depth on the runtime-critical surfaces:**

1. **Constraint evaluation** — the core of Precept's guarantee — is not addressed. Rules vs. ensures, scope indexing, evaluation order, and the anchor model from the Modifiers catalog are all absent.
2. **The executable model boundary** — what `Precept.From()` produces — is asked about (§9.3) but not specified. Both designs conflate the analysis representation with the execution representation.
3. **Proof strategy enumeration** — which obligations the proof engine can discharge and how — is acknowledged as "conservative" but not specified.
4. **Fault context threading** — how the evaluator populates metadata-rich fault context during dispatch — is absent.

The synthesis should take Frank's architectural vision (universal kind annotation, catalog-driven dispatch at every stage, parallel tree TypedModel) and extend it with the runtime depth both designs are missing: constraint scope tables, executable model contract, proof strategy specification, and fault context threading.

**Grade: The strongest architectural framing of the catalog-driven principle. Needs runtime depth to be implementation-ready.**

---

**End of George's Review**
