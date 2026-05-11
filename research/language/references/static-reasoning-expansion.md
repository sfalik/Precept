# Static Reasoning Expansion

**Research date:** 2026-05-30
**Author:** George (Runtime Dev)
**Batch:** 3 — horizon domain, no open proposal
**Relevance:** Compile-time contradiction detection, deadlock detection, satisfiability analysis, range propagation, and extensions to null narrowing

This file is durable horizon-domain research for expanding Precept's compile-time reasoning beyond today's type checks, null narrowing, duplicate-guard detection, and graph analysis. It is not a proposal body. It records why deeper contradiction and deadlock analysis matters, what adjacent systems do, which semantic contracts would need to be explicit, and which directions should stay out of scope until the product is ready.

---

## Background and Problem

### The compile-time promise

Precept's philosophy states: *"The compiler catches structural problems — unreachable states, type mismatches, constraint contradictions, and more — before runtime."* (`docs/philosophy.md` § What makes it different). That promise is real and partially delivered. The question this research addresses is how far it can soundly extend.

The current compiler proves contradictions from a narrow but sound set of facts:

- **Type mismatches** (C39–C41): same-family equality, numeric/boolean/string incompatibilities.
- **Null-flow violations** (C42): nullable fields used in comparisons or arithmetic without a null guard. Conservative narrowing through `when` guards refines nullability from `nullable T` to `T` within the guarded branch.
- **Structural graph checks** (C48–C53): unreachable states, orphaned events, dead-end states, reject-only pairs, events that never succeed, empty precepts.
- **Duplicate and subsumption checks** (C1–C2, C47): `in`/`to` subsumption on the same state+expression; duplicate asserts; identical `when` guard rows.
- **Initial-state default violation** (C3): if the initial state has `in <State> assert X > 5` and field `X` defaults to `0`, the contract is violated before any event fires. Both the default and the initial state are statically known — pure constant evaluation.

### What the design spec requires but has not yet shipped

`docs/PreceptLanguageDesign.md` § Compile-time checks lists two further checks as locked design decisions, currently deferred in `docs/@ToDo.md` under "Later":

| Check | Description | Implementation Status |
|---|---|---|
| **C4 — Same-preposition contradiction** | Multiple asserts with the same preposition on the same state whose conjoined per-field domains are empty (e.g. `in Open assert X > 5` + `in Open assert X < 3`) | Designed. Not yet implemented. |
| **C5 — Cross-preposition deadlock** | `in`/`to` vs `from` asserts on the same state whose conjoined per-field domains are empty — the state is provably unexitable | Designed. Not yet implemented. |

The design doc specifies the algorithm: *"All domain checks use per-field interval/set analysis on the expression AST. Expressions involving `contains` or cross-field relationships that cannot be reduced to per-field domains are assumed satisfiable (no false positives)."*

This is the correct starting constraint. C4 and C5 are the implementation target. The research question beyond them is: what does "per-field interval/set analysis" look like in practice, what adjacent systems have done it, what its limits are, and where it might soundly extend further.

### Where the problem shows up in real samples

**`loan-application.precept`** has a cluster of constraints that are easy to read locally but harder to reason about jointly:

```precept
invariant RequestedAmount >= 0 because "Requested amount cannot be negative"
invariant ApprovedAmount >= 0 because "Approved amount cannot be negative"
invariant ApprovedAmount <= RequestedAmount because "Approved amount cannot exceed the request"
in Funded assert ApprovedAmount > 0 because "Funded loans must have a positive approved amount"
```

The third invariant is cross-field (`ApprovedAmount <= RequestedAmount`) and cannot be reduced to a per-field interval without knowing the other field's bounds. Per-field analysis on `ApprovedAmount` alone sees `[0, ∞)` from the invariants, then `(0, ∞)` from the `in Funded` assert — no contradiction. If an author accidentally added `in Funded assert ApprovedAmount < 0`, C4 would catch it instantly: `(0, ∞) ∩ (-∞, 0) = ∅`.

**`insurance-claim.precept`** and **`it-helpdesk-ticket.precept`** use `from <State> assert` exit gates alongside `in <State> assert` residency constraints. An author who writes conflicting exit and entry conditions on the same state creates C5 — a provably unexitable state. The inspector can surface this via simulation, but the compiler could catch it definitively.

The practical failure mode is author surprise. A precept can be structurally valid, yet contain a state whose rules carve away the entire legal value space. The runtime will still prevent invalid instances, but the author only discovers the mistake through `inspect`, `fire`, or trial scenarios. For a product positioned around prevention and compile-time honesty, that is a meaningful future lane.

### The open research surface beyond C4/C5

