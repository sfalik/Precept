<!--
GENERATED FILE — do not hand-edit.
Source: fault-8-division-business-construction-and-state-hook.cells.json
Regenerate: dotnet run --project tools/Precept.MatrixTools -- render-cells docs/Working/obligation-discharge-matrix-2026-07-19-cells
Hand edits will be overwritten. Edit the .cells.json and regenerate.
-->

# Fault family, group 8 — business-domain division by zero at a construction row and at a state hook

Family id: fault-8-division-business-construction-and-state-hook
Case shape: fault
Definition version: obligation-discharge-matrix-2026-07-19 rev 7 (2026-07-20, draft)

**Where this family's content comes from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses — matrix: Family 4 — fault family, arg-constraint discharge; the base/discharge/near-miss/decision-procedure precedent this file's cells instantiate for business-domain divisor types
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — matrix: Fault minting bullet: catalog-stamped at evaluation sites, each evaluation site in a plan mints its own fault obligations
- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault-prevention obligation family

## Respellability — the family verdict

Can every safe program these rules reject be rewritten into a form they accept? **yes**

Evidence: model-derived
Corpus measurement: pending

**Notes on the family verdict**

- This verdict is extended by analogy from the primitive fault family (Family 4, group 1/2), not separately argued in the matrix for business-domain divisor types; it is model-derived and unmeasured, same standing as every other family in this pass.
- The guard-match built-power gap recorded on the MoneyDivideQuantity and PriceDivideQuantity cells (guard does not discharge for a quantity-typed divisor) is a builtStatus fact, tracked per witness — it is not a respellability question, since the model still licenses the guard spelling; the gap is that the shipped engine does not yet recognize it for this operand type.

**Sources for this verdict**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witness Family 1 — matrix: the respellability paragraph, extended by analogy: every sound divisor-nonzero fact respells into a licensed guard or modifier form
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Ratification protocol — matrix: layer 4: corpus measurement is the hard gate; a family verdict ratifies only when it agrees with the classified sample corpus

## Premise classes — the legend

Where a proof may get its facts (want :19, :143): (a) field modifiers, (b) arg constraints, (c) the handler's guard, (d) all rules holding in the pre-state (the inductive hypothesis), (e) evaluating the applicable rule set at an editable-field write — (e)'s status as a genuinely fifth class vs (a) generalized is an open owner item.

## g08/mdd-construction-row — Money ÷ decimal, construction row

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row: catalog-declared safety precondition at the evaluation site, discharge by premise classes (b)/(c)/(a) per catalog ProofSatisfactions

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | op/MoneyDivideDecimal/numeric-0 |
| evaluation site category | construction-row-action-operand |
| type family | business-domain |

### What must be proven

Obligation: Create.Divisor != 0.0

Weakest precondition: Create.Divisor != 0.0
Key pinned by: Not computed: tools/Precept.MatrixTools' WP calculator (WpCalculator.ComputeEstablishmentWp / ComputePreservationWp) targets rule-family establishment/preservation obligations. The fault-family catalog-declared safety precondition (ProofRequirement) is a different obligation shape the calculator does not reach; live runs against this witness show it as a compile-time [Error] DivisionByZero diagnostic or its absence, never as a printed WP line — a named skip, not an oversight.

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Create.Divisor | the event arg supplying the divisor operand |
| 0.0 | the divisor type's own zero — the decimal literal 0.0 |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (b), (c).

The divisor is read directly from the event's own arg, with no prior write in the plan touching it, so class (b) is available. The construction row carries its own optional when guard (spec § Construction semantics), so class (c) is available too. Class (d) has nothing to instantiate: no pre-state configuration exists before a construction row fires.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (b)**

- Derivation: ingress governance enforces the declared arg modifier on Create.Divisor before any computation reads it, so the runtime value satisfies the modifier's ProofSatisfaction (nonzero: value != 0, or positive: value > 0 which subsumes it) at the point of division
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Look up a nonzero-or-positive ProofSatisfaction for the divisor in its declared arg modifiers (a finite set, ModifierMeta.ProofSatisfactions); its absence, or a bound that does not exclude zero, fails the class.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses — matrix: Family 4's own arg-constraint decision procedure: compute the divisor's interval from its declared modifiers; zero inside the interval, or no bound excluding it, rejects

**Entry 2 — (c)**

- Derivation: guard fact normal-form-equal to the obligation (Create.Divisor != 0.0) -> guard-match
- Validity arguments: Guard normal-form match
- Decision procedure: Normal-form match of the divisor-nonzero condition against the construction row's guard conjuncts (a finite set, per the row's own optional when clause).

- docs/language/precept-language-spec.md § Construction semantics — spec: "Why guards on `on <Event>` are allowed": on Event when condition -> actions is semantically coherent for construction rows, on the same EventRow construct as stateless handlers; live-verified 2026-07-21 at e1a14d91 (guard-test.precept compiled clean with a construction-row when guard present)

### What the failing diagnostic must suggest

- For class (b): bound the argument such that its declared modifier excludes zero (<WP>)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept BillingAccount

field Subtotal as money in 'USD' default '0.00 USD'
field ScaledSubtotal as money in 'USD' default '0.00 USD'

event Create(Amount as money in 'USD', Divisor as decimal) initial

on Create
    -> set Subtotal = Create.Amount
    -> set ScaledSubtotal = Create.Amount / Create.Divisor
