<!--
GENERATED FILE — do not hand-edit.
Source: fault-13-collection-non-empty-write-sites.cells.json
Regenerate: dotnet run --project tools/Precept.MatrixTools -- render-cells docs/Working/obligation-discharge-matrix-2026-07-19-cells
Hand edits will be overwritten. Edit the .cells.json and regenerate.
-->

# Fault family — reading from a collection that may be empty, at a write site (group 13)

Family id: fault-13-collection-non-empty-write-sites
Case shape: fault
Definition version: obligation-discharge-matrix-2026-07-19 rev 7 (2026-07-20, draft)

**Where this family's content comes from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § The cell — matrix: the fault-family cell definition and base-minimality clause
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: premise classes, discharge mechanisms, suggestion schema
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row: catalog-declared safety precondition, premise classes (b)/(c)/(a)
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses — matrix: the Family 4 paragraph — fault family, arg-constraint discharge; the shared decision-procedure precedent this group's (a)/(c) contracts follow
- docs/language/collection-types.md § Emptiness Safety — spec
- docs/language/collection-types.md § Access proof obligations — spec
- docs/language/collection-types.md § Guard pattern — spec
- docs/language/collection-types.md § Constraint Catalog — spec
- src/Precept/Language/Types.cs:183 — code: the seven collection kinds' accessor ProofRequirements begin here and span lines 183-336
- src/Precept/Language/Modifiers.cs:195 — code: Mincount ProofSatisfaction.Numeric; full declaration spans lines 195-208
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — owner-ruling, 2026-07-20: the false-proof hazard for rule-premise discharges

## Respellability — the family verdict

Can every safe program these rules reject be rewritten into a form they accept? **yes**

Evidence: model-derived
Corpus measurement: pending

**Notes on the family verdict**

- Every sound band member's business intent respells into a licensed mincount modifier or guard restating F.count > 0, mirroring Witness Family 1's respellability finding for the numeric case.
- One concrete band member found this session: `F.count >= 1` (semantically equivalent to F.count > 0 for an integer count) is not recognized by the guard-match decision procedure (live-verified) — its licensed respelling is F.count > 0 itself, or a mincount modifier.

**Sources for this verdict**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Ratification protocol — matrix: layer 4: corpus measurement is the hard gate

## Premise classes — the legend

Where a proof may get its facts (want :19, :143): (a) field modifiers, (b) arg constraints, (c) the handler's guard, (d) all rules holding in the pre-state (the inductive hypothesis), (e) evaluating the applicable rule set at an editable-field write — (e)'s status as a genuinely fifth class vs (a) generalized is an open owner item.

## g13/list-first — List .first — reading a possibly-empty collection at a write site

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | list accessor .first |
| evaluation site category | transition-row-action-operand |
| type family | collection |

### What must be proven

Obligation: F.count > 0

Weakest precondition: F.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| F | the collection field the accessor reads |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a), (c), (d).

The read projects the receiver's own .count; no event argument represents a collection's cardinality, so class (b) never applies to this obligation regardless of site. Class (a) applies wherever the field declares mincount. Class (c) applies at every evaluation-site category in this group's region, all three carrying a guard position. Class (d) applies only where a pre-state exists — transition-row-action-operand and state-hook-action-operand, not construction-row-action-operand.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: a statically-literal mincount >= 1 on the collection field establishes F.count >= 1, which entails F.count > 0
- Strategy: IntervalContainment
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Look up a mincount modifier on the collection field's declaration (a finite, statically-literal set). mincount >= 1 discharges the class; no mincount modifier, or mincount 0, fails it (live-verified, this group).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Arg-bound interval arithmetic — governance enforces every declared constraint on a value before computation derives from it; extended here from an arg-declared bound to a field-declared mincount bound, both realized as the same ProofSatisfaction.Numeric catalog shape (src/Precept/Language/Modifiers.cs:195, spanning 195-208)
- src/Precept/Language/Modifiers.cs:195 — code: ModifierKind.Mincount ProofSatisfaction.Numeric(Accessor(count), GreaterThanOrEqual, DeclarationValue); full declaration spans lines 195-208
- docs/language/collection-types.md § Constraint Catalog — spec: a statically-literal mincount 1 discharges access safety

**Entry 2 — (c)**

- Derivation: guard conjunct normal-form-equal to the WP (F.count > 0) -> guard-match
- Capability tier: 1a
- Validity arguments: Guard normal-form match
- Decision procedure: Normal-form match of the WP (F.count > 0) against the row's guard conjuncts (a finite set). A conjunct normal-form-equal to F.count > 0 discharges; a semantically-equivalent but non-normal-form spelling (e.g. F.count >= 1) does not match under the stated normalization and is rejected (live-verified, this group) — a sound-but-unprovable band member, not a near-miss, since it does not weaken the guarantee.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Guard normal-form match — the WP has shape <RHS> <cmp> <bound>, licensed premise is a guard conjunct normal-form-equal to it
- docs/language/collection-types.md § Guard pattern — spec

**Entry 3 — (d)**

- Derivation: a rule stating F.count > 0 (or a bound entailing it) holds in the pre-state, and the write plan does not write F, so the fact is frame-preserved into the post-state the read observes
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Look up a rule establishing F.count > 0 (or an entailing bound) as holding in the pre-state (the inductive hypothesis), and confirm the write plan's actions do not write F (frame-preservation, a finite syntactic check over the plan's action list). Absent such a rule, or a plan that does write F without re-establishing the bound, fails the class.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Inductive hypothesis plus sign monotonicity — premise (d) supplies the pre-state fact; here the write plan does not touch the collection at all (frame-preserved), so no sign-monotonicity step is actually needed beyond identity, a simpler case than Family 1 Base B's subtraction shape
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — owner-ruling, 2026-07-20: false-proof hazard: a rule consumed as a premise can discharge an obligation nothing establishes or preserves; builtStatus for this class is never confirmed by a clean compile alone

### What the failing diagnostic must suggest

- For class (a): declare mincount 1 (or higher) on the collection field
  - docs/language/collection-types.md § Constraint Catalog — spec

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (d): restate <WP> as a rule holding in the pre-state, established at construction and preserved by every other write site of the collection
  - docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — owner-ruling, 2026-07-20

  - This suggestion is offered only where class (d) is structurally available (transition-row-action-operand, state-hook-action-operand — not construction-row-action-operand, which has no pre-state). HEAD does not consume a rule as a premise for this obligation today (live-verified for the list/.first representative, this group); where it is built, the false-proof hazard applies (Defect B).


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept CollectionEmptiness1

field ItemList as list of integer
field Result as integer default 0

state Active initial

event Begin initial
event Read

on Begin
    -> set Result = 0

from Active on Read
    -> set Result = ItemList.first
    -> no transition
```

Required outcome: reject, naming the missing premise classes (a), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*mincount-1* — arg-modifier

- Addition: mincount 1
- Premise classes: (a)
- Derivation: mincount 1 -> IntervalContainment (F.count >= 1 entails F.count > 0)
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified in the batch file witness-g13/discharge-a-mincount.precept (all twelve non-.at accessors of this group compiled together); this field's own diagnostic absence confirms the discharge.
- No structured `application` recorded: the addition modifies the collection field's own declaration (appending the mincount modifier), which fits neither of the schema's two application loci (row-guard is for a row's when clause; event-arg-declarations is for event-argument declarations, not field declarations). This is a schema-vocabulary gap for field-declaration-locus additions, not invented around here.

*guard-nf* — guard

- Addition: when ItemList.count > 0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the WP -> guard-match
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: appended to the guard of the `on Read` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified in the batch file witness-g13/discharge-c-guard.precept.

*rule-premise* — other

- Addition: rule ItemList.count > 0 because "..."
- Premise classes: (d)
- Derivation: premise (d): rule holds in pre-state; write plan does not touch F -> frame-preserved
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Live-verified: with the rule declared and no other premise, the read still raises UnguardedCollectionAccess — this fault check does not consume rule facts as a premise at HEAD. Separately, per the Defect B false-proof hazard, even a built consumption route could not be confirmed by a clean compile alone.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified in witness-g13/discharge-d-rule.precept against ItemList/.first as the group's representative accessor; not independently re-run for the other eleven accessors, which share the identical NumericProofRequirement catalog shape (src/Precept/Language/Types.cs).

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| mincount-1 | mincount 0 | mincount 0 does not establish F.count >= 1 — must still reject, same obligation | reject, naming the same obligation |
| guard-nf | when ItemList.count >= 0 | vacuously true for a non-negative count — carries no information, must still reject, same obligation | reject, naming the same obligation |
| rule-premise | rule ItemList.count >= 0 because "..." | vacuous rule (count is never negative) — establishes nothing beyond the type's own range, must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Evaluation-site coordinate: this cell's coordinates name transition-row-action-operand as the representative site (the group's shared setting: a transition row copying from each accessor into a scalar field). The identical obligation, discharge contract, and decision procedures generalize unchanged to construction-row-action-operand and state-hook-action-operand, per this group's disposition-map entries (all three carry disposition defined for every one of the fifteen accessors): class (a) and (c) discharge at all three sites (spot-verified live at construction-row-action-operand and state-hook-action-operand for the list/.first representative); class (d) is available at transition-row-action-operand and state-hook-action-operand only, because construction-row-action-operand has no pre-state to read from (docs/language/precept-language-spec.md § Stateless/stateful cross-validation).
- Respellability finding: `F.count >= 1` is semantically equivalent to F.count > 0 for an integer count and is NOT accepted by the guard-match decision procedure (live-verified) — a genuine sound-but-unprovable band member under this family's respellability verdict, distinct from the near-miss above (which is genuinely insufficient, not merely unrecognized).

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: Fault family row: the catalog-declared safety precondition at the evaluation site
- src/Precept/Language/Types.cs:297 — code: list accessor .first — NumericProofRequirement(SelfSubject(count), GreaterThan, 0, "List must be non-empty"); full declaration spans lines 297-302
- docs/language/collection-types.md § `list of T` — spec: the accessor table naming the .count > 0 proof requirement and the mincount 1 static discharge
- docs/language/collection-types.md § Guard pattern — spec: F.count > 0 recognized in a when clause as sufficient proof for .peek/.min/.max/.first/.last/dequeue/pop

## g13/list-last — List .last — reading a possibly-empty collection at a write site

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | list accessor .last |
| evaluation site category | transition-row-action-operand |
| type family | collection |

### What must be proven

Obligation: F.count > 0

Weakest precondition: F.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| F | the collection field the accessor reads |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a), (c), (d).

The read projects the receiver's own .count; no event argument represents a collection's cardinality, so class (b) never applies to this obligation regardless of site. Class (a) applies wherever the field declares mincount. Class (c) applies at every evaluation-site category in this group's region, all three carrying a guard position. Class (d) applies only where a pre-state exists — transition-row-action-operand and state-hook-action-operand, not construction-row-action-operand.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: a statically-literal mincount >= 1 on the collection field establishes F.count >= 1, which entails F.count > 0
- Strategy: IntervalContainment
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Look up a mincount modifier on the collection field's declaration (a finite, statically-literal set). mincount >= 1 discharges the class; no mincount modifier, or mincount 0, fails it (live-verified, this group).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Arg-bound interval arithmetic — governance enforces every declared constraint on a value before computation derives from it; extended here from an arg-declared bound to a field-declared mincount bound, both realized as the same ProofSatisfaction.Numeric catalog shape (src/Precept/Language/Modifiers.cs:195, spanning 195-208)
- src/Precept/Language/Modifiers.cs:195 — code: ModifierKind.Mincount ProofSatisfaction.Numeric(Accessor(count), GreaterThanOrEqual, DeclarationValue); full declaration spans lines 195-208
- docs/language/collection-types.md § Constraint Catalog — spec: a statically-literal mincount 1 discharges access safety

**Entry 2 — (c)**

- Derivation: guard conjunct normal-form-equal to the WP (F.count > 0) -> guard-match
- Capability tier: 1a
- Validity arguments: Guard normal-form match
- Decision procedure: Normal-form match of the WP (F.count > 0) against the row's guard conjuncts (a finite set). A conjunct normal-form-equal to F.count > 0 discharges; a semantically-equivalent but non-normal-form spelling (e.g. F.count >= 1) does not match under the stated normalization and is rejected (live-verified, this group) — a sound-but-unprovable band member, not a near-miss, since it does not weaken the guarantee.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Guard normal-form match — the WP has shape <RHS> <cmp> <bound>, licensed premise is a guard conjunct normal-form-equal to it
- docs/language/collection-types.md § Guard pattern — spec

**Entry 3 — (d)**

- Derivation: a rule stating F.count > 0 (or a bound entailing it) holds in the pre-state, and the write plan does not write F, so the fact is frame-preserved into the post-state the read observes
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Look up a rule establishing F.count > 0 (or an entailing bound) as holding in the pre-state (the inductive hypothesis), and confirm the write plan's actions do not write F (frame-preservation, a finite syntactic check over the plan's action list). Absent such a rule, or a plan that does write F without re-establishing the bound, fails the class.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Inductive hypothesis plus sign monotonicity — premise (d) supplies the pre-state fact; here the write plan does not touch the collection at all (frame-preserved), so no sign-monotonicity step is actually needed beyond identity, a simpler case than Family 1 Base B's subtraction shape
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — owner-ruling, 2026-07-20: false-proof hazard: a rule consumed as a premise can discharge an obligation nothing establishes or preserves; builtStatus for this class is never confirmed by a clean compile alone

### What the failing diagnostic must suggest

- For class (a): declare mincount 1 (or higher) on the collection field
  - docs/language/collection-types.md § Constraint Catalog — spec

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (d): restate <WP> as a rule holding in the pre-state, established at construction and preserved by every other write site of the collection
  - docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — owner-ruling, 2026-07-20

  - This suggestion is offered only where class (d) is structurally available (transition-row-action-operand, state-hook-action-operand — not construction-row-action-operand, which has no pre-state). HEAD does not consume a rule as a premise for this obligation today (live-verified for the list/.first representative, this group); where it is built, the false-proof hazard applies (Defect B).


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept CollectionEmptiness2

field ItemList as list of integer
field Result as integer default 0

state Active initial

event Begin initial
event Read

on Begin
    -> set Result = 0

from Active on Read
    -> set Result = ItemList.last
    -> no transition
```

