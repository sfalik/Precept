<!--
GENERATED FILE — do not hand-edit.
Source: fault-18-collection-action-preconditions.cells.json
Regenerate: dotnet run --project tools/Precept.MatrixTools -- render-cells docs/Working/obligation-discharge-matrix-2026-07-19-cells
Hand edits will be overwritten. Edit the .cells.json and regenerate.
-->

# Fault family — collection actions with a safety precondition (group 18)

Family id: fault-18-collection-action-preconditions
Case shape: fault
Definition version: obligation-discharge-matrix-2026-07-19 rev 7 (2026-07-20, draft)

**Where this family's content comes from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § The cell — matrix: the fault-family cell definition and base-minimality clause
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: premise classes, discharge mechanisms, suggestion schema, write-plan mechanics
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the Fault family row: catalog-declared safety precondition, premise classes (b)/(c)/(a)
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses — matrix: the Family 4 paragraph — fault family, arg-constraint discharge; the shared decision-procedure precedent this group's contracts follow
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Constraint kinds — matrix: a rule's attribution is fields and definition scope only — no rule mentions an event argument, which is why class (d) cannot supply a fact about an authored index operand
- docs/language/collection-types.md § Emptiness Safety — spec
- docs/language/collection-types.md § Mutation proof obligations — spec
- docs/language/collection-types.md § Guard pattern — spec
- docs/language/collection-types.md § Constraint Catalog — spec
- docs/language/collection-types.md § `list of T` — spec
- docs/language/collection-types.md § `queue of T by P` — spec
- src/Precept/Language/Actions.cs:108 — code: the six catalog ActionMeta entries this group's sites derive from span lines 108-266 (Dequeue, Pop, Insert, RemoveAt, DequeueBy)
- src/Precept/Language/ProofRequirement.cs:249 — code: IndexBoundsMode (StrictlyBefore for remove/at-access, AtOrBefore for insert) and IndexBoundsProofRequirement, spanning lines 249-270
- src/Precept/Pipeline/ProofEngine.Strategies.cs:390 — code: TryIndexBoundsProof — the guard-in-path strategy this group's index-bounds discharge contracts are measured against, spanning lines 390-474; the guard-required-unconditionally rule for the upper bound is at lines 441-445
- src/Precept/Pipeline/ProofEngine.Diagnostics.cs:303 — code: TryCreateCollectionSafetyDiagnostic — site-shape dispatch to UnguardedCollectionMutation for the action case, spanning lines 303-342
- src/Precept/Language/Modifiers.cs:195 — code: ModifierKind.Mincount ProofSatisfaction.Numeric; full declaration spans lines 195-208
- src/Precept/Language/Modifiers.cs:69 — code: ModifierKind.Nonnegative ProofSatisfaction.Numeric(SelfValue, GreaterThanOrEqual, Constant(0)); full declaration spans lines 69-80
- src/Precept/Pipeline/ProofLedger.cs:100 — code: ProofStrategy enum, including CollectionGrowth = 11 ("a count > 0 obligation discharged by a prior grow action in the same chain"), spanning lines 100-115
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — owner-ruling, 2026-07-20: the false-proof hazard for rule-premise discharges — tested directly for every class-(d) route in this group; every attempt rejected (unresolved-today), so the hazard did not materialize in any witness recorded here

## Respellability — the family verdict

Can every safe program these rules reject be rewritten into a form they accept? **yes**

Evidence: model-derived
Corpus measurement: pending

Spellings that look rejected but are in fact licensed (kept so the boundary stays findable):

- `from Cart on ReorderItem when ReorderItem.FromIndex < LineItems.count and ReorderItem.ToIndex < LineItems.count and LineItems.count > 0 -> insert LineItems ... at ReorderItem.ToIndex -> remove LineItems at ReorderItem.FromIndex` — a real corpus row discharging two index-bounds obligations (insert and remove-at, on the same list) by ANDing three separate guard conjuncts in one when clause — exactly the multi-conjunct guard-match reading this group's guard-alone contract entries rest on, not an invented extension
  - samples/shopping-cart.precept:187 — code: the four-row ReorderItem sequence spans lines 187-196


**Notes on the family verdict**

- Every sound band member's business intent respells into a licensed guard restating F.count > 0 (operand-free cells) or a guard/arg-modifier combination stating both halves of the index-bounds WP (operand cells), mirroring Witness Family 1 and group 13's respellability findings.
- One concrete band member found this session, generalizing group 13's finding to the action (mutating) case: `F.count >= 1` is not recognized by the guard-match decision procedure for the operand-free obligation (live-verified); its licensed respelling is `F.count > 0` itself.

**Sources for this verdict**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Ratification protocol — matrix: layer 4: corpus measurement is the hard gate

## Premise classes — the legend

Where a proof may get its facts (want :19, :143): (a) field modifiers, (b) arg constraints, (c) the handler's guard, (d) all rules holding in the pre-state (the inductive hypothesis), (e) evaluating the applicable rule set at an editable-field write — (e)'s status as a genuinely fifth class vs (a) generalized is an open owner item.

## g18/dequeue-nonempty — Dequeue — queue must be non-empty (operand-free action precondition)

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | action/Dequeue/numeric-0 |
| evaluation site category | operand-free-action-precondition |
| type family | collection |

### What must be proven

Obligation: F.count > 0

Weakest precondition: F.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| F | the queue field the dequeue action mutates |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (c), (d).

Dequeue carries no authored operand — its ActionMeta requirement names the collection field itself as the subject, so no event argument is in play at all and class (b) never applies (there is nothing to constrain). Class (a) (a mincount modifier) does NOT independently apply: declaring mincount on a field a shrinking action mutates introduces its own carrier obligation (CountBoundViolation, PRE0136) that itself requires a guard, and the guard that closes that carrier obligation already closes this one on its own — mincount contributes no discharge power beyond what the guard alone provides (live-verified, this cell; see notes). Class (c) applies at every occurrence of this obligation, since a guard position is always in scope wherever the action fires. Class (d) applies wherever a pre-state exists for this obligation.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: guard conjunct normal-form-equal to the WP (F.count > 0) -> guard-match
- Strategy: GuardInPath
- Capability tier: 1a
- Validity arguments: Guard normal-form match
- Decision procedure: Normal-form match of the WP (F.count > 0) against the row's guard conjuncts (a finite set). A conjunct normal-form-equal to F.count > 0 discharges; a semantically-equivalent but non-normal-form spelling (e.g. F.count >= 1) does not match under the stated normalization and is rejected (live-verified, this group) — a sound-but-unprovable band member, not a near-miss.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Guard normal-form match
- docs/language/collection-types.md § Guard pattern — spec: "the proof engine recognizes F.count > 0 in the when clause as sufficient proof for ... dequeue, and pop operations"

**Entry 2 — (d)**

- Derivation: a rule stating F.count > 0 (or a bound entailing it) holds in the pre-state, and no earlier action in the plan writes F before this dequeue, so the fact is frame-preserved to the precondition check
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: Look up a rule establishing F.count > 0 (or an entailing bound) as holding in the pre-state, and confirm no earlier write-plan action writes F before this one. Absent such a rule, fails the class.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Inductive hypothesis plus sign monotonicity
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — owner-ruling, 2026-07-20: false-proof hazard for rule-premise discharges — tested directly here rather than assumed; the rule premise did not discharge at HEAD (still rejects), so no false accept occurred

