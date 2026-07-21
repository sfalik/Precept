---
status: Externally-Grounded
phase-target: TBD — prerequisite to slice 4 of the obligation-discharge-matrix population; blocks every fault cell's transport citation
comparable-systems-research-status: partial — external precedent carried inline per decision from in-tree Stage-1 research files (Frama-C EVA, Astrée, CBMC/SSA, dominator-based propagation), each with a verbatim excerpt; one field technique (trace partitioning) is named as an explicit research gap with no in-tree file and no primary source read this pass
sources-consulted:
  - docs/philosophy.md — "The compiler does not trust that an expression will succeed — it proves it will."
  - docs/language/precept-language-spec.md § 0.1 — the eleven principles (3, 7, 10, 11 quoted per decision)
  - docs/language/precept-language-spec.md § 0.4 — execution-model properties (no loops, no branches, no reconverging flow, finite state space, expression purity)
  - docs/language/precept-language-spec.md § 0.6 — proof philosophy #1 (safe direction) and the hard-cap admissibility criterion
  - docs/language/precept-language-spec.md § 0.7 — the compile-time/runtime guarantee contract and the boundary of the guarantee (restore is trusted, not re-validated)
  - docs/language/precept-language-spec.md:96, :164, :170 — determinism, sequential assignment, expression purity
  - docs/language/precept-language-spec.md:110 — Principle 10, totality, quantified over expressions
  - docs/language/precept-language-spec.md:160, :162, :168 — quantifiers unfold finitely; no reconverging branches; finite state space
  - docs/language/precept-language-spec.md:258 — grow-establishment proves only `count > 0`; the count-interval delta model
  - docs/language/precept-language-spec.md:1007 — stateless event handlers carry no `when` guard
  - docs/language/precept-language-spec.md:2119 — the inspection result matches execution "for the same inputs"
  - docs/runtime/evaluator.md:495-505 — the Fire lifecycle enumerated with no hook step
  - docs/runtime/evaluator.md:2148 — `to <State>` constraints are evaluated only during Fire
  - docs/language/collection-types.md:796 — the element-constraint / quantifier-governance passage the matrix cites into its Edge cells conflict
  - docs/language/precept-language-spec.md:1061, :1074 — access-mode declaration placement; `omit` clears on state entry
  - docs/language/precept-language-spec.md:1897, :1904 — guards select; transition rows are first-match
  - docs/language/precept-language-spec.md:1915, :1918, :1919 — no-transition skips entry/exit actions; unmatched routed event; undefined event surface
  - docs/language/precept-language-spec.md:1967, :1969, :1971 — working-copy atomicity; ingress before derivation; the mutation-surface list
  - docs/language/precept-language-spec.md:2117 — inspection has the same depth as event execution
  - docs/compiler/proof-engine.md:495 — reject-sibling narrowing is sound only for a single trackable leaf
  - docs/compiler/proof-engine.md:606 — flag-modifier lower bounds; `nonzero` contributes nothing to an interval
  - docs/compiler/proof-engine.md:1249, :1251, :1253 — prefix-effect stamping; full-replacement invalidation; shrink invalidation with upper-bound preservation
  - docs/runtime/evaluator.md:144 — computed-field recomputation is after mutations and before constraint evaluation
  - docs/runtime/evaluator.md:789-790, :817-820 — the inspect path's Kleene guard and unconditional action simulation with `FiredArgs.Empty`
  - docs/runtime/evaluator.md:1485 — `InspectFire(string eventName, JsonElement? args = null)` — args are optional
  - docs/runtime/evaluator.md:2129-2148 — the Constraint Evaluation Matrix and its key rules
  - docs/runtime/runtime-api.md:266 — Restore recomputes computed fields and does not re-validate constraints
  - docs/runtime/runtime-api.md:361, :365-374, :386 — the Fire pipeline, the two-field Update patch, the Update pipeline
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — the 2026-07-21 site-identity ruling
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § The cell — the open design hole and the 2026-07-21 citation-duty ruling
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — write plan, discharge contract, discharge mechanisms, obligation schema
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Rule validity — the ratification gate and the seven arguments
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Per-family case shapes — the fault row and the residency-fact open marker
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Edge cells — the `omit` conflict and the quantifier conflict
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Storage — the generated coordinate enumeration ruling
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Amendments — power-widening and soundness-correction
  - docs/Working/what-i-want-2026-07-16.md:104, :141, :165, :206 — the fault-family worked example, ingress is exactly two points, the lighter runtime, the certificate walk
  - docs/Working/fault-family-validity-arguments-2026-07-21.md § Semantic Rules — Rules 1–4 and the transport rule this document replaces
  - docs/Working/fault-family-validity-arguments-2026-07-21.md § Open questions — the withdrawn transport-index and field-modifier narrowings, with their preserved decision text
  - docs/Working/obligation-discharge-matrix-2026-07-19-cells/slice-3-boundary-report.md:35, :36, :38, :42, :107 — the five uncatalogued requirement kinds, the seven dynamic minting mechanisms, overflow's absence, the syntactic-position soft spot, the nineteen verification passes
  - docs/Working/obligation-discharge-matrix-2026-07-19-cells/fault-axis-disposition-map.csv — the committed fault coordinate enumeration (9,292 rows)
  - docs/Working/obligation-discharge-matrix-2026-07-19-cells/fault-axis-disposition-meta.json — the axis sizes and disposition counts
  - src/Precept/Language/ProofRequirementKind.cs — the thirteen obligation kinds
  - src/Precept/Language/ProofRequirement.cs:140-148, :215-232, :260-270 — assignment-qualifier, count-containment and index-bounds requirement shapes
  - samples/trafficlight.precept — exit hook, `from any`, `omit`, editable window
  - samples/warranty-repair-request.precept — entry hook, residency ensures, editable window
  - samples/production-order-tracking.precept:27-29, :137-144 — the authored guard workaround and its guarded division rows
  - research/architecture/compiler/proof-engine-interval-arithmetic-survey.md:139-142, :230-233, :479-489 — Frama-C EVA and Astrée forward abstract-state propagation with path splitting; CBMC's SSA encoding
  - research/architecture/compiler/solver-free-static-analysis-techniques-survey.md § Family C — dominator-based fact propagation
  - live `precept_compile` at HEAD, 2026-07-21 — every `.precept` sample printed in this document, verdicted from diagnostic codes and obligation records
---

# The transport rule, restated for evaluation occasions

**Status: Design. Implementation is parked. Nothing here is applied.** Every item in § Open questions is routed to the owner, and several of them are prerequisites rather than follow-ons.

## How to read this document

Three terms carry the argument, and each is plain English on purpose.

- **Transport rule** — the rule that says how a fact established in one place (a guard, an argument constraint, a declaration) reaches the place where a proof consumes it, and under what conditions the fact is still true when it gets there.
- **Route** — one class of executions of one operation. The word is this document's; the *idea* is read off the language's own execution model, and § Semantic Rules derives it rather than asserting it.
- **Kill** — a fact is *killed* when something between where it was earned and where it is used may have written a field the fact talks about. A killed fact is dead: not merely unproven, but unusable.

Citations to the obligation-discharge matrix are by **section heading plus verbatim quote**, never by line number — that file was edited on 2026-07-21 and its line numbers moved by roughly forty. Citations to the language spec, the runtime docs and the source tree are line-anchored and were read from the working tree in this pass.

**Evidence discipline.** Every `.precept` sample below was compiled through `precept_compile` in this pass; the results are printed as *observations of what the shipped compiler does today*, never as design authority. The shipped compiler has verified defects, and two of the samples below are witnesses to them.

---

## Goal

When done, the fault family has one written transport rule that is indexed by the same route as the fact it moves, that carries its own truth-preservation argument against the evaluator's documented semantics, and that a checker can decide by a finite walk — demonstrated by a pair of programs differing in two words that the rule gives opposite verdicts and that the shipped compiler today accepts identically.

## Scope

**In scope**

- What a route is, derived from the spec's execution model, and the proof that routes are finitely enumerable at compile time.
- The relation from an evaluation site to the routes that reach it, stated over all twenty-three values of the committed evaluation-site axis.
- What earns a fact, what preserves it, and what kills it, stated precisely enough to mechanize.
- The reconciliation between whole-plan preservation obligations and mid-plan fault sites, leaving the locked write-plan definition untouched.
- The transport rule's validity argument, owed under the matrix's ratification gate.
- What a fault cell's key becomes, and the size and shape consequences for the coordinate enumeration.

**Out of scope**

- The `number` lane. Ruled on 2026-07-21 (matrix, § The approximate lane).
- Regeneration of the fault coordinate enumeration. Ruled (matrix, § Storage).
- Underflow. Open and unrouted; the matrix records it as such and this pass does not touch it.
- Minting — which sites mint obligations. This design consumes the minting rule; § Open questions records two places where the rule's phrasing does not cover a case the committed axis contains, and routes them rather than adjusting them.
- Any code change.

**Deferred to future**

- Pinning the transport step into the closed certificate-step vocabulary. That requires reading `certificate-steps-membership-2026-07-12.md`, which this pass did not read and which is named as the largest unread dependency.
- The five requirement kinds with no catalog declaration site, and the seven dynamic minting mechanisms outside the catalog walk. The route index is supplied for them where they fall inside an existing site category; where they do not, they are routed.

---

## Philosophy Alignment

