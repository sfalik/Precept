<!--
GENERATED FILE — do not hand-edit.
Source: fault-19-choice-ordering-modifier.cells.json
Regenerate: dotnet run --project tools/Precept.MatrixTools -- render-cells docs/Working/obligation-discharge-matrix-2026-07-19-cells
Hand edits will be overwritten. Edit the .cells.json and regenerate.
-->

# Fault family — comparing choice values never declared ordered (group g19, modifier discharge)

Family id: fault-19-choice-ordering-modifier
Case shape: fault
Definition version: obligation-discharge-matrix-2026-07-19 rev 7 (2026-07-20, draft)

**Where this family's content comes from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row: catalog-declared safety precondition at the evaluation site
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — matrix: Fault minting: catalog-stamped at evaluation sites
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: premise classes, incl. (a) field modifiers; the Normalization bullet
- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault family origin
- src/Precept/Language/Operations.cs:866 — code
- src/Precept/Language/ProofRequirement.cs:148 — code
- src/Precept/Language/Modifiers.cs:63 — code

## Respellability — the family verdict

Can every safe program these rules reject be rewritten into a form they accept? **yes**

Evidence: model-derived
Corpus measurement: pending

**Notes on the family verdict**

- This family has no sound-but-unprovable band at all (see each cell's respellability override): `ordered` admits no partial-credit spelling, so there is no algebraically-rearranged or per-term-bound analogue for a reviewer to weigh. Recorded 'yes' at the family level trivially, not as a claim that the numeric family's respellability paragraph transfers.
- Cell count: 12, covering all 80 product coordinates (4 catalog operations x 20 evaluation-site categories x 1 type family) via category clustering, per the disposition-pass authoring note (recorded outside the repo, at the packet path this file was authored against, and not itself a resolvable citation) that this group's applicable-class set does not vary along the site axis. Six cells mint at HEAD (transition-row-write, construction-row-write, state-hook-write, constraint-condition, computed-field, quantifier-predicate); five do not mint (guard-row-and-hook, guard-access-and-activation, message-interpolation, declaration-value-expressions, collection-inner-type-modifier); one (type-qualifier) is unmeasured due to an entangled diagnostic. This mint/non-mint split was independently measured in this file (see each cell's own notes), and agrees with the fault family's general non-minting count (guard x5 + message x2 + declaration x3 = 10 non-minting positions, plus 2 unmeasured).
- The asymmetric-operand-check defect (see the DEFECT NOTE on each minting cell) is this file's second major finding, independent of the missing-validity-argument gap recorded on every cell. It was directly live-verified at all six minting evaluation-site categories (transition-row-action-operand, construction-row-action-operand, state-hook-action-operand, rule-condition, event-ensure-condition, state-ensure-condition, computed-field-expression, quantifier-predicate — eight sites across six cells, since one cell stands for three constraint-condition categories) and is consistent across all of them: only the syntactically-right (second) operand's `ordered` modifier is ever checked.

**Sources for this verdict**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Respellable definition — 'yes' iff every sound program in the band has a provable respelling

## Premise classes — the legend

Where a proof may get its facts (want :19, :143): (a) field modifiers, (b) arg constraints, (c) the handler's guard, (d) all rules holding in the pre-state (the inductive hypothesis), (e) evaluating the applicable rule set at an editable-field write — (e)'s status as a genuinely fifth class vs (a) generalized is an open owner item.

## g19/transition-row-write — Transition-row action operand — StatusA < StatusB

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- src/Precept/Language/Operations.cs:866 — code

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | ChoiceLessThanChoice — choice < choice |
| evaluation site category | transition-row-action-operand |
| type family | primitive |

### What must be proven

Obligation: StatusA < StatusB

Weakest precondition: StatusA is declared ordered and StatusB is declared ordered

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| StatusA | the comparison's left (first) choice-typed operand |
| StatusB | the comparison's right (second) choice-typed operand |
| < | one of <, <=, >, >= over two choice-typed operands — the catalog's four ChoiceXChoice operations, all carrying the identical `ModifierRequirement(Ordered)` shape (Operations.cs:866-897) |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet — comparison-direction flip: `StatusA < StatusB` is normal-form-equal to `StatusB > StatusA`, which is why all four operators mint the identical obligation (both operands declared ordered), just paired into two normal forms (strict / inclusive)

### Which premise classes can discharge it

Applicable classes: (a).

The applicable-class set is {(a)} at this evaluation-site category, and — the one respect in which this whole group differs from every other authored fault group — it is {(a)} at EVERY evaluation-site category in the region, not just this one: the fact consumed (the `ordered` modifier on both operands) is a static declaration attribute, not a runtime value bound, so which site the comparison sits in has no bearing on which premise classes could discharge it. Classes (b)/(c)/(d) supply argument bounds, guard truth, and pre-state value facts respectively — none of which bears on whether a choice TYPE was declared `ordered`. That derivation is the cell's own (matrix § Vocabulary, premise classes; disposition-pass authoring notes for group g19).

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: both comparison operands' declared modifier sets contain `ordered` -> the choice-ordering operator is well-defined; the catalog's `ModifierRequirement(Subject: ParamSubject(PChoice), Required: Ordered)` (Operations.cs:866-897) resolved against each operand's own field/arg/bound-variable declaration
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Look up `ordered` in each operand's own declared modifier set (a finite set per field, event-arg, or quantifier-bound-variable declaration). Both present discharges; either absent rejects, naming class (a) and the specific missing operand(s).

- src/Precept/Language/Operations.cs:866 — code: ChoiceLessThanChoice — ProofRequirements: [ModifierRequirement(ParamSubject(PChoice), Ordered, "Both choice operands must be declared ordered")]
- src/Precept/Language/Operations.cs:874 — code: ChoiceGreaterThanChoice — identical requirement shape
- src/Precept/Language/Operations.cs:882 — code: ChoiceLessThanOrEqualChoice — identical requirement shape
- src/Precept/Language/Operations.cs:890 — code: ChoiceGreaterThanOrEqualChoice — identical requirement shape
- src/Precept/Language/ProofRequirement.cs:148 — code: ModifierRequirement doc comment: "When both operands share the same ParameterMeta reference (as with choice ordering operations), the requirement applies to all matching operand positions" — the catalog's own statement of intent that both operands are meant to be checked
- src/Precept/Language/Modifiers.cs:63 — code: ModifierKind.Ordered — ModifierCategory.Structural, no ProofSatisfactions entries (unlike Nonnegative/Positive/Nonzero, which carry ProofSatisfaction.Numeric) — the catalog's own signal that this modifier is not a value-bound fact
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: premise class (a): field modifiers
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row: catalog-declared safety precondition at the evaluation site; premise classes (b)/(c)/(a) per catalog ProofSatisfactions

### What the failing diagnostic must suggest

- For class (a): declare `ordered` on both of <WP>'s choice-typed operands
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept ChoiceOrderWriteBase

field StatusA as choice of string("Low", "Medium", "High")
field StatusB as choice of string("Low", "Medium", "High")
field IsHigher as boolean default false

state Active initial
state Done terminal

event Open initial
event Recompute
event Finish

on Open
    -> set StatusA = "Low"
    -> set StatusB = "Low"

from Active on Recompute
    -> set IsHigher = StatusA < StatusB
    -> no transition

from Active on Finish
    -> transition Done
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*both-ordered* — other

- Addition: replace `field StatusA as choice of string("Low", "Medium", "High")` with `field StatusA as choice of string("Low", "Medium", "High") ordered`; and replace `field StatusB as choice of string("Low", "Medium", "High")` with `field StatusB as choice of string("Low", "Medium", "High") ordered`
- Premise classes: (a)
- Derivation: both operands' declared modifier sets contain `ordered` -> ModifierRequirement(Ordered) resolves true for both -> the comparison is well-defined
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` block is given: the schema's `application` discriminated union offers only `row-guard` and `event-arg-declarations`, and this addition edits a plain field, event-arg, or collection-inner-type declaration (or, for the quantifier cell, a quantifier-bound variable's source collection) directly — a locus the DU does not yet name. Recorded as a schema gap rather than forced into either existing locus (same gap already flagged in fault-3/fault-5 for field-declaration additions).

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| both-ordered | replace `field StatusA as choice of string("Low", "Medium", "High")` with `field StatusA as choice of string("Low", "Medium", "High") ordered` | Removing `ordered` from the right (second) operand while the left (first) operand keeps it still leaves one operand undeclared -> the modifier lookup still fails for that operand -> must still reject, same obligation. | reject, naming the same obligation |

### Respellability

- Resolution: cell-override

- Overrides the family-wide numeric respellability paragraph (which is about algebraic rearrangement of value bounds and does not apply here — there is no family-wide verdict yet written for the fault case shape's modifier-discharge groups). There is no sound-but-unprovable band for this obligation: unlike a numeric bound, `ordered` has no partial-credit spelling (no per-term conjunct, no alternate bound) — a comparison on a choice type that is not declared `ordered` is not merely unprovable, it is not well-formed (the type declares no ordering; `<` would be arbitrary, per `ordered`'s own hover text, 'ordered by declaration sequence', Modifiers.cs:66). So every sound program in this cell's class already IS in the licensed form (`ordered` declared on both operands); there is nothing to respell.

**What the sources leave unstated or ambiguous here**

- MISSING RULE, central to this file: the matrix's closed § Validity arguments list (seven named arguments) has no argument that argues a discharge whose entire premise is a declared-modifier PRESENCE fact unconnected to a runtime value bound. The seven stated arguments (Guard normal-form match, Arg-bound interval arithmetic, Inductive hypothesis plus sign monotonicity, Literal constant-fold for defaults, Establishment over defaults, Guard-fact substitution for relational rules, Vacuity by activation) are all, in their actual written content, about intervals, guard truth, literal folding, or activation vacuity — none of them is about a static declaration attribute that makes an operator well-defined independent of any runtime value. `ordered` is not enforced on a VALUE at ingress the way a `positive`/`max` bound is (there is no runtime check being preserved through evaluation); it is a fact about the field's TYPE, checked once, structurally, with nothing further for a validity argument to preserve across evaluation. The catalog's own structure corroborates this: `ModifierKind.Ordered` carries no `ProofSatisfactions` entry (Modifiers.cs:63), unlike the numeric sign modifiers the existing arguments were written for. This group is the ONLY group in the fault family's defined region whose discharge is a bare modifier lookup rather than an interval/guard/literal derivation (verified against the disposition map: `requirementKind: "Modifier"` appears at no other catalog site in the fault family). Every discharge-contract entry below cites "Arg-bound interval arithmetic" because the schema's `validityArguments` enum requires one of the seven closed names and that is the least-remote of the seven (it is the argument this same corpus already stretches furthest, from event-arg modifiers to field modifiers, for other groups' class-(a) discharges) — but this citation is recorded as a placeholder, not as an adequate argument. The owner needs to author a new validity argument for declaration-attribute-only discharges (or rule that this requirement kind is type-checking, not proof surface, and does not belong in the matrix's proof-obligation product at all) before any cell in this file can ratify. Nothing here should be read as claiming that gap closed.
- Base, discharge, and (rejecting) near-miss are all live-verified at HEAD, commit e1a14d91, 2026-07-21: base rejects `UnprovedModifierRequirement: Cannot prove that 'StatusB' satisfies the required modifier 'Ordered'`; the both-ordered discharge compiles with zero diagnostics; the near-miss (StatusA ordered, StatusB not) still rejects with the identical diagnostic naming StatusB.
- BUILT DEFECT, live-verified 2026-07-21 (commit e1a14d91, Precept.MatrixTools): HEAD's `Ordered`-modifier check is asymmetric. In every evaluation-site category tested in this file where the site mints at all, declaring `ordered` on only the operand that is syntactically SECOND (right of the comparison operator) and leaving the FIRST (left) operand undeclared compiles with ZERO diagnostics — the engine never checks the left operand's `Ordered` modifier at all, even though the catalog's own `ModifierRequirement` doc comment (ProofRequirement.cs:148) states the requirement is meant to apply to "all matching operand positions" when both operands share the same `ParameterMeta`. The formal near-miss below weakens the OTHER direction (right operand loses `ordered`, left keeps it) because that is the shape that still correctly rejects — matching the schema's near-miss contract. The direction that HEAD gets wrong is recorded here rather than as a formal near-miss, since the schema's `nearMissWitness.expected.outcome` is a fixed `reject` and this shape does not reject at HEAD; forcing it into that slot would misrepresent the schema's own field. Confirmed live at this cell (transition-row-action-operand), so the defect is a property of the check itself, not of one syntactic position. This is a distinct finding from the Part 3 false-proof hazard (authored-expressiveness-gaps.md) — it is not a business rule used as a premise; it is the modifier check itself failing to inspect both operands — so it carries no false-proof-hazard exclusion and is reported as its own soundness gap.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- src/Precept/Language/Operations.cs:866 — code
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — matrix: Fault minting: catalog-stamped at evaluation sites

## g19/construction-row-write — Construction-row action operand — StatusA <= StatusB

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- src/Precept/Language/Operations.cs:882 — code

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | ChoiceLessThanOrEqualChoice — choice <= choice |
| evaluation site category | construction-row-action-operand |
| type family | primitive |

### What must be proven

Obligation: StatusA <= StatusB

Weakest precondition: StatusA is declared ordered and StatusB is declared ordered

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| StatusA | the comparison's left (first) choice-typed operand |
| StatusB | the comparison's right (second) choice-typed operand |
| <= | one of <, <=, >, >= over two choice-typed operands — the catalog's four ChoiceXChoice operations, all carrying the identical `ModifierRequirement(Ordered)` shape (Operations.cs:866-897) |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet — comparison-direction flip: `StatusA < StatusB` is normal-form-equal to `StatusB > StatusA`, which is why all four operators mint the identical obligation (both operands declared ordered), just paired into two normal forms (strict / inclusive)

### Which premise classes can discharge it

Applicable classes: (a).

The applicable-class set is {(a)} at this evaluation-site category, and — the one respect in which this whole group differs from every other authored fault group — it is {(a)} at EVERY evaluation-site category in the region, not just this one: the fact consumed (the `ordered` modifier on both operands) is a static declaration attribute, not a runtime value bound, so which site the comparison sits in has no bearing on which premise classes could discharge it. Classes (b)/(c)/(d) supply argument bounds, guard truth, and pre-state value facts respectively — none of which bears on whether a choice TYPE was declared `ordered`. That derivation is the cell's own (matrix § Vocabulary, premise classes; disposition-pass authoring notes for group g19).

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: both comparison operands' declared modifier sets contain `ordered` -> the choice-ordering operator is well-defined; the catalog's `ModifierRequirement(Subject: ParamSubject(PChoice), Required: Ordered)` (Operations.cs:866-897) resolved against each operand's own field/arg/bound-variable declaration
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Look up `ordered` in each operand's own declared modifier set (a finite set per field, event-arg, or quantifier-bound-variable declaration). Both present discharges; either absent rejects, naming class (a) and the specific missing operand(s).

- src/Precept/Language/Operations.cs:866 — code: ChoiceLessThanChoice — ProofRequirements: [ModifierRequirement(ParamSubject(PChoice), Ordered, "Both choice operands must be declared ordered")]
- src/Precept/Language/Operations.cs:874 — code: ChoiceGreaterThanChoice — identical requirement shape
- src/Precept/Language/Operations.cs:882 — code: ChoiceLessThanOrEqualChoice — identical requirement shape
- src/Precept/Language/Operations.cs:890 — code: ChoiceGreaterThanOrEqualChoice — identical requirement shape
- src/Precept/Language/ProofRequirement.cs:148 — code: ModifierRequirement doc comment: "When both operands share the same ParameterMeta reference (as with choice ordering operations), the requirement applies to all matching operand positions" — the catalog's own statement of intent that both operands are meant to be checked
- src/Precept/Language/Modifiers.cs:63 — code: ModifierKind.Ordered — ModifierCategory.Structural, no ProofSatisfactions entries (unlike Nonnegative/Positive/Nonzero, which carry ProofSatisfaction.Numeric) — the catalog's own signal that this modifier is not a value-bound fact
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: premise class (a): field modifiers
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row: catalog-declared safety precondition at the evaluation site; premise classes (b)/(c)/(a) per catalog ProofSatisfactions

### What the failing diagnostic must suggest

- For class (a): declare `ordered` on both of <WP>'s choice-typed operands
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept ChoiceOrderConstructBase

field StatusA as choice of string("Low", "Medium", "High")
field StatusB as choice of string("Low", "Medium", "High")
field IsAtMost as boolean default false

event Init(A as choice of string("Low", "Medium", "High"), B as choice of string("Low", "Medium", "High")) initial

on Init
    -> set StatusA = Init.A
    -> set StatusB = Init.B
    -> set IsAtMost = StatusA <= StatusB
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*both-ordered* — other

- Addition: replace `field StatusA as choice of string("Low", "Medium", "High")` with `field StatusA as choice of string("Low", "Medium", "High") ordered`; and replace `field StatusB as choice of string("Low", "Medium", "High")` with `field StatusB as choice of string("Low", "Medium", "High") ordered`; and replace `event Init(A as choice of string("Low", "Medium", "High"), B as choice of string("Low", "Medium", "High")) initial` with `event Init(A as choice of string("Low", "Medium", "High") ordered, B as choice of string("Low", "Medium", "High") ordered) initial`
- Premise classes: (a)
- Derivation: both operands' declared modifier sets contain `ordered` -> ModifierRequirement(Ordered) resolves true for both -> the comparison is well-defined
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` block is given: the schema's `application` discriminated union offers only `row-guard` and `event-arg-declarations`, and this addition edits a plain field, event-arg, or collection-inner-type declaration (or, for the quantifier cell, a quantifier-bound variable's source collection) directly — a locus the DU does not yet name. Recorded as a schema gap rather than forced into either existing locus (same gap already flagged in fault-3/fault-5 for field-declaration additions).

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| both-ordered | replace `field StatusA as choice of string("Low", "Medium", "High")` with `field StatusA as choice of string("Low", "Medium", "High") ordered`; and replace `event Init(A as choice of string("Low", "Medium", "High"), B as choice of string("Low", "Medium", "High")) initial` with `event Init(A as choice of string("Low", "Medium", "High") ordered, B as choice of string("Low", "Medium", "High")) initial` | Removing `ordered` from the right (second) operand while the left (first) operand keeps it still leaves one operand undeclared -> the modifier lookup still fails for that operand -> must still reject, same obligation. | reject, naming the same obligation |

