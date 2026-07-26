# The walk — every item arrives with its answer

**Prepared**: 2026-07-26 (slice 1.4; replaces the 1.3 version of this file). **Items: 39** (40 marks —
item 1 splits). **Estimated sitting: 15–20 minutes, one sitting.** **Marks that open a new decision:
zero.**

Every item now carries the answer worked out on the merits — from what you actually wrote, from canon,
from the reset plan — independent of who is claimed to have decided it. So the marks mean:

- *mine* — you said it. The item was already answered.
- *not mine* — **the printed answer stands in place of the claimed ruling. No follow-up, nothing to
  decide later.**
- *don't remember* — same consequence as *not mine*: the printed answer stands, recorded as derived
  rather than ruled.

Where an answer says "either is defensible", the item is a preference and the mark is one glance. The
genuinely expensive questions — **six** — are at the end under "Waiting on you"; they are not marks and
nothing in this sitting asks you to answer them.

Line numbers into `what-i-want-2026-07-16.md` are HEAD lines. Where a cited line's HEAD wording is the
2026-07-18 reword, the original line is given as (72c2e183:N) — the provenance map established the
reword introduced no new claims. **No answer below rests on the 16 lines the 2026-07-19 Copilot
amendment wrote.**

**Changed since the 1.3 version**: three contradiction claims were withdrawn on provenance — all three
said "refuse uniformly" refuses an example you accept, and no message you actually wrote reads an
optional field or contains any arithmetic (want-document-provenance.md:107–113). They are out of this
file. Item 18 is rewritten as the real question they leave behind; item 33's stated basis was one of the
three and is re-derived on lines you wrote.

---

## A. The reset conversation, 2026-07-25/26

These two cover the plan everything now runs under. You had this conversation yesterday; they should be
the cheapest marks in the file and they carry the most.

**1.** The reset plan's "Ruled" table — thirteen phases; a branch, not a copied folder; the certificate
checker back in scope including its runtime half; the bounded counterexample search; the unsized build
phase; the closed-list and three-attempt rules; the closing schedule; the phase-4 stop; confirm / narrow
/ revert; promotion gating nothing; the vocabulary calls — first appears `7474b3f2`, 2026-07-25. Split
into two marks because one row answers differently from the other twelve.
**Answer for the twelve rows:** Right as written. The checker and its runtime load-time half is
want:201–204 almost verbatim; confirm/narrow/revert and narrowing-is-legitimate are want:189; promotion
gating nothing is a fact about `main`; the rest (thirteen phases, only phase 1 sliced, phase 11 unsized,
the vocabulary, branch-not-folder) are process choices, either defensible — rests on want:201–206.
**☐ mine ☐ not mine ☐ don't remember**
**Answer for the "Coverage rests on the catalogs" row:** Correct it rather than confirm it. The coverage
*duty* is forced — want:204 asks the checker to refuse an unproven definition, and step-replay alone
cannot see a missing obligation — but the catalog *arrangement* replaces the checker you described
(walks the recorded steps, stays small and fast, want:206) with one that walks the definition, asks the
catalog what each thing owes, and makes catalog completeness the guarantee's finish line. That mechanism
your text does not support — rests on want:201–206 against compiler-readiness-plan.md:81, :1248–1253.
**☐ mine ☐ not mine ☐ don't remember**

**2.** The evidence rules written into CLAUDE.md the same day — catalogs untrustworthy until phase 9,
sample files never measure anything, compiling a file is not the specification, "Locked / ratified /
owner decision" labels are not authority — first appears `06afde4c`, 2026-07-25. If not yours, phases 3,
7, 9 and 10 lose their ground rules.
**Answer if it isn't yours:** All four stand on their own measurements, not on anyone's authority:
`CertificateSteps` does not exist in the tree while precept-language-spec.md:225 makes emitting a
certificate drawn from it a condition of admissibility; the "64 of 78 sample files" severity claim is
exactly the reasoning the sample rule bans; the confirmed `nonnegative` defect is a compile result
saying Proved about something false; and every commit here is under one identity, which is what empties
a "ruled" label. None changes what Precept is; dropping them removes the only thing separating
measurement from assertion — rests on CLAUDE.md:93, :100, :121, :123;
compiler-readiness-plan.md:136–148, :250–254.
**☐ mine ☐ not mine ☐ don't remember**

## B. Claimed rulings the proof-engine definition rests on (phases 4–7)

All of these are labelled "owner ruling" in the matrix or its satellites. None traces to anything you
wrote. Each now carries what phase 5 should do regardless of the mark.

**3.** A place a check happens is one execution occasion, not a piece of text — an expression reached by
three routes owes three separate proofs — first appears `ce72725d`, 2026-07-21. If not yours, phase 5 is
choosing its central framing itself.
**Answer if it isn't yours:** Right, and forced rather than chosen — the counterexample is on the
record: the guard-match validity argument assumed the facts at an expression could be read off its
position, and state exit actions run before the row's action chain, so one written expression reaches
different fact sets by different routes; the counterexample compiles clean at HEAD, reports Proved, and
divides by zero. One qualification: per-route proofs are the right *default* because they refuse less —
a single proof over the facts common to all routes is also sound, just weaker, and refusing more is
permitted under want:189 — rests on compiler-readiness-plan.md:919–921;
precept-language-spec.md:1994–2006.
**☐ mine ☐ not mine ☐ don't remember**

