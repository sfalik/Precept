# Type Checker Design — Research Validation

> **Validates:** `docs/compiler/type-checker.md`
> **Validation date:** 2026-05-02
> **Author:** Frank (Lead/Architect & Language Designer)
> **Research corpus:** 16 compiler architecture surveys (`research/architecture/compiler/`) + 11 language theory references (`research/language/references/`)
> **Supersedes:** `docs/working/type-checker-research-crossref.md` (working draft, now archived)

---

## Purpose

This document validates the type checker canonical design against the project's research corpus. It establishes the pattern for research validation of design docs: cross-referencing locked design decisions against surveyed compiler architectures and language theory to confirm grounding, justify divergences, and surface gaps.

---

## Well-Grounded (research validates the design)

### 1. 2-Pass Architecture with Registration → Checking

**Design:** Pass 1 registers symbols (field/state/event), Pass 2 resolves expressions and normalizes declarations.

**Research backing:** The compiler pipeline survey documents this exact pattern across all major compilers:
- **Roslyn:** Explicit Declaration phase → Bind phase separation. Declaration builds the symbol table; Bind resolves expressions against it. (Survey §Roslyn Pipeline Stages)
- **TypeScript:** Binder creates `Symbol` objects with scope chains → Checker resolves types on demand against the symbol table. (Survey §TypeScript Pipeline Stages)
- **Go:** `types2` package operates on a pre-built scope chain; symbols are registered before body analysis. (Survey §Go Pipeline Stages)
- **Kotlin K2 FIR:** Phase 1–3 resolve declarations (supertypes, modifiers, return types) before Phase 5 resolves bodies. (Survey §Kotlin K2 FIR Architecture)

**Verdict:** The 2-pass design is universal practice. Our Pass 1/Pass 2 boundary — "symbol tables first, expression resolution second" — is the canonical compiler architecture pattern.

### 2. ErrorType Propagation + Always-Produce-Partial-Results

**Design:** `TypedErrorExpression` replaces failed sub-expressions; the containing declaration is always emitted; ErrorType operands produce ErrorType results (suppresses cascading diagnostics).

**Research backing:**
- **Roslyn:** "Does NOT short-circuit on errors." Parser inserts missing tokens; binder uses `IErrorTypeSymbol` as placeholder. `Compilation` with errors is fully valid — you can still query `SemanticModel`. (Pipeline survey §Roslyn Error Handling; Compilation result survey §Roslyn Partial Success)
- **TypeScript:** `NodeFlags.ThisNodeHasError` flag; parser uses zero-width missing nodes; semantic diagnostics continue past errors. (Pipeline survey §TypeScript Error Handling)
- **Rust:** `TyKind::Error` is the placeholder type; type checking continues past errors. The `ErrorGuaranteed` type carries proof that an error was reported. (Pipeline survey §Rust Error Handling)
- **Kotlin K2:** `FirErrorTypeRef` used as placeholder; resolution continues in presence of errors. (Pipeline survey §Kotlin K2 Error Handling)

**Verdict:** Every production compiler uses error-type propagation to prevent diagnostic cascades. Our `TypedErrorExpression` + "no declaration is ever skipped" policy is directly validated by Roslyn, TypeScript, Rust, and Kotlin.

### 3. Diagnostic Accumulation Without Abandoning

**Design:** Diagnostics are accumulated in `CheckContext.Diagnostics`; no pass is abandoned on first error.

**Research backing:**
- **Roslyn:** Diagnostics returned as `ImmutableArray<Diagnostic>` from `GetDiagnostics()` — collected at each stage, never halting. (Diagnostic survey §Roslyn)
- **TypeScript:** Separate `getSyntacticDiagnostics()`, `getSemanticDiagnostics()`, `getDeclarationDiagnostics()` — all collected, never halting. (Diagnostic survey §TypeScript)
- **Go:** Error callback collects errors; type checking continues. (Pipeline survey §Go Error Handling)
- **Dafny:** `ErrorCount`, `InconclusiveCount`, `TimeoutCount` — all accumulated, pipeline continues. (Proof attribution survey §Dafny)

**Verdict:** The "collect model" is the universal standard. No production compiler abandons analysis on first error. Our accumulate-without-abandoning approach is the only defensible design for LS integration.

### 4. Context-Sensitive Literal Typing via expectedType Propagation

**Design:** Slice 4's `Resolve(Expression expr, TypeKind? expectedType)` passes an expected type downward for numeric literal resolution and typed constants.

**Research backing:**
- **Kotlin:** Uses an "expected type" mechanism. `TypeInferenceContext` carries `ExpectedType`. For literals: if `ctx.expectedType ≠ None` and value fits, resolve to expected type; else default. (Context-sensitive literal survey §Kotlin)
- **Rust:** Expected types from annotations propagate as equality constraints, "effectively implementing a checking mode without an explicit mode parameter." The `{integer}` inference variable is resolved by surrounding context or falls back to `i32`. (Context-sensitive literal survey §Rust)
- **Swift:** Constraint solver treats expected types as equality constraints on type variables, achieving the same effect as a bidirectional check without an explicit mode separation. (Context-sensitive literal survey §Swift)
- **GHC:** `OutsideIn(X)` carries `ExpType` argument. When `ExpType` is `Check tau`, the literal type variable is immediately unified. (Context-sensitive literal survey §Haskell)

