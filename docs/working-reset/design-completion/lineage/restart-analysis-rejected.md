# Precept restart: what went wrong, and a shorter way forward

Written 2026-11-13 against branch `spike/Precept-V2-Radical-reset` at `2f126a3b`. Nothing in the repository was edited.

---

## One-page summary

### What went wrong

1. **On 2026-06-05 the work left code, where a claim can be checked by running it, and moved into prose, where a claim can only be checked by another reader.** It never came back. A reader of 26,000 lines of design documents always finds something, so every review produced findings, every finding produced a record, and every record became one more thing to keep consistent. The number of findings never fell, because it measured the amount of prose, not the distance from your design.

2. **Most of the prose being reconciled describes a compiler core and a runtime that have not been built.** The disagreements phase 2 could not finish are almost all in the compiler and runtime documents, none in the language documents (plan:66–69). They are things like how many diagnostic codes exist and which members a record type has. Code states those once. Prose states them in several places, and the copies drift.

3. **Every other way of settling a question was ruled out, one correction at a time, until only you were left.** Samples are not evidence, compiler behaviour is not evidence, catalogs are not evidence, agent labels are not evidence, the main session is not an authority. Each rule answered a real failure. Together they left your own words as the only thing that could close an argument. That is why your time became the bottleneck. It was built into the arrangement.

4. **The want document contains its own acceptance test, and nobody ran it.** I ran the "what must not compile" table today. Of thirteen mutations, the compiler rejects two, warns on two, and accepts nine without comment. That one result says more about the distance to your goal than the month of phase 2 did.

5. **The earlier diagnoses were accurate, and each one prescribed more documents.** The reset plan put ten phases and six to ten weeks of definition ahead of the first line of product code (plan:208–210).

### The proposed path

- Make the want document's table the first test suite. The scoreboard today is 3 of 15 behaving as you wrote (the example compiling, plus two rejections).
- Build the missing piece directly: every rule becomes something the compiler must prove at every handler that writes a field the rule mentions, and anything it cannot prove is an error. A working prototype of the central calculation already exists in `tools/Precept.MatrixTools/WpCalculator.cs`.
- Build a small evaluator early and use it to attack the compiler: run random events against every definition the compiler accepts and look for a broken rule.
- Stop phase 2 where it is. Retire the thirteen-phase plan. Keep the want document, the philosophy, the language documents, the code, and the bug list.
- You get one short session a week: four numbers, and at most three questions, each shown as a `.precept` example with the message an author would see. No documents to review.

### What I need from you

Four to start:

1. May phase 2 stop without a sign-off, and the thirteen-phase plan be retired?
2. May the documents describing compiler and runtime internals stop being a gate, and be rewritten from the code once it works? The want document, the philosophy and the language documents stay the target.
3. May a small evaluator be built now, ahead of the rest of the runtime, so that tests can execute accepted definitions?
4. As a standing default: when it is unclear whether a proof may rely on something, may the compiler refuse and the case go on a list you can overturn, instead of coming to you first?

Four that do not block the first week:

5. Two non-terminal states that lead only to each other: is that a dead end?
6. Should a rule be allowed to mention the current time, or does time belong only in guards for now? (The temporal document currently allows it; your want document left it open.)
7. When an editable field is edited, are all rules that mention it evaluated against the proposed value before it is accepted, as line 143 of the want document says?
8. Does `from any` mean one row per state? And do a state's entry actions run at construction, and on a transition back into the same state?

---

## Part 1 — What the repository shows

**Code.** `dotnet build`: 0 errors, 2 warnings. `dotnet test`: 7,815 tests, 7,799 pass, 16 fail. Fourteen of the failures are `FieldRefBoundEnforcementTests`, written on 2026-06-07 for a feature that was never built. The other two are a sample fixture (`Test2.precept`) and a parser test. All 32 `NotImplementedException` sites are in `src/Precept/Runtime/`. The lexer, parser, name binder, type checker and graph analyzer are complete in the sense that nothing is stubbed. `docs/Working/bugs.md` lists 50 active bugs and 19 fixed; the word "soundness" appears 42 times.

**History.** Commits touching `src`, `test` or `tools`: 515 in May, 17 in June, 15 in July, 0 in August. The last feature commit is `210229ff` on 2026-06-05. The July commits are almost all `tools/Precept.MatrixTools`, a 5,700-line tool built to manage a design document.

