<!--
GENERATED FILE — do not hand-edit.
Source: fault-15-collection-non-empty-constraint-and-message.cells.json
Regenerate: dotnet run --project tools/Precept.MatrixTools -- render-cells docs/Working/obligation-discharge-matrix-2026-07-19-cells
Hand edits will be overwritten. Edit the .cells.json and regenerate.
-->

# Fault family, group 15 — reading a possibly-empty collection in a constraint condition or a message

Family id: fault-15-collection-non-empty-constraint-and-message
Case shape: fault
Definition version: obligation-discharge-matrix-2026-07-19 rev 7 (2026-07-20, draft)

**Where this family's content comes from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: Fault family row: catalog-declared safety precondition at the evaluation site, replacing the write-site axis with evaluation-site category
- docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — an explicit input — matrix: Fault minting is catalog-stamped at evaluation sites, settled and shipped
- docs/language/precept-language-spec.md § 0.7 The Compile-Time and Runtime Guarantee Contract — spec
- docs/language/collection-types.md § Emptiness Safety — catalog
- docs/language/collection-types.md § Access proof obligations — catalog

## Respellability — the family verdict

Can every safe program these rules reject be rewritten into a form they accept? **yes**

Evidence: model-derived
Corpus measurement: pending

Spellings that look rejected but are in fact licensed (kept so the boundary stays findable):

