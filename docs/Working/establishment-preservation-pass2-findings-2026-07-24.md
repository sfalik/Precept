---
status: Draft findings — 2026-07-24. **Not a design and not lockable.** This records pass-2 fix proposals for the two establishment/preservation designs and the two adversarial rounds run against them. Every proposal here is either amended, refuted, or carries named residual risk; none is ready to be written into a design doc as-is.
scope: companion record for `constraint-establishment-preservation-obligations-2026-07-23.md` (E/P) and `obligation-linkage-and-completeness-2026-07-23.md` (LINK), both UNLOCKED/Draft
method: two adversarial rounds, six independent reviewers, all findings compiled at HEAD via a direct `Compiler.Compile` harness (the precept MCP server was down throughout with the NodaTime load flake — `precept_ping` succeeds while `precept_compile` fails, so ping is not a health check for the compile path)
---

# Establishment/preservation — pass 2 findings

## Why this document exists

Pass 1 (commit `91d7a807`) reworked both designs and was adversarially reviewed the same day; that
review left five defect classes open, which is what keeps both documents Draft. This pass proposed
fixes for those five, then ran two adversarial rounds against the proposals. It did **not** produce
a lockable design — it produced a much better-characterised defect list, four filed compiler bugs,
and a measured cost picture that replaces several guesses in both documents.

The standing discipline is why this is a separate record rather than an edit to the designs: a
soundness argument written in one pass has never survived in this project, and several proposals
below are still on their first or second round. Writing them into a design doc would repeat exactly
the failure that unlocked both documents.

## Proposal state after two rounds

| Proposal | Round 1 | Round 2 | State |
|---|---|---|---|
| **F1** obligations indexed by applicability edges | survives, 6 amendments | **NOT READY** — 3 further refutations | core holds; occasion domain has fail-open holes |
| **F2** construction is establishment-only | survives (proof recorded) | **survives**, needs a third contingency | closest to ready |
| **F3** weakest precondition walks firings, simultaneous multi-target | survives, 4 amendments | survives as a pair with W5, 7 amendments | core holds |
| **F4** transformer dispatched on writer category | **fails** | rows redesigned; W5 leg survives | W7 withdrawn, W5 replaced |
| **F5** count leg restated | 2 of 3 sub-fixes wrong | — | retracted; new rationale needed |
| **F6** citation corrections | incomplete | — | extended below |
| **F7/F8** shared section + linkage corrections | confirmed | — | ready to apply |

## F1 — obligations indexed by applicability edges

**The proposal.** The five constraint forms differ only in *when the constraint applies*. Index both
obligations by two predicates over an occasion `O` with pre-state `σ⁻` and post-state `σ⁺`:

```
AppliesAfter(C,O)    Invariant → true;  StateResident(S) → post-state(O) = S;
                     StateEntry(S) → O enters S;  StateExit(S) → O leaves S;
                     EventPrecondition → false
AppliesBefore(C,O)   Invariant → O has a prior committed configuration;
                     StateResident(S) → O has a pre-state and it is S;
                     edge-triggered kinds → false

Establish(C,O)  iff  AppliesAfter ∧ ¬AppliesBefore          (no mention filter)
Preserve(C,O)   iff  AppliesBefore ∧ AppliesAfter ∧ O.Governed
                     ∧ Targets(O) ∩ mentions(C) ≠ ∅
```

This reproduces matrix `:63`–`:67` and `:203`–`:206` at form level, supplies the transition-moment
obligation both documents currently leave undefined (it *is* establishment at the entering/leaving
row), and makes construction establishment-only without a second diagnostic code.

**Why it is worth pursuing.** The kind-blind quantifier it replaces is not a theoretical over-
rejection — two shipped samples are rejected by it at occasions where the constraint does not apply:
`crosswalk-signal.precept` (mints preservation of `in Walk ensure CountdownSeconds > 0` on a row
*leaving* `Walk` that sets the field to 0) and `restaurant-waitlist.precept` (mints it on a row whose
exit hook clears the field). Under F1 nothing is minted at either.

**Amendments carried from round 1** (all accepted, none yet re-reviewed as a set):

- **A1 — occasions are route-expanded** to (concrete source state, row). A `from any` row has no
  single pre-state at row granularity, so the residency predicate is undefined and, read as false,
  fails open on a row that can break a residency ensure. 5 live `from any` rows in the corpus; the
  spec recommends the idiom (`:1777`); neither design doc mentions `from any` anywhere. Round 2
  confirmed this is not merely allowed but **forced** — phase-3 exit actions depend on the concrete
  source state, so an unexpanded occasion has no well-defined write plan at all — and that it does
  not disturb the whole-write-plan ruling (matrix `:53` fixes granularity, `:240` fixes identity
  per route; expansion never splits a plan).
