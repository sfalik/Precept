<!--
GENERATED FILE — do not hand-edit.
Source: fault-5-division-primitive-declaration-positions.cells.json
Regenerate: dotnet run --project tools/Precept.MatrixTools -- render-cells docs/Working/obligation-discharge-matrix-2026-07-19-cells
Hand edits will be overwritten. Edit the .cells.json and regenerate.
-->

# Fault group 5 — division by zero in a declaration-position value expression, primitive lanes

Family id: fault-5-division-primitive-declaration-positions
Case shape: fault
Definition version: obligation-discharge-matrix-2026-07-19 rev 7 (2026-07-20, draft)

**Where this family's content comes from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § The cell — matrix: fault-family obligation, contract, witness shape
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes (a discriminated union of axis sets) — matrix: fault family row — premise classes (b)/(c)/(a) per catalog ProofSatisfactions
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge (the precedent this group's contract generalizes from class (b) to class (a))
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — an explicit input — matrix: fault minting is catalog-stamped at evaluation sites
- docs/language/precept-language-spec.md § Expression scope — spec: the declaration-order and full-scope rules this group's derivations turn on
- docs/language/precept-language-spec.md § Type References — spec: type qualifier semantics, load-bearing for the type-qualifier empty cells
- docs/language/precept-language-spec.md § Field Modifiers — spec: constraint-modifier desugaring to rule shorthand
- src/Precept/Pipeline/ProofEngine.cs — code: ResolveParamInBinaryOp — the right-operand convention for the shared-ParameterMeta divisor sites
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md — packet: Part 3 — the false-proof hazard this group's categories are confirmed free of (no premise class (d) anywhere in this group's region)

## Respellability — the family verdict

Can every safe program these rules reject be rewritten into a form they accept? **yes**

Evidence: model-derived
Corpus measurement: pending

Spellings that look rejected but are in fact licensed (kept so the boundary stays findable):

- `field Reserved as integer max -1` — Looks unusual (an upper-only, negative bound) but is licensed by ordinary interval combination: `max -1` alone gives the interval (-inf, -1], which excludes zero exactly as `positive`/`nonzero` would, so the same decision procedure discharges it without needing either named sign modifier.
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 1 Base A's own note that per-term bound conjuncts feed the same interval arithmetic regardless of spelling


**Notes on the family verdict**

- Applies to the eight defined cells only; the two empty (type-qualifier) cells have no obligation and therefore no band.
- A genuine band member for this group: `max Budget / (Reserved + 1)` with `Reserved as integer nonnegative` is sound (Reserved+1 is always >= 1) but not covered by the stated decision procedure, which resolves only a bare single-field divisor reference, not a compound arithmetic expression. Respelling: introduce an intermediate field (e.g. `field ReservedPlusOne as integer <- Reserved + 1` with its own established `positive`) and divide by that field directly, which the bare-single-field derivation then covers.

## Premise classes — the legend

Where a proof may get its facts (want :19, :143): (a) field modifiers, (b) arg constraints, (c) the handler's guard, (d) all rules holding in the pre-state (the inductive hypothesis), (e) evaluating the applicable rule set at an editable-field write — (e)'s status as a genuinely fifth class vs (a) generalized is an open owner item.

## g05/field-default-integer — Field default expression divides — integer lane (IntegerDivideInteger)

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § The cell — matrix: item 1 — fault-family obligation schema is the catalog-declared safety precondition at the evaluation site
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — an explicit input — matrix: fault minting is catalog-stamped at evaluation sites

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | IntegerDivideInteger |
| evaluation site category | field-default-value-expression |
| type family | primitive |

### What must be proven

Obligation: PartsCount != 0

Weakest precondition: PartsCount != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| PartsCount | the divisor (right) operand of the IntegerDivideInteger expression at this evaluation site, per ProofEngine.ResolveParamInBinaryOp convention |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a).

At `field-default-value-expression` the scope is narrower than the other four categories in this group: the expression may reference only fields declared *before* this field — a forward reference is impossible in the grammar (construction materializes defaults in declaration order), not merely disallowed. So the only premise source available is an earlier-declared field's own modifier; there is no guard, no event arg, and no later-declared field in scope at this site.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: PartsCount is a field declared before UnitShare, whose default expression divides by it. If PartsCount carries a declared modifier combination whose exact interval excludes zero (e.g. `positive`, `nonzero`, or a `min`/`max` pair bounded away from zero), that field-modifier fact (premise class (a)) discharges the obligation.
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Resolve the divisor sub-expression to a single referenced field (reject the derivation if it is a compound expression with no single resolvable field — see the sound-but-unprovable band note below). Extract that field's exact interval by combining its declared numeric modifiers (`positive`/`nonnegative`/`nonzero`/`min`/`max`) under the same exact-decimal/exact-integer interval combination Family 1's class-(b) argument uses. Zero inside the combined interval, or no modifier excluding it, fails the derivation.

- docs/language/precept-language-spec.md:1350 — spec: Expression scope table — default value expression scope is fields declared before this field, no forward reference
- docs/language/precept-language-spec.md:1354 — spec: a forward reference from a default value expression is impossible, not merely disallowed, because construction materializes defaults in declaration order
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge: the precedent this contract generalizes from class (b) to class (a)

### What the failing diagnostic must suggest

- For class (a): declare the divisor field <WP> positive or nonzero
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept FdIntBase
field PartsCount as integer default 0
field UnitShare as integer default 100 / PartsCount
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*field-modifier-exclusion* — other

