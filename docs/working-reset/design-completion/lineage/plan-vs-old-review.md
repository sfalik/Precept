# Adversarial review: the new design-completion plan against the old compiler-readiness plan

New plan: `/home/sfalik/source/repos/Precept/docs/working-reset/design-completion/lineage/design-completion-plan-v2.md` (cited as "new" or "v2").
Old plan: `/home/sfalik/source/repos/Precept/docs/working-reset/compiler-readiness-plan.md` (cited as "old" or "plan").

Five reviewers each compared the plans from a different angle: the end goal, whether the work converges, how much it asks of the owner, what was dropped, and whether it can actually run. A sceptic then tried to refute every finding. Out of 79 findings, one was refuted. Several others had their severity lowered or their claims narrowed. Where a finding came up under more than one angle, the summary merges it into one item. Every finding appears in full in its angle's section, using the severity it had after the check.

---

## Summary (five minutes)

### Which plan better reaches your end goal

**The new plan, but it is not ready to adopt as written.** The old plan cannot reach a finished design, and that is a property of how it is built. Its finish line is a green compiler ("Phase 12 | The compiler", old :523). It says "No phase of this plan builds the runtime" (old :442). It never designs the runtime or the tooling. It puts build checks and compiler readings inside its design phases (old :460-463, :1390-1397, :1938-1947). And in several places it narrows proof power because of what the first build would ship: "the first honest fragment … considerably smaller than today's grammar" (old :1645-1652), overflow as "an authorized exemption for the MVP" (old :1599-1600), and phase 8's "narrow" (old :1940). That breaks your rules 1 and 4.

The new plan covers all 36 canonical documents, internals included (v2:66-76). It gives each unfinished document a finish condition (v2:134-147), writes no product code, records your full-proof-power ruling (v2:314), and does not accept "not built yet" as a reason to refuse something (v2:131). It also fixes three things that made phase 2 loop. The proof model no longer waits for reconciliation to finish (v2:172-177). Counts are printed by scripts, never typed by agents (v2:487-491, :577). The scope and the standing of the want document are fixed before work starts (v2:151-162, :270).

The new plan's weak point is its stop rules. It removes the only check over the whole proof model and puts in rules that cannot end, or that fire on almost every run. As written it also cannot get past step 0. It drops several safeguards from the old plan that had worked. And it lets agent judgement, or decisions about the first build, back into the design through a few gaps.

### Surviving findings, ranked

**Blocking**

1. **Step 0 cannot finish.** Test 4 requires one real piece of every kind of work (v2:519). But writing, attacking, merging, carrying the model through and the hand walk all need inputs that only exist after steps 1, 2 or 5 (v2:193-194, :211, :179, :217, :665). Nothing else starts until step 0 passes (v2:19, :497). *Fix:* measure those kinds on stand-ins that already exist, such as the phase-5 write-ups and the bank example from the want document, and label the costs as measured on a stand-in. Take the real measurement on the first real run of each kind, which v2:540 already plans.

**Serious**