- **A2 — "enters/leaves S" is a three-case predicate**: construction enters the initial state
  (`precept-language-spec.md:2089`); `no transition` rows never enter or leave (`:1074`);
  self-transition is canon-unpinned for *ensures* and must be pinned in the refusing direction.
- **A3 — the Restore exclusion is the enumeration**, not the predicate. `Restore ∉ Occasions(file)`
  (E/P `:337`). The earlier justification ("it has a pre-state / changes no state") is withdrawn: it
  is not expressible in F1's own predicates, and under the natural no-pre-state reading it would
  mint establishment for every residency ensure — 64 of 78 files refusing, against `:272`.
- **A4 — plan-set hygiene**: `Targets(O)` is the union over plan-set members.
- **A5 — split the two senses of "pre-state"**: a prior committed configuration (for the invariant
  arm) versus a lifecycle coordinate (for the state-anchored arms). Read uniformly, a stateless
  event occasion mints establishment with no mention filter — wrong kind, wrong quantifier.
- **A6 — no third obligation kind**; render the folded transition-moment obligations in the matrix's
  own vocabulary via `ConstraintKind`-keyed message templates. Matrix `:203`/`:204` use identical
  proposition text for residency establishment and the transition-moment obligation, so a third kind
  would be a catalog member with no distinct shape. **Consequence**: E/P Decision 5's repair-class
  rationale (`:853`) becomes false once exit obligations are establishment — re-author that leg.
- **A7 — the minting intersection is over stored-expanded mentions ∩ stored targets**, not raw
  targets. Otherwise every constraint mentioning a computed field mints at every governed occasion.

**Round-2 refutations — these are what keep F1 out of the design docs:**

1. **The occasion domain has two fail-open holes.** (a) `Occasions(file)` enumerates "one occasion per
   construction row" — a file with **no initial event** has no construction row, so establishment is
   minted nowhere and premise class (d) is void file-wide. Legal per `:2079`, and the matrix's own
   Witness Family 1 fixture is this shape. 0 of 78 samples today. (b) The editable-door occasion is
   "(state, editable field) pairs" — ill-typed for a **stateless** precept, so a stateless editable
   field under a bound mints nothing. Compiled clean at HEAD. Stateless precepts are first-class.
2. **Self-transition machinery is undefined exactly where it decides a shipped sample.** Plan-set
   membership for a self-loop is ambiguous between a paired 2-member set and a 4-member (exit ×
   entry) set, and the choice is verdict-bearing. `library-hold-request.precept` — clean at HEAD —
   **rejects under every admissible configuration**, and appears in no cost table in either document.
   Separately, A1 makes "pre-state = S" a per-occasion structural fact, which either licenses
   state-scoped pre-state ensures as premises — settling by the back door the residency-fact question
   matrix `:203` reserves to the owner — or does not, leaving the self-loop cases unprovable. F1
   carries neither consequence.
3. **A7 rests on a computed-purity premise HEAD does not enforce.** `field ObservedAt as instant <- now()`
   compiles clean (verified). Its stored expansion is empty, so no occasion ever mints preservation
   for a constraint mentioning it, while recomputation re-derives it every operation. Filed as
   **BUG-066**; F1 needs an explicit purity side condition regardless.

**Also owed**: F1 narrows the minted set at `Update` doors below matrix `:208`'s letter ("every
constraint mentioning the field") with no recorded matrix-correction entry — the same divergence
class A2 records for `:64`/`:203`.

## F2 — construction is establishment-only

**Survives both rounds.** Recorded proof: `σ⁻ ⊨ WP(C,P) ⟺ σ⁺^P ⊨ C` is the same proposition; at
construction premise class (d) is empty, so the two obligations differ only in mechanism, and
establishment's filters are weaker over the same plan set. Round 2 confirmed the equivalence holds
per plan-set member, through refusing walks, and over identical construction premise sets; no wrong
acceptance was constructible.

**Third contingency, added in round 2 and verified at HEAD.** F2 is sound only if establishment ships
at **full-plan strength**. Today `field Total as decimal default 0 max 1000` with an initial-event
`set Total = Create.Amount` (unbounded) is rejected — but *only* by the per-write containment path
that Decision 2 deletes. If establishment ships as the defaults-only fold rather than the promised
full-plan post-state proof (E/P `:688`), F2 turns that file into a silent unsound accept.

