# Frank's Prove-or-Reject Position — Response to the Identity & Guarantees Thesis

**Status:** Draft position paper — Not Locked
**Author:** Frank (Lead/Architect & Language Designer)
**Date:** 2026-07-11
**Revised for legibility 2026-07-12** — argument and citations unchanged; coined terms defined/replaced, illustrative samples added; sample dispositions are the *proposed* design, not current-compiler output.
**Context:** Written at Shane's request as a full standalone articulation of the one live design disagreement flagged in Frank's review of `docs/Working/precept-identity-and-guarantees-thesis-2026-07-11.md` (§2.4(ii) / §6 Divergence 1): whether a computed value written into a field's declared bound that the compiler cannot prove stays in-bounds should be **Error (prove-or-reject)** — Frank's position — or **GOVERN** — the thesis's position.

---

Shane — here is my prove-or-reject position on Divergence 1, built out in full. Read it standalone; I assume you have the thesis and `philosophy.md` open but have not seen my compressed review.

## 1. The precise scenario in dispute

Strip everything else away. The disagreement is over **exactly one case**, and most of the thesis is not in it. Let me fence it precisely, because three-quarters of the apparent territory is uncontested and the contest lives in the remaining sliver.

**The one disputed case** is: **a value computed by the precept's own rules, flowing into a plain policy limit, where the compiler can neither prove it stays in-bounds nor prove it always breaks.** I will call this "the one disputed case" throughout, and reach for it by that plain name.

Three qualifiers, each load-bearing:

- **Computed by the definition's own rules** — the value is produced *inside* the precept, not supplied from outside. `set Balance = Deposits - Withdrawals`. The write-site is inside an operation plan, on the working copy, mid-evaluation. It is not an event argument, not a construction input, not a direct field edit.
- **A plain policy limit** — the target's bound (`min 0`, `max 100`) is a policy band that no safety proof relies on. It is not a divisor guard or an index cap. Contrast a bound the compiler actually *uses* to prove something safe — `mincount` proving a collection is non-empty so a `dequeue` cannot fail. Where the bound is that second, proof-carrying kind, we both keep prove-or-reject; that side is settled, and §6 of the thesis concedes it.
- **The compiler genuinely can't tell** — the proof engine can neither prove the computed result lands in-band nor prove it always violates the bound. The source interval is wider than the target band and the engine cannot close the gap. This is *undecidable-in-general* territory, not *unproven-so-far-because-the-engine-is-weak*.

To make the "plain policy limit vs. a bound the compiler relies on" distinction concrete — the two kinds of bound behave differently in today's compiler already:

```precept
# A bound the compiler RELIES ON (proof-carrying). The dequeue is only safe if
# the queue is non-empty; the guard establishes count > 0, and the compiler
# leans on that fact to prove the dequeue cannot fail. Drop the guard and today's
# compiler already rejects it (UnguardedCollectionMutation: "'Line' may be empty").
# A `mincount 1` on the field is the same story — an unguarded dequeue that could
# drop below it is rejected today (CountBoundViolation). Both sides keep this.
from Open on ServeNext when Line.count > 0
    -> dequeue Line into Serving
    -> no transition

# A PLAIN POLICY LIMIT (min 0). Nothing downstream relies on PartiesSeated ≥ 0
# for safety; it is a band the author declared as policy. This is the kind of
# bound the disputed case is about.
field PartiesSeated as integer default 0 nonnegative
```

Now the two things that are **not** in dispute, so the contest is clean:

**(a) Externally-supplied values entering a bound → GOVERN. Uncontested.** When an event arg or a field edit carries `min 5`, governance enforces it at the moment the value crosses the contract boundary (`philosophy.md` §0.7 "Governance — enforced at runtime on external input… every value entering the entity from outside the definition"; `precept-language-spec.md:268`). A bad external value is refused with `ConstraintsFailed`, the working copy is discarded, and the *caller* learns their input was rejected. This is governance's home turf and I have never contested it. It is the headline of the product.

