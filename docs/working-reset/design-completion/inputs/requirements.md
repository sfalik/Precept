# Requirements for the next design-completion plan

Written 2026-11-22 against `spike/Precept-V2-Radical-reset` at `2f126a3b`. Nothing in the repository was
edited. This file says what the next plan must contain. It does not write the plan.

## Short names

| Short name | File |
|---|---|
| HF | `scratchpad/v3/history-failures.md` (the 22 recurring failures) |
| HR | `scratchpad/v3/history-rulings.md` (rows 1-121 are his rulings; § 9 is agent labels; § 10 is questions he declined) |
| TPM | `scratchpad/v3/technical-proof-model.md` |
| TP | `scratchpad/v3/technical-precedent.md` |
| TC | `scratchpad/v3/technical-canon-consistency.md` |
| v2 | `scratchpad/design-completion-plan-v2.md` (the current plan) |
| old | `docs/working-reset/compiler-readiness-plan.md` (the thirteen-phase plan) |
| rev | `scratchpad/plan-vs-old-review.md` (the adversarial comparison) |
| want | `docs/Working/what-i-want-2026-07-16.md` |

`scratchpad/` meant the session's temporary folder when this was written; the files it names are now under `docs/working-reset/design-completion/` (`inputs/` and `lineage/`).

## The one idea every requirement below follows

HF's closing analysis (HF:603-615) found that every earlier fix was a rule in a document, enforced by the same
kind of agent it was meant to restrain, and that the only fixes that held were checked by a machine or by
an outside party: twelve attack rounds (HF:368-371), commit hashes checked against commits (HF:180-183), and
scripts that compared declared types across documents (HF:609). So each requirement here is written as a
mechanism: a file in the repository with a fixed form, a script that reads it and prints a count, and a
point where that count stops work or allows it. A requirement that can only be satisfied by an agent saying
it did the thing is not acceptable and is marked as such where it cannot be avoided.

One requirement comes from his request for this workflow, relayed with this task: *"the new workflow should
include a technical track, that looks at what I want from a technical perspective"*. Section 3 is that
track. The plan must carry it as a standing line of work with its own files, not as a one-off read: it owns
the full contents list of the design, the hard problems, the dependency order the steps follow, and the
technical half of every question put to him.

---

## Section 1. The 22 recurring failures

For each: whether the old plan guards against it, whether v2 does, and the mechanism the new plan must
contain.

### F1. The unfinished part of the design was never the part being worked on (HF:67-99)

- **Old plan.** Does not guard; it causes it. The proof model sits at phases 4 to 7 behind a reconciliation
  phase and a numbering phase (old:195-198; HF:89-91). Phase 2 ran a month and never touched it (HF:91).
- **v2.** Partly. Proof model runs beside deduplication rather than after it (v2:172-177), but it still
  waits behind step 0's tool building (v2:19, :497) and all of step 1, including a contents list fixed before
  any proof text exists (v2:193-194; rev:158).
- **Must contain.**
  1. The proof-model line is the first line started after the smallest set of scripts it needs. Its first
     piece is TPM Part 4 step 1 (what an operation is), which needs nothing earlier (TPM:420).
  2. The start script for every other line refuses to start a run while the proof-model line has fewer
     committed canon changes in its target sections than a fixed number set in the settings file (for
     example, its first section). The count is printed from `git diff --stat` over the named sections.
  3. The status script prints, per run, lines changed in the proof-model target sections of canon against
     lines changed everywhere else. This is reported every run; it does not stop work by itself.

### F2. Plans about the work replaced the work, each a new copy (HF:101-130)

- **Old plan.** On paper: "no status page beside it and no second plan" (old:5-6), earlier plans off limits
  (old:479-480). It still grew to 2,278 lines (HF:123-124).
- **v2.** One page at a fixed path with the old plan marked superseded in the same commit (v2:282). But v2
  itself is 819 lines with a change list that governs itself in layers (v2:767, :772, :801; rev:42, :152),
  and it lives only in `/tmp` with two backups (HF:107-110).
- **Must contain.**
  1. One plan file at a fixed repository path, written as clean current text with no change list. The
     commit that adds it also marks the old plan superseded at its head.
  2. A check script that finds exactly one file under `docs/` and `docs/working-reset/` whose status line
     says it is the live plan. More than one fails the check.
  3. The plan holds no counts and no state. All state is in the generated status file (F14). Any edit to
     the plan file after it is adopted requires a row in the answers file (F6) carrying his words that
     authorise it; a script compares the plan file's commit history against those rows and prints edits with
     no row.

### F3. Finish lines that were searches with no end (HF:132-155)

- **Old plan.** "What a finish line has to be" (old:387-395) made conditions 1-3 and 6 checkable; condition
  4, "a reader … produces no disagreement … not already a row" (old:993-996), stayed a search and was
  widened (HF:146-151).
- **v2.** "Every check walks a list named in advance" (v2:268). But step 1 finishes on two derivations
  that must "agree" under canon that does not yet state the mechanism (v2:194; rev:45), and an argument
  passes when "the last two rounds found nothing" with "finding" undefined (v2:252; rev:146).
- **Must contain.**
  1. Every finish condition in the plan names a list file (with its `sha256sum` recorded when the list is
     closed) and a script that counts its open members. A plan-lint script reads the plan's finish-condition
     table and fails on any condition that names no list file and no script.
  2. "A fresh look finds nothing" is not allowed as a finish condition anywhere. A fresh read is allowed as
     an input that adds rows to a list; the rows it adds are worked like any others, and the read itself does
     not hold a step open.
  3. Findings outside the step's list go to the parked-work file (F13) and do not extend the step.
  4. Step 1's deliverable becomes the list of places canon is silent or contradicts itself on the bank
     example (rev:45). The technical track has already produced most of this list (TPM Part 5, TC § 2-3).

### F4. Decided but never carried into canon, then re-derived (HF:157-183)

- **Old plan.** Phase 2's register required a commit hash per row and a grep pattern (old:970-978); this
  partly worked (HF:180-183). Rulings given at the walk went into "Work the walk created" (old:291-340) and
  were routed to later phases, not to canon edits (HR:262-263).
- **v2.** Builds a rulings table (v2:662) and reports "rulings not yet in canon" once (v2:671); no finish
  condition drives it to zero (rev:31).
- **Must contain.**
  1. A rulings file in the repository, seeded from HR rows 1-121, one row each, with: his words; where
     recorded; and exactly one of (a) the canonical file and line that states it with a short quoted fragment
     of the canonical text, (b) for a process ruling, the plan mechanism or `CLAUDE.md` line that carries it,
     (c) "superseded by row N", or (d) "no longer applies" with the reason.
  2. A script counts rows with none of (a)-(d), and for every (a) row re-reads the cited line at HEAD and
     confirms the fragment is there. A failing fragment is not counted as a regression automatically: an
     agent reads the line and records whether the ruling was undone or the sentence was reworded (old:978-994
     is the precedent: two patterns broke under correct later edits).
  3. The script runs after every run that changes canon. The final step cannot finish while the count of
     rows with none of (a)-(d) is above zero.
  4. Section 2 below gives each row's route.

### F5. Agents manufactured owner decisions (HF:185-213)

- **Old plan.** § 3 "Ruled — do not reopen" (old:224-251) and the preparation rule (old:342-352). Phase 1
  sampled ten filtered items at random into the sitting to measure the filter (old:816-823). Each fix was
  followed by a recurrence (HF:212-213).
