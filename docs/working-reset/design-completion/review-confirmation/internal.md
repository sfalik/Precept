# Confirmation of the internal checks on the plan's previous repair

**What was checked.** The two internal checks recorded in the workflow journal for the repair of the
design-completion plan (`journal.jsonl` lines 3 and 4 of workflow `wf_e81893e4-b0c`). The first check
asked whether forty earlier fixes landed. It reported nine as landed only in part, but its findings array
holds eight entries. The second check asked what the repair broke, and reported nine serious and eight
minor findings. Each finding was read against the plan as it stands now
(`docs/working-reset/design-completion/design-completion-plan.md`, 1,302 lines). That file has had one
commit (`b177ce69`) and is the same text both checks read. The line numbers they give still match.

The plan has not changed since the checks ran, so nearly every finding is still true as written. One
thing did change after the checks: the two-page summary now exists. The journal shows a separate
`write:summary` agent producing it after both checks had finished (journal line 8). It is committed as
`plan-summary.md`.

Where a finding is the same defect as one in the outside reviews, it is named below so the two are not
counted twice. "Astra" means `outside-review-report-2.md`, "Astra follow-up" means
`outside-review-report-2-followup.md`, and "Astra follow-up 2" means
`outside-review-report-2-followup-2.md`. "Neon" means `outside-review-report.md`. Plan line numbers are
written `plan:N`.

Seventeen defects are confirmed. After merging the places where the two internal checks found the same
thing, six of them are the same defect as an outside-review finding, and eleven appear only in the
internal checks. Three claims are rejected.

---

## Confirmed, and the same defect as an outside-review finding

These are true of the current plan. Count each one once, under the outside-review finding named.

### The "tidying runs ahead" stop rule cannot clear once the proof model is finished

Second check, serious finding 3. Plan:792 pauses step 14 when "Lines step 14 has changed in canon
exceed twice the lines changed in the proof-model sections", and it is cleared by "The script, when the
proof-model lines catch up". After the proof-model sections stop changing, the lines they have changed
never catch up, so step 14 stays paused. If the general clearing paragraph applies instead (plan:773-779),
the count restarts and the rule fires again, and a refire within three runs of a repair can be cleared
only by his words.

**Same defect as** Astra's major finding "The stop rules can halt correct work with no autonomous way to
finish" ("After the proof text is finished, a cleanup step above its cap has no remaining proof text to
wait for") and Astra follow-up 1.6/2.6, which checks Neon 2.6. Astra follow-up 2 says to keep the priority
and cut the twice-lines test.

**Change:** release the throttle at the proof-model sign-off, or measure step 14 against its own rows.

### The "records outgrow the design" and "found faster than closed" rules fire on ordinary progress

Second check, serious finding 4, and first check, finding 6. Plan:800 halts a step "From a step's third
run on" when "lines of record written for it exceed ten times the lines it has changed in canon". Step 13
writes `handoff.jsonl` and no canon (plan:585-595). Step 0 writes no canon either, and it can run up to
ten times (plan:303). Step 15 changes canon only when it finds a defect. All three hit the rule just by
doing their work. Plan:799 fires when found-and-not-worked rows "have risen across three runs in a row",
which happens when each run finds two defects and closes one. Its second clause compares against "the
number recorded when step 1 finished" and never says whether a clearance resets that number, because
plan:777-778 resets only "every running count". Both rules clear only on "His words".

**Same defect as** Astra's major finding on stop rules ("Three productive runs writing handoff records and
zero canonical lines meet the halt condition"), Astra follow-up 1.1 and 2.2 (checking Neon 1.1 and 2.2),
and Astra follow-up 2's cut list ("Drop both raw-count triggers"; "Cut automatic rework based on physical
lines"). The internal check adds two details: step 0 also has no canon lines to measure against, and the
baseline of the second clause is ambiguous after a clearance.

**Change:** measure each kind of work in its own units, separate first-time discovery from a repair that
keeps failing, and test the rules on traces of successful work.

### `step0-guard`'s limits fire on ordinary step 0 work, and its notice is not counted

