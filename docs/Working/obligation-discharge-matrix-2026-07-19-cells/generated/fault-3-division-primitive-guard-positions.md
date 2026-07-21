<!--
GENERATED FILE — do not hand-edit.
Source: fault-3-division-primitive-guard-positions.cells.json
Regenerate: dotnet run --project tools/Precept.MatrixTools -- render-cells docs/Working/obligation-discharge-matrix-2026-07-19-cells
Hand edits will be overwritten. Edit the .cells.json and regenerate.
-->

# Fault Family 3 -- division by zero inside a guard, where the guard cannot discharge itself

Family id: fault-3-division-primitive-guard-positions
Case shape: fault
Definition version: obligation-discharge-matrix-2026-07-19 rev 7 (2026-07-20, draft)

**Where this family's content comes from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: the four premise classes
- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: the Fault family case-shape row
- docs/Working/obligation-discharge-matrix-2026-07-19.md:186 — matrix: Fault minting: catalog-stamped at evaluation sites
- docs/Working/obligation-discharge-matrix-2026-07-19.md:204 — matrix: Guard normal-form match -- why class (c) cannot discharge an obligation arising within its own evaluation
- docs/Working/obligation-discharge-matrix-2026-07-19.md:94 — matrix: the Open design hole -- nothing detects an obligation that was never minted; every cell in this file is a live instance of exactly this gap
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — review: the false-proof hazard this file's (d) discharges must be recorded against

## Respellability — the family verdict

Can every safe program these rules reject be rewritten into a form they accept? **yes**

Evidence: model-derived
Corpus measurement: pending

**Notes on the family verdict**

