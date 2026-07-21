<!--
GENERATED FILE — do not hand-edit.
Source: fault-9-division-business-guard-positions.cells.json
Regenerate: dotnet run --project tools/Precept.MatrixTools -- render-cells docs/Working/obligation-discharge-matrix-2026-07-19-cells
Hand edits will be overwritten. Edit the .cells.json and regenerate.
-->

# Fault family — business-domain division by zero inside a guard (Group 9)

Family id: fault-9-division-business-guard-positions
Case shape: fault
Definition version: obligation-discharge-matrix-2026-07-19 rev 7 (2026-07-20, draft)

**Where this family's content comes from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row: obligation is the catalog-declared safety precondition at the evaluation site; discharge is premise classes (b)/(c)/(a) per catalog ProofSatisfactions
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge: the worked precedent this group's contracts follow
- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault prevention as an obligation family
- src/Precept/Language/Operations.cs — code: MoneyDivideDecimal and QuantityDivideQuantitySameDimension entries, each carrying a NumericProofRequirement 'Divisor must be non-zero' on the right operand
- src/Precept/Language/Modifiers.cs — code: Positive/Nonnegative/Nonzero modifiers, applicable to Money/Quantity/Price/ExchangeRate/Decimal alike (ZeroBoundNumericTypes), each carrying a ProofSatisfaction.Numeric
- docs/language/precept-language-spec.md § Expression scope — spec: per-context lexical scope table this group's premise-availability derivations read off directly
- docs/language/precept-language-spec.md § Access mode and omit declaration — spec: the access-mode guard's grammar shape — no event anchor

## Respellability — the family verdict

Can every safe program these rules reject be rewritten into a form they accept? **yes**

Evidence: model-derived
Corpus measurement: pending

Spellings that look rejected but are in fact licensed (kept so the boundary stays findable):

- `BillingPeriods min 1` — A `min` bound whose interval excludes zero is licensed by the same interval-containment check as `positive`/`nonzero` — the decision procedure computes the divisor's full declared interval from every bound-establishing modifier, not by pattern-matching modifier names, so `min 1`, `min -5 max -1`, etc. all discharge identically to `positive`/`nonzero` wherever they exclude zero.
  - docs/Working/obligation-discharge-matrix-2026-07-19.md:206 — matrix: Arg-bound interval arithmetic — the argument reasons over the declared interval, not over a fixed set of modifier names


**Notes on the family verdict**

- A genuine band member: a divisor whose nonzero-ness follows only from an algebraic relationship to another field (e.g. `UnitsOrdered == 2 * SomeOtherPositiveField`, with no bound declared on `UnitsOrdered` itself) — sound, but no single-field interval derivation covers it, by the same reasoning Witness Family 1 gives for its own algebraic-rearrangement band member. Respelling: state a direct `positive`/`nonzero`/`min`/`max` bound on the divisor itself.
- All ten cells inherit this verdict; none states an override.

**Sources for this verdict**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Respellable bullet: verdict stated once per contract family, inherited by every cell
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Ratification protocol — matrix: layer 4: corpus measurement is the hard gate

## Premise classes — the legend

Where a proof may get its facts (want :19, :143): (a) field modifiers, (b) arg constraints, (c) the handler's guard, (d) all rules holding in the pre-state (the inductive hypothesis), (e) evaluating the applicable rule set at an editable-field write — (e)'s status as a genuinely fifth class vs (a) generalized is an open owner item.

## g09/money-decimal/transition-row-guard — Money ÷ decimal divisor, transition-row guard

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 shape, extended to a business-domain divisor and a guard evaluation site

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | MoneyDivideDecimal |
| evaluation site category | transition-row-guard |
| type family | business-domain |

### What must be proven

Obligation: BillingPeriods != 0

