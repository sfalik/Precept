# The Hybrid Model

> **Status:** Draft B (independent draft for comparison) — written against the now-deleted root-level hybrid-model copy; rewrite work now lives under `docs/Working/posture-v2-support/`; links are relative to `docs/`
> **Audience:** compiler, runtime, and tooling authors; precept authors who want to know, for any line they write, where it will be enforced and what happens when it fails

**How to read this document.** §1 states the model whole, in one page. §2 explains why the model has two halves rather than one. §3 grounds both halves in Precept's philosophy — not as decoration, but because the split *is* a philosophical commitment made mechanical. §4 names the routing axis and defends it against the two most tempting wrong axes. §5 and §6 are the two halves themselves — compile-time prove-or-reject and runtime governance — at deliberately equal depth. §7 is the operational decision procedure: numbered steps you can run on any line of a definition. §8 is a set of ten worked examples spanning money, quantities, instants, strings, collections, and choices. §9 lists what the model excludes, including the one disposition that looks plausible and is banned. §10 places this document among the other canonical docs.

---

## 1. The model in brief

Precept's guarantee is singular: **an entity governed by a precept can never hold an invalid configuration.** The Hybrid model is the account of how that one guarantee is delivered by two cooperating dispositions:

**Prove-or-reject** — the compile-time disposition. Every fault-prone operation over a value the definition *computes* — a `set` expression, a computed field's derivation, an intermediate result — generates a proof obligation. The proof engine either discharges the obligation before any entity exists, or the definition is rejected with the missing premise named. There is no third outcome for computed values: no deferral, no runtime check standing in for the proof.

**Governed** — the runtime disposition. Every value the outside world *supplies* — event arguments, construction inputs, edits of editable fields — and every declared relationship among such values, is enforced at runtime on every operation, through two mechanisms: **ingress** (per-value, at the boundary, before anything runs) and the **post-mutation sweep** (whole-configuration, after all mutations, before anything commits). There is no unchecked path into a governed entity, and no partially-applied failure: a violating operation is refused whole and the entity is untouched.

One axis decides which disposition applies to any given site: **provenance** — is the value produced *by* the definition, or supplied *to* it? Computed values are proven or the definition is refused. Supplied values, and relationships among independently-supplied values, are governed. Nothing else routes — not the difficulty of the proof, not the type of the value, not the spelling or position of the constraint.

The two dispositions are joined by a **dual-role principle**: every declared constraint — every field modifier, every `rule`, every `ensure` — simultaneously (a) *governs* the external values it speaks about, at runtime, and (b) *feeds the prover* whatever decidable fact it states, as a premise available to discharge proof obligations elsewhere in the definition. A constraint is never assigned to one role; it always carries both, even when one role happens to be idle. This is the seam that makes the model a single net rather than two: a compile-time proof may rest on a premise whose truth at runtime is exactly what governance enforces. [`compiler-and-runtime-design.md`](compiler-and-runtime-design.md) §1 introduces this composition; this document is its full treatment.

## 2. Why a hybrid

The model is a hybrid because both pure alternatives fail Precept's own commitments.

**Pure compile-time ("prove everything")** would require every declared relationship — including relationships among values the outside world has not supplied yet — to be discharged symbolically before acceptance. But a relational invariant over independently-supplied fields is not a statement about the definition; it is a statement about *future external data*. There is nothing for a compiler to prove: the fields' future values are unconstrained by anything the definition computes. Worse, many legitimate domain invariants are symbolically undecidable (nonlinear identities, products of variables), and a prove-everything posture would force their rejection — banishing exactly the rules a domain expert most needs to write. A domain integrity engine that refuses `rule Width * Height <= BedArea` because interval arithmetic cannot bound a product is not honest rigor; it is expressiveness amputated to flatter the prover.

**Pure runtime ("check everything when it happens")** would take every declared constraint — including bounds on values the definition itself computes — and enforce them per-entity, per-operation, discovering violations one execution at a time. This forfeits the core promise. A definition whose own arithmetic can produce an out-of-bounds value is *wrong as a definition* — wrong for every entity, provably, before any entity exists — and a posture that ships it anyway converts a per-definition compile error into an unbounded stream of per-entity runtime failures, each surfacing at the worst possible moment and addressed to a caller who did nothing wrong. Detection where prevention was possible ([`philosophy.md`](philosophy.md)).

The hybrid keeps each half where it is *sound and honest*: proof where the subject matter is the definition's own computation (fully visible at compile time, so provable — and if not provable, the definition is incomplete and should say so); governance where the subject matter is future external data (invisible at compile time, so unprovable in principle — and checkable with certainty at the only moment it exists: runtime, on a concrete configuration). Neither half is a concession. Each is the *correct* epistemic tool for its half of the world.

## 3. Philosophical grounding

The Hybrid model is not an implementation strategy that happens to satisfy the philosophy; it is the philosophy of [`philosophy.md`](philosophy.md), executed. Commitment by commitment:

