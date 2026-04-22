# External Precedents — Per-State Field Access Modes

> **Author:** Frank (Lead/Architect & Language Designer)  
> **Date:** 2026-04-16  
> **Purpose:** Survey how other languages, DSLs, and formal systems handle fields having different properties (visibility, editability, presence) depending on entity state.  
> **Precept candidate syntax:** `in <StateList> define <FieldList> <mode>` where mode ∈ {`absent`, `readonly`, `editable`}

---

## 1. Alloy (MIT — Daniel Jackson)

**Category:** Formal specification language / relational logic

**How it handles per-state field properties:**
Alloy models state using signatures (`sig`) with fields declared as relations between sets. There is no built-in concept of fields existing only in certain states. All fields are structurally present on every instance of the signature.

**State-dependent constraints:** Expressed through facts and predicates that reference state predicates. A modeler might write `fact { all t: Thing | t.state = Open => no t.closedDate }` to constrain `closedDate` to be empty when state is Open — but this is a constraint on values, not a structural absence declaration.

**Absence handling:** Alloy uses the empty set (`none`) to represent absence. A relation `closedDate: lone Date` can be constrained to `no t.closedDate` in certain states, but the field structurally exists everywhere.

**Declaration site:** Fields are declared per-signature (type-level), never per-state.

**Keywords/syntax:** `sig`, `fact`, `pred`, `fun`, `assert`; no per-state field scoping keywords.

**Strengths for Precept:**
- Relational model validates that absence can be modeled as "empty set" — but also demonstrates the cognitive cost: every "absent" field must be manually constrained to emptiness.
- Alloy's fact-based constraint approach is close to Precept's `assert` model.

**Weaknesses for Precept:**
- No declarative per-state field scoping — everything is constraint math.
- Absence is implicit (constrained-to-empty) rather than explicit (structurally absent).
- Verbose: each state/field combination needs its own constraint expression.

---

## 2. TLA+ (Lamport)

**Category:** Formal specification language / temporal logic

**How it handles per-state field properties:**
All variables are declared globally with `VARIABLE` (or `VARIABLES`). There is no per-state variable scoping. Variables exist in every state of the specification; their values change but their existence does not.

**Absence handling:** TLA+ uses sentinel values — the canonical idiom is `NoVal == CHOOSE v : v \notin Val` to create a distinguished "not present" value. A variable can hold `NoVal` in states where it is logically absent. This is a convention, not a language feature.

**State-dependent constraints:** Type invariants (`TypeOK`) are global predicates that constrain variable values across all states. Per-state constraints are expressed as implications: `state = "Open" => closedDate = NoVal`.

**Declaration site:** Always global. Variables are declared at the module top level.

**Keywords/syntax:** `VARIABLE`, `CONSTANT`, primed notation (`x'`) for next-state values. No field-scoping keywords.

**Strengths for Precept:**
- TLA+'s `NoVal` pattern demonstrates the necessity of explicit absence semantics — tooling can't distinguish "field not yet set" from "field set to nothing" without a dedicated concept.
- The implication-based constraint style (`state = X => field = Y`) is the general form that Precept's `in X define F absent` would specialize.

**Weaknesses for Precept:**
- No structural absence — sentinel values pollute the value domain.
- No per-state field scoping or editability concept.
- Specification language, not an execution language — no enforced lifecycle.

---

## 3. SCXML / Statecharts (W3C)

**Category:** XML-based state machine standard / executable statecharts

**How it handles per-state field properties:**
SCXML allows `<datamodel>` and `<data>` elements as children of `<state>` elements (W3C Section 3.3.2). The `binding="late"` attribute on `<scxml>` defers data initialization until the parent state is first entered, enabling a form of per-state data declaration.

**Absence handling:** When `binding="late"`, data items declared in a state are not initialized until entry. Before first entry, the data item exists but has no meaningful value. After exit, the data item retains its last value — there is no structural removal on exit.

