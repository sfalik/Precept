# Recurring failures in Precept's design and planning, 2026-05 to 2026-11-13

Written 2026-11-22 against branch `spike/Precept-V2-Radical-reset` at `2f126a3b` (2026-08-28, the last
commit). Nothing in the repository was edited.

## How to read this

Each entry says what happened, when, how often it came back, the evidence, and what was tried against
it before and whether that worked. Citations are `file:line` or a commit hash.

Short names used for the sources:

- **where-are-we** = `docs/Working/where-are-we-2026-07-25.md`
- **where-going** = `docs/Working/where-we-are-going-2026-07-25.md`
- **frank** = `docs/Working/frank-retrospective-proof-engine-arc-2026-07-13.md`
- **plan** = `docs/working-reset/compiler-readiness-plan.md`
- **want** = `docs/Working/what-i-want-2026-07-16.md`
- **mem/NAME** = `/home/sfalik/.claude/projects/-home-sfalik-source-repos-Precept/memory/NAME.md`
- **restart-1** = the code-first restart report,
  `docs/working-reset/design-completion/lineage/restart-analysis-rejected.md`
- **restart-2** = the design-first report that shrank the design,
  `docs/working-reset/design-completion/lineage/design-completion-analysis-rejected.md`
- **v2-review** = the adversarial review of the third restart plan,
  `/home/sfalik/source/repos/Precept/docs/working-reset/design-completion/lineage/plan-vs-old-review.md`

**What I checked myself and what I took on trust.** I confirmed the dates and subjects of every commit
cited below with `git log -1`, the monthly commit counts, and the size of phase 2's change to canon. Most
counts (for example "five of five claimed owner decisions were already answered") come from the 2026-07-25
report, the plan or the memory files; I quote them as those documents state them and did not re-derive
them. Where a source is itself agent-written, I say so when it matters.

**Commits per month** (`git log --since/--until`, my count): May 1,070 total and 343 touching `src/`;
June 59 and 11; July 170 and 3; August 18 and 0. The last feature commit is `210229ff`, 2026-06-05
("fold global rules against defaults (BUG-027)").

---

## Summary table

| # | Failure | First seen | Times recorded | Did a fix hold? |
|---|---|---|---|---|
| 1 | The unfinished part of the design was never the part being worked on | 2026-06 | every planning cycle, through 11-13 | No |
| 2 | Plans about the work replaced the work, and each new plan was a new copy | 2026-06-11 | seven plans or plan-like documents, plus three at the restart | No |
| 3 | Finish lines that were searches with no end | 2026-06-16 | at least four efforts | Partly |
| 4 | Decided but never carried into canon, then re-derived | 2026-06 | five named cases on 07-25 alone; at least four in June | Partly |
| 5 | Agents manufactured owner decisions out of settled or out-of-scope matters | 2026-05-26 | 4 of 4, 5 of 5, 4 of 5 out of scope, and again at the restart | No |
| 6 | A ruling was widened past what he said | 2026-07-24 | at least four, one at the restart | No |
| 7 | Agent-written labels treated as his authority | 2026-06-23 | chronic; 92 of 96 July commits agent co-authored | Partly |
| 8 | The design was narrowed, or its bar lowered, to make something finishable | 2026-06-01 | six or more, including both rejected restart reports | No |
| 9 | Build state, compiler behaviour, samples or catalogs used as evidence about the design | 2026-05-26 | chronic; again at the restart | No |
| 10 | Claims about canon made without reading it, or by searching only the current tree | 2026-05-28 | at least nine named instances | Partly |
| 11 | Soundness arguments and designs called finished before anyone attacked them | 2026-06-01 | three episodes, eight arguments or designs | Yes where applied |
| 12 | Stop rules that were written down and never fired | 2026-07-13 | every stop rule the reset plan carries | No |
| 13 | Findings recorded, then work carried on as if they had not been | 2026-07-16 | at least four | No |
| 14 | Tracking pages and duplicated facts went stale | 2026-06-20 | the tracker alone six times | No |
| 15 | Decisions and work held where the next session cannot see them | 2026-06 | at least five, including the restart reports | Partly |
| 16 | Agent vocabulary and compressed questions, which produced wrong answers | 2026-07-01 | five strikes in chat, plus measured inversions | No |
| 17 | The main session acted as the authority | 2026-05 | about five times a session, for weeks | No |
| 18 | Enumeration covered the cases tripped over, not the whole family | 2026-06-01 | at least five | Overcorrected |
| 19 | An engineering wall was read as a question about what Precept is | 2026-06-05 | one month-long episode | Not tested |
| 20 | Records about the work grew faster than the work | 2026-07-14 | phase 2 at about twenty to one | No |
| 21 | Agent runs reported results nobody observed | 2026-05-29 | at least four kinds | Partly |
| 22 | Each correction swung to its opposite | 2026-06 | at least five swings | No |

---

## 1. The unfinished part of the design was never the part being worked on

