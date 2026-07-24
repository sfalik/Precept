---
status: Draft — UNLOCKED 2026-07-23 by adversarial review; see § Review record. Was `Locked 2026-07-23` for a few hours; do not build against it.
phase-target: TBD — follows the linkage design; both precede the fault-family re-ratification
comparable-systems-research-status: strong
sources-consulted:
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Constraint kinds — the five constraint forms and their per-kind obligation shapes
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Vocabulary — write plan, discharge contract, premise classes, sound-but-unprovable band
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § The minting rule — the three-leg mention set, activation sites, site identity
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Rule validity — the standard every discharge rule's argument must meet
  - docs/Working/obligation-discharge-matrix-2026-07-19.md § Amendments — power-widening vs soundness-correction
  - docs/Working/obligation-linkage-and-completeness-2026-07-23.md — the citation and completeness machinery this design plugs into
  - docs/Working/what-i-want-2026-07-16.md — the seed model: base case, inductive step, symmetric obligations, modifiers as sugar
  - docs/Working/ensure-vs-rule-analysis-2026-07-20.md — why an ensure is not a conditional rule over a state coordinate
  - docs/Working/certificate-steps-membership-2026-07-12.md Decision 1 — the eleven premise kinds, incl. the compiler-derived ReachabilityFact
  - docs/language/precept-language-spec.md § 0.1 — the eleven design principles
  - docs/language/precept-language-spec.md § 0.6 — the proof-engine design contract and proof philosophy
  - docs/language/precept-language-spec.md § 0.7 — fault prevention, governance, and their composition
  - docs/language/precept-language-spec.md § 2.4 — constraint modifiers are shorthand for rules
  - docs/language/precept-language-spec.md § 3A.1 — constraint semantics and the four ensure anchors
  - docs/language/precept-language-spec.md § 3A.4 — operation execution order and the working-copy commit model
  - docs/language/precept-language-spec.md § 0.4 — no loops or branches; each assignment sees the state left by preceding ones
  - docs/language/precept-language-spec.md § 3.5 — default materialization and computed-field recomputation at construction
  - docs/Working/bugs.md — BUG-033 (write-surface residue) and BUG-034 (PRE0078 message), both bearing on this design's dependencies
  - docs/compiler/proof-engine.md — obligation generation contract, catalog-driven instantiation, the ProofRequirement DU
  - research/architecture/compiler/structural-fact-vs-value-citation-obligation-survey.md — Event-B's establishment/preservation obligation shape
  - src/Precept/Language/Actions.cs — GenerateIntervalContainmentObligations, the per-write minting path
  - src/Precept/Pipeline/ProofEngine.Satisfiability.cs — ScanRulesAgainstDefaults, the rule-vs-default fold
  - src/Precept/Pipeline/ProofLedger.cs — the ProofStrategy enum
  - precept_compile probes (2026-07-23, HEAD) — the modifier/rule asymmetry and the unearned-rule witness
---

# Constraint establishment and preservation obligations

## Review record — why this is no longer locked (2026-07-23)

This document was locked and then reviewed adversarially the same day. Three findings are serious enough that it cannot stand as written. They are recorded here rather than patched, because two of them are wrong in the premises rather than in the details.

**1. Decision 2 would introduce a soundness hole — the opposite of what the document claims it is.** Preservation is keyed to handlers (§ Semantic Rules), and the same decision deletes the per-write minting path (§ Inventory). But a state entry or exit action is not a handler, and the matrix names five write-site categories, not one. Verified at HEAD:

```precept
field Total as decimal default 0 nonnegative max 1000
state Open initial
state Closed terminal
event Finish()
to Closed
    -> set Total = 5000
from Open on Finish
    -> transition Closed
```

Today this is **rejected** — `PRE0078`, `IntervalContainment` `Unresolved` at `[5000 .. 5000]` — and the compiler's own output reports `eventHandlers: []`, confirming the site belongs to no handler. Under this design the modifier spelling stops minting there and the rule spelling never did, so the file would compile clean with a declared `max` violated by a write the compiler can see. That is a power-widening in the unsound direction, inside a document whose § The cost calls it a soundness correction. No acceptance criterion covers a non-handler write site.

**2. Decision 2's scope does not match its own justification.** § 2.4 names **eleven** constraint modifiers as shorthand for rules; the decision collapses **one** requirement kind. `LengthContainment` and `CountContainment` are left untouched, so `maxlength 10` keeps minting its own kind while `rule X.length <= 10` mints something else — which is precisely the divergence the decision rejects Alternative 1 for. A third option was not considered: select the kind by the constraint's **normal form** rather than its spelling, which § 2.4's own second bullet endorses ("proof participation is a function of a constraint's decidability, not its syntactic form"). The "only two ways" inference is therefore unsound. Separately, the denominator arithmetic is wrong in both directions: if `ConstraintPreservation` carries the bound-containment fault code, then by the 2026-07-21 ruling's own criterion it joins the fault family, and the obligation-family axis stops being a partition.

**3. The weakest-precondition rule is not total, and its validity argument asserts that it is.** The substitution `WP(C, w ; rest) = WP(C, rest)[target(w) := rhs(w)]` requires every write to be a field assigned an expression. Eleven of the fifteen action kinds are not: `append Items V` has a target and no right-hand side — the post-state value is `Items ++ [V]`, an expression that appears nowhere in the source — and `dequeue F into X` has two targets. The document asserts totality twice, in § Semantic Rules and again in the backward-substitution validity argument. Both statements are false against the action catalog, which the document's own dependency list half-acknowledges by citing BUG-033. Every collection constraint therefore has no computable weakest precondition under this rule.

**Also found, not fatal but disqualifying for ratification**: Decision 1's cost is measurable and was deferred rather than measured — a sweep of `samples/` finds 7 of 78 files (9%) whose constraints mention a computed field and would newly reject on establishment, with a floor of 23 files affected on the preservation side; the design's own falsifier 1 trips at a quarter of the corpus, and its stated remedy is to reopen a locked owner ruling. And every matrix line citation in the document is stale, because matrix rev 10 landed the same day and shifted them — a locked document whose evidence chain does not resolve cannot be spot-checked at ratification.

**What survived review**: the three witness files reproduce exactly as described; the § 2.4 quotes are real and in force, so Decision 2's *premise* is sound even though its scope and inference are not; the "intermediate states need not be checked" argument holds, and rests on locked spec rather than on unbuilt runtime behaviour; Decision 3 is well-grounded; and Decision 4's soundness claim holds under either ruling, though its amendment bookkeeping is inverted — relative to premise class (d) as the matrix defines it, the design shrinks first and calls the reversal a widening.

