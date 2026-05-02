# Type Checker Design — Research Cross-Reference Analysis

**Date:** 2026-05-02
**Author:** Frank (Lead/Architect)
**Source doc:** `docs/compiler/type-checker.md` (817 lines)
**Research corpus:** `research/architecture/compiler/` (14 surveys), `research/language/references/` (11 references)

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
- **SPARK Ada / GNATprove:** Explicit separation between "generating verification conditions" (from type checking/code analysis) and "proving them" (SMT solver pass). The compiler generates VCs; solvers discharge them. The summary table classifies each obligation by which prover discharged it. (Proof attribution survey §SPARK; Interval arithmetic survey §SPARK)
- **Dafny:** Pipeline is `source → parser → type checker → resolver → Boogie IR → VCs → SMT solver`. Type checking and resolution produce obligations; Boogie + Z3 discharge them. Explicit multi-stage separation. (Proof attribution survey §Dafny)
- **Liquid Haskell:** GHC type-checks the module first; LH plugin then generates Horn clause constraints for `liquid-fixpoint` to solve. Obligation generation is separate from discharge. (Proof attribution survey §Liquid Haskell)

**Verdict:** The separation of obligation *recording* from obligation *discharge* is universal in verification systems. Our design — type checker records `ProofRequirement`, ProofEngine handles interval/range analysis — is directly validated by SPARK, Dafny, and Liquid Haskell.

### 7. Widening/Subtyping via TypeMeta.WidensTo

**Design:** `IsAssignable(source, target)` checks `Types.GetMeta(source).WidensTo.Contains(target)`. Widening paths are metadata on the Types catalog.

**Research backing:**
- **Type system survey:** Documents the coercion hierarchy: `integer → decimal`, `integer → number`, with `decimal ↛ number` following C# semantics. The widening direction is documented as inheriting from the platform. (Type system survey §Coercion hierarchy)
- **Kotlin:** Integer literal types are subtypes of all compatible concrete types — a form of ad-hoc subtyping. (Context-sensitive literal survey §Kotlin)
- **F# Units:** Dimensionless quantities implicitly convert to scalar — special-cased widening in the type system. (Units survey §F# Dimensionless)

**Verdict:** Metadata-driven widening rules (rather than hardcoded subtype hierarchies in checker logic) align with the catalog-driven architecture. The specific widening paths (`integer→decimal`, `integer→number`) are validated by the type system survey's platform-alignment rationale.

### 8. Array-Primary + FrozenDictionary Secondary (SemanticIndex Collections)

**Design:** `ImmutableArray<TypedField>` primary (preserves declaration order) + `FrozenDictionary<string, TypedField>` secondary (O(1) lookup).

**Research backing:**
- **Roslyn:** `ImmutableArray<SyntaxTree>` is the primary collection on `Compilation`. `SemanticModel` provides per-tree access. Symbol lookup is via name-indexed maps internally.
- **Go `types.Info`:** Uses `map[ast.Expr]TypeAndValue` (name-indexed maps) for lookup while `[]Object` preserves declaration order in scopes. (Pipeline survey §Go)
- **TypeScript:** `SourceFile[]` is ordered; symbol table is `SymbolTable` (name → Symbol map). (Pipeline survey §TypeScript)

**Verdict:** The dual-representation pattern (ordered primary for iteration/display, hashed secondary for lookup) is common practice. `FrozenDictionary` specifically is the optimal .NET 8+ choice for read-only maps created once and queried many times.

---

## Justified Divergences (design departs from common practice, but for good reason)

### 1. Flat Semantic Inventory vs. Per-Tree SemanticModel

**Divergence:** Roslyn/TypeScript use a per-file `SemanticModel` that lazily resolves on demand. Precept's `SemanticIndex` is a flat, eagerly-computed inventory of all typed declarations.

