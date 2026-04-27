# Combined Compiler + Runtime Architecture (v2 Source Draft)

## Overview

This document is the authoritative stage-by-stage architecture draft for Precept's compiler and runtime taken as one system. It replaces vague "compiler first, runtime later" thinking with an explicit contract chain:

1. catalog metadata defines the language,
2. compiler stages progressively resolve that metadata into richer artifacts,
3. lowering projects analysis artifacts into an executable model,
4. runtime operations consume only that executable model.

The governing rule is unchanged: **catalogs own language knowledge; stages own algorithms.** Every stage below therefore answers four questions precisely:

- what artifact it consumes,
- which catalogs it reads,
- what artifact it produces,
- who consumes that artifact next.

## Architectural identity

| Principle | Meaning in this document |
|---|---|
| Metadata-driven architecture | `Tokens`, `Types`, `Functions`, `Operators`, `Operations`, `Modifiers`, `Actions`, `Constructs`, `Diagnostics`, and `Faults` are the authoritative machine-readable language definition. |
| Earliest knowable metadata | A stage stamps metadata identities onto its output as soon as that identity is knowable, then downstream stages carry it forward rather than re-deriving it. |
| Split surfaces | `CompilationResult` is the analysis/tooling surface. `Precept` is the executable/runtime surface. |
| No parallel language knowledge | LS, MCP, grammar generation, checker logic, proof obligations, and runtime fault wiring derive from catalogs rather than hand-maintained lists. |

## End-to-end artifact chain

```text
source text
  -> Lexer.Lex                       -> TokenStream
  -> Parser.Parse                    -> SyntaxTree
  -> TypeChecker.Check               -> TypedModel
  -> GraphAnalyzer.Analyze           -> GraphResult
  -> ProofEngine.Prove               -> ProofModel
  -> Compiler.Compile                -> CompilationResult
  -> Precept.From(compilation)       -> Precept
  -> Precept.Create / Restore        -> Version
  -> Version.Fire / Update / Inspect -> outcomes + inspections + new Version
```

## Stage inventory

| # | Stage | Entry point | Primary input | Primary output | External consumers |
|---|---|---|---|---|---|
| 1 | Lexer | `Lexer.Lex` | `string` | `TokenStream` | LS semantic-token baseline, grammar tooling, MCP compile surfaces |
| 2 | Parser | `Parser.Parse` | `TokenStream` | `SyntaxTree` | LS syntax-aware navigation/recovery, diagnostics |
| 3 | Type checker | `TypeChecker.Check` | `SyntaxTree` | `TypedModel` | LS completions/hover/definition, MCP compile output |
| 4 | Graph analyzer | `GraphAnalyzer.Analyze` | `TypedModel` | `GraphResult` | structural diagnostics, runtime precomputation inputs |
| 5 | Proof engine | `ProofEngine.Prove` | `TypedModel` + `GraphResult` | `ProofModel` | proof diagnostics, lowering-time fault projection |
| 6 | Lowering / construction | `Precept.From` | `CompilationResult` | `Precept` | runtime, preview/inspection surfaces |
| 7 | Evaluator | `Evaluator.*` | `Precept` + `Version` + operation input | outcomes / inspections | host apps, MCP fire/update/inspect |
| 8 | Version operations | `Create`, `Restore`, `Fire`, `Update`, `Inspect*` | `Precept` or `Version` | `Version` snapshots + result DUs | host apps, preview tooling |

---

## 1. Lexer

### Stage contract

| Field | Contract |
|---|---|
| Purpose | Convert source text into a flat, span-rich token stream; detect lexical invalidity without blocking downstream stages. |
| Exact inputs | `string source` + `Tokens` catalog + `Diagnostics` catalog. |
| Output artifact | `TokenStream(ImmutableArray<Token> Tokens, ImmutableArray<Diagnostic> Diagnostics)`. |
| Metadata reflection | Each `Token.Kind` is a closed `TokenKind` identity. `Tokens.GetMeta(kind)` supplies text, categories, TextMate scope, semantic token type, and completion-context metadata (`ValidAfter`). |
| Downstream consumers | Parser always consumes `TokenStream`; `CompilationResult` merges lexer diagnostics. |
| External/tooling consumers | LS semantic-token fallback, grammar generator, MCP vocabulary, any raw-token debugging surface. |

### Output shape

