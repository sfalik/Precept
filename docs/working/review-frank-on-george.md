# Architectural Review: George's "Metadata Everywhere" Design

**Reviewer:** Frank, Lead Architect & Language Designer  
**Document under review:** `docs/working/metadata-everywhere-george.md`  
**Date:** 2026-04-27  
**Review type:** Cross-review for synthesis  
**Reference documents cited:**  
- `docs/philosophy.md` (philosophy)  
- `docs/language/catalog-system.md` (catalog architecture)  
- `docs/language/precept-language-spec.md` (language spec)  
- `docs/runtime/runtime-api.md` (runtime API)  
- `docs/compiler/parser.md`, `docs/compiler/lexer.md`, `docs/compiler/diagnostic-system.md`  
- Source: `ActionKind.cs`, `Operations.cs`, `Functions.cs`, `Modifiers.cs`, `Types.cs`

---

## 1. Full Agreement

These are the things George gets exactly right. No modifications needed.

### 1.1 ActionKind on Statement base

George's diagnosis is spot-on: every consumer must type-switch to reach `ActionMeta`, and that is hostile to metadata-driven execution. The fix — `ActionKind Kind` on the `Statement` base — is correct. The parser already knows the kind at construction time. This is the single most impactful structural fix in the entire document.

### 1.2 Parallel tree for TypedModel

George recommends parallel tree over in-place annotation, and his reasoning is correct: type checker as pure function (`SyntaxTree → TypedModel`), parse tree discarded after type checking, evaluator never needs parse tree. This matches my design exactly. The testability benefit is real — construct `TypedModel` instances directly without going through the parser.

### 1.3 Frozen dispatch tables keyed on kind enums

The `FrozenDictionary<ActionKind, ActionExecutor>` pattern is exactly right. Built once at startup, zero per-invocation branching, exhaustive switch enforcement at compile time. George's code for `BuildActionDispatchTable` correctly demonstrates the pattern. Same for `BinaryOperationTable` and `FunctionDispatchTable`.

### 1.4 OperationKind on TypedBinaryExpression / TypedUnaryExpression

George correctly identifies that `OperationKind` belongs on the typed model, not the parse tree — the parser doesn't have type information to resolve which operation a given operator invocation actually represents. The type checker fills this. This is a critical architectural boundary that George understands perfectly.

### 1.5 SlotIndex on FieldReference

`workingCopy[fieldRef.SlotIndex] = value` — O(1) field access. This is a runtime optimization that belongs in the typed model because the type checker has the full field inventory. No dictionary lookup at execution time. Good.

### 1.6 Proof engine as pure metadata reader

George's `CollectObligations` walks the typed tree, reads `OperationMeta.ProofRequirements`, `FunctionOverload.ProofRequirements`, and `ActionMeta.ProofRequirements` — all from catalog metadata. Zero hardcoded obligations. This is the proof engine architecture I designed, and George's implementation sketch is faithful to it.

### 1.7 Correctness axioms

The three axioms George declares (§ Preamble) are non-negotiable and correct:
- Compile-time obligations are exhaustive (proof engine zero-diagnostic → no `StaticallyPreventable` fault fires)
- Metadata is the source of truth (evaluator dispatches through kind enums)
- No runtime re-checking of statically proven obligations

### 1.8 Proof certificates: emit but don't wire

George's "emit but don't wire yet" verdict on proof certificates is pragmatic and correct. The proof logic exists anyway; certificates are free metadata. Wiring them into the evaluator's hot path is premature optimization. Profile first.

### 1.9 Defensive checks as bug-catchers, not re-verification

George's framing is exactly right: the evaluator's defensive check on `DivisionByZero` is a proof-engine bug detector, not user-facing validation. If it fires, the proof engine failed — not the user. This preserves the correctness axiom while maintaining defense-in-depth.

---

## 2. Pushback (with citations)

These are design decisions in George's document that are wrong, incomplete, or contaminated by Roslyn thinking.

