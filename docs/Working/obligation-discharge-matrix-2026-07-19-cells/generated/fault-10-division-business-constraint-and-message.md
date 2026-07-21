<!--
GENERATED FILE — do not hand-edit.
Source: fault-10-division-business-constraint-and-message.cells.json
Regenerate: dotnet run --project tools/Precept.MatrixTools -- render-cells docs/Working/obligation-discharge-matrix-2026-07-19-cells
Hand edits will be overwritten. Edit the .cells.json and regenerate.
-->

# Fault group 10 — business-domain division by zero in constraint conditions and in explanatory messages

Family id: fault-10-division-business-constraint-and-message
Case shape: fault
Definition version: obligation-discharge-matrix-2026-07-19 rev 7 (2026-07-20, draft)

**Where this family's content comes from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge: the fault-family decision-procedure precedent this file's cells follow
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: fault-family row: catalog-declared safety precondition at the evaluation site; premise classes (b)/(c)/(a) per catalog ProofSatisfactions
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the four premise classes (a)-(d); the discharge-contract exactness rule; the decision-procedure requirement
- src/Precept/Language/Operations.cs:442 — code: MoneyDivideDecimal, MoneyDivideMoneySameCurrency, QuantityDivideDecimal, PriceDivideQuantity catalog entries and their NumericProofRequirement divisor subjects
- src/Precept/Language/Modifiers.cs:96 — code: positive/nonnegative/nonzero ProofSatisfaction.Numeric entries
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md — review: Part 3, Defect A and Defect B — the verified HEAD defects this file's hazard flags and mint-gap notes rest on

## Respellability — the family verdict

Can every safe program these rules reject be rewritten into a form they accept? **yes**

Evidence: model-derived
Corpus measurement: pending

**Notes on the family verdict**

- No band member is exhibited in this file: every base witness's divisor is discharged by a declared modifier (nonzero/positive/nonnegative) or a guard/when conjunct restating the same fact — both are the licensed spellings Family 4 already states, so nothing here forces an author into an unprovable spelling. A model-derived yes is recorded rather than asserting corpus agreement; corpus measurement per the ratification protocol is pending, same as every other family in this folder.

**Sources for this verdict**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: Respellable bullet — the sound-but-unprovable band and the yes/no verdict definition

## Premise classes — the legend

Where a proof may get its facts (want :19, :143): (a) field modifiers, (b) arg constraints, (c) the handler's guard, (d) all rules holding in the pre-state (the inductive hypothesis), (e) evaluating the applicable rule set at an editable-field write — (e)'s status as a genuinely fifth class vs (a) generalized is an open owner item.

## g10/computed-field-money-divide-money — Computed field — money ÷ money (same currency), dimensionless ratio

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault-prevention obligation family
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — matrix: fault minting: catalog-stamped at evaluation sites, settled and shipped

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | op/MoneyDivideMoneySameCurrency/numeric-0 |
| evaluation site category | computed-field-expression |
| type family | business-domain |

### What must be proven

Obligation: GrossRevenue != '0.00 USD'

Weakest precondition: GrossRevenue != '0.00 USD'

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| GrossRevenue | the money field occupying the catalog's divisor slot of MoneyDivideMoneySameCurrency |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a).

