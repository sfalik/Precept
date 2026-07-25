---
status: Draft — reworked 2026-07-24 against the three unlock findings; see § Review record. Still UNLOCKED: the owner signs off the re-lock. Was `Locked 2026-07-23` for a few hours; do not build against that version.
phase-target: TBD — follows the linkage design; both precede the fault-family re-ratification
comparable-systems-research-status: strong
sources-consulted:
  - docs/Working/obligation-discharge-matrix-2026-07-19.md **rev 11** — every matrix line cite below is refreshed to rev 11 and re-read at HEAD; rev 11 additionally marks five validity arguments refuted and names four fault kinds (`Numeric`, `Presence`, `CountContainment`, `AssignmentQualifier`) with no validity argument at all, which is what holds the Count leg of Decision 2 open
  - § The write-site surface below — the shared foundation this design and the linkage design both range over, incorporated in identical text in both documents
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
  - src/Precept/Language/ActionKind.cs — the fifteen action kinds; src/Precept/Language/ActionSyntaxShape.cs and Action.cs:132 — the syntax shapes and `ActionSlotRole.IntoTarget`, which is what makes the weakest precondition's totality question decidable from the catalog
  - src/Precept/Language/ProofRequirementKind.cs — the **thirteen** requirement kinds ("The thirteen proof obligation kinds that catalog entries can declare"), the denominator Decision 2's arithmetic must close against
  - src/Precept/Pipeline/ProofEngine.Satisfiability.cs — ScanRulesAgainstDefaults, the rule-vs-default fold
  - src/Precept/Pipeline/ProofLedger.cs — the ProofStrategy enum
  - precept_compile probes (2026-07-23, HEAD) — the modifier/rule asymmetry and the unearned-rule witness
  - local harness probes (2026-07-24, HEAD; `Compiler.Compile`, printing `Diagnostics` + `Proof.Obligations`) — the length and count modifier-vs-rule asymmetries, and the entry-action-on-the-initial-state witness
---

# Constraint establishment and preservation obligations

## Review record

### 2026-07-24 (later) — adversarial review of the rework: E1 **partially closed**, E2 **partially closed**, E3 **not closed**

Status stays **Draft**. The rework is real work and most of its evidence chain holds — every matrix citation was re-checked at rev 11 and resolves verbatim (`:5`, `:53`, `:63`, `:117`, `:122`, `:133`, `:149`, `:172–180`, `:203`, `:217`, `:228`, `:229–232`, `:234`, `:240`, `:294`, `:355`, `:442`, `:459`, `:490`), the § 8 built-status witnesses reproduce at HEAD through the local harness, and Decision 2's denominator arithmetic closes exactly against the thirteen-member `ProofRequirementKind` (13 − {IntervalContainment, LengthContainment} − {Dimension, Modifier} = the nine named). What follows is what the rework did **not** close.

**E1 — the quantifier is constraint-kind-blind, so the re-key over-generates for three of the five constraint forms and under-provides for two.** `Owes(C, O) = O.Governed ∧ Targets(O) ∩ mentions(C) ≠ ∅` (§ The write-site surface, 3) has no `ConstraintKind` term, and § Semantic Rules mints `ConstraintPreservation` for every `C ∈ Constraints(file)`. But matrix `:63` gives each form a *different* obligation shape: `in S ensure` is "preserved by every write that happens **during residency**", and `to S ensure` / `from S ensure` are "a transition-moment obligation on the entering row" / "on the leaving row". Breaking case: `to Closed ensure Total <= 1000` plus a row `Open -> Open` writing `Total`. `mentions(C) = {Total}`, `Targets(O) ∋ Total`, so `Owes` is true and a preservation obligation is minted on a row that never enters `Closed` — a constraint that does not apply there. The same shape rejects a residency ensure on a row *leaving* its state. Conversely, the design defines only `Establish` and `Preserve`, and § The write-site surface, 5 sets `EstablishmentSites = ∅` for `StateEntry` / `StateExit` / `EventPrecondition` — so the transition-moment obligation those three forms actually need is defined nowhere in this document.

**E1 — construction now owes two obligations for one condition, and the preservation half cannot discharge.** Decision 3 makes `Create` a governed occasion owing "preservation as well as establishment". At construction there is no pre-state, so premise class (d) is empty and `Preserve(C, Create)` is the same proposition as `Establish(C, Create)` — computed by a weaker mechanism. Two obligations, two codes (`PRE0167` and `PRE0168`) on one condition is a base-minimality violation against the matrix's standard, which Decision 5 itself invokes.

**E3 — the totality argument is stated over `ActionKind`, but the plan E1 widened contains steps that have no action kind.** § Semantic Rules rule 2 applies "the authored post-state transformer the catalog declares for **that step's action kind**", and rule 3 refuses when "no transformer is authored for that step's action kind". The transformer inventory is enumerated purely over the fifteen `ActionKind` members (§ Semantic Rules, *What the transformer library covers*; § Open questions 2). W4 (default materialization), W5 (computed recomputation), W6 (`omit` reset), W7 (update patch) are firings with **no action kind at all**, so the three-rule case split is not exhaustive on them — it is a category error, not a false branch. Read charitably as refusal, the consequence is corpus-wide: W4 is in every construction plan, so any constraint mentioning a defaulted field makes `Preserve(C, Create)` `Unresolved`; W5 is in every governed plan, so any constraint mentioning a computed field refuses everywhere. Neither is measured, and neither appears in § The cost, which lists only range / length / collection-refusal.

**E3 — decomposing a two-target action into two sequential steps is unsound at the definition level, and the order is unpinned.** A step is (firing, target) and the walk composes strictly sequentially: `WP(C, s₁; …; sₙ) = Step(s₁, WP(C, s₂; …; sₙ))`. But `dequeue Q into D` writes both targets *simultaneously* from one pre-firing state. Take `rule D <= Q.count` and the plan `dequeue Q into D`. Order (Q-step, then D-step): `Step_D` gives `head(Q) <= Q.count`, then `Step_Q` gives `head(tail(Q)) <= tail(Q).count` — wrong, because the `D` transformer has been rewritten by the `Q` transformer. Order (D-step, then Q-step) gives `head(Q₀) <= tail(Q₀).count` — correct. Nothing in § Semantic Rules pins the intra-firing order, and nothing states that a multi-target firing's transformers are all evaluated against the pre-firing state. The hole is latent today only because the collection transformers are held open; the *definition* is wrong now, and this is the exact case acceptance criterion 19 tests.

**E2 — the held count leg contradicts the universal minting rule, so acceptance criterion 4 is unsatisfiable.** The exclusion is scoped to *modifier desugaring* ("`maxcount` / `mincount` are excluded from the desugaring", § Inventory), not to authored rules. `rule Tags.count <= 2` is a `C ∈ Constraints(file)`, and an occasion appending to `Tags` satisfies `Owes`, so the minting rule mints a `ConstraintPreservation` for it — which then refuses for want of a collection transformer. Acceptance criterion 4 and § Open questions 1 both assert that after this design ships "`rule Tags.count <= 2` continues to mint nothing"; the design as written mints one and rejects. The correct post-ship description is that both spellings reject, under different kinds and different diagnostics.

**Shared-section coupling is already broken at authoring time.** § The write-site surface is not shared verbatim: the linkage copy carries a full four-leg block on Decision W-2 (rationale, three rejected alternatives, precedent, tradeoff); the copy here carries **none of the four**. That is not "first person about its own walk", it is missing rationale — a Per-Decision Rationale violation here, and falsifier 6's failure mode occurring before any code is written.

**Smaller citation and bookkeeping defects.** `soundness-and-coverage.md:201–202` is cited in § Decision 2 *Sources consulted* as "the `LengthBoundViolation` and `CountBoundViolation` rows"; at HEAD those two lines are `OutOfRange` and `LengthBoundViolation` (`CountBoundViolation` is `:203`), which contradicts the same decision's rationale paragraph — that one reads 201–202 correctly. Three rows there name `IntervalContainment` or `LengthContainment` (`:200` `NumericOverflow`, `:201` `OutOfRange`, `:202` `LengthBoundViolation`), not two, so both "two entries need no text change" (Decision 2) and "two rows re-pointed" (§ Blast radius) undercount, and § Doc-update enumeration's three-row list disagrees with both. `catalog-system.md:784` is a section heading, not the quoted sentence. § Inventory says the two containment kinds are "removed as mint kinds" while asserting "member count is unchanged at thirteen" — those are only compatible if the enum members and their DU subtypes are actually deleted, which the sentence does not say.

**What was checked and held.** Matrix rev 11 refresh (all sampled cites verbatim); spec cites `:164`, `:223`, `:229`, `:272`, `:1135`, `:1138`, `:1967`, `:1969`, `:1979–1992`, `:1996–2006`, `:2069`, `:2075`, `:2076`, `:2081–2089`, `:2200`, `:2202`, `:2204`; `ActionKind` = 15 with 1 + 3 + 11 exactly as stated; `ProofStrategy` 7/8/9 at `ProofLedger.cs:108–110`; `ActionSlotRole.IntoTarget` at `Action.cs:132`; the denominator arithmetic; and the plan-set treatment of `:2202`, which does honour the standing default in the refusing direction and does not settle it. The `omit` conflict is likewise left unsettled and refused — though the resulting refusal cluster is a fourth unmeasured component of § The cost that the section does not list.

### 2026-07-24 — rework against the three unlock findings

All three findings are addressed and the document is materially different where they landed. Status stays **Draft**; the owner signs off the re-lock. What changed, per finding:

**Finding 1 (preservation misses non-handler write sites) — closed at the definition level.** Preservation is re-keyed from the handler to the **governed operation occasion** (Decision 3, § Semantic Rules), ranging over construction rows, transition and stateless event rows, and editable-field doors. The whole-write-plan ruling is untouched — the granularity is still one obligation per operation, intermediates unobserved — and the matrix's "keyed per handler" wording is now read as the granularity it is, with the quantifier taken from symmetric attachment (matrix `:228`, `:63`) instead. A state exit or entry action is not its own occasion; it is phase 3 or 6 of the enclosing row's plan (`precept-language-spec.md:2000`, `:2003`), so the `to Closed -> set Total = 5000` witness is covered with no handler required.

The adversarial follow-on — that the obvious fix *relocates* the hole to construction, where an entry action on the initial state would be covered by neither establishment nor preservation — is handled rather than inherited. Construction is a governed occasion and owes both; whether the entry action is in its plan is exactly what `precept-language-spec.md:2202` leaves open, so the construction occasion carries a **plan set** and the obligation must discharge over every member. That honours `:2202`'s standing default in the refusing direction without settling it, and collapses to a singleton — a deletion, not a re-derivation — when the owner rules. New witness, compiled at HEAD 2026-07-24: `to Open -> set Total = 5000` on the **initial** state under `max 1000` already rejects today with no handler in the file, so the conservative reading is also the non-regressing one.