Weakest precondition: BillingPeriods != 0
Key pinned by: Not computed: the guard position mints no fault obligation at HEAD (live-verified below), so the calculator has no obligation to key — the printed WPs in the run are the unrelated rule-preservation obligation contributed by a `rule` in scope, not this fault. No canonical key is invented here.

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| BillingPeriods | the divisor — the right operand of the money-over-decimal division (Operations.cs: MoneyDivideDecimal names the requirement's subject 'right operand'; ResolveParamInBinaryOp resolves shared-slot cases to the right operand by convention, not needed on this site since the two operand types differ) |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a), (b).

Per `precept-language-spec.md § Expression scope`, a transition row's guard has 'All field names + current event's args' in scope, so both the divisor's own declared modifier (class a, always available for a field regardless of lexical scope, since a field modifier is enforced on every assignment — `src/Precept/Language/Modifiers.cs`) and, were the divisor instead spelled as the firing event's arg, its declared constraint (class b) are available. Class (c) is excluded: the row's own guard is exactly the expression this obligation arises inside, and per `precept-language-spec.md:1897` ('guards select') a row's actions execute only after its guard is found true — the guard cannot presuppose its own truth as a premise for a fault arising during its own evaluation. Class (d) is not admitted here: the matrix's own per-family case-shape table (`obligation-discharge-matrix-2026-07-19.md` § Per-family case shapes, the Fault family row) states the fault family's premise classes as (a)/(b)/(c) only. This is recorded as a live tension, not resolved unilaterally — see notes.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: the divisor field's own declared bound-establishing modifiers exclude zero from its interval
- Strategy: IntervalContainment
- Capability tier: 1a
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Look up the divisor field's declared bound-establishing modifiers (`min`, `max`, `positive`, `nonnegative`, `nonzero` — the modifier→ProofSatisfaction table, `src/Precept/Language/Modifiers.cs`) and compute its declared interval by the same exact bound extraction the matrix cites for business-domain types (`docs/Working/obligation-discharge-matrix-2026-07-19.md:206`, citing `business-domain-types.md:426`). If the computed interval excludes zero, the class is licensed; a field with no such modifier, or a declared interval spanning zero (e.g. `nonnegative` alone), fails the class.

- docs/Working/obligation-discharge-matrix-2026-07-19.md:206 — matrix: written for a class-(b) event arg in the exact primitive lanes; extended here to a class-(a) field modifier on a business-domain-typed value by the Vocabulary's own stated symmetry between (a) and (b) as ingress-enforced facts (§ Vocabulary, the Discharge mechanisms bullet: 'by the same symmetry the editable-field door makes premises (a) and (d) true for that field')

**Entry 2 — (b)**

- Derivation: the firing event's arg declaration for the divisor excludes zero from its interval, when the divisor is spelled as an event arg rather than a field
- Strategy: IntervalContainment
- Capability tier: 1a
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Same interval computation as the class-(a) entry, run over the firing event's arg declaration instead of a field declaration — this is the matrix's own Family 4 shape directly (`obligation-discharge-matrix-2026-07-19.md` § Witnesses, Family 4: `Months as integer positive` discharging `-Balance / PlanRepayment.Months`), not independently re-witnessed in this cell (the cell's own witness uses a field divisor to demonstrate class (a) directly).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4

### What the failing diagnostic must suggest

- For class (a): declare the divisor's field with a modifier whose declared interval excludes zero (e.g. `positive`, `nonzero`, or a `min`/`max` pair not spanning zero)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (b): declare the firing event's arg (if the divisor is spelled as an arg rather than a field) with a modifier whose declared interval excludes zero
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept BillingTransitionGuardMD
field TotalBilled as money in 'USD' default '0 USD'
field BillingPeriods as decimal default 1.0
state Active initial
state Closed terminal
event Open initial
event Adjust(NewPeriods as decimal)
event Close
on Open
  -> set TotalBilled = '500 USD'
from Active on Adjust when TotalBilled / BillingPeriods < '1000 USD'
  -> set BillingPeriods = Adjust.NewPeriods
  -> no transition
from Active on Close
  -> transition Closed
```

Required outcome: reject, naming the missing premise classes (a), (b), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools (release build), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*field-positive* — other

- Addition: field BillingPeriods as decimal default 1.0 positive
- Premise classes: (a)
- Derivation: positive excludes zero from BillingPeriods' interval -> IntervalContainment (capability tier 1a)
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (the transition-row-guard evaluation site mints no fault obligation at HEAD at all (measured live below); the clean compile observed here is the site not minting, not the addition being accepted as a discharge — per the group's built-status finding.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (release build), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` is recorded: the schema's application DU (row-guard / event-arg-declarations) has no locus for a field-declaration modifier addition — a vocabulary gap for the cell→test conversion, recorded rather than worked around.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| field-positive | field BillingPeriods as decimal default 1.0 nonnegative | nonnegative admits zero; BillingPeriods != 0 is not implied — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Live-verified at HEAD (commit e1a14d91, Precept.MatrixTools release build, 2026-07-21): base, discharge, and near-miss all compile with zero diagnostics — no DivisionByZero, no other error or warning. This confirms the group's carried finding (all five guard positions mint nothing at HEAD) extends to a business-domain Money/Decimal divisor, not only the primitive lanes previously measured. Recorded model-provable / built-unresolved throughout, per the group's authoring notes.
- Exploratory run, not a licensed discharge: a variant substituting `rule BillingPeriods > 0 because "..."` for the field modifier (premise class d) was also live-verified (same tool/date/head) and likewise compiles clean with zero diagnostics — again because the site mints nothing, not because the rule discharges anything. This is NOT recorded as a formal discharge witness because (1) the matrix's own Fault-family case-shape row does not list class (d) as applicable at all, and (2) even if it were, `authored-expressiveness-gaps.md` Part 3 Defect B verifies that a `rule`-premise discharge of a fault-family divisor obligation can be a false proof via the `CompositionalConstraint` strategy at HEAD — so a clean compile through that path could never be read as confirmation regardless. Both points are carried to `missingRules`/`openQuestions` rather than resolved here. The weakened near-miss variant (`rule BillingPeriods >= 0`) was run for completeness and is equally clean, equally uninformative, for the same reason.
- Class (b) is listed as applicable (the category admits an event-arg divisor) but is not independently witnessed in this cell; the cell's own witness demonstrates class (a) on a field divisor. The matrix's own Family 4 witness already works the class-(b) event-arg shape out directly and is cited rather than duplicated.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4
- src/Precept/Language/Operations.cs — code: MoneyDivideDecimal
- docs/language/precept-language-spec.md:1897 — spec: guards select

## g09/quantity-quantity/transition-row-guard — Quantity ÷ quantity (same dimension) divisor, transition-row guard

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 shape, extended to a business-domain divisor and a guard evaluation site

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | QuantityDivideQuantitySameDimension |
| evaluation site category | transition-row-guard |
| type family | business-domain |

### What must be proven

Obligation: UnitsOrdered != 0

Weakest precondition: UnitsOrdered != 0
Key pinned by: Not computed: the guard position mints no fault obligation at HEAD (live-verified below); no canonical key is invented.

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| UnitsOrdered | the divisor — both operand slots of QuantityDivideQuantitySameDimension share one ParameterMeta instance; ProofEngine.ResolveParamInBinaryOp resolves the requirement to the right operand by convention (`src/Precept/Pipeline/ProofEngine.cs`) |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a), (b).

Same derivation as the money/decimal transition-row-guard cell in this file: `precept-language-spec.md § Expression scope` puts the firing event's args in scope alongside all field names, licensing (a) and (b); class (c) is excluded by the self-discharge argument (`precept-language-spec.md:1897`); class (d) is excluded per the matrix's Fault-family case-shape row, recorded as a tension rather than resolved.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: the divisor field's own declared bound-establishing modifiers exclude zero from its interval
- Strategy: IntervalContainment
- Capability tier: 1a
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Same as the money/decimal cell's class-(a) entry, over the quantity-typed divisor's declared modifiers (`positive`/`nonzero`/`min`/`max` apply identically to `Quantity` per `src/Precept/Language/Modifiers.cs` ZeroBoundNumericTypes/RangedNumericTypes).

- docs/Working/obligation-discharge-matrix-2026-07-19.md:206 — matrix
- src/Precept/Language/Modifiers.cs — code

**Entry 2 — (b)**

- Derivation: the firing event's arg declaration for the divisor excludes zero from its interval, when the divisor is spelled as an event arg rather than a field
- Strategy: IntervalContainment
- Capability tier: 1a
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Same as the money/decimal cell's class-(b) entry (matrix § Witnesses, Family 4); not independently witnessed in this cell.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4

### What the failing diagnostic must suggest

- For class (a): declare the divisor's field with a modifier whose declared interval excludes zero (e.g. `positive`, `nonzero`, or a `min`/`max` pair not spanning zero)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (b): declare the firing event's arg (if the divisor is spelled as an arg rather than a field) with a modifier whose declared interval excludes zero
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept BillingTransitionGuardQQ
field UnitsShipped as quantity in 'each' default '0 each'
field UnitsOrdered as quantity in 'each' default '1 each'
state Active initial
state Closed terminal
event Open initial
event Adjust(NewOrdered as quantity in 'each')
event Close
on Open
  -> set UnitsShipped = '0 each'
from Active on Adjust when UnitsShipped / UnitsOrdered < 1.0
  -> set UnitsOrdered = Adjust.NewOrdered
  -> no transition
from Active on Close
  -> transition Closed
```

Required outcome: reject, naming the missing premise classes (a), (b), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools (release build), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*field-positive* — other

- Addition: field UnitsOrdered as quantity in 'each' default '1 each' positive
- Premise classes: (a)
- Derivation: positive excludes zero from UnitsOrdered' interval -> IntervalContainment (capability tier 1a)
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (the transition-row-guard evaluation site mints no fault obligation at HEAD at all (measured live below); the clean compile observed here is the site not minting, not the addition being accepted as a discharge.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (release build), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` is recorded: the schema's application DU has no locus for a field-declaration modifier addition — recorded as a vocabulary gap, not worked around.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| field-positive | field UnitsOrdered as quantity in 'each' default '1 each' nonnegative | nonnegative admits zero; UnitsOrdered != 0 is not implied — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Live-verified at HEAD (commit e1a14d91, 2026-07-21): base, discharge, and near-miss all compile with zero diagnostics. Confirms the mint-nothing finding extends to the quantity/quantity same-dimension lane, not only money/decimal.
- Class (b) is applicable but not independently witnessed here — see the money/decimal cell in this file for the same note; the matrix's own Family 4 witness covers the shape.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4
- src/Precept/Language/Operations.cs — code: QuantityDivideQuantitySameDimension

## g09/money-decimal/state-hook-guard — Money ÷ decimal divisor, state entry-hook guard

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 shape, extended to a business-domain divisor and this guard evaluation site

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | MoneyDivideDecimal |
| evaluation site category | state-hook-guard |
| type family | business-domain |

### What must be proven

Obligation: BillingPeriods != 0

Weakest precondition: BillingPeriods != 0
Key pinned by: Not computed: the guard position mints no fault obligation at HEAD (live-verified below); no canonical key is invented.

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| BillingPeriods | the divisor — the right operand of the money-over-decimal division |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a).