## Goal

When this is done, a `rule` and the field modifier that means the same thing produce the same obligations at the same places, and a definition compiles only when every constraint it declares is proven true at creation and proven still true after every handler that can touch it.

Two files are the test. `rule Other <= 1000` with an unbounded write must reject exactly as `field Other as decimal max 1000` with the same write already does. And the `OrderTotals` file — a rule consumed to prove a divisor safe while an unconstrained argument writes the rule's own field — must reject.

## Scope

**In scope.**

- The two obligation kinds the linkage design inventoried: what each proves, stated as a proof condition, and where each attaches.
- The discharge contracts, per premise class, each naming the finite check that decides "no licensed derivation exists".
- A validity argument for every discharge rule stated here, to the standard of the arguments already in the matrix.
- Replacing the shipped per-write containment check with the per-handler whole-write-plan check the matrix ruled.
- Making the rule-versus-default check an obligation rather than a report.
- What the diagnostic says when either obligation fails.

**Out of scope.**

- The linkage and completeness machinery. Locked separately; cited, not restated.
- The fault family's own obligations and validity arguments — separate in-flight work.
- Relational narrowing, which shipped and is locked elsewhere.
- The number model and overflow, which stay deferred.
- **Whether the residency fact is available as a premise.** The matrix reserves this to the owner. This design is written so that nothing depends on the answer: it takes the conservative position (Decision 4) and names the widening path.

**Deferred to future.**

- Multi-hop reasoning across separately declared constraints. The spec already excludes it and this design does not reopen it.
- Whether a failed preservation proof can point at an individual write inside a plan rather than at the plan — Decision 3 answers it for the common case and names what is left.

## The problem, in two files

Both compiled at HEAD on 2026-07-23 through `precept_compile`.

**Same meaning, two spellings, opposite outcomes.**

```precept
precept BoundVsRule

field Total as decimal default 0 max 1000
field Other as decimal default 0

rule Other <= 1000 because "The other total is capped at the same ceiling"

event Bump(N as decimal)

on Bump
    -> set Total = Bump.N
    -> set Other = Bump.N
```

`Total` gets a check that its default fits the ceiling (`Proved`) and a check at the write site (`Unresolved`, `PRE0078`, file rejected). `Other` gets nothing at all. The spec says these two spellings are the same thing — § 2.4: *"`min 5` and `rule X >= 5` are interchangeable to the proof engine"* — and the code has it exactly inverted: the modifier does the work, the rule is decorative.

**A rule used as a fact that nothing keeps true.**

```precept
precept OrderTotals

field ItemCount as integer default 1
field UnitCost as decimal default 1.0
field Total as decimal default 0.0

rule ItemCount > 0 because "An order with no items has nothing to price"

event Reprice(NewCount as integer, NewCost as decimal)

on Reprice
    -> set ItemCount = Reprice.NewCount
    -> set UnitCost = Reprice.NewCost
    -> set Total = Reprice.NewCost / ItemCount
```

Compiles with zero diagnostics; the divisor is reported `Proved` from the rule. `set ItemCount = Reprice.NewCount` writes the rule's field from an unconstrained argument. Fire `Reprice(0, 5.0)` and it divides by zero. Adding `positive` to the argument also compiles, so the repair discharges — what is missing is the check, not the capability.

**And the check that does exist is too strict in the other direction.**

```precept
precept DoubleWrite

field Total as decimal default 0 max 1000

event Adjust(Delta as decimal min 0 max 100)

on Adjust
    -> set Total = 5000
    -> set Total = Adjust.Delta
```

Rejected today with `PRE0078`. The compiler mints one obligation per write — two of them — and the first, `set Total = 5000`, computes the interval `[5000 .. 5000]` and fails. But the handler's post-state has `Total` in `[0 .. 100]`, comfortably inside the ceiling, and the second obligation reports `Proved`. The intermediate value 5000 exists only on a working copy that is promoted or discarded whole, so no reader ever sees it. Under the matrix's whole-write-plan ruling this file is legal and must compile. Both directions of the exact-power contract are broken today: the rule spelling accepts too much, and the per-write check rejects too much.

## What is already settled

Guard 17 discipline: these are cited and implemented against, not re-decided here.

| Already settled | Where |
|---|---|
| The five constraint forms and their obligation shapes | matrix § Constraint kinds |
| Obligations run over the **whole write plan** per handler, never per individual write; the weakest precondition is computed by backward substitution through the writes in reverse order | matrix § Vocabulary, *Write plan* |
| A constraint **mentions** a field through its condition, its activation guard, or transitively through a computed field; a desugared cross-field modifier mentions both fields | matrix § The minting rule |
| Four premise classes: field modifiers, argument constraints, the handler's guard, and all constraints holding in the pre-state | matrix § Vocabulary, *Premise classes* |
| The discharge contract is exact — a program compiles when and only when a written derivation licenses it — and every contract names its decision procedure | matrix § Vocabulary, *Discharge contract* |
| Establishment as a distinct site set applies to exactly two kinds: `rule` at construction, and residency `ensure` at construction plus every entry into its state | linkage design § Open questions |
| A proof consuming a breakable fact cites that fact's coverage record | linkage design, Decision 1 |
| Constraint modifiers are shorthand for rules and participate in proof identically | `precept-language-spec.md` § 2.4 |
| An unresolved verdict **rejects** — not because it might be broken, but because the definition left a reachable case with no authored disposition | `precept-language-spec.md` § 0.6, Proof philosophy 2 and 5 |
| Actions in a chain are sequenced; a reassignment invalidates prior facts about that field | `precept-language-spec.md` § 0.6, Proof philosophy 7 |
| Rules operate in field scope and cannot reference event arguments | `precept-language-spec.md` § 3A.1 |

## Philosophy Alignment

