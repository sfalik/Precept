<!--
GENERATED FILE — do not hand-edit.
Source: fault-4-division-primitive-constraint-positions.cells.json
Regenerate: dotnet run --project tools/Precept.MatrixTools -- render-cells docs/Working/obligation-discharge-matrix-2026-07-19-cells
Hand edits will be overwritten. Edit the .cells.json and regenerate.
-->

# Division by zero in a constraint condition or a computed field, quantified over every configuration

Family id: fault-4-division-primitive-constraint-positions
Case shape: fault
Definition version: obligation-discharge-matrix-2026-07-19 rev 7 (2026-07-20, draft)

**Where this family's content comes from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row: catalog-declared safety precondition at the evaluation site
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 -- fault family, arg-constraint discharge (the decision procedure this file generalizes from class (b) to class (a))
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — matrix: Fault minting: catalog-stamped at evaluation sites
- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault family origin
- src/Precept/Language/Operations.cs:119 — code
- src/Precept/Language/Operations.cs:150 — code
- src/Precept/Language/Modifiers.cs:100 — code

## Respellability — the family verdict

Can every safe program these rules reject be rewritten into a form they accept? **yes**

Evidence: model-derived
Corpus measurement: pending

Spellings that look rejected but are in fact licensed (kept so the boundary stays findable):

- `field Parts as integer default 1 editable min 1` — A `min 1` bound conjunct implies `Parts >= 1 > 0`, so it excludes zero by the same interval derivation as `nonzero`, just via a different declared modifier. Per-term bound conjuncts feed the same interval arithmetic that discharges the field's own modifier bounds, wherever spelled (matrix § Vocabulary, the sound-but-unprovable-band bullet, generalized here from a guard conjunct to an alternate modifier).
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — owner-ruling, 2026-07-19: the sound-but-unprovable band bullet


**Notes on the family verdict**

- All ten cells inherit this verdict; none states an override. The band member every cell shares is a divisor whose non-zero-ness is true but established only by an algebraic or cross-field fact never restated as a direct modifier on the divisor itself (e.g. `Parts = OtherField + 1` where `OtherField` is declared `nonnegative`, with no bound on `Parts` directly) -- sound, but not discharged by any written derivation. Its respelling is to restate the fact directly as a modifier on the divisor field (`nonzero`, `positive`, or `min` with a positive floor).
- The quantifier-predicate cells' band question is additionally gated on Q10 (whether quantified constraints are proof surface at all); if Q10 resolves 'no', those two cells' respellability verdict is moot rather than yes/no.

**Sources for this verdict**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the sound-but-unprovable band and respellability definitions
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Ratification protocol — matrix: layer 4: corpus measurement is the hard gate

## Premise classes — the legend

Where a proof may get its facts (want :19, :143): (a) field modifiers, (b) arg constraints, (c) the handler's guard, (d) all rules holding in the pre-state (the inductive hypothesis), (e) evaluating the applicable rule set at an editable-field write — (e)'s status as a genuinely fifth class vs (a) generalized is an open owner item.

## fault-4/rule-condition-integer — Rule condition -- division fault quantified over every configuration (integer lane)

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault family origin

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | op/IntegerDivideInteger/numeric-0 |
| evaluation site category | rule-condition |
| type family | primitive |

### What must be proven

Obligation: Parts != 0

Weakest precondition: Parts != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Parts | the field read as the divisor in the rule condition |
| 0 | the catalog-declared threshold the divisor must not equal |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a).

A rule condition is not anchored to any handler: no guard and no event args are in scope, and there is no single pre-state, so the fault obligation must hold over every configuration the entity can occupy. Only class (a), field modifiers, is available without a further ruling (group authoring note); the broader reading that would treat 'other constraints hold in this configuration' as class (d) is exactly what the Part 3 false-proof defect shows going wrong, and is not adopted here.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: field modifier fact: `Parts nonzero` excludes zero from the divisor's declared interval
- Strategy: IntervalContainment
- Capability tier: 1a
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor's interval from its declared modifiers (a finite set); zero inside the interval, or no bound excluding it, rejects.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 -- fault family, arg-constraint discharge: 'compute the divisor's interval from its declared modifiers and in-scope guard facts (a finite set per site); zero inside the interval, or no bound excluding it, rejects' -- generalized here from an arg (b) to a field modifier (a)
- src/Precept/Language/Modifiers.cs:100 — code: ModifierKind.Nonzero -- Value != 0, enforced on every assignment (DesugarsToRule: true)

### What the failing diagnostic must suggest

- For class (a): declare the divisor field's modifiers such that its interval excludes zero, satisfying <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
field Total as integer default 10 editable
field Parts as integer default 1 editable
rule Total / Parts < 100 because "Per-part total {Total / Parts} must stay under 100"
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*parts-nonzero* — other

- Addition: nonzero
- Premise classes: (a)
- Derivation: field modifier fact Parts != 0 excludes zero from the divisor's declared interval -> IntervalContainment
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live run: base + `Parts ... nonzero` compiles with zero diagnostics; the tool's WP trace prints `Parts:nonzero: WP = true` alongside the rule's own establishment WP, confirming the modifier is consumed.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| parts-nonzero | nonnegative | nonnegative admits zero -- the interval [0 .. +inf) still contains the divisor's excluded value, so the same DivisionByZero fault is rejected | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- The discharge contract's validityArguments cites 'Arg-bound interval arithmetic' as the closest written argument, but that argument's own text (matrix § Rule validity, the Validity arguments section) is scoped to 'every declared constraint on every value entering as an event arg' at ingress -- it does not itself cover a field modifier's fact holding at a no-write, all-configuration evaluation site. The reasoning generalizes (Modifiers.cs documents nonzero/positive/etc. as 'enforced on every assignment', so the fact holds of the current configuration regardless of how it was reached), and Family 4's own witness (matrix Witnesses, Family 4) demonstrates the identical interval-exclusion derivation via premise class (b). But no validity argument in the matrix's closed list is written for the field-modifier case. This is recorded here as a missing rule -- the Validity arguments section owes an argument for 'a declared field-modifier bound holds in every reachable configuration because governance enforces it on every assignment' -- not invented into the citation.
- No `application` block is recorded on this cell's discharge or near-miss witnesses: the schema's `application` union has exactly two loci (`row-guard`, appending a guard to a named event's row; `event-arg-declarations`, replacing a named event's arg declaration) -- both anchored to an event. This group's discharge adds a value modifier to a bare FIELD declaration at a site with no handler, no event, and (for rule-condition / computed-field-expression / quantifier-predicate) no state graph at all. Neither locus can express 'append a modifier to this field's own declaration line'. This is recorded as a missing rule for the cell-to-test conversion machinery (a third `field-declaration` locus is needed), not papered over by misusing the event-arg locus on a non-event field. The same gap shows up one level up: the discharge witness `kind` enum (`guard` / `arg-modifier` / `default-constant-fold` / `structural-edit` / `other`) has no member for 'a field's own declared value modifier' either -- `arg-modifier` names event args specifically. Recorded here as `other` rather than misapplied as `arg-modifier`, and flagged as the same missing-vocabulary finding.
- No WP canonical key is recorded: tools/Precept.MatrixTools's WP calculator computes rule-write obligations by backward substitution through a write plan; this fault obligation is evaluated directly against the expression's declared safety precondition at a position with no write plan to substitute through. Recorded as a named gap (README's 'named skip' discipline), not silently omitted.

**What this cell derives from**

- docs/language/precept-language-spec.md § `rule` declaration — spec
- docs/language/precept-language-spec.md § Expression scope — spec
- docs/Working/what-i-want-2026-07-16.md:104 — want-doc
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row: catalog-declared safety precondition at the evaluation site
- src/Precept/Language/Operations.cs:119 — code: OperationKind.IntegerDivideInteger -- Divisor must be non-zero (both operand slots share one ParameterMeta instance; ProofEngine.ResolveParamInBinaryOp resolves the divisor to the RIGHT operand by convention)

## fault-4/rule-condition-decimal — Rule condition -- division fault quantified over every configuration (decimal lane)

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault family origin

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | op/DecimalDivideDecimal/numeric-0 |
| evaluation site category | rule-condition |
| type family | primitive |

### What must be proven

Obligation: Parts != 0

Weakest precondition: Parts != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Parts | the field read as the divisor in the rule condition |
| 0 | the catalog-declared threshold the divisor must not equal |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a).