### Respellability

- Resolution: cell-override

- Overrides the family-wide numeric respellability paragraph (which is about algebraic rearrangement of value bounds and does not apply here — there is no family-wide verdict yet written for the fault case shape's modifier-discharge groups). There is no sound-but-unprovable band for this obligation: unlike a numeric bound, `ordered` has no partial-credit spelling (no per-term conjunct, no alternate bound) — a comparison on a choice type that is not declared `ordered` is not merely unprovable, it is not well-formed (the type declares no ordering; `<` would be arbitrary, per `ordered`'s own hover text, 'ordered by declaration sequence', Modifiers.cs:66). So every sound program in this cell's class already IS in the licensed form (`ordered` declared on both operands); there is nothing to respell.

**What the sources leave unstated or ambiguous here**

- MISSING RULE, central to this file: the matrix's closed § Validity arguments list (seven named arguments) has no argument that argues a discharge whose entire premise is a declared-modifier PRESENCE fact unconnected to a runtime value bound. The seven stated arguments (Guard normal-form match, Arg-bound interval arithmetic, Inductive hypothesis plus sign monotonicity, Literal constant-fold for defaults, Establishment over defaults, Guard-fact substitution for relational rules, Vacuity by activation) are all, in their actual written content, about intervals, guard truth, literal folding, or activation vacuity — none of them is about a static declaration attribute that makes an operator well-defined independent of any runtime value. `ordered` is not enforced on a VALUE at ingress the way a `positive`/`max` bound is (there is no runtime check being preserved through evaluation); it is a fact about the field's TYPE, checked once, structurally, with nothing further for a validity argument to preserve across evaluation. The catalog's own structure corroborates this: `ModifierKind.Ordered` carries no `ProofSatisfactions` entry (Modifiers.cs:63), unlike the numeric sign modifiers the existing arguments were written for. This group is the ONLY group in the fault family's defined region whose discharge is a bare modifier lookup rather than an interval/guard/literal derivation (verified against the disposition map: `requirementKind: "Modifier"` appears at no other catalog site in the fault family). Every discharge-contract entry below cites "Arg-bound interval arithmetic" because the schema's `validityArguments` enum requires one of the seven closed names and that is the least-remote of the seven (it is the argument this same corpus already stretches furthest, from event-arg modifiers to field modifiers, for other groups' class-(a) discharges) — but this citation is recorded as a placeholder, not as an adequate argument. The owner needs to author a new validity argument for declaration-attribute-only discharges (or rule that this requirement kind is type-checking, not proof surface, and does not belong in the matrix's proof-obligation product at all) before any cell in this file can ratify. Nothing here should be read as claiming that gap closed.
- Live-verified at HEAD, commit e1a14d91, 2026-07-21: same pattern as the write-site cell, at a construction row's initial-event action operand instead of a transition row's. Base rejects `UnprovedModifierRequirement` naming 'B' (the event arg); both-ordered compiles clean; near-miss (A ordered only) still rejects naming 'B'.
- BUILT DEFECT, live-verified 2026-07-21 (commit e1a14d91, Precept.MatrixTools): HEAD's `Ordered`-modifier check is asymmetric. In every evaluation-site category tested in this file where the site mints at all, declaring `ordered` on only the operand that is syntactically SECOND (right of the comparison operator) and leaving the FIRST (left) operand undeclared compiles with ZERO diagnostics — the engine never checks the left operand's `Ordered` modifier at all, even though the catalog's own `ModifierRequirement` doc comment (ProofRequirement.cs:148) states the requirement is meant to apply to "all matching operand positions" when both operands share the same `ParameterMeta`. The formal near-miss below weakens the OTHER direction (right operand loses `ordered`, left keeps it) because that is the shape that still correctly rejects — matching the schema's near-miss contract. The direction that HEAD gets wrong is recorded here rather than as a formal near-miss, since the schema's `nearMissWitness.expected.outcome` is a fixed `reject` and this shape does not reject at HEAD; forcing it into that slot would misrepresent the schema's own field. Confirmed live at this cell (construction-row-action-operand), so the defect is a property of the check itself, not of one syntactic position. This is a distinct finding from the Part 3 false-proof hazard (authored-expressiveness-gaps.md) — it is not a business rule used as a premise; it is the modifier check itself failing to inspect both operands — so it carries no false-proof-hazard exclusion and is reported as its own soundness gap.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- src/Precept/Language/Operations.cs:882 — code
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — matrix: Fault minting: catalog-stamped at evaluation sites

## g19/state-hook-write — State-hook action operand — StatusA <= StatusB

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- src/Precept/Language/Operations.cs:882 — code

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | ChoiceLessThanOrEqualChoice — choice <= choice |
| evaluation site category | state-hook-action-operand |
| type family | primitive |

### What must be proven

Obligation: StatusA <= StatusB

Weakest precondition: StatusA is declared ordered and StatusB is declared ordered

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| StatusA | the comparison's left (first) choice-typed operand |
| StatusB | the comparison's right (second) choice-typed operand |
| <= | one of <, <=, >, >= over two choice-typed operands — the catalog's four ChoiceXChoice operations, all carrying the identical `ModifierRequirement(Ordered)` shape (Operations.cs:866-897) |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet — comparison-direction flip: `StatusA < StatusB` is normal-form-equal to `StatusB > StatusA`, which is why all four operators mint the identical obligation (both operands declared ordered), just paired into two normal forms (strict / inclusive)

### Which premise classes can discharge it

Applicable classes: (a).

The applicable-class set is {(a)} at this evaluation-site category, and — the one respect in which this whole group differs from every other authored fault group — it is {(a)} at EVERY evaluation-site category in the region, not just this one: the fact consumed (the `ordered` modifier on both operands) is a static declaration attribute, not a runtime value bound, so which site the comparison sits in has no bearing on which premise classes could discharge it. Classes (b)/(c)/(d) supply argument bounds, guard truth, and pre-state value facts respectively — none of which bears on whether a choice TYPE was declared `ordered`. That derivation is the cell's own (matrix § Vocabulary, premise classes; disposition-pass authoring notes for group g19).

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: both comparison operands' declared modifier sets contain `ordered` -> the choice-ordering operator is well-defined; the catalog's `ModifierRequirement(Subject: ParamSubject(PChoice), Required: Ordered)` (Operations.cs:866-897) resolved against each operand's own field/arg/bound-variable declaration
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Look up `ordered` in each operand's own declared modifier set (a finite set per field, event-arg, or quantifier-bound-variable declaration). Both present discharges; either absent rejects, naming class (a) and the specific missing operand(s).

- src/Precept/Language/Operations.cs:866 — code: ChoiceLessThanChoice — ProofRequirements: [ModifierRequirement(ParamSubject(PChoice), Ordered, "Both choice operands must be declared ordered")]
- src/Precept/Language/Operations.cs:874 — code: ChoiceGreaterThanChoice — identical requirement shape
- src/Precept/Language/Operations.cs:882 — code: ChoiceLessThanOrEqualChoice — identical requirement shape
- src/Precept/Language/Operations.cs:890 — code: ChoiceGreaterThanOrEqualChoice — identical requirement shape
- src/Precept/Language/ProofRequirement.cs:148 — code: ModifierRequirement doc comment: "When both operands share the same ParameterMeta reference (as with choice ordering operations), the requirement applies to all matching operand positions" — the catalog's own statement of intent that both operands are meant to be checked
- src/Precept/Language/Modifiers.cs:63 — code: ModifierKind.Ordered — ModifierCategory.Structural, no ProofSatisfactions entries (unlike Nonnegative/Positive/Nonzero, which carry ProofSatisfaction.Numeric) — the catalog's own signal that this modifier is not a value-bound fact
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: premise class (a): field modifiers
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row: catalog-declared safety precondition at the evaluation site; premise classes (b)/(c)/(a) per catalog ProofSatisfactions

### What the failing diagnostic must suggest

- For class (a): declare `ordered` on both of <WP>'s choice-typed operands
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept ChoiceOrderStateHookBase

field StatusA as choice of string("Low", "Medium", "High")  default "Low" editable
field StatusB as choice of string("Low", "Medium", "High")  default "Low" editable
field IsAtMost as boolean default false

state Draft initial
state Done terminal

event Open initial
event Finish

on Open
    -> set StatusA = "Low"
    -> set StatusB = "Low"

from Draft on Finish
    -> transition Done

to Done -> set IsAtMost = StatusA <= StatusB
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*both-ordered* — other

- Addition: replace `field StatusA as choice of string("Low", "Medium", "High")  default "Low" editable` with `field StatusA as choice of string("Low", "Medium", "High") ordered default "Low" editable`; and replace `field StatusB as choice of string("Low", "Medium", "High")  default "Low" editable` with `field StatusB as choice of string("Low", "Medium", "High") ordered default "Low" editable`
- Premise classes: (a)
- Derivation: both operands' declared modifier sets contain `ordered` -> ModifierRequirement(Ordered) resolves true for both -> the comparison is well-defined
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` block is given: the schema's `application` discriminated union offers only `row-guard` and `event-arg-declarations`, and this addition edits a plain field, event-arg, or collection-inner-type declaration (or, for the quantifier cell, a quantifier-bound variable's source collection) directly — a locus the DU does not yet name. Recorded as a schema gap rather than forced into either existing locus (same gap already flagged in fault-3/fault-5 for field-declaration additions).

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| both-ordered | replace `field StatusA as choice of string("Low", "Medium", "High")  default "Low" editable` with `field StatusA as choice of string("Low", "Medium", "High") ordered default "Low" editable` | Removing `ordered` from the right (second) operand while the left (first) operand keeps it still leaves one operand undeclared -> the modifier lookup still fails for that operand -> must still reject, same obligation. | reject, naming the same obligation |

### Respellability

- Resolution: cell-override

- Overrides the family-wide numeric respellability paragraph (which is about algebraic rearrangement of value bounds and does not apply here — there is no family-wide verdict yet written for the fault case shape's modifier-discharge groups). There is no sound-but-unprovable band for this obligation: unlike a numeric bound, `ordered` has no partial-credit spelling (no per-term conjunct, no alternate bound) — a comparison on a choice type that is not declared `ordered` is not merely unprovable, it is not well-formed (the type declares no ordering; `<` would be arbitrary, per `ordered`'s own hover text, 'ordered by declaration sequence', Modifiers.cs:66). So every sound program in this cell's class already IS in the licensed form (`ordered` declared on both operands); there is nothing to respell.

**What the sources leave unstated or ambiguous here**

- MISSING RULE, central to this file: the matrix's closed § Validity arguments list (seven named arguments) has no argument that argues a discharge whose entire premise is a declared-modifier PRESENCE fact unconnected to a runtime value bound. The seven stated arguments (Guard normal-form match, Arg-bound interval arithmetic, Inductive hypothesis plus sign monotonicity, Literal constant-fold for defaults, Establishment over defaults, Guard-fact substitution for relational rules, Vacuity by activation) are all, in their actual written content, about intervals, guard truth, literal folding, or activation vacuity — none of them is about a static declaration attribute that makes an operator well-defined independent of any runtime value. `ordered` is not enforced on a VALUE at ingress the way a `positive`/`max` bound is (there is no runtime check being preserved through evaluation); it is a fact about the field's TYPE, checked once, structurally, with nothing further for a validity argument to preserve across evaluation. The catalog's own structure corroborates this: `ModifierKind.Ordered` carries no `ProofSatisfactions` entry (Modifiers.cs:63), unlike the numeric sign modifiers the existing arguments were written for. This group is the ONLY group in the fault family's defined region whose discharge is a bare modifier lookup rather than an interval/guard/literal derivation (verified against the disposition map: `requirementKind: "Modifier"` appears at no other catalog site in the fault family). Every discharge-contract entry below cites "Arg-bound interval arithmetic" because the schema's `validityArguments` enum requires one of the seven closed names and that is the least-remote of the seven (it is the argument this same corpus already stretches furthest, from event-arg modifiers to field modifiers, for other groups' class-(a) discharges) — but this citation is recorded as a placeholder, not as an adequate argument. The owner needs to author a new validity argument for declaration-attribute-only discharges (or rule that this requirement kind is type-checking, not proof surface, and does not belong in the matrix's proof-obligation product at all) before any cell in this file can ratify. Nothing here should be read as claiming that gap closed.
- Live-verified at HEAD, commit e1a14d91, 2026-07-21: the same pattern at a `to Done -> set ...` state-entry hook action operand. Base rejects `UnprovedModifierRequirement ... (used in state hook for 'Done')`; both-ordered compiles clean; near-miss (StatusA ordered only) still rejects.
- BUILT DEFECT, live-verified 2026-07-21 (commit e1a14d91, Precept.MatrixTools): HEAD's `Ordered`-modifier check is asymmetric. In every evaluation-site category tested in this file where the site mints at all, declaring `ordered` on only the operand that is syntactically SECOND (right of the comparison operator) and leaving the FIRST (left) operand undeclared compiles with ZERO diagnostics — the engine never checks the left operand's `Ordered` modifier at all, even though the catalog's own `ModifierRequirement` doc comment (ProofRequirement.cs:148) states the requirement is meant to apply to "all matching operand positions" when both operands share the same `ParameterMeta`. The formal near-miss below weakens the OTHER direction (right operand loses `ordered`, left keeps it) because that is the shape that still correctly rejects — matching the schema's near-miss contract. The direction that HEAD gets wrong is recorded here rather than as a formal near-miss, since the schema's `nearMissWitness.expected.outcome` is a fixed `reject` and this shape does not reject at HEAD; forcing it into that slot would misrepresent the schema's own field. Confirmed live at this cell (state-hook-action-operand), so the defect is a property of the check itself, not of one syntactic position. This is a distinct finding from the Part 3 false-proof hazard (authored-expressiveness-gaps.md) — it is not a business rule used as a premise; it is the modifier check itself failing to inspect both operands — so it carries no false-proof-hazard exclusion and is reported as its own soundness gap.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- src/Precept/Language/Operations.cs:882 — code
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — matrix: Fault minting: catalog-stamped at evaluation sites

## g19/guard-row-and-hook — Guard cluster — transition-row-guard and state-hook-guard (non-minting)

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- src/Precept/Language/Operations.cs:890 — code

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | ChoiceGreaterThanOrEqualChoice — choice >= choice |
| evaluation site category | transition-row-guard |
| type family | primitive |

### What must be proven

Obligation: StatusA >= StatusB

Weakest precondition: StatusA is declared ordered and StatusB is declared ordered

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| StatusA | the comparison's left (first) choice-typed operand |
| StatusB | the comparison's right (second) choice-typed operand |
| >= | one of <, <=, >, >= over two choice-typed operands — the catalog's four ChoiceXChoice operations, all carrying the identical `ModifierRequirement(Ordered)` shape (Operations.cs:866-897) |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet — comparison-direction flip: `StatusA < StatusB` is normal-form-equal to `StatusB > StatusA`, which is why all four operators mint the identical obligation (both operands declared ordered), just paired into two normal forms (strict / inclusive)

### Which premise classes can discharge it

Applicable classes: (a).

The applicable-class set is {(a)} at this evaluation-site category, and — the one respect in which this whole group differs from every other authored fault group — it is {(a)} at EVERY evaluation-site category in the region, not just this one: the fact consumed (the `ordered` modifier on both operands) is a static declaration attribute, not a runtime value bound, so which site the comparison sits in has no bearing on which premise classes could discharge it. Classes (b)/(c)/(d) supply argument bounds, guard truth, and pre-state value facts respectively — none of which bears on whether a choice TYPE was declared `ordered`. That derivation is the cell's own (matrix § Vocabulary, premise classes; disposition-pass authoring notes for group g19).

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: both comparison operands' declared modifier sets contain `ordered` -> the choice-ordering operator is well-defined; the catalog's `ModifierRequirement(Subject: ParamSubject(PChoice), Required: Ordered)` (Operations.cs:866-897) resolved against each operand's own field/arg/bound-variable declaration
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Look up `ordered` in each operand's own declared modifier set (a finite set per field, event-arg, or quantifier-bound-variable declaration). Both present discharges; either absent rejects, naming class (a) and the specific missing operand(s).

- src/Precept/Language/Operations.cs:866 — code: ChoiceLessThanChoice — ProofRequirements: [ModifierRequirement(ParamSubject(PChoice), Ordered, "Both choice operands must be declared ordered")]
- src/Precept/Language/Operations.cs:874 — code: ChoiceGreaterThanChoice — identical requirement shape
- src/Precept/Language/Operations.cs:882 — code: ChoiceLessThanOrEqualChoice — identical requirement shape
- src/Precept/Language/Operations.cs:890 — code: ChoiceGreaterThanOrEqualChoice — identical requirement shape
- src/Precept/Language/ProofRequirement.cs:148 — code: ModifierRequirement doc comment: "When both operands share the same ParameterMeta reference (as with choice ordering operations), the requirement applies to all matching operand positions" — the catalog's own statement of intent that both operands are meant to be checked
- src/Precept/Language/Modifiers.cs:63 — code: ModifierKind.Ordered — ModifierCategory.Structural, no ProofSatisfactions entries (unlike Nonnegative/Positive/Nonzero, which carry ProofSatisfaction.Numeric) — the catalog's own signal that this modifier is not a value-bound fact
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: premise class (a): field modifiers
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row: catalog-declared safety precondition at the evaluation site; premise classes (b)/(c)/(a) per catalog ProofSatisfactions

### What the failing diagnostic must suggest

- For class (a): declare `ordered` on both of <WP>'s choice-typed operands
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept ChoiceOrderGuardBase

field StatusA as choice of string("Low", "Medium", "High")
field StatusB as choice of string("Low", "Medium", "High")

state Active initial
state Done terminal

event Open initial
event Check
event Finish

on Open
    -> set StatusA = "Low"
    -> set StatusB = "Low"

from Active on Check when StatusA >= StatusB
    -> no transition

from Active on Finish
    -> transition Done
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*both-ordered* — other

- Addition: replace `field StatusA as choice of string("Low", "Medium", "High")` with `field StatusA as choice of string("Low", "Medium", "High") ordered`; and replace `field StatusB as choice of string("Low", "Medium", "High")` with `field StatusB as choice of string("Low", "Medium", "High") ordered`
- Premise classes: (a)
- Derivation: both operands' declared modifier sets contain `ordered` -> ModifierRequirement(Ordered) resolves true for both -> the comparison is well-defined
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Measured at HEAD (commit e1a14d91, 2026-07-21, Precept.MatrixTools): this evaluation-site category mints no fault obligation at all — the base itself compiles with zero diagnostics regardless of whether either operand is declared `ordered`. A clean compile here records the site's failure to mint, not confirmation that this discharge is accepted.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` block is given: the schema's `application` discriminated union offers only `row-guard` and `event-arg-declarations`, and this addition edits a plain field, event-arg, or collection-inner-type declaration (or, for the quantifier cell, a quantifier-bound variable's source collection) directly — a locus the DU does not yet name. Recorded as a schema gap rather than forced into either existing locus (same gap already flagged in fault-3/fault-5 for field-declaration additions).

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| both-ordered | replace `field StatusA as choice of string("Low", "Medium", "High")` with `field StatusA as choice of string("Low", "Medium", "High") ordered` | Removing `ordered` from the right (second) operand while the left (first) operand keeps it still leaves one operand undeclared -> the modifier lookup still fails for that operand -> must still reject, same obligation. | reject, naming the same obligation |

