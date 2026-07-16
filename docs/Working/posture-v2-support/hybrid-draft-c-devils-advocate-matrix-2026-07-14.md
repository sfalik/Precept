---
title: "Devil's Advocate Matrix — Hybrid Model Draft C"
date: 2026-07-14
author: Fact Checker (Devil's Advocate mode)
status: Working — analysis only, no fixes proposed
target: docs/Working/posture-v2-support/hybrid-model-draft-C-fresh-structure-2026-07-14.md
scope: Sections 1-5, with canonical docs consulted only to test whether Draft C's taxonomy and mechanics partition the documented language cleanly
---

# Devil's Advocate Matrix — Hybrid Model Draft C

This pass reads Draft C **fresh** and treats its model as guilty until proven airtight. The question is not whether the document sounds persuasive on the easy paths; the question is whether its §2 provenance taxonomy and §3/§4/§5 enforcement model still partition the language cleanly once hostile cases are enumerated systematically.

## 1. Top-line findings

### 1.1 The strongest structural gaps

1. **Defaults are not actually partitioned by §2.1.** Draft C's lead sentence says provenance turns on whether "a Precept expression compute[d] this value, or the outside world supplied it," but §2.1's enumerated buckets list exactly three supplied entry points and exactly three computed forms. A field `default` value — literal or expression — is in neither list, even though §2.2 later says "a `default` and a `set` action that assign the same field are both internal to the definition."
2. **Construction-time truth is under-modeled.** Several language-level obligations over defaults are checked at construction / compile-time fold in canon (`precept-language-spec.md`: compile-time rule enforcement against defaults; stateless construction checks). Draft C's runtime story focuses on ingress + sweep during operations and never cleanly assigns ownership to the initial default-only configuration.
3. **Rules over internal/default-only values break the model hardest.** §5.1 says "A rule is always governed," but default-only immutable fields can participate in rules even when no external value ever enters and no operation ever mutates either side. Draft C does not clearly say whether such rules are compile-time-folded, runtime-rechecked forever, or both.
4. **One field can hold values of multiple provenances over time.** Draft C repeatedly says classify values, not constructs/fields, but §2.4 then presents field-level buckets (editable source fields vs computed fields) as if a field has one stable provenance. Fields with `default` + later direct edits, or `default` + later `set`, or editable + action writes, break that simplification.
5. **Event-argument defaults create a hidden fourth/bridge origin.** An omitted event arg filled from a declaration-side default is not straightforwardly "supplied by the outside world," yet it is not one of §2.1's computed forms either.

### 1.2 Severity summary

- **Structural gaps (model likely insufficient as written):** Cases 1-3, 6-8, 10-13, 16-20
- **Wording / scoping gaps (model may survive with explicit qualifications):** Cases 4-5, 9, 14-15
- **Cases that are mostly fine but still expose adjacent ambiguity:** Cases 5, 9, 15

## 2. Matrix of hostile cases

Format per case:
- **(a) Literal Draft C classification** — what §2.1/§2.2 literally says, or that it is silent/contradictory
- **(b) §3/§4/§5 mechanics** — what the later sections imply should happen
- **(c) Gap / inconsistency** — explicit problem statement
- **(d) Severity** — wording gap vs structural gap

---

### Case 1 — Default-only readonly field, literal default, single-field modifier

```precept
field X as number nonzero default 12
```

- **(a) Literal Draft C classification:** `default 12` matches neither of §2.1's three supplied origins nor its three computed forms. §2.2 later says a `default` is internal to the definition, which implies non-supplied / internal, but §2.1 never actually places it in a bucket.
- **(b) §3/§4/§5 mechanics:** If internal values are meant to fall on the compute side, the `nonzero` constraint should be settled before any instance exists. If supplied, governance has no ingress site because no outside value enters. §5 is irrelevant because no rule is involved.
- **(c) Gap / inconsistency:** The model does not literally classify the simplest possible defaulted field. This is the minimal proof that §2.1 is not an exhaustive partition.
- **(d) Severity:** **Structural gap.** The partition fails on the base case.

### Case 2 — Default-only readonly field, compile-time-constant expression default

```precept
field X as number nonzero default sqrt(12)
```

- **(a) Literal Draft C classification:** Draft C's opening slogan says a value computed by a Precept expression is computed, which suggests `sqrt(12)` is computed. But §2.1's computed list does not include default expressions, only `<-`, `set`, and derived collection state. So the lead sentence and the enumerated list disagree.
- **(b) §3/§4/§5 mechanics:** §4.1 says computed values must be proven within constraints and free of evaluation faults. If `default sqrt(12)` counts as computed, the `sqrt` domain and `nonzero` should be compile-time obligations. If it does not count as computed, nothing in §3 gives governance a place to enforce it.
- **(c) Gap / inconsistency:** This is Frank's already-confirmed hole in its strongest form: the lead sentence and the enumerated taxonomy disagree on whether a default expression is computed.
- **(d) Severity:** **Structural gap.** The two-bucket taxonomy cannot be applied mechanically.

### Case 3 — Default-only readonly field, faulting default expression

```precept
field X as number default sqrt(-1)
field Y as number default 1 / 0
```

- **(a) Literal Draft C classification:** Same gap as Case 2: default expressions are not listed in §2.1.
- **(b) §3/§4/§5 mechanics:** §4.1 expressly lists `sqrt` domain violations and division-by-zero inside prove-or-reject's scope for computed values. §3 has no ingress or sweep story for a field that is never supplied. So Draft C strongly *suggests* compile-time rejection, but only via a bucket the taxonomy never explicitly assigns.
- **(c) Gap / inconsistency:** The document's most absolute proof claim (fault-freedom for internal expression results) depends on treating defaults as computed, but §2.1 does not say that.
- **(d) Severity:** **Structural gap.** This is not cosmetic; it controls whether faulting defaults are even covered.

### Case 4 — Default expression referencing an earlier field's default

```precept
field A as number default 12
field B as number nonzero default sqrt(A)
```

- **(a) Literal Draft C classification:** `A`'s default is unclassified (Case 1). `B`'s default expression is also unclassified-by-list though arguably implied-computed by the lead sentence.
- **(b) §3/§4/§5 mechanics:** If `B` is computed, §4 should prove both the `sqrt` domain and `nonzero`. But that proof would itself lean on `A`'s default-origin truth, and Draft C never states whether default-established facts participate in proof the same way governed facts from supplied fields do.
- **(c) Gap / inconsistency:** The document gives no explicit account of how proof premises coming from default-only fields enter the system. This is not an outright contradiction, but it leaves a hidden dependency unmodeled.
- **(d) Severity:** **Wording gap, bordering structural.** The model may survive if defaults are explicitly made first-class internal facts, but Draft C does not say that.

### Case 5 — Computed field over default-only source fields, stateless

```precept
field A as number nonzero default 12
field B as number nonzero default 10
field C as number <- B / sqrt(A)
```

- **(a) Literal Draft C classification:** `C` cleanly fits §2.1's computed-field bucket (`<-`). `A` and `B` remain unclassified defaults.
- **(b) §3/§4/§5 mechanics:** §4 clearly wants `C` on the prove-or-reject side. Its divisor and `sqrt` obligations are compile-time. This is one of the cases Draft C handles relatively well conceptually.
- **(c) Gap / inconsistency:** The computed field itself is fine, but the document still has to smuggle in default-only source facts without ever having classified those source values.
- **(d) Severity:** **Mostly fine, with adjacent wording gap.** This case does not break the model directly, but it shows the model relies on a missing default story underneath it.

### Case 6 — Rule over two default-only immutable fields (simple relational rule)

```precept
field A as number default 12
field B as number default 10
rule B < A because "B must stay below A"
```

- **(a) Literal Draft C classification:** `A` and `B` are again in neither enumerated provenance list. §5.1 nevertheless says "A rule is always governed."
- **(b) §3/§4/§5 mechanics:** Governance in §3 is delivered by ingress + post-mutation sweep on operations. But this precept can have no direct edit, no event arg, and no computed assignment touching either field. If nothing ever changes, the only meaningful enforcement points are construction-time / compile-time default checks — which Draft C does not assign to either region.
- **(c) Gap / inconsistency:** This is the cleanest contradiction between §5.1 (rule always governed) and the actual need for compile-time/default-time rule truth when a rule only touches immutable internal values.
- **(d) Severity:** **Structural gap.** The "rule always governed" slogan is too broad as written.

### Case 7 — Shane's harder case: rule over immutable defaults with `sqrt` inside the rule

```precept
field test  as number nonzero default 12
field test2 as number nonzero default 10
rule test2 > sqrt(test) because "test2 must exceed sqrt(test)"
```

- **(a) Literal Draft C classification:** `test` and `test2` default values are unclassified by §2.1's list. The rule is declared governed by §5.1.
- **(b) §3/§4/§5 mechanics:** Three different obligations now exist: (1) `test`'s own `nonzero`, (2) the `sqrt(test)` domain inside the rule expression, and (3) the truth of `test2 > sqrt(test)`. §4 owns fault-freedom for computed values, but §5 says a rule is never itself proven; §3 owns governed rules, but governance is described operationally, not as a compile-time fold over immutable defaults.
- **(c) Gap / inconsistency:** Draft C has no clear owner for the `sqrt` fault inside a rule over immutable internal defaults. This is the hardest exposure of the taxonomy: it is not just that defaults were omitted; it is that the rule / compute split is no longer sufficient once rule expressions themselves contain fault-prone internal computation.
- **(d) Severity:** **Structural gap, very high.** This is the strongest counterexample in the matrix.

### Case 8 — Editable field with a default, no edits ever occur

```precept
field Price as number nonnegative editable default 12
```

- **(a) Literal Draft C classification:** §2.4's worked definition says editable source fields are supplied and "every value they hold is supplied." §2.2, however, says a `default` is internal to the definition. These directly conflict.
- **(b) §3/§4/§5 mechanics:** If the field's initial default counts as supplied, governance would have to enforce it — but no outside value entered. If it counts as internal, then the field's first inhabitant is not governed at ingress at all.
- **(c) Gap / inconsistency:** This is the simplest mixed-provenance field and it already disproves any field-level reading of Draft C's taxonomy.
- **(d) Severity:** **Structural gap.** The model needs value-instance / write-site provenance, not field-level provenance.

### Case 9 — Editable field with default, then later direct edits

```precept
field Price as number nonnegative editable default 12
```

(Host later edits `Price` directly.)

- **(a) Literal Draft C classification:** Default inhabitant: internal per §2.2 but not listed in §2.1. Later edit inhabitant: supplied per §2.1 / §3.1.
- **(b) §3/§4/§5 mechanics:** The model can plausibly say: initial default settled internally; later edit values governed at ingress and sweep. This is conceptually workable.
- **(c) Gap / inconsistency:** Draft C does not actually *say* provenance can vary by occupant over time for the same field, even though this case forces it. §2.4's editable-field walkthrough reads field-level, not value-instance-level.
- **(d) Severity:** **Wording gap.** The underlying model can probably survive, but the document currently states it in a way that invites the wrong field-level reading.

### Case 10 — Readonly/default-only field later assigned by `set`

```precept
field Balance as number nonnegative default 0
on Recompute -> set Balance = 5
```

- **(a) Literal Draft C classification:** `Balance` holds an internal default first (unclassified by list), then a computed assignment result later (`set` bucket).
- **(b) §3/§4/§5 mechanics:** The later `set` clearly sits in §4. The initial default still needs a story at construction. If the bound is satisfied in both phases, the case is substantively fine.
- **(c) Gap / inconsistency:** This is another proof that the right unit is not "field" but "occupant / origin at a specific write site." Draft C says this in §1.2, but its later exposition slips back into field-level shortcuts.
- **(d) Severity:** **Wording gap.** Manageable, but still an important clarity problem.

### Case 11 — Rule over editable field and default-only immutable field

```precept
field Floor as number default 10
field Amount as number editable default 12
rule Amount >= Floor because "Amount may not drop below Floor"
```

- **(a) Literal Draft C classification:** `Floor` default is omitted from §2.1. `Amount` is contradictory: its default is internal per §2.2; its later edits are supplied per §2.1.
- **(b) §3/§4/§5 mechanics:** Later edits of `Amount` can be governed by sweep against `Floor`. But the initial default/default configuration also needs the rule checked before any edit occurs.
- **(c) Gap / inconsistency:** Draft C explains the operational governance half but not the construction-time half. The rule is meaningful before the first external edit, yet the provenance model only gets comfortable once one operand becomes supplied.
- **(d) Severity:** **Structural gap.** The omission of construction/default semantics leaves the rule half-covered.

### Case 12 — Rule over computed field and default-only field

```precept
field Base as number default 12
field Derived as number <- sqrt(Base)
field Limit as number default 10
rule Derived <= Limit because "Derived must stay within limit"
```

- **(a) Literal Draft C classification:** `Derived` is computed; `Base` and `Limit` defaults are not in §2.1's enumerated partition.
- **(b) §3/§4/§5 mechanics:** §4 wants `Derived` proven. §5 says the rule is always governed and may also be premise. But if the rule mentions only internal values (`Derived`, `Limit`), governance has no obvious runtime work to do unless Draft C intends a perpetual sweep over immutable truths.
- **(c) Gap / inconsistency:** This case makes §5's slogan unstable. A rule over a computed field and an immutable default is not naturally described as a runtime-governed constraint first and a proof premise second.
- **(d) Severity:** **Structural gap.** The rule/field duality story does not cleanly cover internal-only rules.

### Case 13 — Stateless precept, no events, only defaults and rules

```precept
field A as number default 12
field B as number default 10
rule A > B because "A must exceed B"
```

- **(a) Literal Draft C classification:** Same default omission as above.
- **(b) §3/§4/§5 mechanics:** In a stateless no-event precept, Draft C still says the rule is governed and that governance runs on every operation. But there may be no operation after construction. Canon says defaults and global rules are checked if no initial event exists; Draft C does not tell that story.
- **(c) Gap / inconsistency:** The model reads like it presupposes ongoing operational governance, but stateless/default-only precepts need construction-time truth to be first-class.
- **(d) Severity:** **Structural gap.** A whole category of valid definitions is only partially modeled.

### Case 14 — Events exist, but none ever touch the fields in the rule

```precept
field A as number default 12
field B as number default 10
rule A > B because "A must exceed B"

event Ping()
on Ping -> no transition
```

- **(a) Literal Draft C classification:** `A` and `B` defaults omitted from §2.1; rule still labeled governed.
- **(b) §3/§4/§5 mechanics:** If governance means the sweep rechecks all rules on every operation, then `Ping` would keep rechecking an immutable truth. That is mechanically plausible. The missing piece is still the initial / pre-first-op truth of the rule.
- **(c) Gap / inconsistency:** This case is less severe than Case 13 because runtime recheck at least exists, but it still exposes the missing construction story and raises the question whether Draft C is intentionally describing a rule that is operationally governed despite being semantically fixed.
- **(d) Severity:** **Wording gap.** The model likely survives if Draft C explicitly says immutable rules are construction-checked and may be rechecked vacuously thereafter.

### Case 15 — One side of a rule changes; the other is default-only immutable

```precept
field Ceiling as number default 10
field Current as number editable default 5
rule Current <= Ceiling because "Current may not exceed Ceiling"
```

- **(a) Literal Draft C classification:** `Ceiling` default omitted; `Current` is mixed-origin over time.
- **(b) §3/§4/§5 mechanics:** Later edits to `Current` are naturally governed by sweep against `Ceiling`. This is one of Draft C's stronger cases operationally.
- **(c) Gap / inconsistency:** The surviving issue is still the initial default/default configuration and the fact that `Current`'s first occupant is not supplied.
- **(d) Severity:** **Mostly fine, with a missing construction qualifier.** This is not a fatal case by itself.

### Case 16 — Default collection value with field-level count bound, no mutations

```precept
field Items as list of string maxcount 1 default []
```

- **(a) Literal Draft C classification:** The default collection value is again not in either §2.1 list. §2.1's computed bucket only mentions **derived** collection state after grow/shrink, not initial default collection state.
- **(b) §3/§4/§5 mechanics:** The `maxcount 1` obligation on `default []` should trivially hold, but Draft C does not say whether initial collection-shape obligations on default values are compile-time/internal or governed somehow.
- **(c) Gap / inconsistency:** Collections do not escape the default hole; they just make it visible on count/cardinality rather than scalar bounds.
- **(d) Severity:** **Structural gap.** The derived-collection story is incomplete without default-collection provenance.

### Case 17 — Default collection plus later grow/shrink actions

```precept
field Items as queue of string maxcount 1 default []
on AddOne(v as string) -> enqueue Items v
```

- **(a) Literal Draft C classification:** Initial `[]` default is omitted from §2.1; later cardinality after `enqueue` is covered by §2.1's derived-collection-state bucket.
- **(b) §3/§4/§5 mechanics:** Draft C handles the later `enqueue` proof obligation well (§4.4's count example). But it never explains the provenance transition from default cardinality (initial occupant) to derived cardinality (post-action occupant).
- **(c) Gap / inconsistency:** This is the collection analogue of Cases 8-10: the same field's state moves across provenance classes over time, but Draft C only models the post-mutation half explicitly.
- **(d) Severity:** **Wording gap, with structural undertone.** The count-proof mechanism is fine; the taxonomy around initialization is not.

### Case 18 — Rule over default collection cardinality only

```precept
field Items as set of string default []
rule Items.count == 0 because "Starts empty"
```

- **(a) Literal Draft C classification:** The collection default is not enumerated in §2.1, and the rule is labeled always governed by §5.1.
- **(b) §3/§4/§5 mechanics:** If no operation ever mutates `Items`, the rule is another immutable default-only truth that still needs an initial truth check. Draft C gives no explicit home to that check.
- **(c) Gap / inconsistency:** Same structural problem as Case 6, now in collection form.
- **(d) Severity:** **Structural gap.** The model still lacks a construction/default-only rule story.

### Case 19 — Editable field with both direct edits and action-based `set`

```precept
field Balance as number editable default 0
on Recompute(v as number) -> set Balance = Balance + v
```

- **(a) Literal Draft C classification:** `Balance` can hold at least three origins over time: default internal occupant, supplied direct-edit occupant, computed `set` occupant.
- **(b) §3/§4/§5 mechanics:** Direct edits are governed; action results are prove-or-reject; defaults are omitted-by-list but implied internal. So the same field's occupant provenance varies by write site.
- **(c) Gap / inconsistency:** This is the cleanest demonstration that the document cannot safely summarize provenance at the field level. The model must say "classify each *write-produced occupant*" or equivalent; otherwise examples like §2.4 mislead.
- **(d) Severity:** **Structural gap.** This is foundational to whether the taxonomy is actually usable.

### Case 20 — Defaulted event argument (hidden bridge case)

```precept
event Triage(Level as integer positive default 3)
on Triage -> no transition
```

- **(a) Literal Draft C classification:** §2.1 says event arguments are supplied values carried in from a fired event. But if the caller omits `Level` and the definition supplies `default 3`, the actual runtime value was not handed in from outside. It also is not one of §2.1's computed forms.
- **(b) §3/§4/§5 mechanics:** If treated as supplied, governance at ingress makes sense operationally, because the arg slot is still part of the event boundary. But the source of the concrete value is declaration-side default materialization, not caller supply. If treated as internal, then §2.1's event-argument bucket is not actually exhaustive.
- **(c) Gap / inconsistency:** Draft C assumes all event-arg values are supplied; defaulted args show that the boundary is subtler. This is an extra crack beyond Shane's and Frank's findings.
- **(d) Severity:** **Structural gap.** The provenance taxonomy likely needs an explicit rule about declaration-supplied values at external slots.

## 3. Cross-case patterns

### 3.1 Repeated wording gap

Draft C often says the right abstract thing — classify **values**, not constructs — but repeatedly illustrates it with **field-level** descriptions that cease to be true the moment a field can hold values from more than one origin over time.

This affects:
- §2.4's editable-source-field vs computed-field split
- §5.2's flagship examples, which are cleaner than many real fields
- the reference table in §7.1, which presents origins as if they exhaust per-field categories

### 3.2 Repeated structural gap

Anything involving **defaults** — especially rules or faulting expressions over default-only immutable values — exposes that §2.1's two-origin taxonomy is not actually a usable partition unless defaults are made explicit.

The document currently relies on three incompatible moves:
- defaults are omitted from the enumerated buckets (§2.1)
- defaults are later described as internal (§2.2)
- rules are said to be always governed (§5.1), even when only default-only internal values are involved

Those three cannot all remain as written.

### 3.3 Construction is the missing third location in the exposition

Even if the author wants to preserve a two-origin model, the exposition still lacks a first-class account of **construction / default materialization / compile-time default fold** as a place where obligations are settled.

Without that, the document keeps sounding as though all governance begins at operational ingress and sweep, which is false for default-only truths.

## 4. Severity-ranked list of cracks to close before anyone trusts the model again

1. **Add an explicit provenance rule for defaults** — literal defaults, expression defaults, collection defaults, and defaulted event args. Without this, §2.1 is not a partition.
2. **Resolve the rule-over-internal-values problem** — either narrow "A rule is always governed" or explicitly explain the compile-time/default-fold story for rules whose values are never externally supplied.
3. **State the true unit of classification as a write-produced value instance, not a field.** Mixed-origin fields otherwise keep breaking the prose.
4. **Separate origin from enforcement location.** The document currently mixes "where the value came from" with "when/where the engine settles the obligation," and defaults show those are not identical questions.
5. **Add construction-time semantics to the model, not just to canon elsewhere.** Right now Draft C quietly depends on canonical behavior it never narrates.

## 5. Bottom line

Draft C is **not** just missing an edge case. The defaults hole is systemic: once defaults are added back into the matrix, they destabilize §2.1's partition, §2.4's field-level examples, §3's governance story for construction-time truth, and §5.1's "rule always governed" claim.

The hardest single counterexample remains Shane's `rule test2 > sqrt(test)` over immutable default-only fields. It forces the document to answer all of these at once:
- what bucket default expressions belong to,
- who owns fault-prone computation inside a rule over internal values,
- whether the rule is compile-time-proven, runtime-governed, construction-folded, or some combination,
- and whether the document's two-bucket provenance model is actually sufficient.

As written, Draft C does **not** answer that case cleanly.
