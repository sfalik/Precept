# Independent Adversarial Review of the Design-Completion Plan

**Target reviewed:** `docs/working-reset/design-completion/design-completion-plan.md` (branch `spike/Precept-V2-Radical-design-completion`, commit `5aa417ea`)
**Review posture:** Adversarial technical and operational audit against the repository, the canonical documentation, and the owner's stated end goal and rules.

---

## Executive Verdict

**As written, this plan will not reach the owner's end goal.** It will halt repeatedly before finishing the proof model, burn thousands of model calls on process bookkeeping rather than design, override locked canonical decisions without owner authorization, miss concrete contradictions already sitting in the canonical documents, and demand far more owner time and restarts than its accounting admits.

The root cause is structural. Rather than designing the proof model and repairing the canonical documents directly, the plan responds to past agent failures by building a large, unproven software platform first (15 scripts up to 4,000 lines, 4 JavaScript workflow programs, 11 end-to-end tests, concurrent git worktrees, and dozens of tracking files) and wrapping every single design argument in a 12-round, 4-agent-per-round adversarial loop governed by hair-trigger halt rules. Several of those rules contradict one another mathematically: the mandatory attack record volume guarantees that the plan's own record-to-canon size cap will halt every proof step on its third run; the pending-marker citation ban blocks the sequential pipeline the plan claims can keep moving while questions wait; and the reachability design silently replaces locked canonical decisions with an unworkable bounded-model-checking obligation.

### Finding Summary

| Question | Critical | Major | Minor |
|---|---|---|---|
| 1. Reaching the end goal | 4 | 3 | 1 |
| 2. Failure, stall, and loop modes | 2 | 4 | 0 |
| 3. Technical errors (proof model, compiler, runtime, canon) | 4 | 5 | 2 |
| 4. Hidden costs asked of the owner | 2 | 4 | 0 |
| **Total** | **12** | **16** | **3** |

---

## 1. Will This Plan Reach the End Goal? What Stops It?

### 1.1 Mandatory attack records mathematically guarantee that the record-size stop rule halts every proof step

- **Severity:** Critical
- **Evidence:** `docs/working-reset/design-completion/design-completion-plan.md:667–684`, `docs/working-reset/design-completion/design-completion-plan.md:808–810`, `docs/working-reset/design-completion/design-completion-plan.md:1168–1171`
- **Verified in repository:**
  - The plan mandates 12 sequential attack rounds for every proof-model argument (`design-completion-plan.md:667–684`). Each round invokes four separate agents (two attackers required to write concrete counterexamples, one classifier, and one fact-checker), totaling 48 agent outputs per argument before any repair or second-author retry (`design-completion-plan.md:1168–1171`).
  - Each step also writes hand-over notes, dependency records (`technical/dependencies.jsonl`), finding rows (`findings.jsonl`), ruling citation checks (`uses/` files for every cited ruling and every post-run diff scan, `design-completion-plan.md:651–657`), refusal records, and removed-line checks.
  - At the same time, the stop-rule table (`design-completion-plan.md:808–810`) commands: *"From a step's third run on, lines of record written for it exceed ten times the lines it has changed in canon → That step halts. The cap is a number in `settings.json`, changed only when he says so."*
- **Inference and reasoning:**
  - Even if each of the 48 attack-round files is a terse 30 to 50 lines, one argument generates 1,440 to 2,400 lines of record files before a single repair, dependency extraction, or `uses/` check. A canonical proof argument or rule subsection is typically 30 to 150 lines of prose. That puts the record-to-canon line ratio at 15:1 to 80:1 on normal, healthy work—and far higher on steps like Step 11 (which writes a single induction argument) or Steps 8 and 9 (which edit short existing sections).
  - The plan built two rules that directly defeat each other. From run 3 onward, every proof-model step will cross the 10:1 ceiling simply by obeying the plan's own attack procedure, halting the step until the owner manually approves raising the cap.

### 1.2 The pending-marker citation ban deadlocks the sequential pipeline while questions await the owner

- **Severity:** Critical
- **Evidence:** `docs/working-reset/design-completion/design-completion-plan.md:254–277`, `docs/working-reset/design-completion/design-completion-plan.md:444–446`, `docs/working-reset/design-completion/design-completion-plan.md:463–467`, `docs/working-reset/design-completion/design-completion-plan.md:483`, `docs/working-reset/design-completion/design-completion-plan.md:495`, `docs/working-reset/design-completion/design-completion-plan.md:520`, `docs/working-reset/design-completion/design-completion-plan.md:695–696`
- **Verified in repository:**
  - In § 2.4 (`design-completion-plan.md:274–277`), the plan promises that unanswered owner questions do not block progress: *"until he answers, the sub-item is written once for each option, behind pending markers, and its arguments are attacked in each form. So an unanswered question holds back only the step's finish, never the work that can be done without it."*
  - Yet § 4.2 (`design-completion-plan.md:695–696`) forbids any downstream section from using pending text: *"Text behind a pending marker may not be cited by any other canonical section or argument; `attack-check` fails one that does."*
  - And § 2.4 (`design-completion-plan.md:254–255`) enforces strict step gating: *"A step may not start a piece whose prerequisite has not passed its attack rounds, with the one exception written into step 4."*
  - Looking at the actual dependency table (`design-completion-plan.md:259–271`): Step 1 waits on three state-action questions (`:259, :444–446`); Step 2 waits on the integer/decimal overflow decision and the temporal point-bounds conversation (`:260, :463–467`); Step 3 requires Step 2 passed (`:261, :483`); Step 4 requires Steps 1 and 3 passed (`:262, :495`); Step 5 requires Steps 2 and 4 passed (`:263, :520`); Step 6 requires Step 5 passed (`:264, :540`); and Steps 7 through 11 cascade serially from Step 6.
- **Inference and reasoning:**
  - Because Step 1 and Step 2 cannot pass their finish gates while pending markers exist (`design-completion-plan.md:695`), and because no downstream argument may even cite text behind a pending marker (`design-completion-plan.md:695–696`), Steps 3, 4, 5, 6, 7, 8, 9, 10, and 11 are blocked until the owner answers the first batch of seven questions AND holds the open-ended interactive design conversations.
  - The multi-option pending-marker mechanism doubles or triples drafting and 12-round attack cost inside Steps 1 and 2 while buying almost zero downstream concurrency.

### 1.3 Step 0 repeats the project's own documented tooling-bloat failure on top of 14 untested platform assumptions

- **Severity:** Critical
- **Evidence:** `docs/working-reset/design-completion/design-completion-plan.md:373–432`, `docs/working-reset/design-completion/design-completion-plan.md:1106–1208`, `docs/working-reset/design-completion/inputs/history-failures.md:556`, `.claude/` directory inspection
- **Verified in repository:**
  - `inputs/history-failures.md:556` documents that an earlier tool built to manage a design document grew to roughly 5,700 lines of infrastructure before delivering the design.
  - Step 0 of this plan (`design-completion-plan.md:373–432`) commissions 15 custom scripts capped at 4,000 lines, 4 JavaScript workflow programs, 11 end-to-end platform tests, concurrent git worktree lock coordination (`locked-write`), an unattended background loop runner (`run-step`), and custom token-limit tracking before a single line of canonical design is repaired.
  - Checking the repository directly shows that neither `.claude/settings.json` nor `.claude/settings.local.json` exists yet, and only 4 small single-purpose Python checks exist in `docs/working-reset/phase-2/checks/`.
  - The plan's own table in § 7.6 (`design-completion-plan.md:1185–1208`) admits that 14 platform assumptions—including the exact model names (`Fable 5.2`, `Opus 5.5`, `Sonnet 5.5`), whether the `Workflow` tool exists and runs inside a nested shell loop, whether worktrees can share the repository concurrently, whether token ceilings can be measured mechanically, and whether unattended runs survive overnight—have never been tested.
  - The listed fallbacks dismantle the plan's core operating premise: if Test 7 fails, every run drops to one piece (`:1197`); if Test 8 fails, runs are capped at 5 in a row (`:1155–1156`); if Test 9 fails, concurrent worktrees collapse to one line of work (`:1198`); if Test 11 fails, the owner must leave an interactive session open (`:1201–1203`).
