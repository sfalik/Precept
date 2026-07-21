<!--
GENERATED FILE — do not hand-edit.
Source: fault-7-division-business-transition-write.cells.json
Regenerate: dotnet run --project tools/Precept.MatrixTools -- render-cells docs/Working/obligation-discharge-matrix-2026-07-19-cells
Hand edits will be overwritten. Edit the .cells.json and regenerate.
-->

# Division by zero with money, quantity, price and exchange-rate operands, at a transition-row write

Family id: fault-7-division-business-transition-write
Case shape: fault
Definition version: obligation-discharge-matrix-2026-07-19 rev 7 (2026-07-20, draft)

**Where this family's content comes from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the shared interval-exclude-zero story this whole group instantiates
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the fault-family case shape: catalog-declared safety precondition at the evaluation site, premise classes (b)/(c)/(a) per catalog ProofSatisfactions
- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault prevention obligation family
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — matrix: the false-proof hazard on premise (d) via a business rule; considered and found not to arise in this file — the group's shared setting always carries the divisor on the event, never as a pre-state field, so no witness here consumes premise (d)

## Respellability — the family verdict

Can every safe program these rules reject be rewritten into a form they accept? **yes**

Evidence: model-derived
Corpus measurement: pending

**Notes on the family verdict**

- Same story as Witness Family 1: the sound-but-unprovable band here is a divisor-non-zero fact spelled in a form no written derivation covers — for example a guard or a rule deriving the divisor's sign from an arithmetic relationship among other fields/args (`when A > B` where the divisor is `A - B`), rather than a direct bound or a normal-form-equal guard on the divisor itself. Its respelling is the direct bound (`positive`/`nonnegative` on the divisor arg) or a guard restating the divisor's own non-zero condition directly — the same respelling shape Witness Family 1 names.
- This verdict is about the MODEL's power, independent of the built-status gaps recorded per cell (the guard mechanism's non-discharge for business-domain-typed divisors, and the two DisambiguateCandidates reachability findings). Those are built-status facts, not respellability facts: the model licenses the same derivations regardless of divisor type family.

**Sources for this verdict**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Respellable definition
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Ratification protocol — matrix: layer 4: a family verdict ratifies only when corpus-measured

## Premise classes — the legend

Where a proof may get its facts (want :19, :143): (a) field modifiers, (b) arg constraints, (c) the handler's guard, (d) all rules holding in the pre-state (the inductive hypothesis), (e) evaluating the applicable rule set at an editable-field write — (e)'s status as a genuinely fifth class vs (a) generalized is an open owner item.