### What the failing diagnostic must suggest

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the suggestion-schema bullet

- For class (d): restate <WP> as a rule holding in the pre-state, established at construction and preserved by every other write site of the collection
  - docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — owner-ruling, 2026-07-20

  - HEAD does not consume a rule as a premise for this obligation today (live-verified) — where it is built, the false-proof hazard applies (Defect B).


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept CollectionActionG18Dequeue

field WorkQueue as queue of string maxlength 200
field LastItem as string optional maxlength 200

state Active initial

event Begin initial
event Process

on Begin
    -> enqueue WorkQueue "seed"
    -> set LastItem = "none"

from Active on Process
    -> dequeue WorkQueue into LastItem
    -> no transition
```

Required outcome: reject, naming the missing premise classes (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*guard-nf* — guard

- Addition: when WorkQueue.count > 0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the WP -> guard-match
- Strategy: GuardInPath
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: appended to the guard of the `on Process` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified in witness-g18/dequeue-discharge-c-guard.precept.

*rule-premise* — other

- Addition: rule WorkQueue.count > 0 because "seeded queue never runs dry"
- Premise classes: (d)
- Derivation: premise (d): rule holds in pre-state; no earlier plan action writes F -> frame-preserved
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Live-verified: with the rule declared and no other premise, the dequeue still raises UnguardedCollectionMutation — this fault check does not consume rule facts as a premise at HEAD.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified in witness-g18/dequeue-discharge-d-rule.precept.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| guard-nf | when WorkQueue.count >= 0 | vacuously true for a non-negative count — carries no information, must still reject, same obligation | reject, naming the same obligation |
| rule-premise | rule WorkQueue.count >= 0 because "..." | vacuous rule (count is never negative) — establishes nothing beyond the type's own range, must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Mincount finding: declaring `mincount 1` on WorkQueue with NO guard does not discharge this obligation cleanly — it converts the fault into a different one (CountBoundViolation, PRE0136, `docs/language/collection-types.md:792`), because mincount on a field a shrinking action mutates carries its own in-band obligation. The guard that closes CountBoundViolation (`when F.count > mincount`) also closes this cell's obligation on its own; mincount contributes no independent or additive discharge power for a MUTATING site, unlike group 13's pure-read accessors where mincount alone is sufficient. Live-verified (this session): `mincount 1` alone -> CountBoundViolation; `mincount 1` + `when F.count > 1` -> clean; `mincount 1` + `when F.count > 0` (weakened) -> CountBoundViolation, not this cell's obligation, so it cannot serve as this cell's near-miss either. Class (a) is therefore omitted from the applicable set rather than force-fit into a contract entry whose near-miss would misrepresent a different obligation as this one.
- The (c) and (d) discharges/near-misses were live-verified for this specific action (Dequeue). Pop and DequeueBy carry the identical NumericProofRequirement catalog shape (Actions.cs) and were spot-verified for the (c) route only (base + discharge + near-miss); see g18/pop-nonempty and g18/dequeueby-nonempty.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: Fault family row: the catalog-declared safety precondition at the evaluation site
- src/Precept/Language/Actions.cs:108 — code: ActionKind.Dequeue — NumericProofRequirement(SelfSubject(count), GreaterThan, 0, "Queue must be non-empty"); full declaration spans lines 108-121
- docs/language/collection-types.md § `queue` — spec: the dequeue action table naming the .count > 0 proof requirement
- docs/language/collection-types.md § Guard pattern — spec

## g18/pop-nonempty — Pop — stack must be non-empty (operand-free action precondition)

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | action/Pop/numeric-0 |
| evaluation site category | operand-free-action-precondition |
| type family | collection |

### What must be proven

Obligation: F.count > 0

Weakest precondition: F.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| F | the stack field the pop action mutates |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (c), (d).

Identical shape and reasoning to g18/dequeue-nonempty: Pop carries no authored operand, class (b) never applies, class (a) is not independently sufficient on a shrinking action (see that cell's notes), class (c) always applies, class (d) applies wherever a pre-state exists.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: guard conjunct normal-form-equal to the WP (F.count > 0) -> guard-match
- Strategy: GuardInPath
- Capability tier: 1a
- Validity arguments: Guard normal-form match
- Decision procedure: Normal-form match of the WP against the row's guard conjuncts (a finite set); see g18/dequeue-nonempty for the full decision procedure, identical here.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Guard normal-form match
- docs/language/collection-types.md § Guard pattern — spec

**Entry 2 — (d)**

- Derivation: a rule stating F.count > 0 holds in the pre-state, and no earlier action in the plan writes F before this pop, so the fact is frame-preserved to the precondition check
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: As g18/dequeue-nonempty.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Inductive hypothesis plus sign monotonicity
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — owner-ruling, 2026-07-20

### What the failing diagnostic must suggest

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix

- For class (d): restate <WP> as a rule holding in the pre-state, established at construction and preserved by every other write site of the collection
  - docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — owner-ruling, 2026-07-20

  - HEAD does not consume a rule as a premise for this obligation today (not independently re-run for Pop; see g18/dequeue-nonempty's live result on the identical catalog shape).


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept CollectionActionG18Pop

field HistoryStack as stack of string maxlength 200
field LastPopped as string optional maxlength 200

state Active initial

event Begin initial
event Process

on Begin
    -> push HistoryStack "seed"
    -> set LastPopped = "none"

from Active on Process
    -> pop HistoryStack into LastPopped
    -> no transition
```