```

Required outcome: reject, naming the missing premise classes (b), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*arg-modifier* — arg-modifier

- Addition: Divisor as decimal nonzero
- Premise classes: (b)
- Derivation: declared arg modifier excludes zero at ingress -> arg-bound discharge
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: applied by replacing the named event-arg declarations
  - `Create.Divisor` becomes `Divisor as decimal nonzero`

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

*guard* — guard

- Addition: when Create.Divisor != 0.0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the obligation -> guard-match
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: appended to the guard of the `on Create` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| arg-modifier | Divisor as decimal nonnegative | nonnegative allows zero — Create.Divisor = 0.0 is still admitted, and the divisor can still be zero at the division — must still reject, same obligation | reject, naming the same obligation |
| guard | when Create.Divisor >= 0.0 | >= does not exclude zero — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Divisor type is primitive (decimal); the obligation and both discharge derivations are the same interval/guard-match story as the primitive lane (group 1/2), applied to a bare decimal divisor instead of a bare decimal — group 7's authoring note names exactly this distinction.
- Class (c) discharge built status: Guard-match discharges cleanly for a decimal divisor (live-verified) — the same behaviour as the primitive lane (group 1/2).
- MoneyDivideDecimal — money / decimal ranges over a business-domain (Money) numerator and a primitive (decimal) divisor, so this coordinate is live at both typeFamily=business-domain and typeFamily=primitive (src/Precept/Language/Operations.cs:442). This cell's coordinates carry typeFamily=business-domain as the file's own axis (the numerator side, and this file's subject); it also instantiates the equivalent primitive-family coordinate unchanged, since the safety precondition's subject is the divisor and neither derivation below reads the numerator's type at all. Per group 7's authoring note, the requirement's subject (the divisor) is stated explicitly here to keep the family coordinate unambiguous.
- Construction rows have no pre-state (premise (d) is structurally unavailable — no configuration exists before the event fires), so class (d) is not in this cell's applicable set at all, unlike the state-hook sibling cell below.
- A field-modifier discharge (class (a)) is not separately witnessed here: in this setting the divisor is supplied directly as an event arg, so the divisor-nonzero fact the construction row's write plan actually reads comes from the arg (class (b)), not from a field materialized earlier in the same plan. Class (a) remains structurally available at this site in general (an already-materialised field's own modifier, read before this write) and is left out of applicablePremiseClasses because this cell's own written expression does not read one.

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault-prevention obligation family
- src/Precept/Language/Operations.cs:442 — code: MoneyDivideDecimal — money / decimal — NumericProofRequirement on the right operand: 'Divisor must be non-zero'
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses — matrix: Family 4 — the fault-family base/discharge/near-miss pattern this cell instantiates
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — matrix: Fault minting bullet: catalog-stamped at evaluation sites

## g08/mdd-state-hook — Money ÷ decimal, state hook

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row: catalog-declared safety precondition at the evaluation site, discharge by premise classes (b)/(c)/(a) per catalog ProofSatisfactions

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | op/MoneyDivideDecimal/numeric-0 |
| evaluation site category | state-hook-action-operand |
| type family | business-domain |

### What must be proven

Obligation: Divisor != 0.0

Weakest precondition: Divisor != 0.0
Key pinned by: Not computed: tools/Precept.MatrixTools' WP calculator targets rule-family establishment/preservation obligations, not the fault-family's catalog-declared safety precondition. A named skip, as on the construction-row sibling cell.

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Divisor | the field supplying the divisor operand, materialised at construction and read again at the state hook |
| 0.0 | the divisor type's own zero — the decimal literal 0.0 |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a), (c), (d).

The divisor is a field, already materialised by construction before the state hook fires while resident; its own declared modifier is available as class (a). The state hook carries its own optional pre-verb when guard (spec § State action), so class (c) is available. A pre-state configuration exists (the entity has been constructed and has resided in prior states), so class (d) is structurally available too — subject to the HAZARD noted above. Class (b) has nothing to instantiate: no event args are in scope inside a state hook.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: the field's own declared modifier is enforced at every assignment to it (the modifier desugars to a rule, Modifiers.cs DesugarsToRule), so the runtime value of Divisor satisfies the modifier's ProofSatisfaction whenever the state hook reads it
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Look up a nonzero-or-positive ProofSatisfaction for the divisor field in its declared field modifiers; its absence, or a bound that does not exclude zero, fails the class.

- src/Precept/Language/Modifiers.cs:100 — code: ModifierKind.Nonzero: 'Value != 0', DesugarsToRule: true
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses — matrix: Family 4's decision procedure, generalized from arg modifiers to field modifiers — the written argument (Arg-bound interval arithmetic) is scoped in the matrix text to event args at ingress; its extension to a field modifier enforced at every assignment is the same governance reasoning, not separately argued in the matrix as its own named rule — recorded honestly rather than silently assumed

**Entry 2 — (c)**

- Derivation: guard fact normal-form-equal to the obligation (Divisor != 0.0) -> guard-match
- Validity arguments: Guard normal-form match
- Decision procedure: Normal-form match of the divisor-nonzero condition against the state hook's own pre-verb when guard conjuncts (a finite set).

- docs/language/precept-language-spec.md § State action — spec: state actions (entry/exit hooks) support an optional pre-verb when guard between the state target and the action chain

**Entry 3 — (d)**

- Derivation: a business rule stating the divisor's nonzero fact, consumed as a pre-state premise
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: The built engine's CompositionalConstraint strategy normal-form-matches the divisor-nonzero condition against the file's declared rule set and reports Proved when a match is found — but this document names no validity argument that licenses a rule as a premise (d) source for a fault obligation, and the matrix's own open design hole ("nothing detects an obligation that was never minted", § The cell) plus the verified instance in authored-expressiveness-gaps.md Defect B mean this match is not a sound decision procedure as implemented: the rule's own establishment and preservation are never independently checked, so a clean compile through this path does not confirm the divisor is actually nonzero at runtime. Recorded per the HAZARD ruling for this group: builtStatus is unverified, not proven, regardless of what the compiler reports.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § The cell — matrix: Open design hole — nothing detects an obligation that was never minted; the verified instance is a rule consumed as a premise while nothing establishes it
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — matrix: Defect B — a rule is consumed as a premise while nothing establishes it (fail-open); the identical CompositionalConstraint pattern measured here on a business-domain divisor

### What the failing diagnostic must suggest

- For class (a): bound the field such that its declared modifier excludes zero (<WP>)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (d): state a rule normal-form-equal to <WP>, established at construction and preserved by every write to the field
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

  - This is the definition's committed suggestion under the model. It is not currently a suggestion an author should trust as verified: the HAZARD note on this cell's class-(d) contract entry means the compiler will accept this addition without having checked that the stated rule actually holds.


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept BillingAccount

field Subtotal as money in 'USD' default '0.00 USD'
field ScaledSubtotal as money in 'USD' default '0.00 USD'
field Divisor as decimal default 1.0

event Create(Amount as money in 'USD', Rate as decimal) initial
event Finalize

state Draft initial
state Finalized terminal

on Create
    -> set Subtotal = Create.Amount
    -> set Divisor = Create.Rate

from Draft on Finalize
    -> transition Finalized

to Finalized
    -> set ScaledSubtotal = Subtotal / Divisor
```

Required outcome: reject, naming the missing premise classes (a), (c), (d), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*field-modifier* — arg-modifier

- Addition: field Divisor as decimal nonzero default 1.0
- Premise classes: (a)
- Derivation: declared field modifier excludes zero at every assignment -> field-bound discharge
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No application locus fits a field-declaration modifier (see cell notes); the addition text is recorded directly instead.

*guard* — guard

- Addition: to Finalized when Divisor != 0.0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the obligation -> guard-match
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No application locus fits a state-target when guard (see cell notes); the addition text is recorded directly instead.

*rule* — other

- Addition: rule Divisor != 0.0 because "Divisor must never be zero"
- Premise classes: (d)
- Derivation: rule fact normal-form-equal to the obligation, consumed via CompositionalConstraint
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Live-verified clean compile at e1a14d91, but per the group's HAZARD ruling this is NOT recorded as proven-today: the discharge runs through a business rule used as premise (d), which slice 2 verified as a false-proof path (authored-expressiveness-gaps.md Defect B) — nothing establishes the rule, so the clean compile does not confirm the divisor is actually nonzero. See unverifiableWitnesses.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No application locus fits a new top-level rule declaration (see cell notes); the addition text is recorded directly instead.
- This witness is listed in the task's unverifiableWitnesses output: a clean compile via a rule-as-premise (d) discharge is not treated as confirmation, per the group's HAZARD instruction.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| field-modifier | field Divisor as decimal nonnegative default 1.0 | nonnegative allows zero — Divisor = 0.0 is still admitted, and the divisor can still be zero at the state hook — must still reject, same obligation | reject, naming the same obligation |
| guard | to Finalized when Divisor >= 0.0 | >= does not exclude zero — must still reject, same obligation | reject, naming the same obligation |
| rule | rule Divisor >= 0.0 because "Divisor must never be negative" | >= does not exclude zero — must still reject, same obligation (and, per the near-miss run, the built engine does reject this one — the false proof above is specific to a rule text that normal-form-matches the exact obligation, not a general acceptance of any rule mentioning the field) | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Divisor type is primitive (decimal); obligation and discharge shape mirror the construction-row sibling cell in this file, re-sited to a field the state hook reads from residency rather than an arg the row reads from the event.
- Class (c) discharge built status: Guard-match discharges cleanly for a decimal divisor field (live-verified) — the same behaviour as the primitive lane.
- State hooks fire on every inbound/outbound edge regardless of which event triggered the transition, so no event args are in scope here (class (b) is structurally unavailable) — the mirror image of the construction-row asymmetry above, per the matrix's own account of this axis pairing (group 2's setting, same two premise asymmetries, applied here to a qualified divisor).
- The application DU (docs/Working/obligation-discharge-matrix-2026-07-19-cells/cell.schema.json $defs.application) carries only row-guard (keyed by eventName) and event-arg-declarations (keyed by eventName/argName). A state hook's own when guard is keyed by a state target, not an event name, and a field-declaration modifier is neither a row guard nor an event-arg declaration. Neither locus fits, so application is omitted on this witness rather than force-fit into the wrong shape; the same gap applies to the rule addition below, which is a new top-level declaration with no row or arg to attach to.
- HAZARD: the class-(d) discharge below is exactly the false-proof path this group's authoring notes require flagging. The witness compiles clean at HEAD (live-verified), but per instruction that clean compile is NOT recorded as builtStatus proven-today — it is recorded unresolved-today with the hazard named, and listed separately as an unverifiable witness. The near-miss for this addition (rule weakened to >= 0) correctly still rejects, which is itself informative: the built match is a syntactic normal-form check against the rule text, not a soundness check of the rule's own establishment, so a rule stating the wrong thing is rejected while a rule stating the right thing is wrongly accepted without anything having verified it holds.
- MoneyDivideDecimal — money / decimal ranges over a business-domain (Money) numerator and a primitive (decimal) divisor, so this coordinate is live at both typeFamily=business-domain and typeFamily=primitive (src/Precept/Language/Operations.cs:442). This cell's coordinates carry typeFamily=business-domain; it also instantiates the equivalent primitive-family coordinate unchanged, since the safety precondition's subject is the divisor and none of the three derivations above reads the numerator's type.

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault-prevention obligation family
- src/Precept/Language/Operations.cs:442 — code: MoneyDivideDecimal — money / decimal — NumericProofRequirement on the right operand: 'Divisor must be non-zero'
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses — matrix: Family 4 — the fault-family base/discharge/near-miss pattern this cell instantiates
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — matrix: Fault minting bullet: catalog-stamped at evaluation sites

