---
status: Cited
authored: 2026-06-03
author: research (lifecycle-1)
topic: Can static collection count/cardinality-bound checking be sound AND precise in forward abstract interpretation — especially for dedup sets where an add is a membership-dependent +0-or-+1 delta — and how do real verification systems reason about |s ∪ {x}|?
external-engagement: strong
---

# Count / cardinality-bound proof for collections: sound-and-precise checking, and the dedup-set `+0-or-+1` problem

> Precept's count-containment proof (`mincount` / `maxcount`) tracks a per-collection count interval through an action chain and rejects a mutation only when the post-mutation interval *provably* leaves the band (§0.6 item 2, "proven violations only"). The hard case is a **dedup collection** (set / lookup / map): an `add x` is a possible no-op, so its effect on `|s|` is `+0 or +1`, not a deterministic `+1`. This survey asks how real systems reason about set/multiset cardinality, and what that implies for Precept's count-delta model.

## Background

The current implementation (tracked as BUG-018) models **every** grow as a deterministic
`+1` on **both** interval bounds. A probe confirmed this **over-rejects**: `add x; add x`
into a `maxcount 1` set raises the lower bound to 2 and emits a false `CountBoundViolation`
(PRE0136), even though the two adds might add the same element and leave `|s| = 1`.

`docs/compiler/proof-engine.md` (Strategy 10, "Count Containment Proof", lines 1707–1719)
documents the `+1`/`−1` model and claims it is "faithful to the §0.6-item-1 soundness
posture — the compiler never over-rejects a mutation that *might* be in-bounds." For a
**dedup set** that claim is false: modelling a set-add as a lower-bound `+1` over-rejects
the `add x; add x` repro. The claim holds for multiset/list/queue/stack/log adds (which
*are* deterministic `+1`) but not for set/lookup adds.

Two adjacent Precept research surveys established the numeric machinery this builds on, and
**neither covers collection cardinality** (verified — grep for `cardinality`/`set`/`maxcount`
returns only octagon/DBM and numeric-interval content):

- [`proof-engine-interval-arithmetic-survey.md`](proof-engine-interval-arithmetic-survey.md) — numeric interval domains (Frama-C EVA, InferBO, GNATprove interval pass). The interval representation Precept's count tracking reuses.
- [`relational-constraint-representation-survey.md`](relational-constraint-representation-survey.md) — octagons / DBMs for **field-to-field** numeric relations; maps §0.4 "bounded relational closure" to octagon strong closure.

This survey builds forward: the missing piece is how a *collection's size* is abstracted and
how a membership-dependent mutation is handled.

### Spec-first check (Step 1b)

Grepping the canonical docs (`precept-language-spec.md` §0.6/§0.7, §5; `proof-engine.md`
Strategy 10; `collection-types.md`) shows the spec **documents the `±1` count model and the
proven-violations-only posture, but does not lock a decision on the dedup-set delta** — it
asserts soundness for the model without distinguishing set from multiset. The §0.6 *posture*
("soundness over completeness", "proven violations only") is locked; the *application of that
posture to dedup-set adds* is an implementation behavior (BUG-018) that contradicts the
posture. This is therefore a legitimate research case grounding the BUG-018 fix, not a
re-litigation of a settled spec decision. The relevant locked anchors:

> **§0.6 Proof philosophy, item 1 — Soundness over completeness.** "The proof layer must
> never claim an expression is safe when it is not. … False negatives (missed proofs) cause
> author friction … False positives (wrong 'safe' claims) cause runtime failures. The
> language always chooses the safe direction."
> — `precept-language-spec.md` §0.6 (line 221)

> **§0.6 Proof philosophy, item 2 — Proven violations only.** "The language reports what is
> definitively broken, not what might be broken. … Flagging only proven violations makes it
> a trusted guide — when it speaks, it is right."
> — `precept-language-spec.md` §0.6 (line 223)

> **§0.7 Governance.** "Every declared constraint is enforced on every value entering the
> entity from outside the definition … Because mutations execute on a working copy that is
> discarded if any constraint fails, an invalid configuration never persists."
> — `precept-language-spec.md` §0.7 (line 266)