Required outcome: reject, naming the missing premise classes (a), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*mincount-1* — arg-modifier

- Addition: mincount 1
- Premise classes: (a)
- Derivation: mincount 1 -> IntervalContainment (F.count >= 1 entails F.count > 0)
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified in the batch file witness-g13/discharge-a-mincount.precept (all twelve non-.at accessors of this group compiled together); this field's own diagnostic absence confirms the discharge.
- No structured `application` recorded: the addition modifies the collection field's own declaration (appending the mincount modifier), which fits neither of the schema's two application loci (row-guard is for a row's when clause; event-arg-declarations is for event-argument declarations, not field declarations). This is a schema-vocabulary gap for field-declaration-locus additions, not invented around here.

*guard-nf* — guard

- Addition: when ItemList.count > 0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the WP -> guard-match
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: appended to the guard of the `on Read` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified in the batch file witness-g13/discharge-c-guard.precept.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| mincount-1 | mincount 0 | mincount 0 does not establish F.count >= 1 — must still reject, same obligation | reject, naming the same obligation |
| guard-nf | when ItemList.count >= 0 | vacuously true for a non-negative count — carries no information, must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Evaluation-site coordinate: this cell's coordinates name transition-row-action-operand as the representative site (the group's shared setting: a transition row copying from each accessor into a scalar field). The identical obligation, discharge contract, and decision procedures generalize unchanged to construction-row-action-operand and state-hook-action-operand, per this group's disposition-map entries (all three carry disposition defined for every one of the fifteen accessors): class (a) and (c) discharge at all three sites (spot-verified live at construction-row-action-operand and state-hook-action-operand for the list/.first representative); class (d) is available at transition-row-action-operand and state-hook-action-operand only, because construction-row-action-operand has no pre-state to read from (docs/language/precept-language-spec.md § Stateless/stateful cross-validation).
- The (a) and (c) discharges and their near-misses were live-verified for this specific accessor (batch files listed in the discharge/near-miss provenance). The (d) rule-premise route is stated in the discharge contract per the shared derivation but was not independently witnessed for this accessor — see g13/list-first for the live-verified representative; the NumericProofRequirement catalog shape (src/Precept/Language/Types.cs) is identical across all twelve accessors in this group, so the same non-consumption finding is expected to generalize, but this is recorded as model-derived-from-representative, not independently confirmed.
- Respellability finding: `F.count >= 1` is semantically equivalent to F.count > 0 for an integer count and is NOT accepted by the guard-match decision procedure (live-verified) — a genuine sound-but-unprovable band member under this family's respellability verdict, distinct from the near-miss above (which is genuinely insufficient, not merely unrecognized).

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: Fault family row: the catalog-declared safety precondition at the evaluation site
- src/Precept/Language/Types.cs:303 — code: list accessor .last — NumericProofRequirement(SelfSubject(count), GreaterThan, 0, "List must be non-empty"); full declaration spans lines 303-308
- docs/language/collection-types.md § `list of T` — spec: the accessor table naming the .count > 0 proof requirement and the mincount 1 static discharge
- docs/language/collection-types.md § Guard pattern — spec: F.count > 0 recognized in a when clause as sufficient proof for .peek/.min/.max/.first/.last/dequeue/pop

## g13/log-first — Log .first — reading a possibly-empty collection at a write site

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | log accessor .first |
| evaluation site category | transition-row-action-operand |
| type family | collection |

### What must be proven

Obligation: F.count > 0

Weakest precondition: F.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| F | the collection field the accessor reads |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a), (c), (d).

The read projects the receiver's own .count; no event argument represents a collection's cardinality, so class (b) never applies to this obligation regardless of site. Class (a) applies wherever the field declares mincount. Class (c) applies at every evaluation-site category in this group's region, all three carrying a guard position. Class (d) applies only where a pre-state exists — transition-row-action-operand and state-hook-action-operand, not construction-row-action-operand.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: a statically-literal mincount >= 1 on the collection field establishes F.count >= 1, which entails F.count > 0
- Strategy: IntervalContainment
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Look up a mincount modifier on the collection field's declaration (a finite, statically-literal set). mincount >= 1 discharges the class; no mincount modifier, or mincount 0, fails it (live-verified, this group).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Arg-bound interval arithmetic — governance enforces every declared constraint on a value before computation derives from it; extended here from an arg-declared bound to a field-declared mincount bound, both realized as the same ProofSatisfaction.Numeric catalog shape (src/Precept/Language/Modifiers.cs:195, spanning 195-208)
- src/Precept/Language/Modifiers.cs:195 — code: ModifierKind.Mincount ProofSatisfaction.Numeric(Accessor(count), GreaterThanOrEqual, DeclarationValue); full declaration spans lines 195-208
- docs/language/collection-types.md § Constraint Catalog — spec: a statically-literal mincount 1 discharges access safety

**Entry 2 — (c)**

- Derivation: guard conjunct normal-form-equal to the WP (F.count > 0) -> guard-match
- Capability tier: 1a
- Validity arguments: Guard normal-form match
- Decision procedure: Normal-form match of the WP (F.count > 0) against the row's guard conjuncts (a finite set). A conjunct normal-form-equal to F.count > 0 discharges; a semantically-equivalent but non-normal-form spelling (e.g. F.count >= 1) does not match under the stated normalization and is rejected (live-verified, this group) — a sound-but-unprovable band member, not a near-miss, since it does not weaken the guarantee.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Guard normal-form match — the WP has shape <RHS> <cmp> <bound>, licensed premise is a guard conjunct normal-form-equal to it
- docs/language/collection-types.md § Guard pattern — spec

**Entry 3 — (d)**

- Derivation: a rule stating F.count > 0 (or a bound entailing it) holds in the pre-state, and the write plan does not write F, so the fact is frame-preserved into the post-state the read observes
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Look up a rule establishing F.count > 0 (or an entailing bound) as holding in the pre-state (the inductive hypothesis), and confirm the write plan's actions do not write F (frame-preservation, a finite syntactic check over the plan's action list). Absent such a rule, or a plan that does write F without re-establishing the bound, fails the class.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Inductive hypothesis plus sign monotonicity — premise (d) supplies the pre-state fact; here the write plan does not touch the collection at all (frame-preserved), so no sign-monotonicity step is actually needed beyond identity, a simpler case than Family 1 Base B's subtraction shape
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — owner-ruling, 2026-07-20: false-proof hazard: a rule consumed as a premise can discharge an obligation nothing establishes or preserves; builtStatus for this class is never confirmed by a clean compile alone

### What the failing diagnostic must suggest

- For class (a): declare mincount 1 (or higher) on the collection field
  - docs/language/collection-types.md § Constraint Catalog — spec

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (d): restate <WP> as a rule holding in the pre-state, established at construction and preserved by every other write site of the collection
  - docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — owner-ruling, 2026-07-20

  - This suggestion is offered only where class (d) is structurally available (transition-row-action-operand, state-hook-action-operand — not construction-row-action-operand, which has no pre-state). HEAD does not consume a rule as a premise for this obligation today (live-verified for the list/.first representative, this group); where it is built, the false-proof hazard applies (Defect B).


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept CollectionEmptiness3

field EntryLog as log of integer
field Result as integer default 0

state Active initial

event Begin initial
event Read

on Begin
    -> set Result = 0

from Active on Read
    -> set Result = EntryLog.first
    -> no transition
```

Required outcome: reject, naming the missing premise classes (a), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*mincount-1* — arg-modifier

- Addition: mincount 1
- Premise classes: (a)
- Derivation: mincount 1 -> IntervalContainment (F.count >= 1 entails F.count > 0)
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified in the batch file witness-g13/discharge-a-mincount.precept (all twelve non-.at accessors of this group compiled together); this field's own diagnostic absence confirms the discharge.
- No structured `application` recorded: the addition modifies the collection field's own declaration (appending the mincount modifier), which fits neither of the schema's two application loci (row-guard is for a row's when clause; event-arg-declarations is for event-argument declarations, not field declarations). This is a schema-vocabulary gap for field-declaration-locus additions, not invented around here.

*guard-nf* — guard

- Addition: when EntryLog.count > 0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the WP -> guard-match
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: appended to the guard of the `on Read` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified in the batch file witness-g13/discharge-c-guard.precept.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| mincount-1 | mincount 0 | mincount 0 does not establish F.count >= 1 — must still reject, same obligation | reject, naming the same obligation |
| guard-nf | when EntryLog.count >= 0 | vacuously true for a non-negative count — carries no information, must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Evaluation-site coordinate: this cell's coordinates name transition-row-action-operand as the representative site (the group's shared setting: a transition row copying from each accessor into a scalar field). The identical obligation, discharge contract, and decision procedures generalize unchanged to construction-row-action-operand and state-hook-action-operand, per this group's disposition-map entries (all three carry disposition defined for every one of the fifteen accessors): class (a) and (c) discharge at all three sites (spot-verified live at construction-row-action-operand and state-hook-action-operand for the list/.first representative); class (d) is available at transition-row-action-operand and state-hook-action-operand only, because construction-row-action-operand has no pre-state to read from (docs/language/precept-language-spec.md § Stateless/stateful cross-validation).
- The (a) and (c) discharges and their near-misses were live-verified for this specific accessor (batch files listed in the discharge/near-miss provenance). The (d) rule-premise route is stated in the discharge contract per the shared derivation but was not independently witnessed for this accessor — see g13/list-first for the live-verified representative; the NumericProofRequirement catalog shape (src/Precept/Language/Types.cs) is identical across all twelve accessors in this group, so the same non-consumption finding is expected to generalize, but this is recorded as model-derived-from-representative, not independently confirmed.
- Respellability finding: `F.count >= 1` is semantically equivalent to F.count > 0 for an integer count and is NOT accepted by the guard-match decision procedure (live-verified) — a genuine sound-but-unprovable band member under this family's respellability verdict, distinct from the near-miss above (which is genuinely insufficient, not merely unrecognized).

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: Fault family row: the catalog-declared safety precondition at the evaluation site
- src/Precept/Language/Types.cs:227 — code: log accessor .first — NumericProofRequirement(SelfSubject(count), GreaterThan, 0, "Log must be non-empty"); full declaration spans lines 227-232
- docs/language/collection-types.md § `log of T` — spec: the accessor table naming the .count > 0 proof requirement and the mincount 1 static discharge
- docs/language/collection-types.md § Guard pattern — spec: F.count > 0 recognized in a when clause as sufficient proof for .peek/.min/.max/.first/.last/dequeue/pop

## g13/log-last — Log .last — reading a possibly-empty collection at a write site

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | log accessor .last |
| evaluation site category | transition-row-action-operand |
| type family | collection |

### What must be proven

Obligation: F.count > 0

Weakest precondition: F.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| F | the collection field the accessor reads |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a), (c), (d).

The read projects the receiver's own .count; no event argument represents a collection's cardinality, so class (b) never applies to this obligation regardless of site. Class (a) applies wherever the field declares mincount. Class (c) applies at every evaluation-site category in this group's region, all three carrying a guard position. Class (d) applies only where a pre-state exists — transition-row-action-operand and state-hook-action-operand, not construction-row-action-operand.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: a statically-literal mincount >= 1 on the collection field establishes F.count >= 1, which entails F.count > 0
- Strategy: IntervalContainment
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Look up a mincount modifier on the collection field's declaration (a finite, statically-literal set). mincount >= 1 discharges the class; no mincount modifier, or mincount 0, fails it (live-verified, this group).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Arg-bound interval arithmetic — governance enforces every declared constraint on a value before computation derives from it; extended here from an arg-declared bound to a field-declared mincount bound, both realized as the same ProofSatisfaction.Numeric catalog shape (src/Precept/Language/Modifiers.cs:195, spanning 195-208)
- src/Precept/Language/Modifiers.cs:195 — code: ModifierKind.Mincount ProofSatisfaction.Numeric(Accessor(count), GreaterThanOrEqual, DeclarationValue); full declaration spans lines 195-208
- docs/language/collection-types.md § Constraint Catalog — spec: a statically-literal mincount 1 discharges access safety

**Entry 2 — (c)**

- Derivation: guard conjunct normal-form-equal to the WP (F.count > 0) -> guard-match
- Capability tier: 1a
- Validity arguments: Guard normal-form match
- Decision procedure: Normal-form match of the WP (F.count > 0) against the row's guard conjuncts (a finite set). A conjunct normal-form-equal to F.count > 0 discharges; a semantically-equivalent but non-normal-form spelling (e.g. F.count >= 1) does not match under the stated normalization and is rejected (live-verified, this group) — a sound-but-unprovable band member, not a near-miss, since it does not weaken the guarantee.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Guard normal-form match — the WP has shape <RHS> <cmp> <bound>, licensed premise is a guard conjunct normal-form-equal to it
- docs/language/collection-types.md § Guard pattern — spec

**Entry 3 — (d)**

- Derivation: a rule stating F.count > 0 (or a bound entailing it) holds in the pre-state, and the write plan does not write F, so the fact is frame-preserved into the post-state the read observes
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Look up a rule establishing F.count > 0 (or an entailing bound) as holding in the pre-state (the inductive hypothesis), and confirm the write plan's actions do not write F (frame-preservation, a finite syntactic check over the plan's action list). Absent such a rule, or a plan that does write F without re-establishing the bound, fails the class.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Inductive hypothesis plus sign monotonicity — premise (d) supplies the pre-state fact; here the write plan does not touch the collection at all (frame-preserved), so no sign-monotonicity step is actually needed beyond identity, a simpler case than Family 1 Base B's subtraction shape
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — owner-ruling, 2026-07-20: false-proof hazard: a rule consumed as a premise can discharge an obligation nothing establishes or preserves; builtStatus for this class is never confirmed by a clean compile alone

### What the failing diagnostic must suggest

- For class (a): declare mincount 1 (or higher) on the collection field
  - docs/language/collection-types.md § Constraint Catalog — spec

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (d): restate <WP> as a rule holding in the pre-state, established at construction and preserved by every other write site of the collection
  - docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — owner-ruling, 2026-07-20

  - This suggestion is offered only where class (d) is structurally available (transition-row-action-operand, state-hook-action-operand — not construction-row-action-operand, which has no pre-state). HEAD does not consume a rule as a premise for this obligation today (live-verified for the list/.first representative, this group); where it is built, the false-proof hazard applies (Defect B).


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept CollectionEmptiness4

field EntryLog as log of integer
field Result as integer default 0

state Active initial

event Begin initial
event Read

on Begin
    -> set Result = 0

from Active on Read
    -> set Result = EntryLog.last
    -> no transition
```

