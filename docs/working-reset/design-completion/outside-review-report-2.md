# Second outside review: do not adopt the design-completion plan as written

**Reviewer:** GPT-6.2 Astra (`gpt-6.2-astra`). **Revision reviewed:** `39c44ce3`.

The plan can finish with a proof that is valid for firing an event and invalid for previewing that very event. It can also stop a successful design run solely because it produced records rather than edits. Neither problem is a reason to weaken Precept or to start implementing it. Both are reasons to change what this plan requires before asking the owner to approve it.

Below, `design-completion-plan.md` means the proposed plan in this directory. Other filenames resolve to the paths in **Coverage**. **Verified** describes what the documents say. **Consequence** is my reasoning about those statements, not a report of what the compiler does. I did not use compiler results, tests, sample files, or the first review to decide what Precept should do.

## Where the proposed proof can be wrong

### Critical — Inspection can evaluate a division that dispatch proved it would never reach

**Verified.** The plan lets a later row use the negations of earlier guards as proof premises (`design-completion-plan.md:432-438`). Dispatch really does stop at the first match (`precept-language-spec.md:2005-2006`). Inspection must also be fault-free, including with missing arguments (`precept-language-spec.md:2159-2167`). But the inspection design loops over rows, simulates a row's actions, and **only afterwards** marks it impossible because a preceding row won (`evaluator.md:806-808,837-843,856-866`). The paragraph on undetermined values addresses missing inputs, not this case with complete inputs (`evaluator.md:889-891`).

**Consequence.** Suppose the first row refuses a zero divisor and the fallback divides by it. With zero supplied, firing refuses without dividing. The later division has a valid proof for execution: reaching it means the divisor was not zero. The documented preview evaluates it with zero. More proof search cannot repair an evaluator that runs the expression outside the conditions under which it was proved safe.

**Change.** Include inspection in step 4's list of evaluation sites, before signing off the proof model. Require an account of which premises are true *before* evaluating each guard, action, or message. Walk the zero-divisor case with complete inputs and a priority decision with missing inputs. Include construction preview: the seven-operation walk omits `InspectCreate`, although it is public and explicitly required by tooling (`design-completion-plan.md:628-634`; `runtime-api.md:89,186`; `mcp.md:67-72`).

### Critical — The proposed exit check can certify a state nobody can leave

**Verified.** Step 9 distinguishes reaching a state by a run from creation from exhibiting an input satisfying an exit guard (`design-completion-plan.md:177,521-528`). The owner requires that an entity cannot enter a dead end (`philosophy.md:44`; `what-i-want-2026-07-16.md:151-155`). The current graph document instead checks paths to a terminal in a graph of states (`graph-analyzer.md:437-457`).

**Consequence.** An application enters Review only with approval false. Nothing in Review changes approval. Its only exit requires approval true. A different route from the initial state goes straight to the terminal. Both Review and the terminal have real runs from creation. A model with approval true satisfies the exit guard and any declared rules that say nothing about approval. Yet not one applicant in Review can leave. Even requiring one *reachable* exit example would still miss a variant in which some applicants enter Review approved and others remain trapped there.

**Change.** Specify separately: some run enters each state; a particular row can actually fire; and which reachable configurations must have a route out. Require examples that distinguish the three, including two entries into the same state with different data. Do not treat an assignment satisfying a guard as a sequence of operations. If the owner must distinguish “leave this state” from “can reach completion”, show the observable difference, not a choice between graph algorithms.

### Critical — Restore under a changed definition introduces a base case after proof sign-off

**Verified.** The proof-model sign-off follows step 11. Step 12 then **designs** schema evolution on restore (`design-completion-plan.md:555-575`). Restore is trusted because the stored data was governed when written; an older definition is explicitly outside that inference (`what-i-want-2026-07-16.md:141`; `runtime-api.md:277-281,906`). Verifying the certificate at load verifies a **definition**, not a stored instance (`precept-builder.md:637-643`).

**Consequence.** Version one permits a balance of minus five; version two requires a nonnegative balance. Both definitions can have valid certificates, the persisted data can be intact, and the field can retain exactly the same type and name. Structural checks and both certificates still do not establish the new rule on that balance. The very next handler's assumption that the rule held beforehand is unjustified. This is not answered by deciding who owns migration or by rejecting a stale *certificate*.

