# Type Checker Design

Date: 2026-04-19

Status: **Implemented** — 6-partial-class architecture (3,783 LOC) landed in PR #123 (issue #118). All validation phases operational: type inference, narrowing, proof-backed assessments, field constraints, computed fields, collection mutations. Diagnostic surface covers C1–C99 constraint codes. Research-backed architecture review confirmed alignment with Kotlin K2, Roslyn Binder, and F# Checking precedents.

> **Research grounding:** [typechecker-architecture-survey-frank.md](../research/architecture/typechecker-architecture-survey-frank.md) (6 production type checkers) and [typechecker-implementation-patterns-george.md](../research/architecture/typechecker-implementation-patterns-george.md) (.NET implementation patterns). Combined verdict: **KEEP AS-IS.**

---

## Overview

`PreceptTypeChecker` is Precept's compile-time validation layer. It validates the entire `.precept` definition — type inference, null-flow narrowing, field constraints, expression validity, computed field dependency analysis, transition row type checking, rule validation, collection mutation verification — and produces diagnostics (the C1–C99 constraint codes). The type checker runs before any entity instance exists, as a stage of `PreceptCompiler.Validate()`.

The type checker is **not** part of the proof engine. The proof engine (`ProofContext`, `LinearForm`, `RelationalGraph`, documented in [ProofEngineDesign.md](ProofEngineDesign.md)) is a separate component that the type checker consults at specific integration points for interval inference and proof-backed assessments. The type checker owns the diagnostic surface; the proof engine provides the reasoning primitives.

### What the Type Checker Validates

| Validation Domain | Entry Method | Constraint Codes |
|---|---|---|
| Transition row assignments, guards, choice membership | `ValidateTransitionRows` | C38–C47, C56, C65–C69, C92–C94, C97–C98 |
| State action assignments and collection mutations | `ValidateStateActions` | C38–C43, C68, C94 |
| Field-level constraints (type compat, duplicates, defaults, choice) | `ValidateFieldConstraints` | C57–C64, C66 |
| Rules (unconditional and guarded), ensures, edit guards | `ValidateRules` | C38–C42, C46, C69, C95–C96 |
| Computed field expressions, dependency cycles, scope restrictions | `ValidateComputedFields` | C83–C88 |
| Collection mutations (add/remove/enqueue/push/dequeue/pop) | `ValidateCollectionMutations` | C43, C68 |
| Expression type inference (literals, identifiers, binary, unary, functions, conditionals) | `ValidateExpression` → `TryInferKind` | C38–C42, C56, C60, C65, C67, C71–C79, C92–C93 |
| Guard narrowing (null checks, numeric comparisons) | `ApplyNarrowing` | (Refines proof context; no direct diagnostics) |
| Proof-backed assessments (divisor safety, sqrt args, dead/vacuous guards) | `AssessDivisorSafety`, `AssessNonnegativeArgument`, `AssessGuard` | C76, C92–C98 |

### Architecture Summary

The type checker is organized as a single `internal static partial class` split across 6 files by concern:

- **Main** — orchestration, transition/state/rule validation, computed field analysis
- **TypeInference** — expression kind resolution, function overload matching, binary operator type checking
- **Narrowing** — proof context refinement from guards, assignments, ensures
- **ProofChecks** — interval inference, proof-backed assessment generation
- **Helpers** — stateless utilities (mapping, assignability, formatting, copy helpers)
- **FieldConstraints** — self-contained field/arg constraint validation

The single entry point `Check(PreceptDefinition model)` returns a `TypeCheckResult` containing diagnostics, a `PreceptTypeContext` (resolved types per expression position), computed field evaluation order, and the global proof context.

### Properties

- **Compile-time.** The type checker validates the definition structure before any entity instance is constructed. Invalid configurations are caught structurally, not at runtime.
- **Single-pass.** `Check()` makes one forward walk through the definition model. No iterative fixpoint computation.
- **Deterministic.** Same definition produces the same diagnostics in the same order.
- **Stateless.** All methods are `static`. No mutable instance state, no context chain, no ambient dependencies.
- **Complete for the file.** All proof facts — field constraints, rules, guards, ensures — derive from the `.precept` definition. No external oracle.

---

## Philosophy-Rooted Design Principles

The following principles govern the type checker's design and are grounded in Precept's core philosophy (see [philosophy.md](philosophy.md)).

1. **Compile-time prevention, not runtime detection.** The type checker validates the definition before any instance exists. This is the direct realization of the philosophy's prevention commitment: "Invalid entity configurations — combinations of lifecycle position and field values that violate declared rules — cannot exist. They are structurally prevented before any change is committed." The type checker is the mechanism that makes this structural.