**4.** There are exactly four premise classes, and evaluating the rules at an editable-field write is a
discharge mechanism rather than a premise — first appears `b801451b`, 2026-07-20. If not yours, phase
5's premise set changes.
**Answer if it isn't yours:** Half right, half wrong. The editable-ingress check *is* a premise — your
own sentence: "evaluating those rules at the door is exactly the premise that closes the editable write
sites in the compiler's proof" (72c2e183:131; HEAD want:143) — and the four-class list at want:19 is
scoped to the inductive step over handlers, never claiming no other ingress. And the claim that the
editable-ingress discharge is a complete compile-time proof fails outright: computed fields are
re-derived at execution phase 7 after the ingress check at phase 1, so a rule mentioning a computed
field derived from the edited field is not covered by what the door evaluated — and with the
post-mutation sweep removed (want:199), nothing else evaluates it. The plan's ten-minute check at
:223–227 holds — rests on want:19, :143 (72c2e183:131); precept-language-spec.md:1994 vs :2004.
**☐ mine ☐ not mine ☐ don't remember**

**5.** A value written earlier in an operation may be consumed as a fact later in the same operation — a
fifth premise source beyond the four at want:19 — first appears `33707e77`, 2026-07-21.
**Answer if it isn't yours:** Right, and not a new ruling — canon has said it since 2026-04-29, months
before the freeze: "Actions in a chain are sequenced — each subsequent action sees the proof state left
by all preceding actions" (precept-language-spec.md:233, §0.6 item 7, whose last touch predates the
freeze). And it is not a fifth premise class: the fact about the assigned expression comes from the four
classes and the write only carries it forward — backward substitution along the route, the mechanism
your worked example names at want:103 ("the proof closes by substitution"). Phase 5 keeps four premise
classes plus a substitution rule — rests on precept-language-spec.md:233; want:103.
**☐ mine ☐ not mine ☐ don't remember**

**6.** A proof consuming a declared rule or modifier must name what establishes the fact and everything
that preserves it (declaration-fixed facts exempt, citing their declaration with empty sets) — first
appears `ce72725d`, 2026-07-21; one correction recorded only as "owner-accepted in conversation"
(`a793a383`, 2026-07-23).
**Answer if it isn't yours:** Right, and not discretionary — it is what makes the induction an
induction. A proof consuming "rule R held in the pre-state" (your class (d), want:19) is sound only if R
is itself established at construction and preserved by every operation; naming those obligations writes
down a dependency the certificate already must carry (want:199, 72c2e183:192). The declaration-fixed
exemption is a tautology, not a decision: a fact no operation can change has empty establishing and
preserving sets by construction. The 07-23 correction is simply true — the preserving set is quantified
over the whole file, so citation cannot make the check local, which is the same argument that puts
coverage above replay in item 1 — rests on want:19, :24, :199, :204.
**☐ mine ☐ not mine ☐ don't remember**

**7.** All five constraint forms — `rule` plus the four `ensure` forms — are proof obligations, and a
residency ensure is re-established at every entry into its state, so a transition row that writes
nothing still owes a proof — first appears `b801451b`, 2026-07-20; your document never uses the word
ensure.
**Answer if it isn't yours:** Both halves right, and neither needed a ruling. Nothing exempts `ensure`
from the promise — want:171 (72c2e183:159) rejects any constraint whose enforcement the compiler cannot
prove complete, and want:189 makes the guarantee non-negotiable — so all five forms are obligations by
default and the burden is on any claimed exemption. Residency follows from what the construct means:
"`in <State> ensure ...` — residency truth. The constraint holds for every operation while the entity is
in this state" (precept-language-spec.md:1884), so an operation ending resident in S owes it whether or
not it wrote anything. Your document never using `ensure` is not evidence against — it is a worked
example, not an enumeration of constructs. The one genuinely open sub-question — whether "the entity is
resident in S" is itself usable as a premise — is on the Waiting-on-you list, not a mark — rests on
precept-language-spec.md:1884–1887; want:171, :189.
**☐ mine ☐ not mine ☐ don't remember**

**8.** A constraint "mentions" a field through three routes — its condition, its `when` guard, and
transitively through computed fields — and that definition decides where every obligation attaches —
first appears `b801451b`, 2026-07-20 (the attachment principle itself is yours at want:20).
**Answer if it isn't yours:** All three routes are right and each is forced by soundness: the condition
trivially; the activation `when` because writing a field the guard reads switches the constraint on, and
the configuration where it first applies is where it can first be violated
(precept-language-spec.md:1866, :1878); computed-field transitivity because writing F changes X where
`X <- f(F)` (spec:2004). What is *not* established is that three is the complete list — that is an
exhaustiveness claim about the language, and phase 3 closes such lists (plan:184), so it is re-derived
over the closed expression-form list rather than inherited — rests on want:20;
precept-language-spec.md:1866, :1878, :2004.
**☐ mine ☐ not mine ☐ don't remember**

