# Catalog-Driven Type Checker Design Review

**Author:** Frank (Lead/Architect)
**Date:** 2026-05-02
**Subject:** Creative review of the type checker design from a catalog-first perspective
**Request:** Shane asked for outside-the-box thinking on catalog-driven type checking — explicitly countering massive model training bias from traditional compiler codebases.

---

## 1. Traditional Compiler Bias Audit

The current design is already far ahead of where traditional compiler thinking would land — the 70/30 catalog/structural split is real and defensible. But there are residual patterns where the *shape* of the design echoes compiler-internal conventions that don't apply to a closed, metadata-defined type system:

### 1.1 The "Resolve Function as Giant Switch" Pattern

The design describes `Resolve()` as "~250–350 lines" with "16+ arms" — one per AST node type. This is the Roslyn/TypeScript pattern: the type checker is a recursive descent over the expression tree, with per-node-type handling. Traditional compilers *must* do this because their expression forms are open-ended and carry genuinely different semantics.

**In Precept, the ExpressionForms catalog already classifies every form.** The 13 `ExpressionFormKind` members are a closed set with metadata (Category, IsLeftDenotation, LeadTokens). Yet the checker's `Resolve()` function is a manual pattern match over AST class types — not a catalog-driven dispatch. The `[HandlesCatalogMember]` annotations acknowledge this tension but treat it as an enforcement mechanism rather than a design driver.

**Bias:** Traditional compilers have no catalog of expression forms, so they have no choice but to write per-type match arms. Precept has the catalog but doesn't exploit it for dispatch routing.

### 1.2 The "Overload Resolution Algorithm" Pattern

The function overload resolution (§ Function Overload Resolution) describes a 7-step algorithm: arity filter → exact → widened → context retry. This is a miniature C# overload resolution engine. It's correct, but for 15 functions with 2–5 overloads each, it's a *lot* of algorithm for a small surface.

**Bias:** General-purpose languages have unbounded user-defined overloads, so they need sophisticated resolution. Precept has ~60 total overloads across all functions, with no user-defined functions. The catalog *knows* every overload that exists and could precompute resolution tables.

### 1.3 The "Widening as Runtime Computation" Pattern

The binary operation widening fallback (§ Binary Operation Widening Fallback) describes a 6-step priority cascade: try direct → left-widen → right-widen → both-widen. This is a runtime search algorithm.