## g07/money-divide-decimal — Money ÷ decimal, transition-row write — the divisor is a decimal arg

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the interval-exclude-zero derivation this group instantiates (“compute the divisor's interval from its declared modifiers and in-scope guard facts… zero inside the interval, or no bound excluding it, rejects”)
- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault prevention

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | MoneyDivideDecimal |
| evaluation site category | transition-row-action-operand |
| type family | business-domain |

### What must be proven

Obligation: Split.Divisor != 0

Weakest precondition: Split.Divisor != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Divisor | the event arg supplying the divisor operand (decimal-typed) |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (b), (c).

The divisor is the triggering event's own arg (per the group's shared setting: “a divisor carried on the event”) and is not read elsewhere in the plan, so it is neither a written nor a pre-state-mentioned field — premise (d) has nothing to instantiate. Only (b) (the arg's own declared modifiers) and (c) (the row's guard) can supply the interval that excludes zero.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: guard fact normal-form-equal to the divisor's non-zero condition → guard-match
- Capability tier: 1a
- Validity arguments: Guard normal-form match
- Decision procedure: Compute the divisor's interval from its declared modifiers and in-scope guard facts (a finite set per site); zero inside the interval, or no bound excluding it, rejects. (Family 4's own decision-procedure text, restated for this operand pairing — the interval story is unchanged by the numerator/divisor type family.)

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the interval-exclude-zero derivation this group instantiates (“compute the divisor's interval from its declared modifiers and in-scope guard facts… zero inside the interval, or no bound excluding it, rejects”)
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: “Guard normal-form match” is stated in § Validity arguments against Witness Family 1's Bases A and C (a rule-preservation WP). Reusing it here is a direct corollary, not a separately-written argument: the same reasoning — a row fires only if its guard evaluated true, and the guard reads the same pre-state/args the action reads — applies unchanged when the proposition being matched is the fault family's “Divisor ≠ 0” instead of a rule's bound. No dedicated validity argument is written for the fault family in § Validity arguments; Family 4's own § Witnesses entry states the decision procedure but is not one of the six named arguments the schema enumerates. Flagged here rather than silently assumed.

**Entry 2 — (b)**

- Derivation: declared arg-modifier bound on the divisor (e.g. `positive`) excludes 0 from its interval → IntervalContainment
- Strategy: IntervalContainment
- Capability tier: 1a
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor's interval from its declared modifiers and in-scope guard facts (a finite set per site); zero inside the interval, or no bound excluding it, rejects. (Family 4's own decision-procedure text, restated for this operand pairing — the interval story is unchanged by the numerator/divisor type family.)

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the interval-exclude-zero derivation this group instantiates (“compute the divisor's interval from its declared modifiers and in-scope guard facts… zero inside the interval, or no bound excluding it, rejects”)
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: “Arg-bound interval arithmetic” is stated in § Validity arguments against Witness Family 1 Base A (summing two bounded args against a rule's bound). Reusing it here is a direct, simpler corollary: governance enforces the declared modifier on the event arg at ingress (precept-language-spec.md:268), so a `positive`-declared divisor's runtime value is provably > 0, hence ≠ 0 — no sum, no second term. As with the guard argument above, no validity argument is separately written for the fault family; this is the same reuse, flagged rather than assumed.

### What the failing diagnostic must suggest

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (b): bound the args such that the interval combination satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept BillingMoneyDecimal

field Subtotal as money in 'USD' default '0 USD'
field UnitShare as money in 'USD' default '0 USD'

state Draft initial
state Active

event Open(Amount as money in 'USD')
from Draft on Open
    -> set Subtotal = Open.Amount
    -> transition Active

event Split(Divisor as decimal)
from Active on Split
    -> set UnitShare = Subtotal / Split.Divisor
    -> no transition
```

Required outcome: reject, naming the missing premise classes (b), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*positive-arg* — arg-modifier

- Addition: Divisor as decimal positive
- Premise classes: (b)
- Derivation: declared `positive` modifier → Divisor > 0 → Divisor ≠ 0 (IntervalContainment)
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: applied by replacing the named event-arg declarations
  - `Split.Divisor` becomes `Divisor as decimal positive`

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

*guard-nonzero* — guard

- Addition: when Split.Divisor != 0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to `Divisor ≠ 0` → guard-match
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: appended to the guard of the `on Split` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| positive-arg | Divisor as decimal nonnegative | `nonnegative` admits 0 — the interval still contains zero; must still reject, same obligation | reject, naming the same obligation |
| guard-nonzero | when Split.Divisor >= 0 | `>= 0` admits 0 — does not imply the WP; must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Dual type-family site: MoneyDivideDecimal's declared operand types are Money (business-domain) and decimal (primitive), so the site is live at both type-family product coordinates. This one cell covers both: the coordinate recorded above (business-domain) reflects the numerator's family; the requirement's actual subject — the divisor — is the primitive decimal operand in every witness below. Stated explicitly per the group's authoring note so the family coordinate is not read as ambiguous.
- The divisor is a fresh event arg untouched earlier in the (single-write) plan, so the weakest precondition equals the catalog condition verbatim — no backward substitution is needed.
- The mechanized WP calculator (tools/Precept.MatrixTools, WpCalculator.*) targets rule-family write-plan WPs (Witness Family 1); it does not compute a canonicalKey for fault-family catalog preconditions in this build, so wp.canonicalKey is omitted rather than invented.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the interval-exclude-zero derivation this group instantiates (“compute the divisor's interval from its declared modifiers and in-scope guard facts… zero inside the interval, or no bound excluding it, rejects”)
- src/Precept/Language/Operations.cs:442 — catalog: MoneyDivideDecimal catalog entry — divisor subject is the decimal operand, single NumericProofRequirement at index 0

## g07/money-divide-money-same-currency — Money ÷ money (same currency), transition-row write — the divisor is a same-currency money arg

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the interval-exclude-zero derivation this group instantiates (“compute the divisor's interval from its declared modifiers and in-scope guard facts… zero inside the interval, or no bound excluding it, rejects”)
- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault prevention

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | MoneyDivideMoneySameCurrency |
| evaluation site category | transition-row-action-operand |
| type family | business-domain |

### What must be proven

Obligation: Split.Divisor != '0 USD'

Weakest precondition: Split.Divisor != '0 USD'

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Divisor | the event arg supplying the divisor operand (money-typed, same currency as the numerator) |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (b), (c).

The divisor is the triggering event's own money-typed arg and is not read elsewhere in the plan — premise (d) has nothing to instantiate. Only (b) and (c) can supply the interval that excludes zero.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: guard fact normal-form-equal to the divisor's non-zero condition → guard-match
- Capability tier: 1a
- Validity arguments: Guard normal-form match
- Decision procedure: Compute the divisor's interval from its declared modifiers and in-scope guard facts (a finite set per site); zero inside the interval, or no bound excluding it, rejects. (Family 4's own decision-procedure text, restated for this operand pairing — the interval story is unchanged by the numerator/divisor type family.)

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the interval-exclude-zero derivation this group instantiates (“compute the divisor's interval from its declared modifiers and in-scope guard facts… zero inside the interval, or no bound excluding it, rejects”)
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: “Guard normal-form match” is stated in § Validity arguments against Witness Family 1's Bases A and C (a rule-preservation WP). Reusing it here is a direct corollary, not a separately-written argument: the same reasoning — a row fires only if its guard evaluated true, and the guard reads the same pre-state/args the action reads — applies unchanged when the proposition being matched is the fault family's “Divisor ≠ 0” instead of a rule's bound. No dedicated validity argument is written for the fault family in § Validity arguments; Family 4's own § Witnesses entry states the decision procedure but is not one of the six named arguments the schema enumerates. Flagged here rather than silently assumed.

**Entry 2 — (b)**

- Derivation: declared arg-modifier bound on the divisor (e.g. `positive`) excludes 0 from its interval → IntervalContainment
- Strategy: IntervalContainment
- Capability tier: 1a
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor's interval from its declared modifiers and in-scope guard facts (a finite set per site); zero inside the interval, or no bound excluding it, rejects. (Family 4's own decision-procedure text, restated for this operand pairing — the interval story is unchanged by the numerator/divisor type family.)

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the interval-exclude-zero derivation this group instantiates (“compute the divisor's interval from its declared modifiers and in-scope guard facts… zero inside the interval, or no bound excluding it, rejects”)
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: “Arg-bound interval arithmetic” is stated in § Validity arguments against Witness Family 1 Base A (summing two bounded args against a rule's bound). Reusing it here is a direct, simpler corollary: governance enforces the declared modifier on the event arg at ingress (precept-language-spec.md:268), so a `positive`-declared divisor's runtime value is provably > 0, hence ≠ 0 — no sum, no second term. As with the guard argument above, no validity argument is separately written for the fault family; this is the same reuse, flagged rather than assumed.

### What the failing diagnostic must suggest

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (b): bound the args such that the interval combination satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept BillingMoneyMoneySame

field Subtotal as money in 'USD' default '0 USD'
field UnitFraction as decimal default 0.0

state Draft initial
state Active

event Open(Amount as money in 'USD')
from Draft on Open
    -> set Subtotal = Open.Amount
    -> transition Active

event Split(Divisor as money in 'USD')
from Active on Split
    -> set UnitFraction = Subtotal / Split.Divisor
    -> no transition
```

Required outcome: reject, naming the missing premise classes (b), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*positive-arg* — arg-modifier

- Addition: Divisor as money in 'USD' positive
- Premise classes: (b)
- Derivation: declared `positive` modifier → Divisor > '0 USD' → Divisor ≠ '0 USD' (IntervalContainment)
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: applied by replacing the named event-arg declarations
  - `Split.Divisor` becomes `Divisor as money in 'USD' positive`

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

*guard-nonzero* — guard

- Addition: when Split.Divisor != '0 USD'
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to `Divisor ≠ '0 USD'` → guard-match
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Live-verified at HEAD (e1a14d91): a guard restating the divisor's non-zero condition (`when Split.Divisor != '0 USD'`) does not discharge the obligation for a business-domain-typed divisor — the file still rejects with the identical DivisionByZero diagnostic. Tried both the literal-comparison spelling and, for money, the `.amount` projection; both fail identically. This is a genuine built-status gap, not a near-miss: the SAME guard spelling that discharges cleanly when the divisor is `decimal`-typed (MoneyDivideDecimal, QuantityDivideDecimal, PriceDivideDecimal, ExchangeRateDivideDecimal, all verified working in this file) does not discharge when the divisor is a business-domain type. Recorded as builtStatus: unresolved-today per the false-proof-hazard instructions' sibling rule — a live run demonstrating non-discharge, not a business rule used as a premise.)
- How it applies to the base: appended to the guard of the `on Split` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Also tried `when Split.Divisor.amount != 0` (the `.amount` projection) — same rejection, same diagnostic.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| positive-arg | Divisor as money in 'USD' nonnegative | `nonnegative` admits '0 USD' — must still reject, same obligation | reject, naming the same obligation |
| guard-nonzero | when Split.Divisor >= '0 USD' | `>= '0 USD'` admits '0 USD' — does not imply the WP; must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- The divisor is a fresh event arg untouched earlier in the plan, so the WP equals the catalog condition verbatim.
- wp.canonicalKey omitted: the mechanized WP calculator targets rule-family write-plan WPs, not fault-family catalog preconditions, in this build.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the interval-exclude-zero derivation this group instantiates (“compute the divisor's interval from its declared modifiers and in-scope guard facts… zero inside the interval, or no bound excluding it, rejects”)
- src/Precept/Language/Operations.cs:452 — catalog: MoneyDivideMoneySameCurrency catalog entry — Match: Same, divisor subject is the money operand

## g07/money-divide-money-cross-currency — Money ÷ money (cross currency), transition-row write — the divisor is a different-currency money arg

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the interval-exclude-zero derivation this group instantiates (“compute the divisor's interval from its declared modifiers and in-scope guard facts… zero inside the interval, or no bound excluding it, rejects”)
- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault prevention

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | MoneyDivideMoneyCrossCurrency |
| evaluation site category | transition-row-action-operand |
| type family | business-domain |

### What must be proven

Obligation: Split.Divisor != '0 EUR'

Weakest precondition: Split.Divisor != '0 EUR'

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Divisor | the event arg supplying the divisor operand (money-typed, currency different from the numerator) |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (b), (c).

The divisor is the triggering event's own money-typed arg and is not read elsewhere in the plan — premise (d) has nothing to instantiate. Only (b) and (c) can supply the interval that excludes zero, under the model. See the reachability note: at HEAD this obligation is always minted under MoneyDivideMoneySameCurrency's identity (identical requirement content), never this catalog entry's own.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: guard fact normal-form-equal to the divisor's non-zero condition → guard-match
- Capability tier: 1a
- Validity arguments: Guard normal-form match
- Decision procedure: Compute the divisor's interval from its declared modifiers and in-scope guard facts (a finite set per site); zero inside the interval, or no bound excluding it, rejects. (Family 4's own decision-procedure text, restated for this operand pairing — the interval story is unchanged by the numerator/divisor type family.)

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the interval-exclude-zero derivation this group instantiates (“compute the divisor's interval from its declared modifiers and in-scope guard facts… zero inside the interval, or no bound excluding it, rejects”)
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: “Guard normal-form match” is stated in § Validity arguments against Witness Family 1's Bases A and C (a rule-preservation WP). Reusing it here is a direct corollary, not a separately-written argument: the same reasoning — a row fires only if its guard evaluated true, and the guard reads the same pre-state/args the action reads — applies unchanged when the proposition being matched is the fault family's “Divisor ≠ 0” instead of a rule's bound. No dedicated validity argument is written for the fault family in § Validity arguments; Family 4's own § Witnesses entry states the decision procedure but is not one of the six named arguments the schema enumerates. Flagged here rather than silently assumed.

**Entry 2 — (b)**

- Derivation: declared arg-modifier bound on the divisor (e.g. `positive`) excludes 0 from its interval → IntervalContainment
- Strategy: IntervalContainment
- Capability tier: 1a
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor's interval from its declared modifiers and in-scope guard facts (a finite set per site); zero inside the interval, or no bound excluding it, rejects. (Family 4's own decision-procedure text, restated for this operand pairing — the interval story is unchanged by the numerator/divisor type family.)

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the interval-exclude-zero derivation this group instantiates (“compute the divisor's interval from its declared modifiers and in-scope guard facts… zero inside the interval, or no bound excluding it, rejects”)
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: “Arg-bound interval arithmetic” is stated in § Validity arguments against Witness Family 1 Base A (summing two bounded args against a rule's bound). Reusing it here is a direct, simpler corollary: governance enforces the declared modifier on the event arg at ingress (precept-language-spec.md:268), so a `positive`-declared divisor's runtime value is provably > 0, hence ≠ 0 — no sum, no second term. As with the guard argument above, no validity argument is separately written for the fault family; this is the same reuse, flagged rather than assumed.

### What the failing diagnostic must suggest

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (b): bound the args such that the interval combination satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept BillingMoneyMoneyCrossProbe

field BaseCurrency as currency default 'USD'
field QuoteCurrency as currency default 'EUR'
field Subtotal as money in '{BaseCurrency}' default '0 {BaseCurrency}'
field Ratio as decimal default 0.0

state Draft initial
state Active

event Open(Amount as money in '{BaseCurrency}')
from Draft on Open
    -> set BaseCurrency = 'USD'
    -> set QuoteCurrency = 'EUR'
    -> set Subtotal = Open.Amount
    -> transition Active

event Split(Divisor as money in '{QuoteCurrency}')
from Active on Split
    -> set Ratio = Subtotal / Split.Divisor
    -> no transition
```

Required outcome: reject, naming the missing premise classes (b), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*positive-arg* — arg-modifier

- Addition: Divisor as money in '{QuoteCurrency}' positive
- Premise classes: (b)
- Derivation: declared `positive` modifier → Divisor > 0 → Divisor ≠ 0 (IntervalContainment)
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: applied by replacing the named event-arg declarations
  - `Split.Divisor` becomes `Divisor as money in '{QuoteCurrency}' positive`

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified as compiling clean when the result is assigned to a `decimal` field. Per the reachability note, this exercises MoneyDivideMoneySameCurrency's identity at HEAD (identical requirement content), not MoneyDivideMoneyCrossCurrency's own — recorded honestly rather than claimed as a Cross-specific run.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| positive-arg | Divisor as money in '{QuoteCurrency}' nonnegative | `nonnegative` admits zero — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

- The model's respellability story is unchanged from Cell 2 (Same currency) — the reachability finding is a built-status matter, not a model-power matter.

**What the sources leave unstated or ambiguous here**

- Verified reachability limitation (HEAD e1a14d91): the catalog declares MoneyDivideMoneySameCurrency and MoneyDivideMoneyCrossCurrency as two candidates for the (Divide, Money, Money) type pair, but Operations.DisambiguateCandidates (src/Precept/Language/Operations.cs:1305-1326) “selects the Same entry by default” whenever more than one candidate exists — unconditionally, not based on any runtime or static qualifier fact. Consequences, both live-verified: (1) with STATICALLY same-vs-different literal currencies known ('USD' vs 'EUR'), the pairwise qualifier check (TypeChecker.Expressions.cs:1298-1314, PRE0070) fires first and rejects the file outright with CrossCurrencyArithmetic, before any divide-specific reasoning runs — unlike the equivalent Quantity/Quantity check, which explicitly excludes Divide (TypeChecker.Expressions.cs:1324-1325) with a comment naming the reason; the Money/Money check carries no such exclusion. (2) With DYNAMIC (field-interpolated) currency qualifiers — bypassing PRE0070 — the type-checker still always resolves the expression via MoneyDivideMoneySameCurrency's Decimal result type, never Cross's ExchangeRate result type; assigning to an exchangerate-typed field yields TypeMismatch (probed directly and confirmed) and assigning to a decimal-typed field silently compiles as the Same-currency meta regardless of the operands' actual runtime currencies. Net effect: no surface spelling reaches MoneyDivideMoneyCrossCurrency as its own catalog identity — every money/money division is type-checked, and therefore obligation-minted, under MoneyDivideMoneySameCurrency's identity. Because both catalog entries carry byte-for-byte identical NumericProofRequirement content (“Divisor must be non-zero”, subject = the money operand), the FAULT OBLIGATION itself is unaffected in substance — but this cell's catalog site, as its own distinct identity, is unreachable. This is a built-status / reachability finding about HEAD, not a gap in the model: the model's obligation and discharge story for this cell are exactly Family 4's interval-exclude-zero story, unchanged. Recorded here rather than papered over with an invented distinguishing witness.
- No guard-discharge witness is recorded for this cell: given the guard mechanism already fails for the reachable Same-currency identity (Cell 2, live-verified), and this cell's own identity is itself unreachable, adding a guard witness here would not demonstrate anything the reachability note does not already state.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the interval-exclude-zero derivation this group instantiates (“compute the divisor's interval from its declared modifiers and in-scope guard facts… zero inside the interval, or no bound excluding it, rejects”)
- src/Precept/Language/Operations.cs:462 — catalog: MoneyDivideMoneyCrossCurrency catalog entry — Match: Different, divisor subject is the money operand
- src/Precept/Language/Operations.cs:1315 — code: DisambiguateCandidates — selects the Same-qualifier entry by default when multiple candidates exist

## g07/money-divide-quantity — Money ÷ quantity, transition-row write — the divisor is a quantity arg

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the interval-exclude-zero derivation this group instantiates (“compute the divisor's interval from its declared modifiers and in-scope guard facts… zero inside the interval, or no bound excluding it, rejects”)
- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault prevention

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | MoneyDivideQuantity |
| evaluation site category | transition-row-action-operand |
| type family | business-domain |

### What must be proven

Obligation: Split.Divisor != '0 kg'

Weakest precondition: Split.Divisor != '0 kg'

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Divisor | the event arg supplying the divisor operand (quantity-typed) |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (b), (c).

The divisor is the triggering event's own quantity-typed arg and is not read elsewhere in the plan — premise (d) has nothing to instantiate. Only (b) and (c) can supply the interval that excludes zero.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: guard fact normal-form-equal to the divisor's non-zero condition → guard-match
- Capability tier: 1a
- Validity arguments: Guard normal-form match
- Decision procedure: Compute the divisor's interval from its declared modifiers and in-scope guard facts (a finite set per site); zero inside the interval, or no bound excluding it, rejects. (Family 4's own decision-procedure text, restated for this operand pairing — the interval story is unchanged by the numerator/divisor type family.)

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the interval-exclude-zero derivation this group instantiates (“compute the divisor's interval from its declared modifiers and in-scope guard facts… zero inside the interval, or no bound excluding it, rejects”)
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: “Guard normal-form match” is stated in § Validity arguments against Witness Family 1's Bases A and C (a rule-preservation WP). Reusing it here is a direct corollary, not a separately-written argument: the same reasoning — a row fires only if its guard evaluated true, and the guard reads the same pre-state/args the action reads — applies unchanged when the proposition being matched is the fault family's “Divisor ≠ 0” instead of a rule's bound. No dedicated validity argument is written for the fault family in § Validity arguments; Family 4's own § Witnesses entry states the decision procedure but is not one of the six named arguments the schema enumerates. Flagged here rather than silently assumed.

**Entry 2 — (b)**

- Derivation: declared arg-modifier bound on the divisor (e.g. `positive`) excludes 0 from its interval → IntervalContainment
- Strategy: IntervalContainment
- Capability tier: 1a
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor's interval from its declared modifiers and in-scope guard facts (a finite set per site); zero inside the interval, or no bound excluding it, rejects. (Family 4's own decision-procedure text, restated for this operand pairing — the interval story is unchanged by the numerator/divisor type family.)

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the interval-exclude-zero derivation this group instantiates (“compute the divisor's interval from its declared modifiers and in-scope guard facts… zero inside the interval, or no bound excluding it, rejects”)
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: “Arg-bound interval arithmetic” is stated in § Validity arguments against Witness Family 1 Base A (summing two bounded args against a rule's bound). Reusing it here is a direct, simpler corollary: governance enforces the declared modifier on the event arg at ingress (precept-language-spec.md:268), so a `positive`-declared divisor's runtime value is provably > 0, hence ≠ 0 — no sum, no second term. As with the guard argument above, no validity argument is separately written for the fault family; this is the same reuse, flagged rather than assumed.

### What the failing diagnostic must suggest

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (b): bound the args such that the interval combination satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept BillingMoneyQuantity

field Subtotal as money in 'USD' default '0 USD'
field UnitPrice as price in 'USD/kg' default '0 USD/kg'

state Draft initial
state Active

event Open(Amount as money in 'USD')
from Draft on Open
    -> set Subtotal = Open.Amount
    -> transition Active

event Split(Divisor as quantity in 'kg')
from Active on Split
    -> set UnitPrice = Subtotal / Split.Divisor
    -> no transition
```

Required outcome: reject, naming the missing premise classes (b), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*positive-arg* — arg-modifier

- Addition: Divisor as quantity in 'kg' positive
- Premise classes: (b)
- Derivation: declared `positive` modifier → Divisor > '0 kg' → Divisor ≠ '0 kg' (IntervalContainment)
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: applied by replacing the named event-arg declarations
  - `Split.Divisor` becomes `Divisor as quantity in 'kg' positive`

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

*guard-nonzero* — guard

- Addition: when Split.Divisor != '0 kg'
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to `Divisor ≠ '0 kg'` → guard-match
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Live-verified at HEAD (e1a14d91): a guard restating the divisor's non-zero condition (`when Split.Divisor != '0 kg'`) does not discharge the obligation for a business-domain-typed divisor — the file still rejects with the identical DivisionByZero diagnostic. Tried both the literal-comparison spelling and, for money, the `.amount` projection; both fail identically. This is a genuine built-status gap, not a near-miss: the SAME guard spelling that discharges cleanly when the divisor is `decimal`-typed (MoneyDivideDecimal, QuantityDivideDecimal, PriceDivideDecimal, ExchangeRateDivideDecimal, all verified working in this file) does not discharge when the divisor is a business-domain type. Recorded as builtStatus: unresolved-today per the false-proof-hazard instructions' sibling rule — a live run demonstrating non-discharge, not a business rule used as a premise.)
- How it applies to the base: appended to the guard of the `on Split` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| positive-arg | Divisor as quantity in 'kg' nonnegative | `nonnegative` admits '0 kg' — must still reject, same obligation | reject, naming the same obligation |
| guard-nonzero | when Split.Divisor >= '0 kg' | `>= '0 kg'` admits '0 kg' — does not imply the WP; must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- The divisor is a fresh event arg untouched earlier in the plan, so the WP equals the catalog condition verbatim.
- wp.canonicalKey omitted for the same reason as Cell 1.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the interval-exclude-zero derivation this group instantiates (“compute the divisor's interval from its declared modifiers and in-scope guard facts… zero inside the interval, or no bound excluding it, rejects”)
- src/Precept/Language/Operations.cs:472 — catalog: MoneyDivideQuantity catalog entry — divisor subject is the quantity operand

## g07/money-divide-price — Money ÷ price, transition-row write — the divisor is a price arg (numeric-1: the non-zero requirement, not the currency-match requirement at index 0)

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the interval-exclude-zero derivation this group instantiates (“compute the divisor's interval from its declared modifiers and in-scope guard facts… zero inside the interval, or no bound excluding it, rejects”)
- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault prevention

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | MoneyDividePrice |
| evaluation site category | transition-row-action-operand |
| type family | business-domain |

### What must be proven

Obligation: Split.Divisor != '0 USD/kg'

Weakest precondition: Split.Divisor != '0 USD/kg'

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Divisor | the event arg supplying the divisor operand (price-typed ('USD/kg')) |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (b), (c).

The divisor is the triggering event's own price-typed arg and is not read elsewhere in the plan — premise (d) has nothing to instantiate. Only (b) and (c) can supply the interval that excludes zero. This cell covers only the numeric-1 (non-zero divisor) requirement; the qualifier-chain currency-match requirement at numeric index 0 is a different requirement kind (QualifierChain, not Numeric) and is out of this group's scope per the authoring note — it is dispositioned open elsewhere (disposition-summary: “no premise class supplies type-qualifier facts”). The base program satisfies that other requirement by construction (both operands are 'USD') so it rejects for this cell's obligation alone.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: guard fact normal-form-equal to the divisor's non-zero condition → guard-match
- Capability tier: 1a
- Validity arguments: Guard normal-form match
- Decision procedure: Compute the divisor's interval from its declared modifiers and in-scope guard facts (a finite set per site); zero inside the interval, or no bound excluding it, rejects. (Family 4's own decision-procedure text, restated for this operand pairing — the interval story is unchanged by the numerator/divisor type family.)

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the interval-exclude-zero derivation this group instantiates (“compute the divisor's interval from its declared modifiers and in-scope guard facts… zero inside the interval, or no bound excluding it, rejects”)
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: “Guard normal-form match” is stated in § Validity arguments against Witness Family 1's Bases A and C (a rule-preservation WP). Reusing it here is a direct corollary, not a separately-written argument: the same reasoning — a row fires only if its guard evaluated true, and the guard reads the same pre-state/args the action reads — applies unchanged when the proposition being matched is the fault family's “Divisor ≠ 0” instead of a rule's bound. No dedicated validity argument is written for the fault family in § Validity arguments; Family 4's own § Witnesses entry states the decision procedure but is not one of the six named arguments the schema enumerates. Flagged here rather than silently assumed.

**Entry 2 — (b)**

- Derivation: declared arg-modifier bound on the divisor (e.g. `positive`) excludes 0 from its interval → IntervalContainment
- Strategy: IntervalContainment
- Capability tier: 1a
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor's interval from its declared modifiers and in-scope guard facts (a finite set per site); zero inside the interval, or no bound excluding it, rejects. (Family 4's own decision-procedure text, restated for this operand pairing — the interval story is unchanged by the numerator/divisor type family.)

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the interval-exclude-zero derivation this group instantiates (“compute the divisor's interval from its declared modifiers and in-scope guard facts… zero inside the interval, or no bound excluding it, rejects”)
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: “Arg-bound interval arithmetic” is stated in § Validity arguments against Witness Family 1 Base A (summing two bounded args against a rule's bound). Reusing it here is a direct, simpler corollary: governance enforces the declared modifier on the event arg at ingress (precept-language-spec.md:268), so a `positive`-declared divisor's runtime value is provably > 0, hence ≠ 0 — no sum, no second term. As with the guard argument above, no validity argument is separately written for the fault family; this is the same reuse, flagged rather than assumed.

### What the failing diagnostic must suggest

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (b): bound the args such that the interval combination satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept BillingMoneyPrice

field Subtotal as money in 'USD' default '0 USD'
field Qty as quantity in 'kg' default '0 kg'

state Draft initial
state Active

event Open(Amount as money in 'USD')
from Draft on Open
    -> set Subtotal = Open.Amount
    -> transition Active

event Split(Divisor as price in 'USD/kg')
from Active on Split
    -> set Qty = Subtotal / Split.Divisor
    -> no transition
```

Required outcome: reject, naming the missing premise classes (b), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*positive-arg* — arg-modifier

- Addition: Divisor as price in 'USD/kg' positive
- Premise classes: (b)
- Derivation: declared `positive` modifier → Divisor > '0 USD/kg' → Divisor ≠ '0 USD/kg' (IntervalContainment)
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: applied by replacing the named event-arg declarations
  - `Split.Divisor` becomes `Divisor as price in 'USD/kg' positive`

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

*guard-nonzero* — guard

- Addition: when Split.Divisor != '0 USD/kg'
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to `Divisor ≠ '0 USD/kg'` → guard-match
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Live-verified at HEAD (e1a14d91): a guard restating the divisor's non-zero condition (`when Split.Divisor != '0 USD/kg'`) does not discharge the obligation for a business-domain-typed divisor — the file still rejects with the identical DivisionByZero diagnostic. Tried both the literal-comparison spelling and, for money, the `.amount` projection; both fail identically. This is a genuine built-status gap, not a near-miss: the SAME guard spelling that discharges cleanly when the divisor is `decimal`-typed (MoneyDivideDecimal, QuantityDivideDecimal, PriceDivideDecimal, ExchangeRateDivideDecimal, all verified working in this file) does not discharge when the divisor is a business-domain type. Recorded as builtStatus: unresolved-today per the false-proof-hazard instructions' sibling rule — a live run demonstrating non-discharge, not a business rule used as a premise.)
- How it applies to the base: appended to the guard of the `on Split` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| positive-arg | Divisor as price in 'USD/kg' nonnegative | `nonnegative` admits '0 USD/kg' — must still reject, same obligation | reject, naming the same obligation |
| guard-nonzero | when Split.Divisor >= '0 USD/kg' | `>= '0 USD/kg'` admits '0 USD/kg' — does not imply the WP; must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- The divisor is a fresh event arg untouched earlier in the plan, so the WP equals the catalog condition verbatim.
- wp.canonicalKey omitted for the same reason as Cell 1.
- The guard near-miss is model-derived, not live-verified: only the guard *discharge* (which already fails to compile, per builtStatus above) was run live; a separately weakened guard was not additionally tested for this site.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the interval-exclude-zero derivation this group instantiates (“compute the divisor's interval from its declared modifiers and in-scope guard facts… zero inside the interval, or no bound excluding it, rejects”)
- src/Precept/Language/Operations.cs:486 — catalog: MoneyDividePrice catalog entry — two ProofRequirements: index 0 QualifierChain (currency match, out of scope), index 1 Numeric (divisor ≠ 0, this cell)

## g07/quantity-divide-decimal — Quantity ÷ decimal, transition-row write — the divisor is a decimal arg

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the interval-exclude-zero derivation this group instantiates (“compute the divisor's interval from its declared modifiers and in-scope guard facts… zero inside the interval, or no bound excluding it, rejects”)
- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault prevention

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | QuantityDivideDecimal |
| evaluation site category | transition-row-action-operand |
| type family | business-domain |

### What must be proven

Obligation: Split.Divisor != 0

Weakest precondition: Split.Divisor != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Divisor | the event arg supplying the divisor operand (decimal-typed) |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (b), (c).

The divisor is the triggering event's own decimal-typed arg and is not read elsewhere in the plan — premise (d) has nothing to instantiate. Only (b) and (c) can supply the interval that excludes zero.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: guard fact normal-form-equal to the divisor's non-zero condition → guard-match
- Capability tier: 1a
- Validity arguments: Guard normal-form match
- Decision procedure: Compute the divisor's interval from its declared modifiers and in-scope guard facts (a finite set per site); zero inside the interval, or no bound excluding it, rejects. (Family 4's own decision-procedure text, restated for this operand pairing — the interval story is unchanged by the numerator/divisor type family.)

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the interval-exclude-zero derivation this group instantiates (“compute the divisor's interval from its declared modifiers and in-scope guard facts… zero inside the interval, or no bound excluding it, rejects”)
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: “Guard normal-form match” is stated in § Validity arguments against Witness Family 1's Bases A and C (a rule-preservation WP). Reusing it here is a direct corollary, not a separately-written argument: the same reasoning — a row fires only if its guard evaluated true, and the guard reads the same pre-state/args the action reads — applies unchanged when the proposition being matched is the fault family's “Divisor ≠ 0” instead of a rule's bound. No dedicated validity argument is written for the fault family in § Validity arguments; Family 4's own § Witnesses entry states the decision procedure but is not one of the six named arguments the schema enumerates. Flagged here rather than silently assumed.

**Entry 2 — (b)**

- Derivation: declared arg-modifier bound on the divisor (e.g. `positive`) excludes 0 from its interval → IntervalContainment
- Strategy: IntervalContainment
- Capability tier: 1a
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor's interval from its declared modifiers and in-scope guard facts (a finite set per site); zero inside the interval, or no bound excluding it, rejects. (Family 4's own decision-procedure text, restated for this operand pairing — the interval story is unchanged by the numerator/divisor type family.)

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the interval-exclude-zero derivation this group instantiates (“compute the divisor's interval from its declared modifiers and in-scope guard facts… zero inside the interval, or no bound excluding it, rejects”)
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: “Arg-bound interval arithmetic” is stated in § Validity arguments against Witness Family 1 Base A (summing two bounded args against a rule's bound). Reusing it here is a direct, simpler corollary: governance enforces the declared modifier on the event arg at ingress (precept-language-spec.md:268), so a `positive`-declared divisor's runtime value is provably > 0, hence ≠ 0 — no sum, no second term. As with the guard argument above, no validity argument is separately written for the fault family; this is the same reuse, flagged rather than assumed.

### What the failing diagnostic must suggest

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (b): bound the args such that the interval combination satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept BillingQuantityDecimal

field Qty as quantity in 'kg' default '0 kg'
field UnitShare as quantity in 'kg' default '0 kg'

state Draft initial
state Active

event Open(Amount as quantity in 'kg')
from Draft on Open
    -> set Qty = Open.Amount
    -> transition Active

event Split(Divisor as decimal)
from Active on Split
    -> set UnitShare = Qty / Split.Divisor
    -> no transition
```

Required outcome: reject, naming the missing premise classes (b), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*positive-arg* — arg-modifier

- Addition: Divisor as decimal positive
- Premise classes: (b)
- Derivation: declared `positive` modifier → Divisor > 0 → Divisor ≠ 0 (IntervalContainment)
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: applied by replacing the named event-arg declarations
  - `Split.Divisor` becomes `Divisor as decimal positive`

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

*guard-nonzero* — guard

- Addition: when Split.Divisor != 0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to `Divisor ≠ 0` → guard-match
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: appended to the guard of the `on Split` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| positive-arg | Divisor as decimal nonnegative | `nonnegative` admits 0 — must still reject, same obligation | reject, naming the same obligation |
| guard-nonzero | when Split.Divisor >= 0 | `>= 0` admits 0 — does not imply the WP; must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Dual type-family site, same reasoning as Cell 1: QuantityDivideDecimal's declared operand types are Quantity (business-domain) and decimal (primitive); the coordinate recorded here (business-domain) reflects the numerator's family, while the requirement's subject — the divisor — is the primitive decimal operand.
- The divisor is a fresh event arg untouched earlier in the plan, so the WP equals the catalog condition verbatim.
- wp.canonicalKey omitted for the same reason as Cell 1.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the interval-exclude-zero derivation this group instantiates (“compute the divisor's interval from its declared modifiers and in-scope guard facts… zero inside the interval, or no bound excluding it, rejects”)
- src/Precept/Language/Operations.cs:543 — catalog: QuantityDivideDecimal catalog entry — divisor subject is the decimal operand

## g07/quantity-divide-quantity-same-dimension — Quantity ÷ quantity (same dimension), transition-row write — the divisor is a same-unit quantity arg

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the interval-exclude-zero derivation this group instantiates (“compute the divisor's interval from its declared modifiers and in-scope guard facts… zero inside the interval, or no bound excluding it, rejects”)
- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault prevention

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | QuantityDivideQuantitySameDimension |
| evaluation site category | transition-row-action-operand |
| type family | business-domain |

### What must be proven

Obligation: Split.Divisor != '0 kg'

Weakest precondition: Split.Divisor != '0 kg'

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Divisor | the event arg supplying the divisor operand (quantity-typed, same dimension as the numerator) |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (b), (c).

The divisor is the triggering event's own quantity-typed arg and is not read elsewhere in the plan — premise (d) has nothing to instantiate. Only (b) and (c) can supply the interval that excludes zero. Unlike Money/Money and Quantity/Quantity-cross-dimension (Cells 3 and 8), this site has no reachability problem: Operations.DisambiguateCandidates always resolves a multi-candidate (Divide, Quantity, Quantity) pair to the `Match: Same` entry, which is exactly this catalog member — it is the one every plain `quantity / quantity` division actually type-checks as.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: guard fact normal-form-equal to the divisor's non-zero condition → guard-match
- Capability tier: 1a
- Validity arguments: Guard normal-form match
- Decision procedure: Compute the divisor's interval from its declared modifiers and in-scope guard facts (a finite set per site); zero inside the interval, or no bound excluding it, rejects. (Family 4's own decision-procedure text, restated for this operand pairing — the interval story is unchanged by the numerator/divisor type family.)

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the interval-exclude-zero derivation this group instantiates (“compute the divisor's interval from its declared modifiers and in-scope guard facts… zero inside the interval, or no bound excluding it, rejects”)
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: “Guard normal-form match” is stated in § Validity arguments against Witness Family 1's Bases A and C (a rule-preservation WP). Reusing it here is a direct corollary, not a separately-written argument: the same reasoning — a row fires only if its guard evaluated true, and the guard reads the same pre-state/args the action reads — applies unchanged when the proposition being matched is the fault family's “Divisor ≠ 0” instead of a rule's bound. No dedicated validity argument is written for the fault family in § Validity arguments; Family 4's own § Witnesses entry states the decision procedure but is not one of the six named arguments the schema enumerates. Flagged here rather than silently assumed.

**Entry 2 — (b)**

- Derivation: declared arg-modifier bound on the divisor (e.g. `positive`) excludes 0 from its interval → IntervalContainment
- Strategy: IntervalContainment
- Capability tier: 1a
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor's interval from its declared modifiers and in-scope guard facts (a finite set per site); zero inside the interval, or no bound excluding it, rejects. (Family 4's own decision-procedure text, restated for this operand pairing — the interval story is unchanged by the numerator/divisor type family.)

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the interval-exclude-zero derivation this group instantiates (“compute the divisor's interval from its declared modifiers and in-scope guard facts… zero inside the interval, or no bound excluding it, rejects”)
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: “Arg-bound interval arithmetic” is stated in § Validity arguments against Witness Family 1 Base A (summing two bounded args against a rule's bound). Reusing it here is a direct, simpler corollary: governance enforces the declared modifier on the event arg at ingress (precept-language-spec.md:268), so a `positive`-declared divisor's runtime value is provably > 0, hence ≠ 0 — no sum, no second term. As with the guard argument above, no validity argument is separately written for the fault family; this is the same reuse, flagged rather than assumed.

### What the failing diagnostic must suggest

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (b): bound the args such that the interval combination satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept BillingQuantityQuantitySame

field Qty as quantity in 'kg' default '0 kg'
field UnitFraction as decimal default 0.0

state Draft initial
state Active

event Open(Amount as quantity in 'kg')
from Draft on Open
    -> set Qty = Open.Amount
    -> transition Active

event Split(Divisor as quantity in 'kg')
from Active on Split
    -> set UnitFraction = Qty / Split.Divisor
    -> no transition
```

Required outcome: reject, naming the missing premise classes (b), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*positive-arg* — arg-modifier

- Addition: Divisor as quantity in 'kg' positive
- Premise classes: (b)
- Derivation: declared `positive` modifier → Divisor > '0 kg' → Divisor ≠ '0 kg' (IntervalContainment)
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: applied by replacing the named event-arg declarations
  - `Split.Divisor` becomes `Divisor as quantity in 'kg' positive`

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

*guard-nonzero* — guard

- Addition: when Split.Divisor != '0 kg'
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to `Divisor ≠ '0 kg'` → guard-match
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Live-verified at HEAD (e1a14d91): a guard restating the divisor's non-zero condition (`when Split.Divisor != '0 kg' (probed as != '0 m' on an equivalent same-dimension setting)`) does not discharge the obligation for a business-domain-typed divisor — the file still rejects with the identical DivisionByZero diagnostic. Tried both the literal-comparison spelling and, for money, the `.amount` projection; both fail identically. This is a genuine built-status gap, not a near-miss: the SAME guard spelling that discharges cleanly when the divisor is `decimal`-typed (MoneyDivideDecimal, QuantityDivideDecimal, PriceDivideDecimal, ExchangeRateDivideDecimal, all verified working in this file) does not discharge when the divisor is a business-domain type. Recorded as builtStatus: unresolved-today per the false-proof-hazard instructions' sibling rule — a live run demonstrating non-discharge, not a business rule used as a premise.)
- How it applies to the base: appended to the guard of the `on Split` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| positive-arg | Divisor as quantity in 'kg' nonnegative | `nonnegative` admits '0 kg' — must still reject, same obligation | reject, naming the same obligation |
| guard-nonzero | when Split.Divisor >= '0 kg' | `>= '0 kg'` admits '0 kg' — does not imply the WP; must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- The divisor is a fresh event arg untouched earlier in the plan, so the WP equals the catalog condition verbatim.
- wp.canonicalKey omitted for the same reason as Cell 1.
- Guard discharge live-verified as failing on an equivalent setting (`m` distance / `m` divisor rather than `kg`/`kg`) — the unit choice is not load-bearing for the finding; the same-dimension pairing is.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the interval-exclude-zero derivation this group instantiates (“compute the divisor's interval from its declared modifiers and in-scope guard facts… zero inside the interval, or no bound excluding it, rejects”)
- src/Precept/Language/Operations.cs:553 — catalog: QuantityDivideQuantitySameDimension catalog entry — Match: Same, divisor subject is the quantity operand

## g07/quantity-divide-quantity-cross-dimension — Quantity ÷ quantity (cross dimension), transition-row write — the divisor is a different-dimension quantity arg

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the interval-exclude-zero derivation this group instantiates (“compute the divisor's interval from its declared modifiers and in-scope guard facts… zero inside the interval, or no bound excluding it, rejects”)
- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault prevention

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | QuantityDivideQuantityCrossDimension |
| evaluation site category | transition-row-action-operand |
| type family | business-domain |

### What must be proven

Obligation: Split.Divisor != '0 s'

Weakest precondition: Split.Divisor != '0 s'

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Divisor | the event arg supplying the divisor operand (quantity-typed, dimension different from the numerator) |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (b), (c).

The divisor is the triggering event's own quantity-typed arg and is not read elsewhere in the plan — premise (d) has nothing to instantiate. Only (b) and (c) can supply the interval that excludes zero, under the model. See the reachability note: at HEAD this obligation is always minted under QuantityDivideQuantitySameDimension's identity (identical requirement content), never this catalog entry's own.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: guard fact normal-form-equal to the divisor's non-zero condition → guard-match
- Capability tier: 1a
- Validity arguments: Guard normal-form match
- Decision procedure: Compute the divisor's interval from its declared modifiers and in-scope guard facts (a finite set per site); zero inside the interval, or no bound excluding it, rejects. (Family 4's own decision-procedure text, restated for this operand pairing — the interval story is unchanged by the numerator/divisor type family.)

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the interval-exclude-zero derivation this group instantiates (“compute the divisor's interval from its declared modifiers and in-scope guard facts… zero inside the interval, or no bound excluding it, rejects”)
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: “Guard normal-form match” is stated in § Validity arguments against Witness Family 1's Bases A and C (a rule-preservation WP). Reusing it here is a direct corollary, not a separately-written argument: the same reasoning — a row fires only if its guard evaluated true, and the guard reads the same pre-state/args the action reads — applies unchanged when the proposition being matched is the fault family's “Divisor ≠ 0” instead of a rule's bound. No dedicated validity argument is written for the fault family in § Validity arguments; Family 4's own § Witnesses entry states the decision procedure but is not one of the six named arguments the schema enumerates. Flagged here rather than silently assumed.

**Entry 2 — (b)**

- Derivation: declared arg-modifier bound on the divisor (e.g. `positive`) excludes 0 from its interval → IntervalContainment
- Strategy: IntervalContainment
- Capability tier: 1a
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor's interval from its declared modifiers and in-scope guard facts (a finite set per site); zero inside the interval, or no bound excluding it, rejects. (Family 4's own decision-procedure text, restated for this operand pairing — the interval story is unchanged by the numerator/divisor type family.)

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the interval-exclude-zero derivation this group instantiates (“compute the divisor's interval from its declared modifiers and in-scope guard facts… zero inside the interval, or no bound excluding it, rejects”)
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: “Arg-bound interval arithmetic” is stated in § Validity arguments against Witness Family 1 Base A (summing two bounded args against a rule's bound). Reusing it here is a direct, simpler corollary: governance enforces the declared modifier on the event arg at ingress (precept-language-spec.md:268), so a `positive`-declared divisor's runtime value is provably > 0, hence ≠ 0 — no sum, no second term. As with the guard argument above, no validity argument is separately written for the fault family; this is the same reuse, flagged rather than assumed.

### What the failing diagnostic must suggest

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (b): bound the args such that the interval combination satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept QtyQtyCross

field DistUnit as unitofmeasure default 'm'
field TimeUnit as unitofmeasure default 's'
field Distance as quantity in '{DistUnit}' default '0 {DistUnit}'
field Ratio as decimal default 0.0

state Draft initial
state Active

event Open(D as quantity in '{DistUnit}')
from Draft on Open
    -> set DistUnit = 'm'
    -> set TimeUnit = 's'
    -> set Distance = Open.D
    -> transition Active

event Split(Divisor as quantity in '{TimeUnit}')
from Active on Split
    -> set Ratio = Distance / Split.Divisor
    -> no transition
```

Required outcome: reject, naming the missing premise classes (b), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*positive-arg* — arg-modifier

- Addition: Divisor as quantity in '{TimeUnit}' positive
- Premise classes: (b)
- Derivation: declared `positive` modifier → Divisor > 0 → Divisor ≠ 0 (IntervalContainment)
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: applied by replacing the named event-arg declarations
  - `Split.Divisor` becomes `Divisor as quantity in '{TimeUnit}' positive`

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified as compiling clean when the result is assigned to a `decimal` field. Per the reachability note, this exercises QuantityDivideQuantitySameDimension's identity at HEAD, not this cell's own — recorded honestly rather than claimed as a Cross-specific run.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| positive-arg | Divisor as quantity in '{TimeUnit}' nonnegative | `nonnegative` admits zero — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

- The model's respellability story is unchanged from Cell 7 (Same dimension) — the reachability finding is a built-status matter, not a model-power matter.

**What the sources leave unstated or ambiguous here**

- Verified reachability limitation (HEAD e1a14d91), the same mechanism as Cell 3: the catalog declares QuantityDivideQuantitySameDimension and QuantityDivideQuantityCrossDimension as two candidates for the (Divide, Quantity, Quantity) type pair, and Operations.DisambiguateCandidates (src/Precept/Language/Operations.cs:1305-1326) always selects the `Match: Same` entry when multiple candidates exist. Probed directly with dynamic (field-interpolated) unit qualifiers of genuinely different dimensions (distance ÷ time): assigning the division to a compound-quantity-typed field yields TypeMismatch ("Expected a quantity value here, but got 'decimal'"); assigning to a decimal-typed field compiles, but as QuantityDivideQuantitySameDimension's identity, not this one's. Unlike Money/Money, this pair's cross-dimension case is NOT blocked by the cross-dimension pairwise check (PRE0071) — that check explicitly excludes Divide (TypeChecker.Expressions.cs:1324-1325, “division has its own catalog routes”) — but the exclusion only stops PRE0071 from firing; it does not make DisambiguateCandidates pick the Cross entry. Net effect: no surface spelling reaches QuantityDivideQuantityCrossDimension as its own catalog identity. Both catalog entries carry identical NumericProofRequirement content (“Divisor must be non-zero”, subject = the quantity operand), so the fault obligation's substance is unaffected — this is a built-status / reachability finding about HEAD, not a model gap.
- No guard-discharge witness is recorded for this cell, for the same reason as Cell 3: the guard mechanism already fails for the reachable Same-dimension identity (Cell 7, live-verified), and this cell's own identity is itself unreachable at HEAD. The guard's failure on this exact setting was also directly probed (`when Split.Divisor != '0 s'`) and confirmed to still reject.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the interval-exclude-zero derivation this group instantiates (“compute the divisor's interval from its declared modifiers and in-scope guard facts… zero inside the interval, or no bound excluding it, rejects”)
- src/Precept/Language/Operations.cs:563 — catalog: QuantityDivideQuantityCrossDimension catalog entry — Match: Different, divisor subject is the quantity operand
- src/Precept/Language/Operations.cs:1315 — code: DisambiguateCandidates — selects the Same-qualifier entry by default when multiple candidates exist

## g07/price-divide-decimal — Price ÷ decimal, transition-row write — the divisor is a decimal arg

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the interval-exclude-zero derivation this group instantiates (“compute the divisor's interval from its declared modifiers and in-scope guard facts… zero inside the interval, or no bound excluding it, rejects”)
- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault prevention

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | PriceDivideDecimal |
| evaluation site category | transition-row-action-operand |
| type family | business-domain |

### What must be proven

Obligation: Split.Divisor != 0

Weakest precondition: Split.Divisor != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Divisor | the event arg supplying the divisor operand (decimal-typed) |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (b), (c).

The divisor is the triggering event's own decimal-typed arg and is not read elsewhere in the plan — premise (d) has nothing to instantiate. Only (b) and (c) can supply the interval that excludes zero.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: guard fact normal-form-equal to the divisor's non-zero condition → guard-match
- Capability tier: 1a
- Validity arguments: Guard normal-form match
- Decision procedure: Compute the divisor's interval from its declared modifiers and in-scope guard facts (a finite set per site); zero inside the interval, or no bound excluding it, rejects. (Family 4's own decision-procedure text, restated for this operand pairing — the interval story is unchanged by the numerator/divisor type family.)

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the interval-exclude-zero derivation this group instantiates (“compute the divisor's interval from its declared modifiers and in-scope guard facts… zero inside the interval, or no bound excluding it, rejects”)
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: “Guard normal-form match” is stated in § Validity arguments against Witness Family 1's Bases A and C (a rule-preservation WP). Reusing it here is a direct corollary, not a separately-written argument: the same reasoning — a row fires only if its guard evaluated true, and the guard reads the same pre-state/args the action reads — applies unchanged when the proposition being matched is the fault family's “Divisor ≠ 0” instead of a rule's bound. No dedicated validity argument is written for the fault family in § Validity arguments; Family 4's own § Witnesses entry states the decision procedure but is not one of the six named arguments the schema enumerates. Flagged here rather than silently assumed.

**Entry 2 — (b)**

- Derivation: declared arg-modifier bound on the divisor (e.g. `positive`) excludes 0 from its interval → IntervalContainment
- Strategy: IntervalContainment
- Capability tier: 1a
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor's interval from its declared modifiers and in-scope guard facts (a finite set per site); zero inside the interval, or no bound excluding it, rejects. (Family 4's own decision-procedure text, restated for this operand pairing — the interval story is unchanged by the numerator/divisor type family.)

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the interval-exclude-zero derivation this group instantiates (“compute the divisor's interval from its declared modifiers and in-scope guard facts… zero inside the interval, or no bound excluding it, rejects”)
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: “Arg-bound interval arithmetic” is stated in § Validity arguments against Witness Family 1 Base A (summing two bounded args against a rule's bound). Reusing it here is a direct, simpler corollary: governance enforces the declared modifier on the event arg at ingress (precept-language-spec.md:268), so a `positive`-declared divisor's runtime value is provably > 0, hence ≠ 0 — no sum, no second term. As with the guard argument above, no validity argument is separately written for the fault family; this is the same reuse, flagged rather than assumed.

### What the failing diagnostic must suggest

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (b): bound the args such that the interval combination satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept BillingPriceDecimal

field UnitPrice as price in 'USD/kg' default '0 USD/kg'
field ScaledPrice as price in 'USD/kg' default '0 USD/kg'

state Draft initial
state Active

event Open(Amount as price in 'USD/kg')
from Draft on Open
    -> set UnitPrice = Open.Amount
    -> transition Active

event Split(Divisor as decimal)
from Active on Split
    -> set ScaledPrice = UnitPrice / Split.Divisor
    -> no transition
```

Required outcome: reject, naming the missing premise classes (b), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*positive-arg* — arg-modifier

- Addition: Divisor as decimal positive
- Premise classes: (b)
- Derivation: declared `positive` modifier → Divisor > 0 → Divisor ≠ 0 (IntervalContainment)
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: applied by replacing the named event-arg declarations
  - `Split.Divisor` becomes `Divisor as decimal positive`

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

*guard-nonzero* — guard

- Addition: when Split.Divisor != 0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to `Divisor ≠ 0` → guard-match
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: appended to the guard of the `on Split` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| positive-arg | Divisor as decimal nonnegative | `nonnegative` admits 0 — must still reject, same obligation | reject, naming the same obligation |
| guard-nonzero | when Split.Divisor >= 0 | `>= 0` admits 0 — does not imply the WP; must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Dual type-family site, same reasoning as Cell 1: PriceDivideDecimal's declared operand types are Price (business-domain) and decimal (primitive); the coordinate recorded here (business-domain) reflects the numerator's family, while the requirement's subject — the divisor — is the primitive decimal operand.
- The divisor is a fresh event arg untouched earlier in the plan, so the WP equals the catalog condition verbatim.
- wp.canonicalKey omitted for the same reason as Cell 1.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the interval-exclude-zero derivation this group instantiates (“compute the divisor's interval from its declared modifiers and in-scope guard facts… zero inside the interval, or no bound excluding it, rejects”)
- src/Precept/Language/Operations.cs:681 — catalog: PriceDivideDecimal catalog entry — divisor subject is the decimal operand

## g07/price-divide-quantity — Price ÷ quantity (compound, dimension elevation), transition-row write — the divisor is a compound-quantity arg

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the interval-exclude-zero derivation this group instantiates (“compute the divisor's interval from its declared modifiers and in-scope guard facts… zero inside the interval, or no bound excluding it, rejects”)
- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault prevention

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | PriceDivideQuantity |
| evaluation site category | transition-row-action-operand |
| type family | business-domain |

### What must be proven

Obligation: Split.Divisor != '0 each/kg'

Weakest precondition: Split.Divisor != '0 each/kg'

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Divisor | the event arg supplying the divisor operand (compound-quantity-typed (numerator unit 'each', denominator unit 'kg')) |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (b), (c).

The divisor is the triggering event's own compound-quantity-typed arg and is not read elsewhere in the plan — premise (d) has nothing to instantiate. Only (b) and (c) can supply the interval that excludes zero. `ResultQualifierPolicy.CompoundDimensionElevation` (Operation.cs:44-49) takes the result's unit from the divisor's own numerator ('each' here) and the result's currency from the price numerator ('USD') — verified empirically: `price in 'USD/kg' / quantity in 'each/kg' → price in 'USD/each'` compiled with only the target obligation's diagnostic once the divisor is bounded.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: guard fact normal-form-equal to the divisor's non-zero condition → guard-match
- Capability tier: 1a
- Validity arguments: Guard normal-form match
- Decision procedure: Compute the divisor's interval from its declared modifiers and in-scope guard facts (a finite set per site); zero inside the interval, or no bound excluding it, rejects. (Family 4's own decision-procedure text, restated for this operand pairing — the interval story is unchanged by the numerator/divisor type family.)

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the interval-exclude-zero derivation this group instantiates (“compute the divisor's interval from its declared modifiers and in-scope guard facts… zero inside the interval, or no bound excluding it, rejects”)
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: “Guard normal-form match” is stated in § Validity arguments against Witness Family 1's Bases A and C (a rule-preservation WP). Reusing it here is a direct corollary, not a separately-written argument: the same reasoning — a row fires only if its guard evaluated true, and the guard reads the same pre-state/args the action reads — applies unchanged when the proposition being matched is the fault family's “Divisor ≠ 0” instead of a rule's bound. No dedicated validity argument is written for the fault family in § Validity arguments; Family 4's own § Witnesses entry states the decision procedure but is not one of the six named arguments the schema enumerates. Flagged here rather than silently assumed.

**Entry 2 — (b)**

- Derivation: declared arg-modifier bound on the divisor (e.g. `positive`) excludes 0 from its interval → IntervalContainment
- Strategy: IntervalContainment
- Capability tier: 1a
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor's interval from its declared modifiers and in-scope guard facts (a finite set per site); zero inside the interval, or no bound excluding it, rejects. (Family 4's own decision-procedure text, restated for this operand pairing — the interval story is unchanged by the numerator/divisor type family.)

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the interval-exclude-zero derivation this group instantiates (“compute the divisor's interval from its declared modifiers and in-scope guard facts… zero inside the interval, or no bound excluding it, rejects”)
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: “Arg-bound interval arithmetic” is stated in § Validity arguments against Witness Family 1 Base A (summing two bounded args against a rule's bound). Reusing it here is a direct, simpler corollary: governance enforces the declared modifier on the event arg at ingress (precept-language-spec.md:268), so a `positive`-declared divisor's runtime value is provably > 0, hence ≠ 0 — no sum, no second term. As with the guard argument above, no validity argument is separately written for the fault family; this is the same reuse, flagged rather than assumed.

### What the failing diagnostic must suggest

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (b): bound the args such that the interval combination satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept BillingPriceQuantity

field UnitPricePerKg as price in 'USD/kg' default '0 USD/kg'
field UnitPricePerEach as price in 'USD/each' default '0 USD/each'

state Draft initial
state Active

event Open(Amount as price in 'USD/kg')
from Draft on Open
    -> set UnitPricePerKg = Open.Amount
    -> transition Active

event Split(Divisor as quantity in 'each/kg')
from Active on Split
    -> set UnitPricePerEach = UnitPricePerKg / Split.Divisor
    -> no transition
```

Required outcome: reject, naming the missing premise classes (b), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*positive-arg* — arg-modifier

- Addition: Divisor as quantity in 'each/kg' positive
- Premise classes: (b)
- Derivation: declared `positive` modifier → Divisor > '0 each/kg' → Divisor ≠ '0 each/kg' (IntervalContainment)
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: applied by replacing the named event-arg declarations
  - `Split.Divisor` becomes `Divisor as quantity in 'each/kg' positive`

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

*guard-nonzero* — guard

- Addition: when Split.Divisor != '0 each/kg'
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to `Divisor ≠ '0 each/kg'` → guard-match
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Live-verified at HEAD (e1a14d91): a guard restating the divisor's non-zero condition (`when Split.Divisor != '0 each/kg'`) does not discharge the obligation for a business-domain-typed divisor — the file still rejects with the identical DivisionByZero diagnostic. Tried both the literal-comparison spelling and, for money, the `.amount` projection; both fail identically. This is a genuine built-status gap, not a near-miss: the SAME guard spelling that discharges cleanly when the divisor is `decimal`-typed (MoneyDivideDecimal, QuantityDivideDecimal, PriceDivideDecimal, ExchangeRateDivideDecimal, all verified working in this file) does not discharge when the divisor is a business-domain type. Recorded as builtStatus: unresolved-today per the false-proof-hazard instructions' sibling rule — a live run demonstrating non-discharge, not a business rule used as a premise.)
- How it applies to the base: appended to the guard of the `on Split` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| positive-arg | Divisor as quantity in 'each/kg' nonnegative | `nonnegative` admits '0 each/kg' — must still reject, same obligation | reject, naming the same obligation |
| guard-nonzero | when Split.Divisor >= '0 each/kg' | `>= '0 each/kg'` admits '0 each/kg' — does not imply the WP; must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- The divisor is a fresh event arg untouched earlier in the plan, so the WP equals the catalog condition verbatim.
- wp.canonicalKey omitted for the same reason as Cell 1.
- This is the one site in the group needing a genuinely compound (two-unit) divisor to type-check at all; a bare single unit (e.g. `quantity in 'hour'`) was tried first and rejected (`InvalidUnitString`), so `'each/kg'` was used instead — this doesn't change the fault obligation, only what the base needs to type-check.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the interval-exclude-zero derivation this group instantiates (“compute the divisor's interval from its declared modifiers and in-scope guard facts… zero inside the interval, or no bound excluding it, rejects”)
- src/Precept/Language/Operations.cs:691 — catalog: PriceDivideQuantity catalog entry — ResultQualifierPolicy.CompoundDimensionElevation, divisor subject is the quantity operand
- src/Precept/Language/Operation.cs:44 — catalog: CompoundDimensionElevation doc comment — result unit elevated from the compound-quantity numerator (the divisor)

## g07/exchangerate-divide-decimal — ExchangeRate ÷ decimal, transition-row write — the divisor is a decimal arg

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the interval-exclude-zero derivation this group instantiates (“compute the divisor's interval from its declared modifiers and in-scope guard facts… zero inside the interval, or no bound excluding it, rejects”)
- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault prevention

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | ExchangeRateDivideDecimal |
| evaluation site category | transition-row-action-operand |
| type family | business-domain |

### What must be proven

Obligation: Split.Divisor != 0

Weakest precondition: Split.Divisor != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Divisor | the event arg supplying the divisor operand (decimal-typed) |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (b), (c).

The divisor is the triggering event's own decimal-typed arg and is not read elsewhere in the plan — premise (d) has nothing to instantiate. Only (b) and (c) can supply the interval that excludes zero.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: guard fact normal-form-equal to the divisor's non-zero condition → guard-match
- Capability tier: 1a
- Validity arguments: Guard normal-form match
- Decision procedure: Compute the divisor's interval from its declared modifiers and in-scope guard facts (a finite set per site); zero inside the interval, or no bound excluding it, rejects. (Family 4's own decision-procedure text, restated for this operand pairing — the interval story is unchanged by the numerator/divisor type family.)

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the interval-exclude-zero derivation this group instantiates (“compute the divisor's interval from its declared modifiers and in-scope guard facts… zero inside the interval, or no bound excluding it, rejects”)
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: “Guard normal-form match” is stated in § Validity arguments against Witness Family 1's Bases A and C (a rule-preservation WP). Reusing it here is a direct corollary, not a separately-written argument: the same reasoning — a row fires only if its guard evaluated true, and the guard reads the same pre-state/args the action reads — applies unchanged when the proposition being matched is the fault family's “Divisor ≠ 0” instead of a rule's bound. No dedicated validity argument is written for the fault family in § Validity arguments; Family 4's own § Witnesses entry states the decision procedure but is not one of the six named arguments the schema enumerates. Flagged here rather than silently assumed.

**Entry 2 — (b)**

- Derivation: declared arg-modifier bound on the divisor (e.g. `positive`) excludes 0 from its interval → IntervalContainment
- Strategy: IntervalContainment
- Capability tier: 1a
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor's interval from its declared modifiers and in-scope guard facts (a finite set per site); zero inside the interval, or no bound excluding it, rejects. (Family 4's own decision-procedure text, restated for this operand pairing — the interval story is unchanged by the numerator/divisor type family.)

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the interval-exclude-zero derivation this group instantiates (“compute the divisor's interval from its declared modifiers and in-scope guard facts… zero inside the interval, or no bound excluding it, rejects”)
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: “Arg-bound interval arithmetic” is stated in § Validity arguments against Witness Family 1 Base A (summing two bounded args against a rule's bound). Reusing it here is a direct, simpler corollary: governance enforces the declared modifier on the event arg at ingress (precept-language-spec.md:268), so a `positive`-declared divisor's runtime value is provably > 0, hence ≠ 0 — no sum, no second term. As with the guard argument above, no validity argument is separately written for the fault family; this is the same reuse, flagged rather than assumed.

### What the failing diagnostic must suggest

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (b): bound the args such that the interval combination satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept BillingRateDecimal

field Rate as exchangerate in 'USD' to 'EUR' default '1 USD/EUR'
field ScaledRate as exchangerate in 'USD' to 'EUR' default '1 USD/EUR'

state Draft initial
state Active

event Open(Amount as exchangerate in 'USD' to 'EUR')
from Draft on Open
    -> set Rate = Open.Amount
    -> transition Active

event Split(Divisor as decimal)
from Active on Split
    -> set ScaledRate = Rate / Split.Divisor
    -> no transition
```

Required outcome: reject, naming the missing premise classes (b), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*positive-arg* — arg-modifier

- Addition: Divisor as decimal positive
- Premise classes: (b)
- Derivation: declared `positive` modifier → Divisor > 0 → Divisor ≠ 0 (IntervalContainment)
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: applied by replacing the named event-arg declarations
  - `Split.Divisor` becomes `Divisor as decimal positive`

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

*guard-nonzero* — guard

- Addition: when Split.Divisor != 0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to `Divisor ≠ 0` → guard-match
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: appended to the guard of the `on Split` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| positive-arg | Divisor as decimal nonnegative | `nonnegative` admits 0 — must still reject, same obligation | reject, naming the same obligation |
| guard-nonzero | when Split.Divisor >= 0 | `>= 0` admits 0 — does not imply the WP; must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Dual type-family site, same reasoning as Cell 1: ExchangeRateDivideDecimal's declared operand types are ExchangeRate (business-domain) and decimal (primitive); the coordinate recorded here (business-domain) reflects the numerator's family, while the requirement's subject — the divisor — is the primitive decimal operand.
- The divisor is a fresh event arg untouched earlier in the plan, so the WP equals the catalog condition verbatim.
- wp.canonicalKey omitted for the same reason as Cell 1.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the interval-exclude-zero derivation this group instantiates (“compute the divisor's interval from its declared modifiers and in-scope guard facts… zero inside the interval, or no bound excluding it, rejects”)
- src/Precept/Language/Operations.cs:717 — catalog: ExchangeRateDivideDecimal catalog entry — divisor subject is the decimal operand

