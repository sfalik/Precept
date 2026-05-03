# Radical Type Checker Design: The Catalog IS the Type System

**Author:** Frank (Lead/Architect)
**Date:** 2026-05-03
**Status:** Design sketch — input for type-checker rebuild decision
**Builds on:** `parser-radical.md` §7, `frank-catalog-driven-typechecker-review.md`, `docs/compiler/type-checker.md`

---

## §0. The Grammar of Precept (Summary)

The type checker validates the *typed structure* produced by the parser, so it must understand construct shape. This section summarizes the grammar's fundamental pattern — see `parser-radical.md §0` for the full illustration with examples.

**The shape:** Every Precept construct is a leading keyword followed by a flat sequence of typed slots. No recursive nesting at the construct level. The entire grammar is a table of `Leader → Slot → Slot → ... → [Block]` patterns.

**Slot types** the checker must validate: identifiers (field/state/event names), type references (with qualifiers), expressions (guards, conditions, computed values), modifier keywords (constraint annotations), action chains (state-mutating statements), and outcome blocks (transition/reject). See `parser-radical.md §0.7` for the four-concept relationship (constructs, actions, outcomes, slots) and `§0.8` for the revised architectural ruling: outcomes use a two-level pattern (DU for parsing, catalog for enumeration/metadata), same as Actions.

**Construct families:** Some keywords (`in`, `on`, `from`) introduce multiple construct shapes disambiguated by a secondary token. The parser resolves this; the checker receives a fully-typed node with named slots already extracted.

**Why it matters for the type checker:** Because constructs are flat slot sequences — not recursive trees — the checker can validate each slot position independently. A `Tag("condition", ExprProd())` slot always produces an expression that needs boolean-type checking. A `Tag("type", TypeRefProd())` slot always produces a type reference that needs existence/applicability checking. The checker's job is per-slot validation dispatched by slot type — the same "interpret the shape" principle that drives the parser.

---

## 1. The Bet

The existing type-checker design is good. The 11-slice plan covers the right surface area. The SemanticIndex shape is correct. The 2-pass architecture is correct. But the *model* underneath is still shaped by how Roslyn works.

The Roslyn model: the type checker is a recursive AST visitor. It dispatches on node kind, consults metadata for individual facts, and accumulates results. The type system lives in the *checker* — in the arms of `Resolve()`.

The Precept model should be different, for exactly the same reason the parser radical works: **the catalog already contains every answer.** Not facts the checker consults — *complete answers.* Every legal binary expression triple. Every function overload. Every accessor return type. Every widening path. Every action applicability rule. The catalog is not a metadata adjunct to the type checker. The catalog *is* the type system.

The bet: replace `FindCandidates` + widening-fallback cascade + overload-scoring algorithm with a startup precomputed index built from catalog at initialization time. The type resolution pass becomes pure table lookup — not search, not scoring, not retry. One dictionary hit per expression form. The Resolve() function shrinks from ~350 lines to ~80 lines. The "type system" is in the catalog where it belongs, and the checker is a thin query engine over precomputed answers.

The radical insight is the same one that drove the parser: **encode, don't compute.** Traditional compilers compute type answers at check time because user-defined types make the answer set open. Precept's type system is *closed*. Every (op, lhs, rhs) → result triple that will ever exist can be enumerated at startup. There are approximately 300 of them. A `FrozenDictionary` with 300 entries is instantiated once and queried for the lifetime of the process.

---

## 2. Core Model: The TypeCatalogIndex

The traditional checker holds a `Resolve()` function with one arm per node type. The radical checker holds a `TypeCatalogIndex` — a startup-built structure of precomputed frozen tables — and a `Resolve()` that dispatches to one of four resolution strategies, reads results from tables, and assembles typed nodes.

The `TypeCatalogIndex` is built once at static initialization time, before any source file is checked. It never changes. It is not per-file state. It is the compiled form of the entire Precept type system.

```csharp
/// <summary>
/// Precomputed type resolution tables, built from catalog metadata at startup.
/// Shared across all TypeChecker instances. Thread-safe (all frozen collections).
/// </summary>
public sealed class TypeCatalogIndex
{
    // ── Binary operation results (exact + all widened variants baked in) ──────
    public FrozenDictionary<BinaryKey, ResolvedBinaryOp> BinaryOps { get; }

    // ── Unary operation results ────────────────────────────────────────────────
    public FrozenDictionary<UnaryKey, ResolvedUnaryOp> UnaryOps { get; }

    // ── Function overload results (exact + all widened variants baked in) ─────
    public FrozenDictionary<FunctionKey, ResolvedFunction> FunctionOverloads { get; }

    // ── Accessor results (by type + accessor name) ────────────────────────────
    public FrozenDictionary<AccessorKey, ResolvedAccessor> Accessors { get; }

    // ── Action applicability (by action kind + field type) ────────────────────
    public FrozenDictionary<ActionApplicabilityKey, bool> ActionApplicability { get; }

    // ── Resolution strategy by expression form ────────────────────────────────
    public FrozenDictionary<ExpressionFormKind, IResolutionStrategy> Strategies { get; }

    // ── Scope rule by construct kind ──────────────────────────────────────────
    public FrozenDictionary<ConstructKind, ScopeRule> ScopeRules { get; }

    // ── Singleton (built once, shared everywhere) ─────────────────────────────
    public static readonly TypeCatalogIndex Instance = Build();
}
```

Key types:

```csharp
public readonly record struct BinaryKey(OperatorKind Op, TypeKind Left, TypeKind Right);
public readonly record struct UnaryKey(OperatorKind Op, TypeKind Operand);
public readonly record struct FunctionKey(FunctionKind Kind, TypeKind Arg0, TypeKind Arg1 = TypeKind.Void, TypeKind Arg2 = TypeKind.Void);
public readonly record struct AccessorKey(TypeKind OwnerType, string AccessorName);
public readonly record struct ActionApplicabilityKey(ActionKind Action, TypeKind FieldType);

public sealed record ResolvedBinaryOp(BinaryOperationMeta Meta, MatchQuality Quality);
public sealed record ResolvedUnaryOp(UnaryOperationMeta Meta);
public sealed record ResolvedFunction(FunctionMeta Meta, FunctionOverload Overload, MatchQuality Quality);
public sealed record ResolvedAccessor(TypeAccessor Accessor, TypeKind ReturnType, TypeKind? ParameterType);

public enum MatchQuality { Exact, LeftWidened, RightWidened, BothWidened, LeftAndArg0Widened, /* etc. */ }
```

This is the entire type system. Not a 350-line function with 16 arms — a table.

---

## 3. Building the Index: Startup Precomputation

The startup builder is the only place that "knows" the widening rules. After it runs, neither the checker nor anything else needs to think about widening. Widened triples are just entries in the same table as exact triples.