Four related capabilities frame the full horizon:

1. **Same-preposition contradiction detection** (C4 — designed, not yet implemented).
2. **Cross-preposition deadlock detection** (C5 — designed, not yet implemented).
3. **Global invariant satisfiability** — prove that the full set of `invariant` declarations is simultaneously satisfiable by at least one entity state.
4. **Range propagation beyond null narrowing** — carry numeric interval facts through expressions the way Precept already carries null facts today.

---

## Precedent Survey

The survey follows Precept's philosophy positioning categories. Not every category has direct precedent for static reasoning — the gaps are as informative as the matches.

### Category: Formal specification languages

#### Alloy (MIT)

Alloy is a structural modeling language based on first-order relational logic. The Alloy Analyzer finds satisfying and unsatisfying instances inside a bounded relational model; contradiction is exposed as "no instance exists." Every `fact` in an Alloy model is a universal constraint — the analyzer checks whether any combination of atoms satisfies all facts simultaneously. If the constraint set has no satisfying instance, the model is *vacuously true* (trivially unsatisfiable), which is a separate diagnostic.

**URL:** https://alloytools.org/documentation.html

**What this shows for Precept:**
- Alloy's `fact` declarations map directly to Precept's `invariant` statements.
- Alloy's approach to empty-instance detection is the formal precedent for C4/C5 (no per-field satisfying assignment exists).
- Alloy uses bounded scope to achieve decidability. Full first-order logic satisfiability is undecidable; Alloy trades completeness for decidability by bounding the universe.
- **Precept implication:** A full Alloy-style model finder is overkill for Precept's linear scalar fragment. But the user-facing experience — "no valid instance exists in this configuration" — is exactly the message C4/C5 should produce.

#### OCL 2.4 (OMG Object Constraint Language)

OCL is the OMG standard constraint language attached to UML models. OCL invariants express conditions that must hold on all class instances. OCL has no built-in satisfiability checker — tools like USE (UML-based Specification Environment) translate OCL expressions into SMT problems for external solvers, but this has not seen broad commercial adoption.

**URL:** https://www.omg.org/spec/OCL/

**What this shows for Precept:**
- OCL takes the position that constraint authoring is correct; satisfiability is a tooling concern, not a language concern. Enterprise tooling has not embraced automated OCL satisfiability checking in production use.
- The cost of connecting a full SMT backend to a developer-facing constraint language is high, and adoption has been limited.
- **Precept implication:** Full SMT satisfiability is not what Precept should aim for. The fragment Precept can check — simple numeric intervals on single fields — is far more tractable and actionable for authors.

#### Dafny (Microsoft Research)

Dafny is a verification-aware programming language that uses Z3 as its underlying SMT solver. Every `requires`, `ensures`, and `invariant` clause is discharged as a verification condition by Z3. Dafny's type system includes integer intervals and nullable types with static null safety.

**URL:** https://dafny.org/

**What this shows for Precept:**
- Dafny's postcondition checking (`ensures`) corresponds to Precept's `to <State> assert` — "after this operation, this must hold."
- Dafny's loop invariants correspond to Precept's `in <State> assert` — "while in this region, this must hold."
- Dafny uses Z3 for heavy reasoning. The key design decision: Dafny allows user-supplied lemmas when Z3 can't discharge a condition. Precept has no lemma mechanism and should not.
- **Precept implication:** Dafny's architecture clarifies what Precept should *not* be: it should not delegate to an external solver, should not require author-supplied proofs, and should not fail silently when a condition is undecidable.

#### Abstract interpretation

Abstract interpretation (Cousot & Cousot, 1977) is the formal theory of sound overapproximation for program analysis. A program analyzer is proven sound if it operates on an abstract domain that forms a Galois connection with the concrete domain. Interval analysis — tracking `[lo, hi]` bounds for numeric variables — is the canonical simple abstract domain.

**Reference:** Cousot, P. & Cousot, R. (1977). Abstract Interpretation: A Unified Lattice Model. POPL 1977. https://doi.org/10.1145/512950.512973

**Numeric abstract domains:** Apron library documentation — https://antoinemine.github.io/Apron/doc/

**What this shows for Precept:**
- C4/C5 are instances of abstract interpretation over a single-field interval domain. For each field `F`, the constraint set is projected to a per-field interval `[lo_F, hi_F]`. If the intersection across the relevant assertions is empty, the conjunction is unsatisfiable.
- This is the best conceptual frame for Precept's horizon work: prove only what the chosen abstract domain can justify, defer the rest.
- The soundness invariant: if Precept reports C4/C5, it is a real contradiction. If it does not, the constraint set might still be infeasible (but Precept assumed satisfiable — no false positives).
- **Precept implication:** The interval domain is the right implementation target. It is sound, decidable, and implementable without an external solver. Richer abstract domains (octagons, polyhedra) exist but cost proportionally more for diminishing Precept-relevant benefit.

