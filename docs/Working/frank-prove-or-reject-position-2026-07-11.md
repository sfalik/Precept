# Frank's Prove-or-Reject Position — Response to the Identity & Guarantees Thesis

**Status:** Draft position paper — Not Locked
**Author:** Frank (Lead/Architect & Language Designer)
**Date:** 2026-07-11
**Context:** Written at Shane's request as a full standalone articulation of the one live design disagreement flagged in Frank's review of `docs/Working/precept-identity-and-guarantees-thesis-2026-07-11.md` (§2.4(ii) / §6 Divergence 1): whether a merely-unprovable, definition-derived write into a non-proof-carrying declared bound should be **Error (prove-or-reject)** — Frank's position — or **GOVERN** — the thesis's position.

---

Shane — here is my prove-or-reject position on Divergence 1, built out in full. Read it standalone; I assume you have the thesis and `philosophy.md` open but have not seen my compressed review.

## 1. The precise scenario in dispute

Strip everything else away. The disagreement is over **exactly one cell**, and most of the thesis is not in it. Let me fence it precisely, because three-quarters of the apparent territory is uncontested and the contest lives in the remaining sliver.

The cell is: **a definition-derived value, flowing into a non-proof-carrying declared bound, where containment is *merely unprovable*.**

Three qualifiers, each load-bearing:

- **Definition-derived** — the value is *computed by the precept's own rules*, not supplied from outside. `set Balance = Deposits - Withdrawals`. The write-site is inside an operation plan, on the working copy, mid-evaluation. It is not an event argument, not a construction input, not a direct field edit.
- **Non-proof-carrying bound** — the target's bound (`min 0`, `max 100`) discharges no downstream fault obligation. It is a policy band, not a divisor guard or an index cap. (Where the bound *is* proof-carrying — `mincount` discharging a `count>0` access-safety obligation — we both keep prove-or-reject. That side is settled; §6 of the thesis concedes it.)
- **Merely unprovable** — the proof engine can neither prove the derived result in-band nor prove it always-violating. The source interval is wider than the target band and the engine cannot close the gap. This is *undecidable-in-general* territory, not *unproven-so-far-because-the-engine-is-weak*.

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

