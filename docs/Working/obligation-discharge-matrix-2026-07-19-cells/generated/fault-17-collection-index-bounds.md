<!--
GENERATED FILE — do not hand-edit.
Source: fault-17-collection-index-bounds.cells.json
Regenerate: dotnet run --project tools/Precept.MatrixTools -- render-cells docs/Working/obligation-discharge-matrix-2026-07-19-cells
Hand edits will be overwritten. Edit the .cells.json and regenerate.
-->

# Fault family — indexed access out of bounds, .at(N) on list / log / log-by (group 17)

Family id: fault-17-collection-index-bounds
Case shape: fault
Definition version: obligation-discharge-matrix-2026-07-19 rev 7 (2026-07-20, draft)

**Where this family's content comes from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § The cell — matrix: the fault-family cell definition and base-minimality clause
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: premise classes, discharge mechanisms, suggestion schema, Validity arguments closed list
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses — matrix: Family 4 — the group's own authoring notes point at this as the only written fault decision procedure, and this file's central finding is that it does not reach this group's obligation
- docs/language/collection-types.md § `list of T` — spec
- docs/language/collection-types.md § `log of T` — spec
- docs/language/collection-types.md § `log of T by P` — spec
- src/Precept/Language/Types.cs:183 — code: the collection accessor ProofRequirement catalog begins here
- src/Precept/Pipeline/ProofEngine.Strategies.cs:390 — code: TryIndexBoundsProof, the built discharge mechanism this file's missing-rule finding concerns
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — owner-ruling, 2026-07-20: the false-proof hazard for rule-premise discharges (premise class d)

## Respellability — the family verdict

Can every safe program these rules reject be rewritten into a form they accept? **no**

Evidence: model-derived
Corpus measurement: pending

**Notes on the family verdict**

- Family-level verdict recorded 'no' provisionally, not as a surface-shrink decision: with every cell in this group disposition 'open' (no licensed discharge contract exists to measure sound-but-unprovable spellings against), there is no defined contract for a respellability verdict to be measured against yet. 'No' is the honest placeholder pending the missing-rule report below being resolved, not the Vocabulary's surface-shrink 'no' (docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary, the Respellable bullet).

**Sources for this verdict**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Ratification protocol — matrix: layer 4: corpus measurement is the hard gate

## Premise classes — the legend

Where a proof may get its facts (want :19, :143): (a) field modifiers, (b) arg constraints, (c) the handler's guard, (d) all rules holding in the pre-state (the inductive hypothesis), (e) evaluating the applicable rule set at an editable-field write — (e)'s status as a genuinely fifth class vs (a) generalized is an open owner item.

## g17/list-write — list accessor .at(integer) — write site, index from an event argument

Disposition: **open**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses — matrix: Family 4 — the only written fault decision procedure, and the single-operand shape it is limited to
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: the closed list of seven named arguments; none names a cross-subject two-premise decomposition
- src/Precept/Pipeline/ProofEngine.Strategies.cs:390 — code: TryIndexBoundsProof / "Strategy 3: Guard-in-Path Proof" — the actual built mechanism, unnamed in the matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | list accessor .at(integer) |
| evaluation site category | transition-row-action-operand |
| type family | collection |

### What must be proven

Obligation: 0 <= Index and Index < Items.count

Weakest precondition: 0 <= Index and Index < Items.count

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Items | the collection field the .at() accessor reads |
| Index | the index expression passed to .at() |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a), (b), (c), (d).

Full write-site availability, matching the fault-family case-shape row and this group's own hazard note (docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes; group authoring notes). The obligation has two independent subjects, and premise availability differs per subject: the index (an event arg here) draws (b) from its own declared modifiers (live-verified: a `nonnegative` arg modifier supplies its lower bound); the collection's cardinality draws (a) from the field's own mincount/maxcount modifiers when both are literal, or (d) from a rule holding of the collection in the pre-state (the false-proof hazard applies here — see notes) when the write plan does not touch the collection. Class (c), the row's guard, is the only route confirmed live to supply the *relational* fact linking the two subjects (`Index < F.count`), since neither (a) nor (b) alone can express one field's value bounded by another's live cardinality. This cell's representative site (transition-row-action-operand) carries all four classes; construction-row-action-operand structurally lacks (d) (no pre-state) and (per the general handler-scope rule) construction rows also lack a residency guard fact of the kind the group's own field description assumes — folded here as an analogous but not identical write occasion, flagged, not asserted uniform.

### The worked examples

**Base — the program the compiler must reject.**

```precept
precept ListWriteBase

field Items as list of decimal
field Entry as decimal default 0.0

event Create(Seed as decimal) initial
event Pick(Index as integer)

on Create
    -> append Items Create.Seed

on Pick
    -> set Entry = Items.at(Pick.Index)
```

Required outcome: reject, naming the missing premise classes (a), (b), (c), (d), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*nonneg-arg-plus-guard* — guard

