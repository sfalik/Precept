# Name Binder

## 1. Status

| Property | Value |
|---|---|
| Doc maturity | Full |
| Implementation state | Implemented |
| Source | `src/Precept/Pipeline/NameBinder.cs`, `src/Precept/Pipeline/SymbolTable.cs` |
| Upstream | `ConstructManifest` (from Parser) |
| Downstream | TypeChecker, LS name-level features (completions, identifier semantic tokens, "did you mean?") |

---

## 2. Overview

The name binder is a **declaration collector and reference resolver** — a discrete pipeline stage between the Parser and the TypeChecker. It transforms a `ConstructManifest` into a `SymbolTable` containing all declared symbols (fields, states, events, event args) and all resolved identifier references, with diagnostics for duplicate declarations and undeclared names.

This stage exists so the TypeChecker receives a pre-resolved symbol table and never performs name lookup or naming diagnostics. The separation follows the classic compiler architecture pattern: structural parsing, name binding, and type checking are three distinct concerns with cleanly separated responsibilities.

### Architectural Motivation

Traditional DSL compilers often combine name binding into the type checker's first pass. Precept separates them for three reasons:

1. **Clean concern separation.** Name binding requires zero type information — it collects declarations and resolves references purely by name. Type checking requires resolved names but adds semantic validation, expression resolution, and catalog-driven type inference. Merging them conflates two independent concerns.

2. **Language server flexibility.** The `SymbolTable` gives the LS useful data even when the TypeChecker is a stub or has errors. Completions for field/state/event targets, identifier semantic tokens, and "did you mean?" suggestions all derive from the `SymbolTable` without requiring a working type checker.

3. **TypeChecker simplification.** The TypeChecker receives pre-resolved names and never does name lookup, duplicate detection, or reference existence checks. Its only job is semantic validation — type compatibility, expression resolution, and structural analysis.

---

## 3. Responsibilities and Boundaries

### What It Does

- **Collects all declared names:** field declarations (with type, modifiers, computed status, declaration order), state declarations (with modifiers), event declarations (with args and initial marker)
- **Builds O(1) lookup dictionaries:** `FieldsByName`, `StatesByName`, `EventsByName`
- **Resolves all identifier references** to their declarations, producing `SymbolReference` records with resolution targets
- **Enforces scoping rules:** quantifier bindings > event args (in event context) > fields
- **Detects duplicate declarations:** emits `DuplicateFieldName`, `DuplicateStateName`, `DuplicateEventName` diagnostics
- **Detects undeclared references:** emits `UndeclaredField`, `UndeclaredState`, `UndeclaredEvent`, `UndeclaredArg` diagnostics
- **Detects quantifier binding shadowing:** emits `BindingShadowsField` when a quantifier binding name collides with a field name
- **Detects forward references in computed fields:** a computed field expression that references a field declared later in the precept emits `UndeclaredField`

### What It Does NOT Do

- **No type checking** — does not validate type compatibility, expression types, or operation legality
- **No constraint validation** — does not check guard boolean requirements, ensure clause semantics, or rule structure
- **No graph analysis** — does not reason about state reachability or transition coverage
- **No catalog-driven semantic resolution** — does not consult `Operations`, `Functions`, or `Types` catalogs for semantic meaning (the TypeChecker owns this)
- **No cross-precept resolution** — does not resolve references across precept files
- **No import resolution** — no module system exists

---

## 4. Right-Sizing

The NameBinder is a separate stage rather than a TypeChecker pass because:

| Criterion | Separate stage | TypeChecker pass |
|---|---|---|
| Concern purity | Name resolution only — zero type knowledge | Mixed: name + type concerns in one class |
| LS value without TC | `SymbolTable` available even when TC is stub | No intermediate artifact; all-or-nothing |
| TC simplicity | TC receives resolved names; no lookup code | TC must maintain symbol tables + do type work |
| Diagnostic ownership | Naming diagnostics clearly owned by one stage | Naming diagnostics mixed with type diagnostics |
| Testability | Test name binding in isolation from type checking | Must test through TC entry point |
| Pipeline stage count | +1 stage (6 total) | No change (5 stages) |

The +1 stage cost is trivial. The concern-separation benefit is architectural.

---

## 5. Inputs and Outputs

### Input

`ConstructManifest` containing `ImmutableArray<ParsedConstruct>` from the parser. The name binder iterates over constructs and dispatches on `ConstructKind` for declaration-bearing constructs.

### Output

`SymbolTable` — an immutable record containing:

```csharp
public sealed record SymbolTable(
    // Declarations
    ImmutableArray<DeclaredField> Fields,
    ImmutableArray<DeclaredState> States,
    ImmutableArray<DeclaredEvent> Events,

    // O(1) Name Lookup Dictionaries
    ImmutableDictionary<string, DeclaredField> FieldsByName,
    ImmutableDictionary<string, DeclaredState> StatesByName,
    ImmutableDictionary<string, DeclaredEvent> EventsByName,

    // Reference Sites
    ImmutableArray<SymbolReference> References,

    // Stage Diagnostics
    ImmutableArray<Diagnostic> Diagnostics
);
```

### Entry Point

```csharp
public static SymbolTable Bind(ConstructManifest manifest)
```

Pure static function. No instance, no DI, no configuration. Follows the pipeline pattern: `Lexer.Lex`, `Parser.Parse`, `NameBinder.Bind`, `TypeChecker.Check`, `GraphAnalyzer.Analyze`, `ProofEngine.Prove`.

---

## 6. Architecture

The name binder makes two passes over the construct list:

### Pass 1: Declaration Collection

**Input:** `ConstructManifest.Constructs`
**Output:** Mutable declaration builders + working dictionaries in `BinderState`

Dispatches on `ConstructKind`:

| ConstructKind | Action |
|---|---|
| `FieldDeclaration` | Extract name(s), type, modifiers, computed status → build `DeclaredField` with declaration order; detect duplicates |
| `StateDeclaration` | Extract state entries → build `DeclaredState` per entry; detect duplicates |
| `EventDeclaration` | Extract name(s), args, initial marker → build `DeclaredEvent` with `DeclaredArg` array; detect duplicates |
| *(all others)* | Skip — no declarations to collect |

Duplicate detection uses working dictionaries (`_fieldsByName`, `_statesByName`, `_eventsByName`). On duplicate, a diagnostic is emitted and the duplicate is skipped (first declaration wins).

### Pass 2: Reference Resolution

**Input:** Constructs + declaration dictionaries from Pass 1
**Output:** `ImmutableArray<SymbolReference>` + diagnostics for undeclared references

For each construct, resolves:
- **State target slots** — resolve state name references (transition targets)
- **Event target slots** — resolve event name references (rule/transition event anchors)
- **Field target slots** — resolve field name references (access mode targets)
- **Expression slots** — recursively walk guard, ensure, compute, rule, and action expressions; resolve identifiers per scoping rules
- **Outcome slots** — resolve transition target state references

Expression walking handles all `ParsedExpression` subtypes: `IdentifierExpression`, `BinaryOperationExpression`, `UnaryOperationExpression`, `MemberAccessExpression`, `ConditionalExpression`, `FunctionCallExpression`, `MethodCallExpression`, `QuantifierExpression`, `ListLiteralExpression`, `PostfixOperationExpression`, `InterpolatedStringExpression`, `CIFunctionCallExpression`, `GroupedExpression`, `LiteralExpression`, `MissingExpression`.

### Diagnostic Emission Strategy

Diagnostics are accumulated in a builder and emitted as part of the `SymbolTable.Diagnostics` array. The binder continues past errors — it does not short-circuit on first failure. This ensures the TypeChecker receives a best-effort `SymbolTable` even when some references are unresolved.

---

## 7. Component Mechanics

### 7.1 Declaration Records

| Record | Fields | Purpose |
|---|---|---|
| `DeclaredField` | `Name`, `Type` (ParsedTypeReference), `Modifiers`, `IsComputed`, `Syntax`, `NameSpan`, `DeclarationOrder` | Field declaration with ordering for forward-reference detection |
| `DeclaredState` | `Name`, `Modifiers` (ImmutableArray\<ModifierKind\>), `Syntax`, `NameSpan` | State declaration |
| `DeclaredEvent` | `Name`, `Args` (ImmutableArray\<DeclaredArg\>), `IsInitial`, `Syntax`, `NameSpan` | Event declaration with argument list |
| `DeclaredArg` | `Name`, `Type` (TypeMeta), `EventName`, `NameSpan` | Event argument scoped to its declaring event |

### 7.2 Symbol Resolution

`SymbolReference` records each reference site with its resolution result:

```csharp
public sealed record SymbolReference(
    SourceSpan Site,
    string Name,
    SymbolResolution Resolution);
```

`SymbolResolution` is a discriminated union:

| Subtype | Meaning |
|---|---|
| `FieldTarget(DeclaredField)` | Reference resolved to a field declaration |
| `StateTarget(DeclaredState)` | Reference resolved to a state declaration |
| `EventTarget(DeclaredEvent)` | Reference resolved to an event declaration |
| `ArgTarget(DeclaredArg)` | Reference resolved to an event argument (scoped to enclosing event context) |
| `BindingTarget(string)` | Reference resolved to a quantifier binding variable |
| `UnresolvedTarget(string, SymbolCategory)` | Reference could not be resolved — diagnostic emitted |