A rule condition is not anchored to any handler: no guard and no event args are in scope, and there is no single pre-state, so the fault obligation must hold over every configuration the entity can occupy. Only class (a), field modifiers, is available without a further ruling (group authoring note); the broader reading that would treat 'other constraints hold in this configuration' as class (d) is exactly what the Part 3 false-proof defect shows going wrong, and is not adopted here.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: field modifier fact: `Parts nonzero` excludes zero from the divisor's declared interval
- Strategy: IntervalContainment
- Capability tier: 1a
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor's interval from its declared modifiers (a finite set); zero inside the interval, or no bound excluding it, rejects.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 -- fault family, arg-constraint discharge: 'compute the divisor's interval from its declared modifiers and in-scope guard facts (a finite set per site); zero inside the interval, or no bound excluding it, rejects' -- generalized here from an arg (b) to a field modifier (a)
- src/Precept/Language/Modifiers.cs:100 — code: ModifierKind.Nonzero -- Value != 0, enforced on every assignment (DesugarsToRule: true)

### What the failing diagnostic must suggest

- For class (a): declare the divisor field's modifiers such that its interval excludes zero, satisfying <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
field Total as decimal default 10.0 editable
field Parts as decimal default 1.0 editable
rule Total / Parts < 100.0 because "Per-part total {Total / Parts} must stay under 100.0"
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*parts-nonzero* — other

- Addition: nonzero
- Premise classes: (a)
- Derivation: field modifier fact Parts != 0 excludes zero from the divisor's declared interval -> IntervalContainment
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live run: base + `Parts ... nonzero` compiles with zero diagnostics; the tool's WP trace prints `Parts:nonzero: WP = true` alongside the rule's own establishment WP, confirming the modifier is consumed.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| parts-nonzero | nonnegative | nonnegative admits zero -- the interval [0 .. +inf) still contains the divisor's excluded value, so the same DivisionByZero fault is rejected | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- The discharge contract's validityArguments cites 'Arg-bound interval arithmetic' as the closest written argument, but that argument's own text (matrix § Rule validity, the Validity arguments section) is scoped to 'every declared constraint on every value entering as an event arg' at ingress -- it does not itself cover a field modifier's fact holding at a no-write, all-configuration evaluation site. The reasoning generalizes (Modifiers.cs documents nonzero/positive/etc. as 'enforced on every assignment', so the fact holds of the current configuration regardless of how it was reached), and Family 4's own witness (matrix Witnesses, Family 4) demonstrates the identical interval-exclusion derivation via premise class (b). But no validity argument in the matrix's closed list is written for the field-modifier case. This is recorded here as a missing rule -- the Validity arguments section owes an argument for 'a declared field-modifier bound holds in every reachable configuration because governance enforces it on every assignment' -- not invented into the citation.
- No `application` block is recorded on this cell's discharge or near-miss witnesses: the schema's `application` union has exactly two loci (`row-guard`, appending a guard to a named event's row; `event-arg-declarations`, replacing a named event's arg declaration) -- both anchored to an event. This group's discharge adds a value modifier to a bare FIELD declaration at a site with no handler, no event, and (for rule-condition / computed-field-expression / quantifier-predicate) no state graph at all. Neither locus can express 'append a modifier to this field's own declaration line'. This is recorded as a missing rule for the cell-to-test conversion machinery (a third `field-declaration` locus is needed), not papered over by misusing the event-arg locus on a non-event field. The same gap shows up one level up: the discharge witness `kind` enum (`guard` / `arg-modifier` / `default-constant-fold` / `structural-edit` / `other`) has no member for 'a field's own declared value modifier' either -- `arg-modifier` names event args specifically. Recorded here as `other` rather than misapplied as `arg-modifier`, and flagged as the same missing-vocabulary finding.
- No WP canonical key is recorded: tools/Precept.MatrixTools's WP calculator computes rule-write obligations by backward substitution through a write plan; this fault obligation is evaluated directly against the expression's declared safety precondition at a position with no write plan to substitute through. Recorded as a named gap (README's 'named skip' discipline), not silently omitted.

**What this cell derives from**

- docs/language/precept-language-spec.md § `rule` declaration — spec
- docs/language/precept-language-spec.md § Expression scope — spec
- docs/Working/what-i-want-2026-07-16.md:104 — want-doc
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row: catalog-declared safety precondition at the evaluation site
- src/Precept/Language/Operations.cs:150 — code: OperationKind.DecimalDivideDecimal -- Divisor must be non-zero (both operand slots share one ParameterMeta instance; ProofEngine.ResolveParamInBinaryOp resolves the divisor to the RIGHT operand by convention)

## fault-4/state-ensure-condition-integer — State-anchored ensure condition (`in S ensure`) -- division fault quantified over every configuration (integer lane)

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault family origin

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | op/IntegerDivideInteger/numeric-0 |
| evaluation site category | state-ensure-condition |
| type family | primitive |

### What must be proven

Obligation: Parts != 0

Weakest precondition: Parts != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Parts | the field read as the divisor in the state ensure's condition |
| 0 | the catalog-declared threshold the divisor must not equal |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a).

A state-anchored ensure fires on every entry into S and holds throughout residency, so its fault obligation is quantified over every configuration the entity occupies while resident, not anchored to one handler. The evaluation-site enumeration's grammatical reading lists (c) (the ensure's own optional pre-verb `when`) and (d) (pre-state constraints) as syntactically plausible here, but the group's binding authoring note restricts the applicable-class set to {(a)} and records the (b)/(c)/(d) widening as an unadopted, unruled extension: (d) specifically is the reading the Part 3 false-proof defect shows failing. This cell also sits against the matrix's own open item on whether the residency fact itself -- the entity is in S -- is available as a premise at all, and under which class (matrix:150); that question is carried, not answered, and is independent of the (a)-only restriction stated above.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: field modifier fact: `Parts nonzero` excludes zero from the divisor's declared interval
- Strategy: IntervalContainment
- Capability tier: 1a
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor's interval from its declared modifiers (a finite set); zero inside the interval, or no bound excluding it, rejects.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 -- fault family, arg-constraint discharge: 'compute the divisor's interval from its declared modifiers and in-scope guard facts (a finite set per site); zero inside the interval, or no bound excluding it, rejects' -- generalized here from an arg (b) to a field modifier (a)
- src/Precept/Language/Modifiers.cs:100 — code: ModifierKind.Nonzero -- Value != 0, enforced on every assignment (DesugarsToRule: true)