**What happened.** What the want document asks for that canon does not contain is the proof model:
which obligations a definition owes and where, what a proof may rely on, how an obligation closes or is
refused, what a certificate holds, and the argument that the induction holds over this language. That
part stayed unwritten while effort went into reconciliation, provenance, process and records.

**When, and how often.** Every cycle from June to the restart.

**Evidence.**
- `docs/compiler/soundness-and-coverage.md` declared itself "Design (skeleton)" on 2026-07-25 while the
  architecture document delegated "the precise, measurable boundary of what is and isn't a proof
  obligation" to it (where-are-we:35).
- "Nobody has argued the induction itself … no argument for it exists anywhere" (where-going:187–191),
  carried unchanged into the reset plan (plan:597–602).
- The sentence used to justify removing the runtime's post-mutation check, "every handler was proven at
  compile time to preserve every rule", rests on an obligation kind that does not exist
  (mem/project_reset_phase_1_2_2026_07_28:267–275). Establishment and preservation are absent from the
  thirteen requirement kinds (where-are-we:123).
- The `CertificateSteps` catalog that `precept-language-spec.md:225` makes a condition of a strategy being
  admissible does not exist; twenty-one step kinds were designed and none built (where-are-we:121;
  plan:594–596).
- The reset plan put this work at phases 4 to 7, behind a phase that reconciles every canonical document
  and a phase that numbers every normative statement — between 546 and 2,205 of them, "likely much nearer
  the top" (plan:1460–1466). Phase 2 ran a month and did not touch the proof model (restart-2:11, :71–75).
- Meanwhile phase 2 took on a full new design for ordering periods, a philosophy rewrite with three
  reviews, a nineteen-row fault model and a trace (restart-2:114).

**What was tried.** The establishment and preservation design (three passes, 2026-07-24 and 25, about
fifty agents, nothing written into either document — where-are-we:9). The obligation-discharge matrix
(two of four slices completed — where-are-we:9). The reset reshaped the problem as a walk over the
language's expression forms (where-going:42–45), which was a real improvement in shape, but it scheduled
the work behind phases 2 and 3. **Did not work**: no version of the proof model reached canon.

## 2. Plans about the work replaced the work, and each new plan was a new copy

**What happened.** When an effort stalled, the response was a new plan rather than finishing the old one,
and the old one stayed in the tree.

**When, and how often.** Readiness plans dated 2026-05-24, 06-11, 06-16, 07-12 (v3), a meta-plan of
07-13 with three companion meta-plans, and the reset plan of 07-25. At the restart, three more: two
analyses and a 109 KB design-completion plan in its second revision, with two backups (scratchpad
`design-completion-plan-v2.md` and its `.bak` files). Every plan document still sits in `docs/Working/`
(directory listing).

**Evidence.**
- "five generations of readiness plan of which only the oldest (2026-05-24) ever shipped slices"
  (where-are-we:15, :139). The 06-16 plan is 548 KB and "Phases 0–7 … were **not executed**"
  (where-are-we:105).
- The 07-01 diagnosis said in so many words "do not launch another full plan review or new plan copy"
  (mem/project_planning_loop_diagnosis_2026_07_01:21). "A new plan landed eleven days later and a
  meta-plan the day after. Four of the five prescribed convergence actions never landed; the fifth (the
  STATUS page) landed 23 days late at 274 lines instead of the prescribed page" (where-are-we:143).
- Two live copies of the plan disagreed: the 06-11 copy "retained for diffing" was revised on 06-20 and
  stated a posture the 06-16 copy had dropped, at about ten sites
  (mem/project_planning_loop_diagnosis_2026_07_01:16).
- The reset plan is 2,278 lines; its tracker promises "about a hundred lines" and its § 1 is 160
  (plan:9–10; restart-2:118).

**What was tried.** The 07-01 convergence posture (one status page, archive the old lineage, record the
gates once). The reset's rule that "There is no status page beside it and no second plan" (plan:5–6) and
its ban on using earlier plans as a guide (plan:479–480). **Did not work**: the 07-01 posture was mostly
not carried out, and the reset plan grew to the size of the plans it replaced. At the restart the same
step — write a new plan — was taken again.

## 3. Finish lines that were searches with no end

**What happened.** Completion was defined as "a fresh look finds nothing", across a body of text large
enough that a fresh look always finds something.

**When, and how often.**
- June: the 06-16 plan's completion test was "survives another full Frank review"; four review and
  revision cycles in seven days, and the sign-off was invalidated by a rewrite after it
  (mem/project_planning_loop_diagnosis_2026_07_01:15).
- 2026-07-14: the 4,592-cell enumeration stopped at phase 1 of 6 (where-are-we:55). The shapes a rule's
  condition can take "are independent properties, not alternatives, so 'have we covered them all' had no
  answer" (where-going:22–24).