- **Inference and reasoning:**
  - Step 0 front-loads a fragile software engineering project with an artificial 10-run guard (`step0-guard`, `:373–378`). If several of the 14 untested platform assumptions fail or require iterative workarounds, Step 0 will hit its own 10-run ceiling and halt—blocking all design progress while violating owner Rule 5 ("much less of his time").

### 1.4 Circular dependency between Step 6 and Step 9 forces premature freezing or cascading rework

- **Severity:** Critical
- **Evidence:** `docs/working-reset/design-completion/design-completion-plan.md:264–267`, `docs/working-reset/design-completion/design-completion-plan.md:540–544`, `docs/working-reset/design-completion/design-completion-plan.md:557`, `docs/working-reset/design-completion/design-completion-plan.md:580–583`
- **Verified in repository:**
  - Step 6 (the closed list of inference steps) requires `step9/proof-steps-needed.jsonl` from Step 9 as a prerequisite (`design-completion-plan.md:264, :540`).
  - Step 9's final prerequisites are Step 4 and Step 7 (`design-completion-plan.md:267, :580`), and Step 7 requires Step 6 (`design-completion-plan.md:265, :557`).
  - To break this cycle on paper, Step 9 is told to write `step9/proof-steps-needed.jsonl` *"as soon as step 4 has passed"* (`design-completion-plan.md:581–582`)—before Step 5 (type facts), Step 6 (inference steps), and Step 7 (procedures and cost bounds) exist.
- **Inference and reasoning:**
  - Step 9 owns reachability, dead-end detection, dead-row detection, and dominator checks, which explicitly depend on proving guard unsatisfiability and exhibiting concrete paths through field mutations (`design-completion-plan.md:584–587`). Determining which proof steps those analyses require before the type-fact vocabulary (Step 5) and decision procedures (Step 7) are designed is guesswork.
  - When Step 9 later runs for real after Step 7 and discovers it needs an inference step not anticipated after Step 4, Step 6 must reopen—which under the dependency-reopen rule (`:789–790`) invalidates Steps 7, 8, 9, 10, and 11 and forces them back through 12-round attacks.

### 1.5 Condition 5 (`dedup/pairs.jsonl`) creates an unbounded combinatorial sweep with no definition of a "fact"

- **Severity:** Major
- **Evidence:** `docs/working-reset/design-completion/design-completion-plan.md:68–80`, `docs/working-reset/design-completion/design-completion-plan.md:614`, `docs/working-reset/design-completion/design-completion-plan.md:818–828`
- **Verified in repository:**
  - Finish Condition 5 requires `dedup/pairs.jsonl` to hold one row for every pair `(canonical document, owned fact)` (`design-completion-plan.md:68–71`). There are 33 canonical documents across `docs/philosophy.md`, `docs/language/`, `docs/compiler/`, `docs/runtime/`, and `docs/tooling/` (plus `docs/compiler-and-runtime-design.md`, which the plan also cites and updates).
  - For each canonical document, one agent must fill every row across the entire fact list (`design-completion-plan.md:72–74`), and a second agent must re-read every `"does not mention"` row for a sample of documents (`design-completion-plan.md:74–76`).
  - Nowhere in the plan is the granularity of a single "fact" in `ownership.jsonl` defined.
- **Inference and reasoning:**
  - Across 33 detailed technical documents (several over 2,000 lines), any fine-grained fact inventory easily produces 300 to 1,000+ owned facts. Multiplied by 33 documents, `dedup/pairs.jsonl` balloons to 10,000–33,000 rows. Asking one agent per document to accurately classify 500 to 1,000 facts in a single pass exceeds reliable context recall, guarantees noisy `"does not mention"` errors that trip the second-agent sample audit (`:75–76`), and explodes again whenever Step 12 updates frozen facts. Conversely, making "facts" coarse enough to stay small defeats deduplication by letting sub-facts remain duplicated inside large topics.

### 1.6 `cite-check` and `trailer-check` contain operational contradictions that block valid commits and merges

- **Severity:** Major
- **Evidence:** `docs/working-reset/design-completion/design-completion-plan.md:640–641`, `docs/working-reset/design-completion/design-completion-plan.md:836–841`, `docs/working-reset/design-completion/design-completion-plan.md:1138–1140`
- **Verified in repository:**
  - `cite-check` opens every cited path and line using `git show HEAD:path` so that *"an uncommitted edit cannot confirm itself"* (`design-completion-plan.md:640–641`).
  - `trailer-check` checks every commit's canonical diff and verifies that *"every line added to canon is inside the agent's output file"* (`design-completion-plan.md:836–841`).
  - Meanwhile, parallel lines of work run in separate git worktrees and merge via `locked-write` (`design-completion-plan.md:1138–1140`).
- **Inference and reasoning:**
  - First, checking only `git show HEAD:path` breaks whenever a commit adds a new canonical section and a cross-reference or argument citing it in the same commit, or shifts line numbers inside an edited document before commit—forcing artificial split commits where intermediate states are temporarily inconsistent.
  - Second, requiring every line added to canon in a commit to appear verbatim inside an agent's output file breaks on normal git merge conflict resolution between worktrees, automated formatting or line-ending normalization (especially on Windows/CRLF checkouts), reverts of Step 14 commits touching newly frozen facts (`:826–828`), and pointer-replacement edits across multiple files.

### 1.7 `status` and `findings.jsonl` treat Step 15 hand-walk discoveries as illegal state rather than normal iteration

- **Severity:** Major
- **Evidence:** `docs/working-reset/design-completion/design-completion-plan.md:83–84`, `docs/working-reset/design-completion/design-completion-plan.md:624–635`
- **Verified in repository:**
  - Under Finish Condition 3 (`design-completion-plan.md:83–84`), `status` fails if any open row in `findings.jsonl` names an owner step that has already finished.
  - Yet Step 15 (`design-completion-plan.md:624–635`) runs *after* Steps 1–14 have finished and explicitly files any defect found during the 7-operation hand walk as *"an open row in `findings.jsonl` against the step that owns it, which reopens that step"* (`design-completion-plan.md:629–630`).
- **Inference and reasoning:**
  - Because `status` both rejects open findings on finished steps and gates commits/runs, the plan needs an explicit state transition ("reopen step before writing finding row"), and once Step 15 reopens an upstream step (say Step 3 or Step 4), every downstream step in Steps 4–11 loses its passed status and re-enters the 12-round attack pipeline before Step 15 can even finish its remaining six operations.

### 1.8 The plan document already has stale branch, commit, and path references on arrival

- **Severity:** Minor
- **Evidence:** `docs/working-reset/design-completion/design-completion-plan.md:3–7`, `docs/working-reset/design-completion/design-completion-plan.md:29–35`, repository `git status` and directory layout
- **Verified in repository:**
  - `design-completion-plan.md:3–7` states it was written against branch `spike/Precept-V2-Radical-reset` at commit `2f126a3b`, that nothing in the repository was edited, and that Step 0 will place the plan at `docs/working-reset/design-completion-plan.md` and copy `inputs/` from a session scratchpad `v3/`.
  - In reality, the file is already committed on branch `spike/Precept-V2-Radical-design-completion` at `5aa417ea` at `docs/working-reset/design-completion/design-completion-plan.md`, and `inputs/` is already committed there.
  - Furthermore, the short-name table (`design-completion-plan.md:29–34`) maps `v2` to `inputs/design-completion-plan-v2.md` and `review` to `inputs/plan-vs-old-review.md` under `docs/working-reset/design-completion/inputs/`. Both paths do not exist there; both files actually live under `docs/working-reset/design-completion/lineage/`.
- **Inference and reasoning:**
  - Any Step 0 script (`cite-check`, setup automation) that consumes the short-name table or expects the scratchpad path will fail immediately on broken paths unless reconciled first.

---

## 2. Where Will It Fail, Stall, or Loop Without Converging?

### 2.1 Dependency reopen rules combined with `Cascading repairs` create a non-converging halt trap

