I have verified the load-bearing citations first-hand (`docs/philosophy.md` in full; `precept-language-spec.md` §0.6/§0.7/§2.4 and Principles 7/10/11; `fault-floor-definition-2026-06-11.md` §floor/§gaps/§scale-backs/§registry). Findings below, ranked.

---

**FINDINGS — Philosophy-change necessity, alternative fairness, ruling reconciliation**

---

**F1 — CENTRAL DIVERGENCE: the ruling re-absorbs OutOfRange/Length/Count into the fault floor against the very floor-research it builds on; the draft aligns with that research. Draft's divergence is justified on the merits.** (verdict: draft better)

The draft's Rec A moves declared-bound containment (`§0.7:266` "no result outside a declared bound"; Principle 11 "constraint range impossibility" `:112`; `§0.6 item 6:208`) from prove-or-reject to governance + flag-sound warning, *except* for proof-carrying bounds. R5 §2/§5 reports Frank's ruling keeping OutOfRange/LengthBoundViolation/CountBoundViolation *in* the fault floor as prove-or-reject Error, and R5 §7 itself concedes Frank's Q5 "appears to land on the opposite side."

The decisive fact is not draft-vs-ruling — it is ruling-vs-its-own-substrate. `fault-floor-definition-2026-06-11.md:210` instructs "REMOVE from the [StaticallyPreventable] registry and the bijective core: OutOfRange (13), LengthBoundViolation (14), CountBoundViolation (15)," and `:132`/`:177-178` route them to governance + proven-violation warning "FOR NON-PROOF-CARRYING FIELDS ONLY … the boundary is proof-carrying vs not, not band vs not." That doc's own fault definition is by *consequence*, not origin: "A fault is a mid-evaluation abort with no recoverable typed outcome … anything the working-copy discard or ingress refusal handles (ConstraintsFailed) is governance" (`:122`). A band violation resolves to `ConstraintsFailed` — governance by that definition. `:136` records this as a *strong convergence of two independent derivations plus a red-team* ("3 rule-misclassified (OutOfRange/Length/Count)"). The draft reaches the identical boundary independently (`§2.4`).