# The disputed cell — a definition-derived write into a non-proof-carrying
# bound the engine can neither prove in-band nor prove always-violating.
# Deposits − Withdrawals can go negative; the engine cannot show the result
# sits inside [0, +∞). No external party supplied Balance — there is no
# ingress slot at which governance could make the bound true.
field Withdrawals as money in 'USD' default '0.00 USD' nonnegative
field Balance     as money in 'USD' default '0.00 USD' nonnegative   # min 0 — a policy band, not a proof-carrying guard
on Settle -> set Balance = Deposits - Withdrawals
```

The disputed cell is the gap between (a) and (b): a value that is neither external (so governance's ingress mechanism never sees it) nor provable (so the compiler cannot certify it). The thesis routes that gap to GOVERN (§2.4(ii)). I hold Error — prove-or-reject (my ruling Q7, `proof-engine-boundary-ruling:184`).

## 2. The positive argument for Error

My claim in one sentence: **a definition-derived value with an undecidable bound has no external ingress to govern, and a failure with no external agent to refuse it is not a governed refusal — it is a fault wearing a governed refusal's costume.** Five steps, each grounded.

**Step 1 — Governance is, by the philosophy's own text, an *ingress* mechanism.** `philosophy.md` §0.7 scopes the governance leg precisely: "enforced at runtime on external input… every value entering the entity **from outside the definition** — event arguments, construction inputs, direct field edits — at the moment it enters." The mechanism is triggered *at a boundary crossing*. It exists to check values as they arrive from outside. That is not incidental phrasing; it is what makes the refusal meaningful (see Step 3).

**Step 2 — A derived value never crosses that boundary.** `set Balance = Deposits - Withdrawals` produces a value *inside* the plan, from inputs that were *already governed* at their own ingress. The result is not "entering from outside" — it is manufactured by the definition. There is no ingress slot at which governance "makes the carried constraint true." This is exactly why the composition seam of `philosophy.md:55` — "the runtime enforcement is what makes that carried constraint true" — and its operational restatement at `precept-language-spec.md:270` ("proven by the compiler, its precondition discharged by governance, nothing left to a runtime check") **do not close for a derived write.** The seam is engineered for external input: compiler proves "the arg *carries* `min 5`," governance makes it true at ingress. For a derived value there is no ingress at which governance can discharge the precondition. The compiler cannot lean on "governance will make it true," because there is no governance event to lean on. The seam has nothing to attach to.

**Step 3 — Therefore the only two honest dispositions are "prove it in-band" or "reject."** The definition can *either guarantee the derived result lands in-band (given its governed inputs) or it cannot*. If it can, the engine proves it and we are in uncontested case (b). If it cannot, the definition has a reachable internal state in which its own computation produces an out-of-band value, **and there is no external party who supplied that value and could supply a different one.** "Cannot prove" here is not a runtime event awaiting a concrete value — it is a *proof debt*: the definition has not shown that its own arithmetic stays inside the band it declared. `philosophy.md:57` says a clean compile has "no unproven evaluation faults"; a merely-unprovable derived containment *is* an unproven fault. Principle 7 (`precept-language-spec.md`, "the compiler does not guess") forbids the alternative — routing it to a runtime refusal is the compiler guessing "probably fine, the runtime will sort it."

**Step 4 — The catalog confirms there is no "band" fault distinct from a rule fault; the fault is real either way.** `ConstraintKind.cs` enumerates exactly five members — `Invariant`, `StateResident`, `StateEntry`, `StateExit`, `EventPrecondition` — and every one names **where** a constraint is enforced (globally, in-state, on entry/exit, on an event). Not one names *band-vs-rule*. This is correct and it is decisive: `min 5` desugars to `rule Qty >= 5` (`precept-language-spec.md:1138`, "interchangeable to the proof engine"), so there is no separate "band" object with its own softer disposition. A derived write that violates `min 5` violates a rule, full stop. And in the fault registry, that violation has a home: `FaultCode.OutOfRange = 13`, carrying `[StaticallyPreventable(DiagnosticCode.OutOfRange)]` — a *statically preventable fault*, distinct from `NumericOverflow = 12` (value-vs-type-representable-range). The language's own metadata says: an out-of-band derived value is a fault the compiler is expected to prevent. GOVERN asks us to un-say that.

**Step 5 — The philosophy's process guarantees cut the same way.** `philosophy.md:49` promises "Business processes cannot get stuck… No workflow trapped in a state it can never leave," and §0.7's data guarantee promises "no window in between where an invalid combination can exist." A derived-write refusal at runtime means a *legitimately-authorized operation, invoked with valid external inputs that all passed ingress*, dead-ends — not because anyone did anything wrong, but because the definition's own computation has a range it never proved it stays inside. The `because` reason on that refusal ("Balance must be at least 0") is addressed to no one: the caller didn't set `Balance`. That is much closer to "a valid operation trapped" than to "an invalid configuration prevented." Prove-or-reject surfaces the unhandled case *at authoring time*, where the domain expert can answer the real question ("what happens on overdraft?"); GOVERN buries it until a runtime dead-end.

## 3. Answering the thesis's strongest counter-argument

The thesis's best move is the **obligation-based reframe** (§2.4(ii), §6 Divergence 1, bullet 1): proof-carrying is a property of the *obligation*, not the *construct* — "the boundary is proof-carrying vs. not, never band vs. not" — and a merely-unprovable derived band violation "resolves to `ConstraintsFailed`, a recoverable governed refusal, which by the evaluator's own consequence taxonomy (`fault-floor-definition:122`) is governance's home turf, not a fault." It adds that my own substrate said so: `fault-floor-definition:210` instructs "REMOVE… OutOfRange (13)" from the statically-preventable registry, and `:177` routes the scale-back to governance "FOR NON-PROOF-CARRYING FIELDS ONLY." So the thesis says my ruling *contradicts its own substrate* and *assumes the conclusion* by calling this a "fault-prone operation."

I want to concede what's right in this before I say why it fails, because one leg genuinely is.

**First, the honest concession (see also §6 below).** I agree the boundary is proof-carrying-vs-not, not band-vs-not. My Q8 says exactly this — the "fragile split dissolves," all declared constraints are uniformly governed at ingress *and* contributed as premises. So I do not defend "a band is a special softer thing." Good. And I'll drop one weak leg of my Q5 outright: I wrote that GOVERN fails partly because "there is no runtime band representation distinct from a rule to govern it with." The thesis is right that this is asymmetric — I called the same "no runtime representation" point a mere build-price when demoting Model A. A band *is* a rule and rules *are* governed at ingress, so the rule-governance surface exists. That leg was wrong; I withdraw it. My position does not need it.

**Now why the reframe fails: it assumes the derived value reaches runtime as external-shaped input, and it doesn't.** The reframe's whole weight rests on the classification "band violation → `ConstraintsFailed` → governance." But `fault-floor-definition:122` defines a fault as "a mid-evaluation abort with **no recoverable typed outcome**… anything the working-copy discard **or ingress refusal** handles… is governance." The operative word is **recoverable**, and recoverability is not a property of the discard mechanism — it is a property of *whether an external agent can act on the refusal*.

For **external ingress**: bad arg → discard → the *caller* receives a typed `ConstraintsFailed` naming the violated rule → the caller supplies a different arg. Recoverable. Genuinely governance.

For a **derived write**: the external args were all valid and passed ingress; the definition then computed an out-of-band value from its own governed inputs → discard. Who receives the refusal? No one supplied `Balance`. There is no "different value" any external agent can offer. The operation simply cannot complete, and the reason names nothing the outside world can fix. That is *precisely* `:122`'s "mid-evaluation abort with no recoverable typed outcome" — the definition of a **fault**.

So the taxonomy the thesis invokes is the taxonomy that *defeats* it, once you apply its own word "recoverable" honestly. Working-copy discard is **necessary but not sufficient** for governance; it must *also* hand back a refusal an external agent can act on. External ingress does; a derived write does not. The thesis reads `:122` as "discard-handled ⇒ governance" and drops the "recoverable typed outcome" half of the same sentence. **That is where it assumes the conclusion** — it presumes the derived value arrives at a boundary where a refusal is meaningful, which is the very thing in dispute.

On the substrate contradiction (`:177`, `:210`): yes, the `fault-floor-definition` draft recommended exiting non-proof-carrying bands to governance, and my ruling reversed it. That reversal was deliberate, not an oversight. The substrate reached its recommendation by the same move the thesis makes — treating "working-copy discard" as sufficient for governance. My ruling applied `:122`'s own recoverability test and found derived writes fail it. The substrate conflated external and derived writes under one "discard" umbrella; the ruling separated them. A draft's recommendation is an argument, not authority — including my own draft's.

## 4. What GOVERN would actually require to be honest

This is the cost I flagged as "a band-governance surface that does not exist," and I want to be concrete, because it is easy to say "just govern it" and hard to say what that means in the pipeline.

Note first what *does* exist and is **not** the answer: the rule-governance surface at ingress. That surface checks `rule Qty >= 5` against **external transitions** — event args, edits, construction. It is real and it works. It is not the surface GOVERN needs here, because a derived write is not an external transition. So GOVERN for this cell requires building something genuinely new:

**(1) A new enforcement lane: an intra-plan, post-derivation constraint re-check.** After each *internal* assignment action, the runtime would have to re-evaluate the target field's constraints against the working copy and, on failure, produce a *typed, recoverable* outcome. None of `ConstraintKind`'s five members models this — they all model enforcement against state-transition/event positions, not "re-check a field's band against a value the plan just computed, mid-plan." So either `ConstraintKind` grows a sixth position genuinely unlike the other five (a *derived-write checkpoint*), or a parallel enforcement surface appears outside it. Either way it is new machinery, and per our own catalog rules a new enforcement position is a language-surface change, not an implementation detail.

**(2) The fault pipeline would have to be rebuilt to make the outcome recoverable rather than an abort.** Today the *only* runtime thing that catches an out-of-band derived value is the `Faulted` trap — a mid-evaluation abort, the fault floor's defensive backstop (`precept-language-spec.md:110`, "runtime fault traps exist only as defensive redundancy for paths the compiler has already proven unreachable"). So "GOVERN this at runtime" *today* resolves to **the fault trap = the abort = the fault.** To make it a genuine governed refusal you must: remove `OutOfRange` from the `[StaticallyPreventable]` registry (exactly `fault-floor-definition:210`), *and* invent a new recoverable-outcome type for derived-write refusal that is not a fault trap — and then answer the question the machinery cannot answer for you: **recoverable by whom?** A derived value has no supplier. The only honest answer is "recoverable by an author who explicitly opted this field into runtime governance" — which is not a runtime feature at all. It is a *language construct* (my Q7 honest exit, R4; see §7).

**(3) The discharge seam has to be held for proof-carrying bounds simultaneously.** The RED-TEAM AMENDMENT (`fault-floor-definition:128`) is unavoidable: if a bound ever discharges a fault obligation, relocating its enforcement to a runtime sweep *severs the proof* — "a proof whose precondition governance does not actually make true is not a proof" (`philosophy.md:55`). This is why proof-carrying bounds must stay prove-or-reject even in the thesis's own scheme, and the thesis agrees. It means GOVERN can never be a blanket move; it must be a *surgical* one that carves proof-carrying bounds out — which in turn means the runtime needs write-aware tier-2 discharge machinery that today has **zero consumers** (`fault-floor-definition:144`).

So "GOVERN" honestly implemented is: a new intra-plan checkpoint, a new recoverable-outcome type distinct from the fault trap, a removal from the statically-preventable registry, write-aware discharge wiring, *and* an answer to "recoverable by whom" that bottoms out in a language construct. Slapping "GOVERNED" on the current behavior without building all of that ships the `Faulted` trap relabeled — the exact false-security the philosophy exists to forbid. GOVERN is not the cheap option here; it is the expensive one, and its cheapest honest form is a language change.

## 5. What Error actually costs — my own downside, stated honestly

Prove-or-reject is not free, and I will not pretend otherwise.

**The bound-invention cost is real.** The corpus shows 214 unbounded money fields against 10 bounded (`bounds-only:120-122`). If a derived value flows from an unbounded source into a bounded field, prove-or-reject forces the author to do one of: declare a bound on the source, declare the missing relationship as a rule (`rule Deposits >= Withdrawals`), guard the operation (`when …`), or route the out-of-band case to an explicit reject row. For a field where the author *knows* the value is fine but *cannot state a tight numeric bound they actually believe in*, this is "representability masquerading as policy" (`niche-packet:129`) — a bound invented to satisfy the prover, not to express a domain truth. That violates the honesty-about-approximation principle (Principle 8) in spirit: the author writes a `max` they don't mean.

**Practical authoring impact.** A domain expert who writes `set Balance = Deposits - Withdrawals` into `field Balance min 0` gets a *rejection* if the engine can't prove `Deposits >= Withdrawals`. They must now do more up-front work: state the relationship, guard the subtraction, or handle overdraft explicitly. For the author who "just wanted to compute a balance," that is friction, and at scale (214 unbounded money fields) it is not negligible friction. This is the strongest empirical point on the thesis's side and I credit it fully.

What the friction and the fix actually look like — the naive version, then three ways to satisfy the prover:

```precept
# Naive — REJECTED under prove-or-reject. The engine cannot prove
# Deposits − Withdrawals ≥ 0, so the write into Balance (min 0) is an
# undischarged containment obligation.
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