Per `precept-language-spec.md § Expression scope`, 'State action guard / actions' has only 'All field names' in scope — no event is anchored to a state entry/exit hook, so class (b) has nothing to instantiate. Class (a) remains available (a field modifier is a universal structural fact, not scoped by lexical position). Class (c) is excluded by the same self-discharge argument as the transition-row guard (`precept-language-spec.md:1897`). Class (d) is excluded per the matrix's Fault-family case-shape row — a tension recorded, not resolved, in the transition-row-guard cell's notes and carried once for the whole group.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: the divisor field's own declared bound-establishing modifiers exclude zero from its interval
- Strategy: IntervalContainment
- Capability tier: 1a
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Look up the divisor field's declared bound-establishing modifiers and compute its declared interval (`docs/Working/obligation-discharge-matrix-2026-07-19.md:206`); an interval excluding zero licenses the class, one admitting zero (or no modifier at all) fails it.

- docs/Working/obligation-discharge-matrix-2026-07-19.md:206 — matrix

### What the failing diagnostic must suggest

- For class (a): declare the divisor's field with a modifier whose declared interval excludes zero (e.g. `positive`, `nonzero`, or a `min`/`max` pair not spanning zero)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept BillingStateHookGuardMD
field TotalBilled as money in 'USD' default '500 USD'
field BillingPeriods as decimal default 1.0
field PeriodAverage as money in 'USD' default '0 USD'
state Draft initial
state Done terminal
event Open initial
event Finish
on Open
  -> set TotalBilled = '500 USD'
  -> set BillingPeriods = 1.0