| Principle | Affected? (Y/N) | How served (1 sentence + cite) | Tension (1 sentence or N/A) | Tradeoff (1 sentence or N/A) |
|---|---|---|---|---|
| 1. Prevention, not detection | Y | A declared rule becomes structurally enforced on every operation rather than decorative — closing the gap `philosophy.md:49` names between declaring a rule and having it hold on every path. | N/A | Files that compile today stop compiling; § The cost. |
| 2. One file, complete rules | Y | Every premise is a declaration in the same file — the four premise classes are all authored constructs (matrix § Vocabulary), and no external oracle enters (spec § 0.6, Proof philosophy 4). | N/A | N/A |
| 3. Deterministic semantics | Y | Backward substitution through a fixed write order and a finite premise set is a deterministic function of the file; no search, no solver (spec § 0.6, Proof philosophy 3). | N/A | N/A |
| 4. Full inspectability | Y | A failed preservation proof prints the weakest precondition that would discharge it, which is the spec's own stated remedy — "the author resolves an unresolved verdict by supplying the constraint its printed weakest precondition names" (§ 0.6, Proof philosophy 5). | N/A | N/A |
| 5. Keyword-anchored readability | N | N/A — no authored syntax changes; `rule` and `ensure` keep their grammar exactly. | N/A | N/A |
| 6. Explicit domain meaning over primitive convenience | N | N/A — no type or domain-meaning surface is touched. | N/A | N/A |
| 7. Compile-time-first static checking | Y | Both obligations are discharged at compile time with a three-way verdict and no deferral (spec § 0.7, "there is no deferral"). | N/A | Compilation does more work per file; § Operational dimensions. |
| 8. Approximation honesty | Y | Where the write plan's arithmetic is on the approximate lane, the discharge rule's validity argument scopes itself to the exact lanes and says so, rather than claiming a bound it cannot hold. | The strictest reading would refuse approximate-lane preservation entirely; this design instead names the gap and leaves those cells open pending the number-model ruling. | Some approximate-lane programs are rejected that a later ruling may license — the safe direction. |
| 9. Mandatory rationale (`because`) | Y | The failing diagnostic quotes the rule's authored `because` text, so the author sees their own stated reason next to the violation. | N/A | N/A |
| 10. Totality | Y | The `OrderTotals` file is a live counterexample to "a precept that compiles without diagnostics has no unproven arithmetic faults"; making the rule earn its keep restores it. | N/A | N/A |
| 11. Static completeness | Y | Same counterexample at the compiler-to-evaluator bridge — an evaluator divide-by-zero is reachable today from a clean compile. | N/A | N/A |

**Tradeoffs stated.** *Principle 8*: the discharge rules here are proved over exact decimal and integer arithmetic. On the approximate lane, addition rounds, so a bound derived by interval arithmetic needs an outward-rounding side condition that nobody has written. Rather than claim the rule holds there, the design leaves approximate-lane preservation cells open under the existing number-model deferral — refusing rather than over-claiming, which is the direction Principle 8 requires. *Principle 7*: the per-file cost rises because every constraint now produces obligations at every site of every mentioned field. The corpus compiles in about 44ms today, and § Falsifiers sets the threshold at which that trade stops being acceptable.

**Companion commitments.** *Stateless-first-class*: `rule` establishment and preservation are defined over construction and handlers, both of which exist in a stateless precept; only the residency-ensure site set has no instances there, and the design never routes a general behaviour through a state-dependent path. *Domain-expert-primary-author*: the two new diagnostics name the author's own rule, their own `because` text, and the specific line that breaks it, and the repair they suggest is a constraint on an argument or a guard — both constructs the author already knows.

## Language Design Grounding

*Scope note*: this design introduces no authored syntax. It adds catalog member names and two diagnostics, which reach authors through messages and MCP output, so the grounding is carried.

**General language design.** The shape here is the standard one for verifying invariants over mutable state, and Precept is adopting it rather than inventing it. An invariant over a variable generates one obligation that the initial configuration establishes it, and one obligation per mutating operation that the operation preserves it. Event-B's Rodin, the one surveyed comparator that ships a soundness argument for its obligation generator, does exactly this:

> "The resulting model now gives rise to 6 proof obligations in total; 3 of these are to verify that the initialisation establishes invariants inv2 to inv4 and 3 are to verify that the register event maintains invariants inv2 to inv4."

The mechanism for the preservation half is Dijkstra's weakest precondition: to prove a constraint holds after a sequence of assignments, substitute backwards through the assignments and prove the resulting condition holds before them. Precept's version is unusually easy because the language has no loops (§ 0.4), so there is no fixed point to iterate to, and expressions are pure, so nothing changes between statements. The general case in the literature needs loop invariants; Precept's write plan is straight-line by construction.

What Precept takes: the establish-plus-preserve pair, and weakest-precondition substitution for the preservation half. What Precept diverges on is the granularity. Standard practice generates one preservation obligation per assignment or per event depending on the tool; Precept generates one per **handler**, because its commit model makes intermediate states unobservable — mutations run on a working copy that is discarded whole if any constraint fails (§ 3A.4), so a mid-handler state is not a configuration any reader, later handler, or persistence layer can see. Checking intermediates would reject programs whose end state is fine, which under the exact-power contract is as nonconforming as accepting too much.

`research/language/README.md` has no domain entry for invariant verification or weakest preconditions — that index covers authored surface. The architecture-side survey is the on-point in-tree research and is cited rather than duplicated.

**Precept-specific application.** § 2.4 is the section this design implements and the shipped code contradicts. § 0.6 Proof philosophy 2 supplies the three-way verdict the obligations carry. § 0.6 Proof philosophy 7 supplies the sequencing the backward substitution walks. § 3A.1's rule-in-field-scope restriction is what makes the substituted condition's argument references legal even though the rule itself cannot name an argument. § 0.7's composition paragraph is what makes an argument constraint usable as a premise: governance enforces it at ingress, so the compiler may treat it as true of the value.

## Audience and Teachability

**Worked example.** A domain expert writing an ordinary priced-order file, with the rule they meant and the argument constraint that earns it:

```precept
precept OrderTotals

field ItemCount as integer default 1
field UnitCost as decimal default 1.0
field Total as decimal default 0.0

rule ItemCount > 0 because "An order with no items has nothing to price"

event Reprice(NewCount as integer positive, NewCost as decimal)

on Reprice
    -> set ItemCount = Reprice.NewCount
    -> set UnitCost = Reprice.NewCost
    -> set Total = Reprice.NewCost / ItemCount
```

Without `positive` on the argument this is rejected. With it, the preservation proof closes: substituting the write into the rule gives `Reprice.NewCount > 0`, which is what the argument declares.

**Error message.** The plausible misuse is writing a rule's field from an input the author didn't constrain.

