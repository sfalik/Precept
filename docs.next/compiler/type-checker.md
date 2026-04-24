# Type Checker

> **Status:** Draft
> **Decisions answered:** T1 (TypedModel shape — flat symbol tables + declaration arrays), T2 (annotation strategy — embedded TypedExpression pairs), T3 (error type propagation), T4 (context-dependent literal resolution — uniform, no fallback), T5 (modifier applicability validation), T6 (scope model), T7 (functions and operators via catalog system), T8 (ErrorType singleton), T9 (stateless precept handling — cross-validation)
> **Survey references:** context-sensitive-literal-typing survey, pipeline architecture survey, compilation-result-type survey
> **Grounding:** `docs.next/compiler/pipeline-artifacts-and-consumer-contracts.md` (pipeline contract), `docs.next/language/precept-language-spec.md` §3 (type checker stubs), `docs.next/compiler/literal-system.md` (literal resolution contracts), `docs.next/compiler/diagnostic-system.md` (diagnostic infrastructure)

---

## Overview

The type checker is the third stage of the Precept compiler pipeline. It transforms the `SyntaxTree` produced by the parser into a `TypedModel` — a fully resolved semantic model with typed expressions, symbol tables, and diagnostics.

```
SyntaxTree  →  TypeChecker.Check  →  TypedModel
```

The type checker's job is pure semantic analysis — build symbol tables, resolve names, assign types to every expression, validate semantic constraints, and emit diagnostics. It has no knowledge of state-graph reachability or interval arithmetic. Its output is consumed by `GraphAnalyzer.Analyze` and `ProofEngine.Prove`.

The public surface is a static class `TypeChecker` with a single method:

```csharp
public static TypedModel Check(SyntaxTree tree)
```

No instance, no DI, no configuration. Tests call the method directly and assert on the output.

**Bright-line boundaries:** The parser owns grammar rules (syntax structure, missing tokens). The type checker owns everything that requires knowing what a name means — duplicate names, undeclared references, type mismatches, modifier validity, scope rules, function signatures. The graph analyzer owns state-transition reachability. The proof engine owns interval reasoning (unsatisfiable guards, division-by-zero provability). The type checker builds the typed model that makes those downstream analyses possible.

**Stateless precepts:** A precept with no `state` declarations and no `from ... on ...` transition rows is a stateless precept (e.g., `CustomerProfile`). The type checker handles this naturally — the symbol table simply has an empty state map. Rules, field declarations, access modes, and stateless event hooks are all valid. Transition rows, state ensures, and state actions are structurally absent from the AST, so no special-casing is needed.

---

## Design Principles

### Same input always produces same output

`TypeChecker.Check` is a static, pure function: `SyntaxTree → TypedModel`. No instance, no DI, no configuration, no mutable state surviving across calls. All mutable checking state lives in a private `CheckSession` struct instantiated inside `Check()` and discarded after. This matches the pipeline pattern used by all five stages.

### Never stop checking

The type checker always runs to completion, even on broken input. It produces a `TypedModel` with diagnostics — not an exception or a null. `ErrorType` propagation (see Error Recovery) ensures that a single root-cause error does not cascade into dozens of symptom diagnostics. Every consumer (graph analyzer, proof engine, LS, MCP tools) requires a structurally complete model.

### Separation of concerns at the stage boundary

The type checker resolves nothing beyond semantic analysis:
- It does not build a transition graph — that is the graph analyzer's job
- It does not reason about state reachability or dead-end states — that is the graph analyzer's job
- It does not perform interval arithmetic or satisfiability checks — that is the proof engine's job
- It does not evaluate expressions at runtime — that is the evaluator's job

### Parser owns grammar; type checker owns semantics

The bright line: if the grammar rule defines it, the parser enforces it. If enforcement requires knowing what an identifier means, the type checker enforces it. The type checker never builds its own tree — it walks the parser's AST and annotates it with types and resolved references via `TypedExpression` wrappers.

### Explicit bridges over implicit coercion

Numeric lane crossing (`decimal ↔ number`) requires explicit bridge functions (`approximate()`, `round(value, places)`). The type checker enforces this by treating mixed-lane arithmetic as a type error, not by silently inserting conversions. This is the primary mechanism preventing silent precision loss in financial calculations.

---

## Architecture

### Static class + private CheckSession struct

All mutable checking state lives in a private `CheckSession` struct instantiated inside `Check()` and discarded after:

```csharp
public static class TypeChecker
{
    public static TypedModel Check(SyntaxTree tree)
    {
        var session = new CheckSession(tree);
        session.RegisterDeclarations();   // Pass 1: build symbol tables
        session.CheckDeclarations();      // Pass 2: resolve types + validate
        return session.BuildModel();
    }
}
```

`CheckSession` holds the symbol tables (fields, states, events), the diagnostic accumulator, and all `CheckX` helper methods. It is not visible to callers.

Rejected alternative: _Instance class `TypeChecker`_ — adds a constructor, instance fields, and a lifecycle to manage with no benefit. No caller configures or reuses a checker instance.

### Two-pass processing

The type checker makes two passes over the declaration list:

1. **Registration pass:** Walk all declarations, build symbol tables (field names → types, state names, event names → arg types). No expression checking. This ensures all names are available before expression checking begins — declarations can appear in any order.
2. **Checking pass:** Walk all declarations again, resolve expressions, validate types, emit diagnostics.

Rejected alternative: _Single-pass with forward-reference fixup_ — adds complexity for the same result. Two clean passes are simpler to reason about and debug.

---

## Input & Output Contracts

> **Structural note:** This section is unique to the type-checker blueprint. The lexer and parser docs embed their contract information in the Architecture and Consumer Contracts sections. The type checker's contracts are broken out separately because it has the richest input assumptions (parser guarantees) and the most consumers (graph analyzer, proof engine, LS, MCP).

### Input: `SyntaxTree`

```
TypeChecker.Check(SyntaxTree tree) → TypedModel
```

**Parser guarantees the type checker can rely on:**

- `SyntaxTree.Root` is always non-null. It always carries a `PreceptNode` with a `Body` of `Declaration` nodes.
- Every node is structurally complete. Missing tokens are represented as `IsMissing` nodes with zero-length spans — never as nulls in required positions.
- Expression subtrees are fully assembled — operator precedence, associativity, and parenthesization are resolved.
- `IdentifierExpression` nodes carry raw name tokens. They are not resolved — the type checker performs all name resolution.
- Typed constant interiors (`TypedConstantExpression`, `InterpolatedTypedConstantExpression`) carry opaque content. The type checker determines the expected type from context, then validates the content against that type via registered `ITypedConstantValidator` instances.
- `NumberLiteralExpression` tokens carry the raw numeric text. The type checker determines the numeric lane (integer/decimal/number) from context.

**What `IsMissing` means for the type checker:**

- A node with `IsMissing = true` was synthesized by parser error recovery. It has a zero-length span.
- The type checker **skips** missing declarations. A `FieldDeclaration` whose `Names` contains a missing token is not added to the symbol table — the parser already emitted a diagnostic.
- Missing expressions propagate **error type** (see §7). This suppresses cascading type errors from incomplete input.

### Output: `TypedModel`

The type checker always produces a `TypedModel` — even on broken input. The model may be incomplete (missing symbols, error-typed expressions), but it is structurally valid. This is the pipeline's Model A (resilient) contract.

---

## TypedModel Shape

```csharp
public sealed record class TypedModel(
    string                                          PreceptName,
    ImmutableDictionary<string, FieldSymbol>         Fields,
    ImmutableDictionary<string, StateSymbol>          States,
    ImmutableDictionary<string, EventSymbol>          Events,
    ImmutableArray<ResolvedRule>                      Rules,
    ImmutableArray<ResolvedEnsure>                    Ensures,
    ImmutableArray<ResolvedTransitionRow>             TransitionRows,
    ImmutableArray<ResolvedAccessMode>                AccessModes,
    ImmutableArray<ResolvedStateAction>               StateActions,
    ImmutableArray<ResolvedStatelessHook>             StatelessHooks,
    string?                                          InitialState,
    ImmutableArray<Diagnostic>                       Diagnostics
);
```

### Symbol Types

```csharp
/// <summary>A declared field with resolved type, modifiers, and optional computed expression.</summary>
public sealed record FieldSymbol(
    string              Name,
    ResolvedType        Type,
    bool                IsOptional,
    bool                IsComputed,
    ResolvedModifiers   Modifiers,
    TypedExpression?    ComputedExpression,
    TypedExpression?    DefaultValue,
    SourceSpan          Span           // declaration span for go-to-definition
);

/// <summary>A declared state with its modifiers.</summary>
public sealed record StateSymbol(
    string                             Name,
    bool                               IsInitial,
    ImmutableArray<StateModifierKind>  Modifiers,
    SourceSpan                         Span
);

/// <summary>A declared event with typed arguments.</summary>
public sealed record EventSymbol(
    string                                       Name,
    ImmutableDictionary<string, ArgSymbol>        Args,
    bool                                         IsInitial,
    SourceSpan                                   Span
);

/// <summary>An event argument with resolved type and modifiers.</summary>
public sealed record ArgSymbol(
    string            Name,
    ResolvedType      Type,
    bool              IsOptional,
    ResolvedModifiers Modifiers,
    TypedExpression?  DefaultValue,
    SourceSpan        Span
);
```

### Resolved Modifiers

```csharp
/// <summary>All validated modifier values for a field or argument.</summary>
public sealed record ResolvedModifiers(
    bool     Nonnegative,
    bool     Positive,
    bool     Nonzero,
    bool     Notempty,
    bool     Ordered,          // choice fields only
    decimal? MinValue,
    decimal? MaxValue,
    int?     MinLength,
    int?     MaxLength,
    int?     MinCount,
    int?     MaxCount,
    int?     MaxPlaces
);
```

### Resolved Declarations