A computed field's `<-` expression has no handler, no guard, and no event args in scope — it is evaluated over the final configuration in every configuration the entity can occupy (matrix § Per-family case shapes, fault-family row; the site's own read set is the input fields' declared modifiers only). Class (d) is not available here per the group's own scoping (rule-condition, computed-field, quantifier-predicate and rationale sites are free of class (d) — a pre-state constraint about a field is a different kind of fact from a field's own static modifier, and nothing anchors an inductive step at a `<-` expression since it is never a write site).

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: field modifier `nonzero` (or `positive`/`negative`) on GrossRevenue -> interval excludes zero -> IntervalContainment
- Strategy: IntervalContainment
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor's interval from its declared field modifiers (nonzero/positive/negative/min/max — a finite set per site, matrix Family 4). Zero inside the computed interval, or no modifier excluding it, rejects.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4's decision procedure, restated for a field-modifier divisor rather than an arg-modifier one

### What the failing diagnostic must suggest

- For class (a): declare <Divisor> nonzero (or positive/negative) so its interval satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet, adapted to a field-modifier premise


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept MarginComputedBase
field NetRevenue as money in 'USD' default '0.00 USD' editable
field GrossRevenue as money in 'USD' default '0.00 USD' editable
field MarginRatio as decimal <- NetRevenue / GrossRevenue
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools.dll (temp/slice3/bin/Precept.MatrixTools/release), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*nonzero-modifier* — arg-modifier

- Addition: field GrossRevenue as money in 'USD' default '1.00 USD' editable nonzero
- Premise classes: (a)
- Derivation: field modifier nonzero on GrossRevenue -> interval excludes zero -> IntervalContainment
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools.dll (temp/slice3/bin/Precept.MatrixTools/release), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Default raised to '1.00 USD' because a default of '0.00 USD' itself violates the declared nonzero modifier (OutOfRange) — a separate, correctly-firing obligation, not the one this cell tests.
- No `application` locus fits: the schema's application DU covers row-guard and event-arg-declarations only; a field-declaration modifier addition has neither. Applied by hand here; flagged in missingRules as a cell→test conversion gap.
- Strategy name inferred from source reading of ProofEngine.Strategies.cs's dispatch order (TryIntervalContainmentProofNarrowed), not confirmed via CLI output — the CLI tool used for these witnesses prints WP text for rule obligations only, no strategy tag for fault obligations.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| nonzero-modifier | field GrossRevenue as money in 'USD' default '0.00 USD' editable nonnegative | nonnegative admits zero — the interval [0, +inf) still contains the forbidden value, so the same obligation still rejects | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- wp.canonicalKey/canonicalKeySource omitted for every cell in this file: tools/Precept.MatrixTools's WpCalculator computes rule-family establishment/preservation WPs only (Cli.cs — 'Establishment'/'Preservation over the default configuration' sections); it has no fault-family mode, so no mechanized canonical key exists for a fault obligation. Named skip, not silently absent — recorded once here rather than in every cell.
- [dischargeContract premiseClasses=['a']] No validity argument in the matrix's § Validity arguments is written for the fault family; all seven named arguments are scoped to Witness Families 1/2/5 (rule establishment/preservation). 'Arg-bound interval arithmetic' is cited here by mechanism-similarity (governance-enforced modifier truth, exact extraction, business-domain-types.md:426) rather than because the matrix names it for this shape — flagged in missingRules, not invented as a fresh argument.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4
- src/Precept/Language/Operations.cs:452 — code: MoneyDivideMoneySameCurrency catalog entry
- samples/saas-usage-metering-and-billing.precept:36 — code: the corpus's IsFullyPaid computed field shows the same money-arithmetic idiom this cell's setting is drawn from

## g10/computed-field-quantity-divide-decimal — Computed field — quantity ÷ decimal, scaling

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | op/QuantityDivideDecimal/numeric-0 |
| evaluation site category | computed-field-expression |
| type family | primitive |

### What must be proven

Obligation: UnitCount != 0.0

Weakest precondition: UnitCount != 0.0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| UnitCount | the decimal field occupying the catalog's divisor slot of QuantityDivideDecimal |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix

### Which premise classes can discharge it

Applicable classes: (a).

Same reasoning as g10/computed-field-money-divide-money: no handler, no guard, no event args, no premise (d) at a `<-` expression.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: field modifier nonzero on UnitCount -> interval excludes zero -> IntervalContainment
- Strategy: IntervalContainment
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor's interval from its declared field modifiers; zero inside the computed interval, or no modifier excluding it, rejects.

### What the failing diagnostic must suggest

- For class (a): declare <Divisor> nonzero (or positive/negative) so its interval satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept AvgWeightBase
field TotalWeight as quantity in 'kg' default '0 kg' editable
field UnitCount as decimal default 1.0 editable
field AvgWeight as quantity in 'kg' <- TotalWeight / UnitCount
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools.dll (temp/slice3/bin/Precept.MatrixTools/release), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*nonzero-modifier* — arg-modifier

- Addition: field UnitCount as decimal default 1.0 editable nonzero
- Premise classes: (a)
- Derivation: field modifier nonzero on UnitCount -> interval excludes zero -> IntervalContainment
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools.dll (temp/slice3/bin/Precept.MatrixTools/release), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- The declared default 1.0 already satisfies nonzero, so no separate OutOfRange fired here (contrast the money lead cell, which needed the default raised).
- No `application` locus fits (field-declaration modifier addition) — same gap noted on the lead cell.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| nonzero-modifier | field UnitCount as decimal default 1.0 editable nonnegative | nonnegative admits zero — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- [dischargeContract premiseClasses=['a']] Same missing-validity-argument gap as the lead cell: 'Arg-bound interval arithmetic' is the closest written argument, not a fault-family-specific one.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4
- src/Precept/Language/Operations.cs:543 — code: QuantityDivideDecimal catalog entry

## g10/rule-condition-price-divide-quantity — Rule condition — price ÷ compound-quantity, dimension elevation

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | op/PriceDivideQuantity/numeric-0 |
| evaluation site category | rule-condition |
| type family | business-domain |

### What must be proven

Obligation: ConvFactor != 0

Weakest precondition: ConvFactor != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| ConvFactor | the quantity field occupying the catalog's divisor slot of PriceDivideQuantity |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix

### Which premise classes can discharge it

Applicable classes: (a).

A `rule` condition is evaluated against the complete working copy at every checkpoint, in every reachable configuration — not anchored to any handler, so no guard and no event args are in scope (matrix § Per-family case shapes, rule-condition entry in the group's own site sweep). Class (d) is excluded per the group's scoping note (rule-condition sites are free of class (d)).

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: field modifier nonzero on ConvFactor -> interval excludes zero -> IntervalContainment
- Strategy: IntervalContainment
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor's interval from its declared field modifiers; zero inside the computed interval, or no modifier excluding it, rejects.

### What the failing diagnostic must suggest

- For class (a): declare <Divisor> nonzero (or positive/negative) so its interval satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept ElevatedPriceBase
field ListPrice as price in 'USD' of 'mass' default '1.00 USD/kg' editable
field ConvFactor as quantity in 'each/kg' default '1 each/kg' editable
field MaxElevatedPrice as price in 'USD/each' default '100.00 USD/each' editable
rule ListPrice / ConvFactor <= MaxElevatedPrice because "Elevated per-each price capped"
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools.dll (temp/slice3/bin/Precept.MatrixTools/release), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*nonzero-modifier* — arg-modifier

- Addition: field ConvFactor as quantity in 'each/kg' default '1 each/kg' editable nonzero
- Premise classes: (a)
- Derivation: field modifier nonzero on ConvFactor -> interval excludes zero -> IntervalContainment
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools.dll (temp/slice3/bin/Precept.MatrixTools/release), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Confirms the PriceDivideQuantity dimension-elevation shape type-checks as specified (test/Precept.Tests/TypeChecker/PriceDivideCompoundQuantityTests.cs: price[C,X] / quantity[Y/X] -> price[C,Y]) — mass cancels, 'each' from ConvFactor's denominator becomes the result unit, comparable to MaxElevatedPrice's 'USD/each'.
- No `application` locus fits (field-declaration modifier addition).

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| nonzero-modifier | field ConvFactor as quantity in 'each/kg' default '1 each/kg' editable nonnegative | nonnegative admits zero — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- [dischargeContract premiseClasses=['a']] Same missing-validity-argument gap as the other cells in this file.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4
- src/Precept/Language/Operations.cs:691 — code: PriceDivideQuantity catalog entry
- test/Precept.Tests/TypeChecker/PriceDivideCompoundQuantityTests.cs:14 — code: the price/compound-quantity dimension-elevation shape this witness follows

## g10/quantifier-predicate-quantity-divide-decimal — Quantifier predicate — quantity ÷ decimal, hosted in a rule's each-binding

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — matrix
- docs/language/collection-types.md:800 — spec

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | op/QuantityDivideDecimal/numeric-0 |
| evaluation site category | quantifier-predicate |
| type family | primitive |

### What must be proven

Obligation: d != 0

Weakest precondition: d != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| d | the quantifier's binding variable, ranging over Divisors' elements |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix

### Which premise classes can discharge it

Applicable classes: (a).

The quantifier predicate's own premise floor is field modifiers only — the binding variable takes the collection's declared inner type, so an inner-type value modifier bounds the subject (docs/language/collection-types.md:628: 'A quantifier binding ... likewise carries the element bound on x inside the predicate'). Hosted here inside a `rule` condition, which per this file's other rule-condition cell is itself free of guard/args/(d), so no host classes are added.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: inner-type value modifier nonzero on the collection's element type -> the binding variable's interval excludes zero -> IntervalContainment
- Strategy: IntervalContainment
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the binding variable's interval from the collection's declared inner-type value modifier (docs/language/collection-types.md § Element value modifiers); zero inside the computed interval, or no modifier excluding it, rejects.

### What the failing diagnostic must suggest

- For class (a): declare the collection's inner type nonzero (or positive/negative) so the binding variable's interval satisfies <WP>
  - docs/language/collection-types.md § Element value modifiers — spec


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept SplitWeightBase
field TotalWeight as quantity in 'kg' default '0 kg' editable
field Divisors as set of decimal
event AddDivisor(Value as decimal)
on AddDivisor
    -> add Divisors AddDivisor.Value
rule each d in Divisors (TotalWeight / d < '1000 kg') because "each split share must stay under 1000 kg"
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools.dll (temp/slice3/bin/Precept.MatrixTools/release), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*nonzero-element-modifier* — arg-modifier

- Addition: field Divisors as set of decimal nonzero
- Premise classes: (a)
- Derivation: inner-type value modifier nonzero -> binding variable's interval excludes zero -> IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Live-verified: compiling base + this addition still raises DivisionByZero on 'd' at the rule's quantifier predicate. The element-value modifier is accepted syntactically (no diagnostic complains about the declaration itself) but is not consulted by the quantifier fault-check at HEAD. Consistent with docs/language/collection-types.md:10 recording quantifier predicates as 'Not yet built' in the implementation-state table — this is a genuine, currently-open gap between the committed model (docs/language/collection-types.md:628) and the shipped engine, not a false proof and not a minting gap: the site correctly mints the obligation, it simply does not yet consult the one premise class the model says is available.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools.dll (temp/slice3/bin/Precept.MatrixTools/release), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` locus fits (field-declaration modifier addition on a collection field).
- The compiler output also carries an unrelated obligation on this same file — the rule's own PRESERVATION obligation at the 'on AddDivisor' write site (mentioned via the mention-set rule, since Divisors is mentioned in the rule's condition) reports 'NOT SUPPORTED — plan contains a non-set action (Add on Divisors); only scalar set writes are in the single-write calculator's scope' from the WP calculator. That is a different obligation (rule preservation, not this cell's fault obligation) and a named skip of the WP calculator, not a fault-family result; noted for transparency, not folded into this cell's disposition.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| nonzero-element-modifier | field Divisors as set of decimal nonnegative | nonnegative admits zero — still rejects with DivisionByZero (live-verified); same obligation, and consistent either way given the modifier is not yet consulted at HEAD regardless of which one is declared | reject, naming the same obligation |

### Respellability

- Resolution: cell-override
- A representative safe-but-rejected spelling: field Divisors as set of decimal nonzero (the licensed model-derived spelling)

- docs/language/collection-types.md:800 — spec: 'Not yet built' implementation status

- Overridden to no because the gap here is not a spelling limitation an author can work around within the surface (per the model, the nonzero modifier IS the licensed spelling) — it is a build gap in the shipped engine's quantifier fault-check. No respelling exists that discharges this obligation at HEAD today; the family-level yes verdict does not apply to this cell.

- Open items this answer is load-bearing on: Q10
**What the sources leave unstated or ambiguous here**

- Carries the Q10 open item per the authoring notes: whether a quantified constraint (`rule each/any/no ... (...)`) is proof surface at all is open (matrix § Relationship to other work, 'Still gating'). This cell's own obligation — the divisor-safety fault inside the predicate — mints and is answerable regardless of Q10's outcome (fault minting is settled and shipped independently of whether the ENCLOSING quantified rule construct itself is provable as an invariant); Q10 bears on the host construct's own establishment/preservation story, not on this fault cell's discharge contract. Recorded as a carried dependency, not resolved here.
- [dischargeContract premiseClasses=['a']] Same missing-validity-argument gap as the other cells in this file — additionally, this is the one contract entry in the file whose licensed derivation is NOT built at HEAD (see the discharge witness below); the model-derived/built-unresolved split is load-bearing here in a way it is not for the other cells.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Relationship to other work — matrix: Q10 — whether quantified constraints are proof surface at all is still gating
- docs/language/collection-types.md:800 — spec
- src/Precept/Language/Operations.cs:543 — code: QuantityDivideDecimal catalog entry

## g10/rationale-rule-anchored-money-divide-decimal — Constraint rationale — money ÷ decimal, rule-anchored `because` interpolation

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | op/MoneyDivideDecimal/numeric-0 |
| evaluation site category | constraint-rationale-interpolation |
| type family | primitive |

### What must be proven

Obligation: UnitCount != 0.0

Weakest precondition: UnitCount != 0.0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| UnitCount | the decimal field occupying the catalog's divisor slot of MoneyDivideDecimal, read inside a rule's because-interpolation |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix

### Which premise classes can discharge it

Applicable classes: (a).

A rule's `because` interpolation hangs off the constraint, not a row — no event args are in scope on the rule-anchored form (unlike an event-ensure-anchored rationale, which sees the anchoring event's args). Free of class (d) per the group's own scoping (rationale cells are free of (d) throughout this file).

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: field modifier nonzero on UnitCount -> interval excludes zero -> IntervalContainment (per the model; not built at HEAD for this site — see notes)
- Strategy: IntervalContainment
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor's interval from its declared field modifiers; zero inside the computed interval, or no modifier excluding it, rejects.

### What the failing diagnostic must suggest

- For class (a): declare <Divisor> nonzero (or positive/negative) so its interval satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept AvgRationaleRuleBase
field TotalRevenue as money in 'USD' default '0.00 USD' editable
field UnitCount as decimal default 1.0 editable
rule TotalRevenue >= '0.00 USD' because "Average per unit {TotalRevenue / UnitCount} must stay non-negative"
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools.dll (temp/slice3/bin/Precept.MatrixTools/release), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*nonzero-modifier* — arg-modifier

- Addition: field UnitCount as decimal default 1.0 editable nonzero
- Premise classes: (a)
- Derivation: field modifier nonzero on UnitCount -> interval excludes zero -> IntervalContainment (model-committed derivation)
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Live-verified: this exact rationale interpolation ('Average per unit {TotalRevenue / UnitCount}...') mints NO diagnostic at HEAD regardless of the divisor's modifiers — base, this discharge, and the near-miss below all compile with zero diagnostics. This is a live minting gap, not this discharge failing: the rule's CONDITION (TotalRevenue >= '0.00 USD') carries no division and compiles clean as expected; the division lives only in the because-text, and the constraint-rationale-interpolation evaluation site does not raise DivisionByZero at HEAD at all. See matrix § The cell, 'Open design hole — nothing detects an obligation that was never minted' (lines 88-101) for the general shape of this failure mode, and authored-expressiveness-gaps.md's own measurement of the identical shape (b.precept: 'rule Total > 0.0 because "Per-part {Total / Parts} must stay positive"' compiled with no fault diagnostic while the same division in the condition raised DivisionByZero).)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools.dll (temp/slice3/bin/Precept.MatrixTools/release), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` locus fits (field-declaration modifier addition).
- Because the site never mints, this 'discharge' and the base are compiler-indistinguishable today — both simply compile clean. The distinction recorded here is entirely modelStatus/builtStatus, per the matrix's own instruction that a live-verified mark beside builtStatus unresolved-today is a legitimate and useful record.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| nonzero-modifier | field UnitCount as decimal default 1.0 editable nonnegative | Under the model this must still reject (nonnegative admits zero, same obligation). At HEAD it does not reject — nothing does, because the site never mints (see the discharge's builtStatusNote). Recorded as the model's committed near-miss outcome; the built behaviour is the same live gap, not a separate finding. | reject, naming the same obligation |

### Respellability

- Resolution: cell-override

- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md — review: the minting-gap measurement this override rests on

- Overridden to no for the same reason as the quantifier-predicate cell: there is no author-writable spelling that gets this obligation checked at HEAD today, because the site does not mint at all — not a surface-expressiveness question the family verdict is meant to answer.

**What the sources leave unstated or ambiguous here**

- [dischargeContract premiseClasses=['a']] Same missing-validity-argument gap as the other cells in this file. Additionally: this decision procedure cannot be exercised at HEAD at all today because the site mints no obligation to decide (see witness notes) — the contract states the model's committed answer, not a built check.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4, adapted to the interpolation site
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md — review: the b.precept measurement of the identical shape on a primitive decimal field
- src/Precept/Language/Operations.cs:442 — code: MoneyDivideDecimal catalog entry

## g10/rationale-event-ensure-anchored-money-divide-money — Constraint rationale — money ÷ money, event-ensure-anchored `because` interpolation

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | op/MoneyDivideMoneySameCurrency/numeric-0 |
| evaluation site category | constraint-rationale-interpolation |
| type family | business-domain |

### What must be proven

Obligation: Adjust.Amount != '0.00 USD'

Weakest precondition: Adjust.Amount != '0.00 USD'

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Adjust | the event anchoring the ensure |
| Adjust.Amount | the event arg occupying the catalog's divisor slot of MoneyDivideMoneySameCurrency, read inside the ensure's because-interpolation |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix

### Which premise classes can discharge it

Applicable classes: (b).

Unlike the rule-anchored rationale cell, an event-ensure's because-interpolation sees the anchoring event's args, so class (b) is available. Class (a) does not apply here because the divisor is the event arg itself, not a field. Class (c) would become available if the anchoring event ensure carried its own optional `when` guard restating the divisor fact (grammar permits `on E when G ensure BoolExpr`) — not exercised in this witness, so not counted as applicable for this specific cell. Free of class (d), consistent with rationale cells throughout this file.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (b)**

- Derivation: arg modifier nonzero on Adjust.Amount -> interval excludes zero -> IntervalContainment (per the model; not built at HEAD for this site — see notes)
- Strategy: IntervalContainment
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor's interval from its declared arg modifiers; zero inside the computed interval, or no modifier excluding it, rejects.

### What the failing diagnostic must suggest

- For class (b): declare <Divisor> nonzero (or positive/negative) on the event arg so its interval satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept RationaleEventEnsureBase
field TotalRevenue as money in 'USD' default '0.00 USD' editable
event Adjust(Amount as money in 'USD')
on Adjust ensure Adjust.Amount > '0.00 USD' because "Adjustment ratio {TotalRevenue / Adjust.Amount} recorded"
```

Required outcome: reject, naming the missing premise classes (b), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools.dll (temp/slice3/bin/Precept.MatrixTools/release), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*nonzero-arg-modifier* — arg-modifier

- Addition: Amount as money in 'USD' nonzero
- Premise classes: (b)
- Derivation: arg modifier nonzero on Adjust.Amount -> interval excludes zero -> IntervalContainment (model-committed derivation)
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Live-verified: base, this discharge, and the near-miss below all compile with zero diagnostics — the ensure's own condition (Adjust.Amount > '0.00 USD') already type-checks and carries no division, and the because-interpolation's division mints nothing at HEAD regardless of the arg's modifiers. Same live minting gap as the rule-anchored rationale cell, now confirmed on a business-domain (money/money) shape rather than a primitive one.)
- How it applies to the base: applied by replacing the named event-arg declarations
  - `Adjust.Amount` becomes `Amount as money in 'USD' nonzero`

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools.dll (temp/slice3/bin/Precept.MatrixTools/release), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| nonzero-arg-modifier | Amount as money in 'USD' nonnegative | Under the model this must still reject (nonnegative admits zero). At HEAD nothing rejects here either way, per the same minting gap. | reject, naming the same obligation |

### Respellability

- Resolution: cell-override

- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md — review

- Same override reason as the rule-anchored rationale cell: the gap is a minting gap, not a surface-expressiveness question.

**What the sources leave unstated or ambiguous here**

- [dischargeContract premiseClasses=['b']] Same missing-validity-argument gap as the other cells in this file. Same not-built-at-HEAD caveat as the rule-anchored rationale cell — see witness notes.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4, adapted to the interpolation site
- src/Precept/Language/Operations.cs:452 — code: MoneyDivideMoneySameCurrency catalog entry
- docs/language/precept-language-spec.md:1879 — spec: on E when G ensure BoolExpr grammar, cited for the (c)-availability note even though not exercised in this witness

## g10/state-ensure-money-divide-decimal — State-anchored ensure — money ÷ decimal, primitive-lane divisor

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | op/MoneyDivideDecimal/numeric-0 |
| evaluation site category | state-ensure-condition |
| type family | primitive |

### What must be proven

Obligation: UnitCount != 0.0

Weakest precondition: UnitCount != 0.0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| UnitCount | the decimal field occupying the catalog's divisor slot of MoneyDivideDecimal, read inside an in-S-ensure condition |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix

### Which premise classes can discharge it

Applicable classes: (a), (c), (d).

A state-anchored ensure's condition sees field names and the ensure's own optional pre-verb `when` (class (c)); it does not see event args (spec's scope table gives ensure conditions field names only). Because the divisor here IS the field the ensure's own condition reads, all three of (a) a direct field modifier, (c) a when-guard restating the divisor fact, and (d) a pre-state rule about the same field are live candidates for discharging this specific obligation.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: field modifier nonzero on UnitCount -> interval excludes zero -> IntervalContainment
- Strategy: IntervalContainment
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor's interval from its declared field modifiers and the ensure's in-scope when-guard conjuncts (a finite set per site, matrix Family 4); zero inside the computed interval, or no bound excluding it, rejects.

**Entry 2 — (c)**

- Derivation: a when-guard on the ensure restating UnitCount != 0.0 (or a stronger positive/negative bound) -> the same interval computation as class (a), fed from the guard conjunct instead of the field modifier
- Validity arguments: Guard normal-form match
- Decision procedure: Same decision procedure as the (a) entry; not separately witnessed in this cell (model-derived only) — the mechanism is identical to the class-(a) case with the fact sourced from the ensure's own when-guard rather than the field's declared modifier.

**Entry 3 — (d)**

- Derivation: a `rule` mentioning UnitCount (e.g. `rule UnitCount > 0.0`) consumed as a pre-state constraint fact via the CompositionalConstraint strategy
- Strategy: CompositionalConstraint
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Consult the field's currently-declared rule conjuncts mentioning the divisor as an additional interval fact, using the same interval combination as class (a).

- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md — review: Part 3, Defect B — the verified false-proof hazard this entry's witness is flagged against

### What the failing diagnostic must suggest

- For class (a): declare <Divisor> nonzero (or positive/negative) so its interval satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix

- For class (c): add a when-guard on the ensure normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept StateEnsureAvgBase
field TotalRevenue as money in 'USD' default '0.00 USD' editable
field UnitCount as decimal default 1.0 editable
state Draft initial
state Closed terminal
event Open initial
event Close
on Open
  -> set TotalRevenue = '0.00 USD'
in Closed ensure TotalRevenue / UnitCount >= '0.00 USD' because "Average per unit must stay non-negative while closed"
from Draft on Close
  -> transition Closed
```

Required outcome: reject, naming the missing premise classes (a), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools.dll (temp/slice3/bin/Precept.MatrixTools/release), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*nonzero-modifier* — arg-modifier

- Addition: field UnitCount as decimal default 1.0 editable nonzero
- Premise classes: (a)
- Derivation: field modifier nonzero on UnitCount -> interval excludes zero -> IntervalContainment
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools.dll (temp/slice3/bin/Precept.MatrixTools/release), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` locus fits (field-declaration modifier addition).

*rule-premise-hazard* — other

- Addition: rule UnitCount > 0.0 because "unit count must stay positive"
- Premise classes: (d)
- Derivation: rule UnitCount > 0.0 consumed as a pre-state fact via CompositionalConstraint
- Strategy: CompositionalConstraint
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Live-verified: base + this addition compiles with zero diagnostics — the compiler treats the declared rule as sufficient to discharge the divisor-safety obligation. Per the owner's ruling for this slice, this is recorded as unresolved-today (not proven-today) despite the clean compile: nothing in the file establishes or preserves 'UnitCount > 0.0' as an invariant (Defect A — a `rule` mints no write-site obligation at HEAD), so the discharge is a false proof in exactly the shape authored-expressiveness-gaps.md Part 3 Defect B demonstrates. A reachable violation exists in principle (any handler setting UnitCount to a non-positive value while the rule text remains unchanged would go uncaught), though no such handler is included in this minimal witness — the point being tested is the fail-open acceptance itself, not a further reachability chain.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools.dll (temp/slice3/bin/Precept.MatrixTools/release), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` locus fits (a top-level rule addition; the schema's application DU has no locus for adding a new top-level declaration).
- Listed in unverifiableWitnesses per instruction.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| nonzero-modifier | field UnitCount as decimal default 1.0 editable nonnegative | nonnegative admits zero — must still reject, same obligation | reject, naming the same obligation |
| rule-premise-hazard | rule UnitCount >= 0.0 because "unit count must stay non-negative" | Live-verified: this weakened rule (allows zero) still correctly rejects with DivisionByZero — the CompositionalConstraint strategy does read the rule's actual comparison operator rather than merely detecting that some rule mentions the field. This shows the unsoundness is specifically 'a positivity-stating rule is trusted without establishment/preservation checking', not 'any rule mentioning the field is treated as sufficient regardless of content'. | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

- Applies to the (a)/(c) portion of this cell's contract. The (d) rule-premise entry is a hazard, not a respellability question — no respelling is recommended for it (see the dischargeContract entry's notes).

- Open items this answer is load-bearing on: S4
**What the sources leave unstated or ambiguous here**

- [dischargeContract premiseClasses=['a']] Same missing-validity-argument gap as the other cells in this file.
- [dischargeContract premiseClasses=['c']] Model-derived, no separate compile recorded for this exact shape in this cell; the mechanism is exercised live for the event-ensure-condition sibling cell's guard discharge (g10/event-ensure-money-divide-decimal), which is structurally the same 'guard conjunct restates the divisor fact' derivation.
- [dischargeContract premiseClasses=['d']] HAZARD, per the owner's ruling for this slice: this discharge is consumed through a `rule` used as a premise. Slice 2 verified at HEAD that this path can be a false proof — a `rule` mints no preservation obligation of its own (Defect A), so the CompositionalConstraint strategy consumes an unestablished, unpreserved fact. The witness below records that the compile clean-passes, and separately records builtStatus as unresolved-today with the reason named, per instruction — a clean compile here is NOT confirmation the discharge is sound.
- [dischargeContract premiseClasses=['d']] 'Inductive hypothesis plus sign monotonicity' is cited as the model's stated intent for this shape (matching Witness Family 1 Base B's own citation of the identical argument for a (d)+(b) case), not as an endorsement that the argument's own precondition currently holds: the argument's text requires 'every handler carries a preservation obligation' (matrix § Validity arguments), which Defect A shows is false for a bare `rule` construct at HEAD today.
- Carried per the authoring notes: the matrix records as open whether the residency fact itself — that the entity is in state Closed — is available as a premise, and under which class (docs/Working/obligation-discharge-matrix-2026-07-19.md:150, the in-S-ensure case-shape row). This cell's own discharges (field modifier, guard, rule-premise) do not consume the residency fact, so this cell's answers do not depend on that open item resolving either way — but the item is carried here because the authoring instructions name state-ensure cells as its landing site. No Q/S identifier is assigned to this open item in the matrix text (unlike Q7/Q10/Q13/Q14a, which are all numbered); that absence is itself worth surfacing rather than assigning one here.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Inductive hypothesis plus sign monotonicity, and its open dependency naming S4 (the mention-set completeness the hypothesis rests on)
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md — review: Part 3, Defect A and Defect B
- src/Precept/Language/Operations.cs:442 — code: MoneyDivideDecimal catalog entry