- Addition: replace `field PartsCount as integer default 0` with `field PartsCount as integer default 1 positive`
- Premise classes: (a)
- Derivation: PartsCount carries a nonzero-excluding modifier -> field-modifier interval exclusion (class (a), generalized Arg-bound interval arithmetic)
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (measured at HEAD (commit e1a14d91, 2026-07-21, scratch snippet `d.precept` and this cell's own live run): the site mints no fault obligation at all — the file compiles with zero diagnostics regardless of whether the divisor field carries a nonzero-excluding modifier. This is the site failing to mint, not the program being judged safe.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` block is given: the schema's `application` discriminated union offers only two loci (`row-guard`, appending a guard to a transition row; `event-arg-declarations`, replacing named event-arg declarations) and neither fits — this discharge edits the divisor field's own declaration, a locus the DU does not yet name. Recorded as a schema gap rather than forced into either existing locus.
- `dischargeWitness.kind` is recorded as `other`, not `arg-modifier`: the enum's `arg-modifier` value is precedented (Witness Family 1) for an *event-arg* modifier addition; this discharge adds a modifier to a *field* declaration instead, and no enum member names that shape. Recorded as a second schema gap alongside the `application` locus gap.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| field-modifier-exclusion | replace `field PartsCount as integer default 0` with `field PartsCount as integer default 0 nonnegative` | `nonnegative` admits zero — the combined interval still contains 0, so the derivation still fails and the obligation still rejects, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- No false-proof hazard: this evaluation-site category carries only premise class (a) (field modifiers) — never class (d) — so no discharge here can run through a business `rule` used as a premise (docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3, Defect B does not apply). Clean-compile confirmation of the field-modifier discharge would be admissible in principle; it is moot here because the site mints nothing at HEAD regardless (see builtStatusNote).
- This derivation cites "Arg-bound interval arithmetic", the closest written validity argument, but the matrix's own text for that argument (obligation-discharge-matrix-2026-07-19.md, the argument's paragraph) is scoped explicitly to premise class (b) (Witness Family 1, Base A) — declared *argument* bounds summed against a rule's bound. Applying the same interval-extraction technique to a premise class (a) *field* modifier discharging a divisor-exclusion check (rather than a sum-bound check) is an unstated generalization: the mechanism (exact-bound extraction, interval combination) is identical, but the matrix has never written this specific application out. Flagged as a missing rule, not silently assumed identical.
- wp.canonicalKey / canonicalKeySource omitted: the WP calculator (tools/Precept.MatrixTools) computes establishment/preservation WPs through a handler write plan; this obligation sits at a declaration-position evaluation site with no write plan, and the calculator has no computation path for it. Not measured, not inferred.
- Live-verified at HEAD (commit e1a14d91, Precept.MatrixTools, 2026-07-21): base, discharge, and near-miss all compile with zero fault-related diagnostics — the site mints nothing regardless of premises, so the run cannot distinguish base from discharge from near-miss on the fault obligation. The `expected: reject` / `expected: accept` blocks above state the model's committed power, not what the run showed; provenance is marked live-verified because the run itself is exactly what is reported, including its failure to confirm the model's expectation.
- This cell's schema also covers the other integer-lane catalog sites sharing the same 'Divisor must be non-zero' condition and the same discharge contract: IntegerModuloInteger, IntegerDivideDecimal, IntegerDivideNumber, IntegerModuloDecimal, IntegerModuloNumber.

**What this cell derives from**

- docs/language/precept-language-spec.md:1350 — spec: Expression scope table — default value expression scope is fields declared before this field, no forward reference
- docs/language/precept-language-spec.md:1354 — spec: a forward reference from a default value expression is impossible, not merely disallowed, because construction materializes defaults in declaration order
- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault-prevention obligation family
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: premise class (a) — field modifiers

## g05/field-default-decimal — Field default expression divides — decimal lane (DecimalDivideDecimal)

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § The cell — matrix: item 1 — fault-family obligation schema is the catalog-declared safety precondition at the evaluation site
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — an explicit input — matrix: fault minting is catalog-stamped at evaluation sites

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | DecimalDivideDecimal |
| evaluation site category | field-default-value-expression |
| type family | primitive |

### What must be proven

Obligation: Weight != 0

Weakest precondition: Weight != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Weight | the divisor (right) operand of the DecimalDivideDecimal expression at this evaluation site, per ProofEngine.ResolveParamInBinaryOp convention |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a).

At `field-default-value-expression` the scope is narrower than the other four categories in this group: the expression may reference only fields declared *before* this field — a forward reference is impossible in the grammar (construction materializes defaults in declaration order), not merely disallowed. So the only premise source available is an earlier-declared field's own modifier; there is no guard, no event arg, and no later-declared field in scope at this site.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: Weight is a field declared before Portion, whose default expression divides by it. If Weight carries a declared modifier combination whose exact interval excludes zero (e.g. `positive`, `nonzero`, or a `min`/`max` pair bounded away from zero), that field-modifier fact (premise class (a)) discharges the obligation.
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Resolve the divisor sub-expression to a single referenced field (reject the derivation if it is a compound expression with no single resolvable field — see the sound-but-unprovable band note below). Extract that field's exact interval by combining its declared numeric modifiers (`positive`/`nonnegative`/`nonzero`/`min`/`max`) under the same exact-decimal/exact-integer interval combination Family 1's class-(b) argument uses. Zero inside the combined interval, or no modifier excluding it, fails the derivation.

- docs/language/precept-language-spec.md:1350 — spec: Expression scope table — default value expression scope is fields declared before this field, no forward reference
- docs/language/precept-language-spec.md:1354 — spec: a forward reference from a default value expression is impossible, not merely disallowed, because construction materializes defaults in declaration order
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge: the precedent this contract generalizes from class (b) to class (a)

### What the failing diagnostic must suggest

- For class (a): declare the divisor field <WP> positive or nonzero
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept FdDecBase
field Weight as decimal default 0.0
field Portion as decimal default 100.0 / Weight
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*field-modifier-exclusion* — other

- Addition: replace `field Weight as decimal default 0.0` with `field Weight as decimal default 1.0 positive`
- Premise classes: (a)
- Derivation: Weight carries a nonzero-excluding modifier -> field-modifier interval exclusion (class (a), generalized Arg-bound interval arithmetic)
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (measured at HEAD (commit e1a14d91, 2026-07-21, scratch snippet `d.precept` and this cell's own live run): the site mints no fault obligation at all — the file compiles with zero diagnostics regardless of whether the divisor field carries a nonzero-excluding modifier. This is the site failing to mint, not the program being judged safe.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` block is given: the schema's `application` discriminated union offers only two loci (`row-guard`, appending a guard to a transition row; `event-arg-declarations`, replacing named event-arg declarations) and neither fits — this discharge edits the divisor field's own declaration, a locus the DU does not yet name. Recorded as a schema gap rather than forced into either existing locus.
- `dischargeWitness.kind` is recorded as `other`, not `arg-modifier`: the enum's `arg-modifier` value is precedented (Witness Family 1) for an *event-arg* modifier addition; this discharge adds a modifier to a *field* declaration instead, and no enum member names that shape. Recorded as a second schema gap alongside the `application` locus gap.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| field-modifier-exclusion | replace `field Weight as decimal default 0.0` with `field Weight as decimal default 0.0 nonnegative` | `nonnegative` admits zero — the combined interval still contains 0, so the derivation still fails and the obligation still rejects, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- No false-proof hazard: this evaluation-site category carries only premise class (a) (field modifiers) — never class (d) — so no discharge here can run through a business `rule` used as a premise (docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3, Defect B does not apply). Clean-compile confirmation of the field-modifier discharge would be admissible in principle; it is moot here because the site mints nothing at HEAD regardless (see builtStatusNote).
- This derivation cites "Arg-bound interval arithmetic", the closest written validity argument, but the matrix's own text for that argument (obligation-discharge-matrix-2026-07-19.md, the argument's paragraph) is scoped explicitly to premise class (b) (Witness Family 1, Base A) — declared *argument* bounds summed against a rule's bound. Applying the same interval-extraction technique to a premise class (a) *field* modifier discharging a divisor-exclusion check (rather than a sum-bound check) is an unstated generalization: the mechanism (exact-bound extraction, interval combination) is identical, but the matrix has never written this specific application out. Flagged as a missing rule, not silently assumed identical.
- wp.canonicalKey / canonicalKeySource omitted: the WP calculator (tools/Precept.MatrixTools) computes establishment/preservation WPs through a handler write plan; this obligation sits at a declaration-position evaluation site with no write plan, and the calculator has no computation path for it. Not measured, not inferred.
- Live-verified at HEAD (commit e1a14d91, Precept.MatrixTools, 2026-07-21): base, discharge, and near-miss all compile with zero fault-related diagnostics — the site mints nothing regardless of premises, so the run cannot distinguish base from discharge from near-miss on the fault obligation. The `expected: reject` / `expected: accept` blocks above state the model's committed power, not what the run showed; provenance is marked live-verified because the run itself is exactly what is reported, including its failure to confirm the model's expectation.
- This cell's schema also covers the other decimal/number-lane catalog sites sharing the same 'Divisor must be non-zero' condition and the same discharge contract: DecimalModuloDecimal, NumberDivideNumber, NumberModuloNumber.

**What this cell derives from**

- docs/language/precept-language-spec.md:1350 — spec: Expression scope table — default value expression scope is fields declared before this field, no forward reference
- docs/language/precept-language-spec.md:1354 — spec: a forward reference from a default value expression is impossible, not merely disallowed, because construction materializes defaults in declaration order
- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault-prevention obligation family
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: premise class (a) — field modifiers

## g05/field-modifier-integer — Field constraint-modifier bound divides — integer lane (IntegerDivideInteger)

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § The cell — matrix: item 1 — fault-family obligation schema is the catalog-declared safety precondition at the evaluation site
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — an explicit input — matrix: fault minting is catalog-stamped at evaluation sites

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | IntegerDivideInteger |
| evaluation site category | field-modifier-value-expression |
| type family | primitive |

### What must be proven

Obligation: Reserved != 0

Weakest precondition: Reserved != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Reserved | the divisor (right) operand of the IntegerDivideInteger expression at this evaluation site, per ProofEngine.ResolveParamInBinaryOp convention |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a).

