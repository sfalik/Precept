<!--
GENERATED FILE — do not hand-edit.
Source: fault-14-collection-non-empty-guard-positions.cells.json
Regenerate: dotnet run --project tools/Precept.MatrixTools -- render-cells docs/Working/obligation-discharge-matrix-2026-07-19-cells
Hand edits will be overwritten. Edit the .cells.json and regenerate.
-->

# Reading from a possibly-empty collection inside a guard

Family id: fault-14-collection-non-empty-guard-positions
Case shape: fault
Definition version: obligation-discharge-matrix-2026-07-19 rev 7 (2026-07-20, draft)

**Where this family's content comes from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: fault family case shape
- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: the four premise classes
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: sound-but-unprovable band and respellable verdict definitions
- src/Precept/Language/Types.cs:205 — code: queue accessor .peek — Numeric requirement
- src/Precept/Language/Types.cs:297 — code: list accessor .first — Numeric requirement
- src/Precept/Language/Modifiers.cs:195 — code: Mincount ProofSatisfaction

## Respellability — the family verdict

Can every safe program these rules reject be rewritten into a form they accept? **yes**

Evidence: model-derived
Corpus measurement: pending

**Notes on the family verdict**

- Every band member identified in this family (the self-referential same-guard spelling) has at least one always-available respelling: declare the field's `mincount` modifier, or state a top-level `rule` the induction can consume once built. Neither respelling has been measured against the sample corpus.
- This verdict covers the ten authored cells; it does not extend a claim over the un-authored 65 remaining coordinates in this group's region (13 catalog sites x 5 categories minus the 2 sites x 5 categories authored here).

**Sources for this verdict**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the respellable verdict definition
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Ratification protocol — matrix: layer 4 — corpus measurement is the hard gate

## Premise classes — the legend

Where a proof may get its facts (want :19, :143): (a) field modifiers, (b) arg constraints, (c) the handler's guard, (d) all rules holding in the pre-state (the inductive hypothesis), (e) evaluating the applicable rule set at an editable-field write — (e)'s status as a genuinely fifth class vs (a) generalized is an open owner item.

## g14/list-first-transition-row-guard — List.first read inside a transition row's `when` clause

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: fault family case shape
- docs/language/precept-language-spec.md § Transition row — spec

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | accessor/List/first/numeric-0 |
| evaluation site category | transition-row-guard |
| type family | collection |

### What must be proven

Obligation: Items.count > 0

Weakest precondition: Items.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Items | the list field the accessor reads |
| 0 | the fixed non-emptiness threshold — the catalog's declared condition, never authored |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a), (d).

No write occurs at a transition row's `when` clause (it decides applicability/writability; it does not mutate). Class (c) is unavailable — the guard cannot supply a premise to an obligation arising inside itself, the headline asymmetry of this evaluation-site region. Class (b) is unavailable — no event argument represents the collection's cardinality, so no arg constraint can instantiate this obligation. Class (a) (the field's own declared `mincount`) is always in scope. Class (d) is available at this category (pre-state constraints), per the evaluation-site region's own axis facts.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: compile-time interval fact from `Items`'s declared `mincount` modifier (ProofSatisfaction.Numeric: `count >= mincount`, `src/Precept/Language/Modifiers.cs:195-208`) — a declared `mincount` of 1 or more entails `Items.count > 0`, the catalog's own fault precondition (`src/Precept/Language/Types.cs:297`)
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Look up `Items`'s declared `mincount` value, if any, via the ProofSatisfactions table; the fault discharges iff the declared `mincount` is >= 1 (for an integer count, `count >= 1` entails `count > 0`). No `mincount` modifier, or one declaring 0, fails the class.

- src/Precept/Language/Modifiers.cs:195 — code: Mincount ProofSatisfaction — count >= DeclarationValue
- src/Precept/Language/Types.cs:297 — code: list.first's catalog-declared Numeric requirement — List must be non-empty
- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: fault-family case shape: premise classes (b)/(c)/(a) per catalog ProofSatisfactions

**Entry 2 — (d)**

- Derivation: a `rule Items.count > 0` (or an equivalent lower-bound form) holding in the pre-state, consumed directly as the fault's WP with no arithmetic — the same fact, not a derived one
- Strategy: CompositionalConstraint
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Search the pre-state constraint set for a `rule` whose condition is normal-form-equal to (or N13-covers) `Items.count > 0`; found, consumed via CompositionalConstraint. Absent, or naming a different field, fails the class.

- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: premise class (d): all constraints holding in the pre-state
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Inductive hypothesis plus sign monotonicity

### What the failing diagnostic must suggest

- For class (a): apply a `mincount` modifier (at least 1) to the collection field's declaration
  - src/Precept/Language/Modifiers.cs:125 — code: the Notempty HoverDescription names mincount 1, not notempty, as the collection route

- For class (d): add a `rule <WP>` (e.g. `rule Items.count > 0`) holding in the pre-state
  - This suggestion is licensed under the model. At HEAD it is also the exact shape of the Defect B false-proof hazard (authored-expressiveness-gaps.md, Part 3): a rule consumed as a premise while nothing yet establishes or preserves it. Recorded as the model's suggestion; a diagnostic emitting it today would be recommending a currently-unsound spelling. Not resolved here.


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept ListFirstTransitionrowguard
field Items as list of integer
state Active initial
state Closed terminal
event Add(Value as integer)
event Check
event Close
from Active on Add
    -> append Items Add.Value
    -> no transition
from Active on Check when Items.first > 10
    -> no transition
from Active on Close
    -> transition Closed
```

Required outcome: reject, naming the missing premise classes (a), (d), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*mincount-1* — other

- Addition: mincount 1  # added to the `Items` field declaration
- Premise classes: (a)
- Derivation: declared mincount 1 -> count >= 1 -> count > 0 (Inductive hypothesis plus sign monotonicity, trivial/identity case)
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (the transition-row-guard position mints no fault obligation at all at HEAD (measured, commit e1a14d91) — there is nothing for this addition to discharge; the clean compile below is the site failing to mint, not the addition being recognized as a discharge)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live run (2026-07-21, Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), HEAD e1a14d91): `list-transition-row-guard__discharge-a.precept` compiles with rc=0 and no diagnostics (stdout carries only the WP calculator's own '[skipped obligation] ...mincount: accessor-projected bound...' note, which is about the calculator's inability to compute an establishment/preservation WP for the mincount-desugared rule — unrelated to the fault obligation this cell defines).
- `application` is omitted: this addition changes a field declaration's modifier list, which is neither a row guard (`row-guard` locus) nor an event-arg declaration (`event-arg-declarations` locus) — the schema's `application` DU has no locus for a field-modifier addition. The cell->test conversion machinery cannot mechanically apply this discharge today; flagged as a schema/tooling gap, not worked around.

*rule-count-gt-0* — other

- Addition: rule Items.count > 0 because "the collection is provisioned non-empty at handoff"  # added as a top-level declaration
- Premise classes: (d)
- Derivation: pre-state rule Items.count > 0 consumed directly as the WP -> CompositionalConstraint
- Strategy: CompositionalConstraint
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (the transition-row-guard position mints no fault obligation at all at HEAD (measured, commit e1a14d91) — there is nothing for this addition to discharge. Separately and more seriously: even at a position that DOES mint, Defect B shows a `rule` consumed as a premise via CompositionalConstraint while nothing establishes or preserves it is a live false-proof at HEAD (authored-expressiveness-gaps.md, Part 3) — so this discharge must never be read as confirmed by a clean compile, here or elsewhere.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live run (2026-07-21, Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), HEAD e1a14d91): `list-transition-row-guard__discharge-d.precept` compiles with rc=0 and no diagnostics related to the target fault (stdout shows the WP calculator's own printout for the added `rule` obligation itself — establishment WP and a 'NOT SUPPORTED' preservation note for the append action, both about the calculator's own rule-write machinery, not the fault obligation this cell defines).
- `application` is omitted: this addition is a new top-level `rule` declaration, not a row guard or an event-arg declaration — the same schema/tooling gap as the mincount discharge above.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| mincount-1 | mincount 0  # added to the `Items` field declaration | mincount 0 does not exclude an empty collection — the compile-time lower bound is 0, not >= 1, so it still admits the empty case; must still reject under the model, same obligation. | reject, naming the same obligation |
| rule-count-gt-0 | rule Items.count >= 0 because "..."  # weakened top-level declaration | `>= 0` is trivially true of any count and does not establish non-emptiness; must still reject under the model, same obligation. | reject, naming the same obligation |

### Respellability

- Resolution: cell-override
- A representative safe-but-rejected spelling: from Active on Check when Items.count > 0 and Items.first > 10     -> no transition
- Its licensed respelling: declare `Items` with `mincount 1` (class a), or state a top-level `rule Items.count > 0` that the induction consumes once the rule-write establishment/preservation machinery is built (class d) — both close the fault without relying on same-guard conjunct ordering, which the matrix does not license (see notes).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the respellable verdict definition

- The band member is the natural author spelling for this group: a single guard combining the cardinality check and the accessor read in one `and`-joined condition. Whether the first conjunct (`count > 0`) is itself a licensed premise for the second (`.first`/`.peek` above threshold) is an open question the matrix does not answer (see the cell's own notes) — this respelling is offered as the always-available fallback, not as evidence the band member is permanently unlicensable.

**What the sources leave unstated or ambiguous here**

- Built-status: measured at HEAD (commit e1a14d91, 2026-07-21) via `Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll)` — a transition row's `when` clause mints no fault obligation at all. Base, both discharge additions, and both near-misses all compile clean (rc=0, no diagnostic touching this fault) regardless of which addition is present. This is the site failing to mint, not the additions being recognized or the near-misses failing to be caught — see each witness's own notes for the literal compile output.
- Open question, not licensed here: the natural author spelling `when Items.count > 0 and Items.first > 10` puts both facts in the SAME guard. Class (c) — the handler's own guard — is unavailable at every guard position (a guard cannot supply a premise to an obligation arising inside itself), but whether an EARLIER conjunct of that same guard licenses a LATER one is not answered by the matrix or by `precept-language-spec.md`. This cell does not license that spelling; the band-member respelling above is offered as the licensed alternative, not as tacit permission for the self-referential form.
- Class (d) is listed as available at this evaluation-site category (constraints holding in the pre-state), so a discharge witness citing a `rule` is authored here. Per the group's hazard instruction, every such witness is recorded `builtStatus: unresolved-today` with the false-proof reason named — never as confirmed by a clean compile.
- No validity argument in the matrix's § Validity arguments literally covers premise class (a) — a collection field's own declared `mincount` modifier, as opposed to an event arg's modifier — discharging a fault-family obligation. 'Arg-bound interval arithmetic' is scoped in its own text to values 'entering as an event arg' (want:268/ingress governance); it does not by its own words extend to a persistent field's structurally-enforced bound. The closer fit is 'Inductive hypothesis plus sign monotonicity' — since every ProofSatisfaction-bearing modifier desugars to an implicit rule (`DesugarsToRule: true`, `Modifiers.cs`) under the SAME establishment+preservation obligation machinery as an author-written rule — but that argument's own worked text is about combining the hypothesis with a sign-monotonicity step (Family 1 Base B), not the direct-identity case used here. Cited as the closest available named argument; the generalization to synthesized field-modifier rules, and to the trivial (no-arithmetic) case, is not itself written out. Recorded as a missing rule, not silently absorbed by the citation.
- The fault-family case-shape table (matrix, the per-family case-shapes table) lists premise classes (b)/(c)/(a) for the fault family and does not list (d), yet this cell (per the group's own authoring instruction and the region's axis facts) authors a class-(d) discharge, and Defect B independently shows class (d) being consumed for a fault discharge at HEAD via CompositionalConstraint. Whether (d) is a licensed fault-discharge premise class at all is not stated cleanly in the matrix text — flagged as a missing rule, distinct from (and prior to) the false-proof soundness hazard Defect B documents.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: fault family case shape — catalog-declared safety precondition at the evaluation site
- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: the four premise classes
- docs/language/precept-language-spec.md § Transition row — spec
- src/Precept/Language/Types.cs:297 — code: list.first's catalog-declared Numeric requirement
- src/Precept/Language/Diagnostics.cs:568 — code: UnguardedCollectionAccess — PreventsFault CollectionEmptyOnAccess, the write-site analogue of this obligation

## g14/list-first-state-hook-guard — List.first read inside a state entry hook's pre-verb `when` clause

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: fault family case shape
- docs/language/precept-language-spec.md § State action — spec

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | accessor/List/first/numeric-0 |
| evaluation site category | state-hook-guard |
| type family | collection |

### What must be proven

Obligation: Items.count > 0

Weakest precondition: Items.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Items | the list field the accessor reads |
| 0 | the fixed non-emptiness threshold — the catalog's declared condition, never authored |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a), (d).

No write occurs at a state entry hook's pre-verb `when` clause (it decides applicability/writability; it does not mutate). Class (c) is unavailable — the guard cannot supply a premise to an obligation arising inside itself, the headline asymmetry of this evaluation-site region. Class (b) is unavailable — no event argument represents the collection's cardinality, so no arg constraint can instantiate this obligation. Class (a) (the field's own declared `mincount`) is always in scope. Class (d) is available at this category (pre-state constraints), per the evaluation-site region's own axis facts.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: compile-time interval fact from `Items`'s declared `mincount` modifier (ProofSatisfaction.Numeric: `count >= mincount`, `src/Precept/Language/Modifiers.cs:195-208`) — a declared `mincount` of 1 or more entails `Items.count > 0`, the catalog's own fault precondition (`src/Precept/Language/Types.cs:297`)
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Look up `Items`'s declared `mincount` value, if any, via the ProofSatisfactions table; the fault discharges iff the declared `mincount` is >= 1 (for an integer count, `count >= 1` entails `count > 0`). No `mincount` modifier, or one declaring 0, fails the class.

- src/Precept/Language/Modifiers.cs:195 — code: Mincount ProofSatisfaction — count >= DeclarationValue
- src/Precept/Language/Types.cs:297 — code: list.first's catalog-declared Numeric requirement — List must be non-empty
- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: fault-family case shape: premise classes (b)/(c)/(a) per catalog ProofSatisfactions

**Entry 2 — (d)**

- Derivation: a `rule Items.count > 0` (or an equivalent lower-bound form) holding in the pre-state, consumed directly as the fault's WP with no arithmetic — the same fact, not a derived one
- Strategy: CompositionalConstraint
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Search the pre-state constraint set for a `rule` whose condition is normal-form-equal to (or N13-covers) `Items.count > 0`; found, consumed via CompositionalConstraint. Absent, or naming a different field, fails the class.

- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: premise class (d): all constraints holding in the pre-state
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Inductive hypothesis plus sign monotonicity

### What the failing diagnostic must suggest

- For class (a): apply a `mincount` modifier (at least 1) to the collection field's declaration
  - src/Precept/Language/Modifiers.cs:125 — code: the Notempty HoverDescription names mincount 1, not notempty, as the collection route

- For class (d): add a `rule <WP>` (e.g. `rule Items.count > 0`) holding in the pre-state
  - This suggestion is licensed under the model. At HEAD it is also the exact shape of the Defect B false-proof hazard (authored-expressiveness-gaps.md, Part 3): a rule consumed as a premise while nothing yet establishes or preserves it. Recorded as the model's suggestion; a diagnostic emitting it today would be recommending a currently-unsound spelling. Not resolved here.


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept ListFirstStatehookguard
field Items as list of integer
field Note as integer default 0
state Draft initial
state Done terminal
event Add(Value as integer)
event Finish
from Draft on Add
    -> append Items Add.Value
    -> no transition
to Done when Items.first > 10 -> set Note = 1
from Draft on Finish
    -> transition Done
```