to Done when TotalBilled / BillingPeriods > '0 USD' -> set PeriodAverage = '1 USD'
from Draft on Finish
  -> transition Done
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools (release build), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*field-positive* — other

- Addition: field BillingPeriods as decimal default 1.0 positive
- Premise classes: (a)
- Derivation: positive excludes zero from BillingPeriods' interval -> IntervalContainment (capability tier 1a)
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (the state-hook-guard evaluation site mints no fault obligation at HEAD at all; the clean compile observed here is the site not minting.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (release build), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` is recorded: the schema's application DU has no locus for a field-declaration modifier addition.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| field-positive | field BillingPeriods as decimal default 1.0 nonnegative | nonnegative admits zero; BillingPeriods != 0 is not implied — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Live-verified at HEAD (commit e1a14d91, 2026-07-21): base, discharge, and near-miss all compile with zero diagnostics. Matches the group's carried built-status finding for this position.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4
- docs/language/precept-language-spec.md § Expression scope — spec

## g09/quantity-quantity/state-hook-guard — Quantity ÷ quantity (same dimension) divisor, state entry-hook guard

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 shape, extended to a business-domain divisor and this guard evaluation site

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | QuantityDivideQuantitySameDimension |
| evaluation site category | state-hook-guard |
| type family | business-domain |

### What must be proven

Obligation: UnitsOrdered != 0

Weakest precondition: UnitsOrdered != 0
Key pinned by: Not computed: the guard position mints no fault obligation at HEAD (live-verified below); no canonical key is invented.

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| UnitsOrdered | the divisor — shared-slot operand, resolved to the right operand by convention |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a).

Same derivation as the money/decimal state-hook-guard cell in this file.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: the divisor field's own declared bound-establishing modifiers exclude zero from its interval
- Strategy: IntervalContainment
- Capability tier: 1a
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Same as the money/decimal cell's class-(a) entry, over the quantity-typed divisor's declared modifiers.

- docs/Working/obligation-discharge-matrix-2026-07-19.md:206 — matrix

### What the failing diagnostic must suggest

- For class (a): declare the divisor's field with a modifier whose declared interval excludes zero (e.g. `positive`, `nonzero`, or a `min`/`max` pair not spanning zero)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept BillingStateHookGuardQQ
field UnitsShipped as quantity in 'each' default '5 each'
field UnitsOrdered as quantity in 'each' default '1 each'
field FulfillmentRatio as decimal default 0.0
state Draft initial
state Done terminal
event Open initial
event Finish
on Open
  -> set UnitsShipped = '5 each'
  -> set UnitsOrdered = '1 each'
to Done when UnitsShipped / UnitsOrdered > 0.0 -> set FulfillmentRatio = 1.0
from Draft on Finish
  -> transition Done
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools (release build), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*field-positive* — other

- Addition: field UnitsOrdered as quantity in 'each' default '1 each' positive
- Premise classes: (a)
- Derivation: positive excludes zero from UnitsOrdered' interval -> IntervalContainment (capability tier 1a)
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (the state-hook-guard evaluation site mints no fault obligation at HEAD at all; the clean compile observed here is the site not minting.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (release build), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` is recorded: the schema's application DU has no locus for a field-declaration modifier addition.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| field-positive | field UnitsOrdered as quantity in 'each' default '1 each' nonnegative | nonnegative admits zero; UnitsOrdered != 0 is not implied — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Live-verified at HEAD (commit e1a14d91, 2026-07-21): base, discharge, and near-miss all compile with zero diagnostics.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4

## g09/money-decimal/access-mode-guard — Money ÷ decimal divisor, access-mode guard

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 shape, extended to a business-domain divisor and this guard evaluation site

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | MoneyDivideDecimal |
| evaluation site category | access-mode-guard |
| type family | business-domain |

### What must be proven

Obligation: BillingPeriods != 0

Weakest precondition: BillingPeriods != 0
Key pinned by: Not computed: the guard position mints no fault obligation at HEAD (live-verified below); no canonical key is invented.

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| BillingPeriods | the divisor — the right operand of the money-over-decimal division |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a).