### 2.1 TypedDeclaration is missing ConstructKind — this is a major omission

**George's design (§5, line 327):**
```csharp
public abstract record TypedDeclaration(SourceSpan Span);
```

**My design (§4):**
```csharp
public abstract record TypedDeclaration(ConstructKind Kind, SourceSpan Span);
```

George carries `ActionKind` on `TypedStatement` and `OperationKind` on typed expressions — but drops `ConstructKind` from `TypedDeclaration`. This is the same gap he correctly diagnosed for statements, applied to the declaration layer.

**Why this matters:** The `ConstructKind` catalog already has `ConstructMeta.AllowedIn` for semantic scoping, and the graph analyzer, proof engine, and MCP serialization all need to know what kind of declaration they're processing. Without `ConstructKind` on the base, every consumer must type-switch on the C# type hierarchy — exactly the anti-pattern George's own document condemns in § 1.

**Reference:** `docs/language/catalog-system.md` § Catalog Inventory describes `Constructs` as one of the 8 language definition catalogs. The declaration nodes in the parse tree already carry `ConstructKind` on the base. Dropping it in the typed model is a regression.

**Verdict:** Add `ConstructKind Kind` to `TypedDeclaration` base. Non-negotiable.

### 2.2 Separate TypedStatement subtypes with ActionKind on base — Roslyn bias

**George's design (§5, lines 357-368):**
```csharp
public sealed record TypedSetStatement(
    ActionKind Kind, SourceSpan Span,
    FieldReference Target,
    TypedExpression Value) : TypedStatement(Kind, Span);

public sealed record TypedAddStatement(
    ActionKind Kind, SourceSpan Span,
    FieldReference Target,
    TypedExpression Value) : TypedStatement(Kind, Span);
```

George puts `ActionKind` on the base (good) but then retains one sealed record type per action verb (bad). Look at those two records. They are *structurally identical.* `Set` and `Add` both carry `Target` + `Value`. So do `Remove`, `Enqueue`, `Push`. The only actions with a different shape are `Dequeue`, `Pop`, and `Clear` (no `Value`).

This is Roslyn thinking: "each syntax kind gets its own node type." In Roslyn, that's necessary because `SyntaxKind` is a flat enum spanning 400+ kinds across fundamentally different grammar productions. In Precept, `ActionKind` has 8 members across 2 shapes (with-value and without-value). The separate types add nothing that `ActionKind` on the base doesn't already provide — and they force downstream stages back into type-switching.

**The catalog-driven alternative:** `ActionMeta.ValueRequired` already tells you the shape. The evaluator dispatches on `ActionKind`, not on C# type. Why should the typed model force consumers through a type hierarchy that duplicates what the catalog already declares?

**Two shapes, not eight types:**
```csharp
public sealed record TypedAction(
    ActionKind Kind,
    FieldReference Target,
    TypedExpression? Value,  // null iff ActionMeta.ValueRequired == false
    SourceSpan Span);
```

The type checker validates that `Value` is present when `ActionMeta.ValueRequired` is true and absent when false. The typed model carries the validated fact. Consumers dispatch on `Kind`.

**Verdict:** Collapse the eight `TypedXxxStatement` types into one `TypedAction` record. The catalog describes the shape; the typed model carries the result.

### 2.3 TypedFunctionCall carries `string FunctionName` — raw string in metadata-driven model

**George's design (§5, line 393):**
```csharp
public sealed record TypedFunctionCall(
    TypeKind ResultType, SourceSpan Span,
    string FunctionName,
    IReadOnlyList<TypedExpression> Args,
    FunctionKind Kind,
    int OverloadIndex) : TypedExpression(ResultType, Span);
```

`FunctionName` is a raw string. But `FunctionKind` is already on the node. `Functions.GetMeta(kind).Name` gives you the function name from the catalog. The `string FunctionName` field is a parallel copy of catalog data — exactly the anti-pattern that `docs/language/catalog-system.md` § "Derive, never duplicate" prohibits.