## g08/mdq-construction-row — Money ÷ quantity, construction row

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row: catalog-declared safety precondition at the evaluation site, discharge by premise classes (b)/(c)/(a) per catalog ProofSatisfactions

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | op/MoneyDivideQuantity/numeric-0 |
| evaluation site category | construction-row-action-operand |
| type family | business-domain |

### What must be proven

Obligation: Create.Weight != '0 kg'

Weakest precondition: Create.Weight != '0 kg'
Key pinned by: Not computed: tools/Precept.MatrixTools' WP calculator (WpCalculator.ComputeEstablishmentWp / ComputePreservationWp) targets rule-family establishment/preservation obligations. The fault-family catalog-declared safety precondition (ProofRequirement) is a different obligation shape the calculator does not reach; live runs against this witness show it as a compile-time [Error] DivisionByZero diagnostic or its absence, never as a printed WP line — a named skip, not an oversight.

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Create.Weight | the event arg supplying the divisor operand |
| '0 kg' | the divisor type's own zero — the qualified zero quantity '0 kg' (business-domain-types.md's zero representation for a quantity, not a bare numeric literal) |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (b), (c).

The divisor is read directly from the event's own arg, with no prior write in the plan touching it, so class (b) is available. The construction row carries its own optional when guard (spec § Construction semantics), so class (c) is available too. Class (d) has nothing to instantiate: no pre-state configuration exists before a construction row fires.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (b)**

- Derivation: ingress governance enforces the declared arg modifier on Create.Weight before any computation reads it, so the runtime value satisfies the modifier's ProofSatisfaction (nonzero: value != 0, or positive: value > 0 which subsumes it) at the point of division
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Look up a nonzero-or-positive ProofSatisfaction for the divisor in its declared arg modifiers (a finite set, ModifierMeta.ProofSatisfactions); its absence, or a bound that does not exclude zero, fails the class.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses — matrix: Family 4's own arg-constraint decision procedure: compute the divisor's interval from its declared modifiers; zero inside the interval, or no bound excluding it, rejects

**Entry 2 — (c)**

- Derivation: guard fact normal-form-equal to the obligation (Create.Weight != '0 kg') -> guard-match
- Validity arguments: Guard normal-form match
- Decision procedure: Normal-form match of the divisor-nonzero condition against the construction row's guard conjuncts (a finite set, per the row's own optional when clause).

- docs/language/precept-language-spec.md § Construction semantics — spec: "Why guards on `on <Event>` are allowed": on Event when condition -> actions is semantically coherent for construction rows, on the same EventRow construct as stateless handlers; live-verified 2026-07-21 at e1a14d91 (guard-test.precept compiled clean with a construction-row when guard present)

### What the failing diagnostic must suggest

- For class (b): bound the argument such that its declared modifier excludes zero (<WP>)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept BillingAccount

field Subtotal as money in 'USD' default '0.00 USD'
field UnitPrice as price optional

event Create(Amount as money in 'USD', Weight as quantity in 'kg') initial

on Create
    -> set Subtotal = Create.Amount
    -> set UnitPrice = Create.Amount / Create.Weight