Required outcome: reject, naming the missing premise classes (a), (d), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*mincount-1* — other

- Addition: mincount 1  # added to the `Items` field declaration
- Premise classes: (a)
- Derivation: declared mincount 1 -> count >= 1 -> count > 0 (Inductive hypothesis plus sign monotonicity, trivial/identity case)
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (the state-hook-guard position mints no fault obligation at all at HEAD (measured, commit e1a14d91) — there is nothing for this addition to discharge; the clean compile below is the site failing to mint, not the addition being recognized as a discharge)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live run (2026-07-21, Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), HEAD e1a14d91): `list-state-hook-guard__discharge-a.precept` compiles with rc=0 and no diagnostics (stdout carries only the WP calculator's own '[skipped obligation] ...mincount: accessor-projected bound...' note, which is about the calculator's inability to compute an establishment/preservation WP for the mincount-desugared rule — unrelated to the fault obligation this cell defines).
- `application` is omitted: this addition changes a field declaration's modifier list, which is neither a row guard (`row-guard` locus) nor an event-arg declaration (`event-arg-declarations` locus) — the schema's `application` DU has no locus for a field-modifier addition. The cell->test conversion machinery cannot mechanically apply this discharge today; flagged as a schema/tooling gap, not worked around.

*rule-count-gt-0* — other

- Addition: rule Items.count > 0 because "the collection is provisioned non-empty at handoff"  # added as a top-level declaration
- Premise classes: (d)
- Derivation: pre-state rule Items.count > 0 consumed directly as the WP -> CompositionalConstraint
- Strategy: CompositionalConstraint
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (the state-hook-guard position mints no fault obligation at all at HEAD (measured, commit e1a14d91) — there is nothing for this addition to discharge. Separately and more seriously: even at a position that DOES mint, Defect B shows a `rule` consumed as a premise via CompositionalConstraint while nothing establishes or preserves it is a live false-proof at HEAD (authored-expressiveness-gaps.md, Part 3) — so this discharge must never be read as confirmed by a clean compile, here or elsewhere.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live run (2026-07-21, Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), HEAD e1a14d91): `list-state-hook-guard__discharge-d.precept` compiles with rc=0 and no diagnostics related to the target fault (stdout shows the WP calculator's own printout for the added `rule` obligation itself — establishment WP and a 'NOT SUPPORTED' preservation note for the append action, both about the calculator's own rule-write machinery, not the fault obligation this cell defines).
- `application` is omitted: this addition is a new top-level `rule` declaration, not a row guard or an event-arg declaration — the same schema/tooling gap as the mincount discharge above.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| mincount-1 | mincount 0  # added to the `Items` field declaration | mincount 0 does not exclude an empty collection — the compile-time lower bound is 0, not >= 1, so it still admits the empty case; must still reject under the model, same obligation. | reject, naming the same obligation |
| rule-count-gt-0 | rule Items.count >= 0 because "..."  # weakened top-level declaration | `>= 0` is trivially true of any count and does not establish non-emptiness; must still reject under the model, same obligation. | reject, naming the same obligation |

### Respellability

- Resolution: cell-override
- A representative safe-but-rejected spelling: to Done when Items.count > 0 and Items.first > 10 -> set Note = 1
- Its licensed respelling: declare `Items` with `mincount 1` (class a), or state a top-level `rule Items.count > 0` that the induction consumes once the rule-write establishment/preservation machinery is built (class d) — both close the fault without relying on same-guard conjunct ordering, which the matrix does not license (see notes).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the respellable verdict definition

