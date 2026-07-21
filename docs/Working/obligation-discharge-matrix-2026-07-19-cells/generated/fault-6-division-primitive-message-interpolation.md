<!--
GENERATED FILE — do not hand-edit.
Source: fault-6-division-primitive-message-interpolation.cells.json
Regenerate: dotnet run --project tools/Precept.MatrixTools -- render-cells docs/Working/obligation-discharge-matrix-2026-07-19-cells
Hand edits will be overwritten. Edit the .cells.json and regenerate.
-->

# Fault group 6 — division/modulo by zero inside a refusal message or a constraint rationale, primitive lanes

Family id: fault-6-division-primitive-message-interpolation
Case shape: fault
Definition version: obligation-discharge-matrix-2026-07-19 rev 7 (2026-07-20, draft)

**Where this family's content comes from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § The cell — matrix
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row, naming the reject-row/no-write evaluation site explicitly
- docs/Working/what-i-want-2026-07-16.md:72 — want-doc: the reject-row division example the want doc uses
- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault prevention as an obligation family

## Respellability — the family verdict

Can every safe program these rules reject be rewritten into a form they accept? **yes**

Evidence: model-derived
Corpus measurement: pending

**Notes on the family verdict**

- Both evaluation-site categories in this group inherit the verdict; neither cell states an override.
- The verdict covers only the licensed (c)/(a) contract entries; it says nothing about the open class-(d) question, which is a missing-rule item, not a spelling question.

**Sources for this verdict**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Respellable bullet
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Ratification protocol — matrix: layer 4: corpus measurement is the hard gate; a family verdict ratifies only when it agrees with the classified sample corpus

## Premise classes — the legend

Where a proof may get its facts (want :19, :143): (a) field modifiers, (b) arg constraints, (c) the handler's guard, (d) all rules holding in the pre-state (the inductive hypothesis), (e) evaluating the applicable rule set at an editable-field write — (e)'s status as a genuinely fifth class vs (a) generalized is an open owner item.

## fault6/reject-message/int-divide — Reject-message interpolation — integer / integer

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:72 — want-doc: the reject-row interpolation the want doc uses as the reason the fault axis needs a no-write evaluation-site category
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row: 'the site may sit in a guard or a reject-row interpolation with no write at all'

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | IntegerDivideInteger |
| evaluation site category | reject-message-interpolation |
| type family | primitive |

### What must be proven

Obligation: Parts != 0

Weakest precondition: Parts != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Total | the dividend the reject message interpolates |
| Parts | the divisor the reject message interpolates |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (c).