Required outcome: reject, naming the missing premise classes (a), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*mincount-1* — arg-modifier

- Addition: mincount 1
- Premise classes: (a)
- Derivation: mincount 1 -> IntervalContainment (F.count >= 1 entails F.count > 0)
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified in the batch file witness-g13/discharge-a-mincount.precept (all twelve non-.at accessors of this group compiled together); this field's own diagnostic absence confirms the discharge.
- No structured `application` recorded: the addition modifies the collection field's own declaration (appending the mincount modifier), which fits neither of the schema's two application loci (row-guard is for a row's when clause; event-arg-declarations is for event-argument declarations, not field declarations). This is a schema-vocabulary gap for field-declaration-locus additions, not invented around here.

*guard-nf* — guard

- Addition: when EntryLog.count > 0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the WP -> guard-match
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: appended to the guard of the `on Read` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified in the batch file witness-g13/discharge-c-guard.precept.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| mincount-1 | mincount 0 | mincount 0 does not establish F.count >= 1 — must still reject, same obligation | reject, naming the same obligation |
| guard-nf | when EntryLog.count >= 0 | vacuously true for a non-negative count — carries no information, must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Evaluation-site coordinate: this cell's coordinates name transition-row-action-operand as the representative site (the group's shared setting: a transition row copying from each accessor into a scalar field). The identical obligation, discharge contract, and decision procedures generalize unchanged to construction-row-action-operand and state-hook-action-operand, per this group's disposition-map entries (all three carry disposition defined for every one of the fifteen accessors): class (a) and (c) discharge at all three sites (spot-verified live at construction-row-action-operand and state-hook-action-operand for the list/.first representative); class (d) is available at transition-row-action-operand and state-hook-action-operand only, because construction-row-action-operand has no pre-state to read from (docs/language/precept-language-spec.md § Stateless/stateful cross-validation).
- The (a) and (c) discharges and their near-misses were live-verified for this specific accessor (batch files listed in the discharge/near-miss provenance). The (d) rule-premise route is stated in the discharge contract per the shared derivation but was not independently witnessed for this accessor — see g13/list-first for the live-verified representative; the NumericProofRequirement catalog shape (src/Precept/Language/Types.cs) is identical across all twelve accessors in this group, so the same non-consumption finding is expected to generalize, but this is recorded as model-derived-from-representative, not independently confirmed.
- Respellability finding: `F.count >= 1` is semantically equivalent to F.count > 0 for an integer count and is NOT accepted by the guard-match decision procedure (live-verified) — a genuine sound-but-unprovable band member under this family's respellability verdict, distinct from the near-miss above (which is genuinely insufficient, not merely unrecognized).

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: Fault family row: the catalog-declared safety precondition at the evaluation site
- src/Precept/Language/Types.cs:233 — code: log accessor .last — NumericProofRequirement(SelfSubject(count), GreaterThan, 0, "Log must be non-empty"); full declaration spans lines 233-238
- docs/language/collection-types.md § `log of T` — spec: the accessor table naming the .count > 0 proof requirement and the mincount 1 static discharge
- docs/language/collection-types.md § Guard pattern — spec: F.count > 0 recognized in a when clause as sufficient proof for .peek/.min/.max/.first/.last/dequeue/pop

## g13/logby-first — Log By .first — reading a possibly-empty collection at a write site

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | log by accessor .first |
| evaluation site category | transition-row-action-operand |
| type family | collection |

### What must be proven

Obligation: F.count > 0

Weakest precondition: F.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| F | the collection field the accessor reads |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a), (c), (d).

The read projects the receiver's own .count; no event argument represents a collection's cardinality, so class (b) never applies to this obligation regardless of site. Class (a) applies wherever the field declares mincount. Class (c) applies at every evaluation-site category in this group's region, all three carrying a guard position. Class (d) applies only where a pre-state exists — transition-row-action-operand and state-hook-action-operand, not construction-row-action-operand.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: a statically-literal mincount >= 1 on the collection field establishes F.count >= 1, which entails F.count > 0
- Strategy: IntervalContainment
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Look up a mincount modifier on the collection field's declaration (a finite, statically-literal set). mincount >= 1 discharges the class; no mincount modifier, or mincount 0, fails it (live-verified, this group).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Arg-bound interval arithmetic — governance enforces every declared constraint on a value before computation derives from it; extended here from an arg-declared bound to a field-declared mincount bound, both realized as the same ProofSatisfaction.Numeric catalog shape (src/Precept/Language/Modifiers.cs:195, spanning 195-208)
- src/Precept/Language/Modifiers.cs:195 — code: ModifierKind.Mincount ProofSatisfaction.Numeric(Accessor(count), GreaterThanOrEqual, DeclarationValue); full declaration spans lines 195-208
- docs/language/collection-types.md § Constraint Catalog — spec: a statically-literal mincount 1 discharges access safety

**Entry 2 — (c)**

- Derivation: guard conjunct normal-form-equal to the WP (F.count > 0) -> guard-match
- Capability tier: 1a
- Validity arguments: Guard normal-form match
- Decision procedure: Normal-form match of the WP (F.count > 0) against the row's guard conjuncts (a finite set). A conjunct normal-form-equal to F.count > 0 discharges; a semantically-equivalent but non-normal-form spelling (e.g. F.count >= 1) does not match under the stated normalization and is rejected (live-verified, this group) — a sound-but-unprovable band member, not a near-miss, since it does not weaken the guarantee.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Guard normal-form match — the WP has shape <RHS> <cmp> <bound>, licensed premise is a guard conjunct normal-form-equal to it
- docs/language/collection-types.md § Guard pattern — spec

**Entry 3 — (d)**

- Derivation: a rule stating F.count > 0 (or a bound entailing it) holds in the pre-state, and the write plan does not write F, so the fact is frame-preserved into the post-state the read observes
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Look up a rule establishing F.count > 0 (or an entailing bound) as holding in the pre-state (the inductive hypothesis), and confirm the write plan's actions do not write F (frame-preservation, a finite syntactic check over the plan's action list). Absent such a rule, or a plan that does write F without re-establishing the bound, fails the class.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Inductive hypothesis plus sign monotonicity — premise (d) supplies the pre-state fact; here the write plan does not touch the collection at all (frame-preserved), so no sign-monotonicity step is actually needed beyond identity, a simpler case than Family 1 Base B's subtraction shape
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — owner-ruling, 2026-07-20: false-proof hazard: a rule consumed as a premise can discharge an obligation nothing establishes or preserves; builtStatus for this class is never confirmed by a clean compile alone

### What the failing diagnostic must suggest

- For class (a): declare mincount 1 (or higher) on the collection field
  - docs/language/collection-types.md § Constraint Catalog — spec

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (d): restate <WP> as a rule holding in the pre-state, established at construction and preserved by every other write site of the collection
  - docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — owner-ruling, 2026-07-20

  - This suggestion is offered only where class (d) is structurally available (transition-row-action-operand, state-hook-action-operand — not construction-row-action-operand, which has no pre-state). HEAD does not consume a rule as a premise for this obligation today (live-verified for the list/.first representative, this group); where it is built, the false-proof hazard applies (Defect B).


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept CollectionEmptiness5

field KeyedLog as log of integer by integer
field Result as integer default 0

state Active initial

event Begin initial
event Read

on Begin
    -> set Result = 0

from Active on Read
    -> set Result = KeyedLog.first
    -> no transition
```

Required outcome: reject, naming the missing premise classes (a), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*mincount-1* — arg-modifier

- Addition: mincount 1
- Premise classes: (a)
- Derivation: mincount 1 -> IntervalContainment (F.count >= 1 entails F.count > 0)
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified in the batch file witness-g13/discharge-a-mincount.precept (all twelve non-.at accessors of this group compiled together); this field's own diagnostic absence confirms the discharge.
- No structured `application` recorded: the addition modifies the collection field's own declaration (appending the mincount modifier), which fits neither of the schema's two application loci (row-guard is for a row's when clause; event-arg-declarations is for event-argument declarations, not field declarations). This is a schema-vocabulary gap for field-declaration-locus additions, not invented around here.

*guard-nf* — guard

- Addition: when KeyedLog.count > 0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the WP -> guard-match
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: appended to the guard of the `on Read` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified in the batch file witness-g13/discharge-c-guard.precept.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| mincount-1 | mincount 0 | mincount 0 does not establish F.count >= 1 — must still reject, same obligation | reject, naming the same obligation |
| guard-nf | when KeyedLog.count >= 0 | vacuously true for a non-negative count — carries no information, must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Evaluation-site coordinate: this cell's coordinates name transition-row-action-operand as the representative site (the group's shared setting: a transition row copying from each accessor into a scalar field). The identical obligation, discharge contract, and decision procedures generalize unchanged to construction-row-action-operand and state-hook-action-operand, per this group's disposition-map entries (all three carry disposition defined for every one of the fifteen accessors): class (a) and (c) discharge at all three sites (spot-verified live at construction-row-action-operand and state-hook-action-operand for the list/.first representative); class (d) is available at transition-row-action-operand and state-hook-action-operand only, because construction-row-action-operand has no pre-state to read from (docs/language/precept-language-spec.md § Stateless/stateful cross-validation).
- The (a) and (c) discharges and their near-misses were live-verified for this specific accessor (batch files listed in the discharge/near-miss provenance). The (d) rule-premise route is stated in the discharge contract per the shared derivation but was not independently witnessed for this accessor — see g13/list-first for the live-verified representative; the NumericProofRequirement catalog shape (src/Precept/Language/Types.cs) is identical across all twelve accessors in this group, so the same non-consumption finding is expected to generalize, but this is recorded as model-derived-from-representative, not independently confirmed.
- Respellability finding: `F.count >= 1` is semantically equivalent to F.count > 0 for an integer count and is NOT accepted by the guard-match decision procedure (live-verified) — a genuine sound-but-unprovable band member under this family's respellability verdict, distinct from the near-miss above (which is genuinely insufficient, not merely unrecognized).

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: Fault family row: the catalog-declared safety precondition at the evaluation site
- src/Precept/Language/Types.cs:255 — code: log by accessor .first — NumericProofRequirement(SelfSubject(count), GreaterThan, 0, "Log must be non-empty"); full declaration spans lines 255-260
- docs/language/collection-types.md § `log of T by P` — spec: the accessor table naming the .count > 0 proof requirement and the mincount 1 static discharge
- docs/language/collection-types.md § Guard pattern — spec: F.count > 0 recognized in a when clause as sufficient proof for .peek/.min/.max/.first/.last/dequeue/pop

## g13/logby-last — Log By .last — reading a possibly-empty collection at a write site

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | log by accessor .last |
| evaluation site category | transition-row-action-operand |
| type family | collection |

### What must be proven

Obligation: F.count > 0

Weakest precondition: F.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| F | the collection field the accessor reads |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a), (c), (d).

The read projects the receiver's own .count; no event argument represents a collection's cardinality, so class (b) never applies to this obligation regardless of site. Class (a) applies wherever the field declares mincount. Class (c) applies at every evaluation-site category in this group's region, all three carrying a guard position. Class (d) applies only where a pre-state exists — transition-row-action-operand and state-hook-action-operand, not construction-row-action-operand.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: a statically-literal mincount >= 1 on the collection field establishes F.count >= 1, which entails F.count > 0
- Strategy: IntervalContainment
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Look up a mincount modifier on the collection field's declaration (a finite, statically-literal set). mincount >= 1 discharges the class; no mincount modifier, or mincount 0, fails it (live-verified, this group).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Arg-bound interval arithmetic — governance enforces every declared constraint on a value before computation derives from it; extended here from an arg-declared bound to a field-declared mincount bound, both realized as the same ProofSatisfaction.Numeric catalog shape (src/Precept/Language/Modifiers.cs:195, spanning 195-208)
- src/Precept/Language/Modifiers.cs:195 — code: ModifierKind.Mincount ProofSatisfaction.Numeric(Accessor(count), GreaterThanOrEqual, DeclarationValue); full declaration spans lines 195-208
- docs/language/collection-types.md § Constraint Catalog — spec: a statically-literal mincount 1 discharges access safety

**Entry 2 — (c)**

- Derivation: guard conjunct normal-form-equal to the WP (F.count > 0) -> guard-match
- Capability tier: 1a
- Validity arguments: Guard normal-form match
- Decision procedure: Normal-form match of the WP (F.count > 0) against the row's guard conjuncts (a finite set). A conjunct normal-form-equal to F.count > 0 discharges; a semantically-equivalent but non-normal-form spelling (e.g. F.count >= 1) does not match under the stated normalization and is rejected (live-verified, this group) — a sound-but-unprovable band member, not a near-miss, since it does not weaken the guarantee.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Guard normal-form match — the WP has shape <RHS> <cmp> <bound>, licensed premise is a guard conjunct normal-form-equal to it
- docs/language/collection-types.md § Guard pattern — spec

**Entry 3 — (d)**

- Derivation: a rule stating F.count > 0 (or a bound entailing it) holds in the pre-state, and the write plan does not write F, so the fact is frame-preserved into the post-state the read observes
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Look up a rule establishing F.count > 0 (or an entailing bound) as holding in the pre-state (the inductive hypothesis), and confirm the write plan's actions do not write F (frame-preservation, a finite syntactic check over the plan's action list). Absent such a rule, or a plan that does write F without re-establishing the bound, fails the class.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Inductive hypothesis plus sign monotonicity — premise (d) supplies the pre-state fact; here the write plan does not touch the collection at all (frame-preserved), so no sign-monotonicity step is actually needed beyond identity, a simpler case than Family 1 Base B's subtraction shape
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — owner-ruling, 2026-07-20: false-proof hazard: a rule consumed as a premise can discharge an obligation nothing establishes or preserves; builtStatus for this class is never confirmed by a clean compile alone

### What the failing diagnostic must suggest

- For class (a): declare mincount 1 (or higher) on the collection field
  - docs/language/collection-types.md § Constraint Catalog — spec

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (d): restate <WP> as a rule holding in the pre-state, established at construction and preserved by every other write site of the collection
  - docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — owner-ruling, 2026-07-20

  - This suggestion is offered only where class (d) is structurally available (transition-row-action-operand, state-hook-action-operand — not construction-row-action-operand, which has no pre-state). HEAD does not consume a rule as a premise for this obligation today (live-verified for the list/.first representative, this group); where it is built, the false-proof hazard applies (Defect B).


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept CollectionEmptiness6

field KeyedLog as log of integer by integer
field Result as integer default 0

state Active initial

event Begin initial
event Read

on Begin
    -> set Result = 0

from Active on Read
    -> set Result = KeyedLog.last
    -> no transition
```

