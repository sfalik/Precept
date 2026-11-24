# Design completion plan

**Status: proposed.** It becomes the live plan when Shane says yes. Step 0 then marks
`docs/working-reset/compiler-readiness-plan.md` superseded at its head and points the memory index at
this file, which stays at this path.

Written 2026-11-23 on branch `spike/Precept-V2-Radical-design-completion` against `5c4269bc`. It
replaces the earlier text of this file, which two outside reviews and four confirmation checks
(`review-confirmation/`) found could finish falsely and could halt successful work. Those reviews and
the earlier drafts under `lineage/` are history; this page is complete without them.

Paths that do not start with `docs/` are under `docs/working-reset/design-completion/`. Short names:
"want" is `docs/Working/what-i-want-2026-07-16.md`; "spec" is `docs/language/precept-language-spec.md`;
"rulings" is `inputs/history-rulings.md`; "failures" is `inputs/history-failures.md`; "errata" is
`inputs/technical-errata.md`. The three technical inputs (`inputs/technical-proof-model.md`,
`inputs/technical-precedent.md`, `inputs/technical-canon-consistency.md`) are lists of things to check.
Wherever this plan relies on one, it relies on it as corrected by the errata, and the errata entry wins.

---

## 1. The goal, the rules and the order of authority

The goal, in Shane's words: "A completed and consistent design first, then acceptance criteria, tests,
code reviews" (rulings:40). This plan ends when the design is finished. It writes no product code and no
acceptance criteria.

His five rules:

1. **Design first.** Compiler behaviour, test results and sample files never settle a design question.
   Design agents are given no compile or proof tools; the exclusion is enforced by the tool list an agent
   is started with, not by banning words.
2. **All detail stays**, compiler and runtime internals included: "you're suggesting to finish the design
   by removing detail and stripping the docs down? that doesn't work for me" (rulings:41).
3. **One full statement, deduplicate.** One document owns each fact, the others point to it, nothing is
   dropped (rulings:42).
4. **The design states the full intended proof power**: "it describes the full proof power, is that not
   clear from all the forensics and what I want" (rulings:43; want:14, :171, :183). What the first build
   ships never shrinks the design. Something hard to prove is an open design problem with an owning step.
5. **Much less of his time.** Agent runs reach him only when needed. He starts each step himself with one
   line. Nothing runs on a schedule.