**9.** Obligations are judged over a handler's whole set of writes, never per write — intermediate
states are unobserved, so the shipped compiler, which rejects a plan whose final state is legal, must be
widened to accept it — first appears `b801451b`, 2026-07-20.
**Answer if it isn't yours:** Right for *constraint* obligations, wrong as a universal. Mutations run on
a working copy and "An invalid configuration never exists, even transiently... There is no window
between mutation and constraint checking where a partially-committed state with violated rules can be
observed" — that observability clause survives even though the passage carrying it
(precept-language-spec.md:1967) is the post-mutation-sweep text already heading phase 2's correction
list; your immutable-`Version` requirement (want:163) is what makes intermediate states unobservable
outside. So widening constraint checking to the whole plan is correct. But a fault — division by zero,
overflow — happens when the expression is *evaluated*, not when the configuration commits, so fault
obligations stay per-evaluation. Carried forward without that split, the ruling licenses a real hole —
rests on precept-language-spec.md:1967 (the surviving clause); want:163.
**☐ mine ☐ not mine ☐ don't remember**

**10.** The fault-family membership counts — eleven kinds ruled 07-21, ten on 07-23, nine on 07-24:
three counts in four days, each recorded as ruled.
**Answer either way (one glance):** Nothing to confirm or override — the family is whatever the closed
statement of the language can produce, an output of phases 3 and 5, and none of the three counts is
inherited. Three values in four days is the signature of reading the list off `ProofRequirementKind` — a
compiler enum the evidence rules bar — and the plan names this exact churn as the measured reason the
closed-list rule exists. Re-derive at phase 3, record the number with its hash — rests on
compiler-readiness-plan.md:136–139, :152–158; CLAUDE.md:93, :100.
**☐ mine ☐ not mine ☐ don't remember**

**11.** The discharge contract is exact — a compiler that accepts a sound program the contract does not
license is nonconforming — and every contract must name a finite procedure that decides "no derivation
exists" — first appears `5d554d59`, 2026-07-19.
**Answer if it isn't yours:** The exactness half is right and falls out of the certificates section: an
accepted definition must ship a certificate the small load-time checker can walk, so the accepted set is
exactly what the closed step vocabulary licenses — a compiler accepting more emits a definition its own
runtime gate refuses to load (want:204–206). The finite-procedure half holds only with a qualification
the original never states: precept-language-spec.md:225's admissibility criterion (3) licenses a hard
cap with an explicit "couldn't prove — here is what would" fallback, and a cap decides "none found
within the cap", not "no derivation exists". The cap is part of the contract, or the claim contradicts
spec:225. Phase 4 writes it with the cap in — rests on want:204–206, :179 (72c2e183:167);
precept-language-spec.md:225.
**☐ mine ☐ not mine ☐ don't remember**

**12.** Whether an author can respell around a refusal is judged once per family against the whole
derivation set, and a bound conjunct spelled in a guard discharges the same way an argument bound does —
first appears `5d554d59` / `26ed4d68`, 2026-07-19.
**Answer if it isn't yours:** Three questions under one mark. The guard-conjunct half is forced by
want:22 alone — modifiers, arg constraints and rule statements are spellings of one constraint
mechanism, so a bound spelled in a guard discharges through the same interval arithmetic as an arg
modifier. "Judged against the whole derivation set" is not a decision; it is the definition of
sound-but-unprovable. "Once per family rather than per cell" is moot — the reset plan retired
hand-written cells and banned the sample-corpus measurement the family verdict was to ratify against, so
nothing is left for it to govern — rests on want:22; compiler-readiness-plan.md:88.
**☐ mine ☐ not mine ☐ don't remember**

**13.** The normal-form rulings — fold literal arithmetic type-aware including division by a nonzero
literal, run every fold through the compiler's one shared evaluator, never accept algebraic
rearrangement as the same spelling — first appear `61fa9022` / `6505e6d9`, 2026-07-19/20.
**Answer if it isn't yours:** Type-aware folding through the single constant-fold evaluator is a
soundness requirement, not a style call: the evaluator computes with no fault checks, so any value the
matcher folds must be exactly the value the evaluator would produce — which is why the existing fold
path coercing integer operands to decimal before dividing (integer 10/4 folded as 2.5) is a defect to
fix before anything folds division. Refusing algebraic rearrangement is the only posture prove-or-reject
permits for a rewrite with no written validity argument; widening later is a per-rewrite argument owed
at phase 6, not an owner fork. Modulo staying unfolded is correctly open — its sign convention is a
phase-3 language question — rests on precept-language-spec.md:213;
docs/Working/normal-form-draft-2026-07-19.md:35.
**☐ mine ☐ not mine ☐ don't remember**

**14.** On the approximate `number` lane, nonzero / positive / nonnegative also establish not-NaN,
infinity is deferred under the overflow model, and underflow is left open — first appears `ce72725d`,
2026-07-21.
**Answer if it isn't yours:** The not-NaN clause is forced and canon carries the reason: `NaN != 0`
evaluates true, so without it a `nonzero` divisor could be NaN and pass. The infinity clause has no
standing of its own — it rides on the overflow deferral, which item 15 shows does not hold, so it
follows whatever overflow gets. Underflow is genuinely open and recording it open is correct, but under
prove-or-reject the open case must *reject*: two `nonzero` operands can multiply to exactly 0.0, so a
divisor obligation may only discharge from non-zeroness established on the divisor itself — rests on
docs/language/primitive-types.md:662, :663, :667.
**☐ mine ☐ not mine ☐ don't remember**

