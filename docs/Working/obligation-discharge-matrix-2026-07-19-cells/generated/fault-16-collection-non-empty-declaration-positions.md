<!--
GENERATED FILE — do not hand-edit.
Source: fault-16-collection-non-empty-declaration-positions.cells.json
Regenerate: dotnet run --project tools/Precept.MatrixTools -- render-cells docs/Working/obligation-discharge-matrix-2026-07-19-cells
Hand edits will be overwritten. Edit the .cells.json and regenerate.
-->

# Group 16 — reading a possibly-empty collection in a declaration-position value expression

Family id: fault-16-collection-non-empty-declaration-positions
Case shape: fault
Definition version: obligation-discharge-matrix-2026-07-19 rev 7 (2026-07-20)

**Where this family's content comes from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § The cell — matrix: the cell's four-part definition (obligation schema, discharge contract, suggestions, witnesses) that this file instantiates for the fault family
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row: catalog-declared safety precondition at the evaluation site; family x operation kind x evaluation-site category x type; premise classes (b)/(c)/(a) per catalog ProofSatisfactions
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: premise classes, discharge mechanisms (constant folding; interval derivation over declared bounds), normalization, sound-but-unprovable band, respellability
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Literal constant-fold for defaults; Establishment over defaults (the pre-configuration rule) — the two written arguments this file's discharges cite
- docs/Working/obligation-discharge-matrix-2026-07-19.md:141 — matrix: Axis 5, Establishment pre-configuration: establishment WP is taken over the default configuration for fields the initial event does not write
- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: the fault-prevention obligation family
- docs/Working/what-i-want-2026-07-16.md:19 — want-doc: the four premise classes
- src/Precept/Language/Types.cs:297 — catalog: list accessor .first — NumericProofRequirement(SelfSubject(count), GreaterThan, 0), message 'List must be non-empty'
- src/Precept/Language/Types.cs:233 — catalog: log accessor .last — NumericProofRequirement(SelfSubject(count), GreaterThan, 0), message 'Log must be non-empty'
- src/Precept/Language/Modifiers.cs:195 — catalog: mincount — ProofSatisfaction.Numeric(Accessor(count), GreaterThanOrEqual, DeclarationValue()), 'Enforced on every assignment'
- docs/language/precept-language-spec.md:2036 — spec: construction builds the hollow version with defaults applied
- docs/language/precept-language-spec.md § Field Modifiers — spec: field-modifier value expressions have a rule condition's scope and may name any field regardless of declaration order; constraint modifiers desugar to rules

## Respellability — the family verdict

Can every safe program these rules reject be rewritten into a form they accept? **yes**

Evidence: model-derived
Corpus measurement: pending

**Notes on the family verdict**

- This verdict covers the family's defined cells only (the five accessor/List/first cells). The five accessor/Log/last cells in this file are disposition: open and carry no respellability resolution of their own — the missing validity argument they cite (establishment via an initial event's write plan) is exactly what would need to be settled before a verdict over log-typed collections could be stated honestly.
- Band member found while authoring this file (not previously stated in the matrix): a collection populated only through an initial event's write plan (e.g. an 'append' action), then read at one of this group's non-default declaration positions. This file's discharge contract licenses only a literal, non-empty declared default, so that spelling sits outside what is licensed here even though it is sound — its business intent (a collection guaranteed non-empty from construction onward) respells into a literal default wherever the collection type admits one (list). Recorded at cell g16/list-first/field-modifier; the other three non-default List cells state 'family-inherited' and note the same phenomenon without repeating the example.

**Sources for this verdict**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Respellable bullet — verdict stated once per contract family, a cell may override with a more specific verdict

## Premise classes — the legend

Where a proof may get its facts (want :19, :143): (a) field modifiers, (b) arg constraints, (c) the handler's guard, (d) all rules holding in the pre-state (the inductive hypothesis), (e) evaluating the applicable rule set at an editable-field write — (e)'s status as a genuinely fifth class vs (a) generalized is an open owner item.

## g16/list-first/field-default — List .first read inside a field default clause

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc
- docs/Working/obligation-discharge-matrix-2026-07-19.md:141 — matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | list accessor .first (NumericProofRequirement: count > 0) |
| evaluation site category | field-default-value-expression |
| type family | collection |

