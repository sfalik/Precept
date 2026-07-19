---
title: Frank's Opinion — what-i-want-2026-07-16
date: 2026-07-18
author: Frank (Lead/Architect & Language Designer)
status: Working — advisory review for Shane; not a ratified decision
owner: Shane
reviews: docs/Working/what-i-want-2026-07-16.md (Draft — 2026-07-16, Shane)
read-in-full:
  - docs/Working/what-i-want-2026-07-16.md
  - docs/Working/what-i-want-2026-07-16-review.md (adversarial F1–F15, 2026-07-18)
  - docs/Working/third-arm-forensic-analysis-2026-07-15.md
  - docs/Working/prove-or-reject-scope-forensic-2026-07-15.md
  - docs/Working/prove-govern-collation-index-2026-07-15.md (forks F1–F12)
  - docs/philosophy.md
  - docs/Working/decision-index.md
  - docs/language/collection-types.md
  - docs/compiler/soundness-and-coverage.md
  - docs/compiler-and-runtime-design.md §1/§1.1/§1.2 (committed e13b94cd "KNOWN SELF-CONTRADICTION")
  - docs/Working/frank-prove-or-reject-position-2026-07-11.md
  - docs/Working/frank-retrospective-proof-engine-arc-2026-07-13.md
  - docs/Working/frank-go-forward-posture-replay-2026-07-14-v2.md
---

# Frank's Opinion — `what-i-want-2026-07-16.md`

Shane — I read the whole corpus you named, in full, against the tree as it stands today. Not
from memory of prior sessions; those sessions are exactly why this corpus contradicts itself,
and I was not going to inherit the contradiction a fourth time.

**Top line, up front.** This is the strongest single artifact the six-week argument has
produced, and it is the first one that could actually end it — because it is *yours*, which
moots the authority-inflation that let every agent draft "ratify" itself by adjacency
(collation F11). It resolves the two open forensic forks decisively, closes most of the
adversarial review, and closes the defaults hole that two independent passes said produced
*wrong answers*, not gaps. The spine is right.

But it is **not go-as-written**, and the reasons are precise, not stylistic:

1. It **expands compile-side scope** past the ratified 07-12 package — it proves *relational
   invariants by induction*, which the package **governed at runtime** — and it does so
   **without drawing the scope line** that keeps that ambition from becoming the June effort-
   axis spiral again. The stopping rule is implied but not stated. That omission is the single
   most dangerous thing in the document. *(The 07-16 amendment now draws this line for the
   **non-linear** case — a worked `RepaymentCapPercent` rule, restating guards, an unguarded-
   rejection mutation row, and a scope-line paragraph; see §2 M1 and §3, verdict closed. The
   **general** stopping rule and quantified-predicate scoping remain implied; that residue is
   what fix 1 in §6 still asks for.)*
2. It **contradicts canon you committed nine days ago** (`e13b94cd`, whose own message says
   *"KNOWN SELF-CONTRADICTION"*) on the post-mutation sweep. `what-i-want` deletes the sweep;
   `compiler-and-runtime-design.md` §1.1 and §1 keep it as the runtime enforcement point for
   relational invariants. Landing `what-i-want` on top of that canon *without reconciling first*
   is the exact mechanism the whole corpus blames for six weeks — a new draft faithful to one
   half of a self-contradicting canon.
3. It **silently re-imports overflow** as a proven compile-time guarantee (collation F10) and
   **re-closes the escape hatch without naming the band-suppression cost** (collation F4/F9)
   — both are honesty gaps, not architecture gaps, but this document's *job* is honesty.

My recommendation is **REWORK-then-GO** — a conditional go on four named fixes, not a no-go.
Details in §6. Everything above is defended below.

---

## 1. Scope delta — explicit before/after

The question is not "is prove-or-reject new" — it isn't; it has been the posture since the
07-06 ruling and the 07-12 ledger ratified it. The question is **what `what-i-want` changes
about the *shape and reach* of the compile-time obligation.** It changes it materially, in one
direction, and the change is bigger than it looks.

### Before — the readiness plans + 07-06 ruling + 07-12 ledger

The ratified model (`proof-engine-boundary-ruling-2026-07-06.md`; `decision-ledger` #1;
`frank-go-forward-posture-replay-2026-07-14-v2.md` §3) is **Hybrid, total over the *fault
surface*, routed by provenance**:

- **Fault-prone operations over a definition-derived value** → compile-time prove-or-reject.
  The enforced set is division, `sqrt`/`pow`, empty-collection/index access, **declared-bound
  containment** of a *computed* result, and **count/length containment** of a *derived* count.
- **A declared relational invariant over independently-set fields** — linear or nonlinear —
  is **GOVERNED, not proven**, *"Zero expressiveness loss"* (`boundary-ruling:226`; v2 §3, §4).
  The corpus is emphatic about this: `rule Balance == Deposits - Withdrawals` and
  `rule TotalCost == AvgCost * Qty` are *governed*, because a relationship among independent
  fields has no derived-value obligation to discharge.
- **Runtime governance is two mechanisms** in the ratified/committed reading: **ingress**
  (external value at its slot) **and the post-mutation sweep** (whole-entity invariants against
  the resulting configuration). This is committed canon today —
  `compiler-and-runtime-design.md:133`: *"any relational or comparison rule over the
  configuration… mutations run on a working copy discarded whole if any constraint fails
  (spec §3A.4)."*
