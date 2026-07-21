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

Citations to the obligation-discharge matrix are by **section heading plus verbatim quote**, never by line number — that file was edited on 2026-07-21 and its line numbers moved by roughly forty. Citations to the language spec, the runtime docs and the source tree are line-anchored where an anchor is stable and were re-read from the working tree at HEAD in this pass; where commit `33707e77` moved a line, the citation gives the spec **section number** (§3A.4, §3A.6, §0.7) plus a verbatim quote instead, because the numbers in earlier drafts are stale.

### What today's rulings settled, and what this pass folds in

Commit `33707e77` restored the operation's execution order to the spec and swept three questions this design had carried as open. The rewrite below treats the following as **fixed input**, not open:

1. **The nine-phase execution order is normative** (`precept-language-spec.md § 3A.4`, *Operation execution order*). Exit actions, the state change and `omit` reset, the row's mutation chain, entry actions, computed-field recomputation, and constraint evaluation now sit in a stated order that never varies by operation. The transport rule's ordering premise is therefore a **total order across the nine phases**, not the deliberately-partial order the first draft used. Two things stay unpinned and are handled by refusal: multiplicity and order when one state carries several state actions, and whether entry actions fire at construction and on a self-transition.
2. **The write surface is enumerated** (`§ 3A.4`, *What can write during one operation* — eight writers) but **explicitly not build-enforced**. The design must carry that disclosure everywhere it leans on the list. `BUG-033` is the verified instance of the list being consumed wrong: an action's `into` binding target is a second write site that no consumer registers.
3. **A value written earlier carries its fact forward** (matrix § The cell, 2026-07-21 ruling). This is written-value provenance — clause E2 below.
4. **Inspection is best-effort but not exempt** (`§ 3A.6`). The inspection carve-out the first draft relied on is **removed**: no provenance class is route-restricted on account of inspection.
5. **A governed write re-establishes, it does not kill** (`§ 0.7`). The position-zero seal the first draft wrote is corrected — clause E1 below.
6. **The three-argument soundness correction is approved** and lives in the matrix. The corrected clause: no construct on the route between a guard's evaluation and the site may write a field the guard mentions.

The § Open questions section at the end lists, under *Settled since the first draft*, exactly which of this document's own questions each item closes.

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
| 10. Totality | Y | The rule is the bridge between a fact and the stamped predicate at a fault site; a wrong bridge is exactly a silently-`NaN`-or-divide-by-zero acceptance (spec:110 — *"Every expression evaluates to a result — never silent `NaN`, `Infinity`, or `null`"*) | The rule's soundness rests on a list of in-operation writers that `§ 3A.4` enumerates but explicitly does not build-enforce | Recorded as the design's residual soundness dependency; the enumeration now exists, so the residual is build-enforcement (catalog-declare the five uncatalogued writers), not a missing sentence |
| 11. Static completeness | Y | Every occasion of every site on every route mints and must discharge; a site reached by no route is a definition error rather than a silent pass | Five of the thirteen requirement kinds and at least two minting mechanisms sit outside the committed site axis, so the index is not yet total over what the compiler actually mints | Stated as a named gap with the measurement that establishes it, rather than claiming coverage |

**Rows with a live tension, as accepted tradeoffs.**

*Principle 1 and 3.* The corrected rule rejects programs that compile today. That is the expensive half of the matrix's amendment rules — § Amendments, verbatim: *"Shrinking the licensed set is permitted **only** under this class. Always a major definition version; always breaks certificate replay for affected cells; always routed to the owner with the **witness program** demonstrating the unsoundness."* This document therefore **routes** every narrowing with its witness and settles none of them.

*Principle 4.* Route keying means one expression proves on one route and not another. An unqualified diagnostic is then not merely unhelpful, it is false. The matrix already books this — § The minting rule: *"Diagnostics must additionally name the route, since one expression may now be provable on one path and not another; no diagnostic produces route-qualified text today."* This design does not solve the wording problem for a business-analyst reader and says so.

*Principle 10 and 11.* The rule's soundness argument has one step canon states but does not build-enforce: that the list of things which write during an operation is complete. `§ 3A.4`'s writer table now enumerates eight (including the `omit` reset, which an earlier pass had missed by reading only the mutation-surface sentence) and says outright it is *"asserted rather than kept"*. That the table had to be assembled from several sections is evidence the list is discoverable and equally evidence that "I looked and found no more" is not closure — which is why the residual is a build-enforcement item (Q2).

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
> - **outcome** — `transition T`, `no transition`, or `reject`, read syntactically off the row, and — new since the first draft — **`transition T` distinguishes a self-transition (`T` is the current state) from a transition to a different state**. Fixed once the row is fixed.
> - **patch** — for update-class routes: the set of fields the state's access-mode composition admits as writable. A set, never a single field.
>
> **Two executions are on the same route exactly when their descriptors are equal.** A route is a static equivalence class of runtime occasions; a route-indexed obligation is universally quantified over its class.

**Why these components and no others.** The descriptor records exactly the things that decide *which sites are evaluated* and *which constructs may write*.

- The **operation class** is canon and closed, and it is the authority for which constraint buckets each operation evaluates. `docs/runtime/evaluator.md:2133-2143`, verbatim rows: `| Fire | no | yes | always, from <current>, on <event>, to <target> |`; `| InspectFire | no | yes (all rows) | same as Fire, but evaluated for every row |`; `| Update | yes | no | always, in <current> |`; `| Restore | no (bypassed) | no | always, in <current> — computed fields recomputed first |`. This table is why inspection is a separate class and not a mode: `InspectFire` evaluates constraints *for every row*, not only the matched one.
- **Row selection** is a component because rows are first-match. `spec:1904`, verbatim: *"Transition rows are evaluated in declaration order — the first matching guard wins, and remaining rows are not evaluated."*
- **Outcome** is a component because it decides whether hooks are on the operation at all. `spec:1915`, verbatim: *"**Successful no-transition event.** Event fired; in-place mutations committed; no state change. This is a deliberate design allowing in-place data changes to be event-driven **without triggering entry/exit actions**."*
- **Nothing else can branch.** `spec:162`, verbatim: *"There are no `if` statements that split execution into paths that later reconverge."* Guards are the only branch points, and every guard on a route is either fixed by the descriptor or handled by over-approximation.

**Why `outcome` now distinguishes a self-transition.** The descriptor must additionally determine **phase membership** — which of the nine phases of `§ 3A.4` *Operation execution order* run on this route — because the phase list is what supplies the ordering premise (§ 6). Phase membership is a function of `kind` and `outcome`, so no component is added. But a self-transition and a transition to a different state carry **different write sets**: `spec:1074`, verbatim — *"**`omit` clears on state entry** — field value resets to default on any transition into an `omit` state (including self-transitions); does NOT apply to `no transition`"* — so a self-transition carries the phase-4 `omit` reset for the current state's omitted fields, and `§ 3A.4` *Operation execution order* leaves entry-action firing on a self-transition unruled. Folding the two together makes a route whose phase-6 membership and phase-4 write set are unknown indistinguishable from one whose write set is known. The refinement is a descriptor sharpening, not a licensing change.

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
| `rule-condition` | every route **except restore** — the `always` bucket is evaluated on every operation but restore bypasses constraint evaluation (see the restore correction below) |
| `rule-activation-guard` | every route **except restore** |
| `state-ensure-condition` | per the evaluator's matrix: the `in <current>` bucket on fire, update and construction (**not restore** — see the restore correction below); the `to <target>` bucket **only** on fire and construction-with-initial-event (`evaluator.md:2148` — *"`to <State>` constraints are evaluated only during Fire, not Update or Restore"*); the `from <current>` bucket on fire; plus mirrors |
| `ensure-activation-guard` | as its constraint |
| `computed-field-expression` | every route — recomputation appears in every operation's pipeline (`evaluator.md:144`, verbatim: *"**Computed field recomputation** \| After mutations and before constraint evaluation: walk `SlotLayout.ComputedSlots` and re-evaluate"*) |
| `constraint-rationale-interpolation` | its constraint's route set, restricted to routes where the constraint evaluates false; whether these holes enrol at all is a deferred fork, and the index is supplied either way |
| `quantifier-predicate` | its enclosing position's route set; element multiplicity is handled by universal quantification, not by the route (§ 8) |
| `choice-value-expression` | wherever its host is reached |
| `access-mode-guard` | update, its mirror, and restore — **including guarded readonly pairs**, because the guard's evaluation is real even when the write is then denied |
| `operand-free-action-precondition` | fire, inspect-fire, construction and its mirror |

**Restore leaves every constraint-evaluating site category** (correction, this pass). The table above read "every route" for `rule-condition`, the three `state-ensure-condition` buckets, `ensure-activation-guard`, `constraint-rationale-interpolation`, and any `quantifier-predicate` / `type-qualifier-expression` whose host is a constraint. That included restore, which is now wrong. `evaluator.md` § Constraint Evaluation Matrix, **Key rules**, verbatim at HEAD: *"Restore bypasses both access-mode checks and constraint evaluation — trusted hydration (spec §0.7; Decision 5)."* The spec agrees — `spec:1969`, verbatim: *"(Restored state is neither: it is trusted as valid at persistence time and is re-governed only by the next operation's sweep — §0.7.)"* So **restore is removed from the route set of every constraint-evaluating category.** It **stays** in the route set of `computed-field-expression`, because `§ 3A.4`'s writer table names *"Computed-field recomputation | the `<-` declaration (§3.5); recomputed in every operation, including `Restore`"* — which is exactly the `RestoreRecomputeHole` witness's shape (§ Worked failures) and survives untouched.

**Drift finding, reported not silently used.** `evaluator.md` § Constraint Evaluation Matrix contradicts itself at HEAD. The **table row** still reads `| Restore | no (bypassed) | no | always, in <current> — computed fields recomputed first |` — naming `always` and `in <current>` constraint plans — while the **key rule** immediately below says constraint evaluation is bypassed. `33707e77` swept the key rule and the summary lines but not the table row. This is a live doc-sync obligation carried in § Doc-update enumeration; the restore removal above follows the key rule and the spec, which agree.

**The state-hook rows gain a phase anchor.** `state-hook-guard` / `state-hook-action-operand` read "fire and inspect-fire with a transition outcome touching the anchor state". Split by phase: **exit** actions on the source state run at phase 3, **entry** actions on the target state at phase 6, and `spec:1915` removes both on a `no transition` outcome. Construction firing of entry actions remains unruled (`§ 3A.4` *Operation execution order*, *Still unsettled*, verbatim: *"whether entry actions fire at construction and on a self-transition"*).

**One consequence must be said out loud.** `operand-free-action-precondition` is a fault site with no written expression — the emptiness precondition of `dequeue`/`pop`. The occasion unit is therefore **an evaluation event at a catalog-stamped fault site**, not "an occurrence of a written expression". The owner's ruling uses the written-expression phrasing because that is the case it was illustrating; the committed axis contains a case the phrasing does not reach. This is stated as a **scope observation, not an override** — it changes the count for no expression-bearing site — and it is routed in § Open questions, because the matrix reserves the minting rule: § The minting rule, verbatim: *"A change to any part of the minting rule changes which cells exist, so any future amendment to it re-opens every cell the change reaches — it is amended under the amendment rules below, **never adjusted in passing**."*

### 4. The transport rule

Write `p ⪯ᵣ q` for "on route `r`, the construct at `p` is ordered no later than the construct at `q`" under the total phase order § 6 pins. Write `mayWrite(r, p, q)` for the set of fields any construct on route `r` between `p` and `q` may write. Write `deps(φ)` for the read-closure of the fact (§ 7).

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

**The ordering premise is load-bearing and is not decoration.** Without `p ⪯ᵣ q` the rule licenses transporting a fact *backwards in time* — earned at `p`, consumed at `q`, where `q` actually runs first. Two of the witnesses in § Worked failures exploit exactly that hole. A larger kill set cannot repair it: growing a kill set only ever refuses more, it never establishes an ordering.

**The companion rule — where a fact is *earned*.** Transport moves a fact from an earn point; a second rule says what earns one, and transport runs from the **latest** earn point on the route. Keep the transport rule above unchanged — do **not** fold governance exceptions into `mayWrite`, because that would smear establishment into the frame condition and make the certificate step unreplayable. Add instead:

```
   c ∈ constructs(r)      establishes(r, c, φ)
   ────────────────────────────────────────────
              Γ ⊢ᵣ φ earned-at c
```

`establishes` has exactly three clauses, each carrying its own citation duty (§ 5 states them in full):

