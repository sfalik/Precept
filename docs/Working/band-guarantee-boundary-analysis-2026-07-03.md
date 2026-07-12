# The compile-time / runtime guarantee boundary for declared bounds

**status:** Draft analysis — 2026-07-03. Strong analysis commissioned by the owner on the compile-time/runtime guarantee boundary for declared bounds. NOT a design proposal; surfaces the crux for owner deliberation. D2 band split is NOT owner-ratified — this analysis is free to reframe or reject it.

This document folds four independent analyses (conceptual structure, prior art, Precept philosophy, and the false-security failure mode) into one assessment. Where they converge, it says so with confidence. Where they genuinely disagree — and they do, on the single hardest case — it surfaces the disagreement rather than papering over it.

---

## 1. The question, stated precisely

The owner's words:

> "Why check some cases and not others at compile time? That gives a false sense of security. So then we decide it's better to over-reject, and next thing you know we're back to trying to prove everything at compile time."

This is a claim that the design space is a trap with three exits, each bad:

- **PROVE-EVERYTHING** — push every declared bound to compile-time prove-or-reject. Cost: the compiler rejects valid definitions it cannot prove safe (over-rejection), because any sound checker is incomplete.
- **PROVE-NOTHING** — no static guarantee at all on bounds. Cost: abandons the "prevention, not detection" identity.
- **PROVE-SOME** — partial static coverage (the current shape, and the shape the D2 band split extends). Cost: the owner's two-part worry — a *false sense of security* (some cases checked, some not, and nothing tells the reader which), which then generates *pressure to catch more*, which slides back toward PROVE-EVERYTHING and its over-rejection.

The circle: partial coverage → false security → pressure to catch more → over-rejection → the exact problem the band split was fleeing. **Is there a principled way out, or must one simply pick a pole and own its cost?**

The trigger is the D2 band-split proposal, which reclassifies declared bounds (min/max, length, count) that feed no safety proof from compile-time prove-or-reject down to a compile-time **warning** that fires only on a *proven* violation, deferring enforcement to a future runtime governance sweep; bounds that *do* feed a safety proof (a `mincount` that proves collection-access safety) stay prove-or-reject. This proposal has no decision record and is not owner-ratified. This analysis evaluates it, and in one respect corrects it.

---

## 2. Does the circle dissolve?

**Finding: the circle dissolves — but only under a specific, ownable move, and one part of it is genuinely, if benignly, inherent.** All four analyses reach the same core conclusion by different routes. The dissolution is not a cleverer checker; it is a decision about *what axis defines the boundary*.

### The owner's three poles collapse onto the wrong axis

The three poles are arranged along an *effort* axis — how much the compiler tries to prove. An effort axis has no natural stopping point: "catch more, catch more" ratchets toward proving everything, and that is exactly the slide the owner names. The escape is to stop defining the boundary by effort and define it instead by a property that has a hard mathematical edge. Two such properties appear, and they are the same cut viewed from two sides:

- **Consequence.** Does violating the property produce an *unrecoverable fault* (the computation is undefined — division by zero, arithmetic overflow, empty-collection access, enumerated at philosophy.md:53) or a *recoverable governance refusal* (a runtime "no, that write is refused," leaving the entity unchanged — the Fire operation "commits only if every constraint holds… If any fails, the transition is rejected — the invalid configuration never exists," philosophy.md:19)?
- **Decidability.** Is the property's truth decidable *from the definition alone*, or does it depend on runtime data the compiler does not have?

Drawn by these, a checker that is **total over what is decidable from the definition** and **promises nothing about actual runtime values** escapes both horns:

