<!--
GENERATED FILE — do not hand-edit.
Source: family-1-numeric-single-field.cells.json
Regenerate: dotnet run --project tools/Precept.MatrixTools -- render-cells docs/Working/obligation-discharge-matrix-2026-07-19-cells
Hand edits will be overwritten. Edit the .cells.json and regenerate.
-->

# Witness Family 1 — numeric single-field, handler set (live-verified slice)

Family id: witness-family-1
Case shape: rule-write
Definition version: obligation-discharge-matrix-2026-07-19 rev 6 (2026-07-20, draft)

**Where this family's content comes from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witness Family 1 — matrix
- docs/Working/what-i-want-2026-07-16.md:19 — want-doc: the four premise classes
- docs/Working/what-i-want-2026-07-16.md:20 — want-doc: symmetric attachment
- docs/Working/what-i-want-2026-07-16.md:22 — want-doc: max desugars to rule Total <= 1000

## Respellability — the family verdict

Can every safe program these rules reject be rewritten into a form they accept? **yes**

Evidence: model-derived
Corpus measurement: pending

Spellings that look rejected but are in fact licensed (kept so the boundary stays findable):

- `when Add.Amount1 <= 500 and Add.Amount2 <= 500` — Per-term bound conjuncts discharge by the same interval derivation as arg bounds (500 + 500 <= 1000), spelled in the guard instead of on the args; premise class (c), derivation shared with (b). Licensed, therefore not in the sound-but-unprovable band.
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — owner-ruling, 2026-07-19: the sound-but-unprovable band bullet: per-term bound conjuncts discharge by the same interval derivation as arg bounds


**Notes on the family verdict**

- All three preservation bases inherit this verdict; none states an override.
- Every sound band member's business intent respells into a licensed guard or bound form (matrix, Family 1 respellability paragraph).

**Sources for this verdict**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witness Family 1 — matrix: the respellability paragraph
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Ratification protocol — matrix: layer 4: corpus measurement is the hard gate; a family verdict ratifies only when it agrees with the classified sample corpus

## Premise classes — the legend

Where a proof may get its facts (want :19, :143): (a) field modifiers, (b) arg constraints, (c) the handler's guard, (d) all rules holding in the pre-state (the inductive hypothesis), (e) evaluating the applicable rule set at an editable-field write — (e)'s status as a genuinely fifth class vs (a) generalized is an open owner item.

## wf1/establishment-defaults — Standing base-case obligation (establishment over defaults)

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:18 — want-doc: base case: the default configuration satisfies every rule

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | rule-write |
| obligation family | establishment |
| rule structure | single-field |
| write site category | construction-defaults |
| type family | primitive |

### What must be proven

Obligation: default 0.0 satisfies Total <= 1000

Weakest precondition: 0.0 <= 1000
Canonical key: true
Key pinned by: WpCalculator.ComputeEstablishmentWp; pinned in test/Precept.MatrixTools.Tests/WitnessFamily1Tests.cs (Establishment_DefaultSatisfiesMax_FoldsToTrue)

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Total | the field the rule bounds |
| 0.0 | the field's declared default |
| 1000 | the bound |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft: rules N1-N14; N9-division and N14 are owner-ruled but not implemented in the calculator

### Which premise classes can discharge it

Applicable classes: none — no authorable premise is consumed here.

Discharged by constant folding over the default configuration — no authored premise class is consumed.

### The discharge contract — exactly when this counts as proven

**Entry 1 — no authored premise**

- Derivation: constant folding of the rule over the default environment
- Strategy: Literal
- Validity arguments: Literal constant-fold for defaults; Establishment over defaults
- Decision procedure: Constant-fold the rule over the default environment. The fold terminates because expressions are finite, loop-free trees (spec section 0.4). A fold that evaluates false, or cannot complete, leaves the obligation undischarged: reject.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Establishment over defaults (the pre-configuration rule) — the companion argument for taking the WP over the default configuration

### What the failing diagnostic must suggest

Nothing — the applicable-class set is empty, so no authorable premise exists to suggest.

### The worked examples

**Base.** None recorded — see this cell's notes for why.

**Discharge additions — what makes the base compile.**

*default-fold* — default-constant-fold

- Addition: none — a standing discharge (nothing authored to weaken, so no near-miss is owed)
- Derivation: constant fold: default 0.0 satisfies Total <= 1000 (Literal)
- Strategy: Literal
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (precept_compile, 2026-07-19, HEAD ed57d7fd) — the run attests the built-status claim, not the required outcome.

