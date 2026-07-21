<!--
GENERATED FILE — do not hand-edit.
Source: fault-12-function-preconditions.cells.json
Regenerate: dotnet run --project tools/Precept.MatrixTools -- render-cells docs/Working/obligation-discharge-matrix-2026-07-19-cells
Hand edits will be overwritten. Edit the .cells.json and regenerate.
-->

# Fault family, group 12 — function preconditions: sqrt's non-negative argument, pow's non-negative integer exponent

Family id: fault-12-function-preconditions
Case shape: fault
Definition version: obligation-discharge-matrix-2026-07-19 rev 7 (2026-07-20, draft)

**Where this family's content comes from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes (a discriminated union of axis sets) — matrix: the Fault family row: catalog-declared safety precondition at the evaluation site; premise classes (b)/(c)/(a) per catalog ProofSatisfactions
- docs/Working/obligation-discharge-matrix-2026-07-19.md:186 — matrix: fault minting is catalog-stamped at evaluation sites; settled and shipped for the fault family
- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: the fault family gets the identical premise-and-certificate treatment as business rules
- docs/Working/what-i-want-2026-07-16.md:72 — want-doc: a fault expression can sit in a reject-row interpolation with no write at all
- src/Precept/Language/Functions.cs:183 — code: FunctionKind.Pow and FunctionKind.Sqrt catalog entries and their NumericProofRequirement declarations

## Respellability — the family verdict

Can every safe program these rules reject be rewritten into a form they accept? **yes**

Evidence: model-derived
Corpus measurement: pending

**Notes on the family verdict**