**Verdict:** The `expectedType` parameter is the practical implementation of what the research calls "checking mode" in bidirectional type checking. Kotlin's implementation is the closest analog — an explicit `ExpectedType` passed downward, with defaulting when absent. Our design is research-grounded.

### 5. Immutable Output Artifact (SemanticIndex)

**Design:** `SemanticIndex` is a sealed immutable record; mutable state lives only in `CheckContext` during the check pass.

**Research backing:**
- **Roslyn:** `Compilation` is explicitly documented as immutable. "The compilation object is an immutable representation." All mutation returns a new instance. (Compilation result survey §Roslyn Immutability)
- **TypeScript:** `Program` is "effectively immutable once created." (Compilation result survey §TypeScript Immutability)
- **Rust:** `Ty<'tcx>` types are interned and immutable, tied to the arena lifetime. (Pipeline survey §Rust TyCtxt)

**Verdict:** Immutable compilation output is universal. Our `SemanticIndex` sealed record is the correct shape.

### 6. Proof Requirement Recording Separated from Discharge

**Design:** Type checker records `ProofRequirements` on typed expressions (from catalog metadata); ProofEngine discharges them separately.

**Research backing:**
- **SPARK Ada / GNATprove:** Explicit separation between "generating verification conditions" (from type checking/code analysis) and "proving them" (SMT solver pass). The compiler generates VCs; solvers discharge them. (Proof attribution survey §SPARK; Interval arithmetic survey §SPARK)
- **Dafny:** Pipeline is `source → parser → type checker → resolver → Boogie IR → VCs → SMT solver`. Type checking and resolution produce obligations; Boogie + Z3 discharge them. (Proof attribution survey §Dafny)
- **Liquid Haskell:** GHC type-checks the module first; LH plugin then generates Horn clause constraints for `liquid-fixpoint` to solve. (Proof attribution survey §Liquid Haskell)

**Verdict:** The separation of obligation *recording* from obligation *discharge* is universal in verification systems. Our design — type checker records `ProofRequirement`, ProofEngine handles interval/range analysis — is directly validated by SPARK, Dafny, and Liquid Haskell.

### 7. Widening/Subtyping via TypeMeta.WidensTo

**Design:** `IsAssignable(source, target)` checks `Types.GetMeta(source).WidensTo.Contains(target)`. Widening paths are metadata on the Types catalog.

**Research backing:**
- **Type system survey:** Documents the coercion hierarchy: `integer → decimal`, `integer → number`, with `decimal ↛ number` following C# semantics. (Type system survey §Coercion hierarchy)
- **Kotlin:** Integer literal types are subtypes of all compatible concrete types — a form of ad-hoc subtyping. (Context-sensitive literal survey §Kotlin)
- **F# Units:** Dimensionless quantities implicitly convert to scalar — special-cased widening in the type system. (Units survey §F# Dimensionless)

**Verdict:** Metadata-driven widening rules (rather than hardcoded subtype hierarchies in checker logic) align with the catalog-driven architecture.

### 8. Array-Primary + FrozenDictionary Secondary (SemanticIndex Collections)

**Design:** `ImmutableArray<TypedField>` primary (preserves declaration order) + `FrozenDictionary<string, TypedField>` secondary (O(1) lookup).

**Research backing:**
- **Roslyn:** `ImmutableArray<SyntaxTree>` is the primary collection on `Compilation`. Symbol lookup is via name-indexed maps internally.
- **Go `types.Info`:** Uses `map[ast.Expr]TypeAndValue` (name-indexed maps) for lookup while `[]Object` preserves declaration order in scopes. (Pipeline survey §Go)
- **TypeScript:** `SourceFile[]` is ordered; symbol table is `SymbolTable` (name → Symbol map). (Pipeline survey §TypeScript)

**Verdict:** The dual-representation pattern (ordered primary for iteration/display, hashed secondary for lookup) is common practice. `FrozenDictionary` specifically is the optimal .NET 8+ choice for read-only maps created once and queried many times.

---

## Justified Divergences (design departs from common practice, but for good reason)

### 1. Flat Semantic Inventory vs. Per-Tree SemanticModel

**Divergence:** Roslyn/TypeScript use a per-file `SemanticModel` that lazily resolves on demand. Precept's `SemanticIndex` is a flat, eagerly-computed inventory of all typed declarations.

**Justification:** Precept compiles a single `.precept` file (never multi-file), and the typical file is 50–500 lines. Lazy per-tree resolution exists to handle million-line codebases with thousands of files. For a single-file DSL with <1000 declarations, eager full-compile is simpler, faster, and eliminates the invalidation/cache coherence complexity that lazy models require.

