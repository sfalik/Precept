<!--
GENERATED FILE — do not hand-edit.
Source: fault-2-division-primitive-construction-and-state-hook.cells.json
Regenerate: dotnet run --project tools/Precept.MatrixTools -- render-cells docs/Working/obligation-discharge-matrix-2026-07-19-cells
Hand edits will be overwritten. Edit the .cells.json and regenerate.
-->

# Fault family — division/modulo by zero, primitive lanes, construction row and state entry/exit hook

Family id: fault-2-division-construction-and-state-hook
Case shape: fault
Definition version: obligation-discharge-matrix-2026-07-19 rev 7 (2026-07-20, draft)

**Where this family's content comes from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- docs/Working/obligation-discharge-matrix-2026-07-19.md:186 — matrix: fault minting is catalog-stamped at evaluation sites
- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — fault family, arg-constraint discharge (the matrix's own worked precedent for this group's class-(b) story)
- docs/language/precept-language-spec.md:205 — spec: two-tier divisor safety
- src/Precept/Language/Operations.cs:119 — code: the ten catalog `OperationKind` entries this group covers, each carrying a `NumericProofRequirement(... NotEquals, 0m, "Divisor must be non-zero")`

## Respellability — the family verdict

Can every safe program these rules reject be rewritten into a form they accept? **yes**

Evidence: model-derived
Corpus measurement: pending

**Notes on the family verdict**

- Every near-miss in this group weakens a `positive`/`nonzero` bound to a `nonnegative` one (or a strict guard/rule to a non-strict one) — in every case the licensed respelling is simply the stricter bound, guard, or rule the contract already names, spelled directly rather than approximated. No band member requiring algebraic rearrangement or multi-fact combination was found while authoring this group; not corpus-measured, so the verdict rests on this pass's own worked witnesses only, per the ratification protocol's evidence standard.

**Sources for this verdict**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Respellable bullet
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Ratification protocol — matrix: layer 4: corpus measurement is the hard gate

## Premise classes — the legend

Where a proof may get its facts (want :19, :143): (a) field modifiers, (b) arg constraints, (c) the handler's guard, (d) all rules holding in the pre-state (the inductive hypothesis), (e) evaluating the applicable rule set at an editable-field write — (e)'s status as a genuinely fifth class vs (a) generalized is an open owner item.

## fault2/cr-intdiv — Integer division by zero, construction row

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row: catalog-declared safety precondition at the evaluation site
- docs/language/precept-language-spec.md:205 — spec: two-tier divisor safety: a proven-zero divisor is a hard error; an unproven divisor is an obligation the author discharges with nonzero, positive, a rule, or a guard

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | IntegerDivideInteger |
| evaluation site category | construction-row-action-operand |
| type family | primitive |

### What must be proven

Obligation: Divisor != 0

Weakest precondition: Divisor != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Divisor | the divisor operand at this evaluation site — an event arg of the firing construction row, or a field the row does not write |
| 0 | the integer zero value |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a), (b).

There is no pre-state at a construction row — the entity does not yet exist in any state when the initial event fires (spec § Stateless/stateful cross-validation; spec:2064: 'Construction is a special case (the entity is not yet in any state)') — so premise (d) is structurally unavailable: there is nothing for the inductive hypothesis to range over. The handler's own guard (c) does not exist either: construction rows are the `on <Event> initial` form, not a guarded transition row. What remains is whichever of the two classes matches the divisor's own syntactic kind: (b) when the divisor is the firing event's own arg (governed at ingress before this row's actions run, spec:268), and (a) when the divisor is a field the row does not write, whose own declared modifier is established over the default configuration by the same reasoning as any other unwritten field (matrix § Validity arguments, 'Establishment over defaults').

### The discharge contract — exactly when this counts as proven

**Entry 1 — (b)**

- Derivation: governance enforces the event arg's declared modifier at ingress, before this row's actions read it (spec:268); the declared modifier gives an interval on the divisor that excludes zero
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: look up the divisor arg's declared modifiers (a finite set — the modifier-to-ProofSatisfaction table, matrix § Witness Family 1 Base B decision procedure); an interval that excludes zero discharges, otherwise reject naming class (b).

- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — fault family, arg-constraint discharge: the same divisor-arg-positive pattern, already worked as the matrix's own fault-family precedent

**Entry 2 — (a)**

- Derivation: the divisor field carries its own declared modifier (e.g. `positive`/`nonzero`); the firing construction row does not write this field, so its post-construction value is exactly its declared default (matrix § Validity arguments, 'Establishment over defaults'); the modifier's own establishment obligation over that default constant-folds (matrix § Validity arguments, 'Literal constant-fold for defaults')
- Validity arguments: Establishment over defaults; Literal constant-fold for defaults
- Decision procedure: for a divisor field with no write site in the firing construction row, constant-fold the field's declared modifier over its literal default (the fold terminates — expressions are finite, loop-free trees, spec § 0.4); a fold excluding zero discharges, otherwise reject naming class (a).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Establishment over defaults (the pre-configuration rule)

### What the failing diagnostic must suggest

- For class (a): declare a modifier on the divisor field (`positive`, `nonzero`, or an equivalent bound) that the firing construction row does not overwrite, so its established default satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (b): bound the divisor arg (`positive`, `nonzero`, or an equivalent bound) such that its declared interval satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept ConstructIntdiv

field Result as integer default 0

event Init(Numerator as integer, Divisor as integer) initial

on Init -> set Result = Init.Numerator / Init.Divisor
```

Required outcome: reject, naming the missing premise classes (a), (b), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI (release build, temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*b-arg-positive* — arg-modifier

- Addition: Divisor as integer positive
- Premise classes: (b)
- Derivation: governance enforces `Divisor positive` at ingress before the row reads it; the interval (0, +inf) excludes zero
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: applied by replacing the named event-arg declarations
  - `Init.Divisor` becomes `Divisor as integer positive`

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (release build, temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

*a-field-default-positive* — structural-edit

- Addition: replace `event Init(Numerator as integer, Divisor as integer) initial` and `on Init -> set Result = Init.Numerator / Init.Divisor` with: field Divisor as integer positive default 5 event Init(Numerator as integer) initial on Init -> set Result = Init.Numerator / Divisor
- Premise classes: (a)
- Derivation: Divisor is unwritten by the construction row, so its post-construction value is its declared default; the field's own `positive` modifier constant-folds over that literal default to true, and the fold excludes zero
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (release build, temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

- This discharge changes which expression fills the divisor slot (an unwritten field, not the firing event's own arg) rather than adding a line to the stated base's own shape — recorded as `structural-edit` per the schema's own kind vocabulary for this reason.
- The `application` DU (cell.schema.json # application) has no locus for this edit: it only models (i) appending a guard to a named event row via `row-guard` (which requires an `eventName`, and this edit attaches to a state target or a bare field/rule declaration, not an event row) and (ii) replacing a named event-arg declaration. Recording the edit in `addition` and leaving `application` absent, per the same open-vocabulary-item treatment the README already uses for `construction-defaults`.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| b-arg-positive | Divisor as integer nonnegative | nonnegative admits zero — the interval [0, +inf) does not exclude it — must still reject, same obligation | reject, naming the same obligation |
| a-field-default-positive | replace the same two rows with: field Divisor as integer nonnegative default 0 event Init(Numerator as integer) initial on Init -> set Result = Init.Numerator / Divisor | the field's own default is now the zero literal itself, admitted by `nonnegative` — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- docs/Working/obligation-discharge-matrix-2026-07-19.md:186 — matrix: fault minting is catalog-stamped at evaluation sites
- docs/language/precept-language-spec.md:2064 — spec: construction is a special case — the entity is not yet in any state
- docs/language/precept-language-spec.md:1987 — spec: construction rows use `on <EventName>`, not `from <State> on <Event>`, because no state exists yet

## fault2/sh-intdiv — Integer division by zero, state entry/exit hook

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- docs/language/precept-language-spec.md:205 — spec: two-tier divisor safety

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | IntegerDivideInteger |
| evaluation site category | state-hook-action-operand |
| type family | primitive |

### What must be proven

Obligation: Divisor != 0

Weakest precondition: Divisor != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Divisor | the divisor operand at this evaluation site — a field read by a `to S ->` / `from S ->` hook's own action list |
| 0 | the integer zero value |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a), (c), (d).

A state hook fires on every inbound or outbound edge into/out of its named state, regardless of which row or event triggered the transition (spec § State action); the hook's own scope gives it field names only, no event args (spec:1348, the scope table's 'State action guard / actions \| All field names' row) — so premise (b) is structurally unavailable: there is no arg in scope to bound. What is available: (c), the hook's own optional `when` guard, evaluated the same way any guard is (guards select, spec:1897); (d), a pre-state constraint — the hook fires with a real pre-state, so the standing inductive hypothesis over the entity's reachable-configuration history applies; and (a), the divisor field's own declared modifier, carried into the hook's read whenever the hook's own action list does not write the field first. Classes (a) and (d) rest on the identical inductive mechanism here — the only difference is whether the zero-exclusion fact was declared as a modifier on the field or as a standalone `rule` — so both are argued from the same validity-argument text below, with a note on the narrower scope of its stated arithmetic.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: the hook's own `when` guard, evaluated before its actions run, restates the zero-exclusion fact directly
- Validity arguments: Guard normal-form match
- Decision procedure: normal-form match of `Divisor != <zero>` against the hook's own guard conjuncts (a finite set); no match rejects naming class (c).

- docs/language/precept-language-spec.md:1016 — spec: state actions support an optional `when` guard between the state target and the action chain
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Guard normal-form match

**Entry 2 — (a)**

- Derivation: the divisor field carries its own declared modifier (e.g. `nonzero`); that modifier is proven to hold across every write site of the field (a rule-establishment/preservation obligation on the field itself, a separate rule-write-family concern) and this hook's own action list does not write the field before the divisor is read, so the modifier's standing guarantee carries unchanged into the read — the same inductive mechanism premise (d) uses, without needing the sign-monotonicity half of that argument's text (there is no arithmetic transform here, only an unwritten read)
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: if the divisor field is not written by this hook's own action list before the read, look up its declared modifiers (the same finite modifier-to-ProofSatisfaction table class (b)/(a) use elsewhere); an interval excluding zero discharges, otherwise reject naming class (a).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Inductive hypothesis plus sign monotonicity — reused here for its inductive-hypothesis half only; the sign-monotonicity half is inapplicable and unneeded, since no write transforms the divisor between establishment and this read

- This premise's soundness depends on the divisor field's own establishment/preservation proof holding across every one of its write sites elsewhere in the file — exactly the dependency the matrix already names for premise (d) ('the hypothesis is only as sound as the minting rule is complete... a missed write site voids the hypothesis'). Recorded as a dependency, not re-derived here.

**Entry 3 — (d)**

- Derivation: an explicit `rule` states the divisor's zero-exclusion; that rule holds in the pre-state by the standing inductive hypothesis (establishment at construction, preservation at every write site of every mentioned field, atomic commit, spec:1967), and this hook's own action list does not write the divisor before the read, so the pre-state fact carries unchanged into the read
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: if the divisor field is not written by this hook's own action list before the read, look up a rule holding of it in the pre-state that excludes zero (the standing inductive hypothesis); none found rejects naming class (d).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Inductive hypothesis plus sign monotonicity — inductive-hypothesis half only, as for class (a) above
- docs/Working/obligation-discharge-matrix-2026-07-19.md:88 — matrix: Open design hole: nothing detects an obligation that was never minted — the same dependency this premise rests on
- docs/Working/obligation-discharge-matrix-2026-07-19.md:94 — matrix: the verified false-proof instance: a `rule` premise consumed via CompositionalConstraint while a different handler violates it, with zero diagnostics
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md — code: Part 3, Defect B — the same verified defect, worked in full

- HAZARD: this contract entry is consumed via a business `rule` used as a premise. Slice 2 verified at HEAD that this exact discharge shape (a rule stating a field's sign, consumed as premise (d) for an unrelated obligation) can be a false proof when a different handler sets the field to zero elsewhere in the file (matrix:94; authored-expressiveness-gaps.md Part 3, Defect B). This group's witness below has no such violating handler, so its clean compile is not itself an instance of the defect — but the discharge mechanism it exercises is the same one the defect implicates, so its builtStatus is recorded unresolved-today rather than proven-today, per this group's authoring instruction, regardless of the observed clean compile.

### What the failing diagnostic must suggest

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (a): declare a modifier on the divisor field (`positive`, `nonzero`, or an equivalent bound) that this hook does not overwrite, so its standing guarantee satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (d): state a rule that the divisor satisfies <WP>, holding in the pre-state and not written by this hook before the read
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept StateHookIntdiv

state Start initial
state Done terminal

field Divisor as integer default 0
field Result as integer default 0

event Init initial
event Finish

on Init -> set Divisor = 0

from Start on Finish -> transition Done

to Done -> set Result = 100 / Divisor
```