The access-mode declaration (`in S when G modify F readonly\|editable`, `precept-language-spec.md § Access mode and omit declaration`) is not anchored to any event, so no event args are ever in scope for its guard — the spec's Expression Scope table carries no dedicated row for this construct (recorded here as an honest gap rather than an invented answer), but the construct's own grammar shape rules out event-arg scope structurally, the same way the state-action-guard row does. Class (a) remains available (universal structural fact). Class (c) is excluded by the same self-discharge argument. Class (d) is excluded per the matrix's Fault-family case-shape row — the tension is recorded once, in the transition-row-guard cell's notes.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: the divisor field's own declared bound-establishing modifiers exclude zero from its interval
- Strategy: IntervalContainment
- Capability tier: 1a
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Look up the divisor field's declared bound-establishing modifiers and compute its declared interval (`docs/Working/obligation-discharge-matrix-2026-07-19.md:206`); an interval excluding zero licenses the class.

- docs/Working/obligation-discharge-matrix-2026-07-19.md:206 — matrix

### What the failing diagnostic must suggest

- For class (a): declare the divisor's field with a modifier whose declared interval excludes zero (e.g. `positive`, `nonzero`, or a `min`/`max` pair not spanning zero)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept BillingAccessModeGuardMD
field TotalBilled as money in 'USD' default '500 USD'
field BillingPeriods as decimal default 1.0
field Note as string optional editable
state Draft initial
state Done terminal
event Open initial
event Finish
on Open
  -> set TotalBilled = '500 USD'
  -> set BillingPeriods = 1.0
in Draft when TotalBilled / BillingPeriods > '0 USD' modify Note readonly
from Draft on Finish
  -> transition Done
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools (release build), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*field-positive* — other