```
PRE0168: 'Reprice' can leave the rule 'ItemCount > 0' false —
         "An order with no items has nothing to price".
         Line 12 sets 'ItemCount' from 'Reprice.NewCount'. After that,
         holding the rule requires 'Reprice.NewCount > 0', and nothing
         says so.
         Constrain the argument — 'NewCount as integer positive' — or
         guard the row with 'when Reprice.NewCount > 0'.
```

The wording serves the domain expert because it quotes their own rationale back, names the handler and the line in their file, and states the missing condition in the same vocabulary they wrote the rule in. It never says obligation, premise, weakest precondition, or discharge.

**10-minute teaching path.**

1. `docs/language/precept-language-spec.md` § 3A.1 — what a rule promises (2 min).
2. `docs/language/precept-language-spec.md` § 2.4 — constraint modifiers on fields and arguments (3 min).
3. `samples/loan-application.precept` — a file whose rules rest on constrained arguments (3 min).
4. The `PRE0168` entry in `docs/compiler/diagnostic-system.md` — the message and its two repairs (2 min).

## Semantic Rules

Write `C` for a constraint, `σ` for a configuration (the field values, plus the lifecycle position where there is one), and `h` for a handler with write plan `w₁; w₂; …; wₙ`.

**Establishment.** For each establishment site `e` of `C`:

```
Establish(C, e)   holds iff   σₑ ⊨ C
```

where `σₑ` is the configuration at that site — for construction, the defaults as materialized plus whatever the initial event's plan writes; for an entry into a residency state, the post-transition configuration of the entering row. In words: at the moment the constraint first has to be true, it is true.

**Preservation.** For each handler `h` whose write plan touches any field `C` mentions:

```
Preserve(C, h)    holds iff   Premises(h) ⊢ WP(C, w₁; …; wₙ)
```

where `WP` is the weakest precondition — the condition that must hold *before* the plan runs for `C` to hold after it — computed by substituting backwards:

```
WP(C, ε)           =  C
WP(C, w ; rest)    =  WP(C, rest)[ target(w) := rhs(w) ]
```

Read the second line right to left: take the condition you need at the end, and wherever it mentions the field this write targets, replace that field with what the write assigns. Applying this from the last write to the first leaves a condition over the pre-state and the handler's arguments, which is what the premises must entail.

The substitution is total on Precept's write plans because there are no loops (§ 0.4) and expressions are pure (§ 0.6, Proof philosophy 3), so the plan is straight-line and each write has one right-hand side.

**Premises.** `Premises(h)` is the union of the four classes for that handler: the field modifiers in scope, the constraints on `h`'s arguments, `h`'s own guard, and the constraints holding in the pre-state. The fourth is the inductive hypothesis and is exactly the class the citation duty governs — a proof may use it only by citing the coverage record for the constraint it comes from.

**Typing the obligation.** No new typing rule. The two obligations are values of the `ProofRequirement` discriminated union, instantiated by the type checker at catalog-declared sites and read by the proof engine, which is the existing contract:

```
  C ∈ Constraints(file)     e ∈ EstablishmentSites(C)
  ──────────────────────────────────────────────────────────
     ConstraintEstablishmentProofRequirement(C, e)  minted

  C ∈ Constraints(file)     h ∈ Handlers     mentions(C) ∩ writes(h) ≠ ∅
  ────────────────────────────────────────────────────────────────────────
     ConstraintPreservationProofRequirement(C, h)  minted
```

**Verdicts.** Each obligation carries the three-way verdict § 0.6 already defines: *proven*, *proven-violating* (rejected with a witness configuration), or *unresolved* (rejected, with the weakest precondition printed). Both non-proven verdicts block.

**Soundness preservation.**

*Principle 7.* No guessing is introduced: the weakest precondition is computed by substitution, not search, and the premise set is finite and enumerable from the file.

*Principle 10.* The principle is currently violated, not threatened — `OrderTotals` compiles clean with an unproven division. After this, the rule the division rests on must itself be established and preserved, so the divisor's safety no longer stands on an unearned fact.

*Principle 11.* Same counterexample at the compiler-to-evaluator bridge. What remains outside the claim: the weakest precondition is only as sound as the write plan being complete, which depends on the § 3A.4 writer enumeration being consumed correctly — BUG-033 is a verified instance where it is not. That dependency belongs to the linkage design's completeness check and is not re-argued here.

## Validity arguments

Every discharge rule stated below carries a truth-preservation argument, per the matrix's rule-validity gate. Each is written to the standard of the arguments already in the matrix: sound modulo honestly-named open dependencies.

**Backward substitution computes the weakest precondition.** The claim: `Premises ⊢ WP(C, plan)` implies `C` holds in the post-state. Why it is truth-preserving: each write in the plan is a total assignment of a pure expression to one field target, declared per action kind in the action catalog, and the plan is a linear sequence — *"Because there are no loops or branches, there is no join point where two different states must be merged. Each assignment in a row sees the state left by all preceding assignments. This makes sequential flow analysis a linear walk, not a dataflow graph"* (`precept-language-spec.md:164`). So after `set F = E` the post-state value of `F` is the pre-state evaluation of `E` and every other field is unchanged. Substituting `E` for `F` in a condition therefore yields a condition on the pre-state with the same truth value as the original on the post-state — this is the standard assignment axiom, and Precept satisfies its side condition (no aliasing, no pointers) by construction, since a field name denotes exactly one slot. Iterating from the last write backwards composes these one-step equivalences. Because the plan is straight-line, the composition is finite and needs no fixed point. **Open dependencies**: the plan must contain every write the operation performs — the eight-writer enumeration in § 3A.4, whose consumption BUG-033 shows is incomplete; and the sequencing must be the evaluator's actual order, which § 3A.4 pins for the nine phases but leaves unsettled for two cases the transport rule already refuses conservatively.

**Intermediate states need not be checked.** The claim: proving the post-state satisfies `C` is sufficient, even when an intermediate state violates it. Why: the runtime executes the whole plan on a working copy and promotes it only if every constraint passes, discarding it otherwise — *"An invalid configuration never exists, even transiently. There is no window between mutation and constraint checking where a partially-committed state with violated rules can be observed"* (`precept-language-spec.md:1967`). So an intermediate state is not a configuration any observer can reach, and the induction the pre-state premise class rests on quantifies over reachable configurations only. **Open dependency**: this argument is about the documented evaluator, and the runtime that implements the working-copy model is not built; if a future runtime commits incrementally, the argument fails and the per-write check would be the correct one.