- **No false security**, because false security is precisely *advertised guarantee exceeds actual coverage and the gap is invisible*. If the advertised compile-time guarantee is exactly "no faults, no definition incoherence — nothing about your actual runtime values," there is no hidden gap to be secure about. philosophy.md:25 already licenses redrawing the promise to match the coverage: "Precept therefore draws a hard line between exact and approximate behavior and requires that line to be visible in the type system and public surface."
- **No over-rejection**, because over-rejection is only reached by a checker that tries to prove *runtime-value* facts. By construction this checker refuses that; past the decidability edge there is nothing left to *prove*, only runtime data to *enforce*.
- **The ratchet loses its motor**, because there is no "catch more" gradient across a hard edge — only a discrete "this is decidable / this is runtime."

### Precept already runs this architecture

This is not a new pole invented for bounds. It is the standard sound-but-incomplete-analysis architecture, and Precept already runs it for the hardest case — runtime-supplied values — via **carries-proof** (philosophy.md:55): "It requires the value to *carry* a constraint sufficient to prove the calculation safe, and the engine enforces that constraint the moment the value enters. The compiler proves a structural fact — that the value carries its constraint — never the value itself; the runtime enforcement is what makes that carried constraint true, not a second line of defense." That is a static proof of a *structural* fact composed with a runtime check at the frontier, yielding a *total* guarantee from *partial* static coverage. The compiler does not prove the divisor is non-zero; it proves the divisor carries a non-zero obligation, and the runtime makes it true.

### The part that is genuinely inherent (stated honestly)

The dissolution is not total. The conceptual analysis raises, and does not knock down, two residual movements:

1. **The decidable fragment has a moving edge.** "Decidable from the definition" means "decidable in the arithmetic theory the proof engine implements." Linear integer arithmetic is decidable; multiply two variables and you are in undecidable or expensive territory. Widening the engine to decide more coherence facts *is* the "catch more" pressure, relabeled. There will always be a definition just past the current engine's line but still decidable in principle — a "why didn't you catch this" ticket.

2. **The reject/warn line itself shifts with analysis precision.** A smarter narrowing pass reclassifies some "violated on some path" cases into "violated on all paths"; the same definition could warn today and reject next year.

**Why this residue is benign rather than fatal.** Both movements are *soundness-preserving*: extending the coherence theory only ever moves cases from unprovable-and-deferred to provable-and-rejected, and — provided the reachability analysis over-approximates (stays sound) — it never *fabricates* a rejection of a valid definition. The dangerous ratchet the owner fears is specifically the *value-governance* ratchet: trying to prove actual runtime values in range at compile time, which is what forces over-rejection. That ratchet is stopped cold by classifying value-bounds as governance. The *coherence-theory* ratchet that remains is comparatively benign — it makes the compiler catch more true incoherence over time, never reject more valid definitions. So: **the circle dissolves for the horn the owner actually fears (over-rejection); a benign, soundness-preserving version of the "catch more" pressure remains, and the owner should know it is there rather than expect it to vanish.**

### The load-bearing precondition — where the dissolution is currently at risk

Every one of the four analyses independently lands on the same caveat, and it is not conceptual — it is a build-state fact. The dissolution works **if and only if the runtime governance layer that catches the deferred cases actually exists and is total and unbypassable.** The proposal "defer[s] enforcement to a future runtime governance sweep." Until that sweep exists, a downgraded bound in the deferred cases is enforced by *neither* layer: not at compile time (downgraded to a warning) and not at runtime (not built). In that interval the owner's false-security worry is *literally correct as a build fact*, not merely a conceptual risk. The honest sequencing rule the prior art dictates (gradual verification and SPARK never ship the static relaxation ahead of the dynamic backstop): either keep the deferred bounds at prove-or-reject until the runtime governance exists, or ship the reclassification and the runtime enforcement in the same increment, so the static-proof-plus-runtime-enforcement union is never non-total.

**Bottom line for Section 2:** the circle is not inherent. It is an artifact of defining the boundary by checker effort. Redefined by consequence/decidability, with the promise redrawn to match and the runtime backstop actually in place, partial static coverage yields a total guarantee and neither horn is reached. The residual "catch more" pressure on the coherence theory is real but benign. The one place the dissolution can fail today is the missing runtime backstop.