**Justification:** Precept compiles a single `.precept` file (never multi-file), and the typical file is 50–500 lines. Lazy per-tree resolution exists to handle million-line codebases with thousands of files. For a single-file DSL with <1000 declarations, eager full-compile is simpler, faster, and eliminates the invalidation/cache coherence complexity that lazy models require. The design is correct for the problem scale.

**Verdict: Justified.** The divergence is appropriate for Precept's single-file-single-entity model.

### 2. No Query System / No On-Demand Type Resolution

**Divergence:** Rust uses a demand-driven query system where type-checking is incremental per-definition. Precept resolves all expressions eagerly in a single pass.

**Justification:** Same scale argument. Rust's query system exists because a 600K-line crate with generics monomorphization would be prohibitively slow with eager full-compile. Precept's entire type surface is <500 declarations. The overhead of a query system (key→result caching, invalidation, red-zone detection for cycles) would exceed the actual computation cost. Furthermore, Precept has no generics, no monomorphization, no cross-file imports — the complexity drivers for query systems don't exist.

**Verdict: Justified.** A query system would be over-engineering at this scale.

### 3. Catalog-Driven Checker (~70% Catalog / ~30% Structural)

**Divergence:** No production compiler surveyed uses a catalog-driven type checker. Roslyn's binder hardcodes C# semantics; TypeScript's checker.ts is 50K lines of hardcoded type rules; Rust's trait solver is algorithmic, not data-driven.

**Justification:** This is Precept's architectural identity, not an oversight. The catalog-driven architecture means new operators, functions, types, and actions require zero checker code changes. This is explicitly documented in the design spec ("new language features require zero type-checker code changes"). The divergence exists because Precept is a *metadata-resolution engine*, not a general-purpose language compiler. The type system is closed and fully described by catalogs; traditional compilers have open type systems that require algorithmic resolution (generics, variance, higher-kinded types, trait coherence).

**Verdict: Justified — this is the core architectural differentiator.** The divergence is inherent to Precept's design philosophy and wouldn't be possible in a language with generics or open type classes.

### 4. Qualifier Disambiguation as ~15 Lines of Structural Logic

**Divergence:** F# units-of-measure track full dimensional algebra at the type level (exponent vectors, canonical forms, unit variables). Boost.Units does the same via template metaprogramming. Precept uses ~15 lines of post-`FindCandidates` structural logic.

**Justification:** Precept qualifiers are *runtime values*, not type-level parameters. The qualifier identity ("USD", "kg") is not known at compile time — it's determined by entity data. F# can do full dimensional algebra because units are declared at the source level and erased at runtime. Precept cannot: the checker only validates qualifier *compatibility* (same vs. different), not qualifier *identity*. The ~15 lines check whether operand qualifiers are structurally compatible and select the appropriate catalog entry. Deeper obligations (e.g., "prove these two money values have the same currency") are correctly delegated to the ProofEngine.

**Verdict: Justified.** The simplicity is not a limitation — it reflects the genuine boundary between compile-time structural analysis and runtime value identity. A full-blown F#-style unit type system would require qualifier types to be declared at the field level, which is a language surface change, not a type-checker gap.

### 5. No Defaulting Rules for Numeric Literals

**Divergence:** Kotlin defaults to `Int`, Rust to `i32`, Swift to `Int`, Haskell to `Integer`. Precept's design doesn't specify a default when `expectedType` is absent.

**Justification:** In Precept, every numeric literal appears in a context that provides type information: field types are declared, action targets have known types, operator signatures resolve from catalog metadata, function parameter types are known. There is no `let x = 42` in Precept — every expression exists within a typed declaration context. If `expectedType` is truly absent (no context at all), the literal is in an ill-formed position. Defaulting to `number` (the widest type) would suppress legitimate type errors.

**Verdict: Justified — but document the invariant explicitly.** The assumption that every expression resolves within a typed context should be stated as a design invariant in the spec, with a diagnostic for the (impossible?) case where `expectedType` is null at a literal.

---

## Research Gaps (findings the design should incorporate)