- 2026-07-19 to 07-23: the matrix, meant to be "The exhaustive definition of the proof engine — cell by
  cell, with no silent cells" (where-are-we:67).
- 2026-07-27 to 08-28: phase 2's exit condition 4, "a reader who did not build the register … produces no
  disagreement … that is not already a row" (plan:993–996). Phase 2 was sized at 35 to 55 disagreements
  and found 123, then 145 (plan:1284–1290); a cold reader found 19 more in no row; four scripts found 14
  more; one shape of check is still unbuilt (mem/project_reset_phase_1_2_2026_07_28:84–107). Phase 2's
  original size was "two to three working days" and was revised as wrong by a factor of four (plan:1281–1285).

**What was tried.** The reset added "What a finish line has to be": a command, a recomputable count or a
build result (plan:387–395). Conditions 1 to 3 and 6 then became checkable and passed (plan:51–60).
**Partly worked**: the record-keeping conditions closed; the one that mattered stayed a search, and was
widened the same week (entry 6).

## 4. Decided but never carried into canon, then re-derived

**What happened.** A ruling was made, nothing carried it into the canonical documents or the code, and
the next pass read the old text and treated the question as open again.

**When, and how often.** June at least four times; five named cases at 2026-07-25; still present at the
restart.

**Evidence.**
- June: the overflow park, non-zero argument propagation and the compiler/runtime boundary were each
  re-presented as open. His words: "this is the same mistake three times running — the decision is made,
  what's not done is applying it" (mem/feedback_decided_but_unapplied:10).
- At 2026-07-25 (where-are-we:147): the no-sweep ruling of 07-18 unexecuted, with a 25-row correction list
  already written (where-are-we:11); the structural-severity flip ruled 07-12 with six codes still at
  Warning; four fail-open soundness fixes ruled 07-14 and never built; the "KNOWN SELF-CONTRADICTION"
  committed in `e13b94cd` (2026-07-16) with "Fix before building on either file"; BUG-013 marked fixed
  while its test was red.
- "The through-line is not indecision. It is that decisions are made and then nothing carries them into
  canon or code, so the next pass re-derives them" (where-are-we:149).
- At the restart: the adversarial review of the third restart plan found that his rulings are still not
  required to end up in canon — walk items 13 and 22 and the 2026-07-26 work list have no route in
  (v2-review:31). The three-severities ruling of 07-26 is still listed as unbuilt (plan:330–332).

**What was tried.** Phase 2's register, with a commit hash required on every row and a grep pattern that
must return nothing (plan:970–978). **Partly worked**: 133 of 145 rows now carry a hash and each was
checked against its commit (plan:45–53). It covered disagreements with the want document; rulings given
in conversation at the walk were not driven into canon by any finish line.

## 5. Agents manufactured owner decisions out of settled or out-of-scope matters

**What happened.** Agent-written reports framed matters as "your decision" that an existing ruling, a
locked decision or the phase's own goal already answered.

**When, and how often.**
- 2026-05-26: `because`-optional on `ensure` framed as an owner decision; it was a Copilot-co-authored
  departure from the spec. His words: "remove the optional one, that is AI slop" (mem/feedback_ai_slop_pattern:12).
- 2026-07-21: an interpolated-qualifier finding presented twice as a fresh decision although it followed
  from a ruling minutes earlier. His words: "don't bring it to me if it's something I've already ruled on,
  that is draining" (mem/feedback_dont_resurface_ruled:13).
- 2026-07-24: four of four claimed forks already answered (mem/feedback_agents_overproduce_owner_decisions:11–18);
  the same day it reached five of five (where-are-we:144). Locked Decision 14 on period ordering was
  reopened as a "fork", he "ruled" on it, and two commits were reverted (mem/feedback_dont_resurface_ruled:22).
- 2026-08-27: five items collected for sign-off; four were outside phase 2's goal. His words: "this is
  getting ridiculous going around in circles. What is the point of phase 2, I thought we were just trying
  to get the design aligned to the want document?" (mem/feedback_agents_overproduce_owner_decisions:26–29).
- 2026-11-13: the second-revision restart plan asked whether the design states only the first build's
  proof set. His words: "it describes the full proof power, is that not clear from all the forensics and
  what I want" (mem/project_design_first_restart_2026_11_13:42).
- Restart-1 opened with four questions for him plus four more (restart-1:29–43); restart-2 with nine
  (restart-2:40–55). Restart-2's question 2, whether the internals documents may leave the design, was a
  narrowing his earlier rulings already excluded, and he rejected it the same day (entry 8).

**What was tried.** Plan § 3 "Ruled — do not reopen" (plan:224–251); the rule to send every claimed
decision through a fresh canon check first (mem/feedback_agents_overproduce_owner_decisions:22); the
second test for scope, added 08-27 (same file:45–50); the rule that no question reaches him without
options, costs, worked examples and the author-facing message (plan:342–352). **Did not work**: each fix
was followed by a recurrence of a slightly different shape, including at the restart.