```

Required outcome: reject, naming the missing premise classes (b), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*arg-modifier* — arg-modifier

- Addition: Weight as quantity in 'kg' nonzero
- Premise classes: (b)
- Derivation: declared arg modifier excludes zero at ingress -> arg-bound discharge
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: applied by replacing the named event-arg declarations
  - `Create.Weight` becomes `Weight as quantity in 'kg' nonzero`

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

*guard* — guard

- Addition: when Create.Weight != '0 kg'
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the obligation -> guard-match
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Guard-match does NOT discharge for a quantity divisor: live-verified run still reports DivisionByZero with the identical guard spelling that discharges cleanly for a decimal divisor on the sibling MoneyDivideDecimal cell in this file (also tried Create.Weight.amount != 0.0 as an alternate spelling — same rejection). This is a built-power gap, not a modelled one: nothing in the matrix's stated Guard normal-form match argument restricts it to primitive operand types.)
- How it applies to the base: appended to the guard of the `on Create` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| arg-modifier | Weight as quantity in 'kg' nonnegative | nonnegative allows zero — Create.Weight = '0 kg' is still admitted, and the divisor can still be zero at the division — must still reject, same obligation | reject, naming the same obligation |
| guard | when Create.Weight >= '0 kg' | >= does not exclude zero — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Divisor type is business-domain (quantity in 'kg'); the obligation and both discharge derivations are the same interval/guard-match story as the primitive lane (group 1/2), applied to a business-domain quantity divisor instead of a bare decimal — group 7's authoring note names exactly this distinction.
- Class (c) discharge built status: Guard-match does NOT discharge for a quantity divisor: live-verified run still reports DivisionByZero with the identical guard spelling that discharges cleanly for a decimal divisor on the sibling MoneyDivideDecimal cell in this file (also tried Create.Weight.amount != 0.0 as an alternate spelling — same rejection). This is a built-power gap, not a modelled one: nothing in the matrix's stated Guard normal-form match argument restricts it to primitive operand types.
- Construction rows have no pre-state (premise (d) is structurally unavailable — no configuration exists before the event fires), so class (d) is not in this cell's applicable set at all, unlike the state-hook sibling cell below.
- A field-modifier discharge (class (a)) is not separately witnessed here: in this setting the divisor is supplied directly as an event arg, so the divisor-nonzero fact the construction row's write plan actually reads comes from the arg (class (b)), not from a field materialized earlier in the same plan. Class (a) remains structurally available at this site in general (an already-materialised field's own modifier, read before this write) and is left out of applicablePremiseClasses because this cell's own written expression does not read one.

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault-prevention obligation family
- src/Precept/Language/Operations.cs:472 — code: MoneyDivideQuantity — money / quantity — NumericProofRequirement on the right operand: 'Divisor must be non-zero'
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses — matrix: Family 4 — the fault-family base/discharge/near-miss pattern this cell instantiates
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — matrix: Fault minting bullet: catalog-stamped at evaluation sites

## g08/mdq-state-hook — Money ÷ quantity, state hook

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row: catalog-declared safety precondition at the evaluation site, discharge by premise classes (b)/(c)/(a) per catalog ProofSatisfactions

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | op/MoneyDivideQuantity/numeric-0 |
| evaluation site category | state-hook-action-operand |
| type family | business-domain |

### What must be proven

Obligation: Weight != '0 kg'

Weakest precondition: Weight != '0 kg'
Key pinned by: Not computed: tools/Precept.MatrixTools' WP calculator targets rule-family establishment/preservation obligations, not the fault-family's catalog-declared safety precondition. A named skip, as on the construction-row sibling cell.

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Weight | the field supplying the divisor operand, materialised at construction and read again at the state hook |
| '0 kg' | the divisor type's own zero — the qualified zero quantity '0 kg' (business-domain-types.md's zero representation for a quantity, not a bare numeric literal) |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a), (c), (d).

The divisor is a field, already materialised by construction before the state hook fires while resident; its own declared modifier is available as class (a). The state hook carries its own optional pre-verb when guard (spec § State action), so class (c) is available. A pre-state configuration exists (the entity has been constructed and has resided in prior states), so class (d) is structurally available too — subject to the HAZARD noted above. Class (b) has nothing to instantiate: no event args are in scope inside a state hook.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: the field's own declared modifier is enforced at every assignment to it (the modifier desugars to a rule, Modifiers.cs DesugarsToRule), so the runtime value of Weight satisfies the modifier's ProofSatisfaction whenever the state hook reads it
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Look up a nonzero-or-positive ProofSatisfaction for the divisor field in its declared field modifiers; its absence, or a bound that does not exclude zero, fails the class.

- src/Precept/Language/Modifiers.cs:100 — code: ModifierKind.Nonzero: 'Value != 0', DesugarsToRule: true
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses — matrix: Family 4's decision procedure, generalized from arg modifiers to field modifiers — the written argument (Arg-bound interval arithmetic) is scoped in the matrix text to event args at ingress; its extension to a field modifier enforced at every assignment is the same governance reasoning, not separately argued in the matrix as its own named rule — recorded honestly rather than silently assumed

**Entry 2 — (c)**

- Derivation: guard fact normal-form-equal to the obligation (Weight != '0 kg') -> guard-match
- Validity arguments: Guard normal-form match
- Decision procedure: Normal-form match of the divisor-nonzero condition against the state hook's own pre-verb when guard conjuncts (a finite set).

- docs/language/precept-language-spec.md § State action — spec: state actions (entry/exit hooks) support an optional pre-verb when guard between the state target and the action chain

**Entry 3 — (d)**

- Derivation: a business rule stating the divisor's nonzero fact, consumed as a pre-state premise
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: The built engine's CompositionalConstraint strategy normal-form-matches the divisor-nonzero condition against the file's declared rule set and reports Proved when a match is found — but this document names no validity argument that licenses a rule as a premise (d) source for a fault obligation, and the matrix's own open design hole ("nothing detects an obligation that was never minted", § The cell) plus the verified instance in authored-expressiveness-gaps.md Defect B mean this match is not a sound decision procedure as implemented: the rule's own establishment and preservation are never independently checked, so a clean compile through this path does not confirm the divisor is actually nonzero at runtime. Recorded per the HAZARD ruling for this group: builtStatus is unverified, not proven, regardless of what the compiler reports.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § The cell — matrix: Open design hole — nothing detects an obligation that was never minted; the verified instance is a rule consumed as a premise while nothing establishes it
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — matrix: Defect B — a rule is consumed as a premise while nothing establishes it (fail-open); the identical CompositionalConstraint pattern measured here on a business-domain divisor

### What the failing diagnostic must suggest

- For class (a): bound the field such that its declared modifier excludes zero (<WP>)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (d): state a rule normal-form-equal to <WP>, established at construction and preserved by every write to the field
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

  - This is the definition's committed suggestion under the model. It is not currently a suggestion an author should trust as verified: the HAZARD note on this cell's class-(d) contract entry means the compiler will accept this addition without having checked that the stated rule actually holds.


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept BillingAccount

field Subtotal as money in 'USD' default '0.00 USD'
field Weight as quantity in 'kg' default '1 kg'
field UnitPrice as price optional

event Create(Amount as money in 'USD', LoadWeight as quantity in 'kg') initial
event Finalize

state Draft initial
state Finalized terminal

on Create
    -> set Subtotal = Create.Amount
    -> set Weight = Create.LoadWeight

from Draft on Finalize
    -> transition Finalized

to Finalized
    -> set UnitPrice = Subtotal / Weight
```

Required outcome: reject, naming the missing premise classes (a), (c), (d), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*field-modifier* — arg-modifier

- Addition: field Weight as quantity in 'kg' nonzero default '1 kg'
- Premise classes: (a)
- Derivation: declared field modifier excludes zero at every assignment -> field-bound discharge
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No application locus fits a field-declaration modifier (see cell notes); the addition text is recorded directly instead.

*guard* — guard

- Addition: to Finalized when Weight != '0 kg'
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the obligation -> guard-match
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Guard-match does NOT discharge for a quantity divisor field either (live-verified) — same built gap as the construction-row sibling cell.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No application locus fits a state-target when guard (see cell notes); the addition text is recorded directly instead.

*rule* — other

- Addition: rule Weight != '0 kg' because "Weight must never be zero"
- Premise classes: (d)
- Derivation: rule fact normal-form-equal to the obligation, consumed via CompositionalConstraint
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Live-verified clean compile at e1a14d91, but per the group's HAZARD ruling this is NOT recorded as proven-today: the discharge runs through a business rule used as premise (d), which slice 2 verified as a false-proof path (authored-expressiveness-gaps.md Defect B) — nothing establishes the rule, so the clean compile does not confirm the divisor is actually nonzero. See unverifiableWitnesses.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No application locus fits a new top-level rule declaration (see cell notes); the addition text is recorded directly instead.
- This witness is listed in the task's unverifiableWitnesses output: a clean compile via a rule-as-premise (d) discharge is not treated as confirmation, per the group's HAZARD instruction.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| field-modifier | field Weight as quantity in 'kg' nonnegative default '1 kg' | nonnegative allows zero — Weight = '0 kg' is still admitted, and the divisor can still be zero at the state hook — must still reject, same obligation | reject, naming the same obligation |
| guard | to Finalized when Weight >= '0 kg' | >= does not exclude zero — must still reject, same obligation | reject, naming the same obligation |
| rule | rule Weight >= '0 kg' because "Weight must never be negative" | >= does not exclude zero — must still reject, same obligation (and, per the near-miss run, the built engine does reject this one — the false proof above is specific to a rule text that normal-form-matches the exact obligation, not a general acceptance of any rule mentioning the field) | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Divisor type is business-domain (quantity in 'kg'); obligation and discharge shape mirror the construction-row sibling cell in this file, re-sited to a field the state hook reads from residency rather than an arg the row reads from the event.
- Class (c) discharge built status: Guard-match does NOT discharge for a quantity divisor field either (live-verified) — same built gap as the construction-row sibling cell.
- State hooks fire on every inbound/outbound edge regardless of which event triggered the transition, so no event args are in scope here (class (b) is structurally unavailable) — the mirror image of the construction-row asymmetry above, per the matrix's own account of this axis pairing (group 2's setting, same two premise asymmetries, applied here to a qualified divisor).
- The application DU (docs/Working/obligation-discharge-matrix-2026-07-19-cells/cell.schema.json $defs.application) carries only row-guard (keyed by eventName) and event-arg-declarations (keyed by eventName/argName). A state hook's own when guard is keyed by a state target, not an event name, and a field-declaration modifier is neither a row guard nor an event-arg declaration. Neither locus fits, so application is omitted on this witness rather than force-fit into the wrong shape; the same gap applies to the rule addition below, which is a new top-level declaration with no row or arg to attach to.
- HAZARD: the class-(d) discharge below is exactly the false-proof path this group's authoring notes require flagging. The witness compiles clean at HEAD (live-verified), but per instruction that clean compile is NOT recorded as builtStatus proven-today — it is recorded unresolved-today with the hazard named, and listed separately as an unverifiable witness. The near-miss for this addition (rule weakened to >= 0) correctly still rejects, which is itself informative: the built match is a syntactic normal-form check against the rule text, not a soundness check of the rule's own establishment, so a rule stating the wrong thing is rejected while a rule stating the right thing is wrongly accepted without anything having verified it holds.

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault-prevention obligation family
- src/Precept/Language/Operations.cs:472 — code: MoneyDivideQuantity — money / quantity — NumericProofRequirement on the right operand: 'Divisor must be non-zero'
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses — matrix: Family 4 — the fault-family base/discharge/near-miss pattern this cell instantiates
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — matrix: Fault minting bullet: catalog-stamped at evaluation sites