| Principle | Affected? | How served (cite) | Tension | Tradeoff |
|---|---|---|---|---|
| 1. Prevention, not detection | Y | The rule's whole job is to stop a fact being consumed where it is no longer true; a killed fact rejects the definition rather than producing a runtime trap (`philosophy.md` — *"The compiler does not trust that an expression will succeed — it proves it will."*) | The corrected rule rejects programs the shipped compiler accepts, including two witnesses printed below | Authors of exit-hook and cross-surface programs must move or restate a guard; the alternative is a definition that licenses division by zero |
| 2. One file, complete rules | N | A route is computed from the single file's declared states, events, rows and hooks; no external state enters (spec:170 — *"Expressions cannot mutate entity state, trigger side effects, or observe anything outside their evaluation context"*) | N/A | N/A |
| 3. Deterministic semantics | Y | Route identity is descriptor equality over statically declared sets, and the transport decision is a finite intersection — two conforming compilers compute the same set (spec:96 — *"Same definition, same data, same outcome — always."*) | A route descriptor built on a *positional* row index would not be stable under an inserted row; the design uses a declaration identity instead | Certificates carry a slightly larger descriptor so replay cannot silently re-bind |
| 4. Full inspectability | Y | The certificate gains the route descriptor and the ordered list of write targets the compiler used, so a reader can see *which execution* a proof was about (spec § 0.1 #4 — *"Inspectability extends to proof reasoning — proven ranges, source attribution, and what the engine could not prove must all be surfaceable"*) | One written expression now has several verdicts, and no diagnostic produces route-qualified text today | Diagnostics become a hard prerequisite, not polish; § Audience carries the wording problem and does not claim to have solved it |
| 5. Keyword-anchored readability | N | No token, keyword, construct or layout rule changes | N/A | N/A |
| 6. Explicit domain meaning over primitive convenience | N | The rule is stated over fields and coordinates without reference to a type family; business-domain magnitudes transport exactly as primitives do | N/A | N/A |
| 7. Compile-time-first static checking | Y | Route enumeration is a bounded product over statically declared sets with no search and no fixpoint, satisfying the hard-cap requirement in spec § 0.6 proof philosophy #3 (*"where a procedure could blow up it hits a hard cap and falls back to an explicit 'couldn't prove'"*) | Enumeration is linear in the file only if `from any` desugars per state; that desugaring is not settled in canon | Routed; the count, not the rule, depends on it |
| 8. Approximation honesty | N | The argument uses determinism, purity, target-naming and enumerability only — no arithmetic identity, no rounding, no exactness — so it holds uniformly across `integer`, `decimal` and `number` | N/A | N/A |
| 9. Mandatory rationale (`because`) | Y | A `because` interpolation hole is an evaluation site the route relation must index; the index is supplied whether or not the holes are ruled in | Whether the holes enrol at all is a deferred owner-fork; over 400 committed coordinates hang on it | The index is supplied both ways rather than the question being pre-empted |
| 10. Totality | Y | The rule is the bridge between a fact and the stamped predicate at a fault site; a wrong bridge is exactly a silently-`NaN`-or-divide-by-zero acceptance (spec:110 — *"Every expression evaluates to a result — never silent `NaN`, `Infinity`, or `null`"*) | The rule's soundness rests on a list of in-operation writers that canon nowhere declares closed | Recorded as the design's residual soundness dependency and routed as a one-sentence spec addition, not papered over |
| 11. Static completeness | Y | Every occasion of every site on every route mints and must discharge; a site reached by no route is a definition error rather than a silent pass | Five of the thirteen requirement kinds and at least two minting mechanisms sit outside the committed site axis, so the index is not yet total over what the compiler actually mints | Stated as a named gap with the measurement that establishes it, rather than claiming coverage |

**Rows with a live tension, as accepted tradeoffs.**

*Principle 1 and 3.* The corrected rule rejects programs that compile today. That is the expensive half of the matrix's amendment rules — § Amendments, verbatim: *"Shrinking the licensed set is permitted **only** under this class. Always a major definition version; always breaks certificate replay for affected cells; always routed to the owner with the **witness program** demonstrating the unsoundness."* This document therefore **routes** every narrowing with its witness and settles none of them.

*Principle 4.* Route keying means one expression proves on one route and not another. An unqualified diagnostic is then not merely unhelpful, it is false. The matrix already books this — § The minting rule: *"Diagnostics must additionally name the route, since one expression may now be provable on one path and not another; no diagnostic produces route-qualified text today."* This design does not solve the wording problem for a business-analyst reader and says so.

*Principle 10 and 11.* The rule's soundness argument has one step that cannot be closed from canon: that the list of things which write during an operation is complete. One writer that nobody had enumerated was found in this pass by reading the access-mode composition rules (the `omit` reset). That is evidence the list is discoverable and equally evidence that "I looked and found no more" is not closure.

**Companion commitments.** *Stateless-first-class*: a stateless precept has no transition rows, so the route enumeration must not be written in terms of them — § Semantic Rules states the stateless handler as a first-class route source and § Open questions routes the one place canon is silent (whether several stateless handlers may exist for one event, and in what order). *Domain-expert-primary-author*: the rule is invisible to the author except through rejections and through diagnostics that must now name a route. Both pressure points are stated rather than minimised.

---

## Language Design Grounding

**This design introduces and modifies no language surface** — no token, keyword, type, operator, modifier, construct, accessor or expression form. Guard 2 is therefore not triggered.

One consequence is nonetheless author-visible and is treated in § Architecture Grounding rather than here, because it is architectural rather than syntactic: under the matrix's discharge-contract sentence — § Vocabulary, verbatim: *"it pins the accepted set exactly, not as a floor"* — changing the transport rule changes which files compile. That is the *precision* of an analysis, not its surface, and the comparators for it are dataflow and abstract-interpretation systems.

---

## Audience and Teachability

**Not a language-surface change**, so guard 3 is not triggered. Two paragraphs anyway, because the design changes what a domain expert sees.

**What the author sees.** More rejections, in one shape: a guard written at the transition row no longer covers a division that happens somewhere else in the same operation. The fix is to move or repeat the condition. Here is the whole of it, in a plausible shape — a signal controller that clears an emergency reason on the way out of the emergency state:

```precept
# The guard is on the row; the write that breaks it is on the exit hook.
from FlashingRed -> clear EmergencyReason
from FlashingRed on ClearEmergency when EmergencyReason is set
    -> set LastReason = EmergencyReason
    -> transition Red
```

Under this design the `is set` fact does not survive to the row's action, because the exit hook may run first and `clear` empties the field. The author's fix is to read the value before the hook can touch it, or to move the write.

**The diagnostic wording problem, stated and not solved.** The message must now say *which* execution failed. For the exit-hook shape a workable wording is:

```
PRE0083  Division is unsafe: 'Divisor' can be zero when 'Go' fires from
         'Warm' and the transition to 'Cold' runs.

  from Warm -> set Divisor = 0
                  ^^^^^^^ this exit action runs during the same operation
  Your guard `when Divisor != 0` was true before this action ran.
```

That names the route in the author's own words — event, source state, destination — and never says "route", "occasion" or "transport". **What this design does not have** is a wording for the harder shape: a residency ensure reached by four transitions, of which two prove and two do not. Naming four executions in one message, to a reader the spec describes as a *"business analyst, product owner, regulatory specialist"*, is an unsolved problem and is listed in § Falsifiers.

**Ten-minute teaching path.** (1) `samples/trafficlight.precept` lines 27–33 — the exit hook and the editable window, the two constructs that make an operation bigger than a row. (2) `docs/language/precept-language-spec.md` § 3A.2 outcome 2 — why a `no transition` row skips hooks. (3) `samples/production-order-tracking.precept:27-29` — an author's own comment about guarding a division the engine cannot follow.

---

## Semantic Rules

This is the substance of the design. Notation: a **configuration** is a pair of (lifecycle position, field values); `⟦e⟧σ` is the value the evaluator computes for expression `e` in configuration `σ`; `φ` is a fact; `R` is a stamped safety predicate at a fault site.

### 1. What a route is

> **Route.** A **route** is a finite, statically-enumerable descriptor of one class of executions of one operation:
>
> ```
> r  =  ( kind , state? , event? , row? , outcome? , patch? )
> ```
>
> - **kind** — the operation class, drawn from the runtime's own operation table.
> - **state** — the lifecycle position the entity occupies when the operation begins; absent for construction and for stateless precepts.
> - **event** — the event fired; the initial event for construction; absent for update and restore.
> - **row** — a **stable row identity**: the row's `(state, event)` group plus its own declaration identity. Never a positional index. Absent where no row dispatch occurs, and absent on the all-guards-failed class.
> - **outcome** — `transition T`, `no transition`, or `reject`, read syntactically off the row. Fixed once the row is fixed.
> - **patch** — for update-class routes: the set of fields the state's access-mode composition admits as writable. A set, never a single field.
>
> **Two executions are on the same route exactly when their descriptors are equal.** A route is a static equivalence class of runtime occasions; a route-indexed obligation is universally quantified over its class.

**Why these components and no others.** The descriptor records exactly the things that decide *which sites are evaluated* and *which constructs may write*.

- The **operation class** is canon and closed, and it is the authority for which constraint buckets each operation evaluates. `docs/runtime/evaluator.md:2133-2143`, verbatim rows: `| Fire | no | yes | always, from <current>, on <event>, to <target> |`; `| InspectFire | no | yes (all rows) | same as Fire, but evaluated for every row |`; `| Update | yes | no | always, in <current> |`; `| Restore | no (bypassed) | no | always, in <current> — computed fields recomputed first |`. This table is why inspection is a separate class and not a mode: `InspectFire` evaluates constraints *for every row*, not only the matched one.
- **Row selection** is a component because rows are first-match. `spec:1904`, verbatim: *"Transition rows are evaluated in declaration order — the first matching guard wins, and remaining rows are not evaluated."*
- **Outcome** is a component because it decides whether hooks are on the operation at all. `spec:1915`, verbatim: *"**Successful no-transition event.** Event fired; in-place mutations committed; no state change. This is a deliberate design allowing in-place data changes to be event-driven **without triggering entry/exit actions**."*
- **Nothing else can branch.** `spec:162`, verbatim: *"There are no `if` statements that split execution into paths that later reconverge."* Guards are the only branch points, and every guard on a route is either fixed by the descriptor or handled by over-approximation.

**Deliberately excluded**: hook `when` guards, access-mode guards, and conditional-expression branch conditions. Including them multiplies the descriptor by two per declared guard, which spec § 0.6 proof philosophy #3 forbids — *"where a procedure could blow up it hits a hard cap and falls back to an explicit 'couldn't prove', never searching unboundedly"*. They are handled instead by treating their writes as *may*-writes, which is safe in the kill direction under proof philosophy #1 — *"The language always chooses the safe direction."*

### 2. Routes are finitely enumerable

```
routes(P) =
    { fire on each (state, event, row) group member }
  ∪ { the all-guards-failed class per (state, event) group with no unguarded row }
  ∪ { construction, per construction row, or one parameterless construction route }
  ∪ { update, per state admitting a writable field set }
  ∪ { restore, per state }
  ∪ { the stateless handler class, per (event, handler) }
  ∪ the inspection mirrors of the above
```

The count is a bounded sum of statically declared quantities. `spec:168`, verbatim: *"Every transition row, state action, and rule can be enumerated exhaustively. No symbolic execution over unbounded domains is needed."* There is no search and no fixpoint, so the hard cap is met trivially — the enumeration is a product, not a search.

Two enumeration rules exist to close silent gaps that a naive reading leaves:

- **The all-guards-failed class is a route.** `spec:1918`, verbatim: *"**Unmatched routed event.** Transition rows exist for the event but all guards failed — an instance data condition."* On that execution every guard in the group is evaluated, so every fault site inside a guard is live. Omitting it loses those occasions silently.
- **An undefined event surface produces no route, in writing.** `spec:1919`, verbatim: *"**Undefined event surface.** No transition rows defined for this event in the current state — a definition gap."* Nothing is evaluated, so the pruning is a written derivation, per the matrix, § The cell: *"Pruned cells are written, not absent — an incorrectly-pruned cell must be findable, never a silent gap wearing an 'empty' badge."*

**Stateless precepts are first-class in the enumeration, not an afterthought.** A stateless precept has no transition rows; its handlers are the `on Event -> …` form. If the fire clause were written over transition rows only, a stateless precept would produce no fire routes at all, every stateless site would be reached by nothing, and the rule below would either reject the file or vacuously discharge every division in it. The enumeration therefore names the stateless handler class explicitly. Worked, and compiled clean in this pass:

```precept
precept StatelessArgDivide

field UnitCost as decimal default 0.0 nonnegative
field Total as decimal default 0.0 nonnegative

event Allocate(Amount as decimal nonnegative, Units as integer positive)

on Allocate
    -> set UnitCost = Allocate.Amount / Allocate.Units
    -> set Total = Allocate.Amount
```

*Today:* `success: true`, zero diagnostics, divisor obligation `Proved`, `strategy: CompositionalConstraint`. *Under this design:* the division has two routes — the stateless handler class and its inspection mirror — and the second of them is where the argument fact is not available (§ 5).

### 3. Where a site is evaluated — the route relation

The relation must be **total over the committed evaluation-site axis**, which has exactly twenty-three values. Verified in this pass by reading `docs/Working/obligation-discharge-matrix-2026-07-19-cells/fault-axis-disposition-map.csv` (9,292 rows) and its companion `fault-axis-disposition-meta.json` (`"productSize": 9292`, `"proofRequirementSites": 101`, `"evaluationSiteCategories": 23`, four type families).

| Evaluation-site category | Route classes that reach it |
|---|---|
| `transition-row-guard` | fire (rows at or after this one in its group), the all-guards-failed class, inspect-fire, and — see § Open questions — inspect-update's event prospect |
| `transition-row-action-operand` | fire (this row only), inspect-fire |
| `reject-message-interpolation` | fire (this reject row), inspect-fire |
| `state-hook-guard` | fire and inspect-fire with a transition outcome touching the anchor state; construction if the anchor is the initial state (routed) |
| `state-hook-action-operand` | as above |
| `stateless-hook-action-operand` | the stateless handler class, its inspection mirror, construction and its mirror |
| `construction-row-action-operand` | construction and its inspection mirror |
| `field-default-value-expression` | construction and its mirror, **and** every fire route whose outcome is a transition into a state that omits the field — `spec:1074`, verbatim: *"**`omit` clears on state entry** — field value resets to default on any transition into an `omit` state (including self-transitions); does NOT apply to `no transition`."* |
| `event-arg-modifier-value-expression` | every route carrying an event, **at ingress** — `spec:1969`, verbatim: *"as they enter, before the working copy derives any dependent value"* |
| `event-ensure-condition` | as above |
| `field-modifier-value-expression` | every route, post-plan |
| `collection-inner-type-modifier-value-expression` | every route, post-plan |
| `type-qualifier-expression` | every route |
| `rule-condition` | every route — the `always` bucket appears in every row of the evaluator's constraint matrix |
| `rule-activation-guard` | every route |
| `state-ensure-condition` | per the evaluator's matrix: the `in <current>` bucket on fire, update, restore and construction; the `to <target>` bucket **only** on fire and construction-with-initial-event (`evaluator.md:2148` — *"`to <State>` constraints are evaluated only during Fire, not Update or Restore"*); the `from <current>` bucket on fire; plus mirrors |
| `ensure-activation-guard` | as its constraint |
| `computed-field-expression` | every route — recomputation appears in every operation's pipeline (`evaluator.md:144`, verbatim: *"**Computed field recomputation** \| After mutations and before constraint evaluation: walk `SlotLayout.ComputedSlots` and re-evaluate"*) |
| `constraint-rationale-interpolation` | its constraint's route set, restricted to routes where the constraint evaluates false; whether these holes enrol at all is a deferred fork, and the index is supplied either way |
| `quantifier-predicate` | its enclosing position's route set; element multiplicity is handled by universal quantification, not by the route (§ 8) |
| `choice-value-expression` | wherever its host is reached |
| `access-mode-guard` | update, its mirror, and restore — **including guarded readonly pairs**, because the guard's evaluation is real even when the write is then denied |
| `operand-free-action-precondition` | fire, inspect-fire, construction and its mirror |

**One consequence must be said out loud.** `operand-free-action-precondition` is a fault site with no written expression — the emptiness precondition of `dequeue`/`pop`. The occasion unit is therefore **an evaluation event at a catalog-stamped fault site**, not "an occurrence of a written expression". The owner's ruling uses the written-expression phrasing because that is the case it was illustrating; the committed axis contains a case the phrasing does not reach. This is stated as a **scope observation, not an override** — it changes the count for no expression-bearing site — and it is routed in § Open questions, because the matrix reserves the minting rule: § The minting rule, verbatim: *"A change to any part of the minting rule changes which cells exist, so any future amendment to it re-opens every cell the change reaches — it is amended under the amendment rules below, **never adjusted in passing**."*

### 4. The transport rule

Write `p ⪯ᵣ q` for "on route `r`, the construct at `p` is ordered no later than the construct at `q`" under the partial order § 6 pins. Write `mayWrite(r, p, q)` for the set of fields any construct on route `r` between `p` and `q` may write. Write `deps(φ)` for the read-closure of the fact (§ 7).

```
     Γ ⊢ᵣ φ earned-at p      p ⪯ᵣ q      deps(φ) ∩ mayWrite(r, p, q) = ∅
     ─────────────────────────────────────────────────────────────────────
                          Γ ⊢ᵣ φ holds-at q
```

**Name: route-indexed frame and kill.** It replaces the rule at `docs/Working/fault-family-validity-arguments-2026-07-21.md` § Semantic Rules Rule 3, *Frame and kill across the operation's mutation prefix*.

The four premises of a fault discharge become, all on the same route:

```
  adequacy(site, R)   provenance(φ, r, p)   transport(φ, r, p, q)   entail(φ ⊢ R)
  ────────────────────────────────────────────────────────────────────────────────
                            discharge(site, r, R)
```

This discharges the four items the parent design records as owed:

1. *The transport function must take a route rather than a site* — `mayWrite` takes `r`. ✓
2. *"Earned at plan entry" needs the same index* — provenance is `earned-at p` **on route `r`**, so the two halves cannot name different executions. ✓
3. *The justification that there is exactly one transport name is void* — repaired with a true justification: every *occasion* has exactly one write list, because a route fixes its own construct list. The citation rule becomes **exactly one transport name per (position, route) obligation**. ✓
4. *Cell keying* — § 9, and the answer is not `position × route`; see there. ✓

**The ordering premise is load-bearing and is not decoration.** Without `p ⪯ᵣ q` the rule licenses transporting a fact *backwards in time* — earned at `p`, consumed at `q`, where `q` actually runs first. Two of the witnesses in § 11 exploit exactly that hole. A larger kill set cannot repair it: growing a kill set only ever refuses more, it never establishes an ordering.

### 5. What earns a fact, and on which routes

| Provenance | Earned at | Available on | Killable |
|---|---|---|---|
| Ingress-governed argument fact | route position zero | routes whose event declares the argument **and on which ingress governance actually ran** — see the inspection carve-out below | never; nothing in a plan can write an argument |
| Fired-guard fact | immediately after that guard evaluates | routes on which *reaching the site implies the guard was true* — see the inspection carve-out | yes |
| Carried modifier fact / pre-state constraint fact | route position zero only | fire, update and the stateless handler class; **not** construction; **not** restore | yes |
| Literal denotation | at the site | every route | never; its read-closure is empty |
| Catalog-declared callee attribute | at the call site | every route | the *operand* facts are killable; the attribute is not |
| In-chain collection growth (`count > 0`) | after the growing action | every route, **only when the grow both must run and must precede** the consumption | yes, by a later shrink |
| Predecessor exclusion (earlier rows' guards were false) | at that guard's evaluation | fire and the all-guards-failed class | yes |
| Residency fact (the entity is in state `S`) | route position zero | routes with a resident state | yes — killed by a transition out |
| In-plan substitution (`F` equals the value just written) | — | — | **no rule exists**; routed, and it blocks the ruling's own example (§ 11) |

Three restrictions in that table are not obvious and each closes a false-proof shape:

**Kill on *may*; establish on *must*.** A grow inside a guarded hook belongs in the kill set — it may have run — but must **not** establish `count > 0`, because it may not have run. Over-approximation is free in one direction only. This is the general rule and it is stated once here. It agrees with canon's own phrasing at spec:258: *"grow-establishment proves only `count > 0`, which one added element guarantees"*.

**Predecessor exclusion is single-leaf only.** `docs/compiler/proof-engine.md:495`, verbatim: *"The narrowing is sound only when the reject guard reduces to a single trackable leaf. Multi-leaf conjunctions like `when A and B` carry the disjunctive negation `¬A ∨ ¬B`, which can't soundly narrow either field individually."* At the guard of row *j*, only the negations of rows strictly before *j* are available — never the negation of the guard being evaluated.

**The inspection carve-out.** Two provenance classes rest on facts that inspection does not make true, and this is the single sharpest correction in the document. `docs/runtime/evaluator.md:1485` declares `public EventInspection InspectFire(string eventName, JsonElement? args = null)` — **arguments are optional**. `:789-790`, verbatim: *"Inspect path uses EvaluateGuardProspect (Kleene ternary), not EvaluateGuard (bool). // Missing args → Unknown → propagates via Kleene truth table to Possible."* And `:817-820`, verbatim:

```
        // Guard passed or is ambiguous — simulate execution
        var workingCopy = version.Slots.ToArray();
        foreach (var action in row.Actions)
            ExecuteAction(action, workingCopy, args ?? FiredArgs.Empty);
```

So on an inspection with incomplete arguments the guard is *Possible*, not true, and the action chain executes anyway against empty arguments. A fired-guard fact is therefore **not earned** on inspection routes, and an ingress-governed argument fact is **not earned** either, because nothing entered. Both are carved out above. § 11 carries the two witnesses; § Open questions carries the cheaper alternative closure — rule that inspection with incomplete arguments is a caller error rather than a Kleene-supported path — which is a runtime-surface question this pass must not settle.

**The construction and restore carve-outs.** On a construction route, position zero is the hollow default configuration, and whether that configuration satisfies the declared constraints is *precisely the establishment obligation*. Consuming a declared constraint there would close a loop rather than ground it. On a restore route the position-zero configuration was never established by any obligation in the file at all: `spec § 0.7`, verbatim: *"Restored state is trusted as valid at the time it was persisted: hydration is fast and does not re-validate … Such state is reconstituted, not re-governed on load."* `docs/runtime/runtime-api.md:266` agrees — *"It accepts state name and field values separately and **recomputes computed fields**. It does **not** re-validate constraints."* The evaluator doc disagrees on the re-validation half (`:2146` — *"Restore bypasses access-mode checks but enforces constraint checks"*); the spec is the higher authority and the disagreement is routed. **What both agree on is that restore recomputes computed fields**, and the evaluator's own matrix says it does so *first*. So a computed-field expression on a restore route evaluates over host-supplied data with no ingress in front of it and no check upstream of it. Witness in § 11.

### 6. The order the rule is computed over — deliberately partial

Canon pins these orderings and no more:

| Pinned | Citation, verbatim |
|---|---|
| ingress precedes anything derived from an argument | `spec:1969` — *"as they enter, before the working copy derives any dependent value"* |
| a row's guard precedes that row's actions | `spec:1897` — *"Only the row that actually fires (or an explicit `reject` fallback) produces outcomes"* |
| actions in one chain are ordered | `spec:164` — *"Each assignment in a row sees the state left by all preceding assignments."* |
| recomputation is after mutations, before constraint evaluation | `evaluator.md:144` — *"After mutations and before constraint evaluation"* |
| constraint evaluation is after all mutations | `spec:1967` — *"Constraints are evaluated against the working copy after all mutations complete."* |

**Not pinned: where exit hooks, entry hooks and the `omit` reset sit relative to the row's action chain.** `spec:2117` places them — *"It has the same depth as event execution: guard evaluation, exit actions, mutations, entry actions, computed field recomputation, and constraint evaluation — all executed on a working copy without committing"* — but that sentence opens § 3A.6 *Inspection as a First-Class Operation* and asserts the ordering only through the phrase "the same depth as event execution". Both runtime docs enumerate the fire lifecycle with **no hook step at all**: `runtime-api.md:361`, verbatim — *"arg validation → row matching (first-match with guard evaluation) → action chain execution on working copy (mutation rows) or rejection (reject rows) → computed field recomputation → constraint evaluation (collect-all) → commit or discard."*

The design's treatment: **unordered constructs are may-precede in both directions for the kill set, and supply no ordering for the ordering premise.** That has two consequences, and they are opposite in sign:

- For **kill**, the rule is sound under every one of the three canon readings simultaneously, and pinning the order later only recovers precision.
- For **earn**, an unordered pair cannot satisfy `p ⪯ᵣ q`, so a fact earned from an unordered construct is **neither transportable nor killed — it simply does not discharge**, and the definition rejects.

This is why the phase-order silence is a *soundness* dependency rather than a precision question, and why § Open questions promotes it from optional to blocking. A previous framing of this design claimed the silence was free; the collection witness in § 11 falsifies that claim.

### 7. What kills — the write set and the read-closure

**Write constructs and their targets.**

| Construct | Targets | Ground |
|---|---|---|
| `set F = e`, `clear F` | `{F}` | `proof-engine.md:1251` — *"Full replacement (`set`/`clear` — `ReplacesValue` / `Empties` …): stamped onto `ProofObligation.ReassignedBefore`. Every guard fact about the field is invalidated"* |
| `dequeue C into F`, `pop C into F` | `{C, F}` — two targets | the grammar's action form names both |
| collection grow | `{C}` | catalog `ActionMeta.Effect = Grows` |
| collection shrink | `{C}` | `proof-engine.md:1253` — *"A shrink may reduce count to 0 … Upper-bound count facts (`count < N`) are preserved (a shrink only lowers count) and unaffected."* |
| computed-field recomputation | the computed fields whose transitive inputs were written — but killed **at the earliest input write**, not at recomputation | see below |
| the `omit` reset on state entry | every field omitted in the destination state, on any transition outcome including a self-transition | `spec:1074` |
| the residency coordinate | the pseudo-target "which state the entity is in", on any transition outcome that changes state | see below |
| the update patch | the whole writable field set for that state | `runtime-api.md:365-374` shows a two-field patch on both lanes |
| guards, conditions, interpolations, modifier expressions | nothing | `spec:170` expression purity |

**Two named over-approximations, both kill-direction only.** A guarded construct kills regardless of its guard. Several hooks on one state are unioned rather than ordered — I checked `spec` §§ 3A.1, 3A.4, 3A.6 and the state-hook grammar and **could not find** a statement that all hooks on a state fire, or in what order, so union is the reading that is sound under every possibility including "only some fire". Both mirror the graph analyzer's own discipline, which spec § 0.5 states as treating all edges as traversable regardless of guards.

**Computed fields are killed at the earliest write to any transitive input, not at recomputation.** *Rationale:* `evaluator.md:144` pins recomputation after mutations, so a computed slot holds its pre-operation value during the plan; killing at the input write is sound under both the lazy and an eager reading, killing at recomputation only under one. *Alternative rejected:* kill at recomputation — makes soundness contingent on an evaluator scheduling detail and licenses a mid-plan read of a stale computed value as if fresh. *Tradeoff:* precision loss on programs that read a computed field between an input write and the sweep.

**The read-closure `deps(φ)`** is the fields and coordinates the fact names, closed under: the matrix's three mention-set legs (§ The minting rule — condition, activation guard, and *"transitively through a computed field … following the dataflow all the way through chained computed fields"*); desugared cross-field modifiers (same section — *"`field Floor as decimal max Ceiling` desugars to a constraint mentioning **both** fields"*); collection contents, per the shape table below; and **the residency coordinate**, whenever the fact is a residency fact or a state-anchored constraint's truth.

The residency coordinate is an addition and it closes a real hole. A state change writes no field, so without it "the entity is in `S`" transports untouched into a `to T` hook and into the post-mutation sweep, where the entity is in `T`. That is an accept-when-must-reject produced by the rule as otherwise written.

**Kill semantics per fact shape.** The write set is field-indexed; fact *shapes* are not. **Any combination not in this table kills** — unknown means kill, the safe direction.

| Fact shape | vs a grow on C | vs a shrink on C | vs replace/empty on C |
|---|---|---|---|
| `C.count > n` (lower bound) | survives; a *must*-run, *must*-precede grow also establishes `count > 0` | **killed** | killed |
| `C.count < n` (upper bound) | **killed** | survives (`proof-engine.md:1253`) | killed |
| membership | survives | **killed** | killed |
| key presence | **killed** (a put may add the key) | **killed** | killed |
| element property from a quantifier | **killed** | survives | killed |
| anything about the whole value of C | killed | killed | killed |

### 8. Quantifier bodies

A quantifier predicate is evaluated once per element and the element count is data-dependent, so an occasion is not unique inside one. The resolution: a site inside *k* nested quantifier bodies mints **one obligation per (position, route), universally quantified over the *k* bindings.** Sound because `spec:160` makes a quantifier unfold to *"a finite conjunction or disjunction of its predicate: an acyclic expression tree, not a control-flow loop"*, and purity (`spec:170`) means every unfolding shares one fact environment — so a single quantified obligation discharges every occasion. The key stays `(position, route)`; the proof condition gains a universally quantified variable whose only fact source is the collection's inner-type modifiers, which is a provenance question and not a transport one.

Whether quantified constraints are proof surface at all is **inherited-open**, not settled here: the matrix, § Edge cells, records *"A constraint quantified over a collection | canon classifies quantifier predicates as runtime governance (`collection-types.md:796`) — conflicting with no-deferral"* with disposition **Conflicted → open**. The `quantifier-predicate` category is a live axis value with committed coordinates, so the definition owes it an index either way; that index is supplied above.

### 9. Cell keying

> **The obligation is keyed `(position, route)`. The cell is keyed by adding exactly one closed axis — the route class — to the existing fault coordinate. The concrete route is certificate payload, never a cell coordinate.**

A cell is a *schema*: the matrix, § The cell — *"**Obligation schema** — the site's proof condition with metavariables, under the stated normalization"*; and § Vocabulary — *"Two programs sit in the same cell iff — exactly when — their minted obligations unify with the same schema, differing only in how the placeholders are filled in."* A concrete route names one program's states, events and rows, so keying cells by it makes every cell program-specific, destroys schema matching, and makes the coordinate product unbounded — which § Storage forbids, since *"A family's full coordinate product — every combination the axes admit, each carrying a disposition … is produced by the same tooling that produces the cells, committed beside them, and read by the validator."*

This **overrides** the parent design's own fourth owed bullet (*"under the ruling the unit is the occasion, so keying is per (position × route)"*). The correction is stated here rather than applied silently.

Two further axes are rejected. A *phase* axis is a function of position — an exit-hook action operand is always in the hook segment — so storing it is a parallel copy of derivable data, which the catalog rule forbids. A *prefix profile* axis is not derivable from any abstraction of the route: whether an exit hook exists and what it writes is a property of the program. The prefix belongs in the **contract** instead — each fault cell's discharge contract is stated over a `mayWrite` metavariable, and each owes a near-miss witness exercising a non-empty intersection.

**Size and shape, measured.** Against the committed enumeration:

| | Today | With the route-class axis |
|---|---|---|
| Coordinate product | **9,292** = 101 requirement sites × 23 site categories × 4 type families | 101 × 23 × 8 × 4 = **74,336** written coordinates |
| Reachable after the § 3 relation | — | roughly 52,000, with about 22,000 pruned mechanically |
| `defined` cells | **992** | an upper bound of about 5,500, before collapse |
| `empty` / `open` / `deferred` / `conflicted` | 7,047 / 760 / 441 / 52 | scaled by the same relation |

**Honest caveats.** The upper bound is not a prediction: many defined contracts will be identical across route classes and collapse to one cell carrying a range, and the collapse is **not measured**. The mechanical prunings are exactly what § Storage anticipates — *"it is regenerated whenever an axis changes, including after the 2026-07-21 site-identity ruling re-keys the fault family's site axis"* — and each carries a generated one-line derivation, satisfying the written-pruning rule without authoring burden.

**The shape change matters more than the count.** The site-category axis is a free string today with no enum, so the scope check cannot be total. The § 3 relation replaces a flat product with a fixed relation computable from the parse tree, and closes both axes: 23 site categories by 8 route classes. An axis that could not be validated becomes one that can.

**Certificate payload.** A certificate records, per obligation, the route descriptor using the *stable* row identity, and the **ordered list of `(construct identity, write-target set)` pairs** the compiler used. The checker validates the transport premise by intersecting that list against the read-closure; it never re-derives routing, `from any` expansion, or hook attachment. Grounded in `what-i-want-2026-07-16.md:206`, verbatim: *"The certificate records the finished proof step by step, and the checker simply walks those steps and confirms each one … Every step is re-verified; the figuring-out is never repeated."*

### 10. The mid-plan reconciliation

**The locked write-plan definition is not touched, not narrowed and not reinterpreted.**

**The two sentences quantify over different objects.** The preservation obligation is a predicate over **configurations** — its domain is the reachable configuration set, and the locked entry says exactly why: § Vocabulary, Write plan, verbatim — *"no reader, no later handler, and no persistence ever sees a mid-handler state. A mid-handler state is not a reachable configuration, and the induction that premise (d) rests on ranges over reachable configurations only."* Correct, and untouched. A fault obligation is a predicate over **evaluation events**: `spec:110` quantifies over expressions — *"Every expression evaluates to a result — never silent `NaN`, `Infinity`, or `null`."* An evaluation event is not a configuration. Atomicity protects configurations; a discarded working copy does not un-divide by zero.

**The decisive independent ground is inspection.** `Inspect` commits nothing and still carries the full fault surface. `spec:2117`, verbatim: inspection *"has the same depth as event execution: guard evaluation, exit actions, mutations, entry actions, computed field recomputation, and constraint evaluation — all executed on a working copy without committing"*, and *"The inspection result matches what execution would produce for the same inputs."* If atomicity dissolved the mid-plan question, inspection would have no fault surface at all — which contradicts spec § 0.7's *"A precept that compiles without diagnostics cannot produce a runtime fault."* **This argument does not depend on reading the atomicity clause charitably**, because it is about an operation that promotes nothing.

**The seal — the mechanism, not the assertion.**

> A fault proof may cite a declared constraint or field modifier **only at route position zero**, under the citation duty below. It may never cite a constraint's truth at any later position. Every fact a fault proof holds mid-plan arrived there by the transport rule from position zero, or from a guard, an argument, a literal, or a callee attribute — and if any construct in the write set touched a field the fact mentions, the fact is dead.

> **Correction owed (owner, 2026-07-21) — do not apply this seal as written.** It treats a write through a governed entry point as an ordinary write, killing the fact that write just verified. `precept-language-spec.md § 0.7` settles otherwise: governance is enforced on *"event arguments, construction inputs, direct field edits"*, and the compiler's proof rests on the incoming check because it has proven that check sufficient. A governed write therefore re-establishes the field's declared facts at the position where it occurs, rather than killing them. See § Open questions, the entry replacing the position-zero seal question.

Three checkable consequences:

1. Position zero is the pre-state configuration on fire, update and stateless-handler routes, which is exactly the domain the locked induction licenses. Construction and restore are carved out (§ 5).
2. **Mid-plan positions have no constraint set at all.** There is no "the constraints holding at position four"; the rule never needs one and no derivation may manufacture one from the fact that the post-state will satisfy the constraint. That is the false-proof shape the matrix records at § The cell, foreclosed structurally rather than by policy.
3. **The post-plan constraint set is not a premise either.** A fault site in a rule or ensure condition is evaluated *by* the sweep; at that moment the working copy is a candidate, not a configuration, and assuming the constraints hold there is circular — it is what the sweep is deciding.

**The direction asymmetry, named so a reader does not conflate the indices.** Constraint obligations are computed **backward** — weakest precondition by backward substitution through the plan's writes in reverse, locked. Fault facts are computed **forward** — frame and kill along the route. Two directions over one operation, indexing two different predicates. The locked entry already anticipates the coexistence: *"Carrying a guard fact through the prior writes of a plan is a real derivation step (a certificate step, not a matrix column)."* This rule is what that step is indexed by.

**And the locked entry already delegates this question.** Same entry, verbatim: *"an expression later in a plan does see the results of earlier writes … and its own fault obligations are minted per evaluation site — that is a fault-family question about where values come from, not a decomposition of the preservation obligation."* This rule is that fault-family answer. Whole-plan granularity says nothing *owes* a constraint's truth mid-plan; the seal says nothing may *assume* it past a write to the mention set. The two are not merely compatible — the seal is what makes the atomicity concession safe.

### 11. Validity argument — route-indexed frame and kill

Required by the matrix, § Rule validity, which names *"a transport rule"* explicitly among the generative rules owing an argument, and whose gate reads: *"no cell ratifies whose derivation cites an argument-less rule."*

**Claim.** If the rule licenses `Γ ⊢ᵣ φ holds-at q`, then in every runtime execution belonging to route `r`, φ is true of the working copy when the evaluator reaches `q`.

**Step 1 — executions partition by route, and the descriptor is fixed before any fact is consumed.** Operation class, event and resident state come from the caller. Row selection is first-match over a statically declared list (`spec:1904`) and guard evaluation writes nothing (`spec:170`), so routing is decided against the pre-state. Outcome is syntactic once the row is fixed. Every execution belongs to exactly one route — *with one exception, stated rather than hidden*: `evaluator.md:2136` gives `InspectFire` the row-dispatch entry *"yes (all rows)"*, so a single inspection execution simulates every row in the group and therefore belongs to as many routes as the group has rows. That is sound for this rule provided the row simulations do not share a working copy. `evaluator.md:817` shows each row's simulation starting from `version.Slots.ToArray()` — a fresh copy per row — so they do not. The dependency is named because it is an implementation reading, not a canon statement, and it is routed.

**Step 2 — the write set over-approximates the writes actually performed between `p` and `q`.** Two parts. *Coverage*: every construct that can write on a route is in the route's construct list — the four surfaces `spec:1971` enumerates (*"event-driven transitions, stateless event hooks, direct field updates, and state entry/exit actions"*), plus default materialisation at construction, computed-field recomputation (`evaluator.md:144`), the `omit` reset (`spec:1074`), the update patch (`runtime-api.md:386`), and the residency coordinate. **This list's closure is not stated anywhere in canon** — `spec:1971` is a scope statement about the working-copy guarantee, not a writer enumeration, and it omits three of the writers just listed. Closure is therefore a named dependency and the design's residual soundness hole; it is routed as a one-sentence spec addition. *Ordering*: because unpinned orderings are may-precede in both directions, no actual write between `p` and `q` can be excluded on ordering grounds; excess membership costs precision only.

**Step 3 — each construct writes only its declared targets.** Every action statement names its target as a bare identifier; conditional expressions change the *value* written, never *which field* (`spec:162`). `spec:164` makes each chain a linear walk. The already-ratified *Establishment over defaults* argument uses the same step, verbatim: *"actions write only their target fields: a row is a flat assignment sequence."* The five non-action writers have their target sets enumerated in § 7.

**Step 4 — frame.** By steps 2 and 3, every member of the read-closure — field, collection, argument, or the residency coordinate — holds the same value at `q` as at `p` whenever the intersection is empty.

**Step 5 — the fact's truth is a function of its read-closure alone, and re-evaluation is stable.** Nothing outside the closure can move it. `spec:96` gives determinism; `spec:170` gives purity, so evaluating the fact disturbs nothing.

**Step 6 — the ordering premise is what makes step 4 about the right interval.** Without it, "between `p` and `q`" is not a well-formed interval and the frame claim is vacuous: a fact could be earned by a construct that runs *after* the consumption point. The premise is discharged only from the pinned order of § 6; an unordered pair yields no discharge and the definition rejects.

**Step 7 — the reject direction is safe by construction.** A non-empty intersection, or an undischargeable ordering premise, concludes nothing: the obligation falls to another licensed derivation or is undischarged, and the definition rejects. spec § 0.6 proof philosophy #1, verbatim: *"The proof layer must never claim an expression is safe when it is not … The language always chooses the safe direction."* **Only a wrong frame or a wrong earn can be unsound.** That is why every over-approximation in § 7 is free and every earn in § 5 is restricted.

**Step 8 — composition to fault-freedom.** Every execution is on at least one route; every evaluation event on it is an occasion of a site whose route set contains that route; the minting rule mints one obligation per (position, route), universally quantified over quantifier bindings; each is discharged; adequacy supplies that the stamped predicate makes the evaluator's fault unreachable. Hence no evaluation event faults.

**Lane independence.** The argument uses determinism, purity, target-naming and enumerability only — no arithmetic identity, no rounding, no exactness. It holds uniformly across `integer`, `decimal` and `number` and inherits none of the exclusions that block the approximate-lane arguments. Since every fault cell cites transport, this materially limits how far the number-lane question reaches.

**Named side conditions, stated inside the argument** — per the matrix, § Rule validity: *"Where an argument depends on something unbuilt or unsettled, the dependency is named inside the argument — an honest dependency is part of a valid argument, not a footnote to hide."*

| Silence | Where I looked | Conservative default while it stands |
|---|---|---|
| Where hooks and the `omit` reset sit relative to the row chain | `spec:2117`, `:1915`, `:1971`; `runtime-api.md:361`; `evaluator.md:144` | may-precede in both directions for kill; **no** ordering for earn — so it is a soundness dependency, not a precision one |
| Whether the in-operation writer list is closed | spec §§ 3A.1, 3A.4, 3A.5, 3A.6, § 0.4, § 0.6, § 0.7; `proof-engine.md` | **none available** — an unknown writer is unsound in the accept direction. The residual hole |
| Whether several hooks on one state all fire, and in what order | spec §§ 3A.1, 3A.4, 3A.6 and the state-hook grammar | union of all their targets |
| Whether entry hooks fire at construction, and on a self-transition | spec § 3A.5 (which names entry *ensures*, not hooks); `spec:1074` (which says the `omit` reset *does* apply to self-transitions) | include the route and the writes |
| Whether restore re-validates | `spec § 0.7`, `runtime-api.md:266`, `evaluator.md:2143`/`:2146` | follow the spec (trusted, not re-validated) and route the evaluator-doc drift |
| Whether abstraction is carried or re-run | — | abstraction re-runs at the consumption point; the rule transports concrete facts only. Without this the `nonzero`-abstracts-to-nothing behaviour at `proof-engine.md:606` compounds across positions |
| Concurrency and interleaving | grep of the spec for "concurren", "thread-saf", "interleav" — zero hits | stated as an assumption grounded in the immutable-version model, not derived |
| Whether an infeasible route may be pruned | the matrix, § Dead rows, which is scoped to *rows*: *"A transition row whose guard can never be true rejects the file"* | **no pruning is licensed.** An infeasible route mints and may reject. Over-minting is not free under the exact-power contract, so licensing a filter is routed, not assumed |

### 12. Decision procedure and cost

Given a fact, an earn point, a consumption point and a route: (1) look both points up in the route's construct list; (2) check the ordering premise against the pinned order; (3) enumerate the write set — one linear walk of a finite list, with each construct's targets read from catalog metadata rather than by switching on action identity; (4) compute the read-closure, which terminates because the computed-field dependency graph is acyclic by construction; (5) intersect. Non-empty means the fact is **dead**, not merely unproven, and the obligation falls through to rejection with no deferral. Cost is the number of constructs times the number of fields, per obligation, times the number of routes per site. No search, no fixpoint, no widening — which is what spec § 0.4 property 1 buys: *"Expression trees are finite and acyclic. This eliminates the need for fixpoint computation and widening operators."*

### 13. Soundness preservation claim

- **Principle 7 (compile-time-first static checking)** holds because every clause is a finite check with a named decision procedure and no deferral: an undischarged obligation rejects rather than being handed to a runtime test.
- **Principle 10 (totality)** holds *conditionally on the writer-closure dependency*. Given closure, a fact consumed at a fault site was true when the evaluator reached it, so the stamped predicate holds and no expression evaluates to a fault. Without closure the frame step is unproven and the claim cannot be made — this is stated as a hole rather than qualified into a claim that fits what is written.
- **Principle 11 (static completeness)** holds for the sites the committed axis covers. It **does not** hold for the five requirement kinds with no catalog declaration site, nor for the minting mechanisms outside the catalog walk, nor for overflow — all measured, all named in § Open questions. Declaring these "conservative boundaries" would qualify the principle to fit the build; under prevention an unprovable case is a rejection or an open hole, never a skipped one.

---

## Architecture Grounding

### Precept-internal placement

**Layer placement.** The rule is **prose in the matrix** (the definition layer). Its name and its declared scope are **structured data in the cell schema** (the checking layer). Neither is compiler code and neither is a C# catalog entry today. The reason is the matrix's own statement, § Rule validity: *"The certificate checker verifies that proofs *replay* the rules; nothing downstream verifies the rules themselves … The definition is therefore the trusted computing base, and it must argue its own soundness."* Putting the rule in compiler code makes the thing being justified also the justification. Putting it in a C# catalog is premature: § Storage already fixes the trigger — *"The generative layer enters the C# catalogs at constraint-obligation design time"* — and that machinery does not exist.

**The route relation is the exception, and it belongs in the catalog eventually.** The site-to-route relation of § 3 is exactly the kind of thing the catalog principle exists for: a fixed relation between two closed vocabularies that several consumers derive from. Today one of those vocabularies (the evaluation-site category) has no catalog anchor at all, which is why the relation is prose plus data here rather than a catalog entry. That is recorded, not hidden.

**Cross-component propagation**

- **Runtime (parser, type checker, evaluator, diagnostics):** None today. No obligation is added or removed and no diagnostic code changes; what changes is which discharges the *definition* licenses, which becomes a compiler change only when implementation resumes. One future item is named: diagnostics must gain route-qualified text, which is new message content on existing codes.
- **Tooling (syntax highlighting, completions, hover, semantic tokens):** None today. Later, hover's proof attribution would carry the route descriptor as its provenance string.
- **MCP (vocabulary, DTOs, tool output):** None. `precept_compile`'s obligation projection is unchanged by this design.
- **Matrix tooling and cell data:** Affected. The coordinate enumeration gains one axis and is regenerated; the cell schema's fault coordinate object gains a route-class field; the validator gains a relation-totality check.

**Breaking changes.** Two, both inside the definition's own artefacts and neither on a shipped public surface. (1) The fault coordinate object gains a route-class field, and the object is closed to additional properties, so this migrates every existing fault cell. (2) The certificate gains the route descriptor and the write-target list, which breaks replay of existing certificates — a major definition version.

### External architectural precedent

The architectural problem is: *how does a static analysis decide which facts are true at a program point, when the program point is reached by several different executions?* The field's answer is to make the analysis path-sensitive by partitioning executions and propagating a separate abstract state along each partition — and the in-tree survey documents two implementations of it.

**Frama-C EVA — split at the branch, propagate separately, join at the merge.** [`research/architecture/compiler/proof-engine-interval-arithmetic-survey.md:139-142`, verbatim: *"EVA computes an **abstract state** at each program point — a map from variables to abstract values (intervals, congruences). For sequential code: At an **assignment** `x = expr`, EVA evaluates the abstract value of `expr` in the current state, then updates the abstract state by binding `x` to that abstract value. At a **branch** (`if`/`else`), EVA splits the abstract state along the branch condition, propagates separately down each branch, then joins (takes the join in the interval lattice) at the merge point."*]

**Astrée — the same discipline, stated as a soundness requirement.** [same survey, `:230-233`, verbatim: *"Like all abstract interpretation tools, Astrée propagates an **abstract state** forward through the program: At each assignment, the abstract value of the right-hand side is evaluated in the current abstract state, and the variable is updated. At each branch, the state is split, propagated along each path, and joined at merge points. All data pointers and function pointers are resolved automatically; the soundness requirement means all potential targets must be considered."*]

**What Precept takes.** The forward abstract-state discipline exactly: evaluate in the current state, update at each write, and — the load-bearing part — *split at the branch and keep the paths separate*. A route **is** the split. The final clause of the Astrée excerpt is also taken verbatim in spirit: *all potential targets must be considered* is what § 7's may-write over-approximation implements, including for guarded hooks whose guard the descriptor deliberately does not carry.

**Where Precept diverges, and why it is allowed to.** These analyzers **join** at the merge point — they must, because a loop or a reconverging branch would otherwise blow the state count up without bound. Precept never joins. spec § 0.4 property 3, verbatim: *"Because there are no loops or branches, there is no join point where two different states must be merged."* So the route partition is complete rather than an approximation of a partition, and there is no widening operator anywhere in the rule. That is the whole reason a partition-per-route is affordable here and is not affordable in a C analyzer: the number of partitions is a bounded product over declared rows and states, not a function of program paths.

**The alternative the field also offers, and why it is rejected.** CBMC's answer is to eliminate the multiplicity by *encoding* it: [same survey, `:479-489`, verbatim: *"**SSA (Static Single Assignment) transformation**: the program is converted to SSA form, creating a fresh variable for each assignment … For branching: both branches are encoded, with a conditional guard … All paths through the program are encoded simultaneously in the formula; the SAT solver searches for a satisfying assignment representing a concrete execution path that violates the property."*] That is the "desugar the routes away" shape. It is rejected here for the reason the excerpt itself makes plain — it terminates in a SAT/SMT solver, and the resulting witness is a satisfying assignment rather than a walkable derivation. spec § 0.6 proof philosophy #3 permits a solver only when *"authority rests in the certificate, not the search"*, and Precept's certificate must be a step list a small checker replays (`what-i-want-2026-07-16.md:206`). A route-partitioned frame-and-kill pass produces that list directly.

**One nearby technique this design reuses rather than reinvents.** [`research/architecture/compiler/solver-free-static-analysis-techniques-survey.md` § Family C, verbatim: *"**Dominator-based fact propagation (reuse graph-analyzer dominators).** The graph analyzer already computes dominators … A fact established on every path that dominates a use-site is true at the use-site — so dominator information could license propagating a guard/ensure fact forward to dominated rows without re-proving."*] That is the *cross-route* generalisation of this rule and is deliberately out of scope here: it would let a fact earned on every route reaching a site be used once rather than per route. It is the natural power-widening once the per-route rule is settled, and it is named so it is not re-derived from scratch later.

**A named research gap.** The field's own name for what § 1 does is **trace partitioning** (Rival and Mauborgne's abstract domain, the technique Astrée uses to keep paths separate where a plain join would lose the property). **No in-tree research file covers it**, and this pass did not read the primary source, so no verbatim excerpt from it appears anywhere in this document and none should be inferred. Filling that gap is a `/research` task and is listed under § Dependencies.

---

## Inventory of what will be built

Nothing in this inventory is code in `src/`.

| Artefact | Change |
|---|---|
| `docs/Working/obligation-discharge-matrix-2026-07-19.md` § Rule validity | One new named argument — *route-indexed frame and kill* — with its decision procedure and its named side conditions |
| same, § Vocabulary | A definition entry for "route", stated as a static equivalence class of occasions; a cross-reference from the write-plan entry to the fault-family answer it already delegates |
| same, § Per-family case shapes, fault row | The route axis added to the fault case shape's axis list — **owner's edit**, flagged not made |
| `…-cells/cell.schema.json` | Fault coordinate object gains a closed route-class field; contract entries gain a `mayWrite` metavariable slot; the validity-argument registry gains the new name |
| `…-cells/fault-axis-disposition-map.csv` + meta | Regenerated over the new axis, with generated pruning derivations for the mechanically-unreachable combinations |
| `tools/Precept.MatrixTools` + tests | A relation-totality check (every site category has a written route set); a route-class validity check; a near-miss-with-non-empty-intersection requirement per fault cell |
| `docs/compiler/proof-engine.md` | Doc-drift note against `:606`'s "genuinely lies within these bounds" once the field-modifier question is ruled |
| `docs/language/precept-language-spec.md` § 3A | One normative sentence closing the in-operation writer list — **owner-only**, and the design's residual soundness dependency |

---

## Decisions

### Decision 1: A route is a static descriptor of an operation class, and route identity is descriptor equality

**Stakes**: high

- **Rationale**: The owner ruled the site is an *occasion*, which is a runtime notion; a compiler cannot enumerate runtime occasions. The descriptor is the smallest static object that determines which sites are evaluated and which constructs may write, and it is built only from things the language already declares statically — operation class, state, event, row, outcome, writable field set. Everything that could branch beyond those is either fixed by the descriptor or over-approximated in the kill direction.
- **Tradeoff accepted**: A route is *coarser* than an occasion. Two occasions on the same route with different pre-state values are one obligation, so a discharge that would need the pre-state interval to differ between them is refused. § 11's fan-in witness shows a shape where that coarseness may bind.
- **Alternatives considered**:
  - *Reuse the desugared row as the route.* Rejected: it yields **zero** routes for a stateless or data-only precept, so a division in a `rule` condition would mint nothing and be vacuously discharged. The stateless sample in § 2 is the counterexample.
  - *Desugar routes away by expanding to flat traces.* Rejected: this is CBMC's shape (see § Architecture Grounding) and it is exponential with no cap, which spec § 0.6 proof philosophy #3 forbids — *"where a procedure could blow up it hits a hard cap"*.
  - *Carry hook guards in the descriptor so the checker can recompute the trace.* Rejected as stated, but its motive is honoured: the checker needs the *trace*, not the ability to re-derive it, so the trace goes in the certificate (Decision 6) instead of doubling the descriptor per declared guard.
- **Precedent**: forward abstract interpreters split the abstract state at each branch and propagate the paths separately — `research/architecture/compiler/proof-engine-interval-arithmetic-survey.md:232`, verbatim: *"At each branch, the state is split, propagated along each path, and joined at merge points."* Precept-internal precedent: the language already resolves multi-source rows by producing distinct proof units — a comma-delimited state target desugars to one independent row per named state.
- **Sources consulted for this decision**:
  - `docs/runtime/evaluator.md:2133-2143` — *"| `InspectFire` | no | yes (all rows) | same as `Fire`, but evaluated for every row |"* and *"| `Restore` | no (bypassed) | no | `always`, `in <current>` — computed fields recomputed first |"*
  - `docs/language/precept-language-spec.md:1904` — *"Transition rows are evaluated in declaration order — the first matching guard wins, and remaining rows are not evaluated."*
  - `docs/language/precept-language-spec.md:1915` — *"This is a deliberate design allowing in-place data changes to be event-driven without triggering entry/exit actions."*
  - `docs/language/precept-language-spec.md:168` — *"Every transition row, state action, and rule can be enumerated exhaustively."*
  - `docs/language/precept-language-spec.md:162` — *"There are no `if` statements that split execution into paths that later reconverge."*
  - matrix § The minting rule — *"One written expression reached by several routes mints one fault obligation *per route*, each proved from the facts that route establishes."*
  - **Spec-first check**: grepped `precept-language-spec.md`, `proof-engine.md`, `type-checker.md` and the matrix for a prior definition of "route" or any path-to-a-site construct. "Routing" appears only as *"`when` guards are routing logic, not constraints"* (`:1897`) and in the first-match sentence. **The spec is silent on what a route is**; this decision derives it from the execution model rather than inventing it.
- **Strongest counter-evidence**: the owner's ruling text says *occasion*, and a static equivalence class of occasions is my refinement of that word, not the word itself. If the intended granularity is finer — per route *and* pre-state interval, which is what the fan-in witness's second discharge would need — this is the wrong shape, not a coarser version of the right one. I looked for a finer static unit that is still enumerable and found none; the finer unit is data-dependent.
- **Reversibility**: `Hard`. Every fault cell's coordinate and every certificate's descriptor carry it.
- **Blast radius**: the whole fault coordinate enumeration; the cell schema; the certificate format; the matrix's vocabulary. No `src/` change, no diagnostic code, no MCP surface.

### Decision 2: The transport rule carries an explicit ordering premise, over a deliberately partial order

**Stakes**: high

- **Rationale**: A kill set is not an ordering. Written without an ordering premise, the rule licenses a fact earned by a construct that runs *after* the consumption point, and § 11's collection and guard-internal witnesses both exploit exactly that. Making the order *partial* rather than total is what lets the rule be sound under all three canon readings of the operation sequence at once — but the price is that an unordered pair supplies no earn, so the phase-order silence becomes a soundness dependency rather than a precision question. Both halves are stated; a previous framing claimed the silence was free.
- **Tradeoff accepted**: Programs whose fact and consumption sit on either side of an unpinned boundary do not discharge at all, even where they would be safe under every reading. The cure is a canon sentence pinning the order, which is routed.
- **Alternatives considered**:
  - *A total order read off `spec:2117`.* Rejected as the sole basis: that sentence opens the inspection section and asserts the sequence only through "the same depth as event execution", while both runtime docs enumerate the fire lifecycle with no hook step at all. Building 100% of fault certificates on one disputed sentence makes them hostage to it.
  - *No ordering premise, kill set only.* Rejected — it is unsound, with two witnesses.
  - *Pin the order in this design pass.* Rejected: it is a canon addition, not a design-pass call, and it is routed.
- **Precedent**: the sequential-flow commitment already in canon is a *forward* discipline — spec § 0.6 item 7, verbatim: *"Actions in a chain are sequenced — each subsequent action sees the proof state left by all preceding actions."* The rule generalises "preceding" from the chain to the route; what it must not do is drop "preceding".
- **Sources consulted for this decision**:
  - `docs/language/precept-language-spec.md:2117` — *"guard evaluation, exit actions, mutations, entry actions, computed field recomputation, and constraint evaluation — all executed on a working copy without committing."*
  - `docs/runtime/runtime-api.md:361` — *"arg validation → row matching (first-match with guard evaluation) → action chain execution on working copy … → computed field recomputation → constraint evaluation (collect-all) → commit or discard."*
  - `docs/runtime/evaluator.md:144` — *"**Computed field recomputation** | After mutations and before constraint evaluation."*
  - `docs/language/precept-language-spec.md:164` — *"Each assignment in a row sees the state left by all preceding assignments."*
  - `docs/language/precept-language-spec.md:1967` — *"Constraints are evaluated against the working copy after all mutations complete."*
  - **Spec-first check**: grepped the spec and `proof-engine.md` for a normative execution-order section. § 0.6 item 7 settles ordering *within a chain*; `:2117` describes inspection; **no section states the operation's phase order normatively**. The spec is silent, and the silence is routed rather than resolved here.
- **Strongest counter-evidence**: the parent design's preserved Decision-3 text argues the total order directly from `spec:2117` and calls it *"the single most valuable thing this design fixes"*. Response: I agree the index was wrong and that the fix is valuable; I disagree that one sentence in the inspection section is enough foundation for a universal dependency, and the partial order delivers the same soundness without that bet.
- **Reversibility**: `Easy` in one direction — pinning the order later is a pure power-widening that keeps everything previously licensed. `Hard` in the other.
- **Blast radius**: every fault cell's transport citation; the matrix's argument section; no `src/` change.

### Decision 3: Earning a fact is restricted per route class, with carve-outs for inspection, construction and restore

**Stakes**: high

- **Rationale**: A guard fact is only earned where reaching the site *implies the guard was true*, and an argument fact is only earned where ingress governance *ran*. Neither holds on inspection routes: arguments are optional on the inspection entry point, a missing argument makes the guard *Possible* rather than true, and the action chain executes anyway. Construction's position-zero configuration is the very thing the establishment obligation is about, so consuming a declared constraint there closes a loop. Restore's position-zero configuration was never established by any obligation in the file. Without these four carve-outs the rule licenses programs that fault, with three witnesses in § 11.
- **Tradeoff accepted**: This is the largest narrowing in the document. Inspection routes reach almost every site category, so a fault site whose only fact is a guard or an argument constraint now has at least one route on which it cannot discharge. Restore routes make every computed field containing a division unprovable. Both consequences are real rejections of programs that compile today, and both are **routed rather than settled** under the matrix's amendment rules.
- **Alternatives considered**:
  - *Leave the classes route-blind.* Rejected: unsound, with compiled witnesses.
  - *Rule that inspection with incomplete arguments is a caller error rather than a supported path.* Not rejected — it is strictly cheaper than route-restricting the two classes, it removes the problem at the source, and it is a runtime-surface question a design pass must not settle. Routed as a neutral option.
  - *Drop restore from the route set entirely.* Not rejected — `spec § 0.7` arguably already puts it outside the guarantee. Routed as a neutral option, because "include more sites" is **not** the conservative direction under an exact-power contract: over-minting produces false rejections, which are nonconforming in the other direction.
- **Precedent**: the matrix already treats a door where a value enters as the thing that *makes* a premise true rather than as a standing fact — § Vocabulary, verbatim: *"it is the mechanism that *makes* premise (b) true, and by the same symmetry the editable-field door makes premises (a) and (d) true for that field downstream."* If the door is what makes the fact true, a route on which the door did not run does not have the fact. This decision is that sentence applied per route.
- **Sources consulted for this decision**:
  - `docs/runtime/evaluator.md:1485` — `public EventInspection InspectFire(string eventName, JsonElement? args = null)`
  - `docs/runtime/evaluator.md:789-790` — *"Inspect path uses EvaluateGuardProspect (Kleene ternary), not EvaluateGuard (bool). // Missing args → Unknown → propagates via Kleene truth table to Possible."*
  - `docs/runtime/evaluator.md:817-820` — *"// Guard passed or is ambiguous — simulate execution … foreach (var action in row.Actions) ExecuteAction(action, workingCopy, args ?? FiredArgs.Empty);"*
  - `docs/language/precept-language-spec.md:1897` — *"guards select; constraints enforce."* (true on fire, and the sentence is about routing surfaces, not about inspection)
  - `docs/language/precept-language-spec.md § 0.7` — *"Restored state is trusted as valid at the time it was persisted: hydration is fast and does not re-validate … the runtime fault traps backstop any out-of-contract value the evaluator would otherwise fault on."*
  - `docs/runtime/runtime-api.md:266` — *"It … **recomputes computed fields**. It does **not** re-validate constraints."*
  - `docs/runtime/evaluator.md:2143`, `:2146` — *"`always`, `in <current>` — computed fields recomputed first"*; *"Restore bypasses access-mode checks but enforces constraint checks"*
  - matrix § The cell, owner ruling 2026-07-21 — *"a proof may not consume such a fact unless it **names the obligation that established the fact and every obligation that preserves it**, each itself discharged … class (a) is not exempt from it"*
  - **Spec-first check**: grepped the spec for whether inspection's fault surface differs from execution's. `spec:2119` says *"The inspection result matches what execution would produce for the same inputs"* — which does not reach a call with **no** inputs. **Spec is silent on the incomplete-argument inspection path**; the runtime doc is the only source, and it is quoted rather than paraphrased.
- **Strongest counter-evidence**: `spec:2119`'s equivalence sentence, read strongly, says inspection cannot have a wider fault surface than execution. Response: it is conditioned on *"for the same inputs"*, and the hole is precisely a call with fewer inputs. If the owner closes the hole at the runtime surface instead, this carve-out becomes unnecessary — which is why it is routed as a two-option fork rather than settled.
- **Reversibility**: `Hard`. Every guard-sourced and argument-sourced fault cell gains a route restriction; reversing it re-opens them.
- **Blast radius**: the majority of fault cells (guard and argument facts are the family's dominant premises); the route relation; the matrix's argument section; a possible runtime-API surface question if the alternative closure is chosen.

### Decision 4: Kill on may-write; earn on must-run and must-precede

**Stakes**: medium

- **Rationale**: Over-approximation is safe in exactly one direction. A guarded construct that may have run must be in the kill set, and the same construct must not establish anything. Stated once as a general rule, it removes a whole class of ad-hoc reasoning at individual sites — and it is the difference between the collection-growth provenance being safe and being a new false-proof shape.
- **Tradeoff accepted**: Precision loss wherever a guarded write is in fact the only write; the author's respelling is an unguarded write or a guard at the reading site.
- **Alternatives considered**: *Apply may-write symmetrically to earn and kill.* Rejected — a guarded grow that may not have run would establish `count > 0`. *Track guard feasibility to decide must-run.* Rejected here: it needs a satisfiability scan per construct pair, and the matrix's dead-row scan is scoped to rows, so licensing it is a separate widening.
- **Precedent**: canon already draws exactly this asymmetry for collections — `spec:258`, verbatim: *"Both directions are *sound by construction* (invalidation paths are pure rejections; grow-establishment proves only `count > 0`, which one added element guarantees)."* And the graph analyzer's edge over-approximation is the same move on the structural side.
- **Sources consulted for this decision**:
  - `docs/compiler/proof-engine.md:1251` — *"Every guard fact about the field is invalidated for obligations after the write — `when X != 0 → set X = 0 → 100 / X` no longer discharges divisor safety."*
  - `docs/compiler/proof-engine.md:1253` — *"A shrink may reduce count to 0 … Upper-bound count facts (`count < N`) are preserved (a shrink only lowers count) and unaffected."*
  - `docs/language/precept-language-spec.md § 0.6` proof philosophy #1 — *"The language always chooses the safe direction."*
  - **Spec-first check**: grepped the spec and `proof-engine.md` for a prior statement of the asymmetry. § 0.6's sequential-proof-flow paragraph states it for the collection case only; **the spec is silent on the general rule**, which this decision states.
- **Strongest counter-evidence**: none found after checking `proof-engine.md` §§ Strategy 2 and 3, the sequential-flow paragraphs of spec § 0.6, and the matrix's validity arguments. The asymmetry is uncontested wherever canon addresses it.

### Decision 5: The obligation is keyed by position and route; the cell gains exactly one axis, the route class

**Stakes**: high

- **Rationale**: A cell is a schema over metavariables, so a concrete route — which names one program's states, events and rows — cannot be a coordinate without making every cell program-specific and the product unbounded. But the route *class* is a closed vocabulary drawn from the runtime's operation table, so it is a legitimate axis, and adding it turns a site axis that could not be validated into a fixed relation that can.
- **Tradeoff accepted**: The written coordinate product grows roughly eightfold, and the artefact was already described by its own storage ruling as *"large and mostly uninteresting"*. Most of the growth is mechanically-pruned rows carrying generated one-line derivations.
- **Alternatives considered**:
  - *Key cells by the concrete route.* Rejected — destroys schema matching, per the excerpt above.
  - *Add a phase axis.* Rejected — phase is a function of position, so storing it is a parallel copy of derivable data, which the catalog rule forbids and which the validator would then have to cross-check.
  - *Add a prefix-profile axis.* Rejected — whether an exit hook exists and what it writes is a property of the program, not of any abstraction of the route. The prefix belongs in the contract's `mayWrite` metavariable and in each cell's near-miss witness.
- **Precedent**: the matrix's own axis discipline — § Axes: *"Each axis names its source. No axis is induced from an example."* The route class names its source (the runtime's operation table); a concrete route would be induced from examples.
- **Sources consulted for this decision**:
  - matrix § The cell — *"**Obligation schema** — the site's proof condition with metavariables, under the stated normalization."*
  - matrix § Vocabulary — *"Two programs sit in the same cell iff — exactly when — their minted obligations unify with the same schema."*
  - matrix § Storage — *"A family's full coordinate product — every combination the axes admit, each carrying a disposition, including the ones that need no content — is produced by the same tooling that produces the cells"*, and *"it is regenerated whenever an axis changes, including after the 2026-07-21 site-identity ruling re-keys the fault family's site axis."*
  - `docs/Working/…-cells/fault-axis-disposition-meta.json` — `"productSize": 9292`, `"proofRequirementSites": 101`, `"evaluationSiteCategories": 23`, `"defined": 992`, `"empty": 7047`, `"open": 760`, `"deferred": 441`, `"conflicted": 52`
  - `docs/Working/fault-family-validity-arguments-2026-07-21.md` § Semantic Rules Rule 3 — *"The cell coordinate follows. Cells are keyed by syntactic position today; under the ruling the unit is the occasion, so keying is per (position × route)."* — the bullet this decision overrides
  - **Spec-first check**: the cell keying is matrix-owned, not spec-owned; grepped `precept-language-spec.md` and `proof-engine.md` for cell or coordinate keying and found nothing on point. **Spec is silent; the matrix is the authority and is quoted.**
- **Strongest counter-evidence**: the parent design's own owed bullet says `position × route`. Response: it is wrong for the schema reason, and the correction is stated here rather than applied silently — which is itself the honest handling, since the parent design is a peer artefact and not an owner ruling.
- **Reversibility**: `Hard`. Regenerating the enumeration is bulk work in both directions.
- **Blast radius**: every fault cell; the cell schema; the coordinate enumeration; the matrix-tools validator.

### Decision 6: The certificate carries the route descriptor and the ordered write-target list

**Stakes**: medium

- **Rationale**: The checker must validate the transport premise without re-deriving routing, `from any` expansion, or hook attachment — otherwise "the figuring-out" is repeated at every replay, and two conforming compilers can disagree on replay because of desugaring order. Carrying the trace makes replay a walk. Using a *stable* row identity rather than a positional index means inserting a row above another does not silently re-bind an existing certificate.
- **Tradeoff accepted**: Certificates grow by a class tag, two names, a row identity and a list of write-target sets, and replay of existing certificates breaks.
- **Alternatives considered**: *Let the checker re-derive the trace from the descriptor.* Rejected: it requires the descriptor to carry hook guards, which is exponential in declared guards. *Carry only the descriptor and trust the compiler's trace.* Rejected: that is the producer's authority, which the certifying-algorithm discipline exists to remove.
- **Precedent**: `what-i-want-2026-07-16.md:206`, verbatim: *"The certificate records the finished proof step by step, and the checker simply walks those steps and confirms each one … Every step is re-verified; the figuring-out is never repeated. That's why checking stays fast and the checker stays small."*
- **Sources consulted for this decision**:
  - `docs/Working/what-i-want-2026-07-16.md:206` — as quoted above
  - `docs/Working/what-i-want-2026-07-16.md:199` — *"the compiler emits a **certificate**: a checkable record of the proof — per obligation, the theorem, the premises used (which guard, which arg constraint, which pre-state rule), and the derivation."*
  - matrix § The cell — *"the cell triple is the certificate's *schema and index* … The matrix must never be read as fixing the certificate's payload granularity."*
  - **Spec-first check**: grepped `precept-language-spec.md` and `proof-engine.md` for certificate payload requirements. § 0.6 proof philosophy #3 requires a certificate *"drawn from a small, spec-enumerated vocabulary (the `CertificateSteps` catalog) that an independent checker can replay"* but does not enumerate its fields. **Spec is silent on the payload; the want doc is the authority and is quoted.** Pinning the transport step into the closed step-kind vocabulary requires reading `certificate-steps-membership-2026-07-12.md`, which this pass did not read — recorded as a dependency, not assumed.

### Decision 7: A site reached by no route is a definition error, and the stateless and unmatched classes are enumerated so that never fires spuriously

**Stakes**: medium

- **Rationale**: Under a route-indexed rule, an empty route set makes every obligation at that site vacuously discharged — the exact silent-pass shape the whole definition exists to prevent. Treating it as an error makes the failure loud. The two enumeration rules that go with it exist so the error means what it says: a stateless precept must produce fire-class routes, and a group whose guards can all fail must produce the unmatched class.
- **Tradeoff accepted**: An infeasible-but-written route now mints obligations that may reject, because no feasibility filter is licensed. Over-minting is not free; that is why the filter is routed rather than assumed.
- **Alternatives considered**: *Treat an empty route set as a discharge.* Rejected — silent pass. *Treat it as a warning.* Rejected — the matrix's dead-row ruling is explicit that warning severity creates *"exactly the two-tier trustworthiness the want doc forbids."*
- **Precedent**: matrix § Dead rows are an error, verbatim: *"A transition row whose guard can never be true **rejects the file**. It is an error, not a warning … a definition that asserts something false about itself does not ship."* A site no execution reaches is the same shape one level down.
- **Sources consulted for this decision**:
  - matrix § Dead rows are an error — as quoted
  - matrix § The cell — *"Pruned cells are written, not absent — an incorrectly-pruned cell must be findable, never a silent gap wearing an 'empty' badge."*
  - `docs/language/precept-language-spec.md:1918`, `:1919` — the unmatched-routed-event and undefined-event-surface outcomes
  - `docs/language/precept-language-spec.md:1007` — the stateless `on Event -> …` handler form
  - `docs/philosophy.md` — *"States are optional. … a stateless precept provides the same field declarations, rules, and constraint enforcement without a state machine. This is not a secondary capability."*
  - **Spec-first check**: grepped the spec for whether an unreachable *expression* (as opposed to state or row) is an error. § 0.5 covers unreachable states and dead rows; **nothing covers an unreachable evaluation site**, because before the occasion ruling there was no such notion. Genuinely new ground.

---

## Falsifiers

Guard 7 applies: the design changes which programs compile, which external authors see.

1. **If a route-qualified diagnostic for a four-route residency ensure cannot be written in under six lines that a business analyst can act on**, the per-route obligation unit is the wrong author-facing granularity and the diagnostic should aggregate to "provable on some routes, not others" with the failing routes listed separately.
2. **If, after the route relation is written, more than one in five fault-prone programs in `samples/` loses a discharge it has today with no licensed respelling**, the earn restrictions are over-strict and the inspection carve-out should be closed at the runtime surface instead (the alternative option already routed).
3. **If the collapse measurement shows fewer than half of today's 992 defined cells collapse to a single route-class range**, the route class is doing too much work as a cell axis and the contract should carry it as a metavariable the way the write set does.
4. **If a fault cell can be constructed that satisfies all four premises on its route and still licenses a runtime fault**, the writer-closure dependency has bitten and the rule's soundness argument is void until canon closes the list.
5. **If pinning the operation's phase order turns out to *reject* something the partial order accepts** — rather than only accepting more — then the partial order was not the conservative reading and Decision 2's "widening in one direction" claim is false.

---

## Acceptance criteria

Test-shaped. "Cell validator" means the day-one validator the matrix's § Storage specifies.

1. **The discriminating pair.** The exit-hook program and its `no transition` mirror (§ 11) are compiled and verdicted from diagnostic codes and obligation records. Under the design the first **rejects** naming the route and the second **accepts**. *Today both accept identically with `strategy: GuardInPath` — recorded as observed in this pass, and the criterion is the conformance test any implementation must pass.*
2. **Route relation totality.** A test asserts every one of the twenty-three evaluation-site categories in `fault-axis-disposition-map.csv` has a written route set, and that every route class named is drawn from the closed operation vocabulary. Adding a twenty-fourth category **fails** until it is given a route set.
3. **Empty route set rejects.** A fixture whose site is reached by no route **fails** validation with a pruning derivation rather than passing silently. A stateless fixture with a division in an `on Event` chain **passes** — i.e. it does not trip criterion 3.
4. **The ordering premise is exercised.** A fixture cell whose fact is earned by a construct unordered relative to the consumption point **fails** to discharge. The collection witness in § 11 is that fixture.
5. **The inspection carve-out is exercised.** A fixture cell citing a fired-guard fact with an inspection route in its route set **fails** validation. The same cell with the inspection route excluded **passes**.
6. **Kill-set completeness per witness.** Every fault cell carries a near-miss whose intersection with the write set is **non-empty**, so the kill half of the rule is exercised and not merely asserted.
7. **Certificate replay does not re-derive routing.** A checker fixture is given a certificate whose write-target list disagrees with the program's actual hook attachment; the checker **rejects** it rather than recomputing and agreeing.
8. **Coordinate regeneration.** The enumeration is regenerated over the new axis and the validator reports disposition totality over the enlarged product, with every mechanically-pruned coordinate carrying its generated one-line derivation.
9. **Doc-sync.** The matrix's § Rule validity carries the new argument with its named side conditions; the write-plan entry carries a cross-reference to it; a test asserts the argument name appears in both the prose and the schema registry.
10. **Respellability is measured, not asserted** — per the matrix's hard gate: *"Paper-only verdicts do not ratify."* The earn restrictions' cost is classified against the sample corpus before any affected family ratifies.

---

## Dependencies

**Upstream — must be in place first**

- Owner rulings on the four blocking items in § Open questions: the phase order, the inspection carve-out (or its runtime-surface alternative), the restore disposition, and the writer-closure sentence. The first and fourth are soundness dependencies of the rule itself; the second and third are narrowings with witnesses.
- The in-plan substitution provenance ruling. Without it the ruling's own motivating example is not constructible (§ 11), which makes this arguably a prerequisite to the restatement rather than a follow-on.
- Reading `certificate-steps-membership-2026-07-12.md` before the transport step is pinned into the closed certificate vocabulary. **Not read in this pass** — the largest unread dependency here.
- A `/research` pass on trace partitioning (Rival and Mauborgne's abstract domain), for which no in-tree research file exists.

**Downstream — what this enables**

- Every fault cell's transport citation, which is currently resting on a rule the owner's ruling invalidated.
- The coordinate regeneration the matrix's § Storage already ruled, which needs the new axis before it can run.
- The cross-route generalisation (dominator-based propagation), which is the natural power-widening once the per-route rule is settled.

---

## Doc-update enumeration

Per the CLAUDE.md routing table.

| Doc | What changes |
|---|---|
| `docs/Working/obligation-discharge-matrix-2026-07-19.md` § Rule validity | The *route-indexed frame and kill* argument, with its decision procedure and named side conditions |
| same, § Vocabulary | A "route" entry; a cross-reference from the write-plan entry to the fault-family answer it delegates |
| same, § Per-family case shapes (fault row) | The route axis added to the fault case shape — **owner's edit**, flagged not made |
| same, § Validity arguments (*Guard normal-form match*) | The single-write clause corrected, **only if the owner accepts the soundness correction** — routed, not made |
| `docs/Working/…-cells/cell.schema.json` | Route-class coordinate; `mayWrite` metavariable slot; argument-registry entry |
| `docs/Working/…-cells/fault-axis-disposition-map.csv` + meta | Regenerated over the new axis |
| `docs/compiler/proof-engine.md` § Strategy 2 / § Sequential proof flow | Cross-reference to the route-indexed rule; the `:606` doc-drift note once the field-modifier question is ruled |
| `docs/language/precept-language-spec.md` § 3A | One normative sentence closing the in-operation writer list, and (if ruled) a normative execution-order section — **owner-only** |
| `docs/runtime/evaluator.md` § Constraint Evaluation Matrix | Reconcile `:2146`'s "enforces constraint checks" with spec § 0.7 and `runtime-api.md:266` |
| `docs/runtime/runtime-api.md` § Inspect | If the inspection carve-out is closed at the runtime surface, state that incomplete arguments are a caller error |
| `docs/compiler/diagnostic-system.md` | Route-qualified diagnostic wording for the affected codes |
| `tools/Precept.MatrixTools` + `test/Precept.MatrixTools.Tests/` | Relation-totality, route-class validity, non-empty-intersection near-miss checks |

---

## Operational dimensions

**Security** — N/A. No source-text ingestion surface changes; no lexer, parser or MCP input path is touched.

**Observability** — Affected. The route descriptor is the natural provenance string for proof attribution, which spec § 0.1 #4 requires: *"proven ranges, source attribution, and what the engine could not prove must all be surfaceable through diagnostics, hover, and tooling."* This design makes the string exist and does not ship the surfacing. It also makes route-qualified diagnostics a prerequisite rather than polish, because an unqualified message under route keying is false rather than merely terse.

**Evolvability** — N/A on external standards. The rule depends on no external standard: it uses determinism, purity, target-naming and enumerability only. It does depend on two *internal* surfaces that may move — the runtime's operation table and the access-mode composition rules — and both are cited by section so a change is findable.

---

## Worked failures — the behavioural claims, with compiled samples

Every result below was produced by `precept_compile` in this pass at HEAD on 2026-07-21 and is an **observation of current behaviour, never design authority**.

### The exit-hook kill — designed reject, current accept

```precept
precept ExitHookTransportNoModifier

field Divisor as integer default 5
field Slots as integer default 0

state Warm initial
state Cold terminal

event Open initial
event Go

on Open
    -> set Divisor = 5

from Warm -> set Divisor = 0

from Warm on Go when Divisor != 0
    -> set Slots = 100 / Divisor
    -> transition Cold
```

*Today:* `success: true`, zero diagnostics, `{"kind":"Numeric","disposition":"Proved","strategy":"GuardInPath","description":"Divisor must be non-zero"}` — an accepted program that divides by zero at runtime.

*Under this design:* one fire route with a transition outcome, so the exit hook is on its construct list. The read-closure of the guard fact is `{Divisor}`; the hook's write is unordered relative to the row chain, so it is in the kill set. The fact is **dead**, no other provenance names `Divisor`, and the definition rejects naming the route. **This verdict does not depend on resolving the phase-order silence** — the hook is killed under all three readings.

### The discriminating pair — the same program with `-> no transition`

```precept
precept ExitHookNoTransitionMirror

field Divisor as integer default 5
field Slots as integer default 0

state Warm initial
state Cold terminal

event Open initial
event Go
event Finish

on Open
    -> set Divisor = 5

from Warm -> set Divisor = 0

from Warm on Go when Divisor != 0
    -> set Slots = 100 / Divisor
    -> no transition

from Warm on Finish
    -> transition Cold
```

*Today:* identical verdict — zero diagnostics, `Proved / GuardInPath`.

*Under this design:* **accept** on the fire route. `spec:1915` removes entry and exit actions from a no-transition operation, so the hook is not on the route at all, the kill set is empty, and the guard fact transports. Two programs differing by two words, **opposite designed verdicts and identical current verdicts.** This pair is the sharpest available conformance test for any implementation.

### The inspection hole — a guard fact and an argument fact that inspection does not earn

```precept
precept InspectArgHole

field Result as decimal default 0.0
field Balance as decimal default -100.0

state Active initial
state Closed terminal

event PlanRepayment(Months as integer positive)
event Close

from Active on PlanRepayment when PlanRepayment.Months != 0
    -> set Result = -Balance / PlanRepayment.Months
    -> no transition

from Active on Close
    -> transition Closed
```

*Today:* `success: true`, one `PRE0158` warning about `Balance` having no write site, `Proved / GuardInPath`.

*Why it faults.* Call inspection with no arguments. The guard is *Unknown*, hence *Possible*, hence not skipped; the action chain executes against empty arguments; the division divides by an unpopulated slot. Both the guard fact and the `positive` argument fact are unearned on that route.

*Under this design:* the inspection route is in the site's route set (§ 3, row 2), neither class is earned there (§ 5), and the definition rejects — **unless** the owner closes the hole at the runtime surface instead, which is the cheaper option and is routed.

**Collateral, and it matters.** The proposed correction to the ratified *Guard normal-form match* argument repairs only its prefix clause. Its **other** clause — guards select, therefore the guard was true — is equally false, on a different route class, and no routed correction covers it. And the discriminating pair above is marked *accept* for the `no transition` variant on its fire route; that program has an inspection route too.

### The stateless mirror — no guard, no hook, and still a hole

The `StatelessArgDivide` sample in § 2. *Today:* zero diagnostics, `Proved / CompositionalConstraint`. The row is unguarded, so on the inspection path the guard prospect is *Certain* and execution proceeds unconditionally with empty arguments. This is the case `what-i-want-2026-07-16.md:104` rests its worked example on — *"At runtime there is no zero-check anywhere — the ingress validation of the arg is what makes the division safe"* — and the correct reading of "arguments are never killable" is *true on routes where the argument was supplied*, not *always true*.

### Restore — a declared fact at position zero over data nothing established

```precept
precept RestoreRecomputeHole

field Divisor as integer default 1 positive
field Numerator as integer default 100 nonnegative
field Ratio as integer <- Numerator / Divisor
field Note as string optional editable
```

*Today:* `success: true`, two `PRE0158` warnings, `{"strategy":"DeclarationAttribute","description":"Divisor must be non-zero"}` plus the two default folds.

*Why it faults.* Restore with `Divisor` set to zero recomputes computed fields — `runtime-api.md:266`, verbatim: *"It … **recomputes computed fields**. It does **not** re-validate constraints"* — and the recomputation divides by zero. The verdict does not depend on the restore re-validation drift: even under the evaluator doc's reading, `:2143` says computed fields are recomputed *first*, and `:144` pins recomputation before constraint evaluation. Recomputation is strictly upstream of the only check that could catch it.

*Under this design:* the restore carve-out (§ 5) removes the declared fact at position zero, and the definition rejects — or the owner drops restore from the route set entirely, which spec § 0.7 arguably already licenses. Both options are routed.

### The collection witness — a fact earned by a construct that runs later

```precept
precept ExitHookBeforeRowWrites

field Log as queue of string
field Ticket as string optional

state Open initial
state Closed terminal

event Close

from Open -> dequeue Log into Ticket

from Open on Close
    -> enqueue Log "closing"
    -> transition Closed
```

*Today:* `success: false` — `PRE0064` at the dequeue: *"'Log' may be empty — guard with 'when Log.count > 0' before this mutation action, or apply 'notempty' to the collection field."* The shipped compiler carries no cross-construct growth propagation, so this is a **design-only** witness, not a reproduction of a shipped defect.

*What a rule without the ordering premise would license:* the `enqueue` must run on this route, so it establishes `count > 0`; the only other writer of `Log` is the dequeue itself; the intersection is empty; discharged. But `spec:2117` places exit actions **before** the row's mutations, so `Log` is empty when the dequeue runs.

*Under this design:* the ordering premise is undischargeable — the hook and the row chain are unordered — so the fact neither transports nor is killed, and the correct outcome is **reject**. This is the witness that makes the phase-order silence a soundness dependency.

### The fault site inside the guard that earns its own fact

```precept
precept GuardInternalFaultSite

field Total as decimal default 0.0
field Result as decimal default 0.0

state Active initial
state Done terminal

event Split(Parts as integer, Amount as decimal)

from Active on Split when Split.Parts != 0 and Split.Amount / Split.Parts > 5.0
    -> set Result = Split.Amount / Split.Parts
    -> transition Done
```

*Today:* `success: true`, one `PRE0158`, `Proved / GuardInPath`, and — note — **one** numeric obligation for **two** divisions, which is position keying at HEAD.

The division *inside the guard* is a fault site. Its only candidate fact is the sibling conjunct `Split.Parts != 0`, whose earn position is *after* the guard evaluates — i.e. after the site. Without the ordering premise the site discharges from a fact earned strictly later in the same expression. Whether short-circuit evaluation saves it is **unknown**: I checked `precept-language-spec.md`, `proof-engine.md` and `evaluator.md` and could not find a statement of `and`/`or` short-circuit semantics. Under a canon silence the safe direction refuses the discharge; the rule with the ordering premise does refuse it. The short-circuit question is routed as a minting question in its own right.

### The multi-route fan-in — the shape the ruling exists to license

```precept
precept ClaimSettlementRatio

field CoverageLimit as decimal default 1000.0
field ApprovedAmount as decimal default 0.0 nonnegative
field Ratio as decimal default 0.0 nonnegative

state Received initial
state Active
state Settled terminal

event Assess(Limit as decimal)
event Reassess(Limit as decimal positive)
event Settle

in Active ensure ApprovedAmount / CoverageLimit <= 1.0 because "An approved amount may never exceed the policy coverage limit"

from Received on Assess when Assess.Limit != 0.0
    -> set CoverageLimit = Assess.Limit
    -> transition Active

from Received on Reassess
    -> set CoverageLimit = Reassess.Limit
    -> transition Active

from Active on Settle
    -> transition Settled
```

*Today:* `success: false`, one `PRE0083` — *"Division is unsafe: 'CoverageLimit' can be zero while evaluating ensure for 'Active'"* — and **exactly one** numeric obligation, `Unresolved`. Position keying, live.

*Under this design:* the ensure's `in Active` bucket is evaluated on the two entering fire routes, the in-state no-transition routes, the update and restore routes for `Active`, and their inspection mirrors. The two entering routes discharge from *different* facts, and neither holds on the other's route. That is exactly the behaviour the ruling's *Alternative rejected* paragraph says the position reading refuses.

**And here is the finding that must not be lost.** The matrix's own illustration of the ruling reads, verbatim: *"A division inside `in S ensure` reached by two transitions into `S` is two obligations: one may discharge from the first row's guard, the other from the second row's argument constraint."* **That second discharge is not constructible.** State-anchored ensures are field-scope, so an argument-sourced discharge at a post-plan site must cross a `set F = <arg>` — and **no in-plan substitution provenance rule exists**. In the sample above, `Reassess`'s `positive` argument is written into `CoverageLimit` and the ensure reads the field, not the argument. Until that rule is licensed, the occasion reading delivers a well-defined route index over which the ruling's own motivating example cannot be proved. It is routed as a blocking item, not a follow-on.

---

## Open questions

Nothing below is settled. Each row names the class of amendment it falls under, so the owner can see which are soundness corrections (owner-reserved, witness supplied) and which are widenings (cheap and monotone). The governing text is the matrix, § Amendments, verbatim: *"Shrinking the licensed set is permitted **only** under this class. Always a major definition version; always breaks certificate replay for affected cells; always routed to the owner with the **witness program** demonstrating the unsoundness. Never folded silently into a widening or a refactor."*

### Blocking — the rule cannot be written down without these

**Q1. The operation's phase order.** Canon does not state normatively where exit hooks, entry hooks and the `omit` reset sit relative to the row's action chain. `spec:2117` places them inside a sentence about inspection; `runtime-api.md:361` and `evaluator.md:495-505` enumerate the fire lifecycle with no hook step at all. *Options:* (a) write a normative execution-order section in spec § 3A — the collection witness above shows this is a soundness question, not a precision one, so leaving it open leaves real programs undischargeable; (b) leave it unpinned and accept that facts crossing the boundary never discharge; (c) pin only the exit-hook-precedes-mutations half, which is the half the witness needs. *Cost:* (a) is one owner-authored spec section; (b) is a permanent expressiveness loss on cross-surface programs; (c) is the cheapest and leaves entry hooks open.

**Q2. Is the in-operation writer list closed?** This is the design's residual soundness dependency. `spec:1971` — *"This guarantee applies to all mutation surfaces: event-driven transitions, stateless event hooks, direct field updates, and state entry/exit actions"* — is a scope statement about the working-copy guarantee and omits three writers this design enumerates (default materialisation, computed-field recomputation, the `omit` reset), plus the update patch and the residency coordinate. I checked spec §§ 3A.1, 3A.4, 3A.5, 3A.6, § 0.4, § 0.6, § 0.7 and `proof-engine.md` and **could not find** a closure statement. *Options:* (a) add one normative sentence to spec § 3A listing these and only these; (b) accept the gap and state it inside the validity argument permanently. *Cost:* (a) is one sentence and it is the only thing that makes Principle 10 claimable for this rule; (b) means the rule ships with a named hole that voids every fault certificate if one more writer exists.

**Q3. In-plan substitution provenance.** After `set F = <expr>`, a later site reading `F` has no licensed provenance name. *Options:* (a) license it — a power-widening, monotone, cheap; (b) decline. *Cost:* (b) leaves the site-identity ruling's **own motivating example** unprovable (§ Worked failures, fan-in), and leaves the citation rule unsatisfiable for a whole minted site class.

### Narrowings — owner-reserved, each with a witness

**Q4. The inspection carve-out** (classes earned only where the door ran / the guard selected). *Witnesses:* the `InspectArgHole` and `StatelessArgDivide` programs above. *Options:* (a) route-restrict the two provenance classes as § 5 states, which rejects programs that compile today across almost every site category; (b) close the hole at the runtime surface instead — rule that inspection with incomplete arguments is a caller error rather than a Kleene-supported path (`evaluator.md:789-790`, `:1485`), which removes the problem at source and is strictly cheaper, but is a language/runtime-surface question this pass must not settle; (c) something else. *Cost:* (a) is the largest single narrowing in the document; (b) is a public-API behaviour change on `InspectFire`.

**Q5. Restore.** *Options:* (a) exclude declared facts at position zero on restore routes exactly as construction does; (b) drop restore from the route set and declare it outside the fault guarantee, which `spec § 0.7`'s boundary paragraph arguably already does — *"Restored state is trusted as valid at the time it was persisted … the runtime fault traps backstop any out-of-contract value the evaluator would otherwise fault on"*; (c) reconcile the evaluator-doc drift first and decide after. *Cost:* (a) rejects essentially every computed field containing a division; (b) admits an uncovered fault path but matches canon; note that "include restore and reject more" is **not** conservative under an exact-power contract, because an over-mint is a false rejection.

**~~Q6.~~ APPROVED (owner, 2026-07-21).** The replacement clause is adopted and applied in the matrix to all three affected arguments — *Guard normal-form match*, *Arg-bound interval arithmetic*, and *Guard-fact substitution for relational rules*. Recorded there as a shrink under § Amendments: major definition version, certificate replay breaks for cells citing them, every such cell re-checked. The correction is now also confirmed rather than inferred: `precept-language-spec.md § 3A.4`, *Operation execution order*, states normatively that state exit actions run before the row's action chain, which is exactly what makes counting the row's own writes the wrong test.

The second half — that the argument's *other* clause (guards select, therefore the guard was true) is false on inspection routes — is closed separately and did not need this correction. `precept-language-spec.md § 3A.6` now states that inspection is best-effort and not exempt: where a proof's premises are absent, the consequence is undetermined and is not evaluated. So the guard-selects clause holds for every route on which the expression is actually evaluated, and no fact restriction is required.

The original framing is kept below for the record.

**Q6 (original framing). The soundness correction against the ratified *Guard normal-form match* argument.** The ratified sentence reads, verbatim (matrix, § Validity arguments): *"The guard reads the same pre-state and args the first action reads — each assignment sees the state left by all preceding assignments (`precept-language-spec.md:164`), **and for a single-write plan there are none.**"* That clause infers an empty prefix from the row's own write count, and is false whenever an exit hook exists. *Witness:* the exit-hook program above, `Proved / GuardInPath`, zero diagnostics, divides by zero. *Proposed replacement clause:* *"…and there is no construct on the route that may write a field the guard mentions between the guard's evaluation and the site — decided by route-indexed frame and kill, not by counting the row's own writes."* **The correction as routed is incomplete**: the argument's *other* clause (guards select, therefore the guard was true) is false on inspection routes, per Q4, and needs the same treatment. *Blast radius:* three of the seven ratified arguments inherit the defective clause — *Guard normal-form match*; *Arg-bound interval arithmetic*, which cites *"the purity, determinism, and **single-write reasoning of the guard-match argument**"*; and *Guard-fact substitution for relational rules*, which carries its own copy — *"stated for the plan whose only write is the substituted one"*. *Inductive hypothesis plus sign monotonicity* does **not** inherit: it carries no single-write clause and rests on premise (d) over reachable configurations, which § 10 leaves intact. *Options:* apply / defer / dispute the witness.

**~~Q7. The position-zero seal and its relationship to ingress doors.~~ NOT A QUESTION — settled in canon; this design owes a correction (owner, 2026-07-21).**

This was raised as a three-way choice. It is not one: `precept-language-spec.md § 0.7` already answers it, in a locked section, and the design should have cited it rather than re-opening it. The design skill's own spec-first rule applies — *"a decision the canonical spec already settles is NOT a decision"* — and was not applied here.

The governing text, verbatim:

> **Governance — enforced at runtime on external input.** Every declared constraint is enforced on every value entering the entity from outside the definition — event arguments, construction inputs, direct field edits — at the moment it enters, before any computation derives from it.

> **Composition.** For a value supplied at runtime, the compiler's proof that the operation cannot fault rests on the structural fact that the value carries a declared constraint; governance makes that constraint true of the value at ingress. The proof is therefore complete at compile time and the fault never occurs — proven by the compiler, its precondition discharged by governance, nothing left to a runtime check.

Direct field edits are named explicitly, alongside event arguments, and the composition paragraph states the arrangement the whole compile-time/runtime split rests on: the compiler leans on the incoming check *because it has proven that check sufficient*. An argument's declared constraint and an edited field's declared modifier are the same mechanism at two entry points — which is what the matrix says too, § Vocabulary: *"the editable-field door makes premises (a) and (d) true for that field downstream."*

**The correction owed.** § 10's seal treats a governed edit as an ordinary write and kills the fact it just verified. That is backwards: an ordinary write may place any value in the field, whereas a governed one has just been checked against exactly the property the fact asserts. The transport rule must distinguish the two — a write through a governed entry point *re-establishes* the field's declared facts rather than killing them, at the position where it occurs. What still owes statement is the precise scope: which facts a governed entry point establishes (the field's own declared modifiers, and which constraints mentioning it), and what kills them afterwards.

This correction is a widening — it restores the field-modifier discharge the corpus depends on — so it is the cheap amendment class and needs no separate owner authorization. It remains subject to the 2026-07-21 citation duty: a proof consuming a fact established this way names the governed entry point that established it.

**Q8. The `omit` reset as a kill.** § 7 kills on it, citing `spec:1074`. But the matrix records this exact area as **Conflicted** — § Edge cells, verbatim: *"A constraint mentioning a field omitted in some state | canon asserts both reset-to-default and structurally-absent semantics | **Conflicted**"*. Building a kill on one side of an unruled conflict is a ruling. *Options:* rule reset-to-default (which `spec:1074` states) / rule structurally-absent / keep it conflicted and mark every affected coordinate accordingly.

**Q9. The generalisation of the occasion unit.** § 3 states the unit as an evaluation event at a catalog-stamped fault site rather than an occurrence of a written expression, because `operand-free-action-precondition` has no expression. This is a minting-rule matter, and the matrix reserves it: *"never adjusted in passing"*. *Options:* adopt the generalisation / keep the expression phrasing and disposition the operand-free category separately.

### Widenings — cheap, monotone, and each one recovers precision

| Item | Options |
|---|---|
| **Q10. Conditional-branch conditions as licensed facts** | license (an occasion per branch, gated by the condition) / decline. Conservative default in force: the branch position mints on every route reaching the enclosing position and must prove without the condition |
| **Q11. Predecessor exclusion beyond reject siblings** | license the single-leaf generalisation / keep `proof-engine.md:495`'s reject-sibling scope |
| **Q12. Route feasibility pruning** | license a named filter (which?) / license none. Current default is none, which over-mints and can falsely reject |
| **Q13. Cross-route fact reuse (dominator-based)** | license / defer. This is the natural next widening and the research survey already names the mechanism |

### Canon silences that change the enumeration but not the rule

| Item | Options |
|---|---|
| **Q14. `from any` desugaring** | per state (matching the comma form) / intersect across sources / other. Not conservative either way; load-bearing for route counts and for predecessor ordering. The matrix already flags it: *"the spec never states whether `any` does"* |
| **Q15. Self-transition and construction hook firing** | fire / do not. `spec:1074` says the `omit` reset *does* apply to self-transitions; hooks are unstated. Construction: spec § 3A.5 names entry *ensures*, not entry hooks |
| **Q16. Several hooks on one state — multiplicity and order** | declaration order / unspecified. Conservative default: union of targets |
| **Q17. The four construction variants** | keep them separate route classes (the evaluator's matrix splits them precisely because the constraint buckets differ) / collapse and accept an over-approximation on stateless construction |
| **Q18. Inspect-update's event prospect** | it evaluates transition-row guards over a *hypothetical* patched state, which has no position-zero configuration this design accounts for. Rule it in with a defined position zero / rule it out of the fault surface |
| **Q19. Short-circuit semantics of `and` / `or`** | define them / leave undefined. Checked the spec, `proof-engine.md` and `evaluator.md` and **could not find** a statement. Decides whether a division in a later conjunct has an occasion at all |

### Scope gaps in the committed axis — the index cannot be total until these are ruled

**Q20. Five requirement kinds and at least two minting mechanisms sit outside the committed site axis.** Measured: the committed enumeration carries **eight** of the thirteen requirement kinds (verified this pass — Dimension, DimensionalProduct, IndexBounds, KeyPresence, Modifier, Numeric, QualifierChain, QualifierCompatibility). `slice-3-boundary-report.md:35`, verbatim: *"**Five of the thirteen requirement kinds have no catalog declaration site at all** — Presence, IntervalContainment, LengthContainment, CountContainment, AssignmentQualifier … of 1,045 obligations HEAD minted across `samples/`, 464 — **44%** — are of kinds with no catalog site."* Two of the five interact directly with this rule and the interaction is *not* neutral:

- **Assignment qualifier** is the one kind whose declared discharge *is* guard narrowing (`ProofRequirement.cs:140-148`), so it is a pure transport consumer with no row in § 7's fact-shape table — which means the unknown-kills default kills it.
- **Count containment** defines itself as a transport computation rather than a frame-and-kill (`ProofRequirement.cs:215-224`, verbatim: *"the count-before interval (seeded from the declared `[mincount, maxcount]`, narrowed by any same-context `count`-comparison guard or routed reject-row sibling) advanced by the sequential mutation's sound per-kind/per-action delta"*). § 7 says a lower-bound count fact is *killed* by a shrink; canon says it is *advanced*. These are different rules and the difference is not reconciled here.

Plus `slice-3-boundary-report.md:36`, verbatim: *"**Seven distinct dynamic minting mechanisms** live outside the catalog walk"* — of which **count containment on a collection mutation** has no site category at all, since its subject is the collection and there is no operand expression. And `:38`: *"**Overflow has no catalog-declared site whatsoever.**"* *Options:* extend the site axis and regenerate / scope the fault family to the eight catalogued kinds and say so / disposition each of the five separately.

**Q21. Dual-subject requirements transport two facts over two domains.** `QualifierCompatibility`, `QualifierChain`, `DimensionalProduct` and `IndexBounds` each have two subjects — the last carrying an upper-bound accessor against the collection (`ProofRequirement.cs:260-270`). The rule as stated transports a single fact and says nothing about what happens when the two are killed by different constructs. The parent design's citation rule already anticipates this (*"not 'exactly one': `IndexBounds` has two subjects over two domains"*); the composition rule is owed and is not written here.

**Q22. The residency fact's premise class** is recorded unruled in the matrix, § Per-family case shapes: *"**Open**: whether the residency fact — the entity is in `S` — is itself available as a premise, and under which class, is not ruled."* This design puts the residency *coordinate* in the read-closure regardless, which is the sound direction, but the fact's availability as a premise still needs a class.

**Q23. Whether quantified constraints are proof surface at all** — Conflicted in the matrix, § Edge cells, against `collection-types.md:796`. The `quantifier-predicate` category is a live axis value, so the index is supplied either way.

**Q24. Whether `because`-hole interpolations enrol** — a deferred fork with a named re-open trigger. Over four hundred committed coordinates depend on it; the index is supplied either way.

---

## What this design gets wrong, stated rather than defended

**The strongest objection is Q2.** The rule's soundness rests on a writer-closure nobody has written. The partial order removes the phase-order dependency from the *kill* half, but nothing removes the closure dependency. If one construct writes during an operation that § 7 does not enumerate, the frame step fails and every fault certificate is void. I found one such writer that the whole preceding pass had missed — the `omit` reset — by reading the access-mode composition rules. That is evidence the list is discoverable and equally evidence that "I looked and found no more" is not closure.

**The ledger is probably net-negative for authors until Q3 and Q7 are ruled.** This design *removes* licensed programs — every exit-hook and cross-surface case, every `omit`-crossing case, every inspection route, every restore route — and *adds* them only in the multi-route fan-in shape. At post-plan sites, where most corpus fault sites live, the seal kills every declared fact about any written field and no substitution rule replaces it. Presenting this as a pure correctness win would be dishonest.

**The cell arithmetic is a bound, not a measurement.** The figures in § 9 are computed from the committed enumeration, but the collapse of identical contracts across route classes is unmeasured, and the reject half of every one of those cells stays untestable while the cell-to-test conversion produces no executable rows for the fault families.

**"Route" is still my refinement of the owner's word.** The owner ruled *occasions*. I substituted a static equivalence class of occasions and then, at the operand-free category and inside quantifier bodies, generalised the unit away from "one written expression" because the committed axis forces it. Both moves are argued; neither is confirmed. If the intended granularity is finer — per route *and* pre-state interval, which is what the fan-in's second discharge would need — this is the wrong shape, not a coarser version of the right one.

**Nineteen independent verification passes over the existing fault cells found none sound.** `slice-3-boundary-report.md:107`, verbatim: *"**Thirteen returned 'unsound', six returned 'needs rework', none passed.**"* This design's arithmetic in § 9 uses the 992 `defined` count as a baseline; that baseline is a count of cells, not a count of verified cells, and the two must not be conflated.

---

## What I checked and could not find

1. **A definition of "route", or any path-to-a-site construct, in canon.** Checked `precept-language-spec.md` §§ 0.1, 0.4, 0.5, 0.6, 0.7, 3A.1–3A.6; `proof-engine.md`; the matrix; the want doc.
2. **A statement that the in-operation writer list is closed.** Checked spec §§ 3A.1, 3A.4, 3A.5, 3A.6, § 0.4, § 0.6, § 0.7, `proof-engine.md`, `evaluator.md`.
3. **Multi-hook multiplicity and ordering.** Checked spec §§ 3A.1, 3A.4, 3A.6 and the state-hook grammar.
4. **Whether entry hooks fire at construction or on a self-transition.** Checked spec § 3A.5, § 3A.1, § 3A.2, `:1074`.
5. **Short-circuit semantics of `and` / `or`.** Checked `precept-language-spec.md`, `proof-engine.md`, `evaluator.md`.
6. **Whether inspection's row simulations share a working copy.** `evaluator.md:817` shows a fresh `version.Slots.ToArray()` per row, which is an implementation reading, not a canon statement. No canon sentence found either way.
7. **A trace-partitioning research file in `research/`.** None exists; the primary source was not read in this pass and no excerpt from it appears here.
8. **Not read, and named as dependencies:** `certificate-steps-membership-2026-07-12.md` (owns the closed certificate step-kind and premise vocabularies a route-indexed transport step must live in — the largest unread dependency); `normal-form-draft-2026-07-19.md`.