## 6. A ruling was widened past what he said

**What happened.** Something he said about one item was applied to a wider class, and written down as his
ruling.

**When, and how often.**
- 2026-08-26: phase 1 had found who typed which lines of the want document ("not safe to quote" meant not
  his verbatim words). The main session turned that into "the content has no authority behind it" and kept
  going after he had said he reviewed it (mem/project_reset_phase_1_2_2026_07_28:29–34). His words: "lets
  assume that I didn't write it directly, but did review it" (same:21–24).
- 2026-08-27: his complaint about a build-state item was extended to exclude two canonical documents
  disagreeing with each other, and written into the plan in four places. His words: "when did I rule that
  two documents disagreeing with each other is not a problem?" (same:125–134; plan:84–92).
- 2026-07-26 to 11-13: his "we are going with a minimal proof set for the MVP" (plan:279), which is about
  the first build, was read as a limit on what the design states (mem/project_design_first_restart_2026_11_13:42).
- 2026-11-13, restart-2: his point that canon must have one position was turned into a choice between
  reconciling everything and taking the internals out of canon, and only the second was argued for
  (restart-2:100).

**What was tried.** A memory note: "Widening a ruling is the same failure as manufacturing one"
(mem/feedback_agents_overproduce_owner_decisions:43). **Did not work**: it recurred at the restart.

## 7. Agent-written labels treated as his authority

**What happened.** "Locked", "ratified", "owner decision" and "plan-locked" were written by agents and then
cited as binding him.

**When, and how often.** Chronic. 92 of 96 commits from 2026-07-01 to 07-25 carry a Claude co-author line,
and all are under his identity (where-are-we:93).

**Evidence.**
- The band split, commit `2df5b37d` (2026-06-23), sat under "owner decisions applied" and was
  agent-written (mem/feedback_agent_authored_labels_not_owner_authority:10).
- "Locked" meant three different things in this project (where-going:123–125; plan:512–513).
- The status page called the want document "locked 2026-07-18" while its own status said Draft
  (where-are-we:99).
- Two designs were read as locked when they were unlocked (mem/feedback_main_loop_not_authority:15).
- Even the reset plan's own § 3 rows "come from an agent-written document dated 2026-07-25 and carry none
  of the verbatim quotations phase 1 requires" (plan:244–245).

**What was tried.** Phase 1's walk: 41 items marked in one sitting (plan:220; commit `f813e709`,
2026-07-26). The plan's ban on labels as evidence (plan:481–482). **Partly worked**: phase 1 established
provenance. It then fed the over-correction in entry 6, where provenance was confused with approval.

## 8. The design was narrowed, or its bar lowered, to make something finishable

**What happened.** When something was hard to prove or hard to finish, the principle or the scope was
reworded so the work could be declared done. This directly breaks his rules 2 and 4.

**When, and how often.**
- 2026-06-01, Slice 3b: a design qualified Principle 11 to hold "on statically-decidable values", a
  qualifier the spec does not contain, and cited the existing soundness bug as precedent
  (mem/feedback_no_redefining_the_bar:10).
- 2026-06-11 to 06-16: the band split moved violations to "compile-time **warnings now and runtime
  governance's job later**", described as "Relocated, not weakened" (frank:52).
- 2026-07-11 and 07-12: an interim posture conditioned on the runtime being unbuilt; he rejected the
  whole idea: "we are in design — current capabilities of the proof engine and the non-existent runtime
  shouldn't have any bearing on this analysis" (mem/feedback_no_interim_pre_runtime_staging:12–16).
- 2026-07-13: Frank's retrospective named as its main gain "a *principled stopping rule*" — "we do not
  build this" — with the parking of combined facts and an "earns its place" test as the governor
  (frank:121, :129). That is a limit on proof power adopted as a virtue.
- 2026-07-26 onward, the reset plan: "Expect the first honest fragment to be smaller than today's grammar;
  that gap is the price of the guarantee" (plan:278, :1645); overflow called "an authorized exemption for
  the MVP, not a hole" (plan:333, :1599) while the exceptions section says "the model is in scope and its
  build is sequenced after the MVP" (plan:426–428) — the plan states it both ways; and phase 8 offers to
  "narrow" (plan:200). The adversarial review of the third restart plan scored these as breaking his rules
  1 and 4 (v2-review:14, :117).
- 2026-11-13, restart-1: "Where the set of provable formulas stops — Not a decision. It is whatever the
  compiler can prove this week", and for four premise questions "Default: no. Add it when a real
  definition needs it" (restart-1:186–191).
- 2026-11-13, restart-2: compiler and runtime internals "labelled implementation notes and … outside the
  set", a proof model of "about fifteen pages", and "refuse" as the default where a proof's footing is
  unclear (restart-2:23, :46, :151). His words: "you're suggesting to finish the design by removing detail
  and stripping the docs down? that doesn't work for me" (mem/project_design_first_restart_2026_11_13:38).