**Declaration site:** Data can be declared at document level or per-state. However, the ECMAScript data model (Appendix B.2.2) places ALL variables in a single global scope regardless of where they are declared syntactically. The per-state declaration is a cosmetic convenience, not a scoping mechanism.

**Keywords/syntax:** `<datamodel>`, `<data id="..." expr="...">`, `binding="early"|"late"`, `<assign location="..." expr="...">`.

**Strengths for Precept:**
- Closest mainstream standard to per-state field declaration.
- `binding="late"` is a precedent for deferred initialization — analogous to `absent` transitioning to `editable` on entry.
- The `In()` predicate for checking current state in conditions is similar to Precept's `in <State>` scoping.

**Weaknesses for Precept:**
- Per-state declaration is syntactic sugar over global scope in practice (ECMAScript data model).
- No concept of read-only vs editable — all data is always writable.
- No structural absence (field doesn't disappear on exit).
- XML verbosity makes the pattern hard to see.

---

## 4. Plaid (CMU — Aldrich et al.)

**Category:** Typestate-oriented programming language (research, now inactive → Wyvern)

**How it handles per-state field properties:**
Plaid is the system most directly relevant to Precept's `define` approach. In Plaid, a typestate is like a class that defines its own fields and methods. When an object transitions between typestates, its fields and methods change — the object literally gains and loses fields.

**Absence handling:** Structural. If a field is not declared in a typestate, it does not exist on the object in that typestate. Accessing it is a compile-time error (tracked by the permission-based type system).

**Declaration site:** Per-typestate. Each typestate declares its own set of fields. Fields are introduced at the typestate where they first appear and may or may not be present in subsequent typestates.

**Keywords/syntax:** `state`, `method`, permission annotations (`unique`, `immutable`, `shared`, `none`). State changes tracked through access permissions. Key papers: "Typestate-Oriented Programming" (Onward! '09), "First-Class State Change in Plaid" (OOPSLA '11).

**Strengths for Precept:**
- **Strongest precedent for structural per-state field presence.** Plaid proves the concept is implementable and type-checkable.
- Permission system (`immutable` vs `unique`/`shared`) is analogous to `readonly` vs `editable`.
- Fields are declared per-state, not globally with per-state constraints — this is exactly Precept's `define` model.

**Weaknesses for Precept:**
- Research language, inactive since ~2012. No production adoption data.
- Plaid's permission system is complex (5 permission kinds, fractions, borrowing) — far heavier than Precept's 3 modes.
- Object-oriented: state transitions change the object's type, which requires a linear type system. Precept doesn't need this because it controls the transition engine.

---

## 5. Rust Typestate Pattern

**Category:** API design pattern in a systems programming language

**How it handles per-state field properties:**
The Rust typestate pattern uses the type system to encode an object's state at compile time. Each state is a separate type (or type parameter), with operations only defined via `impl` blocks on the relevant state type. Different states can contain different fields by using state-specific structs.

**Key pattern — state types that contain actual state:**
```rust
struct HttpResponse<S: ResponseState> {
    state: Box<ActualResponseState>,
    extra: S,  // S is a state-specific struct with per-state fields
}
struct Start;  // no extra fields
struct Headers { response_code: u8 }  // adds response_code
```
In `Start` state, `response_code` doesn't exist. In `Headers` state, it does. This is enforced at compile time — accessing `self.extra.response_code` on `HttpResponse<Start>` fails to compile.

**Absence handling:** Structural via the type system. Fields not in the state type simply don't exist on the struct in that generic instantiation.

**Declaration site:** Per-state type. Each state type struct declares its own fields.

**Keywords/syntax:** `struct`, `impl`, `trait`, `PhantomData` marker for zero-sized type parameters. Method signatures use `self` by value (consumed) for state transitions.

**Strengths for Precept:**
- **Production-proven pattern** in a widely adopted language (serde, embedded systems, HTTP libraries).
- Demonstrates per-state field presence with compile-time enforcement — no runtime overhead.
- The `impl HttpResponse<Headers>` pattern (operations available only in certain states) parallels `in Headers define response_code editable`.
- State type parameters with traits (`SendingState`) enable multi-state groupings — analogous to `in [State1, State2] define ...`.

**Weaknesses for Precept:**
- Encoding is mechanical and boilerplatey — the programmer must manually create structs, impl blocks, PhantomData markers. Precept's `define` is a declarative DSL that eliminates this boilerplate.
- Rust's move semantics make state transitions consume the old value — not directly analogous to Precept's mutable field update model.
- No read-only concept — methods either exist or don't. Editability is about method signatures (`&self` vs `&mut self`), not about field access mode declarations.

---

## 6. TypeScript Discriminated Unions

**Category:** Type-level pattern in a mainstream programming language

**How it handles per-state field properties:**
TypeScript's discriminated unions use a shared literal-typed discriminant field (e.g., `kind`) to distinguish variants. Each variant is a separate interface with its own fields:
```typescript
interface Circle { kind: "circle"; radius: number; }
interface Square { kind: "square"; sideLength: number; }
type Shape = Circle | Square;
```
After narrowing via `if (shape.kind === "circle")`, TypeScript knows `shape.radius` exists and `shape.sideLength` does not.

**Absence handling:** Structural at the type level. Fields not declared on a variant are inaccessible after narrowing. Before narrowing, only the common discriminant field is accessible.

**Declaration site:** Per-variant interface. Each variant declares its own fields.

**Keywords/syntax:** `interface`, `type` (union), literal types for discriminants. `in` operator narrowing (`"swim" in animal`), `switch` on discriminant for exhaustive checking.

**Strengths for Precept:**
- **Closest mainstream-language analogy** to per-state field presence. The discriminant field is the "current state" and variants are "state-specific field sets."
- Exhaustive checking via `never` type ensures all states are handled — analogous to Precept's completeness analysis.
- `NetworkState` example (Loading/Failed/Success with different fields per state) is structurally identical to Precept's use case (different fields present in different lifecycle states).
- Massive adoption provides evidence that developers understand per-variant/per-state field presence.

**Weaknesses for Precept:**
- Type-level only — TypeScript doesn't enforce transitions. You can construct any variant at any time.
- No editability concept — all fields are either present (read/write) or absent. No `readonly` mode on a per-state basis.
- No lifecycle transitions — variants are independent types, not states in a progression.
- Narrowing requires explicit control flow; Precept's `in <State>` is declarative.

---

## 7. XState v5 (Stately)

**Category:** State machine library for JavaScript/TypeScript

**How it handles per-state field properties:**
XState v5 uses a single `context` object (extended state) that is shared across all states. Context is an unstructured bag — all fields exist in all states, and any event handler can `assign()` to any field.

**Absence handling:** No structural absence. Fields are initialized in the machine config's `context` and persist across all states. A field can be set to `null`/`undefined` by convention, but structurally it always exists.

**Per-state data scoping:** XState v5 does NOT support per-state context shapes. The `context` type is declared once for the entire machine. TypeScript typings enforce the context shape globally, not per-state. The `meta` property on state nodes can carry per-state metadata, but this is informational only — it doesn't affect context structure.

**Declaration site:** Machine-level `context` declaration. Per-state `meta` for annotations.

**Keywords/syntax:** `context`, `assign()` action, `meta`, `input` for actor initialization.

**Strengths for Precept:**
- XState's design decision to use global context is a useful **negative example** — it demonstrates the pain point Precept's `define` is solving. Developers must manually track which fields are meaningful in which states.
- `state.can(event)` for checking transition feasibility is analogous to Precept's guard inspection.
- `state.hasTag(tag)` for state grouping is a weaker form of Precept's multi-state `in [State1, State2]`.

**Weaknesses for Precept:**
- No per-state field scoping — exactly the problem Precept is solving.
- No read-only concept for context fields.
- Context is untyped at the per-state level — developers carry the cognitive burden of knowing which fields are meaningful where.

---

## 8. Drools (Apache KIE)

**Category:** Business rule management system / forward-chaining rule engine

**How it handles per-state field properties:**
Drools operates on facts in working memory using pattern matching. Rules fire when conditions match against the current fact set. Facts are Java objects (POJOs) with getter/setter access — field presence is determined by the Java class definition, not by any state concept.

**Absence handling:** A fact either exists in working memory or it doesn't. Within a fact, field values can be null, but the field structurally exists (it's a Java class member). Drools can test for null values in rule conditions.