Required outcome: reject, naming the missing premise classes (a), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*mincount-1* — arg-modifier

- Addition: mincount 1
- Premise classes: (a)
- Derivation: mincount 1 -> IntervalContainment (F.count >= 1 entails F.count > 0)
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified in the batch file witness-g13/discharge-a-mincount.precept (all twelve non-.at accessors of this group compiled together); this field's own diagnostic absence confirms the discharge.
- No structured `application` recorded: the addition modifies the collection field's own declaration (appending the mincount modifier), which fits neither of the schema's two application loci (row-guard is for a row's when clause; event-arg-declarations is for event-argument declarations, not field declarations). This is a schema-vocabulary gap for field-declaration-locus additions, not invented around here.

*guard-nf* — guard

- Addition: when KeyedLog.count > 0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the WP -> guard-match
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: appended to the guard of the `on Read` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified in the batch file witness-g13/discharge-c-guard.precept.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| mincount-1 | mincount 0 | mincount 0 does not establish F.count >= 1 — must still reject, same obligation | reject, naming the same obligation |
| guard-nf | when KeyedLog.count >= 0 | vacuously true for a non-negative count — carries no information, must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Evaluation-site coordinate: this cell's coordinates name transition-row-action-operand as the representative site (the group's shared setting: a transition row copying from each accessor into a scalar field). The identical obligation, discharge contract, and decision procedures generalize unchanged to construction-row-action-operand and state-hook-action-operand, per this group's disposition-map entries (all three carry disposition defined for every one of the fifteen accessors): class (a) and (c) discharge at all three sites (spot-verified live at construction-row-action-operand and state-hook-action-operand for the list/.first representative); class (d) is available at transition-row-action-operand and state-hook-action-operand only, because construction-row-action-operand has no pre-state to read from (docs/language/precept-language-spec.md § Stateless/stateful cross-validation).
- The (a) and (c) discharges and their near-misses were live-verified for this specific accessor (batch files listed in the discharge/near-miss provenance). The (d) rule-premise route is stated in the discharge contract per the shared derivation but was not independently witnessed for this accessor — see g13/list-first for the live-verified representative; the NumericProofRequirement catalog shape (src/Precept/Language/Types.cs) is identical across all twelve accessors in this group, so the same non-consumption finding is expected to generalize, but this is recorded as model-derived-from-representative, not independently confirmed.
- Respellability finding: `F.count >= 1` is semantically equivalent to F.count > 0 for an integer count and is NOT accepted by the guard-match decision procedure (live-verified) — a genuine sound-but-unprovable band member under this family's respellability verdict, distinct from the near-miss above (which is genuinely insufficient, not merely unrecognized).

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: Fault family row: the catalog-declared safety precondition at the evaluation site
- src/Precept/Language/Types.cs:261 — code: log by accessor .last — NumericProofRequirement(SelfSubject(count), GreaterThan, 0, "Log must be non-empty"); full declaration spans lines 261-266
- docs/language/collection-types.md § `log of T by P` — spec: the accessor table naming the .count > 0 proof requirement and the mincount 1 static discharge
- docs/language/collection-types.md § Guard pattern — spec: F.count > 0 recognized in a when clause as sufficient proof for .peek/.min/.max/.first/.last/dequeue/pop

## g13/queue-peek — Queue .peek — reading a possibly-empty collection at a write site

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | queue accessor .peek |
| evaluation site category | transition-row-action-operand |
| type family | collection |

### What must be proven

Obligation: F.count > 0

Weakest precondition: F.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| F | the collection field the accessor reads |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a), (c), (d).

The read projects the receiver's own .count; no event argument represents a collection's cardinality, so class (b) never applies to this obligation regardless of site. Class (a) applies wherever the field declares mincount. Class (c) applies at every evaluation-site category in this group's region, all three carrying a guard position. Class (d) applies only where a pre-state exists — transition-row-action-operand and state-hook-action-operand, not construction-row-action-operand.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: a statically-literal mincount >= 1 on the collection field establishes F.count >= 1, which entails F.count > 0
- Strategy: IntervalContainment
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Look up a mincount modifier on the collection field's declaration (a finite, statically-literal set). mincount >= 1 discharges the class; no mincount modifier, or mincount 0, fails it (live-verified, this group).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Arg-bound interval arithmetic — governance enforces every declared constraint on a value before computation derives from it; extended here from an arg-declared bound to a field-declared mincount bound, both realized as the same ProofSatisfaction.Numeric catalog shape (src/Precept/Language/Modifiers.cs:195, spanning 195-208)
- src/Precept/Language/Modifiers.cs:195 — code: ModifierKind.Mincount ProofSatisfaction.Numeric(Accessor(count), GreaterThanOrEqual, DeclarationValue); full declaration spans lines 195-208
- docs/language/collection-types.md § Constraint Catalog — spec: a statically-literal mincount 1 discharges access safety

**Entry 2 — (c)**

- Derivation: guard conjunct normal-form-equal to the WP (F.count > 0) -> guard-match
- Capability tier: 1a
- Validity arguments: Guard normal-form match
- Decision procedure: Normal-form match of the WP (F.count > 0) against the row's guard conjuncts (a finite set). A conjunct normal-form-equal to F.count > 0 discharges; a semantically-equivalent but non-normal-form spelling (e.g. F.count >= 1) does not match under the stated normalization and is rejected (live-verified, this group) — a sound-but-unprovable band member, not a near-miss, since it does not weaken the guarantee.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Guard normal-form match — the WP has shape <RHS> <cmp> <bound>, licensed premise is a guard conjunct normal-form-equal to it
- docs/language/collection-types.md § Guard pattern — spec

**Entry 3 — (d)**

- Derivation: a rule stating F.count > 0 (or a bound entailing it) holds in the pre-state, and the write plan does not write F, so the fact is frame-preserved into the post-state the read observes
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Look up a rule establishing F.count > 0 (or an entailing bound) as holding in the pre-state (the inductive hypothesis), and confirm the write plan's actions do not write F (frame-preservation, a finite syntactic check over the plan's action list). Absent such a rule, or a plan that does write F without re-establishing the bound, fails the class.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Inductive hypothesis plus sign monotonicity — premise (d) supplies the pre-state fact; here the write plan does not touch the collection at all (frame-preserved), so no sign-monotonicity step is actually needed beyond identity, a simpler case than Family 1 Base B's subtraction shape
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — owner-ruling, 2026-07-20: false-proof hazard: a rule consumed as a premise can discharge an obligation nothing establishes or preserves; builtStatus for this class is never confirmed by a clean compile alone

### What the failing diagnostic must suggest

- For class (a): declare mincount 1 (or higher) on the collection field
  - docs/language/collection-types.md § Constraint Catalog — spec

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (d): restate <WP> as a rule holding in the pre-state, established at construction and preserved by every other write site of the collection
  - docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — owner-ruling, 2026-07-20

  - This suggestion is offered only where class (d) is structurally available (transition-row-action-operand, state-hook-action-operand — not construction-row-action-operand, which has no pre-state). HEAD does not consume a rule as a premise for this obligation today (live-verified for the list/.first representative, this group); where it is built, the false-proof hazard applies (Defect B).


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept CollectionEmptiness7

field WorkQueue as queue of integer
field Result as integer default 0

state Active initial

event Begin initial
event Read

on Begin
    -> set Result = 0

from Active on Read
    -> set Result = WorkQueue.peek
    -> no transition
```

Required outcome: reject, naming the missing premise classes (a), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*mincount-1* — arg-modifier

- Addition: mincount 1
- Premise classes: (a)
- Derivation: mincount 1 -> IntervalContainment (F.count >= 1 entails F.count > 0)
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified in the batch file witness-g13/discharge-a-mincount.precept (all twelve non-.at accessors of this group compiled together); this field's own diagnostic absence confirms the discharge.
- No structured `application` recorded: the addition modifies the collection field's own declaration (appending the mincount modifier), which fits neither of the schema's two application loci (row-guard is for a row's when clause; event-arg-declarations is for event-argument declarations, not field declarations). This is a schema-vocabulary gap for field-declaration-locus additions, not invented around here.

*guard-nf* — guard

- Addition: when WorkQueue.count > 0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the WP -> guard-match
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: appended to the guard of the `on Read` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified in the batch file witness-g13/discharge-c-guard.precept.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| mincount-1 | mincount 0 | mincount 0 does not establish F.count >= 1 — must still reject, same obligation | reject, naming the same obligation |
| guard-nf | when WorkQueue.count >= 0 | vacuously true for a non-negative count — carries no information, must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Evaluation-site coordinate: this cell's coordinates name transition-row-action-operand as the representative site (the group's shared setting: a transition row copying from each accessor into a scalar field). The identical obligation, discharge contract, and decision procedures generalize unchanged to construction-row-action-operand and state-hook-action-operand, per this group's disposition-map entries (all three carry disposition defined for every one of the fifteen accessors): class (a) and (c) discharge at all three sites (spot-verified live at construction-row-action-operand and state-hook-action-operand for the list/.first representative); class (d) is available at transition-row-action-operand and state-hook-action-operand only, because construction-row-action-operand has no pre-state to read from (docs/language/precept-language-spec.md § Stateless/stateful cross-validation).
- The (a) and (c) discharges and their near-misses were live-verified for this specific accessor (batch files listed in the discharge/near-miss provenance). The (d) rule-premise route is stated in the discharge contract per the shared derivation but was not independently witnessed for this accessor — see g13/list-first for the live-verified representative; the NumericProofRequirement catalog shape (src/Precept/Language/Types.cs) is identical across all twelve accessors in this group, so the same non-consumption finding is expected to generalize, but this is recorded as model-derived-from-representative, not independently confirmed.
- Respellability finding: `F.count >= 1` is semantically equivalent to F.count > 0 for an integer count and is NOT accepted by the guard-match decision procedure (live-verified) — a genuine sound-but-unprovable band member under this family's respellability verdict, distinct from the near-miss above (which is genuinely insufficient, not merely unrecognized).

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: Fault family row: the catalog-declared safety precondition at the evaluation site
- src/Precept/Language/Types.cs:205 — code: queue accessor .peek — NumericProofRequirement(SelfSubject(count), GreaterThan, 0, "Queue must be non-empty"); full declaration spans lines 205-210
- docs/language/collection-types.md § `queue` — spec: the accessor table naming the .count > 0 proof requirement and the mincount 1 static discharge
- docs/language/collection-types.md § Guard pattern — spec: F.count > 0 recognized in a when clause as sufficient proof for .peek/.min/.max/.first/.last/dequeue/pop

## g13/queueby-peek — Queue By .peek — reading a possibly-empty collection at a write site

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | queue by accessor .peek |
| evaluation site category | transition-row-action-operand |
| type family | collection |

### What must be proven

Obligation: F.count > 0

Weakest precondition: F.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| F | the collection field the accessor reads |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a), (c), (d).

The read projects the receiver's own .count; no event argument represents a collection's cardinality, so class (b) never applies to this obligation regardless of site. Class (a) applies wherever the field declares mincount. Class (c) applies at every evaluation-site category in this group's region, all three carrying a guard position. Class (d) applies only where a pre-state exists — transition-row-action-operand and state-hook-action-operand, not construction-row-action-operand.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: a statically-literal mincount >= 1 on the collection field establishes F.count >= 1, which entails F.count > 0
- Strategy: IntervalContainment
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Look up a mincount modifier on the collection field's declaration (a finite, statically-literal set). mincount >= 1 discharges the class; no mincount modifier, or mincount 0, fails it (live-verified, this group).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Arg-bound interval arithmetic — governance enforces every declared constraint on a value before computation derives from it; extended here from an arg-declared bound to a field-declared mincount bound, both realized as the same ProofSatisfaction.Numeric catalog shape (src/Precept/Language/Modifiers.cs:195, spanning 195-208)
- src/Precept/Language/Modifiers.cs:195 — code: ModifierKind.Mincount ProofSatisfaction.Numeric(Accessor(count), GreaterThanOrEqual, DeclarationValue); full declaration spans lines 195-208
- docs/language/collection-types.md § Constraint Catalog — spec: a statically-literal mincount 1 discharges access safety

**Entry 2 — (c)**

- Derivation: guard conjunct normal-form-equal to the WP (F.count > 0) -> guard-match
- Capability tier: 1a
- Validity arguments: Guard normal-form match
- Decision procedure: Normal-form match of the WP (F.count > 0) against the row's guard conjuncts (a finite set). A conjunct normal-form-equal to F.count > 0 discharges; a semantically-equivalent but non-normal-form spelling (e.g. F.count >= 1) does not match under the stated normalization and is rejected (live-verified, this group) — a sound-but-unprovable band member, not a near-miss, since it does not weaken the guarantee.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Guard normal-form match — the WP has shape <RHS> <cmp> <bound>, licensed premise is a guard conjunct normal-form-equal to it
- docs/language/collection-types.md § Guard pattern — spec

**Entry 3 — (d)**

- Derivation: a rule stating F.count > 0 (or a bound entailing it) holds in the pre-state, and the write plan does not write F, so the fact is frame-preserved into the post-state the read observes
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Look up a rule establishing F.count > 0 (or an entailing bound) as holding in the pre-state (the inductive hypothesis), and confirm the write plan's actions do not write F (frame-preservation, a finite syntactic check over the plan's action list). Absent such a rule, or a plan that does write F without re-establishing the bound, fails the class.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Inductive hypothesis plus sign monotonicity — premise (d) supplies the pre-state fact; here the write plan does not touch the collection at all (frame-preserved), so no sign-monotonicity step is actually needed beyond identity, a simpler case than Family 1 Base B's subtraction shape
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — owner-ruling, 2026-07-20: false-proof hazard: a rule consumed as a premise can discharge an obligation nothing establishes or preserves; builtStatus for this class is never confirmed by a clean compile alone

### What the failing diagnostic must suggest

- For class (a): declare mincount 1 (or higher) on the collection field
  - docs/language/collection-types.md § Constraint Catalog — spec

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (d): restate <WP> as a rule holding in the pre-state, established at construction and preserved by every other write site of the collection
  - docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — owner-ruling, 2026-07-20

  - This suggestion is offered only where class (d) is structurally available (transition-row-action-operand, state-hook-action-operand — not construction-row-action-operand, which has no pre-state). HEAD does not consume a rule as a premise for this obligation today (live-verified for the list/.first representative, this group); where it is built, the false-proof hazard applies (Defect B).


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept CollectionEmptiness8

field PriorityQueue as queue of integer by integer
field Result as integer default 0

state Active initial

event Begin initial
event Read

on Begin
    -> set Result = 0

from Active on Read
    -> set Result = PriorityQueue.peek
    -> no transition
```