Unlike field-default-value-expression, this category's scope is NOT narrowed by declaration order: a constraint modifier's value expression is rule shorthand, checked against the complete working copy after all mutations, so any field may be referenced regardless of where it is declared. The only available premise source is still class (a) — field modifiers — because there is no guard, no event arg, and no pre-state induction available at a declaration-position bound (no write plan exists here to found an inductive hypothesis on).

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: Budget and Reserved are fields; PerUnit's `max` bound divides Budget by Reserved. If Reserved carries a declared modifier combination whose exact interval excludes zero (e.g. `positive`, `nonzero`, or a `min`/`max` pair bounded away from zero), that field-modifier fact (premise class (a)) discharges the obligation.
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Resolve the divisor sub-expression to a single referenced field (reject the derivation if it is a compound expression with no single resolvable field — see the sound-but-unprovable band note below). Extract that field's exact interval by combining its declared numeric modifiers (`positive`/`nonnegative`/`nonzero`/`min`/`max`) under the same exact-decimal/exact-integer interval combination Family 1's class-(b) argument uses. Zero inside the combined interval, or no modifier excluding it, fails the derivation.

- docs/language/precept-language-spec.md:1352 — spec: Expression scope table — modifier value expressions have full scope: any field, regardless of declaration order, since a constraint modifier is rule shorthand
- docs/language/precept-language-spec.md:1354 — spec: a rule condition (and a constraint modifier, which is rule shorthand) is checked against the complete working copy after all mutations, so declaration order is irrelevant
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge: the precedent this contract generalizes from class (b) to class (a)

### What the failing diagnostic must suggest

- For class (a): declare the divisor field <WP> positive or nonzero
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept FmIntBase
field Budget as integer default 1000
field Reserved as integer default 0
field PerUnit as integer default 0 max Budget / Reserved
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*field-modifier-exclusion* — other

- Addition: replace `field Reserved as integer default 0` with `field Reserved as integer default 1 positive`
- Premise classes: (a)
- Derivation: Reserved carries a nonzero-excluding modifier -> field-modifier interval exclusion (class (a), generalized Arg-bound interval arithmetic)
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (measured at HEAD (commit e1a14d91, 2026-07-21, scratch snippet `m.precept` and this cell's own live run): the site mints no fault obligation, and additionally the WP calculator reports `[skipped obligation] <field>:max: bound is not a literal declared value (cross-field or unsupported bound source); not representable as a constant-bound rule` — a second, entangled gap distinct from the mint gap: the calculator's own bound-representation currently requires a literal, so it cannot even attempt this obligation's premise regardless of what discharges it.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` block is given: the schema's `application` discriminated union offers only two loci (`row-guard`, appending a guard to a transition row; `event-arg-declarations`, replacing named event-arg declarations) and neither fits — this discharge edits the divisor field's own declaration, a locus the DU does not yet name. Recorded as a schema gap rather than forced into either existing locus.
- `dischargeWitness.kind` is recorded as `other`, not `arg-modifier`: the enum's `arg-modifier` value is precedented (Witness Family 1) for an *event-arg* modifier addition; this discharge adds a modifier to a *field* declaration instead, and no enum member names that shape. Recorded as a second schema gap alongside the `application` locus gap.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| field-modifier-exclusion | replace `field Reserved as integer default 0` with `field Reserved as integer default 0 nonnegative` | `nonnegative` admits zero — the combined interval still contains 0, so the derivation still fails and the obligation still rejects, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- No false-proof hazard: this evaluation-site category carries only premise class (a) (field modifiers) — never class (d) — so no discharge here can run through a business `rule` used as a premise (docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3, Defect B does not apply). Clean-compile confirmation of the field-modifier discharge would be admissible in principle; it is moot here because the site mints nothing at HEAD regardless (see builtStatusNote).
- This derivation cites "Arg-bound interval arithmetic", the closest written validity argument, but the matrix's own text for that argument (obligation-discharge-matrix-2026-07-19.md, the argument's paragraph) is scoped explicitly to premise class (b) (Witness Family 1, Base A) — declared *argument* bounds summed against a rule's bound. Applying the same interval-extraction technique to a premise class (a) *field* modifier discharging a divisor-exclusion check (rather than a sum-bound check) is an unstated generalization: the mechanism (exact-bound extraction, interval combination) is identical, but the matrix has never written this specific application out. Flagged as a missing rule, not silently assumed identical.
- `[skipped obligation]` message observed for this category specifically (see field-modifier scope note above); the event-arg-modifier and collection-inner-type-modifier categories in this same group do NOT print this message for the structurally identical non-literal bound — recorded as a distinct, diagnostic-completeness gap (not a modeling gap): the same underlying representational limitation surfaces inconsistently across evaluation-site categories.
- wp.canonicalKey / canonicalKeySource omitted: the WP calculator (tools/Precept.MatrixTools) computes establishment/preservation WPs through a handler write plan; this obligation sits at a declaration-position evaluation site with no write plan, and the calculator has no computation path for it. Not measured, not inferred.
- Live-verified at HEAD (commit e1a14d91, Precept.MatrixTools, 2026-07-21): base, discharge, and near-miss all compile with zero fault-related diagnostics — the site mints nothing regardless of premises, so the run cannot distinguish base from discharge from near-miss on the fault obligation. The `expected: reject` / `expected: accept` blocks above state the model's committed power, not what the run showed; provenance is marked live-verified because the run itself is exactly what is reported, including its failure to confirm the model's expectation.
- This cell's schema also covers the other integer-lane catalog sites sharing the same 'Divisor must be non-zero' condition and the same discharge contract: IntegerModuloInteger, IntegerDivideDecimal, IntegerDivideNumber, IntegerModuloDecimal, IntegerModuloNumber.

**What this cell derives from**

- docs/language/precept-language-spec.md:1352 — spec: Expression scope table — modifier value expressions have full scope: any field, regardless of declaration order, since a constraint modifier is rule shorthand
- docs/language/precept-language-spec.md:1354 — spec: a rule condition (and a constraint modifier, which is rule shorthand) is checked against the complete working copy after all mutations, so declaration order is irrelevant
- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault-prevention obligation family
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: premise class (a) — field modifiers

## g05/field-modifier-decimal — Field constraint-modifier bound divides — decimal lane (DecimalDivideDecimal)

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § The cell — matrix: item 1 — fault-family obligation schema is the catalog-declared safety precondition at the evaluation site
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — an explicit input — matrix: fault minting is catalog-stamped at evaluation sites

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | DecimalDivideDecimal |
| evaluation site category | field-modifier-value-expression |
| type family | primitive |

### What must be proven

Obligation: Reserved != 0

Weakest precondition: Reserved != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Reserved | the divisor (right) operand of the DecimalDivideDecimal expression at this evaluation site, per ProofEngine.ResolveParamInBinaryOp convention |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a).