## F3 × F4-W5 — the weakest precondition

**F3**: the walk's unit is the **firing**, not the (firing, target) pair; a firing substitutes all its
targets simultaneously against the pre-firing state. This dissolves the `dequeue Q into D` ordering
hole rather than pinning it. Grounded positively in the action definitions (`collection-types.md:197`,
`:261`, spec `:1708`) — the "front element" is the front of the *pre-firing* collection by definition
of removal. Round-1 corrections: `into` is **optional** (`Actions.cs:504`, `:548`), so "three kinds
write two targets" is wrong in both docs — they are three kinds that *may*; and the refusal rule is
refuse-if-**any** free target lacks a transformer.

**F4-W5**: phase 7 is not a determinate firing sequence — canon pins neither its order nor its count —
so the walk treats it as **one composite boundary substitution**: every mentioned computed field is
replaced by its closure expansion over stored fields, in reverse topological order, at the 7/8
boundary. Round 2 verified the algebra composes correctly with the firing walk and that expansion
terminates (cycles are Errors), and measured the corpus: max chain depth 4, largest expansion 25
nodes (8.3× ratio, `invoice-line-item`).

**Round-2 amendments, four of them soundness-bearing:**

- Pin the composite to exactly one application on the phase-8 residual; define computed names
  reintroduced *below* the boundary as slot reads that frame through phases 3–6. Expanding on
  introduction is provably unsound (worked counterexample).
- Refuse any residual computed-field mention at a **construction** occasion — no pre-state exists and
  the slot is hollow at phase 5 (`:2075`). Such reads compile at HEAD.
- Correct the **single-writer premise** wherever it appears, including matrix `:355`, which cites a
  `set`-only check as if it closed the write surface. Filed as **BUG-064**.
- Add a **transitive-omit rule**. On a preservation occasion while resident there is no `omit` firing
  in the plan, so everything frames and the walk can `Prove` a condition over a structurally absent
  field via its declared modifiers. The existing guard (E/P `:392`) does not reach this because the
  proof never consults the reset's post-state value.

Scoping amendments: the "zero live instances" claim must be scoped to *catalog-action* firings — the
W5 composite is itself a simultaneous multi-target substitution, and the `omit` reset is a genuine
multi-target firing whose intra-firing order becomes a live fork the moment Q13 is ruled.

## F4-W7 — withdrawn

The fresh-symbol treatment of the editable door contradicts an owner ruling (matrix `:208`, `:442`,
`:43`), contradicts E/P's own § Semantic Rules sentence at `:585`, and flips `tax-rate-configuration.precept`
— clean at HEAD — to rejected, with **35 of 78** samples carrying an editable field mentioned by a
constraint. The door is **not walked**; its obligation discharges by the set-equality coverage
argument the matrix already ruled. Newly surfaced and still owed: `Update` can patch **several**
fields in one operation (`Version.cs:84`, `:91`) while the occasion model enumerates one occasion per
(state, editable field) pair — the joint-patch occasion appears in no walk.

## The claim that failed outright

Proposed: a computed field's defining equation `K = expr(fields)` is **type-structural context**, not
a premise, so it may be used freely without extending the ruled four-class vocabulary. **Refuted on
three independent grounds.**

1. The 2026-07-21 test is "can some operation **establish or** invalidate it". The proposal argued
   only the invalidate half — while the very line cited as proof of unfalsifiability (`:1985`,
   recomputation runs in every operation) is an operation that *establishes* it. The ruling's
   rationale, its tradeoff clause ("tightly enough that a genuinely establishable fact cannot claim
   the exemption"), its Event-B precedent leg, and its only mechanization (`:359` — `provdeps = ∅ ⟺`
   context) all place the equation on the value-fact side. The matrix's own transport rule already
   calls it **earned** at the phase-7/phase-8 boundary (`:355`).
2. "Context" does not mean cite-free — `:133` says such a fact "discharges the duty **by citing its
   declaration**" — and it answers the *citation duty*, not *availability*. The premise classes pin
   the accepted set exactly (`:47`), and the equation is in none of them.
3. Compiled counterexample: a plan that writes a computed field's input and then reads the computed
   slot sees the **stale** value (recomputation is phase 7), so the equation is false in a readable
   mid-plan window. A cite-free "use anywhere" licence is unsound there.