2. **Single-pass validation follows from the flat execution model.** Precept has no loops, no control-flow branches, and no reconverging flow. This means the type checker can validate every expression, every assignment, and every guard in a single forward pass — no fixpoint computation, no widening, no lattice joins. The single-pass property is a structural consequence of the DSL's design, not an engineering tradeoff.

3. **Deterministic diagnostics.** Same definition, same diagnostics. The type checker uses no non-deterministic solvers and no timing-dependent analysis. This traces to the philosophy's determinism commitment: "The engine is deterministic — same definition, same data, same outcome."

4. **One file, complete type information.** All type information needed for validation — field types, event argument types, constraints, rules, ensures — comes from the `.precept` definition. No external type database, no cross-file references, no ambient configuration. This traces to the philosophy's one-file completeness commitment: "Every field, rule, ensure, and transition lives in the `.precept` definition."

5. **Inspectability through diagnostics.** Every type error, every proof-backed warning, every constraint violation surfaces as a structured diagnostic with a constraint code, a human-readable message, line/column position, and optional proof assessment. These diagnostics flow through the language server (editor squiggles), MCP `precept_compile` (AI agent consumption), and the compilation pipeline. This traces to the philosophy's inspectability commitment: "Nothing is hidden."

6. **Proof-backed diagnostics, not pattern-matched heuristics.** Proof-backed diagnostics (C92–C98) are classified by proof outcome — contradiction, obligation, satisfied — not by syntax shape. The type checker delegates to the proof engine's `IntervalOf` composing query and renders the assessment model, rather than pattern-matching specific expression forms. This follows from ProofEngineDesign.md § Principle 8 (truth-based diagnostic classification).

---

## Research Foundations

The type checker's architecture is grounded in a two-part research survey committed to the repository:

### Production Type Checker Survey (Frank)

[typechecker-architecture-survey-frank.md](../research/architecture/typechecker-architecture-survey-frank.md) surveyed 6 major production type checkers:

| System | Organization | Key Pattern | Size Precedent |
|---|---|---|---|
| **Roslyn (C#)** | 50+ partial `Binder_*.cs` files | Visitor + switch-on-SyntaxKind dispatch; concern-named partials | Binder.cs ~2K LOC; Binder_Expressions.cs ~12K LOC |
| **TypeScript** | Monolithic `checker.ts` | Single 50K+ LOC file; locality over modularity | Viable to ~50K LOC |
| **Rust** | Separate crates per phase | Phase-based: HIR → type check → inference → borrow check | Per-crate separation |
| **Swift** | 100+ `.cpp` files in Sema module | File-per-concern at fine grain; CSGen/CSSimplify/CSSolver | ~2–3K LOC per file |
| **Kotlin K2 (FIR)** | Explicit phases: RAW → TYPES → CHECKERS | Phase-backed modularization; deferred diagnostics | 30+ checkers, 500–2K LOC each |
| **F# Compiler** | 40+ `.fs` files in Checking/ | Domain-specific modules with `.fsi` interfaces | ConstraintSolver ~2.5K LOC |

**Key finding:** Precept's 6-partial split aligns most closely with **Kotlin K2's phase model** (implicit phases: narrowing → proof → inference) and **Roslyn's concern-named partial pattern** (Binder_*.cs). The horizontal layering by concern, not by syntax kind, is a deliberate architectural choice validated by Kotlin K2 precedent.

### .NET Implementation Patterns (George)

[typechecker-implementation-patterns-george.md](../research/architecture/typechecker-implementation-patterns-george.md) surveyed Roslyn Binder, F# Checking, NRules, and DynamicExpresso for C#/.NET-specific patterns:

**Key findings:**
- Partial-class naming convention (`PreceptTypeChecker.<Concern>.cs`) matches Roslyn's `Binder_<Concern>.cs` shape. ✓
- All 6 files are well below the per-file ceiling observed in Roslyn/Swift/F# (max 1,260 LOC vs. 12K LOC in Roslyn). ✓
- Static partial class is unusual vs. Roslyn's instance partials, but justified by stateless validation — no context chain, no instance state. ✓
- Switch-on-NodeKind dispatch matches Roslyn's industry-standard approach (not visitor pattern). ✓
- Centralized Helpers is the one minor divergence from Roslyn (which distributes helpers near consumers). Defensible at current scale. 🟡

**Combined verdict:** KEEP AS-IS. The 6-partial split is well-named, well-sized, well-organized, and well-dispatched.

---

## Execution Model Assumptions

The type checker's tractability rests on structural properties of Precept's execution model that eliminate the complexity found in general-purpose type systems:

1. **Closed AST vocabulary.** The expression node types (`PreceptLiteralExpression`, `PreceptIdentifierExpression`, `PreceptBinaryExpression`, `PreceptUnaryExpression`, `PreceptFunctionCallExpression`, `PreceptConditionalExpression`, `PreceptParenthesizedExpression`) are fully owned by us. No third-party extension, no open type hierarchy. This makes switch-on-NodeKind dispatch correct and complete.

2. **Flat execution model.** No loops, no control-flow branches, no reconverging flow. A transition row is a flat sequence: evaluate guard → execute assignments left-to-right → check rules/ensures. This means type information flows linearly — no dataflow graph, no phi nodes, no join points.

3. **No generics or overloads.** The DSL has a fixed set of scalar types (`string`, `number`, `integer`, `decimal`, `boolean`, `choice`) and collection types (`set<T>`, `queue<T>`, `stack<T>`). No user-defined types, no parametric polymorphism, no overload resolution beyond built-in function dispatch via `FunctionRegistry`.

4. **Finite state space.** States, events, fields, and rules are declared statically. The type checker enumerates all transition rows, state actions, and rules exhaustively — no symbolic execution, no abstract interpretation over unbounded domains.

5. **No separate compilation.** Each `.precept` file is self-contained. No imports, no cross-file references, no incremental compilation. The type checker processes the entire definition model in one call.

---

## Architecture

### 6-Partial Decomposition

| File | LOC | Responsibility | Fan-In From | Calls Into |
|---|---:|---|---|---|
| [PreceptTypeChecker.cs](../src/Precept/Dsl/PreceptTypeChecker.cs) (Main) | 1,260 | Front-matter types, `Check()` entry point, transition/state/rule/computed field validation, collection mutations | PreceptCompiler, language server, tests | TypeInference, Narrowing, ProofChecks, Helpers, FieldConstraints |
| [PreceptTypeChecker.TypeInference.cs](../src/Precept/Dsl/PreceptTypeChecker.TypeInference.cs) | 762 | `ValidateExpression`, `TryInferKind`, `TryInferFunctionCallKind`, `TryInferBinaryKind` | Main (highest fan-in) | Narrowing (`ApplyNarrowing`), ProofChecks (`AssessDivisorSafety`, `AssessNonnegativeArgument`), Helpers |
| [PreceptTypeChecker.Narrowing.cs](../src/Precept/Dsl/PreceptTypeChecker.Narrowing.cs) | 606 | `BuildEventEnsureSymbols`, `BuildStateEnsureNarrowings`, `BuildEventEnsureNarrowings`, `ApplyNarrowing`, `ApplyAssignmentNarrowing`, `TryApplyNullComparisonNarrowing`, `TryApplyNumericComparisonNarrowing`, `TryStoreLinearFact`, `TryDecomposeNullOrPattern` | Main, TypeInference | Helpers, ProofChecks (`FlipComparisonOperator`), ProofContext (via `IntervalOf` for compound RHS) |
| [PreceptTypeChecker.ProofChecks.cs](../src/Precept/Dsl/PreceptTypeChecker.ProofChecks.cs) | 416 | `ExtractFieldInterval`, `TryInferInterval`, `AssessDivisorSafety`, `AssessNonnegativeArgument`, `AssessGuard`, `TryExtractSingleFieldComparison`, `FlipComparisonOperator`, `DescribeExpression`, `TryGetNumericLiteral` | TypeInference, Main, Narrowing | Helpers, ProofContext (`IntervalOf`, `KnowsNonnegative`) |
| [PreceptTypeChecker.Helpers.cs](../src/Precept/Dsl/PreceptTypeChecker.Helpers.cs) | 398 | 29 methods: mapping (`MapFieldContractKind`, `MapScalarType`, `MapKind`), assignability (`IsAssignable`, `NormalizeChoiceKind`), kind predicates (`HasFlag`, `IsExactly`, `IsNumericKind`), formatting (`FormatKinds`, `KindLabel`, `BuildC60Message`, `BuildC79Message`), copy helpers (`CopyRelationalFacts`, `CopyFieldIntervals`, `CopyFlags`, `CopyExprFacts`), symbol builders (`ExpandRowStates`, `BuildSymbolKinds`) | Main, TypeInference, Narrowing, ProofChecks | (leaf — no outgoing calls to other partials) |
| [PreceptTypeChecker.FieldConstraints.cs](../src/Precept/Dsl/PreceptTypeChecker.FieldConstraints.cs) | 341 | `ValidateFieldConstraints`, `ValidateChoiceField`, `ValidateConstraintTypes`, `ValidateConstraintDuplicates`, `ValidateConstraintDefault`, `ConstraintKindKey`, `ConstraintLabel` | Main (single caller) | (self-contained — no dependencies on Narrowing or ProofChecks) |

### Front-Matter Type Inventory

The following types are defined in the Main partial file, following the standard .NET convention of placing supporting types adjacent to the primary type:

| Type | Purpose |
|---|---|
| `StaticValueKind` (flags enum) | Represents inferred expression types. Flags composition supports nullable types (`String \| Null`) and multi-kind inference. Includes `OrderedChoice` / `UnorderedChoice` for choice field type tracking. |
| `PreceptValidationDiagnostic` (record) | A single diagnostic with constraint reference, message, line/column, optional state context, and optional `ProofAssessment`. |
| `PreceptValidationDiagnosticFactory` (static class) | Factory methods for creating diagnostics from expressions (`FromExpression`) or column ranges (`FromColumns`). |
| `PreceptTypeExpressionInfo` (record) | Resolved type for a single expression at a specific line, scope, and state/event context. |
| `PreceptTypeScopeInfo` (record) | Symbol table snapshot at a scope boundary (transition-base, when, transition-actions, state-action, data-rules, event-ensure). |
| `PreceptTypeContext` (class) | Collection of expression infos and scope infos. Provides `FindBestScope()` for line-based scope resolution (used by language server completions). |
| `TypeCheckResult` (record) | Return value from `Check()`: diagnostics, type context, computed field order, global proof context. |
| `ValidationResult` (record) | Return value from `PreceptCompiler.Validate()`: diagnostics, type context, validated model, proof context. |

### Dispatch Model

The type checker uses **switch-on-NodeKind dispatch**, matching the Roslyn Binder's industry-standard approach. The core dispatch site is `TryInferKind` in the TypeInference partial:

```
TryInferKind(expression, context, out kind, out diagnostic)
  switch (expression)
    PreceptLiteralExpression     → MapLiteralKind
    PreceptIdentifierExpression  → symbol table lookup + C56 nullable .length check
    PreceptParenthesizedExpression → recurse into inner
    PreceptUnaryExpression       → operator-specific (not, -)
    PreceptBinaryExpression      → TryInferBinaryKind (further switch on operator)
    PreceptFunctionCallExpression → TryInferFunctionCallKind (FunctionRegistry lookup + overload resolution)
    PreceptConditionalExpression → condition + branch type unification + narrowing
    default                      → C39 unsupported expression
```

This is appropriate because the AST vocabulary is closed and fully owned — no extension points needed. Visitor pattern would add ceremony without benefit (confirmed by research: NRules uses visitor for its open rule vocabulary; Roslyn and Precept both use switch for their closed AST).

### Fan-In Graph

```
Helpers ←────── Main
    ↑           ↑  ↑  ↑
    ├── TypeInference  │  │
    ├── Narrowing ─────┘  │
    └── ProofChecks ──────┘

TypeInference ──→ Narrowing (ApplyNarrowing for and/or short-circuit narrowing)
TypeInference ──→ ProofChecks (AssessDivisorSafety, AssessNonnegativeArgument)
Narrowing ──→ ProofChecks (FlipComparisonOperator)
Main ──→ Narrowing (BuildStateEnsureNarrowings, BuildEventEnsureNarrowings, ApplyNarrowing, ApplyAssignmentNarrowing)
Main ──→ ProofChecks (AssessGuard, TryExtractSingleFieldComparison)
Main ──→ FieldConstraints (ValidateFieldConstraints)
Main ──→ TypeInference (ValidateExpression)
```

Helpers is the leaf — Main, TypeInference, Narrowing, and ProofChecks all depend on it; it depends on none. FieldConstraints is self-contained — called only from Main, no dependencies on Narrowing, ProofChecks, or Helpers. TypeInference has the highest fan-in: called from Main for every expression validation.

---

## Decomposition Rationale

### Why These 6 Seams

The decomposition separates by **concern**, not by **syntax kind**. This is horizontal layering — each partial handles a different analysis phase or domain — influenced by the Kotlin K2 phase model rather than Roslyn's per-construct split.

| Seam | Rationale | Precedent |
|---|---|---|
| **Main (orchestration)** | Central coordination of validation phases; houses the entry point and all domain-specific validation loops (transitions, states, rules, computed fields). Roslyn's `Binder.cs` serves the same role. | Roslyn `Binder.cs` (1,009 LOC) |
| **TypeInference (expression types)** | Expression kind resolution is the highest-fan-in concern — called from every validation domain. Isolating it prevents the Main file from growing unboundedly. | Kotlin K2 TYPES phase; Rust `rustc_infer` |
| **Narrowing (proof refinement)** | Guard narrowing and assignment narrowing are a coherent phase that refines the proof context before downstream consumers use it. Separating this from TypeInference keeps the inference logic pure (type resolution) vs. proof-state mutation. | Kotlin K2 SUPER_TYPES phase; F# `TypeRelations.fs` |
| **ProofChecks (proof-backed assessments)** | Distinct from Narrowing: Narrowing *refines* the proof context; ProofChecks *queries* it for diagnostic purposes. The assessment model (C92–C98) is a self-contained concern. | Kotlin K2 CHECKERS phase; F# `ConstraintSolver.fs` |
| **Helpers (cross-cutting utilities)** | 29 stateless methods used across all partials: mapping, assignability, formatting, copy helpers. Centralizing avoids duplication. F# has an analogous `TypeRelations.fs` for cross-cutting helpers. | F# `TypeRelations.fs`; common in all surveyed systems |
| **FieldConstraints (field/arg validation)** | Self-contained domain: validates constraint suffixes (type compatibility, duplicates, contradiction, defaults, choice metadata). No dependencies on Narrowing or ProofChecks. Runs before rule validation so errors are attributed to constraints, not synthetic rules. | Swift `TypeCheckAttr.cpp` |

### Alternatives Considered

1. **Monolithic single file.** TypeScript's `checker.ts` proves monolithic works at 50K+ LOC. At 3,783 LOC, a single file would be manageable but would sacrifice the concern-based navigation and implicit phase boundaries. Rejected: the 6-file split is already in place and provides clearer code organization without indirection cost.

2. **Per-syntax-kind split** (one file per expression type, one for transitions, one for rules). This is Roslyn's approach at scale (50+ files). At our size, this would produce many small files (~200–400 LOC each) with high cross-file coupling. Rejected: premature for our scale; revisit if any file exceeds 2K LOC.

3. **Visitor pattern.** NRules uses visitor for its open rule vocabulary. Precept's AST is closed and fully owned — visitor would add virtual dispatch overhead and ceremony without enabling any extension scenario. Rejected: switch-on-NodeKind is simpler, faster, and complete for our closed vocabulary.

---

## Integration Points

### Proof Engine Consultation Sites

The type checker consults the proof engine at these call sites:

| Call Site | File | Method Called | Purpose |
|---|---|---|---|
| Transition row assignment validation | Main (`ValidateTransitionRows`) | `context.IntervalOf(assignment.Expression)` | C94: detect assignment provably outside constraint range |
| State action assignment validation | Main (`ValidateStateActions`) | `context.IntervalOf(assignment.Expression)` | C94: same as above for state actions |
| Division/modulo type checking | TypeInference (`TryInferBinaryKind`) | `AssessDivisorSafety` → `context.IntervalOf(divisor)` | C92/C93: divisor safety |
| sqrt argument validation | TypeInference (`TryInferFunctionCallKind`) | `AssessNonnegativeArgument` → `context.IntervalOf(arg)` | C76: sqrt non-negative proof |
| Guard dead/tautology detection | Main (`ValidateTransitionRows`) | `AssessGuard` → `NumericInterval.AreDisjoint` / `Contains` | C97/C98: dead/vacuous guards |
| Rule contradiction/vacuity | Main (`ValidateRules`) | `TryExtractSingleFieldComparison` → interval disjointness | C95/C96: contradictory/vacuous rules |
| Compound RHS interval inference | Narrowing (`ApplyAssignmentNarrowing`) | `context.IntervalOf(rhs)` | Thread post-assignment proof state |

### Language Server Consumption Sites

| Consumer | File | What It Consumes |
|---|---|---|
| Diagnostics (squiggles) | [PreceptAnalyzer.cs](../tools/Precept.LanguageServer/PreceptAnalyzer.cs) | `PreceptTypeChecker.Check(model)` → `TypeCheckResult.Diagnostics` mapped to LSP `Diagnostic[]` |
| Completions (scope-aware symbols) | [PreceptAnalyzer.cs](../tools/Precept.LanguageServer/PreceptAnalyzer.cs) | `TypeCheckResult.TypeContext` → `FindBestScope()` for line/state/event-scoped symbol lists |
| Hover (field types, proof info) | [PreceptDocumentIntellisense.cs](../tools/Precept.LanguageServer/PreceptDocumentIntellisense.cs) | `PreceptTypeChecker.Check(model)` → type context and proof context for hover rendering |

### MCP Tool Consumption Sites

| Tool | File | Integration |
|---|---|---|
| `precept_compile` | [CompileTool.cs](../tools/Precept.Mcp/Tools/CompileTool.cs) | `PreceptCompiler.CompileFromText` → `Validate` → `Check` → diagnostics projected to JSON |
| `precept_fire` | [FireTool.cs](../tools/Precept.Mcp/Tools/FireTool.cs) | Same pipeline; type check errors block engine construction |
| `precept_inspect` | [InspectTool.cs](../tools/Precept.Mcp/Tools/InspectTool.cs) | Same pipeline |
| `precept_update` | [UpdateTool.cs](../tools/Precept.Mcp/Tools/UpdateTool.cs) | Same pipeline |

### Compilation Pipeline Caller

| Caller | File | Integration |
|---|---|---|
| `PreceptCompiler.Validate` | [PreceptRuntime.cs](../src/Precept/Dsl/PreceptRuntime.cs) (line ~2160) | Calls `PreceptTypeChecker.Check(model)`, merges diagnostics, threads computed field order onto model, feeds dead-guard lines to `PreceptAnalysis.Analyze` |
| `PreceptCompiler.Compile` | Same file | Calls `Validate` → `Check` → throws on errors |
| `PreceptCompiler.CompileFromText` | Same file | `Parse` → `Validate` → `Check` → returns `CompileFromTextResult` with model, engine (if no errors), and diagnostics |

---

## Diagnostic Surface

### Diagnostic Production

The type checker produces diagnostics through two mechanisms:

1. **Direct diagnostic creation.** Most validation methods construct `PreceptValidationDiagnostic` instances inline, referencing `DiagnosticCatalog.CXX` for the constraint definition (code, severity, message template).

2. **Assessment-model diagnostics.** Proof-backed diagnostics (C92–C98) route through the shared assessment model: the type checker calls an assessment method (e.g., `AssessDivisorSafety`), receives a `ProofAssessment` with a `ProofOutcome` classification, and renders it via `ProofDiagnosticRenderer.Render()`. The assessment is attached to the diagnostic for structured consumption by hover and MCP.

### Diagnostic Factory

`PreceptValidationDiagnosticFactory` provides two factory methods:

- `FromExpression(constraint, message, line, expression, ...)` — extracts column span from the expression's `Position` property.
- `FromColumns(constraint, message, line, startColumn, endColumn, ...)` — uses explicit column range.

Both produce `PreceptValidationDiagnostic` records carrying the constraint reference, message, position, optional state context, and optional `ProofAssessment`.

### Assessment Model Integration

For proof-backed diagnostics, the flow is:

```
TypeInference/Main → AssessDivisorSafety / AssessNonnegativeArgument / AssessGuard
    → ProofContext.IntervalOf(expr) → ProofResult(interval, attribution)
    → ProofAssessment(requirement, outcome, subject, interval, attribution, ...)
    → ProofDiagnosticRenderer.Render(assessment) → human-readable message
    → PreceptValidationDiagnostic(constraint, message, line, ..., Assessment: assessment)
```

The `ProofAssessment` is the contract center: diagnostics, hover, and MCP all consume it directly. Message text is a rendering of the assessment, not a contract. See [ProofEngineDesign.md](ProofEngineDesign.md) § Principle 11.

---

## Design Decisions

### DD1: Static Partial Class (Not Instance, Not Visitor)

**Decision:** `PreceptTypeChecker` is `internal static partial class` with all methods static.

**Rationale:** The type checker is stateless validation — no context chain, no instance state to carry, no ambient dependencies. Every method takes its inputs as parameters and returns results. Static methods communicate this: there is no object lifecycle, no construction cost, no disposal concern.

**Alternatives rejected:**
- *Instance partial class* (Roslyn pattern): Roslyn's Binder chains parent → child binders for scope management. Precept's scope management is explicit (symbol dictionaries passed as parameters), not implicit (inherited state). Instance partials would add construction overhead for no benefit.
- *Visitor pattern* (NRules pattern): The AST vocabulary is closed and fully owned. Visitor would add virtual dispatch indirection and force all validation logic into Visit methods with uniform signatures, losing the ability to have different return types per validation domain.

**Tradeoff accepted:** Static methods cannot be overridden or mocked in tests. This is acceptable because the type checker is tested through its public entry point (`Check`) with real definitions, not through isolated method mocking.

### DD2: Horizontal Layering by Concern (Not by Syntax Kind)

**Decision:** The 6 partials separate by analysis concern (type inference, narrowing, proof checks, field constraints, helpers, orchestration), not by syntax construct (one file per expression type, one for transitions, one for rules).

**Rationale:** The validation logic for any single construct (e.g., a transition row) touches multiple concerns: type inference for assignment RHS, narrowing for guard context, proof checks for divisor safety, helpers for symbol building. Splitting by syntax kind would scatter these tightly coupled calls across files. Horizontal layering keeps each analysis domain cohesive.

**Precedent:** Kotlin K2's phase model (RAW → TYPES → CHECKERS) separates by analysis phase, not by syntax kind. Precept's partials implicitly respect this: Narrowing ≈ TYPES, ProofChecks ≈ CHECKERS, TypeInference ≈ resolve.

**Alternatives rejected:**
- *Per-syntax-kind split*: At our scale (3,783 LOC), this would produce 10+ files of 200–400 LOC each with high cross-file coupling. Premature for the current codebase size.

**Tradeoff accepted:** The Main file (1,260 LOC) handles multiple validation domains (transitions, states, rules, computed fields). If it grows past 2K LOC, extraction into domain-specific partials would be appropriate.

### DD3: Switch-on-NodeKind Dispatch (Not Visitor Pattern)

**Decision:** `TryInferKind` dispatches on expression node type via C# pattern matching (`switch (expression) { case PreceptLiteralExpression ... }`).

**Rationale:** The AST node types are a closed, finite set owned entirely by the Precept parser. Switch dispatch gives: exhaustiveness checking via the `default` branch, direct access to node-specific properties without casting, and the ability to return different types from different branches.

**Precedent:** Roslyn's Binder uses switch-on-SyntaxKind for the same reason. Confirmed by both research surveys.

**Alternatives rejected:**
- *Visitor pattern*: Would require a visitor interface with one Visit method per node type, a base visitor class, and virtual dispatch. Adds ceremony without enabling any extension scenario — we own the AST, and we don't need external extension points.

**Tradeoff accepted:** Adding a new expression node type requires updating every switch site. This is intentional — the compiler enforces that all dispatch sites handle the new node, preventing silent omissions.

### DD4: Centralized Helpers (Single Helpers Partial)

**Decision:** All 29 stateless utility methods live in a single `Helpers.cs` partial.

**Rationale:** These methods (mapping, assignability, formatting, copy helpers) are genuinely cross-cutting — used from Main, TypeInference, Narrowing, and ProofChecks. Centralizing them avoids duplication and provides a single location for contributors to find utility methods.

**Precedent:** F#'s `TypeRelations.fs` plays the same role. Roslyn distributes helpers near consumers — at 50+ files, this prevents navigating to a single large utility file. At our scale (6 files, 398 LOC for Helpers), centralization is defensible.

**Alternatives rejected:**
- *Distribute near consumers* (Roslyn pattern): At 398 LOC and 6 consumer files, the navigation cost of distribution exceeds the coupling cost of centralization. Revisit if Helpers grows past 600 LOC.

**Tradeoff accepted:** Contributors must know to look in Helpers for utility methods. The file header comment documents this convention.

### DD5: Front-Matter Types in Main File

**Decision:** `StaticValueKind`, `PreceptValidationDiagnostic`, `PreceptValidationDiagnosticFactory`, `PreceptTypeExpressionInfo`, `PreceptTypeScopeInfo`, `PreceptTypeContext`, `TypeCheckResult`, and `ValidationResult` are defined in the Main partial file before the class body.

**Rationale:** Standard .NET convention — supporting types adjacent to the primary type they support. Contributors opening the main file see the type vocabulary first, then the entry point.

**Precedent:** Common across Roslyn, Entity Framework, and .NET runtime code. The main file of a partial class hosts the types that define the class's contract.

**Tradeoff accepted:** The Main file's LOC count includes ~110 lines of front-matter types. This inflates the apparent size but provides immediate context for readers.

### DD6: Proof Engine as Separate Component, Consulted at Integration Points

**Decision:** The proof engine (`ProofContext`, `LinearForm`, `RelationalGraph`) is a separate set of types, not embedded in the type checker. The type checker consults it at specific call sites via `IntervalOf` and `KnowsNonnegative`, plus direct use of proof-engine types (`NumericInterval.AreDisjoint`, `NumericInterval.Contains`, `LinearForm`).

**Rationale:** The proof engine's concerns (interval arithmetic, relational closure, fact storage) are orthogonal to type checking concerns (kind inference, assignability, scope resolution). Embedding proof logic in the type checker would violate single responsibility and make the proof engine untestable in isolation.

**Precedent:** F# separates `ConstraintSolver.fs` from type checking. Rust separates `rustc_infer` from `rustc_hir_typeck`.

**Tradeoff accepted:** Integration points between the type checker and proof engine must be maintained as both components evolve. The call sites are documented in § Integration Points above. Note: [ProofEngineDesign.md](ProofEngineDesign.md) counts 5 `IntervalOf` consultation sites specifically; this document's § Integration Points table counts all proof-engine integration points including interval comparison utilities and flag queries, yielding the broader tally of 7.

### DD7: Narrowing and ProofChecks as Separate Partials (Not Merged)

**Decision:** `Narrowing.cs` (606 LOC) and `ProofChecks.cs` (416 LOC) are separate partials despite both interacting with the proof context.

**Rationale:** They serve different roles: Narrowing *refines* the proof context (copy-on-write updates to symbol tables, intervals, flags, relational facts), while ProofChecks *queries* the proof context for diagnostic purposes (assessment generation). Merging them would conflate mutation-oriented logic with query-oriented logic in a single file.

**Precedent:** Kotlin K2 separates the resolution phases (which refine type information) from the checker phases (which query it for diagnostics).

**Tradeoff accepted:** Some methods could arguably live in either file (e.g., `FlipComparisonOperator` is used by both). The boundary is the *intent*: refining proof state vs. generating assessments.

---

## Re-evaluation Triggers

The following conditions, identified in the research surveys, would warrant revisiting this architecture:

| Trigger | Threshold | Action |
|---|---|---|
| Any single file exceeds 2K LOC | Main is currently 1,260 | Extract domain-specific partials (e.g., `Transitions.cs`, `Rules.cs`, `ComputedFields.cs`) |
| Total type checker exceeds 5K LOC | Currently 3,783 | Consider formalizing implicit phases as explicit pass functions |
| New analysis domain added | e.g., incremental compilation, cross-file analysis | Evaluate whether a new partial or a separate type is appropriate |
| Phase formalization needed | IDE responsiveness requirements, incremental re-narrowing | Adopt explicit phase model (Kotlin K2 pattern): define phase contracts, defer diagnostics to final pass |
| Helpers exceeds 600 LOC | Currently 398 | Distribute domain-specific helpers to consumer files |

---

## Test Obligations

The type checker is tested through these xUnit + FluentAssertions test suites:

| Test File | Coverage Domain |
|---|---|
| `PreceptTypeCheckerTests.cs` | Core type checking: kind inference, assignability, operator validation, diagnostic codes, scope resolution, computed fields |
| `ConditionalExpressionTests.cs` | `if/then/else` expression type inference, branch unification, narrowing through conditions |
| `ProofContextTests.cs` | ProofContext type tests (interval store, flag store, IntervalOf) |
| `ProofContextScopeTests.cs` | Per-event proof scoping, ensure narrowing propagation |
| `ProofEngineComputedFieldTests.cs` | Computed field proof safety (C83–C88 diagnostics) |
| `ProofEngineCompoundDivisorTests.cs` | Compound divisor interval inference (C92/C93 with expressions) |
| `ProofEngineConstraintTests.cs` | Constraint-vs-rule interaction (C94–C96) |
| `ProofEngineSoundnessInvariantTests.cs` | Soundness invariants: no false positives across all proof paths |
| `ProofEngineTransitiveClosureTests.cs` | RelationalGraph BFS closure, fact derivation |
| `ProofEngineSumOnRhsTests.cs` | Sum-on-RHS assignment narrowing correctness |
| `ProofEngineUnsupportedPatternTests.cs` | Graceful degradation for expressions outside proof coverage |

All tests exercise the type checker through `PreceptTypeChecker.Check()` with complete `.precept` definitions — integration tests, not unit tests of individual methods. This matches the stateless-static-class testing model: the entry point is the test surface.

---

## Limitations and Future Work

1. **Phase formalization.** The 6-partial split implicitly respects a phase model (narrowing → proof → inference) but does not formalize it with explicit phase types or contracts. If IDE responsiveness requires incremental re-narrowing on field edits, formalizing phases (Kotlin K2 model) would be the appropriate evolution.

2. **Diagnostics deferral.** Currently, diagnostics are emitted inline during validation. Kotlin K2 defers all diagnostics to a dedicated CHECKERS phase. Deferral would enable post-processing (deduplication, priority ordering) but adds a storage pass. Not needed at current scale.

3. **Helper distribution.** The centralized Helpers partial (398 LOC, 29 methods) is the one minor divergence from Roslyn's distributed-helpers pattern. If Helpers grows past 600 LOC, distributing domain-specific helpers to consumer files would reduce coupling.

4. **Main file growth.** The Main file (1,260 LOC) handles transitions, state actions, rules, computed fields, and collection mutations. If any single domain grows significantly, extracting it into a dedicated partial (e.g., `PreceptTypeChecker.ComputedFields.cs`) would maintain the per-file ceiling.

5. **C99 cross-event field invariant analysis.** Out of scope for the type checker. Requires fixed-point iteration across events, breaking the single-pass guarantee. Tracked separately as issue #117.

6. **Incremental compilation.** The type checker processes the entire definition model in one call. No incremental re-checking of changed regions. Not needed while `.precept` files remain small (typical: 50–200 lines), but would be required if definitions grow to thousands of lines.