---

### Category: Pure validators

#### FluentValidation

FluentValidation validates DTOs at call time with property-scoped rule chains (`RuleFor(x => x.Amount).GreaterThan(0).LessThanOrEqualTo(x => x.RequestedAmount)`). It has no static satisfiability analysis; contradictory rules are an authoring bug discovered at runtime.

**URL:** https://docs.fluentvalidation.net/

**What this shows for Precept:**
- The entire validator category has chosen not to do static satisfiability analysis. Validators are designed to run against incoming data that is expected to be wrong *sometimes*. Contradictory rules are discovered through use.
- **Precept implication:** The category distinction matters here. A contradictory `invariant` set in a precept definition means *no valid entity can ever exist* — that is always a definition bug, not a data-dependent runtime case. This is why compile-time satisfiability analysis is meaningful for Precept and irrelevant for validators.

#### Pydantic

Pydantic validators are functions — not declarative expressions. Field-level constraint annotations (via `Annotated[int, Gt(0), Lt(100)]`) are static metadata, but the standard library performs no cross-validator contradiction analysis.

**URL:** https://docs.pydantic.dev/latest/concepts/validators/

**What this shows for Precept:**
- The annotation model (`Gt(0)`, `Lt(100)`) is the closest Pydantic analogue to Precept's field-scoped assertions. If someone writes `Gt(5)` and `Lt(3)` on the same field, no tool catches it.
- **Precept implication:** Precept's expressions are declarative and structurally reducible to intervals, while Pydantic validators are functions that cannot be statically analyzed. The reducibility of Precept's expression grammar is what makes C4/C5 tractable in the first place.

#### Zod, Joi, Valibot

None of these perform satisfiability analysis. They are runtime validators; the question of "can these constraints be simultaneously satisfied?" does not arise in their architecture.

---

### Category: Policy and decision engines

#### DMN / FEEL (OMG Decision Model and Notation)

DMN decision tables encode business rules as row/column mappings from input ranges to output values. FEEL expressions in DMN cells include range tests like `[10..100]`, `< 200`, and `> ApprovedAmount`. DMN authoring tools (Camunda, Trisotech) perform completeness and overlap analysis: they check whether every possible input combination is covered (completeness) and whether any two rows match the same input (overlap/contradiction).

**URLs:** https://www.omg.org/spec/DMN/ — https://docs.camunda.io/docs/8.7/guides/create-decision-tables-using-dmn/

**What this shows for Precept:**
- DMN's completeness/overlap analysis is the closest commercial precedent to C4/C5 reasoning. It operates on declared ranges in a declarative expression language — exactly the "per-field interval analysis on expression ASTs" approach Precept's design spec describes.
- Camunda's table analyzer is applied per-column, not cross-column — cross-column overlap analysis is optional and rarely used, because it explodes combinatorially. **This is a direct precedent for Precept's "assume satisfiable for cross-field relationships" policy.**
- **Precept implication:** Per-field interval analysis is the right scope. Cross-field constraint satisfiability is commercially unsupported even in dedicated rule systems. The existing policy boundary is correct.

#### Cedar (AWS)

Cedar is AWS's policy language for authorization. Cedar's policy analyzer (`cedar-analysis`) checks authorization policy sets for properties like "does this deny policy always shadow this allow policy?" using an SMT backend (Z3). Shadow analysis is directly analogous to Precept's C4: two policies whose conditions together produce `false` means the second is always shadowed.

**URL:** https://www.cedarpolicy.com/en

**What this shows for Precept:**
- Cedar's shadow analysis is a commercial product-grade example of contradiction detection in a declarative rule language, using Z3.
- Cedar requires Z3 because its policy conditions involve attribute namespaces, entity hierarchies, and set membership — richer than Precept's scalar fragment.
- **Precept implication:** SMT-backed analysis is commercially viable for small, structured rule sets. Precept's assertions per state are small sets of linear inequalities — a subset that a purpose-built interval analyzer can handle without a full SMT backend. Cedar validates the *concept*; Precept's implementation does not need Cedar's *approach*.

#### OPA / Rego

OPA evaluates Rego policies against presented data. Rego does not perform static satisfiability analysis of policy rules. The OPA ecosystem focuses on correctness testing (the `rego.v1` test framework) rather than static rule analysis.

**URL:** https://www.openpolicyagent.org/docs/latest/

