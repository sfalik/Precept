<!--
GENERATED FILE — do not hand-edit.
Source: fault-11-division-business-declaration-positions.cells.json
Regenerate: dotnet run --project tools/Precept.MatrixTools -- render-cells docs/Working/obligation-discharge-matrix-2026-07-19-cells
Hand edits will be overwritten. Edit the .cells.json and regenerate.
-->

# Fault Family — business-domain division by zero in declaration-position value expressions

Family id: fault-11-division-business-declaration-positions
Case shape: fault
Definition version: obligation-discharge-matrix-2026-07-19 rev 7 (2026-07-20, draft)

**Where this family's content comes from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the fault case-shape row: catalog-declared safety precondition at the evaluation site
- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — fault family, arg-constraint discharge, the decision procedure this file instantiates with class (a)
- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault prevention obligation family
- docs/language/precept-language-spec.md § Expression scope — spec: the per-position scope rules this file's five declaration-position categories are read off
- src/Precept/Language/Operations.cs:442 — code: MoneyDivideDecimal catalog entry
- src/Precept/Language/Operations.cs:543 — code: QuantityDivideDecimal catalog entry

## Respellability — the family verdict

Can every safe program these rules reject be rewritten into a form they accept? **yes**

Evidence: model-derived
Corpus measurement: pending

Spellings that look rejected but are in fact licensed (kept so the boundary stays findable):

- `field BillingCycleDays as decimal min 0.01 default 1 (an explicit numeric lower bound excluding 0, spelled with `min` instead of `positive`)` — Family 4's decision procedure computes the divisor's interval from its declared modifiers generically; a `min` bound whose value excludes 0 is the same interval-exclusion fact as `positive`/`nonzero`, discharged by the identical derivation. Licensed, not banded.
  - docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: decision procedure over 'declared modifiers' generically


**Notes on the family verdict**

- Every cell in this file inherits this verdict; none states an override.
- A genuine band member: `rule BillingCycleDays > 0` written as a separate `rule` construct instead of the `positive`/`nonzero` field modifier. The divisor is sound (the rule, if established and preserved elsewhere, guarantees non-zero) but unprovable at this declaration site under the written premise-class restriction — none of these five evaluation-site categories has class (d) (pre-state constraint facts) available, so a `rule` cannot be consumed as a premise here even though it is a real, standing fact about the field. Respelling: use the field modifier directly (`positive`/`nonzero`/an excluding `min`) — spec §2.4 states the modifier and the equivalent rule are interchangeable to the proof engine in every position where either is consumable at all, so no expressiveness is lost, only the choice of surface form.

## Premise classes — the legend

Where a proof may get its facts (want :19, :143): (a) field modifiers, (b) arg constraints, (c) the handler's guard, (d) all rules holding in the pre-state (the inductive hypothesis), (e) evaluating the applicable rule set at an editable-field write — (e)'s status as a genuinely fifth class vs (a) generalized is an open owner item.

## g11/field-default-money — Field default value expression — MoneyDivideDecimal

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § The cell — matrix: item 1 — fault-family obligation schema is the catalog-declared safety precondition at the evaluation site
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — an explicit input — matrix: fault minting is catalog-stamped at evaluation sites

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | MoneyDivideDecimal |
| evaluation site category | field-default-value-expression |
| type family | business-domain |

### What must be proven

Obligation: BillingCycleDays != 0

Weakest precondition: BillingCycleDays != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| BillingCycleDays | the decimal-typed field read as the divisor of the business-domain division inside this declaration position |
| ReferenceAmount | the money-typed field read as the numerator/dividend |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a).

the divisor's own default is materialized in declaration order during construction, so the numerator field's default expression can only see fields already built above it — a forward reference is impossible in the grammar, not merely disallowed. No event args, no handler guard, and no pre-state fact are in scope at this position (field-default carries only class (a) per the evaluation-site sweep); the only fact a derivation can consume is a declared modifier on the divisor field itself.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: the divisor field (BillingCycleDays)'s own declared modifier (`positive` or `nonzero`) supplies a bound on exactly the field the division reads as its divisor -> divisor-interval excludes 0, by the same interval-arithmetic reasoning Family 4 states for an event-arg bound, carried here to a field's own assignment-governed modifier instead of an event's ingress-governed one
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor's interval from its declared modifiers (a finite set per site — here, the divisor field's own min/max/positive/nonnegative/nonzero modifiers); zero inside the interval, or no bound excluding it, rejects. Family 4's decision procedure text names 'declared modifiers' generically, not restricted to event-arg modifiers, so this cell instantiates it with class (a) in place of class (b).

- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — fault family, arg-constraint discharge: decision procedure computing the divisor's interval from its declared modifiers
- src/Precept/Language/Operations.cs:442 — code: OperationKind.MoneyDivideDecimal — money / decimal → money (scaling); ProofRequirements: [NumericProofRequirement(divisor decimal, NotEquals, 0m, "Divisor must be non-zero")]
- src/Precept/Language/Modifiers.cs:84 — code: ModifierKind.Positive — value > 0; ProofSatisfaction.Numeric(SelfValue, GreaterThan, Constant(0m)); ZeroBoundNumericTypes includes Decimal
- src/Precept/Language/Modifiers.cs:16 — code: ZeroBoundNumericTypes array — Integer, Decimal, Number, Money, Quantity, Price, ExchangeRate, Duration, Period all admit positive/nonnegative/nonzero

### What the failing diagnostic must suggest

- For class (a): bound <Divisor> (`positive` or `nonzero`, or an explicit `min` excluding 0) such that its declared interval satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept G11FieldDefaultMoneyBase
field BillingCycleDays as decimal default 0
field ReferenceAmount as money in 'USD' default '100 USD'
field DailyRate as money in 'USD' default (ReferenceAmount / BillingCycleDays)
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*divisor-positive* — other