Frank's origin-routing ("a value a Precept expression has derived from it is proven-or-rejected", R5 §2) therefore contradicts the consequence-boundary in the research the ruling cites. The draft's consequence+proof-carrying boundary is the better-grounded of the two. **This is a genuine open owner question, not settled by either "ruling" label** (agent artifacts, R5's own scope note `:3`,`:358`).

---

**F2 — Frank's strongest counter (no "band" category to warn with; a warning lets a known fault compile clean) is answerable, but ONLY once the runtime governance sweep exists — and the draft's own precondition concedes it does not yet.** (verdict: reconcilable on a timeline; neither doc frames it that way)

Frank rejects warn-and-govern because "a warning is a clean compile that would let a known fault compile clean" (R5 §5 Q5). The draft's answer is the flag-soundness contract: a proven-violation warning is a *true universal statement* about an always-rejecting (dead) row, structurally identical to the existing `UnsatisfiableGuard` warning the spec already ships (`§0.6 item 9:211`; `:223` "when it speaks, it is right"). That is a real rebuttal Frank's doc does not engage.

But the rebuttal only neutralizes the *false-security* half if the runtime actually refuses the violating write. `fault-floor-definition:144` records "DesugarsToRule → runtime constraint-plan wiring … zero consumers today," and `:203-206` makes the coordinated-landing a *hard constraint* ("no intermediate state where bounds are enforced nowhere"). The draft honors this (Rec A: "one coordinated move … never enforced nowhere"). So the honest reconciliation is temporal: **Frank's Error is the correct interim posture while nothing governs bands; the draft's warning+govern is the correct end-state; the flip must be atomic with the sweep.** Neither document states it this way — Frank says Error (permanent, opposite side), the draft says warning+govern (post-coordination) — and the split below (F3) shows the two docs are also bundling two different sub-questions.

---

**F3 — Both draft and ruling BUNDLE two distinct sub-questions that need separating; only one turns on the sweep, and the draft leaves the other (a default/set-action asymmetry) unreconciled.** (verdict: gap in the draft; the D2 tension R5 §7 surfaced is inherited, not resolved)

The contested cell is actually two questions:
- **(ii) merely-unprovable band write** (`[-inf..+inf]` into `max 100`): over-reject (Frank's Family-B prove-or-reject; forces bound-invention, `fault-floor:176`) vs govern-at-runtime (draft). *Sweep-dependent* — this is the real prevention-vs-false-security crux, and the draft's Principle-8 anti-bound-invention argument (`§2.4`; corpus 214 unbounded money fields, `bounds-only:120-122`) is strong here.
- **(i) proven-always-violation**: Error vs Warning. *Sweep-independent* — a row that provably always rejects is dead regardless of runtime.

On (i) the draft inherits an unjustified asymmetry: a default provably outside its bound is Error ("definition incoherence," `§2.3`; `fault-floor:189`), but a set-action provably outside the *same* bound is a Warning ("dead reject row," `§2.5`; `fault-floor:177`). R5 §7 flags exactly this from the D2 analysis ("opposite dispositions to the same structural shape … only a 'when' distinguishing them"). A principled distinguisher exists — default-violation means *no valid Create* (incoherence) whereas a dead set-action leaves a valid entity with a dead row (quality flag, like `UnsatisfiableGuard`) — **but the draft never states it**, so its Rec 4 taxonomy lacks the four-leg rationale on precisely the seam the D2 doc left open. Frank collapses the asymmetry to uniform Error, which is coherent but over-rejects on (ii). Neither supplies the rationale; the draft should.

---

**F4 — OVERFLOW: cleanest divergence. Draft = build the proof (floor obligation); ruling = D1-park + temper the spec. Draft is prevention-first-correct on identity; the ruling is honest about a real blocker; the draft under-surfaces that blocker.** (verdict: split — each half-right)

Draft `§2.2` places representability overflow and temporal overflow in the eleven prove-or-reject families ("must be proven absent … or the definition rejected"), correctly citing `fault-floor:124`. Under the task's stated heuristic — "a prevention-first product should prefer strengthening the design over weakening the philosophy" — the draft's build-it posture is the identity-correct one: overflow is a fault (no graceful runtime home — it aborts), so by the draft's own Axis 1 and Principle 10 (`:110`) it belongs in the floor, and parking it is a genuine guarantee hole.

The ruling's D1-park (R5 §3) is grounded in *cost/blocker* terms — "the single largest Phase-3 build … gated on an undecided number model" — i.e. an explicitly archaeological/buildability rationale, not an identity argument. That is the "weaken the claim rather than strengthen the design" move the heuristic warns against.

BUT the draft **under-weights the blocker it cites**: `fault-floor:124` item (9) calls decimal-representability overflow "THE one missing obligation family … gated on the owner's numeric-representation decision," and `:146-147` makes the number-model decision (fixed-decimal-with-obligation-bounds vs arbitrary-precision) a *hard prerequisite* — arbitrary-precision "deletes the fault class" entirely. The draft presents overflow as a settled floor member without surfacing that the floor's *own* completeness here is blocked on an unmade owner decision. **That is the draft papering over a known-hard weak spot on the necessity axis.** The complete answer needs both: overflow is the identity-correct floor target (draft), AND it is blocked on a number-model decision that must be surfaced first (ruling's honesty), AND — see F5 — the interim overclaim must be disclosed.

---

**F5 — The draft's honesty program is internally inconsistent: it tempers philosophy.md:49 ("No bugs") but leaves philosophy.md:53's overflow-prevention overclaim unaddressed, even though §2.7(a) concedes overflow is an open gap.** (verdict: necessary-but-incomplete recommendation)

Draft Rec B (verified quote) tempers `philosophy.md:49` "**Prevention, not detection. No errors. No bugs. Business logic cannot produce a wrong answer.**" — necessary and *un-paperoverable*, because wrong-but-in-band values are "undecidable for ANY compile-time system" (draft `§2.7(a)`), so no amount of design-strengthening can make `:49` literally true. Good.

But `philosophy.md:53` reads (verified): "Division by zero, **arithmetic overflow**, empty collection access … are compile-time impossibilities." The draft's own `§2.7(a)` lists "the missing representability family" as an open coverage gap, and F4 shows overflow is unbuilt and blocker-gated. So `:53` currently claims prevention of a fault the product does not prevent. The draft's build-to-target framing *defends omitting :53 from the temper list* (overflow is buildable, unlike "no wrong answer") — that distinction is coherent — **but only if paired with an interim-disclosure rider**, which the draft's Rec list omits. R5 §3 flags the same `:53`/P10/P11 overclaim from the ruling side (R6 recommends surfacing it). Both sides agree `:53` may not stand as-is claiming prevention today; the draft's recommendation set silently drops it. Add it.

---

**F6 — Rejected-alternative fairness: all three are beaten fairly and mostly correctly; the draft's finer-grained fault/governance split on "defer-to-runtime" is its strongest and most-defensible card.** (verdict: fair)

- *Defer-unprovable-to-runtime-traps (Model A)*: draft `§2.2` reason 1 rejects it *for faults specifically* — a fault deferred to a trap still aborts, violating Totality (Principle 10 `:110`, "defensive redundancy … proven unreachable"). Correct. Crucially the draft does **not** overreach into rejecting band-relocation: it distinguishes fault-deferral-to-abort (rejected) from band-relocation-to-recoverable-governance (accepted), grounded in the consequence definition `fault-floor:122`. This finer split is exactly where it diverges from Frank (who rejects all deferral uniformly, R5 §6) and is well-founded.
- *Push-everything-to-runtime-governance*: draft `§2.7(c)` is fair — it concedes the governance architecture survives as "a differentiated product," and concedes the Eiffel precedent "may have been ergonomics, not concept." The kill-shot (process-soundness — reachability/dead-end/dominance — is whole-definition and *cannot* be a runtime fact, `philosophy.md:51`, `§0.5`) is correct and load-bearing.
- *Prove-only-the-decidable-fragment*: the draft effectively *adopts* the govern/flag-the-rest version and rejects only the *over-rejecting* Model-B variant, via flag-soundness + legibility (`§2.5`; `niche-packet:117,184`; `:223`; `:225` opaque-solver exclusion). Convergent with Frank's own Model-B rejection (R5 §6). Fair.

No strawmanning found. The one unverified-in-this-pass leg is the `§4a` uniqueness claim ("no surveyed system combines a prove-or-reject floor with governance-as-discharge-mechanism," attributed to "R1 synthesis") — a positioning claim, not load-bearing for the boundary; the mechanism half is confirmed at `§0.7:270`.

---

**F7 — "HEAD already does this" flags. The ruling's no-change conclusion leans archaeologically (R5 §9 already enumerates five); the draft is substantially clean but its endorsements borrow force from spec text it elsewhere wants to change.** (verdict: flag both, asymmetrically)

Per the task's explicit instruction:
- *Ruling side (must be discounted by a forward reviewer):* R5 §9 flag 1 — the boundary ruling's headline "HEAD `:266`/`:268`/`:270` are **already** Hybrid-shaped … no weakening required" is an archaeological claim doing load-bearing work for a *no-change* recommendation. On the necessity question this matters: **the "no spec change needed" option is weaker than the ruling presents it**, because it partly rests on what the code already says rather than on identity — which modestly strengthens the draft's "yes, amend `§0.7:266`" (F1) on the merits axis.
- *Draft side (minor):* the draft's forward discipline holds — it nowhere argues "the code already does X therefore X is right." Its "already" usages (`§2.7(b)` "already constitutionally equipped … Principle 4 already extends inspectability") cite *design intent* (spec/philosophy commitments), which is legitimate grounding, not code-archaeology. The residual caveat: several draft endorsements lean on "the spec already commits to X" (interchangeability `§2.4:1138`; `§0.7:270` seam), so those endorsements inherit whatever identity-grounding those clauses have — and one of them (`§0.7:266`) is a clause the draft simultaneously argues is *wrong* and wants amended. The draft should not treat the surrounding `§0.7` text as authority while proposing to rewrite part of it.

---

**F8 — Strong CONVERGENCE worth recording: the draft (quarantined from R5) independently reproduces the ruling's Q9 legibility answer and the three-category taxonomy. The quarantine validates these as robust, not as owner-ratified.** (verdict: convergence, not authority)

Draft Rec 5 (per-rule "proven / governed / flagged" queryable disposition, `§2.7(b)`, grounded in `§0.6 item 5:229` tri-state + item 6:231 structured attribution) reproduces Frank's Q9 answer (per-*obligation* PROVEN/GOVERNED surface, "No DEFERRED state," R5 §5). The three-category taxonomy (fault floor / definition incoherence / flag layer) appears identically in draft `§2.3`/`§2.5`, Frank's Q11 (R5 §5), and `fault-floor:132` ("both derivations converged"). Two quarantined derivations landing on the same structure is real corroboration of the *architecture*. It is **not** ratification: R5's scope note (`proof-engine-boundary-ruling:3`,`:307-320`,`:358` "Not Locked") and the memory that agent "locked/ruling" labels do not bind the owner both apply. The convergence is an argument-strength signal; the owner still gates the four live disagreements (F1–F5).

---

**F9 — Unresolved precondition neither side has discharged, and the draft does not flag: does the runtime refuse a violating write *today*?** (verdict: draft's Rec A is honest about coordination but silent on the empirical test)

R5 §4/§10 elevates the band-guarantee analysis's "sharpest finding" to an *empirical, not conceptual* test: "does the runtime refuse a violating write today? If yes, no hole. If no, the deferred bounds are already unproven-and-ungoverned." `fault-floor:144` answers it as of that research: "zero consumers today." The draft's Rec A correctly demands the coordinated landing, but the draft's teachable value-prop sentence (`§5`, "everything your actual data decides is enforced on every operation") describes the *end-state* runtime as if constitutive, while `§2.7(c)` leans on "the runtime governance leg is identity-constitutive." Given the runtime is a stub with zero band consumers, the draft should state plainly that **its entire band-relocation (Rec A) and half its value proposition are contingent on a governance sweep that does not exist yet** — the same load-bearing precondition R5 §4 makes central. The draft flags the *coordination constraint* but never states the *current empirical fact*, leaving its strongest claims resting on unbuilt runtime.

---

**Net reconciliation.** The draft and the ruling agree on the architecture (fault floor / definition incoherence / flag layer, flag-soundness, Q9 legibility, Model-A and pure-Model-B both rejected). They diverge on exactly two live cells, and on both the draft has the better *identity* argument while the ruling has the better *interim/buildability* argument: (1) non-proof-carrying band containment — draft's consequence-boundary is corroborated by the floor-research the ruling contradicts (F1), but is contingent on an unbuilt sweep (F2, F9) and leaves a default/set-action asymmetry unreconciled (F3); (2) overflow — draft is prevention-first-correct to build it (F4) but under-surfaces the number-model blocker (F4) and the interim `philosophy.md:53` overclaim (F5). No philosophy change the draft proposes is *paper-over-a-weak-spot* except the overflow-floor placement, which quietly assumes an unmade number-model decision. The one philosophy temper it proposes (`:49`) is genuinely necessary and un-strengthenable-away; the one it *omits* (`:53`) is the more concrete current overclaim.