# Technical precedent and feasibility for the want document

Written 2026-11-22. Read-only study; nothing in the repository was changed.

**What this covers.** For each technical demand in `docs/Working/what-i-want-2026-07-16.md` (cited below as `want:LINE`), which established techniques and systems already do it, what their known limits are, where Precept's small closed language makes the job easier than the general case, and where it is hard and the design still has work to do. Hard places are written as design problems to solve. None of them is offered as a reason to prove less.

**How claims are sourced.** Repository claims carry `file:line`. Outside claims carry a link in § 9, graded there as *read in the repo's mirrored copy*, *from a web search result this session* (search summaries, not a full reading of the paper), or *textbook background not re-fetched*. Where a point is my own inference from those sources, the text says so.

**A note on one word.** The research folder predates the decision to retire "witness". Several file names there contain it. In this document the thing a proof produces to show a violation is a counterexample, and the thing that records a finished proof is a certificate.

---

## 1. The short version

1. **The proof model the want document asks for is the standard one, and it is old and well understood.** Base case plus one preservation check per handler, per rule, is exactly what Event-B generates (its `INIT` and `INV` proof obligations), what Dafny does for object invariants, and what TLA+ users do with Apalache. Nothing about the model itself is speculative.
2. **Precept's handlers have no loops, so the step that defeats general verifiers does not exist here.** In a loop-free handler the condition that must hold beforehand is computed exactly by substituting the assignments into the rule. No loop invariants, no widening, no fixed-point iteration. Each obligation becomes one question: "do these premises imply this formula?"
3. **For linear arithmetic over exact numbers that question has a complete answer with a tiny certificate.** Farkas' lemma: either there is a list of non-negative multipliers that combines the premises into the goal (a certificate checkable by multiply-and-add), or there is a concrete assignment that satisfies the premises and breaks the goal (a counterexample). There is no third outcome. So in the linear part, "prove or reject with a counterexample" is achievable in full, not approximately.
4. **Four places are hard:** non-linear rules, the fact that `decimal` rounds, rules that are true but not inductive, and collections. Each has known technique behind it. § 7 lists what the design must decide for each.
5. **Three places where the canonical docs, the earlier research, and the want document pull apart** are listed in § 8. The most important: the want document's own accepted example needs two separately declared facts combined in one proof, and `docs/compiler/proof-engine.md:2537-2541` puts exactly that out of scope.

---

## 2. What `research/` already holds

`research/INDEX.md:80-100` is the "Proof engine, proof discharge" section. The files that bear on this task:

| File (under `research/architecture/compiler/` unless noted) | Authored | What it settles | Still usable under the want document? |
|---|---|---|---|
| `solver-free-static-analysis-techniques-survey.md` | 2026-06-06 | Ranks numeric abstract domains by cost. Intervals, difference bounds, octagons, Karr's linear equalities and congruence closure are all polynomial and produce readable evidence (`:53-61`, `:147-151`). Convex polyhedra are exponential in the worst case (`:97-105`). | Yes for the technique inventory. Its ranking was done under a ban on solver-style engines that canon has since replaced with a certificate requirement (`docs/language/precept-language-spec.md:225`), so its "avoid" verdicts need re-reading. |
| `witness-checking-soundness-architecture-survey.md` | 2026-06-06 | A small independent checker over fine-grained per-obligation certificates is the right way to stop a prover bug becoming a false proof (`:160-164`). Simple strategies are re-run by the checker; hard ones ship a certificate (`:166-170`). The checker cannot see an obligation that was never generated (`:147`, `:172-176`). | Yes. It does not cover the runtime load-time call site, which the want document adds (`want:204`). Its proof-carrying-code citations were marked as unfetched (`:143`); § 4 below fills that. |
| `relational-constraint-representation-survey.md` | 2026-06-02 | Precept's "bounded relational closure" is the octagon domain's closure step without the loop-widening part (`:145-150`). | Yes, as far as two-field relations go. |
| `fragment-boundary-corpus-validation-2026-06-05.md` | 2026-06-05 | Names which rule shapes fall outside two-variable difference bounds: three-field sums such as `A + B <= C`, and `X <= c * Y` (`:174-178`). | The shapes are useful. Its percentages count sample files, which `CLAUDE.md` says never to treat as coverage. |
| `interval-vs-value-evaluation-prior-art-2026-06-05.md` | 2026-06-05 | Reasoning over bounds and rejecting an unbounded source is the mainstream model (Astrée, Frama-C, SPARK) (`:151`); "supply a bound" is a well-precedented demand on authors (`:153`). | Yes. |
| `count-cardinality-bound-proof-survey.md` | 2026-06-03 | Adding to a set may or may not raise the count; Dafny, SPARK and the SMT literature all model it that way (`:299-303`, `:343-352`). | The mechanics, yes. Its accepted tradeoff leans on a runtime check after the mutation (`:311-322`, `:366-370`), which the want document forbids (`want:135`, `want:171`) and canon has since dropped (`precept-language-spec.md:258`). |
| `liveness-completability-verification-survey.md` | 2026-06-05 | "Can this operation ever succeed" is a satisfiability question, decidable without a solver for interval and difference constraints (`:160`, `:175`); it must be answered by exhibiting a satisfying input (`:162`). | Yes. It proposes a warning tier for the undecided part (`:151`), which the want document's no-tiers position (`want:173`, `want:189`) does not allow. |
| `divisor-nonzero-under-finite-representation-survey.md`, `underflow-…`, `overflow-in-solver-free-interval-domains.md` | 2026-07-26 | "Non-zero times non-zero is non-zero" is false under finite representation (`divisor…:240-242`); the problem disappears under exact rationals (`:246-249`). Overflow is the cheapest obligation once intervals exist (`overflow…:696-699`); temporal point types have no bound vocabulary to prove against (`overflow…:724-729`). | Yes; written after the want document. |
| `structural-fact-vs-value-citation-obligation-survey.md` | 2026-07-21 | Event-B's "establish at initialisation, preserve at each event" is the same duty Precept places on rules (`:142`); facts fixed by a type declaration generate no obligation in any surveyed system (`:141`). | Yes. |
| `type-proof-stage-contract-survey.md`, `proof-attribution-witness-design-survey.md`, `state-graph-analysis-survey.md` | May–June | How verifiers split work between type checker and prover; how proof results are attributed; how SPIN, Alloy and NuSMV report counterexamples (`state-graph…:215-216`). | Background. |
| `research/philosophy/formal-spec-languages-comparators.md` | — | Event-B's machine model (variables, invariants, guarded events) is structurally the same as Precept's (`:27`). At the time it described Precept as checking constraints at runtime after each operation (`:168`), which is the model the want document replaces. | The structural comparison stands; the "Precept checks at runtime" contrast is out of date. |
| `research/language/rational-vs-decimal-exact-numeric-representation.md` | rev. 2026-07-20 | Moving the prover's internal arithmetic to exact rationals removes `System.Decimal`'s silent rounding at 28 digits from derived bounds without touching the language (`:354-360`). | Yes; central to § 5.3. |
| `compile-time-guarantee-boundary-2026-06-10.md` | 2026-06-10 | An earlier answer to "what is proven and what is governed at runtime". | Superseded as an answer by the want document five weeks later; useful only for lineage. |