### 3.1 Binary Operation Index

```csharp
static void BuildBinaryOps(Dictionary<BinaryKey, ResolvedBinaryOp> index)
{
    foreach (var kind in Enum.GetValues<OperationKind>())
    {
        var meta = Operations.GetMeta(kind);
        if (meta is not BinaryOperationMeta bin) continue;

        var lhsType = bin.Left.TypeKind;
        var rhsType = bin.Right.TypeKind;
        var op      = bin.Operator;

        // Exact entry
        TryAdd(index, new BinaryKey(op, lhsType, rhsType), new(bin, Exact));

        // Left-widened entries
        foreach (var wl in Types.GetMeta(lhsType).WidensTo)
            TryAdd(index, new BinaryKey(op, lhsType, rhsType), new(bin, LeftWidened));
            // Wait — we want (op, originalLhs, rhs) → bin? No. We want (op, widenedLhs, rhs)
            // mapped under the ORIGINAL key so the checker passes original types and gets
            // back the resolved op. Actually: store under ORIGINAL types, return meta that
            // says "left will be widened to wl before dispatch."
            // More precisely: index[(op, wl, rhs)] = exact;  index[(op, lhsType, rhs)] = left-widen
            // The key is the CALL SITE types; the value says what op was chosen and what widening occurred.
```

Wait. Let me be precise about the key semantics. The caller has `(op, lhsType, rhsType)` where both are the *caller's actual types*, potentially before widening. The table maps caller types → resolution result. So to support `integer + decimal → decimal`, we need `(Plus, Integer, Decimal)` → `ResolvedBinaryOp(AddDecimalDecimal, LeftWidened)`. The builder pre-populates widened keys:

```csharp
static void BuildBinaryOps(Dictionary<BinaryKey, ResolvedBinaryOp> index)
{
    foreach (var kind in Enum.GetValues<OperationKind>())
    {
        var meta = Operations.GetMeta(kind);
        if (meta is not BinaryOperationMeta bin) continue;

        var op  = bin.Operator;
        var lt  = bin.Left.TypeKind;    // exact left param type
        var rt  = bin.Right.TypeKind;   // exact right param type

        // Exact: (op, lt, rt)
        TryAdd(index, new(op, lt, rt), new(bin, Exact));

        // Left-widened: (op, narrower, rt) — for each type that widens to lt
        foreach (var narrowL in SourcesOf(lt))
            TryAdd(index, new(op, narrowL, rt), new(bin, LeftWidened));

        // Right-widened: (op, lt, narrower)
        foreach (var narrowR in SourcesOf(rt))
            TryAdd(index, new(op, lt, narrowR), new(bin, RightWidened));

        // Both-widened: (op, narrowL, narrowR)
        foreach (var narrowL in SourcesOf(lt))
            foreach (var narrowR in SourcesOf(rt))
                TryAdd(index, new(op, narrowL, narrowR), new(bin, BothWidened));
    }
}

// SourcesOf: types whose WidensTo includes the given target
// Precomputed from Types.All: integer.WidensTo = [decimal, number] → SourcesOf(decimal) = [integer]
static IReadOnlyList<TypeKind> SourcesOf(TypeKind target) => _wideningSources[target];
```

`TryAdd` uses `Exact` priority — if the exact entry already exists, don't overwrite with a widened entry. Since `OperationKind.AddDecimalDecimal` is a catalog entry with exact (Decimal, Decimal) params, it wins over a widened entry at the same key. The `Exact` quality gets priority.

The entire widening fallback cascade — left-first, right-first, both, priority ordering — dissolves into a build-time loop. Check time is one dictionary lookup. No retry. No cascading search.

**How many entries?** Precept has ~80 binary operations × widening variants. `integer` widens to `decimal` and `number` (2 targets), so each (integer, X) entry generates at most 2 additional left-widened entries. The total is well under 500 entries. This is not a performance concern — it's a compile-time table that fits in L1 cache.

### 3.2 Function Overload Index

```csharp
static void BuildFunctionOverloads(Dictionary<FunctionKey, ResolvedFunction> index)
{
    foreach (var kind in Enum.GetValues<FunctionKind>())
    {
        var meta = Functions.GetMeta(kind);
        foreach (var overload in meta.Overloads)
        {
            var paramTypes = overload.Parameters.Select(p => p.TypeKind).ToArray();
            var exactKey   = MakeFunctionKey(kind, paramTypes);

            TryAdd(index, exactKey, new(meta, overload, Exact));

            // Add widened variants — same SourcesOf logic
            foreach (var (widenedKey, quality) in WideningVariants(kind, paramTypes))
                TryAdd(index, widenedKey, new(meta, overload, quality));
        }
    }
}
```

The 7-step overload resolution algorithm (arity filter → exact → widened → context retry → ambiguity) collapses to one lookup. There are ~15 functions × ~5 overloads × widening variants ≈ maybe 120 total entries. A dictionary with 120 entries. The "algorithm" is a hash function.

### 3.3 Accessor Index

```csharp
static void BuildAccessors(Dictionary<AccessorKey, ResolvedAccessor> index)
{
    foreach (var kind in Enum.GetValues<TypeKind>())
    {
        var meta = Types.GetMeta(kind);
        foreach (var accessor in meta.Accessors)
        {
            // Return type: FixedReturnAccessor → accessor.Returns;
            //              base TypeAccessor   → element type (stored as ElementTypeOf(kind));
            //              ElementParameterAccessor → TypeKind.Integer
            var returnType = ComputeAccessorReturn(accessor, meta);
            var paramType  = accessor.ParameterType;
            index[new(kind, accessor.Name)] = new(accessor, returnType, paramType);
        }
    }
}
```

`TypeMeta.ElementType` (new field, see §4.1) gives the index enough information to compute `FixedReturnAccessor`-vs-element-type returns at build time rather than at check time. Accessor lookup during checking: one dictionary hit. No switch on accessor DU subtype in the checker.

### 3.4 Action Applicability Index

```csharp
static void BuildActionApplicability(Dictionary<ActionApplicabilityKey, bool> index)
{
    foreach (var kind in Enum.GetValues<ActionKind>())
    {
        var meta = Actions.GetMeta(kind);
        foreach (var typeKind in Enum.GetValues<TypeKind>())
        {
            var applicable = meta.ApplicableTo.Length == 0   // empty = any type
                || meta.ApplicableTo.Any(target => target.Matches(typeKind));
            index[new(kind, typeKind)] = applicable;
        }
    }
}
```

Action type-checking becomes one lookup. No `meta.ApplicableTo.Any(...)` scan at check time.

---

## 4. Catalog Metadata Expansions

The existing catalogs are already close to sufficient. Three additions make the type checker fully self-sufficient.