- **(E1) Governed re-establishment.** `c` is an update patch's governed write to an editable field `F`, and `φ` is one of `F`'s **own declared modifier-constraints** — the single-field constraints declared directly on `F`. Governance checked the incoming value against those modifiers at ingress (`§ 0.7`). E1 is deliberately narrow: it does **not** re-establish a multi-field constraint that merely mentions `F` (see § 5, and the red-team attacks it forecloses in § Red-team attacks).
- **(E2) Written-value provenance.** `c` is `set F = e` that **must** run on `r`, and `φ` is entailed by the facts holding of `e` at `c`. Matrix § The cell, 2026-07-21 ruling, verbatim: *"After `set F = <expr>`, an expression later in the same operation that reads `F` may consume what is known about `<expr>` at the point of the write, until something on the route writes `F` again."*
- **(E3) Must-run collection growth.** `c` is a growing action that **must** run and provably precedes; it establishes `count > 0`. The existing in-chain grow provenance, restated as an instance of the same shape.

**The precision item 5 gets wrong if read sloppily.** A governed write re-establishes **only what its governance evaluates**. A guard fact strictly stronger than `F`'s declared modifiers — `when Divisor != 0` on a field carrying no `nonzero` — is **still killed** by a governed write to `Divisor`. "Governed writes never kill" is false.

**Why E1 needs no must-run premise, and why it is scoped to update routes.** The update patch's field set is not statically known, so no field is a *must*-write. E1 still holds, by two branches over the interval from position zero to phase 5: if the patch wrote `F`, governance made `φ` true of the written value and phase 5 stored it; if it did not, `F` is unchanged since position zero, where `φ` held as a carried modifier fact. On an update route nothing writes a field before phase 5 except the patch itself, so the "did not write" branch's frame over that interval is trivially clean. **Both branches yield φ at phase 5.** This is why E1 is exempt from the kill-on-may / earn-on-must asymmetry that binds E2 and E3, and why E1 is the *update-patch* clause specifically — the other ingress door, event arguments, produces argument facts (§ 5), not E1.

### 5. What earns a fact, and on which routes

The inspection carve-out the first draft carried is **deleted** (item 4). Governed re-establishment (E1) and written-value provenance (E2) are added. The construction and restore carve-outs survive on grounds that have nothing to do with inspection.

| Provenance | Earned at (phase) | Available on | Killable |
|---|---|---|---|
| Ingress-governed argument fact | phase 1 (ingress governance) | **every route carrying an event, including its inspection mirror** — no route restriction | never; nothing in a plan writes an argument |
| Fired-guard fact | phase 2, immediately after that guard evaluates | fire routes selecting the row; the all-guards-failed class; **inspect-fire** — no route restriction | yes |
| Predecessor exclusion (earlier rows' guards were false) | phase 2, at that guard's evaluation | **fire and the all-guards-failed class only — not inspect-fire** (see the surviving restriction below) | yes |
| Carried modifier fact / pre-state constraint fact | route position zero (before phase 1) | fire, update, the stateless-handler class, and their inspection mirrors; **not** construction; **not** restore | yes, by any ungoverned write to `deps(φ)` |
| **Governed re-establishment (E1)** | at the governed write (phase 5, the update patch) | update routes and their mirror | yes, by any later ungoverned write to `deps(φ)` |
| **Written-value provenance (E2)** | at the `set` (phase 3, 5, or 6 per where the action sits) | every route on which that action **must** run | yes, by any later write to `F` |
| Literal denotation | at the site | every route | never; read-closure empty |
| Catalog-declared callee attribute | at the call site | every route | the operand facts are killable; the attribute is not |
| In-chain collection growth (`count > 0`) (E3) | after the growing action | every route where the grow **must run and provably precedes** — now decidable from the phase order | yes, by a later shrink |
| Residency fact (the entity is in state `S`) | route position zero | routes with a resident state | yes — **killed at phase 4**, so it survives phases 1–3 (including exit actions) and dies before phases 5–8 |
| Default materialization at construction / the `omit` reset | phase 4 (`omit`) / construction | — | **flagged, not settled — see § Open questions N3** |

**Kill on *may*; establish on *must*.** A grow inside a guarded hook belongs in the kill set — it may have run — but must **not** establish `count > 0`, because it may not have run. Over-approximation is free in one direction only. It agrees with canon's own phrasing at spec:258: *"grow-establishment proves only `count > 0`, which one added element guarantees"*. The same asymmetry decides E2 and E3: a `set` or a grow earns a fact **only where it must run** on the route. A state action that is one of several on its state, or that carries a `when` guard, is **not** must-run — it is in the kill set and establishes nothing. This is the clause that makes the multi-exit-action red-team attack reject (§ Red-team attacks, Attack 5).

**Governance re-establishes only what it evaluates (E1).** A governed update-patch write to `F` re-earns `F`'s **own declared modifier-constraints** — the single-field constraints declared directly on `F` (`positive`, `nonzero`, a `max`, etc.), which governance checked against the incoming value at ingress. It does **not** re-earn a multi-field constraint that merely mentions `F`. `what-i-want-2026-07-16.md:143` names a broader ingress set — verbatim: *"For an editable-field write, it's the field's modifier-rules **and** every rule that mentions the field"* — but that is the *runtime* evaluation set at the door; for *compile-time transport* the mentioning-constraint half cannot be argued sound, because a constraint mentioning `F` and `G` is checked at phase 1 before the patch's write to `G` lands at phase 5, so what governance verified is a fact about a configuration that never exists. The mentioning half is routed (§ Open questions N5). This narrowing is exactly what forecloses red-team Attacks 1, 2 and 3 (§ Red-team attacks).

**Written-value provenance (E2).** After a `set F = e` that must run, an expression later on the route reading `F` may consume what holds of `e` at the write, until something writes `F` again (matrix § The cell, quoted in § 4). E2's own scope is `set`: it does **not** reach `dequeue Q into F` (whose value is the collection head, and whose target is BUG-033's blind spot) or `clear F` (which establishes a *presence* fact, not a value fact). Both are minted site classes today; both are routed (§ Open questions N1), not licensed here.

**The deleted paragraph and its replacement.** The first draft's *"inspection carve-out"* paragraph — with its `evaluator.md:1485` / `:789-790` / `:817-820` apparatus — is removed. `§ 3A.6` supplies the replacement, verbatim: *"A proof discharged from an argument constraint or a guard stays discharged: where those premises are absent, the consequence is undetermined and is reported as such rather than evaluated."* Where an argument or guard premise is absent on an inspection route, the site is **not evaluated**, so there is no evaluation occasion to be unsafe — the fact is not route-restricted, it is simply consumed only where the expression actually runs. The runtime mechanism is now in canon too — `evaluator.md`, § Component Mechanics, verbatim: *"an expression whose inputs are not all supplied has no consequence the evaluator can reach, so it is not evaluated and its result is reported as undetermined."*