2. **No check over the whole proof model, and failures arrive one at a time.** The early checkpoint "puts nothing to you" (v2:265). The argument count goes only to the status file, which you are never sent (v2:577, :676). Each failed argument reaches you separately with three choices (v2:254-255). The old plan's phase-4 stop and its three-attempt rule are gone, and nothing replaces them (old :495-503, :1503-1508). *Fix:* make the checkpoint fire on a rule a script checks, for example two of the five arguments still refuted at round three. Add a rule across all arguments: once N arguments have gone to a second author, send one packet asking whether the model can be written. Send the projected agent runs as a carry-on notice.
3. **An argument can pass because its attackers say they found nothing.** The pass rule is "the last two rounds found nothing" (v2:252). The author closes refutations on its own word (v2:533). The old phase 6 required every attack to name an attempted counterexample and to record what happened to it (old :1801-1807). *Fix:* every attacker file records at least one attempted counterexample. A fresh agent, neither the author nor the attacker, confirms each repair. Define "finding" as one of the kinds of flaw written down in advance (v2:253), so wording notes do not reset the count.
4. **The contents list is closed too early, by one drafter, with no rule for reopening.** It is fixed and hashed at the end of step 1, before any proof text exists (v2:193-194). That reverses your 2026-07-26 ruling that the step vocabulary closes when the procedure is written (old :248, :1491-1495). It also drops the requirement for two drafters who must agree within a third (old :1510-1517) and the rule that one repair breaking another argument is a warning sign (old :1859-1861). *Fix:* two independent drafters. Close the list when step 2 finishes. Record which arguments each argument rests on, and reopen those automatically when one of them changes.
5. **Your rulings are not required to end up in canon.** The rulings table is built (v2:662), and "rulings not yet in canon" is reported once (v2:671), but no finish condition drives it to zero. Walk items 13 and 22 and the 2026-07-26 work list have no route into canon (the-walk.md:352-372, :548; old :291-340). *Fix:* every row of the rulings table names the canonical section that states it, or the reason it no longer applies. A script counts rows that have neither, and step 6 cannot finish until that count is zero.
6. **Phase 2's open findings and its executed resolutions are not carried in by name.** The plan's author did not read the contradiction register or the cold-read findings (v2:686). The register is missing from the adjudicator's order of precedence (v2:245). Disagreements written in prose, such as S7 against N15, who owns schema migration, and N108-N116/N118-N119, are not named anywhere, and no script would find them (old :375, :1166-1180). *Fix:* in step 1, copy every open register item, cold-read Part 3 and the "read before signing" items into the inventory as numbered rows. Rank executed register rows as settled input, below your quoted rulings. Re-run their recorded patterns after each step 3 and step 5 run, and have an agent read each one that fails.
7. **Merging copies has no check that detail survived** (your rule 3). Sonnet agents merge "mechanically" (v2:232, :298). The only check for lost lines covers the `proof-engine.md` rewrite (v2:647). Phase 2 already lost paraphrases and a reason this way (old :365; `phase-2/fault-backstop-review-deletions.md`). *Fix:* a second agent lists every sentence removed from a copy that does not appear in the owning statement. A script counts removed lines with no recorded destination, and any count above zero stops the line.
8. **Nothing catches agents changing canon without your authority.** In the new plan you sign off on counts and one worked example (v2:166). The old trace is what caught the agent-written "and nothing wider" narrowing across about ten sentences (old :137-149; trace-walk-rulings.md:42-49). *Fix:* at the proof-model sign-off and at the finish, a fresh agent sorts the changes to canon into the old trace's four groups. You see only the changes that trace to nothing you wrote or ruled, capped and with citations.
9. **Open questions can close by a default held in a side list, or by "it waits".** Defaults are kept "in one list" (v2:166, :280). Nothing requires the answer to be written into the owning section, or the 21-row index at runtime/README.md:34-54 to be emptied. The `Fault` question rests on your "defer until we start building the runtime" (v2:241; old :437-439). Under design-first, that trigger now comes after the design is finished. *Fix:* a default counts as done only once it is in the owning section and its index row is gone, both checked by script. List every row that waits as a known gap. Add one line to batch 1 telling you that your new goal bears on the `Fault` deferral.
10. **The deferral sort lets first-build scope back in.** Its second group is "ruled out of the first version by you" (v2:133), which does not say whether the design is still owed. It has no group for exclusions argued from a principle (proof-engine.md:2537-2541; graph-analyzer-roadmap.md:9) or for capabilities you deliberately left open. Schema evolution (runtime-api.md:906) is never named. *Fix:* four groups instead of three: designed here; excluded on a named principle; a future capability you left open, with your words quoted; and build order only, with the design owed here. Drop "first version" as a reason.
11. **Refusals can cite the admissibility tests on cost and "earns its place" without any bound.** v2:131 lets a refusal cite spec:225. Canon already uses test 4 to refuse a capability (proof-engine.md:2541). The plan never mentions performance or complexity. *Fix:* each proof step carries a written cost bound, attacked like the rest of its argument. A refusal on test 2 needs that bound. A refusal on test 4 needs your word.
12. **Walk item 13 (spellings and normalisation) is not in the contents list.** Your ruling was about the first build, and it named how the language would widen later (the-walk.md:365-372). Under full proof power the design owes either the set of rewrites or a principled stop. The plan never mentions it. *Fix:* add "expression normalisation and constant folding: the intended rewrite set, each with its argument" to 2.1 item 3, and let the fresh check decide whether it is a question for you.
13. **Question packets could get their accept and refuse verdicts from the compiler.** The `precept-author` agent drafts each option's example and what it refuses (v2:664), and that agent's own instructions are to compile on every revision (.claude/agents/precept-author.md:38). *Fix:* two agents derive the accept and refuse column from canon. The MCP tools are used only to check syntax.
14. **The hand walk leaves out loading a definition.** That is where the certificate gate runs (runtime-api.md:250; want:197-210). *Fix:* add a seventh operation, load, with a valid, a stale and a tampered certificate.
15. **The count of what reaches you is asserted rather than derived.** "Up to four" later questions (v2:37, :447) sits beside at least six routes to the question gate and about 70 open-question lines (v2:80, :132, :159, :233, :245, :443). The old caps on your time are gone and nothing replaces them. *Fix:* at the end of step 1, count the rows that could reach you. Cap each batch. When the count goes over, re-run the filter, not your reading. Say where a fifth question goes.
16. **Question 1 does not list everything it changes.** It leaves out your phase-2 register sign-off (old :180), the five ordering choices that "still await your ruling" (v2:790, :818), the two additions (v2:813-816), the six-slicings design content (old :1653-1684: the per-type algebraic table, the re-homed validity arguments, the route through the execution order), and the `docs/Working/` archive trigger. The pilot merges also edit canon before Question 1 is answered (v2:792). *Fix:* one row for each old ruling, marked kept, replaced or moot. Send a clean plan page without the layered change list.
17. **Question 2 states option 2's cost in a way your own text contradicts, and leaves out a known cost.** v2:363 calls it "held by a check, not a proof". want:143 calls the same mechanism a premise of the proof. The computed-field timing problem (old :568-571) and the "partial sweep" warning (old :583) are missing. The question itself is legitimately open (trace-walk-rulings.md:42-46). *Fix:* describe option 2 in want:143's own terms, and add the timing problem as a stated cost for the fresh check to confirm or refute.
18. **The rules that say counts must fall misfire.** Discovery work in steps 1 and 3 raises open counts by design, and "outside canon more than inside" fails on every run of steps 1 and 4 (v2:267, :568). No table of next actions exists, and the session is not allowed to make one up (v2:467). *Fix:* report "found, not yet worked" separately from "worked, then reopened", and require only the second to fall. Bring back the old "a different problem each attempt" test as a script (old :495-503). Write a fixed table of next actions.
19. **Step 1's finish condition is a judgement with no fixed answer.** Two derivations of the bank example must "agree" under canon that does not yet state establishment or preservation (v2:194, :84-91). *Fix:* the deliverable becomes the list of places where canon is silent. Checking results against want:112-127 moves to step 4 or 6.
20. **Parked work has no record and no owner.** "Parked unless a completeness list needs it" (v2:269) replaces the old rule that no deferral is left without an owner (old :397-458). The same gap leaves the build-stage items from old phases 9-12 without a home. *Fix:* a parked-work file and a "handed to the build" file that the scripts read. Step 6 cannot finish while any row lacks an owner, a quoted ruling, or a named exception.
21. **Step 0 is tool-building with no limit.** Ten families of counting scripts, one workflow per kind of work, six kinds of test, no list of pieces, and "cannot be met" undefined (v2:499-521, :569). The precedent is the 2,657-line `enumeration.workflow.js`, which never ran for real (old :1444). *Fix:* a fixed list of scripts with one test each. Build only what step 1 needs. Build later steps' scripts as the first piece of those steps.
22. **Two sessions commit to one working tree.** The lock file is per line (v2:544, :596). The two lines also share the log, the status file, the notices file and the defaults list (v2:282, :575). *Fix:* a worktree per line, or commits limited to named paths. A rule at startup that quarantines half-written files automatically.
23. **The session that starts a step still makes judgement calls over many runs.** It picks the next run from prose and has to name next actions (v2:467, :267). *Fix:* a script prints the next run. Cap how many runs one session chains, say five.
24. **The short-window usage limit is not handled.** Only weekly caps are (v2:550-558). The rolling limit (reference_model_usage_limits.md:22) would turn into frequent manual restarts. *Fix:* the session waits out a short-window reset by itself. Step 0 observes how each kind of limit actually shows up.
25. **When the Fable share runs out, attack stalls too, and extra usage could spend credits silently.** Repairs are Fable work and sit inside every attack run (v2:533, :294), which contradicts v2:555. *Fix:* say so in 5.5. Check the extra-usage setting in step 0, and make the scripts stop on their own.
26. **The author's context window inside a Fable workflow agent is untested.** The reading list is 210-285k tokens (v2:288), and the plan admits nobody checked (v2:286, :690). *Fix:* a step-0 test in which a Fable-pinned agent loads the list, reports its model id, and writes a short section.