### What the failing diagnostic must suggest

- For class (a): declare the divisor field's modifiers such that its interval excludes zero, satisfying <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
field Total as integer default 10 editable
field Parts as integer default 1 editable
state Draft initial
state Done terminal
event Finish
in Done ensure Total / Parts < 100 because "Per-part total {Total / Parts} must stay under 100 while Done"
from Draft on Finish
    -> transition Done
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*parts-nonzero* — other

- Addition: nonzero
- Premise classes: (a)
- Derivation: field modifier fact Parts != 0 excludes zero from the divisor's declared interval -> IntervalContainment
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live run: base + `Parts ... nonzero` compiles with zero diagnostics; the tool's WP trace prints `Parts:nonzero: WP = true`.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| parts-nonzero | nonnegative | nonnegative admits zero -- must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- The discharge contract's validityArguments cites 'Arg-bound interval arithmetic' as the closest written argument, but that argument's own text (matrix § Rule validity, the Validity arguments section) is scoped to 'every declared constraint on every value entering as an event arg' at ingress -- it does not itself cover a field modifier's fact holding at a no-write, all-configuration evaluation site. The reasoning generalizes (Modifiers.cs documents nonzero/positive/etc. as 'enforced on every assignment', so the fact holds of the current configuration regardless of how it was reached), and Family 4's own witness (matrix Witnesses, Family 4) demonstrates the identical interval-exclusion derivation via premise class (b). But no validity argument in the matrix's closed list is written for the field-modifier case. This is recorded here as a missing rule -- the Validity arguments section owes an argument for 'a declared field-modifier bound holds in every reachable configuration because governance enforces it on every assignment' -- not invented into the citation.
- No `application` block is recorded on this cell's discharge or near-miss witnesses: the schema's `application` union has exactly two loci (`row-guard`, appending a guard to a named event's row; `event-arg-declarations`, replacing a named event's arg declaration) -- both anchored to an event. This group's discharge adds a value modifier to a bare FIELD declaration at a site with no handler, no event, and (for rule-condition / computed-field-expression / quantifier-predicate) no state graph at all. Neither locus can express 'append a modifier to this field's own declaration line'. This is recorded as a missing rule for the cell-to-test conversion machinery (a third `field-declaration` locus is needed), not papered over by misusing the event-arg locus on a non-event field. The same gap shows up one level up: the discharge witness `kind` enum (`guard` / `arg-modifier` / `default-constant-fold` / `structural-edit` / `other`) has no member for 'a field's own declared value modifier' either -- `arg-modifier` names event args specifically. Recorded here as `other` rather than misapplied as `arg-modifier`, and flagged as the same missing-vocabulary finding.
- The matrix's open item on the residency fact (matrix:150) has no assigned ⧖ Q/S identifier in the matrix text, unlike Q7/Q10/Q13/Q14a, so it is carried here in notes rather than in openDependencies (whose pattern requires a real Q/S id).
- No WP canonical key is recorded: tools/Precept.MatrixTools's WP calculator computes rule-write obligations by backward substitution through a write plan; this fault obligation is evaluated directly against the expression's declared safety precondition at a position with no write plan to substitute through. Recorded as a named gap (README's 'named skip' discipline), not silently omitted.

**What this cell derives from**

- docs/language/precept-language-spec.md § State/event ensure — spec
- docs/language/precept-language-spec.md § Expression scope — spec
- docs/Working/obligation-discharge-matrix-2026-07-19.md:150 — matrix: the StateResident case-shape row and its residency-fact-as-premise open item
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- src/Precept/Language/Operations.cs:119 — code: OperationKind.IntegerDivideInteger -- Divisor must be non-zero (both operand slots share one ParameterMeta instance; ProofEngine.ResolveParamInBinaryOp resolves the divisor to the RIGHT operand by convention)

## fault-4/state-ensure-condition-decimal — State-anchored ensure condition (`in S ensure`) -- division fault quantified over every configuration (decimal lane)

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault family origin

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | op/DecimalDivideDecimal/numeric-0 |
| evaluation site category | state-ensure-condition |
| type family | primitive |

### What must be proven

Obligation: Parts != 0

Weakest precondition: Parts != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Parts | the field read as the divisor in the state ensure's condition |
| 0 | the catalog-declared threshold the divisor must not equal |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a).

A state-anchored ensure fires on every entry into S and holds throughout residency, so its fault obligation is quantified over every configuration the entity occupies while resident, not anchored to one handler. The evaluation-site enumeration's grammatical reading lists (c) (the ensure's own optional pre-verb `when`) and (d) (pre-state constraints) as syntactically plausible here, but the group's binding authoring note restricts the applicable-class set to {(a)} and records the (b)/(c)/(d) widening as an unadopted, unruled extension: (d) specifically is the reading the Part 3 false-proof defect shows failing. This cell also sits against the matrix's own open item on whether the residency fact itself -- the entity is in S -- is available as a premise at all, and under which class (matrix:150); that question is carried, not answered, and is independent of the (a)-only restriction stated above.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: field modifier fact: `Parts nonzero` excludes zero from the divisor's declared interval
- Strategy: IntervalContainment
- Capability tier: 1a
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor's interval from its declared modifiers (a finite set); zero inside the interval, or no bound excluding it, rejects.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 -- fault family, arg-constraint discharge: 'compute the divisor's interval from its declared modifiers and in-scope guard facts (a finite set per site); zero inside the interval, or no bound excluding it, rejects' -- generalized here from an arg (b) to a field modifier (a)
- src/Precept/Language/Modifiers.cs:100 — code: ModifierKind.Nonzero -- Value != 0, enforced on every assignment (DesugarsToRule: true)

### What the failing diagnostic must suggest

- For class (a): declare the divisor field's modifiers such that its interval excludes zero, satisfying <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
field Total as decimal default 10.0 editable
field Parts as decimal default 1.0 editable
state Draft initial
state Done terminal
event Finish
in Done ensure Total / Parts < 100.0 because "Per-part total {Total / Parts} must stay under 100.0 while Done"
from Draft on Finish
    -> transition Done
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*parts-nonzero* — other