- **Severity:** Critical
- **Evidence:** `docs/working-reset/design-completion/design-completion-plan.md:667`, `docs/working-reset/design-completion/design-completion-plan.md:681–689`, `docs/working-reset/design-completion/design-completion-plan.md:789–795`
- **Verified in repository:**
  - `Dependents reopen` (`design-completion-plan.md:789–790`) requires that changing *any* passed section or argument sends *every* dependent argument back into attacks—not a focused re-check, but the full 12 rounds (`design-completion-plan.md:667, :688–689`).
  - The dependency graph (`technical/dependencies.jsonl`) is not limited to explicit citations; a fresh Opus agent also scans every argument for unstated implicit dependencies and adds them (`design-completion-plan.md:681–684`).
  - Meanwhile, `Cascading repairs` (`design-completion-plan.md:794–795`) halts a section as soon as **two repairs** in that section each cause a refutation in a dependent or subordinate argument—and that halt can be cleared **only by the owner's words**.
- **Inference and reasoning:**
  - In a tightly coupled mathematical proof model (types → rules → premises → inference steps → procedures → reachability → induction), early sections (Steps 1–5) are cited or implicitly depended on by almost everything in Steps 6–11. Refining a wording or tightening a side condition in Step 2 or Step 4 during normal design iteration naturally forces small adjustments in two dependent arguments—at which point `Cascading repairs` freezes the section and pages the owner, while every dependent resets to round 0 of 12.
  - Rather than converging toward a consistent whole, the machinery punishes iterative cross-section alignment with exponential re-attacks and hard halts.

### 2.2 The `Found faster than closed` stop rule halts normal discovery work across large canonical sweeps

- **Severity:** Critical
- **Evidence:** `docs/working-reset/design-completion/design-completion-plan.md:804–807`, `docs/working-reset/design-completion/design-completion-plan.md:851–855`
- **Verified in repository:**
  - `Found faster than closed` (`design-completion-plan.md:804–807`) halts a step if its open finding rows rise across three consecutive runs OR if total found rows ever exceed twice the count recorded when Step 1 finished.
  - Independently, `design-completion-plan.md:851–855` triggers automatic re-extraction of findings whenever `docs/language/` changes.
- **Inference and reasoning:**
  - Discovery and resolution do not proceed at a 1:1 rate per run. In Steps 12, 13, 14, and 15—where agents sweep 33 canonical documents, audit hundreds of fact-document pairs, inspect 21+ runtime open questions, and walk 7 end-to-end operations—finding counts naturally spike during reading passes and drop later during editing passes. And as Section 3 below demonstrates, the baseline conflict list in § 2.3 missed numerous real contradictions in the canonical docs, meaning Step 14 and Step 15 will legitimately discover more than twice Step 1's initial finding count. This rule treats honest discovery as divergence and halts the step.

### 2.3 The Step 4 checkpoint is structurally rigged to either misfire `Whole model` or provide false assurance

- **Severity:** Major
- **Evidence:** `docs/working-reset/design-completion/design-completion-plan.md:496–506`, `docs/working-reset/design-completion/design-completion-plan.md:686–687`, `docs/working-reset/design-completion/design-completion-plan.md:796–801`
- **Verified in repository:**
  - Step 4 writes an early five-piece sketch before Step 2 (arithmetic) and Step 3 (rule table) exist, using exact numbers and excluding state actions, collections, and the clock (`design-completion-plan.md:499–501`).
  - The sketch is attacked for 3 rounds (`design-completion-plan.md:503–504`). If 2 of the 5 pieces still have an open refutation at round 3, `Whole model` halts ALL of Steps 1–11 and calls the owner (`design-completion-plan.md:796–801`).
  - Yet attackers are explicitly instructed to look for rounding, non-inductive, and hidden-assumption counterexamples (`design-completion-plan.md:686–687`).
- **Inference and reasoning:**
  - Because the checkpoint explicitly omits the hardest features (arithmetic overflow/rounding, collections, state actions, temporal bounds, clock), passing it proves almost nothing about whether the real proof model will hold. Conversely, because it is drafted without Step 2 and Step 3 definitions in place and gets only 3 rounds before triggering a global halt across Steps 1–11, ambiguous boundary definitions can easily leave 2 of 5 pieces open at round 3 and trigger an unnecessary full-project stop.

### 2.4 Small-sample bias in the `Whole model` second-author rule triggers premature global halts

- **Severity:** Major
- **Evidence:** `docs/working-reset/design-completion/design-completion-plan.md:796–801`
- **Verified in repository:**
  - `Whole model` (`design-completion-plan.md:796–801`) halts ALL of Steps 1–11 if at least 3 arguments AND more than one-third of finished first-author arguments have been sent to a second author.
- **Inference and reasoning:**
  - Early in the proof model (Steps 1–4), the total number of finished arguments is small (roughly 6 to 8). If just 3 genuinely hard arguments (for example, editable-field ingress, integer/decimal overflow boundaries, and construction base-case handling with defaults) require a second-author pass, 3 out of 8 exceeds one-third and halts the entire proof-model track before the middle steps have even started.

### 2.5 The post-run `uses/` diff scanner creates a subjective, non-converging halt gate on prose edits

- **Severity:** Major
- **Evidence:** `docs/working-reset/design-completion/design-completion-plan.md:651–657`, `docs/working-reset/design-completion/design-completion-plan.md:811–812`
- **Verified in repository:**
  - After every run that edits canon, a fresh agent inspects the git diff for any sentence that *"rests on what he wants or said and cites no row"* and writes a `uses/` check file (`design-completion-plan.md:651–657`).
  - If any use lacks a `uses/` file or is judged `"wider than the words"`, the entire line of work halts immediately (`design-completion-plan.md:811–812`).
- **Inference and reasoning:**
  - Deciding whether an explanatory sentence in a technical specification "rests on what the owner said" versus follows logically from existing definitions is inherently subjective. Two independent language models reviewing the same diff will frequently disagree on edge cases, causing repeated hard halts over prose attribution rather than technical correctness.

### 2.6 Parallel Step 14 work against a mutating frozen-fact list guarantees revert churn

- **Severity:** Major
- **Evidence:** `docs/working-reset/design-completion/design-completion-plan.md:785–786`, `docs/working-reset/design-completion/design-completion-plan.md:818–828`
- **Verified in repository:**
  - Step 14 (consistency and deduplication across all canonical docs) runs in parallel with Steps 1–11 in its own worktree (`design-completion-plan.md:818–820`), pauses whenever its changed lines exceed twice the proof model's changed lines (`:785–786`), and must avoid a "frozen list" of proof-model-touched facts.
  - Because new facts can be added to the frozen list between Step 1 and Step 6, Step 14 commits that touched a later-frozen fact must be audited and reverted (`design-completion-plan.md:824–828`).
- **Inference and reasoning:**
  - Running a broad cross-document deduplication pass while the core proof model is still moving—and then throttling it to 2x proof-model lines while reverting commits whenever the frozen list expands—creates merge conflicts and wasted rework for zero calendar benefit, since Steps 12–15 cannot finish until Steps 1–11 finish anyway.

---

## 3. Technical Errors About the Proof Model, Compiler, Runtime, and Canonical Docs

### 3.1 Step 9 silently overrides locked canonical decisions on reachability and introduces an undecidable model-checking obligation

- **Severity:** Critical
- **Evidence:**
  - Plan: `docs/working-reset/design-completion/design-completion-plan.md:191–200`, `docs/working-reset/design-completion/design-completion-plan.md:585–587`, `docs/working-reset/design-completion/design-completion-plan.md:958`
  - Locked canon: `docs/language/precept-language-spec.md:169`, `docs/language/precept-language-spec.md:199–201`, `docs/language/precept-language-spec.md:223`, `docs/compiler/graph-analyzer.md:62–72`, `docs/compiler/graph-analyzer.md:795–800`, `docs/compiler-and-runtime-design.md:130–145`
  - Plan's own technical input: `docs/working-reset/design-completion/inputs/technical-precedent.md:226`