Required outcome: reject, naming the missing premise classes (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*guard-nf* — guard

- Addition: when HistoryStack.count > 0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the WP -> guard-match
- Strategy: GuardInPath
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: appended to the guard of the `on Process` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified in witness-g18/pop-discharge-c-guard.precept.

*rule-premise* — other

- Addition: rule HistoryStack.count > 0 because "seeded stack never runs dry"
- Premise classes: (d)
- Derivation: premise (d): rule holds in pre-state; no earlier plan action writes F -> frame-preserved
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Not independently re-run for Pop; the identical NumericProofRequirement catalog shape (Actions.cs) is expected to behave as g18/dequeue-nonempty's live result (rejects, unresolved-today) — recorded as model-derived-from-representative, not independently confirmed.)

Required outcome: accept, with the premise list recorded

Provenance: model-derived.

- Model-derived from g18/dequeue-nonempty's live-verified representative; not independently run for Pop.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| guard-nf | when HistoryStack.count >= 0 | vacuously true for a non-negative count — must still reject, same obligation | reject, naming the same obligation |
| rule-premise | rule HistoryStack.count >= 0 because "..." | vacuous rule — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Spot-verified representative of g18/dequeue-nonempty's shared story; the mincount finding recorded there applies unchanged here (identical catalog shape).

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix
- src/Precept/Language/Actions.cs:132 — code: ActionKind.Pop — NumericProofRequirement(SelfSubject(count), GreaterThan, 0, "Stack must be non-empty"); full declaration spans lines 132-145
- docs/language/collection-types.md § `stack` — spec
- docs/language/collection-types.md § Guard pattern — spec

## g18/dequeueby-nonempty — DequeueBy — keyed queue must be non-empty (operand-free action precondition)

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | action/DequeueBy/numeric-0 |
| evaluation site category | operand-free-action-precondition |
| type family | collection |

### What must be proven

Obligation: F.count > 0

Weakest precondition: F.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| F | the keyed queue (queue of T by P) field the dequeue action mutates |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix

### Which premise classes can discharge it

Applicable classes: (c), (d).

Identical shape and reasoning to g18/dequeue-nonempty — DequeueBy is the queue-of-T-by-P analogue of Dequeue and shares the same NumericProofRequirement catalog shape (Actions.cs).

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: guard conjunct normal-form-equal to the WP (F.count > 0) -> guard-match
- Strategy: GuardInPath
- Capability tier: 1a
- Validity arguments: Guard normal-form match
- Decision procedure: As g18/dequeue-nonempty.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Guard normal-form match
- docs/language/collection-types.md § `queue of T by P` — spec: "Proof engine implications: Emptiness obligations identical to queue"

**Entry 2 — (d)**

- Derivation: a rule stating F.count > 0 holds in the pre-state, and no earlier action in the plan writes F before this dequeue, so the fact is frame-preserved
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: As g18/dequeue-nonempty.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Inductive hypothesis plus sign monotonicity
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — owner-ruling, 2026-07-20

### What the failing diagnostic must suggest

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix

- For class (d): restate <WP> as a rule holding in the pre-state, established at construction and preserved by every other write site of the collection
  - docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — owner-ruling, 2026-07-20

  - Not independently re-run; see g18/dequeue-nonempty's live-verified representative.


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept CollectionActionG18DequeueBy

field TriageQueue as queue of string by integer
field LastTriaged as string optional maxlength 200

state Active initial

event Begin initial
event Process

on Begin
    -> enqueue TriageQueue "seed" by 1
    -> set LastTriaged = "none"

from Active on Process
    -> dequeue TriageQueue into LastTriaged
    -> no transition
```

Required outcome: reject, naming the missing premise classes (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*guard-nf* — guard

- Addition: when TriageQueue.count > 0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the WP -> guard-match
- Strategy: GuardInPath
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: appended to the guard of the `on Process` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified in witness-g18/dequeueby-discharge-c-guard.precept.

*rule-premise* — other

- Addition: rule TriageQueue.count > 0 because "seeded queue never runs dry"
- Premise classes: (d)
- Derivation: premise (d): rule holds in pre-state; no earlier plan action writes F -> frame-preserved
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Model-derived from g18/dequeue-nonempty's live-verified representative (identical NumericProofRequirement shape); not independently run for DequeueBy.)

Required outcome: accept, with the premise list recorded

Provenance: model-derived.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| guard-nf | when TriageQueue.count >= 0 | vacuously true for a non-negative count — must still reject, same obligation | reject, naming the same obligation |
| rule-premise | rule TriageQueue.count >= 0 because "..." | vacuous rule — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Spot-verified representative of g18/dequeue-nonempty's shared story for the keyed-queue lane.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix
- src/Precept/Language/Actions.cs:253 — code: ActionKind.DequeueBy — NumericProofRequirement(SelfSubject(count), GreaterThan, 0, "Queue must be non-empty"); full declaration spans lines 253-266
- docs/language/collection-types.md § `queue of T by P` — spec

## g18/insert-indexbounds-transition — Insert at — index bounds [0, count], transition row

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | action/Insert/indexbounds-0 |
| evaluation site category | transition-row-action-operand |
| type family | collection |

### What must be proven

Obligation: 0 <= Index and Index <= F.count

Weakest precondition: 0 <= Index and Index <= F.count

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Index | the authored index expression the insert action's `at` clause supplies |
| F | the list field the insert action mutates |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix: the Normalization bullet

### Which premise classes can discharge it

Applicable classes: (b), (c).

The WP is a conjunction of two relational facts about two different quantities: a lower bound on Index (an event argument) and an upper bound comparing Index against F's own count. Class (b) can supply the lower-bound conjunct alone, via a `nonnegative` arg modifier — a direct instance of the already-written Arg-bound interval arithmetic argument. Class (b) cannot supply the upper-bound conjunct: F.count is a runtime-computed collection property, not a declarable literal bound, so no arg modifier can state a relationship to it. Class (c) can supply either or both conjuncts as guard facts. Class (d) does NOT apply: a rule mentions only fields (`docs/Working/obligation-discharge-matrix-2026-07-19.md § Constraint kinds` — attribution is fields and definition scope), and Index is an event argument, never a rule subject; live-tested this session (a rule stating a bound on F.count, with no guard at all, leaves the obligation rejected) confirms this is not merely unbuilt but structurally outside what a rule can state about this WP's Index term.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: both WP conjuncts matched, each normal-form-equal, against distinct conjuncts of the row's guard -> guard-match
- Strategy: GuardInPath
- Capability tier: 1a
- Validity arguments: Guard normal-form match
- Decision procedure: Family 1's own decision-procedure text speaks of matching the WP against "the row's guard conjuncts (a finite set)" (plural); applied here to a two-conjunct WP, each conjunct is independently matched against the guard's own conjunct set, or the guard states a single relational expression normal-form-equal to the whole conjunction. A stricter guard (e.g. `Index < F.count` in place of `Index <= F.count`) also discharges, since it entails the WP's conjunct (live-verified). Any guard failing to establish both conjuncts — lower bound only, or an off-by-one upper bound — rejects (live-verified, same obligation).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses — matrix: Family 1's own decision procedure for class (c): "normal-form match of the WP against the row's guard conjuncts (a finite set)"
- samples/shopping-cart.precept:187 — code: a real corpus row discharging insert-at and remove-at index-bounds obligations via three ANDed guard conjuncts in the same when clause, spanning lines 187-196

**Entry 2 — (b), (c)**

- Derivation: the index arg declared `nonnegative` discharges the lower-bound conjunct (type-derived, no guard conjunct needed for it); a guard conjunct normal-form-equal to the upper-bound conjunct (Index <= F.count) discharges the rest
- Strategy: GuardInPath
- Validity arguments: Arg-bound interval arithmetic; Guard normal-form match
- Decision procedure: The built strategy (TryIndexBoundsProof) always requires a guard for the upper bound — there is no type-derived counterpart to `nonnegative` for a dynamic collection size — but the lower bound may come from either the guard or the arg's declared `nonnegative` modifier; a guard stating the upper-bound conjunct alone, with the arg declared nonnegative and no lower-bound guard conjunct at all, still discharges (live-verified). Weakening the upper-bound conjunct (off-by-one) still rejects, same obligation (live-verified).

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Arg-bound interval arithmetic — an arg's declared bound instantiated directly as a WP conjunct, the same shape as Family 1 Base A
- src/Precept/Language/Modifiers.cs:69 — code: ModifierKind.Nonnegative ProofSatisfaction.Numeric(SelfValue, GreaterThanOrEqual, Constant(0))
- src/Precept/Pipeline/ProofEngine.Strategies.cs:436 — code: type-derived lower-bound check (IsTypeDerivedNonnegative) and the comment explaining why the upper bound always needs an explicit guard, lines 436-445

### What the failing diagnostic must suggest

- For class (b): declare the index argument `nonnegative` to discharge the lower-bound half of <WP>; the upper-bound half still needs a guard (class c)
  - src/Precept/Language/Modifiers.cs:69 — code

  - Class (b) alone never fully discharges this obligation — it is a partial, always-combine-with-(c) contributor, unlike a single-conjunct WP where an arg bound can close the whole obligation on its own.

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept CollectionActionG18InsertTransition

field ItemList as list of string maxlength 200

state Active initial

event Begin initial
event InsertItem(NewItem as string maxlength 200, Position as integer)

on Begin
    -> append ItemList "seed"

from Active on InsertItem
    -> insert ItemList InsertItem.NewItem at InsertItem.Position
    -> no transition
```

Required outcome: reject, naming the missing premise classes (b), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*guard-both* — guard

- Addition: when InsertItem.Position >= 0 and InsertItem.Position <= ItemList.count
- Premise classes: (c)
- Derivation: both WP conjuncts matched against distinct guard conjuncts -> guard-match
- Strategy: GuardInPath
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: appended to the guard of the `on InsertItem` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified in witness-g18/insert-transition-discharge-c-guard.precept.
- A strictly stronger guard (`Position < ItemList.count`, StrictlyBefore shape instead of AtOrBefore) also discharges cleanly — live-verified in witness-g18/insert-transition-strict-lt.precept — since it entails the AtOrBefore WP; recorded to show the discharge is not brittle to the exact comparison operator, only to the direction of entailment.

*nonneg-arg-plus-upper-guard* — arg-modifier

- Addition: Position as integer nonnegative
- Premise classes: (b), (c)
- Derivation: nonnegative arg -> type-derived lower bound; guard `Position <= ItemList.count` (upper-bound conjunct only) -> guard-match for the rest
- Strategy: GuardInPath
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: applied by replacing the named event-arg declarations
  - `InsertItem.Position` becomes `Position as integer nonnegative`

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified in witness-g18/insert-transition-bc-nonneg-plus-upper.precept. The guard here carries only the upper-bound conjunct (no `Position >= 0` at all) — confirms the lower bound is genuinely type-derived from the modifier, not silently required in the guard too.
- The nonnegative arg alone, with NO guard at all, still rejects (live-verified, witness-g18/insert-transition-b-nonneg-only.precept) — confirms class (b) never independently discharges the whole obligation, matching the code comment that the upper bound always needs an explicit guard (ProofEngine.Strategies.cs:441-445).

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| guard-both | when InsertItem.Position >= 0 | establishes the lower bound only; the upper-bound conjunct is absent, so the collection could still be shorter than Position — must still reject, same obligation | reject, naming the same obligation |
| nonneg-arg-plus-upper-guard | when InsertItem.Position <= ItemList.count + 1 | off-by-one upper bound — admits Position == count + 1, one past the legal insertion range — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: cell-override
- A representative safe-but-rejected spelling: when InsertItem.Position >= 0 and InsertItem.Position <= ItemList.count - 0
- Its licensed respelling: when InsertItem.Position >= 0 and InsertItem.Position <= ItemList.count

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses — matrix

- Not independently corpus-measured for this specific two-conjunct shape; recorded as a per-cell override because the family verdict was stated over the single-conjunct operand-free obligations and this cell's band is a compound-conjunct case the family text does not explicitly speak to.

**What the sources leave unstated or ambiguous here**

- Diagnostic-code discrepancy between doc and code: `docs/language/collection-types.md:1030-1032` states the unguarded diagnostic is `UnguardedCollectionAccess` for insert-at/remove-at; HEAD actually raises `IndexBoundsGuard` (PRE0100) uniformly for every IndexBoundsProofRequirement, confirmed live this session and matching this group's authoring notes ("measured: IndexBoundsGuard for insert-at"). `src/Precept/Language/ProofRequirement.cs:367-370` pins IndexBoundsGuard as the catalog-declared code. This is a doc/code drift the matrix's doc-sync obligations should pick up when this family is promoted; not fixed here (outside this task's scope — the cells file only records the measured behaviour honestly).

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix
- src/Precept/Language/Actions.cs:183 — code: ActionKind.Insert — IndexBoundsProofRequirement(ParamSubject(index), AtOrBefore, CollectionCountAccessor, "Insert index must be within bounds [0, count]"); full declaration spans lines 183-202
- docs/language/collection-types.md § `list of T` — spec: "insert at N ... Precondition: N >= 0 and N <= F.count. Author writes guard; proof engine raises UnguardedCollectionAccess if absent" (line 1030) — the diagnostic actually raised at HEAD is IndexBoundsGuard, not UnguardedCollectionAccess; see notes

## g18/insert-indexbounds-construction — Insert at — index bounds [0, count], construction row

Disposition: **open**.

**Disposition sources**

- src/Precept/Language/DiagnosticCode.cs:207 — code: ConstructionGuardReadsUninitializedField — a construction-row guard cannot read a field at all, live-verified this session; not yet stated in the canonical language spec
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: "Establishment over defaults" and "Literal constant-fold for defaults" are both scoped to fields the firing construction row does NOT write — neither covers a field the plan itself grows and then indexes into

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | action/Insert/indexbounds-0 |
| evaluation site category | construction-row-action-operand |
| type family | collection |

### What must be proven

Obligation: 0 <= Index and Index <= F.count

Weakest precondition: 0 <= Index and Index <= F.count

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Index | the authored index expression the insert action's `at` clause supplies |
| F | the list field the insert action mutates |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix

### The worked examples

**Base — the program the compiler must reject.**

```precept
precept CollectionActionG18InsertConstructionSeq

field ItemList as list of string maxlength 200

state Active initial

event Begin(NewItem as string maxlength 200) initial

on Begin
    -> append ItemList "seed"
    -> insert ItemList Begin.NewItem at 0
```

Required outcome: reject, naming the missing premise classes (b), (c)

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**What the sources leave unstated or ambiguous here**

- This cell is open, not defined — the disposition-pass input (`disposition-map.json`) marked this coordinate `defined`, but authoring it against the matrix's actual written text surfaced that no discharge route is derivable, so it is corrected to `open` here per this task's ground rule 1 (never invent an answer) rather than force a contract that does not exist in the generative sections.
- Why class (c) is unavailable here and nowhere else in this group: a construction-row guard cannot read ANY field — `ConstructionGuardReadsUninitializedField` fires the instant a construction guard mentions `ItemList` at all, live-verified this session (`witness-g18/insert-construction-guard-test2.precept`). This is a hard, structural exclusion specific to the construction-row-action-operand category; transition rows and state hooks read a real pre-existing entity and do not carry it (both live-verified elsewhere in this group).
- Why class (b)/(a) do not cover the gap either: the upper-bound conjunct (`Index <= F.count`) needs a fact about F's count, which no arg modifier can state (count is not a declarable literal bound). Tested whether a literal, statically-known count could substitute — a field with a literal `default [...]` of exactly matching length, combined with an arg `max` bound equal to that length, and no guard at all — and HEAD still rejects (`witness-g18/insert-construction-default-literal.precept`, live-verified). Also tested whether the write plan's own prior actions (an `append` before the `insert`, using a literal index provably in range) discharge via sequential tracking with no guard at all — HEAD still rejects (`witness-g18/insert-construction-seq-track.precept` and `.../insert-construction-seq-empty.precept`, both live-verified). No route tried succeeds.
- Whether the model (as opposed to the built engine) licenses a discharge here at all is the actual open question, not merely an unbuilt feature. The matrix's write-plan vocabulary states generally that "an expression later in a plan does see the results of earlier writes ... and its own fault obligations are minted per evaluation site," which is suggestive that a literal-fold-through-the-plan route SHOULD be derivable in principle — but the two named defaults arguments ("Establishment over defaults," "Literal constant-fold for defaults") are both explicitly scoped to a field the row does NOT write, which is the opposite of this case (the field is grown then indexed within the same plan). No generative rule in the matrix currently covers a field that IS written earlier in the plan and then read for a fault precondition later in the same plan. This is the missing rule to report, not to invent an answer for.
- This is the sharpest form of the two-sided index-bounds gap this group's authoring notes flagged ("behaves like group 17... if the two-sided derivation turns out not to be derivable, that is a missing rule to report"): at construction specifically, even the SINGLE-conjunct pieces (which discharge fine individually at transition-row/state-hook) have no route, because the one mechanism that supplies the upper bound anywhere else in this group (a guard) is structurally excluded here.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix: the obligation is genuinely minted; only its discharge is undecided
- src/Precept/Language/Actions.cs:183 — code

## g18/insert-indexbounds-statehook — Insert at — index bounds [0, count], state hook

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | action/Insert/indexbounds-0 |
| evaluation site category | state-hook-action-operand |
| type family | collection |

### What must be proven

Obligation: 0 <= Index and Index <= F.count

Weakest precondition: 0 <= Index and Index <= F.count

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Index | the field read by the state-hook insert action's `at` clause (the hook has no event args of its own — the value must arrive via a field the entering transition row set) |
| F | the list field the insert action mutates |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix

### Which premise classes can discharge it

Applicable classes: (b), (c).

Identical reasoning to g18/insert-indexbounds-transition. A state entry hook operates on the already-constructed entity (the pre-transition configuration is real), so unlike construction, a hook guard CAN read F.count — live-verified this session (`to StateName when Field ... -> insert ...` compiles and discharges). Class (d) does not apply, same reasoning as the transition-row cell (a rule cannot mention an event/field-sourced index the way this WP's Index metavariable requires; here Index is itself a field, so in principle a rule COULD mention it, but doing so does not supply a fact relating it to F.count — no test in this group found a rule-only route discharging any index-bounds obligation, and none is written in the matrix).

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: both WP conjuncts matched, each normal-form-equal, against distinct conjuncts of the hook's guard -> guard-match
- Strategy: GuardInPath
- Capability tier: 1a
- Validity arguments: Guard normal-form match
- Decision procedure: As g18/insert-indexbounds-transition, applied to the state hook's guard position instead of the transition row's.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses — matrix
- docs/language/precept-language-spec.md:950 — spec: `to` — state action (entry hook); a guard-bearing entry hook (`to S when ...`) is real corpus syntax, `samples/library-hold-request.precept:51`

**Entry 2 — (b), (c)**

- Derivation: the index field's `nonnegative` modifier discharges the lower-bound conjunct (type-derived); a hook guard conjunct normal-form-equal to the upper-bound conjunct discharges the rest
- Strategy: GuardInPath
- Validity arguments: Arg-bound interval arithmetic; Guard normal-form match
- Decision procedure: As g18/insert-indexbounds-transition — not independently re-run at the state-hook site; the guard-alone route above was live-verified here, and the mixed route is expected to generalize on the identical IndexBoundsProofRequirement/TryIndexBoundsProof shape (both site-agnostic per ProofEngine.Strategies.cs).

- src/Precept/Pipeline/ProofEngine.Strategies.cs:428 — code: the guard resolution switches on ObligationContext (TransitionRowContext / StateHookContext / EventHandlerContext) uniformly, lines 428-434 — the strategy does not distinguish transition rows from state hooks

### What the failing diagnostic must suggest

- For class (b): declare the index field `nonnegative` to discharge the lower-bound half of <WP>; the upper-bound half still needs a guard (class c)
  - src/Precept/Language/Modifiers.cs:69 — code

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept CollectionActionG18InsertStateHook

field ItemList as list of string maxlength 200
field Position as integer default 0
field NewItem as string default "x" maxlength 200

state Active initial
state Archived terminal

event Begin initial
event Archive(NewItem as string maxlength 200, Position as integer)

on Begin
    -> append ItemList "seed"

from Active on Archive
    -> set NewItem = Archive.NewItem
    -> set Position = Archive.Position
    -> transition Archived

to Archived
    -> insert ItemList NewItem at Position
```

Required outcome: reject, naming the missing premise classes (b), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

**Discharge additions — what makes the base compile.**

*guard-both* — guard

- Addition: when Position >= 0 and Position <= ItemList.count
- Premise classes: (c)
- Derivation: both WP conjuncts matched against distinct hook-guard conjuncts -> guard-match
- Strategy: GuardInPath
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified in witness-g18/insert-statehook-discharge-c-guard.precept.
- No `application` recorded: the addition attaches to a state entry hook's `when` clause, which fits neither the schema's `row-guard` locus (named for a transition row's event) nor `event-arg-declarations` — a schema-vocabulary gap for hook-guard-locus additions, not invented around here.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| guard-both | when Position >= 0 | lower bound only — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Only the guard-alone route was independently live-verified at this site; the mixed (b)+(c) route is recorded model-derived-from-representative (identical catalog/strategy shape, confirmed site-agnostic in code), consistent with this group's economy of witnessing one full battery per sub-story and spot-checking the rest.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix
- src/Precept/Language/Actions.cs:183 — code
- docs/language/precept-language-spec.md:950 — spec

## g18/removeat-nonempty-transition — Remove at — list must be non-empty, transition row

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | action/RemoveAt/numeric-0 |
| evaluation site category | transition-row-action-operand |
| type family | collection |

### What must be proven

Obligation: F.count > 0

Weakest precondition: F.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| F | the list field the remove-at action mutates |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix

### Which premise classes can discharge it

Applicable classes: (c), (d).

Identical shape/reasoning to g18/dequeue-nonempty. Distinctive to `remove at`: this action mints TWO ProofRequirements at once (this cell's non-empty check and the index-bounds check of g18/removeat-indexbounds-transition), so base minimality requires the base program used FOR THIS CELL to already carry a discharge for the sibling obligation (an index-bounds guard), and vice versa — recorded once here, applies symmetrically to that cell too (mirrors group 13's `.at()` note about a base rejecting only for the target obligation).

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: guard conjunct normal-form-equal to the WP (F.count > 0) -> guard-match
- Strategy: GuardInPath
- Capability tier: 1a
- Validity arguments: Guard normal-form match
- Decision procedure: As g18/dequeue-nonempty.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Guard normal-form match
- docs/language/collection-types.md § Guard pattern — spec

**Entry 2 — (d)**

- Derivation: a rule stating F.count > 0 holds in the pre-state, and no earlier action in the plan writes F before this remove, so the fact is frame-preserved
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: As g18/dequeue-nonempty.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Inductive hypothesis plus sign monotonicity
- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — owner-ruling, 2026-07-20

### What the failing diagnostic must suggest

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix

- For class (d): restate <WP> as a rule holding in the pre-state, established at construction and preserved by every other write site of the collection
  - docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — owner-ruling, 2026-07-20

  - Live-verified this session: with the rule declared and the sibling index-bounds obligation already guarded, the remove still raises UnguardedCollectionMutation — HEAD does not consume the rule.


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept CollectionActionG18RemoveAtTransition

field ItemList as list of string maxlength 200

state Active initial

event Begin initial
event RemoveItem(Position as integer)

on Begin
    -> append ItemList "seed"

from Active on RemoveItem when RemoveItem.Position >= 0 and RemoveItem.Position < ItemList.count
    -> remove ItemList at RemoveItem.Position
    -> no transition
```

Required outcome: reject, naming the missing premise classes (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- The base already carries the sibling index-bounds discharge (`Position >= 0 and Position < ItemList.count`) so that only this cell's target obligation (UnguardedCollectionMutation) is raised — base minimality per the sibling-obligation note above.

**Discharge additions — what makes the base compile.**

*guard-nf* — guard

- Addition: and ItemList.count > 0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the WP -> guard-match
- Strategy: GuardInPath
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: appended to the guard of the `on RemoveItem` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified in witness-g18/removeat-transition-discharge-both.precept (full guard: `ItemList.count > 0 and Position >= 0 and Position < ItemList.count`).
- No `application` recorded as a clean single-clause insertion: the addition is ANDed into an existing guard (the base already carries the sibling index-bounds conjuncts) rather than the whole `when` clause of a bare row — a variant of the `row-guard` locus this schema does not distinguish from a fresh guard; recorded as a conjunct addition, not invented around.

*rule-premise* — other

- Addition: rule ItemList.count > 0 because "hazard probe"
- Premise classes: (d)
- Derivation: premise (d): rule holds in pre-state; no earlier plan action writes F before this remove -> frame-preserved
- Provable under the definition: provable-under-model
- What the engine does today: unresolved-today (Live-verified: with the rule declared and the sibling index-bounds obligation guarded, the remove still raises UnguardedCollectionMutation.)

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified in witness-g18/removeat-transition-d-rule.precept.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| guard-nf | and ItemList.count >= 0 | vacuously true — must still reject, same obligation | reject, naming the same obligation |
| rule-premise | rule ItemList.count >= 0 because "..." | vacuous rule — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix
- src/Precept/Language/Actions.cs:204 — code: ActionKind.RemoveAt — NumericProofRequirement(SelfSubject(count), GreaterThan, 0, "List must be non-empty"); full declaration spans lines 204-229
- docs/language/collection-types.md § `list of T` — spec: `remove at N` — "Precondition: N >= 0 and N < F.count" (line 1032); the non-empty half is the same shape as the operand-free cells

## g18/removeat-nonempty-construction — Remove at — list must be non-empty, construction row

Disposition: **open**.

**Disposition sources**

- src/Precept/Pipeline/ProofLedger.cs:100 — code: ProofStrategy.CollectionGrowth = 11 is the only route that live-discharges this obligation at construction, and no matrix validity argument names it
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Rule validity — matrix: "no cell ratifies whose derivation cites an argument-less rule" — the ratification gate this open disposition avoids violating

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | action/RemoveAt/numeric-0 |
| evaluation site category | construction-row-action-operand |
| type family | collection |

### What must be proven

Obligation: F.count > 0

Weakest precondition: F.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| F | the list field the remove-at action mutates |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix

### The worked examples

**Base — the program the compiler must reject.**

```precept
precept CollectionActionG18RemoveAtConstructionLiteralDefault

field ItemList as list of string maxlength 200 default ["seed"]

state Active initial

event Begin initial

on Begin
    -> remove ItemList at 0
```

Required outcome: reject, naming the missing premise classes (c)

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- This base also raises IndexBoundsGuard simultaneously (the sibling obligation, g18/removeat-indexbounds-construction, is also open and equally undischargeable at construction) — `noOtherDiagnostics` is honestly recorded false rather than force-isolating a clean single-obligation base that does not actually exist at this site under any tested configuration.

**What the sources leave unstated or ambiguous here**

- This cell is open, not defined — the disposition-pass input marked this coordinate `defined`, but authoring it surfaced a real gap between what HEAD does and what the matrix has written, so it is corrected here per ground rule 1.
- Live-verified this session (`witness-g18/removeat-construction-base.precept`): a construction row that `append`s then `remove`s the SAME field in one plan, with NO guard at all and a literal index, compiles with the non-empty obligation silently discharged (only IndexBoundsGuard remains) — this is `ProofStrategy.CollectionGrowth` (`src/Precept/Pipeline/ProofLedger.cs:114`: "a count > 0 obligation discharged by a prior grow action in the same chain"), a real, named, built strategy.
- But this strategy requires an explicit prior GROW ACTION in the same write plan — it is not a general literal-default fold. Tested and confirmed live: a field with a non-empty literal `default ["seed"]` and NO prior append, removed at literal index 0 with no guard, still rejects both obligations (`witness-g18/removeat-construction-literal-default.precept`). So the discharge is narrower than "the field is provably non-empty at this point" in general — it specifically tracks grow actions, not declared defaults.
- Why this is open rather than defined: none of the matrix's seven named validity arguments covers CollectionGrowth. The two candidates that discuss construction-row defaults ("Establishment over defaults," "Literal constant-fold for defaults") are both explicitly scoped to a field the firing construction row does NOT write — the opposite of this case, where the row DOES write the field (via append) before reading it (via remove). The general Vocabulary sentence that "an expression later in a plan does see the results of earlier writes ... and its own fault obligations are minted per evaluation site" states WHAT is minted and WHERE, not that forward-propagation from a grow action is a licensed discharge, and gives no decision procedure for it. Citing this discharge as `defined` would require inventing that connection, which ground rule 1 forbids. The missing rule to report: a validity argument (and, ideally, a stated decision procedure) for the CollectionGrowth strategy — is forward-propagation from a same-plan grow action licensed as a discharge mechanism at all, and if so under which premise class (it consumes no authored premise — it is closer in shape to a standing/constant-fold discharge than to any of classes (a)-(d))?
- Distinct from g18/insert-indexbounds-construction and g18/removeat-indexbounds-construction: those two are open because NO route discharges them at all under any test. This cell IS discharged live by a real strategy — the gap here is purely that the matrix has not written a validity argument for that strategy, which the ratification gate requires before any cell may cite it.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix
- src/Precept/Language/Actions.cs:204 — code

## g18/removeat-nonempty-statehook — Remove at — list must be non-empty, state hook

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | action/RemoveAt/numeric-0 |
| evaluation site category | state-hook-action-operand |
| type family | collection |

### What must be proven

Obligation: F.count > 0

Weakest precondition: F.count > 0

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| F | the list field the remove-at action mutates |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix

### Which premise classes can discharge it

Applicable classes: (c), (d).

Identical to g18/removeat-nonempty-transition — a state hook operates on a real pre-existing entity, so guard availability and the (d) reasoning transfer unchanged.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: guard conjunct normal-form-equal to the WP (F.count > 0) -> guard-match
- Strategy: GuardInPath
- Capability tier: 1a
- Validity arguments: Guard normal-form match
- Decision procedure: As g18/dequeue-nonempty, applied to the hook's guard position.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: Guard normal-form match

**Entry 2 — (d)**

- Derivation: a rule stating F.count > 0 holds in the pre-state, frame-preserved as no earlier plan action writes F before this remove
- Validity arguments: Inductive hypothesis plus sign monotonicity
- Decision procedure: As g18/dequeue-nonempty.

- docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — owner-ruling, 2026-07-20

### What the failing diagnostic must suggest

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix

- For class (d): restate <WP> as a rule holding in the pre-state, established at construction and preserved by every other write site of the collection
  - docs/Working/obligation-discharge-matrix-2026-07-19-cells/authored-expressiveness-gaps.md § Defect B — owner-ruling, 2026-07-20


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept CollectionActionG18RemoveAtStateHook

field ItemList as list of string maxlength 200
field Position as integer default 0

state Active initial
state Archived terminal

event Begin initial
event Archive(Position as integer)

on Begin
    -> append ItemList "seed"

from Active on Archive
    -> set Position = Archive.Position
    -> transition Archived

to Archived when Position >= 0 and Position < ItemList.count
    -> remove ItemList at Position
```

Required outcome: reject, naming the missing premise classes (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Base already carries the sibling index-bounds discharge for isolation, mirroring the transition-row cell.

**Discharge additions — what makes the base compile.**

*guard-nf* — guard

- Addition: and ItemList.count > 0
- Premise classes: (c)
- Derivation: guard fact normal-form-equal to the WP -> guard-match
- Strategy: GuardInPath
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified in witness-g18/removeat-statehook-discharge-both.precept.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| guard-nf | and ItemList.count >= 0 | vacuously true — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- The (d) rule-premise route was not independently re-run at the state-hook site; it is model-derived from the transition-row and operand-free representatives (identical NumericProofRequirement shape, uniformly unresolved-today per every live test in this group).

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix
- src/Precept/Language/Actions.cs:204 — code

## g18/removeat-indexbounds-transition — Remove at — index bounds [0, count), transition row

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | action/RemoveAt/indexbounds-1 |
| evaluation site category | transition-row-action-operand |
| type family | collection |

### What must be proven

Obligation: 0 <= Index and Index < F.count

Weakest precondition: 0 <= Index and Index < F.count

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Index | the authored index expression the remove-at action's `at` clause supplies |
| F | the list field the remove-at action mutates |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix

### Which premise classes can discharge it

Applicable classes: (b), (c).

Same reasoning as g18/insert-indexbounds-transition, with StrictlyBefore in place of AtOrBefore (`< F.count` rather than `<= F.count`) — the only difference in the requirement's declared IndexBoundsMode.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: both WP conjuncts matched, each normal-form-equal, against distinct conjuncts of the row's guard -> guard-match
- Strategy: GuardInPath
- Capability tier: 1a
- Validity arguments: Guard normal-form match
- Decision procedure: As g18/insert-indexbounds-transition, with the strict comparison.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses — matrix
- samples/shopping-cart.precept:195 — code: `remove LineItems at ReorderItem.FromIndex` discharged by the same multi-conjunct guard as the sibling insert

**Entry 2 — (b), (c)**

- Derivation: the index arg declared `nonnegative` discharges the lower-bound conjunct; a guard conjunct normal-form-equal to the upper-bound conjunct (Index < F.count) discharges the rest
- Strategy: GuardInPath
- Validity arguments: Arg-bound interval arithmetic; Guard normal-form match
- Decision procedure: As g18/insert-indexbounds-transition.

- src/Precept/Language/Modifiers.cs:69 — code
- src/Precept/Pipeline/ProofEngine.Strategies.cs:436 — code

### What the failing diagnostic must suggest

- For class (b): declare the index argument `nonnegative` to discharge the lower-bound half of <WP>; the upper-bound half still needs a guard (class c)
  - src/Precept/Language/Modifiers.cs:69 — code

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept CollectionActionG18RemoveAtTransition

field ItemList as list of string maxlength 200

state Active initial

event Begin initial
event RemoveItem(Position as integer)

on Begin
    -> append ItemList "seed"

from Active on RemoveItem when ItemList.count > 0
    -> remove ItemList at RemoveItem.Position
    -> no transition
```

Required outcome: reject, naming the missing premise classes (b), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- The base already carries the sibling non-empty discharge (`ItemList.count > 0`) so only this cell's target obligation (IndexBoundsGuard) is raised.

**Discharge additions — what makes the base compile.**

*guard-both* — guard

- Addition: and RemoveItem.Position >= 0 and RemoveItem.Position < ItemList.count
- Premise classes: (c)
- Derivation: both WP conjuncts matched against distinct guard conjuncts -> guard-match
- Strategy: GuardInPath
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: appended to the guard of the `on RemoveItem` row

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified in witness-g18/removeat-transition-discharge-both.precept (same file as the sibling cell's discharge — the full guard closes both obligations at once).

*nonneg-arg-plus-upper-guard* — arg-modifier

- Addition: Position as integer nonnegative
- Premise classes: (b), (c)
- Derivation: nonnegative arg -> type-derived lower bound; guard `Position < ItemList.count` (upper-bound conjunct only, plus the sibling's `ItemList.count > 0`) -> guard-match for the rest
- Strategy: GuardInPath
- Provable under the definition: provable-under-model
- What the engine does today: proven-today
- How it applies to the base: applied by replacing the named event-arg declarations
  - `RemoveItem.Position` becomes `Position as integer nonnegative`

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified in witness-g18/removeat-transition-bc-nonneg-upper.precept.
- The nonnegative arg alone, with no upper-bound guard conjunct at all, still rejects — live-verified in witness-g18/removeat-transition-b-nonneg-alone.precept — confirms class (b) never independently discharges the whole obligation.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| guard-both | and RemoveItem.Position >= 0 | lower bound only — must still reject, same obligation | reject, naming the same obligation |
| nonneg-arg-plus-upper-guard | and RemoveItem.Position <= ItemList.count | off-by-one upper bound (non-strict where the requirement is strict) — admits Position == count, one past the last valid element — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: cell-override
- A representative safe-but-rejected spelling: when RemoveItem.Position <= ItemList.count - 1
- Its licensed respelling: when RemoveItem.Position >= 0 and RemoveItem.Position < ItemList.count

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses — matrix

- Not independently corpus-measured for this two-conjunct shape; per-cell override for the same reason as g18/insert-indexbounds-transition.

**What the sources leave unstated or ambiguous here**

- Same diagnostic-code discrepancy noted at g18/insert-indexbounds-transition applies here (doc says UnguardedCollectionAccess; HEAD raises IndexBoundsGuard).

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix
- src/Precept/Language/Actions.cs:204 — code: ActionKind.RemoveAt — IndexBoundsProofRequirement(ParamSubject(index), StrictlyBefore, CollectionCountAccessor, "Remove index must be within bounds [0, count)")
- docs/language/collection-types.md § `list of T` — spec: `remove at N` — "Precondition: N >= 0 and N < F.count" (line 1032)

## g18/removeat-indexbounds-construction — Remove at — index bounds [0, count), construction row

Disposition: **open**.

**Disposition sources**

- src/Precept/Language/DiagnosticCode.cs:207 — code: ConstructionGuardReadsUninitializedField — same structural exclusion as g18/insert-indexbounds-construction
- docs/Working/obligation-discharge-matrix-2026-07-19.md § Validity arguments — matrix: the two defaults-scoped arguments do not cover a field the plan itself writes before indexing into it

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | action/RemoveAt/indexbounds-1 |
| evaluation site category | construction-row-action-operand |
| type family | collection |

### What must be proven

Obligation: 0 <= Index and Index < F.count

Weakest precondition: 0 <= Index and Index < F.count

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Index | the authored index expression the remove-at action's `at` clause supplies |
| F | the list field the remove-at action mutates |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix

### The worked examples

**Base — the program the compiler must reject.**

```precept
precept CollectionActionG18RemoveAtConstruction

field ItemList as list of string maxlength 200

state Active initial

event Begin(NewItem as string maxlength 200) initial

on Begin
    -> append ItemList Begin.NewItem
    -> remove ItemList at 0
```

Required outcome: reject, naming the missing premise classes (b), (c)

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Isolated from the sibling non-empty obligation: the same-plan append already discharges non-emptiness via CollectionGrowth (see g18/removeat-nonempty-construction), so IndexBoundsGuard is the only ERROR raised here even with a literal index and no guard at all.
- `noOtherDiagnostics` is false, not true (corrected by adversarial re-run, 2026-07-21, same binary and commit): the base also raises `[Warning] StructuralSinkState: State 'Active' has no outgoing transitions and is not marked 'terminal'`, because a construction-only witness at this evaluation-site category has no transition row. The sibling g18/insert-indexbounds-construction base carries the identical warning and already records false; the two now agree.

**What the sources leave unstated or ambiguous here**

- This cell is open, not defined — same correction as g18/insert-indexbounds-construction, for the same reason: the disposition-pass input marked it `defined`, but no discharge route is derivable from the matrix's written text once tested.
- The asymmetry with the sibling non-empty obligation is itself the finding: CollectionGrowth (ProofStrategy, `src/Precept/Pipeline/ProofLedger.cs:114`) discharges a `count > 0` check from a same-plan prior grow action, but `TryIndexBoundsProof` (`src/Precept/Pipeline/ProofEngine.Strategies.cs:390-474`) has NO such fallback — it unconditionally requires a guard for the upper bound (lines 441-445), and no guard can be written at a construction row (`ConstructionGuardReadsUninitializedField`). So even though the same write plan that silently proves non-emptiness sits right next to the index-bounds check, the index-bounds obligation has no route at all, live-verified even with a literal index (0) that is trivially in range given the preceding append.
- Same missing rule as g18/insert-indexbounds-construction (both share the identical structural cause — the guard mechanism is the only one with an upper-bound story anywhere in this group, and it is unavailable at this evaluation-site category): whether any discharge mechanism is licensed for an index-bounds obligation at a site where the guard route is structurally excluded is undecided in the matrix as written.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix
- src/Precept/Language/Actions.cs:204 — code

## g18/removeat-indexbounds-statehook — Remove at — index bounds [0, count), state hook

Disposition: **defined**.

**Disposition sources**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix

### Where this cell sits on the axes

| Axis | Value |
|---|---|
| case shape | fault |
| obligation family | fault-prevention |
| operation kind | action/RemoveAt/indexbounds-1 |
| evaluation site category | state-hook-action-operand |
| type family | collection |

### What must be proven

Obligation: 0 <= Index and Index < F.count

Weakest precondition: 0 <= Index and Index < F.count

The placeholders, and what each stands for:

| Placeholder | Stands for |
|---|---|
| Index | the field read by the state-hook remove-at action's `at` clause |
| F | the list field the remove-at action mutates |

**Matching is done under these normalization rules**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix

### Which premise classes can discharge it

Applicable classes: (b), (c).

Identical reasoning to g18/insert-indexbounds-statehook, with StrictlyBefore in place of AtOrBefore.

### The discharge contract — exactly when this counts as proven

**Entry 1 — (c)**

- Derivation: both WP conjuncts matched, each normal-form-equal, against distinct conjuncts of the hook's guard -> guard-match
- Strategy: GuardInPath
- Capability tier: 1a
- Validity arguments: Guard normal-form match
- Decision procedure: As g18/insert-indexbounds-statehook.

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Sketch witnesses — matrix

**Entry 2 — (b), (c)**

- Derivation: the index field's `nonnegative` modifier discharges the lower-bound conjunct; a hook guard conjunct normal-form-equal to the upper-bound conjunct discharges the rest
- Strategy: GuardInPath
- Validity arguments: Arg-bound interval arithmetic; Guard normal-form match
- Decision procedure: As g18/insert-indexbounds-statehook — not independently re-run here; expected to generalize on the site-agnostic strategy code.

- src/Precept/Pipeline/ProofEngine.Strategies.cs:428 — code

### What the failing diagnostic must suggest

- For class (b): declare the index field `nonnegative` to discharge the lower-bound half of <WP>; the upper-bound half still needs a guard (class c)
  - src/Precept/Language/Modifiers.cs:69 — code

- For class (c): add a guard normal-form-equal to <WP>
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — matrix


### The worked examples

**Base — the program the compiler must reject.**

```precept
precept CollectionActionG18RemoveAtStateHook

field ItemList as list of string maxlength 200
field Position as integer default 0

state Active initial
state Archived terminal

event Begin initial
event Archive(Position as integer)

on Begin
    -> append ItemList "seed"

from Active on Archive
    -> set Position = Archive.Position
    -> transition Archived

to Archived when ItemList.count > 0
    -> remove ItemList at Position
```

Required outcome: reject, naming the missing premise classes (b), (c), with no other diagnostics

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Base already carries the sibling non-empty discharge for isolation.

**Discharge additions — what makes the base compile.**

*guard-both* — guard

- Addition: and Position >= 0 and Position < ItemList.count
- Premise classes: (c)
- Derivation: both WP conjuncts matched against distinct hook-guard conjuncts -> guard-match
- Strategy: GuardInPath
- Provable under the definition: provable-under-model
- What the engine does today: proven-today

Required outcome: accept, with the premise list recorded

Provenance: live-verified (Precept.MatrixTools, 2026-07-21, HEAD e1a14d91) — the run attests the built-status claim, not the required outcome.

- Live-verified in witness-g18/removeat-statehook-discharge-both.precept.

**Near-misses — each addition weakened past sufficiency; the compiler must still reject, naming the same obligation.**

| Weakens | Weakened addition | Why it still rejects | Required outcome |
|---|---|---|---|
| guard-both | and Position >= 0 | lower bound only — must still reject, same obligation | reject, naming the same obligation |

### Respellability

- Resolution: family-inherited

**What the sources leave unstated or ambiguous here**

- Only the guard-alone route was independently live-verified at this site; the mixed (b)+(c) route is model-derived-from-representative, consistent with this group's spot-check economy.

**What this cell derives from**

- docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — matrix
- src/Precept/Language/Actions.cs:204 — code

## Fields with no rendering rule

Data the generator has no rendering rule for, surfaced verbatim rather than dropped. Each is either a schema addition the generator has not caught up with, or a stray field.

| Where | Field | Value |
|---|---|---|
| cells[g18/insert-indexbounds-transition].respellability | overrideVerdict | "yes" |
| cells[g18/removeat-indexbounds-transition].respellability | overrideVerdict | "yes" |