**What `research/` does not hold**, and this document adds from outside: the induction-relative-to-declared-rules method and its known limit (§ 3.1); completeness of linear reasoning and what a Farkas certificate is as a format (§ 3.2); how proof-carrying code handles a missing obligation (§ 4); computing the missing premise (§ 3.3); the decision-theory status of non-linear arithmetic (§ 5.2); decidability results for sets with counts and for element-wise quantifiers (§ 5.5); time passing with no event (§ 5.4).

---

## 3. Proving every rule in every reachable configuration

### 3.1 The induction itself

**What is asked.** Base case: defaults and every initial event establish every rule. Step: every handler preserves every rule, from four kinds of premise — field modifiers, argument constraints, the handler's guard, and all rules holding beforehand. Obligations attach to every write site of every field a rule mentions (`want:16-20`). Canon states the same model (`precept-language-spec.md:268`).

**Who does this already.**

- **Event-B and the B method.** Rodin generates, for a machine with invariant `I`: an establishment obligation for initialisation (`I` with the initial values substituted in) and, for each event and each invariant, a preservation obligation whose hypotheses are the invariants plus the event's guard and whose goal is the invariant after the event's assignments. Butler's lecture example is the invariant `x + y = C` with the event `x, y := x+1, y−1`, giving the goal `(x+1) + (y−1) = C` [EB1]. That is the want document's model nearly word for word, including "all rules holding in the pre-state" as a hypothesis. The repo's own research already drew this parallel (`structural-fact-vs-value-citation-obligation-survey.md:142`).
- **Dafny, Why3, SPARK.** All three turn code plus contracts into one logical formula per check by substituting assignments into the goal, then hand the formula to automatic provers. SPARK's "declared range is the starting bound" is the precedent for modifiers acting as premises (`interval-vs-value-evaluation-prior-art-2026-06-05.md:151`).
- **TLA+ with Apalache.** The standard workflow is two queries: the invariant holds in initial states, and one step from any state satisfying the invariant lands in a state satisfying it [AP1].
- **Liquid Haskell.** A refinement on a type (`{v:Int | v /= 0}`) is the direct precedent for "a modifier is a compact spelling of a rule" (`want:22`); checking reduces to "does predicate one imply predicate two" (`research/references/interval-vs-value-evaluation/liquid-haskell-decidable-fragment.md`, lecture excerpts).
- **Ivy.** The closest precedent for the whole stance. Ivy restricts its modelling language on purpose so that every check for a loop-free piece of program lands in a decidable logic, and when a step fails it shows the user a concrete state pair and asks for the missing invariant [IV1]. "Shrink the language so the checks are decidable" is the want document's trade (`want:137`, `want:189`), carried out by a research group over ten years.

**The known limit, and it applies to Precept.** The method proves a set of rules only if the set is *inductive*: preserved by every handler starting from any configuration that satisfies the rules, reachable or not. A rule can be true in every reachable configuration and still fail this check, because the proof is allowed to start from unreachable configurations the rules do not exclude. Every system above has this property; in the TLA+ community it is common knowledge that the inductive invariant has to be written by hand and is usually stronger than the property one cares about [AP1]. Ivy's authors state the reason directly: finding inductive invariants is undecidable even when each individual check is decidable [IV1].

This is not an accident of weak tools. *My inference, from textbook results:* a precept with two integer fields, events that add or subtract one, and guards that test for zero is a two-counter machine with finite control, and reachability for those is undecidable (Minsky) [TB]. The single handlers are loop-free, but events can fire in any order any number of times, so the set of reachable configurations is not computable in general. No technique, present or future, computes it exactly.

**What that means for the design** (not for scope):

- "Every reachable configuration" is delivered by proving the rule set inductive. Where a true rule is not inductive by itself, the compiler rejects, and the author's remedy is to state the supporting rule. That is consistent with prove-or-reject (`want:24`) and costs no expressibility, but the design should say in plain words that this is what "cannot prove" will sometimes mean, because the message "add a guard" is the wrong advice in that case — the right advice is "add a rule".
- A failed step's counterexample is a configuration that satisfies every declared rule and is driven out by one event. It may not be reachable. Base-case counterexamples are always real. The design should make the two kinds distinguishable in the diagnostic, since "here is a configuration your rules allow" and "here is what will happen" are different statements. Apalache reports exactly this pair-of-states form [AP1]; Ivy builds its whole user interaction on it [IV1].
- Precept has one advantage none of those systems have by default: a finite, declared set of lifecycle states. Facts that hold only in some states can be stated per state, which makes many otherwise non-inductive rule sets inductive without the author inventing auxiliary variables.