`SymbolCategory` enum: `Field`, `State`, `Event`, `Any`.

### 7.3 Scoping Rules

Identifier resolution follows a three-level scoping order (spec § 3):

1. **Quantifier bindings** (innermost first) — `for item in items` introduces `item` in the predicate scope
2. **Event args** (if in event context) — bare `argName` resolves to the enclosing event's argument
3. **Fields** — global field namespace

Event args shadow fields in event context. Quantifier bindings shadow both.

### 7.4 Qualified Event Argument Access

`MemberAccessExpression` nodes where the target is an event name are handled specially: `EventName.ArgName` resolves the target as an `EventTarget` and the member as an `ArgTarget` on that specific event. If the arg name is not found on the event, `UndeclaredArg` is emitted.

### 7.5 Forward Reference Detection

In computed field expressions, a reference to a field declared *after* the current field (by `DeclarationOrder`) is treated as unresolved. This enforces the rule that computed fields may only reference fields declared before them, preventing circular dependency chains.

---

## 8. Dependencies and Integration Points

### What NameBinder Reads from ConstructManifest

| Slot Kind | Used For |
|---|---|
| `IdentifierListSlot` | Field names, event names |
| `TypeExpressionSlot` | Field type (carried through as `ParsedTypeReference`) |
| `ModifierListSlot` | Field modifiers (carried through) |
| `ComputeExpressionSlot` | Computed field marker + expression to walk |
| `StateEntryListSlot` | State names + modifiers |
| `ArgumentListSlot` | Event argument names + types |
| `InitialMarkerSlot` | Initial event marker |
| `EventTargetSlot` | Event context for scope resolution |
| `StateTargetSlot` | State reference to resolve |
| `FieldTargetSlot` | Field reference to resolve |
| `GuardClauseSlot` | Guard expression to walk |
| `EnsureClauseSlot` | Ensure expression to walk |
| `RuleExpressionSlot` | Rule expression to walk |
| `ActionChainSlot` | Action expressions to walk |
| `OutcomeSlot` | Transition target state to resolve |

### What TypeChecker Expects from SymbolTable

The TypeChecker receives `(ConstructManifest, SymbolTable)`:

```csharp
internal static SemanticIndex Check(ConstructManifest manifest, SymbolTable symbols)
```

The TypeChecker trusts that:
- All declared names are collected in `SymbolTable.Fields`, `.States`, `.Events`
- O(1) dictionaries are available for name lookup
- All identifier references in expressions are resolved (or marked `UnresolvedTarget`)
- Duplicate-name and undeclared-reference diagnostics are already emitted
- The TypeChecker never performs name lookup or naming diagnostics

---

## 9. Failure Modes and Recovery

### Partial Results Strategy

The name binder continues past errors and produces a best-effort `SymbolTable`. The TypeChecker receives:
- All successfully collected declarations (duplicates are skipped, first wins)
- All successfully resolved references
- `UnresolvedTarget` entries for references that could not be resolved
- A complete diagnostics array

This ensures downstream stages and tooling always have something to work with, even on broken input.

### Duplicate Declaration Handling

When a duplicate name is detected (same name in the same namespace — fields, states, or events):
1. Emit diagnostic (`DuplicateFieldName`, `DuplicateStateName`, or `DuplicateEventName`)
2. Skip the duplicate — the first declaration wins and is retained in the dictionaries
3. Continue processing

### Undeclared Reference Handling

When a reference cannot be resolved:
1. Emit diagnostic (`UndeclaredField`, `UndeclaredState`, `UndeclaredEvent`, or `UndeclaredArg`)
2. Record an `UnresolvedTarget` in the `References` array
3. Continue processing

---

## 10. Contracts and Guarantees

| Contract | Guarantee |
|---|---|
| All resolved references are valid | Every `FieldTarget`, `StateTarget`, `EventTarget`, `ArgTarget`, `BindingTarget` in `References` points to a declaration that exists in the `SymbolTable` |
| TypeChecker never does name lookup | All name resolution is complete before the TypeChecker runs |
| No silent failures | Every unresolved reference produces both a diagnostic and an `UnresolvedTarget` record |
| Declaration order preserved | `Fields` array preserves source order; `DeclarationOrder` enables forward-reference detection |
| Scoping correctness | Quantifier bindings > event args > fields — shadowing follows spec § 3 |
| Quantifier binding safety | A quantifier binding that shadows a field name is a hard error (`BindingShadowsField`) |
| Best-effort output | The `SymbolTable` is always produced, even on broken input — never null, never partial abort |