### 4.1 `ResolutionShape` on `ExpressionFormMeta`

This is the key addition. Instead of a 16-arm switch in `Resolve()`, each `ExpressionFormMeta` carries a *resolution shape* — a declarative description of how its type is computed:

```csharp
public sealed record ExpressionFormMeta(
    ExpressionFormKind        Kind,
    ExpressionCategory        Category,
    bool                      IsLeftDenotation,
    IReadOnlyList<TokenKind>  LeadTokens,
    string                    HoverDocs,
    ResolutionShape           Resolution   // NEW
);

// DU of resolution strategies:
public abstract sealed class ResolutionShape { }

/// Resolved by consulting a precomputed catalog table (binary ops, unary ops,
/// functions, accessors). Strategy parameter tells which table to query.
public sealed class TableLookupShape(TableLookupStrategy Strategy) : ResolutionShape;

/// Always returns the same result type regardless of operand types.
public sealed class FixedResultShape(TypeKind Result) : ResolutionShape;

/// Result type propagated from sub-expressions (grouped expression, conditional branches).
public sealed class PropagationShape(PropagationRule Rule) : ResolutionShape;

/// Resolved structurally — name lookup in scope (identifier), literal context rules (literal).
public sealed class StructuralShape() : ResolutionShape;

public enum TableLookupStrategy  { BinaryOp, UnaryOp, FunctionCall, MemberAccess, MethodCall }
public enum PropagationRule      { PassThrough, UnifyBranches }
```

The `ExpressionForms.GetMeta` switch gains one field per entry:

```
Literal         → StructuralShape()                              // context-sensitive literal resolution
Identifier      → StructuralShape()                              // symbol table lookup
Grouped         → PropagationShape(PassThrough)                  // result = inner expression type
BinaryOperation → TableLookupShape(BinaryOp)                     // → BinaryOps table
UnaryOperation  → TableLookupShape(UnaryOp)                      // → UnaryOps table
MemberAccess    → TableLookupShape(MemberAccess)                  // → Accessors table
MethodCall      → TableLookupShape(MethodCall)                    // → Accessors table (param check)
Conditional     → PropagationShape(UnifyBranches)                // left/right branch unification
FunctionCall    → TableLookupShape(FunctionCall)                  // → FunctionOverloads table
ListLiteral     → StructuralShape()                              // element unification
PostfixOperation → FixedResultShape(TypeKind.Boolean)            // is set / is not set
Quantifier      → FixedResultShape(TypeKind.Boolean)             // any / all / none
CIFunctionCall  → TableLookupShape(FunctionCall)                  // → FunctionOverloads (CI variant)
```

The `TypeCatalogIndex.Strategies` map is populated at startup from this data. `Resolve()` dispatches by calling `Strategies[expr.FormKind].Resolve(expr, context)`. That's the entire dispatch mechanism.

### 4.2 `ScopeRule` on `ConstructMeta`

The `CheckContext` manages scope by procedurally setting and clearing `CurrentEventArgs`, `CurrentFieldIndex`, and `FieldScopeMode`. This is implicit knowledge baked into the checker. The catalog should declare it.

```csharp
// New field on ConstructMeta:
ScopeRule? CheckerScope = null

// DU:
public abstract sealed class ScopeRule { }
public sealed class AllFieldsScope()                                  : ScopeRule;
public sealed class PriorFieldsOnlyScope()                            : ScopeRule;
public sealed class EventArgScope(ConstructSlotKind EventNameSlot)    : ScopeRule;
```

Construct-to-scope declarations:

```
FieldDeclaration   (computed expression) → PriorFieldsOnlyScope()
TransitionRow                            → EventArgScope(EventTarget)
EventHandler                             → EventArgScope(EventTarget)
EventEnsure                              → EventArgScope(EventTarget)
RuleDeclaration                          → AllFieldsScope()
StateEnsure                              → AllFieldsScope()
AccessMode                               → AllFieldsScope()
StateAction                              → AllFieldsScope()
```

The checker's "enter construct / exit construct" dance becomes:

```csharp
var scope = catalogIndex.ScopeRules.TryGetValue(construct.Kind, out var s) ? s : new AllFieldsScope();
using var frame = context.PushScope(scope, construct);
// ... process children
// frame.Dispose() restores previous scope
```

No per-construct-kind scope management in the checker. Adding a new construct with event-arg scope requires one metadata entry, not a checker change.

### 4.3 `ElementType` on `TypeMeta`

The Accessor index builder (§3.3) needs to know, at build time, what the element type of a collection type is — without consulting a per-instance `TypedField`. For collection types, `TypeMeta` should declare the element type placeholder:

```csharp
// New field on TypeMeta (collections only; null for non-collections):
TypeKind? ElementTypeKind = null
```

For concrete collection types (`set`, `queue`, `stack`, etc.) this is always their element type — declared in the field type ref. At `TypeMeta` level, we record whether the type *has* an element type (non-null). The actual element type comes from the `TypedField.ElementType` at check time. The index builder uses this flag to decide which accessor return-type computation path to use. The accessor result carries `ReturnType = TypeKind.Element` as a sentinel when the return type is the owning collection's element type:

```csharp
public sealed record ResolvedAccessor(
    TypeAccessor Accessor,
    TypeKind     ReturnType,     // TypeKind.Element = "same as collection's element type"
    TypeKind?    ParameterType
);
```

The checker resolves `TypeKind.Element` at call time using `TypedField.ElementType`. This is the one place where a table value is a symbolic sentinel rather than a concrete answer — acceptable because collection element types are always field-specific.

### 4.4 `ContentValidation` on `TypeMeta` (Already Locked)

This is already specified in `docs/compiler/type-checker.md` Gap 1. The radical design carries it forward unchanged. Typed constant validation is catalog-driven through `ContentValidation?` on `TypeMeta`. No per-`TypeKind` switch in the checker.

---

## 5. The Resolution Strategies

With `ResolutionShape` metadata and the `TypeCatalogIndex`, `Resolve()` has four strategy implementations. These are not a 16-arm switch — they are four small classes that the dispatch table routes to.

### 5.1 The `TableLookupStrategy`

All of binary ops, unary ops, functions, and accessors go through this. The generic logic:

```csharp
sealed class TableLookupStrategy(TableLookupStrategy Strategy) : IResolutionStrategy
{
    public TypedExpression Resolve(Expression expr, TypeKind? expected, ResolverContext ctx)
    {
        var (operandTypes, operandExprs) = ResolveOperands(expr, ctx);

        return Strategy switch
        {
            BinaryOp =>
                ctx.Index.BinaryOps.TryGetValue(MakeBinaryKey(expr, operandTypes), out var op)
                    ? BuildTypedBinaryOp(op, operandExprs, ctx)
                    : HandleQualifierDisambiguation(expr, operandTypes, operandExprs, ctx),

            UnaryOp =>
                ctx.Index.UnaryOps.TryGetValue(MakeUnaryKey(expr, operandTypes), out var op)
                    ? BuildTypedUnaryOp(op, operandExprs, ctx)
                    : EmitError(DiagnosticCode.NoMatchingOperation, expr, ctx),

            FunctionCall =>
                ctx.Index.FunctionOverloads.TryGetValue(MakeFunctionKey(expr, operandTypes), out var fn)
                    ? BuildTypedFunctionCall(fn, operandExprs, ctx)
                    : HandleLiteralRetry(expr, operandTypes, operandExprs, expected, ctx),

            MemberAccess =>
                ctx.Index.Accessors.TryGetValue(MakeAccessorKey(expr, operandTypes), out var acc)
                    ? BuildTypedMemberAccess(acc, operandExprs, ctx)
                    : EmitError(DiagnosticCode.UnknownMember, expr, ctx),

            MethodCall =>
                ctx.Index.Accessors.TryGetValue(MakeAccessorKey(expr, operandTypes), out var acc)
                    ? BuildTypedMethodCall(acc, operandExprs, ctx)
                    : EmitError(DiagnosticCode.UnknownMember, expr, ctx),
        };
    }
}
```

**The qualifier disambiguation caveat:** `FindCandidates` currently returns a span for multi-qualifier entries (e.g., money/money division has both `QualifierMatch.Same` and `QualifierMatch.Different` entries). The precomputed table can't collapse these — qualifier identity is a call-site value. The solution: for operations with multiple qualifier variants, store both in the table under distinct keys using a qualifier-sensitive key:

```csharp
public readonly record struct BinaryKey(OperatorKind Op, TypeKind Left, TypeKind Right, QualifierMatch QualifierHint = Any);
```

`QualifierMatch.Any` is the default lookup key (exact type match, qualifier-agnostic). The checker first tries `Any`; if that returns `null`, it tries both `Same` and `Different` entries and applies the ~15-line qualifier disambiguation logic. Multi-qualifier entries are rare (division and a handful of comparison ops on business-domain types) — the fast path handles ~95% of expressions in one hit.

### 5.2 The `FixedResultStrategy`

```csharp
sealed class FixedResultStrategy(TypeKind Result) : IResolutionStrategy
{
    public TypedExpression Resolve(Expression expr, TypeKind? expected, ResolverContext ctx)
    {
        // Resolve sub-expressions for error propagation, but result type is fixed.
        var operands = ResolveSubExpressions(expr, ctx);
        return new TypedFixedResult(Result, operands, expr);
    }
}
```

`PostfixOperation` and `Quantifier` both land here. The checker has zero knowledge of what `is set` means semantically — it just knows the result is boolean. The catalog says so.

### 5.3 The `PropagationStrategy`

```csharp
sealed class PropagationStrategy(PropagationRule Rule) : IResolutionStrategy
{
    public TypedExpression Resolve(Expression expr, TypeKind? expected, ResolverContext ctx)
    {
        return Rule switch
        {
            PassThrough    => ResolveInner(expr, expected, ctx),      // grouped: pass expected through
            UnifyBranches  => ResolveConditional(expr, expected, ctx) // if/else: unify branch types
        };
    }

    // UnifyBranches: resolve condition (must be boolean), resolve then/else,
    // result = common type; if branches disagree, emit diagnostic, return error.
}
```

### 5.4 The `StructuralStrategy`

This is the one strategy with actual logic — identifier resolution, literal context resolution, list literal element unification. It's the only strategy that touches the symbol table. It's also the only one that changes between constructs (because the scope rule affects identifier resolution).

```csharp
sealed class StructuralStrategy : IResolutionStrategy
{
    public TypedExpression Resolve(Expression expr, TypeKind? expected, ResolverContext ctx)
        => expr switch
        {
            IdentifierExpression id      => ResolveIdentifier(id, ctx),
            LiteralExpression    lit     => ResolveLiteral(lit, expected, ctx),
            TypedConstantExpression tc   => ResolveTypedConstant(tc, expected, ctx),
            InterpolatedStringExpression interp => ResolveInterpolated(interp, ctx),
            InterpolatedTypedConstantExpression itc => ResolveInterpolatedTypedConstant(itc, ctx),
            ListLiteralExpression list   => ResolveListLiteral(list, ctx),
            _                            => throw new UnreachableException()
        };
}
```

The `StructuralStrategy` has a modest internal switch — but it covers only the 6 node types that genuinely need structural logic. The catalog-table strategies cover the other 7 form kinds. Compare this to the traditional `Resolve()` with 16+ arms — more than half of those arms are now table lookups, not code.

`ResolveIdentifier` is the heart of the structural strategy:
1. Check `QuantifierBindings` (innermost wins)
2. Check `CurrentEventArgs` (if scope is event-arg)
3. Check `FieldLookup`, gated by `ctx.Scope` (PriorFieldsOnly → index check)
4. Emit `UnresolvedIdentifier`, return `TypedErrorExpression`

`ResolveLiteral` applies the numeric context rule: default integer, one retry with `expected` type if first resolution fails — the same logic as today, but isolated to this one function, not spread across binary op resolution.

---

## 6. The `Resolve()` Function

The entire public-facing `Resolve()` function, after all the strategy machinery is in place:

```csharp
TypedExpression Resolve(Expression expr, TypeKind? expected, ResolverContext ctx)
{
    // Error propagation: if any sub-expression is already an error, propagate.
    // (Done inside individual strategies when they call ResolveOperands.)

    // Strategy dispatch via catalog:
    var formKind  = ExpressionFormKindOf(expr);    // derived from AST node type, computed once
    var strategy  = _index.Strategies[formKind];
    return strategy.Resolve(expr, expected, ctx);
}
```

That's it. No arms. No switches. No per-form knowledge. The type system is in the catalog — `Strategies[formKind]` is the bridge from AST to catalog.

The `ExpressionFormKindOf` helper maps AST node type to `ExpressionFormKind`:

```csharp
static ExpressionFormKind ExpressionFormKindOf(Expression expr) => expr switch
{
    LiteralExpression or TypedConstantExpression
        or InterpolatedStringExpression or InterpolatedTypedConstantExpression
        or ListLiteralExpression                 => ExpressionFormKind.Literal,
    IdentifierExpression                         => ExpressionFormKind.Identifier,
    ParenthesizedExpression                      => ExpressionFormKind.Grouped,
    BinaryExpression                             => ExpressionFormKind.BinaryOperation,
    UnaryExpression                              => ExpressionFormKind.UnaryOperation,
    MemberAccessExpression                       => ExpressionFormKind.MemberAccess,
    MethodCallExpression                         => ExpressionFormKind.MethodCall,
    ConditionalExpression                        => ExpressionFormKind.Conditional,
    CallExpression                               => ExpressionFormKind.FunctionCall,
    CIFunctionCallExpression                     => ExpressionFormKind.CIFunctionCall,
    IsSetExpression or IsNotSetExpression        => ExpressionFormKind.PostfixOperation,
    QuantifierExpression                         => ExpressionFormKind.Quantifier,
    _ => throw new UnreachableException($"Unknown expression type: {expr.GetType().Name}")
};
```

