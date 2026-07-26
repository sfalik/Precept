/*
 * ENUMERATION — one run, one cut, no follow-up pass.
 *
 * ─────────────────────────────────────────────────────────────────────────────
 * WHAT IT PRODUCES
 * ─────────────────────────────────────────────────────────────────────────────
 *
 * For every canonical document in the UNITS list below:
 *
 *   1. A block-by-block inventory. The block boundaries come from one deterministic shell
 *      command (headings, table starts, fenced blocks) whose output this script partitions
 *      arithmetically, so the block list is not an agent's opinion about how many blocks a
 *      document has. It IS an agent's transcription of that command's output — the workflow
 *      runtime gives this script no shell and no file I/O, so a model runs the command and
 *      retypes the result. That transcription is cross-checked four ways (see CENSUS CHECKS
 *      below); read those before believing the block count. The partition covers every line
 *      of the file with no gaps and no overlaps. Every block carries exactly one disposition
 *      — it states testable behaviour, or it does not and says why in one line.
 *   2. For every behaviour-bearing block, the test cells that exercise what it requires.
 *      Each cell carries: the short DSL fragment the document states, a complete minimal
 *      definition ready to compile, the citation, a VERBATIM QUOTE of the sentence or table
 *      row the expectation comes from, one line saying how that text yields the expectation,
 *      and the expectation itself in one normalised vocabulary.
 *   3. Escalations — document-internal contradictions and anything the walker could not
 *      dispose of — collected by this script from the agents' own structured returns and
 *      republished verbatim. No synthesis stage can drop one.
 *
 * Files under `docs/working-reset/phase-3/`:
 *   inventory/<unit>.md            block inventory, one row per block — written by a clerical
 *                                  agent per unit from the block table THIS script hands it,
 *                                  including the NO-BEHAVIOR reasons, which are otherwise
 *                                  in memory only
 *   cells/<unit>/<batch>.json      the discrete per-batch cell records
 *   cells/<unit>/<batch>.repair.json    a repair round's output, written beside the original
 *                                  rather than over it; the merge is told which of the two
 *                                  this script accepted
 *   assembled/all-cells.json       merged manifest (mechanical concatenation, count-checked)
 *   escalations.md                 every escalation, verbatim, with its block — written by
 *                                  one agent and then re-read by a second that must find each
 *                                  claim quote in it verbatim
 *   doc-omissions-diagnostics.md   reverse code-keyed check, one file per angle
 *   doc-omissions-catalog.md       (see REVERSE_CHECK)
 *   doc-omissions-obligations.md
 *   REPORT.md                      numbers computed by this script, transcribed not re-derived
 *
 * ─────────────────────────────────────────────────────────────────────────────
 * WHAT IT DELIBERATELY DOES NOT DO
 * ─────────────────────────────────────────────────────────────────────────────
 *
 *   - It does not compile anything. No stage calls `precept_compile`, the precept MCP
 *     server, `dotnet run`, or the runner. Expectations are reasoned from the canonical
 *     text and only from it. On 2026-07-14 the opposite produced 447 verdicts that were
 *     thrown out the same day.
 *   - It does not produce verdicts. There is no pass/gap/bad-cell column anywhere. This run
 *     fixes WHAT should be checked; a later deterministic run measures it.
 *   - It does not use `samples/` as a measure of coverage. Samples are read once, in the
 *     vocabulary phase, only to copy the authoring form of a clean definition.
 *   - It does not treat the catalogs or `DiagnosticCode.cs` as a record of the language.
 *     `src/Precept/Language/DiagnosticCode.cs` (164 members, verified 2026-07-25) is read once
 *     for one purpose: to fix the spelling of diagnostic names so 882 cells do not end up in
 *     four naming schemes again. A document naming a code the compiler does not have is
 *     recorded as a finding, never as an error.
 *   - It does not reconcile two cuts of the territory, because it only makes one. See
 *     "ONE CUT" below.
 *   - It does not carry the 2026-07-14 draft corpus forward. Nothing is inherited.
 *
 * ─────────────────────────────────────────────────────────────────────────────
 * MEASURED ANCESTRY
 * ─────────────────────────────────────────────────────────────────────────────
 *
 * Five workflows ran 2026-07-13/14. All five journals survive under
 * `~/.claude/projects/-home-sfalik-source-repos-Precept/*​/workflows/wf_*.json`, each with
 * its full `.script`. What this script takes and what it refuses.
 *
 * (Agent counts below are each journal's own `agentCount`. A second recovery of these runs
 * reported higher figures — 6, 11, 7, 348, 12 against 4, 9, 5, 344, 10. Settled 2026-07-25 by
 * reading the journals: the higher figures are `workflowProgress.length`, and in all five runs
 * the number of `workflowProgress` entries carrying a label is exactly `agentCount`. The
 * surplus rows are unlabelled orchestration entries. `agentCount` is the agent count. Note the
 * gap is not a constant two — `exhaustive-cell-probe` has four surplus rows, not two.)
 *
 *   wf0-spec-correction-ledger    (4 agents,   9 min,  322 K tokens)
 *       Its four defect classes are kept as the escalation vocabulary. Its mistake is not:
 *       it declared a freeze over documents whose ~40 known document-vs-document leads it
 *       had explicitly deferred, and the same integer-overflow contradiction was then
 *       discovered three separate times across three runs and 24 hours.
 *   wf1-gap-ledger                (9 agents,  57 min, 2.41 M tokens)
 *       Its count gates read MET while one angle's denominator was 33 and an independent
 *       extraction of the same documents found 343. Any gate whose denominator is supplied
 *       by the agent being gated is not a gate. That is why the block list here comes from a
 *       fixed command rather than an agent's judgement (phase 2), why the transcription of
 *       that command is cross-checked against a separately-derived count and against a pinned
 *       baseline, and why the counts are array lengths in this file (phase 3).
 *       Its code-keyed angles survive as the reverse check, which cannot define a
 *       denominator but is the only thing that catches a compiler behaviour no document
 *       mentions.
 *   wf1b-cell-by-cell-matrices    (5 agents,  19 min,  746 K tokens)
 *       Exists only because wf1 deferred a bounded piece of its own scope and called it a
 *       time-box. It reported "~984 cells enumerated, ~923 probed (67 live compiles)" and
 *       stamped its item CLOSED; a run 17 minutes later found 3,446 cells in the same four
 *       documents. There is no equivalent stage here. Type-document cell enumeration is
 *       not a follow-up — it is what the document walk produces on its first pass.
 *   exhaustive-cell-probe       (344 agents,  88 min, 21.06 M tokens)
 *       Its 3,446-cell enumeration was the one salvageable half; its 447 verdicts were
 *       discarded. Kept: the family instruction ("do not sample, do not collapse a family
 *       to a representative") and the four per-matrix enumeration checklists, verbatim, in
 *       ENUM_GUIDANCE. Refused: everything in its probe phase.
 *   gap-analysis-phase1          (10 agents,  22 min, 1.46 M tokens)
 *       Produced the 888-block inventory and 4,592 cells. Its accounting check computed
 *       five real problems, put them in a return value nobody read, and launched the
 *       synthesis stage unconditionally; the published report then said "100% of blocks are
 *       accounted in every unit". Three escalations died at that merge, one of which is a
 *       document contradiction still unruled eleven days later
 *       (`temporal-type-system.md` :143 against :1182 — same example, opposite outcome).
 *       Everything in phases 3, 4 and 8 below exists because of that stage.
 *
 * ─────────────────────────────────────────────────────────────────────────────
 * ONE CUT, AND WHY THIS ONE
 * ─────────────────────────────────────────────────────────────────────────────
 *
 * The territory was cut two ways in 2026-07: by document (nine regions) and by type (date,
 * money, quantity, …). They were never reconciled — the type-keyed register was demoted to
 * "historical leads" and the reconciliation phase never ran, so both are still live and
 * still disagree. This script makes exactly one cut, keyed by DOCUMENT, for one measured
 * reason: a type-keyed cut structurally cannot reach a document-keyed denominator. Entering
 * gap-analysis-phase1 the type-keyed draft held 15 cells for the 2,219-line language spec
 * and zero for the proof-engine document; those two regions then produced most of the
 * 1,179 net-new cells, +419 on type-checking alone. The type axis is not lost — it survives
 * as enumeration pressure inside the document walk (ENUM_GUIDANCE), which is where it was
 * always doing its real work.
 *
 * ─────────────────────────────────────────────────────────────────────────────
 * WHERE THIS STILL NEEDS HUMAN JUDGEMENT — read this before believing the numbers
 * ─────────────────────────────────────────────────────────────────────────────
 *
 *  1. WHICH DOCUMENTS ARE CANONICAL is a call encoded in UNITS below, not a fact this
 *     script derives. Every document in `docs/` that could plausibly state compiler behaviour
 *     is named individually in that list with `on: true` or `on: false` and a written reason;
 *     three whole directories are excluded categorically, and that categorical exclusion is
 *     stated at the head of the list rather than left as an absence. The exclusions are
 *     reviewable, but they are still someone's opinion. Check them.
 *  2. DEFINITION CLEANLINESS AND DEFINITION RELEVANCE ARE BOTH UNVERIFIED. Cells carry a
 *     complete definition written to construction rules that should keep it free of the
 *     ambient PRE0158 (no write site) and PRE0093 (no initial event) noise that wrecked the
 *     2026-07-14 probe. Confirming that would require compiling, which this run may not do;
 *     it is the first gate of the measurement run. Separately, nothing in this run compares a
 *     cell's `definition` against what the cell CLAIMS to check — the only script-side test on
 *     it is a length floor. The citation audit is given each sampled cell's `fragment` and
 *     asked whether it exercises the stated behaviour, which covers the sampled ~500 and
 *     nothing else. Both limits are the measurement run's problem and neither is closed here.
 *  3. FAMILY COMPLETENESS INSIDE A BLOCK IS PRESSURE PLUS A SAMPLE, NOT A PROOF. The
 *     partition proves no block was skipped, given a correct anchor list. It cannot prove a
 *     block was read closely enough. The countermeasures are the family instruction, a
 *     table-row floor (phase 3), and an independent re-enumeration of the thinnest blocks
 *     (phase 5) — a targeted probe, not a guarantee. The 2026-07-14 statement of this limit is
 *     honest and is reproduced verbatim in the report.
 *  4. `reject:any` IS AN ADMISSION, NOT AN EXPECTATION. It was 32% of the last corpus and
 *     passes trivially against any ambient error. Here it requires a written reason naming
 *     the under-determined sentence, and the report prints the rate per unit. If that rate
 *     comes back high, the documents are under-determined and a person has to decide
 *     whether that is a document defect worth ruling before measurement is worth running.
 *  5. `value:` CELLS CANNOT BE MEASURED BY THE EXISTING RUNNER, which returns diagnostic
 *     codes and severities only. The rounding-table and decimal-exactness claims need a
 *     value comparison. In 2026-07-14 that observation was made by an agent and died at the
 *     merge; here the shape exists and the count is printed, but building the comparison is
 *     someone's decision.
 *  6. THE PROOF-ENGINE IMPLEMENTATION SECTIONS. The 2026-07-14 run wrote off §6–§9 of
 *     `proof-engine.md` wholesale — 64 of that unit's 112 blocks, the largest write-off
 *     anywhere in the run — on an instruction from a plan that no longer exists. This
 *     script does not inherit the write-off and does not reverse it either: those sections
 *     are walked, and each block is dispositioned individually with its own reason. Expect
 *     the unit to grow and expect a long list of NO-BEHAVIOR reasons to read.
 *  7. ESCALATIONS ARE COLLECTED, NOT RULED. Document-internal contradictions are surfaced
 *     verbatim and the affected cells are flagged `conflict: true`. Ruling them is the
 *     owner's, and cells resting on an unruled contradiction are not trustworthy until he
 *     does.
 *  8. THE REVERSE CHECK LEANS ON CODE THE PROJECT SAYS IS NOT A RECORD OF THE LANGUAGE. Its
 *     findings are document omissions to consider, one-directional, and contribute nothing
 *     to the denominator.
 *  9. NO-BEHAVIOR IS THE CHEAPEST WAY OUT OF THE DENOMINATOR, AND IT IS ONLY SAMPLED. A block
 *     called NO-BEHAVIOR owes no cells, escapes the table-row floor, and is never seen by the
 *     citation audit — its whole defence is a one-line reason. On ~2,047 blocks a large
 *     fraction will be dispositioned that way. Phase 5 sends a bounded sample (the longest
 *     ones, and every NO-BEHAVIOR table with three or more data rows, capped at
 *     NO_BEHAVIOR_REVIEW_BLOCKS) to an agent that reads the block WITHOUT being shown the
 *     reason given, and the run gates on the disagreement rate. That converts an unchecked
 *     write-off into a sampled one. It does not make it a proof, and the threshold below is a
 *     number someone chose.
 * 10. THE VALIDATED CORPUS AND THE PUBLISHED CORPUS ARE THE SAME RECORDS ONLY BY COUNT. This
 *     script validates what the agents RETURNED; `assembled/all-cells.json` is built by an
 *     agent from what they WROTE. Totals, per-unit counts, duplicate ids and superseded-file
 *     bookkeeping are reconciled between the two, and the merge is told which repair and
 *     re-enumeration files this script accepted. Record BODIES are not compared. A batch that
 *     returned a good record and wrote a hollow one is not detectable from here.
 * 11. `wroteCellsFile` IS A SELF-REPORT AND SO IS EVERY OTHER WRITE. The workflow runtime gives
 *     this script no file I/O, so every durable artifact in the run is written by an agent that
 *     then tells the script it wrote it. The one place that is closed is `escalations.md`,
 *     which a second cheap agent re-reads and must find each claim quote in verbatim. Nothing
 *     else has that treatment, and the report agent is the last writer in the run — if IT
 *     fails, `fail()` records the failure in a return value and there is no REPORT.md to
 *     record it in. Splitting phase 8 into five small jobs reduces that exposure; it does not
 *     remove it.
 * 12. THERE IS NO RESUME. Any hard failure means re-running from phase 1 at full cost. The
 *     runtime offers no checkpoint primitive this script could use, and the batch cell files
 *     on disk are not re-read by anything except the final merge. Budget for one run, not for
 *     an iteration loop.
 * 13. THE CHALLENGE RATIO IS UNCALIBRATED. Phase 5 compares an expensive artifact (full cells)
 *     against a cheap one (one-line behaviour descriptions) and calls a block short at
 *     CHALLENGE_SHORTFALL_RATIO = 1.5. Nobody has measured what ratio a healthy block produces,
 *     because the run has never happened. Expect false shortfalls; that is why the number of
 *     re-enumerations is capped, and why every comparison is reported rather than only the ones
 *     over the line. Calibrating the ratio on the first run's numbers is a person's job.
 *
 * ─────────────────────────────────────────────────────────────────────────────
 * CENSUS CHECKS — what makes the block count more than one agent's word
 * ─────────────────────────────────────────────────────────────────────────────
 *
 * The census agent runs a fixed command and retypes its output. For the language spec that is
 * 245 rows of JSON. A model that drops rows produces fewer, larger blocks, and a partition over
 * the surviving anchors still covers every line with no gaps — so contiguity arithmetic cannot
 * detect it. Four things do:
 *
 *   - A SEPARATELY DERIVED COUNT. The agent also runs `<the census command> | wc -l` and
 *     returns that integer. `anchors.length` must equal it. A transcription that drops rows now
 *     has to drop them from an independent count too.
 *   - A PINNED BASELINE. CENSUS_BASELINE below holds the anchor count for every active document
 *     as measured on 2026-07-25. Any deviation is reported. A document edit is the only
 *     legitimate cause and is worth a line in the report either way.
 *   - PER-ANCHOR VALIDATION IN THIS FILE. Every anchor carries its own text, so a `heading` must
 *     match the heading pattern, a `table` must start with a pipe, and a `fence` must start with
 *     a fence marker. Line numbers must be strictly increasing after dedup and inside
 *     1..totalLines. Fabricated or misaligned rows fail here.
 *   - FENCE PARITY. The census command tracks fenced-code state. An unbalanced fence would make
 *     it swallow every heading and table after it and the partition would still validate as one
 *     giant tail block. The agent reports the file's fence-marker line count and the run fails
 *     on an odd number. All 20 active documents are even as of 2026-07-25.
 *
 * What none of that catches: an oversized block. A fenced example is one anchor and its interior
 * is invisible to the command, so `diagnostic-system.md`'s csharp enum listing is 214 lines in
 * ONE block with ONE disposition, and `proof-engine.md` has a 167-line one. Those two are the
 * only blocks at or above 120 lines in the active set (median block is 8 lines, p95 is 26). The
 * run warns on any block over BIG_BLOCK_LINES and lists them with their cell counts, so an
 * oversized block is a named item rather than an average.
 *
 * ─────────────────────────────────────────────────────────────────────────────
 * COST SHAPE — so nobody is surprised twice
 * ─────────────────────────────────────────────────────────────────────────────
 *
 * The enabled document set is 20,944 lines against the ~9,000 walked in 2026-07-14. Running the
 * census command of phase 2 over that set by hand on 2026-07-25 found 2,047 anchors, and running
 * this file's `partition` and `batchBlocks` over that real output gives exactly 2,047 blocks in
 * 139 batches. Those are measurements, not estimates.
 *
 * The 2026-07-14 run reported 888 blocks. The increase is NOT closer reading: that run walked
 * about 9,000 lines for 888 blocks, or 0.099 blocks per line, and this one gets 2,047 over
 * 20,944, or 0.098. Identical granularity. The entire difference is the size of the document
 * set. What this run buys is coverage by construction and a reproducible command, not a finer
 * cut.
 *
 * Agent count, measured by running this file end to end against stubbed agents and the real
 * census output on 2026-07-25:
 *   245  a run with no repairs and no shortfalls
 *          1 vocabulary, 20 census, 139 enumerators, 40 challengers, 20 auditors, 3 reverse,
 *          20 inventory, 1 merge, 1 report. (No no-behaviour reviews in that run, because the
 *          stub dispositioned every block BEHAVIOR; a real run adds up to
 *          NO_BEHAVIOR_REVIEW_BLOCKS of them, and 2 more clerical agents once there are
 *          escalations to write and verify.)
 *   386  the same run with a FULL repair round — 139 more opus/high agents
 *   +20  worst case on top of that, if every challenged block hits the shortfall ratio and the
 *          re-enumeration cap is reached
 * So: roughly 250 at best, roughly 400 at worst, and the repair round is the entire swing. It is
 * the second-most-expensive thing in the file after the enumerators themselves, which is why a
 * per-cell check that misfires on correct work is a cost bug and not just a nuisance. The
 * expectedBasis tripwire in `checkBatch` was exactly that until 2026-07-25: the bare stem
 * `compil` matched "compile-time" and "the compiler must reject", so an ordinary correct basis
 * tripped it and most of 139 batches would have gone to repair. 653 lines in the active
 * documents contain that stem.
 *
 * "Concurrency is capped around 16" is INHERITED, not verified — it is not visible in this
 * script and could not be recovered from the journals. What is known: `exhaustive-cell-probe`
 * ran 344 agents, so a 139-wide fan-out evidently works. Nothing here holds a whole corpus in
 * one pass — the 2026-07-14 whole-corpus extraction stalled three times before a fourth attempt
 * took 52 minutes and 1.43 M tokens on its own.
 *
 * That is a large run and it should be costed before it is started. The comparable failure
 * cost $1,020 of weekly budget and 21 M subagent tokens for a result that was thrown away.
 * The knob that moves cost most is the UNITS list: the eight language documents alone are
 * 11,203 lines and 1,153 anchors, a little over half of it.
 *
 * ─────────────────────────────────────────────────────────────────────────────
 * BEFORE YOU EDIT THIS FILE: `node --check` DOES NOT CHECK IT
 * ─────────────────────────────────────────────────────────────────────────────
 *
 * Measured on Node 24.16.0, 2026-07-25: `node --check` exits 0 on ANY syntax error in a file
 * that contains an `export` statement. It is not a weaker check here, it is not a check at all —
 * a file with an unbalanced parenthesis passes it silently. This file exports `meta`, so it is
 * in that category, and an earlier edit of it did in fact carry an unbalanced parenthesis past
 * a clean `node --check`.
 *
 * What works, and takes a second — strip the export with a line-anchored regex, not a string
 * match, because this comment contains the same words the string match would find:
 *
 *   node -e 'const s=require("fs").readFileSync("docs/working-reset/phase-3/enumeration.workflow.js","utf8")
 *     .replace(/^export /m, "");
 *     new Function("return (async()=>{\n"+s+"\n})()"); console.log("ok")'
 *
 * That parses the whole file as the async function body it actually is. It catches syntax; it
 * does not catch a name used before its `const` is reached, which is a runtime error inside an
 * async body and needs the file actually run. The way to get that is a stub harness: supply
 * `agent`, `parallel`, `pipeline`, `phase` and `log`, have the `agent` stub shell out to the
 * real census command and return well-formed objects for everything else, and run it. Doing
 * that on 2026-07-25 found a ReferenceError that neither check above would have.
 */