Required outcome: reject, naming the missing premise classes (a), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*mincount-1* — arg-modifier

- Addition: mincount 1
- Premise classes: (a)
- Derivation: mincount 1 -> IntervalContainment (F.count >= 1 entails F.count > 0)
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified in the batch file witness-g13/discharge-a-mincount.precept (all twelve non-.at accessors of this group compiled together); this field's own diagnostic absence confirms the discharge.
- No structured `application` recorded: the addition modifies the collection field's own declaration (appending the mincount modifier), which fits neither of the schema's two application loci (row-guard is for a row's when clause; event-arg-declarations is for event-argument declarations, not field declarations). This is a schema-vocabulary gap for field-declaration-locus additions, not invented around here.

*guard-nf* — guard

- Addition: when PriorityQueue.count > 0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the WP -> guard-match
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: appended to the guard of the `on Read` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified in the batch file witness-g13/discharge-c-guard.precept.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| mincount-1 | mincount 0 | mincount 0 does not establish F.count >= 1 — must still reject, same obligation | reject, naming the same obligation |
| guard-nf | when PriorityQueue.count >= 0 | vacuously true for a non-negative count — carries no information, must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Evaluation-site coordinate: this cell's coordinates name transition-row-action-operand as the representative site (the group's shared setting: a transition row copying from each accessor into a scalar field). The identical obligation, discharge contract, and decision procedures generalize unchanged to construction-row-action-operand and state-hook-action-operand, per this group's disposition-map entries (all three carry disposition defined for every one of the fifteen accessors): class (a) and (c) discharge at all three sites (spot-verified live at construction-row-action-operand and state-hook-action-operand for the list/.first representative); class (d) is available at transition-row-action-operand and state-hook-action-operand only, because construction-row-action-operand has no pre-state to read from (docs/language/precept-language-spec.md § Stateless/stateful cross-validation).
- The (a) and (c) discharges and their near-misses were live-verified for this specific accessor (batch files listed in the discharge/near-miss provenance). The (d) rule-premise route is stated in the discharge contract per the shared derivation but was not independently witnessed for this accessor — see g13/list-first for the live-verified representative; the NumericProofRequirement catalog shape (src/Precept/Language/Types.cs) is identical across all twelve accessors in this group, so the same non-consumption finding is expected to generalize, but this is recorded as model-derived-from-representative, not independently confirmed.
- Respellability finding: `F.count >= 1` is semantically equivalent to F.count > 0 for an integer count and is NOT accepted by the guard-match decision procedure (live-verified) — a genuine sound-but-unprovable band member under this family's respellability verdict, distinct from the near-miss above (which is genuinely insufficient, not merely unrecognized).

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: Fault family row: the catalog-declared safety precondition at the evaluation site
- src/Precept/Language/Types.cs:325 — code: queue by accessor .peek — NumericProofRequirement(SelfSubject(count), GreaterThan, 0, "Queue must be non-empty"); full declaration spans lines 325-330
- docs/language/collection-types.md § `queue of T by P` — spec: the accessor table naming the .count > 0 proof requirement and the mincount 1 static discharge
- docs/language/collection-types.md § Guard pattern — spec: F.count > 0 recognized in a when clause as sufficient proof for .peek/.min/.max/.first/.last/dequeue/pop

## g13/queueby-peekby — Queue By .peekby — reading a possibly-empty collection at a write site

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | queue by accessor .peekby |
| evaluation site category | transition-row-action-operand |
| type family | collection |

### What must be proven

Obligation: F.count > 0

Weakest precondition: F.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| F | the collection field the accessor reads |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a), (c), (d).

The read projects the receiver's own .count; no event argument represents a collection's cardinality, so class (b) never applies to this obligation regardless of site. Class (a) applies wherever the field declares mincount. Class (c) applies at every evaluation-site category in this group's region, all three carrying a guard position. Class (d) applies only where a pre-state exists — transition-row-action-operand and state-hook-action-operand, not construction-row-action-operand.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: a statically-literal mincount >= 1 on the collection field establishes F.count >= 1, which entails F.count > 0
- Strategy: IntervalContainment
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Look up a mincount modifier on the collection field's declaration (a finite, statically-literal set). mincount >= 1 discharges the class; no mincount modifier, or mincount 0, fails it (live-verified, this group).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Arg-bound interval arithmetic — governance enforces every declared constraint on a value before computation derives from it; extended here from an arg-declared bound to a field-declared mincount bound, both realized as the same ProofSatisfaction.Numeric catalog shape (src/Precept/Language/Modifiers.cs:195, spanning 195-208)
- src/Precept/Language/Modifiers.cs:195 — code: ModifierKind.Mincount ProofSatisfaction.Numeric(Accessor(count), GreaterThanOrEqual, DeclarationValue); full declaration spans lines 195-208
- docs/language/collection-types.md § Constraint Catalog — spec: a statically-literal mincount 1 discharges access safety

**Entry 2 — (c)**

- Derivation: guard conjunct normal-form-equal to the WP (F.count > 0) -> guard-match
- Capability tier: 1a
- Validity arguments: Guard normal-form match
- Decision procedure: Normal-form match of the WP (F.count > 0) against the row's guard conjuncts (a finite set). A conjunct normal-form-equal to F.count > 0 discharges; a semantically-equivalent but non-normal-form spelling (e.g. F.count >= 1) does not match under the stated normalization and is rejected (live-verified, this group) — a sound-but-unprovable band member, not a near-miss, since it does not weaken the guarantee.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Guard normal-form match — the WP has shape <RHS> <cmp> <bound>, licensed premise is a guard conjunct normal-form-equal to it
- docs/language/collection-types.md § Guard pattern — spec

**Entry 3 — (d)**

- Derivation: a rule stating F.count > 0 (or a bound entailing it) holds in the pre-state, and the write plan does not write F, so the fact is frame-preserved into the post-state the read observes
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Look up a rule establishing F.count > 0 (or an entailing bound) as holding in the pre-state (the inductive hypothesis), and confirm the write plan's actions do not write F (frame-preservation, a finite syntactic check over the plan's action list). Absent such a rule, or a plan that does write F without re-establishing the bound, fails the class.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Inductive hypothesis plus sign monotonicity — premise (d) supplies the pre-state fact; here the write plan does not touch the collection at all (frame-preserved), so no sign-monotonicity step is actually needed beyond identity, a simpler case than Family 1 Base B's subtraction shape
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — owner-ruling, 2026-07-20: false-proof hazard: a rule consumed as a premise can discharge an obligation nothing establishes or preserves; builtStatus for this class is never confirmed by a clean compile alone

### What the failing diagnostic must suggest

- For class (a): declare mincount 1 (or higher) on the collection field
  - docs/language/collection-types.md § Constraint Catalog — spec

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (d): restate <WP> as a rule holding in the pre-state, established at construction and preserved by every other write site of the collection
  - docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — owner-ruling, 2026-07-20

  - This suggestion is offered only where class (d) is structurally available (transition-row-action-operand, state-hook-action-operand — not construction-row-action-operand, which has no pre-state). HEAD does not consume a rule as a premise for this obligation today (live-verified for the list/.first representative, this group); where it is built, the false-proof hazard applies (Defect B).


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept CollectionEmptiness9

field PriorityQueue as queue of integer by integer
field Result as integer default 0

state Active initial

event Begin initial
event Read

on Begin
    -> set Result = 0

from Active on Read
    -> set Result = PriorityQueue.peekby
    -> no transition