| Type | Role |
|---|---|
| `Token` | concrete lexeme with `Kind`, semantic `Text`, and `SourceSpan` |
| `SourceSpan` | offset + line/column identity carried forward so later stages emit located diagnostics without re-reading source |
| `Diagnostic` | lex-stage errors such as invalid characters, unterminated literals, oversized input |

### Catalog entry point

The lexer is the first proof that Precept is metadata-driven rather than table-scattered:

- keyword recognition derives from `Tokens.Keywords`,
- token text/classification derives from `TokenMeta`,
- diagnostic identity derives from `Diagnostics.Create(...)`,
- literal/operator behavior is aligned with catalog vocabulary, not ad hoc strings.

### Current implementation note

`Lexer` is the only materially implemented compiler stage in current source. The shape above is real, not aspirational.

---

## 2. Parser

### Stage contract

| Field | Contract |
|---|---|
| Purpose | Convert the flat token stream into a source-faithful structural model of the authored program, including recovery nodes and spans. |
| Exact inputs | `TokenStream` + `Constructs` catalog + `Tokens` catalog + `Diagnostics` catalog. |
| Output artifact | `SyntaxTree`. Current source only exposes `SyntaxTree(ImmutableArray<Diagnostic>)`; the design target is a root node plus diagnostics. |
| Metadata reflection | Parser-resolvable identities such as construct kind, anchor keyword, action keyword, and operator token are stamped onto syntax nodes immediately. |
| Downstream consumers | Type checker is the sole semantic consumer. LS syntax-facing features may also read it directly. |
| External/tooling consumers | diagnostics, syntax-oriented outline/folding/recovery, source-preserving refactor tooling. |

### Target artifact shape

| Layer | Carries |
|---|---|
| `SyntaxTree` root | whole-file declaration list, source text identity, parser diagnostics |
| declaration nodes | precept header, field/state/event/rule/ensure/access/action/row declarations |
| expression nodes | source-structural expression tree using authored token/construct/operator identities |
| token references | exact authored tokens/spans, including recoverable or missing positions |

### Why `SyntaxTree` exists

`SyntaxTree` is **not** a cheap precursor to `TypedModel`. It owns responsibilities the semantic model should never absorb:

- exact authored structure,
- recovery under broken input,
- missing-token representation,
- delimiter fidelity,
- source-preserving span ownership,
- syntax classification independent of successful typing.

If the typed layer tries to replace this, LS authoring quality collapses on broken files.

### Current implementation note

`Parser.Parse(...)` is presently stubbed. The artifact boundary is correct; the node inventory still needs to be filled in.

---

## 3. Type checker

### Stage contract

| Field | Contract |
|---|---|
| Purpose | Resolve names, scopes, types, overloads, modifiers, legal operations, constraint identities, and semantic subjects. |
| Exact inputs | `SyntaxTree` + `Types`, `Functions`, `Operators`, `Operations`, `Modifiers`, `Actions`, `Constructs`, `Diagnostics`, and relevant token metadata. |
| Output artifact | `TypedModel`. Current source only exposes `TypedModel(ImmutableArray<Diagnostic>)`; the design target is a semantic graph plus diagnostics. |
| Metadata reflection | This is the first stage that resolves type- and operation-level catalog identity: `TypeKind`, `OperationKind`, `FunctionKind`, modifier metadata, action metadata, proof requirements attached to operations/accessors/functions. |
| Downstream consumers | Graph analyzer, proof engine, LS semantic tooling, MCP compile surfaces, lowering. |
| External/tooling consumers | completions, hover, go-to-definition, typed outline, symbol documentation, compile DTOs. |

### Target artifact shape

| Area | Design responsibility |
|---|---|
| symbol tables | fields, states, events, args, rule/ensure declarations |
| typed declarations | semantic records describing the definition independent of exact source sugar |
| typed expressions | resolved expression nodes carrying result type and resolved operation/function identity |
| typed actions | semantic action family (`TypedAction`, `TypedOperandAction`, `TypedBindingAction`) |
| typed constraints | normalized rule/ensure/access/row contracts with semantic subjects |
| diagnostics | type mismatch, undeclared symbol, illegal modifier use, invalid accessor, illegal operation, etc. |

### Why `TypedModel` exists

The checker's job is not "syntax tree plus types." It produces the semantic contract the rest of the system actually reasons over:

- syntax says `Approve.Amount <= RequestedAmount`;
- typed model says "binary operation `OperationKind.NumberLessThanOrEqualNumber` over event arg descriptor + field descriptor, result `boolean`."

That semantic normalization is what graph analysis, proof, and lowering need.

---

## SyntaxTree vs TypedModel (non-negotiable split)

| Question | `SyntaxTree` | `TypedModel` |
|---|---|---|
| Primary job | represent authored structure | represent resolved meaning |
| Shape bias | source-faithful | semantic / normalized |
| Recovery on broken input | yes, mandatory | degraded semantic model only |
| Owns token adjacency / delimiters | yes | no |
| Owns symbol/type resolution | no | yes |
| Owns operation/function overload identity | no | yes |
| Best consumer | parser diagnostics, source tools | LS intelligence, graph/proof, lowering |

### Why both exist

Because the two artifacts solve different architectural problems:

- `SyntaxTree` preserves what the author wrote.
- `TypedModel` explains what the program means.

Trying to collapse them creates one of two failures:

1. **syntax-biased semantic model** — typed layer degenerates into AST-with-annotations and becomes poor input for graph/proof/lowering;
2. **semantic-biased syntax model** — LS loses recovery/source fidelity exactly when the document is broken.

The correct architecture keeps both and forbids accidental mirroring.

---

## 4. Graph analyzer

### Stage contract

| Field | Contract |
|---|---|
| Purpose | Derive structural state-machine facts from the typed definition: reachability, event availability, modifier-structural validity, and precomputed topology the runtime later reuses. |
| Exact inputs | `TypedModel` + `Modifiers` catalog + `Actions` catalog + `Diagnostics` catalog. |
| Output artifact | `GraphResult`. Current source exposes `GraphResult(ImmutableArray<Diagnostic>)`; target shape adds graph facts and indexes. |
| Metadata reflection | State modifiers (`initial`, `terminal`, `required`, `irreversible`, outcome-state semantics) are interpreted from modifier metadata, not hardcoded switch islands. |
| Downstream consumers | Proof engine, lowering, runtime structural queries, diagnostics publishing. |
| External/tooling consumers | unreachable/unhandled diagnostics, state visualization, available-event precomputation. |

### Target artifact shape

| Fact group | Example contents |
|---|---|
| reachability | initial state, reachable states, unreachable states |
| event topology | rows/hooks available per state, undefined-event map |
| modifier-derived facts | terminal-state legality, required-state dominance, irreversible-back-edge checks |
| runtime indexes | state -> available events, state -> applicable structural buckets |

### Why graph is separate from typing

Typing answers "is this semantically legal?" Graph analysis answers "what lifecycle structure does this legal definition imply?" They are adjacent but not the same problem.

---

## 5. Proof engine

### Stage contract

| Field | Contract |
|---|---|
| Purpose | Discharge statically preventable runtime hazards via bounded abstract reasoning over the typed and graph-resolved program. |
| Exact inputs | `TypedModel` + `GraphResult` + `Operations`, `Functions`, `Types`, `Faults`, and `Diagnostics` catalogs. |
| Output artifact | `ProofModel`. Current source exposes `ProofModel(ImmutableArray<Diagnostic>)`; target shape is an obligation/evidence/disposition artifact. |
| Metadata reflection | Proof requirements come from catalog metadata on operations, accessors, and functions; fault/diagnostic linkage is catalog-owned, not evaluator-invented. |
| Downstream consumers | lowering (`Precept.From`) projects runtime fault residue from it; LS/MCP consume diagnostics/explanations. |
| External/tooling consumers | proof diagnostics, future proof explanations, compile-time hazard reporting. |

### Target artifact shape

| Element | Role |
|---|---|
| `ProofObligation` | a statically preventable requirement introduced by a semantic site |
| `ProofDisposition` | `Proven` / `Unproven` / related terminal status |
| `ProofEvidence` | literal, modifier-derived, guard-derived, path-derived, etc. |
| fault/diagnostic link | bridge from obligation to `FaultCode` and `DiagnosticCode` |
| site identity | semantic origin: operation, function overload, accessor, action, or related descriptor |

### Boundary rule

The proof engine stops at **analysis**. It does **not** build the executable runtime model. That boundary remains with `Precept.From(compilation)`.

---

## 6. CompilationResult

### Contract