**Change.** Before step 11 closes, distinguish same-definition hydration from establishing the starting configuration under a different definition. Require the design to say what identifies the governing definition, what establishes the new rules, and when no governed version is produced. Walk the unchanged-shape, tightened-rule example. Leave implementation and storage details to their proper steps.

### Major — The writer list misses an authored destination, not just catalog entries

**Verified.** The plan describes the missing catalog entries and schedules closing the writer list (`design-completion-plan.md:129,360-375`). The spec singles out a dequeue's collection and its second field, the element destination (`precept-language-spec.md:1988-1999`). The collection design also lets that same operation write the **ordering value into a third field**, named `CurrentSeverity` in its example (`collection-types.md:1201-1212,1288`).

**Consequence.** A rule mentioning only `CurrentSeverity` can change truth when the queue is popped. A list that includes the dequeue action and the element destination can appear complete while omitting this preservation obligation. The bank example that seeds the frozen list has no such operation (`design-completion-plan.md:812-818`).

**Change.** Derive the destinations from each permitted form of an action, including optional captures. For every destination show a rule that reads only that field, and a previously true fact that must be reconsidered after the write. Use the same inventory to challenge the checker's independent enumeration. One row per action name is not enough.

### Major — Being unnecessary to a particular proof does not make an ingress check optional

**Verified.** Step 10 derives runtime checks from the premise list; proof search may change without changing soundness (`design-completion-plan.md:490-493,535-545`). The certificate records the premises **used** by a derivation (`soundness-and-coverage.md:144-151`). Separately, every supplied argument's declared constraints are evaluated, and failures are collected (`evaluator.md:1359-1385`). The owner even identifies a declared bound no other proof consumes (`what-i-want-2026-07-16.md:129,210`).

**Consequence.** Consider an event with a positive argument that its handler does not use. No proof of a calculation needs that bound. Giving the event minus one must still fail at ingress. Taking the union of premises cited by successful proofs need not include it. Likewise, two valid proofs using different bounds must not result in different business behaviour.

**Change.** Make step 10 account for both duties: every declared ingress constraint is enforced and every assumption a proof uses is justified. They may share one record. Require the checker to detect a missing check even when no later calculation relied on it; compare two different valid derivations for the same definition.

### Major — Two claims in the technical inputs would license invalid arithmetic

**Verified.** `technical-proof-model.md:331-332` treats a product with an unwritten field as linear and says two nonzero decimals have a nonzero product. The plan cites that passage for its non-linear work but singles out `number` for the zero-product problem (`design-completion-plan.md:164-167,464-475`). The previous plan explicitly warns that underflow is **not** confined to `number` (`compiler-readiness-plan.md:281`); the finite-decimal example is recorded in `where-a-divisors-non-zeroness-comes-from.md:17-23`. The primitive type document distinguishes decimal operations but does not give multiplication the unlimited precision that claim needs (`primitive-types.md:230-239`).

**Consequence.** A rate unchanged by a handler can still vary between configurations. Multiplying an unknown amount by that unknown rate is not a constant-coefficient linear expression in both fields. Separately, on a model rounding to at most 28 fractional places, `10^-16` is representable, but its square, `10^-32`, rounds to zero under nearest rounding. Calling the inputs exact does not prove the stored divisor nonzero. This counterexample is conditional on the representation, not a demand that the owner retain it.

**Change.** Require the proposed rewrites to state whether a coefficient is known, unchanged, or merely bounded. Give step 2 a smallest-positive-value example for every finite arithmetic model it considers, including decimal-backed business types. Do not import either statement as a proof rule before that model is chosen.

### Major — The plan both invents a conflict and promises a diagnostic it cannot always justify

**Verified.** The plan says the exclusion of general dataflow cannot coexist with induction, and requires the diagnostic to distinguish a true but non-inductive rule from a missing guard (`design-completion-plan.md:175-176,204,501-507`). The proof-engine document excludes tracking values across multiple transitions but explicitly permits induction using rules that held before *one* handler (`proof-engine.md:2606-2614`). The owner requires preservation from the declared premises (`what-i-want-2026-07-16.md:16-24`).

**Consequence.** Proving an initial condition and one preservation implication per handler does not require tracing prior transitions. Conversely, a failed implication gives a configuration the declared rules allow, not necessarily one a run can reach. From that evidence alone the compiler cannot know whether the author omitted a supporting invariant or wrote an unsafe handler. Sometimes no useful guard exists: an action that always assigns minus one to a field required to be nonnegative cannot be made safe by any satisfiable guard. A guard that is always false makes a prohibited dead row (`diagnostic-system.md:185-189`).