---

## 3. The false-security question, answered

**Direct answer: partial static coverage is *not* inherently misleading. It misleads in exactly one configuration — when the guarantee boundary is illegible — and that configuration is fixable.** This is the sharpest and most decision-relevant finding, and the prior art establishes it as a theorem, not a hope.

The owner's first link — "partial coverage gives false security" — is where the circle breaks, because it conflates *partial static coverage* with *partial guarantee*. They are not the same. The distinction the systems draw:

- Partial coverage **misleads** exactly when the reader cannot tell, from the artifact or the type system, *where the guarantee stops* — so "it compiled clean" is read as blanket safety over a domain that silently includes cases the compiler never spoke to.
- Partial coverage is **honest** when the boundary is legible — the artifact marks which obligations are statically discharged and which are governed at runtime, so trust is calibrated to what was actually proven.

The decisive precedent is **gradual verification** (Lehmann & Tanter, "Gradual Refinement Types," POPL 2017; Bader, Aldrich & Tanter, "Gradual Program Verification," VMCAI 2018). Its mechanism is exactly the owner's "some static, some dynamic": a partially-precise specification is checked statically as far as static knowledge reaches, and a **residual runtime check is materialized at the frontier where static proof stops.** The consequence, proved as a soundness result (an adapted "gradual guarantee," in the sense of Siek, Vitousek, Cimini & Boyland, "Refined Criteria for Gradual Typing," SNAPL 2015): the guarantee is **total** even though static coverage is **partial** — no case is ever both unproven and unenforced, because the un-proven region is not dropped, it is dynamically guarded. This is the direct refutation of the owner's first link: *false security comes from a fuzzy boundary, not a partial one.*

The same lesson recurs across mature systems, each avoiding false security by making the boundary legible rather than by proving more:

- **SQL CHECK constraints** — enforced purely at write time inside the transaction; the DDL compiler never proves future inserts will satisfy them. Misleads no one, because the guarantee surface is crystalline: the constraint holds on every committed write, atomically, and a violating write is refused. This is the exact structural analogue of Precept's "recoverable governance refusal" for bands.
- **Design by contract** (Eiffel; Spec#/Code Contracts with the Clousot static analyzer, Fähndrich & Logozzo) — proves what it can statically and leaves the rest runtime-checked; the union stays total, and a contract violation is a definite failure at a named place, never a silent wrong answer.
- **SPARK/Ada** (GNATprove) — proves Absence of Run-Time Errors where it can; where it cannot, the obligation does *not* vanish — it remains an undischarged check that must be discharged by proof *or* by test/review, and unproven checks compile to runtime assertions. AdaCore publishes this as graduated assurance *levels* (stone→bronze→silver→gold→platinum), each stating precisely how much is statically proven versus runtime-guarded. The honesty move is the *published surface*: the reader always knows which guarantee they stand on.

Precept already commits to this honesty standard — for approximation (philosophy.md:25, above) — and the bounds question is the same shape. And Precept's clean-compile promise is *already a closed enumeration*, not a blanket claim. philosophy.md:57: "A definition that compiles without diagnostics has no unproven evaluation faults, no unreachable business process states, and no structural dead ends." Three things, exactly — it does *not* say "every declared bound holds at runtime." So, read strictly, moving governance bounds off the compile-time error path does not falsify clean-compile, **provided clean-compile was never advertised to cover them.** The danger is the absolutist copy in the same document — "No errors. No bugs. Business logic cannot produce a wrong answer" (philosophy.md:49-51) and "not what bugs you try to catch, but what bugs cannot exist" (philosophy.md:57) — which a domain-expert author will internalize instead of the three-item enumeration. **That is the actual mechanism of false security: not the band split, but a band split that changes what clean-compile covers without narrowing the advertised meaning of clean-compile to match.** The honesty burden is therefore partly a documentation-and-surface burden.

### The reverse false security of over-rejection

One analysis presses a point the owner should weigh before treating PROVE-EVERYTHING as the "safe" pole: **over-rejection carries its own false security, and it is less legible.** To catch everything, a maximal compiler must decide borderline definitions under a conservative model; where it *admits* a definition, the author reads "proven safe" when the truth is "not proven unsafe under a conservative approximation." Any decidable-fragment proof engine is incomplete, so the author who has been trained "admitted = comprehensively proven" is exactly the author who stops reasoning about the runtime and is blindsided by the case the model conservatively assumed. The refinement-types family is the concrete evidence of both costs at once: LiquidHaskell (Vazou et al.), F* (Swamy et al.), and Flux for Rust (Lehmann, Geller, Vazou & Jhala, "Flux: Liquid Types for Rust," PLDI 2023) prove bound-like predicates via SMT (Z3) at compile time — and they *over-reject*, being sound but incomplete. Liquid Types (Rondon, Kawaguchi & Jhala, PLDI 2008) deliberately restricts refinements to a decidable, quantifier-free fragment precisely to keep "unknown" answers rare; step outside (nonlinear arithmetic) and the program is rejected even when safe. Users cope with an escalating ladder of manual proof (refinement lemmas, ghost variables, `assume`/`admit` escape hatches). For Precept's stated primary author — "the domain expert, not the developer" — that over-rejection tax is specifically disqualifying: a domain expert cannot discharge an SMT obligation with a ghost lemma.

**So neither non-trivial pole eliminates false security.** PROVE-SOME's failure mode is "clean-compile over-read as covering deferred cases." PROVE-EVERYTHING's is "admitted over-read as exhaustively proven, runtime neglected." The distinguishing property is not which pole has no residual risk — neither does — but which one's residual risk is *legible*. A boundary that says "these obligations are discharged here, those are governed there" is inspectable and calibratable. A boundary that says "everything admitted is safe" is a single opaque claim that fails silently the moment the proof model is incomplete, which it always is.

---

## 4. The hard middle case — provable-violation-on-some-path

This is the genuine crux, and it is where the analyses **disagree**. I will not hand-wave it. The case: a set-action the compiler can prove violates a bound on *some* reachable path but not all — the "exists-violation" bucket. Everything unstable about the boundary concentrates here, and the feeling of arbitrariness comes from treating it as one undifferentiated bucket.

First, the sharpening all analyses accept. Three distinct predicates about a set-action at a position:

- **For-all-violation** — every value that can reach this position under the declared constraints violates the bound; formally `(reachable-constraints AND bound-holds)` is unsatisfiable. This is a pure coherence fact — the definition contains an operation that can never satisfy its own governing bound — decided by the *same* satisfiability engine that already decides UnsatisfiableGuard ("this row can never fire," Diagnostics.cs:770) and ContradictoryRule ("no valid configuration can satisfy both," Diagnostics.cs:802). Fully decidable from the definition; total-coverage-able with no false security, because the predicate *is* the decidable set.
- **Exists-violation** — some reachable value violates and some satisfies; both `(reachable AND NOT-bound)` and `(reachable AND bound)` are satisfiable. The *meta*-fact "a violation is possible" is decidable (a satisfiability query); the *object*-fact "this entity violates" depends on which value actually occurs — runtime.
- **Unprovable** — outside the decidable arithmetic fragment (nonlinear terms), the engine decides neither way. This is the genuinely-incomplete tail.

Two caveats that bind the whole cut:

- The for-all / exists line depends on computing "values reachable to this position," and that computation must **over-approximate** (be sound) for the line to be principled. An over-approximating reachability analysis can misclassify a true for-all as exists (incomplete — it only ever downgrades reject→warn, never fabricates a rejection). An *under*-approximating one could misclassify exists as for-all and *falsely reject*. **The entire no-over-rejection guarantee rests on the reachability analysis being sound in the over-approximating direction — this must be verified in the engine, not assumed.**

### Where the analyses disagree: does an always-violating set-action reject or warn?

Consider a set-action that writes a value which *always* violates its bound — e.g. `set X = 150` on a branch, with `X max 100`. Take the for-all sub-case (the branch always writes a forbidden constant). Two of the analyses give *different dispositions*, and the disagreement is real:

- **The coherence-parity view (conceptual structure):** this is definition incoherence, categorically identical to a default that provably violates its bound — OutOfRange, which the code already treats as **Error** (Diagnostics.cs:697, "Default value {1} for field '{0}' violates declared '{2}'"; the DefaultViolatesRule trigger at Diagnostics.cs:884 describes exactly this — "the satisfiability scan folds… against the field default values and finds it provably false"). A default is just the trivial for-all: a single constant that always violates. So an always-violating set-action is the *same kind of fact* and should be an **Error by parity**, independent of whether the bound feeds a safety proof. On this view the band split's single carries-proof cut is **insufficient** — it would mislabel a genuine definition-incoherence as a mere warning.
- **The governance-checkpoint view (Precept philosophy):** what makes OutOfRange reject is that a *default* has **no later governance checkpoint** — the entity is invalid at creation, before any operation runs, so compile-time is the only checkpoint and reject is forced. A set-action, by contrast, flows through **Fire**, which *does* have a runtime governance checkpoint (philosophy.md:19). If the runtime refuses the write, the invalid configuration is never produced, so the compile-time signal can legitimately be an advisory **warning** even for the always-violating case. On this view the unifying rule is: *reject at compile time when there is no later governance checkpoint (creation defaults); defer to runtime governance when there is one (Fire/Update).*

**These genuinely conflict on the always-violating set-action:** coherence-parity says reject, governance-checkpoint says warn-and-govern. This is the crux inside the crux, and the owner should decide it explicitly rather than let it be decided implicitly by whichever severity the implementation happens to emit. Note the two views *agree* on the two endpoints — a violating default rejects (no checkpoint), and a genuinely runtime-supplied value that only *sometimes* violates is governed at runtime — and disagree only on the always-violating *set-action*, where a runtime checkpoint exists but the definition still ships an operation guaranteed to attempt a forbidden write.

A useful reframing that narrows the disagreement: ask *why* the value is reachable. If the violating value is a **literal or all-paths-forced expression internal to the action** (definition-internal, decidable), the coherence-parity view has real force — the definition ships an operation that always attempts a forbidden write, which is incoherence the author can fix at author time. If the violating value is a **runtime input flowing through the action** (`set X = input`, input possibly 150), whether it violates depends on data the compiler does not have — genuine governance, runtime-enforce, optionally warn that a violation is reachable. The provenance of the violating value — definition-internal versus runtime-supplied — is a principled cut *within* the hard middle, and it is not arbitrary. The residual, un-reconciled question is the definition-internal always-violating set-action: is it "incoherence, reject" (parity with the default) or "governed, warn" (a runtime checkpoint exists)? The analysis surfaces this as an open decision; it does not resolve it, because resolving it is a design/classification act reserved for the owner.

---

## 5. Precept-internal consistency

The owner observed that Precept *already* mixes: some provable structural certainties reject, others only warn. The question is whether that mix is arbitrary (feeding the ratchet) or principled (already encoding the right line). **Finding: the existing mix is principled, not arbitrary — it encodes a real "does this produce an invalid configuration or fault" test — and the band split's job is to place its cases on the correct side of that *same* line.**

Read the severities at HEAD:

**Error bucket** — cases where a *producible or reachable* configuration/computation would violate a declared constraint or fault:

- OutOfRange — a default provably violates its bound — **Error**, Proof stage, Safety category (Diagnostics.cs:697)
- DefaultViolatesRule — a default provably violates a rule — **Error** (Diagnostics.cs:877)
- UnsatisfiableInitialState — initial-state ensure unsatisfiable by defaults — **Error** (Diagnostics.cs:867)
- DivisionByZero — "'{0}' can be zero" on a reachable value — **Error** (Diagnostics.cs:821)
- TerminalStateHasOutgoingEdges — a structural impossibility that would corrupt lifecycle semantics — **Error** (Diagnostics.cs:733)

**Warning bucket** — cases of *dead, vacuous, or over-constraining* structure that cannot by itself produce a bad entity:

- UnsatisfiableGuard — "this row can never fire" (dead code) — **Warning** (Diagnostics.cs:770)
- TautologicalGuard — "no narrowing effect" (no-op) — **Warning** (Diagnostics.cs:781)
- VacuousRule — "governs nothing" — **Warning** (Diagnostics.cs:792)
- ContradictoryRule — "no valid configuration can satisfy both" (uninhabited, but no operation forces the bad value into existence) — **Warning** (Diagnostics.cs:802)
- UnsatisfiableRule — "no valid value can satisfy it" — **Warning** (Diagnostics.cs:814)
- UnreachableState — "unreachable from initial state" (dead graph node) — **Warning** (Diagnostics.cs:707)
- DeadEndState — "no path to any terminal state" — **Warning** (Diagnostics.cs:726)

The dividing line is not "provable vs unprovable" — *everything in both buckets is proven with certainty*. The line is: **does admitting this let an invalid configuration be produced (or a computation fault occur) on a reachable path?** Yes → Error (prevention demands rejection). No — it is merely dead, vacuous, or self-defeating logic that *shrinks* the valid set without admitting a bad entity → Warning (a quality smell, a lint). This is precisely the standard *type-error-versus-lint* distinction that GCC, Roslyn, and TypeScript all draw (an unsatisfiable guard is the exact analogue of an "unreachable code" warning). **So the owner's "some reject, some warn" is not evidence of an arbitrary line — it is an already-encoded, defensible classification.**

Now judge the band split against that latent principle. A set-action *provably* violating a band is, on its face, the Error shape — a producible configuration that violates a declared constraint, structurally identical to OutOfRange. The proposal's carries-proof cut (safety-load-bearing → reject; non-safety → warn) is **orthogonal** to this coherence cut, and misses it. The unresolved tension from Section 4 is exactly this: for the for-all / always-violating sub-case, the coherence-parity view says the existing Error principle *already* dictates Error, regardless of carries-proof; the governance-checkpoint view says the existence of a Fire-time checkpoint moves even this to Warning-plus-runtime-governance. **What is *not* in dispute:** the band split cannot be specified by the carries-proof cut alone. Whether the always-violating set-action lands as Error (coherence parity) or as Warning-plus-enforced-runtime-governance (checkpoint view), the classification must be made against the existing "produces an invalid configuration?" line — not left to fall out of the safety-load-bearing test, which is a different axis.

---

## 6. Prior art — the decision-relevant precedents

Three precedents carry the most decision weight; all are named with their mechanism so the owner can check them.

1. **Gradual verification / gradual refinement types** (Lehmann & Tanter, POPL 2017; Bader, Aldrich & Tanter, VMCAI 2018). *Mechanism:* static checking as far as static knowledge reaches, with a **residual dynamic check materialized at the exact frontier where static proof stops**, yielding a total guarantee from partial static coverage (an adapted gradual guarantee, per Siek et al., SNAPL 2015). *Why decision-relevant:* this is the literal "some static, some dynamic" the owner fears, proved to be soundness-preserving. It is the theorem behind "partial coverage ≠ partial guarantee," and it is the architecture Precept already runs via carries-proof (philosophy.md:55).

2. **SQL CHECK constraints (and NOT NULL, foreign keys).** *Mechanism:* enforced purely at write time inside the transaction; no DDL-time proof that future inserts satisfy them; a violating write is atomically refused. *Why decision-relevant:* this is the pure "govern-at-write" pole, run at planetary scale for decades with no false-security problem — because the guarantee surface is unambiguous (holds on every committed write). It is the structural precedent for treating a band violation the runtime can atomically refuse as a *recoverable governance refusal*, which is exactly how the fault-floor research classified bands.

3. **SPARK/Ada with published assurance levels** (GNATprove; AdaCore stone→…→platinum), alongside the honest-boundary discipline of the total verifiers (Dafny over Boogie/Z3, Frama-C/WP — total *within* their fragment, **reject** at the boundary, unsoundness only via an explicit greppable `assume`). *Mechanism:* an undischarged obligation never silently vanishes — it is displayed as undischarged and either proven, runtime-checked, or explicitly assumed; and the *published assurance level* tells the reader exactly how much is statically proven. *Why decision-relevant:* this is the answer to "how do you keep partial static coverage honest" — a *legible, published guarantee surface*, which is the same move philosophy.md:25 already commits Precept to for approximation.

The through-line: **the correct axis is not prove-everything / prove-nothing / prove-some (effort); it is recoverability (consequence).** Prove-or-reject where failure is unrecoverable, because no runtime governance is possible there (Precept's fault floor, philosophy.md:53 — over-rejection accepted as the forced cost). Defer-and-govern where failure is recoverable, because runtime enforcement makes the guarantee total without over-rejecting (the SQL-CHECK / carries-proof pole). "Prove-some" is false security *only* when the complement of the proven set is dropped rather than governed.

*Citation confidence.* The mechanisms above are reliable. The core citations (Liquid Types PLDI 2008; Flux PLDI 2023; Gradual Refinement Types POPL 2017; Gradual Program Verification VMCAI 2018; Refined Criteria for Gradual Typing SNAPL 2015; SPARK assurance levels; Dafny assume/reject; SQL CHECK) are ones I am confident of. Two attributions I flagged as approximate and did *not* rely on: the exact title/authorship of a gradual-verification-of-C0 paper, and the authorship of a "Gradual Liquid Type Inference" (OOPSLA 2018) paper.

---

## 7. The crux the owner must decide

Stated neutrally, as questions, in the order they gate each other:

1. **What is a declared value-bound — a fault-floor invariant or a governance rule?** Is `min 0 max 100` on a field a compile-time prove-or-reject invariant (like the enumerated faults at philosophy.md:53, with carries-proof extended to it and over-rejection accepted as the owned cost), or a governance rule (runtime-enforced on every operation with no bypass, coherence-checked at compile time, carries-proof *not* extended)? The context states the fault-floor "can it compute?" research already answered *governance* — but that answer is the load-bearing premise for the entire dissolution, and if any bound can render a computation *undefined* rather than merely *refused* (e.g. a `max` that also bounds an array index), that bound is fault-carrying and belongs on the reject floor regardless. **This classification, not the severity policy, is where the argument is won or lost.**

2. **For the always-violating set-action, does coherence-parity or governance-checkpoint win?** (Section 4's unresolved disagreement.) When a set-action is *proven* to always write a forbidden constant, is that a definition-incoherence Error by parity with OutOfRange (Diagnostics.cs:697), or a Warning-plus-runtime-governance case because Fire provides a runtime checkpoint (philosophy.md:19)? The two consistent views of the existing severity table diverge here.

3. **Does the runtime refuse a violating write *today*?** This single empirical fact decides which configuration HEAD is actually in. If yes, HEAD is already the total-guarantee shape and the band split merely moves the *compile-time posture* from redundant-reject to warn while the runtime keeps governing — legitimate, SQL-CHECK-shaped, no hole. If no (the "future sweep" is real), the deferred bounds are *already* unproven-and-ungoverned today, the split relabels an existing silent hole rather than creating it, and the real work is standing up the runtime governance, not adjusting a severity.

4. **Is the reachability/narrowing analysis sound in the over-approximating direction?** The no-over-rejection guarantee depends on it (Section 4). This is verifiable in the engine and must be confirmed, not assumed.

5. **Does any of this touch `docs/philosophy.md`?** Per CLAUDE.md, moving bounds from compile-reject to runtime-govern changes "the constraint model or operation surface," which is on the philosophy flag-list. The honest reading here is that philosophy.md does **not** require an edit to be self-consistent — it already locates rule-enforcement at runtime (philosophy.md:19, 68: "A Precept rule holds because the runtime structurally prevents any operation from producing a result that violates it") and reserves compile-time impossibility for faults (philosophy.md:53), and it makes no false claim that bounds are compile-checked (the clean-compile promise at philosophy.md:57 is a closed three-item enumeration that omits bounds). What philosophy.md is **silent** on is the *classification of a declared bound* as fault-versus-governance — and that silence, not any contradiction, is what makes the circle feel unresolved. Whether to state that classification *in* the philosophy is an **owner-gated act**; this analysis flags the potential gap and does not resolve it or propose an edit.

---

## 8. What this analysis does NOT settle / next step

This is analysis, not design. It establishes the *shape* of the space and shows the circle is escapable in principle; it does not establish that the escape is currently *realized*, and it proposes no syntax, no severity values, and no implementation.

Explicitly unsettled and load-bearing:

- **The runtime backstop's existence and totality** (Section 2, Section 7 Q3). Project state indicates the runtime is still a stub; if the value-bound governance sweep is not built, the "coherence at compile, governance at runtime" split is currently missing its second leg — the exact false-security hole, as a build fact. Not audited here.
- **The over-approximation soundness of reachability** (Section 4, Section 7 Q4). Not verified in the engine.
- **The fault-versus-governance classification of bounds** (Section 7 Q1). Taken as given from the fault-floor research; not re-derived. If wrong for any bound, that bound flips to the reject floor and the SQL-CHECK analogy fails for it.
- **The always-violating set-action disposition** (Section 4, Section 7 Q2). Surfaced as a genuine disagreement between two consistent readings; deliberately not resolved.

**Routing.** If a decision emerges to change the band treatment, it is language-surface / constraint-model work and must go through the lifecycle: `/research` to ground the fault-versus-governance classification and the runtime-governance mechanism, then `/design` to lock the reject/warn/govern classification with four-leg rationale, then `/plan`. Per CLAUDE.md's pre-design consultation gate, the fault-versus-governance classification of bounds (Section 7 Q1) is a conversation to have with the owner *before* any of those skills run — it is the "what should we do" question, and it is the owner's to settle. The single most valuable next action that requires no design skill: **answer Section 7 Q3 by inspecting the runtime** — whether a violating write is refused today decides whether there is a live hole or merely a compile-time-posture question.

---

### The single sharpest finding

The owner's circle breaks at its very first link: **partial static *coverage* is not partial *guarantee*.** Gradual verification proves — as a soundness theorem — that "some static, some dynamic" delivers a *total* guarantee when the runtime check is materialized exactly where static proof stops, and SQL CHECK constraints, design-by-contract, and SPARK run this at industrial scale without misleading anyone. False security comes from a *fuzzy* boundary (cases neither proven nor governed, with nothing marking which is which), never from a *partial* one. Precept already runs this architecture (carries-proof, philosophy.md:55) and its existing Error/Warning split is the principled "does this produce an invalid configuration?" line (Diagnostics.cs:697 vs :770), not an arbitrary one. **Therefore the band split is sound in principle — but it becomes the owner's exact false-security configuration in the one interval where it warns-only while deferring to a runtime governance sweep that does not yet exist. The dispositive test is empirical, not conceptual: does the runtime refuse a violating write today? If yes, no hole. If no, the deferred bounds are already unproven-and-ungoverned, and standing up the runtime governance — not tuning a diagnostic severity — is the real work.**