### What must be proven

Obligation: Milestones.count > 0

Weakest precondition: Milestones.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Milestones | the list-typed collection field the .first accessor reads, declared above the reading field |
| count | the catalog's built-in collection-count accessor (CollectionCountAccessor, Types.cs:172) |
| 0 | the catalog-declared non-emptiness bound (NumericProofRequirement, GreaterThan, 0m) |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a).

A default clause has no guard and reads no event args, so classes (b) and (c) have no textual attachment point. Its scope is fields declared above it only (a forward reference is impossible, not merely disallowed — Field Modifiers), so the only fact available is a declared-above field's own modifier or literal default: class (a). This group's authoring note records that class (d) is not consumed anywhere in this group's region.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: constant-fold of the accessor's NumericProofRequirement (count > 0) over the default configuration: Milestones's own declared literal default fixes its element count directly, exactly as the standing establishment obligation folds a rule's bound over a literal default.
- Validity arguments: Literal constant-fold for defaults; Establishment over defaults
- Decision procedure: Constant-fold the accessor's count-non-empty precondition over the default environment, using the same fold Witness Family 1's standing establishment obligation uses. Milestones's default clause must resolve to a list literal (spec:1763, ListLiteralOutsideDefault is the only route to a non-empty list value before any event fires); the fold computes the literal's element count directly. A fold that evaluates to zero, or cannot complete (no literal default, or an empty literal), leaves the obligation undischarged: reject.

- docs/language/precept-language-spec.md:664 — spec: list literal syntax: [1, 2, 3]

### What the failing diagnostic must suggest

- For class (a): give <Collection> a literal, non-empty declared default so <WP> folds true
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept ListFirstDefault
field Milestones as list of decimal
field FirstMilestone as decimal default Milestones.first
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools (dotnet CLI, release build), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*literal-default* — other