**Bias:** Traditional compilers compute widening at check time because the set of types is open (user-defined implicit conversions in C#, trait impls in Rust). Precept has exactly 1 type with widening (`integer → decimal, number`). The entire widening graph is 3 edges. Computing it at runtime is overkill — the catalog could precompute all widened operation triples at startup.

### 1.4 The "Scope Stack" Pattern

`CheckContext` maintains `CurrentEventArgs`, `CurrentFieldIndex`, `CurrentScope`, and a `QuantifierBindings` stack — the classic mutable-walker-state pattern from procedural compilers. This is structural code the catalog can't replace, but the *way* it's framed as "scope management" echoes compilers with lexical scoping, closures, and nested functions.

**Bias:** Precept has exactly 4 scope situations: all fields, prior-fields-only, event-args-visible, quantifier-binding-visible. This is not "scope management" — it's a 4-case visibility rule that the construct catalog could declare per-construct.

### 1.5 The "Action Shape Classification" Pattern

The mapping from `ActionSyntaxShape` → TypedAction DU shape is described as "a stable 3-arm switch." Gap 3 even acknowledges this is catalog-expressible but deprioritizes it. The argument "new shapes naturally fall into existing categories" is a traditional-compiler argument: "we don't need metadata because the human can see the pattern." But seeing a pattern and *encoding* it as metadata are different things, and the catalog-driven principle says: encode.

---

## 2. Catalog-Driven Opportunities (New)

### 2.1 Expression Form Resolution Descriptors

**What it is:** Instead of a 16-arm switch in `Resolve()`, each `ExpressionFormMeta` carries a *resolution descriptor* — a declarative specification of how that form's type is computed.

**What catalog metadata would drive it:**
```csharp
// Extended ExpressionFormMeta with resolution shape:
public sealed record ExpressionFormMeta(
    ExpressionFormKind Kind,
    ExpressionCategory Category,
    bool IsLeftDenotation,
    IReadOnlyList<TokenKind> LeadTokens,
    string HoverDocs,
    ResolutionShape Resolution   // NEW
);

// DU of resolution strategies:
public abstract record ResolutionShape;
public sealed record CatalogLookupResolution(
    CatalogSource Source,        // Operations, Functions, Types (accessors)
    LookupStrategy Strategy      // FindCandidates, ByName, AccessorLookup
) : ResolutionShape;
public sealed record FixedTypeResolution(TypeKind Result) : ResolutionShape;
public sealed record PropagationResolution(PropagationRule Rule) : ResolutionShape;
public sealed record StructuralResolution() : ResolutionShape;  // identifier, grouped
```

**Why traditional compilers don't do this:** Because their expression forms have open-ended semantics — user-defined operators, overloaded methods, template instantiation. There's no finite set of resolution strategies to enumerate. In Precept, there IS. Every expression form resolves through exactly one of: catalog lookup (ops/functions/accessors), fixed result type (boolean for quantifiers and postfix), propagation (grouped/conditional), or structural dispatch (identifiers).

**Concrete example:**
- `BinaryOperation` → `CatalogLookupResolution(Operations, FindCandidates)` — the checker's generic loop asks the catalog
- `FunctionCall` → `CatalogLookupResolution(Functions, ByName)` — same generic loop, different catalog
- `PostfixOperation` → `FixedTypeResolution(TypeKind.Boolean)` — no lookup needed, result is always boolean
- `Quantifier` → `FixedTypeResolution(TypeKind.Boolean)` — ditto
- `Conditional` → `PropagationResolution(UnifyBranches)` — result comes from branch unification

The `Resolve()` function shrinks from 350 lines to maybe 80 lines of generic dispatch that reads the descriptor and invokes the appropriate strategy.

### 2.2 Precomputed Operation Resolution Tables

**What it is:** Instead of calling `FindCandidates(op, lhs, rhs)` at check time and then doing widening fallback, precompute ALL legal (op, lhs, rhs) → result triples at catalog initialization time, *including* widened variants.

**What catalog metadata would drive it:**
```csharp
// Computed once at startup from Operations.All + Types.GetMeta().WidensTo:
FrozenDictionary<(OperatorKind, TypeKind, TypeKind), ResolvedOperation[]> AllOperations;

// Where ResolvedOperation carries the match quality:
public sealed record ResolvedOperation(
    BinaryOperationMeta Meta,
    MatchQuality Quality   // Exact, LeftWidened, RightWidened, BothWidened
);
```

**Why traditional compilers don't do this:** User-defined operators, open type hierarchies, and implicit conversions mean the set is unbounded. Precept's set is finite: ~20 types × ~16 operators = at most ~5,120 possible triples, and the actual set is maybe 200. This trivially fits in a frozen dictionary. The entire widening fallback algorithm *dissolves* into a single dictionary lookup.

**Concrete example:** `money + money` → single lookup → `ResolvedOperation(AddMoneySame, Exact)`. `integer + decimal` → single lookup → `ResolvedOperation(AddDecimalDecimal, LeftWidened)`. No cascading search. No retry loops. The catalog *knows every possible answer* because the type system is closed.

### 2.3 Construct-Declared Scope Rules

**What it is:** Instead of the checker manually setting `CurrentEventArgs` and `CurrentScope` when entering/exiting construct kinds, the Constructs catalog declares the scope rule *per construct.*

**What catalog metadata would drive it:**
```csharp
// New field on ConstructMeta:
ScopeRule? CheckerScope = null

// DU of scope shapes:
public abstract record ScopeRule;
public sealed record EventArgScope(ConstructSlotKind EventSlot) : ScopeRule;
public sealed record PriorFieldsOnlyScope() : ScopeRule;
public sealed record AllFieldsScope() : ScopeRule;
```

**Why traditional compilers don't do this:** Scope rules in general-purpose languages are complex — lexical nesting, captures, type parameter scoping, module systems. They can't be described declaratively. Precept has exactly 4 scope situations that map 1:1 to construct kinds. This is trivially declarative.

**Concrete example:** `ConstructKind.TransitionRow` → `EventArgScope(EventTarget)`. `ConstructKind.FieldDeclaration` (computed) → `PriorFieldsOnlyScope()`. The checker's "enter scope / exit scope" dance becomes: read the construct's `ScopeRule`, push the appropriate frame, process children, pop.

### 2.4 Modifier Validation as Constraint Expressions

**What it is:** Instead of the checker having handwritten modifier validation logic (Slice 7: applicability, conflicts, subsumption, bounds), the Modifiers catalog already declares all the rules — `ApplicableTo`, `MutuallyExclusiveWith`, `Subsumes`. The checker should be a *generic constraint evaluator* that iterates modifier metadata and checks each declared constraint, not a bespoke validator per modifier property.

**What catalog metadata would drive it:** Already exists! `FieldModifierMeta.ApplicableTo`, `.MutuallyExclusiveWith`, `.Subsumes`. The opportunity is *not* to add metadata — it's to recognize that the checker for modifiers should be ~20 lines of generic loop, not a dedicated slice.

**Why traditional compilers don't do this:** Because modifier semantics in C#/Java/Rust are deeply entangled with the type system (access modifiers affect visibility resolution, `static` affects dispatch, `async` changes return types). In Precept, modifiers are *constraints on field values* — they don't change the type system, they just restrict it. Pure declarative validation.

**Concrete example:**
```csharp
// Generic modifier validator (all of Slice 7 in ~20 lines):
foreach (var modifier in field.Modifiers)
{
    var meta = Modifiers.GetMeta(modifier);
    if (meta is FieldModifierMeta fm)
    {
        if (fm.ApplicableTo.Length > 0 && !fm.ApplicableTo.Any(t => t.Matches(field.ResolvedType)))
            emit(Inapplicable);
        foreach (var conflict in fm.MutuallyExclusiveWith)
            if (field.Modifiers.Contains(conflict)) emit(Conflict);
        foreach (var subsumed in fm.Subsumes)
            if (field.Modifiers.Contains(subsumed)) emit(Redundant);
    }
}
```

### 2.5 TypeCategory-Driven Validation Routing

**What it is:** Many validation rules apply to type *categories* (all collections, all temporals, all business-domain types), not individual types. The `TypeCategory` on `TypeMeta` could drive category-level validation without per-type branching.

**What catalog metadata would drive it:** `TypeMeta.TypeCategory` already exists (Scalar, Temporal, BusinessDomain, Collection). The opportunity: validation routines indexed by category rather than per-type switches.

**Why traditional compilers don't do this:** Because type categories in general-purpose languages don't have uniform validation rules. Value types vs reference types have different *boxing* behavior, not different *validation* behavior. In Precept, all collections share the same action-applicability rules, all business-domain types share qualifier validation, all temporals share component-accessor validation patterns.

**Concrete example:** Instead of checking "is this a collection? does it support the `clear` action?" per-type, the action applicability check resolves entirely through `ActionMeta.ApplicableTo` against `TypeCategory`.

---

## 3. Right-Sizing Assessment

### 3.1 Overload Resolution: Over-Engineered

The 7-step overload resolution algorithm (arity → exact → widened → context retry → ambiguity) is designed for a *general-purpose* function system. Precept has:
- 15 functions
- Max 5 overloads per function
- No user-defined functions
- Overloads always differ by type family (numeric vs temporal vs business-domain)

**Right-sized alternative:** A flat lookup table `FrozenDictionary<(FunctionKind, TypeKind[]), FunctionOverload>` precomputed at startup from `Functions.All`. Include widened variants in the table (they're finite). The entire "algorithm" becomes one dictionary lookup. Ambiguity is impossible because the catalog is hand-curated and non-conflicting.

### 3.2 Widening Fallback Cascade: Over-Engineered

The 6-step widening fallback (direct → left → right → both) is designed for a type system with complex subtyping hierarchies. Precept has:
- 1 type that widens: `integer → [decimal, number]`
- No user-defined conversions
- No variance
- No union types

The widening graph has **3 edges total.** A priority cascade is solving a 3-item problem with an algorithm designed for unbounded graphs. Precompute all widened operation entries into the same index that holds exact entries. No fallback needed.

### 3.3 Error Recovery: Appropriately-Sized

The `TypedErrorExpression` + error propagation design is correctly sized. Even at Precept's scale, LS responsiveness requires always-produce-partial-results. This is not over-engineering — it's a requirement for tooling integration.

### 3.4 SemanticIndex Shape: Appropriately-Sized

The array-primary + frozen-dict-secondary pattern is correct. Declaration order matters. O(1) lookup matters. This isn't over-engineered; it's the right dual representation.

### 3.5 CheckContext / Scope Management: Slightly Over-Engineered

A mutable walker with scope stack, current-field-index tracking, and field-scope-mode enum — this is the Roslyn pattern for deeply nested lexical scopes. Precept has *flat* declarations with at most 2 levels (construct → expression). The "scope stack" is never deeper than 2 (event args + quantifier binding). A simpler model: just pass `ResolverContext` as a parameter with the current visibility set, immutably. No stack, no mutations, no cleanup-on-exit.

### 3.6 Qualifier Disambiguation: Correctly-Sized

The ~15 lines of qualifier disambiguation after `FindCandidates` is genuinely structural — qualifier identity is a runtime value the catalog can't know. This is the right amount of code.

### 3.7 The 10-Slice Plan: Over-Scoped for the Surface Area

10 vertical slices for a type checker that checks ~20 types, ~30 operators, ~15 functions is a lot of ceremony. Traditional compilers need this because each slice introduces genuinely new structural challenges. In a catalog-driven system, Slices 2–4 (binary ops, functions, typed constants) are *the same algorithm* hitting different catalog indexes. They could be one slice because the *checker doesn't know the difference.* The catalog knows the difference.

---

## 4. Creative Proposals

### 4.1 The "No Resolve Function" Architecture

**Radical proposal:** Eliminate the `Resolve()` switch entirely. Replace it with a table-driven expression evaluator:

```csharp
// Each ExpressionFormKind maps to a resolution strategy at startup:
FrozenDictionary<ExpressionFormKind, IResolutionStrategy> Strategies;

// The Resolve function:
TypedExpression Resolve(Expression expr, TypeKind? expected) =>
    Strategies[expr.FormKind].Resolve(expr, expected, context);
```

Where `IResolutionStrategy` implementations are *generic* — a `CatalogLookupStrategy` handles binary ops, functions, AND accessors through a single code path parameterized by which catalog to query. The per-form differences are metadata in the strategy table, not code in a switch.

This inverts the traditional "one match arm per form" pattern into "one strategy class per *resolution shape*, shared across forms." Since Precept has only 4 resolution shapes (catalog lookup, fixed type, propagation, structural), the entire expression resolver is 4 small classes + a dispatch dictionary.

### 4.2 The "Closed-World Operation Index" 

**Radical proposal:** Since Precept's type system is *completely closed* (no user-defined types, no generics, no type parameters), precompute the *entire* type-checking result space at startup:

```csharp
// Every possible expression-form type resolution, precomputed:
FrozenDictionary<(OperatorKind, TypeKind, TypeKind), OperationResult> BinaryResults;
FrozenDictionary<(OperatorKind, TypeKind), OperationResult> UnaryResults;
FrozenDictionary<(FunctionKind, TypeKind[]), FunctionResult> FunctionResults;
FrozenDictionary<(TypeKind, string), AccessorResult> AccessorResults;
```

The type checker's "expression resolution" becomes: resolve sub-expressions to types → look up the result in a precomputed table → done. No algorithm. No widening cascade. No overload scoring. The table IS the type checker.

This is something no traditional compiler can do because user-defined types make the table infinite. Precept's closed type system makes it finite (and small — hundreds of entries, not thousands).

### 4.3 The "Declaration-Shape-Driven Checker"

**Radical proposal:** Instead of the checker walking AST nodes by type and manually knowing "a guard must be boolean, a message must be string, actions must target valid fields" — put these constraints *on the ConstructMeta*:

```csharp
// On ConstructMeta — what the checker needs to know per construct:
public sealed record ConstructCheckingShape(
    ImmutableArray<SlotConstraint> SlotConstraints
);

public sealed record SlotConstraint(
    ConstructSlotKind Slot,
    TypeKind? RequiredResultType,    // e.g., GuardClause → Boolean
    ValidationRule[] Rules           // structural rules (e.g., must-reference-valid-state)
);
```

The checker's declaration normalization (Sub-pass 2b) becomes: iterate the construct's slots, resolve each slot's expressions, validate each slot against its declared constraints. No per-construct-kind code. Adding a new construct kind with a new slot pattern *automatically* gets checking behavior from its slot constraints.

### 4.4 The "Inferred Diagnostic Catalog"

**Radical proposal:** Many type-checker diagnostics are *deterministic consequences* of catalog rules. "Modifier X is inapplicable to type Y" is not a diagnostic the checker *decides* to emit — it's a *mathematical fact* derivable from `ModifierMeta.ApplicableTo`. The diagnostic catalog could include a `DerivationSource`:

```csharp
// Diagnostic metadata enhanced:
public sealed record DiagnosticMeta(
    ...,
    DiagnosticDerivation? Derivation = null
);

public abstract record DiagnosticDerivation;
public sealed record ModifierApplicabilityViolation(ModifierKind Modifier) : DiagnosticDerivation;
public sealed record OperationTypeMismatch(OperatorKind Op) : DiagnosticDerivation;
public sealed record FunctionArityMismatch(FunctionKind Fn) : DiagnosticDerivation;
```

This makes diagnostics *traceable back to the catalog rule they enforce.* Tooling can auto-generate "quick fix" suggestions because it knows *which* catalog constraint was violated. The LS can say "this is illegal because the Operations catalog has no entry for (/, string, string)" — citing the catalog as authority.

### 4.5 The "Type Checker as Catalog Consumer Only"

**Most radical proposal:** What if the type checker has *no domain knowledge at all?* What if it's truly generic machinery that takes:
1. A syntax tree
2. A set of catalogs
3. A set of resolution strategies (indexed by catalog)

And produces a SemanticIndex by pure mechanical application of catalog queries?

The test: **could you swap in different catalogs (different types, different operators, different functions) and get a working type checker for a different DSL?** If yes, the checker is truly catalog-driven. If no, it still harbors domain knowledge.

Current answer: *almost* yes. The structural code (scope rules, cycle detection, choice validation) is Precept-specific. But if scope rules were declared on constructs, cycles were detected by a generic graph utility, and choice validation was driven by TypeMeta, then the checker would be fully generic.

This is the *ultimate* catalog-driven design: the type checker is a library, not an application. It's parameterized by metadata, not specialized to Precept. A "miniature type-checking framework" that happens to be configured for Precept's surface. Absurd for Roslyn. Perfect for a 20-type, 30-operator DSL.

---

## 5. Recommendations

Prioritized by impact and alignment with the catalog-driven principle:

| # | Recommendation | Impact | Effort | Priority |
|---|---|---|---|---|
| 1 | **Precompute all operation resolution at startup** — build a frozen `(op, lhs, rhs) → result` table including widened variants. Eliminate the widening fallback algorithm entirely. | HIGH — removes ~40 lines of cascading search logic; makes resolution O(1) | LOW — iterate `Operations.All` × `WidensTo` at startup | **P0** |
| 2 | **Precompute all function resolution at startup** — build a frozen `(name, argTypes[]) → overload` table including widened variants. Eliminate the overload scoring algorithm. | HIGH — removes the 7-step resolution algorithm | LOW — ~60 overloads × widening variants | **P0** |
| 3 | **Declare scope rules on ConstructMeta** — add `ScopeRule?` to the construct catalog. The checker reads it instead of hardcoding per-construct scope setup. | MEDIUM — eliminates scope-management code, makes scope rules visible to tooling and MCP | LOW — 4 scope rules to declare | **P1** |
| 4 | **Add ResolutionShape to ExpressionFormMeta** — make expression resolution strategy metadata-declared. The Resolve function dispatches by strategy, not by AST type. | MEDIUM — shrinks Resolve from 350 lines to ~80 | MEDIUM — need to define and implement ~4 strategy types | **P1** |
| 5 | **Recognize modifier validation as a generic loop** — don't treat Slice 7 as a separate "module." It's 20 lines of generic constraint checking over catalog metadata. Plan it as such. | LOW-MEDIUM — right-sizes the slice plan | ZERO — just reframe the implementation approach | **P1** |
| 6 | **Consider construct-declared slot constraints** (§4.3) — for sub-pass 2b's declaration normalization. Lower priority because it's a bigger abstraction change. | MEDIUM — eliminates per-construct normalization code | MEDIUM — needs SlotConstraint design | **P2** |
| 7 | **Consider precomputed accessor resolution** — build `(TypeKind, accessorName) → AccessorResult` at startup. Trivially finite. | LOW — accessor lookups are already fast | LOW | **P2** |

### What NOT to change:

- **Error recovery policy** — correctly designed, keep as-is
- **SemanticIndex shape** — correctly designed, keep as-is
- **Qualifier disambiguation** — genuinely structural, keep as-is
- **2-pass architecture** — correct for symbol resolution before expression checking
- **`[HandlesCatalogMember]` enforcement** — correct safety mechanism

### Summary principle:

The recurring theme is: **Precept's type system is closed and small enough to precompute.** Traditional compilers compute at check-time because they must. Precept's catalogs know every possible type-checking question and its answer. The design should exploit closure more aggressively — trade initialization-time precomputation for check-time simplicity. The checker should feel less like "an algorithm that searches" and more like "a lookup engine that queries precomputed answers."

---

*This review does not propose code generation. It proposes that the type checker's design lean harder into the implications of a fully-closed, metadata-described type system — which traditional compiler training bias systematically underweights because no mainstream compiler has one.*