export const meta = {
  name: 'enumeration',
  description: 'One-pass walk of the canonical documents: every line of every document lands in exactly one block, every block gets an explicit disposition, every behaviour-bearing block gets test cells reasoned from the text. Establishes complete block coverage, not proven behaviour extraction. No compiling, no verdicts, no follow-up reconciliation',
  phases: [
    { title: 'Vocabulary', detail: 'diagnostic name list + clean-definition authoring form' },
    { title: 'Census', detail: 'fixed-command block anchors per document, cross-checked; this script builds the partition' },
    { title: 'Enumerate', detail: 'per-batch disposition + cells, batched by contiguous blocks' },
    { title: 'Repair', detail: 'one bounded round against this script\'s arithmetic failures' },
    { title: 'Challenge', detail: 'independent re-enumeration of the thinnest blocks + blind review of no-behaviour write-offs' },
    { title: 'Audit', detail: 'sampled per-cell quote-at-line verification' },
    { title: 'Reverse', detail: 'code-keyed check for behaviour no document mentions' },
    { title: 'Publish', detail: 'merge + inventories + escalations (re-read for verbatim claims) + report transcribed from this script\'s numbers' },
  ],
}

// ─────────────────────────────────────────────────────────────────────────────
// Constants
// ─────────────────────────────────────────────────────────────────────────────

const BUNDLE = 'docs/working-reset/enumeration'

// Batch size. A batch is a run of contiguous blocks and is the unit of containment: a bad
// prompt or a dead agent spoils one batch, which is regenerated in place. Whichever limit
// bites first ends the batch.
const BATCH_MAX_LINES = 300
const BATCH_MAX_BLOCKS = 16

// Warn on any block this long or longer. A fenced example is one anchor and its interior is
// invisible to the census command, so a big fence becomes one block with one disposition. Over
// the real active set the median block is 8 lines and p95 is 26; only two blocks reach 120 —
// diagnostic-system.md's 214-line csharp enum listing and a 167-line block in proof-engine.md.
// A dropped census anchor also shows up as an oversized block, which is the second reason for
// this warning.
const BIG_BLOCK_LINES = 120

// Phase 5. How many of the thinnest behaviour-bearing blocks get an independent second
// enumeration, and the ratio at which the challenger's count is treated as a shortfall.
// The ratio is UNCALIBRATED — see judgement call #13. The challenger produces one-line
// behaviour descriptions and the first pass produces full cell records, so the comparison is
// not like-for-like and false shortfalls are expected. CHALLENGE_REMERGE_MAX bounds what a
// wrong ratio can cost: shortfalls are ranked by gap and only this many are re-enumerated.
const CHALLENGE_BLOCKS = 40
const CHALLENGE_SHORTFALL_RATIO = 1.5
const CHALLENGE_REMERGE_MAX = 20

// Phase 5, second half. Blind review of NO-BEHAVIOR write-offs — judgement call #9. The
// reviewer reads the block and never sees the reason it was written off. A disagreement is not
// automatically the reviewer being right, which is why the gate is a rate rather than a single
// finding; the threshold is a chosen number and the individual disagreements are printed.
const NO_BEHAVIOR_REVIEW_BLOCKS = 24
const NO_BEHAVIOR_MAX_DISAGREE_RATE = 0.25

// Phase 6. Cells sampled per unit for quote-at-line verification, and the failure rate above
// which the run is gated. 2% was the cleanliness bar the 2026-07-14 salvage set for itself
// and failed to reach mechanically; 4% is the per-cell error rate a 26-cell audit of the
// probe implied and did not extrapolate. The per-unit gate is looser than the corpus gate
// because a 25-cell sample is small: one failure is 4% on its own.
const AUDIT_SAMPLE_PER_UNIT = 25
const AUDIT_MAX_FAIL_RATE = 0.04
const AUDIT_MAX_UNIT_FAIL_RATE = 0.20
const AUDIT_UNIT_GATE_MIN_SAMPLE = 10

// Phase 7. Off makes the run purely document-keyed.
const REVERSE_CHECK = true

// How many items of an unbounded list get interpolated into a prompt. The report prompt used
// to take `problems` and `warnings` in full; on a bad run — a dead census contributes one
// failure per block — that is a multi-megabyte prompt handed to the one agent whose failure
// leaves no record at all. Truncation is stated in the prompt, never performed silently.
const PROMPT_LIST_MAX = 60

// A gate failure stops publication of a clean report and ends the run in a failed state.
// The report is written first — with the failures in it — so the run is never both failed
// and silent. Set false only to inspect a partial run.
const THROW_ON_GATE_FAILURE = true

// Where the diagnostic enum actually lives. Verified 2026-07-25: there is no
// src/Precept/Diagnostics/ directory, and the enum has 164 members.
const DIAGNOSTIC_CODE_PATH = 'src/Precept/Language/DiagnosticCode.cs'
const CODE_COUNT_BASELINE = 164

// Anchor counts per unit as measured on 2026-07-25 by running the census command of phase 2
// against the real files. A census transcription that drops rows shows up here. A document
// edit is the only legitimate cause of movement and is worth a line in the report either way.
const CENSUS_BASELINE = {
  spec: 245, grammar: 101, 'primitive-types': 87, temporal: 202, 'business-domain': 214,
  collection: 156, 'catalog-system': 133, 'graph-analyzer-roadmap': 15, 'proof-engine': 190,
  lexer: 96, parser: 74, 'name-binder': 54, 'type-checker': 105, 'graph-analyzer': 84,
  'diagnostic-system': 61, 'literal-system': 91, 'soundness-and-coverage': 22,
  'grammar-generator': 31, philosophy: 8, 'compiler-and-runtime-design': 78,
}

// ─────────────────────────────────────────────────────────────────────────────
// The document set.
//
// `on: false` entries carry a written reason and are printed in the report, so an exclusion is
// a reviewable decision rather than an absence nobody notices. This is judgement call #1.
//
// THREE DIRECTORIES ARE EXCLUDED CATEGORICALLY and are not enumerated file by file below,
// because listing several hundred paths would bury the twenty that matter. They are named here
// so the exclusion is still reviewable, and they are printed in the report:
//   docs/Working/    in-flight proposals. CLAUDE.md classes this surface as transient
//                    (promote-or-archive), so it is not canonical by the project's own rule.
//                    A behaviour stated only here is not yet a requirement on the compiler.
//   docs/archive/    superseded specs. CLAUDE.md: "reference only, never update."
//   docs/working-reset/  this run's own scaffolding, including this file.
// Four individually-named files below the active set are the remaining loose ends under docs/:
// docs/contributing/ (3 files) and docs/future/reasoning-ingress-vision.md.
// ─────────────────────────────────────────────────────────────────────────────

const UNITS = [
  // --- the language surface. The owner's position is that docs/language/ plus the spec are
  // the only things that should be comprehensive, so these are the core of the denominator.
  { unit: 'spec', path: 'docs/language/precept-language-spec.md', on: true, model: 'opus', guide: 'GUIDE_SPEC' },
  { unit: 'grammar', path: 'docs/language/precept-grammar.md', on: true, model: 'opus', guide: 'GUIDE_GRAMMAR' },
  { unit: 'primitive-types', path: 'docs/language/primitive-types.md', on: true, model: 'opus', guide: 'GUIDE_PRIMITIVE' },
  { unit: 'temporal', path: 'docs/language/temporal-type-system.md', on: true, model: 'opus', guide: 'GUIDE_TEMPORAL' },
  { unit: 'business-domain', path: 'docs/language/business-domain-types.md', on: true, model: 'opus', guide: 'GUIDE_BUSINESS' },
  { unit: 'collection', path: 'docs/language/collection-types.md', on: true, model: 'opus', guide: 'GUIDE_COLLECTION' },
  { unit: 'catalog-system', path: 'docs/language/catalog-system.md', on: true, model: 'opus', guide: 'GUIDE_CATALOG' },
  { unit: 'graph-analyzer-roadmap', path: 'docs/language/graph-analyzer-roadmap.md', on: true, model: 'opus', guide: 'GUIDE_GENERIC' },

  // --- the compiler stage documents. Mostly implementation description, which is why the
  // 2026-07-14 run took only one of them — but each states normative guarantees in among the
  // mechanics, and the per-block disposition is what separates the two. No document-level
  // write-off here; the write-off happens block by block with a reason each time.
  { unit: 'proof-engine', path: 'docs/compiler/proof-engine.md', on: true, model: 'opus', guide: 'GUIDE_PROOF' },
  { unit: 'lexer', path: 'docs/compiler/lexer.md', on: true, model: 'opus', guide: 'GUIDE_GENERIC' },
  { unit: 'parser', path: 'docs/compiler/parser.md', on: true, model: 'opus', guide: 'GUIDE_GENERIC' },
  { unit: 'name-binder', path: 'docs/compiler/name-binder.md', on: true, model: 'opus', guide: 'GUIDE_GENERIC' },
  { unit: 'type-checker', path: 'docs/compiler/type-checker.md', on: true, model: 'opus', guide: 'GUIDE_GENERIC' },
  { unit: 'graph-analyzer', path: 'docs/compiler/graph-analyzer.md', on: true, model: 'opus', guide: 'GUIDE_GENERIC' },
  { unit: 'diagnostic-system', path: 'docs/compiler/diagnostic-system.md', on: true, model: 'opus', guide: 'GUIDE_GENERIC' },
  { unit: 'literal-system', path: 'docs/compiler/literal-system.md', on: true, model: 'opus', guide: 'GUIDE_GENERIC' },
  { unit: 'soundness-and-coverage', path: 'docs/compiler/soundness-and-coverage.md', on: true, model: 'opus', guide: 'GUIDE_GENERIC' },
  { unit: 'grammar-generator', path: 'docs/compiler/grammar-generator.md', on: true, model: 'opus', guide: 'GUIDE_GENERIC' },

  // --- the two guarantee documents above the stages.
  { unit: 'philosophy', path: 'docs/philosophy.md', on: true, model: 'opus', guide: 'GUIDE_GENERIC' },
  { unit: 'compiler-and-runtime-design', path: 'docs/compiler-and-runtime-design.md', on: true, model: 'opus', guide: 'GUIDE_GENERIC' },

  // --- off, with reasons.
  { unit: 'runtime-api', path: 'docs/runtime/runtime-api.md', on: false,
    why: 'Runtime behaviour. This run enumerates what the COMPILER must do; nothing downstream can measure a runtime expectation. Turning these on is a scope decision, not an oversight — there are 7 runtime documents totalling ~5,200 lines.' },
  { unit: 'evaluator', path: 'docs/runtime/evaluator.md', on: false, why: 'Runtime behaviour — see runtime-api.' },
  { unit: 'fault-system', path: 'docs/runtime/fault-system.md', on: false, why: 'Runtime behaviour — see runtime-api. Note this one is closest to the line: it names fault classes the proof engine must discharge, and those claims are reached through proof-engine.md instead.' },
  { unit: 'precept-builder', path: 'docs/runtime/precept-builder.md', on: false, why: 'Runtime behaviour — see runtime-api.' },
  { unit: 'result-types', path: 'docs/runtime/result-types.md', on: false, why: 'Runtime behaviour — see runtime-api.' },
  { unit: 'descriptor-types', path: 'docs/runtime/descriptor-types.md', on: false, why: 'Runtime behaviour — see runtime-api.' },
  { unit: 'language-server', path: 'docs/tooling/language-server.md', on: false, why: 'Tooling behaviour. A language-server expectation is not a compile-time guarantee and is not measurable by compiling a definition.' },
  { unit: 'mcp', path: 'docs/tooling/mcp.md', on: false, why: 'Tooling behaviour — see language-server.' },
  { unit: 'extension', path: 'docs/tooling/extension.md', on: false, why: 'Tooling behaviour — see language-server.' },
  { unit: 'tooling-surface', path: 'docs/compiler/tooling-surface.md', on: false, why: 'Describes what the compiler exposes to tools rather than what it guarantees about a definition. Borderline: if a reader believes it states compile-time behaviour, turn it on.' },
  { unit: 'glossary', path: 'docs/glossary.md', on: false, why: 'Definitions of terms. A behaviour stated only here and nowhere else would itself be a document defect; the reverse check would surface it.' },
  { unit: 'docs-readme', path: 'docs/README.md', on: false, why: 'Navigation.' },
  { unit: 'language-readme', path: 'docs/language/README.md', on: false, why: 'Navigation.' },
  { unit: 'compiler-readme', path: 'docs/compiler/README.md', on: false, why: 'Navigation.' },
  { unit: 'runtime-readme', path: 'docs/runtime/README.md', on: false, why: 'Navigation.' },
  { unit: 'tooling-readme', path: 'docs/tooling/README.md', on: false, why: 'Navigation.' },
  { unit: 'agent-onboarding', path: 'docs/agent-onboarding.md', on: false, why: 'Instructions to agents, not guarantees about definitions.' },
  { unit: 'contributing-anti-patterns', path: 'docs/contributing/anti-patterns.md', on: false, why: 'Instructions to contributors. States what an author of compiler code should not do, not what the compiler must do to a definition.' },
  { unit: 'contributing-catalog-checklist', path: 'docs/contributing/catalog-driven-checklist.md', on: false, why: 'Instructions to contributors — see anti-patterns.' },
  { unit: 'contributing-readme-template', path: 'docs/contributing/sub-area-readme-template.md', on: false, why: 'A document template.' },
  { unit: 'reasoning-ingress-vision', path: 'docs/future/reasoning-ingress-vision.md', on: false, why: 'Explicitly forward-looking (docs/future/). Nothing in it is a requirement on the compiler as it is specified today. Borderline in one direction only: if it has hardened into a commitment since it was written, it belongs on.' },
]

// Whole directories excluded rather than listed file by file. Printed in the report so the
// categorical call is as reviewable as the per-file ones.
const EXCLUDED_TREES = [
  { path: 'docs/Working/', why: 'In-flight proposals. CLAUDE.md classes this surface as transient (promote-or-archive lifecycle), so it is not canonical by the project\'s own rule. A behaviour stated only here has not yet become a requirement on the compiler.' },
  { path: 'docs/archive/', why: 'Superseded specs. CLAUDE.md: reference only, never update.' },
  { path: 'docs/working-reset/', why: 'This run\'s own scaffolding, including this file.' },
]

const ACTIVE = UNITS.filter(u => u.on)
const EXCLUDED = UNITS.filter(u => !u.on)

// ─────────────────────────────────────────────────────────────────────────────
// The expectation vocabulary.
//
// 2026-07-14 shipped four incompatible ways of naming the same diagnostic — 360 cells in
// PRE#### form, 522 in enum-name form, 7 in schemes nothing could resolve. One form only
// here, checked in this file by regex and then resolved against the real name list.
// ─────────────────────────────────────────────────────────────────────────────

const EXPECTED_RE = /^(accept|reject:any|reject:[A-Za-z][A-Za-z0-9_]*|warns:[A-Za-z][A-Za-z0-9_]*|accept-with-warning:[A-Za-z][A-Za-z0-9_]*|value:.+)$/

// The one thing that would have made the 447 discarded verdicts visible before they were
// trusted: a basis that says the compiler was consulted disqualifies the cell on sight.
// Used by checkBatch. It lives up here with the other patterns because checkBatch is called
// from the enumerate pipeline, which runs before the point in the file where checkBatch is
// written — a `const` beside the function would be in its temporal dead zone at that call.
//
// This pattern was narrowed on 2026-07-25. It used to lead with the bare stem `compil`, which
// matches "compile-time", "compiler" and "compilation" — so a faithful basis for
// business-domain-types.md:176 ("Cross-currency arithmetic is rejected at compile time.") was
// flagged, and 653 lines of the active documents contain that stem. Measured against realistic
// one-line bases: the old pattern flagged 4 of 6 correct ones and missed "reflects current
// behavior of the type checker"; this one flags 0 of 6 correct and catches all 6 wrong ones.
// Anchor on the tell — a first-person report of having run something, or a reference to what
// the compiler currently does — not on the domain vocabulary. Left as it was, this check would
// have sent most of 139 batches to a repair round they did not need, roughly doubling the run.
const COMPILER_BASIS_RE = /\b(?:I|we)\s+(?:ran|compiled|tested|checked|verified|observed)\b|precept_compile|\bdotnet\b|\bthe runner\b|actual output|current behaviou?r|what the compiler (?:does|returns|currently|actually)|compiler output|compiled (?:it|this|the)/i

const EXPECTED_DOC = `
expected — EXACTLY ONE of these SIX shapes, and nothing else:
  accept                         the document says this is well-formed and the compiler must not reject it
  reject:<DiagnosticName>        the document says this is rejected, and names or clearly implies which check
  warns:<DiagnosticName>         the document says this is reported at Warning severity, not rejected
  accept-with-warning:<Name>     accepted, and a warning is also required
  value:<expression>             the document states a produced VALUE, not an accept/reject
                                 (e.g. value:round(2.5)==2 ). Use this rather than forcing a
                                 value claim into accept — nine such claims were lost that way
                                 in 2026-07-14 and the observation died at a merge.
  reject:any                     LAST RESORT. The document plainly says this is rejected but does
                                 not determine by which check. If you write this you MUST fill
                                 expectedUnderdetermined with the sentence that is under-determined
                                 and what it fails to say. It is an admission that the document is
                                 vague, not an expectation. It was 32% of the last corpus and it
                                 passes against literally any error, including ambient noise.

<DiagnosticName> is the C# enum member name from ${DIAGNOSTIC_CODE_PATH} —
never the PRE#### form, never a bare number, never an invented name. The valid list is given
to you below. If the document names a code that is NOT in that list, still write it: a document
naming a code the compiler does not have is a finding, and this script collects it.
`

// ─────────────────────────────────────────────────────────────────────────────
// Enumeration guidance.
//
// The four type-document paragraphs are lifted verbatim from exhaustive-cell-probe's
// ENUM_GUIDANCE — the one part of that run assessed as reusable. The proof-engine paragraph
// is from gap-analysis-phase1, with its blanket §6–§9 write-off removed (judgement call #6).
// ─────────────────────────────────────────────────────────────────────────────

