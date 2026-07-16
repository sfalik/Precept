# The Hybrid Model — Prove-or-Reject and Governed

> **Status:** Canonical design — implemented where the pipeline has landed; MVP-designed where noted
> **Audience:** compiler, runtime, language-server, MCP, and documentation authors; precept authors reasoning about where a rule is enforced

**How to read this document.** Sections 1–2 place the Hybrid model in one picture and ground it in the philosophy. Sections 3–4 give the two dispositions — prove-or-reject and governed — full and equal treatment. Section 5 is the decision model: a step-by-step procedure for classifying any constraint or fault-prone operation by provenance. Section 6 is the heart of the document: seven worked examples in real `.precept` syntax, each walking through what is proven, what is governed, at which enforcement point each check fires, and why. Section 7 names the one disposition that does not exist — governing an unprovable computed value at runtime — and teaches its silhouette so it is recognized rather than reinvented. Section 8 maps this document's relationship to the rest of the canon.

## Contents

- [1. What this document is — the Hybrid model in one picture](#1-what-this-document-is--the-hybrid-model-in-one-picture)
- [2. Why Hybrid — the philosophy grounding](#2-why-hybrid--the-philosophy-grounding)
- [3. Prove-or-reject — the compile-time disposition](#3-prove-or-reject--the-compile-time-disposition)
- [4. Governed — the runtime disposition](#4-governed--the-runtime-disposition)
- [5. The decision model](#5-the-decision-model)
- [6. Worked examples](#6-worked-examples)
- [7. The banned third arm](#7-the-banned-third-arm)
- [8. Relationship to other canonical docs](#8-relationship-to-other-canonical-docs)

---

## 1. What this document is — the Hybrid model in one picture

Precept delivers one guarantee: **no invalid configuration can be produced.** The guarantee is a single closed net woven from two dispositions — two ways a declared truth is made to hold — and the Hybrid model is the name for how those two dispositions divide the work:

- **Prove-or-reject** — the compile-time disposition. Every fault-prone operation over a value **derived inside the definition** carries a proof obligation. The proof engine discharges it before any entity exists, or the definition is rejected. There is no deferral and no runtime fallback.
- **Governed** — the runtime disposition. Every value **supplied from outside the definition** is enforced against its declared constraints at runtime, on every operation, at two enforcement points: per-value **ingress** as the value enters, and the **post-mutation sweep** over the completed working copy before anything commits. There is no unchecked path.

These are not a strong mechanism and a weak one, nor a primary mechanism and a fallback. They are **two regions of one net**, and the split between them is decided by exactly one axis: **provenance** — where the value comes from. A value computed by a Precept expression lives in the proven region. A value supplied by the outside world lives in the governed region. Nothing else routes: not the difficulty of the proof, not the syntactic spelling of the constraint, not the position of the declaration in the file. [`compiler-and-runtime-design.md`](compiler-and-runtime-design.md) §1 establishes this one-net/two-regions framing and §1.1–§1.2 give the pipeline mechanism; this document is the full treatment of the model itself — the two dispositions at equal depth, and the decision procedure that classifies any given site into one of them.

The two regions meet at a seam, and the seam is where the model earns its name. When a compile-time proof depends on an externally-supplied operand, the proof does not inspect the value — it rests on the structural fact that the operand *carries* a declared constraint, and runtime governance is what makes that carried constraint true of the actual value ([`philosophy.md`](philosophy.md) § What makes it different; spec [§0.7](language/precept-language-spec.md)). Proof and governance are two ends of one chain. Neither is complete without the other, and neither is a second line of defense for the other.

One sentence to carry away: **the compiler proves what the definition computes; the runtime governs what the world supplies; provenance — and only provenance — decides which is which.**

---

## 2. Why Hybrid — the philosophy grounding

The Hybrid model is not an engineering compromise between static and dynamic checking. It is the mechanical delivery of commitments [`philosophy.md`](philosophy.md) states as identity, and each commitment forces a piece of the model's shape.

**Prevention, not detection.** "No operation can produce a result that violates a declared rule — the invalid configuration is not reachable" ([`philosophy.md`](philosophy.md) § Prevention, not detection). Prevention has two natural homes, because bad configurations have exactly two origins. A bad value the *definition itself* would compute is preventable before any entity exists — so it is prevented there, by proof, and a definition that would compute it is rejected. A bad value the *world* supplies cannot be known before it arrives — so it is prevented at the moment it arrives, by governance that refuses it before it can join a committed configuration. Both are prevention. Detection — noticing an invalid configuration after it exists — appears nowhere in either disposition, because in neither does the invalid configuration ever exist: the rejected definition never builds an engine, and the refused mutation never commits.

**Governance, not validation.** Validation runs when called; if no path calls it, the rules do not apply. Precept's runtime disposition is not validation with better coverage — it is structural governance: every declared constraint is bound to the entity and enforced on every operation, with **no code path that bypasses the contract** ([`philosophy.md`](philosophy.md) § The hierarchy of concepts). The governed region of the net is exactly as absolute as the proven region. What differs is *when* the truth is established, not *whether*.

**The composition seam.** The philosophy states it precisely: "The compiler proves a structural fact — that the value carries its constraint — never the value itself; the runtime enforcement is what makes that carried constraint true, not a second line of defense" ([`philosophy.md`](philosophy.md) § Prevention, not detection). This single sentence is the load-bearing joint of the Hybrid model. It rules out two misreadings at once: that the compiler could somehow know runtime values (it cannot, and does not pretend to), and that runtime governance is a safety net under the proofs (it is not — it is the discharge of a precondition the proof explicitly rests on). The two dispositions compose into one guarantee because each does exactly the half the other cannot.

**Determinism and inspectability.** Same definition, same data, same outcome; nothing hidden ([`philosophy.md`](philosophy.md) § What the product does). Both dispositions honor this. A proof is a legible, re-checkable certificate, not an opaque solver verdict (spec [§0.6](language/precept-language-spec.md), Proof philosophy #3). A governed refusal is a structured outcome naming the violated constraints and their declared reasons ([`runtime/result-types.md`](runtime/result-types.md)). In neither region does the author face an unexplainable verdict.

**One file, complete rules.** Every field, rule, ensure, and transition lives in the `.precept` definition — and therefore every fact available to the prover and every constraint available to the governor comes from that one file. The proof engine's knowledge boundary is the file boundary (spec §0.6, Proof philosophy #4); the runtime's enforcement surface is the declared contract and nothing else. The Hybrid model does not import truth from anywhere the author cannot read.

**States as coordinate system, stateless precepts first-class.** The model is indifferent to lifecycle. A stateless precept's editable fields are governed at ingress and its rules swept post-mutation exactly as a stateful precept's are; a stateless precept's computed fields carry the same proof obligations. Lifecycle position scopes *which* constraints apply where — it never changes *which disposition* a constraint or operation falls into. Provenance is orthogonal to state.

The deepest philosophical point is the one the category line rests on: "not what bugs you try to catch, but what bugs cannot exist" ([`philosophy.md`](philosophy.md) § Prevention, not detection). A tool that only proved would have to reject every definition touching external data — which is all of them. A tool that only governed would surrender the compile-time certainty that is Precept's headline differentiator, and would have to pretend a runtime refusal of a *computed* value is meaningful (it is not — §7). The Hybrid model is the unique shape under which every declared rule is guaranteed absolutely, external data remains fully expressible, and every enforcement act is addressed to a party who can respond to it. It is not one option among several. Given the philosophy, it is the only shape that closes.

---

## 3. Prove-or-reject — the compile-time disposition

Prove-or-reject is the disposition for every fault-prone operation over a **definition-derived value** — a value produced by a Precept expression: the right-hand side of a `set` action, a computed field's `<-` expression, an intermediate result inside either. The mechanism is specified end to end in [`compiler-and-runtime-design.md`](compiler-and-runtime-design.md) §1.2 and its measurable boundary in [`compiler/soundness-and-coverage.md`](compiler/soundness-and-coverage.md) §3; this section states the disposition's shape, its enforced fault set, and the two facts about decidability the decision model depends on.

### The enforced fault set

A fault-prone site is any place a computed value could fault or land outside a declared limit. The enforced set:

- **Division by zero** — every divisor must be provably non-zero.
- **`sqrt` / `pow` non-negativity** — every `sqrt` operand and `pow(integer, integer)` exponent must be provably non-negative (spec [§0.6](language/precept-language-spec.md), responsibility #4).
- **Empty-collection and index access** — every accessor with a non-empty or in-bounds precondition must have it provably established.
- **Declared-bound containment** (`OutOfRange`) — every computed result flowing into a field with declared `min`/`max` bounds (or their modifier equivalents like `nonnegative`, `positive`) must provably land inside them.
- **Cardinality and length containment** (`CountBoundViolation` / `LengthBoundViolation`) — every derived count or length flowing against a `mincount`/`maxcount`/`minlength`/`maxlength` bound must provably stay inside it.

The set is closed at the `FaultCode` registry and dispositioned cell by cell in [`compiler/soundness-and-coverage.md`](compiler/soundness-and-coverage.md) §3.1–§3.2 — that map, not this list, is the authoritative boundary of what is and is not an obligation.

### Obligation, discharge, rejection

At every such site the type checker stamps a proof obligation — uniformly, from catalog metadata, at every site, with no exception for sites that look hard ([`compiler-and-runtime-design.md`](compiler-and-runtime-design.md) §1.2). The proof engine then discharges each obligation by establishing that the operand **carries a sufficient constraint**: a declared modifier or rule narrowed into its interval, a `when`-guard fact live on the path, or a statically-known safe value. Discharge establishes a structural fact about what the operand carries — never the concrete runtime value.

If the engine cannot discharge an obligation, it **rejects the definition** — and the rejection names what would make the operation provably safe: the weakest precondition, printed verbatim, or a concrete witness configuration that violates the bound (spec [§0.6](language/precept-language-spec.md), Proof philosophy #5). Both non-proven verdicts reject. **There is no deferral, and there is no runtime fallback for a computed value.** The compiler never accepts a definition and leaves a fault-prone computed site to a runtime check it did not prove safe — that stance is excluded on principle, and §7 gives the full argument for why.

Every discharge emits a legible, independently re-checkable certificate drawn from a spec-enumerated vocabulary. The certificate criterion — legible, performant, right-sized, justified — is stated in spec [§0.6](language/precept-language-spec.md) (Proof philosophy #3) and its admissibility gate in [`compiler/soundness-and-coverage.md`](compiler/soundness-and-coverage.md) §3.3 and [`compiler/proof-engine.md`](compiler/proof-engine.md); this document does not restate it.

### Decidability — the oracle, and what it shapes

Two precise facts about decidability, because both are load-bearing in the decision model (§5):

**Decidability is the discharge-vs-reject oracle, not the router.** Provenance decides that a computed value's fault-prone operation is a prove-or-reject obligation — full stop. Decidability then decides, *within* that routed obligation, whether the discharge succeeds (the carried constraints yield a provable interval that contains the result) or the definition is rejected (they do not). Decidability never moves an obligation to the governed disposition. An obligation that cannot be discharged does not become a runtime check; it becomes a rejection.

**Decidability shapes which obligations exist, not just how they discharge.** A containment obligation exists only where there is something decidable to contain against. If a field's only bound is an undecidable constraint — say, a nonlinear rule from which the engine's interval reasoning extracts no interval — then a computed write into that field generates **no containment obligation on that axis**: there is no decidable bound for the write to be proven inside, so there is nothing to discharge and nothing to reject. The nonlinear rule itself is still fully enforced — as a governed constraint, at the post-mutation sweep (§4) — but it contributes no compile-time containment obligation. A reader who expects prove-or-reject to fire wherever a computed value meets *any* constraint has the model slightly wrong: prove-or-reject fires where a computed value meets a *decidable* bound. The declared-bound modifiers (`min`, `max`, `nonnegative`, `positive`, count and length bounds) are decidable by construction; that is why the enforced fault set above is stated in their terms.

### What prove-or-reject buys

A definition that compiles has **no unproven fault-prone computed sites**. Before a single entity exists, every division is known non-zero, every `sqrt` operand known non-negative, every computed result known inside its declared bounds — known by proof, attributed to the constraints that proved it, and re-checkable by tooling and agents. The proven region of the net needs no guards at runtime because it was proven not to need them. That is the compile-time half of "what bugs cannot exist."

---

## 4. Governed — the runtime disposition

Governed is the disposition for every value that enters the entity **from outside the definition** — event arguments, construction inputs, direct edits of `editable` fields — and for every declared constraint insofar as it speaks about such values. Governance is enforcement of declared constraints at runtime on the **contract path**: the mutations entering or produced by the declared operations `Create`, `Fire`, and `Update` ([`runtime/runtime-api.md`](runtime/runtime-api.md)). It is not a lighter guarantee than proof, and it is not a fallback for proofs that failed. It is the disposition external data *must* have — no compiler can prove a value the world has not supplied yet — and Precept makes it exactly as absolute as the proven region: structural, operation-blind, with no unchecked path.

Governance has **two enforcement points**, and the model is not understood until both are, because they answer two different questions about the same external provenance.

### 4.1 Ingress — per-value governance at the door

Every externally-supplied value is checked against **its own declared constraint** at the moment it enters — before any computation derives from it, before it touches the working copy's dependent values. An event argument declared `positive` is refused at ingress if it is not positive. A construction input declared `notempty maxlength 50` is refused if it is empty or too long. A direct edit into a field declared `nonnegative max 100` is refused if it lands outside `[0, 100]`.

Three properties define ingress precisely:

- **It is per-value.** Ingress checks each entering value against the constraints declared *on that value's own field or argument*. It answers the question "is this value, by itself, what its contract says it must be?"
- **It is operation-blind.** Ingress upholds the field's declared contract whether or not any fault-prone operation anywhere in the definition references the field (spec [§0.7](language/precept-language-spec.md)). A `positive` field is kept positive even if nothing divides by it. The contract is the field's, not the operation's.
- **It is the composition seam in action.** When a compile-time proof rests on an externally-supplied operand — "this division is safe because the divisor carries `positive`" — ingress is the mechanism that makes the carried constraint *true of the actual value*. The compiler proved the structural fact; ingress discharges its precondition on every real value, every time. The ingress check is not a second line of defense behind the proof — it is the other end of the same chain ([`philosophy.md`](philosophy.md) § Prevention, not detection; [`compiler-and-runtime-design.md`](compiler-and-runtime-design.md) §1.1 Composition).

Mechanically, ingress runs in the typed ingress layer (`TypeRuntime` / `TypeRuntimeMeta`) on both ingress lanes — JSON and typed — of every mutable operation ([`runtime/runtime-api.md`](runtime/runtime-api.md) § Two-Lane Ingress Principle). No lane skips it; no caller shape is exempt.

### 4.2 The post-mutation sweep — whole-configuration governance before commit

Ingress answers "is each entering value individually within its own contract?" It structurally *cannot* answer a second question: "do the values, in combination, satisfy the constraints that relate them?" A rule like `Balance >= MinBalance` over two independently-editable fields is invisible to ingress by construction — each value, arriving alone, can be perfectly within its own per-field contract while the *pair* violates the relationship. Per-value, operation-blind checking has no vantage point from which a multi-field relationship is even expressible.

The **post-mutation sweep** is that vantage point. After **all** mutations of an operation complete — every `set`, every collection action, every entry/exit action — the engine evaluates **every applicable constraint** against the completed working copy: global rules (unconditional and guarded), state ensures (`in`/`to`/`from`), and event ensures. If every constraint passes, the working copy is promoted to become the entity's committed state. If **any** fails, the working copy is discarded whole and the entity is unchanged. The canonical statement is spec [§3A.4](language/precept-language-spec.md) (Mutation Atomicity); the outcome taxonomy is [`runtime/result-types.md`](runtime/result-types.md), whose `ConstraintsFailed` variant covers exactly this sweep — "post-mutation constraints violated (rules, state ensures, event ensures)."

The sweep is not a separate regime from ingress. It is the **multi-field completion of the same external-provenance carve-out**: ingress governs each external value as it enters; the sweep governs what the external values *jointly became*. Ingress governs *what enters*; the sweep governs *the result* (spec §3A.4). One provenance, two granularities — the value and the configuration. And the sweep is the **only** place a cross-field relational invariant over independently-set fields can be enforced, because only the completed working copy holds the combination the invariant speaks about. This is why relational rules over external fields are never a compile-time question and never an ingress question: they are the sweep's native subject matter.

### 4.3 Working-copy atomicity — the shape of the guarantee

The sweep's guarantee has a precise atomicity shape (spec [§3A.4](language/precept-language-spec.md)):

- **All mutations execute on a working copy.** The committed entity is untouched while an operation runs.
- **Constraints are evaluated after all mutations complete** — against the finished configuration, never a half-built one.
- **Promotion is all-or-nothing.** Every constraint passes → the working copy becomes the committed state. Any constraint fails → the working copy is discarded whole.
- **An invalid configuration never exists, even transiently.** There is no window between mutation and constraint checking in which a partially-committed state with violated rules can be observed.

This applies **uniformly to every mutation surface**: event-driven transitions, stateless event hooks, direct field updates, and state entry/exit actions. Every path through the engine that can modify entity data uses the same working-copy, all-or-nothing model. There is no fast path that commits without the sweep, no bulk path that batches around it, no internal path exempt from it. That uniformity is what makes governance *structural* — the property philosophy calls "no code path that bypasses the contract" — rather than a convention that holds when everyone remembers to call the validator.

### 4.4 The boundary of governance — the contract path, and only the contract path

Governance covers every value entering through the contract: construction, events, edits. Two categories of data sit outside that envelope, and the model is honest about both (spec [§0.7](language/precept-language-spec.md), The boundary of the guarantee):

- **Restored state is trusted, not re-governed on load.** `Restore` reconstitutes an entity from persisted data the way a database read trusts the rows it loads: the prior committed state satisfied the rules in effect when it was written, so hydration is fast and does not re-validate. Restored state is re-governed by the **next operation's sweep** — the first `Fire` or `Update` through the contract evaluates every constraint against the resulting working copy, so any drift is caught at the first governed touch, not silently perpetuated ([`runtime/runtime-api.md`](runtime/runtime-api.md) § Restoration).
- **Host-injected, out-of-contract data is outside the envelope** — values that bypass the engine entirely are not values the engine claimed to govern. For these, the `[StaticallyPreventable]` evaluator fault traps exist as defense-in-depth ([`runtime/fault-system.md`](runtime/fault-system.md)): every runtime fault code carries a compiler-enforced link to the diagnostic that statically prevents it, and for data that entered through the contract the traps are **unreachable**. A trap firing on contract data is a compiler defect to be fixed — never an accommodated path, never a working part of the guarantee.

Stating the boundary is part of the guarantee's honesty: governance is absolute *within* the contract path, and the contract path is exactly the surface the definition declares.

### 4.5 How governed refusals surface

A governed refusal is never an exception and never a silent no-op — it is a structured outcome the caller pattern-matches ([`runtime/result-types.md`](runtime/result-types.md)):

- **`ConstraintsFailed`** — the post-mutation sweep found the completed working copy violating one or more constraints. Carries the full set of `ConstraintViolation`s: which rules, ensures, and bounds failed, each with its declared `because` rationale. This is the sweep's voice.
- **`InvalidArgs`** (on `Fire`) / **`InvalidFields`** (on `Update`) — the entering values failed at the door: wrong type, unknown key, structurally invalid patch, a value outside its declared per-field contract. This is ingress's voice.
- **`FieldNotEditable`** — the field's declared access mode forbids direct editing in the current lifecycle position; the refusal fires before any value is even considered. Editability is itself a declared, governed contract — *whether a value may enter at all* is governed before *what the value is*.

Each refusal is addressed to the party who can act on it: the caller who supplied the bad argument, the editor who attempted the out-of-contract edit, the operator whose mutation would have broken a relational invariant. This addressability is not cosmetic — it is the property that makes runtime refusal *governance* rather than failure, and its absence for computed values is precisely why no computed value is ever governed (§7).

### 4.6 Outcome versus epistemics — the same guarantee, known differently

State this precisely, because collapsing it loses the model.

**The outcome guarantee is identical across the two dispositions.** On the proven path and on the governed path alike, no invalid configuration ever commits. A `nonnegative` bound on a computed field and a `nonnegative` bound on an editable field are equally absolute: no committed configuration violates either, ever. The governed region of the net has no holes the proven region lacks.

**The epistemic guarantee differs.** A proven site is **known safe before any entity exists** — the author holds a compile-time certificate that the fault cannot occur, for all possible executions, unconditionally. A governed site is **made safe at runtime, on every operation** — the author holds a structural promise that any violating mutation will be refused, but which particular mutations will be refused depends on what the world supplies. The proven path tells you *this can never go wrong*; the governed path tells you *this will never be allowed to go wrong*. Both are prevention. They are not the same knowledge, and Precept does not present one as the other — the same honesty commitment that forbids presenting approximation as exactness ([`philosophy.md`](philosophy.md) § What the product does) forbids presenting runtime governance as compile-time proof, or vice versa.

And the converse discipline: **there is no governed disposition without real enforcement machinery behind it.** "Governed" in this document always means the ingress layer and the post-mutation sweep actually enforcing declared constraints on the contract path — never a euphemism for "unchecked," "trusted," or "the runtime will probably notice." A constraint classified as governed is a constraint the runtime structurally enforces. If no machinery enforces it, it is not governed; it is a gap, and gaps are disclosed as gaps.

---

## 5. The decision model

This section is the procedure. Given any constraint or any fault-prone operation in a definition, it classifies the disposition — which region of the net enforces it, and at which enforcement point. The spine of the procedure is one question asked of one thing: **where does the value come from?**

### 5.1 The two obligation families

Before the procedure, the frame it runs in. There are exactly two families of obligation, and they attach to different kinds of thing:

**Every declared constraint always generates both of two roles.** A declared constraint — a field modifier or a `rule`, indistinguishably; `max 100` and `rule X <= 100` are the same construct in different spellings — always does both of the following, simultaneously:

- **Governance role** — it is enforced at runtime on external values: at ingress for the per-value contract it declares, and at the post-mutation sweep for the whole-configuration truth it asserts.
- **Premise role** — it contributes its provable fact to the proof engine, to the extent it is decidable: `nonnegative` contributes the interval `[0, +inf)`; `rule Deposits >= Withdrawals` contributes the relational fact `Deposits − Withdrawals >= 0`.

A constraint is **never routed to one role or the other** — it always carries both. Crucially, the two roles often act on **different targets**: a single rule can *prove* a derived value (premise role, at compile time) while *governing* its own external fields (governance role, at the sweep). Example A in §6 is the canonical demonstration.

**Every fault-prone operation over a definition-derived value carries exactly one obligation**: a compile-time prove-or-reject obligation, routed by the origin and shape of the value it consumes — never by how hard the obligation is to prove.

### 5.2 The procedure

Run these steps in order.

**Step 1 — Identify the thing.** Is it a fault-prone **operation** over a value (a division, a `sqrt`, a collection access, a computed write into a bounded field)? Or a declared **constraint** (a modifier, a `rule`, an `ensure`)? A constraint always does both roles of §5.1 — you are done classifying *it*; what remains is locating its governance enforcement point (Step 2, third arm) and noting which proofs its premise feeds. An operation gets exactly one obligation — continue.

**Step 2 — Ask provenance of the value the operation consumes** (or, for a constraint's governance role, provenance of the fields it speaks about):

- **Derived by a Precept expression** — the RHS of `set X = …`, a computed field's `<-` expression, an intermediate result inside either → **prove-or-reject, at compile time.** The obligation discharges from carried constraints (declared modifiers/rules on the operands, `when`-guard facts on the path, statically-known values), or the definition is **rejected**, naming the carrier that would discharge it. Never governed as a fallback.
- **Supplied from outside** — an event argument, a construction input, an edit of an `editable` field → **governed at ingress**, against its own declared constraint, at the moment it enters.
- **A declared relational invariant over independently-set (external) fields** — a rule relating two or more fields whose values originate externally → **governed by the post-mutation sweep**, against the completed working copy, on every operation.

**Step 3 — Apply the guardrails.** These are the tie-breakers that keep the procedure honest under pressure:

1. **Spelling and position are irrelevant.** `max 100` on the field and `rule X <= 100` in the rules block are the same constraint with the same two roles. A `default 150` under a `max 100` bound and a `set X = 150` into the same field get the same disposition — both are definition-derived values meeting a decidable bound, both are prove-or-reject, and both are rejected as proven-violating. **Same provenance ⇒ same disposition**, however the author spelled or placed it.
2. **Decidability is the discharge-vs-reject oracle for a routed prove-or-reject obligation.** It is not the router, and it never routes anything to governance. An undecidable bound generates no containment obligation (nothing decidable to contain against — §3); an undischargeable obligation rejects. Neither outcome is "governed instead."
3. **A derived value is never governed as a fallback.** If its obligation cannot be discharged, the definition is rejected. Accepting the definition and "governing" the computed value at runtime is the banned third arm (§7) — it does not exist.
4. **"Independently-set" means the value originates externally** — edited, supplied as an argument, or accumulated from external supply — *not* computed from the other fields in the relationship. The `set X = e` versus `rule X == e` distinction is a provenance distinction, not a spelling one: in `set X = e`, X is **computed** — it is a derived value, and the write carries a fault obligation against X's decidable bounds. In `rule X == e`, X is **independently-set** — the rule produces no value and derives nothing; it merely constrains a relationship among independent fields, so there is no fault obligation, and the rule's governance role is enforced at the sweep.

### 5.3 The decision table

| You are classifying | Provenance | Disposition | Enforcement point | On failure |
|---|---|---|---|---|
| A fault-prone operation whose value is **derived by a Precept expression** (`set` RHS, computed `<-`, intermediate result) | Computed inside the definition | **Prove-or-reject** | Compile time — obligation discharged from carried constraints, or definition rejected | Rejection naming the discharging carrier |
| A **value supplied from outside** (event argument, construction input, editable-field edit) | External | **Governed — ingress** | Runtime, at entry, against the value's own declared constraint, before any computation derives from it | `InvalidArgs` / `InvalidFields`; `FieldNotEditable` before the value is considered |
| A **declared relational invariant over independently-set fields** | External (a relationship among externally-originated values) | **Governed — post-mutation sweep** | Runtime, after all mutations, against the completed working copy | `ConstraintsFailed`; working copy discarded whole |
| A **declared constraint** as such (modifier or `rule`, any spelling) | — | **Both roles, always**: governance (rows above, per its targets' provenance) + premise (feeds the prover to the extent decidable) | Both | — |

The table has three enforcement rows and no fourth. There is no row for "computed value, enforced at runtime" — see §7.

---

## 6. Worked examples

Seven examples, each in real `.precept` syntax, each naming the provenance of every field explicitly and walking every obligation to its enforcement point. Example A is the centerpiece: one rule playing both roles at once.

### A. Flagship — one rule, two roles: the derived value proven by a constraint that is itself governed

```precept
precept SettlementAccount

# Running totals fed from outside: each event carries an externally-supplied
# amount that accumulates into its total.
field Deposits as money in 'USD' default '0.00 USD' nonnegative
field Withdrawals as money in 'USD' default '0.00 USD' nonnegative

# The derived value: Balance is computed from the totals on settlement.
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
    -> set Balance = Deposits - Withdrawals   # provably >= 0 under the rule
```

**Provenance, field by field.** `Deposits` and `Withdrawals` are **independently-set, externally-sourced** — running totals accumulated from event arguments supplied by the outside world. `Balance` is **derived** — produced by `set Balance = Deposits - Withdrawals`, a Precept expression. The load-bearing fact: Balance is computed *from* the totals; the totals are not computed from Balance; the totals' values originate externally.

**Two obligations resolve differently — and one construct serves both.**

- **Obligation A — the computed write.** `set Balance = Deposits - Withdrawals` flows a derived value into a field carrying the decidable bound `nonnegative` = `[0, +inf)`, so the type checker stamps a compile-time **prove-or-reject** containment obligation: prove `Deposits - Withdrawals` lands in `[0, +inf)`. Without the rule, the operands are individually `nonnegative` but their difference spans `(-inf, +inf)` — undischargeable, and the definition is **rejected** (Example G walks that rejection). *With* the rule, the prover holds the premise `Deposits >= Withdrawals`, from which `Deposits - Withdrawals >= 0` follows directly — the obligation **discharges at compile time**, the definition compiles cleanly, and Balance is **proven** non-negative for every execution, before any entity exists.
- **Obligation B — the rule itself.** `rule Deposits >= Withdrawals` is a declared constraint over independently-set fields, so per §5.1 it does **both** things at once. **Premise role:** it feeds the prover the relational fact that discharges Obligation A. **Governance role:** it is enforced at runtime — and because it is a *cross-field relationship*, its enforcement point is the **post-mutation sweep**, not per-value ingress. A `Withdraw` whose amount would push `Withdrawals` above `Deposits` passes ingress (the amount is `positive`, within its own contract) and executes its mutation on the working copy — but the completed working copy violates the rule, the sweep discards it whole, and the operation returns `ConstraintsFailed` carrying the rule and its `because`. The entity is unchanged; the invalid configuration never existed.

**The clean division.** The one rule **proves the derived value** (Balance, at compile time, via its premise role) and **governs the external ones** (the Deposits/Withdrawals relationship, at the sweep, via its governance role). Balance itself is **never governed** — no runtime machinery ever checks Balance's non-negativity on the contract path, because the compiler proved no execution can violate it. And the direction of dependence matters: the *governance* of the rule is not what makes Balance safe — the *premise* is. The proof is complete at compile time; the sweep's enforcement of the rule is what keeps the premise true of the running totals, which is the composition seam doing its job. Conflating "the rule is governed" with "so Balance is governed" is precisely the error the Hybrid model exists to prevent.

**No third arm anywhere.** The computed value never touches a runtime governance path. The only runtime governance in this precept is over the external inputs (`positive` at ingress on each amount) and the external relationship (the rule, at the sweep).

### B. Pure ingress — an event argument governed at the door

```precept
precept PlanPricing

field PlanMonthlyFee as money in 'USD' default '0.00 USD' nonnegative
field ConversionStatus as choice of string("NotAttempted", "Converted") default "NotAttempted"

event AcceptOffer(MonthlyFee as money in 'USD' positive)

on AcceptOffer
    -> set ConversionStatus = "Converted"
    -> set PlanMonthlyFee = AcceptOffer.MonthlyFee
```

**Provenance.** `AcceptOffer.MonthlyFee` is **externally supplied** — an event argument the caller provides. `PlanMonthlyFee` receives it by direct assignment; its content originates outside the definition. (This is the shape of [`../samples/saas-trial-to-paid.precept`](../samples/saas-trial-to-paid.precept), condensed.)

**What is governed, and where.** The argument declares `positive`. At the moment a `Fire("AcceptOffer", …)` arrives, **ingress** checks the supplied value against that declaration — before any mutation runs, before anything derives from it. A caller supplying `'0.00 USD'` or a negative amount is refused at the door with `InvalidArgs`; the working copy is never even built. This is per-value, operation-blind governance: `MonthlyFee` is kept positive whether or not anything in the definition divides by it, compares it, or computes from it. The field's contract is upheld because it is declared, not because some operation happens to need it.

**What is proven.** Nothing needs to be. The assignment `set PlanMonthlyFee = AcceptOffer.MonthlyFee` flows a value carrying `positive` = `(0, +inf)` into a field bounded `nonnegative` = `[0, +inf)` — the carried constraint is strictly inside the target bound, so the containment obligation discharges trivially from the carrier. The proof rests on the argument *carrying* `positive`; ingress is what makes `positive` true of every actual value. One seam, both ends visible in four lines.

### C. Pure relational governance — the sweep refusing a mutation

```precept
precept MarginAccount

# Both fields are editable — each value originates from the host's
# account-management surface, independently of the other.
field Balance as money in 'USD' default '0.00 USD' editable
field MinBalance as money in 'USD' default '0.00 USD' nonnegative editable

rule Balance >= MinBalance because "The account balance cannot sit below the declared minimum — the margin floor is a hard contractual term"
```

**Provenance.** `Balance` and `MinBalance` are both **independently-set** — `editable` fields whose values originate externally via `Update`. Neither is computed from the other; the rule relates two values the outside world supplies separately.

**Why this is un-provable — and why that is not a defect.** There is no derived value here. No Precept expression computes anything; therefore no fault-prone operation exists, and no proof obligation is ever stamped. Prove-or-reject has no subject matter in this precept. The rule is *expressible, and never refused*: the compiler accepts it without needing to prove anything about it, because there is nothing about future external values a compiler could prove.

**Why this is un-ingress-checkable.** Ingress is per-value. An `Update` raising `MinBalance` to `'500.00 USD'` supplies a value that is impeccable *by itself* — a `nonnegative` money amount, fully inside its own per-field contract. Ingress passes it, correctly: nothing about the value alone is wrong. The violation exists only *in combination* with the current `Balance` of, say, `'200.00 USD'` — and a per-value, operation-blind check structurally cannot see a combination.

**Where it is enforced — a refusal, step by step.** The `Update` applies `MinBalance = '500.00 USD'` to the working copy. All mutations complete. The **post-mutation sweep** evaluates every constraint against the completed working copy and finds `Balance >= MinBalance` false (`200.00 < 500.00`). The working copy is **discarded whole**; the entity still holds `MinBalance = '0.00 USD'`, `Balance = '200.00 USD'`; the caller receives `UpdateOutcome.ConstraintsFailed` carrying the violated rule and its `because` text. The invalid configuration — the pair, not either value — never existed, even transiently (spec [§3A.4](language/precept-language-spec.md)). Exactly the same refusal fires from the other side: an edit dropping `Balance` below the current floor builds a working copy the same rule condemns. The sweep is symmetric over the relationship, because the relationship — not either field — is its subject.

This is the governed disposition in its purest form: a constraint that no compiler could prove, that no per-value check could catch, enforced absolutely — on every operation, with no bypass — at the only enforcement point that can see it.

### D. The relational edge — a nonlinear invariant the prover cannot read, and does not need to

```precept
precept PanelSpec

# Both dimensions are editable — supplied independently by the host's editor.
field PanelWidthCm as decimal default 10 positive editable
field PanelHeightCm as decimal default 10 positive editable
field MaxPanelAreaCm2 as decimal default 5000 positive

rule PanelWidthCm * PanelHeightCm <= MaxPanelAreaCm2 because "Panel area is capped by the fabrication bed — width times height cannot exceed the bed area"
```

**Provenance.** `PanelWidthCm` and `PanelHeightCm` are **independently-set** editable fields. The rule is a **relational invariant over external fields** — and a nonlinear one: a product of two variables, precisely the shape from which interval reasoning extracts no useful decidable premise for proof purposes.

**The classification, run honestly.** Step 1: this is a declared constraint — both roles, always. Step 2: its fields are external, and the constraint is cross-field, so its governance role enforces at the **post-mutation sweep**. Its premise role contributes whatever is decidable — here, essentially nothing the prover can use. And that is **fine**, because of guardrail 2: decidability is the oracle for *routed prove-or-reject obligations*, and there is no such obligation here. No value is computed; nothing was ever routed to the prover; there is nothing to discharge and therefore nothing to reject.

**"The engine can't prove it" is not a reason to reject a relational invariant over external fields — it was never a proof obligation.** The nonlinearity costs the author nothing: an edit pushing `PanelWidthCm` to `80` while `PanelHeightCm` is `70` builds a working copy with area `5600`, the sweep evaluates the rule against the completed copy — runtime evaluation of a concrete configuration, which is trivially decidable no matter how nonlinear the *symbolic* form is — finds it false, discards the copy, and returns `ConstraintsFailed`. Governance is not a downgraded proof; it is a different question ("does *this* configuration satisfy the rule?") that never inherits proof's decidability limits. This is not a gap, not a disclosed hole, not a weakened case. It is the governed disposition covering exactly what it exists to cover.

One consequence worth knowing (per §3, decidability shapes which obligations *exist*): if some other row computed a write into a field bounded *only* by a rule this nonlinear, that write would generate no containment obligation on that axis — there is no decidable bound to contain against — so it is not rejected on that axis either. The decidable modifiers (`min`/`max`/`nonnegative`/…) are what put a computed write under proof; the nonlinear rule remains the sweep's business.

### E. Prove-or-reject with a governed carrier — division whose safety rests on ingress

```precept
precept UnitCosting

field TotalCost as money in 'USD' default '0.00 USD' nonnegative editable
field Quantity as integer default 1 positive editable

# Derived: cost per unit. The division is a fault-prone site.
field UnitCost as money in 'USD' <- TotalCost / Quantity
```

**Provenance.** `TotalCost` and `Quantity` are **externally supplied** (editable). `UnitCost` is **derived** — a computed field; its `<-` expression is definition-derived by definition.

**The obligation and its discharge.** `TotalCost / Quantity` stamps a compile-time **prove-or-reject** obligation: the divisor must be provably non-zero. The proof engine discharges it from the carried constraint — `Quantity` declares `positive`, whose integer interval `[1, +inf)` excludes zero — and the certificate records the carrier. Had `Quantity` carried no constraint excluding zero (a bare `integer`, or merely `nonnegative`, whose interval `[0, +inf)` includes zero), the obligation would be undischargeable and the definition **rejected** — with the rejection naming what would make it provable: a `positive` or `nonzero` declaration, a rule, or a guard on the consuming path. Not a runtime divide-by-zero guard; a rejection.

**The seam, explicitly.** The compile-time proof rests on `Quantity` *carrying* `positive`. **Ingress** is what makes that true of every actual value: an `Update` supplying `Quantity = 0` is refused at the door (`InvalidFields`) — the value never reaches the working copy, so the division the compiler proved safe is never asked to divide by zero. The proof is complete at compile time; its precondition is discharged by governance on every operation; and the evaluator's divide-by-zero trap ([`runtime/fault-system.md`](runtime/fault-system.md)) remains as defense-in-depth for out-of-contract data only, unreachable on this path. Proven site, governed carrier — the Hybrid model in one field declaration. (The same shape at larger scale is [`../samples/invoice-line-item.precept`](../samples/invoice-line-item.precept), where the modifiers on the editable source fields are what make every computed total provably in-bounds.)

### F. Spelling invariance — same provenance, same disposition, however written

```precept
precept SpellingInvariance

# Spelling 1: bound as a modifier.
field ScoreA as integer default 0 nonnegative max 100 editable

# Spelling 2: the same bound as a rule.
field ScoreB as integer default 0 nonnegative editable
rule ScoreB <= 100 because "Scores are percentages — one hundred is the ceiling"

event Calibrate

on Calibrate
    -> set ScoreA = 150    # REJECTED at compile time: proven-violating
```

**The invariance, in both directions.**

- **As constraints:** `max 100` on `ScoreA` and `rule ScoreB <= 100` are the **same construct** — a declared constraint contributing the same decidable premise (`(-inf, 100]`) to the prover and the same governed contract to the runtime. An external edit of `ScoreA` to `150` is refused at **ingress** (the value violates its own per-field contract); an external edit of `ScoreB` to `150` is refused by the **sweep** (the rule is evaluated against the completed working copy) — different enforcement points as a mechanical matter of where a single-field modifier versus a rule is checked, but the same disposition (governed, external provenance), the same absoluteness, and the same outcome: no configuration with a score above 100 ever commits.
- **As consumed premises:** a computed write into either field is a **prove-or-reject** obligation against the identical bound, and discharges — or rejects — identically. `set ScoreA = 150` is proven-violating against `[0, 100]` and the definition is **rejected** with a witness. Writing `default 150` under the `max 100` bound is the same rejection: a default is a definition-derived value like any other, and the compiler folds it against the declared constraints (spec [§0.6](language/precept-language-spec.md), responsibility #11). `default 150` and `set ScoreA = 150` get the same disposition because they have the same provenance — the author's own expression — not because they share a syntax.

**The rule to carry away** (guardrail 1 of §5.2): position in the file, modifier-versus-rule spelling, default-versus-action placement — none of it routes. **Provenance routes.** Two constructs with the same provenance always land in the same disposition, and a reader classifying a site can ignore its spelling entirely.

### G. The banned third arm — an unprovable computed value, and the three honest fixes

```precept
precept NaiveSettlement

field Deposits as money in 'USD' default '0.00 USD' nonnegative
field Withdrawals as money in 'USD' default '0.00 USD' nonnegative
field Balance as money in 'USD' default '0.00 USD' nonnegative

event Settle

# NAIVE — REJECTED under prove-or-reject.
on Settle
    -> set Balance = Deposits - Withdrawals
```

**Provenance.** As in Example A: `Deposits` and `Withdrawals` external, `Balance` derived. But this time there is **no supporting rule** — nothing relates the operands.

**The rejection.** The `set` stamps a containment obligation: prove `Deposits - Withdrawals` lands in `[0, +inf)`. The operands are individually `nonnegative`, but nothing bounds their *difference* — the derivable interval is `(-inf, +inf)`, the obligation is **unresolved**, and both non-proven verdicts reject (spec [§0.6](language/precept-language-spec.md), Proof philosophy #2, #5). The definition is refused with the weakest precondition printed verbatim — in substance: *establish `Deposits >= Withdrawals` on this path.* No entity is ever created.

**Why "just govern Balance at runtime" is excluded — walked, not asserted.** The tempting alternative: accept the definition, let `Settle` run, and have the runtime refuse the operation whenever the computed Balance comes out negative. This is the banned third arm, and its exclusion is principled:

- **The refusal is addressed to no one.** Every governed refusal in the model hands a recoverable, typed outcome to an external agent who supplied something and can supply something else — a caller who can correct an argument, an editor who can pick a different value. A computed value has **no external supplier**. Who receives the refusal of `Deposits - Withdrawals`? Not the `Settle` caller — `Settle` carries no arguments; the caller supplied nothing to correct. The definition itself produced the bad value, and the definition cannot re-decide at runtime. The "refusal" satisfies the discard-the-working-copy half of a governed outcome but fails the hand-back-an-actionable-outcome half — which makes it a **fault wearing a governed refusal's costume**, not governance.
- **The honest version would be new language surface, and it does not exist.** The only legitimate shape for governing a computed value would be an explicit, author-visible, opt-in construct — a token written in the source by which the author knowingly accepts a runtime refusal on that field's computation. No such construct is built, and none is designed. Absent it, the disposition for an unprovable computed value is **reject** — the compiler declines to manufacture, silently, a runtime behavior the language gives the author no way to see or choose.

**The three honest fixes**, each with its mechanics:

```precept
# Fix 1 — state the relationship as a rule.
rule Deposits >= Withdrawals because "An account cannot withdraw more than it has deposited"
on Settle
    -> set Balance = Deposits - Withdrawals   # now provably >= 0
```

The rule's **premise role** discharges the containment obligation at compile time (Balance is proven); its **governance role** enforces the relationship on the external totals at the sweep. This is Example A — the flagship resolution, and the one that most enriches the contract: the domain truth ("you cannot withdraw more than you deposited") is now declared, proven against, and governed.

```precept
# Fix 2 — guard the operation.
on Settle when Deposits >= Withdrawals
    -> set Balance = Deposits - Withdrawals   # provable inside the guard
```

The `when` guard establishes `Deposits >= Withdrawals` as a path fact; inside the guarded row the obligation discharges from it. Mechanically: a `Settle` fired while the fact does not hold matches no row and returns `Unmatched` ([`runtime/result-types.md`](runtime/result-types.md)) — the operation simply is not available in that configuration. The author has scoped the computation to the region where it is safe, rather than declaring the region universal.

```precept
# Fix 3 — author the failing case explicitly.
on Settle when Withdrawals > Deposits
    -> reject "Cannot settle: withdrawals ({Withdrawals}) exceed deposits ({Deposits})"
on Settle
    -> set Balance = Deposits - Withdrawals   # provable: the reject row excludes the bad case
```

The explicit `reject` row gives the previously-undispositioned case an **authored disposition** — a business refusal, in the author's words, returned as `EventOutcome.Rejected`. And its guard does double duty: the sibling row's exclusion narrows the remaining row's interval (`Withdrawals <= Deposits` holds wherever the second row runs), so the containment obligation discharges. The unresolved verdict existed because the definition left a reachable case with no authored disposition; Fix 3 is the author writing the missing disposition down.

All three fixes share one shape: **they move the missing truth into the definition** — as a declared rule, a guard fact, or an authored refusal — where the prover can read it and every future reader can see it. None of them asks the runtime to absorb, invisibly, a case the author never decided.

---

## 7. The banned third arm

The decision table (§5.3) has two dispositions and three enforcement rows. This section makes the absence explicit, because the missing arm has a recognizable shape and readers will meet arguments for it.

**There is no disposition of the form: "accept the definition, and govern an undecidable computed value at runtime."** It is excluded on principle, not by omission:

1. **A runtime refusal of a computed value is addressed to no one.** Governance is meaningful because every refusal hands a recoverable, typed outcome to an external agent who can act on it — resupply the argument, re-edit the field, choose a different operation. A computed value has no external supplier; the definition produced it, and the definition cannot respond to its own refusal. Such a "refusal" performs the mechanical half of a governed outcome (the working copy is discarded) while failing its essential half (an actionable outcome for a party who can act). It is a **fault in a governed refusal's costume** — the runtime discovering, per-entity and at the worst possible moment, what the compiler already knew per-definition and declined to say.
2. **The honest form would be an explicit language construct — and it is neither built nor designed.** A language *could* offer an author-visible, opt-in token by which the author accepts a runtime refusal on a specific computed field — making the deferral a declared, inspectable part of the contract rather than a silent compiler behavior. Precept has no such construct. Until and unless one goes through design, the disposition for an unprovable computed value is **reject** — with the rejection naming what would make it provable, which is the compiler pushing the missing truth back into the definition where it belongs.

**Know the silhouette.** Three things share a surface resemblance — "the runtime handles something compile time did not prove" — and only one of them is the banned arm:

- **Representational overflow** — a computed value exceeding its *type's* representable range — is today a **disclosed, temporary gap** in the prove-or-reject surface: the containment obligation against *declared* bounds is in force, while the representable-range enforcement lane is deferred by build order and targeted to become prove-or-reject ([`compiler/soundness-and-coverage.md`](compiler/soundness-and-coverage.md) §3.1, `NumericOverflow`). A disclosed hole scheduled to close is not a standing runtime-governance route. Do not mistake a gap for an arm.
- **Relational governance over external fields** (Examples C and D) is arm two of the model working as designed — the sweep governing **external** inputs in combination. It never governs a computed value; the fields in the relationship originate outside the definition. The presence of a rule the prover cannot decide changes nothing: it was never a proof obligation.
- **Governing an undecidable *computed* value** — the definition's own expression, unprovable, accepted anyway and refused per-entity at runtime — is the one shape that is forbidden. Its tell is the provenance of the refused value: if the value the runtime would refuse was produced by the definition itself, the refusal has no addressee, and the correct disposition was rejection at compile time.

One test disambiguates all three: **ask the provenance of the value the runtime would act on.** External → governance (working as designed). Computed, under a disclosed and scheduled gap → gap (track its closure). Computed, as a standing accommodation → the banned arm (the definition should have been rejected).

---

## 8. Relationship to other canonical docs

This document is the canonical treatment of the Hybrid model as a model — the two dispositions at equal depth, and the provenance decision procedure. It deepens, and deliberately does not duplicate, the following:

- [`philosophy.md`](philosophy.md) — the identity commitments this model delivers mechanically: prevention not detection, governance not validation, determinism, one-file completeness, the composition seam. This document's §2 grounds every structural choice in that text; where the two speak of the same commitment, the philosophy is the canonical statement.
- [`compiler-and-runtime-design.md`](compiler-and-runtime-design.md) §1, §1.1, §1.2 — the one-net/two-regions framing, the guarantee-contract mechanism (graph analyzer / proof engine / runtime governance), and the end-to-end prove-or-reject walk from fault-prone site to committed guarantee. That document owns the pipeline mechanism; this one owns the classification model that sits on top of it.
- [`language/precept-language-spec.md`](language/precept-language-spec.md) §0.6 — the proof engine design contract: responsibilities, proof philosophy, the three-way verdict, the certificate criterion. §0.7 — the compile-time and runtime guarantee contract: fault prevention, governance, composition, and the boundary of the guarantee, in author-facing terms. §3A.4 — the canonical definition of mutation atomicity and the post-mutation sweep. This document's §3 and §4 expand those sections into the full two-disposition treatment; the spec sections remain the normative statements.
- [`compiler/soundness-and-coverage.md`](compiler/soundness-and-coverage.md) — the measurable boundary: which fault modes and obligation-creation sites are in, out, or deferred; the certificate admissibility gate; the bucket test for new failing cases. Where this document says "the enforced fault set," that map is the cell-by-cell truth.
- [`compiler/proof-engine.md`](compiler/proof-engine.md) — the proof engine's architecture, strategies, and certificate machinery: how discharge actually happens.
- [`runtime/runtime-api.md`](runtime/runtime-api.md) — the contract path itself: the two-lane ingress principle, the operation surface (`Create`/`Restore`/`Fire`/`Update`), and restoration semantics.
- [`runtime/result-types.md`](runtime/result-types.md) — the structured outcomes through which governed refusals surface: `ConstraintsFailed`, `InvalidArgs`, `InvalidFields`, `FieldNotEditable`, and the full verdict space.
- [`runtime/fault-system.md`](runtime/fault-system.md) — the `[StaticallyPreventable]` chain and the defense-in-depth trap model for out-of-contract data.

Read this document to classify; read those to build.