Unlike field-default-value-expression, this category's scope is NOT narrowed by declaration order: a constraint modifier's value expression is rule shorthand, checked against the complete working copy after all mutations, so any field may be referenced regardless of where it is declared. The only available premise source is still class (a) — field modifiers — because there is no guard, no event arg, and no pre-state induction available at a declaration-position bound (no write plan exists here to found an inductive hypothesis on).

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: Budget and Reserved are fields; PerUnit's `max` bound divides Budget by Reserved. If Reserved carries a declared modifier combination whose exact interval excludes zero (e.g. `positive`, `nonzero`, or a `min`/`max` pair bounded away from zero), that field-modifier fact (premise class (a)) discharges the obligation.
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Resolve the divisor sub-expression to a single referenced field (reject the derivation if it is a compound expression with no single resolvable field — see the sound-but-unprovable band note below). Extract that field's exact interval by combining its declared numeric modifiers (`positive`/`nonnegative`/`nonzero`/`min`/`max`) under the same exact-decimal/exact-integer interval combination Family 1's class-(b) argument uses. Zero inside the combined interval, or no modifier excluding it, fails the derivation.

- docs/language/precept-language-spec.md:1352 — spec: Expression scope table — modifier value expressions have full scope: any field, regardless of declaration order, since a constraint modifier is rule shorthand
- docs/language/precept-language-spec.md:1354 — spec: a rule condition (and a constraint modifier, which is rule shorthand) is checked against the complete working copy after all mutations, so declaration order is irrelevant
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge: the precedent this contract generalizes from class (b) to class (a)

### What the failing diagnostic must suggest

- For class (a): declare the divisor field <WP> positive or nonzero
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept FmDecBase
field Budget as decimal default 1000.0
field Reserved as decimal default 0.0
field PerUnit as decimal default 0.0 max Budget / Reserved
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*field-modifier-exclusion* — other

- Addition: replace `field Reserved as decimal default 0.0` with `field Reserved as decimal default 1.0 positive`
- Premise classes: (a)
- Derivation: Reserved carries a nonzero-excluding modifier -> field-modifier interval exclusion (class (a), generalized Arg-bound interval arithmetic)
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (measured at HEAD (commit e1a14d91, 2026-07-21, scratch snippet `m.precept` and this cell's own live run): the site mints no fault obligation, and additionally the WP calculator reports `[skipped obligation] <field>:max: bound is not a literal declared value (cross-field or unsupported bound source); not representable as a constant-bound rule` — a second, entangled gap distinct from the mint gap: the calculator's own bound-representation currently requires a literal, so it cannot even attempt this obligation's premise regardless of what discharges it.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` block is given: the schema's `application` discriminated union offers only two loci (`row-guard`, appending a guard to a transition row; `event-arg-declarations`, replacing named event-arg declarations) and neither fits — this discharge edits the divisor field's own declaration, a locus the DU does not yet name. Recorded as a schema gap rather than forced into either existing locus.
- `dischargeWitness.kind` is recorded as `other`, not `arg-modifier`: the enum's `arg-modifier` value is precedented (Witness Family 1) for an *event-arg* modifier addition; this discharge adds a modifier to a *field* declaration instead, and no enum member names that shape. Recorded as a second schema gap alongside the `application` locus gap.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| field-modifier-exclusion | replace `field Reserved as decimal default 0.0` with `field Reserved as decimal default 0.0 nonnegative` | `nonnegative` admits zero — the combined interval still contains 0, so the derivation still fails and the obligation still rejects, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- No false-proof hazard: this evaluation-site category carries only premise class (a) (field modifiers) — never class (d) — so no discharge here can run through a business `rule` used as a premise (docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3, Defect B does not apply). Clean-compile confirmation of the field-modifier discharge would be admissible in principle; it is moot here because the site mints nothing at HEAD regardless (see builtStatusNote).
- This derivation cites "Arg-bound interval arithmetic", the closest written validity argument, but the matrix's own text for that argument (obligation-discharge-matrix-2026-07-19.md, the argument's paragraph) is scoped explicitly to premise class (b) (Witness Family 1, Base A) — declared *argument* bounds summed against a rule's bound. Applying the same interval-extraction technique to a premise class (a) *field* modifier discharging a divisor-exclusion check (rather than a sum-bound check) is an unstated generalization: the mechanism (exact-bound extraction, interval combination) is identical, but the matrix has never written this specific application out. Flagged as a missing rule, not silently assumed identical.
- wp.canonicalKey / canonicalKeySource omitted: the WP calculator (tools/Precept.MatrixTools) computes establishment/preservation WPs through a handler write plan; this obligation sits at a declaration-position evaluation site with no write plan, and the calculator has no computation path for it. Not measured, not inferred.
- Live-verified at HEAD (commit e1a14d91, Precept.MatrixTools, 2026-07-21): base, discharge, and near-miss all compile with zero fault-related diagnostics — the site mints nothing regardless of premises, so the run cannot distinguish base from discharge from near-miss on the fault obligation. The `expected: reject` / `expected: accept` blocks above state the model's committed power, not what the run showed; provenance is marked live-verified because the run itself is exactly what is reported, including its failure to confirm the model's expectation.
- This cell's schema also covers the other decimal/number-lane catalog sites sharing the same 'Divisor must be non-zero' condition and the same discharge contract: DecimalModuloDecimal, NumberDivideNumber, NumberModuloNumber.

**What this cell derives from**

- docs/language/precept-language-spec.md:1352 — spec: Expression scope table — modifier value expressions have full scope: any field, regardless of declaration order, since a constraint modifier is rule shorthand
- docs/language/precept-language-spec.md:1354 — spec: a rule condition (and a constraint modifier, which is rule shorthand) is checked against the complete working copy after all mutations, so declaration order is irrelevant
- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault-prevention obligation family
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: premise class (a) — field modifiers

## g05/event-arg-modifier-integer — Event-arg constraint-modifier bound divides — integer lane (IntegerDivideInteger)

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § The cell — matrix: item 1 — fault-family obligation schema is the catalog-declared safety precondition at the evaluation site
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — an explicit input — matrix: fault minting is catalog-stamped at evaluation sites

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | IntegerDivideInteger |
| evaluation site category | event-arg-modifier-value-expression |
| type family | primitive |

### What must be proven

Obligation: Reserved != 0

Weakest precondition: Reserved != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Reserved | the divisor (right) operand of the IntegerDivideInteger expression at this evaluation site, per ProofEngine.ResolveParamInBinaryOp convention |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a).

The bound belongs to an event arg's declaration, but the *divisor* it evaluates is still a reference to a FIELD (Reserved), so the fact source is still premise class (a) — field modifiers on Reserved — not an arg constraint on the arg being bounded. No guard is in scope at an event-arg declaration, and the arg itself is the subject of the bound, not a premise for it.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: Budget and Reserved are fields; event Approve's arg Cap carries a `max` bound dividing Budget by Reserved. If Reserved carries a declared modifier combination whose exact interval excludes zero (e.g. `positive`, `nonzero`, or a `min`/`max` pair bounded away from zero), that field-modifier fact (premise class (a)) discharges the obligation.
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Resolve the divisor sub-expression to a single referenced field (reject the derivation if it is a compound expression with no single resolvable field — see the sound-but-unprovable band note below). Extract that field's exact interval by combining its declared numeric modifiers (`positive`/`nonnegative`/`nonzero`/`min`/`max`) under the same exact-decimal/exact-integer interval combination Family 1's class-(b) argument uses. Zero inside the combined interval, or no modifier excluding it, fails the derivation.