This is the *only* per-node-type switch in the resolver. It's a pure routing table — 12 arms, each a single line, no logic. It maps syntax structure to catalog identity. That's the right job for a switch.

---

## 7. The Type Resolution Pass

The existing spec's 2-pass architecture is correct and survives. What changes is *what happens inside* Pass 2.

### 7.1 Pass 1: Symbol Registration (Unchanged)

Pass 1 is catalog-driven already. Field TypeRef → `Types.ByTokenKind` lookup → `TypeKind`. State modifiers, event args — all straightforward. No changes needed. The existing design is right.

One small addition: after registering all fields, build the `_fieldIndexByName` map (int position per field name) used for forward-reference gating. This is three lines added to Pass 1 assembly.

### 7.2 Pass 2: Declaration Resolution (Simplified)

The existing spec's three "sub-passes" (2a expression resolution, 2b declaration normalization, 2c structural validation) are a reasonable decomposition but not a required architecture. The radical model collapses 2a and 2b into a single pass:

```
Pass 2 (single linear scan):
  for each declaration in syntaxTree.Declarations:
    using var scope = catalogIndex.ScopeRules.TryGetValue(declaration.Kind, out var sr)
                      ? context.PushScope(sr, declaration) : default;
    match declaration:
        FieldDeclarationNode   → NormalizeField(decl, context)
        TransitionRowNode      → NormalizeTransitionRow(decl, context)
        RuleDeclarationNode    → NormalizeRule(decl, context)
        StateEnsureNode        → NormalizeStateEnsure(decl, context)
        EventEnsureNode        → NormalizeEventEnsure(decl, context)
        AccessModeNode         → NormalizeAccessMode(decl, context)
        StateActionNode        → NormalizeStateAction(decl, context)
        EventHandlerNode       → NormalizeEventHandler(decl, context)
        FieldDeclarationNode / StateDeclarationNode / EventDeclarationNode → skip (done in Pass 1)
```

Scope is pushed/popped via the `ScopeRule` from the catalog — no per-kind scope management code in the checker. Each `NormalizeX` function calls `Resolve()` on contained expressions, validates structural constraints (guard = boolean, message = string), and produces a typed entry.

**Sub-pass 2c (structural validation)** stays separate because it needs the complete symbol table and typed declaration list before it can run:

```
Sub-pass 2c (after 2a/2b completes):
  - Computed field cycle detection (DFS over ComputedExpression field refs)
  - Choice field validation (value sets, duplicates, subset/ordering)
  - Forward-reference belt-and-suspenders check (already gated in identifier resolution)
  - Stateless/stateful cross-validation
  - Initial event field assignment completeness
```

These are genuinely structural — they can't be catalog-driven, and they're not trying to be. This is ~100 lines of graph/set logic. The existing spec's coverage is correct.

### 7.3 Traversal Model: Single Pass with Precomputed Context

There is no "retry" pass, no multi-phase expression resolution, no context-propagation feedback loop. The `expected` parameter flows top-down through `Resolve()` calls in the `StructuralStrategy`. That's the only top-down propagation.

The numeric literal context retry (the mechanism that makes `amount > 100` work when `amount: money`) happens inside `HandleLiteralRetry` — a helper called from `TableLookupStrategy` when the initial lookup misses and one operand is a bare literal. It's ~15 lines. Not a second pass. Not an algorithm. A single retry call with `expected = otherOperand.ResultType`.

**This is the same semantic behavior as the existing spec** (Decision 17: "Integer default + one-retry context propagation"). What changes is that the retry lives inside a single helper function, not as a phase in the overload resolution algorithm.

---

## 8. Action Statement Typing

The radical parser (§7.1 of `parser-radical.md`) replaces 14 per-kind action node types with 8 per-shape types carrying `ActionMeta`. For the type checker, this is unambiguously simpler.

### 8.1 What the Type Checker Sees

Instead of pattern-matching against `SetStatement`, `AddStatement`, `RemoveStatement`, etc. (14 types), the type checker pattern-matches against:

```csharp
ActionStatement          // AssignValue shape: field + value
ActionByStatement        // CollectionValueBy shape: field + value + key
ActionIntoStatement      // CollectionInto/IntoBy: field + optional binding target
ActionFieldOnlyStatement // FieldOnly shape: field (clear)
InsertStatement          // field + value + index
PutStatement             // field + key + value
RemoveAtStatement        // field + index
```

Seven types, not fourteen. And the type checker doesn't care about the shape beyond resolving the expressions it contains. The shape *is* the API contract — `ActionByStatement` always has both a value and a key; `ActionFieldOnlyStatement` has neither.

### 8.2 Kind-Specific Validation via `ActionMeta`

After resolving expressions in an action statement, the kind-specific validation reads from `ActionMeta`:

```csharp
TypedAction TypeCheckAction(Statement stmt, ResolverContext ctx)
{
    var (meta, fieldName, expressions) = DeconstructAction(stmt);

    // 1. Resolve the target field
    if (!ctx.FieldLookup.TryGetValue(fieldName, out var field))
        return EmitActionError(DiagnosticCode.UnresolvedField, stmt, ctx);

    // 2. Applicability check — one table lookup
    if (!ctx.Index.ActionApplicability[new(meta.Kind, field.ResolvedType)])
        return EmitActionError(DiagnosticCode.ActionInapplicable, stmt, ctx);

    // 3. Resolve expressions (value, key, index as applicable)
    var typedExprs = expressions.Select(e => Resolve(e, field.ElementType, ctx)).ToImmutableArray();

    // 4. Classify into TypedAction DU — 3-arm switch on ActionSyntaxShape
    return meta.SyntaxShape switch
    {
        ActionSyntaxShape.FieldOnly
            or ActionSyntaxShape.CollectionInto
            or ActionSyntaxShape.CollectionIntoBy
            => new TypedBindingAction(meta.Kind, fieldName, field.ResolvedType,
                                      binding: ExtractBinding(stmt), ProofReqs(meta, field), stmt),

        ActionSyntaxShape.AssignValue
            or ActionSyntaxShape.CollectionValue
            or ActionSyntaxShape.CollectionValueBy
            or ActionSyntaxShape.InsertAtIndex
            or ActionSyntaxShape.MapKeyValue
            or ActionSyntaxShape.RemoveAtIndex
            => new TypedInputAction(meta.Kind, fieldName, field.ResolvedType,
                                    typedExprs[0], SecondaryExpr(stmt, typedExprs),
                                    SecondaryRole(meta.SyntaxShape), ProofReqs(meta, field), stmt),
    };
}
```