Second check, serious finding 5. Plan:303-305 stops step 0 when "the step has used ten runs" or "two runs
in a row passed no new test", and "Only his words clear it". Step 0 builds fifteen scripts, four workflow
scripts and the start command, and three different agents write, test and run eleven tests
(plan:298-332). A run that writes a script before its test exists passes no new test. The notice is
missing from the § 6.5 table (plan:1085-1096).

**Same defect as** Astra follow-up 1.3, which checks Neon 1.3 ("The defect is making *all* of this a
prerequisite to the first substantive design section"), and the `step0-guard` row of Astra follow-up 2's
cut list ("no ten-run production target"). The uncounted notice is Astra follow-up 4.1 ("the step-0
ceiling says 'Only his words clear it'").

**Change:** take the smaller step 0 in Astra follow-up 2, or at least make the stall clause "adds neither a
script nor a passing test" and count the notice.

### The § 6.5 count leaves out contacts, and one of its bounds is not enforced

First check, finding 3, and second check, minor finding 10 (except the parts placed under the
next two headings). These are left out of § 6.5:

- the `step0-guard` notice (plan:305);
- the "reopened work rising" halt, cleared by "His words" (plan:798);
- a refire within three runs of a repair clearance (plan:778-779);
- settings changes (plan:783-784);
- changes to the flaw-kind list (plan:691, "changes only through a row in `answers.md`").

Plan:1090 bounds second-author sheets at "One per step at most". But plan:708 sends a sheet whenever a
step "has no other piece it can run". If he chooses "more rounds or another author" and the argument fails
again, a second sheet goes out for the same step. Nothing stops it.

**Same defect as** Astra's major finding "The stated owner budget counts envelopes, not decisions", which
names the flaw-kind list and says "A failed-arguments sheet needs a choice **for each argument**". Also
Astra follow-up 4.1 and Neon 4.1.

**Change:** add the missing kinds with their bounds, and either enforce one second-author sheet per step
or drop the claim.

### The ownership list has no rule for how it is built, and the pairs file multiplies two lists

First check, finding 8, and second check, minor finding 13. Plan:76-77 generates
`dedup/pairs.jsonl` from "every pair of a canonical document and a fact on the ownership list".
`ownership.jsonl` appears only at plan:69 and :896, and nothing says how its facts are gathered or when it
is complete. A fact that nobody put on the list produces no row, so condition 5 reads zero while the
duplicate remains. The pairs file is also documents multiplied by facts, the shape step 5 rejects under
safeguard 18 ("never the product of two lists", plan:456-457, :1230).

**Same defect as** Astra's critical finding "Zero bad rows is not evidence that every required row exists"
("Omit the fact from the inventory entirely and there is not even a row to challenge"), and Astra
follow-up 1.5, which checks Neon 1.5 ("That is documents times facts").

**Change:** as Astra proposes, require exactly one full statement per fact, and also check in the reverse
direction, from each original passage to its kept statement or pointer.

### The summary now exists but does not do what § 6.3 asks of it

First check, finding 1, and second check, minor finding 17. Both checks said the summary was never
written. That is no longer true (see the rejected claims below). What is still true is that plan:1033-1037
requires the summary to state "what is kept and replaced from the old plan (§ 9), and everything in § 6.5",
and his yes adopts "the starting values in § 4.5" (plan:296-297). `plan-summary.md` says only that the plan
"replaces the thirteen-phase readiness plan" (plan-summary:3). It gives none of the § 9 rows and none of
the stop-rule thresholds. It also names the plan `design-completion-plan-v3.md` (plan-summary:3), which
is the scratchpad name, not the committed file. It inherits the wrong count of philosophy edits too
("up to three philosophy-edit approvals", plan-summary:46). And it says stop rules "clear only on his words
or an independent repair" (plan-summary:66), while four rules in plan:791-801 are cleared by script.

**Same defect as** Astra's minor finding "The adoption instructions point at the wrong copies" ("The
owner's summary names yet another filename") and Astra's major finding on the owner budget ("the reading
aid ... without stating the record and cleanup thresholds"). The missing § 9 content and the wrong
statement about clearance are new.

**Change:** after the plan is revised, regenerate the summary from it. Name the committed file, give the
§ 9 rows and the thresholds he adopts, and compare the summary with the plan item by item.

---

## Confirmed, and found only by the internal checks

### The batch rule and the rule to write every option push his first answers to the latest point

Second check, serious finding 1. Plan:260-262: "until he answers, the sub-item is written once for each
option, behind pending markers, and its arguments are attacked in each form." Plan:953-955: "A batch is
sent as soon as some step can run no further piece without an answer in it." Under the first rule, a step
that waits on a question always has another piece to run (the next option) until every option has been
written and attacked. So the second rule never fires early. Plan:971-972 says "All seven go in the first
batch, because steps 1 to 4 cannot finish without them and none needs anything written first". That
sentence does not say they are sent at the start, and the batch rule says they are not. Plan:1005 says
the two conversations are "raised with the first batch", so they are delayed the same way. Every option
of every waiting sub-item is written and attacked before he is asked a question that is known to be
needed from the beginning.

**Relation to the outside reviews.** Astra follow-up 2 separately says "Do not write and attack both
versions of an unanswered question unless independent work genuinely requires both". That is the cost
half of this defect, not the timing half. Neon 1.2 and Astra follow-up 1.2 discuss waits, not this
interaction.

**Change:** send the seven certain sheets and the two conversations when step 1 starts. Keep the "no
further piece" trigger for the conditional sheets only. Write more than one option only where independent
work needs it, which is Astra's limit.

### The plan's own question filter refuses two questions the plan says only he may decide

Second check, serious finding 2. Plan:164 says that if a rule is found "that field bounds would prove and
this wording rejects, that case goes to Shane as a question (§ 6.2), because ... no agent may choose".
Plan:177 sends an unmeetable reachability standard to him. But `question-check` refuses a sheet when "the
options differ only in how much the compiler can prove" (plan:948), which describes sheet 14 exactly. It
also refuses "A question about what Precept is or how much it promises" (plan:947), which describes
sheet 15. So if either case comes up, step 6 or step 9 can neither ask nor decide, and cannot finish.

**Relation to the outside reviews.** This is the same pair of sheets that Astra follow-up 2 rules on, but
a different defect. Astra finds sheet 14 "Already answered; no generic escalation" (want:135 and want:104
settle it), and sheet 15 "Goal answered; proof method is engineering", to be brought to him only as "a
concrete ordinary workflow that demonstrably requires an author-visible restriction". Under the order of
authority, Astra's ruling is the better repair. The internal check's proposed fix, an exception that lets
both sheets through, would put back questions the want document already answers.

**Change:** remove sheet 14 and the routing sentence at plan:164, and treat the bank example's sign proof
and cap refusal as settled, as Astra says. Restate sheet 15 as a feasibility case with a worked
workflow. Make that case pass the filter explicitly, so that the plan's text and `question-check` agree.

### The `Fault` row argues against its own deferral

Second check, serious finding 7. Plan:894: "Stays deferred on his words. He said building, and this work
designs, so reading his words as covering the design would widen them. It is the one exception under
conditions 6 and 7." The middle sentence argues that his words cover only building, and then the row
defers the design anyway. Plan:756-757 puts a deferral that is "build order only" in the group "with the
design owed here". Rule 2 requires runtime internals in the design (plan:50). The routes file aims this row
at outcome 1, "canon records it open, with his words" (`inputs/rulings-routes.md`, row 65).

**Relation to the outside reviews.** Astra follow-up 3.4, which checks Neon 3.4, checks the wording of his
words ("defer until we start building the runtime"), and finds no new owner decision is needed. It does
not address the contradiction inside plan:894 or the conflict with § 4.4. This is a separate defect on the
same subject.

**Change:** make the row consistent with § 4.4. Either the design of who constructs a `Fault` is owed in
a named step, and the exception in conditions 6 and 7 (plan:89-90) is dropped, or the plan asks him in one
line whether his deferral covers designing it. It cannot keep both the sentence and the exception.

### Parts of the two frozen documents have no step that may write them

First check, finding 7, and second check, serious finding 8. Step 14 "Does not write in
`soundness-and-coverage.md` or `proof-engine.md`" (plan:604). Yet step 14 is the step that sorts "Every
'deferred', 'post-MVP', 'later' and 'v1' in canon" and answers "Every open question in canon"
(plan:609-611). Step 12 is given the frozen mechanical conflicts and the frozen pairs rows
(plan:570-579), not those deferrals or open questions. Plan:279 says each of steps 1 to 11 fills the
placeholders it owns in `soundness-and-coverage.md` and "no other step writes there".

A count on the current files finds 18 lines with one of the deferral words in `soundness-and-coverage.md`
and 5 in `proof-engine.md`. `proof-engine.md` also has a whole § 13 "Open Questions / Implementation
Notes". Sections such as `soundness-and-coverage.md` § 3.4 "The bucket test", § 6 "Soundness-hole coverage
(Slice-0 fail-open fixes)" and § 7 "Rails" are not obviously owned by any contents item. Condition 6
requires every open question closed, and `status` fails an open row whose owner step has finished
(plan:83-85). Those rows have no owner that is allowed to close them.

**Relation to the outside reviews.** Not raised by them. Astra follow-up 3.3's last row assumes the frozen
proof document "is frozen for step 12", and tells the planner to "verify it is reached". This finding is
that check, and it fails.

**Change:** give step 12 explicitly the deferral sort and the open-question answers inside the frozen
documents, and let it, and the owner step of a step-15 defect, write there after sign-off. Have step 0 map
every section of `soundness-and-coverage.md` to an owner step.

### Every answer he gives becomes a ruling row, and operational answers have no outcome to close with

Second check, serious finding 9. Plan:877-880: `rulings-check` "appends a row to `rulings.jsonl` for
every new row of `answers.md`, including his direction after a notice and his choice on a sheet", and
condition 4 "cannot be met while any row has none of the four outcomes". The four outcomes (plan:866-869)
are a canonical line, a plan rule or `CLAUDE.md` line or handoff row, superseded, or no longer applies.
Outcomes 3 and 4 need his later words (plan:871-875). A clearance of a halt, a "run more rounds", a
settings change or the permission yes fits none of them. Each one would end up on his sign-off list. That
is reading the plan never counts, and it puts condition 4 at risk.

**Relation to the outside reviews.** Not raised. Astra follow-up 2's "Keep consent separate from language
semantics" points the same way, but does not identify this mechanism.

**Change:** add an outcome for an operational answer that is used up by the clearance or run it
authorised, recorded by script with that row. Route only answers that say what Precept does as design
rulings.

### The cap of "fifteen sheets in three batches" conflicts with the batch rule

First check, finding 5, and the batch part of second check, minor finding 10. Plan:1007: "The cap:
fifteen sheets in three batches". Plan:953-958 sends a batch whenever a step is blocked. Conditional sheets
can come up in steps 1, 2, 3, 6, 9 and 10 (plan:241-250). Nothing says which of the two rules wins.

**Change:** cap the sheets only, or say how the batch rule stays within three.

### The count of philosophy edits is three where the plan's own routes give two

Part of second check, minor finding 10. Plan:1088 lists "a 'no' on the clock question, and the two
memory-only rulings if his words are found". The bounds-qualifier ruling (plan:928) leads to a recorded
line, a mechanical edit or a sign-off line, never to a philosophy edit. Only the approximation ruling
(plan:929) "touches the philosophy". The wrong number is repeated in the summary (plan-summary:46).

**Change:** make it at most two.

### Jobs given to "a script" that is not on the closed script list

First check, finding 2 part 1, and second check, minor finding 12. Plan:318-323 lists the scripts, and
"`plan-lint` fails if `scripts/` holds anything else". `step0-guard` refuses when "`scripts/` holds a
file that is not on the list" (plan:304). But the plan gives these jobs to "a script" without naming one:

- the only writer of `clearances.jsonl` (plan:774);
- applying a settings change from a proposal file (plan:784);
- recording dependencies from citations (plan:695-696);
- generating `dedup/pairs.jsonl` (plan:77);
- listing the step 14 commits that touched a newly frozen fact (plan:820);
- comparing list members when a hash changes (plan:851);
- reverting a commit (plan:838-839);
- deciding from the frozen list which step carries a mechanical conflict (plan:220).

**Relation to the outside reviews.** Not raised. If Astra follow-up 2's cut list is adopted, the list is
rewritten and this needs re-checking against the new list.

**Change:** give each job to a named script, or add it to the list.

### Tests 1 to 3 have no fallback that lets step 0 finish

First check, finding 2 part 2, and second check, minor finding 15. Plan:1182-1183: "Every test has a
fallback that lets step 0 finish". For tests 1 to 3 the fallback is "The script is repaired; this is
bounded by `step0-guard`, whose notice reaches Shane" (plan:1187-1189). That is a repair loop that ends
in a halt, not a way to finish.

**Change:** limit the sentence to tests 4 to 11, or say what the halt notice offers him when the repair
does not succeed.

### The guarantee-contract ruling is routed as "Record only"

First check, finding 4. Plan:906: "The guarantee contract | Record only; its fourth part is superseded,
with the later words quoted". "Record only" is none of the four outcomes (plan:866-869). The routes file
aims the row at "2 / 3" (`inputs/rulings-routes.md:34`). But the rulings file says parts 1 to 3 "are in
canon (`precept-language-spec.md:262–276`)" (`inputs/history-rulings.md:154`), which makes it outcome 1,
not outcome 2.

**Change:** give parts 1 to 3 outcome 1 with the spec lines, and part 4 outcome 3 with the later words.

### A paraphrase from memory is treated as his recorded words

Second check, minor finding 16. Plan:915 says "His words are recorded" for "'Constraint' is the wider
word, 'I used the term "rules" more loosely', and all five constraint forms join the proof, each by where
it is anchored", and step 3 builds the rules table "on that footing". The memory file quotes only "I used
the term 'rules' more loosely". The five-forms model is the session's own summary
(`project_boundary_define_what_i_want_2026_07_16.md:175`). The rulings file still marks the row "unclear"
(`inputs/history-rulings.md:165`, row 92).

**Change:** quote only his words as the ruling. Treat the five-forms reading as a use of the ruling, with
its `uses/` width file, before step 3 relies on it.

### The certificate-naming checkers are handed an agent's narrowing of the question

Second check, serious finding 6, the narrower part. Plan:988 cites old:280 and then says "So the two
checkers are given that ruling and the internals rule of § 4.2 as inputs". Old:280 is agent preparation:
"the preparation narrows it, because the coverage job the naming would do is the job you gave the
catalogs". Plan:943-944 requires the checkers to be given the rulings and walk files "and not the
proposer's conclusion". Pointing them at the narrowing and at the internals-default route steers a check
that is meant to be blind. Downgraded from serious to minor: the routing itself (asked after step 10's
write-up if it stays open) matches his words, "would need more context and/or research".

**Relation to the outside reviews.** Astra follow-up 2's sheet 8 ("Engineering unless a public
explanation would differ") rules on the same question. That ruling is why the internal check's wider
fix, putting the question back among the certain sheets, is not adopted.

**Change:** give the checkers only his words and the walk records, and leave the internals rule out of
their inputs.

---

## Rejected

- **"The summary was never written"** (first check, finding 1, and second check, minor finding 17). This
  is no longer true. A separate `write:summary` agent wrote `plan-summary.md` after both checks ran
  (journal line 8), and it is committed. What is still wrong with it is confirmed above. The plan growing
  from 1,002 to 1,302 lines is not a defect in itself, because rule 2 keeps all detail.
- **"The plan decides the reachability standard itself"** (second check, minor finding 11). Under the
  order of authority, want:151 ("Every state is reachable ... A state no path reaches is rejected") ranks
  above spec:191. Astra follow-up 3.1 and § 3: "Where the two disagree, correct canon. It is not an
  unauthorized change to honor the want document." Plan:177 adopting want:151 is correct. Astra's
  separate point that "an input satisfying an exit guard" is not enough is an outside-review finding, not
  this one.
- **"The overflow sheets leave out a third option"** (second check, minor finding 14). The third technique
  in proof-model:358, "A declared range per type that ingress enforces ... sums still need proof", is a
  way of meeting the fixed-range model, not a separate model. It still rejects `Deposit` until a bound is
  declared, so an author sees no different outcome. Astra follow-up 2 also treats sheet 3 as engineering
  first. Nothing here matters for the end goal.