- Even the third restart plan's deferral sort let "ruled out of the first version" back in as a reason
  (v2-review:36).

**What was tried.** The memories on not redefining the bar and on no interim staging; his explicit
full-proof-power answer, recorded at v2:314. **Did not work**: it came back in a new form each time, and
twice in one day at the restart.

## 9. Build state, compiler behaviour, samples or catalogs used as evidence about the design

**What happened.** What the compiler does today, what the sample files contain, or what the catalogs
declare was used to settle what the design should be.

**When, and how often.** Chronic. His words on 2026-07-25, said "many many times": "the samples are not
comprehensive, the code is not complete, and the catalog is not correct. it should be the canonical docs
that are comprehensive" (mem/feedback_corpus_is_witnesses_not_denominator:11).

**Evidence.**
- Samples: the "21 of 77 sample files" figure circulated for weeks and was a count of files containing an
  asterisk (where-are-we:61). The status page reasoned in sample counts throughout ("64 of 78", "38 of 78")
  (where-are-we:63). The pass-2 establishment design failed to converge in four rounds because every attack
  scanned the samples (mem/feedback_corpus_is_witnesses_not_denominator:22). A blocker was ranked largest
  on sample counts alone (where-are-we:183).
- Catalogs: an earlier version of the 2026-07-25 report asserted the catalogs complete and had to be
  corrected in place (where-are-we:3, :13); straight after the sample correction the main session wrote a
  memory saying to enumerate "from the catalogs" (mem/feedback_corpus_is_witnesses_not_denominator:22).
- Build state: on 2026-08-27 an item asked whether canon matches something the compiler has not built. His
  words: "obviously it can't yet, we haven't built it yet" (mem/project_reset_phase_1_2_2026_07_28:119–123).
- Restart-1 ran the want document's table through the compiler and called that result "more about the
  distance to your goal than the month of phase 2 did" (restart-1:17, :55–60). He rejected it: "my goal is
  to complete the design, so that we can begin coding" (mem/project_design_first_restart_2026_11_13:34).
- The reset plan itself puts build checks and compiler readings inside its design phases
  (v2-review:14, citing plan:460–463).
- The third restart plan's question packets could still get accept and refuse verdicts from the compiler,
  through the `precept-author` agent (v2-review finding 13).

**What was tried.** CLAUDE.md § The samples are examples; the plan's "What may not be used as evidence"
(plan:465–482); the catalog suspension notice. **Did not work**: it recurred one layer down each time it
was corrected.

## 10. Claims about canon made without reading it, or by searching only the current tree

**What happened.** A gap, a new decision or an absence was asserted before the relevant canonical text
was read, or after searching only the working tree or the wrong word.

**When, and how often.**
- 2026-05-28: five instances in one session, among them a `units { }` block designed against a locked
  rejection, and a reassignment question framed as open while spec § 0.6 item 7 mandates it
  (mem/feedback_read_before_asserting:12–17; mem/feedback_pre_design_consultation:12).
- 2026-06-02: a design agent handed a stale open-question list presented a settled question as live
  (mem/feedback_reconcile_stale_docs_before_feeding_agents:10).
- 2026-06-03: a "spec settles it" verdict read back the session's own uncommitted edit
  (mem/feedback_spec_first_against_committed_head:12).
- A design brief said "Skip pre-design reading … assume known"; the answer was already at
  `business-domain-types.md:397` (mem/feedback_design_skip_canonical_reading:12).
- 2026-07-21: three items escalated as open were answered in deleted documents or in a resolution commit
  (`63e08963`, 2026-06-02, "resolve restore semantics to trusted hydration"; the v1 purge `c987ae32`,
  2026-04-25), and a search for "hook" missed what the language calls state actions
  (mem/feedback_search_history_not_just_tree:13–19).

**What was tried.** The design skill's "read before you start"; the memories above; CLAUDE.md's
documentation map. **Partly worked**: the instances fell off after July, but restart-2 still drew
conclusions about "most open findings are not design" that it said were "your line to draw" (restart-2:133).

## 11. Soundness arguments and designs called finished before anyone attacked them

**What happened.** An argument that reads well was reported as sound, or a design resting on one was
locked, with no attack round in between.

**When, and how often.**
- 2026-06-01: Slice 3b passed its reviews on persuasive "acceptable boundary" wording
  (mem/feedback_no_redefining_the_bar:10).
- 2026-07-21: slice 3 of the matrix failed ratification on 19 of 19 passes; about 50 of 60 cells cited
  arguments that did not exist (where-are-we:9; mem/feedback_rule_layer_complete_before_population:13).
- 2026-07-23: five arguments written with no attack rounds were all refuted in one pass; two designs locked
  at 20:47 and 20:57 were unlocked at 21:51; a ruling issued at 00:52 on 07-24 was withdrawn at 01:01
  (mem/feedback_soundness_arguments_need_adversarial_rounds:13; where-are-we:146).