- `when Log.count >= 1 (as the ensure's own pre-verb guard)` — Not licensed — this is a genuine band member, not a licensed non-band example; recorded here with the correction because the family-wide verdict needs the concrete evidence. Live-verified 2026-07-21 at HEAD e1a14d91 (Precept.MatrixTools CLI): `on Finish when Log.count >= 1 ensure Log.last == "closed" because "..."` still rejects with UnguardedCollectionAccess, even though `Log.count >= 1` and `Log.count > 0` are the same fact for an integer count. `>=`/`>` interchange over a `+1`/`-1` shift is not among the adopted normalization rules N1-N14 (normal-form-draft-2026-07-19.md) — no rule folds `x >= 1` into `x > 0` for a bare integer (N14 flips negated comparisons, not this). The licensed respelling `Log.count > 0` (verified in this file's event-ensure-condition cell) closes it. Kept in this field rather than moved to a real 'licensed' example, since the schema names this array for spellings a reader would expect rejected but that are actually accepted — this one is the opposite, a genuine band member — but it is the concrete evidence for the verdict below and is recorded honestly rather than dropped.

**Notes on the family verdict**

- Every cell in this file inherits this verdict unless it states its own override; none does. Evidence is model-derived from one live-verified probe, not the ratification-gate's full corpus measurement (matrix § Ratification protocol, layer 4), which is a separate, later activity this pass does not perform.

**Sources for this verdict**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Respellable bullet: the verdict is stated once per contract family and inherited per cell
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft: N1-N14: no rule equates `count >= 1` with `count > 0`

## Premise classes — the legend

Where a proof may get its facts (want :19, :143): (a) field modifiers, (b) arg constraints, (c) the handler's guard, (d) all rules holding in the pre-state (the inductive hypothesis), (e) evaluating the applicable rule set at an editable-field write — (e)'s status as a genuinely fifth class vs (a) generalized is an open owner item.

## g15/rule-condition — Rule condition reads a possibly-empty collection accessor

Disposition: **defined**.

**Disposition sources**

- docs/language/precept-language-spec.md § `rule` declaration — spec
- docs/Working/what-i-want-2026-07-16.md:104 — want-doc: fault-prevention obligation family

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | CollectionNonEmptyAccess — the 'receiver's .count' > 0 precondition shared verbatim by accessor/{List,Log,LogBy}/{first,last,at}/numeric-0, accessor/{Queue,QueueBy}/peek{,by}/numeric-0, accessor/Set/{min,max}/numeric-0, and accessor/Stack/peek/numeric-0 (15 catalog sites, Types.cs accessor tables); represented here by accessor/List/first/numeric-0 |
| evaluation site category | rule-condition |
| type family | collection |

### What must be proven

Obligation: Items.count > 0

Weakest precondition: Items.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Items | the collection field read through a non-empty-requiring accessor (.first/.last/.at/.peek/.peekby/.min/.max) inside the rule's condition; any of the fifteen catalog sites this cell's operationKind lists collapses to this same schema, since every one of them declares the identical requirement over the receiver's own .count (collection-types.md § Access proof obligations) |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a).

A rule condition is evaluated against the complete working copy at every checkpoint, in every reachable configuration — it is not anchored to any handler, so no guard and no event args are in scope and there is no single pre-state to appeal to (precept-language-spec.md § `rule` declaration; § Expression scope). Field modifiers are the only class this site category ever offers. Live-verified: the base below rejects with exactly one diagnostic naming `Items`, and only a field-declaration modifier discharges it — no guard-shaped addition is even syntactically available at a rule condition.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: a statically-literal `mincount N` (N >= 1) declared on the collection field discharges every non-empty-requiring accessor on it
- Strategy: DeclarationAttribute
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Scan the field's declared modifier list (a finite, statically-declared set) for a literal `mincount N` with N >= 1. Present: the accessor discharges structurally — matches `docs/language/precept-language-spec.md:1681` ('A statically-literal mincount N (N>=1) statically discharges .min/.max/.peek/.peekby/.first/.last access obligations... .at(N) is not discharged — see the `at`-coupled cell in this file'). Absent, `mincount 0`, or no `mincount` at all: reject, naming class (a) as the missing premise.

- docs/language/precept-language-spec.md:1681 — spec
- docs/language/collection-types.md § Access proof obligations — catalog
- docs/compiler/proof-engine.md § Strategy 2: Declaration Attribute Proof — code: the Modifier -> ProofSatisfaction table's `mincount(N)` row: Numeric(Accessor("count"), GreaterThanOrEqual, DeclarationValue)

### What the failing diagnostic must suggest

- For class (a): declare `mincount 1` (or a narrower literal N >= 1) on <Field> so <WP> holds structurally
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet — a schema over <WP>, never a constant

  - No prior witness family demonstrates a class-(a) suggestion; Family 1's worked schemas cover (b) and (c) only. This is a new schema instance, not a restatement of one already ratified.
  - The compiler's ACTUAL diagnostic text ('guard with if Items.count > 0 before accessing .first') does not follow this per-class schema — it is one flat templated string regardless of site, and at a rule condition it suggests an `if` guard that has no legal spelling there at all (rule conditions carry no guard construct). This is an implementation gap against the Vocabulary's suggestion-schema requirement, not a model answer; recorded here rather than silently accepted as conforming.


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept RuleCondListFirst
field Items as list of decimal
event AddItem(Value as decimal)
on AddItem
  -> append Items AddItem.Value
rule Items.first > 0.0 because "the first item must be positive"
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*mincount-1* — other

- Addition: mincount 1 (added to the `Items` field declaration: `field Items as list of decimal mincount 1`)
- Premise classes: (a)
- Derivation: static mincount>=1 modifier discharges the accessor -> DeclarationAttribute
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` locus fits: the addition is a field-declaration modifier, not a transition-row guard or an event-arg-declaration replacement. The `application` DU (row-guard / event-arg-declarations) has no case for a field-modifier addition — recorded as a vocabulary gap, the same shape the README already flags for `construction-defaults`.
- `strategy: DeclarationAttribute` is read off `src/Precept/Pipeline/ProofLedger.cs` (ProofStrategy enum) and `docs/compiler/proof-engine.md § Strategy 2: Declaration Attribute Proof`; the CLI used here reports diagnostics only, not a per-obligation disposition/strategy line, so this attribution is inferred from source, not confirmed by the run itself.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| mincount-1 | mincount 0 (i.e., no cardinality floor — equivalent to omitting the modifier) | mincount 0 imposes no floor; count can still be 0 at the access — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

- No guard construct exists at a rule condition, so the family's `count >= 1` vs `count > 0` band member does not arise here — `mincount N` has no alternate spelling to band against.

**What the sources leave unstated or ambiguous here**

- MISSING RULE, load-bearing for every cell in this file that relies on class (a) alone (this cell, computed-field-expression, quantifier-predicate, and the QueueBy.peekby cell): the matrix's § Validity arguments section (docs/Working/obligation-discharge-matrix-2026-07-19.md:200-217) states exactly seven named arguments, and every one of them is written for event-ARG modifiers (governance at ingress, precept-language-spec.md:268), guards, pre-state induction, or literal defaults. None is written for premise class (a) — FIELD modifiers — as such, which is what a collection's own `mincount` declaration is (spec:266's more general 'operand carries a sufficient constraint — a declared modifier or rule' is the correct grounding, but no argument paragraph has been written from it). The citation above ('Arg-bound interval arithmetic') is the closest existing named argument, not a literal fit: its written text is scoped to arg governance at spec:268, not field declarations. Under the Rule Validity gate ('no cell ratifies whose derivation cites an argument-less rule', docs/Working/obligation-discharge-matrix-2026-07-19.md:197), this cell cannot ratify until that argument is written; it is recorded here as `defined` because its obligation, applicable classes, decision procedure and witnesses are independently derivable and empirically verified, per the matrix's own two-stage separation of 'defined' from 'ratified' (docs/Working/obligation-discharge-matrix-2026-07-19.md § Rule validity).

**What this cell derives from**

- docs/language/precept-language-spec.md § `rule` declaration — spec
- docs/language/precept-language-spec.md § Expression scope — spec
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix

## g15/computed-field-expression — Computed field expression reads a possibly-empty collection accessor

Disposition: **defined**.

**Disposition sources**

- docs/language/precept-language-spec.md § `field` declaration — spec

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | CollectionNonEmptyAccess (see g15/rule-condition for the full 15-site equivalence class); represented here by accessor/Log/last/numeric-0 |
| evaluation site category | computed-field-expression |
| type family | collection |

### What must be proven

Obligation: Events.count > 0

Weakest precondition: Events.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Events | the collection field read through a non-empty-requiring accessor inside a computed field's `<-` expression |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a).

A computed field's `<-` expression is evaluated over the final configuration every time the field is read, in every configuration the entity can occupy — the same all-configurations quantification as a rule condition: no guard, no event args, no single pre-state (precept-language-spec.md § `field` declaration; § Expression scope; § Computed field validation). Only field modifiers are available without further ruling; the live base and discharge below confirm exactly this.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: a statically-literal `mincount N` (N >= 1) declared on the collection field discharges every non-empty-requiring accessor on it
- Strategy: DeclarationAttribute
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Same finite scan as g15/rule-condition: look for a literal `mincount N` (N >= 1) on the field's declared modifier list. Present: discharges. Absent or `mincount 0`: reject, naming class (a).

- docs/language/precept-language-spec.md:1681 — spec
- docs/compiler/proof-engine.md § Strategy 2: Declaration Attribute Proof — code

### What the failing diagnostic must suggest

- For class (a): declare `mincount 1` (or a narrower literal N >= 1) on <Field> so <WP> holds structurally
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept ComputedLogLast
field Events as log of string
field MostRecent as string <- Events.last
event Record(Note as string)
on Record
  -> append Events Record.Note
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*mincount-1* — other

- Addition: mincount 1 (added to the `Events` field declaration)
- Premise classes: (a)
- Derivation: static mincount>=1 modifier discharges the accessor -> DeclarationAttribute
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` locus fits (field-declaration modifier, not a row guard or an event-arg replacement) — same vocabulary gap as g15/rule-condition.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| mincount-1 | mincount 0 | no cardinality floor — count can still be 0 at the read — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

- No guard construct exists at a computed-field expression, so the `count >= 1` vs `count > 0` band member does not arise here.

**What the sources leave unstated or ambiguous here**

- Same premise-class-(a) validity-argument gap as g15/rule-condition; see that cell's notes for the full statement rather than repeating it here.

**What this cell derives from**

- docs/language/precept-language-spec.md § `field` declaration — spec
- docs/language/precept-language-spec.md § Computed field validation — spec
- docs/Working/obligation-discharge-matrix-2026-07-19.md:127 — matrix: computed-field transitivity is a write-site-axis question for constraints, a distinct concern from this evaluation-site cell

## g15/quantifier-predicate-nested-accessor — Quantifier predicate reads a possibly-empty collection accessor on a different collection than the one quantified

Disposition: **defined**.

**Disposition sources**

- docs/language/precept-language-spec.md § Quantifier expression grammar — spec

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | CollectionNonEmptyAccess (see g15/rule-condition for the full 15-site equivalence class); represented here by accessor/Set/min/numeric-0, nested inside a quantifier predicate whose own collection is a different field |
| evaluation site category | quantifier-predicate |
| type family | collection |

### What must be proven

Obligation: Caps.count > 0

Weakest precondition: Caps.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Caps | the non-quantified collection field read through a non-empty-requiring accessor inside a quantifier's parenthesized predicate |
| Amounts | the quantified collection; its binding variable is NOT the subject of this obligation — contrast with the division witness (`each n in Nums (Total / n < 100.0)`), where the binding variable itself is the subject |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft: N10: the binding variable is alpha-renamed; irrelevant here since Caps is not the bound variable

### Which premise classes can discharge it

Applicable classes: (a).

A quantifier predicate inherits the premise classes of whatever site encloses it, plus its own binding-variable-typed premises where the obligation's subject IS the bound variable (precept-language-spec.md § Quantifier expression grammar; § Quantifier binding variable scope). Here the subject (`Caps`) is NOT the bound variable — the predicate merely reads a second collection's accessor — so this cell's available classes are exactly the host's (a rule condition here, which offers class (a) alone). Live-verified: the base rejects naming `Caps`, not `Amounts` or the binding variable `a`, confirming the engine correctly attributes the obligation to the accessor's own receiver rather than to the quantified collection.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: a statically-literal `mincount N` (N >= 1) declared on the read collection field discharges every non-empty-requiring accessor on it, independent of the quantifier nesting it sits inside
- Strategy: DeclarationAttribute
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Same finite scan as g15/rule-condition, applied to the accessor's own receiver (`Caps`), not the quantified collection (`Amounts`). Present: discharges. Absent: reject, naming class (a) and `Caps`.

- docs/language/precept-language-spec.md:1681 — spec
- docs/compiler/proof-engine.md § Strategy 2: Declaration Attribute Proof — code

### What the failing diagnostic must suggest

- For class (a): declare `mincount 1` (or a narrower literal N >= 1) on <Field> so <WP> holds structurally
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept QuantifierNestedAccessor
field Amounts as list of decimal
field Caps as set of decimal
event AddAmount(Value as decimal)
event AddCap(Value as decimal)
on AddAmount
  -> append Amounts AddAmount.Value
on AddCap
  -> add Caps AddCap.Value
rule each a in Amounts (a > Caps.min) because "every amount must clear the lowest cap"
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*mincount-1* — other

- Addition: mincount 1 (added to the `Caps` field declaration)
- Premise classes: (a)
- Derivation: static mincount>=1 modifier discharges the accessor -> DeclarationAttribute
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` locus fits — same vocabulary gap as g15/rule-condition.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| mincount-1 | mincount 0 | no cardinality floor on `Caps` — count can still be 0 at the read — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

- No guard construct exists at a quantifier predicate on the host used here (a rule condition), so the `count >= 1` vs `count > 0` band member does not arise in this witness.

**What the sources leave unstated or ambiguous here**

- Same premise-class-(a) validity-argument gap as g15/rule-condition; not repeated in full here.
- This cell's obligation (the accessor's own fault precondition) is settled and minted independently of ⧖ Q10 — whether a quantified RULE is itself proof surface (docs/Working/obligation-discharge-matrix-2026-07-19.md:117, :312). Q10 concerns the quantified constraint's own establishment/preservation as an invariant; this cell concerns a fault-prevention obligation nested inside the predicate text, which the fault family mints independently of that question (matrix § The minting rule). Carried as adjacent open context per the authoring instruction, not conflated with this cell's own disposition.
- Live evidence that the quantifier-predicate evaluation-site category does mint fault obligations at HEAD (general fact, not specific to this cell): the group's authoring notes cite `each n in Nums (Total / n < 100.0)` raising DivisionByZero with the binding variable correctly identified as the subject. This cell's own live run corroborates the site mints for a DIFFERENT shape too — a nested accessor on a non-bound collection — and correctly attributes the obligation to that collection, not to the quantifier or its binding variable.

**What this cell derives from**

- docs/language/precept-language-spec.md § Quantifier expression grammar — spec
- docs/language/precept-language-spec.md § Quantifier binding variable scope — spec
- docs/language/precept-language-spec.md § Execution Model Properties — spec

## g15/quantifier-predicate-self-reference — Open — a non-per-element accessor on the SAME collection being quantified, inside its own predicate

Disposition: **open**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Vacuity by activation (Family 5) is written only for `when`-guarded rules; no analogous argument is written for a quantifier's own vacuous-when-empty semantics
- docs/Working/obligation-discharge-matrix-2026-07-19.md:312 — matrix: ⧖ Q10, adjacent open context — carried, not answered

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | CollectionNonEmptyAccess; represented here by accessor/Stack/peek/numeric-0, read on the very collection the enclosing quantifier ranges over |
| evaluation site category | quantifier-predicate |
| type family | collection |

**What the sources leave unstated or ambiguous here**

- The specific open question this cell records, distinct from Q10: `each` over an empty collection is vacuously true, and its predicate is never evaluated for any binding — so a non-per-element accessor read on the SAME collection inside the predicate (e.g. `each x in Nums (x >= Nums.peek)`) can never actually fault at runtime when Nums is empty, because the predicate that contains the read is never reached. Is this vacuity a licensed derivation for discharging the accessor's non-empty obligation WITHOUT a `mincount`/guard addition? The matrix's only vacuity argument ('Vacuity by activation', Witness Family 5) is written for a `when`-guarded rule construct — a different mechanism (an activation condition making the WHOLE rule inert) — not for a quantifier's per-binding vacuity. No generative rule in this matrix currently states whether that reasoning transfers.
- Measured at HEAD 2026-07-21 (Precept.MatrixTools CLI, commit e1a14d91): `precept QuantifierSelfReference\nfield Nums as stack of decimal\nevent Push(Value as decimal)\non Push\n  -> push Nums Push.Value\nrule each x in Nums (x >= Nums.peek) because \"every element is at least the top element\"\n` rejects with `UnguardedCollectionAccess: 'Nums' may be empty` — the same diagnostic as every other cell in this file, with no special-casing for the self-reference shape. Adding `mincount 1` to `Nums` discharges it (also live-verified), exactly the same as g15/quantifier-predicate-nested-accessor. So HEAD does NOT implement a vacuity-based discharge here today — but that measurement answers only what the engine does, not what the definition should commit to, which is the open question above.
- This is recorded `open` rather than folded into g15/quantifier-predicate-nested-accessor's `defined` disposition because the two cells' measured discharge behavior happens to coincide (both need `mincount 1`) but the underlying QUESTION differs: g15/quantifier-predicate-nested-accessor's applicable-class set and decision procedure are fully derivable from the matrix's written rules with no open dependency; this cell's is not, because a sound derivation might exist here that the current rules simply have not stated (a possible additional discharge path, not a fact about applicable classes today).

## g15/state-ensure-condition — State residency ensure reads a possibly-empty collection accessor

Disposition: **defined**.

**Disposition sources**

- docs/language/precept-language-spec.md § State/event ensure — spec
- docs/Working/obligation-discharge-matrix-2026-07-19.md:150 — matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | CollectionNonEmptyAccess (see g15/rule-condition for the full 15-site equivalence class); represented here by accessor/Log/last/numeric-0 |
| evaluation site category | state-ensure-condition |
| type family | collection |

### What must be proven

Obligation: Events.count > 0

Weakest precondition: Events.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Events | the collection field read through a non-empty-requiring accessor inside an `in S ensure` condition |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a), (c), (d).