**Documents.** The canonical documents are about 31,000 lines. `docs/Working/` and `docs/working-reset/` together hold about 423,000 lines, including generated data. Phase 2 alone produced 18,166 lines of records. Its net effect on the canonical documents was 926 lines added and 954 removed across 22 files (`git diff --shortstat b93d1db1 HEAD`). That is roughly ten lines of record for every line changed.

**The want document's own test.** I put the worked example and thirteen of its fourteen mutations through `Compiler.Compile`. This shows what is built. It says nothing about what is right.

| Mutation from the want document's table | Compiler today |
|---|---|
| The example unchanged | Compiles clean |
| Delete `Withdraw`'s guard | Compiles clean |
| Delete `ReduceLimit`'s guard | Compiles clean |
| Remove `nonnegative` from `Deposit`'s amount | Compiles clean |
| Remove `min 0.0` from the opening balance | Compiles clean |
| Remove `nonnegative` from the `OpenAccount` limit argument | Compiles clean |
| Remove `positive` from `Months` | **Error**, division unsafe |
| Remove `PlanRepayment`'s `Balance < 0.0` guard | Compiles clean |
| Remove `PlanRepayment`'s cap condition | Compiles clean |
| Remove `ReduceLimit`'s cap condition | Compiles clean |
| Change the default to `20000.0` | **Error**, violates `max` |
| Add a state nothing reaches | Warning only |
| Delete the `Unfreeze` rows | Warning only |
| Make `CloseAccount`'s guard contradict the rule | Compiles clean |

(The tenth row of the want table, guarding on `Balance < 0.0` alone, is the same edit as the cap-condition row, so I ran it once.)

The compiler today uses a declared rule as a fact that helps other proofs. It never treats a rule as something a handler must be shown to keep true. `proof-engine.md:2614` says so itself ("there is no general carrying of 'every rule held before this operation' into a handler's discharge"), and the table above is what that looks like from outside. This is the centre of the want document, and it is absent from the code.

**The central calculation already exists, in the wrong place.** `tools/Precept.MatrixTools/WpCalculator.cs` was written on 2026-07-19 (`61fa9022`). Given a handler and a rule, it works out by substitution what must be true beforehand for the rule to hold afterwards. Its tests in `test/Precept.MatrixTools.Tests/BankExampleTests.cs` show it producing the right condition for `Withdraw`, `ReduceLimit`, `Deposit` and `PlanRepayment`, and telling a correct guard from a near miss. It was built to fill in cells of a design document. It was never connected to the compiler. Its header says handlers with more than one write are "an open owner decision" and are "deliberately not implemented" (`WpCalculator.cs:17–20`). The method for that case is in any textbook on program verification, and you had in any case ruled on 2026-07-26 that a handler is judged on the configuration it commits (plan, "Work the 2026-07-26 walk created").

---

## Part 2 — Why it happened

### 2.1 The trigger was real

On 2026-06-01 you caught a slice claiming to close a gap that it had only partly closed, with a green test suite and a design document that had quietly reworded the principle to fit (`feedback_no_redefining_the_bar.md`). The conclusion drawn was sound: building the proof engine one bug at a time was accumulating holes, and it needed a definition. The mistake was in what came next. The response to "the code is being built without a clear target" was to stop building until the target was complete on paper.

### 2.2 Prose has no finish line

In code, "done" has a mechanical meaning: the test passes. In prose the only check is a reader. The record shows what that does:

- Phase 2 was sized at two to three days and 35–55 disagreements. It found 123, then 145 (plan:1410–1416, commit `42d6accc`).
- A fresh reader then found 28 more, of which 19 were in no row (`phase-2/cold-read-register-diff.md`).
- Four scripted checks built the next day found 14 more (plan:70–74).
- When one careful reader was tested against known items in its own territory, it missed four of them (`project_reset_phase_1_2_2026_07_28.md`, "Reverse check").

A search that keeps finding new items at the same rate is not approaching the bottom. This is the normal behaviour of a large body of prose that says the same things in several places.

