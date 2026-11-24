# The design completion plan, summarised

A reading aid for `design-completion-plan.md` in this folder, as rewritten on 2026-11-23 against
`5c4269bc`. Where the two differ, the plan stands. The plan finishes the design. It writes no product
code and no acceptance criteria, and it replaces the thirteen-phase readiness plan.

## What changed

The outside reviews found that the earlier plan could finish with a fact missing, could halt work that
was going well, and built fifteen scripts before any design paragraph. This version is 599 lines, down
from 1,302. The design's scope is unchanged; plan machinery was cut.

## What finished means

Eight conditions and your signature. Every content item is written with its arguments attacked. Every
hard problem is closed by a design or an allowed refusal. Every disagreement in canon is closed. Every
ruling of yours about what Precept does is stated in canon. Each fact has exactly one full statement,
checked both ways: from every original requirement to its statement, and back. Nothing is left open
except who constructs a `Fault`, on your words. The finish procedure must itself pass a test: six worked
cases, each with one fact deliberately removed, and it must refuse to finish each time. The finish then
hands over an exact revision and the list of claims; acceptance criteria are written afterwards.

## The steps

Fifteen, one at a time, by one writer. You start each with one line. No schedule.

- **0.** Your yes recorded, one small start-and-status check, and two walks on paper: a whole step from
  one start line, and the schedule with your edit answer still outstanding. Every other check is built
  when the work first needs it, and tested on cases that must pass and cases that must be refused.
- **1.** What an operation is. The first text is the path of an ordinary row, from guard selection
  through its writes to recomputation. It is a draft nothing may rely on until it has been attacked.
- **2 and 3.** What every operator means on every type; the table of what counts as a rule.
- **4.** What must be proved at creation, at restore and at every write; what a proof may assume; what is
  checked on the way in; preview.
- **5 to 8.** Facts per type, proof steps, the procedures that find proofs, verdicts and explanations.
- **9 and 10.** Reachability and exits; the certificate and its checker.
- **11.** The induction argument. Then your proof-model sign-off.
- **12 and 13.** Catalog entries, diagnostics, runtime, tooling, and every section of the two proof
  documents that no content item owns; then tidying everything else.
- **14.** The finish checks, the hand walk, your finish sign-off.

## What is kept

The twelve attack rounds, unchanged: two fresh attackers a round, each with a concrete attempted
counterexample; repairs confirmed by someone else; a second author on a different model if an argument
fails. A round counts only against the text it attacked; if the meaning has changed since, it is run
again. Your three sign-offs. The order of authority: your words, the want document, then canon.

## What is fixed in the design target

Errors found in the technical inputs are listed in `inputs/technical-errata.md`. The plan now requires: a
real run into every state and a real way out for every entity in it, not just an arrow on a graph;
preview evaluated under the conditions the proof assumed; a rule for data restored under a changed
definition, before the proof-model sign-off; every field an action writes counted; every declared check
on incoming values enforced even if no proof used it; and no assumption that two non-zero `decimal`
values multiply to a non-zero result.

## When the work stops, and who clears it

No rule counts lines or totals. Before first use each rule is run on two ordinary cases, where it must
stay quiet, and on one real failure, where it must fire.

| Rule | Threshold | Cleared by |
|---|---|---|
| A piece relies on a claim with an open finding | Any | The finding closed and its repair confirmed by someone else. Unrelated work continues |
| A piece cites text still marked as a draft | Any | The cited text passing its twelve rounds |
| An argument is refuted by a different counterexample each time | Three repairs in a row | Not a halt: it goes to the second author early |
| The same requirement fails again | Successive repairs refuted with no change of design | The repair holding, or your choice |
| An argument has not passed | Twelve rounds, then up to six more with a second author on a different model | After eighteen it reaches you with its counterexample. You choose more rounds, a different argument, or a language change. Weakening the guarantee is not offered |
| One counterexample breaks several arguments | Any | The repair holding, or your choice on one sheet |
| A use of your words is unchecked or too wide | Any | An independent reader confirming a corrected use |
| A run produces nothing checkable | A literal failure or an empty output | Your next start line, which allows one retry |
| Step 0 is not working | Three runs | Your words |
| A session you have open, when runs cannot be unattended | Five runs | Your restart line |

## What reaches you

- Three sign-offs: this plan, the proof model, the finish.
- Six questions, written out in `owner-questions.md`: three about state entry actions, whether
  `integer` has an upper limit, whether a rule may mention the current time, and what a direct edit
  checks. The `integer` sheet also asks you to confirm that `decimal` has no upper limit, which your bank
  example appears to require and which changes the type callers receive; your "no decision yet" was
  recorded about numbers as a whole. The last sheet is mostly a confirmation of your own text. That file
  also lists the questions found already answered, and four decided as engineering without words of
  yours: whether a stored proof names what establishes each fact, where a divisor's non-zeroness must
  come from, how many digits a `decimal` division keeps, and the shape of results a caller receives.
- Fifteen start lines, one per step, plus one for each restart.
- One yes or no on the exact permission list for unattended runs, asked before the first such run.
- At most one approval of a philosophy edit, and only if your clock answer needs it.
- Six further questions, each only if it arises: the design would have to refuse `Deposit` as you wrote
  it; the design changes the number of digits a `decimal` division keeps from canon's 28; a
  realistic workflow cannot be proved reachable without restricting authors; a new author-written bound
  on dates is proposed; who supplies a migration when old data is restored; a refusal on "earns its
  place".
- One-line notices when you approve the proof model: every state must be able to reach a `terminal`
  state; whether a proof record names what establishes each fact; who constructs a `Fault` stays
  deferred; `decimal` gets no upper limit, which changes the type callers receive, unless you answered
  otherwise on the `integer` sheet.
- Sheets for arguments that still fail, sheets for a shared false premise, and notices after a failed
  run, including the one after three failed runs of step 0, which only your words clear. None of these
  three is bounded.
- Three more things that need your say, none of them bounded: any change to which model does which job;
  drawing on extra paid usage; and any edit to the plan after you adopt it.
- The shape of results a caller receives is not sent to you as a question. Canon already fixes what such
  a result must carry, so the shape is chosen in step 12 and shown at your finish sign-off.

## Weak points

- **The proof model may not close.** Your own document calls feasibility the open question. An early
  small test is meant to show that soon, not prevent it.
- **Everything is checked on paper by agents**, who can share a blind spot.
- **Your time is not capped.** An argument that still fails after eighteen rounds reaches you with its
  counterexample, with no limit on how many. The sign-off reads have no page limit.
- **Later steps wait for your answers.** Step 4 cannot finish without the edit answer, and seven steps
  rely on it.
- **This summary has not yet been compared with the plan by an agent that did not write it.** The plan
  requires that comparison, item by item, before you read this page.
- **Worst case for starts.** If runs can neither be unattended nor resume, eight arguments alone take
  about twenty sessions.
- **The final both-way read is large**, and of unknown size.
- **The two confirmation checks differ on one point**: whether "every state can reach an end state" is
  settled by canon or is yours. The plan treats it as settled and tells you at the sign-off.
- **The list of disagreements in canon is incomplete.** Each step reads its documents in full.