**Change.** Keep the induction; retain the dataflow exclusion unless a specific required proof actually needs it. Distinguish a replayable run, a counterexample to preservation from the stated rules, and a search that reached its limit. Only promise a suggested premise when it is expressible, consistent, and would close the proof. Otherwise explain which assignment or rule must change without claiming to know the author's intent.

## Where the work can finish falsely or repeat

### Critical — Zero bad rows is not evidence that every required row exists

**Verified.** The duplicate-fact check counts unfilled rows and full statements in the *wrong* document. It builds the pairs from facts **already on** the ownership list (`design-completion-plan.md:69-81`). The procedure coverage check counts a step or permitted refusal per member, while the earlier plan also required checking each applicable subdivision (`design-completion-plan.md:472-482`; `compiler-readiness-plan.md:1714-1715`). Step 15 then sets every document's maturity to Full (`design-completion-plan.md:633-640`).

**Consequence.** Mark a fact “does not mention” for every document, including its designated owner. Both stated duplicate-fact counts are zero. Omit the fact from the inventory entirely and there is not even a row to challenge. Likewise, a comparison row citing a numeric proof and a date row citing equality say nothing about how to prove a date ordering. Two independent readers of the same generated list can agree on every row.

**Change.** Require **exactly one** full statement for each fact, at a checked location in the assigned document. Work in the reverse direction too: take each original normative passage to its retained statement, pointer, or justified replacement. For a proof rule that varies by type or action form, follow its applicable cases to an answer; do not multiply unrelated lists. Try removing the sole owner, the ordering-value capture, and one applicable comparison case from a made-up completed inventory. The finish check should reject each omission.

### Major — Acceptance criteria are written too early and the handoff can still omit them

**Verified.** The owner puts the completed design first (`outside-review-brief.md:11-15`). Step 0 calls the bank mutations acceptance criteria, steps 1-11 write criteria, and step 13 can close before steps 12, 14, and 15. Its check looks for an invalid kind or missing owner on **existing** rows (`design-completion-plan.md:283,313-316,587-595,628-640`). The old acceptance stage traced **every claim** to a criterion, including checker obligations (`compiler-readiness-plan.md:2044,2055-2057`).

**Consequence.** The design could specify a diagnostic or certificate-load refusal in step 12, fix it after the hand walk, and deliver no criterion for its final behaviour. All existing handoff rows could still pass. Also, the old bounded search tried to falsify **each numeric proof rule**; finding examples for the compiler's rejection messages is a different job (`compiler-readiness-plan.md:1807-1810`; `design-completion-plan.md:501-505,588-593,1247`). A rule that wrongly accepts an unsafe operation produces no rejection message to investigate.

**Change.** Keep the bank cases as design evidence now. At sign-off hand over a revision and the complete list of claims. In the **following** acceptance stage, check both for a claim with no criterion and a criterion with no claim. Name both counterexample searches and their purposes; neither needs to execute during design.

### Major — Fourteen bank rows are not fourteen independent demonstrations

**Verified.** The plan knows about the unguarded optional read and leaves the want document unchanged (`design-completion-plan.md:206,1025-1026`). The same optional field also occurs in the final refusal message (`what-i-want-2026-07-16.md:35,75-81`); a message interpolation needs a presence proof too (`precept-language-spec.md:1561`). Two of the required mutations remove the very same repayment-cap conjunct (`what-i-want-2026-07-16.md:68,121,123`). Step 8 counts named diagnostics for fourteen rows; step 15 runs the original and the mutations (`design-completion-plan.md:508-510,629-635`).

**Consequence.** An unrelated rejection of the original could make every mutated definition reject. Adding a presence check only to the first guard can send an unset value into the final message. And counting both descriptions of the same edit twice adds no new proof case.

**Change.** Preserve all fourteen source rows. Beside them, identify the intended accepted baseline under each unsettled decision. Show which row handles an absent repayment, including its message. Then show the changed obligation and expected diagnostic for each distinct edit, with an explicit link where two source rows describe one edit. An unrelated failure is not a pass.

### Major — The stop rules can halt correct work with no autonomous way to finish