- **v2.** Every candidate question goes to a fresh agent first (v2:308, :663), with the filter that options
  differing only in proof power are design work (v2:447). Still: v2 asked two questions an agent could
  settle (rev:173-174), and its predecessor asked one he had answered (HF:202-204).
- **Must contain.**
  1. A question file reaches him only if two checker files exist for it, written by two agents who were
     given the rulings file, the walk file, `trace-walk-rulings.md` and `five-items-walk-2026-08-27.md` but
     not the proposer's conclusion, and both record that no text settles it. A script refuses to put a
     question into a batch without both files.
  2. A third check file per question records the question against all five of his 2026-11-13 rules at once
     (HF:613-615 names this as the step every rejected document skipped). Any rule it breaks drops the
     question.
  3. Every batch carries the list of dropped questions, one line each, with the settling citation.
  4. A random sample of dropped questions (the old plan's ten-of-the-filtered rule, old:816-823) is shown
     with each batch so the filter's error rate is measured. If more than one in the sample was wrongly
     dropped, the filter is re-run before he reads anything else.
  5. A fixed cap on questions per batch, set from the count of candidates at the end of step 1 (rev:41).

### F6. A ruling was widened past what he said (HF:215-235)

- **Old plan.** Nothing mechanical.
- **v2.** Nothing. The review notes v2 never says where his answers are written (rev:181).
- **Must contain.**
  1. An answers file: the question exactly as put to him, his reply verbatim, the date, written in the same
     sitting (old:862, "Record your marks as you give them").
  2. Every place a ruling is used — a canon edit, a plan rule, an adjudication — cites an answers-file row
     or a rulings-file row.
  3. A scope-check file per use, written by an agent that did not make the use, saying whether the use
     covers only what the quoted words cover. A script counts uses with no scope-check file and uses the
     check marked as wider than the words. Either count above zero stops the line that made the use.

### F7. Agent-written labels treated as his authority (HF:237-257)

- **Old plan.** Bans labels as evidence (old:481-482); phase 1's walk established provenance (HF:255-257).
  Its own § 3 rows carried no quotations (old:244-245).
- **v2.** Precedence order puts his quoted ruling first (v2:245).
- **Must contain.**
  1. The adjudicator's precedence order, as a file every adjudicator brief includes: his quoted words; the
     want document (which carries his approval in full, HR:47-49) and philosophy; the language documents; the
     compiler and runtime documents; executed register rows from phase 2; a restating site. HR § 9's eighteen
     labelled items are listed in the same file as "not his ruling".
  2. A lint over every new canon diff and every file sent to him for "locked", "ratified", "owner
     decision", "settled", "ruled"; each hit must cite an answers-file or rulings-file row, or the lint
     reports it.
  3. `docs/Working/decision-index.md` is marked as not authority, and named as off limits to question
     checkers (rev:77, :211).

### F8. The design narrowed, or its bar lowered, to make something finishable (HF:259-294)

- **Old plan.** Causes it: "Expect the first honest fragment to be … smaller" (old:278, :1645-1652),
  overflow "an authorized exemption for the MVP" (old:333, :1599), phase 8 "narrow" (old:1940; rev:14).
- **v2.** Records the full-proof-power answer (v2:314), forbids "not built yet" as a reason (v2:131), forbids
  refuse-more defaults (v2:276). Still lets "ruled out of the first version" back in (v2:133; rev:36), and
  lets a refusal cite the cost and "earns its place" tests with no bound (rev:37).
- **Must contain.**
  1. **No removed line without a destination.** A script lists every line removed from canon in each run and
     requires each to appear in a destination record (the owning statement it was merged into, the section
     it moved to, or a rejected-alternatives section). Removed lines with no destination stop the line
     (rev:33).
  2. **A closed list of reasons a design may refuse something**, and a refusal row that cites anything else
     fails a script: want:135 (no solver the compiler does not carry; non-linear rules closed only by a
     premise that states the condition); `precept-language-spec.md:225` tests 1 and 3 (checkable certificate,
     determinism); a mathematical impossibility with a source — TP:264 names exactly two, exact reachability
     over unbounded integers and general non-linear integer arithmetic, and in both the response is "prove
     from declared premises or reject"; a ruling of his with quotation. Test 2 (recompile cost) may be cited
     only with a written cost bound for the step, attacked with its argument. Test 4 ("earns its place") may
     be cited only with his word (rev:37).
  3. **Four groups for every deferral in canon** (rev:36): designed here; excluded on a named principle from
     item 2; a capability he deliberately left open, with his words; build order only, design owed here. "The
     first version" and "the MVP" are never a reason on their own; "MVP" is a build milestone by his
     confirmation (HR row 71).
  4. **Hard problems are named work that closes only with a design** (section 3.2 below). A hard problem
     cannot be closed by moving it into the refused list unless one of item 2's reasons applies.
  5. Canon's present text that states permanent limits (TC T6: `proof-engine.md:139`, `:2500`,
     `:2537-2541`, `:2597-2606`, `:2618`; `compiler-and-runtime-design.md:165`; `precept-language-spec.md:256`;
     `soundness-and-coverage.md:271-296`) is a named work item in section 3.3.

### F9. Build state, compiler behaviour, samples or catalogs used as evidence (HF:296-326)

- **Old plan.** States the evidence rules (old:465-482) and then puts build checks and compiler readings
  inside design phases (old:460-463; rev:14).
- **v2.** No code; lists from `docs/language/`, never catalogs (v2:125). But question packets get their
  "what it refuses" column from the `precept-author` agent, whose instructions are to compile on every
  revision (rev:39).
- **Must contain.**
  1. Workflow agents in design steps are given no compile or proof tools. The workflow scripts set each
     agent's tool list; a step-0 test confirms an agent cannot reach `precept_compile` or `precept_proofs`.
     The `precept-author` agent may be used for syntax only, and two other agents derive the accept and refuse
     column from canon (rev:39).
  2. A citation lint over every design output file: a citation into `src/`, `test/`, `samples/` or a
     catalog source file, or the words "compiles", "Proved", "the compiler accepts", is reported and the piece
     is redone. His 2026-08-28 words carry this: *"forget about the code for now, this is about getting the
     design right"* (HR row 79).
  3. The technical files already meet this (TPM:20, TC:244); the plan keeps that as the standard.

### F10. Claims about canon without reading it, or by searching only the tree (HF:328-350)