Required outcome: reject, naming the missing premise classes (a), (c), (d), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI (release build, temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*c-hook-guard* — guard

- Addition: when Divisor != 0
- Premise classes: (c)
- Derivation: the hook's own guard normal-form-equal to the WP discharges by guard-match, the same mechanism as a transition row's guard, generalized to a state hook's `when` (spec § State action)
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (release build, temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

- The `application` DU's only guard locus (`row-guard`) requires an `eventName`, which a state hook does not have (it is anchored to a state target, spec § State action) — `application` is left absent for this reason, per the same open-vocabulary-item treatment the README uses elsewhere; the addition text is recorded in prose instead.

*a-field-nonzero-unwritten* — other

- Addition: field Divisor as integer nonzero default 5  (replacing the base's unconstrained `field Divisor as integer default 0`; the construction row's own `set Divisor = 0` is changed to `set Divisor = 5` to keep the row internally consistent with the field's own modifier)
- Premise classes: (a)
- Derivation: Divisor's own `nonzero` modifier is established at construction (over the literal it is set to) and this hook does not write Divisor before the read, so the modifier's standing guarantee carries unchanged into the read
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (release build, temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

- The `application` DU (cell.schema.json # application) has no locus for this edit: it only models (i) appending a guard to a named event row via `row-guard` (which requires an `eventName`, and this edit attaches to a state target or a bare field/rule declaration, not an event row) and (ii) replacing a named event-arg declaration. Recording the edit in `addition` and leaving `application` absent, per the same open-vocabulary-item treatment the README already uses for `construction-defaults`.

*d-rule-nonzero-unwritten* — other

- Addition: rule Divisor != 0 because "Divisor must never be zero — a zero divisor would make every downstream rate calculation undefined"  (with the field's own default changed from 0 to 5 and the construction row's `set Divisor = 5`, so the rule is establishable)
- Premise classes: (d)
- Derivation: the rule holds in the pre-state by the standing inductive hypothesis and this hook does not write Divisor before the read, so the pre-state fact carries unchanged into the read
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (HAZARD, per this group's authoring instruction: this discharge is consumed via a business `rule` premise, the same discharge shape the verified false-proof defect implicates (matrix:94; authored-expressiveness-gaps.md Part 3, Defect B — CompositionalConstraint accepting a rule as a premise with no check that some other handler violates it). The live run below in fact compiled clean (no DivisionByZero), but per the authoring instruction this is recorded as unresolved-today, not proven-today, because the discharge mechanism is not certified sound in general, even though this specific witness has no violating handler.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (release build, temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

- The `application` DU (cell.schema.json # application) has no locus for this edit: it only models (i) appending a guard to a named event row via `row-guard` (which requires an `eventName`, and this edit attaches to a state target or a bare field/rule declaration, not an event row) and (ii) replacing a named event-arg declaration. Recording the edit in `addition` and leaving `application` absent, per the same open-vocabulary-item treatment the README already uses for `construction-defaults`.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| c-hook-guard | when Divisor >= 0 | the guard admits zero — does not imply the WP — must still reject, same obligation | reject, naming the same obligation |
| a-field-nonzero-unwritten | field Divisor as integer nonnegative default 0  (construction row keeps `set Divisor = 0`) | nonnegative admits the field's own default of zero — must still reject, same obligation | reject, naming the same obligation |
| d-rule-nonzero-unwritten | rule Divisor >= 0 because "Divisor must never be negative..."  (field default kept at 0) | the rule admits zero — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- The matrix records as open whether the residency fact itself (the entity is in state S) is a premise, and under which class (matrix:150, the `in S ensure` case-shape row's Open bullet). This cell's own divisor obligation does not depend on residency — the discharges above rest on the field's own history, not on which state the entity is in — but the group's authoring instruction is to carry this open item on every state-hook cell rather than assume either answer, so it is recorded here. No packet Q/S identifier has been minted for this specific open item as of this pass (checked: review-1 through review-6 under obligation-discharge-matrix-2026-07-19-reviews/, and the want-analysis decision packet); `openDependencies` is left empty rather than filled with a fabricated identifier, and this note carries the dependency in prose instead.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- docs/language/precept-language-spec.md § State action — spec: the state-action grammar and its optional `when` guard
- docs/language/precept-language-spec.md:1348 — spec: the scope table: state action guard / actions see all field names, nothing else

## fault2/cr-intmod — Integer modulo by zero, construction row

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row: catalog-declared safety precondition at the evaluation site
- docs/language/precept-language-spec.md:205 — spec: two-tier divisor safety: a proven-zero divisor is a hard error; an unproven divisor is an obligation the author discharges with nonzero, positive, a rule, or a guard

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | IntegerModuloInteger |
| evaluation site category | construction-row-action-operand |
| type family | primitive |

### What must be proven

Obligation: Divisor != 0

Weakest precondition: Divisor != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Divisor | the divisor operand at this evaluation site — an event arg of the firing construction row, or a field the row does not write |
| 0 | the integer zero value |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a), (b).

There is no pre-state at a construction row — the entity does not yet exist in any state when the initial event fires (spec § Stateless/stateful cross-validation; spec:2064: 'Construction is a special case (the entity is not yet in any state)') — so premise (d) is structurally unavailable: there is nothing for the inductive hypothesis to range over. The handler's own guard (c) does not exist either: construction rows are the `on <Event> initial` form, not a guarded transition row. What remains is whichever of the two classes matches the divisor's own syntactic kind: (b) when the divisor is the firing event's own arg (governed at ingress before this row's actions run, spec:268), and (a) when the divisor is a field the row does not write, whose own declared modifier is established over the default configuration by the same reasoning as any other unwritten field (matrix § Validity arguments, 'Establishment over defaults').

### The discharge contract — exactly when this counts as proven

**Entry 1 — (b)**

- Derivation: governance enforces the event arg's declared modifier at ingress, before this row's actions read it (spec:268); the declared modifier gives an interval on the divisor that excludes zero
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: look up the divisor arg's declared modifiers (a finite set — the modifier-to-ProofSatisfaction table, matrix § Witness Family 1 Base B decision procedure); an interval that excludes zero discharges, otherwise reject naming class (b).

- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — fault family, arg-constraint discharge: the same divisor-arg-positive pattern, already worked as the matrix's own fault-family precedent

**Entry 2 — (a)**

- Derivation: the divisor field carries its own declared modifier (e.g. `positive`/`nonzero`); the firing construction row does not write this field, so its post-construction value is exactly its declared default (matrix § Validity arguments, 'Establishment over defaults'); the modifier's own establishment obligation over that default constant-folds (matrix § Validity arguments, 'Literal constant-fold for defaults')
- Validity arguments: Establishment over defaults; Literal constant-fold for defaults
- Decision procedure: for a divisor field with no write site in the firing construction row, constant-fold the field's declared modifier over its literal default (the fold terminates — expressions are finite, loop-free trees, spec § 0.4); a fold excluding zero discharges, otherwise reject naming class (a).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Establishment over defaults (the pre-configuration rule)

### What the failing diagnostic must suggest

- For class (a): declare a modifier on the divisor field (`positive`, `nonzero`, or an equivalent bound) that the firing construction row does not overwrite, so its established default satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (b): bound the divisor arg (`positive`, `nonzero`, or an equivalent bound) such that its declared interval satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept ConstructIntmod

field Result as integer default 0

event Init(Numerator as integer, Divisor as integer) initial

on Init -> set Result = Init.Numerator % Init.Divisor
```

Required outcome: reject, naming the missing premise classes (a), (b), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI (release build, temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*b-arg-positive* — arg-modifier

- Addition: Divisor as integer positive
- Premise classes: (b)
- Derivation: governance enforces `Divisor positive` at ingress before the row reads it; the interval (0, +inf) excludes zero
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: applied by replacing the named event-arg declarations
  - `Init.Divisor` becomes `Divisor as integer positive`

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (release build, temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

*a-field-default-positive* — structural-edit

- Addition: replace `event Init(Numerator as integer, Divisor as integer) initial` and `on Init -> set Result = Init.Numerator % Init.Divisor` with: field Divisor as integer positive default 5 event Init(Numerator as integer) initial on Init -> set Result = Init.Numerator % Divisor
- Premise classes: (a)
- Derivation: Divisor is unwritten by the construction row, so its post-construction value is its declared default; the field's own `positive` modifier constant-folds over that literal default to true, and the fold excludes zero
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (release build, temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

- This discharge changes which expression fills the divisor slot (an unwritten field, not the firing event's own arg) rather than adding a line to the stated base's own shape — recorded as `structural-edit` per the schema's own kind vocabulary for this reason.
- The `application` DU (cell.schema.json # application) has no locus for this edit: it only models (i) appending a guard to a named event row via `row-guard` (which requires an `eventName`, and this edit attaches to a state target or a bare field/rule declaration, not an event row) and (ii) replacing a named event-arg declaration. Recording the edit in `addition` and leaving `application` absent, per the same open-vocabulary-item treatment the README already uses for `construction-defaults`.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| b-arg-positive | Divisor as integer nonnegative | nonnegative admits zero — the interval [0, +inf) does not exclude it — must still reject, same obligation | reject, naming the same obligation |
| a-field-default-positive | replace the same two rows with: field Divisor as integer nonnegative default 0 event Init(Numerator as integer) initial on Init -> set Result = Init.Numerator % Divisor | the field's own default is now the zero literal itself, admitted by `nonnegative` — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- docs/Working/obligation-discharge-matrix-2026-07-19.md:186 — matrix: fault minting is catalog-stamped at evaluation sites
- docs/language/precept-language-spec.md:2064 — spec: construction is a special case — the entity is not yet in any state
- docs/language/precept-language-spec.md:1987 — spec: construction rows use `on <EventName>`, not `from <State> on <Event>`, because no state exists yet

## fault2/sh-intmod — Integer modulo by zero, state entry/exit hook

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- docs/language/precept-language-spec.md:205 — spec: two-tier divisor safety

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | IntegerModuloInteger |
| evaluation site category | state-hook-action-operand |
| type family | primitive |

### What must be proven

Obligation: Divisor != 0

Weakest precondition: Divisor != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Divisor | the divisor operand at this evaluation site — a field read by a `to S ->` / `from S ->` hook's own action list |
| 0 | the integer zero value |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a), (c), (d).

A state hook fires on every inbound or outbound edge into/out of its named state, regardless of which row or event triggered the transition (spec § State action); the hook's own scope gives it field names only, no event args (spec:1348, the scope table's 'State action guard / actions \| All field names' row) — so premise (b) is structurally unavailable: there is no arg in scope to bound. What is available: (c), the hook's own optional `when` guard, evaluated the same way any guard is (guards select, spec:1897); (d), a pre-state constraint — the hook fires with a real pre-state, so the standing inductive hypothesis over the entity's reachable-configuration history applies; and (a), the divisor field's own declared modifier, carried into the hook's read whenever the hook's own action list does not write the field first. Classes (a) and (d) rest on the identical inductive mechanism here — the only difference is whether the zero-exclusion fact was declared as a modifier on the field or as a standalone `rule` — so both are argued from the same validity-argument text below, with a note on the narrower scope of its stated arithmetic.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: the hook's own `when` guard, evaluated before its actions run, restates the zero-exclusion fact directly
- Validity arguments: Guard normal-form match
- Decision procedure: normal-form match of `Divisor != <zero>` against the hook's own guard conjuncts (a finite set); no match rejects naming class (c).

- docs/language/precept-language-spec.md:1016 — spec: state actions support an optional `when` guard between the state target and the action chain
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Guard normal-form match

**Entry 2 — (a)**

- Derivation: the divisor field carries its own declared modifier (e.g. `nonzero`); that modifier is proven to hold across every write site of the field (a rule-establishment/preservation obligation on the field itself, a separate rule-write-family concern) and this hook's own action list does not write the field before the divisor is read, so the modifier's standing guarantee carries unchanged into the read — the same inductive mechanism premise (d) uses, without needing the sign-monotonicity half of that argument's text (there is no arithmetic transform here, only an unwritten read)
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: if the divisor field is not written by this hook's own action list before the read, look up its declared modifiers (the same finite modifier-to-ProofSatisfaction table class (b)/(a) use elsewhere); an interval excluding zero discharges, otherwise reject naming class (a).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Inductive hypothesis plus sign monotonicity — reused here for its inductive-hypothesis half only; the sign-monotonicity half is inapplicable and unneeded, since no write transforms the divisor between establishment and this read

- This premise's soundness depends on the divisor field's own establishment/preservation proof holding across every one of its write sites elsewhere in the file — exactly the dependency the matrix already names for premise (d) ('the hypothesis is only as sound as the minting rule is complete... a missed write site voids the hypothesis'). Recorded as a dependency, not re-derived here.

**Entry 3 — (d)**

- Derivation: an explicit `rule` states the divisor's zero-exclusion; that rule holds in the pre-state by the standing inductive hypothesis (establishment at construction, preservation at every write site of every mentioned field, atomic commit, spec:1967), and this hook's own action list does not write the divisor before the read, so the pre-state fact carries unchanged into the read
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: if the divisor field is not written by this hook's own action list before the read, look up a rule holding of it in the pre-state that excludes zero (the standing inductive hypothesis); none found rejects naming class (d).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Inductive hypothesis plus sign monotonicity — inductive-hypothesis half only, as for class (a) above
- docs/Working/obligation-discharge-matrix-2026-07-19.md:88 — matrix: Open design hole: nothing detects an obligation that was never minted — the same dependency this premise rests on
- docs/Working/obligation-discharge-matrix-2026-07-19.md:94 — matrix: the verified false-proof instance: a `rule` premise consumed via CompositionalConstraint while a different handler violates it, with zero diagnostics
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md — code: Part 3, Defect B — the same verified defect, worked in full

- HAZARD: this contract entry is consumed via a business `rule` used as a premise. Slice 2 verified at HEAD that this exact discharge shape (a rule stating a field's sign, consumed as premise (d) for an unrelated obligation) can be a false proof when a different handler sets the field to zero elsewhere in the file (matrix:94; authored-expressiveness-gaps.md Part 3, Defect B). This group's witness below has no such violating handler, so its clean compile is not itself an instance of the defect — but the discharge mechanism it exercises is the same one the defect implicates, so its builtStatus is recorded unresolved-today rather than proven-today, per this group's authoring instruction, regardless of the observed clean compile.

### What the failing diagnostic must suggest

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (a): declare a modifier on the divisor field (`positive`, `nonzero`, or an equivalent bound) that this hook does not overwrite, so its standing guarantee satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (d): state a rule that the divisor satisfies <WP>, holding in the pre-state and not written by this hook before the read
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept StateHookIntmod

state Start initial
state Done terminal

field Divisor as integer default 0
field Result as integer default 0

event Init initial
event Finish

on Init -> set Divisor = 0

from Start on Finish -> transition Done

to Done -> set Result = 100 % Divisor
```

Required outcome: reject, naming the missing premise classes (a), (c), (d), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI (release build, temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*c-hook-guard* — guard

- Addition: when Divisor != 0
- Premise classes: (c)
- Derivation: the hook's own guard normal-form-equal to the WP discharges by guard-match, the same mechanism as a transition row's guard, generalized to a state hook's `when` (spec § State action)
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (release build, temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

- The `application` DU's only guard locus (`row-guard`) requires an `eventName`, which a state hook does not have (it is anchored to a state target, spec § State action) — `application` is left absent for this reason, per the same open-vocabulary-item treatment the README uses elsewhere; the addition text is recorded in prose instead.

*a-field-nonzero-unwritten* — other

- Addition: field Divisor as integer nonzero default 5  (replacing the base's unconstrained `field Divisor as integer default 0`; the construction row's own `set Divisor = 0` is changed to `set Divisor = 5` to keep the row internally consistent with the field's own modifier)
- Premise classes: (a)
- Derivation: Divisor's own `nonzero` modifier is established at construction (over the literal it is set to) and this hook does not write Divisor before the read, so the modifier's standing guarantee carries unchanged into the read
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (release build, temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

- The `application` DU (cell.schema.json # application) has no locus for this edit: it only models (i) appending a guard to a named event row via `row-guard` (which requires an `eventName`, and this edit attaches to a state target or a bare field/rule declaration, not an event row) and (ii) replacing a named event-arg declaration. Recording the edit in `addition` and leaving `application` absent, per the same open-vocabulary-item treatment the README already uses for `construction-defaults`.

*d-rule-nonzero-unwritten* — other

- Addition: rule Divisor != 0 because "Divisor must never be zero — a zero divisor would make every downstream rate calculation undefined"  (with the field's own default changed from 0 to 5 and the construction row's `set Divisor = 5`, so the rule is establishable)
- Premise classes: (d)
- Derivation: the rule holds in the pre-state by the standing inductive hypothesis and this hook does not write Divisor before the read, so the pre-state fact carries unchanged into the read
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (HAZARD, per this group's authoring instruction: this discharge is consumed via a business `rule` premise, the same discharge shape the verified false-proof defect implicates (matrix:94; authored-expressiveness-gaps.md Part 3, Defect B — CompositionalConstraint accepting a rule as a premise with no check that some other handler violates it). The live run below in fact compiled clean (no DivisionByZero), but per the authoring instruction this is recorded as unresolved-today, not proven-today, because the discharge mechanism is not certified sound in general, even though this specific witness has no violating handler.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (release build, temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

- The `application` DU (cell.schema.json # application) has no locus for this edit: it only models (i) appending a guard to a named event row via `row-guard` (which requires an `eventName`, and this edit attaches to a state target or a bare field/rule declaration, not an event row) and (ii) replacing a named event-arg declaration. Recording the edit in `addition` and leaving `application` absent, per the same open-vocabulary-item treatment the README already uses for `construction-defaults`.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| c-hook-guard | when Divisor >= 0 | the guard admits zero — does not imply the WP — must still reject, same obligation | reject, naming the same obligation |
| a-field-nonzero-unwritten | field Divisor as integer nonnegative default 0  (construction row keeps `set Divisor = 0`) | nonnegative admits the field's own default of zero — must still reject, same obligation | reject, naming the same obligation |
| d-rule-nonzero-unwritten | rule Divisor >= 0 because "Divisor must never be negative..."  (field default kept at 0) | the rule admits zero — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- The matrix records as open whether the residency fact itself (the entity is in state S) is a premise, and under which class (matrix:150, the `in S ensure` case-shape row's Open bullet). This cell's own divisor obligation does not depend on residency — the discharges above rest on the field's own history, not on which state the entity is in — but the group's authoring instruction is to carry this open item on every state-hook cell rather than assume either answer, so it is recorded here. No packet Q/S identifier has been minted for this specific open item as of this pass (checked: review-1 through review-6 under obligation-discharge-matrix-2026-07-19-reviews/, and the want-analysis decision packet); `openDependencies` is left empty rather than filled with a fabricated identifier, and this note carries the dependency in prose instead.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- docs/language/precept-language-spec.md § State action — spec: the state-action grammar and its optional `when` guard
- docs/language/precept-language-spec.md:1348 — spec: the scope table: state action guard / actions see all field names, nothing else

## fault2/cr-decdiv — Decimal division by zero, construction row

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row: catalog-declared safety precondition at the evaluation site
- docs/language/precept-language-spec.md:205 — spec: two-tier divisor safety: a proven-zero divisor is a hard error; an unproven divisor is an obligation the author discharges with nonzero, positive, a rule, or a guard

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | DecimalDivideDecimal |
| evaluation site category | construction-row-action-operand |
| type family | primitive |

### What must be proven

Obligation: Divisor != 0.0

Weakest precondition: Divisor != 0.0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Divisor | the divisor operand at this evaluation site — an event arg of the firing construction row, or a field the row does not write |
| 0.0 | the decimal zero value |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a), (b).

There is no pre-state at a construction row — the entity does not yet exist in any state when the initial event fires (spec § Stateless/stateful cross-validation; spec:2064: 'Construction is a special case (the entity is not yet in any state)') — so premise (d) is structurally unavailable: there is nothing for the inductive hypothesis to range over. The handler's own guard (c) does not exist either: construction rows are the `on <Event> initial` form, not a guarded transition row. What remains is whichever of the two classes matches the divisor's own syntactic kind: (b) when the divisor is the firing event's own arg (governed at ingress before this row's actions run, spec:268), and (a) when the divisor is a field the row does not write, whose own declared modifier is established over the default configuration by the same reasoning as any other unwritten field (matrix § Validity arguments, 'Establishment over defaults').

### The discharge contract — exactly when this counts as proven

**Entry 1 — (b)**

- Derivation: governance enforces the event arg's declared modifier at ingress, before this row's actions read it (spec:268); the declared modifier gives an interval on the divisor that excludes zero
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: look up the divisor arg's declared modifiers (a finite set — the modifier-to-ProofSatisfaction table, matrix § Witness Family 1 Base B decision procedure); an interval that excludes zero discharges, otherwise reject naming class (b).

- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — fault family, arg-constraint discharge: the same divisor-arg-positive pattern, already worked as the matrix's own fault-family precedent

**Entry 2 — (a)**

- Derivation: the divisor field carries its own declared modifier (e.g. `positive`/`nonzero`); the firing construction row does not write this field, so its post-construction value is exactly its declared default (matrix § Validity arguments, 'Establishment over defaults'); the modifier's own establishment obligation over that default constant-folds (matrix § Validity arguments, 'Literal constant-fold for defaults')
- Validity arguments: Establishment over defaults; Literal constant-fold for defaults
- Decision procedure: for a divisor field with no write site in the firing construction row, constant-fold the field's declared modifier over its literal default (the fold terminates — expressions are finite, loop-free trees, spec § 0.4); a fold excluding zero discharges, otherwise reject naming class (a).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Establishment over defaults (the pre-configuration rule)

### What the failing diagnostic must suggest

- For class (a): declare a modifier on the divisor field (`positive`, `nonzero`, or an equivalent bound) that the firing construction row does not overwrite, so its established default satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (b): bound the divisor arg (`positive`, `nonzero`, or an equivalent bound) such that its declared interval satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept ConstructDecdiv

field Result as decimal default 0.0

event Init(Numerator as decimal, Divisor as decimal) initial

on Init -> set Result = Init.Numerator / Init.Divisor
```

Required outcome: reject, naming the missing premise classes (a), (b), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI (release build, temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*b-arg-positive* — arg-modifier

- Addition: Divisor as decimal positive
- Premise classes: (b)
- Derivation: governance enforces `Divisor positive` at ingress before the row reads it; the interval (0, +inf) excludes zero
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: applied by replacing the named event-arg declarations
  - `Init.Divisor` becomes `Divisor as decimal positive`

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (release build, temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

*a-field-default-positive* — structural-edit

- Addition: replace `event Init(Numerator as decimal, Divisor as decimal) initial` and `on Init -> set Result = Init.Numerator / Init.Divisor` with: field Divisor as decimal positive default 5.0 event Init(Numerator as decimal) initial on Init -> set Result = Init.Numerator / Divisor
- Premise classes: (a)
- Derivation: Divisor is unwritten by the construction row, so its post-construction value is its declared default; the field's own `positive` modifier constant-folds over that literal default to true, and the fold excludes zero
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (release build, temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

- This discharge changes which expression fills the divisor slot (an unwritten field, not the firing event's own arg) rather than adding a line to the stated base's own shape — recorded as `structural-edit` per the schema's own kind vocabulary for this reason.
- The `application` DU (cell.schema.json # application) has no locus for this edit: it only models (i) appending a guard to a named event row via `row-guard` (which requires an `eventName`, and this edit attaches to a state target or a bare field/rule declaration, not an event row) and (ii) replacing a named event-arg declaration. Recording the edit in `addition` and leaving `application` absent, per the same open-vocabulary-item treatment the README already uses for `construction-defaults`.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| b-arg-positive | Divisor as decimal nonnegative | nonnegative admits zero — the interval [0, +inf) does not exclude it — must still reject, same obligation | reject, naming the same obligation |
| a-field-default-positive | replace the same two rows with: field Divisor as decimal nonnegative default 0.0 event Init(Numerator as decimal) initial on Init -> set Result = Init.Numerator / Divisor | the field's own default is now the zero literal itself, admitted by `nonnegative` — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- This cell also covers op/IntegerDivideDecimal/numeric-0: each declares the identical `NumericProofRequirement(ParamSubject(PDecimal), NotEquals, 0m, "Divisor must be non-zero")` on the same divisor-type-family subject as DecimalDivideDecimal (src/Precept/Language/Operations.cs), so the obligation schema, applicable-class set, and decision procedures below are identical up to which side of the operator widens; no separate cell is authored for them under the matrix's schema-unification criterion (matrix § The cell, item 1: 'Two programs sit in the same cell iff ... their minted obligations unify with this schema').

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- docs/Working/obligation-discharge-matrix-2026-07-19.md:186 — matrix: fault minting is catalog-stamped at evaluation sites
- docs/language/precept-language-spec.md:2064 — spec: construction is a special case — the entity is not yet in any state
- docs/language/precept-language-spec.md:1987 — spec: construction rows use `on <EventName>`, not `from <State> on <Event>`, because no state exists yet

## fault2/sh-decdiv — Decimal division by zero, state entry/exit hook

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- docs/language/precept-language-spec.md:205 — spec: two-tier divisor safety

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | DecimalDivideDecimal |
| evaluation site category | state-hook-action-operand |
| type family | primitive |

### What must be proven

Obligation: Divisor != 0.0

Weakest precondition: Divisor != 0.0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Divisor | the divisor operand at this evaluation site — a field read by a `to S ->` / `from S ->` hook's own action list |
| 0.0 | the decimal zero value |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a), (c), (d).

A state hook fires on every inbound or outbound edge into/out of its named state, regardless of which row or event triggered the transition (spec § State action); the hook's own scope gives it field names only, no event args (spec:1348, the scope table's 'State action guard / actions \| All field names' row) — so premise (b) is structurally unavailable: there is no arg in scope to bound. What is available: (c), the hook's own optional `when` guard, evaluated the same way any guard is (guards select, spec:1897); (d), a pre-state constraint — the hook fires with a real pre-state, so the standing inductive hypothesis over the entity's reachable-configuration history applies; and (a), the divisor field's own declared modifier, carried into the hook's read whenever the hook's own action list does not write the field first. Classes (a) and (d) rest on the identical inductive mechanism here — the only difference is whether the zero-exclusion fact was declared as a modifier on the field or as a standalone `rule` — so both are argued from the same validity-argument text below, with a note on the narrower scope of its stated arithmetic.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: the hook's own `when` guard, evaluated before its actions run, restates the zero-exclusion fact directly
- Validity arguments: Guard normal-form match
- Decision procedure: normal-form match of `Divisor != <zero>` against the hook's own guard conjuncts (a finite set); no match rejects naming class (c).

- docs/language/precept-language-spec.md:1016 — spec: state actions support an optional `when` guard between the state target and the action chain
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Guard normal-form match

**Entry 2 — (a)**

- Derivation: the divisor field carries its own declared modifier (e.g. `nonzero`); that modifier is proven to hold across every write site of the field (a rule-establishment/preservation obligation on the field itself, a separate rule-write-family concern) and this hook's own action list does not write the field before the divisor is read, so the modifier's standing guarantee carries unchanged into the read — the same inductive mechanism premise (d) uses, without needing the sign-monotonicity half of that argument's text (there is no arithmetic transform here, only an unwritten read)
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: if the divisor field is not written by this hook's own action list before the read, look up its declared modifiers (the same finite modifier-to-ProofSatisfaction table class (b)/(a) use elsewhere); an interval excluding zero discharges, otherwise reject naming class (a).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Inductive hypothesis plus sign monotonicity — reused here for its inductive-hypothesis half only; the sign-monotonicity half is inapplicable and unneeded, since no write transforms the divisor between establishment and this read

- This premise's soundness depends on the divisor field's own establishment/preservation proof holding across every one of its write sites elsewhere in the file — exactly the dependency the matrix already names for premise (d) ('the hypothesis is only as sound as the minting rule is complete... a missed write site voids the hypothesis'). Recorded as a dependency, not re-derived here.

**Entry 3 — (d)**

- Derivation: an explicit `rule` states the divisor's zero-exclusion; that rule holds in the pre-state by the standing inductive hypothesis (establishment at construction, preservation at every write site of every mentioned field, atomic commit, spec:1967), and this hook's own action list does not write the divisor before the read, so the pre-state fact carries unchanged into the read
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: if the divisor field is not written by this hook's own action list before the read, look up a rule holding of it in the pre-state that excludes zero (the standing inductive hypothesis); none found rejects naming class (d).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Inductive hypothesis plus sign monotonicity — inductive-hypothesis half only, as for class (a) above
- docs/Working/obligation-discharge-matrix-2026-07-19.md:88 — matrix: Open design hole: nothing detects an obligation that was never minted — the same dependency this premise rests on
- docs/Working/obligation-discharge-matrix-2026-07-19.md:94 — matrix: the verified false-proof instance: a `rule` premise consumed via CompositionalConstraint while a different handler violates it, with zero diagnostics
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md — code: Part 3, Defect B — the same verified defect, worked in full

- HAZARD: this contract entry is consumed via a business `rule` used as a premise. Slice 2 verified at HEAD that this exact discharge shape (a rule stating a field's sign, consumed as premise (d) for an unrelated obligation) can be a false proof when a different handler sets the field to zero elsewhere in the file (matrix:94; authored-expressiveness-gaps.md Part 3, Defect B). This group's witness below has no such violating handler, so its clean compile is not itself an instance of the defect — but the discharge mechanism it exercises is the same one the defect implicates, so its builtStatus is recorded unresolved-today rather than proven-today, per this group's authoring instruction, regardless of the observed clean compile.

### What the failing diagnostic must suggest

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (a): declare a modifier on the divisor field (`positive`, `nonzero`, or an equivalent bound) that this hook does not overwrite, so its standing guarantee satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (d): state a rule that the divisor satisfies <WP>, holding in the pre-state and not written by this hook before the read
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept StateHookDecdiv

state Start initial
state Done terminal

field Divisor as decimal default 0.0
field Result as decimal default 0.0

event Init initial
event Finish

on Init -> set Divisor = 0.0

from Start on Finish -> transition Done

to Done -> set Result = 100 / Divisor
```

Required outcome: reject, naming the missing premise classes (a), (c), (d), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI (release build, temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*c-hook-guard* — guard

- Addition: when Divisor != 0.0
- Premise classes: (c)
- Derivation: the hook's own guard normal-form-equal to the WP discharges by guard-match, the same mechanism as a transition row's guard, generalized to a state hook's `when` (spec § State action)
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (release build, temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

- The `application` DU's only guard locus (`row-guard`) requires an `eventName`, which a state hook does not have (it is anchored to a state target, spec § State action) — `application` is left absent for this reason, per the same open-vocabulary-item treatment the README uses elsewhere; the addition text is recorded in prose instead.

*a-field-nonzero-unwritten* — other

- Addition: field Divisor as decimal nonzero default 5.0  (replacing the base's unconstrained `field Divisor as decimal default 0.0`; the construction row's own `set Divisor = 0.0` is changed to `set Divisor = 5.0` to keep the row internally consistent with the field's own modifier)
- Premise classes: (a)
- Derivation: Divisor's own `nonzero` modifier is established at construction (over the literal it is set to) and this hook does not write Divisor before the read, so the modifier's standing guarantee carries unchanged into the read
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (release build, temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

- The `application` DU (cell.schema.json # application) has no locus for this edit: it only models (i) appending a guard to a named event row via `row-guard` (which requires an `eventName`, and this edit attaches to a state target or a bare field/rule declaration, not an event row) and (ii) replacing a named event-arg declaration. Recording the edit in `addition` and leaving `application` absent, per the same open-vocabulary-item treatment the README already uses for `construction-defaults`.

*d-rule-nonzero-unwritten* — other

- Addition: rule Divisor != 0.0 because "Divisor must never be zero — a zero divisor would make every downstream rate calculation undefined"  (with the field's own default changed from 0.0 to 5.0 and the construction row's `set Divisor = 5.0`, so the rule is establishable)
- Premise classes: (d)
- Derivation: the rule holds in the pre-state by the standing inductive hypothesis and this hook does not write Divisor before the read, so the pre-state fact carries unchanged into the read
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (HAZARD, per this group's authoring instruction: this discharge is consumed via a business `rule` premise, the same discharge shape the verified false-proof defect implicates (matrix:94; authored-expressiveness-gaps.md Part 3, Defect B — CompositionalConstraint accepting a rule as a premise with no check that some other handler violates it). The live run below in fact compiled clean (no DivisionByZero), but per the authoring instruction this is recorded as unresolved-today, not proven-today, because the discharge mechanism is not certified sound in general, even though this specific witness has no violating handler.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (release build, temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

- The `application` DU (cell.schema.json # application) has no locus for this edit: it only models (i) appending a guard to a named event row via `row-guard` (which requires an `eventName`, and this edit attaches to a state target or a bare field/rule declaration, not an event row) and (ii) replacing a named event-arg declaration. Recording the edit in `addition` and leaving `application` absent, per the same open-vocabulary-item treatment the README already uses for `construction-defaults`.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| c-hook-guard | when Divisor >= 0.0 | the guard admits zero — does not imply the WP — must still reject, same obligation | reject, naming the same obligation |
| a-field-nonzero-unwritten | field Divisor as decimal nonnegative default 0.0  (construction row keeps `set Divisor = 0.0`) | nonnegative admits the field's own default of zero — must still reject, same obligation | reject, naming the same obligation |
| d-rule-nonzero-unwritten | rule Divisor >= 0.0 because "Divisor must never be negative..."  (field default kept at 0.0) | the rule admits zero — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- This cell also covers op/IntegerDivideDecimal/numeric-0: each declares the identical `NumericProofRequirement(ParamSubject(PDecimal), NotEquals, 0m, "Divisor must be non-zero")` on the same divisor-type-family subject as DecimalDivideDecimal (src/Precept/Language/Operations.cs), so the obligation schema, applicable-class set, and decision procedures below are identical up to which side of the operator widens; no separate cell is authored for them under the matrix's schema-unification criterion (matrix § The cell, item 1: 'Two programs sit in the same cell iff ... their minted obligations unify with this schema').
- The matrix records as open whether the residency fact itself (the entity is in state S) is a premise, and under which class (matrix:150, the `in S ensure` case-shape row's Open bullet). This cell's own divisor obligation does not depend on residency — the discharges above rest on the field's own history, not on which state the entity is in — but the group's authoring instruction is to carry this open item on every state-hook cell rather than assume either answer, so it is recorded here. No packet Q/S identifier has been minted for this specific open item as of this pass (checked: review-1 through review-6 under obligation-discharge-matrix-2026-07-19-reviews/, and the want-analysis decision packet); `openDependencies` is left empty rather than filled with a fabricated identifier, and this note carries the dependency in prose instead.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- docs/language/precept-language-spec.md § State action — spec: the state-action grammar and its optional `when` guard
- docs/language/precept-language-spec.md:1348 — spec: the scope table: state action guard / actions see all field names, nothing else

## fault2/cr-decmod — Decimal modulo by zero, construction row

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row: catalog-declared safety precondition at the evaluation site
- docs/language/precept-language-spec.md:205 — spec: two-tier divisor safety: a proven-zero divisor is a hard error; an unproven divisor is an obligation the author discharges with nonzero, positive, a rule, or a guard

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | DecimalModuloDecimal |
| evaluation site category | construction-row-action-operand |
| type family | primitive |

### What must be proven

Obligation: Divisor != 0.0

Weakest precondition: Divisor != 0.0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Divisor | the divisor operand at this evaluation site — an event arg of the firing construction row, or a field the row does not write |
| 0.0 | the decimal zero value |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a), (b).

There is no pre-state at a construction row — the entity does not yet exist in any state when the initial event fires (spec § Stateless/stateful cross-validation; spec:2064: 'Construction is a special case (the entity is not yet in any state)') — so premise (d) is structurally unavailable: there is nothing for the inductive hypothesis to range over. The handler's own guard (c) does not exist either: construction rows are the `on <Event> initial` form, not a guarded transition row. What remains is whichever of the two classes matches the divisor's own syntactic kind: (b) when the divisor is the firing event's own arg (governed at ingress before this row's actions run, spec:268), and (a) when the divisor is a field the row does not write, whose own declared modifier is established over the default configuration by the same reasoning as any other unwritten field (matrix § Validity arguments, 'Establishment over defaults').

### The discharge contract — exactly when this counts as proven

**Entry 1 — (b)**

- Derivation: governance enforces the event arg's declared modifier at ingress, before this row's actions read it (spec:268); the declared modifier gives an interval on the divisor that excludes zero
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: look up the divisor arg's declared modifiers (a finite set — the modifier-to-ProofSatisfaction table, matrix § Witness Family 1 Base B decision procedure); an interval that excludes zero discharges, otherwise reject naming class (b).

- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — fault family, arg-constraint discharge: the same divisor-arg-positive pattern, already worked as the matrix's own fault-family precedent

**Entry 2 — (a)**

- Derivation: the divisor field carries its own declared modifier (e.g. `positive`/`nonzero`); the firing construction row does not write this field, so its post-construction value is exactly its declared default (matrix § Validity arguments, 'Establishment over defaults'); the modifier's own establishment obligation over that default constant-folds (matrix § Validity arguments, 'Literal constant-fold for defaults')
- Validity arguments: Establishment over defaults; Literal constant-fold for defaults
- Decision procedure: for a divisor field with no write site in the firing construction row, constant-fold the field's declared modifier over its literal default (the fold terminates — expressions are finite, loop-free trees, spec § 0.4); a fold excluding zero discharges, otherwise reject naming class (a).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Establishment over defaults (the pre-configuration rule)

### What the failing diagnostic must suggest

- For class (a): declare a modifier on the divisor field (`positive`, `nonzero`, or an equivalent bound) that the firing construction row does not overwrite, so its established default satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (b): bound the divisor arg (`positive`, `nonzero`, or an equivalent bound) such that its declared interval satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept ConstructDecmod

field Result as decimal default 0.0

event Init(Numerator as decimal, Divisor as decimal) initial

on Init -> set Result = Init.Numerator % Init.Divisor
```

Required outcome: reject, naming the missing premise classes (a), (b), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI (release build, temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*b-arg-positive* — arg-modifier

- Addition: Divisor as decimal positive
- Premise classes: (b)
- Derivation: governance enforces `Divisor positive` at ingress before the row reads it; the interval (0, +inf) excludes zero
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: applied by replacing the named event-arg declarations
  - `Init.Divisor` becomes `Divisor as decimal positive`

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (release build, temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

*a-field-default-positive* — structural-edit

- Addition: replace `event Init(Numerator as decimal, Divisor as decimal) initial` and `on Init -> set Result = Init.Numerator % Init.Divisor` with: field Divisor as decimal positive default 5.0 event Init(Numerator as decimal) initial on Init -> set Result = Init.Numerator % Divisor
- Premise classes: (a)
- Derivation: Divisor is unwritten by the construction row, so its post-construction value is its declared default; the field's own `positive` modifier constant-folds over that literal default to true, and the fold excludes zero
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (release build, temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

- This discharge changes which expression fills the divisor slot (an unwritten field, not the firing event's own arg) rather than adding a line to the stated base's own shape — recorded as `structural-edit` per the schema's own kind vocabulary for this reason.
- The `application` DU (cell.schema.json # application) has no locus for this edit: it only models (i) appending a guard to a named event row via `row-guard` (which requires an `eventName`, and this edit attaches to a state target or a bare field/rule declaration, not an event row) and (ii) replacing a named event-arg declaration. Recording the edit in `addition` and leaving `application` absent, per the same open-vocabulary-item treatment the README already uses for `construction-defaults`.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| b-arg-positive | Divisor as decimal nonnegative | nonnegative admits zero — the interval [0, +inf) does not exclude it — must still reject, same obligation | reject, naming the same obligation |
| a-field-default-positive | replace the same two rows with: field Divisor as decimal nonnegative default 0.0 event Init(Numerator as decimal) initial on Init -> set Result = Init.Numerator % Divisor | the field's own default is now the zero literal itself, admitted by `nonnegative` — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- This cell also covers op/IntegerModuloDecimal/numeric-0: each declares the identical `NumericProofRequirement(ParamSubject(PDecimal), NotEquals, 0m, "Divisor must be non-zero")` on the same divisor-type-family subject as DecimalModuloDecimal (src/Precept/Language/Operations.cs), so the obligation schema, applicable-class set, and decision procedures below are identical up to which side of the operator widens; no separate cell is authored for them under the matrix's schema-unification criterion (matrix § The cell, item 1: 'Two programs sit in the same cell iff ... their minted obligations unify with this schema').

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- docs/Working/obligation-discharge-matrix-2026-07-19.md:186 — matrix: fault minting is catalog-stamped at evaluation sites
- docs/language/precept-language-spec.md:2064 — spec: construction is a special case — the entity is not yet in any state
- docs/language/precept-language-spec.md:1987 — spec: construction rows use `on <EventName>`, not `from <State> on <Event>`, because no state exists yet

## fault2/sh-decmod — Decimal modulo by zero, state entry/exit hook

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- docs/language/precept-language-spec.md:205 — spec: two-tier divisor safety

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | DecimalModuloDecimal |
| evaluation site category | state-hook-action-operand |
| type family | primitive |

### What must be proven

Obligation: Divisor != 0.0

Weakest precondition: Divisor != 0.0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Divisor | the divisor operand at this evaluation site — a field read by a `to S ->` / `from S ->` hook's own action list |
| 0.0 | the decimal zero value |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a), (c), (d).

A state hook fires on every inbound or outbound edge into/out of its named state, regardless of which row or event triggered the transition (spec § State action); the hook's own scope gives it field names only, no event args (spec:1348, the scope table's 'State action guard / actions \| All field names' row) — so premise (b) is structurally unavailable: there is no arg in scope to bound. What is available: (c), the hook's own optional `when` guard, evaluated the same way any guard is (guards select, spec:1897); (d), a pre-state constraint — the hook fires with a real pre-state, so the standing inductive hypothesis over the entity's reachable-configuration history applies; and (a), the divisor field's own declared modifier, carried into the hook's read whenever the hook's own action list does not write the field first. Classes (a) and (d) rest on the identical inductive mechanism here — the only difference is whether the zero-exclusion fact was declared as a modifier on the field or as a standalone `rule` — so both are argued from the same validity-argument text below, with a note on the narrower scope of its stated arithmetic.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: the hook's own `when` guard, evaluated before its actions run, restates the zero-exclusion fact directly
- Validity arguments: Guard normal-form match
- Decision procedure: normal-form match of `Divisor != <zero>` against the hook's own guard conjuncts (a finite set); no match rejects naming class (c).

- docs/language/precept-language-spec.md:1016 — spec: state actions support an optional `when` guard between the state target and the action chain
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Guard normal-form match

**Entry 2 — (a)**

- Derivation: the divisor field carries its own declared modifier (e.g. `nonzero`); that modifier is proven to hold across every write site of the field (a rule-establishment/preservation obligation on the field itself, a separate rule-write-family concern) and this hook's own action list does not write the field before the divisor is read, so the modifier's standing guarantee carries unchanged into the read — the same inductive mechanism premise (d) uses, without needing the sign-monotonicity half of that argument's text (there is no arithmetic transform here, only an unwritten read)
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: if the divisor field is not written by this hook's own action list before the read, look up its declared modifiers (the same finite modifier-to-ProofSatisfaction table class (b)/(a) use elsewhere); an interval excluding zero discharges, otherwise reject naming class (a).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Inductive hypothesis plus sign monotonicity — reused here for its inductive-hypothesis half only; the sign-monotonicity half is inapplicable and unneeded, since no write transforms the divisor between establishment and this read

- This premise's soundness depends on the divisor field's own establishment/preservation proof holding across every one of its write sites elsewhere in the file — exactly the dependency the matrix already names for premise (d) ('the hypothesis is only as sound as the minting rule is complete... a missed write site voids the hypothesis'). Recorded as a dependency, not re-derived here.

**Entry 3 — (d)**

- Derivation: an explicit `rule` states the divisor's zero-exclusion; that rule holds in the pre-state by the standing inductive hypothesis (establishment at construction, preservation at every write site of every mentioned field, atomic commit, spec:1967), and this hook's own action list does not write the divisor before the read, so the pre-state fact carries unchanged into the read
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: if the divisor field is not written by this hook's own action list before the read, look up a rule holding of it in the pre-state that excludes zero (the standing inductive hypothesis); none found rejects naming class (d).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Inductive hypothesis plus sign monotonicity — inductive-hypothesis half only, as for class (a) above
- docs/Working/obligation-discharge-matrix-2026-07-19.md:88 — matrix: Open design hole: nothing detects an obligation that was never minted — the same dependency this premise rests on
- docs/Working/obligation-discharge-matrix-2026-07-19.md:94 — matrix: the verified false-proof instance: a `rule` premise consumed via CompositionalConstraint while a different handler violates it, with zero diagnostics
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md — code: Part 3, Defect B — the same verified defect, worked in full

- HAZARD: this contract entry is consumed via a business `rule` used as a premise. Slice 2 verified at HEAD that this exact discharge shape (a rule stating a field's sign, consumed as premise (d) for an unrelated obligation) can be a false proof when a different handler sets the field to zero elsewhere in the file (matrix:94; authored-expressiveness-gaps.md Part 3, Defect B). This group's witness below has no such violating handler, so its clean compile is not itself an instance of the defect — but the discharge mechanism it exercises is the same one the defect implicates, so its builtStatus is recorded unresolved-today rather than proven-today, per this group's authoring instruction, regardless of the observed clean compile.

### What the failing diagnostic must suggest

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (a): declare a modifier on the divisor field (`positive`, `nonzero`, or an equivalent bound) that this hook does not overwrite, so its standing guarantee satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (d): state a rule that the divisor satisfies <WP>, holding in the pre-state and not written by this hook before the read
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept StateHookDecmod

state Start initial
state Done terminal

field Divisor as decimal default 0.0
field Result as decimal default 0.0

event Init initial
event Finish

on Init -> set Divisor = 0.0

from Start on Finish -> transition Done

to Done -> set Result = 100 % Divisor
```

Required outcome: reject, naming the missing premise classes (a), (c), (d), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI (release build, temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*c-hook-guard* — guard

- Addition: when Divisor != 0.0
- Premise classes: (c)
- Derivation: the hook's own guard normal-form-equal to the WP discharges by guard-match, the same mechanism as a transition row's guard, generalized to a state hook's `when` (spec § State action)
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (release build, temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

- The `application` DU's only guard locus (`row-guard`) requires an `eventName`, which a state hook does not have (it is anchored to a state target, spec § State action) — `application` is left absent for this reason, per the same open-vocabulary-item treatment the README uses elsewhere; the addition text is recorded in prose instead.

*a-field-nonzero-unwritten* — other

- Addition: field Divisor as decimal nonzero default 5.0  (replacing the base's unconstrained `field Divisor as decimal default 0.0`; the construction row's own `set Divisor = 0.0` is changed to `set Divisor = 5.0` to keep the row internally consistent with the field's own modifier)
- Premise classes: (a)
- Derivation: Divisor's own `nonzero` modifier is established at construction (over the literal it is set to) and this hook does not write Divisor before the read, so the modifier's standing guarantee carries unchanged into the read
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (release build, temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

- The `application` DU (cell.schema.json # application) has no locus for this edit: it only models (i) appending a guard to a named event row via `row-guard` (which requires an `eventName`, and this edit attaches to a state target or a bare field/rule declaration, not an event row) and (ii) replacing a named event-arg declaration. Recording the edit in `addition` and leaving `application` absent, per the same open-vocabulary-item treatment the README already uses for `construction-defaults`.

*d-rule-nonzero-unwritten* — other

- Addition: rule Divisor != 0.0 because "Divisor must never be zero — a zero divisor would make every downstream rate calculation undefined"  (with the field's own default changed from 0.0 to 5.0 and the construction row's `set Divisor = 5.0`, so the rule is establishable)
- Premise classes: (d)
- Derivation: the rule holds in the pre-state by the standing inductive hypothesis and this hook does not write Divisor before the read, so the pre-state fact carries unchanged into the read
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (HAZARD, per this group's authoring instruction: this discharge is consumed via a business `rule` premise, the same discharge shape the verified false-proof defect implicates (matrix:94; authored-expressiveness-gaps.md Part 3, Defect B — CompositionalConstraint accepting a rule as a premise with no check that some other handler violates it). The live run below in fact compiled clean (no DivisionByZero), but per the authoring instruction this is recorded as unresolved-today, not proven-today, because the discharge mechanism is not certified sound in general, even though this specific witness has no violating handler.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (release build, temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

- The `application` DU (cell.schema.json # application) has no locus for this edit: it only models (i) appending a guard to a named event row via `row-guard` (which requires an `eventName`, and this edit attaches to a state target or a bare field/rule declaration, not an event row) and (ii) replacing a named event-arg declaration. Recording the edit in `addition` and leaving `application` absent, per the same open-vocabulary-item treatment the README already uses for `construction-defaults`.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| c-hook-guard | when Divisor >= 0.0 | the guard admits zero — does not imply the WP — must still reject, same obligation | reject, naming the same obligation |
| a-field-nonzero-unwritten | field Divisor as decimal nonnegative default 0.0  (construction row keeps `set Divisor = 0.0`) | nonnegative admits the field's own default of zero — must still reject, same obligation | reject, naming the same obligation |
| d-rule-nonzero-unwritten | rule Divisor >= 0.0 because "Divisor must never be negative..."  (field default kept at 0.0) | the rule admits zero — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- This cell also covers op/IntegerModuloDecimal/numeric-0: each declares the identical `NumericProofRequirement(ParamSubject(PDecimal), NotEquals, 0m, "Divisor must be non-zero")` on the same divisor-type-family subject as DecimalModuloDecimal (src/Precept/Language/Operations.cs), so the obligation schema, applicable-class set, and decision procedures below are identical up to which side of the operator widens; no separate cell is authored for them under the matrix's schema-unification criterion (matrix § The cell, item 1: 'Two programs sit in the same cell iff ... their minted obligations unify with this schema').
- The matrix records as open whether the residency fact itself (the entity is in state S) is a premise, and under which class (matrix:150, the `in S ensure` case-shape row's Open bullet). This cell's own divisor obligation does not depend on residency — the discharges above rest on the field's own history, not on which state the entity is in — but the group's authoring instruction is to carry this open item on every state-hook cell rather than assume either answer, so it is recorded here. No packet Q/S identifier has been minted for this specific open item as of this pass (checked: review-1 through review-6 under obligation-discharge-matrix-2026-07-19-reviews/, and the want-analysis decision packet); `openDependencies` is left empty rather than filled with a fabricated identifier, and this note carries the dependency in prose instead.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- docs/language/precept-language-spec.md § State action — spec: the state-action grammar and its optional `when` guard
- docs/language/precept-language-spec.md:1348 — spec: the scope table: state action guard / actions see all field names, nothing else

## fault2/cr-numdiv — Number division by zero, construction row

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row: catalog-declared safety precondition at the evaluation site
- docs/language/precept-language-spec.md:205 — spec: two-tier divisor safety: a proven-zero divisor is a hard error; an unproven divisor is an obligation the author discharges with nonzero, positive, a rule, or a guard

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | NumberDivideNumber |
| evaluation site category | construction-row-action-operand |
| type family | primitive |

### What must be proven

Obligation: Divisor != 0.0

Weakest precondition: Divisor != 0.0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Divisor | the divisor operand at this evaluation site — an event arg of the firing construction row, or a field the row does not write |
| 0.0 | the number zero value |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a), (b).

There is no pre-state at a construction row — the entity does not yet exist in any state when the initial event fires (spec § Stateless/stateful cross-validation; spec:2064: 'Construction is a special case (the entity is not yet in any state)') — so premise (d) is structurally unavailable: there is nothing for the inductive hypothesis to range over. The handler's own guard (c) does not exist either: construction rows are the `on <Event> initial` form, not a guarded transition row. What remains is whichever of the two classes matches the divisor's own syntactic kind: (b) when the divisor is the firing event's own arg (governed at ingress before this row's actions run, spec:268), and (a) when the divisor is a field the row does not write, whose own declared modifier is established over the default configuration by the same reasoning as any other unwritten field (matrix § Validity arguments, 'Establishment over defaults').

### The discharge contract — exactly when this counts as proven

**Entry 1 — (b)**

- Derivation: governance enforces the event arg's declared modifier at ingress, before this row's actions read it (spec:268); the declared modifier gives an interval on the divisor that excludes zero
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: look up the divisor arg's declared modifiers (a finite set — the modifier-to-ProofSatisfaction table, matrix § Witness Family 1 Base B decision procedure); an interval that excludes zero discharges, otherwise reject naming class (b).

- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4 — fault family, arg-constraint discharge: the same divisor-arg-positive pattern, already worked as the matrix's own fault-family precedent

**Entry 2 — (a)**

- Derivation: the divisor field carries its own declared modifier (e.g. `positive`/`nonzero`); the firing construction row does not write this field, so its post-construction value is exactly its declared default (matrix § Validity arguments, 'Establishment over defaults'); the modifier's own establishment obligation over that default constant-folds (matrix § Validity arguments, 'Literal constant-fold for defaults')
- Validity arguments: Establishment over defaults; Literal constant-fold for defaults
- Decision procedure: for a divisor field with no write site in the firing construction row, constant-fold the field's declared modifier over its literal default (the fold terminates — expressions are finite, loop-free trees, spec § 0.4); a fold excluding zero discharges, otherwise reject naming class (a).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Establishment over defaults (the pre-configuration rule)

### What the failing diagnostic must suggest

- For class (a): declare a modifier on the divisor field (`positive`, `nonzero`, or an equivalent bound) that the firing construction row does not overwrite, so its established default satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (b): bound the divisor arg (`positive`, `nonzero`, or an equivalent bound) such that its declared interval satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept ConstructNumdiv

field Result as number default 0.0

event Init(Numerator as number, Divisor as number) initial

on Init -> set Result = Init.Numerator / Init.Divisor
```

Required outcome: reject, naming the missing premise classes (a), (b), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI (release build, temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*b-arg-positive* — arg-modifier

- Addition: Divisor as number positive
- Premise classes: (b)
- Derivation: governance enforces `Divisor positive` at ingress before the row reads it; the interval (0, +inf) excludes zero
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: applied by replacing the named event-arg declarations
  - `Init.Divisor` becomes `Divisor as number positive`

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (release build, temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

*a-field-default-positive* — structural-edit

- Addition: replace `event Init(Numerator as number, Divisor as number) initial` and `on Init -> set Result = Init.Numerator / Init.Divisor` with: field Divisor as number positive default 5.0 event Init(Numerator as number) initial on Init -> set Result = Init.Numerator / Divisor
- Premise classes: (a)
- Derivation: Divisor is unwritten by the construction row, so its post-construction value is its declared default; the field's own `positive` modifier constant-folds over that literal default to true, and the fold excludes zero
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (release build, temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

- This discharge changes which expression fills the divisor slot (an unwritten field, not the firing event's own arg) rather than adding a line to the stated base's own shape — recorded as `structural-edit` per the schema's own kind vocabulary for this reason.
- The `application` DU (cell.schema.json # application) has no locus for this edit: it only models (i) appending a guard to a named event row via `row-guard` (which requires an `eventName`, and this edit attaches to a state target or a bare field/rule declaration, not an event row) and (ii) replacing a named event-arg declaration. Recording the edit in `addition` and leaving `application` absent, per the same open-vocabulary-item treatment the README already uses for `construction-defaults`.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| b-arg-positive | Divisor as number nonnegative | nonnegative admits zero — the interval [0, +inf) does not exclude it — must still reject, same obligation | reject, naming the same obligation |
| a-field-default-positive | replace the same two rows with: field Divisor as number nonnegative default 0.0 event Init(Numerator as number) initial on Init -> set Result = Init.Numerator / Divisor | the field's own default is now the zero literal itself, admitted by `nonnegative` — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- This cell also covers op/IntegerDivideNumber/numeric-0, op/NumberModuloNumber/numeric-0, op/IntegerModuloNumber/numeric-0: each declares the identical `NumericProofRequirement(ParamSubject(PNumber), NotEquals, 0m, "Divisor must be non-zero")` on the same divisor-type-family subject as NumberDivideNumber (src/Precept/Language/Operations.cs), so the obligation schema, applicable-class set, and decision procedures below are identical up to which side of the operator widens; no separate cell is authored for them under the matrix's schema-unification criterion (matrix § The cell, item 1: 'Two programs sit in the same cell iff ... their minted obligations unify with this schema').

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- docs/Working/obligation-discharge-matrix-2026-07-19.md:186 — matrix: fault minting is catalog-stamped at evaluation sites
- docs/language/precept-language-spec.md:2064 — spec: construction is a special case — the entity is not yet in any state
- docs/language/precept-language-spec.md:1987 — spec: construction rows use `on <EventName>`, not `from <State> on <Event>`, because no state exists yet

## fault2/sh-numdiv — Number division by zero, state entry/exit hook

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- docs/language/precept-language-spec.md:205 — spec: two-tier divisor safety

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | NumberDivideNumber |
| evaluation site category | state-hook-action-operand |
| type family | primitive |

### What must be proven

Obligation: Divisor != 0.0

Weakest precondition: Divisor != 0.0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Divisor | the divisor operand at this evaluation site — a field read by a `to S ->` / `from S ->` hook's own action list |
| 0.0 | the number zero value |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a), (c), (d).

A state hook fires on every inbound or outbound edge into/out of its named state, regardless of which row or event triggered the transition (spec § State action); the hook's own scope gives it field names only, no event args (spec:1348, the scope table's 'State action guard / actions \| All field names' row) — so premise (b) is structurally unavailable: there is no arg in scope to bound. What is available: (c), the hook's own optional `when` guard, evaluated the same way any guard is (guards select, spec:1897); (d), a pre-state constraint — the hook fires with a real pre-state, so the standing inductive hypothesis over the entity's reachable-configuration history applies; and (a), the divisor field's own declared modifier, carried into the hook's read whenever the hook's own action list does not write the field first. Classes (a) and (d) rest on the identical inductive mechanism here — the only difference is whether the zero-exclusion fact was declared as a modifier on the field or as a standalone `rule` — so both are argued from the same validity-argument text below, with a note on the narrower scope of its stated arithmetic.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: the hook's own `when` guard, evaluated before its actions run, restates the zero-exclusion fact directly
- Validity arguments: Guard normal-form match
- Decision procedure: normal-form match of `Divisor != <zero>` against the hook's own guard conjuncts (a finite set); no match rejects naming class (c).

- docs/language/precept-language-spec.md:1016 — spec: state actions support an optional `when` guard between the state target and the action chain
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Guard normal-form match

**Entry 2 — (a)**

- Derivation: the divisor field carries its own declared modifier (e.g. `nonzero`); that modifier is proven to hold across every write site of the field (a rule-establishment/preservation obligation on the field itself, a separate rule-write-family concern) and this hook's own action list does not write the field before the divisor is read, so the modifier's standing guarantee carries unchanged into the read — the same inductive mechanism premise (d) uses, without needing the sign-monotonicity half of that argument's text (there is no arithmetic transform here, only an unwritten read)
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: if the divisor field is not written by this hook's own action list before the read, look up its declared modifiers (the same finite modifier-to-ProofSatisfaction table class (b)/(a) use elsewhere); an interval excluding zero discharges, otherwise reject naming class (a).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Inductive hypothesis plus sign monotonicity — reused here for its inductive-hypothesis half only; the sign-monotonicity half is inapplicable and unneeded, since no write transforms the divisor between establishment and this read

- This premise's soundness depends on the divisor field's own establishment/preservation proof holding across every one of its write sites elsewhere in the file — exactly the dependency the matrix already names for premise (d) ('the hypothesis is only as sound as the minting rule is complete... a missed write site voids the hypothesis'). Recorded as a dependency, not re-derived here.

**Entry 3 — (d)**

- Derivation: an explicit `rule` states the divisor's zero-exclusion; that rule holds in the pre-state by the standing inductive hypothesis (establishment at construction, preservation at every write site of every mentioned field, atomic commit, spec:1967), and this hook's own action list does not write the divisor before the read, so the pre-state fact carries unchanged into the read
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: if the divisor field is not written by this hook's own action list before the read, look up a rule holding of it in the pre-state that excludes zero (the standing inductive hypothesis); none found rejects naming class (d).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Inductive hypothesis plus sign monotonicity — inductive-hypothesis half only, as for class (a) above
- docs/Working/obligation-discharge-matrix-2026-07-19.md:88 — matrix: Open design hole: nothing detects an obligation that was never minted — the same dependency this premise rests on
- docs/Working/obligation-discharge-matrix-2026-07-19.md:94 — matrix: the verified false-proof instance: a `rule` premise consumed via CompositionalConstraint while a different handler violates it, with zero diagnostics
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md — code: Part 3, Defect B — the same verified defect, worked in full

- HAZARD: this contract entry is consumed via a business `rule` used as a premise. Slice 2 verified at HEAD that this exact discharge shape (a rule stating a field's sign, consumed as premise (d) for an unrelated obligation) can be a false proof when a different handler sets the field to zero elsewhere in the file (matrix:94; authored-expressiveness-gaps.md Part 3, Defect B). This group's witness below has no such violating handler, so its clean compile is not itself an instance of the defect — but the discharge mechanism it exercises is the same one the defect implicates, so its builtStatus is recorded unresolved-today rather than proven-today, per this group's authoring instruction, regardless of the observed clean compile.

### What the failing diagnostic must suggest

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (a): declare a modifier on the divisor field (`positive`, `nonzero`, or an equivalent bound) that this hook does not overwrite, so its standing guarantee satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (d): state a rule that the divisor satisfies <WP>, holding in the pre-state and not written by this hook before the read
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept StateHookNumdiv

state Start initial
state Done terminal

field Divisor as number default 0.0
field Result as number default 0.0

event Init initial
event Finish

on Init -> set Divisor = 0.0

from Start on Finish -> transition Done

to Done -> set Result = 100 / Divisor
```

Required outcome: reject, naming the missing premise classes (a), (c), (d), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI (release build, temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*c-hook-guard* — guard

- Addition: when Divisor != 0
- Premise classes: (c)
- Derivation: the hook's own guard normal-form-equal to the WP discharges by guard-match, the same mechanism as a transition row's guard, generalized to a state hook's `when` (spec § State action)
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (release build, temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

- The `application` DU's only guard locus (`row-guard`) requires an `eventName`, which a state hook does not have (it is anchored to a state target, spec § State action) — `application` is left absent for this reason, per the same open-vocabulary-item treatment the README uses elsewhere; the addition text is recorded in prose instead.

*a-field-nonzero-unwritten* — other

- Addition: field Divisor as number nonzero default 5.0  (replacing the base's unconstrained `field Divisor as number default 0.0`; the construction row's own `set Divisor = 0.0` is changed to `set Divisor = 5.0` to keep the row internally consistent with the field's own modifier)
- Premise classes: (a)
- Derivation: Divisor's own `nonzero` modifier is established at construction (over the literal it is set to) and this hook does not write Divisor before the read, so the modifier's standing guarantee carries unchanged into the read
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (release build, temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

- The `application` DU (cell.schema.json # application) has no locus for this edit: it only models (i) appending a guard to a named event row via `row-guard` (which requires an `eventName`, and this edit attaches to a state target or a bare field/rule declaration, not an event row) and (ii) replacing a named event-arg declaration. Recording the edit in `addition` and leaving `application` absent, per the same open-vocabulary-item treatment the README already uses for `construction-defaults`.

*d-rule-nonzero-unwritten* — other

- Addition: rule Divisor != 0 because "Divisor must never be zero — a zero divisor would make every downstream rate calculation undefined"  (with the field's own default changed from 0.0 to 5.0 and the construction row's `set Divisor = 5.0`, so the rule is establishable)
- Premise classes: (d)
- Derivation: the rule holds in the pre-state by the standing inductive hypothesis and this hook does not write Divisor before the read, so the pre-state fact carries unchanged into the read
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (HAZARD, per this group's authoring instruction: this discharge is consumed via a business `rule` premise, the same discharge shape the verified false-proof defect implicates (matrix:94; authored-expressiveness-gaps.md Part 3, Defect B — CompositionalConstraint accepting a rule as a premise with no check that some other handler violates it). The live run below in fact compiled clean (no DivisionByZero), but per the authoring instruction this is recorded as unresolved-today, not proven-today, because the discharge mechanism is not certified sound in general, even though this specific witness has no violating handler.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (release build, temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

- The `application` DU (cell.schema.json # application) has no locus for this edit: it only models (i) appending a guard to a named event row via `row-guard` (which requires an `eventName`, and this edit attaches to a state target or a bare field/rule declaration, not an event row) and (ii) replacing a named event-arg declaration. Recording the edit in `addition` and leaving `application` absent, per the same open-vocabulary-item treatment the README already uses for `construction-defaults`.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| c-hook-guard | when Divisor >= 0 | the guard admits zero — does not imply the WP — must still reject, same obligation | reject, naming the same obligation |
| a-field-nonzero-unwritten | field Divisor as number nonnegative default 0.0  (construction row keeps `set Divisor = 0.0`) | nonnegative admits the field's own default of zero — must still reject, same obligation | reject, naming the same obligation |
| d-rule-nonzero-unwritten | rule Divisor >= 0 because "Divisor must never be negative..."  (field default kept at 0.0) | the rule admits zero — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- This cell also covers op/IntegerDivideNumber/numeric-0, op/NumberModuloNumber/numeric-0, op/IntegerModuloNumber/numeric-0: each declares the identical `NumericProofRequirement(ParamSubject(PNumber), NotEquals, 0m, "Divisor must be non-zero")` on the same divisor-type-family subject as NumberDivideNumber (src/Precept/Language/Operations.cs), so the obligation schema, applicable-class set, and decision procedures below are identical up to which side of the operator widens; no separate cell is authored for them under the matrix's schema-unification criterion (matrix § The cell, item 1: 'Two programs sit in the same cell iff ... their minted obligations unify with this schema').
- The matrix records as open whether the residency fact itself (the entity is in state S) is a premise, and under which class (matrix:150, the `in S ensure` case-shape row's Open bullet). This cell's own divisor obligation does not depend on residency — the discharges above rest on the field's own history, not on which state the entity is in — but the group's authoring instruction is to carry this open item on every state-hook cell rather than assume either answer, so it is recorded here. No packet Q/S identifier has been minted for this specific open item as of this pass (checked: review-1 through review-6 under obligation-discharge-matrix-2026-07-19-reviews/, and the want-analysis decision packet); `openDependencies` is left empty rather than filled with a fabricated identifier, and this note carries the dependency in prose instead.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- docs/language/precept-language-spec.md § State action — spec: the state-action grammar and its optional `when` guard
- docs/language/precept-language-spec.md:1348 — spec: the scope table: state action guard / actions see all field names, nothing else