### Respellability

- Resolution: family-inherited

- The matrix's family verdict paragraph names the three preservation bases explicitly; this cell inherits under the family-level rule (verdict stated once per contract family, inherited by every cell the family's contracts govern).

**What the sources leave unstated or ambiguous here**

- The matrix records this cell as a standing discharged obligation only — no rejecting base program and no near-miss triple are stated for it; none is invented here. The discharge is standing (no authored addition exists to weaken), so no near-miss is owed under the one-per-addition rule.
- The WP 0.0 <= 1000 constant-folds to true over the default environment; the canonical key records the folded form.
- suggestions is empty because the applicable-class set is empty: no authorable premise class exists at this site, so there is no per-class suggestion to state.
- No initial event exists in this slice; the establishment half over initial events is vacuous here (matrix, Witness Family 1).
- The write-site coordinate construction-defaults is a file-local value: the matrix's write-site axis (axis 3) lists only authored write-site kinds and has no member for the establishment-over-defaults site — an open vocabulary item flagged, not silently absorbed.

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:18 — want-doc
- docs/Working/what-i-want-2026-07-16.md:22 — want-doc: max 1000 desugars to rule Total <= 1000
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witness Family 1 — matrix: the standing base-case obligation paragraph

## wf1/preservation-reads-args — Base A — RHS reads args only

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:19 — want-doc: inductive step: preservation from the four premise classes

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | rule-write |
| obligation family | preservation |
| rule structure | single-field |
| write site category | handler-set |
| RHS read set | args |
| type family | primitive |

### What must be proven

Obligation: Amount1 + Amount2 <= 1000