The §0.7 fact is decisive for the *stakes*: count bounds are **governed at runtime** (the
post-mutation sweep, `proof-engine.md`-adjacent §3A.4 / spec line 1969, always rejects an
over-add). So the compile-time count check is a **best-effort catch of *provable* authoring
errors**, never the safety mechanism. The §0.6 posture (proven-violations-only) and the §0.7
backstop together push toward a **sound-but-incomplete** compile-time check.

## Methodology

- **Research question.** Can static count/cardinality-bound checking be made *sound* (never
  over-rejects an in-bounds program) AND *precise* (catches provable `maxcount` overflow /
  `mincount` underflow) in a forward abstract-interpretation setting — given that a dedup-set
  add is a membership-dependent `+0-or-+1`? How do real systems reason about `|s ∪ {x}|`?

- **Search strategy.** (a) Canonical Precept docs (`precept-language-spec.md`,
  `proof-engine.md`) grepped for the locked posture and current model. (b) Prior Precept
  research in `research/architecture/compiler/` confirmed not to cover cardinality. (c)
  External: the Dafny verifier's Boogie axiom prelude (`DafnyPrelude.bpl`); the Dafny
  Reference Manual; SPARK formal-container specs (`AdaCore/SPARKlib`); the Liquid Haskell
  set tutorial; the SMT finite-sets-with-cardinality decision-procedure literature
  (Bansal/Barrett/Reynolds/Tinelli); the sized-types abstract-interpretation literature
  (Serrano/Lopez-Garcia/Hermenegildo); TLA+/Alloy cardinality (to classify the
  model-checking-vs-AI difference). Venues: arXiv, LMCS, TPLP, dafny.org, adacore.com,
  ucsd-progsys.github.io, raw GitHub source.

- **Inclusion criteria.** Systems that (a) statically reason about collection size, and (b)
  either handle or deliberately punt on the set-add membership-dependent delta. **Excluded:**
  general container *shape*/aliasing analyses with no size abstraction; runtime-only
  enforcement; pure type-soundness work with no cardinality reasoning.

- **Source-grade mix.** Primary: Dafny prelude axioms, Dafny RM, SPARK formal-container
  `.ads` specs, the Bansal et al. LMCS 2018 paper, the Serrano et al. TPLP 2014 paper.
  Secondary: AdaCore rationale blog, Liquid Haskell tutorial, Alloy/TLA+ docs. Tertiary:
  none load-bearing.

- **Time bounds.** External fetches 2026-06-03. Boogie prelude / SPARK specs are pinned to
  `master` at fetch time; mirrored to `research/references/count-cardinality-proof/`.

## Findings

### The mathematical core, in one line

For a **set**: `|s ∪ {x}| = |s| + (if x ∈ s then 0 else 1)`. The delta is **membership-
dependent**. For a **multiset/list/sequence**: `|m ⊎ {x}| = |m| + 1`, **unconditionally**.
Every system below either (a) encodes the conditional precisely (membership-aware), (b)
requires the caller to discharge membership as a precondition, or (c) abstracts size as a
numeric interval and accepts incompleteness on the set-add case.

### Comparator table

