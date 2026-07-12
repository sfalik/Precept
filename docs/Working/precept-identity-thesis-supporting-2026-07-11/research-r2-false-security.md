# Research Brief: "False Sense of Security" in Partial/Gradual Verification, and How Tools Communicate Residual Risk

## Framing

This brief is neutral grounding, not a design recommendation. It covers (1) the gradual-typing soundness debate in industry languages, (2) gradual-verification research proper, (3) how partial-proof tools signal the proven/unproven boundary, and (4) synthesis on what measurably calibrates user trust versus what is cosmetic. Section 5 grounds the concern in the repo's own prior work — three Working docs already did substantial independent research on exactly this question for Precept's declared-bound classification; I cite them as existing context, not as conclusions this brief adopts or extends.

---

## 1. The gradual-typing soundness debate

### 1.1 The foundational distinction: sound gradual typing vs. optional/erasure typing

Gradual typing was introduced by **Siek & Taha, "Gradual Typing for Functional Languages," Scheme and Functional Programming Workshop 2006, pp. 81–92**. Their framework is built on *type consistency* (not subtyping) and a soundness contract: "a gradually typed program is sound if typed components can assume that their types accurately describe the values that they receive" (per the framework's own soundness definition, confirmed via cross-source search). Soundness is enforced via **cast insertion** at the boundary between typed and untyped code — the "guarded" semantics that later work calls the **Deep** strategy.

Three distinct enforcement strategies for what happens when a typed and untyped value meet emerged and are named precisely in **Tunnell Wilson, Greenman, Pombrio & Krishnamurthi, "The Behavior of Gradual Types: A User Study," DLS 2018** (read first-hand, `gradual-user-study.pdf`):

- **Deep** (Typed Racket) — inserts higher-order contracts at every typed/untyped boundary; "if a typed expression reduces to a value... the programmer can trust the static types" fully, including through higher-order values (proxies/wrappers protect future inputs and outputs).
- **Shallow** (Reticulated Python) — checks only the top-level shape of a value at the boundary ("ensures that typed code does not 'go wrong' ... every function call targets a callable value"); guarantees are "weak and non-compositional" — a tuple typed `(Number, Number)` might contain a string two levels deep and Shallow will not catch it.
- **Erasure** (TypeScript) — "the Erasure strategy uses types for static analysis, and nothing more. At runtime, any value may flow into any context regardless of the type annotations... despite the complete lack of type soundness, the Erasure strategy is popular" (paper §2.2, direct quote) — because it needs no runtime representation, requires no new semantics for host-language interop, and runs at native speed.

### 1.2 TypeScript's unsoundness is a deliberate, named design tradeoff

TypeScript does not claim soundness. Per **Vanderkam, "The Seven Sources of Unsoundness in TypeScript,"** effectivetypescript.com, 2021-05-06 (fetched first-hand): "soundness is not a design goal of TypeScript at all. Instead, TypeScript favors convenience and the ability to work with existing JavaScript libraries." The seven named sources: `any`, type assertions (`as`), unchecked array/object index lookups, inaccurate `.d.ts` library type definitions, array covariance, control-flow narrowing invalidated by intervening function calls, and one deliberately-unresolved internal edge case ("there are five turtles"). The article's own framing is instructive for the "does documentation fix it" question: it does *not* warn readers up front that trust may be misplaced — it opens by validating the complaint, then pivots to reassurance ("TypeScript is a great language"), and treats the seven sources as things to *learn to route around* rather than a boundary the tool itself displays per-site. That is a documentation-only mitigation, discussed in §4 below.

### 1.3 mypy — optional typing, not gradual-with-soundness-goal

mypy (Python, PEP 484) is explicitly in the **optional typing** camp, which "forgoes enforcement and type soundness altogether" (cross-confirmed via search of the gradual-typing literature framing mypy against Siek/Taha's stricter sense). mypy's own documentation states the caveat directly: `Any` and `# type: ignore` are static-only — "using `Any` or `type: ignore` silences the type checker but does not prevent runtime errors if the actual runtime types don't match your code expects" (mypy.readthedocs.io, `common_issues.html`, confirmed via search of current docs). Anecdotal practitioner reports (Wolt engineering blog, "Professional-grade mypy configuration") describe strict mode surfacing "dozens of latent bugs" in mature codebases when first enabled — evidence that partial coverage *does* find real defects, which is the countervailing case to over-indexing on the false-security risk (a partial checker is not worthless merely because it is partial).

### 1.4 Sorbet (Ruby, Stripe) — the boundary is a first-class type, not a silent default

**sorbet.org/docs/gradual** (fetched first-hand) names the boundary type explicitly: `T.untyped` is "a type in Sorbet's type system... unlike any type found in Go or Java" with two special properties — anything can be asserted into it, and it can be asserted into anything. The docs illustrate the danger directly with a worked example (a variable annotated `String` at compile time actually holding `0` at runtime) and then describe three concrete mitigations Sorbet ships, not just documents:
- **Runtime signature verification** — a `sig` is checked against the actual return value at call time in non-production/test contexts, so an annotation error surfaces as a crash near its source rather than propagating silently.
- **Strictness levels** (`# typed: false/true/strict/strong`) that let a team *ratchet* how much `T.untyped` is tolerated in a file, file by file.
- **`T.reveal_type`** — an explicit, on-demand query the developer runs at a specific call site to see what Sorbet currently believes the type is, making an otherwise invisible inference visible where it matters.

This is a structurally different mitigation shape than TypeScript's: the boundary is a *queryable type*, not an absence of information.

### 1.5 Is Sound Gradual Typing Dead? — why unsound approaches won

**Takikawa, Feltey, Greenman, New, Vitek & Felleisen, "Is Sound Gradual Typing Dead?", POPL 2016**, DOI 10.1145/2837614.2837630 (read first-hand, `popl16-sound-dead.pdf`). Direct quotes: "We find that Typed Racket's cost of soundness is *not* tolerable. If applying our method to other gradual type system implementations yields similar results, then sound gradual typing is dead." Their own benchmark data (read directly from the figures, e.g. `tetris`: max overhead 117×, only 0% of configurations "3/10-usable"; `snake`: max overhead 121×; `kcfa`: mean overhead 9.23×) shows that **enforcing soundness at every typed/untyped boundary is frequently too expensive to ship**, which is the load-bearing reason the industry converged on Erasure/Shallow (TypeScript, mypy-optional, Reticulated/Transient) rather than Deep. This matters directly for the "why does the false-security risk exist at all" question: it is not merely a communication failure — it is downstream of a real cost tradeoff that pushed most production gradual-typing systems toward the unsound end, which is precisely the configuration where the boundary must be communicated well because it cannot be closed by brute-force enforcement.

The response strand — **Vitousek, Siek & Chaudhuri, "Gradual Typing in an Open World"** and **Greenman et al., "Optimizing and Evaluating Transient Gradual Typing," DLS 2019** (Reticulated Python's "Transient" semantics) — tries to recover cheaper checks by only verifying top-level shape at boundaries (first-order checks, no proxies), trading soundness depth for tractable overhead. This is the industrial compromise position between Deep and Erasure.

### 1.6 Direct empirical evidence: what users actually expect vs. what ships

The most directly relevant empirical data point is **Tunnell Wilson et al., DLS 2018** (§1.1 above), because it is the only paper in this literature that surveys *developer* attitudes rather than measuring compiler behavior. Findings, read first-hand from the paper's results section:

- "For both the software engineer and student populations, **Erasure is both Disliked and Unexpected** while the Deep... behavior is **Liked and Expected**." (§4.1, Consensus)
- On a complex, multi-boundary program (Q4/Q7/Q8), a majority of professional software engineers and students **disliked all three behaviors** — even Deep's own error output, when it identifies the fault at the *wrong source-code line* relative to where the mismatch is intuitively understood to originate, is judged wrong by the reader. Out of 34 software engineers who commented on one such question, 24 expected the error at a *different* line than any of the three semantics actually reported.
- This is the paper's central empirical warning for the present brief: **developers' mental model of where a fault should be attributed does not automatically match any of the three shipped semantics** — not even the sound one. A tool can be sound and still mislead about *where* the boundary/fault lives if its attribution doesn't match the reader's causal story.

This is direct evidence that the false-security failure mode is not simply "developers don't know the system is unsound" — even fully-sound Deep semantics can be misread if the *diagnostic surfacing* (which line, which value) doesn't match the reader's model of causation. Legibility of the enforcement point, not just soundness of the enforcement itself, is independently load-bearing.

---

## 2. Gradual verification research (beyond typing)

### 2.1 Gradual Refinement Types

**Lehmann & Tanter, "Gradual Refinement Types," POPL 2017**, DOI 10.1145/3009837.3009856 (abstract confirmed via search). Extends the gradual-typing agenda to logically-refined types (refinement predicates, not just shapes), addressing "imprecise logical information" and dependent function types — i.e., what happens when a refinement predicate is only partially known. The paper introduces a notion of *locality* for refinement formulas so that partial precision composes soundly rather than silently discarding information.

### 2.2 Gradual Program Verification

**Bader, Aldrich & Tanter, "Gradual Program Verification," VMCAI 2018**, LNCS 10747 pp. 25–46 (read first-hand, `gradual-verification-vmcai18.pdf`). Abstract, direct quote: "Both static and dynamic program verification approaches have significant disadvantages when considered in isolation... we propose *gradual verification* to seamlessly and flexibly combine static and dynamic verification... This approach yields, by construction, a gradual verification system that is compatible with the original static system, but overcomes its rigidity by resorting to dynamic verification when desired... the formal semantics... including the gradual guarantees of Siek et al., have been fully mechanized in the Coq proof assistant." Their own motivating example (§1): a static Hoare-logic verifier can *fail* to verify a correct program not because the program is wrong but because "the postcondition... is insufficient to justify" a later call — a false negative from under-specification, not a bug. Their fix is to let a `?` (unknown) marker appear *inside* a contract, so a precondition can be partially specified, and only the unknown part is checked dynamically; the known part stays statically enforced. This is the mechanism directly relevant to "how does a partial-proof system signal its own boundary": the unknown-marker is *inside the specification itself*, at the exact clause that is unproven, not a blanket file-level or project-level flag.

### 2.3 The soundness theorem underneath both

**Siek, Vitousek, Cimini & Boyland, "Refined Criteria for Gradual Typing," SNAPL 2015**, is the paper both Lehmann/Tanter and Bader/Aldrich/Tanter invoke for the **gradual guarantee**: informally, that removing type annotations (making a program "more dynamic") should never change its outcome except by removing type errors that would have manifested as dynamic errors instead — the guarantee is a formal criterion for *when* a partial-static/partial-dynamic system's total behavior is well-behaved, as opposed to merely "sound in patches." This is the theoretical basis for the internal repo doc's claim (see §5) that "partial coverage ≠ partial guarantee" when the dynamic check is materialized exactly at the frontier where static proof stops.

---

## 3. How systems that prove some things and not others signal the boundary

| System | Mechanism | What it actually marks |
|---|---|---|
| **SPARK/Ada (GNATprove)** | Five *published, named* assurance levels: Stone (in the analyzable subset) → Bronze (flow analysis: initialization/aliasing/data races, no proofs) → Silver (Absence of Run-Time Errors: div-by-zero, array OOB, overflow) → Gold (proof of specific integrity properties) → Platinum (full functional correctness). Per-level costs/benefits documented in AdaCore & Thales's "Implementation Guidance for the Adoption of SPARK" (confirmed via AdaCore docs, docs.adacore.com/spark2014-docs). | The level names a **project- or unit-scoped** target, not a per-obligation fact — "Silver as the default target for critical software" is a team decision, and an undischarged proof obligation at a given level does not vanish; it "remains an undischarged check" that must be resolved by proof, review, or runtime assertion (per the internal band-guarantee doc's citation, corroborated independently by the AdaCore level definitions themselves — Bronze explicitly does zero proof, only flow analysis, so nothing pretends to be proven that isn't). |
| **Dafny** | `assert` (must be proven) vs. `assume` (asserted without proof, an explicit escape hatch). Community practice and open issues (dafny-lang/dafny #547, #663 — confirmed via GitHub search) push toward making every `assume` a **compile error unless explicitly annotated**, precisely because an un-flagged `assume` is invisible unsoundness; one issue states the goal plainly: "every occurrence of an assume should give a compiler error... otherwise it is too easy to forget that external methods introduce unsoundness." | A single, greppable keyword at the exact clause that is unproven — not a project-level flag. The mitigation under active development is to make that keyword *impossible to omit accidentally* (compile error unless paired with an opt-in annotation). |
| **Refinement types (LiquidHaskell, F\*, Flux for Rust)** | Restrict refinements to a decidable SMT-checkable fragment (Rondon, Kawaguchi & Jhala, "Liquid Types," PLDI 2008, per the internal band-guarantee doc's citation) and provide `assume`/`admit`/ghost-lemma escape hatches when the SMT solver cannot decide a refinement. | The escape hatches are explicit keywords in source, but using them correctly requires the author to *do proof work* (write a ghost lemma, an auxiliary invariant) — this is the "over-rejection tax" the band-guarantee doc names: a domain-expert author, as opposed to a type-theorist, cannot discharge an SMT obligation with a ghost lemma, so the escape hatch is technically legible but not *usable* by the target audience. |
| **TypeScript** | `any`, type assertions (`as`), `@ts-ignore`/`@ts-expect-error`. | These are per-line markers, but **nothing marks the boundary at the downstream use site once an `any` value has propagated** — `any` is "contagious": once a value is `any`, every further operation on it silently stays unchecked with no local marker at the point of actual failure. This is the sharpest contrast with Sorbet's `T.untyped`, which is a first-class type that a hover/`T.reveal_type` query can surface at any downstream point, not just at the point of original assertion. |
| **mypy** | `Any`, `# type: ignore[code]`. | Same shape as TypeScript: propagation of `Any` is silent past the annotation site; there is no per-downstream-use marker. |

---

## 4. Synthesis: what causes overtrust, and what mitigates it — measured vs. cosmetic

### 4.1 What makes users overtrust the unproven part

Convergent across the literature read above:

1. **A single global claim standing in for a per-site fact.** TypeScript's marketing/rhetorical framing ("types help you catch bugs") and a codebase's aggregate "compiles clean" status both invite the reader to generalize a whole-file or whole-project claim onto every individual site, when the actual guarantee is site-specific and some sites are unchecked. The gradual-verification literature's fix (Bader/Aldrich/Tanter's `?` inside the contract, not a file flag) is a direct rebuttal to this pattern: the unknown-marker is granular, at the clause, not the file.
2. **Silent propagation with no re-marking at downstream use.** `any`/`Any` contagion (TypeScript, mypy) means the *place where things actually go wrong* (a downstream use of a value that started as `any` three functions ago) carries no local signal — the reader has no way to know, standing at that line, that they are relying on unchecked data. Sorbet's `T.untyped`-as-a-real-type and `T.reveal_type` is the structural counter-example: the unsoundness marker travels *with the value*, queryable at any point, not just visible at the origin.
3. **Diagnostic attribution mismatch, independent of soundness.** Tunnell Wilson et al. 2018 shows that even a fully sound system (Deep/Typed Racket) can still mislead a majority of professional engineers when its error message names a different line than the one the reader's causal model expects. Soundness of the *check* and legibility of the *diagnostic* are separate axes; a mismatch on the second axis produces distrust or misplaced trust regardless of the first.
4. **Repeated-caveat fatigue.** A caveat repeated at every mention trains readers to skim past it rather than internalize it (this is the general trust-calibration finding — see below — not specific to any one paper, but stated as an explicit design rationale in the internal Precept doc discussed in §5, which rejects "a repeated per-diagnostic caveat" for exactly this reason).
5. **Cost-driven abandonment of the sound path without a compensating signal.** Takikawa et al. 2016's overhead numbers are the concrete reason most production systems chose Erasure/Shallow over Deep — but the choice to accept unsoundness for performance is a decision made by tool implementers, not something the average user is shown making a tradeoff on. When the cost tradeoff is invisible, the resulting unsoundness reads as a design accident rather than a deliberate, priced choice.

The general human-factors frame for this is **Lee & See, "Trust in Automation: Designing for Appropriate Reliance," Human Factors 46(1), 2004, pp. 50–80** (abstract/framework confirmed via search): trust in an automated system should be *calibrated* to its actual capability; **overtrust leads to overuse/misuse, undertrust leads to disuse**, and the paper's three components of appropriate trust are *calibration* (does perceived capability match actual capability), *resolution* (can the user distinguish fine-grained levels of capability, not just "trustworthy/not"), and *specificity* (is trust tied to the right sub-function, not generalized across the whole system). Mapped onto this literature: a tool that offers only "compiles clean / doesn't compile" has poor *resolution* — it cannot express "proven here, not there" — and poor *specificity* — it invites a single generalized trust judgment across heterogeneous guarantees.

### 4.2 What measurably works (per the literature read above)

- **Per-obligation, queryable disposition** — SPARK's per-check "remains an undischarged check," Sorbet's `T.reveal_type`, Dafny's `assert`/`assume` distinction. All three make the boundary a fact you can ask about *at a specific site*, not a global claim. This directly answers Lee & See's *resolution* and *specificity* criteria.
- **The check materializes exactly at the frontier where static proof stops, not before or after** — this is Bader/Aldrich/Tanter's and Lehmann/Tanter's central mechanism, and it is what makes the *total* guarantee (static ∪ dynamic) hold even though static coverage is partial. A boundary that is off by even one position (checked too early, or deferred past where it should bite) reopens the gap.
- **A greppable/typeable marker that resists silent omission** — Dafny's active push to make bare `assume` a compile error unless paired with an explicit opt-in annotation is direct evidence that "the marker exists" is not sufficient; the marker must also be *hard to introduce by accident* to stay meaningful over time.
- **Narrowing the advertised claim to match actual coverage** — this is documentation, but *precise* documentation of a closed enumeration (rather than open-ended reassurance) is different from cosmetic disclaimer text; the distinguishing test in the literature is whether the claim is falsifiable/checkable against the actual mechanism (SPARK's levels are precisely this: each level has a checkable definition of what it does and does not include).

### 4.3 What is cosmetic (measured to not move the needle, or never actually tested)

- **A prose caveat in documentation, once, without a per-site or per-query surface.** TypeScript's own unsoundness writeups (the "seven sources" article) are exactly this shape — accurate, well-written, and read by a fraction of users, at a time disconnected from the moment they actually rely on an unsound path. No paper in this literature measures documentation-only disclosure as moving trust calibration; the mechanisms that are *shown* to work (SPARK levels, Sorbet's queryable type, Dafny's keyword) are all structural, not textual.
- **A single project-level or file-level flag standing in for per-obligation truth** (e.g., "strict mode: on") — useful for *ratcheting effort* (Sorbet's strictness levels are explicitly framed this way — "manage `T.untyped` in a codebase") but not for telling a reader, at a specific line, whether *this* value is trustworthy. Coarse-grained flags answer "how much of the file is checked," not "is this fact proven."
- **Repeating the same caveat at every occurrence** — explicitly named as counterproductive (trains the reader to filter it out) rather than merely untested; this is stated as a design rationale, not measured in a controlled study, so it should be read as a plausible-and-argued claim rather than an empirically-established one.
- **Marketing-register reassurance ("X is safe," "no bugs")** juxtaposed against a technically precise but narrower guarantee elsewhere in the same material — the exact configuration Tunnell Wilson et al.'s data implies breeds mismatched expectations, though their study measured expectation-mismatch on gradual-typing error semantics specifically, not on marketing copy generally; extending that finding to marketing register is an inference, not something the paper itself tested.

### 4.4 An open tension the literature does not resolve

Every mechanism in §4.2 that "measurably works" also has a documented cost: SPARK levels cost real engineering effort per level (AdaCore's own guidance frames Gold/Platinum as reserved for a "subset of the code" for exactly this reason); Dafny's harder-to-omit `assume` is still an open, unmerged proposal precisely because making it a hard compile error has ergonomic costs for legitimate uses; Sorbet's runtime signature checks add overhead comparable to (though smaller than) the Deep-strategy costs Takikawa et al. measured. **No paper read in this brief demonstrates a mechanism that fully calibrates trust at zero cost** — the field's convergent finding is that legibility is achievable, not that it is free.

---

## 5. Repo grounding — existing internal work on this exact question

Three Working docs in this repository already conducted substantial independent research on the compile-time/runtime guarantee boundary for Precept specifically. I cite them as existing context the user may want to be aware of — **not** as conclusions this neutral brief adopts, extends, or is trying to validate:

- `docs/Working/band-guarantee-boundary-analysis-2026-07-03.md` — a commissioned analysis of the same "false sense of security" question, independently citing much of the same literature (gradual verification POPL 2017/VMCAI 2018, SPARK assurance levels, SQL CHECK constraints, refinement-type over-rejection). Its central finding (§3, direct quote): "partial static coverage is not inherently misleading. It misleads in exactly one configuration — when the guarantee boundary is illegible — and that configuration is fixable." It also names an open, owner-flagged tension between an absolutist philosophy passage and a precise one (`docs/philosophy.md:49` vs `:57`), explicitly not resolved in that doc.
- `docs/Working/prove-govern-classification-and-legibility-2026-07-06.md` (status: Locked 2026-07-06) — a design doc building on the above, which independently converges on "per-rule compiler-derived disposition, surfaced once on the model, projected everywhere" as its Decision 2, citing SPARK's published-levels precedent and explicitly rejecting "a repeated per-diagnostic caveat" (§ Decision 2, Alternatives considered) for the fatigue reason named in §4.3 above.
- `research/architecture/compiler/fault-floor-definition-2026-06-11.md` and `research/architecture/compiler/compile-time-guarantee-boundary-2026-06-10.md` — earlier research establishing the fault-floor/governance split that the above docs build on.

These docs already did the work of applying this literature to Precept's specific classification questions (fault vs. definition-incoherence vs. runtime-governed bounds). This brief does not re-derive or evaluate their conclusions — that is design-track work already in flight, gated by CLAUDE.md's pre-design consultation rules, and outside this brief's neutral-research charge.

---

## Sources

**External (verified first-hand where noted):**
- Siek & Taha, "Gradual Typing for Functional Languages," SFP 2006, pp. 81–92 (title/venue confirmed via search; soundness definition confirmed via search of framework).
- Takikawa, Feltey, Greenman, New, Vitek, Felleisen, "Is Sound Gradual Typing Dead?", POPL 2016 (read first-hand, pp. 1–2, 8–9).
- Tunnell Wilson, Greenman, Pombrio, Krishnamurthi, "The Behavior of Gradual Types: A User Study," DLS 2018 (read first-hand, pp. 1–7).
- Bader, Aldrich, Tanter, "Gradual Program Verification," VMCAI 2018, pp. 25–46 (read first-hand, pp. 1–2/abstract+intro).
- Lehmann & Tanter, "Gradual Refinement Types," POPL 2017 (abstract confirmed via search).
- Siek, Vitousek, Cimini, Boyland, "Refined Criteria for Gradual Typing," SNAPL 2015 (title/role confirmed via search, cited for the gradual guarantee).
- Vitousek, Siek, Chaudhuri, "Gradual Typing in an Open World"; Greenman et al., "Optimizing and Evaluating Transient Gradual Typing," DLS 2019 (confirmed via search).
- Vanderkam, "The Seven Sources of Unsoundness in TypeScript," effectivetypescript.com, 2021 (fetched first-hand).
- mypy documentation, `common_issues.html` (Any/type:ignore caveats, confirmed via search of current docs).
- Sorbet documentation, `sorbet.org/docs/gradual` (fetched first-hand).
- AdaCore, SPARK/GNATprove assurance-level documentation and "Implementation Guidance for the Adoption of SPARK" (confirmed via search of docs.adacore.com).
- Dafny `assume`/`assert` discipline, dafny-lang/dafny GitHub issues #547, #663, #922 (confirmed via search).
- Rondon, Kawaguchi, Jhala, "Liquid Types," PLDI 2008 — cited via `docs/Working/band-guarantee-boundary-analysis-2026-07-03.md` §3; not independently re-fetched in this session.
- Lee & See, "Trust in Automation: Designing for Appropriate Reliance," Human Factors 46(1), 2004, pp. 50–80 (framework confirmed via search).

**Repo (read first-hand this session):**
- `docs/Working/band-guarantee-boundary-analysis-2026-07-03.md`
- `docs/Working/prove-govern-classification-and-legibility-2026-07-06.md`
- `research/architecture/compiler/compile-time-guarantee-boundary-2026-06-10.md`
- `research/architecture/compiler/fault-floor-definition-2026-06-11.md`

**Downloaded artifacts** (for reproducibility, in scratchpad): `/tmp/claude-1000/-home-sfalik-source-repos-Precept/0e47a38b-7c8f-4da8-b6f6-4cfb966d152b/scratchpad/research-pdfs/{gradual-user-study.pdf, popl16-sound-dead.pdf, gradual-verification-vmcai18.pdf}`