- **Verified in repository:**
  - Locked canon explicitly sets a structural over-approximation policy for state reachability: `docs/language/precept-language-spec.md:199–201` adopts the over-approximation rule (structural edges ignore `when` guards as the intended compile-time tradeoff); `precept-language-spec.md:223` classifies unreachable states under proven violations only; `precept-language-spec.md:169` states *"No symbolic execution over unbounded domains is needed"*; `docs/compiler/graph-analyzer.md:62–72, :795–800` and `docs/compiler-and-runtime-design.md:130–145` specify graph-based BFS reachability upstream of the proof engine.
  - Even the plan's own technical background (`inputs/technical-precedent.md:226`) warns that exact reachability with data guards and mutations is undecidable and recommends structural or rule-relative over-approximation.
  - Despite this, `design-completion-plan.md:191–194` and `:585–587` unilaterally adopt the exact opposite requirement as the plan's baseline: the compiler must **exhibit a concrete run from creation to every state** and **reject any state for which it cannot exhibit a concrete run**, and must exhibit a concrete input leaving every non-terminal state (treating undecided exit guards as dead ends, `:196–200`). It does not put this override to the owner up front, deferring consultation only if Step 9 later fails (`:958`).
- **Inference and reasoning:**
  - Technically, requiring a deterministic, solver-free compiler to synthesize a concrete multi-step sequence of inputs and transitions from construction to every state across arbitrary guards and mutations IS bounded symbolic execution / model checking. It directly violates `precept-language-spec.md:169` and the no-solver commitment (`docs/Working/what-i-want-2026-07-16.md:135`).
  - Worse, rejecting any state for which a bounded synthesizer cannot construct a concrete trace will falsely reject completely valid, reachable precepts (for example, a state entered only after a counter crosses a threshold over multiple events, or through a non-linear guard, or via a temporal condition). Treating undecided exit guards as dead ends creates the same false rejections on valid non-terminal states.
  - Process-wise, overriding a locked canonical decision without explicit owner consultation violates `CLAUDE.md` (locked-decision override consultation) and the plan's own no-silent-override rule.

### 3.2 The plan's non-linear rule restriction contradicts the owner's own worked example and proof-power goal

- **Severity:** Critical
- **Evidence:**
  - Plan: `docs/working-reset/design-completion/design-completion-plan.md:133–134`, `docs/working-reset/design-completion/design-completion-plan.md:875`
  - Want document: `docs/Working/what-i-want-2026-07-16.md:14`, `docs/Working/what-i-want-2026-07-16.md:104`, `docs/Working/what-i-want-2026-07-16.md:135`
  - Proof engine doc: `docs/compiler/proof-engine.md:1735`
- **Verified in repository:**
  - `design-completion-plan.md:133–134` locks the following rule: *"Sign and monotonicity facts may be used only inside a permitted rewrite that shows an author's stated premise is that condition. They may not prove a non-linear rule from field bounds alone."*
  - Yet in the owner's own worked banking example (`docs/Working/what-i-want-2026-07-16.md:104`), `PlanRepayment` computes `MonthlyRepayment = -Balance / PlanRepayment.Months` (a quotient of two variables—non-linear) and the owner explicitly states that *"the `nonnegative` bound on `MonthlyRepayment` is closed by the guard (`Balance < 0.0` makes `-Balance` positive)"* together with `Months positive`—using sign reasoning alone, without any guard restating `-Balance / PlanRepayment.Months >= 0`.
  - Similarly, `what-i-want-2026-07-16.md:135` draws the line specifically at a *relational rule whose post-state condition is non-linear* (such as `MonthlyRepayment <= OverdraftLimit * RepaymentCapPercent`, comparing a quotient against a product of four free variables), NOT at sign/interval propagation through products or quotients (`Total <- Price * Quantity` with `Price nonnegative` and `Quantity nonnegative` proving `Total >= 0`, as in `docs/compiler/proof-engine.md` Strategy 10).
- **Inference and reasoning:**
  - By over-tightening `what-i-want-2026-07-16.md:135` into a blanket ban against using sign or monotonicity facts to prove any non-linear rule from field bounds alone (`design-completion-plan.md:133–134`), the plan forbids proving both the owner's own `MonthlyRepayment nonnegative` example (`want:104`) and trivial sign-of-product bounds (`nonnegative * nonnegative >= 0`), violating Rule 4 (full intended proof power).

### 3.3 The plan's conflict list (§ 2.3) misses major direct contradictions already sitting in canonical documents

- **Severity:** Critical
- **Evidence:**
  - `docs/language/business-domain-types.md:1834–1837`, `docs/language/business-domain-types.md:1892`, `docs/language/business-domain-types.md:1921`, `docs/language/business-domain-types.md:1985`, `docs/language/business-domain-types.md:2022–2030`, `docs/language/business-domain-types.md:2043`
  - `docs/language/precept-language-spec.md:1994` vs `docs/runtime/evaluator.md:1975–1977` vs `docs/working-reset/design-completion/design-completion-plan.md:116`
  - `docs/runtime/precept-builder.md:761`, `docs/runtime/precept-builder.md:813` vs `docs/runtime/evaluator.md:1879`, `docs/runtime/evaluator.md:1914`, `docs/runtime/evaluator.md:2005`
  - `docs/runtime/runtime-api.md:770–798`
  - `docs/language/precept-grammar.md:854`
  - `docs/compiler/soundness-and-coverage.md:208–215` vs `CLAUDE.md:83–101`
  - Compare against `docs/working-reset/design-completion/design-completion-plan.md:226–250`
- **Verified in repository:**
  None of the following verified canonical contradictions appear in the plan's conflict inventory (§ 2.3, `design-completion-plan.md:226–250`):
  1. **Self-contradiction inside `docs/language/business-domain-types.md`:**
     - Line `1921` specifies that `Money`, `Quantity`, `Price`, and `ExchangeRate` are pure data records with *"No arithmetic operators, no validation logic, no named computation methods"* and all computation lives in static executor modules (`MoneyOperations`, etc.). Ninety lines later, lines `2022–2030` specify the exact opposite: value objects named `MoneyValue`, `QuantityValue`, `PriceValue`, `ExchangeRateValue` with C# operator overloads (`MoneyValue.operator+`) AND runtime domain-precondition validation surfacing `ConstraintViolation` (which also contradicts prove-or-reject).
     - Line `1892` states that `exchangerate`'s implicit `positive` constraint is enforced at *"set time (runtime)"*, contradicting compile-time prove-or-reject (`precept-language-spec.md` §0.7).
     - Line `1834` states that `QualifierMismatch` follows a *"proven-violation-only policy"*, yet line `1837` rejects unproven assignments.
     - Line `2043` refers to an *"entity-scoped units block"* that line `1985` explicitly rejects as out of scope.
  2. **Computed fields during `Restore`:**
     - `docs/language/precept-language-spec.md:1994` and `design-completion-plan.md:116` state that computed fields are recomputed on every operation, explicitly including `Restore`.
     - `docs/runtime/evaluator.md:1975–1977` explicitly states that `FromJson` (`Restore`) does **not** recompute computed fields and leaves stored computed values stale until the caller runs `Update()` or `InspectUpdate()`.
  3. **Runtime storage and evaluation stack representation:**
     - `docs/runtime/precept-builder.md:761` specifies entity state as boxed `object?[]` and line `813` specifies a heap-allocated `new Stack<PreceptValue>()`.
     - `docs/runtime/evaluator.md:1879, :1914, :2005` mandates unboxed 32-byte `PreceptValue[]` arrays and stack-allocated evaluation spans.
  4. **Stale CLR type mapping table in `docs/runtime/runtime-api.md:770–798`:**
     - Lists retired or nonexistent DSL types (`text`, `stateref`, `list of T keyed by K`, `map K to T`) and omits current canonical collection and text types (`set`, `bag`, `queue`, `stack`, `queue of T by P`, `log`, `log of T by P`, `lookup of K to V`, `~string`).
  5. **Defensive runtime faults contradicted in `docs/language/precept-grammar.md:854`:**
     - Defines the `Faults` catalog as *"defensive coverage for paths the compiler proved safe"*, directly contradicting `docs/runtime/fault-system.md`, `docs/compiler-and-runtime-design.md:165–175`, and `precept-language-spec.md` §0.7.
  6. **Catalog authority contradiction in `docs/compiler/soundness-and-coverage.md:208–215`:**
     - Still treats `FaultCode.cs` and `ProofRequirementKind.cs` as closed authorities even though `CLAUDE.md:83–101` suspended catalog authority precisely because `ProofRequirementKind` lacks establishment and preservation and five of eight write sites are uncatalogued.