const GUIDES = {
  GUIDE_GENERIC:
    'Enumerate every behaviour the block states, and every member of the family it belongs to rather than the one instance the prose names. Where the block describes how the compiler is built rather than what it guarantees about a definition, that is a NO-BEHAVIOR disposition — say so in the reason, per block, and do not write off a whole section at once.',

  GUIDE_SPEC:
    'The language specification. Regions differ sharply: the lexer sections are literal lanes and tokenisation (every lane x every valid and invalid form); the parser sections are grammar productions (every construct x present, missing, duplicated, misordered clause); the name-binding and type-checking sections are operator x operand-type matrices and every sentence containing "is a type error"; the semantics, graph-analyzer and proof-engine sections are evaluation semantics and the structural checks (reachability, dead-end, unreachable, sink) and prove-or-reject at the specification level. Expect the type-checking region to be the largest by cells; in 2026-07-14 it alone produced 419 net-new.',

  GUIDE_GRAMMAR:
    'The grammar. Every production: what parses, what does not, and what the error is. Optional and repeated elements each give a present/absent/repeated case. Precedence and associativity claims are behaviour.',

  GUIDE_PRIMITIVE:
    'ALL operators (arithmetic, comparison, logical); every numeric literal lane (whole, decimal, exponent 1e2, fractional 0.5) in EVERY position (assignment target, comparison peer, function argument, default value, field-constraint value); ALL functions (abs round floor ceil truncate min max clamp pow sqrt); ALL field-constraints (min max notempty maxplaces, choice-of member validation in default/set/compare); string interpolation of every value category.',

  GUIDE_TEMPORAL:
    'ALL binary operators (+ - * / < > <= >= == !=) against every sensible right-hand operand (same type, period, duration, inline temporal-quantity literal, field-ref); ALL accessors (.year .month .day .dayOfWeek .hour .minute .second .offset etc as applicable to this type); every literal form the spec defines for this type; navigation/set operations; every field-constraint (min/max) form.',

  GUIDE_BUSINESS:
    'ALL operators including same-currency vs cross-currency and same-dimension vs cross-dimension variants; the in/of qualifiers (currency, unit, compound units incl. time-denominator like USD/hours); ALL functions (abs round floor ceil truncate min max clamp); unit-string / currency-code content validation; field-constraints (maxplaces incl. currency.minorUnit, min/max). This document has locked decisions — a cell checks BEHAVIOUR, it does not re-open a decision.',

  GUIDE_COLLECTION:
    'ALL actions (add remove enqueue dequeue push pop append put insert) on this kind AND on the WRONG kind; ALL accessors (.count .min .max .at .first .last .contains .sum and any others); literal defaults ([] and [x]); by-<key> projection; inner-element-type constraints; the guard/proof-obligation cases (unguarded access, index bounds, key presence, non-empty). Where the document says a feature is "not yet implemented", that is an implementation-state note and not a specification: the behaviour is still a real cell with the expectation the document states. Do not flip an expectation on the strength of a build-status note — in 2026-07-14 eight cells were flipped that way, unilaterally and unlogged.',

  GUIDE_CATALOG:
    'The catalog system document. Behaviour here is what the catalogs must make true of a definition, not the shape of the catalog classes. Where a block describes catalog architecture, that is NO-BEHAVIOR with a reason. Note the standing project position: the catalogs are not a complete record of the language and where a catalog and a canonical document disagree the document wins — so a cell here cites this document, never a catalog file.',

  GUIDE_PROOF:
    'Scope to the guarantee the document states. Enumerate: fault classes { division-by-zero, modulo-by-zero, integer overflow, decimal overflow, sqrt/root of negative, other rejectable math, unguarded collection index/access, unguarded key lookup, null/absent access } x contexts { rule expression, ensure expression, guard/when expression, computed field default, transition action, arg default } x outcome { statically-safe -> accept-with-proof-certificate, statically-violating -> reject-with-counterexample, not-statically-decidable -> reject (unprovable = reject, never skip) }. Draw the fault-class list from the document itself, not from this paragraph, and add any class it names that this paragraph does not.\n\nOn the architecture, component-mechanics, dependencies and failure-mode sections: the 2026-07-14 run wrote all four off in one instruction and lost 64 of 112 blocks. Do NOT do that. Walk them, and disposition each block on its own merits — most will be NO-BEHAVIOR with a one-line reason saying it describes construction rather than guarantee, and a few will state a normative guarantee in the middle of the mechanics. Those few are why the walk happens.',
}

// ─────────────────────────────────────────────────────────────────────────────
// Schemas
// ─────────────────────────────────────────────────────────────────────────────

// An escalation is an object with required fields. It is deliberately impossible to write
// the word "None" into it: an empty array is the only way to say there is nothing. In
// 2026-07-14 a free-text entry beginning "None outright, but flagged for Phase 2: <a real
// contradiction>" was read as "none" by a synthesis agent and the contradiction is still
// unruled.
const ESCALATION_SCHEMA = {
  type: 'object',
  additionalProperties: false,
  required: ['kind', 'docPath', 'lines', 'claimA', 'claimB', 'whyItBlocks', 'affectedBlockIds'],
  properties: {
    kind: {
      type: 'string',
      enum: [
        'document-contradiction',   // two places in canonical documents cannot both hold
        'under-determined',         // the document states an outcome without determining it
        'unschemable',              // the claim does not fit the expectation vocabulary at all
        'cannot-dispose',           // could not decide whether this block states behaviour
        'names-unknown-code',       // document names a diagnostic the compiler does not have
      ],
    },
    docPath: { type: 'string' },
    lines: { type: 'array', items: { type: 'integer' } },
    claimA: { type: 'string', description: 'Verbatim quote of the first text.' },
    claimB: { type: 'string', description: 'Verbatim quote of the conflicting or missing text. Empty string if the kind has only one side.' },
    whyItBlocks: { type: 'string', description: 'What cannot be written correctly until this is ruled.' },
    affectedBlockIds: { type: 'array', items: { type: 'string' } },
  },
}

const CELL_SCHEMA = {
  type: 'object',
  additionalProperties: false,
  required: ['id', 'blockId', 'desc', 'fragment', 'definition', 'specCite', 'quote', 'expectedBasis', 'expected', 'expectedUnderdetermined', 'conflict'],
  properties: {
    id: { type: 'string', description: 'kebab-case, prefixed "<unit>/", globally unique' },
    blockId: { type: 'string', description: 'the block id you were given; must be one of them' },
    desc: { type: 'string', description: 'one line: the behaviour being checked' },
    fragment: { type: 'string', description: 'the short DSL fragment the document states, isolating the behaviour' },
    definition: { type: 'string', description: 'a COMPLETE minimal precept definition embedding the fragment, written to the construction rules' },
    specCite: { type: 'string', description: '<docPath>:<line> or <docPath>:<from>-<to>' },
    quote: { type: 'string', description: 'VERBATIM text from those lines — the sentence or table row the expectation comes from' },
    expectedBasis: { type: 'string', description: 'one line: how that quoted text yields this expectation' },
    expected: { type: 'string' },
    expectedUnderdetermined: { type: 'string', description: 'required non-empty when expected is reject:any; empty string otherwise' },
    conflict: { type: 'boolean', description: 'true if this expectation depends on text that contradicts another part of the documents — also raise an escalation' },
  },
}

const BATCH_SCHEMA = {
  type: 'object',
  additionalProperties: false,
  required: ['unit', 'batchId', 'dispositions', 'cells', 'escalations', 'wroteCellsFile', 'cellsOmittedForLength', 'omissionNote'],
  properties: {
    unit: { type: 'string' },
    batchId: { type: 'string' },
    dispositions: {
      type: 'array',
      items: {
        type: 'object',
        additionalProperties: false,
        required: ['blockId', 'disposition', 'reason', 'nonTestableRows', 'nonTestableReason'],
        properties: {
          blockId: { type: 'string' },
          disposition: { type: 'string', enum: ['BEHAVIOR', 'NO-BEHAVIOR'] },
          reason: { type: 'string', description: 'required for NO-BEHAVIOR: one line saying why there is nothing to check. Empty string for BEHAVIOR.' },
          nonTestableRows: { type: 'integer', description: 'TABLE BLOCKS ONLY, and 0 everywhere else including when every row is testable. How many of this table\'s data rows state nothing testable, so the table-row floor is lowered by that much. Never more than the table\'s data-row count.' },
          nonTestableReason: { type: 'string', description: 'required non-empty when nonTestableRows > 0: which rows, and why each states nothing testable. Empty string otherwise.' },
        },
      },
    },
    cells: { type: 'array', items: CELL_SCHEMA },
    escalations: { type: 'array', items: ESCALATION_SCHEMA },
    wroteCellsFile: { type: 'boolean' },
    // Bounding INPUT lines does not bound OUTPUT cells: a 300-line batch over a wide operator
    // table with the family instruction applied can owe several hundred cell records. An agent
    // that hits its output ceiling will truncate silently unless it has somewhere to say so.
    cellsOmittedForLength: { type: 'integer', description: 'How many cells you identified but could NOT include in this return because the return was getting too long. 0 if you returned every cell you found. Do not pad, do not guess: if you dropped cells, say how many. A non-zero number here fails the run and the batch is re-cut by hand — that is the correct outcome and far better than a silently short return.' },
    omissionNote: { type: 'string', description: 'required non-empty when cellsOmittedForLength > 0: which blocks are short and roughly what is missing. Empty string otherwise.' },
  },
}

const CENSUS_SCHEMA = {
  type: 'object',
  additionalProperties: false,
  required: ['unit', 'docPath', 'totalLines', 'anchors', 'anchorLineCount', 'fenceMarkerLines', 'commandRun'],
  properties: {
    unit: { type: 'string' },
    docPath: { type: 'string' },
    totalLines: { type: 'integer', description: 'from wc -l, plus 1 if the file lacks a trailing newline' },
    commandRun: { type: 'string', description: 'the exact commands you ran, so this is reproducible' },
    // The cross-check on the transcription. Derived by piping the same command to wc -l rather
    // than by counting the array you just typed — the whole point is that it is a SEPARATE
    // number. If it disagrees with the length of `anchors`, you dropped or invented rows.
    anchorLineCount: { type: 'integer', description: 'the output of <the census command> | wc -l. Run it as its own command. Do NOT compute this by counting your own anchors array — it exists precisely to be compared against that array.' },
    // Fence parity. The command tracks fenced-code state; an unbalanced fence makes it swallow
    // every heading and table after it, and the resulting partition still validates cleanly as
    // one giant tail block.
    fenceMarkerLines: { type: 'integer', description: 'the output of: grep -cE \'^[ \\t]*```\' <file>. An odd number means an unbalanced fence and fails the run.' },
    anchors: {
      type: 'array',
      items: {
        type: 'object',
        additionalProperties: false,
        required: ['line', 'kind', 'text', 'tableRows', 'tableCols'],
        properties: {
          line: { type: 'integer' },
          kind: { type: 'string', enum: ['heading', 'table', 'fence'] },
          text: { type: 'string', description: 'the anchor line itself, trimmed, truncated to 120 chars' },
          tableRows: { type: 'integer', description: 'for kind=table: the number of DATA rows (pipe-lines minus header and separator). 0 otherwise. This number drives a hard gate downstream, so count it, do not estimate it.' },
          tableCols: { type: 'integer', description: 'for kind=table: the number of columns in the header row (pipe-delimited fields, ignoring the empty fields at the ends). 0 otherwise. Feeds a warning about matrix tables, not a gate.' },
        },
      },
    },
  },
}

// ─────────────────────────────────────────────────────────────────────────────
// Shared brief
// ─────────────────────────────────────────────────────────────────────────────

const NO_COMPILE = `
HARD RULE — DO NOT COMPILE ANYTHING.
Do not call precept_compile. Do not call any precept MCP tool. Do not run dotnet, the runner,
or the test suite. Do not open src/ to find out what the compiler currently does.
The expectation you write is what the CANONICAL DOCUMENT REQUIRES, reasoned from its text.
It is not what the compiler does, and the compiler's behaviour is not evidence about it.
The compiler is unfinished and at least one of its proof results is known wrong. On
2026-07-14 a run derived 447 expectations from compiler output and all 447 were discarded
inside a day. An unbuilt-but-specified behaviour is a REAL cell — that is the point of the
exercise, not a problem with it.

Do not use samples/ as a measure of what the language covers. They illustrate; they do not
cover. Do not treat the catalogs, DiagnosticCode.cs or any source file as a record of the
language — where code and canonical document disagree, the document wins and the difference
is a bug in the code.

This applies to what you WRITE as well. Never justify an expectation by what the compiler does,
and never say so in expectedBasis. Cite the document. Note that saying "the document states
this is a compile-time error" is CITING THE DOCUMENT and is correct — the prohibition is on
consulting the compiler, not on the words "compile" or "compiler".
`

const SHARED = `
You are enumerating what Precept's canonical documents require of its compiler. Precept is a
.NET domain-integrity DSL: the compiler proves business rules make invalid data structurally
impossible — prevention, not detection. The canonical documents state the intended behaviour,
whether or not it is built. This run fixes WHAT must be checked. It produces no verdicts.

DEFINITIONS
- BLOCK — a span of the document. The block list is NOT yours to choose. It came from one fixed
  command over the file (every heading, every table start, every fenced block, plus the prose
  between them) and it covers every line of the document with no gaps and no overlaps. You are
  handed your blocks with their ids and line ranges. You dispose of each one.
- CELL — one specific compile-time behaviour a document says the compiler must have.
  "money divided by money of a different currency is a type error" is a cell. One document
  claim gives one cell.

EVERY BLOCK YOU ARE GIVEN GETS EXACTLY ONE DISPOSITION:
  BEHAVIOR      — it states one or more testable compile-time behaviours. Enumerate the cells.
  NO-BEHAVIOR   — pure prose, rationale, motivation, cross-reference, navigation, or a
                  description of how the compiler is BUILT rather than what it guarantees
                  about a definition. Give a one-line reason. One line, specific to that block.
If you genuinely cannot decide, raise an escalation of kind cannot-dispose. Do not guess and
do not leave a block out — this script counts your dispositions against the block list it gave
you and fails the run on any mismatch.

ENUMERATE THE WHOLE FAMILY, not just the single instance the prose names: every operator x
operand-type, every accessor and member-call, every literal lane x position, every wrong-kind
and wrong-arity misuse, qualifier and unit variants, and the boundary and error cases (empty,
zero, negative, overflow, unguarded). Do not sample. Do not collapse a family to a
representative. If the document defines 8 operators over 4 operand types, that is up to 32
cells. UNDER-ENUMERATION IS THE PRIMARY FAILURE MODE OF THIS PASS — err toward more cells.
THE TABLE-ROW FLOOR, stated exactly as it is enforced. A BEHAVIOR table block with three or
more data rows owes at least (data rows − nonTestableRows) cells, where nonTestableRows is a
number YOU declare on that block's disposition, with a written reason naming which rows and why
they state nothing testable. It defaults to 0. Declaring it is the ONLY way to lower the floor;
it is not a loophole, it is the accounting entry for "these three rows are examples, not
requirements", and the reason is printed in the report next to the block. This is a floor and
not a target: a 3-row by 5-column matrix table states far more than three behaviours, and a
matrix table that produces only one cell per row is reported as thin.

EVERY CELL CARRIES ITS EVIDENCE. Four fields do this work and none of them is optional:
  specCite    the line or line range in the document
  quote       the sentence or table row AT THOSE LINES, copied verbatim, no paraphrase.
              This is checked by a later stage that greps the file for it.
  expectedBasis  one line saying how that quoted text yields this expectation
  expected    the expectation itself
If you cannot quote text that determines the expectation, you do not have a cell — you have
an escalation of kind under-determined.

THE DEFINITION FIELD. Write a COMPLETE minimal precept definition, not a fragment. A later
run compiles exactly this text; nothing downstream should have to author anything. In
2026-07-14 the cells held fragments, the full texts were discarded, 97% of them were
unrecoverable, and the measurement never happened. Construction rules:
  - a precept header, and every field the fragment references declared
  - every field must have a write site, and there must be an initial event, so that the
    definition does not fire the two ambient lifecycle diagnostics:
      PRE0158 FieldNeverSet — the field has no write site. Emitted as a WARNING, so it does
        not break a reject expectation, but it does corrupt an "accept" or a "warns:" one.
      PRE0093 RequiredFieldsNeedInitialEvent — required fields exist with no initial event.
    Between them they fire on essentially any minimal definition, and they are what made 3,446
    probe results unreadable in 2026-07-14.
  - nothing in the definition that is not needed for the cell. Extra rules are extra ways to
    fail for the wrong reason.
  - read one or two files under samples/ first for the authoring FORM only — the layout of a
    definition. They are not evidence of what the language covers.

${EXPECTED_DOC}
${NO_COMPILE}
`

// ─────────────────────────────────────────────────────────────────────────────
// Small helpers
// ─────────────────────────────────────────────────────────────────────────────

const problems = []          // hard gate failures — these end the run
const warnings = []          // reportable, non-fatal
const escalations = []       // collected from returns; never re-judged by an agent

function fail(msg) { problems.push(msg); log(`GATE FAILURE — ${msg}`) }
function warn(msg) { warnings.push(msg); log(`warning — ${msg}`) }

// Every fan-out in this file goes through this. The runtime's failure contract for `parallel`
// and `pipeline` is not documented anywhere I could find — the older block-inventory workflow
// `.filter(Boolean)`s its results, which implies null-on-failure, but that is inference. This
// makes the question moot: a thunk that throws still yields a TAGGED object, so a thrown thunk
// is never indistinguishable from a phase that ran and found nothing. `tag` identifies what was
// being worked on; `r` is null when it failed and `threw` says why.
function guard(tag, promise) {
  return promise.then(r => ({ ...tag, r, threw: '' }), e => ({ ...tag, r: null, threw: String((e && e.message) || e) }))
}

// Bound what goes into a prompt, and SAY that it is bounded rather than letting the agent
// truncate silently. The report prompt used to interpolate `problems` in full.
function bounded(arr, render, max = PROMPT_LIST_MAX) {
  if (!arr.length) return '  (none)'
  const out = arr.slice(0, max).map(render)
  if (arr.length > max) out.push(`  … showing ${max} of ${arr.length}. The full list is in the workflow's return value, not in this prompt. Say so in what you write.`)
  return out.join('\n')
}