- **Old plan.** Nothing in the plan; memory files only.
- **v2.** Gives question checkers named sources (v2:663).
- **Must contain.**
  1. Every factual claim in a produced file carries `file:line` with a short quoted fragment. A citation
     script opens each cited line at HEAD (read with `git show HEAD:path`, not the working tree, so a
     session's own uncommitted edit cannot confirm itself) and reports any citation whose fragment is not
     there.
  2. Any claim that canon is silent records the search terms used and the output of a history search
     (`git log -S` over the canonical folders). A script rejects an absence claim missing either.

### F11. Soundness arguments called finished before anyone attacked them (HF:352-371)

- **Old plan.** Phase 6 conditions 3-5: two separate attacker sessions, each naming a concrete attempted
  counterexample, each with a recorded disposition (old:1801-1807); phase 5 condition 7-8, not signed in
  the session written, three recorded attacks on the induction argument (old:1716-1745).
- **v2.** Twelve rounds per argument, fresh attackers, flaw kinds written before round one, a classifier, a
  second author, never settled in the session that wrote it (v2:249-255). Weak where rev:29 and rev:63 say:
  attackers can pass an argument by reporting nothing, and proof text enters canon before its rounds.
- **Must contain.**
  1. Keep twelve rounds per argument with no early pass.
  2. Each attacker file contains at least one concrete attempted counterexample: a definition, a sequence of
     operations, and the claimed wrong outcome. A script rejects an attacker file without one, and the round
     is rerun rather than counted.
  3. A "finding" is one of the flaw kinds written down before round one (rev:146). Wording notes do not
     reset the count.
  4. Each repair is confirmed by a fresh agent that is neither author nor attacker (rev:29).
  5. Attackers are seeded with the known counterexamples (old:1679-1684; rev:75) and with TP's two
     limits that apply to every argument: non-inductive rules (TP:61-69) and rounding (TP:170-189).
  6. One fact-checker per round reads the argument's citations against canon (rev:78).
  7. Argument text sits in canon behind a fixed pending marker until it passes; a script fails any other
     canon section or argument that cites a section still carrying the marker (rev:63).

### F12. Stop rules written down and never fired (HF:373-398)

- **Old plan.** Three-attempt rule and the attempts column (old:495-503); never incremented (old:194;
  HF:379-381). The phase-4 stop (old:546-548).
- **v2.** Drops both and replaces them with per-argument budgets and "counts must fall" rules that fire on
  almost every run of a discovery step (rev:28, :44).
- **Must contain.**
  1. Every stop rule is computed by a script from the progress files and read by the start script, which
     refuses to start a run while a rule has fired. No stop rule depends on an agent noticing.
  2. Counts are split into "found, not yet worked" and "worked, then reopened"; only the second must fall
     (rev:44).
  3. A whole-model stop, by script: if a fixed number of arguments have gone to a second author, or two of
     the first five checkpoint arguments still have a refutation open at round three, the proof-model line
     halts and one packet goes to him asking whether the model can be written as it stands (rev:28).
  4. The old "different problem each attempt" test (old:495-503) as a script over the flaw classifier's
     output: three consecutive repairs of one argument each refuted on a different flaw kind halts that
     argument and sends it to the second author early.
  5. Cascading repairs: a repair that causes a refutation in an argument it rests on, or that rests on it,
     is counted, and two such in one section halt the section (old:1859-1861; rev:30).
  6. A fixed table of next actions for each stop rule, written into the plan, so the session never invents
     one (rev:44, :49).
  7. Step 0 shows each stop rule firing on a prepared input.

### F13. Findings recorded, then work carried on as if they had not been (HF:400-420)

- **Old plan.** "No phase closes with an open deferral that has no owner" (old:397-458), which forced the
  fault-backstop family through (HF:417-420).
- **v2.** "Parked unless a completeness list needs it" (v2:269), with no record and no owner (rev:46). The
  frozen set keeps some work off documents the proof model will change (v2:179).
- **Must contain.**
  1. A findings file where each finding names the sections and arguments that rest on the thing it found.
     The start script will not run a piece whose inputs include a section with an open finding against it.
  2. A dependency record per argument: which arguments and canon sections it rests on. When one changes, the
     arguments resting on it are reopened automatically (rev:30).
  3. A parked-work file and a handed-to-the-build file (rev:46), each row with an owner: a step whose scope
     reaches it, a quoted ruling, or a named exception. The final step cannot finish while any row lacks one.

### F14. Tracking pages and duplicated facts went stale (HF:422-443)

- **Old plan.** "A slice whose tracker row was not updated in the same commit is not finished" (old:15-18);
  it failed six times (HF:433).
- **v2.** Counts only from scripts; the status file is regenerated after every run (v2:490, :577). Pointer
  form and ownership table generated from canon (v2:153).
- **Must contain.**
  1. Keep v2's rule that no count is typed by an agent. The status file is generated entirely by a script;
     it has no hand-written text.
  2. For canon: his ruling "one full statement, deduplicate" (HR row 3) is carried as one fixed pointer form
     naming the owning document and section; a script resolves every pointer and finds every second full
     statement of a listed fact (v2:153-155).
  3. The memory index's pointer to the live plan is updated in the same step the new plan is adopted, and a
     script checks it (v2:282).

### F15. Decisions and work held where the next session cannot see them (HF:445-464)

- **Old plan.** The reset branch committed steadily (HF:462-464).
- **v2.** Progress files in the repository (v2:493); memory files copied in with their copy date (v2:491).
  But v2, the review, the two rejected analyses and these requirement files are all in `/tmp` (HF:458-460).
- **Must contain.**
  1. Before step 0, the inputs this plan rests on are committed under `docs/working-reset/`: the five v3
     files, this file, the review, and v2 marked as the superseded draft.
  2. A script checks that every path the plan or a progress file cites exists in the repository at HEAD, and
     fails on any `/tmp` path.
  3. The answers file (F6) is committed in the sitting his words are given.

### F16. Agent vocabulary and compressed questions (HF:466-486)

- **Old plan.** Questions prepared with options, cost as "what stops compiling", a worked example and the
  author-facing message (old:342-352).
- **v2.** Keeps that preparation (v2:664; rev:183).
- **Must contain.**
  1. Every workflow agent prompt contains the `CLAUDE.md` "How to write" section; a script reads each
     workflow script's prompt strings and fails one without it (CLAUDE.md requires this for hand-written
     prompts).
  2. A vocabulary lint over every canon diff and every file sent to him: the banned list in `CLAUDE.md`, plus
     register row identifiers, walk item numbers and `N`-numbers in anything sent to him (his words, *"those n
     numbers mean nothing to me"*, HR row 102). Hits are reported and the file is rewritten before it goes.
  3. Every question is a plain consequence with a worked case (HR row 103); one at a time in plain text, no
     modal prompts (HR row 104).

### F17. The main session acted as the authority (HF:488-504)

- **Old plan.** Nothing mechanical.
- **v2.** Replaces the orchestrating session with a starting session doing five fixed things (v2:467), and
  adjudicators started by script (v2:245). It still picks the next run from prose (rev:49).
- **Must contain.**
  1. A script prints the next run from the progress files; the session runs what it prints (rev:49).
  2. A cap on runs one session chains before it stops with a notice (rev:49 suggests five).
  3. Every commit that changes canon or a design file carries a trailer naming the workflow run and the
     output file it came from. A script lists canon commits without one. The main session writes no canon or
     design text.

### F18. Enumeration covered the cases tripped over, then overcorrected (HF:506-524)

- **Old plan.** The expression-form walk (old:239, :1563-1566), one case per form, and its warning against
  crossing independent properties (old:1653-1658).
- **v2.** Lists extracted from `docs/language/` with expression positions, checked by a second agent and
  hashed (v2:193); a coverage table generated from them (v2:131).
- **Must contain.**
  1. Each family is one closed list extracted from `docs/language/` and hashed. The coverage table has one
     row per member of each list. It is never the product of two lists; where a step splits into cases, the
     split is argued inside that step's argument.
  2. Lists are re-extracted or re-checked after every step that changes `docs/language/` (rev:73, :203).
  3. Merges are checked sentence by sentence, not by term grep, because paraphrases defeat greps (HF:516-518;
     rev:33).

### F19. An engineering wall read as a question about what Precept is (HF:526-541)

- **Old plan.** Nothing; Frank's one-page diagnosis rule (HF:538) was never in a plan.
- **v2.** A failing argument goes to a second author, then to him with three choices (v2:254-255).
- **Must contain.**
  1. For each hard problem in section 3.2 the first piece is a one-page engineering write-up: the problem,
     the candidate techniques from TPM Part 3 and TP §§ 3-5, and what each costs. The technical track already
     holds the candidates.
  2. A question about what Precept is, or how much it promises, is dropped by the five-rule check (F5 item 2):
     want:14, want:171 and his 2026-11-13 answer settle it (HR row 4).

### F20. Records about the work grew faster than the work (HF:543-559)

- **Old plan.** Nothing (HF:559).
- **v2.** Counts lines outside canon against lines changed in it (v2:282), but the rule fires on every run of
  a discovery step (rev:44).
- **Must contain.**
  1. Every record is a fixed-form data file (one per piece, v2:475-483), not a prose essay. A script fails a
     piece's record that exceeds a fixed size set in the settings file.
  2. One adversarial review per artifact. No review of a review. The plan names, per kind of work, the only
     record files it may produce.
  3. The outside-versus-inside line count is printed per step and reported to him only on steps that write
     canon (2, 3, 5).

### F21. Agent runs reported results nobody observed (HF:561-577)

- **Old plan.** Nothing in the plan; memory rules.
- **v2.** An agent that returns nothing has not done its piece (v2:489); counts by script (v2:490); copy
  literal script output into documents (v2:490).
- **Must contain.** Keep all of v2's rules. Add: every output file records the model the agent ran on and the
  run identifier; a script rejects output from a model other than the one the workflow script pinned for
  that role.

### F22. Each correction swung to its opposite (HF:579-597)

- **Old plan.** Nothing.
- **v2.** Nothing; its own change list shows the swings (rev:28).
- **Must contain.** Any proposed change to the plan, or any new rule added during the work, is checked by an
  agent that did not propose it against all five of his 2026-11-13 rules and against the rulings file at
  once, before it is adopted. The check file must exist before the plan edit is committed (F2 item 3 enforces
  it).

---

## Section 2. His rulings: the route of each into canon, or why none is needed

Each row number is HR's. "Fixed input" means the rulings-file row (F4) records the canon line, the
re-check script keeps it there, and the proof-model author and every adjudicator are given it as settled.

### 2.1 Already in canon — record the line, keep it, give it as fixed input

Rows 5, 8, 9, 10, 13, 15, 18, 19, 20, 21, 22, 23, 26, 31, 33, 43, 45, 46, 47, 49, 50, 53, 54, 57, 58, 60,
61, 66, 67, 69, 70, 71, 74, 75, 78, 80, 88, 93, 95, 96 (HR:248). Route: rulings-file row of type (a) with
the line HR gives; nothing else. Eight of the in-canon rows need more than that, because canon states the
target and not the design:

| Row | What canon has | What the new plan must add |
|---|---|---|
| 4 Full proof power | Target at `precept-language-spec.md:266-268` | The work item in 3.3 that removes canon's permanent-limit text (TC T6), and the closed list of refusal reasons (F8 item 2) |
| 6, 51 The induction; rules used as premises must be established and preserved | Target at spec:268; gap stated at `proof-engine.md:2614-2616` | The proof model (section 3.1, items 3-6). This is the largest piece of work in the plan (TC:55-56) |
| 12 Non-linear rules | `precept-language-spec.md:270` | The definition of "states the post-state condition directly" (section 3.2, problem 2) |
| 28 Certificate and one checker | Commitment at `soundness-and-coverage.md:159-161` | The certificate and checker design (section 3.1, item 11; TC C5, C6, C7) |
| 42 Overflow build deferred past the MVP | `precept-language-spec.md:272` | The overflow model is still designed here (TC T8; section 3.2, problem 4). The ruling governs build order only |
| 59 Three verdicts visible; "minimal proof set for the MVP" | `diagnostic-system.md:183` | Under row 4 this ruling is about the first build. The design states the three verdicts' diagnostics for rule obligations too, not only containment (TC C2, `diagnostic-system.md:181`) |
| 65 Who constructs a `Fault`, deferred "until we start building the runtime" | Recorded open at `runtime/fault-system.md:8`, `runtime/README.md:28` | Stays a named exception. One line of information in the first batch: the runtime design is now part of this work, so his trigger falls inside it; nothing is asked unless he says so (rev:35). Also correct `runtime/README.md:36`, which words it "until the runtime design is done" (v2:241) |
| 87 `integer` fixed width now, model open | `primitive-types.md:81`, `:697` | His question (section 3.4, Q-B) |

### 2.2 Partly in canon — the missing part and its owning section

| Row | Missing part | Route |
|---|---|---|
| 3 One full statement, deduplicate | Applied in places (`primitive-types.md:740`); twelve canon-against-canon rows await it (TC:111-117) | Pointer form and ownership table (F14 item 2); the twelve rows are work in 3.3 |
| 11 The "What must not compile" table | Ten of the fourteen rows have no named diagnostic (TC:52) | Each row becomes an acceptance criterion in the handoff file now (TPM:444), and each gets its diagnostic in `diagnostic-system.md` when the obligation model is written |
| 14 Authors extend constraints to entry points | Spec states ingress governance (`:274`) but not the requirement on the author | Ingress contract section, written with the obligation model (TPM 2.11, step 5) |
| 24 No tiers of trustworthiness | Covered by `philosophy.md:109`, not in those words | State it in the guarantee section the spec owns (`precept-language-spec.md:262-280`). Philosophy is not edited |
| 25 Never inserts the premise | "Never inserts" not stated; tooling defines code actions that edit source (TC T9) | Verdicts section (TPM step 9) and `language-server.md` |
| 27 The proof stops at one precept | Canon says no cross-precept resolution (`name-binder.md:374`), not the proof boundary | Guarantee section in the spec; `name-binder.md` points to it |
| 35 Coverage rests on the catalogs | `CLAUDE.md:89-109` states the goal and suspends it | `catalog-system.md` specifies the catalog entries the checker asks (TPM step 12). Separately: this work takes its lists from `docs/language/`, never today's catalogs (v2:125) |
| 38 A no-write route into a state owes its constraints | Residency defined (`precept-language-spec.md:1892`); the obligation is not | Obligation sites in the proof model (TPM 2.4, "A change of state is a write") |
| 56 Create, Fire, Update owe preservation; Restore exempt | Restore half at spec:278 | Per-operation obligations in the proof model |
| 62 Period ordering, "build both" | `collection-types.md:533` still says the collection half "does not follow" | Mechanical edit in 3.3 |
| 64 Period-ordering design locked | Scalar rule in canon; the other decisions not in their owning documents | Each of the eight decisions stated in its owning document: proof engine, catalog system, diagnostic system (rev:57, :127), plus Decision 6 (the type gate on relational facts) in the proof model (old:1610-1616) |
| 68 One runtime index entry comes out | `descriptor-types.md:109` still discusses `AccessModes` | A checker reads `trace-walk-rulings.md:50-59` and the line and records which it is |
| 81 Guarantee contract, part 4 superseded | Parts 1-3 in canon | None beyond the record |
| 90 WF-0 rulings | Collections implementation-state row unchecked | A checker confirms or records it |
| 98 The spec states the target | Stated for philosophy, not for the spec; spec:196 and :2224 say the opposite | Mechanical edit in 3.3 (TC T10) |

### 2.3 Design rulings not in canon

| Row | Ruling | Route |
|---|---|---|
| 7, 55 Obligations attach to every write site of every field a rule mentions | Not found in canon | Proof model obligation sites (TPM 2.4), over the closed writer list (TPM 2.1; spec:1984-1999) |
| 16 Restore handles schema evolution post-MVP | Canon disagrees with itself | Mechanical: want:141 names the owner; edit `compiler-and-runtime-design.md:718` (TC C11). The design of that capability goes in the deferral sort's fourth group, design owed (F8 item 3) |
| 17 What an editable write evaluates | Ruled out of canon pending a decision (`trace-walk-rulings.md:42-48`) | His question (3.4 Q-A), then mechanical across about twenty sentences that still state the narrow answer (TC:39) |
| 29 The premise list is the runtime's validation surface | Open | Design work: certificate and runtime-check derivation (TC C4; TPM 2.14 item 7) |
| 30 Inspect cites provenance | Open | Design work: inspection result types (TC C3) |
| 32 May a rule mention the current time | Undecided; canon says yes (`temporal-type-system.md:568`, `:577`) and so does `philosophy.md:28` (TC C9) | His question (3.4 Q-C) |
| 37 One proof per path to an expression | Not in canon | Proof model premise and obligation sections |
| 39 Writing a field read in a rule's `when`, or through a computed field, takes on the obligation | Not in canon | Proof model "which pairs need work" (TPM 2.4) |
| 40 Faults prevented at the point of evaluation; rules judged on the committed configuration | Not in canon; sequential flow is (`spec:233`) | Proof model; fault obligations (TPM 2.6) |
| 41 The compiler works with the expression as written; widening later | Not in canon | His ruling is about the first build (HR row 4 governs the design). The design states the intended rewrite set, each rewrite with its argument against the evaluator's arithmetic, and marks the first build's set (rev:38; TPM problem 2; TP:166) |
| 52 A computed field's modifier may not be a premise while nothing establishes it | Transient bug list only | Proof model premise section |
| 91 Fold literal division through the one shared evaluator | Working draft only | Operator-meaning section (TPM step 2) and the one-definition-shared-three-ways requirement (TP:133) |

### 2.4 Process rulings — where each is carried

| Row | Carried by |
|---|---|
| 1 Design first | Plan opening; F5 item 2 five-rule check |
| 2 All detail stays | F8 item 1 (no removed line without a destination); status "Full" set by script (rev:58) |
| 34 Reset plan rows "mostly mine" | Kept-rulings table in the new plan, each row marked kept, replaced or moot (rev:42) |
| 36, 72, 79 Samples, code and catalogs are not evidence; build state is not design business | F9 |
| 63 Research gate waived for period ordering only | Rulings-file row; no further route |
| 73 Canon against canon is in scope | The consistency checks read all of canon (v2:155) |
| 77 Close phase 2 with defects recorded | Phase 2's open items carried in as numbered rows (section 4, rev:32) |
| 99 End-state terms; current engine has no bearing | F5 item 2; F8 |
| 101 Plain prose | F16 items 1-2 |
| 102 Plain register; labels are not names | F16 item 2 |
| 103 Plain consequence with a worked case | F16 item 3; question preparation (old:342-346) |
| 104 No modal prompts | F16 item 3 |
| 105 Numbered steps, not labels | Plan steps numbered 0, 1, 2 …; no workstream codes |
| 106 Agent reviews do not stand in for his | Sign-offs are his; agent verdicts are shown with what they did not cover |
| 107 Agent-written documents are not his decisions | F7 |
| 108 Do not bring him what he has ruled | F5 |
| 109 Widening a ruling is the same failure | F6 |
| 110 Keep the session thin | F17 |
| 111 Spend for the best result; Fable for its power | Model table pinned per role (v2:292-300); no model change without his say (v2:554) |
| 112 Consult before new language surface | Any design item needing new syntax becomes a conversation opener, then `/design` (v2:443) |
| 113 Working folder is transient | Canon cites no `docs/Working/` or `docs/working-reset/` path; lint (TC T11 already finds one) |
| 114 Execute must not rewrite design | Handoff file to the build stage |
| 115 Precept-reviewer at every slice boundary | Handoff file to the build stage |
| 116 Solo developer, no back-compat | Brief rule for agents: no migration cost in any design argument |
| 117 Stop re-flagging the runtime stub | Brief rule; vocabulary lint is not needed |
| 118 Use the whole time budget | Runs end only on finish, stop rule or spending limit (v2:459-465) |
| 119 Search history before claiming silence | F10 item 2 |
| 120 NodaTime not pinned | No route; not design |
| 121 `bugs.md` and the want document stay live | Plan states it; neither is archived |

### 2.5 Superseded — no route, with the reason

Rows 44 (matrix → expression-form walk), 48 and 82 (philosophy rewrite of 2026-07-28), 76 (row 73 the same
day), 83 (want:14 and :171 and row 43), 84 (checker back in scope, row 28), 85 (row 42), 94 (step vocabulary
closes when the procedure is written; see section 4 on its authority), 97 (rows 46 and 62), 100 (row 1).

One thing in the record needs correcting outside the repository: `memory/feedback_dont_resurface_ruled.md:103`
still teaches the 2026-07-24 version of row 97, which rows 46 and 62 reversed (HR:265-267). The main session
should correct it with his say; this file does not edit memory.

### 2.6 Unclear — fresh check, then question only if the check finds his words

Row 86 (approximation honesty per operation instead of the type-level lane split): philosophy edits need his
approval, so if the check finds his words it becomes a question, otherwise it is recorded as not applied.
Row 89 (the `in 'each'` example): a checker finds the line or records it moved. Row 92 ("constraint" as the
umbrella term): a glossary entry, mechanical once the check confirms his words.

### 2.7 Agent labels (HR § 9) and declined questions (HR § 10)

The eighteen labelled items in HR:203-227 go into the precedence file as "not his ruling" (F7 item 1). The
six reset-plan rows dated "Settled 2026-07-26" (HR:225) are among them, which matters for section 4.

HR § 10's declined questions route as follows. Must a proof name what establishes and keeps each fact: his
question (3.4 Q-D), since he said he was not prepared to rule without more context (HR:231-233). Divisor
non-zeroness, earlier rows' guards as premises, a computed field's formula as a premise, residency as a
premise, where the discharge fragment stops: design work under full proof power, not questions (v2:314-317).
`from any`: a fresh check whether the two readings differ in anything an author or caller can observe; if
not, design work (v2:320). State-action multiplicity and entry actions at construction: his question (3.4
Q-E). `integer` model: 3.4 Q-B. The current time: 3.4 Q-C.

---

## Section 3. The technical track: what the design must contain, in what order

This section is the technical track his request asks for. The plan must carry it as a line of work whose
files live in the repository and whose order the steps follow.

### 3.1 The full contents of the design

The contents list is the technical track's first committed file, taken from TPM Part 2 and the requirement
list R1-R22 (TPM:43-66), each item naming its owning canonical document. Current canon state is TPM's.

1. **The system the induction runs over** (TPM:72-82). Configuration; operations; the eight-step order;
   the closed writer list (`precept-language-spec.md:1984-1999`, five of eight uncatalogued); and the three
   open points that define "handler": several state actions on one state, entry actions at construction and on
   self-transition (spec:2016), and `from any` (spec:2203).
2. **What counts as a rule** (TPM:84-104). One table turning every constraint spelling into the statement
   that must hold and where: unguarded and guarded rules, every modifier on every type including on optional
   fields, the four ensure anchors, event ensures, element-type modifiers, quantified rules, bounds on computed
   fields, `maxplaces`.
3. **The base case** (TPM:106-123): the default configuration, every initial event per rule from argument
   constraints, guard and surviving defaults, the overwritten-default exemption, construction with no initial
   event, guarded rules established not skipped, the value of a field with no default.
4. **The inductive step** (TPM:125-138): which (rule, handler) pairs need work; every write site of every
   mentioned field; a change of state as a write; a handler's effect as a substitution for every action kind
   including the `into` target, `clear`, the reset of omitted fields and collection actions; rows that write
   nothing; editable writes.
5. **What a proof may rely on** (TPM:140-159): the complete premise set at a site and the statement that
   nothing else may be used, including the negation of earlier rows' guards, guarded rules as "if G then P",
   facts from earlier actions in the chain, facts intrinsic to a type, the value checked at an editable write;
   and a decision on inferred facts the author never declared (strategy 7, `proof-engine.md:1444-1446`).
6. **Fault obligations at every evaluation position** (TPM:161-169), including guards and rule conditions
   evaluated at ingress and dispatch, and the short-circuit of `and`/`or` as a premise rule.
7. **What each numeric type requires** (TPM:171-179): `integer`, `decimal` with its rounding, `maxplaces`,
   `number`.
8. **Temporal, quantity and money** (TPM:181-187), including the qualifier layer and qualifiers that come
   from event data.
9. **Strings, choices, optional values, collections, computed fields** (TPM:189-203), with presence as a fact
   that actions create and destroy, and quantified rules proved for all sizes.
10. **Guards, linear and non-linear relational rules, transitions, stateless definitions** (TPM:205-220).
11. **Ingress and editable fields** (TPM:222-230): argument ingress, the editable-write evaluation, its order,
    which states allow the edit.
12. **Discharge, verdicts and explanations** (TPM:232-246): the inference steps, whether each class always
    reaches an answer, three verdicts, the counterexample, the missing premise.
13. **Graph structure with dead rows feeding reachability** (TPM:248-257).
14. **Certificates and the checker** (TPM:259-281): the record, the step vocabulary, the checker's own duty
    to recompute the obligation set (TP:127, the proof-carrying-code precedent), what ties a certificate to a
    definition, how it reaches the runtime, the compiler stage, the load gate and its failure, the runtime
    checks derived from premises, what the checker trusts.
15. **What the runtime does and does not do** (TPM:283-304), and the runtime and tooling requirements in
    TC § 4 (TC:200-219): inspection with provenance, refusal contents, the clock at runtime (TC T14), the
    three verdicts in tooling (TC T16).
16. **The induction argument itself**, naming state residency, computed fields recomputed every operation,
    `omit` resets, entry and exit chains and first-match rows (old:1716-1718; old:597-602), with the statement
    that "every reachable configuration" is delivered by proving the rule set inductive, and that a true but
    non-inductive rule is rejected with advice to add a rule, not a guard (TP:61-69).
17. **The overflow model** for `integer`, `decimal`, the temporal types and `number` (v2:422; TC T8).
18. **Catalog entries and diagnostics** for obligation kinds, premise kinds, certificate steps and writers
    (TPM:442).

The contents list stays open until the inference steps (item 12, TPM step 7) are written, and closes then
with a hash (section 4, rev:30). It is drafted by two agents independently; if their item counts differ by
more than a third, it is redrafted before anyone relies on it (old:1510-1517).

### 3.2 The hard problems, as named work

Each is a work item with its own file, a one-page engineering write-up first (F19), then a design, then its
arguments through twelve rounds. None may be closed by moving it to the refused list unless a reason in F8
item 2 applies.

| # | Problem | Source | What the design must decide |
|---|---|---|---|
| H1 | Combining several declared facts; deciding the linear class completely | TPM:312-324; TP:79-93 | Whether linear obligations are decided completely with linear-combination multipliers as the certificate (complete over exact numbers, TP:17); integer rounding steps; case splits with a stated cap. Replaces the single-step wording at spec:256, `proof-engine.md:2539`, `soundness-and-coverage.md:332-337`. The want's own `Deposit` and `OpenAccount` need it (TPM:214) |
| H2 | Non-linear rules: what "states the condition directly" means | TPM:326-332; TP:151-168 | The rewrite set, each true of machine arithmetic; products linear in disguise; sign and monotonicity facts recorded as a decision either way; products as opaque terms (TP:162) |
| H3 | Evaluator arithmetic is not proof arithmetic | TPM:334-347; TP:170-189 | One written arithmetic model per type, shared by evaluator, prover and checker; how exactness is shown or an error bound carried; what is provable about `number`. TP:179 found no verifier reasoning about base-10 rounding: a dedicated research pass first, via `/research` |
| H4 | Overflow | TPM:349-362; TP:143-149 | The model per type. The want's `Deposit` is unbounded and called provable (want:101), which is evidence about which model he expects; this goes with Q-B |
| H5 | Rules over collection elements | TPM:364-376; TP:203-218 | A preservation table per collection action per quantifier form; existential rules under removal; element rules mixed with fields |
| H6 | Temporal arithmetic | TPM:378-386; TP:191-201 | The list of calendar properties a proof may use; zone conversion as a function only; range (with H4) |
| H7 | Scale | TPM:388-396 | Which pairs are skipped; `from any` counting; where the case-split cap sits and why a real definition will not reach it; a cost bound per proof step (F8 item 2; old:1723-1731) |
| H8 | Rows tried in order | TPM:398-400 | Negated earlier guards as premises, by case splitting |
| H9 | The editable-write check | TPM:402-404 | One evaluation over the candidate configuration, proved unable to fault, worded without contradicting "nothing is evaluated after a mutation". Depends on Q-A |
| H10 | What the checker is trusted for | TPM:406-408; TP:127, :133-137 | The checker recomputes the obligation set from the definition; what it reads from the catalog; its own arithmetic exact |
| H11 | Producing the missing premise | TPM:410-412; TP:95-112 | The smallest consistent suggestion (TP:102); guard versus argument constraint; "add a rule" for non-inductive failures; counterexample values that are legal field values, for arguments too |
| H12 | Rules true but not inductive | TP:61-69, :254 | How the diagnostic tells this apart from a missing guard; presenting a step counterexample as "a configuration your rules allow" |
| H13 | Wording of the reachability guarantee | TP:226, :262 | "No state is provably unreachable from what was declared"; dead rows read the relational rules (TPM:256) |

### 3.3 The canon conflicts, as named work

All are work items in one file, each with its classification from TC (mechanical, design argument, his).
The plan must carry every one by name:

- **Canon against canon on the proof model**, TPM Part 5 items 1-9 (TPM:454-462): base case and initial-event
  arguments; unknown results and guarded ensures in the base case; `to` ensures of the initial state;
  stateless construction; `maxplaces`; quantified rules governed at runtime; what crosses to the runtime;
  stateless guards; which pre-state rules are premises. These are design arguments resolved by the proof model
  sections that own them, except item 4 and T1 (mechanical).
- **Canon against the want**, TPM items 10-16 (TPM:466-472) and TC C1-C11 (TC:37-105). Items 10 (combining
  facts) and 12-13 (dead rows reading relational rules; reachability) go to H1 and H13; item 11 is Q-A; item
  14 is Q-C; item 15 (the `ReduceLimit` guard reading an optional with no presence test, want:75) is a
  consequence of the presence rule he marked his (v2:318), shown in the first packet, not asked; item 16 goes
  with Q-B.
- **Strategy numbering** (TC C12) and the fifteen check rows N105-N119 (TC:111-117): twelve mechanical
  under "one full statement", three design (N106, N107 with C4, N117 with C8).
- **Fourteen cold-read findings with no row** (TC:119-136): thirteen mechanical, one small design argument
  (the `StructuralSinkState` check).
- **Seventeen new findings T1-T17** (TC:142-196): eight mechanical (T1, T2 first half, T3, T4, T7, T10, T11,
  T15, T17 as TC lists them), eight design (T5, T6, T8, T9, T12, T13, T14, T16), one for him through C9.
- **The register's bookkeeping slip** on `N82` (TC:25) and the stale `N105` classification (rev:74).

TC's count: 58 open, 35 mechanical, 19 design arguments, 4 needing him first (TC:225-232). The nineteen hang
off four pieces of design: the induction's machinery, the certificate, what inspection and refusals report,
and where proof stops (TC:236). The plan must schedule them as those four, not as nineteen.

Mechanical items that touch nothing in the frozen set (v2:179) may run at any time on a separate line. The
rest wait for the proof-model section that owns them.

### 3.4 Questions the technical track finds that only he can answer

These are the questions that survive TC's classification. Each still goes through F5's checks first.

| | Question | Why only he can answer | Source |
|---|---|---|---|
| Q-A | What closes a rule when an editable field it mentions is edited | His own lines pull two ways; he declined to have it settled in passing | TC C1; HR row 17; v2:347-367 with rev:43's corrections |
| Q-B | The `integer` model, and what the overflow model must do with the want's unbounded `Deposit` | Recorded as his open item | HR row 87; TPM:353; TC T8 |
| Q-C | May a rule mention the current time | He said undecided; canon and approved philosophy say yes | HR row 32; TC C9 |
| Q-D | Must a certificate name what establishes and keeps each fact | He reserved it | HR:231-233; v2:436-437 |
| Q-E | State actions: several per state, at construction, on self-transition | Changes what the language means | spec:2016; v2:369-388 |
| Q-F | Is an event `ensure` ingress validation, a premise class, both, or neither | The want never names `ensure`; canon disagrees with itself | TC C8 |
| Q-G | Decimal arithmetic model, if H3's research shows a language-facing choice | TP:187 says it has a language-facing side | TP:184-189 |

Two more, as conversation openers before any `/design` pass, not as forks: bounds on temporal point types
(TPM:360; TP:197), and the two open language questions in `collection-types.md:1296-1300` (v2:443).

### 3.5 The dependency order the steps must follow

From TPM Part 4 (TPM:416-446). Each plan step names which of these it carries; a step may not start a piece
whose prerequisite here has not passed its attack rounds.

1. **What an operation is** (TPM step 1). Needs nothing. Waits on Q-E for one sub-item, written both ways
   meanwhile.
2. **What every operator means on every type**, rounding and overflow included (TPM step 2; H3, H4). Needs
   nothing. Waits on Q-B, and on Q-G if it arises.
3. **The table of rules** (TPM step 3). Needs 2. Waits on Q-C for clock rules; quantified rules (H5) and
   `maxplaces` are here.
4. **The obligation model and the ingress contract together** (TPM steps 4-5; H9). Needs 1 and 3. Waits on
   Q-A and Q-F.
5. **Facts per type and how each action changes them** (TPM step 6; H5, H6). Needs 2 and 4.
6. **The inference steps, which are the certificate vocabulary** (TPM step 7; H1, H2, H8). Needs 5. The
   contents list closes here. This is the centre: most of what TPM marks Silent is decided here (TPM:446).
7. **The procedures that find proofs**, with limits and cost bounds (TPM step 8; H7). Needs 6.
8. **Verdicts, counterexample, missing premise** (TPM step 9; H11, H12). Needs 7.
9. **Graph structure with dead rows** (TPM step 10; H13). Needs 4 and 7; the design shows the loop with
   unreachable-state obligations ends in one pass or defines the order (TPM:438).
10. **Certificate and checker** (TPM step 11; H10). Needs 4 and 6. Waits on Q-D.
11. **The induction argument** (3.1 item 16). Needs 1, 4, 5 and 6; attacked as an argument like any other.
12. **Catalog entries and diagnostics; runtime and tooling carry-through** (TPM step 12; TC § 4). Needs all of
    the above.
13. **Acceptance criteria.** The fourteen mutations (want:112-127) are written into the handoff file now;
    criteria for each type and hard problem follow 3-11 (TPM:444).

Steps 1, 2 and 3 run side by side. Deduplication of plain copies outside the frozen set runs beside all of
them. Q-A, Q-B, Q-E and Q-F gate the earliest steps, so they go in the first batch; Q-C and Q-D can go in the
second.

---

## Section 4. The review's findings and what the old plan did better

### 4.1 Numbered findings (rev:24-52)

| # | Finding | Disposition | Reason |
|---|---|---|---|
| 1 | Step 0 cannot finish: test 4 needs inputs from later steps | **Keep the fix** | Measure on stand-ins that exist (the phase-5 write-ups, the bank example), label them so, re-measure on the first real run. Combined with item 21 |
| 2 | No check over the whole proof model; failures one at a time | **Keep** | F12 item 3. Send the argument count and implied agent runs as information with the step 1 notice |
| 3 | An argument passes on attackers' silence | **Keep** | F11 items 2-4 |
| 4 | Contents list closed too early, one drafter, no reopening | **Keep the substance; reject the stated authority** | Close at the inference steps (3.5 step 6), two drafters, dependency record (F13 item 2). The review calls the closing point "your 2026-07-26 ruling" (rev:30), but HR:225 lists that row among agent labels with no quotation. It stands on its reasoning (old:527-532), not as his word |
| 5 | Rulings not required to reach canon | **Keep** | F4; section 2 |
| 6 | Phase 2's open findings and executed rows not carried by name | **Keep** | TC § 2 already lists them; copy every open row, cold-read finding and N105-N119 into the conflicts file as numbered items (3.3). Executed rows rank below his quoted words and the want (F7 item 1). Re-run their patterns after each canon-writing run, with failing patterns read by an agent |
| 7 | Merging has no check that detail survived | **Keep** | F8 item 1; F18 item 3 |
| 8 | Nothing catches agents changing canon without his authority | **Keep** | At the proof-model sign-off and at the finish, a fresh agent sorts the canon diff into the old trace's four groups (old:370); he sees only the group that traces to nothing he wrote or ruled, capped, with citations |
| 9 | Questions close by a side-list default or by "it waits" | **Keep** | A default counts only once written into its owning section and its index row removed, both by script; every "waits" row listed as a known gap; the `Fault` line in 2.1 |
| 10 | Deferral sort lets first-build scope back in | **Keep** | F8 item 3; schema evolution and cross-precept proof named |
| 11 | Admissibility tests on cost and "earns its place" as a loophole | **Keep** | F8 item 2 |
| 12 | Walk item 13 missing from the contents list | **Keep** | Section 2.3 row 41; H2 |
| 13 | Packets could get verdicts from the compiler | **Keep** | F9 item 1 |
| 14 | Hand walk omits loading a definition | **Keep** | Seventh operation, load, with valid, stale and tampered certificates; plus the typed-lane and JSON boundary (rev:126) |
| 15 | Count of what reaches him is asserted | **Keep** | F5 item 5 |
| 16 | Question 1 does not list everything it changes | **Keep** | Kept-rulings table with every old ruling marked kept, replaced or moot; a clean plan page (F2). Pilot merges wait for his answer |
| 17 | Q2 misstates option 2 and omits the computed-field timing problem | **Keep** | Option 2 in want:143's own terms; the timing problem (old:568-571) and the "partial sweep" warning (old:581-583) as stated costs for the fresh check |
| 18 | Count-must-fall rules misfire | **Keep** | F12 items 2, 4, 6 |
| 19 | Step 1's agreement criterion has no fixed answer | **Keep** | F3 item 4 |
| 20 | Parked work has no record or owner | **Keep** | F13 item 3 |
| 21 | Step 0 is tool-building with no limit | **Keep** | A fixed list of scripts, one test each; build only what the first line needs; later scripts are the first piece of the step that uses them |
| 22 | Two sessions commit to one working tree | **Keep** | A git worktree per line; shared files (log, status, notices, defaults) written only by a script that takes a lock; half-written files quarantined at start |
| 23 | The starting session still judges | **Keep** | F17 items 1-2 |
| 24 | Short-window usage limit not handled | **Keep** | The session waits out a short-window reset itself; step 0 records how each limit shows up |
| 25 | Fable share exhaustion stalls attacks; extra usage | **Keep** | State it; check the extra-usage setting in step 0; scripts stop on their own |
| 26 | Fable author's context untested | **Keep** | A step-0 test: a Fable-pinned agent loads the reading list, reports its model id, writes a short section |

### 4.2 Minor findings (rev:57-86)

Keep, each as rev states it: period-ordering decisions in their owning documents (2.2 row 64); "Full" status
set by script; the `CLAUDE.md` catalog-notice edit says the design now specifies the catalog contents and the
suspension stays until the build enforces it; the argument count and implied runs sent with the step 1
notice; a handoff file to acceptance criteria; tooling followed through hover, `precept_proofs` and inspect
on paper; proof text behind a pending marker (F11 item 7); "finding" defined (F11 item 3); the plan's change
list replaced by clean text; restarts expected per line given in the first packet; missing approvals added to
the totals; an answers file (F6); the filter's error rate measured (F5 item 4); the want:75 point (shown, not
asked; the want file itself is not edited); language lists re-checked (F18 item 2); N105 corrected; known
counterexamples to attackers (F11 item 5); a cost bound per strategy (F8 item 2, H7); decision index marked
(F7 item 3); a fact-checker per round (F11 item 6); the scale of agent runs stated before the plan is
replaced; Q-E cannot block the checkpoint (the creation argument is attacked in a form with no state actions);
the token ceiling passed in arguments and compared against spent output tokens; a skill or command, not a
saved workflow, as entry point; push notices used and a heartbeat in the lock file; some attackers on a model
other than the second author's for those rounds; permission prompts tested and the rules recorded; quote his
words rather than credit him with the loop diagnosis.