- Addition: field BillingPeriods as decimal default 1.0 positive
- Premise classes: (a)
- Derivation: positive excludes zero from BillingPeriods' interval -> IntervalContainment (capability tier 1a)
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (the access-mode-guard evaluation site mints no fault obligation at HEAD at all; the clean compile observed here is the site not minting.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (release build), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` is recorded: the schema's application DU has no locus for a field-declaration modifier addition.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| field-positive | field BillingPeriods as decimal default 1.0 nonnegative | nonnegative admits zero; BillingPeriods != 0 is not implied — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Live-verified at HEAD (commit e1a14d91, 2026-07-21): base, discharge, and near-miss all compile with zero diagnostics.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4
- docs/language/precept-language-spec.md § Access mode and omit declaration — spec

## g09/quantity-quantity/access-mode-guard — Quantity ÷ quantity (same dimension) divisor, access-mode guard

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 shape, extended to a business-domain divisor and this guard evaluation site

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | QuantityDivideQuantitySameDimension |
| evaluation site category | access-mode-guard |
| type family | business-domain |

### What must be proven

Obligation: UnitsOrdered != 0

Weakest precondition: UnitsOrdered != 0
Key pinned by: Not computed: the guard position mints no fault obligation at HEAD (live-verified below); no canonical key is invented.

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| UnitsOrdered | the divisor — shared-slot operand, resolved to the right operand by convention |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a).

Same derivation as the money/decimal access-mode-guard cell in this file.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: the divisor field's own declared bound-establishing modifiers exclude zero from its interval
- Strategy: IntervalContainment
- Capability tier: 1a
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Same as the money/decimal cell's class-(a) entry, over the quantity-typed divisor's declared modifiers.

- docs/Working/obligation-discharge-matrix-2026-07-19.md:206 — matrix

### What the failing diagnostic must suggest

- For class (a): declare the divisor's field with a modifier whose declared interval excludes zero (e.g. `positive`, `nonzero`, or a `min`/`max` pair not spanning zero)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept BillingAccessModeGuardQQ
field UnitsShipped as quantity in 'each' default '5 each'
field UnitsOrdered as quantity in 'each' default '1 each'
field Note as string optional editable
state Draft initial
state Done terminal
event Open initial
event Finish
on Open
  -> set UnitsShipped = '5 each'
  -> set UnitsOrdered = '1 each'
in Draft when UnitsShipped / UnitsOrdered > 0.0 modify Note readonly
from Draft on Finish
  -> transition Done
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools (release build), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*field-positive* — other

- Addition: field UnitsOrdered as quantity in 'each' default '1 each' positive
- Premise classes: (a)
- Derivation: positive excludes zero from UnitsOrdered' interval -> IntervalContainment (capability tier 1a)
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (the access-mode-guard evaluation site mints no fault obligation at HEAD at all; the clean compile observed here is the site not minting.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (release build), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` is recorded: the schema's application DU has no locus for a field-declaration modifier addition.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| field-positive | field UnitsOrdered as quantity in 'each' default '1 each' nonnegative | nonnegative admits zero; UnitsOrdered != 0 is not implied — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Live-verified at HEAD (commit e1a14d91, 2026-07-21): base, discharge, and near-miss all compile with zero diagnostics.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4

## g09/money-decimal/ensure-activation-guard — Money ÷ decimal divisor, state-anchored ensure activation guard

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 shape, extended to a business-domain divisor and this guard evaluation site

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | MoneyDivideDecimal |
| evaluation site category | ensure-activation-guard |
| type family | business-domain |

### What must be proven

Obligation: BillingPeriods != 0

Weakest precondition: BillingPeriods != 0
Key pinned by: Not computed: the guard position mints no fault obligation at HEAD (live-verified below); no canonical key is invented.

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| BillingPeriods | the divisor — the right operand of the money-over-decimal division |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a).