**Where the small language makes this easier.**

- No loops and no reconverging branches (`precept-language-spec.md:158-164`), so the formula for each obligation is produced by plain substitution, in time linear in the handler. General verifiers spend most of their difficulty on loop invariants; Precept has none to find. Canon makes this point itself (`precept-language-spec.md:174`; `docs/compiler/proof-engine.md:2612`).
- Whole definition in one file (`precept-language-spec.md:172`), so "every write site of every field a rule mentions" (`want:20`) is a finite enumeration over declared rows, state actions and editable-field declarations. General languages cannot enumerate writers of a variable because of aliasing and separate compilation.
- Problem sizes are tiny. A file has tens of fields. Decision procedures that are exponential in the worst case are run on inputs where the exponent is small. The earlier research measured the whole corpus compiling in about 44 ms (`solver-free-static-analysis-techniques-survey.md:11`, `:153`); that is a statement about speed, not about coverage.

**Current canon versus this demand.** Canon admits two gaps: the pre-state premise is carried only where a declared rule contributes an unconditional fact, and the write-site enumeration is asserted in prose but not kept closed by the build (`proof-engine.md:2614`). Both are recorded there as gaps against the target.

### 3.2 Linear rules, including rules that relate several fields

**What is asked.** `Balance >= -OverdraftLimit` proven across `OpenAccount`, `Deposit`, `Withdraw` and `ReduceLimit` (`want:100-103`).

**Technique.** After substitution every one of those obligations is "a conjunction of linear inequalities implies a linear inequality". Over exact rationals this is decided by linear programming, and Farkas' lemma gives the completeness result in § 1 item 3: a valid implication always has a certificate consisting of one non-negative number per premise, and an invalid one always has a concrete counterexample [TB]. Checking the certificate is: multiply each premise by its number, add them up, compare with the goal. Verified checkers for exactly this exist — an Isabelle/HOL formalisation certifies simplex results through Farkas coefficients [FK1], an SMT proof format extends clause-level proofs with Farkas-derived steps at low overhead [FK2], and the Verasco verified static analyser validates its polyhedra operations the same way (`witness-checking-soundness-architecture-survey.md:134`; mirror at `research/references/witness-checking/verasco-jourdan-laporte-blazy-leroy-pichardie-popl2015.txt`).

Abstract-interpretation domains — intervals, difference bounds, octagons, two-variables-per-inequality, Karr's equalities — are cheaper approximations of the same question, built for programs with loops and thousands of variables, where exact reasoning is unaffordable (`solver-free-static-analysis-techniques-survey.md:53-61`). Astrée proved the Airbus A340 and A380 flight-control code free of runtime errors with zero false alarms, but only after its domains were specialised and tuned to that family of programs [AS1]. Two lessons carry over: a narrow, known program family is what makes "no false rejections" reachable, and the tuning has to be designed, not hoped for.

**The worked example already needs more than one fact per proof.** `OpenAccount` is proven from `OpeningBalance >= 0` and `OverdraftLimit >= 0` together (`want:100`). `Deposit` is proven from the pre-state rule and `Amount >= 0` together (`want:101`). Each is a Farkas combination of two separately declared premises with multipliers (1, 1). See § 8 item 1 for why this matters against current canon.

**Limits.**

- Disjunctions, `!=`, and conditional expressions split a problem into cases. The number of cases can double per construct. Standard, and harmless at Precept's sizes, but the design needs a stated cap and a stated behaviour at the cap; canon already requires one (`precept-language-spec.md:225`, criterion 3).
- Integers are harder than rationals. A linear implication can be true over integers and false over rationals (`2x <= 1` implies `x <= 0` only for integers). Deciding linear integer arithmetic is NP-complete and certificates need extra rounding steps [TB]. `integer` fields, `.count`, `floor`/`ceil`/`round` bring this in.
- All of the above assumes the arithmetic is exact. § 5.3.

### 3.3 Rejecting with the missing premise and a counterexample

**What is asked.** When a proof cannot close, name the premise that would close it, show it, explain why, never insert it (`want:24`, `want:179`). Canon adds a counterexample for proven violations and a printed "weakest precondition" for unresolved ones (`precept-language-spec.md:223`, `:229`), with a rule that a counterexample is re-evaluated before it is shown (`docs/compiler/soundness-and-coverage.md:131-138`).

**Technique and precedent.**

- **The missing premise is computable exactly for loop-free code.** It is the rule with the handler's assignments substituted in (Dijkstra's weakest precondition) [TB]. The want document's own examples are this: the suggested guard for `ReduceLimit` is the rule with `NewLimit` substituted for `OverdraftLimit` (`want:103`, `want:179`). The earlier research identified the same mechanism (`solver-free-static-analysis-techniques-survey.md:143`).
- **Making the suggestion small is a studied problem.** The raw substituted formula repeats things the author already declared. Dillig, Dillig and Aiken (PLDI 2012) compute the smallest and most general missing fact such that known facts plus it imply the goal and it does not contradict the known facts, and showed in a user study that presenting those to users improved speed and accuracy [DL1]. That second condition matters for Precept: a suggested guard that contradicts the rules produces a dead row, which the want document also forbids (`want:155`).
- **SPARK** prints a counterexample only when it can confirm the failure and otherwise reports the check with a suggested fix; its user guide sorts failed proofs into missing assertion, tool limitation, prover timeout, or wrong assertion [SP1]. That is the same honesty rule canon adopted (`soundness-and-coverage.md:131-138`).
- **Alloy and TLA+'s TLC** are the strongest counterexample producers in the field, but they search a bounded space and "no counterexample found" is not a proof (`state-graph-analysis-survey.md:215-216`). They are precedent for how to show a counterexample, not for how to prove.

**Where Precept is easier.** In the linear part the counterexample comes free: the same linear-programming run that fails to find a certificate returns a point satisfying the premises and violating the goal (§ 3.2). There is no separate search.