Items 2 and 15 overlap. Item 20 also covers the old phase 9-12 handoff finding, which is minor taken alone.

**Minor**, one line each (full text below):
- The period-ordering design is owed only to the type documents (v2:144). Its proof, catalog and diagnostic parts are not (period-ordering-design.md:429-501).
- "Full" status is a label nothing sets.
- The proposed CLAUDE.md catalog-notice edit must not lift the suspension at the end of the design.
- The argument count, the implied agent runs and the weekly resets are never sent to you.
- No handoff to acceptance criteria is defined.
- Tooling documents are checked only for leftover markers.
- Proof text goes into canon before its attack rounds (v2:200 against :278).
- No mechanical counterexample search survives. Whether one would count as code settling design is your call.
- The "last two rounds found nothing" rule uses the word "finding" without defining it.
- The plan's change list governs itself in layers.
- Q4 (dead end) and Q8 (builder metadata) should be resolved without you.
- The cost of restarts is listed but has no bound.
- Several approvals are missing from the totals.
- Nothing says where your answers are written down, verbatim.
- The question filter has no measured error rate.
- The want:75 file-edit point is unclear.
- Language lists are copied by agents and not re-checked after canon edits.
- N105's classification is stale.
- Known counterexamples are not handed to attackers.
- No cost check on strategies.
- The decision index is still unmarked.
- No fact-checker in the attack rounds.
- No scale of agent runs is given before Question 1 (roughly 1,000 or more Opus runs for attack alone).
- Question 3 can block the checkpoint.
- The token-ceiling mechanism is wrong.
- A saved workflow cannot be the entry point.
- Push notices are available now, and the stale-lock rule has no way to detect a live run.
- Second authors run on the same model as the attackers.
- Untested permission prompts in unattended runs.
- Question 1 credits you with a loop diagnosis the assistant made (v2:345). Quote your words instead.