- **Overflow** is the one **disclosed park** (D1; `decision-index:19`; v2 §2).
- The **stopping rule** — the thing my retrospective (§5) says the June effort lacked — is the
  certificate-criterion clause 4 ("earns its place"), the §1b deferral, and §6 ("author states
  the fact, engine applies it"). *That trio is what makes the package buildable rather than an
  engine that grows forever.*

### After — `what-i-want`

- **Relational invariants are now PROVEN by induction over reachable configurations, or
  rejected** — they are no longer governed. This is the load-bearing change. The doc's own
  words: *"proof by induction over reachable configurations"*, with **premise class (d) — "all
  rules holding in the pre-state"** as the inductive hypothesis, and **"symmetric obligations…
  attach to every write site of *every* field the rule mentions."* The worked `ReduceLimit`
  row is the demonstration: `rule Balance >= -OverdraftLimit` is *proven* preserved at the
  `OverdraftLimit` write site by substitution under a guard — **not swept at runtime.**
- **The post-mutation sweep is deleted as a governance mechanism.** *"the runtime does **not**
  re-evaluate them against the post-mutation configuration"* (Certificates); *"No… re-checking
  of proven bounds on writes — the evaluator just computes"* (The runtime's role).
- **Runtime governance collapses to ingress, and ingress is exactly two points** — event args
  (incl. construction) and editable-field writes. Relational enforcement over *editable* fields
  is folded **into editable-field ingress** as a *declared proof premise*: *"For an editable-
  field write, it's the field's modifier-rules **and every rule that mentions the field**…
  evaluating those rules at the ingress point is exactly the premise that closes the editable
  write sites in the compiler's proof."*
- **Modifiers are sugar for rules** — *"There is one constraint mechanism underneath."* Bound
  containment and business rules are the *same* obligation kind, proven the same way.
- **Certificate + independent checker** is elevated from "criterion" to a **shipped component
  with two call sites** — a compiler stage *and* a **load-time runtime gate** that refuses to
  govern under an unverified certificate.

**Net delta, stated plainly:**

| Axis | Ratified (before) | `what-i-want` (after) | Direction |
|---|---|---|---|
| Relational invariants over independent fields | **Governed** at runtime (sweep) | **Proven** inductively, or **rejected** | **Compile scope ↑↑** |
| Post-mutation sweep | Second governance mechanism (committed canon) | **Deleted** | Runtime ↓ |
| Editable-field relational enforcement | Sweep | Editable-**ingress** as a proof **premise** | Reframed |
| Certificate re-checker | Deferred (soundness §5b) | **Load-time gate, shipped** | Scope ↑ |
| Containment ground (i/ii) | Contested (fault vs computed) | **Dissolved** — one mechanism | Simplified |
| Overflow | Disclosed park (D1) | **Silent** — reads as proven | Honesty ↓ (fixable) |

**What is *added*:** inductive invariant preservation as the proof model (Floyd–Hoare/Event-B
territory); the inductive-hypothesis premise class (d); symmetric write-site obligations; the
shipped certificate checker with a load-time gate; weakest-precondition *synthesis* for
diagnostics.

**What is *removed*:** the post-mutation sweep as a governance arm; the "relational invariants
are governed, zero expressiveness loss" concession that answered the whole "21/77 products"
cost argument.

**This is not a restatement of the ratified posture. It is a strictly larger compile-time
bet.** It takes the hardest, most-deferred thing in the corpus — proving relational invariants
across every reachable operation ordering — and moves it from "governed" to "prove-or-reject."
That can be the right call. But it must be *seen as a scope expansion*, because the last time
prove-or-reject was pushed past what the engine could carry over the corpus's house style, it
read as a posture failure and cost a month (retrospective §1.2, §4). The document does not
flag that it is enlarging the bet. It should.

---

## 2. Feasibility — buildable solo + agents?

**Verdict: buildable, conditionally — and the condition is a scope line the document now draws
for the non-linear case but not yet across the board.** The linear/decidable core is a bounded
build for one person with agents. The nonlinear-relational-preservation ambition is a research
project *if taken literally*, and *must* be scoped as reject-or-author-states-it — which, since
the 07-16 amendment, the document now does explicitly: it works a non-linear rule
(`RepaymentCapPercent`), draws the line in prose, and enumerates the unguarded-rejection case in
the "what must not compile" table (see M1 below and §3). Overflow must stay parked. With those
cuts, yes. Without them, this recreates the June spiral, and I will say so before it happens
rather than after.

Here is every genuinely-new piece of machinery `what-i-want` requires that the ratified
package did **not**, with a buildability call on each.

### New machinery M1 — Inductive rule-preservation proof (base case + inductive step + symmetric obligations)

The ratified engine *governed* relational invariants. `what-i-want` *proves* them: for every
handler, prove every rule is preserved, discharging from premise classes (a) field modifiers,
(b) arg constraints, (c) the guard, and **(d) all pre-state rules (the inductive hypothesis).**

- **Linear/decidable fragment** (`Balance >= -OverdraftLimit`, `Floor <= Ceiling`,
  `EndDate >= StartDate`): **MEDIUM, buildable.** This is Hoare-style substitution of the
  handler's mutations into each rule, then discharge by linear arithmetic over the collected
  facts including the IH. The BankAccount example is depth-1 linear and closes exactly this
  way. It is a genuine extension of the built "one field vs one field" engine and of §1a
  (multi-term linear facts), but it is the *same class of reasoning*, and the ⊥-rail /
  single-hop discipline from soundness §7 already applies. A solo build with agents can do this.
- **Nonlinear fragment** — and here the corpus has been conflating two different things under
  the one word "nonlinear," so I separate them before I rule:

  - **(a) A non-linear *expression*** — a product or quotient of fields, `AvgCost * Qty`,
    `-Balance / Months`. Non-linearity of the *term*.
  - **(b) A non-linear relational *shape*** — the obligation the compiler must discharge is
    itself non-linear, e.g. proving `MonthlyRepayment <= OverdraftLimit * RepaymentCapPercent`
    holds across an arbitrary write, where a *product of free variables* stands on one side and
    there is nothing to cancel.

  The corpus treats these as the same undecidability and uses (a) to justify governing (b).
  `hybrid-model-draft-B-fable-coordinator-2026-07-14.md:26` argues a prove-everything posture
  *"would force their rejection — banishing exactly the rules a domain expert most needs to
  write,"* citing `rule Width * Height <= BedArea`; the boundary ruling
  (`proof-engine-boundary-ruling-2026-07-06.md:226`) lists *"Genuinely nonlinear governance rule
  (product / ratio / balance identity)"* as **Expressible — governed, "Zero expressiveness
  loss."** Both are still accurate quotations on re-check — and both slide from "the *expression*
  is non-linear" to "therefore *govern* it." That slide is the error. Non-linearity of the term
  does not settle the fate of the obligation. A non-linear obligation has **three** fates, not
  the two ("reject, or build a solver") I wrote in an earlier draft of this section:

  1. **Reject** — no guard or arg constraint makes the post-state condition directly checkable.
     The compiler refuses and names the premise that would close it. Buildable today; it is the
     existing prove-or-reject rail.
  2. **A restating guard closes it — premise class (c), zero new machinery.** If a guard (or arg
     constraint) *restates the post-state condition verbatim*, the proof closes against what the
     author wrote, not against algebra the compiler attempts. This is the **middle fate the
     earlier draft missed**, and it is the important one: a *guarded* non-linear rule is not a
     solver problem at all — it collapses onto the exact premise-(c) discharge the linear engine
     already carries (`PlanRepayment`'s guard restates `-Balance / Months <= OverdraftLimit *
     RepaymentCapPercent`; the proof is a syntactic match, no interval arithmetic). Buildable
     today, on machinery already scoped.
  3. **Bounds + induction with no restating guard — needs a solver.** Only if you try to
     *derive* preservation of a product-bearing invariant from field bounds and the inductive
     hypothesis, with no premise that restates the condition, do you land in genuine non-linear
     reasoning — interval arithmetic, a real new proof burden, the research track. This is the
     one fate that is HARD → undecidable in general and **not** buildable solo.

  **My verdict: an unguarded non-linear relational rule should reject; a guarded one closes by
  premise class (c) with zero new machinery.** The compiler never reaches for fate 3 — it never
  carries a solver, never governs via a post-mutation sweep. So "prove relational invariants
  inductively, non-linear included" is *not* the research project the two-fate framing implied,
  **provided the line is: guarded → premise (c); unguarded → reject.** The whole cost is the
  guard tax, which the corpus already prices.

  **This was the crux feasibility risk, and the 07-16 amendment now speaks to it directly.**
  "Prove or reject all business rules" over a corpus whose house style is 21/77 files using
  products still means a real guard tax — either rejections the author repairs by adding a
  restating guard, or rules linearized. But that is fate 1/fate 2, not an engine that grows
  forever, and the doc now says so: the scope-line paragraph draws the line in the open (*"the
  author guards them, linearizes them, or the compiler refuses them"*) and the worked
  `RepaymentCapPercent` case demonstrates fate 2 while the mutation table demonstrates fate 1.
  The doc's own escape valves — *"trade off some expressibility to gain proof"* and *"the
  surface is the negotiable part"* — are the right license, and the amendment finally turns that
  license into a drawn line. Whether the drawn line *satisfies* the omission I flagged is a
  comprehensiveness question — I answer it in §3.

### New machinery M2 — Certificate format + independent checker, shipped, with a load-time runtime gate

The format is designed (`soundness-and-coverage.md` §5a). The re-checker is **deferred** in
canon (§5b: *"not in the MVP… a separate, larger build"*). `what-i-want` **un-defers it** and
adds a **second call site the package never had — a load-time gate in the runtime.**

- **Checker: MEDIUM, buildable.** By construction it walks a derivation and re-applies
  primitive inferences from the `CertificateSteps` catalog. It is bounded, not a solver —
  that is the whole point of the certificate criterion. This is proof-carrying code done right
  (Necula & Lee, which the doc cites correctly) and the small-trusted-kernel discipline of LCF-
  style provers. Prior art is deep and on our side.
- **Load-time gate: EASY–MEDIUM, buildable, and I *like* it.** It resolves adversarial F15
  (the "trust the compiler" vs "re-verify at runtime" tension) by making the trust *earned by
  verification the runtime performs itself, once, at load*. That is a better answer than
  either "blindly trust" or "re-check every operation," and it is cheap: one walk per
  definition load. Keep it.

### New machinery M3 — Weakest-precondition *synthesis* for teachable diagnostics

The package already scopes WP *printing* (`Unresolved(condition)` carries "the weakest
precondition printed verbatim", Stage 0b Decision A). `what-i-want` wants more: *"it computes
the guard or arg constraint that would close the proof and shows it — 'adding `when Balance >=
-ReduceLimit.NewLimit` would make it provable'."* That is **abduction**, not printing.

- **Linear fragment: MEDIUM.** For the linear case the missing guard often *is* the WP, so
  synthesis and printing nearly coincide. Buildable.
- **General: HARD, over-promised.** For nonlinear obligations there may be no clean single
  authored premise. Scope the *teachable synthesis* to the same fragment as M1 and, outside it,
  fall back to "here is the WP; I cannot suggest a single clean repair." Honest, and it matches
  the M1 line.

### New machinery M4 — Editable-field ingress evaluating "every rule that mentions the field," against the resulting configuration

This is the *replacement* for the deleted sweep, scoped to editable writes.

- **EASY–MEDIUM, buildable.** It is the existing constraint evaluator, indexed by field, run at
  `Update`. The machinery already exists in the runtime design; `what-i-want` narrows it from
  "sweep everything every operation" to "evaluate the affected invariants at the editable
  ingress." *But be honest about what it is* (see §4): evaluating `Floor <= Ceiling` while
  editing `Floor` **reads `Ceiling` from stored state** — it is a targeted evaluation against
  the *resulting configuration*, not a "pure ingress check on the entering value." That is
  fine and sound; it just is not what philosophy.md:55's composition seam describes (§6 flag).

### New machinery M5 — Overflow prevention

`what-i-want` lists overflow in the proven fault family and omits its guards from the runtime.
But overflow is **parked (GATE-O), not built** — *"no live compile-time guarantee"* (D1;
soundness §6). As an *ideal-world* claim it aligns with philosophy.md:53; as a *build* claim it
is false today. **Not new machinery to build now — but the document must carry the park as its
one disclosed carve-out**, exactly as v2 §2 does, or a reader re-imports overflow as enforced.

**Bottom line on feasibility.** M2 and M4 are clean wins. M1-linear and M3-linear are buildable
extensions of work already scoped. M1-nonlinear, correctly framed, is not the research project
the two-fate reading implied — a *guarded* non-linear rule closes on premise class (c) (fate 2),
an *unguarded* one rejects (fate 1), and the compiler never enters fate 3; the 07-16 amendment
now draws exactly that line (scope-line paragraph + worked `RepaymentCapPercent` case). M3-general
and quantified-predicate preservation are where "one person + agents" still either holds a hard
scope line or spirals, and those the document has *not* yet lined the way it now lines the
non-linear case. **Buildable = yes, iff the line is: prove the linear/decidable fragment
inductively; discharge guarded non-linear rules by premise class (c); reject the rest with a
teachable message and let the author state the rule; park overflow. The document now says that
for the non-linear case; it must say it for the rest.**

---

## 3. Comprehensiveness gap list

The worked example is scalar, single-precept, stateless-adjacent. It is a *good* illustration
and the "what must not compile" table is the best thing in the document — the promise made
operational. But it is not the obligation surface. Here is what it omits, each tagged **"more
of the same premise class"** (the four premise classes and prove-or-reject discharge it as-is)
or **"new proof burden"** (needs machinery or a ruling the four classes don't supply) — and,
first, the one previously-flagged omission the 07-16 amendment has since **closed**.

- **Non-linear relational rules** — `rule MonthlyRepayment <= OverdraftLimit * RepaymentCapPercent`,
  a product of fields. **This was the sharp omission I flagged as unaddressed — the scope silently
  expanded past the ledger with no stated scope-line, the exact shape of the June miss. The 07-16
  amendment closes it.** **More of the same premise class** (guarded → premise (c); unguarded →
  reject), *not* a new proof burden — the §2 M1 (a)/(b) analysis shows why: non-linearity of the
  *expression* does not make the *obligation* undecidable when a guard restates the post-state
  condition. The amendment now (i) adds the rule and a field `RepaymentCapPercent`, (ii) closes it
  by restating guards on both `PlanRepayment` and `ReduceLimit` (the symmetric write site, from the
  `OverdraftLimit` side), (iii) enumerates the unguarded-rejection case in the mutation table (*"the
  product is non-linear, so substitution can't close it and no post-mutation sweep or solver stands
  behind it — the guard must state the post-state condition directly, or the rule must be
  linearized"*), and (iv) states the discipline in prose (*"never governed by a post-mutation sweep,
  and never deferred to a solver the compiler doesn't carry… the author guards them, linearizes
  them, or the compiler refuses them"*). The whole thing compiles clean in `samples/Test.precept`.
  **Verdict: closed.** The scope-line requirement I flagged — *the document must state that unguarded
  non-linear relational rules reject, and demonstrate it* — is now met on all three axes: stated,
  worked (fate 2), and refused (fate 1). The only residual is the generic "one scalar case is not
  the whole product/ratio surface" caveat that applies to every worked example; it is not a defect,
  and it does not reopen the omission. This gap is off the required-fixes list (see §6, fix 1).

- **Collection access / emptiness / index / key safety** (`.peek`/`.first`/`.at`/`dequeue`/
  `pop`/`lookup for K`, `collection-types.md` §Emptiness Safety). **More of the same premise
  class.** These are fault obligations discharged by a `.count > 0` / index / `contains K`
  guard — premise class (c), identical in shape to `PlanRepayment`'s divide-by-zero discharge.
  Nothing new.
- **Cardinality-bound containment** (`mincount`/`maxcount` across `add`/`remove`/`enqueue`/
  `dequeue`, `CountBoundViolation`). **More of the same premise class**, with one wrinkle: the
  inductive step is over **count arithmetic** (`count + 1`, `count - 1`) rather than value
  arithmetic. Same linear reasoning, same discharge, no new burden.
- **Quantified invariant *preservation*** (`rule each x in C (x > 0)`, `rule any x in C (…)`).
  **NEW PROOF BURDEN, and this is the sharp one.** `collection-types.md` currently calls a
  quantifier *"a **runtime governance** rule over the current elements"* — i.e. today's design
  **governs** it, does not prove it. Under `what-i-want`'s no-sweep, prove-or-reject-everything
  model, a quantified rule mutated by a *handler* action (not an editable edit) has no ingress
  door and must be **proven preserved inductively**:
  - **Universal under `add`**: tractable — prove the *added* element satisfies the predicate;
    existing elements are covered by the IH. This is premise-class (d) applied to a quantifier
    — buildable.
  - **Existential under `remove`**: **generally not statically decidable** — removing an
    element can break `any x in C (P(x))` unless you can prove the removed element was not the
    sole witness, which the language cannot in general. This forces **reject**, or a fallback
    to editable-ingress-style re-evaluation that `what-i-want`'s model does not currently
    provide for handler-driven collection mutations.
  - **Predicates coupling elements to other fields** (`each x in C (x <= Budget)` where
    `Budget` is edited): the same transitive-affect problem as computed fields (below).

  This is the single biggest comprehensiveness hole. The four premise classes were written for
  scalar rules; quantified-predicate preservation, especially existential-under-removal, is a
  genuinely new obligation the worked example does not touch and the corpus explicitly
  **governed** to avoid. `what-i-want` must either extend the model to handle it or explicitly
  scope collections' quantified invariants as reject-or-author-guards.
- **Computed fields transitively affected by an edit.** `field Total <- Price * Qty`,
  `rule Total <= Budget`, `Price` editable. The doc's rule "editable ingress evaluates every
  rule that *mentions* the field" is **too narrow** — `Total <= Budget` does not mention
  `Price`. Preservation must follow the *dataflow* (edit `Price` → recompute `Total` → obligation
  on `Total <= Budget`). **More of the same premise class** (the symmetric-obligation +
  inductive-step machinery covers it *if* "affected by the write, including via computed-field
  derivation" replaces "mentions the field") — but the *wording* in the document is a latent
  soundness hole. Fix the wording to dataflow-reachability, not textual mention.
- **State-scoped `ensure` and access-mode (`editable`) transitions across states.** The
  example uses `in Active modify … editable`, but the general obligation — a rule/ensure whose
  applicability is gated by state, and the interaction of the inductive step with state
  transitions — is not worked. **More of the same premise class** (state is another guard fact,
  premise class (c)), but the base case must be established *per initial state* and the
  inductive step must respect state-conditional applicability. Worth one worked example.
- **Temporal / "current moment" rules** (`ExpiryDate > today`). **NEW — and correctly left
  open** by the document. There is no ingress door for `today` and no inductive step for pure
  passage of time. The doc parks this honestly as an open question. The gap-analysis baseline
  still needs a *disposition* (my recommendation: time belongs in guards, evaluated at event
  time; a standing `rule` over `today` is rejected or reinterpreted). Not a build blocker;
  a ruling owed.
- **Cross-entity / saga.** Out of MVP scope by declaration. Fine. Flagged only so the baseline
  records it as parked, not silently absent.

**Summary:** most of the omitted surface is **"more of the same"** — the four premise classes
plus guard discharge cover collection access, count bounds, and computed-field dataflow, given
one wording fix (dataflow, not "mentions"). The **one genuinely new burden** is **quantified-
invariant preservation**, and it is new precisely because the current design *governs* it and
`what-i-want` refuses to govern. That is the comprehensiveness cost of deleting the sweep, and
it is not "illustrative more-of-the-same." It is the next `Floor <= Ceiling`.

---

## 4. Clarity verdict

**Can this end the recurring "what is Precept / prove vs govern / compile vs runtime / third
arm" argument?** *Yes — but only if it is reworked to reconcile with committed canon and to
draw the scope line.* As written it has the right spine and would still re-open the argument on
two seams. I will not soften that: a document that ends the argument cannot itself land on top
of a `KNOWN SELF-CONTRADICTION` commit and an unbounded proof ambition.

### The two open forensic forks — my explicit stance, with citations

**Fork 1 — the third arm: spec §0.7 (`:268`, ingress-only) vs §3A.4 (`:1969`, "the sweep is
one of two enforcement points").** These contradict; committed 11 minutes apart on 2026-06-02;
unruled (`third-arm-forensic-analysis-2026-07-15.md` §3, §6).

**`what-i-want` rules for §0.7, and I endorse the ruling** — with a required addition.
- Citation that it rules: *"the runtime does **not** re-evaluate them against the post-mutation
  configuration"* (Certificates); *"No… re-checking of proven bounds on writes"* (runtime role).
  It deletes the sweep as an enforcement point. That is the §0.7 side.
- **But §0.7-alone was always incomplete** — it never said where relational invariants over
  independently-set fields get enforced (the forensic doc's base case, `Floor <= Ceiling`,
  *"appears nowhere in the entire corpus"*). `what-i-want` is the first artifact to fill the
  hole: relational invariants over **handler-written** fields are **proven inductively**;
  over **editable** fields they are **evaluated at editable ingress as a declared premise**.
  That is what makes ruling-for-§0.7 *coherent* rather than a deletion that strands cases.
- **The required addition — honesty about the editable-ingress case.** Evaluating
  `Floor <= Ceiling` while editing `Floor` reads `Ceiling` from *stored state* — it is a
  targeted evaluation against the **resulting configuration**, which is the sweep's job, done
  at ingress scope and re-labeled as a proof premise. That is sound and I accept it, but the
  document must *say* it plainly, or the §0.7/§3A.4 confusion recurs under the word "premise."
  This is not "pure ingress on the entering value."
- **The canon action this ruling entails** (and the doc does *not* name): **delete
  §3A.4:1969's second paragraph** — the mechanism the forensic doc identified as the anti-
  recurrence fix (*"whichever wins, the other sentence is deleted"*). The doc self-flags only
  the philosophy.md Fire sentence; it must also flag the §3A.4 deletion and the re-edit of
  `compiler-and-runtime-design.md` §1/§1.1 (see the working-tree conflict below).

**Fork 2 — containment ground: (i) "it's a fault" (refuted) vs (ii) "it's computed" (the
undeclared migration).** (`prove-or-reject-scope-forensic-2026-07-15.md` §1, §6.)

**`what-i-want` dissolves the fork — the best available outcome — landing effectively on ground
(ii).** Citation: *"Modifiers are sugar for rules. `nonnegative` on `OverdraftLimit` is a
compact spelling of `rule OverdraftLimit >= 0`… There is one constraint mechanism underneath."*
Containment is a rule-preservation obligation proven inductively, not a fault-registry
membership claim. Because *rules and faults get "the identical premise-and-certificate
treatment,"* it no longer *matters* whether out-of-range is "a fault" — the machinery is one.
That is stronger than picking (ii); it makes the (i)/(ii) distinction inert.
- **One residual wording flag.** The `PlanRepayment` bullet still writes *"faults (division by
  zero, overflow, out-of-range)"* — ground-(i) language, calling out-of-range a fault. Tighten
  it: out-of-range is a bound-containment obligation proven like any rule. Left as-is, it is
  the quotable trapdoor the forensic doc warns about (*"every future draft can re-derive the
  gate faithfully and collapse the band lane"*, §11). Minor, but it is the fossil.

### The working-tree files — do they already commit to a router that conflicts with `what-i-want`?

**Partly yes, and this is a go-blocker until reconciled.** The two files the collation flagged
(compiler-and-runtime-design.md §1.1, soundness-and-coverage.md §1.1) are no longer working-
tree — they were **committed as `e13b94cd`, whose message is literally
`docs(compiler): commit the in-flight canon edits -- KNOWN SELF-CONTRADICTION`.**

- **Compile-vs-runtime router = provenance: NO conflict.** `compiler-and-runtime-design.md:125`
  routes by provenance (*"where it is proven (computed…) versus governed (…externally-supplied)
  is decided by provenance"*). `what-i-want` agrees: handler-computed → proven; arg/editable →
  ingress. Same axis.
- **Runtime governance mechanism = sweep: DIRECT conflict.** `compiler-and-runtime-design.md:133`
  keeps the sweep as canon — *"any relational or comparison rule over the configuration…
  mutations run on a working copy discarded whole if any constraint fails (spec §3A.4)"* — and
  :125 routes `PlanMonthlyFee` and every *"free-standing relational `rule`"* to **runtime
  governance**, on **every operation**, not just edits. `what-i-want` **deletes the sweep** and
  **proves** those relational invariants (or ingress-checks the editable ones). For a rule like
  `PlanMonthlyFee > 0 when Converted`, where `ConversionStatus` flips in a *handler*, committed
  canon **sweeps**; `what-i-want` **proves preservation by the handler**. Different mechanism,
  same case.
- **The `KNOWN SELF-CONTRADICTION` itself is unresolved by `what-i-want`.** soundness §1.1 says
  the boundary is *"proof-carrying-vs-not, never band-vs-rule"*; design §1.1 says *"provenance."*
  The collation flagged this (F5). `what-i-want` lands on a *third* framing (inductive
  preservation + ingress premise) and reconciles neither. So `what-i-want` would require
  **re-editing both committed files** — replacing sweep-governance-of-relational-invariants with
  inductive-proof + editable-ingress-premise, and picking one router wording.

**Conclusion:** the committed canon commits to a router answer (provenance) that `what-i-want`
*keeps*, and a governance mechanism (sweep) that `what-i-want` *contradicts*. Reconcile the
canon **before** promoting `what-i-want`, or you re-run the six-week failure mode: a new draft
faithful to one half of a self-contradicting canon.

### Proposed distillation structure — the gap-analysis baseline

A canonical doc usable to gap-analyze *exhaustive obligation cells* against **is achievable**,
and `what-i-want`'s worked example + "what must not compile" table is exactly the seed. Extend
`soundness-and-coverage.md` §3 (the existing coverage-matrix skeleton) into an **obligation-cell
matrix**:

- **Rows** = every *(write site × rule/fault kind)* pair, over the full surface: scalar
  containment, faults (div/sqrt/overflow), relational invariants, collection access, count/length
  bounds, quantified predicates, temporal, state-scoped ensures.
- **Columns** = the four obligation positions `what-i-want` names: **base case** (default
  config + initial events), **inductive step** (per handler, premise classes a–d), **symmetric
  obligations** (per mentioned/derived field), **ingress premises** (arg + editable-field).
- **Each cell disposition** = one of: **Proven** (which premise class discharges it) /
  **Rejected** (teachable message; the §6 author-states-it repair) / **Ingress-premise**
  (editable-field re-eval against resulting config) / **Parked carve-out** (overflow, temporal,
  cross-entity — disclosed, not silently absent).

That matrix *is* the anti-recurrence device: it turns "does Precept prove X?" into a written
cell (the bucket test, soundness §3.4), and it makes the scope line **visible** — every
"Rejected" cell is where expressibility was traded for proof, on the record. Build that, and the
argument has nowhere left to hide.

---

## 5. Prior-review disposition

### (a) Adversarial review F1–F15 vs the *current* revision

The current tagged revision has visibly grown a worked example, a mutation table, an "ingress is
exactly two points" statement, and a philosophy-flag — several read as direct answers to the
F-numbers. My assessment, with citations to the current doc:

| # | Finding | Verdict | Citation in current doc |
|---|---|---|---|
| F1 | "Proves or rejects" has two readings | **CLOSED** | *"proof by induction over reachable configurations — not proof that each rule is a static mathematical truth"* — a precise third reading, neither A nor B |
| F2 | Never rules on post-mutation constraint eval | **CLOSED** | *"the runtime does **not** re-evaluate them against the post-mutation configuration"*; philosophy-flag names the Fire sentence |
| F3 | No-deferral boundary not mechanically applicable (relational/temporal/authored) | **CLOSED (temporal parked)** | relational → `ReduceLimit` proven via symmetric obligation; authored → *"It never inserts the premise itself. The author writes it"*; temporal → explicit open question |
| F4 | "Dilute vs balance" no criterion | **CLOSED** | *"the guarantee is non-negotiable; the surface is the negotiable part… Weakening the guarantee is dilution"* |
| F5 | "Lighter" undefined + philosophy conflict | **CLOSED** | *"'Lighter' means concretely: fault-prevention checks are omitted at evaluation time… no re-checking of proven bounds"* + philosophy-flag |
| F6 | Ingress set incomplete / exhaustive? | **CLOSED** | *"Ingress is exactly two points: event args (including on initial/construction events) and editable-field writes. Restore/rehydration is not ingress"* |
| F7 | "Simple validation" two readings | **PARTIAL** | resolved toward "evaluate the declared rules, relational included" (*"every rule that mentions the field"*) — but the earlier "stays simple" adjective is now mildly loose against relational eval |
| F8 | Assumption: every precondition expressible as a constraint | **PARTIAL** | mostly answered — relational preconditions live in **guards** (*"anything relational an event needs lives in its guards"*) — but the general expressibility-closure assumption is still unstated |
| F9 | Assumption: simplicity ⇒ decidable proof (stated as fact) | **CLOSED** | now demoted to a question: *"Whether the current surface actually delivers tractable proof everywhere is a feasibility question"* |
| F10 | Assumption: cross-entity via a governable door | **CLOSED (by scoping out)** | *"cross-entity data is out of scope"*; cross-precept promise *"Deliberately left open"* |
| F11 | "Shouldn't have to understand" vs mechanisms force it | **CLOSED** | adopts exactly F11's offered reconciliation: *"the guarantee is uniform… what the author is spared is… ever having to ask 'how strongly is this rule held?'"* |
| F12 | "Not an exhaustive list" escape hatch on runtime duties | **PARTIAL / OPEN** | forbidden side is now sharper (prove-or-reject), but the permitted side still reads *"not an exhaustive list"* — open-ended |
| F13 | "Strict immutable versioning and write guarantees" undefined | **CLOSED** | now defined + cited: *"Version is an immutable snapshot; every operation returns a new Version"* (`runtime-api.md`) |
| F14 | MVP-scoping imports balancing into the ideal; post-MVP ideal undefined | **PARTIAL** | single-precept clearly labeled MVP and cross-precept explicitly deferred, but "single-precept as ideal vs practicality" is still slightly conflated |
| F15 | Certificate-at-runtime vs "trust the compiler" | **CLOSED** | turned into a feature: *"a trust it establishes by verification, once, at load"*; *"rooted in verification the runtime performed itself, not in provenance it assumes"* |

**Score: 11 closed, 4 partial (F7, F8, F12, F14), 0 open.** This is a strong revision. The
partials are wording/openness, not architecture — none of them is load-bearing the way F1–F3
were, and F1–F3 are the ones that closed hardest.

### (b) Collation index forks F1–F12 vs `what-i-want`

| # | Fork | Verdict | Basis |
|---|---|---|---|
| F1 | Ingress-only vs sweep (canon vs canon) | **CLOSES (substance)** | rules for §0.7 (no sweep); supplies inductive proof + editable-ingress premise. *Must still pair with §3A.4:1969 deletion + canon re-edit* |
| F2 | Scope of the "no third arm" ban | **CLOSES (dissolves)** | no third arm exists — relational invariants are proven or ingress-checked, never swept |
| F3 | Ledger #1's justification conditional on declined package items (#3/#5) | **SIDE-STEPS** | drops the dependency on the ledger's justification; re-grounds on the inductive model + guarantee/surface criterion. But *enlarges* the engine bet past the package — makes the "honest only over a strengthened engine" condition **bigger**, not smaller |
| F4 | Ledger #9 closed the escape hatch without engaging constraint-deletion | **SIDE-STEPS / REOPENS** | re-closes the hatch (*"a premise the author never wrote is deferral relabeled as authorship"*) and does **not** name the band-suppression cost my own v2 §9 accepted on the record |
| F5 | Router: provenance or proof-carrying (live canon contradiction) | **SIDE-STEPS + NEW CONFLICT** | keeps provenance; introduces a fresh conflict with committed canon on the **sweep**; does not resolve the `KNOWN SELF-CONTRADICTION` |
| F6 | Two dispositions or four | **SIDE-STEPS (moots)** | offers its own clean taxonomy — prove / reject / ingress-govern — without reconciling the 2-vs-4 debate |
| F7 | The defaults hole (`default 1` vs `rule X >= 10` — wrong answer, not gap) | **CLOSES** | base case makes it a compile-time rejection: *"Change the default to 20000.0 → Default value violates `max 10000.0` — the base case has a counterexample"* |
| F8 | `set X = e` vs `rule X == e` evadability | **CLOSES (dissolves)** | rules are proven inductively too (modifiers = rules), so there is no "rewrite as rule to get governed" laundering path |
| F9 | Band-suppression overridden, not solved | **REOPENS (silent)** | unaddressed; same gap as F4 |
| F10 | Overflow: philosophy asserts what isn't built; two-sided statement required | **REOPENS (silent)** | lists overflow in the proven fault family; omits the disclosed park. Must carry the two-sided carve-out |
| F11 | Authority inflation (folded items ratified by adjacency) | **CLOSES** | owner-authored ground truth replaces inherited agent ratifications — the whole point of the document |
| F12 | Unclosed implementation semantics (PRE0078, OutOfRange registry, BUGs) | **SIDE-STEPS (out of scope)** | correctly out of scope for a want statement |

**Score: closes 5, side-steps 4, reopens 3 (F4/F9 band-suppression; F10 overflow; F5 new
canon conflict).** The reopens are the rework list. None is fatal; all are nameable fixes.

---

## 6. Overall opinion

**Soundness.** The architecture is sound *for the linear/decidable fragment* and sound *as a
posture*. Inductive invariant preservation is the correct and honest model for what "prove a
business rule" means over runtime-supplied values — it is what philosophy.md:51 actually
demands (*"No operation can produce a result that violates a declared rule"*), and it delivers
:51 **more faithfully than the sweep model did**, because it makes the invalid configuration
*unreachable at authoring time* rather than *discarded at commit*. The one soundness *hazard* in
the text is the wording "every rule that **mentions** the field" — it must be
dataflow-reachability (including computed-field derivation), or an edit to `Price` slips past
`rule Total <= Budget`. Fix the wording.

**Uniqueness.** The *techniques* are established prior art — inductive invariant preservation is
TLA+/Event-B/refinement-types + Floyd–Hoare; the certificate + small checker is proof-carrying
code (Necula & Lee, cited correctly) + LCF-kernel discipline. **That is a strength, not a
weakness** — it means the hard parts are known-tractable for bounded, loop-free, few-variable
problems, which is exactly Precept's shape. The *novel* thing is the **packaging**: formal
invariant proof + fault-freedom + graph soundness, unified under one constraint mechanism,
emitted as author-legible certificates, gated at load, for a DSL whose **primary author is a
domain expert, not a formal-methods person**, with diagnostics that *synthesize the missing
premise* instead of demanding the author know weakest-precondition calculus. Nobody ships that
combination. Keep the honesty my v2 §6 insisted on, though: a dedicated prover out-proves
Precept on a shared invariant *by design* — Precept trades deductive generality for an
integrated, legible, no-PhD-required package. `what-i-want` currently omits that concession; add
one sentence.

**Value.** High. This is the headline differentiator — compile-time prevention with a
re-checkable certificate — stated more sharply than any prior artifact. It is the thing that
makes Precept a category rather than "a better validator that also governs."

**Philosophy alignment.** Strong, with two flags:

- **Self-flagged, and I concur — the Fire sentence** (`philosophy.md:19`: *"evaluates all
  applicable constraints against the resulting configuration, and commits only if every
  constraint holds"*). Under `what-i-want` the runtime does **not** blanket-re-evaluate proven
  rules post-mutation. **Escalate for owner-approved rewording** — but *surgically*: Fire still
  evaluates ingress premises (args + editable-field rules against the resulting config); what
  changes is that it does not re-check *proven* obligations. The rewrite is "Fire enforces
  ingress premises and relies on discharged obligations," not "Fire checks nothing."
- **NOT self-flagged, and it must be — the composition seam** (`philosophy.md:55`: *"the runtime
  enforcement is what makes that carried constraint true, not a second line of defense"*). That
  sentence describes **one external value carrying its own constraint** for a downstream proof.
  `what-i-want`'s editable-ingress evaluates **relational rules over stored state** (`Floor <=
  Ceiling` reads `Ceiling`) — which :55 does not describe. This is the same seam the forensic
  doc said the philosophy *"does not cover"*. **Escalate:** either :55 is widened to cover
  ingress-triggered evaluation of affected invariants against the resulting configuration, or
  `what-i-want`'s editable-ingress mechanism is re-described so it does not lean on :55. Do not
  let it ride as if :55 already blesses it.

*(Overflow, philosophy.md:53, is not a philosophy conflict — `what-i-want` agrees with the
philosophy. It is the F10 build-state honesty gap: carry the park as the one disclosed
carve-out.)*

### Self-consistency with my prior positions — stated, not drifted

The task rightly demands I not silently contradict myself. Two places I move:

1. **The escape hatch.** My 07-11 position (`frank-prove-or-reject-position-2026-07-11.md` §7)
   held an author-visible **opt-in governance construct** as my *sole exit* and the honest form
   of GOVERN. `what-i-want` closes that exit. **I already abandoned it by v2** (07-14 §9:
   *"CLOSED — no escape hatch,"* accepting band-suppression as a known cost). So `what-i-want`
   is consistent with my *current* position, not my 07-11 one. No new contradiction — but the
   band-suppression cost I accepted *on the record in v2* is missing from `what-i-want`, and it
   must be restored (F4/F9).
2. **The post-mutation sweep — I reverse v2 §5.** My v2 (`frank-go-forward-posture-replay-2026-
   07-14-v2.md` §4, §5) adopted the sweep as **Mechanism 2 of governance** — a co-equal runtime
   enforcement point for relational invariants over independently-set fields. `what-i-want`
   deletes it, and **I now agree with the deletion.** Two reasons, both legitimate under my own
   rules: (a) *I learned something new* — the third-arm forensic (2026-07-15, written **after**
   my v2) showed the sweep-as-governance rests on §3A.4:1969, a sentence committed 11 minutes
   after its own negation (§0.7) and ridden in on a *Restore* commit with **no four-leg
   rationale** — my v2 was faithful to a contested half of canon, not to a ruled decision; and
   (b) *the ground changed* — `what-i-want` supplies the machinery (inductive proof for handler
   writes, editable-ingress premise for edits) that makes the §0.7 side *complete* rather than a
   deletion that strands `Floor <= Ceiling`. v2 adopted the sweep because it was the only home
   the corpus offered for relational invariants; `what-i-want` builds a better home. I withdraw
   v2 §5's Mechanism 2. Relational invariants are **proven or ingress-checked, not swept.**

   *What I do **not** withdraw from v2:* the stopping-rule insistence (§8) and the retrospective's
   core lesson (nothing said *stop*). Those apply to `what-i-want` **more** forcefully, because
   `what-i-want` enlarges the compile-time bet. The certificate criterion clause-4, the §6
   author-states-it valve, and an explicit "reject/shrink the surface, never strengthen-forever"
   rule are the difference between this being buildable and this being June again.

### Recommendation: REWORK-then-GO (conditional go)

Not no-go. The spine is right, it is owner-authored ground truth, it rules both forensic forks,
and it is the first artifact that *can* end the argument. But it is not the argument-ending
document until four things are fixed. In priority order:

1. **Draw the buildability scope line, and state the stopping rule.** Linear/decidable
   relational invariants proven inductively; nonlinear → **reject** with a teachable message +
   §6 author-states-the-rule; overflow parked (disclosed carve-out); quantified-predicate
   preservation scoped (universal-under-add proven; existential-under-removal reject-or-author-
   guard). Say explicitly: *the response to an unprovable case is reject-or-shrink-surface,
   never strengthen-the-engine-forever.* Without this sentence, this is the effort axis again.
   **Update (07-16 amendment): the non-linear sub-clause of this fix is now satisfied.** The
   worked `RepaymentCapPercent` case, its restating guards on `PlanRepayment` and `ReduceLimit`,
   the unguarded-rejection mutation row, and the scope-line paragraph draw exactly this line for
   the non-linear case (see §2 M1 and §3 — verdict: closed). What remains of fix 1 is the
   **general** stopping-rule sentence and the **quantified-predicate** scoping; those are not yet
   lined the way the non-linear case now is.
2. **Reconcile with committed canon before promoting.** Delete §3A.4:1969's sweep-as-second-
   enforcement-point; re-edit `compiler-and-runtime-design.md` §1/§1.1 and
   `soundness-and-coverage.md` §1.1 to replace sweep-governance-of-relational-invariants with
   inductive-proof + editable-ingress-premise, and resolve the `KNOWN SELF-CONTRADICTION`
   (provenance vs proof-carrying wording) in one direction. `what-i-want` must not land on top
   of a self-contradicting canon.
3. **Restore the two honesty statements.** Carry the two-sided overflow park (F10) as the one
   disclosed carve-out; name the band-suppression / constraint-deletion cost as an accepted
   tradeoff (F4/F9), as v2 §9 did.
4. **Escalate both philosophy flags** — the self-flagged Fire sentence (surgical rewording) and
   the **un-flagged composition-seam :55** — for owner deliberation. Do not auto-sync either;
   philosophy is owner-gated.

Minor, bundle into (1)–(3): fix "mentions the field" → dataflow-reachability; tighten "faults
(…out-of-range…)" so it does not re-quote the fossil ground-(i); add the one-sentence Event-B
honesty concession.

Do those four, extend the obligation-cell matrix in `soundness-and-coverage.md` §3, and you have
the canonical document that ends six weeks and gives the follow-on feasibility study something
it can gap-analyze against cell by cell. Leave them undone and promote as-is, and the next
draft will be "faithful to the corpus" while contradicting it — which is the entire disease.

The posture is right. The bet is bigger than the document admits. Name the bet, hold the line,
reconcile the canon, and ship it.

— Frank