- The band member is the natural author spelling for this group: a single guard combining the cardinality check and the accessor read in one `and`-joined condition. Whether the first conjunct (`count > 0`) is itself a licensed premise for the second (`.first`/`.peek` above threshold) is an open question the matrix does not answer (see the cell's own notes) — this respelling is offered as the always-available fallback, not as evidence the band member is permanently unlicensable.

**What the sources leave unstated or ambiguous here**

- Built-status: measured at HEAD (commit e1a14d91, 2026-07-21) via `Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll)` — a state entry hook's pre-verb `when` clause mints no fault obligation at all. Base, both discharge additions, and both near-misses all compile clean (rc=0, no diagnostic touching this fault) regardless of which addition is present. This is the site failing to mint, not the additions being recognized or the near-misses failing to be caught — see each witness's own notes for the literal compile output.
- Open question, not licensed here: the natural author spelling `when Items.count > 0 and Items.first > 10` puts both facts in the SAME guard. Class (c) — the handler's own guard — is unavailable at every guard position (a guard cannot supply a premise to an obligation arising inside itself), but whether an EARLIER conjunct of that same guard licenses a LATER one is not answered by the matrix or by `precept-language-spec.md`. This cell does not license that spelling; the band-member respelling above is offered as the licensed alternative, not as tacit permission for the self-referential form.
- Class (d) is listed as available at this evaluation-site category (constraints holding in the pre-state), so a discharge witness citing a `rule` is authored here. Per the group's hazard instruction, every such witness is recorded `builtStatus: unresolved-today` with the false-proof reason named — never as confirmed by a clean compile.
- No validity argument in the matrix's § Validity arguments literally covers premise class (a) — a collection field's own declared `mincount` modifier, as opposed to an event arg's modifier — discharging a fault-family obligation. 'Arg-bound interval arithmetic' is scoped in its own text to values 'entering as an event arg' (want:268/ingress governance); it does not by its own words extend to a persistent field's structurally-enforced bound. The closer fit is 'Inductive hypothesis plus sign monotonicity' — since every ProofSatisfaction-bearing modifier desugars to an implicit rule (`DesugarsToRule: true`, `Modifiers.cs`) under the SAME establishment+preservation obligation machinery as an author-written rule — but that argument's own worked text is about combining the hypothesis with a sign-monotonicity step (Family 1 Base B), not the direct-identity case used here. Cited as the closest available named argument; the generalization to synthesized field-modifier rules, and to the trivial (no-arithmetic) case, is not itself written out. Recorded as a missing rule, not silently absorbed by the citation.
- The fault-family case-shape table (matrix, the per-family case-shapes table) lists premise classes (b)/(c)/(a) for the fault family and does not list (d), yet this cell (per the group's own authoring instruction and the region's axis facts) authors a class-(d) discharge, and Defect B independently shows class (d) being consumed for a fault discharge at HEAD via CompositionalConstraint. Whether (d) is a licensed fault-discharge premise class at all is not stated cleanly in the matrix text — flagged as a missing rule, distinct from (and prior to) the false-proof soundness hazard Defect B documents.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: fault family case shape — catalog-declared safety precondition at the evaluation site
- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: the four premise classes
- docs/language/precept-language-spec.md § State action — spec
- src/Precept/Language/Types.cs:297 — code: list.first's catalog-declared Numeric requirement
- src/Precept/Language/Diagnostics.cs:568 — code: UnguardedCollectionAccess — PreventsFault CollectionEmptyOnAccess, the write-site analogue of this obligation

## g14/list-first-access-mode-guard — List.first read inside a state-scoped access-mode declaration's pre-verb `when` clause

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: fault family case shape
- docs/language/precept-language-spec.md § Access mode and omit declaration — spec

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | accessor/List/first/numeric-0 |
| evaluation site category | access-mode-guard |
| type family | collection |

### What must be proven

Obligation: Items.count > 0

Weakest precondition: Items.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Items | the list field the accessor reads |
| 0 | the fixed non-emptiness threshold — the catalog's declared condition, never authored |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a), (d).

No write occurs at a state-scoped access-mode declaration's pre-verb `when` clause (it decides applicability/writability; it does not mutate). Class (c) is unavailable — the guard cannot supply a premise to an obligation arising inside itself, the headline asymmetry of this evaluation-site region. Class (b) is unavailable — no event argument represents the collection's cardinality, so no arg constraint can instantiate this obligation. Class (a) (the field's own declared `mincount`) is always in scope. Class (d) is available at this category (pre-state constraints), per the evaluation-site region's own axis facts.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: compile-time interval fact from `Items`'s declared `mincount` modifier (ProofSatisfaction.Numeric: `count >= mincount`, `src/Precept/Language/Modifiers.cs:195-208`) — a declared `mincount` of 1 or more entails `Items.count > 0`, the catalog's own fault precondition (`src/Precept/Language/Types.cs:297`)
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Look up `Items`'s declared `mincount` value, if any, via the ProofSatisfactions table; the fault discharges iff the declared `mincount` is >= 1 (for an integer count, `count >= 1` entails `count > 0`). No `mincount` modifier, or one declaring 0, fails the class.

- src/Precept/Language/Modifiers.cs:195 — code: Mincount ProofSatisfaction — count >= DeclarationValue
- src/Precept/Language/Types.cs:297 — code: list.first's catalog-declared Numeric requirement — List must be non-empty
- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: fault-family case shape: premise classes (b)/(c)/(a) per catalog ProofSatisfactions

**Entry 2 — (d)**

- Derivation: a `rule Items.count > 0` (or an equivalent lower-bound form) holding in the pre-state, consumed directly as the fault's WP with no arithmetic — the same fact, not a derived one
- Strategy: CompositionalConstraint
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Search the pre-state constraint set for a `rule` whose condition is normal-form-equal to (or N13-covers) `Items.count > 0`; found, consumed via CompositionalConstraint. Absent, or naming a different field, fails the class.

- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: premise class (d): all constraints holding in the pre-state
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Inductive hypothesis plus sign monotonicity

### What the failing diagnostic must suggest

- For class (a): apply a `mincount` modifier (at least 1) to the collection field's declaration
  - src/Precept/Language/Modifiers.cs:125 — code: the Notempty HoverDescription names mincount 1, not notempty, as the collection route

- For class (d): add a `rule <WP>` (e.g. `rule Items.count > 0`) holding in the pre-state
  - This suggestion is licensed under the model. At HEAD it is also the exact shape of the Defect B false-proof hazard (authored-expressiveness-gaps.md, Part 3): a rule consumed as a premise while nothing yet establishes or preserves it. Recorded as the model's suggestion; a diagnostic emitting it today would be recommending a currently-unsound spelling. Not resolved here.


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept ListFirstAccessmodeguard
field Items as list of integer
field Note as string optional editable
state Draft initial
state Done terminal
event Add(Value as integer)
event Finish
from Draft on Add
    -> append Items Add.Value
    -> no transition
in Draft when Items.first > 10 modify Note readonly
from Draft on Finish
    -> transition Done
```

Required outcome: reject, naming the missing premise classes (a), (d), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*mincount-1* — other

- Addition: mincount 1  # added to the `Items` field declaration
- Premise classes: (a)
- Derivation: declared mincount 1 -> count >= 1 -> count > 0 (Inductive hypothesis plus sign monotonicity, trivial/identity case)
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (the access-mode-guard position mints no fault obligation at all at HEAD (measured, commit e1a14d91) — there is nothing for this addition to discharge; the clean compile below is the site failing to mint, not the addition being recognized as a discharge)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live run (2026-07-21, Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), HEAD e1a14d91): `list-access-mode-guard__discharge-a.precept` compiles with rc=0 and no diagnostics (stdout carries only the WP calculator's own '[skipped obligation] ...mincount: accessor-projected bound...' note, which is about the calculator's inability to compute an establishment/preservation WP for the mincount-desugared rule — unrelated to the fault obligation this cell defines).
- `application` is omitted: this addition changes a field declaration's modifier list, which is neither a row guard (`row-guard` locus) nor an event-arg declaration (`event-arg-declarations` locus) — the schema's `application` DU has no locus for a field-modifier addition. The cell->test conversion machinery cannot mechanically apply this discharge today; flagged as a schema/tooling gap, not worked around.

*rule-count-gt-0* — other

- Addition: rule Items.count > 0 because "the collection is provisioned non-empty at handoff"  # added as a top-level declaration
- Premise classes: (d)
- Derivation: pre-state rule Items.count > 0 consumed directly as the WP -> CompositionalConstraint
- Strategy: CompositionalConstraint
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (the access-mode-guard position mints no fault obligation at all at HEAD (measured, commit e1a14d91) — there is nothing for this addition to discharge. Separately and more seriously: even at a position that DOES mint, Defect B shows a `rule` consumed as a premise via CompositionalConstraint while nothing establishes or preserves it is a live false-proof at HEAD (authored-expressiveness-gaps.md, Part 3) — so this discharge must never be read as confirmed by a clean compile, here or elsewhere.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live run (2026-07-21, Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), HEAD e1a14d91): `list-access-mode-guard__discharge-d.precept` compiles with rc=0 and no diagnostics related to the target fault (stdout shows the WP calculator's own printout for the added `rule` obligation itself — establishment WP and a 'NOT SUPPORTED' preservation note for the append action, both about the calculator's own rule-write machinery, not the fault obligation this cell defines).
- `application` is omitted: this addition is a new top-level `rule` declaration, not a row guard or an event-arg declaration — the same schema/tooling gap as the mincount discharge above.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| mincount-1 | mincount 0  # added to the `Items` field declaration | mincount 0 does not exclude an empty collection — the compile-time lower bound is 0, not >= 1, so it still admits the empty case; must still reject under the model, same obligation. | reject, naming the same obligation |
| rule-count-gt-0 | rule Items.count >= 0 because "..."  # weakened top-level declaration | `>= 0` is trivially true of any count and does not establish non-emptiness; must still reject under the model, same obligation. | reject, naming the same obligation |

### Respellability

- Resolution: cell-override
- A representative safe-but-rejected spelling: in Draft when Items.count > 0 and Items.first > 10 modify Note readonly
- Its licensed respelling: declare `Items` with `mincount 1` (class a), or state a top-level `rule Items.count > 0` that the induction consumes once the rule-write establishment/preservation machinery is built (class d) — both close the fault without relying on same-guard conjunct ordering, which the matrix does not license (see notes).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the respellable verdict definition

- The band member is the natural author spelling for this group: a single guard combining the cardinality check and the accessor read in one `and`-joined condition. Whether the first conjunct (`count > 0`) is itself a licensed premise for the second (`.first`/`.peek` above threshold) is an open question the matrix does not answer (see the cell's own notes) — this respelling is offered as the always-available fallback, not as evidence the band member is permanently unlicensable.

**What the sources leave unstated or ambiguous here**

- Built-status: measured at HEAD (commit e1a14d91, 2026-07-21) via `Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll)` — a state-scoped access-mode declaration's pre-verb `when` clause mints no fault obligation at all. Base, both discharge additions, and both near-misses all compile clean (rc=0, no diagnostic touching this fault) regardless of which addition is present. This is the site failing to mint, not the additions being recognized or the near-misses failing to be caught — see each witness's own notes for the literal compile output.
- Open question, not licensed here: the natural author spelling `when Items.count > 0 and Items.first > 10` puts both facts in the SAME guard. Class (c) — the handler's own guard — is unavailable at every guard position (a guard cannot supply a premise to an obligation arising inside itself), but whether an EARLIER conjunct of that same guard licenses a LATER one is not answered by the matrix or by `precept-language-spec.md`. This cell does not license that spelling; the band-member respelling above is offered as the licensed alternative, not as tacit permission for the self-referential form.
- Class (d) is listed as available at this evaluation-site category (constraints holding in the pre-state), so a discharge witness citing a `rule` is authored here. Per the group's hazard instruction, every such witness is recorded `builtStatus: unresolved-today` with the false-proof reason named — never as confirmed by a clean compile.
- No validity argument in the matrix's § Validity arguments literally covers premise class (a) — a collection field's own declared `mincount` modifier, as opposed to an event arg's modifier — discharging a fault-family obligation. 'Arg-bound interval arithmetic' is scoped in its own text to values 'entering as an event arg' (want:268/ingress governance); it does not by its own words extend to a persistent field's structurally-enforced bound. The closer fit is 'Inductive hypothesis plus sign monotonicity' — since every ProofSatisfaction-bearing modifier desugars to an implicit rule (`DesugarsToRule: true`, `Modifiers.cs`) under the SAME establishment+preservation obligation machinery as an author-written rule — but that argument's own worked text is about combining the hypothesis with a sign-monotonicity step (Family 1 Base B), not the direct-identity case used here. Cited as the closest available named argument; the generalization to synthesized field-modifier rules, and to the trivial (no-arithmetic) case, is not itself written out. Recorded as a missing rule, not silently absorbed by the citation.
- The fault-family case-shape table (matrix, the per-family case-shapes table) lists premise classes (b)/(c)/(a) for the fault family and does not list (d), yet this cell (per the group's own authoring instruction and the region's axis facts) authors a class-(d) discharge, and Defect B independently shows class (d) being consumed for a fault discharge at HEAD via CompositionalConstraint. Whether (d) is a licensed fault-discharge premise class at all is not stated cleanly in the matrix text — flagged as a missing rule, distinct from (and prior to) the false-proof soundness hazard Defect B documents.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: fault family case shape — catalog-declared safety precondition at the evaluation site
- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: the four premise classes
- docs/language/precept-language-spec.md § Access mode and omit declaration — spec
- src/Precept/Language/Types.cs:297 — code: list.first's catalog-declared Numeric requirement
- src/Precept/Language/Diagnostics.cs:568 — code: UnguardedCollectionAccess — PreventsFault CollectionEmptyOnAccess, the write-site analogue of this obligation

## g14/list-first-ensure-activation-guard — List.first read inside an ensure's pre-verb `when` clause

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: fault family case shape
- docs/language/precept-language-spec.md § State/event ensure — spec

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | accessor/List/first/numeric-0 |
| evaluation site category | ensure-activation-guard |
| type family | collection |

### What must be proven

Obligation: Items.count > 0

Weakest precondition: Items.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Items | the list field the accessor reads |
| 0 | the fixed non-emptiness threshold — the catalog's declared condition, never authored |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a), (d).

No write occurs at an ensure's pre-verb `when` clause (it decides applicability/writability; it does not mutate). Class (c) is unavailable — the guard cannot supply a premise to an obligation arising inside itself, the headline asymmetry of this evaluation-site region. Class (b) is unavailable — no event argument represents the collection's cardinality, so no arg constraint can instantiate this obligation. Class (a) (the field's own declared `mincount`) is always in scope. Class (d) is available at this category (pre-state constraints), per the evaluation-site region's own axis facts.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: compile-time interval fact from `Items`'s declared `mincount` modifier (ProofSatisfaction.Numeric: `count >= mincount`, `src/Precept/Language/Modifiers.cs:195-208`) — a declared `mincount` of 1 or more entails `Items.count > 0`, the catalog's own fault precondition (`src/Precept/Language/Types.cs:297`)
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Look up `Items`'s declared `mincount` value, if any, via the ProofSatisfactions table; the fault discharges iff the declared `mincount` is >= 1 (for an integer count, `count >= 1` entails `count > 0`). No `mincount` modifier, or one declaring 0, fails the class.

- src/Precept/Language/Modifiers.cs:195 — code: Mincount ProofSatisfaction — count >= DeclarationValue
- src/Precept/Language/Types.cs:297 — code: list.first's catalog-declared Numeric requirement — List must be non-empty
- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: fault-family case shape: premise classes (b)/(c)/(a) per catalog ProofSatisfactions

**Entry 2 — (d)**

- Derivation: a `rule Items.count > 0` (or an equivalent lower-bound form) holding in the pre-state, consumed directly as the fault's WP with no arithmetic — the same fact, not a derived one
- Strategy: CompositionalConstraint
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Search the pre-state constraint set for a `rule` whose condition is normal-form-equal to (or N13-covers) `Items.count > 0`; found, consumed via CompositionalConstraint. Absent, or naming a different field, fails the class.

- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: premise class (d): all constraints holding in the pre-state
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Inductive hypothesis plus sign monotonicity

### What the failing diagnostic must suggest

- For class (a): apply a `mincount` modifier (at least 1) to the collection field's declaration
  - src/Precept/Language/Modifiers.cs:125 — code: the Notempty HoverDescription names mincount 1, not notempty, as the collection route

- For class (d): add a `rule <WP>` (e.g. `rule Items.count > 0`) holding in the pre-state
  - This suggestion is licensed under the model. At HEAD it is also the exact shape of the Defect B false-proof hazard (authored-expressiveness-gaps.md, Part 3): a rule consumed as a premise while nothing yet establishes or preserves it. Recorded as the model's suggestion; a diagnostic emitting it today would be recommending a currently-unsound spelling. Not resolved here.


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept ListFirstEnsureactivationguard
field Items as list of integer
field Total as decimal default 0.0 editable
state Draft initial
state Done terminal
event Add(Value as integer)
event Finish
from Draft on Add
    -> append Items Add.Value
    -> no transition
in Done when Items.first > 10 ensure Total > 0.0 because "bounded"
from Draft on Finish
    -> transition Done
```

Required outcome: reject, naming the missing premise classes (a), (d), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*mincount-1* — other

- Addition: mincount 1  # added to the `Items` field declaration
- Premise classes: (a)
- Derivation: declared mincount 1 -> count >= 1 -> count > 0 (Inductive hypothesis plus sign monotonicity, trivial/identity case)
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (the ensure-activation-guard position mints no fault obligation at all at HEAD (measured, commit e1a14d91) — there is nothing for this addition to discharge; the clean compile below is the site failing to mint, not the addition being recognized as a discharge)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live run (2026-07-21, Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), HEAD e1a14d91): `list-ensure-activation-guard__discharge-a.precept` compiles with rc=0 and no diagnostics (stdout carries only the WP calculator's own '[skipped obligation] ...mincount: accessor-projected bound...' note, which is about the calculator's inability to compute an establishment/preservation WP for the mincount-desugared rule — unrelated to the fault obligation this cell defines).
- `application` is omitted: this addition changes a field declaration's modifier list, which is neither a row guard (`row-guard` locus) nor an event-arg declaration (`event-arg-declarations` locus) — the schema's `application` DU has no locus for a field-modifier addition. The cell->test conversion machinery cannot mechanically apply this discharge today; flagged as a schema/tooling gap, not worked around.

*rule-count-gt-0* — other