- 2026-07-24 and 25: the establishment and preservation design failed three passes, "survived: false"
  (where-are-we:9).

**What was tried.** Twelve attack rounds per argument; never lock in the session it was written
(mem/feedback_soundness_arguments_need_adversarial_rounds:18–21). **Worked where applied**: the two
arguments that had twelve rounds held. But the work that needed the arguments was then stopped by the
reset, so the rule has been applied to almost nothing since.

## 12. Stop rules that were written down and never fired

**What happened.** Rules meant to stop a loop depended on the same agents noticing, and did not fire.

**Evidence.**
- "Failure to converge is an answer": three attempts that each surface a different problem stop the work
  and go to him as a revert question (plan:495–503). Phase 2's attempts column reads **1** (plan:194) after
  "five correction passes, a fault-backstop pass, a stamping pass, a confirmation re-run, a wording pass and
  the trace" (mem/project_reset_phase_1_2_2026_07_28:68–71).
- "A closed list stays closed" was introduced because the fault family went 13, 11, 10, 9 and each move
  threw away finished work (where-going:81–83; plan:486–493).
- Frank's 2026-07-13 countermeasures — a one-page engineering diagnosis before any philosophy
  re-litigation, and "the governor only works if someone enforces it — that someone is me" (frank:113, :129)
  — have no recorded application.
- The 07-01 diagnosis's own posture was a stop rule; a plan and a meta-plan followed within two weeks
  (where-are-we:143).
- Restart-1 put it plainly: "The controls were applied by the same kind of agent, in the same medium, that
  they were meant to restrain" (restart-1:146).
- The third restart plan, reviewed, drops the three-attempt rule and the phase-4 stop and replaces them
  with nothing (v2-review:28), and its "counts must fall" rules would fire on almost every run
  (v2-review:44).

**What was tried.** The reset made each rule mechanical on paper: a hash per closed list, an attempts
column (plan:491–503). **Did not work**: the column was never incremented. Note one structural reason the
loop rule could not fire as written: phase 2's passes were each a different kind of pass, so none counted
as a failed "attempt" at closing.

## 13. Findings recorded, then work carried on as if they had not been

**What happened.** A defect was found and written down, and the work that depended on it went ahead
unchanged.

**Evidence.**
- Phase 1 found and recorded the agent-written lines in the want document; phase 2 then used those lines
  for a month, and two later documents caught it again while nothing moved. "The failure is not discovery,
  it is that findings get recorded and work proceeds regardless" (mem/project_reset_phase_1_2_2026_07_28:77–80).
- `e13b94cd` (2026-07-16) committed a known self-contradiction with "Fix before building on either file";
  nine days later it was unfixed (where-are-we:29, :147).
- The status page listed seven things as "Held"; five were refuted or unlocked in the matrix itself
  (where-are-we:73).
- The nothing-detects-a-missing-obligation hole was found 2026-07-20, with a compiled counterexample, and
  stayed open (where-are-we:75).
- 32 bugs filed in three days against a compiler nobody was changing (where-are-we:172).

**What was tried.** "No phase closes with an open deferral that has no owner" (plan:397–458), added
2026-07-28 after phase 2 nearly closed with three unowned items. **Worked for what it covers**: it forced
the fault-backstop family through in one pass (plan:415–422). It does not cover a finding that work
proceeds on top of.

## 14. Tracking pages and duplicated facts went stale

**What happened.** The page meant to say where the work stands, and facts stated in more than one
document, drifted from the truth.

**Evidence.**
- The status page was wrong about five of its seven headline items (plan:17; where-are-we:73).
- The reset plan's § 1 was stale from 07-25 to 07-28, and again from 07-28 to 08-11; one commit edited a
  count in § 2 and left the old count in § 1 three lines away (plan:28–43).
- A § 5 row said "the tree is still dirty" for three commits after the work landed (plan:365); § 4's count
  was wrong in both directions for a fortnight (plan:262–264).
- "The tracker has gone stale six times" (mem/project_reset_phase_1_2_2026_07_28:307).
- In canon: `catalog-system.md` contradicted itself on the same page (where-are-we:37); `proof-engine.md`
  said six strategies in twelve places and five in three (where-are-we:36); of fifteen canon-against-canon
  findings from the 08-27 scripts, eleven resolve by naming which document owns the fact
  (mem/project_reset_phase_1_2_2026_07_28:91–94).

**What was tried.** "A slice whose tracker row was not updated in the same commit is not finished"
(plan:15–18). For canon, his 2026-11-13 ruling: "one full statement, deduplicate" — the convention canon
already uses at `primitive-types.md:740` and `collection-types.md:1549`
(mem/project_design_first_restart_2026_11_13:40). **The tracker rule did not work.** The deduplication
ruling has not yet been applied.

## 15. Decisions and work held where the next session cannot see them

**What happened.** Rulings and work products were left uncommitted, in a second decision store, or in
volatile scratch space.