**Prevention, not detection.** Prove-or-reject is prevention in its strongest form: the invalid configuration is impossible because the definition that could produce it never compiles. Governance is prevention too — the philosophy's own framing — because a governed refusal happens *before* commit: the violating configuration is never observable, never persisted, never even transiently real. Both dispositions prevent; they differ only in when the prevention is *established* (per-definition at compile time vs. per-operation at runtime), never in whether an invalid configuration can exist. It cannot, on either path.

**Governance, not validation.** The runtime half is named "governed," not "validated," deliberately. Validation is a check invoked at chosen boundaries by cooperating code — bypassable by any path that forgets to call it. Governance is structural: ingress and the sweep are not called by the host, they *are* the operation surface. There is no way to fire an event, construct an entity, or edit a field except through them. A rule is not something the host runs; it is something the entity *is under*.

**One file, complete rules.** Both dispositions read the same single source: the `.precept` definition. The proof engine's premises are the declared modifiers, rules, and guards — nothing imported, nothing configured elsewhere. The runtime's enforcement set is the same declarations. A reader of the file sees everything that is proven and everything that is governed; there is no second file where the "real" checks live.

**Determinism.** Same definition, same data, same outcome — on both halves. A proof either discharges or it does not, for a given definition, every time; there is no heuristic that proves on Tuesday. A governed operation either violates the completed working copy's constraints or it does not, for given inputs, every time; there is no sampling, no advisory mode, no severity dial.

**Compile-time structural checking.** The prove-or-reject half is the fault-prevention wing of the same commitment that rejects unreachable states and contradictory guards: properties of the *definition* are settled at compile time. The Hybrid model extends the list — division by zero, results outside declared bounds, empty-collection access — without ever extending it across the provenance line into claims about future external data, which would be structural checking's dishonest imitation.

**Honesty about approximation.** The model's most understated commitment. Prove-or-reject never bluffs: an obligation that cannot be discharged is a rejection, stated with the missing premise, not a silently weakened guarantee. Governance never bluffs either: it does not claim compile-time knowledge it lacks — it claims, and delivers, something different (no violating configuration will ever commit). And the model refuses the one arrangement that *would* be a bluff: presenting a runtime check on a computed value as if the computation had been proven (§9).

**The primary author is the domain expert.** The routing axis is one a domain expert already thinks in: *is this number mine (computed by my rules) or theirs (supplied by the world)?* No knowledge of decidability, interval arithmetic, or prover internals is needed to predict where any declaration is enforced. The expert who writes `rule Deposits >= Withdrawals because "..."` has stated a domain truth; the model makes it simultaneously a runtime contract and a compile-time premise without asking the expert to know the difference.

## 4. The routing axis: provenance, never difficulty

The single question that classifies any site is: **where does the value come from?**

- **Derived** — produced by a Precept expression: the right-hand side of `set X = …`, a computed field's derivation, any intermediate subexpression of either, a `default` expression. Derived values route to **prove-or-reject**.
- **Externally supplied** — crossing the boundary from the host: an event argument, a construction input, an edit of an editable field. Externally-supplied values route to **governance at ingress**.
- **A relationship among independently-set fields** — a declared invariant relating two or more fields whose values each originate externally (supplied, edited, or accumulated from external supply — the point is that neither is computed *from the others in the relationship*). Such relationships route to **governance at the post-mutation sweep**.

Two wrong axes must be named and refused, because both are seductive:

**Wrong axis 1: proof difficulty.** "If the prover can discharge it, prove it; otherwise check it at runtime." This axis is incoherent, and the model rejects it totally. Difficulty (decidability) plays exactly one role: for an obligation *already routed* to prove-or-reject, it is the oracle that determines discharge versus rejection — never a router that determines proof versus governance. A hard-to-prove obligation on a derived value is a **rejection**, not a runtime check. An easy-to-state relationship over external fields is **governed**, even when the prover could trivially reason about its form — because there is no obligation to prove: the subject is future data, not the definition. Routing by difficulty would make the guarantee's strength a function of prover sophistication — silently shifting sites between dispositions as the prover improves — and would launder unproven computations into runtime checks nobody chose. Provenance is stable; difficulty is not.

**Wrong axis 2: spelling.** `field Score as integer max 100` and `field Score as integer` + `rule Score <= 100` are the same constraint. Modifier versus rule, position in the file, one declaration versus two — none of it affects classification. Both spellings govern external writes to `Score` and both feed the prover the premise `Score <= 100`. The one distinction that *looks* like spelling but is actually provenance: `set X = e` versus `rule X == e`. These are not two spellings of one thing. In `set X = e`, the definition computes X — X is derived, and the write carries a proof obligation against X's declared bounds. In `rule X == e`, nothing is computed — X is independently set, the rule constrains a relationship between externally-originated values, and it is governed at the sweep. Rewriting one into the other is not a rephrasing; it is a redesign of the entity (the field changes provenance, from computed to externally-settable). The model treats them differently because they *are* different — and this difference is the axis itself, seen up close.

## 5. Prove-or-reject: the compile-time disposition

### 5.1 What it covers