- Addition: rule Items.count > 0 because "the collection is provisioned non-empty at handoff"  # added as a top-level declaration
- Premise classes: (d)
- Derivation: pre-state rule Items.count > 0 consumed directly as the WP -> CompositionalConstraint
- Strategy: CompositionalConstraint
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (the ensure-activation-guard position mints no fault obligation at all at HEAD (measured, commit e1a14d91) — there is nothing for this addition to discharge. Separately and more seriously: even at a position that DOES mint, Defect B shows a `rule` consumed as a premise via CompositionalConstraint while nothing establishes or preserves it is a live false-proof at HEAD (authored-expressiveness-gaps.md, Part 3) — so this discharge must never be read as confirmed by a clean compile, here or elsewhere.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live run (2026-07-21, Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), HEAD e1a14d91): `list-ensure-activation-guard__discharge-d.precept` compiles with rc=0 and no diagnostics related to the target fault (stdout shows the WP calculator's own printout for the added `rule` obligation itself — establishment WP and a 'NOT SUPPORTED' preservation note for the append action, both about the calculator's own rule-write machinery, not the fault obligation this cell defines).
- `application` is omitted: this addition is a new top-level `rule` declaration, not a row guard or an event-arg declaration — the same schema/tooling gap as the mincount discharge above.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| mincount-1 | mincount 0  # added to the `Items` field declaration | mincount 0 does not exclude an empty collection — the compile-time lower bound is 0, not >= 1, so it still admits the empty case; must still reject under the model, same obligation. | reject, naming the same obligation |
| rule-count-gt-0 | rule Items.count >= 0 because "..."  # weakened top-level declaration | `>= 0` is trivially true of any count and does not establish non-emptiness; must still reject under the model, same obligation. | reject, naming the same obligation |

### Respellability

- Resolution: cell-override
- A representative safe-but-rejected spelling: in Done when Items.count > 0 and Items.first > 10 ensure Total > 0.0 because "bounded"
- Its licensed respelling: declare `Items` with `mincount 1` (class a), or state a top-level `rule Items.count > 0` that the induction consumes once the rule-write establishment/preservation machinery is built (class d) — both close the fault without relying on same-guard conjunct ordering, which the matrix does not license (see notes).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the respellable verdict definition

- The band member is the natural author spelling for this group: a single guard combining the cardinality check and the accessor read in one `and`-joined condition. Whether the first conjunct (`count > 0`) is itself a licensed premise for the second (`.first`/`.peek` above threshold) is an open question the matrix does not answer (see the cell's own notes) — this respelling is offered as the always-available fallback, not as evidence the band member is permanently unlicensable.

**What the sources leave unstated or ambiguous here**

- Built-status: measured at HEAD (commit e1a14d91, 2026-07-21) via `Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll)` — an ensure's pre-verb `when` clause mints no fault obligation at all. Base, both discharge additions, and both near-misses all compile clean (rc=0, no diagnostic touching this fault) regardless of which addition is present. This is the site failing to mint, not the additions being recognized or the near-misses failing to be caught — see each witness's own notes for the literal compile output.
- Open question, not licensed here: the natural author spelling `when Items.count > 0 and Items.first > 10` puts both facts in the SAME guard. Class (c) — the handler's own guard — is unavailable at every guard position (a guard cannot supply a premise to an obligation arising inside itself), but whether an EARLIER conjunct of that same guard licenses a LATER one is not answered by the matrix or by `precept-language-spec.md`. This cell does not license that spelling; the band-member respelling above is offered as the licensed alternative, not as tacit permission for the self-referential form.
- Class (d) is listed as available at this evaluation-site category (constraints holding in the pre-state), so a discharge witness citing a `rule` is authored here. Per the group's hazard instruction, every such witness is recorded `builtStatus: unresolved-today` with the false-proof reason named — never as confirmed by a clean compile.
- No validity argument in the matrix's § Validity arguments literally covers premise class (a) — a collection field's own declared `mincount` modifier, as opposed to an event arg's modifier — discharging a fault-family obligation. 'Arg-bound interval arithmetic' is scoped in its own text to values 'entering as an event arg' (want:268/ingress governance); it does not by its own words extend to a persistent field's structurally-enforced bound. The closer fit is 'Inductive hypothesis plus sign monotonicity' — since every ProofSatisfaction-bearing modifier desugars to an implicit rule (`DesugarsToRule: true`, `Modifiers.cs`) under the SAME establishment+preservation obligation machinery as an author-written rule — but that argument's own worked text is about combining the hypothesis with a sign-monotonicity step (Family 1 Base B), not the direct-identity case used here. Cited as the closest available named argument; the generalization to synthesized field-modifier rules, and to the trivial (no-arithmetic) case, is not itself written out. Recorded as a missing rule, not silently absorbed by the citation.
- The fault-family case-shape table (matrix, the per-family case-shapes table) lists premise classes (b)/(c)/(a) for the fault family and does not list (d), yet this cell (per the group's own authoring instruction and the region's axis facts) authors a class-(d) discharge, and Defect B independently shows class (d) being consumed for a fault discharge at HEAD via CompositionalConstraint. Whether (d) is a licensed fault-discharge premise class at all is not stated cleanly in the matrix text — flagged as a missing rule, distinct from (and prior to) the false-proof soundness hazard Defect B documents.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: fault family case shape — catalog-declared safety precondition at the evaluation site
- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: the four premise classes
- docs/language/precept-language-spec.md § State/event ensure — spec
- src/Precept/Language/Types.cs:297 — code: list.first's catalog-declared Numeric requirement
- src/Precept/Language/Diagnostics.cs:568 — code: UnguardedCollectionAccess — PreventsFault CollectionEmptyOnAccess, the write-site analogue of this obligation

## g14/list-first-rule-activation-guard — List.first read inside a conditional rule's `when` clause

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: fault family case shape
- docs/language/precept-language-spec.md § rule` declaration — spec

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | accessor/List/first/numeric-0 |
| evaluation site category | rule-activation-guard |
| type family | collection |

### What must be proven

Obligation: Items.count > 0

Weakest precondition: Items.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Items | the list field the accessor reads |
| 0 | the fixed non-emptiness threshold — the catalog's declared condition, never authored |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a).

No write occurs at a conditional rule's `when` clause (it decides applicability/writability; it does not mutate). Class (c) is unavailable — the guard cannot supply a premise to an obligation arising inside itself, the headline asymmetry of this evaluation-site region. Class (b) is unavailable — no event argument represents the collection's cardinality, so no arg constraint can instantiate this obligation. Class (a) (the field's own declared `mincount`) is always in scope. Class (d) is NOT listed as available at this category, unlike the other four guard positions in this group — a rule's own activation guard has no handler guard, no event args, and (per the region's axis definition) no pre-state-constraint class either.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: compile-time interval fact from `Items`'s declared `mincount` modifier (ProofSatisfaction.Numeric: `count >= mincount`, `src/Precept/Language/Modifiers.cs:195-208`) — a declared `mincount` of 1 or more entails `Items.count > 0`, the catalog's own fault precondition (`src/Precept/Language/Types.cs:297`)
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Look up `Items`'s declared `mincount` value, if any, via the ProofSatisfactions table; the fault discharges iff the declared `mincount` is >= 1 (for an integer count, `count >= 1` entails `count > 0`). No `mincount` modifier, or one declaring 0, fails the class.

- src/Precept/Language/Modifiers.cs:195 — code: Mincount ProofSatisfaction — count >= DeclarationValue
- src/Precept/Language/Types.cs:297 — code: list.first's catalog-declared Numeric requirement — List must be non-empty
- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: fault-family case shape: premise classes (b)/(c)/(a) per catalog ProofSatisfactions

### What the failing diagnostic must suggest

- For class (a): apply a `mincount` modifier (at least 1) to the collection field's declaration
  - src/Precept/Language/Modifiers.cs:125 — code: the Notempty HoverDescription names mincount 1, not notempty, as the collection route


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept ListFirstRuleactivationguard
field Items as list of integer editable
field Total as decimal default 0.0 editable
rule Total > 0.0 when Items.first > 10 because "bounded"
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*mincount-1* — other

- Addition: mincount 1  # added to the `Items` field declaration
- Premise classes: (a)
- Derivation: declared mincount 1 -> count >= 1 -> count > 0 (Inductive hypothesis plus sign monotonicity, trivial/identity case)
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (the rule-activation-guard position mints no fault obligation at all at HEAD (measured, commit e1a14d91) — there is nothing for this addition to discharge; the clean compile below is the site failing to mint, not the addition being recognized as a discharge)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live run (2026-07-21, Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), HEAD e1a14d91): `list-rule-activation-guard__discharge-a.precept` compiles with rc=0 and no diagnostics (stdout carries only the WP calculator's own '[skipped obligation] ...mincount: accessor-projected bound...' note, which is about the calculator's inability to compute an establishment/preservation WP for the mincount-desugared rule — unrelated to the fault obligation this cell defines).
- `application` is omitted: this addition changes a field declaration's modifier list, which is neither a row guard (`row-guard` locus) nor an event-arg declaration (`event-arg-declarations` locus) — the schema's `application` DU has no locus for a field-modifier addition. The cell->test conversion machinery cannot mechanically apply this discharge today; flagged as a schema/tooling gap, not worked around.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| mincount-1 | mincount 0  # added to the `Items` field declaration | mincount 0 does not exclude an empty collection — the compile-time lower bound is 0, not >= 1, so it still admits the empty case; must still reject under the model, same obligation. | reject, naming the same obligation |

### Respellability

- Resolution: cell-override
- A representative safe-but-rejected spelling: rule Total > 0.0 when Items.count > 0 and Items.first > 10 because "bounded"
- Its licensed respelling: declare `Items` with `mincount 1` (class a), or state a top-level `rule Items.count > 0` that the induction consumes once the rule-write establishment/preservation machinery is built (class d) — both close the fault without relying on same-guard conjunct ordering, which the matrix does not license (see notes).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the respellable verdict definition

- The band member is the natural author spelling for this group: a single guard combining the cardinality check and the accessor read in one `and`-joined condition. Whether the first conjunct (`count > 0`) is itself a licensed premise for the second (`.first`/`.peek` above threshold) is an open question the matrix does not answer (see the cell's own notes) — this respelling is offered as the always-available fallback, not as evidence the band member is permanently unlicensable.

**What the sources leave unstated or ambiguous here**

- Built-status: measured at HEAD (commit e1a14d91, 2026-07-21) via `Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll)` — a conditional rule's `when` clause mints no fault obligation at all. Base, both discharge additions, and both near-misses all compile clean (rc=0, no diagnostic touching this fault) regardless of which addition is present. This is the site failing to mint, not the additions being recognized or the near-misses failing to be caught — see each witness's own notes for the literal compile output.
- Open question, not licensed here: the natural author spelling `when Items.count > 0 and Items.first > 10` puts both facts in the SAME guard. Class (c) — the handler's own guard — is unavailable at every guard position (a guard cannot supply a premise to an obligation arising inside itself), but whether an EARLIER conjunct of that same guard licenses a LATER one is not answered by the matrix or by `precept-language-spec.md`. This cell does not license that spelling; the band-member respelling above is offered as the licensed alternative, not as tacit permission for the self-referential form.
- Only premise class (a) is available at a conditional rule's own `when` clause: there is no handler guard and no event args in scope here, and — per the region's own axis definition — this category does not carry class (d) either, unlike the other four guard positions in this group. Whether a rule's own activation guard can discharge a fault obligation arising inside its own condition at all is a separate open question the matrix does not answer (recorded, not resolved, at this cell).
- No validity argument in the matrix's § Validity arguments literally covers premise class (a) — a collection field's own declared `mincount` modifier, as opposed to an event arg's modifier — discharging a fault-family obligation. 'Arg-bound interval arithmetic' is scoped in its own text to values 'entering as an event arg' (want:268/ingress governance); it does not by its own words extend to a persistent field's structurally-enforced bound. The closer fit is 'Inductive hypothesis plus sign monotonicity' — since every ProofSatisfaction-bearing modifier desugars to an implicit rule (`DesugarsToRule: true`, `Modifiers.cs`) under the SAME establishment+preservation obligation machinery as an author-written rule — but that argument's own worked text is about combining the hypothesis with a sign-monotonicity step (Family 1 Base B), not the direct-identity case used here. Cited as the closest available named argument; the generalization to synthesized field-modifier rules, and to the trivial (no-arithmetic) case, is not itself written out. Recorded as a missing rule, not silently absorbed by the citation.
- The fault-family case-shape table (matrix, the per-family case-shapes table) lists premise classes (b)/(c)/(a) for the fault family and does not list (d). This cell authors no class-(d) discharge — class (d) is recorded unavailable at a conditional rule's own activation guard — but the group's other eight cells do, and Defect B independently shows class (d) being consumed for a fault discharge at HEAD via CompositionalConstraint. Whether (d) is a licensed fault-discharge premise class at all is not stated cleanly in the matrix text — flagged as a missing rule, distinct from (and prior to) the false-proof soundness hazard Defect B documents.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: fault family case shape — catalog-declared safety precondition at the evaluation site
- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: the four premise classes
- docs/language/precept-language-spec.md § rule` declaration — spec
- src/Precept/Language/Types.cs:297 — code: list.first's catalog-declared Numeric requirement
- src/Precept/Language/Diagnostics.cs:568 — code: UnguardedCollectionAccess — PreventsFault CollectionEmptyOnAccess, the write-site analogue of this obligation