**What this shows for Precept:**
- Dynamic policy systems rely on testing and simulation rather than static analysis, because their input spaces are open.
- **Precept implication:** Precept's closed entity model (finite declared fields, fixed type domains) is what makes static analysis viable. Open policy systems with arbitrary input shapes cannot enumerate the satisfiable domain.

#### Drools decision-table verification

Drools has explicit decision-table verification capabilities that surface conflicts, gaps, redundancy, and subsumption diagnostics. This is the most direct commercial-rule-engine precedent for contradiction-style author feedback.

**URL:** https://docs.drools.org/7.74.1.Final/drools-docs/html_single/#_verifying_and_validating_decision_tables

**What this shows for Precept:**
- Business-rule tooling can surface contradiction diagnostics as a first-class authoring feature — not just execute rules.
- **Precept implication:** Drools confirms there is established user demand for this category of feedback. The approach (per-column interval analysis in decision tables) is architecturally similar to Precept's intended C4/C5 approach.

---

### Category: Enterprise record-model platforms

#### Salesforce, ServiceNow, Guidewire

Enterprise platforms with field validation rules do not perform cross-rule satisfiability analysis. Validation rules are evaluated at record save time. Contradictory rules produce a save-time error on every attempt to save — the error is discovered through use, not at configuration time.

**What this shows for Precept:**
- Enterprise platform gaps are consistent: cross-rule satisfiability is not checked at definition time. The authoring experience relies on testing.
- **Precept implication:** The enterprise platform category is not a source of best practice for this feature. It is evidence that Precept has room to differentiate. Shipping C4/C5 would put Precept ahead of the platform tier on compile-time constraint correctness.

---

### Category: Pure state machines

#### XState

XState's state machine model has no constraint declarations on field values. XState has a `can(event)` method that checks whether an event can be taken from the current state (based on the state machine structure), but no equivalent to "is this state's data contract internally consistent?" XState's guards are runtime predicates, not static declarations.

**URL:** https://stately.ai/docs/guards

**What this shows for Precept:**
- State machines do not enter this analysis space at all. They do not declare field constraints; the question of "can these constraints be jointly satisfied?" simply does not arise in a pure state machine.
- **Precept implication:** This is exactly the gap that makes Precept a different category of tool. C4/C5 are only meaningful because Precept combines lifecycle structure with declared field constraints. Neither pure validators nor pure state machines can offer this.

---

### Category: Type systems and narrowing

#### TypeScript type narrowing

TypeScript performs control-flow-sensitive type narrowing. After a guard `if (x !== null)`, TypeScript narrows `x`'s type from `T | null` to `T` in the true branch. TypeScript also performs equality narrowing, `instanceof` narrowing, and discriminated union narrowing. TypeScript deliberately does *not* propagate numeric range bounds: after `if (x > 5)`, TypeScript does not narrow `x` to "number greater than 5."

**URL:** https://www.typescriptlang.org/docs/handbook/2/narrowing.html

**What this shows for Precept:**
- Precept has already implemented the equivalent of TypeScript's null narrowing.
- TypeScript's deliberate non-extension to numeric ranges is a precedent. The TypeScript team has repeatedly declined numeric range types because the complexity-to-benefit ratio is poor for general-purpose code.
- **Precept implication:** Null narrowing is justified because the benefit is concrete and universal. Value-range propagation through guards is more specialized, and TypeScript's posture is a warning: even a well-resourced compiler team with a strong culture of type-system investment has not found the range-propagation payoff worth the cost for general-purpose code.

#### Kotlin smart casts

Kotlin's smart cast system refines types after null-checks and `is T` tests. Like TypeScript, Kotlin does not propagate numeric range bounds.

**URL:** https://kotlinlang.org/docs/typecasts.html#smart-casts

**What this shows for Precept:**
- Two major industry languages with excellent type systems have drawn the same boundary: type narrowing yes, range narrowing no. The consistent cross-language posture strengthens the case for deferring range propagation within guards.

---

### Category: Database constraint checking

#### PostgreSQL CHECK constraints

PostgreSQL validates `CHECK` constraints at write time. The database does *not* check whether two `CHECK` constraints on the same column are mutually satisfiable at `CREATE TABLE` time. Adding `CHECK (x > 5)` and `CHECK (x < 3)` creates a table where no row can ever be inserted — PostgreSQL will not warn the author at schema definition time.

**URL:** https://www.postgresql.org/docs/current/ddl-constraints.html#DDL-CONSTRAINTS-CHECK-CONSTRAINTS

**What this shows for Precept:**
- Even production databases with decades of development do not statically analyze constraint satisfiability at definition time. This is a known authoring pain point.
- **Precept implication:** Shipping C4/C5 would put Precept *ahead* of PostgreSQL on compile-time constraint correctness. This is a defensible product differentiator.