- Addition: BillingCycleDays as decimal positive default 1 (replacing the unconstrained `default 0` declaration)
- Premise classes: (a)
- Derivation: divisor field's own `positive` modifier excludes 0 from its interval -> divisor-interval derivation (class (a), Family 4's decision procedure)
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (this evaluation-site category mints no fault obligation at all at HEAD, so nothing distinguishes the base from this addition in the live run — both compile clean. The discharge is recorded as unresolved-today for the site-minting reason, not because the modifier itself failed to discharge anything.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` locus is recorded: the schema's application DU covers `row-guard` and `event-arg-declarations` only, and this addition replaces a FIELD declaration's own modifier/default — a locus the schema does not carry. Recorded as a missing-vocabulary item rather than forced into a mismatched locus.
- `validityArguments` cites 'Arg-bound interval arithmetic', whose written text is scoped to values 'entering as an event arg' (class (b)) — an extension to a field's own assignment-governed modifier (class (a)), not a verbatim match. The matrix has no validity argument written specifically for the fault family's own class-(a)-or-(b) divisor-interval derivation (Family 4 itself carries a decision procedure but no named validity-argument citation). Flagged, not silently assumed; see this file's top-level missing-rules note.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| divisor-positive | BillingCycleDays as decimal nonnegative default 0 (weakens `positive` to `nonnegative` — 0 is still admitted) | nonnegative admits 0 exactly; the divisor's interval still contains 0, so the obligation is not discharged — must still reject, same obligation. | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- No false-proof hazard: this evaluation-site category carries only premise class (a) (field modifiers) — never class (d) — so no discharge here can run through a business `rule` used as a premise (docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3, Defect B does not apply). Clean-compile confirmation of the field-modifier discharge would be admissible in principle; it is moot here because the site mints nothing at HEAD regardless.
- Live-verified at commit e1a14d91 (2026-07-21, Precept.MatrixTools): base — clean compile (no fault diagnostic); only [Warning] FieldNeverSet on all three fields and 'no computable obligations (no rules and no desugaring modifiers in scope).'
- Live-verified discharge (positive modifier) at the same commit: clean compile; WP calculator prints 'Establishment over the default configuration: BillingCycleDays:positive: WP = true' (the modifier's own establishment obligation, folded over its own literal default) and nothing else.
- Live-verified near-miss (nonnegative modifier) at the same commit: clean compile; same shape, 'BillingCycleDays:nonnegative: WP = true'.
- Model status for all three witnesses: provable-under-model (the obligation 'divisor must be non-zero' is catalog-declared and the matrix's fault case shape commits to a proof obligation at every evaluation site the catalog stamps, docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — an explicit input). Built status: unresolved-today for all three — this evaluation-site category mints no fault obligation at HEAD, so the base compiles clean instead of rejecting, and the discharge/near-miss additions change nothing observable. The `baseWitness`/`nearMissWitness` schema shapes carry no separate modelStatus/builtStatus split (only `dischargeWitness` does); the model/built distinction for the base and near-miss is recorded here in cell notes instead, by the same reasoning the schema states for discharge provenance: provenance attests the run, not the `expected` block, which states the definition's committed obligation.
- `noOtherDiagnostics: true` on the base is stated against the model's idealized reject scenario (only the divisor-nonzero obligation named); the live run's actual output carries incidental `FieldNeverSet` warnings (and, for the field-modifier category, the '[skipped obligation] ... bound is not a literal declared value' skip record already reported for the primitive lane in the parallel group) that are pre-existing structural noise unrelated to this obligation, not a second obligation on the same site.
- wp.canonicalKey / canonicalKeySource omitted: the WP calculator (tools/Precept.MatrixTools) computes establishment/preservation WPs through a handler write plan; this obligation sits at a declaration-position evaluation site with no write plan, and the calculator has no computation path for it. Not measured, not inferred.
- the divisor's own default is materialized in declaration order during construction, so the numerator field's default expression can only see fields already built above it — a forward reference is impossible in the grammar, not merely disallowed.
- This cell's schema also covers the other money-anchored business-domain division sites in this group sharing the identical 'Divisor must be non-zero' NumericProofRequirement and the identical class-(a) discharge shape at this evaluation-site category: MoneyDivideMoneySameCurrency, MoneyDivideMoneyCrossCurrency, MoneyDivideQuantity, MoneyDividePrice (its numeric-1 requirement), PriceDivideDecimal, PriceDivideQuantity, and ExchangeRateDivideDecimal. MoneyDividePrice, MoneyDivideQuantity, and PriceDivideQuantity additionally carry a QualifierChain currency-match requirement (a separate obligation at a different site id — currency compatibility, not divisor non-zero — not covered by this cell).

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault prevention obligation family
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: fault case shape row
- docs/language/precept-language-spec.md:1350 — spec: Expression scope table, 'Default value expression' row: field names declared before this field only (no self-reference, no forward reference)
- src/Precept/Language/Operations.cs:442 — code: OperationKind.MoneyDivideDecimal — money / decimal → money (scaling); ProofRequirements: [NumericProofRequirement(divisor decimal, NotEquals, 0m, "Divisor must be non-zero")]

## g11/field-default-quantity — Field default value expression — QuantityDivideDecimal

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § The cell — matrix: item 1 — fault-family obligation schema is the catalog-declared safety precondition at the evaluation site
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — an explicit input — matrix: fault minting is catalog-stamped at evaluation sites

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | QuantityDivideDecimal |
| evaluation site category | field-default-value-expression |
| type family | business-domain |

### What must be proven

Obligation: BillingCycleDays != 0

Weakest precondition: BillingCycleDays != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| BillingCycleDays | the decimal-typed field read as the divisor of the business-domain division inside this declaration position |
| ReferenceQuantity | the quantity-typed field read as the numerator/dividend |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a).

the divisor's own default is materialized in declaration order during construction, so the numerator field's default expression can only see fields already built above it — a forward reference is impossible in the grammar, not merely disallowed. No event args, no handler guard, and no pre-state fact are in scope at this position (field-default carries only class (a) per the evaluation-site sweep); the only fact a derivation can consume is a declared modifier on the divisor field itself.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: the divisor field (BillingCycleDays)'s own declared modifier (`positive` or `nonzero`) supplies a bound on exactly the field the division reads as its divisor -> divisor-interval excludes 0, by the same interval-arithmetic reasoning Family 4 states for an event-arg bound, carried here to a field's own assignment-governed modifier instead of an event's ingress-governed one
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor's interval from its declared modifiers (a finite set per site — here, the divisor field's own min/max/positive/nonnegative/nonzero modifiers); zero inside the interval, or no bound excluding it, rejects. Family 4's decision procedure text names 'declared modifiers' generically, not restricted to event-arg modifiers, so this cell instantiates it with class (a) in place of class (b).

- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — fault family, arg-constraint discharge: decision procedure computing the divisor's interval from its declared modifiers
- src/Precept/Language/Operations.cs:543 — code: OperationKind.QuantityDivideDecimal — quantity / decimal → quantity (scaling); ProofRequirements: [NumericProofRequirement(divisor decimal, NotEquals, 0m, "Divisor must be non-zero")]
- src/Precept/Language/Modifiers.cs:84 — code: ModifierKind.Positive — value > 0; ProofSatisfaction.Numeric(SelfValue, GreaterThan, Constant(0m)); ZeroBoundNumericTypes includes Decimal
- src/Precept/Language/Modifiers.cs:16 — code: ZeroBoundNumericTypes array — Integer, Decimal, Number, Money, Quantity, Price, ExchangeRate, Duration, Period all admit positive/nonnegative/nonzero

### What the failing diagnostic must suggest

- For class (a): bound <Divisor> (`positive` or `nonzero`, or an explicit `min` excluding 0) such that its declared interval satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept G11FieldDefaultQtyBase
field BillingCycleDays as decimal default 0
field ReferenceQuantity as quantity in 'kg' default '100 kg'
field DailyUsage as quantity in 'kg' default (ReferenceQuantity / BillingCycleDays)
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*divisor-positive* — other

- Addition: BillingCycleDays as decimal positive default 1 (replacing the unconstrained `default 0` declaration)
- Premise classes: (a)
- Derivation: divisor field's own `positive` modifier excludes 0 from its interval -> divisor-interval derivation (class (a), Family 4's decision procedure)
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (this evaluation-site category mints no fault obligation at all at HEAD, so nothing distinguishes the base from this addition in the live run — both compile clean. The discharge is recorded as unresolved-today for the site-minting reason, not because the modifier itself failed to discharge anything.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` locus is recorded: the schema's application DU covers `row-guard` and `event-arg-declarations` only, and this addition replaces a FIELD declaration's own modifier/default — a locus the schema does not carry. Recorded as a missing-vocabulary item rather than forced into a mismatched locus.
- `validityArguments` cites 'Arg-bound interval arithmetic', whose written text is scoped to values 'entering as an event arg' (class (b)) — an extension to a field's own assignment-governed modifier (class (a)), not a verbatim match. The matrix has no validity argument written specifically for the fault family's own class-(a)-or-(b) divisor-interval derivation (Family 4 itself carries a decision procedure but no named validity-argument citation). Flagged, not silently assumed; see this file's top-level missing-rules note.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| divisor-positive | BillingCycleDays as decimal nonnegative default 0 (weakens `positive` to `nonnegative` — 0 is still admitted) | nonnegative admits 0 exactly; the divisor's interval still contains 0, so the obligation is not discharged — must still reject, same obligation. | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- No false-proof hazard: this evaluation-site category carries only premise class (a) (field modifiers) — never class (d) — so no discharge here can run through a business `rule` used as a premise (docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3, Defect B does not apply). Clean-compile confirmation of the field-modifier discharge would be admissible in principle; it is moot here because the site mints nothing at HEAD regardless.
- Live-verified at commit e1a14d91 (2026-07-21, Precept.MatrixTools): base — clean compile (no fault diagnostic); only FieldNeverSet warnings and 'no computable obligations'.
- Live-verified discharge (positive modifier) at the same commit: clean compile; 'BillingCycleDays:positive: WP = true' only.
- Live-verified near-miss (nonnegative modifier) at the same commit: clean compile; 'BillingCycleDays:nonnegative: WP = true' only.
- Model status for all three witnesses: provable-under-model (the obligation 'divisor must be non-zero' is catalog-declared and the matrix's fault case shape commits to a proof obligation at every evaluation site the catalog stamps, docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — an explicit input). Built status: unresolved-today for all three — this evaluation-site category mints no fault obligation at HEAD, so the base compiles clean instead of rejecting, and the discharge/near-miss additions change nothing observable. The `baseWitness`/`nearMissWitness` schema shapes carry no separate modelStatus/builtStatus split (only `dischargeWitness` does); the model/built distinction for the base and near-miss is recorded here in cell notes instead, by the same reasoning the schema states for discharge provenance: provenance attests the run, not the `expected` block, which states the definition's committed obligation.
- `noOtherDiagnostics: true` on the base is stated against the model's idealized reject scenario (only the divisor-nonzero obligation named); the live run's actual output carries incidental `FieldNeverSet` warnings (and, for the field-modifier category, the '[skipped obligation] ... bound is not a literal declared value' skip record already reported for the primitive lane in the parallel group) that are pre-existing structural noise unrelated to this obligation, not a second obligation on the same site.
- wp.canonicalKey / canonicalKeySource omitted: the WP calculator (tools/Precept.MatrixTools) computes establishment/preservation WPs through a handler write plan; this obligation sits at a declaration-position evaluation site with no write plan, and the calculator has no computation path for it. Not measured, not inferred.
- the divisor's own default is materialized in declaration order during construction, so the numerator field's default expression can only see fields already built above it — a forward reference is impossible in the grammar, not merely disallowed.
- This cell's schema also covers the other quantity-anchored business-domain division sites in this group sharing the identical 'Divisor must be non-zero' NumericProofRequirement and the identical class-(a) discharge shape at this evaluation-site category: QuantityDivideQuantitySameDimension and QuantityDivideQuantityCrossDimension.

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault prevention obligation family
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: fault case shape row
- docs/language/precept-language-spec.md:1350 — spec: Expression scope table, 'Default value expression' row: field names declared before this field only (no self-reference, no forward reference)
- src/Precept/Language/Operations.cs:543 — code: OperationKind.QuantityDivideDecimal — quantity / decimal → quantity (scaling); ProofRequirements: [NumericProofRequirement(divisor decimal, NotEquals, 0m, "Divisor must be non-zero")]

## g11/field-modifier-money — Field modifier (max) value expression — MoneyDivideDecimal

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § The cell — matrix: item 1 — fault-family obligation schema is the catalog-declared safety precondition at the evaluation site
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — an explicit input — matrix: fault minting is catalog-stamped at evaluation sites

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | MoneyDivideDecimal |
| evaluation site category | field-modifier-value-expression |
| type family | business-domain |

### What must be proven

Obligation: BillingCycleDays != 0

Weakest precondition: BillingCycleDays != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| BillingCycleDays | the decimal-typed field read as the divisor of the business-domain division inside this declaration position |
| ReferenceAmount | the money-typed field read as the numerator/dividend |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a).

a constraint modifier's value expression is rule shorthand and carries a rule condition's scope: any field regardless of declaration order. No event args, no handler guard, and no pre-state fact are in scope at this position (field-modifier carries only class (a) per the evaluation-site sweep); the only fact a derivation can consume is a declared modifier on the divisor field itself.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: the divisor field (BillingCycleDays)'s own declared modifier (`positive` or `nonzero`) supplies a bound on exactly the field the division reads as its divisor -> divisor-interval excludes 0, by the same interval-arithmetic reasoning Family 4 states for an event-arg bound, carried here to a field's own assignment-governed modifier instead of an event's ingress-governed one
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor's interval from its declared modifiers (a finite set per site — here, the divisor field's own min/max/positive/nonnegative/nonzero modifiers); zero inside the interval, or no bound excluding it, rejects. Family 4's decision procedure text names 'declared modifiers' generically, not restricted to event-arg modifiers, so this cell instantiates it with class (a) in place of class (b).

- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — fault family, arg-constraint discharge: decision procedure computing the divisor's interval from its declared modifiers
- src/Precept/Language/Operations.cs:442 — code: OperationKind.MoneyDivideDecimal — money / decimal → money (scaling); ProofRequirements: [NumericProofRequirement(divisor decimal, NotEquals, 0m, "Divisor must be non-zero")]
- src/Precept/Language/Modifiers.cs:84 — code: ModifierKind.Positive — value > 0; ProofSatisfaction.Numeric(SelfValue, GreaterThan, Constant(0m)); ZeroBoundNumericTypes includes Decimal
- src/Precept/Language/Modifiers.cs:16 — code: ZeroBoundNumericTypes array — Integer, Decimal, Number, Money, Quantity, Price, ExchangeRate, Duration, Period all admit positive/nonnegative/nonzero