## g14/queue-peek-transition-row-guard — Queue.peek read inside a transition row's `when` clause

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: fault family case shape
- docs/language/precept-language-spec.md § Transition row — spec

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | accessor/Queue/peek/numeric-0 |
| evaluation site category | transition-row-guard |
| type family | collection |

### What must be proven

Obligation: Orders.count > 0

Weakest precondition: Orders.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Orders | the queue field the accessor reads |
| 0 | the fixed non-emptiness threshold — the catalog's declared condition, never authored |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a), (d).

No write occurs at a transition row's `when` clause (it decides applicability/writability; it does not mutate). Class (c) is unavailable — the guard cannot supply a premise to an obligation arising inside itself, the headline asymmetry of this evaluation-site region. Class (b) is unavailable — no event argument represents the collection's cardinality, so no arg constraint can instantiate this obligation. Class (a) (the field's own declared `mincount`) is always in scope. Class (d) is available at this category (pre-state constraints), per the evaluation-site region's own axis facts.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: compile-time interval fact from `Orders`'s declared `mincount` modifier (ProofSatisfaction.Numeric: `count >= mincount`, `src/Precept/Language/Modifiers.cs:195-208`) — a declared `mincount` of 1 or more entails `Orders.count > 0`, the catalog's own fault precondition (`src/Precept/Language/Types.cs:205`)
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Look up `Orders`'s declared `mincount` value, if any, via the ProofSatisfactions table; the fault discharges iff the declared `mincount` is >= 1 (for an integer count, `count >= 1` entails `count > 0`). No `mincount` modifier, or one declaring 0, fails the class.

- src/Precept/Language/Modifiers.cs:195 — code: Mincount ProofSatisfaction — count >= DeclarationValue
- src/Precept/Language/Types.cs:205 — code: queue.peek's catalog-declared Numeric requirement — Queue must be non-empty
- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: fault-family case shape: premise classes (b)/(c)/(a) per catalog ProofSatisfactions

**Entry 2 — (d)**

- Derivation: a `rule Orders.count > 0` (or an equivalent lower-bound form) holding in the pre-state, consumed directly as the fault's WP with no arithmetic — the same fact, not a derived one
- Strategy: CompositionalConstraint
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Search the pre-state constraint set for a `rule` whose condition is normal-form-equal to (or N13-covers) `Orders.count > 0`; found, consumed via CompositionalConstraint. Absent, or naming a different field, fails the class.

- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: premise class (d): all constraints holding in the pre-state
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Inductive hypothesis plus sign monotonicity

### What the failing diagnostic must suggest

- For class (a): apply a `mincount` modifier (at least 1) to the collection field's declaration
  - src/Precept/Language/Modifiers.cs:125 — code: the Notempty HoverDescription names mincount 1, not notempty, as the collection route

- For class (d): add a `rule <WP>` (e.g. `rule Orders.count > 0`) holding in the pre-state
  - This suggestion is licensed under the model. At HEAD it is also the exact shape of the Defect B false-proof hazard (authored-expressiveness-gaps.md, Part 3): a rule consumed as a premise while nothing yet establishes or preserves it. Recorded as the model's suggestion; a diagnostic emitting it today would be recommending a currently-unsound spelling. Not resolved here.


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept QueuePeekTransitionrowguard
field Orders as queue of integer
state Active initial
state Closed terminal
event Add(Value as integer)
event Check
event Close
from Active on Add
    -> enqueue Orders Add.Value
    -> no transition
from Active on Check when Orders.peek > 10
    -> no transition
from Active on Close
    -> transition Closed
```

Required outcome: reject, naming the missing premise classes (a), (d), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*mincount-1* — other

- Addition: mincount 1  # added to the `Orders` field declaration
- Premise classes: (a)
- Derivation: declared mincount 1 -> count >= 1 -> count > 0 (Inductive hypothesis plus sign monotonicity, trivial/identity case)
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (the transition-row-guard position mints no fault obligation at all at HEAD (measured, commit e1a14d91) — there is nothing for this addition to discharge; the clean compile below is the site failing to mint, not the addition being recognized as a discharge)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live run (2026-07-21, Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), HEAD e1a14d91): `queue-transition-row-guard__discharge-a.precept` compiles with rc=0 and no diagnostics (stdout carries only the WP calculator's own '[skipped obligation] ...mincount: accessor-projected bound...' note, which is about the calculator's inability to compute an establishment/preservation WP for the mincount-desugared rule — unrelated to the fault obligation this cell defines).
- `application` is omitted: this addition changes a field declaration's modifier list, which is neither a row guard (`row-guard` locus) nor an event-arg declaration (`event-arg-declarations` locus) — the schema's `application` DU has no locus for a field-modifier addition. The cell->test conversion machinery cannot mechanically apply this discharge today; flagged as a schema/tooling gap, not worked around.

*rule-count-gt-0* — other

- Addition: rule Orders.count > 0 because "the collection is provisioned non-empty at handoff"  # added as a top-level declaration
- Premise classes: (d)
- Derivation: pre-state rule Orders.count > 0 consumed directly as the WP -> CompositionalConstraint
- Strategy: CompositionalConstraint
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (the transition-row-guard position mints no fault obligation at all at HEAD (measured, commit e1a14d91) — there is nothing for this addition to discharge. Separately and more seriously: even at a position that DOES mint, Defect B shows a `rule` consumed as a premise via CompositionalConstraint while nothing establishes or preserves it is a live false-proof at HEAD (authored-expressiveness-gaps.md, Part 3) — so this discharge must never be read as confirmed by a clean compile, here or elsewhere.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live run (2026-07-21, Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), HEAD e1a14d91): `queue-transition-row-guard__discharge-d.precept` compiles with rc=0 and no diagnostics related to the target fault (stdout shows the WP calculator's own printout for the added `rule` obligation itself — establishment WP and a 'NOT SUPPORTED' preservation note for the enqueue action, both about the calculator's own rule-write machinery, not the fault obligation this cell defines).
- `application` is omitted: this addition is a new top-level `rule` declaration, not a row guard or an event-arg declaration — the same schema/tooling gap as the mincount discharge above.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| mincount-1 | mincount 0  # added to the `Orders` field declaration | mincount 0 does not exclude an empty collection — the compile-time lower bound is 0, not >= 1, so it still admits the empty case; must still reject under the model, same obligation. | reject, naming the same obligation |
| rule-count-gt-0 | rule Orders.count >= 0 because "..."  # weakened top-level declaration | `>= 0` is trivially true of any count and does not establish non-emptiness; must still reject under the model, same obligation. | reject, naming the same obligation |

### Respellability

- Resolution: cell-override
- A representative safe-but-rejected spelling: from Active on Check when Orders.count > 0 and Orders.peek > 10     -> no transition
- Its licensed respelling: declare `Orders` with `mincount 1` (class a), or state a top-level `rule Orders.count > 0` that the induction consumes once the rule-write establishment/preservation machinery is built (class d) — both close the fault without relying on same-guard conjunct ordering, which the matrix does not license (see notes).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the respellable verdict definition

- The band member is the natural author spelling for this group: a single guard combining the cardinality check and the accessor read in one `and`-joined condition. Whether the first conjunct (`count > 0`) is itself a licensed premise for the second (`.first`/`.peek` above threshold) is an open question the matrix does not answer (see the cell's own notes) — this respelling is offered as the always-available fallback, not as evidence the band member is permanently unlicensable.

**What the sources leave unstated or ambiguous here**

- Built-status: measured at HEAD (commit e1a14d91, 2026-07-21) via `Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll)` — a transition row's `when` clause mints no fault obligation at all. Base, both discharge additions, and both near-misses all compile clean (rc=0, no diagnostic touching this fault) regardless of which addition is present. This is the site failing to mint, not the additions being recognized or the near-misses failing to be caught — see each witness's own notes for the literal compile output.
- Open question, not licensed here: the natural author spelling `when Orders.count > 0 and Orders.peek > 10` puts both facts in the SAME guard. Class (c) — the handler's own guard — is unavailable at every guard position (a guard cannot supply a premise to an obligation arising inside itself), but whether an EARLIER conjunct of that same guard licenses a LATER one is not answered by the matrix or by `precept-language-spec.md`. This cell does not license that spelling; the band-member respelling above is offered as the licensed alternative, not as tacit permission for the self-referential form.
- Class (d) is listed as available at this evaluation-site category (constraints holding in the pre-state), so a discharge witness citing a `rule` is authored here. Per the group's hazard instruction, every such witness is recorded `builtStatus: unresolved-today` with the false-proof reason named — never as confirmed by a clean compile.
- No validity argument in the matrix's § Validity arguments literally covers premise class (a) — a collection field's own declared `mincount` modifier, as opposed to an event arg's modifier — discharging a fault-family obligation. 'Arg-bound interval arithmetic' is scoped in its own text to values 'entering as an event arg' (want:268/ingress governance); it does not by its own words extend to a persistent field's structurally-enforced bound. The closer fit is 'Inductive hypothesis plus sign monotonicity' — since every ProofSatisfaction-bearing modifier desugars to an implicit rule (`DesugarsToRule: true`, `Modifiers.cs`) under the SAME establishment+preservation obligation machinery as an author-written rule — but that argument's own worked text is about combining the hypothesis with a sign-monotonicity step (Family 1 Base B), not the direct-identity case used here. Cited as the closest available named argument; the generalization to synthesized field-modifier rules, and to the trivial (no-arithmetic) case, is not itself written out. Recorded as a missing rule, not silently absorbed by the citation.
- The fault-family case-shape table (matrix, the per-family case-shapes table) lists premise classes (b)/(c)/(a) for the fault family and does not list (d), yet this cell (per the group's own authoring instruction and the region's axis facts) authors a class-(d) discharge, and Defect B independently shows class (d) being consumed for a fault discharge at HEAD via CompositionalConstraint. Whether (d) is a licensed fault-discharge premise class at all is not stated cleanly in the matrix text — flagged as a missing rule, distinct from (and prior to) the false-proof soundness hazard Defect B documents.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: fault family case shape — catalog-declared safety precondition at the evaluation site
- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: the four premise classes
- docs/language/precept-language-spec.md § Transition row — spec
- src/Precept/Language/Types.cs:205 — code: queue.peek's catalog-declared Numeric requirement
- src/Precept/Language/Diagnostics.cs:568 — code: UnguardedCollectionAccess — PreventsFault CollectionEmptyOnAccess, the write-site analogue of this obligation

## g14/queue-peek-state-hook-guard — Queue.peek read inside a state entry hook's pre-verb `when` clause

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: fault family case shape
- docs/language/precept-language-spec.md § State action — spec

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | accessor/Queue/peek/numeric-0 |
| evaluation site category | state-hook-guard |
| type family | collection |

### What must be proven

Obligation: Orders.count > 0

Weakest precondition: Orders.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Orders | the queue field the accessor reads |
| 0 | the fixed non-emptiness threshold — the catalog's declared condition, never authored |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a), (d).

No write occurs at a state entry hook's pre-verb `when` clause (it decides applicability/writability; it does not mutate). Class (c) is unavailable — the guard cannot supply a premise to an obligation arising inside itself, the headline asymmetry of this evaluation-site region. Class (b) is unavailable — no event argument represents the collection's cardinality, so no arg constraint can instantiate this obligation. Class (a) (the field's own declared `mincount`) is always in scope. Class (d) is available at this category (pre-state constraints), per the evaluation-site region's own axis facts.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: compile-time interval fact from `Orders`'s declared `mincount` modifier (ProofSatisfaction.Numeric: `count >= mincount`, `src/Precept/Language/Modifiers.cs:195-208`) — a declared `mincount` of 1 or more entails `Orders.count > 0`, the catalog's own fault precondition (`src/Precept/Language/Types.cs:205`)
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Look up `Orders`'s declared `mincount` value, if any, via the ProofSatisfactions table; the fault discharges iff the declared `mincount` is >= 1 (for an integer count, `count >= 1` entails `count > 0`). No `mincount` modifier, or one declaring 0, fails the class.

- src/Precept/Language/Modifiers.cs:195 — code: Mincount ProofSatisfaction — count >= DeclarationValue
- src/Precept/Language/Types.cs:205 — code: queue.peek's catalog-declared Numeric requirement — Queue must be non-empty
- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: fault-family case shape: premise classes (b)/(c)/(a) per catalog ProofSatisfactions

**Entry 2 — (d)**

- Derivation: a `rule Orders.count > 0` (or an equivalent lower-bound form) holding in the pre-state, consumed directly as the fault's WP with no arithmetic — the same fact, not a derived one
- Strategy: CompositionalConstraint
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Search the pre-state constraint set for a `rule` whose condition is normal-form-equal to (or N13-covers) `Orders.count > 0`; found, consumed via CompositionalConstraint. Absent, or naming a different field, fails the class.

- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: premise class (d): all constraints holding in the pre-state
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Inductive hypothesis plus sign monotonicity

### What the failing diagnostic must suggest

- For class (a): apply a `mincount` modifier (at least 1) to the collection field's declaration
  - src/Precept/Language/Modifiers.cs:125 — code: the Notempty HoverDescription names mincount 1, not notempty, as the collection route

- For class (d): add a `rule <WP>` (e.g. `rule Orders.count > 0`) holding in the pre-state
  - This suggestion is licensed under the model. At HEAD it is also the exact shape of the Defect B false-proof hazard (authored-expressiveness-gaps.md, Part 3): a rule consumed as a premise while nothing yet establishes or preserves it. Recorded as the model's suggestion; a diagnostic emitting it today would be recommending a currently-unsound spelling. Not resolved here.


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept QueuePeekStatehookguard
field Orders as queue of integer
field Note as integer default 0
state Draft initial
state Done terminal
event Add(Value as integer)
event Finish
from Draft on Add
    -> enqueue Orders Add.Value
    -> no transition
to Done when Orders.peek > 10 -> set Note = 1
from Draft on Finish
    -> transition Done
```