**(b) Values the type checker can statically prove in-band → no runtime obligation at all. Uncontested.** If the engine proves `Deposits - Withdrawals` stays ≥ 0 given the governed input bounds, there is nothing to enforce and nothing to reject. Clean compile, no gate.

The three cases side by side, in one fragment (money's `nonnegative` is the `min 0` band):

```precept
# (a) External value entering a bound — uncontested GOVERN.
# The caller's Amount is checked at ingress; a negative value is refused
# with ConstraintsFailed before it ever enters the entity.
event Deposit(Amount as money in 'USD' nonnegative)

# (b) A value the compiler proves in-band — uncontested, no runtime gate.
# Deposits and Interest are both nonnegative, so their sum is provably ≥ 0.
field Deposits     as money in 'USD' default '0.00 USD' nonnegative
field Interest     as money in 'USD' default '0.00 USD' nonnegative
field TotalCredits as money in 'USD' default '0.00 USD' nonnegative
on Accrue -> set TotalCredits = Deposits + Interest

# The one disputed case — a definition-computed write into a plain policy limit
# the engine can neither prove in-band nor prove always-violating.
# Deposits − Withdrawals can go negative; the engine cannot show the result
# sits inside [0, +∞). No external party supplied Balance — there is no
# ingress slot at which governance could make the bound true.
field Withdrawals as money in 'USD' default '0.00 USD' nonnegative
field Balance     as money in 'USD' default '0.00 USD' nonnegative   # min 0 — a policy band, not a proof-carrying guard
on Settle -> set Balance = Deposits - Withdrawals
```

The one disputed case is the gap between (a) and (b): a value that is neither external (so governance's ingress mechanism never sees it) nor provable (so the compiler cannot certify it). The thesis routes that gap to GOVERN (§2.4(ii)). I hold Error — prove-or-reject (my ruling's position on this case, `proof-engine-boundary-ruling:184`).

## 2. The positive argument for Error

My claim in one sentence: **a definition-computed value with a bound the compiler can't decide has no external ingress to govern, and a failure with no external agent to refuse it is not a governed refusal — it is a fault wearing a governed refusal's costume.** Five steps, each grounded.

**Step 1 — Governance is, by the philosophy's own text, an *ingress* mechanism.** `philosophy.md` §0.7 scopes the governance leg precisely: "enforced at runtime on external input… every value entering the entity **from outside the definition** — event arguments, construction inputs, direct field edits — at the moment it enters." The mechanism is triggered *at a boundary crossing*. It exists to check values as they arrive from outside. That is not incidental phrasing; it is what makes the refusal meaningful (see Step 3).

**Step 2 — A computed value never crosses that boundary.** `set Balance = Deposits - Withdrawals` produces a value *inside* the plan, from inputs that were *already governed* at their own ingress. The result is not "entering from outside" — it is manufactured by the definition. There is no ingress slot at which governance "makes the carried constraint true." This is exactly why the **compiler→runtime hand-off** described at `philosophy.md:55` — "the runtime enforcement is what makes that carried constraint true" — and its operational restatement at `precept-language-spec.md:270` ("proven by the compiler, its precondition discharged by governance, nothing left to a runtime check") **do not close for a computed write.** That hand-off is engineered for external input: the compiler proves "the arg *carries* `min 5`," and the runtime *makes it true* at ingress when the value arrives. For a computed value there is no ingress at which the runtime can discharge the precondition. The compiler cannot lean on "the runtime will make it true," because there is no governance event to lean on. The hand-off has nothing to attach to.

**Step 3 — Therefore the only two honest dispositions are "prove it in-band" or "reject."** The definition can *either guarantee the computed result lands in-band (given its governed inputs) or it cannot*. If it can, the engine proves it and we are in uncontested case (b). If it cannot, the definition has a reachable internal state in which its own computation produces an out-of-band value, **and there is no external party who supplied that value and could supply a different one.** "Cannot prove" here is not a runtime event awaiting a concrete value — it is *an unmet proof the author still owes* (I will call this a proof debt): the definition has not shown that its own arithmetic stays inside the band it declared. `philosophy.md:57` says a clean compile has "no unproven evaluation faults"; a computed containment the compiler can't decide *is* an unproven fault. Principle 7 (`precept-language-spec.md`, "the compiler does not guess") forbids the alternative — routing it to a runtime refusal is the compiler guessing "probably fine, the runtime will sort it."

**Step 4 — The catalog confirms there is no "band" fault distinct from a rule fault; the fault is real either way.** `ConstraintKind.cs` enumerates exactly five members — `Invariant`, `StateResident`, `StateEntry`, `StateExit`, `EventPrecondition` — and every one names **where** a constraint is enforced (globally, in-state, on entry/exit, on an event). Not one names *band-vs-rule*. This is correct and it is decisive: `min 5` desugars to `rule Qty >= 5` (`precept-language-spec.md:1138`, "interchangeable to the proof engine"), so there is no separate "band" object with its own softer disposition. A computed write that violates `min 5` violates a rule, full stop. And in the fault registry, that violation has a home: `FaultCode.OutOfRange = 13`, carrying `[StaticallyPreventable(DiagnosticCode.OutOfRange)]` — a *statically preventable fault*, distinct from `NumericOverflow = 12` (value-vs-type-representable-range). The language's own metadata says: an out-of-band computed value is a fault the compiler is expected to prevent. GOVERN asks us to un-say that.

**Step 5 — The philosophy's process guarantees cut the same way.** `philosophy.md:49` promises "Business processes cannot get stuck… No workflow trapped in a state it can never leave," and §0.7's data guarantee promises "no window in between where an invalid combination can exist." A computed-write refusal at runtime means a *legitimately-authorized operation, invoked with valid external inputs that all passed ingress*, dead-ends — not because anyone did anything wrong, but because the definition's own computation has a range it never proved it stays inside. The `because` reason on that refusal ("Balance must be at least 0") is addressed to no one: the caller didn't set `Balance`. That is much closer to "a valid operation trapped" than to "an invalid configuration prevented." Prove-or-reject surfaces the unhandled case *at authoring time*, where the domain expert can answer the real question ("what happens on overdraft?"); GOVERN buries it until a runtime dead-end.

**The payoff is not hypothetical — it catches a bug that ships in the corpus today.** Take `samples/shopping-cart.precept`. It declares `rule DiscountPercent <= 100` (`:54`) and then accumulates promotions:

```precept
rule DiscountPercent <= 100 because "Combined promotion discount cannot exceed 100 percent"

# ApplyPromotion.DiscountPercent is validated `positive max 100` at ingress — each
# single promotion is a valid ≤100 value. But the handler ADDS it to the running
# total, and nothing bounds the sum:
from Cart on ApplyPromotion
    -> put CartPromotions ApplyPromotion.PromotionCode = ApplyPromotion.DiscountPercent
    -> set DiscountPercent = DiscountPercent + ApplyPromotion.DiscountPercent   # :205
    -> ...
```

Two individually-valid promotions — say 70% and 50%, each ≤ 100 at ingress — accumulate to 120 and breach `rule DiscountPercent <= 100`. This file **compiles clean today** (I verified: zero diagnostics), so the breach ships silently. Under prove-or-reject the accumulating write is rejected at authoring time, because the engine cannot prove the running sum stays ≤ 100 — forcing the author to answer the real question (cap the total, guard the promotion, or reject once it would exceed 100). This is exactly the class of bug the positive argument is about, sitting in the shipped corpus right now.

## 3. Answering the thesis's strongest counter-argument

The thesis's best move is what I'll call **its strongest counter — that the disposition attaches to the *obligation*, not the syntax** (§2.4(ii), §6 Divergence 1, bullet 1): proof-carrying is a property of the *obligation*, not the *construct* — "the boundary is proof-carrying vs. not, never band vs. not" — and a computed band violation the compiler can't decide "resolves to `ConstraintsFailed`, a recoverable governed refusal, which by the evaluator's own rule for classifying what happens when a check fails (`fault-floor-definition:122`) is governance's home turf, not a fault." It adds that my own substrate said so: `fault-floor-definition:210` instructs "REMOVE… OutOfRange (13)" from the statically-preventable registry, and `:177` routes the scale-back to governance "FOR NON-PROOF-CARRYING FIELDS ONLY." So the thesis says my ruling *contradicts its own substrate* and *assumes the conclusion* by calling this a "fault-prone operation."

I want to concede what's right in this before I say why it fails, because one leg genuinely is.

**First, the honest concession (see also §6 below).** I agree the boundary is proof-carrying-vs-not, not band-vs-not. My ruling's finding that the band-vs-rule split dissolves says exactly this — the "fragile split dissolves," all declared constraints are uniformly governed at ingress *and* contributed as premises. So I do not defend "a band is a special softer thing." Good. And I'll drop one weak leg of my ruling's severity call outright: I wrote that GOVERN fails partly because "there is no runtime band representation distinct from a rule to govern it with." The thesis is right that this is asymmetric — I called the same "no runtime representation" point a mere build-price when demoting the defer-unprovable-cases-to-the-runtime model. A band *is* a rule and rules *are* governed at ingress, so the rule-governance surface exists. That leg was wrong; I withdraw it. My position does not need it.

**Now why the reframe fails: it assumes the computed value reaches runtime as external-shaped input, and it doesn't.** The reframe's whole weight rests on the classification "band violation → `ConstraintsFailed` → governance." But `fault-floor-definition:122` defines a fault as "a mid-evaluation abort with **no recoverable typed outcome**… anything the working-copy discard **or ingress refusal** handles… is governance." The operative word is **recoverable**, and recoverability is not a property of the discard mechanism — it is a property of *whether an external agent can act on the refusal*.

For **external ingress**: bad arg → discard → the *caller* receives a typed `ConstraintsFailed` naming the violated rule → the caller supplies a different arg. Recoverable. Genuinely governance.

For a **computed write**: the external args were all valid and passed ingress; the definition then computed an out-of-band value from its own governed inputs → discard. Who receives the refusal? No one supplied `Balance`. There is no "different value" any external agent can offer. The operation simply cannot complete, and the reason names nothing the outside world can fix. That is *precisely* `:122`'s "mid-evaluation abort with no recoverable typed outcome" — the definition of a **fault**.

So the taxonomy the thesis invokes is the taxonomy that *defeats* it, once you apply its own word "recoverable" honestly. Working-copy discard is **necessary but not sufficient** for governance; it must *also* hand back a refusal an external agent can act on. External ingress does; a computed write does not. The thesis reads `:122` as "discard-handled ⇒ governance" and drops the "recoverable typed outcome" half of the same sentence. **That is where it assumes the conclusion** — it presumes the computed value arrives at a boundary where a refusal is meaningful, which is the very thing in dispute.

On the substrate contradiction (`:177`, `:210`): yes, the `fault-floor-definition` draft recommended exiting non-proof-carrying bands to governance, and my ruling reversed it. That reversal was deliberate, not an oversight. The substrate reached its recommendation by the same move the thesis makes — treating "working-copy discard" as sufficient for governance. My ruling applied `:122`'s own recoverability test and found computed writes fail it. The substrate conflated external and computed writes under one "discard" umbrella; the ruling separated them. A draft's recommendation is an argument, not authority — including my own draft's.

## 4. What GOVERN would actually require to be honest

This is the cost I flagged as "a band-governance surface that does not exist," and I want to be concrete, because it is easy to say "just govern it" and hard to say what that means in the pipeline.

Note first what *does* exist and is **not** the answer: the rule-governance surface at ingress. That surface checks `rule Qty >= 5` against **external transitions** — event args, edits, construction. It is real and it works. It is not the surface GOVERN needs here, because a computed write is not an external transition. So GOVERN for this case requires building something genuinely new:

**(1) A new enforcement lane: an intra-plan, post-computation constraint re-check.** After each *internal* assignment action, the runtime would have to re-evaluate the target field's constraints against the working copy and, on failure, produce a *typed, recoverable* outcome. None of `ConstraintKind`'s five members models this — they all model enforcement against state-transition/event positions, not "re-check a field's band against a value the plan just computed, mid-plan." So either `ConstraintKind` grows a sixth position genuinely unlike the other five (a *computed-write checkpoint*), or a parallel enforcement surface appears outside it. Either way it is new machinery, and per our own catalog rules a new enforcement position is a language-surface change, not an implementation detail.

**(2) The fault pipeline would have to be rebuilt to make the outcome recoverable rather than an abort.** Today the *only* runtime thing that catches an out-of-band computed value is **the runtime's last-resort abort** — a mid-evaluation abort, the fault floor's defensive backstop (`precept-language-spec.md:110`, "runtime fault traps exist only as defensive redundancy for paths the compiler has already proven unreachable"). So "GOVERN this at runtime" *today* resolves to **that last-resort abort = the fault.** To make it a genuine governed refusal you must: remove `OutOfRange` from the `[StaticallyPreventable]` registry (exactly `fault-floor-definition:210`), *and* invent a new recoverable-outcome type for computed-write refusal that is not the last-resort abort — and then answer the question the machinery cannot answer for you: **recoverable by whom?** A computed value has no supplier. The only honest answer is "recoverable by an author who explicitly opted this field into runtime governance" — which is not a runtime feature at all. It is a *language construct* (my ruling's own honest exit — the opt-in construct idea the ruling flagged for its own `/design`; see §7).

**(3) The proof hand-off has to be held for proof-carrying bounds simultaneously.** There is a point the adversarial review of the fault-floor draft insisted on (`fault-floor-definition:128`) that is unavoidable here: if a bound ever discharges a fault obligation, relocating its enforcement to a runtime sweep *severs the proof* — "a proof whose precondition governance does not actually make true is not a proof" (`philosophy.md:55`). This is why proof-carrying bounds must stay prove-or-reject even in the thesis's own scheme, and the thesis agrees. It means GOVERN can never be a blanket move; it must be a *surgical* one that carves proof-carrying bounds out — which in turn means the runtime needs machinery that tracks which later writes could invalidate a bound, machinery that today has **zero consumers** (`fault-floor-definition:144`).

So "GOVERN" honestly implemented is: a new intra-plan checkpoint, a new recoverable-outcome type distinct from the last-resort abort, a removal from the statically-preventable registry, write-aware discharge wiring, *and* an answer to "recoverable by whom" that bottoms out in a language construct. Slapping "GOVERNED" on the current behavior without building all of that ships the last-resort abort relabeled — the exact false-security the philosophy exists to forbid. GOVERN is not the cheap option here; it is the expensive one, and its cheapest honest form is a language change.

## 5. What Error actually costs — my own downside, stated honestly

Prove-or-reject is not free, and I will not pretend otherwise.

**The bound-invention cost is real.** The corpus shows 214 unbounded money fields against 10 bounded (`bounds-only:120-122`). If a computed value flows from an unbounded source into a bounded field, prove-or-reject forces the author to do one of: declare a bound on the source, declare the missing relationship as a rule (`rule Deposits >= Withdrawals`), guard the operation (`when …`), or route the out-of-band case to an explicit reject row. For a field where the author *knows* the value is fine but *cannot state a tight numeric bound they actually believe in*, this is "representability masquerading as policy" (`niche-packet:129`) — a bound invented to satisfy the prover, not to express a domain truth. That violates the honesty-about-approximation principle (Principle 8) in spirit: the author writes a `max` they don't mean.

**Practical authoring impact.** A domain expert who writes `set Balance = Deposits - Withdrawals` into `field Balance min 0` gets a *rejection* if the engine can't prove `Deposits >= Withdrawals`. They must now do more up-front work: state the relationship, guard the subtraction, or handle overdraft explicitly. For the author who "just wanted to compute a balance," that is friction, and at scale (214 unbounded money fields) it is not negligible friction. This is the strongest empirical point on the thesis's side and I credit it fully.

What the friction and the fix actually look like — the naive version, then three ways to satisfy the prover:

```precept
# Naive — REJECTED under prove-or-reject. The engine cannot prove
# Deposits − Withdrawals ≥ 0, so the write into Balance (min 0) leaves the
# compiler's duty to prove the value fits the field's bound unmet.
# (Today's compiler: compiles clean — this posture is not built yet.)
field Balance as money in 'USD' default '0.00 USD' nonnegative
on Settle -> set Balance = Deposits - Withdrawals
```

```precept
# Fix 1 — state the missing relationship as a rule the prover can lean on.
rule Deposits >= Withdrawals because "An account cannot withdraw more than it has deposited"
on Settle -> set Balance = Deposits - Withdrawals   # now provably ≥ 0 — clean compile

# Fix 2 — guard the operation so the subtraction only runs when it is safe.
from Open on Settle when Deposits >= Withdrawals
    -> set Balance = Deposits - Withdrawals
    -> no transition

# Fix 3 — answer the domain question: handle overdraft explicitly instead of
# leaving the case unproven.
from Open on Settle
    -> reject "Withdrawals exceed deposits — overdraft is not permitted on this account"
```

All three fixes compile clean today (I verified each) — the author has answered the question the naive form left open.

**Why I still hold, given the cost.** The friction is the cost of *confronting an unhandled case*, not the cost of a *spurious* rejection. If `Deposits - Withdrawals` can go negative and `Balance` cannot, the author has a genuine domain question — what happens on overdraft? — that they have not answered. Prove-or-reject makes them answer it at authoring time, in the source, where the answer is legible and permanent. GOVERN lets them not answer it, and converts the unanswered question into a runtime refusal addressed to no one. I would rather pay authoring friction that yields a complete definition than buy authoring convenience that ships an incomplete one. But I hold this as a *judgment about which cost is worth paying*, not as a proof that GOVERN's cost is zero — it isn't, and the corpus says so.

## 6. The one correction I already conceded — and why it doesn't resolve this

The thesis's "one sharpening on the ruling's own terms" (end of §6) is correct and I accept it: **decidability shapes which obligations *exist*, not merely how they discharge.** An undecidable constraint — say a field bounded only by a nonlinear rule — contributes *no* interval to the proof engine, so a computed write into that field generates *no* obligation to prove containment at all, and therefore is governed at ingress like any rule, not rejected. The precise one-line boundary is "computed values are proven against every **decidable** constraint," not "every constraint." I conceded this and I stand by the concession.

But it **does not touch the one disputed case**, and it is important to see exactly why, because the thesis itself admits "the fault floor proper is unaffected."

The concession is about cases where the *constraint* is undecidable (nonlinear rule → no interval → no obligation → governed). The one disputed case is the opposite: the constraint is **fully decidable** (a literal `min 0` / `max 100` extracts a clean interval `[0, +∞)` with certainty) but the computed **value** is one the compiler can't place (an unbounded source interval `[−∞, +∞]` that the engine cannot show sits inside `[0, +∞)`). There, the obligation *does* exist — the band is decidable, so the compiler's duty to prove the computed value fits the field's bound is created — and it is *undischarged*. My concession relocates the cases where *no obligation exists*; it says nothing about a case where the obligation plainly exists and cannot be met. The refinement sharpens the boundary's phrasing; it leaves the decidable-band-but-unplaceable-value case exactly where my ruling put it: prove or reject.

## 7. Bottom line — and what would move me

**My position:** For a definition-computed value flowing into a plain policy limit that is *decidable* but where the compiler can't place the value in-bounds, the disposition is **Error — prove-or-reject** (`proof-engine-boundary-ruling`, my ruling's position on this case). The computed value has no external ingress to govern; the compiler→runtime hand-off of `philosophy.md:55` cannot close for it; a runtime refusal of it satisfies the *discard* half of `fault-floor-definition:122` but fails the *recoverable typed outcome* half, which makes it a fault, not governance. "Cannot prove" is a proof debt — an unmet proof the author still owes — that must be discharged at authoring time, not a runtime event to defer.

**Am I immovable? No — and this is the honest steelman.** There is exactly one argument that moves me to GOVERN for this case, and it is the one I named in my own ruling as the sole exit ("the only place the verdict could move"): **an explicit, author-visible, opt-in language construct** — the opt-in construct idea the ruling flagged for its own `/design`. If the author can write, *in the source*, a visible token that says "this field is governed at runtime, not proven — I accept a runtime refusal here," then the computed write acquires the one thing it structurally lacks: **an explicit signal the author wrote in the source.** The refusal is no longer addressed to no one; it is addressed to the author who explicitly opted the field into runtime governance and told the language, in permanent legible source, "check this at runtime." That converts a *silent deferral* (which I reject) into a *declared authoring choice* (which I would accept). Under that construct, GOVERN for this case becomes honest, and I would ratify it.

To picture the *shape* of that opt-in exit — and only the shape:

```precept
# ILLUSTRATIVE ONLY — not implemented, not designed, not a keyword proposal, and
# deliberately NOT compiled: this is not real syntax.
# The exact surface (token, placement, spelling) is for /design to settle.
# This sketch shows only WHERE the authored intent would live: inline on the
# field, in permanent legible source — something in the shape of an opt-in that
# says "govern this bound at runtime; I accept a runtime refusal here rather
# than discharge it as a proof debt."
field Balance as money in 'USD' default '0.00 USD' nonnegative governed-at-runtime
on Settle -> set Balance = Deposits - Withdrawals   # a runtime refusal here is now addressed
                                                     # to the author who explicitly opted in
```

The token `governed-at-runtime` above is a placeholder standing in for *some* authored construct — do not read it as a proposed keyword. The point is structural: the trust becomes visible in the source, not inferred from a hover tag.

**What does *not* move me** is the thesis's actual proposed safeguard: ambient legibility — "render GOVERNED at the constraint" (§2.4, §6). That is a mitigation, and it may well be a good one, but it is a **UI promise about an undesigned surface** — the thesis's own Recommendation G routes the whole disposition surface to `/design` as an "untested hypothesis" with the false-security question as its *acceptance criterion, not answered*. I will not demote a compile-time guarantee (`precept-language-spec.md:266`, "no result outside a declared bound"; §0.6 item 6, `:208`) on the strength of a hover tag that is itself hypothetical. A hover tag is not load-bearing in the source; a language construct is. An author who writes `min 5` today reasonably reads it as compiler-enforced; ambient rendering *asks* them to read it differently, an explicit construct *makes* them write it differently. Only the second is a real safeguard.

So the fork for you, stated cleanly:

- **Ratify Error (my position):** computed writes into decidable plain policy limits the compiler can't place prove-or-reject; accept the bound-invention cost (§5) explicitly as a named tradeoff on unbounded sources.
- **Ratify GOVERN (the thesis):** but only honestly, which means funding the machinery in §4 *and*, to close the false-security hole, doing it through the explicit author-visible trust construct — not as a default, and not on the strength of ambient legibility alone.

I am not asking you to treat my ruling as binding — it is self-labeled "not locked," and agent labels don't bind you. I am asking you to see that the thesis's GOVERN, honestly built, converges on the *same* language construct I named as my own exit. We are closer than the "Frank vs. thesis" framing suggests. The real question is not "prove-or-reject vs. govern" — it is **"is a computed write the compiler can't place a proof debt the author discharges at authoring time, or a governance choice the author declares explicitly in source?"** I say proof debt by default, governance only by explicit authored opt-in. That is the sentence I'd like you to rule on.
