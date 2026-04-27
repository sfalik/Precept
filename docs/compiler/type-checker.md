# Type Checker

## Status

| Property | Value |
|---|---|
| Doc maturity | Stub |
| Implementation state | Diagnostics-only stub |
| Source | `src/Precept/Pipeline/TypeChecker.cs`, `src/Precept/Pipeline/SemanticIndex.cs` |
| Upstream | SyntaxTree (from Parser), catalog metadata |
| Downstream | GraphAnalyzer, ProofEngine, Precept Builder, LS semantic features |

---

## Overview

The type checker is the first pipeline stage that reasons about semantics. It transforms `SyntaxTree` into `SemanticIndex` — a flat semantic inventory of resolved symbols, typed expressions, normalized declarations, and dependency facts. Its key design choice is producing a flat inventory (not an annotated tree) because downstream consumers need declarations organized by semantic role, not by source position.

---

## Responsibilities and Boundaries

**OWNS:** name resolution, type resolution, overload selection, modifier combination legality, semantic identity stamping for all declarations and expressions, SemanticIndex production.

**Does NOT OWN:** source structure (Parser), graph topology (GraphAnalyzer), proof obligations (ProofEngine), execution planning (Precept Builder).

---

## Right-Sizing

The type checker is the semantic resolution boundary — everything that requires names, types, or overloads is here; everything that requires topology or proof is downstream. The flat `SemanticIndex` shape is a deliberate sizing decision: if the inventory were tree-shaped, downstream consumers would have to walk it like a syntax tree, duplicating parser concerns. By producing a role-organized inventory, the type checker frees every downstream stage from knowing anything about source structure.

---

## Inputs and Outputs

**Input:**
- `SyntaxTree` (from Parser)
- Catalog metadata: Types, Functions, Operators, Operations, Modifiers, Actions, Constructs, Constraints, ProofRequirements, Diagnostics

**Output:**
- `SemanticIndex` — flat inventory: `TypedField[]`, `TypedState[]`, `TypedEvent[]`, `TypedTransitionRow[]`, typed expressions with resolved `OperationKind`/`FunctionKind`/`TypeKind`, dependency facts, syntax-node back-pointers, diagnostics

---

## Processing Model

Single-pass semantic resolution. Each declaration is resolved in catalog order: fields first (types resolved), states second (modifiers resolved), events and args third, transition rows fourth (guards and action chains resolved), constraints fifth. The `SemanticIndex` is built incrementally and sealed at the end of the pass.

---

## Semantic Inventory Design

### SemanticIndex Flat Inventory Shape

The `SemanticIndex` is NOT a decorated syntax tree — it is a flat inventory by semantic role. `TypedField` carries resolved `TypeKind`, modifier set, and back-pointer to `FieldDeclarationSyntax`. `TypedTransitionRow` carries resolved state/event identities, typed guard expression, typed action chain, and back-pointer to `TransitionRowSyntax`. Syntax back-pointers are navigation conveniences for LS features — downstream stages (graph, proof, builder) consume only the semantic inventories.

### Anti-Mirroring Rules

Downstream consumers (GraphAnalyzer, ProofEngine, Precept Builder) must consume semantic inventories, NOT traverse syntax via back-pointers. If graph analysis walked syntax to extract transitions, a parser recovery change could break it. The `SemanticIndex` must provide everything downstream needs as semantic facts.

### Type Resolution

Every `TypeRef` in the syntax tree is resolved to a `TypeKind` by catalog lookup. Unknown types produce a diagnostic; unknown fields in references produce a diagnostic. No partial resolution — each site is either fully resolved or carries an error marker.

### Expression Resolution

Expressions are resolved bottom-up. Leaf nodes resolve to `TypedField`, `TypedArg`, or `TypedLiteral` with known types. Binary expressions resolve to `OperationKind` by catalog lookup on the `(left TypeKind, operator, right TypeKind)` triple. Function calls resolve to `FunctionKind` by overload selection on argument type signatures.

### Typed Action Family

Actions resolve to exactly one of three sealed shapes:

- `TypedAction` — no input, no binding (e.g., `transition`, `clear`)
- `TypedInputAction` — input value (e.g., `set Amount to 100`)
- `TypedBindingAction` — binding to an arg or expression (e.g., `bind Status to event.Status`)