- Every safe program in this family's band has an unambiguous escape hatch that does not depend on any open dependency: declare the divisor's exclusion of zero directly as a field modifier (class a) -- positive, nonzero, or a min bound above zero -- rather than spelling it inside the guard. This routes through the same modifier-declaration mechanism class (a) already licenses (matrix line 38), independent of whether an earlier guard conjunct or a rule can ALSO discharge the same fact. The verdict is stated yes on the strength of that escape hatch alone.
- A genuine band member is the guard-spelled form itself when no modifier is declared -- e.g. `when Parts != 0 and Total / Parts > 1` -- because whether an earlier conjunct of the same guard licenses a later one is an open dependency this family does not resolve (see each cell's notes). Its respelling is the modifier-declaration form above.
- Not corpus-measured: this pass did not find a division inside a guard, where the guard is the only available conjunct and no modifier or rule exists on the divisor, exercised verbatim in `samples/` today.

**Sources for this verdict**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:45 — matrix: the respellable verdict definition
- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: (a) field modifiers

## Premise classes — the legend

Where a proof may get its facts (want :19, :143): (a) field modifiers, (b) arg constraints, (c) the handler's guard, (d) all rules holding in the pre-state (the inductive hypothesis), (e) evaluating the applicable rule set at an editable-field write — (e)'s status as a genuinely fifth class vs (a) generalized is an open owner item.

## g03/transition-row-guard-int — Transition-row guard -- integer lane

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:186 — matrix: Fault minting: catalog-stamped at evaluation sites
- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: the Fault family case-shape row

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | IntegerDivideInteger |
| evaluation site category | transition-row-guard |
| type family | primitive |

### What must be proven

Obligation: Parts != 0

Weakest precondition: Parts != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Total | the field read as the guard's dividend -- immaterial to the safety precondition itself |
| Parts | the field read as the guard's divisor |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:42 — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a), (d).

Derived per evaluation-site category, not asserted: this is a transition row's `when` (`from S on E when G -> action`). Classes (a) and (d) instantiate; (b) does not (see notes); (c) never does (structural, see notes).

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: field modifier on the divisor field, excluding zero -> interval containment over the modifier's declared bound
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor field's interval from its own declared modifiers (a finite set -- the modifier-to-ProofSatisfaction table, docs/compiler/proof-engine.md § Strategy 2); a computed interval excluding zero closes the obligation. Modelled on the matrix's own Family 4 sketch decision procedure (matrix line 299: "compute the divisor's interval from its declared modifiers and in-scope guard facts"), generalized here from premise class (b) (an event-arg modifier) to premise class (a) (a field modifier) on the same divisor. No excluding bound rejects, naming class (a).

- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: the four premise classes, including (a) field modifiers
- docs/Working/obligation-discharge-matrix-2026-07-19.md:206 — matrix: Arg-bound interval arithmetic -- the modifier-to-ProofSatisfaction interval derivation this entry extends from arg modifiers to field modifiers
- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 sketch -- "compute the divisor's interval from its declared modifiers and in-scope guard facts"

**Entry 2 — (d)**

- Derivation: a rule established and preserved over the divisor field, consumed as the inductive hypothesis -> the divisor's pre-state fact closes the obligation directly (CompositionalConstraint)
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Look up a nonzero fact for the divisor field among the constraints holding in the pre-state (the CompositionalConstraint strategy already exercised at HEAD for this exact shape -- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3, "rule PlannedQuantity > 0 ... discharges a division-by-zero obligation via the CompositionalConstraint strategy"). No such fact rejects, naming class (d).

- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: (d) all constraints holding in the pre-state
- docs/Working/obligation-discharge-matrix-2026-07-19.md:208 — matrix: Inductive hypothesis plus sign monotonicity -- extended here from its written sign-monotonicity/subtraction shape to a bare nonzero fact read directly
- docs/Working/obligation-discharge-matrix-2026-07-19.md:94 — matrix: the Open design hole section -- the matrix's own worked example of a rule consumed as premise (d) for a divisor-nonzero fault obligation
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — review: Defect B: a rule consumed as premise (d) while nothing establishes it (fail-open)

### What the failing diagnostic must suggest

- For class (a): declare a field modifier on <WP>'s divisor that excludes zero (e.g. positive, nonzero, or a min bound above zero)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md:46 — matrix: the suggestion-schema bullet

- For class (d): add a rule establishing and preserving <WP> over the divisor field, available as premise (d)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md:46 — matrix: the suggestion-schema bullet

  - A discharge accepted through this suggestion inherits the false-proof hazard: record its builtStatus as unresolved-today and its provenance as model-derived, never as confirmed by a clean compile, until the rule's own establishment/preservation obligations are built (docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3, Defect A and Defect B).


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept TrgBaseInt
field Total as integer default 10
field Parts as integer default 0
state Active initial
state Closed terminal
event Open initial
event Check
event Close
on Open
    -> set Total = 10
    -> set Parts = 0
from Active on Check when Total / Parts > 1
    -> no transition
from Active on Close
    -> transition Closed
```

Required outcome: reject, naming the missing premise classes (a), (d), with no other diagnostics

Provenance: model-derived.

**Discharge additions — what makes the base compile.**

*transition-row-guard-int-a* — other

- Addition: field Parts as integer default 1 nonzero
- Premise classes: (a)
- Derivation: field modifier excluding zero on the divisor -> interval containment (class a)
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Live-verified (Precept.MatrixTools, 2026-07-21, commit e1a14d91): this addition compiles with zero diagnostics and no obligation is reported for the division -- the same no-mint behaviour as the base. builtStatus is unresolved-today because the site does not mint at HEAD at all; a clean compile here records the site's failure to mint, not confirmation that this discharge is accepted. This discharge is a field modifier (class a), which is NOT subject to the false-proof hazard (task authoring notes: "guard-based and modifier-based discharges ... are unaffected").)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Replaces the base's `field Parts as integer default 0` declaration (and the matching construction assignment) with `field Parts as integer default 1 nonzero`. The compiled program is the base program with that one field declaration replaced; it is not stored verbatim anywhere, and no `application` record reconstructs it mechanically (see the schema-vocabulary note below).
- kind is recorded as "other": the closed dischargeWitness.kind vocabulary has "arg-modifier" for an event-arg modifier but no corresponding value for a FIELD modifier, even though class (a) field modifiers is named in the Vocabulary (matrix line 38) as a first-class premise source. Flagged as a vocabulary gap.
- The schema's `application` discriminated union (row-guard / event-arg-declarations) has no locus for a field-declaration modifier addition or a whole new `rule` declaration -- neither of this discharge's addition shapes is either of the two loci the schema expresses. `application` is omitted rather than forced into an ill-fitting locus; the day-one validator's cell-to-test conversion cannot yet mechanically apply this addition. Flagged as a schema-vocabulary gap, not resolved here.

*transition-row-guard-int-d* — other

- Addition: rule Parts != 0 because "Parts must stay nonzero"
- Premise classes: (d)
- Derivation: rule establishing the divisor's nonzero-ness, consumed as premise (d) via CompositionalConstraint
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Doubly unverifiable, so no live run is cited as confirmation here even though compiling this exact program shows the same no-mint behaviour as every other witness in this file: (i) this evaluation-site category mints no fault obligation at HEAD at all (measured on the base and on this addition); (ii) independent of (i), a `rule` consumed as premise (d) is exactly the false-proof-prone discharge path verified at HEAD (docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3, Defect B -- a rule can be accepted as a divisor-nonzero premise via the CompositionalConstraint strategy while nothing establishes or preserves it). A clean compile could never be read as confirmation of this discharge even if the site did mint. modelStatus is stated as provable-under-model on the strength of premise (d)'s general validity argument (matrix line 208), extended from its written sign-monotonicity/subtraction shape to a bare nonzero fact read directly -- an extension the matrix does not explicitly write out (see missing rules).)

Required outcome: accept, with the premise list recorded

Provenance: model-derived.

- Adds `rule Parts != 0 because "Parts must stay nonzero"` as a new top-level declaration and marks Total and Parts `editable` (parity with the standalone-rule shape the false-proof hazard's own repro uses; editability is incidental to the derivation, not load-bearing for it).
- kind is recorded as "other": the closed dischargeWitness.kind vocabulary has no value for "a new rule declaration added to the file" -- the closest options (guard, arg-modifier, default-constant-fold, structural-edit) all name a different shape. Flagged as a vocabulary gap.
- The schema's `application` discriminated union (row-guard / event-arg-declarations) has no locus for a field-declaration modifier addition or a whole new `rule` declaration -- neither of this discharge's addition shapes is either of the two loci the schema expresses. `application` is omitted rather than forced into an ill-fitting locus; the day-one validator's cell-to-test conversion cannot yet mechanically apply this addition. Flagged as a schema-vocabulary gap, not resolved here.
- Listed in this task's unverifiableWitnesses output: false-proof hazard (rule used as premise (d)), per docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3, Defect B, compounded by this site's no-mint status at HEAD.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| transition-row-guard-int-a | field Parts as integer default 0 nonnegative | `nonnegative` (unlike `nonzero`/`positive`) does not exclude 0: the divisor's declared interval still contains zero, so the interval-containment derivation does not close -- must still reject, same obligation. | reject, naming the same obligation |
| transition-row-guard-int-d | rule Parts >= 0 because "Parts must stay nonnegative" | `rule Parts >= 0` (unlike `!=`) does not exclude zero: the pre-state fact still admits a zero divisor, so the inductive-hypothesis derivation does not close -- must still reject, same obligation. | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Class (c) -- the handler's guard -- is structurally unavailable and is excluded from every contract entry in this file. The obligation arises from evaluating the guard expression's own division, before the guard has produced the truth value anything downstream (a row's actions, an ensure, a rule, an access-mode decision) could rely on. Class (c) is defined as a fact the guard's truth supplies to obligations arising after it (matrix line 38: "(c) the handler's guard"; the Guard normal-form match argument, matrix line 204, grounds (c) precisely in a row's actions executing only after the guard evaluated true -- a precondition the guard's own internal evaluation cannot satisfy about itself). A guard cannot supply its own truth as a premise for an obligation it must discharge in order to compute that very truth value. This is the headline asymmetry the whole cell group demonstrates; it holds identically across all five evaluation-site categories and both lanes.
- Argument constraints (b) would be available in principle -- the triggering event's args are in scope at a transition row's guard -- but this cell's guard reads only field-typed operands (Total, Parts are both fields, not event args), so there is no event-arg premise to instantiate here. The applicable-class set is a per-cell fact derived from what the guard actually reads (matrix line 74), not a ceiling on what this evaluation-site category could support with a different divisor.
- Pre-state constraints (d) are available: the guard is evaluated against the configuration in place immediately before the row's own actions would run -- a definite pre-state, exactly as for the row's actions themselves (the rule-anchored write-site case shape, matrix line 149; the Inductive hypothesis plus sign monotonicity argument, matrix line 208, grounds a rule holding in the pre-state as usable wherever a definite single pre-state exists, and a transition row's guard shares the row's one pre-state).
- Not answered here, carried as an open dependency: whether an EARLIER conjunct of this same guard would be a premise for a fault arising in a LATER conjunct (e.g. `when Parts != 0 and Total / Parts > 1`) is not settled by anything the matrix currently states. This is a different question from class (c)'s exclusion above (which concerns the guard supplying a premise to an obligation arising within itself, full stop) -- it asks whether the guard's own internal conjunct ordering creates a second, narrower notion of "before" that class (c)'s exclusion does not reach. This cell does not attempt that spelling and states no answer for it.
- Measured live (Precept.MatrixTools, 2026-07-21, commit e1a14d91) on the base program below and on every discharge and near-miss addition in this cell: all compile with zero diagnostics, and the tool reports no obligation for this division (transition-row-guard mints nothing at HEAD in either lane). This is a direct, self-contained instance of the matrix's own "open design hole -- nothing detects an obligation that was never minted" (matrix lines 88-101): the compiler visits this site, computes no fault obligation for it, and nothing anywhere flags the absence.
- operationKind is recorded as the catalog's declaring-entry name ("IntegerDivideInteger", src/Precept/Language/Operations.cs; ProofRequirementKind.Numeric, src/Precept/Language/ProofRequirementKind.cs) rather than the coarser RequirementKind ("Numeric", shared by both lanes) -- the coarser value could not distinguish the integer and decimal lane cells as separate coordinates. Recorded so the choice is checkable, not asserted silently.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: the four premise classes
- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: the Fault family case-shape row
- docs/Working/obligation-discharge-matrix-2026-07-19.md:186 — matrix: Fault minting: catalog-stamped at evaluation sites

## g03/transition-row-guard-dec — Transition-row guard -- decimal lane

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:186 — matrix: Fault minting: catalog-stamped at evaluation sites
- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: the Fault family case-shape row

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | DecimalDivideDecimal |
| evaluation site category | transition-row-guard |
| type family | primitive |

### What must be proven

Obligation: Parts != 0

Weakest precondition: Parts != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Total | the field read as the guard's dividend -- immaterial to the safety precondition itself |
| Parts | the field read as the guard's divisor |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:42 — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a), (d).

Derived per evaluation-site category, not asserted: this is a transition row's `when` (`from S on E when G -> action`). Classes (a) and (d) instantiate; (b) does not (see notes); (c) never does (structural, see notes).

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: field modifier on the divisor field, excluding zero -> interval containment over the modifier's declared bound
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor field's interval from its own declared modifiers (a finite set -- the modifier-to-ProofSatisfaction table, docs/compiler/proof-engine.md § Strategy 2); a computed interval excluding zero closes the obligation. Modelled on the matrix's own Family 4 sketch decision procedure (matrix line 299: "compute the divisor's interval from its declared modifiers and in-scope guard facts"), generalized here from premise class (b) (an event-arg modifier) to premise class (a) (a field modifier) on the same divisor. No excluding bound rejects, naming class (a).

- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: the four premise classes, including (a) field modifiers
- docs/Working/obligation-discharge-matrix-2026-07-19.md:206 — matrix: Arg-bound interval arithmetic -- the modifier-to-ProofSatisfaction interval derivation this entry extends from arg modifiers to field modifiers
- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 sketch -- "compute the divisor's interval from its declared modifiers and in-scope guard facts"

**Entry 2 — (d)**

- Derivation: a rule established and preserved over the divisor field, consumed as the inductive hypothesis -> the divisor's pre-state fact closes the obligation directly (CompositionalConstraint)
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Look up a nonzero fact for the divisor field among the constraints holding in the pre-state (the CompositionalConstraint strategy already exercised at HEAD for this exact shape -- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3, "rule PlannedQuantity > 0 ... discharges a division-by-zero obligation via the CompositionalConstraint strategy"). No such fact rejects, naming class (d).

- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: (d) all constraints holding in the pre-state
- docs/Working/obligation-discharge-matrix-2026-07-19.md:208 — matrix: Inductive hypothesis plus sign monotonicity -- extended here from its written sign-monotonicity/subtraction shape to a bare nonzero fact read directly
- docs/Working/obligation-discharge-matrix-2026-07-19.md:94 — matrix: the Open design hole section -- the matrix's own worked example of a rule consumed as premise (d) for a divisor-nonzero fault obligation
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — review: Defect B: a rule consumed as premise (d) while nothing establishes it (fail-open)

### What the failing diagnostic must suggest

- For class (a): declare a field modifier on <WP>'s divisor that excludes zero (e.g. positive, nonzero, or a min bound above zero)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md:46 — matrix: the suggestion-schema bullet

- For class (d): add a rule establishing and preserving <WP> over the divisor field, available as premise (d)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md:46 — matrix: the suggestion-schema bullet

  - A discharge accepted through this suggestion inherits the false-proof hazard: record its builtStatus as unresolved-today and its provenance as model-derived, never as confirmed by a clean compile, until the rule's own establishment/preservation obligations are built (docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3, Defect A and Defect B).


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept TrgBaseDec
field Total as decimal default 10.0
field Parts as decimal default 0.0
state Active initial
state Closed terminal
event Open initial
event Check
event Close
on Open
    -> set Total = 10.0
    -> set Parts = 0.0
from Active on Check when Total / Parts > 1.0
    -> no transition
from Active on Close
    -> transition Closed
```

Required outcome: reject, naming the missing premise classes (a), (d), with no other diagnostics

Provenance: model-derived.

**Discharge additions — what makes the base compile.**

*transition-row-guard-dec-a* — other

- Addition: field Parts as decimal default 1.0 nonzero
- Premise classes: (a)
- Derivation: field modifier excluding zero on the divisor -> interval containment (class a)
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Live-verified (Precept.MatrixTools, 2026-07-21, commit e1a14d91): this addition compiles with zero diagnostics and no obligation is reported for the division -- the same no-mint behaviour as the base. builtStatus is unresolved-today because the site does not mint at HEAD at all; a clean compile here records the site's failure to mint, not confirmation that this discharge is accepted. This discharge is a field modifier (class a), which is NOT subject to the false-proof hazard (task authoring notes: "guard-based and modifier-based discharges ... are unaffected").)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Replaces the base's `field Parts as decimal default 0.0` declaration (and the matching construction assignment) with `field Parts as decimal default 1.0 nonzero`. The compiled program is the base program with that one field declaration replaced; it is not stored verbatim anywhere, and no `application` record reconstructs it mechanically (see the schema-vocabulary note below).
- kind is recorded as "other": the closed dischargeWitness.kind vocabulary has "arg-modifier" for an event-arg modifier but no corresponding value for a FIELD modifier, even though class (a) field modifiers is named in the Vocabulary (matrix line 38) as a first-class premise source. Flagged as a vocabulary gap.
- The schema's `application` discriminated union (row-guard / event-arg-declarations) has no locus for a field-declaration modifier addition or a whole new `rule` declaration -- neither of this discharge's addition shapes is either of the two loci the schema expresses. `application` is omitted rather than forced into an ill-fitting locus; the day-one validator's cell-to-test conversion cannot yet mechanically apply this addition. Flagged as a schema-vocabulary gap, not resolved here.

*transition-row-guard-dec-d* — other

- Addition: rule Parts != 0.0 because "Parts must stay nonzero"
- Premise classes: (d)
- Derivation: rule establishing the divisor's nonzero-ness, consumed as premise (d) via CompositionalConstraint
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Doubly unverifiable, so no live run is cited as confirmation here even though compiling this exact program shows the same no-mint behaviour as every other witness in this file: (i) this evaluation-site category mints no fault obligation at HEAD at all (measured on the base and on this addition); (ii) independent of (i), a `rule` consumed as premise (d) is exactly the false-proof-prone discharge path verified at HEAD (docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3, Defect B -- a rule can be accepted as a divisor-nonzero premise via the CompositionalConstraint strategy while nothing establishes or preserves it). A clean compile could never be read as confirmation of this discharge even if the site did mint. modelStatus is stated as provable-under-model on the strength of premise (d)'s general validity argument (matrix line 208), extended from its written sign-monotonicity/subtraction shape to a bare nonzero fact read directly -- an extension the matrix does not explicitly write out (see missing rules).)

Required outcome: accept, with the premise list recorded

Provenance: model-derived.

- Adds `rule Parts != 0.0 because "Parts must stay nonzero"` as a new top-level declaration and marks Total and Parts `editable` (parity with the standalone-rule shape the false-proof hazard's own repro uses; editability is incidental to the derivation, not load-bearing for it).
- kind is recorded as "other": the closed dischargeWitness.kind vocabulary has no value for "a new rule declaration added to the file" -- the closest options (guard, arg-modifier, default-constant-fold, structural-edit) all name a different shape. Flagged as a vocabulary gap.
- The schema's `application` discriminated union (row-guard / event-arg-declarations) has no locus for a field-declaration modifier addition or a whole new `rule` declaration -- neither of this discharge's addition shapes is either of the two loci the schema expresses. `application` is omitted rather than forced into an ill-fitting locus; the day-one validator's cell-to-test conversion cannot yet mechanically apply this addition. Flagged as a schema-vocabulary gap, not resolved here.
- Listed in this task's unverifiableWitnesses output: false-proof hazard (rule used as premise (d)), per docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3, Defect B, compounded by this site's no-mint status at HEAD.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| transition-row-guard-dec-a | field Parts as decimal default 0.0 nonnegative | `nonnegative` (unlike `nonzero`/`positive`) does not exclude 0.0: the divisor's declared interval still contains zero, so the interval-containment derivation does not close -- must still reject, same obligation. | reject, naming the same obligation |
| transition-row-guard-dec-d | rule Parts >= 0.0 because "Parts must stay nonnegative" | `rule Parts >= 0.0` (unlike `!=`) does not exclude zero: the pre-state fact still admits a zero divisor, so the inductive-hypothesis derivation does not close -- must still reject, same obligation. | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Class (c) -- the handler's guard -- is structurally unavailable and is excluded from every contract entry in this file. The obligation arises from evaluating the guard expression's own division, before the guard has produced the truth value anything downstream (a row's actions, an ensure, a rule, an access-mode decision) could rely on. Class (c) is defined as a fact the guard's truth supplies to obligations arising after it (matrix line 38: "(c) the handler's guard"; the Guard normal-form match argument, matrix line 204, grounds (c) precisely in a row's actions executing only after the guard evaluated true -- a precondition the guard's own internal evaluation cannot satisfy about itself). A guard cannot supply its own truth as a premise for an obligation it must discharge in order to compute that very truth value. This is the headline asymmetry the whole cell group demonstrates; it holds identically across all five evaluation-site categories and both lanes.
- Argument constraints (b) would be available in principle -- the triggering event's args are in scope at a transition row's guard -- but this cell's guard reads only field-typed operands (Total, Parts are both fields, not event args), so there is no event-arg premise to instantiate here. The applicable-class set is a per-cell fact derived from what the guard actually reads (matrix line 74), not a ceiling on what this evaluation-site category could support with a different divisor.
- Pre-state constraints (d) are available: the guard is evaluated against the configuration in place immediately before the row's own actions would run -- a definite pre-state, exactly as for the row's actions themselves (the rule-anchored write-site case shape, matrix line 149; the Inductive hypothesis plus sign monotonicity argument, matrix line 208, grounds a rule holding in the pre-state as usable wherever a definite single pre-state exists, and a transition row's guard shares the row's one pre-state).
- Not answered here, carried as an open dependency: whether an EARLIER conjunct of this same guard would be a premise for a fault arising in a LATER conjunct (e.g. `when Parts != 0 and Total / Parts > 1`) is not settled by anything the matrix currently states. This is a different question from class (c)'s exclusion above (which concerns the guard supplying a premise to an obligation arising within itself, full stop) -- it asks whether the guard's own internal conjunct ordering creates a second, narrower notion of "before" that class (c)'s exclusion does not reach. This cell does not attempt that spelling and states no answer for it.
- Measured live (Precept.MatrixTools, 2026-07-21, commit e1a14d91) on the base program below and on every discharge and near-miss addition in this cell: all compile with zero diagnostics, and the tool reports no obligation for this division (transition-row-guard mints nothing at HEAD in either lane). This is a direct, self-contained instance of the matrix's own "open design hole -- nothing detects an obligation that was never minted" (matrix lines 88-101): the compiler visits this site, computes no fault obligation for it, and nothing anywhere flags the absence.
- operationKind is recorded as the catalog's declaring-entry name ("DecimalDivideDecimal", src/Precept/Language/Operations.cs; ProofRequirementKind.Numeric, src/Precept/Language/ProofRequirementKind.cs) rather than the coarser RequirementKind ("Numeric", shared by both lanes) -- the coarser value could not distinguish the integer and decimal lane cells as separate coordinates. Recorded so the choice is checkable, not asserted silently.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: the four premise classes
- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: the Fault family case-shape row
- docs/Working/obligation-discharge-matrix-2026-07-19.md:186 — matrix: Fault minting: catalog-stamped at evaluation sites

## g03/state-hook-guard-int — State entry/exit hook guard -- integer lane

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:186 — matrix: Fault minting: catalog-stamped at evaluation sites
- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: the Fault family case-shape row

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | IntegerDivideInteger |
| evaluation site category | state-hook-guard |
| type family | primitive |

### What must be proven

Obligation: Parts != 0

Weakest precondition: Parts != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Total | the field read as the guard's dividend -- immaterial to the safety precondition itself |
| Parts | the field read as the guard's divisor |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:42 — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a), (d).

Derived per evaluation-site category, not asserted: this is a state entry/exit hook's own pre-verb `when` (`to S when G -> action` / `from S when G -> action`). Classes (a) and (d) instantiate; (b) does not (see notes); (c) never does (structural, see notes).

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: field modifier on the divisor field, excluding zero -> interval containment over the modifier's declared bound
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor field's interval from its own declared modifiers (a finite set -- the modifier-to-ProofSatisfaction table, docs/compiler/proof-engine.md § Strategy 2); a computed interval excluding zero closes the obligation. Modelled on the matrix's own Family 4 sketch decision procedure (matrix line 299: "compute the divisor's interval from its declared modifiers and in-scope guard facts"), generalized here from premise class (b) (an event-arg modifier) to premise class (a) (a field modifier) on the same divisor. No excluding bound rejects, naming class (a).

- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: the four premise classes, including (a) field modifiers
- docs/Working/obligation-discharge-matrix-2026-07-19.md:206 — matrix: Arg-bound interval arithmetic -- the modifier-to-ProofSatisfaction interval derivation this entry extends from arg modifiers to field modifiers
- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 sketch -- "compute the divisor's interval from its declared modifiers and in-scope guard facts"

**Entry 2 — (d)**

- Derivation: a rule established and preserved over the divisor field, consumed as the inductive hypothesis -> the divisor's pre-state fact closes the obligation directly (CompositionalConstraint)
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Look up a nonzero fact for the divisor field among the constraints holding in the pre-state (the CompositionalConstraint strategy already exercised at HEAD for this exact shape -- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3, "rule PlannedQuantity > 0 ... discharges a division-by-zero obligation via the CompositionalConstraint strategy"). No such fact rejects, naming class (d).

- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: (d) all constraints holding in the pre-state
- docs/Working/obligation-discharge-matrix-2026-07-19.md:208 — matrix: Inductive hypothesis plus sign monotonicity -- extended here from its written sign-monotonicity/subtraction shape to a bare nonzero fact read directly
- docs/Working/obligation-discharge-matrix-2026-07-19.md:94 — matrix: the Open design hole section -- the matrix's own worked example of a rule consumed as premise (d) for a divisor-nonzero fault obligation
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — review: Defect B: a rule consumed as premise (d) while nothing establishes it (fail-open)

### What the failing diagnostic must suggest

- For class (a): declare a field modifier on <WP>'s divisor that excludes zero (e.g. positive, nonzero, or a min bound above zero)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md:46 — matrix: the suggestion-schema bullet

- For class (d): add a rule establishing and preserving <WP> over the divisor field, available as premise (d)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md:46 — matrix: the suggestion-schema bullet

  - A discharge accepted through this suggestion inherits the false-proof hazard: record its builtStatus as unresolved-today and its provenance as model-derived, never as confirmed by a clean compile, until the rule's own establishment/preservation obligations are built (docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3, Defect A and Defect B).


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept ShgBaseInt
field Total as integer default 10
field Parts as integer default 0
field Note as integer default 0
state Draft initial
state Done terminal
event Open initial
event Finish
on Open
    -> set Total = 10
    -> set Parts = 0
to Done when Total / Parts > 1 -> set Note = 1
from Draft on Finish
    -> transition Done
```

Required outcome: reject, naming the missing premise classes (a), (d), with no other diagnostics

Provenance: model-derived.

**Discharge additions — what makes the base compile.**

*state-hook-guard-int-a* — other

- Addition: field Parts as integer default 1 nonzero
- Premise classes: (a)
- Derivation: field modifier excluding zero on the divisor -> interval containment (class a)
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Live-verified (Precept.MatrixTools, 2026-07-21, commit e1a14d91): this addition compiles with zero diagnostics and no obligation is reported for the division -- the same no-mint behaviour as the base. builtStatus is unresolved-today because the site does not mint at HEAD at all; a clean compile here records the site's failure to mint, not confirmation that this discharge is accepted. This discharge is a field modifier (class a), which is NOT subject to the false-proof hazard (task authoring notes: "guard-based and modifier-based discharges ... are unaffected").)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Replaces the base's `field Parts as integer default 0` declaration (and the matching construction assignment) with `field Parts as integer default 1 nonzero`. The compiled program is the base program with that one field declaration replaced; it is not stored verbatim anywhere, and no `application` record reconstructs it mechanically (see the schema-vocabulary note below).
- kind is recorded as "other": the closed dischargeWitness.kind vocabulary has "arg-modifier" for an event-arg modifier but no corresponding value for a FIELD modifier, even though class (a) field modifiers is named in the Vocabulary (matrix line 38) as a first-class premise source. Flagged as a vocabulary gap.
- The schema's `application` discriminated union (row-guard / event-arg-declarations) has no locus for a field-declaration modifier addition or a whole new `rule` declaration -- neither of this discharge's addition shapes is either of the two loci the schema expresses. `application` is omitted rather than forced into an ill-fitting locus; the day-one validator's cell-to-test conversion cannot yet mechanically apply this addition. Flagged as a schema-vocabulary gap, not resolved here.

*state-hook-guard-int-d* — other

- Addition: rule Parts != 0 because "Parts must stay nonzero"
- Premise classes: (d)
- Derivation: rule establishing the divisor's nonzero-ness, consumed as premise (d) via CompositionalConstraint
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Doubly unverifiable, so no live run is cited as confirmation here even though compiling this exact program shows the same no-mint behaviour as every other witness in this file: (i) this evaluation-site category mints no fault obligation at HEAD at all (measured on the base and on this addition); (ii) independent of (i), a `rule` consumed as premise (d) is exactly the false-proof-prone discharge path verified at HEAD (docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3, Defect B -- a rule can be accepted as a divisor-nonzero premise via the CompositionalConstraint strategy while nothing establishes or preserves it). A clean compile could never be read as confirmation of this discharge even if the site did mint. modelStatus is stated as provable-under-model on the strength of premise (d)'s general validity argument (matrix line 208), extended from its written sign-monotonicity/subtraction shape to a bare nonzero fact read directly -- an extension the matrix does not explicitly write out (see missing rules).)

Required outcome: accept, with the premise list recorded

Provenance: model-derived.

- Adds `rule Parts != 0 because "Parts must stay nonzero"` as a new top-level declaration and marks Total and Parts `editable` (parity with the standalone-rule shape the false-proof hazard's own repro uses; editability is incidental to the derivation, not load-bearing for it).
- kind is recorded as "other": the closed dischargeWitness.kind vocabulary has no value for "a new rule declaration added to the file" -- the closest options (guard, arg-modifier, default-constant-fold, structural-edit) all name a different shape. Flagged as a vocabulary gap.
- The schema's `application` discriminated union (row-guard / event-arg-declarations) has no locus for a field-declaration modifier addition or a whole new `rule` declaration -- neither of this discharge's addition shapes is either of the two loci the schema expresses. `application` is omitted rather than forced into an ill-fitting locus; the day-one validator's cell-to-test conversion cannot yet mechanically apply this addition. Flagged as a schema-vocabulary gap, not resolved here.
- Listed in this task's unverifiableWitnesses output: false-proof hazard (rule used as premise (d)), per docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3, Defect B, compounded by this site's no-mint status at HEAD.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| state-hook-guard-int-a | field Parts as integer default 0 nonnegative | `nonnegative` (unlike `nonzero`/`positive`) does not exclude 0: the divisor's declared interval still contains zero, so the interval-containment derivation does not close -- must still reject, same obligation. | reject, naming the same obligation |
| state-hook-guard-int-d | rule Parts >= 0 because "Parts must stay nonnegative" | `rule Parts >= 0` (unlike `!=`) does not exclude zero: the pre-state fact still admits a zero divisor, so the inductive-hypothesis derivation does not close -- must still reject, same obligation. | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Class (c) -- the handler's guard -- is structurally unavailable and is excluded from every contract entry in this file. The obligation arises from evaluating the guard expression's own division, before the guard has produced the truth value anything downstream (a row's actions, an ensure, a rule, an access-mode decision) could rely on. Class (c) is defined as a fact the guard's truth supplies to obligations arising after it (matrix line 38: "(c) the handler's guard"; the Guard normal-form match argument, matrix line 204, grounds (c) precisely in a row's actions executing only after the guard evaluated true -- a precondition the guard's own internal evaluation cannot satisfy about itself). A guard cannot supply its own truth as a premise for an obligation it must discharge in order to compute that very truth value. This is the headline asymmetry the whole cell group demonstrates; it holds identically across all five evaluation-site categories and both lanes.
- Argument constraints (b) are structurally unavailable at this evaluation-site category regardless of what the guard reads: no triggering event is in scope here (a state hook fires on every inbound/outbound edge and an access-mode declaration carries no event at all), so there is no event-arg premise vocabulary to draw on.
- Pre-state constraints (d) are available: a state entry/exit hook's own guard scopes a discrete transition-moment obligation with a definite pre-state -- the configuration immediately before the transition. The StateEntry/StateExit case-shape rows (matrix lines 151-152) describe the analogous ensure-anchored transition-moment obligation as drawing on "the entering row's guard, args, and pre-state"; the hook's own `when` is the same transition-moment evaluation occasion.
- Not answered here, carried as an open dependency: whether an EARLIER conjunct of this same guard would be a premise for a fault arising in a LATER conjunct (e.g. `when Parts != 0 and Total / Parts > 1`) is not settled by anything the matrix currently states. This is a different question from class (c)'s exclusion above (which concerns the guard supplying a premise to an obligation arising within itself, full stop) -- it asks whether the guard's own internal conjunct ordering creates a second, narrower notion of "before" that class (c)'s exclusion does not reach. This cell does not attempt that spelling and states no answer for it.
- Measured live (Precept.MatrixTools, 2026-07-21, commit e1a14d91) on the base program below and on every discharge and near-miss addition in this cell: all compile with zero diagnostics, and the tool reports no obligation for this division (state-hook-guard mints nothing at HEAD in either lane). This is a direct, self-contained instance of the matrix's own "open design hole -- nothing detects an obligation that was never minted" (matrix lines 88-101): the compiler visits this site, computes no fault obligation for it, and nothing anywhere flags the absence.
- operationKind is recorded as the catalog's declaring-entry name ("IntegerDivideInteger", src/Precept/Language/Operations.cs; ProofRequirementKind.Numeric, src/Precept/Language/ProofRequirementKind.cs) rather than the coarser RequirementKind ("Numeric", shared by both lanes) -- the coarser value could not distinguish the integer and decimal lane cells as separate coordinates. Recorded so the choice is checkable, not asserted silently.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: the four premise classes
- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: the Fault family case-shape row
- docs/Working/obligation-discharge-matrix-2026-07-19.md:186 — matrix: Fault minting: catalog-stamped at evaluation sites

## g03/state-hook-guard-dec — State entry/exit hook guard -- decimal lane

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:186 — matrix: Fault minting: catalog-stamped at evaluation sites
- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: the Fault family case-shape row

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | DecimalDivideDecimal |
| evaluation site category | state-hook-guard |
| type family | primitive |

### What must be proven

Obligation: Parts != 0

Weakest precondition: Parts != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Total | the field read as the guard's dividend -- immaterial to the safety precondition itself |
| Parts | the field read as the guard's divisor |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:42 — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a), (d).

Derived per evaluation-site category, not asserted: this is a state entry/exit hook's own pre-verb `when` (`to S when G -> action` / `from S when G -> action`). Classes (a) and (d) instantiate; (b) does not (see notes); (c) never does (structural, see notes).

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: field modifier on the divisor field, excluding zero -> interval containment over the modifier's declared bound
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor field's interval from its own declared modifiers (a finite set -- the modifier-to-ProofSatisfaction table, docs/compiler/proof-engine.md § Strategy 2); a computed interval excluding zero closes the obligation. Modelled on the matrix's own Family 4 sketch decision procedure (matrix line 299: "compute the divisor's interval from its declared modifiers and in-scope guard facts"), generalized here from premise class (b) (an event-arg modifier) to premise class (a) (a field modifier) on the same divisor. No excluding bound rejects, naming class (a).

- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: the four premise classes, including (a) field modifiers
- docs/Working/obligation-discharge-matrix-2026-07-19.md:206 — matrix: Arg-bound interval arithmetic -- the modifier-to-ProofSatisfaction interval derivation this entry extends from arg modifiers to field modifiers
- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 sketch -- "compute the divisor's interval from its declared modifiers and in-scope guard facts"

**Entry 2 — (d)**

- Derivation: a rule established and preserved over the divisor field, consumed as the inductive hypothesis -> the divisor's pre-state fact closes the obligation directly (CompositionalConstraint)
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Look up a nonzero fact for the divisor field among the constraints holding in the pre-state (the CompositionalConstraint strategy already exercised at HEAD for this exact shape -- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3, "rule PlannedQuantity > 0 ... discharges a division-by-zero obligation via the CompositionalConstraint strategy"). No such fact rejects, naming class (d).

- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: (d) all constraints holding in the pre-state
- docs/Working/obligation-discharge-matrix-2026-07-19.md:208 — matrix: Inductive hypothesis plus sign monotonicity -- extended here from its written sign-monotonicity/subtraction shape to a bare nonzero fact read directly
- docs/Working/obligation-discharge-matrix-2026-07-19.md:94 — matrix: the Open design hole section -- the matrix's own worked example of a rule consumed as premise (d) for a divisor-nonzero fault obligation
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — review: Defect B: a rule consumed as premise (d) while nothing establishes it (fail-open)

### What the failing diagnostic must suggest

- For class (a): declare a field modifier on <WP>'s divisor that excludes zero (e.g. positive, nonzero, or a min bound above zero)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md:46 — matrix: the suggestion-schema bullet

- For class (d): add a rule establishing and preserving <WP> over the divisor field, available as premise (d)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md:46 — matrix: the suggestion-schema bullet

  - A discharge accepted through this suggestion inherits the false-proof hazard: record its builtStatus as unresolved-today and its provenance as model-derived, never as confirmed by a clean compile, until the rule's own establishment/preservation obligations are built (docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3, Defect A and Defect B).


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept ShgBaseDec
field Total as decimal default 10.0
field Parts as decimal default 0.0
field Note as decimal default 0.0
state Draft initial
state Done terminal
event Open initial
event Finish
on Open
    -> set Total = 10.0
    -> set Parts = 0.0
to Done when Total / Parts > 1.0 -> set Note = 1.0
from Draft on Finish
    -> transition Done
```

Required outcome: reject, naming the missing premise classes (a), (d), with no other diagnostics

Provenance: model-derived.

**Discharge additions — what makes the base compile.**

*state-hook-guard-dec-a* — other

- Addition: field Parts as decimal default 1.0 nonzero
- Premise classes: (a)
- Derivation: field modifier excluding zero on the divisor -> interval containment (class a)
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Live-verified (Precept.MatrixTools, 2026-07-21, commit e1a14d91): this addition compiles with zero diagnostics and no obligation is reported for the division -- the same no-mint behaviour as the base. builtStatus is unresolved-today because the site does not mint at HEAD at all; a clean compile here records the site's failure to mint, not confirmation that this discharge is accepted. This discharge is a field modifier (class a), which is NOT subject to the false-proof hazard (task authoring notes: "guard-based and modifier-based discharges ... are unaffected").)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Replaces the base's `field Parts as decimal default 0.0` declaration (and the matching construction assignment) with `field Parts as decimal default 1.0 nonzero`. The compiled program is the base program with that one field declaration replaced; it is not stored verbatim anywhere, and no `application` record reconstructs it mechanically (see the schema-vocabulary note below).
- kind is recorded as "other": the closed dischargeWitness.kind vocabulary has "arg-modifier" for an event-arg modifier but no corresponding value for a FIELD modifier, even though class (a) field modifiers is named in the Vocabulary (matrix line 38) as a first-class premise source. Flagged as a vocabulary gap.
- The schema's `application` discriminated union (row-guard / event-arg-declarations) has no locus for a field-declaration modifier addition or a whole new `rule` declaration -- neither of this discharge's addition shapes is either of the two loci the schema expresses. `application` is omitted rather than forced into an ill-fitting locus; the day-one validator's cell-to-test conversion cannot yet mechanically apply this addition. Flagged as a schema-vocabulary gap, not resolved here.

*state-hook-guard-dec-d* — other

- Addition: rule Parts != 0.0 because "Parts must stay nonzero"
- Premise classes: (d)
- Derivation: rule establishing the divisor's nonzero-ness, consumed as premise (d) via CompositionalConstraint
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Doubly unverifiable, so no live run is cited as confirmation here even though compiling this exact program shows the same no-mint behaviour as every other witness in this file: (i) this evaluation-site category mints no fault obligation at HEAD at all (measured on the base and on this addition); (ii) independent of (i), a `rule` consumed as premise (d) is exactly the false-proof-prone discharge path verified at HEAD (docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3, Defect B -- a rule can be accepted as a divisor-nonzero premise via the CompositionalConstraint strategy while nothing establishes or preserves it). A clean compile could never be read as confirmation of this discharge even if the site did mint. modelStatus is stated as provable-under-model on the strength of premise (d)'s general validity argument (matrix line 208), extended from its written sign-monotonicity/subtraction shape to a bare nonzero fact read directly -- an extension the matrix does not explicitly write out (see missing rules).)

Required outcome: accept, with the premise list recorded

Provenance: model-derived.

- Adds `rule Parts != 0.0 because "Parts must stay nonzero"` as a new top-level declaration and marks Total and Parts `editable` (parity with the standalone-rule shape the false-proof hazard's own repro uses; editability is incidental to the derivation, not load-bearing for it).
- kind is recorded as "other": the closed dischargeWitness.kind vocabulary has no value for "a new rule declaration added to the file" -- the closest options (guard, arg-modifier, default-constant-fold, structural-edit) all name a different shape. Flagged as a vocabulary gap.
- The schema's `application` discriminated union (row-guard / event-arg-declarations) has no locus for a field-declaration modifier addition or a whole new `rule` declaration -- neither of this discharge's addition shapes is either of the two loci the schema expresses. `application` is omitted rather than forced into an ill-fitting locus; the day-one validator's cell-to-test conversion cannot yet mechanically apply this addition. Flagged as a schema-vocabulary gap, not resolved here.
- Listed in this task's unverifiableWitnesses output: false-proof hazard (rule used as premise (d)), per docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3, Defect B, compounded by this site's no-mint status at HEAD.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| state-hook-guard-dec-a | field Parts as decimal default 0.0 nonnegative | `nonnegative` (unlike `nonzero`/`positive`) does not exclude 0.0: the divisor's declared interval still contains zero, so the interval-containment derivation does not close -- must still reject, same obligation. | reject, naming the same obligation |
| state-hook-guard-dec-d | rule Parts >= 0.0 because "Parts must stay nonnegative" | `rule Parts >= 0.0` (unlike `!=`) does not exclude zero: the pre-state fact still admits a zero divisor, so the inductive-hypothesis derivation does not close -- must still reject, same obligation. | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Class (c) -- the handler's guard -- is structurally unavailable and is excluded from every contract entry in this file. The obligation arises from evaluating the guard expression's own division, before the guard has produced the truth value anything downstream (a row's actions, an ensure, a rule, an access-mode decision) could rely on. Class (c) is defined as a fact the guard's truth supplies to obligations arising after it (matrix line 38: "(c) the handler's guard"; the Guard normal-form match argument, matrix line 204, grounds (c) precisely in a row's actions executing only after the guard evaluated true -- a precondition the guard's own internal evaluation cannot satisfy about itself). A guard cannot supply its own truth as a premise for an obligation it must discharge in order to compute that very truth value. This is the headline asymmetry the whole cell group demonstrates; it holds identically across all five evaluation-site categories and both lanes.
- Argument constraints (b) are structurally unavailable at this evaluation-site category regardless of what the guard reads: no triggering event is in scope here (a state hook fires on every inbound/outbound edge and an access-mode declaration carries no event at all), so there is no event-arg premise vocabulary to draw on.
- Pre-state constraints (d) are available: a state entry/exit hook's own guard scopes a discrete transition-moment obligation with a definite pre-state -- the configuration immediately before the transition. The StateEntry/StateExit case-shape rows (matrix lines 151-152) describe the analogous ensure-anchored transition-moment obligation as drawing on "the entering row's guard, args, and pre-state"; the hook's own `when` is the same transition-moment evaluation occasion.
- Not answered here, carried as an open dependency: whether an EARLIER conjunct of this same guard would be a premise for a fault arising in a LATER conjunct (e.g. `when Parts != 0 and Total / Parts > 1`) is not settled by anything the matrix currently states. This is a different question from class (c)'s exclusion above (which concerns the guard supplying a premise to an obligation arising within itself, full stop) -- it asks whether the guard's own internal conjunct ordering creates a second, narrower notion of "before" that class (c)'s exclusion does not reach. This cell does not attempt that spelling and states no answer for it.
- Measured live (Precept.MatrixTools, 2026-07-21, commit e1a14d91) on the base program below and on every discharge and near-miss addition in this cell: all compile with zero diagnostics, and the tool reports no obligation for this division (state-hook-guard mints nothing at HEAD in either lane). This is a direct, self-contained instance of the matrix's own "open design hole -- nothing detects an obligation that was never minted" (matrix lines 88-101): the compiler visits this site, computes no fault obligation for it, and nothing anywhere flags the absence.
- operationKind is recorded as the catalog's declaring-entry name ("DecimalDivideDecimal", src/Precept/Language/Operations.cs; ProofRequirementKind.Numeric, src/Precept/Language/ProofRequirementKind.cs) rather than the coarser RequirementKind ("Numeric", shared by both lanes) -- the coarser value could not distinguish the integer and decimal lane cells as separate coordinates. Recorded so the choice is checkable, not asserted silently.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: the four premise classes
- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: the Fault family case-shape row
- docs/Working/obligation-discharge-matrix-2026-07-19.md:186 — matrix: Fault minting: catalog-stamped at evaluation sites

## g03/access-mode-guard-int — Access-mode declaration guard -- integer lane

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:186 — matrix: Fault minting: catalog-stamped at evaluation sites
- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: the Fault family case-shape row

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | IntegerDivideInteger |
| evaluation site category | access-mode-guard |
| type family | primitive |

### What must be proven

Obligation: Parts != 0

Weakest precondition: Parts != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Total | the field read as the guard's dividend -- immaterial to the safety precondition itself |
| Parts | the field read as the guard's divisor |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:42 — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a), (d).

Derived per evaluation-site category, not asserted: this is a state-scoped access-mode declaration's `when` (`in S when G modify F readonly\|editable`). Classes (a) and (d) instantiate; (b) does not (see notes); (c) never does (structural, see notes).

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: field modifier on the divisor field, excluding zero -> interval containment over the modifier's declared bound
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor field's interval from its own declared modifiers (a finite set -- the modifier-to-ProofSatisfaction table, docs/compiler/proof-engine.md § Strategy 2); a computed interval excluding zero closes the obligation. Modelled on the matrix's own Family 4 sketch decision procedure (matrix line 299: "compute the divisor's interval from its declared modifiers and in-scope guard facts"), generalized here from premise class (b) (an event-arg modifier) to premise class (a) (a field modifier) on the same divisor. No excluding bound rejects, naming class (a).

- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: the four premise classes, including (a) field modifiers
- docs/Working/obligation-discharge-matrix-2026-07-19.md:206 — matrix: Arg-bound interval arithmetic -- the modifier-to-ProofSatisfaction interval derivation this entry extends from arg modifiers to field modifiers
- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 sketch -- "compute the divisor's interval from its declared modifiers and in-scope guard facts"

**Entry 2 — (d)**

- Derivation: a rule established and preserved over the divisor field, consumed as the inductive hypothesis -> the divisor's pre-state fact closes the obligation directly (CompositionalConstraint)
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Look up a nonzero fact for the divisor field among the constraints holding in the pre-state (the CompositionalConstraint strategy already exercised at HEAD for this exact shape -- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3, "rule PlannedQuantity > 0 ... discharges a division-by-zero obligation via the CompositionalConstraint strategy"). No such fact rejects, naming class (d).

- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: (d) all constraints holding in the pre-state
- docs/Working/obligation-discharge-matrix-2026-07-19.md:208 — matrix: Inductive hypothesis plus sign monotonicity -- extended here from its written sign-monotonicity/subtraction shape to a bare nonzero fact read directly
- docs/Working/obligation-discharge-matrix-2026-07-19.md:94 — matrix: the Open design hole section -- the matrix's own worked example of a rule consumed as premise (d) for a divisor-nonzero fault obligation
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — review: Defect B: a rule consumed as premise (d) while nothing establishes it (fail-open)

### What the failing diagnostic must suggest

- For class (a): declare a field modifier on <WP>'s divisor that excludes zero (e.g. positive, nonzero, or a min bound above zero)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md:46 — matrix: the suggestion-schema bullet

- For class (d): add a rule establishing and preserving <WP> over the divisor field, available as premise (d)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md:46 — matrix: the suggestion-schema bullet

  - A discharge accepted through this suggestion inherits the false-proof hazard: record its builtStatus as unresolved-today and its provenance as model-derived, never as confirmed by a clean compile, until the rule's own establishment/preservation obligations are built (docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3, Defect A and Defect B).


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept AmgBaseInt
field Total as integer default 10
field Parts as integer default 0
field Note as string optional editable
state Draft initial
state Done terminal
event Open initial
event Finish
on Open
    -> set Total = 10
    -> set Parts = 0
in Draft when Total / Parts > 1 modify Note readonly
from Draft on Finish
    -> transition Done
```

Required outcome: reject, naming the missing premise classes (a), (d), with no other diagnostics

Provenance: model-derived.

**Discharge additions — what makes the base compile.**

*access-mode-guard-int-a* — other

- Addition: field Parts as integer default 1 nonzero
- Premise classes: (a)
- Derivation: field modifier excluding zero on the divisor -> interval containment (class a)
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Live-verified (Precept.MatrixTools, 2026-07-21, commit e1a14d91): this addition compiles with zero diagnostics and no obligation is reported for the division -- the same no-mint behaviour as the base. builtStatus is unresolved-today because the site does not mint at HEAD at all; a clean compile here records the site's failure to mint, not confirmation that this discharge is accepted. This discharge is a field modifier (class a), which is NOT subject to the false-proof hazard (task authoring notes: "guard-based and modifier-based discharges ... are unaffected").)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Replaces the base's `field Parts as integer default 0` declaration (and the matching construction assignment) with `field Parts as integer default 1 nonzero`. The compiled program is the base program with that one field declaration replaced; it is not stored verbatim anywhere, and no `application` record reconstructs it mechanically (see the schema-vocabulary note below).
- kind is recorded as "other": the closed dischargeWitness.kind vocabulary has "arg-modifier" for an event-arg modifier but no corresponding value for a FIELD modifier, even though class (a) field modifiers is named in the Vocabulary (matrix line 38) as a first-class premise source. Flagged as a vocabulary gap.
- The schema's `application` discriminated union (row-guard / event-arg-declarations) has no locus for a field-declaration modifier addition or a whole new `rule` declaration -- neither of this discharge's addition shapes is either of the two loci the schema expresses. `application` is omitted rather than forced into an ill-fitting locus; the day-one validator's cell-to-test conversion cannot yet mechanically apply this addition. Flagged as a schema-vocabulary gap, not resolved here.

*access-mode-guard-int-d* — other

- Addition: rule Parts != 0 because "Parts must stay nonzero"
- Premise classes: (d)
- Derivation: rule establishing the divisor's nonzero-ness, consumed as premise (d) via CompositionalConstraint
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Doubly unverifiable, so no live run is cited as confirmation here even though compiling this exact program shows the same no-mint behaviour as every other witness in this file: (i) this evaluation-site category mints no fault obligation at HEAD at all (measured on the base and on this addition); (ii) independent of (i), a `rule` consumed as premise (d) is exactly the false-proof-prone discharge path verified at HEAD (docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3, Defect B -- a rule can be accepted as a divisor-nonzero premise via the CompositionalConstraint strategy while nothing establishes or preserves it). A clean compile could never be read as confirmation of this discharge even if the site did mint. modelStatus is stated as provable-under-model on the strength of premise (d)'s general validity argument (matrix line 208), extended from its written sign-monotonicity/subtraction shape to a bare nonzero fact read directly -- an extension the matrix does not explicitly write out (see missing rules).)

Required outcome: accept, with the premise list recorded

Provenance: model-derived.

- Adds `rule Parts != 0 because "Parts must stay nonzero"` as a new top-level declaration and marks Total and Parts `editable` (parity with the standalone-rule shape the false-proof hazard's own repro uses; editability is incidental to the derivation, not load-bearing for it).
- kind is recorded as "other": the closed dischargeWitness.kind vocabulary has no value for "a new rule declaration added to the file" -- the closest options (guard, arg-modifier, default-constant-fold, structural-edit) all name a different shape. Flagged as a vocabulary gap.
- The schema's `application` discriminated union (row-guard / event-arg-declarations) has no locus for a field-declaration modifier addition or a whole new `rule` declaration -- neither of this discharge's addition shapes is either of the two loci the schema expresses. `application` is omitted rather than forced into an ill-fitting locus; the day-one validator's cell-to-test conversion cannot yet mechanically apply this addition. Flagged as a schema-vocabulary gap, not resolved here.
- Listed in this task's unverifiableWitnesses output: false-proof hazard (rule used as premise (d)), per docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3, Defect B, compounded by this site's no-mint status at HEAD.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| access-mode-guard-int-a | field Parts as integer default 0 nonnegative | `nonnegative` (unlike `nonzero`/`positive`) does not exclude 0: the divisor's declared interval still contains zero, so the interval-containment derivation does not close -- must still reject, same obligation. | reject, naming the same obligation |
| access-mode-guard-int-d | rule Parts >= 0 because "Parts must stay nonnegative" | `rule Parts >= 0` (unlike `!=`) does not exclude zero: the pre-state fact still admits a zero divisor, so the inductive-hypothesis derivation does not close -- must still reject, same obligation. | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Class (c) -- the handler's guard -- is structurally unavailable and is excluded from every contract entry in this file. The obligation arises from evaluating the guard expression's own division, before the guard has produced the truth value anything downstream (a row's actions, an ensure, a rule, an access-mode decision) could rely on. Class (c) is defined as a fact the guard's truth supplies to obligations arising after it (matrix line 38: "(c) the handler's guard"; the Guard normal-form match argument, matrix line 204, grounds (c) precisely in a row's actions executing only after the guard evaluated true -- a precondition the guard's own internal evaluation cannot satisfy about itself). A guard cannot supply its own truth as a premise for an obligation it must discharge in order to compute that very truth value. This is the headline asymmetry the whole cell group demonstrates; it holds identically across all five evaluation-site categories and both lanes.
- Argument constraints (b) are structurally unavailable at this evaluation-site category regardless of what the guard reads: no triggering event is in scope here (a state hook fires on every inbound/outbound edge and an access-mode declaration carries no event at all), so there is no event-arg premise vocabulary to draw on.
- Pre-state constraints (d) are available on the following reading, offered here as this cell's own derivation rather than a matrix-stated rule: whatever configuration the entity currently occupies when the access-mode check runs is a reachable configuration the induction ranges over, so a rule established-and-preserved up to that point is a valid fact regardless of which check is reading it -- nothing in the Inductive hypothesis argument (matrix line 208) is specific to the write-site occasion. This is NOT the same question as the matrix's own explicitly open one -- "whether the residency fact itself is a premise" (matrix line 150) -- which concerns whether being-in-S is a fact, not whether an ordinary rule holding in the current configuration is. NOTE: the identical argument would appear to extend to ensure-activation-guard below (also a state-scoped `when` guard), yet this file's authoring guidance treats (d) as unavailable/open there. That asymmetry is recorded as a missing rule -- see ensure-activation-guard's notes and this file's missing-rule list -- not resolved by this cell.
- Not answered here, carried as an open dependency: whether an EARLIER conjunct of this same guard would be a premise for a fault arising in a LATER conjunct (e.g. `when Parts != 0 and Total / Parts > 1`) is not settled by anything the matrix currently states. This is a different question from class (c)'s exclusion above (which concerns the guard supplying a premise to an obligation arising within itself, full stop) -- it asks whether the guard's own internal conjunct ordering creates a second, narrower notion of "before" that class (c)'s exclusion does not reach. This cell does not attempt that spelling and states no answer for it.
- Measured live (Precept.MatrixTools, 2026-07-21, commit e1a14d91) on the base program below and on every discharge and near-miss addition in this cell: all compile with zero diagnostics, and the tool reports no obligation for this division (access-mode-guard mints nothing at HEAD in either lane). This is a direct, self-contained instance of the matrix's own "open design hole -- nothing detects an obligation that was never minted" (matrix lines 88-101): the compiler visits this site, computes no fault obligation for it, and nothing anywhere flags the absence.
- operationKind is recorded as the catalog's declaring-entry name ("IntegerDivideInteger", src/Precept/Language/Operations.cs; ProofRequirementKind.Numeric, src/Precept/Language/ProofRequirementKind.cs) rather than the coarser RequirementKind ("Numeric", shared by both lanes) -- the coarser value could not distinguish the integer and decimal lane cells as separate coordinates. Recorded so the choice is checkable, not asserted silently.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: the four premise classes
- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: the Fault family case-shape row
- docs/Working/obligation-discharge-matrix-2026-07-19.md:186 — matrix: Fault minting: catalog-stamped at evaluation sites

## g03/access-mode-guard-dec — Access-mode declaration guard -- decimal lane

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:186 — matrix: Fault minting: catalog-stamped at evaluation sites
- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: the Fault family case-shape row

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | DecimalDivideDecimal |
| evaluation site category | access-mode-guard |
| type family | primitive |

### What must be proven

Obligation: Parts != 0

Weakest precondition: Parts != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Total | the field read as the guard's dividend -- immaterial to the safety precondition itself |
| Parts | the field read as the guard's divisor |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:42 — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a), (d).

Derived per evaluation-site category, not asserted: this is a state-scoped access-mode declaration's `when` (`in S when G modify F readonly\|editable`). Classes (a) and (d) instantiate; (b) does not (see notes); (c) never does (structural, see notes).

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: field modifier on the divisor field, excluding zero -> interval containment over the modifier's declared bound
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor field's interval from its own declared modifiers (a finite set -- the modifier-to-ProofSatisfaction table, docs/compiler/proof-engine.md § Strategy 2); a computed interval excluding zero closes the obligation. Modelled on the matrix's own Family 4 sketch decision procedure (matrix line 299: "compute the divisor's interval from its declared modifiers and in-scope guard facts"), generalized here from premise class (b) (an event-arg modifier) to premise class (a) (a field modifier) on the same divisor. No excluding bound rejects, naming class (a).

- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: the four premise classes, including (a) field modifiers
- docs/Working/obligation-discharge-matrix-2026-07-19.md:206 — matrix: Arg-bound interval arithmetic -- the modifier-to-ProofSatisfaction interval derivation this entry extends from arg modifiers to field modifiers
- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 sketch -- "compute the divisor's interval from its declared modifiers and in-scope guard facts"

**Entry 2 — (d)**

- Derivation: a rule established and preserved over the divisor field, consumed as the inductive hypothesis -> the divisor's pre-state fact closes the obligation directly (CompositionalConstraint)
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Look up a nonzero fact for the divisor field among the constraints holding in the pre-state (the CompositionalConstraint strategy already exercised at HEAD for this exact shape -- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3, "rule PlannedQuantity > 0 ... discharges a division-by-zero obligation via the CompositionalConstraint strategy"). No such fact rejects, naming class (d).

- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: (d) all constraints holding in the pre-state
- docs/Working/obligation-discharge-matrix-2026-07-19.md:208 — matrix: Inductive hypothesis plus sign monotonicity -- extended here from its written sign-monotonicity/subtraction shape to a bare nonzero fact read directly
- docs/Working/obligation-discharge-matrix-2026-07-19.md:94 — matrix: the Open design hole section -- the matrix's own worked example of a rule consumed as premise (d) for a divisor-nonzero fault obligation
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — review: Defect B: a rule consumed as premise (d) while nothing establishes it (fail-open)

### What the failing diagnostic must suggest

- For class (a): declare a field modifier on <WP>'s divisor that excludes zero (e.g. positive, nonzero, or a min bound above zero)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md:46 — matrix: the suggestion-schema bullet

- For class (d): add a rule establishing and preserving <WP> over the divisor field, available as premise (d)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md:46 — matrix: the suggestion-schema bullet

  - A discharge accepted through this suggestion inherits the false-proof hazard: record its builtStatus as unresolved-today and its provenance as model-derived, never as confirmed by a clean compile, until the rule's own establishment/preservation obligations are built (docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3, Defect A and Defect B).


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept AmgBaseDec
field Total as decimal default 10.0
field Parts as decimal default 0.0
field Note as string optional editable
state Draft initial
state Done terminal
event Open initial
event Finish
on Open
    -> set Total = 10.0
    -> set Parts = 0.0
in Draft when Total / Parts > 1.0 modify Note readonly
from Draft on Finish
    -> transition Done
```

Required outcome: reject, naming the missing premise classes (a), (d), with no other diagnostics

Provenance: model-derived.

**Discharge additions — what makes the base compile.**

*access-mode-guard-dec-a* — other

- Addition: field Parts as decimal default 1.0 nonzero
- Premise classes: (a)
- Derivation: field modifier excluding zero on the divisor -> interval containment (class a)
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Live-verified (Precept.MatrixTools, 2026-07-21, commit e1a14d91): this addition compiles with zero diagnostics and no obligation is reported for the division -- the same no-mint behaviour as the base. builtStatus is unresolved-today because the site does not mint at HEAD at all; a clean compile here records the site's failure to mint, not confirmation that this discharge is accepted. This discharge is a field modifier (class a), which is NOT subject to the false-proof hazard (task authoring notes: "guard-based and modifier-based discharges ... are unaffected").)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Replaces the base's `field Parts as decimal default 0.0` declaration (and the matching construction assignment) with `field Parts as decimal default 1.0 nonzero`. The compiled program is the base program with that one field declaration replaced; it is not stored verbatim anywhere, and no `application` record reconstructs it mechanically (see the schema-vocabulary note below).
- kind is recorded as "other": the closed dischargeWitness.kind vocabulary has "arg-modifier" for an event-arg modifier but no corresponding value for a FIELD modifier, even though class (a) field modifiers is named in the Vocabulary (matrix line 38) as a first-class premise source. Flagged as a vocabulary gap.
- The schema's `application` discriminated union (row-guard / event-arg-declarations) has no locus for a field-declaration modifier addition or a whole new `rule` declaration -- neither of this discharge's addition shapes is either of the two loci the schema expresses. `application` is omitted rather than forced into an ill-fitting locus; the day-one validator's cell-to-test conversion cannot yet mechanically apply this addition. Flagged as a schema-vocabulary gap, not resolved here.

*access-mode-guard-dec-d* — other

- Addition: rule Parts != 0.0 because "Parts must stay nonzero"
- Premise classes: (d)
- Derivation: rule establishing the divisor's nonzero-ness, consumed as premise (d) via CompositionalConstraint
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Doubly unverifiable, so no live run is cited as confirmation here even though compiling this exact program shows the same no-mint behaviour as every other witness in this file: (i) this evaluation-site category mints no fault obligation at HEAD at all (measured on the base and on this addition); (ii) independent of (i), a `rule` consumed as premise (d) is exactly the false-proof-prone discharge path verified at HEAD (docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3, Defect B -- a rule can be accepted as a divisor-nonzero premise via the CompositionalConstraint strategy while nothing establishes or preserves it). A clean compile could never be read as confirmation of this discharge even if the site did mint. modelStatus is stated as provable-under-model on the strength of premise (d)'s general validity argument (matrix line 208), extended from its written sign-monotonicity/subtraction shape to a bare nonzero fact read directly -- an extension the matrix does not explicitly write out (see missing rules).)

Required outcome: accept, with the premise list recorded

Provenance: model-derived.

- Adds `rule Parts != 0.0 because "Parts must stay nonzero"` as a new top-level declaration and marks Total and Parts `editable` (parity with the standalone-rule shape the false-proof hazard's own repro uses; editability is incidental to the derivation, not load-bearing for it).
- kind is recorded as "other": the closed dischargeWitness.kind vocabulary has no value for "a new rule declaration added to the file" -- the closest options (guard, arg-modifier, default-constant-fold, structural-edit) all name a different shape. Flagged as a vocabulary gap.
- The schema's `application` discriminated union (row-guard / event-arg-declarations) has no locus for a field-declaration modifier addition or a whole new `rule` declaration -- neither of this discharge's addition shapes is either of the two loci the schema expresses. `application` is omitted rather than forced into an ill-fitting locus; the day-one validator's cell-to-test conversion cannot yet mechanically apply this addition. Flagged as a schema-vocabulary gap, not resolved here.
- Listed in this task's unverifiableWitnesses output: false-proof hazard (rule used as premise (d)), per docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3, Defect B, compounded by this site's no-mint status at HEAD.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| access-mode-guard-dec-a | field Parts as decimal default 0.0 nonnegative | `nonnegative` (unlike `nonzero`/`positive`) does not exclude 0.0: the divisor's declared interval still contains zero, so the interval-containment derivation does not close -- must still reject, same obligation. | reject, naming the same obligation |
| access-mode-guard-dec-d | rule Parts >= 0.0 because "Parts must stay nonnegative" | `rule Parts >= 0.0` (unlike `!=`) does not exclude zero: the pre-state fact still admits a zero divisor, so the inductive-hypothesis derivation does not close -- must still reject, same obligation. | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Class (c) -- the handler's guard -- is structurally unavailable and is excluded from every contract entry in this file. The obligation arises from evaluating the guard expression's own division, before the guard has produced the truth value anything downstream (a row's actions, an ensure, a rule, an access-mode decision) could rely on. Class (c) is defined as a fact the guard's truth supplies to obligations arising after it (matrix line 38: "(c) the handler's guard"; the Guard normal-form match argument, matrix line 204, grounds (c) precisely in a row's actions executing only after the guard evaluated true -- a precondition the guard's own internal evaluation cannot satisfy about itself). A guard cannot supply its own truth as a premise for an obligation it must discharge in order to compute that very truth value. This is the headline asymmetry the whole cell group demonstrates; it holds identically across all five evaluation-site categories and both lanes.
- Argument constraints (b) are structurally unavailable at this evaluation-site category regardless of what the guard reads: no triggering event is in scope here (a state hook fires on every inbound/outbound edge and an access-mode declaration carries no event at all), so there is no event-arg premise vocabulary to draw on.
- Pre-state constraints (d) are available on the following reading, offered here as this cell's own derivation rather than a matrix-stated rule: whatever configuration the entity currently occupies when the access-mode check runs is a reachable configuration the induction ranges over, so a rule established-and-preserved up to that point is a valid fact regardless of which check is reading it -- nothing in the Inductive hypothesis argument (matrix line 208) is specific to the write-site occasion. This is NOT the same question as the matrix's own explicitly open one -- "whether the residency fact itself is a premise" (matrix line 150) -- which concerns whether being-in-S is a fact, not whether an ordinary rule holding in the current configuration is. NOTE: the identical argument would appear to extend to ensure-activation-guard below (also a state-scoped `when` guard), yet this file's authoring guidance treats (d) as unavailable/open there. That asymmetry is recorded as a missing rule -- see ensure-activation-guard's notes and this file's missing-rule list -- not resolved by this cell.
- Not answered here, carried as an open dependency: whether an EARLIER conjunct of this same guard would be a premise for a fault arising in a LATER conjunct (e.g. `when Parts != 0 and Total / Parts > 1`) is not settled by anything the matrix currently states. This is a different question from class (c)'s exclusion above (which concerns the guard supplying a premise to an obligation arising within itself, full stop) -- it asks whether the guard's own internal conjunct ordering creates a second, narrower notion of "before" that class (c)'s exclusion does not reach. This cell does not attempt that spelling and states no answer for it.
- Measured live (Precept.MatrixTools, 2026-07-21, commit e1a14d91) on the base program below and on every discharge and near-miss addition in this cell: all compile with zero diagnostics, and the tool reports no obligation for this division (access-mode-guard mints nothing at HEAD in either lane). This is a direct, self-contained instance of the matrix's own "open design hole -- nothing detects an obligation that was never minted" (matrix lines 88-101): the compiler visits this site, computes no fault obligation for it, and nothing anywhere flags the absence.
- operationKind is recorded as the catalog's declaring-entry name ("DecimalDivideDecimal", src/Precept/Language/Operations.cs; ProofRequirementKind.Numeric, src/Precept/Language/ProofRequirementKind.cs) rather than the coarser RequirementKind ("Numeric", shared by both lanes) -- the coarser value could not distinguish the integer and decimal lane cells as separate coordinates. Recorded so the choice is checkable, not asserted silently.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: the four premise classes
- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: the Fault family case-shape row
- docs/Working/obligation-discharge-matrix-2026-07-19.md:186 — matrix: Fault minting: catalog-stamped at evaluation sites

## g03/ensure-activation-guard-int — Ensure activation guard -- integer lane

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:186 — matrix: Fault minting: catalog-stamped at evaluation sites
- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: the Fault family case-shape row

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | IntegerDivideInteger |
| evaluation site category | ensure-activation-guard |
| type family | primitive |

### What must be proven

Obligation: Parts != 0

Weakest precondition: Parts != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Total | the field read as the guard's dividend -- immaterial to the safety precondition itself |
| Parts | the field read as the guard's divisor |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:42 — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a).

Derived per evaluation-site category, not asserted: this is the optional pre-verb `when` of a state-anchored ensure (`in S when G ensure ...`). Only class (a) instantiates without further ruling; (b), (c), and (d) do not (see notes).

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: field modifier on the divisor field, excluding zero -> interval containment over the modifier's declared bound
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor field's interval from its own declared modifiers (a finite set -- the modifier-to-ProofSatisfaction table, docs/compiler/proof-engine.md § Strategy 2); a computed interval excluding zero closes the obligation. Modelled on the matrix's own Family 4 sketch decision procedure (matrix line 299: "compute the divisor's interval from its declared modifiers and in-scope guard facts"), generalized here from premise class (b) (an event-arg modifier) to premise class (a) (a field modifier) on the same divisor. No excluding bound rejects, naming class (a).

- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: the four premise classes, including (a) field modifiers
- docs/Working/obligation-discharge-matrix-2026-07-19.md:206 — matrix: Arg-bound interval arithmetic -- the modifier-to-ProofSatisfaction interval derivation this entry extends from arg modifiers to field modifiers
- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 sketch -- "compute the divisor's interval from its declared modifiers and in-scope guard facts"

### What the failing diagnostic must suggest

- For class (a): declare a field modifier on <WP>'s divisor that excludes zero (e.g. positive, nonzero, or a min bound above zero)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md:46 — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept EwgBaseInt
field Total as integer default 10
field Parts as integer default 0
state Draft initial
state Done terminal
event Open initial
event Finish
on Open
    -> set Total = 10
    -> set Parts = 0
in Done when Total / Parts > 1 ensure Total > 0 because "bounded"
from Draft on Finish
    -> transition Done
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: model-derived.

**Discharge additions — what makes the base compile.**

*ensure-activation-guard-int-a* — other

- Addition: field Parts as integer default 1 nonzero
- Premise classes: (a)
- Derivation: field modifier excluding zero on the divisor -> interval containment (class a)
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Live-verified (Precept.MatrixTools, 2026-07-21, commit e1a14d91): this addition compiles with zero diagnostics and no obligation is reported for the division -- the same no-mint behaviour as the base. builtStatus is unresolved-today because the site does not mint at HEAD at all; a clean compile here records the site's failure to mint, not confirmation that this discharge is accepted. This discharge is a field modifier (class a), which is NOT subject to the false-proof hazard (task authoring notes: "guard-based and modifier-based discharges ... are unaffected").)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Replaces the base's `field Parts as integer default 0` declaration (and the matching construction assignment) with `field Parts as integer default 1 nonzero`. The compiled program is the base program with that one field declaration replaced; it is not stored verbatim anywhere, and no `application` record reconstructs it mechanically (see the schema-vocabulary note below).
- kind is recorded as "other": the closed dischargeWitness.kind vocabulary has "arg-modifier" for an event-arg modifier but no corresponding value for a FIELD modifier, even though class (a) field modifiers is named in the Vocabulary (matrix line 38) as a first-class premise source. Flagged as a vocabulary gap.
- The schema's `application` discriminated union (row-guard / event-arg-declarations) has no locus for a field-declaration modifier addition or a whole new `rule` declaration -- neither of this discharge's addition shapes is either of the two loci the schema expresses. `application` is omitted rather than forced into an ill-fitting locus; the day-one validator's cell-to-test conversion cannot yet mechanically apply this addition. Flagged as a schema-vocabulary gap, not resolved here.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| ensure-activation-guard-int-a | field Parts as integer default 0 nonnegative | `nonnegative` (unlike `nonzero`/`positive`) does not exclude 0: the divisor's declared interval still contains zero, so the interval-containment derivation does not close -- must still reject, same obligation. | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Class (c) -- the handler's guard -- is structurally unavailable and is excluded from every contract entry in this file. The obligation arises from evaluating the guard expression's own division, before the guard has produced the truth value anything downstream (a row's actions, an ensure, a rule, an access-mode decision) could rely on. Class (c) is defined as a fact the guard's truth supplies to obligations arising after it (matrix line 38: "(c) the handler's guard"; the Guard normal-form match argument, matrix line 204, grounds (c) precisely in a row's actions executing only after the guard evaluated true -- a precondition the guard's own internal evaluation cannot satisfy about itself). A guard cannot supply its own truth as a premise for an obligation it must discharge in order to compute that very truth value. This is the headline asymmetry the whole cell group demonstrates; it holds identically across all five evaluation-site categories and both lanes.
- Class (d) is NOT included in this cell's applicablePremiseClasses. This slice's authoring guidance names only the transition-row, state-hook, and access-mode guards as positions where (d) is available; ensure-activation-guard is excluded from that list. The matrix itself does not state this exclusion, and the access-mode-guard cell's own derivation (a rule holding in whatever configuration the check runs against is a valid premise regardless of occasion) would appear to extend symmetrically to a state-anchored ensure's own activation guard, since both are ordinary state-scoped `when` guards evaluated against the current configuration. Recorded as an open item/missing rule rather than resolved either way: this cell states no (d) contract entry, but the exclusion is a curatorial choice for this pass, not an independently matrix-derived asymmetry.
- Not answered here, carried as an open dependency: whether an EARLIER conjunct of this same guard would be a premise for a fault arising in a LATER conjunct (e.g. `when Parts != 0 and Total / Parts > 1`) is not settled by anything the matrix currently states. This is a different question from class (c)'s exclusion above (which concerns the guard supplying a premise to an obligation arising within itself, full stop) -- it asks whether the guard's own internal conjunct ordering creates a second, narrower notion of "before" that class (c)'s exclusion does not reach. This cell does not attempt that spelling and states no answer for it.
- Measured live (Precept.MatrixTools, 2026-07-21, commit e1a14d91) on the base program below and on every discharge and near-miss addition in this cell: all compile with zero diagnostics, and the tool reports no obligation for this division (ensure-activation-guard mints nothing at HEAD in either lane). This is a direct, self-contained instance of the matrix's own "open design hole -- nothing detects an obligation that was never minted" (matrix lines 88-101): the compiler visits this site, computes no fault obligation for it, and nothing anywhere flags the absence.
- operationKind is recorded as the catalog's declaring-entry name ("IntegerDivideInteger", src/Precept/Language/Operations.cs; ProofRequirementKind.Numeric, src/Precept/Language/ProofRequirementKind.cs) rather than the coarser RequirementKind ("Numeric", shared by both lanes) -- the coarser value could not distinguish the integer and decimal lane cells as separate coordinates. Recorded so the choice is checkable, not asserted silently.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: the four premise classes
- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: the Fault family case-shape row
- docs/Working/obligation-discharge-matrix-2026-07-19.md:186 — matrix: Fault minting: catalog-stamped at evaluation sites

## g03/ensure-activation-guard-dec — Ensure activation guard -- decimal lane

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:186 — matrix: Fault minting: catalog-stamped at evaluation sites
- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: the Fault family case-shape row

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | DecimalDivideDecimal |
| evaluation site category | ensure-activation-guard |
| type family | primitive |

### What must be proven

Obligation: Parts != 0

Weakest precondition: Parts != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Total | the field read as the guard's dividend -- immaterial to the safety precondition itself |
| Parts | the field read as the guard's divisor |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:42 — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a).

Derived per evaluation-site category, not asserted: this is the optional pre-verb `when` of a state-anchored ensure (`in S when G ensure ...`). Only class (a) instantiates without further ruling; (b), (c), and (d) do not (see notes).

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: field modifier on the divisor field, excluding zero -> interval containment over the modifier's declared bound
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor field's interval from its own declared modifiers (a finite set -- the modifier-to-ProofSatisfaction table, docs/compiler/proof-engine.md § Strategy 2); a computed interval excluding zero closes the obligation. Modelled on the matrix's own Family 4 sketch decision procedure (matrix line 299: "compute the divisor's interval from its declared modifiers and in-scope guard facts"), generalized here from premise class (b) (an event-arg modifier) to premise class (a) (a field modifier) on the same divisor. No excluding bound rejects, naming class (a).

- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: the four premise classes, including (a) field modifiers
- docs/Working/obligation-discharge-matrix-2026-07-19.md:206 — matrix: Arg-bound interval arithmetic -- the modifier-to-ProofSatisfaction interval derivation this entry extends from arg modifiers to field modifiers
- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 sketch -- "compute the divisor's interval from its declared modifiers and in-scope guard facts"

### What the failing diagnostic must suggest

- For class (a): declare a field modifier on <WP>'s divisor that excludes zero (e.g. positive, nonzero, or a min bound above zero)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md:46 — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept EwgBaseDec
field Total as decimal default 10.0
field Parts as decimal default 0.0
state Draft initial
state Done terminal
event Open initial
event Finish
on Open
    -> set Total = 10.0
    -> set Parts = 0.0
in Done when Total / Parts > 1.0 ensure Total > 0.0 because "bounded"
from Draft on Finish
    -> transition Done
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: model-derived.

**Discharge additions — what makes the base compile.**

*ensure-activation-guard-dec-a* — other

- Addition: field Parts as decimal default 1.0 nonzero
- Premise classes: (a)
- Derivation: field modifier excluding zero on the divisor -> interval containment (class a)
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Live-verified (Precept.MatrixTools, 2026-07-21, commit e1a14d91): this addition compiles with zero diagnostics and no obligation is reported for the division -- the same no-mint behaviour as the base. builtStatus is unresolved-today because the site does not mint at HEAD at all; a clean compile here records the site's failure to mint, not confirmation that this discharge is accepted. This discharge is a field modifier (class a), which is NOT subject to the false-proof hazard (task authoring notes: "guard-based and modifier-based discharges ... are unaffected").)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Replaces the base's `field Parts as decimal default 0.0` declaration (and the matching construction assignment) with `field Parts as decimal default 1.0 nonzero`. The compiled program is the base program with that one field declaration replaced; it is not stored verbatim anywhere, and no `application` record reconstructs it mechanically (see the schema-vocabulary note below).
- kind is recorded as "other": the closed dischargeWitness.kind vocabulary has "arg-modifier" for an event-arg modifier but no corresponding value for a FIELD modifier, even though class (a) field modifiers is named in the Vocabulary (matrix line 38) as a first-class premise source. Flagged as a vocabulary gap.
- The schema's `application` discriminated union (row-guard / event-arg-declarations) has no locus for a field-declaration modifier addition or a whole new `rule` declaration -- neither of this discharge's addition shapes is either of the two loci the schema expresses. `application` is omitted rather than forced into an ill-fitting locus; the day-one validator's cell-to-test conversion cannot yet mechanically apply this addition. Flagged as a schema-vocabulary gap, not resolved here.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| ensure-activation-guard-dec-a | field Parts as decimal default 0.0 nonnegative | `nonnegative` (unlike `nonzero`/`positive`) does not exclude 0.0: the divisor's declared interval still contains zero, so the interval-containment derivation does not close -- must still reject, same obligation. | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Class (c) -- the handler's guard -- is structurally unavailable and is excluded from every contract entry in this file. The obligation arises from evaluating the guard expression's own division, before the guard has produced the truth value anything downstream (a row's actions, an ensure, a rule, an access-mode decision) could rely on. Class (c) is defined as a fact the guard's truth supplies to obligations arising after it (matrix line 38: "(c) the handler's guard"; the Guard normal-form match argument, matrix line 204, grounds (c) precisely in a row's actions executing only after the guard evaluated true -- a precondition the guard's own internal evaluation cannot satisfy about itself). A guard cannot supply its own truth as a premise for an obligation it must discharge in order to compute that very truth value. This is the headline asymmetry the whole cell group demonstrates; it holds identically across all five evaluation-site categories and both lanes.
- Class (d) is NOT included in this cell's applicablePremiseClasses. This slice's authoring guidance names only the transition-row, state-hook, and access-mode guards as positions where (d) is available; ensure-activation-guard is excluded from that list. The matrix itself does not state this exclusion, and the access-mode-guard cell's own derivation (a rule holding in whatever configuration the check runs against is a valid premise regardless of occasion) would appear to extend symmetrically to a state-anchored ensure's own activation guard, since both are ordinary state-scoped `when` guards evaluated against the current configuration. Recorded as an open item/missing rule rather than resolved either way: this cell states no (d) contract entry, but the exclusion is a curatorial choice for this pass, not an independently matrix-derived asymmetry.
- Not answered here, carried as an open dependency: whether an EARLIER conjunct of this same guard would be a premise for a fault arising in a LATER conjunct (e.g. `when Parts != 0 and Total / Parts > 1`) is not settled by anything the matrix currently states. This is a different question from class (c)'s exclusion above (which concerns the guard supplying a premise to an obligation arising within itself, full stop) -- it asks whether the guard's own internal conjunct ordering creates a second, narrower notion of "before" that class (c)'s exclusion does not reach. This cell does not attempt that spelling and states no answer for it.
- Measured live (Precept.MatrixTools, 2026-07-21, commit e1a14d91) on the base program below and on every discharge and near-miss addition in this cell: all compile with zero diagnostics, and the tool reports no obligation for this division (ensure-activation-guard mints nothing at HEAD in either lane). This is a direct, self-contained instance of the matrix's own "open design hole -- nothing detects an obligation that was never minted" (matrix lines 88-101): the compiler visits this site, computes no fault obligation for it, and nothing anywhere flags the absence.
- operationKind is recorded as the catalog's declaring-entry name ("DecimalDivideDecimal", src/Precept/Language/Operations.cs; ProofRequirementKind.Numeric, src/Precept/Language/ProofRequirementKind.cs) rather than the coarser RequirementKind ("Numeric", shared by both lanes) -- the coarser value could not distinguish the integer and decimal lane cells as separate coordinates. Recorded so the choice is checkable, not asserted silently.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: the four premise classes
- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: the Fault family case-shape row
- docs/Working/obligation-discharge-matrix-2026-07-19.md:186 — matrix: Fault minting: catalog-stamped at evaluation sites

## g03/rule-activation-guard-int — Conditional rule activation guard -- integer lane

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:186 — matrix: Fault minting: catalog-stamped at evaluation sites
- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: the Fault family case-shape row

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | IntegerDivideInteger |
| evaluation site category | rule-activation-guard |
| type family | primitive |

### What must be proven

Obligation: Parts != 0

Weakest precondition: Parts != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Total | the field read as the guard's dividend -- immaterial to the safety precondition itself |
| Parts | the field read as the guard's divisor |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:42 — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a).

Derived per evaluation-site category, not asserted: this is the `when` of a conditional rule (`rule ... when G because ...`). Only class (a) instantiates without further ruling; (b), (c), and (d) do not (see notes).

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: field modifier on the divisor field, excluding zero -> interval containment over the modifier's declared bound
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor field's interval from its own declared modifiers (a finite set -- the modifier-to-ProofSatisfaction table, docs/compiler/proof-engine.md § Strategy 2); a computed interval excluding zero closes the obligation. Modelled on the matrix's own Family 4 sketch decision procedure (matrix line 299: "compute the divisor's interval from its declared modifiers and in-scope guard facts"), generalized here from premise class (b) (an event-arg modifier) to premise class (a) (a field modifier) on the same divisor. No excluding bound rejects, naming class (a).

- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: the four premise classes, including (a) field modifiers
- docs/Working/obligation-discharge-matrix-2026-07-19.md:206 — matrix: Arg-bound interval arithmetic -- the modifier-to-ProofSatisfaction interval derivation this entry extends from arg modifiers to field modifiers
- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 sketch -- "compute the divisor's interval from its declared modifiers and in-scope guard facts"

### What the failing diagnostic must suggest

- For class (a): declare a field modifier on <WP>'s divisor that excludes zero (e.g. positive, nonzero, or a min bound above zero)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md:46 — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept RagBaseInt
field Total as integer default 10 editable
field Parts as integer default 0 editable
rule Total > 0 when Total / Parts > 1 because "bounded"
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: model-derived.

**Discharge additions — what makes the base compile.**

*rule-activation-guard-int-a* — other

- Addition: field Parts as integer default 1 editable nonzero
- Premise classes: (a)
- Derivation: field modifier excluding zero on the divisor -> interval containment (class a)
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Live-verified (Precept.MatrixTools, 2026-07-21, commit e1a14d91): this addition compiles with zero diagnostics and no obligation is reported for the division -- the same no-mint behaviour as the base. builtStatus is unresolved-today because the site does not mint at HEAD at all; a clean compile here records the site's failure to mint, not confirmation that this discharge is accepted. This discharge is a field modifier (class a), which is NOT subject to the false-proof hazard (task authoring notes: "guard-based and modifier-based discharges ... are unaffected").)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Replaces the base's `field Parts as integer default 0` declaration (and the matching construction assignment) with `field Parts as integer default 1 editable nonzero`. The compiled program is the base program with that one field declaration replaced; it is not stored verbatim anywhere, and no `application` record reconstructs it mechanically (see the schema-vocabulary note below).
- kind is recorded as "other": the closed dischargeWitness.kind vocabulary has "arg-modifier" for an event-arg modifier but no corresponding value for a FIELD modifier, even though class (a) field modifiers is named in the Vocabulary (matrix line 38) as a first-class premise source. Flagged as a vocabulary gap.
- The schema's `application` discriminated union (row-guard / event-arg-declarations) has no locus for a field-declaration modifier addition or a whole new `rule` declaration -- neither of this discharge's addition shapes is either of the two loci the schema expresses. `application` is omitted rather than forced into an ill-fitting locus; the day-one validator's cell-to-test conversion cannot yet mechanically apply this addition. Flagged as a schema-vocabulary gap, not resolved here.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| rule-activation-guard-int-a | field Parts as integer default 0 editable nonnegative | `nonnegative` (unlike `nonzero`/`positive`) does not exclude 0: the divisor's declared interval still contains zero, so the interval-containment derivation does not close -- must still reject, same obligation. | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Class (c) -- the handler's guard -- is structurally unavailable and is excluded from every contract entry in this file. The obligation arises from evaluating the guard expression's own division, before the guard has produced the truth value anything downstream (a row's actions, an ensure, a rule, an access-mode decision) could rely on. Class (c) is defined as a fact the guard's truth supplies to obligations arising after it (matrix line 38: "(c) the handler's guard"; the Guard normal-form match argument, matrix line 204, grounds (c) precisely in a row's actions executing only after the guard evaluated true -- a precondition the guard's own internal evaluation cannot satisfy about itself). A guard cannot supply its own truth as a premise for an obligation it must discharge in order to compute that very truth value. This is the headline asymmetry the whole cell group demonstrates; it holds identically across all five evaluation-site categories and both lanes.
- Class (d) is NOT included in this cell's applicablePremiseClasses, on a derivation this cell can state directly from the matrix's own text (not merely a curatorial choice): class (d) is defined as "all constraints holding in THE pre-state" (matrix line 38, definite article -- one specific configuration, the one a single write plan starts from). A `rule`'s own activation guard is not anchored to any one write plan: the Constraint kinds table states a `rule` (Invariant) must hold "always, after every mutation" (matrix line 59) -- its activation condition is checked at every checkpoint, not keyed to one specific pre-state. "The pre-state" is therefore ill-defined for an obligation quantified over every reachable configuration rather than anchored to a single write plan's start, which is why this cell records (d) as open rather than available, in agreement with this slice's authoring guidance.
- Not answered here, carried as an open dependency: whether an EARLIER conjunct of this same guard would be a premise for a fault arising in a LATER conjunct (e.g. `when Parts != 0 and Total / Parts > 1`) is not settled by anything the matrix currently states. This is a different question from class (c)'s exclusion above (which concerns the guard supplying a premise to an obligation arising within itself, full stop) -- it asks whether the guard's own internal conjunct ordering creates a second, narrower notion of "before" that class (c)'s exclusion does not reach. This cell does not attempt that spelling and states no answer for it.
- Measured live (Precept.MatrixTools, 2026-07-21, commit e1a14d91) on the base program below and on every discharge and near-miss addition in this cell: all compile with zero diagnostics, and the tool reports no obligation for this division (rule-activation-guard mints nothing at HEAD in either lane). This is a direct, self-contained instance of the matrix's own "open design hole -- nothing detects an obligation that was never minted" (matrix lines 88-101): the compiler visits this site, computes no fault obligation for it, and nothing anywhere flags the absence.
- operationKind is recorded as the catalog's declaring-entry name ("IntegerDivideInteger", src/Precept/Language/Operations.cs; ProofRequirementKind.Numeric, src/Precept/Language/ProofRequirementKind.cs) rather than the coarser RequirementKind ("Numeric", shared by both lanes) -- the coarser value could not distinguish the integer and decimal lane cells as separate coordinates. Recorded so the choice is checkable, not asserted silently.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: the four premise classes
- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: the Fault family case-shape row
- docs/Working/obligation-discharge-matrix-2026-07-19.md:186 — matrix: Fault minting: catalog-stamped at evaluation sites

## g03/rule-activation-guard-dec — Conditional rule activation guard -- decimal lane

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:186 — matrix: Fault minting: catalog-stamped at evaluation sites
- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: the Fault family case-shape row

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | DecimalDivideDecimal |
| evaluation site category | rule-activation-guard |
| type family | primitive |

### What must be proven

Obligation: Parts != 0

Weakest precondition: Parts != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Total | the field read as the guard's dividend -- immaterial to the safety precondition itself |
| Parts | the field read as the guard's divisor |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:42 — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a).

Derived per evaluation-site category, not asserted: this is the `when` of a conditional rule (`rule ... when G because ...`). Only class (a) instantiates without further ruling; (b), (c), and (d) do not (see notes).

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: field modifier on the divisor field, excluding zero -> interval containment over the modifier's declared bound
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor field's interval from its own declared modifiers (a finite set -- the modifier-to-ProofSatisfaction table, docs/compiler/proof-engine.md § Strategy 2); a computed interval excluding zero closes the obligation. Modelled on the matrix's own Family 4 sketch decision procedure (matrix line 299: "compute the divisor's interval from its declared modifiers and in-scope guard facts"), generalized here from premise class (b) (an event-arg modifier) to premise class (a) (a field modifier) on the same divisor. No excluding bound rejects, naming class (a).

- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: the four premise classes, including (a) field modifiers
- docs/Working/obligation-discharge-matrix-2026-07-19.md:206 — matrix: Arg-bound interval arithmetic -- the modifier-to-ProofSatisfaction interval derivation this entry extends from arg modifiers to field modifiers
- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 sketch -- "compute the divisor's interval from its declared modifiers and in-scope guard facts"

### What the failing diagnostic must suggest

- For class (a): declare a field modifier on <WP>'s divisor that excludes zero (e.g. positive, nonzero, or a min bound above zero)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md:46 — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept RagBaseDec
field Total as decimal default 10.0 editable
field Parts as decimal default 0.0 editable
rule Total > 0.0 when Total / Parts > 1.0 because "bounded"
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: model-derived.

**Discharge additions — what makes the base compile.**

*rule-activation-guard-dec-a* — other

- Addition: field Parts as decimal default 1.0 editable nonzero
- Premise classes: (a)
- Derivation: field modifier excluding zero on the divisor -> interval containment (class a)
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Live-verified (Precept.MatrixTools, 2026-07-21, commit e1a14d91): this addition compiles with zero diagnostics and no obligation is reported for the division -- the same no-mint behaviour as the base. builtStatus is unresolved-today because the site does not mint at HEAD at all; a clean compile here records the site's failure to mint, not confirmation that this discharge is accepted. This discharge is a field modifier (class a), which is NOT subject to the false-proof hazard (task authoring notes: "guard-based and modifier-based discharges ... are unaffected").)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Replaces the base's `field Parts as decimal default 0.0` declaration (and the matching construction assignment) with `field Parts as decimal default 1.0 editable nonzero`. The compiled program is the base program with that one field declaration replaced; it is not stored verbatim anywhere, and no `application` record reconstructs it mechanically (see the schema-vocabulary note below).
- kind is recorded as "other": the closed dischargeWitness.kind vocabulary has "arg-modifier" for an event-arg modifier but no corresponding value for a FIELD modifier, even though class (a) field modifiers is named in the Vocabulary (matrix line 38) as a first-class premise source. Flagged as a vocabulary gap.
- The schema's `application` discriminated union (row-guard / event-arg-declarations) has no locus for a field-declaration modifier addition or a whole new `rule` declaration -- neither of this discharge's addition shapes is either of the two loci the schema expresses. `application` is omitted rather than forced into an ill-fitting locus; the day-one validator's cell-to-test conversion cannot yet mechanically apply this addition. Flagged as a schema-vocabulary gap, not resolved here.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| rule-activation-guard-dec-a | field Parts as decimal default 0.0 editable nonnegative | `nonnegative` (unlike `nonzero`/`positive`) does not exclude 0.0: the divisor's declared interval still contains zero, so the interval-containment derivation does not close -- must still reject, same obligation. | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Class (c) -- the handler's guard -- is structurally unavailable and is excluded from every contract entry in this file. The obligation arises from evaluating the guard expression's own division, before the guard has produced the truth value anything downstream (a row's actions, an ensure, a rule, an access-mode decision) could rely on. Class (c) is defined as a fact the guard's truth supplies to obligations arising after it (matrix line 38: "(c) the handler's guard"; the Guard normal-form match argument, matrix line 204, grounds (c) precisely in a row's actions executing only after the guard evaluated true -- a precondition the guard's own internal evaluation cannot satisfy about itself). A guard cannot supply its own truth as a premise for an obligation it must discharge in order to compute that very truth value. This is the headline asymmetry the whole cell group demonstrates; it holds identically across all five evaluation-site categories and both lanes.
- Class (d) is NOT included in this cell's applicablePremiseClasses, on a derivation this cell can state directly from the matrix's own text (not merely a curatorial choice): class (d) is defined as "all constraints holding in THE pre-state" (matrix line 38, definite article -- one specific configuration, the one a single write plan starts from). A `rule`'s own activation guard is not anchored to any one write plan: the Constraint kinds table states a `rule` (Invariant) must hold "always, after every mutation" (matrix line 59) -- its activation condition is checked at every checkpoint, not keyed to one specific pre-state. "The pre-state" is therefore ill-defined for an obligation quantified over every reachable configuration rather than anchored to a single write plan's start, which is why this cell records (d) as open rather than available, in agreement with this slice's authoring guidance.
- Not answered here, carried as an open dependency: whether an EARLIER conjunct of this same guard would be a premise for a fault arising in a LATER conjunct (e.g. `when Parts != 0 and Total / Parts > 1`) is not settled by anything the matrix currently states. This is a different question from class (c)'s exclusion above (which concerns the guard supplying a premise to an obligation arising within itself, full stop) -- it asks whether the guard's own internal conjunct ordering creates a second, narrower notion of "before" that class (c)'s exclusion does not reach. This cell does not attempt that spelling and states no answer for it.
- Measured live (Precept.MatrixTools, 2026-07-21, commit e1a14d91) on the base program below and on every discharge and near-miss addition in this cell: all compile with zero diagnostics, and the tool reports no obligation for this division (rule-activation-guard mints nothing at HEAD in either lane). This is a direct, self-contained instance of the matrix's own "open design hole -- nothing detects an obligation that was never minted" (matrix lines 88-101): the compiler visits this site, computes no fault obligation for it, and nothing anywhere flags the absence.
- operationKind is recorded as the catalog's declaring-entry name ("DecimalDivideDecimal", src/Precept/Language/Operations.cs; ProofRequirementKind.Numeric, src/Precept/Language/ProofRequirementKind.cs) rather than the coarser RequirementKind ("Numeric", shared by both lanes) -- the coarser value could not distinguish the integer and decimal lane cells as separate coordinates. Recorded so the choice is checkable, not asserted silently.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: the four premise classes
- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: the Fault family case-shape row
- docs/Working/obligation-discharge-matrix-2026-07-19.md:186 — matrix: Fault minting: catalog-stamped at evaluation sites

