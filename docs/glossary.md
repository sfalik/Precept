---
status: Active
purpose: One-paragraph canonical definitions for Precept's load-bearing terms, with pointers to the docs that elaborate
---

# Glossary

Load-bearing terms used across the Precept corpus. Each entry is a one-paragraph definition with a category tag and a pointer to the canonical doc. Use this as a discovery aid — grep here when a term feels ambiguous before grepping the rest of the docs.

The glossary is **not** authoritative for behavior. The canonical docs (linked from each entry) are. Where a definition here diverges from a canonical doc, the canonical doc wins.

## Conventions

Entries are alphabetical. Each entry carries a **category tag**:

- **[Language]** — what `.precept` authors see in source files
- **[Catalog]** — the metadata-driven architecture
- **[Pipeline]** — compiler stages and the artifacts that flow between them
- **[Runtime]** — post-compile execution
- **[Process]** — lifecycle, design, review
- **[Philosophy]** — core commitments

Polysemous terms are listed multiple times with disambiguators (e.g., **Descriptor [Runtime]** vs **Descriptor [Tooling]**).

---

## A

### Accessor [Language] [Catalog]
A member-access operation on a catalog-defined type. `field X as datetime` admits `.year`, `.month`, etc.; the accessor surface is declared in the relevant `TypeMeta`. The parser routes member-access tokens through the accessor declarations; the type checker validates the result type. Canonical: [`catalog-system.md § Accessor declarations`](language/catalog-system.md).

### Action [Language] [Catalog]
A discrete operation a transition can perform — `set`, `assign`, `transition`, `reject`, `no transition`, plus action-style verbs that participate in row outcomes. Declared in the `Actions` catalog. Canonical: [`precept-language-spec.md § Actions`](language/precept-language-spec.md), [`catalog-system.md § Actions catalog`](language/catalog-system.md).

### Approximation honesty [Philosophy]
Precept does not present approximation as exactness. The distinction between exact and approximate types (e.g., `decimal` vs `number`) is visible at the surface, and the proof engine refuses to prove what it cannot prove exactly. See [philosophy.md](philosophy.md) and the Approximation Stance sections in the type docs (Phase 3 of the corpus improvement plan).

### Audience [Philosophy]
The primary author of a `.precept` definition is the **domain expert / business analyst**, not the software developer. This constrains every language-surface decision — keyword-anchored grammar, mandatory `because` clauses, readable diagnostics, no opaque proof. Canonical: [philosophy.md § Who authors a precept](philosophy.md).

---

## B

### Because clause [Language]
The mandatory rationale attached to a `rule` or `ensure`. `rule X >= 0 because "Negative amounts not allowed"`. Forces every constraint to carry a domain-readable explanation. See Principle 9 in [`precept-language-spec.md § 0.1`](language/precept-language-spec.md).

---

## C

### Catalog [Catalog]
A registry of metadata describing a closed set of language elements (tokens, types, modifiers, actions, etc.). Each catalog is a `*Kind` enum + a `*Meta` record + a static class with an exhaustive `GetMeta(kind)` switch. The catalogs together ARE the Precept language specification in machine-readable form. The canonical inventory lives in [`catalog-system.md`](language/catalog-system.md).

### Catalog member [Catalog]
A single entry in a catalog — one enum value with its corresponding metadata record. Adding a catalog member is the atomic act of adding a language feature; the exhaustive switch on the enum forces every downstream consumer to handle it.

### Catalog DU [Catalog]
A catalog whose `*Meta` type is a **discriminated union** (abstract base + sealed subtypes) rather than a flat record, because members fall into groups with genuinely different fields. Switching on the DU **subtype** is correct (the subtype IS the metadata shape); switching on the catalog enum identity to dispatch per-member behavior is a violation. Canonical: [`catalog-system.md § Meta record shape`](language/catalog-system.md).

### Compilation [Pipeline]
The artifact produced by the compiler pipeline — a fully-typed, validated, proven representation of a `.precept` definition. Consumed by the runtime (`PreceptBuilder`) to produce an executable model. See [`compiler-and-runtime-design.md § The pipeline`](compiler-and-runtime-design.md).

### Configuration [Runtime]
A snapshot of a precept instance's data + state at a moment in time. Operations against a precept produce new Configurations; the runtime guarantees every Configuration satisfies the precept's rules. Canonical: [`runtime/runtime-api.md`](runtime/runtime-api.md).