**15.** The arithmetic overflow model — integer, temporal, and number-lane infinity — is deferred past
the MVP with no compile-time guarantee in the meantime — stamped as ruled 2026-07-14 (`dfeb52cc`,
`0aa3723e`, `d32d0135`).
**Answer if it isn't yours:** The deferral does not stand as an exemption from the guarantee, whoever
stamped it: overflow is named as a fault the compiler proves or rejects in the philosophy, twice in the
spec's principles, and in your own text, and want:165 (72c2e183:153) removes the runtime guards — a
fault class with neither is a hole. Whether overflow is a phase-5 fault family, and what happens to the
441 dropped fault coordinates, follows the arithmetic-model choice — fixed-width 64-bit under
prove-or-reject versus arbitrary precision for `integer` — which is already on the Waiting-on-you list,
not decided here — rests on docs/philosophy.md:53; precept-language-spec.md:110, :112; want:104
(72c2e183:98), :165.
**☐ mine ☐ not mine ☐ don't remember**

**16.** Where a value came from — not whether it can be proven — decides prove-versus-govern, so a
relational rule over independently-set fields is governed at runtime rather than proven — first appears
`69e12c9f` / `e13b94cd`, 2026-07-16, still in canon at compiler-and-runtime-design.md:125. This is the
single largest canon-versus-want fight.
**Answer if it isn't yours:** Wrong, and it goes. Your own worked example is precisely this case —
`ReduceLimit` writes one field of a relational rule over two independently-set fields, and you say the
ideal compiler rejects the handler without a guard and closes the proof by substitution with one — so a
declared premise decides prove-versus-govern, not where the value came from. The one licensed piece
underneath the canon claim: evaluating every rule mentioning an editable field at the editable write is
a premise you name (want:143, 72c2e183:131); re-evaluating rules against the post-mutation configuration
on every operation is not — you deny it outright (want:199, 72c2e183:192). The fight resolves against
canon; phase 2 deletes the canon sentence and phase 5 proves relational rules — rests on want:103, :199
against compiler-and-runtime-design.md:125.
**☐ mine ☐ not mine ☐ don't remember**

**17.** The obligation-discharge matrix, once populated and ratified, was to become the canonical
exhaustive definition of the proof engine — first appears `5d554d59`, 2026-07-19.
**Answer either way (one glance):** No — already settled by the plan you had the conversation about:
three of the matrix's six ways of slicing survive as inputs, one is corrected, two are dropped, and the
structure is a walk over the language's expression forms instead. Nothing downstream reads the matrix as
a definition, so the mark costs nothing — rests on compiler-readiness-plan.md:890–914.
**☐ mine ☐ not mine ☐ don't remember**

## C. Claimed rulings in the language statement and canon (phases 2–3)

**18.** Message-hole presence — "refuse uniformly" was locked 2026-07-24 and written into the spec, and
the argument used to lock it (that it refuses a worked example you accept) is now withdrawn: no message
you wrote reads an optional field and none contains arithmetic (want-document-provenance.md:107–113).
The real question left behind: does the rule stand on some other footing, or does the lock come off?
**Answer:** The spec sentence stands on its own footing; only the recorded locking argument is struck.
What it says is that an interpolation hole is a read like any other, so an optional read there enrols a
presence obligation discharged by a `when … is set` guard or an `if … then … else` fallback and rejected
otherwise — prove-or-reject applied unchanged. "Uniformly" means the rule does not vary between a
`reject` message and a `set` right-hand side, which is the author-facing uniformity you require at
want:173 (verbatim 72c2e183:161). The fallback idiom already exists, so no new language surface is
involved — rests on precept-language-spec.md:1553, :1487; want:173.
**☐ the rule stands ☐ the lock comes off ☐ don't remember**

**19.** Periods do not order (equality only; Decision 14 stands) — the same-day withdrawal (`e33be4b6`,
2026-07-24) of a contrary same-basis-ordering ruling (`338ecafa`) that was *also* recorded as yours; at
most one of the pair can be.
**Answer if neither is yours:** D14 stands: periods carry equality only. For unqualified periods this is
forced — "is 1 month > 30 days" has no determinate answer, and presenting one breaks approximation
honesty — and canon is now internally consistent, the one contrary row having been corrected down to D14
(collection-types.md:533). The only two-sided part is a same-basis carve-out (`period in 'days'` reduces
to one integer component); it is a preference, cheap either way, needs a qualifier-conditional ordering
trait that does not exist, and D14's stated workaround (compare concrete dates) already covers it —
rests on docs/language/temporal-type-system.md:1478; docs/language/collection-types.md:533.
**☐ mine ☐ not mine ☐ don't remember**