- Addition: nonzero
- Premise classes: (a)
- Derivation: field modifier fact Parts != 0 excludes zero from the divisor's declared interval -> IntervalContainment
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live run: base + `Parts ... nonzero` compiles with zero diagnostics; the tool's WP trace prints `Parts:nonzero: WP = true`.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| parts-nonzero | nonnegative | nonnegative admits zero -- must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- The discharge contract's validityArguments cites 'Arg-bound interval arithmetic' as the closest written argument, but that argument's own text (matrix § Rule validity, the Validity arguments section) is scoped to 'every declared constraint on every value entering as an event arg' at ingress -- it does not itself cover a field modifier's fact holding at a no-write, all-configuration evaluation site. The reasoning generalizes (Modifiers.cs documents nonzero/positive/etc. as 'enforced on every assignment', so the fact holds of the current configuration regardless of how it was reached), and Family 4's own witness (matrix Witnesses, Family 4) demonstrates the identical interval-exclusion derivation via premise class (b). But no validity argument in the matrix's closed list is written for the field-modifier case. This is recorded here as a missing rule -- the Validity arguments section owes an argument for 'a declared field-modifier bound holds in every reachable configuration because governance enforces it on every assignment' -- not invented into the citation.
- No `application` block is recorded on this cell's discharge or near-miss witnesses: the schema's `application` union has exactly two loci (`row-guard`, appending a guard to a named event's row; `event-arg-declarations`, replacing a named event's arg declaration) -- both anchored to an event. This group's discharge adds a value modifier to a bare FIELD declaration at a site with no handler, no event, and (for rule-condition / computed-field-expression / quantifier-predicate) no state graph at all. Neither locus can express 'append a modifier to this field's own declaration line'. This is recorded as a missing rule for the cell-to-test conversion machinery (a third `field-declaration` locus is needed), not papered over by misusing the event-arg locus on a non-event field. The same gap shows up one level up: the discharge witness `kind` enum (`guard` / `arg-modifier` / `default-constant-fold` / `structural-edit` / `other`) has no member for 'a field's own declared value modifier' either -- `arg-modifier` names event args specifically. Recorded here as `other` rather than misapplied as `arg-modifier`, and flagged as the same missing-vocabulary finding.
- The matrix's open item on the residency fact (matrix:150) has no assigned ⧖ Q/S identifier in the matrix text, unlike Q7/Q10/Q13/Q14a, so it is carried here in notes rather than in openDependencies (whose pattern requires a real Q/S id).
- No WP canonical key is recorded: tools/Precept.MatrixTools's WP calculator computes rule-write obligations by backward substitution through a write plan; this fault obligation is evaluated directly against the expression's declared safety precondition at a position with no write plan to substitute through. Recorded as a named gap (README's 'named skip' discipline), not silently omitted.

**What this cell derives from**

- docs/language/precept-language-spec.md § State/event ensure — spec
- docs/language/precept-language-spec.md § Expression scope — spec
- docs/Working/obligation-discharge-matrix-2026-07-19.md:150 — matrix: the StateResident case-shape row and its residency-fact-as-premise open item
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- src/Precept/Language/Operations.cs:150 — code: OperationKind.DecimalDivideDecimal -- Divisor must be non-zero (both operand slots share one ParameterMeta instance; ProofEngine.ResolveParamInBinaryOp resolves the divisor to the RIGHT operand by convention)

## fault-4/event-ensure-condition-integer — Event ensure condition (`on E ensure`) -- division fault quantified over every configuration (integer lane)

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault family origin

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | op/IntegerDivideInteger/numeric-0 |
| evaluation site category | event-ensure-condition |
| type family | primitive |

### What must be proven

Obligation: Parts != 0

Weakest precondition: Parts != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Parts | the field read as the divisor in the event ensure's condition |
| 0 | the catalog-declared threshold the divisor must not equal |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a).

An event ensure checks its condition against the event's arguments before any mutation runs; the matrix names this the ingress door that makes premise class (b) true for arg-typed values (matrix:153), and the expression-scope table (spec § Expression scope) additionally admits all field names plus the current event's args in an ensure condition, so (b) is grammatically plausible whenever the divisor is itself an event arg. This cell's witness places the divisor on a FIELD rather than an event arg specifically to keep its discharge inside the group's ruled {(a)} set; a witness with an arg-typed divisor would raise the same class-(b) question this group does not adopt, for the same 'only (a) without further ruling' reason. The (c)/(d) widening the general evaluation-site reading admits is likewise not adopted, per the group's binding authoring note and the Part 3 false-proof hazard for class (d) specifically.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: field modifier fact: `Parts nonzero` excludes zero from the divisor's declared interval
- Strategy: IntervalContainment
- Capability tier: 1a
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor's interval from its declared modifiers (a finite set); zero inside the interval, or no bound excluding it, rejects.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 -- fault family, arg-constraint discharge: 'compute the divisor's interval from its declared modifiers and in-scope guard facts (a finite set per site); zero inside the interval, or no bound excluding it, rejects' -- generalized here from an arg (b) to a field modifier (a)
- src/Precept/Language/Modifiers.cs:100 — code: ModifierKind.Nonzero -- Value != 0, enforced on every assignment (DesugarsToRule: true)

### What the failing diagnostic must suggest

- For class (a): declare the divisor field's modifiers such that its interval excludes zero, satisfying <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
field Total as integer default 10 editable
field Parts as integer default 1 editable
state Draft initial
state Done terminal
event Finish
on Finish ensure Total / Parts < 100 because "Per-part total {Total / Parts} must stay under 100 at Finish"
from Draft on Finish
    -> transition Done
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*parts-nonzero* — other

- Addition: nonzero
- Premise classes: (a)
- Derivation: field modifier fact Parts != 0 excludes zero from the divisor's declared interval -> IntervalContainment
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live run: base + `Parts ... nonzero` compiles with zero diagnostics; the tool's WP trace prints `Parts:nonzero: WP = true`.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| parts-nonzero | nonnegative | nonnegative admits zero -- must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- The discharge contract's validityArguments cites 'Arg-bound interval arithmetic' as the closest written argument, but that argument's own text (matrix § Rule validity, the Validity arguments section) is scoped to 'every declared constraint on every value entering as an event arg' at ingress -- it does not itself cover a field modifier's fact holding at a no-write, all-configuration evaluation site. The reasoning generalizes (Modifiers.cs documents nonzero/positive/etc. as 'enforced on every assignment', so the fact holds of the current configuration regardless of how it was reached), and Family 4's own witness (matrix Witnesses, Family 4) demonstrates the identical interval-exclusion derivation via premise class (b). But no validity argument in the matrix's closed list is written for the field-modifier case. This is recorded here as a missing rule -- the Validity arguments section owes an argument for 'a declared field-modifier bound holds in every reachable configuration because governance enforces it on every assignment' -- not invented into the citation.
- No `application` block is recorded on this cell's discharge or near-miss witnesses: the schema's `application` union has exactly two loci (`row-guard`, appending a guard to a named event's row; `event-arg-declarations`, replacing a named event's arg declaration) -- both anchored to an event. This group's discharge adds a value modifier to a bare FIELD declaration at a site with no handler, no event, and (for rule-condition / computed-field-expression / quantifier-predicate) no state graph at all. Neither locus can express 'append a modifier to this field's own declaration line'. This is recorded as a missing rule for the cell-to-test conversion machinery (a third `field-declaration` locus is needed), not papered over by misusing the event-arg locus on a non-event field. The same gap shows up one level up: the discharge witness `kind` enum (`guard` / `arg-modifier` / `default-constant-fold` / `structural-edit` / `other`) has no member for 'a field's own declared value modifier' either -- `arg-modifier` names event args specifically. Recorded here as `other` rather than misapplied as `arg-modifier`, and flagged as the same missing-vocabulary finding.
- No WP canonical key is recorded: tools/Precept.MatrixTools's WP calculator computes rule-write obligations by backward substitution through a write plan; this fault obligation is evaluated directly against the expression's declared safety precondition at a position with no write plan to substitute through. Recorded as a named gap (README's 'named skip' discipline), not silently omitted.

**What this cell derives from**

- docs/language/precept-language-spec.md § State/event ensure — spec
- docs/language/precept-language-spec.md § Event arg access — spec
- docs/Working/obligation-discharge-matrix-2026-07-19.md:153 — matrix: the EventPrecondition case-shape row -- ingress evaluation is the mechanism that makes premise (b) true
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- src/Precept/Language/Operations.cs:119 — code: OperationKind.IntegerDivideInteger -- Divisor must be non-zero (both operand slots share one ParameterMeta instance; ProofEngine.ResolveParamInBinaryOp resolves the divisor to the RIGHT operand by convention)

## fault-4/event-ensure-condition-decimal — Event ensure condition (`on E ensure`) -- division fault quantified over every configuration (decimal lane)

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault family origin

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | op/DecimalDivideDecimal/numeric-0 |
| evaluation site category | event-ensure-condition |
| type family | primitive |

### What must be proven

Obligation: Parts != 0

Weakest precondition: Parts != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Parts | the field read as the divisor in the event ensure's condition |
| 0 | the catalog-declared threshold the divisor must not equal |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a).