## g10/state-ensure-money-divide-money — State-anchored ensure — money ÷ money, business-domain-lane divisor

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | op/MoneyDivideMoneySameCurrency/numeric-0 |
| evaluation site category | state-ensure-condition |
| type family | business-domain |

### What must be proven

Obligation: GrossRevenue != '0.00 USD'

Weakest precondition: GrossRevenue != '0.00 USD'

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| GrossRevenue | the money field occupying the catalog's divisor slot of MoneyDivideMoneySameCurrency, read inside an in-S-ensure condition |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix

### Which premise classes can discharge it

Applicable classes: (a), (c), (d).

Same reasoning as g10/state-ensure-money-divide-decimal, on the business-domain lane: the divisor is the field the ensure's own condition reads, so (a)/(c)/(d) are all live candidates; no event args are in scope.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: field modifier nonzero on GrossRevenue -> interval excludes zero -> IntervalContainment
- Strategy: IntervalContainment
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor's interval from its declared field modifiers and the ensure's in-scope when-guard conjuncts; zero inside the computed interval, or no bound excluding it, rejects.

**Entry 2 — (c)**

- Derivation: a when-guard on the ensure restating GrossRevenue != '0.00 USD' -> same interval computation as class (a)
- Validity arguments: Guard normal-form match
- Decision procedure: Same decision procedure as the (a) entry; not separately witnessed in this cell (model-derived only).