**An argument constraint is true of the value the handler evaluates.** The claim: a constraint declared on an event argument may be assumed when proving the weakest precondition. Why: governance enforces every declared constraint on every value entering from outside, *"at the moment it enters, before any computation derives from it"* (§ 0.7), and the composition paragraph in the same section makes this the licensed pairing — the compiler proves the structural fact that the value carries its constraint, governance makes the constraint true of the value. This is the same argument the matrix's existing arg-bound interval rule rests on, applied to a whole condition rather than a numeric bound. **Open dependency**: none beyond the ones that rule already carries.

**The default configuration is a known configuration.** The claim: establishment at construction can be decided by folding the constraint against the declared defaults. Why: a default is an authored literal or a constant expression, evaluated once in declaration order at construction (§ 3.5), so the configuration is statically known wherever every mentioned field's default is foldable. The existing implementation already computes this and shares one default environment and one constant-fold evaluator with the initial-state ensure check. **Open dependency**: when a mentioned field's default is not foldable — a computed field, or a non-constant default — the configuration is not statically known, and the obligation is `Unresolved` rather than proven. Decision 1 is precisely about what that verdict does.

**A guard fact reaches the write it justifies.** Not restated — this is the matrix's existing guard-match argument together with the transport rule's frame-and-kill, both of which are written and carry their own open dependencies. Preservation proofs consuming a guard fact inherit them unchanged.

## Architecture Grounding

### Precept-internal placement

**Layer placement.** The obligations are catalog metadata — two subtypes of the `ProofRequirement` discriminated union, instantiated by the type checker at declared sites and read by the proof engine. That is the existing contract, stated in the proof-engine doc: *"The proof engine does NOT maintain its own list of what needs to be proved."*

The weakest-precondition computation is pipeline code in the proof engine, not catalog metadata, because it is an algorithm over expression trees rather than a declared fact. What *is* catalog metadata is the substitution rule per action kind — which field a write targets and what it assigns — and that already exists as `ActionMeta` write semantics; the algorithm reads it rather than switching on action identity.

The discharge strategies are proof-engine code, as the existing eleven are. No new strategy enum member is needed for the common cases: the existing `IntervalContainment`, `GuardInPath`, `CompositionalConstraint` and `Literal` strategies discharge the substituted conditions they already handle in their present form.

**Cross-component propagation.**

- *Runtime (parser, type checker, evaluator, diagnostics)*: the type checker gains the minting of both kinds at their site sets. The proof engine gains the weakest-precondition walk and loses the per-write containment path in `Actions.cs`. Diagnostics gain two codes. The parser, lexer and evaluator are untouched.
- *Tooling (syntax highlighting, completions, hover, semantic tokens)*: hover over a rule gains its establishment and preservation status per handler. No highlighting, completion or semantic-token change.
- *MCP (vocabulary, DTOs, tool output)*: the two new requirement-kind names enter the vocabulary through the existing catalog formatter. `precept_compile`'s obligation projection carries them with no shape change — they are additional values of an existing enum, and the DTO already projects requirement kind, disposition and strategy.

**Breaking changes.** Yes.

1. `ProofRequirementKind` loses `IntervalContainment` as a mint kind and gains two members (Decision 2). The analyzer enforcing one-to-one correspondence between the enum and the discriminated union makes this compile-checked.
2. Files that compile today stop compiling — the substance of the change.
3. `PRE0078`'s emission moves from the per-write path to the per-handler path, so its span changes from a write to a handler. Note that `PRE0078`'s message is separately wrong (BUG-034 — it reports a bound violation as a range overflow); this design does not fix that, and moving the emission does not make it worse.

### External architectural precedent

The architectural question is where obligation generation sits relative to the constraint's declaration.

Event-B's Rodin generates obligations from the model's declared structure: one initialisation obligation per invariant, one preservation obligation per invariant-event pair, produced by a static generator that walks the model rather than by the prover. Precept takes that placement exactly — the type checker stamps requirements at declared sites, the proof engine discharges them, and neither invents its own list.

Precept diverges on granularity, and the divergence is forced by a difference in the execution models rather than chosen. An Event-B event's actions are simultaneous, so there is no intermediate state to reason about. Precept's actions are sequenced and each sees the state left by the previous one (`precept-language-spec.md:164`), which would ordinarily mean checking after each. Precept does not, because its commit model makes intermediates unobservable — the working copy is promoted or discarded whole. So Precept ends up at Event-B's granularity by a different route: not because the writes are simultaneous, but because their intermediate results are unreachable.

The weakest-precondition mechanism itself is standard and needs no defense; what is worth naming is that Precept gets its easy case for free. Tools in this space spend most of their engineering on loop invariants and aliasing. Precept has no loops (§ 0.4) and no aliasing — a field name denotes exactly one slot — so backward substitution is total, terminating, and exact, with no approximation step at all.

## Inventory of what will be built

**Catalog.**

- `ProofRequirementKind.ConstraintEstablishment`, `ProofRequirementKind.ConstraintPreservation` — added; `ProofRequirementKind.IntervalContainment` — removed as a mint kind (`src/Precept/Language/ProofRequirementKind.cs`).
- `ConstraintEstablishmentProofRequirement(ConstraintIdentity Constraint, EstablishmentSite Site, string Description)` and `ConstraintPreservationProofRequirement(ConstraintIdentity Constraint, HandlerIdentity Handler, ImmutableArray<string> MentionedFieldsWritten, string Description)` — two sealed subtypes (`src/Precept/Language/ProofRequirement.cs`).
- `ProofRequirementMeta` entries for both, each naming its diagnostic code (`src/Precept/Language/ProofRequirements.cs`).

**Pipeline.**

- `ProofEngine.WeakestPrecondition.cs` — backward substitution over a write plan, reading `ActionMeta` write semantics per action kind.
- `ProofEngine.Analysis.cs` — mint both kinds at their site sets; the establishment path absorbs and replaces `ScanRulesAgainstDefaults`.
- `Actions.cs` — `GenerateIntervalContainmentObligations` deleted; its interval discharge survives as a strategy applied to a substituted condition.
- Modifier desugaring — a constraint modifier produces a `ConstraintIdentity` with a generated rationale, so it flows through the same minting path as an authored `rule` (§ 2.4).

**Diagnostics.**

- `PRE0167` — a constraint is not established at one of its sites.
- `PRE0168` — a handler can leave a constraint false (the message in § Audience and Teachability).

**Tests.**