**Verdict:** Remove `string FunctionName`. Consumers who need the display name call `Functions.GetMeta(kind).Name`.

### 2.4 FaultContext uses `Type? DeclaredFieldType` — CLR type leak

**George's design (§4, line 303):**
```csharp
Type? DeclaredFieldType = null
```

This is a `System.Type` — a CLR reflection type. In a metadata-driven model, the field's declared type is `TypeKind`, not `Type`. The fault bridge should carry `TypeKind? DeclaredFieldType`. This is a small fix but a telling one: the CLR type system should not leak into Precept's domain model.

**Verdict:** Replace `Type? DeclaredFieldType` with `TypeKind? DeclaredFieldType`.

### 2.5 Defensive division-by-zero check contradicts the correctness axiom

George's evaluator (§2, lines 155-158) includes:
```csharp
if ((long)(r!) == 0)
    Fail(FaultCode.DivisionByZero);
```

George then says (§2, line 166): "defensive checks remain as bug-catchers."

I understand the intent, and I agree with the principle of defense-in-depth. But the implementation muddies the contract. The evaluator should use `Debug.Assert` or a conditional compilation guard (`#if DEBUG`) for these checks — not a `Fail()` call that produces a user-facing `Fault`. A `Fail()` call says "this is a valid runtime fault path." A `Debug.Assert` says "if you hit this, the proof engine has a bug."

**Reference:** `docs/language/catalog-system.md` § Roslyn Enforcement Layer — PRECEPT0001/PRECEPT0002 establish that every `FaultCode` maps 1:1 to a `DiagnosticCode` that should have prevented it. If the proof engine guarantees it, the evaluator should assert, not fail.

**Verdict:** Use `Debug.Assert` (or a dedicated `ProofViolation` internal exception) for proof-guaranteed paths. Reserve `Fail(FaultCode)` for genuinely reachable runtime fault paths.

---

## 3. Gaps and Missed Opportunities

These are areas George's design does not cover that a "metadata everywhere" vision must address.

### 3.1 No modifier node story

George's design covers statements (`ActionKind`), expressions (`OperationKind`, `FunctionKind`), and declarations (partially). It does not address how modifier nodes carry `ModifierKind` through the pipeline.

**Current state (from my design, §3.5):** Modifier arrays carry `Token[]` — consumers must re-translate to `ModifierKind`. This is a metadata gap. The parse tree should carry `ModifierKind` on modifier nodes, and the typed model should carry `ModifierKind[]` on declarations.

George's `FieldReference` does carry `IReadOnlyList<ModifierKind> Modifiers` (§5, line 417) — good. But there is no discussion of how modifiers are represented in the parse tree or how the parser resolves them. The modifier-to-kind mapping is a parser responsibility: `Modifiers.GetMeta()` bridges from `TokenKind` to `ModifierKind` via the `Token` field on each `ModifierMeta`. George should have shown this.

**Impact:** Without a complete modifier node story, the graph analyzer — which reads `ModifierKind` for semantic graph properties like `Subsumes`, `MutuallyExclusiveWith`, and `ImpliedModifiers` — has no designed input path.

### 3.2 No TypeRef node story

George's design jumps from parse tree to typed model without addressing how type references work at parse time. The parser sees `field Amount as integer` — the `integer` token must resolve to `TypeKind.Integer` at parse time (it's a pure lexical mapping from keyword to type). This is done via the `TypesByToken` frozen index derived from `Types.All`.

My design (§3.4) covers this: TypeRef nodes carry `TypeKind` in the parse tree. They vanish in the TypedModel because `TypeKind` appears directly on the typed nodes. George skips this entirely, which means the parse-tree-to-typed-model bridge is under-specified.

### 3.3 No graph analyzer coverage

George's document scope says "Evaluator, proof engine, TypedModel node shapes, dispatch tables, fault bridge." The graph analyzer sits between the type checker and the proof engine, consuming the `TypedModel` and producing `GraphResult` — which the proof engine needs as input. George's `ProofEngine.Prove(TypedModel model, GraphResult graph)` signature (§3, line 203) takes `GraphResult` as a parameter but never discusses what it contains or how it's produced.