**20.** Three fork stamps: function-argument constraints are build-work, not a fork (`4774ed43`);
`notempty` is string-only (`c67f334e`, dated back to 06-03); currency codes are uppercase-only, D13b
(`d74740cf`, 2026-07-24).
**Answer if any is not yours:** All three answer the same way on the merits, though they are unrelated
questions. Function-argument constraints are build work: the per-parameter mechanism exists, the spec
names `FunctionArgConstraintViolation` for `round(x, -1)`, the diagnostic is defined and marked
statically preventable, and the analyzer allow-list asserts `TypeMismatch` fires in its place
(DiagnosticCoverageAllowLists.cs:38) — so it is either built via the substitute or a defect, and neither
is a fork. `notempty` string-only is right: collection cardinality is single-sourced as
`mincount`/`maxcount` and `set of string notempty` already means each element (spec:1150, :1671;
collection-types.md:632, :776, :794 against the one contrary row at primitive-types.md:582). Currency
uppercase-only is right on the ISO 4217 × UCUM collision — case-insensitive lookup lets
`price in 'Zar' of 'mass'` compile clean, silently capturing a live UCUM unit as a currency — rests on
precept-language-spec.md:1614 with src/Precept/Language/DiagnosticCode.cs:51;
docs/language/business-domain-types.md:1806–1807.
**☐ mine ☐ not mine ☐ don't remember**

**21.** philosophy.md:49 was reworded ("Business logic cannot produce an answer that violates a declared
rule") and the edit is live at HEAD — philosophy edits require your explicit approval, and the ledger
entry authorising it says the wording would be "drafted for owner approval before applying" while commit
`33dde428` applied it in the same motion.
**Answer if it wasn't approved:** The reworded clause is right and stays: "cannot produce a wrong
answer" is a claim philosophy.md's own definition of validity does not support (:43 — a valid entity is
one where every constraint holds), and reverting would put a false sentence back at the top of the
document. The process breach is real and worth recording, but it changes the record, not the wording.
One line for phase 2, not this item's substance: the unqualified "No errors. No bugs." in front of it is
the bigger overclaim — rests on docs/philosophy.md:49 against :43;
docs/Working/proof-engine-decision-ledger-2026-07-12.md:63 against commit `33dde428`.
**☐ approved ☐ not approved ☐ don't remember**

**22.** Three further severity flips to Error — required-state dominance, unsatisfiable rule,
contradictory rule — recorded 2026-07-12 alongside a disclosure that a fabricated philosophy citation
had sat in the ruling's grounding; none of the three is in the code.
**Answer if it isn't yours:** All three belong at Error, derivable without your memory: an unsatisfiable
rule and a contradictory pair make the definition uninhabitable — the same defect as
DefaultViolatesRule, already `Severity.Error` at Diagnostics.cs:879, only stronger, and want:124 puts
the weaker default-violates case in the must-not-compile table; required-state dominance is a property
philosophy.md:51 says the compiler "proves impossible at definition time", and a warning is not a proof.
The fabricated citation damages the record, not the conclusion. All three are still Warning at
Diagnostics.cs:747, :802, :814, so phase 10 keeps the three tests either way — rests on
docs/philosophy.md:51; src/Precept/Language/Diagnostics.cs:879 against :747, :802, :814; want:124.
**☐ mine ☐ not mine ☐ don't remember**

**23.** The doc convention that a spec states the target while a status field states the gap — so
philosophy claiming compile-time impossibility beside a "no live guarantee" note is not a contradiction.
**Answer if it isn't yours:** The convention is right and already written down independently
(CLAUDE.md:63–70 gives every doc a status declaring the gap; want:8 treats philosophy.md as the
statement of what Precept should be). But it does not cover the case it was invoked for: philosophy.md
carries no status field at all and the disclosure sits in a different document, so philosophy.md:53's
unqualified "compile-time impossibilities" is not an instance of the convention — under :23's own
honesty commitment it stays a real row for phase 2 — rests on CLAUDE.md:63–70; docs/philosophy.md:1–12,
:53 against docs/language/primitive-types.md:697.
**☐ mine ☐ not mine ☐ don't remember**

## D. Authority claims from the earlier arc

**24.** You ratified the 2026-07-06 boundary ruling and cleared the 07-12 architecture gate — the
boundary document's own footer says "pending Shane ratification. Not Locked", yet a 07-12 note claims
"all four architecture decisions ruled by the owner".
**Answer if you never ratified:** Treat neither as authority and nothing is lost. The boundary document
says so itself twice — front matter and closing line both read "pending Shane ratification. Not Locked"
— and its central conclusion, that a relational rule over externally-supplied fields is governed rather
than proven, is contradicted by your later want:14 and want:171 whichever way the memory question falls.
Of the four architecture decisions: A is CLAUDE.md-mandated by the doc's own admission, B is answered
against by want:201–204 and already reversed by the reset plan, F5 traces to want:126, and only C (where
the step vocabulary lives) is a real engineering call, itself grounded in the catalog-before-code rule —
rests on docs/Working/proof-engine-boundary-ruling-2026-07-06.md:358; want:171;
docs/Working/prove-govern-collation-index-2026-07-15.md:178.
**☐ ratified ☐ never ratified ☐ don't remember**

## E. Recorded as agent work — one mark each, to confirm none is actually yours

Expected answer is *not mine*; flag anything in a group that is actually yours. Items 25–28 are one
glance each — nothing rests on the mark. Item 29 carries substance.