- docs/language/precept-language-spec.md:927 — spec: ArgDecl := Identifier as TypeRef FieldModifier* — an event arg's modifier value expression uses the same FieldModifier production as a field's
- docs/language/precept-language-spec.md § Field Modifiers — spec: field modifiers appear after the type reference and before any computed expression, on both field and arg declarations
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge: the precedent this contract generalizes from class (b) to class (a)

### What the failing diagnostic must suggest

- For class (a): declare the divisor field <WP> positive or nonzero
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept EaIntBase
field Budget as integer default 1000
field Reserved as integer default 0
event Approve(Cap as integer max Budget / Reserved)
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*field-modifier-exclusion* — other

- Addition: replace `field Reserved as integer default 0` with `field Reserved as integer default 1 positive`
- Premise classes: (a)
- Derivation: Reserved carries a nonzero-excluding modifier -> field-modifier interval exclusion (class (a), generalized Arg-bound interval arithmetic)
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (measured at HEAD (commit e1a14d91, 2026-07-21, scratch snippet `ag.precept` and this cell's own live run): the site mints no fault obligation. Unlike field-modifier-value-expression, no `[skipped obligation]` message is printed here either — the position is silently passed over, not even reported as skipped.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` block is given: the schema's `application` discriminated union offers only two loci (`row-guard`, appending a guard to a transition row; `event-arg-declarations`, replacing named event-arg declarations) and neither fits — this discharge edits the divisor field's own declaration, a locus the DU does not yet name. Recorded as a schema gap rather than forced into either existing locus.
- `dischargeWitness.kind` is recorded as `other`, not `arg-modifier`: the enum's `arg-modifier` value is precedented (Witness Family 1) for an *event-arg* modifier addition; this discharge adds a modifier to a *field* declaration instead, and no enum member names that shape. Recorded as a second schema gap alongside the `application` locus gap.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| field-modifier-exclusion | replace `field Reserved as integer default 0` with `field Reserved as integer default 0 nonnegative` | `nonnegative` admits zero — the combined interval still contains 0, so the derivation still fails and the obligation still rejects, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- No false-proof hazard: this evaluation-site category carries only premise class (a) (field modifiers) — never class (d) — so no discharge here can run through a business `rule` used as a premise (docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3, Defect B does not apply). Clean-compile confirmation of the field-modifier discharge would be admissible in principle; it is moot here because the site mints nothing at HEAD regardless (see builtStatusNote).
- This derivation cites "Arg-bound interval arithmetic", the closest written validity argument, but the matrix's own text for that argument (obligation-discharge-matrix-2026-07-19.md, the argument's paragraph) is scoped explicitly to premise class (b) (Witness Family 1, Base A) — declared *argument* bounds summed against a rule's bound. Applying the same interval-extraction technique to a premise class (a) *field* modifier discharging a divisor-exclusion check (rather than a sum-bound check) is an unstated generalization: the mechanism (exact-bound extraction, interval combination) is identical, but the matrix has never written this specific application out. Flagged as a missing rule, not silently assumed identical.
- wp.canonicalKey / canonicalKeySource omitted: the WP calculator (tools/Precept.MatrixTools) computes establishment/preservation WPs through a handler write plan; this obligation sits at a declaration-position evaluation site with no write plan, and the calculator has no computation path for it. Not measured, not inferred.
- Live-verified at HEAD (commit e1a14d91, Precept.MatrixTools, 2026-07-21): base, discharge, and near-miss all compile with zero fault-related diagnostics — the site mints nothing regardless of premises, so the run cannot distinguish base from discharge from near-miss on the fault obligation. The `expected: reject` / `expected: accept` blocks above state the model's committed power, not what the run showed; provenance is marked live-verified because the run itself is exactly what is reported, including its failure to confirm the model's expectation.
- This cell's schema also covers the other integer-lane catalog sites sharing the same 'Divisor must be non-zero' condition and the same discharge contract: IntegerModuloInteger, IntegerDivideDecimal, IntegerDivideNumber, IntegerModuloDecimal, IntegerModuloNumber.

**What this cell derives from**

- docs/language/precept-language-spec.md:927 — spec: ArgDecl := Identifier as TypeRef FieldModifier* — an event arg's modifier value expression uses the same FieldModifier production as a field's
- docs/language/precept-language-spec.md § Field Modifiers — spec: field modifiers appear after the type reference and before any computed expression, on both field and arg declarations
- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault-prevention obligation family
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: premise class (a) — field modifiers

## g05/event-arg-modifier-decimal — Event-arg constraint-modifier bound divides — decimal lane (DecimalDivideDecimal)

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § The cell — matrix: item 1 — fault-family obligation schema is the catalog-declared safety precondition at the evaluation site
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — an explicit input — matrix: fault minting is catalog-stamped at evaluation sites

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | DecimalDivideDecimal |
| evaluation site category | event-arg-modifier-value-expression |
| type family | primitive |

### What must be proven

Obligation: Reserved != 0

Weakest precondition: Reserved != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Reserved | the divisor (right) operand of the DecimalDivideDecimal expression at this evaluation site, per ProofEngine.ResolveParamInBinaryOp convention |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a).

The bound belongs to an event arg's declaration, but the *divisor* it evaluates is still a reference to a FIELD (Reserved), so the fact source is still premise class (a) — field modifiers on Reserved — not an arg constraint on the arg being bounded. No guard is in scope at an event-arg declaration, and the arg itself is the subject of the bound, not a premise for it.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: Budget and Reserved are fields; event Approve's arg Cap carries a `max` bound dividing Budget by Reserved. If Reserved carries a declared modifier combination whose exact interval excludes zero (e.g. `positive`, `nonzero`, or a `min`/`max` pair bounded away from zero), that field-modifier fact (premise class (a)) discharges the obligation.
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Resolve the divisor sub-expression to a single referenced field (reject the derivation if it is a compound expression with no single resolvable field — see the sound-but-unprovable band note below). Extract that field's exact interval by combining its declared numeric modifiers (`positive`/`nonnegative`/`nonzero`/`min`/`max`) under the same exact-decimal/exact-integer interval combination Family 1's class-(b) argument uses. Zero inside the combined interval, or no modifier excluding it, fails the derivation.

- docs/language/precept-language-spec.md:927 — spec: ArgDecl := Identifier as TypeRef FieldModifier* — an event arg's modifier value expression uses the same FieldModifier production as a field's
- docs/language/precept-language-spec.md § Field Modifiers — spec: field modifiers appear after the type reference and before any computed expression, on both field and arg declarations
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge: the precedent this contract generalizes from class (b) to class (a)

### What the failing diagnostic must suggest

- For class (a): declare the divisor field <WP> positive or nonzero
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept EaDecBase
field Budget as decimal default 1000.0
field Reserved as decimal default 0.0
event Approve(Cap as decimal max Budget / Reserved)
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*field-modifier-exclusion* — other