An event ensure checks its condition against the event's arguments before any mutation runs; the matrix names this the ingress door that makes premise class (b) true for arg-typed values (matrix:153), and the expression-scope table (spec § Expression scope) additionally admits all field names plus the current event's args in an ensure condition, so (b) is grammatically plausible whenever the divisor is itself an event arg. This cell's witness places the divisor on a FIELD rather than an event arg specifically to keep its discharge inside the group's ruled {(a)} set; a witness with an arg-typed divisor would raise the same class-(b) question this group does not adopt, for the same 'only (a) without further ruling' reason. The (c)/(d) widening the general evaluation-site reading admits is likewise not adopted, per the group's binding authoring note and the Part 3 false-proof hazard for class (d) specifically.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: field modifier fact: `Parts nonzero` excludes zero from the divisor's declared interval
- Strategy: IntervalContainment
- Capability tier: 1a
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor's interval from its declared modifiers (a finite set); zero inside the interval, or no bound excluding it, rejects.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 -- fault family, arg-constraint discharge: 'compute the divisor's interval from its declared modifiers and in-scope guard facts (a finite set per site); zero inside the interval, or no bound excluding it, rejects' -- generalized here from an arg (b) to a field modifier (a)
- src/Precept/Language/Modifiers.cs:100 — code: ModifierKind.Nonzero -- Value != 0, enforced on every assignment (DesugarsToRule: true)

### What the failing diagnostic must suggest

- For class (a): declare the divisor field's modifiers such that its interval excludes zero, satisfying <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
field Total as decimal default 10.0 editable
field Parts as decimal default 1.0 editable
state Draft initial
state Done terminal
event Finish
on Finish ensure Total / Parts < 100.0 because "Per-part total {Total / Parts} must stay under 100.0 at Finish"
from Draft on Finish
    -> transition Done
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*parts-nonzero* — other

- Addition: nonzero
- Premise classes: (a)
- Derivation: field modifier fact Parts != 0 excludes zero from the divisor's declared interval -> IntervalContainment
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live run: base + `Parts ... nonzero` compiles with zero diagnostics; the tool's WP trace prints `Parts:nonzero: WP = true`.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| parts-nonzero | nonnegative | nonnegative admits zero -- must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- The discharge contract's validityArguments cites 'Arg-bound interval arithmetic' as the closest written argument, but that argument's own text (matrix § Rule validity, the Validity arguments section) is scoped to 'every declared constraint on every value entering as an event arg' at ingress -- it does not itself cover a field modifier's fact holding at a no-write, all-configuration evaluation site. The reasoning generalizes (Modifiers.cs documents nonzero/positive/etc. as 'enforced on every assignment', so the fact holds of the current configuration regardless of how it was reached), and Family 4's own witness (matrix Witnesses, Family 4) demonstrates the identical interval-exclusion derivation via premise class (b). But no validity argument in the matrix's closed list is written for the field-modifier case. This is recorded here as a missing rule -- the Validity arguments section owes an argument for 'a declared field-modifier bound holds in every reachable configuration because governance enforces it on every assignment' -- not invented into the citation.
- No `application` block is recorded on this cell's discharge or near-miss witnesses: the schema's `application` union has exactly two loci (`row-guard`, appending a guard to a named event's row; `event-arg-declarations`, replacing a named event's arg declaration) -- both anchored to an event. This group's discharge adds a value modifier to a bare FIELD declaration at a site with no handler, no event, and (for rule-condition / computed-field-expression / quantifier-predicate) no state graph at all. Neither locus can express 'append a modifier to this field's own declaration line'. This is recorded as a missing rule for the cell-to-test conversion machinery (a third `field-declaration` locus is needed), not papered over by misusing the event-arg locus on a non-event field. The same gap shows up one level up: the discharge witness `kind` enum (`guard` / `arg-modifier` / `default-constant-fold` / `structural-edit` / `other`) has no member for 'a field's own declared value modifier' either -- `arg-modifier` names event args specifically. Recorded here as `other` rather than misapplied as `arg-modifier`, and flagged as the same missing-vocabulary finding.
- No WP canonical key is recorded: tools/Precept.MatrixTools's WP calculator computes rule-write obligations by backward substitution through a write plan; this fault obligation is evaluated directly against the expression's declared safety precondition at a position with no write plan to substitute through. Recorded as a named gap (README's 'named skip' discipline), not silently omitted.

**What this cell derives from**

- docs/language/precept-language-spec.md § State/event ensure — spec
- docs/language/precept-language-spec.md § Event arg access — spec
- docs/Working/obligation-discharge-matrix-2026-07-19.md:153 — matrix: the EventPrecondition case-shape row -- ingress evaluation is the mechanism that makes premise (b) true
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- src/Precept/Language/Operations.cs:150 — code: OperationKind.DecimalDivideDecimal -- Divisor must be non-zero (both operand slots share one ParameterMeta instance; ProofEngine.ResolveParamInBinaryOp resolves the divisor to the RIGHT operand by convention)

## fault-4/computed-field-expression-integer — Computed field expression (`<-`) -- division fault quantified over every configuration (integer lane)

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault family origin

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | op/IntegerDivideInteger/numeric-0 |
| evaluation site category | computed-field-expression |
| type family | primitive |

### What must be proven

Obligation: Parts != 0

Weakest precondition: Parts != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Parts | the field read as the divisor in the computed field's expression |
| 0 | the catalog-declared threshold the divisor must not equal |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a).

A computed field can never be assigned, but its expression is evaluated over the final configuration on every read, in every configuration the entity can occupy -- the same all-configurations quantification as a rule condition, with no guard and no event args in scope. Only class (a), field modifiers, is available without a further ruling; the class-(d) widening is not adopted here for the same Part 3 reason as the rule-condition cell.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: field modifier fact: `Parts nonzero` excludes zero from the divisor's declared interval
- Strategy: IntervalContainment
- Capability tier: 1a
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor's interval from its declared modifiers (a finite set); zero inside the interval, or no bound excluding it, rejects.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 -- fault family, arg-constraint discharge: 'compute the divisor's interval from its declared modifiers and in-scope guard facts (a finite set per site); zero inside the interval, or no bound excluding it, rejects' -- generalized here from an arg (b) to a field modifier (a)
- src/Precept/Language/Modifiers.cs:100 — code: ModifierKind.Nonzero -- Value != 0, enforced on every assignment (DesugarsToRule: true)

### What the failing diagnostic must suggest