**25.** Matrix tooling and cell mechanics — validators, schemas, generated prose and coordinate maps,
corpus measurements and their counts, restored strings, file layouts (`60ef5a11`, `a70fd182`,
`a1d2212a`, `e4e6a660`, `d25d5672`, `0f488f76`, `b6810eef` and kin).
**Answer either way:** Nothing left to decide — the plan disposes of the whole group: the 107 cell files
are frozen with a marker saying no cell is citable, phase 5 is forbidden from citing them, and only the
six ways of slicing survive (three as inputs, one corrected, two dropped), that survival decided by
phase 5's own reading — rests on compiler-readiness-plan.md:553–556, :996, :890–906.
**☐ mine ☐ not mine ☐ don't remember**

**26.** Records of failure and reversal — the five refuted validity arguments, the four fault kinds with
no argument, slice 3's failed ratification, the design loop that did not converge, the retracted
root-cause counts, the same-batch reversals (`b59cd79b`, `982cbbb8`, `631eabc4`, `2133b682`, `c79e8a6b`,
`e89496d9` and kin).
**Answer either way:** Authorship does not matter — a record that an argument was refuted keeps its
force from the refutation, which is re-checkable. Two things carry forward rather than file as failures:
the withdrawal of the fresh-symbol treatment of the editable-field door restores want:143, and the
refutation of treating a computed field's defining equation as typing context is already on the
Waiting-on-you list — rests on want:143 (72c2e183:131).
**☐ mine ☐ not mine ☐ don't remember**

**27.** The bug filings, BUG-033 through BUG-066 — verified defects, filed not approved, owned by phases
10 and 11.
**Answer either way:** A bug filing is a claim that the code contradicts a canonical document, each
entry recording how it was reproduced — checkable, not authorised. Worth knowing before the mark: four
inherit their standing from items in this walk (BUG-062 from item 20's D13b; BUG-060 and BUG-061 from
item 18's rule; BUG-047 from item 19's single-basis canon) — if those fall, those four get re-checked
and the rest are untouched — rests on docs/Working/bugs.md:107–114 (BUG-062's filing against D13b).
**☐ mine ☐ not mine ☐ don't remember**

**28.** The 2026-07-12 slice designs and the certificate vocabulary counts — money, case-by-case,
single-fact, counterexample, constant-rule, shared-core, the 21 step kinds and 11 premise kinds.
**Answer either way:** The plan has already ruled the counts carry nothing — the 21 step kinds were
counted over the existing compiler's discharge paths, not over the phase-5 walk, so measuring a new list
against that number is, in the plan's words, a coin toss with a figure written on it. The one thing
worth keeping is not a count: want:199 (72c2e183:192) fixes what a certificate must contain — per
obligation, the theorem, the premises used, and the derivation — rests on
compiler-readiness-plan.md:829–831; want:199.
**☐ mine ☐ not mine ☐ don't remember**

**29.** The decisions inside the two unlocked establishment/preservation designs and the
externally-grounded transport restatement — occasion keying details, the state-scoped premise
restriction, diagnostic splits, coverage records, completeness checks,
kill-on-may-write/earn-on-must-run (`97b12d7c`, `3976c1d2`, `b130b018`, `c295d7b4`, `9e719666`). All
marked not-ready at HEAD; phase 5 rewrites them.
**Answer either way:** The group holds three kinds of thing. Kill-on-may-write / earn-on-must-run is
forced — any weaker pairing lets a proof cite a fact something may have overwritten, the fail-open
want:24 rules out — so phase 5 re-derives it rather than drops it. The state-scoped premise restriction
is a deliberately conservative position, taken by the design's own account to stay independent of a
question the plan reserves — whether "the entity is resident in S" is usable as a premise (plan:901, the
same question item 7 routes to the Waiting-on-you list); it is a strict subset of the permissive answer,
sound under either ruling, and phase 5 revisits it when that question closes. The coverage-record shape
and the two-level completeness check are engineering calls, either defensible — rests on want:24;
docs/Working/constraint-establishment-preservation-obligations-2026-07-23.md:867;
compiler-readiness-plan.md:901.
**☐ mine ☐ not mine ☐ don't remember**

## F. The sample — ten traced rows, now a verification result

These ten were drawn at random from the ~73 rows classified *traced to your writing*, as the error check
on the classification: agents built the register, and without this check agents decide which claimed
rulings you never see. Since the 1.3 version, each was re-checked against the provenance map — does the
cited line carry the claim, and did you write the line? **Result: nine of ten hold** (three with limits
noted below); **one — item 33 — rested on a line the Copilot amendment wrote**, and the register
corrected its verdict before this sitting. That is exactly the error the sample exists to catch: 1 in
10, caught and corrected. If the check is right, all ten come back *mine* — item 32 on its conditional
form, item 33 on its corrected basis, item 38 on the blocking rule rather than the vocabulary. More than
one *not mine* still redoes slice 1.1.

**30.** A rule can be consumed as a proof premise while nothing establishes or preserves it — a
confirmed fail-open defect, not intended behaviour.
**Check:** the quotation carries the claim. want:24 makes acceptance mean "no reachable operation can
violate any rule"; want:18–19 make premise class (d) usable only if the rule has a base case and an
inductive step; a file that consumes a rule with neither and divides by zero is :24's direct negation.
All three lines are in the 182 you committed — rests on want:18–19, :24;
want-document-provenance.md:27–29.
**☐ mine ☐ not mine ☐ don't remember**

**31.** A proof may cite a fact only if something establishes and preserves that fact — the
divide-in-one-handler, zero-in-another file breaches your promise.
**Check:** carries — the same claim as item 30 restated from the linkage design, so the consistency
check passes. :24 supplies the promise; :104 supplies the fault half (the declared premise is what makes
the division safe, no runtime zero-check behind it) — :104's HEAD wording is the reword, but the claim
is verbatim yours at 72c2e183:98, already substituted in the register — rests on want:24, :104
(72c2e183:98).
**☐ mine ☐ not mine ☐ don't remember**

**32.** A constraint modifier on a computed field is established by nothing and must not be consumable
as a premise (BUG-048).
**Check:** carries half. :22 makes the modifier a rule and :18 requires every rule to be established, so
no-consumption-while-unestablished follows directly — but "established by *nothing*" does not, because
whether the defining equation can establish it is precisely the open question on the Waiting-on-you
list. BUG-048's own fix note says the same: the no-consumption rule holds "until then". Mark *mine* on
the conditional form, not the permanent one — rests on want:18, :22; docs/Working/bugs.md:390.
**☐ mine ☐ not mine ☐ don't remember**

**33.** A fault-prone expression inside a reject message is a compile-time obligation like any other.
**Check:** this is the caught error. The stated basis, want:72 — a reject message containing a division
— was written by the Copilot amendment; no message in the document as you committed it contains any
arithmetic, and the register re-verdicted the row before this sitting. The claim is still right on the
merits, on different lines: :104 (72c2e183:98) puts faults on the same footing as rules, and :165
(72c2e183:153) removes every runtime fault check, so any expression the evaluator will actually run — a
reject message included — is proven at compile time or nothing catches it — rests on
want-document-provenance.md:103, :113; want:104, :165.
**☐ mine ☐ not mine ☐ don't remember**

**34.** `nonnegative` contributing nothing to a field's declared interval is a defect by your own model,
not a design choice.
**Check:** carries cleanly. :22 says `nonnegative` on a field is the same object as `rule X >= 0` with
one constraint mechanism underneath, so an interval honouring `max 100` while ignoring `nonnegative` is
two spellings behaving differently under one mechanism; the −10 that results is :24's negation. (The
failing behaviour is compiler observation — legitimate for showing a defect against the standard, never
for setting the standard.) — rests on want:22.
**☐ mine ☐ not mine ☐ don't remember**