### What the old plan did better, which the new plan should take back

- A stop over the whole proof model that reaches you with a count (phase 4, old :1503-1508), and the "different problem each attempt" convergence test (old :495-503), this time applied by script.
- Concrete attempted counterexamples and a recorded disposition for every attack (old :1801-1807).
- Two drafters for the step list, the vocabulary closing only when the procedure is written, and cascading repairs treated as a warning sign (old :248, :1510-1517, :1859-1861).
- No deferral without an owner (old :397-458). Rulings tracked with their owners (old :221, :291-340).
- Phase 2's register as settled input, with its open findings listed by citation (old :517, :1044-1080, :1166-1180).
- The trace of what justified each change to canon, and a review of what was deleted (old :137-149, :365).
- Caps on your time, a random sample of filtered items to measure the filter's error rate, and recording your marks in the same sitting (old :816-823, :842-864, :862).
- Specific content: the per-type algebraic table, the validity arguments re-homed as derivation rules, the route through the execution order (old :1653-1684), the known counterexamples (old :1679-1684), the strategy cost model (old :1723-1731), and period-ordering Decision 6 placed in the proof model (old :1610-1616).

### What this review did not cover

- None of the reviewers checked whether the new plan's 36-document scope list is itself complete against `docs/`. The finding that tooling is checked only for leftover markers is the only coverage finding there.
- No reviewer checked the five "checkpoint" arguments' choice, or the twelve-round figure, against the full record. They checked only the memory file.
- The runtime and tooling design questions themselves (the 21 runtime index rows, the extension stub) were not assessed for difficulty. Only their routing was.
- The cost estimates rest on per-agent token figures from two plan-review workflows (wf_3af62bee, wf_6a3126c0), not from task agents, and the weekly cap is not recorded anywhere.
- The reviewers worked on the plan text. No step of either plan was run.
- I spot-checked a handful of citations (v2:133, :265, :519; old :442, :523). The rest rely on the reviewers' and the sceptic's reading.

---

## Angle 1: the end goal

**Overall.** The new plan is far closer to the end goal. Before it is adopted, fix the deferral sort, the admissibility loophole, the defaults held in a side list, the routing of rulings and phase-2 findings into canon, the missing normalisation item, the missing load walk and the merge detail check.

**Surviving findings**