## g08/qdd-construction-row — Quantity ÷ decimal, construction row

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row: catalog-declared safety precondition at the evaluation site, discharge by premise classes (b)/(c)/(a) per catalog ProofSatisfactions

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | op/QuantityDivideDecimal/numeric-0 |
| evaluation site category | construction-row-action-operand |
| type family | business-domain |

### What must be proven

Obligation: Create.Factor != 0.0

Weakest precondition: Create.Factor != 0.0
Key pinned by: Not computed: tools/Precept.MatrixTools' WP calculator (WpCalculator.ComputeEstablishmentWp / ComputePreservationWp) targets rule-family establishment/preservation obligations. The fault-family catalog-declared safety precondition (ProofRequirement) is a different obligation shape the calculator does not reach; live runs against this witness show it as a compile-time [Error] DivisionByZero diagnostic or its absence, never as a printed WP line — a named skip, not an oversight.

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Create.Factor | the event arg supplying the divisor operand |
| 0.0 | the divisor type's own zero — the decimal literal 0.0 |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (b), (c).

The divisor is read directly from the event's own arg, with no prior write in the plan touching it, so class (b) is available. The construction row carries its own optional when guard (spec § Construction semantics), so class (c) is available too. Class (d) has nothing to instantiate: no pre-state configuration exists before a construction row fires.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (b)**

- Derivation: ingress governance enforces the declared arg modifier on Create.Factor before any computation reads it, so the runtime value satisfies the modifier's ProofSatisfaction (nonzero: value != 0, or positive: value > 0 which subsumes it) at the point of division
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Look up a nonzero-or-positive ProofSatisfaction for the divisor in its declared arg modifiers (a finite set, ModifierMeta.ProofSatisfactions); its absence, or a bound that does not exclude zero, fails the class.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses — matrix: Family 4's own arg-constraint decision procedure: compute the divisor's interval from its declared modifiers; zero inside the interval, or no bound excluding it, rejects

**Entry 2 — (c)**

- Derivation: guard fact normal-form-equal to the obligation (Create.Factor != 0.0) -> guard-match
- Validity arguments: Guard normal-form match
- Decision procedure: Normal-form match of the divisor-nonzero condition against the construction row's guard conjuncts (a finite set, per the row's own optional when clause).

- docs/language/precept-language-spec.md § Construction semantics — spec: "Why guards on `on <Event>` are allowed": on Event when condition -> actions is semantically coherent for construction rows, on the same EventRow construct as stateless handlers; live-verified 2026-07-21 at e1a14d91 (guard-test.precept compiled clean with a construction-row when guard present)

### What the failing diagnostic must suggest

- For class (b): bound the argument such that its declared modifier excludes zero (<WP>)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept BillingAccount

field Weight as quantity in 'kg' default '0 kg'
field ScaledWeight as quantity in 'kg' default '0 kg'

event Create(Load as quantity in 'kg', Factor as decimal) initial

on Create
    -> set Weight = Create.Load
    -> set ScaledWeight = Create.Load / Create.Factor