Weakest precondition: Amount1 + Amount2 <= 1000
Canonical key: (le (+ arg(Add.Amount1) arg(Add.Amount2)) #1000)
Key pinned by: WpCalculator.ComputePreservationWp; pinned in test/Precept.MatrixTools.Tests/WitnessFamily1Tests.cs (BaseA_WpIsArgSumBound)

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Amount1 | an event arg the RHS reads |
| Amount2 | an event arg the RHS reads |
| 1000 | the bound |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (b), (c).

The read set does not intersect the mention set and the written field is not read, so premise (d) has nothing to instantiate — availability is derived per the read-set axis, not from 'overwrite'.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: guard fact normal-form-equal to the WP -> guard-match
- Capability tier: 1a
- Validity arguments: Guard normal-form match
- Decision procedure: Normal-form match of the WP against the row's guard conjuncts (a finite set).

**Entry 2 — (c)**

- Derivation: per-term bound conjuncts (term op constant) over the WP's terms, combined by the same exact-decimal interval arithmetic as class (b)
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Failing the whole-condition match: collect the guard's per-term bound conjuncts (term op constant) over the WP's terms and run the same exact-decimal interval combination as class (b) — a term with no bound conjunct, or a combined bound exceeding the rule bound, fails the derivation.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — owner-ruling, 2026-07-19: the sound-but-unprovable band bullet: per-term bound conjuncts feed the same interval arithmetic that discharges arg-modifier bounds, wherever they are spelled

**Entry 3 — (b)**

- Derivation: exact-decimal upper-bound sum over the args' declared max bounds -> IntervalContainment
- Strategy: IntervalContainment
- Capability tier: 1a
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Exact-decimal upper-bound sum over the args' declared max bounds; an arg with no upper bound, or a computed sum exceeding the rule bound, fails the class. If all derivations fail, reject, naming both classes.

### What the failing diagnostic must suggest

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (b): bound the args such that the interval combination satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

  - The matrix's cell text states 'The suggestion schema names both'; the per-class schema texts here are the Vocabulary's own stated schemas, not cell-local wording.


### The worked examples

**Base — the program the compiler must reject.**

```precept
field Total as decimal max 1000 default 0.0
event Add(Amount1 as decimal, Amount2 as decimal)
on Add -> set Total = Add.Amount1 + Add.Amount2
```

Required outcome: reject, naming the missing premise classes (b), (c), with no other diagnostics

Provenance: model-derived.

**Discharge additions — what makes the base compile.**

*guard-nf* — guard

- Addition: when Add.Amount1 + Add.Amount2 <= 1000
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the WP -> guard-match (capability tier 1a)
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (multi-term fact shape unbuilt)
- How it applies to the base: appended to the guard of the `on Add` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (precept_compile, 2026-07-19, HEAD ed57d7fd) — the run attests the built-status claim, not the required outcome.

*arg-bounds* — arg-modifier

- Addition: Amount1 max 250, Amount2 max 750
- Premise classes: (b)
- Derivation: 250 + 750 <= 1000 -> IntervalContainment
- Strategy: IntervalContainment
- What the engine does today: proven-today
- How it applies to the base: applied by replacing the named event-arg declarations
  - `Add.Amount1` becomes `Amount1 as decimal max 250`
  - `Add.Amount2` becomes `Amount2 as decimal max 750`

Required outcome: accept, with the premise list recorded

Provenance: live-verified (precept_compile, 2026-07-19, HEAD ed57d7fd) — the run attests the built-status claim, not the required outcome.

- The 250/750 split is an instantiation witness of the class schema — the suggestion is the schema, never these constants.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| arg-bounds | Amount1 max 600, Amount2 max 600 | 600 + 600 = 1200 > 1000 — must still reject, same obligation | reject, naming the same obligation |
| guard-nf | when Add.Amount1 <= 1000 | guard does not imply the WP — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited
- A representative safe-but-rejected spelling: when Add.Amount1 <= 1000 - Add.Amount2
- Its licensed respelling: the normal-form guard (when Add.Amount1 + Add.Amount2 <= 1000) or per-term bounds

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witness Family 1 — matrix: the respellability paragraph — sound, but no defined derivation covers moving a term across the inequality

**What the sources leave unstated or ambiguous here**

- The matrix's witness tables mark provenance on addition statuses only; base rows carry no explicit live-verification mark, so the base is recorded model-derived per the matrix's 'unmarked = model-derived' rule, even though the family is titled a live-verified slice.

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:19 — want-doc
- docs/Working/what-i-want-2026-07-16.md:20 — want-doc
- docs/Working/what-i-want-2026-07-16.md:22 — want-doc
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witness Family 1 — matrix: the Base A block

## wf1/preservation-reads-written-decrease — Base B — RHS reads the written field, decrease

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:19 — want-doc

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | rule-write |
| obligation family | preservation |
| rule structure | single-field |
| write site category | handler-set |
| RHS read set | written-field, args |
| type family | primitive |

### What must be proven

Obligation: Total_pre - Amount <= 1000

Weakest precondition: Total_pre - Amount <= 1000
Canonical key: (le (+ (neg arg(Subtract.Amount)) pre(Total)) #1000)
Key pinned by: WpCalculator.ComputePreservationWp; pinned in test/Precept.MatrixTools.Tests/WitnessFamily1Tests.cs (BaseB_WpReadsPreStateTotal)

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Total_pre | the written field's pre-state value |
| Amount | the event arg the RHS subtracts |
| 1000 | the bound |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (d), (b).

The read set includes the written field, so premise (d) is available.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (d), (b)**

- Derivation: (d) Total_pre <= 1000 plus (b) Amount >= 0: Total_pre - Amount <= Total_pre <= 1000 -> linear-with-hypothesis
- Capability tier: 1a
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Shape-match the RHS as written-field-minus-term (one syntactic pattern). Then look up a nonnegativity fact for the subtracted term in its declared modifiers (a finite set — the modifier-to-ProofSatisfaction table, docs/compiler/proof-engine.md § Strategy 2), and the rule's own bound in the pre-state hypothesis. Either lookup failing rejects.
  - Open items this answer is load-bearing on: S4, Q5

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: the inductive-hypothesis argument names its open dependency: the hypothesis is only as sound as the minting rule is complete, so the mention set (S4, which also carries computed-field transitivity) and activation sites (Q5) are load-bearing for every cell consuming premise (d); a missed write site voids the hypothesis

### What the failing diagnostic must suggest

- For class (b): bound the args such that the interval combination satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

  - The contract's class-(d) half is the standing inductive hypothesis, not an authorable premise; the base's missing-class list names (b) alone, so the suggestion addresses (b). For this cell the interval fact the combination needs is the subtracted term's sign bound (nonnegativity).


### The worked examples

**Base — the program the compiler must reject.**

```precept
field Total as decimal max 1000 default 0.0
event Subtract(Amount as decimal)
on Subtract -> set Total = Total - Subtract.Amount
```

Required outcome: reject, naming the missing premise classes (b), with no other diagnostics

Provenance: model-derived.

**Discharge additions — what makes the base compile.**

*nonnegative-arg* — arg-modifier

- Addition: Amount as decimal nonnegative
- Premise classes: (d), (b)
- Derivation: Total_pre - Amount <= Total_pre <= 1000 -> linear-with-hypothesis (capability tier 1a)
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (pre-state rule not carried as a premise — the engine widens Total to [-inf .. +inf] at the read)
- How it applies to the base: applied by replacing the named event-arg declarations
  - `Subtract.Amount` becomes `Amount as decimal nonnegative`

Required outcome: accept, with the premise list recorded

Provenance: live-verified (precept_compile, 2026-07-19, HEAD ed57d7fd) — the run attests the built-status claim, not the required outcome.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| nonnegative-arg | Amount as decimal max 100 | sign unconstrained: a negative Amount still breaches — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

- The matrix states Bases B and C are analogous to Base A — every sound band member's business intent respells into a licensed guard or bound form; no concrete band member is stated for this cell, and none is invented here.

**What the sources leave unstated or ambiguous here**

- The matrix does not enumerate this cell's full applicable-class set; it states only that premise (d) is available, and its decision procedure names the (d)+(b) shape alone. Whether class (c) (a guard restating the WP) is applicable here is unstated — the guard-match validity argument names Bases A and C only. Recorded as stated; flagged, not resolved.
- The matrix does not state the base's named missing-class list for this cell; (b) is recorded as the class the stated addition supplies.
- The matrix gives only the row for this base; the event declaration (event Subtract(Amount as decimal), unconstrained) is reconstructed as the base the stated addition modifies.

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:19 — want-doc
- docs/Working/what-i-want-2026-07-16.md:22 — want-doc
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witness Family 1 — matrix: the Base B block

## wf1/preservation-reads-written-increase — Base C — RHS reads the written field, increase

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:19 — want-doc

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | rule-write |
| obligation family | preservation |
| rule structure | single-field |
| write site category | handler-set |
| RHS read set | written-field, args |
| type family | primitive |

### What must be proven

Obligation: Total_pre + Amount <= 1000

Weakest precondition: Total_pre + Amount <= 1000
Canonical key: (le (+ arg(Grow.Amount) pre(Total)) #1000)
Key pinned by: WpCalculator.ComputePreservationWp; pinned in test/Precept.MatrixTools.Tests/WitnessFamily1Tests.cs (BaseC_LicensedGuardMatchesWp)

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Total_pre | the written field's pre-state value |
| Amount | the event arg the RHS adds (declared positive in the base) |
| 1000 | the bound |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (c).

Premise (d) is available but insufficient (1000 + positive > 1000); no arg bound closes it; (c) is the only applicable class — the applicable-class set is a per-cell fact.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: guard fact normal-form-equal to the WP -> guard-match
- Capability tier: 1a
- Validity arguments: Guard normal-form match
- Decision procedure: The class-(c) normal-form match, against this row's guard conjuncts. No match rejects (no other class applies in this cell — the applicable-class set is the cell's own fact).

### What the failing diagnostic must suggest

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
field Total as decimal max 1000 default 0.0
event Grow(Amount as decimal positive)
on Grow -> set Total = Total + Grow.Amount
```

Required outcome: reject, naming the missing premise classes (c), with no other diagnostics

Provenance: model-derived.

**Discharge additions — what makes the base compile.**

*guard-nf* — guard

- Addition: when Total + Grow.Amount <= 1000
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the WP -> guard-match (capability tier 1a)
- Provable under the definition: provable-under-model
- How it applies to the base: appended to the guard of the `on Grow` row

Required outcome: accept, with the premise list recorded

Provenance: model-derived.

- The matrix states 'provable under want' with no built-status entry and no live-verification mark for this addition; the built status is left unstated rather than inferred.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| guard-nf | when Grow.Amount <= 1000 | ignores Total_pre — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

- The matrix states Bases B and C are analogous to Base A — every sound band member's business intent respells into a licensed guard or bound form; no concrete band member is stated for this cell, and none is invented here.

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:19 — want-doc
- docs/Working/what-i-want-2026-07-16.md:22 — want-doc
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witness Family 1 — matrix: the Base C block

