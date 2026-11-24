# Review of the design-completion plan

Plan reviewed: `/home/sfalik/source/repos/Precept/docs/working-reset/design-completion/lineage/design-completion-plan-v1.md` (250 lines), written against `spike/Precept-V2-Radical-reset` at `2f126a3b`. Line references of the form `:NN` without a file name are to that plan.

Five reviewers each examined the plan from one angle: the owner's four rules, the plan's factual claims, whether the plan will converge, whether its definition of finished covers canon, and the questions it puts to the owner. A skeptic then tried to refute each finding against the sources. Only findings that survived are listed in the summary. Where the skeptic corrected a line number or a severity, the corrected value is used.

---

## Five-minute summary

### Is the plan sound enough to start?

Not as written. Its overall shape is right: the design is finished before any code, no detail is removed, each fact gets one owning statement, the proof model is the centre of the work, and the owner is asked less. Two problems would repeat the failures of June to August on the first day and in week 5. First, the owner gets his first batch of questions on day 1, before anything has checked whether he has already answered them. Second, the proof model gets three rounds of attack in total, where this project's own record says each argument that held needed about twelve. A number of serious problems follow those. None of them requires a different plan. Each has a concrete fix, and after the fixes the plan can start.

### Findings that survived, most severe first

Several reviewers found the same problem from different angles. Those are merged here and the angles are named.

**Blocking**

1. **The owner's first questions arrive before anyone checks them.** Day 1 has him answer seven questions (:225); the table of his past rulings is built on days 1–2 (:227) and the "probably answered" checks run on days 2–3 (:229). That breaks the plan's own rule at :167 and his standing correction (feedback_agents_overproduce_owner_decisions.md:22). The plan's author also says it did not read the walk file (:248), and the rulings table leaves out his 2026-08-26 rulings (trace-walk-rulings.md:40–78). *Fix:* move the batch to the end of week 1; first have a fresh agent check each question against the walk file, trace-walk-rulings.md, the old plan's rulings, and the 2026-11-13 memory, and prepare every survivor with a worked example and what each answer makes the compiler say. (Owner rules, owner questions.)

2. **The attack budget for the proof model is a quarter of what has been measured, and failure sends him a question about shrinking the language.** Three rounds for the whole model (:116, :141); a "new kind of flaw" in round three goes to him as a narrowing question (:204). The measured record is twelve rounds per argument that held (feedback_soundness_arguments_need_adversarial_rounds.md:13), and whether a flaw is "new" is a judgement made by the agents doing the work, like the old attempts column that never moved. *Fix:* budget rounds per argument on the measured figure; have a fresh agent, not the author, classify each refutation; send a failing argument to a second author before it reaches him; when it does reach him, offer more rounds, a revised argument or narrowing on equal terms, with the counterexamples. (Convergence, owner rules.)

**Serious**

3. **The language lists the coverage check depends on do not exist.** The plan says phase 3's lists "are kept" (:152), but phase 3 never started (compiler-readiness-plan.md:195–197) and phase-3/ holds no list data. Coverage item 4 (:107) and the example set (:228) rest on them. *Fix:* add a sized step-1 task that extracts the lists from `docs/language/`, not from the catalogs, including where an expression can sit (soundness-and-coverage.md:244–263), and treat the write-site list (spec:1999) as design work. (Convergence, completeness.)

4. **Deduplication is treated as mechanical where the copies disagree on substance.** Four of the fifteen scripted duplicates were already found substantive (compiler-readiness-plan.md:1096–1100), Restore recomputes computed fields or not depending on the document (:1065–1066), and result-types.md:148/:152 write the open editable-write question into a record that the plan sends to immediate deduplication (:132). Picking the owning text picks the answer. *Fix:* split straight restatements (merged mechanically) from disagreements (decided against the want document and spec by a fresh agent, or sent to the question gate); hold result types and anything touching Update, Restore or constraints until the proof model settles. (Owner rules, convergence, completeness.)

5. **Duplicated prose is left out of deduplication, though rule 3 covers it.** The plan's own example of duplication is a prose paragraph (:88), yet no check looks for it (:118), and the summary claims scripts confirm no second statement exists (:12). *Fix:* list the normative concepts once, give each an owning section, search for each concept's terms across canon, and turn the hits into pointers; reword the summary claim. (Owner rules, convergence.)