**Design problems.**

1. *Which message for which failure.* A failed step can mean "add a guard", "bound the argument", or "your rules are not inductive; add a rule" (§ 3.1). The design needs a rule for choosing, or it will tell authors to guard something that needs a supporting rule.
2. *Counterexample values an author can read.* A raw solution point can be an awkward fraction or a number with more decimal places than the field allows. The values shown have to be values the field could actually hold. Canon's current design shows counterexamples only for field values and falls back to "unresolved" for arguments and collection elements (`soundness-and-coverage.md:131-138`); under the want document every row of the mutation table (`want:112-127`) should get a concrete case, and most of those rows involve arguments.
3. *When two different premises would each close the proof.* The want document's examples sometimes offer both ("add a guard or bound the arg", `want:114`). The design needs to say which is shown first and why.

---

## 4. Certificates checked at compile time and at runtime load

**What is asked.** Per obligation, a record of the theorem, the premises used and the derivation; one small checker called from a compiler stage and from a load-time gate; the runtime refuses a definition whose certificate does not verify; the premise list tells the runtime which checks it must run (`want:199-211`). Canon carries the same target and says plainly that none of it is built (`soundness-and-coverage.md:153-176`; `proof-engine.md:2534`).

**Precedent.**

- **Proof-carrying code** (Necula, POPL 1997): the receiver of untrusted code publishes a safety policy, the producer ships a proof, and the receiver validates it with a simple checker before running the code [PC1]. This is the load-time gate.
- **Certifying algorithms** (McConnell, Mehlhorn, Näher, Schweitzer, 2011): a program returns its answer together with evidence a much simpler program can check; trust moves to the checker and shrinks only if the checker is both simple and correct (`witness-checking-soundness-architecture-survey.md:132`, `:148`; mirrored paper in `research/references/witness-checking/`).
- **Proof-assistant kernels** (LCF, Coq, Lean): every theorem passes through a small set of primitive inference steps (`witness-checking-…:136`).
- **Fine-grained beats coarse.** SAT solvers' DRAT proofs are cheap to check because each step is tiny; the SMT format Alethe has coarse steps that push work back into the checker (`witness-checking-…:137-138`). A certificate that says "proved by strategy 4" is the coarse kind.

**The most important thing the earlier survey left open has a precedent answer.** The survey notes that a checker cannot notice an obligation the prover never generated (`witness-checking-…:147`, `:172-176`), and canon repeats it (`soundness-and-coverage.md:60-63`). In proof-carrying code the receiver does not take the list of obligations from the producer. It computes the safety condition from the code itself, using its own published policy, and then checks the supplied proof against that [PC1]. *Carried over to Precept, as my inference:* the checker would regenerate every obligation from the loaded definition — rules × write sites, by substitution, which § 3.1 says is linear-time — and require a valid certificate for each. Then a missing obligation fails the check the same way a wrong proof does, and "stale or tampered" (`want:204`) is caught because the certificates no longer match the obligations the checker derived, with no reliance on signatures or hashes. The cost is that the obligation generator becomes part of the trusted checker, so it has to be small too. For a loop-free language it can be.

**Where Precept is easier.** Each certificate is a handful of numbers and references. Farkas certificates and equality chains are checked in one pass (`witness-checking-…:155`). Proof-carrying code had to encode proofs in a general logic for machine code; Precept's vocabulary of proof steps can be a short closed list, which canon already requires (`precept-language-spec.md:225`, criterion 1, naming a `CertificateSteps` catalog that `CLAUDE.md` records as not existing yet).

**Design problems.**

1. *One definition of each operation, shared three ways.* The prover, the checker and the runtime evaluator must agree on what `/`, `round`, date arithmetic and every other operation compute. A certificate checked against a mathematical model the evaluator does not follow proves nothing about the running system. `research/architecture/compiler/static-vs-runtime-expression-evaluation-survey.md` covers how other systems keep two evaluators in agreement; three is the case here.
2. *The checker's own arithmetic must be exact* (rationals of unbounded size), or it inherits the rounding problem in § 5.3.
3. *How the checker is trusted.* The earlier survey leaves "exhaustively tested or formally verified" open (`witness-checking-…:189`). It is still open.
4. *The premise list as the runtime's list of required checks* (`want:210`) has no direct precedent I found. It follows from the certificate format — collect every premise cited by any certificate — but only if premises are cited by identity (this guard on this row), which canon's format requires by source location (`soundness-and-coverage.md:144-151`).
5. *The editable-field case.* An edit is proven by evaluating the rules at the moment of the edit (`want:143`). That is a premise of a different kind from a guard, and the certificate vocabulary needs a step for it.

---

## 5. The demanding arithmetic and data

### 5.1 Faults: division by zero, overflow, out of range

**What is asked.** Faults get the same premise-and-certificate treatment as rules, and the runtime then omits the checks (`want:104`, `want:165`).

**Precedent.** This is what SPARK's "absence of run-time errors" level and Astrée do for a living. A divisor obligation is discharged by a declared constraint on the divisor in Liquid Haskell and SPARK (`interval-vs-value-evaluation-prior-art-2026-06-05.md:153`). Overflow needs nothing beyond the interval already computed for each expression (`overflow-in-solver-free-interval-domains.md:696-699`).

**Limit that matters.** A divisor that is itself computed is not proven non-zero by its parts being non-zero when the arithmetic can round to zero (`divisor-nonzero-under-finite-representation-survey.md:240-242`). And overflow for `integer`, `number` and temporal types has no decided model in canon (`docs/language/primitive-types.md:697-701`; `precept-language-spec.md:272`). The design has to decide that model before "a definition that compiles cannot fault" is a complete statement; the question of *when it is built* is separate and does not change what the design must say.

### 5.2 Non-linear rules

**What is asked.** A rule whose condition after the write multiplies or divides fields is proven only when a guard or argument constraint states that condition directly; otherwise rejected, naming the guard. Never checked after the mutation, never handed to a solver the compiler does not carry (`want:105`, `want:123`, `want:135`). Canon matches (`precept-language-spec.md:270`).