Measured payoff: **1 of 8** computed-mentioning sample files clears, not "mostly". The residual
blockers are guard-complement facts (5 of 8), cross-operation premise conservatism (3), deliberate
sweep-reliance (2), and min-algebra (1).

**What the design must do instead**: route the equation as earned-fact machinery — the transport
rule's value-availability bridge intra-operation, plus a written validity argument for the
cross-operation leg (every committed configuration satisfies the equation; induction over operations,
with recomputation-under-`Restore` as the base case). That argument does not exist and needs
adversarial rounds.

## Corrections to both documents (verified at HEAD)

- **E/P `:858`** — "PRE0078 covers bound containment across every containment shape" is **false**;
  `PRE0078` is `NumericOverflow` only, with `PRE0079`/`PRE0135`/`PRE0136` splitting by shape
  (`diagnostic-system.md:163`). This is Decision 5's *precedent* leg and it argues the opposite of
  what it claims — re-author, do not patch.
- **LINK `:526`** — "the DU carries twelve sealed subtypes, `Modifier` has no dedicated subtype, so
  `proof-engine.md` is drift" is **false**. There are 13 subtypes; `ModifierRequirement`
  (`ProofRequirement.cs:157`) is the thirteenth, breaking only the naming convention.
  `proof-engine.md:531` is **correct**, and LINK `:834`'s doc-update entry would edit a correct
  canonical statement into a false one — **delete it**.
- **LINK `:554` / E/P `:660`** — "the analyzer enforcing one-to-one correspondence between the enum
  and the DU" is false. `PRECEPT0026` polices switch-arm completeness over `[CatalogDU]` types;
  `PRECEPT0007` gates `GetMeta` exhaustiveness. The correspondence is **test**-held
  (`ProofRequirementCatalogTests`).
- **LINK `Expected(C)` (`:469`)** has no carve-out for the `maxcount`/`mincount` desugaring exclusion
  E/P's inventory (`:690`) requires — as written the pair fires a false compiler-internal error on
  **every** `maxcount` file.
- **`soundness-and-coverage.md` duty is larger than three rows.** §3.1 rows `:200`, `:201`, `:202`
  re-point; but `:220` and `:221` in §3.2 also name the two kinds, `:263` is a prose example, and —
  load-bearing — the §3.1 preamble at `:183–185` *defines* that column as naming a
  `ProofRequirementKind`, which the collapse falsifies regardless of row edits. Both docs also cite
  the column as "prevented by"; it is named "Discharging obligation / owner" (`:187`).
- **E/P `:778`** — ":201-202 = the `LengthBoundViolation` and `CountBoundViolation` rows"; those are
  `:202`–`:203`. **E/P `:762`/`:784`/`:936`** give three different row counts.
- Both docs and **LINK `:870`** — "`:1958` points forward to migration logic"; the migration sentence
  is `precept-language-spec.md:1946`.
- **E/P `:402`** — `ConstructKind.AccessMode` is `:33` (not `:31`); `OmitDeclaration` is `:36`.
- **E/P `:631`** — "a default is an authored literal or a constant expression" is false against
  `:1350`; defaults may reference earlier-declared fields.
- **E/P `:240`** — the Guard-17 "already settled" row (establishment applies to exactly two kinds) is
  re-decided by F1 and must move out of that table in the same edit.
- **LINK § Open questions live item 1** (the admissibility-condition "owner question") was ruled
  already-answered; STATUS `:145–157` assigns pass 2 an editorial sharpening, not a fork. Delete it.
- **Shared § 6 range**: E/P reads "(5)–(7)", LINK "(5)–(8)". E/P is the coherent copy — items 5–7 are
  the `:2202` plan-set items the collapse clause applies to; item 8 states its own refusal.
- **E/P is missing all four rationale legs on Decisions W-2 and W-3**, which LINK carries in full;
  and § 10 exists only in E/P. Byte-equality of the shared section is unachievable while its preamble
  licenses first-person divergence — replace the goal with "identical modulo an enumerated
  allowed-differences list" and give falsifier 6 a mechanical comparator.

## Measured, at HEAD, 2026-07-24

- 78 sample files: 64 carry `in S ensure`, 59 a `rule`, 2 `from S ensure`, **0** `to S ensure`, and
  190 `on E ensure` constraints.