```

Required outcome: reject, naming the missing premise classes (a), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*mincount-1* — arg-modifier

- Addition: mincount 1
- Premise classes: (a)
- Derivation: mincount 1 -> IntervalContainment (F.count >= 1 entails F.count > 0)
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified in the batch file witness-g13/discharge-a-mincount.precept (all twelve non-.at accessors of this group compiled together); this field's own diagnostic absence confirms the discharge.
- No structured `application` recorded: the addition modifies the collection field's own declaration (appending the mincount modifier), which fits neither of the schema's two application loci (row-guard is for a row's when clause; event-arg-declarations is for event-argument declarations, not field declarations). This is a schema-vocabulary gap for field-declaration-locus additions, not invented around here.

*guard-nf* — guard

- Addition: when PriorityQueue.count > 0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the WP -> guard-match
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: appended to the guard of the `on Read` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified in the batch file witness-g13/discharge-c-guard.precept.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| mincount-1 | mincount 0 | mincount 0 does not establish F.count >= 1 — must still reject, same obligation | reject, naming the same obligation |
| guard-nf | when PriorityQueue.count >= 0 | vacuously true for a non-negative count — carries no information, must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Evaluation-site coordinate: this cell's coordinates name transition-row-action-operand as the representative site (the group's shared setting: a transition row copying from each accessor into a scalar field). The identical obligation, discharge contract, and decision procedures generalize unchanged to construction-row-action-operand and state-hook-action-operand, per this group's disposition-map entries (all three carry disposition defined for every one of the fifteen accessors): class (a) and (c) discharge at all three sites (spot-verified live at construction-row-action-operand and state-hook-action-operand for the list/.first representative); class (d) is available at transition-row-action-operand and state-hook-action-operand only, because construction-row-action-operand has no pre-state to read from (docs/language/precept-language-spec.md § Stateless/stateful cross-validation).
- The (a) and (c) discharges and their near-misses were live-verified for this specific accessor (batch files listed in the discharge/near-miss provenance). The (d) rule-premise route is stated in the discharge contract per the shared derivation but was not independently witnessed for this accessor — see g13/list-first for the live-verified representative; the NumericProofRequirement catalog shape (src/Precept/Language/Types.cs) is identical across all twelve accessors in this group, so the same non-consumption finding is expected to generalize, but this is recorded as model-derived-from-representative, not independently confirmed.
- Respellability finding: `F.count >= 1` is semantically equivalent to F.count > 0 for an integer count and is NOT accepted by the guard-match decision procedure (live-verified) — a genuine sound-but-unprovable band member under this family's respellability verdict, distinct from the near-miss above (which is genuinely insufficient, not merely unrecognized).

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: Fault family row: the catalog-declared safety precondition at the evaluation site
- src/Precept/Language/Types.cs:331 — code: queue by accessor .peekby — NumericProofRequirement(SelfSubject(count), GreaterThan, 0, "Queue must be non-empty"); full declaration spans lines 331-336
- docs/language/collection-types.md § `queue of T by P` — spec: the accessor table naming the .count > 0 proof requirement and the mincount 1 static discharge
- docs/language/collection-types.md § Guard pattern — spec: F.count > 0 recognized in a when clause as sufficient proof for .peek/.min/.max/.first/.last/dequeue/pop

## g13/stack-peek — Stack .peek — reading a possibly-empty collection at a write site

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | stack accessor .peek |
| evaluation site category | transition-row-action-operand |
| type family | collection |

### What must be proven

Obligation: F.count > 0

Weakest precondition: F.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| F | the collection field the accessor reads |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a), (c), (d).

The read projects the receiver's own .count; no event argument represents a collection's cardinality, so class (b) never applies to this obligation regardless of site. Class (a) applies wherever the field declares mincount. Class (c) applies at every evaluation-site category in this group's region, all three carrying a guard position. Class (d) applies only where a pre-state exists — transition-row-action-operand and state-hook-action-operand, not construction-row-action-operand.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: a statically-literal mincount >= 1 on the collection field establishes F.count >= 1, which entails F.count > 0
- Strategy: IntervalContainment
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Look up a mincount modifier on the collection field's declaration (a finite, statically-literal set). mincount >= 1 discharges the class; no mincount modifier, or mincount 0, fails it (live-verified, this group).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Arg-bound interval arithmetic — governance enforces every declared constraint on a value before computation derives from it; extended here from an arg-declared bound to a field-declared mincount bound, both realized as the same ProofSatisfaction.Numeric catalog shape (src/Precept/Language/Modifiers.cs:195, spanning 195-208)
- src/Precept/Language/Modifiers.cs:195 — code: ModifierKind.Mincount ProofSatisfaction.Numeric(Accessor(count), GreaterThanOrEqual, DeclarationValue); full declaration spans lines 195-208
- docs/language/collection-types.md § Constraint Catalog — spec: a statically-literal mincount 1 discharges access safety

**Entry 2 — (c)**

- Derivation: guard conjunct normal-form-equal to the WP (F.count > 0) -> guard-match
- Capability tier: 1a
- Validity arguments: Guard normal-form match
- Decision procedure: Normal-form match of the WP (F.count > 0) against the row's guard conjuncts (a finite set). A conjunct normal-form-equal to F.count > 0 discharges; a semantically-equivalent but non-normal-form spelling (e.g. F.count >= 1) does not match under the stated normalization and is rejected (live-verified, this group) — a sound-but-unprovable band member, not a near-miss, since it does not weaken the guarantee.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Guard normal-form match — the WP has shape <RHS> <cmp> <bound>, licensed premise is a guard conjunct normal-form-equal to it
- docs/language/collection-types.md § Guard pattern — spec

**Entry 3 — (d)**

- Derivation: a rule stating F.count > 0 (or a bound entailing it) holds in the pre-state, and the write plan does not write F, so the fact is frame-preserved into the post-state the read observes
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Look up a rule establishing F.count > 0 (or an entailing bound) as holding in the pre-state (the inductive hypothesis), and confirm the write plan's actions do not write F (frame-preservation, a finite syntactic check over the plan's action list). Absent such a rule, or a plan that does write F without re-establishing the bound, fails the class.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Inductive hypothesis plus sign monotonicity — premise (d) supplies the pre-state fact; here the write plan does not touch the collection at all (frame-preserved), so no sign-monotonicity step is actually needed beyond identity, a simpler case than Family 1 Base B's subtraction shape
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — owner-ruling, 2026-07-20: false-proof hazard: a rule consumed as a premise can discharge an obligation nothing establishes or preserves; builtStatus for this class is never confirmed by a clean compile alone

### What the failing diagnostic must suggest

- For class (a): declare mincount 1 (or higher) on the collection field
  - docs/language/collection-types.md § Constraint Catalog — spec

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (d): restate <WP> as a rule holding in the pre-state, established at construction and preserved by every other write site of the collection
  - docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — owner-ruling, 2026-07-20

  - This suggestion is offered only where class (d) is structurally available (transition-row-action-operand, state-hook-action-operand — not construction-row-action-operand, which has no pre-state). HEAD does not consume a rule as a premise for this obligation today (live-verified for the list/.first representative, this group); where it is built, the false-proof hazard applies (Defect B).


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept CollectionEmptiness10

field HistoryStack as stack of integer
field Result as integer default 0

state Active initial

event Begin initial
event Read

on Begin
    -> set Result = 0

from Active on Read
    -> set Result = HistoryStack.peek
    -> no transition
```

Required outcome: reject, naming the missing premise classes (a), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*mincount-1* — arg-modifier

- Addition: mincount 1
- Premise classes: (a)
- Derivation: mincount 1 -> IntervalContainment (F.count >= 1 entails F.count > 0)
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified in the batch file witness-g13/discharge-a-mincount.precept (all twelve non-.at accessors of this group compiled together); this field's own diagnostic absence confirms the discharge.
- No structured `application` recorded: the addition modifies the collection field's own declaration (appending the mincount modifier), which fits neither of the schema's two application loci (row-guard is for a row's when clause; event-arg-declarations is for event-argument declarations, not field declarations). This is a schema-vocabulary gap for field-declaration-locus additions, not invented around here.

*guard-nf* — guard

- Addition: when HistoryStack.count > 0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the WP -> guard-match
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: appended to the guard of the `on Read` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified in the batch file witness-g13/discharge-c-guard.precept.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| mincount-1 | mincount 0 | mincount 0 does not establish F.count >= 1 — must still reject, same obligation | reject, naming the same obligation |
| guard-nf | when HistoryStack.count >= 0 | vacuously true for a non-negative count — carries no information, must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Evaluation-site coordinate: this cell's coordinates name transition-row-action-operand as the representative site (the group's shared setting: a transition row copying from each accessor into a scalar field). The identical obligation, discharge contract, and decision procedures generalize unchanged to construction-row-action-operand and state-hook-action-operand, per this group's disposition-map entries (all three carry disposition defined for every one of the fifteen accessors): class (a) and (c) discharge at all three sites (spot-verified live at construction-row-action-operand and state-hook-action-operand for the list/.first representative); class (d) is available at transition-row-action-operand and state-hook-action-operand only, because construction-row-action-operand has no pre-state to read from (docs/language/precept-language-spec.md § Stateless/stateful cross-validation).
- The (a) and (c) discharges and their near-misses were live-verified for this specific accessor (batch files listed in the discharge/near-miss provenance). The (d) rule-premise route is stated in the discharge contract per the shared derivation but was not independently witnessed for this accessor — see g13/list-first for the live-verified representative; the NumericProofRequirement catalog shape (src/Precept/Language/Types.cs) is identical across all twelve accessors in this group, so the same non-consumption finding is expected to generalize, but this is recorded as model-derived-from-representative, not independently confirmed.
- Respellability finding: `F.count >= 1` is semantically equivalent to F.count > 0 for an integer count and is NOT accepted by the guard-match decision procedure (live-verified) — a genuine sound-but-unprovable band member under this family's respellability verdict, distinct from the near-miss above (which is genuinely insufficient, not merely unrecognized).

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: Fault family row: the catalog-declared safety precondition at the evaluation site
- src/Precept/Language/Types.cs:216 — code: stack accessor .peek — NumericProofRequirement(SelfSubject(count), GreaterThan, 0, "Stack must be non-empty"); full declaration spans lines 216-221
- docs/language/collection-types.md § `stack` — spec: the accessor table naming the .count > 0 proof requirement and the mincount 1 static discharge
- docs/language/collection-types.md § Guard pattern — spec: F.count > 0 recognized in a when clause as sufficient proof for .peek/.min/.max/.first/.last/dequeue/pop

## g13/set-min — Set .min — reading a possibly-empty collection at a write site

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | set accessor .min |
| evaluation site category | transition-row-action-operand |
| type family | collection |

### What must be proven

Obligation: F.count > 0

Weakest precondition: F.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| F | the collection field the accessor reads |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a), (c), (d).

The read projects the receiver's own .count; no event argument represents a collection's cardinality, so class (b) never applies to this obligation regardless of site. Class (a) applies wherever the field declares mincount. Class (c) applies at every evaluation-site category in this group's region, all three carrying a guard position. Class (d) applies only where a pre-state exists — transition-row-action-operand and state-hook-action-operand, not construction-row-action-operand.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: a statically-literal mincount >= 1 on the collection field establishes F.count >= 1, which entails F.count > 0
- Strategy: IntervalContainment
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Look up a mincount modifier on the collection field's declaration (a finite, statically-literal set). mincount >= 1 discharges the class; no mincount modifier, or mincount 0, fails it (live-verified, this group).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Arg-bound interval arithmetic — governance enforces every declared constraint on a value before computation derives from it; extended here from an arg-declared bound to a field-declared mincount bound, both realized as the same ProofSatisfaction.Numeric catalog shape (src/Precept/Language/Modifiers.cs:195, spanning 195-208)
- src/Precept/Language/Modifiers.cs:195 — code: ModifierKind.Mincount ProofSatisfaction.Numeric(Accessor(count), GreaterThanOrEqual, DeclarationValue); full declaration spans lines 195-208
- docs/language/collection-types.md § Constraint Catalog — spec: a statically-literal mincount 1 discharges access safety

**Entry 2 — (c)**

- Derivation: guard conjunct normal-form-equal to the WP (F.count > 0) -> guard-match
- Capability tier: 1a
- Validity arguments: Guard normal-form match
- Decision procedure: Normal-form match of the WP (F.count > 0) against the row's guard conjuncts (a finite set). A conjunct normal-form-equal to F.count > 0 discharges; a semantically-equivalent but non-normal-form spelling (e.g. F.count >= 1) does not match under the stated normalization and is rejected (live-verified, this group) — a sound-but-unprovable band member, not a near-miss, since it does not weaken the guarantee.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Guard normal-form match — the WP has shape <RHS> <cmp> <bound>, licensed premise is a guard conjunct normal-form-equal to it
- docs/language/collection-types.md § Guard pattern — spec

**Entry 3 — (d)**

- Derivation: a rule stating F.count > 0 (or a bound entailing it) holds in the pre-state, and the write plan does not write F, so the fact is frame-preserved into the post-state the read observes
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Look up a rule establishing F.count > 0 (or an entailing bound) as holding in the pre-state (the inductive hypothesis), and confirm the write plan's actions do not write F (frame-preservation, a finite syntactic check over the plan's action list). Absent such a rule, or a plan that does write F without re-establishing the bound, fails the class.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Inductive hypothesis plus sign monotonicity — premise (d) supplies the pre-state fact; here the write plan does not touch the collection at all (frame-preserved), so no sign-monotonicity step is actually needed beyond identity, a simpler case than Family 1 Base B's subtraction shape
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — owner-ruling, 2026-07-20: false-proof hazard: a rule consumed as a premise can discharge an obligation nothing establishes or preserves; builtStatus for this class is never confirmed by a clean compile alone

### What the failing diagnostic must suggest

- For class (a): declare mincount 1 (or higher) on the collection field
  - docs/language/collection-types.md § Constraint Catalog — spec

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (d): restate <WP> as a rule holding in the pre-state, established at construction and preserved by every other write site of the collection
  - docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — owner-ruling, 2026-07-20

  - This suggestion is offered only where class (d) is structurally available (transition-row-action-operand, state-hook-action-operand — not construction-row-action-operand, which has no pre-state). HEAD does not consume a rule as a premise for this obligation today (live-verified for the list/.first representative, this group); where it is built, the false-proof hazard applies (Defect B).


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept CollectionEmptiness11

field Tags as set of integer
field Result as integer default 0

state Active initial

event Begin initial
event Read

on Begin
    -> set Result = 0

from Active on Read
    -> set Result = Tags.min
    -> no transition