**Where the field stands.**

- Non-linear arithmetic over the reals is decidable (Tarski) but the general procedure is doubly exponential, and over the integers it is undecidable (Matiyasevich's answer to Hilbert's tenth problem) [TB].
- Every production verifier treats it as its weak point. SPARK's user guide says automatic provers "often" fail on simple checks involving multiplication, division or modulo, and sends users to a library of hand-proved lemmas [SP1][SP2]. The Ironclad team first reported that small unrelated edits made non-linear Dafny proofs flip between passing and failing, a problem later measured systematically [DF2]; Dafny has switches to reduce automatic non-linear reasoning [DF1]. Verus turns non-linear reasoning off by default and has users opt in per assertion [VR1].

So the want document's line — do not attempt algebra the compiler cannot do reliably — is where the most experienced verifier teams ended up. It is the mainstream position, not a retreat.

**The technique that implements that line exactly.** Treat each product or quotient as an opaque named quantity, and reason linearly about those names plus the fact that the same expression has the same value. That combination (linear arithmetic with uninterpreted terms, decided by linear programming plus congruence closure) is decidable, and congruence closure produces an explanation chain as it goes (`solver-free-static-analysis-techniques-survey.md:109-115`). The earlier research calls the idea "atomization" (`fragment-boundary-corpus-validation-2026-06-05.md:174`). Under it, `PlanRepayment`'s guard closes the cap rule because guard and goal contain the same two opaque terms in the same inequality, and deleting the guard conjunct leaves the goal with no premise mentioning those terms — which is the rejection the want document specifies (`want:121`, `want:123`).

**Design problems.**

1. *What "states the condition directly" means precisely.* Identical text? Identical after renaming the argument to the field it is assigned to (`ReduceLimit.NewLimit` for `OverdraftLimit`, `want:75`)? Identical after reordering a product? Each widening is a claim that two expressions always evaluate to the same value, and under rounding arithmetic some algebraically true rewrites are false (`a * b / c` versus `a / c * b`). The definition has to be stated against the evaluation semantics.
2. *The cases between "restated verbatim" and "general algebra".* Some non-linear facts need no search at all: a product of two non-negative values is non-negative; raising one factor of a product whose other factor is positive does not lower it. An event that *raises* `OverdraftLimit` preserves the cap rule by the second fact. The want document's text would have the author restate the condition there too (`want:135`). That is a consistent position; the design should state whether sign and monotonicity facts are part of the proof vocabulary, with its reasons either way, so that it is a recorded decision.
3. *Multiplying by a constant is linear.* `X <= 0.25 * Y` needs no special handling under § 3.2. The line is at field-times-field, and the design should draw it there in so many words (the earlier research lists this shape as a gap only because the engine was limited to unit coefficients, `fragment-boundary-…:176`).

### 5.3 Decimal arithmetic

**What is asked.** The worked example is all `decimal` (`want:33-37`), and its proofs are stated as ordinary algebra over real numbers.

**The difficulty.** Canon calls `decimal` exact (`primitive-types.md:82`, `:230`) and also says division yields "full 28-digit precision" (`primitive-types.md:239`). Those two statements together mean decimal arithmetic is exact only while results fit; beyond that it rounds, silently in `System.Decimal` (`rational-vs-decimal-exact-numeric-representation.md:211-216`, `:358-359`). A proof done over real numbers is then a proof about a different function from the one the evaluator runs.

**Precedent.**

- For binary floating point this is a solved-with-effort problem. Miné's relational domains for floating point and Astrée model each operation as the real operation plus a bounded error (`research/references/solver-free-static-analysis/mine-floating-point-relational-domains-esop2004.txt`; `astree-analyzer-cousot-et-al-esop2005.txt`). SMT solvers have an exact theory of floating point.
- For *decimal* there is much less. The SMT-LIB floating-point theory covers the binary formats only [FP1]. I found no established verifier that reasons about base-10 rounding. *This is an area where Precept has little prior art to lean on, and the absence is itself a finding.*
- Exact rationals remove the problem for the prover (`rational-vs-decimal-…:354-360`; `divisor-nonzero-…:246-249`), and that is how SMT's real-arithmetic theory works.

**Where Precept is easier.** Business decimals have declared scale and bounds far more often than program variables do. `maxplaces` and `min`/`max` (`primitive-types.md:249`, `:581`) are exactly the facts needed to prove that a sum or product is computed without rounding: bounded magnitude and bounded decimal places in, bounded magnitude and places out. And rounding to nearest is monotone — a larger exact result never rounds to a smaller stored one — so many order facts survive rounding even where exactness does not [TB].

**Design problems.**

1. *A written arithmetic model for `decimal`*: for each operation, when the result is exact and what it is when it is not. Without it nothing in § 3.2 is sound for decimals, and the checker in § 4 has nothing to check against.
2. *What to do when exactness cannot be shown.* The candidates the field offers are: make "this operation does not round" an obligation the author discharges with bounds; reason with an explicit error term as Astrée does; or change what the evaluator computes so the question does not arise. Each keeps the guarantee whole. Choosing among them is a design decision with a language-facing side, so it belongs in a conversation with the owner before a design pass.
3. *Division.* A quotient is the one decimal operation that rounds routinely. The want document's `PlanRepayment` proofs survive this because the guard and the assignment compute the same quotient (`want:68-69`). But the companion claim that `Balance < 0` makes `-Balance / Months` non-negative (`want:104`) is a claim about a rounded quotient and needs the monotonicity argument above, stated as a proof step.
4. *`number`.* Canon says the proof engine cannot prove exact equality on the approximate type (`primitive-types.md:83`). Order facts over binary floating point are provable by the Astrée and Miné methods. The design needs to say what rules over `number` fields are provable, since "cannot prove, therefore reject" for every such rule would make the type unusable in rules.