| System | Tracks size statically? | Count-only or membership-aware? | How it handles set-`add` `|s ∪ {x}|` | Sound / complete | Annotation / cost burden |
|---|---|---|---|---|---|
| **Dafny** (Boogie/Z3 backend) | Yes — `|s|`, `|m|`, `|seq|` first-class | **Membership-aware** for sets | Two axioms guarded on `a[x]`: `a[x] ⇒ card = card(a)`; `!a[x] ⇒ card = card(a)+1`. Multiset add is unconditional `+1`. | Sound + (relatively) complete via SMT | Heavy: discharges via Z3; opaque proof witness |
| **SMT finite-sets+cardinality** (Bansal/Barrett/Reynolds/Tinelli, cvc) | Yes — `card` as a theory symbol | **Membership-aware**; the calculus *is* about set overlap | A dedicated DPLL(T) calculus: "Cardinality reasoning involves tracking how different sets overlap"; incrementally materializes Venn regions only as needed | Sound + decidable (the fragment), but high worst-case complexity | None for the user; cost is in the solver |
| **SPARK / Ada formal containers** (GNATprove) | Yes — `Length`, `Capacity` (ghost model) | **Membership-aware via contracts**, not inference | Set `Insert` precondition `Length < Capacity OR Contains(x)`; `Contract_Cases`: `Contains ⇒ not Inserted, Length unchanged`; else `Length+1`. Membership is a **discharged precondition**, never forward-inferred. | Sound + complete *given* the caller proves the precondition | Caller must establish membership/capacity at each call site |
| **Liquid Haskell** (refinement types over `Data.Set`) | **No size** — membership/subset only | Membership-aware, **but no cardinality measure** | Lifts `member`/`union`/`intersection`/`difference` into SMT set theory; reasons about *contents*, not `|s|`. No `|s|` measure exists. | Sound; cardinality simply not expressed | Refinement annotations; set theory via SMT |
| **Sized types / AI resource analysis** (Serrano/Lopez-Garcia/Hermenegildo, CiaoPP) | Yes — size as **lower+upper numeric bounds**, inferred by abstract interpretation | Count-only (size as interval); **not** membership-aware for dedup | "lower and upper bounds on the size of a set of terms … within the abstract interpretation framework." Size is a numeric abstraction; a membership-dependent op widens the interval. | Sound; incomplete (trivial/wide bounds for hard cases) | Fully automatic; no annotations |
| **TLA+ / Alloy** (`Cardinality`, `#`) | Yes — exact cardinality | Membership-aware (set semantics) | Exact: enumerated over a **bounded finite scope** by a model checker — *not* forward abstract interpretation | Sound+complete **within scope only** (bounded-exhaustive) | Define a finite scope; not a static AI domain |

### Detail: Dafny — membership-conditional axioms (the strongest direct precedent)

Dafny's verifier axiomatizes set cardinality with **two axioms guarded on membership**
[Primary; access 2026-06-03; mirrored to
`research/references/count-cardinality-proof/dafny-prelude-set-cardinality-axioms.md`]:

> ```boogie
> axiom (forall<T> a: Set T, x: T :: { Set#Card(Set#UnionOne(a, x)) }
>   a[x] ==> Set#Card(Set#UnionOne(a, x)) == Set#Card(a));
> axiom (forall<T> a: Set T, x: T :: { Set#Card(Set#UnionOne(a, x)) }
>   !a[x] ==> Set#Card(Set#UnionOne(a, x)) == Set#Card(a) + 1);
> ```
> — `DafnyPrelude.bpl` (`project-everest/vale` `tools/Dafny/DafnyPrelude.bpl`,
> fetched 2026-06-03)

`Set#UnionOne(a, x)` is "add one element". The cardinality rises `+1` **only** when
`!a[x]` (the element is not already present); when `a[x]`, it is unchanged. The multiset
analogue is unconditional:

> ```boogie
> axiom (forall<T> a: MultiSet T, x: T :: { MultiSet#Card(MultiSet#UnionOne(a, x)) }
>   MultiSet#Card(MultiSet#UnionOne(a, x)) == MultiSet#Card(a) + 1);
> ```