The reviews had the same property. The 2026-07-01 diagnosis already recorded that "full reviews of a 1,749-line source-line-citing doc never return zero findings". On 2026-07-21 nineteen of nineteen adversarial passes returned defects. A reviewer told to refute will find something to refute in any long document. In code, a reviewer who says "this is wrong" has to produce an input that fails.

### 2.3 The documents being reconciled describe things that do not exist

`docs/runtime/evaluator.md` is 2,183 lines. The evaluator is a set of method signatures that throw. `docs/compiler/proof-engine.md` is 2,653 lines, and the part the want document cares about is not in the code. These documents were written by agents ahead of the build, and they are constrained by nothing except each other.

Look at what phase 2 was stuck on at the end (plan, exit condition 4): a record type with seven members in one document and eight in another; a diagnostic list that says 147 in one place and 164 in three; a rule number range given two ways; a pass order listed two ways. Eleven of the fifteen scripted findings came down to "this type is written out in two documents". For the ones that exist in code, the code has a single answer and one search finds it. For the ones that do not exist yet, any answer is a guess that the build will overwrite.

On 2026-08-28 the instruction was "forget about the code for now, this is about getting the design right" (commit `2f126a3b`). I think that instruction, though understandable, pointed the wrong way. For facts about internal types and counts, the code is the only place that can be right.

Your own position on 2026-08-27 was that if two canonical documents disagree, canon has no single position. That is logically correct. But it made consistency across 26,000 lines of agent-written prose a condition for starting, and that condition cannot be met by reading.

### 2.4 The rules of evidence left one judge

This is the part I expect to be least welcome. The assistant's memory folder holds thirty-seven standing corrections from you. Read individually, nearly every one is right. Read together, this is what they say may not settle a question:

- the samples (`CLAUDE.md`, "The samples are examples")
- what the compiler does today (same section)
- the catalogs (`CLAUDE.md`, "Catalog System")
- any "locked" or "ratified" label, because agents wrote them (`feedback_agent_authored_labels_not_owner_authority.md`)
- the specification itself, when cited over you, because agents drafted it (same file)
- earlier plans (plan, "What may not be used as evidence")
- the main session's own reasoning, checking, or analysis (`feedback_main_loop_not_authority.md`)
- at one point, sixteen lines of the want document, because an agent typed them

What remains is your verbatim word. So every question, however small, had to travel to you, and the agents then built machinery to manage the traffic: a register of who decided what, a walk through it, a trace of every edit back to something you said, a check of the trace. The memory file on the main session says "you cannot fix an authority defect from inside the authority" and prescribes an independent agent for every claim. That multiplied agents and documents, and each check was still a reader reading.

The agents made this worse in a specific way that the memory records accurately: they turned textbook or already-answered matters into "owner decisions". Four of four checked on 2026-07-24 were already answered; four of five put to you on 2026-08-27 were outside the phase's scope. I read that as a consequence of the arrangement more than a quirk of the model. When nothing but your word closes a question, sending it to you is the only safe move an agent has.

### 2.5 Completeness was demanded up front, where it is least available

"Enumerate the whole family", "no silent cells", "the canonical documents should be comprehensive". The matrix effort tried to list every combination of obligation and expression form in a document before building. The fault family count moved thirteen, eleven, ten, nine, and each move discarded finished work (plan, "A closed list stays closed").

The want document does not need that list to exist on paper. Its guarantee is "prove or reject". If the compiler generates an obligation for every rule at every handler that writes a field the rule mentions, and treats anything it cannot prove as an error, then a gap in its proving ability shows up as a definition that will not compile. That is visible and annoying, which is what you want from a gap. The old engine was built the other way round: obligations were attached case by case and anything unrecognised was skipped, so a gap showed up as silence. Your ruling of 2026-07-26, "a minimal proof set for the MVP, but we can and will expand the compiler's proof power over time", already accepts this. It was recorded and the enumeration effort carried on.

### 2.6 The feasibility question was never asked the cheap way

The want document says at line 10 that it describes an ideal, and that the next task is to find out whether one person with agents can build it. That task was turned into a definition effort. The cheap version is to try the worked example. The reset plan did include a cheap early stop (phase 4, count the certificate steps) and placed it two and a half to three and a half weeks in, behind phases 2 and 3. It was never reached.

### 2.7 Why acting on the earlier diagnoses did not help