### Construct [Language] [Catalog]
A top-level structural element in `.precept` source — `field`, `state`, `event`, `rule`, `ensure`, `transition`, `from`, etc. Declared in the `Constructs` catalog with a leading-token map (`ByLeadingToken`) and slot model (which expressions/identifiers fill which positions). Canonical: [`catalog-system.md § Construct Slot Model`](language/catalog-system.md).

### ConstructMeta [Catalog]
The metadata record for a `Construct` catalog entry. Carries leading tokens, slot definitions, syntax forms, and per-construct parser dispatch metadata. Source: `src/Precept/Language/Constructs.cs`.

---

## D

### Descriptor [Runtime]
A first-class runtime identity for a declared program element (field, state, event, arg, constraint, fault site). The runtime references program elements by descriptor rather than name. Canonical: [`runtime/descriptor-types.md`](runtime/descriptor-types.md).

### Descriptor [Tooling]
In TextMate grammar / LSP semantic tokens, a "scope descriptor" classifies tokens for visual display. Unrelated to runtime descriptors. Canonical: [`compiler/tooling-surface.md`](compiler/tooling-surface.md).

### Determinism [Philosophy]
Same definition + same data + same operation produces the same outcome every time. No hidden state, no time-dependent behavior, no platform-dependent floating-point. Canonical: [philosophy.md](philosophy.md), [`precept-language-spec.md § 0.1 Principle 3`](language/precept-language-spec.md).

### Diagnostic [Pipeline] [Catalog]
A compile-time message reporting a violation or condition. Each diagnostic carries a code (e.g., `PRE0123`), severity, message template, and (often) recovery steps. Declared in the `Diagnostics` catalog. The runtime mirror is **Fault**. Canonical: [`compiler/diagnostic-system.md`](compiler/diagnostic-system.md).

### Domain expert primary author [Philosophy]
See **Audience**.

---

## E

### Ensure [Language]
A constraint attached to an event handler — `on Activate ensure Price > 0 because "Plan price must be positive"`. Asserts a precondition that, if violated, rejects the event. Distinct from `rule` (which constrains data continuously). Canonical: [`precept-language-spec.md § Ensures`](language/precept-language-spec.md).

### Evaluator [Runtime]
The runtime component that executes plans — applies an event, validates against rules, computes the new Configuration, returns the outcome. Canonical: [`runtime/evaluator.md`](runtime/evaluator.md).

### Event [Language]
A named operation that can be fired against a precept instance, optionally carrying typed arguments. `event Activate with Plan as string, Price as number`. Triggers transitions and rule evaluation. Canonical: [`precept-language-spec.md § Events`](language/precept-language-spec.md).

### ExpressionForm [Catalog]
A catalog entry for a kind of expression the parser recognizes — literal, identifier, binary operation, member access, function call, etc. Declared in the `ExpressionForms` catalog with binding-power and led-form metadata. Canonical: [`catalog-system.md § ExpressionForms catalog`](language/catalog-system.md).

---

## F

### Fault [Runtime] [Catalog]
A runtime failure mode — the runtime counterpart of a compile-time diagnostic. Each fault carries a code, severity, and message template. Declared in the `Faults` catalog. Faults are emitted when invariants the compiler couldn't prove are violated at runtime. Canonical: [`runtime/fault-system.md`](runtime/fault-system.md).

### Field [Language]
A typed data slot on a precept — `field PlanName as string nullable`. Carries a type, optional modifiers (default, nullable, nonnegative, maxplaces, etc.), and optional computed expression. Canonical: [`precept-language-spec.md § Fields`](language/precept-language-spec.md).

### From row [Language]
A transition row — `from State on Event when Guard -> Action -> Action -> transition NewState`. The atomic unit of state machine behavior. Canonical: [`precept-language-spec.md § Transition rows`](language/precept-language-spec.md).

---

## G

### Governance not validation [Philosophy]
Rules are declarations bound to the entity, structurally enforced on every operation — not validators called at boundaries that can be bypassed. The entity cannot exist in an invalid state, period. See [philosophy.md](philosophy.md).

### GraphAnalyzer [Pipeline]
The pipeline stage that analyzes the state graph — detects unreachable states, dead-end states, missing transitions. Consumes `SemanticIndex`, produces `StateGraph`. Canonical: [`compiler/graph-analyzer.md`](compiler/graph-analyzer.md).

