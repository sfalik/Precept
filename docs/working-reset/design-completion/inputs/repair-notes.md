# Repair notes — design-completion-plan-v3.md, one pass, 2026-11-22

The plan before this pass is `design-completion-plan-v3.before-repair.md`. Section numbers below are
those of the repaired plan. One new working file was written beside it, `rulings-routes.md`, because the
ruling row numbers and the refusal-barred rows had to go somewhere once they left the page Shane reads.
Nothing in the repository was edited.

Sources I re-read to make these fixes: `precept-language-spec.md:96, :191, :225, :256, :268-270, :1456`;
want `:14, :34-36, :52-55, :101, :127, :135, :143, :151-152, :171, :183-189, :199, :204`;
`proof-engine.md:109, :2092`; `primitive-types.md:667`; `collection-types.md:1296-1300`;
`compiler-readiness-plan.md:280, :282`; `phase-2/trace-walk-rulings.md:42-46`; the memory file line for
ruling 92; and the cited lines of the precedent, proof-model, rulings and failures files.

## Findings from the "failures" check

1. **Fired stop rules have no clearing route; thresholds unprotected — fixed.** § 4.5 now has a
   "Cleared by" column, a `clearances.jsonl` file only a script writes (his words in `answers.md`, or a
   repair file by an uninvolved agent plus a condition the script computes), counts that restart from the
   clearance, and a rule that a second firing within three runs of a repair needs his words.
   `settings.json` is hash-checked against `answers.md` by `plan-lint`; measured run ceilings moved to
   `measured.json` so a script-set number is not a threshold change. Step 0 test 2 now tests staying
   fired and clearing.
2. **Found-item growth exempt from every stop — fixed.** New rule "Found faster than closed" (three
   rising runs in a step, or twice the count at the end of step 1), producing one sheet with the trend
   and a grouping by cause. Stated that it covers step 15's hand walk. I did not change "No progress"
   to ignore rounds: an argument needs four attack runs to pass, so a closures-only test would misfire;
   the plan now says rounds per argument are bounded (12, then 6, then Shane) and the new rule covers
   the rest.
3. **Finish condition 6 met by open items with an owner — fixed.** Split into conditions 6 (findings,
   parked items, open questions in canon: closed) and 7 (handoff and deferrals: owned, and of an allowed
   kind). `status` fails any open row whose owner step has finished. "Named exception" replaced by "a row
   citing his quoted words"; one is known.
4. **Rulings retired by an agent; no route from answers to rulings — fixed.** § 5: "superseded" and "no
   longer applies" need the later words quoted and a `uses/` file, otherwise the row stays open and is
   listed at a sign-off. `rulings-check` appends a ruling row for every new `answers.md` row. The
   memory-only ruling on approximation is no longer "recorded as not applied" when a search is empty.
5. **Condition 5 is a count of what a script can see — fixed.** § 1 states the mechanical part and an
   agent part over a closed list `dedup/pairs.jsonl` (one row per document and owned fact, filled one
   document at a time, counted by script, re-run at step 15).
6. **refusal-check checks only the label — fixed.** § 4.3: every refusal is an argument and goes
   through attack rounds; "this refusal narrows the design" is a fixed flaw kind; every reason-4 refusal
   needs a `uses/` file; first-build and build-order rulings are barred (list in `rulings-routes.md`).
   I added the schema-evolution-at-restore ruling to the barred list, since § 5 also classes it as build
   order.
7. **Stop rules depend on agent classification or fire on normal progress — fixed.** One list of flaw
   kinds, written in step 0 by an agent that authors and attacks nothing, hashed. Dependencies come from
   a script reading citations plus a fresh agent, not the author. Reopenings by dependency are counted
   apart and excluded from "Reopened work rising". The three-escalation threshold is now "at least three
   and more than a third, since the last clearance". Step 11's "changed arguments only" corrected.
8. **Step 14 unbounded and writing in the proof model's document — fixed.** Placeholders in
   `soundness-and-coverage.md` go to the owning steps; that document and `proof-engine.md` are on the
   frozen list whole; new stop rule "Tidying runs ahead" pauses step 14 at twice the proof-model lines.