6. **The boundary of what the compiler will prove is defaulted to one example and shown to him in week 10.** The default is "the smallest set that proves your example and rejects its table" (:175), read only at the refused-list review in weeks 9–11 (:143). want:185 measures the trade against "the kinds of business problems we intend to solve", not one example, so most of the temporal, business and collection language would land on the refused side by default, and an objection then reworks steps 2–5. *Fix:* send a draft refused list, grouped by language area with an example and message per item, on the first Friday and again after step 4. (Owner rules, convergence; the owner-questions reviewer's version of this was downgraded to minor.)

7. **The default rule contradicts itself and is applied to questions he kept for himself.** :169 allows only defaults that refuse more, then forbids defaulting anything that changes what an author may write, which every refuse-more default does. The naming question is defaulted without saying which way (:176) though he said he was not ready to rule (the-walk.md:201–202). The divisor question is defaulted though he asked for outside research (where-a-divisors-non-zeroness-comes-from.md:126) and it is "closer to one decision than two" with the integer model. `from any` is defaulted to the side that refuses less (spec:2203). *Fix:* restate the rule as one test (refuses more, can be widened later, touches no locked decision and nothing he reserved); send naming and divisor-with-integer-model to him with their prepared write-ups. (Owner rules, owner questions.)

8. **The editable-write question is framed to one answer, and the write-ups it relies on are out of date.** Q2 (:32) asks him to confirm the want:143 reading, where four options were prepared (what-discharges-an-editable-write.md) and he declined on 2026-08-26 to have it settled in passing (trace-walk-rulings.md:42–46). His own approved example is accepted only under the want:143 reading (want:36, :40), but the phase-5 write-ups still call those lines "not yours" (what-discharges-an-editable-write.md:37–42; editable-write-cascade.md:116–122), and :155 hands those write-ups to the proof-model author unflagged. *Fix:* have a fresh agent recheck it against the approved want document; if it still goes to him, show every option with what it refuses and quote want:143, want:199, philosophy.md:22 and :42; mark the stale write-up passages superseded first. (Owner rules, owner questions.)

9. **Q8 offers to move the overflow design out of the finished design.** Canon says the model is in scope and only its build is deferred (spec:104, :110, :272); he confirmed "MVP" means a build milestone (trace-walk-rulings.md:71–77) and rejected shrinking the design (project_design_first_restart_2026_11_13.md:17). The real open question, fixed-width or arbitrary precision for `integer` (the-walk.md:420–421), is not asked. *Fix:* drop Q8; ask the integer model choice, prepared, together with the divisor question. (Owner rules, owner questions; the convergence reviewer's version was downgraded to minor.)

10. **Q3 on time-referencing rules does not say one option overrides locked text.** "Time in guards only" overrides philosophy.md:28 and temporal-type-system.md:568; the other option sits against philosophy.md:18 and :42, which is a philosophy gap CLAUDE.md says to raise. *Fix:* quote the locked lines word for word, label the override, and give each option a worked example. (Owner questions serious; owner rules minor.)

11. **The earlier-row default would refuse his own corrected example.** want:80–81's reject message reads `{MonthlyRepayment}`; once row 1's guard is corrected, only "earlier rows did not match" proves it is set, so the default "no" (:179) refuses his message. *Fix:* work the corrected `ReduceLimit` first, then put the earlier-row question to him with this case or state the rewrite it forces. (Owner questions.)

12. **The plan lets the proof model run beside deduplication but marks the model's reach before the model exists.** The reach is marked on days 1–2 (:226), before the contents list (:229) and the hand-working (:230); result types, the checker's place in the stage order (compiler/README.md:61) and records tied to open questions sit in the "safe now" set. *Fix:* mark the reach from the week-1 hand-working; freeze those facts; any later change to a deduplicated fact edits its one owning statement and is logged. (Convergence.)

13. **No one is named to settle disputes between agents.** Two want maps are "compared" (:104), paired derivations "must agree" (:115), a request queue has no one emptying it (:162), and the main session may not analyse (:159). *Fix:* one fresh-context adjudicator per dispute with a written rule of precedence, rulings logged at path:line. (Convergence.)

14. **No rule for reopening the proof model after step 4.** Steps 5 and 6 will find gaps; the old "closed list stays closed" rule with a recorded hash was dropped. *Fix:* hash the model when step 4 passes; any later change reruns attacks on the changed arguments; pilot the hand walk on the front-end stages in weeks 1–2; give step 6 a repair budget and stop rule. (Convergence.)

15. **The stop rules and record rules have no force, and the plan creates about a dozen side records.** One combined weekly total (:205) lets fast deduplication hide a stalled proof model; exception files are approved by their own authors; the new plan has no fixed path and MEMORY.md:3 still names the old plan as live. *Fix:* two tracks with separate stop conditions; exceptions approved by another agent; ownership recorded inside canon and regenerated by script; fixed path for the plan and the memory pointer updated in the same commit. (Convergence.)

16. **The 8–12 week estimate has no measured basis and no time-based stop.** Phase 2 ran four times over; its month held commits on about eight days, the rest waiting on the owner. *Fix:* report agent effort and owner waiting separately, measure a rate in week 1, and add a week-4 checkpoint with written pass criteria; for Q2 and Q5 proceed on both answers in parallel. (Convergence.)

17. **"One author" for three weeks is a series of sessions with no hand-over, and its reading list misses proof-engine.md and catalog-system.md.** *Fix:* the author writes drafts straight into the target canon sections with a short hand-over note; add those two documents; drive type-document reading from the coverage table; name the phase-5 files individually. (Convergence.)

18. **Decisions and defaults are not routed through CLAUDE.md's four-part rationale and `/design` rules.** The plan never mentions rationale, alternatives, precedent, tradeoff or `/design`. *Fix:* each decision and default carries the four parts; narrowing from a stop goes through `/design`. (Convergence.)

19. **The open-question inventory misses most of canon's open questions, and the runtime index has 21 rows, not seven.** Item 5 reads only the spec and two README indexes (:108); collection-types.md:1294–1300, language-server.md:1575, tooling-surface.md:217/:439 and catalog-system.md:1509 are missed, and the search phrases would not find them. Most runtime rows are internals choices the default rule cannot cover. *Fix:* build the inventory from every open-questions heading and pending marker in canon; correct the count; add a step where agents draft answers to internals questions with rationale, recorded as overturnable defaults. (Completeness serious; facts reviewer's count error alone minor.)

20. **Deferrals tied to "later in the build" can pass as "waits" with no rule.** Fault Q3 lives only in the frozen old plan (compiler-readiness-plan.md:443–445); Q8 frames overflow as integer only though spec:272 and primitive-types.md:697–701 include temporal and number infinity. *Fix:* list every deferral and classify it as designed now, ruled out of v1 by him (quoted), or deferred in build order only, which still needs its design written. (Completeness.)

21. **Unfinished documents outside the proof model have no step.** extension.md is a stub (:7), preview/inspect is design-only (language-server.md:1575), plus the five uncatalogued writers and the graph roadmap modifiers; the plan's own "no stubs" test would fail. *Fix:* one row per unfinished document with a finish condition. (Completeness serious; owner rules minor.)

22. **Canon names source files as the owner of several lists.** soundness-and-coverage.md:200–203, :214–217, tooling-surface.md:1251–1261 and the type documents point to `.cs` files as "source of truth"; pointer-style deduplication would make code the design. *Fix:* each such list is stated in full in canon and the pointers reversed. (Completeness.)

23. **Create, Restore and inspection are missing from the hand walk and partly from the contents list.** The walk runs only a Fire and an Update (:114); Create is the base case (spec:268), Restore is disputed (compiler-readiness-plan.md:1066), inspection must keep the guarantee with missing inputs (spec:2165–2167). *Fix:* add all four operations to the walk and the missing items to the contents list. (Completeness.)

24. **The reading and decisions asked of him are larger than "one page a week", and the counts disagree.** Plus a defaults list to sign, up to five prepared questions a week, a four-page first Friday, and "three need you now" against four marked his. *Fix:* give one honest total of questions, pages and sign-offs, schedule each question's preparation, reconcile the counts. (Owner rules.)

25. **The owner's example has a second unguarded optional read.** want:81 interpolates `{MonthlyRepayment}` without a presence test, refused by spec:1561, so the example is refused in two places, not one, and the mutation table assumes it is accepted. *Fix:* treat it as a consequence of the locked presence ruling, correct both lines, report in one line rather than ask. (Facts serious; owner rules and owner questions minor.)

**Minor** (one line each; full text in the angle sections): the git command and phase-2 record size (~17,750 lines, not 14,000); "145 applied corrections" is 133 applied plus 12 dispositioned, and N105, N106, N107, N117 belong to the proof model; Q4 on dead ends misquotes the philosophy and omits spec:186 and compiler-and-runtime-design.md:147; the "nine documents" term counts cannot be reproduced; two of the four check scripts already read prose; spec § 5 and proof-engine.md are misdescribed; the placeholder list misses soundness-and-coverage.md:66; Q6 re-asks a policy he set; the computed-field item ignores walk item 32; the ensure item covers only half its finding; Q5 omits spec:1024, :1082, :2092; Q1 does not say which walk-item-1 rulings survive and drops the bounded counterexample search; the contents list omits several items want:179, :210, :211 and the soundness document assign; the language documents get no internal-contradiction check beyond the scripts; documents restating the model (overview § 1, glossary, onboarding, docs/README.md) are missing from step 5; no status value means "finished".

### What this review did not cover

- **Use of frontier models (owner rule 4).** No reviewer tested whether one session can hold the proof-model reading list, how many agents each step needs, or which model each should run on. The plan itself lists the first as unverified (:250).
- **Whether the proof model can be written at all in the time given.** The reviewers judged the schedule and budgets against the project's record; nobody attempted any part of the model.
- **Line-number accuracy across the whole plan.** The facts reviewer checked about 40 claims; the rest were checked only where a finding touched them. The skeptic corrected several line numbers cited by reviewers (for example compiler-readiness-plan.md:1096–1100 rather than :1085–1090, where-a-divisors…:126 rather than :122).
- **Full reading of the walk file and contradiction register.** Reviewers quoted them where a finding needed them; no one cross-checked every ruling against every question.
- **Effects of the fixes on each other.** Several fixes add work (lists, adjudicator, per-argument rounds, prose deduplication). Nobody re-estimated the schedule with them applied.

---

## Angle 1: the owner's rules

Overall: the shape honours the rules, but the day-1 batch, the defaults, the proof-set boundary, Q8 and the treatment of deduplication break them in specific places.

### Surviving findings

**The starting batch reaches the owner before any check that he has already answered it — blocking.** Day 1 is "You answer the starting batch" (:225); the rulings table is built on days 1–2 from "the walk file and plan § 4" only (:227); the "probably answered" checks run days 2–3 (:229). This breaks :167 and feedback_agents_overproduce_owner_decisions.md:22 ("Never forward a claimed owner decision without an independent canon-check first"). The plan's author did not read the walk file, register or period-ordering design (:248) and did not verify the rulings (:250). Q2 and Q4 lean on want-document text (:32, :34), which by :94 and rule 6 (:208) may make them writing work rather than questions. The rulings table's sources miss trace-walk-rulings.md:40–78 (2026-08-26: the narrowing comes out, the runtime question indexes stay, the MVP definition confirmed), compiler-readiness-plan.md:224–251 and :291–340, and project_design_first_restart_2026_11_13.md:17–19. Fix: move the batch to end of week 1 after an unprimed fresh-agent check against all those sources, quoting the settling text; widen the table's sources; drop questions that survive only because the walk file was not read.

**The defaults break the plan's own rule and settle questions he was not ready to rule on — serious.** :169 says a question "that changes what an author may write … is never defaulted", yet :177, :179 and :181 each refuse what an author could write (:179 states that cost). :176 says "Default" without naming the option. He was "not prepared to rule" on naming (compiler-readiness-plan.md:280) and asked for outside research on the divisor (where-a-divisors-non-zeroness-comes-from.md:126). That write-up calls the divisor and integer model "closer to one decision than two" (:145–147), yet one is defaulted and the other waits (:41, :184). Q6 (:36) would approve these in bulk. Fix: a default only where he has ruled the general policy, each listed by name with its example and chosen option; naming and divisor go to him with their write-ups; pair divisor with the integer model.

**The largest design decision is defaulted to what one example needs and shown to him only at the end — serious.** :175 defaults the provable set to the bank example's needs, reviewed in weeks 9–11 (:143). compiler-readiness-plan.md:278 says this blocks the proof model; his ruling was "a minimal proof set for the MVP" (:279); want:185 and want:10 measure against the business problems intended and a stated ideal. Item 4 (:107) forces a step or a refusal for every form, so most of the language would be refused by default. Expected results "come from your want document only" (:228), but want:112–127 covers only the bank example. Fix: bring the boundary forward as a prepared question by week 3 with a draft refused list grouped by area; mark expected results for other examples as agent-derived from canon and send unanswered ones through the question gate.

**Q2 on editable writes is framed to one answer when four options were prepared — serious.** :32 cites only want:143. what-discharges-an-editable-write.md § 3 is headed "Your own document does not settle this, in either direction" (:36), with four options (:110, :168, :201, :215). compiler-readiness-plan.md:282 says want:143 and want:171/:199 "pull opposite ways"; trace-walk-rulings.md:42–46 records that he declined to settle it in passing. Fix: put the prepared write-up to him as it is, with each option's cost as what stops compiling; remove the want:143 reference from the question.

**Q8 offers to fence the overflow and integer design out of the finished design — serious.** Canon: in scope, build deferred by ordering (spec:104, :110, :112, :272). want:104 and :165 make overflow a fault family under the same treatment. The walk exemption (compiler-readiness-plan.md:1599) is from the MVP build, which is a build milestone (trace-walk-rulings.md:71–78). Offering "design waits" is the narrowing he rejected (project_design_first_restart_2026_11_13.md:17). Fix: treat the overflow model as part of the design; ask only the fixed-width-or-arbitrary-precision choice, prepared and paired with the divisor question.

**Deduplication is treated as needing no judgement, but several duplicates disagree on substance — serious.** :131 ("none of your input and little judgement"), :161 ("mechanical output"), :151 ("most resolve by naming an owner"). Four of fifteen scripted duplicates are substantive (compiler-readiness-plan.md:1096–1100); Restore recompute-or-stale (:1065); result-types.md:148/:152 encode the narrow reading of Q2 yet result types go to immediate deduplication (:132). An agent with only excerpts settles design questions by picking an owner. Fix: split agreeing copies (mechanical) from disagreeing ones (decided against want document and spec by a fresh checker, else the question gate); give deduplication agents the want document and owning spec section; hold anything touching Update, Restore or constraints.

**Prose restatement is left out of deduplication, contrary to rule 3 — serious.** :88 names the induction paragraph in three documents; :112 limits the ownership table to records, enums, counts, orderings and catalog-keyed tables; :118 says no check reads for prose. Restated rules (induction, ingress, operation order, the dead-end definition at graph-analyzer.md:457, the writer list) get no owner. The plan states the gap openly, but a finite fix exists. Fix: enumerate normative concepts from the spec's sections and glossary, assign owners, search each concept's terms across canon, and turn hits into pointers.

**The three-round cap goes against the measured record and pushes failures onto him as narrowing — serious.** :116, :141, :204; twelve rounds per argument that held (feedback_soundness_arguments_need_adversarial_rounds.md:13); the plan's own risk note (:217) expects all three rounds used. Fix: per-argument caps on the measured record; stop when open refutations rise rather than on a round number; present more rounds, a revised argument or narrowing neutrally with the counterexamples. (The convergence angle rated the same problem blocking; see the summary.)

**The reading and decisions put on him are larger than the summary says, and the counts disagree — serious.** :27 promises one page a week and two longer reads; :122 adds signing the defaults list; :167 attaches an example and compiler output to up to five questions a week; :235–236 makes the first Friday one page plus three. Only Q2 has a prepared write-up (compiler-readiness-plan.md:280–282); Q3, Q4, Q5 need § 4-standard preparation (:342–346) and none is scheduled. :193 "Three need you now" against four marked **Yours** (:178, :183, :185, :189). Fix: one honest total of questions, pages and sign-offs; schedule preparation before asking; reconcile the counts. (The "four findings" heading at :187 was refuted: the ensure bullet merges two register findings.)

**Q3 on time-referencing rules does not show that either option breaks something he approved — minor.** Option one weakens what a rule guarantees (want:173, :189); option two overrides philosophy.md:28 (approved, trace-walk-rulings.md:14–15) and temporal-type-system.md:568, which CLAUDE.md's Tier 3 requires to be framed as an override. :185 carries some of this. Fix: say both plainly, with a worked example each.

**Q4 on dead ends is either already answered or misstated — minor.** philosophy.md:44 includes "Required states lie on every path to completion"; graph-analyzer.md:455–457 defines the dead end by reaching a terminal state; want:152 may settle it under rule 6 (:208); the no-terminal-state case (graph-analyzer.md:447–451) is missing. Fix: fresh agent decides whether want:152 answers it; if not, present both readings with philosophy.md:44 in full and the no-terminal-state case.

**Q7 misses the second unguarded optional read — minor.** want:80–81 interpolates `{MonthlyRepayment}` unguarded; spec:1561 refuses it; project_reset_phase_1_2_2026_07_28.md:50 already names both sites. Fix: verify the canon line, name both sites, show corrected lines and any mutation row (want:122) whose wording changes.

**The "probably answered" reading on ensure covers only half the finding — minor.** compiler-readiness-plan.md:1052–1055 has two parts: an event ensure evaluated at ingress against want:143, and ensure's absence from the want document. :190 addresses only the second. Fix: split into two items and keep the ingress half open for the fresh check.

**The contents list omits things the want document requires — minor (downgraded from serious).** :106 omits the computed closing guard (want:179), runtime checks derived from the premise list (want:210), inspect provenance (want:211), catalog-driven coverage, and the bounded counterexample search. The skeptic noted the week-1 want map (:104) must reach zero gaps, so these would come up early, and the counterexample-search row carries no verbatim owner quotation (compiler-readiness-plan.md:234, :244–245). Fix: build the contents list from the want map and add these items.

**Internal and non-proof unwritten items have no step, and the hand walk uses one example — minor.** extension.md:7 is a stub; the five uncatalogued writers (spec near :1999) and the single home for structural rules (:75) have no step; step 5 (:142) carries only the proof model; the walk (:114) uses one example. Fix: add a step for these; run the walk over at least one example per type family.

### Refuted findings from this angle

None refuted in full. Part of one was: the "four findings" count at :187 is correct, because the ensure bullet merges two register findings.

---

## Angle 2: the plan's factual claims

Overall: about 40 claims checked; most hold (the 36-document, 30,154-line inventory; 235 declared types; 20 differing types; 147 codes, 109 unnamed; philosophy.md:3, :28; temporal-type-system.md:568; the spec, want-document and old-plan citations listed in the reviewer's summary). Two errors matter.

### Surviving findings

**The owner's example has a second unguarded optional read — serious.** want:35 declares `MonthlyRepayment` optional; Q7 names only the :75 guard; :80–81 interpolates it in a reject message without a presence test; spec:1561 refuses that. As written the owner would approve a correction that leaves the example refused, and the mutation table (want:114–127) assumes the unmutated example is accepted. Fix: have a fresh agent read the example against the spec for every optional read (guards, set right-hand sides, message holes) and bring all corrections in one item.

**The runtime open-questions index has 21 rows, not seven — minor (downgraded).** docs/runtime/README.md:34–54 holds 21 rows; the compiler's 11 is right. Item 5 already says "every row", so no row drops from scope, but the workload is understated by 14 and some rows (certificate gate, route into `Precept.From`) sit inside the proof model. Fix: restate the counts and say which runtime rows get a default and which go to the owner.

**The git shortstat command does not reproduce, and phase-2 records are ~17,750 lines — minor.** As quoted (:206), `git diff --shortstat b93d1db1 HEAD` gives 87 files, +21,087/−1,104. Filtered to `docs/working-reset/phase-2` it gives 55 files, +17,756; the folder's markdown is 17,749 lines. Filtered to canon the reviewer got 23 files +928/−956 and the skeptic 21 files +924/−952, so the filter must be stated. Fix: quote the command with its path filter and the reproduced figures; correct the record figure.

**"145 applied corrections" overstates what phase 2 executed — minor.** compiler-readiness-plan.md:194: 133 carry a commit, 12 state a reason. Rows N105–N119 (contradiction-register.md:529–543) are resolved but unexecuted; N105, N106, N107 and N117 are substantive (register :707). Fix: say 133 applied and 12 dispositioned; name N105–N119 as unexecuted and put the four substantive ones with the facts the proof model will change.

**The tally of owner questions does not match the table — minor.** :193 "Three need you now" against four marked **Yours**; `from any` is a default (:182) though its row says "you see it", against :169. Fix: recount; decide whether `from any` is a default or a question; make the tally, summary and § 3.5 agree.

**The philosophy's position on a two-state cycle is less clear than claimed — minor.** The plan omits graph-analyzer.md:457 (exit-only rationale beside an algorithm that rejects the cycle) and compiler-and-runtime-design.md:147. The skeptic found philosophy.md:44's wording leans toward the exit-only reading, so the main problem is the missing citations. Fix: cite want:152, compiler-and-runtime-design.md:147, graph-analyzer.md:439–457, and philosophy.md:44.

**Term counts are tied to an unnamed set of nine documents — minor.** :73 never names them; whole-canon counts are 19, 15 and 99; "certificate" in soundness-and-coverage.md is 24 lines, 25 occurrences. Fix: name the nine or give whole-canon counts with the method.

**The description of the four check scripts is wrong in two places — minor.** code-stage-attribution.py reads diagnostic-system.md (:13) plus a fixed stage map (:15–27) and searches full prose (:44–56); stated-counts.py scans every prose line (:104). :113 says the checks read fenced code and nearby numbers only. Fix: describe each script's inputs; note two already read prose.

**The spec's proof section and proof-engine.md are misdescribed — minor.** spec:2177–2197 has real content (PRE0115 construction exemption, meaning of a proved qualifier, implemented checks), not just pointers; proof-engine.md:8 says the prove-or-reject MVP is "designed, not yet implemented", not "as built". This matters for the paragraph-by-paragraph split in step 5. Fix: describe both accurately.

**The placeholder list in soundness-and-coverage.md is incomplete — minor.** Misses :66; :155 reads "fill when scheduled"; the coverage tables also list containment obligations (:250–254). The completeness search would still catch :66. Fix: add :66 and quote :155 accurately.

### Refuted findings from this angle

None.

---

## Angle 3: convergence

Overall: the plan diagnoses phase 2's loop correctly, but several stop rules depend on judgements by the agents being stopped or have no stated consequence; its round cap and estimate sit below the project's own record; it depends on lists that do not exist; and it has no rule for reopening the model or settling disputes. The likeliest next loop: the round cap fires around week 5 with a narrowing question, or the hand walk in weeks 9–11 repeats phase 2's cold read after he first sees the refused list.

### Surviving findings

**The three-round cap is set below the measured record, so the stop rule will most likely fire, and the burden lands on him — blocking.** :19, :116, :141, :204. Twelve rounds per argument that held, zero-round arguments all fell (feedback_soundness_arguments_need_adversarial_rounds.md:13). The old plan priced the discharge procedure and the arguments separately (compiler-readiness-plan.md:1768, :1841–1845); the establishment-and-preservation design drew a new objection on each of three passes (:497–499). "New kind of flaw" is an agent's tag, the same shape as the attempts column that stayed at 1 (:194; project_reset_phase_1_2_2026_07_28.md:68–71). Narrowing is also language-surface work under CLAUDE.md's Tier 3 and `/design`. Fix: attack each argument on the contents list separately with up to about twelve rounds; write the classification of flaws before round one; a fresh non-author classifies; per-argument counts in a data file the script reads; a failing argument goes to a second author before reaching him, with the `/design` and Tier 3 cost stated.

**The language lists that items 3–4 and the example set depend on do not exist — serious.** :152 vs compiler-readiness-plan.md:197 ("Not started", 0 attempts); phase-3/ holds method write-ups and scripts, no data; the old plan sized it at one to two weeks (:1429–1431); the write-site list is still open (spec:1999). Fix: a sized step extracting lists from `docs/language/`, closed with a recorded hash, with the write-site list treated as design; schedule before the example set and contents list; add the time.

**The "beside" rule marks the model's reach before the model exists, and the "safe now" list holds facts the model will change — serious.** Reach marked days 1–2 (:226), contents list days 2–3 (:229), hand-working days 3–5 (:230). result-types.md:119 justifies no `EventOutcome.ConstraintsFailed` by the induction; :148/:152 encode Q2; compiler/README.md:61 leaves the checker's stage placement open. Fix: derive the reach from the week-1 hand-working; freeze result types, stage order and records in open questions; if the model must change a deduplicated fact, it edits the one owning statement and logs why.

**No one is named to decide disputes — serious.** :104, :115, :162, :131 against four-of-fifteen substantive (project_reset_phase_1_2_2026_07_28.md:94–97); the main session may not analyse (:159). Fix: a fresh-context adjudicator per dispute with a written precedence rule (canon section over restating site; want document over canon; otherwise a refuse-more default), rulings logged at path:line; name who empties the queue; say which member-list conflicts are design questions.

**No rule for reopening the proof model after it passes — serious.** Step 5 and the walk (:141–143) will find gaps; the old hash-and-halt rule (compiler-readiness-plan.md:486–492, :509–523) was dropped; parked runtime questions already bear on the model (project_reset_phase_1_2_2026_07_28.md:256–260); the last cold read found 19 of 28 items in no register row (compiler-readiness-plan.md:66–68). Fix: hash on passing step 4; rerun attacks on changed arguments only; count changes weekly; pilot the walk in weeks 1–2; fixed repair budget and stop rule for step 6.

**The weekly-total and records rules have no force — serious.** One combined total (:205) lets the mechanical track hide a stall; exception files are self-approved (compiler-readiness-plan.md:970–974); about a dozen side records beside "one append-only log" (:206); the new plan has no fixed path and MEMORY.md:3 still names the old one, and two live plans was the July root cause (project_planning_loop_diagnosis_2026_07_01.md:16). The skeptic noted the plan does mark the old plan superseded (:149, :225). Fix: two tracks with separate stop conditions and named next actions; exceptions approved by another agent; ownership recorded in canon and regenerated by script; fixed plan path and memory pointer updated in the same commit.

**The 8–12 week estimate has no measured basis, depends on his availability, and has no time-based stop — serious.** No rate supports steps 2, 3, 5; phase 2 ran four times over (compiler-readiness-plan.md:1281–1302); commits since 2026-07-26 fell on 07-26 to 07-29, 08-11 and 08-26 to 08-28; Q2 and Q5 block parts of the model and cannot be defaulted (:169, :216); "first real evidence at week 4" (:217) has no criterion. Fix: separate agent effort from owner waiting; measure a rate in week 1; week-4 checkpoint with written pass criteria and a stated action; proceed on both answers to Q2 and Q5 in parallel.

**He reads the refused list only at the very end — serious.** :21, :27, :122, :143, :213; refuse-by-default (:36, :169) plus the smallest proof set (:175); want:189 accepts narrowing without saying how far. An objection in week 11 reworks steps 2–5. Fix: a draft refused list from the week-1 hand-working on the first Friday, updated after step 4, each item with an example and the author-facing message.

**The single author is a series of sessions with no hand-over, and the reading list does not cover the output — serious.** Step 2 runs three weeks (:18, :139); :250 admits the reading list may not fit one session; the list (:160) omits proof-engine.md (thirteen obligation kinds) and catalog-system.md (named at :75); "when a case needs them" does not meet item 4 (:107). Fix: write drafts straight into the target canon sections with a hand-over note; add those documents; drive type-document reading from the coverage table; name the phase-5 files.

**The summary claims each fact is stated once, but no step removes duplicated prose — serious.** Same as the owner-rules finding; :12 overstates what the scripts check. Fix: a fixed prose check driven by the ownership table and the paragraphs named in § 1.3; reword summary item 2.

**Decisions and defaults are not routed through CLAUDE.md's rationale and `/design` rules — serious.** No mention of rationale, alternatives, precedent, tradeoff or `/design`; defaults are only an overturnable list (:169). The plan does cite the locked text for Q3 and Q4, though not word for word. Fix: each decision and default carries the four parts; narrowing from the stop rule goes through `/design`; quote locked text for Q3 and Q4.

**Question 8 offers to narrow the design — minor (downgraded).** Q8 is unneeded (compiler-readiness-plan.md:425–427). The second half of the finding, that rule 6 could treat Q2 as settled, was refuted: the plan already treats Q2 as open and his (:32, :178).

### Refuted findings from this angle

- *Moving "today's build" text out of proof-engine.md keeps two accounts in canon* — refuted: proof-engine.md already has § 13 with an Implementation Status subsection (:2523–2525) and :2614 marks build gaps as gaps against the target; CLAUDE.md routes stage build state to such a section, so this is the project's convention, not the relabelling he rejected.

---

## Angle 4: completeness of the definition of finished

Overall: the three tests are right and the proof model belongs at the centre, but "finished" does not cover canon as it stands.

### Surviving findings

**The coverage check depends on lists that do not exist — serious.** compiler-readiness-plan.md:195; the only data file under docs/working-reset is phase-1/decision-register.json; the generated, extracted or hand-kept choice is undecided (:1371–1386). Fix: a step-1 task extracting lists of expression forms, actions, writers, constraint kinds, types, operations and expression positions from canonical prose; record the arrangement; correct :152.

**The coverage lists leave out where an expression can sit — serious.** soundness-and-coverage.md:244–249 makes the creation site a separate dimension; :256–263 (guards, member-call arguments, field and event-argument defaults, lookup keys, field-reference bounds, message holes) are all not yet enrolled. Fix: add expression positions from spec § 2 and soundness § 3.2; generate the table over form × position.

**The open-question inventory misses most of canon's open questions — serious.** Missed: collection-types.md:1294–1300; language-server.md:1575; tooling-surface.md:217, :439; catalog-system.md:1509; the empty TBD sections at temporal-type-system.md:1935 and business-domain-types.md:2085. The skeptic noted type-checker.md:1079's W2 and G1–G3 are implementation items. Fix: build the inventory from every open-questions heading and pending marker; widen the search phrases; treat an undefined item named in an open-questions section as a failure.

**Runtime questions are miscounted, and the default rule cannot cover most of them — serious.** 21 rows (runtime/README.md:34–54); :169 cannot apply to Version representation, PreceptValue layout, opcode semantics, stack against registers, opcode representation or slot ownership; the question budget and :193 were computed against a third of them; no step designs answers. Fix: correct the count; a step where agents draft each internals answer with rationale and rejected alternatives, recorded as an overturnable default; a separate default rule for internals; recompute his load.

**Deferrals keyed to later in the build now fall inside finished — serious.** Fault Q3 (runtime/README.md:36) lives only in the frozen old plan (compiler-readiness-plan.md:443–445; :155); Q8 frames overflow as integer only against spec:272 and primitive-types.md:697–701. The skeptic corrected: migration is consistently deferred (compiler-and-runtime-design.md:718; runtime-api.md:277), not contradictory; FunctionArgConstraintViolation and D5 are already in the compiler index; the roadmap's "Compiler 1.1" is at graph-analyzer-roadmap.md:18 and :63. Fix: list and classify every deferral (designed now; ruled out of v1 by him, quoted; or build-order only, design still owed); widen Q8 to all three types.

**Unfinished documents outside the proof model have no step — serious.** extension.md:7 stub; language-server.md:1575 design-only; graph-analyzer-roadmap.md's ten modifiers. The skeptic corrected: spec § 4 is a pointer to graph-analyzer.md, which is what deduplication wants, and literal-system.md and fault-system.md's open items are already in the indexes. Fix: one row per unfinished document with a finish condition; treat preview/inspect as design work.

**Canon names source files as the owner of several lists — serious.** soundness-and-coverage.md:200–203, :214–217; tooling-surface.md:1251–1261; primitive-types.md:707 and the other type documents. Fix: an ownership-table column for lists canon hands to code; state each in full in canon and reverse the pointers.

**Real contradictions are treated as deduplication — serious.** compiler-readiness-plan.md:1096–1100, :1065, :1066. The plan does hold constraint descriptors and proof records back (:132), but result types are not held back and nothing says how a disagreement is decided. Fix: sort each fact into restatement or disagreement; decide disagreements against the want document and philosophy with a citation, else a question; never settle one by choosing the owning document.

**Create, Restore and inspection are missing from the hand walk and partly from the contents list — serious.** :114 runs only Fire and Update; spec:268, :2183–2185, :2165–2167; runtime-api.md:277; compiler-readiness-plan.md:1066. The contents list does name "rules at creation" and the induction (:106), so the base case is partly present. Fix: add Create, Restore, InspectFire and InspectUpdate to the walk; add the construction exemption, what restore assumes and recomputes, and proofs under absent inputs to the contents list.

**The language documents get no internal-contradiction check — minor (downgraded).** The old cold read skipped them because phase 3 would reread them (compiler-readiness-plan.md:372), and the plan moves phase 3 later. The declared-types script already covers docs/language and the plan states its prose limit openly. What remains is a list-driven prose comparison, which depends on the lists. Fix: for each member of each language list, compare every canonical statement of its rules.

**The contents list omits parts the soundness document assigns to it — minor.** The rails (soundness-and-coverage.md:334–337), admissibility limits (spec:225; soundness :273–279), the second write of an `into` target (spec:1997); soundness § 6 (:319–325) ties completion to code fixes, against design-first. Fix: add the items; rewrite § 6 as a design statement.

**Documents restating the proof model are missing from step 5 and the inventory — minor.** compiler-and-runtime-design.md § 1, spec § 0.6–0.7, glossary.md, agent-onboarding.md, docs/README.md (status taxonomy, :24–44), CLAUDE.md; docs/future/reasoning-ingress-vision.md is not mentioned. Fix: add them to step 5 or to deduplication scope as pointer sites; record the future/ document as excluded.

**No status value means finished — minor.** The taxonomy mixes document completeness with build state (docs/README.md:28–37); the check at :113 only compares README columns with each document's own status. Fix: define a finished-design status separate from build state and require it everywhere.

### Refuted findings from this angle

None in full. Parts corrected by the skeptic are noted above (migration, D5 and FunctionArgConstraintViolation, spec § 4, literal-system and fault-system, type-checker W2/G1–G3).

---

## Angle 5: the questions put to the owner

Overall: of the nine questions, Q8 is already answered, Q7 is a consequence of a locked ruling, Q6 re-asks a policy he set, and Q2 may be settled by his own approved example. The default rule contradicts itself and is applied to an item he reserved. Q3 and Q4 misstate or omit locked text. Running the fresh-agent filter before the batch and preparing survivors to the old § 4 standard fixes most of this.

### Surviving findings

**The starting batch reaches him on day 1, before the plan's filters and preparation — serious.** :225 vs :167, :229; the § 4 preparation standard (compiler-readiness-plan.md:341–345); all ten items re-asked as worked cases came back with different answers (the-walk.md:47–61). Fix: move to end of week 1; fresh check for "already answered" and "a design question at all"; prepare survivors; list dropped items in one line with the settling citation.

**The default rule contradicts itself, and the listed defaults break it — serious.** :169's two tests conflict; four restriction defaults refuse what authors could write (:176–181); `from any` = yes refuses less (spec:2203). Fix: one test (refuses more, widenable without undoing, touches no locked decision or reserved item); drop the contradictory clause; recheck each row, which removes `from any` and naming.

**The naming question is defaulted though he reserved it, and the default is never stated — serious.** the-walk.md:201–202, :207; compiler-readiness-plan.md:280; must-a-proof-name-what-holds-a-fact-up.md:106–114; both options sound, so neither refuses more. Fix: make it a prepared question pointing at the six-minute write-up, with the recommendation stated as his choice.

**"Your example does not need it" is false for the earlier-row question once the example is corrected — serious.** want:80–81, spec:1561, the locked presence ruling; with row 1 corrected, only earlier rows not matching proves the field set at row 3. Fix: settle Q7 first, work the corrected `ReduceLimit`, then either ask with this case or state the forced rewrite with `if MonthlyRepayment is set then … else …`.

**The editable-write question is a yes to one option, omits text that may settle it, and relies on stale write-ups — serious.** :32; compiler-readiness-plan.md:283; want:36, :40, :98, :110; what-discharges-an-editable-write.md:37–42; editable-write-cascade.md:116–122; project_reset_phase_1_2_2026_07_28.md:21–26; philosophy.md:22, :42; :155. Fix: fresh recheck against the approved want document, leading with the finding that only the want:143 reading accepts his example; if still asked, all options with what each refuses and the quotes; mark the stale passages superseded first.

**The clock question offers an override of locked text without saying so and hides the other option's cost — serious.** philosophy.md:3, :18, :28, :42; temporal-type-system.md:568, :577; he said it is undecided (commit 1969e3e5). Fix: quote verbatim, label the override, state per option what an author writes and what is refused, and raise the "at every moment" clash as a philosophy question.

**Question 8 is already answered; the open question is the integer arithmetic model — serious.** spec:110; trace-walk-rulings.md:71–77; feedback_no_interim_pre_runtime_staging.md; the-walk.md:420–421; where-a-divisors-non-zeroness-comes-from.md:136–141. Fix: drop the scope question with a one-line citation; ask the model choice, prepared, before or with the divisor question.

**The dead-end question misstates the philosophy and drops a definition — minor (downgraded).** philosophy.md:44; graph-analyzer.md:451, :457; spec:186; compiler-readiness-plan.md:1046–1051. Fix: quote want:152, philosophy.md:38 and :44, graph-analyzer.md:451/:457, compiler-and-runtime-design.md:147 and spec:186; do not say what the philosophy decides; show the cycle and the all-rows-reject state as worked cases.

**The plan takes the MVP's minimal proof set as the design's scope without asking — minor (downgraded).** :175 measures against one example, against want:185 and feedback_no_interim_pre_runtime_staging.md:12; but the plan does give him the edge at the refused-list read, and he did rule "a minimal proof set for the MVP" (compiler-readiness-plan.md:279). Fix: ask plainly whether the design states the MVP's proof power or the full intended power with the MVP as a build subset; measure any default against want:185 and the language lists.

**The computed-field item is "probably answered" from lines the walk found do not settle it — minor (downgraded).** the-walk.md:719–731; compiler-readiness-plan.md:284. The plan already routes it through a fresh check. Fix: brief that checker with the walk record and the unwritten argument; if the check produces the argument, it goes through attack rounds.

**`from any` is defaulted against the spec's own statement and may not be his question — minor.** spec:2203, :968, :1071. Fix: fresh check whether it changes any observable outcome; if not, a proof-design choice with a soundness argument covering `from any` and `in any`; if so, a worked case with no default.

**Q7 misses the second unguarded read and is a consequence of a locked ruling — minor.** want:75, :81; spec:1561; project_presence_tolerant_rendering_2026_07_24.md; feedback_dont_resurface_ruled.md. Fix: a one-line notice naming both lines, corrected text on the Friday page, noting the earlier-row interaction.

**Q6 re-asks what his ruling and standing correction settle — minor.** :169 quoting compiler-readiness-plan.md:279; feedback_dont_resurface_ruled.md. Fix: drop it; one line saying the default follows the 2026-07-26 ruling and every default is listed weekly for overturning.

**The ensure item covers only half the finding — minor.** philosophy.md:20 answers only absence; the ingress placement against want:143 and evaluator.md:1369 (compiler-readiness-plan.md:1052–1053) are dropped. Fix: split into two items and send the fresh check the relational-ensure case and evaluator.md:1369.

**The state-action question omits canon text and guards — minor.** spec:1024 (`when` guards on state actions), :1082 (omit reset on self-transitions), :2092 (construction as entry into the initial state). Fix: quote them with :2016; show a state with two guarded entry actions and a self-transition under each answer.

**Q1 does not show which walk-item-1 rulings it drops, and the tally is wrong — minor.** the-walk.md:91–100 (thirteen phases, phase-8 confirm/narrow/revert, branch promote or revert, three-attempt rule, bounded counterexample search); the search is not in :106; :193 says three where four are his. Fix: list each walk-item-1 ruling as kept, replaced or dropped; add the counterexample search to the contents list; correct the count.

### Refuted findings from this angle

- *Missing question: his "defer until we build the runtime" ruling on who produces a `Fault`* — refuted: item 5 accepts "your word that it waits" (:108), he gave exactly that on 2026-08-11 and it is recorded as a named exception (compiler-readiness-plan.md:273, :374); re-asking would re-raise a made ruling. At most the plan should list it as an inherited named exception. (The completeness angle's separate point, that this exception lives only in a file the plan freezes, still stands.)