The spec's scope table gives ensure conditions field names only, so event args are not available here; the ensure's own optional pre-verb `when` is available as class (c) (precept-language-spec.md § State/event ensure). Class (d) — a fact from a `rule` holding in the pre-state — is nominally available at this site category (pre-state facts are readable at every checkpoint). Class (b) is NOT applicable to this cell specifically: the read subject is a FIELD, not an event arg, so no arg-constraint can instantiate it, even though the site category generically exposes class (b) for OTHER cells whose subject is an arg. This exclusion is a per-cell fact derived from the read subject, per the matrix's own methodology (Family 1 Base B similarly excludes classes that are nominally imaginable but do not apply to its specific facts).

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: a statically-literal `mincount N` (N >= 1) declared on the collection field discharges every non-empty-requiring accessor on it
- Strategy: DeclarationAttribute
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Same finite modifier scan as g15/rule-condition.

- docs/language/precept-language-spec.md:1681 — spec

**Entry 2 — (c)**

- Derivation: the ensure's own pre-verb `when Events.count > 0` establishes the same fact the ensure condition needs, evaluated at the identical checkpoint with no interceding mutation
- Strategy: GuardInPath
- Validity arguments: Guard normal-form match
- Decision procedure: Normal-form match the ensure condition's collection-count precondition against the pre-verb guard's conjuncts (a finite set) — `Events.count > 0` matches verbatim; `Events.count >= 1` does NOT (no adopted normalization rule equates them; see this file's respellability block). No match: reject, naming class (c).

- docs/compiler/proof-engine.md § Strategy 3: Guard-in-Path Proof — code

- 'Guard normal-form match' is cited as the closest existing named argument, not a literal fit: its written text (matrix § Validity arguments) reasons about an ACTION re-evaluating an RHS after a guard fires. A state ensure has no write at all — the ensure condition is evaluated in the SAME configuration the guard was, with no intervening mutation to reason about, which is a strictly simpler (and at least as sound) case than the one the argument's prose covers. No validity argument has been written specifically for a no-write ensure guard; flagged as a smaller instance of the same class-of-gap noted on g15/rule-condition.

**Entry 3 — (d)**

- Derivation: a `rule Events.count > 0` holding in every reachable pre-state would, in principle, supply the same fact premise class (d) supplies for any other fault obligation
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Not exercised at HEAD for this obligation shape (see the hazard note below) — no decision procedure is confirmed to exist for this specific (premise class, obligation) pair; recorded as unresolved rather than invented.

- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md — review: Part 3 — the verified false-proof hazard for class (d) used as a premise

- HAZARD, per this group's authoring instructions: Slice 2 verified that a business `rule` used as a premise can produce a FALSE proof for a fault obligation (Part 3, Defect B — a numeric divide-by-zero case). Live-verified 2026-07-21 at HEAD e1a14d91 for THIS shape specifically: adding `rule Events.count > 0 because "the log always carries at least one entry"` to the base, with nothing else changed, does NOT discharge — the compile still rejects with the same `UnguardedCollectionAccess`, naming `Events`, and zero other diagnostics. So for this collection-non-empty obligation the measured outcome is 'unresolved-today', not a false accept — the false-proof hazard from Part 3 evidently does not reach this particular (strategy, requirement-kind) combination at HEAD. This is recorded as a genuine, different measured result, not an assumption that the hazard is absent everywhere in this cell's region; if the engine is later extended to consume rule facts for `CollectionCountAccessor`-shaped obligations (docs/compiler/proof-engine.md:783, `FixedReturnAccessor.ReturnNonnegative`), the same false-proof risk Part 3 documents would apply, since nothing in this witness establishes or preserves `Events.count > 0` as an actual invariant (no diagnostic checks the rule's own establishment or preservation at HEAD either).

### What the failing diagnostic must suggest

- For class (a): declare `mincount 1` (or a narrower literal N >= 1) on <Field> so <WP> holds structurally
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (c): add a pre-verb `when` guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept StateEnsureCollNonEmpty
field Events as log of string
state Draft initial
state Done terminal
event Open initial
event Note(Text as string)
event Finish
on Open
  -> append Events "opened"
in Done ensure Events.last == "closed" because "the last entry must record closure"
from Draft on Note
  -> append Events Note.Text
  -> no transition
from Draft on Finish
  -> transition Done
```

Required outcome: reject, naming the missing premise classes (a), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*mincount-1* — other

- Addition: mincount 1 (added to the `Events` field declaration)
- Premise classes: (a)
- Derivation: static mincount>=1 modifier discharges the accessor -> DeclarationAttribute
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` locus fits — same vocabulary gap as g15/rule-condition.

*ensure-guard* — guard

- Addition: when Events.count > 0 (as the ensure's own pre-verb guard: `in Done when Events.count > 0 ensure ...`)
- Premise classes: (c)
- Derivation: pre-verb guard normal-form-equal to the ensure's own precondition -> GuardInPath
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` locus fits: the `application` DU's `row-guard` case is specifically described as a TRANSITION ROW's `when` clause after an event target; a state-anchored ensure's pre-verb `when` (`(in\|to\|from) StateTarget ("when" BoolExpr)? ensure ...`, precept-language-spec.md:994) is a different grammar position keyed by state, not by event. Recorded as a vocabulary gap rather than mechanically misapplied.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| mincount-1 | mincount 0 | no cardinality floor — count can still be 0 at the read — must still reject, same obligation | reject, naming the same obligation |
| ensure-guard | when Events.count >= 0 (as the ensure's own pre-verb guard) | the guard is trivially true and does not entail count > 0 — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: cell-override
- A representative safe-but-rejected spelling: in Done when Events.count >= 1 ensure Events.last == "closed" because "..."
- Its licensed respelling: in Done when Events.count > 0 ensure Events.last == "closed" because "..."

- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft: N1-N14: no rule equates `count >= 1` with `count > 0`

- Live-verified: `when Events.count >= 1` rejects the same as no guard at all; the family's licensedNonBandExamples entry records the exact run against this cell's own event-ensure sibling, which shares the identical guard shape.

**What the sources leave unstated or ambiguous here**

- Same premise-class-(a) validity-argument gap as g15/rule-condition; not repeated in full here.
- The matrix records as open whether the residency fact itself (the entity is in S) is a usable premise, under which class (docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes, `in S ensure` row). That question is about a DIFFERENT fact (state residency) than this cell's subject (collection non-emptiness); not conflated here, but noted since both concern this same evaluation-site category.
- The class-(d) dischargeContract entry above is NOT paired with a witness: live-verified 2026-07-21 at HEAD e1a14d91, adding `rule Events.count > 0 because "the log always carries at least one entry"` to the base (nothing else changed) still REJECTS with the same UnguardedCollectionAccess, no other diagnostics. Recorded in prose rather than as a fabricated witness triple, for the same reason given on g15/event-ensure-condition's identical note: an accepting `dischargeWitness` cannot honestly be written when the run rejects, and no honest near-miss exists for an addition that never discharged in the first place.

**What this cell derives from**

- docs/language/precept-language-spec.md § State/event ensure — spec
- docs/Working/obligation-discharge-matrix-2026-07-19.md:150 — matrix
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md — review: Part 3, the false-proof hazard this cell's class-(d) entry is measured against

## g15/event-ensure-condition — Event precondition ensure reads a possibly-empty collection accessor

Disposition: **defined**.

**Disposition sources**

- docs/language/precept-language-spec.md § State/event ensure — spec
- docs/Working/obligation-discharge-matrix-2026-07-19.md:153 — matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | CollectionNonEmptyAccess (see g15/rule-condition for the full 15-site equivalence class); represented here by accessor/Log/last/numeric-0 |
| evaluation site category | event-ensure-condition |
| type family | collection |

### What must be proven

Obligation: Log.count > 0

Weakest precondition: Log.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Log | the collection field read through a non-empty-requiring accessor inside an `on E ensure` condition |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a), (c), (d).

An event ensure runs before any mutation of the plan, so the pre-state is available and the ensure's own pre-verb `when` is class (c) (precept-language-spec.md § State/event ensure; § Event arg access). Class (b) is nominally available at this site category (event args ARE in scope, and this is the ingress door that manufactures arg facts for other sites), but does NOT apply to this cell: the read subject (`Log`) is a FIELD, not an event arg, so no arg-constraint can instantiate it. Class (d) is nominally available (pre-state facts are readable before mutation) — see the hazard entry below.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: a statically-literal `mincount N` (N >= 1) declared on the collection field discharges every non-empty-requiring accessor on it
- Strategy: DeclarationAttribute
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Same finite modifier scan as g15/rule-condition.

- docs/language/precept-language-spec.md:1681 — spec

**Entry 2 — (c)**

- Derivation: the ensure's own pre-verb `when Log.count > 0` establishes the same fact the ensure condition needs, at the identical pre-mutation checkpoint
- Strategy: GuardInPath
- Validity arguments: Guard normal-form match
- Decision procedure: Normal-form match the ensure condition's collection-count precondition against the pre-verb guard's conjuncts. `Log.count > 0` matches; `Log.count >= 1` does not (no adopted normalization rule equates them). No match: reject, naming class (c).

- docs/compiler/proof-engine.md § Strategy 3: Guard-in-Path Proof — code

- Same 'closest-fit, not literal' citation caveat as g15/state-ensure-condition's guard entry; not repeated in full.

**Entry 3 — (d)**

- Derivation: a `rule Log.count > 0` holding in every reachable pre-state would, in principle, supply the same fact premise class (d) supplies for any other fault obligation
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Not confirmed to exist at HEAD for this obligation shape (see hazard note) — recorded as unresolved rather than invented.

- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md — review: Part 3 — the verified false-proof hazard for class (d) used as a premise

- HAZARD, same shape as g15/state-ensure-condition. Live-verified 2026-07-21 at HEAD e1a14d91: adding `rule Log.count > 0 because "the log always carries at least one entry"` to the base, nothing else changed, does NOT discharge — still rejects with the same `UnguardedCollectionAccess`, naming `Log`, no other diagnostics. Unresolved-today, not a false accept, for this specific (strategy, requirement-kind) pair — recorded as a genuine measured result, with the same forward-looking caveat as g15/state-ensure-condition's identical entry.

### What the failing diagnostic must suggest

- For class (a): declare `mincount 1` (or a narrower literal N >= 1) on <Field> so <WP> holds structurally
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (c): add a pre-verb `when` guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept EventEnsureCollNonEmpty
field Log as log of string
state Draft initial
state Done terminal
event Open initial
event Note(Text as string)
event Finish
on Open
  -> append Log "opened"
on Finish ensure Log.last == "closed" because "the log must already record closure before finishing"
from Draft on Note
  -> append Log Note.Text
  -> no transition
from Draft on Finish
  -> transition Done
```

Required outcome: reject, naming the missing premise classes (a), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*mincount-1* — other

- Addition: mincount 1 (added to the `Log` field declaration)
- Premise classes: (a)
- Derivation: static mincount>=1 modifier discharges the accessor -> DeclarationAttribute
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` locus fits — same vocabulary gap as g15/rule-condition.

*ensure-guard* — guard

- Addition: when Log.count > 0 (as the event ensure's own pre-verb guard: `on Finish when Log.count > 0 ensure ...`)
- Premise classes: (c)
- Derivation: pre-verb guard normal-form-equal to the ensure's own precondition -> GuardInPath
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` locus fits — an event ensure's pre-verb `when` is a distinct grammar position from a transition row's guard; same vocabulary gap noted on g15/state-ensure-condition.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| mincount-1 | mincount 0 | no cardinality floor — must still reject, same obligation | reject, naming the same obligation |
| ensure-guard | when Log.count >= 0 (as the event ensure's own pre-verb guard) | the guard is trivially true and does not entail count > 0 — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: cell-override
- A representative safe-but-rejected spelling: on Finish when Log.count >= 1 ensure Log.last == "closed" because "..."
- Its licensed respelling: on Finish when Log.count > 0 ensure Log.last == "closed" because "..."

- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft: N1-N14: no rule equates `count >= 1` with `count > 0`

- This is the exact run recorded in the family-level `licensedNonBandExamples` entry — live-verified rejection of `>= 1`, live-verified acceptance of `> 0`.

**What the sources leave unstated or ambiguous here**

- Same premise-class-(a) validity-argument gap as g15/rule-condition; not repeated in full here.
- The class-(d) dischargeContract entry above is NOT paired with a witness: live-verified 2026-07-21 at HEAD e1a14d91, adding `rule Log.count > 0 because "the log always carries at least one entry"` to the base (nothing else changed) still REJECTS with the same UnguardedCollectionAccess, no other diagnostics — the addition does not accept, so it cannot honestly be recorded as a `dischargeWitness` (which requires `expected.outcome: accept`) nor would a near-miss make sense to weaken (the schema's near-miss pairing rule requires exactly one per non-null addition, which this measured-as-rejecting addition cannot honestly satisfy either way). The finding is recorded here in prose instead of as a fabricated witness triple: class (d) is unresolved-today for this obligation shape, and — separately — proving the ADDED rule ITSELF sound (an inductive collection-cardinality invariant over Log's append-only growth) is not covered by any written validity argument either, a second, independent missing-rule layered under the first.

**What this cell derives from**

- docs/language/precept-language-spec.md § State/event ensure — spec
- docs/language/precept-language-spec.md § Event arg access — spec
- docs/Working/obligation-discharge-matrix-2026-07-19.md:153 — matrix

## g15/reject-message-interpolation — Reject-row message interpolation reads a possibly-empty collection accessor — the site the want doc names, and the site that does not mint at HEAD

Disposition: **defined**.

**Disposition sources**

- docs/Working/what-i-want-2026-07-16.md:72 — want-doc: the exact interpolation-position example the want doc uses to argue the fault axis cannot reuse the write-site axis
- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | CollectionNonEmptyAccess (see g15/rule-condition for the full 15-site equivalence class); represented here by accessor/Log/last/numeric-0 |
| evaluation site category | reject-message-interpolation |
| type family | collection |

### What must be proven

Obligation: Log.count > 0

Weakest precondition: Log.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Log | the collection field read through a non-empty-requiring accessor inside a `-> reject StringExpr` interpolation hole |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a), (c), (d).

The row matched, so its own guard holds (class c) and the triggering event's args are in scope (precept-language-spec.md § Interpolation Reassembly). Class (b) is nominally available (event args in scope) but does not apply to this cell — the read subject is a field, not an arg. Class (d) is nominally available (pre-state facts). The MODEL commits to an obligation here regardless of premise availability (matrix § Per-family case shapes, Fault family row: 'the site may sit in a guard or a reject-row interpolation with no write at all' — citing want :72's own `{-Balance / PlanRepayment.Months}` example verbatim). Measured at HEAD, this evaluation-site category does not mint the obligation at all (see builtStatus below) — a live gap in the shipped engine, not a change to the model's commitment.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: a statically-literal `mincount N` (N >= 1) declared on the collection field discharges every non-empty-requiring accessor on it
- Strategy: DeclarationAttribute
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Same finite modifier scan as g15/rule-condition — stated per the model; not exercised at HEAD since nothing mints here to discharge (see builtStatus).

- docs/language/precept-language-spec.md:1681 — spec

**Entry 2 — (c)**

- Derivation: the row's own guard, true when the row fired, is read at the same evaluation occasion as the reject message
- Validity arguments: Guard normal-form match
- Decision procedure: Stated per the model, same shape as g15/state-ensure-condition's guard entry — not exercised at HEAD since nothing mints here to discharge.

**Entry 3 — (d)**

- Derivation: a `rule Log.count > 0` holding in every reachable pre-state would, in principle, supply the fact
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Not exercised at HEAD (nothing mints to discharge); same hazard caveat as the other two cells in this file that carry class (d) applies IN PRINCIPLE, if and when this site is built to mint at all.

- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md — review: Part 3 — the false-proof hazard, carried forward as a standing caveat for this class even though it cannot be exercised at this site today

### What the failing diagnostic must suggest

- For class (a): declare `mincount 1` (or a narrower literal N >= 1) on <Field> so <WP> holds structurally
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept RejectInterpCollNonEmpty
field Log as log of string
state Draft initial
state Done terminal
event Open initial
event Note(Text as string)
event Finish(Force as boolean)
on Open
  -> append Log "opened"
from Draft on Note
  -> append Log Note.Text
  -> no transition
from Draft on Finish when Finish.Force
  -> transition Done
from Draft on Finish
  -> reject "most recent entry was: {Log.last}"
```

Required outcome: reject, naming the missing premise classes (a), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*mincount-1* — other

- Addition: mincount 1  # added to the `Log` field declaration
- Premise classes: (a)
- Derivation: declared `mincount 1` gives `Log.count >= 1`, which entails the obligation `Log.count > 0` on the integer count (the contract's class-(a) entry above)
- Strategy: DeclarationAttribute
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (the reject-row message interpolation position mints no fault obligation at all at HEAD (measured 2026-07-21, commit e1a14d91) — there is nothing here for the addition to discharge; the clean compile is the site failing to mint, not the addition being recognized as a discharge)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live run (2026-07-21, HEAD e1a14d91): base + `mincount 1` compiles with zero diagnostics — but so does the base itself, so the run attests only the `builtStatus: unresolved-today` claim (the site does not mint), never the `expected: accept` claim, which is the model's committed power.
- Recorded in the schema's own vocabulary rather than left empty. The earlier empty `discharges` array was a validator error (the schema's defined-cell arm requires at least one entry); the sibling encoding — a discharge carrying `builtStatus: unresolved-today` plus a `builtStatusNote` naming the non-minting — is already in use at `fault-14-collection-non-empty-guard-positions.cells.json`, so no schema gap blocked recording it.
- `application` is omitted: this addition changes a field declaration's modifier list, which is neither a row guard nor an event-arg declaration, and the schema's `application` DU carries no locus for a field-modifier addition. The cell→test conversion cannot apply this discharge mechanically today; recorded as a tooling gap, not worked around.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| mincount-1 | mincount 0  # added to the `Log` field declaration | `mincount 0` leaves the compile-time lower bound at 0, which still admits the empty collection, so `Log.count > 0` is not entailed — must still reject under the model, naming the same obligation. | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

- Not exercised: with nothing minted at this site at HEAD, there is no live acceptance to band-test against.

**What the sources leave unstated or ambiguous here**

- SCHEMA GAP, honestly recorded: `baseWitness` has no `builtStatus`/`builtStatusNote` field (unlike `dischargeWitness`), so there is no schema-native place to record 'the model says reject, the engine does not.' Recorded here instead: live-verified 2026-07-21 at HEAD e1a14d91 (Precept.MatrixTools CLI) — the exact base program above compiles with ZERO diagnostics. `provenance.mark: live-verified` on the base above is honest (I ran this exact source and am reporting what came back); `expected.outcome: reject` is the MODEL's committed claim per the citations above, not a claim the run corroborated. This is a live gap, not a passing discharge — no `discharges`/`nearMisses` are recorded because there is nothing to add source to: the base already 'passes' today for the wrong reason (the obligation was never minted), and inventing a discharge witness against an obligation that doesn't exist at HEAD would misrepresent what was tested.
- This is the exact evaluation-site category the group's disposition summary generalizes from the division-fault measurement (`s2.precept`/`rj.precept`, commit e1a14d91: `Total / Parts` in a reject-row interpolation compiled clean with no `DivisionByZero`). This cell's own independent run confirms the same non-minting for the collection-non-empty requirement kind, at the same evaluation-site category — the gap is per-SITE, not per-requirement-kind.
- The `dischargeContract` entries above state the model's committed derivations (owed once this site is built to mint), consistent with the matrix's committed-power/built-power separation; none is exercised by a live witness because there is nothing to discharge at HEAD.
- Missing premise-class-(a)/(c) validity-argument caveats noted on earlier cells apply here too, in the same closest-fit form; not repeated in full.
- The discharge and near-miss recorded here are the model's answer: at HEAD this evaluation-site category mints nothing, so neither the accept nor the reject was observed. The near-miss is marked `model-derived` for that reason rather than carrying a live mark beside an unobserved rejection.

**What this cell derives from**

- docs/Working/what-i-want-2026-07-16.md:72 — want-doc
- docs/Working/obligation-discharge-matrix-2026-07-19.md:154 — matrix
- docs/language/precept-language-spec.md § Interpolation Reassembly — spec

## g15/constraint-rationale-interpolation — Rule/ensure `because` rationale interpolation reads a possibly-empty collection accessor — also does not mint at HEAD

Disposition: **defined**.

**Disposition sources**

- docs/language/precept-language-spec.md § `rule` declaration — spec
- docs/language/precept-language-spec.md § Interpolation Reassembly — spec

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | CollectionNonEmptyAccess (see g15/rule-condition for the full 15-site equivalence class); represented here by accessor/Log/last/numeric-0 |
| evaluation site category | constraint-rationale-interpolation |
| type family | collection |

### What must be proven

Obligation: Log.count > 0

Weakest precondition: Log.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Log | the collection field read through a non-empty-requiring accessor inside a rule's or ensure's mandatory `because StringExpr` |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a).

Structurally the reject-row interpolation's twin — an explanatory string carrying arbitrary expressions — but it hangs off a CONSTRAINT rather than a ROW, so no event args are in scope on the rule and state-ensure forms this cell's witness uses. Only field modifiers are unconditionally available; class (b) is nominally listed for this site category (per the event-ensure `because` variant, where args ARE in scope) but does not apply to this cell's witness, which uses a bare `rule ... because "..."` with no event in scope at all.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: a statically-literal `mincount N` (N >= 1) declared on the collection field discharges every non-empty-requiring accessor on it
- Strategy: DeclarationAttribute
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Same finite modifier scan as g15/rule-condition — stated per the model; not exercised at HEAD since nothing mints here to discharge (see builtStatus).

- docs/language/precept-language-spec.md:1681 — spec

### What the failing diagnostic must suggest

- For class (a): declare `mincount 1` (or a narrower literal N >= 1) on <Field> so <WP> holds structurally
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept RationaleInterpCollNonEmpty
field Log as log of string
event Note(Text as string)
on Note
  -> append Log Note.Text
rule Log.count >= 0 because "most recent entry: {Log.last}"
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*mincount-1* — other

- Addition: mincount 1  # added to the `Log` field declaration
- Premise classes: (a)
- Derivation: declared `mincount 1` gives `Log.count >= 1`, which entails the obligation `Log.count > 0` on the integer count (the contract's class-(a) entry above)
- Strategy: DeclarationAttribute
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (the `because` rationale interpolation position mints no fault obligation at all at HEAD (measured 2026-07-21, commit e1a14d91) — there is nothing here for the addition to discharge; the clean compile is the site failing to mint, not the addition being recognized as a discharge)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools (dotnet temp/slice3/bin/Precept.MatrixTools/release/Precept.MatrixTools.dll), 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live run (2026-07-21, HEAD e1a14d91): base + `mincount 1` compiles with zero diagnostics — but so does the base itself, so the run attests only the `builtStatus: unresolved-today` claim (the site does not mint), never the `expected: accept` claim, which is the model's committed power.
- Recorded in the schema's own vocabulary rather than left empty. The earlier empty `discharges` array was a validator error (the schema's defined-cell arm requires at least one entry); the sibling encoding — a discharge carrying `builtStatus: unresolved-today` plus a `builtStatusNote` naming the non-minting — is already in use at `fault-14-collection-non-empty-guard-positions.cells.json`, so no schema gap blocked recording it.
- `application` is omitted: this addition changes a field declaration's modifier list, which is neither a row guard nor an event-arg declaration, and the schema's `application` DU carries no locus for a field-modifier addition. The cell→test conversion cannot apply this discharge mechanically today; recorded as a tooling gap, not worked around.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| mincount-1 | mincount 0  # added to the `Log` field declaration | `mincount 0` leaves the compile-time lower bound at 0, which still admits the empty collection, so `Log.count > 0` is not entailed — must still reject under the model, naming the same obligation. | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

- Not exercised: nothing minted at this site at HEAD.

**What the sources leave unstated or ambiguous here**

- Same schema gap as g15/reject-message-interpolation: no `builtStatus` field exists on `baseWitness`. Live-verified 2026-07-21 at HEAD e1a14d91 (Precept.MatrixTools CLI) — the base program above compiles with ZERO diagnostics; `Log.count >= 0` (the rule's actual condition, chosen trivially true so the file is otherwise well-formed) raises nothing, and `Log.last` inside the `because` string raises nothing either. `provenance.mark: live-verified` reports the run honestly; `expected.outcome: reject` is the model's claim per the site citations, not a claim the run corroborated. No discharges/near-misses recorded, for the same reason as g15/reject-message-interpolation.
- The matrix records as open whether the failure context itself (the constraint's condition is false when the message renders) is a usable premise (docs/Working/obligation-discharge-matrix-2026-07-19.md § 'Missing rules' item 8, want-doc :19 area). That question is about a different fact than this cell's subject; not conflated here.
- The discharge and near-miss recorded here are the model's answer: at HEAD this evaluation-site category mints nothing, so neither the accept nor the reject was observed. The near-miss is marked `model-derived` for that reason rather than carrying a live mark beside an unobserved rejection.

**What this cell derives from**

- docs/language/precept-language-spec.md § `rule` declaration — spec
- docs/language/precept-language-spec.md § State/event ensure — spec
- docs/language/precept-language-spec.md § Interpolation Reassembly — spec

## g15/at-accessor-index-and-nonempty-coupling — Open — `.at(N)`'s non-empty (numeric-0) half is not independently dischargeable from its index-bounds half at HEAD

Disposition: **open**.

**Disposition sources**

- docs/language/precept-language-spec.md:1681 — spec: '.at(N) is not discharged [by mincount] — it needs an index-bounds guard N >= 0 and N < count, which count >= 1 does not provide'

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | accessor/List/at/numeric-0, coupled at the same catalog site to accessor/List/at/indexbounds-1 (out of scope for this group — see notes) |
| evaluation site category | rule-condition |
| type family | collection |

**What the sources leave unstated or ambiguous here**

- The specific open question: `.at(N)` is catalog-declared with TWO separate `ProofRequirement` entries at one site — a `Numeric` one ('Collection must be non-empty', this group's scope) and an `IndexBounds` one ('Index must be within bounds [0, count)', a different group's scope). The matrix's own written decision procedures are per-obligation, but at HEAD the two do not surface as two separate obligations in the diagnostic stream at all: measured 2026-07-21 (Precept.MatrixTools CLI, commit e1a14d91), `rule Items.at(0) > 0.0 because "..."` with `Items` unconstrained raises ONLY `IndexBoundsGuard` (twice, worded slightly differently — see below), never `UnguardedCollectionAccess`. Adding `mincount 1` to `Items` reduces this to a SINGLE `IndexBoundsGuard` but does not clear the file — `.at(0)` still requires its own index-bounds guard regardless of `mincount`. So this group's numeric-0 obligation for `.at` cannot be witnessed in isolation the way it can for `.first`/`.last`/`.peek`/`.min`/`.max`: at HEAD there is no diagnostic naming 'Collection must be non-empty' independently of the index-bounds check for this specific accessor, and the matrix does not state whether the two catalog entries are meant to surface as one merged obligation or two. Recorded `open` rather than folded into another cell's `defined` disposition, per this group's ground rules against inventing an answer where the written rules do not decide the shape.
- Live-verified 2026-07-21 at HEAD e1a14d91 (Precept.MatrixTools CLI), unconstrained `Items`: `[Error] IndexBoundsGuard: 'Items' access at index 'index' is not bounds-checked...` AND a second, differently-worded `[Error] IndexBoundsGuard: 'Items' access at index ''0'' is not bounds-checked...` — two diagnostics for what the source declares as one `.at(0)` call, with no `UnguardedCollectionAccess` at all. With `mincount 1` added, only the second ('0'-quoted) line remains. This double-diagnostic shape is itself an observation worth flagging to the group owning `IndexBoundsGuard` (indexbounds-1) — it is reported here as measured evidence, not analyzed further, since resolving it is outside this group's scope.
- This cell intentionally overlaps a catalog site (`accessor/List/at/numeric-0`, and by the same reasoning `Log/LogBy`'s `.at`) that also appears, folded into the general schema, in g15/rule-condition's operationKind equivalence class. That fold was written before this specific measurement; this cell narrows the claim for `.at` alone. `.at` should be treated as NOT covered by g15/rule-condition's clean discharge story until this open question resolves — flagged rather than silently left ambiguous between the two cells.
- Out of scope, not decided here: whatever cell in a different group owns `indexbounds-1` for `.at(N)` should read this cell before authoring its own — the two obligations' coupling at one call site is exactly the kind of cross-group interaction the matrix's Composition check section (docs/Working/obligation-discharge-matrix-2026-07-19.md § Composition check) says per-cell isolation does not by itself resolve.

## g15/computed-field-two-parameter-collection — Computed field expression reads a possibly-empty two-parameter collection accessor (`queue of T by P`)

Disposition: **defined**.

**Disposition sources**

- docs/language/collection-types.md § Access proof obligations — catalog

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | accessor/QueueBy/peekby/numeric-0 — demonstrating the same schema as g15/rule-condition holds for the two-parameter `queue of T by P` kind, not only the single-parameter kinds |
| evaluation site category | computed-field-expression |
| type family | collection |

### What must be proven

Obligation: ClaimQueue.count > 0

Weakest precondition: ClaimQueue.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| ClaimQueue | a `queue of T by P` field read through `.peekby` (or `.peek`) inside a computed field's `<-` expression |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet
- docs/Working/normal-form-draft-2026-07-19.md — normal-form-draft

### Which premise classes can discharge it

Applicable classes: (a).

Same reasoning as g15/computed-field-expression: an all-configurations quantification with only field modifiers available. This cell exists to confirm the schema holds for the two-type-parameter collection kind (`queue of T by P`) and not only the single-parameter kinds g15/computed-field-expression and g15/rule-condition demonstrate — `.peekby`'s obligation is declared identically to `.peek`'s (docs/language/collection-types.md § Access proof obligations: `QueueByPField.peekby` \| `QueueByPField.count > 0`).

### The discharge contract — exactly when this counts as proven

**Entry 1 — (a)**

- Derivation: a statically-literal `mincount N` (N >= 1) declared on the collection field discharges every non-empty-requiring accessor on it, including `.peek`/`.peekby` on `queue of T by P`
- Strategy: DeclarationAttribute
- Validity arguments: Arg-bound interval arithmetic
- Decision procedure: Same finite modifier scan as g15/rule-condition.

- docs/language/precept-language-spec.md:1681 — spec
- docs/language/collection-types.md:1147 — catalog: 'A statically-literal mincount 1 discharges .peek/.peekby access obligations' for queue of T by P specifically

### What the failing diagnostic must suggest

- For class (a): declare `mincount 1` (or a narrower literal N >= 1) on <Field> so <WP> holds structurally
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept ComputedQueueByPeekby
field ClaimQueue as queue of string by integer
field TopSeverity as integer <- ClaimQueue.peekby
event FileClaim(ClaimId as string, Severity as integer)
on FileClaim
  -> enqueue ClaimQueue FileClaim.ClaimId by FileClaim.Severity
```

Required outcome: reject, naming the missing premise classes (a), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools CLI, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*mincount-1* — other

- Addition: mincount 1 (added to the `ClaimQueue` field declaration)
- Premise classes: (a)
- Derivation: static mincount>=1 modifier discharges the accessor -> DeclarationAttribute
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools CLI, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- No `application` locus fits — same vocabulary gap as g15/rule-condition.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| mincount-1 | mincount 0 | no cardinality floor — count can still be 0 at the read — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

- No guard construct exists at a computed-field expression, so the `count >= 1` vs `count > 0` band member does not arise here.

**What the sources leave unstated or ambiguous here**

- Same premise-class-(a) validity-argument gap as g15/rule-condition; not repeated in full here.

**What this cell derives from**

- docs/language/collection-types.md § Access proof obligations — catalog
- docs/language/precept-language-spec.md § `field` declaration — spec

## Fields with no rendering rule

Data the generator has no rendering rule for, surfaced verbatim rather than dropped. Each is either a schema addition the generator has not caught up with, or a stray field.

| Where | Field | Value |
|---|---|---|
| cells[g15/state-ensure-condition].respellability | overrideVerdict | "yes" |
| cells[g15/event-ensure-condition].respellability | overrideVerdict | "yes" |

