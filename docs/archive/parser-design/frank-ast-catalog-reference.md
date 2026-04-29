# AST Node Type Reference on ConstructMeta — Architectural Analysis

**By:** Frank
**Date:** 2026-04-27
**Status:** Architectural ruling — answers Shane's question
**References:** `frank-catalog-parser-scope.md` (Layer D scope), `frank-catalog-parser-cross-review.md` (Layer D walkback), `george-catalog-parser-estimate.md` (BuildNode estimate)

---

## Shane's Question

> "Regarding the AST node hierarchy — can't we have a reference in the catalog to the AST node type to simplify that?"

Shane is asking whether `ConstructMeta` could carry a `Type NodeType` or a factory delegate so `ParseConstruct()` can instantiate the correct AST node without a per-construct switch.

---

## 1. Is This Feasible in C#?

**Yes, mechanically.** Three shapes are possible:

### Option A: `Type` reference

```csharp
public sealed record ConstructMeta(
    ConstructKind                Kind,
    string                       Name,
    string                       Description,
    string                       UsageExample,
    ConstructKind[]              AllowedIn,
    IReadOnlyList<ConstructSlot> Slots,
    TokenKind                    LeadingToken,
    Type                         NodeType,        // ← typeof(FieldDeclarationNode)
    string?                      SnippetTemplate = null);
```

Usage: `Activator.CreateInstance(meta.NodeType, span, slots...)`. Requires reflection. No compile-time type safety on constructor signatures. If `FieldDeclarationNode` changes its constructor parameters, the call site compiles but fails at runtime.

### Option B: Typed factory delegate

```csharp
public sealed record ConstructMeta(
    ...
    Func<SourceSpan, SyntaxNode?[], Declaration> NodeFactory,
    ...);
```

Usage: `meta.NodeFactory(span, parsedSlots)`. No reflection. But the delegate signature is generic — every construct receives a `SyntaxNode?[]` and must positionally extract its named fields. The factory is a closure over per-construct knowledge:

```csharp
ConstructKind.FieldDeclaration => new ConstructMeta(
    ...
    NodeFactory: (span, slots) => new FieldDeclarationNode(
        span,
        (IdentifierListNode)slots[0],
        (TypeExpressionNode?)slots[1],
        (ModifierListNode?)slots[2],
        (ComputeExpressionNode?)slots[3]),
    ...),
```

### Option C: External factory registry (not on ConstructMeta)

```csharp
// Separate from catalog — registered at parser startup
FrozenDictionary<ConstructKind, Func<SourceSpan, SyntaxNode?[], Declaration>> NodeFactories;
```

Mechanically identical to Option B, but the factory lives outside the catalog.

**All three are feasible.** The question is whether they should exist and where they should live.

---

## 2. Does It Actually Solve the BuildNode Problem?

**No. It relocates the per-construct code; it doesn't eliminate it.**

Walk through `FieldDeclaration` with slots: `[IdentifierList, TypeExpression, ModifierList, ComputeExpression]`.

After generic slot parsing, you have: `SyntaxNode?[] parsedSlots = [identifiers, typeExpr, modifiers, computeExpr]`.

To build `FieldDeclarationNode(span, identifiers, typeExpr, modifiers, computeExpr)`, something must:

1. **Know the positional mapping** — slot 0 is identifiers, slot 1 is type, etc.
2. **Know the types** — slot 0 must be cast to `IdentifierListNode`, slot 1 to `TypeExpressionNode?`.
3. **Know the constructor signature** — `FieldDeclarationNode` takes these 4 slots in this order.

Whether this knowledge lives in:
- A `switch` arm in `BuildNode()` (current design)
- A lambda in `ConstructMeta.NodeFactory` (Option B)
- A lambda in a separate registry (Option C)
- A reflective call with `Activator.CreateInstance` (Option A)

...it is the **same per-construct knowledge expressed in a different location.** The 11 per-construct mapping functions still exist. They're just hosted differently.

**The factory on `ConstructMeta` doesn't reduce the code — it distributes it.** Instead of one `BuildNode` switch with 11 arms (centralized, greppable, testable as a unit), you have 11 lambdas scattered across `Constructs.GetMeta()` entries. That's strictly worse for debuggability — a breakpoint in `BuildNode` catches all 11 constructs; breakpoints in 11 scattered lambdas require 11 breakpoints.

### The real BuildNode cost