Parts is a field written by a separate handler (AdjustParts), not an event arg of ComputePerPart, so class (b) has nothing to instantiate at this site (arg constraints are enforced on ComputePerPart's own args, and Parts is not one of them). The reject row matched to render its message, so its own guard is available as class (c) over exactly the pre-state Parts reads (want-doc :72; matrix § Per-family case shapes, Fault family row). Class (d) is listed as available at this evaluation-site category (pre-state constraints), but no contract entry here cites it: the matrix's § Validity arguments carries no argument for 'a pre-state rule directly discharges a fault-family divisor obligation', so citing it would violate the rule-validity gate (§ Rule validity: 'no cell ratifies whose derivation cites an argument-less rule'). Recorded as a missing rule, not answered here — see notes.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: guard fact normal-form-equal to the WP -> guard-match, read at the reject row itself: the row matched, so its guard evaluated true over the same pre-state the message string is interpolated against, and nothing writes between the guard's evaluation and the message's (the row writes nothing at all)
- Validity arguments: Guard normal-form match
- Decision procedure: First: normal-form match of the divisor-nonzero WP against the reject row's own guard conjuncts (a finite set) — Family 1's class-(c) procedure. Failing that: collect the guard's per-term bound conjuncts over the divisor and run the same exact-lane interval exclusion Family 4 states for arg bounds (a term whose bound excludes zero discharges; no matching term, or a bound admitting zero, fails the derivation). If both fail, reject, naming class (c).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Guard normal-form match — the argument's reasoning (guards select, evaluation is pure and deterministic, the same pre-state is read) does not depend on a write existing between the guard and the re-evaluation; applied here to a no-write evaluation site as a strict subcase, not a re-derivation
- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4's stated decision procedure (arg-bound interval arithmetic over a divisor), reused here for the guard-spelled variant per the band ruling that per-term bound conjuncts feed the same interval derivation wherever they are spelled

### What the failing diagnostic must suggest

- For class (c): add a guard normal-form-equal to <WP>, or a per-term bound conjunct on the divisor that excludes zero
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
field Total as integer default 0
field Parts as integer default 1

state Active initial

event Create initial
event AdjustParts(NewParts as integer)
event ComputePerPart(Threshold as integer)

on Create
    -> set Total = 0
    -> set Parts = 1

from Active on AdjustParts
    -> set Parts = AdjustParts.NewParts
    -> no transition

from Active on ComputePerPart when ComputePerPart.Threshold > 0
    -> no transition
from Active on ComputePerPart
    -> reject "Cannot compute: per-part total is {Total / Parts}"
```

Required outcome: reject, naming the missing premise classes (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*guard-nf* — guard

- Addition: when Parts != 0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the WP -> guard-match, at a no-write evaluation site
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (the evaluation-site enumeration measured this category at HEAD and found it mints no fault obligation at all, so the compile is clean regardless of the addition — it confirms the no-mint gap, not that the guard was consumed as a genuine premise)
- How it applies to the base: appended to the guard of the `on ComputePerPart` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

- Measured: adding `when Parts != 0` to the base's reject row compiles with zero diagnostics at HEAD (e1a14d91) — the same clean result as the base, because the site never mints a diagnostic to discharge in the first place.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| guard-nf | when Parts >= 0 | Parts >= 0 admits zero — must still reject, same obligation, under the model | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Built-status gap, live-measured: base compiled clean at HEAD (e1a14d91, 2026-07-21, Precept.MatrixTools) with zero diagnostics — no DivisionByZero-shaped fault, contradicting the model's required rejection. This is the position's no-mint gap (the evaluation-site enumeration found the same for both categories in this group), not evidence the base is sound; base and near-miss provenance are marked live-verified to report the literal (clean) result honestly, not to claim it confirms the required reject outcome.
- The WP calculator (tools/Precept.MatrixTools) computes weakest preconditions through write plans; this evaluation site has no write plan at all (the row writes nothing), so no canonical key is pinned here — a named skip, not a silent omission, consistent with the day-one validator's design (docs/Working/obligation-discharge-matrix-2026-07-19.md § Storage).
- Missing rule, not answered here: whether premise class (d) — a `rule` holding in the pre-state — can license a fault-family divisor-nonzero obligation directly has no written validity argument in the matrix. Measured 2026-07-21 (Precept.MatrixTools, commit e1a14d91): adding `rule Parts != 0 because "..."` to this base, with AdjustParts still able to set Parts to 0 with nothing checking it (Defect A — `rule` mints no write-site obligation at HEAD), compiles with zero diagnostics. That is the same shape as the false proof docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3 (Defect B) verifies for the write-site fault family: a `rule` consumed as a premise while nothing establishes or preserves it. Not recorded as a licensed discharge here — doing so would cite a rule with no validity argument, which the rule-validity gate forbids.
- Open dependency carried, not answered (per the group's authoring notes): whether the failure context itself — the fact that the row matched and is in the middle of refusing — is a premise in its own right is not settled by anything the matrix currently states.

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:72 — want-doc
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- src/Precept/Language/Operations.cs:119 — code: OperationKind.IntegerDivideInteger declaration
- src/Precept/Language/Operations.cs:125 — code: the 'Divisor must be non-zero' NumericProofRequirement
- docs/language/precept-language-spec.md § Rejections — spec
- docs/language/precept-language-spec.md § 2.5 Interpolation Reassembly — spec
- samples/travel-reimbursement.precept:61 — code: a shipped precedent of division inside a reject-row interpolation

## fault6/reject-message/dec-divide — Reject-message interpolation — decimal / decimal

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:72 — want-doc: the reject-row interpolation the want doc uses as the reason the fault axis needs a no-write evaluation-site category
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row: 'the site may sit in a guard or a reject-row interpolation with no write at all'

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | DecimalDivideDecimal |
| evaluation site category | reject-message-interpolation |
| type family | primitive |

### What must be proven

Obligation: Parts != 0

Weakest precondition: Parts != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Total | the dividend the reject message interpolates |
| Parts | the divisor the reject message interpolates |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (c).

Parts is a field written by a separate handler (AdjustParts), not an event arg of ComputePerPart, so class (b) has nothing to instantiate at this site (arg constraints are enforced on ComputePerPart's own args, and Parts is not one of them). The reject row matched to render its message, so its own guard is available as class (c) over exactly the pre-state Parts reads (want-doc :72; matrix § Per-family case shapes, Fault family row). Class (d) is listed as available at this evaluation-site category (pre-state constraints), but no contract entry here cites it: the matrix's § Validity arguments carries no argument for 'a pre-state rule directly discharges a fault-family divisor obligation', so citing it would violate the rule-validity gate (§ Rule validity: 'no cell ratifies whose derivation cites an argument-less rule'). Recorded as a missing rule, not answered here — see notes.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: guard fact normal-form-equal to the WP -> guard-match, read at the reject row itself: the row matched, so its guard evaluated true over the same pre-state the message string is interpolated against, and nothing writes between the guard's evaluation and the message's (the row writes nothing at all)
- Validity arguments: Guard normal-form match
- Decision procedure: First: normal-form match of the divisor-nonzero WP against the reject row's own guard conjuncts (a finite set) — Family 1's class-(c) procedure. Failing that: collect the guard's per-term bound conjuncts over the divisor and run the same exact-lane interval exclusion Family 4 states for arg bounds (a term whose bound excludes zero discharges; no matching term, or a bound admitting zero, fails the derivation). If both fail, reject, naming class (c).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Guard normal-form match — the argument's reasoning (guards select, evaluation is pure and deterministic, the same pre-state is read) does not depend on a write existing between the guard and the re-evaluation; applied here to a no-write evaluation site as a strict subcase, not a re-derivation
- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4's stated decision procedure (arg-bound interval arithmetic over a divisor), reused here for the guard-spelled variant per the band ruling that per-term bound conjuncts feed the same interval derivation wherever they are spelled

### What the failing diagnostic must suggest

- For class (c): add a guard normal-form-equal to <WP>, or a per-term bound conjunct on the divisor that excludes zero
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
field Total as decimal default 0.0
field Parts as decimal default 1.0

state Active initial

event Create initial
event AdjustParts(NewParts as decimal)
event ComputePerPart(Threshold as decimal)

on Create
    -> set Total = 0.0
    -> set Parts = 1.0

from Active on AdjustParts
    -> set Parts = AdjustParts.NewParts
    -> no transition

from Active on ComputePerPart when ComputePerPart.Threshold > 0
    -> no transition
from Active on ComputePerPart
    -> reject "Cannot compute: per-part total is {Total / Parts}"
```

Required outcome: reject, naming the missing premise classes (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*guard-nf* — guard

- Addition: when Parts != 0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the WP -> guard-match, at a no-write evaluation site
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (the evaluation-site enumeration measured this category at HEAD and found it mints no fault obligation at all, so the compile is clean regardless of the addition — it confirms the no-mint gap, not that the guard was consumed as a genuine premise)
- How it applies to the base: appended to the guard of the `on ComputePerPart` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

- Measured: adding `when Parts != 0` to the base's reject row compiles with zero diagnostics at HEAD (e1a14d91) — the same clean result as the base, because the site never mints a diagnostic to discharge in the first place.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| guard-nf | when Parts >= 0 | Parts >= 0 admits zero — must still reject, same obligation, under the model | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Built-status gap, live-measured: base compiled clean at HEAD (e1a14d91, 2026-07-21, Precept.MatrixTools) with zero diagnostics — no DivisionByZero-shaped fault, contradicting the model's required rejection. This is the position's no-mint gap (the evaluation-site enumeration found the same for both categories in this group), not evidence the base is sound; base and near-miss provenance are marked live-verified to report the literal (clean) result honestly, not to claim it confirms the required reject outcome.
- The WP calculator (tools/Precept.MatrixTools) computes weakest preconditions through write plans; this evaluation site has no write plan at all (the row writes nothing), so no canonical key is pinned here — a named skip, not a silent omission, consistent with the day-one validator's design (docs/Working/obligation-discharge-matrix-2026-07-19.md § Storage).
- Missing rule, not answered here: whether premise class (d) — a `rule` holding in the pre-state — can license a fault-family divisor-nonzero obligation directly has no written validity argument in the matrix. Measured 2026-07-21 (Precept.MatrixTools, commit e1a14d91): adding `rule Parts != 0 because "..."` to this base, with AdjustParts still able to set Parts to 0 with nothing checking it (Defect A — `rule` mints no write-site obligation at HEAD), compiles with zero diagnostics. That is the same shape as the false proof docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3 (Defect B) verifies for the write-site fault family: a `rule` consumed as a premise while nothing establishes or preserves it. Not recorded as a licensed discharge here — doing so would cite a rule with no validity argument, which the rule-validity gate forbids.
- Open dependency carried, not answered (per the group's authoring notes): whether the failure context itself — the fact that the row matched and is in the middle of refusing — is a premise in its own right is not settled by anything the matrix currently states.

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:72 — want-doc
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- src/Precept/Language/Operations.cs:150 — code: OperationKind.DecimalDivideDecimal declaration
- src/Precept/Language/Operations.cs:156 — code: the 'Divisor must be non-zero' NumericProofRequirement
- docs/language/precept-language-spec.md § Rejections — spec
- docs/language/precept-language-spec.md § 2.5 Interpolation Reassembly — spec
- samples/travel-reimbursement.precept:61 — code: a shipped precedent of division inside a reject-row interpolation

## fault6/reject-message/num-divide — Reject-message interpolation — number / number

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:72 — want-doc: the reject-row interpolation the want doc uses as the reason the fault axis needs a no-write evaluation-site category
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row: 'the site may sit in a guard or a reject-row interpolation with no write at all'

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | NumberDivideNumber |
| evaluation site category | reject-message-interpolation |
| type family | primitive |

### What must be proven

Obligation: Parts != 0

Weakest precondition: Parts != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Total | the dividend the reject message interpolates |
| Parts | the divisor the reject message interpolates |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (c).

Parts is a field written by a separate handler (AdjustParts), not an event arg of ComputePerPart, so class (b) has nothing to instantiate at this site (arg constraints are enforced on ComputePerPart's own args, and Parts is not one of them). The reject row matched to render its message, so its own guard is available as class (c) over exactly the pre-state Parts reads (want-doc :72; matrix § Per-family case shapes, Fault family row). Class (d) is listed as available at this evaluation-site category (pre-state constraints), but no contract entry here cites it: the matrix's § Validity arguments carries no argument for 'a pre-state rule directly discharges a fault-family divisor obligation', so citing it would violate the rule-validity gate (§ Rule validity: 'no cell ratifies whose derivation cites an argument-less rule'). Recorded as a missing rule, not answered here — see notes.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: guard fact normal-form-equal to the WP -> guard-match, read at the reject row itself: the row matched, so its guard evaluated true over the same pre-state the message string is interpolated against, and nothing writes between the guard's evaluation and the message's (the row writes nothing at all)
- Validity arguments: Guard normal-form match
- Decision procedure: First: normal-form match of the divisor-nonzero WP against the reject row's own guard conjuncts (a finite set) — Family 1's class-(c) procedure. Failing that: collect the guard's per-term bound conjuncts over the divisor and run the same exact-lane interval exclusion Family 4 states for arg bounds (a term whose bound excludes zero discharges; no matching term, or a bound admitting zero, fails the derivation). If both fail, reject, naming class (c).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Guard normal-form match — the argument's reasoning (guards select, evaluation is pure and deterministic, the same pre-state is read) does not depend on a write existing between the guard and the re-evaluation; applied here to a no-write evaluation site as a strict subcase, not a re-derivation
- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4's stated decision procedure (arg-bound interval arithmetic over a divisor), reused here for the guard-spelled variant per the band ruling that per-term bound conjuncts feed the same interval derivation wherever they are spelled

### What the failing diagnostic must suggest

- For class (c): add a guard normal-form-equal to <WP>, or a per-term bound conjunct on the divisor that excludes zero
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
field Total as number default 0
field Parts as number default 1

state Active initial

event Create initial
event AdjustParts(NewParts as number)
event ComputePerPart(Threshold as number)

on Create
    -> set Total = 0
    -> set Parts = 1

from Active on AdjustParts
    -> set Parts = AdjustParts.NewParts
    -> no transition

from Active on ComputePerPart when ComputePerPart.Threshold > 0
    -> no transition
from Active on ComputePerPart
    -> reject "Cannot compute: per-part total is {Total / Parts}"
```

Required outcome: reject, naming the missing premise classes (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*guard-nf* — guard

- Addition: when Parts != 0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the WP -> guard-match, at a no-write evaluation site
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (the evaluation-site enumeration measured this category at HEAD and found it mints no fault obligation at all, so the compile is clean regardless of the addition — it confirms the no-mint gap, not that the guard was consumed as a genuine premise)
- How it applies to the base: appended to the guard of the `on ComputePerPart` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

- Measured: adding `when Parts != 0` to the base's reject row compiles with zero diagnostics at HEAD (e1a14d91) — the same clean result as the base, because the site never mints a diagnostic to discharge in the first place.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| guard-nf | when Parts >= 0 | Parts >= 0 admits zero — must still reject, same obligation, under the model | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Built-status gap, live-measured: base compiled clean at HEAD (e1a14d91, 2026-07-21, Precept.MatrixTools) with zero diagnostics — no DivisionByZero-shaped fault, contradicting the model's required rejection. This is the position's no-mint gap (the evaluation-site enumeration found the same for both categories in this group), not evidence the base is sound; base and near-miss provenance are marked live-verified to report the literal (clean) result honestly, not to claim it confirms the required reject outcome.
- The WP calculator (tools/Precept.MatrixTools) computes weakest preconditions through write plans; this evaluation site has no write plan at all (the row writes nothing), so no canonical key is pinned here — a named skip, not a silent omission, consistent with the day-one validator's design (docs/Working/obligation-discharge-matrix-2026-07-19.md § Storage).
- Missing rule, not answered here: whether premise class (d) — a `rule` holding in the pre-state — can license a fault-family divisor-nonzero obligation directly has no written validity argument in the matrix. Measured 2026-07-21 (Precept.MatrixTools, commit e1a14d91): adding `rule Parts != 0 because "..."` to this base, with AdjustParts still able to set Parts to 0 with nothing checking it (Defect A — `rule` mints no write-site obligation at HEAD), compiles with zero diagnostics. That is the same shape as the false proof docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3 (Defect B) verifies for the write-site fault family: a `rule` consumed as a premise while nothing establishes or preserves it. Not recorded as a licensed discharge here — doing so would cite a rule with no validity argument, which the rule-validity gate forbids.
- Open dependency carried, not answered (per the group's authoring notes): whether the failure context itself — the fact that the row matched and is in the middle of refusing — is a premise in its own right is not settled by anything the matrix currently states.

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:72 — want-doc
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- src/Precept/Language/Operations.cs:181 — code: OperationKind.NumberDivideNumber declaration
- src/Precept/Language/Operations.cs:187 — code: the 'Divisor must be non-zero' NumericProofRequirement
- docs/language/precept-language-spec.md § Rejections — spec
- docs/language/precept-language-spec.md § 2.5 Interpolation Reassembly — spec
- samples/travel-reimbursement.precept:61 — code: a shipped precedent of division inside a reject-row interpolation

## fault6/reject-message/decmod-modulo — Reject-message interpolation — decimal % decimal

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:72 — want-doc: the reject-row interpolation the want doc uses as the reason the fault axis needs a no-write evaluation-site category
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row: 'the site may sit in a guard or a reject-row interpolation with no write at all'

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | DecimalModuloDecimal |
| evaluation site category | reject-message-interpolation |
| type family | primitive |

### What must be proven

Obligation: Parts != 0

Weakest precondition: Parts != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Total | the dividend the reject message interpolates |
| Parts | the divisor the reject message interpolates |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (c).

Parts is a field written by a separate handler (AdjustParts), not an event arg of ComputePerPart, so class (b) has nothing to instantiate at this site (arg constraints are enforced on ComputePerPart's own args, and Parts is not one of them). The reject row matched to render its message, so its own guard is available as class (c) over exactly the pre-state Parts reads (want-doc :72; matrix § Per-family case shapes, Fault family row). Class (d) is listed as available at this evaluation-site category (pre-state constraints), but no contract entry here cites it: the matrix's § Validity arguments carries no argument for 'a pre-state rule directly discharges a fault-family divisor obligation', so citing it would violate the rule-validity gate (§ Rule validity: 'no cell ratifies whose derivation cites an argument-less rule'). Recorded as a missing rule, not answered here — see notes.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: guard fact normal-form-equal to the WP -> guard-match, read at the reject row itself: the row matched, so its guard evaluated true over the same pre-state the message string is interpolated against, and nothing writes between the guard's evaluation and the message's (the row writes nothing at all)
- Validity arguments: Guard normal-form match
- Decision procedure: First: normal-form match of the divisor-nonzero WP against the reject row's own guard conjuncts (a finite set) — Family 1's class-(c) procedure. Failing that: collect the guard's per-term bound conjuncts over the divisor and run the same exact-lane interval exclusion Family 4 states for arg bounds (a term whose bound excludes zero discharges; no matching term, or a bound admitting zero, fails the derivation). If both fail, reject, naming class (c).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Guard normal-form match — the argument's reasoning (guards select, evaluation is pure and deterministic, the same pre-state is read) does not depend on a write existing between the guard and the re-evaluation; applied here to a no-write evaluation site as a strict subcase, not a re-derivation
- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4's stated decision procedure (arg-bound interval arithmetic over a divisor), reused here for the guard-spelled variant per the band ruling that per-term bound conjuncts feed the same interval derivation wherever they are spelled

### What the failing diagnostic must suggest

- For class (c): add a guard normal-form-equal to <WP>, or a per-term bound conjunct on the divisor that excludes zero
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
field Total as decimal default 0.0
field Parts as decimal default 1.0

state Active initial

event Create initial
event AdjustParts(NewParts as decimal)
event ComputePerPart(Threshold as decimal)

on Create
    -> set Total = 0.0
    -> set Parts = 1.0

from Active on AdjustParts
    -> set Parts = AdjustParts.NewParts
    -> no transition

from Active on ComputePerPart when ComputePerPart.Threshold > 0
    -> no transition
from Active on ComputePerPart
    -> reject "Cannot compute: per-part total is {Total % Parts}"
```

Required outcome: reject, naming the missing premise classes (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*guard-nf* — guard

- Addition: when Parts != 0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the WP -> guard-match, at a no-write evaluation site
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (the evaluation-site enumeration measured this category at HEAD and found it mints no fault obligation at all, so the compile is clean regardless of the addition — it confirms the no-mint gap, not that the guard was consumed as a genuine premise)
- How it applies to the base: appended to the guard of the `on ComputePerPart` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

- Measured: adding `when Parts != 0` to the base's reject row compiles with zero diagnostics at HEAD (e1a14d91) — the same clean result as the base, because the site never mints a diagnostic to discharge in the first place.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| guard-nf | when Parts >= 0 | Parts >= 0 admits zero — must still reject, same obligation, under the model | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Built-status gap, live-measured: base compiled clean at HEAD (e1a14d91, 2026-07-21, Precept.MatrixTools) with zero diagnostics — no DivisionByZero-shaped fault, contradicting the model's required rejection. This is the position's no-mint gap (the evaluation-site enumeration found the same for both categories in this group), not evidence the base is sound; base and near-miss provenance are marked live-verified to report the literal (clean) result honestly, not to claim it confirms the required reject outcome.
- The WP calculator (tools/Precept.MatrixTools) computes weakest preconditions through write plans; this evaluation site has no write plan at all (the row writes nothing), so no canonical key is pinned here — a named skip, not a silent omission, consistent with the day-one validator's design (docs/Working/obligation-discharge-matrix-2026-07-19.md § Storage).
- Missing rule, not answered here: whether premise class (d) — a `rule` holding in the pre-state — can license a fault-family divisor-nonzero obligation directly has no written validity argument in the matrix. Measured 2026-07-21 (Precept.MatrixTools, commit e1a14d91): adding `rule Parts != 0 because "..."` to this base, with AdjustParts still able to set Parts to 0 with nothing checking it (Defect A — `rule` mints no write-site obligation at HEAD), compiles with zero diagnostics. That is the same shape as the false proof docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3 (Defect B) verifies for the write-site fault family: a `rule` consumed as a premise while nothing establishes or preserves it. Not recorded as a licensed discharge here — doing so would cite a rule with no validity argument, which the rule-validity gate forbids.
- Open dependency carried, not answered (per the group's authoring notes): whether the failure context itself — the fact that the row matched and is in the middle of refusing — is a premise in its own right is not settled by anything the matrix currently states.

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:72 — want-doc
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- src/Precept/Language/Operations.cs:159 — code: OperationKind.DecimalModuloDecimal declaration
- src/Precept/Language/Operations.cs:165 — code: the 'Divisor must be non-zero' NumericProofRequirement
- docs/language/precept-language-spec.md § Rejections — spec
- docs/language/precept-language-spec.md § 2.5 Interpolation Reassembly — spec
- samples/travel-reimbursement.precept:61 — code: a shipped precedent of division inside a reject-row interpolation

## fault6/constraint-rationale/int-divide — Constraint-rationale interpolation — integer / integer

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row: a no-write evaluation site
- docs/language/precept-language-spec.md:288 — spec: mandatory `because` clauses — every `rule` carries one, so the interpolation site always exists wherever a rule's rationale contains arithmetic

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | IntegerDivideInteger |
| evaluation site category | constraint-rationale-interpolation |
| type family | primitive |

### What must be proven

Obligation: Parts != 0

Weakest precondition: Parts != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Total | the dividend the rule's `because` string interpolates |
| Parts | the divisor the rule's `because` string interpolates |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a).

This cell's setting is a plain top-level `rule` (not a state or event ensure), so no event is in scope and class (b) (argument constraints) has nothing to instantiate — a rule's condition and `because` string read fields only. Class (c) (the handler's guard) does not apply either: a `rule` is not attached to a row. A rule's own `when` activation clause is not treated as a premise for its own message here — that is an open question the matrix does not resolve (see notes), not an assumed 'no'. Class (a), the divisor field's own declared modifier, is available: Parts is `editable`, so the modifier is continuously ingress-enforced at the editable-field door, the same discharge mechanism that makes class (b) true for event args (matrix § Vocabulary, Discharge mechanisms: 'by the same symmetry the editable-field door makes premises (a) and (d) true for that field downstream').

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: ingress evaluation at the editable-field door continuously enforces the field's declared modifier (matrix § Vocabulary, Discharge mechanisms, the class-(a)/class-(b) symmetry) -> the divisor's declared bound excludes zero
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor field's interval from its declared modifiers (a finite set); zero inside the interval, or no bound excluding it, rejects — Family 4's procedure, read against the field's declaration rather than an event arg's.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: Discharge mechanisms: 'the editable-field door makes premises (a) and (d) true for that field downstream' — the argument written for class (b) at ingress is cited here for class (a) under that stated symmetry, since both are ingress-enforced declared bounds; the matrix carries no separate class-(a) argument text
- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4's stated decision procedure (arg-bound interval arithmetic over a divisor), applied to a field modifier instead of an arg modifier

### What the failing diagnostic must suggest

- For class (a): declare the divisor field's modifier such that its interval satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet, adapted from arg bounds to field modifiers


### The worked examples

**Base — the program the compiler must reject.**

```precept
field Total as integer default 0 editable
field Parts as integer default 1 editable

rule Total >= 0 because "Per-part total is currently {Total / Parts}"
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*field-modifier-positive* — other

- Addition: field Parts as integer default 1 positive editable
- Premise classes: (a)
- Derivation: field modifier `positive` on Parts -> interval (0, +inf) excludes zero, at a no-write evaluation site
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (the evaluation-site enumeration measured this category at HEAD and found it mints no fault obligation at all, so the compile is clean regardless of the addition)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

- No `application` locus is stated: the schema's application DU covers row-guard and event-arg-declaration additions only, not a field declaration's own modifier list — recorded as an honest schema gap rather than a forced fit.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| field-modifier-positive | field Parts as integer default 1 nonnegative editable | nonnegative admits zero — must still reject, same obligation, under the model | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Built-status gap, live-measured: base compiled clean at HEAD (e1a14d91, 2026-07-21, Precept.MatrixTools) with zero diagnostics, contradicting the model's required rejection — the same no-mint gap as the reject-message category. Base and near-miss provenance are marked live-verified to report the literal result honestly, not to claim confirmation of the required outcome.
- This cell's witness deliberately avoids a fractional numeric literal (e.g. `0.0`) inside a `number`-typed rule's condition: measured at HEAD, `rule Total >= 0.0` on a `number` field raises `TypeMismatch: Expected a number value here, but got 'decimal'`, while the whole-number literal `rule Total >= 0` does not. This looks like a real type-resolution gap on the `number` lane inside rule conditions specifically (comparisons elsewhere resolve a fractional literal against a `number` peer correctly per docs/language/primitive-types.md), but it is outside this group's scope (division-by-zero at a message-interpolation site) and is recorded here only to explain why the `number`-lane and `integer`-lane witnesses use whole-number defaults throughout, not as a finding of this cell.
- No canonical WP key is pinned: the WP calculator computes weakest preconditions through write plans, and a rule's `because` string is a no-write evaluation site — a named skip, consistent with docs/Working/obligation-discharge-matrix-2026-07-19.md § Storage.
- Open dependency carried, not answered: whether a rule's own condition (or, for a conditional rule, its `when`) is available as a premise for its own `because` message is not settled by anything the matrix currently states — the matrix's own text observes only that 'the `because` message renders only when the constraint is false', which is a fact about when the site evaluates, not about what premises are available there.

**What this cell derives from**

- docs/language/precept-language-spec.md:288 — spec: mandatory `because` clauses
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- src/Precept/Language/Operations.cs:119 — code: OperationKind.IntegerDivideInteger declaration
- src/Precept/Language/Operations.cs:125 — code: the 'Divisor must be non-zero' NumericProofRequirement
- docs/language/precept-language-spec.md § 2.5 Interpolation Reassembly — spec
- samples/loan-application.precept:38 — code: a shipped precedent of arithmetic inside a rule's `because` string

## fault6/constraint-rationale/dec-divide — Constraint-rationale interpolation — decimal / decimal

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row: a no-write evaluation site
- docs/language/precept-language-spec.md:288 — spec: mandatory `because` clauses — every `rule` carries one, so the interpolation site always exists wherever a rule's rationale contains arithmetic

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | DecimalDivideDecimal |
| evaluation site category | constraint-rationale-interpolation |
| type family | primitive |

### What must be proven

Obligation: Parts != 0

Weakest precondition: Parts != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Total | the dividend the rule's `because` string interpolates |
| Parts | the divisor the rule's `because` string interpolates |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a).

This cell's setting is a plain top-level `rule` (not a state or event ensure), so no event is in scope and class (b) (argument constraints) has nothing to instantiate — a rule's condition and `because` string read fields only. Class (c) (the handler's guard) does not apply either: a `rule` is not attached to a row. A rule's own `when` activation clause is not treated as a premise for its own message here — that is an open question the matrix does not resolve (see notes), not an assumed 'no'. Class (a), the divisor field's own declared modifier, is available: Parts is `editable`, so the modifier is continuously ingress-enforced at the editable-field door, the same discharge mechanism that makes class (b) true for event args (matrix § Vocabulary, Discharge mechanisms: 'by the same symmetry the editable-field door makes premises (a) and (d) true for that field downstream').

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: ingress evaluation at the editable-field door continuously enforces the field's declared modifier (matrix § Vocabulary, Discharge mechanisms, the class-(a)/class-(b) symmetry) -> the divisor's declared bound excludes zero
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor field's interval from its declared modifiers (a finite set); zero inside the interval, or no bound excluding it, rejects — Family 4's procedure, read against the field's declaration rather than an event arg's.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: Discharge mechanisms: 'the editable-field door makes premises (a) and (d) true for that field downstream' — the argument written for class (b) at ingress is cited here for class (a) under that stated symmetry, since both are ingress-enforced declared bounds; the matrix carries no separate class-(a) argument text
- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4's stated decision procedure (arg-bound interval arithmetic over a divisor), applied to a field modifier instead of an arg modifier

### What the failing diagnostic must suggest

- For class (a): declare the divisor field's modifier such that its interval satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet, adapted from arg bounds to field modifiers


### The worked examples

**Base — the program the compiler must reject.**

```precept
field Total as decimal default 0.0 editable
field Parts as decimal default 1.0 editable

rule Total >= 0.0 because "Per-part total is currently {Total / Parts}"
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*field-modifier-positive* — other

- Addition: field Parts as decimal default 1.0 positive editable
- Premise classes: (a)
- Derivation: field modifier `positive` on Parts -> interval (0, +inf) excludes zero, at a no-write evaluation site
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (the evaluation-site enumeration measured this category at HEAD and found it mints no fault obligation at all, so the compile is clean regardless of the addition)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

- No `application` locus is stated: the schema's application DU covers row-guard and event-arg-declaration additions only, not a field declaration's own modifier list — recorded as an honest schema gap rather than a forced fit.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| field-modifier-positive | field Parts as decimal default 1.0 nonnegative editable | nonnegative admits zero — must still reject, same obligation, under the model | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Built-status gap, live-measured: base compiled clean at HEAD (e1a14d91, 2026-07-21, Precept.MatrixTools) with zero diagnostics, contradicting the model's required rejection — the same no-mint gap as the reject-message category. Base and near-miss provenance are marked live-verified to report the literal result honestly, not to claim confirmation of the required outcome.
- This cell's witness deliberately avoids a fractional numeric literal (e.g. `0.0`) inside a `number`-typed rule's condition: measured at HEAD, `rule Total >= 0.0` on a `number` field raises `TypeMismatch: Expected a number value here, but got 'decimal'`, while the whole-number literal `rule Total >= 0` does not. This looks like a real type-resolution gap on the `number` lane inside rule conditions specifically (comparisons elsewhere resolve a fractional literal against a `number` peer correctly per docs/language/primitive-types.md), but it is outside this group's scope (division-by-zero at a message-interpolation site) and is recorded here only to explain why the `number`-lane and `integer`-lane witnesses use whole-number defaults throughout, not as a finding of this cell.
- No canonical WP key is pinned: the WP calculator computes weakest preconditions through write plans, and a rule's `because` string is a no-write evaluation site — a named skip, consistent with docs/Working/obligation-discharge-matrix-2026-07-19.md § Storage.
- Open dependency carried, not answered: whether a rule's own condition (or, for a conditional rule, its `when`) is available as a premise for its own `because` message is not settled by anything the matrix currently states — the matrix's own text observes only that 'the `because` message renders only when the constraint is false', which is a fact about when the site evaluates, not about what premises are available there.

**What this cell derives from**

- docs/language/precept-language-spec.md:288 — spec: mandatory `because` clauses
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- src/Precept/Language/Operations.cs:150 — code: OperationKind.DecimalDivideDecimal declaration
- src/Precept/Language/Operations.cs:156 — code: the 'Divisor must be non-zero' NumericProofRequirement
- docs/language/precept-language-spec.md § 2.5 Interpolation Reassembly — spec
- samples/loan-application.precept:38 — code: a shipped precedent of arithmetic inside a rule's `because` string

## fault6/constraint-rationale/num-divide — Constraint-rationale interpolation — number / number

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row: a no-write evaluation site
- docs/language/precept-language-spec.md:288 — spec: mandatory `because` clauses — every `rule` carries one, so the interpolation site always exists wherever a rule's rationale contains arithmetic

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | NumberDivideNumber |
| evaluation site category | constraint-rationale-interpolation |
| type family | primitive |

### What must be proven

Obligation: Parts != 0

Weakest precondition: Parts != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Total | the dividend the rule's `because` string interpolates |
| Parts | the divisor the rule's `because` string interpolates |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a).

This cell's setting is a plain top-level `rule` (not a state or event ensure), so no event is in scope and class (b) (argument constraints) has nothing to instantiate — a rule's condition and `because` string read fields only. Class (c) (the handler's guard) does not apply either: a `rule` is not attached to a row. A rule's own `when` activation clause is not treated as a premise for its own message here — that is an open question the matrix does not resolve (see notes), not an assumed 'no'. Class (a), the divisor field's own declared modifier, is available: Parts is `editable`, so the modifier is continuously ingress-enforced at the editable-field door, the same discharge mechanism that makes class (b) true for event args (matrix § Vocabulary, Discharge mechanisms: 'by the same symmetry the editable-field door makes premises (a) and (d) true for that field downstream').

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: ingress evaluation at the editable-field door continuously enforces the field's declared modifier (matrix § Vocabulary, Discharge mechanisms, the class-(a)/class-(b) symmetry) -> the divisor's declared bound excludes zero
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor field's interval from its declared modifiers (a finite set); zero inside the interval, or no bound excluding it, rejects — Family 4's procedure, read against the field's declaration rather than an event arg's.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: Discharge mechanisms: 'the editable-field door makes premises (a) and (d) true for that field downstream' — the argument written for class (b) at ingress is cited here for class (a) under that stated symmetry, since both are ingress-enforced declared bounds; the matrix carries no separate class-(a) argument text
- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4's stated decision procedure (arg-bound interval arithmetic over a divisor), applied to a field modifier instead of an arg modifier

### What the failing diagnostic must suggest

- For class (a): declare the divisor field's modifier such that its interval satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet, adapted from arg bounds to field modifiers


### The worked examples

**Base — the program the compiler must reject.**

```precept
field Total as number default 0 editable
field Parts as number default 1 editable

rule Total >= 0 because "Per-part total is currently {Total / Parts}"
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*field-modifier-positive* — other

- Addition: field Parts as number default 1 positive editable
- Premise classes: (a)
- Derivation: field modifier `positive` on Parts -> interval (0, +inf) excludes zero, at a no-write evaluation site
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (the evaluation-site enumeration measured this category at HEAD and found it mints no fault obligation at all, so the compile is clean regardless of the addition)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

- No `application` locus is stated: the schema's application DU covers row-guard and event-arg-declaration additions only, not a field declaration's own modifier list — recorded as an honest schema gap rather than a forced fit.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| field-modifier-positive | field Parts as number default 1 nonnegative editable | nonnegative admits zero — must still reject, same obligation, under the model | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Built-status gap, live-measured: base compiled clean at HEAD (e1a14d91, 2026-07-21, Precept.MatrixTools) with zero diagnostics, contradicting the model's required rejection — the same no-mint gap as the reject-message category. Base and near-miss provenance are marked live-verified to report the literal result honestly, not to claim confirmation of the required outcome.
- This cell's witness deliberately avoids a fractional numeric literal (e.g. `0.0`) inside a `number`-typed rule's condition: measured at HEAD, `rule Total >= 0.0` on a `number` field raises `TypeMismatch: Expected a number value here, but got 'decimal'`, while the whole-number literal `rule Total >= 0` does not. This looks like a real type-resolution gap on the `number` lane inside rule conditions specifically (comparisons elsewhere resolve a fractional literal against a `number` peer correctly per docs/language/primitive-types.md), but it is outside this group's scope (division-by-zero at a message-interpolation site) and is recorded here only to explain why the `number`-lane and `integer`-lane witnesses use whole-number defaults throughout, not as a finding of this cell.
- No canonical WP key is pinned: the WP calculator computes weakest preconditions through write plans, and a rule's `because` string is a no-write evaluation site — a named skip, consistent with docs/Working/obligation-discharge-matrix-2026-07-19.md § Storage.
- Open dependency carried, not answered: whether a rule's own condition (or, for a conditional rule, its `when`) is available as a premise for its own `because` message is not settled by anything the matrix currently states — the matrix's own text observes only that 'the `because` message renders only when the constraint is false', which is a fact about when the site evaluates, not about what premises are available there.

**What this cell derives from**

- docs/language/precept-language-spec.md:288 — spec: mandatory `because` clauses
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- src/Precept/Language/Operations.cs:181 — code: OperationKind.NumberDivideNumber declaration
- src/Precept/Language/Operations.cs:187 — code: the 'Divisor must be non-zero' NumericProofRequirement
- docs/language/precept-language-spec.md § 2.5 Interpolation Reassembly — spec
- samples/loan-application.precept:38 — code: a shipped precedent of arithmetic inside a rule's `because` string

## fault6/constraint-rationale/decmod-modulo — Constraint-rationale interpolation — decimal % decimal

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row: a no-write evaluation site
- docs/language/precept-language-spec.md:288 — spec: mandatory `because` clauses — every `rule` carries one, so the interpolation site always exists wherever a rule's rationale contains arithmetic

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | DecimalModuloDecimal |
| evaluation site category | constraint-rationale-interpolation |
| type family | primitive |

### What must be proven

Obligation: Parts != 0

Weakest precondition: Parts != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Total | the dividend the rule's `because` string interpolates |
| Parts | the divisor the rule's `because` string interpolates |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a).

This cell's setting is a plain top-level `rule` (not a state or event ensure), so no event is in scope and class (b) (argument constraints) has nothing to instantiate — a rule's condition and `because` string read fields only. Class (c) (the handler's guard) does not apply either: a `rule` is not attached to a row. A rule's own `when` activation clause is not treated as a premise for its own message here — that is an open question the matrix does not resolve (see notes), not an assumed 'no'. Class (a), the divisor field's own declared modifier, is available: Parts is `editable`, so the modifier is continuously ingress-enforced at the editable-field door, the same discharge mechanism that makes class (b) true for event args (matrix § Vocabulary, Discharge mechanisms: 'by the same symmetry the editable-field door makes premises (a) and (d) true for that field downstream').

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: ingress evaluation at the editable-field door continuously enforces the field's declared modifier (matrix § Vocabulary, Discharge mechanisms, the class-(a)/class-(b) symmetry) -> the divisor's declared bound excludes zero
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor field's interval from its declared modifiers (a finite set); zero inside the interval, or no bound excluding it, rejects — Family 4's procedure, read against the field's declaration rather than an event arg's.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: Discharge mechanisms: 'the editable-field door makes premises (a) and (d) true for that field downstream' — the argument written for class (b) at ingress is cited here for class (a) under that stated symmetry, since both are ingress-enforced declared bounds; the matrix carries no separate class-(a) argument text
- docs/Working/obligation-discharge-matrix-2026-07-19.md:299 — matrix: Family 4's stated decision procedure (arg-bound interval arithmetic over a divisor), applied to a field modifier instead of an arg modifier

### What the failing diagnostic must suggest

- For class (a): declare the divisor field's modifier such that its interval satisfies <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet, adapted from arg bounds to field modifiers


### The worked examples

**Base — the program the compiler must reject.**

```precept
field Total as decimal default 0.0 editable
field Parts as decimal default 1.0 editable

rule Total >= 0.0 because "Per-part total is currently {Total % Parts}"
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*field-modifier-positive* — other

- Addition: field Parts as decimal default 1.0 positive editable
- Premise classes: (a)
- Derivation: field modifier `positive` on Parts -> interval (0, +inf) excludes zero, at a no-write evaluation site
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (the evaluation-site enumeration measured this category at HEAD and found it mints no fault obligation at all, so the compile is clean regardless of the addition)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91ddfb5eda44b9f64a4ba86559459a10c0) — the run attests the built-status claim, not the required outcome.

- No `application` locus is stated: the schema's application DU covers row-guard and event-arg-declaration additions only, not a field declaration's own modifier list — recorded as an honest schema gap rather than a forced fit.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| field-modifier-positive | field Parts as decimal default 1.0 nonnegative editable | nonnegative admits zero — must still reject, same obligation, under the model | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Built-status gap, live-measured: base compiled clean at HEAD (e1a14d91, 2026-07-21, Precept.MatrixTools) with zero diagnostics, contradicting the model's required rejection — the same no-mint gap as the reject-message category. Base and near-miss provenance are marked live-verified to report the literal result honestly, not to claim confirmation of the required outcome.
- This cell's witness deliberately avoids a fractional numeric literal (e.g. `0.0`) inside a `number`-typed rule's condition: measured at HEAD, `rule Total >= 0.0` on a `number` field raises `TypeMismatch: Expected a number value here, but got 'decimal'`, while the whole-number literal `rule Total >= 0` does not. This looks like a real type-resolution gap on the `number` lane inside rule conditions specifically (comparisons elsewhere resolve a fractional literal against a `number` peer correctly per docs/language/primitive-types.md), but it is outside this group's scope (division-by-zero at a message-interpolation site) and is recorded here only to explain why the `number`-lane and `integer`-lane witnesses use whole-number defaults throughout, not as a finding of this cell.
- No canonical WP key is pinned: the WP calculator computes weakest preconditions through write plans, and a rule's `because` string is a no-write evaluation site — a named skip, consistent with docs/Working/obligation-discharge-matrix-2026-07-19.md § Storage.
- Open dependency carried, not answered: whether a rule's own condition (or, for a conditional rule, its `when`) is available as a premise for its own `because` message is not settled by anything the matrix currently states — the matrix's own text observes only that 'the `because` message renders only when the constraint is false', which is a fact about when the site evaluates, not about what premises are available there.

**What this cell derives from**

- docs/language/precept-language-spec.md:288 — spec: mandatory `because` clauses
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- src/Precept/Language/Operations.cs:159 — code: OperationKind.DecimalModuloDecimal declaration
- src/Precept/Language/Operations.cs:165 — code: the 'Divisor must be non-zero' NumericProofRequirement
- docs/language/precept-language-spec.md § 2.5 Interpolation Reassembly — spec
- samples/loan-application.precept:38 — code: a shipped precedent of arithmetic inside a rule's `because` string