### What the failing diagnostic must suggest

- For class (a): bound <Divisor> (`positive` or `nonzero`, or an explicit `min` excluding 0) such that its declared interval satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept G11FieldModifierMoneyBase
field BillingCycleDays as decimal default 0
field ReferenceAmount as money in 'USD' default '100 USD'
field CappedRate as money in 'USD' default '0 USD' max (ReferenceAmount / BillingCycleDays)
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*divisor-positive* — other

- Addition: BillingCycleDays as decimal positive default 1 (replacing the unconstrained `default 0` declaration)
- Premise classes: (a)
- Derivation: divisor field's own `positive` modifier excludes 0 from its interval -> divisor-interval derivation (class (a), Family 4's decision procedure)
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (this evaluation-site category mints no fault obligation at all at HEAD, so nothing distinguishes the base from this addition in the live run — both compile clean. The discharge is recorded as unresolved-today for the site-minting reason, not because the modifier itself failed to discharge anything.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` locus is recorded: the schema's application DU covers `row-guard` and `event-arg-declarations` only, and this addition replaces a FIELD declaration's own modifier/default — a locus the schema does not carry. Recorded as a missing-vocabulary item rather than forced into a mismatched locus.
- `validityArguments` cites 'Arg-bound interval arithmetic', whose written text is scoped to values 'entering as an event arg' (class (b)) — an extension to a field's own assignment-governed modifier (class (a)), not a verbatim match. The matrix has no validity argument written specifically for the fault family's own class-(a)-or-(b) divisor-interval derivation (Family 4 itself carries a decision procedure but no named validity-argument citation). Flagged, not silently assumed; see this file's top-level missing-rules note.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| divisor-positive | BillingCycleDays as decimal nonnegative default 0 (weakens `positive` to `nonnegative` — 0 is still admitted) | nonnegative admits 0 exactly; the divisor's interval still contains 0, so the obligation is not discharged — must still reject, same obligation. | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- No false-proof hazard: this evaluation-site category carries only premise class (a) (field modifiers) — never class (d) — so no discharge here can run through a business `rule` used as a premise (docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3, Defect B does not apply). Clean-compile confirmation of the field-modifier discharge would be admissible in principle; it is moot here because the site mints nothing at HEAD regardless.
- Live-verified at commit e1a14d91 (2026-07-21, Precept.MatrixTools): base — clean compile; the WP calculator additionally prints '[skipped obligation] CappedRate:max: bound is not a literal declared value (cross-field or unsupported bound source); not representable as a constant-bound rule' — the bound's own obligation is skipped as a separate, entangled gap, and no fault diagnostic is raised either.
- Live-verified discharge (positive modifier) at the same commit: clean compile; same skip record plus 'BillingCycleDays:positive: WP = true'.
- Live-verified near-miss (nonnegative modifier) at the same commit: clean compile; same skip record plus 'BillingCycleDays:nonnegative: WP = true'.
- Model status for all three witnesses: provable-under-model (the obligation 'divisor must be non-zero' is catalog-declared and the matrix's fault case shape commits to a proof obligation at every evaluation site the catalog stamps, docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — an explicit input). Built status: unresolved-today for all three — this evaluation-site category mints no fault obligation at HEAD, so the base compiles clean instead of rejecting, and the discharge/near-miss additions change nothing observable. The `baseWitness`/`nearMissWitness` schema shapes carry no separate modelStatus/builtStatus split (only `dischargeWitness` does); the model/built distinction for the base and near-miss is recorded here in cell notes instead, by the same reasoning the schema states for discharge provenance: provenance attests the run, not the `expected` block, which states the definition's committed obligation.
- `noOtherDiagnostics: true` on the base is stated against the model's idealized reject scenario (only the divisor-nonzero obligation named); the live run's actual output carries incidental `FieldNeverSet` warnings (and, for the field-modifier category, the '[skipped obligation] ... bound is not a literal declared value' skip record already reported for the primitive lane in the parallel group) that are pre-existing structural noise unrelated to this obligation, not a second obligation on the same site.
- wp.canonicalKey / canonicalKeySource omitted: the WP calculator (tools/Precept.MatrixTools) computes establishment/preservation WPs through a handler write plan; this obligation sits at a declaration-position evaluation site with no write plan, and the calculator has no computation path for it. Not measured, not inferred.
- a constraint modifier's value expression is rule shorthand and carries a rule condition's scope: any field regardless of declaration order.
- This cell's schema also covers the other money-anchored business-domain division sites in this group sharing the identical 'Divisor must be non-zero' NumericProofRequirement and the identical class-(a) discharge shape at this evaluation-site category: MoneyDivideMoneySameCurrency, MoneyDivideMoneyCrossCurrency, MoneyDivideQuantity, MoneyDividePrice (its numeric-1 requirement), PriceDivideDecimal, PriceDivideQuantity, and ExchangeRateDivideDecimal. MoneyDividePrice, MoneyDivideQuantity, and PriceDivideQuantity additionally carry a QualifierChain currency-match requirement (a separate obligation at a different site id — currency compatibility, not divisor non-zero — not covered by this cell).

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault prevention obligation family
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: fault case shape row
- docs/language/precept-language-spec.md:1352 — spec: Expression scope table, 'Modifier value expressions' row: all field names, any declaration order, since a constraint modifier is rule shorthand (§2.4)
- src/Precept/Language/Operations.cs:442 — code: OperationKind.MoneyDivideDecimal — money / decimal → money (scaling); ProofRequirements: [NumericProofRequirement(divisor decimal, NotEquals, 0m, "Divisor must be non-zero")]

## g11/field-modifier-quantity — Field modifier (max) value expression — QuantityDivideDecimal

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § The cell — matrix: item 1 — fault-family obligation schema is the catalog-declared safety precondition at the evaluation site
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — an explicit input — matrix: fault minting is catalog-stamped at evaluation sites

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | QuantityDivideDecimal |
| evaluation site category | field-modifier-value-expression |
| type family | business-domain |

### What must be proven

Obligation: BillingCycleDays != 0

Weakest precondition: BillingCycleDays != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| BillingCycleDays | the decimal-typed field read as the divisor of the business-domain division inside this declaration position |
| ReferenceQuantity | the quantity-typed field read as the numerator/dividend |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a).

a constraint modifier's value expression is rule shorthand and carries a rule condition's scope: any field regardless of declaration order. No event args, no handler guard, and no pre-state fact are in scope at this position (field-modifier carries only class (a) per the evaluation-site sweep); the only fact a derivation can consume is a declared modifier on the divisor field itself.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: the divisor field (BillingCycleDays)'s own declared modifier (`positive` or `nonzero`) supplies a bound on exactly the field the division reads as its divisor -> divisor-interval excludes 0, by the same interval-arithmetic reasoning Family 4 states for an event-arg bound, carried here to a field's own assignment-governed modifier instead of an event's ingress-governed one
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor's interval from its declared modifiers (a finite set per site — here, the divisor field's own min/max/positive/nonnegative/nonzero modifiers); zero inside the interval, or no bound excluding it, rejects. Family 4's decision procedure text names 'declared modifiers' generically, not restricted to event-arg modifiers, so this cell instantiates it with class (a) in place of class (b).

- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — fault family, arg-constraint discharge: decision procedure computing the divisor's interval from its declared modifiers
- src/Precept/Language/Operations.cs:543 — code: OperationKind.QuantityDivideDecimal — quantity / decimal → quantity (scaling); ProofRequirements: [NumericProofRequirement(divisor decimal, NotEquals, 0m, "Divisor must be non-zero")]
- src/Precept/Language/Modifiers.cs:84 — code: ModifierKind.Positive — value > 0; ProofSatisfaction.Numeric(SelfValue, GreaterThan, Constant(0m)); ZeroBoundNumericTypes includes Decimal
- src/Precept/Language/Modifiers.cs:16 — code: ZeroBoundNumericTypes array — Integer, Decimal, Number, Money, Quantity, Price, ExchangeRate, Duration, Period all admit positive/nonnegative/nonzero

### What the failing diagnostic must suggest

- For class (a): bound <Divisor> (`positive` or `nonzero`, or an explicit `min` excluding 0) such that its declared interval satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept G11FieldModifierQtyBase
field BillingCycleDays as decimal default 0
field ReferenceQuantity as quantity in 'kg' default '100 kg'
field CappedUsage as quantity in 'kg' default '0 kg' max (ReferenceQuantity / BillingCycleDays)
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*divisor-positive* — other

- Addition: BillingCycleDays as decimal positive default 1 (replacing the unconstrained `default 0` declaration)
- Premise classes: (a)
- Derivation: divisor field's own `positive` modifier excludes 0 from its interval -> divisor-interval derivation (class (a), Family 4's decision procedure)
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (this evaluation-site category mints no fault obligation at all at HEAD, so nothing distinguishes the base from this addition in the live run — both compile clean. The discharge is recorded as unresolved-today for the site-minting reason, not because the modifier itself failed to discharge anything.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` locus is recorded: the schema's application DU covers `row-guard` and `event-arg-declarations` only, and this addition replaces a FIELD declaration's own modifier/default — a locus the schema does not carry. Recorded as a missing-vocabulary item rather than forced into a mismatched locus.
- `validityArguments` cites 'Arg-bound interval arithmetic', whose written text is scoped to values 'entering as an event arg' (class (b)) — an extension to a field's own assignment-governed modifier (class (a)), not a verbatim match. The matrix has no validity argument written specifically for the fault family's own class-(a)-or-(b) divisor-interval derivation (Family 4 itself carries a decision procedure but no named validity-argument citation). Flagged, not silently assumed; see this file's top-level missing-rules note.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| divisor-positive | BillingCycleDays as decimal nonnegative default 0 (weakens `positive` to `nonnegative` — 0 is still admitted) | nonnegative admits 0 exactly; the divisor's interval still contains 0, so the obligation is not discharged — must still reject, same obligation. | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- No false-proof hazard: this evaluation-site category carries only premise class (a) (field modifiers) — never class (d) — so no discharge here can run through a business `rule` used as a premise (docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3, Defect B does not apply). Clean-compile confirmation of the field-modifier discharge would be admissible in principle; it is moot here because the site mints nothing at HEAD regardless.
- Live-verified at commit e1a14d91 (2026-07-21, Precept.MatrixTools): base — clean compile; same '[skipped obligation] CappedUsage:max: bound is not a literal declared value ...' skip record, no fault diagnostic.
- Live-verified discharge (positive modifier) at the same commit: clean compile; same skip record plus 'BillingCycleDays:positive: WP = true'.
- Live-verified near-miss (nonnegative modifier) at the same commit: clean compile; same skip record plus 'BillingCycleDays:nonnegative: WP = true'.
- Model status for all three witnesses: provable-under-model (the obligation 'divisor must be non-zero' is catalog-declared and the matrix's fault case shape commits to a proof obligation at every evaluation site the catalog stamps, docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — an explicit input). Built status: unresolved-today for all three — this evaluation-site category mints no fault obligation at HEAD, so the base compiles clean instead of rejecting, and the discharge/near-miss additions change nothing observable. The `baseWitness`/`nearMissWitness` schema shapes carry no separate modelStatus/builtStatus split (only `dischargeWitness` does); the model/built distinction for the base and near-miss is recorded here in cell notes instead, by the same reasoning the schema states for discharge provenance: provenance attests the run, not the `expected` block, which states the definition's committed obligation.
- `noOtherDiagnostics: true` on the base is stated against the model's idealized reject scenario (only the divisor-nonzero obligation named); the live run's actual output carries incidental `FieldNeverSet` warnings (and, for the field-modifier category, the '[skipped obligation] ... bound is not a literal declared value' skip record already reported for the primitive lane in the parallel group) that are pre-existing structural noise unrelated to this obligation, not a second obligation on the same site.
- wp.canonicalKey / canonicalKeySource omitted: the WP calculator (tools/Precept.MatrixTools) computes establishment/preservation WPs through a handler write plan; this obligation sits at a declaration-position evaluation site with no write plan, and the calculator has no computation path for it. Not measured, not inferred.
- a constraint modifier's value expression is rule shorthand and carries a rule condition's scope: any field regardless of declaration order.
- This cell's schema also covers the other quantity-anchored business-domain division sites in this group sharing the identical 'Divisor must be non-zero' NumericProofRequirement and the identical class-(a) discharge shape at this evaluation-site category: QuantityDivideQuantitySameDimension and QuantityDivideQuantityCrossDimension.

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault prevention obligation family
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: fault case shape row
- docs/language/precept-language-spec.md:1352 — spec: Expression scope table, 'Modifier value expressions' row: all field names, any declaration order, since a constraint modifier is rule shorthand (§2.4)
- src/Precept/Language/Operations.cs:543 — code: OperationKind.QuantityDivideDecimal — quantity / decimal → quantity (scaling); ProofRequirements: [NumericProofRequirement(divisor decimal, NotEquals, 0m, "Divisor must be non-zero")]

## g11/event-arg-modifier-money — Event-arg modifier (max) value expression — MoneyDivideDecimal

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § The cell — matrix: item 1 — fault-family obligation schema is the catalog-declared safety precondition at the evaluation site
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — an explicit input — matrix: fault minting is catalog-stamped at evaluation sites

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | MoneyDivideDecimal |
| evaluation site category | event-arg-modifier-value-expression |
| type family | business-domain |

### What must be proven

Obligation: BillingCycleDays != 0

Weakest precondition: BillingCycleDays != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| BillingCycleDays | the decimal-typed field read as the divisor of the business-domain division inside this declaration position |
| ReferenceAmount | the money-typed field read as the numerator/dividend |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a).

an event argument's constraint modifier (ArgDecl := Identifier as TypeRef FieldModifier*) is the same rule shorthand as a field modifier, so its value expression carries the identical any-field scope; the arg being constrained is the subject of the bound, not a premise for it, and no guard or other event's args are in scope. No event args, no handler guard, and no pre-state fact are in scope at this position (event-arg-modifier carries only class (a) per the evaluation-site sweep); the only fact a derivation can consume is a declared modifier on the divisor field itself.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: the divisor field (BillingCycleDays)'s own declared modifier (`positive` or `nonzero`) supplies a bound on exactly the field the division reads as its divisor -> divisor-interval excludes 0, by the same interval-arithmetic reasoning Family 4 states for an event-arg bound, carried here to a field's own assignment-governed modifier instead of an event's ingress-governed one
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor's interval from its declared modifiers (a finite set per site — here, the divisor field's own min/max/positive/nonnegative/nonzero modifiers); zero inside the interval, or no bound excluding it, rejects. Family 4's decision procedure text names 'declared modifiers' generically, not restricted to event-arg modifiers, so this cell instantiates it with class (a) in place of class (b).

- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — fault family, arg-constraint discharge: decision procedure computing the divisor's interval from its declared modifiers
- src/Precept/Language/Operations.cs:442 — code: OperationKind.MoneyDivideDecimal — money / decimal → money (scaling); ProofRequirements: [NumericProofRequirement(divisor decimal, NotEquals, 0m, "Divisor must be non-zero")]
- src/Precept/Language/Modifiers.cs:84 — code: ModifierKind.Positive — value > 0; ProofSatisfaction.Numeric(SelfValue, GreaterThan, Constant(0m)); ZeroBoundNumericTypes includes Decimal
- src/Precept/Language/Modifiers.cs:16 — code: ZeroBoundNumericTypes array — Integer, Decimal, Number, Money, Quantity, Price, ExchangeRate, Duration, Period all admit positive/nonnegative/nonzero

### What the failing diagnostic must suggest

- For class (a): bound <Divisor> (`positive` or `nonzero`, or an explicit `min` excluding 0) such that its declared interval satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept G11EventArgModifierMoneyBase
field BillingCycleDays as decimal default 0
field ReferenceAmount as money in 'USD' default '100 USD'
state Draft initial
state Done terminal
event Open initial
event Finish(Cap as money in 'USD' max (ReferenceAmount / BillingCycleDays))
on Open
  -> set BillingCycleDays = 0
from Draft on Finish
  -> transition Done
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*divisor-positive* — other