**35.** Preservation attaches to every governed operation occasion — construction rows, state actions,
the editable-field door — not only event handlers.
**Check:** carries the enumeration, not the re-keying. :20 attaches a rule's obligations to every write
site of every field it mentions — covering construction rows, state actions and editable writes whenever
they write such a field — and :143 (72c2e183:131) names the editable door as a proof site in your own
words. What :20 does not reach is the shift from "write site" to "operation occasion": an occasion that
writes nothing owes nothing under :20; that extension is item 7's claim, separately no-trace and
answered there. One more line: :19 says "every handler", and the design reads that as granularity rather
than quantifier — defensible, but a reading — rests on want:19, :20, :143;
constraint-establishment-preservation-obligations-2026-07-23.md:815.
**☐ mine ☐ not mine ☐ don't remember**

**36.** Whether an operation owes preservation is declared per operation — Create, Fire and Update owe
it; Restore does not.
**Check:** carries the substance, not the mechanism. :141 (verbatim yours at 72c2e183:129) says restored
data is trusted and unchecked because it was governed when written — exactly why Restore owes nothing —
and Create/Fire/Update map onto the base case (:18), handlers (:19) and the editable ingress (:143).
Declaring the duty on the operation rather than the writer category is a design choice your document
does not speak to; that half is grounded in canon — restored state "trusted as valid at the time it was
persisted… reconstituted, not re-governed on load" — rests on want:141 (72c2e183:129);
precept-language-spec.md:272, :1969;
constraint-establishment-preservation-obligations-2026-07-23.md:315.
**☐ mine ☐ not mine ☐ don't remember**

**37.** The same Restore-exempt duty, incorporated verbatim into the linkage design.
**Check:** identical claim, identical verdict, same limit as item 36 — the exemption is yours, the
per-operation declaration mechanism is not. The duplicate consistency check passes; both get the same
mark — rests on want:141 (72c2e183:129); obligation-linkage-and-completeness-2026-07-23.md:226.
**☐ mine ☐ not mine ☐ don't remember**

**38.** Every containment obligation resolves to proven / proven-violating / unresolved, and both
non-proven verdicts block compilation.
**Check:** carries the blocking half and the printed missing premise; not the vocabulary. :171 (verbatim
72c2e183:159) — "a business rule whose enforcement the compiler cannot prove complete is rejected" —
makes unresolved block, and :110 plus :124 make a proven counterexample a compile-time rejection. The
printed condition is also yours: :179 (72c2e183:167) has the compiler "compute the guard or arg
constraint that would close the proof" and show it. Only the three verdict *names* have no counterpart —
harmless vocabulary. A *mine* mark covers the blocking rule and the printed condition, not the taxonomy
— rests on want:171, :110, :124, :179.
**☐ mine ☐ not mine ☐ don't remember**