**Order of authority.** His own quoted words; then the want document, which he approved whole ("lets
assume that I didn't write it directly, but did review it", rulings:47-49); then canon:
`docs/philosophy.md`, `docs/language/`, `docs/compiler/`, `docs/runtime/`, `docs/tooling/`. Where canon
disagrees with the want document, canon changes, unless a later quoted word of his says otherwise.
Labels an agent wrote, such as "locked" or "ratified", carry no authority. `docs/philosophy.md` is edited
only on his approval. Anything that would add to or change what an author may write goes to him as a
conversation and then through `/design`; no agent proposes syntax.

**Two measured facts this plan is built on** (failures:362-371, :549-556). Five arguments written with no
attack rounds "were all refuted in one pass", and "the two arguments that had twelve rounds held". And
earlier tracking produced "about 18,000 lines of records" and a tool of "about 5,700 lines" built to
manage a design document. So the twelve rounds stay exactly as they are (§ 6), and the machinery around
them is cut to what a later decision or check actually reads.

---

## 2. What finished means

The design is finished when all eight of these hold and Shane has signed the finish. The first seven are
checked by a reader who starts from the original documents, not from a list the work built for itself.

1. **Contents.** Every item in § 4.1 is written in its owning canonical document, and every argument it
   rests on has passed its rounds (§ 6).
2. **Hard problems.** Every problem in § 4.2 is closed by a design, or by a refusal on a reason § 6.2
   allows whose argument has passed its rounds.
3. **Conflicts.** Every place canon disagrees with the want document or with itself is closed by a
   committed edit (§ 4.3).
4. **His rulings.** Every ruling of his about what Precept does is stated in canon, or carried by a named
   rule of this plan or of `CLAUDE.md`, or retired on his own later quoted words. An answer that only
   operates the work (a start, a clearance, a permission, "run more rounds") is closed by the action it
   authorised and is not a design ruling.
5. **One full statement, checked in both directions.**
   - *From the original outward.* For every normative passage in canon as it stood at `5c4269bc`, and for
     every normative sentence removed since, a reader finds exactly one full owning statement in today's
     canon, or a written and justified disposition (merged into a named statement, replaced by a named
     decision, moved to a rejected-alternatives section). Zero full statements fails. Two fail.
   - *From the finished design back.* For every full statement, the reader finds the requirement it
     states, the argument that was attacked, and every applicable case it must cover. Where a proof rule
     varies by type or by action form, each applicable case is followed to an answer; a row that cites a
     numeric proof does not answer date ordering.
   - Every other mention of a fact is a pointer of one fixed form that resolves to a place that still
     states the fact.
   - The list of facts and owners has an origin: two agents draft it independently from canon and a third
     compares the drafts item by item, as for the contents list.
6. **Nothing open.** No open finding, no parked item, no open question in canon, no deferral that is not
   in one of four groups: designed here; refused on a reason from § 6.2; a capability he deliberately left
   open, with his words; build order only, with the design written here. One exception, on his quoted
   words: who constructs a `Fault` ("do we need to decide this now? seems like something we can defer
   until we start building the runtime", `docs/working-reset/compiler-readiness-plan.md:273`). He
   deferred the decision, so the decision is what waits; canon records it open with those words.
7. **The six omission cases** (§ 2.1) give the stated outcomes intact, and each deliberate omission
   blocks the finish for the stated reason.
8. **The handover.** The finish names the exact revision and the complete list of claims the design
   makes. Acceptance criteria are derived from that list in the next stage, which checks both a claim
   with no criterion and a criterion with no claim. The bank example and its fourteen mutations (want:112-127)
   are design evidence here, not criteria.

No document is marked complete because every *listed* row passed. Recording a finding and reopening the
claim it is against are one action, so a late discovery can never look like a violated finish.

### 2.1 The required test of the finish procedure

Four are given in full in § 4 of `outside-review-report-2-followup.md`; two are from finding 1 of
`review-confirmation/process.md`. A reader has the finished design, the finish procedure and the case
inputs. No compiler runs. For each case the reader records the owning canonical statement, the obligation
the compiler generates, how the checker or another trusted step enforces it, and what the caller sees.
Then that statement and its derived row are struck, every other row left complete, and the finish
procedure is run again from the original inputs. It passes only if it refuses the finish, names the
missing obligation and reopens the right work. It fails if only a reader told of the omission can find it.

| Case | Intact outcome | Omission that must block the finish |
|---|---|---|
| **A dequeue writes a third field.** A queue can hold an item of severity 9; the severity field has a maximum of 5; the item and its severity are both captured (`docs/language/collection-types.md:1205-1210`) | The definition is rejected, because 9 can replace 1. With only severities up to 5 queued it is accepted | Strike the ordering-value destination and its preservation proof. Writer coverage and certificate completeness must fail |
| **Preview of a division an earlier row guards.** The first row refuses divisor zero; the fallback divides. Preview with 0, 2 and no divisor | For 0, firing and preview both select the refusal and the division is not evaluated. For 2, both give 5. With no divisor, preview invents no winner and no quotient (spec:2006, :2161-2167) | Evaluate the fallback's actions before checking that an earlier row won, or leave preview out of the evaluation positions. The walk from proof to evaluator must fail |
| **A trapped application.** Review is entered with approval false; nothing in Review changes it; the only exit needs approval true. Also the version with two entries, and the repaired version with an Approve operation | The first two are rejected, the repaired one accepted (want:151-155) | Replace "from every reachable configuration" with "values that satisfy an exit guard". The graph sign-off must fail and ask for a continuation from the false applicant |
| **Old data under a tighter rule.** The old definition allows balance 0 or more, the new one 10 or more; balance 5 was stored under the old; both certificates are valid | Restoring 5 under its own definition, and 12 under the new one that wrote it, need no new check (want:141). For 5 under the new rules, either a stated migration establishes the new rule before any version is exposed, or no governed version is returned | Strike the condition that connects old data to the new rule. The induction sign-off and the restore walk must fail, without adding a check to same-definition restore |
| **A fact whose only full statement is gone.** Any fact with one full owning statement and at least one pointer to it elsewhere in canon | The read from the original outward finds exactly one full statement, and every pointer resolves to it | Strike the full statement and leave the pointers. The both-direction read must report zero full statements for that fact and refuse the finish; a pointer does not count as a statement |
| **A comparison rule with one type's case missing.** A proof rule for comparison that varies by type, with a numeric case and a date-ordering case, each followed to its own answer | The read from the finished design back finds an answer for every applicable case | Strike the date-ordering case and leave the numeric one. The read must refuse the finish and name date ordering as the unanswered case |

---

## 3. Design targets this plan fixes

Each of these was confirmed against the repository (`review-confirmation/technical.md`). Each is a
target the design must meet, not a question, and each owning step's brief carries it.

**Reachability and dead ends.** The standard is the one in `outside-review-report-2-followup.md` § 3:
"Every declared state must have at least one feasible finite run from a valid construction. From every
reachable configuration in a non-terminal state, at least one feasible finite continuation must reach a
different state." A configuration includes data as well as the state name. Runs use legal arguments,
first-match selection, actual writes, and legal edits inside their access windows. A refusal or an
unchanged self-transition is not an exit; a preparatory operation in the same state can be part of the
route. This follows want:151 ("Every state is reachable"), want:152 ("A state you can enter but never
leave is rejected") and `docs/philosophy.md:44` ("no dead end an entity enters and cannot leave").
Canon's graph that treats every row as able to fire (spec:191) cannot establish it and changes. The
compiler reports one of three results: proved; proved impossible, with the contradiction or the trapped
configuration; not proved, naming the route or enabling condition that is missing. "Not proved" never
certifies a definition and never accuses the author of a dead end. Values that satisfy an exit guard are
not a sequence of operations (errata 2.1). A separate test covers rows that can never fire (want:155).
Positive answers need evidence a checker can check: a feasible run, an exact analysis of finite choices,
or a checked argument over many values and steps, such as a counter that increases to a threshold.

*What is Shane's here.* Not the standard, which his text sets. One thing only: the separate question of
completion, whether every state must also be able to reach a `terminal` state. Canon answers it
(`docs/compiler/graph-analyzer.md:439`, "no structural path to any terminal"), nothing above canon
contradicts it, so the design keeps it and tells him in one line at the proof-model sign-off
(`owner-questions.md`, answered list). If a realistic workflow cannot be proved under the standard
without restricting what an author writes, that one case goes to him with the best argument tried.

**Required states.** The target is the spec's: "all initial→terminal paths visit this state" (spec:430;
`docs/philosophy.md:44`). `docs/compiler/graph-analyzer.md:489-496` checks dominance of one terminal only
and is repaired (errata 1.4).

**Inspection is an evaluation position.** Every preview operation, `InspectCreate` included
(`docs/runtime/runtime-api.md:89`, :186), is among the positions where the design says which premises hold
before each guard, action and message is evaluated. `docs/runtime/evaluator.md:837-866` runs a later
row's actions before noting an earlier row won, and is repaired (errata 1.4).

**Every way a configuration comes to exist is a base case.** Creation; restore under the same definition,
which stays "trusted and unchecked" (want:141); and starting under a different definition, where the
design says what identifies the governing definition, what establishes the new rules, and when no
governed version is returned. This is designed before the proof model is signed off (errata 1.4).

**Write destinations come from every permitted form of every action**, optional captures included. A
dequeue with an ordering capture writes three fields (errata 1.3). For each destination the design shows
a rule that reads only that field.

**Two duties at ingress.** Every declared ingress constraint is enforced, whether or not any proof used
it (want:129, :143, :165), and every premise a proof uses is justified (want:210). The run-time checks
never depend on which proof the search happened to find (errata 2.3).

**Arithmetic.** A product of two fields is not linear because the handler leaves one unwritten. Two
non-zero values can multiply or divide to a stored zero in every finite model, `decimal` included.
Identical text proves nothing if an operand changed between the two evaluations (errata 1.1, 1.2, 1.8).
`decimal` addition cannot overflow, because the want document's `Deposit` is provable as written
(want:98, :101); canon's "Overflow is an error" (`docs/language/primitive-types.md:236`) changes, and the
design owes the representation and its caller boundary. That is an agent's reading, so question 4 asks
him to confirm it; work proceeds on it meanwhile. It says nothing about `integer` (errata 1.9).

**Non-linear rules.** The restriction is want:135's: a *relational rule* whose post-state condition is
non-linear is proved only from a guard or argument constraint that states the condition. It is not a ban
on any product or quotient. The sign proof in the bank example (want:104: the guard `Balance < 0.0` and
`Months positive` close the `nonnegative` bound through a division) and the refusal when the cap conjunct
is removed (want:121) are both accepted results the inference steps must reproduce, whether the bound is
spelled as a modifier or as a rule (want:22).

**Explanations.** A suggested premise is promised only when one can be written, is consistent with the
rules and would close the proof. Otherwise the diagnostic names the assignment or rule that must change.
A replayable run, a counterexample to preservation from the stated rules, and a search that reached its
cap are three different results (errata 1.7, 1.12, 2.6).

---

## 4. What the finished design contains

### 4.1 Contents

Eighteen items. The owning document is a proposal confirmed in step 1, where two agents draft this list
independently from canon and a third compares the drafts item by item; every item found in only one
draft is added, merged into a named item, or dropped with a citation. The readings of how complete canon
is today are in `inputs/technical-proof-model.md` Part 2, as corrected by errata § 1. The list closes
when step 6 has written the inference steps.

| # | Content | Proposed owner | Step |
|---|---|---|---|
| 1 | The system the induction runs over: a configuration; each operation; the order inside one; every write destination of every action form; what a handler is, including state actions and `from any` | spec; `catalog-system.md` for writer entries | 1 |
| 2 | What counts as a rule: one table from every constraint spelling to the statement that must hold and where | spec | 3 |
| 3 | The base cases: the default configuration, every initial event, construction with no initial event, restore, and starting under a different definition | `soundness-and-coverage.md`, `proof-engine.md` | 4, 11 |
| 4 | The inductive step: every write site of every field a rule mentions, a change of state as a write, a handler's effect as a substitution for every action form, rows that write nothing, editable writes | the same two | 4 |
| 5 | What a proof may rely on: the complete premise set at each site and the statement that nothing else may be used | `soundness-and-coverage.md` | 4 |
| 6 | Fault obligations at every position where an expression is evaluated, in execution and in every preview operation; the early stop of `and` and `or` | `soundness-and-coverage.md`; `evaluator.md` | 4 |
| 7 | What each numeric type requires: `integer`, `decimal` with rounding, `maxplaces`, `number` | `primitive-types.md`; the proof documents | 2, 5 |
| 8 | Temporal, quantity and money; qualifiers; conversions that are identity, exact, approximate or rounded | `temporal-type-system.md`, `business-domain-types.md` | 2, 5 |
| 9 | Strings, choices, optional values, collections, computed fields, as meaning and as proof facts | the type documents | 3, 5 |
| 10 | Guards, linear and non-linear relational rules, transitions, stateless definitions | `soundness-and-coverage.md`; spec | 4, 6 |
| 11 | Ingress: arguments, edits, order, states, and both duties of § 3 | spec; `evaluator.md` | 4, 10 |
| 12 | Discharge, the three verdicts, the counterexample, the suggested premise | `soundness-and-coverage.md`; `diagnostic-system.md` | 6, 7, 8 |
| 13 | Graph structure to the standard of § 3: reachability, exits, required states, dead rows, rows hidden behind earlier rows | `graph-analyzer.md` | 9 |
| 14 | Certificates and the checker: the record, the step vocabulary, the checker working out for itself what a definition owes, what ties a certificate to a definition, the load check and its failure | `soundness-and-coverage.md`; `precept-builder.md`; `runtime-api.md` | 10 |
| 15 | What the runtime does and does not do, and what tooling shows | `docs/runtime/`, `docs/tooling/` | 12 |
| 16 | The induction argument itself, including that removing dead rows and dropping obligations on unreachable states is sound | `soundness-and-coverage.md`; the guarantee in spec | 11 |
| 17 | The overflow model for each type family | `primitive-types.md`, `temporal-type-system.md` | 2 |
| 18 | Catalog entries and diagnostics for obligation kinds, premise kinds, certificate steps and writers | `catalog-system.md`, `diagnostic-system.md` | 12 |

The finding behind all of it: canon states the promise as an induction in one paragraph (spec:268), and
`docs/compiler/proof-engine.md:2614` records that "there is no general carrying of 'every rule held
before this operation' into a handler's discharge". No obligation says a handler must leave an authored
rule holding, and none says an initial event must make it true.

### 4.2 Hard problems

Each gets a short engineering write-up first (the problem, the candidate techniques, what each costs),
then a design, then its arguments through § 6. Each closes with a design unless § 6.2 allows a refusal.

| Problem | What the design must decide | Step |
|---|---|---|
| Combining several declared facts | Complete decision of linear obligations and its checkable record; strict inequalities; integer-only consequences; contradictory premises; what happens at a cap (errata 1.7) | 6, 7 |
| Non-linear rules | What "states the condition directly" means; the permitted rewrites, each true of the evaluator's arithmetic and each saying whether a coefficient is known, unchanged or bounded (§ 3) | 6 |
| The evaluator's arithmetic and the prover's | One written model per type shared by evaluator, prover and checker; for each operation, that it is defined and that its actual rounded result satisfies the rule. Starts with a `/research` pass on base-10 rounding | 2 |
| Overflow | `decimal` per § 3; `integer` after Shane's answer; temporal values by a guard or a representable-range fact, with no new syntax; `number` proved or rejected | 2 |
| Zero from non-zero parts | What a proof may conclude about a product or quotient in each finite model, with a smallest-positive-value example for each (errata 1.2) | 2, 6 |
| Inexact conversions | Logarithmic units, the plane-angle family, and duration division (errata 3.5): what is proved, or the rejection and its message | 2, 5 |
| Rules over collection elements | A preservation table that describes the whole action and everything the predicate reads (errata 1.10) | 3, 5 |
| Temporal arithmetic | The calendar properties a proof may use; time-zone conversion; range | 2, 5 |
| Scale | Which pairs are skipped; a written cost bound per proof step; any cap as a number or a formula with an attacked argument (errata 2.5) | 7 |
| Rows are tried in order | Negated earlier guards as premises, in execution and in preview | 4, 6 |
| The check at an editable write | After Shane's answer (`owner-questions.md`, question 6) | 4 |
| What the checker is trusted for | Completeness of coverage and validity of derivations as two separate checks | 10 |
| Producing the suggested premise | Per § 3 | 8 |
| Rules true but not inductive | Per § 3; the counterexample shown as "a configuration your rules allow" | 8, 11 |
| Reachability and exits | Per § 3 | 9 |
| Preview under the proof's premises | Per § 3 | 4 |
| Starting under a different definition | Per § 3 | 4, 11 |
| Run-time checks from two duties | Per § 3 | 10 |

Two limits are mathematics and not effort: exact reachability over unbounded integers and general
non-linear integer arithmetic. The response to both is the want document's: prove from declared premises
or reject. Neither changes which result may be certified.

### 4.3 Conflicts in canon

The inventory starts from `inputs/technical-canon-consistency.md` and errata § 3.1, and is not complete:
that input "searched, not read" the type documents. So each step reads its owning documents end to end
for conflicts before it treats its part of the inventory as complete, and each distinct decision gets its
own entry. The count of 58 in the input is not used to size anything.

A conflict whose right side is already decided by the want document or a quoted ruling is a mechanical
edit, made in step 13, or in step 12 if it lies in a document the proof model changes. A conflict that
needs a design is closed by the step that owns the design. Conflicts already known to belong to a step
are listed in that step's brief when it starts. The proof-engine sign rules at
`docs/compiler/proof-engine.md:1475-1476` and the passages that write the limits of proof as permanent
are replaced in step 6. All four canonical statements of the `Fault` deferral are corrected to his words
(errata 3.1). What context a `Fault` carries depends on that deferral ("there is nothing to decide until
Q3 settles", `docs/runtime/fault-system.md:310`), so those choices stay conditional, recorded open
beside it, and no step settles them. The claimed conflict between the dataflow exclusion and the
induction does not exist and is not worked (errata 3.4).

---

## 5. The steps

Fifteen steps, 0 to 14, worked one at a time in order by one writer. No step has a date or a size.
Shane starts each with one line (§ 10). When a step can go no further without an answer of his, the
notice says so, and he may start the next step for the pieces that do not depend on that answer. Later
work that does depend on it waits; this plan does not pretend otherwise.

**The brief.** When a step starts, an agent writes `briefs/step-N.md` from this plan, the errata and the
inputs: the documents to read end to end, the lists to extract from `docs/language/` (never from the
catalogs), the conflicts and rulings routed to the step, and the detail only that step needs. A brief
cannot add or drop scope. A second agent checks it against this plan before any design text is written.

**How steps 1 to 11 work.** One author writes one section per sitting straight into the owning canonical
document, marked as a draft. A draft is committed, and nothing may rely on it or cite it until its
arguments have passed § 6; then the mark comes off. A question still unanswered is handled by writing
only what does not depend on it. More than one option is written only where independent work needs both.
Every decision carries its four parts: reasoning, alternatives rejected, precedent, tradeoff accepted.

**A step is finished** when every section it owns is written, every argument in them has passed, every
hard problem and conflict it owns is closed, and no finding is open against it.

| Step | Designs | Notes that bind it |
|---|---|---|
| **0** | The start | § 5.1 |
| **1** | What an operation is | § 5.2. Waits on Shane's three state-action answers for the sections on entering and leaving a state, not for the first section |
| **2** | What every operator means on every type, rounding and overflow included | Waits on his `integer` answer for `integer` overflow only. `decimal` proceeds on the reading question 4 asks him to confirm |
| **3** | The table of rules | Waits on his clock answer for rules that mention the clock only. His quoted words are "I used the term 'rules' more loosely"; the reading that all five constraint forms join the proof is a use of those words, checked for width before the table relies on it |
| **4** | The obligation model, every base case, every evaluation position including preview, and the ingress contract, as one piece | Waits on his edit answer for the edit section. Its last piece lists the proof steps the graph analysis will need, so step 6 has them |
| **5** | The facts each type contributes and how each action changes them | |
| **6** | The inference steps, which are also the certificate's vocabulary | Reproduces the accepted bank results of § 3. Closes the contents list. The first refusals, if any |
| **7** | The procedures that find proofs, with limits and cost bounds | The search can change without touching soundness (spec:225) |
| **8** | Verdicts, the counterexample, the suggested premise | Names two searches and their purposes: one finds the concrete case behind a rejection; the other tries to break each numeric proof rule. Both run after the design. Maps each of the fourteen bank mutations to the proof it loses and its diagnostic, links the two rows that describe one edit (want:121, :123), and records the accepted baseline beside the unedited original, with the presence test the guard and the message at want:75 and :81 need |
| **9** | Graph structure | To the standard of § 3 |
| **10** | The certificate and the checker | Both duties of § 3. Decides, as engineering, whether a certificate names what establishes and preserves each fact; he is told the result at the sign-off |
| **11** | The induction argument | Covers every base case of § 3. Then the proof-model sign-off (§ 9) |
| **12** | Catalog entries, diagnostics, runtime, tooling | Carries the proof model into every document that states or restates it. Sorts every deferral and answers every open question inside `soundness-and-coverage.md` and `proof-engine.md`, which step 13 does not touch. The step-1 contents comparison assigns every section of those two documents to a contents item or to step 12, and step 12 finishes and deduplicates whatever no contents item owns. Decides the caller-visible result shapes with four-part reasons. Proposes the edit to the `CLAUDE.md` catalog notice |
| **13** | Everything the proof model does not reach | Mechanical conflicts; plain copies merged into one owning statement with pointers; every deferral sorted into the four groups; every open question answered; unfinished documents finished. Runs after the sign-off, so nothing it touches is still moving |
| **14** | The finish | § 2 in full: the both-direction read, the six omission cases, worked examples carried by hand through every document and every operation (create, fire, update, restore, load a definition with a valid, a stale and a tampered certificate, and the three previews `InspectCreate`, `InspectFire` and `InspectUpdate`), the sort of the whole change by what justifies it, and the handover list. Then the finish sign-off |

### 5.1 Step 0 — the start

Small on purpose. It produces only what the first design section needs:

- The record of his yes, word for word, as the first entry of `answers.md`.
- `progress.md`: the approved revision of this plan; the one author and the one independent reader for
  step 1, by pinned model (§ 10); the files and tools each may use; the order of authority; the step he
  has authorised; the last committed piece; any blocking finding. One writer advances it, from results it
  observed.
- One start-and-status check, small enough to read in a sitting. It refuses an unauthorised start, an
  unmet prerequisite, a missing or unreadable record, and a round with no evidence. It is tested before
  use on a good case and on a missing, a malformed and a half-written record. A read error never counts
  as "nothing outstanding".
- Two walks on paper, recorded in `progress.md`. One follows a whole step from its one start line
  through a stop, a restart and its finish. The other delays one answer, the edit answer (question 6):
  it lists which pieces of steps 4 to 11 can run and which wait, and confirms the start-and-status check
  allows the first set and refuses the second.
- The old plan marked superseded and the memory index pointed here.
- The brief for step 1.

Step 0 is one run's work. If the check is not working after three runs it stops with the literal failure
in a notice. Nothing else is built now: step 0 builds no question checker, no lock for concurrent
writers, no conversion of the inputs into data files, and no table of every document against every fact.

**Every other check is built at its first real use**, by an agent other than the one whose work it will
check, and tested the same way before it is relied on:

| Check | Built when |
|---|---|
| The round check of § 6 | Before the first round is credited. It compares the revision a round attacked with the current one, and is tested on a good round and on an empty return, a duplicate round, a round against a stale revision, a pending dependency and a refuted argument, refusing credit for each of the five |
| The check on a new question for Shane (§ 9) | At the first candidate question that is not already in `owner-questions.md` |
| The check on retiring a ruling | At the first proposed retirement |
| The refusal check of § 6.2 | At the first refusal |
| Destinations for removed sentences | At the first substantive deletion or merge |
| The pointer check, and the four phase-2 checks (`docs/working-reset/phase-2/checks/`) widened to all of canon | Before step 12 first needs them |
| Unattended running: denied permission, spending stop, resume after a stop, a notice reaching him | Before the first unattended run is offered (§ 10) |
| Combining the work of two writers | Only if a second writer is ever introduced. None is planned |

### 5.2 Step 1 — the first section

The first design text is **what an operation is**: the ordered path of an ordinary transition row, from
guard selection (first match, spec:2006), through the row's writes in order, to recomputation of computed
fields. It includes every destination an action form writes, a dequeue's ordering capture among them
(§ 3), and which facts an earlier write in the chain invalidates. Known exceptions are written out as
exceptions. Neither the clock question nor any state-action question changes the order of this ordinary
case, so it waits on nothing.

It is committed to the spec's section on operation execution order as a draft. No other section and no
argument may rely on it until its arguments have passed twelve rounds.

Step 1 then writes the rest of contents item 1, the two independent drafts of the contents list and
their comparison, and the checkpoint: five small arguments written in the working folder and not in
canon (a rule established at creation; preserved across one `set`; and a guard, an argument constraint
and a rule that held beforehand each used as a premise), over exact numbers with no state actions,
collections or clock, attacked for three rounds. It tests early whether the model can be written at all
(want:183). Nothing from it enters canon, and step 4 attacks the real arguments in full.

---

## 6. Attack rounds

Kept exactly as `outside-review-report-2-followup-2.md` § 3 describes them.

- Every soundness argument and every refusal argument gets **twelve credited rounds**. No early pass.
- Each round has **two fresh attackers**. Each attacker's file contains at least one concrete attempt: a
  definition, a sequence of operations and the claimed wrong outcome. A file without one is not a round;
  the round is run again.
- Each round has an **independent classification** of every finding and an **independent check of the
  argument's citations** against canon. A real flaw fails the argument even if no listed kind names it;
  a note on wording fails nothing.
- A **repair is confirmed by someone who did not make it.**
- An argument **passes** when twelve credited rounds exist, none is missing, nothing is open, and the
  last two rounds found nothing. It is **never signed off in the sitting that drafted it.**
- An argument that fails it goes to a **second author on a different model for up to six more rounds.**
- When a passed section or argument changes in meaning, **it and every argument that relies on it go back
  into rounds.** A fresh reader looks for dependencies the author did not cite. A move of words with the
  same meaning sends nothing back.
- An internals choice nobody outside the compiler or runtime can observe gets its four-part reasons and
  one attack, **only while no proof depends on it.** Once one does, it is an argument and gets twelve.
- The record of a round keeps the revision attacked, the attackers' identities, each attempt, each
  result, and the repair it led to. Nothing else is copied. **A round counts only against the revision
  it attacked**: if the text has changed in meaning since, the result is stale and the round is run again.
- An agent that returns nothing has not done its piece, and is never a round that found no flaw.

One round is two attackers, a classifier and a citation checker, so one argument is at least 48 agent
runs before any repair.

### 6.1 When an argument still fails

After the second author's rounds, the failure reaches Shane only with its counterexample and its
consequences, and never as a request to waive review. The choices are more rounds or another author, a
different argument, or a change to the language through `/design`. Weakening the guarantee is not a
choice (want:189). He chooses for each argument.

### 6.2 The only reasons a design may refuse something

1. Want:135: no solver the compiler does not carry, and a non-linear relational rule is closed only by a
   premise that states the condition, read with the scope in § 3.
2. Spec:225's first test, that a proof leaves a certificate an independent checker can replay; and
   determinism (spec:96).
3. A mathematical impossibility with a published result, with an argument that the result applies to
   this case and not merely near it.
4. A quoted ruling of Shane's, checked for width by a reader who did not make the use. A ruling about
   build order or the first build is never a reason: overflow being built after the first compiler, the
   minimal proof set for the first build, the expression as written, schema evolution later.

Cost is a reason only with a written cost bound for that proof step, attacked with its argument. A cap is
a reason only written as a number or a formula over a definition's declared sizes, with an attacked
argument from canon alone of which definitions stay under it. "Earns its place" is a reason only on his
word. "Not built yet", "the first version" and "the MVP" are never reasons. A refusal states its exact
scope and a counterexample, and is itself an argument with twelve rounds.

---

## 7. Evidence, citations and records

- **Every factual claim carries `file:line` and the words relied on**, read from committed text. A claim
  that canon is silent records its search terms and a search of history.
- **Canon cites nothing under `docs/Working/` or `docs/working-reset/`.** The link between a canonical
  edit and the ruling or requirement behind it is kept outside canon, in `changes.md`, which records the
  ruling and its quotation at a fixed revision. Whether canon still states the ruling is checked against
  the current owning statement, not against a line number that a later merge moves.
- **Every use of his words is checked for width** by a reader who did not make the use. His sentence is
  recorded separately from the engineering conclusion drawn from it, so a disputed conclusion never
  reopens the ruling.
- **A dispute between agents** goes to one fresh reader with the order of authority in § 1.
- **Rulings.** `inputs/history-rulings.md` lists his rulings and `inputs/rulings-routes.md` the route of
  each. Its step numbers are the earlier plan's: its step 13 is now the stage after this plan, its 14 is
  step 13 here and its 15 is step 14. Six routes are corrected: the `Fault` deferral as in § 2; the
  guarantee contract's first three parts are in canon (spec:262-276) and its fourth is superseded by his
  later words; the "constraint" ruling as in step 3; approximation per operation is answered by the
  philosophy he approved later (`docs/philosophy.md:30`); the `in 'each'` example is verified against
  canon and not asked; the memory note that still teaches the old period-ordering ruling is corrected to
  his later words and mentioned to him. A ruling is retired only on his later quoted words.
- **Records.** `answers.md` (his words, verbatim, committed in the sitting); `progress.md`; one file per
  argument or refusal under `claims/` with its dependencies and round evidence; `findings.md`;
  `changes.md`; the briefs. Status and notices are generated from these. One working record per fact:
  nothing is hand-copied into an inventory, a dated report and a dashboard. A record that no later
  decision or check reads is not produced.
- **No line leaves canon without a destination**: the statement it merged into, the section it moved to,
  or a rejected-alternatives section, checked sentence by sentence by a second reader.
- **Plain writing.** Every prompt carries the "How to write" section of `CLAUDE.md`. Nothing sent to
  Shane uses a row number or code in place of a description.
- **This page.** An edit after adoption needs his words and a check, by an agent that did not propose it,
  against all five rules at once.

---

## 8. Stop rules

Only rules that block on a real failure. None counts lines or raw totals. Before first use each rule is
run, by an agent that did not write it, on three written traces. It must stay quiet on a step that
produces records and no canonical edits and on a run that finds two new defects while closing one, and
it must fire on a trace that really fails. A rule that fails either test is repaired first.

| Rule | Fires when | What happens | Cleared by |
|---|---|---|---|
| Open finding | A piece would rely on a claim with an open finding | That piece and its dependents wait; unrelated sections continue | The finding closed and its repair confirmed by someone who did not make it |
| Draft relied on | A section or argument cites text still marked as a draft | The citing piece is rejected | The cited text passing its rounds |
| Different problem each time | Three repairs in a row of one argument are each refuted by a different real counterexample | The argument goes to the second author early | Not a halt |
| The same requirement fails again | A repair of the same requirement is refuted on successive attempts with no change of design, or the same defect is rediscovered | A fresh reader takes it; if no technical repair works within the second author's rounds, § 6.1 | The repair holding, or his choice under § 6.1 |
| A shared false premise | One demonstrated counterexample breaks several arguments, or the checkpoint fails on its own stated assumptions | The arguments that rest on that premise wait. The premise is repaired by a fresh author. If independent attempts fail, one sheet to Shane with the counterexample and the choices of § 6.1 | The repair holding, or his choice |
| Wide use of a ruling | A use of his words is unchecked or found wider than the words | The use waits | An independent reader confirming a corrected use, or the use withdrawn |
| No result | A literal failure, an empty output, or repeated attempts with nothing checkable to show | The run stops, keeps the last good piece, and writes a notice with the literal error. The drafting agent cannot clear its own block | His next start line, which permits the one approved retry |
| Setup bound | Step 0 is not working after three runs | Stop with a notice | His words |

A refutation with a counterexample is progress and is recorded as such; it is not a pass and not "no
result". Finding defects has no quota. The finish still requires every finding closed.

---

## 9. What reaches Shane

**Three sign-offs**, which no agent review stands in for ("don't substitute Frank's approval for mine",
rulings:184). Each read says what the agents did not cover.

1. **This plan.** He reads `plan-summary.md`, written from this page and giving every threshold in § 8 and
   § 10 and everything counted below, and this page if he wishes. First an agent that did not write the
   summary compares the two item by item, recorded in `review-confirmation/summary-comparison.md`. Where
   they differ this page stands and the difference is a defect.
2. **The proof model**, after step 11. He reads his bank example worked under the finished model; the
   list of refusals with the reason for each; the one-line notices (the completion rule of § 3, the
   certificate-naming decision, the `Fault` deferral, the change to `decimal`'s caller-facing type); any
   ruling proposed for retirement; and the changes to canon that trace to nothing he wrote or ruled.
3. **The finish**, after step 14. He reads the result of each condition in § 2, the same sort of the
   whole change, the proposed edit to `CLAUDE.md`, the result shapes decided in step 12, and any
   philosophy edit his clock answer requires.

**Six questions**, written out in `owner-questions.md` with his earlier words, the options and a worked
example each: three on state actions; `integer`, with a confirmation about `decimal`; the clock in rules;
and what a direct edit checks. He can answer at any time. Nothing else is asked at the start.

**A new question reaches him only if** two readers who were not given the proposer's conclusion both find
that none of his words, the want document or uncontradicted canon answers it, and the options differ in
something an author or caller can see. Options that differ only in how much the compiler can prove are
design work. So is the shape of a caller-visible result where canon fixes what it must carry ("it refuses
and says why", `docs/philosophy.md:46`): step 12 chooses it and the finish sign-off shows it
(`review-confirmation/owner-questions.md:308-315`). The sheet has the same form as the six. Known
candidates, each only if it arises: the design would have to refuse `Deposit` as he wrote it; the design
changes the digits a `decimal` division keeps from canon's 28; a realistic workflow cannot be proved
reachable without restricting authors; a new author-written bound on dates is proposed; a policy on who
supplies a migration at restore; a refusal on "earns its place".

**Notices.** He hears from the work when a step finishes or can go no further without him, when a stop
rule that needs him fires, and when a run fails. Every notice lists what was recorded as his since the
last one. Nothing is sent on a schedule.

**The count, stated honestly.**

| What | How many |
|---|---|
| Sign-offs | 3 |
| Answers to the six questions | 6. The fourth includes a confirmation about `decimal`; the last is mostly a confirmation of his own text |
| Yes or no to the exact permission list for unattended runs, shown with the narrower fallback | 1, asked before the first unattended run and not folded into the plan's approval |
| Start lines | 15, one per step, plus one for each restart |
| Approval of a philosophy edit | At most one known cause, his clock answer |
| Conditional questions | The six candidates above |
| Failed-argument sheets | Not bounded. Each holds one choice per argument |
| Shared-false-premise sheets, and notices after a failed run, including the one that follows three failed runs of step 0, which only his words clear (§ 8) | Not bounded |
| His say before any model in § 10 is changed | Not bounded; none is planned |
| His say before extra paid usage is drawn on | Not bounded; one for each time a usage limit would otherwise be passed |
| His words for an edit to this page after adoption (§ 7), such as moving a check to its first use | Not bounded |

Decisions inside a sheet, pages to read and restarts are reported separately in every notice, and the
first real sign-off packet is measured in pages and choices before any claim is made about how much of
his time the rest will take.

---

## 10. How the work runs

- **He starts each step with one line**, typed to a session opened in the repository:
  `Run step N of the design-completion plan`. A step never starts another step. Within a step the
  session runs piece after piece until the step finishes, a stop rule fires, or a spending limit is
  reached.
- **One writer.** Authors, attackers and readers are separate agents that return files; one writer
  commits them in order and advances `progress.md`. Readers and attackers run side by side; writers do
  not. So there are no worktrees to combine and no lock to build.
- **Progress is in committed files.** A piece is done when its file exists and is committed. A stopped
  run resumes at the first piece not recorded.
- **Models are pinned by role.** Proof-model author: Fable 5.2, one section per sitting with a hand-over
  note. Attackers, classifiers, citation checkers, repair confirmers, width readers, question readers and
  the finish readers: Opus 5.5, never the author of the text read. Second author: Opus 5.5, with some
  attackers moved to Fable 5.2 for those rounds. Merging plain copies and extracting lists: Sonnet 5.5.
  Each output records the model requested and, where it can be read, the model observed. No weaker model
  is substituted to keep going, and none is changed without his say.
- **Unattended runs** are offered only after the tests in § 5.1 pass and he has approved the exact
  permission list. If he declines, or a test fails, runs happen in a session he has open.
- **The cost if both fallbacks apply**, shown before approval. If a stopped run cannot resume, every run
  is one piece, and one piece is one round of one argument. If runs cannot be unattended, a session stops
  after five runs. Eight arguments at twelve rounds are then 96 runs, about 20 sessions: one start and 19
  restarts for the attacks alone. The number of arguments is unknown until step 1 and fixed at step 6.
- **Usage limits.** A short window is waited out. A weekly limit pauses the work that needs that model
  with one notice; he restarts with the same line. Extra paid usage is never drawn on silently.

---

## 11. Known weak points

- **The proof model may not close.** The want document says feasibility is the open question (want:183).
  The checkpoint and the shared-false-premise rule exist to show that early and bring it to him once,
  with choices that keep the guarantee.
- **Everything here is checked on paper by agents.** Attack rounds, the both-direction read and the six
  omission cases can share a blind spot. The certificate checker, which is code, is the eventual
  backstop, and it is built after this plan ends.
- **The both-direction read is large and rests on readers.** Its size is unknown until step 13 merges the
  copies. A reader can miss a restated fact, an uncited dependency or an uncited use of his words.
- **Later steps really do wait for his answers.** Step 4 cannot finish without the edit answer, and
  steps 5 to 11 rely on step 4.
- **His time is not bounded by this plan.** Failed-argument sheets, restarts and the length of the
  proof-model sign-off read have no cap. The count in § 9 is a list, not a promise.
- **The checks are built as the work reaches them**, so a check can turn out harder to build than
  expected in the middle of a step. That is the price of not building fifteen scripts first.
- **Two confirmation checks differ on one point.** One treats the completion question as answered by
  canon; the other leaves it as possibly his. This plan follows the first and tells him at the sign-off.
- **Nothing mechanical proves an entry in `answers.md` is his.** The session he talks to writes it. Every
  notice shows him what was recorded.
- **The inputs were not all read end to end**, and the conflict inventory is known to be incomplete
  (§ 4.3). The precedent input's outside sources were not checked against the originals (errata 2.7).
- **How the work runs is untested** until step 0 and the first-use tests have run.