- `test/Precept.Tests/ConstraintEstablishmentTests.cs` — the defaults fold, the unfoldable-default rejection, the residency-entry site set.
- `test/Precept.Tests/ConstraintPreservationTests.cs` — the two files in § The problem; the modifier and rule spellings producing identical obligations; multi-write plans where an intermediate violates and the post-state does not.
- `test/Precept.Tests/WeakestPreconditionTests.cs` — substitution over hand-computed plans, including a plan writing the same field twice.
- Corpus run over `samples/`, with every newly-rejected file listed and its repair recorded.

## Decisions

### Decision 1: Establishment at construction becomes an obligation with the three-way verdict — an unfoldable default is `Unresolved` and rejects

**Stakes**: high

- **Rationale**: today the rule-versus-default check is a *report*: only a provably-false fold rejects, and an unfoldable default silently passes. The code says so in its own comment. That is the correct posture for a violation report, and the wrong one for the base case of an induction. The whole inductive argument rests on the constraint being true at construction; if the compiler cannot show it, the base case is unproven and every preservation proof that leans on the pre-state premise is standing on nothing. The spec already draws exactly this distinction and puts bounded-write checks on the obligation side, with unresolved blocking *"not because it 'might be broken,' but because the definition left a reachable case with no authored disposition."* An establishment check is that same shape.
- **Tradeoff accepted**: a definition whose field default is a non-constant expression now needs the author to make the constraint provable at construction — by making the default foldable, or by declaring the constraint in a form the initial event's argument constraints discharge. That is real friction on a shape that compiles silently today. Accepted because the alternative is an induction with an unproven base case, which makes every downstream proof vacuous.
- **Alternatives considered**:
  - *Keep report semantics and treat construction as trusted.* Rejected: it is the exact fail-open the want doc's base case exists to close, and it would let a `default 0` under a `min 1` make every downstream proof vacuous — a failure mode the soundness survey named as the first place to look for a real breach.
  - *Warn on unfoldable, reject only on proven-false.* Rejected on the same ground the dead-rows ruling rejected warning severity: a file that compiles with an asterisk on its guarantee is the two-tier trustworthiness the want doc forbids.
  - *Fold harder — evaluate computed-field defaults transitively.* Not rejected, but not sufficient: it narrows the unfoldable set without eliminating it, and the verdict question still has to be answered for what remains. Worth doing as a separate completeness improvement.
- **Precedent**: Event-B generates an initialisation obligation per invariant and requires it discharged — *"3 of these are to verify that the initialisation establishes invariants inv2 to inv4"* — rather than reporting only provable violations. In-tree, the spec's own three-way verdict for bounded writes is the established shape.
- **Sources consulted for this decision**:
  - `src/Precept/Pipeline/ProofEngine.Satisfiability.cs:51-53` — "Prove-or-reject: only a proven `false` fold rejects — an unknown/unfoldable default (computed field, non-constant default) yields `null` and never rejects (soundness over completeness, spec § 0.6 #1)."
  - `docs/language/precept-language-spec.md:223` — "**Bounded-write checks are different — they are containment *obligations*, not reports**, and carry a three-way verdict"
  - `docs/language/precept-language-spec.md` § 0.6 item 11 — "Compile-time rule enforcement against defaults. Rules and initial-state ensures are checked against default field values at compile time." (checked for prior settlement: the spec settles *that* the check exists, and states its current report semantics as implementation status; it does not settle that report semantics is the target under an inductive proof obligation.)
  - `docs/Working/what-i-want-2026-07-16.md:18` — "**Base case.** The default configuration satisfies every rule, and every initial event *establishes* every rule, provable from the constraints declared on its args."
- **Strongest counter-evidence**: § 0.6 Proof philosophy 1 says "soundness over completeness … false negatives (missed proofs) cause author friction," and the shipped comment cites exactly that to justify never rejecting an unfoldable default. Response: that principle governs which way to err when the compiler cannot decide, and rejecting *is* the safe direction for an obligation — it is passing that would be the unsound choice. The comment applies the principle correctly for a report and incorrectly for an obligation, which is precisely the distinction § 0.6 Proof philosophy 2 draws.
- **Reversibility**: `Hard` once authors have adjusted files to it; `Easy` now, pre-release.
- **Blast radius**: catalogs — `ProofRequirementKind`, `ProofRequirement`. Docs — `docs/language/precept-language-spec.md` § 0.6 item 11 (implementation status), `docs/compiler/proof-engine.md`, `docs/compiler/diagnostic-system.md`. Samples — any file with a non-constant default under a constraint; enumerated by the corpus run. External consumers — none.

### Decision 2: `IntervalContainment` stops being a minted requirement kind and becomes a discharge strategy for a preservation obligation

**Stakes**: high

- **Rationale**: this is implementation against locked spec rather than a fresh choice. § 2.4 states that constraint modifiers are shorthand for rules and that `min 5` and `rule X >= 5` are *"interchangeable to the proof engine."* Today they are not interchangeable at all — the modifier mints an interval-containment obligation at every write and the rule mints nothing. There are only two ways to make them equal, and one does not work: a rule cannot mint an interval-containment obligation, because that requirement's payload is a single target field with a numeric minimum and maximum, and a rule can be relational, multi-field, or guarded. So the equality has to run the other way — both spellings mint a preservation obligation, and interval arithmetic over declared bounds becomes the decision procedure that discharges the single-field-literal-bound case. The name survives where it already exists as a strategy.
- **Tradeoff accepted**: this re-homes a member of the fault family. The 2026-07-21 family-scope ruling defines the fault family as the eleven requirement kinds backed by a fault code, and interval containment is one of them; after this change, a bound violation is reported by a constraint-preservation obligation carrying that fault code rather than by a requirement kind of its own. The fault guarantee is unchanged — "no result outside a declared bound" is still prevented and still names its fault code — but the fault-family denominator drops by one and the fault-family cells that key on this kind re-anchor. That is real bookkeeping cost on work currently in flight, and it is the honest consequence of the spec's own equivalence.
- **Alternatives considered**:
  - *Leave interval containment as its own kind and additionally mint preservation for rules.* Rejected: the same ceiling written two ways would produce two different obligation kinds with different cell coordinates, different diagnostics, and different discharge contracts — which is precisely the non-interchangeability § 2.4 forbids.
  - *Generalize the interval-containment payload to carry arbitrary conditions.* Rejected: that is constraint preservation with a misleading name, and it would leave the catalog with a member whose name describes one of the cases it handles.
  - *Treat the spec statement as aspirational and keep the split.* Rejected: § 2.4 is a locked spec statement with no deferral marker, and the memory of this project is that impl-versus-spec divergences of this kind are cleanup, not decisions.