### Guard [Language]
The optional `when` clause on a transition row, rule, or ensure that gates its applicability. `from Trial on Activate when PlanName == null -> ...`. Canonical: [`precept-language-spec.md § Guards`](language/precept-language-spec.md).

---

## I

### Inspectability [Philosophy]
Every behavior of a precept must be inspectable at compile time — including which actions would fire on a hypothetical event, what diagnostics each transition surfaces, what proof obligations the proof engine discharged. No black boxes. See [philosophy.md](philosophy.md).

---

## M

### Modifier [Language] [Catalog]
A keyword that attaches behavior to a construct — `nullable`, `default 0`, `nonnegative`, `maxplaces 2`, `initial`, `terminal`. The `Modifiers` catalog is a DU with subtypes for value modifiers (carry a value), flag modifiers (presence-only), state lifecycle modifiers (`initial`, `terminal`), access modifiers, and anchor modifiers. The exact applicability and semantics live in the catalog entry. Canonical: [`catalog-system.md § Modifiers catalog`](language/catalog-system.md).

---

## O

### One file, complete rules [Philosophy]
Every field, rule, ensure, and transition for a precept lives in the `.precept` definition. No scattered logic across validators, handlers, or service code. See [philosophy.md](philosophy.md), [`precept-language-spec.md § 0.1 Principle 2`](language/precept-language-spec.md).

### Outcome [Language] [Catalog]
The terminating action of a transition row — `transition NewState`, `no transition`, `reject "message"`. Declared in the `Outcomes` catalog. Distinct from intermediate actions (`set`, `assign`) that prepare for the outcome. Canonical: [`catalog-system.md § Outcomes catalog`](language/catalog-system.md).

---

## P

### Pipeline [Pipeline]
The ordered sequence of compiler stages: Lexer → Parser → NameBinder → TypeChecker → GraphAnalyzer → ProofEngine. Each stage consumes one artifact type and produces another. Stage boundaries are type contracts. Canonical: [`compiler-and-runtime-design.md`](compiler-and-runtime-design.md), [`compiler/README.md`](compiler/README.md).

### Plan [Runtime]
The PreceptBuilder output — an execution-ready data structure (descriptor tables, dispatch indexes) the Evaluator consumes to apply operations. Distinct from a Configuration (which is data-at-rest); a Plan is the machinery that transforms Configurations. Canonical: [`runtime/precept-builder.md`](runtime/precept-builder.md).

### Precept [Language] [Runtime]
The top-level entity definition. As a language construct, it is the named contract — `precept Subscription` — that owns all fields, states, events, rules, transitions. As a runtime type (`Precept<TConfig>`), it is the compiled, type-safe handle to that contract.

### Prevention not detection [Philosophy]
Invalid configurations are structurally impossible. The compiler refuses to build a precept where an invariant can be violated; the runtime cannot construct a Configuration that violates the rules. Detection happens at compile time; the runtime is the safe consequence. See [philosophy.md](philosophy.md), [`precept-language-spec.md § 0.1 Principle 1`](language/precept-language-spec.md).

### ProofEngine [Pipeline]
The pipeline stage that discharges proof obligations — divisor safety, sqrt safety, dead guards, contradictory rules, tautological guards, assignment-constraint satisfaction. Consumes typed semantic state, produces a `ProofLedger`. Canonical: [`compiler/proof-engine.md`](compiler/proof-engine.md).

### ProofRequirement [Catalog]
A catalog entry describing what kind of proof obligation a construct produces. The proof engine routes obligations through the requirements catalog. Canonical: [`catalog-system.md § ProofRequirements catalog`](language/catalog-system.md).

### ProofSubject [Pipeline]
The "thing" a proof obligation is about — an expression, a constraint, a transition. A DU shape — different subject kinds carry different metadata for the proof engine to reason about. Source: `src/Precept/Pipeline/ProofObligations.cs`.

---

## Q

### Qualifier [Language] [Pipeline]
A type-narrowing parameter that flows through a typed value — `money in 'USD'` (currency qualifier), `quantity in 'kg'` (unit qualifier), `exchangerate in 'USD' to 'EUR'`. Qualifiers participate in type checking (cross-currency arithmetic is rejected) and proof obligations. Canonical: [`catalog-system.md § Qualifier Propagation`](language/catalog-system.md).