**State-dependent constraints:** Rules use `when` conditions to match fact patterns. State-dependent behavior is encoded as separate rules that fire on different fact configurations — but there's no first-class state concept.

**Declaration site:** Fact types declared as Java classes or DRL `declare` blocks. Fields are structural — no per-state variation.

**Keywords/syntax:** `rule`, `when`, `then`, `end`, `declare` (for inline fact types), `modify`, `insert`, `retract`.

**Strengths for Precept:**
- Drools' `when`/`then` pattern is broadly analogous to Precept's guard/action model.
- `declare` keyword for inline type definitions is a precedent for declaration-site specification, though it operates at the type level, not state level.
- Production rule semantics (fact matching → action) validates the constraint-as-declaration approach.

**Weaknesses for Precept:**
- No per-state field scoping or access mode concept.
- Fact-based: objects are inserted/retracted from working memory, but individual fields don't gain/lose presence.
- Stateless execution model — rules fire on current working memory snapshot, not through a lifecycle progression.

---

## 9. Cedar (AWS) / Rego (OPA)

**Category:** Policy languages for authorization and data policy

### Cedar

**Approach:** Cedar evaluates `permit`/`forbid` policies against requests containing principal, action, resource, and context. Entity attributes are accessed via dot notation (`resource.owner`, `principal.department`). There is no per-state concept — Cedar evaluates policies against a point-in-time authorization request.