- **Inference and reasoning:**
  - The plan built its conflict inventory from earlier phase-2 working logs rather than a fresh verification against the actual canonical files. Because Step 14 assumes the hard contradictions are already captured in § 2.3 and Step 12 treats runtime cleanup as mostly mechanical internal decisions, these substantive design contradictions will either be missed or trigger surprise halts late in the plan.

### 3.4 The plan misstates the owner's parking condition on runtime `Fault` vs exception and fails to plan the canonical cleanup

- **Severity:** Critical
- **Evidence:**
  - Plan: `docs/working-reset/design-completion/design-completion-plan.md:618`, `docs/working-reset/design-completion/design-completion-plan.md:882`
  - Canon: `docs/runtime/fault-system.md:309`, `docs/runtime/evaluator.md:2010`, `docs/runtime/README.md:36`
- **Verified in repository:**
  - `design-completion-plan.md:882` claims the owner parked the runtime `Fault`-versus-exception question *"until we start building the runtime"*, and therefore grants it a blanket permanent exception from Finish Conditions 6 and 7 (`design-completion-plan.md:882–883`).
  - Yet both `docs/runtime/fault-system.md:309` and `docs/runtime/evaluator.md:2010` record the exact opposite wording: *"The owner has parked this until the runtime design is done."*
  - Furthermore, `design-completion-plan.md:618` cites only `docs/runtime/README.md:36` for the carve-out and never aligns `fault-system.md:309` or `evaluator.md:2010`.
- **Inference and reasoning:**
  - "Until the runtime design is done" falls inside this plan (at the end of Step 12), whereas "until we start building the runtime" pushes it past this plan entirely. Worse, leaving `Fault` vs thrown exception unresolved leaves the public return-type contract of `Fire`, `Update`, `Create`, and `Restore` (`docs/runtime/result-types.md`) and the `ArrayPool` mid-row abandonment question (`docs/runtime/evaluator.md:2019`) permanently ambiguous—which violates the owner's Rule 1 ("A completed and consistent design first").

### 3.5 Step 2 overlooks irrational UCUM plane-angle units and decimal rounding inside duration cancellation

- **Severity:** Major
- **Evidence:**
  - Plan: `docs/working-reset/design-completion/design-completion-plan.md:145`, `docs/working-reset/design-completion/design-completion-plan.md:455–467`
  - Canon: `docs/language/catalog-system.md:1395` (UCUM atom metadata `ScaleIsRational`), `docs/language/business-domain-types.md:1857`, `docs/language/business-domain-types.md:2017`
- **Verified in repository:**
  - `design-completion-plan.md:145` states that UCUM exact rational conversion excludes only logarithmic units (`dB`, `[pH]`).
  - However, `docs/language/catalog-system.md` (UCUM atom metadata) explicitly records that `ScaleIsRational` is `false` for TWO families: logarithmic units (`B`, `Np`, `dB`) AND plane-angle units (`rad`, `deg`, `gon`, `'`, `''`), whose reduction passes through $\pi$ and is stored only as a rational approximation.
  - Additionally, `docs/language/business-domain-types.md:1857, :2017` defines `duration` scalar extraction in time-unit cancellation as `(decimal)duration.ToInt64Nanoseconds() / <nanoseconds-per-unit>m` (for example, dividing by `3_600_000_000_000m` for hours or `60_000_000_000m` for minutes), which produces non-terminating base-10 decimals (`1/3`, `1/6`, `1/3600`) and forces decimal rounding inside temporal-to-money/price multiplication.
- **Inference and reasoning:**
  - Step 2 treats UCUM normalization as exact outside logarithmic units and treats temporal arithmetic separately from decimal rounding. A sound proof model for `quantity`, `price`, and `duration` must account for irrational-scale plane-angle units (either excluding them from exact cross-unit proofs or bounding their approximation error) and for the base-10 rounding introduced when `duration` cancels against `hours` or `minutes`.

### 3.6 Splitting dead-row fixed-point termination (Step 9) from simultaneous induction soundness (Step 11) hides a real cyclic interaction

- **Severity:** Major
- **Evidence:** `docs/working-reset/design-completion/design-completion-plan.md:584–585`, `docs/working-reset/design-completion/design-completion-plan.md:590`, `docs/working-reset/design-completion/design-completion-plan.md:600–604`
- **Verified in repository:**
  - Step 9 (`design-completion-plan.md:584–585`) asserts as a deliverable *"a demonstration that the loop with obligations on unreachable states ends in one pass"*.
  - Step 11 (`design-completion-plan.md:600–604`) separately owns the simultaneous induction on run length that justifies dropping dead rows while assuming rules hold only over live rows.
- **Inference and reasoning:**
  - The claim that the dead-row / unreachable-state loop "ends in one pass" is false in general whenever reachability feeds back into proof premises (for example, removing a dead row can make state `B` unreachable, which eliminates the only transition from `B` to `C`, which can strengthen the set of facts guaranteed upon entering `C`, which in turn can render another guard in `C` unsatisfiable).
  - Even on a finite state graph where fixed-point iteration terminates in at most $|S| + |R|$ passes (not one pass), proving termination for a one-pass procedure in Step 9 separately from soundness of the simultaneous induction in Step 11 risks proving an unsound one-pass shortcut or discovering in Step 11 that Step 9's algorithm must be redesigned.

### 3.7 The plan assigns Step 10/12 agents to resolve public API and language gaps that are caller- or author-visible

- **Severity:** Major
- **Evidence:**
  - Plan: `docs/working-reset/design-completion/design-completion-plan.md:222`, `docs/working-reset/design-completion/design-completion-plan.md:593–598`, `docs/working-reset/design-completion/design-completion-plan.md:697–701`
  - Canon: `docs/compiler/literal-system.md:569–575`, `docs/compiler/literal-system.md:628–629`, `docs/language/primitive-types.md:669`, `docs/language/collection-types.md:533`, `docs/runtime/precept-builder.md:841–845`, `docs/runtime/evaluator.md:2012–2015`, `docs/runtime/result-types.md:149`, `docs/runtime/result-types.md:249`, `docs/runtime/runtime-api.md` (no clock API), `docs/language/temporal-type-system.md:563`
- **Verified in repository:**
  - The plan authorizes agents to settle *"internals nobody outside can observe"* without asking the owner (`design-completion-plan.md:697–701`).
  - Yet the canonical docs contain numerous **externally observable** language and public-API open questions that are missing from the plan's owner question list (§ 6.2):
    - `docs/compiler/literal-system.md:569–575`: String interpolation rendering for all 7 business-domain types plus `choice` is explicitly unbuilt and marked as *"a cluster of determinism decisions... not a mechanical fill-in"*; line `628–629` leaves `'10 percent'` type and semantics TBD.
    - `docs/language/primitive-types.md:669`: Adding `not-NaN` semantics to `nonzero`/`positive`/`nonnegative` on `number` is marked as *"a modifier-semantics change and is designed before it ships"*.
    - `docs/language/collection-types.md:533`: Whether `.min`/`.max` apply to `set of period in 'days'` is explicitly marked *"a separate decision, not yet taken"*.
    - `docs/runtime/runtime-api.md`: Contains zero clock or `TimeProvider` injection API, even though `docs/language/temporal-type-system.md:563` defines `now()` and `design-completion-plan.md:222` assigns "the clock" to Step 12.
    - `docs/runtime/precept-builder.md:841–845`: How the proof certificate crosses into `Precept.From` and whether certificate-gate failure throws or returns a structured result are public runtime API contracts.
    - `docs/runtime/evaluator.md:2012–2015` and `docs/runtime/result-types.md:149, :249`: The public type carrying proof provenance on `TransitionInspection` / `UpdateInspection` / `EventInspection`, and the public shape of `InvalidArgs`, are caller-facing .NET types.
- **Inference and reasoning:**
  - None of these are invisible internals. Either Step 10/12 agents will invent public .NET APIs and language formatting/operator rules unilaterally (violating the owner's authority and `CLAUDE.md`'s pre-design consultation rules for new language constructs), or they will blow past the plan's 15-question cap (`design-completion-plan.md:972–975`).

### 3.8 Unreconciled compile-to-runtime boundary between `ProofLedger`, `ConstraintInfluenceMap`, and the proof certificate