---

## R

### Rule [Language]
A continuous invariant on a precept's data — `rule MonthlyPrice >= 0 because "..."`. Must hold in every Configuration. Distinct from `ensure` (event-scoped). Canonical: [`precept-language-spec.md § Rules`](language/precept-language-spec.md).

---

## S

### SemanticIndex [Pipeline]
The TypeChecker's output artifact — a **flat semantic inventory** of typed fields, typed states, typed events, typed rules, typed transitions. Explicitly NOT a structural mirror of the parse tree; downstream stages consume semantic data, not parser shape. Canonical: [`compiler/type-checker.md`](compiler/type-checker.md), [`compiler-and-runtime-design.md § SemanticIndex`](compiler-and-runtime-design.md).

### Slot [Catalog]
A positional placeholder in a construct's syntax — `field <Name> as <Type>` has a name slot and a type slot. The `Constructs` catalog declares each construct's slot model; the parser fills slots with `SlotValue` instances. Canonical: [`catalog-system.md § Construct Slot Model`](language/catalog-system.md).

### SlotValue [Pipeline]
The parsed content of a single slot in a construct — an expression, identifier, modifier list, or sub-construct. Source: `src/Precept/Pipeline/SlotValue.cs`.

### State [Language]
A named lifecycle position for a precept. `state Trial initial, Active, Cancelled`. Optional — stateless precepts are first-class. Canonical: [`precept-language-spec.md § States`](language/precept-language-spec.md).

### Stateless first-class [Philosophy]
A precept can govern stateful workflows (with lifecycle states and transitions) **or** stateless domain objects (fields, rules, constraints — no states). Stateless is not a degenerate case; it's a primary mode. See [philosophy.md](philosophy.md), [`precept-language-spec.md § 0.1 Principle 2`](language/precept-language-spec.md).

---

## T

### Token [Language] [Catalog]
A lexer-produced unit — keyword, identifier, operator, punctuation, literal. Declared in the `Tokens` catalog with category, syntactic role, and (for keywords) `ValidAfter` rules used by completion filtering. Canonical: [`compiler/lexer.md`](compiler/lexer.md), [`catalog-system.md § Tokens catalog`](language/catalog-system.md).

### Transition row [Language]
See **From row**.

### TypeChecker [Pipeline]
The pipeline stage that types every expression, validates modifier applicability, builds the `SemanticIndex`. Canonical: [`compiler/type-checker.md`](compiler/type-checker.md).

### TypedField [Pipeline]
The TypeChecker's typed representation of a `field` declaration — name, resolved type, modifiers (typed), default expression (typed), computed expression (typed), declared bounds (e.g., `DeclaredMaxLength`, `DeclaredMaxPlaces`). Lives in `SemanticIndex`. Source: `src/Precept/Pipeline/SemanticIndex.cs`.

### TypedState [Pipeline]
The TypeChecker's typed representation of a `state` declaration — name, initial/terminal flags, modifiers, source span. Lives in `SemanticIndex`.

---

## V

### Version [Runtime]
A runtime type (`Version<TConfig>`) representing a specific Configuration of a precept instance at a specific point in time. Operations on a Version produce new Versions; old Versions remain inspectable. Canonical: [`runtime/runtime-api.md`](runtime/runtime-api.md).

---

## Cross-reference: docs that elaborate each category

- **Language surface**: [`docs/language/README.md`](language/README.md), [`docs/language/precept-language-spec.md`](language/precept-language-spec.md), the four type docs
- **Catalog system**: [`docs/language/catalog-system.md`](language/catalog-system.md), [`docs/contributing/catalog-driven-checklist.md`](contributing/catalog-driven-checklist.md)
- **Pipeline**: [`docs/compiler/README.md`](compiler/README.md), [`docs/compiler-and-runtime-design.md`](compiler-and-runtime-design.md), per-stage docs in `docs/compiler/`
- **Runtime**: [`docs/runtime/README.md`](runtime/README.md), per-component docs in `docs/runtime/`
- **Philosophy**: [`docs/philosophy.md`](philosophy.md)
- **Process**: [`CONTRIBUTING.md`](../CONTRIBUTING.md), the lifecycle skills in `.claude/skills/`

If a term you need isn't here, grep the corpus and add it — the glossary is a living artifact, not a fixed reference.