Every fault-prone operation whose operands include a derived value. The fault classes are those the language treats as compile-time impossibilities ([`language/precept-language-spec.md`](language/precept-language-spec.md) §0.7): division by zero, numeric results outside a field's declared bounds, empty-collection access, and their kin. Concretely, obligations attach to:

- **Computed writes into bounded fields** — `set Balance = Deposits - Withdrawals` into a `nonnegative` field obligates a proof that the difference is non-negative on that path.
- **Computed field derivations** — `field UnitCost as money in 'USD' <- TotalCost / Units` obligates a proof that `Units` is nonzero, and that the quotient satisfies any bounds `UnitCost` declares.
- **Defaults** — `default 150` under `max 100` is a derived value like any other and is folded against the declared constraints; a violating default is a rejected definition, not a runtime surprise.
- **Collection access preconditions** — `dequeue Q` or `Q.peek` on a possibly-empty queue obligates a proof of non-emptiness on the path (typically discharged by a `when Q.count > 0` guard).

### 5.2 The verdict space

For each obligation the proof engine reaches one of three verdicts ([`language/precept-language-spec.md`](language/precept-language-spec.md) §0.6):

- **Proven** — the obligation follows from the available premises. The definition compiles; the property holds for every entity and every execution, guaranteed before any entity exists.
- **Proven violating** — the premises entail a *counterexample*: the engine can exhibit concrete operand values, permitted by every declared constraint, under which the fault occurs. The definition is rejected with the witness shown.
- **Unresolved** — neither proven nor refuted from the available premises. The definition is rejected, and the rejection states the **weakest missing premise** — the fact which, if declared, would discharge the obligation.

Both non-proven verdicts reject. This is the "or-reject" half of the name, and it is absolute for derived values: an unresolved obligation is not "checked at runtime instead" — it is the compiler telling the author their definition is missing a stated truth.

### 5.3 What the prover reasons from

Premises come only from the definition itself — the one-file commitment made mechanical:

1. **Declared constraints on operands** — modifiers (`nonnegative`, `positive`, `min`/`max`, `maxlength`, cardinality bounds) and rules, via their premise role. `Deposits nonnegative` contributes the interval `[0, +inf)`; `rule Deposits >= Withdrawals` contributes the relational fact.
2. **Path facts** — `when` guards on the row where the operation runs. Inside `on Settle when Deposits >= Withdrawals`, the guard's condition is a premise for that row's obligations.
3. **Structural exclusions** — a sibling `reject` row whose guard captures the violating case narrows the interval on the remaining rows.
4. **Statically-known values** — literals and constants.

### 5.4 Why derived values are never governed instead

Three independent reasons, any one of which would suffice:

1. **A refusal of a computed value has no addressee.** Every governed refusal is a message to an external party who supplied something and can supply something else. A computed value has no supplier but the definition itself — and the definition cannot re-decide at runtime. Refusing `Settle` because the definition's own subtraction came out negative hands the caller — who supplied nothing — a failure they cannot correct, about arithmetic they cannot see. That is a fault report, not governance.
2. **The violation is a property of the definition, knowable per-definition.** If the definition's arithmetic can violate its own declared bounds, that fact is true once, for all entities, and visible at compile time. Deferring it to runtime converts one compile error into unbounded per-entity failures — detection where prevention was available, the exact inversion of the core commitment.
3. **The deferral would be invisible.** Nothing in the source would distinguish "this bound is proven" from "this bound is checked per-operation." The author, the reviewer, and the reader would all see the same declaration and receive silently different guarantees. Honesty about approximation forbids exactly this.

### 5.5 The author's remediation paths

A rejection is a demand for a missing truth, and there are four honest ways to supply it — each of which puts the truth *into the definition* where the prover and every future reader can see it:

1. **Declare the relationship as a rule** — `rule Deposits >= Withdrawals because "..."`. The rule's premise role discharges the obligation; its governance role enforces the relationship on the external fields at runtime. This is the richest fix: the domain truth becomes part of the contract (§8.9).
2. **Guard the path** — `on Settle when Deposits >= Withdrawals -> …`. The fact holds inside the guard; outside it the event is simply unavailable (`Unmatched`).
3. **Author the failing case** — a sibling `reject` row that captures the violating region and refuses it in the author's own words (`EventOutcome.Rejected`), narrowing the remaining rows to the provable region.
4. **Tighten a declaration** — strengthen an operand's modifier (`positive` instead of `nonnegative`; `maxlength 50` on the argument) so the carried constraint discharges the containment directly.

## 6. Governed: the runtime disposition

Governance is not the junior partner, and this section gives it the same rigor as §5. Its subject matter — external data and relationships among external data — is the half of the world about which compile-time proof is *impossible in principle*, not merely hard: no analysis of the definition can bound what the world will supply next Tuesday. Governance is the correct, complete answer to that half, and it delivers the identical bottom line as proof: **no invalid configuration ever commits.**

### 6.1 What it covers

- **Every externally-supplied value's own contract** — the declared constraints on event arguments, construction inputs, and editable fields: bounds, `positive`/`nonnegative`, `notempty`, `maxlength`, cardinality (`mincount`/similar) on supplied collections, choice membership.
- **Every declared relationship among independently-set fields** — rules (guarded or not), state ensures, event ensures — whether or not the relationship also serves any proof.
- **Structural access itself** — whether a field is even editable in the current state; whether an event matches any row from the current state.

