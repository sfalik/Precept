# Metadata Everywhere: Complete Architectural Vision
## The Precept Pipeline as a Metadata-Driven Engine

**Status:** Working design — pending cross-review with George's runtime design  
**Author:** Frank, Lead Architect & Language Designer  
**Date:** 2026-04-26  
**Audience:** Implementers, language designers, tooling authors  
**Scope:** The complete pipeline from SyntaxTree through Runtime Evaluator, grounded in the catalog-driven philosophy.

---

## Executive Summary

The Precept compiler and runtime are not traditional pipeline stages. They are **generic machinery that reads metadata.** Every node in the parse tree, every type in the type system, every expression in the graph model, and every dispatch decision in the evaluator must be **kind-annotated and metadata-driven.**

Right now, we have catalogs for Tokens, Types, Operators, Operations, Modifiers, Actions, Constructs, and failure modes. But we have not yet **applied the principle uniformly across all node families.** `Declaration` nodes carry `ConstructKind`. Good. But `Statement` nodes are one-type-per-verb with no kind discriminant — the type checker must switch on C# type structure to reach metadata. `Expression` nodes carry `OperatorKind` on some shapes but not `OperationKind` or `FunctionKind` — downstream stages can't answer "what operation is this?" without re-deriving. `Modifier` arrays carry `Token[]`, not `ModifierKind[]` — the graph analyzer must re-translate.

This document establishes the complete vision: **every node family carries the kind enum of its language-surface metadata. Every downstream stage reads kind, not C# type.** The pipeline is generic machinery reading the catalog. When a new kind is added to a catalog, a stage that switches on the kind gains a new case — the compiler refuses to build until that case is filled. This is not a nice-to-have consistency; it is the foundational architectural principle that makes the metadata-driven approach possible.

---

## 1. Philosophical Grounding: Why Metadata, Not Type Structure

### The Traditional Compiler Model (Roslyn, GCC, TypeScript)

Domain knowledge is scattered across pipeline stage implementations. Each stage is a domain expert:
- The parser doesn't need to know about operations — it just emits expressions.
- The type checker embeds the rules for which operations are legal.
- The optimizer embeds knowledge about operator properties.
- The evaluator embeds knowledge about how to execute each operation.

The result: domain knowledge is duplicated and dispersed. Add an operator? Touch the parser, the type checker, the optimizer, and the evaluator. Add a modifier? Touch everywhere it applies. The compiler is powerful because it accumulates expertise across its stages — but that expertise is locked in code.

### The Catalog-Driven Model (Precept)

Domain knowledge is declared as structured metadata in catalogs. Each catalog is a registry — an exhaustive inventory of one aspect of the language. Pipeline stages are generic machinery that reads the registry:

- The parser reads `Constructs.All` to know the exhaustive set of declaration shapes.
- The type checker reads `Operations.All` to validate which operator combinations are legal.
- The graph analyzer reads `Modifiers.All` to know the semantic properties of each modifier.
- The evaluator reads `Actions.All` to dispatch to the correct action implementation.

The result: domain knowledge is declared once, in machine-readable form. When a new operator is added, it is added to the `Operations` catalog as a new `OperationKind` member with its metadata. Every consumer — the type checker, the evaluator, the language server, the MCP tools — derives from the same source. There is no duplication, no drift, no parallel copies.

### Why This Matters for Nodes

The pipeline is only metadata-driven if **every decision point reads kind, not C# type.** If the type checker sees an `Expression` and must switch on `expression.GetType().Name` to know whether it's a `BinaryExpression` or `UnaryExpression`, it is not reading metadata — it is reading implementation structure. If it switches on `binaryExpression.Operation` to know what operator this is, it is reading metadata.

When a node carries the kind enum of its metadata, stages never need to case on C# type. They case on kind. The compiler enforces exhaustiveness on the kind enum, not on the type hierarchy. If a new kind is added to the `OperationKind` enum, the compiler refuses to build until the evaluator's switch statement includes a case for it. This is the enforcement mechanism that makes the metadata-driven approach possible.

### The Catalog System Design Principle

From `docs/language/catalog-system.md`:

> **If something is part of the Precept language, it gets cataloged.**

This principle applies to nodes. A `BinaryExpression` is part of the language — it appears in `.precept` files with a specific operator and operand types. The operator's identity (which operation is this?) is language surface. Therefore, a `BinaryExpression` must carry the operation kind — not as a derived property computed from its operands, but as a declared, cataloged kind.