### 1. Out-of-Range Literal Checking — HIGH PRIORITY (Slice 4)

**Research finding:** Every surveyed language performs compile-time range checking once a literal's type is resolved:
- Kotlin: `INTEGER_LITERAL_OUT_OF_RANGE` — compile error if value exceeds resolved type's range.
- Rust: "literal out of range for `u8`" with explicit note showing the valid range.
- Swift: "integer literal '200' overflows when stored into 'Int8'" — error during constraint solving.
- Ada: Checked against subtype bounds at compile time.

**Gap in design:** The type checker spec discusses `expectedType` propagation for literal type resolution (Slice 4) but does not specify range checking after resolution. For example: if `expectedType` is `integer` (Int64), a literal like `99999999999999999999` should produce a diagnostic. If the ContentValidation DU lands for typed constants, money/date formats are validated — but *numeric* range validation isn't addressed.

**Recommendation:** Add to Slice 4 spec: after resolving a numeric literal's type via `expectedType`, validate the literal's value against the type's representable range. For `integer` → check Int64 bounds. For `decimal` → check Decimal.MaxValue/MinValue. For `number` → check Double range (with precision loss warning for integers > 2^53). This is ~10 lines of logic in the literal resolution arm, sourced from `TypeMeta` (add `MinValue`/`MaxValue` fields or a `ValidateRange(object value)` method on `TypeMeta`).

### 2. Precision Propagation Awareness for Decimal Arithmetic — MEDIUM PRIORITY (future)

**Research finding:** The exact decimal arithmetic survey documents critical precision behaviors:
- Division can produce inexact results (silently rounded in .NET Decimal).
- Multiplication accumulates scale (`scale_a + scale_b`), which can overflow the 96-bit mantissa.
- Trailing zeros may be lost when mantissa reduction is needed.
- Division by zero throws OverflowException, not a special value.

**Gap in design:** The type checker currently resolves `decimal / decimal → decimal` and `decimal * decimal → decimal` without noting that these operations have different precision characteristics than addition. The ProofEngine handles overflow obligations, but the type checker has no mechanism to *warn* about precision-lossy operations.

**Recommendation:** This is a ProofEngine concern, not a type checker concern — and the design correctly places it there. However, the type checker spec should explicitly state that precision propagation is NOT its responsibility, referencing the exact decimal survey. If we ever add `ProofRequirement.PrecisionWarning` to the catalog metadata for division operations, the type checker would record it automatically through the existing `BinaryOperationMeta.ProofRequirements` mechanism. No design change needed — just document the boundary.

### 3. Temporal Type Cross-Validation: Period vs Duration in ContentValidation — LOW PRIORITY

**Research finding:** The temporal type hierarchy survey documents NodaTime's critical distinction:
- `Duration`: fixed nanoseconds (machine time). Calendar-independent.
- `Period`: human chronological amount (years, months, days). NOT a fixed length of time.
- `LocalDate + Period → LocalDate` is valid; `LocalDate + Duration` is NOT (deliberately unsupported).
- `Period` with time-unit components applied to `LocalDate` raises `ArgumentException`.

**Gap in design:** The ContentValidation DU mentions `NodaTimeValidation` for date/time/period parsing. But the type checker's `Types.WidensTo` and `Operations.FindCandidates` entries for temporal arithmetic must distinguish:
- `date + integer → date` (days) — valid
- `date - date → integer` (day count) — valid
- `date + period → date` — valid only if period contains no time components
- `date + duration` — conceptually invalid (duration is machine-time)

The Operations catalog presumably encodes these as separate entries, but the type checker spec doesn't reference the temporal hierarchy survey's distinction or confirm that the catalog entries are correctly shaped for temporal type safety.