| Diagnosis | What it got right | What it prescribed | What happened |
|---|---|---|---|
| 2026-07-01 (memory file) | The plan's finish line was "survives another review"; no code since 06-05 | Tidy the plan, one status page, answer the open gates, then ship the first slice | A third plan, a plan about the plan (1,067 lines), and a gap list by 07-13. No slice. |
| 2026-07-25 (`where-are-we`) | Code frozen fifty days; the status page wrong on five of seven items; three design passes failed to settle | A reset: thirteen phases, ten before product code, with rules to stop loops | Phase 2 ran a month and did not close |
| 2026-07-28 (memory, "the loop diagnosis") | Each pass found that the thing the previous pass measured against was unverified | Close with defects recorded | The exit condition was widened and thirty more findings arrived |

Each diagnosis treated the problem as a lack of discipline inside the document process and added controls to that process. The controls were applied by the same kind of agent, in the same medium, that they were meant to restrain. The plan's own rule says three failed attempts means stop and ask you whether to revert. The board shows "1" in the attempts column for phase 2 after at least eight passes (plan:193). The tracker's opening section spends about forty lines describing the times the tracker itself went stale (plan:26–45).

None of the three diagnoses said the medium was the problem. I think it was.

### 2.8 What I am inferring rather than reading

- That the rules of evidence caused the bottleneck is my reading of the memory files as a set. No single file says it.
- That a stronger model would not have fixed this by itself. I believe a better model in the same arrangement writes the same documents faster. I cannot test that.

---

## Part 3 — The path forward

### 3.1 The principle

The want document and the philosophy say what Precept must do. The language documents say what an author can write and what it means. Everything about how the compiler and runtime do it is settled by building it and attacking what was built. Tests and the compiler's output on examples are the record. Documents about internals are written after the code works, from the code, and kept short.

### 3.2 Keep, drop

**Keep as the target:** `what-i-want-2026-07-16.md`, `docs/philosophy.md`, `docs/language/`.

**Keep as working material:** all of `src/` and `test/`; `bugs.md`; `WpCalculator.cs`, `Canonicalizer.cs` and their tests, moved into the compiler; the rulings you actually gave, copied onto one page with your words (no post-mutation re-evaluation; a declared premise decides, not where a value came from; a minimal proof set that grows; errors say whether a case is proven unsafe or merely unproven; three warnings rise to errors; same-basis periods order, both halves; overflow after the MVP; the question of who constructs a `Fault` waits for the runtime).

**Stop treating as a gate:** `docs/compiler/proof-engine.md`, `docs/compiler/soundness-and-coverage.md`, and `docs/runtime/*`. Mark them as design notes that the code will replace. Do not delete them; they contain good thinking that the builder should read once.

**Archive:** the thirteen-phase plan, the phase folders, the register, the trace, the matrix and its cells, the five earlier plans, and the rest of `docs/Working/` except the want document and `bugs.md`.

**Replace the plan with one page:** the scoreboard, the current slice, the defaults taken, the questions for you.

### 3.3 Phase 2, and the open findings

Stop it. No sign-off is needed, because nothing downstream will read the register. The 145 corrections already made are in the canonical documents and stay there. Of what remains open:

- The twelve disagreements between compiler and runtime documents, and the fifteen from the scripted checks: leave them. They are about internal types and counts. They get resolved when the code that defines them is written or, where it exists, by reading it.
- The four between the want document and canon are real, and small. Two are short questions about meaning: what counts as a dead end (question 5), and how `ensure` on an event fits your statement that an event's relational needs live in its guards (not yet on the list above; it can wait for the slice that touches it). One is which artefact carries the certificate out of the compiler, which is decided when the certificate is built. One is that the want document does not mention `ensure` at all, which needs a sentence from you at some point and blocks nothing.

### 3.4 The eleven questions waiting on you