**Why I still hold, given the cost.** The friction is the cost of *confronting an unhandled case*, not the cost of a *spurious* rejection. If `Deposits - Withdrawals` can go negative and `Balance` cannot, the author has a genuine domain question — what happens on overdraft? — that they have not answered. Prove-or-reject makes them answer it at authoring time, in the source, where the answer is legible and permanent. GOVERN lets them not answer it, and converts the unanswered question into a runtime refusal addressed to no one. I would rather pay authoring friction that yields a complete definition than buy authoring convenience that ships an incomplete one. But I hold this as a *judgment about which cost is worth paying*, not as a proof that GOVERN's cost is zero — it isn't, and the corpus says so.

## 6. The one correction I already conceded — and why it doesn't resolve this

The thesis's "one sharpening on the ruling's own terms" (end of §6) is correct and I accept it: **decidability shapes which obligations *exist*, not merely how they discharge.** An undecidable constraint — say a field bounded only by a nonlinear rule — contributes *no* interval to the proof engine, so a derived write into that field generates *no* family-B containment obligation at all, and therefore is governed at ingress like any rule, not rejected. The precise one-line boundary is "derived values are proven against every **decidable** constraint," not "every constraint." I conceded this and I stand by the concession.

But it **does not touch the disputed cell**, and it is important to see exactly why, because the thesis itself admits "the fault floor proper is unaffected."