**Entry 3 — (d)**

- Derivation: a `rule` mentioning GrossRevenue consumed as a pre-state constraint fact via CompositionalConstraint
- Strategy: CompositionalConstraint
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Consult the field's currently-declared rule conjuncts mentioning the divisor as an additional interval fact, using the same interval combination as class (a).

- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md — review: Part 3, Defect B

### What the failing diagnostic must suggest

- For class (a): declare <Divisor> nonzero (or positive/negative) so its interval satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix

- For class (c): add a when-guard on the ensure normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept StateEnsureRatioBase
field NetRevenue as money in 'USD' default '0.00 USD' editable
field GrossRevenue as money in 'USD' default '0.00 USD' editable
state Draft initial
state Closed terminal
event Open initial
event Close
on Open
  -> set NetRevenue = '0.00 USD'
in Closed ensure NetRevenue / GrossRevenue >= 0.0 because "Margin ratio must stay non-negative while closed"
from Draft on Close
  -> transition Closed
```

Required outcome: reject, naming the missing premise classes (a), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools.dll (temp/slice3/bin/Precept.MatrixTools/release), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*nonzero-modifier* — arg-modifier

- Addition: field GrossRevenue as money in 'USD' default '1.00 USD' editable nonzero
- Premise classes: (a)
- Derivation: field modifier nonzero on GrossRevenue -> interval excludes zero -> IntervalContainment
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools.dll (temp/slice3/bin/Precept.MatrixTools/release), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Default raised to '1.00 USD' for the same OutOfRange reason as the computed-field lead cell.
- No `application` locus fits (field-declaration modifier addition).

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| nonzero-modifier | field GrossRevenue as money in 'USD' default '0.00 USD' editable nonnegative | nonnegative admits zero — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

- Same scoping as the primitive-lane sibling cell: applies to the (a)/(c) portion only; the (d) entry is a hazard, not a respellability question.

- Open items this answer is load-bearing on: S4
**What the sources leave unstated or ambiguous here**

- [dischargeContract premiseClasses=['a']] Same missing-validity-argument gap as the other cells in this file.
- [dischargeContract premiseClasses=['c']] Model-derived; see the primitive-lane sibling cell's note for where this mechanism is exercised live.
- [dischargeContract premiseClasses=['d']] Same hazard as the primitive-lane sibling cell (g10/state-ensure-money-divide-decimal). Not separately live-witnessed here to avoid redundant compiles — the hazard is reproduced once, at that cell, and applies analogously to this business-domain lane by the same mechanism (a bare `rule` mints no preservation obligation regardless of the field's type family).
- Carries the same residency-fact open item as the primitive-lane sibling cell (docs/Working/obligation-discharge-matrix-2026-07-19.md:150) — see that cell's note for the full statement; not repeated in full here to avoid duplication.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4
- src/Precept/Language/Operations.cs:452 — code: MoneyDivideMoneySameCurrency catalog entry

## g10/event-ensure-money-divide-decimal — Event ensure — money ÷ decimal, primitive-lane divisor, ingress door

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | op/MoneyDivideDecimal/numeric-0 |
| evaluation site category | event-ensure-condition |
| type family | primitive |

### What must be proven

Obligation: Finalize.Divisor != 0.0

Weakest precondition: Finalize.Divisor != 0.0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Finalize | the event this ensure anchors |
| Finalize.Divisor | the event arg occupying the catalog's divisor slot of MoneyDivideDecimal |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix

### Which premise classes can discharge it

Applicable classes: (b), (c).

The divisor here is Finalize's own event arg, not a field, so class (a) (a field modifier) does not apply to it, and class (d) (a pre-state rule about a field) does not apply either — a rule cannot hold of a value that does not exist until the event fires. The two live classes are (b) an arg modifier on Divisor, and (c) the ensure's own optional when-guard restating the divisor fact. This is the event door the matrix names as making premise (b) true in the first place (matrix § Vocabulary, Discharge mechanisms bullet; § Per-family case shapes, on-E-ensure row) — the ensure's condition runs after ingress governance has already validated the args, which is why an arg modifier is a trustworthy premise here.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (b)**

- Derivation: arg modifier nonzero on Finalize.Divisor -> interval excludes zero -> IntervalContainment
- Strategy: IntervalContainment
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor's interval from its declared arg modifiers and the ensure's in-scope when-guard conjuncts; zero inside the computed interval, or no bound excluding it, rejects.

**Entry 2 — (c)**

- Derivation: a when-guard on the ensure restating Finalize.Divisor != 0.0 -> same interval computation, fed from the guard conjunct instead of the arg modifier
- Validity arguments: Guard normal-form match
- Decision procedure: Same decision procedure as the (b) entry.

- docs/language/precept-language-spec.md:1879 — spec: on E when G ensure BoolExpr grammar

### What the failing diagnostic must suggest

- For class (b): declare <Divisor> nonzero (or positive/negative) on the event arg so its interval satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix

- For class (c): add a when-guard on the ensure normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept EventEnsureAvgBase
field TotalRevenue as money in 'USD' default '0.00 USD' editable
event Finalize(Divisor as decimal)
on Finalize ensure TotalRevenue / Finalize.Divisor >= '0.00 USD' because "average must stay non-negative at finalize"
```