Dispositions that differ from rev:

- **Q4 (dead end) should not reach him** (rev:67, :173): **keep, narrowed.** TC C10 classes it as design
  argument. Resolve under the consistency work against want:152 and `philosophy.md:44`; send him only the
  narrow case of two non-terminal states looping with no route to completion, if the fresh check finds the
  text does not decide it.
- **Q8 (builder metadata) should be a default** (rev:67, :174): **keep.** The "pending owner ruling" label
  came from a Copilot co-authored commit (rev:174) and nothing an author writes changes.
- **No mechanical counterexample search survives** (rev:64, :156): **reject for the design stage.** Under his
  rule 1 the design finishes before code. The design specifies the bounded counterexample search (v2:126); the
  search itself goes in the handoff file. If he wants it sooner, that is his to say.

### 4.3 What the old plan did better (rev:88-97)

| Old practice | Disposition | Where in the new plan |
|---|---|---|
| Whole-model stop that reaches him with a count; "different problem each attempt" test | **Take back, applied by script** | F12 items 3-4 |
| A concrete attempted counterexample and a disposition for every attack (old:1801-1807) | **Take back** | F11 item 2 |
| Two drafters; vocabulary closing when the procedure is written; cascading repairs as a warning | **Take back** (authority as in 4.1 item 4) | 3.1 closing paragraph; F12 item 5 |
| No deferral without an owner; rulings tracked with owners | **Take back** | F13 item 3; F4 |
| Phase 2's register as settled input, open findings listed by citation | **Take back** | 4.1 item 6; 3.3 |
| The trace of what justified each change to canon, and a review of what was deleted | **Take back** | 4.1 item 8; F8 item 1 |
| Caps on his time; a random sample of filtered items; marks recorded in the sitting | **Take back** | F5 items 4-5; F6 item 1 |
| The per-type algebraic table (old:1676-1677) | **Take back** | 3.1 items 7-9; it is TPM 2.7-2.9's content |
| The validity arguments re-homed as derivation rules (old:1664-1667) | **Take back as inputs only** | Given to H1 and the inference-steps section as claims of unknown authority to attack, not as settled; old:1819-1820 forbids citing refuted arguments |
| The route through the execution order (old:1679-1684) | **Take back** | 3.1 items 1 and 4; it is the handler-as-substitution requirement |
| The known counterexamples (old:1679-1684) | **Take back** | F11 item 5 |
| The strategy cost model (old:1723-1731) | **Take back** | H7; F8 item 2 |
| Period-ordering Decision 6 in the proof model (old:1610-1616) | **Take back** | 2.2 row 64 |