### 6.2 Mechanism 1 — ingress: per-value enforcement at the boundary

Ingress is the door. When a value arrives — an argument in a `Fire`, a field in an `Update`, an input to construction — it is checked against *its own declared constraint* before any mutation runs, before a working copy exists, before anything derives from it ([`runtime/runtime-api.md`](runtime/runtime-api.md)). Construction mirrors operations: inputs pass the same governance as arguments.

Properties worth stating precisely:

- **Per-value and operation-blind.** Ingress asks one question — does this value satisfy its own declaration? — with no knowledge of what the operation will do with it. `positive` on a divisor is enforced whether or not anything divides by it this time.
- **Earliest possible refusal.** A bad value never reaches the working copy; the caller receives `InvalidArgs` (event arguments) or the field-level analogue for edits ([`runtime/result-types.md`](runtime/result-types.md)) naming the violated declaration. `FieldNotEditable` fires even earlier — before the value is considered at all.
- **Structurally blind to combinations.** Ingress cannot refuse a value for being wrong *in combination* with current state, because each value is impeccable by itself. That is not a weakness to patch; it is the precise boundary between ingress's question and the sweep's.

### 6.3 Mechanism 2 — the post-mutation sweep: whole-configuration enforcement before commit

The sweep is the model's second enforcement point and the only one that can see relationships. Every operation executes against a **working copy**: mutations apply, computed fields recompute, and then — before anything becomes visible — every applicable constraint (global rules, state ensures for the copy's state, event ensures for the fired event) is evaluated against the **completed** working copy ([`language/precept-language-spec.md`](language/precept-language-spec.md) §3A.4; [`runtime/runtime-api.md`](runtime/runtime-api.md)). If any fails, the working copy is **discarded whole** and the caller receives `ConstraintsFailed`, carrying the violated constraints and their `because` texts. The entity is exactly as it was; the invalid configuration never existed, even transiently.

Four properties define the sweep:

1. **It sees combinations.** `Balance >= MinBalance` is invisible per-value and trivial per-configuration. The sweep evaluates the relationship against concrete values — a question that is always decidable at runtime, however undecidable the symbolic form is. This is why nonlinear invariants cost the author nothing (§8.5): evaluation does not inherit proof's limits.
2. **It is symmetric over the relationship.** The rule condemns the *pair*, not a field. Raising the floor above the balance and dropping the balance below the floor are refused identically, whichever operation builds the violating copy.
3. **It is atomic and collect-all.** No partial application ever; all violated constraints are reported together, not first-failure-only.
4. **It is unconditional.** Every operation, every path, every constraint applicable to the resulting configuration. There is no fast path around it and no host-visible way to suppress it. Governance, not validation.

### 6.4 The dual role, seen from the governed side

Every governed constraint also feeds the prover (§1). Usually one role dominates: a rule over external fields whose facts no proof needs is governed with an idle premise role; `positive` on a divisor is ingress-governed *and* the premise that discharges the division proof. The model's signature case is one construct exercising both roles on different targets: `rule Deposits >= Withdrawals` *governs* the two external totals (sweep) while its premise *proves* the derived `Balance` (compile time). The rule is never "used up" by the proof — its governance is what keeps the premise true of the running data, which is exactly what makes the compile-time certificate sound. The two roles are two ends of one chain.

### 6.5 What governance guarantees — and what it honestly does not

Governance guarantees the **outcome**: no configuration violating a declared constraint will ever commit. It does not guarantee the **epistemics** of proof: it cannot tell you at compile time *whether* a given operation will succeed — that depends on data that does not exist yet. This is not a weaker promise; it is a different question. Proof answers "can this definition ever go wrong?" (about the definition, per-definition, in advance). Governance answers "is this concrete operation acceptable?" (about the data, per-operation, at the moment of truth). Precept never trades one question's answer for the other's, and never presents either as the other — the honesty commitment, applied to enforcement itself.

One more honest boundary: governance binds the **contract path** — the operation surface of `Create`/`Restore`/`Fire`/`Update`. Data corrupted outside that surface is the defense-in-depth trap layer's business ([`runtime/result-types.md`](runtime/result-types.md) fault taxonomy), not governance's.

## 7. The decision procedure

Run these steps, in order, on any declared constraint or any fault-prone operation.

**Step 1 — Name the thing.** Is it a declared **constraint** (modifier, `rule`, state/event `ensure`) or a fault-prone **operation** (a computed write into a bounded field, a division, a collection access, a computed field derivation, a `default`)?