### 5.4 Temporal arithmetic, and time passing

**Arithmetic.** Instants and fixed durations are counts of ticks, so comparisons and sums over them are linear integer arithmetic and all of § 3.2 applies. Constraints of the form "this time minus that time is at most so much" are difference bounds, the representation timed-automata tools use for clocks (`research/references/liveness-completability/dbm-wikipedia.md`). The earlier research found every date ordering it looked at to be of this form (`fragment-boundary-corpus-validation-2026-06-05.md:174`).

Calendar arithmetic is where it stops being linear. Adding a month is not adding a fixed number of days, clamps at month ends, and is not reversible; time-zone conversion depends on external tables. The standard treatment for operations like these is to reason about them only through a short list of stated properties (adding a positive period does not move a date earlier; one month is between 28 and 31 days) and treat them as opaque otherwise, as in § 5.2. Which properties, for which operations, is a list the design has to write and the checker has to share. Prior research on dividing by a time span already separates the sound fixed-duration case from the calendar-period case (`research/language/division-by-temporal-span-prior-art.md`, per `research/INDEX.md:44`).

Overflow of temporal values has no premise to prove from, because temporal point types take no bounds today (`primitive-types.md:701`; `overflow-in-solver-free-interval-domains.md:724-729`). Named there as an owner conversation; still one.

**Time passing with no event.** The want document leaves open whether a rule may mention the current moment at all (`want:219`). Canon currently shows one that does: `rule now() - IncidentAt <= '72 hours'` (`docs/language/temporal-type-system.md:577`), and accepts that such a rule gives different answers at different times (`:568`). Under the induction that rule can be true after every event and become false an hour later with nothing having happened, which is precisely the hole the want document describes.

Precedent offers one clean way to think about it. In Event-B and TLA+ models, time is an ordinary variable advanced by its own event, and that event has to preserve every invariant like any other [TB]. Read that way, a rule mentioning the current moment is acceptable exactly when it stays true as time moves forward: `CreatedAt <= now` passes, `now <= Deadline` fails. That test is a linear check of the kind in § 3.2. Timed automata take the other route — a location's invariant simply stops time from advancing — which is available to a model and not to a system running against a real clock [TB]. I am naming this as a way to frame the open question, not proposing an answer or any syntax; it is the owner's question (`want:219`).

### 5.5 Collections

**What is asked.** The want document's example has no collection, but its promise covers every rule, and canon makes count bounds rules (`precept-language-spec.md:258`) and has locked element-wise predicates `each`, `any`, `no` (`docs/language/collection-types.md:800-812`).