- **Precedent**: in-tree, the strategy enum already carries `IntervalContainment` as a discharge mechanism (`ProofStrategy.IntervalContainment = 7`), so the vocabulary for this split already exists and the change removes a duplicate rather than inventing one. Externally, Event-B has one obligation shape for an invariant regardless of whether the invariant is a simple range or a relation over several variables — the obligation kind is not specialized by the predicate's shape.
- **Sources consulted for this decision**:
  - `docs/language/precept-language-spec.md:1135` — "**Constraint modifiers are shorthand for rules.** A constraint modifier … desugars to the equivalent `rule` with a generated rationale."
  - `docs/language/precept-language-spec.md:1138` — "`min 5` and `rule X >= 5` are interchangeable to the proof engine."
  - `docs/compiler/proof-engine.md:586-594` — `IntervalContainmentProofRequirement(ProofSubject Subject, string TargetField, decimal? DeclaredMin, decimal? DeclaredMax, …)` — the single-field numeric payload that cannot express a relational rule.
  - `src/Precept/Pipeline/ProofLedger.cs:108` — `IntervalContainment = 7` in the `ProofStrategy` enum.
  - `docs/Working/obligation-discharge-matrix-2026-07-19.md:143` — the 2026-07-21 family-scope ruling, "the fault family is the eleven fault-preventing kinds".
- **Strongest counter-evidence**: the 2026-07-21 family-scope ruling counted eleven fault kinds and this design changes that count two days later, which is exactly the kind of churn a locked ruling is meant to prevent. Response: the ruling settles *which kinds are fault kinds*, keyed on fault-code backing; it does not settle *which kinds exist*. Nothing in it is overturned — the bound-containment fault is still prevented and still carries its fault code. What changes is which obligation kind carries it, and that follows from a spec statement older than the ruling.
- **Reversibility**: `Hard`. The kind name reaches MCP vocabulary and the fault-family cell coordinates. Pre-release, so no external consumer is affected, but the in-flight fault-family work re-anchors.
- **Blast radius**: catalogs — `ProofRequirementKind`, `ProofRequirement`, `ProofRequirements`. Docs — `docs/compiler/proof-engine.md` § ProofRequirement Catalog DU, `docs/language/precept-language-spec.md` § 0.6 items 6 and 11, `docs/compiler/diagnostic-system.md`. Working — the fault-family cell files keyed on `IntervalContainment`, and the fault-family coordinate map. Samples — none directly. External consumers — none.

### Decision 3: A preservation obligation is keyed to the handler, and its diagnostic names the last write that makes the condition unprovable

**Stakes**: medium

- **Rationale**: the keying is settled — the matrix ruled obligations run over the whole write plan per handler. What was left open was explicitly flagged as not a definitional question: *"how a failed multi-write proof reports — whether the diagnostic can point at an individual write inside the plan, or names the handler. That is diagnostic quality, decided when the diagnostic is built."* This design builds it, so it decides it. The backward substitution already produces an intermediate condition after each step, so the walk knows the first step (working backwards) at which the condition stopped being entailed by the premises. Reporting that write costs nothing and is what the author needs — naming only the handler leaves them to find which of five lines broke it.
- **Tradeoff accepted**: the reported write is the one where the *proof* fails, which is not always the one the author would consider at fault — a later write can make an earlier one's contribution unprovable. The message therefore names the write and prints the condition, rather than asserting the write is wrong.
- **Alternatives considered**:
  - *Name only the handler.* Rejected: correct but unhelpful, and the author has to bisect.
  - *Name every write that touches a mentioned field.* Rejected: on a long plan this is noise, and it implies all of them are at fault.
- **Precedent**: the spec already commits to printing the weakest precondition on an unresolved verdict — *"the author resolves an unresolved verdict by supplying the constraint its printed weakest precondition names"* — so printing the condition is settled and only its anchor is being chosen.
- **Sources consulted for this decision**:
  - `docs/Working/obligation-discharge-matrix-2026-07-19.md:49` — "One thing the ruling does not settle, and which is not a definitional question: how a failed multi-write proof reports … That is diagnostic quality, decided when the diagnostic is built."
  - `docs/language/precept-language-spec.md:229` — "The author resolves an unresolved verdict by supplying the constraint its printed weakest precondition names."

### Decision 4: For a residency ensure, the pre-state premise class is limited to unconditional constraints until the residency-fact question is ruled

**Stakes**: medium

- **Rationale**: proving that a handler preserves `in S ensure C` may want other facts that are true only while resident in `S`. Whether the entity's residency in `S` is itself available as a premise is reserved to the owner by the matrix and is not settled here. Rather than depend on the answer, this design takes the position that costs nothing to reverse: a preservation proof for any constraint may use unconditional constraints from the pre-state, and may not use state-scoped ones. That is sound under either ruling — it consumes a strict subset of what the permissive answer would allow — and it makes the rest of the design independent of the question.
- **Tradeoff accepted**: some safe programs are rejected that the permissive ruling would accept, specifically those where one residency ensure's preservation needs another residency ensure in the same state. Those land in the sound-but-unprovable band and the author's respelling is to declare the needed fact as an unconditional rule. If the owner rules the residency fact admissible, admitting it is a power-widening — the cheap amendment class, monotone, minor version, no certificate replay break.
- **Alternatives considered**:
  - *Assume the residency fact is available.* Rejected: it is the owner's question, reserved in the matrix, and assuming the permissive answer is the direction that cannot be walked back without a soundness correction.
  - *Refuse residency-ensure preservation entirely until ruled.* Rejected as far more restrictive than the uncertainty warrants — the obligation is well-defined without the extra premise.
- **Precedent**: the matrix's own amendment protocol makes the conservative-then-widen path the cheap direction, and the sound-but-unprovable band is the standing inventory of candidates for exactly this kind of later widening.
- **Sources consulted for this decision**:
  - `docs/Working/obligation-discharge-matrix-2026-07-19.md:197` — "**Open**: whether the residency fact — the entity is in `S` — is itself available as a premise, and under which class, is not ruled … the question is the owner's and is recorded here rather than assumed"
  - `docs/Working/obligation-discharge-matrix-2026-07-19.md:429` — "**Power-widening** … Monotone: everything previously licensed stays licensed. Minor definition version; previously issued certificates remain valid."
  - `docs/Working/ensure-vs-rule-analysis-2026-07-20.md` § Verdict — the rewrite of a residency ensure as a conditional rule over a state coordinate is inexpressible, verified by live compile, so the question cannot be dissolved by restating the construct.