```

Required outcome: reject, naming the missing premise classes (b), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*arg-modifier* — arg-modifier

- Addition: Factor as decimal nonzero
- Premise classes: (b)
- Derivation: declared arg modifier excludes zero at ingress -> arg-bound discharge
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: applied by replacing the named event-arg declarations
  - `Create.Factor` becomes `Factor as decimal nonzero`

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

*guard* — guard

- Addition: when Create.Factor != 0.0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the obligation -> guard-match
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: appended to the guard of the `on Create` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| arg-modifier | Factor as decimal nonnegative | nonnegative allows zero — Create.Factor = 0.0 is still admitted, and the divisor can still be zero at the division — must still reject, same obligation | reject, naming the same obligation |
| guard | when Create.Factor >= 0.0 | >= does not exclude zero — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Divisor type is primitive (decimal); the obligation and both discharge derivations are the same interval/guard-match story as the primitive lane (group 1/2), applied to a bare decimal divisor instead of a bare decimal — group 7's authoring note names exactly this distinction.
- Class (c) discharge built status: Guard-match discharges cleanly for a decimal divisor (live-verified) — the same behaviour as the primitive lane (group 1/2).
- QuantityDivideDecimal — quantity / decimal ranges over a business-domain (Quantity) numerator and a primitive (decimal) divisor, so this coordinate is live at both typeFamily=business-domain and typeFamily=primitive (src/Precept/Language/Operations.cs:543). This cell's coordinates carry typeFamily=business-domain as the file's own axis (the numerator side, and this file's subject); it also instantiates the equivalent primitive-family coordinate unchanged, since the safety precondition's subject is the divisor and neither derivation below reads the numerator's type at all. Per group 7's authoring note, the requirement's subject (the divisor) is stated explicitly here to keep the family coordinate unambiguous.
- Construction rows have no pre-state (premise (d) is structurally unavailable — no configuration exists before the event fires), so class (d) is not in this cell's applicable set at all, unlike the state-hook sibling cell below.
- A field-modifier discharge (class (a)) is not separately witnessed here: in this setting the divisor is supplied directly as an event arg, so the divisor-nonzero fact the construction row's write plan actually reads comes from the arg (class (b)), not from a field materialized earlier in the same plan. Class (a) remains structurally available at this site in general (an already-materialised field's own modifier, read before this write) and is left out of applicablePremiseClasses because this cell's own written expression does not read one.

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault-prevention obligation family
- src/Precept/Language/Operations.cs:543 — code: QuantityDivideDecimal — quantity / decimal — NumericProofRequirement on the right operand: 'Divisor must be non-zero'
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses — matrix: Family 4 — the fault-family base/discharge/near-miss pattern this cell instantiates
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — matrix: Fault minting bullet: catalog-stamped at evaluation sites

## g08/qdd-state-hook — Quantity ÷ decimal, state hook

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row: catalog-declared safety precondition at the evaluation site, discharge by premise classes (b)/(c)/(a) per catalog ProofSatisfactions

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | op/QuantityDivideDecimal/numeric-0 |
| evaluation site category | state-hook-action-operand |
| type family | business-domain |

### What must be proven

Obligation: Factor != 0.0

Weakest precondition: Factor != 0.0
Key pinned by: Not computed: tools/Precept.MatrixTools' WP calculator targets rule-family establishment/preservation obligations, not the fault-family's catalog-declared safety precondition. A named skip, as on the construction-row sibling cell.

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Factor | the field supplying the divisor operand, materialised at construction and read again at the state hook |
| 0.0 | the divisor type's own zero — the decimal literal 0.0 |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a), (c), (d).

The divisor is a field, already materialised by construction before the state hook fires while resident; its own declared modifier is available as class (a). The state hook carries its own optional pre-verb when guard (spec § State action), so class (c) is available. A pre-state configuration exists (the entity has been constructed and has resided in prior states), so class (d) is structurally available too — subject to the HAZARD noted above. Class (b) has nothing to instantiate: no event args are in scope inside a state hook.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: the field's own declared modifier is enforced at every assignment to it (the modifier desugars to a rule, Modifiers.cs DesugarsToRule), so the runtime value of Factor satisfies the modifier's ProofSatisfaction whenever the state hook reads it
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Look up a nonzero-or-positive ProofSatisfaction for the divisor field in its declared field modifiers; its absence, or a bound that does not exclude zero, fails the class.

- src/Precept/Language/Modifiers.cs:100 — code: ModifierKind.Nonzero: 'Value != 0', DesugarsToRule: true
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses — matrix: Family 4's decision procedure, generalized from arg modifiers to field modifiers — the written argument (Arg-bound interval arithmetic) is scoped in the matrix text to event args at ingress; its extension to a field modifier enforced at every assignment is the same governance reasoning, not separately argued in the matrix as its own named rule — recorded honestly rather than silently assumed

**Entry 2 — (c)**

- Derivation: guard fact normal-form-equal to the obligation (Factor != 0.0) -> guard-match
- Validity arguments: Guard normal-form match
- Decision procedure: Normal-form match of the divisor-nonzero condition against the state hook's own pre-verb when guard conjuncts (a finite set).

- docs/language/precept-language-spec.md § State action — spec: state actions (entry/exit hooks) support an optional pre-verb when guard between the state target and the action chain

**Entry 3 — (d)**

- Derivation: a business rule stating the divisor's nonzero fact, consumed as a pre-state premise
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: The built engine's CompositionalConstraint strategy normal-form-matches the divisor-nonzero condition against the file's declared rule set and reports Proved when a match is found — but this document names no validity argument that licenses a rule as a premise (d) source for a fault obligation, and the matrix's own open design hole ("nothing detects an obligation that was never minted", § The cell) plus the verified instance in authored-expressiveness-gaps.md Defect B mean this match is not a sound decision procedure as implemented: the rule's own establishment and preservation are never independently checked, so a clean compile through this path does not confirm the divisor is actually nonzero at runtime. Recorded per the HAZARD ruling for this group: builtStatus is unverified, not proven, regardless of what the compiler reports.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § The cell — matrix: Open design hole — nothing detects an obligation that was never minted; the verified instance is a rule consumed as a premise while nothing establishes it
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — matrix: Defect B — a rule is consumed as a premise while nothing establishes it (fail-open); the identical CompositionalConstraint pattern measured here on a business-domain divisor

### What the failing diagnostic must suggest

- For class (a): bound the field such that its declared modifier excludes zero (<WP>)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (d): state a rule normal-form-equal to <WP>, established at construction and preserved by every write to the field
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

  - This is the definition's committed suggestion under the model. It is not currently a suggestion an author should trust as verified: the HAZARD note on this cell's class-(d) contract entry means the compiler will accept this addition without having checked that the stated rule actually holds.


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept BillingAccount

field Weight as quantity in 'kg' default '0 kg'
field ScaledWeight as quantity in 'kg' default '0 kg'
field Factor as decimal default 1.0

event Create(Load as quantity in 'kg', Rate as decimal) initial
event Finalize

state Draft initial
state Finalized terminal

on Create
    -> set Weight = Create.Load
    -> set Factor = Create.Rate

from Draft on Finalize
    -> transition Finalized

to Finalized
    -> set ScaledWeight = Weight / Factor
```

Required outcome: reject, naming the missing premise classes (a), (c), (d), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*field-modifier* — arg-modifier

- Addition: field Factor as decimal nonzero default 1.0
- Premise classes: (a)
- Derivation: declared field modifier excludes zero at every assignment -> field-bound discharge
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No application locus fits a field-declaration modifier (see cell notes); the addition text is recorded directly instead.

*guard* — guard

- Addition: to Finalized when Factor != 0.0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the obligation -> guard-match
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No application locus fits a state-target when guard (see cell notes); the addition text is recorded directly instead.

*rule* — other

- Addition: rule Factor != 0.0 because "Factor must never be zero"
- Premise classes: (d)
- Derivation: rule fact normal-form-equal to the obligation, consumed via CompositionalConstraint
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Live-verified clean compile at e1a14d91, but per the group's HAZARD ruling this is NOT recorded as proven-today: the discharge runs through a business rule used as premise (d), which slice 2 verified as a false-proof path (authored-expressiveness-gaps.md Defect B) — nothing establishes the rule, so the clean compile does not confirm the divisor is actually nonzero. See unverifiableWitnesses.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No application locus fits a new top-level rule declaration (see cell notes); the addition text is recorded directly instead.
- This witness is listed in the task's unverifiableWitnesses output: a clean compile via a rule-as-premise (d) discharge is not treated as confirmation, per the group's HAZARD instruction.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| field-modifier | field Factor as decimal nonnegative default 1.0 | nonnegative allows zero — Factor = 0.0 is still admitted, and the divisor can still be zero at the state hook — must still reject, same obligation | reject, naming the same obligation |
| guard | to Finalized when Factor >= 0.0 | >= does not exclude zero — must still reject, same obligation | reject, naming the same obligation |
| rule | rule Factor >= 0.0 because "Factor must never be negative" | >= does not exclude zero — must still reject, same obligation (and, per the near-miss run, the built engine does reject this one — the false proof above is specific to a rule text that normal-form-matches the exact obligation, not a general acceptance of any rule mentioning the field) | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Divisor type is primitive (decimal); obligation and discharge shape mirror the construction-row sibling cell in this file, re-sited to a field the state hook reads from residency rather than an arg the row reads from the event.
- Class (c) discharge built status: Guard-match discharges cleanly for a decimal divisor field (live-verified) — the same behaviour as the primitive lane.
- State hooks fire on every inbound/outbound edge regardless of which event triggered the transition, so no event args are in scope here (class (b) is structurally unavailable) — the mirror image of the construction-row asymmetry above, per the matrix's own account of this axis pairing (group 2's setting, same two premise asymmetries, applied here to a qualified divisor).
- The application DU (docs/Working/obligation-discharge-matrix-2026-07-19-cells/cell.schema.json $defs.application) carries only row-guard (keyed by eventName) and event-arg-declarations (keyed by eventName/argName). A state hook's own when guard is keyed by a state target, not an event name, and a field-declaration modifier is neither a row guard nor an event-arg declaration. Neither locus fits, so application is omitted on this witness rather than force-fit into the wrong shape; the same gap applies to the rule addition below, which is a new top-level declaration with no row or arg to attach to.
- HAZARD: the class-(d) discharge below is exactly the false-proof path this group's authoring notes require flagging. The witness compiles clean at HEAD (live-verified), but per instruction that clean compile is NOT recorded as builtStatus proven-today — it is recorded unresolved-today with the hazard named, and listed separately as an unverifiable witness. The near-miss for this addition (rule weakened to >= 0) correctly still rejects, which is itself informative: the built match is a syntactic normal-form check against the rule text, not a soundness check of the rule's own establishment, so a rule stating the wrong thing is rejected while a rule stating the right thing is wrongly accepted without anything having verified it holds.
- QuantityDivideDecimal — quantity / decimal ranges over a business-domain (Quantity) numerator and a primitive (decimal) divisor, so this coordinate is live at both typeFamily=business-domain and typeFamily=primitive (src/Precept/Language/Operations.cs:543). This cell's coordinates carry typeFamily=business-domain; it also instantiates the equivalent primitive-family coordinate unchanged, since the safety precondition's subject is the divisor and none of the three derivations above reads the numerator's type.

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault-prevention obligation family
- src/Precept/Language/Operations.cs:543 — code: QuantityDivideDecimal — quantity / decimal — NumericProofRequirement on the right operand: 'Divisor must be non-zero'
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses — matrix: Family 4 — the fault-family base/discharge/near-miss pattern this cell instantiates
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — matrix: Fault minting bullet: catalog-stamped at evaluation sites