That's the entire action typing implementation. The 3-arm DU classification (base/input/binding) is a stable 2-arm switch because `FieldOnly` and `CollectionInto*` are the only "no primary value" shapes. Everything else carries an input expression. The `meta.Kind` check (step 2) is one table lookup — no `meta.Kind == ActionKind.Add && field.ResolvedType == TypeKind.Set` branching anywhere.

**The shape-based AST from the radical parser pays dividends here.** With 14 per-kind types, the type checker had to inspect each node's kind to figure out what expressions it contained. With 8 per-shape types, the type checker knows the expression layout from the C# type. `ActionByStatement` always has `.Value` and `.Key`. No branching needed to find them.

---

## 9. SemanticIndex Shape

The existing spec's `SemanticIndex` shape is largely correct. The radical design preserves it with one clarification and one small addition.

### 9.1 What Survives Unchanged

All record types from the existing spec survive:
- `TypedField`, `TypedState`, `TypedEvent`, `TypedArg` — unchanged
- `QualifierBinding` DU — unchanged
- `TypedTransitionRow`, `TypedRule`, `TypedEnsure`, `TypedAccessMode`, `TypedStateHook`, `TypedEventHandler`, `TypedEditDeclaration` — unchanged
- `TypedAction`, `TypedInputAction`, `TypedBindingAction` DU — unchanged
- `TypedBinaryOp`, `TypedUnaryOp`, `TypedFunctionCall`, `TypedMemberAccess`, `TypedConditional`, `TypedQuantifier` — unchanged
- `SemanticIndex` record with arrays + derived frozen dicts — unchanged

The radical model is not better served by a different output shape. The SemanticIndex is the API contract with downstream consumers (GraphAnalyzer, ProofEngine, Builder, LS). Those consumers read typed declarations and expressions. The radical model's contribution is *how the index is built*, not what it looks like.

### 9.2 `TypedErrorExpression` (Confirmed Unchanged)

```csharp
public sealed record TypedErrorExpression(
    Expression Syntax
) : TypedExpression(TypeKind.Error, Syntax);
```

This is the central error-handling node. Unchanged. What the radical design contributes is the **debug-only invariant assertion** (§10) and the **`CandidateTypes` future lane** (§11) — both addressed below.

### 9.3 One Small Addition: `MatchQuality` on Typed Operation Nodes

The precomputed index knows the `MatchQuality` of every resolved operation (Exact, LeftWidened, RightWidened, BothWidened). This information is currently discarded after resolution. It shouldn't be.

```csharp
public sealed record TypedBinaryOp(
    TypeKind ResultType,
    OperationKind ResolvedOp,
    TypedExpression Left,
    TypedExpression Right,
    MatchQuality Quality,            // NEW — Exact, LeftWidened, etc.
    QualifierBinding? ResultQualifier,
    ImmutableArray<ProofRequirement> ProofRequirements,
    Expression Syntax
) : TypedExpression(ResultType, Syntax);
```

`Quality` costs nothing to propagate (it comes directly from the table lookup result) and enables the language server to highlight implicit widening — a future LS feature that would be impossible without it. It's one field. Add it now.

---

## 10. Error Handling

### 10.1 `TypedErrorExpression` and ErrorType Propagation

The policy is identical to the existing spec: always produce partial results. Any sub-expression that fails resolution is replaced with `TypedErrorExpression`. The containing declaration is still emitted. ErrorType propagates through binary ops and function calls without emitting additional diagnostics — suppressing cascades.

The radical model handles this transparently: `TableLookupStrategy.ResolveOperands()` inspects resolved sub-expressions before the table lookup. If any operand is `TypeKind.Error`, it short-circuits the lookup and returns `TypedErrorExpression` immediately. No lookup attempt on an error operand. This is ~5 lines in `ResolveOperands` and eliminates "spurious missing-operation" diagnostics for expressions downstream of a resolution failure.

### 10.2 The Debug Invariant Assertion

**Decision D-26 from the existing spec, carried forward as a first-class design requirement.**

The rule: any `SemanticIndex` that contains a `TypedErrorExpression` anywhere in its trees MUST contain at least one `Diagnostic` with `DiagnosticSeverity.Error`.

This is a test-time-only assertion — it fires during debug builds and test runs, not in production. Cost: zero at runtime. Value: catches the class of bug where a code path produces `TypedErrorExpression` without emitting the corresponding diagnostic. This is a real bug class — error expression nodes become stranded with no user-visible report.

Implementation in the `FinalAssembly` method (equivalently, the terminal step of what the existing spec calls Slice 10):

```csharp
[Conditional("DEBUG")]
static void AssertErrorInvariant(SemanticIndex index)
{
    bool hasErrorExpression = HasAnyTypedErrorExpression(index);
    bool hasErrorDiagnostic = index.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

    Debug.Assert(
        !hasErrorExpression || hasErrorDiagnostic,
        "Invariant violated: SemanticIndex contains TypedErrorExpression " +
        "but no Error-severity Diagnostic. A resolution failure path is " +
        "emitting error nodes without corresponding diagnostics.");
}
```

`HasAnyTypedErrorExpression` walks all typed expressions in all typed declarations. It's a tree walk that stops at the first `TypedErrorExpression` found — O(n) in expression count, called only in DEBUG builds.

The assertion location is `FinalAssembly`, after the complete `SemanticIndex` is constructed and before it's returned. One call, one line in production code. The `[Conditional("DEBUG")]` attribute removes it entirely from release builds.

### 10.3 `CandidateTypes` — Post-V1 Future Lane

**Explicitly in scope as a known future enhancement — do not implement in initial version.**

Add to `TypedErrorExpression`:

```csharp
public sealed record TypedErrorExpression(
    Expression Syntax,
    ImmutableArray<TypeKind>? CandidateTypes = null   // POST-V1: "did you mean?"
) : TypedExpression(TypeKind.Error, Syntax);
```

`CandidateTypes` captures what *would have* matched if the caller had used a different type. Scenario: the user writes `remove total` where `total: money`. The `remove` action applies to sets, queues, stacks, and optionals — not money. The `TypedErrorExpression` for this action failure could carry `CandidateTypes = [Set, Queue, Stack]` — the types for which `remove` IS applicable.

How it would be populated (future implementation): when `ActionApplicability[(kind, fieldType)] == false`, query the table for all TypeKinds where `ActionApplicability[(kind, candidateType)] == true` → that's the candidates list. Similar logic applies to binary operation mismatches and function overload failures.