| Question in the plan | What I propose |
|---|---|
| Where the set of provable formulas stops | Not a decision. It is whatever the compiler can prove this week. You see it as a list of sample files that stop compiling and what each would need. |
| Must a proof name what establishes and preserves each fact it uses | Wait until the certificate is built. Start with the fuller record, since line 199 of the want document lists premises by name. Cheap to reduce later. |
| Where a divisor's non-zeroness may come from | Default: only from something declared on the divisor itself. Anything else is refused as unproven and goes on the list. |
| May a proof use the fact that an earlier row did not match | Default: no. Add it when a real definition needs it. |
| May a computed field's formula establish a constraint on it | Default: no. Same. |
| May "the entity is in state S" be used as a premise | Default: no. Same. |
| Integer arithmetic model | Stays with overflow, which you deferred until after the MVP. |
| What discharges a rule when an editable field is edited | Question 7. I read line 143 of the want document as already answering it; the plan says lines 171 and 199 pull the other way. One line from you settles it. |
| Does `from any` mean one row per state | Question 8. |
| State actions: how many, and when entry actions run | Question 8. |
| May a rule mention the current time | Question 6. |

Six of the eleven go away under the default in question 4, which is the trade the want document already makes at line 189: the guarantee is fixed and the language is the negotiable part. One waits for the build. Four are short.

### 3.5 How the work is checked without you reading documents

1. **The want table as tests.** Fifteen tests: the example compiles, and each mutation is an error whose message names the missing premise. These are your words turned into assertions, so the agent building a slice did not write its own exam.

2. **Premise deletion on every sample, by machine.** The want document's idea, "delete a premise and the proof must fail", can be applied automatically: for each sample, remove each guard condition and each modifier in turn. Each mutant is either rejected by the compiler or accepted.

3. **A small evaluator that attacks accepted definitions.** For every definition the compiler accepts, including the accepted mutants, fire a few thousand random events with random arguments, apply ingress checks as the want document describes, and check every rule after every step. A rule that breaks, or a division by zero, is a compiler bug with a concrete counterexample attached. This uses the samples as inputs to a test. It makes no claim that they cover the language, and random runs can show a hole exists but cannot show none do.

4. **The cost report.** How many of the sample files still compile under prove-or-reject, and for each that does not, the message. This is the feasibility study the want document asks for at line 133, as a measurement.

5. **One independent review per slice, of the code diff only**, by an agent told to produce a failing `.precept` input. A finding without a failing input is not a finding.

6. **Later, the certificate checker**, small and separate, as the want document describes. It comes after there are proofs to check.

### 3.6 How the stronger model is used

- One agent with the want document, the language specification and the proof engine source all in context builds a slice end to end and runs the tests itself. The old arrangement passed summaries between many small agents, and each handoff lost detail.
- Agents do not write documents for other agents to review. They write code, tests, and a half-page slice note.
- The expensive judgement is spent in one place: each kind of proof step the compiler is allowed to take gets a short written argument and a set of attacking tests. That list is counted as it grows, which gives the plan's phase 4 number from real code.

### 3.7 The first two weeks

**Day 1.** One commit: archive as above, add the one-page plan, add the fifteen want-table tests (mostly red), tag the sixteen known-red tests so the baseline is honest.

**Days 2–6, slice 1: rules become obligations.** Every rule, and every modifier treated as the rule it abbreviates, generates an obligation at every initial event and every handler that writes a field it mentions. The compiler computes what must hold beforehand (the existing calculator, extended to several writes in sequence), then tries to close it from the guard, the argument constraints, the field modifiers, and the rules assumed true beforehand. If it cannot, that is an error naming what is missing. Target: the nine silently accepted mutations are rejected and the example still compiles.

The hard case is `Deposit`. Showing `Balance + Amount >= -OverdraftLimit` from `Balance >= -OverdraftLimit` and `Amount >= 0` needs a small amount of linear reasoning, more than substitution and matching. If the estimate slips, it slips here.

**Days 4–9, alongside: the evaluator and the attack runner.** Fill in enough of `src/Precept/Runtime/` to execute expressions, actions and events. Build the random runner and the premise-deletion runner.

**Days 7–10, slice 2.** Unreachable states, dead ends and dead rows become errors, as the want table requires. The rejection message suggests the missing guard for the linear cases (want document, line 179).

**End of week 2, your first session.** Four numbers: want-table tests passing out of 15; samples still compiling out of 78; accepted definitions broken by the runner; kinds of proof step in use. Plus the list of defaults taken, and up to three questions. Then you decide: carry on, narrow the language, or stop. That is the plan's phase 8 decision, reached with data in two weeks.