- No fault-family-specific respellability paragraph is written in the matrix yet; the verdict here is authored by extension of the same general argument the matrix states for the rule-write family (Witness Family 1), not restated matrix text. Flagged so the extension is findable, not silently absorbed as if the matrix already said it for this family.
- Representative band member (both cells' sqrt/pow forms): a guard spelled with a cancelling additive term, e.g. `when Delta + 1 >= 1` for sqrt or `when Exponent + 2 >= 2` for pow — algebraically equivalent to the licensed `>= 0` form but not reachable by the matrix's two stated normalization rules (comparison-direction flip; commutative-operand reorder). Respelling: the direct bound, `when Delta >= 0` / `when Exponent >= 0`.

**Sources for this verdict**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the sound-but-unprovable band is spelling no bounded derivation covers — algebraic rearrangement — not alternate spellings of covered premise shapes
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witness Family 1 — matrix: the respellability paragraph this group's verdict is authored by extension of

## Premise classes — the legend

Where a proof may get its facts (want :19, :143): (a) field modifiers, (b) arg constraints, (c) the handler's guard, (d) all rules holding in the pre-state (the inductive hypothesis), (e) evaluating the applicable rule set at an editable-field write — (e)'s status as a genuinely fifth class vs (a) generalized is an open owner item.

## g12/sqrt-write — sqrt argument non-negativity at a transition-row write

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | Sqrt |
| evaluation site category | transition-row-action-operand |
| type family | primitive |

### What must be proven

Obligation: sqrt(Value) where Value >= 0

Weakest precondition: Value >= 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Value | the number-lane expression passed as sqrt's sole argument at this write site |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a), (b), (c).

The evaluation site is an action operand in a transition row: the row's guard has already evaluated true and the triggering event's args are in scope, so classes (a) field modifiers, (b) argument constraints, and (c) the handler's guard can all supply the non-negativity fact directly on Value or on the field/arg it reads. Class (d) is a separate, unresolved question for this site — see notes.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: guard fact normal-form-equal to the WP (Value >= 0) discharges via GuardInPath's per-term bound match
- Strategy: GuardInPath
- Validity arguments: Guard normal-form match
- Decision procedure: Live-verified decision procedure (src/Precept/Pipeline/ProofEngine.Strategies.cs:713-787, GuardSubsumes at :971): the guard's conjuncts are extracted as (field, comparison, literal) constraints; a conjunct whose field matches Value's field/arg name and whose (comparison, literal) pair subsumes (>=, 0) discharges. No matching conjunct in any AND-branch of the guard rejects.

**Entry 2 — (b)**

- Derivation: the event arg Value reads is declared nonnegative, enforced at ingress before the handler runs
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Same GuardSubsumes machinery reads the arg's declared modifier as a standing constraint rather than a guard conjunct; a declared bound that does not subsume (>=, 0) fails the class.

- src/Precept/Pipeline/ProofEngine.Strategies.cs:971 — code: GuardSubsumes / NumericConstraintSubsumes — the shared (field, comparison, literal) matcher underlying both (b) and (c)

**Entry 3 — (a)**

- Derivation: the field Value reads (when Value is a bare field reference, not a compound expression) carries a nonnegative/min-0 modifier
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Same (field, comparison, literal) matcher, sourced from the field's own declared modifiers rather than the event arg's; a field with no qualifying bound fails the class.

### What the failing diagnostic must suggest

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (b): bound the args such that the interval combination satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (a): declare the field with a modifier bound satisfying <WP>
  - The matrix's Vocabulary section states the (b)/(c) schemas verbatim but no explicit (a) schema text; this schema is authored here by the same pattern, not quoted from the matrix.


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept SqrtWriteBase

field MeasurementRoot as number default 0

event Adjust(Delta as number)

on Adjust
    -> set MeasurementRoot = sqrt(Adjust.Delta)
```

Required outcome: reject, naming the missing premise classes (a), (b), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*guard-nf* — guard

- Addition: when Adjust.Delta >= 0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the WP -> GuardInPath
- Strategy: GuardInPath
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: appended to the guard of the `on Adjust` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

*arg-mod* — arg-modifier

- Addition: Delta as number nonnegative
- Premise classes: (b)
- Derivation: arg ingress bound Delta >= 0 -> GuardInPath (reads declared modifier)
- Strategy: GuardInPath
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: applied by replacing the named event-arg declarations
  - `Adjust.Delta` becomes `Delta as number nonnegative`

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| guard-nf | when Adjust.Delta > -100 | the guard bounds Delta only below -100, not at 0 — must still reject, same obligation | reject, naming the same obligation |
| arg-mod | Delta as number min -100 | the declared floor is -100, not 0 — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- HAZARD: a rule stated over the field sqrt reads is a distinct, unlicensed-by-the-matrix discharge path that the shipped compiler nonetheless accepts. Live-verified demonstration at this exact evaluation-site category (commit e1a14d91, 2026-07-21, Precept.MatrixTools CLI): `field Delta as number default 0`, `field MeasurementRoot as number default 0`, `rule Delta >= 0 because "Delta must never be negative"`, `event Drift(Amount as number)`, `event Adjust(NewDelta as number nonnegative)`, `on Adjust -> set Delta = Adjust.NewDelta -> set MeasurementRoot = sqrt(Delta)`, `on Drift -> set Delta = Drift.Amount` compiles with **zero diagnostics** even though `Drift` drives `Delta` negative completely unguarded and `Adjust` then takes `sqrt(Delta)` — the CompositionalConstraint strategy accepts the rule as a premise for the fault obligation. This is the same shape as the verified false-proof in authored-expressiveness-gaps.md Part 3, Defect B, reproduced here for sqrt specifically. No discharge witness citing this path is recorded above — see missingRules.
- Missing rule, recorded rather than resolved: the matrix's own fault-family case-shape row states premise classes '(b)/(c)/(a) per catalog ProofSatisfactions' with no (d). Whether a pre-state rule is ever a licensed premise for a fault obligation — and if so, under what derivation and validity argument — is not stated. Until that is ruled, the live behaviour demonstrated above is recorded as a verified defect (an acceptance the current contract text does not license), not as a fourth discharge-contract entry.
- wp.canonicalKey is omitted throughout this file: tools/Precept.MatrixTools's WP calculator (WpCalculator.ComputeEstablishmentWp/ComputePreservationWp) prints canonical keys only for authored `rule` invariants, not for fault-family ProofRequirement obligations — confirmed by running these witnesses through the tool, whose 'Establishment/Preservation' printout addresses the file's own `rule` declarations only, never the sqrt/pow precondition itself. No canonical key exists to record without inventing one.

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc
- src/Precept/Language/Functions.cs:200 — code

## g12/sqrt-construction — sqrt argument non-negativity at a construction row

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | Sqrt |
| evaluation site category | construction-row-action-operand |
| type family | primitive |

### What must be proven

Obligation: sqrt(Value) where Value >= 0

Weakest precondition: Value >= 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Value | the number-lane expression passed as sqrt's sole argument in the construction row (the handler for an event marked initial) |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a), (b), (c).

The entity does not yet exist before a construction row fires, so there is no pre-state: class (d) is structurally unavailable here, free of the hazard the write/state-hook/some-guard cells carry (there is no prior configuration a rule could have been established over). Field modifiers (a), the constructing event's arg constraints (b), and the construction row's own guard (c) remain available and, live-verified below, functional.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: the construction row's own guard, normal-form-equal to the WP, discharges via GuardInPath
- Strategy: GuardInPath
- Validity arguments: Guard normal-form match
- Decision procedure: Same GuardSubsumes matcher as the write-tier cell (src/Precept/Pipeline/ProofEngine.Strategies.cs:713-787): the construction row's guard branches are extracted and checked for a conjunct subsuming (>=, 0) on Value's field/arg.

**Entry 2 — (b)**

- Derivation: the constructing event's arg Value reads is declared nonnegative at ingress
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Same matcher, sourced from the arg's declared modifier.

**Entry 3 — (a)**

- Derivation: a field already materialised earlier in the same construction row (declaration order) carries a qualifying modifier, and Value reads that field
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Same matcher, sourced from the field's declared modifiers.

### What the failing diagnostic must suggest

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix

- For class (b): bound the args such that the interval combination satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix

- For class (a): declare the field with a modifier bound satisfying <WP>
  - Authored here by the same pattern as the (b)/(c) schemas the matrix states; not matrix-quoted text.


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept SqrtConstructionBase

field MeasurementRoot as number default 0

event Open(Reading as number) initial

on Open
    -> set MeasurementRoot = sqrt(Open.Reading)
```

Required outcome: reject, naming the missing premise classes (a), (b), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*guard-nf* — guard

- Addition: when Open.Reading >= 0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the WP -> GuardInPath
- Strategy: GuardInPath
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: appended to the guard of the `on Open` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

*arg-mod* — arg-modifier

- Addition: Reading as number nonnegative
- Premise classes: (b)
- Derivation: arg ingress bound Reading >= 0 -> GuardInPath
- Strategy: GuardInPath
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: applied by replacing the named event-arg declarations
  - `Open.Reading` becomes `Reading as number nonnegative`

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| guard-nf | when Open.Reading > -50 | the guard bounds Reading only below -50, not at 0 — must still reject, same obligation | reject, naming the same obligation |
| arg-mod | Reading as number min -50 | the declared floor is -50, not 0 — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Free of the premise-(d) hazard by construction: no prior configuration exists for a rule to have been established over before this row fires.
- wp.canonicalKey omitted — see the write-tier cell's note; the WP calculator does not compute fault-family canonical keys.

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc
- src/Precept/Language/Functions.cs:200 — code

## g12/sqrt-state-hook — sqrt argument non-negativity at a state entry hook

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | Sqrt |
| evaluation site category | state-hook-action-operand |
| type family | primitive |

### What must be proven

Obligation: sqrt(Value) where Value >= 0

Weakest precondition: Value >= 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Value | the number-lane expression passed as sqrt's sole argument in a state entry/exit hook action |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a), (c).

A hook fires on every inbound/outbound edge regardless of which row triggered it, so the triggering row's own guard and args are not in scope for it — class (b) is structurally unavailable and, live-verified below, a guard on the ENTERING TRANSITION ROW does not discharge the hook's obligation. What IS available: (a) a modifier declared directly on the field the hook reads (enforced at every write site via IntervalContainment, independent of which row wrote it), and (c) the hook's own optional pre-verb `when`, which live-verifies as a working discharge distinct from the entering row's guard.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: the hook's own pre-verb guard, normal-form-equal to the WP, discharges via GuardInPath (StateHookContext.Hook.Guard)
- Strategy: GuardInPath
- Validity arguments: Guard normal-form match
- Decision procedure: Same GuardSubsumes matcher (src/Precept/Pipeline/ProofEngine.Strategies.cs:723-787), reading the guard off StateHookContext rather than TransitionRowContext.

- src/Precept/Pipeline/ProofEngine.Strategies.cs:723 — code

**Entry 2 — (a)**

- Derivation: the field Value reads carries a qualifying modifier (e.g. nonnegative), which IntervalContainment enforces at every write site of the field regardless of which handler wrote it
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Same (field, comparison, literal) matcher, sourced from the field's declared modifier.

### What the failing diagnostic must suggest

- For class (c): add a guard normal-form-equal to <WP> on the hook itself (to/from S when <WP>)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix

  - The entering transition row's own guard does not discharge this obligation; the suggestion must point at the hook's guard, not the row's.

- For class (a): declare the field with a modifier bound satisfying <WP>
  - Authored here by the same pattern as the (b)/(c) schemas the matrix states; not matrix-quoted text.


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept SqrtStateHookBase

field Reading as number default 0
field MeasurementRoot as number default 0

state Idle initial
state Active terminal

event Advance(NewReading as number)

from Idle on Advance
    -> set Reading = Advance.NewReading
    -> transition Active

to Active -> set MeasurementRoot = sqrt(Reading)
```

Required outcome: reject, naming the missing premise classes (a), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*hook-guard-nf* — guard

- Addition: to Active when Reading >= 0 -> set MeasurementRoot = sqrt(Reading)
- Premise classes: (c)
- Derivation: the hook's own guard, normal-form-equal to the WP -> GuardInPath
- Strategy: GuardInPath
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: appended to the guard of the `on __state_hook_to_Active` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- The `application` locus vocabulary (row-guard/event-arg-declarations) has no member for 'entry hook's own guard clause' — this addition is recorded as the full replacement hook line rather than through the schema's application DU, an open vocabulary gap flagged rather than forced into the wrong locus.

*field-mod* — arg-modifier

- Addition: field Reading as number default 0 nonnegative
- Premise classes: (a)
- Derivation: field modifier Reading >= 0, enforced at every write site by IntervalContainment -> GuardInPath reads it as a standing fact
- Strategy: GuardInPath
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- `kind: arg-modifier` is a schema-vocabulary compromise: the addition is a FIELD modifier, not an event-arg modifier, and the application DU's `event-arg-declarations` locus does not fit a field declaration either. Recorded as free text; flagged as an open vocabulary gap rather than mis-fit into an existing locus.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| hook-guard-nf | to Active when Reading > -50 -> set MeasurementRoot = sqrt(Reading) | the hook's guard bounds Reading only below -50, not at 0 — must still reject, same obligation | reject, naming the same obligation |
| field-mod | field Reading as number default 0 min -50 | the declared floor is -50, not 0 — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Live-verified negative finding: a guard on the ENTERING TRANSITION ROW (`from Idle on Advance when Advance.NewReading >= 0`) does NOT discharge this obligation, nor does a `nonnegative` modifier on the triggering event's arg — both compiled and still rejected with SqrtOfNegative (2026-07-21, commit e1a14d91). This matches the premise-availability argument: the hook fires on every inbound edge, and the row that happened to trigger it is not distinguished from any other inbound row, so its guard cannot be assumed by the hook.
- HAZARD: a bare `rule` stating the field's non-negativity IS accepted as a discharge at this site, and it is unsound in the same way as the write-tier cell's hazard note. Live-verified demonstration (commit e1a14d91, 2026-07-21): `field Reading as number default 0`, `rule Reading >= 0 because "Reading must never be negative"`, a second unguarded event `Drift(Amount as number) -> set Reading = Drift.Amount -> transition Active` alongside the guarded `Advance`, compiles with **zero diagnostics** even though `Drift` can drive `Reading` negative with nothing checking it, and the hook then takes `sqrt(Reading)`. Distinguishing this from the field-modifier discharge above: a `nonnegative` MODIFIER is enforced at every write site via IntervalContainment (Defect A in authored-expressiveness-gaps.md does not apply to modifiers), whereas a bare `rule` mints no write-site obligation at all at HEAD — so the modifier discharge is sound today and the rule-premise discharge is not. No discharge witness citing the rule path is recorded above; see the write-tier cell's missing-rule note, which applies identically here.
- wp.canonicalKey omitted — see the write-tier cell's note.

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc
- src/Precept/Language/Functions.cs:200 — code

## g12/sqrt-guard — sqrt argument non-negativity inside a guard expression

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | Sqrt |
| evaluation site category | transition-row-guard |
| type family | primitive |

### What must be proven

Obligation: sqrt(Value) where Value >= 0

Weakest precondition: Value >= 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Value | the number-lane expression passed as sqrt's sole argument inside a guard's boolean expression |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a), (b).

This cell stands for the five guard-shaped evaluation-site categories the group's coordinate space carries (transition-row-guard, state-hook-guard, access-mode-guard, ensure-activation-guard, rule-activation-guard) plus reject-message-interpolation, folded in here because a reject row's message is the sibling arm of the same guarded row. A guard cannot discharge an obligation arising inside its own expression — that is this axis's headline asymmetry — so class (c) is unavailable at every one of these positions even though the identical expression IS class (c) for the row's action operands. Field modifiers (a) and, on categories with event args in scope (transition-row-guard, ensure-activation-guard on the event-anchored form, reject-message-interpolation), argument constraints (b) remain the only classes the model licenses. Class (d) reasoning is a separate, unresolved question flagged in notes, distinct per sub-category (see notes).

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: the field Value reads carries a qualifying modifier
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: The same (field, comparison, literal) matcher named in the write-tier cell, applied under the model to a guard-position obligation once the site mints one.

**Entry 2 — (b)**

- Derivation: the event arg Value reads is declared with a qualifying bound at ingress
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Same matcher, sourced from the declared arg modifier, under the model.

### What the failing diagnostic must suggest

- For class (a): declare the field with a modifier bound satisfying <WP>
  - Authored by extension; not matrix-quoted text.

- For class (b): bound the args such that the interval combination satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept SqrtGuardBase

field MeasurementRoot as number default 0

event Adjust(Delta as number)

on Adjust when sqrt(Adjust.Delta) >= 0
    -> set MeasurementRoot = 1
```

Required outcome: reject, naming the missing premise classes (a), (b), with no other diagnostics

Provenance: model-derived.

**Discharge additions — what makes the base compile.**

*arg-mod* — arg-modifier

- Addition: Delta as number nonnegative
- Premise classes: (b)
- Derivation: arg ingress bound Delta >= 0, under the model
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (the guard position mints no fault obligation at all at HEAD — base and this addition both compile clean with zero diagnostics, so nothing here confirms a discharge; it confirms only that the site is silent)
- How it applies to the base: applied by replacing the named event-arg declarations
  - `Adjust.Delta` becomes `Delta as number nonnegative`

Required outcome: accept, with the premise list recorded

Provenance: model-derived.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| arg-mod | Delta as number min -100 | under the model, the declared floor is -100, not 0, so this must still reject with the same obligation — untestable at HEAD since the base itself does not reject here | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Live-verified, 2026-07-21, commit e1a14d91: `on Adjust when sqrt(Adjust.Delta) >= 0 -> set MeasurementRoot = 1` with `Delta` completely unbounded compiles with ZERO diagnostics. This matches the disposition pass's finding that all five guard-shaped categories mint no fault obligation at HEAD today. Per the matrix's fault-minting rule (docs/Working/obligation-discharge-matrix-2026-07-19.md:186) and the want doc's fault-family paragraph (want :104), the model commits to an obligation existing here; the silence is a built-status gap, not a model exclusion. Because baseWitness carries no explicit builtStatus field in the schema, this gap is recorded here in prose rather than left implicit in a provenance mark that could be misread as confirming a rejection.
- reject-message-interpolation is folded into this cell rather than given its own: live-verified separately (2026-07-21, commit e1a14d91), `on Adjust -> reject "root would be {sqrt(Adjust.Delta)}"` with Delta unbounded compiles clean (only an unrelated FieldNeverSet warning, no fault diagnostic) — the same non-minting result as the guard positions. HAZARD applies to it per the authoring instructions carried into this group.
- HAZARD (per the authoring instructions carried into this group): transition-row-guard, state-hook-guard, and access-mode-guard are named explicitly as hazard-relevant; reject-message-interpolation is named alongside them. This cell could not independently verify a false-proof reproduction at these positions, since none of them mint any obligation at HEAD to be falsely proved in the first place — the hazard is a model-level premise-availability concern (should the site start minting, would a bare rule be wrongly accepted the way it is at write/state-hook sites), not a currently-observable HEAD behaviour here. Recorded as stated in the authoring instructions, not independently reproduced.
- ensure-activation-guard and rule-activation-guard are also folded into this cell's evaluation-site coordinate but were NOT named in either the hazard or the free list the authoring instructions gave. Recorded as an open item rather than assigned a guess: rule-activation-guard has no event args and, per the spec, cannot see a handler guard or the pre-state constraints of an anchored handler either — whether a pre-state rule is even meaningfully 'available' to a rule's own activation guard is a different, unopened question the matrix does not address (it addresses premise (d) for handler-anchored sites only). ensure-activation-guard shares the same self-discharge exclusion as the other four guard positions. Neither is asserted hazard or free here; both are left as stated coordinates with this ambiguity flagged rather than resolved by guess. See missingRules.
- wp.canonicalKey omitted — see the write-tier cell's note.

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc
- src/Precept/Language/Functions.cs:200 — code

## g12/sqrt-constraint — sqrt argument non-negativity inside a constraint's own condition

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | Sqrt |
| evaluation site category | rule-condition |
| type family | primitive |

### What must be proven

Obligation: sqrt(Value) where Value >= 0

Weakest precondition: Value >= 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Value | the number-lane expression passed as sqrt's sole argument inside a rule's condition, a computed field's expression, or a quantifier's predicate |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a).