This is exactly the **set-vs-multiset asymmetry** Precept's per-collection-kind determinism
mirrors. The set union cardinality uses inclusion–exclusion (`|a∪b| + |a∩b| = |a| + |b|`),
i.e. the overlap is the correction term. Dafny's RM confirms `|s|` is first-class (§9.26
"Cardinality Expressions") — the older OnlineTutorial claim that "There is no way to get the
cardinality of a set" is version-specific and outdated. **Cost:** Dafny discharges these
through Z3, an opaque solver — which §0.6 item 3 ("Opaque solvers are rejected on
principle") **rules out for Precept**. The *axiom shape* transfers; the *discharge
mechanism* does not.

### Detail: SMT finite-sets-with-cardinality — why the combination is hard

Bansal, Barrett, Reynolds, Tinelli, *Reasoning with Finite Sets and Cardinality Constraints
in SMT*, **Logical Methods in Computer Science 14(4:12), 2018** [Primary; access 2026-06-03;
mirrored to `…/bansal-finite-sets-cardinality-smt-lmcs2018.txt`]:

> "We develop a calculus describing a modular combination of a procedure for reasoning about
> membership constraints with a procedure for reasoning about cardinality constraints.
> Cardinality reasoning involves tracking how different sets overlap."
> — Abstract (LMCS 2018, p. 1)

The paper's core message is that **membership and cardinality are two separate procedures
that must be combined**, and the combination is the hard part — precisely the
`|s ∪ {x}| = |s| + (if x∈s …)` coupling. Naively reducing membership to cardinality
(via Venn regions) explodes:

> "in a reduction to BAPA, membership reasoning is reduced to reasoning about cardinalities
> of different sets. … the algorithm in [SSK11] will reduce the problem to an arithmetic
> problem involving variables for 2^101 Venn regions derived from S1, S2, …, S100, and the
> singleton set introduced for x. … reasoning about the cardinalities of Venn regions is the
> main bottleneck for this fragment."
> — §1 Introduction (LMCS 2018, pp. 2–3)

> "While this problem is decidable, it has high worst-case time complexity."
> — §3 (p. ~7)

**Reading for Precept:** a *general* membership-aware cardinality domain is decidable but
expensive and Venn-region-combinatorial in the worst case — and is exactly the SMT/opaque-
solver shape §0.6 forbids. A **bounded** membership-tracking layer (only literal members
known *within a single transition*) sidesteps the combinatorics: there is no general
overlap to track, only a finite set of known-distinct literals in the current action chain.

### Detail: SPARK formal containers — membership as a discharged precondition

SPARK's bounded formal containers prove no-overflow not by forward-inferring the post-count
but by a **caller-discharged precondition** [Primary; access 2026-06-03; mirrored to
`…/spark-formal-containers-cardinality-contracts.md`]:

> ```ada
> --  bounded list Append:
> Pre  => (SPARKlib_Defensive => Length (Container) < Container.Capacity),
> Post => Length (Container) = Length (Container)'Old + 1,
> ```
> — `spark-containers-formal-doubly_linked_lists.ads`

For a **set**, where the duplicate add is a no-op, the `Insert` contract becomes
membership-conditional:

> ```ada
> --  formal hashed set Insert (with Inserted flag):
> Pre  => (SPARKlib_Defensive => Length (Container) < Container.Capacity
>                                or Contains (Container, New_Item))
> --  Contract_Cases: Contains ⇒ not Inserted and Length unchanged
> --                  others   ⇒ Inserted and Length = Length'Old + 1
> ```
> — `spark-containers-formal-hashed_sets.ads`

The set-add `+0-or-+1` is resolved by **requiring membership status at the call site**
(the `Contains` precondition / `Contract_Cases` split + an `Inserted` ghost flag). The
capacity precondition is *relaxed* to `Length < Capacity OR Contains` precisely because a
duplicate add cannot overflow. The analyzer never forward-infers `|s ∪ {x}|` from unknown
membership — it is always a discharged precondition. This is the "membership precondition /
ghost state" option made concrete by a shipping prevention-grade verifier.

### Detail: Liquid Haskell — membership without cardinality

Liquid Haskell lifts `Data.Set` operators into SMT set theory but has **no cardinality
measure** [Secondary; access 2026-06-03]:

> "The above operators are interpreted by the SMT solver. … LiquidHaskell reasons about the
> actual *contents* of data structures, via the theory of sets" — tracking which elements
> belong, **not** how many.
> — *Programming with Refinement Types*, Ch. 8 "Measuring Sets"
> (ucsd-progsys.github.io/liquidhaskell-tutorial, accessed 2026-06-03)

Useful as a **negative datapoint**: a mature refinement system deliberately models set
*membership* and omits set *cardinality* — the two are separable, and a system can have one
without the other. (Precept's relationship is the inverse: it tracks count and is asking
whether to add a *bounded* membership layer.)

### Detail: Sized types — size as an inferred numeric interval (the AI precedent)

Serrano, Lopez-Garcia, Hermenegildo, *Resource Usage Analysis of Logic Programs via Abstract
Interpretation Using Sized Types*, **TPLP 2014** [Primary; access 2026-06-03; mirrored to
`…/serrano-sized-types-resource-analysis-tplp2014.txt`]:

> "Sized types are representations that incorporate structural (shape) information and allow
> expressing both lower and upper bounds on the size of a set of terms and their subterms …
> Our new resource analysis has been developed within the abstract interpretation framework,
> as an extension of the sized types abstract domain."
> — Abstract (TPLP 2014, p. 1)

This is the **direct AI precedent** for Precept's choice: abstract a collection's size as a
`[lower, upper]` numeric bound and propagate it through a forward analysis. It is
**count-only** (the size is a number, not a membership-aware structure); the paper notes the
honest consequence — without richer information, "the analysis [infers] trivial bounds for a
large class of programs" (§1). I.e. count-only size abstraction is **sound but incomplete**,
and the incompleteness shows up exactly where the size delta is data-dependent (the dedup
case). This validates that Precept's interval-of-count representation is a recognized AI
domain shape, and that its incompleteness on data-dependent deltas is the *expected* tradeoff
of staying count-only.

### Classification: TLA+ / Alloy are not forward AI

TLA+ `Cardinality(S)` and Alloy `#r` compute **exact** cardinality, but by **bounded
model-checking over a finite scope** — exhaustive enumeration of states/instances within a
declared bound, not a forward abstract-interpretation transfer function over an abstract
domain [Secondary; access 2026-06-03; alloytools.org, en.wikipedia.org/wiki/TLA+]. Alloy 6
docs: bounded model checking "is possible because the state space is finite thanks to scopes
on signatures." They are sound+complete **only within scope** and do not generalize. They are
therefore **not comparators for the forward-AI question** — they answer "does a counterexample
exist within N elements", not "is this mutation provably in-bounds for all inputs". Recorded
to mark the boundary of the comparator space, not as a candidate technique.

## Threats to Validity

- **Two PDFs were binary on first fetch** (Bansal LMCS, Serrano TPLP). Both were extracted
  locally via `pdftotext` and mirrored as text; quotes are from the extracted text. Page
  numbers for the Bansal complexity quote (§3) are approximate (PDF reflow) — the abstract
  and §1 quotes are exact. Mitigated by mirroring the full text.
- **SPARK `.ads` excerpts** were returned via WebFetch summarization of the raw `.ads`, not
  a byte-for-byte paste of the full contract; the `Contract_Cases` post is paraphrased in
  the comment lines. The load-bearing facts (precondition `Length < Capacity OR Contains`;
  membership-conditional `Length+1`) are quoted from the fetch and are the documented SPARK
  set semantics. A reviewer wanting byte-exactness should open the pinned `.ads`. Graded
  Primary because the source artifact is AdaCore's own spec; the *transcription* carries mild
  risk.
- **Dafny tutorial vs RM tension** (older "cardinality inaccessible" vs modern `|s|`): I
  resolved this by citing the RM §9.26 and the prelude axioms (which presuppose `Set#Card`),
  not the outdated tutorial. If the prelude vendored in `project-everest/vale` has drifted
  from upstream `dafny-lang/dafny`, the axiom *shape* (membership-guarded) is nonetheless the
  long-standing Dafny encoding.
- **Selection bias.** Comparators were chosen to span the design space (SMT-backed
  membership-aware; contract-based; refinement; AI sized-types; model-checking) rather than
  exhaustively. A membership-aware *abstract-interpretation* container domain that forward-
  infers exact set-cardinality without a solver would be the strongest possible counter-
  evidence to "count-only is the AI norm"; I did not find one (the closest, fluid-updates /
  Dillig, is a heap/array *content* analysis, not a cardinality-interval domain). Its absence
  is itself a finding but rests on not-finding rather than proof.
- **Recency.** All external sources fetched 2026-06-03; SPARK/Dafny pinned to `master` at
  fetch.

## Implications for Precept

1. **The current model is unsound for dedup sets, full stop.** Modelling a set-add as a
   lower-bound `+1` over-rejects `add x; add x` into `maxcount 1`. Every membership-aware
   system surveyed (Dafny, SMT-sets, SPARK) agrees the set-add lower-bound delta is `+0`,
   not `+1`. The §0.6 item-1 soundness claim in `proof-engine.md:1719` is contradicted for
   dedup kinds — this is the BUG-018 fix target.

2. **The set/multiset asymmetry is real and load-bearing.** Dafny's prelude encodes it
   directly (set add membership-guarded; multiset add unconditional `+1`). Precept already
   has the catalog metadata to distinguish these (`ActionMeta.Effect`, per-collection-kind);
   per-kind determinism (multiset/list/queue/stack/log = exact `+1`; set/lookup = upper`+1`,
   lower-unchanged) is precisely the Dafny split, minus the membership oracle.

3. **The §0.7 runtime backstop changes the bar.** Because count bounds are *governed* at
   runtime (ingress + post-mutation sweep, §0.7 / spec:1969), the compile-time check is
   best-effort. SPARK is the precedent for a prevention-grade system **deliberately** making
   the cardinality obligation a discharged precondition rather than a forward inference — and
   SPARK has no runtime backstop, so it *must* be complete-given-precondition. Precept's
   backstop means it can be **even more conservative** (sound-but-narrow) than SPARK without
   losing the guarantee: an over-add the compiler can't prove is caught at runtime. This is a
   genuine precedent for **best-effort/sound-but-incomplete cardinality checking because a
   runtime mechanism backstops it** — though it is Precept's *composition* (§0.7), not a
   single external system, that supplies it; SPARK supplies the "make it a precondition"
   half, and the sized-types AI work supplies the "count-only size interval is a recognized,
   intentionally-incomplete domain" half.

4. **A bounded membership layer is feasible without a solver.** The SMT literature's warning
   is about the *general* overlap problem (Venn-region blowup). Precept's transitions are
   straight-line, loop-free (§0.4), and a *single transition* sees only a finite set of
   literal element values. Tracking "these literal adds are pairwise-distinct ⇒ provably
   `+1` each; this re-add of the same literal ⇒ provably `+0`" within one chain is a bounded,
   legible, solver-free refinement — not the general decidable-but-expensive problem. It
   buys exactly the precision needed to *prove* `add 'a'; add 'b'` overflows `maxcount 1`
   while keeping `add x; add x` (unknown `x`) clean.

5. **The prior membership-scoping note argues for restraint, not against precision.**
   `proof-engine.md:1249` already declares "Membership facts across a shrink … are out of
   scope for this mechanism." That scoping supports **Option 1** (stay count-only) as the
   *sound floor*: it is the conservative posture Precept already adopted for the boolean
   non-empty model. But it does not *forbid* Option 2 — Option 2 only tracks **distinctness of
   known literals within a single transition**, never membership across a shrink. The two are
   compatible: Option 2 is a bounded sharpening on top of the Option 1 floor.

## Conclusions

### Conclusion 1 (primary) — Option 1 (per-collection-kind sound count-only deltas) is the correct floor; it must replace the current unconditional `+1` for dedup kinds.

- **Rationale.** It is the minimal change that restores §0.6 item-1 soundness: a dedup-set
  `add` raises only the **upper** bound (`+1`), leaves the **lower** bound unchanged
  (membership-unknown ⇒ could be a no-op); multiset/list/queue/stack/log `add` stays exact
  `+1` on both bounds; `clear` = `[0,0]`; a literal collection = `[n,n]`. Set-add overflow
  then remains provable only for the cases where the lower bound *is* known to rise — an add
  into a statically-empty collection, or distinct literals (the latter only once Option 2 is
  added). This is sound by construction and solver-free, and `§0.7` runtime governance
  backstops every set-add the compiler now (correctly) declines to reject.
- **Alternatives considered and rejected.** *(a) Keep unconditional `+1`* — rejected: unsound
  for dedup sets (the BUG-018 over-rejection); contradicts every membership-aware comparator.
  *(b) Membership-aware SMT domain (Dafny/cvc style)* — rejected: requires an opaque solver,
  which §0.6 item 3 forbids on principle ("opaque proof witnesses violate the inspectability
  commitment"), and the general problem is Venn-region-combinatorial (Bansal et al.).
  *(c) SPARK-style mandatory membership precondition on every set-add* — rejected as the
  *floor*: Precept's authors are domain experts, not provers; forcing a `Contains`-style
  precondition on every `add` is annotation burden the §0.7 backstop makes unnecessary. (It
  remains available as an opt-in sharpening, see Conclusion 2 / Option 2.)
- **Precedent.** Dafny's membership-guarded set-card axiom (`!a[x] ⇒ +1`, `a[x] ⇒ +0`) — the
  lower-bound-unchanged posture is the sound abstraction of "we don't know `a[x]`"; the
  multiset unconditional-`+1` axiom grounds the per-kind split. Serrano et al. (TPLP 2014)
  grounds count-as-interval as a recognized, intentionally-incomplete AI domain.
- **Tradeoff accepted.** **Incompleteness**: a dedup-set overflow built from
  unknown-membership adds (`add x; add x; … ` into `maxcount 1`) is *never* proven at compile
  time — only caught at runtime. This is the deliberate sound-but-incomplete posture §0.6
  item 2 + §0.7 license. Authors lose a compile-time error on a class of provable-at-runtime
  over-adds; they gain never seeing a false `CountBoundViolation`.

### Conclusion 2 (secondary) — Option 2 (bounded literal-distinctness tracking within a transition) is the worth-it precision sharpening, and is solver-free and legible.

- **Rationale.** It recovers the most common authoring-error overflow — distinct literal adds
  past the cap (`add 'a'; add 'b'` into `maxcount 1`) — and the dup no-op (`add 'a'; add 'a'`
  is provably `+1` total, clean) — by tracking only the **set of known-distinct literal
  members added so far in the current action chain**. No membership-across-shrink, no general
  overlap, no solver: within one loop-free transition, distinct literals are provably
  distinct, and a repeat literal is provably a no-op. This stays inside §0.6 item 3
  (legible, structured witness) and §0.4 (bounded, single-pass).
- **Alternatives considered and rejected.** *Full membership domain* — rejected per
  Conclusion 1(b). *Do nothing beyond Option 1* — viable (the §0.7 backstop holds), but
  leaves the single most likely authoring error (literal over-fill of a small `maxcount`)
  uncaught at compile time, which is the exact "provable authoring error" the check exists to
  catch.
- **Precedent.** SPARK's `Contract_Cases` split on `Contains` (membership-conditional
  `Length+1`) shows a prevention-grade verifier resolving the `+0-or-+1` by *knowing*
  membership; Precept's bounded version *derives* membership for literals it can see, rather
  than requiring the author to assert it. Bansal et al. grounds *why* this must stay bounded
  (the general overlap problem is the blowup).
- **Tradeoff accepted.** **Scope-limited precision**: only *literal* adds within *one*
  transition get the sharpening; adds of event-arg/field values, or distinctness across
  transitions, stay count-only (upper`+1`/lower-unchanged). Some real distinct-value
  overflows remain unprovable. Accepted because those are exactly the cases where the runtime
  backstop is the right enforcer, and chasing them would re-introduce the general membership
  problem.

**Recommended sequencing:** ship Conclusion 1 first (the soundness fix — it is the BUG-018
correction), then Conclusion 2 as a precision pass if sample evidence shows literal-overfill
authoring errors are common enough to warrant the compile-time catch.

## What would change this conclusion

- **If a membership-aware *abstract-interpretation* container domain (no solver, forward,
  loop-free) that proves exact set-cardinality deltas turns up** — i.e. a domain that does
  Option-2-grade reasoning over *arbitrary* (non-literal) values soundly and legibly — then
  Option 1's "count-only is the AI floor" framing weakens and Option 2 should generalize
  toward it.
- **If sample evidence shows dedup-set `maxcount` fields are rare or never overflowed by
  distinct literals**, Conclusion 2 (the literal-distinctness sharpening) is not worth its
  implementation cost — ship Option 1 alone and rely on §0.7.
- **If the §0.7 runtime governance of count bounds were ever weakened or removed** (so the
  compile-time check became the safety mechanism, not best-effort), the whole "sound-but-
  incomplete is fine" posture collapses and Precept would be forced toward the SPARK model
  (mandatory membership/capacity preconditions on every set-add) to stay sound *and* useful —
  a much heavier authoring burden.
- **If three or more prevention-grade systems are found that model an unknown-membership
  set-add as a lower-bound `+1`** (rather than `+0`), the soundness argument for Option 1's
  lower-bound-unchanged rule is wrong. (Every system surveyed does the opposite; this would
  require them to be misread.)

## Open Questions

- **Lookup/map cardinality vs set cardinality.** Precept's `lookup`/map adds are keyed; an
  `put k v` over an existing key `k` is a `+0` (overwrite), a new key is `+1` — the *same*
  membership-dependent delta as a set, but keyed on the *key* not the whole element. Option 2
  for maps tracks distinct literal *keys*. This survey treated set and lookup together; a
  build slice should confirm the key-distinctness tracking is the same mechanism.
- **`mincount` underflow symmetry.** The survey focused on `maxcount` overflow (the over-add).
  The `mincount` floor under a `remove`/`clear` is the mirror: a dedup-set `remove x` is also
  `−0-or-−1` (removing an absent element is a no-op). The same per-kind/lower-bound posture
  should apply symmetrically; not independently surveyed here.
- **Witness shape.** What does the legible proof witness for a *declined* set-add look like
  (the §0.6 item 6 attribution) — "not rejected: lower bound unproven because membership of
  `x` is unknown"? Out of scope for this survey; a design concern.

## Sources

- **Dafny `DafnyPrelude.bpl`** — Boogie axiom prelude for the Dafny verifier (set/multiset
  cardinality axioms). Vendored in `project-everest/vale` `tools/Dafny/DafnyPrelude.bpl`.
  **Primary.** Accessed 2026-06-03. Mirrored to
  `research/references/count-cardinality-proof/dafny-prelude-set-cardinality-axioms.md`.
- **Dafny Reference Manual** — `https://dafny.org/latest/DafnyRef/DafnyRef.html`, §9.26
  Cardinality Expressions, §5.5 Collection Types. **Primary.** Accessed 2026-06-03.
- **Kshitij Bansal, Clark Barrett, Andrew Reynolds, Cesare Tinelli — "Reasoning with Finite
  Sets and Cardinality Constraints in SMT."** *Logical Methods in Computer Science* 14(4:12),
  2018. DOI:10.23638/LMCS-14(4:12)2018. **Primary.** Accessed 2026-06-03 via
  `https://arxiv.org/pdf/1702.06259`. Mirrored to
  `…/bansal-finite-sets-cardinality-smt-lmcs2018.txt`.
- **A. Serrano, P. Lopez-Garcia, M. V. Hermenegildo — "Resource Usage Analysis of Logic
  Programs via Abstract Interpretation Using Sized Types."** *Theory and Practice of Logic
  Programming* (TPLP), 2014. arXiv:1405.4256. **Primary.** Accessed 2026-06-03. Mirrored to
  `…/serrano-sized-types-resource-analysis-tplp2014.txt`.
- **AdaCore SPARKlib formal container specs** — `spark-containers-formal-doubly_linked_lists.ads`,
  `spark-containers-formal-hashed_sets.ads` (`github.com/AdaCore/SPARKlib`, master).
  **Primary** (the annotated specs GNATprove proves against). Accessed 2026-06-03. Mirrored
  (excerpts + reading) to `…/spark-formal-containers-cardinality-contracts.md`.
- **SPARK 2014 Rationale: Formal Containers** — AdaCore blog,
  `adacore.com/blog/spark-2014-rationale-formal-containers`. **Secondary.** Accessed
  2026-06-03.
- **"Measuring Sets," in *Programming with Refinement Types* (Liquid Haskell tutorial),
  Ch. 8** — `ucsd-progsys.github.io/liquidhaskell-tutorial/Tutorial_08_Measure_Set.html`.
  **Secondary.** Accessed 2026-06-03.
- **Alloy 6 documentation** (`alloytools.org/alloy6.html`) and **TLA+** (Wikipedia /
  FiniteSets `Cardinality`) — for the bounded-model-checking-vs-AI classification.
  **Secondary.** Accessed 2026-06-03.

### Precept-internal anchors (not external sources)

- `docs/language/precept-language-spec.md` §0.6 (lines 221, 223), §0.7 (line 266), §5 count
  containment (line 1681), post-mutation sweep (line 1969).
- `docs/compiler/proof-engine.md` Strategy 10 "Count Containment Proof" (lines 1707–1719);
  membership-across-shrink scoping note (line 1249).
- `research/architecture/compiler/proof-engine-interval-arithmetic-survey.md`,
  `research/architecture/compiler/relational-constraint-representation-survey.md` — the
  numeric-interval and octagon foundations this builds forward from (confirmed to not cover
  cardinality).