// Validate the census transcription, then build the block partition from it.
//
// What is arithmetic here: given an anchor list, the block set follows from the line numbers and
// no agent's judgement enters. What is NOT: the anchor list itself was retyped by a model from
// the output of a shell command this script cannot run. The three checks that follow — anchor
// text matching its declared kind, line numbers in range and strictly increasing, and a
// separately-derived count compared in the caller — are what stands between the block count and
// "an agent's word for it".
//
// Deliberately NOT here: contiguity assertions over the built partition. They were removed on
// 2026-07-25 because they could not fail. `end` is defined as `next.line - 1`, `starts` is
// sorted and deduplicated, a front-matter start is inserted when the first anchor is past line
// 1, and the last `end` is assigned totalLines — so "starts at 1", "ends at totalLines" and
// "no gap between adjacent blocks" are all algebraically true by construction. A check that
// cannot fail publishes a clean result that means nothing, which is the failure this whole file
// is built against. The real checks are below and in the census loop.
function partition(unit, census) {
  const KIND_RE = { heading: /^#{1,6} /, table: /^[ \t]*\|/, fence: /^[ \t]*```/ }

  // Per-anchor validation, over the array AS RETURNED — before the sort below, because the
  // command emits in increasing line order and a return that is not in that order has been
  // reordered or reconstructed rather than transcribed. Sorting first would make this check
  // unfalsifiable, which is the mistake the removed contiguity assertions made.
  let lastLine = 0
  for (const a of census.anchors) {
    if (!Number.isInteger(a.line) || a.line < 1 || a.line > census.totalLines) {
      fail(`${unit}: census anchor at line ${a.line} is outside 1..${census.totalLines}`)
    }
    if (a.line < lastLine) fail(`${unit}: census anchors are out of order — line ${a.line} follows line ${lastLine}; the command emits in increasing line order`)
    lastLine = a.line
    const re = KIND_RE[a.kind]
    if (re && !re.test(a.text || '')) {
      fail(`${unit}: anchor at line ${a.line} is declared kind=${a.kind} but its text does not look like one: ${JSON.stringify((a.text || '').slice(0, 60))}`)
    }
    if (a.kind === 'table' && (a.tableRows || 0) === 0) {
      // The census prompt used to say "report 0 if the run is malformed", which silently
      // disabled the only mechanical under-enumeration check for that table.
      warn(`${unit}: table anchor at line ${a.line} reports 0 data rows — the table-row floor does not apply to it. Check the table by hand: ${JSON.stringify((a.text || '').slice(0, 60))}`)
    }
  }

  const anchors = [...census.anchors].sort((a, b) => a.line - b.line)
  const starts = []
  if (!anchors.length || anchors[0].line > 1) {
    starts.push({ line: 1, kind: 'prose', text: '(front matter)', tableRows: 0, tableCols: 0 })
  }
  for (const a of anchors) {
    if (starts.length && starts[starts.length - 1].line === a.line) continue  // two anchors, one line
    starts.push(a)
  }
  const blocks = []
  for (let i = 0; i < starts.length; i++) {
    const end = i + 1 < starts.length ? starts[i + 1].line - 1 : census.totalLines
    if (end < starts[i].line) { fail(`${unit}: census anchors are not monotonic at line ${starts[i].line}`); return [] }
    blocks.push({
      id: `${unit}/b${String(i + 1).padStart(4, '0')}`,
      unit,
      docPath: census.docPath,
      kind: starts[i].kind,
      text: starts[i].text,
      tableRows: starts[i].tableRows || 0,
      tableCols: starts[i].tableCols || 0,
      startLine: starts[i].line,
      endLine: end,
      lines: end - starts[i].line + 1,
    })
  }
  if (!blocks.length) fail(`${unit}: census produced no blocks`)
  return blocks
}

// Batches are runs of contiguous blocks. One batch is the containment boundary: a bad prompt
// or a dead agent spoils exactly one, and it is regenerated in place.
function batchBlocks(blocks) {
  const out = []
  let cur = []
  let lines = 0
  for (const b of blocks) {
    if (cur.length && (lines + b.lines > BATCH_MAX_LINES || cur.length >= BATCH_MAX_BLOCKS)) {
      out.push(cur); cur = []; lines = 0
    }
    cur.push(b); lines += b.lines
  }
  if (cur.length) out.push(cur)
  return out
}

function blockTable(blocks) {
  return blocks.map(b =>
    `${b.id} | ${b.kind}${b.kind === 'table' ? ` (${b.tableRows} data rows x ${b.tableCols} columns)` : ''} | lines ${b.startLine}-${b.endLine} | ${b.text}`
  ).join('\n')
}

// The specCite a cell carries should point inside the block it hangs off. Both numbers are here.
function citeLines(specCite) {
  const m = /:(\d+)(?:-(\d+))?\s*$/.exec(specCite || '')
  if (!m) return null
  const from = Number(m[1])
  return { from, to: m[2] ? Number(m[2]) : from }
}

// ─────────────────────────────────────────────────────────────────────────────
// Phase 1 — Vocabulary
//
// Two things fetched once and handed to every enumerator: the real diagnostic names, so all
// cells spell them one way; and the authoring form of a clean definition. Neither is used to
// decide what the compiler SHOULD do.
// ─────────────────────────────────────────────────────────────────────────────

phase('Vocabulary')

const vocab = await agent(`Two mechanical lookups. Do not compile anything and do not judge any behaviour.

1. Read ${DIAGNOSTIC_CODE_PATH}. Return EVERY enum member name together with its PRE#### number
   — the complete list, not a selection and not the interesting ones. This list exists only so
   that several dozen agents spell the same diagnostic the same way. It is NOT a statement about
   what the language requires — the canonical documents are that, and where the two disagree the
   document wins.

   Then, as a SEPARATE command, count the members mechanically — something like
   \`grep -cE '^\\s+[A-Za-z][A-Za-z0-9_]*\\s*=\\s*[0-9]+,' ${DIAGNOSTIC_CODE_PATH}\` — and return
   that integer in memberCountFromGrep along with the exact command you ran. Do not compute it by
   counting the array you just typed: it exists to be compared against that array. A partial list
   that goes unnoticed is worse than no list, because downstream every real diagnostic name then
   reads as one the compiler does not have.

2. Read two or three files in samples/ and return a short authoring note: the minimal shape of
   a precept definition that declares a field, gives it a write site, and has an initial event,
   so that it does not fire PRE0158 FieldNeverSet (the field has no write site — a Warning) or
   PRE0093 RequiredFieldsNeedInitialEvent (required fields with no initial event). Give one
   complete worked example of such a definition, ~10-15 lines, as text. This is authoring FORM
   only — samples are not evidence of what the language covers and must never be used as a
   measure of coverage.`, {
  label: 'vocabulary',
  phase: 'Vocabulary',
  model: 'sonnet',
  effort: 'medium',
  schema: {
    type: 'object', additionalProperties: false,
    required: ['codes', 'memberCountFromGrep', 'countCommandRun', 'cleanDefinitionNote', 'cleanDefinitionExample'],
    properties: {
      codes: {
        type: 'array',
        items: {
          type: 'object', additionalProperties: false, required: ['name', 'pre'],
          properties: { name: { type: 'string' }, pre: { type: 'string' } },
        },
      },
      memberCountFromGrep: { type: 'integer' },
      countCommandRun: { type: 'string' },
      cleanDefinitionNote: { type: 'string' },
      cleanDefinitionExample: { type: 'string' },
    },
  },
})

// A dead vocabulary agent was already a gate failure. A PARTIAL one was not, and it is worse:
// CODE_NAMES becomes a short set, every legitimately-named diagnostic downstream fails the
// membership test, and the report fills with false "the documents name a code the compiler does
// not have" findings. Both directions are gated here against a count the agent derived
// separately, and against the member count measured by hand on 2026-07-25.
if (!vocab) {
  fail('vocabulary agent did not return; enumerators would have no diagnostic name list')
} else {
  const listed = (vocab.codes || []).length
  if (listed !== vocab.memberCountFromGrep) {
    fail(`vocabulary listed ${listed} diagnostic names but its own count of ${DIAGNOSTIC_CODE_PATH} found ${vocab.memberCountFromGrep} members (command: ${vocab.countCommandRun}) — the name list is partial and every downstream unknown-code finding would be false`)
  }
  if (listed !== CODE_COUNT_BASELINE) {
    warn(`vocabulary listed ${listed} diagnostic names; the baseline measured 2026-07-25 is ${CODE_COUNT_BASELINE}. Either the enum changed or the list is wrong.`)
  }
}
const CODE_NAMES = new Set((vocab?.codes || []).map(c => c.name))
const CODE_LIST = (vocab?.codes || []).map(c => `${c.name} (${c.pre})`).join(', ')
log(`Vocabulary: ${CODE_NAMES.size} diagnostic names`)

// Abort before the expensive phases. A dead or partial vocabulary agent costs one sonnet call
// to detect and, left alone, is discovered after ~140 opus enumerators, a repair round, the
// challenges and the audits have all been paid for. The comparable failure cost $1,020.
if (problems.length) {
  log(`Aborting after Vocabulary — ${problems.length} hard failure(s) before any expensive phase ran.`)
  for (const p of problems) log(`  ${p}`)
  if (THROW_ON_GATE_FAILURE) throw new Error(`enumeration aborted in Vocabulary: ${problems.join(' | ')}`)
}

// ─────────────────────────────────────────────────────────────────────────────
// Phase 2 — Census
//
// One agent per document running one fence-aware awk. The COMMAND makes no judgement, so the
// block list is not anybody's opinion about how many blocks a document has. The TRANSCRIPTION
// of its output into JSON is still a model retyping up to 245 rows, and that is the largest
// unchecked assumption in the run unless it is checked — so it is checked three ways here (a
// separately-derived count, a pinned baseline, per-anchor validation in `partition`) plus fence
// parity. See CENSUS CHECKS in the header.
//
// Fence-awareness matters: the language spec has a line reading "# This is a standalone
// comment" inside a fenced example, which a naive heading grep counts as a section. Verified
// 2026-07-25 — fence-awareness suppresses 1 false heading in the spec and 17 in
// business-domain-types.md.
// ─────────────────────────────────────────────────────────────────────────────

phase('Census')

// String.raw so the backslashes reach awk intact. The fence pattern is interpolated because a
// literal backtick cannot appear unescaped in a template, and an escaped one would keep its
// backslash under String.raw.
const FENCE = '```'
const CENSUS_AWK = String.raw`awk '
{
  if ($0 ~ /^[ \t]*${FENCE}/) { infence = !infence; if (infence) print NR "\tfence\t" $0; prev=""; next }
  if (infence) { next }
  if ($0 ~ /^#{1,6} /)        { print NR "\theading\t" $0; prev=$0; next }
  if ($0 ~ /^[ \t]*\|/ && prev !~ /^[ \t]*\|/) { print NR "\ttable\t" $0 }
  prev=$0
}' `

// Results are matched to their unit by a tag carried through the thunk, not by array position.
const censuses = await parallel(ACTIVE.map(u => () => guard({ u }, agent(
  `Mechanical block census of ${u.path}. No judgement, no reading for meaning, no compiling.

Run exactly this, and report the command you ran:

  ${CENSUS_AWK}${u.path}

It walks the file tracking fenced-code state, and prints one line per anchor: the line number,
the kind (heading / fence / table), and the anchor line itself. It skips anything inside a
fenced block on purpose — this file contains lines that look like headings inside examples.

Then run these as SEPARATE commands and report each result:
  - the total line count: wc -l (add 1 if the file has no trailing newline)
  - the anchor count, by piping the same census command to wc -l. This is a cross-check on your
    transcription and it only works if it is a separate command. Do NOT count the array you
    typed and report that number — the two are compared and a disagreement fails the run,
    which is the point.
  - the fence-marker line count: grep -cE '^[ \\t]*${FENCE}' ${u.path}. An odd result means an
    unbalanced fence, which would make the census swallow the rest of the file.

Then, per anchor:
  - for every anchor of kind "table", count its DATA rows: the run of consecutive lines starting
    at that anchor that begin with a pipe, minus the header row and the separator row. This
    number drives a hard gate downstream — count it, do not estimate it. If a table is malformed
    and you cannot count it, report 0 and it will be flagged for a human to look at rather than
    passed over. Report 0 for every non-table anchor.
  - for every anchor of kind "table", also report tableCols: how many columns the header row has
    (pipe-delimited fields, not counting the empty fields at the two ends). 0 for non-tables.
  - trim each anchor's text and truncate it to 120 characters.

Return the schema. unit="${u.unit}", docPath="${u.path}". Report every anchor the command
printed, in the order it printed them — do not filter, do not merge, do not tidy, do not
reorder. A later stage decides what is interesting; your job is that the list is complete and
the line numbers are right. This transcription is the denominator of the entire run: a row you
drop becomes a span of the document that no agent is ever asked to read.`,
  { label: `census:${u.unit}`, phase: 'Census', model: 'sonnet', effort: 'medium', schema: CENSUS_SCHEMA }
))))

const BLOCKS = []            // every block, every unit
const blocksByUnit = {}
const bigBlocks = []         // blocks over BIG_BLOCK_LINES — reported with their cell counts
if (censuses.length !== ACTIVE.length) {
  fail(`census fan-out returned ${censuses.length} results for ${ACTIVE.length} documents — the runtime dropped ${ACTIVE.length - censuses.length} and this script cannot say which`)
}
for (const u of ACTIVE) blocksByUnit[u.unit] = []
for (const entry of censuses) {
  const u = entry?.u
  const c = entry?.r
  if (!u) { fail('a census thunk returned nothing at all — a document may be unwalked and unnamed'); continue }
  // A dead agent is a gate failure, not a filtered-out array element. The 2026-07-14 script
  // did `.filter(Boolean)` and a missing unit would have vanished from the totals silently.
  if (!c) { fail(`${u.unit}: census agent did not return — document not walked${entry.threw ? ` (${entry.threw})` : ''}`); continue }
  if (c.totalLines <= 0) { fail(`${u.unit}: census reported ${c.totalLines} lines`); continue }

  // The transcription cross-check. Without this the block count is one model's word for it, and
  // a partition over a truncated anchor list still covers every line with no gaps.
  const listed = (c.anchors || []).length
  if (listed !== c.anchorLineCount) {
    fail(`${u.unit}: census transcribed ${listed} anchors but its own separate count of the same command output was ${c.anchorLineCount} — ${Math.abs(listed - c.anchorLineCount)} rows were dropped or invented. The block list for this document is not trustworthy.`)
  }
  const baseline = CENSUS_BASELINE[u.unit]
  if (baseline !== undefined && listed !== baseline) {
    warn(`${u.unit}: ${listed} anchors against the ${baseline} measured on 2026-07-25. Either ${u.path} was edited since, or the census is wrong. Check which before trusting this unit's block count.`)
  }
  if (c.fenceMarkerLines % 2 !== 0) {
    fail(`${u.unit}: ${c.fenceMarkerLines} fence-marker lines in ${u.path} — an odd number, so a fence is unbalanced. The census would have swallowed every heading and table after it, and the partition would still have validated as one giant tail block.`)
  }

  const blocks = partition(u.unit, c)
  blocksByUnit[u.unit] = blocks
  for (const b of blocks) {
    BLOCKS.push(b)
    // An oversized block is either a large fenced example (fence interiors are invisible to the
    // census, so a 214-line csharp listing is ONE block with ONE disposition) or the fingerprint
    // of a dropped anchor. Either way it wants naming rather than averaging.
    if (b.lines >= BIG_BLOCK_LINES) bigBlocks.push(b)
  }
  log(`${u.unit}: ${c.totalLines} lines -> ${blocks.length} blocks`)
}
for (const b of bigBlocks) {
  warn(`oversized block ${b.id} — ${b.lines} lines (${b.kind}) at ${b.docPath}:${b.startLine}-${b.endLine}. One disposition covers all of it. Over the active set as measured on 2026-07-25 the median block is 8 lines and only two blocks reach ${BIG_BLOCK_LINES}; anything else here is a candidate dropped census anchor.`)
}
log(`Census complete: ${BLOCKS.length} blocks across ${ACTIVE.length} documents, ${bigBlocks.length} oversized`)

// Second abort point. A dead or mistranscribed census is cheap to detect and ruinously
// expensive to discover after the enumerators have run.
if (problems.length) {
  log(`Aborting after Census — ${problems.length} hard failure(s) before the ~140 enumerators ran.`)
  for (const p of problems) log(`  ${p}`)
  if (THROW_ON_GATE_FAILURE) throw new Error(`enumeration aborted in Census: ${problems.length} hard failures — ${problems.slice(0, 5).join(' | ')}`)
}

// ─────────────────────────────────────────────────────────────────────────────
// Phase 3 — Enumerate
//
// Per batch: disposition every block, and write the cells for the behaviour-bearing ones.
// The agent writes its own discrete cell file AND returns the records. The file is the
// durable artifact you can open and fix one of; the return is what this script checks.
// ─────────────────────────────────────────────────────────────────────────────

phase('Enumerate')

function enumeratePrompt(u, blocks, batchId, retryNotes, writeSuffix) {
  return `${SHARED}

=== YOUR ASSIGNMENT ===
UNIT: ${u.unit}
DOCUMENT: ${u.path}
BATCH: ${batchId}
Read lines ${blocks[0].startLine}-${blocks[blocks.length - 1].endLine} of ${u.path} in full.
Read a little either side for context if you need it, but dispose of ONLY these blocks:

${blockTable(blocks)}

WHAT TO ENUMERATE IN THIS DOCUMENT: ${GUIDES[u.guide] || GUIDES.GUIDE_GENERIC}

DIAGNOSTIC NAMES — use these spellings, one form only, never PRE####:
${CODE_LIST || '(the name list is unavailable; write the name the document uses and raise a names-unknown-code escalation)'}

WRITING A CLEAN DEFINITION: ${vocab?.cleanDefinitionNote || '(unavailable — follow the construction rules above)'}
${vocab?.cleanDefinitionExample ? `\nWorked example of the form:\n${vocab.cleanDefinitionExample}\n` : ''}
${retryNotes ? `\n=== THIS BATCH IS BEING REDONE. The checks that failed:\n${retryNotes}

Fix exactly these and return the whole batch again. Two things this round must NOT do:
  - It must not shrink the batch. Deleting a cell that failed a check is not a fix; correct it.
    A repair that returns fewer cells for a block than the first pass did, or that turns a
    BEHAVIOR block into a NO-BEHAVIOR one, is rejected outright and the run fails on it.
  - It must not lower a table's floor by declaring rows non-testable unless that is TRUE of
    those specific rows and you can say which and why. The declaration is printed in the report
    next to the block and it is read.
` : ''}
WRITE ${BUNDLE}/cells/${u.unit}/${batchId}${writeSuffix || ''}.json — a JSON array of your cell
objects, exactly the records you return. Then return the schema. Set unit="${u.unit}",
batchId="${batchId}".

Return one disposition for every block id above — all ${blocks.length} of them, no more and no
fewer. Cell ids must start "${u.unit}/" and be unique within your batch.`
}

const BATCHES = []
for (const u of ACTIVE) {
  const groups = batchBlocks(blocksByUnit[u.unit] || [])
  groups.forEach((g, i) => BATCHES.push({ u, blocks: g, batchId: `${u.unit}-${String(i + 1).padStart(3, '0')}` }))
}
log(`Enumerate: ${BATCHES.length} batches`)

// pipeline: enumerate, then a pure-JS per-batch structural check so a batch is validated as
// soon as it lands rather than at the end of the whole fan-out.
const batchResults = await pipeline(
  BATCHES,

  b => agent(enumeratePrompt(b.u, b.blocks, b.batchId), {
    label: b.batchId, phase: 'Enumerate', model: b.u.model, effort: 'high', schema: BATCH_SCHEMA,
  }).then(r => ({ ...b, result: r }), e => ({ ...b, result: null, threw: String((e && e.message) || e) })),

  rec => {
    if (!rec) return rec
    rec.issues = checkBatch(rec)
    const hard = rec.issues.filter(i => i.sev === 'hard').length
    if (rec.issues.length) log(`${rec.batchId}: ${rec.issues.length} check failure(s), ${hard} of them hard`)
    return rec
  },
)

// `pipeline` dropping an element is the same shape of loss as `.filter(Boolean)` on `parallel`
// and would take a whole batch out of the corpus silently. The whole-corpus block accounting
// further down catches it too, but it catches it as ~16 orphaned blocks rather than as the one
// fact that explains them.
if (batchResults.length !== BATCHES.length) {
  const got = new Set(batchResults.filter(Boolean).map(r => r.batchId))
  const lost = BATCHES.filter(b => !got.has(b.batchId)).map(b => b.batchId)
  fail(`enumerate pipeline returned ${batchResults.length} records for ${BATCHES.length} batches; missing: ${lost.join(', ') || '(cannot identify)'}`)
}

// ─────────────────────────────────────────────────────────────────────────────
// The checks. All of them are arithmetic or string tests over the returned records. None of
// them is a question put to a model, and none of them can be settled by a later stage writing
// prose. In 2026-07-14 the equivalent check computed five real problems, was never read, and
// the published report said "100% of blocks are accounted in every unit".
// ─────────────────────────────────────────────────────────────────────────────

// Two severities, because one repair round over 139 batches and ~14 per-cell string tests will
// not come back with zero issues, and a 39-character definition should not end the run the way
// a missing disposition does.
//   hard — the corpus is untrustworthy: a block unaccounted for, a cell on a NO-BEHAVIOR block,
//          a table under its floor, a basis derived from the compiler, a truncated return.
//          These fail the run.
//   soft — this one cell is malformed. The cell is marked, counted per unit in the report, and
//          carried through so the counts still reconcile with what is on disk. The run finishes.
// Both go to the repair round; only `hard` reaches the gate.
function hard(msg, cellId) { return { sev: 'hard', msg, cellId: cellId || '' } }
function soft(msg, cellId) { return { sev: 'soft', msg, cellId: cellId || '' } }

function checkBatch(rec) {
  const iss = []
  const r = rec.result
  if (!r) return [hard(`agent did not return — ${rec.blocks.length} blocks unaccounted${rec.threw ? ` (${rec.threw})` : ''}`)]

  // 0. A return that silently dropped cells because it was getting long. Bounding a batch by
  //    INPUT lines does not bound its OUTPUT: a 300-line batch over a wide operator table with
  //    the family instruction applied can owe several hundred records.
  if ((r.cellsOmittedForLength || 0) > 0) {
    iss.push(hard(`batch reports ${r.cellsOmittedForLength} cells identified but omitted for return length: ${r.omissionNote || '(no note)'} — this batch needs re-cutting by hand into smaller pieces`))
  }

  // 1. Accounting: exactly one disposition per block given, and no extras. Disjoint by
  //    construction — there is no third category to overload, which is how two units broke
  //    the arithmetic last time and had it patched in footnotes.
  const given = new Map(rec.blocks.map(b => [b.id, b]))
  const seen = new Map()
  for (const d of r.dispositions || []) {
    if (!given.has(d.blockId)) iss.push(hard(`disposition for unknown block ${d.blockId}`))
    if (seen.has(d.blockId)) iss.push(hard(`block ${d.blockId} dispositioned twice`))
    seen.set(d.blockId, d)
    if (d.disposition === 'NO-BEHAVIOR' && (d.reason || '').trim().length < 15) {
      iss.push(hard(`block ${d.blockId}: NO-BEHAVIOR with no usable reason — a write-off is the cheapest way out of the denominator and the reason is its only defence`))
    }
    // The floor-lowering declaration has to be about something real and about the right block.
    const nt = d.nonTestableRows || 0
    if (nt > 0) {
      const b = given.get(d.blockId)
      if (!b || b.kind !== 'table') iss.push(hard(`block ${d.blockId}: declares ${nt} non-testable rows but is not a table block`))
      else if (nt > b.tableRows) iss.push(hard(`block ${d.blockId}: declares ${nt} non-testable rows but the table has ${b.tableRows} data rows`))
      if (!(d.nonTestableReason || '').trim()) iss.push(hard(`block ${d.blockId}: declares ${nt} non-testable rows with no statement of which rows or why`))
    }
  }
  for (const b of rec.blocks) if (!seen.has(b.id)) iss.push(hard(`block ${b.id} has no disposition`))

  // 2. Every behaviour-bearing block owes at least one cell; no cell may hang off a
  //    NO-BEHAVIOR block or a block from another batch.
  const cellsByBlock = new Map()
  for (const c of r.cells || []) {
    if (!given.has(c.blockId)) { iss.push(hard(`cell ${c.id} cites block ${c.blockId}, not in this batch`, c.id)); continue }
    if (!cellsByBlock.has(c.blockId)) cellsByBlock.set(c.blockId, [])
    cellsByBlock.get(c.blockId).push(c)
  }
  for (const [id, d] of seen) {
    const n = (cellsByBlock.get(id) || []).length
    if (d.disposition === 'BEHAVIOR' && n === 0) iss.push(hard(`block ${id}: BEHAVIOR with zero cells`))
    if (d.disposition === 'NO-BEHAVIOR' && n > 0) iss.push(hard(`block ${id}: NO-BEHAVIOR but carries ${n} cells`))
  }

  // 3. Table-row floor. A table block with 40 data rows and 3 cells is under-enumerated, and
  //    that is the one shape of under-enumeration a script can actually detect.
  //
  //    The excusal used to be "any escalation of any kind naming this block", which meant one
  //    `cannot-dispose` object voided an arbitrary shortfall — and the repair prompt handed the
  //    agent the failure text verbatim, so the check told the agent being checked how to get
  //    past it for the cost of one object. Now the floor is lowered only by the block's own
  //    `nonTestableRows` declaration, which is specific, counted, and printed in the report; and
  //    the two escalation kinds that genuinely mean "the document does not determine this" are
  //    the only ones that still excuse the remainder. Every excused shortfall is recorded.
  const EXCUSING_KINDS = new Set(['under-determined', 'unschemable'])
  rec.excusedShortfalls = []
  for (const b of rec.blocks) {
    if (b.kind !== 'table' || b.tableRows < 3) continue
    const d = seen.get(b.id)
    if (!d || d.disposition !== 'BEHAVIOR') continue
    const floor = Math.max(0, b.tableRows - (d.nonTestableRows || 0))
    const n = (cellsByBlock.get(b.id) || []).length
    if (n < floor) {
      const excused = (r.escalations || []).some(e => EXCUSING_KINDS.has(e.kind) && (e.affectedBlockIds || []).includes(b.id))
      if (excused) {
        rec.excusedShortfalls.push({ blockId: b.id, rows: b.tableRows, nonTestable: d.nonTestableRows || 0, cells: n, floor })
      } else {
        iss.push(hard(`block ${b.id}: table has ${b.tableRows} data rows, ${d.nonTestableRows || 0} declared non-testable, so it owes ${floor} cells and has ${n}. Write the missing cells.`))
      }
    }
  }

  // 4. Per-cell evidence.
  const blockOf = given
  for (const c of r.cells || []) {
    const id = c.id
    if (!(c.id || '').startsWith(`${rec.u.unit}/`)) iss.push(soft(`cell ${id}: id is not prefixed "${rec.u.unit}/"`, id))
    if (!EXPECTED_RE.test(c.expected || '')) iss.push(soft(`cell ${id}: expected "${c.expected}" is not one of the six shapes`, id))
    // One naming form. 2026-07-14 shipped 360 cells in PRE#### form and 522 in enum-name form
    // for the same diagnostics, and nothing in the bundle converted between them.
    if (/^(?:reject|warns|accept-with-warning):PRE\d/i.test(c.expected || '')) iss.push(soft(`cell ${id}: expected "${c.expected}" uses the PRE#### form; use the DiagnosticCode.cs member name`, id))
    if (c.expected === 'reject:any' && !(c.expectedUnderdetermined || '').trim()) iss.push(soft(`cell ${id}: reject:any with no statement of what the document fails to determine`, id))
    if ((c.quote || '').trim().length < 12) iss.push(soft(`cell ${id}: quote missing or too short to anchor anything`, id))
    if (!(c.expectedBasis || '').trim()) iss.push(soft(`cell ${id}: no expectedBasis`, id))
    if (!(c.specCite || '').startsWith(`${rec.u.path}:`)) iss.push(soft(`cell ${id}: specCite "${c.specCite}" is not ${rec.u.path}:<line>`, id))
    if (!(c.fragment || '').trim()) iss.push(soft(`cell ${id}: empty fragment`, id))
    if ((c.definition || '').trim().length < 40) iss.push(soft(`cell ${id}: definition is missing, or is a fragment rather than a complete definition`, id))
    // Free, and nothing else in the run does it: the cited line should be inside the block the
    // cell hangs off. Both numbers are already here.
    const cl = citeLines(c.specCite)
    const b = blockOf.get(c.blockId)
    if (cl && b && (cl.from < b.startLine || cl.from > b.endLine)) {
      iss.push(soft(`cell ${id}: cites ${rec.u.path}:${cl.from}, which is outside block ${c.blockId} (lines ${b.startLine}-${b.endLine})`, id))
    }
    if (COMPILER_BASIS_RE.test(c.expectedBasis || '')) {
      iss.push(hard(`cell ${id}: expectedBasis reads as a report of what the compiler does rather than what the document says — ${JSON.stringify((c.expectedBasis || '').slice(0, 120))}`, id))
    }
  }
  if (!r.wroteCellsFile) iss.push(hard('batch reports it did not write its cells file'))
  return iss
}

// ─────────────────────────────────────────────────────────────────────────────
// Phase 4 — Repair. One bounded round. Anything still failing after it is a gate failure and
// stays visible; it is not resolved by a downstream writer.
// ─────────────────────────────────────────────────────────────────────────────

phase('Repair')

const needRepair = batchResults.filter(r => r && r.issues.length)
log(`Repair: ${needRepair.length}/${batchResults.length} batches`)

// Cell counts and dispositions BEFORE the repair round, so a repair that "fixes" five quote
// failures by deleting the five cells that had them can be seen doing it. Comparing issue counts
// alone scores that repair 0 and accepts it.
function shapeOf(result) {
  const cells = new Map()
  const disp = new Map()
  for (const c of (result?.cells || [])) cells.set(c.blockId, (cells.get(c.blockId) || 0) + 1)
  for (const d of (result?.dispositions || [])) disp.set(d.blockId, d.disposition)
  return { cells, disp, total: (result?.cells || []).length }
}

// A repair writes to <batchId>.repair.json rather than over <batchId>.json. The old arrangement
// wrote unconditionally and accepted conditionally, so a rejected repair sat on disk while
// memory held the original — and the divergence surfaced as an unexplained count mismatch at the
// merge, in the last phase, with no repair path. The merge is told below which file this script
// accepted for each batch.
const repairAccepted = new Map()   // batchId -> 'original' | 'repair'
const repairLog = []               // one row per repaired batch, printed in the report

if (needRepair.length) {
  const repaired = await parallel(needRepair.map(rec => () => guard({ rec }, agent(
    enumeratePrompt(rec.u, rec.blocks, rec.batchId, rec.issues.map(i => `  - [${i.sev}] ${i.msg}`).join('\n'), '.repair'),
    { label: `repair:${rec.batchId}`, phase: 'Repair', model: rec.u.model, effort: 'high', schema: BATCH_SCHEMA }
  ))))
  for (const entry of repaired) {
    if (!entry || !entry.rec) { fail('a repair thunk returned nothing at all and cannot be named'); continue }
    const rec = entry.rec
    repairAccepted.set(rec.batchId, 'original')
    if (!entry.r) { warn(`repair of ${rec.batchId} did not return${entry.threw ? ` (${entry.threw})` : ''}; the first-pass result and its ${rec.issues.length} issues stand`); continue }

    const before = shapeOf(rec.result)
    const after = shapeOf(entry.r)
    const iss = checkBatch({ ...rec, result: entry.r })

    // A repair may not shrink the corpus. Neither of these is a judgement call: both are
    // arithmetic over the two returns.
    const shrunk = []
    for (const [blockId, n] of before.cells) {
      const m = after.cells.get(blockId) || 0
      if (m < n) shrunk.push(`${blockId} ${n}->${m} cells`)
    }
    for (const [blockId, d] of before.disp) {
      if (d === 'BEHAVIOR' && after.disp.get(blockId) === 'NO-BEHAVIOR') shrunk.push(`${blockId} BEHAVIOR->NO-BEHAVIOR`)
    }
    if (shrunk.length) {
      fail(`repair of ${rec.batchId} shrank the corpus rather than fixing it: ${shrunk.join('; ')}. Deleting a cell that failed a check is not a repair.`)
      repairLog.push({ batchId: rec.batchId, before: before.total, after: after.total, accepted: false, why: `shrank: ${shrunk.join('; ')}` })
      continue
    }

    const hardBefore = rec.issues.filter(i => i.sev === 'hard').length
    const hardAfter = iss.filter(i => i.sev === 'hard').length
    if (hardAfter <= hardBefore && iss.length <= rec.issues.length) {
      rec.result = entry.r
      rec.issues = iss
      repairAccepted.set(rec.batchId, 'repair')
      repairLog.push({ batchId: rec.batchId, before: before.total, after: after.total, accepted: true, why: `${rec.issues.length} issues remain` })
    } else {
      // Rejected in memory AND on disk — the merge is told to use the original file.
      warn(`repair of ${rec.batchId} came back worse (${hardBefore}->${hardAfter} hard, ${rec.issues.length}->${iss.length} total) and was rejected; the merge uses the original file`)
      repairLog.push({ batchId: rec.batchId, before: before.total, after: after.total, accepted: false, why: `worse: ${hardBefore}->${hardAfter} hard issues` })
    }
  }
}

// Hard issues end the run. Soft issues mark their cell, are counted per unit in the report, and
// do not — a 39-character definition is a malformed cell, not an untrustworthy corpus, and one
// repair round over 139 batches will not clear every per-cell string test.
const malformedCells = new Map()   // cellId -> [reasons]
for (const rec of batchResults) {
  if (!rec) continue
  for (const i of rec.issues) {
    if (i.sev === 'hard') fail(`${rec.batchId}: ${i.msg}`)
    else {
      if (i.cellId) {
        if (!malformedCells.has(i.cellId)) malformedCells.set(i.cellId, [])
        malformedCells.get(i.cellId).push(i.msg)
      }
      warnings.push(`${rec.batchId}: ${i.msg}`)
    }
  }
}
log(`Repair complete: ${problems.length} hard failures, ${malformedCells.size} cells marked malformed`)

// Table shortfalls that an under-determined/unschemable escalation excused. Excusing one is
// legitimate; excusing it invisibly is not, which is what "any escalation naming this block"
// used to do.
const excusedShortfalls = []
for (const rec of batchResults) {
  for (const s of (rec?.excusedShortfalls || [])) excusedShortfalls.push({ ...s, batchId: rec.batchId })
}
if (excusedShortfalls.length) {
  const rowsUnenumerated = excusedShortfalls.reduce((a, s) => a + (s.floor - s.cells), 0)
  warn(`${excusedShortfalls.length} table blocks are under their row floor and excused by an under-determined or unschemable escalation, leaving ${rowsUnenumerated} data rows with no cell: ${excusedShortfalls.map(s => `${s.blockId} (${s.cells}/${s.floor})`).join(', ')}`)
}

// ─────────────────────────────────────────────────────────────────────────────
// Collect. Everything below works from these arrays, which are built here from the agents'
// own returns. No stage is asked to carry a finding forward on trust.
// ─────────────────────────────────────────────────────────────────────────────

const CELLS = []
const dispositionOf = new Map()
for (const rec of batchResults) {
  if (!rec || !rec.result) continue
  for (const d of rec.result.dispositions || []) dispositionOf.set(d.blockId, d)
  for (const c of rec.result.cells || []) {
    CELLS.push({ ...c, unit: rec.u.unit, batchId: rec.batchId, malformed: malformedCells.get(c.id) || [] })
  }
  for (const e of rec.result.escalations || []) escalations.push({ ...e, unit: rec.u.unit, batchId: rec.batchId })
}

// Global id uniqueness — batch-local checks cannot see this.
const idSeen = new Map()
for (const c of CELLS) {
  if (idSeen.has(c.id)) fail(`duplicate cell id ${c.id} (${idSeen.get(c.id)} and ${c.batchId})`)
  idSeen.set(c.id, c.batchId)
}

// Whole-corpus accounting: every block from the partition must have exactly one disposition
// somewhere. The live check here is `blocksMissing` — a block that no batch dispositioned,
// which is how a batch that vanished entirely (or a `pipeline` that dropped an element) shows
// up. The three-way sum against BLOCKS.length that used to sit here was removed on 2026-07-25:
// every block increments exactly one of three counters, so the sum was a tautology that could
// not fail, and printing it as an accounting check made an unfalsifiable statement look like a
// verified one.
//
// Recomputed rather than computed once, because phase 5 can flip a disposition and the figures
// the report prints must be the ones after that.
let blocksBehavior = 0, blocksNone = 0, blocksMissing = 0
function recountBlocks(reportMissing) {
  blocksBehavior = 0; blocksNone = 0; blocksMissing = 0
  for (const b of BLOCKS) {
    const d = dispositionOf.get(b.id)
    if (!d) {
      blocksMissing++
      if (reportMissing) fail(`block ${b.id} (${b.docPath}:${b.startLine}-${b.endLine}) has no disposition anywhere`)
      continue
    }
    if (d.disposition === 'BEHAVIOR') blocksBehavior++; else blocksNone++
  }
}
recountBlocks(true)

log(`Collected: ${CELLS.length} cells, ${blocksBehavior} behaviour blocks, ${blocksNone} no-behaviour, ${escalations.length} escalations`)

// ─────────────────────────────────────────────────────────────────────────────
// Phase 5 — Challenge the thinnest blocks.
//
// The named primary failure mode is under-enumeration, and the family instruction is pressure
// rather than a check. This is the check that is possible: rank behaviour blocks by cells per
// line, take the thinnest, and have a fresh agent enumerate the SAME BLOCK FROM THE DOCUMENT
// with no sight of the existing cells. Independence is the whole point — in 2026-07-14 the
// verifier was handed the prober's cell descriptions, re-materialised the same flawed snippet
// and confirmed the same wrong answer. A checker that reads the prior agent's output is
// correlated with it and adds nothing.
// ─────────────────────────────────────────────────────────────────────────────

phase('Challenge')

const cellCountByBlock = new Map()
for (const c of CELLS) cellCountByBlock.set(c.blockId, (cellCountByBlock.get(c.blockId) || 0) + 1)

// The population this sample comes from, so the report can state the denominator rather than
// just "40 blocks challenged". Ranking by cells-per-line systematically picks long thin prose
// blocks and can never pick a 20-line block with 4 cells that owes 40; the `lines >= 8`
// threshold excludes short blocks outright. Both facts are printed.
const behaviorBlocks = BLOCKS.filter(b => dispositionOf.get(b.id)?.disposition === 'BEHAVIOR')
const eligible = behaviorBlocks.filter(b => b.lines >= 8)
const challengeExcludedShort = behaviorBlocks.length - eligible.length

const candidates = eligible
  .map(b => ({ b, n: cellCountByBlock.get(b.id) || 0, density: (cellCountByBlock.get(b.id) || 0) / b.lines }))
  .sort((a, b) => a.density - b.density)
  .slice(0, CHALLENGE_BLOCKS)

log(`Challenge: ${candidates.length} of ${eligible.length} eligible behaviour blocks (${challengeExcludedShort} excluded as under 8 lines)`)

const challenges = await parallel(candidates.map(({ b, n }) => () => guard({ b, n }, agent(
  `${SHARED}

=== INDEPENDENT ENUMERATION ===
You are enumerating one block from scratch. Another agent has already enumerated it. You are
NOT being shown its work and you must not go looking for it — do not read anything under
${BUNDLE}. Two agents reading the same text independently is the only kind of second opinion
worth having here.

DOCUMENT: ${b.docPath}
BLOCK: ${b.id} — ${b.kind}, lines ${b.startLine}-${b.endLine}
  ${b.text}

Read those lines. Enumerate every distinct compile-time behaviour the block requires, to the
family standard in the brief above. Return a one-line description of each — you do NOT need to
write definitions, quotes or expectations here. Count carefully and do not round.

Also return, in whatMattersMost, the two or three behaviours in your list that you think are
most likely to be missed by a reader working quickly.

If while reading you find a document-internal contradiction, or text that states an outcome
without determining it, put it in escalations. An empty array is how you say there is nothing;
there is no other way to say it.`,
  { label: `challenge:${b.id}`, phase: 'Challenge', model: 'opus', effort: 'high',
    schema: {
      type: 'object', additionalProperties: false,
      required: ['blockId', 'behaviors', 'whatMattersMost', 'escalations'],
      properties: {
        blockId: { type: 'string' },
        behaviors: { type: 'array', items: { type: 'string' } },
        whatMattersMost: { type: 'array', items: { type: 'string' } },
        escalations: { type: 'array', items: ESCALATION_SCHEMA },
      },
    } }
))))

// Three numbers, not one. "0 shortfalls" out of 40 challenged used to be printed identically
// whether 40 agents read 40 blocks and found nothing wrong or 40 thunks threw before reading
// anything — which is exactly what happened, because `n` was unbound in this fan-out and every
// thunk raised a ReferenceError. A non-return is now a hard failure, not a warning: this is the
// ONLY check in the run aimed at the failure mode the brief calls primary, and a phase that
// silently does not run is worse than a phase that is switched off.
const shortfalls = []
const challengeComparisons = []
let challengeReturned = 0
for (const entry of challenges) {
  if (!entry || !entry.b) { fail('a challenge thunk returned nothing at all and cannot be named — the under-enumeration check did not cover its block'); continue }
  const { b, n, r: ch } = entry
  if (!ch) { fail(`challenge on ${b.id} did not return${entry.threw ? ` (${entry.threw})` : ''}; its first-pass ${n} cells are unchallenged and the primary failure mode is unchecked for that block`); continue }
  challengeReturned++
  for (const e of ch.escalations || []) escalations.push({ ...e, unit: b.unit, batchId: `challenge:${b.id}` })
  const found = (ch.behaviors || []).length
  // Every comparison is recorded, not only the ones over the ratio. A block with 4 cells where
  // the challenger found 6 is a 50% shortfall that does not clear 1.5x, and dropping it with no
  // record means the report cannot show how close to the line the corpus sits.
  challengeComparisons.push({ blockId: b.id, docPath: b.docPath, lines: `${b.startLine}-${b.endLine}`, primary: n, challenger: found })
  if (found > Math.max(1, n) * CHALLENGE_SHORTFALL_RATIO) {
    shortfalls.push({ block: b, primary: n, challenger: found, gap: found - n, behaviors: ch.behaviors, mattersMost: ch.whatMattersMost })
  }
}
log(`Challenge: ${challengeReturned}/${candidates.length} challengers returned, ${shortfalls.length} over the ${CHALLENGE_SHORTFALL_RATIO}x ratio`)

// ─────────────────────────────────────────────────────────────────────────────
// Phase 5b — Blind review of NO-BEHAVIOR write-offs.
//
// A block called NO-BEHAVIOR owes no cells, escapes the table-row floor, and is never seen by
// the citation audit. Its entire defence is a one-line reason that, until this was added, no
// stage in the run ever read. On ~2,047 blocks that is the largest single way a behaviour
// leaves the denominator, and the table-row floor makes it worse rather than better: a wide
// table is the most expensive block in the corpus to enumerate and the cheapest to write off.
//
// The reviewer reads the block and is NOT shown the reason. That is the same independence the
// challenger has, for the same reason. Judgement call #9 in the header.
// ─────────────────────────────────────────────────────────────────────────────

const noBehaviorBlocks = BLOCKS.filter(b => dispositionOf.get(b.id)?.disposition === 'NO-BEHAVIOR')
const nbTables = noBehaviorBlocks.filter(b => b.kind === 'table' && b.tableRows >= 3).sort((a, b) => b.tableRows - a.tableRows)
const nbLongest = noBehaviorBlocks.filter(b => !(b.kind === 'table' && b.tableRows >= 3)).sort((a, b) => b.lines - a.lines)
const nbPicked = [...nbTables, ...nbLongest].slice(0, NO_BEHAVIOR_REVIEW_BLOCKS)
log(`No-behaviour review: ${nbPicked.length} of ${noBehaviorBlocks.length} write-offs (${nbTables.length} of them tables with 3+ rows)`)

const nbReviews = nbPicked.length ? await parallel(nbPicked.map(b => () => guard({ b }, agent(
  `${SHARED}

=== BLIND REVIEW OF ONE BLOCK ===
Another agent read this block and decided it states NO testable compile-time behaviour, so no
test cells were written for it. You are NOT being shown the reason it gave, and you must not go
looking for it — do not read anything under ${BUNDLE}. Read the block itself and answer for
yourself.

DOCUMENT: ${b.docPath}
BLOCK: ${b.id} — ${b.kind}${b.kind === 'table' ? ` (${b.tableRows} data rows x ${b.tableCols} columns)` : ''}, lines ${b.startLine}-${b.endLine}
  ${b.text}

The question is narrow. Does this block state one or more compile-time behaviours a definition
could be checked against — something the compiler must accept, must reject, must warn about, or
must produce as a value? Or is it prose, rationale, motivation, navigation, a cross-reference,
or a description of how the compiler is BUILT rather than what it guarantees about a definition?

Writing off a description of construction is CORRECT and is the expected answer for most of
these. Do not manufacture a behaviour to look thorough. Equally, do not agree by default: if a
normative sentence is sitting in the middle of the mechanics, name it.

statesBehavior: true or false. If true, list in behaviors the specific behaviours you found, one
line each, and quote in evidence the sentence or table row that states one of them, verbatim.`,
  { label: `nb-review:${b.id}`, phase: 'Challenge', model: 'opus', effort: 'medium',
    schema: {
      type: 'object', additionalProperties: false,
      required: ['blockId', 'statesBehavior', 'behaviors', 'evidence'],
      properties: {
        blockId: { type: 'string' },
        statesBehavior: { type: 'boolean' },
        behaviors: { type: 'array', items: { type: 'string' } },
        evidence: { type: 'string', description: 'verbatim quote from the block supporting statesBehavior=true; empty string when false' },
      },
    } }
)))) : []

let nbReviewed = 0
const nbDisagreements = []
for (const entry of nbReviews) {
  if (!entry || !entry.b) { fail('a no-behaviour review thunk returned nothing at all and cannot be named'); continue }
  if (!entry.r) { fail(`no-behaviour review of ${entry.b.id} did not return${entry.threw ? ` (${entry.threw})` : ''}; that write-off is unreviewed`); continue }
  nbReviewed++
  if (entry.r.statesBehavior) {
    nbDisagreements.push({
      blockId: entry.b.id,
      docPath: entry.b.docPath,
      lines: `${entry.b.startLine}-${entry.b.endLine}`,
      writeOffReason: dispositionOf.get(entry.b.id)?.reason || '(none)',
      behaviors: entry.r.behaviors || [],
      evidence: entry.r.evidence || '',
    })
  }
}
const nbRate = nbReviewed ? nbDisagreements.length / nbReviewed : 0
log(`No-behaviour review: ${nbDisagreements.length}/${nbReviewed} write-offs disputed (${(nbRate * 100).toFixed(0)}%)`)
for (const d of nbDisagreements) {
  warn(`no-behaviour write-off disputed — ${d.blockId} (${d.docPath}:${d.lines}). Written off as: "${d.writeOffReason}". A blind reader found: ${d.behaviors.join('; ')} — evidence: ${JSON.stringify(d.evidence.slice(0, 200))}`)
}
if (nbReviewed && nbRate > NO_BEHAVIOR_MAX_DISAGREE_RATE) {
  fail(`${(nbRate * 100).toFixed(0)}% of sampled NO-BEHAVIOR write-offs were disputed by a blind reader, over the ${(NO_BEHAVIOR_MAX_DISAGREE_RATE * 100).toFixed(0)}% threshold. The write-offs are the largest way a behaviour leaves the denominator and this sample says they are not reliable; the disputed blocks and their reasons are in the report.`)
}
if (noBehaviorBlocks.length && !nbReviewed) {
  fail(`${noBehaviorBlocks.length} blocks were written off as NO-BEHAVIOR and none of them was reviewed`)
}

// Re-enumerate the shortfall blocks with both readings in view. This happens inside the run.
// It is not a note for a later pass, because a later pass is exactly what this script exists
// to avoid.
//
// Capped at CHALLENGE_REMERGE_MAX, ranked by the size of the gap. The ratio that produces this
// list is uncalibrated (judgement call #13) and each re-enumeration is an opus/high agent, so
// the cap is what stops a badly-chosen ratio from turning a 40-block sample into 40 expensive
// re-writes. Shortfalls past the cap are still reported; they are just not re-done in this run.
const remergeQueue = [...shortfalls].sort((a, b) => b.gap - a.gap).slice(0, CHALLENGE_REMERGE_MAX)
const remergeDeferred = shortfalls.length - remergeQueue.length
if (remergeDeferred > 0) {
  warn(`${remergeDeferred} blocks were over the shortfall ratio but past the re-enumeration cap of ${CHALLENGE_REMERGE_MAX}; they keep their first-pass cells and are listed in the report`)
}
const remergedBlockIds = new Set()
if (remergeQueue.length) {
  const merged = await parallel(remergeQueue.map(s => () => {
    const u = ACTIVE.find(x => x.unit === s.block.unit)
    return guard({ s }, agent(`${SHARED}

=== RE-ENUMERATION ===
Block ${s.block.id} of ${s.block.docPath}, lines ${s.block.startLine}-${s.block.endLine}, was
enumerated into ${s.primary} cells. An independent reader of the same lines found
${s.challenger} distinct behaviours:

${s.behaviors.map(x => `  - ${x}`).join('\n')}

Most easily missed, in that reader's judgement:
${s.mattersMost.map(x => `  - ${x}`).join('\n')}

Read the block yourself and produce the FULL cell set for it — the complete set, not a top-up.
Where the two readings disagree, the document decides, and if the document does not decide,
that is an escalation of kind under-determined rather than a cell.

WHAT TO ENUMERATE IN THIS DOCUMENT: ${GUIDES[u?.guide] || GUIDES.GUIDE_GENERIC}

DIAGNOSTIC NAMES: ${CODE_LIST}
WRITING A CLEAN DEFINITION: ${vocab?.cleanDefinitionNote || ''}

WRITE ${BUNDLE}/cells/${s.block.unit}/${s.block.id.replace('/', '-')}-remerge.json and return the
schema, with batchId="${s.block.id.replace('/', '-')}-remerge", unit="${s.block.unit}", and one
disposition for block ${s.block.id}. Every cell you return must carry blockId="${s.block.id}" —
if reading the block makes you want to write a cell about a neighbouring block, do not; that
block belongs to another agent and a cell attached to it here is dropped.`,
      { label: `remerge:${s.block.id}`, phase: 'Challenge', model: 'opus', effort: 'high', schema: BATCH_SCHEMA }))
  }))

  for (const entry of merged) {
    if (!entry || !entry.s) { fail('a re-enumeration thunk returned nothing at all and cannot be named'); continue }
    const s = entry.s
    const m = entry.r
    if (!m || !(m.cells || []).length) {
      warn(`re-enumeration of ${s.block.id} did not return usable cells${entry.threw ? ` (${entry.threw})` : ''}; first-pass ${s.primary} cells stand against an independent count of ${s.challenger}. The -remerge file may or may not exist on disk; the merge is told to ignore it.`)
      continue
    }

    // These cells bypassed phase 3 entirely, and they are the cells for the blocks flagged as
    // MOST likely to be under-enumerated — the last ones you want unvalidated. Run the same
    // checks over a synthetic single-block record.
    const synthetic = { u: ACTIVE.find(x => x.unit === s.block.unit), blocks: [s.block], batchId: `${s.block.id}-remerge`, result: m }
    const riss = checkBatch(synthetic)
    const rhard = riss.filter(i => i.sev === 'hard')
    if (rhard.length) {
      for (const i of rhard) fail(`remerge ${s.block.id}: ${i.msg}`)
      warn(`re-enumeration of ${s.block.id} failed ${rhard.length} hard checks and was NOT ingested; first-pass cells stand and the merge is told to ignore the -remerge file`)
      continue
    }
    for (const i of riss) {
      warnings.push(`remerge ${s.block.id}: ${i.msg}`)
      if (i.cellId) {
        if (!malformedCells.has(i.cellId)) malformedCells.set(i.cellId, [])
        malformedCells.get(i.cellId).push(i.msg)
      }
    }

    // Replace that block's cells wholesale — the re-enumeration was asked for the full set.
    // Their ids must come OUT of idSeen as well as out of CELLS. Leaving them in meant the
    // re-enumeration, reading the same text and generating the same kebab-case ids, collided
    // with its own predecessors: the old cells were gone, most of the new ones were refused as
    // duplicates, and a block could end with fewer cells than it started with, or none. The
    // phase that exists to fix under-enumeration was able to cause it.
    const remergeBatchId = `${s.block.id}-remerge`
    for (let k = CELLS.length - 1; k >= 0; k--) {
      if (CELLS[k].blockId === s.block.id) { idSeen.delete(CELLS[k].id); CELLS.splice(k, 1) }
    }
    let kept = 0, foreign = 0
    for (const c of m.cells) {
      if (c.blockId !== s.block.id) { foreign++; continue }
      if (idSeen.has(c.id)) { warn(`re-enumeration of ${s.block.id} produced id ${c.id}, which is already in use by ${idSeen.get(c.id)}; that cell is dropped`); continue }
      idSeen.set(c.id, remergeBatchId)
      CELLS.push({ ...c, unit: s.block.unit, batchId: remergeBatchId, malformed: malformedCells.get(c.id) || [] })
      kept++
    }
    if (foreign) warn(`re-enumeration of ${s.block.id} returned ${foreign} cells attached to other blocks; they were dropped because those blocks belong to other agents`)
    for (const e of m.escalations || []) escalations.push({ ...e, unit: s.block.unit, batchId: remergeBatchId })
    for (const d of m.dispositions || []) if (d.blockId === s.block.id) dispositionOf.set(d.blockId, d)
    remergedBlockIds.add(s.block.id)
    // The KEPT count, not the returned count. Logging the agent's number overstated the repair
    // whenever anything was dropped.
    log(`${s.block.id}: re-enumerated ${s.primary} -> ${kept} cells kept (${m.cells.length} returned)`)
  }
}

// Phase 5 can flip a disposition, so the corpus figures the report prints are recomputed here
// rather than left at their pre-challenge values.
recountBlocks(false)

// ─────────────────────────────────────────────────────────────────────────────
// Quality figures. Computed HERE, after the re-enumeration has changed CELLS — not earlier,
// where they would have been quietly stale. These are not gates. They are the numbers a person
// has to look at before deciding the corpus is worth measuring at all.
// ─────────────────────────────────────────────────────────────────────────────

// Diagnostic names the documents use that the compiler does not have. Not an error — a finding,
// and one of the more interesting ones, because it points at either a document naming something
// that was never built or a name that drifted.
const unknownCodes = new Map()
// No `CODE_NAMES.size &&` guard. An empty name list used to switch this whole check off, so a
// dead vocabulary agent produced zero findings — which reads in the report as "no document names
// a code the compiler does not have", the opposite of what happened. An empty list is now
// already a gate failure at the end of phase 1 and the run never gets here.
for (const c of CELLS) {
  const m = /^(?:reject|warns|accept-with-warning):([A-Za-z][A-Za-z0-9_]*)$/.exec(c.expected || '')
  if (!m || m[1] === 'any') continue
  if (!CODE_NAMES.has(m[1])) {
    if (!unknownCodes.has(m[1])) unknownCodes.set(m[1], [])
    unknownCodes.get(m[1]).push(c.id)
  }
}
for (const [name, ids] of unknownCodes) warn(`documents name diagnostic "${name}" (${ids.length} cells); no such member in ${DIAGNOSTIC_CODE_PATH}`)

// The same finding has two channels — a cell whose expected names a code the compiler lacks,
// and a names-unknown-code escalation raised by hand. They were never compared. A name in one
// and not the other means an agent noticed something a cell did not record, or the reverse.
const escalatedUnknown = new Set()
for (const e of escalations) {
  if (e.kind !== 'names-unknown-code') continue
  for (const m of `${e.claimA} ${e.claimB}`.matchAll(/\b([A-Z][A-Za-z0-9_]{4,})\b/g)) escalatedUnknown.add(m[1])
}
for (const name of unknownCodes.keys()) {
  if (escalatedUnknown.size && !escalatedUnknown.has(name)) {
    warn(`"${name}" is used as an expectation by ${unknownCodes.get(name).length} cells and is not a diagnostic the compiler has, but no names-unknown-code escalation mentions it`)
  }
}

// `conflict: true` says a cell rests on text that contradicts another part of the documents.
// The schema told the agent to also raise an escalation and nothing checked that it had, so the
// report could print "N cells rest on an unruled contradiction" with no way to find out which
// contradiction. Both directions are cheap set comparisons.
const escalatedBlocks = new Set()
for (const e of escalations) for (const b of (e.affectedBlockIds || [])) escalatedBlocks.add(b)
const contradictionBlocks = new Set()
for (const e of escalations) {
  if (e.kind !== 'document-contradiction') continue
  for (const b of (e.affectedBlockIds || [])) contradictionBlocks.add(b)
}
const orphanConflictCells = CELLS.filter(c => c.conflict && !escalatedBlocks.has(c.blockId))
if (orphanConflictCells.length) {
  fail(`${orphanConflictCells.length} cells are flagged as resting on a document contradiction, but no escalation names their block, so the contradiction they rest on cannot be found or ruled: ${orphanConflictCells.slice(0, 20).map(c => c.id).join(', ')}${orphanConflictCells.length > 20 ? ` (+${orphanConflictCells.length - 20} more)` : ''}`)
}
const unflaggedContradictionBlocks = [...contradictionBlocks].filter(id => {
  const cs = CELLS.filter(c => c.blockId === id)
  return cs.length > 0 && cs.every(c => !c.conflict)
})
if (unflaggedContradictionBlocks.length) {
  warn(`${unflaggedContradictionBlocks.length} blocks are named by a document-contradiction escalation but none of their cells is flagged conflict:true — either the cells do not in fact depend on the contradicted text, or the flag was missed: ${unflaggedContradictionBlocks.join(', ')}`)
}

const perUnit = {}
for (const u of ACTIVE) {
  const cells = CELLS.filter(c => c.unit === u.unit)
  const blocks = blocksByUnit[u.unit] || []
  perUnit[u.unit] = {
    path: u.path,
    blocks: blocks.length,
    behavior: blocks.filter(b => dispositionOf.get(b.id)?.disposition === 'BEHAVIOR').length,
    noBehavior: blocks.filter(b => dispositionOf.get(b.id)?.disposition === 'NO-BEHAVIOR').length,
    unaccounted: blocks.filter(b => !dispositionOf.get(b.id)).length,
    cells: cells.length,
    rejectAny: cells.filter(c => c.expected === 'reject:any').length,
    named: cells.filter(c => /^(reject|warns|accept-with-warning):(?!any$)/.test(c.expected || '')).length,
    accept: cells.filter(c => c.expected === 'accept').length,
    valueCells: cells.filter(c => (c.expected || '').startsWith('value:')).length,
    conflictCells: cells.filter(c => c.conflict).length,
    malformedCells: cells.filter(c => (c.malformed || []).length).length,
  }
  if (perUnit[u.unit].cells === 0 && perUnit[u.unit].behavior > 0) fail(`${u.unit}: ${perUnit[u.unit].behavior} behaviour blocks and zero cells`)
}

// The per-unit columns and the corpus totals are computed at different points over the same
// data. If they ever disagree the report would print both and rely on a reader noticing that a
// 20-row column sums differently from a headline four sections up — which is the exact shape of
// the 2026-07-14 "temporal 210 != 192" failure. One line closes it.
// Matrix tables. The row floor asks for one cell per data row, but a 3-row by 5-column matrix
// states about 3x4 things, not 3 — and matrix tables are simultaneously the densest source of
// cells and the place the floor is weakest. This is reported and counted, not gated and not fed
// into the repair round: `tableCols` is a transcribed number, the "half of rows x (cols-1)"
// shape is a guess at what a healthy matrix produces, and a check built on both of those driving
// a repair round would cost as much as the row floor and mean less. It is a list for a person.
const thinMatrixTables = []
for (const b of BLOCKS) {
  if (b.kind !== 'table' || b.tableCols < 3 || b.tableRows < 2) continue
  if (dispositionOf.get(b.id)?.disposition !== 'BEHAVIOR') continue
  const n = CELLS.filter(c => c.blockId === b.id).length
  const looseFloor = Math.ceil(b.tableRows * (b.tableCols - 1) * 0.5)
  if (n < looseFloor) thinMatrixTables.push({ blockId: b.id, at: `${b.docPath}:${b.startLine}-${b.endLine}`, rows: b.tableRows, cols: b.tableCols, cells: n, looseFloor })
}
if (thinMatrixTables.length) {
  warn(`${thinMatrixTables.length} matrix tables (3+ columns) carry roughly one cell per row rather than one per matrix cell. They clear the row floor and may still be under-enumerated; they are listed in the report.`)
}

const perUnitBehaviorSum = ACTIVE.reduce((a, u) => a + perUnit[u.unit].behavior, 0)
const perUnitNoneSum = ACTIVE.reduce((a, u) => a + perUnit[u.unit].noBehavior, 0)
const perUnitCellSum = ACTIVE.reduce((a, u) => a + perUnit[u.unit].cells, 0)
if (perUnitBehaviorSum !== blocksBehavior || perUnitNoneSum !== blocksNone || perUnitCellSum !== CELLS.length) {
  fail(`per-unit table and corpus totals disagree: behaviour ${perUnitBehaviorSum} vs ${blocksBehavior}, no-behaviour ${perUnitNoneSum} vs ${blocksNone}, cells ${perUnitCellSum} vs ${CELLS.length}`)
}

// ─────────────────────────────────────────────────────────────────────────────
// Phase 6 — Citation audit.
//
// Sampled, mechanical, and gated. Every cell claims a verbatim quote at a cited line; this
// checks that the quote is actually there and that it says what the cell says it says. It is
// the check on the one thing no schema can enforce — that the expectation came from the text
// rather than from the agent.
// ─────────────────────────────────────────────────────────────────────────────

phase('Audit')

function sample(arr, n) {
  if (arr.length <= n) return arr
  const step = arr.length / n
  const out = []
  for (let i = 0; i < n; i++) out.push(arr[Math.floor(i * step)])
  return out
}

const auditSent = new Map()   // unit -> the exact ids this script handed the auditor
const audits = await parallel(ACTIVE.map(u => () => {
  const picked = sample(CELLS.filter(c => c.unit === u.unit), AUDIT_SAMPLE_PER_UNIT)
  auditSent.set(u.unit, picked.map(c => c.id))
  if (!picked.length) return Promise.resolve({ u, sampled: 0, r: { unit: u.unit, verdicts: [], notes: 'no cells' }, threw: '' })
  return guard({ u, sampled: picked.length }, agent(`Citation audit for ${u.path}. Read the document.

For each cell below, answer three questions and nothing else:
  1. quotePresent — does that exact text appear in ${u.path} within the cited lines (allow a few
     lines of drift and allow whitespace differences, but not paraphrase)? Use Grep or Read on
     the cited range.
  2. basisSupported — reading that text and only that text, does it actually support the stated
     expectation? You are not judging whether the expectation is good policy. Only: does the
     quoted sentence say this?
  3. fragmentExercises — does the DSL fragment attached to the cell actually exercise the
     behaviour the cell claims to check? A cell can quote the right sentence about cross-currency
     money division and carry a fragment that divides money by a number. Nothing else in this run
     looks at that, so this is the only place it is checked at all.

RETURN A VERDICT FOR EVERY CELL ID BELOW — all ${picked.length} of them, passes included. The
set of ids you return is compared against the set sent, and a missing id fails the run. Saying
nothing about a cell is not the same as passing it, and the difference between "I checked 25 and
they were fine" and "I checked 6" has to be visible from the return.

${NO_COMPILE}

${picked.map(c => `id: ${c.id}\n  cite: ${c.specCite}\n  quote: ${JSON.stringify(c.quote)}\n  expected: ${c.expected}\n  basis: ${c.expectedBasis}\n  desc: ${c.desc}\n  fragment: ${JSON.stringify(c.fragment)}`).join('\n\n')}`,
    { label: `audit:${u.unit}`, phase: 'Audit', model: 'opus', effort: 'high',
      schema: {
        type: 'object', additionalProperties: false,
        required: ['unit', 'verdicts', 'notes'],
        properties: {
          unit: { type: 'string' },
          verdicts: {
            type: 'array',
            items: {
              type: 'object', additionalProperties: false,
              required: ['id', 'quotePresent', 'basisSupported', 'fragmentExercises', 'note'],
              properties: {
                id: { type: 'string' },
                quotePresent: { type: 'boolean' },
                basisSupported: { type: 'boolean' },
                fragmentExercises: { type: 'boolean' },
                note: { type: 'string', description: 'required non-empty when any of the three is false; empty string otherwise' },
              },
            },
          },
          notes: { type: 'string' },
        },
      } }))
}))

// Per-cell verdicts compared as SETS against what was sent. The previous version told the
// auditor to "say nothing about the ones that pass", which made a return of zero failures
// indistinguishable from a return where 19 of the 25 were never opened — and the only guard on
// that was a self-reported `checked` integer the agent had to volunteer against itself.
let auditChecked = 0, auditFailed = 0
const auditFailIds = []
const auditFragmentFails = []
const auditUnitRows = []
for (const entry of audits) {
  if (!entry || !entry.u) { fail('a citation audit thunk returned nothing; one unit is unaudited and cannot be named'); continue }
  const unitName = entry.u.unit
  const a = entry.r
  const sentIds = auditSent.get(unitName) || []
  if (!a) { fail(`${unitName}: citation audit did not return${entry.threw ? ` (${entry.threw})` : ''} — that unit's cells are unverified`); continue }
  if (!sentIds.length) { auditUnitRows.push({ unit: unitName, sent: 0, failed: 0, rate: 0 }); continue }

  const returned = new Set((a.verdicts || []).map(v => v.id))
  const missing = sentIds.filter(id => !returned.has(id))
  const extra = [...returned].filter(id => !sentIds.includes(id))
  if (missing.length) {
    fail(`${unitName}: citation audit returned no verdict for ${missing.length} of the ${sentIds.length} cells it was sent, so its failure rate is computed over a sample it chose rather than the one it was given: ${missing.slice(0, 10).join(', ')}${missing.length > 10 ? ' …' : ''}`)
  }
  if (extra.length) warn(`${unitName}: citation audit returned verdicts for ${extra.length} ids that were not sent to it: ${extra.slice(0, 10).join(', ')}`)

  const bad = new Set()
  for (const v of a.verdicts || []) {
    if (!sentIds.includes(v.id)) continue
    if (!v.quotePresent || !v.basisSupported) bad.add(v.id)
    if (!v.fragmentExercises) auditFragmentFails.push(`${v.id}: ${v.note || '(no note)'}`)
  }
  auditChecked += sentIds.length
  auditFailed += bad.size
  for (const id of bad) auditFailIds.push(id)

  // Per-unit gate as well as corpus-wide. 20 units x 25 cells at a 4% corpus threshold permits
  // about 20 failures, so one unit could fail 20 of its 25 while the run passes.
  const unitRate = bad.size / sentIds.length
  auditUnitRows.push({ unit: unitName, sent: sentIds.length, failed: bad.size, rate: unitRate })
  if (sentIds.length >= AUDIT_UNIT_GATE_MIN_SAMPLE && unitRate > AUDIT_MAX_UNIT_FAIL_RATE) {
    fail(`${unitName}: citation audit failed ${bad.size} of ${sentIds.length} sampled cells (${(unitRate * 100).toFixed(0)}%), over the ${(AUDIT_MAX_UNIT_FAIL_RATE * 100).toFixed(0)}% per-unit threshold — that unit's cells are not trustworthy even if the corpus rate passes`)
  }
}

// The cells that failed stay in the corpus; the measurement run must be able to tell them apart
// from the ones that passed, so the merge is told to stamp them.
for (const c of CELLS) if (auditFailIds.includes(c.id)) c.auditFailed = true

const auditRate = auditChecked ? auditFailed / auditChecked : 0
log(`Audit: ${auditFailed}/${auditChecked} sampled cells failed (${(auditRate * 100).toFixed(1)}%), ${auditFragmentFails.length} fragments do not exercise what their cell claims`)
if (auditFragmentFails.length) {
  warn(`citation audit: ${auditFragmentFails.length} sampled cells carry a fragment that does not exercise the behaviour the cell claims to check — ${auditFragmentFails.slice(0, 20).join(' | ')}`)
}
if (auditChecked === 0) fail('citation audit checked nothing')
else if (auditRate > AUDIT_MAX_FAIL_RATE) {
  fail(`citation audit failure rate ${(auditRate * 100).toFixed(1)}% exceeds ${(AUDIT_MAX_FAIL_RATE * 100).toFixed(0)}% — the corpus is not trustworthy and the failing cells are: ${auditFailIds.join(', ')}`)
} else if (auditFailed) {
  warn(`citation audit: ${auditFailed} cells failed and are listed in the report: ${auditFailIds.join(', ')}`)
}

// ─────────────────────────────────────────────────────────────────────────────
// Phase 7 — Reverse check.
//
// Keyed by code, not by document. It CANNOT define a denominator — the documents do that — and
// nothing it finds becomes a cell. It exists because it is the only thing that catches a
// behaviour the compiler has that no document mentions, and in 2026-07-13 it was the pass that
// found the obligation-creation holes. Its findings are document omissions, for a person.
// ─────────────────────────────────────────────────────────────────────────────

const reverseFindings = []
if (REVERSE_CHECK) {
  phase('Reverse')
  const docList = ACTIVE.map(u => u.path).join(', ')
  const angles = [
    { key: 'diagnostics', floor: CODE_NAMES.size, brief: `Every member of ${DIAGNOSTIC_CODE_PATH} — all ${CODE_NAMES.size} of them. For each, is there any canonical document that states the condition under which it fires? Report the ones no document describes.` },
    { key: 'catalog', brief: 'Every catalog under src/Precept/Language/ — keywords, types, operators, modifiers, constructs, actions. For each member, is there a canonical document that states its behaviour? Report members no document mentions. Remember the standing position: a catalog member the documents do not describe is a document gap OR a catalog that has drifted, and you cannot tell which — report it as the question it is.' },
    { key: 'obligations', brief: 'Every faultable expression position and every fault code the compiler knows about (start from the proof engine and the fault code enumeration in src/). For each, does a canonical document state what must be proven there? Report positions no document covers.' },
  ]
  const rev = await parallel(angles.map(a => () => guard({ a }, agent(
    `Reverse check — code-keyed, one direction only. Do not compile anything and do not run the test suite.

${a.brief}

The canonical documents in scope are: ${docList}

WHAT THIS IS FOR. A separate pass has enumerated what those documents REQUIRE. You are doing the
opposite: looking for things the compiler has that the documents never mention. Every finding is
a DOCUMENT OMISSION to put in front of the owner. None of it is a test cell and none of it
changes what the documents require. Do not propose language surface, do not decide what the
behaviour should be, and do not treat the code as evidence of what is correct — where code and
canonical document disagree, the document wins and the code is the bug.

itemsChecked is how many items you actually opened and decided about — not how many exist and
not how many you sampled. It is compared against the real size of your scope, and a number well
under that fails the run rather than being reported as coverage you did not have.

Write ${BUNDLE}/doc-omissions-${a.key}.md and return the schema. Return every finding in the
structured array as well as writing it to the file: the file is for a person to read, the array
is what reaches the report, and a finding that exists only in the file is a finding that can
disappear without anyone noticing.`,
    { label: `reverse:${a.key}`, phase: 'Reverse', model: 'opus', effort: 'high',
      schema: {
        type: 'object', additionalProperties: false,
        required: ['angle', 'itemsChecked', 'findings', 'couldNotDetermine', 'wroteFile'],
        properties: {
          angle: { type: 'string' },
          itemsChecked: { type: 'integer' },
          findings: {
            type: 'array',
            items: {
              type: 'object', additionalProperties: false,
              required: ['item', 'evidence', 'question'],
              properties: {
                item: { type: 'string' },
                evidence: { type: 'string', description: 'file:line for the code side' },
                question: { type: 'string', description: 'the document-side question this raises' },
              },
            },
          },
          couldNotDetermine: { type: 'array', items: { type: 'string' } },
          wroteFile: { type: 'boolean' },
        },
      } }
  ))))
  for (const entry of rev) {
    if (!entry || !entry.a) { fail('a reverse check thunk returned nothing at all and cannot be named'); continue }
    const { a, r } = entry
    if (!r) { fail(`reverse check "${a.key}" did not return${entry.threw ? ` (${entry.threw})` : ''}; that angle is unrun and its zero findings would otherwise read as a clean result`); continue }

    // The findings themselves used to be dropped here and only the COUNT was kept — so the
    // report printed "40 findings over 312 items" and the 40 findings existed only inside a file
    // whose existence was an unchecked boolean. That is the same loss the escalation machinery
    // exists to prevent, in the one phase with the best record for finding real holes.
    reverseFindings.push({
      angle: a.key,
      itemsChecked: r.itemsChecked,
      count: (r.findings || []).length,
      findings: r.findings || [],
      couldNotDetermine: r.couldNotDetermine || [],
      wroteFile: !!r.wroteFile,
    })
    if (!r.wroteFile) fail(`reverse check "${a.key}" reports it did not write ${BUNDLE}/doc-omissions-${a.key}.md`)
    // itemsChecked is self-reported with no floor: an agent that finds nothing returns 0 items
    // and 0 findings, and the log line reads the same as a thorough angle that found nothing.
    if (a.floor && r.itemsChecked < a.floor) {
      fail(`reverse check "${a.key}" reports ${r.itemsChecked} items checked; there are ${a.floor} to check. It covered ${Math.round((r.itemsChecked / a.floor) * 100)}% of its scope, so its findings are not a statement about the rest.`)
    }
    if (!a.floor && r.itemsChecked === 0) {
      fail(`reverse check "${a.key}" reports 0 items checked and 0 findings — that is a phase that did not run, not a clean result`)
    }
    if ((r.couldNotDetermine || []).length) {
      warn(`reverse check "${a.key}" could not determine ${r.couldNotDetermine.length} items: ${r.couldNotDetermine.slice(0, 15).join('; ')}${r.couldNotDetermine.length > 15 ? ' …' : ''}`)
    }
    log(`reverse:${a.key}: ${(r.findings || []).length} document omissions over ${r.itemsChecked} items checked`)
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Phase 8 — Publish.
//
// Five clerical jobs, not two. The merge concatenates and its counts are checked against this
// script's. The inventories are written one agent per unit from the block table this script
// hands over — the dispositions and no-behaviour reasons live only in memory otherwise, so the
// old arrangement asked one agent to reconstruct a block inventory from cells files that provably
// could not contain it. `escalations.md` is written by one agent and then re-read by a second
// that must find every claim quote in it verbatim, because "wroteEscalations: true" is the
// writer auditing its own omissions. The report agent is given the numbers and told to transcribe
// them; it computes nothing, reconciles nothing, and re-judges nothing. That instruction is not
// politeness — the 2026-07-14 report was written by an agent that re-derived the numbers from
// markdown, and it published "100% of blocks are accounted in every unit" over five detected
// problems.
//
// Splitting also bounds each turn. One agent writing REPORT.md, escalations.md and 20 inventory
// files, with `problems` and every escalation interpolated in full, is the single point whose
// failure leaves no record of anything — and it is the one most likely to hit an output ceiling.
// ─────────────────────────────────────────────────────────────────────────────

phase('Publish')

const unitRows = ACTIVE.map(u => {
  const p = perUnit[u.unit]
  return `${u.unit} | ${p.path} | blocks ${p.blocks} (behaviour ${p.behavior}, no-behaviour ${p.noBehavior}, unaccounted ${p.unaccounted}) | cells ${p.cells} | named ${p.named} | accept ${p.accept} | reject:any ${p.rejectAny} | value ${p.valueCells} | malformed ${p.malformedCells} | resting on an unruled contradiction ${p.conflictCells}`
}).join('\n')

// Escalations are collected, never re-judged, and never deduplicated — the temporal :143/:1182
// contradiction will be raised by every batch that touches either line. Collecting every copy is
// correct; making a person find the duplicates by hand is not. Grouping is presentation only:
// every copy is still published, and the group header says how many there are.
const escalationGroups = new Map()
for (const e of escalations) {
  const key = `${e.kind}|${e.docPath}|${(e.lines || []).join(',')}`
  if (!escalationGroups.has(key)) escalationGroups.set(key, [])
  escalationGroups.get(key).push(e)
}
const escalationText = escalations.length
  ? [...escalationGroups.values()].map((group, i) => {
      const e = group[0]
      const raisedBy = group.map(g => g.batchId).join(', ')
      return `### ${i + 1}. ${e.kind} — ${e.docPath}:${(e.lines || []).join(',')}  (${e.unit})
- raised ${group.length} time${group.length === 1 ? '' : 's'}, by: ${raisedBy}
- claimA: ${JSON.stringify(e.claimA)}
- claimB: ${JSON.stringify(e.claimB)}
- why it blocks: ${e.whyItBlocks}
- blocks affected: ${[...new Set(group.flatMap(g => g.affectedBlockIds || []))].join(', ')}`
    }).join('\n\n')
  : '(none returned by any agent)'

// Which file on disk this script accepted for each batch, and which -remerge files it ingested.
// Without this the merge reads whatever an agent last wrote, including repairs this script
// rejected and re-enumerations that failed their checks — and the divergence surfaces as an
// unexplained count mismatch in the last phase with no repair path.
const repairFileRows = [...repairAccepted.entries()].map(([batchId, which]) => {
  const rec = batchResults.find(r => r && r.batchId === batchId)
  const dir = `${BUNDLE}/cells/${rec ? rec.u.unit : '?'}`
  return which === 'repair'
    ? `USE ${dir}/${batchId}.repair.json   AND IGNORE ${dir}/${batchId}.json`
    : `USE ${dir}/${batchId}.json          AND IGNORE ${dir}/${batchId}.repair.json (this script rejected that repair)`
})
const remergeUseRows = [...remergedBlockIds].map(id => `USE ${BUNDLE}/cells/${id.split('/')[0]}/${id.replace('/', '-')}-remerge.json — it replaces every cell for block ${id}`)
const remergeIgnoreRows = shortfalls
  .filter(s => !remergedBlockIds.has(s.block.id))
  .map(s => `IGNORE ${BUNDLE}/cells/${s.block.unit}/${s.block.id.replace('/', '-')}-remerge.json if it exists — this script did not accept it, so block ${s.block.id} keeps its original cells`)

const supersededExpected = shortfalls
  .filter(s => remergedBlockIds.has(s.block.id))
  .reduce((a, s) => a + s.primary, 0)

const merge = await agent(`Mechanical merge. No judgement, nothing added, nothing tidied.

Read every JSON file under ${BUNDLE}/cells/ and concatenate the arrays into
${BUNDLE}/assembled/all-cells.json — one flat JSON array, in filename order. Do not edit any
record except as instructed below. Do not deduplicate. Do not fix anything you think looks
wrong; report it in anomalies instead — that field is read and reported, so use it.

This is roughly ${BATCHES.length} files and, at ${CELLS.length} records, several megabytes of
JSON. Do NOT try to do it by reading files into your context and retyping them. Write a small
script, run it, and report what it printed. The same goes for the counts below: count them with
the script, do not eyeball them.

WHICH FILE WINS. Some batches were repaired and some blocks were re-enumerated, and this script
decided which version it accepted. Follow this list exactly — it is not advisory:

${repairFileRows.length ? repairFileRows.join('\n') : '(no batch was repaired)'}

${remergeUseRows.length ? remergeUseRows.join('\n') : '(no block was re-enumerated)'}
${remergeIgnoreRows.length ? '\n' + remergeIgnoreRows.join('\n') : ''}

Where a -remerge file is in the USE list, drop every cell for that block from the batch file it
came from and keep only the -remerge cells. Report how many you dropped as superseded.

STAMP THESE RECORDS. Add "auditFailed": true to any record whose id is in this list. They failed
the citation audit and are staying in the corpus, and the measurement run has to be able to tell
them apart from the ones that passed:
${auditFailIds.length ? bounded(auditFailIds, id => `  ${id}`) : '  (none)'}

Add "malformed": true to any record whose id is in this list. They failed a structural check on
the cell itself:
${malformedCells.size ? bounded([...malformedCells.keys()], id => `  ${id}`) : '  (none)'}

Return: the total written, the count per unit (by the "unit/" prefix on each cell id), any
duplicate ids you saw, the count you dropped as superseded, how many records you stamped, and
anything at all that looked wrong.`, {
  label: 'merge', phase: 'Publish', model: 'sonnet', effort: 'medium',
  schema: {
    type: 'object', additionalProperties: false,
    required: ['total', 'perUnit', 'duplicateIds', 'supersededDropped', 'recordsStamped', 'wroteFile', 'anomalies'],
    properties: {
      total: { type: 'integer' },
      perUnit: { type: 'array', items: { type: 'object', additionalProperties: false, required: ['unit', 'count'], properties: { unit: { type: 'string' }, count: { type: 'integer' } } } },
      duplicateIds: { type: 'array', items: { type: 'string' } },
      supersededDropped: { type: 'integer' },
      recordsStamped: { type: 'integer' },
      wroteFile: { type: 'boolean' },
      anomalies: { type: 'string' },
    },
  },
})

// The merge is checked against this script's own arrays. If the file on disk and the records
// this script validated disagree, that is a gate failure — not something to notice later.
//
// What this compares is CARDINALITY, not content: totals, per-unit counts, duplicate ids and the
// superseded bookkeeping. A batch that returned a good record and wrote a hollow one to disk is
// not detectable from here, and the file on disk is what a measurement run consumes. That limit
// is admitted as judgement call #10 rather than papered over.
if (!merge) fail('merge agent did not return; assembled/all-cells.json is unverified')
else {
  if (!merge.wroteFile) fail('merge agent reports it did not write assembled/all-cells.json')
  if (merge.total !== CELLS.length) fail(`merged file holds ${merge.total} cells; this script validated ${CELLS.length}`)
  if ((merge.duplicateIds || []).length) fail(`merged file has duplicate ids: ${merge.duplicateIds.join(', ')}`)
  // Only the units the agent chose to report used to be reconciled, so a unit simply left out of
  // that array was never compared to anything.
  const reported = new Map((merge.perUnit || []).map(r => [r.unit, r.count]))
  for (const u of ACTIVE) {
    const mine = CELLS.filter(c => c.unit === u.unit).length
    if (!reported.has(u.unit)) {
      if (mine > 0) fail(`merge did not report a count for ${u.unit}, which has ${mine} validated cells — that unit was never reconciled against the file on disk`)
    } else if (reported.get(u.unit) !== mine) {
      fail(`merged file has ${reported.get(u.unit)} cells for ${u.unit}; this script validated ${mine}`)
    }
  }
  for (const unit of reported.keys()) {
    if (!ACTIVE.some(u => u.unit === unit)) warn(`merge reports ${reported.get(unit)} cells for "${unit}", which is not an active unit`)
  }
  // This script knows exactly how many cells it spliced out for re-enumerated blocks.
  if (merge.supersededDropped !== supersededExpected) {
    fail(`merge dropped ${merge.supersededDropped} cells as superseded by a re-enumeration; this script spliced out ${supersededExpected}. The two are reading different files as authoritative.`)
  }
  const stampExpected = new Set([...auditFailIds, ...malformedCells.keys()]).size
  if ((merge.recordsStamped || 0) !== stampExpected) {
    warn(`merge stamped ${merge.recordsStamped} records; ${stampExpected} were listed for stamping. The measurement run may not be able to tell a failed cell from a passed one.`)
  }
  // The merge's one channel for reporting a problem used to be a field nothing read.
  if ((merge.anomalies || '').trim()) warn(`merge reported anomalies: ${merge.anomalies}`)
}

// ── The inventories. One agent per unit, and the rows are handed over rather than reconstructed.
//
// The dispositions and their NO-BEHAVIOR reasons exist only in this script's memory: the batch
// agents write CELLS to disk and nothing else. The old prompt told one agent to build the block
// inventory "from the cells files and the dispositions in them", which cannot be done — every
// NO-BEHAVIOR block, likely most of 2,047, appears in no file anywhere. Either the run failed on
// a thousand missing ids or the agent filled the gap plausibly. The reasons are the evidence that
// a block was considered and rejected, and without this they are discarded when the process exits.
const cellIdsByBlock = new Map()
for (const c of CELLS) {
  if (!cellIdsByBlock.has(c.blockId)) cellIdsByBlock.set(c.blockId, [])
  cellIdsByBlock.get(c.blockId).push(c.id)
}
function inventoryRows(unit) {
  return (blocksByUnit[unit] || []).map(b => {
    const d = dispositionOf.get(b.id)
    const disp = d ? d.disposition : 'UNACCOUNTED'
    const ids = cellIdsByBlock.get(b.id) || []
    const tail = disp === 'BEHAVIOR'
      ? `cells (${ids.length}): ${ids.join(' ')}`
      : disp === 'NO-BEHAVIOR'
        ? `no-behaviour reason: ${d.reason}${(d.nonTestableRows || 0) ? ` | non-testable rows ${d.nonTestableRows}: ${d.nonTestableReason}` : ''}`
        : 'NO DISPOSITION — this block was never accounted for'
    const kind = `${b.kind}${b.kind === 'table' ? ` ${b.tableRows}x${b.tableCols}` : ''}`
    return `${b.id} | ${kind} | ${b.docPath}:${b.startLine}-${b.endLine} (${b.lines} lines) | ${disp} | ${JSON.stringify(b.text)} | ${tail}`
  })
}

const inventories = await parallel(ACTIVE.map(u => () => guard({ u }, agent(
  `Clerical transcription. No judgement, nothing added, nothing summarised, nothing omitted.

Write ${BUNDLE}/inventory/${u.unit}.md — the block inventory for ${u.path}.

Below are ${(blocksByUnit[u.unit] || []).length} rows, one per block, already in order. Put every
one of them in the file as a row of a markdown table with these columns: block id, kind, line
range, disposition, heading or anchor text, and either the cell ids or the no-behaviour reason.
Do not merge rows. Do not shorten a reason. Do not drop a row because it looks repetitive — the
no-behaviour reasons are the evidence that each of those blocks was read and rejected, and this
file is the only place they exist.

Head the file with: the unit name, the document path, the block count, and a one-line note that
the block boundaries come from a fixed command over the document text and the dispositions come
from the agents that read each block.

Return how many rows you wrote and the ids of any you could not write.

${inventoryRows(u.unit).join('\n')}`,
  { label: `inventory:${u.unit}`, phase: 'Publish', model: 'sonnet', effort: 'medium',
    schema: {
      type: 'object', additionalProperties: false,
      required: ['unit', 'rowsWritten', 'rowsSkipped', 'wroteFile'],
      properties: {
        unit: { type: 'string' },
        rowsWritten: { type: 'integer' },
        rowsSkipped: { type: 'array', items: { type: 'string' } },
        wroteFile: { type: 'boolean' },
      },
    } }
))))

let inventoryFilesWritten = 0
for (const entry of inventories) {
  if (!entry || !entry.u) { fail('an inventory thunk returned nothing at all and cannot be named'); continue }
  const expected = (blocksByUnit[entry.u.unit] || []).length
  if (!entry.r) { fail(`inventory for ${entry.u.unit} did not return${entry.threw ? ` (${entry.threw})` : ''}; ${expected} blocks and their no-behaviour reasons are unpublished`); continue }
  if (!entry.r.wroteFile) { fail(`inventory for ${entry.u.unit} reports it did not write ${BUNDLE}/inventory/${entry.u.unit}.md`); continue }
  inventoryFilesWritten++
  if (entry.r.rowsWritten !== expected) {
    fail(`inventory for ${entry.u.unit} wrote ${entry.r.rowsWritten} rows for ${expected} blocks; skipped: ${(entry.r.rowsSkipped || []).join(', ') || '(unnamed)'}`)
  }
}
log(`Publish: ${inventoryFilesWritten}/${ACTIVE.length} inventory files`)

// ── Escalations: written by one agent, then read back by a different one.
//
// The collection side of this is solved — required-field objects, an empty array as the only way
// to say nothing, gathered by this script from structured returns. The PUBLICATION side was a
// boolean the writer set about its own work, which is where the 2026-07-14 escalation actually
// died. A second cheap agent now has to find each claim quote in the file, verbatim.
const escalationClaims = escalations.map((e, i) => ({ i, claim: (e.claimA || '').trim() })).filter(x => x.claim.length >= 12)

const escalationWrite = escalations.length ? await agent(
  `Write ${BUNDLE}/escalations.md. Clerical transcription, verbatim, nothing summarised.

There are ${escalations.length} escalations below, grouped by document and line so the duplicates
are visible — the same contradiction gets raised by every batch that touches either side of it,
and every copy is kept on purpose. Put ALL of them in the file, each with its kind, its document
and lines, both claim quotes EXACTLY as given including punctuation, why it blocks, and the
blocks affected.

Do not resolve one. Do not rank them. Do not condense two into one because they look similar. An
escalation is not resolved by being summarised, and a second agent is going to read this file and
search it for each claim quote character by character.

Return the number you wrote.

${escalationText}`,
  { label: 'escalations', phase: 'Publish', model: 'sonnet', effort: 'medium',
    schema: {
      type: 'object', additionalProperties: false,
      required: ['written', 'wroteFile', 'notes'],
      properties: { written: { type: 'integer' }, wroteFile: { type: 'boolean' }, notes: { type: 'string' } },
    } }
) : null

if (escalations.length) {
  if (!escalationWrite) fail(`escalations.md was not written and there are ${escalations.length} escalations to publish`)
  else if (!escalationWrite.wroteFile) fail('escalations agent reports it did not write escalations.md')

  const verify = escalationWrite && escalationWrite.wroteFile ? await agent(
    `Verification, not writing. Read ${BUNDLE}/escalations.md.

Below are ${escalationClaims.length} quoted claims that must each appear in that file verbatim.
For each, search the file for that exact string. Report the index numbers of the ones you CANNOT
find as an exact substring. Whitespace and line-wrapping differences are fine; a paraphrase, a
truncation, an ellipsis or a condensed version is NOT found.

Do not fix the file. Do not add anything to it. Report what is missing.

${escalationClaims.map(x => `[${x.i}] ${JSON.stringify(x.claim)}`).join('\n')}`,
    { label: 'escalations-verify', phase: 'Publish', model: 'sonnet', effort: 'medium',
      schema: {
        type: 'object', additionalProperties: false,
        required: ['checked', 'missingIndexes', 'notes'],
        properties: {
          checked: { type: 'integer' },
          missingIndexes: { type: 'array', items: { type: 'integer' } },
          notes: { type: 'string' },
        },
      } }
  ) : null

  if (escalationWrite && escalationWrite.wroteFile) {
    if (!verify) fail('escalations.md was written but could not be verified; its completeness is the writer\'s own word')
    else {
      if (verify.checked !== escalationClaims.length) {
        fail(`escalations verification checked ${verify.checked} of ${escalationClaims.length} claim quotes, so its clean result covers a sample it chose`)
      }
      if ((verify.missingIndexes || []).length) {
        fail(`${verify.missingIndexes.length} escalation claim quotes are not in escalations.md verbatim — they were summarised, truncated or dropped. Missing: ${verify.missingIndexes.map(i => `#${i} (${escalations[i]?.kind} ${escalations[i]?.docPath}:${(escalations[i]?.lines || []).join(',')})`).join('; ')}`)
      }
    }
  }
}

// ── The gate verdict, computed HERE.
//
// It used to be snapshotted before the merge ran, so merge failures reached the failure LIST
// interpolated below but not the headline — and the report agent was handed "GATE: PASSED" above
// a list of hard failures and asked to transcribe faithfully. That is the 2026-07-14 failure one
// step earlier in the chain: the contradiction was not merely permitted, it was supplied.
const gateFailed = problems.length > 0

const reportPrompt = `Write ${BUNDLE}/REPORT.md.

YOUR JOB IS TRANSCRIPTION. Every number below was computed by the workflow script from the
agents' own structured returns. Do not recompute any of them. Do not reconcile any of them. Do
not explain a mismatch away, do not add a footnote that makes an inconsistent pair consistent,
and do not soften a hedge. If two numbers here disagree, print both and say they disagree. On
2026-07-14 a report at this exact position took five detected problems, resolved two of them in
footnotes, dropped three, and published "100% of blocks are accounted in every unit". That is
the failure this instruction exists to prevent.

=== RUN STATE ===
GATE: ${gateFailed ? `FAILED — ${problems.length} hard failures. This corpus is NOT ready to measure.` : 'PASSED'}

HARD FAILURES (${problems.length}):
${bounded(problems, p => `  - ${p}`)}

WARNINGS (${warnings.length}):
${bounded(warnings, p => `  - ${p}`)}

=== TOTALS ===
documents walked: ${ACTIVE.length}
blocks: ${BLOCKS.length}
  Block boundaries come from one fixed shell command over each document (headings, table starts,
  fenced blocks), transcribed by a census agent and cross-checked against a separately-derived
  count, a pinned baseline, and per-anchor validation. They are not an agent's judgement about
  how many blocks a document has; they ARE an agent's transcription of a deterministic command.
  behaviour-bearing: ${blocksBehavior}
  no behaviour:      ${blocksNone}
  unaccounted:       ${blocksMissing}
oversized blocks (over ${BIG_BLOCK_LINES} lines, one disposition covering all of them): ${bigBlocks.length}
${bigBlocks.length ? bigBlocks.map(b => `    ${b.id} — ${b.lines} lines, ${b.kind}, ${b.docPath}:${b.startLine}-${b.endLine}, ${(cellIdsByBlock.get(b.id) || []).length} cells`).join('\n') : '    (none)'}
cells: ${CELLS.length}
  of which malformed (failed a structural check, kept and marked): ${malformedCells.size}
  of which failed the citation audit (kept and marked): ${auditFailIds.length}
escalations: ${escalations.length} raised, ${escalationGroups.size} distinct after grouping by document and lines
citation audit: ${auditFailed}/${auditChecked} sampled cells failed (${(auditRate * 100).toFixed(1)}%), corpus threshold ${(AUDIT_MAX_FAIL_RATE * 100).toFixed(0)}%, per-unit threshold ${(AUDIT_MAX_UNIT_FAIL_RATE * 100).toFixed(0)}%
  per unit: ${auditUnitRows.map(r => `${r.unit} ${r.failed}/${r.sent}`).join(', ')}
  sampled cells whose fragment does not exercise what the cell claims: ${auditFragmentFails.length}
independent re-enumeration (phase 5):
  eligible behaviour blocks: ${eligible.length} of ${behaviorBlocks.length} (${challengeExcludedShort} excluded as under 8 lines)
  challenged: ${candidates.length}, selected as the lowest cells-per-line — note this ranking
    systematically picks long thin prose blocks and cannot pick a short block that owes a lot
  challengers that returned: ${challengeReturned}
  over the ${CHALLENGE_SHORTFALL_RATIO}x ratio: ${shortfalls.length}
  re-enumerated inside this run: ${remergedBlockIds.size} (cap ${CHALLENGE_REMERGE_MAX}, ${remergeDeferred} deferred)
  every comparison, not only the ones over the ratio:
${challengeComparisons.map(c => `    ${c.blockId} (${c.docPath}:${c.lines}) first pass ${c.primary} cells, independent reader ${c.challenger} behaviours`).join('\n') || '    (none)'}
blind review of NO-BEHAVIOR write-offs (phase 5b):
  write-offs: ${noBehaviorBlocks.length}, reviewed: ${nbReviewed}, disputed: ${nbDisagreements.length} (${(nbRate * 100).toFixed(0)}%, threshold ${(NO_BEHAVIOR_MAX_DISAGREE_RATE * 100).toFixed(0)}%)
${nbDisagreements.map(d => `    ${d.blockId} (${d.docPath}:${d.lines}) — written off as ${JSON.stringify(d.writeOffReason)}; a blind reader found: ${d.behaviors.join('; ')}`).join('\n') || '    (no disputes)'}
table-row floor: ${excusedShortfalls.length} table blocks under their floor and excused by an under-determined or unschemable escalation
${bounded(excusedShortfalls, s => `    ${s.blockId} — ${s.rows} rows, ${s.nonTestable} declared non-testable, floor ${s.floor}, ${s.cells} cells`, 40)}
matrix tables (3+ columns) carrying about one cell per row rather than one per matrix cell: ${thinMatrixTables.length}
  These clear the row floor. They are not gated, because the column count is a transcribed number
  and the comparison is a guess at what a healthy matrix produces — but a matrix table is the
  densest thing in the corpus and this is where under-enumeration would hide.
${bounded(thinMatrixTables, t => `    ${t.blockId} (${t.at}) — ${t.rows}x${t.cols}, ${t.cells} cells, a matrix reading would suggest at least ${t.looseFloor}`, 40)}
repair round: ${repairLog.length} batches repaired, ${repairLog.filter(r => !r.accepted).length} rejected
${bounded(repairLog, r => `    ${r.batchId} — ${r.before} -> ${r.after} cells, ${r.accepted ? 'accepted' : 'REJECTED'} (${r.why})`, 40)}
reverse code-keyed check: ${REVERSE_CHECK ? reverseFindings.map(r => `${r.angle} ${r.count} findings over ${r.itemsChecked} items`).join('; ') || '(no angle returned)' : 'not run'}

=== PER UNIT ===
${unitRows}

=== DOCUMENTS DELIBERATELY NOT WALKED ===
Named individually, with reasons:
${EXCLUDED.map(u => `${u.path} — ${u.why}`).join('\n')}

Excluded as whole directories rather than file by file:
${EXCLUDED_TREES.map(t => `${t.path} — ${t.why}`).join('\n')}

=== REVERSE CHECK FINDINGS ===
These are DOCUMENT OMISSIONS for the owner: things the compiler has that no canonical document
mentions. They are not test cells and they change nothing about what the documents require. Put
them in REPORT.md with their evidence and their question. Each angle also wrote its own file
under ${BUNDLE}/doc-omissions-<angle>.md.
${reverseFindings.map(rf => `
--- ${rf.angle} (${rf.count} findings over ${rf.itemsChecked} items checked) ---
${bounded(rf.findings, f => `  - ${f.item} [${f.evidence}] — ${f.question}`, 40)}
  could not determine (${rf.couldNotDetermine.length}): ${rf.couldNotDetermine.slice(0, 20).join('; ') || '(none)'}`).join('\n') || '(reverse check not run)'}

=== ESCALATIONS ===
These are already published verbatim in ${BUNDLE}/escalations.md by a separate agent, and a
second agent has verified that every claim quote appears there character for character. Summarise
them in REPORT.md — kind, document, lines, and what each one blocks — and point at
escalations.md for the full quotes. Do not resolve one and do not rank them.

${bounded([...escalationGroups.values()], g => `  - ${g[0].kind} at ${g[0].docPath}:${(g[0].lines || []).join(',')} (raised ${g.length}x) — ${g[0].whyItBlocks}`, 40)}

=== THE INVENTORY FILES ===
Already written, one per unit, under ${BUNDLE}/inventory/. ${inventoryFilesWritten} of
${ACTIVE.length} were confirmed. Do not write them and do not rewrite them; just say in REPORT.md
that they are there and what they contain.

=== WHAT THIS RUN ESTABLISHES, AND WHAT IT DOES NOT ===
Reproduce this in REPORT.md, in these words, plus the numbered limits below it:

  "This is the strongest available structural check against the docs, not a mathematical proof
  of exhaustiveness. The method guarantees that every block a careful reader identified in each
  truth doc received an explicit disposition, and that every non-NO-BEHAVIOR block maps to >=1
  concrete cell. It does not guarantee that every testable behavior was extracted from every
  block. The named residual: a behavior stated only deep in prose — a single qualifying clause,
  a parenthetical, a footnote inside an otherwise-covered section — that the region reader did
  not decompose into its own cell. Block-level accounting cannot detect a behavior it never saw."

Then state, plainly:
  1. Block boundaries came from a fixed command rather than from any agent's count of how many
     blocks a document has, and the transcription of that command was cross-checked against a
     separately-derived count, a pinned per-document baseline and per-anchor validation. Given a
     correct anchor list the partition is arithmetic. The residual above is then the way a
     behaviour goes missing: not a block nobody saw, but a behaviour inside a block somebody read
     too quickly. The 2026-07-14 version could also lose a whole block to an agent's own count.
  2. No definition in this corpus has been compiled, so definition cleanliness is unproven. The
     first gate of the measurement run is that every definition compiles free of PRE0158
     FieldNeverSet (no write site, a Warning) and PRE0093 RequiredFieldsNeedInitialEvent noise.
     Separately, nothing in this run compares a definition against what its cell claims to check
     — only the ${auditChecked} audited cells had their FRAGMENT examined for relevance, and
     ${auditFragmentFails.length} of those failed. Both are open going into measurement.
  3. ${CELLS.filter(c => c.expected === 'reject:any').length} cells carry reject:any, which
     passes against any error at all including ambient noise. Each names the sentence that is
     under-determined. That count is a measure of how determinate the documents are, and it is
     for the owner to look at.
  4. ${CELLS.filter(c => (c.expected || '').startsWith('value:')).length} cells state a produced
     VALUE. The existing runner returns diagnostic codes and severities only and cannot check
     them. Building that comparison is an open decision.
  5. ${CELLS.filter(c => c.conflict).length} cells rest on text that contradicts another part of
     the canonical documents. They are flagged and they are not trustworthy until the
     contradiction is ruled.
  6. The reverse code-keyed check is one-directional and leans on code the project does not
     treat as a record of the language. Its findings are questions about the documents.
  7. ${blocksNone} blocks were written off as stating no testable behaviour. ${nbReviewed} of
     them were re-read by an agent that was not shown the reason given, and
     ${nbDisagreements.length} of those were disputed. The rest of the write-offs rest on their
     one-line reason and nothing else. Those reasons are in the inventory files; they are worth
     reading.
  8. ${excusedShortfalls.length} table blocks are below their row floor with an escalation
     explaining why, and ${remergeDeferred} blocks were over the under-enumeration ratio but past
     this run's re-enumeration cap and keep their first-pass cells.
  9. assembled/all-cells.json was built by an agent from files other agents wrote. This script
     reconciled its totals, per-unit counts, duplicate ids and superseded bookkeeping against its
     own validated records. It did not compare record CONTENT. A batch that returned a good
     record and wrote a hollow one is not detectable from here.

Return the schema.`

const report = await agent(reportPrompt, {
  label: 'report', phase: 'Publish', model: 'opus', effort: 'high',
  schema: {
    type: 'object', additionalProperties: false,
    required: ['wroteReport', 'hardFailuresTranscribed', 'warningsTranscribed', 'notes'],
    properties: {
      wroteReport: { type: 'boolean' },
      hardFailuresTranscribed: { type: 'integer', description: 'how many of the hard failures you put in REPORT.md' },
      warningsTranscribed: { type: 'integer', description: 'how many of the warnings you put in REPORT.md' },
      notes: { type: 'string' },
    },
  },
})

// This is the last writer in the run, so a failure here has nowhere to be recorded except the
// return value. That is admitted as judgement call #11 rather than claimed away: splitting phase
// 8 into five jobs means a report failure no longer takes the inventories, the escalations and
// the merged corpus with it, but it still means no REPORT.md.
if (!report) fail('report agent did not return — REPORT.md does not exist, and this failure can only be read from the workflow return value')
else {
  if (!report.wroteReport) fail('REPORT.md was not written')
  const shownProblems = Math.min(problems.length, PROMPT_LIST_MAX)
  if (report.hardFailuresTranscribed < shownProblems) {
    fail(`report transcribed ${report.hardFailuresTranscribed} of the ${shownProblems} hard failures it was given — the rest were summarised or dropped`)
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// The gate. Loud, last, and not delegated.
// ─────────────────────────────────────────────────────────────────────────────

const result = {
  gate: problems.length ? 'FAILED' : 'PASSED',
  documentsWalked: ACTIVE.length,
  documentsExcluded: EXCLUDED.map(u => u.path),
  directoriesExcluded: EXCLUDED_TREES.map(t => t.path),
  blocks: BLOCKS.length,
  blocksBehavior, blocksNoBehavior: blocksNone, blocksUnaccounted: blocksMissing,
  oversizedBlocks: bigBlocks.map(b => ({ id: b.id, lines: b.lines, kind: b.kind, at: `${b.docPath}:${b.startLine}-${b.endLine}`, cells: (cellIdsByBlock.get(b.id) || []).length })),
  cells: CELLS.length,
  cellsMalformed: [...malformedCells.entries()].map(([id, why]) => ({ id, why })),
  perUnit,
  escalations,
  hardFailures: problems,
  warnings,
  audit: {
    checked: auditChecked, failed: auditFailed, rate: auditRate, failingIds: auditFailIds,
    perUnit: auditUnitRows, fragmentFailures: auditFragmentFails,
  },
  challenge: {
    eligibleBlocks: eligible.length,
    excludedAsShort: challengeExcludedShort,
    blocksChallenged: candidates.length,
    challengersReturned: challengeReturned,
    comparisons: challengeComparisons,
    shortfalls: shortfalls.map(s => ({ block: s.block.id, primary: s.primary, challenger: s.challenger })),
    reEnumerated: [...remergedBlockIds],
    deferredPastCap: remergeDeferred,
  },
  noBehaviorReview: { writeOffs: noBehaviorBlocks.length, reviewed: nbReviewed, disputed: nbDisagreements },
  tableFloor: { excusedShortfalls, thinMatrixTables },
  repair: repairLog,
  reverse: reverseFindings,
  merge,
  inventoryFilesWritten,
}

if (problems.length) {
  log('')
  log('══════════════════════════════════════════════════════════════════')
  log(`  GATE FAILED — ${problems.length} hard failures. The corpus is not ready to measure.`)
  log('══════════════════════════════════════════════════════════════════')
  for (const p of problems) log(`  ${p}`)
  log('══════════════════════════════════════════════════════════════════')
  // Thrown after REPORT.md is on disk, so the run is never both failed and silent: the record
  // shows a failure and the report shows why.
  if (THROW_ON_GATE_FAILURE) throw new Error(`enumeration gate failed: ${problems.length} hard failures — see ${BUNDLE}/REPORT.md`)
}

log(`Enumeration complete: ${BLOCKS.length} blocks, ${CELLS.length} cells, ${escalations.length} escalations, gate ${result.gate}`)
return result