## g08/pdq-construction-row — Price ÷ compound quantity, construction row

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row: catalog-declared safety precondition at the evaluation site, discharge by premise classes (b)/(c)/(a) per catalog ProofSatisfactions

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | op/PriceDivideQuantity/numeric-0 |
| evaluation site category | construction-row-action-operand |
| type family | business-domain |

### What must be proven

Obligation: Create.Usage != '0 kg/h'

Weakest precondition: Create.Usage != '0 kg/h'
Key pinned by: Not computed: tools/Precept.MatrixTools' WP calculator (WpCalculator.ComputeEstablishmentWp / ComputePreservationWp) targets rule-family establishment/preservation obligations. The fault-family catalog-declared safety precondition (ProofRequirement) is a different obligation shape the calculator does not reach; live runs against this witness show it as a compile-time [Error] DivisionByZero diagnostic or its absence, never as a printed WP line — a named skip, not an oversight.

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Create.Usage | the event arg supplying the divisor operand |
| '0 kg/h' | the divisor type's own zero — the qualified zero compound quantity '0 kg/h' |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (b), (c).

The divisor is read directly from the event's own arg, with no prior write in the plan touching it, so class (b) is available. The construction row carries its own optional when guard (spec § Construction semantics), so class (c) is available too. Class (d) has nothing to instantiate: no pre-state configuration exists before a construction row fires.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (b)**

- Derivation: ingress governance enforces the declared arg modifier on Create.Usage before any computation reads it, so the runtime value satisfies the modifier's ProofSatisfaction (nonzero: value != 0, or positive: value > 0 which subsumes it) at the point of division
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Look up a nonzero-or-positive ProofSatisfaction for the divisor in its declared arg modifiers (a finite set, ModifierMeta.ProofSatisfactions); its absence, or a bound that does not exclude zero, fails the class.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses — matrix: Family 4's own arg-constraint decision procedure: compute the divisor's interval from its declared modifiers; zero inside the interval, or no bound excluding it, rejects

**Entry 2 — (c)**

- Derivation: guard fact normal-form-equal to the obligation (Create.Usage != '0 kg/h') -> guard-match
- Validity arguments: Guard normal-form match
- Decision procedure: Normal-form match of the divisor-nonzero condition against the construction row's guard conjuncts (a finite set, per the row's own optional when clause).

- docs/language/precept-language-spec.md § Construction semantics — spec: "Why guards on `on <Event>` are allowed": on Event when condition -> actions is semantically coherent for construction rows, on the same EventRow construct as stateless handlers; live-verified 2026-07-21 at e1a14d91 (guard-test.precept compiled clean with a construction-row when guard present)

### What the failing diagnostic must suggest

- For class (b): bound the argument such that its declared modifier excludes zero (<WP>)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept BillingAccount

field UnitPrice as price in 'USD/kg' default '0.00 USD/kg'
field ElevatedPrice as price optional

event Create(Rate as price in 'USD/kg', Usage as quantity in 'kg/h') initial

on Create
    -> set UnitPrice = Create.Rate
    -> set ElevatedPrice = Create.Rate / Create.Usage
```

Required outcome: reject, naming the missing premise classes (b), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*arg-modifier* — arg-modifier

- Addition: Usage as quantity in 'kg/h' nonzero
- Premise classes: (b)
- Derivation: declared arg modifier excludes zero at ingress -> arg-bound discharge
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: applied by replacing the named event-arg declarations
  - `Create.Usage` becomes `Usage as quantity in 'kg/h' nonzero`

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

*guard* — guard

- Addition: when Create.Usage != '0 kg/h'
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the obligation -> guard-match
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Guard-match does NOT discharge for a compound-quantity divisor: live-verified run still reports DivisionByZero with the identical guard spelling that discharges cleanly for a decimal divisor on the sibling cells in this file. Same built-power gap as MoneyDivideQuantity, here on a compound (rate) quantity rather than a plain one.)
- How it applies to the base: appended to the guard of the `on Create` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| arg-modifier | Usage as quantity in 'kg/h' nonnegative | nonnegative allows zero — Create.Usage = '0 kg/h' is still admitted, and the divisor can still be zero at the division — must still reject, same obligation | reject, naming the same obligation |
| guard | when Create.Usage >= '0 kg/h' | >= does not exclude zero — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Divisor type is business-domain (compound quantity in 'kg/h'); the obligation and both discharge derivations are the same interval/guard-match story as the primitive lane (group 1/2), applied to a business-domain compound quantity divisor instead of a bare decimal — group 7's authoring note names exactly this distinction.
- Class (c) discharge built status: Guard-match does NOT discharge for a compound-quantity divisor: live-verified run still reports DivisionByZero with the identical guard spelling that discharges cleanly for a decimal divisor on the sibling cells in this file. Same built-power gap as MoneyDivideQuantity, here on a compound (rate) quantity rather than a plain one.
- Construction rows have no pre-state (premise (d) is structurally unavailable — no configuration exists before the event fires), so class (d) is not in this cell's applicable set at all, unlike the state-hook sibling cell below.
- A field-modifier discharge (class (a)) is not separately witnessed here: in this setting the divisor is supplied directly as an event arg, so the divisor-nonzero fact the construction row's write plan actually reads comes from the arg (class (b)), not from a field materialized earlier in the same plan. Class (a) remains structurally available at this site in general (an already-materialised field's own modifier, read before this write) and is left out of applicablePremiseClasses because this cell's own written expression does not read one.

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault-prevention obligation family
- src/Precept/Language/Operations.cs:691 — code: PriceDivideQuantity — price / compound-quantity (dimension elevation) — NumericProofRequirement on the right operand: 'Divisor must be non-zero'
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses — matrix: Family 4 — the fault-family base/discharge/near-miss pattern this cell instantiates
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — matrix: Fault minting bullet: catalog-stamped at evaluation sites

## g08/pdq-state-hook — Price ÷ compound quantity, state hook

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row: catalog-declared safety precondition at the evaluation site, discharge by premise classes (b)/(c)/(a) per catalog ProofSatisfactions

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | op/PriceDivideQuantity/numeric-0 |
| evaluation site category | state-hook-action-operand |
| type family | business-domain |

### What must be proven

Obligation: Usage != '0 kg/h'

Weakest precondition: Usage != '0 kg/h'
Key pinned by: Not computed: tools/Precept.MatrixTools' WP calculator targets rule-family establishment/preservation obligations, not the fault-family's catalog-declared safety precondition. A named skip, as on the construction-row sibling cell.

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Usage | the field supplying the divisor operand, materialised at construction and read again at the state hook |
| '0 kg/h' | the divisor type's own zero — the qualified zero compound quantity '0 kg/h' |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a), (c), (d).

The divisor is a field, already materialised by construction before the state hook fires while resident; its own declared modifier is available as class (a). The state hook carries its own optional pre-verb when guard (spec § State action), so class (c) is available. A pre-state configuration exists (the entity has been constructed and has resided in prior states), so class (d) is structurally available too — subject to the HAZARD noted above. Class (b) has nothing to instantiate: no event args are in scope inside a state hook.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: the field's own declared modifier is enforced at every assignment to it (the modifier desugars to a rule, Modifiers.cs DesugarsToRule), so the runtime value of Usage satisfies the modifier's ProofSatisfaction whenever the state hook reads it
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Look up a nonzero-or-positive ProofSatisfaction for the divisor field in its declared field modifiers; its absence, or a bound that does not exclude zero, fails the class.

- src/Precept/Language/Modifiers.cs:100 — code: ModifierKind.Nonzero: 'Value != 0', DesugarsToRule: true
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses — matrix: Family 4's decision procedure, generalized from arg modifiers to field modifiers — the written argument (Arg-bound interval arithmetic) is scoped in the matrix text to event args at ingress; its extension to a field modifier enforced at every assignment is the same governance reasoning, not separately argued in the matrix as its own named rule — recorded honestly rather than silently assumed

**Entry 2 — (c)**

- Derivation: guard fact normal-form-equal to the obligation (Usage != '0 kg/h') -> guard-match
- Validity arguments: Guard normal-form match
- Decision procedure: Normal-form match of the divisor-nonzero condition against the state hook's own pre-verb when guard conjuncts (a finite set).

- docs/language/precept-language-spec.md § State action — spec: state actions (entry/exit hooks) support an optional pre-verb when guard between the state target and the action chain

**Entry 3 — (d)**

- Derivation: a business rule stating the divisor's nonzero fact, consumed as a pre-state premise
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: The built engine's CompositionalConstraint strategy normal-form-matches the divisor-nonzero condition against the file's declared rule set and reports Proved when a match is found — but this document names no validity argument that licenses a rule as a premise (d) source for a fault obligation, and the matrix's own open design hole ("nothing detects an obligation that was never minted", § The cell) plus the verified instance in authored-expressiveness-gaps.md Defect B mean this match is not a sound decision procedure as implemented: the rule's own establishment and preservation are never independently checked, so a clean compile through this path does not confirm the divisor is actually nonzero at runtime. Recorded per the HAZARD ruling for this group: builtStatus is unverified, not proven, regardless of what the compiler reports.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § The cell — matrix: Open design hole — nothing detects an obligation that was never minted; the verified instance is a rule consumed as a premise while nothing establishes it
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — matrix: Defect B — a rule is consumed as a premise while nothing establishes it (fail-open); the identical CompositionalConstraint pattern measured here on a business-domain divisor

### What the failing diagnostic must suggest

- For class (a): bound the field such that its declared modifier excludes zero (<WP>)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (d): state a rule normal-form-equal to <WP>, established at construction and preserved by every write to the field
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

  - This is the definition's committed suggestion under the model. It is not currently a suggestion an author should trust as verified: the HAZARD note on this cell's class-(d) contract entry means the compiler will accept this addition without having checked that the stated rule actually holds.


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept BillingAccount

field UnitPrice as price in 'USD/kg' default '0.00 USD/kg'
field Usage as quantity in 'kg/h' default '1 kg/h'
field ElevatedPrice as price optional

event Create(Rate as price in 'USD/kg', UsageRate as quantity in 'kg/h') initial
event Finalize

state Draft initial
state Finalized terminal

on Create
    -> set UnitPrice = Create.Rate
    -> set Usage = Create.UsageRate

from Draft on Finalize
    -> transition Finalized

to Finalized
    -> set ElevatedPrice = UnitPrice / Usage
```