9. **Clock and certificate questions deadlock — fixed.** Clock is in the first batch; the certificate
   question is conditional and no longer shares a batch with it. § 6.1 has a script rule for when a
   batch goes out and a check against a question waiting on a step that waits on it.
10. **Step 0 open-ended — fixed, with one limit.** `step0-guard` is built first and bounds runs, script
    lines and stalled runs. The script list is closed (fifteen in step 0, six named for later steps,
    `plan-lint` fails extras). The main session writes no script or test; three separate agents write,
    test and run. Every test in § 7.6 now has a fallback that lets step 0 finish. Limit: the guard's own
    numbers (ten runs, 4,000 lines) are my starting values, not measured.
11. **Fourth restart document, full of codes — partly fixed.** All conflict codes and ruling row
    numbers are off the page; § 2.3 and § 5 are in words; numbers live in `rulings-routes.md` and the
    conflict codes in the input files that step 0 copies into `technical/conflicts.jsonl`. I ran the
    banned-word list and a code pattern over the page by hand: no hits. § 6.3 now says he signs a
    two-page plain summary. **Not done: I did not write that summary.** Not changed: committing the
    inputs in step 0. `cite-check` can only open committed text and failure 15 was work held outside the
    repository, so the inputs have to be committed; the plan now says why.