Each resolved declaration wraps the corresponding AST node with typed expressions and resolved symbol references.

```csharp
/// <summary>A rule with its typed condition, optional guard, and message.</summary>
public sealed record ResolvedRule(
    TypedExpression  Condition,       // must be boolean
    TypedExpression? Guard,           // must be boolean if present
    TypedExpression  Message,         // must be string
    SourceSpan       Span
);

/// <summary>An ensure constraint with anchor, resolved states, typed condition, and message.</summary>
public sealed record ResolvedEnsure(
    EnsureAnchor               Anchor,
    EnsureScope                Scope,       // state or event
    ImmutableArray<string>     StateNames,  // resolved state names (empty for event ensures)
    string?                    EventName,   // resolved event name (null for state ensures)
    TypedExpression            Condition,
    TypedExpression?           Guard,
    TypedExpression            Message,
    SourceSpan                 Span
);

public enum EnsureScope { State, Event }

/// <summary>A transition row with resolved states, event, guard, actions, and outcome.</summary>
public sealed record ResolvedTransitionRow(
    ImmutableArray<string>           FromStates,
    string                           EventName,
    TypedExpression?                 Guard,
    ImmutableArray<ResolvedAction>   Actions,
    ResolvedOutcome                  Outcome,
    SourceSpan                       Span
);

/// <summary>A typed action step.</summary>
public sealed record ResolvedAction(
    ActionKind       Kind,
    string           FieldName,
    TypedExpression? Value,          // null for clear, dequeue/pop without value
    string?          IntoFieldName,  // dequeue/pop target
    SourceSpan       Span
);

public enum ActionKind { Set, Add, Remove, Enqueue, Dequeue, Push, Pop, Clear }

/// <summary>A resolved transition outcome.</summary>
public abstract record ResolvedOutcome(SourceSpan Span);
public sealed record TransitionOutcome(string StateName, SourceSpan Span) : ResolvedOutcome(Span);
public sealed record NoTransitionOutcome(SourceSpan Span) : ResolvedOutcome(Span);
public sealed record RejectOutcome(TypedExpression Message, SourceSpan Span) : ResolvedOutcome(Span);

/// <summary>A resolved access mode declaration.</summary>
public sealed record ResolvedAccessMode(
    ImmutableArray<string>  StateNames,   // empty for root-level write
    AccessMode              Mode,
    ImmutableArray<string>  FieldNames,   // resolved field names, or all fields if IsAll
    bool                    IsAll,
    TypedExpression?        Guard,
    SourceSpan              Span
);

/// <summary>A resolved state action (to/from hooks).</summary>
public sealed record ResolvedStateAction(
    StateActionAnchor              Anchor,
    ImmutableArray<string>         StateNames,
    TypedExpression?               Guard,
    ImmutableArray<ResolvedAction> Actions,
    SourceSpan                     Span
);

/// <summary>A resolved stateless event hook.</summary>
public sealed record ResolvedStatelessHook(
    string                         EventName,
    ImmutableArray<ResolvedAction> Actions,
    SourceSpan                     Span
);
```

### Typed Expressions

```csharp
/// <summary>A typed wrapper around an expression AST node. Carries the resolved type.</summary>
public sealed record TypedExpression(
    Expression    Syntax,        // the original AST node
    ResolvedType  Type,
    SourceSpan    Span
);
```

### T2 — Annotation Strategy: Embedded TypedExpression Pairs

**Decision: TypedExpression pairs embedded in resolved declarations, not a separate tree or map.**

Each resolved declaration (`ResolvedRule`, `ResolvedTransitionRow`, etc.) carries `TypedExpression` fields that pair the original `Expression` AST node with its `ResolvedType`. These pairs are constructed during the checking pass — the type checker already has both the expression and its resolved type in hand, so bundling them is free.

**Rationale:**

- AST nodes are immutable records. Adding type information requires either `with` cloning (every node, recursively — expensive and breaks reference equality) or mutable fields (violates the pipeline immutability invariant). Wrapping is the only option.
- The `TypedExpression` pair is a lightweight convenience — it bundles data the checker already computed. No extra construction cost.
- Consumers iterating declarations (runtime evaluating rules, proof engine analyzing guards) get the expression and its type together without a secondary lookup.
- The original AST remains untouched and available for position-based queries via the `SyntaxTree`.

**Alternatives considered:**

- _Annotated AST (TypeScript model):_ Requires mutable nodes. Rejected — violates pipeline immutability.
- _Node-keyed map `Dictionary<Expression, ResolvedType>`:_ Was considered for the LS "I have a syntax node, what's its type?" path. Rejected after tracing every consumer — none hold a random expression node without also having a name or resolved declaration. The map had no consumer.
- _Red-green tree (Roslyn model):_ Disproportionate for Precept's scale. Rejected.

### ResolvedType

```csharp
/// <summary>The type system's representation of a resolved type.</summary>
public abstract record ResolvedType;

// ── Scalar types ────────────────────────────────────────────────
public sealed record StringType()     : ResolvedType;
public sealed record BooleanType()    : ResolvedType;
public sealed record IntegerType()    : ResolvedType;
public sealed record DecimalType()    : ResolvedType;
public sealed record NumberType()     : ResolvedType;
public sealed record ChoiceType(ImmutableArray<string> Values, bool IsOrdered) : ResolvedType;

// ── Temporal types ──────────────────────────────────────────────
public sealed record DateType()           : ResolvedType;
public sealed record TimeType()           : ResolvedType;
public sealed record InstantType()        : ResolvedType;
public sealed record DurationType()       : ResolvedType;
public sealed record PeriodType(string? Qualifier)  : ResolvedType;  // "date", "time", or null (unconstrained)
public sealed record TimezoneType()       : ResolvedType;
public sealed record ZonedDateTimeType()  : ResolvedType;
public sealed record DateTimeType()       : ResolvedType;

// ── Business-domain types ───────────────────────────────────────
public sealed record MoneyType(string? CurrencyBasis)         : ResolvedType;
public sealed record CurrencyType()                           : ResolvedType;
public sealed record QuantityType(string? DimensionFamily)    : ResolvedType;
public sealed record UnitOfMeasureType()                      : ResolvedType;
public sealed record DimensionType()                          : ResolvedType;
// TODO(B5): PriceType and ExchangeRateType need qualifier parameters
// (currency, unit, numerator/denominator) to represent `in` declarations —
// same pattern as MoneyType(CurrencyBasis) and QuantityType(DimensionFamily).
// Design during implementation phase; see design review B5.
public sealed record PriceType()                              : ResolvedType;
public sealed record ExchangeRateType()                       : ResolvedType;

// ── Collection types ────────────────────────────────────────────
public sealed record SetType(ResolvedType ElementType)   : ResolvedType;
public sealed record QueueType(ResolvedType ElementType) : ResolvedType;
public sealed record StackType(ResolvedType ElementType) : ResolvedType;

// ── Special types ───────────────────────────────────────────────
/// <summary>Propagated when a sub-expression has an error. Suppresses cascading diagnostics.</summary>
public sealed record ErrorType() : ResolvedType;

/// <summary>The type of a state name reference in transition/ensure targets.</summary>
public sealed record StateRefType() : ResolvedType;
```

**Why an abstract record hierarchy (not an enum):**

- Collection types carry an element type. Choice types carry the value set. Money and quantity types carry qualifiers. An enum cannot carry this data.
- Pattern matching on records is idiomatic C# and gives exhaustiveness checking with `switch` expressions (combined with a discard arm for `ErrorType`).
- Each type record is a singleton-like value (no mutable state). Record equality works correctly for type comparison.

---

## Type System

### Scalar Types

| Type | Values | Literal form | Widening target |
|------|--------|--------------|-----------------|
| `boolean` | `true`, `false` | Bare keywords | — |
| `integer` | Whole numbers | `NumberLiteral` (no decimal point, no exponent) | `decimal`, `number` |
| `decimal` | Exact base-10 | `NumberLiteral` (with or without decimal point) | — (`number` requires explicit `approximate()`) |
| `number` | IEEE 754 double | `NumberLiteral` (any form including exponent) | — |
| `string` | Text | `"..."` string literals | — |
| `choice` | Finite string set | `"..."` string literals | — |

### Type Widening Rules

Implicit widening is lossless and one-directional. Only two implicit widenings exist:

```
integer  →  decimal     (implicit — lossless)
integer  →  number      (implicit — lossless, direct)
```

- `integer` widens to `decimal` (every integer is exactly representable in base-10).
- `integer` widens to `number` (every integer within safe integer range is exactly representable as an IEEE 754 double — this is a direct widening, not transitive through `decimal`).
- `decimal` does **NOT** implicitly widen to `number` in any context. The conversion is lossy — use `approximate(decimalValue)` to explicitly convert. See §4.2a.
- No implicit narrowing. `number` values cannot be assigned to `integer` or `decimal` fields without an explicit bridge function (see §4.2a).

**Widening applies in these contexts:**

- Assignment: `set IntegerField = ...` where the RHS is `integer` and the field is `decimal` — allowed.
- Binary operators: `IntegerField + DecimalField` — `integer` widens to `decimal`, result is `decimal`.
- Function arguments: `min(IntegerExpr, DecimalExpr)` — `integer` widens to `decimal`.
- Default values: `field X as decimal default 42` — `42` (integer form) widens to `decimal`.
- Comparison: `IntegerField > DecimalField` — `integer` widens, comparison is valid.

**No comparison exception.** `decimal == number`, `decimal > number`, etc. are also type errors. Cross-lane equality is semantically dangerous — IEEE 754 represents `0.1 + 0.2` as `0.30000000000000004`, so `number(0.1 + 0.2) == decimal(0.3)` silently returns `false`. The author must bridge one operand to choose which lane the comparison lives in.

**Widening ceiling for business-domain types:** The `integer → decimal` widening applies when scalars interact with `money`, `quantity`, `price`, and `exchangerate` operators. The `decimal → number` widening does NOT — `number` is permanently excluded from business-domain operator tables (D12 in business-domain-types.md). The operator table per type is the authority on which scalar widenings are accepted.