```

Required outcome: reject, naming the missing premise classes (a), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*mincount-1* — arg-modifier

- Addition: mincount 1
- Premise classes: (a)
- Derivation: mincount 1 -> IntervalContainment (F.count >= 1 entails F.count > 0)
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified in the batch file witness-g13/discharge-a-mincount.precept (all twelve non-.at accessors of this group compiled together); this field's own diagnostic absence confirms the discharge.
- No structured `application` recorded: the addition modifies the collection field's own declaration (appending the mincount modifier), which fits neither of the schema's two application loci (row-guard is for a row's when clause; event-arg-declarations is for event-argument declarations, not field declarations). This is a schema-vocabulary gap for field-declaration-locus additions, not invented around here.

*guard-nf* — guard

- Addition: when Tags.count > 0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the WP -> guard-match
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: appended to the guard of the `on Read` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified in the batch file witness-g13/discharge-c-guard.precept.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| mincount-1 | mincount 0 | mincount 0 does not establish F.count >= 1 — must still reject, same obligation | reject, naming the same obligation |
| guard-nf | when Tags.count >= 0 | vacuously true for a non-negative count — carries no information, must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Evaluation-site coordinate: this cell's coordinates name transition-row-action-operand as the representative site (the group's shared setting: a transition row copying from each accessor into a scalar field). The identical obligation, discharge contract, and decision procedures generalize unchanged to construction-row-action-operand and state-hook-action-operand, per this group's disposition-map entries (all three carry disposition defined for every one of the fifteen accessors): class (a) and (c) discharge at all three sites (spot-verified live at construction-row-action-operand and state-hook-action-operand for the list/.first representative); class (d) is available at transition-row-action-operand and state-hook-action-operand only, because construction-row-action-operand has no pre-state to read from (docs/language/precept-language-spec.md § Stateless/stateful cross-validation).
- The (a) and (c) discharges and their near-misses were live-verified for this specific accessor (batch files listed in the discharge/near-miss provenance). The (d) rule-premise route is stated in the discharge contract per the shared derivation but was not independently witnessed for this accessor — see g13/list-first for the live-verified representative; the NumericProofRequirement catalog shape (src/Precept/Language/Types.cs) is identical across all twelve accessors in this group, so the same non-consumption finding is expected to generalize, but this is recorded as model-derived-from-representative, not independently confirmed.
- Respellability finding: `F.count >= 1` is semantically equivalent to F.count > 0 for an integer count and is NOT accepted by the guard-match decision procedure (live-verified) — a genuine sound-but-unprovable band member under this family's respellability verdict, distinct from the near-miss above (which is genuinely insufficient, not merely unrecognized).

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: Fault family row: the catalog-declared safety precondition at the evaluation site
- src/Precept/Language/Types.cs:186 — code: set accessor .min — NumericProofRequirement(SelfSubject(count), GreaterThan, 0, "Collection must be non-empty"); full declaration spans lines 186-192
- docs/language/collection-types.md § `set` — spec: the accessor table naming the .count > 0 proof requirement and the mincount 1 static discharge
- docs/language/collection-types.md § Guard pattern — spec: F.count > 0 recognized in a when clause as sufficient proof for .peek/.min/.max/.first/.last/dequeue/pop

## g13/set-max — Set .max — reading a possibly-empty collection at a write site

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | set accessor .max |
| evaluation site category | transition-row-action-operand |
| type family | collection |

### What must be proven

Obligation: F.count > 0

Weakest precondition: F.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| F | the collection field the accessor reads |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a), (c), (d).

The read projects the receiver's own .count; no event argument represents a collection's cardinality, so class (b) never applies to this obligation regardless of site. Class (a) applies wherever the field declares mincount. Class (c) applies at every evaluation-site category in this group's region, all three carrying a guard position. Class (d) applies only where a pre-state exists — transition-row-action-operand and state-hook-action-operand, not construction-row-action-operand.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: a statically-literal mincount >= 1 on the collection field establishes F.count >= 1, which entails F.count > 0
- Strategy: IntervalContainment
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Look up a mincount modifier on the collection field's declaration (a finite, statically-literal set). mincount >= 1 discharges the class; no mincount modifier, or mincount 0, fails it (live-verified, this group).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Arg-bound interval arithmetic — governance enforces every declared constraint on a value before computation derives from it; extended here from an arg-declared bound to a field-declared mincount bound, both realized as the same ProofSatisfaction.Numeric catalog shape (src/Precept/Language/Modifiers.cs:195, spanning 195-208)
- src/Precept/Language/Modifiers.cs:195 — code: ModifierKind.Mincount ProofSatisfaction.Numeric(Accessor(count), GreaterThanOrEqual, DeclarationValue); full declaration spans lines 195-208
- docs/language/collection-types.md § Constraint Catalog — spec: a statically-literal mincount 1 discharges access safety

**Entry 2 — (c)**

- Derivation: guard conjunct normal-form-equal to the WP (F.count > 0) -> guard-match
- Capability tier: 1a
- Validity arguments: Guard normal-form match
- Decision procedure: Normal-form match of the WP (F.count > 0) against the row's guard conjuncts (a finite set). A conjunct normal-form-equal to F.count > 0 discharges; a semantically-equivalent but non-normal-form spelling (e.g. F.count >= 1) does not match under the stated normalization and is rejected (live-verified, this group) — a sound-but-unprovable band member, not a near-miss, since it does not weaken the guarantee.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Guard normal-form match — the WP has shape <RHS> <cmp> <bound>, licensed premise is a guard conjunct normal-form-equal to it
- docs/language/collection-types.md § Guard pattern — spec

**Entry 3 — (d)**

- Derivation: a rule stating F.count > 0 (or a bound entailing it) holds in the pre-state, and the write plan does not write F, so the fact is frame-preserved into the post-state the read observes
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Look up a rule establishing F.count > 0 (or an entailing bound) as holding in the pre-state (the inductive hypothesis), and confirm the write plan's actions do not write F (frame-preservation, a finite syntactic check over the plan's action list). Absent such a rule, or a plan that does write F without re-establishing the bound, fails the class.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Inductive hypothesis plus sign monotonicity — premise (d) supplies the pre-state fact; here the write plan does not touch the collection at all (frame-preserved), so no sign-monotonicity step is actually needed beyond identity, a simpler case than Family 1 Base B's subtraction shape
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — owner-ruling, 2026-07-20: false-proof hazard: a rule consumed as a premise can discharge an obligation nothing establishes or preserves; builtStatus for this class is never confirmed by a clean compile alone

### What the failing diagnostic must suggest

- For class (a): declare mincount 1 (or higher) on the collection field
  - docs/language/collection-types.md § Constraint Catalog — spec

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (d): restate <WP> as a rule holding in the pre-state, established at construction and preserved by every other write site of the collection
  - docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — owner-ruling, 2026-07-20

  - This suggestion is offered only where class (d) is structurally available (transition-row-action-operand, state-hook-action-operand — not construction-row-action-operand, which has no pre-state). HEAD does not consume a rule as a premise for this obligation today (live-verified for the list/.first representative, this group); where it is built, the false-proof hazard applies (Defect B).


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept CollectionEmptiness12

field Tags as set of integer
field Result as integer default 0

state Active initial

event Begin initial
event Read

on Begin
    -> set Result = 0

from Active on Read
    -> set Result = Tags.max
    -> no transition
```

Required outcome: reject, naming the missing premise classes (a), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*mincount-1* — arg-modifier

- Addition: mincount 1
- Premise classes: (a)
- Derivation: mincount 1 -> IntervalContainment (F.count >= 1 entails F.count > 0)
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified in the batch file witness-g13/discharge-a-mincount.precept (all twelve non-.at accessors of this group compiled together); this field's own diagnostic absence confirms the discharge.
- No structured `application` recorded: the addition modifies the collection field's own declaration (appending the mincount modifier), which fits neither of the schema's two application loci (row-guard is for a row's when clause; event-arg-declarations is for event-argument declarations, not field declarations). This is a schema-vocabulary gap for field-declaration-locus additions, not invented around here.

*guard-nf* — guard

- Addition: when Tags.count > 0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the WP -> guard-match
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: appended to the guard of the `on Read` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified in the batch file witness-g13/discharge-c-guard.precept.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| mincount-1 | mincount 0 | mincount 0 does not establish F.count >= 1 — must still reject, same obligation | reject, naming the same obligation |
| guard-nf | when Tags.count >= 0 | vacuously true for a non-negative count — carries no information, must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Evaluation-site coordinate: this cell's coordinates name transition-row-action-operand as the representative site (the group's shared setting: a transition row copying from each accessor into a scalar field). The identical obligation, discharge contract, and decision procedures generalize unchanged to construction-row-action-operand and state-hook-action-operand, per this group's disposition-map entries (all three carry disposition defined for every one of the fifteen accessors): class (a) and (c) discharge at all three sites (spot-verified live at construction-row-action-operand and state-hook-action-operand for the list/.first representative); class (d) is available at transition-row-action-operand and state-hook-action-operand only, because construction-row-action-operand has no pre-state to read from (docs/language/precept-language-spec.md § Stateless/stateful cross-validation).
- The (a) and (c) discharges and their near-misses were live-verified for this specific accessor (batch files listed in the discharge/near-miss provenance). The (d) rule-premise route is stated in the discharge contract per the shared derivation but was not independently witnessed for this accessor — see g13/list-first for the live-verified representative; the NumericProofRequirement catalog shape (src/Precept/Language/Types.cs) is identical across all twelve accessors in this group, so the same non-consumption finding is expected to generalize, but this is recorded as model-derived-from-representative, not independently confirmed.
- Respellability finding: `F.count >= 1` is semantically equivalent to F.count > 0 for an integer count and is NOT accepted by the guard-match decision procedure (live-verified) — a genuine sound-but-unprovable band member under this family's respellability verdict, distinct from the near-miss above (which is genuinely insufficient, not merely unrecognized).

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: Fault family row: the catalog-declared safety precondition at the evaluation site
- src/Precept/Language/Types.cs:193 — code: set accessor .max — NumericProofRequirement(SelfSubject(count), GreaterThan, 0, "Collection must be non-empty"); full declaration spans lines 193-199
- docs/language/collection-types.md § `set` — spec: the accessor table naming the .count > 0 proof requirement and the mincount 1 static discharge
- docs/language/collection-types.md § Guard pattern — spec: F.count > 0 recognized in a when clause as sufficient proof for .peek/.min/.max/.first/.last/dequeue/pop

## g13/list-at — List .at(N) — non-empty half of the two-requirement accessor, at a write site

Disposition: **open**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § The cell — matrix: the Base bullet's base-minimality requirement
- src/Precept/Language/Types.cs:309 — code
- src/Precept/Language/Types.cs:315 — code

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | list accessor .at(integer) |
| evaluation site category | transition-row-action-operand |
| type family | collection |

### What must be proven

Obligation: F.count > 0

Weakest precondition: F.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| F | the collection field the accessor reads |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a), (c), (d).

Same as the group's other twelve accessors — class (b) never applies (no arg represents cardinality); (a)/(c) apply at every site in this group's region; (d) applies only where a pre-state exists.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: a statically-literal mincount >= 1 on the collection field establishes F.count >= 1, which entails F.count > 0
- Strategy: IntervalContainment
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Look up a mincount modifier on the collection field's declaration; mincount >= 1 discharges the non-empty half (live-verified: the generic-subject diagnostic disappears). The sibling index-bounds half is unaffected and is group 17's concern.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Arg-bound interval arithmetic — governance enforces every declared constraint on a value before computation derives from it; extended here from an arg-declared bound to a field-declared mincount bound, both realized as the same ProofSatisfaction.Numeric catalog shape (src/Precept/Language/Modifiers.cs:195, spanning 195-208)

**Entry 2 — (c)**

- Derivation: guard conjunct normal-form-equal to the WP (F.count > 0) -> guard-match
- Validity arguments: Guard normal-form match
- Decision procedure: Normal-form match of F.count > 0 against the row's guard conjuncts discharges the non-empty half (live-verified). The sibling index-bounds half is unaffected.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Guard normal-form match — the WP has shape <RHS> <cmp> <bound>, licensed premise is a guard conjunct normal-form-equal to it

**Entry 3 — (d)**

- Derivation: a rule stating F.count > 0 holds in the pre-state, frame-preserved into the post-state the read observes
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Same procedure as the other twelve accessors in this group (not independently re-verified for .at specifically).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Inductive hypothesis plus sign monotonicity — premise (d) supplies the pre-state fact; here the write plan does not touch the collection at all (frame-preserved), so no sign-monotonicity step is actually needed beyond identity, a simpler case than Family 1 Base B's subtraction shape
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — owner-ruling, 2026-07-20

### What the failing diagnostic must suggest

- For class (a): declare mincount 1 (or higher) on the collection field
  - docs/language/collection-types.md § Constraint Catalog — spec

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (d): restate <WP> as a rule holding in the pre-state, established at construction and preserved by every other write site of the collection
  - docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — owner-ruling, 2026-07-20

  - Same hazard caveat as the other twelve accessors in this group.


### The worked examples

**Base.** None recorded — see this cell's notes for why.

**Discharge additions — what makes the base compile.**

*mincount-1* — arg-modifier

- Addition: mincount 1
- Premise classes: (a)
- Derivation: mincount 1 -> IntervalContainment (F.count >= 1 entails F.count > 0)
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified: this addition clears the generic-subject IndexBoundsGuard diagnostic (this cell's target). The sibling index-named diagnostic (group 17's obligation) remains — expected, not a violation of this discharge's own sufficiency, since the index itself is still unconstrained. The overall program therefore still fails to compile clean; this discharge is witnessed by the diagnostic-count/content change, not by a standalone clean compile, given the coupling described in this cell's notes.

*guard-nf* — guard

- Addition: when ItemList.count > 0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the WP -> guard-match
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified: this addition clears the generic-subject diagnostic; the sibling index-named diagnostic (group 17's) remains present, for the same reason as the mincount discharge above.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| mincount-1 | mincount 0 | mincount 0 does not establish F.count >= 1 — the generic-subject diagnostic still fires, same obligation | reject, naming the same obligation |
| guard-nf | when ItemList.count >= 0 | vacuously true — carries no information, the generic-subject diagnostic still fires, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- list accessor .at(integer) mints TWO catalog ProofRequirement entries at the same evaluation site (src/Precept/Language/Types.cs:309 and :315): a Numeric requirement (non-empty, this group's target — 'F.count > 0') and an IndexBounds requirement (group 17's target — '0 <= N < F.count'). This group owns the non-empty half only; group 17 owns the index-bounds half.
- Live-verified (Precept.MatrixTools, commit e1a14d91, 2026-07-21): an unconstrained `.at` call — no mincount, no guard, index from an unconstrained event arg — raises TWO IndexBoundsGuard diagnostics at the same call: one with a generic 'index' subject (this group's non-empty obligation) and one naming the actual index expression (group 17's obligation). Adding `mincount 1` on the field, or a `when F.count > 0` guard, removes exactly the generic-subject diagnostic and leaves the index-named one in place — confirming the generic diagnostic is this cell's target and that class (a)/(c) discharge it exactly as for the other twelve accessors in this group. A full compound guard (`when Idx >= 0 and Idx < F.count`) was also tried and does NOT clear the index-named diagnostic — that discharge route is unresolved-today, matching group 17's own missing-decision-procedure note.
- Base-minimality tension (reported honestly, not resolved): because both ProofRequirement entries mint at the same call and HEAD has no known way to discharge the index-bounds half independently of the non-empty half, every constructible base for this obligation co-mints group 17's diagnostic alongside this group's — live-verified, no combination of premises produced a base with only the target diagnostic present. The matrix's base-minimality clause ('the base rejects only for the target obligation... no other diagnostics') does not state whether a disclosed, expected companion diagnostic from a different cell at the same site is 'other' in the sense the rule forbids, or whether base-minimality simply does not apply at a site minting two obligations until a written decision procedure decouples them. No base witness is recorded here rather than falsely asserting noOtherDiagnostics.
- The discharge contract (classes a/c/d) and decision procedures are otherwise identical to the other twelve accessors in this group and are stated below for the non-empty half specifically; only the witness base is affected by the coupling above.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § The cell — matrix: item 4, the Base bullet: base-minimality requires a base that rejects for the target obligation only
- src/Precept/Language/Types.cs:309 — code: the non-empty NumericProofRequirement half of .at's ProofRequirements array; spans lines 309-314
- src/Precept/Language/Types.cs:315 — code: the sibling IndexBoundsProofRequirement — group 17's obligation, minted at the same accessor call; spans lines 315-319
- docs/language/collection-types.md § Access proof obligations — spec: `.at(N)` obligation stated as the compound N >= 0 and N < F.count under a single UnguardedCollectionAccess label — at odds with the catalog's two separate ProofRequirement entries; not resolved here

## g13/log-at — Log .at(N) — non-empty half of the two-requirement accessor, at a write site

Disposition: **open**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § The cell — matrix: the Base bullet's base-minimality requirement
- src/Precept/Language/Types.cs:239 — code
- src/Precept/Language/Types.cs:245 — code

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | log accessor .at(integer) |
| evaluation site category | transition-row-action-operand |
| type family | collection |

### What must be proven

Obligation: F.count > 0

Weakest precondition: F.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| F | the collection field the accessor reads |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a), (c), (d).