Required outcome: reject, naming the missing premise classes (a), (d), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*mincount-1* — other

- Addition: mincount 1  # added to the `Orders` field declaration
- Premise classes: (a)
- Derivation: declared mincount 1 -> count >= 1 -> count > 0 (Inductive hypothesis plus sign monotonicity, trivial/identity case)
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (the state-hook-guard position mints no fault obligation at all at HEAD (measured, commit e1a14d91) — there is nothing for this addition to discharge; the clean compile below is the site failing to mint, not the addition being recognized as a discharge)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live run (2026-07-21, Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), HEAD e1a14d91): `queue-state-hook-guard__discharge-a.precept` compiles with rc=0 and no diagnostics (stdout carries only the WP calculator's own '[skipped obligation] ...mincount: accessor-projected bound...' note, which is about the calculator's inability to compute an establishment/preservation WP for the mincount-desugared rule — unrelated to the fault obligation this cell defines).
- `application` is omitted: this addition changes a field declaration's modifier list, which is neither a row guard (`row-guard` locus) nor an event-arg declaration (`event-arg-declarations` locus) — the schema's `application` DU has no locus for a field-modifier addition. The cell->test conversion machinery cannot mechanically apply this discharge today; flagged as a schema/tooling gap, not worked around.

*rule-count-gt-0* — other

- Addition: rule Orders.count > 0 because "the collection is provisioned non-empty at handoff"  # added as a top-level declaration
- Premise classes: (d)
- Derivation: pre-state rule Orders.count > 0 consumed directly as the WP -> CompositionalConstraint
- Strategy: CompositionalConstraint
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (the state-hook-guard position mints no fault obligation at all at HEAD (measured, commit e1a14d91) — there is nothing for this addition to discharge. Separately and more seriously: even at a position that DOES mint, Defect B shows a `rule` consumed as a premise via CompositionalConstraint while nothing establishes or preserves it is a live false-proof at HEAD (authored-expressiveness-gaps.md, Part 3) — so this discharge must never be read as confirmed by a clean compile, here or elsewhere.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live run (2026-07-21, Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), HEAD e1a14d91): `queue-state-hook-guard__discharge-d.precept` compiles with rc=0 and no diagnostics related to the target fault (stdout shows the WP calculator's own printout for the added `rule` obligation itself — establishment WP and a 'NOT SUPPORTED' preservation note for the enqueue action, both about the calculator's own rule-write machinery, not the fault obligation this cell defines).
- `application` is omitted: this addition is a new top-level `rule` declaration, not a row guard or an event-arg declaration — the same schema/tooling gap as the mincount discharge above.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| mincount-1 | mincount 0  # added to the `Orders` field declaration | mincount 0 does not exclude an empty collection — the compile-time lower bound is 0, not >= 1, so it still admits the empty case; must still reject under the model, same obligation. | reject, naming the same obligation |
| rule-count-gt-0 | rule Orders.count >= 0 because "..."  # weakened top-level declaration | `>= 0` is trivially true of any count and does not establish non-emptiness; must still reject under the model, same obligation. | reject, naming the same obligation |

### Respellability

- Resolution: cell-override
- A representative safe-but-rejected spelling: to Done when Orders.count > 0 and Orders.peek > 10 -> set Note = 1
- Its licensed respelling: declare `Orders` with `mincount 1` (class a), or state a top-level `rule Orders.count > 0` that the induction consumes once the rule-write establishment/preservation machinery is built (class d) — both close the fault without relying on same-guard conjunct ordering, which the matrix does not license (see notes).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the respellable verdict definition