This cell stands for rule-condition, computed-field-expression, and quantifier-predicate — live-verified below to share one discharge story — plus constraint-rationale-interpolation (folded in; confirmed non-minting). None of these three is anchored to a specific handler: a rule condition is checked against every reachable configuration, a computed field's expression is re-evaluated on every read, and a quantifier's predicate inherits its host's premise set plus the collection's element bound. None has a handler guard or event args in scope, and there is no single pre-state to appeal to, so only class (a) field/element modifiers are available without further ruling. Live-verified: an event-arg modifier on the handler that writes the underlying field does NOT discharge here (only the field's own modifier does) — the event that writes the field is not privileged over any other write, which is exactly why no per-handler class (b)/(c) applies to a condition evaluated over every configuration.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: the field/element Value reads carries a qualifying modifier
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Live-verified decision procedure: the same (field, comparison, literal) matcher as the write-tier cell, sourced from the field's own declared modifier (or, for quantifier-predicate, the collection's inner-type value modifier on the binding variable).

### What the failing diagnostic must suggest

- For class (a): declare the field (or the collection's inner-type value) with a modifier bound satisfying <WP>
  - Authored by extension; not matrix-quoted text.


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept SqrtRuleConditionBase

field Delta as number default 0
field MeasurementRoot as number default 0

rule sqrt(Delta) >= 0 because "root must be real"

event Adjust(NewDelta as number)

on Adjust
    -> set Delta = Adjust.NewDelta
    -> set MeasurementRoot = Delta
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*field-mod* — arg-modifier

- Addition: field Delta as number default 0 nonnegative
- Premise classes: (a)
- Derivation: field modifier Delta >= 0
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- `kind: arg-modifier` and the absence of an `application` locus are schema-vocabulary compromises: this addition is a field-declaration modifier with no event-arg/row locus in the application DU. Recorded as free text (mirrors the state-hook cell's same gap).

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| field-mod | field Delta as number default 0 min -5 | the declared floor is -5, not 0 — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Live-verified negative finding, 2026-07-21, commit e1a14d91: declaring the WRITING EVENT's arg `NewDelta as number nonnegative` (leaving the field Delta itself unbounded) does NOT discharge — SqrtOfNegative still fires. Only a modifier on the field ITSELF discharges. This is the concrete evidence behind the premise-availability argument above.
- computed-field-expression, live-verified separately (2026-07-21, commit e1a14d91): `field MeasurementRoot as number <- sqrt(Delta)` mints the identical obligation, and a `nonnegative` modifier on `Delta` discharges it the same way. Folded into this cell rather than given its own — same obligation shape, same discharge story, confirmed independently rather than assumed.
- quantifier-predicate, live-verified separately (2026-07-21, commit e1a14d91): `rule each x in Readings (sqrt(x) >= 0)` (with `Readings as set of number`) mints the identical obligation — despite collection-types.md:10 marking quantifier predicates as a whole 'not yet built' for the RULE-family establishment/preservation machinery, the FAULT-family evaluation-site scan does run inside the predicate. Discharge status differs from rule-condition/computed-field, however: declaring `Readings as set of number nonnegative` (the collection's inner-type value modifier, which collection-types.md:628 states carries the bound onto the quantifier's binding variable) did NOT discharge at HEAD — SqrtOfNegative still fired with the modifier present. Recorded as modelStatus provable-under-model (collection-types.md:628 licenses it), builtStatus unresolved-today, live-verified as a negative result (not folded into this cell's discharge list above, which covers the rule-condition/computed-field field-modifier story that DOES work; the quantifier's own inner-type-modifier discharge is a distinct, currently-unresolved contract entry this cell does not claim).
- constraint-rationale-interpolation, live-verified (2026-07-21, commit e1a14d91): `rule Delta >= -5 because "root would be {sqrt(Delta)}"` compiles clean with zero diagnostics regardless of Delta's bound — the rationale interpolation does not mint, matching the authoring instructions' FREE-of-hazard classification for it (mint-vs-hazard: free here because there is nothing to falsely prove, not because a hazard was checked and found absent).
- state-ensure-condition and event-ensure-condition sit in this cell's coordinate group under the task's six-setting structure ('a constraint condition' is one setting), but their premise story is NOT identical to rule-condition/computed-field/quantifier's: the authoring instructions carried into this group name them HAZARD, meaning (unlike this cell's three live-verified siblings) a pre-state rule fact is available and, by the same pattern verified at the write and state-hook tiers, would be expected to falsely discharge via CompositionalConstraint if a `rule` is used as premise there. Neither was independently live-verified in this pass — the analogy to the write/state-hook hazard demonstrations is asserted, not measured, and is recorded as such rather than guessed at face value. A discharge witness for these two categories citing a `rule` would need builtStatus unverified with this reason named, per the group's authoring instructions; none is added here since it was not run. See missingRules.
- wp.canonicalKey omitted — see the write-tier cell's note.

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc
- src/Precept/Language/Functions.cs:200 — code

## g12/sqrt-declaration — sqrt argument non-negativity inside a declaration-position value expression

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | Sqrt |
| evaluation site category | field-default-value-expression |
| type family | primitive |

### What must be proven

Obligation: sqrt(Value) where Value >= 0

Weakest precondition: Value >= 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Value | the number-lane expression passed as sqrt's sole argument inside a field default, a field/event-arg constraint-modifier value, a collection inner-type value modifier, or a type-qualifier expression |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a).

This cell stands for all five declaration-position categories (field-default-value-expression, field-modifier-value-expression, event-arg-modifier-value-expression, collection-inner-type-modifier-value-expression, type-qualifier-expression). None has a guard, event args in a handler sense, or a pre-state; a default/modifier/qualifier expression may only read fields declared earlier (a forward reference is impossible), so the only class available is (a) — a modifier on one of those earlier fields, or, for a literal Value, the constant-fold derivation the matrix already states for establishment over defaults.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: Value is a literal, or reads a field declared earlier that carries a qualifying modifier
- Validity arguments: Literal constant-fold for defaults; Arg-bound interval arithmetic
- Decision procedure: Under the model: constant-fold Value if it is a literal (terminates over finite, loop-free expression trees); otherwise the (field, comparison, literal) matcher named in the write-tier cell, sourced from the earlier field's declared modifier.

### What the failing diagnostic must suggest

- For class (a): supply a literal value satisfying <WP>, or declare the earlier field with a modifier bound satisfying <WP>
  - Authored by extension; not matrix-quoted text.


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept SqrtDeclarationDefaultLit

field MeasurementRoot as number default sqrt(-4)
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: model-derived.

**Discharge additions — what makes the base compile.**

*literal-fold* — other

- Addition: field MeasurementRoot as number default sqrt(4)
- Derivation: under the model: constant-fold sqrt(4) >= 0 over the literal default (replaces the base's literal, from -4 to 4)
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (field-default-value-expression mints no fault obligation at all at HEAD, so there is nothing here for a fold to discharge — see notes)

Required outcome: accept, with the premise list recorded

Provenance: model-derived.

- No `application` locus fits 'replace a field declaration's default-value expression' — the application DU covers row-guard and event-arg-declarations only. Recorded as free text; the same open vocabulary gap flagged on the state-hook and constraint cells' field-modifier additions. `kind: other` used rather than `default-constant-fold` because this addition DOES change authored text (the literal), unlike a true standing discharge.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| literal-fold | field MeasurementRoot as number default sqrt(-1) | under the model, -1 is still negative — must still reject, same obligation — untestable at HEAD since the base itself does not reject here | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Live-verified, 2026-07-21, commit e1a14d91: `field MeasurementRoot as number default sqrt(-4)` compiles with zero diagnostics — no SqrtOfNegative, no error of any kind. A second run with `default sqrt(Seed)` reading an earlier unbounded field default confirmed the same silence. field-default-value-expression mints no fault obligation at HEAD.
- field-modifier-value-expression and event-arg-modifier-value-expression, live-verified separately (2026-07-21, commit e1a14d91): `field MeasurementRoot as number max sqrt(Delta)` and `event Adjust(Cap as number max sqrt(Delta))`, both with `Delta` defaulting to -4, compile with no SqrtOfNegative diagnostic either. All three of the five declaration categories that were measured mint nothing at HEAD.
- collection-inner-type-modifier-value-expression and type-qualifier-expression were NOT tested for sqrt in this pass. Not asserted either minting or non-minting; recorded as unmeasured rather than inferred from the three that were measured.
- This cell's discharge is recorded entirely under the model (modelStatus provable-under-model, builtStatus unresolved-today) precisely because the site mints nothing to discharge at HEAD — the standing default-constant-fold discharge (kind: default-constant-fold, addition: null) is the schema's own vocabulary for 'no authored addition exists', which fits this case for a different reason than family 1's (there, the fold is proven-today; here, the site never mints in the first place).
- wp.canonicalKey omitted — see the write-tier cell's note.

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc
- src/Precept/Language/Functions.cs:200 — code

## g12/pow-write — pow integer-overload exponent non-negativity at a transition-row write

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | Pow |
| evaluation site category | transition-row-action-operand |
| type family | primitive |

### What must be proven

Obligation: pow(Base, Exponent) where Exponent >= 0, and only when both Base and Exponent resolve to the integer overload (integer, integer) -> integer

Weakest precondition: Exponent >= 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Base | pow's first argument; the precondition below is independent of its value |
| Exponent | pow's second argument, at this write site — the requirement attaches ONLY to overload 0 ([Integer,Integer] -> Integer); the decimal and number overloads carry no such requirement |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a), (b), (c).

Identical structure to g12/sqrt-write, substituting the integer-overload exponent for sqrt's argument: the row's guard has already evaluated true and the triggering event's args are in scope, so (a), (b), and (c) can all supply the non-negativity fact. Class (d) reasoning is the same open/hazard question flagged there.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: guard fact normal-form-equal to the WP (Exponent >= 0) discharges via GuardInPath
- Strategy: GuardInPath
- Validity arguments: Guard normal-form match
- Decision procedure: Same decision procedure as g12/sqrt-write (src/Precept/Pipeline/ProofEngine.Strategies.cs:713-787).

**Entry 2 — (b)**

- Derivation: the event arg Exponent reads is declared nonnegative at ingress
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Same matcher, sourced from the declared arg modifier.

**Entry 3 — (a)**

- Derivation: the field Exponent reads (when it is a bare field reference) carries a qualifying modifier
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Same matcher, sourced from the field's declared modifiers.

### What the failing diagnostic must suggest

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix

- For class (b): bound the args such that the interval combination satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix

- For class (a): declare the field with a modifier bound satisfying <WP>
  - Authored by extension; not matrix-quoted text.


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept PowWriteBase

field ScaleFactor as integer default 1

event Rescale(Exponent as integer)

on Rescale
    -> set ScaleFactor = pow(2, Rescale.Exponent)
```

Required outcome: reject, naming the missing premise classes (a), (b), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*guard-nf* — guard

- Addition: when Rescale.Exponent >= 0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the WP -> GuardInPath
- Strategy: GuardInPath
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: appended to the guard of the `on Rescale` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

*arg-mod* — arg-modifier

- Addition: Exponent as integer nonnegative
- Premise classes: (b)
- Derivation: arg ingress bound Exponent >= 0 -> GuardInPath
- Strategy: GuardInPath
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: applied by replacing the named event-arg declarations
  - `Rescale.Exponent` becomes `Exponent as integer nonnegative`

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| guard-nf | when Rescale.Exponent > -10 | the guard bounds Exponent only below -10, not at 0 — must still reject, same obligation | reject, naming the same obligation |
| arg-mod | Exponent as integer min -10 | the declared floor is -10, not 0 — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Verified HEAD diagnostic-code defect: the base above rejects with `[Error] SqrtOfNegative: 'Exponent' can be negative in event handler 'Rescale', so sqrt(...) is unsafe` — the diagnostic names sqrt even though the program contains no sqrt call. Root cause: GetNumericRequirementDiagnosticCode (src/Precept/Pipeline/ProofEngine.Diagnostics.cs:390-392) maps every Numeric requirement with comparison >= and threshold 0 to DiagnosticCode.SqrtOfNegative, with no separate code for pow's exponent precondition. The obligation itself is correctly identified and checked (kind=Numeric, the right condition, the right site); only the diagnostic's code and message are wrong. Every pow witness in this file inherits this same mislabeling; recorded once here rather than repeated per cell.
- HAZARD: identical to g12/sqrt-write's hazard note, reproduced for pow. Live-verified demonstration (commit e1a14d91, 2026-07-21): `field Exponent as integer default 0`, `rule Exponent >= 0 because "Exponent must never be negative"`, `event Drift(Amount as integer)`, `event Rescale(NewExponent as integer nonnegative)`, `on Rescale -> set Exponent = Rescale.NewExponent -> set ScaleFactor = pow(2, Exponent)`, `on Drift -> set Exponent = Drift.Amount` compiles with zero diagnostics even though Drift drives Exponent negative unguarded. Same missing-rule status as sqrt's write cell — see there rather than repeating.
- wp.canonicalKey omitted — see g12/sqrt-write's note; the same applies here.

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc
- src/Precept/Language/Functions.cs:183 — code

## g12/pow-construction — pow integer-overload exponent non-negativity at a construction row

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | Pow |
| evaluation site category | construction-row-action-operand |
| type family | primitive |

### What must be proven

Obligation: pow(Base, Exponent) where Exponent >= 0, integer overload only

Weakest precondition: Exponent >= 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Base | pow's first argument |
| Exponent | pow's second argument, at this construction row (the handler for an event marked initial), integer overload only |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a), (b), (c).

Identical structure to g12/sqrt-construction: no pre-state exists before the row fires, so class (d) is structurally unavailable and free of the hazard the write/state-hook cells carry. (a), (b), and (c) remain available and, live-verified below, functional.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: the construction row's own guard, normal-form-equal to the WP, discharges via GuardInPath
- Strategy: GuardInPath
- Validity arguments: Guard normal-form match
- Decision procedure: Same GuardSubsumes matcher as g12/sqrt-construction.

**Entry 2 — (b)**

- Derivation: the constructing event's arg Exponent reads is declared nonnegative at ingress
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Same matcher, sourced from the arg's declared modifier.

**Entry 3 — (a)**

- Derivation: a field already materialised earlier in the same construction row carries a qualifying modifier, and Exponent reads that field
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Same matcher, sourced from the field's declared modifiers.

### What the failing diagnostic must suggest

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix

- For class (b): bound the args such that the interval combination satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix

- For class (a): declare the field with a modifier bound satisfying <WP>
  - Authored by extension; not matrix-quoted text.


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept PowConstructionBase

field ScaleFactor as integer default 1

event Open(Exponent as integer) initial

on Open
    -> set ScaleFactor = pow(2, Open.Exponent)
```

Required outcome: reject, naming the missing premise classes (a), (b), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*guard-nf* — guard

- Addition: when Open.Exponent >= 0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the WP -> GuardInPath
- Strategy: GuardInPath
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: appended to the guard of the `on Open` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

*arg-mod* — arg-modifier

- Addition: Exponent as integer nonnegative
- Premise classes: (b)
- Derivation: arg ingress bound Exponent >= 0 -> GuardInPath
- Strategy: GuardInPath
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: applied by replacing the named event-arg declarations
  - `Open.Exponent` becomes `Exponent as integer nonnegative`

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| guard-nf | when Open.Exponent > -5 | the guard bounds Exponent only below -5, not at 0 — must still reject, same obligation | reject, naming the same obligation |
| arg-mod | Exponent as integer min -5 | the declared floor is -5, not 0 — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Same diagnostic-code mislabeling as g12/pow-write (SqrtOfNegative reported in place of a pow-specific code) — see that cell's note rather than repeating.
- Free of the premise-(d) hazard by construction, same argument as g12/sqrt-construction.
- wp.canonicalKey omitted — see g12/sqrt-write's note.

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc
- src/Precept/Language/Functions.cs:183 — code

## g12/pow-state-hook — pow integer-overload exponent non-negativity at a state entry hook

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | Pow |
| evaluation site category | state-hook-action-operand |
| type family | primitive |

### What must be proven

Obligation: pow(Base, Exponent) where Exponent >= 0, integer overload only

Weakest precondition: Exponent >= 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Base | pow's first argument |
| Exponent | pow's second argument, at this state hook, integer overload only |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a), (c).

Identical structure to g12/sqrt-state-hook: class (b) is structurally unavailable (the hook has no event args), a guard on the entering row does not discharge, and only (a) a modifier declared directly on the field and (c) the hook's own pre-verb guard are available and functional.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: the hook's own pre-verb guard, normal-form-equal to the WP, discharges via GuardInPath
- Strategy: GuardInPath
- Validity arguments: Guard normal-form match
- Decision procedure: Same GuardSubsumes matcher as g12/sqrt-state-hook, reading StateHookContext.Hook.Guard.

**Entry 2 — (a)**

- Derivation: the field Exponent reads carries a qualifying modifier, enforced at every write site by IntervalContainment
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Same matcher, sourced from the field's declared modifier.

### What the failing diagnostic must suggest

- For class (c): add a guard normal-form-equal to <WP> on the hook itself (to/from S when <WP>)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix

- For class (a): declare the field with a modifier bound satisfying <WP>
  - Authored by extension; not matrix-quoted text.


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept PowStateHookBase

field Exponent as integer default 0
field ScaleFactor as integer default 1

state Idle initial
state Active terminal

event Advance(NewExponent as integer)

from Idle on Advance
    -> set Exponent = Advance.NewExponent
    -> transition Active

to Active -> set ScaleFactor = pow(2, Exponent)
```

Required outcome: reject, naming the missing premise classes (a), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*hook-guard-nf* — guard

- Addition: to Active when Exponent >= 0 -> set ScaleFactor = pow(2, Exponent)
- Premise classes: (c)
- Derivation: the hook's own guard, normal-form-equal to the WP -> GuardInPath
- Strategy: GuardInPath
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: appended to the guard of the `on __state_hook_to_Active` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Same application-locus vocabulary gap as g12/sqrt-state-hook.

*field-mod* — arg-modifier

- Addition: field Exponent as integer default 0 nonnegative
- Premise classes: (a)
- Derivation: field modifier Exponent >= 0, enforced at every write site by IntervalContainment -> GuardInPath
- Strategy: GuardInPath
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Same schema-vocabulary compromise as g12/sqrt-state-hook.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| hook-guard-nf | to Active when Exponent > -5 -> set ScaleFactor = pow(2, Exponent) | the hook's guard bounds Exponent only below -5, not at 0 — must still reject, same obligation | reject, naming the same obligation |
| field-mod | field Exponent as integer default 0 min -5 | the declared floor is -5, not 0 — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Same diagnostic-code mislabeling as g12/pow-write — see that cell's note.
- Live-verified negative finding, same as g12/sqrt-state-hook: a guard on the entering transition row does not discharge; only the hook's own guard or a direct field modifier do.
- HAZARD: identical shape to g12/sqrt-state-hook's hazard note, reproduced for pow. Live-verified (commit e1a14d91, 2026-07-21): `rule Exponent >= 0`, an unguarded `Drift` handler that sets `Exponent` to any value, and the guarded `Advance` handler together compile with zero diagnostics even though the hook then computes `pow(2, Exponent)` after an unguarded write could have made it negative. Same missing-rule status as g12/sqrt-write — see there.
- wp.canonicalKey omitted — see g12/sqrt-write's note.

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc
- src/Precept/Language/Functions.cs:183 — code

## g12/pow-guard — pow integer-overload exponent non-negativity inside a guard expression

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | Pow |
| evaluation site category | transition-row-guard |
| type family | primitive |

### What must be proven

Obligation: pow(Base, Exponent) where Exponent >= 0, integer overload only

Weakest precondition: Exponent >= 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Base | pow's first argument |
| Exponent | pow's second argument, inside a guard's boolean expression, integer overload only |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a), (b).

Identical structure to g12/sqrt-guard: stands for the same five guard-shaped categories plus reject-message-interpolation. Class (c) is unavailable (a guard cannot discharge an obligation arising inside itself); (a) and (b) are the model-licensed classes on the categories with the relevant scope. Class (d) reasoning is the same open/hazard question flagged there.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: the field Exponent reads carries a qualifying modifier
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Same matcher as g12/sqrt-guard, applied under the model.

**Entry 2 — (b)**

- Derivation: the event arg Exponent reads is declared with a qualifying bound at ingress
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Same matcher, under the model.

### What the failing diagnostic must suggest

- For class (a): declare the field with a modifier bound satisfying <WP>
  - Authored by extension; not matrix-quoted text.

- For class (b): bound the args such that the interval combination satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept PowGuardBase

field ScaleFactor as integer default 1

event Rescale(Exponent as integer)

on Rescale when pow(2, Rescale.Exponent) >= 0
    -> set ScaleFactor = 1
```

Required outcome: reject, naming the missing premise classes (a), (b), with no other diagnostics

Provenance: model-derived.

**Discharge additions — what makes the base compile.**

*arg-mod* — arg-modifier

- Addition: Exponent as integer nonnegative
- Premise classes: (b)
- Derivation: arg ingress bound Exponent >= 0, under the model
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (the guard position mints no fault obligation at all at HEAD, for either function — base and this addition both compile clean)
- How it applies to the base: applied by replacing the named event-arg declarations
  - `Rescale.Exponent` becomes `Exponent as integer nonnegative`

Required outcome: accept, with the premise list recorded

Provenance: model-derived.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| arg-mod | Exponent as integer min -10 | under the model, the declared floor is -10, not 0 — untestable at HEAD since the base itself does not reject here | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Live-verified, 2026-07-21, commit e1a14d91: `on Rescale when pow(2, Rescale.Exponent) >= 0 -> set ScaleFactor = 1` compiles with zero diagnostics regardless of Exponent's bound. Guard positions mint nothing at HEAD for pow either, matching g12/sqrt-guard's finding.
- reject-message-interpolation, live-verified separately (2026-07-21, commit e1a14d91): `on Rescale -> reject "scale would be {pow(2, Rescale.Exponent)}"` compiles clean (only an unrelated FieldNeverSet warning). Same non-minting result as sqrt's, folded into this cell on the same basis as g12/sqrt-guard.
- The same ensure-activation-guard / rule-activation-guard open item flagged on g12/sqrt-guard applies here identically — not repeated in full.
- wp.canonicalKey omitted — see g12/sqrt-write's note.

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc
- src/Precept/Language/Functions.cs:183 — code

## g12/pow-constraint — pow integer-overload exponent non-negativity inside a constraint's own condition

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | Pow |
| evaluation site category | rule-condition |
| type family | primitive |

### What must be proven

Obligation: pow(Base, Exponent) where Exponent >= 0, integer overload only

Weakest precondition: Exponent >= 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Base | pow's first argument |
| Exponent | pow's second argument, inside a rule condition, computed field expression, or quantifier predicate, integer overload only |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a).

Identical structure to g12/sqrt-constraint: no handler guard, no event args, no single pre-state, so only class (a) field/element modifiers are available. Live-verified for rule-condition; computed-field-expression and quantifier-predicate are expected by the same evaluation-site-level argument that carried across for sqrt but were NOT independently run for pow in this pass — see notes rather than assumed silently.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: the field Exponent reads carries a qualifying modifier
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Live-verified decision procedure: the same (field, comparison, literal) matcher as g12/sqrt-constraint, sourced from the field's own declared modifier.

### What the failing diagnostic must suggest

- For class (a): declare the field (or the collection's inner-type value) with a modifier bound satisfying <WP>
  - Authored by extension; not matrix-quoted text.


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept PowRuleConditionBase

field Exponent as integer default 0
field ScaleFactor as integer default 1

rule pow(2, Exponent) >= 0 because "scale factor exponent must be well-formed"

event Rescale(NewExponent as integer)

on Rescale
    -> set Exponent = Rescale.NewExponent
    -> set ScaleFactor = Exponent
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*field-mod* — arg-modifier

- Addition: field Exponent as integer default 0 nonnegative
- Premise classes: (a)
- Derivation: field modifier Exponent >= 0
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Same schema-vocabulary compromise as g12/sqrt-constraint.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| field-mod | field Exponent as integer default 0 min -3 | the declared floor is -3, not 0 — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Same diagnostic-code mislabeling as g12/pow-write — see that cell's note.
- computed-field-expression and quantifier-predicate were NOT independently live-verified for pow in this pass (sqrt's equivalents were). Not asserted either way beyond the structural argument that evaluation-site minting is a site-level property, not a per-function one — flagged as unverified-for-pow rather than silently inherited from the sqrt result.
- state-ensure-condition and event-ensure-condition carry the same HAZARD flag and same not-independently-tested status as noted on g12/sqrt-constraint, reproduced for pow rather than repeated in full. See missingRules.
- constraint-rationale-interpolation is folded into this cell on the same basis as g12/sqrt-constraint's rationale finding, NOT independently re-verified for pow's exponent condition in this pass.
- wp.canonicalKey omitted — see g12/sqrt-write's note.

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc
- src/Precept/Language/Functions.cs:183 — code

## g12/pow-declaration — pow integer-overload exponent non-negativity inside a declaration-position value expression

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | Pow |
| evaluation site category | field-default-value-expression |
| type family | primitive |

### What must be proven

Obligation: pow(Base, Exponent) where Exponent >= 0, integer overload only

Weakest precondition: Exponent >= 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Base | pow's first argument |
| Exponent | pow's second argument, inside a field default, a field/event-arg constraint-modifier value, a collection inner-type value modifier, or a type-qualifier expression, integer overload only |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a).

Identical structure to g12/sqrt-declaration: stands for all five declaration-position categories; only class (a) — a literal fold or an earlier field's modifier — is available under the model.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: Exponent is a literal, or reads a field declared earlier that carries a qualifying modifier
- Validity arguments: Literal constant-fold for defaults; Arg-bound interval arithmetic
- Decision procedure: Under the model: constant-fold if Exponent is a literal; otherwise the (field, comparison, literal) matcher named in g12/pow-write, sourced from the earlier field's declared modifier.

### What the failing diagnostic must suggest

- For class (a): supply a literal value satisfying <WP>, or declare the earlier field with a modifier bound satisfying <WP>
  - Authored by extension; not matrix-quoted text.


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept PowDeclarationDefaultLit

field ScaleFactor as integer default pow(2, -3)
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: model-derived.

**Discharge additions — what makes the base compile.**

*literal-fold* — other

- Addition: field ScaleFactor as integer default pow(2, 3)
- Derivation: under the model: constant-fold pow(2, 3) >= 0-exponent check over the literal default (replaces the base's literal exponent, from -3 to 3)
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (field-default-value-expression mints no fault obligation at all at HEAD, so there is nothing here for a fold to discharge)

Required outcome: accept, with the premise list recorded

Provenance: model-derived.

- Same application-locus vocabulary gap as g12/sqrt-declaration.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| literal-fold | field ScaleFactor as integer default pow(2, -1) | under the model, -1 is still negative — must still reject, same obligation — untestable at HEAD since the base itself does not reject here | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Live-verified, 2026-07-21, commit e1a14d91: `field ScaleFactor as integer default pow(2, -3)` compiles with zero diagnostics — no fault diagnostic of any kind. field-default-value-expression mints nothing at HEAD for pow either, matching g12/sqrt-declaration's finding.
- field-modifier-value-expression and event-arg-modifier-value-expression were NOT independently re-run for pow in this pass; sqrt's equivalents were live-verified non-minting. Not asserted for pow beyond the structural site-level argument — flagged as unverified-for-pow rather than silently inherited.
- collection-inner-type-modifier-value-expression and type-qualifier-expression are unmeasured for both functions, same as g12/sqrt-declaration.
- wp.canonicalKey omitted — see g12/sqrt-write's note.

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc
- src/Precept/Language/Functions.cs:183 — code