**After that**, in rough order: fault obligations (division, range) brought under the same prove-or-reject verdicts; editable-field ingress; the certificate and its checker; the active bug list, each bug first becoming a failing test; then short replacements for the internal documents, written from the code.

### 3.8 What you are asked, and how often

- Now: the four starting questions.
- Weekly, about half an hour: the four numbers, the defaults list, at most three questions. Each question arrives as a `.precept` example, what each answer makes the compiler say, and what stops compiling. The plan already had this rule (plan, § 4 "No row here is put to you as an abstract fork") and it is worth keeping.
- Anything else is decided by the default in question 4 and logged for you to overturn.
- You are not asked to review a document longer than a page.

---

## Part 4 — Risks, and what is lost

**The June failure could repeat.** Code was stopped because a slice claimed a result that green tests did not support. Three things are different: the first tests come from your document, not from the builder; the random runner does not read prose and cannot be argued with; and because every rule generates an obligation and unproven means error, a gap appears as a refusal and not as silence. This does not cover obligations that come from operators and functions (division, indexing), which are still attached case by case. The runner is the only net there until the certificate checker exists.

**Building against documents with known contradictions will cause some rework.** Some piece will be built to the wrong reading and redone. I judge that cheaper than the alternative, having watched the alternative for three months, but it is a real cost.

**The paper assurance of completeness is given up for now.** The matrix aimed at a written argument for every case before any code. This path replaces that with refusal by default, machine attack, and later a checker. Random testing finds holes; it never shows there are none. The written arguments come back, in smaller number, attached to the certificate's step kinds. If that list turns out long, this is where you will see it, at week two rather than month three.

**The compiler may refuse a lot.** Under prove-or-reject with modest proving ability, many samples may stop compiling. That is the answer to the feasibility question, and it may be an unwelcome one. It is better known in two weeks.

**The worked example may itself need a correction.** The assistant's memory records that `ReduceLimit`'s guard reads the optional `MonthlyRepayment` without checking it is set, which the specification is said to refuse. I did not verify the specification line. The current compiler accepts the example, which may be one of the open presence bugs. Slice 1 will hit this and bring it to you with the actual message.

**Your rule that the canonical documents are what completeness is measured against is narrowed.** Under this proposal it holds for the language documents and stops holding for documents about internals. That is a reversal of something you have said many times, and it is your call (question 2). If you say no, the alternative I would suggest is to delete the internal documents' restated types and counts and leave only reasoning in them. That removes most of what phase 2 could not settle.

**Less of your attention means more agent defaults.** The defaults list has to stay short and readable or it becomes the register again. If it passes about twenty open entries, that is the sign to stop and talk.

**The early evaluator could drift from the runtime design.** It should be written as the start of the real evaluator in `src/Precept/Runtime/`, not as a throwaway. The runtime documents will then be partly overtaken by it.

**Two weeks is an estimate from the May commit rate and from the calculator having been built in a day.** It could be four. The way to tell slow from wrong: slow is the `Deposit` reasoning taking longer; wrong is the example refusing to compile while its mutations are also refused, with no small change fixing both.

**Material that is archived is not lost, but will not be maintained.** The period-ordering design, the divisor research and the editable-write analyses under `docs/working-reset/phase-5/` are worth one read by whoever builds the matching slice.

---

## Sources read

`docs/Working/what-i-want-2026-07-16.md` (all); `docs/philosophy.md`; `docs/working-reset/compiler-readiness-plan.md` (§§ 1–5, "How this plan is executed", phase sizes, phase 2 exit condition 4); `phase-2/five-items-walk-2026-08-27.md`; folder listings and sizes for phases 1–5 and 9; `docs/Working/where-are-we-2026-07-25.md`; the memory index and thirteen memory files in full; `git log` from 2026-06-05; `tools/Precept.MatrixTools/WpCalculator.cs` and `BankExampleTests.cs`; `src/Precept/Compiler.cs`; `docs/Working/bugs.md` headings. I ran `dotnet build`, `dotnet test`, and a throwaway console program in the scratchpad (`probe/`) that compiled the want example and its mutations.

Not read closely: `cold-read-register-diff.md` beyond what the plan quotes from it; the contradiction register itself; the five earlier plans beyond their sizes and the memory summaries of them; `docs/README.md` and `docs/language/README.md`.