---

### Category: Workflow orchestrators

#### Temporal, MassTransit

Workflow orchestrators (Temporal, MassTransit Sagas) manage distributed process state. They have no declared field constraint model; the question of constraint satisfiability does not arise.

**What this shows for Precept:**
- This category is not a source of precedent for static reasoning. It confirms the complementary positioning: Precept constrains entity data; orchestrators route work. The analysis here lives inside Precept's entity model, not at the orchestration level.

---

### Cross-category pattern

The precedent split is consistent:

- **Model finders and SMT systems** prove satisfiability by exploring models or delegating to solvers.
- **Abstract interpreters** prove only what their abstract domain can soundly derive — and this is the correct category for Precept's approach.
- **Business-rule verifiers** (Drools, DMN tools) focus on contradiction, redundancy, and coverage inside narrower rule shapes — the closest commercial precedent.
- **Ergonomic language analyzers** (TypeScript, Kotlin) start with local flow facts before attempting global theorem proving — and explicitly decline range propagation.
- **Enterprise platforms, validators, and orchestrators** do not attempt this analysis at all.

For Precept, the most plausible progression is:

1. Extend local fact propagation beyond null (range narrowing within guards, if value proven).
2. Add sound interval / finite-set reasoning for narrow rule fragments (C4/C5 implementation).
3. Reserve solver-backed satisfiability for a later phase only if the simpler domain proves insufficient.

That path fits the product better than starting with general-purpose SAT/SMT integration.

## Philosophy Fit

Static reasoning expansion fits Precept's philosophy only if it remains a **proof system for obvious contradictions**, not a second hidden runtime.

**Prevention, not detection.** C4/C5 implement Precept's prevention promise at the definition level: a contradictory assertion set or a deadlocked state cannot be compiled into a working engine. This is prevention of a definition mistake — the author wrote something that makes part of their own contract unreachable. The engine refuses to instantiate an unsound definition.

**Compile-time-first static analysis.** `docs/PreceptLanguageDesign.md` principle #8: *"The DSL should reject real semantic mistakes early, but never guess. If the checker can't prove a contradiction, it assumes satisfiable."* C4/C5 follow this principle exactly: they prove emptiness of the per-field interval intersection, produce no false positives for cross-field or non-interval expressions, and the boundary between provable and unprovable is structural and authoring-visible.

**Deterministic inspectability.** Diagnostics must explain *why* a contradiction was proven in domain terms (`Amount > 5` and `Amount < 3` cannot both hold simultaneously), not surface opaque solver jargon or symbolic internals.

**Flat, keyword-anchored readability.** The analysis should derive facts from existing declarations and expressions. It should not require users to learn theorem-prover syntax, quantifiers, or sidecar proof annotations.

**Compile-time honesty.** Precept should only raise error-severity diagnostics where the checker can actually prove emptiness or impossibility. "Maybe contradictory" belongs in future hints or not at all.

**AI legibility.** This lane is valuable because it turns hidden semantic traps into named diagnostics. It becomes harmful if authors or agents can no longer tell which expression fragment the engine understood and which fragment it treated as unknown. The provable/unprovable boundary must be explicit in documentation and diagnostic messages.

**One file, complete rules.** The benefit of static contradiction/deadlock detection is directly proportional to this principle. Because all constraints live in the same `.precept` file, the compiler has full visibility over the constraint set for each state. There is no scenario where a contradictory assertion is hidden in a separate service and invisible to the checker.

**What this analysis does not change.** Static reasoning expansion does not change the expression grammar, the runtime execution model, or the inspectability guarantee. `precept_inspect` continues to be the tool for data-dependent reasoning that static analysis cannot cover. Existing diagnostic severities are not affected — C4 and C5 are Errors; the same severity policy applies to any extensions.

The philosophy filter therefore favors **sound but intentionally incomplete** reasoning. Precept should prove a useful fragment well and leave the rest to `inspect`/`fire`, rather than pretend to solve arbitrary business logic.

## Semantic Contracts

These contracts must be settled before static reasoning expansion becomes active proposal work.

### 1. Supported proof fragment — the AST reducibility boundary

The language work must say exactly which expression AST node types can be reduced to per-field intervals and which cannot. This table is the core semantic contract:

| Expression form | Reducible to per-field interval? | Resulting interval |
|---|---|---|
| `F > n` (field vs numeric literal) | **Yes** | `(n, ∞)` |
| `F >= n` | **Yes** | `[n, ∞)` |
| `F < n` | **Yes** | `(-∞, n)` |
| `F <= n` | **Yes** | `(-∞, n]` |
| `F == n` | **Yes** | `[n, n]` (point) |
| `F != n` | **Partial** | Complement `(-∞, n) ∪ (n, ∞)` — useful when other constraints narrow the range |
| `F > G` (field vs field) | **No** | Assumed satisfiable |
| `F + G > n` | **No** | Assumed satisfiable (cross-field arithmetic) |
| `F.count > n` (collection) | **Yes** | Interval on the count dimension |
| `F contains v` | **No** | Set membership; not interval-reducible |
| `!E` | Yes if E is reducible | Complement of E's interval |
| `E1 && E2` | Yes if both reducible | Intersection of intervals |
| `E1 \|\| E2` | Yes if both reducible | Union of intervals (may be discontiguous) |
| Null checks (`F != null`, `F == null`) | **Yes** | Existing null narrowing domain |

The soundness invariant: if an expression is not reducible, the analyzer returns ⊤ (the full domain) for that field in that expression. Intersection of ⊤ with any other interval is the other interval — so non-reducible expressions never falsely contribute to an empty intersection.

Everything not in this table must be explicitly classified as either reduced to the supported fragment, treated as unknown/satisfiable, or excluded from the proposal. Without this boundary, "contradiction detection" sounds broader than the implementation can honestly support.

### 2. Abstract domain and proof posture

The checker needs a declared fact model. The most natural horizon contract is:

- **interval domains** for numbers,
- **finite sets** for booleans and future choice-like finite domains,
- **null / non-null facts** as the already-established seed,
- **unknown** for expressions the domain cannot safely represent.

The key posture should remain: **prove contradictions only from domain facts; never guess from uninterpreted structure.**

### 3. Severity policy

Precept's current diagnostic policy is the right template:

- **Error** only when emptiness or impossibility is proven from the supported fragment.
- **Warning/Hint** for structural observations the analyzer can justify without claiming impossibility.
- **No diagnostic** when the fragment is too rich to prove safely.

That preserves trust. A contradiction error that later turns out to depend on an unsound approximation would damage the language more than omitting a few proofs.

### 4. State-graph interaction

Deadlock analysis (C5) has to define what "unexitable" means in Precept's semantics:

- Is a state deadlocked only if every outgoing row is impossible?
- Do explicit `reject` rows count toward proof?
- Does a `no transition` self-stay count as "not exiting" or as a valid non-deadlocked outcome?
- How do entry assertions, `in` assertions, and `from` assertions combine with first-match row selection?

The analysis cannot just reason about assertion sets in isolation. It must respect the actual fire pipeline and outcome semantics.

### 5. Global invariant satisfiability (future extension)

A natural extension of C4 is to apply the same intersection check to `invariant` statements: if the set of all invariants produces an empty per-field domain, no valid entity can ever exist. The contract would state:

- Scope: all `invariant` statements in the definition.
- Cross-field invariants (e.g., `invariant ApprovedAmount <= RequestedAmount`) are assumed satisfiable — not reduced.
- Result: diagnostic on the invariant body, not on any specific state.
- Severity: Error — same as C4/C5.

This is a sound extension of C4 that checks the definition-level constraint set. Same algorithm, different scope. It is a natural candidate for the same implementation wave as C4/C5.

### 6. Diagnostic attribution

If the checker proves emptiness, the diagnostic surface must tell the author:

- which declarations participated,
- which domain facts were combined,
- which state / preposition / event context the proof applies to,
- and which portion of the expression was outside the proof fragment, if any.

The message model matters as much as the proof model. "Contradictory assertions" is weaker than "In `Approved`, `RequestedAmount > 10000` and `RequestedAmount < 5000` leave no legal value for `RequestedAmount`."

### 7. Performance budget

This analysis runs on every compile, language-server refresh, and MCP compile call. The expected complexity of interval intersection over a small assertion set (typically 2–6 constraints per state) is O(n) in the number of assertions. The implementation does not need a full CP solver — a hand-rolled interval reducer over the AST is sufficient for the fragment Precept cares about. Compile-path cost is a semantic contract, not merely an implementation detail.

## Dead Ends and Rejected Directions

Several directions are attractive on paper but are poor first moves for Precept.

### Full solver-first compilation

Driving every relevant rule set through SMT or SAT (Z3, CVC5) would make the compile pipeline harder to explain, harder to bound, and harder to keep diagnostic-friendly.