No flat nullable-field variants. This is a discriminated union enforced at the type system level.

---

## Dependencies and Integration Points

- **SyntaxTree** (upstream): all declaration nodes, expression nodes, identifier references
- **Catalog metadata** (upstream): Types, Functions, Operators, Operations, Modifiers, Actions, Constructs, Constraints, ProofRequirements, Diagnostics
- **GraphAnalyzer** (downstream): consumes TypedTransitionRow inventory and TypedState modifier facts
- **ProofEngine** (downstream): consumes typed expressions with resolved ProofRequirements
- **Precept Builder** (downstream): consumes entire SemanticIndex to build descriptor tables and execution plans
- **LS semantic features** (downstream): uses back-pointers + semantic inventories for hover, go-to-definition, completions

---

## Failure Modes and Recovery

The type checker accumulates all diagnostics without abandoning the pass. An unresolved type produces a `TypeKind.Unknown` marker that flows downstream without crashing; downstream stages treat Unknown as a signal to suppress derivative errors. Multiple declarations with the same name produce a diagnostic; only the first declaration is retained in the inventory.

---

## Contracts and Guarantees

- Every identifier in the output is either resolved to a catalog-backed identity or marked as an error with a corresponding diagnostic.
- No `SyntaxTree` nodes are silently discarded — every node produces either a typed inventory entry or a diagnostic.
- Downstream consumers receive a `SemanticIndex` even from partially invalid programs, allowing graph analysis and proof to run as far as the resolved portion permits.

---

## Design Rationale and Decisions

TBD — design rationale section to be populated during implementation.

---

## Innovation

- **Catalog-driven resolution:** Type checking resolves against catalog metadata rather than encoding per-construct behavior in checker logic. Adding an operation or function to the catalog automatically makes it resolvable — no type-checker code change needed.
- **Flat semantic inventory:** The `SemanticIndex` shape is driven by what consumers need, not by what the parser produces. The anti-mirroring rules enforce this structurally, preventing implementation drift toward tree-walking.
- **Syntax-node back-pointers with consumer discipline:** Cheap LS navigation (direct object reference, not span correlation) without polluting downstream semantic consumers with source structure concerns.
- **Three-shape typed action family:** Actions resolve to exactly one of three sealed shapes (`TypedAction`, `TypedInputAction`, `TypedBindingAction`) — no flat nullable-field variants. This makes action dispatch at evaluation time a structural property, not a runtime if-chain.

---

## Open Questions / Implementation Notes

1. `TypeChecker.Check` throws `NotImplementedException` — implementation not started beyond stub wiring.
2. Define the full `TypedField`, `TypedState`, `TypedEvent`, `TypedTransitionRow`, `TypedExpression` inventory shapes before implementing.
3. Decide `SemanticIndex` collection types: `ImmutableDictionary<string, TypedField>` vs. `ImmutableArray<TypedField>` — keyed by name for lookup, or ordered for iteration? Both consumers exist; consider supporting both via a wrapper.
4. Determine handling of duplicate declaration names (two fields with the same name) — emit diagnostic, add first, skip second? Consistent with parser recovery behavior.
5. Validate anti-mirroring rule enforcement: add a test that graph analysis, proof, and builder code paths do NOT call back-pointer properties.

---

## Deliberate Exclusions

- **No graph topology:** Reachability, dominance, and edge sets are the GraphAnalyzer's responsibility.
- **No proof obligations:** ProofRequirement instantiation is the ProofEngine's responsibility.
- **No runtime planning:** Descriptor production and execution plan compilation are the Precept Builder's responsibility.

---

## Cross-References

| Topic | Document |
|---|---|
| Full SemanticIndex design and anti-mirroring rules | `docs/compiler-and-runtime-design.md §6` |
| All catalogs the type checker consumes | `docs/language/catalog-system.md` |
| SyntaxTree input contract | `docs/compiler/parser.md` |
| SemanticIndex consumer | `docs/compiler/graph-analyzer.md` |

---

## Source Files

| File | Purpose |
|---|---|
| `src/Precept/Pipeline/TypeChecker.cs` | Type checker implementation — `TypeChecker` static class with `Check(SyntaxTree)` entry point |
| `src/Precept/Pipeline/SemanticIndex.cs` | `SemanticIndex` — flat semantic inventory artifact |