**Counts.** A count is an integer changed by each action by a known amount, so it joins the linear reasoning of § 3.2: `add` raises it by exactly one for a list or queue, by zero or one for a set (`count-cardinality-bound-proof-survey.md:343-352`, matching Dafny's set axioms mirrored at `research/references/count-cardinality-proof/dafny-prelude-set-cardinality-axioms.md`). Under prove-or-reject the set case costs the author a guard on the count when the bound is tight; nothing unsound and nothing deferred. SPARK's containers make the same demand as a precondition (`count-cardinality-…:386-390`).

Reasoning about several sets and their sizes together (unions, overlaps) is decidable — Kuncak and Rinard's logic of sets with size constraints — but expensive; the quantifier-free part is in non-deterministic exponential time [BA1]. Precept's collections have no union or intersection actions that I saw in the action tables, so this cost should not arise unless they are added.

**Element-wise predicates.** Canon says a quantifier over a finite collection "unfolds to a finite conjunction" (`precept-language-spec.md:160`). That describes evaluation at runtime. At compile time the collection's size and contents are unknown, so there is nothing to unfold; the proof has to be per action. Precedent: Bradley, Manna and Sipma's array property fragment decides "for every element, P" style formulas by reducing them to finitely many instances, and shows that several natural extensions are undecidable [AR1]; Ivy restricts itself to universally quantified invariants for the same reason [IV1]. The per-action argument is simple for the universal forms: "every element satisfies P" survives `add e` exactly when `P(e)` holds, and survives any removal. It is not symmetric: "some element satisfies P" survives additions and can be broken by any removal, and the proof needs to know which element left.

**Design problems.**

1. *A preservation rule per collection action per predicate form* (`each`, `no`, `any`, membership, key presence, `.min`/`.max`, ordering in the priority-ordered kinds). That is a finite table, since both lists are closed, and it is the collections analogue of the write-site enumeration. I did not find it written in canon; canon marks the quantifier work "not yet implemented" (`collection-types.md:802`).
2. *Existential rules under removal.* `any` after `remove`, `dequeue` or `pop` has no cheap general proof. The honest options are a premise the author states (the removed element is not the only one satisfying P, expressed through a count the definition maintains) or rejection. The design must say which rule shapes are provable so that authors are not left guessing.
3. *Rules mixing element values with fields* ("every line amount is at most `Limit`") are threatened by writes to `Limit` as well as by additions — the symmetric write site of `want:20` with a collection on one side. Lowering `Limit` cannot be proven safe by substitution, because the elements are not individually visible; it needs a maintained bound or a guard over an aggregate.
4. *Membership across removal* is recorded in canon as an open refinement (`precept-language-spec.md:258`, final sentence).

### 5.6 Shape of the lifecycle: reachable states, dead ends, dead rows

**What is asked.** Every state reachable, no dead ends unless marked terminal, and a row whose guard can never be true is rejected, with the consequence for reachability following from it (`want:149-155`, `want:125-127`).

**Precedent.** Graph reachability and dominators are textbook and already in canon (`precept-language-spec.md:184-189`). A dead row is a satisfiability question: do the rules plus this guard have any solution? In the linear part that is the same linear-programming question as § 3.2 with a certificate of the same kind when the answer is no (`solver-free-static-analysis-techniques-survey.md:141`; `liveness-completability-verification-survey.md:160`).

**Limit.** By the counter-machine argument in § 3.1, whether a state is reachable *taking guards and data into account* is undecidable in general. What can be decided is reachability relative to the declared rules: a row is dead if its guard contradicts the rules that hold in its source state. That is what the want document's example does (`want:127`: the guard contradicts the rule). A row whose guard is consistent with every declared rule but which no actual run ever satisfies will not be caught unless the author's rules are strong enough to exclude it. Canon states the over-approximation openly (`precept-language-spec.md:191`). The design should carry the same sentence into the statement of the guarantee, so that "every state is reachable" is read as "no state is provably unreachable from what was declared".

"No dead ends" has the opposite direction of risk, as the earlier research noted: to promise a state can be left, the compiler has to exhibit an input that satisfies some exit row's guard, and treating an undecided case as "can be left" would be unsound (`liveness-completability-verification-survey.md:162`).

---

## 6. Where the closed, small domain makes the problem easier — collected

| General-case difficulty | Why it is absent or small in Precept | Source |
|---|---|---|
| Loop invariants must be found or supplied | No loops, no recursion, no branches that rejoin; one substitution per handler | `precept-language-spec.md:158-164`, `:174` |
| Aliasing and separate compilation hide who writes a variable | One file, no imports, fields written only by declared actions and declared edits | `precept-language-spec.md:172`; `want:141` |
| Open-ended types and user functions need general logics | Closed type vocabulary, closed function catalog, closed action list | `precept-language-spec.md:166` |
| Thousands of variables force cheap approximate domains | Tens of fields; exact linear reasoning is affordable on every keystroke | `solver-free-static-analysis-techniques-survey.md:153` |
| Proof search in a general solver is unpredictable | Search may be anything; only the certificate has authority, and the certificate vocabulary is a short closed list | `precept-language-spec.md:225` |
| Proof-carrying code needs a general proof logic | Certificates are multipliers, equality chains and case splits | § 3.2, § 4 |
| Authors cannot be asked to restructure a program for the prover | The language is the negotiable part by the owner's own rule | `want:137`, `want:189` |
| Invariants must be invented with auxiliary variables | A finite declared lifecycle lets facts be stated per state | § 3.1 |

---

## 7. Where full proof power is hard, as design problems

Ordered by how much of the promise depends on them.

1. **A written arithmetic model for `decimal`, `integer`, `number` and temporal values** — what each operation computes, when it is exact, what overflow is. Every proof and every certificate check is relative to it. (§ 5.1, § 5.3, § 5.4)
2. **Linear reasoning that combines several declared premises, with Farkas multipliers as the certificate.** The worked example needs it; precedent says it is complete over exact numbers; current canon excludes it. (§ 3.2, § 8 item 1)
3. **The checker regenerates the obligations.** Otherwise the certificate check cannot detect a missing obligation, which canon already names as the remaining hole. (§ 4)
4. **Rules that are true but not inductive.** The compiler will reject them; the design must say so, distinguish that diagnostic from a missing guard, and present the step's counterexample as a configuration the rules allow. (§ 3.1, § 3.3)
5. **The exact meaning of "the premise states the condition directly"** for non-linear rules, and a recorded decision on sign and monotonicity facts. (§ 5.2)
6. **Integer reasoning** — `integer` fields, counts, and rounding functions make some true implications unprovable by rational reasoning alone; the certificate vocabulary needs steps for it. (§ 3.2)
7. **A preservation table for collections**: every action against every predicate form, including the existential ones that removal can break. (§ 5.5)
8. **Counterexample values that are legal field values**, for arguments as well as fields. (§ 3.3)
9. **Calendar operations**: the list of properties the prover may use. (§ 5.4)
10. **Rules that mention the current moment** — open by the owner's own statement. (§ 5.4)
11. **One definition of each operation shared by prover, checker and evaluator.** (§ 4)
12. **Wording of the reachability guarantee** relative to declared rules. (§ 5.6)

None of these says a class of rule cannot be proven in principle, with two exceptions that are facts of mathematics and not of effort: exact reachability over unbounded integers (§ 3.1) and general non-linear integer arithmetic (§ 5.2). In both, the want document's own method — prove from declared premises or reject, and let the author state more — is the response the field uses.

---

## 8. Where canon, earlier research and the want document pull apart

These are observations for a fresh check against the documents, not rulings.

1. **Combining facts.** `docs/compiler/proof-engine.md:2537-2541` defers "combining two or more separately-declared facts to prove a bound neither states alone" and argues it never adds expressibility because the author can state the combined bound as one rule. `precept-language-spec.md:256` keeps the same single-step limit. But `OpenAccount` and `Deposit` in the want document are each proven from two separately declared facts (`want:100-101`), and in `Deposit` one of the two is a constraint on an event argument, which cannot be folded into a rule over fields. As I read it, the accepted example cannot be proven under the deferral as written. The same section of the proof-engine doc lists "multi-term single-fact + guard-sum matching" as a planned strategy (`proof-engine.md:2535`), which may be intended to cover part of this; whether it covers `Deposit` should be checked by someone reading that design closely.
2. **Research that assumed a runtime check behind the proof.** `count-cardinality-bound-proof-survey.md:311-322` and `:366-370` accept incompleteness "because a runtime mechanism backstops it". `liveness-completability-verification-survey.md:151` proposes a warning tier. `compile-time-guarantee-boundary-2026-06-10.md` draws a boundary the want document redrew. These predate the want document; their technique findings stand and their accepted tradeoffs do not.
3. **A rule mentioning `now()` appears in canon** (`temporal-type-system.md:577`) while the want document says whether such rules exist at all is deliberately open (`want:219`).

Also worth one line: the admissibility rule in `precept-language-spec.md:225` already replaced the earlier ban on solver-style search with "authority rests in the certificate". The want document's phrase "a solver the compiler doesn't carry" (`want:135`) is compatible with that: what is excluded is trusting an answer that comes without a checkable certificate, not any particular search method. The research written under the old ban (`solver-free-static-analysis-techniques-survey.md`, `interval-vs-value-evaluation-prior-art-2026-06-05.md:155`) should be read with that in mind.

---

## 9. Outside sources

**From web search results this session** (summaries and abstracts; the papers were not read in full):

- [EB1] Event-B proof obligations, invariant preservation and initialisation; Butler lecture notes and Abrial et al. — <https://eprints.soton.ac.uk/390481/2/Lecture_202.pdf>, <https://web-archive.southampton.ac.uk/deploy-eprints.ecs.soton.ac.uk/130/1/main.pdf>, Rodin Handbook <https://www3.hhu.de/stups/handbook/rodin/current/html/modeling_notation.html>
- [AP1] Apalache and inductive invariants — "Specification and Verification with the TLA+ Trifecta" <https://arxiv.org/pdf/2211.07216>; ChonkyBFT case study <https://arxiv.org/pdf/2503.15380>; TLA+ list discussion <https://groups.google.com/g/tlaplus/c/b1WdYOGsUe4/m/v2fH4tIvCQAJ>
- [IV1] Padon, McMillan, Panda, Sagiv, Shoham, "Ivy: Safety Verification by Interactive Generalization", PLDI 2016 — <https://ilyasergey.net/CS6213/_static/papers/ivy.pdf>; project page <https://cs.stanford.edu/~padon/ivy/>; Shoham talk <https://www.tau.ac.il/~sharonshoham/talks/sas18.pdf>
- [FK1] Verified simplex with Farkas certificates in Isabelle/HOL — <https://link.springer.com/chapter/10.1007/978-3-030-29007-8_13>
- [FK2] Otoni et al., "Theory-Specific Proof Steps Witnessing Correctness of SMT Executions", DAC 2021 — <https://verify.inf.usi.ch/sites/default/files/Theory-Specific_Proof_Steps_Witnessing_Correctness_of_SMT_Executions_(DAC21).pdf>
- [AS1] Astrée on Airbus flight-control software — <https://www.astree.ens.fr/>; Kästner et al. 2010 <https://cs.nyu.edu/~pmc309/publications.www/KastnerEtAl-ERTS2-May-2010.pdf>; Delmas and Souyris (Airbus) <https://www.di.ens.fr/~delmas/papers/sas07.pdf>
- [DL1] Dillig, Dillig, Aiken, "Automated Error Diagnosis Using Abductive Inference", PLDI 2012 — <https://www.cs.utexas.edu/~isil/pldi022-dillig.pdf>
- [SP1] SPARK User's Guide, "How to Investigate Unproved Checks" — <https://docs.adacore.com/spark2014-docs/html/ug/en/source/how_to_investigate_unproved_checks.html>
- [SP2] AdaCore, "GNATprove Tips and Tricks: Using the Lemma Library" — <https://blog.adacore.com/gnatprove-tips-and-tricks-using-the-lemma-library>
- [DF1] Dafny FAQ on non-linear arithmetic — <https://dafny.org/dafny/HowToFAQ/FAQNonlinearArith>
- [DF2] Zhou et al., "Mariposa: Measuring SMT Instability in Automated Program Verification" — <https://www.andrew.cmu.edu/user/bparno/papers/mariposa.pdf>
- [VR1] Verus guide, non-linear arithmetic — <https://verus-lang.github.io/verus/guide/nonlinear.html>
- [PC1] Necula, "Proof-Carrying Code", POPL 1997 — abstract <https://www.cs.cmu.edu/~fox/foxnet/papers/necula-popl97.abstract>; paper copy <https://courses.grainger.illinois.edu/cs421/fa2010/papers/necula-pcc.pdf>
- [BA1] Kuncak, Nguyen, Rinard on sets with size constraints — <https://arxiv.org/abs/cs/0407045>; quantifier-free fragment <https://infoscience.epfl.ch/record/110254/files/KuncakRinard07TowardsEfficientSatisfiabilityCheckingBoolean.pdf>
- [AR1] Bradley, Manna, Sipma, "What's Decidable About Arrays?", VMCAI 2006 — <https://link.springer.com/doi/10.1007/11609773_28>
- [FP1] SMT-LIB FloatingPoint theory (binary formats only) — <https://smt-lib.org/theories-FloatingPoint.shtml>

**Read in the repo's mirrored copies** (via the research files that quote them): Miné and Jeannet on Apron; Miné on octagons; Müller-Olm and Seidl on Karr's equalities; Simon, King and Howe on two-variable inequalities; Bagnara, Hill and Zaffanella on the Parma Polyhedra Library; Nieuwenhuis and Oliveras on congruence closure; McConnell et al. on certifying algorithms; Jourdan et al. on Verasco; the DRAT and Alethe proof formats; the Liquid Haskell course; Dafny's set axioms; SPARK's container contracts. Paths are under `research/references/`.

**[TB] Textbook background, not re-fetched this session.** Farkas' lemma and linear-programming duality; Dijkstra's weakest precondition; Minsky's undecidability of two-counter machines; Tarski's decision procedure for real arithmetic and its doubly exponential cost; Matiyasevich's theorem; NP-completeness of linear integer arithmetic; monotonicity of correctly rounded operations; time modelled as an event in Event-B and TLA+; invariants on locations in timed automata; Why3's design as a front end that splits obligations and sends them to several provers. Each is standard, but a design that rests on one of them should have the primary source fetched and mirrored under `research/references/` first, as the research skill requires.

**Not established here.** I found no verifier that reasons about base-10 rounding, and searched only briefly; a dedicated search is warranted before the decimal model in § 7 item 1 is designed. I did not read `docs/compiler/proof-engine.md` end to end (2,653 lines); the claims about it rest on the lines cited. I did not verify the collection action tables for union-like actions beyond a scan of headings. The counter-machine argument in § 3.1 and the "checker regenerates obligations" reading in § 4 are my inferences and should be tested by a fresh reader before anything is built on them.