George's estimate put `BuildDeclaration` at **20 hours** out of Layer D's 154 total. That's the actual cost of writing the per-construct factory — 11 exhaustive switch arms mapping positional slots to named record fields. The per-construct switch is not the expensive part of Layer D. The expensive parts are the generic slot iterator (~30h), the slot metadata enrichment (~12h), the `ActionChain` loop encoding (~40h), and the regression suite (~40h). `BuildNode` itself is straightforward — it's mechanical field-mapping.

**Moving the factories into `ConstructMeta` saves zero hours from the estimate.** You still write 11 factory functions. You just write them as lambdas inside `Constructs.GetMeta()` instead of switch arms inside `BuildNode()`.

---

## 3. Architectural Fit with the Metadata-Driven Model

**This is where the proposal fails.**

### The catalog's job

The catalog describes **what the language is** — keywords, constructs, slots, types, operators, constraints. Catalogs are the language specification in machine-readable form. Their consumers are: grammar generation, completions, hover, semantic tokens, MCP vocabulary, diagnostics. All of these need to know what the language *looks like*. None of them need to know how AST nodes are constructed.

### The AST node hierarchy's job

AST node types are **pipeline infrastructure** — internal data structures the parser produces and the type checker consumes. They are not language surface. They don't appear in `.precept` files. They don't carry semantics that tooling consumers need. They are implementation details of the compilation pipeline.

### The decision framework test

From `catalog-system.md` § Architectural Identity:

> "Is it language surface? Does it appear in .precept files, carry semantics that consumers need, or represent a concept that would appear in a complete description of the Precept language?"

`FieldDeclarationNode` is not language surface. It is a C# record that the parser outputs. A complete description of the Precept language mentions `field` declarations, not `FieldDeclarationNode`. The catalog describes that a field declaration has slots `[IdentifierList, TypeExpression, ModifierList, ComputeExpression]` — that IS the language description. How those slots get assembled into an internal C# type is pipeline machinery, not language structure.

### Dependency direction violation

The catalog lives in `src/Precept/Language/`. The AST node hierarchy lives in (or will live in) `src/Precept/Pipeline/`. The dependency direction is: **Pipeline depends on Language, not the reverse.**

- `Parser.cs` reads `ConstructMeta.Slots` — Pipeline → Language ✓
- `TypeChecker.cs` reads `DiagnosticMeta` — Pipeline → Language ✓
- Grammar generation reads `Tokens.All` — Tooling → Language ✓

If `ConstructMeta` holds `Type NodeType = typeof(FieldDeclarationNode)` or a factory delegate that constructs `FieldDeclarationNode`, then Language depends on Pipeline. That's a circular dependency. The catalog would need to `using Precept.Pipeline;` to reference AST types.

**Option A (`Type NodeType`)**: requires `typeof(FieldDeclarationNode)` in the catalog initializer. Circular dependency.

**Option B (factory delegate)**: requires the lambda to reference `FieldDeclarationNode`'s constructor. Same circular dependency.

**Option C (external registry)**: avoids the circular dependency by keeping factories in Pipeline. But then the factory is NOT on `ConstructMeta` — it's a separate registry keyed by `ConstructKind`. At that point you've arrived at exactly the `BuildNode` switch, just spelled as a dictionary.

### Verdict on architectural fit

**Options A and B violate the dependency direction between Language and Pipeline.** Option C avoids the violation but doesn't put anything on `ConstructMeta`, which means it's not Shane's proposal — it's just the `BuildNode` switch in dictionary form.

---

## 4. Alternatives

### Alternative 1: The exhaustive switch (what we already designed)

```csharp
// In Parser infrastructure (Pipeline layer)
private static Declaration BuildNode(ConstructKind kind, SourceSpan span, SyntaxNode?[] slots)
    => kind switch
    {
        ConstructKind.FieldDeclaration => new FieldDeclarationNode(span,
            (IdentifierListNode)slots[0],
            (TypeExpressionNode?)slots[1],
            (ModifierListNode?)slots[2],
            (ComputeExpressionNode?)slots[3]),
        ConstructKind.StateDeclaration => new StateDeclarationNode(span,
            (IdentifierListNode)slots[0],
            (StateModifierListNode?)slots[1]),
        // ... 9 more arms
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
```

**Properties:**
- Centralized — one method, greppable, one breakpoint
- Exhaustive — CS8509 fires when construct #12 is added
- Pipeline-internal — no catalog dependency pollution
- 11 arms × ~3 lines each = ~33 lines of mechanical code
- George estimated 20h including edge cases and tests

### Alternative 2: Per-construct factory dictionary (Pipeline-internal)