The alternative — deriving the operation kind from the operands at every stage — is:
1. Computationally wasteful (re-derive every time it's needed)
2. Error-prone (derivation logic must be duplicated or centralized but not visible)
3. Architecturally weak (if derivation changes, all consumers must change)

Declaring the kind once, at parse time, when the operation is identified, and carrying it forward is clean, efficient, and enforces consistency.

---

## 2. The Unified Principle

**Every node family in the pipeline carries the kind enum of its catalog. No stage resolves kind from C# type structure. No stage re-derives kind from operands or context. Kind is assigned once, at the earliest stage where it is determined, and carried forward as a declared property.**

Expressed as constraints:

| Constraint | Rationale |
|-----------|-----------|
| Every semantically distinct node form has a `Kind` property of the appropriate enum type | Enables kind-based dispatch; eliminates type-based switching |
| Kind is assigned at parse time (or the earliest stage where identity is determined) | Single source of truth; no re-derivation; eliminates race conditions |
| Pipeline stages dispatch on kind via exhaustive switch, never on C# type | Compiler enforces completeness; catalog additions force code completion |
| Metadata for a kind is accessed via `Catalog.GetMeta(kind)`, never via reflection or ad-hoc lookups | Catalog is the single source; all metadata is centralized |
| No "derived" kind properties — kind is immutable once assigned | Eliminates inconsistency; kind is a fact, not a computation |

---

## 3. Complete Node Design

### 3.1 Declaration Nodes

**Current State:** ✓ Already catalog-driven.

`ConstructKind` already lives on the base `Declaration` record. Every construct (field, state, event, rule, transition, ensure, access mode, action, etc.) is identified by its `ConstructKind`. The type checker validates semantic scoping by checking `ConstructMeta.AllowedIn`.

**Kind Metadata Unlock:**

```csharp
var constructMeta = Constructs.GetMeta(decl.Kind);
// Now available:
// - constructMeta.LeadingToken (determines parser dispatch)
// - constructMeta.Slots (expected node fields)
// - constructMeta.AllowedIn (semantic scoping rules for type checker)
// - constructMeta.UsageExample (for diagnostics and MCP)
```

### 3.2 Statement Nodes

**Current State:** ✗ One type per verb, no kind discriminant.

The `ActionKind` catalog already exists for the atomic action verbs (`Set`, `Add`, `Remove`, etc.). The gap: this kind is not carried on the `Statement` base. Every consumer must type-switch to reach `ActionMeta`.

**Fix:** `ActionKind Kind` on the `Statement` base. The parser knows the kind at construction time — it just wasn't recording it.

### 3.3 Expression Nodes (THE MAJOR GAP)

**Current State:** ✗ Carry `OperatorKind` on binary/unary expressions, but not `OperationKind` or `FunctionKind`.

**The key insight:** `OperationKind` cannot go on parse tree nodes — the parser doesn't know operand types. The parser emits `OperatorKind` (the lexical symbol). The type checker resolves which operation this actually is and annotates the TypedModel node.

```csharp
// SyntaxTree (output of parser) — lexical only
public sealed record BinaryExpression(
    Expression Left,
    OperatorKind Operator,   // "Plus", "Times", etc.
    Expression Right,
    SourceSpan Span) : Expression;

// TypedModel (output of type checker) — semantically resolved
public sealed record TypedBinaryExpression(
    TypedExpression Left,
    OperatorKind Operator,
    OperationKind Operation,  // <- type checker fills this
    TypedExpression Right,
    TypeKind ResultType,
    SourceSpan Span) : TypedExpression;
```

Same pattern for function calls (`FunctionKind` + `OverloadIndex`) and member access (`TypeAccessor` from `TypeMeta.Accessors`).

### 3.4 TypeRef Nodes

Type references must carry `TypeKind` in the parse tree — the parser resolves keyword tokens (`"string"`, `"date"`, etc.) to `TypeKind` via a `TypesByToken` index built from the Types catalog. TypeRef nodes vanish in the TypedModel; `TypeKind` appears directly on the nodes that use it.

### 3.5 Modifier Nodes

**Current State:** ✗ Modifier arrays carry `Token[]` — consumers must re-translate to `ModifierKind`.

**Fix:** Abstract `Modifier` base with `ModifierKind Kind` on the base. Three concrete shapes: `FieldModifier` (with optional `Expression? Value`), `StateModifier`, `AnchorModifier`. Every modifier carries its kind; graph analyzer reads `Modifiers.GetMeta()` directly.

---

## 4. The Typed Model as Kind-Enriched Tree

### Structure

```csharp
public sealed record TypedModel(
    ImmutableArray<TypedDeclaration> Declarations,
    ImmutableArray<Diagnostic> Diagnostics);

public abstract record TypedDeclaration(ConstructKind Kind, SourceSpan Span);
public abstract record TypedExpression(TypeKind ResultType, SourceSpan Span);
```

### TypedExpression Hierarchy

```csharp
public sealed record TypedLiteral(
    TypeKind ResultType, object Value, SourceSpan Span) : TypedExpression(ResultType, Span);

public sealed record TypedFieldRef(
    TypeKind ResultType, Identifier FieldName, SourceSpan Span) : TypedExpression(ResultType, Span);

public sealed record TypedBinaryExpression(
    TypeKind ResultType,
    OperatorKind Operator,
    OperationKind Operation,   // type checker
    TypedExpression Left,
    TypedExpression Right,
    SourceSpan Span) : TypedExpression(ResultType, Span);

public sealed record TypedUnaryExpression(
    TypeKind ResultType,
    OperatorKind Operator,
    OperationKind Operation,   // type checker
    TypedExpression Operand,
    SourceSpan Span) : TypedExpression(ResultType, Span);

public sealed record TypedFunctionCall(
    TypeKind ResultType,
    FunctionKind Function,     // type checker
    ImmutableArray<TypedExpression> Arguments,
    SourceSpan Span) : TypedExpression(ResultType, Span);

public sealed record TypedMemberAccess(
    TypeKind ResultType,
    TypedExpression Subject,
    TypeAccessor Accessor,     // from TypeMeta.Accessors
    SourceSpan Span) : TypedExpression(ResultType, Span);

public sealed record TypedConditional(
    TypeKind ResultType,
    TypedExpression Condition,
    TypedExpression Consequent,
    TypedExpression Alternative,
    SourceSpan Span) : TypedExpression(ResultType, Span);

public sealed record TypedEventArgRef(
    TypeKind ResultType,
    Identifier EventName,
    Identifier ParameterName,
    SourceSpan Span) : TypedExpression(ResultType, Span);
```

### TypedAction

```csharp
public sealed record TypedAction(
    ActionKind Kind,           // from Actions catalog
    TypedExpression TargetField,
    TypedExpression? Value,    // Set requires it, Clear doesn't
    SourceSpan Span);
```

The type checker validates `ActionMeta.ApplicableTo`, `ActionMeta.ValueRequired`, and `ActionMeta.ProofRequirements` — all from catalog metadata.

---

## 5. The Proof Engine Unlock

With `OperationKind` on every binary expression and `FunctionKind` on every function call, the proof engine is a pure metadata reader:

```csharp
foreach (var expr in WalkExpressionsInModel(model))
{
    var requirements = expr switch
    {
        TypedBinaryExpression b => Operations.GetMeta(b.Operation).ProofRequirements,
        TypedUnaryExpression u  => Operations.GetMeta(u.Operation).ProofRequirements,
        TypedFunctionCall f     => Functions.GetMeta(f.Function)
                                      .ResolveOverload(f.Arguments.Select(a => a.ResultType))
                                      .ProofRequirements,
        TypedMemberAccess m     => m.Accessor.ProofRequirements,
        _                       => null
    };
    foreach (var req in requirements ?? [])
        obligations.Add(new ProofObligation(req, expr));
}
```

Zero hardcoded obligation rules. Every obligation comes from catalog metadata. When a new operation is added to `Operations.cs` with a `ProofRequirement`, the proof engine collects it automatically.

**Scope of static proof:** Conservative. The proof engine handles literal constants and field modifiers (e.g., `nonnegative` proves a value can't be zero). Unknown expressions emit a diagnostic telling the author to add a guard. No SMT solver — fast, deterministic, bounded.

---

## 6. The Evaluator: Descriptor-Keyed Dispatch

The evaluator's dispatch tables are built once at startup from kind enums, frozen, and shared across all executions:

```csharp
private static readonly FrozenDictionary<ActionKind, ActionExecutor>     ActionDispatch;
private static readonly FrozenDictionary<OperationKind, BinaryOpExecutor> BinaryOpDispatch;
private static readonly FrozenDictionary<FunctionKind, FunctionExecutor>  FunctionDispatch;
```

Every `ActionKind`, `OperationKind`, and `FunctionKind` must have an entry — the compiler enforces exhaustiveness. Adding a member to any of these enums causes a compile error until the dispatch table is updated.

The evaluator trusts the proof engine. Zero proof diagnostics → no `StaticallyPreventable` fault fires at runtime.

---

## 7. The Runtime Fault Bridge

Faults carry `FaultCode`. Every `FaultCode` is tagged `[StaticallyPreventable(DiagnosticCode)]`. With metadata-saturated execution, a fault at runtime can carry:

- `ActionKind` — which action was executing
- `OperationKind` / `FunctionKind` — which operation triggered the proof requirement
- `FieldName`, `EventName` — execution context
- `FailedValue` — the actual value that violated the constraint

This closes the loop: every runtime fault maps back to the compile-time diagnostic that should have blocked it.

---

## 8. Additional Unlocks

### Language Server
- **Hover on binary expression:** `Operations.GetMeta(binExpr.Operation).Description` — no ad-hoc strings
- **Completions in modifier position:** Filter `Modifiers.All` by `FieldModifierMeta.ApplicableTo` for the field's resolved `TypeKind`
- **Semantic tokens:** `Constructs.GetMeta(decl.Kind).SemanticTokenType` drives colorization

### MCP
`precept_language` becomes a complete structured export of all 10 catalogs — typed vocabulary, not prose.

### Tests
Theory tests auto-generated from `Operations.All`, `Actions.All`, `Functions.All` — every catalog entry gets a coverage test verifying the evaluator has a dispatch case for it.

---

## 9. Open Design Questions for Shane

These are genuine judgment calls, not architecture questions:

### 9.1 TypedModel: Parallel Tree vs. In-Place Annotation

**Recommendation:** Parallel tree. TypeChecker is a pure function: `SyntaxTree → TypedModel`. Each stage consumes a well-defined input type and produces a well-defined output type. Testing is cleaner — construct TypedModel instances directly without going through the parser. The parse tree is discarded after type checking; we don't hold both simultaneously.

### 9.2 FunctionKind: One Enum Member Per Family vs. Per Overload

**Recommendation:** One per family. `FunctionKind.Min` identifies the function; the type checker resolves which overload and stores `OverloadIndex` alongside it. Avoids combinatorial explosion in the enum.

### 9.3 ExecutableModel: Produced by ProofEngine or Separate Stage?

**Options:**
- A. `SyntaxTree → TypeChecker → TypedModel → ProofEngine → ExecutableModel`
- B. `SyntaxTree → TypeChecker → TypedModel → Compiler → ExecutableModel` (proof engine is pure verification)

**Recommendation:** Option A. The proof engine already knows all the metadata; assembling the executable model as part of proving soundness is natural.

### 9.4 ProofModel Detail Level

**Options:** Detailed obligations (proven + unproven + diagnostics) vs. aggregate status (IsComplete + diagnostics).

**Recommendation:** Detailed obligations. Tooling and advanced analysis benefit from fine-grained information.

### 9.5 Constraint Violation Capture

**Options:** Metadata only vs. full expression tree in fault.

**Recommendation:** Metadata only for now. Source location + type checker output indexed by span can reconstruct context if needed.

---

## 10. Summary: Stage → Kind Dispatch Table

| Stage | Input | Output | Primary Dispatch Key |
|-------|-------|--------|----------------------|
| Parser | TokenStream | SyntaxTree | `ConstructKind` (declarations), `OperatorKind` (expressions) |
| Type Checker | SyntaxTree | TypedModel | `TypeKind` for compatibility; emits `OperationKind`, `FunctionKind` |
| Graph Analyzer | TypedModel | GraphResult | `ModifierKind` for semantic graph properties |
| Proof Engine | TypedModel + GraphResult | ProofModel | `OperationKind`, `FunctionKind`, `ProofRequirement[]` |
| Evaluator | TypedModel / ExecutableModel | Outcome | `ActionKind`, `OperationKind`, `FunctionKind` dispatch tables |

At every stage, the machinery reads metadata. Domain knowledge is declared once, in catalogs. Completeness is enforced by the compiler's exhaustiveness checker on switches.

---

**End of Frank's Design Document**