The concession is about cases where the *constraint* is undecidable (nonlinear rule → no interval → no obligation → governed). The disputed cell is the opposite: the constraint is **fully decidable** (a literal `min 0` / `max 100` extracts a clean interval `[0, +∞)` with certainty) but the derived **value** is merely unprovable (an unbounded source interval `[−∞, +∞]` that the engine cannot show sits inside `[0, +∞)`). There, the obligation *does* exist — the band is decidable, so the family-B containment obligation is created — and it is *undischarged*. My concession relocates the cases where *no obligation exists*; it says nothing about a case where the obligation plainly exists and cannot be met. The refinement sharpens the boundary's phrasing; it leaves the merely-unprovable-decidable-band cell exactly where Q7 put it: prove or reject.

## 7. Bottom line — and what would move me

**My position:** For a definition-derived value flowing into a non-proof-carrying but *decidable* declared bound, where containment is merely unprovable, the disposition is **Error — prove-or-reject** (`proof-engine-boundary-ruling` Q7). The derived value has no external ingress to govern; the composition seam of `philosophy.md:55` cannot close for it; a runtime refusal of it satisfies the *discard* half of `fault-floor-definition:122` but fails the *recoverable typed outcome* half, which makes it a fault, not governance. "Cannot prove" is a proof debt the author must discharge at authoring time, not a runtime event to defer.