Not taken back: the thirteen phases; the build phases inside the design; phase 8's "narrow" as a plan step
(narrowing is language design for him and `/design`, v2:255); "the first honest fragment … smaller" (old:1645);
overflow "not a hole" (old:333).

---

## Section 5. What in v2 is good and must be kept

- **Scope and level of detail**: all 36 canonical documents, internals included (v2:66-76), each unfinished
  document with a finish condition (v2:134-147). This is his rule 2.
- **Full proof power recorded with its grounds** (v2:314), "not built yet" never a reason (v2:131), no
  refuse-more defaults (v2:276).
- **Proof model beside deduplication, not after it** (v2:172-177), with a frozen set of facts the proof model
  will change (v2:179).
- **Copies that agree are merged; copies that disagree are decisions** (v2:230-235), never settled by picking
  an owner.
- **Lists that canon hands to source files are restated in canon** (v2:100, :154).
- **Lists from `docs/language/`, never from the catalogs** (v2:125, :193), with the two senses of "coverage
  rests on the catalogs" kept apart (v2:125).
- **Counts printed by scripts, never typed; a failed agent is "not done", never "found nothing"** (v2:487-490).
- **Progress saved to fixed-form files after every piece; the next run starts from the files** (v2:469-493).
- **Step 0's tests against made-up, missing, half-written and malformed inputs**, and running workflow
  scripts end to end under stand-ins rather than parsing them (v2:516-518), using the `new Function` check
  rather than `node --check` (v2:517).