Required outcome: reject, naming the missing premise classes (b), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools.dll (temp/slice3/bin/Precept.MatrixTools/release), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*nonzero-arg-modifier* — arg-modifier

- Addition: Divisor as decimal nonzero
- Premise classes: (b)
- Derivation: arg modifier nonzero on Finalize.Divisor -> interval excludes zero -> IntervalContainment
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: applied by replacing the named event-arg declarations
  - `Finalize.Divisor` becomes `Divisor as decimal nonzero`

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools.dll (temp/slice3/bin/Precept.MatrixTools/release), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

*guard-nf* — guard

- Addition: when Finalize.Divisor != 0.0
- Premise classes: (c)
- Derivation: when-guard on the ensure restating Finalize.Divisor != 0.0 -> same interval computation, guard-sourced
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: appended to the guard of the `on Finalize` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools.dll (temp/slice3/bin/Precept.MatrixTools/release), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- The `row-guard` application locus is used loosely here: the schema's own description ('the addition text is inserted as the row's when clause') fits an `on E when G ensure ...` hook as well as a transition row, since both are keyed by eventName and both admit exactly one when clause. Flagged in case the intended reading is transition-rows-only.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| nonzero-arg-modifier | Divisor as decimal nonnegative | nonnegative admits zero — must still reject, same obligation | reject, naming the same obligation |
| guard-nf | when Finalize.Divisor >= 0.0 | guard admits zero — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- [dischargeContract premiseClasses=['b']] Same missing-validity-argument gap as the other cells in this file.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: Discharge mechanisms bullet — ingress evaluation is what makes premise (b) true at the event door
- src/Precept/Language/Operations.cs:442 — code: MoneyDivideDecimal catalog entry