### Respellability

- Resolution: cell-override

- Overrides the family-wide numeric respellability paragraph (which is about algebraic rearrangement of value bounds and does not apply here — there is no family-wide verdict yet written for the fault case shape's modifier-discharge groups). There is no sound-but-unprovable band for this obligation: unlike a numeric bound, `ordered` has no partial-credit spelling (no per-term conjunct, no alternate bound) — a comparison on a choice type that is not declared `ordered` is not merely unprovable, it is not well-formed (the type declares no ordering; `<` would be arbitrary, per `ordered`'s own hover text, 'ordered by declaration sequence', Modifiers.cs:66). So every sound program in this cell's class already IS in the licensed form (`ordered` declared on both operands); there is nothing to respell.

**What the sources leave unstated or ambiguous here**

- MISSING RULE, central to this file: the matrix's closed § Validity arguments list (seven named arguments) has no argument that argues a discharge whose entire premise is a declared-modifier PRESENCE fact unconnected to a runtime value bound. The seven stated arguments (Guard normal-form match, Arg-bound interval arithmetic, Inductive hypothesis plus sign monotonicity, Literal constant-fold for defaults, Establishment over defaults, Guard-fact substitution for relational rules, Vacuity by activation) are all, in their actual written content, about intervals, guard truth, literal folding, or activation vacuity — none of them is about a static declaration attribute that makes an operator well-defined independent of any runtime value. `ordered` is not enforced on a VALUE at ingress the way a `positive`/`max` bound is (there is no runtime check being preserved through evaluation); it is a fact about the field's TYPE, checked once, structurally, with nothing further for a validity argument to preserve across evaluation. The catalog's own structure corroborates this: `ModifierKind.Ordered` carries no `ProofSatisfactions` entry (Modifiers.cs:63), unlike the numeric sign modifiers the existing arguments were written for. This group is the ONLY group in the fault family's defined region whose discharge is a bare modifier lookup rather than an interval/guard/literal derivation (verified against the disposition map: `requirementKind: "Modifier"` appears at no other catalog site in the fault family). Every discharge-contract entry below cites "Arg-bound interval arithmetic" because the schema's `validityArguments` enum requires one of the seven closed names and that is the least-remote of the seven (it is the argument this same corpus already stretches furthest, from event-arg modifiers to field modifiers, for other groups' class-(a) discharges) — but this citation is recorded as a placeholder, not as an adequate argument. The owner needs to author a new validity argument for declaration-attribute-only discharges (or rule that this requirement kind is type-checking, not proof surface, and does not belong in the matrix's proof-obligation product at all) before any cell in this file can ratify. Nothing here should be read as claiming that gap closed.
- This cell stands for two evaluation-site categories in the region: transition-row-guard (tested directly, program below) and state-hook-guard, which the disposition-pass authoring notes place in the same premise tier (both are a row/hook's own `when` guard) and which the fault-family's general measurement already established mints nothing at HEAD (docs/Working/obligation-discharge-matrix-2026-07-19-cells/fault-3-division-primitive-guard-positions.cells.json records the identical no-mint finding for the numeric division case at every guard category). Not independently re-run here for state-hook-guard; citing the general finding rather than re-measuring, per the note's own citation duty.
- Live-verified at HEAD, commit e1a14d91, 2026-07-21: base, discharge, and near-miss all compile with zero diagnostics for `when StatusA >= StatusB` guarding a transition row — the guard is never evaluated for fault obligations at all (open design hole: nothing detects an obligation that was never minted, matrix § The cell). `expected: reject` states the model's committed power, not what the run produced.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- src/Precept/Language/Operations.cs:890 — code
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — matrix: Fault minting: catalog-stamped at evaluation sites

## g19/guard-access-and-activation — Guard cluster — access-mode, ensure-activation, and rule-activation guards (non-minting)

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- src/Precept/Language/Operations.cs:882 — code

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | ChoiceLessThanOrEqualChoice — choice <= choice |
| evaluation site category | access-mode-guard |
| type family | primitive |

### What must be proven

Obligation: StatusA <= StatusB

Weakest precondition: StatusA is declared ordered and StatusB is declared ordered

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| StatusA | the comparison's left (first) choice-typed operand |
| StatusB | the comparison's right (second) choice-typed operand |
| <= | one of <, <=, >, >= over two choice-typed operands — the catalog's four ChoiceXChoice operations, all carrying the identical `ModifierRequirement(Ordered)` shape (Operations.cs:866-897) |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet — comparison-direction flip: `StatusA < StatusB` is normal-form-equal to `StatusB > StatusA`, which is why all four operators mint the identical obligation (both operands declared ordered), just paired into two normal forms (strict / inclusive)

### Which premise classes can discharge it

Applicable classes: (a).

The applicable-class set is {(a)} at this evaluation-site category, and — the one respect in which this whole group differs from every other authored fault group — it is {(a)} at EVERY evaluation-site category in the region, not just this one: the fact consumed (the `ordered` modifier on both operands) is a static declaration attribute, not a runtime value bound, so which site the comparison sits in has no bearing on which premise classes could discharge it. Classes (b)/(c)/(d) supply argument bounds, guard truth, and pre-state value facts respectively — none of which bears on whether a choice TYPE was declared `ordered`. That derivation is the cell's own (matrix § Vocabulary, premise classes; disposition-pass authoring notes for group g19).

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: both comparison operands' declared modifier sets contain `ordered` -> the choice-ordering operator is well-defined; the catalog's `ModifierRequirement(Subject: ParamSubject(PChoice), Required: Ordered)` (Operations.cs:866-897) resolved against each operand's own field/arg/bound-variable declaration
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Look up `ordered` in each operand's own declared modifier set (a finite set per field, event-arg, or quantifier-bound-variable declaration). Both present discharges; either absent rejects, naming class (a) and the specific missing operand(s).

- src/Precept/Language/Operations.cs:866 — code: ChoiceLessThanChoice — ProofRequirements: [ModifierRequirement(ParamSubject(PChoice), Ordered, "Both choice operands must be declared ordered")]
- src/Precept/Language/Operations.cs:874 — code: ChoiceGreaterThanChoice — identical requirement shape
- src/Precept/Language/Operations.cs:882 — code: ChoiceLessThanOrEqualChoice — identical requirement shape
- src/Precept/Language/Operations.cs:890 — code: ChoiceGreaterThanOrEqualChoice — identical requirement shape
- src/Precept/Language/ProofRequirement.cs:148 — code: ModifierRequirement doc comment: "When both operands share the same ParameterMeta reference (as with choice ordering operations), the requirement applies to all matching operand positions" — the catalog's own statement of intent that both operands are meant to be checked
- src/Precept/Language/Modifiers.cs:63 — code: ModifierKind.Ordered — ModifierCategory.Structural, no ProofSatisfactions entries (unlike Nonnegative/Positive/Nonzero, which carry ProofSatisfaction.Numeric) — the catalog's own signal that this modifier is not a value-bound fact
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: premise class (a): field modifiers
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row: catalog-declared safety precondition at the evaluation site; premise classes (b)/(c)/(a) per catalog ProofSatisfactions

### What the failing diagnostic must suggest

- For class (a): declare `ordered` on both of <WP>'s choice-typed operands
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept ChoiceOrderAccessModeBase

field StatusA as choice of string("Low", "Medium", "High") default "Low" editable
field StatusB as choice of string("Low", "Medium", "High") default "Low" editable
field Note as string optional editable

state Draft initial
state Done terminal

event Open initial
event Finish

on Open
    -> set StatusA = "Low"
    -> set StatusB = "Low"

in Draft when StatusA <= StatusB modify Note readonly

from Draft on Finish
    -> transition Done
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*both-ordered* — other

- Addition: replace `field StatusA as choice of string("Low", "Medium", "High") default "Low" editable` with `field StatusA as choice of string("Low", "Medium", "High") ordered default "Low" editable`; and replace `field StatusB as choice of string("Low", "Medium", "High") default "Low" editable` with `field StatusB as choice of string("Low", "Medium", "High") ordered default "Low" editable`
- Premise classes: (a)
- Derivation: both operands' declared modifier sets contain `ordered` -> ModifierRequirement(Ordered) resolves true for both -> the comparison is well-defined
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Measured at HEAD (commit e1a14d91, 2026-07-21, Precept.MatrixTools): this evaluation-site category mints no fault obligation at all — the base itself compiles with zero diagnostics regardless of whether either operand is declared `ordered`. A clean compile here records the site's failure to mint, not confirmation that this discharge is accepted.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` block is given: the schema's `application` discriminated union offers only `row-guard` and `event-arg-declarations`, and this addition edits a plain field, event-arg, or collection-inner-type declaration (or, for the quantifier cell, a quantifier-bound variable's source collection) directly — a locus the DU does not yet name. Recorded as a schema gap rather than forced into either existing locus (same gap already flagged in fault-3/fault-5 for field-declaration additions).

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| both-ordered | replace `field StatusA as choice of string("Low", "Medium", "High") default "Low" editable` with `field StatusA as choice of string("Low", "Medium", "High") ordered default "Low" editable` | Removing `ordered` from the right (second) operand while the left (first) operand keeps it still leaves one operand undeclared -> the modifier lookup still fails for that operand -> must still reject, same obligation. | reject, naming the same obligation |

### Respellability

- Resolution: cell-override

- Overrides the family-wide numeric respellability paragraph (which is about algebraic rearrangement of value bounds and does not apply here — there is no family-wide verdict yet written for the fault case shape's modifier-discharge groups). There is no sound-but-unprovable band for this obligation: unlike a numeric bound, `ordered` has no partial-credit spelling (no per-term conjunct, no alternate bound) — a comparison on a choice type that is not declared `ordered` is not merely unprovable, it is not well-formed (the type declares no ordering; `<` would be arbitrary, per `ordered`'s own hover text, 'ordered by declaration sequence', Modifiers.cs:66). So every sound program in this cell's class already IS in the licensed form (`ordered` declared on both operands); there is nothing to respell.

**What the sources leave unstated or ambiguous here**

- MISSING RULE, central to this file: the matrix's closed § Validity arguments list (seven named arguments) has no argument that argues a discharge whose entire premise is a declared-modifier PRESENCE fact unconnected to a runtime value bound. The seven stated arguments (Guard normal-form match, Arg-bound interval arithmetic, Inductive hypothesis plus sign monotonicity, Literal constant-fold for defaults, Establishment over defaults, Guard-fact substitution for relational rules, Vacuity by activation) are all, in their actual written content, about intervals, guard truth, literal folding, or activation vacuity — none of them is about a static declaration attribute that makes an operator well-defined independent of any runtime value. `ordered` is not enforced on a VALUE at ingress the way a `positive`/`max` bound is (there is no runtime check being preserved through evaluation); it is a fact about the field's TYPE, checked once, structurally, with nothing further for a validity argument to preserve across evaluation. The catalog's own structure corroborates this: `ModifierKind.Ordered` carries no `ProofSatisfactions` entry (Modifiers.cs:63), unlike the numeric sign modifiers the existing arguments were written for. This group is the ONLY group in the fault family's defined region whose discharge is a bare modifier lookup rather than an interval/guard/literal derivation (verified against the disposition map: `requirementKind: "Modifier"` appears at no other catalog site in the fault family). Every discharge-contract entry below cites "Arg-bound interval arithmetic" because the schema's `validityArguments` enum requires one of the seven closed names and that is the least-remote of the seven (it is the argument this same corpus already stretches furthest, from event-arg modifiers to field modifiers, for other groups' class-(a) discharges) — but this citation is recorded as a placeholder, not as an adequate argument. The owner needs to author a new validity argument for declaration-attribute-only discharges (or rule that this requirement kind is type-checking, not proof surface, and does not belong in the matrix's proof-obligation product at all) before any cell in this file can ratify. Nothing here should be read as claiming that gap closed.
- This cell stands for three evaluation-site categories in the region: access-mode-guard (tested directly, program below), ensure-activation-guard, and rule-activation-guard — the disposition pass's 'guard' premise tier. All three are `when`-guards outside a row/hook (an `in S when ... modify`, an `in S when ... ensure`, and a `rule ... when ...` activation guard respectively); the fault family's general guard-position measurement (fault-3, cited above) found no minting at any of the five guard categories for the numeric case, and this file's own direct measurement of two guard categories (this one and transition-row-guard, cell g19/guard-row-and-hook) agrees. The remaining two (ensure-activation-guard, rule-activation-guard) are not independently re-run here; generalized from the two measured guard categories plus the fault-3 precedent rather than re-measured one by one.
- Live-verified at HEAD, commit e1a14d91, 2026-07-21: base, discharge, and near-miss all compile with zero diagnostics for `in Draft when StatusA <= StatusB modify Note readonly` — the access-mode guard is never evaluated for fault obligations at all.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- src/Precept/Language/Operations.cs:882 — code
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — matrix: Fault minting: catalog-stamped at evaluation sites

## g19/message-interpolation — Message cluster — reject-message and constraint-rationale interpolation (non-minting)

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- src/Precept/Language/Operations.cs:882 — code

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | ChoiceLessThanOrEqualChoice — choice <= choice |
| evaluation site category | reject-message-interpolation |
| type family | primitive |

### What must be proven

Obligation: StatusA <= StatusB

Weakest precondition: StatusA is declared ordered and StatusB is declared ordered

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| StatusA | the comparison's left (first) choice-typed operand |
| StatusB | the comparison's right (second) choice-typed operand |
| <= | one of <, <=, >, >= over two choice-typed operands — the catalog's four ChoiceXChoice operations, all carrying the identical `ModifierRequirement(Ordered)` shape (Operations.cs:866-897) |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet — comparison-direction flip: `StatusA < StatusB` is normal-form-equal to `StatusB > StatusA`, which is why all four operators mint the identical obligation (both operands declared ordered), just paired into two normal forms (strict / inclusive)

### Which premise classes can discharge it

Applicable classes: (a).

The applicable-class set is {(a)} at this evaluation-site category, and — the one respect in which this whole group differs from every other authored fault group — it is {(a)} at EVERY evaluation-site category in the region, not just this one: the fact consumed (the `ordered` modifier on both operands) is a static declaration attribute, not a runtime value bound, so which site the comparison sits in has no bearing on which premise classes could discharge it. Classes (b)/(c)/(d) supply argument bounds, guard truth, and pre-state value facts respectively — none of which bears on whether a choice TYPE was declared `ordered`. That derivation is the cell's own (matrix § Vocabulary, premise classes; disposition-pass authoring notes for group g19).

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: both comparison operands' declared modifier sets contain `ordered` -> the choice-ordering operator is well-defined; the catalog's `ModifierRequirement(Subject: ParamSubject(PChoice), Required: Ordered)` (Operations.cs:866-897) resolved against each operand's own field/arg/bound-variable declaration
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Look up `ordered` in each operand's own declared modifier set (a finite set per field, event-arg, or quantifier-bound-variable declaration). Both present discharges; either absent rejects, naming class (a) and the specific missing operand(s).

- src/Precept/Language/Operations.cs:866 — code: ChoiceLessThanChoice — ProofRequirements: [ModifierRequirement(ParamSubject(PChoice), Ordered, "Both choice operands must be declared ordered")]
- src/Precept/Language/Operations.cs:874 — code: ChoiceGreaterThanChoice — identical requirement shape
- src/Precept/Language/Operations.cs:882 — code: ChoiceLessThanOrEqualChoice — identical requirement shape
- src/Precept/Language/Operations.cs:890 — code: ChoiceGreaterThanOrEqualChoice — identical requirement shape
- src/Precept/Language/ProofRequirement.cs:148 — code: ModifierRequirement doc comment: "When both operands share the same ParameterMeta reference (as with choice ordering operations), the requirement applies to all matching operand positions" — the catalog's own statement of intent that both operands are meant to be checked
- src/Precept/Language/Modifiers.cs:63 — code: ModifierKind.Ordered — ModifierCategory.Structural, no ProofSatisfactions entries (unlike Nonnegative/Positive/Nonzero, which carry ProofSatisfaction.Numeric) — the catalog's own signal that this modifier is not a value-bound fact
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: premise class (a): field modifiers
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row: catalog-declared safety precondition at the evaluation site; premise classes (b)/(c)/(a) per catalog ProofSatisfactions

### What the failing diagnostic must suggest

- For class (a): declare `ordered` on both of <WP>'s choice-typed operands
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept ChoiceOrderMsgBase

field StatusA as choice of string("Low", "Medium", "High")  default "Low" editable
field StatusB as choice of string("Low", "Medium", "High")  default "Low" editable

state Active initial
state Done terminal

event Open initial
event Check(Confirm as boolean)
event Finish

on Open
    -> set StatusA = "Low"

from Active on Check when Check.Confirm
    -> no transition
from Active on Check
    -> reject "StatusA at most StatusB: {StatusA <= StatusB}"

from Active on Finish
    -> transition Done
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*both-ordered* — other

- Addition: replace `field StatusA as choice of string("Low", "Medium", "High")  default "Low" editable` with `field StatusA as choice of string("Low", "Medium", "High") ordered default "Low" editable`; and replace `field StatusB as choice of string("Low", "Medium", "High")  default "Low" editable` with `field StatusB as choice of string("Low", "Medium", "High") ordered default "Low" editable`
- Premise classes: (a)
- Derivation: both operands' declared modifier sets contain `ordered` -> ModifierRequirement(Ordered) resolves true for both -> the comparison is well-defined
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Measured at HEAD (commit e1a14d91, 2026-07-21, Precept.MatrixTools): this evaluation-site category mints no fault obligation at all — the base itself compiles with zero diagnostics regardless of whether either operand is declared `ordered`. A clean compile here records the site's failure to mint, not confirmation that this discharge is accepted.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` block is given: the schema's `application` discriminated union offers only `row-guard` and `event-arg-declarations`, and this addition edits a plain field, event-arg, or collection-inner-type declaration (or, for the quantifier cell, a quantifier-bound variable's source collection) directly — a locus the DU does not yet name. Recorded as a schema gap rather than forced into either existing locus (same gap already flagged in fault-3/fault-5 for field-declaration additions).

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| both-ordered | replace `field StatusA as choice of string("Low", "Medium", "High")  default "Low" editable` with `field StatusA as choice of string("Low", "Medium", "High") ordered default "Low" editable` | Removing `ordered` from the right (second) operand while the left (first) operand keeps it still leaves one operand undeclared -> the modifier lookup still fails for that operand -> must still reject, same obligation. | reject, naming the same obligation |

### Respellability

- Resolution: cell-override

- Overrides the family-wide numeric respellability paragraph (which is about algebraic rearrangement of value bounds and does not apply here — there is no family-wide verdict yet written for the fault case shape's modifier-discharge groups). There is no sound-but-unprovable band for this obligation: unlike a numeric bound, `ordered` has no partial-credit spelling (no per-term conjunct, no alternate bound) — a comparison on a choice type that is not declared `ordered` is not merely unprovable, it is not well-formed (the type declares no ordering; `<` would be arbitrary, per `ordered`'s own hover text, 'ordered by declaration sequence', Modifiers.cs:66). So every sound program in this cell's class already IS in the licensed form (`ordered` declared on both operands); there is nothing to respell.

**What the sources leave unstated or ambiguous here**

- MISSING RULE, central to this file: the matrix's closed § Validity arguments list (seven named arguments) has no argument that argues a discharge whose entire premise is a declared-modifier PRESENCE fact unconnected to a runtime value bound. The seven stated arguments (Guard normal-form match, Arg-bound interval arithmetic, Inductive hypothesis plus sign monotonicity, Literal constant-fold for defaults, Establishment over defaults, Guard-fact substitution for relational rules, Vacuity by activation) are all, in their actual written content, about intervals, guard truth, literal folding, or activation vacuity — none of them is about a static declaration attribute that makes an operator well-defined independent of any runtime value. `ordered` is not enforced on a VALUE at ingress the way a `positive`/`max` bound is (there is no runtime check being preserved through evaluation); it is a fact about the field's TYPE, checked once, structurally, with nothing further for a validity argument to preserve across evaluation. The catalog's own structure corroborates this: `ModifierKind.Ordered` carries no `ProofSatisfactions` entry (Modifiers.cs:63), unlike the numeric sign modifiers the existing arguments were written for. This group is the ONLY group in the fault family's defined region whose discharge is a bare modifier lookup rather than an interval/guard/literal derivation (verified against the disposition map: `requirementKind: "Modifier"` appears at no other catalog site in the fault family). Every discharge-contract entry below cites "Arg-bound interval arithmetic" because the schema's `validityArguments` enum requires one of the seven closed names and that is the least-remote of the seven (it is the argument this same corpus already stretches furthest, from event-arg modifiers to field modifiers, for other groups' class-(a) discharges) — but this citation is recorded as a placeholder, not as an adequate argument. The owner needs to author a new validity argument for declaration-attribute-only discharges (or rule that this requirement kind is type-checking, not proof surface, and does not belong in the matrix's proof-obligation product at all) before any cell in this file can ratify. Nothing here should be read as claiming that gap closed.
- This cell stands for both evaluation-site categories in the region: reject-message-interpolation (tested directly, program below) and constraint-rationale-interpolation (a `rule ... because "{...}"` form, tested separately — see the file-level notes for that program). Both are 'interpolation positions'; the fault family's general measurement found neither mints at HEAD for the numeric case (fault-6-division-primitive-message-interpolation.cells.json), and this file's own direct measurement of both agrees.
- Live-verified at HEAD, commit e1a14d91, 2026-07-21: `reject "StatusA at most StatusB: {StatusA <= StatusB}"` compiles with zero diagnostics regardless of whether either operand is `ordered` — the comparison inside the interpolation hole is never evaluated for fault obligations.
- Constraint-rationale-interpolation companion measurement (not this cell's primary program, recorded here rather than as a second cell): `rule Flag because "StatusA at most StatusB: {StatusA <= StatusB}"` (Flag default true) — base, both-ordered, and near-miss all compile with zero diagnostics identically. Live-verified, same commit/date/tool.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- src/Precept/Language/Operations.cs:882 — code
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — matrix: Fault minting: catalog-stamped at evaluation sites

## g19/constraint-condition — Constraint cluster — rule condition, event-ensure, and state-ensure conditions

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- src/Precept/Language/Operations.cs:882 — code

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | ChoiceLessThanOrEqualChoice — choice <= choice |
| evaluation site category | rule-condition |
| type family | primitive |

### What must be proven

Obligation: StatusA <= StatusB

Weakest precondition: StatusA is declared ordered and StatusB is declared ordered

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| StatusA | the comparison's left (first) choice-typed operand |
| StatusB | the comparison's right (second) choice-typed operand |
| <= | one of <, <=, >, >= over two choice-typed operands — the catalog's four ChoiceXChoice operations, all carrying the identical `ModifierRequirement(Ordered)` shape (Operations.cs:866-897) |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet — comparison-direction flip: `StatusA < StatusB` is normal-form-equal to `StatusB > StatusA`, which is why all four operators mint the identical obligation (both operands declared ordered), just paired into two normal forms (strict / inclusive)

### Which premise classes can discharge it

Applicable classes: (a).

The applicable-class set is {(a)} at this evaluation-site category, and — the one respect in which this whole group differs from every other authored fault group — it is {(a)} at EVERY evaluation-site category in the region, not just this one: the fact consumed (the `ordered` modifier on both operands) is a static declaration attribute, not a runtime value bound, so which site the comparison sits in has no bearing on which premise classes could discharge it. Classes (b)/(c)/(d) supply argument bounds, guard truth, and pre-state value facts respectively — none of which bears on whether a choice TYPE was declared `ordered`. That derivation is the cell's own (matrix § Vocabulary, premise classes; disposition-pass authoring notes for group g19).

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: both comparison operands' declared modifier sets contain `ordered` -> the choice-ordering operator is well-defined; the catalog's `ModifierRequirement(Subject: ParamSubject(PChoice), Required: Ordered)` (Operations.cs:866-897) resolved against each operand's own field/arg/bound-variable declaration
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Look up `ordered` in each operand's own declared modifier set (a finite set per field, event-arg, or quantifier-bound-variable declaration). Both present discharges; either absent rejects, naming class (a) and the specific missing operand(s).

- src/Precept/Language/Operations.cs:866 — code: ChoiceLessThanChoice — ProofRequirements: [ModifierRequirement(ParamSubject(PChoice), Ordered, "Both choice operands must be declared ordered")]
- src/Precept/Language/Operations.cs:874 — code: ChoiceGreaterThanChoice — identical requirement shape
- src/Precept/Language/Operations.cs:882 — code: ChoiceLessThanOrEqualChoice — identical requirement shape
- src/Precept/Language/Operations.cs:890 — code: ChoiceGreaterThanOrEqualChoice — identical requirement shape
- src/Precept/Language/ProofRequirement.cs:148 — code: ModifierRequirement doc comment: "When both operands share the same ParameterMeta reference (as with choice ordering operations), the requirement applies to all matching operand positions" — the catalog's own statement of intent that both operands are meant to be checked
- src/Precept/Language/Modifiers.cs:63 — code: ModifierKind.Ordered — ModifierCategory.Structural, no ProofSatisfactions entries (unlike Nonnegative/Positive/Nonzero, which carry ProofSatisfaction.Numeric) — the catalog's own signal that this modifier is not a value-bound fact
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: premise class (a): field modifiers
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row: catalog-declared safety precondition at the evaluation site; premise classes (b)/(c)/(a) per catalog ProofSatisfactions

### What the failing diagnostic must suggest

- For class (a): declare `ordered` on both of <WP>'s choice-typed operands
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept ChoiceOrderRuleCondBase

field StatusA as choice of string("Low", "Medium", "High") default "Low" editable
field StatusB as choice of string("Low", "Medium", "High") default "Low" editable

rule StatusA <= StatusB because "StatusA must not exceed StatusB"
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*both-ordered* — other

- Addition: replace `field StatusA as choice of string("Low", "Medium", "High") default "Low" editable` with `field StatusA as choice of string("Low", "Medium", "High") ordered default "Low" editable`; and replace `field StatusB as choice of string("Low", "Medium", "High") default "Low" editable` with `field StatusB as choice of string("Low", "Medium", "High") ordered default "Low" editable`
- Premise classes: (a)
- Derivation: both operands' declared modifier sets contain `ordered` -> ModifierRequirement(Ordered) resolves true for both -> the comparison is well-defined
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` block is given: the schema's `application` discriminated union offers only `row-guard` and `event-arg-declarations`, and this addition edits a plain field, event-arg, or collection-inner-type declaration (or, for the quantifier cell, a quantifier-bound variable's source collection) directly — a locus the DU does not yet name. Recorded as a schema gap rather than forced into either existing locus (same gap already flagged in fault-3/fault-5 for field-declaration additions).

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| both-ordered | replace `field StatusA as choice of string("Low", "Medium", "High") default "Low" editable` with `field StatusA as choice of string("Low", "Medium", "High") ordered default "Low" editable` | Removing `ordered` from the right (second) operand while the left (first) operand keeps it still leaves one operand undeclared -> the modifier lookup still fails for that operand -> must still reject, same obligation. | reject, naming the same obligation |

### Respellability

- Resolution: cell-override

- Overrides the family-wide numeric respellability paragraph (which is about algebraic rearrangement of value bounds and does not apply here — there is no family-wide verdict yet written for the fault case shape's modifier-discharge groups). There is no sound-but-unprovable band for this obligation: unlike a numeric bound, `ordered` has no partial-credit spelling (no per-term conjunct, no alternate bound) — a comparison on a choice type that is not declared `ordered` is not merely unprovable, it is not well-formed (the type declares no ordering; `<` would be arbitrary, per `ordered`'s own hover text, 'ordered by declaration sequence', Modifiers.cs:66). So every sound program in this cell's class already IS in the licensed form (`ordered` declared on both operands); there is nothing to respell.

**What the sources leave unstated or ambiguous here**

- MISSING RULE, central to this file: the matrix's closed § Validity arguments list (seven named arguments) has no argument that argues a discharge whose entire premise is a declared-modifier PRESENCE fact unconnected to a runtime value bound. The seven stated arguments (Guard normal-form match, Arg-bound interval arithmetic, Inductive hypothesis plus sign monotonicity, Literal constant-fold for defaults, Establishment over defaults, Guard-fact substitution for relational rules, Vacuity by activation) are all, in their actual written content, about intervals, guard truth, literal folding, or activation vacuity — none of them is about a static declaration attribute that makes an operator well-defined independent of any runtime value. `ordered` is not enforced on a VALUE at ingress the way a `positive`/`max` bound is (there is no runtime check being preserved through evaluation); it is a fact about the field's TYPE, checked once, structurally, with nothing further for a validity argument to preserve across evaluation. The catalog's own structure corroborates this: `ModifierKind.Ordered` carries no `ProofSatisfactions` entry (Modifiers.cs:63), unlike the numeric sign modifiers the existing arguments were written for. This group is the ONLY group in the fault family's defined region whose discharge is a bare modifier lookup rather than an interval/guard/literal derivation (verified against the disposition map: `requirementKind: "Modifier"` appears at no other catalog site in the fault family). Every discharge-contract entry below cites "Arg-bound interval arithmetic" because the schema's `validityArguments` enum requires one of the seven closed names and that is the least-remote of the seven (it is the argument this same corpus already stretches furthest, from event-arg modifiers to field modifiers, for other groups' class-(a) discharges) — but this citation is recorded as a placeholder, not as an adequate argument. The owner needs to author a new validity argument for declaration-attribute-only discharges (or rule that this requirement kind is type-checking, not proof surface, and does not belong in the matrix's proof-obligation product at all) before any cell in this file can ratify. Nothing here should be read as claiming that gap closed.
- This cell stands for three evaluation-site categories in the region: rule-condition (tested directly, program below), event-ensure-condition, and state-ensure-condition — all three independently live-verified in this file to mint and to show the identical asymmetric-check pattern (see the companion programs recorded in the file-level notes), so this is a direct measurement across all three, not a generalization from one.
- Live-verified at HEAD, commit e1a14d91, 2026-07-21: `rule StatusA <= StatusB because "..."` (both fields defaulted to "Low", editable) rejects `UnprovedModifierRequirement: Cannot prove that 'StatusB' satisfies the required modifier 'Ordered' (used while evaluating rule at index 0)`; both-ordered compiles clean; the near-miss (StatusA ordered, StatusB not) still rejects with the same diagnostic.
- Event-ensure-condition companion measurement: `on Finish ensure StatusA <= StatusB because "must hold at Finish"` — base rejects `UnprovedModifierRequirement ... (used while evaluating ensure for 'Finish')`; both-ordered compiles clean; near-miss (StatusA ordered only) still rejects. Live-verified, same commit/date/tool.
- State-ensure-condition companion measurement: `in Done ensure StatusA <= StatusB because "must hold while Done"` — base rejects `UnprovedModifierRequirement ... (used while evaluating ensure for 'Done')`; both-ordered compiles clean; near-miss (StatusA ordered only) still rejects. Live-verified, same commit/date/tool.
- BUILT DEFECT, live-verified 2026-07-21 (commit e1a14d91, Precept.MatrixTools): HEAD's `Ordered`-modifier check is asymmetric. In every evaluation-site category tested in this file where the site mints at all, declaring `ordered` on only the operand that is syntactically SECOND (right of the comparison operator) and leaving the FIRST (left) operand undeclared compiles with ZERO diagnostics — the engine never checks the left operand's `Ordered` modifier at all, even though the catalog's own `ModifierRequirement` doc comment (ProofRequirement.cs:148) states the requirement is meant to apply to "all matching operand positions" when both operands share the same `ParameterMeta`. The formal near-miss below weakens the OTHER direction (right operand loses `ordered`, left keeps it) because that is the shape that still correctly rejects — matching the schema's near-miss contract. The direction that HEAD gets wrong is recorded here rather than as a formal near-miss, since the schema's `nearMissWitness.expected.outcome` is a fixed `reject` and this shape does not reject at HEAD; forcing it into that slot would misrepresent the schema's own field. Confirmed live at rule-condition, event-ensure-condition, and state-ensure-condition (each independently live-verified: StatusB ordered / StatusA not compiles clean in all three), so the defect is a property of the check itself, not of one syntactic position. This is a distinct finding from the Part 3 false-proof hazard (authored-expressiveness-gaps.md) — it is not a business rule used as a premise; it is the modifier check itself failing to inspect both operands — so it carries no false-proof-hazard exclusion and is reported as its own soundness gap.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- src/Precept/Language/Operations.cs:882 — code
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — matrix: Fault minting: catalog-stamped at evaluation sites

## g19/computed-field — Computed-field expression — IsAtMost <- StatusA <= StatusB

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- src/Precept/Language/Operations.cs:882 — code

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | ChoiceLessThanOrEqualChoice — choice <= choice |
| evaluation site category | computed-field-expression |
| type family | primitive |

### What must be proven

Obligation: StatusA <= StatusB

Weakest precondition: StatusA is declared ordered and StatusB is declared ordered

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| StatusA | the comparison's left (first) choice-typed operand |
| StatusB | the comparison's right (second) choice-typed operand |
| <= | one of <, <=, >, >= over two choice-typed operands — the catalog's four ChoiceXChoice operations, all carrying the identical `ModifierRequirement(Ordered)` shape (Operations.cs:866-897) |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet — comparison-direction flip: `StatusA < StatusB` is normal-form-equal to `StatusB > StatusA`, which is why all four operators mint the identical obligation (both operands declared ordered), just paired into two normal forms (strict / inclusive)

### Which premise classes can discharge it

Applicable classes: (a).

The applicable-class set is {(a)} at this evaluation-site category, and — the one respect in which this whole group differs from every other authored fault group — it is {(a)} at EVERY evaluation-site category in the region, not just this one: the fact consumed (the `ordered` modifier on both operands) is a static declaration attribute, not a runtime value bound, so which site the comparison sits in has no bearing on which premise classes could discharge it. Classes (b)/(c)/(d) supply argument bounds, guard truth, and pre-state value facts respectively — none of which bears on whether a choice TYPE was declared `ordered`. That derivation is the cell's own (matrix § Vocabulary, premise classes; disposition-pass authoring notes for group g19).

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: both comparison operands' declared modifier sets contain `ordered` -> the choice-ordering operator is well-defined; the catalog's `ModifierRequirement(Subject: ParamSubject(PChoice), Required: Ordered)` (Operations.cs:866-897) resolved against each operand's own field/arg/bound-variable declaration
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Look up `ordered` in each operand's own declared modifier set (a finite set per field, event-arg, or quantifier-bound-variable declaration). Both present discharges; either absent rejects, naming class (a) and the specific missing operand(s).

- src/Precept/Language/Operations.cs:866 — code: ChoiceLessThanChoice — ProofRequirements: [ModifierRequirement(ParamSubject(PChoice), Ordered, "Both choice operands must be declared ordered")]
- src/Precept/Language/Operations.cs:874 — code: ChoiceGreaterThanChoice — identical requirement shape
- src/Precept/Language/Operations.cs:882 — code: ChoiceLessThanOrEqualChoice — identical requirement shape
- src/Precept/Language/Operations.cs:890 — code: ChoiceGreaterThanOrEqualChoice — identical requirement shape
- src/Precept/Language/ProofRequirement.cs:148 — code: ModifierRequirement doc comment: "When both operands share the same ParameterMeta reference (as with choice ordering operations), the requirement applies to all matching operand positions" — the catalog's own statement of intent that both operands are meant to be checked
- src/Precept/Language/Modifiers.cs:63 — code: ModifierKind.Ordered — ModifierCategory.Structural, no ProofSatisfactions entries (unlike Nonnegative/Positive/Nonzero, which carry ProofSatisfaction.Numeric) — the catalog's own signal that this modifier is not a value-bound fact
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: premise class (a): field modifiers
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row: catalog-declared safety precondition at the evaluation site; premise classes (b)/(c)/(a) per catalog ProofSatisfactions

### What the failing diagnostic must suggest

- For class (a): declare `ordered` on both of <WP>'s choice-typed operands
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept ChoiceOrderComputedBase

field StatusA as choice of string("Low", "Medium", "High") default "Low" editable
field StatusB as choice of string("Low", "Medium", "High") default "Low" editable
field IsAtMost as boolean <- StatusA <= StatusB
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*both-ordered* — other

- Addition: replace `field StatusA as choice of string("Low", "Medium", "High") default "Low" editable` with `field StatusA as choice of string("Low", "Medium", "High") ordered default "Low" editable`; and replace `field StatusB as choice of string("Low", "Medium", "High") default "Low" editable` with `field StatusB as choice of string("Low", "Medium", "High") ordered default "Low" editable`
- Premise classes: (a)
- Derivation: both operands' declared modifier sets contain `ordered` -> ModifierRequirement(Ordered) resolves true for both -> the comparison is well-defined
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` block is given: the schema's `application` discriminated union offers only `row-guard` and `event-arg-declarations`, and this addition edits a plain field, event-arg, or collection-inner-type declaration (or, for the quantifier cell, a quantifier-bound variable's source collection) directly — a locus the DU does not yet name. Recorded as a schema gap rather than forced into either existing locus (same gap already flagged in fault-3/fault-5 for field-declaration additions).

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| both-ordered | replace `field StatusA as choice of string("Low", "Medium", "High") default "Low" editable` with `field StatusA as choice of string("Low", "Medium", "High") ordered default "Low" editable` | Removing `ordered` from the right (second) operand while the left (first) operand keeps it still leaves one operand undeclared -> the modifier lookup still fails for that operand -> must still reject, same obligation. | reject, naming the same obligation |

### Respellability

- Resolution: cell-override

- Overrides the family-wide numeric respellability paragraph (which is about algebraic rearrangement of value bounds and does not apply here — there is no family-wide verdict yet written for the fault case shape's modifier-discharge groups). There is no sound-but-unprovable band for this obligation: unlike a numeric bound, `ordered` has no partial-credit spelling (no per-term conjunct, no alternate bound) — a comparison on a choice type that is not declared `ordered` is not merely unprovable, it is not well-formed (the type declares no ordering; `<` would be arbitrary, per `ordered`'s own hover text, 'ordered by declaration sequence', Modifiers.cs:66). So every sound program in this cell's class already IS in the licensed form (`ordered` declared on both operands); there is nothing to respell.

**What the sources leave unstated or ambiguous here**

- MISSING RULE, central to this file: the matrix's closed § Validity arguments list (seven named arguments) has no argument that argues a discharge whose entire premise is a declared-modifier PRESENCE fact unconnected to a runtime value bound. The seven stated arguments (Guard normal-form match, Arg-bound interval arithmetic, Inductive hypothesis plus sign monotonicity, Literal constant-fold for defaults, Establishment over defaults, Guard-fact substitution for relational rules, Vacuity by activation) are all, in their actual written content, about intervals, guard truth, literal folding, or activation vacuity — none of them is about a static declaration attribute that makes an operator well-defined independent of any runtime value. `ordered` is not enforced on a VALUE at ingress the way a `positive`/`max` bound is (there is no runtime check being preserved through evaluation); it is a fact about the field's TYPE, checked once, structurally, with nothing further for a validity argument to preserve across evaluation. The catalog's own structure corroborates this: `ModifierKind.Ordered` carries no `ProofSatisfactions` entry (Modifiers.cs:63), unlike the numeric sign modifiers the existing arguments were written for. This group is the ONLY group in the fault family's defined region whose discharge is a bare modifier lookup rather than an interval/guard/literal derivation (verified against the disposition map: `requirementKind: "Modifier"` appears at no other catalog site in the fault family). Every discharge-contract entry below cites "Arg-bound interval arithmetic" because the schema's `validityArguments` enum requires one of the seven closed names and that is the least-remote of the seven (it is the argument this same corpus already stretches furthest, from event-arg modifiers to field modifiers, for other groups' class-(a) discharges) — but this citation is recorded as a placeholder, not as an adequate argument. The owner needs to author a new validity argument for declaration-attribute-only discharges (or rule that this requirement kind is type-checking, not proof surface, and does not belong in the matrix's proof-obligation product at all) before any cell in this file can ratify. Nothing here should be read as claiming that gap closed.
- Live-verified at HEAD, commit e1a14d91, 2026-07-21: `field IsAtMost as boolean <- StatusA <= StatusB` rejects `UnprovedModifierRequirement ... (used in the computed expression for field 'IsAtMost')`; both-ordered compiles clean; the near-miss (StatusA ordered only) still rejects.
- BUILT DEFECT, live-verified 2026-07-21 (commit e1a14d91, Precept.MatrixTools): HEAD's `Ordered`-modifier check is asymmetric. In every evaluation-site category tested in this file where the site mints at all, declaring `ordered` on only the operand that is syntactically SECOND (right of the comparison operator) and leaving the FIRST (left) operand undeclared compiles with ZERO diagnostics — the engine never checks the left operand's `Ordered` modifier at all, even though the catalog's own `ModifierRequirement` doc comment (ProofRequirement.cs:148) states the requirement is meant to apply to "all matching operand positions" when both operands share the same `ParameterMeta`. The formal near-miss below weakens the OTHER direction (right operand loses `ordered`, left keeps it) because that is the shape that still correctly rejects — matching the schema's near-miss contract. The direction that HEAD gets wrong is recorded here rather than as a formal near-miss, since the schema's `nearMissWitness.expected.outcome` is a fixed `reject` and this shape does not reject at HEAD; forcing it into that slot would misrepresent the schema's own field. Confirmed live at this cell (computed-field-expression), so the defect is a property of the check itself, not of one syntactic position. This is a distinct finding from the Part 3 false-proof hazard (authored-expressiveness-gaps.md) — it is not a business rule used as a premise; it is the modifier check itself failing to inspect both operands — so it carries no false-proof-hazard exclusion and is reported as its own soundness gap.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- src/Precept/Language/Operations.cs:882 — code
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — matrix: Fault minting: catalog-stamped at evaluation sites

## g19/quantifier-predicate — Quantifier predicate — each s in Statuses (s >= Threshold)

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- src/Precept/Language/Operations.cs:890 — code

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | ChoiceGreaterThanOrEqualChoice — choice >= choice |
| evaluation site category | quantifier-predicate |
| type family | primitive |

### What must be proven

Obligation: StatusA >= StatusB

Weakest precondition: StatusA is declared ordered and StatusB is declared ordered

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| StatusA | the comparison's left (first) choice-typed operand |
| StatusB | the comparison's right (second) choice-typed operand |
| >= | one of <, <=, >, >= over two choice-typed operands — the catalog's four ChoiceXChoice operations, all carrying the identical `ModifierRequirement(Ordered)` shape (Operations.cs:866-897) |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet — comparison-direction flip: `StatusA < StatusB` is normal-form-equal to `StatusB > StatusA`, which is why all four operators mint the identical obligation (both operands declared ordered), just paired into two normal forms (strict / inclusive)

### Which premise classes can discharge it

Applicable classes: (a).

The applicable-class set is {(a)} at this evaluation-site category, and — the one respect in which this whole group differs from every other authored fault group — it is {(a)} at EVERY evaluation-site category in the region, not just this one: the fact consumed (the `ordered` modifier on both operands) is a static declaration attribute, not a runtime value bound, so which site the comparison sits in has no bearing on which premise classes could discharge it. Classes (b)/(c)/(d) supply argument bounds, guard truth, and pre-state value facts respectively — none of which bears on whether a choice TYPE was declared `ordered`. That derivation is the cell's own (matrix § Vocabulary, premise classes; disposition-pass authoring notes for group g19).

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: both comparison operands' declared modifier sets contain `ordered` -> the choice-ordering operator is well-defined; the catalog's `ModifierRequirement(Subject: ParamSubject(PChoice), Required: Ordered)` (Operations.cs:866-897) resolved against each operand's own field/arg/bound-variable declaration
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Look up `ordered` in each operand's own declared modifier set (a finite set per field, event-arg, or quantifier-bound-variable declaration). Both present discharges; either absent rejects, naming class (a) and the specific missing operand(s).

- src/Precept/Language/Operations.cs:866 — code: ChoiceLessThanChoice — ProofRequirements: [ModifierRequirement(ParamSubject(PChoice), Ordered, "Both choice operands must be declared ordered")]
- src/Precept/Language/Operations.cs:874 — code: ChoiceGreaterThanChoice — identical requirement shape
- src/Precept/Language/Operations.cs:882 — code: ChoiceLessThanOrEqualChoice — identical requirement shape
- src/Precept/Language/Operations.cs:890 — code: ChoiceGreaterThanOrEqualChoice — identical requirement shape
- src/Precept/Language/ProofRequirement.cs:148 — code: ModifierRequirement doc comment: "When both operands share the same ParameterMeta reference (as with choice ordering operations), the requirement applies to all matching operand positions" — the catalog's own statement of intent that both operands are meant to be checked
- src/Precept/Language/Modifiers.cs:63 — code: ModifierKind.Ordered — ModifierCategory.Structural, no ProofSatisfactions entries (unlike Nonnegative/Positive/Nonzero, which carry ProofSatisfaction.Numeric) — the catalog's own signal that this modifier is not a value-bound fact
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: premise class (a): field modifiers
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row: catalog-declared safety precondition at the evaluation site; premise classes (b)/(c)/(a) per catalog ProofSatisfactions

### What the failing diagnostic must suggest

- For class (a): declare `ordered` on both of <WP>'s choice-typed operands
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept ChoiceOrderQuantBase

field Statuses as set of choice of string("Low", "Medium", "High")  editable
field Threshold as choice of string("Low", "Medium", "High")  default "Low" editable

rule each s in Statuses (s >= Threshold) because "no status below threshold"
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*both-ordered* — other

- Addition: replace `field Statuses as set of choice of string("Low", "Medium", "High")  editable` with `field Statuses as set of choice of string("Low", "Medium", "High") ordered editable`; and replace `field Threshold as choice of string("Low", "Medium", "High")  default "Low" editable` with `field Threshold as choice of string("Low", "Medium", "High") ordered default "Low" editable`
- Premise classes: (a)
- Derivation: both operands' declared modifier sets contain `ordered` -> ModifierRequirement(Ordered) resolves true for both -> the comparison is well-defined
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` block is given: the schema's `application` discriminated union offers only `row-guard` and `event-arg-declarations`, and this addition edits a plain field, event-arg, or collection-inner-type declaration (or, for the quantifier cell, a quantifier-bound variable's source collection) directly — a locus the DU does not yet name. Recorded as a schema gap rather than forced into either existing locus (same gap already flagged in fault-3/fault-5 for field-declaration additions).

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| both-ordered | replace `field Statuses as set of choice of string("Low", "Medium", "High")  editable` with `field Statuses as set of choice of string("Low", "Medium", "High") ordered editable` | Removing `ordered` from the right (second) operand while the left (first) operand keeps it still leaves one operand undeclared -> the modifier lookup still fails for that operand -> must still reject, same obligation. | reject, naming the same obligation |

### Respellability

- Resolution: cell-override

- Overrides the family-wide numeric respellability paragraph (which is about algebraic rearrangement of value bounds and does not apply here — there is no family-wide verdict yet written for the fault case shape's modifier-discharge groups). There is no sound-but-unprovable band for this obligation: unlike a numeric bound, `ordered` has no partial-credit spelling (no per-term conjunct, no alternate bound) — a comparison on a choice type that is not declared `ordered` is not merely unprovable, it is not well-formed (the type declares no ordering; `<` would be arbitrary, per `ordered`'s own hover text, 'ordered by declaration sequence', Modifiers.cs:66). So every sound program in this cell's class already IS in the licensed form (`ordered` declared on both operands); there is nothing to respell.

**What the sources leave unstated or ambiguous here**

- MISSING RULE, central to this file: the matrix's closed § Validity arguments list (seven named arguments) has no argument that argues a discharge whose entire premise is a declared-modifier PRESENCE fact unconnected to a runtime value bound. The seven stated arguments (Guard normal-form match, Arg-bound interval arithmetic, Inductive hypothesis plus sign monotonicity, Literal constant-fold for defaults, Establishment over defaults, Guard-fact substitution for relational rules, Vacuity by activation) are all, in their actual written content, about intervals, guard truth, literal folding, or activation vacuity — none of them is about a static declaration attribute that makes an operator well-defined independent of any runtime value. `ordered` is not enforced on a VALUE at ingress the way a `positive`/`max` bound is (there is no runtime check being preserved through evaluation); it is a fact about the field's TYPE, checked once, structurally, with nothing further for a validity argument to preserve across evaluation. The catalog's own structure corroborates this: `ModifierKind.Ordered` carries no `ProofSatisfactions` entry (Modifiers.cs:63), unlike the numeric sign modifiers the existing arguments were written for. This group is the ONLY group in the fault family's defined region whose discharge is a bare modifier lookup rather than an interval/guard/literal derivation (verified against the disposition map: `requirementKind: "Modifier"` appears at no other catalog site in the fault family). Every discharge-contract entry below cites "Arg-bound interval arithmetic" because the schema's `validityArguments` enum requires one of the seven closed names and that is the least-remote of the seven (it is the argument this same corpus already stretches furthest, from event-arg modifiers to field modifiers, for other groups' class-(a) discharges) — but this citation is recorded as a placeholder, not as an adequate argument. The owner needs to author a new validity argument for declaration-attribute-only discharges (or rule that this requirement kind is type-checking, not proof surface, and does not belong in the matrix's proof-obligation product at all) before any cell in this file can ratify. Nothing here should be read as claiming that gap closed.
- Live-verified at HEAD, commit e1a14d91, 2026-07-21: `rule each s in Statuses (s >= Threshold) because "..."` rejects `UnprovedModifierRequirement: Cannot prove that 'Threshold' satisfies the required modifier 'Ordered' (used while evaluating rule at index 0)`; both-ordered (Statuses' inner type AND Threshold both `ordered`) compiles clean; the near-miss (Statuses' inner type ordered, Threshold not) still rejects with the same diagnostic.
- Here the syntactically-'right' operand is `Threshold` (a plain field) and the syntactically-'left' operand is `s` (the quantifier-bound variable, whose modifier set is inherited from `Statuses`' inner type declaration) — the same left/right asymmetry as every other cell in this file, just with the left operand's `ordered` fact sourced from a collection's inner-type declaration rather than a plain field or event-arg declaration.
- Not adopted as an answer here, but recorded per the matrix's own open item: whether quantified constraints are proof surface at all is open elsewhere in the fault family (⧖ Q10, matrix line 312; the disposition-pass summary rides this on groups 4, 10, and 15). It does NOT ride on this group: the disposition map records this coordinate as `defined`, not `open`, and Q10 is about whether the quantified rule construct itself mints obligations at all — a question this cell's own live-verified base answers directly (it does mint, for this requirement kind). Flagged so the two questions are not conflated.
- BUILT DEFECT, live-verified 2026-07-21 (commit e1a14d91, Precept.MatrixTools): HEAD's `Ordered`-modifier check is asymmetric. In every evaluation-site category tested in this file where the site mints at all, declaring `ordered` on only the operand that is syntactically SECOND (right of the comparison operator) and leaving the FIRST (left) operand undeclared compiles with ZERO diagnostics — the engine never checks the left operand's `Ordered` modifier at all, even though the catalog's own `ModifierRequirement` doc comment (ProofRequirement.cs:148) states the requirement is meant to apply to "all matching operand positions" when both operands share the same `ParameterMeta`. The formal near-miss below weakens the OTHER direction (right operand loses `ordered`, left keeps it) because that is the shape that still correctly rejects — matching the schema's near-miss contract. The direction that HEAD gets wrong is recorded here rather than as a formal near-miss, since the schema's `nearMissWitness.expected.outcome` is a fixed `reject` and this shape does not reject at HEAD; forcing it into that slot would misrepresent the schema's own field. Confirmed live at this cell (quantifier-predicate), so the defect is a property of the check itself, not of one syntactic position. This is a distinct finding from the Part 3 false-proof hazard (authored-expressiveness-gaps.md) — it is not a business rule used as a premise; it is the modifier check itself failing to inspect both operands — so it carries no false-proof-hazard exclusion and is reported as its own soundness gap.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- src/Precept/Language/Operations.cs:890 — code
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — matrix: Fault minting: catalog-stamped at evaluation sites

## g19/declaration-value-expressions — Declaration cluster — field-default, field-modifier, and event-arg-modifier value expressions (non-minting)

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- src/Precept/Language/Operations.cs:882 — code

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | ChoiceLessThanOrEqualChoice — choice <= choice |
| evaluation site category | field-default-value-expression |
| type family | primitive |

### What must be proven

Obligation: StatusA <= StatusB

Weakest precondition: StatusA is declared ordered and StatusB is declared ordered

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| StatusA | the comparison's left (first) choice-typed operand |
| StatusB | the comparison's right (second) choice-typed operand |
| <= | one of <, <=, >, >= over two choice-typed operands — the catalog's four ChoiceXChoice operations, all carrying the identical `ModifierRequirement(Ordered)` shape (Operations.cs:866-897) |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet — comparison-direction flip: `StatusA < StatusB` is normal-form-equal to `StatusB > StatusA`, which is why all four operators mint the identical obligation (both operands declared ordered), just paired into two normal forms (strict / inclusive)

### Which premise classes can discharge it

Applicable classes: (a).

The applicable-class set is {(a)} at this evaluation-site category, and — the one respect in which this whole group differs from every other authored fault group — it is {(a)} at EVERY evaluation-site category in the region, not just this one: the fact consumed (the `ordered` modifier on both operands) is a static declaration attribute, not a runtime value bound, so which site the comparison sits in has no bearing on which premise classes could discharge it. Classes (b)/(c)/(d) supply argument bounds, guard truth, and pre-state value facts respectively — none of which bears on whether a choice TYPE was declared `ordered`. That derivation is the cell's own (matrix § Vocabulary, premise classes; disposition-pass authoring notes for group g19).

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: both comparison operands' declared modifier sets contain `ordered` -> the choice-ordering operator is well-defined; the catalog's `ModifierRequirement(Subject: ParamSubject(PChoice), Required: Ordered)` (Operations.cs:866-897) resolved against each operand's own field/arg/bound-variable declaration
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Look up `ordered` in each operand's own declared modifier set (a finite set per field, event-arg, or quantifier-bound-variable declaration). Both present discharges; either absent rejects, naming class (a) and the specific missing operand(s).

- src/Precept/Language/Operations.cs:866 — code: ChoiceLessThanChoice — ProofRequirements: [ModifierRequirement(ParamSubject(PChoice), Ordered, "Both choice operands must be declared ordered")]
- src/Precept/Language/Operations.cs:874 — code: ChoiceGreaterThanChoice — identical requirement shape
- src/Precept/Language/Operations.cs:882 — code: ChoiceLessThanOrEqualChoice — identical requirement shape
- src/Precept/Language/Operations.cs:890 — code: ChoiceGreaterThanOrEqualChoice — identical requirement shape
- src/Precept/Language/ProofRequirement.cs:148 — code: ModifierRequirement doc comment: "When both operands share the same ParameterMeta reference (as with choice ordering operations), the requirement applies to all matching operand positions" — the catalog's own statement of intent that both operands are meant to be checked
- src/Precept/Language/Modifiers.cs:63 — code: ModifierKind.Ordered — ModifierCategory.Structural, no ProofSatisfactions entries (unlike Nonnegative/Positive/Nonzero, which carry ProofSatisfaction.Numeric) — the catalog's own signal that this modifier is not a value-bound fact
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: premise class (a): field modifiers
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row: catalog-declared safety precondition at the evaluation site; premise classes (b)/(c)/(a) per catalog ProofSatisfactions

### What the failing diagnostic must suggest

- For class (a): declare `ordered` on both of <WP>'s choice-typed operands
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept ChoiceOrderFieldDefaultBase

field StatusA as choice of string("Low", "Medium", "High") default "Low" editable
field StatusB as choice of string("Low", "Medium", "High") default "Low" editable
field IsAtMost as boolean default StatusA <= StatusB editable
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*both-ordered* — other

- Addition: replace `field StatusA as choice of string("Low", "Medium", "High") default "Low" editable` with `field StatusA as choice of string("Low", "Medium", "High") ordered default "Low" editable`; and replace `field StatusB as choice of string("Low", "Medium", "High") default "Low" editable` with `field StatusB as choice of string("Low", "Medium", "High") ordered default "Low" editable`
- Premise classes: (a)
- Derivation: both operands' declared modifier sets contain `ordered` -> ModifierRequirement(Ordered) resolves true for both -> the comparison is well-defined
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Measured at HEAD (commit e1a14d91, 2026-07-21, Precept.MatrixTools): this evaluation-site category mints no fault obligation at all — the base itself compiles with zero diagnostics regardless of whether either operand is declared `ordered`. A clean compile here records the site's failure to mint, not confirmation that this discharge is accepted.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` block is given: the schema's `application` discriminated union offers only `row-guard` and `event-arg-declarations`, and this addition edits a plain field, event-arg, or collection-inner-type declaration (or, for the quantifier cell, a quantifier-bound variable's source collection) directly — a locus the DU does not yet name. Recorded as a schema gap rather than forced into either existing locus (same gap already flagged in fault-3/fault-5 for field-declaration additions).

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| both-ordered | replace `field StatusA as choice of string("Low", "Medium", "High") default "Low" editable` with `field StatusA as choice of string("Low", "Medium", "High") ordered default "Low" editable` | Removing `ordered` from the right (second) operand while the left (first) operand keeps it still leaves one operand undeclared -> the modifier lookup still fails for that operand -> must still reject, same obligation. | reject, naming the same obligation |

### Respellability

- Resolution: cell-override

- Overrides the family-wide numeric respellability paragraph (which is about algebraic rearrangement of value bounds and does not apply here — there is no family-wide verdict yet written for the fault case shape's modifier-discharge groups). There is no sound-but-unprovable band for this obligation: unlike a numeric bound, `ordered` has no partial-credit spelling (no per-term conjunct, no alternate bound) — a comparison on a choice type that is not declared `ordered` is not merely unprovable, it is not well-formed (the type declares no ordering; `<` would be arbitrary, per `ordered`'s own hover text, 'ordered by declaration sequence', Modifiers.cs:66). So every sound program in this cell's class already IS in the licensed form (`ordered` declared on both operands); there is nothing to respell.

**What the sources leave unstated or ambiguous here**

- MISSING RULE, central to this file: the matrix's closed § Validity arguments list (seven named arguments) has no argument that argues a discharge whose entire premise is a declared-modifier PRESENCE fact unconnected to a runtime value bound. The seven stated arguments (Guard normal-form match, Arg-bound interval arithmetic, Inductive hypothesis plus sign monotonicity, Literal constant-fold for defaults, Establishment over defaults, Guard-fact substitution for relational rules, Vacuity by activation) are all, in their actual written content, about intervals, guard truth, literal folding, or activation vacuity — none of them is about a static declaration attribute that makes an operator well-defined independent of any runtime value. `ordered` is not enforced on a VALUE at ingress the way a `positive`/`max` bound is (there is no runtime check being preserved through evaluation); it is a fact about the field's TYPE, checked once, structurally, with nothing further for a validity argument to preserve across evaluation. The catalog's own structure corroborates this: `ModifierKind.Ordered` carries no `ProofSatisfactions` entry (Modifiers.cs:63), unlike the numeric sign modifiers the existing arguments were written for. This group is the ONLY group in the fault family's defined region whose discharge is a bare modifier lookup rather than an interval/guard/literal derivation (verified against the disposition map: `requirementKind: "Modifier"` appears at no other catalog site in the fault family). Every discharge-contract entry below cites "Arg-bound interval arithmetic" because the schema's `validityArguments` enum requires one of the seven closed names and that is the least-remote of the seven (it is the argument this same corpus already stretches furthest, from event-arg modifiers to field modifiers, for other groups' class-(a) discharges) — but this citation is recorded as a placeholder, not as an adequate argument. The owner needs to author a new validity argument for declaration-attribute-only discharges (or rule that this requirement kind is type-checking, not proof surface, and does not belong in the matrix's proof-obligation product at all) before any cell in this file can ratify. Nothing here should be read as claiming that gap closed.
- This cell stands for three evaluation-site categories in the region: field-default-value-expression (tested directly, program below), field-modifier-value-expression, and event-arg-modifier-value-expression — the disposition pass's 'declaration' premise tier, minus the two categories that get their own cells below (type-qualifier-expression, collection-inner-type-modifier-value-expression) because their measurements diverged. The fault family's own declaration-position precedent for the numeric case (fault-5-division-primitive-declaration-positions.cells.json) found the same three categories mint nothing at HEAD, and this file's own direct measurement of all three agrees (see companion programs below).
- Live-verified at HEAD, commit e1a14d91, 2026-07-21: `field IsAtMost as boolean default StatusA <= StatusB editable` compiles with zero diagnostics regardless of whether either operand is `ordered` — the default-value expression is never evaluated for fault obligations.
- Field-modifier-value-expression companion measurement: `field Cap as integer default 0 max (if StatusA <= StatusB then 100 else 200) editable` — compiles with zero choice-ordering diagnostics regardless of `ordered` (a `[skipped obligation] Cap:max: bound is not a literal declared value` advisory line is printed by the tool itself, not a compiler diagnostic — it is the WP calculator declining a cross-field bound, unrelated to this obligation). Live-verified, same commit/date/tool.
- Event-arg-modifier-value-expression companion measurement: `event Adjust(Cap as integer max (if StatusA <= StatusB then 100 else 200))` — compiles with zero diagnostics regardless of `ordered`. Live-verified, same commit/date/tool.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- src/Precept/Language/Operations.cs:882 — code
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — matrix: Fault minting: catalog-stamped at evaluation sites

## g19/type-qualifier — Type-qualifier expression — unmeasured (entangled diagnostic)

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- src/Precept/Language/Operations.cs:882 — code

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | ChoiceLessThanOrEqualChoice — choice <= choice |
| evaluation site category | type-qualifier-expression |
| type family | primitive |

### What must be proven

Obligation: StatusA <= StatusB

Weakest precondition: StatusA is declared ordered and StatusB is declared ordered

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| StatusA | the comparison's left (first) choice-typed operand |
| StatusB | the comparison's right (second) choice-typed operand |
| <= | one of <, <=, >, >= over two choice-typed operands — the catalog's four ChoiceXChoice operations, all carrying the identical `ModifierRequirement(Ordered)` shape (Operations.cs:866-897) |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet — comparison-direction flip: `StatusA < StatusB` is normal-form-equal to `StatusB > StatusA`, which is why all four operators mint the identical obligation (both operands declared ordered), just paired into two normal forms (strict / inclusive)

### Which premise classes can discharge it

Applicable classes: (a).

Same derivation as every other cell in this file: the applicable-class set is {(a)} regardless of site, because the fact consumed is a static declaration attribute, not a runtime value bound.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: both comparison operands' declared modifier sets contain `ordered` -> the choice-ordering operator is well-defined; the catalog's `ModifierRequirement(Subject: ParamSubject(PChoice), Required: Ordered)` (Operations.cs:866-897) resolved against each operand's own field/arg/bound-variable declaration
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Look up `ordered` in each operand's own declared modifier set (a finite set per field, event-arg, or quantifier-bound-variable declaration). Both present discharges; either absent rejects, naming class (a) and the specific missing operand(s).

- src/Precept/Language/Operations.cs:866 — code: ChoiceLessThanChoice — ProofRequirements: [ModifierRequirement(ParamSubject(PChoice), Ordered, "Both choice operands must be declared ordered")]
- src/Precept/Language/Operations.cs:874 — code: ChoiceGreaterThanChoice — identical requirement shape
- src/Precept/Language/Operations.cs:882 — code: ChoiceLessThanOrEqualChoice — identical requirement shape
- src/Precept/Language/Operations.cs:890 — code: ChoiceGreaterThanOrEqualChoice — identical requirement shape
- src/Precept/Language/ProofRequirement.cs:148 — code: ModifierRequirement doc comment: "When both operands share the same ParameterMeta reference (as with choice ordering operations), the requirement applies to all matching operand positions" — the catalog's own statement of intent that both operands are meant to be checked
- src/Precept/Language/Modifiers.cs:63 — code: ModifierKind.Ordered — ModifierCategory.Structural, no ProofSatisfactions entries (unlike Nonnegative/Positive/Nonzero, which carry ProofSatisfaction.Numeric) — the catalog's own signal that this modifier is not a value-bound fact
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: premise class (a): field modifiers
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row: catalog-declared safety precondition at the evaluation site; premise classes (b)/(c)/(a) per catalog ProofSatisfactions

### What the failing diagnostic must suggest

- For class (a): declare `ordered` on both of <WP>'s choice-typed operands
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base.** None recorded — see this cell's notes for why.

**Discharge additions — what makes the base compile.**

*both-ordered* — other

- Addition: declare `ordered` on both StatusA and StatusB
- Premise classes: (a)
- Derivation: both operands' declared modifier sets contain `ordered` -> ModifierRequirement(Ordered) resolves true for both
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Measured attempt, not a clean measurement: this evaluation-site category's own grammar restricts a type qualifier's `{...}` interpolation hole to a single whole-value expression of the qualifier's target type (`currency` for `money in`), not an arbitrary boolean expression. `field Subtotal as money in '{if StatusA <= StatusB then CurrencyA else CurrencyB}' default '0 USD' editable` (CurrencyA/CurrencyB as currency default 'USD'/'EUR') compiles to `QualifierMismatch: Value does not match the '{Conditional}' qualifier on field 'Subtotal'` — an unrelated diagnostic that fires before (or instead of) any choice-ordering check, so the run cannot isolate whether this site mints the choice-ordering obligation at all. Recorded as unmeasured rather than inferred, matching this category's own pre-existing documented status (scratch `eval-sites.json` in the disposition-pass working set: 'whether the position mints fault obligations at HEAD is unmeasured, and is recorded as unmeasured rather than inferred').)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` block: same schema gap as every discharge in this file (no locus for a field-declaration modifier addition).

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| both-ordered | remove `ordered` from StatusB only, keeping it on StatusA | One operand still undeclared -> the modifier lookup still fails for it -> must still reject, same obligation (model claim; not run, for the same entanglement reason as the discharge). | reject, naming the same obligation |

### Respellability

- Resolution: cell-override

- Overrides the family-wide numeric respellability paragraph (which is about algebraic rearrangement of value bounds and does not apply here — there is no family-wide verdict yet written for the fault case shape's modifier-discharge groups). There is no sound-but-unprovable band for this obligation: unlike a numeric bound, `ordered` has no partial-credit spelling (no per-term conjunct, no alternate bound) — a comparison on a choice type that is not declared `ordered` is not merely unprovable, it is not well-formed (the type declares no ordering; `<` would be arbitrary, per `ordered`'s own hover text, 'ordered by declaration sequence', Modifiers.cs:66). So every sound program in this cell's class already IS in the licensed form (`ordered` declared on both operands); there is nothing to respell.

**What the sources leave unstated or ambiguous here**

- MISSING RULE, central to this file: the matrix's closed § Validity arguments list (seven named arguments) has no argument that argues a discharge whose entire premise is a declared-modifier PRESENCE fact unconnected to a runtime value bound. The seven stated arguments (Guard normal-form match, Arg-bound interval arithmetic, Inductive hypothesis plus sign monotonicity, Literal constant-fold for defaults, Establishment over defaults, Guard-fact substitution for relational rules, Vacuity by activation) are all, in their actual written content, about intervals, guard truth, literal folding, or activation vacuity — none of them is about a static declaration attribute that makes an operator well-defined independent of any runtime value. `ordered` is not enforced on a VALUE at ingress the way a `positive`/`max` bound is (there is no runtime check being preserved through evaluation); it is a fact about the field's TYPE, checked once, structurally, with nothing further for a validity argument to preserve across evaluation. The catalog's own structure corroborates this: `ModifierKind.Ordered` carries no `ProofSatisfactions` entry (Modifiers.cs:63), unlike the numeric sign modifiers the existing arguments were written for. This group is the ONLY group in the fault family's defined region whose discharge is a bare modifier lookup rather than an interval/guard/literal derivation (verified against the disposition map: `requirementKind: "Modifier"` appears at no other catalog site in the fault family). Every discharge-contract entry below cites "Arg-bound interval arithmetic" because the schema's `validityArguments` enum requires one of the seven closed names and that is the least-remote of the seven (it is the argument this same corpus already stretches furthest, from event-arg modifiers to field modifiers, for other groups' class-(a) discharges) — but this citation is recorded as a placeholder, not as an adequate argument. The owner needs to author a new validity argument for declaration-attribute-only discharges (or rule that this requirement kind is type-checking, not proof surface, and does not belong in the matrix's proof-obligation product at all) before any cell in this file can ratify. Nothing here should be read as claiming that gap closed.
- No `base` witness is recorded for this cell — deliberately, not by omission. A first, more literal attempt at a base (`field Subtotal as money in (if StatusA <= StatusB then "USD" else "EUR") default '0 USD' editable`, following the spec's `TypeQualifier := (in \| of \| to) Expr` production literally) is not valid Precept at all: it rejects `ExpectedToken: Expected typed constant here, but found '('` — confirming the implemented parser restricts the qualifier position to an interpolated-typed-constant form (`'{...}'`), not an arbitrary Expr, despite the spec production. Live-verified, commit e1a14d91, 2026-07-21. The corrected-syntax attempt below (in the discharge's builtStatusNote) is valid Precept but produces an unrelated `QualifierMismatch` diagnostic before any choice-ordering check could fire. Neither attempt yields a program that rejects naming exactly this obligation with no other diagnostics, which the schema's `baseWitness` requires — so no `base` is stated here rather than forcing either attempt into that shape. Base-witness presence is not yet validator-enforced for defined cells (cell.schema.json README, "Witness-triple minimum ... Pending"), so this is a recorded gap, not a silent one.
- Recorded `defined` (not `open`) because the OBLIGATION and premise-class derivation are exactly as derivable here as at every other cell in this file — nothing about this site's grammar changes the model-side answer, only the built-side measurement is inconclusive. This mirrors the type-qualifier declaration position's own pre-existing status for the numeric case, which is `empty` there (no numeric type has a qualifier), not `defined`-with-unmeasured-built-status — a difference worth noting: for a business-domain field with a CHOICE-typed conditional qualifier expression, the coordinate is reachable in principle (the grammar's Expr production is written generally) even though the specific spelling attempted here does not parse; a different spelling reaching the choice comparison through the qualifier's interpolation hole was not found in the time available and is not claimed to be impossible.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — matrix: Fault minting: catalog-stamped at evaluation sites
- docs/language/precept-language-spec.md § Type References — spec: §2.3 Type References — TypeQualifier := (in \| of \| to) Expr (precept-language-spec.md:1116); in practice the parser routes the qualifier position through the interpolated-typed-constant path, not a general Expr — see the ExpectedToken result below

## g19/collection-inner-type-modifier — Collection inner-type modifier value expression — Queue max (if StatusA <= StatusB ...)

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- src/Precept/Language/Operations.cs:882 — code

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | ChoiceLessThanOrEqualChoice — choice <= choice |
| evaluation site category | collection-inner-type-modifier-value-expression |
| type family | primitive |

### What must be proven

Obligation: StatusA <= StatusB

Weakest precondition: StatusA is declared ordered and StatusB is declared ordered

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| StatusA | the comparison's left (first) choice-typed operand |
| StatusB | the comparison's right (second) choice-typed operand |
| <= | one of <, <=, >, >= over two choice-typed operands — the catalog's four ChoiceXChoice operations, all carrying the identical `ModifierRequirement(Ordered)` shape (Operations.cs:866-897) |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet — comparison-direction flip: `StatusA < StatusB` is normal-form-equal to `StatusB > StatusA`, which is why all four operators mint the identical obligation (both operands declared ordered), just paired into two normal forms (strict / inclusive)

### Which premise classes can discharge it

Applicable classes: (a).

The applicable-class set is {(a)} at this evaluation-site category, and — the one respect in which this whole group differs from every other authored fault group — it is {(a)} at EVERY evaluation-site category in the region, not just this one: the fact consumed (the `ordered` modifier on both operands) is a static declaration attribute, not a runtime value bound, so which site the comparison sits in has no bearing on which premise classes could discharge it. Classes (b)/(c)/(d) supply argument bounds, guard truth, and pre-state value facts respectively — none of which bears on whether a choice TYPE was declared `ordered`. That derivation is the cell's own (matrix § Vocabulary, premise classes; disposition-pass authoring notes for group g19).

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: both comparison operands' declared modifier sets contain `ordered` -> the choice-ordering operator is well-defined; the catalog's `ModifierRequirement(Subject: ParamSubject(PChoice), Required: Ordered)` (Operations.cs:866-897) resolved against each operand's own field/arg/bound-variable declaration
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Look up `ordered` in each operand's own declared modifier set (a finite set per field, event-arg, or quantifier-bound-variable declaration). Both present discharges; either absent rejects, naming class (a) and the specific missing operand(s).

- src/Precept/Language/Operations.cs:866 — code: ChoiceLessThanChoice — ProofRequirements: [ModifierRequirement(ParamSubject(PChoice), Ordered, "Both choice operands must be declared ordered")]
- src/Precept/Language/Operations.cs:874 — code: ChoiceGreaterThanChoice — identical requirement shape
- src/Precept/Language/Operations.cs:882 — code: ChoiceLessThanOrEqualChoice — identical requirement shape
- src/Precept/Language/Operations.cs:890 — code: ChoiceGreaterThanOrEqualChoice — identical requirement shape
- src/Precept/Language/ProofRequirement.cs:148 — code: ModifierRequirement doc comment: "When both operands share the same ParameterMeta reference (as with choice ordering operations), the requirement applies to all matching operand positions" — the catalog's own statement of intent that both operands are meant to be checked
- src/Precept/Language/Modifiers.cs:63 — code: ModifierKind.Ordered — ModifierCategory.Structural, no ProofSatisfactions entries (unlike Nonnegative/Positive/Nonzero, which carry ProofSatisfaction.Numeric) — the catalog's own signal that this modifier is not a value-bound fact
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: premise class (a): field modifiers
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row: catalog-declared safety precondition at the evaluation site; premise classes (b)/(c)/(a) per catalog ProofSatisfactions

### What the failing diagnostic must suggest

- For class (a): declare `ordered` on both of <WP>'s choice-typed operands
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept ChoiceOrderCollInnerModBase

field StatusA as choice of string("Low", "Medium", "High") default "Low" editable
field StatusB as choice of string("Low", "Medium", "High") default "Low" editable
field Queue as queue of integer max (if StatusA <= StatusB then 100 else 200) editable
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*both-ordered* — other

- Addition: replace `field StatusA as choice of string("Low", "Medium", "High") default "Low" editable` with `field StatusA as choice of string("Low", "Medium", "High") ordered default "Low" editable`; and replace `field StatusB as choice of string("Low", "Medium", "High") default "Low" editable` with `field StatusB as choice of string("Low", "Medium", "High") ordered default "Low" editable`
- Premise classes: (a)
- Derivation: both operands' declared modifier sets contain `ordered` -> ModifierRequirement(Ordered) resolves true for both -> the comparison is well-defined
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Measured at HEAD (commit e1a14d91, 2026-07-21, Precept.MatrixTools): this evaluation-site category mints no fault obligation at all — the base itself compiles with zero diagnostics regardless of whether either operand is declared `ordered`. A clean compile here records the site's failure to mint, not confirmation that this discharge is accepted.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` block is given: the schema's `application` discriminated union offers only `row-guard` and `event-arg-declarations`, and this addition edits a plain field, event-arg, or collection-inner-type declaration (or, for the quantifier cell, a quantifier-bound variable's source collection) directly — a locus the DU does not yet name. Recorded as a schema gap rather than forced into either existing locus (same gap already flagged in fault-3/fault-5 for field-declaration additions).

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| both-ordered | replace `field StatusA as choice of string("Low", "Medium", "High") default "Low" editable` with `field StatusA as choice of string("Low", "Medium", "High") ordered default "Low" editable` | Removing `ordered` from the right (second) operand while the left (first) operand keeps it still leaves one operand undeclared -> the modifier lookup still fails for that operand -> must still reject, same obligation. | reject, naming the same obligation |

### Respellability

- Resolution: cell-override

- Overrides the family-wide numeric respellability paragraph (which is about algebraic rearrangement of value bounds and does not apply here — there is no family-wide verdict yet written for the fault case shape's modifier-discharge groups). There is no sound-but-unprovable band for this obligation: unlike a numeric bound, `ordered` has no partial-credit spelling (no per-term conjunct, no alternate bound) — a comparison on a choice type that is not declared `ordered` is not merely unprovable, it is not well-formed (the type declares no ordering; `<` would be arbitrary, per `ordered`'s own hover text, 'ordered by declaration sequence', Modifiers.cs:66). So every sound program in this cell's class already IS in the licensed form (`ordered` declared on both operands); there is nothing to respell.

**What the sources leave unstated or ambiguous here**

- MISSING RULE, central to this file: the matrix's closed § Validity arguments list (seven named arguments) has no argument that argues a discharge whose entire premise is a declared-modifier PRESENCE fact unconnected to a runtime value bound. The seven stated arguments (Guard normal-form match, Arg-bound interval arithmetic, Inductive hypothesis plus sign monotonicity, Literal constant-fold for defaults, Establishment over defaults, Guard-fact substitution for relational rules, Vacuity by activation) are all, in their actual written content, about intervals, guard truth, literal folding, or activation vacuity — none of them is about a static declaration attribute that makes an operator well-defined independent of any runtime value. `ordered` is not enforced on a VALUE at ingress the way a `positive`/`max` bound is (there is no runtime check being preserved through evaluation); it is a fact about the field's TYPE, checked once, structurally, with nothing further for a validity argument to preserve across evaluation. The catalog's own structure corroborates this: `ModifierKind.Ordered` carries no `ProofSatisfactions` entry (Modifiers.cs:63), unlike the numeric sign modifiers the existing arguments were written for. This group is the ONLY group in the fault family's defined region whose discharge is a bare modifier lookup rather than an interval/guard/literal derivation (verified against the disposition map: `requirementKind: "Modifier"` appears at no other catalog site in the fault family). Every discharge-contract entry below cites "Arg-bound interval arithmetic" because the schema's `validityArguments` enum requires one of the seven closed names and that is the least-remote of the seven (it is the argument this same corpus already stretches furthest, from event-arg modifiers to field modifiers, for other groups' class-(a) discharges) — but this citation is recorded as a placeholder, not as an adequate argument. The owner needs to author a new validity argument for declaration-attribute-only discharges (or rule that this requirement kind is type-checking, not proof surface, and does not belong in the matrix's proof-obligation product at all) before any cell in this file can ratify. Nothing here should be read as claiming that gap closed.
- Live-verified at HEAD, commit e1a14d91, 2026-07-21: `field Queue as queue of integer max (if StatusA <= StatusB then 100 else 200) editable` compiles with zero diagnostics regardless of whether either operand is `ordered` — the collection inner-type's modifier-value expression is never evaluated for fault obligations. Same non-minting pattern as the plain field-modifier-value-expression cell (g19/declaration-value-expressions); kept as its own cell rather than folded in because the disposition map's own group manifest lists it as a separate evaluation-site category with its own inner-type declaration surface, not because the measured behavior differs.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- src/Precept/Language/Operations.cs:882 — code
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — matrix: Fault minting: catalog-stamped at evaluation sites

## Fields with no rendering rule

Data the generator has no rendering rule for, surfaced verbatim rather than dropped. Each is either a schema addition the generator has not caught up with, or a stray field.

| Where | Field | Value |
|---|---|---|
| cells[g19/transition-row-write].respellability | overrideVerdict | "yes" |
| cells[g19/construction-row-write].respellability | overrideVerdict | "yes" |
| cells[g19/state-hook-write].respellability | overrideVerdict | "yes" |
| cells[g19/guard-row-and-hook].respellability | overrideVerdict | "yes" |
| cells[g19/guard-access-and-activation].respellability | overrideVerdict | "yes" |
| cells[g19/message-interpolation].respellability | overrideVerdict | "yes" |
| cells[g19/constraint-condition].respellability | overrideVerdict | "yes" |
| cells[g19/computed-field].respellability | overrideVerdict | "yes" |
| cells[g19/quantifier-predicate].respellability | overrideVerdict | "yes" |
| cells[g19/declaration-value-expressions].respellability | overrideVerdict | "yes" |
| cells[g19/type-qualifier].respellability | overrideVerdict | "yes" |
| cells[g19/collection-inner-type-modifier].respellability | overrideVerdict | "yes" |