- **Severity:** Major
- **Evidence:**
  - `docs/compiler/proof-engine.md:107`, `docs/compiler/proof-engine.md:2602`
  - `docs/runtime/precept-builder.md:10`, `docs/runtime/precept-builder.md:66`, `docs/runtime/precept-builder.md:785–788`, `docs/runtime/precept-builder.md:800–804`, `docs/runtime/precept-builder.md:845`
  - `docs/working-reset/design-completion/design-completion-plan.md:593–598`
- **Verified in repository:**
  - `docs/compiler/proof-engine.md:107` and `:2602` state that `ProofLedger` *"does NOT cross the compile-runtime boundary — nothing from it crosses into runtime."*
  - Yet `docs/runtime/precept-builder.md:10, :66, :785–788` explicitly consumes `ProofLedger` (to build `ConstraintInfluenceMap` from `ConstraintInfluenceEntries`), notes that `ConstraintPlanIndex` may be replaced by a premise list derived from the certificate (`:800–804`), and requires a certificate gate at `Precept.From` (`:845`).
- **Inference and reasoning:**
  - Step 10 (`design-completion-plan.md:593–598`) focuses on certificate format and checker soundness, while Step 12 cleans up runtime internals. Because nobody owns the single unified artifact contract across `ProofEngine → Compilation → PreceptBuilder → Precept` (what crosses: certificate, influence edges, inspection provenance, ingress premise list), the current contradiction between `proof-engine.md` and `precept-builder.md` risks surviving the split.

### 3.9 Editable-field ingress is treated as settled by `want:145` despite a newer owner ruling keeping it open

- **Severity:** Major
- **Evidence:**
  - Plan: `docs/working-reset/design-completion/design-completion-plan.md:208–215`, `docs/working-reset/design-completion/design-completion-plan.md:259`, `docs/working-reset/design-completion/design-completion-plan.md:442–448`, `docs/working-reset/design-completion/design-completion-plan.md:945–947`
  - Owner ruling: `docs/working-reset/phase-2/trace-walk-rulings.md:42–46`
- **Verified in repository:**
  - On 2026-08-26 (`docs/working-reset/phase-2/trace-walk-rulings.md:42–46`), the owner ordered the removal of narrowing sentences on editable-field ingress specifically because *"he did not want to settle the underlying question now"*.
  - Yet `design-completion-plan.md:214` directs Step 1 to build the proof model directly around `want:145` (editable writes check all rules mentioning the edited field), omits editable writes from Step 1's waiting-on-owner list (`:259`), and lists it only as a conditional escalation if an attacker later finds a concrete flaw (`:945–947`).
- **Inference and reasoning:**
  - If Step 1 locks editable-field ingress to `want:145` and builds Steps 4, 6, 10, and 11 on top of it without confirming with the owner first, any disagreement from the owner at the Step 11 proof-model sign-off invalidates the entire proof model after all 11 steps have already run.

### 3.10 Minor canonical inconsistencies omitted from the plan's baseline

- **Severity:** Minor
- **Evidence:**
  - `docs/compiler/lexer.md:677`, `docs/compiler/lexer.md:720` (uses obsolete prototype syntax `field X : set<string>` and `set X to ...` instead of `as` and `=`)
  - `docs/compiler/type-checker.md:1110` (still references retired `writable` modifier and `TypedEditDeclaration`)
  - `docs/runtime/result-types.md:363` (states `Update` was renamed from `Edit` to avoid confusion with DSL `edit` declarations, which were replaced by `modify`)
  - `docs/compiler/diagnostic-system.md:732, :748–753, :757` (bans numeric `PRECEPTnnn` codes while using `PRE0116`/`PRE0104`/`PRE0020` and linking to three nonexistent root docs: `docs/ArchitecturalCoherenceDesign.md`, `docs/ComponentAlignmentInventory.md`, `docs/CatalogInfrastructureDesign.md`)
  - `docs/compiler/graph-analyzer.md:704` vs `docs/language/precept-language-spec.md:187` (iterative dataflow dominators vs Lengauer-Tarjan algorithm)
  - `docs/language/README.md` (omits 3 of the 9 files in `docs/language/`: `catalog-system.md`, `graph-analyzer-roadmap.md`, `precept-grammar.md`)
  - `docs/compiler/soundness-and-coverage.md:323` and `docs/language/catalog-system.md:14` (canonical files linking directly into `docs/Working/`, which `design-completion-plan.md:849` forbids)
  - `docs/language/precept-language-spec.md:2200` and `:2204` (duplicated Open Question paragraphs on the write list being only half catalogued, four lines apart)
- **Verified in repository:** All eight items were verified directly at the cited lines.
- **Inference and reasoning:** Individually small, these confirm that the canonical corpus has far more live drift than the plan's pre-counted conflict inventory suggests, directly aggravating the `Found faster than closed` stop rule (§ 2.2 above).

### 3.11 Plan's no-code rule is slightly undercut by its own short-name line-number references

- **Severity:** Minor
- **Evidence:** `docs/working-reset/design-completion/design-completion-plan.md:13–15`, `docs/working-reset/design-completion/design-completion-plan.md:21–32`
- **Verified in repository:** The plan states *"This page uses no lookup codes"* (`:13`), yet readers without the scratchpad/lineage files cannot resolve citations such as `v2:516`, `req:748-750`, or `review:...` because two of those short names point to broken paths (`:29–30`) and `req` (`inputs/requirements.md`) is not even part of the reviewable brief set.
- **Inference and reasoning:** Minor hygiene defect, easily fixed by citing canonical repository paths directly and removing references to superseded drafts (`v2`, `review`).

---

## 4. What Does It Ask of the Owner That It Does Not Admit to Asking?

Section 6.5 (`design-completion-plan.md:1066–1095`) presents a reassuringly small bill: **14 certain decisions or reads plus 16 start lines**, with bounded conditional add-ons. That accounting omits or severely understates six substantial demands on the owner's time.

### 4.1 Uncounted stop-rule halts that only the owner can clear

- **Severity:** Critical
- **Evidence:** `docs/working-reset/design-completion/design-completion-plan.md:373–378`, `docs/working-reset/design-completion/design-completion-plan.md:774–775`, `docs/working-reset/design-completion/design-completion-plan.md:794–812`, `docs/working-reset/design-completion/design-completion-plan.md:1066–1095`
- **Verified in repository:**
  - Section 6.5 counts **zero** stop-rule interventions.
  - Yet the plan establishes seven separate halt conditions that explicitly require the owner's words or `settings.json` approval to resume: `step0-guard` (`:373–378`), `Records outgrow the design` (`:808–810`), `Found faster than closed` (`:804–807`), `Cascading repairs` (`:794–795`), `Whole model` (`:796–801`), second `Reopened work rising` (`:774–775, :802–803`), and `uses/` width violations (`:811–812`).
- **Inference and reasoning:**
  - As shown in Sections 1.1, 2.1, 2.2, and 2.5, `Records outgrow the design` alone will mathematically fire on nearly every proof-model step (up to 11 times), and `Found faster than closed`, `Cascading repairs`, and `uses/` checks add multiple additional halts. Every halt forces the owner to read diagnostic notices, diagnose whether the problem is real or a process false alarm, write clearance text, and manually restart the step.

### 4.2 Uncounted weekly usage-limit stops and fallback restarts

- **Severity:** Critical
- **Evidence:** `docs/working-reset/design-completion/design-completion-plan.md:1058`, `docs/working-reset/design-completion/design-completion-plan.md:1076–1083`, `docs/working-reset/design-completion/design-completion-plan.md:1155–1156`, `docs/working-reset/design-completion/design-completion-plan.md:1168–1176`, `docs/working-reset/design-completion/design-completion-plan.md:1197–1203`
- **Verified in repository:**
  - Section 6.5 counts **16 start lines** (`:1083`), assuming roughly one start line per step.
  - Yet the plan requires 48 Opus 5.5 calls per argument across 12 attack rounds (`:1168–1171`), plus Fable 5.2 drafting, repairs, second-author passes, dependency extractors, `uses/` diff scanners, and 10,000+ rows in `dedup/pairs.jsonl`.
  - Section 7.5 (`:1173–1176`) admits that hitting the weekly subscription usage ceiling pauses work until the owner notices after reset and manually enters `Run the design-completion plan` again (`:1058`), and Test 8's fallback caps consecutive runs at **5 in a row** (`:1155–1156`).