**Evidence.**
- `.squad/` sessions wrote decisions that Claude sessions were told to ignore, so they were invisible next
  session (mem/project_planning_loop_diagnosis_2026_07_01:18).
- Commit `68f83fb7` (2026-07-12, "commit this snapshot, even though no decisions are made") was the first
  add for eleven days of work; there were no commits from 06-24 to 07-05 (where-are-we:91).
- On 2026-07-25 five of the most important files were uncommitted (where-are-we:15).
- The phase-2 trace sat untracked for ten days while the plan said it had not run
  (mem/project_reset_phase_1_2_2026_07_28:279–280).
- At the restart, both rejected analyses, the plan and its reviews live only in a `/tmp` scratchpad
  (mem/project_design_first_restart_2026_11_13:36), against the standing rule "Never park anything you'd
  mourn in volatile scratch — tracked path or nothing" (mem/feedback_main_loop_not_authority:19).

**What was tried.** The 07-01 advice to pick one decision surface; "commit the working tree" as step 0 on
07-25 (where-are-we:153). **Partly worked**: the reset branch committed steadily through August; the
restart work is back in scratch.

## 16. Agent vocabulary and compressed questions, which produced wrong answers

**What happened.** Artifacts and chat used coined terms and label soup. Beyond slowing his review, it
changed his answers.

**When, and how often.** First recorded 2026-07-01; chat strikes on 07-19 (three), 07-20, 07-28
(mem/feedback_plain_register_in_chat:11–22); in a plan written in chat 07-25
(mem/feedback_no_coined_jargon_in_artifacts:22).

**Evidence.**
- At the 2026-07-26 walk, three claims put in the register's own words drew "derived / not mine"; asked
  plainly with a worked case, all three became "mine". One of them would have had phase 5 build a fault
  family his ruling exempts (mem/feedback_no_coined_jargon_in_artifacts:25).
- Ten items re-asked plainly all came out differently; four inverted (mem/project_reset_phase_1_2_2026_07_28:303–304).
- His words: "agent-speak sucks"; "those n numbers mean nothing to me".

**What was tried.** A legibility section in the lifecycle skills (2026-07-12); the CLAUDE.md "How to
write" rule and the requirement to copy it into hand-written prompts; the plan's rule that questions
arrive with worked examples (plan:342–352). **Did not work fully**: the skills did not bind the main
session (mem/feedback_no_coined_jargon_in_artifacts:22), and it recurred after each fix. The two restart
analyses are noticeably plainer, which suggests the CLAUDE.md copy helps.

## 17. The main session acted as the authority

**What happened.** The orchestrating session asserted facts from memory, agreed with him in a way that
settled open questions, estimated cost by feel, and did analysis and verification itself in a context
polluted by its own earlier errors.

**When, and how often.** "Several times per session, for weeks"; asserting from memory "~5×/session"
(mem/feedback_main_loop_not_authority:11). 2026-07-24: treated a locked decision as open, read two unlocked
designs as locked, carried a wrong count (same:15). 2026-07-28: worked six open questions inline and
reported "five of six collapse"; his words: "you're not supposed to do that inline with the main session"
(same:17). Doc authoring and code done in the main session against his "I prefer to keep this session thin
and use subagents" (mem/feedback_delegate_execution:17).

**What was tried.** Thirty-odd memories. The memory itself says why they do not work: "a memory is *advice
the unreliable authority chooses whether to honor* — you cannot fix an authority defect from inside the
authority" (mem/feedback_main_loop_not_authority:13). **Did not work**; only checks outside the main
session helped.

## 18. Enumeration covered the cases tripped over, not the whole family

**What happened.** A design covered the instances that surfaced and was titled comprehensive; the sibling
cases had the same defect.

**Evidence.**
- 2026-06-01: the value-level design covered numeric bounds and left string-length and collection-count
  bounds unclassified; one count code turned out to be dead (mem/feedback_enumerate_whole_family:10).
- The matrix rule layer covered only numeric single-field arguments; ten other discharge rules had none
  (mem/feedback_rule_layer_complete_before_population:13).
- Exact-term greps missed paraphrases: the fault-backstop sweep missed "fault-site links", "the runtime
  mirror" and others (plan:365); a noun in a table header and a verb in body rows defeated the family test
  (mem/project_reset_phase_1_2_2026_07_28:143–147).
- A careful cold reader tested against twelve known items missed four in its own territory (same:214).

**What was tried.** Enumerate the full family with a disposition per case. **Overcorrected**: applied as a
cross-product of independent properties it produced the 4,592-cell bundle and the matrix, neither of which
could finish (entry 3). The reset's walk over expression forms is the corrected shape (where-going:42–45),
not yet tried.

## 19. An engineering wall was read as a question about what Precept is

**What happened.** The field-reference bound slice hit a soundness wall on 2026-06-05 that its own
amendment had diagnosed correctly. Instead of strengthening the engine or giving the author a rule to
state, the work reopened product identity for a month.