**Verified.** From the third run the records rule pauses a step when record lines exceed ten times its changed canonical lines; only the owner clears it. The cleanup rule pauses step 14 above twice the proof-model line changes and clears when those lines catch up (`design-completion-plan.md:791-802`). Step 13 produces handoff records, not canonical edits; step 15 consists largely of recorded independent walks (`design-completion-plan.md:585-595,626-636`). The plan also halts if a backlog grows, even while work is being completed.

**Consequence.** Three productive runs writing handoff records and zero canonical lines meet the halt condition: any positive count exceeds ten times zero. After the proof text is finished, a cleanup step above its cap has no remaining proof text to wait for. An owner override, an artificial edit, or abandoned work is not convergence.

**Change.** Measure each kind of work in its own units: verified handoffs per claim, walk cases finished, and confirmed repairs. Stop the cleanup throttle when the protected proof work is finished. Distinguish a known queue being enumerated from a defect being rediscovered. Run the stop-rule tests on these *successful* traces, not just on inputs prepared to trigger a halt.

### Major — The parallel schedule and the one-line start do not describe the same execution

**Verified.** Step 6 needs an early piece of step 9; that piece needs step 4, **not** the completion of step 7 (`design-completion-plan.md:468,515-520`). This is not inherently a cycle. But a new step begins only on the owner's start after a previous step finishes (`design-completion-plan.md:1143-1148`). Step 12 needs the pointer checker, first built in step 14, which itself can be paused (`design-completion-plan.md:318-323,579-582,599-605`). Parallel lines each have their own Git worktree and a lock for shared writes; the concurrency test asks whether two commits succeed, not whether their combined state was checked (`design-completion-plan.md:350-353,1130-1138`).

**Consequence.** A strictly numbered run waits for a later step that the owner has not started. A runner that silently starts later steps solves a different problem than the one the owner authorized. And two branches can each commit a correct update to their own copy of a progress file from the same parent. Serializing writes alone does not make either branch see the other's commit.

**Change.** Give the early graph work to step 4 and build each shared checker before its earliest required use. State exactly which already-authorized pieces may run concurrently. Have one integration point accept results against a named revision and recalculate status on the combined tree. Test a stale result, a stop and restart between commits, and a whole run with one start per step; two successful Git commands do not establish this.

### Major — The stated owner budget counts envelopes, not decisions

**Verified.** Plan approval adopts the thresholds (`design-completion-plan.md:296-305`), but the reading aid promises fourteen reads or decisions and sixteen starts without stating the record and cleanup thresholds (`plan-summary.md:38-46`; `design-completion-plan.md:791-802,1064-1096`). A previously unlisted kind of flaw and a refusal because a strategy does not earn its place require the owner's words (`design-completion-plan.md:689-694,740-748`). A failed-arguments sheet needs a choice **for each argument**, not one choice for the sheet (`design-completion-plan.md:704-709,1053-1055`). The fallback already admits unbounded failure restarts; its numerical estimate assumes eight arguments advanced together (`design-completion-plan.md:1093-1096,1161-1174`).

**Consequence.** Two documented fallbacks can apply together: a failed resume test cuts every run to one piece, and the fallback session stops after five runs. One round of one argument is one piece (`design-completion-plan.md:1130-1132,1156-1157,1195`). Eight arguments at twelve rounds each then require at least 96 runs, or 20 sessions, for attacks alone. That is a first start plus at least 19 restarts, before writing or repairs. The published estimate counts four attack runs for eight arguments. Nor does a tally of notices tell the owner how many counterexamples or proposed changes they must understand in each one.

**Change.** Show the actual settings being accepted. Report decisions requiring a change in product behaviour separately from operational interruptions; count choices inside sheets and estimate reading from one real packet. Give independent repairs and exhausted retries an explicit technical disposition. Reserve owner time for a choice with a concrete consequence, not for clearing a record-count threshold. Keep manual starts and no schedule.

### Major — A canonical edit cannot meet both rules for citing an owner decision

**Verified.** Each canonical edit that relies on the owner's words must cite the corresponding decision row. Certain words in a changed line also require that line to cite the row (`design-completion-plan.md:665-671`). But canon may cite nothing under `docs/Working/` or `docs/working-reset/`, where those records live (`design-completion-plan.md:92-93,847-849`). The citation checker also opens evidence at the current `HEAD`, by line number (`design-completion-plan.md:655-658`).

