# Precept Language Specification (v2)

---

## Status

| Property | Value |
|---|---|
| Doc maturity | Incremental — grows as each compiler stage is designed and implemented |
| Implementation state | §1 Lexer complete; §2 Parser complete; §3 Name Binding and Type Checker complete; §3A Language Semantics complete; §0 Preamble complete; §4 Graph Analyzer complete; §5 Proof Engine — base engine implemented; prove-or-reject MVP + fail-open soundness fixes designed, not yet implemented (see docs/compiler/proof-engine.md) |
| Grounding | `docs/archive/language-design/precept-language-vision.md` (archived v1 vision) |
| Clean room rule | References v1 grammar and keyword inventory; does not import v1 implementation details |

> [!IMPORTANT]
> **Non-Negotiable Rules — Read Before Implementing**
>
> - **Prevention, not detection** — invalid configurations must be structurally impossible before any change is committed, not merely caught afterward. See [§0.1 Design Principles](#01-design-principles).
> - **One file, complete rules** — the `.precept` definition is the whole contract, with no scattered enforcement across service layers, validators, or handlers. See [§0.1 Design Principles](#01-design-principles).
> - **Deterministic semantics** — the same definition and the same data must always produce the same outcome. See [§0.1 Design Principles](#01-design-principles).
> - **Mandatory rationale** — every rule and ensure must carry a syntactically required `because` clause; rationale is part of the language contract. See [§0.1 Design Principles](#01-design-principles).
> - **Full inspectability** — every possible action and its outcome must be previewable without executing the operation. See [§0.1 Design Principles](#01-design-principles).

## Contents

- [Tooling Surface Snapshot](#tooling-surface-snapshot)
- [0. Preamble](#0-preamble)
  - [0.1 Design Principles](#01-design-principles)
  - [0.2 Language Model](#02-language-model)
  - [0.3 Governance, Not Validation](#03-governance-not-validation)
  - [0.4 Execution Model Properties](#04-execution-model-properties)
  - [0.5 Graph Analyzer Design Contract](#05-graph-analyzer-design-contract)
  - [0.6 Proof Engine Design Contract](#06-proof-engine-design-contract)
  - [0.7 The Compile-Time and Runtime Guarantee Contract](#07-the-compile-time-and-runtime-guarantee-contract)
  - [0.8 Authoring Audience](#08-authoring-audience)
- [1. Lexer](#1-lexer)
  - [1.1 Token Vocabulary](#11-token-vocabulary)
  - [1.2 Reserved Keywords](#12-reserved-keywords)
  - [1.3 Literal Syntax](#13-literal-syntax)
  - [1.4 Comments and Whitespace](#14-comments-and-whitespace)
  - [1.5 Operator and Punctuation Scanning](#15-operator-and-punctuation-scanning)
  - [1.6 Dual-Use Token Disambiguation](#16-dual-use-token-disambiguation)
  - [1.8 Lexer Diagnostics](#18-lexer-diagnostics)
- [2. Parser](#2-parser)
  - [2.1 Expression Precedence](#21-expression-precedence)
  - [2.2 Declaration Grammar](#22-declaration-grammar)
  - [2.3 Type References](#23-type-references)
  - [2.4 Field Modifiers](#24-field-modifiers)
  - [2.5 Interpolation Reassembly](#25-interpolation-reassembly)
  - [2.6 Error Recovery](#26-error-recovery)
  - [2.7 Parser Diagnostics](#27-parser-diagnostics)
- [3. Name Binding and Type Checking](#3-name-binding-and-type-checking)
  - [3.1 Processing Model](#31-processing-model)
  - [3.2 Type Widening Rules](#32-type-widening-rules)
  - [3.3 Context-Sensitive Type Resolution](#33-context-sensitive-type-resolution)
  - [3.4 Name Resolution](#34-name-resolution)
  - [3.5 Scope Rules](#35-scope-rules)
  - [3.6 Expression Typing Rules](#36-expression-typing-rules)
  - [3.7 Built-in Function Catalog](#37-built-in-function-catalog)
  - [3.8 Semantic Checks](#38-semantic-checks)
  - [3.9 Error Recovery](#39-error-recovery)
  - [3.10 Diagnostic Catalog](#310-diagnostic-catalog)
- [3A. Language Semantics](#3a-language-semantics)
  - [3A.1 Constraint Semantics](#3a1-constraint-semantics)
  - [3A.2 Outcomes and Semantic Verdicts](#3a2-outcomes-and-semantic-verdicts)
  - [3A.3 Constraint Violation Subject Attribution](#3a3-constraint-violation-subject-attribution)
  - [3A.4 Mutation Atomicity](#3a4-mutation-atomicity)
  - [3A.5 Entity Construction](#3a5-entity-construction)
  - [3A.6 Inspection as a First-Class Operation](#3a6-inspection-as-a-first-class-operation)
- [4. Graph Analyzer](#4-graph-analyzer)
- [5. Proof Engine](#5-proof-engine)
- [Open Questions / Implementation Notes](#open-questions--implementation-notes)
- [Cross-References](#cross-references)

---

## Tooling Surface Snapshot

The shipped editor surface for `.precept` authoring currently includes diagnostics, semantic tokens, completions, hover, definition, references, document highlights, rename, signature help, document symbols, workspace symbols across open documents, folding ranges, selection ranges, and quick-fix code actions.

This tooling surface is intentionally authoring-focused. Preview / inspect remains a separate tooling track and is not part of the shipped language-server surface described here.

---

## 0. Preamble

This section captures the language's foundational identity: the design principles, conceptual model, governance philosophy, execution model properties, and pre-implementation contracts for pipeline stages not yet built. It is authoritative — any redesign that contradicts material here has departed from Precept's identity.

### 0.1 Design Principles

The language must preserve these principles. They are non-negotiable — any redesign that violates one has departed from Precept's identity.

1. **Prevention, not detection.** Invalid entity configurations — combinations of lifecycle position and field values that violate declared rules — cannot exist. They are structurally prevented before any change is committed, not caught after the fact. This is the defining commitment: the contract prevents them. A compiler that validates on request is doing detection; a compiler that makes invalid configurations structurally impossible is doing prevention.

2. **One file, complete rules.** Every field, rule, ensure, and transition lives in the `.precept` definition. There is no scattered logic across service layers, validators, event handlers, or ORM interceptors. All proof facts, all type information, all constraint scope, all routing — everything needed to understand and enforce the contract — derives from a single file. No imports, no cross-file references, no external rule injection, no ambient configuration.

3. **Deterministic semantics.** Same definition, same data, same outcome — always. No non-deterministic solvers, no timing-dependent analysis, no culture-dependent operations, no stochastic reasoning. This is what makes the engine trustworthy as a business rules host and auditable as an AI agent tool.

4. **Full inspectability.** The language must make the engine's reasoning fully exposable. At any point, you can preview every possible action and its outcome without executing anything. The engine exposes the complete reasoning: conditions evaluated, branches taken, constraints applied. You see not only what would happen, but why. Nothing is hidden. Inspectability extends to proof reasoning — proven ranges, source attribution, and what the engine could not prove must all be surfaceable through diagnostics, hover, and tooling.

5. **Keyword-anchored readability.** Structure is explicit and line-oriented, but "line-oriented" means declarations are anchored by keywords, not terminated by newlines. Statement kind is identified by its opening keyword sequence. A declaration may span multiple lines freely; whitespace — including newlines — is cosmetic within the declaration. Inside transitions, `from` starts a new transition row and each `->` starts the next pipeline step; those keywords are the structural boundaries, not layout. This keeps the language AI-safe for authoring, avoids layout traps during reformatting/copy-paste/code generation, keeps tooling boundary detection keyword-driven instead of layout-driven, and lets dense compound declarations such as `queue of T by choice of string(...) ordered` wrap across lines for human readability without changing meaning.

6. **Explicit domain meaning over primitive convenience.** When a value has real domain identity (money, date, quantity, currency), the language should name it as a distinct type with its own operator rules and compile-time enforcement. Primitive types are the storage mechanism; domain types are the meaning mechanism.

7. **Compile-time-first static checking.** The compiler proves what it can, rejects what it can prove invalid, and does not guess. Compile-time structural checking catches unreachable states, type mismatches, constraint contradictions, division by zero, overflow, empty collection access, and more — before runtime, before any entity instance exists. A precept that compiles without diagnostics has no unproven evaluation faults. **Overflow is on that list as the target and is not implemented yet**: the arithmetic overflow model is in scope, and its build is deferred by ordering rather than dropped, so nothing proves the absence of overflow today; see §0.7. This is where the "contract" metaphor becomes literal: the compile step validates the definition's structural soundness before any instance exists.

8. **Approximation honesty.** The language does not present approximation as exactness. If a value or operation is inherently approximate, that fact must be explicit in the contract. Exact-value lanes remain exact. Silent approximation inside an exact-looking path weakens the user's ability to reason about outcomes. The line between exact and approximate behavior must be visible in the type system and the language surface.

9. **Mandatory rationale.** Every constraint carries a mandatory reason. The engine requires not just the rule, but its rationale. This is a language requirement, not a convention — the `because` clause is syntactically required on every rule and ensure. Constraint modifiers (§2.4) are rule shorthand and carry a *generated* rationale, which satisfies this requirement without an authored clause; an author who wants a specific reason writes the constraint as a full `rule … because "…"`.

10. **Totality.** Every expression evaluates to a result — never silent `NaN`, `Infinity`, or `null`. The evaluation surface has no undefined behavior. For any expression that *could* fault at runtime — division by zero, overflow, empty collection access — the compiler must either prove safety or emit a diagnostic requiring the author to supply constraints that make safety provable. A precept that compiles without diagnostics has no unproven arithmetic or access faults. There are no runtime fault traps: the evaluator computes and does not re-test what the compiler proved. What makes that safe is the proof certificate the runtime verifies once, when it loads the definition — not a redundant check at evaluation time. **Overflow is on that list as the target and is not implemented yet**: the arithmetic overflow model is in scope, and its build is deferred by ordering rather than dropped, so nothing proves the absence of overflow today; see §0.7.

11. **Static completeness.** If a precept compiles without diagnostics, it does not fault at runtime. The compiler catches all type errors, proves all arithmetic safety obligations, and verifies all access preconditions at compile time. Every fault class the language admits — type mismatch, division by zero, overflow, empty collection access — is linked to a compiler diagnostic that prevents it. A value straying outside a declared bound is not on that list: a bound is rule shorthand (§2.4), so keeping a value inside it is rule preservation, proven and refused exactly as any other rule is. Runtime fault checks do not exist — the evaluator computes, and what gives confidence that it may is the proof certificate the runtime verifies once at load, not a second check at evaluation time. **Overflow is on that list as the target and is not implemented yet**: the arithmetic overflow model is in scope, and its build is deferred by ordering rather than dropped, so nothing proves the absence of overflow today; see §0.7. This is the bridge between the compiler and the evaluator: the compiler's job is to leave the evaluator no error path to take. (How compile-time fault prevention and runtime governance compose to deliver this without deferral: §0.7.)

### 0.2 Language Model

A precept defines a governed business entity.

The entity may be:

1. Stateful: lifecycle positions, transitions, state-scoped rules, and event routing.
2. Stateless: data integrity, editability, and event-driven mutation without a state machine.

The governing concepts are:

1. Fields: stored or computed data.
2. Rules: global data truth.
3. Ensures: contextual truth tied to state or event context.
4. Events: typed triggers.
5. Actions: mutations.
6. Transitions: movement truth.
7. Editability: direct mutation permissions.
8. Modifiers: declaration-attached structural, semantic, or severity intent.

The language protects configurations, not isolated values. In a stateful precept, a configuration is current state plus current field data. In a stateless precept, it is the current field data alone.

### 0.3 Governance, Not Validation

This distinction is fundamental to the language's identity and governs how every constraint surface works.

**Validation** checks data at a moment in time, when called. A validator runs when you invoke it. Code paths that don't call the validator bypass the rules. There is always a window where invalid state can exist.

**Governance** declares what the data is allowed to become and enforces that declaration structurally, on every operation, with no code path that bypasses the contract. The language makes certain configurations structurally impossible — not checked, not caught, but prevented.

This distinction shapes the language at every level:

1. Rules are not assertions called at a checkpoint. They are declarations that hold structurally on every operation where they apply.
2. Guarded rules do not weaken the guarantee. They make it precise — the rule applies exactly where the domain says it should, and the engine ensures it cannot be bypassed.
3. State is not a passive label. It is an active rule-activator. An entity in `Approved` has different data requirements than the same entity in `Draft` — the state defines what must be true about the data there.
4. Mutations are atomic. All mutations execute on a working copy. If the operation runs to completion, the working copy is promoted; if the operation refuses part-way — an authored `reject` row matched, an incoming value failed its ingress check, a patch failed its access-mode check — the working copy is discarded whole. Promotion is not conditional on a constraint check: every handler was proven at compile time to preserve every rule, so a completed working copy is valid by construction. There is no window where a partially-committed mutation with a violated rule exists.
5. An invalid definition cannot produce an engine. The compile-time gate is a structural boundary — not a convention, not a best practice. If the definition has errors, no engine exists to run.

The full guarantee is about configurations: the pair of (current lifecycle position, current field values) for stateful entities, or simply current field values for stateless entities. Invalid configurations are structurally impossible. A valid entity is simply one where every constraint holds for its current configuration.

### 0.4 Execution Model Properties

The following properties of Precept's execution model are language design choices, not implementation accidents. They are what make tractable compile-time reasoning possible, and any future compiler must preserve them.

1. **No loops.** The language has no iteration constructs. Expression trees are finite and acyclic. This eliminates the need for fixpoint computation and widening operators.

   *Bounded quantifier predicates (`each`, `any`, `no`) are not iteration constructs.* A quantifier asserts a truth value over every, some, or no element in a statically-declared finite collection — producing a single boolean value, introducing no loop variable that carries state across elements, mutating nothing. Structurally, a quantifier over a finite collection unfolds to a finite conjunction or disjunction of its predicate: an acyclic expression tree, not a control-flow loop. The prohibition is on general iteration, recursion, and any construct that requires fixpoint reasoning to analyze. Quantifiers that assert bounded truths over finite domains without mutation require no fixpoint reasoning and are not in that category.

2. **No control-flow branches.** A transition row is a flat sequence: evaluate a guard, then execute assignments left-to-right. There are no `if` statements that split execution into paths that later reconverge. Conditional *expressions* (`if/then/else`) produce a single value — both branches are type-checked, exactly one is evaluated — but they do not create control-flow divergence.

3. **No reconverging flow.** Because there are no loops or branches, there is no join point where two different states must be merged. Each assignment in a row sees the state left by all preceding assignments. This makes sequential flow analysis a linear walk, not a dataflow graph.

4. **Closed type vocabulary.** The language has a fixed set of types. No user-defined types, no parametric polymorphism, no open type hierarchies. This makes exhaustive type checking possible over a finite, fully-known vocabulary.

5. **Finite state space.** States, events, fields, and rules are declared statically. Every transition row, state action, and rule can be enumerated exhaustively. No symbolic execution over unbounded domains is needed.

6. **Expression purity.** Expressions cannot mutate entity state, trigger side effects, or observe anything outside their evaluation context (current field values and event arguments). This is a language semantic, not a caller convention. It is what makes inspection safe — you can always ask what an expression would produce without affecting anything.

7. **No separate compilation.** Each `.precept` file is self-contained. No imports, no cross-file references. The compiler processes the entire definition in one pass. All proof facts derive from the single file.

> **Draft 2026-11-25** — under adversarial review; nothing may cite this section until the mark is removed.

Properties 2 and 3 describe a row's own action chain. The one construct that makes an operation run along more than one path is a guarded state action, which runs only when its guard holds at its turn; each path is still a flat sequence, and the compiler follows each path separately rather than merging them (§3A.4, *Known exceptions*, item 13).

These properties are the reason the language can support tractable compile-time proofs. Standard interval arithmetic, bounded relational closure, and single-pass validation are directly applicable without the lattice infrastructure that general-purpose analyzers require. The absence of widening is a feature: in general-purpose analyzers, widening is the primary source of precision loss.

### 0.5 Graph Analyzer Design Contract

> **Design contract.** This section captures the language's shipped graph-analyzer guarantees. §4 is the implementation. Capabilities listed here are the contract the implementation satisfies; capabilities planned for a future graph-analyzer expansion are documented separately in [`graph-analyzer-roadmap.md`](graph-analyzer-roadmap.md).

The compiler must build and reason over the full state transition graph at compile time. The graph is constructed from declared states, events, and transition rows. This is a first-class language requirement, not an optional optimization.

The shipped graph-analysis surface supports these reasoning capabilities:

1. **BFS/DFS reachability from initial.** Detects unreachable states (PRE-coded diagnostic) and defines the reachable state set that other modifiers reason over. `initial` provides the root.
2. **Terminal state identification.** States with no outgoing transition rows. Anchors path analysis and validates `terminal` modifier declarations.
3. **Dead-end state detection.** Non-terminal states where all outgoing rows reject or produce no-transition — transition machinery that never succeeds. Reported as a structural diagnostic.
4. **Dominator analysis.** Required for the `required` modifier — the author asserts that all initial→terminal paths must visit this state. Dominator analysis (O(V+E) via Lengauer-Tarjan) confirms or refutes that claim.
5. **Reverse-reachability.** Required for `irreversible` — there is no path from this state back to any ancestor state in the initial→forward ordering.
6. **Outcome-classification diagnostics.** Per (state, event) pair the analyzer recognizes reject-only pairs and events whose every row is reject/no-transition, surfacing the relevant structural diagnostics.

**Overapproximation rule.** Structural graph analysis treats all edges as traversable regardless of `when` guards — it overapproximates reachability. This is sound: structural guarantees cannot account for guard-dependent path selection because guard satisfaction depends on runtime data. A modifier that claims "all paths visit this state" means all *structurally declared* paths, not all guard-satisfiable paths. This is the correct tradeoff for compile-time analysis.

**Forward-looking graph-analysis work.** Additional modifier-driven analyses — `guarded`, `entry`, `isolated`, `universal`, `sealed after`, `writeonce`, `advancing`, `settling`, `completing`, `absorbing` — are deferred to a future graph-analyzer roadmap. They are pure compile-time graph properties (no runtime support required) and can ship additively after the runtime gate opens. See [`graph-analyzer-roadmap.md`](graph-analyzer-roadmap.md) for the deferred set and the analyses each one requires.

### 0.6 Proof Engine Design Contract

> **Design contract.** This section captures the language's requirements for the proof engine. §5 is implemented. These requirements define the contract the implementation satisfies.

#### Proof-system responsibilities

The proof layer must be able to support the language's proof-bearing claims. That includes:

1. **Numeric interval reasoning.** Field constraints, rules, and guards contribute provable numeric ranges. The proof system tracks these ranges through assignment chains.
2. **Relational reasoning** over numeric expressions involving multiple fields.
3. **Divisor safety.** Two-tier: proven-zero divisors are hard errors; divisors with no compile-time nonzero proof are obligation diagnostics requiring the author to supply a constraint (e.g., `nonzero`, `positive`, a rule, or a guard).
4. **Non-negative proof obligations** such as `sqrt` inputs and `pow(integer, integer)` exponents. The compiler requires a provable non-negative path — via `nonnegative` constraint, a rule, an ensure, or a guard — before accepting the expression.
5. **Unit-aware numeric interval reasoning.** When field bounds and assignment expressions use different **statically convertible** units within the same physical dimension (e.g., `max '5 kg'` with `set x = '6 [lb_av]'`), the proof engine normalizes magnitudes to base-unit equivalents via UCUM scale factors before interval containment comparison. Normalization is applied once at the TypeChecker extraction boundary; downstream consumers read pre-normalized values. Explicit counting-unit mismatches (`each` vs `box`) are not covered by this guarantee — they require matching qualifiers or a separate runtime conversion field (see §6.7).
6. **Assignment range impossibility.** An assignment expression provably outside the target field's constraint range is a compile-time error.
7. **Contradictory / unsatisfiable rule detection.** When a single rule's range is provably empty under the field's declared bounds (no value can satisfy it), or two rules' ranges are provably incompatible (no value can satisfy both simultaneously), the definition is **uninhabitable** — no valid entity can exist — and the compiler **rejects the definition**. An uninhabitable rule set is a prevention-worthy defect, not hygiene: a definition whose own rules admit no satisfying configuration builds an engine that can govern nothing. (Severity model and the scope of this rejection — which structural diagnostics block versus report — are in [`../compiler/diagnostic-system.md`](../compiler/diagnostic-system.md).)
8. **Vacuous rule detection.** When a rule is provably always true given field constraints, the compiler reports it as tautological. This stays a report, not a rejection: an always-true rule is redundant, but the entity remains fully inhabitable (every value satisfies it), so it is inert hygiene rather than a prevention defect.
9. **Dead guard detection.** A guard provably always false means the row or block can never execute.
10. **Tautological guard detection.** A guard provably always true means the `when` clause has no effect.
11. **Compile-time rule enforcement against defaults.** Rules and initial-state ensures are checked against default field values at compile time. A definition where default values violate a declared rule is rejected before any instance exists. Both halves are implemented: initial-state ensures fold against defaults (`UnsatisfiableInitialState`), and unguarded global rules fold against defaults (`DefaultViolatesRule`), sharing one default environment and one constant-fold evaluator. A provably-false fold rejects. So does an unknown or unfoldable one, on the same three-way verdict the bounded-write obligations carry (Proof philosophy #2 and #5): a default that forms part of the initial configuration and cannot be shown to satisfy the rules leaves the base case of the induction open, and prevention does not admit a reachable configuration with no authored disposition. The one exception is designed: a default the construction event provably overwrites is a placeholder that never reaches a committed configuration, so no rule has to hold of it.
12. **Sharpening of reachability and routing diagnostics** from proven-dead guards.
13. **Structured proof attribution** suitable for hover, diagnostics, and agent consumption — every proven range carries the constraints and rules that contributed to it.

#### Proof philosophy

The proof layer is governed by these requirements, which are language-level commitments, not implementation preferences:

1. **Soundness over completeness.** The proof layer must never claim an expression is safe when it is not. Every proof path must return a provably correct result or conservatively decline. False negatives (missed proofs) cause author friction — the author must supply additional constraints. False positives (wrong "safe" claims) cause runtime failures. The language always chooses the safe direction.

2. **Proven violations only — for violation *reports*.** This scope covers violation **reports** — dead rows, contradictory rules, unreachable states. There, the language reports what is definitively broken, not what might be broken: flagging possible violations turns the compiler into a nag that trains authors to ignore warnings, while flagging only proven violations makes it a trusted guide. **Bounded-write checks are different — they are containment *obligations*, not reports**, and carry a three-way verdict: **proven** (the write provably stays inside its declared bound), **proven-violating** (the write provably breaks its bound — rejected, with a concrete counterexample), or **unresolved** (the compiler can neither prove nor disprove containment — rejected, with the weakest precondition that would discharge it printed verbatim). Both non-proven verdicts **block**: under prevention (Principle 1), a reachable write the definition left with no authored disposition is a definition defect, not a "might be broken" warning.

3. **Legible certificates, not opaque solvers.** The language's proof reasoning must be legible — to authors, to tooling, and to AI agents. Proof counterexamples must be structured data, not opaque solver traces. If the compiler cannot prove safety, it says so explicitly — the author is never confronted with an unexplainable verdict. A proof strategy is admissible **if and only if** it satisfies all four criteria: (1) **legible and independently re-checkable** — it emits a certificate drawn from a small, spec-enumerated vocabulary (the `CertificateSteps` catalog) that an independent checker can replay and the author can read; (2) **performant** — it does not break single-file recompile on every keystroke; (3) **right-sized for Precept** — its complexity is matched to Precept's small, few-variable, loop-free problems, and where a procedure could blow up it hits a hard cap and falls back to an explicit "couldn't prove — here is what would," never searching unboundedly; (4) **justified** — each strategy earns its place with evidence of real value to Precept (added expressibility for the domains that matter, better authoring quality, or genuine prevention of runtime faults), never added speculatively. The *search* procedure is unconstrained — a strategy may use any method to find a proof — but authority rests in the certificate, not the search. This criterion replaces an earlier outright ban on solver-style engines: legibility is the actual commitment, and a replayable certificate from a spec-owned vocabulary satisfies it more directly than banning a class of tool.

4. **One file, complete proof facts.** All proof facts derive from the `.precept` definition. No external oracle, no hidden configuration, no side channel. The proof engine's knowledge boundary is the file boundary.

5. **Truth-based diagnostic classification.** Proof outcomes are classified into three categories: *proven-violating* (the compiler can demonstrate a violation), *proven safe* (the compiler can demonstrate correctness), and *unresolved* (the compiler can prove neither). These map to distinct author-facing outcomes: a proven-violating write is **rejected** with a counterexample; a proven-safe write is discharged; an **unresolved** write is also **rejected** — not because it "might be broken," but because the definition left a reachable case with no authored disposition, which prevention (Principle 1) does not admit. The author resolves an unresolved verdict by supplying the constraint its printed weakest precondition names. Diagnostics are classified by proof outcome, not by syntax shape.

6. **Proof attribution is required, not optional.** Every proven range must carry its source attribution — the field constraints, rules, and guards that contributed. Authors must see what the engine proved, what it could not prove, and why. Proof results flow as structured data, not parsed prose — tooling and agents consume the proof model directly, never by parsing diagnostic message text.

7. **Sequential proof flow.** Actions in a chain are sequenced — each subsequent action sees the proof state left by all preceding actions. When a field is reassigned, prior proof facts about that field are invalidated before the new assignment's facts are stored. This is a language semantic that ensures proof reasoning tracks the actual mutation sequence.

#### Implementation status

The proof-engine obligations enumerated above describe the full language-level contract. The proof engine's satisfiability scan now emits the rule- and guard-level obligations that a prior version of this table marked unbuilt — see [`diagnostic-system.md`](../compiler/diagnostic-system.md) and [`proof-engine.md`](../compiler/proof-engine.md):

| # | Obligation | Status |
|---|---|---|
| 7 | Contradictory rule detection (`ContradictoryRule` PRE0155, `UnsatisfiableRule` PRE0159) | Implemented — emitting |
| 8 | Vacuous rule detection (`VacuousRule` PRE0154) | Implemented — emitting |
| 9 | Dead guard detection (`UnsatisfiableGuard` PRE0082) | Implemented — emitting |
| 10 | Tautological guard detection (`TautologicalGuard` PRE0153) | Implemented — emitting |
| 12 | Sharpened reachability/routing diagnostics from proven-dead guards | Specification-only — depends on #9 + #10 |

All other obligations (numeric intervals, relational reasoning, divisor safety, non-negative obligations, unit-aware comparison, assignment-range impossibility, default-violation enforcement, proof attribution) are implemented and exercised by the proof-engine test suite. Obligation 12 (sharpened reachability/routing from proven-dead guards) is the one remaining specification-only item; an author who depends on it today supplies the equivalent hand-written constraint (an explicit `rule` or `ensure`) until it ships.

**Assignment-range impossibility (item 6) — string-length dimension.** Length containment for `minlength`/`maxlength`-bounded string fields now covers **non-literal** assignment RHS, not only string literals. The proof engine computes a static string-length interval for the assigned expression (literal, field/arg reference, concatenation, conditional, interpolation, member access, and the length-stable string functions) and discharges the obligation only when that interval is provably within the declared bounds; an unbounded source flowing into a capped field is a genuine gap that the author closes by declaring the matching bound on the source (§0.7 Composition). A collection's string inner type may carry the same `minlength`/`maxlength` bound (`queue of string maxlength 200` — §2.3): an element read via a collection accessor (`.peek`/`.first`/`.last`) then carries that bound and proves into a capped destination, and an element write-site must prove its source within the bound.

**Sequential proof flow (item 7) — implemented (corrected 2026-05-28, completed 2026-05-29).** A prior version of this table claimed item 7 was implemented; a probe disproved it (a guard fact survived a reassignment, proving a divide-by-zero "safe"). It is now genuinely implemented across both clauses, classified catalog-side by `ActionMeta.Effect` (`ActionEffectClass`):

- **Reassignment-invalidation** — full-value-replacement actions (`set`/`clear`) drop *every* prior guard fact about the field for obligations after the write, across all guard-narrowing strategies (guard-in-path numeric + presence, index-bounds, key-presence, field-to-field). `when X != 0 → set X = 0 → 100 / X` no longer over-proves.
- **Forward-propagation** — in-place collection grow/shrink adjusts the non-empty (`count > 0`) fact: a grow (`add`/`append`/`insert`/`enqueue`/`push`/`put`) establishes `count > 0` for a following `dequeue`/`pop`/etc. without an author guard (closing a false-positive); a shrink (`remove`/`dequeue`/`pop`/…) invalidates a pre-shrink `count > 0` guard, so `when Q.count > 0 → dequeue Q → dequeue Q` no longer over-proves the second dequeue.

**Relational reasoning (item 2) and divisor safety (item 3) — rule-sourced relations live.** A declared **unconditional** field-to-field rule (`rule X >= Y because "…"` and its `>`/`<=`/`<` siblings) now participates in proof exactly as a same-row `when X >= Y` guard does: it contributes a provable bound to the subject's interval and discharges a downstream fault-prone obligation. A divisor over a *subtraction* of two related fields — `Z / (X − Y)` from `rule X > Y` — is provably non-zero (`X > Y ⟺ X − Y > 0`, no knowledge of either operand's sign required) and discharges; the equivalent bare-field divisor `Z / X` from `X > Y` stays an obligation (the relation alone does not prove `X != 0`). A *guarded* relational rule contributes no unconditional fact (it discharges only within its own guard scope), and a rule whose relation contradicts the subject's declared bounds contributes no fact at all — the dependent fault-prone operation then rejects, never compiles a false "safe." The mechanism is single-pass and depth-bounded (no transitive chasing of a third field), per §0.4. This single-hop bound is deliberately retained: the engine never combines two separately-declared facts to derive a bound neither states alone (multi-hop reasoning). An author who needs a combined bound declares its consequence as one rule, which then participates in proof directly (the relational-reasoning mechanism this paragraph describes, item 2) — the honest, legible fix, in place of the engine chasing a chain.

Both directions are *sound by construction* (invalidation paths are pure rejections; grow-establishment proves only `count > 0`, which one added element guarantees). Alongside this boolean non-empty model, the same sequential pass tracks a numeric **count interval** `[lo, hi]` per `mincount`/`maxcount`-bounded field (seeded from the governed band, advanced by each mutation's sound per-kind/per-action delta) so collection cardinality bounds are *carrier-discharged obligations* — an unprovable grow/shrink emits `CountBoundViolation` (PRE0136) and names a `when C.count …` guard (§0.7: a `mincount`/`maxcount` bound is rule shorthand, so keeping the collection inside it is rule preservation — discharged by an author guard, never deferred to a runtime check). This closes the band incompleteness for the *cardinality* axis (`add x; add x` into `maxcount 1` now rejects). The boolean model remains the mechanism for *accessor-safety* (`dequeue`/`pop` non-empty preconditions), a distinct obligation; its `count >= 2 → dequeue → dequeue` boolean-vs-interval incompleteness for accessor safety, and membership-across-shrink, are the remaining future refinements (sound, conservative), not soundness gaps.

---

### 0.7 The Compile-Time and Runtime Guarantee Contract

Precept's guarantees are delivered by two distinct mechanisms; keeping them distinct is what makes the guarantee precise rather than magical.

**Fault prevention — established entirely at compile time.** A precept that compiles without diagnostics cannot produce a runtime fault: no division by zero, no overflow, no empty-collection access. A value coming to rest outside a declared bound is deliberately not on that list. A bound is rule shorthand (§2.4), so containment within it is rule preservation under the same single constraint mechanism as any other rule — the disposition is identical (prove it or reject the definition), but it is not a fault gate and should not be quoted as one.

The compiler delivers both by proof over the reachable configurations, from four classes of premise the author declared: field modifiers, constraints on event arguments, the handler's guard, and the rules that already hold in the pre-state. The base case is that the default configuration and every initial event — together with the initial state's entry actions, which construction runs (§3A.5) — establish every rule; the inductive step is that every handler preserves every rule. Where a proof cannot close, the compiler **rejects the definition** and names the premise that would close it. It never compiles a fault-prone operation in the hope a runtime check catches it; there is no deferral. (Principles 7, 10, 11.)

**Non-linear rules are proven from a premise that states the condition directly, or the definition is rejected.** Substitution closes a linear rule quietly — a guard such as `Balance - Amount >= -OverdraftLimit` is literally the post-state condition, so the proof falls out. A rule whose post-state condition is *non-linear* — a product or quotient of two fields, such as `MonthlyRepayment <= OverdraftLimit * RepaymentCapPercent` — has no such cancellation available: a write that sets one operand to a quotient and is checked against a product leaves four free variables and nothing to eliminate. Such a rule is proven only when premise class (c), the handler's guard, or class (b), a constraint on an event argument, states the post-state condition directly, so the proof closes against what the author wrote. Where no such premise exists the compiler **rejects the handler** and names the guard that would close it — "cannot prove `MonthlyRepayment <= OverdraftLimit * RepaymentCapPercent`; add a `when` guard restating the post-state condition". This holds at every write site of every field the rule mentions, so lowering `OverdraftLimit` inherits the same obligation from the other side even though `MonthlyRepayment` is untouched. It is never governed by a pass over the finished configuration, and never deferred to a solver the compiler does not carry. The rule is stated separately here only because the linear cases close so quietly that the harder ones could look as though they come along for free; they do not.

**Overflow is where this does not hold today, and saying so is the point of this paragraph.** The arithmetic overflow model — integer, temporal, and number-lane infinity — is in scope and not yet implemented. By an explicit ruling its build is sequenced after the MVP, the first working compiler, which is an internal milestone and not something anybody receives, so this is an ordering decision about when the work happens and not a gap the product is meant to carry (`docs/compiler/soundness-and-coverage.md` defines the milestone at its head and records the sequencing in §3.1 and §6). Until it is built the compiler does not prove the absence of overflow, and the runtime carries no guard against it, so a definition that compiles clean can still overflow when an expression is evaluated. The paragraph above states the target the language is being built toward and that target is not being weakened to match; this note records the one place the target is not yet met, so no reader takes the list above as a description of current behaviour.

**Governance — enforced at runtime on external input.** Every value entering the entity from outside the definition — event arguments, construction inputs, direct field edits — is held to the constraints declared on the slot it enters, at the moment it enters, before any computation derives from it. This enforcement is operation-blind: it upholds the field's declared contract whether or not any fault-prone operation references the field. An invalid configuration never persists because every handler was proven at compile time to preserve every rule and ingress makes the premises those proofs rest on true of the values that arrive — this is prevention, not detection. (Principles 1, 6.)

**Composition.** For a value supplied at runtime, the compiler's proof that the operation cannot fault rests on a premise the author declared — a modifier on the field or the argument, the handler's guard, or a rule that already held; governance makes that premise true of the value at ingress. The proof is therefore complete at compile time and the fault never occurs — proven by the compiler, its precondition discharged by governance, nothing left to a runtime check.

**The one precondition of the guarantee.** Every value in a committed configuration entered through an operation the compiler proved, which is why restoring one needs no re-checking: hydration is fast and does not re-validate, exactly as a direct database read trusts the rows it loads. The guarantee therefore rests on one condition — **data must not be tampered with between the moment it is persisted and the moment it is restored.** A configuration changed by any route other than a declared operation is no longer the configuration that was proven, and nothing the compiler established about it still applies. Nothing in the engine detects that: state is reconstituted, not re-governed on load, no later operation re-governs it — there is no post-mutation pass that would — and there are no runtime fault traps waiting to catch such a value at evaluation time. This is a condition on how the data is kept, stated so that it is met deliberately rather than assumed quietly.

**Where that condition shows up in the proof, and it is worth naming rather than leaving implicit.** Every proof that leans on "this rule already held before the operation" is *proven* for data that arrived through a declared operation and *assumed* for data that did not. The precondition above is exactly what makes the assumption safe: a configuration kept intact between persisting and restoring is one every proof about it still describes.

Mechanism — how the pipeline discharges these obligations and where governance runs — is in `compiler-and-runtime-design.md`.

---

### 0.8 Authoring Audience

The primary author of a `.precept` definition is the **domain expert** — a business analyst, product owner, regulatory specialist, or subject-matter expert who reasons in terms of "what is this data allowed to become." Not the software developer.

This is the most distinguishing positioning claim Precept makes (see [`philosophy.md § Who authors a precept`](../philosophy.md)). It constrains every language-surface decision in this spec. The constraints below are not aspirational — they are *forcing functions* on what the language is allowed to look like.

**Operational implications:**

1. **Keyword-anchored grammar.** Every construct begins with a known keyword (`field`, `state`, `event`, `rule`, `ensure`, `from`, `on`, `transition`, `set`, `assign`). The parser dispatches on leading tokens; the author reads top-to-bottom in domain vocabulary. See Principle 5 in [§ 0.1](#01-design-principles) and the `Constructs.ByLeadingToken` catalog. Symbol-heavy or expression-first grammars are deliberate exclusions — they optimize for developer terseness over domain-expert readability.

2. **Mandatory `because` clauses.** Every `rule` and `ensure` carries a domain-readable rationale (Principle 9). The clause is not documentation; it is part of the construct. A rule without a `because` is rejected by the parser. The domain expert reads the rationale; the developer would have skipped writing it.

3. **Readable diagnostics.** Compile-time error messages target domain vocabulary, not compiler internals. A message that says "monthly price cannot be negative" speaks to the author; a message that says "type mismatch: expected `decimal{nonnegative}`, got `decimal`" does not. The diagnostic catalog ([`diagnostic-system.md`](../compiler/diagnostic-system.md)) enforces audience-targeted wording. Hover text in the language server follows the same constraint.

4. **No opaque proof.** The proof engine's obligations and discharges are inspectable — `precept_proofs` MCP tool surfaces them; LS hover surfaces them. The domain expert can ask "what did the compiler prove about this rule and how?" and get a domain-readable answer. SMT-style or theorem-prover-style proof artifacts (which would be opaque to the audience) are excluded.

5. **No iteration constructs.** Loops, comprehensions, recursive function definitions are absent. The domain expert reasons declaratively — what *is* true, what *must* be true. Iteration is procedural reasoning; it belongs in the hosting layer, not the precept.

**The audience constraint is reviewable.** When a new language-surface change is proposed, the design must demonstrate it serves the domain expert — not merely "is technically expressive." Designs that fail this check are CONCERNs at minimum; designs that assume a developer-tier reader without acknowledging the tradeoff are BLOCKERs (per [`precept-reviewer.md`](../../.claude/agents/precept-reviewer.md)).

**Reading by audience tier:**

- **Domain expert / business analyst**: this spec, the type docs ([`primitive-types.md`](primitive-types.md), [`temporal-type-system.md`](temporal-type-system.md), [`business-domain-types.md`](business-domain-types.md), [`collection-types.md`](collection-types.md)), the samples in [`samples/`](../../samples/).
- **Developer integrating Precept**: this spec plus [`runtime/runtime-api.md`](../runtime/runtime-api.md) and [`tooling/`](../tooling/README.md).
- **Compiler / runtime author**: this spec plus [`compiler-and-runtime-design.md`](../compiler-and-runtime-design.md), the per-stage docs in [`compiler/`](../compiler/README.md), and [`catalog-system.md`](catalog-system.md).

The spec is read by all three. The audience commitment ensures that "domain expert" remains a real reader of this document, not a category referenced only in philosophy.md.

---

## 1. Lexer

The lexer transforms source text into a flat token stream. It produces `TokenStream` containing an `ImmutableArray<Token>` and an `ImmutableArray<Diagnostic>` for lexer-level errors (unterminated strings, unrecognized characters).

The lexer enforces a hard source-size ceiling of **65,536 characters (64 KB)** as a **security guardrail**. The limit exists to bound lexer work and memory usage on adversarial input; it is not a language expressiveness rule.

### 1.1 Token Vocabulary

Every token the lexer can produce. Organized by category to match the `TokenKind` enum.

#### Keywords: Declaration

| Token | Text | Context |
|-------|------|---------|
| `Precept` | `precept` | Precept header declaration |
| `Field` | `field` | Field declaration |
| `State` | `state` | State declaration |
| `Event` | `event` | Event declaration |
| `Rule` | `rule` | Named rule / invariant declaration |
| `Ensure` | `ensure` | State/event assertion keyword |
| `As` | `as` | Type annotation (`field X as number`) |
| `Default` | `default` | Default value modifier |
| `Optional` | `optional` | Field optionality modifier (v2) |
| (retired) | (retired) | The `writable` field-level modifier was retired in favor of the unified `editable` access-mode adjective. The `Editable` token now stands at both field-declaration and per-state-modify positions. |
| `Because` | `because` | Reason clause |
| `Initial` | `initial` | Initial state marker |
| `Ascending` | `ascending` | Sort direction modifier — ascending order for `queue of T by P` and `log of T by P` (v3) |
| `Descending` | `descending` | Sort direction modifier — descending order for `queue of T by P` (v3) |

#### Keywords: Prepositions

| Token | Text | Context |
|-------|------|---------|
| `In` | `in` | State-scoped scope preposition (`in State ensure ...`, `in State modify|omit ...`) |
| `To` | `to` | Entry-gate ensure (`to State ensure ...`) |
| `From` | `from` | Exit-gate ensure or transition source (`from State ...`) |
| `On` | `on` | Event trigger (`on Event ensure ...`, `from State on Event ...`) |
| `Of` | `of` | Collection inner type (`set of string`) |
| `Into` | `into` | Dequeue/pop target (`dequeue Queue into Field`) |
| `By` | `by` | Ordering key preposition — `log of T by P`, `queue of T by P`, `append ... by P`, `enqueue ... by P` (v3) |
| `At` | `at` | Index position preposition — `insert F Expr at N`, `remove F at N` (v3) |
| `For` | `for` | Lookup key access infix operator — `F for K` (v3) |

#### Keywords: Control

| Token | Text | Context |
|-------|------|---------|
| `When` | `when` | Guard clause |
| `If` | `if` | Conditional expression |
| `Then` | `then` | Conditional expression |
| `Else` | `else` | Conditional expression |

#### Keywords: Actions

| Token | Text | Context |
|-------|------|---------|
| `Set` | `set` | Field assignment action keyword; in type position the parser reinterprets the surface word as `SetType` |
| `Add` | `add` | Set add action |
| `Remove` | `remove` | Set remove action |
| `Enqueue` | `enqueue` | Queue enqueue action |
| `Dequeue` | `dequeue` | Queue dequeue action |
| `Push` | `push` | Stack push action |
| `Pop` | `pop` | Stack pop action |
| `Clear` | `clear` | Collection clear action |
| `Append` | `append` | Log/list append action (v3) |
| `Insert` | `insert` | List indexed insert action (v3) |
| `Put` | `put` | Lookup upsert action (v3) |

#### Keywords: Outcomes

| Token | Text | Context |
|-------|------|---------|
| `Transition` | `transition` | State transition outcome |
| `No` | `no` | Prefix for `no transition` |
| `Reject` | `reject` | Rejection outcome |

#### Keywords: Access Modes (v2 → v3)

| Token | Text | Context |
|-------|------|---------|
| `Modify` | `modify` | State-scoped access mode verb (`in <State> [when <Guard>] modify <Field> readonly\|editable …`) |
| `Readonly` | `readonly` | Access mode adjective — constrain to read-only (`in <State> [when <Guard>] modify <Field> readonly …`) |
| `Editable` | `editable` | Access mode adjective — declare editable (`in <State> [when <Guard>] modify <Field> editable …`) |
| `Omit` | `omit` | State-scoped structural exclusion (`in <State> omit …`) |

#### Keywords: Logical Operators

| Token | Text | Context |
|-------|------|---------|
| `And` | `and` | Logical conjunction |
| `Or` | `or` | Logical disjunction |
| `Not` | `not` | Logical negation |

#### Keywords: Membership

| Token | Text | Context |
|-------|------|---------|
| `Contains` | `contains` | Collection membership test |
| `Is` | `is` | Multi-token operator prefix (`is set`, `is not set`) |

#### Keywords: Quantifiers / Modifiers

| Token | Text | Context |
|-------|------|---------|
| `All` | `all` | Universal quantifier / `modify all` / `omit all` (state-scoped) |
| `Any` | `any` | Quantifier keyword (`any item in Coll (pred)`) / state wildcard (`in any`, `from any`) |
| `Each` | `each` | Universal quantifier — true when every element in the collection satisfies the predicate. Usage: `each item in Collection (item > 0)` |
| `No` (quantifier role) | `no` | Quantifier keyword (`no item in Coll (pred)`) — dual role with `no transition` outcome keyword. Disambiguation by lookahead: followed by `Identifier in CollectionRef (` → quantifier; otherwise → `no transition`. |

#### Keywords: State Modifiers (v2)

| Token | Text | Context |
|-------|------|---------|
| `Terminal` | `terminal` | Structural: no outgoing transitions |
| `Required` | `required` | Structural: all initial→terminal paths visit this state (dominator) |
| `Irreversible` | `irreversible` | Structural: no path back to any ancestor state |
| `Success` | `success` | Semantic: marks a success outcome state |
| `Warning` | `warning` | Semantic: marks a warning outcome state |
| `Error` | `error` | Semantic: marks an error outcome state |

#### Keywords: Constraints

| Token | Text | Context |
|-------|------|---------|
| `Nonnegative` | `nonnegative` | Number/integer constraint: value >= 0 |
| `Positive` | `positive` | Number/integer constraint: value > 0 |
| `Nonzero` | `nonzero` | Number/integer constraint: value != 0 |
| `Notempty` | `notempty` | String constraint: non-empty (per-element in a collection's inner-type position; collection cardinality is `mincount 1`) |
| `Min` | `min` | Numeric minimum constraint / built-in function (dual-use) |
| `Max` | `max` | Numeric maximum constraint / built-in function (dual-use) |
| `Minlength` | `minlength` | String minimum length constraint |
| `Maxlength` | `maxlength` | String maximum length constraint |
| `Mincount` | `mincount` | Collection minimum count constraint |
| `Maxcount` | `maxcount` | Collection maximum count constraint |
| `Maxplaces` | `maxplaces` | Decimal maximum decimal places constraint |
| `Ordered` | `ordered` | Choice ordinal comparison constraint |
| `Countof` | `countof` | Bag element-count accessor — valid as a member name after `.` (v3) |
| `Peekby` | `peekby` | Priority-queue ordering-key peek accessor — valid as a member name after `.` (v3) |

#### Keywords: Types

| Token | Text | Context |
|-------|------|---------|
| `StringType` | `string` | Scalar type |
| `BooleanType` | `boolean` | Scalar type |
| `IntegerType` | `integer` | Scalar type (v2: explicit integer, separate from number) |
| `DecimalType` | `decimal` | Scalar type (v2: exact base-10) |
| `NumberType` | `number` | Scalar type (general numeric) |
| `ChoiceType` | `choice` | Enumerated value set type — requires explicit element type (`choice of T(...)`) |
| `SetType` | `set` | Set collection type; parser-synthesized type-position alias for the surface word `set` |
| `QueueType` | `queue` | Queue collection type |
| `StackType` | `stack` | Stack collection type |
| `BagType` | `bag` | Bag collection type (v3) |
| `ListType` | `list` | List collection type (v3) |
| `LogType` | `log` | Log collection type (v3) |
| `LookupType` | `lookup` | Lookup collection type (v3) |

#### Keywords: Temporal Types (v2)

| Token | Text | Context |
|-------|------|---------|
| `DateType` | `date` | Temporal: calendar date |
| `TimeType` | `time` | Temporal: time of day |
| `InstantType` | `instant` | Temporal: UTC point in time |
| `DurationType` | `duration` | Temporal: elapsed time quantity |
| `PeriodType` | `period` | Temporal: calendar quantity |
| `TimezoneType` | `timezone` | Temporal: timezone identity |
| `ZonedDateTimeType` | `zoneddatetime` | Temporal: date+time+timezone |
| `DateTimeType` | `datetime` | Temporal: local date+time |

#### Keywords: Business-Domain Types (v2)

| Token | Text | Context |
|-------|------|---------|
| `MoneyType` | `money` | Business: monetary amount |
| `CurrencyType` | `currency` | Business: currency identity |
| `QuantityType` | `quantity` | Business: measured quantity |
| `UnitOfMeasureType` | `unitofmeasure` | Business: unit identity |
| `DimensionType` | `dimension` | Business: dimension family identity |
| `PriceType` | `price` | Business: compound money/quantity rate |
| `ExchangeRateType` | `exchangerate` | Business: compound currency/currency rate |

#### Keywords: Literals

| Token | Text | Context |
|-------|------|---------|
| `True` | `true` | Boolean literal |
| `False` | `false` | Boolean literal |

#### Operators

| Token | Text | Precedence note |
|-------|------|-----------------|
| `DoubleEquals` | `==` | Comparison |
| `NotEquals` | `!=` | Comparison |
| `GreaterThanOrEqual` | `>=` | Comparison (scanned before `>`) |
| `LessThanOrEqual` | `<=` | Comparison (scanned before `<`) |
| `GreaterThan` | `>` | Comparison |
| `LessThan` | `<` | Comparison |
| `Assign` | `=` | Assignment (scanned after `==`) |
| `Plus` | `+` | Arithmetic |
| `Minus` | `-` | Arithmetic / unary negation |
| `Star` | `*` | Arithmetic |
| `Slash` | `/` | Arithmetic |
| `Percent` | `%` | Arithmetic (modulo) |
| `Arrow` | `->` | Action chain / outcome separator |
| `BackArrow` | `<-` | Computed field derivation arrow — structural separator on `field` declarations (not an expression-tree operator) |
| `CaseInsensitiveEquals` | `~=` | Case-insensitive comparison (string-only) |
| `CaseInsensitiveNotEquals` | `!~` | Case-insensitive not-equals (string-only) |
| `Tilde` | `~` | Case-insensitive modifier — collection inner type (`set of ~string`) or scalar field type qualifier (`field Email as ~string`) |

**Scan order for operators:** Multi-character operators must be attempted before their single-character prefixes: `!~` before `!=` before `!` (if ever reintroduced), `~=` before `~`, `->` before `-`, `<-` before `<=` and `<`, `==` before `=`, `>=` before `>`, `<=` before `<`. The `Tilde` token is always emitted wherever `~` appears. Invalid uses (e.g., `~` before a non-`string` type, `~` before an identifier that is not `startsWith`/`endsWith`) are caught by the parser or type checker, not the lexer.

**`~startsWith` and `~endsWith`** are not single tokens. They lex as two tokens: `Tilde` followed by the identifier `startsWith` or `endsWith`. The parser recognizes `Tilde` in null-denotation (prefix) position immediately before one of these identifiers as a CI function call. This is distinct from `~=` and `!~`, which are single compound tokens scanned as units. The Tilde token in expression prefix position is only valid before `startsWith`/`endsWith` identifiers. If `Tilde` in expression position is followed by any identifier other than `startsWith` or `endsWith`, the parser emits `ExpectedToken` with `{0}` = `'startsWith or endsWith'`, `{1}` = the actual identifier text, and highlights the `Tilde` + identifier span.

#### Punctuation

| Token | Text |
|-------|------|
| `Dot` | `.` |
| `Comma` | `,` |
| `LeftParen` | `(` |
| `RightParen` | `)` |
| `LeftBracket` | `[` |
| `RightBracket` | `]` |

#### Literals

| Token | Produced when |
|-------|---------------|
| `NumberLiteral` | Digit sequence; optionally followed by a decimal part (`.` + more digits) and/or an exponent part (`e`/`E`, optional sign, digits) — see §1.3 for the full grammar |
| `StringLiteral` | `"..."` with no `{` interpolation (emitted as a single token) |
| `StringStart` | `"...{` — text before the first interpolation opening |
| `StringMiddle` | `}...{` — text between interpolation segments |
| `StringEnd` | `}..."` — text after the last interpolation closing |
| `TypedConstant` | `'...'` with no `{` interpolation (emitted as a single token) |
| `TypedConstantStart` | `'...{` — typed constant before first interpolation |
| `TypedConstantMiddle` | `}...{` — typed constant between interpolation segments |
| `TypedConstantEnd` | `}...'` — typed constant after last interpolation |

**`Token.Text` contract for quoted literals:** `Text` contains the semantic content — delimiters stripped, escape sequences resolved. `StringLiteral` for `"hello"` → `Text = "hello"`. `StringStart` for `"Hello {` → `Text = "Hello "`. `StringEnd` for `} world"` → `Text = " world"`. `TypedConstant` for `'2026-04-23'` → `Text = "2026-04-23"`. A zero-length segment (e.g. `"{Name}"` where nothing precedes the first `{`) produces an empty `Text` — the token is still emitted.

**`Token.Offset` and `Token.Length` contract for quoted literals:** `Offset` and `Length` span the full raw source range including opening and closing delimiters. `StringLiteral` for `"hello"` has `Length = 7` (both quotes included). `StringStart` for `"Hello {` spans through the `{`. This allows tools to highlight or replace the exact source text without having to re-infer delimiter positions. `Text` (content-only) and `Offset`/`Length` (raw-source span) are complementary.

See [§1.3 Literal Syntax](#13-literal-syntax) for full rules. See `docs/compiler/literal-system.md` for the complete literal system design.

#### Identifiers

| Token | Produced when |
|-------|---------------|
| `Identifier` | A word matching the identifier grammar that is not a reserved keyword |

```
Identifier  :=  Letter (Letter | Digit | '_')*
Letter      :=  [a-zA-Z]
Digit       :=  [0-9]
```

Identifiers are case-sensitive. Leading underscores are not permitted. Examples: `Balance`, `applicantName`, `Phase1`, `line_item_total`.

#### Structure

| Token | Produced when |
|-------|---------------|
| `Comment` | `#` through end of line |
| `NewLine` | Line terminator (LF, CRLF, or CR) |
| `EndOfSource` | Sentinel appended after all source text is consumed |

### 1.2 Reserved Keywords

Keywords are **strictly lowercase**. Identifiers are case-sensitive: `From` is a valid identifier, `from` is a reserved keyword.

The complete v2 reserved keyword set:

```
precept  field  as  default  optional  rule  because
state  initial  terminal  required  irreversible  event  ensure
success  warning  error
in  to  from  on  when  any  all  each  of  by  at  for
set  add  remove  enqueue  dequeue  push  pop  clear  into  append  insert  put
transition  no  reject
omit  modify  readonly  editable
string  number  boolean  integer  decimal  choice  maxplaces  ordered
queue  stack  bag  list  log  lookup
date  time  instant  duration  period  timezone  zoneddatetime  datetime
money  currency  quantity  unitofmeasure  dimension  price  exchangerate
true  false
and  or  not  contains  is
if  then  else
nonnegative  positive  nonzero  notempty
min  max  minlength  maxlength  mincount  maxcount
ascending  descending  countof  peekby
```

**v2 additions** (not in v1): `optional`, `omit`, `clear`, `nonzero`, `is`, `integer`, `decimal`, `choice`, `maxplaces`, `ordered`, `terminal`, `required`, `irreversible`, `success`, `warning`, `error`, `date`, `time`, `instant`, `duration`, `period`, `timezone`, `zoneddatetime`, `datetime`, `money`, `currency`, `quantity`, `unitofmeasure`, `dimension`, `price`, `exchangerate`, `each`.

> **Quantifier keywords:** `each` (v2), `any`, and `no` serve as quantifier keywords in expression position when followed by `Identifier in CollectionRef (`. `each` is quantifier-only. `any` also appears as state wildcard (`in any`, `from any`). `no` also appears in `no transition`. Disambiguation is by lookahead at the parser level — the lexer emits a single token kind for each.

**v3 additions** (not in v2): `modify`, `readonly`, `editable`, `bag`, `list`, `log`, `lookup`, `by`, `at`, `for`, `ascending`, `descending`, `append`, `insert`, `put`, `countof`, `peekby`. The access mode verbs `write`/`read` are removed entirely in favor of the `modify` verb + adjective pattern. `write` and `read` are no longer reserved — they are ordinary identifiers in v3.

**v3 removals:** `write` and `read` are not reserved in v3. The `modify` verb + `readonly`/`editable` adjective pattern replaces them. Both are ordinary identifiers in v3; no special parser recognition is needed.

**v1 removals:** `nullable`, `null`, and `edit` are not reserved in v2. `optional` replaces `nullable`. `modify` replaces `edit`. The `null` literal is removed entirely — `optional` fields use `is set`/`is not set` for presence testing and `clear` for value removal. All three are ordinary identifiers in v2; no special parser recognition is needed.

### 1.3 Literal Syntax

The language has two quoted literal forms with distinct roles. Double-quoted strings (`"..."`) always produce `string`. Single-quoted typed constants (`'...'`) produce non-primitive values — the lexer treats their content opaquely and the type checker determines the specific type (see [§3.3 Context-Sensitive Type Resolution](#33-context-sensitive-type-resolution)). There is no constructor-call syntax for non-primitive types (e.g., no `date(...)` or `period(...)`) — typed constants are the sole mechanism for constructing non-primitive literal values. Both quoted forms support `{expr}` interpolation. Numeric and boolean literals remain bare tokens.

#### Numeric literals

A numeric literal is a sequence of decimal digits, optionally preceded by a unary minus, and optionally followed by a decimal part and/or an exponent part. The parser constant-folds `-` followed by a `NumberLiteral` into a single signed literal value — negative numbers are first-class literals, not runtime negation. No leading `+`. No underscores or grouping separators.

```
NumberLiteral  :=  '-'? Digits ('.' Digits)? (('e' | 'E') ('+' | '-')? Digits)?
Digits         :=  [0-9]+
```

Examples: `0`, `42`, `-1`, `3.14`, `-3.14`, `0.5`, `1.5e2`, `-1e5`, `1e-5`, `3.0E+10`

The lexer produces a single `NumberLiteral` token for all numeric forms. The type checker determines the specific numeric type based on context (see [§3.3 Context-Sensitive Type Resolution](#33-context-sensitive-type-resolution)).

**Exponent notation and numeric types:** Exponent notation (`e`/`E`) is only valid for the `number` type. It is a type error to use exponent notation in a context that requires `integer` or `decimal` — `integer` is whole numbers only, and `decimal` is exact base-10 representation where exponent form would be semantically misleading. The lexer accepts all forms; the type checker enforces the restriction.

#### String literals (`"..."`)

String literals are delimited by double quotes. They always produce `string` type values.

**Without interpolation:** `"hello world"` → single `StringLiteral` token.

**With interpolation:** The lexer decomposes the string into segments at `{` and `}` boundaries:

```
"Hello {Name}, your balance is {Balance}"
```
→ `StringStart("Hello ")`, `Identifier(Name)`, `StringMiddle(", your balance is ")`, `Identifier(Balance)`, `StringEnd("")`

Interpolation is always-on — `{` inside a string always opens an interpolation expression. To include a literal `{`, escape it as `{{`. To include a literal `}`, escape it as `}}`.

**Empty interpolation:** `"{}"` is lexically valid. The lexer emits `StringStart("")`, then immediately sees `}` and emits `StringEnd("")` with no expression tokens between them. The parser rejects empty interpolation as a syntax error (expected expression). Zero-length `Text` on `StringStart`/`StringEnd` is normal and expected — the lexer always emits the boundary token even when the content is empty.

**Escape sequences in strings:** `\"` (double quote), `\\` (backslash), `\n` (newline), `\t` (tab), `{{` (literal brace), `}}` (literal brace).

#### Typed constants (`'...'`)

Typed constants are delimited by single quotes. The type is determined by expression context (not by content) — the lexer treats the content opaquely.

**Without interpolation:** `'2026-04-23'` → single `TypedConstant` token.

**With interpolation:** Same decomposition rules as strings, producing `TypedConstantStart`, `TypedConstantMiddle`, `TypedConstantEnd` tokens.

**Escape sequences:** `\'` (single quote), `\\` (backslash), `{{` (literal brace), `}}` (literal brace).

**Content words are not keywords.** Words that appear inside typed constant content — such as `days`, `hours`, `minutes`, `seconds`, `months`, `years`, `weeks`, `USD`, `kg` — are not language keywords. They are validated by the type checker against the context-determined type, not by the lexer. This keeps the reserved keyword set stable as new types are added.

#### List literals

List literals are delimited by `[` and `]` with comma-separated scalar values. The lexer produces individual punctuation and value tokens — list structure is assembled by the parser.

```
[1, 2, 3]       → LeftBracket, NumberLiteral, Comma, NumberLiteral, Comma, NumberLiteral, RightBracket
["a", "b"]       → LeftBracket, StringLiteral, Comma, StringLiteral, RightBracket
[]               → LeftBracket, RightBracket
```

### 1.4 Comments and Whitespace

Precept source has no indentation significance. Indentation is purely cosmetic — the lexer treats leading spaces and tabs identically to inter-token whitespace.

**Comments:** `#` begins a comment that extends to the end of the line. Comments can appear standalone on a line or after code on the same line. The lexer emits a `Comment` token (the parser may discard or preserve it).

```
# This is a standalone comment
field Balance as number  # This is an inline comment
```

**Whitespace:** Spaces and tabs between tokens are consumed silently (no token emitted). They serve only as delimiters between keyword/identifier tokens.

**Newlines:** Line terminators (LF `\n`, CRLF `\r\n`, CR `\r`) produce `NewLine` tokens, but they are trivia to the parser rather than structural boundaries. Statement kind is determined entirely by the opening keyword sequence, not by line position or termination. Declarations may span multiple lines freely. Within transitions, `from` introduces a new transition row and each `->` introduces a new pipeline step; those keywords are the only structural separators inside the transition surface.

### 1.5 Operator and Punctuation Scanning

Operators and punctuation are scanned after attempting keyword/identifier matches. Multi-character operators are tried before their single-character prefixes to avoid false matches.

**Scan priority (highest first):**

1. `->` (Arrow)
2. `<-` (BackArrow) — before `<=` and lone `<`
3. `~=` (CaseInsensitiveEquals) — before lone `~`
4. `!~` (CaseInsensitiveNotEquals) — before `!=`
5. `==` (DoubleEquals)
6. `!=` (NotEquals)
7. `>=` (GreaterThanOrEqual)
8. `<=` (LessThanOrEqual)
9. `~` (Tilde) — after `~=`, so lone tilde is only reached when `~=` did not match
10. `=` (Assign)
11. `>` (GreaterThan)
12. `<` (LessThan)
13. `+`, `-`, `*`, `/`, `%` (Arithmetic)
14. `.`, `,`, `(`, `)`, `[`, `]` (Punctuation)

### 1.6 Dual-Use Token Disambiguation

Three surface forms serve double duty. For `set`, the lexer emits `Set` and the parser may reinterpret it as `SetType` in type position; for the others, the lexer emits a single token kind and the parser disambiguates by syntactic context.

#### `set` — Collection Type and Action Keyword

| Preceding context | Interpretation | Example |
|-------------------|---------------|---------|
| After `as` or `of` (type position) | `SetType` | `field Tags as set of string` |
| After `->` (action position) | `Set` (action) | `-> set Balance = 0` |

A third use exists: `set` as an adjective in the presence operators `is set` / `is not set` (for `optional` fields). This is not a lexer disambiguation concern — the lexer emits separate `Is`, `Not`, and `Set` tokens, and the parser composes the multi-token operator.

**Lexer strategy (locked):** The lexer always emits `TokenKind.Set` for the word `set`. `TokenKind.SetType` is never produced by the lexer — it is a parser-synthesized token kind used in the AST to represent `set` in a type position. The `Tokens.Keywords` dictionary maps `"set"` to `TokenKind.Set` only. The parser reinterprets the `Set` token as `SetType` when the preceding token is `As` or `Of`. The token model is therefore `Set` (lexer) plus `SetType` (parser-synthesized type alias), not one dual-category token.

#### `min` / `max` — Constraint Keyword and Built-in Function

| Following context | Interpretation | Example |
|-------------------|---------------|---------|
| Followed by a number literal (constraint zone) | Constraint | `field Score as number min 0 max 100` |
| Followed by `(` (expression context) | Function call | `set Amount = min(Requested, Available)` |

The `(` disambiguates: constraint keywords are never followed by `(`, function calls always are.

#### `in` / `of` — Preposition and Type Qualifier

`in` and `of` each serve two roles: routing/scoping prepositions and type-position qualifiers.

| Context | Role | Example |
|---------|------|---------|
| After a state name (`in Draft ensure ...`) | Routing preposition | `in Draft ensure Amount > 0` |
| After a domain type in a field declaration | Type qualifier | `field Amount as money in 'USD'` |
| After `set of`, `queue of`, `stack of` | Collection inner type | `field Tags as set of string`, `field Labels as set of ~string` |
| After a domain type in a field declaration | Dimension family qualifier | `field Distance as quantity of 'length'` |

Type qualifiers narrow the value domain of the field — they are part of the type annotation, not a declaration modifier. `in '<unit>'` pins to a specific unit or currency. `of '<family>'` constrains to a dimension or component family. A field may use `in` or `of`, never both — with one exception: `price` allows `in` (currency-only) combined with `of` (denominator dimension), because price has two independent axes. For `price`, `of 'time'` and `of 'date'` are valid temporal denominator qualifiers on that same axis; the proof engine uses them to validate `price * period` and `price * duration` cancellation. When `in` specifies a compound `'currency/unit'` value, `of` is rejected. The preceding token (always a type keyword or collection keyword) makes the type-qualifier role unambiguous at LL(1).

For business counting units, the dimension-family view and the unit-identity view are deliberately different. Units such as `each`, `box`, `case`, and `pallet` all report the shared count dimension (`DimensionVector.None`) and normalize with factor one, but that representation only says "these are count quantities." It does **not** mean the codes are interchangeable or silently convertible. The current cross-counting-unit bug exists precisely because `each.dimension = DimensionVector.None` is true for every packaging-relative count unit, so any rule that checks only dimension compatibility cannot distinguish unit identity inside that family.

This applies equally to familiar bundle words such as `dozen` and `gross`: lexical familiarity does not grant an implicit compile-time conversion. If business meaning depends on how many `each` values are in one `dozen` or `gross`, authors must model that relationship explicitly with a conversion field rather than relying on silent unit reconciliation.

The lexer uses a mode stack to handle nested interpolation in string and typed-constant literals. This ensures `{expr}` inside a literal correctly lexes the expression tokens and then returns to the literal context.

**Modes:**

| Mode | Active when |
|------|-------------|
| `Normal` | Default mode — scanning keywords, identifiers, operators, literals |
| `String` | Inside a `"..."` literal, scanning text and looking for `{` or `"` |
| `TypedConstant` | Inside a `'...'` literal, scanning text and looking for `{` or `'` |
| `Interpolation` | Inside `{...}` within a literal, scanning expression tokens and looking for `}` |

**Transitions:**

| From | On | To | Emits |
|------|----|----|-------|
| Normal | `"` | String | (begins string scanning) |
| String | `{` | Interpolation (push) | `StringStart` or `StringMiddle` |
| String | `"` | Normal | `StringEnd` or `StringLiteral` |
| Normal | `'` | TypedConstant | (begins typed constant scanning) |
| TypedConstant | `{` | Interpolation (push) | `TypedConstantStart` or `TypedConstantMiddle` |
| TypedConstant | `'` | Normal | `TypedConstantEnd` or `TypedConstant` |
| Interpolation | `}` | (pop to String or TypedConstant) | (resumes enclosing literal scanning) |
| Interpolation | `"` | String (push) | (nested string inside interpolation) |
| Interpolation | `'` | TypedConstant (push) | (nested typed constant inside interpolation) |

Nesting is fully supported: a string interpolation expression can contain a typed constant, and vice versa. The mode stack has a maximum depth of **8**. If a push would exceed this limit, the lexer emits an `UnterminatedInterpolation` diagnostic and resumes using the recovery rule for unterminated interpolations. Realistic nesting depth is 3 or fewer; the limit exists to prevent unbounded stack growth on adversarial input.

### 1.8 Lexer Diagnostics

The lexer emits diagnostics for malformed input. These are collected alongside tokens in the `TokenStream`.

The `InputTooLarge` diagnostic is different from the other entries in this table: it is a security failure, not a syntax failure. Once the source crosses the 65,536-character ceiling, lexing aborts immediately and returns only the `EndOfSource` sentinel so downstream stages never process the hostile input.

| Condition | Diagnostic Code | Severity | Description |
|-----------|-----------------|----------|-------------|
| Input too large | `InputTooLarge` | Error | Source exceeds 65536 characters (64 KB), which is the lexer security limit; lexing is aborted and the token stream contains only the `EndOfSource` sentinel |
| Unterminated string literal | `UnterminatedStringLiteral` | Error | `"hello` with no closing `"` before end of line/source |
| Unterminated typed constant | `UnterminatedTypedConstant` | Error | `'2026-01-01` with no closing `'` before end of line/source |
| Unterminated interpolation | `UnterminatedInterpolation` | Error | `"hello {Name` with no closing `}` before end of line |
| Unrecognized character | `InvalidCharacter` | Error | Character that is not part of any valid token |
| Unrecognized string escape | `UnrecognizedStringEscape` | Error | `\X` inside `"..."` where X is not `"`, `\`, `n`, or `t` |
| Unrecognized typed constant escape | `UnrecognizedTypedConstantEscape` | Error | `\X` inside `'...'` where X is not `'` or `\` (note: `\n` and `\t` are also invalid here) |
| Unescaped `}` in literal | `UnescapedBraceInLiteral` | Error | A lone `}` inside a string or typed constant that was not doubled (`}}`) |

The lexer continues scanning after diagnostics to maximize token recovery for downstream error reporting.

#### Recovery rules

Each error condition has a defined recovery boundary:

| Condition | Recovery boundary | Rationale |
|-----------|-------------------|-----------|
| Unterminated string literal | Scan to end of current line; resume in `Normal` mode on the next line | Prevents the rest of the file from being consumed as string content |
| Unterminated typed constant | Scan to end of current line; resume in `Normal` mode on the next line | Same as above for `'...'` literals |
| Unterminated interpolation | Scan forward for a `}` at depth 0; if none found before end of current line, resume in the enclosing literal mode at the next line | Recovers the enclosing literal context where possible |
| Unrecognized character | Skip the single character; resume scanning at the next character | Minimal disruption — one bad character should not invalidate surrounding tokens |
| Unrecognized string escape | Skip the `\` and the following character; continue in `String` mode | Preserves surrounding content; the bad sequence is omitted from `Text` |
| Unrecognized typed constant escape | Skip the `\` and the following character; continue in `TypedConstant` mode | Same as above |
| Unescaped `}` in literal | Preserve the `}` as literal content in the token's `Text`; continue scanning | Recovers the character so surrounding content remains intact |

In all cases the invalid source span still produces a diagnostic with the correct `SourceSpan`. Post-recovery tokens are emitted normally so the parser and downstream stages can report additional errors.

---

## 2. Parser

The parser transforms the flat `TokenStream` into a `ConstructManifest` — a flat ordered list of parsed constructs representing the semantic structure of the precept definition. The parser is a hand-written recursive descent parser with a Pratt expression parser for operator precedence. It produces a flat manifest (not a tree or CST) — comments and whitespace are consumed silently.

```
TokenStream  →  Parser.Parse  →  ConstructManifest
```

The public surface is a static class `Parser` with a single method:

```csharp
public static ConstructManifest Parse(TokenStream tokens)
```

The parser always runs to end-of-source. On malformed input it emits diagnostics and inserts `IsMissing` nodes or skips to sync points, ensuring downstream stages receive a structurally coherent manifest.

### 2.1 Expression Precedence

| Precedence | Token(s) | Role | Associativity |
|:----------:|----------|------|:-------------:|
| 10 | `or` | logical disjunction | left |
| 20 | `and` | logical conjunction | left |
| 25 (prefix) | `not` | logical negation | right (prefix) |
| 30 | `==` `!=` `~=` `!~` `<` `>` `<=` `>=` | comparison | non-associative |
| 40 | `contains` | collection membership | non-associative |
| 40 | `for` | lookup key access (infix) | left |
| 50 | `+` `-` (infix) | additive arithmetic | left |
| 60 | `*` `/` `%` | multiplicative arithmetic | left |
| 60 | `is` (`is set` / `is not set`) | presence test (postfix) | non-associative |
| 65 (prefix) | `-` (unary) | negation | right (prefix) |
| 80 | `.` | member access | left |
| 80 | `(` (postfix) | function/method call | left |

**Non-associative operators:** Comparison operators (`==`, `!=`, etc.) and `contains` are non-associative — chaining (`A == B == C`, `A contains B contains C`) is a parse error. The parser detects when the left operand is already a non-associative binary expression and emits a `NonAssociativeComparison` diagnostic. (Right-binding powers P+1 prevent right-chaining; the explicit left-operand check prevents left-chaining.) **Postfix `is set`/`is not set`** are non-associative — a presence-test result is a `boolean`, not a collection, so chaining is always a type error regardless. At precedence 60 they share a level with `*`/`/`/`%`; the Pratt `nextMinPrec = 61` when parsing a multiplicative right operand ensures `A * B is set` parses as `(A * B) is set`, not `A * (B is set)`.

*Implementation note:* The expression parser uses Pratt parsing (top-down operator precedence). `ParseExpression(int minBp)` parses a complete expression, stopping when it encounters a token whose left-binding power is ≤ `minBp`.

**Structural separators (not expression operators):** `->` (action / outcome separator) and `<-` (computed-field derivation arrow on `field` declarations) are lexed as operator tokens but are not part of expression precedence — they delimit declaration slots and are consumed by the construct walker, never by the Pratt parser. `<-` appears only between a field's modifier list and the computed-expression body (see [§ 2.2 Declaration Grammar](#22-declaration-grammar)).

#### Null-denotation (atoms and prefix)

| Token | Production |
|-------|------------|
| `Identifier` | `IdentifierExpression` |
| `NumberLiteral` | `LiteralExpression` |
| `True` / `False` | `LiteralExpression` |
| `StringLiteral` | `LiteralExpression` |
| `StringStart` | `InterpolatedStringExpression` (reassembly loop) |
| `TypedConstant` | `TypedConstantExpression` |
| `TypedConstantStart` | `InterpolatedTypedConstantExpression` (reassembly loop) |
| `LeftBracket` | `ListLiteralExpression` |
| `LeftParen` | `ParenthesizedExpression` |
| `Not` | `UnaryExpression(Not, ParseExpression(25))` |
| `Minus` | `UnaryExpression(Negate, ParseExpression(65))` |
| `If` | `ConditionalExpression` (`if` Expr `then` Expr `else` Expr) |
| `Each` / `Any` (quantifier) / `No` (quantifier) — when followed by `Identifier in CollectionRef (` | `QuantifierExpression { Kind, BindingVar, Collection, Predicate }` — consumes `Identifier in CollectionRef ( BoolExpr )`. `any` and `no` are disambiguated by lookahead: if the token after `any`/`no` is `Identifier` followed by `in` followed by `CollectionRef` followed by `(`, parse as quantifier; otherwise `any` continues as type/state modifier, `no` continues as transition keyword. |
| `Tilde` (when next token is `startsWith` or `endsWith` identifier) | `CIFunctionCallExpression` — consumes the identifier and `(` Expr `,` Expr `)` to produce `~startsWith(arg1, arg2)` or `~endsWith(arg1, arg2)`. Return type: `boolean`. First argument must be `~string`; type-checker errors otherwise (see §3.8 `~string` enforcement). |
| _other_ | missing `IdentifierExpression` + diagnostic |

#### Left-denotation (infix and postfix)

| Token | Production |
|-------|------------|
| `Or` | `BinaryExpression(Or, ParseExpression(10))` |
| `And` | `BinaryExpression(And, ParseExpression(20))` |
| `==` `!=` `~=` `!~` `<` `>` `<=` `>=` | `BinaryExpression(op, ParseExpression(31))` |
| `Contains` | `BinaryExpression(Contains, ParseExpression(41))` |
| `For` | `BinaryExpression(LookupAccess, ParseExpression(41))` — lookup field access; left operand is the `lookup of K to V` field, right is the key expression `K`; result type is `V` |
| `Is` | `IsSetExpression` — consumes optional `Not`, then `Set` |
| `+` `-` (infix) | `BinaryExpression(op, ParseExpression(50))` |
| `*` `/` `%` | `BinaryExpression(op, ParseExpression(60))` |
| `.` (Dot) | `MemberAccessExpression(left, Identifier)` |
| `(` (LeftParen) | If `left` is `MemberAccessExpression` → `MethodCallExpression`; if `IdentifierExpression` → `CallExpression`; else → diagnostic |

### 2.2 Declaration Grammar

After the `precept <Name>` header, the parser enters a loop that dispatches on the current non-trivia token to select a declaration production.

#### Top-level dispatch

| Leading token | Production |
|---------------|-----------|
| `field` | `FieldDeclaration` |
| `state` | `StateDeclaration` |
| `event` | `EventDeclaration` |
| `rule` | `RuleDeclaration` |
| `in` | `StateEnsureDeclaration`, `AccessModeDeclaration`, or `OmitDeclaration` |
| `to` | `StateEnsureDeclaration` or `StateActionDeclaration` |
| `from` | `TransitionRowDeclaration`, `StateEnsureDeclaration`, or `StateActionDeclaration` |
| `on` | `EventEnsureDeclaration` or `EventHandlerDeclaration` |
| `EndOfSource` | exit loop |
| _anything else_ | diagnostic + sync-point resync |

#### `field` declaration

```
field Identifier ("," Identifier)* as TypeRef FieldModifier* ("<-" Expr)?
```

Multi-name shorthand: `field A, B, C as string` declares three fields of the same type. Modifiers appear before the computed expression arrow. The `<-` introduces a computed expression.

#### `state` declaration

```
state StateEntry ("," StateEntry)*
StateEntry  :=  Identifier ("initial")? StateModifier*
StateModifier  :=  terminal | required | irreversible | success | warning | error
```

#### `event` declaration

```
event Identifier ("," Identifier)* ("(" ArgList ")")? ("initial")?
ArgList  :=  ArgDecl ("," ArgDecl)*
ArgDecl  :=  Identifier as TypeRef FieldModifier*
```

Event arguments use parenthesized syntax. The `initial` keyword follows the argument list.

#### `rule` declaration

```
rule BoolExpr ("when" BoolExpr)? because StringExpr
```

The optional `when` guard scopes the rule to states where the guard is true.

#### `in` / `to` / `from` dispatch

These preposition keywords parse a state target, then look ahead to select the production:

| Preposition | Following verb | Production |
|-------------|---------------|-----------|
| `in` | `ensure` | state ensure (scoped to `in`) |
| `in` | `modify` | access mode (state-scoped) |
| `in` | `omit` | omit declaration (state-scoped structural exclusion) |
| `to` | `ensure` | state ensure (scoped to `to`) |
| `to` | `->` | state action (entry hook) |
| `from` | `on` | transition row |
| `from` | `ensure` | state ensure (scoped to `from`) |
| `from` | `->` | state action (exit hook) |

```
StateTarget  :=  Identifier ("," Identifier)* | any
EventTarget  :=  Identifier
```

`any` remains the state wildcard. A comma-delimited `StateTarget` is pure syntactic sugar: the compiler expands it into one independent state-scoped declaration per named state, preserving source order and leaving the guard, actions, ensure body, access mode, omit target, or outcome unchanged. State ensures, state actions, and access modes support an optional pre-verb `when` guard between the state target and the construct verb. Transition rows keep their guard after the event target, and `omit` declarations do not support `when`.

#### Transition row

> **Draft 2026-11-25** — under adversarial review; nothing may cite this section until the mark is removed.

```
from StateTarget on EventTarget ("when" BoolExpr)?
("->" ActionStatement)*
"->" Outcome

ActionStatement  :=  set Identifier "=" Expr
                  |  add Identifier Expr
                  |  remove Identifier Expr
                  |  remove Identifier "at" Expr
                  |  enqueue Identifier Expr
                  |  enqueue Identifier Expr "by" Expr
                  |  dequeue Identifier ("into" Identifier ("by" Identifier)?)?
                  |  push Identifier Expr
                  |  pop Identifier ("into" Identifier)?
                  |  clear Identifier
                  |  append Identifier Expr
                  |  append Identifier Expr "by" Expr
                  |  insert Identifier Expr "at" Expr
                  |  put Identifier Expr "=" Expr

Outcome  :=  transition Identifier
          |  no transition
          |  reject StringExpr
```

Each action and the outcome are introduced by `->`. The `->` arrow is deliberately overloaded to create a visual pipeline that reads top-to-bottom: each step in a transition — guard, actions, outcome — flows through the same arrow. The parser loops consuming `->` followed by an action keyword, and breaks out when the token after `->` is an outcome keyword. If `StateTarget` is a comma-delimited list, the row desugars to one independent transition row per named state; each expanded row keeps the same event target, guard, action chain, and outcome.

> **Draft 2026-11-25** — under adversarial review; nothing may cite this section until the mark is removed.

Two clauses name fields that receive a value. `into Identifier` on `dequeue` and `pop` names the field that receives the removed element, and `by Identifier` on `dequeue` names the field that receives the removed element's ordering value. The `by` capture is permitted only after `into` and only on a `queue of T by P` ([`collection-types.md`](collection-types.md) § Dequeue with Ordering Value Capture). It is a capture, not a selector: `dequeue` always removes the front element. This is a different use of `by` from `enqueue F Expr by Expr` and `append F Expr by Expr`, where the expression after `by` supplies the ordering value of the element being added. Every field each action form writes is listed in §3A.4, *What can write during one operation*.

#### State/event ensure

```
(in|to|from) StateTarget ("when" BoolExpr)? ensure BoolExpr because StringExpr
on EventTarget ("when" BoolExpr)? ensure BoolExpr because StringExpr
```

The optional `when` guard is pre-verb: it appears after the state/event target and before `ensure`. For state-anchored ensures, a comma-delimited `StateTarget` expands to one independent ensure per named state.

#### Stateless event hook

> **Draft 2026-11-25** — under adversarial review; nothing may cite this section until the mark is removed.

```
on EventTarget ("when" BoolExpr)?
("->" ActionStatement)*

on EventTarget ("when" BoolExpr)?
"->" reject StringExpr
```

An event row is the `on Event -> ...` form: it has no state precondition. It is used for the event handlers of a stateless precept and for construction rows (§3A.5); the parser produces the same construct for both, and the type checker tells them apart by the event's `initial` modifier. An event row may carry a `when` guard after the event target, and it either runs an action chain or rejects with a reason; it has no `transition` or `no transition` outcome. Several rows for one event are tried in declaration order and the first whose guard is true wins (§3A.1, §3A.4). `on Event ensure ...` is the separate event ensure form, and an event row has no trailing `ensure`.

An earlier version of this subsection said these rows take no `when` guard. §3A.5 records why that restriction was removed — it was a parser simplification with no semantic reason behind it — and its construction rows use the guard and the `reject` form; this subsection now agrees with it.

#### State action

```
(to|from) StateTarget ("when" BoolExpr)?
("->" ActionStatement)*
```

State actions support an optional `when` guard between the state target and the action chain. The guard is passed through to the AST node. A comma-delimited `StateTarget` expands to one independent hook per named state.

#### Quantifier expression grammar

```
QuantifierExpr   :=  QuantifierKind Identifier in CollectionRef '(' BoolExpr ')'
QuantifierKind   :=  each | any | no
CollectionRef    :=  Identifier   (bare field name only in v1; member access is a parse error)
```

`CollectionRef` is restricted to a bare field name (`Identifier`) in v1. `event.FieldName` or computed expressions are not valid — emit `ExpectedFieldName` at the `in` position if a non-identifier follows.

`QuantifierExpr` is a boolean-valued expression. It appears in all boolean expression positions: `when` guards, `rule` expressions, `ensure` expressions, and (recursively) inside other quantifier predicates. The binding variable (`Identifier`) is a fresh name introduced by the quantifier and scoped strictly to the parenthesized `BoolExpr`. See §3.5 for binding variable scope rules and §3.8 for type-checking rules.

#### Access mode and omit declaration

Two separate constructs govern per-state field access: `AccessMode` (optional pre-verb guard + `modify` + adjective) and `OmitDeclaration` (omit, no guard). They share the `in` preposition but are distinct `ConstructKind`s with different slot sequences.

```
FieldTarget  :=  identifier ("," identifier)* | all

── AccessMode: modify (field present, access level declared) ───────────────────
in StateTarget ("when" BoolExpr)? modify Field readonly                  ← singular access constraint
in StateTarget ("when" BoolExpr)? modify Field editable                  ← singular access upgrade
in StateTarget ("when" BoolExpr)? modify Field { "," Field }* readonly  ← comma-separated shorthand
in StateTarget ("when" BoolExpr)? modify Field { "," Field }* editable  ← comma-separated shorthand
in StateTarget ("when" BoolExpr)? modify all readonly                    ← state-scoped all
in StateTarget ("when" BoolExpr)? modify all editable                    ← state-scoped all

── OmitDeclaration: omit (field structurally absent — no guard) ────────────────
in StateTarget omit Field                                            ← singular structural exclusion
in StateTarget omit Field { "," Field }*                             ← comma-separated shorthand
in StateTarget omit all                                              ← state-scoped all (no fields visible)
```

**Two verbs, two roles:**
- `modify` = constraint verb (field present, access mode applied). Takes a field target and an access mode adjective (`readonly` or `editable`). Parallel to `omit` — both are verbs, different semantics.
- `omit` = exclusion verb (field absent from state entirely). Takes a field target only — no adjective, no guard.

**Two-layer access mode composition model:**

- **Layer 1 — field-level baseline (`editable` modifier):** `editable` on a field declaration sets that field's baseline access mode to writable across all states. Fields without `editable` default to read-only.
- **Layer 2 — state-level override (`in <State> modify|omit`):** State-scoped declarations override the field's baseline for a specific (field, state) pair only. State-level always wins over the field-level baseline.
- **Undeclared (field, state) pairs** use the field's baseline: read-only for fields without `editable`, editable for fields with `editable`.

Root-level access mode declarations are **not valid syntax** — use the `editable` modifier on the field declaration for field-level mutability. All access mode overrides are state-scoped.

State-scoped access modes (`in StateTarget`) use `modify` for constraint declarations and `omit` for structural exclusion. `StateTarget` may be a single state name, a comma-delimited list of state names, or `any`; comma-delimited state targets expand to one independent access-mode or omit declaration per named state. Guarded access modes read `in <StateTarget> when <Guard> modify <FieldTarget> readonly|editable`; the field target is either `all` or a comma-separated list of field names.

The `editable` keyword is a single shared keyword at both positions (field declaration and per-state `modify F editable`). This is the cross-position-unified pattern used by 6 of 8 surveyed comparator languages (TypeScript `readonly`, Kotlin `val`/`var`, Swift `let`/`var`, C# `readonly`, Java `final`, Scala `val`/`var`); the prior split between `writable` (field declaration) and `editable` (per-state) was retired before external authors saw it.

**Composition rules:**
1. **Field baseline** — `editable` modifier on a field declaration sets the field's default to editable across all states.
2. **D3 default** — fields without `editable` default to read-only for every (field, state) pair unless overridden by a state-scoped declaration.
3. **State-level override always wins** — an explicit `in <State> modify|omit` declaration overrides the field's baseline for that (field, state) pair only.
4a. **`readonly` and `editable` are the only guarded access modes** — guarded `editable` upgrades a read-only baseline to editable when the guard holds; guarded `readonly` downgrades a writable baseline to read-only when the guard holds; in both cases the field is always structurally present. `omit` cannot be guarded because conditional structural presence breaks static per-state field maps.
4b. **Guarded `readonly` requires an `editable` baseline** — a guarded `readonly` on a field without `editable` is a compile error (`RedundantAccessMode`); both branches would otherwise resolve to read-only, making the guard vacuous.
4c. **Unguarded declarations must change the effective mode** — `in <State> modify F editable` where `F` carries `editable` (editable is already the baseline) and `in <State> modify F readonly` where `F` lacks `editable` (read-only is the D3 default) are both compile errors (`RedundantAccessMode`). A declaration that resolves to the same mode the field already falls back to changes nothing — it is dead code. This mirrors the `RedundantModifier` pattern: declarations that have no effect are refused, not merely warned about. **`omit` is exempt** — it operates on structural presence rather than mutability and always changes the effective shape of the state, so it can never be redundant on the mutability axis. **`all` forms (`in <State> modify all readonly`) are also exempt** — a broadcast declaration's effective change depends on the current field population; applying redundancy checks to bulk forms would make valid declarations brittle as fields are added or removed.
5. **`omit` clears on state entry** — field value resets to default on any transition into an `omit` state (including self-transitions); does NOT apply to `no transition`.

   **D132 — RequiredFieldUnassignedOnEntry:** When a transition moves a required field (non-optional, no default value, not computed) from `omit` in the source state to non-omit in the target state, the transition action chain must include a `set` for that field. This is the structural dual of `InitialEventMissingAssignments` (D94) applied to state-crossing transitions.

   **D143 — MaterializedFieldSelfReference:** The first assignment that materializes that field on the transition cannot read the field directly or through a computed-field dependency before any value exists in the source state.
6. **`set` targeting an `omit` field in the target state** is a compile error; `readonly`/`editable` do not restrict `set`.

   **D130 — OmittedFieldReadInState:** Reading a field in a state-anchored expression context (transition guard, `in`-state ensure, `from`-state ensure, state hook guard, or action RHS) when that field is `omit` in the anchoring state is a compile error.

   **D131 — OmittedFieldSetInTargetState:** A `set` (or any write) action that targets a field `omit` in the transition's target state is a compile error.
7. **Conflicting modes** on the same (field, state) pair is a compile error.
8. **`editable` on a computed field** is a compile error (`ComputedFieldNotWritable`).
9. **`editable` on an event argument** is a compile error (`EditableOnEventArg`).

### 2.3 Type References

```
TypeRef  :=  ScalarType TypeQualifier?
          |  CollectionType
          |  ChoiceType

ScalarType  :=  ~string | string | number | integer | decimal | boolean
             |  date | time | instant | duration | period
             |  timezone | zoneddatetime | datetime
             |  money | currency | quantity | unitofmeasure
             |  dimension | price | exchangerate

CollectionType  :=  (set | queue | stack) of CollectionInnerType
                |   bag of CollectionInnerType
                |   list of CollectionInnerType
                |   log of CollectionInnerType
                |   log of CollectionInnerType by CollectionInnerType
                |   queue of CollectionInnerType by CollectionInnerType DirectionModifier?
                |   lookup of CollectionInnerType to CollectionInnerType
CollectionInnerType  :=  ScalarType TypeQualifier? ValueModifier* | ChoiceType
DirectionModifier  :=  ascending | descending
ValueModifier      :=  minlength Expr | maxlength Expr
                    |   min Expr | max Expr
                    |   nonnegative | positive | nonzero | maxplaces Expr
ChoiceType        :=  choice "of" ChoiceElementType "(" ChoiceValueExpr ("," ChoiceValueExpr)* ")" ordered?
ChoiceElementType :=  string | integer | decimal | number | boolean
ChoiceValueExpr   :=  StringLiteral | NumberLiteral | true | false
TypeQualifier   :=  (in | of | to) Expr
```

> **`~string` is not a valid `ChoiceElementType`.** The `choice` type guarantees the stored value IS the canonical declared string; `~string`'s storage-preserving model cannot be reconciled with this guarantee without new surface. Use `toLower()` normalization at the ingestion boundary before assigning to a choice field. Attempting `choice of ~string(...)` produces an `ExpectedToken` parse error — `~string` is not in the `ChoiceElementType` grammar production.

Type qualifiers narrow the value domain: `in '<unit>'` pins to a specific unit or currency, `of '<family>'` constrains to a dimension family. A field may use `in` or `of`, not both.

**Value modifiers in inner-type position.** A collection's scalar inner type may carry a trailing value-modifier list, after any type qualifier — `queue of string maxlength 200`, `set of money in 'USD' nonnegative`. The qualifier *and* the value modifiers both bind to the **inner type** (the element), not the collection field: `maxlength 200` caps each element's length, not the number of elements (`maxcount` is the separate cardinality axis). An inner-type value modifier is the per-element analogue of the same modifier on a scalar field — it desugars to a per-element rule (§2.4) and participates in proof identically: an element write-site must prove its source within the bound, and an element read (`.peek`/`.first`/`.last`) carries the bound as its value interval so it discharges into a same-or-wider-bounded destination. The modifier↔type compatibility table (§2.4) is consulted with the **element** type as subject — `set of integer maxlength 5` emits `InvalidModifierForType` exactly as `maxlength` on an integer field would. The inner-type `ValueModifier` production admits the scalar-only value modifiers — string length (`minlength`/`maxlength`), string non-emptiness (`notempty`), numeric range (`min`/`max`), numeric sign flags (`nonnegative`/`positive`/`nonzero`), and `maxplaces` — each validated, write-proven, and read-carried per element exactly as on a scalar field; a `set of money in 'USD' min '0 USD' max '100 USD'` element bound normalizes to the same UCUM/currency base units a qualified field uses, so `.min`/`.max` reads into a same-currency-and-unit destination discharge. *(The catalog-derived routed set is exactly the scalar value modifiers that apply to no collection type. `notempty` is string-only, so it routes to the element: `set of string notempty` means each element is a non-empty string, identical to `set of string minlength 1`. Collection cardinality — "the collection has at least one element" — is the separate `mincount 1`. `min`/`max` etc. are likewise scalar-only and unambiguous.)*

**`~string` in `ScalarType` position.** `~string` is valid in field declarations (`field Email as ~string`) and event argument declarations (`event Foo(Email as ~string)`). It is not valid in `ChoiceElementType` position (see note above). The parser recognizes `~string` via a new additive `Tilde`-handling path in `ParseTypeRef()` — the existing collection inner type path is unchanged. `~string` is `TypeKind.String` with `CaseInsensitive = true` on the type reference node; it is not a new `TypeKind`.

**`ChoiceType` delimiter note:** The `(...)` enclosing choice values is a type-level constraint parameter — those values define the allowed domain and are part of the type itself, not a value being assigned. This is intentionally distinct from the `[...]` list literal syntax used in `default` clauses, which is a value expression. Using `(...)` here signals type parameterization; using `[...]` would create a visual collision in compound forms like `set of choice of string(...) ... default [...]` where both delimiters would appear in the same declaration for different purposes.

**`set` disambiguation:** The lexer always emits `TokenKind.Set`. In `ParseTypeRef()`, type-position `set` is reinterpreted as parser-synthesized `TokenKind.SetType` (for example, `field Tags as set of string`). Outside type position, `TokenKind.Set` remains the action keyword. This is a `Set` + `SetType` split model, not one dual-category token.

### 2.4 Field Modifiers

Field modifiers appear after the type reference and before any computed expression.

**Constraint modifiers are shorthand for rules.** A constraint modifier — `nonnegative`, `positive`, `nonzero`, `notempty`, `min`, `max`, `minlength`, `maxlength`, `mincount`, `maxcount`, `maxplaces` — desugars to the equivalent `rule` with a generated rationale. `field Qty as number min 5` is shorthand for `field Qty as number` plus `rule Qty >= 5 because "Qty must be at least 5"`. They exist precisely to remove that boilerplate — the constraint and its self-evident reason — for the common per-field bound. Two consequences follow:

- The modifier's rationale is **generated**, which satisfies the mandatory-reason requirement (§0.1 Principle 9) without an authored `because`. A constraint modifier is not a rationale-free constraint; it is a rule whose reason the compiler supplies. An author who wants a more specific rationale simply writes the constraint as a full `rule … because "…"` instead of the modifier shorthand — the two are equivalent, so choosing the longhand is purely a choice to spell out a custom reason.
- A constraint modifier **participates in compile-time proof identically to the equivalent rule** (§5 — "field constraints, rules, and guards contribute provable numeric ranges"). `min 5` and `rule X >= 5` are interchangeable to the proof engine. Proof participation is a function of a constraint's **decidability**, not its syntactic form: a literal bound is always decidable, and a relational constraint is decidable only insofar as the referenced fields' own bounds make it so — equally true whether written as a modifier or as a `rule`.

The flag modifiers `optional`, `editable`, and `ordered` are *not* rule shorthand — they declare nullability, per-state write access, and ordinal-comparison capability respectively, none of which is a value constraint.

| Modifier | Syntax | Category |
|----------|--------|----------|
| `optional` | flag | Field is nullable; use `is set`/`is not set` for presence |
| `editable` | flag (access modifier) | Field baseline is directly editable across all states (unless overridden per-state); invalid on computed fields and event args. Same keyword stands at the per-state `in <State> modify F editable` position. |
| `ordered` | flag | Choice field supports ordinal comparison |
| `nonnegative` | flag | Value ≥ 0 |
| `positive` | flag | Value > 0 |
| `nonzero` | flag | Value ≠ 0 |
| `notempty` | flag | String is non-empty. In a collection's inner-type position it constrains each element (`set of string notempty` = each element is a non-empty string); collection cardinality is `mincount 1`. Mutually exclusive with `optional` — use `on Event ensure ... is not set or ....length > 0` for "non-empty if present". |
| `default` _Expr_ | value | Default value |
| `min` _Expr_ | value | Minimum value |
| `max` _Expr_ | value | Maximum value |
| `minlength` _Expr_ | value | Minimum string length |
| `maxlength` _Expr_ | value | Maximum string length |
| `mincount` _Expr_ | value | Minimum collection count |
| `maxcount` _Expr_ | value | Maximum collection count |
| `maxplaces` _Expr_ | value | Maximum decimal places |

### 2.5 Interpolation Reassembly

The parser reassembles interpolated literals from the segmented token stream the lexer produced. Both `ParseInterpolatedString()` and `ParseInterpolatedTypedConstant()` use the same loop:

1. Consume `Start` token → `TextSegment`
2. `ParseExpression(0)` → `ExpressionSegment`
3. If `Middle` → `TextSegment`, go to step 2
4. If `End` → `TextSegment`, done

`ParseExpression(0)` terminates naturally at `StringMiddle`/`StringEnd`/`TypedConstantMiddle`/`TypedConstantEnd` because these token kinds have no binding power in the expression parser. This is the depth-unaware reassembly property: because `}` always ends an interpolation hole and has no meaning in the expression grammar, the parser stops naturally without tracking nesting depth.

### 2.6 Error Recovery

The parser uses two complementary mechanisms:

#### Missing-node insertion

When an expected token is absent, the parser emits a diagnostic and creates a synthetic token with `IsMissing = true` and a zero-length span at the current position. The resulting AST node is structurally complete. Used for: missing identifiers, missing keywords (`as`, `because`, `ensure`), missing expression atoms.

#### Sync-point resync

When the parser is structurally lost at the top level, it scans forward for a sync token:

| Sync token | Keyword |
|------------|---------|
| `Precept` | `precept` |
| `Field` | `field` |
| `State` | `state` |
| `Event` | `event` |
| `Rule` | `rule` |
| `From` | `from` |
| `To` | `to` |
| `In` | `in` |
| `On` | `on` |

These are unambiguous top-level declaration starters. Continuation tokens (`when`, `->`, `set`, `transition`, `ensure`, `because`) are never sync points — they appear mid-production and would cause the parser to skip valid content.

### 2.7 Parser Diagnostics

| Condition | Diagnostic Code | Severity | Description |
|-----------|-----------------|----------|-------------|
| Expected token not found | `ExpectedToken` | Error | "Expected {0} here, but found '{1}'" |
| Keyword token in expression/value slot | `UnexpectedKeyword` | Error | "'{0}' is a keyword and cannot be used as a value — expected an expression here" |
| Chained comparison (`A == B == C`) | `NonAssociativeComparison` | Error | "Comparisons like == and < cannot be chained — {0}" |
| Non-callable expression followed by `(` | `InvalidCallTarget` | Error | "'{0}' is not callable — only function names and member access expressions can be called (e.g., 'foo.method(args)')" |
| `omit` access mode with `when` guard | `OmitDoesNotSupportGuard` | Error | "'omit' is an unconditional structural exclusion — 'when' guards are not allowed" |
| `when` guard placed before `on Event` on a transition row | `PreEventGuardNotAllowed` | Error | "A 'when' guard before the event target is not supported on transition rows — place the guard after 'on Event'" |
| Transition row has no outcome | `ExpectedOutcome` | Error | "Expected a transition outcome ('-> transition State', '-> no transition', or '-> reject Message') but none was found" |
| `choice of T()` with empty argument list | `EmptyChoice` | Error | "A choice type must have at least one value" |
| `choice(...)` without `of T` | `ChoiceMissingElementType` | Error | "A choice type requires an explicit element type — use 'choice of string(...)', 'choice of integer(...)', etc." |
| Literal kind does not match choice element type | `ChoiceElementTypeMismatch` | Error | "Expected a {0} literal — this choice is declared as 'choice of {0}'" |

> **Draft 2026-11-25** — under adversarial review; nothing may cite this section until the mark is removed.

`EventHandlerDoesNotSupportGuard` used to be listed here as the error for an `on Event ->` row carrying a `when` guard. It is retired: an event row may carry a guard (§2.2, *Stateless event hook*; §3A.5, *Why guards on `on <Event>` are allowed*), so the parser no longer emits it. The code is still declared in `../compiler/diagnostic-system.md`; removing it there is diagnostic-catalog work.

---

## 3. Name Binding and Type Checking

The name binder collects all declarations from the `ConstructManifest` into a `SymbolTable` and resolves all identifier references to their declarations. The type checker then validates the resolved symbols and expressions semantically, transforming the `ConstructManifest` + `SymbolTable` into a `SemanticIndex` — a flat collection of typed declarations and diagnostics. Both stages always produce a result, even on broken input (the pipeline's resilient contract).

```
ConstructManifest  →  NameBinder.Bind  →  SymbolTable
ConstructManifest + SymbolTable  →  TypeChecker.Check  →  SemanticIndex
```

The public surfaces are static methods:

```csharp
public static SymbolTable Bind(ConstructManifest manifest)
public static SemanticIndex Check(ConstructManifest manifest, SymbolTable symbols)
```

### 3.1 Processing Model

Name binding makes two passes:

1. **Declaration pass.** Walk all declarations, collect symbols: field names → types, state names, event names → arguments. Detect duplicate names. Build O(1) lookup dictionaries.
2. **Reference pass.** Walk all constructs again, resolve identifier references in expressions, slot targets, and outcomes. Emit diagnostics for undeclared references.

The type checker then makes a checking pass over the constructs:

1. **Checking pass.** Walk all declarations, resolve expressions, validate types, emit diagnostics. The TypeChecker reads from the pre-resolved `SymbolTable` — it never performs name lookup.

Declaration order does not matter. A rule at line 5 can reference a field declared at line 50. A transition row can reference states declared after it.

### 3.2 Type Widening Rules

Implicit widening is lossless and one-directional. Only two implicit widenings exist:

```
integer  →  decimal     (implicit — lossless)
integer  →  number      (implicit — lossless, direct)
```

- `integer` widens to `decimal` — every integer is exactly representable in base-10.
- `integer` widens to `number` — every integer within safe integer range is exactly representable as an IEEE 754 double. This is a direct widening, not transitive through `decimal`.
- `decimal` does **NOT** implicitly widen to `number` in any context. The conversion is lossy — IEEE 754 cannot exactly represent all base-10 values (`0.1 + 0.2 ≠ 0.3`). Use `approximate(decimalValue)` to explicitly convert `decimal → number`, or `round(numberValue, places)` to convert `number → decimal`. See §3.7 for bridge function signatures.
- No implicit narrowing. A `number` value cannot be assigned to an `integer` or `decimal` field without an explicit bridge function (`round`, `floor`, `ceil`, `truncate`).

**Widening applies in these contexts:**

| Context | Example |
|---------|---------|
| Assignment | `set IntegerField = ...` where the RHS is `integer` and the field is `decimal` |
| Binary operators | `IntegerField + DecimalField` — `integer` widens to `decimal`, result is `decimal` |
| Function arguments | `min(IntegerExpr, DecimalExpr)` — `integer` widens to `decimal` |
| Default values | `field X as decimal default 42` — `42` widens to `decimal` |
| Comparison | `IntegerField > DecimalField` — `integer` widens, comparison is valid |

For the complete conversion map — including the context-by-context matrix, bridge function catalog, and rationale for why comparisons also require bridging — see [Primitive Types · Numeric Lane Rules](primitive-types.md#numeric-lane-rules).

### 3.3 Context-Sensitive Type Resolution

Multiple literal forms produce tokens whose specific type cannot be determined at lex time. The type checker resolves these uniformly using expression context: field type, assignment target, binary operator peer, function argument position, constraint value position, default value position.

**Uniform rule:** If no context is available for any context-dependent literal, the type checker emits a diagnostic and assigns `ErrorType`. No implicit fallback.

#### Numeric literals

A `NumberLiteral` token does not carry an inherent numeric lane. Context determines the type.

| Literal form | Valid target types | Resolution rule |
|---|---|---|
| Whole number (`42`) | `integer`, `decimal`, `number` | Context determines. If target is `integer`, resolves as `integer`. If `decimal`, resolves as `decimal`. If `number`, resolves as `number`. If no context, diagnostic + `ErrorType`. |
| Fractional (`3.14`) | `decimal`, `number` | If target is `decimal`, resolves as `decimal`. If `number`, resolves as `number`. If no context, diagnostic + `ErrorType`. Type error if target is `integer`. |
| Exponent (`1.5e2`) | `number` only | Always `number`. Type error if target is `integer` or `decimal`. |

No literal suffix syntax exists — context is the sole resolution mechanism.

#### Typed constants

A `TypedConstant` token's content is opaque to the lexer. The type checker resolves the type using context-born resolution — the same model as numeric literals:

1. **Context determines the type.** The expression context (field type, operator peer, function signature, comparison operand) propagates an expected type inward. This is the same top-down inference that resolves `42` to `integer`, `decimal`, or `number`.
2. **Content is validated against the expected type.** The content is parsed and validated as a value of the context-determined type. Invalid content is a compile error.
3. **No context → compile error.** A typed constant in a position with no type expectation is a compile error.

**Content validation table** — given context-determined type, valid content patterns:

| Expected type | Valid content | Examples |
|---|---|---|
| `date` | `YYYY-MM-DD` | `'2026-04-15'` |
| `time` | `HH:MM:SS` or `HH:MM` | `'14:30:00'` |
| `instant` | ISO 8601 with `T`, trailing `Z` | `'2026-04-15T14:30:00Z'` |
| `datetime` | ISO 8601 with `T`, no zone | `'2026-04-15T14:30:00'` |
| `zoneddatetime` | ISO 8601 with `T`, `[Zone]` bracket | `'2026-04-15T14:30:00[America/New_York]'` |
| `timezone` | `Word/Word` IANA identifier | `'America/New_York'` |
| `duration` | `<integer> <temporal-unit>` (with optional `+`) | `'72 hours'` |
| `period` | `<integer> <temporal-unit>` (with optional `+`) | `'30 days'`, `'2 years + 6 months'` |
| `money` | `<number> <ISO-4217-code>` | `'100 USD'` |
| `quantity` | `<number> <unit-name>` | `'5 kg'` |
| `price` | `<number> <currency>/<unit>` | `'4.17 USD/each'` |
| `exchangerate` | `<number> <currency>/<currency>` | `'1.08 USD/EUR'` |
| `currency` | `<ISO-4217-code>` (3-letter) | `'USD'` |
| `unitofmeasure` | Unit name | `'kg'` |
| `dimension` | Dimension name (UCUM registry) | `'mass'` |

### 3.4 Name Resolution

All names are registered in the first pass. The checking pass validates every reference against the symbol tables.

| Check | Fires when | Diagnostic |
|-------|-----------|------------|
| Duplicate field name | Two `field` declarations declare the same name | `DuplicateFieldName` |
| Duplicate state name | Two state entries have the same name | `DuplicateStateName` |
| Duplicate event name | Two `event` declarations share a name | `DuplicateEventName` |
| Duplicate event arg | Two args in the same event have the same name | `DuplicateArgName` |
| Undeclared field reference | `IdentifierExpression` in expression context resolves to no field name or quantifier binding (a bare reference to an in-scope event arg is `UnqualifiedEventArgReference`, not this) | `UndeclaredField` |
| Undeclared state reference | State name in `from`/`to`/`in` target or `transition` outcome does not match a declared state | `UndeclaredState` |
| Undeclared event reference | Event name in `from ... on`, `on` ensure, or event handler does not match a declared event | `UndeclaredEvent` |
| Multiple initial states | More than one state entry has `initial` | `MultipleInitialStates` |
| No initial state | Stateful precept (has states) but none is marked `initial` | `NoInitialState` |

### 3.5 Scope Rules

Precept has a small, well-defined scope model. There are no nested scopes, no imports, no modules.

#### Global scope

Fields, states, and events are all declared at the top level. They are visible everywhere in the precept body. Order of declaration does not matter (see §3.1 — registration pass).

#### Expression scope

| Context | What's in scope |
|---------|----------------|
| Rule condition / guard | All field names |
| Ensure condition / guard | All field names |
| Transition row guard | All field names + current event's args (via `EventName.ArgName`) |
| Transition row actions (RHS of `set`, value of `add`/`enqueue`/`push`) | All field names + current event's args (via `EventName.ArgName`) |
| State action guard / actions | All field names |
| Event handler actions | All field names + current event's args (via `EventName.ArgName`) |
| Default value expression | Field names declared **before** this field (no self-reference, no forward reference) |
| Computed expression (`field X as T <- Expr`) | All field names except those that would form a dependency cycle (no self-reference, no mutual cycles) |
| Modifier value expressions (`min N`, `max N`, etc.) | All field names — a constraint modifier is rule shorthand (§2.4), so its value expression has the same scope as a rule condition: it may reference any field regardless of declaration order, including fields declared later in the precept. Self-reference is vacuous (`min X` ⟹ `X >= X`) and mutual reference is satisfiable (`A min B` + `B min A` ⟹ `A == B`), not a cycle — unlike a computed expression (row above), a constraint cannot form an evaluation cycle. |

> **Draft 2026-11-25** — under adversarial review; nothing may cite this section until the mark is removed.

The three expression scopes track the **evaluation model**, not preference. A *default value* is materialized in declaration order during construction, so it can only see fields already built above it — a forward reference is *impossible*, not merely disallowed, and a read of a field that has no value yet is refused (§3A.4, phase 0). A *computed expression* is derived from the configuration it is read in — the working copy as it stands at the read, and at commit the final configuration (§3A.4, *What a computed field returns when it is read during an operation*) — so it sees every field except those that would form a dependency cycle: an assignment cycle has no fixed point and cannot be evaluated. A *rule condition* — and a *constraint modifier*, which is rule shorthand (§2.4) — constrains the complete configuration rather than any one point in a mutation sequence, so declaration order is irrelevant and any field is in scope. That constraint is a proof obligation the compiler discharges against every operation that could affect it, not a check the runtime runs once the mutations are done. Because a constraint is a predicate rather than an assignment, it cannot form an evaluation cycle the way a computed expression can: mutual bounds simply conjoin (`A min B` + `B min A` ⟹ `A == B`), a satisfiable constraint, not a structural error.

Field names that resolve to fields declared `omit` in the anchoring state are syntactically in scope but produce `OmittedFieldReadInState` (D130) — reading a structurally absent field is a compile error.

#### Quantifier binding variable scope

`QuantifierExpr` introduces a binding variable scoped strictly to the predicate expression:

| Property | Rule |
|----------|------|
| **Type** | The collection field's inner type. If `Tags` is `set of ~string`, the binding variable is `~string`. If `ClaimQueue` is `queue of T by P`, the binding variable is a two-field projection (`.value` → `T`, `.by` → `P`). |
| **Scope** | Strictly within the `(` … `)` predicate expression. Not visible outside the quantifier. |
| **Shadowing** | If a field with the same name exists at global scope, the binding variable shadows it inside the predicate. Error: `BindingShadowsField` — rename the binding to avoid confusion. |
| **Keyword collision** | If the binding variable name is a reserved keyword, it is a parse error (`ExpectedIdentifier` with message `'{0}' is a reserved keyword and cannot be used as a binding variable`). |
| **CI inheritance** | If the collection is `set of ~string`, the binding variable is `~string`, so all `~string` enforcement rules apply inside the predicate. `each item in Tags (item == "admin")` where `Tags` is `set of ~string` triggers `CaseInsensitiveFieldRequiresTildeEquals` on `item == "admin"`. |

#### Event arg access

Event args are accessed via dotted notation: `EventName.ArgName`. The type checker resolves this by:

1. Checking if the object of a `MemberAccessExpression` is an `IdentifierExpression` that matches a declared event name.
2. If so, the member is resolved against the event's arg declarations.
3. Event arg access is only valid in contexts where an event is in scope (transition rows, event ensures, event handlers).

Dotted notation is the **only** way to reach an event arg. A bare identifier always names a field (or quantifier binding) — never an arg — so an arg whose name collides with a field is reachable only as `EventName.ArgName`, and a bare reference to an in-scope arg is rejected with `UnqualifiedEventArgReference` (PRE0163). This keeps a bare name's meaning unambiguous: it is always the field. (Referencing an arg of a *different* event than the one in scope is the distinct `EventArgOutOfScope`, PRE0050.)

### 3.6 Expression Typing Rules

#### Binary operators

**Core scalar operators:**

| Operator | Left type | Right type | Result type | Widening? |
|----------|-----------|------------|-------------|-----------|
| `+` `-` `*` `/` `%` | numeric | numeric | common numeric type | Yes — widen to common |
| `+` | `string` | `string` | `string` | No (concatenation) |
| `+` | `~string` | `~string` | `string` | No (concatenation — CI qualifier not preserved through transformation) |
| `+` | `~string` | `string` | `string` | No (concatenation — CI qualifier not preserved) |
| `+` | `string` | `~string` | `string` | No (concatenation — CI qualifier not preserved) |
| `==` `!=` | any T (not `~string`) | same T | `boolean` | Yes — `integer` widens to `decimal` or `number`; `decimal` vs `number` is a type error (see §3.2). **Exception:** `~string == ~string` and `~string == string` are compile errors — see §3.8 `~string` enforcement. Use `~=` instead. |
| `~=` `!~` | `string` or `~string` | `string` or `~string` | `boolean` | No — case-insensitive ordinal comparison (`OrdinalIgnoreCase`); type error on non-string operands |
| `~startsWith` | `~string` | `string` | `boolean` | No — CI prefix test; compile error if first arg is not `~string` |
| `~endsWith` | `~string` | `string` | `boolean` | No — CI suffix test; compile error if first arg is not `~string` |
| `<` `>` `<=` `>=` | numeric | numeric | `boolean` | Yes — `integer` widens to `decimal` or `number`; `decimal` vs `number` is a type error (see §3.2) |
| `<` `>` `<=` `>=` | `choice of T` (ordered) | `choice of T` (ordered, same element type, order-preserving subsequence) | `boolean` | No (declaration-position rank) |
| `<` `>` `<=` `>=` | `string` or `~string` | _any_ | _type error_ | String ordering is intentionally out of scope — see [Primitive Types · String Ordering — Out of Scope](primitive-types.md#string-ordering--out-of-scope) for the rationale and idiomatic substitutes (`choice of T(...) ordered`, `startsWith`, numeric/temporal types). |
| `and` `or` | `boolean` | `boolean` | `boolean` | No |

**Common numeric type:** When two numeric operands have different lanes, the result is the wider type: `integer op decimal` → `decimal`; `integer op number` → `number`. However, `decimal op number` is a **type error** — the author must use an explicit bridge function (`approximate(decimalValue)` to convert to `number`, or `round(numberValue, places)` to convert to `decimal`). There is no implicit `decimal → number` widening in any context — the conversion is lossy. See [Primitive Types · Numeric Lane Rules](primitive-types.md#numeric-lane-rules) for the complete conversion map and §3.7 for bridge function signatures.

**Temporal operators** — see the [temporal type system](temporal-type-system.md#semantic-rules) for the full per-type operator matrix. Summary:

| Left | Op | Right | Result | Notes |
|------|----|-------|--------|-------|
| `date` | `±` | `period of 'date'` | `date` | Unconstrained period → `UnqualifiedPeriodArithmetic`. |
| `date` | `-` | `date` | `period` | Calendar distance. |
| `date` | `+` | `time` | `datetime` | Composition. Commutative. |
| `time` | `±` | `period of 'time'` | `time` | Unconstrained period → `UnqualifiedPeriodArithmetic`. |
| `time` | `±` | `duration` | `time` | Sub-day bridging. Wraps at midnight. |
| `time` | `-` | `time` | `period` | |
| `instant` | `-` | `instant` | `duration` | |
| `instant` | `±` | `duration` | `instant` | |
| `datetime` | `±` | `period` | `datetime` | Accepts all period components. |
| `datetime` | `-` | `datetime` | `period` | |
| `duration` | `±` | `duration` | `duration` | |
| `duration` | `*` `/` | `integer` or `number` | `duration` | Scaling. `decimal` is a type error. Commutative for `*`. |
| `duration` | `/` | `duration` | `number` | Ratio. |
| `period` | `±` | `period` | `period` | |
| `zoneddatetime` | `±` | `duration` | `zoneddatetime` | Timeline arithmetic. |
| `zoneddatetime` | `-` | `zoneddatetime` | `duration` | |

**Temporal comparison:** `date`, `time`, `instant`, `duration`, `datetime` support all comparison operators. `timezone` and `zoneddatetime` support only `==`/`!=` — ordering operators are type errors. `period` supports `==`/`!=` always, and the ordering operators only when both operands are pinned to the same single unit (`period in 'days'` against `period in 'days'`); every other pairing is a type error — see [Decision 14](temporal-type-system.md#14-period-orders-only-when-both-operands-are-pinned-to-the-same-single-unit-revised-2026-07-26), which also records that this is not built yet. Cross-type temporal comparison is always a type error.

**Business-domain operators** — see the [business-domain types](business-domain-types.md) for the full per-type operator matrix with cancellation rules. Summary:

| Left | Op | Right | Result | Notes |
|------|----|-------|--------|-------|
| `money` | `±` | `money` | `money` | Same currency required. |
| `money` | `*` `/` | `decimal` | `money` | Commutative for `*`. `number` is a type error. |
| `money` | `/` | `money` (same curr.) | `decimal` | Dimensionless ratio. |
| `money` | `/` | `money` (diff. curr.) | `exchangerate` | |
| `money` | `/` | `quantity` / `period` / `duration` | `price` | Price derivation. |
| `quantity` | `±` | `quantity` | `quantity` | Same dimension required. |
| `quantity` | `*` `/` | `decimal` | `quantity` | Commutative for `*`. |
| `quantity` | `/` | `quantity` (same dim.) | `decimal` | |
| `quantity` | `/` | `quantity` (diff. dim.) | `quantity` (compound) | |
| `price` | `*` | `quantity` / `period` / `duration` | `money` | Dimensional cancellation. Commutative. `price * period` requires matching temporal dimension; `price * duration` requires a time-denominated price. |
| `price` | `*` `/` | `decimal` | `price` | Commutative for `*`. |
| `price` | `±` | `price` | `price` | Same currency and unit required. |
| `exchangerate` | `*` | `money` | `money` | Currency conversion. Commutative. |
| `exchangerate` | `*` `/` | `decimal` | `exchangerate` | Commutative for `*`. |

**Business-domain comparison:** `money`, `quantity`, `price` support all comparison operators. `money` requires matching currency. `quantity` and `price` require qualifier compatibility on the relevant axis: physically convertible UCUM units may compare after normalization, while explicit counting units inside the shared `count` dimension still require matching unit identity unless the author supplies an explicit conversion field. This stricter count-dimension rule is surfaced by PRE0137: dimension compatibility alone is insufficient when multiple business unit codes intentionally share `DimensionVector.None`. `samples/inventory-item.precept` is the canonical example of the intended model: many fields ride the shared count dimension through dynamic qualifiers, but any static cross-unit business comparison still requires explicit authored conversion. `exchangerate`, `currency`, `unitofmeasure`, `dimension` support only `==`/`!=` — ordering operators are type errors.

For quantity normalization, affine UCUM units with catalog-encoded `(scale, offset)` metadata are in scope, including temperature units such as `Cel`, `[degF]`, and `[degRe]`. The exclusion boundary is not "anything non-linear-looking"; it is specifically logarithmic/reference-level units such as `dB` and `[pH]`, which do not reduce to a stable affine conversion law for compile-time normalization.

#### Unary operators

| Operator | Operand type | Result type |
|----------|-------------|-------------|
| `not` | `boolean` | `boolean` |
| `-` (negate) | numeric | same numeric type |
| `-` (negate) | `duration` | `duration` |
| `-` (negate) | `period` | `period` (negates each component — preserves structural components) |
| `-` (negate) | `money` | `money` (preserves currency) |
| `-` (negate) | `quantity` | `quantity` (preserves unit/dimension) |
| `-` (negate) | `price` | `price` (preserves currency/unit) |

#### `contains`

| Collection type | Value type | Result |
|-----------------|-----------|--------|
| `set of T` | `T` (or widens to `T`) | `boolean` |
| `queue of T` | `T` | `boolean` |
| `stack of T` | `T` | `boolean` |
| `log of T` | `T` | `boolean` — value membership |
| `log of T by P` | `T` | `boolean` — value membership |
| `log of T by P` | `P` | `boolean` — key (P-type argument) membership; use this to guard `append by P` |
| `bag of T` | `T` | `boolean` — value present (count ≥ 1) |
| `list of T` | `T` | `boolean` — value membership |
| `queue of T by P` | `T` | `boolean` — value membership |
| `lookup of K to V` | `K` | `boolean` — key membership |
| non-collection | — | type error |

**CI rules for `contains`:** `set of string contains ~string` → `CaseInsensitiveValueInCaseSensitiveContains` (the CI value may not be found in the CS collection). `set of ~string contains string` (or `~string`) → case-insensitive membership via `OrdinalIgnoreCase`. Same rules apply to all collection kinds above when the inner type is `~string`.

#### `is set` / `is not set`

| Operand | Valid? | Result |
|---------|--------|--------|
| `optional` field | Yes | `boolean` |
| Non-optional field | Type error — field always has a value | — |

#### Conditional (`if ... then ... else ...`)

The `then` and `else` branches must have compatible types (same type, or one widens to the other). The result type is the common type.

**`~string` unification rules:**

| Branches | Result type | Rationale |
|---------|------------|-----------|
| `~string` + `~string` | `~string` | Both branches carry CI semantics; the result inherits them |
| `~string` + `string` | `~string` | CI preserved — `if/then/else` is selection, not transformation; when the `~string` branch wins, the result IS that field with its CI semantics intact |
| `string` + `string` | `string` | Ordinal, no CI semantics |

> **Concatenation is different.** `~string + string → string` under the `+` operator — concatenation produces a new value with no lineage claim, so the CI qualifier does not survive transformation operations. This is distinct from conditional selection, which preserves the CI qualifier.

#### Member access (`.`)

**Collection and core accessors:**

| Object type | Member | Result type | Proof |
|-------------|--------|-------------|-------|
| `set of T` | `count` | `integer` | |
| `set of T` (T orderable) | `min` | `T` | `count > 0` |
| `set of T` (T orderable) | `max` | `T` | `count > 0` |
| `queue of T` | `count` | `integer` | |
| `queue of T` | `peek` | `T` | `count > 0` |
| `queue of T by P` | `count` | `integer` | |
| `queue of T by P` | `peek` | `T` | `count > 0` |
| `queue of T by P` | `peekby` | `P` | `count > 0` |
| `stack of T` | `count` | `integer` | |
| `stack of T` | `peek` | `T` | `count > 0` |
| `bag of T` | `count` | `integer` | |
| `bag of T` | `countof(E)` | `integer` | returns how many times `E` appears in the bag (v3) |
| `list of T` | `count` | `integer` | |
| `list of T` | `first` | `T` | `count > 0` |
| `list of T` | `last` | `T` | `count > 0` |
| `list of T` | `at(N)` | `T` | `count > 0` |
| `log of T` | `count` | `integer` | |
| `log of T` | `first` | `T` | `count > 0` |
| `log of T` | `last` | `T` | `count > 0` |
| `log of T` | `at(N)` | `T` | `count > 0` |
| `log of T by P` | `count` | `integer` | |
| `log of T by P` | `first` | `T` | `count > 0` |
| `log of T by P` | `last` | `T` | `count > 0` |
| `log of T by P` | `at(N)` | `T` | `count > 0` |
| `lookup of K to V` | `count` | `integer` | |
| `string` | `length` | `integer` | |
| Event arg reference (`EventName.ArgName`) | — | arg's declared type | |

> **Lookup field access:** To retrieve the value for key `K` from a `lookup of K to V` field `F`, use the infix `for` operator: `F for K` — result type is `V`. This is an expression-level operator (precedence 40), not a dot-member accessor.

**Temporal accessors** — see the [temporal type system](temporal-type-system.md) for the full per-type accessor tables. Summary: `date` has `.year`, `.month`, `.day`, `.dayOfWeek` → `integer`. `time` has `.hour`, `.minute`, `.second` → `integer`. `instant` has only `.inZone(tz)` → `zoneddatetime` (no skip-level accessors). `duration` has `.totalDays`, `.totalHours`, `.totalMinutes`, `.totalSeconds` → `number`. `period` has `.years`, `.months`, `.weeks`, `.days`, `.hours`, `.minutes`, `.seconds` → `integer`; `.hasDateComponent`, `.hasTimeComponent` → `boolean`; `.basis` → `string`; `.dimension` → `dimension`. `zoneddatetime` has `.instant`, `.timezone`, `.datetime`, `.date`, `.time` and integer component accessors. `datetime` has `.date`, `.time`, `.inZone(tz)`, and integer component accessors.

**Business-domain accessors** — see the [business-domain types](business-domain-types.md#accessors-per-type) for the full accessor table. Summary: `money` has `.amount` → `decimal`, `.currency` → `currency`. `quantity` has `.amount` → `decimal`, `.unit` → `unitofmeasure`, `.dimension` → `dimension`. `price` has `.amount` → `decimal`, `.currency` → `currency`, `.unit` → `unitofmeasure`, `.dimension` → `dimension`. `exchangerate` has `.amount` → `decimal`, `.from`/`.to` → `currency`. `unitofmeasure` has `.dimension` → `dimension`. `period` also has `.basis` → `string` and `.dimension` → `dimension` for its `in`/`of` qualification system.

| _other_ | — | `InvalidMemberAccess` diagnostic |

#### Function calls

See §3.7 for the complete built-in function catalog.

#### Parenthesized expressions

Type is the type of the inner expression. Transparent.

#### String interpolation

Each `{expr}` inside `"..."` is type-checked independently. Any scalar type is coercible to string. Collections are a type error inside string interpolation. **An interpolation hole is a read:** an `optional` field read at a hole enrolls a presence obligation exactly as it would in any value position, so an unguarded optional at a hole is refused — the author guards it (`when … is set`) or supplies fallback text with the `if … then … else …` conditional. This rule does not vary by string position: it is identical in a `because`/`reject` message and in a `set` right-hand side. A field appearing only as the operand of a presence test (`{Opt is set}`) is *named, not read*, and enrolls nothing.

#### Typed constant interpolation

Each `{expr}` inside `'...'` is type-checked independently. After interpolation expressions are typed, the full content is validated against the context-determined type as described in §3.3.

### 3.7 Built-in Function Catalog

Functions are validated against a closed catalog. There are no user-defined functions, no registration mechanism, no extension point.

| Function | Signature | Return type | Constraints |
|----------|-----------|-------------|-------------|
| `min(a, b)` | `(integer\|decimal\|number, integer\|decimal\|number) → common numeric type` | Common numeric type of args | Primitive numeric widening only |
|  | `(money, money) → money` | `money` | Both args must share the same currency qualifier; result preserves it |
|  | `(quantity, quantity) → quantity` | `quantity` | Both args must share the same unit qualifier; result preserves it |
| `max(a, b)` | `(integer\|decimal\|number, integer\|decimal\|number) → common numeric type` | Common numeric type of args | Primitive numeric widening only |
|  | `(money, money) → money` | `money` | Both args must share the same currency qualifier; result preserves it |
|  | `(quantity, quantity) → quantity` | `quantity` | Both args must share the same unit qualifier; result preserves it |
| `abs(value)` | `(integer\|decimal\|number) → same type` | Same primitive numeric type as input | Primitive numeric lane only |
|  | `(money) → money` | `money` | Result preserves the input currency qualifier |
|  | `(quantity) → quantity` | `quantity` | Result preserves the input unit qualifier |
| `clamp(value, lo, hi)` | `(integer\|decimal\|number, integer\|decimal\|number, integer\|decimal\|number) → common numeric type` | Common numeric type | Primitive numeric widening only |
|  | `(money, money, money) → money` | `money` | `value`, `lo`, and `hi` must share the same currency qualifier; result preserves it |
|  | `(quantity, quantity, quantity) → quantity` | `quantity` | `value`, `lo`, and `hi` must share the same unit qualifier; result preserves it |
| `floor(value)` | `(decimal\|number) → integer` | `integer` | — |
| `ceil(value)` | `(decimal\|number) → integer` | `integer` | — |
| `truncate(value)` | `(decimal\|number) → integer` | `integer` | — |
| `round(value)` | `(decimal\|number) → integer` | `integer` | Banker's rounding |
| `round(value, places)` | `(integer\|decimal\|number, integer) → decimal` | `decimal` | `places` must be non-negative integer; **explicit bridge: number→decimal** within the primitive numeric lanes |
|  | `(money, integer) → money` | `money` | `places` must be non-negative integer; result preserves the input currency qualifier |
|  | `(quantity, integer) → quantity` | `quantity` | `places` must be non-negative integer; result preserves the input unit qualifier |
| `approximate(value)` | `(decimal) → number` | `number` | **Explicit bridge: decimal→number**; makes precision loss visible |
| `pow(base, exp)` | `(numeric, integer) → numeric` | Same numeric type as `base` | `exp` must be non-negative for integer lane |
| `sqrt(value)` | `(number) → number` | `number` | Number-lane only; `decimal` inputs are a type error (no .NET `Math.Sqrt` overload for `decimal`; use `approximate(value)` to convert first); `integer` widens losslessly to `number`. Proof engine checks non-negativity. |
| `trim(value)` | `(string) → string` | `string` | — |
| `startsWith(s, prefix)` | `(string, string) → boolean` | `boolean` | Case-sensitive. Compile error when first arg is `~string` — use `~startsWith` instead. See `CaseInsensitiveFieldRequiresTildeStartsWith`. |
| `endsWith(s, suffix)` | `(string, string) → boolean` | `boolean` | Case-sensitive. Compile error when first arg is `~string` — use `~endsWith` instead. See `CaseInsensitiveFieldRequiresTildeEndsWith`. |
| `~startsWith(s, prefix)` | `(~string, string) → boolean` | `boolean` | CI prefix test using `OrdinalIgnoreCase`. First arg must be `~string`; compile error otherwise. |
| `~endsWith(s, suffix)` | `(~string, string) → boolean` | `boolean` | CI suffix test using `OrdinalIgnoreCase`. First arg must be `~string`; compile error otherwise. |
| `toLower(s)` | `(string) → string` | `string` | Lowercase (invariant culture) |
| `toUpper(s)` | `(string) → string` | `string` | Uppercase (invariant culture) |
| `left(s, n)` | `(string, integer) → string` | `string` | Leftmost N code units (clamped to string length) |
| `right(s, n)` | `(string, integer) → string` | `string` | Rightmost N code units (clamped to string length) |
| `mid(s, start, length)` | `(string, integer, integer) → string` | `string` | 1-indexed substring (clamped); `start` and `length` must be positive `integer` |
| `now()` | `() → instant` | `instant` | — |

> **CI functions and the catalog.** `~startsWith` and `~endsWith` have dedicated `FunctionKind` catalog entries (`TildeStartsWith`, `TildeEndsWith`) and are syntactically distinct from regular function calls. The leading `~` token is the null-denotation leader for the `CIFunctionCallExpression` expression form (ExpressionForms catalog). Completions, hover, and MCP vocabulary derive from `FunctionMeta` entries — not from the `HasCIVariant` flag on the base functions.

> **`~string` argument compatibility for string functions.** Functions accepting `(string)` parameters — `trim`, `toLower`, `toUpper`, `left`, `right`, `mid` — also accept `~string` arguments via the bidirectional assignment compatibility rule (§3.8). No enforcement diagnostic is emitted for these functions. CI semantics do not apply to structural operations — these functions operate on the stored value regardless of the `~` qualifier.

> **Primitive numeric shorthand in this table.** The primitive numeric lane consists of `integer`, `decimal`, and `number`, with the widening rules from §3.4. `money` and `quantity` are domain types, not members of that widening family: they use their own overload rows above, do not widen to primitive numerics or to each other, and preserve their qualifier in the result. Where multiple `money` or `quantity` arguments appear in one overload, those arguments must share the same qualifier.

**Lane bridge functions.** Within the primitive numeric lanes (`integer`, `decimal`, `number`), two functions are the sole explicit bridges: `approximate(decimal) → number` and `round(value, places)` on primitive numeric inputs, which returns `decimal`. For `money` and `quantity`, `round(value, places)` is not a bridge — it rounds while preserving the domain type and qualifier. The rounding family (`floor`, `ceil`, `truncate`, `round` with no places) provide `decimal|number → integer`. No other mechanism crosses primitive numeric lane boundaries — `decimal * NumberField` without `approximate()` is a type error (see type-checker.md §4.2a).

**Function validation checks:**

| Check | Fires when | Diagnostic |
|-------|-----------|------------|
| Unknown function name | `foo(...)` where `foo` is not in the catalog | `UndeclaredFunction` |
| Wrong arity | `min(a)` or `min(a, b, c)` | `FunctionArityMismatch` |
| Arg type mismatch | `min("a", "b")` — strings to numeric function | `TypeMismatch` |
| Arg constraint violation | `round(x, -1)` — negative places | `FunctionArgConstraintViolation` |

### 3.8 Semantic Checks

#### Type compatibility

| Check | Fires when | Diagnostic |
|-------|-----------|------------|
| Assignment type mismatch | `set Field = Expr` where `Expr`'s type is not assignable to `Field`'s type (after widening) | `TypeMismatch` |
| Guard not boolean | `when Expr` where `Expr`'s type is not `boolean` | `TypeMismatch` |
| Rule condition not boolean | `rule Expr` where `Expr`'s type is not `boolean` | `TypeMismatch` |
| Ensure condition not boolean | `ensure Expr` where `Expr`'s type is not `boolean` | `TypeMismatch` |
| Message not string | `because Expr` or `reject Expr` where `Expr` is not `string` | `TypeMismatch` |
| Binary operator type error | Operator applied to incompatible types (e.g., `string + boolean`) | `TypeMismatch` |
| Comparison on unordered choice | `<` / `>` / `<=` / `>=` on a `choice` field without the `ordered` modifier | `TypeMismatch` |
| Conditional branch mismatch | `if ... then A else B` where A and B have no common type | `TypeMismatch` |
| Default value type mismatch | `default Expr` where `Expr`'s type is incompatible with the field type | `TypeMismatch` |
| Collection element type mismatch | `add Field Expr` where `Expr`'s type doesn't match the collection's element type | `TypeMismatch` |
| Numeric literal incompatible | Fractional literal in `integer` context, or exponent literal in `integer`/`decimal` context | `TypeMismatch` |

Operator result typing is catalog-derived: fixed-result operators declare `boolean` directly, `for` derives from the left operand's element/value type, unary `-` preserves its operand type, and binary arithmetic derives from the resolved typed operation.

#### `~string` enforcement

All three rules below ship together — the enforcement model is not separable. Checks fire at the comparison or call site, not at the declaration site. The CI flag is carried per field reference in the semantic index.

| Context | Condition | Diagnostic |
|---------|-----------|------------|
| Binary equality (`==`) | Either operand is `~string` | `CaseInsensitiveFieldRequiresTildeEquals` |
| Binary inequality (`!=`) | Either operand is `~string` | `CaseInsensitiveFieldRequiresTildeNotEquals` |
| Collection `contains` | Collection is case-sensitive (`set`/`list`/`log`/`queue`/`stack`/`bag` of `string`) and value is `~string` | `CaseInsensitiveValueInCaseSensitiveContains` |
| `startsWith(s, ...)` call | First arg `s` resolves to `~string` | `CaseInsensitiveFieldRequiresTildeStartsWith` |
| `endsWith(s, ...)` call | First arg `s` resolves to `~string` | `CaseInsensitiveFieldRequiresTildeEndsWith` |

Enforcement fires in all expression positions where `==`/`!=`/`contains`/`startsWith`/`endsWith` appear, including `when` guards, `rule` expressions, `ensure` expressions, and quantifier predicates. The enforcement checks both operand positions for binary operators (e.g., `"admin@example.com" == Email` where `Email` is `~string` also fires).

`string` and `~string` are fully assignment-compatible in both directions. The enforcement is comparison-site only, never assignment-site.

#### Quantifier predicate validation

| Check | Fires when | Diagnostic |
|-------|-----------|------------|
| Predicate must be boolean | `BoolExpr` in quantifier resolves to non-boolean type | `QuantifierPredicateNotBoolean` |
| Collection must be a collection field | `CollectionRef` in quantifier resolves to a non-collection type | `InvalidQuantifierTarget` |
| Binding variable is reserved keyword | Binding variable name is in the reserved keyword set | `ExpectedIdentifier` |
| `~string` enforcement applies inside predicate | Binding variable is `~string` (collection inner type), used with `==`/`!=`/`startsWith`/`endsWith` | Existing enforcement diagnostics (`CaseInsensitiveFieldRequiresTildeEquals`, etc.) |

#### Modifier validation

Modifiers are constraints on field/arg values. The type checker validates applicability:

| Modifier | Applicable to | Error when applied to |
|----------|---------------|----------------------|
| `editable` (access modifier) | any non-computed field type at field-declaration site AND per-state `modify F editable` site | computed fields (`ComputedFieldNotWritable`); event arguments (`EditableOnEventArg`) |
| `nonnegative` | `integer`, `decimal`, `number`, `money`, `quantity`, `price`, `exchangerate`, `duration`, `period` | `string`, `boolean`, `choice`, collections, non-magnitude temporals (`date`/`time`/`instant`/`datetime`) |
| `positive` | (same as `nonnegative`) | (same as above) |
| `nonzero` | (same as `nonnegative`) | (same as above) |
| `notempty` | `string` (incl. per-element in a collection's inner-type position) | `integer`, `decimal`, `number`, `boolean`, `choice` |
| `min` / `max` | `integer`, `decimal`, `number`, `money`, `quantity`, `price` | `string`, `boolean`, `choice`, collections, `exchangerate`, temporals |
| `minlength` / `maxlength` | `string` | `number`, `integer`, `decimal`, `boolean`, collections |
| `mincount` / `maxcount` | `set`, `queue`, `stack`, `log`, `log of T by P`, `bag`, `list`, `queue of T by P`, `lookup of K to V` | scalars |
| `maxplaces` | `decimal`, `money`, `quantity`, `price`, `exchangerate` | `integer`, `number`, `string`, `boolean`, `choice`, collections |
| `ordered` | `choice` | all non-choice types |
| `optional` | any field type | — (always valid) |

> **Per-element consultation (collection inner types).** When a value modifier sits in collection inner-type position (`queue of string maxlength 200` — §2.3), this same table is consulted with the **element** scalar type as subject. `set of integer maxlength 5` emits `InvalidModifierForType` because `maxlength` does not apply to `integer`, exactly as it would on an `integer` field. No second compatibility axis exists — the element type is a scalar type, and a scalar type's modifier set is already defined here.

> **Collection non-emptiness — `mincount 1`:** "The collection has at least one element" is `mincount N` with `N ≥ 1` (canonically `mincount 1`). A statically-literal `mincount N` (`N ≥ 1`) statically discharges `.min`/`.max`/`.peek`/`.peekby`/`.first`/`.last` access obligations on the kinds that surface those accessors — no per-access `.count > 0` guard is needed. (`.at(N)` is **not** discharged — it needs an index-bounds guard `N >= 0 and N < count`, which `count ≥ 1` does not provide.) (`notempty` is string-only and does not name collection cardinality; in a collection's inner-type position it constrains each element — `set of string notempty` = each element is a non-empty string.)

> **`notempty` + `optional` are mutually exclusive:** A field or event argument cannot be both `optional` and `notempty`. `optional` means the value may be absent; `notempty` asserts the value has content. These constraints cannot both hold — the compiler emits `ConflictingModifiers` (C120) when both appear on the same declaration. To express "non-empty if present" semantics, use: `on Event ensure Arg is not set or Arg.length > 0 because "..."`. 

**Modifier value validation:**

| Check | Fires when | Diagnostic |
|-------|-----------|------------|
| `min` > `max` | `min` value exceeds `max` value on the same field | `InvalidModifierBounds` |
| `minlength` > `maxlength` | `minlength` exceeds `maxlength` | `InvalidModifierBounds` |
| `mincount` > `maxcount` | `mincount` exceeds `maxcount` | `InvalidModifierBounds` |
| Negative count/length/places | `minlength`/`maxlength`/`mincount`/`maxcount`/`maxplaces` is negative | `InvalidModifierValue` |
| `maxplaces` not integer | Decimal places must be a whole number | `InvalidModifierValue` |
| Duplicate modifier | Same modifier applied twice to one field | `DuplicateModifier` |
| Redundant modifier | `nonnegative` and `positive` on the same field (`positive` subsumes `nonnegative`) | `RedundantModifier` (warning) |
| Conflicting modifiers | `optional` and `notempty` on the same field or event arg | `ConflictingModifiers` (C120) |

#### Action statement validation

> **Draft 2026-11-25** — under adversarial review; nothing may cite this section until the mark is removed.

| Action | Field type required | Value type required | Additional checks |
|--------|--------------------|--------------------|-------------------|
| `set F = Expr` | Any scalar | Assignable to field type | Field must not be computed |
| `add F Expr` | `set of T`, `bag of T` | `T` | — |
| `remove F Expr` | `set of T`, `bag of T`, `list of T`, `lookup of K to V` | `T` (set/bag/list), `K` (lookup) | For lookup, removes the entry with key `Expr`; see `remove F at N` for list indexed removal |
| `remove F at N` | `list of T` | — | `N` is a zero-based `integer` index; removes the element at that position (v3) |
| `enqueue F Expr` | `queue of T` | `T` | — |
| `enqueue F Expr by Expr` | `queue of T by P` | `T`, then `P` | Explicit ordering key required; simple `enqueue F Expr` is not valid on `queue of T by P` (v3) |
| `dequeue F (into G (by H)?)?` | `queue of T`, `queue of T by P` | — | If `into G`, `G` must be type `T` and receives the removed element. If `by H` (only after `into`, only on `queue of T by P`), `H` must be type `P` and receives the removed element's ordering value; the element removed is always the front one, and `H` does not select it ([`collection-types.md`](collection-types.md) § Dequeue with Ordering Value Capture). `G` and `H` are write targets like the target of `set` (§3A.4). Requires emptiness proof (`UnguardedCollectionMutation`) (v3) |
| `push F Expr` | `stack of T` | `T` | — |
| `pop F (into G)?` | `stack of T` | — | If `into G`, `G` must be type `T` and receives the removed element; `G` is a write target like the target of `set` (§3A.4). Requires emptiness proof (`UnguardedCollectionMutation`) |
| `clear F` | `set of T`, `queue of T`, `stack of T`, `bag of T`, `list of T`, `queue of T by P`, `lookup of K to V`; any `optional` field | — | On `optional` fields, resets the field to "not set" (see §1.2). On `lookup of K to V`, drops all key-value entries. Not valid on `log of T` or `log of T by P` (append-only chronicle). |
| `append F Expr` | `log of T`, `list of T` | `T` | Appends to the end of the log or list (v3) |
| `append F Expr by Expr` | `log of T by P` | `T`, then `P` | Explicit ordering key for log-by append (v3) |
| `insert F Expr at N` | `list of T` | `T`, then `integer` | `N` is a zero-based index; inserts before element at position `N` (v3) |
| `put F K = V` | `lookup of K to V` | `K`, then `V` | Upserts the entry with key `K`; inserts if absent, replaces if present (v3) |

Type errors: applying a set operation to a non-set field, a queue operation to a non-queue field, etc.

#### Access mode validation

| Check | Fires when | Diagnostic |
|-------|-----------|------------|
| Field not declared | Access mode names a field that doesn't exist | `UndeclaredField` |
| State not declared | Access mode scoped to a state that doesn't exist | `UndeclaredState` |
| Computed field in editable mode | A computed field is listed in a `modify ... editable` access mode declaration | `ComputedFieldNotWritable` |
| `editable` on computed field | A computed field carries the `editable` modifier | `ComputedFieldNotWritable` |
| `editable` on event arg | An event argument carries the `editable` modifier | `EditableOnEventArg` |
| Conflicting access modes | Same field has both `modify` and `omit` in the same state | `ConflictingAccessModes` |
| Redundant access mode (unguarded) | `in <State> modify F editable` where `F` is `editable` (baseline already editable), or `in <State> modify F readonly` where `F` is not `editable` (baseline already read-only); named-field forms only | `RedundantAccessMode` (error) |
| Redundant access mode (guarded) | `in <State> when Guard modify F readonly` where `F` is not `editable` — guard-true branch = read-only, guard-false branch = read-only (D3 baseline); the guard changes nothing | `RedundantAccessMode` (error) |

#### Computed field validation

| Check | Fires when | Diagnostic |
|-------|-----------|------------|
| Self-reference | Computed expression references its own field | `CircularComputedField` |
| Transitive cycle | Computed fields form a dependency cycle (A→B→A, or A→B→C→A, etc.) | `CircularComputedField` |
| Expression type mismatch | Computed expression type doesn't match field type | `TypeMismatch` |
| Computed with default | Field has both `<-` and `default` | `ComputedFieldWithDefault` |
| Computed as write target | `set` action targets a computed field | `ComputedFieldNotWritable` |

#### Choice type validation

| Check | Fires when | Diagnostic |
|-------|-----------|------------|
| Missing element type | `choice("a", "b")` — no `of T` | `ChoiceMissingElementType` |
| Empty choice | `choice of string()` — no values | `EmptyChoice` |
| Duplicate choice value | `choice of string("a", "a")` | `DuplicateChoiceValue` |
| Wrong literal kind | `choice of integer("not-a-number")` | `ChoiceElementTypeMismatch` |
| Non-choice assigned to choice | `set Priority = someStringVar` | `NonChoiceAssignedToChoice` |
| Choice literal not in set | `set Priority = "Unknown"` where `"Unknown"` not declared | `ChoiceLiteralNotInSet` |
| Choice arg outside field set | Arg `choice("Low")` supplied to `choice("Low","Med","High")` field — values outside | `ChoiceArgOutsideFieldSet` |
| Element type mismatch | `choice of integer` arg to `choice of string` field | `ChoiceElementTypeMismatch` |
| Rank conflict | `choice("Med","Low")` arg to `choice("Low","Med","High")` field — order not preserved | `ChoiceRankConflict` |

**v1 notes:** Negative numeric literals (e.g., `choice of integer(-1, 0, 1)`) are supported via parser constant-folding. Typed choice nested inside a collection element type (e.g., `set of choice of string("Low","High") ordered`) is supported; the `ordered` modifier on the inner choice propagates through accessors (`.min`, `.max`, `.first`, `.last`) so ordinal proofs discharge cleanly.

#### List literal validation

| Check | Fires when | Diagnostic |
|-------|-----------|------------|
| Element type mismatch | List element type doesn't match collection element type | `TypeMismatch` |
| List in non-default position | List literal used outside a `default` clause | `ListLiteralOutsideDefault` |
| Empty list as default | Valid — empty collection | — |

#### Transition outcome validation

| Check | Fires when | Diagnostic |
|-------|-----------|------------|
| Undeclared target state | `transition StateName` where `StateName` is not declared | `UndeclaredState` |
| Reject message not string | `reject Expr` where `Expr` is not string | `TypeMismatch` |

#### Stateless/stateful cross-validation

A precept that contains both `EventHandlerDeclaration` nodes (`on Event -> actions`) and any `state` declarations is an error (`PRE0092`). In a stateful precept, event handlers are redundant with `from any on Event -> no transition` followed by rules. Mixing the two creates ambiguity about execution order.

This restriction is deliberate. `from any on Event -> no transition` already expresses the state-agnostic handling case in a stateful precept, so a bare handler would add only redundant surface. Allowing both forms would also invite pseudo-lifecycle designs where field mutation bypasses explicit state topology and weakens the lifecycle guarantee.

**Construction row exemption:** Construction rows (`on <InitialEvent>`) share the `EventHandlerDeclaration` parse form but are exempt from `PRE0092`. The type checker identifies them as construction rows (via `resolvedEvent.IsInitial`) before the stateless-handler rule fires. A construction row in a stateful precept is correct — the entity does not yet exist in any state, so the `from`-less form is semantically honest rather than a stateless-handler violation.

A stateless precept (no states, no `from`, no transitions) that uses only event hooks is valid.

### 3.9 Error Recovery

#### ErrorType propagation

When the type checker encounters an unresolvable expression (missing node, undeclared name, type error in a sub-expression), it assigns `ErrorType`. `ErrorType` is compatible with every other type for the purpose of further checking — it suppresses all downstream type errors that would cascade from the original failure.

**Rules:**

1. Any operation involving `ErrorType` produces `ErrorType`.
2. `ErrorType` satisfies any type constraint — no further diagnostics are emitted for expressions that already carry `ErrorType`.
3. `ErrorType` never appears in a valid program. It only exists in the presence of other diagnostics.

#### Handling `IsMissing` AST nodes

| Node category | Recovery behavior |
|---|---|
| Declaration with `IsMissing` name | Skip — do not add to symbol table. Parser already emitted a diagnostic. |
| Expression with `IsMissing` | Assign `ErrorType`. No diagnostic emitted (parser already reported it). |
| TypeRef with `IsMissing` | Resolve to `ErrorType`. Fields with error types still appear in the symbol table but their type is `ErrorType`. |
| Guard with `IsMissing` subexpression | Guard is assigned `ErrorType`. The transition row is still processed — other checks continue. |
| Missing state/event name tokens | Skip the containing declaration. |

#### One diagnostic per root cause

The type checker emits diagnostics for root causes only. When `ErrorType` is flowing through an expression tree, the type checker stays silent. The first diagnostic emitted for a given expression chain is the root cause; all subsequent type mismatches involving `ErrorType` are symptoms.

### 3.10 Diagnostic Catalog

**Canonical sources:**
- [`src/Precept/Language/DiagnosticCode.cs`](../../src/Precept/Language/DiagnosticCode.cs) — enum; ordinals are stable across versions
- [`src/Precept/Language/Diagnostics.cs`](../../src/Precept/Language/Diagnostics.cs) — exhaustive `GetMeta` switch; message templates, severities, stages, fix hints, related codes, fault-prevention links

Do not duplicate code-level data here — the catalog is the source of truth. For schema, design rationale, and the `FaultCode → DiagnosticCode` chain, see [`docs/compiler/diagnostic-system.md`](../compiler/diagnostic-system.md).

#### Diagnostic groups

Codes are grouped by semantic domain. Parse-stage codes (9–16) are also covered in §2.7.

| Group | Stage | Ordinals | Subsystem doc |
|---|---|---|---|
| Lex errors | Lex | 1–8 | — |
| Parse errors | Parse | 9–16 | §2.7 |
| Symbol resolution | Type | 17–30 | — |
| State machine | Type | 31–32 | — |
| Modifier validity | Type | 33–37 | — |
| Computed fields | Type | 38–40 | — |
| Access modes | Type | 41–43 | — |
| Typed constants & choice syntax | Type / Parse | 44–46, 52–54, 85–91 | — |
| Lifecycle validation | Type | 92–94 | — |
| Temporal types | Type | 55–62 | [temporal-type-system.md](temporal-type-system.md) |
| Collection safety | Type | 63–65, 99–106 | [collection-types.md](collection-types.md) |
| Case-insensitive string | Type | 66, 95–98 | — |
| Business-domain types | Type | 67–77 | [business-domain-types.md](business-domain-types.md) |
| Value safety | Type | 78–79 | — |
| Graph analysis | Graph | 80–81 | — |
| Proof engine | Proof | 82–84 | — |

#### Design notes

**Choice type parse-stage codes.** `EmptyChoice` (46), `ChoiceMissingElementType` (90), and `ChoiceElementTypeMismatch` (88) carry ordinals in the choice group but are `DiagnosticStage.Parse` — they are detected during `ParseTypeRef()` before type-checker context exists. See §2.7 for the full parse-stage diagnostic table.

**Code 66 — `~string` operator reassignment.** `CaseInsensitiveStringOnNonCollection` (ordinal 66) was reserved in anticipation of scalar `~string` but was never emitted by the parser. When scalar `~string` ships, ordinal 66 is **reassigned** to `CaseInsensitiveFieldRequiresTildeEquals`. The numeric value is retained; no ordinals shift. Existing source references to `DiagnosticCode.CaseInsensitiveStringOnNonCollection` will not compile — update them to `DiagnosticCode.CaseInsensitiveFieldRequiresTildeEquals`.
---

## 3A. Language Semantics

This section defines the semantic model that sits between mechanical type checking (§3) and structural graph analysis (§4). It specifies the meaning of constraint surfaces, outcome verdicts, mutation guarantees, entity construction, and inspection — the concepts that give the language its governance identity beyond syntax and types.

### 3A.1 Constraint Semantics

The language distinguishes categories of truth.

#### Rules

`rule` expresses global data truth — constraints that hold after every mutation.

Rules support optional guards:

```precept
rule Score >= 680 because "Credit score too low"
rule DownPayment >= RequestedAmount * 0.20 when LoanType == "conventional" because "Conventional loans require 20% down"
```

A guarded rule applies only when its guard is true. This is conditional constraint scoping — the rule is precise about where the domain says it should apply — not a weakening of the guarantee. The engine ensures the rule holds in every configuration where its guard is satisfied.

Rules operate in field scope. They cannot reference event arguments — this ensures reusability across all events.

#### Ensures

`ensure` expresses contextual truth — constraints scoped to a specific state or event.

Ensures also support optional guards:

```precept
in Review ensure Reviewers.count >= 2 because "Review requires at least two reviewers"
in Open when Escalated ensure Priority >= 3 because "Escalated tickets must be high priority"
on Submit when Submit.Type == "payment" ensure Submit.Amount > 0 because "Payment amounts must be positive"
```

The surface includes these anchors:

1. `in <State> ensure ...` — residency truth. The constraint holds for every operation while the entity is in this state.
2. `to <State> ensure ...` — entry truth. The constraint is checked on any transition into this state.
3. `from <State> ensure ...` — exit truth. The constraint is checked on any transition out of this state.
4. `on <Event> ensure ...` — event-argument truth. The constraint validates the caller-provided event arguments.

#### Rejections

`reject` is authored prohibition, not failed data truth. This is a designed prohibition in the definition, not a constraint violation — it means the author deliberately forbade this outcome.

The distinction between rejection and constraint failure is semantically significant. A rejection says "the definition explicitly disallows this"; a constraint failure says "the data would violate a declared truth." They require different responses from callers and different diagnostic framing.

#### Guards

`when` guards are routing logic, not constraints. They do not produce violations. A guard that evaluates to false simply means the row does not match — the runtime moves to the next row. Only the row that actually fires (or an explicit `reject` fallback) produces outcomes. This is a fundamental language semantic: guards select; constraints enforce.

#### Collect-all vs first-match

> **Draft 2026-11-25** — under adversarial review; nothing may cite this section until the mark is removed.

The language makes a semantic distinction between:

1. **Validation surfaces,** which are collect-all. Validation runs at ingress, on the values an operation brings in from outside — event arguments (at construction, the initial event's arguments, which are the only input `Create` takes — §3A.4, *The operations*), and the field values in an update patch. Every check that can be made on those values is made, and every failure found is reported: the caller receives the complete set of failures, not just the first one encountered. A constraint over a value that failed an earlier check, or whose proof relied on another constraint of the same check that failed, is not evaluated (§3A.4 phase 1, *What collect-all does not evaluate*). Rules and ensures are not re-evaluated after the mutations complete; the compiler proved every handler preserves them (§0.7), so there is nothing left for the runtime to re-check. Whether the check on a direct edit of an `editable` field also covers every rule that mentions the field is an outstanding owner decision; see [Open Questions](#open-questions--implementation-notes).
2. **Routing surfaces,** which are first-match and order-sensitive. Transition rows are evaluated in declaration order — the first matching guard wins, and remaining rows are not evaluated.

This distinction is a language design choice, not an optimization. Validation must be exhaustive because partial feedback is useless to callers — reporting only the first violation forces trial-and-error correction. Routing must be first-match because transition rows are authored with priority ordering — evaluating all rows would create ambiguity about which outcome applies.

### 3A.2 Outcomes and Semantic Verdicts

The language defines a stable semantic verdict space. Every operation that can mutate an entity produces one of a fixed set of outcomes. These outcome types are semantically distinct — not just diagnostic convenience — and callers must be able to discriminate between them.

The outcome types are:

> **Draft 2026-11-25** — under adversarial review; nothing may cite this section until the mark is removed.

1. **Successful transition.** Event fired; state changed; entity updated.
2. **Successful no-transition event.** Event fired; in-place mutations committed; no state change. This is a deliberate design allowing in-place data changes to be event-driven without triggering entry/exit actions.
3. **Explicit rejection.** An authored `reject` row matched — a designed prohibition, not a data constraint failure.
4. **Ingress validation failure.** A value entering from outside — an event argument (at construction, an argument of the initial event), or a field value in an update patch — violates a constraint declared on the slot it enters. Refused before any mutation runs, so nothing is rolled back. There is no outcome for a rule violated *after* mutation: the compiler proved that cannot happen (§0.7).
5. **Unmatched routed event.** Transition rows exist for the event but all guards failed — an instance data condition.
6. **Undefined event.** `Fire` was given a name the post-construction event space does not contain, or an event with no rows to dispatch in the current state. Three causes, each stated in §3A.4 phases 1 and 2: the name is not a declared event (a caller error); the name is the initial event's, which can be fired only by `Create` (the fire-once guarantee of §3A.5 working as designed, not a defect in the definition); or the event is declared but has no rows to dispatch — no transition rows from the current state, or, on a stateless precept, no `on <Event>` rows (a definition gap).
7. **Successful direct update.** Field write committed.
8. **Access mode failure.** Patch targets a field not editable in the current state — either `readonly` (read-only) or `omit` (structurally absent).
9. **Invalid input failure.** Patch is structurally malformed.

**Why the distinctions matter:**

- **Rejection vs ingress validation failure.** Rejection is an authored decision — the definition forbids this outcome. An ingress failure is a data truth violation — the value the caller supplied is not one the declaration admits. They require different responses from callers and different diagnostic framing.
- **Unmatched vs undefined.** Undefined means there is nothing to dispatch to — an unknown name, the initial event's name after construction, or an event with no rows in the current state (item 6 above); only the last can be a definition gap the author should address. Unmatched means routing exists but the current data does not satisfy any guard (an instance data condition the caller can address).
- **Transition vs no-transition.** Both are successes. No-transition events execute mutations without state change — a meaningful event-driven pattern, not a degenerate case.

#### Construction outcomes

Entity construction via the initial event produces a restricted outcome space. Construction always terminates in the initial state — the grammar structurally excludes `transition` from construction rows, making `Transitioned` impossible. Valid construction outcomes:

| Outcome | Meaning |
|---------|---------|
| `Created` | Construction succeeded. Entity now exists in the initial state (stateful) or with `State = null` (stateless). |
| `Rejected` | Intake refused — an authored `reject` row matched, or all guards failed with no unconditional fallback. |
| `InvalidArgs` | Argument validation failed at ingress, before row matching — collect-all as §3A.4 phase 1 defines it, so every failure the check finds is reported. |
| `Faulted` | A breach of one of the engine's own invariants — a defect in the compiler, the builder or the evaluator. Nothing constructs one today (`runtime/fault-system.md` Q3). |

Structurally impossible outcomes: `Transitioned` (grammar excludes `transition` from construction rows), `Applied` (exclusively a `Fire()` outcome — `Create()` produces `Created`), `Unmatched` (compiler guarantees construction rows exist; guard-failure produces `Rejected`), `UndefinedEvent` (compiler guarantees the initial event exists), and `ConstraintsFailed` (there is no post-mutation constraint evaluation to fail — the compiler proved the initial event establishes every rule, §0.7).

#### Restoration outcomes

Entity restoration from persisted data produces a distinct outcome space: successful restoration, or invalid input (structural mismatch — undefined state, unknown fields, type mismatch). Restoration does not evaluate the current definition's rules and ensures against the persisted data. Restore is not an ingress point: the data was governed when it was written, so hydration trusts it and does not re-check it (§0.7). The verification that does run at load is on the **definition**, not on the data — the runtime checks the compiler's proof certificate before it will govern under a definition at all, and refuses one whose certificate does not verify. Future migration logic runs during restoration, transforming persisted data to conform to the current definition.

### 3A.3 Constraint Violation Subject Attribution

When a constraint is violated, the language supports semantic subject attribution — not just the violation message, but what the violation is about.

Every constraint has both **semantic subjects** (the fields or args the constraint references) and a **scope** (why this constraint exists — which rule, which state ensure, which event ensure).

The four constraint kinds have distinct attribution:

1. **Event ensures** target event arguments plus the event scope. The user provided those args.
2. **Rules** target directly referenced fields plus the definition scope. The runtime does not reverse-map through mutations — if the author wants arg-level feedback, they write an event ensure.
3. **State ensures** target directly referenced fields plus the state scope (with anchor: in, to, or from).
4. **Transition rejections** target the event as a whole — this is an authored routing rule, not a data constraint.

Computed fields referenced in constraints are also targets, with transitive expansion to the concrete stored fields they depend on.

This attribution model is a language-level requirement because it flows from how the language distinguishes constraint scopes. The consumer decides rendering; the language provides the semantic structure.

### 3A.4 Mutation Atomicity

> **Draft 2026-11-25** — under adversarial review; nothing may cite this section until the mark is removed.

This section is the one full statement of what an operation is: what a configuration is, which operations produce or change one, the steps each operation runs and in what order, every write those steps can make, and what an earlier write does to anything already known about the values it changes. The runtime documents (`docs/runtime/evaluator.md`, `docs/runtime/runtime-api.md`) and `docs/compiler-and-runtime-design.md` describe how these steps are carried out and point here for what they are.

All mutations execute on a working copy. If the operation runs to completion, the working copy is promoted to become the entity's committed state. If the operation refuses part-way — an authored `reject` row matched, an incoming value failed its ingress check, a patch failed its access-mode check — the working copy is discarded whole and the entity's state is unchanged. Promotion is not conditional on a constraint check: every handler was proven at compile time to preserve every rule, so a working copy the operation completed is valid by construction. An invalid configuration never exists, even transiently, and there is no window in which a partially-committed state with violated rules can be observed.

Externally-sourced values are validated against their declared constraints at **ingress** — as they enter, before the working copy derives any dependent value (governance, §0.7). That check upholds the declared contract of the slot the value enters; there is no second pass over the finished working copy. Restored state is not ingress at all: it is trusted as valid at persistence time, and no later operation re-governs it (§0.7).

This guarantee applies to every path that can change entity data: event-driven transitions, stateless event handlers, construction, direct field updates, and state entry and exit actions. The working copy semantics are uniform — every such path uses the same all-or-nothing promotion or discard.

#### Configurations

A **configuration** is everything an entity is at one moment between operations (§0.2). It has these parts and no others:

1. **The state.** In a stateful precept, the name of exactly one declared state. A stateless precept has no state part; the runtime represents that as `Version.State == null` (§3A.5).
2. **The value of every stored field**, which is one of the following.
   - A non-optional scalar field holds exactly one value of its type.
   - An `optional` field holds either one value of its type or is **not set**. Not set is a value of the configuration, not a missing part of it: two configurations that differ only in whether an optional field is set are different configurations, and `is set` distinguishes them (§3.6).
   - A collection field holds its whole contents, which means everything its kind keeps about its elements ([`collection-types.md`](collection-types.md)): which elements are present (`set`); how many of each (`bag`); the elements and their order (`queue`, `stack`, `list`, `log`); each element together with its ordering value, and the order of arrival among elements that share an ordering value (`queue of T by P`, where arrival order breaks ties as a language guarantee, and `log of T by P`); each key with the value it maps to (`lookup`). The empty collection is a value like any other.
   - A field the current state **omits** (`in <State> omit F`, §2.2) has no value in this configuration. It is structurally absent: no expression anchored in that state may read it and no write may target it there (§2.2, D130 and D131). This is not the same as an optional field that is not set — `is set` cannot be asked of an omitted field, because the field is not there to ask about. What the runtime keeps in the field's storage while the field is omitted is its reset value — the value of its `default` (phase 0, below), put there by phase 0 at construction and by the `omit` reset on every later entry into a state that omits it. For a `default` that reads other fields, whether the reset value is the one computed at construction or the `default` evaluated again at the moment of the reset is not stated (*Known exceptions*, item 12). That is what the field holds when a later transition enters a state that does not omit it, unless a write later in that same operation gives it a value: the row's own actions or an entry action of the state being entered. A required field with no default (non-optional, no default, not computed — §2.2, D132) has no reset value: while it is omitted it has no value at all — D143 speaks of the assignment that brings it back as made "before any value exists in the source state" — and the `omit` reset leaves it with no value (phase 6). That is why D132 requires the transition that brings it back to assign it, and D132 places that assignment in "the transition action chain", the row's own actions. An entry action of the state being entered does not meet D132, even though it runs in the same operation: the entry action may be guarded, and it runs for every transition into the state, so it is not the transition's own statement that the field has a value.
3. **The value of every computed field.** A computed field's value is the value of its `<-` expression over the stored fields of the same configuration. It is fully determined by them, so it carries no information of its own, and no action, patch or restore writes it. It is listed as part of the configuration because guards, rules and ensures read it.

Event arguments are not part of any configuration. They exist for the length of one operation and are never stored unless an action copies them into a field.

A committed `Version` (`runtime/runtime-api.md`) is exactly one configuration. The **working copy** is a configuration being built; it is not a configuration of the entity until it is committed, and nothing outside the operation can observe it.

A configuration is **reachable** when `Create` can produce it, or when `Fire` or `Update` can produce it from a reachable configuration. `Restore` does not add to the reachable configurations: it returns a configuration that an earlier operation committed, which is the one precondition of §0.7.

A collection field declared without a `default` and without `optional` starts empty. *Reasoning:* a collection cannot be the target of `set` ([`collection-types.md`](collection-types.md) § Action Summary, *Cross-kind errors*: "Applying a scalar action (`set =`) to a collection field emits `ScalarOperationOnCollection`"), so if it had no starting value nothing could ever give it one; three kinds — `log of T by P`, `queue of T by P` and `lookup of K to V` — cannot take a `default` at all ([`collection-types.md`](collection-types.md), the *Constraints* line of each, and the common-surface paragraph that lists list-literal defaults "except `queue of T by P` and `lookup of K to V`"), so for them an empty start is the only start there could be; and every declaration in [`collection-types.md`](collection-types.md) written without a `default` is immediately followed by element actions on it. It follows that a collection field is never a **required field** in the sense canon uses — "non-optional, no default" in `RequiredFieldsNeedInitialEvent`, `InitialEventMissingAssignments` and `UninitializedFieldReadInInitialAssignment` (§3A.5), and "non-optional, no default value, not computed" in D132 and D143 (§2.2). Each of those means a field that has no starting value, and a collection has one. Read that way, a collection without a `default` does not need an initial event, an initial event does not have to assign it, and a transition that brings it back from `omit` does not have to `set` it — which no row could do. What an `optional` collection without a `default` starts as is not stated (*Known exceptions*, item 12). *Alternatives rejected:* treating it as a required field with no value, which would make every such declaration unusable; requiring `default []` everywhere, which contradicts the declarations canon itself gives. *Precedent:* collection-typed members in most languages with a governed construction step (a Kotlin `mutableListOf()` initialiser, a SQL child table with no rows) start empty. *Tradeoff accepted:* the empty start is implicit, so an author who wanted a non-empty start and forgot the `default` gets no diagnostic for it.

#### The operations

Four operations produce or change a configuration, and three more preview them. The runtime API that exposes them is `runtime/runtime-api.md`; what each one *is* is stated here.

| Operation | Starts from | Takes from outside | On success |
|---|---|---|---|
| `Create`, the precept declares an initial event | nothing — no configuration exists yet | the initial event's arguments | commits the first configuration, in the initial state (stateful) or with no state (stateless); outcome `Created` |
| `Create`, no initial event declared | nothing | nothing — arguments a caller passes anyway are not read (*Known exceptions*, item 2) | commits the first configuration; outcome `Created`. It cannot refuse (§3A.5) |
| `Fire` | a committed configuration | an event name and that event's arguments | commits a new configuration; outcome `Transitioned` when the winning row's outcome is `transition`, `Applied` when it is `no transition` or the precept is stateless |
| `Update` | a committed configuration | a patch: new values for any set of `editable` fields, the empty set included (below) | commits a new configuration in the same state; outcome `Updated` |
| `Restore` | nothing in memory — a state name and field values that were persisted | the persisted state name and field values | returns the persisted configuration as a `Version`; outcome `Restored`. It commits nothing new |
| `InspectCreate` | as `Create` | as `Create`, each argument optional | commits nothing; reports what `Create` would do |
| `InspectFire` | as `Fire` | as `Fire`, each argument optional | commits nothing; reports what `Fire` would do |
| `InspectUpdate` | as `Update` | as `Update`, the patch optional | commits nothing; reports what `Update` would do, and then what each event would do from the result |

There is no `InspectRestore`: `Restore` evaluates nothing that depends on a choice, so there is nothing to preview.

**An empty patch.** An update handler is `Update` "for any set of fields `editable` in that state" (*Handlers*), and the empty set is one of them. `Update` with an empty patch — or none — has nothing to check at phase 1 and nothing to write at phase 5; phase 7 gives every computed field the value it already had, and the operation commits `Updated` with a configuration equal to the one it was given, as the `Update` pseudocode in `runtime/evaluator.md` § 7.1 already does for a null patch. `InspectUpdate` given no patch previews exactly that `Update`, so its answer is complete, not partial (§3A.6). *Reasoning:* this is the update handler's definition applied to the empty set, and it is what the evaluator's canonical description already does. *Alternatives rejected:* refusing an empty patch as invalid input, which no outcome in §3A.2 names and which would give `InspectUpdate`'s no-patch form a different meaning from the operation it previews. *Precedent:* a SQL `UPDATE` that sets no column to a new value succeeds and leaves the row as it was. *Tradeoff accepted:* a caller that sends an empty patch by mistake gets `Updated`, not an error.

The read-only queries a `Version` answers (`runtime/runtime-api.md` § `Version`) are not operations: they change nothing and produce no configuration. One of them evaluates an expression. `FieldAccess` reports each field's access mode in the version's state, and for a field under a guarded access mode (§2.2 composition rule 4a) that mode depends on the guard, so the query evaluates the guard against the version. That is the evaluation `Update` makes at phase 1 of the same guard against the same configuration (*When a guarded access mode's guard is evaluated*), so it is safe wherever that one is, and it reports exactly the access mode `Update` would apply.

Every operation takes an immutable `Version` (or nothing) and returns a new one; none changes its input (`runtime/runtime-api.md`).

#### Handlers

A **handler** is one complete way an operation can run from its starting configuration to a commit. It is the unit the inductive step of §0.7 speaks about — "every handler preserves every rule" — so its extent has to be exact: it is everything that runs between the starting configuration and the commit, not only the row the author wrote.

- **A transition handler** is one pair of a source state and a transition row that applies in it and is not a `reject` row. It consists of the row's guard, the source state's exit actions (when the row's outcome is `transition`), the row's own action chain, the `omit` reset and entry actions of the target state (when the outcome is `transition`), and computed-field recomputation. A row with a `no transition` outcome is a handler made of its guard, its action chain and recomputation only. A handler that contains a guarded state action can run along more than one path — one for each combination of its guarded state actions whose guards hold — and each path is a sequence with no branch in it (*What each writer takes away*, and *Known exceptions*, item 13).
- **A construction handler** is one construction row (§3A.5) that is not a `reject` row: default materialization, the row's guard, its action chain, the initial state's entry actions (stateful precepts), and recomputation. On a precept with no initial event there is exactly one construction handler: default materialization, the initial state's entry actions (stateful precepts), and recomputation.
- **A stateless event handler** is one `on <Event>` row of a stateless precept, for an event that is not the initial event, that is not a `reject` row: its guard, its action chain, and recomputation.
- **An update handler** is `Update` in one state: the patch write of phase 5, for any set of fields `editable` in that state, followed by recomputation. A stateless precept has no state, and has exactly one update handler: `Update` on any configuration, for any set of fields that carry the field-level `editable` modifier (§2.2 composition rule 1; a state-level override needs a state to name, so there is none). It has no row and no guard of its own. It is part of the inductive step exactly as the other handlers are, because `Update` produces reachable configurations (*Configurations*) and §0.7 counts direct field edits among the ways values enter the entity: every update handler must preserve every rule, or the definition is rejected. A guarded access mode decides from its guard whether the field is editable (§2.2 composition rule 4a): a guarded `editable` makes it editable only when the guard holds, so the guard is a premise of the update handler for that field; a guarded `readonly` makes it read-only when the guard holds, so the field is editable only when the guard is false, and the premise is the guard's negation. Either way it plays the part a row's guard plays for a transition handler. *Which other premise* closes the proof — the patched value's own declared constraints only, or also every rule that mentions the field, checked at ingress — is an outstanding owner decision (see [Open Questions](#open-questions--implementation-notes)); that the update handler is in the induction does not depend on the answer.

**What a handler must make true of the configuration it commits.** "Every rule" in §0.7 is the whole of it only for rules; the state-scoped ensures of §3A.1 add obligations that depend on what the handler does with the state.

- *Every rule*, whatever the handler (§0.7).
- *The `in` ensures of the state the handler commits in*, each whenever its guard holds of the committed configuration. §3A.1 says an `in` ensure "holds for every operation while the entity is in this state", so this binds every handler that commits in that state — a `transition` into it, a `no transition` row in it, an update handler in it, and a construction handler when it is the initial state.
- *The `to` ensures of the state the handler enters*, for a handler whose outcome enters a state: every `transition`, including one back to the same state, and every construction handler of a stateful precept. Each must hold of the configuration the handler commits — after the row's actions, the `omit` reset, the entry actions and recomputation. A `no transition` row and an update handler enter no state, so `to` ensures are not their obligation; a `to` ensure is transitional (`compiler-and-runtime-design.md`), true of the configuration a transition into the state committed and not of every configuration in the state.
  - *Reasoning.* §3A.5 already states this position for construction: the initial state's entry ensures must hold of "what construction commits", which "is the result of the initial event's actions followed by the initial state's entry actions". A transition enters a state the same way construction does (phase 6), and §3A.1 says only that a `to` ensure is "checked on any transition into this state", so the same position applies.
  - *Alternatives rejected.* Holding it of the working copy before the entry actions: the entry actions are part of entering, and §3A.5 counts them for construction. Holding it of the configuration before the row's actions: that configuration holds only what the source state and its exit actions left, so the ensure would be asked of what is being left rather than of what is being entered, and §3A.1 says a `to` ensure is "checked on any transition into this state". (This rejection does not depend on which configuration a `from` ensure holds of, which is not stated — *Known exceptions*, item 12.)
  - *Precedent.* The v1 language design evaluated every state-scoped assertion against the configuration the operation was about to commit; a postcondition on a method in a design-by-contract language holds of the state the method leaves behind, after all of its body.
  - *Tradeoff accepted.* An entry action can make a `to` ensure true that the row alone left false, so a reader who looks only at the row cannot tell whether the ensure is met.
- *The `from` ensures of the state the handler leaves*, for a handler whose outcome is `transition`. §3A.1 says one is "checked on any transition out of this state", and §2.2 D130 anchors its reads to the state being left, but canon does not say which configuration of the handler it must hold of — the starting configuration, the working copy after the exit actions, or the configuration after the row's actions. That is not stated (*Known exceptions*, item 12), and no analysis may assume an answer.

Two things are not handlers.

- **A `reject` row** writes nothing and commits nothing, so there is nothing for it to preserve. Its guard and the expressions interpolated into its reason string are still evaluated (*Operation execution order*, phase 2), so they are positions where an expression must be safe to evaluate.
- **A state action** (`to <State> -> …`, `from <State> -> …`, §2.2) has no operation of its own. It runs only as part of each handler that enters or leaves its state, at the position phases 3 and 6 give it. Whatever has to be true for a state action's expressions to be safe has to be true at that position in every handler that contains it.

**How rows written with `any` or a comma list become handlers.** A comma-delimited `StateTarget` expands to one independent declaration per named state, preserving source order (§2.2), so `from A, B on E …` is two rows and gives two handlers. A row written `from any on E …` applies in every declared state: at dispatch it is a candidate whatever the current state is, at the position its declaration holds among the other rows. Each execution of it starts from one particular state, runs that state's exit actions, and reads that state's fields, so there is one handler per source state for it, exactly as for the comma form. The same holds for state actions and ensures written with `any` or a comma list: each applies to each state it names. Whether the *compiler* analyses a `from any` row once or once per state is still open (see [Open Questions](#open-questions--implementation-notes)); whichever it does, what it concludes has to hold for the handler of every source state, because those are the paths that execute.

#### Operation execution order

Every operation — `Create`, `Fire`, `Update`, `Restore`, and their inspection counterparts (§3A.6) — executes these phases in this order. A phase that does not apply to an operation is skipped; the relative order of the phases that do apply never varies by operation. The table after the list says which phases each operation runs.

0. **The starting working copy.** `Fire` and `Update` start from a copy of the configuration they were given. `Restore` starts from the persisted state name and field values, written into the fields as given (restore slot injection). `Create` starts from **default materialization**: the state is the initial state (stateful precepts), and each stored field is given its starting value in declaration order — the value of its `default` expression, which may read fields declared before it (§3.5); not set for an `optional` scalar field without a default; empty for a non-optional collection without a default (*Configurations*); and no value yet for a required field without a default, which the construction row must assign before anything reads it (§3A.5, `InitialEventMissingAssignments` and the two `Uninitialized…` diagnostics). What an `optional` collection without a default starts as is not stated (*Known exceptions*, item 12). A field the initial state omits is materialized like every other field — it is absent from the configuration construction commits (§3A.5), but its storage holds the value materialization gave it, which is its reset value (*Configurations*; §2.2 composition rule 5 resets an omitted field "to default"). A required field without a default that the initial state omits is a case canon does not settle: `InitialEventMissingAssignments` (§3A.5) requires the construction row to assign every required field without a default and makes no exception for an omitted one, while §2.2 D131 forbids a write to a field the target state omits and speaks only of a transition's target, so whether such a definition is refused, whether the construction row must assign the field, or whether it stays without a value until D132 makes the transition that brings it back assign it, is not stated (*Known exceptions*, item 12). The working copy built this way for `Create` is what §3A.5 calls the hollow version.

   **A read of a field that has no value at that point is refused at compile time.** Default materialization runs in declaration order, so at the moment a `default` expression is evaluated, the fields declared after it have no value yet, and neither has any required field without a default, wherever it is declared. A `default` expression that reads such a field — directly, or through a computed field whose `<-` expression reads it (*What a computed field returns when it is read during an operation*) — is a compile error. The same holds for every expression `Create` evaluates before the construction row's actions: the guard of a construction row, the reason string of a construction `reject` row (phase 2), the constraints on the initial event's arguments and its `on <InitialEvent> ensure` declarations (phase 1) may not read a required field without a default, directly or through a computed field. An `optional` scalar field without a default (not set) and a non-optional collection without a default (empty) have a value from phase 0 on and may be read in all of these positions — `when Notes is set` is exactly the question `is set` exists to ask.
   - *Reasoning.* Canon already refuses every other read of a field that has no value yet: reading a non-defaulted field in a construction guard is a compile-time error (`runtime/runtime-api.md` § Create, *Hollow context*); `UninitializedFieldReadInInitialAssignment` and `UninitializedCrossFieldReadInInitialAssignment` refuse it in the construction row's actions; D143 refuses it, "directly or through a computed-field dependency", in the assignment that brings a required field back from `omit` (§2.2). A `default` expression, an event ensure and a construction `reject` row's reason string are further positions where the same read can happen, and §0.1 principle 10 (totality) admits no evaluation that has nothing to read. Applied the same way here, the rule says nothing new. The rule is drawn at "no value", not at "no `default`": `runtime/runtime-api.md`'s own reason is that "a field with no default has no value until a construction action establishes it", and an `optional` field without a default is not set, which §3.6 treats as a value `is set` can test, while a collection without a default starts empty (*Configurations*). Refusing those reads would refuse a read that cannot fail.
   - *Alternatives rejected.* Evaluating the read against a zero value of the field's type: it invents a value the author never gave, and for `date`, `money` and similar types there is no such value. Faulting at runtime: there are no runtime fault traps (§0.7).
   - *Precedent.* Definite-assignment analysis in C# and Java refuses a read of a local or field before every path to it has assigned it.
   - *Tradeoff accepted.* No diagnostic is assigned yet for the `default`, event-ensure and reject-reason positions (see [Open Questions](#open-questions--implementation-notes)); and a `default` that reads a computed field is refused whenever that computed field depends, even indirectly, on a field declared later, which an author may find surprising.
1. **Ingress governance.** First, `Fire` resolves the event name. A name that is not a declared event, or that names the initial event, ends the operation here with `UndefinedEvent`, before anything is checked or evaluated: the initial event is not in the post-construction event space at all (§3A.5, *Fire-once guarantee*), so its argument constraints and `on <InitialEvent> ensure` declarations are evaluated only by `Create`, and its construction rows are never candidates for `Fire` — on a stateless precept as on a stateful one. Then externally-supplied values are checked against their declared constraints as they enter, before the working copy derives any dependent value (§0.7). For `Create` and `Fire` these are the event's arguments: that every argument the caller names is one the event declares (a name it does not declare is invalid input, `InvalidArgs` — `runtime/runtime-api.md` § Create, "unknown key"), their types and presence, the constraints declared on each argument, and the event's `on <Event> ensure` declarations — each of these with its `when` guard first, where it has one (*A guarded event ensure*, below). For `Update` they are the patch: that every field the patch names is a declared field (a patch that names one that is not is structurally invalid — §3A.2 item 9, `InvalidFields`); then the access mode of each patched field in the current state (§2.2), evaluating the guard of any guarded access mode against the starting configuration; then the type of each patched value; then the constraints declared on each patched field, each reading every other field it mentions at the value that field will hold once the patch is written — the patched value where the patch names the field, the starting value where it does not, and for a computed field the value of its `<-` expression over those same values (*Which values a patched field's own constraints read*, below). Whether every rule that mentions the field is also evaluated here is the outstanding owner decision of *Known exceptions*, item 11. The check is collect-all (§3A.1), within the limit stated just below. Any failure refuses the operation here, with nothing written. `Restore` has no ingress: it checks only that the state name is declared, that every field named is declared, and that every value has its field's type. Data that fails any of these is refused as invalid input (§3A.2, *Restoration outcomes*; `runtime/runtime-api.md` § Restoration, which says these are the only ways `Restore` can fail). What `Restore` does when the persisted data gives no value for a stored field that the persisted state does not omit is not stated: a restored `Version` has to be a configuration (*Configurations*), which data missing a field is not, yet canon also says restore must not block data written under an earlier definition and that a host restoring such data has stepped outside the guarantee (`runtime/runtime-api.md` § Restoration). See *Known exceptions*, item 12. What a restored entity holds in the storage of a field its persisted state omits is not stated (*Known exceptions*, item 12). Because this phase comes before dispatch, an event's argument constraints and `on <Event> ensure` declarations are evaluated in whatever state the entity is in, including a state where the event has no rows.

   **What collect-all does not evaluate.** Collect-all means every check this phase makes is made and every failure is reported; it does not mean that every declared constraint is evaluated whatever the earlier checks found. The checks come in the order given above — presence and type (and, for `Update`, access mode) first, then constraints — and a constraint is evaluated only over values that passed the earlier checks. A constraint or an `on <Event> ensure` that reads a required argument that was not supplied, or an argument of the wrong type, is not evaluated; a constraint declared on a patched field whose access check or type check failed is not evaluated, because that value is refused and is not entering the field, and neither is a constraint that reads another patched field whose access or type check failed.

   **A constraint whose safety was proved from another phase-1 constraint is evaluated only if that constraint passed.** The constraints of this phase are themselves evaluations, and the compiler has to prove each of them safe to evaluate (§0.1 principles 10 and 11). What it may prove one from is what holds when phase 1 reaches it: the types and presence of the incoming values; for `Fire` and `Update`, the rules and the `in` ensures of the starting state, as they are known of the starting configuration (*What is known*) — for `Update` only those that read no field the patch names, directly or through a computed field, because a constraint at this phase reads a field the patch names at its patched value (*Which values a patched field's own constraints read*), and what the starting configuration's rules say about that field's starting value says nothing about it; for `Create`, what default materialization gave each field (*What is known*); and other constraints of this same phase. With `rule B > A` and `field C as integer editable max 100 / (B - A)`, `A` and `B` both `editable`, the update handler covers the patch `{A: 5, C: 1}`, which names `A`; so `B > A` is not a premise for `C`'s `max` there, and unless something else proves `B - A` nonzero over the patched values the definition is rejected. A constraint of this phase can have failed in the same check — collect-all goes on after a failure — so a constraint whose proof relied on another phase-1 constraint is evaluated only when every phase-1 constraint its proof relied on passed; otherwise it is not evaluated and nothing about it is reported, exactly as for a constraint that reads a value that failed its type check. With `event E(X as integer positive)` and `on E ensure 100 / E.X > 1`, the division is proved from `positive`, so `E.X = 0` reports the failure of `positive` and the ensure is not evaluated. With `field B as integer positive editable` and `field A as integer editable max 100 / B`, the patch `{A: 1, B: 0}` reports `positive` on `B` and does not evaluate `A`'s `max`, which reads the patched `B`; the same holds when `A`'s constraint reads `B` through a computed field. The compiler's reliance among phase-1 constraints must not be circular — a constraint cannot be proved safe from one whose own proof relies on it — so the order in which they are evaluated always exists. A constraint whose proof relies on no other phase-1 constraint is evaluated whatever the others found.
   - *Reasoning.* Collect-all (§3A.1) and the fault guarantee (§0.1 principles 10 and 11) both bind phase 1, and evaluating every constraint over every value that passed type and presence breaks the second as soon as one constraint's safety rests on another that just failed. This is the rule §3A.6 already applies to previews — an expression is evaluated only when every premise the compiler's proof of it relied on is known to hold — applied to the one place where an operation evaluates constraints, and it keeps every proof an author would expect: an event ensure may divide by an argument its own modifier keeps positive.
   - *Alternatives rejected.* Forbidding the compiler to rely on one phase-1 constraint when proving another: it refuses `on E ensure 100 / E.X > 1` with `X positive`, which can never fail, and gives up proof power the design is meant to state in full. Two fixed groups, the constraints declared on one value first and every other constraint after: it still refuses a value's own constraint that relies on another of its own, such as `max 100 / A` beside `positive` on `A`. Evaluating in declaration order and stopping at a failure: it makes the report depend on the order the author happened to write and contradicts collect-all.
   - *Precedent.* §3A.6 of this specification, for previews; dependent validation rules in validation libraries (FluentValidation's `DependentRules`), which run a rule only when the rules it depends on passed; short-circuit evaluation of `&&` in C-family languages.
   - *Tradeoff accepted.* Which constraint failures a refused operation reports depends on which premises the compiler's proof used, so a caller who fixes one failure can see a further failure that the first one hid; the executable model has to carry, for each phase-1 constraint, the phase-1 constraints its proof relied on.

   > **Draft 2026-11-25** — under adversarial review; nothing may cite this section until the mark is removed.

   **A guarded event ensure.** An `on <Event> ensure` may carry a `when` guard (§2.2, *State/event ensure*; §3A.1 gives `on Submit when Submit.Type == "payment" ensure Submit.Amount > 0`). Phase 1 evaluates such an ensure in two steps: its guard, and then, only when the guard is true, its body. When the guard is false the ensure passes and its body is not evaluated. The guard is an evaluation like any other phase-1 constraint, held to every limit of this phase (*What collect-all does not evaluate*, and the rule that follows it); the body is an evaluation whose safety the compiler may prove from its own guard being true, as well as from everything else that holds at phase 1. So `on Pay when Pay.Parts > 0 ensure Pay.Total / Pay.Parts >= 10`, given `Parts = 0`, evaluates the guard, finds it false, and passes without dividing. What a guarded event ensure makes known for the rest of the operation is not its body but "the guard was false, or the body was true" (*What is known*, ingress constraints on arguments): `on Submit when Submit.Type == "payment" ensure Submit.Amount > 0` does not make `100 / Submit.Amount` safe in a row unless the row also knows `Submit.Type == "payment"`.
   - *Reasoning.* §3A.1 says of a guarded rule that it "applies only when its guard is true" and gives ensures the same optional guard, and this section already holds an `in` ensure only "whenever its guard holds" (*Handlers*). An ensure whose guard is false does not apply, so there is nothing to evaluate its body for, and evaluating it anyway would evaluate an expression the author protected with that guard — `Pay.Total / Pay.Parts` with `Parts = 0` — which §0.1 principles 10 and 11 forbid. Treating the body as known whatever the guard said would make the compiler accept `100 / Submit.Amount` after a `refund` with `Amount = 0`.
   - *Alternatives rejected.* Evaluating the body whenever everything it reads was supplied, ignoring the guard: it evaluates protected expressions and reports failures for ensures that do not apply. Treating every event ensure's body as a premise after phase 1: unsound for the reason just given.
   - *Precedent.* A row's `when` guard in this language, which the row's actions may rely on (phase 2, *What is known*); a conditional constraint in SQL written `CHECK (NOT guard OR body)`, which holds whenever the guard is false.
   - *Tradeoff accepted.* An author who wants the body known in every case has to write the ensure without a guard, or repeat the guard where the fact is needed.

   **An `optional` argument the caller leaves out is not a missing argument.** It is not set, and not set is a value: §2.4 says of `optional` on an event argument that "the value may be absent" and recommends `on Event ensure Arg is not set or Arg.length > 0`, an ensure that exists to be evaluated over an argument left out; `runtime/runtime-api.md` § `IArgBuilder` says an unset optional argument "remain[s] absent" while an unset required argument is `InvalidArgs`. So an omitted `optional` argument passes the presence check, and every argument constraint and `on <Event> ensure` that reads it is evaluated, reading not set — exactly as a constraint over an `optional` field that is not set is evaluated. Only a *required* argument — one that is neither `optional` nor carries a `default` (the next paragraph) — that was not supplied is a failure here, and only it leaves unevaluated the constraints that read it. This is what makes an `on <Event> ensure` a premise the compiler may rely on whatever the caller supplied (a guarded one in the weaker form given under *A guarded event ensure*): `on Submit ensure Submit.Note is set or Submit.Amount < 100` holds at every position after phase 1 of an operation that passed it, including one in which `Note` was left out. Each of those is already a failure the caller receives; nothing about the constraint that was not evaluated is reported. Constraints that read only values that passed are evaluated and their failures reported, as collect-all requires. How one outcome reports failures of more than one kind is a question about outcome shapes, not about what is evaluated (`runtime/result-types.md`).
   - *Reasoning.* §0.1 principle 10 (totality) admits no evaluation that has nothing to read, and the same rule phase 0 applies to fields with no value applies to arguments with no value: `E.Amount > 0` with no `Amount` supplied, or `max 100` over a string, has no defined result. §3A.1 asks that the caller receive "the complete set of failures"; a check that could not be made is not a failure the caller can act on beyond the one already reported for the missing or wrong value. A preview is bound by the same limit (§3A.6, rule 2), so a preview never evaluates a constraint over a value the operation would refuse before reaching that constraint.
   - *Alternatives rejected.* Evaluating every constraint regardless: it evaluates expressions over values that are not of the declared type or are not there. Stopping at the first failed check of any kind: it contradicts collect-all (§3A.1).
   - *Precedent.* Form and schema validators (JSON Schema, ASP.NET model binding) report a type error for a field and do not then run that field's range checks on the unconverted value; definite-assignment rules refuse a read of a value that was never given.
   - *Tradeoff accepted.* A caller who fixes a missing or mistyped value can then receive a new constraint failure for that value on the next attempt, so the report is complete for what could be checked rather than for everything declared.

   **An argument with a `default` that the caller leaves out takes its default.** An argument may carry `default`: `ArgDecl` takes the same `FieldModifier` list a field does (§2.2, §2.4), the modifier-validation table of §3.8 does not refuse `default` on an argument, `runtime/descriptor-types.md` gives `ArgDescriptor` a `DefaultExpression`, and `../compiler/diagnostic-system.md` and `../compiler/proof-engine.md` treat "field/arg defaults" alike. Canon calls a value required when it is "non-optional, no default" (§3A.5, §2.2 D132), so an argument with a `default` is not a required argument. Left out, it passes the presence check and holds the value of its `default` expression from that point on, exactly as if the caller had supplied that value: it is given that value at this phase, before any constraint of this phase is evaluated, so its own constraints, any other constraint that reads it and the event's `on <Event> ensure` declarations are evaluated over it, and what they establish is known for the rest of the operation as for a supplied value (*What is known*). An argument that is both `optional` and has a `default` takes its default when left out, as a field that is both starts at its default (phase 0). What an argument's `default` expression may read — only literals, or also fields, and in that case against which configuration — is not stated (*Known exceptions*, item 12).
   - *Reasoning.* Canon gives arguments the `default` modifier and the descriptor that carries it, and draws "required" at "non-optional, no default" for fields; applied the same way to arguments, a defaulted argument the caller omits is complete input, and the operation runs with its default, as phase 0 does for a defaulted field.
   - *Alternatives rejected.* Treating an omitted defaulted argument as missing (`InvalidArgs`): the declared default could then never be used, and the declaration would be dead text. Refusing `default` on an argument: it contradicts the grammar, the §3.8 table and the descriptor, and removes a form canon shows.
   - *Precedent.* Optional parameters with default values in C# and Python: a call that omits one runs with the default, and the call is complete.
   - *Tradeoff accepted.* The operation's result does not show whether a value was supplied or defaulted, and a preview that leaves a defaulted argument out answers for the default (§3A.6), so a caller cannot ask "what if I supply this later?" by leaving it out.

   **When a guarded access mode's guard is evaluated.** Against the starting configuration, at phase 1, before any patched value is written. §2.2 composition rule 4a says a guarded `editable` makes a field editable "when the guard holds" and a guarded `readonly` makes it read-only "when the guard holds"; this says which configuration the guard is asked of.
   - *Reasoning.* Phase 1 runs before phase 5, so the starting configuration is the only configuration that exists when access is checked. The access mode says whether the entity, as it is, admits a direct edit of the field; a row's guard is asked of the starting working copy for the same reason (phase 2). The update handler's premise follows: the guard (or its negation, for a guarded `readonly`) holds of the starting configuration, and, like any guard fact, it is lost for a field the patch writes (*What an earlier write invalidates*).
   - *Alternatives rejected.* Asking the guard of the configuration the patch would produce: the patch would then grant its own permission — from a locked record, a patch that sets the lock field to unlocked and edits the guarded field in the same patch would be accepted. Asking it of both and requiring both: it adds a rule §2.2 does not state, and refuses a patch that locks a record while making a last permitted edit.
   - *Precedent.* A row's guard in this language (phase 2); an operating-system or database permission check, which is made against the object as it stands when the write is requested.
   - *Tradeoff accepted.* One patch may make an edit the starting configuration permits together with an edit that ends the permission (`{Locked: true, Amount: 999}` from an unlocked record is accepted), so an author who wants the lock and the guarded field never to change together needs a rule saying so.

   **Which values a patched field's own constraints read.** A constraint declared on a patched field may mention another field (§2.4 speaks of "a relational constraint", "whether written as a modifier or as a `rule`"). At phase 1 it reads each field it mentions at the value that field will hold once the patch is written: the patched value where the patch names the field, the starting value where it does not. A computed field it mentions — which no patch names (*Configurations*) — is read as the value of its `<-` expression over those same values, the patched values together with the unpatched starting values, because that is the value phase 7 gives it in the configuration the patch produces. This is the one place an expression reads a computed field over values other than the working copy as it then stands (*Known exceptions*, item 14): at phase 1 the patch has not been written, and reading the computed field over the starting working copy would check `max Budget` with `Budget <- Limit - Reserve` against the old `Limit` while the patch changes `Limit`, and commit a `Price` above the `Budget` phase 7 then derives. Evaluating the `<-` expression here is an evaluation position like any other: its safety is proved from what holds at phase 1, under the rule above for constraints whose proof relies on other phase-1 constraints. This is about the constraints declared on the patched fields only; whether anything beyond them is evaluated is the outstanding owner decision of *Known exceptions*, item 11, and this paragraph does not depend on its answer.
   - *Reasoning.* A constraint declared on a field is a rule (§2.4), and the configuration `Update` commits must satisfy every rule (§0.7; *Handlers*, update handler). In an update handler the patch is the only write to a stored field (phase 5), so the values a patched field's constraint has to hold of at commit are exactly the patched values together with the unpatched starting values. Evaluating it against those values checks the configuration that will exist; nothing later can change the answer.
   - *Alternatives rejected.* Reading the other fields at their starting values: with `field A as integer max B editable` and `field B as integer editable`, the patch `{A: 10, B: 5}` from `B = 20` would pass and commit `A = 10` with `B = 5`, a configuration that breaks the constraint — or, to stop that, the compiler would have to reject every definition in which both fields are editable, since nothing else could close the proof. Requiring it to hold against both the starting and the patched values: it refuses a patch that moves both fields to a consistent pair, and adds a check §0.7 does not ask for.
   - *Precedent.* A SQL `CHECK` constraint is evaluated against the row as the `UPDATE` leaves it, with every changed column at its new value and every other column at its old one.
   - *Tradeoff accepted.* Whether one patched value is accepted can depend on another patched value, so a caller has to read the whole patch to understand a refusal; §3A.3 attributes the failure to every field the constraint reads, which names both.
2. **Dispatch.** For row-dispatched operations, the candidate rows are taken in declaration order — for `Fire` on a stateful precept, the transition rows whose `from` names the current state, by name, by comma list or by `any`; for `Fire` on a stateless precept, the event's `on <Event>` rows (never the initial event's, phase 1); for `Create` with an initial event, the construction rows. If `Fire` has no candidate rows the operation ends with `UndefinedEvent`. `Create` always has at least one: a precept that declares an initial event with no construction row is a compile error (`ZeroConstructionRows`), which is why `UndefinedEvent` is not in construction's outcome space (§3A.2). Otherwise each candidate's guard is evaluated in turn against the starting working copy and the arguments; a row with no guard matches. The first row whose guard is true wins, and the rows after it are not evaluated at all — neither their guards nor anything else in them (§3A.1, *Collect-all vs first-match*). Guard evaluation selects; it writes nothing (§0.4 property 6). If no guard is true, `Fire` ends with `Unmatched` and `Create` ends with `Rejected` (§3A.2). If the winning row is a `reject` row, its reason string is evaluated — it may interpolate fields and arguments, which read the same values the guard read — and the operation ends with `Rejected`; nothing is written.
3. **Exit actions.** The state actions declared on the state being left (`from <State> -> …`, §2.2) are applied to the working copy. Their expressions resolve against the state being left: a state action is anchored to the state it names, so its guard and the right-hand sides of its actions are state-anchored expressions of that state (§2.2, D130, which lists "state hook guard" and "action RHS" among the state-anchored contexts). Every `transition` outcome leaves its source state, including a transition back to the same state. A `no transition` outcome and a construction row leave no state, so this phase is skipped for them.
4. **The state change.** The entity's state becomes the row's target state. This sits after exit actions and before entry actions, so each state action runs while the working copy carries the name of the state it is anchored to: an exit action its source state, an entry action its target. The state change writes no field, and no expression can read the state name, so nothing an operation evaluates can tell exactly where between phases 3 and 6 it happens.
5. **Mutation.** The selected row's action chain — or the update patch — is applied to the working copy in written order, each write seeing the state left by all preceding writes (§0.4 properties 2 and 3). A row's actions read the fields of the state the row was written `from`, as changed by the exit actions and by the row's own earlier actions (§2.2, D130 and D143), and write fields of the row's target state (§2.2, D131 and D132). A `no transition` row stays in its source state, so the state it commits in is that state, and a write it makes to a field that state omits is refused as D131 refuses one to a field the target omits: the field is absent from the state (§2.2, "field absent from state entirely"), the configuration the row commits is in that state and gives the field no value (*Configurations*), and no `omit` reset follows to discard the write (phase 6 is skipped), so the write would leave in the field's storage a value other than its reset value, which a later transition that brings the field back would then expose.
6. **Entering the target state.** First the **`omit` reset** (§2.2 composition rule 5): every field the target state omits is put back to its reset value — the value of its `default`, as for phase 0 (for a `default` that reads other fields, whether that is the value computed at construction or the `default` evaluated now is not stated — *Known exceptions*, item 12). A field without a `default` is put back to the starting value phase 0 gives such a field: an `optional` scalar field becomes not set, a non-optional collection becomes empty (what an `optional` collection without a default becomes is as unstated here as at phase 0 — *Known exceptions*, item 12), and a required field without a default is left with no value, which D132 and D143 (§2.2) already presuppose (*Configurations*). Whatever the field held in the source state is gone in every case. No expression can read the field itself until a transition brings it back, when D132 makes the row assign a required one before anything reads it; whether a computed field that derives from it may be read while it is omitted, and what such a read returns, is not stated (*What sits between the guard and each later position*; *Known exceptions*, item 12). Then the state actions declared on the state being entered (`to <State> -> …`) are applied. Their expressions resolve against the state being entered, the state they are anchored to (§2.2, D130 and D131). Every `transition` outcome enters its target state, including a transition back to the same state, and `Create` enters the initial state (§3A.5); for `Create` the reset is not run, because phase 0 already gave every field the initial state omits its reset value. That is the whole of it only if the construction row cannot write a field the initial state omits, which canon does not settle (*Known exceptions*, item 12). A `no transition` outcome enters no state, so this phase is skipped for it.
7. **Computed-field recomputation.** After all mutations, every computed field is re-derived from the working copy (§3.5, expression scope), in dependency order: a computed field that reads another computed field is derived after it. The order always exists, because a dependency cycle among computed fields is a compile error (§3.8, `CircularComputedField`). Whether a computed field may be omitted by a state, and what this phase does with one that is, is not stated (*Known exceptions*, item 12).
8. **Commit.** The working copy is promoted. There is no constraint-evaluation phase between recomputation and commit: the rules were proven preserved at compile time (§0.7), so the completed working copy needs no verdict before it is promoted. An operation that refused at phase 1 or ended at phase 2 never reaches this phase, and its working copy is discarded whole. `Restore` returns its working copy as a `Version` at this point; it promotes nothing, because there was no earlier version in memory to replace.

| Phase | `Create`, initial event | `Create`, no initial event | `Fire`, `transition` row | `Fire`, `no transition` row | `Fire`, stateless precept | `Update` | `Restore` |
|---|---|---|---|---|---|---|---|
| 0 Starting working copy | default materialization | default materialization | copy of the given version | copy | copy | copy | persisted values |
| 1 Ingress governance | event arguments | nothing to check | event arguments | event arguments | event arguments | the patch | none — structural check only |
| 2 Dispatch | construction rows | — | transition rows | transition rows | the event's rows | — | — |
| 3 Exit actions | — | — | source state's | — | — | — | — |
| 4 State change | — (already the initial state) | — | to the target | — | — | — | — |
| 5 Mutation | the row's actions | — | the row's actions | the row's actions | the row's actions | the patch | — |
| 6 Entering the target state | initial state's entry actions (stateful) | initial state's entry actions (stateful) | `omit` reset, then entry actions | — | — | — | — |
| 7 Recomputation | yes | yes | yes | yes | yes | yes | yes |
| 8 Commit | `Created` | `Created` | `Transitioned` | `Applied` | `Applied` | `Updated` | returns `Restored` |

The inspection operations run the same phases as the operation they preview, up to and including phase 7, and never phase 8 (§3A.6).

Phases 3, 4 and 6 restore a statement that was made canonically and then lost. `docs/ArchitectureDesign.md` stated this order twice — once for the mutating operation (*"executes the event pipeline (exit actions → row mutations → entry actions → derived fields → constraint evaluation), and commits on success"*) and once for its inspection counterpart (*"guard evaluation, exit state actions, row mutations, entry state actions, derived field recomputation, constraint evaluation"*) — and `docs/EngineDesign.md` carried the same sequence in its rationale. Both documents were deleted whole in the v1 documentation reset; the sentence surviving in §3A.6 is the last remaining fragment of the statement they made in full. Nothing in canon has ever stated a different order for the actions. Composition rule 6 of §2.2 is not a separate source for this order: it says only that a write may not target a field the target state omits.

**Several state actions on one state.** A state may carry more than one exit action and more than one entry action, whether written as separate declarations, written with `any`, or produced by a comma-delimited `StateTarget`, which expands into one declaration per named state, preserving source order (§2.2). All of them run, in declaration order, and each sees the writes of those before it, exactly as the actions of one `->` chain do (§0.4 properties 2 and 3). A guarded state action runs only when its guard holds; the guard is evaluated when that state action's turn comes, against the working copy as the earlier writes of the operation have left it, and a guard that is false skips that state action and nothing else. Exit actions and entry actions follow the same rule. That all of them run, in declaration order, was ruled by the owner on 2026-11-24 — "you can always have more than one -> action, it's no different here than in other places" — on the grounds that a state action is an action chain like any other, so several of them behave as one longer chain would, and the comma expansion already promises source order.

When a guarded state action's guard is evaluated is not part of that ruling, and is decided here: at that state action's turn, against the working copy as the earlier writes of the operation have left it.
- *Reasoning.* The ruling makes several state actions behave as one longer chain, and in a chain each step sees the state left by all preceding steps (§0.4 properties 2 and 3). A guard is the first thing its state action evaluates, so it sees what the earlier steps wrote, as the state action's own actions do. A row's guard is different because it selects among rows before anything runs (phase 2); a state action's guard selects nothing among alternatives, it only decides whether that one state action runs.
- *Alternatives rejected.* Evaluating every state-action guard against the starting working copy, or against the working copy as it stood when the phase began: the guard and the actions it protects would then read different values of the same field, and a fact the guard established would not hold for the actions after an earlier state action wrote that field — the situation *What an earlier write invalidates* exists to exclude.
- *Precedent.* A sequence of `if` statements in any imperative language evaluates each condition when control reaches it; UML entry and exit behaviours that test data do so when they run.
- *Tradeoff accepted.* Whether a later state action runs can depend on what an earlier one wrote, so the author has to read the state actions on a state in order to know which of them run.

**A transition back to the same state** (`from Review on Revise -> transition Review`) is an ordinary `transition` outcome, and all of phases 3 to 6 apply to it: exit actions, the state change, the row's own actions, then the `omit` reset (which §2.2 composition rule 5 already states includes self-transitions) and entry actions. The grammar has one `transition` outcome form, with no separate form for a self-transition (§2.2). The outcome that changes data while staying in place without running any state action is `no transition` (§3A.2).

**Construction enters the initial state.** `Create` on a stateful precept enters the initial state, so that state's entry actions run after the construction row's own actions, at phase 6, and before computed fields are recomputed. They run on a parameterless `Create` as well, against the default values. No state is left, so no exit actions run. This follows from how canon already treats construction: the initial state's entry ensures must hold of what construction produces, and the fields the initial state omits are absent from what construction builds (§3A.5). Entry actions are the third effect of the same `to <State>` prefix. A stateless precept has no state actions.

**Where the `omit` reset sits, and why it is not in phase 4.** Earlier text of this section placed the reset in phase 4, before the row's actions. That contradicted §2.2: D130 and D143 let a row's action read a field of the state the row is written `from`, and a field that the source state has and the target state omits is such a field, so with the reset ahead of the row a permitted read — `from Draft on Submit -> set Summary = Notes -> transition Submitted`, where `Submitted` omits `Notes` — would have read the reset value instead of the value the guard had just read. The reset now runs in phase 6, as the first step of entering the target state.
- *Reasoning.* Composition rule 5 says the reset happens "on state entry", and phase 6 is state entry. The row cannot write a field the target omits (D131) and entry actions cannot read or write one (D130, D131), so the only thing the reset's position can change is what the row's own reads return, and §2.2 already says those reads see the source state's fields. Placing it after the row also means nothing the author did not write changes a field between the guard and the row's actions.
- *Alternatives rejected.* Keeping the reset ahead of the row and letting the row read the reset value: a silent wrong answer, and a required field without a default would have no value to read at all. Keeping it there and making the read a compile error: this removes the ordinary case of carrying a value out of a state that is about to drop it, and adds a restriction §2.2 does not state.
- *Precedent.* §2.2 D130, D143 and composition rule 5 themselves; UML state machines run a transition's effect before the target state's entry behaviour, and the target state's own setup is part of entering it.
- *Tradeoff accepted.* During phase 5 the working copy carries the target state's name while still holding values for fields that state omits. Nothing can observe that, because the working copy is not a configuration until it is committed.

**What a computed field returns when it is read during an operation.** A read of a computed field, wherever it occurs in an operation, returns the value of its `<-` expression over the working copy as it stands at that moment, with one exception: a constraint declared on a patched field reads it over the values the patch will produce (phase 1; *Known exceptions*, item 14). Phase 7 is the step that fixes the values the committed configuration carries; it is not the only moment a computed field has an up-to-date value.
- *Reasoning.* §0.4 property 3 says each assignment in a row sees the state left by all preceding assignments, and a computed field has no state of its own to be left behind (see *Configurations*). If a read returned the value from before the operation, `set A = 5 -> set B = Total` with `Total <- A + 1` would store a `Total` that matches no value `A` has in the chain, and the same expression text would mean two different things inside one chain. During construction there is no earlier value at all.
- *Alternatives rejected.* Returning the value stored at the last recomputation — it contradicts read-your-writes and forces every proof about a computed field read in a chain to carry two values for it. Forbidding a read of a computed field after a write to something it depends on — a restriction on the language that buys nothing the first choice does not.
- *Precedent.* A spreadsheet formula cell, a SQL view or generated column, a property getter: each is read as its definition over current data. §2.2 D143 already reasons about a read "through a computed-field dependency" as a read of what the computed field depends on.
- *Tradeoff accepted.* The runtime must derive a computed field on read, or re-derive after each write to something it depends on, rather than once per operation. How it does so is the evaluator's business (`runtime/evaluator.md`). And every read of a computed field is an evaluation of its `<-` expression at that position, so the expression's fault obligations (a divisor not zero, a collection not empty, and the rest) have to be discharged at each position where it is read, from what is known there (*What an earlier write invalidates*), not only once over a committed configuration. A committed configuration satisfies the rules; a working copy part-way through an operation need not, so a proof of the `<-` expression that rests on a rule does not carry over to a read after a write to a field that rule mentions. Where such an obligation cannot be discharged at a read position, the definition is rejected there as at any other position (§0.7): `rule B != 0`, `field Ratio <- 100 / B`, and a row `set B = 0 -> set X = Ratio` is refused, because the read of `Ratio` divides by a `B` the row has just set to zero. During `Create`, a read of a computed field that depends on a field with no value yet is refused wherever it occurs. Before the construction row's actions the rule in phase 0 refuses it. In the construction row's own actions, `UninitializedFieldReadInInitialAssignment` and `UninitializedCrossFieldReadInInitialAssignment` (§3A.5) refuse a read of a required field before the row's assignment of it, and a read through a computed field is such a read: `on Open -> set Note = Fee -> set Amount = Open.A`, with `Fee <- Amount * 0.1` and `Amount` required with no default, reads `Amount` before its assignment and is refused. This is the reading D143 (§2.2) states for the assignment that brings a required field back from `omit` — "directly or through a computed-field dependency" — applied to the same kind of read at construction; a computed field has no value of its own (*Configurations*), so reading it is reading what it derives from. After the construction row's actions every required field without a default has been assigned (`InitialEventMissingAssignments`), so entry actions and phase 7 have no such read to refuse. This replaces the reading in §3.5 that a computed expression is evaluated only over a final configuration: §3.5's scope rule — any field except those forming a cycle — is unchanged, because it follows from there being no evaluation cycle, not from when the evaluation happens.

**Every evaluation of `now()` in one operation returns the same instant.** [`temporal-type-system.md`](temporal-type-system.md) § `now()` states that Precept guarantees "same inputs, same outputs within a single fire"; the clock reading is therefore taken once per operation and is one of that operation's inputs.
- *Reasoning.* Without it, a guard `when now() < Deadline` could pass and a later action in the same chain could read a `now()` past the deadline, which is the same expression text giving two values with no write in between.
- *Alternatives rejected.* Reading the clock at each evaluation, which makes an operation's result depend on how long it takes to run.
- *Precedent.* SQL's `CURRENT_TIMESTAMP` is fixed for the length of a statement or transaction.
- *Tradeoff accepted.* A value written by a long-running operation carries the time the operation started, not the time the write happened.
This says nothing about whether a rule or an ensure may mention `now()`, which is an outstanding owner decision.

#### What can write during one operation

Compile-time reasoning that carries a fact forward — a guard establishes something about a field, and a later expression relies on it — depends on knowing every way that field's value can change in between. A writer that is not on this list is a fact that survives a write it should not, which is an accepted fault rather than a missed one. The enumeration is made from the language documents — this specification, [`collection-types.md`](collection-types.md) and the other type documents — and not from the catalogs.

These are the writers, in the order the phases reach them:

| Writer | Phase | Which fields it writes | Where it is declared |
|---|---|---|---|
| Default materialization | 0, `Create` only | every stored field | the `default` modifier (§2.4), applied in declaration order (§3.5) |
| Restore slot injection | 0, `Restore` only | every stored field named in the persisted data | §0.7 |
| An action in a state's exit action chain | 3 | the destinations of that action — next table | the state-action form (§2.2) |
| The state change | 4 | no field; the state part of the configuration | the `transition` outcome (§2.2) |
| An action in a transition row, construction row or stateless event row | 5 | the destinations of that action — next table | the row forms (§2.2, §3A.5) |
| An update patch | 5, `Update` only | each `editable` field the patch names; the whole value is replaced | the `editable` access mode (§2.2) |
| The `omit` reset | 6 | every field the state being entered omits — to its reset value, or, for a required field without a default, to no value (phase 6) | §2.2 composition rule 5 |
| An action in a state's entry action chain | 6 | the destinations of that action — next table | the state-action form (§2.2) |
| Computed-field recomputation | 7, every operation including `Restore` | every computed field | the `<-` declaration (§3.5) |

**Write destinations of every action form.** These are all the action forms the language permits (§2.2 grammar, §3.8 *Action statement validation*, and [`collection-types.md`](collection-types.md) § Action Summary). `F` is the action's first field; `G` and `H` are capture targets.

| Action form | `F` may be | Fields written | What the write does |
|---|---|---|---|
| `set F = Expr` | any scalar field that is not computed, optional or not | `F` | replaces the whole value with the value of `Expr` |
| `clear F` | any `optional` scalar field | `F` | `F` becomes not set (§3.8: "On `optional` fields, resets the field to 'not set'") |
| `clear F` | an `optional` `set`, `bag`, `queue`, `stack`, `list`, `queue of T by P` or `lookup` | `F` | not stated: §3.8 says `clear` on an `optional` field makes it not set, and [`collection-types.md`](collection-types.md) says `clear` on each of these kinds removes all elements, with no exception for `optional` (*Known exceptions*, item 12). Either way the write is to `F` alone, and either way it leaves nothing previously known about `F` standing |
| `clear F` | a `set`, `bag`, `queue`, `stack`, `list`, `queue of T by P` or `lookup` that is not `optional` (never a `log` kind, which is append-only — §3.8, [`collection-types.md`](collection-types.md)) | `F` | `F` becomes empty |
| `clear F` | a `log of T` or `log of T by P`, `optional` or not | — | not permitted. §3.8 lists "any `optional` field" among the valid targets and also says `clear` is "Not valid on `log of T` or `log of T by P` (append-only chronicle)"; the second is the specific statement and [`collection-types.md`](collection-types.md) repeats it with no exception for `optional` — "No removal operations exist by design. `clear`, `remove`, and all other mutation actions are type errors on a log field", and `clear LogField` is listed as a type error for both log kinds. So an `optional` log has no `clear` writer, and a fact that rests on a log only growing is not taken away by one |
| `add F Expr` | `set` | `F` | adds the element; no change if it is already present |
| `add F Expr` | `bag` | `F` | raises that element's count by one |
| `remove F Expr` | `set` | `F` | removes the element; no change if it is absent |
| `remove F Expr` | `bag` | `F` | lowers that element's count by one, removing the element at zero; no change if it is absent |
| `remove F Expr` | `list` | `F` | removes the first occurrence, moving every later element down one position; no change if it is absent |
| `remove F Expr` | `lookup` (`Expr` is a key) | `F` | removes the entry with that key; no change if it is absent |
| `remove F at N` | `list` | `F` | removes the element at position `N`, moving every later element down one position |
| `enqueue F Expr` | `queue` | `F` | adds the element at the back |
| `enqueue F Expr by P` | `queue of T by P` | `F` | adds the element with ordering value `P`; its place is decided by the ordering value and, among equals, by arrival |
| `dequeue F` | `queue`, `queue of T by P` | `F` | removes the front element (for `queue of T by P`, the best-ordered one) and discards it |
| `dequeue F into G` | `queue`, `queue of T by P` | `F` **and `G`** | removes the front element; `G`'s whole value is replaced with the removed element's value |
| `dequeue F into G by H` | `queue of T by P` | `F`, **`G` and `H`** | removes the front element; `G`'s whole value is replaced with the removed element's value and `H`'s whole value with its ordering value |
| `push F Expr` | `stack` | `F` | adds the element at the top |
| `pop F` | `stack` | `F` | removes the top element and discards it |
| `pop F into G` | `stack` | `F` **and `G`** | removes the top element; `G`'s whole value is replaced with it |
| `append F Expr` | `log`, `list` | `F` | adds the element at the end |
| `append F Expr by P` | `log of T by P` | `F` | adds the entry at the place its ordering key `P` gives it |
| `insert F Expr at N` | `list` | `F` | inserts the element at position `N`, moving the element at `N` and every later one up one position |
| `put F K = V` | `lookup` | `F` | sets key `K` to `V`, adding the entry if the key is absent and replacing its value if present |

Three things hold of every row of that table.

- **Operands are evaluated before anything is written.** The expressions an action carries (`Expr`, `N`, `K`, `V`, `P`) are evaluated against the working copy as it stands before the action; then the action's writes are made. For the capture forms, the removed element is the one at the front or top of `F` as it stood before the action. Expressions are pure (§0.4 property 6), so the order in which one action's own operands are evaluated cannot be observed.
- **A capture is a full write of a second or third field.** `dequeue Q into D` writes both `Q` and `D`; `dequeue Q into D by S` writes `Q`, `D` and `S`. Any analysis that derives write sites from an action's first field alone misses the others, and a fact about `D` or `S` will appear to survive. A capture target is held to the same restrictions as the target of `set`: it is a scalar field of the element type (for `G`) or the ordering type (for `H`), it is not computed, and it is held to whatever restriction §2.2 D131 places on a `set` of the same field at the same position. Canon states those restrictions for `set` (§3.8) and for "any write" (§2.2, D131); they apply to a capture because a capture replaces a field's whole value exactly as `set` does. D131 speaks of a field omitted in "the transition's target state"; for a write made by an exit action, whether that is the state being entered or the state being left is not stated (*Known exceptions*, item 12), and a capture in an exit action inherits the same open point. A capture also counts as the assignment of its target wherever canon requires a field to be assigned: in `InitialEventMissingAssignments` (§3A.5); in D132's requirement that the transition action chain "include a `set`" for a required field it brings back from `omit` (§2.2); and as the "first assignment" of D143, `UninitializedFieldReadInInitialAssignment` and `UninitializedCrossFieldReadInInitialAssignment`. *Reasoning:* each of those rules exists so that the field has a value before anything reads it and when the configuration commits, and a capture gives its target a whole value exactly as `set` does; the removal it captures from is proved to have an element to remove (`UnguardedCollectionMutation`, §3.8), so whenever the capture runs it writes; and D131 already writes "a `set` (or any write)", treating `set` as the representative write. *Alternatives rejected:* reading D132 literally, so that only `set` meets it, which refuses `from Idle on Next when Q.count > 0 -> dequeue Q into Current -> transition Busy` although `Current` always has a value when it commits, and forces a redundant second write. *Precedent:* definite-assignment analysis in C# counts passing a variable as an `out` argument as an assignment, exactly as `=` is. *Tradeoff accepted:* D132's own wording, and any diagnostic text that names `set`, reads narrower than the rule now applied.
- **A form that may change nothing is still a write.** `add` to a `set` that already holds the element, `remove` of an absent element and `put` of the value a key already has leave `F` as it was. Whether one of them changed anything depends on data, so reasoning that follows it has to allow for both.

The ordering-value capture exists only together with a value capture: [`collection-types.md`](collection-types.md) § Dequeue with Ordering Value Capture defines `dequeue F into G by H` as an extension of the `into` form, and § Action Summary lists no `dequeue F by H`. The grammar in §2.2 is written to match.

**This enumeration is not yet enforced.** Actions are catalogued and their write semantics are catalog metadata; the other five writers that write fields — default materialization, restore slot injection, the update patch, the `omit` reset and computed-field recomputation — are not catalogued at all — the `omit` reset in particular has no catalog member. The intended end state is that every writer is catalog-declared and that the exhaustiveness analyzer used elsewhere for catalog coverage is applied to the writers, so that adding a writer without teaching every consumer about it fails the build. Until that exists, these tables are asserted rather than kept, and any soundness argument that depends on the enumeration being complete should say so.

#### What an earlier write invalidates

Within one operation, an expression is evaluated at one position and reads the working copy as it stands there. Two occurrences of the same expression text at two positions are two evaluations, and they are the same value only if nothing either of them reads was written in between. **Identical text proves nothing by itself.** `when Q.count > 0 -> dequeue Q -> dequeue Q` evaluates `Q.count` once, in the guard; by the second `dequeue` the queue has lost an element and the guard says nothing about it. `when X != 0 -> set X = 0 -> set Y = 100 / X` divides by the `X` the first action wrote, not the `X` the guard tested. `when D != 0 -> dequeue Q into D -> set R = 100 / D` does the same through a capture.

The rule, stated once:

> Something known about the value of an expression at one position in an operation may be relied on at a later position only if no write between the two positions changes any field the expression reads. A field is read through a computed field as well as directly: a write to `A` is a write to everything any computed field derives from `A`.

What follows is that rule applied to each thing an operation can know and each thing that can write.

**What is known, and from where.**

- *The rules, and the `in` ensures of the starting state.* Every rule holds of the starting configuration of `Fire` and `Update`, because it was committed (§0.7), and so does every `in` ensure of the state that configuration is in whose guard holds of it (*Handlers*, what a handler must make true). Each says something about the fields it reads as they stood at phase 0 — the fields it mentions, and every field a computed field it mentions derives from (the rule above). After a write to any of those, what it said is no longer known of the working copy: `rule Gap > 0` with `Gap <- A - B` says nothing about `Gap` after `set A = B`. (That it holds again at commit is what the compiler proves; it is not something a mid-operation expression may assume.)
- *The rules, and the `in` ensures of the persisted state, at `Restore`.* `Restore`'s starting configuration is one an earlier operation committed — the one precondition of §0.7 (*Configurations*) — so every rule, and every `in` ensure of the persisted state whose guard holds of it, is known of it, exactly as for the starting configuration of `Fire` and `Update`. The only expressions `Restore` evaluates are the computed fields' `<-` expressions at phase 7, after no write to a stored field, so all of it is still known there. A `<-` expression whose safety rests on a rule is therefore safe to evaluate at `Restore`'s phase 7. If the precondition is false the guarantee does not hold (§0.7); that is the host's responsibility, not a position the compiler has to prove anything about. What `Restore` returns when, for that reason, a `<-` expression cannot be evaluated is not stated (*Known exceptions*, item 12).
- *What default materialization gave each field, at `Create`.* Phase 0 is a write like any other (*What can write during one operation*), and what it establishes is known afterwards: each stored field holds the starting value phase 0 gave it — the value of its `default` expression, not set for an `optional` scalar field without a default, empty for a non-optional collection without a default. What is known of that value is lost, as any write's result is, only when a later write changes that field. A `default` that reads other fields also establishes a relation — the field equals its `default` expression — and that relation, like the relation a `set` establishes (*What each writer takes away*), is lost when a later write changes a field the expression read; what was known of the field's own value stays. With `field A as integer default 5` and `field B as integer default A + 1`, `B` is 6 at every position of `Create` until something writes `B`, even after `set A = …`; only "`B` equals `A + 1`" is gone. With `field B as integer default 4 positive`, `B` is 4 at every position of `Create` until something writes `B`, so `100 / B` in a construction row's action, in an initial-state entry action or in a computed field's `<-` expression at phase 7 is safe. The rules are *not* known during `Create`: there is no committed starting configuration, and that construction makes them true is what the compiler proves of the configuration `Create` commits (§0.7, base case), not something an expression evaluated before the commit may assume. A field modifier is a rule (§2.4), so a modifier is known of a field during `Create` only through the value the field was given — a default that satisfies the modifier, or an argument whose declared constraints imply it.
- *Ingress constraints on arguments.* They hold of the arguments from phase 1 on. Arguments are never written, so anything known about arguments alone holds for the whole operation. This includes the `on <Event> ensure` declarations: each unguarded one was true at phase 1, of the arguments and of the fields it reads as they stood in the starting working copy; of a guarded one, what was true at phase 1 is that its guard was false or its body was true (phase 1, *A guarded event ensure*), so its body is known at a later position only together with a fact, known there, that its guard was true at phase 1. What either says about fields is lost, as a guard's is, after a write to one of them.
- *Ingress constraints on patched values, at `Update`.* Phase 1 checks each patched value against the constraints declared on the field it enters, and refuses the operation if any fails. It evaluates each of them against the values the fields will hold once the patch is written (phase 1, *Which values a patched field's own constraints read*). So after the patch writes the fields at phase 5, every constraint declared on a patched field is known of the configuration the patch produced, including one that reads another field — the patch write establishes it, together with the values. Nothing after the patch writes a stored field in an update handler, so it stays known to the end. What else phase 1 evaluates for `Update` is the outstanding owner decision of *Known exceptions*, item 11; this list states nothing that depends on its answer.
- *The row's guard.* It is true of the starting working copy and the arguments at phase 2. It says nothing about a field after that field is written.
- *The guards of the earlier candidate rows.* Dispatch is first-match (phase 2), so a row runs only when the guard of every candidate row declared before it was false. Each of those guards is false of the starting working copy and the arguments, and that is known at the winning row exactly as its own guard is, and lost in the same way: after a write to a field an earlier guard read, its falsity says nothing about that field. This is the fact that lets `when Counter >= MaxCount -> reject …` followed by `-> set Counter = Counter + 1` prove the increment stays below the cap (`../compiler/proof-engine.md`, cross-row interval composition). It holds only for rows declared *before* the winning row: a row declared after it is never reached when the winning row's guard is true, so its guard being false is not known there, and with the two rows above in the opposite order the increment has no proof. It holds only among the rows that are candidates in the same dispatch (phase 2): for `Fire` on a stateful precept, the rows for the same event whose `from` names the same source state; for `Fire` on a stateless precept, the event's `on <Event>` rows; for `Create`, the construction rows. For a row written with `any` or a comma list, the earlier candidates are those of the source state of each of its handlers. Construction rows and a stateless precept's event rows have no source state, and the fact holds among them in the same way, because their dispatch is first-match exactly as a stateful precept's is (phase 2).
- *A state action's guard, true or false.* A guarded state action divides the handler into two paths from its turn on (*What each writer takes away*). On the path on which it ran, its guard is true at the moment its turn came, of the working copy as it then stood. On the path on which it did not run, its guard is false at that moment, and that falsity is known for everything after it on that path, exactly as the falsity of an earlier candidate row's guard is known at the winning row. Either way the fact is lost, as any guard fact is, after a write to a field the guard read. Neither is known in a handler in which that state action has no turn.
- *An earlier action.* After `set F = Expr`, `F` equals what `Expr` evaluated to. After a capture, the capture target equals what was removed, and what was known of the removed element carries to it (*What each writer takes away*, a write that replaces a whole value). After the collection actions, what the table above says the action does is true of `F`. After an update patch write, `F` equals the patched value. What an action establishes is stated in terms of what was known immediately before it: `set X = X + 1` makes `X` one more than it was, so a range known for `X` before the action is known after it, shifted by one; the collection actions do the same for `count` and for membership (*What each writer takes away*). This is not an exception to the rule above — the old fact about `X` is lost, and a new one, derived through the write, takes its place.

These are the four classes of premise of §0.7 — the classes the owner's want document names (`what-i-want-2026-07-16.md:19`) — and what the handler's own writes establish, spelled out position by position; the list adds no class. Field modifiers (class a) are rules (§2.4) and come in with the rules — at `Fire`, `Update` and `Restore`, where the rules are known of the starting configuration — and, at `Create` and for a patched field, through the value a write gave the field, as the two entries above say. Default materialization and the update patch are among the handler's own writes (*Handlers*: default materialization is the first part of a construction handler, the patch write the first part of an update handler), so what they establish is in the last group, not a new class; so is the starting configuration of `Restore`, which is a committed configuration and is known exactly as the starting configuration of `Fire` and `Update` is. The `in` ensures of the starting state belong to class (d), "the rules that already hold in the pre-state": an `in` ensure is a constraint every handler that commits in that state must make true (*Handlers*), so it holds of a committed starting configuration exactly as a rule does. The falsity of the earlier candidate rows' guards and a state action's own guard, true on one path and false on the other, belong to class (c), the handler's guard: a row is only reached when the earlier guards were false, so the condition under which the handler runs is its own guard together with their negations, and a state action's guard, or its negation, is part of the condition under which each path of the handler runs. What an earlier action established is not a premise the author declared; it is a consequence of the handler's own writes, which every proof through a chain uses (§0.6, *Proof philosophy*, item 7, sequential proof flow). What this subsection adds to §0.7 is how long each of them stays known.

**What each writer takes away.**

- *A write that replaces a whole value* — `set F`, `clear` of an optional field (for an `optional` collection, whichever of the two readings in the table above is meant, since both leave nothing earlier standing), a capture into `G` or `H`, the `omit` reset, an update patch value — removes everything previously known about `F` (or `G`, or `H`), whatever its source: guard, rule, modifier-derived range, presence, or an earlier action. What is known afterwards is only what the write itself establishes. What `set F = Expr` establishes is of two kinds. Whatever was known of the value `Expr` had at that position — a range, presence, a sign — becomes known of `F`, and stays known until `F` itself is written, because the expression `F` reads only `F`. The relation "`F` equals `Expr`" is lost when a later write changes a field `Expr` read, because `Expr` would now evaluate to something else; losing it takes nothing away from what was already known of `F`'s value. A capture establishes the same two kinds of fact about its target. Whatever was known, immediately before the action, of the element it removes becomes known of `G` — what was known of `.peek` of the queue or stack, every `each x in F (Pred)` known of `F` applied to that element (and, through `no x in F (Pred)`, the negation of `Pred`), and the bound an inner-type value modifier gives every element ([`collection-types.md`](collection-types.md) § Element value modifiers) — and, for `dequeue F into G by H`, whatever was known of `.peekby` becomes known of `H`, with an `each` over the pair applied to the pair `G`, `H`. These stay known until `G` or `H` itself is written, even though the same action takes away everything known about `.peek`, `.peekby` and every quantifier over `F` (the next item), because they now describe a value held in another field. So `when Q.count > 0 and Q.peek != 0 -> dequeue Q into D -> set R = 100 / D` is safe, and `rule each x in Q (x > 0)` with `field D as integer positive` is preserved by `dequeue Q into D`. *Reasoning:* a capture replaces its target's whole value exactly as `set D = Q.peek` would, and `set` already carries what was known of its expression to its target; the removal is proved to have an element to remove (`UnguardedCollectionMutation`, §3.8), so the captured value is always that element. *Alternatives rejected:* carrying only the bare equality with "what was removed", which, once the action has taken away every fact about `Q`, leaves nothing known of `D` and rejects both definitions above although neither can fail. *Precedent:* the `set` rule just above; an assignment from an array element in program verification carries what was known of that element. *Tradeoff accepted:* the proof engine has to read the facts about the removed element before it applies the removal's own loss of facts about `F`.
- *A write that changes a collection in place* — every other action form — removes everything previously known about every expression that reads that collection, as the rule above says: its `count`, membership (`contains`), every accessor (`.peek`, `.peekby`, `.min`, `.max`, `.first`, `.last`, `.at(N)`, `.countof(E)`, `F for K`), every quantifier over it, and, for an `optional` collection, whether it is set (what an element action does to an `optional` collection that is not set is not stated — *Known exceptions*, item 12). What is known afterwards is what the action's definition, in the table above, establishes from what was known immediately before it (*What is known*, an earlier action). That is the full proof power the design intends, and it is all of the following:
  - *The change to `count`.* Each form changes `count` in the way its row of the table says: by one for a form that always adds or always removes one element (`add` to a `bag`, whose `count` counts every copy — [`collection-types.md`](collection-types.md) § bag; `enqueue`, `push`, `append`, `insert`, `dequeue`, `pop`, `remove … at`), and by one or not at all for a form that may change nothing (`add` to a `set`, `remove` of an element, `put`). A range known for `count` before the action is carried through it, shifted by that change: `when C.count < 5 -> add C x -> add C y` keeps `C.count <= 6` known after both, and `when Q.count >= 2 -> dequeue Q -> dequeue Q` keeps `Q.count >= 1` known before the second `dequeue`, so both are safe. How the proof engine carries this is `../compiler/proof-engine.md`, *Strategy 10: Count Containment Proof*; §0.6 records the cases it does not yet carry, which are unbuilt proof power, not a narrower design.
  - *The element or key the action names.* After any form that adds one element — `add` to a `set` or `bag`, `enqueue` (with or without `by`), `push`, `append` (with or without `by`), `insert` — the collection contains that element; after `append F Expr by P` on a `log of T by P` it also contains the ordering key `P`, which is the membership `contains` tests with a `P` operand and the precondition of the next `append … by` ([`collection-types.md`](collection-types.md) § Membership Operator). After `remove` of an element from a `set`, it does not contain it. On a `bag`, `add` raises `.countof(E)` of the element named by one, and `remove` lowers it by one when it was at least one and leaves it at zero otherwise. After `put F K = V`, `F` has key `K` and `F for K` is `V`; after `remove` of a key from a `lookup`, it does not have that key.
  - *What the action leaves alone.* These hold of what was known immediately before the action:
    - A form that names one element or key — `add`, `remove` of an element, `remove` of a key, `put`, and `append … by P` for its ordering key — leaves unchanged the membership of every element or key known to be different from the one it names, the `.countof` of every such element of a `bag`, and the value `F for K` of every such key of a `lookup`.
    - A form that adds and removes nothing — `add`, `enqueue`, `push`, `append`, `insert`, and `put`, which replaces at most the value of the key it names — leaves every element (and every ordering key of a `log of T by P`, every key of a `lookup`) that was known present still present.
    - A form that removes and adds nothing — `remove`, `remove … at`, `dequeue`, `pop`, and `clear` of a collection that is not `optional` — leaves every element and key that was known absent still absent.
  - *Positions and accessors.* After `enqueue F Expr` on a `queue` and `append F Expr` on a `list` or `log of T`, the element added is the last: `.last` is `Expr` where the kind has `.last`, and, when `F` was known not to be empty, `.peek` and `.first` are what they were and `.at(M)` is what it was for every `M` below the old `count`; when `F` was known to be empty, `.peek` and `.first` are `Expr`. After `push F Expr`, `.peek` is `Expr`. After `insert F Expr at N`, `.at(N)` is `Expr`, `.at(M)` is what it was for every `M` below `N`, and `.at(M + 1)` is what `.at(M)` was for every `M` from `N` up to the old `count` minus one. After `remove F at N` on a `list`, `.at(M)` is what it was for every `M` below `N`. After `add F Expr` on a `set` whose element type is orderable, `.max` is the greater of `Expr` and the old `.max` and `.min` the lesser of `Expr` and the old `.min`, when `F` was known not to be empty, and both are `Expr` when it was known to be empty. After `clear F` that empties `F`, `count` is 0.
  - *Quantifiers.* Every `each x in F (Pred)` and `no x in F (Pred)` that was known before a form that removes and adds nothing (listed above) is still known after it, because every element afterwards was an element before. After a form that adds one element and removes none, other than `put`, an `each x in F (Pred)` that was known before is known after when `Pred` is known to hold of the element added, a `no x in F (Pred)` likewise when `Pred` is known to be false of it, and an `any x in F (Pred)` that was known before is known after; for `queue of T by P`, whose binding variable has `.value` and `.by` ([`collection-types.md`](collection-types.md) § Quantifier Predicates on Ordering Axis), "the element added" is the pair of `Expr` and its ordering value. After `clear F` that empties `F`, every `each` and `no` over `F` is true and every `any` over `F` is false.
  Nothing else about the collection is known after the action: in particular not `.peek` or `.peekby` of a `queue of T by P` after `enqueue … by`, nor `.first`, `.last` or `.at(M)` of a `log of T by P` after `append … by`, whose place depends on the ordering value; not `.min` or `.max` after a removal; not the position of an element after `remove` of an element from a `list`; not the front or top after `dequeue` or `pop` beyond what `count` says; and not an `any` over `F` after a removal or after `put`. A fact of these kinds that a definition needs is restored by a guard or a later action, or the definition is rejected.
  - *Reasoning.* This list has to state the full proof power the design intends, so it has to say what each form carries and stop there; the earlier closing sentence — nothing else is known "unless the action's definition gives it" — left two readings of the same definition, one accepting and one rejecting. Each fact above follows from the row of the action table that defines the form and from the meaning of the accessor or quantifier in [`collection-types.md`](collection-types.md), with no other premise: an element that was added is present; an action that names one element or key does nothing to the others; an action that only adds cannot make an element absent, and one that only removes cannot make one present; an element added at the back or top does not move the front; a quantifier over a collection that only lost elements still holds of each that is left. The quantifier facts are needed for the inductive step itself: `rule each x in Q (x > 0)` is preserved by `enqueue Q E.Amount` only if the rule known before and `E.Amount > 0` together carry it through, and [`collection-types.md`](collection-types.md) § Element value modifiers already carries a per-element bound through an element write in the same way. The facts left out are those whose value after the action depends on data the proof does not have — which element a `by` ordering puts in front, which element a removal took from the middle of an order.
  - *Alternatives rejected.* Carrying membership only, as the earlier text did: it rejects `append Log "a" by Rec.A -> append Log "b" by Rec.B` behind a guard that proves both keys absent and distinct, which cannot fail, and rejects every `each` rule over a collection that is ever added to. Carrying every fact the action does not visibly contradict: unsound for the order-dependent accessors listed as not carried.
  - *Precedent.* The read-over-write rules for arrays in program verification (McCarthy's `select` and `store`), which carry what is known of every index other than the one written; the frame rule of separation logic; and Dafny's treatment of sequence append and set union, which carries a `forall` over the old contents to the new contents given the new element.
  - *Tradeoff accepted.* The proof engine has to carry these facts form by form, and `../compiler/proof-engine.md` records which of them it does not yet carry (§0.6); each such case is unbuilt proof power, not a narrower design. An author whose definition needs one of the facts not carried has to write a guard for it.
- *A write that may not happen* — an action inside a guarded state action. Whether it runs depends on that state action's guard, evaluated at its turn, so from that turn the handler has two paths: one on which the guard held and the state action's actions ran, and one on which the guard was false and nothing was written. What is known on each path is what happened on it. On the first, the guard (as of its turn) and what the actions established. On the second, everything that was known before the state action, unchanged, together with the guard's falsity (*What is known*). An expression after it is safe only if it is safe on every path that reaches it, from what is known on that path. Several guarded state actions in one handler give one path for each combination of them that runs. So `to B when D == 0 -> set D = 1` followed by `to B -> set R = 100 / D` is safe: on one path `D` is 1, on the other `D != 0`.
  - *Reasoning.* A handler is "one complete way an operation can run" (*Handlers*); with a guarded state action the operation can run two ways, so they are analysed as two. Each path is a sequence with no branch in it, so §0.4's linear walk holds of each path and nothing is merged. The path that skips the state action is reached only when its guard was false, exactly as a later row is reached only when the earlier rows' guards were false, and canon already counts that falsity as a premise of class (c); leaving it out would state less proof power than the design intends and reject definitions that are safe on every execution.
  - *Alternatives rejected.* Merging the two paths after the state action and keeping only what was known before it minus its destinations (the earlier text here): it is the join §0.4 property 3 says the language does not have, and it rejects the definition above although it cannot fail. Merging and keeping an "either–or" fact: the same proof power as following each path, but it needs a join and facts with alternatives in them that the proof model carries nowhere else.
  - *Precedent.* Path-by-path analysis of code without loops, as in symbolic execution and in bounded model checking; within this language, one handler per source state for a `from any` row (*Handlers*) and the falsity of earlier rows' guards at first-match dispatch.
  - *Tradeoff accepted.* The number of paths doubles with each guarded state action that can run in one handler. It is finite, because state actions are declared statically and nothing loops (§0.4 properties 1 and 5); where it exceeds the hard cap that §0.6, *Proof philosophy*, item 3, requires of any procedure that could grow without bound, the obligation is unresolved and the definition is rejected, with the explanation that item requires. No value for that cap is stated in canon, for this procedure or any other, so canon does not yet say how many guarded state actions one handler can carry before this happens.
- *Computed-field recomputation* writes only computed fields, and gives each the value a read of it would already have returned (see *What a computed field returns when it is read during an operation*). It changes nothing a later expression could have known, and nothing in the operation is evaluated after it.

**What sits between the guard and each later position.** For a `transition` row, the writes between the row's guard and the row's first action are the source state's exit actions — all of them, including guarded ones. A fact the guard established about a field an exit action writes is not available to the row's actions. The `omit` reset writes the fields the target state omits. Neither the row's actions nor the entry actions can read one of those directly (§2.2, D130 and D131), but by the rule above the reset is also a write to every computed field that derives from one of them, so a fact about such a computed field does not survive it; and what a read of such a computed field means in a state that omits the field it derives from is not stated (*Known exceptions*, item 12). The entry actions run after every write of the exit actions, the row and the reset.

What an exit or entry action may rely on is what is known at its position on each path of the handler it is running in — the row's guard, the falsity of the earlier candidate rows' guards, the guards of the earlier guarded state actions, true or false according to the path, the rules and the `in` ensures of the starting state (for a construction handler, which has no starting configuration, what default materialization gave each field instead), and earlier actions — each kept only for the fields nothing has written since, exactly as for the row's own actions. There is no separate rule for state actions. Because a state action runs in every handler that leaves or enters its state, an expression in it must be safe from what is known at its position in each of those handlers (*Two things are not handlers*): a fact that one handler supplies and another does not discharges nothing. So with `B` not the initial state, `from A on Go when D != 0 -> transition B` as the only row that enters `B`, nothing writing `D`, and `to B -> set R = 100 / D`, the division is safe; add a second row into `B` without that guard and it is not. Make `B` the initial state and it is not either, whatever the rows say: every construction handler enters the initial state and runs its entry actions (*Construction enters the initial state*), so a `Create` on which nothing establishes `D != 0` — a parameterless one with `D` defaulting to 0, for instance — is a further handler in which the division sits, and it has no row guard at all. (Earlier text here said entry actions could never rely on the row's guard. That contradicted the rule above and the definition of a handler, which contains the row's guard, and it gave up proof power the design is meant to state in full.) State actions read fields only, never arguments (§3.5), so what the guard says about arguments alone does not reach them. For a `no transition` row, a construction row and a stateless event row, nothing sits between the guard and the row's first action.

**Between operations nothing is carried except the rules.** What an operation's guard or actions established is gone when it commits. The next operation starts knowing of its configuration only what every reachable configuration satisfies: the rules, the `in` ensures of its state whose guards hold, and the field modifiers, which are rules (§2.4). A `to` ensure is not among them: it holds of the configuration a transition into the state committed, and a later `Update` or `no transition` row in that state need not keep it (*Handlers*, what a handler must make true). A `from` ensure is not among them either: nothing makes it true of a configuration until the entity leaves the state.

#### Known exceptions

Each of these departs from the general statement above, and is written here so that no reader has to discover it.

1. **`Restore` checks no constraint and runs no row.** It has no ingress, no dispatch, no actions and no `omit` reset; access modes are bypassed. It does recompute computed fields. The guarantee for a restored configuration rests on the one precondition of §0.7.
2. **`Create` on a precept with no initial event cannot refuse.** It takes no input, so phases 1 and 2 have nothing to do. The runtime signature accepts arguments whether or not an initial event is declared (`runtime/runtime-api.md` § Precept, `Create(JsonElement? args = null)`), and arguments a caller passes to this `Create` are not read and are not checked: phase 1's "unknown key" check tests each name against the arguments the event declares, and here there is no event, while §3A.5 and `runtime/runtime-api.md` § Create both say this `Create` is parameterless and always returns `Created`. `InspectCreate` does the same; the compiler guarantees every field has a starting value — a default, `optional`, computed, or a collection (§3A.5, and *Configurations* for collections).
3. **A construction with no matching row is `Rejected`, not `Unmatched`** (§3A.2). `Fire` with no matching row is `Unmatched`.
4. **The initial event cannot be fired again.** `Fire` with the initial event's name ends at the start of phase 1 with `UndefinedEvent`, before its arguments are checked, on a stateful and a stateless precept alike (§3A.5, *Fire-once guarantee*).
5. **A `reject` row evaluates its reason string.** It is the one thing a refused operation evaluates after its guard, and it runs only for the row that won.
6. **A `no transition` row runs no state action and no `omit` reset**, even though it commits writes (§3A.2).
7. **`Create` runs entry actions and no exit actions**, and its `omit` handling is part of phase 0, not phase 6: phase 0 gives each field the initial state omits its reset value.
8. **Overflow.** The compiler does not yet prove the absence of arithmetic overflow, and the runtime carries no guard against it (§0.7). Every statement here that an evaluation is safe excludes overflow until that is built.
9. **The clock is an input.** An operation that reads `now()` gives a result that depends on when it runs, so a preview and the operation that follows it can differ for that reason alone (§3A.6).
10. **A preview given fewer inputs than the operation needs** reports parts of its answer as undetermined instead of evaluating them (§3A.6).
11. **A direct edit's check is undecided.** What `Update` evaluates at phase 1 beyond the constraints declared on each patched field waits on an outstanding owner decision; [Open Questions](#open-questions--implementation-notes) records it, with the want document's reading as the working assumption until he confirms it. The order of phases for `Update`, and which values the patched fields' own constraints read (phase 1), do not depend on the answer, and nothing in this section does.
12. **Not stated anywhere in canon, and not settled here:**
    - what `dequeue F into X by X` means when the element type and the ordering type are the same and one field is named for both captures;
    - at what moment a `default` expression that reads other fields is evaluated when the `omit` reset restores it;
    - what a rule, an ensure or a computed expression that mentions a field means in a state that omits that field — including an `on <Event> ensure` that reads a field, which phase 1 evaluates in whatever state the entity is in, so in a state that omits the field it reads a field that is not there, and §2.2 D130 lists only state-anchored contexts;
    - what an `optional` collection without a `default` starts as — not set, because it is `optional`, or empty, because it is a collection — what an element action (`add`, `enqueue`, `push`, `append`, `put`, `insert`) does to an `optional` collection that is not set, and what `clear` does to an `optional` collection (§3.8 says not set; [`collection-types.md`](collection-types.md) says it removes all elements). [`collection-types.md`](collection-types.md) says `optional` applies to collections and that an `optional` field needs an `is set` guard "before use"; if an element action is a use, an `optional` `queue of T by P`, `log of T by P` or `lookup`, which cannot take a `default`, could never become set;
    - which configuration of a `transition` handler a `from <State> ensure` must hold of: the starting configuration, the working copy after the exit actions, or the working copy after the row's actions (*Handlers*);
    - whether a required field without a default may be omitted by the initial state, and if so whether the construction row must assign it — `InitialEventMissingAssignments` (§3A.5) makes no exception for it — or may not write it, as §2.2 D131 says of a transition's target state; and, with that, whether a construction row may write any field the initial state omits;
    - what `Restore` does with persisted data that gives no value for a stored field the persisted state does not omit;
    - what a restored entity holds in the storage of a field its persisted state omits, which is what that field holds when a later transition brings it back;
    - what `Restore` returns when a computed field's `<-` expression cannot be evaluated over the persisted values — data written under an earlier definition can break a rule the current definition's proof of that expression rests on (`rule B != 0` with `Ratio <- 100 / B`, and a persisted `B` of 0). Canon says restore must not re-check such data and that the host has stepped outside the guarantee (`runtime/runtime-api.md` § Restoration), but `Restore`'s outcomes are only `Restored` and invalid input (§3A.2), neither of which fits, and there are no runtime fault traps (§0.7);
    - whether a computed field may be omitted by a state — `in <State> omit all` names every field and the grammar makes no exception for computed fields (§2.2) — and, if it may, what it holds while omitted: *Configurations* says an omitted field has no value and that every computed field has the value of its `<-` expression, phase 6 gives a reset value only to stored fields, and phase 7 re-derives every computed field;
    - which state's `omit` restricts a write made by an exit action: §2.2 D131 refuses a write to a field omitted in "the transition's target state", which for an exit action would refuse the write in every transition out of the state into one that omits the field, even though the `omit` reset then discards it; and whether an exit action may write a field the state being left omits, which D131 does not mention — the same question covers a capture target (*Write destinations of every action form*);
    - what an argument's `default` expression may read — only literals, or also fields, and if fields, which configuration (phase 1, *An argument with a `default` that the caller leaves out takes its default*); §3.5's expression-scope table gives a scope for a field's default and none for an argument's;
    - whether a constraint declared on an event argument (a modifier such as `max Balance`) may read a field at all. §3.5's expression-scope table gives "modifier value expressions" "all field names", on the grounds that a modifier is rule shorthand, and does not distinguish a modifier on a field from one on an argument; but the want document says ingress for an argument is "its modifier-rules; anything relational an event needs lives in its guards" (`what-i-want-2026-07-16.md:143`), which is the same sentence that leaves the next question open, and it leaves this one open in the same way. Every statement in this section and §3A.5–§3A.6 about an argument constraint that reads a field — phase 0's refusal of a read of a required field, and, at `Fire`, phase 1's use of the starting state's rules and `in` ensures as premises for an argument constraint — says what follows if such constraints are allowed; if they are refused, at `Create` and `Fire` each of those statements applies only to `on <Event> ensure` declarations, and only if the next question is answered yes (at `Update` they apply to the constraints declared on patched fields, which read fields whatever the answer);
    - whether an `on <Event> ensure` may read a field at all. §3.5's expression-scope table gives every ensure condition "all field names", and §3.5 lets an event ensure name the event's arguments; but §3A.1 calls it "event-argument truth" that "validates the caller-provided event arguments", §3A.3 says event ensures "target event arguments", and the want document says ingress for an argument is "its modifier-rules; anything relational an event needs lives in its guards" (`what-i-want-2026-07-16.md:143`). Every statement in this section and §3A.5–§3A.6 about an event ensure that reads a field — phase 0's refusal of a read of a required field, what such an ensure makes known (*What is known*), the item above on a field a state omits, why `Create` builds the hollow version before checking arguments, and the event previews `InspectUpdate` runs — says what follows if such ensures are allowed; if they are refused, each of those statements has nothing to apply to.

    No analysis may assume an answer to any of them.
13. **A guarded state action gives a handler more than one path.** §0.4 properties 2 and 3 say a transition row is a flat sequence with no branch and no point where two paths rejoin. That holds of every row's own action chain, and of each path through a handler, but a handler that contains a guarded state action runs along one of several paths — one for each combination of its guarded state actions whose guards hold. The compiler follows each path separately and never merges them, so the linear walk of §0.4 and of §0.6, *Proof philosophy*, item 7, applies to each path, and an expression is safe only if it is safe on every path that reaches it (*What each writer takes away*, a write that may not happen). Nothing else in a handler branches: a row's guard is decided before the handler starts (phase 2), and a conditional expression yields one value without dividing the handler (§0.4 property 2).
14. **A patched field's constraint reads a computed field over values that are not yet in the working copy.** Everywhere else a read of a computed field returns its `<-` expression over the working copy as it stands at the read (*What a computed field returns when it is read during an operation*). At `Update`'s phase 1, a constraint declared on a patched field reads a computed field over the patched values together with the unpatched starting values, because that is the configuration the constraint is checked of (phase 1, *Which values a patched field's own constraints read*). `InspectUpdate` does the same.
15. **Ingress does not evaluate a constraint whose proof relied on a phase-1 constraint that failed** (phase 1). Collect-all reports every check that could be made; such a constraint is not one of them.

### 3A.5 Entity Construction

Construction is modeled as an **initial event** — the precept's constructor. This solves the fundamental problem that entities with required fields (non-optional, no default) cannot be constructed parameterlessly: the author would be forced to either invent nonsense defaults or make things optional that should not be.

#### Event declaration with `initial` modifier

```precept
event Create(ApplicantName as string, Amount as currency in USD, CreditScore as integer) initial
```

The `initial` modifier on an event declaration designates it as the construction event. Only one event per precept may carry `initial`. The modifier appears after the parameter list (or after the event name if parameterless): `event <Name>[(args)] initial`.

#### Construction row syntax

Construction rows use the `on <EventName>` form — NOT the `from <State> on <EventName>` transition row form. The entity does not yet exist in any state when the initial event fires; the `from` prefix would be misinformation.

```precept
// Construction row — success path (sets initial field values)
on Create
    -> set ApplicantName = Create.ApplicantName
    -> set RequestedAmount = Create.Amount

// Construction row — guarded success path
on Create when Create.Amount > '0.00 USD'
    -> set ApplicantName = Create.ApplicantName
    -> set RequestedAmount = Create.Amount

// Construction row — reject path
on Create when Create.Amount <= '0.00 USD'
    -> reject "Loan amount must be positive"

// Construction row — unconditional reject fallback
on Create
    -> reject "Invalid construction arguments"
```

Construction rows are NOT transition rows. Key distinctions:

| Property | Construction row (`on Event`) | Transition row (`from State on Event`) |
|----------|-------------------------------|----------------------------------------|
| Prefix | `on <InitialEvent>` | `from <State> on <Event>` |
| `transition` allowed | No — structurally excluded by grammar | Yes |
| `no transition` allowed | No — structurally excluded by grammar | Yes |
| When fires | At entity creation (before entity exists) | After entity exists, in a specific state |
| Result on success | `EventOutcome.Created` | `EventOutcome.Transitioned` or `Applied` |

The type checker classifies a row as a construction row when `resolvedEvent.IsInitial` is true. This is a semantic classification — the parser produces the same `EventRow` construct for both construction rows and stateless event handlers.

#### Construction Idioms

Precept has two first-class construction idioms. The choice is domain-driven: whether the entity has meaningful governed existence before the data that makes it fully identified or ready for activation is available.

| Idiom | Shape | Appropriate when | Example |
|-------|-------|------------------|---------|
| **Constructor with existential fields** | Declare `event Create(...) initial` and populate birth-time data through `on Create` construction rows. | The entity cannot meaningfully exist without specific values at birth. Those values are existential requirements, not later enrichment. | `LoanApplication` is a natural fit when applicant identity and requested amount are constitutive at intake. |
| **Free construction + governed draft state** | Omit an initial event, let parameterless `Create()` materialize the initial draft state, allow progressive enrichment through `editable` declarations, and enforce readiness on a later activation event such as `Publish` or `Submit`. | The domain has a real draft or pending phase in which data is accumulated progressively before activation. | `inventory-item.precept`, where `Unlisted` is initial and `Publish` is the activation gate. |

Pattern B is not an ungoverned loophole. The entity is governed from birth by the same type constraints, rules, and state-scoped ensures that apply to its initial state; what changes is that the required truth is lifecycle-appropriate to a draft. Pattern B therefore requires birth-time-absent fields to be optional or defaulted, while Pattern A uses the initial event to make existential intake requirements structural.

#### Construction semantics

> **Draft 2026-11-25** — under adversarial review; nothing may cite this section until the mark is removed.

The `initial` modifier on an event designates it as the construction event. The runtime's `Create(args)` operation runs this event once, atomically, as part of entity creation. It runs it itself, through the same phases `Fire` runs for any other event; it does not call `Fire`, which refuses the initial event's name (§3A.4 phase 1):

1. Build a hollow version (defaults applied, initial state set, omitted fields structurally absent) — §3A.4 phase 0, default materialization.
2. Run the initial event with the caller's args through the same phases as any other event — the same ingress validation of the supplied arguments (phase 1), the same first-match guard selection, here among the construction rows (phase 2), and the same action chain (phase 5).
3. Run the initial state's entry actions (`to <InitialState> -> …`), in declaration order, after the matched row's action chain — §3A.4 phase 6. Construction enters the initial state; see §3A.4, *Construction enters the initial state*.
4. Recompute computed fields and commit (phases 7 and 8), and return the outcome — same verdict space the caller uses for every event (see §3A.2).

The steps and their order are stated in full in §3A.4, *Operation execution order*; this list is that order read for construction. The hollow version is built before the arguments are checked. *Reasoning:* every operation runs phase 0 before phase 1 (§3A.4), and `Fire` and `Update` also start from their working copy before ingress. Defaults never read arguments and ingress writes nothing, so for checks that read only arguments the two orders cannot be told apart. The order matters only for an `on <InitialEvent> ensure` that reads a field, which then has a value to read; whether an event ensure may read a field is not settled (§3A.4, *Known exceptions*, item 12), and this order is right under either answer. *Alternatives rejected:* checking arguments first, which `runtime/runtime-api.md` and a sentence below once stated — it makes construction the one operation whose phases run in a different order, and, if event ensures may read fields, leaves one that reads a defaulted field with nothing to read. *Precedent:* a constructor's field initialisers run before its body in C# and Java. *Tradeoff accepted:* a construction that is refused at ingress has evaluated the defaults for nothing.

If the precept does not declare an initial event, `Create()` is parameterless and always succeeds (the compiler guarantees every field has a starting value — a default, `optional`, computed, or a collection, which starts empty (§3A.4, *Configurations*) — enforced by `RequiredFieldsNeedInitialEvent` / `InitialEventMissingAssignments`). It still enters the initial state, so the initial state's entry actions run against the default values before computed fields are recomputed.

**Construction-time constraint composition.** Constraints compose at construction with no new language surface, and they compose the same way they do for every other event — which means there is exactly one place a constraint is *evaluated* at construction, and everything else is proven before the entity exists.

1. **At ingress, evaluated.** The caller's arguments are held to the constraints declared on them — the modifiers on each arg and any `on <InitialEvent> ensure ...` — before any construction row is tried and before any action derives a value from them. This is collect-all (§3A.1) as §3A.4 phase 1 defines it: every failure the check finds is reported, not the first. A failure here is `InvalidArgs`; nothing was mutated, so nothing is rolled back.
2. **Everything else, proven.** Global rules (`rule ...`), entry ensures (`to <InitialState> ensure ...`) and residency ensures (`in <InitialState> ensure ...`) are the base case of the induction in §0.7: the compiler proves that the default configuration and every initial event *establish* them, from the constraints declared on the initial event's arguments. What construction commits is the result of the initial event's actions followed by the initial state's entry actions (step 3 above), so the base case covers the entry actions too — on a parameterless `Create`, the defaults followed by the entry actions. A definition whose initial event cannot be shown to establish them is rejected before any instance exists. There is no step between the action chain and the commit at which they are re-evaluated, because the compiler already proved the initial event establishes them — which is why `ConstraintsFailed` is not in construction's outcome space (§3A.2).

No special "construction constraint" form is needed. `to <InitialState> ensure` remains the natural way to write a construction-time truth — it is the truth that must hold on entry into the initial state, which is exactly what construction produces. What changed is not where the author writes it but who discharges it: the compiler, once, rather than the runtime on every construction.

**Compiler enforcement:**
- **`RequiredFieldsNeedInitialEvent`:** Precept has required fields (non-optional, no default) but does not declare an initial event — construction cannot produce a valid initial version.
- **`InitialEventMissingAssignments`:** Initial event does not assign all required fields that lack defaults — post-construction state may violate constraints.
- **`UninitializedFieldReadInInitialAssignment`:** A required field with no default is read inside its own first initial-event assignment — the read would observe an undefined value.
- **`UninitializedCrossFieldReadInInitialAssignment`:** An initial-event assignment reads another required field before that other field's first assignment in the same action chain establishes a value.

**Design rationale:** Construction goes through the full event pipeline because entities must satisfy their constraints from the moment they exist. A parameterless construction path cannot enforce business invariants at intake. By modeling construction as an event, the language reuses all existing machinery — guards can discriminate construction routing, ensures validate args, `reject` can refuse intake, and the caller uses the same pattern matching they use for every event.

**Why `on <Event>` and not `from <InitialState> on <Event>`.** `from State on Event` means "when the entity IS IN this state and this event fires." At construction time, the entity is not yet in any state — it is being brought into existence. The `from` prefix would be misinformation, dressing unconditional genesis as state-dispatched transition. Removing it makes the syntax honest: `on Create -> ...` means "when the act of creation happens, do this."

**Why `transition` is structurally excluded from construction rows.** When construction is structurally terminal, `initial` on both state and event means "origin" — consistent, learnable, single-meaning. If construction could route elsewhere, "initial state" would degrade to "where the dispatch table starts and then immediately leaves." Structural exclusion via grammar is superior to a type-checker rejection diagnostic: you cannot write what the grammar does not express. Authors get a parse error, not a downstream diagnostic, and tooling will not offer `transition` completions in construction context.

**Why the same `EventRow` construct serves stateful construction, stateless construction, and stateless event handlers.** A row that begins with `on <Event>` always means "fire actions when this event is invoked, with no state precondition." Construction is a special case (the entity is not yet in any state); stateless precepts always have this shape. Promoting the row shape to a single construct keeps the grammar smaller and lets the type checker decide *which kind* of row it is from the event's `initial` modifier, rather than the parser inventing parallel construct trees for what is one shape.

**Why `initial` is not renamed to `constructor`.** With structural exclusion in place, `initial state` and `initial event` both mean "origin" — same word, coherent semantics. A rename adds vocabulary without adding clarity; the keyword overload that motivated a rename proposal disappears once `transition` is grammar-excluded from construction rows.

**Why guards on `on <Event>` are allowed.** `on Event when condition -> actions` is semantically coherent. Multiple guarded construction rows provide first-match routing at intake — directly analogous to Swift's `guard … else { return nil }` inside `init?`, but declarative and exhaustive rather than imperative. The earlier "no guards on `on Event`" restriction was a parser simplification with no semantic backing; removing it costs a small bump in `EventRowDeclaration` complexity and unlocks guarded construction.

**Why non-initial events cannot use `on <Event>` form in stateful precepts.** Three independent reasons:

1. **Redundancy.** `from any on Event -> no transition` already expresses state-agnostic handling in stateful precepts.
2. **Execution-order ambiguity.** Mixing bare `on Event` handlers with `from State on Event` rows in the same precept creates an ambiguous execution model (before? after? instead of?). No resolution rule is obvious.
3. **Pseudo-lifecycle antipattern.** Bare event handlers that mutate fields without participating in state topology make the lifecycle underdeclared. Stateful precepts derive their guarantees from explicit transitions; side-channel handlers undermine that.

The construction-row exception (`initial` events via `on <Event>`) is justified because no prior state exists to dispatch from — the restriction's rationale does not apply at construction time.

**Cross-language precedent.** Conditional construction is the established norm across surveyed languages. The "constructors shouldn't fail" position is a ghost — a misreading of C++ exception-safety advice that no modern language community holds.

| Language | Mechanism | Precedent for Precept's `reject` |
|---|---|---|
| Swift | `init?` / `init throws` | Failable initializer with `guard ... else { return nil }` — closest syntactic analogue to guarded construction rows |
| Rust | `TryFrom` / `new() -> Result<T,E>` | Fallibility as a type-level property of construction |
| Haskell/ML | Smart constructors returning `Maybe`/`Either` | The settled functional idiom — construction IS validation |
| Go | `New*() (*T, error)` | The *only* construction idiom — always conditional |
| C++ | Throw from constructor (Core Guidelines C.42) | "If you can't establish invariants, throw" |
| DDD | Evans/Vernon: factory must refuse invalid aggregates | Domain-level mandate, mechanism-agnostic |

Full cross-language survey: see [`research/language/research-conditional-construction.md`](../../research/language/research-conditional-construction.md).

**Precept's contribution over surveyed mechanisms.** The construction decision matrix is *declarative*, *exhaustive*, and *provably complete*. Swift's `init?` is imperative; Rust's `TryFrom` is imperative. Precept's `when` guards + `reject` outcome make the construction decision space inspectable, analyzable, and formally verifiable. `reject` takes a message string (`-> reject "Claims require a positive amount"`), giving it the error-context richness of Swift's `init throws` while retaining the declarative structure of `init?` — so the gap that split Swift's failable construction into two mechanisms does not exist in Precept.

**"Construction must always be possible."** Swift requires every `init?` to have at least one success path (the compiler warns on `init?` that always returns nil). C++ Core Guidelines C.42 says throw when you can't establish invariants — the corollary is that if you can't establish them on any path, the type is broken. Precept's requirement that at least one construction row can produce `Created` (enforced by `AlwaysRejecting` promoted to Error severity for initial events) is the structural version of this universal principle: forced exhaustiveness over construction outcomes.

#### Fire-once guarantee

The initial event fires exactly once — at construction time. After construction succeeds, the initial event is excluded from the entity's post-construction event space:

- **Compile-time:** The initial event cannot appear in any `from <State> on <InitialEvent>` transition row. If it does, the compiler emits `InitialEventInTransitionRow` (Error). The initial event's dispatch space is construction-only.
> **Draft 2026-11-25** — under adversarial review; nothing may cite this section until the mark is removed.

- **Runtime:** `version.Fire(initialEventName, args)` on an existing entity returns `EventOutcome.UndefinedEvent`, whatever the arguments, on a stateful or a stateless precept. The initial event does not appear in the post-construction dispatch table, and `Fire` refuses it before checking its arguments, so its argument constraints, its `on <InitialEvent> ensure` declarations and its construction rows are never evaluated after construction (§3A.4 phase 1).
- **`Version.AvailableEvents`:** Excludes the initial event. Only events that can be fired in the entity's current post-construction state are listed.

#### Stateless Precepts (no states declared)

For precepts that declare no states, `Version.State` is `null` in the constructed version. The construction algorithm adapts as follows:

- **Step 1 (initial state set) is omitted.** There is no initial state to assign. The hollow version is built with `State = null`.
- **State-entry semantics do not fire.** `to <State> ensure` guards and `in <State> ensure` residency checks require a named state to evaluate against. Because no state is entered, they are structurally absent — not evaluated, not skipped conditionally, simply not applicable.
> **Draft 2026-11-25** — under adversarial review; nothing may cite this section until the mark is removed.

- **All other steps apply unchanged.** If an initial event is declared, `Create` runs it through the same phases as any other event, without calling `Fire` (ingress validation of the arguments, row selection among the construction rows, the row's actions, recomputation — §3A.4). The same construction diagnostics apply here as in stateful precepts, including missing required assignments and self-reads of undefined required fields in the initial handler. If no initial event is declared, defaults are materialized and computed fields are derived. Global rules are not evaluated in either case: the compiler proved construction establishes them (§0.7). `Version.State` is `null` in the returned version.

This is the natural extension of §3A.5 to precepts without a state machine. Null state is the honest representation of "current field values alone" (§0.2) — no sentinel, no hidden machinery, no separate API path.

> **Compiler:** The graph analyzer is exempt from initial-state reachability checks, dead-end-state checks, and unreachable-state checks for stateless precepts. These checks require a state machine topology to operate on. See CC#26.

### 3A.6 Inspection as a First-Class Operation

> **Draft 2026-11-25** — under adversarial review; nothing may cite this section until the mark is removed.

Inspection is not a reporting layer — it is a fundamental language operation. It has the same depth as event execution: ingress validation of the supplied values, guard evaluation, exit actions, mutations, the `omit` reset, entry actions, and computed field recomputation — all executed on a working copy that is never committed. The normative phase order is §3A.4, *Operation execution order*, which this sentence agrees with; there is no constraint-evaluation phase to preview, because there is none in execution.

The answer to "what would happen?" is always available, from any state, for any event, and is honest. The inspection result matches what execution would produce for the same inputs. Inspectability is what makes the governance contract trustworthy — you can always ask, and the language guarantees the answer matches what execution would do.

This is not merely a tooling convenience. It is a language-level guarantee that flows from the execution model's properties: expression purity (§0.4 property 6 — expressions cannot mutate state), deterministic semantics (§0.1 principle 3 — same inputs produce same outputs), and working copy isolation (§3A.4 — mutations execute on a copy). These properties together make inspection safe and honest by construction, not by convention.

Inspection may be asked for an answer it cannot reach — an input it depends on has not been supplied. It is **best-effort**: it answers with everything it can legitimately determine and reports the rest as undetermined. It never guesses and never substitutes a value the caller did not give, so the honesty guarantee above holds with the inputs actually present.

Being best-effort does not make inspection exempt from any guarantee. It is a fundamental operation, not a preview mode, so the fault guarantee (§0.1 principles 10 and 11) holds during inspection exactly as during execution — and it holds without weakening what the compiler may prove. A proof discharged from an argument constraint or a guard stays discharged: where those premises are absent, the consequence is undetermined and is reported as such rather than evaluated. (The mechanism — how undetermined values are represented and propagated — is runtime behaviour: `docs/runtime/evaluator.md`.)

> **Draft 2026-11-25** — under adversarial review; nothing may cite this section until the mark is removed.

#### What a preview evaluates

A preview is not a separate calculation that happens to give the same answer. Every expression a preview evaluates is evaluated at a position, and the fault guarantee covers an expression only at the positions the operation itself evaluates it: the compiler proved it safe there, from what is true there — the arguments passed ingress, this row's guard is true, these writes have already been made. The rule that follows has two halves, and both bind.

1. **A preview evaluates everything the operation would, in the same order.** `InspectCreate`, `InspectFire` and `InspectUpdate` run the phases of §3A.4, *Operation execution order*, that `Create`, `Fire` and `Update` run, in that order, on a working copy: the starting working copy, ingress, dispatch, exit actions, the state change, the row's actions or the patch, the `omit` reset and entry actions, and recomputation. They stop before phase 8. A preview that left out a step the operation runs — the exit actions, say — would report field values the operation would not produce.
2. **A preview evaluates nothing the operation would not.** In particular:
   - If the supplied values fail ingress, nothing after phase 1 is evaluated. Ingress here is the whole of phase 1 as the operation runs it: for a preview of `Fire` or `Create`, the presence and type of each supplied argument, the constraints declared on each argument, and the event's `on <Event> ensure` declarations — each one evaluated whenever everything it reads was supplied and passed the earlier checks and every phase-1 constraint its proof relied on passed (§3A.4 phase 1), and, for one with a `when` guard, its guard evaluated first under that same rule and its body only when the guard is certainly true: a guard that is false makes the ensure pass, and a guard that is undetermined leaves the body undetermined and unevaluated (§3A.4 phase 1, *A guarded event ensure*), including an ensure that reads only fields, if an event ensure may read a field at all (§3A.4, *Known exceptions*, item 12). A required argument the caller left out is not a failure in a preview (see *When inputs are missing*), but a check that can be made over what was supplied is made, and if it fails the preview reports the refusal and evaluates nothing further, because the operation would have refused there. This holds for the event previews `InspectUpdate` runs as well, which are given no arguments: an `on <Event> ensure` that reads only fields is evaluated against the configuration the update would produce. A preview of `Update` does not apply a refused patch and does not recompute computed fields from it: a computed expression is proven safe over values that passed their checks, not over a value that just failed one. Within phase 1 itself, a constraint on a patched field that reads a computed field over the patched values (§3A.4 phase 1) is held to the same rule as every other phase-1 constraint: it is not evaluated when a phase-1 constraint its proof relied on failed.
   - Once a row has certainly won dispatch, no later row is evaluated — not its guard, not its actions, not its reason string. The operation would never reach it (§3A.4 phase 2), so a later row's expressions have no proof that covers evaluating them here. The later rows still appear in the report, marked as unable to fire because an earlier row wins; that is a statement about dispatch, made without evaluating them. A `reject` row reported this way, or reported because its guard is certainly false, carries its reason as written, with its interpolations not filled in.
   - A row whose guard is certainly false has only its guard evaluated. Its actions, its reason string and the state actions it would have triggered are not run.
   - State actions are run only for the row being simulated, and only when that row's outcome is one that runs them (§3A.4).

**When inputs are missing.** With every input supplied, the two halves above make a preview evaluate exactly what the operation evaluates. With a required argument missing — one that is neither `optional` nor carries a `default` (§3A.4 phase 1) — the operation could not run at all, so there is no single run to match, and the preview answers as far as the supplied inputs determine (what follows).

A patch is never missing a value: `Update` takes any set of editable fields, the empty set included (§3A.4, *The operations*), so `InspectUpdate` previews `Update` with exactly the patch it is given, or with none, and its answer is certain wherever that `Update`'s is. An argument with a `default` that the caller leaves out is not a missing input either: the operation runs with its default (§3A.4 phase 1), and so does the preview. An `optional` argument the caller leaves out is not a missing input. The operation given the same inputs runs, with that argument not set (§3A.4 phase 1, *An `optional` argument the caller leaves out is not a missing argument*), so the preview reads it as not set too, and a preview whose only omissions are `optional` arguments evaluates exactly what the operation evaluates and reports a certain answer where the operation's is certain. This holds for the event previews `InspectUpdate` runs, which pass no arguments: an event whose arguments are all `optional` or carry a `default` is previewed as fired with each `optional` one not set and each defaulted one at its default.
- *Reasoning.* §3A.6 promises that "the inspection result matches what execution would produce for the same inputs", and the operation given those inputs reads not set. A preview that treated the argument as unknown would report `Possible` where the operation's outcome is fixed, with every input the operation needs in hand.
- *Alternatives rejected.* Treating an omitted `optional` argument as not yet supplied: it makes every preview of an event with an `optional` argument less certain than the operation it previews, and gives `InspectUpdate`'s event previews no way to be certain for such an event. Letting the caller mark an omitted `optional` argument as "not decided yet": a further input form no canonical document describes.
- *Precedent.* A function call that omits an optional parameter runs with the parameter's absent value, in C# and Python alike; the call is complete, not partial.
- *Tradeoff accepted.* A caller building an input form step by step cannot ask "what if I fill in this optional argument later?" by leaving it out; the preview answers for not set, which is what `Fire` would do with the same inputs.

With a required argument missing:

- An expression that reads a missing input is **undetermined**. It is not evaluated with a substitute value.
- A guard that is undetermined leaves its row possible, neither won nor lost. The preview may then go on — to that row's consequences as they would be if its guard were true, and to the rows after it as they would be if its guard were false — because the operation might reach either.
- On those possible paths, an expression is evaluated only when every premise the compiler's proof of it relied on is known to hold for the supplied inputs. Where one is not known — the guard it sits behind is undetermined (the row's own guard, or the guard of a guarded state action it sits in), an earlier operand of the same `and` or `or`, or the condition of the `if … then … else` it sits in, is undetermined and the compiler's proof of it relied on that operand or condition, the falsity of an earlier candidate row's guard it relies on (§3A.4, *What an earlier write invalidates*) is undetermined, or the argument constraint or `on <Event> ensure` it relies on reads an argument that was not supplied — the expression is not evaluated and its result is undetermined. This is the sentence in the paragraph before this subsection ("where those premises are absent, the consequence is undetermined and is reported as such rather than evaluated") applied to every position. It covers a later row's own guard, a `reject` row's reason string, and the exit and entry actions a possible row would run, with their guards, as well as the row's own actions: after an earlier row whose guard is undetermined, a later row's guard whose proof relied on that earlier guard being false is itself undetermined, not evaluated.
- A write whose value is undetermined makes its destination undetermined. A write that may or may not happen — an action in a state action whose guard is undetermined — makes every field it could write undetermined. Anything that reads an undetermined field is undetermined, computed fields included.
- A row is reported as certain to fire only when its guard is certainly true and every earlier row's guard is certainly false.

**`InspectUpdate` and the events that follow.** `InspectUpdate` also reports what each event that has rows in the current state would do from the configuration the update would produce, the initial event excluded (it cannot be fired — §3A.4 phase 1). An event with no rows in the state is not reported (`runtime/runtime-api.md` § `InspectUpdate`, "every event defined in the current state"). Each of those is an `InspectFire` with no arguments, run against that configuration, and is held to everything above. They are run only when the previewed update would commit: a refused update produces no configuration to fire anything from.

**The one way a faithful preview and the operation can differ.** The clock is an input (§3A.4, *Known exceptions*). A preview that reads `now()` and the operation that follows it read different instants, and may differ for that reason and no other.

How undetermined values are represented and reported — `Prospect`, `FieldSnapshot.IsResolved`, the three-valued guard evaluator — is `docs/runtime/result-types.md` and `docs/runtime/evaluator.md`.

---

## 4. Graph Analyzer

> **Status:** Implemented. See [`docs/compiler/graph-analyzer.md`](../compiler/graph-analyzer.md) for implementation detail.

---

## 5. Proof Engine

> **Status:** Base engine implemented; prove-or-reject MVP and fail-open soundness fixes designed, not yet implemented — see docs/compiler/proof-engine.md.

Precept's proof system is the compile-time layer that prevents invalid qualifier and safety configurations from entering an accepted definition. Conceptually it runs in two passes: it first instantiates proof obligations from typed expressions and actions, then discharges those obligations from literals, declaration metadata, guards, simple flow facts, and qualifier compatibility. It also checks initial-state satisfiability against default values (`PRE0115`). See [`docs/compiler/proof-engine.md`](../compiler/proof-engine.md) for implementation detail and §7 there for qualifier-resolution mechanics.

**Construction exemption for initial-state satisfiability (`PRE0115`):** The exception the language allows is **per field**, and it is the one designed in §0.6 item 11: a default that the construction event *provably overwrites* is a placeholder that never reaches a committed configuration, so no rule and no initial-state ensure has to hold of it. Checking such a default would be a false positive for a Pattern A precept whose initial state carries unguarded ensures.

A default the construction event does **not** provably assign is a different thing: it is still the entity's initial value, so a rule or ensure over that field is part of the base case, and suppressing the check leaves the base case of the induction open for that field — a prevention hole rather than a conservative under-emit. The engine today early-returns for the whole precept as soon as a construction handler exists, which is wider than this exception; that is an implementation gap, not settled design, and [`../compiler/proof-engine.md`](../compiler/proof-engine.md) records it as one.

For authors, a **proved** qualifier constraint means the compiler established the required fact for every accepted execution reaching the proof site: the expression can rely on that qualifier relation at runtime without an additional author-supplied guard. The proof system is intentionally conservative. If the compiler cannot prove the required fact, the program is rejected with a diagnostic rather than accepted on a guess.

The implemented qualifier-oriented checks include:
- numeric/range obligations such as nonzero and nonnegative requirements on operations
- currency-axis compatibility and conversion-result currency resolution
- unit, dimension, and temporal-dimension compatibility
- declaration-based facts carried by qualifiers, modifiers, and presence metadata

Interval containment proofs for quantity and price fields normalize statically convertible magnitudes to UCUM base units before comparison, preventing false-positive `NumericOverflow` diagnostics on cross-unit assignments (e.g., `max '5 kg'` with `set field = '6 [lb_av]'` is correctly proved safe because 6 lb ≈ 2.72 kg < 5 kg). Explicit counting-unit mismatches such as `each` vs `box` are not meaningfully comparable without a separate conversion field, so they must not be proved by same-dimension fallback.

An unprovable obligation is not treated as "probably fine."It remains unresolved, surfaces a compile-time diagnostic, and requires the author to add the missing qualifier, modifier, default, guard, or other proof-bearing fact.

---

## Open Questions / Implementation Notes

> **Draft 2026-11-25** — under adversarial review; nothing may cite this section until the mark is removed.

**Does `from any` desugar per state?** A comma-delimited `StateTarget` is defined as pure syntactic sugar that expands to one independent declaration per named state (§2.2). The spec does not say whether the `any` wildcard expands the same way. It matters in two places. First, any analysis that counts per-state units — proof obligations, reachability, coverage — gets different answers for `from A, B on E` and `from any on E` in a two-state precept, which are otherwise the same machine. Second, it decides whether a construct written with `any` has one set of facts available to it or one set per source state. *Options:* expand per state, matching the comma form; treat `any` as a single unit whose available facts are what holds in every source state; or something else. Not conservative either way — expanding widens what can be proven and multiplies the units; not expanding does the reverse. What is not open is execution: §3A.4, *Handlers*, states that a `from any` row is a candidate in every state and gives one handler per source state, so whichever way the compiler analyses it, its conclusions have to hold for each of those.

**Owner decision outstanding — the number model: fixed-size or unlimited `integer` (and possibly `decimal`).** Whether `integer` stays fixed-width 64-bit, with every operation proved to stay in range, or becomes unlimited in size, is not decided; the owner may answer it for the whole number model, `decimal` included. His words (2026-07-20): *"I am considering arbitrary precision but no decision yet."* Waiting on it: the post-MVP overflow proof, the `long` type the runtime API gives `integer`, and temporal arithmetic, which follows whatever `integer` does. Until he rules, canon applies as written: `integer` is fixed-width 64-bit with overflow handling as §0.7 and `primitive-types.md` § Open Questions state, and `decimal` is the fixed 28-digit .NET `System.Decimal`. Full entry: [`primitive-types.md`](primitive-types.md) § Open Questions.

**Owner decision outstanding — may a rule or an ensure mention `now()`.** A rule that reads the clock can become false while no operation is running, and the induction of §0.7 has no step in which time passes on its own. Whether such rules and ensures are allowed, refused, or allowed in a restricted form is not decided. The owner's want document: *"Do rules that reference the current moment exist at all, or does time belong only in guards (evaluated at event time, like any other premise)? Deliberately left open."* (`what-i-want-2026-07-16.md:219`). Asked again on 2026-08-27, after being told the temporal type document answered it, he said: *"it is undecided, add it to the list"*. Waiting on it: how a rule mentioning `now()` is proved, and whether the type checker refuses `now()` in rule and ensure positions. Until he rules there is no default: no analysis may assume that rules or ensures mentioning `now()` are allowed, nor that they are refused. Guards and actions may read `now()`. See [`temporal-type-system.md`](temporal-type-system.md) § `now()`.

**Owner decision outstanding — what a direct edit of an editable field checks.** When a caller writes an `editable` field through `Update` and a rule mentions that field, it is not settled whether the edit is checked, before it commits, only against the field's own declared constraints or also against every rule that mentions the field. The owner's want document answers it in his words: "For an editable-field write, it's the field's modifier-rules *and* every rule that mentions the field — an edit is an arbitrary incoming value that no static proof can cover, so evaluating those rules at the ingress point is exactly the premise that closes the editable write sites in the compiler's proof." (`what-i-want-2026-07-16.md:143`). On 2026-08-26 he had narrower wording removed from canon and chose not to settle the question then; that choice is recorded only in paraphrase. Waiting on it: what `Update`'s ingress check evaluates, how the compiler closes the proof at an editable write site, and which outcome reports a rule the edit would break. The want document's reading is the working assumption until he confirms it.

> **Draft 2026-11-25** — under adversarial review; nothing may cite this section until the mark is removed.

**Whether the set of in-operation writers is closed.** §3A.4, *What can write during one operation*, enumerates the writers and every field each action form writes. Only the actions are catalog-declared; the others — default materialization, restore slot injection, the update patch, the `omit` reset, and computed-field recomputation — are not, and the `omit` reset has neither a catalog member nor an implementation. The exhaustiveness analyzer that fails the build when a consumer misses a catalog member is applied to three enums today, none of them covering the writers, so nothing detects a new writer arriving. Until both are addressed, the enumeration is asserted by that section rather than kept by the build. A proof that relies on a fact surviving an operation depends on the list being complete, so this is a soundness dependency, not a documentation nicety — and BUG-033 (`docs/Working/bugs.md`) is a verified instance of the list being wrong: an action's `into` target is not treated as a write site. Deferred to implementation.

**Thirteen things §3A.4 found unstated.** §3A.4, *Known exceptions*, item 12 lists them: what `dequeue F into X by X` means; when a `default` expression that reads other fields is evaluated at the `omit` reset; what a rule, an ensure or a computed expression that mentions a field means in a state that omits that field, event ensures included; what an `optional` collection without a `default` starts as, what an element action does to one that is not set, and what `clear` does to one (§3.8 and `collection-types.md` disagree); which configuration of a transition a `from <State> ensure` must hold of; whether a required field without a default may be omitted by the initial state, and whether a construction row may write a field the initial state omits; what `Restore` does with data missing a stored field; what a restored entity holds in the storage of a field its persisted state omits; what `Restore` returns when a computed field cannot be evaluated over persisted data that breaks a rule; whether a computed field may be omitted, and what it holds if so; which state's `omit` restricts a write made by an exit action; what an argument's `default` expression may read; and whether an `on <Event> ensure` may read a field at all (§3.5 and §3A.1 disagree, and the want document puts relations an event needs in its guards). Nothing may rely on an answer to any of them until one is written.

**No diagnostic is assigned to three refused reads.** §3A.4 phase 0 refuses a `default` expression, an `on <InitialEvent> ensure` or initial-event argument constraint, and the reason string of a construction `reject` row, that reads a field with no value yet. Canon names diagnostics for the same read in a construction guard, in the construction row's actions (`UninitializedFieldReadInInitialAssignment`, `UninitializedCrossFieldReadInInitialAssignment`) and in a materializing transition (`MaterializedFieldSelfReference`), but none for these three positions. Assigning one is diagnostic-catalog work (`../compiler/diagnostic-system.md`), not a design question.

---

## Cross-References

| Document | Relationship |
|---|---|
| [Compiler and Runtime Design](../compiler-and-runtime-design.md) | Pipeline architecture; stage contracts |
| [Catalog System](catalog-system.md) | Machine-readable language definition that feeds all pipeline stages |
| [Lexer](../compiler/lexer.md) | §1 implementation detail |
| [Parser](../compiler/parser.md) | §2 implementation detail |
| [Type Checker](../compiler/type-checker.md) | §3 implementation detail |
| [Graph Analyzer](../compiler/graph-analyzer.md) | §4 implementation detail |
| [Proof Engine](../compiler/proof-engine.md) | §5 implementation detail |
| [Language Vision](../archive/language-design/precept-language-vision.md) | Archived target language surface — this spec tracks what's implemented |