## g10/event-ensure-price-divide-quantity — Event ensure — price ÷ compound-quantity, business-domain-lane divisor

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | op/PriceDivideQuantity/numeric-0 |
| evaluation site category | event-ensure-condition |
| type family | business-domain |

### What must be proven

Obligation: Convert.Factor != 0

Weakest precondition: Convert.Factor != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Convert | the event this ensure anchors |
| Convert.Factor | the event arg occupying the catalog's divisor slot of PriceDivideQuantity |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix

### Which premise classes can discharge it

Applicable classes: (b), (c).

Same reasoning as g10/event-ensure-money-divide-decimal: the divisor is the anchoring event's own arg, so (a)/(d) do not apply; (b) and (c) are the live classes.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (b)**

- Derivation: arg modifier nonzero on Convert.Factor -> interval excludes zero -> IntervalContainment
- Strategy: IntervalContainment
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor's interval from its declared arg modifiers and the ensure's in-scope when-guard conjuncts; zero inside the computed interval, or no bound excluding it, rejects.

**Entry 2 — (c)**

- Derivation: a when-guard on the ensure restating Convert.Factor != 0 -> same interval computation, guard-sourced
- Validity arguments: Guard normal-form match
- Decision procedure: Same decision procedure as the (b) entry.

### What the failing diagnostic must suggest