### Decision 5: Two diagnostic codes, one per obligation kind

**Stakes**: low

- **Rationale**: the two failures have different repairs. An establishment failure is fixed at the default or at the initial event; a preservation failure is fixed at the writing handler. One code carrying both would have to name both repairs on every occurrence.
- **Tradeoff accepted**: two more entries in a catalog that is already large.

## The cost

This is a **soundness correction** under the matrix's amendment protocol, not a widening: it shrinks the set of files that compile. Major definition version, certificate replay breaks for affected cells, routed to the owner with witness programs. Both witnesses are in § The problem and both were compiled at HEAD on 2026-07-23.

The size of the shrink is not guessed here. The corpus run in § Acceptance criteria measures it, and every newly-rejected sample is listed with its repair before the change lands.

## Acceptance criteria

1. `rule Other <= 1000` with `set Other = Bump.N` (unconstrained argument) is rejected with `PRE0168`, and the message quotes the rule's `because` text and names line and handler.
2. The same file with `field Other as decimal max 1000` instead of the rule is rejected identically — same obligation count, same verdict, same repair suggested. A test asserts the two spellings produce equal obligation sets.
3. `OrderTotals` is rejected; `OrderTotals` with `NewCount as integer positive` compiles clean and the preservation obligation reports `Proved`.
4. The `DoubleWrite` file in § The problem compiles, with exactly **one** preservation obligation for the handler rather than one per write. This is where the per-handler keying differs observably from shipped behaviour, and today's output — two obligations, the first `Unresolved` at `[5000 .. 5000]`, the second `Proved` at `[0 .. 100]`, file rejected — is the before-picture the test asserts against.
5. A rule mentioning a field only through a computed field's inputs produces a preservation obligation on the handler writing those inputs — the transitive leg of the mention set.
6. A rule mentioning a field only in its activation guard produces a preservation obligation on the handler writing that field.
7. A definition whose field default is non-constant, under a rule mentioning that field, is rejected with `PRE0167` — the change from report to obligation semantics.
8. A definition whose defaults provably violate a rule is still rejected, with the same witness it produces today — the change does not lose the case that already worked.
9. The weakest-precondition walk over a hand-computed three-write plan produces the expected condition, asserted literally rather than by round-trip.
10. Every `samples/` file either compiles or appears in a committed list with the constraint it needs and the repair applied.
11. No obligation is minted for a handler whose write plan touches no field the constraint mentions — the surplus direction, asserted so the completeness check's set equality can hold.
12. Hover over a rule reports its establishment status and its preservation status per handler.

## Dependencies

**Upstream.**

- The linkage and completeness design — supplies the coverage records these obligations populate and the citation the pre-state premise class requires.
- The § 3A.4 writer enumeration, for the write plan the substitution walks. Complete as a category list; its consumption is not, per BUG-033.
- The matrix's guard-match argument and transport rule, inherited unchanged by any preservation proof consuming a guard fact.

**Downstream.**

- The fault-family re-ratification: the pre-state premise class becomes usable with a citation, which is what the fault cells needed.
- BUG-017, BUG-018, BUG-019 — the recorded breaches where a rule is consumed without being enforced.
- The fault-family denominator and any cell keyed on `IntervalContainment`, which re-anchor per Decision 2.

## Doc-update enumeration

- `docs/language/precept-language-spec.md` § 0.6 items 6 and 11 and the implementation-status table — the obligation semantics and the kind collapse; § 2.4 — a pointer noting the equivalence is now enforced rather than asserted.
- `docs/compiler/proof-engine.md` § ProofRequirement Catalog DU — the two added subtypes and the removed one; § Obligation Generation Contract; § Contracts and Guarantees § Obligation Completeness.
- `docs/compiler/diagnostic-system.md` — `PRE0167`, `PRE0168`, and the `PRE0078` span change.
- `docs/language/catalog-system.md` — the requirement-kind inventory.
- `docs/tooling/mcp.md` — the requirement-kind vocabulary.
- `docs/tooling/language-server.md` — constraint hover.
- `docs/Working/obligation-discharge-matrix-2026-07-19.md` — § Rule validity gains the arguments in § Validity arguments above; § Vocabulary's write-plan entry records the divergence as corrected.
- `docs/Working/bugs.md` — BUG-017/018/019 dispositions; BUG-034 noted as unaffected.
- `README.md` — no change; it makes no claim about rule enforcement mechanics.

## Operational dimensions

**Security.** N/A — no source-text ingestion surface changes.

**Observability.** Both new diagnostics print the condition that would discharge them, which is the spec's stated remedy for an unresolved verdict. The preservation message anchors on a specific write (Decision 3). Obligation dispositions flow through the existing proof ledger to `precept_compile`, `precept_proofs` and hover, so "which handlers must preserve this rule, and did they" is answerable without reading compiler output.

**Evolvability.** N/A — no dependency on an external standard.

## Falsifiers

1. If the corpus run rejects more than a quarter of `samples/`, the obligation set is over-generating and the mention set's transitive leg is the first place to look — the design would need a narrower definition of what a computed field's inputs make a write site for.
2. If compile time for the median sample more than doubles, the per-handler substitution is too expensive as specified and the walk needs memoizing per constraint rather than recomputing per handler. (Baseline: the full corpus compiles in about 44ms.)
3. If more than two samples need a rule restated as an unconditional constraint purely to work around Decision 4's conservative premise set, the residency-fact question is load-bearing in practice and should be put to the owner as urgent rather than as a later widening.
4. If a domain expert reads `PRE0168` and repairs the wrong line more often than the right one, Decision 3's anchor is wrong and the message should name the handler and list every candidate write instead.
5. If a preservation proof is found that discharges through the pre-state premise class without a coverage citation, the linkage design's admissibility rule is not actually enforced on this path and both designs need re-checking together.

## Open questions

None. The residency-fact question is genuinely open and genuinely the owner's, but this design does not depend on it — Decision 4 takes the position that is sound under either ruling and names the widening path. It is recorded in § Scope as out of scope rather than as an open question, because nothing here waits on it.