- **[Blocking] The old plan does not end in a finished design.** It stops at the compiler (old :442, :523). The runtime and tooling design have no owning phase (old :162-168; runtime/README.md:34-54). Build checks sit inside its design phases (old :460-463, :1390-1397), and phase 8's code inventory feeds the decision to carry on, narrow or revert (old :1919-1923, :1938-1947). The new plan scopes all 36 documents and gives each unfinished one a finish condition (v2:66-76, :134-147). This is the main reason to prefer the new plan.
- **[Blocking] The old plan narrows proof power because of what the first build ships, and the new plan takes the full-power ruling.** Old :1638-1652, :1595-1600, :1940. New :131, :143, :276, :314, :422. One nuance: old :426-428 calls overflow "in scope", but no phase designs it. Keep the new provisions as written.
- **[Serious] The deferral sort lets first-build scope back in.** v2:133's "ruled out of the first version" says nothing about whether the design is still owed. There is no group for exclusions argued from a principle (proof-engine.md:2537-2541; graph-analyzer-roadmap.md:9). Schema evolution (runtime-api.md:906) would fall into the "design owed" group, and nobody has decided that. Correction from the sceptic: sagas and messaging appear only in the want document (want:191-195, :220), not in canon. Fix: the four groups listed in the summary, item 10. Name schema evolution and cross-precept proofs.
- **[Serious] The admissibility tests on cost and "earns its place" give agents a way to refuse.** v2:131, :314; spec:225; proof-engine.md:2541; soundness-and-coverage.md:271/:281. Overstated in one respect: a cost bound can be argued on paper. Fix: a written cost bound per proof step. A refusal on test 4 needs your word.
- **[Serious] Nothing requires your rulings to end up in canon.** v2:116-160, :662, :671, :688. Partly mitigated: the proof-model contents list names walk items 1, 3, 5, 7, 8, 9, 18, 32 and 35 (v2:119-130). Fix: every row of the rulings table names its canonical section, and a script count must reach zero.
- **[Serious] Phase 2's open findings are not carried in by name.** Some are covered by 1.3 or by `declared-types.py` (v2:97-102). Not covered: S7/N15 (old :1166-1169), who owns schema migration (old :375), evaluator.md:1933-1936 (old :1170), PRE0113-0116, DiagnosticCategory, and N108-N116/N118-N119. Fix: copy all of them into the step 1 inventory as numbered rows.
- **[Serious] An open question can close by a default held in a side list, or by "it waits".** v2:132, :166, :207, :241, :280-282. Fix: summary item 9.
- **[Serious] Walk item 13 is not resolved.** the-walk.md:352-372. The same item carries the soundness requirement that folding runs through the one evaluator, and the integer-division defect (the-walk.md:356-360). Fix: summary item 12.
- **[Serious] Question packets could take verdicts from the compiler.** v2:308, :521, :664; precept-author.md:38, :170. Fix: summary item 13. Drop the MCP requirement from test 6 unless a design step needs it.
- **[Serious] Merging copies has no check that detail survived.** v2:207, :232, :298, :535, :647. Fix: summary item 7.
- **[Serious] The hand walk leaves out loading a definition.** v2:125, :158; runtime-api.md:250; want:197-210. The typed-lane and JSON boundary (runtime-api.md:609-635) are not walked either. Fix: summary item 14.
- **[Minor] Period ordering is owed only to the type documents.** v2:144 against period-ordering-design.md:429, :476, :487, :501; old :1610-1623. Fix: state every decision in its owning document (proof-engine.md, catalog-system.md, diagnostic-system.md).
- **[Minor] "Full" status is a label.** v2:149. Fix: a script writes the maturity row from the check results.
- **[Minor] The CLAUDE.md catalog-notice edit.** v2:218 against CLAUDE.md:103. Fix: the edit says the design now specifies the full catalog contents, and the suspension stays until the build enforces it.
- **[Minor] The argument count goes to a status file you are never sent.** v2:577, :676; old :1502-1534. Fix: put the count, the minimum agent runs and the implied weekly resets in the step 1 notice, for information only.
- **[Minor] No handoff to acceptance criteria.** v2:43, :228. Fix: a short closing section listing the inputs to the next stage, and saying that the first-build cut is its first decision.
- **[Minor] Tooling documents are checked only for leftover markers.** v2:140, :158. Fix: follow the bank example's certificate and one refusal through hover, `precept_proofs` and inspect.

**Refuted:** none.

---

## Angle 2: convergence

**Overall.** The new plan removes three of the mechanisms behind the loop. It does not fix the stop rule that never fires: it removes the only stop over the whole model. It also adds new ways to loop: tool-building in step 0, notice rules that misfire, a starting session that still exercises judgement, and plan text that contradicts itself in layers.

**Surviving findings**

