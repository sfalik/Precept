I have verified all the load-bearing uniqueness citations first-hand. The cited path:line references hold — I found no fabricated or misquoted citation carrying the uniqueness argument (philosophy.md:49/55/57, formal-spec:164/168, entity-governance-landscape:430, governance-vs-validation failure modes, prior-art SPARK/Liquid Haskell:118-124, niche-packet caveat 5:133 all check out). The findings below are about where specific sentences overreach beyond what the (largely sound) core supports.

---

**FINDING 1 — The Event-B engagement blurs the one place Precept's proof is WEAKER than Event-B's, and the §4a fusion sentence trades on that blur. [most severe — this is the "hardest test"]**

Passage: §4a — "In Event-B the proof exists and then a human re-implements the machine; in Precept the proved artifact *is* the deployed machine — no refinement gap for divergence to enter. That fusion, not the abstract machine, is the invention."

Why overstated: The shared object of both systems is *invariants that must hold in every reachable state*. Event-B **proves** invariant preservation deductively — it "generates a proof obligation for each event: given that the invariants hold before the event fires, prove they hold after… the invariants cannot be violated by any event, provably" (`formal-spec-languages-comparators.md:168`). Precept does **not** prove its invariants/rules; it **enforces them at runtime** — the research states this plainly: "Event-B's guarantee is deductive (formal proof); Precept's is operational (runtime enforcement)" (`:168`), and the draft's own §2.4 relocates declared-band/rule containment to *governance, runtime-enforced, not proved*. So on the central invariant both systems share, Event-B out-proves Precept by design. The draft's "the proved artifact is the deployed machine" equivocates on *what* is proved: Precept's artifact is proved **fault-free and non-self-contradicting** (a strictly smaller surface — division-by-zero, overflow, empty-access, and default-incoherence), while its **business invariants are runtime-enforced**. The passage leaves the impression that Precept fuses proof *onto* Event-B's model, when the honest trade is: Precept **gives up** Event-B's invariant-preservation proof in exchange for runtime enforcement + a solver-free fault proof + no refinement gap. The uniqueness (fusion + legibility + inspection) survives this, but the draft should state the trade, not paper it.

Evidence that would settle it: a side-by-side of proof *scope* — Event-B's per-event invariant-preservation obligation (`:168`, Abrial 2010) vs. Precept's proof surface as the draft itself scopes it in §2.2–2.4 (fault floor + incoherence only; rules/bands = govern). If Precept's proof engine does *not* prove "rule R holds after every event," then "the proved artifact is the deployed machine" is true only for fault-freedom, and the sentence must say so.

---

**FINDING 2 — The single sentence carrying "the invention" is falsified on a plain reading by SPARK; uniqueness survives only on a narrow qualifier the draft buries. [high]**

Passage: §4a — "**No surveyed system combines a prove-or-reject static floor with a governed runtime** that is not a fallback but the *discharge mechanism of the proof itself*."

Why overstated as written: SPARK combines a prove-or-reject static fault floor with a deployed runtime *today*. The draft's own cited source describes SPARK proving Absence-of-Run-Time-Errors — "range checks… by a simple static analysis of bounds… a cheap bound-propagation pass first, SMT only for what bounds can't settle… **option (a) shipping in safety-critical production**" (`interval-vs-value-evaluation-prior-art-2026-06-05.md:124`), and the compiled Ada program is the production artifact. So "prove-or-reject static floor + running deployed program" is not unique. The uniqueness rests **entirely** on the buried qualifier — that Precept's *ingress governance automatically discharges the proof's precondition*, rather than the precondition being proved at each call site (SPARK) or checked as a fallback. That is a real and defensible distinction, but the lead clause reads far broader than the distinction supports, and the draft cites SPARK approvingly two sections earlier (§2.2) as *precedent* for exactly the "supply-the-precondition" discipline it then claims no one shares.

Evidence that would settle it: SPARK's AoRTE guarantee + deployed-binary model (`prior-art:114-126`, SPARK User's Guide). The defensible claim is the narrow one ("the caller obligation is discharged automatically by ingress governance rather than by a human at every call site" — which the draft *does* state in §2.2); §4a should lead with that, not with the broad combination sentence.

---

**FINDING 3 — The "empty cell" moat is a near-tautological artifact of a six-property conjunction and a self-selected 2×2; the draft flags the source but not the low erosion barrier. [medium]**

Passage: §3 — "The seam Precept owns: *entity-bound + lifecycle-aware + structurally non-bypassable + embeddable + definition-proved + inspectable*, in one artifact… no incumbent occupies it (`entity-governance-landscape.md:430`)."

Why under-examined: The draft commendably adds four caveats (company-commissioned research, DbC omitted, category-education burden, BRMS deployment tension). But it omits the structural one: an *empty cell is guaranteed by any sufficiently long property conjunction*. Each of the six properties is individually occupied — validators own rules, state machines own lifecycle (`entity-governance-landscape.md:424-425`), DB CHECK constraints own non-bypass ("closest to structural governance… mandatory enforcement, no bypass from any client," `governance-vs-validation.md:111`), SPARK owns definition-proof, embeddability is a packaging choice. The moat is the *conjunction*, which is real but low-barrier: a competitor need add only one property to an existing tool (e.g., XState + a runtime constraint-rollback, which the memo itself notes XState "stops short of," `entity-governance-landscape.md:360`). The landscape memo's 2×2 axes (lifecycle × data-enforcement) are chosen by the positioning author; different axes populate different cells. "No incumbent occupies it" is credible but is a statement about *current packaging*, not a durable defensive barrier.