**Finding 2 (Decision 2's scope) — collapse re-scoped to Interval + Length, Count held, denominator stated as nine.** The asymmetry is verified at HEAD for all three containment kinds, not one: `maxlength 3` mints `LengthContainmentProofRequirement` while `rule Name.length <= 3` mints zero obligations, and `maxcount 2` mints `CountContainmentProofRequirement` while `rule Tags.count <= 2` mints zero. Interval and length collapse to discharge strategies (both already exist as `ProofStrategy` members). **The count leg is held**, because matrix rev 11 records `CountContainment` as one of four fault kinds with no validity argument at all — collapsing it ahead of its argument would replace a working narrow check with a fail-open. The resulting § 2.4 violation is recorded as an explicit open hole (§ Open questions) with an acceptance criterion asserting the divergence, so it cannot close silently.

The arithmetic is redone and now closes: thirteen `ProofRequirementKind` members, minus the two re-homed, plus the two added, is thirteen — nine fault kinds, two type-requirement kinds, two constraint kinds. Eleven → ten (rev 10) → **nine** is the denominator path, and eight is explicitly rejected. The review's point that the obligation-family axis stops being a partition is answered structurally: the `FaultCode` moves to the *strategy*, not to the constraint kind, so `soundness-and-coverage.md`'s "prevented by" entries need no text change and the axis stays a partition. The "only two ways" inference is deleted; the third option the review named — selecting the kind by normal form — is now considered explicitly and rejected on two stated grounds, with a re-open trigger.

**Finding 3 (the weakest-precondition rule is not total) — replaced, and totality is now a property of the construction rather than an assertion.** The old rule `WP(C, w;rest) = WP(C,rest)[target(w) := rhs(w)]` is gone. In its place is a backward walk over **steps**, where a step is (writer firing, target field), decided by three exhaustive rules: frame if the target is not free in the residual; else apply the catalog's authored post-state transformer for that action kind; else `Unresolved` and reject. Multi-target actions contribute one step per target, read from `ActionSlotRole.IntoTarget` — a per-`ActionKind` transformer table is rejected explicitly because a one-value-per-action key cannot carry two targets and would silently frame the `into` write, which is BUG-033's shape arriving through the proof engine.

The action-catalog arithmetic is corrected while we were there: the review said eleven of fifteen action kinds are not `field := expr`; the true figure is **fourteen** — exactly one (`Set`, the sole `AssignValue` shape) matches, three write two targets, eleven write one target with no free-standing right-hand side. The collection transformers are **held open** alongside the count leg, and the fallback is refusal, not silence. The validity argument is split into soundness-of-a-step and totality-of-the-function, because conflating them is what made the old argument false.

**Also incorporated: § The write-site surface**, a new section shared verbatim with `obligation-linkage-and-completeness-2026-07-23.md`. It defines the eight §3A.4 writer categories, the four operations, and the derived `WritePlan` / `Targets` / `Owes` functions that both this design's minting walk and the linkage design's `Expected(C)` read. `Restore` stays on the writer list — the transport rule quantifies over all eight (matrix `:355`) — but owes no preservation, through a declared `Governed` attribute on the *operation* rather than a per-category flag; a per-category flag is ill-typed because computed recomputation fires under both governed and ungoverned operations (`precept-language-spec.md:1985`). Without that distinction the linkage design's completeness diagnostic would fire on every file declaring any constraint.

**Matrix citations refreshed rev 10 → rev 11** throughout, each re-read at HEAD; the mapping is tabulated in § The write-site surface, 10.

**Still open after this pass.**

- Decision 1's corpus cost is **still unmeasured**. The 2026-07-23 review measured 7 of 78 samples newly rejecting on establishment with a floor of 23 on the preservation side; that measurement predates the range widening in Decision 3, the length leg in Decision 2, and the collection-refusal cluster, so it is a floor and not an estimate. Falsifier 1's quarter-of-corpus threshold is more likely to trip now, not less.
- The count leg of § 2.4 (§ Open questions 1) and the collection transformer library (§ Open questions 2) are named holes with named prerequisites, not closed.
- `precept-language-spec.md:2202` and the `omit`-reset canon conflict stay owner questions. The design is written to be correct under either ruling of each; neither is settled here.
- **A new coupling.** § The write-site surface must be textually identical in both documents, and `Minted(C)` here must equal `Expected(C)` there on every corpus file. That is the point of the shared section and it is also the new way the two designs can fail together; falsifier 6 is the check.
- No bugs were filed and `bugs.md` was not edited — divergences between today's compiler and this definition are recorded as built-status (§ The write-site surface, 8), per the spec-finalization posture.

### 2026-07-23 — why this was unlocked

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

When this is done, a `rule` and the field modifier that means the same thing produce the same obligations at the same places, and a definition compiles only when every constraint it declares is proven true at creation and proven still true after **every governed operation that can touch it** — not only after its event handlers, but after a state entry or exit action, after an edit through an editable-field door, and after construction itself.

Three files are the test. `rule Other <= 1000` with an unbounded write must reject exactly as `field Other as decimal max 1000` with the same write already does. The `OrderTotals` file — a rule consumed to prove a divisor safe while an unconstrained argument writes the rule's own field — must reject. And `rule Total <= 1000` with `to Closed -> set Total = 5000` and no event handler anywhere in the file must reject, exactly as the `max 1000` spelling of it already does at HEAD.

## Scope

**In scope.**

- The two obligation kinds the linkage design inventoried: what each proves, stated as a proof condition, and where each attaches.
- **The write surface both obligations range over** — § The write-site surface, shared verbatim with the linkage design.
- The discharge contracts, per premise class, each naming the finite check that decides "no licensed derivation exists".
- A validity argument for every discharge rule stated here, to the standard of the arguments already in the matrix.
- Replacing the shipped per-write containment check with the whole-write-plan check the matrix ruled, keyed **per governed operation occasion**.
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

## The problem, in four files

The first three were compiled at HEAD on 2026-07-23 through `precept_compile`; the fourth, added by the 2026-07-24 rework, through the local harness at HEAD on 2026-07-24.

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

Rejected today with `PRE0078`. The compiler mints one obligation per write — two of them — and the first, `set Total = 5000`, computes the interval `[5000 .. 5000]` and fails. But the operation's post-state has `Total` in `[0 .. 100]`, comfortably inside the ceiling, and the second obligation reports `Proved`. The intermediate value 5000 exists only on a working copy that is promoted or discarded whole, so no reader ever sees it. Under the matrix's whole-write-plan ruling this file is legal and must compile. Both directions of the exact-power contract are broken today: the rule spelling accepts too much, and the per-write check rejects too much.

**And the write surface is larger than the handler set.** Compiled at HEAD on 2026-07-24 through the local harness.

```precept
precept InitEntry

field Total as decimal default 0 max 1000

state Open initial
state Closed terminal

to Open
    -> set Total = 5000

event Finish()

from Open on Finish
    -> transition Closed
```

Rejected — `NumericOverflow`, with an `IntervalContainmentProofRequirement` `Unresolved` and a `NumericProofRequirement` `Proved` on the default. There is no event handler in this file that writes `Total`; the write lives in a state entry action, on the **initial** state, so it fires during construction if it fires at all. Replace `max 1000` with `rule Total <= 1000` and the file compiles clean with zero obligations. Two things follow. First, any design that keys preservation to handlers loses the modifier spelling's rejection here — that is the soundness hole this document was unlocked for. Second, any design that fixes that by ranging over post-construction occasions only loses it again, one occasion to the left. Both are addressed in Decision 3.

## What is already settled

Guard 17 discipline: these are cited and implemented against, not re-decided here.

| Already settled | Where |
|---|---|
| The five constraint forms and their obligation shapes | matrix § Constraint kinds |
| Obligations run over the **whole write plan**, one per operation, never per individual write; the weakest precondition is computed by backward substitution through the writes in reverse order. (The matrix's wording is "keyed per handler"; that fixes the *granularity* — one obligation per operation, intermediates unobserved — and is not the quantifier. The quantifier is symmetric attachment, next row but one. Decision 3 records the misreading and its correction.) | matrix `:53` |
| A constraint **mentions** a field through its condition, its activation guard, or transitively through a computed field; a desugared cross-field modifier mentions both fields | matrix `:229–232`, `:234` |
| **Symmetric attachment** — a constraint's obligations attach to *every write site of every field the constraint mentions*, and a `rule` is "established at construction; preserved by every write site of every mentioned field" | matrix `:228`, `:63` |
| Four premise classes: field modifiers, argument constraints, the handler's guard, and all constraints holding in the pre-state | matrix § Vocabulary, *Premise classes* |
| The discharge contract is exact — a program compiles when and only when a written derivation licenses it — and every contract names its decision procedure | matrix § Vocabulary, *Discharge contract* |
| Establishment as a distinct site set applies to exactly two kinds: `rule` at construction, and residency `ensure` at construction plus every entry into its state | linkage design § Open questions |
| A proof consuming a breakable fact cites that fact's coverage record | linkage design, Decision 1 |
| Constraint modifiers are shorthand for rules and participate in proof identically | `precept-language-spec.md` § 2.4 |
| An unresolved verdict **rejects** — not because it might be broken, but because the definition left a reachable case with no authored disposition | `precept-language-spec.md` § 0.6, Proof philosophy 2 and 5 |
| Actions in a chain are sequenced; a reassignment invalidates prior facts about that field | `precept-language-spec.md` § 0.6, Proof philosophy 7 |
| Rules operate in field scope and cannot reference event arguments | `precept-language-spec.md` § 3A.1 |

## The write-site surface

*This section is the shared foundation, incorporated 2026-07-24. It is shared verbatim in substance with `docs/Working/obligation-linkage-and-completeness-2026-07-23.md` — the two copies differ only where each speaks in the first person about its own walk. It exists because that design's completeness check compares its expected set against the set this design mints, and the comparison is only meaningful if both walks range over the same surface. Neither document may change its **content** unilaterally; a substantive change here is a change to both, and a divergence between the copies is a defect in both. Section numbers below are internal to this section.*

Both designs walk the write surface, and before this rework they walked different ones. This design minted preservation per **handler**; the linkage design's `Expected(C)` quantified over **`Handlers`**. Neither quantifier reaches a state-action write, an editable-field edit, or a computed recomputation — and `Minted(C) = Expected(C)` only held because both walks were wrong in the same way. Fix one and the completeness check fires on every file that has any of the three. This section is the one surface both walks range over.

### 1. Three notions, kept apart

- **Writer category** — a mechanism that places a value into a field slot during one operation. Eight of them, `precept-language-spec.md:1981–1988`. This is a closed list about the *language*.
- **Operation occasion** — one reachable execution of `Create` / `Fire` / `Update` / `Restore` along one route. Per the site-identity ruling, a site is an evaluation occasion, not a syntactic position (`obligation-discharge-matrix-2026-07-19.md:240`).
- **Write plan** — for one occasion, the ordered sequence of firings of every writer category that occasion encloses, in the phase order of `precept-language-spec.md:1998–2006`. This is the matrix's existing term (`:53`) and the unit the whole-write-plan ruling quantifies over.

A "write site" is derived, not declared: the triple (occasion, category, target field). The matrix's write-site-category axis (`:172–180`) is a *projection* of the writer list, not a peer list — see § 3.

### 2. The eight writer categories

Enumerated from `precept-language-spec.md` § 3A.4 *What can write during one operation* (`:1979–1988`), each with the phase it occupies (`:1998–2006`) and the operations that can enclose it (`:1996`: "A phase that does not apply to an operation is skipped; the relative order of the phases that do apply never varies by operation").

| # | Category | Spec | Phase | Enclosed by | How its target set is read |
|---|---|---|---|---|---|
| W1 | Action-chain write in a transition row or event handler | `:1981` | 5 | Create, Fire | the action's primary field target, per `ActionMeta` (`src/Precept/Language/Actions.cs`) |
| W2 | The `into` binding second target of `dequeue` / `pop` / `dequeueBy` | `:1982`, `:1990` | 5 | Create, Fire | the action's `ActionSlotRole.IntoTarget` slot (`src/Precept/Language/Action.cs:132`) |
| W3 | A state exit or entry action's action chain | `:1983` | 3 (exit), 6 (entry) | Create†, Fire | same as W1, at the `ConstructKind.StateAction` positions (`src/Precept/Language/ConstructKind.cs:39`) |
| W4 | Default materialization at construction | `:1984` | before 5 (`:2075`) | Create | every field declaring `default`, in declaration order (§ 3.5) |
| W5 | Computed-field recomputation | `:1985` | 7 | Create, Fire, Update, **Restore** | every `<-` field |
| W6 | The `omit` reset on entering a state that omits the field | `:1986` | 4 (`:2001`); at construction, the hollow build (`:2075`) | Create, Fire | the fields the entered state omits (`ConstructKind.OmitDeclaration`, `ConstructKind.cs:35`; `omit` as an access modifier, `catalog-system.md:495`) |
| W7 | An update patch applied to an editable field | `:1987` | 5 | Update | the fields the current state marks `editable` (`ConstructKind.AccessMode`) |
| W8 | Restore writing persisted values into slots | `:1988` | 5 | Restore | every stored field |

† W3-under-Create is the unsettled case. See § 6.

**Reconciliation with the matrix's write-site-category axis** (`obligation-discharge-matrix-2026-07-19.md:172–180`). The matrix axis has five values; they are not five more writers. `handler set` (`:176`) and `collection action` (`:179`) are both W1, split by `ActionKind` — a split the action catalog already owns, so the axis reads it rather than restating it. `entry hook / state action` (`:178`) is W3. `editable-field edit` (`:177`) is W7. `computed-field transitive` (`:180`) is **not a writer at all** — it is mention-set leg (c) (`:232`), which maps a constraint onto the write sites of a computed field's *inputs*; the writer that changes the computed slot itself is W5. Writing that mapping down once is load-bearing: it is the reason the two walks can read one list and produce the same product.

**Double-counting is closed by the collapse, not by deduplication.** Leg (c) reaches occasion `O` through the input's W1 firing, and W5 reaches the same occasion through the computed slot. Because obligations run over the whole write plan per occasion (matrix `:53`, owner ruling 2026-07-20), both routes name the same single obligation. Had the design stayed per-write, the two walks would each have to deduplicate identically or the set comparison would fail — which is a second, independent reason the whole-write-plan ruling is load-bearing here.

### 3. Per-category attributes — and why preservation duty is not one of them

**The adversarial finding this section exists to answer.** `Restore` is a §3A.4 writer (W8) and must stay on the list, because the transport rule's kill leg "unions the write-targets of every firing that *may run strictly between* p and q, over all **eight** writers" (matrix `:355`) — a fact established before a restore does not survive it. But `Restore` owes no preservation obligation: restored state "is trusted as valid at the time it was persisted: hydration is fast and does not re-validate … Such state is reconstituted, not re-governed on load" (`precept-language-spec.md:272`), and § 3A.4 says it in one line — "(Restored state is neither: it is trusted as valid at persistence time and is re-governed only by the next operation's sweep — §0.7.)" (`:1969`). A flat writer list cannot express that.

#### Decision W-1: preservation duty is declared on the **operation**, not on the writer category

`OwesPreservation(category, occasion) = occasion.Governed`, where `Governed` is a declared per-operation attribute: true for `Create`, `Fire`, `Update`; false for `Restore`.

- **Rationale**: a per-category boolean is **ill-typed against canon**, and W5 is the proof. `precept-language-spec.md:1985` declares computed-field recomputation "recomputed in every operation, **including `Restore`**". A per-category `OwesPreservation` on W5 would have to be simultaneously true (under `Fire`) and false (under `Restore`). The duty therefore varies over the (category, operation) pair, and the only axis it actually varies on is the operation — which is also where canon puts it: `:272` is a statement about *the boundary of the guarantee*, an operation-level fact, not a fact about slot-writing mechanisms. Declaring it there makes W8's exemption **derived** (its enclosure is `{Restore}`, and `Restore` is ungoverned) rather than stipulated, so no category carries an exception clause.
- **Alternatives considered and rejected**:
  - *A per-category `OwesPreservation` boolean, false on W8.* Rejected: W5 above. It would also need a second exception the moment any future writer fires under both a governed and an ungoverned operation.
  - *Drop `Restore` from the category list, since it owes nothing.* Rejected: it would silently break the transport rule, which quantifies over all eight (matrix `:355`). Removing a writer to express "owes no preservation" conflates two different uses of one list — precisely the conflation this section exists to prevent.
  - *Keep two lists — a "writers" list for fact survival and a "preservation sites" list for obligations.* Rejected on the catalog rule: two hand-maintained lists of the same language fact drift, and the linkage design's independence argument requires both walks to read *one* declaration, not two.
- **Precedent**: in-tree, `ActionMeta` already separates `WriteSemantics` (`ActionWriteSemantics`, `src/Precept/Language/ActionWriteSemantics.cs`) from `Effect` (`ActionEffectClass`) for exactly this reason — one action's write behaviour is read by two consumers asking different questions (`FieldNeverSet` vs sequential proof flow), and the catalog declares both rather than letting one consumer infer the other. Externally, Event-B's obligation generator produces no obligation for an initialisation that is outside the model's mutating events, without removing that state from the model.
- **Tradeoff accepted**: a reader of the write-site catalog alone cannot answer "does this owe preservation" — they must also read the operation catalog. Accepted because the join is one hop and mechanical, and the alternative encodes a falsehood about W5.

#### What each category declares

`WriteSiteMeta` — a discriminated union, per `catalog-system.md:784` ("When members fall into groups with genuinely different fields, the meta type is a **discriminated union**"), because the *target derivation* genuinely differs in shape per category (an action slot, a modifier scan, a state's omit list, "every stored field"). Fields common to every subtype:

- `Kind` — the closed 8-member enum.
- `Phase` — the § 3A.4 phase ordinal, which is what orders a plan.
- `Enclosure` — the subset of `{Create, Fire, Update, Restore}` under which the category can fire.
- `DeclaredAt` — the existing catalog or spec surface that owns the category's own semantics (see § 7).
- `BuiltStatus` — whether the pipeline consumes it today; the allow-list key for the linkage design's build-time analyzer. Populated in § 8.

`OperationSurfaceMeta` — a 4-member catalog: `Kind`, `Governed`, and whether the operation is an establishment occasion.

Everything the two walks need is then **derived** from those two declarations plus the mention set:

```
WritePlan(O)      = firings of every cat with O ∈ Enclosure(cat), ordered by Phase
Targets(O)        = ⋃ { targets of cat at O | cat ∈ WritePlan(O) }
Owes(C, O)        = O.Governed  ∧  Targets(O) ∩ mentions(C) ≠ ∅
```

No third knob. That matters: every independent attribute is a place the two walks can disagree, and the completeness check's whole value is that they cannot.

**The establishment role needs no separate attribute** — it is `Enclosure` intersected with the establishment occasions (§ 5). W4 contributes only at construction because its enclosure is `{Create}`; W7 never contributes because `Update` is not an establishment occasion; W8 never, for the same reason plus `Governed = false`.

### 4. The preservation unit — occasions, not handlers

#### Decision W-2: `Preserve` is keyed to the **governed operation occasion**

Both designs previously keyed to a handler. Replace, in both:

```
Preserve-units(C) = { O ∈ Occasions(file) | Owes(C, O) }
```

where `Occasions(file)` is:

- one occasion per **construction row** (`Create`); reject rows carry no data obligation (matrix `:184`);
- one occasion per **transition row** and per stateless **event row** (`Fire`); reject rows excluded, as above;
- one occasion per **editable-field door** — the (state, editable field) pairs the access modes declare (`Update`); the matrix already treats this as a first-class category with its own case shape and its own discharge, ingress evaluation (matrix `:177`, `:442`);
- **no occasion for `Restore`** — `Governed = false`, so `Owes` is false for every constraint.

The last bullet is the one that keeps the completeness check quiet. `Restore` is available on every file. If `Expected(C)` included it (it is a §3A.4 writer) while the minting walk did not, the linkage design's completeness diagnostic would fire on **every file declaring any constraint**. That failure mode is why the attribute in § 3 has to exist, and it is the concrete cost of getting it wrong.

A state exit or entry action is **not its own occasion**. It is phase 3 or phase 6 of the enclosing row's plan (`:2000`, `:2003`), so the entry-action write folds into that row's single obligation. This is what closes review finding 1: the `to Closed -> set Total = 5000` witness, which today rejects with `PRE0078` while the compiler reports `eventHandlers: []`, is covered because the write belongs to the plan of the row that enters `Closed` — no handler required.

#### Decision W-3: both walks call the same function

The minting walk and `Expected(C)` differ only in *where they get the category list*, never in what they compute from it. This design's minting rule becomes:

```
  C ∈ Constraints(file)   O ∈ Occasions(file)   Owes(C, O)
  ──────────────────────────────────────────────────────────
     ConstraintPreservationProofRequirement(C, O)  minted
```

and the linkage design's expected set becomes:

```
Expected(C) = { Establish(C, e) | e ∈ EstablishmentSites(C) }
            ∪ { Preserve(C, O)  | O ∈ Occasions(file), Owes(C, O) }
```

Same product, two derivations from one declaration — which is exactly the independence the linkage design requires, and it now holds over a surface that actually covers the write plan. The linkage design's sentence "`writes(h)` is the union of the fields written by the writers § 3A.4 enumerates for that handler" is replaced by `Targets(O)`; the phrase "for that handler" was the defect.

**One correction the linkage design owes itself.** Its § Semantic Rules said a field modifier's expected set uses "`mentions` replaced by `{F}`", four lines after stating that a desugared cross-field modifier mentions both fields. The matrix is explicit: `field Floor as decimal max Ceiling` "desugars to a constraint mentioning **both** fields, so a write to `Ceiling` carries the obligation just as a write to `Floor` does" (matrix `:234`). Under § 2.4 a constraint modifier *is* a rule, so a modifier has no separate rule — `mentions(·)` applies unchanged and the `{F}` clause is deleted.

### 5. Establishment sites

Unchanged in substance from what both docs already carried; restated here so one place owns it.

```
EstablishmentSites(C) = { the construction occasion(s) }                              for ConstraintKind.Invariant
                      = { construction } ∪ { every row entering S }                   for StateResident on S
                      = ∅                                                             for StateEntry, StateExit, EventPrecondition
```

The three edge-triggered and ingress-class kinds have no separate establishment site: the transition-moment obligation *is* the check (matrix `:63–67`; "nothing is owed while merely resident"), and an event precondition is discharged by ingress evaluation. Residency establishment at **every** entry, not only the initial state, is the 2026-07-20 activation-sites ruling (matrix `:237`), and the matrix records that today's engine folds residency ensures against defaults for the initial state only (`:69`) — implementation gap, not a definitional limit.

### 6. The construction case

Canon is not ambiguous about *whether* creation is governed. It is ambiguous about *one phase* of the construction plan. The surface is written so that the second ambiguity never touches the first.

**What the surface asserts.**

1. `Create` is a governed operation. Construction "fire[s] the initial event with the caller's args through the standard pipeline — same guards, same mutations, same ensures, same constraint checking as any other event" (`:2076`), and the entity "is governed from birth by the same type constraints, rules, and state-scoped ensures that apply to its initial state" (`:2069`). Establishment at construction is implementation against canon, not a design choice.
2. Every constraint that applies at the initial state must be **established** over the construction plan. The composing sets are canon and need no new form: arg ensures, field constraints, global rules, entry ensures, residency ensures (`:2081–2088`), and "No special 'construction constraint' form is needed. `to <InitialState> ensure` is the natural construction-time rule" (`:2089`).
3. The construction plan contains, in order: W4 default materialization and the hollow build's omit application — "Build a hollow version (defaults applied, initial state set, omitted fields structurally absent)" (`:2075`) — then W1 and W2 from the initial event's action chain (phase 5), then W5 recomputation (phase 7), then constraint evaluation (phase 8).
4. Under prove-or-reject, a constraint whose truth at creation cannot be established **rejects**. That is the engine working. Corpus rejections are measured and listed, never used as an argument to weaken (1)–(3).

**What the surface deliberately does not assert.**

5. **Whether W3 (entry actions) is in the construction plan.** `:2202` states the standing default: "Until these are ruled, no analysis may assume an ordering among several state actions on one state, nor that entry actions run at construction." The surface therefore carries, for the construction occasion, a **plan set** rather than a plan: `{ plan-without-W3 }` when the initial state declares no entry actions, and `{ plan-without-W3, plan-with-W3 }` when it does. Establishment holds iff it holds over **every** member. This is still one obligation per (constraint, occasion) — the whole-write-plan ruling is untouched; the obligation's proof condition is a conjunction over the set.
6. **Any order among several state actions on one state.** Same sentence at `:2202`. Where a state carries *k* state actions, the plan set carries one plan per admissible ordering and the obligation must discharge over all of them. The definitional statement is the enumeration; an order-independent abstraction is a legitimate implementation of it only if shown to accept exactly the same programs, and that equivalence is not asserted here.
7. **Self-transitions.** `:2202` leaves entry actions on a self-transition unsettled in the same breath; the same plan-set treatment applies to a self-transition row, with no further claim.
8. **What the `omit` reset leaves in the slot.** W6's *targets* are declared (the omitted fields). Its *post-state value* is conflicted canon — "canon asserts both reset-to-default and structurally-absent semantics" (matrix `:294`, `:459`, ⧖ Q13). No discharge rule may assume either reading; a proof needing W6's post-state value is `Unresolved` until the conflict is ruled.

The direction of (5)–(7) is refusal, not admission: a file whose safety depends on which reading is true does not compile. That is sound under either ruling, and when the owner rules, the plan set collapses to one member — a deletion, not a re-derivation. Note that the conservative reading is also the *shipped* behaviour: `to Closed -> set Total = 5000` is rejected at HEAD (`PRE0078`, `IntervalContainment` `Unresolved` at `[5000 .. 5000]`) regardless of which rows reach `Closed`.

### 7. Where it lives

**A suitable surface exists for three of the eight categories and must be reused, not forked.**

- `ActionMeta` already carries `WriteSemantics` (`ActionWriteSemantics`) and per-slot roles (`src/Precept/Language/Action.cs`). W1's target derivation is `ActionMeta`'s existing primary target; W3's is the same catalog read at the `ConstructKind.StateAction` position (`ConstructKind.cs:39`).
- W2's target is **already declared**: `ActionSlotRole.IntoTarget` (`Action.cs:132`), populated for `ActionSyntaxShape.CollectionInto` and `CollectionIntoBy` (`Actions.cs:504`, `:548`). The catalog knows about the second target; the pipeline does not consume it (§ 8). W2 must read that slot role, not re-enumerate which actions have an `into`.
- W6's target derivation reads `ConstructKind.OmitDeclaration` (`ConstructKind.cs:35`) and the `omit` access modifier (`catalog-system.md:495`); W7's reads `ConstructKind.AccessMode` (`:31`).

**A surface does not exist for the writer categories themselves, and the spec says so.** `precept-language-spec.md:1992`: "Actions are catalogued and their write semantics are catalog metadata; the other five writers are not catalogued at all — the `omit` reset in particular has no catalog member. The intended end state is that every writer is catalog-declared and that the exhaustiveness analyzer used elsewhere for catalog coverage is applied to the write surface." The linkage design's `WriteSiteCategories` catalog entry is therefore the *already-intended* end state, not a new invention — and this section is what it declares.

**No catalog surface exists for the four operations.** `OperationKind` / `Operations.cs` is the typed-operator catalog, unrelated. `Create` / `Fire` / `Update` / `Restore` appear only in the runtime (`src/Precept/Runtime/Precept.cs`, `Evaluator.cs`, `RestoreOutcome.cs`) and in spec prose (`:1996`). `OperationSurfaceMeta` is a new 4-member catalog and is the smallest new surface this foundation requires.

**Prose vs metadata split.** What goes in the catalog: the eight categories, their phase, their enclosure, their target derivation, the four operations and their `Governed` flag. What stays design prose: the derivations in § 3 (`WritePlan`, `Targets`, `Owes`), because they are functions over declared facts, not declared facts. What must not be written anywhere: a second copy of the action list, the `into`-bearing action set, or the omit/editable field sets — all four already have catalog owners.

**Consequence for the linkage design's `MentionSetLegs` entry.** Legs (a) and (b) are scans of a constraint's own expression; leg (c) is the computed-field dataflow. Only leg (c) touches this surface, and it touches it as a *mapping onto W1/W2 targets*, not as a category. The linkage design's inventory should say so, or a reader will expect a ninth category.

### 8. Built status per category

Recorded as built-status on the surface, per the spec-finalization posture. Not defects of the definition; no bugs filed. All witnesses compiled at HEAD on 2026-07-24 through the local harness (`Compiler.Compile`, printing `Diagnostics` + `Proof.Obligations`).

| Category | Minted today? | Witness |
|---|---|---|
| W1, modifier spelling | **yes** | `field Total as decimal default 0 max 1000` + `event Start(N as decimal) initial` / `on Start -> set Total = Start.N` → `IntervalContainmentProofRequirement` `Unresolved`, `NumericOverflow` error |
| W1, rule spelling | **no** | same file with `rule Total <= 1000 because "capped"` instead of `max` → `HasErrors=False`, **zero obligations** |
| W1, collection action | **yes** | `field Tags as list of string maxcount 2` / `append Tags "a"` → `CountContainmentProofRequirement` `Unresolved`, `CountBoundViolation` |
| W2 `into` target | **no** | `field D as integer default 0 max 10` / `dequeue Q into D` → only the default check on `D`; the compiler additionally warns `FieldNeverSet` on `D`, i.e. it does not treat the `into` slot as a write site at all (the BUG-033 shape) |
| W3 state action, modifier spelling | **yes** | `to Closed -> set Total = 5000` under `max 1000` → `IntervalContainment` `Unresolved` with no enclosing handler (review finding 1's witness, reproduced) |
| W3 state action, rule spelling | **no** | same file with `rule Total <= 1000` → `HasErrors=False`, **zero obligations** |
| W4 default materialization | **partial** | modifier spelling mints a real obligation (`NumericProofRequirement` "Default value of 'Total' must satisfy 'max'", `Proved`, strategy `Literal`); rule spelling produces a **diagnostic with no obligation** — `field Total as decimal default 5000` + `rule Total <= 1000` → `DefaultViolatesRule` error, **zero obligations**. This is the report-vs-obligation asymmetry Decision 1 addresses, now with an obligation-count witness rather than a code comment |
| W5 computed recomputation | **no** | `field Doubled as integer <- A * 2` + `rule Doubled <= 10` + `on Bump -> set A = Bump.N` (unconstrained arg) → `HasErrors=False`, **zero obligations** |
| W6 `omit` reset | **no** | `precept-language-spec.md:1992` — "the `omit` reset in particular has no catalog member"; `:2200` adds it "has neither a catalog member nor an implementation" |
| W7 update patch | **no** | `field Total as decimal default 0 nonnegative max 1000 editable` → only default checks; and `field Total as decimal default 0 editable` + `rule Total <= 1000` → `HasErrors=False`, **zero obligations** |
| W8 restore injection | n/a | owes nothing (§ 3); its only live use is the transport rule's fact-survival quantification |

Read across: **five of the eight categories mint nothing at all today, and two of the three that do, do so only for the modifier spelling.** That is the size of the gap both designs are closing, stated once.

### 9. Open dependencies this surface names and does not close

1. **Whether the eight-writer list is complete.** Asserted by the spec, not kept by the build (`:1992`, `:2204`). Every soundness argument resting on this surface inherits it. The build-time analyzer in the linkage design is what converts it from silent to loud.
2. **Entry actions at construction and state-action ordering** (`:2202`). Handled by the plan set (§ 6), not settled.
3. **The `omit` semantics conflict** (matrix `:294`, `:459`, ⧖ Q13). W6's targets are declared; its post-state value is not usable as a premise.
4. **Premise class (d) across a `Restore`.** `Restore` is ungoverned, so it owes no preservation — but the induction still needs a base case for a restored entity. Canon supplies one, and it is narrower than it looks: "the prior committed state satisfied the rules in effect **when it was written**" (`:272`). Under an unchanged definition that is the base case; under a changed definition it is not, and `:1958` points forward to migration logic that does not exist. The matrix's own argument leans on the same clause (`:276`). Named here, not resolved — it is the honest cost of `Governed = false` on `Restore`, and it belongs to whoever writes the establishment validity argument.
5. **The residency fact as a premise** (matrix `:203`). Untouched by this surface; Decision 4 keeps everything independent of it.

### 10. Matrix citations refreshed (rev 10 → rev 11)

Every matrix line cite in both documents was stale. Each was re-read at HEAD and replaced throughout; the mapping is recorded here once so a reviewer can spot-check the refresh rather than re-derive it.

| Quoted text | Cited as | Resolves at rev 11 |
|---|---|---|
| "A completeness check that computes 'sites that should have minted' …" | `:113` | **`:117`** |
| "Tests check the cases someone thought of …" | `:114` | **`:118`** |
| "Prior art in this codebase points at build-time checking …" | `:116` | **`:120`** |
| "names the obligation that established the fact and every obligation that preserves it" | `:118` | **`:122`** |
| "The duty applies to a consumed fact **iff** some operation can establish or break it" | `:127` | **`:133`** |
| "the fault family is the eleven fault-preventing kinds" | `:143` | **`:149`** |
| "whether the residency fact … is itself available as a premise" | `:197` | **`:203`** |
| "**Power-widening** … Monotone: everything previously licensed stays licensed" | `:429` | **`:490`** |
| "Warning severity would also create exactly the two-tier trustworthiness …" | `:211` | **`:217`** |
| "how a failed multi-write proof reports … That is diagnostic quality" | `:49` | **`:53`** |
| `provdeps` as the type-structural discriminator (§ Rule validity) | § ref | **`:359–363`** |
| Symmetric attachment / mention-set legs / cross-field modifiers | § ref | **`:228`, `:229–232`, `:234`** |
| Write-site-category axis | § ref | **`:172–180`**; rule/Invariant row **`:63`** |

## Philosophy Alignment

| Principle | Affected? (Y/N) | How served (1 sentence + cite) | Tension (1 sentence or N/A) | Tradeoff (1 sentence or N/A) |
|---|---|---|---|---|
| 1. Prevention, not detection | Y | A declared rule becomes structurally enforced on **every governed operation** — including state actions, editable-field doors and construction — rather than on the handler subset, closing the gap `philosophy.md:49` names between declaring a rule and having it hold on every path. | The count spelling is still not enforced after this design ships (Decision 2's held leg), so one declared-bound shape remains detectable-not-prevented; it is named in § Open questions rather than papered over. | Files that compile today stop compiling; § The cost. |
| 2. One file, complete rules | Y | Every premise is a declaration in the same file — the four premise classes are all authored constructs (matrix § Vocabulary), and no external oracle enters (spec § 0.6, Proof philosophy 4). | N/A | N/A |
| 3. Deterministic semantics | Y | The backward step walk over a fixed phase order and a finite premise set is a deterministic function of the file; no search, no solver (spec § 0.6, Proof philosophy 3), and the three step rules are exhaustive so the walk returns a verdict on every plan. | Where `precept-language-spec.md:2202` leaves the plan unsettled, the verdict is a conjunction over the plan set — still deterministic, but determined by a set canon has not yet reduced to one member. | Some safe programs are refused until `:2202` is ruled; § Validity arguments states why that is the sound direction. |
| 4. Full inspectability | Y | A failed preservation proof prints the weakest precondition that would discharge it, which is the spec's own stated remedy — "the author resolves an unresolved verdict by supplying the constraint its printed weakest precondition names" (§ 0.6, Proof philosophy 5). | N/A | N/A |
| 5. Keyword-anchored readability | N | N/A — no authored syntax changes; `rule` and `ensure` keep their grammar exactly. | N/A | N/A |
| 6. Explicit domain meaning over primitive convenience | N | N/A — no type or domain-meaning surface is touched. | N/A | N/A |
| 7. Compile-time-first static checking | Y | Both obligations are discharged at compile time with a three-way verdict and no deferral (spec § 0.7, "there is no deferral"). | N/A | Compilation does more work per file; § Operational dimensions. |
| 8. Approximation honesty | Y | Where the write plan's arithmetic is on the approximate lane, the discharge rule's validity argument scopes itself to the exact lanes and says so, rather than claiming a bound it cannot hold. | The strictest reading would refuse approximate-lane preservation entirely; this design instead names the gap and leaves those cells open pending the number-model ruling. | Some approximate-lane programs are rejected that a later ruling may license — the safe direction. |
| 9. Mandatory rationale (`because`) | Y | The failing diagnostic quotes the rule's authored `because` text, so the author sees their own stated reason next to the violation. | N/A | N/A |
| 10. Totality | Y | The `OrderTotals` file is a live counterexample to "a precept that compiles without diagnostics has no unproven arithmetic faults"; making the rule earn its keep restores it, and the weakest precondition is total by construction so no plan escapes with no verdict (§ Semantic Rules). | N/A | Totality is bought by making the uncovered case *refuse*, so plans over collection writes reject until their transformers are authored. |
| 11. Static completeness | Y | Same counterexample at the compiler-to-evaluator bridge — an evaluator divide-by-zero is reachable today from a clean compile. | N/A | N/A |

**Tradeoffs stated.** *Principle 8*: the discharge rules here are proved over exact decimal and integer arithmetic. On the approximate lane, addition rounds, so a bound derived by interval arithmetic needs an outward-rounding side condition that nobody has written. Rather than claim the rule holds there, the design leaves approximate-lane preservation cells open under the existing number-model deferral — refusing rather than over-claiming, which is the direction Principle 8 requires. *Principle 7*: the per-file cost rises because every constraint now produces obligations at every occasion writing any mentioned field — a strictly larger range than the handler set, so the rise is larger than the earlier version of this section estimated. The corpus compiles in about 44ms today, and § Falsifiers sets the threshold at which that trade stops being acceptable. *Principle 1*: the honest statement of what ships is "prevention for the interval and length spellings at every governed occasion, and not yet for the count spelling anywhere" — a partial that is recorded as an open hole with a named prerequisite (§ Open questions), never as a completed equivalence.

**Companion commitments.** *Stateless-first-class*: `rule` establishment and preservation are defined over construction and over event occasions, both of which exist in a stateless precept; only the residency-ensure site set has no instances there, and the design never routes a general behaviour through a state-dependent path. *Domain-expert-primary-author*: the two new diagnostics name the author's own rule, their own `because` text, and the specific line that breaks it, and the repair they suggest is a constraint on an argument or a guard — both constructs the author already knows.

## Language Design Grounding

*Scope note*: this design introduces no authored syntax. It adds catalog member names and two diagnostics, which reach authors through messages and MCP output, so the grounding is carried.

**General language design.** The shape here is the standard one for verifying invariants over mutable state, and Precept is adopting it rather than inventing it. An invariant over a variable generates one obligation that the initial configuration establishes it, and one obligation per mutating operation that the operation preserves it. Event-B's Rodin, the one surveyed comparator that ships a soundness argument for its obligation generator, does exactly this:

> "The resulting model now gives rise to 6 proof obligations in total; 3 of these are to verify that the initialisation establishes invariants inv2 to inv4 and 3 are to verify that the register event maintains invariants inv2 to inv4."

The mechanism for the preservation half is Dijkstra's weakest precondition: to prove a constraint holds after a sequence of writes, walk backwards through the writes and prove the resulting condition holds before them. Precept's version is easier than the general case in one dimension and not easier in another, and it is worth being precise about which. Easier: the language has no loops (§ 0.4), so there is no fixed point to iterate to, and expressions are pure, so nothing changes between statements — the general case in the literature needs loop invariants and Precept's write plan is straight-line by construction. Not easier: the textbook rule is stated for an assignment `x := e`, and only one of Precept's fifteen action kinds has that shape. The rest mutate collections in terms of their own prior value, and three of them write two targets at once. So Precept inherits the *shape* of the mechanism and has to author the per-operation transformers itself, which is the standard situation for any language whose store is richer than scalar variables.

What Precept takes: the establish-plus-preserve pair, and weakest-precondition substitution for the preservation half. What Precept diverges on is the granularity. Standard practice generates one preservation obligation per assignment or per event depending on the tool; Precept generates one per **governed operation occasion**, because its commit model makes intermediate states unobservable — mutations run on a working copy that is discarded whole if any constraint fails (§ 3A.4), so a mid-operation state is not a configuration any reader, later operation, or persistence layer can see. Checking intermediates would reject programs whose end state is fine, which under the exact-power contract is as nonconforming as accepting too much.

Two things follow from taking the occasion as the unit, and both are places Precept is *closer* to the comparators than the earlier draft of this design was. First, the range is over the model's operations, not over a syntactic subset of them: Event-B generates a preservation obligation per (invariant, event) pair across every state-changing operation the model declares, and Precept's equivalent range is every governed occasion — which is why a state entry action and an editable-field door are in it and a handler-only reading was a narrowing of the standard shape, not an application of it. Second, the tools in this space all separate the *frame* question from the *substitution* question — Event-B's `INV` obligations discharge trivially for invariants over variables the event does not assign — and Precept's step walk does the same, with framing as the first of its three rules.

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

Write `C` for a constraint, `σ` for a configuration (the field values, plus the lifecycle position where there is one), and `O` for a governed operation occasion whose write plan is the step sequence `s₁; s₂; …; sₙ`. `Occasions`, `WritePlan`, `Targets` and `Owes` are the functions § The write-site surface defines; nothing below re-derives them.

**Establishment.** For each establishment site `e` of `C` (§ The write-site surface, 5):

```
Establish(C, e)   holds iff   for every plan P ∈ PlanSet(e),   σₑ^P ⊨ C
```

where `σₑ^P` is the configuration at that site under plan `P` — for construction, the defaults as materialized plus whatever the initial event's plan writes; for an entry into a residency state, the post-transition configuration of the entering row. In words: at the moment the constraint first has to be true, it is true, and it is true no matter which of the admissible plans canon turns out to license.

`PlanSet(e)` is a singleton wherever canon fixes the plan. It has more than one member only in the cases `precept-language-spec.md:2202` leaves unsettled — entry actions at construction, and the order among several state actions on one state — and the conjunction over the set is what honours that sentence's standing default without settling it. § The write-site surface, 6 states the treatment in full; the consequence here is that the obligation count does not change (one obligation per site) and only its proof condition widens.

**Preservation.** For each governed operation occasion `O` with `Owes(C, O)`:

```
Preserve(C, O)    holds iff   for every plan P ∈ PlanSet(O),   Premises(O) ⊢ WP(C, P)
```

The unit is the **occasion**, not the handler. A handler is one source of occasions; construction rows, state-action-bearing transition rows, and editable-field doors are others, and each of them is a place a declared constraint can be broken. Keying to the handler is what review finding 1 identified as a soundness hole, and § The write-site surface, 4 is where the replacement quantifier is defined.

**The weakest precondition, total by construction.** `WP` is the condition that must hold *before* the plan runs for `C` to hold after it. It is computed by a **backward walk over write steps**, where a step is the pair (writer firing, target field) — not over actions, and not over "assignments":

```
WP(C, ε)             =  C
WP(C, s₁; …; sₙ)     =  Step(s₁, WP(C, s₂; …; sₙ))
```

Read it from the inside out: the innermost application is `sₙ`, the plan's *last* step, and `s₁` is applied last of all — so the walk runs from `sₙ` back to `s₁`, and each `Step` sees the residual `R` that the steps after it already produced. `Step(s, R)` is decided by the write-site and action catalogs, in this order:

1. **Frame.** If `target(s)` does not occur free in `R`, the step is skipped and `Step(s, R) = R`. This is the frame rule, and it is what makes most steps in a real plan cost nothing.
2. **Transform.** Otherwise, apply the **authored post-state transformer** the catalog declares for that step's action kind — the expression, in terms of the pre-step state, that the step leaves in `target(s)`. For `ActionSyntaxShape.AssignValue` (`set F = E`) the transformer is `E`, and rule 2 degenerates to the textbook substitution `[F := E]`. For the collection kinds the transformer is a function of the *pre-state collection*, which is why the naive rule never worked there.
3. **Refuse.** If `target(s)` is free in `R` and no transformer is authored for that step's action kind, `Step(s, R) = ⊥` — the obligation is `Unresolved` and **rejects**. It is never framed-and-continued, and the step is never treated as if it did not happen.

Rule 3 is the totality guarantee, and it is a guarantee about the *function*, not about the transformer library: `WP` returns a value for every plan the language admits, and where the library is incomplete the value is `Unresolved`. That is the only construction under which "the weakest precondition is total" is a true sentence, and stating it any other way is what review finding 3 caught.

**Why steps, not actions.** A step is (firing, target), so a two-target action contributes **two** steps. `dequeue Q into D` contributes a step targeting `Q` and a step targeting `D`, and both are walked. A single per-action classification — one transformer keyed on `ActionKind` — structurally cannot represent this, and the failure mode is not a missing case but a silent one: the `into` write would be framed away and a fact about `D` would survive a write that changed it. That is BUG-033's exact shape, arriving through the proof engine instead of through the graph analyzer. The step's target comes from `ActionSlotRole` (`src/Precept/Language/Action.cs:132`), which already declares `IntoTarget` as a distinct slot — so the catalog knows, and the walk reads it.

**What the transformer library covers, and what it does not.** Verified at HEAD against the action catalog: `ActionKind` has **fifteen** members (`src/Precept/Language/ActionKind.cs`). Exactly **one** — `Set`, the sole `ActionSyntaxShape.AssignValue` (`Actions.cs:71`) — has the `field := expr` shape the naive rule assumed. Of the remaining fourteen, **three** (`Dequeue`, `Pop`, `DequeueBy`, shapes `CollectionInto` / `CollectionIntoBy` at `Actions.cs:111`, `:135`, `:257`) write two targets, and **eleven** write one target with no free-standing right-hand side — their post-state is a function of the pre-state collection (`Add`, `Remove`, `Enqueue`, `Push`, `Clear`, `Append`, `AppendBy`, `Insert`, `RemoveAt`, `Put`, `EnqueueBy`). 1 + 3 + 11 = 15.

The transformer for `Set` is authored here. **The collection transformers are held open**, alongside the Count leg of Decision 2 and for the same reason — the matrix records `CountContainment` as one of four fault kinds with **no validity argument at all** (matrix `:5`, rev 11), and a transformer for `append` whose validity argument does not exist is a fail-open dressed as coverage. Until they are authored, rule 3 applies: a constraint mentioning a collection field, over a plan containing a collection write, is `Unresolved` and rejects. That is refusal, not silence, and it is strictly stronger than today's behaviour on the rule spelling, which mints nothing at all (`rule Tags.count <= 2` with `append Tags Add.T` → `HasErrors=False`, zero obligations, harness 2026-07-24).

**Premises.** `Premises(O)` is the union of the four classes for that occasion: the field modifiers in scope, the constraints on the occasion's arguments — event arguments at an event or construction occasion, the patch value at an editable-field door — the occasion's own guard where it has one, and the constraints holding in the pre-state. The fourth is the inductive hypothesis and is exactly the class the citation duty governs — a proof may use it only by citing the coverage record for the constraint it comes from. An editable-field door has no guard and no action chain; its plan is the single W7 step and its discharge is ingress evaluation (matrix `:442`), which is a discharge mechanism rather than a premise class (matrix § Vocabulary, *Premise classes*).

**Typing the obligation.** No new typing rule. The two obligations are values of the `ProofRequirement` discriminated union, instantiated by the type checker at catalog-declared sites and read by the proof engine, which is the existing contract:

```
  C ∈ Constraints(file)     e ∈ EstablishmentSites(C)
  ──────────────────────────────────────────────────────────
     ConstraintEstablishmentProofRequirement(C, e)  minted

  C ∈ Constraints(file)     O ∈ Occasions(file)     Owes(C, O)
  ──────────────────────────────────────────────────────────────
     ConstraintPreservationProofRequirement(C, O)  minted
```

This is Decision W-3's minting rule, unchanged. The linkage design's `Expected(C)` is the same product computed from the same two declarations by a separate walk; that the two agree is the completeness check, and it is only a check because neither walk is derived from the other.

**Verdicts.** Each obligation carries the three-way verdict § 0.6 already defines: *proven*, *proven-violating* (rejected with a witness configuration), or *unresolved* (rejected, with the weakest precondition printed). Both non-proven verdicts block.

**Soundness preservation.**

*Principle 7.* No guessing is introduced: the weakest precondition is computed by a backward walk over declared steps, not by search, and the premise set is finite and enumerable from the file. Where the walk has no authored transformer for a step, it does not guess one — it returns `Unresolved`, which rejects.

*Principle 10.* The principle is currently violated, not threatened — `OrderTotals` compiles clean with an unproven division. After this, the rule the division rests on must itself be established and preserved, so the divisor's safety no longer stands on an unearned fact.

*Principle 11.* Same counterexample at the compiler-to-evaluator bridge. Two things remain outside the claim, and both are named rather than absorbed. First, the weakest precondition is only as sound as the write plan being complete, which depends on the § 3A.4 writer enumeration being consumed correctly — BUG-033 is a verified instance where it is not, and the enumeration is asserted rather than build-kept (`precept-language-spec.md:1992`). That dependency belongs to the linkage design's completeness check and its build-time analyzer, and is not re-argued here. Second, Decision 2's held count leg means a count bound written as a rule is still not enforced after this design ships; that is a *stated* incompleteness with a named prerequisite, not a silent one, and it is the reason § Open questions is not empty.

## Validity arguments

Every discharge rule stated below carries a truth-preservation argument, per the matrix's rule-validity gate. Each is written to the standard of the arguments already in the matrix: sound modulo honestly-named open dependencies.

**The backward step walk computes the weakest precondition, and returns a verdict on every plan.** The claim has two halves and they are proved separately, because conflating them is what made the earlier version of this argument false.

*Half one — soundness of a step.* `Premises ⊢ WP(C, plan)` implies `C` holds in the post-state. Why each step is truth-preserving: a step is one writer firing against one target field, and the plan is a linear sequence — *"Because there are no loops or branches, there is no join point where two different states must be merged. Each assignment in a row sees the state left by all preceding assignments. This makes sequential flow analysis a linear walk, not a dataflow graph"* (`precept-language-spec.md:164`). Take the three step rules in turn. **Frame** is sound because a writer firing changes exactly the slots its declared targets name and no others, so a residual condition not mentioning `target(s)` has the same truth value either side of the step; this is the frame condition, and it rests on the same write-surface enumeration the transport rule's kill leg rests on (matrix `:355`). **Transform** is sound because the catalog's post-state transformer for a step is by definition the expression, over the pre-step state, that the step leaves in the target — so replacing the target with the transformer yields a condition on the pre-step state with the same truth value as the original on the post-step state. For `Set` the transformer is the assigned expression and this is the standard assignment axiom; Precept satisfies its side condition (no aliasing, no pointers) by construction, since a field name denotes exactly one slot. **Refuse** is sound vacuously: it licenses nothing. Iterating from the last step backwards composes these one-step equivalences; because the plan is straight-line, the composition is finite and needs no fixed point.

*Half two — totality of the function.* `WP` returns a value for every plan the language admits. This is true by the three rules being **exhaustive on a step**: either the target is free in the residual or it is not (frame), and if it is, either a transformer is authored for that action kind or it is not (transform / refuse). There is no fourth case, and in particular there is no case in which an unhandled step is passed over. The earlier version of this argument claimed totality from "each write has one right-hand side", which is false against the action catalog — fourteen of fifteen action kinds have no right-hand side in that sense, and three write two targets. Totality is recovered not by widening the transformer library but by making the third case a verdict.

*What totality does not buy.* It does not mean every plan gets a proof. With the collection transformers held open, every plan containing a collection write against a mentioned collection field returns `Unresolved` and rejects. Totality means the function is defined there and the answer is refusal — which is what prove-or-reject requires and what the earlier statement obscured.

**Open dependencies**: the plan must contain every write the operation performs — the eight-writer enumeration in § 3A.4, asserted rather than build-kept (`:1992`), with one verified instance of its consumption being wrong (BUG-033); the sequencing must be the evaluator's actual order, which § 3A.4 pins for the nine phases (`:1998–2006`) and leaves unsettled at `:2202` for entry actions at construction and for the order among several state actions on one state — handled here by the plan set rather than assumed away; and every authored transformer needs its own truth-preservation argument, of which exactly one (`Set`) exists today.

**The plan set discharges over its members, and that is the conservative direction.** The claim: taking `Establish` / `Preserve` to hold iff the condition holds under every admissible plan is sound under either eventual ruling of `precept-language-spec.md:2202`. Why: the true plan, whatever the owner rules, is one of the members — the set is constructed to contain both readings and nothing else — so a conjunction over the set implies the condition under the true plan. The converse fails, which is the cost: a file safe under the true plan but unsafe under the other is rejected. That is the refusing direction, and refusal is the direction the sentence at `:2202` names ("no analysis may assume … that entry actions run at construction"). When the ruling lands, the non-licensed member is deleted and every previously-accepted file still compiles — a power-widening, the cheap amendment class (matrix `:490`). **Open dependency**: none beyond `:2202` itself; the argument is written so that the ruling is a deletion rather than a re-derivation.

**Intermediate states need not be checked.** The claim: proving the post-state satisfies `C` is sufficient, even when an intermediate state violates it. Why: the runtime executes the whole plan on a working copy and promotes it only if every constraint passes, discarding it otherwise — *"An invalid configuration never exists, even transiently. There is no window between mutation and constraint checking where a partially-committed state with violated rules can be observed"* (`precept-language-spec.md:1967`). So an intermediate state is not a configuration any observer can reach, and the induction the pre-state premise class rests on quantifies over reachable configurations only. **Open dependency**: this argument is about the documented evaluator, and the runtime that implements the working-copy model is not built; if a future runtime commits incrementally, the argument fails and the per-write check would be the correct one.

**An argument constraint is true of the value the handler evaluates.** The claim: a constraint declared on an event argument may be assumed when proving the weakest precondition. Why: governance enforces every declared constraint on every value entering from outside, *"at the moment it enters, before any computation derives from it"* (§ 0.7), and the composition paragraph in the same section makes this the licensed pairing — the compiler proves the structural fact that the value carries its constraint, governance makes the constraint true of the value. This is the same argument the matrix's existing arg-bound interval rule rests on, applied to a whole condition rather than a numeric bound. **Open dependency**: none beyond the ones that rule already carries.

**The default configuration is a known configuration.** The claim: establishment at construction can be decided by folding the constraint against the declared defaults. Why: a default is an authored literal or a constant expression, evaluated once in declaration order at construction (§ 3.5), so the configuration is statically known wherever every mentioned field's default is foldable. The existing implementation already computes this and shares one default environment and one constant-fold evaluator with the initial-state ensure check. **Open dependency**: when a mentioned field's default is not foldable — a computed field, or a non-constant default — the configuration is not statically known, and the obligation is `Unresolved` rather than proven. Decision 1 is precisely about what that verdict does.

**A guard fact reaches the write it justifies.** Not restated — this is the matrix's existing guard-match argument together with the transport rule's frame-and-kill, both of which are written and carry their own open dependencies. Preservation proofs consuming a guard fact inherit them unchanged.

## Architecture Grounding

### Precept-internal placement

**Layer placement.** The obligations are catalog metadata — two subtypes of the `ProofRequirement` discriminated union, instantiated by the type checker at declared sites and read by the proof engine. That is the existing contract, stated in the proof-engine doc: *"The proof engine does NOT maintain its own list of what needs to be proved."*

The step walk is pipeline code in the proof engine, not catalog metadata, because it is an algorithm over expression trees rather than a declared fact. What *is* catalog metadata is everything the walk branches on, and this is the place the catalog rule bites hardest:

- **Which categories fire under which operation, and in what order** — `WriteSiteMeta.Enclosure` and `.Phase`. The walk orders a plan by reading the phase ordinal; it does not restate `precept-language-spec.md:1998–2006` in its own logic.
- **Which field(s) a step targets** — `ActionMeta`'s primary target and `ActionSlotRole` (`Action.cs:132`), which already declares `IntoTarget` as a distinct slot. This is what makes the two-target case a catalog read rather than a special case in the algorithm, and it is why the walk enumerates steps rather than actions: a per-`ActionKind` transformer table is a one-value-per-action key, and a one-value-per-action key structurally cannot carry two targets. Choosing that shape would reintroduce BUG-033 inside the proof engine.
- **The post-state transformer for a step** — new catalog metadata on the action entry, and the only genuinely new per-action declaration this design adds. Authored for `Set` today; held open for the collection kinds.
- **Whether an operation owes preservation** — `OperationSurfaceMeta.Governed`.

The walk switches on none of these by enum identity: it reads `Phase`, reads `Enclosure`, reads the slot roles, and looks up the transformer. Where the lookup misses, it returns `Unresolved` — which is the catalog-driven form of "the language has a member this consumer was not taught about", and is why the missing-transformer case is a verdict rather than an unhandled branch.

The discharge strategies are proof-engine code, as the existing eleven are. No new strategy enum member is needed for the common cases: the existing `IntervalContainment`, `LengthContainment`, `GuardInPath`, `CompositionalConstraint` and `Literal` strategies discharge the substituted conditions they already handle in their present form — which is the point of Decision 2 re-homing the two containment kinds to strategies rather than inventing anything.

**Cross-component propagation.**

- *Runtime (parser, type checker, evaluator, diagnostics)*: the type checker gains the minting of both kinds, ranging over `Occasions(file)` rather than over handlers. The proof engine gains the backward step walk and loses the per-write interval and length containment paths in `Actions.cs`. Two new catalogs land — `WriteSiteMeta` and `OperationSurfaceMeta` — shared with the linkage design. Diagnostics gain two codes. The parser, lexer and evaluator are untouched.
- *Tooling (syntax highlighting, completions, hover, semantic tokens)*: hover over a rule gains its establishment status and its preservation status per governed operation occasion. No highlighting, completion or semantic-token change.
- *MCP (vocabulary, DTOs, tool output)*: the two new requirement-kind names enter the vocabulary through the existing catalog formatter. `precept_compile`'s obligation projection carries them with no shape change — they are additional values of an existing enum, and the DTO already projects requirement kind, disposition and strategy.

**Breaking changes.** Yes.

1. `ProofRequirementKind` loses `IntervalContainment` **and `LengthContainment`** as mint kinds and gains two members (Decision 2); `CountContainment` stays. The member count is unchanged at thirteen, so the analyzer enforcing one-to-one correspondence between the enum and the discriminated union makes this compile-checked without a count-based test needing an update — a place where "it still builds" is *not* evidence the change landed, and the tests in § Acceptance criteria are what carry that instead.
2. Files that compile today stop compiling — the substance of the change.
3. `PRE0078`'s emission moves from the per-write path to the per-occasion path, so its span changes from a write to an occasion — a row, a state action, an editable-field declaration, or the construction row. Note that `PRE0078`'s message is separately wrong (BUG-034 — it reports a bound violation as a range overflow); this design does not fix that, and moving the emission does not make it worse.

### External architectural precedent

The architectural question is where obligation generation sits relative to the constraint's declaration.

Event-B's Rodin generates obligations from the model's declared structure: one initialisation obligation per invariant, one preservation obligation per invariant-event pair, produced by a static generator that walks the model rather than by the prover. Precept takes that placement exactly — the type checker stamps requirements at declared sites, the proof engine discharges them, and neither invents its own list.

Precept diverges on granularity, and the divergence is forced by a difference in the execution models rather than chosen. An Event-B event's actions are simultaneous, so there is no intermediate state to reason about. Precept's actions are sequenced and each sees the state left by the previous one (`precept-language-spec.md:164`), which would ordinarily mean checking after each. Precept does not, because its commit model makes intermediates unobservable — the working copy is promoted or discarded whole. So Precept ends up at Event-B's granularity by a different route: not because the writes are simultaneous, but because their intermediate results are unreachable.

The weakest-precondition mechanism itself is standard and needs no defense; what is worth naming is which of its usual difficulties Precept escapes and which it does not. Tools in this space spend most of their engineering on **loop invariants** and **aliasing**, and Precept has neither problem: no loops (§ 0.4), and a field name denotes exactly one slot. So the walk terminates in one pass, needs no fixed point, and introduces no approximation step.

What Precept does *not* get for free is the per-operation transformer. In an imperative verifier every statement is an assignment and the substitution rule is uniform; in Precept only one of fifteen action kinds is (`Set`), three write two targets, and the rest transform a collection in terms of its own prior value. That is not a harder version of the same problem — it is closer to a small algebra of data-structure operations, each needing its own post-state characterization, which is where a separation-logic or theory-of-arrays treatment would normally enter. This design does not import one. It authors the `Set` transformer, holds the collection transformers, and makes the missing case a refusal (§ Semantic Rules). The honest comparison is therefore: Precept's *control flow* is trivially easier than the comparators', and its *store model* is not automatically easier — the collection actions are the place the standard mechanism does not simply drop in.

## Inventory of what will be built

**Catalog.**

- `ProofRequirementKind.ConstraintEstablishment`, `ProofRequirementKind.ConstraintPreservation` — added; `ProofRequirementKind.IntervalContainment` and `ProofRequirementKind.LengthContainment` — removed as mint kinds; `CountContainment` — **retained**, per Decision 2's held leg (`src/Precept/Language/ProofRequirementKind.cs`). Member count is unchanged at thirteen.
- `ConstraintEstablishmentProofRequirement(ConstraintIdentity Constraint, EstablishmentSite Site, string Description)` and `ConstraintPreservationProofRequirement(ConstraintIdentity Constraint, OperationOccasion Occasion, ImmutableArray<string> MentionedFieldsWritten, string Description)` — two sealed subtypes (`src/Precept/Language/ProofRequirement.cs`). The second field is the **occasion**, not a handler identity; `OperationOccasion` discriminates construction row / transition row / stateless event row / editable-field door, which is the `Occasions(file)` enumeration of § The write-site surface, 4.
- `ProofRequirementMeta` entries for both, each naming its diagnostic code (`src/Precept/Language/ProofRequirements.cs`).
- `WriteSiteMeta` — the eight-member writer catalog (a DU; `Kind`, `Phase`, `Enclosure`, `DeclaredAt`, `BuiltStatus`, plus per-subtype target derivation), and `OperationSurfaceMeta` — the four-member operation catalog carrying `Governed`. Both specified in § The write-site surface, 3 and 7. **Shared with the linkage design**: these are one declaration read by two walks, not two catalogs.

**Pipeline.**

- `ProofEngine.WeakestPrecondition.cs` — the backward **step** walk (frame / transform / refuse) over a write plan, reading `ActionMeta` write semantics and `ActionSlotRole` per step, and the `WriteSiteMeta` phase ordinal to order the plan. Multi-target actions contribute one step per target.
- `ProofEngine.Analysis.cs` — mint both kinds at their site sets, ranging over `Occasions(file)`; the establishment path absorbs and replaces `ScanRulesAgainstDefaults`, and carries the plan set for the construction occasion.
- `Actions.cs` — `GenerateIntervalContainmentObligations` deleted, and the length-containment minting path with it; both interval and length discharge survive as strategies applied to a substituted condition. The count path is **untouched**.
- Modifier desugaring — a constraint modifier produces a `ConstraintIdentity` with a generated rationale, so it flows through the same minting path as an authored `rule` (§ 2.4). `maxcount` / `mincount` are excluded from the desugaring until Decision 2's count leg closes; that exclusion is the one place the implementation must carry a spelling-dependent branch, and it carries a comment pointing at the missing validity argument rather than at this document.

**Diagnostics.**

- `PRE0167` — a constraint is not established at one of its sites.
- `PRE0168` — a governed operation can leave a constraint false (the message in § Audience and Teachability). Its span is the occasion: a transition or event row, a state action, an editable-field declaration, or the construction row.

**Tests.**

- `test/Precept.Tests/ConstraintEstablishmentTests.cs` — the defaults fold, the unfoldable-default rejection, the residency-entry site set, and the construction plan set where the initial state declares an entry action.
- `test/Precept.Tests/ConstraintPreservationTests.cs` — the three files in § The problem; the modifier and rule spellings producing identical obligations for the interval and length legs and *deliberately different* ones for the count leg; multi-write plans where an intermediate violates and the post-state does not.
- `test/Precept.Tests/PreservationOccasionRangeTests.cs` — one test per member of `Occasions(file)`: construction row, transition row, stateless event row, editable-field door; plus the negative that `Restore` mints nothing. This file is the regression barrier for the hole that unlocked the design, and its cases are enumerated from `OperationSurfaceMeta` rather than hand-listed, so a new operation cannot be added without a failing test.
- `test/Precept.Tests/WeakestPreconditionTests.cs` — the step walk over hand-computed plans, including a plan writing the same field twice, a frame-only plan, the `dequeue … into` two-step case, and the generated all-fifteen-`ActionKind` totality corpus.
- Corpus run over `samples/`, with every newly-rejected file listed and its repair recorded, and the collection-refusal cluster reported separately from the genuine proof failures.

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

### Decision 2: `IntervalContainment` and `LengthContainment` stop being minted requirement kinds and become discharge strategies under a constraint obligation; `CountContainment` is **held**

**Stakes**: high

- **Rationale**: this is implementation against locked spec rather than a fresh choice, and its scope is set by where the spec's equivalence is both *broken* and *safely closable today*.

  § 2.4 states that constraint modifiers are shorthand for rules and that `min 5` and `rule X >= 5` are *"interchangeable to the proof engine."* They are not interchangeable at all. Verified at HEAD on 2026-07-24 through the local harness, for all three containment kinds:

  | Modifier spelling | Rule spelling |
  |---|---|
  | `field Total as decimal default 0 max 1000` → `IntervalContainmentProofRequirement` `Unresolved` at the write | `rule Total <= 1000` → `HasErrors=False`, **zero obligations** |
  | `field Name as string default "ab" maxlength 3` → `LengthContainmentProofRequirement` `Unresolved` + `LengthBoundViolation` | `rule Name.length <= 3` → `HasErrors=False`, **zero obligations** |
  | `field Tags as list of string maxcount 2` / `append Tags Add.T` → `CountContainmentProofRequirement` `Unresolved` + `CountBoundViolation` | `rule Tags.count <= 2` → `HasErrors=False`, **zero obligations** |

  Three instances of one asymmetry, in the same direction: the modifier does the work, the rule is decorative. The earlier version of this decision collapsed the first row only and left the other two — which reproduced, between kinds, the very divergence it rejected its own Alternative 1 for. That is review finding 2 and it is corrected here.

  For interval and length the direction of the collapse is forced. A rule cannot mint an interval-containment or a length-containment obligation, because those payloads are a single target field with a numeric floor and ceiling (`proof-engine.md:586-594`) or a single target field with a minimum and maximum length, and a rule may be relational, multi-field, or guarded. So the equality runs the other way: both spellings mint a constraint obligation, and interval arithmetic over declared bounds — and the length analogue — become the decision procedures that discharge the single-field-literal-bound cases. The names survive where they already exist as strategies (`ProofStrategy.IntervalContainment = 7`, `LengthContainment = 8`, `src/Precept/Pipeline/ProofLedger.cs:101-110`).

  **Count is held, deliberately, and the hole is recorded rather than closed.** Collapsing count would require the collection transformer the weakest precondition needs for `append` / `enqueue` / `insert` — and matrix rev 11 records `CountContainment` as one of **four fault kinds with no validity argument at all** (`obligation-discharge-matrix-2026-07-19.md:5`). Collapsing a kind whose discharge rule has no written validity argument replaces a working narrow check with a fail-open wide one: today `maxcount 2` at least mints and rejects, and after an unargued collapse a rule-spelled count bound would mint an obligation that nothing can discharge or — worse, if the transformer were guessed — one that discharges unsoundly. So the count leg stays where it is, and the resulting divergence is **an open hole this design names**: `maxcount 2` and `rule Tags.count <= 2` remain non-interchangeable, in violation of § 2.4, until the `CountContainment` validity argument is written. It is listed in § Open questions, not buried.

  **The denominator, stated and closed.** The 2026-07-21 family-scope ruling counts eleven fault kinds — the thirteen `ProofRequirementKind` members (`src/Precept/Language/ProofRequirementKind.cs`, "The thirteen proof obligation kinds") less `Dimension` and `Modifier`, which carry no `FaultCode` and form the type-requirement family (matrix `:149`). Matrix rev 10 already moved it to **ten** by re-homing `IntervalContainment`. This decision moves it to **nine** by additionally re-homing `LengthContainment`. Eight would require the count leg, which is rejected above. The nine are `Numeric`, `Presence`, `QualifierCompatibility`, `QualifierChain`, `CountContainment`, `KeyPresence`, `IndexBounds`, `DimensionalProduct`, `AssignmentQualifier`.

  The arithmetic over the DU closes exactly, and it is worth writing out because the earlier version's did not:

  ```
  13  ProofRequirementKind members today
   −2  IntervalContainment, LengthContainment          (re-homed to strategies)
   +2  ConstraintEstablishment, ConstraintPreservation (added by this design)
  ─────
  13  members after
   =  9  fault family        (fault-code-backed)
   +  2  type-requirement family  (Dimension, Modifier — 2026-07-21 ruling)
   +  2  constraint family    (the two this design adds)
  ```

  So the obligation-family axis remains a **partition over requirement kinds**, which is what review finding 2 said it stopped being. It remains one because the fault code moves to the *strategy*, not to the constraint kind: `soundness-and-coverage.md:201-202` names `IntervalContainment` and `LengthContainment` in its "prevented by" column, and those two entries need no text change — their referent moves from the kind enum to the identically-spelled strategy enum. A `ConstraintPreservation` obligation does not itself carry a `FaultCode`; the fault is discharged by the strategy that closes it, and the fault-code registry keeps naming that strategy. Had the kind carried the code, the 2026-07-21 criterion would have pulled the constraint family into the fault family and the axis really would have collapsed — that is the reading the review correctly flagged, and it is not the one adopted.

- **Tradeoff accepted**: two members of the fault family re-home, so the fault-family denominator moves 11 → 10 → **9** in four days and every in-flight fault-family cell keyed on either kind re-anchors to a constraint obligation. That is real bookkeeping cost on work currently in flight, twice the cost the earlier version admitted, and it is the honest consequence of the spec's own equivalence. Accepted, with the second half of the cost stated plainly: because the count leg is held, § 2.4's equivalence is **still broken for one of the three containment kinds after this design ships**, and the design does not claim otherwise.
- **Alternatives considered**:
  - *Leave containment as its own kinds and additionally mint preservation for rules.* Rejected: the same ceiling written two ways would produce two different obligation kinds with different cell coordinates, different diagnostics, and different discharge contracts — which is precisely the non-interchangeability § 2.4 forbids.
  - *Generalize the containment payloads to carry arbitrary conditions.* Rejected: that is constraint preservation with a misleading name, and it would leave the catalog with members whose names describe one of the cases they handle.
  - *Select the requirement kind by the constraint's **normal form** rather than by its spelling.* This is the third option the earlier version's "only two ways" inference wrongly excluded, and it is genuinely well-grounded: § 2.4's own second bullet says "proof participation is a function of a constraint's decidability, not its syntactic form", which is exactly a normal-form criterion, and the matrix already defines a normalization (§ Vocabulary, *Normalization*) under which schema matching is done. Under it, `rule Total <= 1000` normalizes to a single-field literal bound and would mint `IntervalContainment`; `rule Floor <= Ceiling` would not, and would mint a preservation obligation. **Rejected, on two grounds, neither of which is "there is no third option".** First, the normal form is *incomplete by its own admission* — the matrix states only two rewriting steps and records "the full normal form (beyond these two rules) is an open item for the canonical doc" (§ Vocabulary, *Normalization*). Routing requirement-kind identity through an unfinished normal form makes the catalog's shape depend on an open item, and every later normal-form rule silently re-routes obligations between kinds — a soundness-correction-class change triggered by a definitional edit elsewhere. Second, it buys nothing the collapse does not: the collapse already keeps interval arithmetic as the *discharge strategy* for exactly the normalized shapes this option would route by, so the two proposals agree on which decision procedure runs and disagree only on whether that fact is recorded in the kind or in the strategy. Recording it in the strategy is where the vocabulary already exists. **Re-open trigger**: if the full normal form is ever locked and a case appears where kind identity must vary with it, this option returns as the cheaper one.
  - *Treat the spec statement as aspirational and keep the split.* Rejected: § 2.4 is a locked spec statement with no deferral marker, and the memory of this project is that impl-versus-spec divergences of this kind are cleanup, not decisions.
  - *Collapse all three, count included.* Rejected: see the count paragraph above. Under prove-or-reject an unargued discharge rule is a fail-open, and the matrix's rule-validity gate exists precisely to stop a collapse from landing ahead of its argument.
- **Precedent**: in-tree, the strategy enum already carries both `IntervalContainment` (7) and `LengthContainment` (8) as discharge mechanisms, so the vocabulary for this split already exists for both legs and the change removes duplicates rather than inventing anything. It also carries `CountContainment` (9) — which is why the held leg is a *scoping* decision rather than a structural one, and why closing it later costs nothing beyond the missing argument. Externally, Event-B has one obligation shape for an invariant regardless of whether the invariant is a simple range, a length bound, or a relation over several variables — the obligation kind is not specialized by the predicate's shape.
- **Sources consulted for this decision**:
  - `docs/language/precept-language-spec.md:1135` — "**Constraint modifiers are shorthand for rules.** A constraint modifier … desugars to the equivalent `rule` with a generated rationale."
  - `docs/language/precept-language-spec.md:1138` — "`min 5` and `rule X >= 5` are interchangeable to the proof engine."
  - `docs/compiler/proof-engine.md:586-594` — `IntervalContainmentProofRequirement(ProofSubject Subject, string TargetField, decimal? DeclaredMin, decimal? DeclaredMax, …)` — the single-field numeric payload that cannot express a relational rule; `LengthContainmentProofRequirement` is the same shape over lengths.
  - `src/Precept/Language/ProofRequirementKind.cs:4` — "The thirteen proof obligation kinds that catalog entries can declare" — the denominator.
  - `src/Precept/Pipeline/ProofLedger.cs:101-110` — `IntervalContainment = 7`, `LengthContainment = 8`, `CountContainment = 9` in the `ProofStrategy` enum.
  - `docs/compiler/soundness-and-coverage.md:201-202` — the `LengthBoundViolation` and `CountBoundViolation` rows of the per-`FaultCode` enumeration, whose "prevented by" column is what re-anchors.
  - `docs/Working/obligation-discharge-matrix-2026-07-19.md:149` — the 2026-07-21 family-scope ruling, "the fault family is the eleven fault-preventing kinds".
  - `docs/Working/obligation-discharge-matrix-2026-07-19.md:5` — rev 11's warning that four fault kinds, `CountContainment` among them, have no validity argument at all.
  - Local harness, HEAD, 2026-07-24 — the three modifier-versus-rule pairs tabulated above.
- **Strongest counter-evidence**: the 2026-07-21 family-scope ruling counted eleven fault kinds; the matrix moved that to ten on 2026-07-23; this design moves it to nine on 2026-07-24. Three denominators in four days is exactly the churn a locked ruling is meant to prevent, and a reviewer is entitled to ask whether the ruling is being eroded one kind at a time. Response, in two parts. The ruling settles *which kinds are fault kinds*, keyed on fault-code backing; it does not settle *which kinds exist*, and nothing in it is overturned — both containment faults are still prevented and still named in the `FaultCode` registry. But the churn objection is not fully answered by that, so the second part is a commitment: this design states the **terminal** scope. Interval and length re-home; count does not; no further containment kind is a candidate, because there are no others. Anyone reading the denominator after this design can rely on nine unless the count leg's validity argument lands, and that would be a single further, pre-announced move to eight.
- **Reversibility**: `Hard`. The kind names reach MCP vocabulary and the fault-family cell coordinates. Pre-release, so no external consumer is affected, but the in-flight fault-family work re-anchors twice over.
- **Blast radius**: catalogs — `ProofRequirementKind`, `ProofRequirement`, `ProofRequirements`. Docs — `docs/compiler/proof-engine.md` § ProofRequirement Catalog DU, `docs/compiler/soundness-and-coverage.md` §3.1 (two rows re-pointed from kind to strategy), `docs/language/precept-language-spec.md` § 0.6 items 6 and 11, `docs/compiler/diagnostic-system.md`. Working — the fault-family cell files keyed on `IntervalContainment` **or `LengthContainment`**, and the fault-family coordinate map. Samples — none directly. External consumers — none.

### Decision 3: A preservation obligation is keyed to the **governed operation occasion**, not to the handler; its diagnostic names the last write that makes the condition unprovable

**Stakes**: high (raised from medium — the earlier keying was the soundness hole that unlocked this document)

- **Rationale**: two things are settled and one was got wrong between them.

  Settled: obligations run over the **whole write plan**, never per individual write (matrix `:53`, owner ruling 2026-07-20). Settled: a constraint's obligations attach to *every write site of every field the constraint mentions* (matrix `:63`, `:228` — symmetric attachment). The earlier version of this decision honoured the first and violated the second, by taking "per handler" from the write-plan ruling's prose as if it were the quantifier rather than the granularity. It is the granularity. The unit must be one-obligation-per-operation — that is what keeps intermediates unobserved — and the *range* must be every occasion at which a mentioned field can be written, which is strictly larger than the handler set. A state entry action is not a handler; an editable-field door is not a handler; construction is not a handler. Verified at HEAD, the compiler reports `eventHandlers: []` for the `to Closed -> set Total = 5000` file while still rejecting it, which is the shipped engine telling us the same thing.

  The replacement quantifier is `O ∈ Occasions(file)` with `Owes(C, O)`, both defined once in § The write-site surface, 4 and read identically by the linkage design's `Expected(C)`. Nothing about the whole-write-plan ruling changes: one occasion, one plan, one obligation, intermediates unobserved.

  **The construction relocation, handled rather than inherited.** Re-keying preservation from handlers to occasions has an adversarially-found failure mode: it can move the hole rather than close it. A state entry action on the **initial** state writes during construction. If construction were treated as establishment-only, and preservation ranged over post-construction occasions only, that write would be covered by neither and the hole would reappear one occasion to the left. This design does not let that happen, and it does it **inside** the `:2202` default rather than by settling `:2202`:

  - Construction is an occasion like any other. It is `Governed` (`:2076`, `:2069`), so it owes preservation as well as establishment, and every constraint mentioning a field the construction plan writes is proved over that plan.
  - Whether W3 — the entry action — is *in* the construction plan is exactly what `:2202` leaves unsettled: *"Until these are ruled, no analysis may assume an ordering among several state actions on one state, nor that entry actions run at construction."* The design therefore takes the construction plan to be a **plan set** and requires the obligation to discharge over **every** member (§ The write-site surface, 6; § Semantic Rules). A write that is unsafe under the entry-actions-run reading is refused, and a write that is unsafe under the entry-actions-do-not-run reading is refused.
  - The conservative treatment is contingent on `:2202` and says so. When the owner rules, the non-licensed member is deleted; nothing is re-derived and no previously-accepted file stops compiling. Until then, no analysis in this design assumes entry actions run at construction, and none assumes they do not.

  Verified at HEAD on 2026-07-24: `field Total as decimal default 0 max 1000` with `state Open initial` and `to Open -> set Total = 5000` **rejects** (`NumericOverflow`, `IntervalContainment` `Unresolved`), with no event handler in the file at all. Today's engine already refuses this case, so the conservative reading is also the non-regressing one — the design must preserve that refusal, and the plan set is how it does.

  Separately, and unchanged: the matrix explicitly flagged the *reporting* question as not definitional — *"how a failed multi-write proof reports — whether the diagnostic can point at an individual write inside the plan, or names the handler. That is diagnostic quality, decided when the diagnostic is built."* This design builds it, so it decides it. The backward step walk already produces an intermediate condition after each step, so the walk knows the first step (working backwards) at which the condition stopped being entailed by the premises. Reporting that write costs nothing and is what the author needs — naming only the occasion leaves them to find which of five lines broke it.

- **Tradeoff accepted**: three, and the first is the largest cost in this document.

  *Obligation volume.* Ranging over occasions rather than handlers multiplies the obligation count by the number of construction rows, state-action-bearing rows and editable-field doors in the file. Falsifier 2's compile-time threshold is where that stops being acceptable.

  *Refusals from the plan set.* A file whose safety depends on which `:2202` reading is true does not compile, even though one of the two readings is the truth and the file may be safe under it. That is refusal in the direction `:2202` names, and it is the direction prove-or-reject requires; the alternative is a compiler whose acceptance set depends on an unruled question.

  *Anchor imprecision.* The reported write is the one where the *proof* fails, which is not always the one the author would consider at fault — a later write can make an earlier one's contribution unprovable. The message therefore names the write and prints the condition, rather than asserting the write is wrong.

- **Alternatives considered**:
  - *Keep the handler keying.* Rejected: it is the soundness hole review finding 1 verified, with a compiled witness the design would have made compile clean.
  - *Range over occasions but treat construction as establishment-only.* Rejected: this is the relocation above. It closes the handler hole and opens an initial-state entry-action hole, which is not an improvement — it is the same fail-open moved somewhere less likely to be tested.
  - *Range over occasions and settle `:2202` in the permissive direction (entry actions do not run at construction), so the construction plan is a singleton.* Rejected twice over: the sentence at `:2202` forbids it in terms, and it is the direction that cannot be walked back — if entry actions do run, every file accepted under the assumption is retroactively unsound, a soundness correction rather than a widening (matrix `:490`).
  - *Go per-write rather than per-occasion, so that no write can hide inside a plan.* Rejected: it contradicts the whole-write-plan ruling and reintroduces the `DoubleWrite` over-rejection in § The problem. Coverage is a question about the *range*; granularity is a separate question, already ruled.
  - *Name only the occasion in the diagnostic.* Rejected: correct but unhelpful, and the author has to bisect.
  - *Name every write that touches a mentioned field.* Rejected: on a long plan this is noise, and it implies all of them are at fault.
- **Precedent**: in-tree, the matrix's write-site-category axis (`:172–180`) already enumerates five categories, of which the handler `set` is one — so the language's own definition surface had the broader range before this design narrowed it, and this restores rather than invents. The spec already commits to printing the weakest precondition on an unresolved verdict — *"the author resolves an unresolved verdict by supplying the constraint its printed weakest precondition names"* — so printing the condition is settled and only its anchor is being chosen. Externally, Event-B generates a preservation obligation per (invariant, event) pair where "event" is every state-changing operation the model declares, including the ones with no parameters; the quantifier is over the model's operations, not over a syntactic subset of them.
- **Sources consulted for this decision**:
  - `docs/Working/obligation-discharge-matrix-2026-07-19.md:53` — the write-plan ruling: "Obligations are defined over the **whole write plan** and never per individual write"; and "One thing the ruling does not settle, and which is not a definitional question: how a failed multi-write proof reports … That is diagnostic quality, decided when the diagnostic is built."
  - `docs/Working/obligation-discharge-matrix-2026-07-19.md:63` — the rule/Invariant row: "established at construction; preserved by every write site of every mentioned field".
  - `docs/Working/obligation-discharge-matrix-2026-07-19.md:228` — symmetric attachment, "a constraint's obligations attach to every write site of every field the constraint mentions".
  - `docs/Working/obligation-discharge-matrix-2026-07-19.md:172–180` — the five-value write-site-category axis.
  - `docs/language/precept-language-spec.md:2202` — "Until these are ruled, no analysis may assume an ordering among several state actions on one state, nor that entry actions run at construction."
  - `docs/language/precept-language-spec.md:2076`, `:2069` — construction runs the standard pipeline, "same guards, same mutations, same ensures, same constraint checking as any other event"; "governed from birth".
  - `docs/language/precept-language-spec.md:229` — "The author resolves an unresolved verdict by supplying the constraint its printed weakest precondition names."
  - Local harness, HEAD, 2026-07-24 — the entry-action-on-the-initial-state witness above.
- **Reversibility**: `Medium`. The range is a property of the minting walk and the linkage design's expected-set walk, which must move together; the plan set collapses to a singleton on an owner ruling with no re-derivation.
- **Blast radius**: pipeline — the minting walk and the weakest-precondition entry point. Docs — `docs/compiler/proof-engine.md` § Obligation Generation Contract, `docs/compiler/diagnostic-system.md`. Working — the linkage design's `Expected(C)`, which must range identically. Samples — every file with state actions or editable fields under a constraint; enumerated by the corpus run.

### Decision 4: For a residency ensure, the pre-state premise class is limited to unconditional constraints until the residency-fact question is ruled

**Stakes**: medium

- **Rationale**: proving that a governed occasion preserves `in S ensure C` may want other facts that are true only while resident in `S`. Whether the entity's residency in `S` is itself available as a premise is reserved to the owner by the matrix and is not settled here. Rather than depend on the answer, this design takes the position that costs nothing to reverse: a preservation proof for any constraint may use unconditional constraints from the pre-state, and may not use state-scoped ones. That is sound under either ruling — it consumes a strict subset of what the permissive answer would allow — and it makes the rest of the design independent of the question.
- **Tradeoff accepted**: some safe programs are rejected that the permissive ruling would accept, specifically those where one residency ensure's preservation needs another residency ensure in the same state. Those land in the sound-but-unprovable band and the author's respelling is to declare the needed fact as an unconditional rule. If the owner rules the residency fact admissible, admitting it is a power-widening — the cheap amendment class, monotone, minor version, no certificate replay break.
- **Alternatives considered**:
  - *Assume the residency fact is available.* Rejected: it is the owner's question, reserved in the matrix, and assuming the permissive answer is the direction that cannot be walked back without a soundness correction.
  - *Refuse residency-ensure preservation entirely until ruled.* Rejected as far more restrictive than the uncertainty warrants — the obligation is well-defined without the extra premise.
- **Precedent**: the matrix's own amendment protocol makes the conservative-then-widen path the cheap direction, and the sound-but-unprovable band is the standing inventory of candidates for exactly this kind of later widening.
- **Sources consulted for this decision**:
  - `docs/Working/obligation-discharge-matrix-2026-07-19.md:203` — "**Open**: whether the residency fact — the entity is in `S` — is itself available as a premise, and under which class, is not ruled … the question is the owner's and is recorded here rather than assumed"
  - `docs/Working/obligation-discharge-matrix-2026-07-19.md:490` — "**Power-widening** … Monotone: everything previously licensed stays licensed. Minor definition version; previously issued certificates remain valid."
  - `docs/Working/ensure-vs-rule-analysis-2026-07-20.md` § Verdict — the rewrite of a residency ensure as a conditional rule over a state coordinate is inexpressible, verified by live compile, so the question cannot be dissolved by restating the construct.

### Decision 5: Two diagnostic codes, one per obligation kind

**Stakes**: low

- **Rationale**: the two failures have different repairs. An establishment failure is fixed at the default or at the initial event; a preservation failure is fixed at the writing occasion. One code carrying both would have to name both repairs on every occurrence, and the author would have to work out which half applied to them.
- **Tradeoff accepted**: two more entries in a catalog that is already large, and a reader scanning the diagnostic list sees two codes where the underlying idea is one.
- **Alternatives considered**:
  - *One code for both, with the repair chosen at message-render time.* Rejected: the diagnostic-system convention in this codebase is one code per repair class, and a code whose message forks on a payload discriminator is harder to search for and harder to document than two codes.
  - *One code per (obligation kind × constraint kind)* — establishment-of-a-rule, establishment-of-a-residency-ensure, and so on. Rejected as over-splitting: the repair is the same within each obligation kind regardless of which of the five constraint forms raised it, so the extra codes would carry no extra information.
- **Precedent**: in-tree, the diagnostic catalog already pairs a code with a repair class rather than with an internal kind — `PRE0078` covers bound containment across every containment shape, and the qualifier codes split by what the author must change, not by which requirement kind minted them. Externally, Rodin surfaces the obligation *name* (`INITIALISATION/inv2/INV` vs `register/inv2/INV`) rather than a single "obligation failed" code, which is the same split by establishment-versus-preservation.
- **Coupling to the linkage design, flagged not resolved**: the linkage design carries its own completeness diagnostic, and on a file where a constraint is minted at no occasion at all both that code and `PRE0168` could fire, or the linkage code becomes a dead duplicate. Base-minimality — one diagnostic for the target obligation — is the matrix's standard, so the two designs must agree on which code owns which failure before either locks. This design's position: `PRE0167`/`PRE0168` own *a specific obligation failing to discharge*, and the linkage code owns *an obligation that should exist not existing*. Those are different conditions and both are reachable, but the boundary needs stating in one place rather than two, and the linkage design is the natural owner. Recorded here so the coupling is visible from this side.

## The cost

This is a **soundness correction** under the matrix's amendment protocol, not a widening: it shrinks the set of files that compile. Major definition version, certificate replay breaks for affected cells, routed to the owner with witness programs. Three witnesses are in § The problem, compiled at HEAD on 2026-07-23 and 2026-07-24.

The shrink is larger after this rework than the earlier version claimed, in three separate places, and each is named rather than folded into one estimate:

- **Range.** Preservation now covers construction rows, state actions and editable-field doors as well as handlers (Decision 3). Every constraint over a field written at one of those sites newly acquires an obligation. § The write-site surface, 8 measures how much of that surface mints nothing today: five of the eight writer categories, plus the rule spelling of two of the three that do.
- **Length.** The length leg of Decision 2 adds `rule X.length <= N` to the set of spellings that now reject where they previously compiled clean.
- **Refusal on collection plans.** With the collection transformers held open, a constraint mentioning a collection field over a plan containing a collection write is `Unresolved` and rejects (§ Semantic Rules). This is the single largest unmeasured component, and it is refusal by construction rather than a proof failure — it will be visible in the corpus run as a cluster.

Against those, one relaxation in the widening direction: the `DoubleWrite` shape compiles where it is rejected today, because the check moves from per-write to per-occasion. That is a power-widening the matrix already ruled and this design implements.

The size of the shrink is not guessed here. The corpus run in § Acceptance criteria measures it, and every newly-rejected sample is listed with its repair before the change lands.

## Acceptance criteria

**Spelling equivalence (Decision 2).**

1. `rule Other <= 1000` with `set Other = Bump.N` (unconstrained argument) is rejected with `PRE0168`, and the message quotes the rule's `because` text and names the line and the occasion.
2. The same file with `field Other as decimal max 1000` instead of the rule is rejected identically — same obligation count, same verdict, same repair suggested. A test asserts the two spellings produce equal obligation sets.
3. **Length parity.** `field Name as string default "ab" maxlength 3` with `set Name = Rename.N` and `field Name as string default "ab"` + `rule Name.length <= 3` produce equal obligation sets. Today's before-picture, asserted against: the modifier spelling mints two `LengthContainmentProofRequirement`s (one `Unresolved` at the write, one `Proved` on the default, strategy `LengthContainment`) and the rule spelling mints **zero**, `HasErrors=False`.
4. **Count non-parity, asserted as held.** `field Tags as list of string maxcount 2` continues to mint a `CountContainmentProofRequirement` and `rule Tags.count <= 2` continues to mint nothing. This is a test of the *held* leg: it asserts the divergence is present and deliberate, so that closing the count leg later is a visible test change rather than a silent one. The test carries a comment naming the missing `CountContainment` validity argument as the reason.

**Range — the occasions preservation must cover (Decision 3, E1).**

5. `rule Total <= 1000` with `to Closed -> set Total = 5000` and **no event handler in the file** is rejected with `PRE0168`. This is review finding 1's witness in its rule spelling; the modifier spelling of it rejects at HEAD today, and a test asserts both spellings reject with the same obligation count.
6. A rule over a field marked `editable` in some state, with no handler writing it, produces a preservation obligation at the editable-field door and is rejected when the door's ingress evaluation does not discharge it. Today's before-picture: `field Total as decimal default 0 editable` + `rule Total <= 1000` → `HasErrors=False`, zero obligations.
7. A rule over a field written only by a state **exit** action produces a preservation obligation on the row whose plan contains that exit action.
8. A rule mentioning a field only through a computed field's inputs produces a preservation obligation on the occasion writing those inputs — the transitive leg of the mention set — and **exactly one**, not one per route, since leg (c) and the computed slot's own recomputation name the same occasion.
9. A rule mentioning a field only in its activation guard produces a preservation obligation on the occasion writing that field.
10. `Restore` produces **no** preservation obligation for any constraint in any file. Asserted directly, because it is the criterion that keeps the linkage design's completeness check from firing on every file that declares a constraint.
11. No obligation is minted for an occasion whose write plan touches no field the constraint mentions — the surplus direction, asserted so the completeness check's set equality can hold.

**Construction (Decision 1, and E1's relocation guard).**

12. A definition whose field default is non-constant, under a rule mentioning that field, is rejected with `PRE0167` — the change from report to obligation semantics.
13. A definition whose defaults provably violate a rule is still rejected, with the same witness it produces today — the change does not lose the case that already worked.
14. `field Total as decimal default 0 max 1000` with `state Open initial` and `to Open -> set Total = 5000` is rejected, in **both** spellings of the bound. Today the modifier spelling rejects (`NumericOverflow`, `IntervalContainment` `Unresolved`, harness 2026-07-24) and the rule spelling does not; after this change both do. This is the acceptance criterion that would have caught the construction relocation.
15. Where the initial state declares an entry action, the construction obligation's proof condition is a conjunction over the plan set, and a file provable under one member but not the other is **rejected**. A companion test asserts that a file provable under *both* members compiles, so the plan set is not silently rejecting everything.

**The weakest precondition (E3).**

16. The `DoubleWrite` file in § The problem compiles, with exactly **one** preservation obligation for the occasion rather than one per write. Today's output — two obligations, the first `Unresolved` at `[5000 .. 5000]`, the second `Proved` at `[0 .. 100]`, file rejected — is the before-picture the test asserts against.
17. The step walk over a hand-computed three-step plan produces the expected condition, asserted literally rather than by round-trip.
18. **Frame.** A plan whose only writes target fields the constraint does not mention leaves the residual condition syntactically unchanged, asserted on the residual itself rather than on the verdict.
19. **Two-target totality.** `dequeue Q into D` under a constraint mentioning `D` produces a step targeting `D`, and the obligation does **not** discharge by framing. Asserted as an obligation-level test, because the failure mode is silent: today the compiler does not treat the `into` slot as a write site at all (it warns `FieldNeverSet` on `D`).
20. **Refusal, not silence.** A constraint mentioning a collection field, over a plan containing `append` / `enqueue` / `insert` against that field, yields `Unresolved` and rejects — asserted directly, so that authoring a collection transformer later is a visible change of verdict.
21. `WP` returns a value for every plan in a generated corpus covering all fifteen `ActionKind` members in both single- and multi-step plans; no plan produces an exception or a missing verdict. This is the totality claim, tested as totality rather than assumed from the prose.

**Corpus and tooling.**

22. Every `samples/` file either compiles or appears in a committed list with the constraint it needs and the repair applied.
23. Hover over a rule reports its establishment status and its preservation status per governed operation occasion.

## Dependencies

**Upstream.**

- The linkage and completeness design — supplies the coverage records these obligations populate and the citation the pre-state premise class requires. **It also shares § The write-site surface verbatim**, and this design's minting range is only correct if that section is identical in both documents: `Minted(C)` here and `Expected(C)` there must be two derivations from one declaration. A divergence between the two copies is a defect in both.
- The § 3A.4 writer enumeration, for the write plan the step walk orders. Complete as a category list; asserted rather than build-kept (`:1992`), and its consumption is incomplete, per BUG-033.
- `precept-language-spec.md:2202` — not a blocker, but the plan set exists because of it, and the plan set is where the design pays for the question being open.
- The `CountContainment` validity argument — not a blocker for anything in scope, but the prerequisite for closing Decision 2's held leg.
- The matrix's guard-match argument and transport rule, inherited unchanged by any preservation proof consuming a guard fact.

**Downstream.**

- The fault-family re-ratification: the pre-state premise class becomes usable with a citation, which is what the fault cells needed.
- BUG-017, BUG-018, BUG-019 — the recorded breaches where a rule is consumed without being enforced.
- The fault-family denominator — **nine** after this design, from eleven at the 2026-07-21 ruling and ten at matrix rev 10 — and every cell keyed on `IntervalContainment` **or `LengthContainment`**, which re-anchor per Decision 2.
- The linkage design's `WriteSiteCategories` and `MentionSetLegs` catalog entries, which are the same declaration § The write-site surface specifies here.

## Doc-update enumeration

- `docs/language/precept-language-spec.md` § 0.6 items 6 and 11 and the implementation-status table — the obligation semantics and the kind collapse; § 2.4 — a pointer noting the equivalence is now enforced for the interval and length spellings and **explicitly not yet for the count spelling**, with the missing validity argument named; § 3A.4 — a pointer from the writer table to the `WriteSiteMeta` catalog that now declares it, replacing "this table is asserted rather than kept" when the catalog lands.
- `docs/compiler/proof-engine.md` § ProofRequirement Catalog DU — the two added subtypes and the two removed ones; § Obligation Generation Contract (the range is occasions, not handlers); § Contracts and Guarantees § Obligation Completeness.
- `docs/compiler/soundness-and-coverage.md` §3.1 — the `OutOfRange` / `NumericOverflow` / `LengthBoundViolation` rows re-point their "prevented by" column from the requirement kind to the identically-named discharge strategy; the `CountBoundViolation` row is unchanged.
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
2. If compile time for the median sample more than doubles, the per-occasion step walk is too expensive as specified and the walk needs memoizing per constraint rather than recomputing per occasion. (Baseline: the full corpus compiles in about 44ms. Note the range widened at this rework: the occasion count per file is larger than the handler count, so this falsifier is more likely to trip than it was, and it is the one to watch first.)
3. If more than two samples need a rule restated as an unconditional constraint purely to work around Decision 4's conservative premise set, the residency-fact question is load-bearing in practice and should be put to the owner as urgent rather than as a later widening.
4. If a domain expert reads `PRE0168` and repairs the wrong line more often than the right one, Decision 3's anchor is wrong and the message should name the occasion and list every candidate write instead.
5. If a preservation proof is found that discharges through the pre-state premise class without a coverage citation, the linkage design's admissibility rule is not actually enforced on this path and both designs need re-checking together.
6. If `Minted(C)` and the linkage design's `Expected(C)` disagree on any corpus file after both designs land, then § The write-site surface is not in fact being read by both walks — the two documents have drifted, or one walk has re-derived what it should have read. That is the coupling this rework created and it is the first thing to check when the completeness diagnostic fires unexpectedly.
7. If the collection transformers, once authored, turn out to need a premise the four classes cannot supply, then Decision 2's held count leg is not merely unfinished but blocked, and the count divergence becomes a surface question rather than a proof-power one.

## Open questions

Two, both created or made explicit by this rework. Neither blocks the rest of the design.

1. **Decision 2's count leg — a named, open § 2.4 violation.** `field Tags as list of string maxcount 2` and `rule Tags.count <= 2` are not interchangeable and will still not be interchangeable after this design ships: the modifier mints and rejects, the rule mints nothing (both verified at HEAD, 2026-07-24). Closing it requires the `CountContainment` validity argument that matrix rev 11 records as absent (`:5`), and the collection post-state transformer the weakest precondition needs. Held rather than closed because an unargued collapse is a fail-open, and acceptance criterion 4 asserts the divergence so it cannot close silently. **Not the owner's question** — it is unfinished work with a named prerequisite, and it belongs to whoever writes the fault-family arguments.
2. **The collection transformer library.** Fourteen of fifteen action kinds have no authored post-state transformer, so every plan containing a collection write against a mentioned collection field is `Unresolved` and rejects. The weakest precondition is total over this — rule 3 gives it a verdict — but the verdict is refusal, and the size of what that refuses is not measured here. The corpus run (acceptance criterion 22) measures it.

The residency-fact question is genuinely open and genuinely the owner's, but this design does not depend on it — Decision 4 takes the position that is sound under either ruling and names the widening path. It is recorded in § Scope as out of scope rather than as an open question, because nothing here waits on it.

`precept-language-spec.md:2202` — entry actions at construction, and the order among several state actions on one state — is also genuinely open and genuinely the owner's, and this design does not settle it and does not depend on the answer. The plan set (§ The write-site surface, 6; Decision 3) makes the design correct under either ruling and makes the ruling a deletion when it lands. Same for the `omit`-reset canon conflict (matrix `:294`, `:459`): W6's targets are declared and usable; its post-state value is not usable as a premise, and no rule here assumes one.