### Numeric Lane Integrity and Explicit Bridges

The three numeric types form three **lanes** — `integer`, `decimal`, `number` — with strict crossing rules. Implicit widening flows in one direction only (§4.2). All reverse-direction or cross-lane conversions require explicit bridge functions that make the author's intent visible.

**Lane crossing rules:**

| Direction | Mechanism | Type checker behavior |
|---|---|---|
| `integer → decimal` | Implicit widening | Allowed silently in all expression contexts |
| `integer → number` | Implicit widening | Allowed silently (direct — every integer is exactly representable as IEEE 754 double) |
| `decimal → number` | `approximate(value)` | Required in all contexts — `decimal * NumberField` without `approximate()` is a type error; `set NumberField = decimalExpr` without `approximate()` is a type error |
| `number → decimal` | `round(value, places)` | Required — the rounding makes precision loss explicit |
| `number → integer` | `floor(value)`, `ceil(value)`, `truncate(value)`, `round(value)` | Required — the rounding mode makes truncation semantics explicit |
| `decimal → integer` | `floor(value)`, `ceil(value)`, `truncate(value)`, `round(value)` | Required — same functions, same explicitness |

**Key rule: `decimal` and `number` cannot be mixed without an explicit bridge — no exceptions.** In arithmetic (`+`, `-`, `*`, `/`, `%`), `decimal op number` is a type error. In assignment, `set numberField = decimalExpr` is a type error without `approximate()`. In function arguments, passing a `decimal` where `number` is expected is a type error without `approximate()`. In comparisons, `decimal == number` and `decimal > number` are type errors — the author must bridge one operand first. This prevents silent precision loss in all contexts.

**Bridge function signatures (type checker validates these):**

| Function | Input | Output | Semantic |
|---|---|---|---|
| `approximate(x)` | `decimal` | `number` | Explicit lossy conversion to IEEE 754 |
| `round(x, places)` | `numeric`, `integer` | `decimal` | Round to N decimal places; normalizes `number → decimal` |
| `round(x)` | `decimal \| number` | `integer` | Banker's rounding to nearest integer |
| `floor(x)` | `decimal \| number` | `integer` | Round toward negative infinity |
| `ceil(x)` | `decimal \| number` | `integer` | Round toward positive infinity |
| `truncate(x)` | `decimal \| number` | `integer` | Truncate toward zero |

**Type checker enforcement:** When the type checker encounters a binary arithmetic expression with mismatched lanes (`decimal op number` or vice versa), it emits a `TypeMismatch` diagnostic with a teachable message suggesting the appropriate bridge function. The rounding functions (`floor`, `ceil`, `truncate`, `round`) and `approximate()` are the ONLY paths between lanes — no cast syntax, no implicit coercion.

### Numeric Lane Resolution (Context-Sensitive)

A `NumberLiteral` token does not intrinsically know its numeric lane. The type checker resolves it:

| Literal form | Valid target types | Resolution rule |
|---|---|---|
| Whole number (`42`) | `integer`, `decimal`, `number` | Context determines. If target is `integer`, resolves as `integer`. If `decimal`, resolves as `decimal`. If `number`, resolves as `number`. If no context, diagnostic + `ErrorType`. |
| Fractional (`3.14`) | `decimal`, `number` | If target is `decimal`, resolves as `decimal`. If `number`, resolves as `number`. If no context, diagnostic + `ErrorType`. Type error if target is `integer`. |
| Exponent (`1.5e2`) | `number` only | Always `number`. Type error if target is `integer` or `decimal`. |

**No implicit fallback.** If a numeric literal appears in a position with no type expectation (no field type, no operator peer, no function signature), the type checker emits a diagnostic and assigns `ErrorType`. In practice this only occurs in constant expressions that reference no declared data — which are meaningless in a precept. This rule is consistent with typed constant resolution (§4.4), where contextless multi-member families are also compile errors.

### Typed Constant Resolution

Typed constants (`'...'`) follow the same context-born resolution model as numeric literals (see `literal-system.md`):

1. **Context determines the type.** The type checker propagates an expected type inward from the enclosing expression — field declaration, assignment target, operator peer, function parameter, or comparison operand. This is the same top-down inference that resolves `42` to `integer`, `decimal`, or `number`.

2. **Content is validated against the expected type.** Once the expected type is known, the content is parsed and validated by the type's registered `ITypedConstantValidator`. If the content doesn't parse as the expected type, it is a compile error.

3. **Content validation → compile-time error on malformed values.** For example, `'2026-02-30'` fails date validation (no February 30th); `'XYZ 100.00'` fails money validation (XYZ is not a recognized ISO 4217 currency code). The `ITypedConstantValidator` registry is layered: the checker defines the hook, each domain type family registers its validator. If no validator is registered for a type, the checker accepts shape validation only. This layered approach avoids circular dependencies between the checker core and domain-specific validation logic.

4. **No context → compile error.** A typed constant in a position with no type expectation is a compile error, just as a numeric literal in a contextless position is.

**Content validation table** — given context-determined type, valid content patterns:

| Expected type | Valid content | Examples |
|---|---|---|
| `date` | `YYYY-MM-DD` | `'2026-04-15'` |
| `time` | `HH:MM:SS` or `HH:MM` | `'14:30:00'`, `'14:30'` |
| `instant` | ISO 8601 with `T`, trailing `Z` | `'2026-04-15T14:30:00Z'` |
| `datetime` | ISO 8601 with `T`, no zone | `'2026-04-15T14:30:00'` |
| `zoneddatetime` | ISO 8601 with `T`, `[Zone]` bracket | `'2026-04-15T14:30:00[America/New_York]'` |
| `timezone` | `Word/Word` IANA identifier | `'America/New_York'` |
| `duration` | `<integer> <temporal-unit>` (with optional `+`) | `'72 hours'` |
| `period` | `<integer> <temporal-unit>` (with optional `+`) | `'30 days'`, `'2 years + 6 months'` |
| `money` | `<number> <ISO-4217-code>` | `'100 USD'`, `'50.25 EUR'` |
| `quantity` | `<number> <unit-name>` | `'5 kg'`, `'24 each'` |
| `price` | `<number> <currency>/<unit>` | `'4.17 USD/each'` |
| `exchangerate` | `<number> <currency>/<currency>` | `'1.08 USD/EUR'` |
| `currency` | `<ISO-4217-code>` (3-letter) | `'USD'`, `'EUR'` |
| `unitofmeasure` | Unit name (lowercase/mixed) | `'kg'`, `'each'` |
| `dimension` | Dimension name (UCUM registry) | `'mass'`, `'length'` |
| No context | **Compile error** | — |

### Collection Types

| Collection | Element constraint | Member accessors | Operations |
|---|---|---|---|
| `set of T` | T must be a scalar type | `.count` → `integer` | `add`, `remove`, `clear`, `contains` |
| `queue of T` | T must be a scalar type | `.count` → `integer`, `.peek` → `T` | `enqueue`, `dequeue`, `clear` |
| `stack of T` | T must be a scalar type | `.count` → `integer`, `.peek` → `T` | `push`, `pop`, `clear` |

Collection element types are always scalar — no nested collections.

**Emptiness guard requirement.** Accessors that read from a collection (`.peek`, `.min`, `.max`) and mutations that consume from a collection (`dequeue`, `pop`) require proof that the collection is non-empty. Without proof, the type checker emits `UnguardedCollectionAccess` (for accessors) or `UnguardedCollectionMutation` (for mutations).

Proof sources:
- **Conditional guard:** `if Collection.count > 0` — the accessor/mutation appears in the `then` branch of a conditional whose condition tests `.count > 0` (or equivalently `.count != 0`, `.count >= 1`).
- **`mincount` constraint:** `field Items as set of string mincount 1` — the field's `mincount` modifier guarantees the collection is never empty.

The idiomatic pattern for safe access is `if Items.count > 0 then Items.peek else fallbackValue`.

### Expression Typing Rules

#### Binary operators

**Core scalar operators:**

| Operator | Left type | Right type | Result type | Widening? |
|----------|-----------|------------|-------------|-----------|
| `+` `-` `*` `/` `%` | numeric | numeric | common numeric type | Yes — widen to common |
| `+` | `string` | `string` | `string` | No (concatenation) |
| `==` `!=` | any T | same T | `boolean` | Yes — numeric widening |
| `~=` `!~` | `string` | `string` | `boolean` | No — case-insensitive ordinal; type error on non-string |
| `<` `>` `<=` `>=` | numeric | numeric | `boolean` | Yes — numeric widening |
| `<` `>` `<=` `>=` | `string` | `string` | `boolean` | No (lexicographic) |
| `<` `>` `<=` `>=` | `choice` (ordered) | `choice` (ordered, same set) | `boolean` | No (ordinal) |
| `and` `or` | `boolean` | `boolean` | `boolean` | No |