- **Inference and reasoning:**
  - A multi-thousand-call workload on top-tier reasoning models (`Fable 5.2` and `Opus 5.5`) will hit weekly usage ceilings repeatedly over many weeks, and if any of Tests 7, 8, or 11 fall back, the owner will be typing start lines dozens or hundreds of times instead of 16—and possibly leaving an interactive terminal open overnight (`:1201–1203`).

### 4.3 Two multi-turn `/design` sessions disguised as "2" single items

- **Severity:** Major
- **Evidence:** `docs/working-reset/design-completion/design-completion-plan.md:659–660`, `docs/working-reset/design-completion/design-completion-plan.md:1079`, `CLAUDE.md` (Pre-Design Owner Consultation & `/design` skill rules)
- **Verified in repository:**
  - Section 6.5 counts the two conversations on new language (temporal point bounds; quantifier binding and ordering keywords) as `2` items on the same footing as a single yes/no approval (`:1079`).
  - Under `CLAUDE.md`, new language constructs require pre-design owner consultation followed by the full `/design` workflow (research, four-part rationale, language-design grounding, architecture grounding, review, and sign-off).
- **Inference and reasoning:**
  - Each of those two items is an interactive, multi-turn design collaboration spanning research review, syntax/semantics tradeoffs, and iteration—representing hours of owner engagement, not two quick answers.

### 4.4 Heavy reading and auditing load compressed inside the two "1"-counted sign-offs and every notice

- **Severity:** Major
- **Evidence:** `docs/working-reset/design-completion/design-completion-plan.md:846–847`, `docs/working-reset/design-completion/design-completion-plan.md:908–911`, `docs/working-reset/design-completion/design-completion-plan.md:1027–1040`, `docs/working-reset/design-completion/design-completion-plan.md:1080–1081`, `docs/working-reset/design-completion/design-completion-plan.md:1277–1279`
- **Verified in repository:**
  - The proof-model sign-off (counted as `1` read, `:1080`) requires the owner to read: the entire worked banking example under the new model, every compiler refusal with its stated reason, every proposed ruling retirement, and every canonical edit that cited no ruling (`:1027–1031`).
  - The finish sign-off (counted as `1` read, `:1081`) requires reading all seven condition counts, the four-group sort of every change made across all 33 canonical documents, all remaining ruling retirements, `CLAUDE.md` edits, and up to three `docs/philosophy.md` edits (`:1032–1040`).
  - Every question batch also attaches the full dropped-questions list and audit error rate for review (`:908–911`).
  - Crucially, `design-completion-plan.md:1277–1279` admits that nothing mechanically proves a row written into `answers.md` actually came from the owner—so every notice echoes the `answers.md` diff and relies on **the owner** to visually audit it every single time (`:846–847`).
- **Inference and reasoning:**
  - Compressing full-corpus change audits, ruling retirements, dropped-question reviews, and recurring `answers.md` integrity checks into two line items and uncounted per-notice boilerplate substantially understates the owner's actual reading burden.

### 4.5 Uncounted public-API and language decisions hidden in canon

- **Severity:** Major
- **Evidence:** Same canonical citations as Finding 3.7 (`docs/compiler/literal-system.md:569–575, :628–629`, `docs/language/primitive-types.md:669`, `docs/language/collection-types.md:533`, `docs/runtime/precept-builder.md:841–845`, `docs/runtime/evaluator.md:2012–2015`, `docs/runtime/result-types.md:149, :249`, `docs/runtime/runtime-api.md`), plus `docs/working-reset/design-completion/design-completion-plan.md:972–975`
- **Verified in repository:** At least eight externally observable language or public .NET API questions are explicitly open in canon and absent from § 6.2.
- **Inference and reasoning:** These either force an uncounted "raise the 15-sheet cap" interruption (`:972–975`) or push the owner into auditing unwanted agent-invented public APIs at the final sign-off.

### 4.6 Uncounted security/permission review and environment configuration

- **Severity:** Major
- **Evidence:** `docs/working-reset/design-completion/design-completion-plan.md:401–403`, `docs/working-reset/design-completion/design-completion-plan.md:1076`, `docs/working-reset/design-completion/design-completion-plan.md:1192–1194`
- **Verified in repository:**
  - Step 0 generates `.claude/settings.local.json` with pre-approved shell and tool permissions so agents can run unattended loops and git worktree commands, and asks the owner to review and approve it (`:401–403`).
  - Section 7.6 (`:1192–1194`) also notes that the owner may need to disable the repository's Precept MCP server during design completion to stop agents from querying stale compiler behavior.
- **Inference and reasoning:** Auditing unattended shell-execution allowlists in `.claude/settings.local.json` is a non-trivial security review on the owner's personal machine, not a trivial one-line sign-off.

---

## 5. What Would You Change?

To satisfy all five of the owner's rules—a complete and consistent design with full compiler/runtime detail, single-owner deduplication, full intended proof power, and genuinely low owner effort—restructure the plan around six concrete changes:

### 5.1 Drop Step 0's 4,000-line meta-platform; keep only 4 lightweight, deterministic checks

- Do not build 15 custom orchestration scripts, 4 workflow engines, concurrent git worktrees, `locked-write`, or `trailer-check`. Run one sequential working branch so there are no merge-conflict or worktree-locking bugs.
- Keep only lightweight, fast, deterministic Python checks (building directly on `docs/working-reset/phase-2/checks/`):
  1. Broken markdown link and path check (catching nonexistent docs like `docs/ArchitecturalCoherenceDesign.md` and forbidden links from canon into `docs/Working/`).
  2. Pending-marker and open-question inventory check (listing every unresolved marker or `TBD` in `docs/`).
  3. Banned-word and writing-style lint (checking `CLAUDE.md` writing rules automatically).
  4. Catalog/type-table drift check (verifying canonical type names like `set`, `bag`, `queue`, `stack`, `log`, `lookup`, `~string` against `runtime-api.md`, `lexer.md`, `type-checker.md`, and `precept-grammar.md`).

### 5.2 Replace the 12-round × 4-agent attack factory and 10× record-ratio halt with a bounded 3-pass review loop

- Instead of 12 mandatory rounds × 4 agents (48 calls per argument) plus a 10:1 record-to-canon cap that guarantees self-halting, use a converging **Draft → Attack → Repair → Verify** cycle per milestone:
  - **Pass 1 (Draft):** One strong author drafts the milestone's complete canonical section and worked examples (including concrete failure/rejection cases).
  - **Pass 2 (Independent Adversarial Critique):** One fresh reviewer attempts to construct concrete counterexamples (arithmetic, boundary, temporal, collection, state-action, or induction flaws) and checks consistency against upstream canonical sections.
  - **Pass 3 (Repair & Re-verify):** The author fixes every confirmed counterexample, and a fresh reviewer verifies ONLY the repaired claims and any directly affected downstream clauses—stopping after a clean pass (or escalating to the owner if a core assumption fails twice).
- Delete `Records outgrow the design`, `Found faster than closed`, `uses/` diff-halting, and `Whole model` fraction traps. Replace them with one simple budget rule: **max 2 repair cycles per section before escalating a concrete decision sheet to the owner**.

### 5.3 Front-load ALL true owner decisions in one clean intake batch before drafting the proof model

- Eliminate the deadlocking multi-option pending-marker scheme (`design-completion-plan.md:274–277` vs `:695–696`). Put every genuinely open owner decision into **one concise, structured decision packet** up front (grouped into quick rulings vs the two `/design` sessions) so Steps 1–11 run on settled ground:
  1. The 3 state-action semantics questions (multiplicity/order, construction entry, self-transition entry).
  2. `from any` expansion semantics.
  3. Editable-field ingress scope (explicitly confirming `want:145` vs narrower ingress, since `trace-walk-rulings.md:42–46` kept it open).
  4. Integer/decimal overflow posture (fixed-width under prove-or-reject vs arbitrary precision for `integer`).
  5. Reachability posture (explicit owner decision on whether to override locked canon: keep structural over-approximation plus dead-row removal per `precept-language-spec.md:199–201`, OR authorize bounded-depth path verification with explicit conservative fallback—never silently require exact concrete-run synthesis).
  6. Non-linear rule boundary (explicitly confirm that sign/interval propagation through products and quotients like `want:104` `-Balance / Months >= 0` and `Price * Quantity >= 0` IS permitted, while relational non-linear rules like `MonthlyRepayment <= OverdraftLimit * RepaymentCapPercent` require an author guard).
  7. Runtime `Fault` vs thrown exception (resolving `fault-system.md:309` and `evaluator.md:2010` before freezing `result-types.md`), plus the small bundle of caller-visible API decisions (`Precept.From` certificate failure shape, clock injection API, `Restore` computed-field freshness, `.min`/`.max` on `set of period in 'days'`, business-domain string interpolation formatting, `number`-lane `not-NaN` modifiers).
  8. The two `/design` conversations (temporal point bounds; quantifier binding and ordering keywords).

