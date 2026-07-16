---
title: "The Hybrid Model — Four Dispositions for Every Obligation"
date: 2026-07-14
author: Frank
status: Working — Draft D (candidate for canonical promotion)
---

# The Hybrid Model — Four Dispositions for Every Obligation

Every declared constraint in a `.precept` definition becomes an obligation the engine must settle: a field modifier such as `nonnegative` or `maxlength 200`, a `rule`, an `ensure`, a guard. Precept makes two guarantees about how those obligations are settled, both stated in the language spec's guarantee contract (§0.7): a definition that compiles without diagnostics cannot produce a runtime fault, and every value entering the entity from outside is held to its declared constraints before it can persist.

Those two guarantees are delivered by **four dispositions**. Every obligation in a real definition lands in exactly one of them. The sections below name the four, give the three questions that route each obligation to one of them, state the two habits that keep the routing exact, and describe the moment each disposition takes effect.

- §1 — the four dispositions
- §2 — routing: the questions that place an obligation
- §3 — the unit of classification: the write and the obligation
- §4 — internal facts as premises
- §5 — when each settlement takes effect
- §6 — defaults and collections
- §7 — reference cross-section

---

## 1. The four dispositions

### 1.1 Prove-or-reject

**Scope.** A value that an expression inside the definition produces — a computed field (`<-`), an assignment result (`set F = expr`), a collection cardinality after a grow or shrink — and whose value is not fixed at compile time. The obligation covers every declared bound or rule constraining that value, together with the fault-freedom of the expression that produces it. Faults are one enumerable family within this scope, alongside bound containment: division by zero, `sqrt`/`pow` domain violations, numeric overflow, empty-collection access, a result outside a declared bound (§0.6, §0.7). The scope is set by the value being computed; fault-freedom is one part of what must hold, and every declared bound and rule on the value is the rest.