- Addition: Index as integer nonnegative   (event arg declaration, replacing `Index as integer`) when Index < Items.count and Items.count > 0   (row guard on the Pick row)
- Premise classes: (b), (c)
- Derivation: empirically: Strategy 3 (Guard-in-Path Proof, src/Precept/Pipeline/ProofEngine.Strategies.cs:390) takes the lower bound from the arg's own `nonnegative` modifier (type-derived, no guard needed for that half) and the upper bound from a guard branch comparing the index against F.count; not named by any matrix validityArguments entry
- What the engine does today: proven-today
- How it applies to the base: appended to the guard of the `on Pick` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No single `application` locus captures this addition: it spans BOTH an event-arg-declaration replacement (adding `nonnegative` to the Index arg) AND a row-guard addition (`Index < Items.count and Items.count > 0`). The schema's application DU admits only one locus per discharge entry; `application` above records the row-guard half only (the more novel half — the arg-modifier half is the same `nonnegative`-on-an-arg shape Witness Family 1 already covers). A schema-vocabulary gap for two-locus additions, not invented around here — same shape of gap fault-13 already flagged for its own field-declaration-locus addition.
- Live-verified 2026-07-21 at HEAD e1a14d91: base + this addition compiles with zero diagnostics (witness-g17/list-write-discharge.precept). The `Items.count > 0` conjunct is NOT part of this group's own obligation — it independently discharges the accessor's separate, catalog-declared non-empty NumericProofRequirement (identical to fault-13's obligation; src/Precept/Language/Types.cs:309), which mints at the same site under the same diagnostic code (IndexBoundsGuard) and must be discharged too or its own error survives. builtStatus is proven-today for the empirical outcome; modelStatus is deliberately omitted — no named validity argument licenses the derivation (see cell-level missing-rule note).

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| nonneg-arg-plus-guard | when Index < Items.count   (row guard on the Pick row, `Items.count > 0` conjunct dropped; the arg-declaration `nonnegative` half is unchanged) | live-verified: with `F.count > 0` removed, the compile still raises IndexBoundsGuard (witness-g17/list-write-nearmiss.precept) — must still reject, same obligation. This specific near-miss demonstrates the built strategy requires both conjuncts even though 0 <= Index < F.count logically already entails F.count > 0; a built-power quirk, recorded rather than smoothed over. | reject, naming the same obligation |

### Respellability

- Resolution: cell-override

- Overrides the family verdict rather than inheriting it: with disposition open and no licensed derivation named, 'is every sound program in the band respellable' cannot be answered yet either — there is no defined contract to respell against. Recorded no (not yet resolvable) rather than left silently defaulted to the family's yes, since that verdict presumes a defined contract this cell does not have.

**What the sources leave unstated or ambiguous here**

- MISSING RULE, reported rather than filled: this obligation (`0 <= Index and Index < F.count`) relates two DIFFERENT subjects — the index and a separate field's live cardinality — not one operand's own interval. The matrix's only written fault decision procedure (Family 4, docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses) computes a single operand's interval from its own declared modifiers and in-scope guard facts; it does not state a derivation for comparing that operand against a second, independently-varying subject. Direct testing this session (src/Precept/Pipeline/ProofEngine.Strategies.cs:390, TryIndexBoundsProof, "Strategy 3: Guard-in-Path Proof") confirms the shipped compiler discharges this obligation through dedicated code — decomposing it into an independently-checked lower bound (type-derived from a `nonnegative`/`positive` modifier, or a matching guard branch) and an independently-checked upper bound (a guard branch comparing the index against the collection's `.count`) — but no paragraph under docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments names this decomposition, and none of its seven closed `validityArguments` entries covers a cross-subject relational comparison discharged by splitting a conjunction across two premise classes. A verbatim whole-condition guard match (`when Index >= 0 and Index < F.count`, tested directly) does NOT discharge at HEAD either (live-verified below), which rules out reading the existing 'Guard normal-form match' argument as already covering this obligation by extension. Disposition is `open`, not `defined`, on every cell in this group for this reason — never invented past what is written.
- This position mints the catalog-declared obligation at HEAD (live-verified).
- Coordinates name transition-row-action-operand as the representative evaluation-site category for this cell. Folded under the same representative (authoring-economy compression across this group's twenty raw categories, not a claim of identical premise availability in every case — see premiseAvailability above for exactly what differs): construction-row-action-operand, state-hook-action-operand.
- Full triple live-verified this session at HEAD e1a14d91: list-write-base.precept, list-write-discharge.precept, list-write-nearmiss.precept under witness-g17/.

**What this cell derives from**

- docs/language/collection-types.md:1056 — spec: Items.at(N) — `list of T`
- src/Precept/Language/Types.cs:309 — code: list accessor .at(integer) catalog declaration: NumericProofRequirement(count>0) plus IndexBoundsProofRequirement(0<=Index<count), Parameters:[PCollectionIndex]
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: Fault family row

## g17/list-guard — list accessor .at(integer) — guard position, index from an event argument

Disposition: **open**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses — matrix: Family 4 — the only written fault decision procedure, and the single-operand shape it is limited to
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: the closed list of seven named arguments; none names a cross-subject two-premise decomposition
- src/Precept/Pipeline/ProofEngine.Strategies.cs:390 — code: TryIndexBoundsProof / "Strategy 3: Guard-in-Path Proof" — the actual built mechanism, unnamed in the matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | list accessor .at(integer) |
| evaluation site category | transition-row-guard |
| type family | collection |

### What must be proven

Obligation: 0 <= Index and Index < Items.count

Weakest precondition: 0 <= Index and Index < Items.count

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Items | the collection field the .at() accessor reads |
| Index | the index expression passed to .at() |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a), (b), (d).

Class (c) is structurally unavailable: by the same reasoning the 'Guard normal-form match' validity argument states (docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — "a row's actions execute only if its guard evaluated true"), the guard's own truth is exactly what is undetermined while the guard itself is being evaluated, so it cannot supply itself as a premise for an obligation minted inside it. Classes (a) and (b) remain available (the index's own declared modifiers, whichever kind of expression supplies it in this position); (d) is available wherever a pre-state exists for the guard's anchor. This cell folds six raw evaluation-site categories under one representative (transition-row-guard): the other five guard/interpolation positions in this group's region share the same 'cannot discharge itself' / 'no mutation here' shape but were not all independently exercised — see notes for exactly which were.

### The worked examples

**Base — the program the compiler must reject.**

```precept
precept TestGuardPosition

field Items as list of decimal
field Flag as boolean default false

event Create(Seed as decimal) initial
event Check(Index as integer)

on Create
    -> append Items Create.Seed

on Check when Items.at(Check.Index) > 0.0
    -> set Flag = true
```

Required outcome: reject, naming the missing premise classes (a), (b), (d), with no other diagnostics

Provenance: model-derived.

### Respellability

- Resolution: cell-override

- Overrides the family verdict rather than inheriting it: with disposition open and no licensed derivation named, 'is every sound program in the band respellable' cannot be answered yet either — there is no defined contract to respell against. Recorded no (not yet resolvable) rather than left silently defaulted to the family's yes, since that verdict presumes a defined contract this cell does not have.

**What the sources leave unstated or ambiguous here**

- MISSING RULE, reported rather than filled: this obligation (`0 <= Index and Index < F.count`) relates two DIFFERENT subjects — the index and a separate field's live cardinality — not one operand's own interval. The matrix's only written fault decision procedure (Family 4, docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses) computes a single operand's interval from its own declared modifiers and in-scope guard facts; it does not state a derivation for comparing that operand against a second, independently-varying subject. Direct testing this session (src/Precept/Pipeline/ProofEngine.Strategies.cs:390, TryIndexBoundsProof, "Strategy 3: Guard-in-Path Proof") confirms the shipped compiler discharges this obligation through dedicated code — decomposing it into an independently-checked lower bound (type-derived from a `nonnegative`/`positive` modifier, or a matching guard branch) and an independently-checked upper bound (a guard branch comparing the index against the collection's `.count`) — but no paragraph under docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments names this decomposition, and none of its seven closed `validityArguments` entries covers a cross-subject relational comparison discharged by splitting a conjunction across two premise classes. A verbatim whole-condition guard match (`when Index >= 0 and Index < F.count`, tested directly) does NOT discharge at HEAD either (live-verified below), which rules out reading the existing 'Guard normal-form match' argument as already covering this obligation by extension. Disposition is `open`, not `defined`, on every cell in this group for this reason — never invented past what is written.
- This position does NOT mint the catalog-declared obligation at HEAD (live-verified) — a built-power gap, independent of the missing-rule finding below.
- Coordinates name transition-row-guard as the representative evaluation-site category for this cell. Folded under the same representative (authoring-economy compression across this group's twenty raw categories, not a claim of identical premise availability in every case — see premiseAvailability above for exactly what differs): state-hook-guard, access-mode-guard, ensure-activation-guard, rule-activation-guard, reject-message-interpolation, constraint-rationale-interpolation.
- Live-verified 2026-07-21 at HEAD e1a14d91: this exact program (an out-of-bounds-capable index read inside the row's own `when` clause, with no modifier or other fact anywhere) compiles with ZERO diagnostics — the model's committed rejection is NOT confirmed by this run, so provenance on the base above is recorded model-derived rather than live-verified, per the provenance field's meaning (it attests only what a run actually confirmed). The position mints no fault obligation at HEAD for this catalog site — a built-power gap, independent of the discharge question this cell is otherwise open on. No discharge/near-miss witnesses are recorded: since the base already compiles clean, there is nothing an addition could be shown to change.

**What this cell derives from**

- docs/language/collection-types.md:1056 — spec: Items.at(N) — `list of T`
- src/Precept/Language/Types.cs:309 — code: list accessor .at(integer) catalog declaration: NumericProofRequirement(count>0) plus IndexBoundsProofRequirement(0<=Index<count), Parameters:[PCollectionIndex]
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: Fault family row

## g17/list-constraint — list accessor .at(integer) — constraint condition, index from an event argument

Disposition: **open**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses — matrix: Family 4 — the only written fault decision procedure, and the single-operand shape it is limited to
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: the closed list of seven named arguments; none names a cross-subject two-premise decomposition
- src/Precept/Pipeline/ProofEngine.Strategies.cs:390 — code: TryIndexBoundsProof / "Strategy 3: Guard-in-Path Proof" — the actual built mechanism, unnamed in the matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | list accessor .at(integer) |
| evaluation site category | rule-condition |
| type family | collection |

### What must be proven

Obligation: 0 <= Index and Index < Items.count

Weakest precondition: 0 <= Index and Index < Items.count

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Items | the collection field the .at() accessor reads |
| Index | the index expression passed to .at() |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a).

A bare `rule` condition is not anchored to any handler: it is evaluated against the complete working copy at every checkpoint, in every reachable configuration (docs/Working/obligation-discharge-matrix-2026-07-19.md § Axes, constraint-structure framing; docs/language/precept-language-spec.md on rule declarations). No guard is in scope (there is none), no event args are in scope (no event fired), and there is no single pre-state to appeal to (the condition must hold over every configuration, not a particular predecessor). Only class (a), field modifiers, is available without further ruling. This cell folds four other raw categories under this representative — state-ensure-condition and event-ensure-condition in fact carry a wider class set (their own optional `when` supplies class (c), and event-ensure additionally has (b)) per the general per-category scope table; this representative (rule-condition) is the NARROWEST of the five, and the folding is a representativeness compression, not a claim that all five share identical availability — flagged in notes, not silently absorbed.

### The worked examples

**Base — the program the compiler must reject.**

```precept
precept TestRuleCondition

field Items as list of decimal
field Watched as integer default 0

rule Items.at(Watched) > 0.0 because "watched slot must stay positive"

event Create(Seed as decimal) initial
on Create
    -> append Items Create.Seed
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

### Respellability

- Resolution: cell-override

- Overrides the family verdict rather than inheriting it: with disposition open and no licensed derivation named, 'is every sound program in the band respellable' cannot be answered yet either — there is no defined contract to respell against. Recorded no (not yet resolvable) rather than left silently defaulted to the family's yes, since that verdict presumes a defined contract this cell does not have.

**What the sources leave unstated or ambiguous here**

- MISSING RULE, reported rather than filled: this obligation (`0 <= Index and Index < F.count`) relates two DIFFERENT subjects — the index and a separate field's live cardinality — not one operand's own interval. The matrix's only written fault decision procedure (Family 4, docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses) computes a single operand's interval from its own declared modifiers and in-scope guard facts; it does not state a derivation for comparing that operand against a second, independently-varying subject. Direct testing this session (src/Precept/Pipeline/ProofEngine.Strategies.cs:390, TryIndexBoundsProof, "Strategy 3: Guard-in-Path Proof") confirms the shipped compiler discharges this obligation through dedicated code — decomposing it into an independently-checked lower bound (type-derived from a `nonnegative`/`positive` modifier, or a matching guard branch) and an independently-checked upper bound (a guard branch comparing the index against the collection's `.count`) — but no paragraph under docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments names this decomposition, and none of its seven closed `validityArguments` entries covers a cross-subject relational comparison discharged by splitting a conjunction across two premise classes. A verbatim whole-condition guard match (`when Index >= 0 and Index < F.count`, tested directly) does NOT discharge at HEAD either (live-verified below), which rules out reading the existing 'Guard normal-form match' argument as already covering this obligation by extension. Disposition is `open`, not `defined`, on every cell in this group for this reason — never invented past what is written.
- This position mints the catalog-declared obligation at HEAD (live-verified).
- Coordinates name rule-condition as the representative evaluation-site category for this cell. Folded under the same representative (authoring-economy compression across this group's twenty raw categories, not a claim of identical premise availability in every case — see premiseAvailability above for exactly what differs): state-ensure-condition, event-ensure-condition, computed-field-expression, quantifier-predicate.
- Live-verified 2026-07-21 at HEAD e1a14d91: this program raises IndexBoundsGuard (site mints here, unlike the guard/declaration buckets). A further live test this session — the same rule condition with BOTH the collection's cardinality pinned by literal `mincount`/`maxcount` and the index field bounded by a literal `nonnegative max` — still raises IndexBoundsGuard (witness-g17/test-rulecond-static.precept): no discharge mechanism reaches a bare rule condition at HEAD (TryIndexBoundsProof requires a handler guard, which a rule condition does not have). No discharge witness is recorded for this bucket: none is known to exist, at any premise combination, model or built.

**What this cell derives from**

- docs/language/collection-types.md:1056 — spec: Items.at(N) — `list of T`
- src/Precept/Language/Types.cs:309 — code: list accessor .at(integer) catalog declaration: NumericProofRequirement(count>0) plus IndexBoundsProofRequirement(0<=Index<count), Parameters:[PCollectionIndex]
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: Fault family row

## g17/list-declaration — list accessor .at(integer) — declaration-position expression, index from an earlier-declared field

Disposition: **open**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses — matrix: Family 4 — the only written fault decision procedure, and the single-operand shape it is limited to
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: the closed list of seven named arguments; none names a cross-subject two-premise decomposition
- src/Precept/Pipeline/ProofEngine.Strategies.cs:390 — code: TryIndexBoundsProof / "Strategy 3: Guard-in-Path Proof" — the actual built mechanism, unnamed in the matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | list accessor .at(integer) |
| evaluation site category | field-default-value-expression |
| type family | collection |

### What must be proven

Obligation: 0 <= Index and Index < Items.count

Weakest precondition: 0 <= Index and Index < Items.count

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Items | the collection field the .at() accessor reads |
| Index | the index expression passed to .at() |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a).

A field's `default` expression is materialised once, during construction, and its scope is restricted to fields declared above it in the file — a forward reference is impossible, not merely disallowed (docs/language/precept-language-spec.md:1352, the modifier/default value-expression scope row). No event is in scope at all (declarations are not anchored to any event), so the group's own shared setting ('the index comes from an event argument') cannot literally hold here — the index this cell tests is a second, earlier-declared field's default value instead. No guard, no event args, no pre-state; only class (a), the modifiers of fields declared earlier, is available. This cell folds four other declaration-position categories under this representative; they share the same no-event/no-guard/no-pre-state shape by the same spec citation, but were not all independently exercised.

### The worked examples

**Base — the program the compiler must reject.**

```precept
precept TestDeclDefault

field Items as list of decimal default [1.0, 2.0, 3.0]
field DefaultIndex as integer default 5
field Selected as decimal default Items.at(DefaultIndex)

event Touch()

on Touch
    -> set Selected = Selected
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: model-derived.

### Respellability

- Resolution: cell-override

- Overrides the family verdict rather than inheriting it: with disposition open and no licensed derivation named, 'is every sound program in the band respellable' cannot be answered yet either — there is no defined contract to respell against. Recorded no (not yet resolvable) rather than left silently defaulted to the family's yes, since that verdict presumes a defined contract this cell does not have.

**What the sources leave unstated or ambiguous here**

- MISSING RULE, reported rather than filled: this obligation (`0 <= Index and Index < F.count`) relates two DIFFERENT subjects — the index and a separate field's live cardinality — not one operand's own interval. The matrix's only written fault decision procedure (Family 4, docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses) computes a single operand's interval from its own declared modifiers and in-scope guard facts; it does not state a derivation for comparing that operand against a second, independently-varying subject. Direct testing this session (src/Precept/Pipeline/ProofEngine.Strategies.cs:390, TryIndexBoundsProof, "Strategy 3: Guard-in-Path Proof") confirms the shipped compiler discharges this obligation through dedicated code — decomposing it into an independently-checked lower bound (type-derived from a `nonnegative`/`positive` modifier, or a matching guard branch) and an independently-checked upper bound (a guard branch comparing the index against the collection's `.count`) — but no paragraph under docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments names this decomposition, and none of its seven closed `validityArguments` entries covers a cross-subject relational comparison discharged by splitting a conjunction across two premise classes. A verbatim whole-condition guard match (`when Index >= 0 and Index < F.count`, tested directly) does NOT discharge at HEAD either (live-verified below), which rules out reading the existing 'Guard normal-form match' argument as already covering this obligation by extension. Disposition is `open`, not `defined`, on every cell in this group for this reason — never invented past what is written.
- This position does NOT mint the catalog-declared obligation at HEAD (live-verified) — a built-power gap, independent of the missing-rule finding below.
- Coordinates name field-default-value-expression as the representative evaluation-site category for this cell. Folded under the same representative (authoring-economy compression across this group's twenty raw categories, not a claim of identical premise availability in every case — see premiseAvailability above for exactly what differs): field-modifier-value-expression, event-arg-modifier-value-expression, collection-inner-type-modifier-value-expression, type-qualifier-expression.
- Live-verified 2026-07-21 at HEAD e1a14d91: this program declares `DefaultIndex default 5` against a 3-element default list literal — an index that is out of bounds by the declaration's own literal facts — and compiles with ZERO diagnostics. The model's committed rejection is NOT confirmed by this run; provenance on the base is recorded model-derived accordingly. The position mints no fault obligation at HEAD for this catalog site regardless of the value's actual safety — a built-power gap distinct from the missing-rule question this cell is otherwise open on.
- A further, only-sketched possibility (not tested, not claimed): where BOTH the collection's default and the index field's default are literal, the whole obligation reduces to a pure compile-time fold with no cross-subject relational proof needed at all, potentially reachable under the existing 'Literal constant-fold for defaults' validity argument. Left unexplored and unclaimed here — worth a follow-up probe, not folded into this cell's disposition.

**What this cell derives from**

- docs/language/collection-types.md:1056 — spec: Items.at(N) — `list of T`
- src/Precept/Language/Types.cs:309 — code: list accessor .at(integer) catalog declaration: NumericProofRequirement(count>0) plus IndexBoundsProofRequirement(0<=Index<count), Parameters:[PCollectionIndex]
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: Fault family row

## g17/log-write — log accessor .at(integer) — write site, index from an event argument

Disposition: **open**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses — matrix: Family 4 — the only written fault decision procedure, and the single-operand shape it is limited to
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: the closed list of seven named arguments; none names a cross-subject two-premise decomposition
- src/Precept/Pipeline/ProofEngine.Strategies.cs:390 — code: TryIndexBoundsProof / "Strategy 3: Guard-in-Path Proof" — the actual built mechanism, unnamed in the matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | log accessor .at(integer) |
| evaluation site category | transition-row-action-operand |
| type family | collection |

### What must be proven

Obligation: 0 <= Index and Index < History.count

Weakest precondition: 0 <= Index and Index < History.count

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| History | the collection field the .at() accessor reads |
| Index | the index expression passed to .at() |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a), (b), (c), (d).

Full write-site availability, matching the fault-family case-shape row and this group's own hazard note (docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes; group authoring notes). The obligation has two independent subjects, and premise availability differs per subject: the index (an event arg here) draws (b) from its own declared modifiers (live-verified: a `nonnegative` arg modifier supplies its lower bound); the collection's cardinality draws (a) from the field's own mincount/maxcount modifiers when both are literal, or (d) from a rule holding of the collection in the pre-state (the false-proof hazard applies here — see notes) when the write plan does not touch the collection. Class (c), the row's guard, is the only route confirmed live to supply the *relational* fact linking the two subjects (`Index < F.count`), since neither (a) nor (b) alone can express one field's value bounded by another's live cardinality. This cell's representative site (transition-row-action-operand) carries all four classes; construction-row-action-operand structurally lacks (d) (no pre-state) and (per the general handler-scope rule) construction rows also lack a residency guard fact of the kind the group's own field description assumes — folded here as an analogous but not identical write occasion, flagged, not asserted uniform.

### The worked examples

**Base — the program the compiler must reject.**

```precept
precept LogWriteBase

field History as log of decimal
field Entry as decimal default 0.0

event Create(Seed as decimal) initial
event Pick(Index as integer)

on Create
    -> append History Create.Seed

on Pick
    -> set Entry = History.at(Pick.Index)
```

Required outcome: reject, naming the missing premise classes (a), (b), (c), (d), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*nonneg-arg-plus-guard* — guard

- Addition: Index as integer nonnegative   (event arg declaration, replacing `Index as integer`) when Index < History.count and History.count > 0   (row guard on the Pick row)
- Premise classes: (b), (c)
- Derivation: empirically: Strategy 3 (Guard-in-Path Proof, src/Precept/Pipeline/ProofEngine.Strategies.cs:390) takes the lower bound from the arg's own `nonnegative` modifier (type-derived, no guard needed for that half) and the upper bound from a guard branch comparing the index against F.count; not named by any matrix validityArguments entry
- What the engine does today: proven-today
- How it applies to the base: appended to the guard of the `on Pick` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No single `application` locus captures this addition: it spans BOTH an event-arg-declaration replacement (adding `nonnegative` to the Index arg) AND a row-guard addition (`Index < History.count and History.count > 0`). The schema's application DU admits only one locus per discharge entry; `application` above records the row-guard half only (the more novel half — the arg-modifier half is the same `nonnegative`-on-an-arg shape Witness Family 1 already covers). A schema-vocabulary gap for two-locus additions, not invented around here — same shape of gap fault-13 already flagged for its own field-declaration-locus addition.
- Live-verified 2026-07-21 at HEAD e1a14d91: base + this addition compiles with zero diagnostics (witness-g17/log-write-discharge.precept). The `History.count > 0` conjunct is NOT part of this group's own obligation — it independently discharges the accessor's separate, catalog-declared non-empty NumericProofRequirement (identical to fault-13's obligation; src/Precept/Language/Types.cs:239), which mints at the same site under the same diagnostic code (IndexBoundsGuard) and must be discharged too or its own error survives. builtStatus is proven-today for the empirical outcome; modelStatus is deliberately omitted — no named validity argument licenses the derivation (see cell-level missing-rule note).

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| nonneg-arg-plus-guard | when Index < History.count   (row guard on the Pick row, `History.count > 0` conjunct dropped; the arg-declaration `nonnegative` half is unchanged) | live-verified: with `F.count > 0` removed, the compile still raises IndexBoundsGuard (witness-g17/log-write-nearmiss.precept) — must still reject, same obligation. This specific near-miss demonstrates the built strategy requires both conjuncts even though 0 <= Index < F.count logically already entails F.count > 0; a built-power quirk, recorded rather than smoothed over. | reject, naming the same obligation |

### Respellability

- Resolution: cell-override

- Overrides the family verdict rather than inheriting it: with disposition open and no licensed derivation named, 'is every sound program in the band respellable' cannot be answered yet either — there is no defined contract to respell against. Recorded no (not yet resolvable) rather than left silently defaulted to the family's yes, since that verdict presumes a defined contract this cell does not have.

**What the sources leave unstated or ambiguous here**

- MISSING RULE, reported rather than filled: this obligation (`0 <= Index and Index < F.count`) relates two DIFFERENT subjects — the index and a separate field's live cardinality — not one operand's own interval. The matrix's only written fault decision procedure (Family 4, docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses) computes a single operand's interval from its own declared modifiers and in-scope guard facts; it does not state a derivation for comparing that operand against a second, independently-varying subject. Direct testing this session (src/Precept/Pipeline/ProofEngine.Strategies.cs:390, TryIndexBoundsProof, "Strategy 3: Guard-in-Path Proof") confirms the shipped compiler discharges this obligation through dedicated code — decomposing it into an independently-checked lower bound (type-derived from a `nonnegative`/`positive` modifier, or a matching guard branch) and an independently-checked upper bound (a guard branch comparing the index against the collection's `.count`) — but no paragraph under docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments names this decomposition, and none of its seven closed `validityArguments` entries covers a cross-subject relational comparison discharged by splitting a conjunction across two premise classes. A verbatim whole-condition guard match (`when Index >= 0 and Index < F.count`, tested directly) does NOT discharge at HEAD either (live-verified below), which rules out reading the existing 'Guard normal-form match' argument as already covering this obligation by extension. Disposition is `open`, not `defined`, on every cell in this group for this reason — never invented past what is written.
- This position mints the catalog-declared obligation at HEAD (live-verified).
- Coordinates name transition-row-action-operand as the representative evaluation-site category for this cell. Folded under the same representative (authoring-economy compression across this group's twenty raw categories, not a claim of identical premise availability in every case — see premiseAvailability above for exactly what differs): construction-row-action-operand, state-hook-action-operand.
- Full triple live-verified this session at HEAD e1a14d91: log-write-base.precept, log-write-discharge.precept, log-write-nearmiss.precept under witness-g17/.

**What this cell derives from**

- docs/language/collection-types.md:337 — spec: History.at(N) — `log of T`
- src/Precept/Language/Types.cs:239 — code: log accessor .at(integer) catalog declaration: NumericProofRequirement(count>0) plus IndexBoundsProofRequirement(0<=Index<count), Parameters:[PCollectionIndex]
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: Fault family row

## g17/log-guard — log accessor .at(integer) — guard position, index from an event argument

Disposition: **open**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses — matrix: Family 4 — the only written fault decision procedure, and the single-operand shape it is limited to
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: the closed list of seven named arguments; none names a cross-subject two-premise decomposition
- src/Precept/Pipeline/ProofEngine.Strategies.cs:390 — code: TryIndexBoundsProof / "Strategy 3: Guard-in-Path Proof" — the actual built mechanism, unnamed in the matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | log accessor .at(integer) |
| evaluation site category | transition-row-guard |
| type family | collection |

### What must be proven

Obligation: 0 <= Index and Index < History.count

Weakest precondition: 0 <= Index and Index < History.count

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| History | the collection field the .at() accessor reads |
| Index | the index expression passed to .at() |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a), (b), (d).

Class (c) is structurally unavailable: by the same reasoning the 'Guard normal-form match' validity argument states (docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — "a row's actions execute only if its guard evaluated true"), the guard's own truth is exactly what is undetermined while the guard itself is being evaluated, so it cannot supply itself as a premise for an obligation minted inside it. Classes (a) and (b) remain available (the index's own declared modifiers, whichever kind of expression supplies it in this position); (d) is available wherever a pre-state exists for the guard's anchor. This cell folds six raw evaluation-site categories under one representative (transition-row-guard): the other five guard/interpolation positions in this group's region share the same 'cannot discharge itself' / 'no mutation here' shape but were not all independently exercised — see notes for exactly which were.

### The worked examples

**Base — the program the compiler must reject.**

```precept
precept LogGuardBase

field History as log of decimal
field Flag as boolean default false

event Create(Seed as decimal) initial
event Check(Index as integer)

on Create
    -> append History Create.Seed

on Check when History.at(Check.Index) > 0.0
    -> set Flag = true
```

Required outcome: reject, naming the missing premise classes (a), (b), (d), with no other diagnostics

Provenance: model-derived.

### Respellability

- Resolution: cell-override

- Overrides the family verdict rather than inheriting it: with disposition open and no licensed derivation named, 'is every sound program in the band respellable' cannot be answered yet either — there is no defined contract to respell against. Recorded no (not yet resolvable) rather than left silently defaulted to the family's yes, since that verdict presumes a defined contract this cell does not have.

**What the sources leave unstated or ambiguous here**

- MISSING RULE, reported rather than filled: this obligation (`0 <= Index and Index < F.count`) relates two DIFFERENT subjects — the index and a separate field's live cardinality — not one operand's own interval. The matrix's only written fault decision procedure (Family 4, docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses) computes a single operand's interval from its own declared modifiers and in-scope guard facts; it does not state a derivation for comparing that operand against a second, independently-varying subject. Direct testing this session (src/Precept/Pipeline/ProofEngine.Strategies.cs:390, TryIndexBoundsProof, "Strategy 3: Guard-in-Path Proof") confirms the shipped compiler discharges this obligation through dedicated code — decomposing it into an independently-checked lower bound (type-derived from a `nonnegative`/`positive` modifier, or a matching guard branch) and an independently-checked upper bound (a guard branch comparing the index against the collection's `.count`) — but no paragraph under docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments names this decomposition, and none of its seven closed `validityArguments` entries covers a cross-subject relational comparison discharged by splitting a conjunction across two premise classes. A verbatim whole-condition guard match (`when Index >= 0 and Index < F.count`, tested directly) does NOT discharge at HEAD either (live-verified below), which rules out reading the existing 'Guard normal-form match' argument as already covering this obligation by extension. Disposition is `open`, not `defined`, on every cell in this group for this reason — never invented past what is written.
- This position does NOT mint the catalog-declared obligation at HEAD (live-verified) — a built-power gap, independent of the missing-rule finding below.
- Coordinates name transition-row-guard as the representative evaluation-site category for this cell. Folded under the same representative (authoring-economy compression across this group's twenty raw categories, not a claim of identical premise availability in every case — see premiseAvailability above for exactly what differs): state-hook-guard, access-mode-guard, ensure-activation-guard, rule-activation-guard, reject-message-interpolation, constraint-rationale-interpolation.
- Live-verified 2026-07-21 at HEAD e1a14d91: this exact program (an out-of-bounds-capable index read inside the row's own `when` clause, with no modifier or other fact anywhere) compiles with ZERO diagnostics — the model's committed rejection is NOT confirmed by this run, so provenance on the base above is recorded model-derived rather than live-verified, per the provenance field's meaning (it attests only what a run actually confirmed). The position mints no fault obligation at HEAD for this catalog site — a built-power gap, independent of the discharge question this cell is otherwise open on. No discharge/near-miss witnesses are recorded: since the base already compiles clean, there is nothing an addition could be shown to change.

**What this cell derives from**

- docs/language/collection-types.md:337 — spec: History.at(N) — `log of T`
- src/Precept/Language/Types.cs:239 — code: log accessor .at(integer) catalog declaration: NumericProofRequirement(count>0) plus IndexBoundsProofRequirement(0<=Index<count), Parameters:[PCollectionIndex]
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: Fault family row

## g17/log-constraint — log accessor .at(integer) — constraint condition, index from an event argument

Disposition: **open**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses — matrix: Family 4 — the only written fault decision procedure, and the single-operand shape it is limited to
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: the closed list of seven named arguments; none names a cross-subject two-premise decomposition
- src/Precept/Pipeline/ProofEngine.Strategies.cs:390 — code: TryIndexBoundsProof / "Strategy 3: Guard-in-Path Proof" — the actual built mechanism, unnamed in the matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | log accessor .at(integer) |
| evaluation site category | rule-condition |
| type family | collection |

### What must be proven

Obligation: 0 <= Index and Index < History.count

Weakest precondition: 0 <= Index and Index < History.count

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| History | the collection field the .at() accessor reads |
| Index | the index expression passed to .at() |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a).

A bare `rule` condition is not anchored to any handler: it is evaluated against the complete working copy at every checkpoint, in every reachable configuration (docs/Working/obligation-discharge-matrix-2026-07-19.md § Axes, constraint-structure framing; docs/language/precept-language-spec.md on rule declarations). No guard is in scope (there is none), no event args are in scope (no event fired), and there is no single pre-state to appeal to (the condition must hold over every configuration, not a particular predecessor). Only class (a), field modifiers, is available without further ruling. This cell folds four other raw categories under this representative — state-ensure-condition and event-ensure-condition in fact carry a wider class set (their own optional `when` supplies class (c), and event-ensure additionally has (b)) per the general per-category scope table; this representative (rule-condition) is the NARROWEST of the five, and the folding is a representativeness compression, not a claim that all five share identical availability — flagged in notes, not silently absorbed.

### The worked examples

**Base — the program the compiler must reject.**

```precept
precept LogRuleCondBase

field History as log of decimal
field Watched as integer default 0

rule History.at(Watched) > 0.0 because "watched slot must stay positive"

event Create(Seed as decimal) initial
on Create
    -> append History Create.Seed
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

### Respellability

- Resolution: cell-override

- Overrides the family verdict rather than inheriting it: with disposition open and no licensed derivation named, 'is every sound program in the band respellable' cannot be answered yet either — there is no defined contract to respell against. Recorded no (not yet resolvable) rather than left silently defaulted to the family's yes, since that verdict presumes a defined contract this cell does not have.

**What the sources leave unstated or ambiguous here**

- MISSING RULE, reported rather than filled: this obligation (`0 <= Index and Index < F.count`) relates two DIFFERENT subjects — the index and a separate field's live cardinality — not one operand's own interval. The matrix's only written fault decision procedure (Family 4, docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses) computes a single operand's interval from its own declared modifiers and in-scope guard facts; it does not state a derivation for comparing that operand against a second, independently-varying subject. Direct testing this session (src/Precept/Pipeline/ProofEngine.Strategies.cs:390, TryIndexBoundsProof, "Strategy 3: Guard-in-Path Proof") confirms the shipped compiler discharges this obligation through dedicated code — decomposing it into an independently-checked lower bound (type-derived from a `nonnegative`/`positive` modifier, or a matching guard branch) and an independently-checked upper bound (a guard branch comparing the index against the collection's `.count`) — but no paragraph under docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments names this decomposition, and none of its seven closed `validityArguments` entries covers a cross-subject relational comparison discharged by splitting a conjunction across two premise classes. A verbatim whole-condition guard match (`when Index >= 0 and Index < F.count`, tested directly) does NOT discharge at HEAD either (live-verified below), which rules out reading the existing 'Guard normal-form match' argument as already covering this obligation by extension. Disposition is `open`, not `defined`, on every cell in this group for this reason — never invented past what is written.
- This position mints the catalog-declared obligation at HEAD (live-verified).
- Coordinates name rule-condition as the representative evaluation-site category for this cell. Folded under the same representative (authoring-economy compression across this group's twenty raw categories, not a claim of identical premise availability in every case — see premiseAvailability above for exactly what differs): state-ensure-condition, event-ensure-condition, computed-field-expression, quantifier-predicate.
- Live-verified 2026-07-21 at HEAD e1a14d91: this program raises IndexBoundsGuard (site mints here, unlike the guard/declaration buckets). A further live test this session — the same rule condition with BOTH the collection's cardinality pinned by literal `mincount`/`maxcount` and the index field bounded by a literal `nonnegative max` — still raises IndexBoundsGuard (witness-g17/test-rulecond-static.precept): no discharge mechanism reaches a bare rule condition at HEAD (TryIndexBoundsProof requires a handler guard, which a rule condition does not have). No discharge witness is recorded for this bucket: none is known to exist, at any premise combination, model or built.

**What this cell derives from**

- docs/language/collection-types.md:337 — spec: History.at(N) — `log of T`
- src/Precept/Language/Types.cs:239 — code: log accessor .at(integer) catalog declaration: NumericProofRequirement(count>0) plus IndexBoundsProofRequirement(0<=Index<count), Parameters:[PCollectionIndex]
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: Fault family row

## g17/log-declaration — log accessor .at(integer) — declaration-position expression, index from an earlier-declared field

Disposition: **open**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses — matrix: Family 4 — the only written fault decision procedure, and the single-operand shape it is limited to
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: the closed list of seven named arguments; none names a cross-subject two-premise decomposition
- src/Precept/Pipeline/ProofEngine.Strategies.cs:390 — code: TryIndexBoundsProof / "Strategy 3: Guard-in-Path Proof" — the actual built mechanism, unnamed in the matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | log accessor .at(integer) |
| evaluation site category | field-default-value-expression |
| type family | collection |

### What must be proven

Obligation: 0 <= Index and Index < History.count

Weakest precondition: 0 <= Index and Index < History.count

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| History | the collection field the .at() accessor reads |
| Index | the index expression passed to .at() |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a).

A field's `default` expression is materialised once, during construction, and its scope is restricted to fields declared above it in the file — a forward reference is impossible, not merely disallowed (docs/language/precept-language-spec.md:1352, the modifier/default value-expression scope row). No event is in scope at all (declarations are not anchored to any event), so the group's own shared setting ('the index comes from an event argument') cannot literally hold here — the index this cell tests is a second, earlier-declared field's default value instead. No guard, no event args, no pre-state; only class (a), the modifiers of fields declared earlier, is available. This cell folds four other declaration-position categories under this representative; they share the same no-event/no-guard/no-pre-state shape by the same spec citation, but were not all independently exercised.

### The worked examples

**Base — the program the compiler must reject.**

```precept
precept LogG17DeclDefault

field History as log of decimal
field DefaultIndex as integer default 5
field Selected as decimal default History.at(DefaultIndex)

event Touch()

on Touch
    -> set Selected = Selected
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: model-derived.

### Respellability

- Resolution: cell-override

- Overrides the family verdict rather than inheriting it: with disposition open and no licensed derivation named, 'is every sound program in the band respellable' cannot be answered yet either — there is no defined contract to respell against. Recorded no (not yet resolvable) rather than left silently defaulted to the family's yes, since that verdict presumes a defined contract this cell does not have.

**What the sources leave unstated or ambiguous here**

- MISSING RULE, reported rather than filled: this obligation (`0 <= Index and Index < F.count`) relates two DIFFERENT subjects — the index and a separate field's live cardinality — not one operand's own interval. The matrix's only written fault decision procedure (Family 4, docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses) computes a single operand's interval from its own declared modifiers and in-scope guard facts; it does not state a derivation for comparing that operand against a second, independently-varying subject. Direct testing this session (src/Precept/Pipeline/ProofEngine.Strategies.cs:390, TryIndexBoundsProof, "Strategy 3: Guard-in-Path Proof") confirms the shipped compiler discharges this obligation through dedicated code — decomposing it into an independently-checked lower bound (type-derived from a `nonnegative`/`positive` modifier, or a matching guard branch) and an independently-checked upper bound (a guard branch comparing the index against the collection's `.count`) — but no paragraph under docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments names this decomposition, and none of its seven closed `validityArguments` entries covers a cross-subject relational comparison discharged by splitting a conjunction across two premise classes. A verbatim whole-condition guard match (`when Index >= 0 and Index < F.count`, tested directly) does NOT discharge at HEAD either (live-verified below), which rules out reading the existing 'Guard normal-form match' argument as already covering this obligation by extension. Disposition is `open`, not `defined`, on every cell in this group for this reason — never invented past what is written.
- This position does NOT mint the catalog-declared obligation at HEAD (live-verified) — a built-power gap, independent of the missing-rule finding below.
- Coordinates name field-default-value-expression as the representative evaluation-site category for this cell. Folded under the same representative (authoring-economy compression across this group's twenty raw categories, not a claim of identical premise availability in every case — see premiseAvailability above for exactly what differs): field-modifier-value-expression, event-arg-modifier-value-expression, collection-inner-type-modifier-value-expression, type-qualifier-expression.
- NOT independently run this session for History — analogous program shown for completeness, carried from the List representative (g17/list-declaration) by the identical catalog ProofRequirement shape across all three accessors (src/Precept/Language/Types.cs). Recorded as unmeasured for this site, not assumed to match, per the group's own honesty duty.

**What this cell derives from**

- docs/language/collection-types.md:337 — spec: History.at(N) — `log of T`
- src/Precept/Language/Types.cs:239 — code: log accessor .at(integer) catalog declaration: NumericProofRequirement(count>0) plus IndexBoundsProofRequirement(0<=Index<count), Parameters:[PCollectionIndex]
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: Fault family row

## g17/logby-write — log by accessor .at(integer) — write site, index from an event argument

Disposition: **open**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses — matrix: Family 4 — the only written fault decision procedure, and the single-operand shape it is limited to
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: the closed list of seven named arguments; none names a cross-subject two-premise decomposition
- src/Precept/Pipeline/ProofEngine.Strategies.cs:390 — code: TryIndexBoundsProof / "Strategy 3: Guard-in-Path Proof" — the actual built mechanism, unnamed in the matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | log by accessor .at(integer) |
| evaluation site category | transition-row-action-operand |
| type family | collection |

### What must be proven

Obligation: 0 <= Index and Index < AuditByKey.count

Weakest precondition: 0 <= Index and Index < AuditByKey.count

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| AuditByKey | the collection field the .at() accessor reads |
| Index | the index expression passed to .at() |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a), (b), (c), (d).

Full write-site availability, matching the fault-family case-shape row and this group's own hazard note (docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes; group authoring notes). The obligation has two independent subjects, and premise availability differs per subject: the index (an event arg here) draws (b) from its own declared modifiers (live-verified: a `nonnegative` arg modifier supplies its lower bound); the collection's cardinality draws (a) from the field's own mincount/maxcount modifiers when both are literal, or (d) from a rule holding of the collection in the pre-state (the false-proof hazard applies here — see notes) when the write plan does not touch the collection. Class (c), the row's guard, is the only route confirmed live to supply the *relational* fact linking the two subjects (`Index < F.count`), since neither (a) nor (b) alone can express one field's value bounded by another's live cardinality. This cell's representative site (transition-row-action-operand) carries all four classes; construction-row-action-operand structurally lacks (d) (no pre-state) and (per the general handler-scope rule) construction rows also lack a residency guard fact of the kind the group's own field description assumes — folded here as an analogous but not identical write occasion, flagged, not asserted uniform.

### The worked examples

**Base — the program the compiler must reject.**

```precept
precept LogByWriteBase

field AuditByKey as log of decimal by integer
field Entry as decimal default 0.0

event Create initial
event Seed(Value as decimal, Seq as integer)
event Pick(Index as integer)

on Create
    -> set Entry = 0.0

on Seed when not (AuditByKey contains Seed.Seq)
    -> append AuditByKey Seed.Value by Seed.Seq

on Pick
    -> set Entry = AuditByKey.at(Pick.Index)
```

Required outcome: reject, naming the missing premise classes (a), (b), (c), (d), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*nonneg-arg-plus-guard* — guard

- Addition: Index as integer nonnegative   (event arg declaration, replacing `Index as integer`) when Index < AuditByKey.count and AuditByKey.count > 0   (row guard on the Pick row)
- Premise classes: (b), (c)
- Derivation: empirically: Strategy 3 (Guard-in-Path Proof, src/Precept/Pipeline/ProofEngine.Strategies.cs:390) takes the lower bound from the arg's own `nonnegative` modifier (type-derived, no guard needed for that half) and the upper bound from a guard branch comparing the index against F.count; not named by any matrix validityArguments entry
- What the engine does today: proven-today
- How it applies to the base: appended to the guard of the `on Pick` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No single `application` locus captures this addition: it spans BOTH an event-arg-declaration replacement (adding `nonnegative` to the Index arg) AND a row-guard addition (`Index < AuditByKey.count and AuditByKey.count > 0`). The schema's application DU admits only one locus per discharge entry; `application` above records the row-guard half only (the more novel half — the arg-modifier half is the same `nonnegative`-on-an-arg shape Witness Family 1 already covers). A schema-vocabulary gap for two-locus additions, not invented around here — same shape of gap fault-13 already flagged for its own field-declaration-locus addition.
- Live-verified 2026-07-21 at HEAD e1a14d91: base + this addition compiles with zero diagnostics (witness-g17/logby-write-discharge.precept). The `AuditByKey.count > 0` conjunct is NOT part of this group's own obligation — it independently discharges the accessor's separate, catalog-declared non-empty NumericProofRequirement (identical to fault-13's obligation; src/Precept/Language/Types.cs:267), which mints at the same site under the same diagnostic code (IndexBoundsGuard) and must be discharged too or its own error survives. builtStatus is proven-today for the empirical outcome; modelStatus is deliberately omitted — no named validity argument licenses the derivation (see cell-level missing-rule note).

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| nonneg-arg-plus-guard | when Index < AuditByKey.count   (row guard on the Pick row, `AuditByKey.count > 0` conjunct dropped; the arg-declaration `nonnegative` half is unchanged) | live-verified: with `F.count > 0` removed, the compile still raises IndexBoundsGuard (witness-g17/logby-write-nearmiss.precept) — must still reject, same obligation. This specific near-miss demonstrates the built strategy requires both conjuncts even though 0 <= Index < F.count logically already entails F.count > 0; a built-power quirk, recorded rather than smoothed over. | reject, naming the same obligation |

### Respellability

- Resolution: cell-override

- Overrides the family verdict rather than inheriting it: with disposition open and no licensed derivation named, 'is every sound program in the band respellable' cannot be answered yet either — there is no defined contract to respell against. Recorded no (not yet resolvable) rather than left silently defaulted to the family's yes, since that verdict presumes a defined contract this cell does not have.

**What the sources leave unstated or ambiguous here**

- MISSING RULE, reported rather than filled: this obligation (`0 <= Index and Index < F.count`) relates two DIFFERENT subjects — the index and a separate field's live cardinality — not one operand's own interval. The matrix's only written fault decision procedure (Family 4, docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses) computes a single operand's interval from its own declared modifiers and in-scope guard facts; it does not state a derivation for comparing that operand against a second, independently-varying subject. Direct testing this session (src/Precept/Pipeline/ProofEngine.Strategies.cs:390, TryIndexBoundsProof, "Strategy 3: Guard-in-Path Proof") confirms the shipped compiler discharges this obligation through dedicated code — decomposing it into an independently-checked lower bound (type-derived from a `nonnegative`/`positive` modifier, or a matching guard branch) and an independently-checked upper bound (a guard branch comparing the index against the collection's `.count`) — but no paragraph under docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments names this decomposition, and none of its seven closed `validityArguments` entries covers a cross-subject relational comparison discharged by splitting a conjunction across two premise classes. A verbatim whole-condition guard match (`when Index >= 0 and Index < F.count`, tested directly) does NOT discharge at HEAD either (live-verified below), which rules out reading the existing 'Guard normal-form match' argument as already covering this obligation by extension. Disposition is `open`, not `defined`, on every cell in this group for this reason — never invented past what is written.
- This position mints the catalog-declared obligation at HEAD (live-verified).
- Coordinates name transition-row-action-operand as the representative evaluation-site category for this cell. Folded under the same representative (authoring-economy compression across this group's twenty raw categories, not a claim of identical premise availability in every case — see premiseAvailability above for exactly what differs): construction-row-action-operand, state-hook-action-operand.
- Full triple live-verified this session at HEAD e1a14d91: logby-write-base.precept, logby-write-discharge.precept, logby-write-nearmiss.precept under witness-g17/.

**What this cell derives from**

- docs/language/collection-types.md:417 — spec: AuditByKey.at(N) — `log of T by P`
- src/Precept/Language/Types.cs:267 — code: log by accessor .at(integer) catalog declaration: NumericProofRequirement(count>0) plus IndexBoundsProofRequirement(0<=Index<count), Parameters:[PCollectionIndex]
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: Fault family row

## g17/logby-guard — log by accessor .at(integer) — guard position, index from an event argument

Disposition: **open**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses — matrix: Family 4 — the only written fault decision procedure, and the single-operand shape it is limited to
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: the closed list of seven named arguments; none names a cross-subject two-premise decomposition
- src/Precept/Pipeline/ProofEngine.Strategies.cs:390 — code: TryIndexBoundsProof / "Strategy 3: Guard-in-Path Proof" — the actual built mechanism, unnamed in the matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | log by accessor .at(integer) |
| evaluation site category | transition-row-guard |
| type family | collection |

### What must be proven

Obligation: 0 <= Index and Index < AuditByKey.count

Weakest precondition: 0 <= Index and Index < AuditByKey.count

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| AuditByKey | the collection field the .at() accessor reads |
| Index | the index expression passed to .at() |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a), (b), (d).

Class (c) is structurally unavailable: by the same reasoning the 'Guard normal-form match' validity argument states (docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — "a row's actions execute only if its guard evaluated true"), the guard's own truth is exactly what is undetermined while the guard itself is being evaluated, so it cannot supply itself as a premise for an obligation minted inside it. Classes (a) and (b) remain available (the index's own declared modifiers, whichever kind of expression supplies it in this position); (d) is available wherever a pre-state exists for the guard's anchor. This cell folds six raw evaluation-site categories under one representative (transition-row-guard): the other five guard/interpolation positions in this group's region share the same 'cannot discharge itself' / 'no mutation here' shape but were not all independently exercised — see notes for exactly which were.

### The worked examples

**Base — the program the compiler must reject.**

```precept
precept LogByGuardBase

field AuditByKey as log of decimal by integer
field Flag as boolean default false

event Create initial
event Seed(Value as decimal, Seq as integer)
event Check(Index as integer)

on Create
    -> set Flag = false

on Seed when not (AuditByKey contains Seed.Seq)
    -> append AuditByKey Seed.Value by Seed.Seq

on Check when AuditByKey.at(Check.Index) > 0.0
    -> set Flag = true
```

Required outcome: reject, naming the missing premise classes (a), (b), (d), with no other diagnostics

Provenance: model-derived.

### Respellability

- Resolution: cell-override

- Overrides the family verdict rather than inheriting it: with disposition open and no licensed derivation named, 'is every sound program in the band respellable' cannot be answered yet either — there is no defined contract to respell against. Recorded no (not yet resolvable) rather than left silently defaulted to the family's yes, since that verdict presumes a defined contract this cell does not have.

**What the sources leave unstated or ambiguous here**

- MISSING RULE, reported rather than filled: this obligation (`0 <= Index and Index < F.count`) relates two DIFFERENT subjects — the index and a separate field's live cardinality — not one operand's own interval. The matrix's only written fault decision procedure (Family 4, docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses) computes a single operand's interval from its own declared modifiers and in-scope guard facts; it does not state a derivation for comparing that operand against a second, independently-varying subject. Direct testing this session (src/Precept/Pipeline/ProofEngine.Strategies.cs:390, TryIndexBoundsProof, "Strategy 3: Guard-in-Path Proof") confirms the shipped compiler discharges this obligation through dedicated code — decomposing it into an independently-checked lower bound (type-derived from a `nonnegative`/`positive` modifier, or a matching guard branch) and an independently-checked upper bound (a guard branch comparing the index against the collection's `.count`) — but no paragraph under docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments names this decomposition, and none of its seven closed `validityArguments` entries covers a cross-subject relational comparison discharged by splitting a conjunction across two premise classes. A verbatim whole-condition guard match (`when Index >= 0 and Index < F.count`, tested directly) does NOT discharge at HEAD either (live-verified below), which rules out reading the existing 'Guard normal-form match' argument as already covering this obligation by extension. Disposition is `open`, not `defined`, on every cell in this group for this reason — never invented past what is written.
- This position does NOT mint the catalog-declared obligation at HEAD (live-verified) — a built-power gap, independent of the missing-rule finding below.
- Coordinates name transition-row-guard as the representative evaluation-site category for this cell. Folded under the same representative (authoring-economy compression across this group's twenty raw categories, not a claim of identical premise availability in every case — see premiseAvailability above for exactly what differs): state-hook-guard, access-mode-guard, ensure-activation-guard, rule-activation-guard, reject-message-interpolation, constraint-rationale-interpolation.
- Live-verified 2026-07-21 at HEAD e1a14d91: this exact program (an out-of-bounds-capable index read inside the row's own `when` clause, with no modifier or other fact anywhere) compiles with ZERO diagnostics — the model's committed rejection is NOT confirmed by this run, so provenance on the base above is recorded model-derived rather than live-verified, per the provenance field's meaning (it attests only what a run actually confirmed). The position mints no fault obligation at HEAD for this catalog site — a built-power gap, independent of the discharge question this cell is otherwise open on. No discharge/near-miss witnesses are recorded: since the base already compiles clean, there is nothing an addition could be shown to change.

**What this cell derives from**

- docs/language/collection-types.md:417 — spec: AuditByKey.at(N) — `log of T by P`
- src/Precept/Language/Types.cs:267 — code: log by accessor .at(integer) catalog declaration: NumericProofRequirement(count>0) plus IndexBoundsProofRequirement(0<=Index<count), Parameters:[PCollectionIndex]
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: Fault family row

## g17/logby-constraint — log by accessor .at(integer) — constraint condition, index from an event argument

Disposition: **open**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses — matrix: Family 4 — the only written fault decision procedure, and the single-operand shape it is limited to
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: the closed list of seven named arguments; none names a cross-subject two-premise decomposition
- src/Precept/Pipeline/ProofEngine.Strategies.cs:390 — code: TryIndexBoundsProof / "Strategy 3: Guard-in-Path Proof" — the actual built mechanism, unnamed in the matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | log by accessor .at(integer) |
| evaluation site category | rule-condition |
| type family | collection |

### What must be proven

Obligation: 0 <= Index and Index < AuditByKey.count

Weakest precondition: 0 <= Index and Index < AuditByKey.count

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| AuditByKey | the collection field the .at() accessor reads |
| Index | the index expression passed to .at() |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a).

A bare `rule` condition is not anchored to any handler: it is evaluated against the complete working copy at every checkpoint, in every reachable configuration (docs/Working/obligation-discharge-matrix-2026-07-19.md § Axes, constraint-structure framing; docs/language/precept-language-spec.md on rule declarations). No guard is in scope (there is none), no event args are in scope (no event fired), and there is no single pre-state to appeal to (the condition must hold over every configuration, not a particular predecessor). Only class (a), field modifiers, is available without further ruling. This cell folds four other raw categories under this representative — state-ensure-condition and event-ensure-condition in fact carry a wider class set (their own optional `when` supplies class (c), and event-ensure additionally has (b)) per the general per-category scope table; this representative (rule-condition) is the NARROWEST of the five, and the folding is a representativeness compression, not a claim that all five share identical availability — flagged in notes, not silently absorbed.

### The worked examples

**Base — the program the compiler must reject.**

```precept
precept LogByRuleCondBase

field AuditByKey as log of decimal by integer
field Watched as integer default 0

rule AuditByKey.at(Watched) > 0.0 because "watched slot must stay positive"

event Create initial
event Seed(Value as decimal, Seq as integer)

on Create
    -> set Watched = 0

on Seed when not (AuditByKey contains Seed.Seq)
    -> append AuditByKey Seed.Value by Seed.Seq
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

### Respellability

- Resolution: cell-override

- Overrides the family verdict rather than inheriting it: with disposition open and no licensed derivation named, 'is every sound program in the band respellable' cannot be answered yet either — there is no defined contract to respell against. Recorded no (not yet resolvable) rather than left silently defaulted to the family's yes, since that verdict presumes a defined contract this cell does not have.

**What the sources leave unstated or ambiguous here**

- MISSING RULE, reported rather than filled: this obligation (`0 <= Index and Index < F.count`) relates two DIFFERENT subjects — the index and a separate field's live cardinality — not one operand's own interval. The matrix's only written fault decision procedure (Family 4, docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses) computes a single operand's interval from its own declared modifiers and in-scope guard facts; it does not state a derivation for comparing that operand against a second, independently-varying subject. Direct testing this session (src/Precept/Pipeline/ProofEngine.Strategies.cs:390, TryIndexBoundsProof, "Strategy 3: Guard-in-Path Proof") confirms the shipped compiler discharges this obligation through dedicated code — decomposing it into an independently-checked lower bound (type-derived from a `nonnegative`/`positive` modifier, or a matching guard branch) and an independently-checked upper bound (a guard branch comparing the index against the collection's `.count`) — but no paragraph under docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments names this decomposition, and none of its seven closed `validityArguments` entries covers a cross-subject relational comparison discharged by splitting a conjunction across two premise classes. A verbatim whole-condition guard match (`when Index >= 0 and Index < F.count`, tested directly) does NOT discharge at HEAD either (live-verified below), which rules out reading the existing 'Guard normal-form match' argument as already covering this obligation by extension. Disposition is `open`, not `defined`, on every cell in this group for this reason — never invented past what is written.
- This position mints the catalog-declared obligation at HEAD (live-verified).
- Coordinates name rule-condition as the representative evaluation-site category for this cell. Folded under the same representative (authoring-economy compression across this group's twenty raw categories, not a claim of identical premise availability in every case — see premiseAvailability above for exactly what differs): state-ensure-condition, event-ensure-condition, computed-field-expression, quantifier-predicate.
- Live-verified 2026-07-21 at HEAD e1a14d91: this program raises IndexBoundsGuard (site mints here, unlike the guard/declaration buckets). A further live test this session — the same rule condition with BOTH the collection's cardinality pinned by literal `mincount`/`maxcount` and the index field bounded by a literal `nonnegative max` — still raises IndexBoundsGuard (witness-g17/test-rulecond-static.precept): no discharge mechanism reaches a bare rule condition at HEAD (TryIndexBoundsProof requires a handler guard, which a rule condition does not have). No discharge witness is recorded for this bucket: none is known to exist, at any premise combination, model or built.

**What this cell derives from**

- docs/language/collection-types.md:417 — spec: AuditByKey.at(N) — `log of T by P`
- src/Precept/Language/Types.cs:267 — code: log by accessor .at(integer) catalog declaration: NumericProofRequirement(count>0) plus IndexBoundsProofRequirement(0<=Index<count), Parameters:[PCollectionIndex]
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: Fault family row

## g17/logby-declaration — log by accessor .at(integer) — declaration-position expression, index from an earlier-declared field

Disposition: **open**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses — matrix: Family 4 — the only written fault decision procedure, and the single-operand shape it is limited to
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: the closed list of seven named arguments; none names a cross-subject two-premise decomposition
- src/Precept/Pipeline/ProofEngine.Strategies.cs:390 — code: TryIndexBoundsProof / "Strategy 3: Guard-in-Path Proof" — the actual built mechanism, unnamed in the matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | log by accessor .at(integer) |
| evaluation site category | field-default-value-expression |
| type family | collection |

### What must be proven

Obligation: 0 <= Index and Index < AuditByKey.count

Weakest precondition: 0 <= Index and Index < AuditByKey.count

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| AuditByKey | the collection field the .at() accessor reads |
| Index | the index expression passed to .at() |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (a).

A field's `default` expression is materialised once, during construction, and its scope is restricted to fields declared above it in the file — a forward reference is impossible, not merely disallowed (docs/language/precept-language-spec.md:1352, the modifier/default value-expression scope row). No event is in scope at all (declarations are not anchored to any event), so the group's own shared setting ('the index comes from an event argument') cannot literally hold here — the index this cell tests is a second, earlier-declared field's default value instead. No guard, no event args, no pre-state; only class (a), the modifiers of fields declared earlier, is available. This cell folds four other declaration-position categories under this representative; they share the same no-event/no-guard/no-pre-state shape by the same spec citation, but were not all independently exercised.

### The worked examples

**Base — the program the compiler must reject.**

```precept
precept LogByG17DeclDefault

field AuditByKey as log of decimal by integer
field DefaultIndex as integer default 5
field Selected as decimal default AuditByKey.at(DefaultIndex)

event Touch()

on Touch
    -> set Selected = Selected
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: model-derived.

### Respellability

- Resolution: cell-override

- Overrides the family verdict rather than inheriting it: with disposition open and no licensed derivation named, 'is every sound program in the band respellable' cannot be answered yet either — there is no defined contract to respell against. Recorded no (not yet resolvable) rather than left silently defaulted to the family's yes, since that verdict presumes a defined contract this cell does not have.

**What the sources leave unstated or ambiguous here**

- MISSING RULE, reported rather than filled: this obligation (`0 <= Index and Index < F.count`) relates two DIFFERENT subjects — the index and a separate field's live cardinality — not one operand's own interval. The matrix's only written fault decision procedure (Family 4, docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses) computes a single operand's interval from its own declared modifiers and in-scope guard facts; it does not state a derivation for comparing that operand against a second, independently-varying subject. Direct testing this session (src/Precept/Pipeline/ProofEngine.Strategies.cs:390, TryIndexBoundsProof, "Strategy 3: Guard-in-Path Proof") confirms the shipped compiler discharges this obligation through dedicated code — decomposing it into an independently-checked lower bound (type-derived from a `nonnegative`/`positive` modifier, or a matching guard branch) and an independently-checked upper bound (a guard branch comparing the index against the collection's `.count`) — but no paragraph under docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments names this decomposition, and none of its seven closed `validityArguments` entries covers a cross-subject relational comparison discharged by splitting a conjunction across two premise classes. A verbatim whole-condition guard match (`when Index >= 0 and Index < F.count`, tested directly) does NOT discharge at HEAD either (live-verified below), which rules out reading the existing 'Guard normal-form match' argument as already covering this obligation by extension. Disposition is `open`, not `defined`, on every cell in this group for this reason — never invented past what is written.
- This position does NOT mint the catalog-declared obligation at HEAD (live-verified) — a built-power gap, independent of the missing-rule finding below.
- Coordinates name field-default-value-expression as the representative evaluation-site category for this cell. Folded under the same representative (authoring-economy compression across this group's twenty raw categories, not a claim of identical premise availability in every case — see premiseAvailability above for exactly what differs): field-modifier-value-expression, event-arg-modifier-value-expression, collection-inner-type-modifier-value-expression, type-qualifier-expression.
- NOT independently run this session for AuditByKey — analogous program shown for completeness, carried from the List representative (g17/list-declaration) by the identical catalog ProofRequirement shape across all three accessors (src/Precept/Language/Types.cs). Recorded as unmeasured for this site, not assumed to match, per the group's own honesty duty.

**What this cell derives from**

- docs/language/collection-types.md:417 — spec: AuditByKey.at(N) — `log of T by P`
- src/Precept/Language/Types.cs:267 — code: log by accessor .at(integer) catalog declaration: NumericProofRequirement(count>0) plus IndexBoundsProofRequirement(0<=Index<count), Parameters:[PCollectionIndex]
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: Fault family row

## Fields with no rendering rule

Data the generator has no rendering rule for, surfaced verbatim rather than dropped. Each is either a schema addition the generator has not caught up with, or a stray field.

| Where | Field | Value |
|---|---|---|
| cells[g17/list-write].respellability | overrideVerdict | "no" |
| cells[g17/list-guard].respellability | overrideVerdict | "no" |
| cells[g17/list-constraint].respellability | overrideVerdict | "no" |
| cells[g17/list-declaration].respellability | overrideVerdict | "no" |
| cells[g17/log-write].respellability | overrideVerdict | "no" |
| cells[g17/log-guard].respellability | overrideVerdict | "no" |
| cells[g17/log-constraint].respellability | overrideVerdict | "no" |
| cells[g17/log-declaration].respellability | overrideVerdict | "no" |
| cells[g17/logby-write].respellability | overrideVerdict | "no" |
| cells[g17/logby-guard].respellability | overrideVerdict | "no" |
| cells[g17/logby-constraint].respellability | overrideVerdict | "no" |
| cells[g17/logby-declaration].respellability | overrideVerdict | "no" |