- **`CountContainment` produces zero obligations corpus-wide** — never generated, not merely never
  proved. It is stamped only when a *field* declares `mincount`/`maxcount`, and no field in any
  sample does. Controls: `LengthContainment` 298 obligations (298 Proved), `IntervalContainment` 74
  (72 Proved). The discharge path exists, is unit-tested, and does prove.
- Collection cluster (constraint mentions a collection field that is collection-written): **6 files**
  — E/P calls this "the single largest unmeasured component"; it is the smallest.
- Default-materialization cluster: **54 files**. Computed cluster: **7–8**. Editable-mentioned: **35**.
  `omit`: 25. Presence-form residency conditions: **287 of 370**.
- Obligation volume under F1: worst file ~3,360 preservation obligations before mention filtering;
  corpus upper bound ~49,000. Bounded, but an order of magnitude above anything either document
  contemplates — falsifier 2 (compile-time) deserves re-reading against it.
- **Two independent at-risk counts disagree** — 50 ensures / 32 files versus ~94 pairs / 45 files,
  using different at-risk definitions. Reconcile before either number enters a design doc.

## Bugs filed this pass

**BUG-063** (`into` slot not type-checked against element type, spec `:1708` — blocks shipping),
**BUG-064** (computed-field write protection is `set`-only; `dequeue … into K` and `clear K` slip
through — and matrix `:355`'s single-writer premise cites exactly this check), **BUG-065** (a computed
expression may reference an event argument, against `:1351`), **BUG-066** (`<- now()` compiles clean;
non-configuration computed expression, with the proof-engine consequence that its stored dependency
set is empty).

## What is genuinely the owner's

1. **Guard-complement facts.** No premise class reaches the complement of an earlier first-match row's
   guard, and the premise classes are owner-ruled (want `:19`). It is semantically sound to license —
   `:1999` makes first-match dispatch evaluate earlier guards false against the same pre-state at
   phase 2, before any write, and survival to the write site is the existing frame-and-kill. Measured
   need: **2 shipped files confirmed by hand** (`crosswalk-signal`; `saas-customer-success` at three
   separate occasions), with a **38-of-78** structural ceiling. Until ruled, every needing file
   rejects. This is now the largest single blocker in the corpus and was carried through round 1 as
   "unresolved risk".
2. **How to classify the computed-coherence fact.** It fits neither bucket of the 2026-07-21 ruling:
   as a value-fact its citation duty is unsatisfiable (no family mints an establishing obligation for
   it), and as context it fails the ruling's iff. Small, with an obvious safe shape, but it is an
   extension of an owner ruling against that ruling's own tightly-drawn tradeoff clause.
3. *(Standing, unchanged)* `:2202` and the ⧖ Q13 `omit`-value conflict. The self-transition question
   for **ensures** is a new neighbour of `:2202` — the design can proceed in the refusing direction
   without a ruling, and the ruling would be a deletion rather than a re-derivation.

**Checked and NOT an owner fork**: whether a value fact written by an earlier operation can be a
premise for a later obligation. Canon forecloses it — the transport rule is explicitly
intra-operation (`:140`, "the same operation"), premise class (d) reaches declared *constraints* only,
the certificate premise vocabulary is closed at eleven kinds with none for cross-operation values,
and the 2026-07-21 citation duty makes such a proof unstatable (no declared constraint means no
establishing obligation to name). The `loan-application` rejection is the ruled behaviour of the
2026-07-20 activation-sites ruling, and it has a licensed two-line respelling (a guard on the
entering row, premise class (c)) — plus an honest-absence rewrite already worked and compiled in
`compile-time-niche-evidence-legs-2026-06-10.md`, which made the file *shorter*. The want doc
embraces this friction explicitly (`:139`).

## What pass 3 must do

1. Close F1's two occasion-domain holes and pin self-loop plan-set membership; record the
   `library-hold-request` regression in the cost section rather than discovering it at corpus-run.
2. Decide whether A1's route expansion licenses state-scoped pre-state premises — and if it does,
   route that to the owner rather than absorbing it.
3. Apply the F3/W5 amendments and write the closure fixpoint derivation `:1354` licenses.
4. Re-author Decision 2's held-count rationale against the measurement (zero corpus instances; the
   collapse would cost a demonstrably-proving discharge, and would *not* be a fail-open — the current
   stated reason is wrong), and Decision 5's precedent leg from scratch.
5. Apply the correction list above to both documents, and reconcile the shared section.
6. Put the two genuine owner items forward, with the measured populations.
7. Only then consider re-locking — and not in the same session as any argument's first draft.