**Why this is rejected:**
1. **Author-opacity.** When an SMT solver reports unsatisfiability, the counterexample is an assignment that violates the constraints — not a human-readable explanation of which rule contradicts which. Authors need a diagnostic that points at the specific assert pair.
2. **Performance.** SMT queries run in the language server on every keystroke. Introducing solver latency would damage the authoring experience.
3. **Dependency.** Shipping a .NET library that depends on Z3 creates a native binary dependency, complicating packaging, cross-platform support, and security reviews in enterprise environments.

Solvers remain a valid escalation path for a future wave. Not the right default posture for the first expansion step.

### Cross-field general theorem proving

Expressions like `A + B > C * 2` or collection-heavy predicates are real business rules, but treating all such formulas as first-class proof targets pushes the language toward a research-grade theorem prover. Cross-field satisfiability requires a system of linear inequalities — a linear programming instance. This is decidable (LP is polynomial), but:

1. **Author-opacity.** The infeasibility witness for an LP instance is a dual solution, not a readable pair of contradicting assertions. Authors need to see "these two asserts contradict" — not a basis solution.
2. **Expression reduction complexity.** Reducing general Precept expressions to LP coefficients requires a dedicated algebraic reduction pass that handles all combinations of +, -, *, /, and field identifiers. Expressions with division are not linear.
3. **Sample evidence.** The current sample corpus does not show patterns where cross-field constraint infeasibility would be a realistic authoring hazard.

Cross-field satisfiability is a clear dead end for the first implementation wave.

### Range propagation through guards as a general extension

TypeScript's deliberate non-extension to numeric range types is instructive. Within a single transition row, after `when F > n` has matched, the type checker *could* narrow `F`'s range for subsequent expressions in that row body. But:

1. **Low marginal value.** The runtime already catches violations post-execution (the invariant fires and rejects the transition). The inspector provides exact simulation. Static guard-range propagation would only move the discovery from run time to compile time for a narrow class of cases.
2. **Complexity cost.** Range narrowing adds a new dimension to the type checker's state machine.
3. **Industry posture.** TypeScript and Kotlin both explicitly declined this for general-purpose languages. The consistent cross-language decision is a data point.

Range propagation through guards is a dead end as a general-purpose extension. It might have narrow value in a future "prove-the-invariant-is-never-violated" analysis, but that is a future research question.

### Cross-entity or temporal reasoning

The horizon domain here is about one precept's current state/value configuration, not distributed workflows, time progression, or inter-entity references. Pulling in temporal or relational reasoning would be a category change.

### Heuristic "probably impossible" diagnostics

Guessing that a rule set looks contradictory based on pattern matching would violate Precept's proof-oriented diagnostic posture. This lane should raise only diagnostics the checker can justify from an explicit semantic model.

### Rich string, regex, and collection-cardinality proofs as phase one

String membership (e.g., `contains`) and collection cardinality predicates are interesting, but they are not the fastest path to user value. Numeric intervals, null facts, and finite-value sets cover the clearest contradiction/deadlock cases in today's language. String and collection reasoning are a follow-on if and when the simpler domain is implemented and the usage data supports it.

## Why This Is a Horizon Domain

### No open proposals, and that is intentional

The design doc already specifies C4 and C5 as planned compile-time checks. The task for those two is implementation, not research — the *what* is clear. This document's purpose is to understand what lies *beyond* C4/C5 and whether it constitutes a coherent proposal domain. The answer: yes, there is a coherent next layer, but it does not yet constitute a proposal for the following reasons.

### The implementation cost of C4/C5 must be understood first

C4 and C5 require building the per-field interval reduction pass over the expression AST — the core of all further static reasoning. Until that pass exists and its performance and coverage characteristics are known, extending it further is premature. The research here establishes the theoretical grounding for that pass; the implementation will establish its practical limits.

### The expression language expansion batches come first

Batch 2 research (expression expansion, entity-modeling surface) includes computed fields. Computed field expressions extend the AST with derived-value nodes. The interval reduction pass must handle computed fields correctly — a derived field's interval is a function of its source fields' intervals. Implementing interval reduction before the expression language is stable would require revisiting the analyzer when new expression forms land.

### The value of the next layer is bounded

Beyond C4/C5 and the global invariant satisfiability check (Contract 5), the remaining extensions have diminishing marginal value relative to what the inspector already provides. The inspector (`precept_inspect`) provides exact runtime simulation for any specific data state. Static analysis adds value precisely for *definition-level* mistakes that exist before any instance is created:

- Global invariant infeasibility: catches "this precept can never have a valid instance."
- C4: catches "this state's entry/residence conditions are self-contradictory."
- C5: catches "this state is permanently unexitable."

These three cover the set of definition-level static mistakes that produce structurally broken states. Beyond them, static analysis would require runtime data values — and that is the inspector's domain.

### The boundary is clean and defensible