- The band member is the natural author spelling for this group: a single guard combining the cardinality check and the accessor read in one `and`-joined condition. Whether the first conjunct (`count > 0`) is itself a licensed premise for the second (`.first`/`.peek` above threshold) is an open question the matrix does not answer (see the cell's own notes) — this respelling is offered as the always-available fallback, not as evidence the band member is permanently unlicensable.

**What the sources leave unstated or ambiguous here**

- Built-status: measured at HEAD (commit e1a14d91, 2026-07-21) via `Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll)` — a state entry hook's pre-verb `when` clause mints no fault obligation at all. Base, both discharge additions, and both near-misses all compile clean (rc=0, no diagnostic touching this fault) regardless of which addition is present. This is the site failing to mint, not the additions being recognized or the near-misses failing to be caught — see each witness's own notes for the literal compile output.
- Open question, not licensed here: the natural author spelling `when Orders.count > 0 and Orders.peek > 10` puts both facts in the SAME guard. Class (c) — the handler's own guard — is unavailable at every guard position (a guard cannot supply a premise to an obligation arising inside itself), but whether an EARLIER conjunct of that same guard licenses a LATER one is not answered by the matrix or by `precept-language-spec.md`. This cell does not license that spelling; the band-member respelling above is offered as the licensed alternative, not as tacit permission for the self-referential form.
- Class (d) is listed as available at this evaluation-site category (constraints holding in the pre-state), so a discharge witness citing a `rule` is authored here. Per the group's hazard instruction, every such witness is recorded `builtStatus: unresolved-today` with the false-proof reason named — never as confirmed by a clean compile.
- No validity argument in the matrix's § Validity arguments literally covers premise class (a) — a collection field's own declared `mincount` modifier, as opposed to an event arg's modifier — discharging a fault-family obligation. 'Arg-bound interval arithmetic' is scoped in its own text to values 'entering as an event arg' (want:268/ingress governance); it does not by its own words extend to a persistent field's structurally-enforced bound. The closer fit is 'Inductive hypothesis plus sign monotonicity' — since every ProofSatisfaction-bearing modifier desugars to an implicit rule (`DesugarsToRule: true`, `Modifiers.cs`) under the SAME establishment+preservation obligation machinery as an author-written rule — but that argument's own worked text is about combining the hypothesis with a sign-monotonicity step (Family 1 Base B), not the direct-identity case used here. Cited as the closest available named argument; the generalization to synthesized field-modifier rules, and to the trivial (no-arithmetic) case, is not itself written out. Recorded as a missing rule, not silently absorbed by the citation.
- The fault-family case-shape table (matrix, the per-family case-shapes table) lists premise classes (b)/(c)/(a) for the fault family and does not list (d), yet this cell (per the group's own authoring instruction and the region's axis facts) authors a class-(d) discharge, and Defect B independently shows class (d) being consumed for a fault discharge at HEAD via CompositionalConstraint. Whether (d) is a licensed fault-discharge premise class at all is not stated cleanly in the matrix text — flagged as a missing rule, distinct from (and prior to) the false-proof soundness hazard Defect B documents.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: fault family case shape — catalog-declared safety precondition at the evaluation site
- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: the four premise classes
- docs/language/precept-language-spec.md § State action — spec
- src/Precept/Language/Types.cs:205 — code: queue.peek's catalog-declared Numeric requirement
- src/Precept/Language/Diagnostics.cs:568 — code: UnguardedCollectionAccess — PreventsFault CollectionEmptyOnAccess, the write-site analogue of this obligation

## g14/queue-peek-access-mode-guard — Queue.peek read inside a state-scoped access-mode declaration's pre-verb `when` clause

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: fault family case shape
- docs/language/precept-language-spec.md § Access mode and omit declaration — spec

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | accessor/Queue/peek/numeric-0 |
| evaluation site category | access-mode-guard |
| type family | collection |

### What must be proven

Obligation: Orders.count > 0

Weakest precondition: Orders.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Orders | the queue field the accessor reads |
| 0 | the fixed non-emptiness threshold — the catalog's declared condition, never authored |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a), (d).

No write occurs at a state-scoped access-mode declaration's pre-verb `when` clause (it decides applicability/writability; it does not mutate). Class (c) is unavailable — the guard cannot supply a premise to an obligation arising inside itself, the headline asymmetry of this evaluation-site region. Class (b) is unavailable — no event argument represents the collection's cardinality, so no arg constraint can instantiate this obligation. Class (a) (the field's own declared `mincount`) is always in scope. Class (d) is available at this category (pre-state constraints), per the evaluation-site region's own axis facts.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: compile-time interval fact from `Orders`'s declared `mincount` modifier (ProofSatisfaction.Numeric: `count >= mincount`, `src/Precept/Language/Modifiers.cs:195-208`) — a declared `mincount` of 1 or more entails `Orders.count > 0`, the catalog's own fault precondition (`src/Precept/Language/Types.cs:205`)
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Look up `Orders`'s declared `mincount` value, if any, via the ProofSatisfactions table; the fault discharges iff the declared `mincount` is >= 1 (for an integer count, `count >= 1` entails `count > 0`). No `mincount` modifier, or one declaring 0, fails the class.

- src/Precept/Language/Modifiers.cs:195 — code: Mincount ProofSatisfaction — count >= DeclarationValue
- src/Precept/Language/Types.cs:205 — code: queue.peek's catalog-declared Numeric requirement — Queue must be non-empty
- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: fault-family case shape: premise classes (b)/(c)/(a) per catalog ProofSatisfactions

**Entry 2 — (d)**

- Derivation: a `rule Orders.count > 0` (or an equivalent lower-bound form) holding in the pre-state, consumed directly as the fault's WP with no arithmetic — the same fact, not a derived one
- Strategy: CompositionalConstraint
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Search the pre-state constraint set for a `rule` whose condition is normal-form-equal to (or N13-covers) `Orders.count > 0`; found, consumed via CompositionalConstraint. Absent, or naming a different field, fails the class.

- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: premise class (d): all constraints holding in the pre-state
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Inductive hypothesis plus sign monotonicity

### What the failing diagnostic must suggest

- For class (a): apply a `mincount` modifier (at least 1) to the collection field's declaration
  - src/Precept/Language/Modifiers.cs:125 — code: the Notempty HoverDescription names mincount 1, not notempty, as the collection route

- For class (d): add a `rule <WP>` (e.g. `rule Orders.count > 0`) holding in the pre-state
  - This suggestion is licensed under the model. At HEAD it is also the exact shape of the Defect B false-proof hazard (authored-expressiveness-gaps.md, Part 3): a rule consumed as a premise while nothing yet establishes or preserves it. Recorded as the model's suggestion; a diagnostic emitting it today would be recommending a currently-unsound spelling. Not resolved here.


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept QueuePeekAccessmodeguard
field Orders as queue of integer
field Note as string optional editable
state Draft initial
state Done terminal
event Add(Value as integer)
event Finish
from Draft on Add
    -> enqueue Orders Add.Value
    -> no transition
in Draft when Orders.peek > 10 modify Note readonly
from Draft on Finish
    -> transition Done
```

Required outcome: reject, naming the missing premise classes (a), (d), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*mincount-1* — other

- Addition: mincount 1  # added to the `Orders` field declaration
- Premise classes: (a)
- Derivation: declared mincount 1 -> count >= 1 -> count > 0 (Inductive hypothesis plus sign monotonicity, trivial/identity case)
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (the access-mode-guard position mints no fault obligation at all at HEAD (measured, commit e1a14d91) — there is nothing for this addition to discharge; the clean compile below is the site failing to mint, not the addition being recognized as a discharge)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live run (2026-07-21, Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), HEAD e1a14d91): `queue-access-mode-guard__discharge-a.precept` compiles with rc=0 and no diagnostics (stdout carries only the WP calculator's own '[skipped obligation] ...mincount: accessor-projected bound...' note, which is about the calculator's inability to compute an establishment/preservation WP for the mincount-desugared rule — unrelated to the fault obligation this cell defines).
- `application` is omitted: this addition changes a field declaration's modifier list, which is neither a row guard (`row-guard` locus) nor an event-arg declaration (`event-arg-declarations` locus) — the schema's `application` DU has no locus for a field-modifier addition. The cell->test conversion machinery cannot mechanically apply this discharge today; flagged as a schema/tooling gap, not worked around.

*rule-count-gt-0* — other

- Addition: rule Orders.count > 0 because "the collection is provisioned non-empty at handoff"  # added as a top-level declaration
- Premise classes: (d)
- Derivation: pre-state rule Orders.count > 0 consumed directly as the WP -> CompositionalConstraint
- Strategy: CompositionalConstraint
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (the access-mode-guard position mints no fault obligation at all at HEAD (measured, commit e1a14d91) — there is nothing for this addition to discharge. Separately and more seriously: even at a position that DOES mint, Defect B shows a `rule` consumed as a premise via CompositionalConstraint while nothing establishes or preserves it is a live false-proof at HEAD (authored-expressiveness-gaps.md, Part 3) — so this discharge must never be read as confirmed by a clean compile, here or elsewhere.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live run (2026-07-21, Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), HEAD e1a14d91): `queue-access-mode-guard__discharge-d.precept` compiles with rc=0 and no diagnostics related to the target fault (stdout shows the WP calculator's own printout for the added `rule` obligation itself — establishment WP and a 'NOT SUPPORTED' preservation note for the enqueue action, both about the calculator's own rule-write machinery, not the fault obligation this cell defines).
- `application` is omitted: this addition is a new top-level `rule` declaration, not a row guard or an event-arg declaration — the same schema/tooling gap as the mincount discharge above.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| mincount-1 | mincount 0  # added to the `Orders` field declaration | mincount 0 does not exclude an empty collection — the compile-time lower bound is 0, not >= 1, so it still admits the empty case; must still reject under the model, same obligation. | reject, naming the same obligation |
| rule-count-gt-0 | rule Orders.count >= 0 because "..."  # weakened top-level declaration | `>= 0` is trivially true of any count and does not establish non-emptiness; must still reject under the model, same obligation. | reject, naming the same obligation |

### Respellability

- Resolution: cell-override
- A representative safe-but-rejected spelling: in Draft when Orders.count > 0 and Orders.peek > 10 modify Note readonly
- Its licensed respelling: declare `Orders` with `mincount 1` (class a), or state a top-level `rule Orders.count > 0` that the induction consumes once the rule-write establishment/preservation machinery is built (class d) — both close the fault without relying on same-guard conjunct ordering, which the matrix does not license (see notes).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the respellable verdict definition

- The band member is the natural author spelling for this group: a single guard combining the cardinality check and the accessor read in one `and`-joined condition. Whether the first conjunct (`count > 0`) is itself a licensed premise for the second (`.first`/`.peek` above threshold) is an open question the matrix does not answer (see the cell's own notes) — this respelling is offered as the always-available fallback, not as evidence the band member is permanently unlicensable.

**What the sources leave unstated or ambiguous here**

- Built-status: measured at HEAD (commit e1a14d91, 2026-07-21) via `Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll)` — a state-scoped access-mode declaration's pre-verb `when` clause mints no fault obligation at all. Base, both discharge additions, and both near-misses all compile clean (rc=0, no diagnostic touching this fault) regardless of which addition is present. This is the site failing to mint, not the additions being recognized or the near-misses failing to be caught — see each witness's own notes for the literal compile output.
- Open question, not licensed here: the natural author spelling `when Orders.count > 0 and Orders.peek > 10` puts both facts in the SAME guard. Class (c) — the handler's own guard — is unavailable at every guard position (a guard cannot supply a premise to an obligation arising inside itself), but whether an EARLIER conjunct of that same guard licenses a LATER one is not answered by the matrix or by `precept-language-spec.md`. This cell does not license that spelling; the band-member respelling above is offered as the licensed alternative, not as tacit permission for the self-referential form.
- Class (d) is listed as available at this evaluation-site category (constraints holding in the pre-state), so a discharge witness citing a `rule` is authored here. Per the group's hazard instruction, every such witness is recorded `builtStatus: unresolved-today` with the false-proof reason named — never as confirmed by a clean compile.
- No validity argument in the matrix's § Validity arguments literally covers premise class (a) — a collection field's own declared `mincount` modifier, as opposed to an event arg's modifier — discharging a fault-family obligation. 'Arg-bound interval arithmetic' is scoped in its own text to values 'entering as an event arg' (want:268/ingress governance); it does not by its own words extend to a persistent field's structurally-enforced bound. The closer fit is 'Inductive hypothesis plus sign monotonicity' — since every ProofSatisfaction-bearing modifier desugars to an implicit rule (`DesugarsToRule: true`, `Modifiers.cs`) under the SAME establishment+preservation obligation machinery as an author-written rule — but that argument's own worked text is about combining the hypothesis with a sign-monotonicity step (Family 1 Base B), not the direct-identity case used here. Cited as the closest available named argument; the generalization to synthesized field-modifier rules, and to the trivial (no-arithmetic) case, is not itself written out. Recorded as a missing rule, not silently absorbed by the citation.
- The fault-family case-shape table (matrix, the per-family case-shapes table) lists premise classes (b)/(c)/(a) for the fault family and does not list (d), yet this cell (per the group's own authoring instruction and the region's axis facts) authors a class-(d) discharge, and Defect B independently shows class (d) being consumed for a fault discharge at HEAD via CompositionalConstraint. Whether (d) is a licensed fault-discharge premise class at all is not stated cleanly in the matrix text — flagged as a missing rule, distinct from (and prior to) the false-proof soundness hazard Defect B documents.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: fault family case shape — catalog-declared safety precondition at the evaluation site
- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: the four premise classes
- docs/language/precept-language-spec.md § Access mode and omit declaration — spec
- src/Precept/Language/Types.cs:205 — code: queue.peek's catalog-declared Numeric requirement
- src/Precept/Language/Diagnostics.cs:568 — code: UnguardedCollectionAccess — PreventsFault CollectionEmptyOnAccess, the write-site analogue of this obligation

## g14/queue-peek-ensure-activation-guard — Queue.peek read inside an ensure's pre-verb `when` clause

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: fault family case shape
- docs/language/precept-language-spec.md § State/event ensure — spec

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | accessor/Queue/peek/numeric-0 |
| evaluation site category | ensure-activation-guard |
| type family | collection |

### What must be proven

Obligation: Orders.count > 0

Weakest precondition: Orders.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Orders | the queue field the accessor reads |
| 0 | the fixed non-emptiness threshold — the catalog's declared condition, never authored |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a), (d).

No write occurs at an ensure's pre-verb `when` clause (it decides applicability/writability; it does not mutate). Class (c) is unavailable — the guard cannot supply a premise to an obligation arising inside itself, the headline asymmetry of this evaluation-site region. Class (b) is unavailable — no event argument represents the collection's cardinality, so no arg constraint can instantiate this obligation. Class (a) (the field's own declared `mincount`) is always in scope. Class (d) is available at this category (pre-state constraints), per the evaluation-site region's own axis facts.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: compile-time interval fact from `Orders`'s declared `mincount` modifier (ProofSatisfaction.Numeric: `count >= mincount`, `src/Precept/Language/Modifiers.cs:195-208`) — a declared `mincount` of 1 or more entails `Orders.count > 0`, the catalog's own fault precondition (`src/Precept/Language/Types.cs:205`)
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Look up `Orders`'s declared `mincount` value, if any, via the ProofSatisfactions table; the fault discharges iff the declared `mincount` is >= 1 (for an integer count, `count >= 1` entails `count > 0`). No `mincount` modifier, or one declaring 0, fails the class.

- src/Precept/Language/Modifiers.cs:195 — code: Mincount ProofSatisfaction — count >= DeclarationValue
- src/Precept/Language/Types.cs:205 — code: queue.peek's catalog-declared Numeric requirement — Queue must be non-empty
- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: fault-family case shape: premise classes (b)/(c)/(a) per catalog ProofSatisfactions

**Entry 2 — (d)**

- Derivation: a `rule Orders.count > 0` (or an equivalent lower-bound form) holding in the pre-state, consumed directly as the fault's WP with no arithmetic — the same fact, not a derived one
- Strategy: CompositionalConstraint
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Search the pre-state constraint set for a `rule` whose condition is normal-form-equal to (or N13-covers) `Orders.count > 0`; found, consumed via CompositionalConstraint. Absent, or naming a different field, fails the class.

- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: premise class (d): all constraints holding in the pre-state
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Inductive hypothesis plus sign monotonicity

### What the failing diagnostic must suggest

- For class (a): apply a `mincount` modifier (at least 1) to the collection field's declaration
  - src/Precept/Language/Modifiers.cs:125 — code: the Notempty HoverDescription names mincount 1, not notempty, as the collection route

- For class (d): add a `rule <WP>` (e.g. `rule Orders.count > 0`) holding in the pre-state
  - This suggestion is licensed under the model. At HEAD it is also the exact shape of the Defect B false-proof hazard (authored-expressiveness-gaps.md, Part 3): a rule consumed as a premise while nothing yet establishes or preserves it. Recorded as the model's suggestion; a diagnostic emitting it today would be recommending a currently-unsound spelling. Not resolved here.


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept QueuePeekEnsureactivationguard
field Orders as queue of integer
field Total as decimal default 0.0 editable
state Draft initial
state Done terminal
event Add(Value as integer)
event Finish
from Draft on Add
    -> enqueue Orders Add.Value
    -> no transition
in Done when Orders.peek > 10 ensure Total > 0.0 because "bounded"
from Draft on Finish
    -> transition Done
```

Required outcome: reject, naming the missing premise classes (a), (d), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*mincount-1* — other

- Addition: mincount 1  # added to the `Orders` field declaration
- Premise classes: (a)
- Derivation: declared mincount 1 -> count >= 1 -> count > 0 (Inductive hypothesis plus sign monotonicity, trivial/identity case)
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (the ensure-activation-guard position mints no fault obligation at all at HEAD (measured, commit e1a14d91) — there is nothing for this addition to discharge; the clean compile below is the site failing to mint, not the addition being recognized as a discharge)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live run (2026-07-21, Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), HEAD e1a14d91): `queue-ensure-activation-guard__discharge-a.precept` compiles with rc=0 and no diagnostics (stdout carries only the WP calculator's own '[skipped obligation] ...mincount: accessor-projected bound...' note, which is about the calculator's inability to compute an establishment/preservation WP for the mincount-desugared rule — unrelated to the fault obligation this cell defines).
- `application` is omitted: this addition changes a field declaration's modifier list, which is neither a row guard (`row-guard` locus) nor an event-arg declaration (`event-arg-declarations` locus) — the schema's `application` DU has no locus for a field-modifier addition. The cell->test conversion machinery cannot mechanically apply this discharge today; flagged as a schema/tooling gap, not worked around.

*rule-count-gt-0* — other

- Addition: rule Orders.count > 0 because "the collection is provisioned non-empty at handoff"  # added as a top-level declaration
- Premise classes: (d)
- Derivation: pre-state rule Orders.count > 0 consumed directly as the WP -> CompositionalConstraint
- Strategy: CompositionalConstraint
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (the ensure-activation-guard position mints no fault obligation at all at HEAD (measured, commit e1a14d91) — there is nothing for this addition to discharge. Separately and more seriously: even at a position that DOES mint, Defect B shows a `rule` consumed as a premise via CompositionalConstraint while nothing establishes or preserves it is a live false-proof at HEAD (authored-expressiveness-gaps.md, Part 3) — so this discharge must never be read as confirmed by a clean compile, here or elsewhere.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live run (2026-07-21, Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), HEAD e1a14d91): `queue-ensure-activation-guard__discharge-d.precept` compiles with rc=0 and no diagnostics related to the target fault (stdout shows the WP calculator's own printout for the added `rule` obligation itself — establishment WP and a 'NOT SUPPORTED' preservation note for the enqueue action, both about the calculator's own rule-write machinery, not the fault obligation this cell defines).
- `application` is omitted: this addition is a new top-level `rule` declaration, not a row guard or an event-arg declaration — the same schema/tooling gap as the mincount discharge above.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| mincount-1 | mincount 0  # added to the `Orders` field declaration | mincount 0 does not exclude an empty collection — the compile-time lower bound is 0, not >= 1, so it still admits the empty case; must still reject under the model, same obligation. | reject, naming the same obligation |
| rule-count-gt-0 | rule Orders.count >= 0 because "..."  # weakened top-level declaration | `>= 0` is trivially true of any count and does not establish non-emptiness; must still reject under the model, same obligation. | reject, naming the same obligation |

### Respellability

- Resolution: cell-override
- A representative safe-but-rejected spelling: in Done when Orders.count > 0 and Orders.peek > 10 ensure Total > 0.0 because "bounded"
- Its licensed respelling: declare `Orders` with `mincount 1` (class a), or state a top-level `rule Orders.count > 0` that the induction consumes once the rule-write establishment/preservation machinery is built (class d) — both close the fault without relying on same-guard conjunct ordering, which the matrix does not license (see notes).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the respellable verdict definition