- Addition: replace `field Reserved as decimal default 0.0` with `field Reserved as decimal default 1.0 positive`
- Premise classes: (a)
- Derivation: Reserved carries a nonzero-excluding modifier -> field-modifier interval exclusion (class (a), generalized Arg-bound interval arithmetic)
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (measured at HEAD (commit e1a14d91, 2026-07-21, scratch snippet `ag.precept` and this cell's own live run): the site mints no fault obligation. Unlike field-modifier-value-expression, no `[skipped obligation]` message is printed here either — the position is silently passed over, not even reported as skipped.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` block is given: the schema's `application` discriminated union offers only two loci (`row-guard`, appending a guard to a transition row; `event-arg-declarations`, replacing named event-arg declarations) and neither fits — this discharge edits the divisor field's own declaration, a locus the DU does not yet name. Recorded as a schema gap rather than forced into either existing locus.
- `dischargeWitness.kind` is recorded as `other`, not `arg-modifier`: the enum's `arg-modifier` value is precedented (Witness Family 1) for an *event-arg* modifier addition; this discharge adds a modifier to a *field* declaration instead, and no enum member names that shape. Recorded as a second schema gap alongside the `application` locus gap.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| field-modifier-exclusion | replace `field Reserved as decimal default 0.0` with `field Reserved as decimal default 0.0 nonnegative` | `nonnegative` admits zero — the combined interval still contains 0, so the derivation still fails and the obligation still rejects, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- No false-proof hazard: this evaluation-site category carries only premise class (a) (field modifiers) — never class (d) — so no discharge here can run through a business `rule` used as a premise (docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3, Defect B does not apply). Clean-compile confirmation of the field-modifier discharge would be admissible in principle; it is moot here because the site mints nothing at HEAD regardless (see builtStatusNote).
- This derivation cites "Arg-bound interval arithmetic", the closest written validity argument, but the matrix's own text for that argument (obligation-discharge-matrix-2026-07-19.md, the argument's paragraph) is scoped explicitly to premise class (b) (Witness Family 1, Base A) — declared *argument* bounds summed against a rule's bound. Applying the same interval-extraction technique to a premise class (a) *field* modifier discharging a divisor-exclusion check (rather than a sum-bound check) is an unstated generalization: the mechanism (exact-bound extraction, interval combination) is identical, but the matrix has never written this specific application out. Flagged as a missing rule, not silently assumed identical.
- wp.canonicalKey / canonicalKeySource omitted: the WP calculator (tools/Precept.MatrixTools) computes establishment/preservation WPs through a handler write plan; this obligation sits at a declaration-position evaluation site with no write plan, and the calculator has no computation path for it. Not measured, not inferred.
- Live-verified at HEAD (commit e1a14d91, Precept.MatrixTools, 2026-07-21): base, discharge, and near-miss all compile with zero fault-related diagnostics — the site mints nothing regardless of premises, so the run cannot distinguish base from discharge from near-miss on the fault obligation. The `expected: reject` / `expected: accept` blocks above state the model's committed power, not what the run showed; provenance is marked live-verified because the run itself is exactly what is reported, including its failure to confirm the model's expectation.
- This cell's schema also covers the other decimal/number-lane catalog sites sharing the same 'Divisor must be non-zero' condition and the same discharge contract: DecimalModuloDecimal, NumberDivideNumber, NumberModuloNumber.

**What this cell derives from**

- docs/language/precept-language-spec.md:927 — spec: ArgDecl := Identifier as TypeRef FieldModifier* — an event arg's modifier value expression uses the same FieldModifier production as a field's
- docs/language/precept-language-spec.md § Field Modifiers — spec: field modifiers appear after the type reference and before any computed expression, on both field and arg declarations
- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault-prevention obligation family
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: premise class (a) — field modifiers

## g05/collection-inner-type-modifier-integer — Collection inner-type modifier bound divides — integer lane (IntegerDivideInteger)

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § The cell — matrix: item 1 — fault-family obligation schema is the catalog-declared safety precondition at the evaluation site
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — an explicit input — matrix: fault minting is catalog-stamped at evaluation sites

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | IntegerDivideInteger |
| evaluation site category | collection-inner-type-modifier-value-expression |
| type family | primitive |

### What must be proven

Obligation: Reserved != 0

Weakest precondition: Reserved != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Reserved | the divisor (right) operand of the IntegerDivideInteger expression at this evaluation site, per ProofEngine.ResolveParamInBinaryOp convention |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a).

The bound is a `max` value modifier in a collection's inner-type position (the per-element analogue of a scalar field bound, desugaring to a per-element rule — spec:1123). The divisor it evaluates is still a field reference (Reserved), so premise class (a) — field modifiers on Reserved — is the sole available fact source, exactly as for field-modifier-value-expression; no guard, no arg, no pre-state is in scope.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: Budget and Reserved are fields; Queue's inner-type `max` bound divides Budget by Reserved. If Reserved carries a declared modifier combination whose exact interval excludes zero (e.g. `positive`, `nonzero`, or a `min`/`max` pair bounded away from zero), that field-modifier fact (premise class (a)) discharges the obligation.
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Resolve the divisor sub-expression to a single referenced field (reject the derivation if it is a compound expression with no single resolvable field — see the sound-but-unprovable band note below). Extract that field's exact interval by combining its declared numeric modifiers (`positive`/`nonnegative`/`nonzero`/`min`/`max`) under the same exact-decimal/exact-integer interval combination Family 1's class-(b) argument uses. Zero inside the combined interval, or no modifier excluding it, fails the derivation.

- docs/language/precept-language-spec.md:1123 — spec: value modifiers in inner-type position desugar to a per-element rule and participate in proof identically to the same modifier on a scalar field
- docs/language/precept-language-spec.md:1108 — spec: CollectionInnerType := ScalarType TypeQualifier? ValueModifier* — the inner-type ValueModifier production admits min/max identically to the scalar field production
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge: the precedent this contract generalizes from class (b) to class (a)

### What the failing diagnostic must suggest

- For class (a): declare the divisor field <WP> positive or nonzero
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept CiIntBase
field Budget as integer default 1000
field Reserved as integer default 0
field Queue as queue of integer max Budget / Reserved
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*field-modifier-exclusion* — other