- For class (a): declare the divisor field's modifiers such that its interval excludes zero, satisfying <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
field Total as integer default 10 editable
field Parts as integer default 1 editable
field PerPart as integer <- Total / Parts
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*parts-nonzero* — other

- Addition: nonzero
- Premise classes: (a)
- Derivation: field modifier fact Parts != 0 excludes zero from the divisor's declared interval -> IntervalContainment
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live run: base + `Parts ... nonzero` compiles with zero diagnostics; the tool's WP trace prints `Parts:nonzero: WP = true`.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| parts-nonzero | nonnegative | nonnegative admits zero -- must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- The discharge contract's validityArguments cites 'Arg-bound interval arithmetic' as the closest written argument, but that argument's own text (matrix § Rule validity, the Validity arguments section) is scoped to 'every declared constraint on every value entering as an event arg' at ingress -- it does not itself cover a field modifier's fact holding at a no-write, all-configuration evaluation site. The reasoning generalizes (Modifiers.cs documents nonzero/positive/etc. as 'enforced on every assignment', so the fact holds of the current configuration regardless of how it was reached), and Family 4's own witness (matrix Witnesses, Family 4) demonstrates the identical interval-exclusion derivation via premise class (b). But no validity argument in the matrix's closed list is written for the field-modifier case. This is recorded here as a missing rule -- the Validity arguments section owes an argument for 'a declared field-modifier bound holds in every reachable configuration because governance enforces it on every assignment' -- not invented into the citation.
- No `application` block is recorded on this cell's discharge or near-miss witnesses: the schema's `application` union has exactly two loci (`row-guard`, appending a guard to a named event's row; `event-arg-declarations`, replacing a named event's arg declaration) -- both anchored to an event. This group's discharge adds a value modifier to a bare FIELD declaration at a site with no handler, no event, and (for rule-condition / computed-field-expression / quantifier-predicate) no state graph at all. Neither locus can express 'append a modifier to this field's own declaration line'. This is recorded as a missing rule for the cell-to-test conversion machinery (a third `field-declaration` locus is needed), not papered over by misusing the event-arg locus on a non-event field. The same gap shows up one level up: the discharge witness `kind` enum (`guard` / `arg-modifier` / `default-constant-fold` / `structural-edit` / `other`) has no member for 'a field's own declared value modifier' either -- `arg-modifier` names event args specifically. Recorded here as `other` rather than misapplied as `arg-modifier`, and flagged as the same missing-vocabulary finding.
- No WP canonical key is recorded: tools/Precept.MatrixTools's WP calculator computes rule-write obligations by backward substitution through a write plan; this fault obligation is evaluated directly against the expression's declared safety precondition at a position with no write plan to substitute through. Recorded as a named gap (README's 'named skip' discipline), not silently omitted.

**What this cell derives from**

- docs/language/precept-language-spec.md § `field` declaration — spec
- docs/language/precept-language-spec.md § Expression scope — spec
- docs/language/precept-language-spec.md § Computed field validation — spec
- docs/Working/obligation-discharge-matrix-2026-07-19.md:127 — matrix: computed-field transitivity (write-site axis, mention-set leg (c))
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- src/Precept/Language/Operations.cs:119 — code: OperationKind.IntegerDivideInteger -- Divisor must be non-zero (both operand slots share one ParameterMeta instance; ProofEngine.ResolveParamInBinaryOp resolves the divisor to the RIGHT operand by convention)

## fault-4/computed-field-expression-decimal — Computed field expression (`<-`) -- division fault quantified over every configuration (decimal lane)

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault family origin

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | op/DecimalDivideDecimal/numeric-0 |
| evaluation site category | computed-field-expression |
| type family | primitive |

### What must be proven

Obligation: Parts != 0

Weakest precondition: Parts != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Parts | the field read as the divisor in the computed field's expression |
| 0 | the catalog-declared threshold the divisor must not equal |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a).

A computed field can never be assigned, but its expression is evaluated over the final configuration on every read, in every configuration the entity can occupy -- the same all-configurations quantification as a rule condition, with no guard and no event args in scope. Only class (a), field modifiers, is available without a further ruling; the class-(d) widening is not adopted here for the same Part 3 reason as the rule-condition cell.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: field modifier fact: `Parts nonzero` excludes zero from the divisor's declared interval
- Strategy: IntervalContainment
- Capability tier: 1a
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor's interval from its declared modifiers (a finite set); zero inside the interval, or no bound excluding it, rejects.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 -- fault family, arg-constraint discharge: 'compute the divisor's interval from its declared modifiers and in-scope guard facts (a finite set per site); zero inside the interval, or no bound excluding it, rejects' -- generalized here from an arg (b) to a field modifier (a)
- src/Precept/Language/Modifiers.cs:100 — code: ModifierKind.Nonzero -- Value != 0, enforced on every assignment (DesugarsToRule: true)

### What the failing diagnostic must suggest

- For class (a): declare the divisor field's modifiers such that its interval excludes zero, satisfying <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
field Total as decimal default 10.0 editable
field Parts as decimal default 1.0 editable
field PerPart as decimal <- Total / Parts
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*parts-nonzero* — other

- Addition: nonzero
- Premise classes: (a)
- Derivation: field modifier fact Parts != 0 excludes zero from the divisor's declared interval -> IntervalContainment
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live run: base + `Parts ... nonzero` compiles with zero diagnostics; the tool's WP trace prints `Parts:nonzero: WP = true`.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| parts-nonzero | nonnegative | nonnegative admits zero -- must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- The discharge contract's validityArguments cites 'Arg-bound interval arithmetic' as the closest written argument, but that argument's own text (matrix § Rule validity, the Validity arguments section) is scoped to 'every declared constraint on every value entering as an event arg' at ingress -- it does not itself cover a field modifier's fact holding at a no-write, all-configuration evaluation site. The reasoning generalizes (Modifiers.cs documents nonzero/positive/etc. as 'enforced on every assignment', so the fact holds of the current configuration regardless of how it was reached), and Family 4's own witness (matrix Witnesses, Family 4) demonstrates the identical interval-exclusion derivation via premise class (b). But no validity argument in the matrix's closed list is written for the field-modifier case. This is recorded here as a missing rule -- the Validity arguments section owes an argument for 'a declared field-modifier bound holds in every reachable configuration because governance enforces it on every assignment' -- not invented into the citation.
- No `application` block is recorded on this cell's discharge or near-miss witnesses: the schema's `application` union has exactly two loci (`row-guard`, appending a guard to a named event's row; `event-arg-declarations`, replacing a named event's arg declaration) -- both anchored to an event. This group's discharge adds a value modifier to a bare FIELD declaration at a site with no handler, no event, and (for rule-condition / computed-field-expression / quantifier-predicate) no state graph at all. Neither locus can express 'append a modifier to this field's own declaration line'. This is recorded as a missing rule for the cell-to-test conversion machinery (a third `field-declaration` locus is needed), not papered over by misusing the event-arg locus on a non-event field. The same gap shows up one level up: the discharge witness `kind` enum (`guard` / `arg-modifier` / `default-constant-fold` / `structural-edit` / `other`) has no member for 'a field's own declared value modifier' either -- `arg-modifier` names event args specifically. Recorded here as `other` rather than misapplied as `arg-modifier`, and flagged as the same missing-vocabulary finding.
- No WP canonical key is recorded: tools/Precept.MatrixTools's WP calculator computes rule-write obligations by backward substitution through a write plan; this fault obligation is evaluated directly against the expression's declared safety precondition at a position with no write plan to substitute through. Recorded as a named gap (README's 'named skip' discipline), not silently omitted.

**What this cell derives from**

- docs/language/precept-language-spec.md § `field` declaration — spec
- docs/language/precept-language-spec.md § Expression scope — spec
- docs/language/precept-language-spec.md § Computed field validation — spec
- docs/Working/obligation-discharge-matrix-2026-07-19.md:127 — matrix: computed-field transitivity (write-site axis, mention-set leg (c))
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- src/Precept/Language/Operations.cs:150 — code: OperationKind.DecimalDivideDecimal -- Divisor must be non-zero (both operand slots share one ParameterMeta instance; ProofEngine.ResolveParamInBinaryOp resolves the divisor to the RIGHT operand by convention)

## fault-4/quantifier-predicate-integer — Quantifier predicate (`each`/`any`/`no`) -- division fault quantified over every configuration (integer lane)

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault family origin

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | op/IntegerDivideInteger/numeric-0 |
| evaluation site category | quantifier-predicate |
| type family | primitive |

### What must be proven

Obligation: n != 0

Weakest precondition: n != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| n | the quantifier's binding variable, typed to the collection's inner type, read as the divisor |
| 0 | the catalog-declared threshold the divisor must not equal |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a).