- Addition: BillingCycleDays as decimal positive default 1 (replacing the unconstrained `default 0` declaration)
- Premise classes: (a)
- Derivation: divisor field's own `positive` modifier excludes 0 from its interval -> divisor-interval derivation (class (a), Family 4's decision procedure)
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (this evaluation-site category mints no fault obligation at all at HEAD, so nothing distinguishes the base from this addition in the live run — both compile clean. The discharge is recorded as unresolved-today for the site-minting reason, not because the modifier itself failed to discharge anything.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` locus is recorded: the schema's application DU covers `row-guard` and `event-arg-declarations` only, and this addition replaces a FIELD declaration's own modifier/default — a locus the schema does not carry. Recorded as a missing-vocabulary item rather than forced into a mismatched locus.
- `validityArguments` cites 'Arg-bound interval arithmetic', whose written text is scoped to values 'entering as an event arg' (class (b)) — an extension to a field's own assignment-governed modifier (class (a)), not a verbatim match. The matrix has no validity argument written specifically for the fault family's own class-(a)-or-(b) divisor-interval derivation (Family 4 itself carries a decision procedure but no named validity-argument citation). Flagged, not silently assumed; see this file's top-level missing-rules note.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| divisor-positive | BillingCycleDays as decimal nonnegative default 0 (weakens `positive` to `nonnegative` — 0 is still admitted) | nonnegative admits 0 exactly; the divisor's interval still contains 0, so the obligation is not discharged — must still reject, same obligation. | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- No false-proof hazard: this evaluation-site category carries only premise class (a) (field modifiers) — never class (d) — so no discharge here can run through a business `rule` used as a premise (docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3, Defect B does not apply). Clean-compile confirmation of the field-modifier discharge would be admissible in principle; it is moot here because the site mints nothing at HEAD regardless.
- Live-verified at commit e1a14d91 (2026-07-21, Precept.MatrixTools): base — clean compile; only a FieldNeverSet warning on ReferenceAmount and 'no computable obligations' — the arg's own max bound is not even reported as a skip here (unlike the field-modifier category), no fault diagnostic.
- Live-verified discharge (positive modifier) at the same commit: clean compile; establishment obligations for BillingCycleDays:positive print for both the default configuration and the construction row 'on Open', and the preservation section for 'from Draft on Finish' reports '(no writes — every obligation frame-preserves)'.
- Live-verified near-miss (nonnegative modifier) at the same commit: clean compile; same shape with BillingCycleDays:nonnegative.
- Model status for all three witnesses: provable-under-model (the obligation 'divisor must be non-zero' is catalog-declared and the matrix's fault case shape commits to a proof obligation at every evaluation site the catalog stamps, docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — an explicit input). Built status: unresolved-today for all three — this evaluation-site category mints no fault obligation at HEAD, so the base compiles clean instead of rejecting, and the discharge/near-miss additions change nothing observable. The `baseWitness`/`nearMissWitness` schema shapes carry no separate modelStatus/builtStatus split (only `dischargeWitness` does); the model/built distinction for the base and near-miss is recorded here in cell notes instead, by the same reasoning the schema states for discharge provenance: provenance attests the run, not the `expected` block, which states the definition's committed obligation.
- `noOtherDiagnostics: true` on the base is stated against the model's idealized reject scenario (only the divisor-nonzero obligation named); the live run's actual output carries incidental `FieldNeverSet` warnings (and, for the field-modifier category, the '[skipped obligation] ... bound is not a literal declared value' skip record already reported for the primitive lane in the parallel group) that are pre-existing structural noise unrelated to this obligation, not a second obligation on the same site.
- wp.canonicalKey / canonicalKeySource omitted: the WP calculator (tools/Precept.MatrixTools) computes establishment/preservation WPs through a handler write plan; this obligation sits at a declaration-position evaluation site with no write plan, and the calculator has no computation path for it. Not measured, not inferred.
- an event argument's constraint modifier (ArgDecl := Identifier as TypeRef FieldModifier*) is the same rule shorthand as a field modifier, so its value expression carries the identical any-field scope; the arg being constrained is the subject of the bound, not a premise for it, and no guard or other event's args are in scope.
- This cell's schema also covers the other money-anchored business-domain division sites in this group sharing the identical 'Divisor must be non-zero' NumericProofRequirement and the identical class-(a) discharge shape at this evaluation-site category: MoneyDivideMoneySameCurrency, MoneyDivideMoneyCrossCurrency, MoneyDivideQuantity, MoneyDividePrice (its numeric-1 requirement), PriceDivideDecimal, PriceDivideQuantity, and ExchangeRateDivideDecimal. MoneyDividePrice, MoneyDivideQuantity, and PriceDivideQuantity additionally carry a QualifierChain currency-match requirement (a separate obligation at a different site id — currency compatibility, not divisor non-zero — not covered by this cell).

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault prevention obligation family
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: fault case shape row
- docs/language/precept-language-spec.md:927 — spec: ArgDecl := Identifier as TypeRef FieldModifier* — an event arg's constraint modifiers are the same FieldModifier production as a field's
- src/Precept/Language/Operations.cs:442 — code: OperationKind.MoneyDivideDecimal — money / decimal → money (scaling); ProofRequirements: [NumericProofRequirement(divisor decimal, NotEquals, 0m, "Divisor must be non-zero")]

## g11/event-arg-modifier-quantity — Event-arg modifier (max) value expression — QuantityDivideDecimal

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § The cell — matrix: item 1 — fault-family obligation schema is the catalog-declared safety precondition at the evaluation site
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — an explicit input — matrix: fault minting is catalog-stamped at evaluation sites

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | QuantityDivideDecimal |
| evaluation site category | event-arg-modifier-value-expression |
| type family | business-domain |

### What must be proven

Obligation: BillingCycleDays != 0

Weakest precondition: BillingCycleDays != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| BillingCycleDays | the decimal-typed field read as the divisor of the business-domain division inside this declaration position |
| ReferenceQuantity | the quantity-typed field read as the numerator/dividend |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a).

an event argument's constraint modifier (ArgDecl := Identifier as TypeRef FieldModifier*) is the same rule shorthand as a field modifier, so its value expression carries the identical any-field scope; the arg being constrained is the subject of the bound, not a premise for it, and no guard or other event's args are in scope. No event args, no handler guard, and no pre-state fact are in scope at this position (event-arg-modifier carries only class (a) per the evaluation-site sweep); the only fact a derivation can consume is a declared modifier on the divisor field itself.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: the divisor field (BillingCycleDays)'s own declared modifier (`positive` or `nonzero`) supplies a bound on exactly the field the division reads as its divisor -> divisor-interval excludes 0, by the same interval-arithmetic reasoning Family 4 states for an event-arg bound, carried here to a field's own assignment-governed modifier instead of an event's ingress-governed one
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor's interval from its declared modifiers (a finite set per site — here, the divisor field's own min/max/positive/nonnegative/nonzero modifiers); zero inside the interval, or no bound excluding it, rejects. Family 4's decision procedure text names 'declared modifiers' generically, not restricted to event-arg modifiers, so this cell instantiates it with class (a) in place of class (b).

- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — fault family, arg-constraint discharge: decision procedure computing the divisor's interval from its declared modifiers
- src/Precept/Language/Operations.cs:543 — code: OperationKind.QuantityDivideDecimal — quantity / decimal → quantity (scaling); ProofRequirements: [NumericProofRequirement(divisor decimal, NotEquals, 0m, "Divisor must be non-zero")]
- src/Precept/Language/Modifiers.cs:84 — code: ModifierKind.Positive — value > 0; ProofSatisfaction.Numeric(SelfValue, GreaterThan, Constant(0m)); ZeroBoundNumericTypes includes Decimal
- src/Precept/Language/Modifiers.cs:16 — code: ZeroBoundNumericTypes array — Integer, Decimal, Number, Money, Quantity, Price, ExchangeRate, Duration, Period all admit positive/nonnegative/nonzero

### What the failing diagnostic must suggest

- For class (a): bound <Divisor> (`positive` or `nonzero`, or an explicit `min` excluding 0) such that its declared interval satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept G11EventArgModifierQtyBase
field BillingCycleDays as decimal default 0
field ReferenceQuantity as quantity in 'kg' default '100 kg'
state Draft initial
state Done terminal
event Open initial
event Finish(Cap as quantity in 'kg' max (ReferenceQuantity / BillingCycleDays))
on Open
  -> set BillingCycleDays = 0
from Draft on Finish
  -> transition Done
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*divisor-positive* — other

- Addition: BillingCycleDays as decimal positive default 1 (replacing the unconstrained `default 0` declaration)
- Premise classes: (a)
- Derivation: divisor field's own `positive` modifier excludes 0 from its interval -> divisor-interval derivation (class (a), Family 4's decision procedure)
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (this evaluation-site category mints no fault obligation at all at HEAD, so nothing distinguishes the base from this addition in the live run — both compile clean. The discharge is recorded as unresolved-today for the site-minting reason, not because the modifier itself failed to discharge anything.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` locus is recorded: the schema's application DU covers `row-guard` and `event-arg-declarations` only, and this addition replaces a FIELD declaration's own modifier/default — a locus the schema does not carry. Recorded as a missing-vocabulary item rather than forced into a mismatched locus.
- `validityArguments` cites 'Arg-bound interval arithmetic', whose written text is scoped to values 'entering as an event arg' (class (b)) — an extension to a field's own assignment-governed modifier (class (a)), not a verbatim match. The matrix has no validity argument written specifically for the fault family's own class-(a)-or-(b) divisor-interval derivation (Family 4 itself carries a decision procedure but no named validity-argument citation). Flagged, not silently assumed; see this file's top-level missing-rules note.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| divisor-positive | BillingCycleDays as decimal nonnegative default 0 (weakens `positive` to `nonnegative` — 0 is still admitted) | nonnegative admits 0 exactly; the divisor's interval still contains 0, so the obligation is not discharged — must still reject, same obligation. | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- No false-proof hazard: this evaluation-site category carries only premise class (a) (field modifiers) — never class (d) — so no discharge here can run through a business `rule` used as a premise (docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3, Defect B does not apply). Clean-compile confirmation of the field-modifier discharge would be admissible in principle; it is moot here because the site mints nothing at HEAD regardless.
- Live-verified at commit e1a14d91 (2026-07-21, Precept.MatrixTools): base — clean compile; only a FieldNeverSet warning on ReferenceQuantity and 'no computable obligations', no fault diagnostic.
- Live-verified discharge (positive modifier) at the same commit: clean compile; establishment obligations for BillingCycleDays:positive at both the default configuration and the construction row; preservation for 'from Draft on Finish' frame-preserves (no writes).
- Live-verified near-miss (nonnegative modifier) at the same commit: clean compile; same shape with BillingCycleDays:nonnegative.
- Model status for all three witnesses: provable-under-model (the obligation 'divisor must be non-zero' is catalog-declared and the matrix's fault case shape commits to a proof obligation at every evaluation site the catalog stamps, docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — an explicit input). Built status: unresolved-today for all three — this evaluation-site category mints no fault obligation at HEAD, so the base compiles clean instead of rejecting, and the discharge/near-miss additions change nothing observable. The `baseWitness`/`nearMissWitness` schema shapes carry no separate modelStatus/builtStatus split (only `dischargeWitness` does); the model/built distinction for the base and near-miss is recorded here in cell notes instead, by the same reasoning the schema states for discharge provenance: provenance attests the run, not the `expected` block, which states the definition's committed obligation.
- `noOtherDiagnostics: true` on the base is stated against the model's idealized reject scenario (only the divisor-nonzero obligation named); the live run's actual output carries incidental `FieldNeverSet` warnings (and, for the field-modifier category, the '[skipped obligation] ... bound is not a literal declared value' skip record already reported for the primitive lane in the parallel group) that are pre-existing structural noise unrelated to this obligation, not a second obligation on the same site.
- wp.canonicalKey / canonicalKeySource omitted: the WP calculator (tools/Precept.MatrixTools) computes establishment/preservation WPs through a handler write plan; this obligation sits at a declaration-position evaluation site with no write plan, and the calculator has no computation path for it. Not measured, not inferred.
- an event argument's constraint modifier (ArgDecl := Identifier as TypeRef FieldModifier*) is the same rule shorthand as a field modifier, so its value expression carries the identical any-field scope; the arg being constrained is the subject of the bound, not a premise for it, and no guard or other event's args are in scope.
- This cell's schema also covers the other quantity-anchored business-domain division sites in this group sharing the identical 'Divisor must be non-zero' NumericProofRequirement and the identical class-(a) discharge shape at this evaluation-site category: QuantityDivideQuantitySameDimension and QuantityDivideQuantityCrossDimension.

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault prevention obligation family
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: fault case shape row
- docs/language/precept-language-spec.md:927 — spec: ArgDecl := Identifier as TypeRef FieldModifier* — an event arg's constraint modifiers are the same FieldModifier production as a field's
- src/Precept/Language/Operations.cs:543 — code: OperationKind.QuantityDivideDecimal — quantity / decimal → quantity (scaling); ProofRequirements: [NumericProofRequirement(divisor decimal, NotEquals, 0m, "Divisor must be non-zero")]

## g11/collection-inner-type-money — Collection inner-type modifier (max) value expression — MoneyDivideDecimal

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § The cell — matrix: item 1 — fault-family obligation schema is the catalog-declared safety precondition at the evaluation site
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — an explicit input — matrix: fault minting is catalog-stamped at evaluation sites

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | MoneyDivideDecimal |
| evaluation site category | collection-inner-type-modifier-value-expression |
| type family | business-domain |

### What must be proven

Obligation: BillingCycleDays != 0

Weakest precondition: BillingCycleDays != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| BillingCycleDays | the decimal-typed field read as the divisor of the business-domain division inside this declaration position |
| ReferenceAmount | the money-typed field read as the numerator/dividend |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a).

an inner-type value modifier desugars to a per-element rule and participates in proof identically to the same modifier on a scalar field, so its value expression carries the same any-field scope; the subject is a collection element rather than a field, which changes what a discharging fact must range over, not the scope rule itself. No event args, no handler guard, and no pre-state fact are in scope at this position (collection-inner-type carries only class (a) per the evaluation-site sweep); the only fact a derivation can consume is a declared modifier on the divisor field itself.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: the divisor field (BillingCycleDays)'s own declared modifier (`positive` or `nonzero`) supplies a bound on exactly the field the division reads as its divisor -> divisor-interval excludes 0, by the same interval-arithmetic reasoning Family 4 states for an event-arg bound, carried here to a field's own assignment-governed modifier instead of an event's ingress-governed one
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor's interval from its declared modifiers (a finite set per site — here, the divisor field's own min/max/positive/nonnegative/nonzero modifiers); zero inside the interval, or no bound excluding it, rejects. Family 4's decision procedure text names 'declared modifiers' generically, not restricted to event-arg modifiers, so this cell instantiates it with class (a) in place of class (b).

- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — fault family, arg-constraint discharge: decision procedure computing the divisor's interval from its declared modifiers
- src/Precept/Language/Operations.cs:442 — code: OperationKind.MoneyDivideDecimal — money / decimal → money (scaling); ProofRequirements: [NumericProofRequirement(divisor decimal, NotEquals, 0m, "Divisor must be non-zero")]
- src/Precept/Language/Modifiers.cs:84 — code: ModifierKind.Positive — value > 0; ProofSatisfaction.Numeric(SelfValue, GreaterThan, Constant(0m)); ZeroBoundNumericTypes includes Decimal
- src/Precept/Language/Modifiers.cs:16 — code: ZeroBoundNumericTypes array — Integer, Decimal, Number, Money, Quantity, Price, ExchangeRate, Duration, Period all admit positive/nonnegative/nonzero

### What the failing diagnostic must suggest

- For class (a): bound <Divisor> (`positive` or `nonzero`, or an explicit `min` excluding 0) such that its declared interval satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept G11CollectionInnerMoneyBase
field BillingCycleDays as decimal default 0
field ReferenceAmount as money in 'USD' default '100 USD'
field UnitPrices as list of money in 'USD' max (ReferenceAmount / BillingCycleDays)
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*divisor-positive* — other

- Addition: BillingCycleDays as decimal positive default 1 (replacing the unconstrained `default 0` declaration)
- Premise classes: (a)
- Derivation: divisor field's own `positive` modifier excludes 0 from its interval -> divisor-interval derivation (class (a), Family 4's decision procedure)
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (this evaluation-site category mints no fault obligation at all at HEAD, so nothing distinguishes the base from this addition in the live run — both compile clean. The discharge is recorded as unresolved-today for the site-minting reason, not because the modifier itself failed to discharge anything.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` locus is recorded: the schema's application DU covers `row-guard` and `event-arg-declarations` only, and this addition replaces a FIELD declaration's own modifier/default — a locus the schema does not carry. Recorded as a missing-vocabulary item rather than forced into a mismatched locus.
- `validityArguments` cites 'Arg-bound interval arithmetic', whose written text is scoped to values 'entering as an event arg' (class (b)) — an extension to a field's own assignment-governed modifier (class (a)), not a verbatim match. The matrix has no validity argument written specifically for the fault family's own class-(a)-or-(b) divisor-interval derivation (Family 4 itself carries a decision procedure but no named validity-argument citation). Flagged, not silently assumed; see this file's top-level missing-rules note.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| divisor-positive | BillingCycleDays as decimal nonnegative default 0 (weakens `positive` to `nonnegative` — 0 is still admitted) | nonnegative admits 0 exactly; the divisor's interval still contains 0, so the obligation is not discharged — must still reject, same obligation. | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- No false-proof hazard: this evaluation-site category carries only premise class (a) (field modifiers) — never class (d) — so no discharge here can run through a business `rule` used as a premise (docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3, Defect B does not apply). Clean-compile confirmation of the field-modifier discharge would be admissible in principle; it is moot here because the site mints nothing at HEAD regardless.
- Live-verified at commit e1a14d91 (2026-07-21, Precept.MatrixTools): base — clean compile; only FieldNeverSet warnings and 'no computable obligations' — no skip record and no fault diagnostic.
- Live-verified discharge (positive modifier) at the same commit: clean compile; 'BillingCycleDays:positive: WP = true' only.
- Live-verified near-miss (nonnegative modifier) at the same commit: clean compile; 'BillingCycleDays:nonnegative: WP = true' only.
- Model status for all three witnesses: provable-under-model (the obligation 'divisor must be non-zero' is catalog-declared and the matrix's fault case shape commits to a proof obligation at every evaluation site the catalog stamps, docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — an explicit input). Built status: unresolved-today for all three — this evaluation-site category mints no fault obligation at HEAD, so the base compiles clean instead of rejecting, and the discharge/near-miss additions change nothing observable. The `baseWitness`/`nearMissWitness` schema shapes carry no separate modelStatus/builtStatus split (only `dischargeWitness` does); the model/built distinction for the base and near-miss is recorded here in cell notes instead, by the same reasoning the schema states for discharge provenance: provenance attests the run, not the `expected` block, which states the definition's committed obligation.
- `noOtherDiagnostics: true` on the base is stated against the model's idealized reject scenario (only the divisor-nonzero obligation named); the live run's actual output carries incidental `FieldNeverSet` warnings (and, for the field-modifier category, the '[skipped obligation] ... bound is not a literal declared value' skip record already reported for the primitive lane in the parallel group) that are pre-existing structural noise unrelated to this obligation, not a second obligation on the same site.
- wp.canonicalKey / canonicalKeySource omitted: the WP calculator (tools/Precept.MatrixTools) computes establishment/preservation WPs through a handler write plan; this obligation sits at a declaration-position evaluation site with no write plan, and the calculator has no computation path for it. Not measured, not inferred.
- an inner-type value modifier desugars to a per-element rule and participates in proof identically to the same modifier on a scalar field, so its value expression carries the same any-field scope; the subject is a collection element rather than a field, which changes what a discharging fact must range over, not the scope rule itself.
- This cell's schema also covers the other money-anchored business-domain division sites in this group sharing the identical 'Divisor must be non-zero' NumericProofRequirement and the identical class-(a) discharge shape at this evaluation-site category: MoneyDivideMoneySameCurrency, MoneyDivideMoneyCrossCurrency, MoneyDivideQuantity, MoneyDividePrice (its numeric-1 requirement), PriceDivideDecimal, PriceDivideQuantity, and ExchangeRateDivideDecimal. MoneyDividePrice, MoneyDivideQuantity, and PriceDivideQuantity additionally carry a QualifierChain currency-match requirement (a separate obligation at a different site id — currency compatibility, not divisor non-zero — not covered by this cell).

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault prevention obligation family
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: fault case shape row
- docs/language/precept-language-spec.md:1123 — spec: 'Value modifiers in inner-type position': an inner-type value modifier desugars to a per-element rule and participates in proof identically to the same modifier on a scalar field
- src/Precept/Language/Operations.cs:442 — code: OperationKind.MoneyDivideDecimal — money / decimal → money (scaling); ProofRequirements: [NumericProofRequirement(divisor decimal, NotEquals, 0m, "Divisor must be non-zero")]

## g11/collection-inner-type-quantity — Collection inner-type modifier (max) value expression — QuantityDivideDecimal

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § The cell — matrix: item 1 — fault-family obligation schema is the catalog-declared safety precondition at the evaluation site
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — an explicit input — matrix: fault minting is catalog-stamped at evaluation sites

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | QuantityDivideDecimal |
| evaluation site category | collection-inner-type-modifier-value-expression |
| type family | business-domain |

### What must be proven

Obligation: BillingCycleDays != 0

Weakest precondition: BillingCycleDays != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| BillingCycleDays | the decimal-typed field read as the divisor of the business-domain division inside this declaration position |
| ReferenceQuantity | the quantity-typed field read as the numerator/dividend |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a).

an inner-type value modifier desugars to a per-element rule and participates in proof identically to the same modifier on a scalar field, so its value expression carries the same any-field scope; the subject is a collection element rather than a field, which changes what a discharging fact must range over, not the scope rule itself. No event args, no handler guard, and no pre-state fact are in scope at this position (collection-inner-type carries only class (a) per the evaluation-site sweep); the only fact a derivation can consume is a declared modifier on the divisor field itself.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: the divisor field (BillingCycleDays)'s own declared modifier (`positive` or `nonzero`) supplies a bound on exactly the field the division reads as its divisor -> divisor-interval excludes 0, by the same interval-arithmetic reasoning Family 4 states for an event-arg bound, carried here to a field's own assignment-governed modifier instead of an event's ingress-governed one
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor's interval from its declared modifiers (a finite set per site — here, the divisor field's own min/max/positive/nonnegative/nonzero modifiers); zero inside the interval, or no bound excluding it, rejects. Family 4's decision procedure text names 'declared modifiers' generically, not restricted to event-arg modifiers, so this cell instantiates it with class (a) in place of class (b).

- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — fault family, arg-constraint discharge: decision procedure computing the divisor's interval from its declared modifiers
- src/Precept/Language/Operations.cs:543 — code: OperationKind.QuantityDivideDecimal — quantity / decimal → quantity (scaling); ProofRequirements: [NumericProofRequirement(divisor decimal, NotEquals, 0m, "Divisor must be non-zero")]
- src/Precept/Language/Modifiers.cs:84 — code: ModifierKind.Positive — value > 0; ProofSatisfaction.Numeric(SelfValue, GreaterThan, Constant(0m)); ZeroBoundNumericTypes includes Decimal
- src/Precept/Language/Modifiers.cs:16 — code: ZeroBoundNumericTypes array — Integer, Decimal, Number, Money, Quantity, Price, ExchangeRate, Duration, Period all admit positive/nonnegative/nonzero

### What the failing diagnostic must suggest

- For class (a): bound <Divisor> (`positive` or `nonzero`, or an explicit `min` excluding 0) such that its declared interval satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept G11CollectionInnerQtyBase
field BillingCycleDays as decimal default 0
field ReferenceQuantity as quantity in 'kg' default '100 kg'
field UnitQuantities as list of quantity in 'kg' max (ReferenceQuantity / BillingCycleDays)
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*divisor-positive* — other

- Addition: BillingCycleDays as decimal positive default 1 (replacing the unconstrained `default 0` declaration)
- Premise classes: (a)
- Derivation: divisor field's own `positive` modifier excludes 0 from its interval -> divisor-interval derivation (class (a), Family 4's decision procedure)
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (this evaluation-site category mints no fault obligation at all at HEAD, so nothing distinguishes the base from this addition in the live run — both compile clean. The discharge is recorded as unresolved-today for the site-minting reason, not because the modifier itself failed to discharge anything.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` locus is recorded: the schema's application DU covers `row-guard` and `event-arg-declarations` only, and this addition replaces a FIELD declaration's own modifier/default — a locus the schema does not carry. Recorded as a missing-vocabulary item rather than forced into a mismatched locus.
- `validityArguments` cites 'Arg-bound interval arithmetic', whose written text is scoped to values 'entering as an event arg' (class (b)) — an extension to a field's own assignment-governed modifier (class (a)), not a verbatim match. The matrix has no validity argument written specifically for the fault family's own class-(a)-or-(b) divisor-interval derivation (Family 4 itself carries a decision procedure but no named validity-argument citation). Flagged, not silently assumed; see this file's top-level missing-rules note.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| divisor-positive | BillingCycleDays as decimal nonnegative default 0 (weakens `positive` to `nonnegative` — 0 is still admitted) | nonnegative admits 0 exactly; the divisor's interval still contains 0, so the obligation is not discharged — must still reject, same obligation. | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- No false-proof hazard: this evaluation-site category carries only premise class (a) (field modifiers) — never class (d) — so no discharge here can run through a business `rule` used as a premise (docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3, Defect B does not apply). Clean-compile confirmation of the field-modifier discharge would be admissible in principle; it is moot here because the site mints nothing at HEAD regardless.
- Live-verified at commit e1a14d91 (2026-07-21, Precept.MatrixTools): base — clean compile; only FieldNeverSet warnings and 'no computable obligations' — no skip record and no fault diagnostic.
- Live-verified discharge (positive modifier) at the same commit: clean compile; 'BillingCycleDays:positive: WP = true' only.
- Live-verified near-miss (nonnegative modifier) at the same commit: clean compile; 'BillingCycleDays:nonnegative: WP = true' only.
- Model status for all three witnesses: provable-under-model (the obligation 'divisor must be non-zero' is catalog-declared and the matrix's fault case shape commits to a proof obligation at every evaluation site the catalog stamps, docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — an explicit input). Built status: unresolved-today for all three — this evaluation-site category mints no fault obligation at HEAD, so the base compiles clean instead of rejecting, and the discharge/near-miss additions change nothing observable. The `baseWitness`/`nearMissWitness` schema shapes carry no separate modelStatus/builtStatus split (only `dischargeWitness` does); the model/built distinction for the base and near-miss is recorded here in cell notes instead, by the same reasoning the schema states for discharge provenance: provenance attests the run, not the `expected` block, which states the definition's committed obligation.
- `noOtherDiagnostics: true` on the base is stated against the model's idealized reject scenario (only the divisor-nonzero obligation named); the live run's actual output carries incidental `FieldNeverSet` warnings (and, for the field-modifier category, the '[skipped obligation] ... bound is not a literal declared value' skip record already reported for the primitive lane in the parallel group) that are pre-existing structural noise unrelated to this obligation, not a second obligation on the same site.
- wp.canonicalKey / canonicalKeySource omitted: the WP calculator (tools/Precept.MatrixTools) computes establishment/preservation WPs through a handler write plan; this obligation sits at a declaration-position evaluation site with no write plan, and the calculator has no computation path for it. Not measured, not inferred.
- an inner-type value modifier desugars to a per-element rule and participates in proof identically to the same modifier on a scalar field, so its value expression carries the same any-field scope; the subject is a collection element rather than a field, which changes what a discharging fact must range over, not the scope rule itself.
- This cell's schema also covers the other quantity-anchored business-domain division sites in this group sharing the identical 'Divisor must be non-zero' NumericProofRequirement and the identical class-(a) discharge shape at this evaluation-site category: QuantityDivideQuantitySameDimension and QuantityDivideQuantityCrossDimension.

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault prevention obligation family
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: fault case shape row
- docs/language/precept-language-spec.md:1123 — spec: 'Value modifiers in inner-type position': an inner-type value modifier desugars to a per-element rule and participates in proof identically to the same modifier on a scalar field
- src/Precept/Language/Operations.cs:543 — code: OperationKind.QuantityDivideDecimal — quantity / decimal → quantity (scaling); ProofRequirements: [NumericProofRequirement(divisor decimal, NotEquals, 0m, "Divisor must be non-zero")]

## g11/type-qualifier-money — Type qualifier expression — MoneyDivideDecimal

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § The cell — matrix: item 1 — fault-family obligation schema is the catalog-declared safety precondition at the evaluation site
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — an explicit input — matrix: fault minting is catalog-stamped at evaluation sites

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | MoneyDivideDecimal |
| evaluation site category | type-qualifier-expression |
| type family | business-domain |

### What must be proven

Obligation: BillingCycleDays != 0

Weakest precondition: BillingCycleDays != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| BillingCycleDays | the decimal-typed field read as the divisor of the business-domain division inside this declaration position |
| ReferenceAmount | the money-typed field read as the numerator/dividend |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a).

TypeQualifier := (in \| of \| to) Expr is an ordinary expression position, not a literal slot; the Expression scope table does not itemize a qualifier-expression row separately — an unstated scope rule, recorded here rather than assumed — so this cell treats it by the same reasoning as the modifier-value-expression row: evaluated against the complete configuration rather than materialized in declaration order. No event args, no handler guard, and no pre-state fact are in scope at this position (type-qualifier carries only class (a) per the evaluation-site sweep); the only fact a derivation can consume is a declared modifier on the divisor field itself.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: the divisor field (BillingCycleDays)'s own declared modifier (`positive` or `nonzero`) supplies a bound on exactly the field the division reads as its divisor -> divisor-interval excludes 0, by the same interval-arithmetic reasoning Family 4 states for an event-arg bound, carried here to a field's own assignment-governed modifier instead of an event's ingress-governed one
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor's interval from its declared modifiers (a finite set per site — here, the divisor field's own min/max/positive/nonnegative/nonzero modifiers); zero inside the interval, or no bound excluding it, rejects. Family 4's decision procedure text names 'declared modifiers' generically, not restricted to event-arg modifiers, so this cell instantiates it with class (a) in place of class (b).

- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — fault family, arg-constraint discharge: decision procedure computing the divisor's interval from its declared modifiers
- src/Precept/Language/Operations.cs:442 — code: OperationKind.MoneyDivideDecimal — money / decimal → money (scaling); ProofRequirements: [NumericProofRequirement(divisor decimal, NotEquals, 0m, "Divisor must be non-zero")]
- src/Precept/Language/Modifiers.cs:84 — code: ModifierKind.Positive — value > 0; ProofSatisfaction.Numeric(SelfValue, GreaterThan, Constant(0m)); ZeroBoundNumericTypes includes Decimal
- src/Precept/Language/Modifiers.cs:16 — code: ZeroBoundNumericTypes array — Integer, Decimal, Number, Money, Quantity, Price, ExchangeRate, Duration, Period all admit positive/nonnegative/nonzero

### What the failing diagnostic must suggest

- For class (a): bound <Divisor> (`positive` or `nonzero`, or an explicit `min` excluding 0) such that its declared interval satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept G11TypeQualifierMoneyBase
field BillingCycleDays as decimal default 0
field ReferenceAmount as money in 'USD' default '100 USD'
field Amount as money in '{(ReferenceAmount / BillingCycleDays).currency}' optional
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*divisor-positive* — other

- Addition: BillingCycleDays as decimal positive default 1 (replacing the unconstrained `default 0` declaration)
- Premise classes: (a)
- Derivation: divisor field's own `positive` modifier excludes 0 from its interval -> divisor-interval derivation (class (a), Family 4's decision procedure)
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (this evaluation-site category mints no fault obligation at all at HEAD, so nothing distinguishes the base from this addition in the live run — both compile clean. The discharge is recorded as unresolved-today for the site-minting reason, not because the modifier itself failed to discharge anything.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` locus is recorded: the schema's application DU covers `row-guard` and `event-arg-declarations` only, and this addition replaces a FIELD declaration's own modifier/default — a locus the schema does not carry. Recorded as a missing-vocabulary item rather than forced into a mismatched locus.
- `validityArguments` cites 'Arg-bound interval arithmetic', whose written text is scoped to values 'entering as an event arg' (class (b)) — an extension to a field's own assignment-governed modifier (class (a)), not a verbatim match. The matrix has no validity argument written specifically for the fault family's own class-(a)-or-(b) divisor-interval derivation (Family 4 itself carries a decision procedure but no named validity-argument citation). Flagged, not silently assumed; see this file's top-level missing-rules note.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| divisor-positive | BillingCycleDays as decimal nonnegative default 0 (weakens `positive` to `nonnegative` — 0 is still admitted) | nonnegative admits 0 exactly; the divisor's interval still contains 0, so the obligation is not discharged — must still reject, same obligation. | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- No false-proof hazard: this evaluation-site category carries only premise class (a) (field modifiers) — never class (d) — so no discharge here can run through a business `rule` used as a premise (docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3, Defect B does not apply). Clean-compile confirmation of the field-modifier discharge would be admissible in principle; it is moot here because the site mints nothing at HEAD regardless.
- Live-verified at commit e1a14d91 (2026-07-21, Precept.MatrixTools): base — clean compile; only FieldNeverSet warnings and 'no computable obligations' — the qualifier's own division parses and type-checks (the '.currency' accessor on the MoneyDivideDecimal result resolves to Currency, satisfying the qualifier's type), and raises no fault diagnostic.
- Live-verified discharge (positive modifier) at the same commit: clean compile; 'BillingCycleDays:positive: WP = true' only.
- Live-verified near-miss (nonnegative modifier) at the same commit: clean compile; 'BillingCycleDays:nonnegative: WP = true' only.
- Model status for all three witnesses: provable-under-model (the obligation 'divisor must be non-zero' is catalog-declared and the matrix's fault case shape commits to a proof obligation at every evaluation site the catalog stamps, docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — an explicit input). Built status: unresolved-today for all three — this evaluation-site category mints no fault obligation at HEAD, so the base compiles clean instead of rejecting, and the discharge/near-miss additions change nothing observable. The `baseWitness`/`nearMissWitness` schema shapes carry no separate modelStatus/builtStatus split (only `dischargeWitness` does); the model/built distinction for the base and near-miss is recorded here in cell notes instead, by the same reasoning the schema states for discharge provenance: provenance attests the run, not the `expected` block, which states the definition's committed obligation.
- `noOtherDiagnostics: true` on the base is stated against the model's idealized reject scenario (only the divisor-nonzero obligation named); the live run's actual output carries incidental `FieldNeverSet` warnings (and, for the field-modifier category, the '[skipped obligation] ... bound is not a literal declared value' skip record already reported for the primitive lane in the parallel group) that are pre-existing structural noise unrelated to this obligation, not a second obligation on the same site.
- wp.canonicalKey / canonicalKeySource omitted: the WP calculator (tools/Precept.MatrixTools) computes establishment/preservation WPs through a handler write plan; this obligation sits at a declaration-position evaluation site with no write plan, and the calculator has no computation path for it. Not measured, not inferred.
- TypeQualifier := (in \| of \| to) Expr is an ordinary expression position, not a literal slot; the Expression scope table does not itemize a qualifier-expression row separately — an unstated scope rule, recorded here rather than assumed — so this cell treats it by the same reasoning as the modifier-value-expression row: evaluated against the complete configuration rather than materialized in declaration order.
- This cell's schema also covers the other money-anchored business-domain division sites in this group sharing the identical 'Divisor must be non-zero' NumericProofRequirement and the identical class-(a) discharge shape at this evaluation-site category: MoneyDivideMoneySameCurrency, MoneyDivideMoneyCrossCurrency, MoneyDivideQuantity, MoneyDividePrice (its numeric-1 requirement), PriceDivideDecimal, PriceDivideQuantity, and ExchangeRateDivideDecimal. MoneyDividePrice, MoneyDivideQuantity, and PriceDivideQuantity additionally carry a QualifierChain currency-match requirement (a separate obligation at a different site id — currency compatibility, not divisor non-zero — not covered by this cell).

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault prevention obligation family
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: fault case shape row
- docs/language/precept-language-spec.md:1116 — spec: TypeQualifier := (in \| of \| to) Expr — the qualifier's value is an arbitrary expression, not a restricted literal slot
- src/Precept/Language/Operations.cs:442 — code: OperationKind.MoneyDivideDecimal — money / decimal → money (scaling); ProofRequirements: [NumericProofRequirement(divisor decimal, NotEquals, 0m, "Divisor must be non-zero")]

## g11/type-qualifier-quantity — Type qualifier expression — QuantityDivideDecimal

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § The cell — matrix: item 1 — fault-family obligation schema is the catalog-declared safety precondition at the evaluation site
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — an explicit input — matrix: fault minting is catalog-stamped at evaluation sites

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | QuantityDivideDecimal |
| evaluation site category | type-qualifier-expression |
| type family | business-domain |

### What must be proven

Obligation: BillingCycleDays != 0

Weakest precondition: BillingCycleDays != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| BillingCycleDays | the decimal-typed field read as the divisor of the business-domain division inside this declaration position |
| ReferenceQuantity | the quantity-typed field read as the numerator/dividend |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a).

TypeQualifier := (in \| of \| to) Expr is an ordinary expression position, not a literal slot; the Expression scope table does not itemize a qualifier-expression row separately — an unstated scope rule, recorded here rather than assumed — so this cell treats it by the same reasoning as the modifier-value-expression row: evaluated against the complete configuration rather than materialized in declaration order. No event args, no handler guard, and no pre-state fact are in scope at this position (type-qualifier carries only class (a) per the evaluation-site sweep); the only fact a derivation can consume is a declared modifier on the divisor field itself.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: the divisor field (BillingCycleDays)'s own declared modifier (`positive` or `nonzero`) supplies a bound on exactly the field the division reads as its divisor -> divisor-interval excludes 0, by the same interval-arithmetic reasoning Family 4 states for an event-arg bound, carried here to a field's own assignment-governed modifier instead of an event's ingress-governed one
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor's interval from its declared modifiers (a finite set per site — here, the divisor field's own min/max/positive/nonnegative/nonzero modifiers); zero inside the interval, or no bound excluding it, rejects. Family 4's decision procedure text names 'declared modifiers' generically, not restricted to event-arg modifiers, so this cell instantiates it with class (a) in place of class (b).

- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — fault family, arg-constraint discharge: decision procedure computing the divisor's interval from its declared modifiers
- src/Precept/Language/Operations.cs:543 — code: OperationKind.QuantityDivideDecimal — quantity / decimal → quantity (scaling); ProofRequirements: [NumericProofRequirement(divisor decimal, NotEquals, 0m, "Divisor must be non-zero")]
- src/Precept/Language/Modifiers.cs:84 — code: ModifierKind.Positive — value > 0; ProofSatisfaction.Numeric(SelfValue, GreaterThan, Constant(0m)); ZeroBoundNumericTypes includes Decimal
- src/Precept/Language/Modifiers.cs:16 — code: ZeroBoundNumericTypes array — Integer, Decimal, Number, Money, Quantity, Price, ExchangeRate, Duration, Period all admit positive/nonnegative/nonzero

### What the failing diagnostic must suggest

- For class (a): bound <Divisor> (`positive` or `nonzero`, or an explicit `min` excluding 0) such that its declared interval satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept G11TypeQualifierQtyBase
field BillingCycleDays as decimal default 0
field ReferenceQuantity as quantity in 'kg' default '100 kg'
field Amount as quantity of '{(ReferenceQuantity / BillingCycleDays).dimension}' optional
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*divisor-positive* — other

- Addition: BillingCycleDays as decimal positive default 1 (replacing the unconstrained `default 0` declaration)
- Premise classes: (a)
- Derivation: divisor field's own `positive` modifier excludes 0 from its interval -> divisor-interval derivation (class (a), Family 4's decision procedure)
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (this evaluation-site category mints no fault obligation at all at HEAD, so nothing distinguishes the base from this addition in the live run — both compile clean. The discharge is recorded as unresolved-today for the site-minting reason, not because the modifier itself failed to discharge anything.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` locus is recorded: the schema's application DU covers `row-guard` and `event-arg-declarations` only, and this addition replaces a FIELD declaration's own modifier/default — a locus the schema does not carry. Recorded as a missing-vocabulary item rather than forced into a mismatched locus.
- `validityArguments` cites 'Arg-bound interval arithmetic', whose written text is scoped to values 'entering as an event arg' (class (b)) — an extension to a field's own assignment-governed modifier (class (a)), not a verbatim match. The matrix has no validity argument written specifically for the fault family's own class-(a)-or-(b) divisor-interval derivation (Family 4 itself carries a decision procedure but no named validity-argument citation). Flagged, not silently assumed; see this file's top-level missing-rules note.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| divisor-positive | BillingCycleDays as decimal nonnegative default 0 (weakens `positive` to `nonnegative` — 0 is still admitted) | nonnegative admits 0 exactly; the divisor's interval still contains 0, so the obligation is not discharged — must still reject, same obligation. | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- No false-proof hazard: this evaluation-site category carries only premise class (a) (field modifiers) — never class (d) — so no discharge here can run through a business `rule` used as a premise (docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3, Defect B does not apply). Clean-compile confirmation of the field-modifier discharge would be admissible in principle; it is moot here because the site mints nothing at HEAD regardless.
- Live-verified at commit e1a14d91 (2026-07-21, Precept.MatrixTools): base — clean compile; only FieldNeverSet warnings and 'no computable obligations' — the qualifier's own division parses and type-checks (the '.dimension' accessor on the QuantityDivideDecimal result), and raises no fault diagnostic.
- Live-verified discharge (positive modifier) at the same commit: clean compile; 'BillingCycleDays:positive: WP = true' only.
- Live-verified near-miss (nonnegative modifier) at the same commit: clean compile; 'BillingCycleDays:nonnegative: WP = true' only.
- Model status for all three witnesses: provable-under-model (the obligation 'divisor must be non-zero' is catalog-declared and the matrix's fault case shape commits to a proof obligation at every evaluation site the catalog stamps, docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — an explicit input). Built status: unresolved-today for all three — this evaluation-site category mints no fault obligation at HEAD, so the base compiles clean instead of rejecting, and the discharge/near-miss additions change nothing observable. The `baseWitness`/`nearMissWitness` schema shapes carry no separate modelStatus/builtStatus split (only `dischargeWitness` does); the model/built distinction for the base and near-miss is recorded here in cell notes instead, by the same reasoning the schema states for discharge provenance: provenance attests the run, not the `expected` block, which states the definition's committed obligation.
- `noOtherDiagnostics: true` on the base is stated against the model's idealized reject scenario (only the divisor-nonzero obligation named); the live run's actual output carries incidental `FieldNeverSet` warnings (and, for the field-modifier category, the '[skipped obligation] ... bound is not a literal declared value' skip record already reported for the primitive lane in the parallel group) that are pre-existing structural noise unrelated to this obligation, not a second obligation on the same site.
- wp.canonicalKey / canonicalKeySource omitted: the WP calculator (tools/Precept.MatrixTools) computes establishment/preservation WPs through a handler write plan; this obligation sits at a declaration-position evaluation site with no write plan, and the calculator has no computation path for it. Not measured, not inferred.
- TypeQualifier := (in \| of \| to) Expr is an ordinary expression position, not a literal slot; the Expression scope table does not itemize a qualifier-expression row separately — an unstated scope rule, recorded here rather than assumed — so this cell treats it by the same reasoning as the modifier-value-expression row: evaluated against the complete configuration rather than materialized in declaration order.
- This cell's schema also covers the other quantity-anchored business-domain division sites in this group sharing the identical 'Divisor must be non-zero' NumericProofRequirement and the identical class-(a) discharge shape at this evaluation-site category: QuantityDivideQuantitySameDimension and QuantityDivideQuantityCrossDimension.

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault prevention obligation family
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: fault case shape row
- docs/language/precept-language-spec.md:1116 — spec: TypeQualifier := (in \| of \| to) Expr — the qualifier's value is an arbitrary expression, not a restricted literal slot
- src/Precept/Language/Operations.cs:543 — code: OperationKind.QuantityDivideDecimal — quantity / decimal → quantity (scaling); ProofRequirements: [NumericProofRequirement(divisor decimal, NotEquals, 0m, "Divisor must be non-zero")]