The graph analyzer is where `ModifierKind` metadata drives reachability analysis, state graph completeness, field access legality per state, and modifier subsumption checking. Omitting it from a "metadata everywhere" design leaves a gap between type checking and proof.

### 3.4 No constraint evaluation architecture

George's evaluator (§7, line 493) calls:
```csharp
var violations = EvaluateConstraints(model, workingCopy);
```

This is the critical enforcement path — the reason Precept exists. "No invalid configuration can persist." And it's a one-line hand-wave. How are `rule` and `ensure` declarations represented in the typed model? How does `EvaluateConstraints` know which constraints apply in the current state? How are state-scoped ensures vs. global rules distinguished?

The philosophy says: "Rules are the broadest enforcement surface — they hold regardless of lifecycle position." And: "State-conditional ensures layer lifecycle-aware data constraints on top." These are architecturally distinct enforcement paths. George's `TypedModel` has `TypedFieldDeclaration.Constraints` (§5, line 339) but no `TypedRuleDeclaration` or `TypedEnsureDeclaration`. Where do rules and ensures live?

**Impact:** This is not a minor gap. Constraint evaluation is *the core of Precept's guarantee.* A metadata-everywhere design that hand-waves constraint evaluation is incomplete.

### 3.5 No MCP serialization story

The MCP tools (`precept_compile`, `precept_inspect`, `precept_fire`) are first-class distribution surfaces. When the TypedModel gains `OperationKind`, `FunctionKind`, `ActionKind`, `SlotIndex`, and `ModifierKind[]`, the MCP DTOs must serialize them. George's design doesn't address how the new metadata-rich nodes map to MCP output.

**Reference:** `docs/runtime/runtime-api.md` § Metadata-First Principle — "Every identifier that appears in the public API should be backed by a model-owned descriptor." The MCP surface is a public API. If the MCP DTOs don't reflect the metadata-rich typed model, the AI surface diverges from the runtime surface — which violates Precept's AI-first principle (philosophy: "full inspectability").

### 3.6 No language server unlock

George's design focuses on the evaluator path and ignores the tooling path. With `OperationKind` and `FunctionKind` on typed nodes, the language server unlocks:
- Hover on binary expression: `Operations.GetMeta(binExpr.Operation).Description`
- Completions in modifier position: filter `Modifiers.All` by `FieldModifierMeta.ApplicableTo` for the field's `TypeKind`
- Semantic tokens: `Constructs.GetMeta(decl.Kind).SemanticTokenType`

These are not nice-to-haves — they are direct consequences of metadata saturation that validate the architecture is working. A design that doesn't show the tooling unlock is only telling half the story.

### 3.7 Missing TypedEventArgRef in expression hierarchy

George's expression hierarchy (§5, lines 372-409) includes `TypedLiteral`, `TypedFieldAccess`, `TypedBinaryExpression`, `TypedUnaryExpression`, `TypedFunctionCall`, `TypedMemberAccess`, and `TypedConditional`. Missing: `TypedEventArgRef` — accessing event argument values in expressions (e.g., `set Amount = args.Amount` or guard expressions like `when args.Amount > 0`).

My design includes this (§4):
```csharp
public sealed record TypedEventArgRef(
    TypeKind ResultType,
    Identifier EventName,
    Identifier ParameterName,
    SourceSpan Span) : TypedExpression(ResultType, Span);
```

Event argument references are a fundamental part of the expression language. They appear in guards, actions, and constraint expressions. Omitting them from the typed expression hierarchy means the evaluator has no designed way to resolve event argument values.

### 3.8 Precept runtime class mixes concerns