- **Twelve attack rounds per argument with no early pass, flaw kinds written first, a classifier, a second
  author on another model, never settled in the session written** (v2:249-255), and "no proof step enters the
  design until its argument has passed" (v2:278).
- **An adjudicator per dispute with a written order of precedence, started by script** (v2:245).
- **Questions checked by a fresh agent that does not see the plan's conclusions**, and the filter that
  options differing only in proof power are design work (v2:308, :447); preparation with quoted text, worked
  example, what each option refuses, the author's message (v2:664).
- **Internals answers drafted with four-part reasoning, attacked, recorded as defaults he can overturn**, with
  caller-observable ones flagged (v2:239); and the four-part reasoning on every decision (v2:274).
- **The hand walk across stages and operations by separate agents, with paired derivation** (v2:158-159).
- **No dates and no size estimate**; size given as counted pieces times measured cost (v2:624-630).
- **He starts each step with one line; the same line restarts it; nothing on a schedule** (v2:581-606), and
  he hears from the work only on named conditions (v2:560-579).
- **Models pinned by name per role; no weaker model substituted at a limit** (v2:292-300, :554).
- **The stated limits**: paper checks can share a blind spot; the checker, which is code, is the eventual
  backstop (v2:162); and its honest "not verified" lists (v2:688-690).