```csharp
public sealed record class CompilationResult(
    TokenStream                Tokens,
    SyntaxTree                 SyntaxTree,
    TypedModel                 Model,
    GraphResult                Graph,
    ProofModel                 Proof,
    ImmutableArray<Diagnostic> Diagnostics,
    bool                       HasErrors
);
```

### Role

`CompilationResult` is the immutable analysis snapshot produced by `Compiler.Compile`. It is the entire authoring/tooling truth surface, including on broken input.

### Consumers

| Consumer | Reads |
|---|---|
| LS diagnostics | merged `Diagnostics` |
| LS syntax/structural features | `Tokens` and `SyntaxTree` |
| LS semantic features | primarily `Model`; secondarily `Graph` and `Proof` |
| MCP compile-style tools | `Diagnostics` + typed structure from `Model` |
| Lowering | entire snapshot, gated by `HasErrors == false` |

---

## 7. Lowering / `Precept.From(compilation)`

### Stage contract

| Field | Contract |
|---|---|
| Purpose | Convert an error-free analysis snapshot into an immutable executable model optimized for runtime operations. |
| Exact inputs | `CompilationResult` where `HasErrors == false`; practically this means consuming `Model`, `Graph`, and runtime-relevant residue from `Proof`, plus catalog-backed descriptor metadata. |
| Output artifact | `Precept` executable model. |
| Metadata reflection | Catalog and typed metadata are lowered into descriptors, slot indices, executable actions/expressions, scope-indexed constraint plans, and fault-site descriptors. |
| Downstream consumers | `Create`, `Restore`, `Version` operations, preview/inspection runtime surfaces. |
| External/tooling consumers | preview/inspection only; ordinary LS intelligence should not depend on lowering. |

### Target executable contents

| Executable part | Why it exists |
|---|---|
| field/state/event/arg descriptors | stable public/runtime identity surface |
| lowered expressions | resolved `OperationKind`, `FunctionKind`, accessor metadata, slot-aware evaluation |
| lowered actions | executable action family aligned to typed action family |
| `ExecutableConstraintPlan` | prebucketed rule/ensure evaluation plans |
| `ExecutableFaultPlan` | `FaultSiteDescriptor`-style impossible-path backstops linked to `FaultCode` + `DiagnosticCode` |
| structural indexes | available events, applicable constraints, state topology, initial state/event |

### Critical boundary

`Precept.From(compilation)` is where analysis turns into execution. Neither `GraphResult` nor `ProofModel` should be asked to masquerade as a runtime artifact.

### Current implementation note

`Precept` exists as a stubbed shell. The architecture boundary is already documented correctly in source and docs; the actual lowering implementation remains open.

---

## 8. Runtime execution model

### Runtime types

| Type | Role |
|---|---|
| `Precept` | executable definition shared by all versions of one compiled definition |
| `Version` | immutable entity snapshot at a point in time |
| `Evaluator` | stateless execution engine over `Precept` + operation input |
| outcome unions | `EventOutcome`, `UpdateOutcome`, `RestoreOutcome` |
| inspection records | `EventInspection`, `UpdateInspection`, `RowInspection`, `ConstraintResult`, `FieldSnapshot` |

### Evaluator stage contract

| Field | Contract |
|---|---|
| Purpose | Execute create/restore/fire/update/inspect against the lowered executable model without mutating prior versions. |
| Exact inputs | `Precept` executable model + existing `Version` (when applicable) + operation input dictionaries/descriptors. |
| Output artifact | one result DU (`EventOutcome`, `UpdateOutcome`, `RestoreOutcome`) or inspection artifact, usually carrying a new `Version` on success. |
| Metadata reflection | evaluator dispatches through lowered descriptor-backed plans; failures classify through `Faults.Create(...)`; constraints reference `ConstraintDescriptor`. |
| Downstream consumers | host app code and preview tools pattern-match outcomes. |
| External/tooling consumers | MCP fire/update/inspect; runtime-backed preview. |

### Business outcomes vs faults

| Category | Meaning |
|---|---|
| `Rejected`, `Unmatched`, `InvalidArgs`, constraint failures, access denial | normal authored/runtime outcomes |
| `Fault` | defense-in-depth impossible path that proof should have prevented |

That distinction must stay hard. Runtime faults are not a second validation channel.

---

## 9. Version operations

### Public runtime surface