**The one surviving inspection restriction — predecessor exclusion, and it is flagged not made.** `§ 3A.6`'s clause reaches *absent* premises only. Predecessor exclusion's premise on an inspect-fire route is not absent — it is *false*, because inspection simulates every row regardless of whether an earlier guard matched (`evaluator.md` § Constraint Evaluation Matrix, verbatim: `| InspectFire | no | yes (all rows) | same as Fire, but evaluated for every row |`). So predecessor exclusion is **not** rescued by § 3A.6 on inspect-fire routes. This is a shrink and it is flagged, not made: it either stands (predecessor exclusion unavailable on inspect-fire, narrowing every reject-sibling narrowing the shipped compiler performs — `proof-engine.md:495`'s `BuildSiblingRejectExclusions`), or the owner rules a simulated non-selected row outside the fault surface. Routed (§ Open questions N8). It is also why predecessor exclusion, alone among the guard-family provenances, keeps a route restriction in the table above.

**The construction and restore carve-outs — unchanged in substance, on independent grounds.** On a construction route, position zero is the hollow default configuration, and whether it satisfies the declared constraints *is* the establishment obligation — consuming a declared constraint there closes a loop. On a restore route the position-zero configuration was never established by any obligation in the file: `spec § 0.7`, verbatim: *"Restored state is trusted as valid at the time it was persisted: hydration is fast and does not re-validate … Such state is reconstituted, not re-governed on load."* `spec:1969` and the corrected `evaluator.md` key rule agree, and `§ 3A.4`'s writer table names *"Restore writing persisted values into slots | §0.7"* as an **ungoverned** writer. So on a restore route position zero carries no declared facts **and** the slot injection kills whatever a default fold established — leaving a computed-field division on a restore route with no fact source (the `RestoreRecomputeHole` witness). Q5 (the restore disposition) stays open exactly as routed.

**Sign of § 5 overall: net widening** — two provenance classes added, one carve-out removed — with **one flagged shrink** (predecessor exclusion on inspect-fire).

### 6. The order the rule is computed over — the normative phase order

`⪯ᵣ` is the **total order over the nine phases of `§ 3A.4` *Operation execution order***, refined within phase 5 by written chain order (`spec:164`) and within phase 1 by ingress-before-derivation (`spec:1969`). The spec states the phases and their normativity verbatim: *"Every operation that can change entity data — `Create`, `Fire`, `Update`, `Restore`, and their inspection counterparts (§3A.6) — executes these phases in this order. A phase that does not apply to an operation is skipped; the relative order of the phases that do apply never varies by operation."* The nine phases are ingress governance, dispatch, exit actions, the state change and `omit` reset, mutation, entry actions, computed-field recomputation, constraint evaluation, commit-or-discard.

Both runtime docs now **defer** to this order rather than contradicting it — `runtime-api.md`, § Fire, and `evaluator.md`, § Working Copy Management, both carry a note naming `§ 3A.4` as the normative sequence. This retires the first draft's Decision 2 ground, which rejected a total order because *"both runtime docs enumerate the fire lifecycle with no hook step at all."* That is no longer true at HEAD.

**Exactly what remains partial, and why — both named by the spec's own *Still unsettled* note, not by this design:**

1. **Within phase 3 and within phase 6, when one state carries several state actions.** `§ 3A.4` *Operation execution order*, *Still unsettled*, verbatim: *"when a state carries more than one state action, whether all of them fire and in what order (declaration order is the obvious candidate but is unstated)"*; and the Open Questions restatement, verbatim: *"Until these are ruled, no analysis may assume an ordering among several state actions on one state, nor that entry actions run at construction."* Two state actions on the *same* state in the *same* phase are mutually unordered; each is still totally ordered against every construct in every other phase. This residue never crosses a phase boundary. It is a **precision** loss handled by may-write over-approximation, and — through the must-run half of § 5 — a **membership** rule for the earn side: neither of two same-state actions is must-run, so neither establishes.
2. **Whether entry actions fire at construction and on a self-transition.** Same note. This is a **membership** question, not an ordering one, and the design classifies it as such: the construct is **in the kill set** (it may run) and **establishes nothing** (it is not must-run). That is a pure application of the § 5 asymmetry, not a new default.
3. **Whether the `omit` reset is a write at all** — unruled (§ Open questions N3/Q8), unchanged.

**The first draft's "soundness dependency" framing is withdrawn.** The phase-order silence is closed. Pinning the order is **monotone in both halves**: it *adds* earns (previously-unordered pairs become ordered) and it *shrinks* the kill interval (a construct now provably outside `(p, q)` leaves the may-write set). Nothing previously licensed is lost. That answers the first draft's Falsifier 5 with a flat **no**: pinning cannot reject anything the partial order accepted. The residue in items 1–2 is a precision-and-membership question, handled entirely by over-approximation in the safe direction; it is not a soundness hole.

### 7. What kills — the write set and the read-closure

**Write constructs and their targets.**

| Construct | Targets | Ground |
|---|---|---|
| `set F = e`, `clear F` | `{F}` | `proof-engine.md:1251` — *"Full replacement (`set`/`clear` — `ReplacesValue` / `Empties` …): stamped onto `ProofObligation.ReassignedBefore`. Every guard fact about the field is invalidated"* |
| `dequeue C into F`, `pop C into F` | `{C, F}` — two targets | the grammar's action form names both |
| collection grow | `{C}` | catalog `ActionMeta.Effect = Grows` |
| collection shrink | `{C}` | `proof-engine.md:1253` — *"A shrink may reduce count to 0 … Upper-bound count facts (`count < N`) are preserved (a shrink only lowers count) and unaffected."* |
| computed-field recomputation | the computed fields whose transitive inputs were written — killed at the earliest transitive input write **and again at phase-7 recomputation** (see below) | `§ 3A.4` phase 7; `evaluator.md:144` |
| the `omit` reset on state entry | every field omitted in the destination state, on any transition into an `omit` state including a self-transition | `spec:1074`; `§ 3A.4` phase 4 |
| restore slot injection | every slot | `§ 3A.4` writer table — *"Restore writing persisted values into slots \| §0.7"*, an ungoverned writer |
| the update patch | the whole writable field set for that state | `runtime-api.md`, § Update, shows a two-field patch on both lanes |
| the residency coordinate — **this design's addition, not on the spec's list** | the pseudo-target "which state the entity is in", on any transition that changes state | see below |
| guards, conditions, interpolations, modifier expressions | nothing | `spec:170` expression purity |

**The writer list is taken from `§ 3A.4` *What can write during one operation*, not assembled here** — eight writers, and the design adds a ninth (the residency coordinate) it labels as an extension. Two entries the first draft either lacked or mis-grounded: **restore slot injection**, now named; and the **`into` binding target**, which the first draft grounded on "the grammar's action form names both". Re-ground it on `§ 3A.4`, verbatim: *"`dequeue Q into D` writes both the collection and `D`. Any analysis that derives write sites from an action's primary field target alone will miss it, and a fact about `D` will appear to survive"* — with **BUG-033** as the behaviourally-confirmed instance (divisor obligation `Proved` / `GuardInPath` on a program that divides by zero).

**The list is asserted, not build-enforced, and the design says so at every point it leans on it.** `§ 3A.4`, verbatim: *"**This enumeration is not yet enforced.** … Until that exists, this table is asserted rather than kept, and any soundness argument that depends on the enumeration being complete should say so."* This disclosure is repeated at § 11 Step W3, § 13's Principle-10 claim, and the § Semantic Rules seal (§ 10). It must never be paraphrased into "canon closes the list."

**Two named over-approximations, both kill-direction only.** A guarded construct kills regardless of its guard. Several state actions on one state are unioned rather than ordered, per the *Still unsettled* note that leaves their multiplicity open — union is sound under every possibility including "only some fire".

**Computed fields are killed twice — at the earliest transitive input write and at phase-7 recomputation.** *Rationale:* the first draft killed a computed field's fact only at the earliest input write, arguing that was strictly earlier than recomputation and therefore sufficient. That held only while every earn sat at position zero or in a guard. E1 and E2 move earns to phase 5, and a fact earned at phase 5 can sit **inside** the interval `(earliest-input-write, recomputation)` where the input-write kill no longer reaches it — the red-team's Attack 1 exploits exactly this. The fix: recomputation at phase 7 is itself a write to the computed field (`§ 3A.4` phase 7), so a fact about a computed field is killed by it like any write, **in addition to** the earlier input-write kill. Both fire; whichever falls in `(p, q)` applies. This is strictly more killing — the safe direction — and closes Attack 1's `[5, 7]` gap (§ Red-team attacks). *Tradeoff:* precision loss on programs that legitimately read a computed field between its recomputation and the sweep — recoverable later, not taken here.

**The corrected kill rule, with the governance exemption.** A construct `c` on the route, ordered strictly between `p` and `q`, kills `φ` when `targets(c) ∩ deps(φ) ≠ ∅` — **unless** `establishes(r, c, φ)` holds (E1/E2/E3), in which case `φ` is not transported through `c` but **re-earned at `c`** and transported forward from there. Per-writer disposition — the precise scope the owner's correction says the design owes:

| Writer | Governed? | Effect on the field's **declared modifier** facts | Effect on a **stronger** guard fact |
|---|---|---|---|
| Row / handler action `set F = e` | no | **re-earned by E2** (whatever holds of `e`, where it must run) | killed |
| `into` binding target | no | killed — E2's ruling text is *"After `set F = <expr>`"*, so its extension to a binding target is **not** covered (N1) | killed |
| State exit / entry action chain | no | as `set` (E2 applies where it must run — a multi-action state's actions are never must-run) | killed |
| Default materialization at construction | no ingress door, but the establishment fold discharges the field's declared facts (matrix § Validity arguments, *Literal constant-fold for defaults*) | see N3 | killed |
| Computed-field recomputation | no | see below | killed |
| The `omit` reset | no | see N3 — gated on the unruled `omit` conflict (Q8) | killed |
| Update patch on an editable field | **yes** (`§ 0.7`: *"event arguments, construction inputs, direct field edits"*) | **preserved by E1**, on both branches, for the field's **own** modifiers only | killed; and a multi-field constraint mentioning the field is **not** preserved (N5) |
| Restore slot injection | **no** — explicitly outside the guarantee (`§ 0.7`) | killed | killed |

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

**One cell is not reconciled and is carried as such.** `ProofRequirement.cs:215-224` defines a lower-bound count fact as *advanced* by a mutation delta, where this table says it is *killed* by a shrink. These describe two different objects — the obligation's own interval computation versus a transported fact — and may well be compatible; nothing settled today touches it, and it is routed (Q20) rather than resolved here.

### 8. Quantifier bodies

A quantifier predicate is evaluated once per element and the element count is data-dependent, so an occasion is not unique inside one. The resolution: a site inside *k* nested quantifier bodies mints **one obligation per (position, route), universally quantified over the *k* bindings.** Sound because `spec:160` makes a quantifier unfold to *"a finite conjunction or disjunction of its predicate: an acyclic expression tree, not a control-flow loop"*, and purity (`spec:170`) means every unfolding shares one fact environment — so a single quantified obligation discharges every occasion. The key stays `(position, route)`; the proof condition gains a universally quantified variable whose only fact source is the collection's inner-type modifiers, which is a provenance question and not a transport one.

One consequential edge from § 3's restore correction: a quantifier predicate inside a *constraint condition* loses its restore routes along with its host. Whether quantified constraints are proof surface at all is **inherited-open**, not settled here: the matrix, § Edge cells, records *"A constraint quantified over a collection | canon classifies quantifier predicates as runtime governance (`collection-types.md:796`) — conflicting with no-deferral"* with disposition **Conflicted → open**. The `quantifier-predicate` category is a live axis value with committed coordinates, so the definition owes it an index either way; that index is supplied above.

### 9. Cell keying

> **The obligation is keyed `(position, route)`. The cell is keyed by adding exactly one closed axis — the route class — to the existing fault coordinate. The concrete route is certificate payload, never a cell coordinate.**

A cell is a *schema*: the matrix, § The cell — *"**Obligation schema** — the site's proof condition with metavariables, under the stated normalization"*; and § Vocabulary — *"Two programs sit in the same cell iff — exactly when — their minted obligations unify with the same schema, differing only in how the placeholders are filled in."* A concrete route names one program's states, events and rows, so keying cells by it makes every cell program-specific, destroys schema matching, and makes the coordinate product unbounded — which § Storage forbids, since *"A family's full coordinate product — every combination the axes admit, each carrying a disposition … is produced by the same tooling that produces the cells, committed beside them, and read by the validator."*

This **overrides** the parent design's own fourth owed bullet (*"under the ruling the unit is the occasion, so keying is per (position × route)"*). The correction is stated here rather than applied silently.

Two further axes are rejected. A *phase* axis is a function of position — an exit-hook action operand is always in the hook segment — so storing it is a parallel copy of derivable data, which the catalog rule forbids. A *prefix profile* axis is not derivable from any abstraction of the route: whether an exit hook exists and what it writes is a property of the program. The prefix belongs in the **contract** instead — each fault cell's discharge contract is stated over a `mayWrite` metavariable, and each owes a near-miss witness exercising a non-empty intersection.

**The contract needs a second metavariable.** The first draft put only the write prefix in the contract, as a `mayWrite` metavariable. E1 and E2 introduce an **earn position that is neither route position zero nor a guard**, so each fault cell's discharge contract also needs an `establishedAt` slot (the establishing construct) plus the **citation set** the matrix's 2026-07-21 ruling requires — § The cell, verbatim: *"a proof may not consume such a fact unless it **names the obligation that established the fact and every obligation that preserves it**, each itself discharged."* Both are added to the `cell.schema.json` line of § Inventory and § Doc-update enumeration.

**Size and shape.** The written coordinate product is unchanged in size: 101 requirement sites × 23 site categories × 8 route classes × 4 type families = **74,336**. But the first draft's prose figures — *"roughly 52,000 reachable, with about 22,000 pruned"* and *"an upper bound of about 5,500 defined"* — are **stale in the shrinking direction** and are dropped rather than restated: restore now leaves every constraint-evaluating site category (§ 3), and the inspection mirrors' contracts coincide with their mutating twins except at predecessor exclusion and inspect-update (§ 2), so the collapse is materially larger than the first draft's "not measured" hedge implied. **No new number is published in prose** — the enumeration is generated data (matrix § Storage: *"The coordinate enumeration is generated data, not a report"*), so the figure comes from regeneration or not at all. Today's baseline (`fault-axis-disposition-meta.json`: `"productSize": 9292`, `"defined": 992`, `"empty": 7047`, `"open": 760`, `"deferred": 441`, `"conflicted": 52`) is a count of coordinates, not of verified cells.

**The shape change matters more than the count.** The site-category axis is a free string today with no enum, so the scope check cannot be total. The § 3 relation replaces a flat product with a fixed relation computable from the parse tree, and closes both axes: 23 site categories by 8 route classes. An axis that could not be validated becomes one that can.

**Certificate payload.** A certificate records, per obligation, the route descriptor using the *stable* row identity, and the **phase-ordered list of `(construct identity, phase, write-target set, governed / ungoverned)` entries** the compiler used. The phase ordering and the governed flag let the checker replay the E1 preserve clause without re-deriving which doors are governance doors. The checker validates the transport premise by intersecting that list against the read-closure; it never re-derives routing, `from any` expansion, or hook attachment. Grounded in `what-i-want-2026-07-16.md:206`, verbatim: *"The certificate records the finished proof step by step, and the checker simply walks those steps and confirms each one … Every step is re-verified; the figuring-out is never repeated."* The validity argument (§ 11) records one sharpening this creates: re-deriving *which routes reach a site* is proof-side work the checker need not repeat, but re-deriving *which constructs a named route contains* is a syntactic gather the checker **must** repeat, because it is the entire content of the frame.

### 10. The mid-plan reconciliation

**The locked write-plan definition is not touched, not narrowed and not reinterpreted.**

**The two sentences quantify over different objects.** The preservation obligation is a predicate over **configurations** — its domain is the reachable configuration set, and the locked entry says exactly why: § Vocabulary, Write plan, verbatim — *"no reader, no later handler, and no persistence ever sees a mid-handler state. A mid-handler state is not a reachable configuration, and the induction that premise (d) rests on ranges over reachable configurations only."* Correct, and untouched. A fault obligation is a predicate over **evaluation events**: `spec:110` quantifies over expressions — *"Every expression evaluates to a result — never silent `NaN`, `Infinity`, or `null`."* An evaluation event is not a configuration. Atomicity protects configurations; a discarded working copy does not un-divide by zero.

**The decisive independent ground is inspection.** `Inspect` commits nothing and still carries the full fault surface. `spec:2117`, verbatim: inspection *"has the same depth as event execution: guard evaluation, exit actions, mutations, entry actions, computed field recomputation, and constraint evaluation — all executed on a working copy without committing"*, and *"The inspection result matches what execution would produce for the same inputs."* If atomicity dissolved the mid-plan question, inspection would have no fault surface at all — which contradicts spec § 0.7's *"A precept that compiles without diagnostics cannot produce a runtime fault."* **This argument does not depend on reading the atomicity clause charitably**, because it is about an operation that promotes nothing.

**The seal — the mechanism, not the assertion.**

> A fault proof may cite a declared constraint or field modifier **only at route position zero**, under the citation duty below. It may never cite a constraint's truth at any later position. Every fact a fault proof holds mid-plan arrived there by the transport rule from position zero, or from a guard, an argument, a literal, or a callee attribute — and if any construct in the write set touched a field the fact mentions, the fact is dead.

The seal as first written read *"A fault proof may cite a declared constraint or field modifier only at route position zero … It may never cite a constraint's truth at any later position."* That is corrected. `§ 0.7` names direct field edits alongside event arguments as governed, and states the compiler's proof rests on the incoming check *because it has proven that check sufficient* — so a governed write **re-establishes** the field's declared facts at the position where it occurs, rather than killing them. The rewritten seal:

> A fault proof may cite a declared constraint or field modifier:
> - **at route position zero**, on routes whose position zero is an established configuration — fire, update and the stateless-handler class. **Not** construction (position zero is the hollow default configuration, and whether it satisfies the declared constraints is precisely the establishment obligation — citing it there closes a loop rather than grounding one). **Not** restore (position zero was never established by any obligation in the file — `§ 0.7`, `spec:1969`, and the corrected `evaluator.md` key rule all agree).
> - **at the governed update-patch write that re-establishes it (E1)**, for exactly the written field's **own declared modifier-constraints**.
> - **at a preceding must-run write that established it (E2)**, for exactly what is known of the written expression at the write.
>
> It may **never** cite a constraint's truth at a position merely because the post-state will satisfy it. Every fact a fault proof holds mid-plan arrived by transport from one of the earn points above, or from a guard, an argument, a literal, or a callee attribute — and if any construct in the write set touched a field the fact mentions and did not re-establish it, the fact is dead. Each citation carries the matrix's 2026-07-21 citation duty.

Three checkable consequences:

1. Position zero is the pre-state configuration on fire, update and stateless-handler routes, which is exactly the domain the locked induction licenses. Construction and restore are carved out (§ 5). E1 and E2 recover the mid-plan facts the old seal killed at post-plan sites.
2. **Mid-plan positions have no constraint set manufactured.** There is no "the constraints holding at position four"; the rule never needs one and no derivation may manufacture one from the fact that the post-state will satisfy the constraint. That is the false-proof shape the matrix records at § The cell, foreclosed structurally rather than by policy.
3. **The post-plan constraint set is not a premise either.** A fault site in a rule or ensure condition is evaluated *by* the sweep; at that moment the working copy is a candidate, not a configuration, and assuming the constraints hold there is circular — it is what the sweep is deciding.

**The seal was the first draft's single largest narrowing at post-plan sites, and E1/E2 recover most of it.** Inspection strengthens the independent ground rather than weakening it: `Inspect` commits nothing and still carries the fault surface — `§ 3A.6`, verbatim, states outright that *"the fault guarantee (§0.1 principles 10 and 11) holds during inspection exactly as during execution."*

**The direction asymmetry, named so a reader does not conflate the indices.** Constraint obligations are computed **backward** — weakest precondition by backward substitution through the plan's writes in reverse, locked. Fault facts are computed **forward** — frame and kill along the route. Two directions over one operation, indexing two different predicates. The locked entry already anticipates the coexistence: *"Carrying a guard fact through the prior writes of a plan is a real derivation step (a certificate step, not a matrix column)."* This rule is what that step is indexed by.

**And the locked entry already delegates this question.** Same entry, verbatim: *"an expression later in a plan does see the results of earlier writes … and its own fault obligations are minted per evaluation site — that is a fault-family question about where values come from, not a decomposition of the preservation obligation."* This rule is that fault-family answer. Whole-plan granularity says nothing *owes* a constraint's truth mid-plan; the seal says nothing may *assume* it past a write to the mention set. The two are not merely compatible — the seal is what makes the atomicity concession safe.

### 11. Validity argument — route-indexed frame, kill and establishment

Required by the matrix, § Rule validity, which names *"a transport rule"* explicitly among the generative rules owing an argument, and whose gate reads: *"no cell ratifies whose derivation cites an argument-less rule."* Two rules are argued together, because neither is truth-preserving alone: **transport** (§ 4) and **establishment** (§ 4's companion rule, clauses E1–E3). Every fault cell cites transport, so the exclusions below are exclusions on the whole family.

**Claim.** If the rules license `Γ ⊢ᵣ φ holds-at q`, then in every runtime execution belonging to route `r`, **either** the evaluator does not evaluate the site at `q` at all, **or** `φ` is true of the working copy at the moment it does.

The disjunctive form is not a hedge. `§ 3A.6` gives the guarantee that shape, verbatim: *"where those premises are absent, the consequence is undetermined and is reported as such rather than evaluated."* A site that is not evaluated hosts no evaluation event, and the fault family's obligations are predicates over evaluation events (matrix § The cell, fault row: the obligation is *"the **catalog-declared safety precondition at the evaluation site**"*). The second disjunct discharges the fault guarantee exactly as the first. § *Where the argument cannot be made sound as stated* below states what the disjunct costs.

#### 11.1 Scope — what this argument does **not** establish

Named first, in the argument's own text, because an argument that quietly overreaches is the failure mode here.

1. **Entailment.** That `φ` implies the stamped safety predicate `R` at `q` (interval arithmetic, guard normal-form match, a callee attribute) is a separate premise. Two matrix arguments — *Arg-bound interval arithmetic* and *Guard-fact substitution* — now cite **this** rule for their corrected route clause, so this argument is **upstream** of them and may not lean on them. The stratification that makes "upstream" non-circular is stated in 11.5.
2. **Provenance admissibility.** That a fact of class X is available *at all* on route class Y — the earn table (§ 5) — is separate from whether it survives to `q`. This argument takes the earn point as given and defends survival. Consequences: the construction and restore carve-outs are not defended here, and **predecessor exclusion on inspect-fire routes is not defended here** (§ 5's flagged shrink; § 3A.6 reaches *absent* premises, and on an inspect-fire route the predecessor premise is *false*, not absent).
3. **Minting completeness.** That every evaluation occasion of every site mints an obligation at all. Transport moves a fact to a site; it says nothing about sites nobody minted. That is the matrix's open design hole (§ The cell: *"nothing detects an obligation that was never minted"*), and the 2026-07-21 citation duty is its partial closure, not this rule's.
4. **Adequacy.** That the stamped predicate makes the evaluator's fault unreachable.
5. **The number lane.** Ruled out of the whole design's scope (§ Scope). This argument's transport half is lane-independent (11.6); its E1/E2 establishment half touches value identity and is lane-sensitive, so it inherits the design's number-lane exclusion rather than claiming immunity.

The argument is also **relative** to two enumerations it does not itself establish: the writer table of `§ 3A.4` *What can write during one operation*, and the mention-set legs of the matrix § The minting rule. Both are named in 11.7.

#### 11.2 The failure condition, decomposed

Let `x` be a runtime execution on route `r` that evaluates the site at `q`. The claim fails there exactly when `φ` is false. Since the conclusion is reached only through transport from an earn at `p`, falsity requires one of:

- **W1 — the earn was wrong**: `φ` was not true at `p`.
- **W2 — the order was wrong**: `p` did not precede `q` in `x`.
- **W3 — an unenumerated or under-computed write**: something in `x` strictly between `p` and `q` changed a member of `deps(φ)` and was not in `mayWrite(r, p, q)`.
- **W4 — `φ` changed without a write**: `φ` is not a function of `deps(φ)`.
- **W5 — the wrong execution**: `x` does not belong to `r`, so `mayWrite(r, …)` was computed for the wrong write set.
- **W6 — the occasion is uncovered**: `q` was reached on an occasion the obligation's `(position, route)` key does not cover.

These six are exhaustive over the rules' premises. Each is closed as **impossible**, **excluded by the rule**, or **open with a stated side condition**.

**W1 — the earn.** By provenance class:

- *Ingress-governed argument fact.* Governance is unconditional at the door and precedes derivation: `§ 0.7`, verbatim — *"Every declared constraint is enforced on every value entering the entity from outside the definition — event arguments, construction inputs, direct field edits — at the moment it enters, before any computation derives from it"*; `§ 3A.4` phase 1 places it first. On an inspection route with the argument absent, the door did not run — but then any site reading the argument is *not evaluated* (§ 3A.6), so there is no occasion. **Closed**, with the dependency at 11.7(d): the evaluator must implement undetermined-value propagation, including through a slot written from an undetermined expression (see 11.4, W1-E2, and red-team Attack 4).
- *Fired-guard fact — commit routes.* A row's actions run only if its guard was true: `spec:1897` and `§ 3A.4` phase 2 (*"Guard evaluation selects; it writes nothing"*). **Closed.**
- *Fired-guard fact — inspect-fire.* Inspection simulates a row when its guard prospect is `Certain` or `Possible`, never `Impossible` (`evaluator.md` inspect path skips `Prospect.Impossible` rows before building a working copy). For a **conjunctive** guard, a field-only conjunct that is false forces the whole guard `Impossible` by Kleene monotonicity (`evaluator.md`: *"Missing args → Unknown → propagates via Kleene truth table to Possible"*, and a definite `false` dominates), so the row is skipped and the guard fact is not consumed on a false-guard route. For a **disjunctive** guard (`when Divisor != 0 or Flag`), a `Possible` verdict does *not* establish any single disjunct — but **no provenance in § 5 earns a fact from a disjunct**, so nothing is licensed to consume it and nothing faults. Both branches yield the claim; the load-bearing lemma is Kleene monotonicity for conjunctions, stated here rather than inferred. **Closed**, same dependency 11.7(d).
- *Predecessor exclusion on inspect-fire.* **Open** — 11.1 exclusion 2 and § 5's flagged shrink. This argument does not license it.
- *Position-zero declared fact.* On construction, position zero is the hollow default configuration whose satisfaction *is* the establishment obligation — **excluded by the rule** (no position-zero declared fact on construction). On restore, position zero was never established by any obligation in the file (`§ 0.7`, `spec:1969`, corrected `evaluator.md` key rule) — **excluded**. On fire / update / stateless-handler, the fact is valid over reachable configurations because the induction ranges over exactly those — the ground the ratified *Inductive hypothesis plus sign monotonicity* argument rests on. **Open, not closed**: that induction depends on premise (d)'s minting completeness (11.7(a)), which the matrix records as machinery that does not yet exist. Marked open rather than "closed conditional", because at post-plan sites this is the family's dominant premise.
- *Literal denotation / callee attribute.* Literal: the fact is the literal's value, a single-representation dependency the ratified *Literal constant-fold* argument already names. Callee attribute: a property of the callee, not of the entity's data, so no write moves it. **Closed by inheritance / impossible.**
- *E1 governed re-establishment.* Restricted to the update patch and to `F`'s **own declared modifier-constraints** (§ 5). The earn position is phase 5 (the value lands), not phase 1 (the check): `§ 3A.4` phase 1 checks the incoming value, phase 5 stores it; between them `F` holds its pre-patch value, covered by the position-zero fact on update routes. The two-branch argument (§ 4) yields `φ` at phase 5 whether or not the patch wrote `F`, and it is sound **without a must-run premise** precisely because on an update route nothing writes a field before phase 5 except the patch itself. The mentioning-constraint half is **not** defended (11.7(e)); the multi-field and computed-field over-reads are foreclosed by the § 5 narrowing, which is why red-team Attacks 1–3 reject.
- *E2 written-value provenance.* After `set F = e` that must run, `F` holds the value the evaluator computed for `e`, because `set` is full replacement (`proof-engine.md`: *"Full replacement … Every guard fact about the field is invalidated"*), evaluation is pure and deterministic, and `§ 3A.4` phase 5 stores it before any later read. E2's RHS facts are themselves transported and discharged at the write by this same rule, recursively; the recursion terminates because a route's construct list is finite (`spec:168`) and every recursive earn is strictly earlier in the total order. **Two residues flagged, not closed:** (i) the stored value must equal the evaluated value — I could not find a canon sentence stating that a `set` stores the evaluated value unmodified, and it fails across the `quantity`/`price` UCUM normalization boundary (`proof-engine.md` § Normalization boundary), so E2 licenses only facts invariant under the write's own transformation; (ii) the compiler's knowledge of `e` must be computed by the single shared constant-fold evaluator (`spec:213`) — `normal-form-draft-2026-07-19.md:35` records a live defect where the fold path divides integers as decimals, so E2 must not be cited for a fact whose derivation folded a division until that is rewired.
- *E3 must-run collection growth.* One added element guarantees `count > 0` (`spec:258`), restricted to grows that must run and provably precede — both now decidable from the phase order. **Closed.**
- *Residency fact.* Its **earn** provenance class is recorded unruled in the matrix, § Per-family case shapes: *"**Open**: whether the residency fact — the entity is in `S` — is itself available as a premise, and under which class, is not ruled."* The design puts the residency *coordinate* in the read-closure regardless (the sound direction — it only adds kills), but the fact's availability as an earned premise is **open** (Q22).

**W2 — the order.** The nine phases execute in a fixed order that never varies by operation (`§ 3A.4`, quoted in § 6). This is a **total order across phases**, refined within phase 5 by written chain order and within phase 1 by ingress-before-derivation. Two residues, both handled by refusing rather than assuming: two state actions on one state in one phase are mutually unordered (`§ 3A.4` *Still unsettled*), so neither transports to the other and — via the must-run rule — neither establishes; each is still totally ordered against every construct in every other phase. Intra-expression order (a sibling conjunct earning a fact for a fault site in the same expression, the `GuardInternalFaultSite` shape) has no canon short-circuit semantics for `and`/`or` — checked `precept-language-spec.md`, `proof-engine.md`, `evaluator.md`, not found — so the ordering premise refuses, the safe direction. On a `no transition` outcome, `spec:1915` removes phases 3, 4 and 6, which is what makes the discriminating pair's mirror accept. **The order is not unqualifiedly total**: restore slot injection is a writer the `§ 3A.4` table names but the nine-phase list does not place (see W3), so on a restore route it is unordered relative to every phase — which is sound because it only adds kills.

**W3 — an unenumerated or under-computed write.** `mayWrite(r, p, q)` is the union of the write targets of the route's constructs whose phase lies in the open interval `(p, q)`, minus those provably outside by chain order. *Coverage* rests on the `§ 3A.4` writer table (eight writers) plus the design's residency-coordinate extension. **This is the argument's residual soundness dependency and it is not closable here** — `§ 3A.4`, verbatim: *"**This enumeration is not yet enforced.** … any soundness argument that depends on the enumeration being complete should say so."* This argument depends on it and says so (11.7(b)). *Target correctness* is verified wrong once at the implementation level — BUG-033, the `into` target — so the rule is sound at the definition level (the writer and its two targets are enumerated) but only for a compiler whose target sets match the table. The residency coordinate is killed at **phase 4**, so a residency fact survives phases 1–3 (including exit actions) and dies before phases 5–8 — an interval the old partial order could not state. The read-closure `deps(φ)` closes under the matrix's three mention-set legs plus desugared cross-field modifiers plus the residency coordinate, and terminates because the computed-field graph is acyclic.

**W4 — `φ` changed without a write.** Impurity is impossible (`spec:170` — expressions observe nothing outside their evaluation context). Nondeterminism is impossible (`spec:96`). Abstraction drift is excluded by the rule transporting **concrete** facts and re-running abstraction at the consumption point — without this the `nonzero`-contributes-no-interval behaviour (`proof-engine.md:606`) compounds across positions. Structural absence under the `omit` reset sits on an **unruled conflict** (matrix § Edge cells records both reset-to-default and structurally-absent semantics as **Conflicted**); building a kill on one side is itself a ruling, so this is **open** (Q8) — and note the sign has flipped since the first draft: under reset-to-default the reset is fact-*establishing*, not merely a killer.

**W5 — the wrong execution.** An execution belonging to no route makes its obligations vacuously discharged, which the rule forbids: a site reached by no route is a **definition error** (§ Decision 7). One execution may belong to several routes — `InspectFire` dispatches all rows — which is sound provided the row simulations do not share a working copy; `evaluator.md`'s inspect path starts each row from `version.Slots.ToArray()`, an implementation reading named at 11.7(h). The descriptor is fixed before any fact is consumed: operation class, event and resident state come from the caller; row selection is first-match over a static list (`spec:1904`); guard evaluation writes nothing (`§ 3A.4` phase 2); outcome is syntactic once the row is fixed. The `outcome` component's self-transition refinement (§ 1) is required here: a self-transition and a cross-state transition have different phase-4 write sets, so folding them merges routes with different `mayWrite`. This is a use of the existing `outcome` component, not a new descriptor field.

**W6 — the occasion.** A site inside *k* nested quantifier bodies mints one obligation per `(position, route)`, universally quantified over the *k* bindings — sound because `spec:160` unfolds a quantifier to a finite acyclic conjunction/disjunction and purity gives one shared fact environment. Several occasions of one site on one route (two same-phase state actions, or an all-rows inspection reaching a shared post-plan site once per row) are each on a route and covered by the route-class quantification, conditional on `mayWrite` being computed over the union of everything the class admits.

#### 11.3 What the certificate records, and what the checker recomputes

Per transported fact, the certificate records: the route descriptor (with `outcome` distinguishing self-transition, and the row by a stable declaration identity); `φ` in normal form; `deps(φ)` as an explicit field set plus the residency coordinate; the earn class and establishing construct (or the token `position-zero`); the citation set the 2026-07-21 duty requires; the consumption position `q`; and the **phase-ordered walk** — per construct `(identity, phase, chain-ordinal or ⊥, write-target set, governed / ungoverned)`.

**The checker recomputes the walk; it does not accept the producer's list.** This is the load-bearing correction to Decision 6. Re-deriving *which routes reach a site* is proof-side work the checker need not repeat. Re-deriving *which constructs a named route contains* is a syntactic gather over declared states, rows and state actions, and it **must** be repeated, because it is the entire content of the frame: a producer that omits an exit action from the walk produces a certificate that replays perfectly and licenses a false frame — the exit-hook witness, and BUG-033 one level down. The gather is syntactic **except** where `from any` appears: whether `from any` attaches per state is unsettled (`§ Open questions`, spec: *"The spec does not say whether the `any` wildcard expands the same way"*), so the walk recomputation inherits that dependency (11.7(l)). Conservative reading while it stands: `from any` attaches to every state (the inclusion direction for kill, the exclusion direction for must-run).

#### 11.4 The methodological clause — not repeating the three-argument defect

The defect corrected in three ratified matrix arguments inferred an empty write prefix from the row's own write count (*"and for a single-write plan there are none"*), false because exit actions run at phase 3, before the row's chain at phase 5. This argument imposes on itself, and on any derivation citing it:

> **`mayWrite(r, p, q)` is computed by walking the route's construct list across every phase in the open interval `(p, q)`. It is never inferred from a property of the construct at `p`, of the construct at `q`, or of the row that contains either — not from its write count, not from its arity, not from its outcome.**

The only admissible narrowings of the interval are the phase order itself and `spec:1915`'s removal of phases 3, 4 and 6 on a `no transition` outcome — both properties of the **route**, not of a construct's text. `ExitHookTransportNoModifier` is the standing witness: phase 3 < phase 5, so the exit hook **provably** precedes the row's division.

One place the argument must not repeat the defect at one remove: **E1's second branch** ("if the patch did not write `F`, `F` is unchanged") is a frame claim, and it is discharged not by asserting a property of the patch but by observing that on an update route the phase-ordered walk contains **no** field writer before phase 5 other than the patch — so the frame over `(position-zero, phase-5)` is walked, not proxied. This is why E1 is stated for update routes specifically.

#### 11.5 The stratification that makes "upstream" non-circular

11.1 exclusion 1 says this argument may not lean on the three matrix arguments that now cite it, yet W1's position-zero case leans on the induction, whose preservation obligations are discharged by those very arguments. The cycle is broken by **induction on the number of committed operations**: position zero of operation *n* is the committed post-state of operation *n − 1*, and every transport use *inside* operation *n − 1* is complete before operation *n* begins (mutation atomicity commits *n − 1* whole, `spec:1967`). So transport's earn at operation *n* rests only on discharges finished at operation *n − 1*, never on a discharge at operation *n* that cites transport at operation *n*. The recursion is well-founded on operation count. This is stated because "upstream, may not lean" is otherwise false as written.

#### 11.6 Lane independence, and what it costs

The transport half uses determinism, purity, phase order, target-naming and enumerability only — no arithmetic identity, no rounding, no exactness — so it holds uniformly on the exact lanes (`integer`, `decimal`); the `number` lane is out of the design's scope (§ Scope). The **E2 establishment half touches value identity** (W1-E2 residue (i)), so E2 alone is lane-sensitive at the normalization and approximate boundaries. Since every fault cell cites transport, this materially limits how far the number-lane question reaches — but not to zero, and the E2 clause is where it does not reach.

#### 11.7 Open dependencies — everything the argument rests on that is not itself established

| # | Dependency | Status | Effect if it fails |
|---|---|---|---|
| (a) | **Minting completeness for the position-zero declared fact.** Valid only if every write site of every mentioned field carries a preservation obligation. | Machinery does not exist (`ProofRequirement` carries no establishment/preservation subtype); the 2026-07-21 citation duty is the local check that makes the gap visible. | Every proof citing a position-zero declared fact is void, while remaining individually replayable. This is why W1's position-zero case reads *open*. |
| (b) | **The in-operation writer enumeration is complete.** | **Asserted, not build-enforced** — `§ 3A.4`. Verified wrong once at the implementation level (BUG-033). | The frame step fails and every fault certificate is void. The residual soundness hole; disclosed at § 7, the seal, and the Principle-10 claim, never paraphrased into "canon closes the list." |
| (c) | **The mention set is the right read-closure.** | Owner-ruled 2026-07-20 (three legs + desugared cross-field modifiers); implementation sweep unverified. | `deps(φ)` too small; a write outside it kills a fact the rule transports. |
| (d) | **The evaluator implements undetermined-value propagation, including the slot round-trip.** W1's argument- and guard-fact inspection cases, and E2 on inspect routes (Attack 4), rest on `§ 3A.6`; the mechanism is `evaluator.md` § Component Mechanics. | Documented, conservative; the slot round-trip (a slot written from an undetermined expression must read back undetermined) is not separately stated in canon. | If a slot written from an unsupplied arg is read as its default rather than as undetermined, E2 on an inspect route transports a fact about a value that was never stored — the shape of Attack 4. |
| (e) | **What the editable-field door evaluates.** E1's mentioning-constraint half, and whether `runtime-api.md` § Update even runs constraint ingress on the patch (it lists a type check and a post-mutation sweep, no ingress evaluation of the field's modifiers). | `§ 0.7` and `what-i-want-2026-07-16.md:143` agree in prose; no runtime-surface contract states it for the patch path. | E1 shrinks to the field's own declared modifiers — which the two-branch argument covers unconditionally, and which is exactly the scope § 5 takes. If even the field's own modifiers are not evaluated at the patch door, E1 has no mechanism. |
| (f) | **Analyzer-matches-evaluator arithmetic identity**, for E2. | Live defect — `normal-form-draft-2026-07-19.md:35`, the fold path divides integers as decimals. | E2 transports facts computed under the wrong arithmetic. |
| (g) | **Single-threaded execution / no interleaving.** | No canon statement; grep for "concurren", "thread-saf", "interleav" returns zero hits. Assumption grounded in the immutable-version model. | An interleaved writer is an unenumerated write (W3). |
| (h) | **Inspection's row simulations do not share a working copy.** | `evaluator.md` inspect path — implementation reading, no canon sentence either way. | One route's writes leak into another's frame. |
| (i) | **Predecessor exclusion on inspect-fire.** | **Open** — 11.1 exclusion 2, § 5 flagged shrink, Q8/N8. | The argument does not cover it; it must be excluded from inspect-fire route sets or the site ruled out of the fault surface. |
| (j) | **The `omit` conflict.** | Unruled; matrix records it Conflicted (Q8). | Whether the reset is killer, establisher, or neither is undetermined. |
| (k) | **Count containment: killed vs advanced.** | Unreconciled between § 7's fact-shape table and `ProofRequirement.cs:215-224` (Q20). | One of the two rules is wrong for lower-bound count facts. |
| (l) | **`from any` desugaring.** | Spec Open Questions: *"The spec does not say whether the `any` wildcard expands the same way."* | Changes which constructs each route carries, and whether the walk recomputation (11.3) is a pure syntactic gather. |

#### 11.8 Where the argument cannot be made sound as stated

Three, stated rather than qualified away.

1. **Principle 10 is not claimable while dependency (b) stands.** Given closure of the writer list, a fact consumed at a fault site was true when the evaluator reached it. Without closure the frame step is unproven. A hole, not a "conservative boundary."
2. **E1's mentioning-constraint half is not sound as canon stands** (11.7(e)). E1 ships as the field's-own-modifiers clause; the side condition that would widen it is written down and routed (N5).
3. **Predecessor exclusion on inspect-fire routes is unsound as licensed by the shipped compiler** (11.7(i)). The side condition is route-restriction or an owner ruling on the fault surface of simulated non-selected rows.

Restore (Q5) is a fourth in practical effect, but there the argument is *complete* — it reaches "no fact transports on a restore route" under both readings of slot injection — and what is open is the owner's disposition of restore routes, not the argument.

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
| `docs/Working/obligation-discharge-matrix-2026-07-19.md` § Rule validity | One new named argument — *route-indexed frame, kill and establishment* — with its decision procedure and named open dependencies |
| same, § Vocabulary | A definition entry for "route", stated as a static equivalence class of occasions; a cross-reference from the write-plan entry to the fault-family answer it already delegates |
| same, § Per-family case shapes, fault row | The route axis added to the fault case shape's axis list — **owner's edit**, flagged not made |
| `…-cells/cell.schema.json` | Fault coordinate object gains a closed route-class field; contract entries gain a `mayWrite` metavariable slot **and an `establishedAt` slot plus the citation set** (E1/E2/E3 earns and the 2026-07-21 citation duty); the validity-argument registry gains the new name |
| `…-cells/fault-axis-disposition-map.csv` + meta | Regenerated over the new axis, with generated pruning derivations for the mechanically-unreachable combinations |
| `tools/Precept.MatrixTools` + tests | A relation-totality check (every site category has a written route set); a route-class validity check; a near-miss-with-non-empty-intersection requirement per fault cell |
| `docs/runtime/evaluator.md` § Constraint Evaluation Matrix | Fix the self-contradiction: the `Restore` **table row** still lists `always, in <current>` constraint plans while the key rule below says constraint evaluation is bypassed (§ 3) |
| `docs/compiler/proof-engine.md` | Doc-drift note against `:606`'s "genuinely lies within these bounds" once the field-modifier question is ruled |
| `docs/language/precept-language-spec.md` § 3A.4 | Implementation, not owner ruling: catalog-declare the five uncatalogued writers and apply the exhaustiveness analyzer to the write surface (the enumeration exists and is honestly labelled unenforced — Q2 is partly discharged) |

---

## Decisions

### Decision 1: A route is a static descriptor of an operation class, and route identity is descriptor equality

**Stakes**: high

- **Rationale**: The owner ruled the site is an *occasion*, which is a runtime notion; a compiler cannot enumerate runtime occasions. The descriptor is the smallest static object that determines which sites are evaluated and which constructs may write, and it is built only from things the language already declares statically — operation class, state, event, row, outcome, writable field set. Everything that could branch beyond those is either fixed by the descriptor or over-approximated in the kill direction. **Refined this pass** (no component added): the descriptor must additionally determine **phase membership** (which of `§ 3A.4`'s nine phases run), which is a function of `kind` + `outcome`; and `outcome`'s `transition T` case distinguishes a **self-transition** from a transition to a different state, because the two carry different phase-4 `omit`-reset write sets and different phase-6 membership.
- **Tradeoff accepted**: A route is *coarser* than an occasion. Two occasions on the same route with different pre-state values are one obligation, so a discharge that would need the pre-state interval to differ between them is refused. The fan-in witness in § Worked failures shows a shape where that coarseness may bind.
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

### Decision 2: The transport rule's ordering premise is the total order over the nine execution phases

**Stakes**: high

- **Rationale**: A kill set is not an ordering. Written without an ordering premise, the rule licenses a fact earned by a construct that runs *after* the consumption point, and § Worked failures' collection and guard-internal witnesses both exploit exactly that. The first draft used a *deliberately partial* order because canon had no normative phase sequence. Commit `33707e77` restored one: `§ 3A.4` *Operation execution order* states nine phases whose relative order *"never varies by operation"*. The ordering premise is therefore the **total order over those phases**, refined within phase 5 by written chain order and within phase 1 by ingress-before-derivation. The only residues are intra-phase (several state actions on one state) and one cross-phase writer the phase list does not place (restore slot injection); both are handled by refusal, not by an assumption.
- **Tradeoff accepted**: Two state actions on one state in one phase are mutually unordered and yield no earn between them, and intra-expression order is refused for lack of canon short-circuit semantics — precision losses in the safe direction. Nothing is unsound.
- **Alternatives considered**:
  - *Keep the deliberately-partial order.* Rejected: the phase order is now normative, so the partial order's soundness-dependency framing is obsolete. Pinning the order is monotone — it adds earns and shrinks the kill interval — so nothing previously licensed is lost, which the first draft's Falsifier 5 asked and this answers "no".
  - *No ordering premise, kill set only.* Rejected — unsound, with two witnesses.
  - *A total order read off `spec:2117` (the old inspection-section sentence) as the sole basis.* This was the first draft's rejected alternative; it is now **dead as a rejection**, because the order is normative in its own `§ 3A.4` section and both runtime docs defer to it. The parent design's Decision-3 argument for the total order is thereby vindicated, and this design says so rather than quietly dropping it.
- **Precedent**: the sequential-flow commitment already in canon — spec § 0.6 item 7, verbatim: *"Actions in a chain are sequenced — each subsequent action sees the proof state left by all preceding actions."* The rule generalises "preceding" from the chain to the phase order.
- **Sources consulted for this decision**:
  - `precept-language-spec.md § 3A.4`, *Operation execution order* — *"the relative order of the phases that do apply never varies by operation."*
  - `runtime-api.md` § Fire and `evaluator.md` § Working Copy Management — both now carry a note deferring to `§ 3A.4` as the normative order.
  - `precept-language-spec.md:164` — *"Each assignment in a row sees the state left by all preceding assignments."*
  - `precept-language-spec.md:1967` — *"Constraints are evaluated against the working copy after all mutations complete."*
  - **Spec-first check**: `§ 3A.4` *Operation execution order* now states the phase order normatively; the first draft's spec-first check (which found the spec silent) is stale, because `33707e77` created that section in the same window this design was drafted.
- **Strongest counter-evidence — now vindicated**: the parent design's preserved Decision-3 text argued the total order directly and called it *"the single most valuable thing this design fixes"*. The first draft disagreed that one inspection-section sentence was enough foundation; at HEAD the foundation is a normative section, so the parent design was right and this design adopts the total order.
- **Reversibility**: `Hard`. The order is now load-bearing in every fault certificate.
- **Blast radius**: every fault cell's transport citation; the matrix's argument section; no `src/` change.

### Decision 3: Earning a fact is restricted per route class; the inspection carve-out is removed, construction and restore survive

**Stakes**: high

- **Rationale**: The first draft carved out inspection routes for guard and argument facts, on the reading that inspection with a missing argument runs the guard as *Possible* and executes the chain anyway. `§ 3A.6` (added by `33707e77`) settles that differently: inspection is best-effort and **not exempt** — *"where those premises are absent, the consequence is undetermined and is reported as such rather than evaluated."* Where an argument or guard premise is absent, the site is not evaluated, so there is no occasion, so the fact needs no route restriction. The inspection carve-out is therefore **removed**: guard and argument facts are available on inspection mirrors exactly as on their mutating twins. Construction and restore carve-outs survive on independent grounds (position zero is the establishment obligation on construction, and was never established by any obligation on restore) — those are not inspection questions.
- **Tradeoff accepted**: The owner took a third path from the two the first draft weighed. Not "route-restrict the two classes" and not "make incomplete inspection a caller error" — instead **best-effort inspection with undetermined propagation**, which keeps the caller path supported *and* keeps the proof. Two residues remain, re-routed separately: **predecessor exclusion on inspect-fire** is a *flagged shrink* (§ 5, N8) because its premise is *false*, not absent, so § 3A.6 does not reach it; and inspect-update's hypothetical patched state (Q18).
- **Alternatives considered**:
  - *Leave the classes route-blind.* Rejected: unsound.
  - *Route-restrict guard and argument facts on inspection* (the first draft's Decision 3). **Superseded** by § 3A.6's best-effort ruling — the restriction is unnecessary once undetermined propagation is the mechanism.
  - *Rule incomplete-argument inspection a caller error* (the first draft's routed alternative). **Not** the resolution the owner took; recorded so nobody re-adopts it.
  - *Drop restore from the route set entirely.* Still open — `§ 0.7` arguably puts it outside the guarantee; routed as Q5, because "include more sites" is not the conservative direction under an exact-power contract.
- **Precedent**: `§ 3A.6`, verbatim, on the best-effort guarantee; and the matrix § Vocabulary door-makes-the-premise-true sentence for construction/restore.
- **Sources consulted for this decision**:
  - `precept-language-spec.md § 3A.6` — *"A proof discharged from an argument constraint or a guard stays discharged: where those premises are absent, the consequence is undetermined and is reported as such rather than evaluated."*; *"the fault guarantee (§0.1 principles 10 and 11) holds during inspection exactly as during execution."*
  - `evaluator.md` § Constraint Evaluation Matrix — `| InspectFire | no | yes (all rows) | same as Fire, but evaluated for every row |` (the ground for the predecessor-exclusion shrink).
  - `precept-language-spec.md § 0.7`, `spec:1969`, corrected `evaluator.md` key rule — restore is trusted, not re-validated.
  - matrix § The cell, 2026-07-21 — the citation duty, class (a) not exempt.
- **Strongest counter-evidence**: `spec:2119`'s *"matches what execution would produce for the same inputs"* read strongly says inspection has no wider fault surface — which is now consistent with the removal, because § 3A.6 makes the fewer-inputs case *undetermined and not evaluated* rather than a wider surface.
- **Reversibility**: `Hard` for the construction/restore carve-outs; the inspection removal is a widening (fewer restrictions).
- **Blast radius**: guard and argument facts are the family's dominant premises; the route relation; the matrix's argument section; the one flagged shrink (predecessor exclusion) is routed separately.

### Decision 4: Kill on may-write; earn on must-run and must-precede; a governed write preserves

**Stakes**: medium

- **Rationale**: Over-approximation is safe in exactly one direction. A guarded construct that may have run must be in the kill set, and the same construct must not establish anything. **Extended this pass to three cases**: (i) an ungoverned may-write kills; (ii) a must-run establishing write is a new earn (E2/E3); (iii) a **governed** write (the update patch on an editable field) *preserves* the field's own declared modifiers, with **no must-run premise required** — because on an update route nothing writes a field before phase 5 except the patch, so the two-branch argument (patch wrote F: governance made the modifier true; patch did not write F: F is unchanged from position zero) yields the fact either way. This is why E1 is exempt from the must-run requirement that binds E2 and E3.
- **Tradeoff accepted**: Precision loss wherever a guarded write is in fact the only write; the author's respelling is an unguarded write or a guard at the reading site. The governed-preserve clause is deliberately narrow — the field's own single-field modifiers only, not multi-field constraints mentioning the field (which depend on the unsettled multi-field governance evaluation config, N5).
- **Alternatives considered**: *Apply may-write symmetrically to earn and kill.* Rejected — a guarded grow that may not have run would establish `count > 0`. *Track guard feasibility to decide must-run.* Rejected here: it needs a satisfiability scan per construct pair, and the matrix's dead-row scan is scoped to rows, so licensing it is a separate widening. *Smear the governed exemption into `mayWrite`.* Rejected — it would make the certificate step unreplayable; the governed write is a re-earn at its position, not a hole in the frame condition.
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
- **Amended this pass**: the discharge contract gains a second metavariable, `establishedAt` (the establishing construct for an E1/E2/E3 earn, which is neither position zero nor a guard), plus the citation set the 2026-07-21 duty requires. Stale reachable/defined prose estimates are removed — the figures come from regeneration or not at all (§ 9). The collapse expectation is raised: restore now leaves every constraint-evaluating category and the inspection mirrors coincide with their twins except at predecessor exclusion.
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

### Decision 6: The certificate carries the route descriptor and the phase-ordered, governance-flagged write list; the checker recomputes the walk

**Stakes**: medium

- **Rationale**: The checker must validate the transport premise without re-deriving *which routes reach a site* (proof-side work) — but it **must** recompute *which constructs a named route contains*, because that gather is the entire content of the frame, and a producer that omits an exit action from the list produces a certificate that replays perfectly and licenses a false frame (the exit-hook witness; BUG-033 one level down). The list is **phase-ordered** and each entry carries a **governed / ungoverned** flag so the checker can replay the E1 preserve clause without re-deriving which doors are governance doors. Using a *stable* row identity rather than a positional index means inserting a row above another does not silently re-bind an existing certificate.
- **Tradeoff accepted**: Certificates grow by a class tag, a row identity, and a phase-ordered governance-flagged list; replay of existing certificates breaks. The walk recomputation is a syntactic gather except where `from any` appears, whose per-state desugaring is unsettled (Q14) — so the gather inherits that dependency.
- **Alternatives considered**: *Let the checker re-derive the whole trace from the descriptor.* Rejected: it requires the descriptor to carry hook guards, which is exponential in declared guards. *Trust the producer's list entirely* (the first draft's Decision 6 wording, "never re-derives … hook attachment"). **Corrected**: it lets a producer omit a writer and license a false frame; the distinction between re-deriving routing (not needed) and recomputing a route's construct set (required) is the fix.
- **Precedent**: `what-i-want-2026-07-16.md:206`, verbatim: *"The certificate records the finished proof step by step, and the checker simply walks those steps and confirms each one … Every step is re-verified; the figuring-out is never repeated. That's why checking stays fast and the checker stays small."*
- **Sources consulted for this decision**:
  - `docs/Working/what-i-want-2026-07-16.md:206` — as quoted above
  - `docs/Working/what-i-want-2026-07-16.md:199` — *"the compiler emits a **certificate**: a checkable record of the proof — per obligation, the theorem, the premises used (which guard, which arg constraint, which pre-state rule), and the derivation."*
  - matrix § The cell — *"the cell triple is the certificate's *schema and index* … The matrix must never be read as fixing the certificate's payload granularity."*
  - **Spec-first check**: grepped `precept-language-spec.md` and `proof-engine.md` for certificate payload requirements. § 0.6 proof philosophy #3 requires a certificate *"drawn from a small, spec-enumerated vocabulary (the `CertificateSteps` catalog) that an independent checker can replay"* but does not enumerate its fields. **Spec is silent on the payload; the want doc is the authority and is quoted.** Pinning the transport step into the closed step-kind vocabulary requires reading `certificate-steps-membership-2026-07-12.md`, which this pass did not read — recorded as a dependency, not assumed.

### Decision 7: A site reached by no route is a definition error, and the stateless and unmatched classes are enumerated so that never fires spuriously

**Stakes**: medium

- **Rationale**: Under a route-indexed rule, an empty route set makes every obligation at that site vacuously discharged — the exact silent-pass shape the whole definition exists to prevent. Treating it as an error makes the failure loud. The two enumeration rules that go with it exist so the error means what it says: a stateless precept must produce fire-class routes, and a group whose guards can all fail must produce the unmatched class. **Verified this pass** that removing restore from the constraint-evaluating site categories (§ 3) orphans no category: every such category retains fire, update or construction routes — checked against the `evaluator.md` Constraint Evaluation Matrix, where the `always`, `from <current>`, `on <event>`, `to <target>` and `in <current>` buckets each have at least one non-restore operation.
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
2. **If, after the route relation is written, more than one in five fault-prone programs in `samples/` loses a discharge it has today with no licensed respelling**, the earn restrictions are over-strict. *Re-scoped this pass*: with the inspection carve-out removed, the two remaining candidate over-narrowings are **predecessor exclusion on inspect-fire** (N8) and **restore** (Q5); the falsifier applies to those, not to the general inspection case.
3. **If the collapse measurement shows fewer than half of today's defined cells collapse to a single route-class range**, the route class is doing too much work as a cell axis and the contract should carry it as a metavariable the way the write set does.
4. **If a fault cell can be constructed that satisfies all four premises on its route and still licenses a runtime fault**, the writer-closure dependency has bitten and the rule's soundness argument is void until canon closes the list.
5. **~~Falsifier 5 (phase-order rejection) — resolved *no*.~~** Pinning the operation's phase order is monotone in both halves — it adds earns and shrinks the kill interval — so it cannot reject anything the partial order accepted (§ 6). Recorded resolved rather than left live.

---

## Acceptance criteria

Test-shaped. "Cell validator" means the day-one validator the matrix's § Storage specifies.

1. **The discriminating pair.** The exit-hook program and its `no transition` mirror (§ Worked failures) are compiled and verdicted from diagnostic codes and obligation records. Under the design the first **rejects** naming the route and the second **accepts**. *Today both accept identically with `strategy: GuardInPath` — recorded as observed in this pass, and the criterion is the conformance test any implementation must pass.*
2. **Route relation totality.** A test asserts every one of the twenty-three evaluation-site categories in `fault-axis-disposition-map.csv` has a written route set, and that every route class named is drawn from the closed operation vocabulary. Adding a twenty-fourth category **fails** until it is given a route set.
3. **Empty route set rejects.** A fixture whose site is reached by no route **fails** validation with a pruning derivation rather than passing silently. A stateless fixture with a division in an `on Event` chain **passes** — i.e. it does not trip criterion 3.
4. **The ordering premise is exercised.** A fixture cell whose fact is earned by a construct that provably runs *after* the consumption point (the collection witness, now a *known* wrong order under the phase order) **fails** to discharge.
5. **Inverted this pass — inspection mirror passes; predecessor exclusion on inspect-fire fails.** A fixture cell citing a fired-guard fact whose route set includes the inspection mirror now **passes** validation (the carve-out is gone). The failing fixture is a *predecessor-exclusion* cell with an inspect-fire route in its set — it **fails** until the route is excluded or the site ruled out of the fault surface.
6. **Kill-set completeness per witness.** Every fault cell carries a near-miss whose intersection with the write set is **non-empty**, so the kill half of the rule is exercised and not merely asserted.
7. **Certificate replay does not re-derive routing.** A checker fixture is given a certificate whose write-target list disagrees with the program's actual hook attachment; the checker **rejects** it rather than recomputing and agreeing.
8. **Coordinate regeneration.** The enumeration is regenerated over the new axis and the validator reports disposition totality over the enlarged product, with every mechanically-pruned coordinate carrying its generated one-line derivation.
9. **Doc-sync.** The matrix's § Rule validity carries the new argument with its named side conditions; the write-plan entry carries a cross-reference to it; a test asserts the argument name appears in both the prose and the schema registry.
10. **Respellability is measured, not asserted** — per the matrix's hard gate: *"Paper-only verdicts do not ratify."* The earn restrictions' cost is classified against the sample corpus before any affected family ratifies.

---

## Dependencies

**Upstream — must be in place first**

- Owner rulings on the blocking items in § Open questions: the writer-closure sentence (the rule's residual soundness dependency), the restore disposition, and the predecessor-exclusion-on-inspect-fire shrink. The phase order, the inspection carve-out, and the in-plan substitution provenance are **settled since the first draft** (see § Open questions) and are no longer upstream blockers.
- Reading `certificate-steps-membership-2026-07-12.md` before the transport step is pinned into the closed certificate vocabulary. **Not read in this pass** — the largest unread dependency here.
- A `/research` pass on trace partitioning (Rival and Mauborgne's abstract domain), for which no in-tree research file exists.

**Downstream — what this enables**

- Every fault cell's transport citation, which is currently resting on a rule the site-identity ruling invalidated.
- The coordinate regeneration the matrix's § Storage already ruled, which needs the new axis before it can run.
- The cross-route generalisation (dominator-based propagation), which is the natural power-widening once the per-route rule is settled.

---

## Doc-update enumeration

Per the CLAUDE.md routing table.

| Doc | What changes |
|---|---|
| `docs/Working/obligation-discharge-matrix-2026-07-19.md` § Rule validity | The *route-indexed frame, kill and establishment* argument, with its decision procedure and named open dependencies |
| same, § Vocabulary | A "route" entry; a cross-reference from the write-plan entry to the fault-family answer it delegates |
| same, § Per-family case shapes (fault row) | The route axis added to the fault case shape — **owner's edit**, flagged not made |
| `docs/Working/…-cells/cell.schema.json` | Route-class coordinate; `mayWrite` metavariable slot; `establishedAt` slot + citation set; argument-registry entry |
| `docs/Working/…-cells/fault-axis-disposition-map.csv` + meta | Regenerated over the new axis |
| `docs/compiler/proof-engine.md` § Strategy 2 / § Sequential proof flow | Cross-reference to the route-indexed rule; the `:606` doc-drift note once the field-modifier question is ruled |
| `docs/language/precept-language-spec.md` § 3A.4 | Catalog-declare the five uncatalogued writers and apply the exhaustiveness analyzer to the write surface (Q2 — implementation, since the enumeration already exists) |
| `docs/runtime/evaluator.md` § Constraint Evaluation Matrix | Fix the self-contradicting `Restore` **table row** (still lists `always, in <current>`) against the key rule that says constraint evaluation is bypassed |
| `docs/language/precept-language-spec.md` § Open Questions | The stale writer note still says *"at least four further writers exist outside it"* and cites recomputation as *"phase 4"* — recomputation is phase **7** and the table enumerates **eight** (drift found this pass) |
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

*Under this design:* one fire route with a transition outcome, so the exit hook is on its construct list. The read-closure of the guard fact is `{Divisor}`; the exit action is at **phase 3** and the row's division at **phase 5**, so the hook **provably precedes** and is strictly inside the kill interval. The fact is **dead**, no other provenance names `Divisor`, and the definition rejects naming the route. **The verdict is now determinate on a known order** (phase 3 < phase 5), not order-agnostic — the first draft's parenthetical about the phase-order silence is moot.

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

*Under this design:* **accept** on the fire route. `spec:1915` removes phases 3, 4 and 6 (exit actions, the state change and `omit` reset, entry actions) from a no-transition operation, so the hook is not on the route at all, the kill set is empty, and the guard fact transports. Its **inspection mirror also accepts** now that the carve-out is gone (§ 5), so the discriminating pair is no longer contaminated by an inspection-route rejection. Two programs differing by two words, **opposite designed verdicts and identical current verdicts.** This pair is the sharpest available conformance test for any implementation.

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

*Under this design:* **the witness is retired as a rejection witness.** Under `§ 3A.6`, when inspection is called with no arguments the guard's premises are absent, so the division's consequence is *undetermined and not evaluated* — there is no evaluation occasion to be unsafe. `InspectArgHole` is re-purposed as the worked illustration of the best-effort ruling, not as a program the design must reject. The remaining dependency is that the evaluator actually withholds evaluation on an absent premise, including a slot written from an undetermined expression (§ 11.7(d)); red-team Attack 4 is the sharp case.

### The stateless mirror — no guard, no hook, and still a hole

The `StatelessArgDivide` sample in § 2. *Today:* zero diagnostics, `Proved / CompositionalConstraint`. The row is unguarded, so on the inspection path the guard prospect is *Certain* and execution proceeds with empty arguments. *Under this design:* **accept** — on the same § 3A.6 ground as `InspectArgHole`. On a route where the argument is not supplied there is no evaluation occasion at all, so the first draft's gloss (*"true on routes where the argument was supplied"*) is superseded: the site is simply not evaluated. This is the case `what-i-want-2026-07-16.md:104` rests its worked example on — *"At runtime there is no zero-check anywhere — the ingress validation of the arg is what makes the division safe."*

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

*Under this design:* the `dequeue` is at **phase 3** and the establishing `enqueue` at **phase 5**, so the grow provably does **not** precede — `p ⪯ᵣ q` fails on a *known* order, not an unknown one. The correct outcome is **reject**. This witness is re-labelled: it no longer shows that the phase-order silence is a soundness dependency (there is no silence), it shows that the phase order is load-bearing **and supplied**.

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

**The finding that blocked the first draft is now discharged.** The matrix's own illustration reads, verbatim: *"A division inside `in S ensure` reached by two transitions into `S` is two obligations: one may discharge from the first row's guard, the other from the second row's argument constraint."* The first draft found the second discharge *not constructible*, because a state-anchored ensure reads the field `CoverageLimit`, not the argument, and there was no in-plan substitution provenance. **E2 licenses it.** `set CoverageLimit = Reassess.Limit` (phase 5) is a must-run write whose RHS carries the arg's `positive` fact, so `CoverageLimit positive` is earned at that write and transports to the ensure's division at phase 8, with nothing in between writing `CoverageLimit`. The proof names the write *and* the arg's ingress governance, per the citation duty. The ruling's own motivating example is now expressible — E2 (the 2026-07-21 written-value ruling) is exactly the provenance the first draft was missing.

---

## Red-team attacks and how the design answers them

Five constructed programs, every one syntax-validated through `precept_compile` at HEAD; the compiler output is reported as observation only. Attacks 1, 2 and 3 compile clean today (`Proved`) and force a rule change; 4 and 5 reject today for unrelated reasons and are design-only witnesses of the corrected rule.

**Attack 1 — E1 re-establishing a constraint that mentions a computed field (forced a rule change).**

```precept
precept UpdateGovernedComputed

field Divisor as integer default 5 editable
field Base as integer default 1 nonnegative editable
field Twice as integer <- Base * 2
field Ratio as decimal <- 100.0 / (Divisor - Twice)

rule Divisor > Twice because "The divisor must stay above the doubled base so the ratio is well defined"

state Active initial
state Closed terminal
event Finish
from Active on Finish
    -> transition Closed
```

*Today:* `success: true`, `Proved / FlowNarrowing`. *The fault:* on `Update` with patch `{Divisor: 6, Base: 3}`, ingress at phase 1 checks `Divisor > Twice` against the *stale* `Twice = 2` (6 > 2 true), phase 5 stores the patch, phase 7 recomputes `Twice = 6`, and `Ratio = 100.0/(6-6)` divides by zero. A naive E1 that re-establishes "every rule mentioning `Divisor`" would license this. **The design answers it two ways, either sufficient.** First, E1 is scoped to the field's **own declared modifier-constraints** (§ 5, Decision 4); `Divisor > Twice` is a multi-field rule mentioning a computed field, not a modifier on `Divisor`, so E1 does not re-establish it — the fact is killed by the patch write and the obligation does not discharge. Second, the computed-field kill now fires **at phase-7 recomputation as well as at the earliest input write** (§ 7), closing the `[5, 7]` gap the first draft's kill-relocation left open once earns moved to phase 5.

**Attack 2 — the stateless instance of Attack 1.** The same fields and rule with no states (`StatelessGovernedComputed`) also compiles clean today. It is the minimal setting — patch at phase 5, recompute at phase 7 — and the same two fixes answer it, so the defect cannot be dismissed as a state-machine edge case.

**Attack 3 — two governed writes in one patch, each check reading the other's stale value.**

```precept
precept UpdateTwoEditableStaleCheck

field Alpha as integer default 5 editable
field Beta as integer default 2 editable
rule Alpha > Beta because "Alpha must stay above Beta so the spread below is well defined"
state Active initial
state Closed terminal
in Active ensure 100.0 / (Alpha - Beta) > 1.0 because "The spread share must exceed one percent"
event Finish
from Active on Finish
    -> transition Closed
```

*Today:* `success: true`, `Proved / FlowNarrowing`. *The fault:* patch `{Alpha: 4, Beta: 4}` — `Alpha` checked against pre-patch `Beta = 2`, `Beta` checked against pre-patch `Alpha = 5`, both pass, post-patch `Alpha - Beta = 0`. `Alpha > Beta` mentions both stored fields present in the patch, so the "constraints whose mentions are all stored fields present in the patch" rule alone does **not** catch it. The design answers it with the same E1 narrowing as Attack 1: `Alpha > Beta` is a **multi-field rule**, not a declared modifier on either field, so E1 does not re-establish it. The mentioning-constraint half — which would need governance to evaluate against a fully-patched configuration — is explicitly **not** defended (§ 5, N5); until that config question is ruled, `Alpha > Beta` is not an E1 fact and the ensure's division rejects.

**Attack 4 — E2 across an inspection route (design-only; forced a named dependency, not a rule change).**

```precept
precept InspectArgIntoField

field Months as integer default 0
field Result as decimal default 0.0
state Active initial
state Done terminal
event Plan(Term as integer positive)
from Active on Plan
    -> set Months = Plan.Term
    -> set Result = 100.0 / Months
    -> transition Done
```

`InspectFire("Plan")` with no arguments: `set Months = Plan.Term` has an unsupplied input, so `Months` is not written; `set Result = 100.0 / Months` has all-field inputs, so an input-keyed evaluator evaluates it and divides by the default `0`. E2 would move `Term positive` into `Months` and carry it to the division. **The design does not change the transport rule** — E2's premise (the fact of `Plan.Term`) is itself *absent* on the no-args route, so E2 earns nothing there. What it adds is a **named soundness dependency** (§ 11.7(d)): the evaluator must key undetermined propagation on the premise set and store an undetermined slot as undetermined, so `set Result = 100.0 / Months` reads `Months` as undetermined and is not evaluated. The gap is between `§ 3A.6` (premise-keyed norm) and `evaluator.md`'s input-keyed mechanism, and it is routed as a doc-sync / evaluator item, not settled here.

**Attack 5 — multi-exit-action establishment (design-only; forced the must-run clause).**

```precept
precept MultiExitEstablish

field Divisor as integer default 0
field Note as string optional
field Slot as integer default 0
state Open initial
state Closed terminal
event Close
from Open -> set Divisor = 5
from Open -> set Note = "leaving open"
from Open on Close
    -> set Slot = 100 / Divisor
    -> transition Closed
```

Two exit actions on `Open`; one sets `Divisor = 5`. A naive reading — "the exit action at phase 3 establishes `Divisor = 5`, the kill interval `(3, 5)` is empty because the sibling writes `Note`" — would license the division. The design answers it with the **must-run half of Decision 4**: `§ 3A.4`'s *Still unsettled* note leaves *"whether all of them fire"* open, so neither of two state actions on one state is must-run, so neither establishes anything at **any** phase. `set Divisor = 5` establishes nothing; `Divisor` may hold its default `0`; the division rejects. (Adjacent: state actions are guardable — `spec:960` — so "declared on the route" is never a must-run test even for a singleton; the singleton unguarded case rests on `§ 3A.4` phase 3's unconditional *"are applied"*, and if the owner rules firing unsettled even for singletons, every state-action-sourced earn disappears while the kill half is unaffected — flagged in § 11 and N-singleton.)

**Attacks that held (no rule change).** A guard fact consumed after an exit action (`ExitHookTransportNoModifier`) rejects on a known order. BUG-033's `into` target is sound at the definition level (E2 is deliberately not extended to it, N1). The `omit` reset (phase 4) between a phase-2 guard fact and a phase-5 site kills correctly. A stale mid-plan computed-field read is killed at the earliest input write — the one place the kill-relocation helps, which is why the fix *adds* the phase-7 kill rather than moving it. A disjunctive guard on an inspect-fire route faults at runtime but no provenance earns a fact from a disjunct, so the rule licenses nothing. Restore routes and construction/self-transition entry firing hold as specified.

---

## Open questions

Nothing in the *Blocking / Narrowings / Widenings / Canon silences / Scope gaps* groups below is settled. Each row names the class of amendment it falls under, so the owner can see which are soundness corrections (owner-reserved, witness supplied) and which are widenings (cheap and monotone). The governing text is the matrix, § Amendments, verbatim: *"Shrinking the licensed set is permitted **only** under this class. Always a major definition version; always breaks certificate replay for affected cells; always routed to the owner with the **witness program** demonstrating the unsoundness. Never folded silently into a widening or a refactor."*

### Settled since the first draft — do not re-open

`33707e77` closed four of this document's own questions. Recorded here so nobody re-opens them:

- **Q1 (the operation's phase order)** — **closed.** `§ 3A.4` *Operation execution order* states the nine-phase order normatively. Only two narrower residues remain, moved to Q15/Q16: multi-action-state multiplicity, and construction/self-transition entry firing.
- **Q3 (in-plan substitution provenance)** — **closed** by the matrix's 2026-07-21 written-value ruling (clause E2). The fan-in motivating example is now constructible (§ Worked failures).
- **Q4 (the inspection carve-out)** — **closed** by `§ 3A.6`'s best-effort ruling: the carve-out is removed, not adopted (Decision 3). Its two witnesses (`InspectArgHole`, `StatelessArgDivide`) are retired as rejection witnesses.
- **Q6 (the three-argument soundness correction)** — **approved** in the matrix and applied to all three arguments (`33707e77`).
- **Q7 (the position-zero seal)** — **corrected**, not owed: the seal is rewritten in § 10 with E1/E2 (`§ 0.7`).
- **Falsifier 5 (phase-order rejection)** — **resolved *no*** (§ 6): pinning is monotone.

### New questions these corrections open

- **N1. Does E2 extend beyond `set`?** The ruling's text is *"After `set F = <expr>`"*. Two uncovered site classes: `dequeue Q into F` (the value is the collection head, and the `into` target is BUG-033's blind spot) and `clear F` (which establishes a *presence* fact, `F is not set`). Both are minted today. *Options:* license each / decline. **Widening if licensed, but the `into` extension would build directly on BUG-033's defect and should not be done casually.**
- **N2. Does a computed field's own discharged `IntervalContainment` obligation re-establish its declared bounds at phase-7 recomputation?** (`proof-engine.md`: *"A computed numeric field carrying explicit declared bounds gets an `IntervalContainmentProofRequirement` against its own bounds."*) If so, recomputation is a *preserving* writer for the field's own bounds, an E1-shaped clause with a non-ingress establisher. *Options:* license / decline.
- **N3. Do default materialization and the `omit` reset establish the field's declared facts by the already-discharged establishment fold?** (matrix § Validity arguments, *Literal constant-fold for defaults*.) The `omit` half is gated on the unruled `omit` conflict (Q8). Note the sign flip: under reset-to-default the reset is fact-*establishing*, not merely a killer.
- **N4. E1's composition boundary.** A governed write to `F` re-establishing a constraint mentioning `F` and `G` is killed normally by a later write to `G`. Stated so E1 is not over-read as "governed once, true forever". (Applies only if N5 later widens E1 past single-field modifiers.)
- **N5. What does the editable-field door evaluate for a multi-field patch** — every mentioning constraint against the fully-patched configuration, or only the field's own modifiers? E1's two-branch argument holds unconditionally for the field's own modifiers; the mentioning half depends on this. Runtime-surface question; `runtime-api.md` § Update shows no constraint ingress on the patch path at all, which is the prior question. *Options:* rule the config / restrict E1 to own-modifiers permanently.
- **N6. What does an E1 proof cite?** The citation duty is phrased over **obligations**, but a governed entry point is a **discharge mechanism**, not an obligation (matrix § Vocabulary, premise-class-(e) retirement). The citation vocabulary needs a mechanism-shaped entry.
- **N7. Do the inspection mirrors still warrant separate route classes,** now that their fact environments coincide with their mutating twins — or only separate *reachability* (all-rows dispatch, inspect-update's prospect)? This decides whether the route-class axis is 8 or 6, and therefore the coordinate product.
- **N8. Predecessor exclusion on inspect-fire** (§ 5's flagged shrink). Its premise is *false*, not absent, so § 3A.6 does not rescue it. *Options:* remove it from inspect-fire route sets (a shrink, witness owed) / rule a simulated non-selected row outside the fault surface.
- **N-singleton. Is a lone unguarded state action must-run?** The design reads `§ 3A.4` phase 3's *"are applied"* as unconditional for a singleton and the *Still unsettled* doubt as scoped to *"more than one"*. If the owner rules firing unsettled even for singletons, every state-action-sourced E2/E3 earn disappears (the kill half is unaffected). Flagged because it is the same shape as the corrected defect (a semantic property read off a syntactic count).

### Blocking — the rule cannot be written down without these

**Q2. Is the in-operation writer list closed?** This is the design's residual soundness dependency and the one blocking item that survives `33707e77`. `§ 3A.4` *What can write during one operation* enumerates eight writers and states, verbatim: *"**This enumeration is not yet enforced.** … this table is asserted rather than kept, and any soundness argument that depends on the enumeration being complete should say so."* *Options:* (a) catalog-declare the five uncatalogued writers and apply the exhaustiveness analyzer to the write surface, making an added writer a build failure — now an **implementation** item, since the enumeration itself exists; (b) accept the gap and carry it inside the validity argument permanently. *Cost:* (b) means the rule ships with a named hole that voids every fault certificate if one more writer exists — which is why the disclosure is repeated at § 7, the seal, and the Principle-10 claim. The first draft's request for "one normative sentence closing the list" is **partly discharged**: the sentence exists and is honestly labelled unenforced, so the residual is the implementation work of (a), not an owner ruling.

### Narrowings — owner-reserved, each with a witness

**Q5. Restore.** *Options:* (a) exclude declared facts at position zero on restore routes exactly as construction does (already in force in § 5); (b) drop restore from the route set and declare it outside the fault guarantee, which `spec § 0.7`'s boundary paragraph arguably already does — *"Restored state is trusted as valid at the time it was persisted … the runtime fault traps backstop any out-of-contract value the evaluator would otherwise fault on"*; (c) reconcile the evaluator-doc table-row drift (§ 3, § Doc-update enumeration) first and decide after. *Cost:* (a) rejects essentially every computed field containing a division on a restore route; (b) admits an uncovered fault path but matches canon; note that "include restore and reject more" is **not** conservative under an exact-power contract, because an over-mint is a false rejection. Now better grounded: `§ 3A.4`'s writer table names restore slot injection as an ungoverned writer and computed-field recomputation as running *"in every operation, including `Restore`"*.

**N8 (predecessor exclusion on inspect-fire)** is the other owner-reserved shrink — stated in full in *New questions these corrections open* above.

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

**The strongest objection is Q2.** The rule's soundness rests on a writer-closure that canon states but does not build-enforce. The phase order (now normative) removes the phase-order dependency entirely, but nothing removes the closure dependency. If one construct writes during an operation that the `§ 3A.4` writer table does not enumerate, the frame step fails and every fault certificate is void. The `§ 3A.4` table exists and is honest about being asserted-not-kept — but "the table is complete" is exactly the claim it declines to make, and BUG-033 is a verified instance of a consumer getting a *listed* writer's targets wrong.

**The ledger is now roughly net-neutral, not net-negative.** The first draft removed licensed programs at post-plan sites and added them only in the fan-in shape, because its seal killed every declared fact about any written field. **E1 and E2 recover most of that** — a governed edit re-establishes the field's own modifiers, and a `set F = e` carries `e`'s facts forward — so the obsolete "the seal kills every declared fact and no substitution rule replaces it" objection is deleted, not softened. What remains removed: cross-field constraints mentioning a written field (until N5), and any program that leaned on a fact crossing a governed write it was strictly stronger than.

**The cell arithmetic is not published.** The written coordinate product size stands, but the reachable/defined estimates are dropped (§ 9): the figure comes from regeneration, not prose, because the collapse across route classes is generated data and materially larger than the first draft implied.

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