**Field access:** Attributes are accessed in `when`/`unless` conditions. If an attribute is missing from the entity, the policy doesn't match (safe failure). Cedar's schema system allows declaring expected entity attributes, but these are structural (per entity type), not state-dependent.

**Relevance to Precept:** Cedar's `when { resource.private }` condition-on-attribute-access is analogous to Precept guards. Schema-driven attribute typing is a validation precedent. But Cedar has no lifecycle, no state transitions, and no field access modes.

### Rego (OPA)

**Approach:** Rego is a declarative policy language that evaluates rules over structured data (JSON documents). References to undefined fields evaluate to `undefined`, which causes the containing expression to be undefined — a form of safe/silent failure.

**Field access:** Rego uses dot-access references into nested documents. Variables are existentially quantified. There's no per-state field concept — Rego operates on the current data snapshot.

**Relevance to Precept:** Rego's handling of `undefined` (expression silently fails rather than erroring) is a design counterpoint to Precept's approach. Precept explicitly declares absence (`define F absent`) and makes accessing an absent field a compile/runtime error. Rego's permissive undefined propagation avoids errors but creates debugging difficulty. This validates Precept's explicit-absence approach.

---

## Synthesis

### Pattern Taxonomy

The surveyed systems fall into four categories of how they handle per-state field properties:

| Pattern | Systems | Mechanism | Absence Model |
|---------|---------|-----------|---------------|
| **Structural per-state fields** | Plaid, Rust typestate | Different states literally have different field sets | Compile-time: field doesn't exist |
| **Type-level variants** | TypeScript discriminated unions | Each variant declares its own fields; narrowing gates access | Type-system enforcement after narrowing |
| **Constraint on global fields** | Alloy, TLA+, SCXML | All fields exist globally; per-state constraints limit values | Sentinel values or constrained-to-empty |
| **Unscoped (no per-state concept)** | XState v5, Drools, Cedar, Rego | Fields exist uniformly; state is orthogonal to field presence | Null/undefined by convention |