- For class (b): declare <Divisor> nonzero (or positive/negative) on the event arg so its interval satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept EventEnsureElevatedBase
field ListPrice as price in 'USD' of 'mass' default '1.00 USD/kg' editable
field MaxElevatedPrice as price in 'USD/each' default '100.00 USD/each' editable
event Convert(Factor as quantity in 'each/kg')
on Convert ensure ListPrice / Convert.Factor <= MaxElevatedPrice because "elevated per-each price must stay capped at convert"
```

Required outcome: reject, naming the missing premise classes (b), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools.dll (temp/slice3/bin/Precept.MatrixTools/release), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*nonzero-arg-modifier* — arg-modifier

- Addition: Factor as quantity in 'each/kg' nonzero
- Premise classes: (b)
- Derivation: arg modifier nonzero on Convert.Factor -> interval excludes zero -> IntervalContainment
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: applied by replacing the named event-arg declarations
  - `Convert.Factor` becomes `Factor as quantity in 'each/kg' nonzero`

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools.dll (temp/slice3/bin/Precept.MatrixTools/release), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| nonzero-arg-modifier | Factor as quantity in 'each/kg' nonnegative | nonnegative admits zero — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- [dischargeContract premiseClasses=['b']] Same missing-validity-argument gap as the other cells in this file.
- [dischargeContract premiseClasses=['c']] Model-derived; not separately witnessed in this cell — the identical mechanism is live-witnessed on the primitive-lane sibling cell (g10/event-ensure-money-divide-decimal).

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4
- src/Precept/Language/Operations.cs:691 — code: PriceDivideQuantity catalog entry

## g10/reject-message-money-divide-decimal — Reject message — money ÷ decimal, primitive-lane divisor

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:72 — want-doc: the want doc's own example of a reject-row interpolation carrying arithmetic ({-Balance / PlanRepayment.Months})
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | op/MoneyDivideDecimal/numeric-0 |
| evaluation site category | reject-message-interpolation |
| type family | primitive |

### What must be proven

Obligation: UnitCount != 0.0

Weakest precondition: UnitCount != 0.0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| UnitCount | the decimal field occupying the catalog's divisor slot of MoneyDivideDecimal, read inside a reject row's message interpolation |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix

### Which premise classes can discharge it

Applicable classes: (a), (c), (d).

The reject row's own guard, if any, and the triggering event's args are in scope (matrix § Per-family case shapes, fault-family row; want :72 names this exact site as the reason the fault axis cannot reuse the write-site axis). In this witness the divisor is a field (not the triggering event's arg), so (a) a field modifier and (d) a pre-state rule are the field-facing classes, and (c) the row's own guard is available structurally (no guard is declared in this witness, so it is listed as applicable-but-unexercised, matching the site's general availability). No (b) here because Note carries no args in this witness.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: field modifier nonzero on UnitCount -> interval excludes zero -> IntervalContainment (per the model; not built at HEAD for this site — see notes)
- Strategy: IntervalContainment
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor's interval from its declared field modifiers and the row's in-scope guard conjuncts; zero inside the computed interval, or no bound excluding it, rejects.

**Entry 2 — (d)**

- Derivation: a `rule` mentioning UnitCount consumed as a pre-state constraint fact via CompositionalConstraint
- Strategy: CompositionalConstraint
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Consult the field's currently-declared rule conjuncts mentioning the divisor as an additional interval fact, using the same interval combination as class (a).

- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md — review: Part 3, Defect B

### What the failing diagnostic must suggest

- For class (a): declare <Divisor> nonzero (or positive/negative) so its interval satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept RejectMessageAvgBase
field TotalRevenue as money in 'USD' default '0.00 USD' editable
field UnitCount as decimal default 1.0 editable
state Draft initial
state Done terminal
event Open initial
event Note
event Finish
on Open
  -> set TotalRevenue = '0.00 USD'
from Draft on Note
  -> reject "average per unit is {TotalRevenue / UnitCount}"
from Draft on Finish
  -> transition Done
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools.dll (temp/slice3/bin/Precept.MatrixTools/release), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*nonzero-modifier* — arg-modifier

- Addition: field UnitCount as decimal default 1.0 editable nonzero
- Premise classes: (a)
- Derivation: field modifier nonzero on UnitCount -> interval excludes zero -> IntervalContainment (model-committed derivation)
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Live-verified: base, this discharge, and the near-miss below all compile with only a pre-existing 'StateAlwaysRejects'/'AlwaysRejecting' structural warning (Note has a single unconditional reject row — an artifact of this minimal witness's shape, present identically in the group's own rj.precept measurement, not related to this obligation) and NO DivisionByZero at all, regardless of the divisor's modifiers. This is the live minting gap the want doc's own example site (want :72) turns out to have at HEAD: the reject-row interpolation position mints nothing today. See authored-expressiveness-gaps.md's identical measurement (rj.precept: `-> reject "per part {Total / Parts}"` with unconstrained Parts compiled clean) and matrix § The cell, 'Open design hole — nothing detects an obligation that was never minted.')

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools.dll (temp/slice3/bin/Precept.MatrixTools/release), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` locus fits (field-declaration modifier addition).

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| nonzero-modifier | field UnitCount as decimal default 1.0 editable nonnegative | Under the model this must still reject. At HEAD nothing rejects either way, per the same minting gap. | reject, naming the same obligation |