**Recommendation:** Add a cross-reference note to the type checker spec (§Catalog Integration or §Catalog Gaps): "Temporal arithmetic operations are encoded in the Operations catalog with type-specific signatures (e.g., `date + integer → date`, not `date + number → date`). The temporal type hierarchy survey (`research/architecture/compiler/temporal-type-hierarchy-survey.md`) validates these signatures against NodaTime's type-safety model. No special type-checker logic is needed — the catalog entry shapes handle temporal safety generically." This confirms the catalog-driven approach handles temporal types correctly without per-type branching.

### 4. Structured Error Recovery Modes — LOW PRIORITY (future LS enhancement)

**Research finding:** Roslyn's `SemanticModel` provides best-effort type information even for erroneous code — not just "ErrorType" but *candidate* types that were considered before the error. TypeScript's checker similarly provides partial resolution info for IntelliSense in error positions.

**Gap in design:** Our `TypedErrorExpression` carries only the `Expression Syntax` back-pointer. It doesn't carry any "candidate types" or "closest match" information that the LS could use for completions at error positions.

**Recommendation:** Consider adding an optional `ImmutableArray<TypeKind>? CandidateTypes` to `TypedErrorExpression` in a future enhancement. When `FindCandidates` returns entries but none matches qualifier/widening constraints, recording what *would have* matched enables the LS to provide "did you mean?" suggestions. This is not needed for Slice 2–10 (the LS can fall back to context-based completions), but is a known future lane. Note it as a post-v1 consideration.

### 5. ErrorGuaranteed Pattern (Rust) — DESIGN CONSIDERATION

**Research finding:** Rust's `ErrorGuaranteed` type ensures that if code proceeds past an error point, it carries *proof* that an error was reported. This prevents the situation where error-path code runs without a corresponding diagnostic.

**Gap in design:** Our design relies on convention: "downstream stages must handle `TypedErrorExpression` gracefully." But there's no structural guarantee that a `TypedErrorExpression` in the SemanticIndex implies a corresponding `Diagnostic` was emitted.

**Recommendation:** Consider adding a debug-only assertion (not a runtime cost) that validates the invariant: any `SemanticIndex` containing a `TypedErrorExpression` anywhere in its trees MUST also contain at least one `Diagnostic` with Error severity. This catches bugs where we accidentally produce `TypedErrorExpression` without emitting the corresponding diagnostic. Lightweight, test-time-only, no production cost.

### 6. Unit-of-Measure Type Variables — NOT APPLICABLE (confirmed)

**Research finding:** F# supports generic unit variables (`float<'u>`) enabling polymorphic functions over units. Boost.Units uses template parameters for the same purpose.

**Non-gap:** Precept correctly does NOT attempt this. Qualifiers are runtime values, not type-level parameters. Generic qualifier variables would require a fundamentally different language surface (parameterized type declarations, qualifier inference across function boundaries). The research confirms that full unit algebra requires units to be *declared* at the type level and *erased* at runtime — the opposite of Precept's model where qualifiers are *runtime data* and *opaque* at compile time.

**Verdict:** No action needed. The design's boundary between qualifier structural compatibility (type checker) and qualifier value identity (ProofEngine/Evaluator) is the correct split for a runtime-value qualifier model.

---

## Summary

| Category | Count | Assessment |
|----------|-------|------------|
| **Well-Grounded** | 8 decisions | Strong research validation across all major compilers |
| **Justified Divergences** | 5 decisions | All divergences explained by Precept's architectural identity or problem-scale |
| **Research Gaps** | 5 findings | 1 high (range checking), 1 medium (doc only), 3 low (future enhancements) |

**Overall assessment:** The type checker design is exceptionally well-grounded. The 2-pass architecture, error recovery strategy, diagnostic accumulation, expectedType propagation, immutable output, and proof separation all have direct validation from multiple production compiler surveys. The divergences from common practice (catalog-driven resolution, flat inventory, no query system, simple qualifier logic) are all justified by Precept's specific constraints: single-file DSL, closed type system, metadata-driven architecture, runtime-value qualifiers.

The single high-priority gap — out-of-range literal checking — is a straightforward addition to Slice 4 that follows universal compiler practice.