**39.** There is no author-visible "trust me" escape hatch that softens prove-or-reject.
**Check:** carries most directly of the ten. :189 names "constructs that compile without their
obligations proven, checks silently deferred, warnings where there should be errors" as dilution — an
author-visible trust-me marker is that construct by definition — and :173's "no tiers of trustworthiness
to reason about" reaches the same place independently (:173's HEAD wording is the reword; the claim is
verbatim yours at 72c2e183:161) — rests on want:189; want:173 (72c2e183:161).
**☐ mine ☐ not mine ☐ don't remember**

---

## Waiting on you — the six standing questions, not marks

These are decisions to make when you choose to, with what they stall listed. Nothing above converts into
one of these, and nothing here is due in this sitting.

- **May a proof consume the complement of an earlier first-match row's guard?** Called the largest
  single blocker on real files (`e89496d9`); its size was measured only in sample-file counts, which the
  plan rules out, so the size is unestablished but the question is real. Stalls: phase 5's fragment
  edge.
- **How is a computed field's defining equation classified as a premise?** Treating it as typing context
  was refuted on three grounds; routing it as an earned fact needs a cross-operation argument nobody has
  written (`e89496d9`). Stalls: phase 5's premise set, and the permanent form of item 32.
- **May "the entity is resident in S" itself be cited as a premise?** Reserved at
  compiler-readiness-plan.md:901; items 7 and 29 both route here. Stalls: whether a residency ensure can
  feed other proofs, and the state-scoped premise restriction (which stays conservative until this
  closes).
- **Does `from any` desugar to one row per state?** Comma-delimited state lists do; the wildcard is
  unstated (spec:2198). Stalls: phase 5's route enumeration; closes at phase 3.
- **State-action multiplicity, and whether entry actions run at construction and on self-transition**
  (spec:2202). Stalls: phase 5's must-run/kill rule for state actions; the current designs carry both
  readings simultaneously to avoid it.
- **The arithmetic overflow model itself** — fixed-width 64-bit under prove-or-reject versus arbitrary
  precision for `integer`, and the temporal case that follows it; the number lane has only
  prove-or-reject available since `decimal` is already the exact lane. Stalls: overflow's place in phase
  5's families and the 441 deferred temporal coordinates. (Item 15's deferral is answered above; this is
  the underlying choice either way.)

## Appendix — filtered out, and where it went

**Contradicted by your own writing → phase 2's contradiction list** (they need resolution, not your
memory; phase 2 brings you only the ones its rules cannot settle):

- The post-mutation sweep written into the spec as normative (`33707e77`, spec:2005, and the
  stale-restored-entity variant) — already the largest row on the 25-row correction list.
- The prove/govern family: runtime-governed bounds and the author-facing disposition split
  (prove-govern-classification-2026-07-06.md:204/:226), totality-over-faults-only, nonlinear-rules-
  governed (two documents), the stay-Warning severity set, and the matrix's fault column omitting
  premise class (d). All contradict want:14/:135/:155/:171/:173/:189.
- The overflow family's canon rows (`90f0792f`, `d32d0135`, the 06-11 plan's parks, the ledger stamp) —
  answered at item 15; the canon edits are phase 2's either way.
- The checker cut from the MVP (architecture Decision B, proof-engine.md:2612) — already reversed by the
  reset plan; phase 2 removes the stale canon.
- The quick-fix-counts-as-authorship rescoping of your sentence at want:179 (`5d554d59`, matrix:50).
- The transport rule's narrowing of the editable-field door against want:143 (`c295d7b4`) — the document
  itself flags it as open, and phase 2 settles it.
- "Coverage rests on the catalogs" replacing your step-replay checker (`7474b3f2`) — answered inside
  item 1's split mark.
- Restore appearing in the expected-check set (`b59cd79b`) — already corrected in the 07-24 rework.
- The matrix's vocabulary restriction ("rule" means the construct only) against your own general usage.
- The STATUS page's rule that a four-leg Decision block counts as authority (`33344201`) — already
  reversed by the plan's evidence rules; recorded so it is not lost that this sentence is where the
  label problem came from.

**Withdrawn since the 1.3 version**: the three claims that "refuse uniformly" contradicts your worked
example — item 18's old framing, item 33's old basis (want:72), and the register's contradicted verdict
on the refuse-uniformly ruling. All three rested on lines the Copilot amendment wrote
(want-document-provenance.md:103, :107–113); the register was corrected accordingly. Item 5's dual
verdict (once no-trace, once contradicted) remains and item 5 covers it.

**Traced and removed as answered**: ~73 rows, chiefly the want document's own commitments (`72c2e183`)
and later rows that quote it correctly — induction, symmetric obligations, modifiers-as-sugar,
no-deferral, two ingress points, the certificate and its two checker call sites, no post-mutation
re-evaluation, the mutation table, MVP scope, guarantee-non-negotiable, the philosophy flag, dead-rows-
as-errors, restore-is-trusted, BUG-033/034/051/054 groundings, and the reset plan's traced rows
(spec-superseded-by-want, checker-in-scope, narrowing-is-legitimate). Ten are back in as the sample
(items 30–39).

**Register defects found while preparing this file**: the register cites hash `9e719661…`, which does
not exist; it resolves to `9e719666` (2026-07-23, the transport and type-structural validity arguments)
and is cited as such above.