**Step 2 — If it is a constraint: it has both roles; classify only its enforcement point.** Do the fields it speaks about originate externally? (They do, in every legal case — a constraint over a *derived* field is really a bound on the expression that computes it, which Step 4 handles as the operation's obligation.) Then:
- Single-field, checkable per value → its governance role enforces at **ingress** on every supply of that field.
- Cross-field or state-scoped or event-scoped — anything about a *combination* → its governance role enforces at the **post-mutation sweep**.
- Either way, its premise role feeds the prover whatever decidable fact it states. You are done.

**Step 3 — If it is an operation: ask provenance of the value it consumes.** Produced by a Precept expression (a `set` RHS, a `<-` derivation, a subexpression, a `default`)? → derived → continue to Step 4. Supplied whole from outside and merely stored? → the *storage* is fine; the supplied value's own contract is governed at ingress (Step 2), and any containment obligation on the assignment discharges from the carried constraint — or rejects, if the carrier is wider than the target (§8.8).

**Step 4 — Derived value: the obligation is prove-or-reject. Full stop.** Gather the premises (operand constraints, path guards, sibling-row exclusions, literals). Discharged → proven, compiled, done. Not discharged → the definition is rejected; go to Step 5.

**Step 5 — Rejected: supply the missing truth, in the definition.** Choose among the four remediation paths of §5.5 — declare the rule, guard the path, author the reject row, tighten a declaration. Never "let the runtime catch it": that option does not exist for derived values (§9).

**Step 6 — Cross-check with the guardrails.**
- *Same provenance ⇒ same disposition*, whatever the spelling or position (§4).
- *Difficulty never routes.* An unprovable derived obligation rejects; an unprovable relationship over external fields was never an obligation (§8.5).
- *`set X = e` vs `rule X == e` is provenance, not phrasing* (§4).
- *A constraint is never consumed by proof* — serving as a premise does not remove its runtime enforcement, and vice versa.

**Step 7 — Sanity: locate the failure surface.** Prove-or-reject failures surface at compile time as rejections naming missing premises or witnesses. Governance failures surface as typed refusals: `InvalidArgs`/field-level ingress refusals at the door, `ConstraintsFailed` from the sweep, `Rejected` from authored reject rows, `Unmatched` from unavailable operations ([`runtime/result-types.md`](runtime/result-types.md)). If you cannot say which of these a hypothetical failure would be, re-run from Step 1 — you have misclassified the provenance.

## 8. Worked examples

Ten examples across different domains and constraint kinds. Each names every field's provenance, walks every obligation to its verdict or its enforcement point, and states what would change if the definition changed. No single example is the paradigm — the procedure of §7 is, and it lands identically on each.

### 8.1 Single-field ingress: a supplied percentage

```precept
precept PromotionEntry

field DiscountPercent as decimal default 0.0 nonnegative maxplaces 2

event ApplyPromotion(DiscountPercent as decimal positive max 100 maxplaces 2)

on ApplyPromotion
    -> set DiscountPercent = ApplyPromotion.DiscountPercent
```

**Provenance.** The argument is externally supplied; the field stores it directly — nothing is derived beyond the pass-through.

**Governed, at the door.** The argument declares `positive max 100 maxplaces 2`. Ingress checks the concrete supplied value against that declaration the moment the `Fire` arrives: `-5`, `101`, or `12.345` is refused with `InvalidArgs` before any working copy exists. This is per-value and operation-blind — the argument is held to its contract regardless of what the row does with it.

**Proven, trivially.** The assignment flows a value carrying `(0, 100]` into a field bounded `[0, +inf)`; the carried constraint is inside the target bound, so the containment obligation discharges directly from the carrier. The compile-time proof and the runtime door are two ends of one chain: the proof rests on the argument *carrying* `positive max 100`; ingress is what makes that true of every actual value.

### 8.2 A guarded business rule with an idle premise role

```precept
precept TrialSubscription

field ConversionStatus as choice of string("NotAttempted", "Converted") default "NotAttempted"
field PlanMonthlyFee as money in 'USD' default '0.00 USD' nonnegative

rule PlanMonthlyFee > '0.00 USD' when ConversionStatus == "Converted" because "A converted subscription must carry a positive monthly fee"

event Convert(MonthlyFee as money in 'USD' positive)

on Convert
    -> set ConversionStatus = "Converted"
    -> set PlanMonthlyFee = Convert.MonthlyFee
```

**Provenance.** Both fields are externally driven — one by an argument, one by the row's choice assignment tracking an external act. The rule relates them; nothing in the rule is computed *from* the other field.

**Governed, at the sweep.** The rule is a cross-field, guarded relationship — exactly what per-value ingress cannot see (a fee of `'0.00 USD'` is impeccable by itself; the violation exists only in combination with `Converted`). The **post-mutation sweep** evaluates the whole guarded rule against every completed working copy: a copy with `ConversionStatus == "Converted"` and a zero fee is condemned, discarded whole, refused with `ConstraintsFailed`. The `when` guard does not weaken anything — it scopes the rule precisely; unconverted configurations satisfy it vacuously, converted ones are held to it absolutely, in both directions, on every operation that could produce either.

**The premise role is idle — and that is normal.** No derived value in this precept needs the rule's fact. The rule still carries its premise role (a future computed field could lean on it); today it simply has no customer. A constraint with an idle premise role is not a lesser constraint — most business rules live their whole lives this way, fully governed, never consulted by a proof.

### 8.3 An event ensure playing both roles: quantities with units

```precept
precept StockItem

field QuantityOnHand as quantity in 'kg' default '0 kg' nonnegative

event RecordShrinkage(Qty as quantity in 'kg' positive)

on RecordShrinkage ensure RecordShrinkage.Qty <= QuantityOnHand because "Cannot write off {RecordShrinkage.Qty} when only {QuantityOnHand} is on hand"

on RecordShrinkage
    -> set QuantityOnHand = QuantityOnHand - RecordShrinkage.Qty
```

**Provenance.** `Qty` is supplied; `QuantityOnHand` accumulates external supply; the subtraction's result is **derived**.

**Three obligations, three answers.**
- The argument's own contract (`positive`): **ingress**. A zero-or-negative write-off is refused at the door, `InvalidArgs`.
- The ensure (`Qty <= QuantityOnHand`): a relationship between an argument and current state — a combination, invisible per-value — **governed** in the post-fire constraint pass, surfacing as `ConstraintsFailed` with the `because` text when violated. Event-scoping narrows *when* it is evaluated, not how absolutely it binds.
- The subtraction into a `nonnegative` field: **prove-or-reject**. From the field and argument bounds alone the difference is unbounded below — undischargeable. The ensure's **premise role** supplies the missing fact on exactly the paths where this event's rows run: under `Qty <= QuantityOnHand`, the difference is provably in `[0 kg, +inf)`. Discharged at compile time.

One declared constraint, two roles, two targets: the ensure *governs* the external combination and *proves* the derived write. Remove the ensure and the definition is rejected — not "checked at runtime instead."

### 8.4 Cross-field relational governance, twice: money and instants

```precept
precept InsuranceClaim

field CoverageLimit as money in 'USD' default '0.00 USD' nonnegative editable
field ApprovedAmount as money in 'USD' default '0.00 USD' nonnegative editable
field ShippedAt as instant optional editable
field DeliveredAt as instant optional editable

rule ApprovedAmount <= CoverageLimit because "An approval cannot exceed the policy's coverage limit"
rule DeliveredAt >= ShippedAt when ShippedAt is set and DeliveredAt is set because "Delivery {DeliveredAt} cannot precede shipment {ShippedAt}"
```

**Provenance.** All four fields are independently set — each edited on its own schedule, none computed from another. Two relationships, one in money, one in time.

**Both governed at the sweep, identically.** Neither rule has a proof subject: nothing is derived anywhere in this precept, so prove-or-reject has no obligation to route. Neither is ingress-visible: an approval of `'8,000.00 USD'` is impeccable by itself — the violation exists only against the current `'5,000.00 USD'` limit; a delivery instant is impeccable by itself — the violation exists only against the current shipment instant. The sweep condemns the violating *pair* symmetrically: raising the approval above the limit and lowering the limit below the approval are refused alike; back-dating delivery and forward-dating shipment are refused alike. Working copy discarded whole, `ConstraintsFailed`, entity untouched.

**The point of the twin.** The classification never consulted the type. Money and instants, numeric order and temporal order — the procedure of §7 asks provenance, finds "independently set," finds "combination," and lands on the sweep both times. The Hybrid model is type-agnostic; provenance is the only input.

### 8.5 The permanent resident: a nonlinear identity that will never be proven — and never needs to be

```precept
precept ReceivingReport

field ReportedUnitCost as money in 'USD' default '0.00 USD' nonnegative editable
field ReportedQuantity as decimal default 0 nonnegative editable
field ReportedTotalCost as money in 'USD' default '0.00 USD' nonnegative editable

rule ReportedTotalCost == ReportedUnitCost * ReportedQuantity because "The reported total must equal unit cost times quantity — a mismatched report is a data-entry error, not a judgment call"
```

**Provenance.** All three fields are **reported** — supplied independently from an external document. Critically, `ReportedTotalCost` is *not* computed by the definition (that would be `set` or `<-`, and a different precept): the author's intent is to cross-check three independently transcribed numbers against each other.

**Governed, permanently — a stable disposition, not a deferred item.** The rule is a nonlinear relationship (a product of two variables) over external fields. Symbolically, interval reasoning extracts nothing useful from it; no future prover milestone changes its classification, because classification never depended on provability. There is no proof obligation here to discharge or reject: nothing is derived, so prove-or-reject has no subject. The **sweep** enforces the identity on every operation — evaluating a concrete product against a concrete total, which is trivially decidable however intractable the symbolic form — and any edit that breaks the identity is refused whole with `ConstraintsFailed`.

**Why this example matters.** It is the clearest counterexample to the "difficulty routes" fallacy (§4). The rule is maximally hard for a prover and maximally easy for the sweep, and the model never once weighed that: it asked provenance, got "external," and was done. Governed-forever is not a queue position. It is the correct, final answer for relationships whose subject is external data.

### 8.6 Division at the composition seam

```precept
precept UnitCosting

field TotalCost as money in 'USD' default '0.00 USD' nonnegative editable
field Units as decimal default 1 positive editable

field UnitCost as money in 'USD' <- TotalCost / Units
```

**Provenance.** `TotalCost` and `Units` supplied; `UnitCost` **derived** by the computed-field expression.

**Prove-or-reject, discharged by a carried constraint.** The division obligates a compile-time proof that `Units` is nonzero. `positive` carries the interval `(0, +inf)` — zero excluded — and the obligation discharges from the carrier. Had `Units` declared only `nonnegative` (zero included) or nothing, the verdict would be unresolved and the definition **rejected**, naming the fix: a stronger modifier, a rule, or a guard on the consuming path.

**Governance keeps the premise true.** The proof rests on `Units` *carrying* `positive`; **ingress** makes it so: an edit supplying `Units = 0` is refused at the door and never reaches the working copy, so the division the compiler certified is never asked to divide by zero. The certificate and the door are one mechanism viewed from two sides — this is the composition seam of §6.4 in its smallest complete form.

### 8.7 Collections: a proven access precondition and a governed cardinality

```precept
precept HelpDeskTicket

field AgentQueue as queue of string maxlength 200

state Open initial
state Assigned terminal

event RegisterAgent(AgentName as string notempty maxlength 200)
event Assign

from Open on RegisterAgent
    -> enqueue AgentQueue RegisterAgent.AgentName
    -> no transition

from Open on Assign when AgentQueue.count > 0
    -> dequeue AgentQueue
    -> transition Assigned
from Open on Assign
    -> reject "No agent is available — RegisterAgent first"
```

**Provenance.** The queue's *elements* are supplied (governed at ingress: `notempty maxlength 200` per argument). The `dequeue` is an **operation** whose fault mode — empty-collection access — is one of the compile-time fault classes.

**The access is prove-or-reject, and the guard is its premise.** `dequeue` obligates a proof of non-emptiness on its row. The `when AgentQueue.count > 0` guard is a path fact stating exactly that; discharged. Strip the guard and the obligation is unresolved — the definition is **rejected**, never accepted-with-a-runtime-trap: an empty-dequeue failure at runtime would be addressed to no one (the caller supplied no queue), the signature of the banned arm (§9).

**The uncovered case has an authored disposition.** An `Assign` on an empty queue matches the sibling row and returns the author's own refusal, `EventOutcome.Rejected` — the definition decides every reachable case, in the author's words.

**Contrast: cardinality on a *supplied* collection is ingress material.** A construction input like `CatalogItems as set of string notempty mincount 1` declares cardinality on a value crossing the boundary whole — checked at the door, before any working copy, like any other per-value contract. Count consumed by an inside operation → proof premise. Count of a value crossing the boundary → ingress. Same measure, two provenances, two dispositions — the procedure of §7 does not hesitate.

### 8.8 Strings: containment is not only arithmetic

```precept
precept AdvisorRecord

field AdvisorName as string optional maxlength 50

event Approve(Advisor as string notempty maxlength 100)

on Approve
    -> set AdvisorName = Approve.Advisor    # REJECTED at compile time
```

**Provenance.** The argument is supplied; the assignment flows it into the field. The containment obligation on the write is discharged — or not — from the constraint the value carries.

**The rejection.** `maxlength 50` is a decidable bound; length is simply the measure. The carrier admits length up to 100; the target admits up to 50; a 60-character name is a live counterexample — **proven violating**, definition rejected with the mismatch named. The honest fixes are declaration-level: tighten the argument to `maxlength 50`, or widen the field. Each puts the truth where prover and reader both see it.

**The string-domain banned arm.** "Accept it and truncate," or "accept it and refuse the operation when the name comes out too long," is governing a value the definition itself flowed into the field — a refusal with no honest addressee and a loss with no visible declaration. Silent truncation is precisely the quiet approximation the honesty commitment forbids ([`philosophy.md`](philosophy.md)). With the fix applied, ingress governs the (now-tight) carrier at the door — proven write, governed source, the same seam as §8.1 and §8.6, measured in characters.

### 8.9 One rule, two roles, two targets: the settlement account

```precept
precept SettlementAccount

field Deposits as money in 'USD' default '0.00 USD' nonnegative
field Withdrawals as money in 'USD' default '0.00 USD' nonnegative
field Balance as money in 'USD' default '0.00 USD' nonnegative

rule Deposits >= Withdrawals because "An account cannot withdraw more than it has deposited"

event Deposit(Amount as money in 'USD' positive)
event Withdraw(Amount as money in 'USD' positive)
event Settle

on Deposit
    -> set Deposits = Deposits + Deposit.Amount

on Withdraw
    -> set Withdrawals = Withdrawals + Withdraw.Amount

on Settle
    -> set Balance = Deposits - Withdrawals
```

**Provenance.** `Deposits` and `Withdrawals` accumulate external supply — independently set, fed by their own events. `Balance` is **derived**: produced by the `set`, computed from the totals (never the reverse).

**Obligation A — the derived write: prove-or-reject.** `set Balance = Deposits - Withdrawals` into `nonnegative` obligates a proof that the difference is non-negative. Without the rule, the operands are individually non-negative but the difference spans `(-inf, +inf)`: unresolved, **rejected**, missing premise stated — in substance, *establish `Deposits >= Withdrawals`*. With the rule, the premise entails the bound directly: **proven** at compile time, for every entity, before any exists. Balance is never governed at runtime — no sweep clause, no ingress check, nothing on the contract path ever re-examines its non-negativity, because the compiler certified that no execution can violate it.

**Obligation B — the rule itself: governed, and a premise.** The rule relates two independently-set fields, so its governance role enforces at the **sweep**: a `Withdraw` that would push `Withdrawals` above `Deposits` passes ingress (the amount is `positive` — fine by itself), mutates the working copy, and is then condemned by the sweep — copy discarded whole, `ConstraintsFailed` with the `because`. Its premise role is what discharged Obligation A.

**The division of labor, exactly.** One declaration; the derived value proven by its premise, the external relationship governed by its enforcement; and the direction of soundness runs through the seam: the sweep keeps the premise true of the running totals, which is what makes the compile-time certificate for Balance sound forever. Note this is the same structure as §8.3 with a rule in place of an ensure and money in place of kilograms — the pattern belongs to the model, not to bank accounts.

### 8.10 The banned third arm, walked

```precept
precept NaiveSettlement

field Deposits as money in 'USD' default '0.00 USD' nonnegative
field Withdrawals as money in 'USD' default '0.00 USD' nonnegative
field Balance as money in 'USD' default '0.00 USD' nonnegative

event Settle

on Settle
    -> set Balance = Deposits - Withdrawals    # REJECTED: no premise bounds the difference
```

**What the compiler does.** Unresolved obligation (§8.9 without the rule): the definition is **rejected** with the missing premise named. No entity is ever created.

**What the banned alternative would look like — recognize the silhouette.** "Accept the definition; at runtime, whenever `Settle` computes a negative Balance, refuse the operation." Concretely: the working copy is built, the subtraction runs, the copy is discarded, the caller gets a refusal. Structurally it *looks* like the sweep. It is not governance, and the tell is provenance: the refused value was produced by the definition itself. The refusal has no addressee (`Settle` carries no arguments; the caller supplied nothing to correct); the failure was knowable per-definition at compile time and is instead being rediscovered per-entity at runtime; and nothing in the source discloses that this field's bound is checked rather than proven. All three violations of §5.4, in one move. The same silhouette recurs in every domain: the unguarded `dequeue` "caught" at runtime (§8.7), the over-long computed name silently truncated (§8.8), the unproven division "protected" by a runtime trap (§8.6). Whenever a runtime refusal's subject is a value the definition computed, the correct disposition was rejection at compile time — and the honest fixes are §5.5's four, each of which moves the missing truth into the definition.

**What is *not* the banned arm.** The sweep refusing a relationship over external fields (§8.2, §8.4, §8.5) is arm two of the model working as designed — the refused subject is external data in combination. The defense-in-depth traps for out-of-contract data are outside the contract path entirely. The banned arm is only and exactly: a *computed* value, unproven, accepted anyway, checked per-entity.

## 9. What the model excludes

Stated as flatly as the model itself, so absences cannot be mistaken for oversights:

1. **No runtime governance of derived values.** The banned third arm (§8.10). An unprovable computed value is a rejected definition, always.
2. **No routing by difficulty.** Decidability decides *discharge versus reject* for an already-routed obligation; it never decides *proof versus governance* (§4, §8.5).
3. **No advisory mode.** No severity dial, no warn-and-commit, no sampling. Every governed constraint refuses absolutely; every proof obligation rejects absolutely.
4. **No partial application.** No operation ever half-commits; the working copy is discarded whole or committed whole.
5. **No host-side bypass.** There is no operation surface that skips ingress or the sweep; governance is structural, not conventional.
6. **No silent guarantee downgrades.** Nothing in the model may present a runtime check as a compile-time proof, or vice versa; the enforcement point of every declaration is a stable function of provenance, readable from the source.

## 10. Relationship to other canonical documents

- [`philosophy.md`](philosophy.md) — the commitments this model mechanizes. §3 of this document maps them one-to-one; where both speak, the philosophy is the canonical statement.
- [`compiler-and-runtime-design.md`](compiler-and-runtime-design.md) — the pipeline and runtime architecture that implements both dispositions, and the brief statement of the compile-prevention/runtime-governance split that this document expands. That document owns the machinery; this one owns the model and the classification procedure.
- [`language/precept-language-spec.md`](language/precept-language-spec.md) — the normative language semantics: the proof-engine design contract (§0.6), the guarantee contract in author-facing terms (§0.7), and mutation atomicity / the post-mutation sweep (§3A.4). Where this document paraphrases, the spec governs.
- [`runtime/runtime-api.md`](runtime/runtime-api.md) — the operation surface (`Create`/`Restore`/`Fire`/`Update`), the Fire pipeline, and ingress mechanics.
- [`runtime/result-types.md`](runtime/result-types.md) — the typed refusal surface: `InvalidArgs`, `ConstraintsFailed`, `Rejected`, `Unmatched`, `FieldNotEditable`, and their scopes.

Read this document to *classify*; read those to *build*.