**Common numeric type resolution:** When two numeric operands have different lanes, the result is the wider type: `integer op decimal` → `decimal`; `integer op number` → `number`. `decimal op number` is a **type error** — the author must bridge one operand first. See [Primitive Types · Numeric Lane Rules](../language/primitive-types.md#numeric-lane-rules) for the complete conversion map.

**Temporal arithmetic operators** — per-type operator table. Cross-domain operations (e.g., `date ± duration`, `instant ± period`) are type errors; see the [temporal type system](../language/temporal-type-system.md#cross-type-arithmetic-whats-not-allowed-and-why) for the full rejection table with teachable messages.

| Left | Op | Right | Result | Notes |
|------|----|-------|--------|-------|
| `date` | `±` | `period of 'date'` | `date` | Calendar arithmetic. Period must be provably date-only (constraint or guard). `date ± period` (unconstrained) is `UnqualifiedPeriodArithmetic`. |
| `date` | `-` | `date` | `period` | Calendar distance. |
| `date` | `+` | `time` | `datetime` | Composition. Commutative (`time + date` also valid). |
| `time` | `±` | `period of 'time'` | `time` | Period must be provably time-only. `time ± period` (unconstrained) is `UnqualifiedPeriodArithmetic`. |
| `time` | `±` | `duration` | `time` | Sub-day bridging. Wraps at midnight. |
| `time` | `-` | `time` | `period` | Time-component period. |
| `instant` | `-` | `instant` | `duration` | Elapsed time. |
| `instant` | `±` | `duration` | `instant` | Offset forward/backward. |
| `datetime` | `±` | `period` | `datetime` | Calendar arithmetic. No constraint — accepts all period components. |
| `datetime` | `-` | `datetime` | `period` | Calendar distance. |
| `duration` | `±` | `duration` | `duration` | Combined / difference. |
| `duration` | `*` | `integer` or `number` | `duration` | Scaling. `decimal` is a type error. |
| `integer` or `number` | `*` | `duration` | `duration` | Commutative. |
| `duration` | `/` | `integer` or `number` | `duration` | Scaling. |
| `duration` | `/` | `duration` | `number` | Ratio. |
| `period` | `±` | `period` | `period` | Combined / difference. |
| `zoneddatetime` | `±` | `duration` | `zoneddatetime` | Timeline arithmetic. |
| `zoneddatetime` | `-` | `zoneddatetime` | `duration` | Instant subtraction. |

**Temporal comparison operators:**

| Type | `==` `!=` | `<` `>` `<=` `>=` | Notes |
|------|-----------|-------------------|-------|
| `date` | ✓ | ✓ | ISO calendar order. |
| `time` | ✓ | ✓ | Within-day order. |
| `instant` | ✓ | ✓ | Nanosecond timeline. |
| `duration` | ✓ | ✓ | Nanosecond magnitude. |
| `datetime` | ✓ | ✓ | Same-calendar order. |
| `period` | ✓ | **✗ — type error** | No natural ordering (variable-length months). |
| `timezone` | ✓ | **✗ — type error** | Equality by IANA identifier only. |
| `zoneddatetime` | ✓ | **✗ — type error** | No natural ordering. Compare via `.instant` or `.datetime` accessor. |

Cross-type temporal comparison is always a type error.

**Business-domain arithmetic operators** — all seven types use `decimal` as magnitude backing. Scalar operands must be `decimal` (not `number`); `integer` widens to `decimal` losslessly. `number` scalars are type errors for all business-domain operations. See [business-domain types](../language/business-domain-types.md) for the complete proposal.

| Left | Op | Right | Result | Notes |
|------|----|-------|--------|-------|
| `money` | `±` | `money` | `money` | Same currency required; cross-currency is a compile error. |
| `money` | `*` | `decimal` | `money` | Scaling. Commutative. |
| `money` | `/` | `decimal` | `money` | Division by scalar. Divisor safety applies. |
| `money` | `/` | `money` (same currency) | `decimal` | Dimensionless ratio. |
| `money` | `/` | `money` (different currency) | `exchangerate` | Currency-pair derivation. |
| `money` | `/` | `quantity` | `price` | Price derivation. |
| `money` | `/` | `period` | `price` | Time-based price derivation (D15). |
| `money` | `/` | `duration` | `price` | Duration-based price derivation for fixed-length units (D15). |
| `quantity` | `±` | `quantity` | `quantity` | Same dimension required; auto-converts if commensurable. |
| `quantity` | `*` | `decimal` | `quantity` | Scaling. Commutative. |
| `quantity` | `/` | `decimal` | `quantity` | Division by scalar. |
| `quantity` | `/` | `quantity` (same dim.) | `decimal` | Dimensionless ratio. |
| `quantity` | `/` | `quantity` (diff. dim.) | `quantity` (compound) | Compound unit: `kg / each → kg/each`. |
| `quantity` | `/` | `period` | `quantity` (compound) | Time-denominator rate (D15). |
| `quantity` | `/` | `duration` | `quantity` (compound) | Duration-denominator rate (D15). |
| `quantity` (compound) | `*` | `quantity` | `quantity` | Dimensional cancellation: `(kg/each) × each → kg`. Commutative. |
| `quantity` (compound) | `*` | `period` | `quantity` | Time-denominator cancellation (D15). Commutative. |
| `quantity` (compound) | `*` | `duration` | `quantity` | Duration cancellation for fixed-length units (D15). Commutative. |
| `price` | `*` | `quantity` | `money` | Dimensional cancellation: `(USD/each) × each → USD`. Commutative. |
| `price` | `*` | `period` | `money` | Time-denominator cancellation (D15). Commutative. |
| `price` | `*` | `duration` | `money` | Duration cancellation for fixed-length units (D15). Commutative. |
| `price` | `*` | `decimal` | `price` | Scaling. Commutative. |
| `price` | `/` | `decimal` | `price` | Division by scalar. |
| `price` | `±` | `price` | `price` | Same currency and unit required. |
| `exchangerate` | `*` | `money` | `money` | Currency conversion: `(USD/EUR) × EUR → USD`. Commutative. |
| `exchangerate` | `*` | `decimal` | `exchangerate` | Scaling. Commutative. |
| `exchangerate` | `/` | `decimal` | `exchangerate` | Division by scalar. |

**Business-domain comparison operators:**

| Type | `==` `!=` | `<` `>` `<=` `>=` | Notes |
|------|-----------|-------------------|-------|
| `money` | ✓ | ✓ | Same currency required. |
| `quantity` | ✓ | ✓ | Same dimension required; auto-converts. |
| `price` | ✓ | ✓ | Same currency and unit required. |
| `exchangerate` | ✓ | **✗ — type error** | No meaningful ordering outside time context. |
| `currency` | ✓ | **✗ — type error** | Identity type — equality only. |
| `unitofmeasure` | ✓ | **✗ — type error** | Identity type — equality only. |
| `dimension` | ✓ | **✗ — type error** | Identity type — equality only. |

Cross-type business-domain comparison is always a type error.

#### Unary operators

| Operator | Operand type | Result type |
|----------|-------------|-------------|
| `not` | `boolean` | `boolean` |
| `-` (negate) | numeric | same numeric type |
| `-` (negate) | `duration` | `duration` |
| `-` (negate) | `money` | `money` (preserves currency) |
| `-` (negate) | `quantity` | `quantity` (preserves unit/dimension) |
| `-` (negate) | `price` | `price` (preserves currency/unit) |

#### `contains`

| Collection type | Value type | Result |
|-----------------|-----------|--------|
| `set of T` | `T` (or widens to `T`) | `boolean` |
| `queue of T` | `T` | `boolean` |
| `stack of T` | `T` | `boolean` |
| non-collection | — | type error |

#### `is set` / `is not set`

| Operand | Valid? | Result |
|---------|--------|--------|
| `optional` field | Yes | `boolean` |
| Non-optional field | Type error — field always has a value | — |

#### Conditional (`if ... then ... else ...`)

The `then` and `else` branches must have compatible types (same type, or one widens to the other). The result type is the common type.

#### Member access (`.`)

**Collection and core accessors:**

| Object type | Member | Result type |
|-------------|--------|-------------|
| `set of T` | `count` | `integer` |
| `queue of T` | `count` | `integer` |
| `queue of T` | `peek` | `T` |
| `stack of T` | `count` | `integer` |
| `stack of T` | `peek` | `T` |
| `string` | `length` | `integer` |
| Event arg reference (`EventName.ArgName`) | — | arg's declared type |

**Temporal accessors:**

| Object type | Member | Result type | Notes |
|-------------|--------|-------------|-------|
| `date` | `.year` | `integer` | Calendar year |
| `date` | `.month` | `integer` | Month (1–12) |
| `date` | `.day` | `integer` | Day of month (1–31) |
| `date` | `.dayOfWeek` | `integer` | ISO day of week (Mon=1, Sun=7) |
| `time` | `.hour` | `integer` | Hour (0–23) |
| `time` | `.minute` | `integer` | Minute (0–59) |
| `time` | `.second` | `integer` | Second (0–59) |
| `instant` | `.inZone(tz)` | `zoneddatetime` | Sole accessor on `instant`. `tz` is a `timezone` field reference. |
| `duration` | `.totalDays` | `number` | Total elapsed 24-hour days (may be fractional) |
| `duration` | `.totalHours` | `number` | Total elapsed hours |
| `duration` | `.totalMinutes` | `number` | Total elapsed minutes |
| `duration` | `.totalSeconds` | `number` | Total elapsed seconds |
| `period` | `.years` | `integer` | Years component (structural, not normalized) |
| `period` | `.months` | `integer` | Months component |
| `period` | `.weeks` | `integer` | Weeks component |
| `period` | `.days` | `integer` | Days component |
| `period` | `.hours` | `integer` | Hours component |
| `period` | `.minutes` | `integer` | Minutes component |
| `period` | `.seconds` | `integer` | Seconds component |
| `period` | `.hasDateComponent` | `boolean` | True if any date part is non-zero |
| `period` | `.hasTimeComponent` | `boolean` | True if any time part is non-zero |
| `period` | `.basis` | `string` | Canonical basis name from `in` constraint |
| `period` | `.dimension` | `dimension` | `'date'`, `'time'`, or `'datetime'` |
| `zoneddatetime` | `.instant` | `instant` | Underlying UTC point |
| `zoneddatetime` | `.timezone` | `timezone` | Bound IANA timezone |
| `zoneddatetime` | `.datetime` | `datetime` | Local date+time in bound timezone |
| `zoneddatetime` | `.date` | `date` | Local calendar date |
| `zoneddatetime` | `.time` | `time` | Local time |
| `zoneddatetime` | `.year`, `.month`, `.day`, `.hour`, `.minute`, `.second`, `.dayOfWeek` | `integer` | Local components in bound timezone |
| `datetime` | `.date` | `date` | Date component |
| `datetime` | `.time` | `time` | Time component |
| `datetime` | `.year`, `.month`, `.day`, `.hour`, `.minute`, `.second`, `.dayOfWeek` | `integer` | Direct components |
| `datetime` | `.inZone(tz)` | `zoneddatetime` | Anchor local reading to timeline |

**Strict hierarchy — no skip-level accessors (Decision #22).** `instant.date` is a compile error — must go through `instant.inZone(tz).date`. `instant.year`, `instant.hour`, etc. are all compile errors. The type checker validates that the accessed member is known for the object's resolved type.

**Business-domain accessors:**

| Object type | Member | Result type | Notes |
|-------------|--------|-------------|-------|
| `money` | `.amount` | `decimal` | Magnitude (numeric part) |
| `money` | `.currency` | `currency` | ISO 4217 code |
| `quantity` | `.amount` | `decimal` | Magnitude (numeric part) |
| `quantity` | `.unit` | `unitofmeasure` | Specific UCUM unit |
| `quantity` | `.dimension` | `dimension` | UCUM dimension category |
| `price` | `.amount` | `decimal` | Magnitude (numeric part) |
| `price` | `.currency` | `currency` | Numerator currency |
| `price` | `.unit` | `unitofmeasure` | Denominator unit |
| `exchangerate` | `.amount` | `decimal` | Magnitude (numeric part) |
| `exchangerate` | `.numerator` | `currency` | Numerator currency code |
| `exchangerate` | `.denominator` | `currency` | Denominator currency code |
| `unitofmeasure` | `.dimension` | `dimension` | UCUM dimension category |

**Discrete equality narrowing:** Business-domain accessors (`.currency`, `.unit`, `.dimension`, `.basis`) participate in guard narrowing. `when Payment.currency == 'USD'` injects `$eq:Payment.currency:USD` into the symbol table, enabling same-currency arithmetic in the guarded scope. Fields declared with static `in` values pre-seed `$eq:` markers unconditionally. See the [business-domain type proposal](../language/business-domain-types.md#discrete-equality-narrowing) for the full narrowing mechanism.

| _other_ | — | `InvalidMemberAccess` diagnostic |

#### Function calls

See §5.7 for the complete built-in function catalog.

#### Parenthesized expressions

Type is the type of the inner expression. Transparent.

#### String interpolation

Each `{expr}` inside `"..."` is type-checked independently. Any scalar type is coercible to string. Collections are a type error inside string interpolation.

#### Typed constant interpolation

Each `{expr}` inside `'...'` is type-checked independently. After interpolation expressions are typed, the full content is validated against the context-determined type as described in §4.4.

---

## Semantic Checks

### Name Resolution

| Check | Fires when | Diagnostic |
|-------|-----------|------------|
| **Duplicate field name** | Two `field` declarations declare the same name | `DuplicateFieldName` |
| **Duplicate state name** | Two state entries have the same name | `DuplicateStateName` |
| **Duplicate event name** | Two `event` declarations share a name | `DuplicateEventName` |
| **Duplicate event arg** | Two args in the same event have the same name | `DuplicateArgName` |
| **Undeclared field reference** | `IdentifierExpression` in expression context does not match a field name (or event arg, in scope) | `UndeclaredField` |
| **Undeclared state reference** | State name in `from`/`to`/`in` target, `transition` outcome does not match a declared state | `UndeclaredState` |
| **Undeclared event reference** | Event name in `from ... on`, `on` ensure, stateless hook does not match a declared event | `UndeclaredEvent` |
| **Multiple initial states** | More than one state entry has `initial` | `MultipleInitialStates` |
| **No initial state** | Stateful precept (has states) but none is marked `initial` | `NoInitialState` |

### Type Compatibility

| Check | Fires when | Diagnostic |
|-------|-----------|------------|
| **Assignment type mismatch** | `set Field = Expr` where `Expr`'s type is not assignable to `Field`'s type (after widening) | `TypeMismatch` |
| **Guard not boolean** | `when Expr` where `Expr`'s type is not `boolean` | `TypeMismatch` |
| **Rule condition not boolean** | `rule Expr` where `Expr`'s type is not `boolean` | `TypeMismatch` |
| **Ensure condition not boolean** | `ensure Expr` where `Expr`'s type is not `boolean` | `TypeMismatch` |
| **Message not string** | `because Expr` or `reject Expr` where `Expr` is not `string` | `TypeMismatch` |
| **Binary operator type error** | Operator applied to incompatible types (e.g., `string + boolean`) | `TypeMismatch` |
| **Comparison on unordered choice** | `<` / `>` / `<=` / `>=` on a `choice` field without the `ordered` modifier | `TypeMismatch` |
| **Conditional branch mismatch** | `if ... then A else B` where A and B have no common type | `TypeMismatch` |
| **Default value type mismatch** | `default Expr` where `Expr`'s type is incompatible with the field type | `TypeMismatch` |
| **Collection element type mismatch** | `add Field Expr` where `Expr`'s type doesn't match the collection's element type | `TypeMismatch` |
| **Numeric literal incompatible** | Fractional literal in `integer` context, or exponent literal in `integer`/`decimal` context | `TypeMismatch` |

### Modifier Validation

Modifiers are constraints on field/arg values. The type checker validates applicability:

| Modifier | Applicable to | Error when applied to |
|----------|---------------|----------------------|
| `nonnegative` | `integer`, `decimal`, `number` | `string`, `boolean`, `choice`, collections, temporal, domain |
| `positive` | `integer`, `decimal`, `number` | (same as above) |
| `nonzero` | `integer`, `decimal`, `number` | (same as above) |
| `notempty` | `string` | `number`, `integer`, `decimal`, `boolean`, `choice`, collections |
| `min` / `max` | `integer`, `decimal`, `number` | `string`, `boolean`, collections |
| `minlength` / `maxlength` | `string` | `number`, `integer`, `decimal`, `boolean`, collections |
| `mincount` / `maxcount` | `set`, `queue`, `stack` | scalars |
| `maxplaces` | `decimal` | `integer`, `number`, `string`, `boolean`, collections |
| `ordered` | `choice` | all non-choice types |
| `optional` | any field type | — (always valid) |

**Modifier value validation:**

| Check | Fires when | Diagnostic |
|-------|-----------|------------|
| `min` > `max` | `min` value exceeds `max` value on the same field | `InvalidModifierBounds` |
| `minlength` > `maxlength` | `minlength` exceeds `maxlength` | `InvalidModifierBounds` |
| `mincount` > `maxcount` | `mincount` exceeds `maxcount` | `InvalidModifierBounds` |
| Negative `minlength`/`maxlength`/`mincount`/`maxcount`/`maxplaces` | Count/length value is negative | `InvalidModifierValue` |
| `maxplaces` not an integer | Decimal places must be a whole number | `InvalidModifierValue` |
| Duplicate modifier | Same modifier applied twice to one field | `DuplicateModifier` |
| Conflicting modifiers | `nonnegative` and `positive` on the same field (redundant but not conflicting — `positive` subsumes `nonnegative`) | `RedundantModifier` (warning) |

### Scope Rules

Precept has a small, well-defined scope model.

#### Global scope

Fields, states, and events are all declared at the top level. They are visible everywhere in the precept body. Order of declaration does not matter — all names are registered in a first pass before expression checking.

#### Expression scope

| Context | What's in scope |
|---------|----------------|
| Rule condition / guard | All field names |
| Ensure condition / guard | All field names |
| Transition row guard | All field names + current event's args (via `EventName.ArgName`) |
| Transition row actions (RHS of `set`, value of `add`/`enqueue`/`push`) | All field names + current event's args |
| State action guard / actions | All field names |
| Stateless event hook actions | All field names + current event's args |
| Default value expression | Field names declared **before** this field (no self-reference, no forward reference in defaults) |
| Computed expression (`field X as T -> Expr`) | All field names except those that would form a dependency cycle (no self-reference, no mutual cycles) |
| Modifier value expressions (`min N`, `max N`, etc.) | Only literal values — no field references |

#### Event arg access

Event args are accessed via dotted notation: `EventName.ArgName`. The type checker resolves this by:

1. Checking if the `Object` of a `MemberAccessExpression` is an `IdentifierExpression` that matches a declared event name.
2. If so, the `Member` is resolved against the event's arg declarations.
3. Event arg access is only valid in contexts where an event is in scope (transition rows, event ensures, stateless hooks).

### Access Mode Validation

| Check | Fires when | Diagnostic |
|-------|-----------|------------|
| **Field not declared** | Access mode names a field that doesn't exist | `UndeclaredField` |
| **State not declared** | Access mode scoped to a state that doesn't exist | `UndeclaredState` |
| **Computed field in write mode** | A computed field is listed in a `write` access mode | `ComputedFieldNotWritable` |
| **Conflicting access modes** | Same field has both `write` and `omit` in the same state | `ConflictingAccessModes` |

### Ensure/Rule Validation

| Check | Fires when | Diagnostic |
|-------|-----------|------------|
| **Condition must be boolean** | `ensure Expr` / `rule Expr` where `Expr` is not `boolean` | `TypeMismatch` |
| **Message must be string** | `because Expr` where `Expr` is not `string` or interpolated string | `TypeMismatch` |

### Built-in Function Catalog

The type checker validates function calls against a closed catalog of built-in functions. There are no user-defined functions.

| Function | Signature | Return type | Constraints |
|----------|-----------|-------------|-------------|
| `min(a, b)` | `(numeric, numeric) → numeric` | Common numeric type of args | — |
| `max(a, b)` | `(numeric, numeric) → numeric` | Common numeric type of args | — |
| `abs(value)` | `(numeric) → numeric` | Same numeric type as input | — |
| `clamp(value, lo, hi)` | `(numeric, numeric, numeric) → numeric` | Common numeric type | — |
| `floor(value)` | `(decimal\|number) → integer` | `integer` | — |
| `ceil(value)` | `(decimal\|number) → integer` | `integer` | — |
| `truncate(value)` | `(decimal\|number) → integer` | `integer` | — |
| `round(value)` | `(decimal\|number) → integer` | `integer` | Banker's rounding |
| `round(value, places)` | `(numeric, integer) → decimal` | `decimal` | `places` must be non-negative integer; **explicit bridge: number→decimal** |
| `approximate(value)` | `(decimal) → number` | `number` | **Explicit bridge: decimal→number**; makes precision loss visible |
| `pow(base, exp)` | `(numeric, integer) → numeric` | Same numeric type as `base` | `exp` must be non-negative for integer lane |
| `sqrt(value)` | `(numeric) → number` | `number` | Number-lane only; proof engine checks non-negativity |
| `trim(value)` | `(string) → string` | `string` | — |
| `startsWith(s, prefix)` | `(string, string) → boolean` | `boolean` | Case-sensitive prefix test |
| `endsWith(s, suffix)` | `(string, string) → boolean` | `boolean` | Case-sensitive suffix test |
| `toLower(s)` | `(string) → string` | `string` | Lowercase (invariant culture) |
| `toUpper(s)` | `(string) → string` | `string` | Uppercase (invariant culture) |
| `left(s, n)` | `(string, integer) → string` | `string` | Leftmost N code units (clamped to string length) |
| `right(s, n)` | `(string, integer) → string` | `string` | Rightmost N code units (clamped to string length) |
| `mid(s, start, length)` | `(string, integer, integer) → string` | `string` | 1-indexed substring (clamped); `start` and `length` must be positive `integer` |
| `now()` | `() → instant` | `instant` | — |

**Lane bridge functions.** Two functions are the sole explicit bridges between numeric lanes: `approximate(decimal) → number` and `round(value, places) → decimal`. The rounding family (`floor`, `ceil`, `truncate`, `round` with no places) provide `decimal|number → integer`. No other mechanism crosses lane boundaries — `decimal * NumberField` without `approximate()` is a type error (see §4.2a).

**Function validation checks:**

| Check | Fires when | Diagnostic |
|-------|-----------|------------|
| Unknown function name | `foo(...)` where `foo` is not in the catalog | `UndeclaredFunction` |
| Wrong arity | `min(a)` or `min(a, b, c)` | `FunctionArityMismatch` |
| Arg type mismatch | `min("a", "b")` — strings to numeric function | `TypeMismatch` |
| Arg constraint violation | `round(x, -1)` — negative places | `FunctionArgConstraintViolation` |

### Action Statement Validation

| Action | Field type required | Value type required | Additional checks |
|--------|--------------------|--------------------|-------------------|
| `set F = Expr` | Any scalar | Assignable to field type | Field must not be computed |
| `add F Expr` | `set of T` | `T` | — |
| `remove F Expr` | `set of T` | `T` | — |
| `enqueue F Expr` | `queue of T` | `T` | — |
| `dequeue F (into G)?` | `queue of T` | — | If `into G`, `G` must be type `T`. Requires emptiness proof (`UnguardedCollectionMutation`) |
| `push F Expr` | `stack of T` | `T` | — |
| `pop F (into G)?` | `stack of T` | — | If `into G`, `G` must be type `T`. Requires emptiness proof (`UnguardedCollectionMutation`) |
| `clear F` | Any collection | — | — |

Type errors: applying a set operation to a non-set field, a queue operation to a non-queue field, etc.

### Transition Outcome Validation

| Check | Fires when | Diagnostic |
|-------|-----------|------------|
| **Undeclared target state** | `transition StateName` where `StateName` is not declared | `UndeclaredState` |
| **Reject message not string** | `reject Expr` where `Expr` is not string | `TypeMismatch` |

### List Literal Validation

| Check | Fires when | Diagnostic |
|-------|-----------|------------|
| **Element type mismatch** | List element type doesn't match collection element type | `TypeMismatch` |
| **List in non-default position** | List literal used outside a `default` clause | `ListLiteralOutsideDefault` |
| **Empty list as default** | Valid — empty collection | — |

### Choice Type Validation

| Check | Fires when | Diagnostic |
|-------|-----------|------------|
| **Duplicate choice value** | `choice("a", "a")` | `DuplicateChoiceValue` |
| **Empty choice** | `choice()` — no values | `EmptyChoice` |
| **Non-string choice value** | `choice(42)` | `TypeMismatch` |

### Computed Field Validation

| Check | Fires when | Diagnostic |
|-------|-----------|------------|
| **Self-reference** | Computed expression references its own field | `CircularComputedField` |
| **Transitive cycle** | Computed fields form a dependency cycle (A→B→A, or A→B→C→A, etc.) | `CircularComputedField` |
| **Expression type mismatch** | Computed expression type doesn't match field type | `TypeMismatch` |
| **Computed field with default** | Field has both `->` and `default` | `ComputedFieldWithDefault` |
| **Computed field as write target** | `set` action targets a computed field | `ComputedFieldNotWritable` |

**Dependency graph construction.** The type checker builds a directed graph of computed field dependencies: an edge from field A to field B means A's computed expression references B. Self-references are detected during expression walking (immediate error). After all computed expressions are typed, the checker runs a topological sort on the dependency graph. If the sort fails (cycle detected), every field participating in the cycle receives a `CircularComputedField` diagnostic naming the cycle path.

The topological order also determines the evaluation order for computed fields at runtime — fields that depend on nothing are evaluated first, then their dependents, and so on. This order is emitted as part of the typed model for the evaluator.

---

## Diagnostic Catalog

### Existing codes (already in `DiagnosticCode.cs`)

| Code | Stage | Severity | Message template | Fires when |
|------|-------|----------|------------------|------------|
| `UndeclaredField` | Type | Error | "Field '{0}' is not declared" | Identifier in expression context doesn't match any field |
| `TypeMismatch` | Type | Error | "Expected a {0} value here, but got '{1}'" | Type incompatibility in any expression context |
| `NullInNonNullableContext` | Type | Error | "'{0}' requires a value and cannot be empty here" | Optional field used where value is required |
| `InvalidMemberAccess` | Type | Error | "'.{0}' is not available on {1} fields" | Dot access on unsupported type |
| `FunctionArityMismatch` | Type | Error | "'{0}' takes {1} inputs, but {2} were provided" | Wrong number of function arguments |
| `FunctionArgConstraintViolation` | Type | Error | "Value {0} for '{1}' is not valid: {2}" | Function arg violates constraint |

### New codes (to be added to `DiagnosticCode.cs`)

| Code | Stage | Severity | Message template | Fires when |
|------|-------|----------|------------------|------------|
| `DuplicateFieldName` | Type | Error | "Field '{0}' is already declared" | Two field declarations with same name |
| `DuplicateStateName` | Type | Error | "State '{0}' is already declared" | Duplicate state entry |
| `DuplicateEventName` | Type | Error | "Event '{0}' is already declared" | Duplicate event declaration |
| `DuplicateArgName` | Type | Error | "Argument '{0}' is already declared on event '{1}'" | Duplicate arg in same event |
| `UndeclaredState` | Type | Error | "State '{0}' is not declared" | Reference to non-existent state |
| `UndeclaredEvent` | Type | Error | "Event '{0}' is not declared" | Reference to non-existent event |
| `UndeclaredFunction` | Type | Error | "'{0}' is not a recognized function" | Unknown function name in call |
| `MultipleInitialStates` | Type | Error | "Only one state can be marked 'initial' — '{0}' and '{1}' both are" | Two or more initial states |
| `NoInitialState` | Type | Error | "This precept has states but none is marked 'initial'" | Stateful precept without initial |
| `InvalidModifierForType` | Type | Error | "The '{0}' constraint does not apply to {1} fields" | Modifier on inapplicable type |
| `InvalidModifierBounds` | Type | Error | "{0} ({1}) cannot exceed {2} ({3})" | min > max, minlength > maxlength, etc. |
| `InvalidModifierValue` | Type | Error | "The value for '{0}' must be {1}" | Negative count/length, non-integer maxplaces |
| `DuplicateModifier` | Type | Error | "The '{0}' constraint is already applied to this field" | Same modifier twice |
| `RedundantModifier` | Type | Warning | "'{0}' is unnecessary — '{1}' already implies it" | nonnegative + positive |
| `ComputedFieldNotWritable` | Type | Error | "Field '{0}' is computed and cannot be assigned" | `set` targeting computed field, or in `write` mode |
| `ComputedFieldWithDefault` | Type | Error | "Field '{0}' is computed and cannot have a default value" | Both `->` and `default` |
| `CircularComputedField` | Type | Error | "Computed field '{0}' has a circular dependency: {1}" | Self-reference or transitive cycle in computed field dependency graph |
| `ConflictingAccessModes` | Type | Error | "Field '{0}' has conflicting access modes in state '{1}'" | write + omit same field same state |
| `ListLiteralOutsideDefault` | Type | Error | "List values can only appear in default clauses" | `[...]` outside default position |
| `DuplicateChoiceValue` | Type | Error | "Choice value '{0}' is duplicated" | Repeated string in choice set |
| `EmptyChoice` | Type | Error | "A choice type must have at least one value" | `choice()` with no args |
| `CollectionOperationOnScalar` | Type | Error | "'{0}' is a {1} operation, but '{2}' is not a {1}" | add/remove on non-set, etc. |
| `ScalarOperationOnCollection` | Type | Error | "'{0}' cannot be used with collection field '{1}'" | set = on collection field |
| `IsSetOnNonOptional` | Type | Error | "'{0}' always has a value — 'is set' only works on optional fields" | is set / is not set on required field |
| `EventArgOutOfScope` | Type | Error | "Event '{0}' arguments are not accessible here" | Event.Arg access outside transition/ensure/hook |
| `InvalidInterpolationCoercion` | Type | Error | "A {0} value cannot appear inside a text interpolation" | Collection in `{...}` inside string |
| `UnresolvedTypedConstant` | Type | Error | "Cannot determine the type of '{0}' — no type context available" | Typed constant in a position with no expected type |
| `InvalidTypedConstantContent` | Type | Error | "'{0}' is not a valid {1} value" | Content doesn't parse as the context-determined type |
| `DefaultForwardReference` | Type | Error | "Default value for '{0}' cannot reference '{1}', which is declared later" | Field default references later field |

### Business-domain type codes

These codes enforce business-domain type constraints (money, quantity, price, exchangerate, currency, unitofmeasure, dimension). Authoritative definitions in `business-domain-types.md`.

| Code | Stage | Severity | Message template | Fires when |
|------|-------|----------|------------------|------------|
| `QualifierMismatch` | Type | Error | "Value does not match the '{0}' qualifier on field '{1}'" | `in` constraint violation — assigned currency/unit doesn't match |
| `DimensionCategoryMismatch` | Type | Error | "Dimension '{0}' does not match the declared category '{1}' on field '{2}'" | `of` constraint violation — dimension mismatch |
| `CrossCurrencyArithmetic` | Type | Error | "Cannot combine '{0}' ({1}) with '{2}' ({3}) — different currencies" | Money values with different currencies in arithmetic |
| `CrossDimensionArithmetic` | Type | Error | "Cannot combine '{0}' ({1}) with '{2}' ({3}) — incompatible dimensions" | Quantity values with incompatible dimensions in arithmetic |
| `DenominatorUnitMismatch` | Type | Error | "Denominator unit '{0}' does not match operand unit '{1}'" | Rate denominator doesn't match operand |
| `DurationDenominatorMismatch` | Type | Error | "Duration cannot cancel '{0}' denominator — days, weeks, months, and years have variable length" | Duration vs variable-length time denominator |
| `CompoundPeriodDenominator` | Type | Error | "Compound period '{0}' cannot cancel single-unit denominator '{1}'" | Compound period vs single-unit denominator |
| `MutuallyExclusiveQualifiers` | Parse | Error | "'in' and 'of' cannot both appear on the same field declaration" | `in` and `of` on same field |
| `InvalidUnitString` | Type | Error | "'{0}' is not a valid unit" | Structural chars in atomic unit value |
| `InvalidCurrencyCode` | Type | Error | "'{0}' is not a recognized ISO 4217 currency code" | Invalid currency code |
| `InvalidDimensionString` | Type | Error | "'{0}' is not a recognized dimension" | Not a recognized UCUM dimension |
| `MaxPlacesExceeded` | Type | Error | "Value has {0} decimal places, but field '{1}' allows at most {2}" | Too many decimal places |

### Temporal type codes

These codes enforce temporal type constraints (date, time, instant, duration, period, timezone, zoneddatetime, datetime). Authoritative definitions in `temporal-type-system.md`.

| Code | Stage | Severity | Message template | Fires when |
|------|-------|----------|------------------|------------|
| `InvalidDateValue` | Type | Error | "Invalid date: {0} does not exist" | Typed constant content is syntactically valid but refers to a nonexistent date (e.g., Feb 30) |
| `InvalidDateFormat` | Type | Error | "Dates must be written as YYYY-MM-DD. Use '{0}'" | Typed constant content uses a non-ISO date format (e.g., MM/DD/YYYY) |
| `InvalidTimeValue` | Type | Error | "Invalid time: {0} must be 0–23 for hours, 0–59 for minutes and seconds" | Typed constant content has out-of-range time components |
| `InvalidInstantFormat` | Type | Error | "Instants must end with Z to indicate UTC. Use '{0}Z'" | Typed constant content is a valid datetime but missing the UTC designator |
| `InvalidTimezoneId` | Type | Error | "'{0}' is not a recognized timezone — use canonical IANA form like 'America/New_York'" | Typed constant content is not a canonical IANA timezone identifier (legacy abbreviations, Windows names) |
| `UnqualifiedPeriodArithmetic` | Type | Error | "Period field '{0}' may contain {1} components — use `period of '{2}'` to constrain it" | Arithmetic with a period field that lacks an `of` qualifier where the target type requires provably date-only or time-only components |
| `MissingTemporalUnit` | Type | Error | "A bare number doesn't specify a unit. Use '{0} + ''{1}''' to add {1}" | Bare integer/number used in arithmetic with a temporal type (e.g., `DueDate + 2`) |
| `FractionalUnitValue` | Type | Error | "Unit values must be whole numbers. Use smaller units for fractions: '{0}'" | Non-integer magnitude in a temporal unit literal (e.g., `'0.5 days'`) |

### Collection safety codes

These codes enforce the emptiness guard requirement for collection accessors and mutations. They ensure that `.peek`, `.min`, `.max`, `dequeue`, and `pop` never operate on an empty collection at runtime.

| Code | Stage | Severity | Message template | Fires when |
|------|-------|----------|------------------|------------|
| `UnguardedCollectionAccess` | Type | Error | "'{0}' may be empty — guard with `if {0}.count > 0` before accessing `.{1}`" | `.peek`, `.min`, or `.max` used without emptiness proof (conditional guard or `mincount` constraint) |
| `UnguardedCollectionMutation` | Type | Error | "'{0}' may be empty — guard with `if {0}.count > 0` before `{1}`" | `dequeue` or `pop` used without emptiness proof (conditional guard or `mincount` constraint) |

---

## Error Recovery

### T3 — Error Type Propagation

**Decision: Poison type with cascade suppression.**

When the type checker encounters an unresolvable expression (missing node, undeclared name, type error in a sub-expression), it assigns `ErrorType` to that expression. `ErrorType` is compatible with every other type for the purpose of further checking — it suppresses all downstream type errors that would cascade from the original failure.

**Rules:**

1. Any operation involving `ErrorType` produces `ErrorType`.
2. `ErrorType` satisfies any type constraint — no further diagnostics are emitted for expressions that already carry `ErrorType`.
3. `ErrorType` never appears in a valid program. It only exists in the presence of other diagnostics.

**Rationale:**

This is the standard approach used by Roslyn, TypeScript, and Go. Without poison types, a single undeclared field would cascade into dozens of "type mismatch" errors at every expression that references it. The user sees one diagnostic (the root cause) instead of many (the symptoms).

### Handling `IsMissing` AST nodes

| Node category | Recovery behavior |
|---|---|
| Declaration with `IsMissing` name | Skip — do not add to symbol table. Parser already emitted a diagnostic. |
| Expression with `IsMissing` | Assign `ErrorType`. No diagnostic emitted (parser already reported it). |
| TypeRef with `IsMissing` | Resolve to `ErrorType`. Fields with error types still appear in the symbol table but their type is `ErrorType`. |
| Guard with `IsMissing` subexpression | Guard is assigned `ErrorType`. The transition row is still processed — other checks continue. |
| Missing state/event name tokens | Skip the containing declaration. |

### Principle: One diagnostic per root cause

The type checker emits diagnostics for root causes only. When `ErrorType` is flowing through an expression tree, the type checker stays silent. The first diagnostic emitted for a given expression chain is the root cause; all subsequent type mismatches involving `ErrorType` are symptoms.

---

## Design Decisions

### T1 — TypedModel as flat symbol tables + declaration arrays

**Chosen:** String-keyed symbol tables (`Fields`, `States`, `Events` dictionaries), flat declaration arrays (`Rules[]`, `TransitionRows[]`, etc. in source order), and a diagnostics array. No typed tree, no node-keyed type map.

**Three consumer paths:**

| I have... | I want... | I use... |
|-----------|-----------|----------|
| A name (`"Score"`) | Its declaration + type | `model.Fields["Score"]` |
| Nothing — iterating | All rules / transitions / etc. | `model.Rules`, `model.TransitionRows` |
| A syntax node from position | Its declaration | Syntax tree for position → identifier name → symbol table |

**Why string-keyed symbol tables exist:** The type checker needs them for name resolution. When it encounters `IdentifierExpression { Name: "Score" }`, the only key connecting the reference to the declaration is the string `"Score"`. String-keyed lookup is the join between reference sites and declaration sites. Exposing these tables to consumers is free — they already exist.

**Why no node-keyed type map:** Every anticipated consumer was traced — LS (semantic tokens, completions, hover, go-to-definition), MCP compile, graph analyzer, proof engine, runtime. None hold a random syntax node and ask "what type is this?" without already having a name or a resolved declaration. The LS hover path goes: position → syntax tree → identifier name → symbol table. No consumer needs `Dictionary<Expression, ResolvedType>` on the model.

**Why no typed tree:** Precept declarations are a flat list — no nested scopes, no modules, no closures. A typed tree wrapping them would be a flat list of typed declarations — structurally identical to the arrays, with extra ceremony. No consumer walks a typed tree root; they either look up by name or iterate by kind. This matches the pattern used by DSL-scale compilers (Go `types.Info`, CEL, Rego) — flat maps, no typed tree. Full-complexity languages (Roslyn, TypeScript) build typed trees for nested scope resolution, which Precept doesn't need.

**Why consumers don't need pre-built indexes:** At DSL scale (10–50 transition rows, 5–20 rules), linear scans of `ImmutableArray<T>` are faster than dictionary lookups due to cache locality. If indexing is ever needed (e.g., `(state, event) → rows`), it belongs in `Precept.From()` (the one-time compilation-to-runtime bridge), not in the TypedModel.

**Alternatives considered:**

- _Full tree rewrite (bound tree):_ Rejected — no consumer walks a typed tree. Structural overhead for no gain.
- _Node-keyed type map (Go `types.Info` pattern):_ Rejected — no consumer has a syntax node without also having a name or resolved declaration. The map had no real consumer.
- _Pre-built declaration indexes (e.g., `(state, event) → rows`):_ Rejected — different consumers want different index shapes. Linear scans are sufficient at DSL scale. Consumers can build their own if profiling shows a need.

### T4 — Context-dependent literal resolution: uniform rule, no fallback

**Chosen:** All context-dependent literals resolve from expression context. If context cannot determine the type, the type checker emits a diagnostic and assigns `ErrorType`. No implicit fallback.

This applies uniformly to:

| Literal kind | Context source | No context → |
|---|---|---|
| Numeric (`42`, `3.14`) | Field type, operator peer, function signature | Diagnostic + ErrorType |
| Typed constant (`'30 days'`, `'USD'`, `'2026-04-15'`) | Field type, operator peer, function signature | Diagnostic + ErrorType |
| List literal (`[1, 2, 3]`) | Target field's collection element type | Diagnostic + ErrorType |

**Rationale:** An implicit fallback (e.g., "whole numbers default to integer") creates silent type assumptions. In every realistic Precept expression, context is always available — every expression traces back to a declared field, operator peer, or function signature. The only case where context is absent is a constant expression referencing no data (e.g., `rule 42 > 0 because "..."`), which shouldn't exist in a meaningful precept. Rejecting it explicitly is better than silently assuming a type.

This applies uniformly: numeric literals, typed constants, and list literals all follow the same context-born resolution model.

**Alternatives considered:**

- _Numeric fallback:_ Whole numbers default to `integer`, fractional to `decimal`. Rejected — implicit behavior that hides the absence of a typed anchor.
- _Literal-first:_ `42` is always `integer`, `3.14` is always `decimal`. Widening handles mismatches. Rejected — unnecessary widening conversions.
- _Suffixed literals:_ `42i`, `42d`, `42n`. Rejected by the language vision — no literal suffixes.

### T5 — Modifier validation at check time (not parse time)

**Chosen:** The parser accepts all modifiers on all fields. The type checker validates applicability.

**Rationale:** The parser has no type information. It cannot know that `notempty` is invalid on a `number` field. The type checker already has the field's resolved type and can emit precise diagnostics.

### T6 — Two-pass declaration processing

See Architecture § Two-pass processing above. Two clean passes — registration then checking — are simpler than single-pass with forward-reference fixup.

### T7 — Built-in functions and operators as catalog system

**Chosen:** Functions and operators are validated via the catalog system pattern defined in [catalog-system.md](../catalog-system.md). Each uses a `Kind` enum (`FunctionKind`, `OperatorKind`) with `[Meta]` attributes, a `GetMeta()` method (exhaustive switch, CS8509-enforced), an `All` property, and a `Create()` factory. The type checker calls `FunctionCatalog.GetMeta(kind)` to retrieve parameter types and return types. No registration mechanism, no user-defined functions, no extension point.

**Rationale:** The catalog pattern is already established for `DiagnosticCode`, `TokenCategory`, and `ConstructKind`. Functions and operators follow the same shape — a closed, enumerable set known at compile time. The exhaustive switch ensures the compiler catches missing cases when a new function or operator is added. This is the single source of truth for both the type checker (type signatures) and the evaluator (runtime dispatch).

### T8 — ErrorType as a singleton

**Chosen:** `ErrorType()` has no parameters. All error-typed expressions carry the same type object.

**Alternative considered:** `ErrorType(DiagnosticCode reason)` — carry the original error. Rejected — no consumer needs to distinguish between different error causes via the type. The diagnostic is already emitted. The type's only job is to suppress cascades.

### T9 — Stateless precepts: no special case, one cross-validation

**Chosen:** The type checker does not have a "stateless mode." If the state table is empty, state-dependent checks simply don't fire. One cross-validation: if a precept contains both `StatelessEventHookDeclaration` nodes (`on Event -> actions`) and any state declarations, the type checker emits an error.

**Rationale:** Stateless event hooks (`on Event -> set X = ...`) are syntactically distinct from transition rows (`from State on Event -> ...`). In a stateful precept, `on Event -> actions` is redundant with `from any on Event -> no transition` followed by rules. Allowing both creates ambiguity about execution order and whether the hook participates in the transition pipeline. Erroring on the combination forces authors to use the stateful form (`from any on Event ->`) explicitly.

**Degenerate case:** A stateless precept (no states, no `from`, no transitions) that uses only event hooks is valid. This is the intended authoring path for stateless precepts — event hooks work identically to their stateful counterparts without transition semantics.

---

## Consumer Contracts

### Graph analyzer

`GraphAnalyzer.Analyze(TypedModel)` receives the typed model as input. See [architecture-planning.md § 2.4](../architecture-planning.md#24-graph-analyzer) for the graph analyzer's scope and design requirements. Its contracts:

- Reads `TypedModel.TransitionRows` to build the state-event transition graph.
- Reads `TypedModel.States` and `TypedModel.InitialState` for the state set.
- Does not re-check types — trusts `ResolvedType` on all expressions.
- Uses `ErrorType` as a signal to skip transitions with unresolved components.

### Proof engine

`ProofEngine.Prove(TypedModel)` receives the typed model as input. See [architecture-planning.md § 2.5](../architecture-planning.md#25-proof-engine) for the proof engine's scope, proof attribution model, and design requirements. Its contracts:

- Reads `TypedModel.Rules`, `TypedModel.Ensures`, and guard expressions on `TypedModel.TransitionRows`.
- Uses `TypedExpression.Type` to determine which interval arithmetic operations apply.
- `ErrorType` expressions are skipped — no proof obligations generated for unresolvable expressions.

### Language server

The language server reads `CompilationResult.TypedModel`. Its contracts:

| LS feature | What it reads |
|------------|--------------|
| Diagnostics | `TypedModel.Diagnostics` (type-stage codes) |
| Hover | Symbol tables — `Fields[name]`, `States[name]`, `Events[name]` for type info display |
| Go-to-definition | `FieldSymbol.Span`, `StateSymbol.Span`, `EventSymbol.Span` — declaration source positions |
| Completions | `Fields`, `States`, `Events` dictionaries for name suggestions in expression positions |
| Semantic tokens | `ResolvedType` on typed expressions for type-aware token classification |

### MCP server

MCP tools (`precept_compile`) receive `CompilationResult`, which contains `TypedModel`. Type diagnostics surface in the `Diagnostics` array with `DiagnosticStage.Type`. MCP callers can distinguish type errors from parse errors via the stage field. The `Fields`, `States`, `Events` dictionaries and `TransitionRows` array provide the structured compilation output for `precept_compile`.

---

## Deliberate Exclusions

The type checker intentionally does NOT:

- **Build a transition graph.** The type checker resolves transition rows (from-states, event name, guard type, action types, outcome) but does not connect them into a graph structure. That is the graph analyzer's job.
- **Reason about state reachability.** Whether a state is reachable from the initial state, or whether a state is a dead end, requires graph traversal. The type checker builds the raw transition data that makes this analysis possible.
- **Perform interval arithmetic.** Whether a guard is satisfiable, whether a `sqrt()` argument is provably non-negative, whether a `min`/`max` constraint is achievable — these require interval reasoning over typed expressions. That is the proof engine's job.
- **Evaluate expressions at runtime.** The type checker assigns types to expressions but does not compute their values. Runtime evaluation is the evaluator's job.
- **Validate typed constant content without a registered validator.** The type checker validates content when a validator is registered for the expected type (§4.4 step 3). If no validator is registered for a type, the checker accepts shape validation only. Content validation is never deferred to runtime — when the validator exists, malformed constants are compile-time errors.
- **Short-circuit on first error.** The type checker always runs to completion. `ErrorType` propagation ensures broken sub-expressions do not cascade into symptom diagnostics.
- **Resolve names contextually.** All name resolution is flat — field names, state names, event names, arg names are looked up in their respective symbol tables. There are no nested scopes, no closures, no modules.

---

## Cross-References

| Topic | Document |
|-------|----------|
| Pipeline stages and artifact contracts | [pipeline-artifacts-and-consumer-contracts.md](pipeline-artifacts-and-consumer-contracts.md) |
| Literal system — resolution contracts | [literal-system.md](literal-system.md) §Type Checker |
| Diagnostic infrastructure | [diagnostic-system.md](diagnostic-system.md) |
| Parser output (input to type checker) | [parser.md](parser.md) §Consumer Contracts |
| Catalog system pattern | [catalog-system.md](../catalog-system.md) |
| Numeric lane integrity and bridges | [precept-language-vision.md](../language/precept-language-vision.md) §Numeric lanes |
| Language spec §3 (type checker section) | [precept-language-spec.md](../language/precept-language-spec.md) §3 |
| Temporal type system design | [temporal-type-system.md](../language/temporal-type-system.md) |
| Business-domain types design | [business-domain-types.md](../language/business-domain-types.md) |
| AST node types | `src/Precept.Next/Pipeline/SyntaxNodes.cs` |
| Existing diagnostic codes | `src/Precept.Next/Pipeline/DiagnosticCode.cs` |
| Existing diagnostic messages | `src/Precept.Next/Pipeline/Diagnostics.cs` |
| Original architecture planning notes | [architecture-planning.md](../architecture-planning.md) §2.3 |

---

## Source Files

| File | Purpose |
|------|---------|
| `src/Precept.Next/Pipeline/TypeChecker.cs` | Type checker implementation — static class, `CheckSession` struct |
| `src/Precept.Next/Pipeline/TypedModel.cs` | `TypedModel` record + all symbol/resolved types (expand from current stub) |
| `src/Precept.Next/Pipeline/ResolvedType.cs` | `ResolvedType` hierarchy |
| `src/Precept.Next/Pipeline/TypedExpression.cs` | `TypedExpression` wrapper |
| `src/Precept.Next/Pipeline/DiagnosticCode.cs` | Add new Type-stage diagnostic codes |
| `src/Precept.Next/Pipeline/Diagnostics.cs` | Add metadata entries for new codes |
| `test/Precept.Next.Tests/TypeCheckerTests.cs` | Type checker test suite |