### 5.4 Collapse the 16 fragmented steps into 5 coherent, acyclic milestones

Replace the 16-step dependency web (including the Step 6 ↔ Step 9 cycle and the Step 14 revert-churn loop) with 5 sequential milestones:

1. **Milestone A — Owner Decisions & Known-Contradiction Baseline:** Collect the intake batch above and compile one verified contradiction list from the actual canonical files (including all findings in Section 3.3 and 3.10).
2. **Milestone B — Core Semantics, Types, and Arithmetic Domains:** Settle operations, write sites, state actions, editable ingress, numeric/overflow/rounding (including `duration` cancellation rounding and irrational UCUM plane-angle units), collections, and temporal semantics directly in `docs/language/`.
3. **Milestone C — Unified Proof Model, Reachability, and Certificates:** Design premises, inference rules, decision procedures, dead-row/reachability fixed-point interaction (bounded finite-pass iteration, not a false "one pass" claim), the simultaneous induction soundness argument, and the single compile-to-runtime artifact contract (`ProofLedger` / `ProofCertificate` / `ConstraintInfluenceMap` / inspection provenance) together as one cohesive unit—breaking the Step 6 ↔ Step 9 and Step 9 ↔ Step 11 splits. Checkpoint with the full banking example (`what-i-want-2026-07-16.md`) and every one of its 14 must-reject mutations.
4. **Milestone D — Compiler, Runtime, and Tooling Alignment:** Resolve all remaining internal contradictions in `docs/compiler/`, `docs/runtime/`, and `docs/tooling/` (box-free `PreceptValue` storage, `MoneyOperations` vs `MoneyValue`, `runtime-api.md` CLR types, `Restore` computed-field freshness, `Precept.From` certificate gate, clock injection, stale `lexer.md` / `precept-grammar.md` / `type-checker.md` / `diagnostic-system.md`).
5. **Milestone E — Topic-Owned Deduplication and End-to-End Walk:** Instead of a 33 × N `dedup/pairs.jsonl` matrix, assign each of the ~20–30 major architectural topics (e.g., operation execution order, prove-or-reject contract, CLR type mapping, write-site enumeration, certificate gate, diagnostic severity) to ONE canonical owner document, sweep the other 32 documents by topic to replace duplicated prose with links to the owner, and run the 7-operation hand walk (`Create`, `Fire`, `Update`, `Restore`, `Inspect`, `InspectUpdate`, `Compile`) across the finished docs before the final owner sign-off.

### 5.5 Give the owner an honest interaction budget

Replace § 6.5's artificially compressed count with an honest schedule:
- **1 upfront decision packet** (quick rulings on the settled list above) + **2 interactive `/design` sessions** (temporal bounds; quantifiers).
- **1 mid-point design sign-off** after Milestone C (proof model + banking example + all 14 rejection mutations).
- **1 final sign-off** after Milestone E (contradiction closure + topic deduplication summary + 7-operation walk).
- **Explicit acknowledgement** that multi-milestone runs may still pause on subscription usage ceilings, with clean milestone boundaries so resuming takes one short prompt without lost work.

---

## Coverage

Every file named in the review brief was opened and read directly in the repository (`spike/Precept-V2-Radical-design-completion`, commit `5aa417ea`). Zero named files were left unread.

### Named Files Read in Full (23 of 41)

1. `docs/working-reset/design-completion/design-completion-plan.md` (1,302 lines)
2. `docs/Working/what-i-want-2026-07-16.md` (221 lines)
3. `docs/working-reset/design-completion/inputs/history-failures.md` (615 lines)
4. `docs/working-reset/design-completion/inputs/history-rulings.md` (269 lines)
5. `docs/working-reset/design-completion/inputs/technical-proof-model.md` (482 lines)
6. `docs/working-reset/design-completion/inputs/technical-precedent.md` (305 lines)
7. `docs/working-reset/design-completion/inputs/technical-canon-consistency.md` (246 lines)
8. `docs/philosophy.md` (115 lines)
9. `docs/language/README.md` (45 lines)
10. `docs/language/graph-analyzer-roadmap.md` (84 lines)
11. `docs/language/primitive-types.md` (748 lines)
12. `docs/compiler/README.md` (90 lines)
13. `docs/compiler/grammar-generator.md` (279 lines)
14. `docs/compiler/graph-analyzer.md` (844 lines)
15. `docs/compiler/name-binder.md` (401 lines)
16. `docs/compiler/soundness-and-coverage.md` (357 lines)
17. `docs/runtime/README.md` (83 lines)
18. `docs/runtime/descriptor-types.md` (217 lines)
19. `docs/runtime/fault-system.md` (355 lines)
20. `docs/runtime/result-types.md` (455 lines)
21. `docs/tooling/README.md` (35 lines)
22. `docs/tooling/extension.md` (188 lines)
23. `docs/tooling/mcp.md` (416 lines)

### Named Files Read in Part (18 of 41)

1. `docs/working-reset/compiler-readiness-plan.md` (2,278 lines) — read lines 1–371, 1450–1860
2. `docs/language/business-domain-types.md` (2,099 lines) — read lines 1800–2099
3. `docs/language/catalog-system.md` (1,601 lines) — read lines 1–220, 1350–1601
4. `docs/language/collection-types.md` (1,555 lines) — read lines 500–620, 1240–1541
5. `docs/language/precept-grammar.md` (960 lines) — read lines 820–960 (plus targeted searches across file)
6. `docs/language/precept-language-spec.md` (2,224 lines) — read lines 1–380, 1200–1610, 1750–2224
7. `docs/language/temporal-type-system.md` (1,947 lines) — read lines 530–630, 1650–1873
8. `docs/compiler/diagnostic-system.md` (802 lines) — read lines 580–802
9. `docs/compiler/lexer.md` (757 lines) — read lines 600–757
10. `docs/compiler/literal-system.md` (655 lines) — read lines 500–655
11. `docs/compiler/parser.md` (572 lines) — read lines 430–572
12. `docs/compiler/proof-engine.md` (2,653 lines) — read lines 1–260, 1650–2653
13. `docs/compiler/tooling-surface.md` (1,293 lines) — read lines 1100–1293
14. `docs/compiler/type-checker.md` (1,210 lines) — read lines 1000–1210
15. `docs/runtime/evaluator.md` (2,183 lines) — read lines 1–110, 1850–2128
16. `docs/runtime/precept-builder.md` (968 lines) — read lines 1–120, 700–968
17. `docs/runtime/runtime-api.md` (928 lines) — read lines 1–110, 700–928 (plus targeted searches across file)
18. `docs/tooling/language-server.md` (1,641 lines) — read lines 1250–1641

### Named Files Could Not Open or Did Not Read (0 of 41)

None. Every file named in the brief was opened and inspected.

### Additional Supporting Repository Files Inspected (Not Named in Brief)

- `CLAUDE.md` — read in full
- `docs/working-reset/design-completion/inputs/rulings-routes.md` — read in full
- `docs/compiler-and-runtime-design.md` — read lines 1–215
- `docs/working-reset/phase-2/trace-walk-rulings.md` — read lines 1–80
- `docs/working-reset/phase-2/five-items-walk-2026-08-27.md` — read lines 90–145
- `docs/working-reset/phase-1/the-walk.md` — read lines 185–220, 825–855
- Directory listings verified for `docs/working-reset/phase-2/checks/`, `docs/working-reset/phase-5/`, `docs/working-reset/design-completion/lineage/`, and `.claude/`.