The right framing for a future proposal: *"Implement C4/C5 (already spec'd), then extend to global invariant satisfiability."* That is a contained, bounded proposal. It does not require new language surface, new expression forms, or external solver dependencies — only the interval reduction pass and a set of diagnostic emission rules that follow the existing severity policy.

That proposal should be written after C4/C5 ship and after the implementation team understands the performance and coverage characteristics of the interval reduction pass.

---

## Summary Table

| Static reasoning form | Precept check | Status | Tractability | Closest precedent |
|---|---|---|---|---|
| Same-preposition contradiction | C4 | Designed, not implemented | Per-field interval analysis | DMN table overlap analysis, Cedar shadow analysis, Drools conflict detection |
| Cross-preposition deadlock | C5 | Designed, not implemented | Per-field interval analysis | Alloy unreachable states |
| Initial-state default violation | C3 | **Implemented** | Constant evaluation | PostgreSQL `DEFAULT` + `CHECK` interaction |
| Global invariant satisfiability | Future extension | Not designed yet | Same pass as C4/C5 | Alloy vacuous facts |
| Guard range propagation | Low-value extension | Deferred | Tractable but low payoff | TypeScript (declined), Kotlin (declined) |
| Cross-field satisfiability | Rejected direction | N/A | LP-tractable but author-opaque | OCL tools (academic only) |
| Full SMT-backed analysis | Rejected direction | N/A | Decidable but wrong trade-offs | Dafny (verification language), Cedar (authorization) |

---

## Criteria for Activating Proposal Work

This domain should stay in horizon research until all of the following are true:

1. **The target fragment is frozen.** The team agrees on a first-wave proof fragment: null facts + numeric intervals + finite equality domains, with explicit non-goals.
2. **Value-domain work has settled enough.** The active type-system and constraint-composition lanes have clarified which scalar domains and field-local constraints Precept wants to reason about. Static analysis should not outrun the value model it analyzes.
3. **There is concrete sample or user pressure.** At least a small corpus of real contradictions/deadlocks exists in samples, tests, or support feedback so the proposal is grounded in author pain rather than theoretical elegance.
4. **Diagnostic wording is designed up front.** The team has example messages that are understandable to authors without exposing abstract-interpretation or solver internals.
5. **Compile-path cost is acceptable.** A prototype or spike shows that always-on analysis is fast enough for language-server and MCP usage.
6. **The team is willing to stay sound and incomplete.** If the proposal goal drifts toward "catch every impossible business rule," it is not ready. The right activation criterion is "catch a useful, explainable fragment with high trust."

## PM Read

The research points toward a narrow, high-trust path:

- start from abstract-interpretation style interval and finite-set reasoning,
- extend today's null narrowing rather than replacing it,
- keep contradiction/deadlock proofs limited to fragments authors can understand from the diagnostic text,
- and treat solver-backed satisfiability as a later escalation path, not the opening move.

That keeps the lane aligned with Precept's core identity: deterministic, inspectable, compile-time honest, and configuration-like rather than theorem-prover-like.

---

## Key References

- Cousot, P. & Cousot, R. "Abstract Interpretation: A Unified Lattice Model for Static Analysis of Programs." POPL 1977. https://doi.org/10.1145/512950.512973
- Alloy Analyzer (MIT): https://alloytools.org/documentation.html
- OCL Specification 2.4 (OMG): https://www.omg.org/spec/OCL/
- DMN Specification (OMG): https://www.omg.org/spec/DMN/
- Camunda DMN docs: https://docs.camunda.io/docs/8.7/guides/create-decision-tables-using-dmn/
- Dafny language and verifier: https://dafny.org/
- Z3 theorem prover (Microsoft): https://microsoft.github.io/z3guide/
- Cedar policy language: https://www.cedarpolicy.com/en
- OPA documentation: https://www.openpolicyagent.org/docs/latest/
- Drools decision-table verification: https://docs.drools.org/7.74.1.Final/drools-docs/html_single/#_verifying_and_validating_decision_tables
- TypeScript narrowing handbook: https://www.typescriptlang.org/docs/handbook/2/narrowing.html
- Kotlin smart casts: https://kotlinlang.org/docs/typecasts.html#smart-casts
- PostgreSQL CHECK constraints: https://www.postgresql.org/docs/current/ddl-constraints.html#DDL-CONSTRAINTS-CHECK-CONSTRAINTS
- FluentValidation documentation: https://docs.fluentvalidation.net/
- Pydantic validators: https://docs.pydantic.dev/latest/concepts/validators/
- Apron numeric abstract domains: https://antoinemine.github.io/Apron/doc/
- SMT-LIB standard: https://smt-lib.org/