**Consequence.** An author recording a new ruling can add the required citation and fail the canonical-document check, or leave it out and be sent back for an unsupported use. A later deduplication edit can also move quoted words off the recorded line without changing the decision. Repeating the review cannot reconcile the instructions.

**Change.** Put the link between the canonical edit and the decision in a separate change record. Identify the decision and its quotation at an immutable revision; check the current owning statement separately. Keep the full product rule in canon without making that document depend on the working folder.

### Minor — The adoption instructions point at the wrong copies

**Verified.** The plan calls two inputs scratchpad files to be committed under `inputs` (`design-completion-plan.md:32-37,308-311`). In the reviewed tree the two committed originals identify themselves under `lineage` (`lineage/design-completion-plan-v2.md:1-5`; `lineage/plan-vs-old-review.md:1-4`). The owner's summary names yet another filename (`plan-summary.md:3`).

**Consequence and change.** A literal implementation of the citation check will either fail to open the stated paths or manufacture duplicate documents to satisfy them. Correct the path map to the committed originals and identify the revision the owner is approving. No new planning document is needed.

## What I would do before adoption

Use one paper exercise to decide whether the revised plan is ready. Start with the queue's third destination, the shadowed division in preview, the trapped application, and the unchanged data restored under a tighter rule. For each, have the plan author identify the owning canonical statement, what the compiler must prove, what the checker must reject if omitted, and what the caller observes. Then deliberately omit one of those answers and run the **proposed finish procedure on paper**. It must fail for the missing answer. Separately walk a record-only step and a parallel step through to completion without an extra owner start. This tests the design work and the autonomous process on their own terms; it neither writes product code nor reduces what Precept promises.

## Coverage

**Read in full:** `docs/working-reset/design-completion/outside-review-brief.md`, `docs/working-reset/design-completion/design-completion-plan.md`, `docs/Working/what-i-want-2026-07-16.md`, `docs/philosophy.md`; all five named files in `docs/working-reset/design-completion/inputs/`: `history-failures.md`, `history-rulings.md`, `technical-proof-model.md`, `technical-precedent.md`, `technical-canon-consistency.md`. Also read each of `docs/language/README.md`, `docs/compiler/README.md`, `docs/runtime/README.md`, and `docs/tooling/README.md` in full.

**Read in part:** `docs/working-reset/compiler-readiness-plan.md` (phase status, owner decisions, and selected procedure, counterexample-search and acceptance passages; a separate read-only comparison also covered its later phases). For the four requested canonical directories, the following are the remaining tracked files. “In part” includes targeted searches of the text, not an end-to-end audit:

| Directory | Files read in part |
|---|---|
| `docs/language/` | `business-domain-types.md`, `catalog-system.md`, `collection-types.md`, `graph-analyzer-roadmap.md`, `precept-grammar.md`, `precept-language-spec.md`, `primitive-types.md`, `temporal-type-system.md` |
| `docs/compiler/` | `diagnostic-system.md`, `grammar-generator.md`, `graph-analyzer.md`, `lexer.md`, `literal-system.md`, `name-binder.md`, `parser.md`, `proof-engine.md`, `soundness-and-coverage.md`, `tooling-surface.md`, `type-checker.md` |
| `docs/runtime/` | `descriptor-types.md`, `evaluator.md`, `fault-system.md`, `precept-builder.md`, `result-types.md`, `runtime-api.md` |
| `docs/tooling/` | `extension.md`, `language-server.md`, `mcp.md` |

**Other material consulted:** Read `docs/README.md`, `docs/agent-onboarding.md`, `docs/working-reset/design-completion/plan-summary.md` and `inputs/rulings-routes.md` in full. Read parts of `docs/compiler-and-runtime-design.md`, `docs/working-reset/phase-1/the-walk.md`, `docs/working-reset/phase-2/trace-walk-rulings.md`, `docs/working-reset/phase-5/where-a-divisors-non-zeroness-comes-from.md`, and the plan's `inputs/repair-notes.md`, `lineage/design-completion-plan-v2.md` and `lineage/plan-vs-old-review.md`.

**Could not open or did not read:** None of the brief's required input files was inaccessible or wholly unread. I did not open `outside-review-report.md` (the other reviewer's output), private memory files, or the external papers cited by the input studies. No finding depends on treating those papers or private recollections as independently verified.