This cell instantiates a **state-anchored** ensure (`in Done when G ensure H`, a StateResident constraint per the matrix's Constraint kinds table). Per `precept-language-spec.md § Expression scope`, 'Ensure condition / guard' has only 'All field names' in scope, with no split shown in the table between the state-anchored and event-anchored ensure forms; for this state-anchored instantiation no event is anchored at all, so class (b) has nothing to instantiate. Class (a) remains available. Class (c) is excluded by the self-discharge argument. Class (d) is excluded per the matrix's Fault-family case-shape row. Whether an event-anchored ensure (`on E when G ensure H`, an EventPrecondition) gets event-arg scope for its activation guard the way the table's 'Event handler actions' row does is not settled by the table's literal text and is not instantiated in this cell — recorded as a missing rule, not assumed either way.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: the divisor field's own declared bound-establishing modifiers exclude zero from its interval
- Strategy: IntervalContainment
- Capability tier: 1a
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Look up the divisor field's declared bound-establishing modifiers and compute its declared interval (`docs/Working/obligation-discharge-matrix-2026-07-19.md:206`); an interval excluding zero licenses the class.

- docs/Working/obligation-discharge-matrix-2026-07-19.md:206 — matrix

### What the failing diagnostic must suggest

- For class (a): declare the divisor's field with a modifier whose declared interval excludes zero (e.g. `positive`, `nonzero`, or a `min`/`max` pair not spanning zero)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept BillingEnsureGuardMD
field TotalBilled as money in 'USD' default '500 USD'
field BillingPeriods as decimal default 1.0
state Draft initial
state Done terminal
event Open initial
event Finish
in Done when TotalBilled / BillingPeriods > '0 USD' ensure TotalBilled > '0 USD' because "period average must be positive when done"
on Open
  -> set TotalBilled = '500 USD'
  -> set BillingPeriods = 1.0
from Draft on Finish
  -> transition Done
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools (release build), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*field-positive* — other

- Addition: field BillingPeriods as decimal default 1.0 positive
- Premise classes: (a)
- Derivation: positive excludes zero from BillingPeriods' interval -> IntervalContainment (capability tier 1a)
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (the ensure-activation-guard evaluation site mints no fault obligation at HEAD at all; the clean compile observed here is the site not minting.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (release build), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` is recorded: the schema's application DU has no locus for a field-declaration modifier addition.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| field-positive | field BillingPeriods as decimal default 1.0 nonnegative | nonnegative admits zero; BillingPeriods != 0 is not implied — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Live-verified at HEAD (commit e1a14d91, 2026-07-21): base, discharge, and near-miss all compile with zero diagnostics.
- The task's built-status hazard note (about a `rule`-premise false proof, `authored-expressiveness-gaps.md` Part 3 Defect B) is not exercised in this cell — no class-(d)/rule-premise discharge is authored here, per the case-shape tension recorded on the transition-row-guard cell.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Constraint kinds — matrix: StateResident ensures

## g09/quantity-quantity/ensure-activation-guard — Quantity ÷ quantity (same dimension) divisor, state-anchored ensure activation guard

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 shape, extended to a business-domain divisor and this guard evaluation site

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | QuantityDivideQuantitySameDimension |
| evaluation site category | ensure-activation-guard |
| type family | business-domain |

### What must be proven

Obligation: UnitsOrdered != 0

Weakest precondition: UnitsOrdered != 0
Key pinned by: Not computed: the guard position mints no fault obligation at HEAD (live-verified below); no canonical key is invented.

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| UnitsOrdered | the divisor — shared-slot operand, resolved to the right operand by convention |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a).

Same derivation as the money/decimal ensure-activation-guard cell in this file (state-anchored instantiation).

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: the divisor field's own declared bound-establishing modifiers exclude zero from its interval
- Strategy: IntervalContainment
- Capability tier: 1a
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Same as the money/decimal cell's class-(a) entry, over the quantity-typed divisor's declared modifiers.

- docs/Working/obligation-discharge-matrix-2026-07-19.md:206 — matrix

### What the failing diagnostic must suggest

- For class (a): declare the divisor's field with a modifier whose declared interval excludes zero (e.g. `positive`, `nonzero`, or a `min`/`max` pair not spanning zero)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept BillingEnsureGuardQQ
field UnitsShipped as quantity in 'each' default '5 each'
field UnitsOrdered as quantity in 'each' default '1 each'
state Draft initial
state Done terminal
event Open initial
event Finish
in Done when UnitsShipped / UnitsOrdered > 0.0 ensure UnitsShipped > '0 each' because "shipped units must be positive when done"
on Open
  -> set UnitsShipped = '5 each'
  -> set UnitsOrdered = '1 each'
from Draft on Finish
  -> transition Done
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools (release build), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*field-positive* — other

- Addition: field UnitsOrdered as quantity in 'each' default '1 each' positive
- Premise classes: (a)
- Derivation: positive excludes zero from UnitsOrdered' interval -> IntervalContainment (capability tier 1a)
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (the ensure-activation-guard evaluation site mints no fault obligation at HEAD at all; the clean compile observed here is the site not minting.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (release build), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` is recorded: the schema's application DU has no locus for a field-declaration modifier addition.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| field-positive | field UnitsOrdered as quantity in 'each' default '1 each' nonnegative | nonnegative admits zero; UnitsOrdered != 0 is not implied — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Live-verified at HEAD (commit e1a14d91, 2026-07-21): base, discharge, and near-miss all compile with zero diagnostics.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4

## g09/money-decimal/rule-activation-guard — Money ÷ decimal divisor, conditional rule's activation guard

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 shape, extended to a business-domain divisor and this guard evaluation site

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | MoneyDivideDecimal |
| evaluation site category | rule-activation-guard |
| type family | business-domain |

### What must be proven

Obligation: BillingPeriods != 0

Weakest precondition: BillingPeriods != 0
Key pinned by: Not computed by the calculator for this fault: the compiler folds the guard into the printed rule-preservation WP (observed live below) but raises no obligation over the division inside it, so there is no fault-obligation canonical key to pin — no key is invented.

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| BillingPeriods | the divisor — the right operand of the money-over-decimal division |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a).

Per `precept-language-spec.md § Expression scope`, 'Rule condition / guard' has only 'All field names' in scope — no event is ever anchored to a `rule`, so class (b) never applies here under any instantiation. Class (a) remains available. Class (c) is excluded for the self-discharge reason, doubly so here since the obligation arises inside the very condition class (c) would need to name. Class (d) is excluded per the matrix's Fault-family case-shape row; note this position is itself testing a fact about the pre-state, which makes the (d)-exclusion for this position the sharpest instance of the tension recorded on the transition-row-guard cell.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: the divisor field's own declared bound-establishing modifiers exclude zero from its interval
- Strategy: IntervalContainment
- Capability tier: 1a
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Look up the divisor field's declared bound-establishing modifiers and compute its declared interval (`docs/Working/obligation-discharge-matrix-2026-07-19.md:206`); an interval excluding zero licenses the class.

- docs/Working/obligation-discharge-matrix-2026-07-19.md:206 — matrix

### What the failing diagnostic must suggest

- For class (a): declare the divisor's field with a modifier whose declared interval excludes zero (e.g. `positive`, `nonzero`, or a `min`/`max` pair not spanning zero)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept BillingRuleGuardMD
field TotalBilled as money in 'USD' default '500 USD' editable
field BillingPeriods as decimal default 1.0 editable
rule TotalBilled > '0 USD' when TotalBilled / BillingPeriods > '0 USD' because "billing periods must yield a positive per-period amount"
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools (release build), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*field-positive* — other

- Addition: field BillingPeriods as decimal default 1.0 positive editable
- Premise classes: (a)
- Derivation: positive excludes zero from BillingPeriods' interval -> IntervalContainment (capability tier 1a)
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (the rule-activation-guard evaluation site mints no fault obligation at HEAD at all — the compiler folds the guard into the printed rule[0] preservation WP but raises nothing over the division inside it (observed live below).)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (release build), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` is recorded: the schema's application DU has no locus for a field-declaration modifier addition.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| field-positive | field BillingPeriods as decimal default 1.0 nonnegative editable | nonnegative admits zero; BillingPeriods != 0 is not implied — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Live-verified at HEAD (commit e1a14d91, 2026-07-21): base, discharge, and near-miss all compile with zero diagnostics beyond the printed WP tables (which report the rule's own preservation obligation, an unrelated obligation to the fault this cell defines). The printed base WP was `(0 < (TotalBilled / 1)) implies (0 < TotalBilled)` — the compiler visibly folded the guard's division into the rule's own weakest precondition while minting no diagnostic over that division, exactly the behaviour the group's built-status finding describes.
- Of the whole group, this position has no event and no handler guard anywhere in scope — the narrowest applicable-class set in the group (class (a) alone).

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Constraint kinds — matrix: the Invariant (`rule`) kind

## g09/quantity-quantity/rule-activation-guard — Quantity ÷ quantity (same dimension) divisor, conditional rule's activation guard

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 shape, extended to a business-domain divisor and this guard evaluation site

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | QuantityDivideQuantitySameDimension |
| evaluation site category | rule-activation-guard |
| type family | business-domain |

### What must be proven

Obligation: UnitsOrdered != 0

Weakest precondition: UnitsOrdered != 0
Key pinned by: Not computed by the calculator for this fault: the compiler folds the guard into the printed rule-preservation WP (observed live below) but raises no obligation over the division inside it; no key is invented.

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| UnitsOrdered | the divisor — shared-slot operand, resolved to the right operand by convention |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a).

Same derivation as the money/decimal rule-activation-guard cell in this file.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: the divisor field's own declared bound-establishing modifiers exclude zero from its interval
- Strategy: IntervalContainment
- Capability tier: 1a
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Same as the money/decimal cell's class-(a) entry, over the quantity-typed divisor's declared modifiers.

- docs/Working/obligation-discharge-matrix-2026-07-19.md:206 — matrix

### What the failing diagnostic must suggest

- For class (a): declare the divisor's field with a modifier whose declared interval excludes zero (e.g. `positive`, `nonzero`, or a `min`/`max` pair not spanning zero)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept BillingRuleGuardQQ
field UnitsShipped as quantity in 'each' default '5 each' editable
field UnitsOrdered as quantity in 'each' default '1 each' editable
rule UnitsShipped > '0 each' when UnitsShipped / UnitsOrdered > 0.0 because "shipped units must be positive whenever the fulfillment ratio is positive"
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools (release build), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*field-positive* — other

- Addition: field UnitsOrdered as quantity in 'each' default '1 each' positive editable
- Premise classes: (a)
- Derivation: positive excludes zero from UnitsOrdered' interval -> IntervalContainment (capability tier 1a)
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (the rule-activation-guard evaluation site mints no fault obligation at HEAD at all — the compiler folds the guard into the printed rule preservation WP but raises nothing over the division inside it (observed live below).)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (release build), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` is recorded: the schema's application DU has no locus for a field-declaration modifier addition.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| field-positive | field UnitsOrdered as quantity in 'each' default '1 each' nonnegative editable | nonnegative admits zero; UnitsOrdered != 0 is not implied — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Live-verified at HEAD (commit e1a14d91, 2026-07-21): base, discharge, and near-miss all compile with zero diagnostics beyond the printed WP tables (the rule's own preservation obligation, unrelated to this cell's fault).

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4