Required outcome: reject, naming the missing premise classes (a), (c), (d), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*field-modifier* — arg-modifier

- Addition: field Usage as quantity in 'kg/h' nonzero default '1 kg/h'
- Premise classes: (a)
- Derivation: declared field modifier excludes zero at every assignment -> field-bound discharge
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No application locus fits a field-declaration modifier (see cell notes); the addition text is recorded directly instead.

*guard* — guard

- Addition: to Finalized when Usage != '0 kg/h'
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the obligation -> guard-match
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Guard-match does NOT discharge for a compound-quantity divisor field either (live-verified) — same built gap as the construction-row sibling cell.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No application locus fits a state-target when guard (see cell notes); the addition text is recorded directly instead.

*rule* — other

- Addition: rule Usage != '0 kg/h' because "Usage must never be zero"
- Premise classes: (d)
- Derivation: rule fact normal-form-equal to the obligation, consumed via CompositionalConstraint
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Live-verified clean compile at e1a14d91, but per the group's HAZARD ruling this is NOT recorded as proven-today: the discharge runs through a business rule used as premise (d), which slice 2 verified as a false-proof path (authored-expressiveness-gaps.md Defect B) — nothing establishes the rule, so the clean compile does not confirm the divisor is actually nonzero. See unverifiableWitnesses.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No application locus fits a new top-level rule declaration (see cell notes); the addition text is recorded directly instead.
- This witness is listed in the task's unverifiableWitnesses output: a clean compile via a rule-as-premise (d) discharge is not treated as confirmation, per the group's HAZARD instruction.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| field-modifier | field Usage as quantity in 'kg/h' nonnegative default '1 kg/h' | nonnegative allows zero — Usage = '0 kg/h' is still admitted, and the divisor can still be zero at the state hook — must still reject, same obligation | reject, naming the same obligation |
| guard | to Finalized when Usage >= '0 kg/h' | >= does not exclude zero — must still reject, same obligation | reject, naming the same obligation |
| rule | rule Usage >= '0 kg/h' because "Usage must never be negative" | >= does not exclude zero — must still reject, same obligation (and, per the near-miss run, the built engine does reject this one — the false proof above is specific to a rule text that normal-form-matches the exact obligation, not a general acceptance of any rule mentioning the field) | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Divisor type is business-domain (compound quantity in 'kg/h'); obligation and discharge shape mirror the construction-row sibling cell in this file, re-sited to a field the state hook reads from residency rather than an arg the row reads from the event.
- Class (c) discharge built status: Guard-match does NOT discharge for a compound-quantity divisor field either (live-verified) — same built gap as the construction-row sibling cell.
- State hooks fire on every inbound/outbound edge regardless of which event triggered the transition, so no event args are in scope here (class (b) is structurally unavailable) — the mirror image of the construction-row asymmetry above, per the matrix's own account of this axis pairing (group 2's setting, same two premise asymmetries, applied here to a qualified divisor).
- The application DU (docs/Working/obligation-discharge-matrix-2026-07-19-cells/cell.schema.json $defs.application) carries only row-guard (keyed by eventName) and event-arg-declarations (keyed by eventName/argName). A state hook's own when guard is keyed by a state target, not an event name, and a field-declaration modifier is neither a row guard nor an event-arg declaration. Neither locus fits, so application is omitted on this witness rather than force-fit into the wrong shape; the same gap applies to the rule addition below, which is a new top-level declaration with no row or arg to attach to.
- HAZARD: the class-(d) discharge below is exactly the false-proof path this group's authoring notes require flagging. The witness compiles clean at HEAD (live-verified), but per instruction that clean compile is NOT recorded as builtStatus proven-today — it is recorded unresolved-today with the hazard named, and listed separately as an unverifiable witness. The near-miss for this addition (rule weakened to >= 0) correctly still rejects, which is itself informative: the built match is a syntactic normal-form check against the rule text, not a soundness check of the rule's own establishment, so a rule stating the wrong thing is rejected while a rule stating the right thing is wrongly accepted without anything having verified it holds.

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault-prevention obligation family
- src/Precept/Language/Operations.cs:691 — code: PriceDivideQuantity — price / compound-quantity (dimension elevation) — NumericProofRequirement on the right operand: 'Divisor must be non-zero'
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses — matrix: Family 4 — the fault-family base/discharge/near-miss pattern this cell instantiates
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — matrix: Fault minting bullet: catalog-stamped at evaluation sites