**Am I immovable? No — and this is the honest steelman.** There is exactly one argument that moves me to GOVERN for this cell, and it is the one I named in my own ruling as the sole exit (Q7, "the only place the verdict could move"): **an explicit, author-visible, opt-in language construct** — the R4 trust construct routed to `/design`. If the author can write, *in the source*, a visible token that says "this field is governed at runtime, not proven — I accept a runtime refusal here," then the derived write acquires the one thing it structurally lacks: **an authored ingress-of-intent.** The refusal is no longer addressed to no one; it is addressed to the author who explicitly opted the field into runtime governance and told the language, in permanent legible source, "check this at runtime." That converts a *silent deferral* (which I reject) into a *declared authoring choice* (which I would accept). Under that construct, GOVERN for this cell becomes honest, and I would ratify it.

To picture the *shape* of that R4 exit — and only the shape:

```precept
# ILLUSTRATIVE ONLY — not implemented, not designed, not a keyword proposal.
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

- **Ratify Error (my position):** derived writes into non-proof-carrying decidable bands prove-or-reject; accept the bound-invention cost (§5) explicitly as a named tradeoff on unbounded sources.
- **Ratify GOVERN (the thesis):** but only honestly, which means funding the machinery in §4 *and*, to close the false-security hole, doing it through the explicit author-visible trust construct of R4 — not as a default, and not on the strength of ambient legibility alone.

I am not asking you to treat my ruling as binding — it is self-labeled "not locked," and agent labels don't bind you. I am asking you to see that the thesis's GOVERN, honestly built, converges on the *same* language construct I named as my own exit. We are closer than the "Frank vs. thesis" framing suggests. The real question is not "prove-or-reject vs. govern" — it is **"is a merely-unprovable derived write a proof debt the author discharges at authoring time, or a governance choice the author declares explicitly in source?"** I say proof debt by default, governance only by explicit authored opt-in. That is the sentence I'd like you to rule on.