The quantifier predicate nests inside a rule condition here, inheriting that host's all-configurations quantification and its field-modifiers-only premise availability. It also introduces a premise source no host site has: the binding variable's type is the collection's inner type (spec § Quantifier binding variable scope), so the collection's per-element inner-type value modifiers bound the value the variable can take, including at the divisor position. Only class (a) is available without a further ruling, matching the group's binding note. This cell is additionally authored against the open question of whether quantified constraints are proof surface at all (matrix:312, ⧖ Q10) -- carried as an openDependency, not resolved by authoring the cell.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: the quantifier's binding variable takes the collection's inner type (spec § Quantifier binding variable scope), so a per-element inner-type value modifier (`nonzero`) bounds every value the binding variable can take, including the divisor position inside the predicate
- Capability tier: 1a
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor's interval from the collection's declared per-element (inner-type) modifiers (a finite set); zero inside the interval, or no bound excluding it, rejects.
  - Open items this answer is load-bearing on: Q10

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4's decision procedure, generalized from an arg bound to a collection inner-type bound
- src/Precept/Language/Modifiers.cs:100 — code: ModifierKind.Nonzero -- Value != 0
- docs/language/precept-language-spec.md § Quantifier binding variable scope — spec: the binding variable's type is the collection's inner type
- docs/Working/obligation-discharge-matrix-2026-07-19.md:312 — matrix: ⧖ Q10 -- whether quantified constraints are proof surface at all is open; this cell is authored against that open dependency, not blocked by it

### What the failing diagnostic must suggest

- For class (a): declare the collection's inner-type value modifiers such that every element's interval excludes zero, satisfying <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
field Nums as set of integer editable
field Total as integer default 10 editable
rule each n in Nums (Total / n < 100) because "bounded"
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*nums-nonzero* — other

- Addition: nonzero
- Premise classes: (a)
- Derivation: collection inner-type value modifier fact n != 0 for every n in Nums, bounding the quantifier's binding variable away from zero at the divisor position
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Live-verified: adding `nonzero` to the collection's inner type does not change the compiler's output at all -- base, discharge, and near-miss all reject identically with the same DivisionByZero error. A control probe outside the quantifier (`Nums.first` on a `list of integer nonzero mincount 1`, dividing into a computed field) shows the same gap: the inner-type modifier is not consumed at an accessor-projected read either, matching the normal-form draft's own out-of-scope item 8 ('accessor-projected bounds ... yield an explicit skipped-obligation record'). The gap is in wiring inner-type modifier facts to any per-element proof site, not specific to quantifiers.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- This discharge witness's builtStatus is unverified-as-accepted in the ordinary sense: the live run shows the addition is NOT accepted at HEAD (still rejects). Recorded per the family-1 precedent for genuinely unresolved-today additions: modelStatus states the definition's committed power; builtStatus and its note state, honestly, that HEAD does not yet realize it. This is not the Part 3 false-proof hazard (no rule premise is consumed here) -- it is the opposite direction: the built engine under-proves relative to the model, which is a normal, allowed provenance combination per the cells README.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| nums-nonzero | nonnegative | nonnegative admits zero -- must still reject, same obligation (and at HEAD rejects for the additional, unrelated reason that neither modifier is consumed at this site yet) | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

- Open items this answer is load-bearing on: Q10
**What the sources leave unstated or ambiguous here**

- The discharge contract's validityArguments cites 'Arg-bound interval arithmetic' as the closest written argument, but that argument's own text (matrix § Rule validity, the Validity arguments section) is scoped to 'every declared constraint on every value entering as an event arg' at ingress -- it does not itself cover a field modifier's fact holding at a no-write, all-configuration evaluation site. The reasoning generalizes (Modifiers.cs documents nonzero/positive/etc. as 'enforced on every assignment', so the fact holds of the current configuration regardless of how it was reached), and Family 4's own witness (matrix Witnesses, Family 4) demonstrates the identical interval-exclusion derivation via premise class (b). But no validity argument in the matrix's closed list is written for the field-modifier case. This is recorded here as a missing rule -- the Validity arguments section owes an argument for 'a declared field-modifier bound holds in every reachable configuration because governance enforces it on every assignment' -- not invented into the citation.
- No `application` block is recorded on this cell's discharge or near-miss witnesses: the schema's `application` union has exactly two loci (`row-guard`, appending a guard to a named event's row; `event-arg-declarations`, replacing a named event's arg declaration) -- both anchored to an event. This group's discharge adds a value modifier to a bare FIELD declaration at a site with no handler, no event, and (for rule-condition / computed-field-expression / quantifier-predicate) no state graph at all. Neither locus can express 'append a modifier to this field's own declaration line'. This is recorded as a missing rule for the cell-to-test conversion machinery (a third `field-declaration` locus is needed), not papered over by misusing the event-arg locus on a non-event field. The same gap shows up one level up: the discharge witness `kind` enum (`guard` / `arg-modifier` / `default-constant-fold` / `structural-edit` / `other`) has no member for 'a field's own declared value modifier' either -- `arg-modifier` names event args specifically. Recorded here as `other` rather than misapplied as `arg-modifier`, and flagged as the same missing-vocabulary finding.
- collection-types.md:10 lists quantifier predicates themselves as 'Not yet built' in its implementation-state header; this cell's own live run shows the surrounding fault DOES mint (DivisionByZero fires), so the gap measured here is specifically inner-type-modifier consumption at the binding-variable position, not fault minting in general.
- No WP canonical key is recorded: tools/Precept.MatrixTools's WP calculator computes rule-write obligations by backward substitution through a write plan; this fault obligation is evaluated directly against the expression's declared safety precondition at a position with no write plan to substitute through. Recorded as a named gap (README's 'named skip' discipline), not silently omitted.

**What this cell derives from**

- docs/language/precept-language-spec.md § Quantifier expression grammar — spec
- docs/language/precept-language-spec.md § Quantifier binding variable scope — spec
- docs/language/precept-language-spec.md § Expression scope — spec
- docs/Working/obligation-discharge-matrix-2026-07-19.md:312 — matrix: ⧖ Q10 -- whether quantified constraints are proof surface at all is open
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- src/Precept/Language/Operations.cs:119 — code: OperationKind.IntegerDivideInteger -- Divisor must be non-zero (both operand slots share one ParameterMeta instance; ProofEngine.ResolveParamInBinaryOp resolves the divisor to the RIGHT operand by convention)

## fault-4/quantifier-predicate-decimal — Quantifier predicate (`each`/`any`/`no`) -- division fault quantified over every configuration (decimal lane)

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault family origin

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | op/DecimalDivideDecimal/numeric-0 |
| evaluation site category | quantifier-predicate |
| type family | primitive |

### What must be proven

Obligation: n != 0

Weakest precondition: n != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| n | the quantifier's binding variable, typed to the collection's inner type, read as the divisor |
| 0 | the catalog-declared threshold the divisor must not equal |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a).