**Verdict.** Three outcomes (§0.6 philosophy #2, #5): *proven* (discharged), *proven-violating* (rejected with a witnessing configuration), *unresolved* (rejected, with the weakest precondition that would discharge it printed verbatim). Both non-proven verdicts block; a reachable computed write the definition left with no authored disposition is a defect under prevention.

**Example (money).** In `samples/invoice-line-item.precept`, `field DiscountAmount as money in '{CurrencyCode}' <- Subtotal * DiscountPercent / 100` computes a value from editable source fields. The division carries a fault-freedom obligation, and the `money` result carries containment against its currency and bounds. The literal divisor `100` discharges divisor safety directly; a divisor drawn from a field discharges only when a `nonzero`/`positive` modifier or a rule supplies the fact (§0.6 #3).

### 1.2 Govern

**Scope.** A constraint is governed when at least one value it references can still be written from outside the definition on some reachable path — a construction input, an event argument, or a direct edit to an `editable` field (§0.7). This reachable external write path is what gives governance something to enforce.

**Two enforcement points (§3A.4).** *Ingress* checks each external value against its declared constraints as it enters, before any computation derives from it. The *post-mutation sweep* re-checks every constraint against the completed working copy before promotion. A working copy that fails any constraint is discarded, so an invalid configuration never persists. Ingress governs what enters; the sweep governs the result.

**Example (temporal counter and event argument).** In `samples/library-book-checkout.precept`, `event Checkout(..., LoanDays as integer positive)` supplies `LoanDays` from outside, and `positive` is checked at ingress on every `Checkout`. The field `field RenewalCount as integer default 0 nonnegative` under `rule RenewalCount <= 2 because "..."` is governed across the sweep whenever the renewal path writes it.

### 1.3 Definition incoherence

**Scope.** A constraint over values that are all fixed at compile time — constant defaults, defaulted event arguments — that constant-folds to a provable violation. The definition admits no valid entity: the values that would exist can never satisfy the constraint (§0.6 #7, #11).

**Verdict.** Error. The compiler rejects the definition and names the witnessing configuration. The diagnostic family is `DefaultViolatesRule` and `UnsatisfiableInitialState` for defaults against rules and initial-state ensures (§0.6 #11), `ContradictoryRule` (PRE0155) and `UnsatisfiableRule` (PRE0159) for a rule set no value can satisfy (§0.6 #7), and assignment-range impossibility (`OutOfRange`) for a fixed value outside a declared bound (§0.6 #6).

**Example (money).** A fee definition declaring `field MinCharge as money in 'USD' default '10.00 USD'` and `field FlatFee as money in 'USD' default '5.00 USD'` under `rule FlatFee >= MinCharge because "the flat fee floors the minimum charge"` folds its fixed defaults to `5 >= 10`, which is false. `DefaultViolatesRule` rejects it before any instance exists — no valid `Create` is possible (the `money in 'USD' default '...'` and `rule ... because` constructs are as used in `samples/library-book-checkout.precept`).

### 1.4 Dead/flag

**Scope.** A constraint over compile-time-determined values that constant-folds to true for every admissible configuration, or a guard or branch that can never execute. The constraint constrains nothing, and the entity stays fully inhabitable (§0.6 #8, #9, #10).

**Verdict.** Warning — a report, and the definition still compiles. The diagnostic family is `VacuousRule` (PRE0154) for an always-true rule, and `TautologicalGuard` (PRE0153) and `UnsatisfiableGuard` (PRE0082) for a guard with no effect or no reachable execution.

**Example (collection counter).** A `rule ItemCount >= 0` placed over the `field ItemCount as integer default 0 nonnegative` in `samples/shopping-cart.precept` restates the field's own `nonnegative` contract, folds to always-true, and reports as `VacuousRule`.

---

## 2. Routing: the questions that place an obligation

Three questions decide which disposition an obligation lands in. Each is asked about the value the obligation constrains, and — for fault-freedom — the expression that produces it.

1. **Origin.** Did an expression inside the definition produce this value, or did it enter from outside? A value the definition computes (a `<-` field, a `set` result, a derived collection cardinality) has internal origin. A value handed in — construction input, event argument, direct edit — has external origin. The spec draws this line at the moment a value enters, before any computation derives from it (§0.7).

2. **Reachability of an external write.** Can this value still be written from outside on some reachable path? An `editable` field, a construction input, and an event argument are externally writable. A field with no `editable`, no `<-`, and no `set` anywhere, holding only its default, is fixed once determined.

3. **Decidability.** Is the constraint's truth settled by the definition alone, or does it depend on values that vary at runtime?

The answers route as follows:

| Obligation over a value that is… | Constraint folds to… | Disposition |
|---|---|---|
| internal origin, varies at runtime | (compile-time proof of containment + fault-freedom) | **Prove-or-reject** |
| reachable by an external write | (checked as values arrive and at the sweep) | **Govern** |
| all fixed at compile time | a provable violation | **Definition incoherence** |
| all fixed at compile time | always-true / unreachable | **Dead/flag** |

The reachable-external-write question is load-bearing. Governance has work to do only where a value can arrive from outside; a constraint over values that are all fixed internally has no external supplier to answer to, so it settles at compile time — as coherence (incoherence or dead/flag) — rather than under governance. A constraint that mixes an externally-writable value with a fixed internal one is governed on the strength of the writable value: the sweep re-checks it whenever that value moves, and the fixed value enters the check as a known premise (§4).

---

## 3. The unit of classification: the write and the obligation

Two habits keep the routing exact.

**Provenance attaches to each write.** A field is storage; the values it holds over its lifetime can have different origins. `field UnitPrice as money in '{CurrencyCode}' default '0 {CurrencyCode}' nonnegative editable` in `samples/invoice-line-item.precept` holds a fixed default at construction and an external edit afterward. The default is a fixed internal value, settled at compile time against `nonnegative`; each later edit is an external value, governed at ingress. A field that carries a default, then an event-driven `set`, then a direct edit holds three occupants of three origins, and each is classified at its own write site. Provenance is a property of a write, spatially and temporally, and a field carries no single provenance of its own.

**Obligations are counted one by one inside a rule.** A rule is a statement that can contain several obligations. A rule of the shape `Ceiling > sqrt(Base)` over two fields produces a fault-freedom obligation on `sqrt(Base)` — its operand must be non-negative (§0.6 #4) — a relational containment obligation that `sqrt(Base)` stay below `Ceiling`, and, if either field is externally writable, a governance obligation over the pair. Each routes on its own operands. A rule is never itself proven; the proof obligations belong to the values a rule constrains and to the expressions inside it, and a rule can serve as a premise that discharges another value's obligation (§0.6 relational reasoning; §4).

---

## 4. Internal facts as premises

A proof discharges by drawing on what is already known. The knowledge a premise contributes depends on its origin.

**An external value contributes its declared envelope.** `event RecordMeasurement(Measured as quantity in 'mm')` in `samples/manufacturing-quality-inspection.precept` supplies a measurement whose only compile-time-known fact is the constraint it carries. A downstream `sqrt(Measured)` discharges when a `nonnegative` modifier or a rule puts `Measured >= 0` in scope, and the proof rests on that envelope, with no specific magnitude available to it.

**A fixed internal value contributes its exact value.** A field fixed at `default 16` enters a proof as `16`, so `sqrt` of it discharges directly. A field fixed at a negative default makes `sqrt` of it fold to a certain domain violation, and the obligation is proven-violating and rejected (§0.6 #4, philosophy #1). An exact value settles obligations that an envelope would leave unresolved, and it turns a possible fault into a proven one.

A fixed internal value therefore carries weight beyond its own coherence check: it is also a premise for every obligation that reads it. This is what lets a chain of internal facts — one default expression reading another field's fixed default — discharge cleanly, each fixed value supplying its exact magnitude to the proof above it.

---

## 5. When each settlement takes effect

Disposition answers which settlement an obligation gets. A second, independent question is when that settlement takes effect. There are three moments.

**Compile-time — before any instance exists.** Prove-or-reject, definition incoherence, and dead/flag all settle here. The compiler discharges or rejects, and no runtime step repeats the work.

**Construction-time — the first moment an entity exists.** Construction is modeled as an initial event (§3A.5). Two things settle here: the external values supplied to construction — construction inputs and initial event arguments — are governed at ingress and across the construction sweep; and the internal defaults materialize into the new entity, carrying obligations already discharged at compile time. A definition whose required fields cannot be supplied at birth is rejected (`RequiredFieldsNeedInitialEvent`, `InitialEventMissingAssignments`, §3A.5). A stateless, default-only precept still has this construction moment even when no operation ever follows it.

**Operation-time — every event, edit, or state action on an existing entity.** External values entering the operation are governed at ingress; the post-mutation sweep re-checks every constraint against the working copy before promotion (§3A.4).

Location is orthogonal to disposition:

| Disposition | Compile-time | Construction-time | Operation-time |
|---|---|---|---|
| Prove-or-reject | settles | — | — |
| Definition incoherence | settles | — | — |
| Dead/flag | settles | — | — |
| Govern | — | ingress on construction inputs + sweep | ingress on edits/event args + sweep |

Governance settles at two moments — construction-time for birth data, operation-time for everything after — against the same declared constraints. A field governed at ingress on an edit is the same field whose default was discharged at compile time and materialized at construction. The constraint is one; its settlements are several, each at its own moment.

---

## 6. Defaults and collections

A default value has internal origin: the definition authored it, and §0.7 places the external boundary at values entering from outside. Its disposition follows the routing of §2.

- **A defaulted, externally-writable field** holds a fixed internal value at construction and external values thereafter. The default settles at compile time against the field's constraints; the edits are governed. `field TaxRate as decimal default 0.08 nonnegative max 1 maxplaces 4 editable` (`samples/invoice-line-item.precept`) discharges `0.08` against `nonnegative max 1` at compile time and governs every later edit.

- **A defaulted field with no reachable external write and no recomputation** holds its default forever. Its constraint is a coherence obligation: satisfied silently when the fixed default holds, rejected as definition incoherence when the fixed default violates it (the fee-schedule shape in §1.3), or reported as dead/flag when the constraint folds to always-true. Such a field discharges its default against its own modifiers at compile time — `default 1` against `positive`, `default 0` against `nonnegative` — and a fixed default the field's own rules exclude rejects with `DefaultViolatesRule` (§0.6 #11).

- **A default expression** is subject to fault-freedom like any other expression, and its operands are fixed, so the obligation folds by exact value (§4). A default computed from a safe constant discharges; one that would fault is proven-violating and rejects.

Collections fold in with one extension. A constant default collection has a known initial cardinality — `default []` has count 0 — and that cardinality is a coherence obligation against the field's count bounds, settled at compile time, mechanically the same check the proof engine runs on a grow or shrink at runtime (§0.6 count-interval reasoning; `CountBoundViolation` PRE0136). A `mincount 1` field defaulted empty is rejected at compile time as an unsatisfiable initial cardinality; a `maxcount` the initial default never exceeds discharges. Once an operation grows or shrinks the collection, the resulting cardinality is a computed value under prove-or-reject, and the initial-default cardinality and the post-mutation cardinality are distinct obligations at distinct write sites (§3). In `samples/shopping-cart.precept`, `field GiftMessages as queue of string` starts empty and grows through `enqueue GiftMessages AddGiftMessage.Message`; the empty initial state and each enqueue are separate cardinality obligations.

---

## 7. Reference cross-section

Each row traces one declaration through origin, external writability, disposition, and settlement location. Examples are drawn from across domains.

| Declaration (source) | Origin | External write reachable? | Disposition | Settles at |
|---|---|---|---|---|
| `field Subtotal as money in '{CurrencyCode}' <- UnitPrice * Quantity` (invoice) | internal | no | Prove-or-reject | compile-time |
| `LoanDays as integer positive` event arg (library-book-checkout) | external | yes | Govern | operation-time (ingress) |
| `field CustomerId as string notempty maxlength 100` (shopping-cart, construction input) | external | yes (at construction) | Govern | construction-time (ingress) |
| `field ConversionFactor as decimal default 1 positive editable` (unit-of-measure) | default: internal / edits: external | yes | default → coherence; edits → Govern | compile-time + operation-time |
| `rule RenewalCount <= 2` with `RenewalCount` written by events (library) | governs a written field | yes | Govern | operation-time (sweep) |
| `rule FlatFee >= MinCharge` over two fixed money defaults | internal, fixed | no | Definition incoherence | compile-time |
| `rule ItemCount >= 0` over a `nonnegative` field (shopping-cart) | internal, fixed | no | Dead/flag | compile-time |
| `field GiftMessages as queue of string` default-empty, grown by `enqueue` (shopping-cart) | default: internal / growth: internal computed | no | initial count → coherence; growth → Prove-or-reject | compile-time + operation-time |

---

## 8. How the two guarantees follow

The guarantee contract of §0.7 holds because each obligation has exactly one of these settlements and each settlement is sound. A compiled definition carries no undischarged fault obligation — prove-or-reject blocks any computed write it cannot discharge, and definition incoherence blocks any fixed configuration that violates its own constraints. Every externally-supplied value meets its constraints before it can persist — governance checks it at ingress and re-checks the whole configuration at the sweep, and a working copy that fails is discarded (§3A.4). The four dispositions enumerate how a definition's obligations reach those two guarantees: origin and reachability route each obligation to its disposition, exact-valued internal premises discharge the proofs that read them, and the settlement axis fixes when each takes effect.