- Addition: default [1.0, 2.0]
- Premise classes: (a)
- Derivation: constant fold: default [1.0, 2.0] gives Milestones.count == 2, so Milestones.count > 0 folds true
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (field-default-value-expression does not mint the accessor's fault obligation at HEAD: both the bare base and the literal-default addition compile with only a FieldNeverSet warning, no fault diagnostic either way. Measured live 2026-07-21 at commit e1a14d91.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (dotnet CLI, release build), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- A separate live check confirms the accessor obligation genuinely does mint at other sites (a transition-row action-operand: 'set FirstMilestone = Milestones.first' with Milestones empty raises [Error] UnguardedCollectionAccess), so the absence of any diagnostic at the field-default position is the site's own gap, not a syntax mistake in this witness.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| literal-default | mincount 1 | Milestones's own declared default stays the implicit empty list; mincount 1 desugars to rule Milestones.count >= 1 (Field Modifiers), and that rule's own establishment (folded over the same empty default) does not hold, so the fact is unearned even before considering whether class (a) may be consumed this way. Milestones's actual value is unchanged from the base, so the accessor's fold still evaluates over an empty collection: same obligation, must still reject. | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

- No sound-but-unprovable band member exists for this cell specifically: the only way to make a list non-empty before any event fires is a literal default, and that is exactly what this cell's contract licenses. The family's band member (an initial-event-populated collection) cannot arise here at all, because a field-default clause evaluates strictly before any event runs (precept-language-spec.md:2036).

**What the sources leave unstated or ambiguous here**

- The authoring notes for this group flag a derivation subtlety specific to field-default cells: is the base 'merely unproven' or 'provably violated by the default configuration'? Here it is the latter — Milestones's implicit default is the empty list, so the fold Establishment-over-defaults commits to computes Milestones.count == 0, and 0 > 0 is false. The obligation is provably violated by the default configuration, not merely undischarged.
- All witnesses carry a FieldNeverSet structural warning (this is a fully event-less precept); this is orthogonal to the target obligation and present uniformly across base, discharge, and near-miss.

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc
- docs/language/precept-language-spec.md § Field Modifiers — spec: default clauses scope to fields declared above
- src/Precept/Language/Types.cs:297 — catalog

## g16/list-first/field-modifier — List .first read inside a field-modifier bound expression

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc
- docs/language/precept-language-spec.md § Field Modifiers — spec: a constraint modifier desugars to the equivalent rule

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | list accessor .first (NumericProofRequirement: count > 0) |
| evaluation site category | field-modifier-value-expression |
| type family | collection |

### What must be proven

Obligation: Milestones.count > 0

Weakest precondition: Milestones.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Milestones | the list-typed collection field the .first accessor reads |
| count | the catalog's built-in collection-count accessor (Types.cs:172) |
| 0 | the catalog-declared non-emptiness bound |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix

### Which premise classes can discharge it

Applicable classes: (a).

A field-modifier value expression desugars to a rule condition and has a rule condition's scope (no ordering restriction), but carries no guard and reads no event args, so classes (b) and (c) have no attachment point here. Only class (a) — a mentioned field's own declared modifier/default fact — is available; this group's authoring note records class (d) as unconsumed throughout the region.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: constant-fold of the accessor's NumericProofRequirement over the file's default configuration. In this witness there is no event and no handler at all, so the default configuration is the only configuration the file ever reaches; the field-modifier's desugared rule and the field-default clause are therefore decided by the identical fold.
- Validity arguments: Literal constant-fold for defaults; Establishment over defaults
- Decision procedure: Constant-fold the accessor's count-non-empty precondition over the default environment, exactly as the field-default cell's decision procedure does. This extends 'Literal constant-fold for defaults' and 'Establishment over defaults' beyond their literally-stated scope (the default clause construct, and rule-family establishment respectively) to a fault obligation evaluated at a non-default declaration position; the extension rests on the observation, not itself separately argued in the matrix, that an event-less file's default configuration is its only reachable one. Recorded as a citable-but-extended application, and listed as a missing rule: the matrix does not yet state a fault-family analogue of 'Establishment over defaults'.

### What the failing diagnostic must suggest

- For class (a): give <Collection> a literal, non-empty declared default so <WP> folds true
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept ListFirstFieldMod
field Milestones as list of decimal
field Cap as decimal max Milestones.first default 0.0
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools (dotnet CLI, release build), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*literal-default* — other

- Addition: default [1.0, 2.0]
- Premise classes: (a)
- Derivation: constant fold: default [1.0, 2.0] gives Milestones.count == 2, so Milestones.count > 0 folds true
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (field-modifier-value-expression does not mint the accessor's fault obligation at HEAD: base and discharge both compile with only a FieldNeverSet warning and the matrix tool's own '[skipped obligation] Cap:max: bound is not a literal declared value' note (a WP-calculator limitation, not a compiler diagnostic); no fault diagnostic fires either way. Measured live 2026-07-21 at commit e1a14d91.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (dotnet CLI, release build), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| literal-default | mincount 1 | Milestones's declared default stays empty; mincount 1's own desugared rule is unearned against that default, and Milestones's actual value is unchanged, so the fold behind Cap's bound still sees an empty collection: same obligation, must still reject. | reject, naming the same obligation |

### Respellability

- Resolution: cell-override
- A representative safe-but-rejected spelling: field Milestones as list of decimal event Start(FirstValue as decimal) initial on Start -> append Milestones Start.FirstValue field Cap as decimal max Milestones.first default 0.0
- Its licensed respelling: field Milestones as list of decimal default [1.0] field Cap as decimal max Milestones.first default 0.0

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Respellable bullet

- The band member is sound (Milestones is non-empty from construction onward, via the initial event's append) but this cell's contract licenses only a literal default fold; discharging the band member's shape would need a validity argument the matrix has not written (establishment via an initial event's write plan — see the missingRules entry on the corresponding accessor/Log/last cells in this file). Its business intent — a collection guaranteed non-empty at construction — respells losslessly into a literal default, which list-typed collections can always express.

**What the sources leave unstated or ambiguous here**

- Field-modifier bound expressions carry no declaration-order restriction (spec § Field Modifiers), unlike field-default; that distinction does not change this cell's discharge in the event-less witness used here, since both positions are decided by the same default-configuration fold in the absence of any handler.
- All witnesses carry a FieldNeverSet structural warning, orthogonal to the target obligation.

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc
- docs/language/precept-language-spec.md § Field Modifiers — spec
- src/Precept/Language/Types.cs:297 — catalog

## g16/list-first/event-arg-modifier — List .first read inside an event-arg-modifier bound expression

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc
- docs/Working/what-i-want-2026-07-16.md:141 — want-doc: the event-arg ingress door

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | list accessor .first (NumericProofRequirement: count > 0) |
| evaluation site category | event-arg-modifier-value-expression |
| type family | collection |

### What must be proven

Obligation: Milestones.count > 0

Weakest precondition: Milestones.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Milestones | the list-typed collection field the .first accessor reads |
| count | the catalog's built-in collection-count accessor |
| 0 | the catalog-declared non-emptiness bound |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix

### Which premise classes can discharge it

Applicable classes: (a).

This position sits at the event-arg ingress door: the arg's own modifier-rules are what ingress validation evaluates, but no guard is in scope and the arg being constrained (Delta) is the subject, not a premise. The only fact this bound expression can consume about a different field (Milestones) is that field's own declared modifier/default: class (a). Class (d) is not consumed anywhere in this group's region.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: constant-fold of the accessor's NumericProofRequirement over the file's default configuration, identical in mechanism to the field-modifier cell above: the event's argument bound is checked once ingress validation runs, but Milestones itself is never written in this witness, so its value is the one the default configuration fixes.
- Validity arguments: Literal constant-fold for defaults; Establishment over defaults
- Decision procedure: Same procedure as g16/list-first/field-modifier, applied to the event-arg's bound expression instead of a field's. Carries the same extension-beyond-stated-scope caveat.

### What the failing diagnostic must suggest

- For class (a): give <Collection> a literal, non-empty declared default so <WP> folds true
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept ListFirstArgMod
field Milestones as list of decimal
event Adjust(Delta as decimal max Milestones.first)
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools (dotnet CLI, release build), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*literal-default* — other

- Addition: default [1.0, 2.0]
- Premise classes: (a)
- Derivation: constant fold: default [1.0, 2.0] gives Milestones.count == 2, so Milestones.count > 0 folds true
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (event-arg-modifier-value-expression does not mint the accessor's fault obligation at HEAD: base and discharge both compile clean (only the routine FieldNeverSet warning on Milestones). Measured live 2026-07-21 at commit e1a14d91.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (dotnet CLI, release build), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| literal-default | mincount 1 | Milestones's declared default stays empty and its actual value is unchanged, so the event-arg bound's fold still sees an empty collection: same obligation, must still reject. | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

- Same band member and respelling as g16/list-first/field-modifier (an initial-event-populated Milestones respells into a literal default); not repeated here.

**What the sources leave unstated or ambiguous here**

- All witnesses carry a FieldNeverSet structural warning, orthogonal to the target obligation.

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc
- docs/Working/what-i-want-2026-07-16.md:141 — want-doc
- src/Precept/Language/Types.cs:297 — catalog

## g16/list-first/inner-type-modifier — List .first read inside a collection inner-type modifier bound expression

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc
- docs/language/precept-language-spec.md § Type References — spec: value modifiers in inner-type position desugar to per-element rules and participate in proof identically to the same modifier on a scalar field

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | list accessor .first (NumericProofRequirement: count > 0) |
| evaluation site category | collection-inner-type-modifier-value-expression |
| type family | collection |

### What must be proven

Obligation: Milestones.count > 0

Weakest precondition: Milestones.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Milestones | the list-typed collection field the .first accessor reads |
| count | the catalog's built-in collection-count accessor |
| 0 | the catalog-declared non-emptiness bound |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix

### Which premise classes can discharge it

Applicable classes: (a).

The inner-type value modifier desugars to a per-element rule with the element as subject (spec § Type References); the same rule-condition scope and lack of guard/args applies, so only class (a) — Milestones's own declared modifier/default — is available.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: constant-fold of the accessor's NumericProofRequirement over the file's default configuration, identical in mechanism to the field-modifier cell above.
- Validity arguments: Literal constant-fold for defaults; Establishment over defaults
- Decision procedure: Same procedure as g16/list-first/field-modifier, applied to the collection's inner-type bound expression. Carries the same extension-beyond-stated-scope caveat.

### What the failing diagnostic must suggest

- For class (a): give <Collection> a literal, non-empty declared default so <WP> folds true
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept ListFirstInnerType
field Milestones as list of decimal
field Caps as list of decimal max Milestones.first
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools (dotnet CLI, release build), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*literal-default* — other

- Addition: default [1.0, 2.0]
- Premise classes: (a)
- Derivation: constant fold: default [1.0, 2.0] gives Milestones.count == 2, so Milestones.count > 0 folds true
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (collection-inner-type-modifier-value-expression does not mint the accessor's fault obligation at HEAD: base and discharge both compile clean (only the FieldNeverSet warnings on Milestones and Caps). Measured live 2026-07-21 at commit e1a14d91.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (dotnet CLI, release build), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| literal-default | mincount 1 | Milestones's declared default stays empty and its actual value is unchanged, so the inner-type bound's fold still sees an empty collection: same obligation, must still reject. | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

- Same band member and respelling as g16/list-first/field-modifier; not repeated here.

**What the sources leave unstated or ambiguous here**

- All witnesses carry FieldNeverSet structural warnings, orthogonal to the target obligation.

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc
- docs/language/precept-language-spec.md § Type References — spec
- src/Precept/Language/Types.cs:297 — catalog

## g16/list-first/type-qualifier — List .first (of a currency-typed list) read inside a type-qualifier expression

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc
- docs/language/precept-language-spec.md § Type References — spec: TypeQualifier := (in \| of \| to) Expr

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | list accessor .first (NumericProofRequirement: count > 0) |
| evaluation site category | type-qualifier-expression |
| type family | collection |

### What must be proven

Obligation: Currencies.count > 0

Weakest precondition: Currencies.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Currencies | the list-typed collection field (of currency) the .first accessor reads |
| count | the catalog's built-in collection-count accessor |
| 0 | the catalog-declared non-emptiness bound |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix

### Which premise classes can discharge it

Applicable classes: (a).

The qualifier expression (in practice, an interpolated typed constant) has no guard and reads no event args; only class (a), the read collection's own declared modifier/default, is available.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: constant-fold of the accessor's NumericProofRequirement over the file's default configuration, identical in mechanism to the field-modifier cell above; here the read collection is Currencies (list of currency) rather than a decimal-typed collection, since money's 'in' qualifier requires a currency-typed expression.
- Validity arguments: Literal constant-fold for defaults; Establishment over defaults
- Decision procedure: Same procedure as g16/list-first/field-modifier, applied to the qualifier's interpolated expression. Carries the same extension-beyond-stated-scope caveat.

### What the failing diagnostic must suggest

- For class (a): give <Collection> a literal, non-empty declared default so <WP> folds true
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept ListFirstQualifier
field Currencies as list of currency
field Amount as money in '{Currencies.first}' default '0 {Currencies.first}'
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools (dotnet CLI, release build), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*literal-default* — other

- Addition: default [CurrencyCode] (Currencies), reading an earlier field 'field CurrencyCode as currency default "USD"'
- Premise classes: (a)
- Derivation: constant fold: Currencies defaults to a one-element list built from CurrencyCode's own literal default, so Currencies.count == 1 and Currencies.count > 0 folds true. (A bare currency typed-constant list literal, e.g. default ['USD'], does not type-check standalone — currency typed constants need a target-typed context to resolve, confirmed live: UnresolvedTypedConstant on a bare 'USD' list element — so the discharge routes the literal through an earlier currency-typed field instead.)
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (type-qualifier-expression does not mint the accessor's fault obligation at HEAD: base and discharge both compile clean (only FieldNeverSet warnings). Measured live 2026-07-21 at commit e1a14d91.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (dotnet CLI, release build), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Concretely tested as: field CurrencyCode as currency default 'USD' / field Currencies as list of currency default [CurrencyCode] / field Amount as money in '{Currencies.first}' default '0 {Currencies.first}' — compiles clean.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| literal-default | mincount 1 | Currencies's declared default stays empty and its actual value is unchanged, so the qualifier's fold still sees an empty collection: same obligation, must still reject. | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

- Same phenomenon as g16/list-first/field-modifier (an initial-event-populated Currencies respells into a literal default, routed through an earlier currency-typed field as this cell's discharge does); not repeated here.

**What the sources leave unstated or ambiguous here**

- TypeQualifier's grammar (spec § Type References) states 'TypeQualifier := (in \| of \| to) Expr', but the parser accepts only a typed-constant/interpolated-string production in this position at HEAD, not an arbitrary member-access expression — confirmed live: 'money in Currencies.first' (no quotes) raises ExpectedToken ('Expected typed constant here'); the interpolated form 'money in \'{Currencies.first}\'' is required and does parse. Recorded as an observed grammar/implementation gap distinct from this group's fault-minting finding, not resolved here.
- All witnesses carry FieldNeverSet structural warnings, orthogonal to the target obligation.

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:104 — want-doc
- docs/language/precept-language-spec.md § Type References — spec
- src/Precept/Language/Types.cs:297 — catalog

## g16/log-last/field-default — Log .last read inside a field default clause

Disposition: **open**.

**Disposition sources**

- docs/language/precept-language-spec.md:2036 — spec: construction builds the hollow version with defaults applied — before any event fires
- src/Precept/Language/DiagnosticCode.cs:96 — code: ListLiteralOutsideDefault; TypeChecker.Expressions.Callables.cs:618-674 resolves a list literal to TypeKind.List with no widening to Log, confirmed live: 'field Readings as log of decimal default [1.0, 2.0]' raises TypeMismatch ('Expected a log value here, but got list')
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: no written argument covers establishing a field's non-emptiness through an initial event's write plan; Witness Family 1 (line 248) states this case is vacuous/untested

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | log accessor .last (NumericProofRequirement: count > 0) |
| evaluation site category | field-default-value-expression |
| type family | collection |

### What must be proven

Obligation: Readings.count > 0

Weakest precondition: Readings.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Readings | the log-typed collection field the .last accessor reads, declared above the reading field |
| count | the catalog's built-in collection-count accessor |
| 0 | the catalog-declared non-emptiness bound |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix

**What the sources leave unstated or ambiguous here**

- MISSING RULE: a field-default clause evaluates strictly before any event fires (spec:2036), so the only way a collection could be non-empty at that point is a literal default. List literals are the sole non-empty collection-literal syntax and resolve only to TypeKind.List (TypeChecker.Expressions.Callables.cs:618-674) — confirmed live that a log-typed field's default cannot be a list literal (TypeMismatch). So no expression can ever populate Readings before this read occurs, for any log-typed (and, by the identical argument, queue/stack/set/bag/queue-by/lookup-typed) collection. The obligation is real and always minted where it mints at all, but no derivation the matrix licenses — nor, as far as this pass can tell, any derivation that COULD exist under the language's current construction-order semantics — discharges it. None of the five disposition kinds cleanly names 'a real obligation this type can never satisfy, structurally, at this site' (empty is for pruned/non-arising cells; this one does arise). Marked open rather than forcing a fit, and reported as a missing rule: the matrix should state what disposition a site x type combination gets when no discharge can exist by construction, not merely none is yet written.
- Not authored: obligation/dischargeContract/suggestions/witnesses/respellability are omitted per schema (only required when disposition is 'defined'). The obligation schema above is included for context since it is fully derivable; the discharge side is exactly what is missing.

## g16/log-last/field-modifier — Log .last read inside a field-modifier bound expression

Disposition: **open**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:37 — matrix: Vocabulary: establishment's site set is construction (defaults + initial events)
- docs/Working/obligation-discharge-matrix-2026-07-19.md:141 — matrix: Axis 5: establishment WP is over defaults only for fields the initial event does not write, implying a different rule for fields it does write
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: none of the seven written arguments covers discharging a fault obligation via an initial event's collection-action write plan; Witness Family 1 (line 248) flags initial-event establishment as untested in this slice

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | log accessor .last (NumericProofRequirement: count > 0) |
| evaluation site category | field-modifier-value-expression |
| type family | collection |

### What must be proven

Obligation: Readings.count > 0

Weakest precondition: Readings.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Readings | the log-typed collection field the .last accessor reads |
| count | the catalog's built-in collection-count accessor |
| 0 | the catalog-declared non-emptiness bound |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix

**What the sources leave unstated or ambiguous here**

- MISSING RULE (shared with g16/log-last/event-arg-modifier, g16/log-last/inner-type-modifier, g16/log-last/type-qualifier): unlike a field-default clause, this position is a rule-condition-shaped check over the fully-built configuration, so — per Axis 5 — if an initial event writes Readings (e.g. an 'append' action), establishment would in principle be evaluated over the post-initial-event state, in which Readings is non-empty. But the matrix has written no validity argument for HOW an initial event's collection-action write plan discharges an obligation; Witness Family 1 explicitly leaves initial-event establishment untested ('the establishment half over initial events is vacuous here', line 248). Citing 'Literal constant-fold for defaults' / 'Establishment over defaults' here would be invented, since log-typed collections cannot use the literal-default route this file's List cells use (confirmed on g16/log-last/field-default) and the initial-event route has no written argument. Left open rather than inventing an extension; this is the same missing rule as g16/log-last/field-default's second half, minus the field-default-specific construction-order impossibility.
- Not authored: dischargeContract/suggestions/witnesses/respellability omitted per schema (not required when disposition is 'open').

## g16/log-last/event-arg-modifier — Log .last read inside an event-arg-modifier bound expression

Disposition: **open**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:37 — matrix
- docs/Working/obligation-discharge-matrix-2026-07-19.md:141 — matrix
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | log accessor .last (NumericProofRequirement: count > 0) |
| evaluation site category | event-arg-modifier-value-expression |
| type family | collection |

### What must be proven

Obligation: Readings.count > 0

Weakest precondition: Readings.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Readings | the log-typed collection field the .last accessor reads |
| count | the catalog's built-in collection-count accessor |
| 0 | the catalog-declared non-emptiness bound |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix

**What the sources leave unstated or ambiguous here**

- Same missing rule as g16/log-last/field-modifier: an initial-event append could in principle make Readings non-empty by the point this bound is checked, but the matrix has written no validity argument for discharging a fault obligation through an initial event's write plan. Left open; see g16/log-last/field-modifier for the full derivation.
- Not authored: dischargeContract/suggestions/witnesses/respellability omitted per schema (not required when disposition is 'open').

## g16/log-last/inner-type-modifier — Log .last read inside a collection inner-type modifier bound expression

Disposition: **open**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:37 — matrix
- docs/Working/obligation-discharge-matrix-2026-07-19.md:141 — matrix
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | log accessor .last (NumericProofRequirement: count > 0) |
| evaluation site category | collection-inner-type-modifier-value-expression |
| type family | collection |

### What must be proven

Obligation: Readings.count > 0

Weakest precondition: Readings.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Readings | the log-typed collection field the .last accessor reads |
| count | the catalog's built-in collection-count accessor |
| 0 | the catalog-declared non-emptiness bound |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix

**What the sources leave unstated or ambiguous here**

- Same missing rule as g16/log-last/field-modifier. Left open; see that cell for the full derivation.
- Not authored: dischargeContract/suggestions/witnesses/respellability omitted per schema (not required when disposition is 'open').

## g16/log-last/type-qualifier — Log .last (of a currency-typed log) read inside a type-qualifier expression

Disposition: **open**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md:37 — matrix
- docs/Working/obligation-discharge-matrix-2026-07-19.md:141 — matrix
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | log accessor .last (NumericProofRequirement: count > 0) |
| evaluation site category | type-qualifier-expression |
| type family | collection |

### What must be proven

Obligation: CurrencyLog.count > 0

Weakest precondition: CurrencyLog.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| CurrencyLog | the log-typed collection field (of currency) the .last accessor reads |
| count | the catalog's built-in collection-count accessor |
| 0 | the catalog-declared non-emptiness bound |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix

**What the sources leave unstated or ambiguous here**

- Same missing rule as g16/log-last/field-modifier. Left open; see that cell for the full derivation.
- Not authored: dischargeContract/suggestions/witnesses/respellability omitted per schema (not required when disposition is 'open').

## Fields with no rendering rule

Data the generator has no rendering rule for, surfaced verbatim rather than dropped. Each is either a schema addition the generator has not caught up with, or a stray field.

| Where | Field | Value |
|---|---|---|
| cells[g16/list-first/field-modifier].respellability | overrideVerdict | "yes" |