- **[Serious, down from blocking] The early checkpoint stops nothing.** v2:265, :341, :577, :676; old :1504-1508, :1853-1857, :495-500. Lowered because the old stop was itself weak (the attempts column still reads 1, old :194) and because failures do reach you through second-author failures (v2:254-255, :567). Fix: summary item 2.
- **[Serious] Attackers can pass an argument by saying they found nothing.** v2:252, :489, :533; old :1801-1807; feedback_soundness_arguments_need_adversarial_rounds.md. Fix: summary item 3.
- **[Minor, down from serious] "The last two rounds found nothing" repeats the "survive another full review" loop.** The sceptic showed the rule is bounded (12 rounds, then up to 6 more, then you; v2:252-255). What remains is that "finding" is undefined (v2:254) and that arguments are written by section but attacked by argument (v2:200, :251). Fix: define a finding as one of the kinds listed in advance, and scope each argument to one claim.
- **[Serious] The contents list has one drafter, a hash with no consequence, and no rule for reopening or cascades.** v2:193, :251, :259, :338; old :486-493, :1513-1515, :1425-1427, :1859-1861. Fix: summary item 4. Also correct or enforce the "Kept" row in Question 1.
- **[Serious] Parked work is never closed.** v2:269, :223; old :403-422; old :1003-1004. Mitigation: step 3 would probably catch a fault-backstop-style disagreement (v2:207). Fix: summary item 20.
- **[Serious] Step 1's agreement criterion has no fixed answer.** v2:84-91, :194, :265, :665; old :1118-1123. Fix: summary item 19.
- **[Serious] The wrong-way count rules fire by design.** v2:267, :282, :467, :568. Fix: summary item 18.
- **[Serious] The starting session is still a main loop.** v2:267, :467, :690; old :1020-1025. Fix: summary item 23.
- **[Minor, down from serious] The plan contradicts itself through its change list.** The supersession rules are stated explicitly (v2:767, :772), and "one page" may mean one document. What remains is that the five ordering choices are uncounted (v2:790, :818, :45-58, :632-645). Fix: rewrite the plan once as clean current text, and count or default the five choices.
- **[Serious] Step 0 is tool-building with no limit.** v2:497, :499-521, :569; old :460-463, :1444. Fix: summary item 21.
- **[Serious] Two sessions, one checkout.** v2:487, :521, :544, :596. Fix: summary item 22. Add a concurrent commit to test 6.
- **[Minor] Proof text goes into canon before its attack rounds.** v2:200 against :278, with :179 and :233. Fix: a fixed pending marker that step 3 may not cite, or keep the text outside canon until it passes.
- **[Minor] No mechanical check survives.** Old :1807-1814 against v2:126, :162. The old search tested compiler code, so restoring it means code. Fix: ask you whether a standalone counterexample search counts as code settling design.
- **[Minor] Question 1 credits you with the loop diagnosis.** v2:345 against five-items-walk-2026-08-27.md:13. Fix: quote your words.
- **[Minor, down from serious; credits the new plan] The proof model no longer waits behind reconciliation.** v2:172-177, :614-622 against old :195-198. It still waits behind step 1 (v2:193-194), so this is reduced, not removed.
- **[Serious; credits the new plan] Counts come from scripts, and a failed agent counts as work not done.** v2:489-490, :516, :577 against old :28-43. Caveat: the next-action text in notices is still written by an agent (v2:568).
- **[Serious; credits the new plan] Scope and the want document's standing are fixed up front.** v2:151-162, :245, :270 against old :1000-1025. Fix: Part 2's finish conditions may change only on your quoted word, logged.

**Refuted:** none.

---

## Angle 3: what it asks of you

**Overall.** The new plan asks less of you and correctly removes five old questions. But its promise to reach you only when needed is asserted, not derived. Restarts and approvals go uncounted. It still produces two questions you do not need to answer. Its Question 2 framing works against your own text. And it drops the trace and register sign-off.

**Surviving findings**

- **[Serious] The count of what reaches you is asserted, and the time caps were removed.** v2:37, :80, :132, :159, :233, :245, :280, :443, :447, :645. Overstated in two respects: most of the 126 deferral lines need no ruling from you (v2:133), and the old caps covered only the phase-1 sitting (old :842-864). Fix: summary item 15.
- **[Minor, down from serious] Q4 (dead end) should not reach you.** want:126, :152; philosophy.md:44; graph-analyzer.md:451-457; v2:233, :245. Lowered because the fresh check (v2:308) could drop it. One narrow point is legitimate for you: two non-terminal states that loop with no route to completion. Fix: resolve Q4 under 3.3, and send that one narrow point only if the fresh check finds it.
- **[Minor, down from serious] Q8 (builder metadata) is an agent-written label turned into a question.** catalog-system.md:1509; precept-builder.md:893-897; the label was introduced in commit 004eede1 (Co-authored-by: Copilot); v2:239, :440. Fix: make it a default under 3.4.
- **[Serious] Q2's framing.** Citation corrections from the sceptic: the sentence calling the ingress check load-bearing is at want:129, not :110, and want:155 is about dead rows. The question is legitimately open (trace-walk-rulings.md:42-46; old :282). What remains: the cost stated at v2:363 contradicts want:143 and is at best arguable against philosophy.md:42. Fix: summary item 17.
- **[Serious] Sign-offs shrink to counts, and the trace is dropped.** v2:166, :233, :245; old :370, :1276, :1421-1423; trace-walk-rulings.md:42-49. Caveat: the trace was a one-off pass looking back, not a standing mechanism. Fix: summary item 8.
- **[Serious] Question 1 does not list everything.** v2:27, :106, :330-343, :767-818, :792; old :180. Fix: summary item 16.
- **[Minor, down from serious] Restarts and checking for notices.** Restarts are listed but not bounded (v2:643). v2:630 promises an estimate of resets. Push delivery is assigned to step 0 (v2:575). Fix: the expected restarts per line in the first packet, push delivery as a pass condition of step 0, and one start line that resumes every paused line.
- **[Serious] Failed arguments reach you one at a time.** v2:249, :254-255, :265, :567-568; old :495-503, :546-548. Fix: summary item 2.
- **[Minor] Approvals missing from the totals.** CLAUDE.md edits (v2:149, :218), design-review sign-offs (v2:255, :365, :443), defaults a caller can observe (v2:239), and philosophy edits (v2:363, :414). Fix: add rows to Part 6, and keep one file of requests for your word, as old :354-355 did.
- **[Minor] Nothing says where your answers are recorded.** v2:488, :566, :605; old :697-698, :862. Fix: a single answers file, written verbatim, with a different agent checking that each recorded ruling covers only what you said.
- **[Minor] The question filter has no measured error rate.** v2:308, :663, :754; old :816-823. Fix: two checkers per question, or the old random sample.
- **[Minor; credits the new plan] The proof-power answer removes five old questions, and the requirements for preparing a question are kept.** v2:314-317, :664; old :278-289, :342-346.
- **[Minor] The `Fault` deferral under the new goal.** Part (a), the want:75 correction, mostly does not stand: it is shown to you and follows from walk item 18. Only the question of whether the want document file itself is edited remains (old :1314-1316). Part (b) stands: v2:241. Fix: one line in batch 1.