---

## 11. Design Rationale and Decisions

### Why a Separate Stage?

**Decision (2026-05-07, locked):** Name binding is a separate pipeline stage between Parser and TypeChecker.

- **Rationale:** Name resolution — collecting declarations, resolving references, detecting duplicates and undeclared names — is a pure name-resolution concern that requires zero type information. The TypeChecker should receive pre-resolved symbols and focus exclusively on semantic validation. This follows the canonical compiler architecture pattern (lexing → parsing → name binding → type checking) and gives the LS useful data at the `SymbolTable` level even when the TypeChecker is a stub.
- **Alternatives considered:** Keeping name binding as TypeChecker Pass 1 (rejected — conflates concerns, delays LS value, complicates TypeChecker).
- **Precedent:** TypeScript separates binding (`binder.ts`) from checking (`checker.ts`). Roslyn separates declaration building from semantic analysis. The compiler pipeline architecture survey confirms this is universal practice.

### Quantifier Binding Shadows Field — Hard Error

**Decision (2026-05-07, Q6):** When a quantifier binding variable has the same name as a declared field, the NameBinder emits a hard error (`BindingShadowsField`). Silent shadowing is rejected because it cuts off access to the field inside the predicate with no escape hatch in current DSL syntax.

### Forward-Reference Detection in NameBinder

**Decision (2026-05-07, Q7):** Forward-reference detection (a computed field expression references a field declared later) is owned by the NameBinder, not the TypeChecker. Name resolution — including detecting that a name does not yet exist at the point of reference — is a name-resolution concern.

---

## 12. Innovation

The NameBinder follows the well-established two-pass binding pattern (declaration collection → reference resolution). The notable design choices are:

- **SymbolResolution as a discriminated union** with six subtypes, including `BindingTarget` for quantifier variables and `UnresolvedTarget` for failed resolution. This gives every reference site a typed resolution result that downstream consumers can pattern-match on without re-resolving.
- **DeclarationOrder on DeclaredField** enables forward-reference detection without maintaining a separate ordering structure.
- **Event-scoped argument resolution** with qualified access (`EventName.ArgName`) handled directly in expression walking.

---

## 13. Open Questions / Implementation Notes

> All design questions (Q5–Q8) are locked. No open questions remain.

**Implementation note:** The `BuildDictionaries()` method is currently a no-op because working dictionaries are populated during Pass 1 collection. The method exists as a structural placeholder for the pass boundary.

---

## 14. Deliberate Exclusions

| Exclusion | Rationale |
|---|---|
| Cross-precept resolution | No module system exists; each `.precept` file is self-contained |
| Import resolution | No import mechanism in the DSL |
| Type inference | Type information flows from `ParsedTypeReference` through to the TypeChecker; the NameBinder does not resolve or validate types |
| Expression type checking | Expression walking resolves identifiers only — it does not check operand types, function signatures, or operation legality |
| State modifier validation | Initial/terminal/required counting is deferred to the TypeChecker (semantic validation) |

---

## 15. Cross-References

| Document | Relationship |
|---|---|
| [Parser](./parser.md) | Upstream — produces `ConstructManifest` |
| [Type Checker](./type-checker.md) | Downstream — consumes `SymbolTable` + `ConstructManifest` |
| [compiler-and-runtime-design.md](../compiler-and-runtime-design.md) | Pipeline overview, artifact inventory |
| [diagnostic-system.md](./diagnostic-system.md) | Diagnostic code definitions and emission conventions |
| [Precept Language Spec](../language/precept-language-spec.md) | Scoping rules (§ 3) |

---

## 16. Source Files

| File | Purpose |
|---|---|
| `src/Precept/Pipeline/NameBinder.cs` | Name binder implementation — `NameBinder` static class with `Bind(ConstructManifest)` entry point |
| `src/Precept/Pipeline/SymbolTable.cs` | `SymbolTable` record, declaration records (`DeclaredField`, `DeclaredState`, `DeclaredEvent`, `DeclaredArg`), `SymbolReference`, `SymbolResolution` DU (`FieldTarget`, `StateTarget`, `EventTarget`, `ArgTarget`, `BindingTarget`, `UnresolvedTarget`), `SymbolCategory` enum |
| `src/Precept/Compiler.cs` | Pipeline orchestration — calls `NameBinder.Bind(manifest)` between `Parser.Parse` and `TypeChecker.Check` |
| `src/Precept/Pipeline/Compilation.cs` | `Compilation` record — includes `SymbolTable Symbols` field |