The quantifier predicate nests inside a rule condition here, inheriting that host's all-configurations quantification and its field-modifiers-only premise availability. It also introduces a premise source no host site has: the binding variable's type is the collection's inner type (spec § Quantifier binding variable scope), so the collection's per-element inner-type value modifiers bound the value the variable can take, including at the divisor position. Only class (a) is available without a further ruling, matching the group's binding note. This cell is additionally authored against the open question of whether quantified constraints are proof surface at all (matrix:312, ⧖ Q10) -- carried as an openDependency, not resolved by authoring the cell.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: the quantifier's binding variable takes the collection's inner type (spec § Quantifier binding variable scope), so a per-element inner-type value modifier (`nonzero`) bounds every value the binding variable can take, including the divisor position inside the predicate
- Capability tier: 1a
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Compute the divisor's interval from the collection's declared per-element (inner-type) modifiers (a finite set); zero inside the interval, or no bound excluding it, rejects.
  - Open items this answer is load-bearing on: Q10

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4's decision procedure, generalized from an arg bound to a collection inner-type bound
- src/Precept/Language/Modifiers.cs:100 — code: ModifierKind.Nonzero -- Value != 0
- docs/language/precept-language-spec.md § Quantifier binding variable scope — spec: the binding variable's type is the collection's inner type
- docs/Working/obligation-discharge-matrix-2026-07-19.md:312 — matrix: ⧖ Q10 -- whether quantified constraints are proof surface at all is open; this cell is authored against that open dependency, not blocked by it

### What the failing diagnostic must suggest

- For class (a): declare the collection's inner-type value modifiers such that every element's interval excludes zero, satisfying <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
field Nums as set of decimal editable
field Total as decimal default 10.0 editable
rule each n in Nums (Total / n < 100.0) because "bounded"
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*nums-nonzero* — other

- Addition: nonzero
- Premise classes: (a)
- Derivation: collection inner-type value modifier fact n != 0 for every n in Nums, bounding the quantifier's binding variable away from zero at the divisor position
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Live-verified: adding `nonzero` to the collection's inner type does not change the compiler's output at all -- base, discharge, and near-miss all reject identically with the same DivisionByZero error. A control probe outside the quantifier (`Nums.first` on a `list of integer nonzero mincount 1`, dividing into a computed field) shows the same gap: the inner-type modifier is not consumed at an accessor-projected read either, matching the normal-form draft's own out-of-scope item 8 ('accessor-projected bounds ... yield an explicit skipped-obligation record'). The gap is in wiring inner-type modifier facts to any per-element proof site, not specific to quantifiers.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- This discharge witness's builtStatus is unverified-as-accepted in the ordinary sense: the live run shows the addition is NOT accepted at HEAD (still rejects). Recorded per the family-1 precedent for genuinely unresolved-today additions: modelStatus states the definition's committed power; builtStatus and its note state, honestly, that HEAD does not yet realize it. This is not the Part 3 false-proof hazard (no rule premise is consumed here) -- it is the opposite direction: the built engine under-proves relative to the model, which is a normal, allowed provenance combination per the cells README.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| nums-nonzero | nonnegative | nonnegative admits zero -- must still reject, same obligation (and at HEAD rejects for the additional, unrelated reason that neither modifier is consumed at this site yet) | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

- Open items this answer is load-bearing on: Q10
**What the sources leave unstated or ambiguous here**

- The discharge contract's validityArguments cites 'Arg-bound interval arithmetic' as the closest written argument, but that argument's own text (matrix § Rule validity, the Validity arguments section) is scoped to 'every declared constraint on every value entering as an event arg' at ingress -- it does not itself cover a field modifier's fact holding at a no-write, all-configuration evaluation site. The reasoning generalizes (Modifiers.cs documents nonzero/positive/etc. as 'enforced on every assignment', so the fact holds of the current configuration regardless of how it was reached), and Family 4's own witness (matrix Witnesses, Family 4) demonstrates the identical interval-exclusion derivation via premise class (b). But no validity argument in the matrix's closed list is written for the field-modifier case. This is recorded here as a missing rule -- the Validity arguments section owes an argument for 'a declared field-modifier bound holds in every reachable configuration because governance enforces it on every assignment' -- not invented into the citation.
- No `application` block is recorded on this cell's discharge or near-miss witnesses: the schema's `application` union has exactly two loci (`row-guard`, appending a guard to a named event's row; `event-arg-declarations`, replacing a named event's arg declaration) -- both anchored to an event. This group's discharge adds a value modifier to a bare FIELD declaration at a site with no handler, no event, and (for rule-condition / computed-field-expression / quantifier-predicate) no state graph at all. Neither locus can express 'append a modifier to this field's own declaration line'. This is recorded as a missing rule for the cell-to-test conversion machinery (a third `field-declaration` locus is needed), not papered over by misusing the event-arg locus on a non-event field. The same gap shows up one level up: the discharge witness `kind` enum (`guard` / `arg-modifier` / `default-constant-fold` / `structural-edit` / `other`) has no member for 'a field's own declared value modifier' either -- `arg-modifier` names event args specifically. Recorded here as `other` rather than misapplied as `arg-modifier`, and flagged as the same missing-vocabulary finding.
- collection-types.md:10 lists quantifier predicates themselves as 'Not yet built' in its implementation-state header; this cell's own live run shows the surrounding fault DOES mint (DivisionByZero fires), so the gap measured here is specifically inner-type-modifier consumption at the binding-variable position, not fault minting in general.
- No WP canonical key is recorded: tools/Precept.MatrixTools's WP calculator computes rule-write obligations by backward substitution through a write plan; this fault obligation is evaluated directly against the expression's declared safety precondition at a position with no write plan to substitute through. Recorded as a named gap (README's 'named skip' discipline), not silently omitted.

**What this cell derives from**

- docs/language/precept-language-spec.md § Quantifier expression grammar — spec
- docs/language/precept-language-spec.md § Quantifier binding variable scope — spec
- docs/language/precept-language-spec.md § Expression scope — spec
- docs/Working/obligation-discharge-matrix-2026-07-19.md:312 — matrix: ⧖ Q10 -- whether quantified constraints are proof surface at all is open
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- src/Precept/Language/Operations.cs:150 — code: OperationKind.DecimalDivideDecimal -- Divisor must be non-zero (both operand slots share one ParameterMeta instance; ProofEngine.ResolveParamInBinaryOp resolves the divisor to the RIGHT operand by convention)