Evidence that would settle it: whether any *single* property is individually defensible against a fast follower (none is), vs. whether the *fusion* imposes a real engineering barrier (the solver-free legible-proof + one-file semantics may — that is where the defensibility actually lives, not in the empty-cell count). Recommend recasting "the seam Precept owns" from an occupancy claim to a *cost-to-replicate* claim.

---

**FINDING 4 — "The proof claim (the moat)" is called a moat while the draft's own sources report near-zero current catch-rate; it is a bet, not a moat. [medium]**

Passage: §5 — "**The proof claim (the moat):** the definition itself is proven sound before any entity exists." And the meta-proposition "Precept sells calibrated trust."

Why overstated: By the draft's own §2.7(a) and its cited source, the proof leg today catches almost nothing in real corpora: "the niche's corpus catch-rate is honestly near zero against the 77 idiomatic samples" and "the case rests on floors-plus-demand, not measured corpus incidence" (`compile-time-niche-decision-packet-2026-06-10.md:121`); the value rests on "corpus-bias reasoning plus external demand evidence plus three shipped-sample defects, not on measured incidence" (`:133` caveat 1); and "the runtime is stubbed: every claim about governance/inspector behavior is from canon design docs, not observed execution" (`:133` caveat 3). A "moat" connotes durable, demonstrated defensibility. A capability with near-zero measured catch and no runtime yet is a **prospective bet**. The draft is honest about the ceiling elsewhere (§2.7(a): wrong-but-in-band undecidable; equality/disjunction/3-variable sums provably out of reach, `bounds-only`/`prior-art:65`) — which makes the unqualified "moat" label in §5 inconsistent with its own analysis.

Evidence that would settle it: a measured defect-catch rate on real, non-idiom-aware authoring — which does not yet exist (runtime stubbed). Until then, "moat" should read "the differentiator we are betting on."

---

**FINDING 5 — §2.7(c) over-attributes the §0.4 language austerity to *proof*, skewing the answer to "would relaxing to runtime leave Precept without a value prop." [low–medium — neutrality slip]**

Passage: §1d / §2.7(c) — "the language has already paid for proof… a runtime-only Precept keeps the expressiveness bill and returns the purchase," citing §0.4 (`precept-language-spec.md:158-174`).

Why skewed: The no-loops / closed-vocabulary / one-file austerity serves **determinism and inspectability** at least as much as proof — both are independent core commitments (determinism, `philosophy.md:22`; full inspectability, `philosophy.md:60`) that a runtime-only Precept would *still* need and *still* pay for. So "returns the purchase" over-attributes the austerity to the proof leg specifically. The honest version: relaxing to runtime forfeits the *process-topology* proof (reachability/dead-end/dominance — genuinely compile-time-only, `philosophy.md:51`) and the honest live "Certain" verdict, but does **not** obviously "return" the §0.4 constraints, which determinism and inspectability retain. This matters because the task's question (c) is an *open owner question* — the niche packet lists "whether Layer 2 becomes the marketed identity or remains an unmarketed quality layer" as a "live choice point this packet does not settle" (`:133` caveat 6) — and the draft's framing ("proof is constitutive… below it a precedented graveyard") leans harder toward "proof is mandatory identity" than the evidence forces.

Evidence that would settle it: whether §0.4's restrictions are load-bearing for determinism/inspectability independent of the proof engine (they are). The neutral framing: process-topology proof and inspection-honesty are the constitutive losses; the language-austerity "sunk cost" argument is weaker than stated.

---

**FINDING 6 — The Eiffel/Code-Contracts "documented corpse" precedent is sourced to un-locatable R1/R3 synthesis and cuts weaker than the draft uses it. [low]**

Passage: §2.7(c)/§3 — "a documented corpse in this jurisdiction… commercially dead in .NET," per "R1/R3" and "eiffel.org."

Why to flag: The Code Contracts discontinuation is factually real, but I could not locate the "R1/R3" research artifacts in the repo to verify the specific empirical claims (the eiffel.org "compilation option makes no difference" quote, the commercial-failure attribution) first-hand — they are cited as external synthesis, not a repo path. More substantively, the draft's own hedge ("the failure may have been ergonomics, not concept") undercuts the "graveyard" conclusion: DbC's failure is widely attributed to *tooling/ergonomics and the absence of static proof* — precisely the gap Precept's fused static floor closes. So the precedent is at least as much evidence *for* Precept's differentiation (add the missing proof + one-file + inspection) as against runtime-enforcement per se. The draft leans on it as a cautionary corpse while its own reasoning shows it supports the opposite.

Evidence that would settle it: the R1/R3 sources (not in-repo as far as I found) and a first-hand read of the Code Contracts post-mortem attributing failure to ergonomics vs. concept.

---

**Net assessment (plainly): the core uniqueness case is sound.** The *fused artifact* — one `.precept` file that is simultaneously the definition, the deployed non-bypassable enforcer, and a production inspection API (`Inspect`, `philosophy.md:18,60`), with solver-free *legible* proof aimed at a domain-expert author (`precept-language-spec.md:225,231`) — is genuinely not occupied by any surveyed tool, and the draft's honesty is above average for this kind of document: it concedes the model is Event-B (`formal-spec:164`), flags the company-commissioned nature of the landscape memo, concedes the proof coverage ceiling and near-zero current corpus catch, and surfaces the `philosophy.md:49` "No bugs" overclaim as an owner-gated fix. The six findings above are about specific sentences that reach past that sound core — chiefly (1) blurring that Event-B *proves* the invariant Precept only *enforces*, and (2) a lead sentence that SPARK falsifies unless narrowed to the ingress-discharge qualifier. Tightening those two would make the uniqueness claim both narrower and more defensible.