**Refuted:**
- *Q5 offers an option that overrides approved philosophy on equal terms.* Refuted. v2:415 labels the option "This overrides locked text … which only you can authorise", and v2:414 raises the philosophy tension as a gap for your direction. That is exactly what Tier 3 requires.

---

## Angle 4: what was dropped

**Overall.** The new plan is better structurally, but it throws away or weakens real assets from the old one. It should be adopted only after Question 1 lists every old ruling, the register is treated as settled input that gets re-checked, a file handing work to the build exists, and the inherited findings are listed with their routes.

**Surviving findings**

- **[Serious] Phase 2's 133 executed resolutions are neither treated as settled nor re-checked.** v2:106, :116, :245, :270, :662, :686; old :517, :972-994. Caveat: two of the old patterns broke under correct later rewording (old :978-994), so each failing pattern has to be read by an agent, not just counted. Fix: summary item 6.
- **[Serious] Q2 is missing the computed-field timing problem.** The walk-item-16 half of the finding was refuted: item 16 calls want:143's check "a premise you name", and it concerned event writes (the-walk.md:430-447). What remains: old :568-571 and :583 are missing from v2:363. Fix: summary item 17.
- **[Minor, down from serious] Walk item 13 is dropped.** The rulings table and the "not yet in canon" count would pick it up (v2:662, :671). The gap is that the contents list does not name it, and the tension with v2:131 is not reconciled. Fix: summary item 12.
- **[Serious] The contents list closes before the proof model is written.** v2:118, :122, :193-194, :259, :338, :628; old :248, :1491-1517. Fix: summary item 4.
- **[Serious] Question 1 under-reports what changes.** Old :246-251, :1653-1684. The new plan has no "algebra" or "execution order". Fix: summary item 16, and add the six-slicings content to the contents list.
- **[Minor, down from serious] Later-phase work has no home.** Some items are held elsewhere: bugs.md BUG-033 at :866 and BUG-068 at :85, and diagnostic-system.md:193. The test disposition, the orphaned `# EXPECT:` test and the false-green rule live only in old :2068-2092. Fix: a "handed to the build" record (summary item 20).
- **[Minor, down from serious] Language lists are copied by agents.** A reasonable choice when the design comes first, but nothing re-checks the lists after canon edits (v2:193, :224, :661). Fix: re-extract or re-check after steps 2, 3 and 5.
- **[Serious] Merging copies has no detail check.** Same as angle 1. Fix: summary item 7.
- **[Serious] No check that a change to canon traces to you.** v2:166, :200, :247-255, :274; old :137-149. Fix: summary item 8.
- **[Minor, down from serious] Inherited decisions with no want line behind them.** N80 and N100 are "Waiting on Shane" (contradiction-register.md:428, :564). The "dispositioned with a reason" wording at v2:106 is misleading. Most of the rest are covered by some route (v2:97, :124, :133, :239). Fix: an explicit carry-over table, with the question budget sized from it.
- **[Minor] N105's classification is stale.** contradiction-register.md:529 against :707; v2:106, :179, :218. Fix: treat it as forced and merge it in step 3, and correct the register's tally line.
- **[Minor] Known counterexamples are not handed to attackers.** Old :1679-1684, :1736-1739, :568-571; the-walk.md item 14; v2:253. Fix: seed every attacker brief with them.
- **[Minor] The admissibility cost check is dropped.** spec:225; v2:122; old :1723-1731, :2244-2248. Fix: a cost model per strategy as part of each strategy argument.
- **[Minor] Period-ordering Decision 6 and the qualifier-on-binding rule.** v2:121, :144; old :1610-1616; period-ordering-design.md:3, :111. Fix: add them to the contents list.
- **[Minor] The decision index is still unmarked.** docs/Working/decision-index.md:1-5; old :222. Fix: mark it in step 0, and name it as off limits for the question checkers.
- **[Minor; credits the new plan] What the new plan keeps well.** v2:155, :241, :288, :310-447, :490, :577 against old :28-34, :1281-1290. Caveat: it does not retire the old plan as openly as it claims (see Question 1).