### Key Findings

1. **Structural absence is the strongest guarantee.** Plaid and Rust typestate are the only two systems that make field presence a structural, compile-time property of the current state. Both achieve the guarantee Precept is targeting — different states expose different fields. Both validate that the concept is implementable and valuable.

2. **TypeScript discriminated unions are the closest mainstream analogy.** The `NetworkState` pattern (Loading has no `code`, Failed has `code`, Success has `response`) is structurally identical to Precept's `in Failed define code editable, in Success define response readonly`. Massive adoption proves developers understand per-variant field presence.

3. **Global context is the common pain point.** XState v5, Drools, SCXML (despite syntactic support), and most operational systems use global field declarations with per-state constraints at best. This forces developers to mentally track which fields are meaningful in which states — exactly the burden Precept's `define` eliminates.

4. **No precedent system has all three modes (absent/readonly/editable).** The closest is Plaid's permission system, which distinguishes `immutable`, `unique`, `shared`, and `none` — but these are concurrent-access permissions, not lifecycle access modes. Precept's three-mode system is genuinely novel across the surveyed landscape.

5. **`readonly` is the least-precedented mode.** Per-state structural absence has strong precedents (Plaid, Rust, TypeScript). Per-state editability has weaker precedents (Plaid permissions, Rust `&self` vs `&mut self`). Per-state readonly as a declared mode has essentially no precedent — the closest analogy is TypeScript's `Readonly<T>` utility type, which is applied at the type level, not per-state.

### How Precept's `define` Approach Compares

Precept's `in <StateList> define <FieldList> <mode>` is positioned uniquely among the surveyed systems:

- **Declarative, not mechanical:** Unlike Rust typestate (which requires manual struct/impl boilerplate) or TypeScript (which requires separate interfaces per state), Precept makes per-state field access a single declarative line.
- **Runtime-enforced, not just compile-time:** Unlike Plaid/Rust/TypeScript (which enforce at compile time in the host language), Precept enforces at runtime through its engine — making it applicable to data-driven configurations, not just statically typed code.
- **Three-mode spectrum:** No surveyed system offers a declared choice of absent/readonly/editable per state. This is genuinely novel territory.
- **Lifecycle-aware:** Unlike policy languages (Cedar, Rego) that evaluate point-in-time snapshots, Precept's `define` operates within a state machine lifecycle where fields evolve through declared mode progressions.

The primary risk of the `define` approach — keyword cost (4 new keywords) and adjective-category novelty — finds no mitigating precedent in the survey. No surveyed DSL uses adjective keywords (`readonly`, `editable`, `absent`) for field mode declarations. This confirms the concern raised in the `define` syntax analysis: the adjective-category keywords are a grammatical novelty in a language that has only used verbs, nouns, prepositions, and adverbs.

---

## References

| System | Primary Source |
|--------|---------------|
| Alloy | alloytools.org; Jackson, *Software Abstractions* (MIT Press) |
| TLA+ | Lamport, *Specifying Systems* (Addison-Wesley); Wikipedia |
| SCXML | W3C TR/scxml (2015); statecharts.dev |
| Plaid | cs.cmu.edu/~aldrich/plaid/; Aldrich et al., "Typestate-Oriented Programming" (Onward! '09) |
| Rust typestate | Cliffle, "The Typestate Pattern in Rust" (2019); Rust Embedded Book |
| TypeScript | typescriptlang.org/docs/handbook/2/narrowing.html#discriminated-unions |
| XState v5 | stately.ai/docs/context; stately.ai/docs/states |
| Drools | Wikipedia; docs.drools.org |
| Cedar | docs.cedarpolicy.com/policies/syntax-policy.html |
| Rego | openpolicyagent.org/docs/latest/policy-language/ |