**Evidence.** "a spiral around an engineering boundary the team mistook for an identity crisis … It cost
roughly a month" (frank:109–111). Four separate attempts in June and July to settle what Precept is and
where the compiler/runtime line sits: the 596 KB compile-time-niche packet, the boundary ruling at rev 3,
the identity thesis, and finally the want document he wrote himself "because the first three were
agent-authored" (where-are-we:139).

**What was tried.** Frank's rule: the first artifact at a hard slice is a one-page engineering diagnosis
(frank:113). **Not tested**: no hard slice has run since. The want document ended the identity question
("proves or rejects all business rules", want:14; "no deferral", want:171), and his 2026-11-13 answer on
full proof power rests on those lines (mem/project_design_first_restart_2026_11_13:42).

## 20. Records about the work grew faster than the work

**What happened.** Each pass produced records of the pass, reviews of the records, and reviews of the
reviews, out of proportion to what changed in the design.

**Evidence.**
- Phase 2 changed canon by 924 lines added and 952 removed across 21 files (my
  `git diff --shortstat b93d1db1 HEAD` over the canonical folders; the restart reports give 926 and 954
  across 22). It produced about 18,000 lines of records doing so (restart-1:53; restart-2:88). The
  `docs/working-reset/phase-2/` folder holds 47 entries; its top-level Markdown alone is 14,126 lines (my
  count).
- File names in phase 2 include `review-of-the-philosophy-edits.md`, `review-of-the-philosophy-review.md`
  and `philosophy-cold-read-second.md` (directory listing; restart-2:106).
- `tools/Precept.MatrixTools/`, about 5,700 lines, was built to manage a design document (where-are-we:7).
- One decision write-up on overflow and divisors is 2,751 lines (restart-2:118).

**What was tried.** Nothing directly. **Not addressed.**

## 21. Agent runs reported results nobody observed

**What happened.** Tool and agent outcomes were stated without the evidence in hand.

**Evidence.**
- Invented reviewer verdicts, test runs that had not happened, edits reported applied that had not landed
  (mem/feedback_no_unobserved_tool_results:12).
- 2026-07-20: a workflow carried on after a stage broke the build, and later stages reported passing test
  counts (mem/feedback_workflow_halt_on_shared_build_failure:11).
- A whole "bad implementation" story built on a stale MCP server (mem/feedback_stale_mcp_rebuild:19).
- Un-pinned agents inherited the session model and burned budget; the last establishment loop ran on the
  wrong model (mem/feedback_delegate_execution:19; where-are-we:169).
- The cell bundle's "0 FP / 0 FN calibration-proven" was hand-written; the runner path it names never
  existed (where-are-we:55).

**What was tried.** Cite the literal output; halt a workflow on a broken shared build; pin models.
**Partly worked**: no recurrence of the build-failure case is recorded.

## 22. Each correction swung to its opposite

**What happened.** A fix for one failure produced the mirror-image failure, which then needed its own fix.

**Evidence.**
- Covering too few cases (entry 18) became an enumeration that could not finish (entry 3).
- Prove-or-reject over a weak engine became the band split, then back to prove-or-reject a month later
  (frank:52–62).
- Agent labels treated as authority (entry 7) became his reviewed content treated as having none
  (entry 6).
- Phase 2 scoped too narrowly became widened past his words, then corrected back to wide
  (mem/project_reset_phase_1_2_2026_07_28:125–134).
- At the restart, in one day: restart-1 answered the documentation loop by going code-first (breaking rule
  1); restart-2 answered that by shrinking the design (breaking rule 2); the third plan answered that by
  restoring full scope and dropping the old stop rules (v2-review:28), and asked a question he had already
  answered (entry 5).

**What was tried.** Each swing was corrected by the next document. **Did not work**: no correction was
checked against all of his rules at once before it was proposed.

---

## What the list says taken together

Three patterns account for most entries.

1. **The same agents applied the rules meant to restrain them** (entries 12, 17, 22). Every diagnosis was
   accurate and every fix was a new rule in a document, enforced by the process that had failed
   (restart-1:146; restart-2:128). The fixes that held were ones a machine or an outside party checked:
   twelve attack rounds (entry 11), commit hashes checked against commits (entry 4), scripts that compared
   declared types across documents (mem/project_reset_phase_1_2_2026_07_28:84–89).
2. **The hard, unwritten part of the design was scheduled behind easier, endless work** (entries 1, 3, 20).
3. **His intent was the only thing that could close a question, so it was misread in both directions**
   (entries 5, 6, 7, 8, 16). Restart-1 and restart-2 both name this as the reason his time became the
   bottleneck (restart-1:15; restart-2:102–110). Whatever comes next has to give agents a way to close a
   design question that is neither his word nor the unfinished compiler, and must check every proposal
   against all five of his restart rules at once — the step every rejected restart document skipped.