| Entry | Consumes | Produces | Notes |
|---|---|---|---|
| `Precept.Create(...)` | executable model + initial args | `EventOutcome` | may auto-fire initial event |
| `Precept.InspectCreate(...)` | executable model + optional args | `EventInspection` | preview construction path |
| `Precept.Restore(...)` | persisted state/data | `RestoreOutcome` | validated reconstruction, bypasses access modes |
| `Version.Fire(...)` | current version + event args | `EventOutcome` | event pipeline |
| `Version.Update(...)` | current version + field patch | `UpdateOutcome` | atomic patch |
| `Version.InspectFire(...)` | version + optional args | `EventInspection` | non-committing event preview |
| `Version.InspectUpdate(...)` | version + optional patch | `UpdateInspection` | non-committing update preview |

### Runtime operation order

| Operation | Core order |
|---|---|
| `Create` | build initial working version -> optional initial event -> compute/recompute -> constraints -> commit outcome |
| `Restore` | validate state/fields -> recompute -> constraints -> `Restored` or failure |
| `Fire` | validate args -> route rows/hooks -> execute actions -> recompute -> constraints -> outcome |
| `Update` | validate patch/access -> apply working copy -> recompute -> constraints -> outcome |
| `Inspect*` | same execution path as commit, but discard working copy and return annotated landscape |

### Design point

Inspection and commit must share one execution contract. Preview is not a toy reimplementation.

---

## 10. Language server consumption model

### The correct design split

| LS feature | Primary artifact | Why |
|---|---|---|
| diagnostics | `CompilationResult.Diagnostics` | merged stage truth |
| semantic-token baseline | `TokenStream` + `TokenMeta.SemanticTokenType` | token categories are known lexically |
| syntax-aware recovery / outline | `SyntaxTree` | source structure and spans |
| completions | mostly `TypedModel`, with token/construct metadata assist | needs scope + semantic context |
| hover | `TypedModel` + catalogs | resolved symbol/type/operation docs |
| go-to-definition | `TypedModel` linked back to syntax spans | semantic identity with source location |
| preview/inspect | `Precept` + runtime inspection results | requires executable model |

### Tokens vs SyntaxTree vs TypedModel vs runtime surface

| Artifact | LS should use it for | LS should not use it for |
|---|---|---|
| `TokenStream` | token classification, lightweight lexical context, grammar-adjacent highlighting | semantic resolution |
| `SyntaxTree` | source structure, broken-file recovery, exact spans | final type or symbol truth |
| `TypedModel` | completions, hover, definition, semantic understanding | source-fidelity tasks that need recovery nodes |
| `Precept` | runtime preview only | ordinary authoring intelligence |

### Current implementation reality

The current `tools\Precept.LanguageServer` project is a bare server shell (`Program.cs`) and does **not** yet consume `CompilationResult` or `Precept`. So the model above is an explicit design contract, not a description of already-shipped LS behavior.

That difference must remain visible. We do not get to hand-wave "the LS uses the model" when the source currently does not.

---

## 11. External consumer map

| Consumer | Artifact boundary |
|---|---|
| MCP compile-style surfaces | `CompilationResult` |
| MCP execution-preview surfaces | `Precept` / `Version` |
| TextMate grammar generation | catalogs, primarily `Tokens` / `Types` / `Constructs` |
| LS semantic tokens | token metadata first; typed resolution second where identifiers must be classified semantically |
| Host application runtime | `Precept` + `Version` outcomes |
| Design/docs generation | catalogs first, stage artifacts second |

---

## 12. Architectural decisions locked by this draft

1. **The compiler is a five-stage analysis pipeline terminating at `ProofModel`.**
2. **`CompilationResult` is the analysis snapshot and tooling contract.**
3. **`Precept.From(compilation)` is the lowering boundary and executable-model constructor.**
4. **`SyntaxTree` and `TypedModel` both remain first-class artifacts because they serve different architectural responsibilities.**
5. **The language server is a `CompilationResult` consumer for intelligence and a `Precept` consumer only for preview.**
6. **Runtime faults remain defense-in-depth impossible-path signals, never ordinary business outcomes.**
7. **Catalog metadata enters every stage and must never be duplicated by downstream consumer logic.**

This is the honest compiler/runtime architecture for Precept: metadata first, analysis complete, lowering explicit, runtime executable, and every surface reading the artifact that matches its job.