George's `Precept` class (§7, lines 506-512) bundles `TypedModel`, `ProofModel`, and three slot maps into one class. The runtime API design (`docs/runtime/runtime-api.md`) establishes `Precept` as the public API surface — the executable model that hosts expose. But George's sketch puts `TypedModel` directly on it, which means the analysis-oriented tree is exposed through the runtime contract.

The runtime API doc explicitly states: "`Precept.From(CompilationResult)` lowers the analysis-oriented typed model into a runtime-optimized executable form." The executable model is a *lowered* form. It is not the `TypedModel` itself. George's design collapses this boundary.

**Impact:** If `Precept` exposes `TypedModel` directly, consumers can reach into analysis-level nodes during execution. The executable model should be a separate, runtime-optimized representation — potentially pre-computed dispatch arrays rather than tree-walking. George's design forecloses this optimization path.

---

## 4. Questions I'd Want Answered

### 4.1 Why eight TypedStatement subtypes if ActionKind is on the base?

George puts `ActionKind` on `TypedStatement` and then creates eight sealed subtypes. What shape variance between `TypedSetStatement` and `TypedAddStatement` justifies separate types? Both carry `FieldReference Target` + `TypedExpression Value`. If the answer is "future extensibility," that's not a Precept answer — that's a Roslyn answer. Precept's action catalog already handles extensibility through metadata.

### 4.2 Where do `rule` and `ensure` declarations live in the TypedModel?

George's `TypedModel.Declarations` contains `TypedFieldDeclaration`, `TypedEventDeclaration`, and `TypedTransitionRow`. But the language has `rule`, `ensure`, `access`, and `state` declarations too. Are these represented? If `TypedFieldDeclaration.Constraints` absorbs rules, how are state-scoped ensures distinct from global rules?

### 4.3 How does the evaluator resolve event arguments in expressions?

George's evaluator sketch (§7-8) passes `args` as `IReadOnlyDictionary<string, object?>` and the `TypedEventArg` carries `SlotIndex`. But there's no `TypedEventArgRef` expression type. How does `set Amount = args.RequestedAmount` evaluate? What node type does the evaluator encounter?

### 4.4 What is the executable model, and when is it produced?

The runtime API design doc identifies `Precept.From(CompilationResult)` as a lowering step. George's design puts `TypedModel` directly on `Precept`. My design asks (§9.3): is the executable model produced by the proof engine or a separate stage? George's design doesn't address this question at all. The boundary between analysis representation and execution representation needs a design decision.

### 4.5 How does `EvaluateConstraints` know which constraints apply in the current state?

The philosophy distinguishes rules (always apply) from ensures (state- or event-scoped). George's one-liner `EvaluateConstraints(model, workingCopy)` doesn't show how state-scoped filtering works. In a metadata-driven model, this should be derivable from catalog metadata — but which metadata, and how?

---

## Summary Assessment

George's design is **correct on the evaluator core** — the dispatch table architecture, kind-enum-keyed execution, and proof engine integration are exactly right. His runtime instincts are sound: slot-indexed field access, frozen dispatch tables, defensive-but-not-duplicative runtime checks.

Where the design falls short is **metadata saturation depth.** George applies the principle selectively — to the evaluator path — and does not carry it through declarations (`ConstructKind` missing), modifiers (no node story), type references (no parse-time resolution), constraint evaluation (hand-waved), MCP serialization (absent), or language server tooling (absent). The design also retains Roslyn-shaped patterns (one type per action verb) that the catalog system has already superseded.

The synthesis should take George's evaluator core (dispatch tables, slot-indexed working copy, proof certificates, fault bridge) and embed it in the complete metadata-saturation architecture: `ConstructKind` on declarations, unified `TypedAction` replacing eight subtypes, modifier and TypeRef node stories, constraint evaluation architecture, MCP and LS unlocks.

**Grade: Strong evaluator design, incomplete as a "metadata everywhere" vision.** The evaluator sections (§1-4, §7-8) are ready for implementation planning. The TypedModel (§5) needs the corrections above. The scope must expand to cover the full pipeline.

---

**End of Frank's Review**