**Verdict: Justified.** The divergence is appropriate for Precept's single-file-single-entity model.

### 2. No Query System / No On-Demand Type Resolution

**Divergence:** Rust uses a demand-driven query system where type-checking is incremental per-definition. Precept resolves all expressions eagerly in a single pass.

**Justification:** Same scale argument. Rust's query system exists because a 600K-line crate with generics monomorphization would be prohibitively slow with eager full-compile. Precept's entire type surface is <500 declarations. The overhead of a query system (key→result caching, invalidation, red-zone detection for cycles) would exceed the actual computation cost. Furthermore, Precept has no generics, no monomorphization, no cross-file imports.

**Verdict: Justified.** A query system would be over-engineering at this scale.

### 3. Catalog-Driven Checker (~70% Catalog / ~30% Structural)

**Divergence:** No production compiler surveyed uses a catalog-driven type checker. Roslyn's binder hardcodes C# semantics; TypeScript's checker.ts is 50K lines of hardcoded type rules; Rust's trait solver is algorithmic, not data-driven.

**Justification:** This is Precept's architectural identity, not an oversight. The catalog-driven architecture means new operators, functions, types, and actions require zero checker code changes. The divergence exists because Precept is a *metadata-resolution engine*, not a general-purpose language compiler. The type system is closed and fully described by catalogs; traditional compilers have open type systems that require algorithmic resolution.

**Verdict: Justified — this is the core architectural differentiator.**

### 4. Qualifier Disambiguation as ~15 Lines of Structural Logic

**Divergence:** F# units-of-measure track full dimensional algebra at the type level. Precept uses ~15 lines of post-`FindCandidates` structural logic.

**Justification:** Precept qualifiers are *runtime values*, not type-level parameters. The qualifier identity ("USD", "kg") is not known at compile time. The ~15 lines check whether operand qualifiers are structurally compatible and select the appropriate catalog entry. Deeper obligations are correctly delegated to the ProofEngine.

**Verdict: Justified.** The simplicity reflects the genuine boundary between compile-time structural analysis and runtime value identity.

### 5. No Defaulting Rules for Numeric Literals

**Divergence:** Kotlin defaults to `Int`, Rust to `i32`, Swift to `Int`, Haskell to `Integer`. Precept's design doesn't specify a default when `expectedType` is absent.

**Justification:** In Precept, every numeric literal appears in a context that provides type information: field types are declared, action targets have known types, operator signatures resolve from catalog metadata. There is no `let x = 42` in Precept — every expression exists within a typed declaration context.

**Verdict: Justified — but document the invariant explicitly.** The assumption that every expression resolves within a typed context should be stated as a design invariant.

---

## Research Gaps (findings the design should incorporate)

### HIGH PRIORITY

**1. Out-of-Range Literal Checking (Slice 4)**

Every surveyed language performs compile-time range checking once a literal's type is resolved (Kotlin: `INTEGER_LITERAL_OUT_OF_RANGE`; Rust: "literal out of range for `u8`"; Swift: "integer literal overflows"; Ada: subtype bounds). The type checker spec discusses `expectedType` propagation but does not specify range checking after resolution. After resolving a numeric literal's type via `expectedType`, the checker must validate the literal's value against the type's representable range. ~10 lines of logic, sourced from `TypeMeta` range metadata.

### LOWER PRIORITY

**2. Precision Propagation Awareness for Decimal Arithmetic** — Not a type-checker concern (correctly placed in ProofEngine), but the design should explicitly state this boundary. No design change needed.

**3. Temporal Type Cross-Validation** — Operations catalog entries handle temporal safety generically; confirm catalog entry shapes align with NodaTime's type-safety model. Cross-reference note only.

**4. Structured Error Recovery Modes** — `TypedErrorExpression` could carry optional `CandidateTypes` for future LS "did you mean?" features. Post-v1 consideration.

**5. ErrorGuaranteed Pattern** — Debug-only assertion that any `SemanticIndex` containing `TypedErrorExpression` also contains at least one Error-severity diagnostic. Lightweight invariant validation.

---

## Summary

| Category | Count | Assessment |
|----------|-------|------------|
| **Well-Grounded** | 8 decisions | Strong research validation across all major compilers |
| **Justified Divergences** | 5 decisions | All divergences explained by Precept's architectural identity or problem-scale |
| **Research Gaps** | 5 findings | 1 high (range checking), 1 medium (doc only), 3 low (future enhancements) |

**Overall assessment:** The type checker design is exceptionally well-grounded. The 2-pass architecture, error recovery strategy, diagnostic accumulation, expectedType propagation, immutable output, and proof separation all have direct validation from multiple production compiler surveys. The divergences from common practice (catalog-driven resolution, flat inventory, no query system, simple qualifier logic) are all justified by Precept's specific constraints: single-file DSL, closed type system, metadata-driven architecture, runtime-value qualifiers.

The single high-priority gap — out-of-range literal checking — is a straightforward addition to Slice 4 that follows universal compiler practice.