```csharp
// In Parser infrastructure (Pipeline layer)
private static readonly FrozenDictionary<ConstructKind, Func<SourceSpan, SyntaxNode?[], Declaration>> NodeFactories
    = new Dictionary<ConstructKind, Func<SourceSpan, SyntaxNode?[], Declaration>>
    {
        [ConstructKind.FieldDeclaration] = (span, slots) => new FieldDeclarationNode(span,
            (IdentifierListNode)slots[0], ...),
        // ...
    }.ToFrozenDictionary();
```

**Properties:** Same as Alternative 1 but in dictionary form. No exhaustive switch enforcement (dictionaries don't get CS8509). Adds indirection for no benefit. Strictly worse.

### Alternative 3: Source-generated factory

A source generator that reads `ConstructMeta.Slots` and generates the `BuildNode` switch from AST node record constructors. Possible if AST node constructors follow a strict convention: constructor parameters match slot order and types are derivable from `ConstructSlotKind`.

**Problems:** The mapping from `ConstructSlotKind` to C# AST node type (`ConstructSlotKind.IdentifierList → IdentifierListNode`) is itself per-construct knowledge. The source generator needs to know it. You've moved the knowledge from a hand-written switch to a source generator convention. At 11 constructs, the generator is more complex than the switch it replaces.

### Recommendation: Alternative 1 (exhaustive switch)

The `BuildNode` exhaustive switch is the right pattern. It's:
- In the right layer (Pipeline, not Language)
- Enforced by the compiler (CS8509 on missing arms)
- Centralized and greppable
- 33 lines of mechanical code at 11 constructs
- Not the expensive part of any parser design

---

## 5. Impact on George's Layer D Estimate

**None.** The `BuildNode` factory was estimated at 20 hours. That number doesn't change regardless of where the factory code lives. The factory is 13% of Layer D's 154-hour total. Even if a catalog type reference eliminated the factory entirely (it doesn't), Layer D would go from 154h to 134h — still 3.5–4 weeks, still the same risk profile, still not recommended.

Layer D was walked back because of the **slot iteration complexity** (ActionChain loops, Outcome sub-grammar, boundary-token contracts, full regression rewrite) — not because of `BuildNode`. The factory is the easy part. Putting a type reference on `ConstructMeta` addresses the cheapest 13% of the problem while introducing an architectural layer violation.

---

## 6. Ruling

**No. Do not add a Type reference or factory delegate to `ConstructMeta`.**

**Reasons:**
1. **Layer violation.** `ConstructMeta` is Language; AST nodes are Pipeline. The dependency direction is Pipeline → Language, not the reverse. A `Type`/delegate reference on `ConstructMeta` inverts this.
2. **Doesn't eliminate per-construct code.** The 11 factory functions exist regardless of where they're hosted. A lambda on `ConstructMeta` and a switch arm in `BuildNode` contain identical per-construct knowledge.
3. **Scatters what should be centralized.** 11 lambdas distributed across `Constructs.GetMeta()` entries are harder to debug, harder to grep, and harder to review than one exhaustive switch.
4. **Doesn't change the Layer D calculus.** The factory is 13% of Layer D's cost. Layer D was rejected for the other 87%.
5. **Fails the catalog decision framework.** "Does it appear in .precept files, carry semantics that consumers need, or represent a concept in a complete description of the language?" AST node types fail all three tests.

**The correct shape for the factory is: a `BuildNode` exhaustive switch on `ConstructKind` in the Pipeline layer, co-located with the parser.** This is what my scope document already specified. Shane's instinct — "can the catalog simplify this?" — is the right instinct applied to the wrong layer.

### What the catalog CAN simplify

The catalog already contributes everything it should to this problem:

| What the catalog provides | How it helps |
|---------------------------|-------------|
| `ConstructMeta.Slots` | Defines the slot sequence — the generic iterator reads this |
| `ConstructSlot.Kind` | Routes to the correct slot parser via `ConstructSlotKind → ParseMethod` dictionary |
| `ConstructSlot.IsRequired` | Drives optionality logic in the generic iterator |
| `ConstructMeta.Kind` | Keys the `BuildNode` switch to select the correct AST factory |

The catalog defines the **what** (which slots, in which order, required or optional). The parser defines the **how** (parse each slot, assemble the AST node). `BuildNode` is squarely in the "how" — it's pipeline machinery, not language description.

---

## Precedent Note

This analysis reinforces the boundary we established in the cross-review: **catalog-drive vocabulary always; catalog-describe structure for consumers; hand-write grammar mechanics.** AST node construction is grammar mechanics. It stays hand-written, in the Pipeline layer, enforced by the compiler's exhaustive switch.