- Addition: replace `field Reserved as integer default 0` with `field Reserved as integer default 1 positive`
- Premise classes: (a)
- Derivation: Reserved carries a nonzero-excluding modifier -> field-modifier interval exclusion (class (a), generalized Arg-bound interval arithmetic)
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (measured at HEAD (commit e1a14d91, 2026-07-21, this cell's own live run — not previously measured by the prior probing pass): the site mints no fault obligation. As with event-arg-modifier-value-expression, and unlike field-modifier-value-expression, no `[skipped obligation]` message is printed — the position is silently passed over.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` block is given: the schema's `application` discriminated union offers only two loci (`row-guard`, appending a guard to a transition row; `event-arg-declarations`, replacing named event-arg declarations) and neither fits — this discharge edits the divisor field's own declaration, a locus the DU does not yet name. Recorded as a schema gap rather than forced into either existing locus.
- `dischargeWitness.kind` is recorded as `other`, not `arg-modifier`: the enum's `arg-modifier` value is precedented (Witness Family 1) for an *event-arg* modifier addition; this discharge adds a modifier to a *field* declaration instead, and no enum member names that shape. Recorded as a second schema gap alongside the `application` locus gap.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| field-modifier-exclusion | replace `field Reserved as integer default 0` with `field Reserved as integer default 0 nonnegative` | `nonnegative` admits zero — the combined interval still contains 0, so the derivation still fails and the obligation still rejects, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- No false-proof hazard: this evaluation-site category carries only premise class (a) (field modifiers) — never class (d) — so no discharge here can run through a business `rule` used as a premise (docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3, Defect B does not apply). Clean-compile confirmation of the field-modifier discharge would be admissible in principle; it is moot here because the site mints nothing at HEAD regardless (see builtStatusNote).
- This derivation cites "Arg-bound interval arithmetic", the closest written validity argument, but the matrix's own text for that argument (obligation-discharge-matrix-2026-07-19.md, the argument's paragraph) is scoped explicitly to premise class (b) (Witness Family 1, Base A) — declared *argument* bounds summed against a rule's bound. Applying the same interval-extraction technique to a premise class (a) *field* modifier discharging a divisor-exclusion check (rather than a sum-bound check) is an unstated generalization: the mechanism (exact-bound extraction, interval combination) is identical, but the matrix has never written this specific application out. Flagged as a missing rule, not silently assumed identical.
- wp.canonicalKey / canonicalKeySource omitted: the WP calculator (tools/Precept.MatrixTools) computes establishment/preservation WPs through a handler write plan; this obligation sits at a declaration-position evaluation site with no write plan, and the calculator has no computation path for it. Not measured, not inferred.
- Live-verified at HEAD (commit e1a14d91, Precept.MatrixTools, 2026-07-21): base, discharge, and near-miss all compile with zero fault-related diagnostics — the site mints nothing regardless of premises, so the run cannot distinguish base from discharge from near-miss on the fault obligation. The `expected: reject` / `expected: accept` blocks above state the model's committed power, not what the run showed; provenance is marked live-verified because the run itself is exactly what is reported, including its failure to confirm the model's expectation.
- This cell's schema also covers the other integer-lane catalog sites sharing the same 'Divisor must be non-zero' condition and the same discharge contract: IntegerModuloInteger, IntegerDivideDecimal, IntegerDivideNumber, IntegerModuloDecimal, IntegerModuloNumber.

**What this cell derives from**

- docs/language/precept-language-spec.md:1123 — spec: value modifiers in inner-type position desugar to a per-element rule and participate in proof identically to the same modifier on a scalar field
- docs/language/precept-language-spec.md:1108 — spec: CollectionInnerType := ScalarType TypeQualifier? ValueModifier* — the inner-type ValueModifier production admits min/max identically to the scalar field production
- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault-prevention obligation family
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: premise class (a) — field modifiers

## g05/collection-inner-type-modifier-decimal — Collection inner-type modifier bound divides — decimal lane (DecimalDivideDecimal)

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § The cell — matrix: item 1 — fault-family obligation schema is the catalog-declared safety precondition at the evaluation site
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — an explicit input — matrix: fault minting is catalog-stamped at evaluation sites

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | DecimalDivideDecimal |
| evaluation site category | collection-inner-type-modifier-value-expression |
| type family | primitive |

### What must be proven

Obligation: Reserved != 0

Weakest precondition: Reserved != 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Reserved | the divisor (right) operand of the DecimalDivideDecimal expression at this evaluation site, per ProofEngine.ResolveParamInBinaryOp convention |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a).

The bound is a `max` value modifier in a collection's inner-type position (the per-element analogue of a scalar field bound, desugaring to a per-element rule — spec:1123). The divisor it evaluates is still a field reference (Reserved), so premise class (a) — field modifiers on Reserved — is the sole available fact source, exactly as for field-modifier-value-expression; no guard, no arg, no pre-state is in scope.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: Budget and Reserved are fields; Queue's inner-type `max` bound divides Budget by Reserved. If Reserved carries a declared modifier combination whose exact interval excludes zero (e.g. `positive`, `nonzero`, or a `min`/`max` pair bounded away from zero), that field-modifier fact (premise class (a)) discharges the obligation.
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Resolve the divisor sub-expression to a single referenced field (reject the derivation if it is a compound expression with no single resolvable field — see the sound-but-unprovable band note below). Extract that field's exact interval by combining its declared numeric modifiers (`positive`/`nonnegative`/`nonzero`/`min`/`max`) under the same exact-decimal/exact-integer interval combination Family 1's class-(b) argument uses. Zero inside the combined interval, or no modifier excluding it, fails the derivation.

- docs/language/precept-language-spec.md:1123 — spec: value modifiers in inner-type position desugar to a per-element rule and participate in proof identically to the same modifier on a scalar field
- docs/language/precept-language-spec.md:1108 — spec: CollectionInnerType := ScalarType TypeQualifier? ValueModifier* — the inner-type ValueModifier production admits min/max identically to the scalar field production
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Witnesses — matrix: Family 4 — fault family, arg-constraint discharge: the precedent this contract generalizes from class (b) to class (a)

### What the failing diagnostic must suggest

- For class (a): declare the divisor field <WP> positive or nonzero
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept CiDecBase
field Budget as decimal default 1000.0
field Reserved as decimal default 0.0
field Queue as queue of decimal max Budget / Reserved
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*field-modifier-exclusion* — other