- The band member is the natural author spelling for this group: a single guard combining the cardinality check and the accessor read in one `and`-joined condition. Whether the first conjunct (`count > 0`) is itself a licensed premise for the second (`.first`/`.peek` above threshold) is an open question the matrix does not answer (see the cell's own notes) — this respelling is offered as the always-available fallback, not as evidence the band member is permanently unlicensable.

**What the sources leave unstated or ambiguous here**

- Built-status: measured at HEAD (commit e1a14d91, 2026-07-21) via `Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll)` — an ensure's pre-verb `when` clause mints no fault obligation at all. Base, both discharge additions, and both near-misses all compile clean (rc=0, no diagnostic touching this fault) regardless of which addition is present. This is the site failing to mint, not the additions being recognized or the near-misses failing to be caught — see each witness's own notes for the literal compile output.
- Open question, not licensed here: the natural author spelling `when Orders.count > 0 and Orders.peek > 10` puts both facts in the SAME guard. Class (c) — the handler's own guard — is unavailable at every guard position (a guard cannot supply a premise to an obligation arising inside itself), but whether an EARLIER conjunct of that same guard licenses a LATER one is not answered by the matrix or by `precept-language-spec.md`. This cell does not license that spelling; the band-member respelling above is offered as the licensed alternative, not as tacit permission for the self-referential form.
- Class (d) is listed as available at this evaluation-site category (constraints holding in the pre-state), so a discharge witness citing a `rule` is authored here. Per the group's hazard instruction, every such witness is recorded `builtStatus: unresolved-today` with the false-proof reason named — never as confirmed by a clean compile.
- No validity argument in the matrix's § Validity arguments literally covers premise class (a) — a collection field's own declared `mincount` modifier, as opposed to an event arg's modifier — discharging a fault-family obligation. 'Arg-bound interval arithmetic' is scoped in its own text to values 'entering as an event arg' (want:268/ingress governance); it does not by its own words extend to a persistent field's structurally-enforced bound. The closer fit is 'Inductive hypothesis plus sign monotonicity' — since every ProofSatisfaction-bearing modifier desugars to an implicit rule (`DesugarsToRule: true`, `Modifiers.cs`) under the SAME establishment+preservation obligation machinery as an author-written rule — but that argument's own worked text is about combining the hypothesis with a sign-monotonicity step (Family 1 Base B), not the direct-identity case used here. Cited as the closest available named argument; the generalization to synthesized field-modifier rules, and to the trivial (no-arithmetic) case, is not itself written out. Recorded as a missing rule, not silently absorbed by the citation.
- The fault-family case-shape table (matrix, the per-family case-shapes table) lists premise classes (b)/(c)/(a) for the fault family and does not list (d), yet this cell (per the group's own authoring instruction and the region's axis facts) authors a class-(d) discharge, and Defect B independently shows class (d) being consumed for a fault discharge at HEAD via CompositionalConstraint. Whether (d) is a licensed fault-discharge premise class at all is not stated cleanly in the matrix text — flagged as a missing rule, distinct from (and prior to) the false-proof soundness hazard Defect B documents.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: fault family case shape — catalog-declared safety precondition at the evaluation site
- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: the four premise classes
- docs/language/precept-language-spec.md § State/event ensure — spec
- src/Precept/Language/Types.cs:205 — code: queue.peek's catalog-declared Numeric requirement
- src/Precept/Language/Diagnostics.cs:568 — code: UnguardedCollectionAccess — PreventsFault CollectionEmptyOnAccess, the write-site analogue of this obligation

## g14/queue-peek-rule-activation-guard — Queue.peek read inside a conditional rule's `when` clause

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: fault family case shape
- docs/language/precept-language-spec.md § rule` declaration — spec

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | accessor/Queue/peek/numeric-0 |
| evaluation site category | rule-activation-guard |
| type family | collection |

### What must be proven

Obligation: Orders.count > 0

Weakest precondition: Orders.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Orders | the queue field the accessor reads |
| 0 | the fixed non-emptiness threshold — the catalog's declared condition, never authored |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a).

No write occurs at a conditional rule's `when` clause (it decides applicability/writability; it does not mutate). Class (c) is unavailable — the guard cannot supply a premise to an obligation arising inside itself, the headline asymmetry of this evaluation-site region. Class (b) is unavailable — no event argument represents the collection's cardinality, so no arg constraint can instantiate this obligation. Class (a) (the field's own declared `mincount`) is always in scope. Class (d) is NOT listed as available at this category, unlike the other four guard positions in this group — a rule's own activation guard has no handler guard, no event args, and (per the region's axis definition) no pre-state-constraint class either.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: compile-time interval fact from `Orders`'s declared `mincount` modifier (ProofSatisfaction.Numeric: `count >= mincount`, `src/Precept/Language/Modifiers.cs:195-208`) — a declared `mincount` of 1 or more entails `Orders.count > 0`, the catalog's own fault precondition (`src/Precept/Language/Types.cs:205`)
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Look up `Orders`'s declared `mincount` value, if any, via the ProofSatisfactions table; the fault discharges iff the declared `mincount` is >= 1 (for an integer count, `count >= 1` entails `count > 0`). No `mincount` modifier, or one declaring 0, fails the class.

- src/Precept/Language/Modifiers.cs:195 — code: Mincount ProofSatisfaction — count >= DeclarationValue
- src/Precept/Language/Types.cs:205 — code: queue.peek's catalog-declared Numeric requirement — Queue must be non-empty
- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: fault-family case shape: premise classes (b)/(c)/(a) per catalog ProofSatisfactions

### What the failing diagnostic must suggest

- For class (a): apply a `mincount` modifier (at least 1) to the collection field's declaration
  - src/Precept/Language/Modifiers.cs:125 — code: the Notempty HoverDescription names mincount 1, not notempty, as the collection route


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept QueuePeekRuleactivationguard
field Orders as queue of integer editable
field Total as decimal default 0.0 editable
rule Total > 0.0 when Orders.peek > 10 because "bounded"
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*mincount-1* — other

- Addition: mincount 1  # added to the `Orders` field declaration
- Premise classes: (a)
- Derivation: declared mincount 1 -> count >= 1 -> count > 0 (Inductive hypothesis plus sign monotonicity, trivial/identity case)
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (the rule-activation-guard position mints no fault obligation at all at HEAD (measured, commit e1a14d91) — there is nothing for this addition to discharge; the clean compile below is the site failing to mint, not the addition being recognized as a discharge)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live run (2026-07-21, Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), HEAD e1a14d91): `queue-rule-activation-guard__discharge-a.precept` compiles with rc=0 and no diagnostics (stdout carries only the WP calculator's own '[skipped obligation] ...mincount: accessor-projected bound...' note, which is about the calculator's inability to compute an establishment/preservation WP for the mincount-desugared rule — unrelated to the fault obligation this cell defines).
- `application` is omitted: this addition changes a field declaration's modifier list, which is neither a row guard (`row-guard` locus) nor an event-arg declaration (`event-arg-declarations` locus) — the schema's `application` DU has no locus for a field-modifier addition. The cell->test conversion machinery cannot mechanically apply this discharge today; flagged as a schema/tooling gap, not worked around.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| mincount-1 | mincount 0  # added to the `Orders` field declaration | mincount 0 does not exclude an empty collection — the compile-time lower bound is 0, not >= 1, so it still admits the empty case; must still reject under the model, same obligation. | reject, naming the same obligation |

### Respellability

- Resolution: cell-override
- A representative safe-but-rejected spelling: rule Total > 0.0 when Orders.count > 0 and Orders.peek > 10 because "bounded"
- Its licensed respelling: declare `Orders` with `mincount 1` (class a), or state a top-level `rule Orders.count > 0` that the induction consumes once the rule-write establishment/preservation machinery is built (class d) — both close the fault without relying on same-guard conjunct ordering, which the matrix does not license (see notes).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the respellable verdict definition

- The band member is the natural author spelling for this group: a single guard combining the cardinality check and the accessor read in one `and`-joined condition. Whether the first conjunct (`count > 0`) is itself a licensed premise for the second (`.first`/`.peek` above threshold) is an open question the matrix does not answer (see the cell's own notes) — this respelling is offered as the always-available fallback, not as evidence the band member is permanently unlicensable.

**What the sources leave unstated or ambiguous here**

- Built-status: measured at HEAD (commit e1a14d91, 2026-07-21) via `Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll)` — a conditional rule's `when` clause mints no fault obligation at all. Base, both discharge additions, and both near-misses all compile clean (rc=0, no diagnostic touching this fault) regardless of which addition is present. This is the site failing to mint, not the additions being recognized or the near-misses failing to be caught — see each witness's own notes for the literal compile output.
- Open question, not licensed here: the natural author spelling `when Orders.count > 0 and Orders.peek > 10` puts both facts in the SAME guard. Class (c) — the handler's own guard — is unavailable at every guard position (a guard cannot supply a premise to an obligation arising inside itself), but whether an EARLIER conjunct of that same guard licenses a LATER one is not answered by the matrix or by `precept-language-spec.md`. This cell does not license that spelling; the band-member respelling above is offered as the licensed alternative, not as tacit permission for the self-referential form.
- Only premise class (a) is available at a conditional rule's own `when` clause: there is no handler guard and no event args in scope here, and — per the region's own axis definition — this category does not carry class (d) either, unlike the other four guard positions in this group. Whether a rule's own activation guard can discharge a fault obligation arising inside its own condition at all is a separate open question the matrix does not answer (recorded, not resolved, at this cell).
- No validity argument in the matrix's § Validity arguments literally covers premise class (a) — a collection field's own declared `mincount` modifier, as opposed to an event arg's modifier — discharging a fault-family obligation. 'Arg-bound interval arithmetic' is scoped in its own text to values 'entering as an event arg' (want:268/ingress governance); it does not by its own words extend to a persistent field's structurally-enforced bound. The closer fit is 'Inductive hypothesis plus sign monotonicity' — since every ProofSatisfaction-bearing modifier desugars to an implicit rule (`DesugarsToRule: true`, `Modifiers.cs`) under the SAME establishment+preservation obligation machinery as an author-written rule — but that argument's own worked text is about combining the hypothesis with a sign-monotonicity step (Family 1 Base B), not the direct-identity case used here. Cited as the closest available named argument; the generalization to synthesized field-modifier rules, and to the trivial (no-arithmetic) case, is not itself written out. Recorded as a missing rule, not silently absorbed by the citation.
- The fault-family case-shape table (matrix, the per-family case-shapes table) lists premise classes (b)/(c)/(a) for the fault family and does not list (d). This cell authors no class-(d) discharge — class (d) is recorded unavailable at a conditional rule's own activation guard — but the group's other eight cells do, and Defect B independently shows class (d) being consumed for a fault discharge at HEAD via CompositionalConstraint. Whether (d) is a licensed fault-discharge premise class at all is not stated cleanly in the matrix text — flagged as a missing rule, distinct from (and prior to) the false-proof soundness hazard Defect B documents.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix: fault family case shape — catalog-declared safety precondition at the evaluation site
- docs/Working/obligation-discharge-matrix-2026-07-19.md:38 — matrix: the four premise classes
- docs/language/precept-language-spec.md § rule` declaration — spec
- src/Precept/Language/Types.cs:205 — code: queue.peek's catalog-declared Numeric requirement
- src/Precept/Language/Diagnostics.cs:568 — code: UnguardedCollectionAccess — PreventsFault CollectionEmptyOnAccess, the write-site analogue of this obligation

## Fields with no rendering rule

Data the generator has no rendering rule for, surfaced verbatim rather than dropped. Each is either a schema addition the generator has not caught up with, or a stray field.

| Where | Field | Value |
|---|---|---|
| cells[g14/list-first-transition-row-guard].respellability | overrideVerdict | "yes" |
| cells[g14/list-first-state-hook-guard].respellability | overrideVerdict | "yes" |
| cells[g14/list-first-access-mode-guard].respellability | overrideVerdict | "yes" |
| cells[g14/list-first-ensure-activation-guard].respellability | overrideVerdict | "yes" |
| cells[g14/list-first-rule-activation-guard].respellability | overrideVerdict | "yes" |
| cells[g14/queue-peek-transition-row-guard].respellability | overrideVerdict | "yes" |
| cells[g14/queue-peek-state-hook-guard].respellability | overrideVerdict | "yes" |
| cells[g14/queue-peek-access-mode-guard].respellability | overrideVerdict | "yes" |
| cells[g14/queue-peek-ensure-activation-guard].respellability | overrideVerdict | "yes" |
| cells[g14/queue-peek-rule-activation-guard].respellability | overrideVerdict | "yes" |