**Refuted (in part):** the walk-item-16 half of the Q2 finding. Item 16 supports want:143's premise, not the rejected sweep (the-walk.md:430-447).

---

## Angle 5: can it run

**Overall.** The new plan has a running mechanism and the old one had none. But it cannot get past step 0, and several of its untested assumptions (permissions, usage limits, the Fable share, a shared working tree, the author's context window) would stop the work or keep pulling you back in.

**Surviving findings**

- **[Blocking] Step 0's test 4 depends on later steps.** v2:19, :188, :193-194, :200, :211, :179, :205, :217, :475-483, :497, :519, :665-667. Fix: summary item 1.
- **[Minor, down from serious] Permission prompts in unattended runs.** The project's .claude/settings.local.json allows only three commands. Auto mode names a different repository as trusted. Test 6 (v2:521) partly covers this. Fix: test less common operations, record the permission rules that made unattended running possible, and list "stopped at a prompt" among the causes of a silent stop at v2:579.
- **[Serious] The short-window usage limit.** v2:550-558, :570, :603; reference_model_usage_limits.md:22. Fix: summary item 24.
- **[Serious] The Fable share and extra usage.** v2:294, :478, :533, :555; reference_model_usage_limits.md:15. Fix: summary item 25.
- **[Minor, down from serious] No estimate of agent runs.** The inputs check out (wf_3af62bee: 278,905 tokens over 3 agents; wf_6a3126c0: 1,416,705 over 11; enumeration.workflow.js:286: $1,020 and 21M tokens). But the plan does give the formula (v2:628, :630), and the 2,500-4,000 total rests on unverified multipliers. Fix: before Question 1, one paragraph saying attack alone is about 1,000 or more Opus runs and probably several weekly resets.
- **[Minor] Twelve rounds, and no fact-checker.** v2:249-254, :295; feedback_soundness_arguments_need_adversarial_rounds.md:13, :18. Fix: one fact-checker against canon in each round, and record the last round in which each argument had a new finding.
- **[Serious] Two sessions in one tree.** This is worse than first stated, because the log, status, notices and defaults files are shared (v2:282, :575). Fix: summary item 22.
- **[Serious] The count-must-fall rule fires on discovery work.** v2:254, :267, :568; old :495-503. Fix: summary item 18.
- **[Minor] The session picks the next run by judgement.** v2:467, :490. Fix: a script prints the next run.
- **[Serious] The Fable author's context window is untested.** v2:286, :288, :521, :534, :690; reference_workflow_agent_models.md:13. Fix: summary item 26.
- **[Minor] Q3 can block the checkpoint.** v2:199, :211, :265, :385-388, :794. Fix: attack the creation argument in a form with no state actions.
- **[Minor] The token-ceiling mechanism.** v2:540. `budget.total` comes only from the turn's directive and counts output tokens only. Fix: pass the ceiling in args and compare it against `budget.spent()`, calibrated on output tokens.
- **[Minor] A saved workflow cannot be the entry point.** v2:467, :487, :594; enumeration.workflow.js:195-197. Fix: a skill or command that wraps the workflow.
- **[Minor] Push notices and stale locks.** v2:575, :579. PushNotification is available, and agentPushNotifEnabled is true. Fix: push every notice. Write a heartbeat into the lock file, passed through args or written by the session.
- **[Minor] Second authors share a model with the attackers.** v2:295-296. Fix: run some attackers on Fable for those rounds, or say plainly that agreement there counts for less.
- **[Minor; credits the new plan] The running mechanism.** v2:487-490, :516-518 against old :491-493, :502-503. Caveats: "committed means done" is undercut by the shared working tree, and the old convergence test was dropped outside the per-argument rule.

**Refuted:** none.