Same as the group's other twelve accessors — class (b) never applies (no arg represents cardinality); (a)/(c) apply at every site in this group's region; (d) applies only where a pre-state exists.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: a statically-literal mincount >= 1 on the collection field establishes F.count >= 1, which entails F.count > 0
- Strategy: IntervalContainment
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Look up a mincount modifier on the collection field's declaration; mincount >= 1 discharges the non-empty half (live-verified: the generic-subject diagnostic disappears). The sibling index-bounds half is unaffected and is group 17's concern.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Arg-bound interval arithmetic — governance enforces every declared constraint on a value before computation derives from it; extended here from an arg-declared bound to a field-declared mincount bound, both realized as the same ProofSatisfaction.Numeric catalog shape (src/Precept/Language/Modifiers.cs:195, spanning 195-208)

**Entry 2 — (c)**

- Derivation: guard conjunct normal-form-equal to the WP (F.count > 0) -> guard-match
- Validity arguments: Guard normal-form match
- Decision procedure: Normal-form match of F.count > 0 against the row's guard conjuncts discharges the non-empty half (live-verified). The sibling index-bounds half is unaffected.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Guard normal-form match — the WP has shape <RHS> <cmp> <bound>, licensed premise is a guard conjunct normal-form-equal to it

**Entry 3 — (d)**

- Derivation: a rule stating F.count > 0 holds in the pre-state, frame-preserved into the post-state the read observes
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Same procedure as the other twelve accessors in this group (not independently re-verified for .at specifically).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Inductive hypothesis plus sign monotonicity — premise (d) supplies the pre-state fact; here the write plan does not touch the collection at all (frame-preserved), so no sign-monotonicity step is actually needed beyond identity, a simpler case than Family 1 Base B's subtraction shape
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — owner-ruling, 2026-07-20

### What the failing diagnostic must suggest

- For class (a): declare mincount 1 (or higher) on the collection field
  - docs/language/collection-types.md § Constraint Catalog — spec

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (d): restate <WP> as a rule holding in the pre-state, established at construction and preserved by every other write site of the collection
  - docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — owner-ruling, 2026-07-20

  - Same hazard caveat as the other twelve accessors in this group.


### The worked examples

**Base.** None recorded — see this cell's notes for why.

**Discharge additions — what makes the base compile.**

*mincount-1* — arg-modifier

- Addition: mincount 1
- Premise classes: (a)
- Derivation: mincount 1 -> IntervalContainment (F.count >= 1 entails F.count > 0)
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified: this addition clears the generic-subject IndexBoundsGuard diagnostic (this cell's target). The sibling index-named diagnostic (group 17's obligation) remains — expected, not a violation of this discharge's own sufficiency, since the index itself is still unconstrained. The overall program therefore still fails to compile clean; this discharge is witnessed by the diagnostic-count/content change, not by a standalone clean compile, given the coupling described in this cell's notes.

*guard-nf* — guard

- Addition: when EntryLog.count > 0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the WP -> guard-match
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified: this addition clears the generic-subject diagnostic; the sibling index-named diagnostic (group 17's) remains present, for the same reason as the mincount discharge above.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| mincount-1 | mincount 0 | mincount 0 does not establish F.count >= 1 — the generic-subject diagnostic still fires, same obligation | reject, naming the same obligation |
| guard-nf | when EntryLog.count >= 0 | vacuously true — carries no information, the generic-subject diagnostic still fires, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- log accessor .at(integer) mints TWO catalog ProofRequirement entries at the same evaluation site (src/Precept/Language/Types.cs:239 and :245): a Numeric requirement (non-empty, this group's target — 'F.count > 0') and an IndexBounds requirement (group 17's target — '0 <= N < F.count'). This group owns the non-empty half only; group 17 owns the index-bounds half.
- Live-verified (Precept.MatrixTools, commit e1a14d91, 2026-07-21): an unconstrained `.at` call — no mincount, no guard, index from an unconstrained event arg — raises TWO IndexBoundsGuard diagnostics at the same call: one with a generic 'index' subject (this group's non-empty obligation) and one naming the actual index expression (group 17's obligation). Adding `mincount 1` on the field, or a `when F.count > 0` guard, removes exactly the generic-subject diagnostic and leaves the index-named one in place — confirming the generic diagnostic is this cell's target and that class (a)/(c) discharge it exactly as for the other twelve accessors in this group. A full compound guard (`when Idx >= 0 and Idx < F.count`) was also tried and does NOT clear the index-named diagnostic — that discharge route is unresolved-today, matching group 17's own missing-decision-procedure note.
- Base-minimality tension (reported honestly, not resolved): because both ProofRequirement entries mint at the same call and HEAD has no known way to discharge the index-bounds half independently of the non-empty half, every constructible base for this obligation co-mints group 17's diagnostic alongside this group's — live-verified, no combination of premises produced a base with only the target diagnostic present. The matrix's base-minimality clause ('the base rejects only for the target obligation... no other diagnostics') does not state whether a disclosed, expected companion diagnostic from a different cell at the same site is 'other' in the sense the rule forbids, or whether base-minimality simply does not apply at a site minting two obligations until a written decision procedure decouples them. No base witness is recorded here rather than falsely asserting noOtherDiagnostics.
- The discharge contract (classes a/c/d) and decision procedures are otherwise identical to the other twelve accessors in this group and are stated below for the non-empty half specifically; only the witness base is affected by the coupling above.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § The cell — matrix: item 4, the Base bullet: base-minimality requires a base that rejects for the target obligation only
- src/Precept/Language/Types.cs:239 — code: the non-empty NumericProofRequirement half of .at's ProofRequirements array; spans lines 239-244
- src/Precept/Language/Types.cs:245 — code: the sibling IndexBoundsProofRequirement — group 17's obligation, minted at the same accessor call; spans lines 245-249
- docs/language/collection-types.md § Access proof obligations — spec: `.at(N)` obligation stated as the compound N >= 0 and N < F.count under a single UnguardedCollectionAccess label — at odds with the catalog's two separate ProofRequirement entries; not resolved here

## g13/logby-at — Log By .at(N) — non-empty half of the two-requirement accessor, at a write site

Disposition: **open**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § The cell — matrix: the Base bullet's base-minimality requirement
- src/Precept/Language/Types.cs:267 — code
- src/Precept/Language/Types.cs:273 — code

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | log by accessor .at(integer) |
| evaluation site category | transition-row-action-operand |
| type family | collection |

### What must be proven

Obligation: F.count > 0

Weakest precondition: F.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| F | the collection field the accessor reads |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a), (c), (d).

Same as the group's other twelve accessors — class (b) never applies (no arg represents cardinality); (a)/(c) apply at every site in this group's region; (d) applies only where a pre-state exists.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: a statically-literal mincount >= 1 on the collection field establishes F.count >= 1, which entails F.count > 0
- Strategy: IntervalContainment
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Look up a mincount modifier on the collection field's declaration; mincount >= 1 discharges the non-empty half (live-verified: the generic-subject diagnostic disappears). The sibling index-bounds half is unaffected and is group 17's concern.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Arg-bound interval arithmetic — governance enforces every declared constraint on a value before computation derives from it; extended here from an arg-declared bound to a field-declared mincount bound, both realized as the same ProofSatisfaction.Numeric catalog shape (src/Precept/Language/Modifiers.cs:195, spanning 195-208)

**Entry 2 — (c)**

- Derivation: guard conjunct normal-form-equal to the WP (F.count > 0) -> guard-match
- Validity arguments: Guard normal-form match
- Decision procedure: Normal-form match of F.count > 0 against the row's guard conjuncts discharges the non-empty half (live-verified). The sibling index-bounds half is unaffected.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Guard normal-form match — the WP has shape <RHS> <cmp> <bound>, licensed premise is a guard conjunct normal-form-equal to it

**Entry 3 — (d)**

- Derivation: a rule stating F.count > 0 holds in the pre-state, frame-preserved into the post-state the read observes
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Same procedure as the other twelve accessors in this group (not independently re-verified for .at specifically).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Inductive hypothesis plus sign monotonicity — premise (d) supplies the pre-state fact; here the write plan does not touch the collection at all (frame-preserved), so no sign-monotonicity step is actually needed beyond identity, a simpler case than Family 1 Base B's subtraction shape
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — owner-ruling, 2026-07-20

### What the failing diagnostic must suggest

- For class (a): declare mincount 1 (or higher) on the collection field
  - docs/language/collection-types.md § Constraint Catalog — spec

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (d): restate <WP> as a rule holding in the pre-state, established at construction and preserved by every other write site of the collection
  - docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — owner-ruling, 2026-07-20

  - Same hazard caveat as the other twelve accessors in this group.


### The worked examples

**Base.** None recorded — see this cell's notes for why.

**Discharge additions — what makes the base compile.**

*mincount-1* — arg-modifier

- Addition: mincount 1
- Premise classes: (a)
- Derivation: mincount 1 -> IntervalContainment (F.count >= 1 entails F.count > 0)
- Strategy: IntervalContainment
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified: this addition clears the generic-subject IndexBoundsGuard diagnostic (this cell's target). The sibling index-named diagnostic (group 17's obligation) remains — expected, not a violation of this discharge's own sufficiency, since the index itself is still unconstrained. The overall program therefore still fails to compile clean; this discharge is witnessed by the diagnostic-count/content change, not by a standalone clean compile, given the coupling described in this cell's notes.

*guard-nf* — guard

- Addition: when KeyedLog.count > 0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the WP -> guard-match
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified: this addition clears the generic-subject diagnostic; the sibling index-named diagnostic (group 17's) remains present, for the same reason as the mincount discharge above.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| mincount-1 | mincount 0 | mincount 0 does not establish F.count >= 1 — the generic-subject diagnostic still fires, same obligation | reject, naming the same obligation |
| guard-nf | when KeyedLog.count >= 0 | vacuously true — carries no information, the generic-subject diagnostic still fires, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- log by accessor .at(integer) mints TWO catalog ProofRequirement entries at the same evaluation site (src/Precept/Language/Types.cs:267 and :273): a Numeric requirement (non-empty, this group's target — 'F.count > 0') and an IndexBounds requirement (group 17's target — '0 <= N < F.count'). This group owns the non-empty half only; group 17 owns the index-bounds half.
- Live-verified (Precept.MatrixTools, commit e1a14d91, 2026-07-21): an unconstrained `.at` call — no mincount, no guard, index from an unconstrained event arg — raises TWO IndexBoundsGuard diagnostics at the same call: one with a generic 'index' subject (this group's non-empty obligation) and one naming the actual index expression (group 17's obligation). Adding `mincount 1` on the field, or a `when F.count > 0` guard, removes exactly the generic-subject diagnostic and leaves the index-named one in place — confirming the generic diagnostic is this cell's target and that class (a)/(c) discharge it exactly as for the other twelve accessors in this group. A full compound guard (`when Idx >= 0 and Idx < F.count`) was also tried and does NOT clear the index-named diagnostic — that discharge route is unresolved-today, matching group 17's own missing-decision-procedure note.
- Base-minimality tension (reported honestly, not resolved): because both ProofRequirement entries mint at the same call and HEAD has no known way to discharge the index-bounds half independently of the non-empty half, every constructible base for this obligation co-mints group 17's diagnostic alongside this group's — live-verified, no combination of premises produced a base with only the target diagnostic present. The matrix's base-minimality clause ('the base rejects only for the target obligation... no other diagnostics') does not state whether a disclosed, expected companion diagnostic from a different cell at the same site is 'other' in the sense the rule forbids, or whether base-minimality simply does not apply at a site minting two obligations until a written decision procedure decouples them. No base witness is recorded here rather than falsely asserting noOtherDiagnostics.
- The discharge contract (classes a/c/d) and decision procedures are otherwise identical to the other twelve accessors in this group and are stated below for the non-empty half specifically; only the witness base is affected by the coupling above.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § The cell — matrix: item 4, the Base bullet: base-minimality requires a base that rejects for the target obligation only
- src/Precept/Language/Types.cs:267 — code: the non-empty NumericProofRequirement half of .at's ProofRequirements array; spans lines 267-272
- src/Precept/Language/Types.cs:273 — code: the sibling IndexBoundsProofRequirement — group 17's obligation, minted at the same accessor call; spans lines 273-277
- docs/language/collection-types.md § Access proof obligations — spec: `.at(N)` obligation stated as the compound N >= 0 and N < F.count under a single UnguardedCollectionAccess label — at odds with the catalog's two separate ProofRequirement entries; not resolved here