This enables "did you mean?" suggestions in the language server: "remove applies to set, queue, or stack fields, but total is money." The LS already has the `CandidateTypes` array — it just formats them for display. Without `CandidateTypes`, the LS would have to re-run catalog lookups from scratch to generate the same suggestions.

**Not needed for initial implementation.** The LS can fall back to context-based completions for V1. But `CandidateTypes` must be on the record from day one — adding it later is a breaking change to the `TypedErrorExpression` API. Note it as a known future lane, define the field as nullable/optional, leave it `null` in all initial code paths.

---

## 11. Relationship to the Existing 11-Slice Spec

The 11-slice spec is well-reasoned. The coverage is comprehensive, the dependency graph is correct, the ordering reflects genuine structural dependencies. It doesn't need to be thrown away — it needs to be re-understood through the radical lens.

### 11.1 What the Radical Model Collapses

The most striking thing about the 11 slices is that Slices 2, 3, 4, and 8 are all "expression resolution." They introduce different expression forms but execute the *same algorithm*: resolve sub-expressions, look up the result in a catalog, emit a typed node. They're separate slices because the traditional implementation adds new arms to `Resolve()` for each form. The radical implementation has no `Resolve()` arms to add — the strategy table is pre-populated for all 13 expression forms at startup.

What this means for slicing: **Slices 2, 3, 4, and 8 can be developed as one slice.** The infrastructure (strategy dispatch, precomputed tables) handles all expression forms. The incremental implementation effort is in building and testing each strategy implementation — but each strategy is independent code that can be developed and tested in isolation without affecting the others. The `StructuralStrategy` (covering Slice 2's identifier/literal forms AND Slice 4's typed constant forms AND Slice 9's quantifier binding) is one class. The `TableLookupStrategy` (covering Slice 2's binary/unary ops, Slice 3's functions/accessors, Slice 8's CI functions) is one class. You build the class once and test it end-to-end.

The radical implementation doesn't need 11 slices. It needs:

| Radical Slice | Equivalent Old Slices | What It Is |
|---|---|---|
| Shape Commit | Pre-Slice 0 | All TypedXxx record definitions — unchanged |
| Symbol Registration | Slice 1 | Pass 1 unchanged |
| Index Build + Strategy Dispatch | Slice 2 core | TypeCatalogIndex + Resolve() dispatch + TableLookupStrategy |
| Structural Strategy | Slices 2, 4, 9 (partial) | Identifiers, literals, typed constants, list literals |
| Fixed + Propagation Strategies | Slices 6, 9 (partial) | Postfix, quantifiers, conditional, grouped |
| Declaration Normalization | Slices 5, 6 core | All NormalizeX functions + action typing |
| Modifier Validation | Slice 7 | ~30-line generic constraint loop |
| Structural Validation | Slice 6 remainder | Cycle detection, choice validation |
| Final Assembly | Slice 10 | CheckContext → SemanticIndex + debug assertion |

That's 9 conceptual units instead of 11, and several of them are substantially smaller than their equivalents. Not a dramatic reduction in *work* — but a significant reduction in *mental overhead*, because the work is organized around what kind of thing it is, not what phase it was added in.

### 11.2 What the Radical Model Preserves

The radical model genuinely preserves:

- **The 2-pass architecture** — symbol registration before expression checking is correct and unchanged
- **The SemanticIndex shape** — the output contract is right; don't change it
- **Qualifier disambiguation logic** — 15 lines of structural logic that can't be catalog-driven; kept
- **Structural validation (Sub-pass 2c)** — cycle detection, choice validation, stateless/stateful checks are genuinely structural; kept
- **`[HandlesCatalogMember]` enforcement** — still valuable; `ExpressionFormKind` coverage still needs enforcement. In the radical model, the annotation moves to strategy implementations rather than `Resolve()` arms, but the enforcement principle is identical
- **ErrorType propagation** — the existing spec's approach is correct; the radical model extends it slightly (short-circuit in `ResolveOperands`)
- **The `TypedAction` 3-shape DU** — the existing spec's `TypedAction`/`TypedInputAction`/`TypedBindingAction` DU is the right shape for downstream consumers; unchanged

### 11.3 Where the Radical Model Genuinely Improves

| Area | Old Model | Radical Model |
|---|---|---|
| Binary op widening | 6-step cascade search at check time | Table lookup at check time; cascade at build time (once) |
| Overload resolution | 7-step scoring algorithm at check time | Table lookup at check time |
| Scope management | Procedural push/pop in per-construct-kind code | `ScopeRule` on `ConstructMeta`, generic push/pop |
| Expression dispatch | 16-arm switch in `Resolve()` | 4 strategy classes, dispatch by `ExpressionFormKind` |
| Action applicability | `meta.ApplicableTo.Any(...)` scan at check time | Single table lookup |
| Modifier validation | Described as a "slice" with bespoke logic | ~30-line generic constraint loop over `FieldModifierMeta` |

The modifier validation point deserves emphasis. The existing spec's Slice 7 describes applicability, conflicts, and subsumption as distinct cases to implement. With the radical lens: all three are generic iterations over `FieldModifierMeta` fields that already exist. The entire thing is:

```csharp
// Entire modifier validator — ~25 lines:
foreach (var modifier in field.Modifiers)
{
    if (Modifiers.GetMeta(modifier) is not FieldModifierMeta fm) continue;

    if (fm.ApplicableTo.Length > 0 && !fm.ApplicableTo.Any(t => t.Matches(field.ResolvedType)))
        ctx.Emit(DiagnosticCode.ModifierInapplicable, modifier.Span);

    foreach (var conflict in fm.MutuallyExclusiveWith)
        if (field.Modifiers.Contains(conflict))
            ctx.Emit(DiagnosticCode.ModifierConflict, modifier.Span);

    foreach (var subsumed in fm.Subsumes)
        if (field.Modifiers.Contains(subsumed))
            ctx.Emit(DiagnosticCode.ModifierRedundant, modifier.Span);
}
// Plus 5 lines for min > max bounds validation, 3 lines for writable-on-computed prevention
```

Slice 7 is not a slice. It's a 30-line loop over metadata that already exists.

---

## 12. The `ResolverContext` Model

The traditional `CheckContext` is a mutable object with setters for current scope, current field index, event args, and quantifier bindings. The radical model makes context immutable and pass-by-value:

```csharp
// Immutable context passed through Resolve() calls.
// PushScope() returns a new context; doesn't mutate the parent.
public sealed record ResolverContext(
    TypeCatalogIndex             Index,           // shared, never changes
    FieldSymbolTable             Fields,          // populated in Pass 1, read-only in Pass 2
    StateSymbolTable             States,
    EventSymbolTable             Events,
    ScopeFrame                   Scope,           // current scope (event args, field index, etc.)
    ImmutableStack<QuantifierBinding> Quantifiers, // innermost at top
    DiagnosticAccumulator        Diagnostics      // mutable accumulator — one exception to immutability
);

public sealed record ScopeFrame(
    FieldScopeMode               Mode,             // AllFields or PriorFieldsOnly
    int                          FieldIndex,       // -1 = no restriction
    IReadOnlyDictionary<string, TypedArg>? EventArgs // null = no event scope
);
```

`PushScope(ScopeRule rule, Construct construct) → ResolverContext` creates a new context with an updated `ScopeFrame`. The old context is unchanged. No mutable state to clean up. No `using` scopes or `finally` blocks.

The `DiagnosticAccumulator` is the one mutable member — it's shared across all scopes to collect diagnostics in declaration order. Making it immutable would require merging diagnostic lists on scope pop, which is unnecessary complexity.

**`QuantifierBindings` as `ImmutableStack`:** The existing spec uses `Stack<(string Name, TypeKind Type)>` — correct semantics but mutable. An `ImmutableStack<QuantifierBinding>` carries the same semantic with pure value semantics. Pushing a quantifier binding produces a new context with the new binding on top; exiting the quantifier scope returns to the outer context (which never changed). No `Push`/`Pop` pairs to coordinate.

---

## 13. Size Estimate

The type checker resolves to two components: the `TypeCatalogIndex` (built once at startup) and the `TypeChecker` itself.

### `TypeCatalogIndex` (in `Language/`)

| Section | Lines |
|---|---|
| `BinaryKey`, `UnaryKey`, `FunctionKey`, `AccessorKey`, key types | 30 |
| `ResolvedBinaryOp`, `ResolvedUnaryOp`, `ResolvedFunction`, `ResolvedAccessor` result types | 25 |
| `ResolutionShape` DU + strategy enums (on `ExpressionFormMeta`) | 25 |
| `ScopeRule` DU (on `ConstructMeta`) | 20 |
| `TypeCatalogIndex` record with all table fields | 30 |
| Binary op index builder (with widening) | 40 |
| Unary op index builder | 15 |
| Function overload index builder | 30 |
| Accessor index builder | 25 |
| Action applicability index builder | 15 |
| Strategy map builder (from `ExpressionForms.GetMeta`) | 20 |
| Scope rule map builder (from `Constructs.GetMeta`) | 15 |
| `WideningSources` reverse index helper | 20 |
| **Total — `TypeCatalogIndex`** | **~310 lines** |

(This lives in `Language/`, alongside the existing catalogs. It is catalog infrastructure, not checker logic.)

### `TypeChecker` (in `Pipeline/`)

| Section | Lines |
|---|---|
| `ResolverContext` record + `ScopeFrame` + `DiagnosticAccumulator` | 40 |
| Pass 1: symbol registration (fields, states, events, args, duplicates) | 95 |
| `ExpressionFormKindOf` routing helper | 20 |
| `Resolve()` dispatch entry point | 10 |
| `TableLookupStrategy` — all 5 lookup paths + qualifier disambiguation | 90 |
| `FixedResultStrategy` | 15 |
| `PropagationStrategy` (PassThrough + UnifyBranches) | 25 |
| `StructuralStrategy` — identifier, literal, typed constant, list literal | 70 |
| `ResolveOperands` helper (error short-circuit + sub-expression resolver) | 20 |
| `HandleLiteralRetry` helper (numeric context retry) | 15 |
| Pass 2: declaration normalization — all `NormalizeX` functions | 110 |
| `TypeCheckAction` — shape decomposition + applicability + DU classification | 55 |
| Sub-pass 2c: structural validation (cycles, choice, forward-ref, cross-validation) | 95 |
| Modifier validation generic loop | 30 |
| `FinalAssembly` — CheckContext → SemanticIndex + debug assertion | 55 |
| `HasAnyTypedErrorExpression` tree walker (DEBUG only) | 25 |
| **Total — `TypeChecker`** | **~770 lines** |

**Combined: ~1,080 lines** across two files (`TypeCatalogIndex.cs` ≈ 310 lines, `TypeChecker.cs` ≈ 770 lines).

The existing spec's implied size: `Resolve()` at 250–350 lines + 10 NormalizeX functions at ~80 lines + modifiers at ~60 lines + structural validation at ~90 lines + CheckContext + Pass 1 + final assembly ≈ **~900–1,000 lines** in a single file — not dramatically larger, but less organized.

The radical design's advantage is not primarily line count. It's *where the logic lives*:
- Widening logic: 40 lines in `TypeCatalogIndex` (build time), 0 lines in `TypeChecker` (check time)
- Overload scoring: 30 lines in `TypeCatalogIndex` (build time), 0 lines in `TypeChecker` (check time)
- Scope management: 20 lines in `ConstructMeta` metadata, 15 lines of generic push/pop
- Modifier validation: 30 lines — not a "slice," just a loop
- Per-form dispatch: 4 strategy classes, 0 arms in `Resolve()`

Every time a new type, operator, function, or action is added to the catalog: `TypeCatalogIndex` automatically picks it up on next startup. Zero type-checker changes. That's the value of the catalog IS the type system.

---

## 14. What the Radical Lens Shows

The existing 11-slice spec is a traditional compiler design executed well. The coverage is right, the decisions are well-argued, the slice ordering reflects genuine dependencies. None of that is wrong.

What the radical lens reveals is that the 11-slice design is organized around *when code is written during development* (slice ordering) rather than *why logic exists at all* (catalog vs. structural). When you sort by "why does this logic exist," the landscape looks different:

- Logic that exists because of catalog facts (widening, overload selection, applicability) → should be in the catalog index, computed once, never recomputed
- Logic that exists because of expression structure (how to walk a tree) → should be in strategy classes, parameterized by form kind
- Logic that exists because of scope rules (what's in scope when) → should be on construct metadata
- Logic that exists because of symbol tables (is this name defined?) → genuinely structural, stays in Pass 1/2
- Logic that exists because of topology (are there cycles?) → genuinely structural, stays in structural validation

When sorted this way, the checker becomes a **query engine** over precomputed answers, not an **inference engine** that recomputes known facts. The difference is observable at check time: every expression resolution is O(1). The type system's entire knowledge is in frozen dictionaries built from the same catalogs the rest of the compiler already trusts.

Precept's type system is not hard. It has 20 types, ~80 binary operations, 15 functions, and a handful of widening edges. That's not a type system that needs an algorithm. That's a type system that fits in a table. Build the table once. Query it forever.

---

*The type checker's job is not to implement the type system — the catalog already did that. The type checker's job is to apply it.*