### Respellability

- Resolution: cell-override

- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md — review

- Same minting-gap override reason as the rationale cells.

**What the sources leave unstated or ambiguous here**

- [dischargeContract premiseClasses=['a']] Same missing-validity-argument gap as the other cells in this file. This decision procedure cannot be exercised at HEAD today because the site mints no obligation to decide (see witness notes).
- [dischargeContract premiseClasses=['d']] HAZARD, per the owner's ruling for this slice. Not separately live-witnessed in this cell (the site mints nothing at all regardless, so a rule-premise witness here would be indistinguishable from the modifier witness — both simply compile clean for the same reason: the minting gap, not a strategy discharge). The hazard's live reproduction lives at g10/state-ensure-money-divide-decimal; here the more load-bearing finding is the minting gap itself (see witness notes), which subsumes the hazard question for this cell — there is no obligation for a false proof to attach to.

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:72 — want-doc
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md — review: rj.precept — the identical measurement on a primitive shape

## g10/reject-message-money-divide-money — Reject message — money ÷ money, business-domain-lane divisor

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:72 — want-doc
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | op/MoneyDivideMoneySameCurrency/numeric-0 |
| evaluation site category | reject-message-interpolation |
| type family | business-domain |

### What must be proven

Obligation: GrossRevenue != '0.00 USD'

Weakest precondition: GrossRevenue != '0.00 USD'

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| GrossRevenue | the money field occupying the catalog's divisor slot of MoneyDivideMoneySameCurrency, read inside a reject row's message interpolation |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix

### Which premise classes can discharge it

Applicable classes: (a), (c), (d).

Same reasoning as g10/reject-message-money-divide-decimal, on the business-domain lane.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: field modifier nonzero on GrossRevenue -> interval excludes zero -> IntervalContainment (per the model; not built at HEAD for this site)
- Strategy: IntervalContainment
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor's interval from its declared field modifiers and the row's in-scope guard conjuncts; zero inside the computed interval, or no bound excluding it, rejects.

**Entry 2 — (d)**

- Derivation: a `rule` mentioning GrossRevenue consumed as a pre-state constraint fact via CompositionalConstraint
- Strategy: CompositionalConstraint
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Consult the field's currently-declared rule conjuncts mentioning the divisor as an additional interval fact.

- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md — review: Part 3, Defect B

### What the failing diagnostic must suggest

- For class (a): declare <Divisor> nonzero (or positive/negative) so its interval satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept RejectMessageRatioBase
field NetRevenue as money in 'USD' default '0.00 USD' editable
field GrossRevenue as money in 'USD' default '0.00 USD' editable
state Draft initial
state Done terminal
event Open initial
event Note
event Finish
on Open
  -> set NetRevenue = '0.00 USD'
from Draft on Note
  -> reject "margin ratio is {NetRevenue / GrossRevenue}"
from Draft on Finish
  -> transition Done
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools.dll (temp/slice3/bin/Precept.MatrixTools/release), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*nonzero-modifier* — arg-modifier

- Addition: field GrossRevenue as money in 'USD' default '1.00 USD' editable nonzero
- Premise classes: (a)
- Derivation: field modifier nonzero on GrossRevenue -> interval excludes zero -> IntervalContainment (model-committed derivation)
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Live-verified: base, this discharge, and the near-miss below all compile with only the same pre-existing structural warning noted on the primitive-lane sibling cell, and NO DivisionByZero at all. Same live minting gap, confirmed here on the business-domain (money/money) lane.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools.dll (temp/slice3/bin/Precept.MatrixTools/release), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` locus fits (field-declaration modifier addition).

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| nonzero-modifier | field GrossRevenue as money in 'USD' default '0.00 USD' editable nonnegative | Under the model this must still reject. At HEAD nothing rejects either way, per the same minting gap. | reject, naming the same obligation |

### Respellability

- Resolution: cell-override

- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md — review

**What the sources leave unstated or ambiguous here**

- [dischargeContract premiseClasses=['a']] Same missing-validity-argument gap as the other cells in this file.
- [dischargeContract premiseClasses=['d']] Same hazard and same not-separately-witnessed reasoning as the primitive-lane sibling cell.

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:72 — want-doc
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4
- src/Precept/Language/Operations.cs:452 — code: MoneyDivideMoneySameCurrency catalog entry

## Fields with no rendering rule

Data the generator has no rendering rule for, surfaced verbatim rather than dropped. Each is either a schema addition the generator has not caught up with, or a stray field.

| Where | Field | Value |
|---|---|---|
| cells[g10/quantifier-predicate-quantity-divide-decimal].respellability | overrideVerdict | "no" |
| cells[g10/rationale-rule-anchored-money-divide-decimal].respellability | overrideVerdict | "no" |
| cells[g10/rationale-event-ensure-anchored-money-divide-money].respellability | overrideVerdict | "no" |
| cells[g10/reject-message-money-divide-decimal].respellability | overrideVerdict | "no" |
| cells[g10/reject-message-money-divide-money].respellability | overrideVerdict | "no" |