12. **Total record volume unbounded — fixed.** New stop rule "Records outgrow the design" (ten record
    lines per canon line, from a step's third run), threshold in the protected settings.
13. **Rule 5 not checked against the touchpoints — fixed.** New § 6.5 counts every contact. The dropped
    sample is re-checked by a fresh agent and he gets only the error rate. Second-author failures are one
    sheet per step. The fallback-restart arithmetic is stated.
14. **Width check and trailer-check stop nothing — fixed.** `trailer-check` verifies the output file and
    reverts a failing commit; a fresh agent reads each canon diff for uses of his words that cite no row.
15. **Smaller contradictions — fixed.** Memory index named as the one outside path; contents drafts
    compared item by item by a third agent; § 4.7 says how a closed list reopens and which rows re-run;
    the scratchpad sentence corrected; the model-rejection rule now says it cannot work if test 5 falls
    back, and § 10 lists that.

## Findings from the "rulings-and-rules" check

16. **Refusal reasons: spec:225 test 3 misnamed; first-build rulings quotable — fixed.** Reason 2 is now
    test 1 plus determinism cited to spec:96. Caps (test 3) need a written cap and an attacked argument
    from canon alone. Barred rulings as in finding 6. I confirmed in spec:225 that test 3 is the
    "right-sized … hard cap" test.
17. **Reachability promise weakened by agent decision — fixed.** The hard-problem row is now
    "Reachability to the standard he wrote" (want:151): show reachable or reject. The precedent file's
    sentence is explicitly not adopted. If the write-up shows the standard cannot be met it goes to him
    as a conditional question with a worked case.
18. **Sign and monotonicity facts left to agents — fixed.** § 2.2 states his approved text binds
    (want:135, spec:270); such facts may only serve a permitted rewrite; a case where bounds would prove
    what that text rejects goes to him as a conditional question.
19. **Editable-write question framed as a confirmation — fixed.** Question 1 gives the three options
    from the old plan on equal terms and names the pull between want:143 and want:171/:199. The
    hard-problem row and step 4 cover all three until he answers.
20. **Whole-model sheet — fixed.** Three fixed choices, weakening the guarantee not offered, and the
    sheet passes the five-rule check.
21. **Count of what he is asked understated — fixed.** § 6.5: fourteen certain decisions or reads plus
    sixteen start lines, then each open-ended kind with what bounds it. Conversations do not count
    against the sheet cap. The permission decision is named in step 0.
22. **Fault deferral and "named exception" — fixed.** I chose: stays deferred on his words (he said
    "building the runtime"; reading that as "designing" would widen it), listed as the only exception,
    with one line telling him so and that he can say otherwise. The general "named exception" is gone.
    This is a judgement call between his rule 2 and his own deferral; he should see that line.
23. **Two ruling routes fail condition 4 — fixed.** The research waiver is carried by lines in steps 2
    and 5; NodaTime is a build-stage rule in `handoff.jsonl`; the "constraint is the wider word" ruling
    is routed to steps 3 and 4 (I confirmed his words at the memory file's line 175); the bounds-example
    ruling has an outcome for each search result.
24. **Question 5 skipped the filter; question 3 bundled — fixed.** The certificate question is
    conditional, with the catalog ruling and the internals rule as checker inputs. State actions are
    three sheets.
25. **Two conversations have no place in the step order — fixed.** § 6.2 table and the wait lines of
    steps 2, 3 and 5 name the steps and what they write meanwhile.
26. **Internals get one attack — fixed by stating why.** § 4.2: an internals default is a decision, not
    an argument; `attack-check` fails any argument that cites one; if one is needed by an argument it
    gets twelve rounds.
27. **Plan full of row labels — fixed.** See finding 11.
28. **Contents lists compared by count — fixed.** See finding 15.
29. **precept-author consults the compiler — fixed.** Removed; syntax comes from the grammar sections
    with a second agent confirming.

## Findings from the "technical" check

30. **Overflow question about `integer` but example is `decimal` — fixed.** I confirmed `Balance` and
    `Amount` are `decimal` (want:34, :52). The `decimal` overflow model is its own sheet with `Deposit`
    as the worked case; the `integer` sheet uses an integer example; the hard-problem row attributes the
    evidence to `decimal`.
31. **Step 9 needs proof steps closed earlier — fixed.** Step 9 writes its list of needed proof steps
    as soon as step 4 passes; step 6 cannot close the contents list without it and names the
    no-solution and exhibit-values steps; step 10's piece on graph verdicts needs step 9; the
    exhibit-an-exit requirement is in step 9.
32. **Induction does not depend on dead-row removal — fixed.** Step 11 needs step 9 for that piece;
    contents item 16 names it; the sign-off waits for steps 1 to 11.
33. **Two hard problems with no owner — fixed.** Added the zero `number` product (steps 2, 6) and units
    the exact model cannot hold (steps 2, 5); `proof-engine.md:109` and spec:1456 added to the
    permanent-limits conflict. Hard problems are now fifteen. I did not use the finding's phrase
    "irrational-scale" as a fact, since I found no canon line naming such a unit; the row says "units
    whose conversion the exact model cannot hold".
34. **Collections table too narrow; count multiplies two lists — fixed.** All predicate forms, one row
    per kind and action; the count is of cells that canon gives, and the text says why this is not the
    forbidden product.
35. **Conditional waits not listed — fixed.** Steps 1, 3 and 4.
36. **Steps 1, 2, 3 side by side — fixed.** Steps 1 and 2 side by side, step 3 after step 2. This also
    exposed that the checkpoint waited on step 3; it now starts from step 1's first section and states
    its rules in full.
37. **Rows hidden behind earlier rows — fixed.** Contents item 13 and step 9.
38. **String facts missing — fixed.** Contents item 9 and step 5.
39. **Linear integer completeness against cost — fixed.** Named in the combining-facts row and step 7;
    strict inequalities named in step 6.
40. **Frozen list grows after step 14 has merged — fixed.** § 4.6, last paragraph.

## Not fixed, or left for someone else

- The two-page summary for his sign-off is required by § 6.3 but not written.
- The page grew from 1,002 to about 1,300 lines. It is still one page with no change layers, but
  longer; the summary matters more as a result.
- New starting numbers are mine and unmeasured: step 0's ten runs and 4,000 lines, the two-to-one
  tidying ratio, the ten-to-one record ratio, three rising runs, twice the step-1 count, the cap of
  fifteen sheets. All sit in the protected settings and are his to change.
- The first batch is now seven sheets and two conversations. That is the honest count after splitting
  bundled questions, but it is more in one sitting than the old "four per batch".
- I did not re-verify citations carried over unchanged from the plan before repair.