- Addition: replace `field Reserved as decimal default 0.0` with `field Reserved as decimal default 1.0 positive`
- Premise classes: (a)
- Derivation: Reserved carries a nonzero-excluding modifier -> field-modifier interval exclusion (class (a), generalized Arg-bound interval arithmetic)
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (measured at HEAD (commit e1a14d91, 2026-07-21, this cell's own live run — not previously measured by the prior probing pass): the site mints no fault obligation. As with event-arg-modifier-value-expression, and unlike field-modifier-value-expression, no `[skipped obligation]` message is printed — the position is silently passed over.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` block is given: the schema's `application` discriminated union offers only two loci (`row-guard`, appending a guard to a transition row; `event-arg-declarations`, replacing named event-arg declarations) and neither fits — this discharge edits the divisor field's own declaration, a locus the DU does not yet name. Recorded as a schema gap rather than forced into either existing locus.
- `dischargeWitness.kind` is recorded as `other`, not `arg-modifier`: the enum's `arg-modifier` value is precedented (Witness Family 1) for an *event-arg* modifier addition; this discharge adds a modifier to a *field* declaration instead, and no enum member names that shape. Recorded as a second schema gap alongside the `application` locus gap.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| field-modifier-exclusion | replace `field Reserved as decimal default 0.0` with `field Reserved as decimal default 0.0 nonnegative` | `nonnegative` admits zero — the combined interval still contains 0, so the derivation still fails and the obligation still rejects, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- No false-proof hazard: this evaluation-site category carries only premise class (a) (field modifiers) — never class (d) — so no discharge here can run through a business `rule` used as a premise (docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md Part 3, Defect B does not apply). Clean-compile confirmation of the field-modifier discharge would be admissible in principle; it is moot here because the site mints nothing at HEAD regardless (see builtStatusNote).
- This derivation cites "Arg-bound interval arithmetic", the closest written validity argument, but the matrix's own text for that argument (obligation-discharge-matrix-2026-07-19.md, the argument's paragraph) is scoped explicitly to premise class (b) (Witness Family 1, Base A) — declared *argument* bounds summed against a rule's bound. Applying the same interval-extraction technique to a premise class (a) *field* modifier discharging a divisor-exclusion check (rather than a sum-bound check) is an unstated generalization: the mechanism (exact-bound extraction, interval combination) is identical, but the matrix has never written this specific application out. Flagged as a missing rule, not silently assumed identical.
- wp.canonicalKey / canonicalKeySource omitted: the WP calculator (tools/Precept.MatrixTools) computes establishment/preservation WPs through a handler write plan; this obligation sits at a declaration-position evaluation site with no write plan, and the calculator has no computation path for it. Not measured, not inferred.
- Live-verified at HEAD (commit e1a14d91, Precept.MatrixTools, 2026-07-21): base, discharge, and near-miss all compile with zero fault-related diagnostics — the site mints nothing regardless of premises, so the run cannot distinguish base from discharge from near-miss on the fault obligation. The `expected: reject` / `expected: accept` blocks above state the model's committed power, not what the run showed; provenance is marked live-verified because the run itself is exactly what is reported, including its failure to confirm the model's expectation.
- This cell's schema also covers the other decimal/number-lane catalog sites sharing the same 'Divisor must be non-zero' condition and the same discharge contract: DecimalModuloDecimal, NumberDivideNumber, NumberModuloNumber.

**What this cell derives from**

- docs/language/precept-language-spec.md:1123 — spec: value modifiers in inner-type position desugar to a per-element rule and participate in proof identically to the same modifier on a scalar field
- docs/language/precept-language-spec.md:1108 — spec: CollectionInnerType := ScalarType TypeQualifier? ValueModifier* — the inner-type ValueModifier production admits min/max identically to the scalar field production
- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault-prevention obligation family
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: premise class (a) — field modifiers

## g05/type-qualifier-integer — Type qualifier expression divides — integer lane (IntegerDivideInteger) — unreachable

Disposition: **empty** — empty: a type-qualifier `Expr` (`in`/`of`/`to`) is a typed constant whose single interpolation hole resolves against the `WholeValue` slot, which admits only a hole of the field's own target type (money/quantity) — never a numeric hole. Re-verified by direct measurement (commit e1a14d91, 2026-07-21, Precept.MatrixTools): `field Amount as money in '{Base / Rate}'` with integer Base/Rate rejects with `[Error] InterpolatedTypedConstantHoleTypeMismatch: '{wholevalue}' expects a the target type value, but the expression is integer — use a compatible field or literal`, and `field Amt as quantity of '{Base / Rate}'` with decimal Base/Rate rejects with the identical message reading `decimal`. The two escape routes are closed as well: an unquoted qualifier expression (`money in Base / Rate`) rejects at the parser with `[Error] ExpectedToken: Expected typed constant here, but found 'Base'`, and a multi-hole qualifier form (`money in '{Base / Rate} USD'`) rejects with `[Error] InvalidInterpolatedTypedConstantForm: 'currency' doesn't match a recognized pattern for this type` — the qualifier position recognizes a single-slot `currency` form, so no Magnitude slot is reachable there. So a primitive numeric division/modulo expression can never occupy this evaluation-site category — the type-family-mismatch pruning derivation the matrix's other empty cells use, confirmed here by direct measurement rather than by catalog cross-reference. This corrects the disposition map's 'defined' label for this coordinate, which had recorded the position as unmeasured rather than unreachable.

**Disposition sources**

- docs/language/precept-language-spec.md § Type References — spec: type qualifiers narrow the value domain to a unit/currency (`in`) or dimension family (`of`); the qualifier's Expr resolves to that identity, not to the field's own scalar type
- src/Precept/Pipeline/TypeChecker.Expressions.TypedConstants.cs — code: IsSlotCompatible (line 295) and its caller at line 542: the `WholeValue` arm accepts a hole only when `holeType == targetType`, and `Magnitude` — the sole arm admitting integer/decimal/number — is not the slot a qualifier hole resolves to. The `InterpolationSlotKind` enum itself is declared in src/Precept/Pipeline/SemanticIndex.cs:199-209.

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | IntegerDivideInteger |
| evaluation site category | type-qualifier-expression |
| type family | primitive |

**What the sources leave unstated or ambiguous here**

- The disposition map (slice-3 planning artifact) classified this coordinate as `defined`, reflecting the prior probe's own uncertainty (eval-sites.json: 'no unsafe operation was placed inside a qualifier, so whether the position mints fault obligations at HEAD is unmeasured'). This cell's own live testing resolves that uncertainty: the coordinate is unreachable, not merely unmeasured. Recorded as a correction, per the ground rule against inventing a 'defined' answer the matrix's rules cannot actually derive.

**What this cell derives from**

- docs/language/precept-language-spec.md § Type References — spec: type qualifiers narrow the value domain to a unit/currency (`in`) or dimension family (`of`); the qualifier's Expr resolves to that identity, not to the field's own scalar type
- src/Precept/Pipeline/TypeChecker.Expressions.TypedConstants.cs — code: IsSlotCompatible (line 295) and its caller at line 542: the `WholeValue` arm accepts a hole only when `holeType == targetType`, and `Magnitude` — the sole arm admitting integer/decimal/number — is not the slot a qualifier hole resolves to. The `InterpolationSlotKind` enum itself is declared in src/Precept/Pipeline/SemanticIndex.cs:199-209.

## g05/type-qualifier-decimal — Type qualifier expression divides — decimal lane (DecimalDivideDecimal) — unreachable

Disposition: **empty** — empty: a type-qualifier `Expr` (`in`/`of`/`to`) is a typed constant whose single interpolation hole resolves against the `WholeValue` slot, which admits only a hole of the field's own target type (money/quantity) — never a numeric hole. Re-verified by direct measurement (commit e1a14d91, 2026-07-21, Precept.MatrixTools): `field Amount as money in '{Base / Rate}'` with integer Base/Rate rejects with `[Error] InterpolatedTypedConstantHoleTypeMismatch: '{wholevalue}' expects a the target type value, but the expression is integer — use a compatible field or literal`, and `field Amt as quantity of '{Base / Rate}'` with decimal Base/Rate rejects with the identical message reading `decimal`. The two escape routes are closed as well: an unquoted qualifier expression (`money in Base / Rate`) rejects at the parser with `[Error] ExpectedToken: Expected typed constant here, but found 'Base'`, and a multi-hole qualifier form (`money in '{Base / Rate} USD'`) rejects with `[Error] InvalidInterpolatedTypedConstantForm: 'currency' doesn't match a recognized pattern for this type` — the qualifier position recognizes a single-slot `currency` form, so no Magnitude slot is reachable there. So a primitive numeric division/modulo expression can never occupy this evaluation-site category — the type-family-mismatch pruning derivation the matrix's other empty cells use, confirmed here by direct measurement rather than by catalog cross-reference. This corrects the disposition map's 'defined' label for this coordinate, which had recorded the position as unmeasured rather than unreachable.

**Disposition sources**

- docs/language/precept-language-spec.md § Type References — spec: type qualifiers narrow the value domain to a unit/currency (`in`) or dimension family (`of`); the qualifier's Expr resolves to that identity, not to the field's own scalar type
- src/Precept/Pipeline/TypeChecker.Expressions.TypedConstants.cs — code: IsSlotCompatible (line 295) and its caller at line 542: the `WholeValue` arm accepts a hole only when `holeType == targetType`, and `Magnitude` — the sole arm admitting integer/decimal/number — is not the slot a qualifier hole resolves to. The `InterpolationSlotKind` enum itself is declared in src/Precept/Pipeline/SemanticIndex.cs:199-209.

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | DecimalDivideDecimal |
| evaluation site category | type-qualifier-expression |
| type family | primitive |

**What the sources leave unstated or ambiguous here**

- The disposition map (slice-3 planning artifact) classified this coordinate as `defined`, reflecting the prior probe's own uncertainty (eval-sites.json: 'no unsafe operation was placed inside a qualifier, so whether the position mints fault obligations at HEAD is unmeasured'). This cell's own live testing resolves that uncertainty: the coordinate is unreachable, not merely unmeasured. Recorded as a correction, per the ground rule against inventing a 'defined' answer the matrix's rules cannot actually derive.

**What this cell derives from**

- docs/language/precept-language-spec.md § Type References — spec: type qualifiers narrow the value domain to a unit/currency (`in`) or dimension family (`of`); the qualifier's Expr resolves to that identity, not to the field's own scalar type
- src/Precept/Pipeline/TypeChecker.Expressions.TypedConstants.cs — code: IsSlotCompatible (line 295) and its caller at line 542: the `WholeValue` arm accepts a hole only when `holeType == targetType`, and `Magnitude` — the sole arm admitting integer/decimal/number — is not the slot a qualifier hole resolves to. The `InterpolationSlotKind` enum itself is declared in src/Precept/Pipeline/SemanticIndex.cs:199-209.

