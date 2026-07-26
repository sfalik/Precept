/*
 * Block inventory — recovered workflow.
 *
 * WHERE IT CAME FROM. This is the workflow that ran on 2026-07-14 and produced an
 * exhaustive block-by-block inventory of nine regions of the canonical documents:
 * 888 blocks, every one of them labelled and accounted for, written to
 * docs/Working/exhaustive-gap-analysis-2026-07-14/. It existed only inside Claude Code
 * session state — workflow record wf_6f0ba18d-2f4, project
 * d73a29fc-606b-4eff-baca-888af6a3789d — and was recovered into the repository on
 * 2026-07-25 before that bundle is deleted.
 *
 * WHAT THE RUN COST, from the workflow record: ten agents, 22 minutes of wall clock,
 * 1.46 million tokens, 263 tool calls. The five units below with `model: undefined`
 * inherited the session's model, which was claude-opus-4-8[1m]; the other four were
 * pinned to sonnet. Pin them deliberately next time rather than inheriting.
 *
 * NEITHER THE ORIGINAL RUN NOR THIS EDIT HAS BEEN REVIEWED BY ANYONE. The 888 figure is
 * what the run reported about itself. Read docs/working-reset/phase-3/block-inventory-method.md
 * for what the method does and does not establish, and for the two things that have to be
 * decided before this is run again.
 *
 * WHAT WAS CHANGED FROM THE RECOVERED ORIGINAL, and nothing else:
 *
 *  1. The draft corpus is gone. The original passed each agent a file of pre-existing
 *     draft cells and had it label every block COVERED (a draft cell already exercised the
 *     block) or ADDED (no draft cell, so write some). There is no draft corpus now — the
 *     cells are being regenerated from nothing — so those two labels collapse into one:
 *     the block carries behaviour, or it does not. Every behaviour-bearing block gets its
 *     cells written in the same pass. The output schema, the arithmetic check and the
 *     per-document guidance are all edited to match.
 *
 *     KNOW WHAT THIS COSTS. The block inventory reproduces from this script. The cells do
 *     not. Of the 4,592 cells the 2026-07-14 run reported, about 3,413 came in as that
 *     draft corpus and only about 1,179 were written here, filling its gaps. This script
 *     completes a cell corpus; it does not produce one. Run it expecting a full corpus and
 *     roughly a quarter arrives, with nothing saying the rest is missing.
 *  2. The four spec line ranges were re-checked against HEAD. Three are unchanged. The
 *     language spec grew from 2,170 lines to 2,219, and the semantics-and-proof region is
 *     the last one in the file, so its end moved from 2169 to 2219.
 *  3. The proof-engine guidance is left as it was and marked. See the comment above it.
 *  4. Vocabulary, no change of meaning: two phrasings reworded to say the canonical
 *     documents govern, and one "reject-with-witness" changed to "reject-with-counterexample"
 *     to match the rename phase 0 ran across the repository.
 *
 * Everything else is as recovered: what counts as a block, the every-block-accounted rule,
 * the one-line reason required for a no-behaviour call, the pressure to enumerate the whole
 * family rather than the instance the prose names, the do-not-compile instruction, the
 * per-document model and effort assignments, the arithmetic check, and all nine guidance
 * paragraphs.
 */

export const meta = {
  name: 'block-inventory',
  description: 'Walk the canonical documents block by block; account for every block; enumerate the behaviour each one states',
  phases: [
    { title: 'Walk', detail: 'one agent per document region: block inventory + enumerate that region\'s cells' },
    { title: 'Synthesize', detail: 'merge per-unit outputs into coverage-report.md rollup + master cell manifest' },
  ],
}

// Set this to wherever the run's output should land.
const BUNDLE = 'docs/working-reset/block-inventory'

const SUMMARY_SCHEMA = {
  type: 'object',
  additionalProperties: false,
  required: ['unit', 'docPath', 'lineRange', 'blocksTotal', 'blocksWithBehavior', 'blocksNoBehavior', 'cellsTotal', 'stopAndFix', 'notes', 'wroteCoverageFile', 'wroteCellsFile'],
  properties: {
    unit: { type: 'string' },
    docPath: { type: 'string' },
    lineRange: { type: 'string' },
    blocksTotal: { type: 'integer' },
    blocksWithBehavior: { type: 'integer' },
    blocksNoBehavior: { type: 'integer' },
    cellsTotal: { type: 'integer' },
    stopAndFix: { type: 'array', items: { type: 'string' } },
    notes: { type: 'string' },
    wroteCoverageFile: { type: 'boolean' },
    wroteCellsFile: { type: 'boolean' },
  },
}

const SHARED = `
You are assembling the CELL SET for Precept's exhaustive gap analysis, Phase 1. Precept is a .NET domain-integrity DSL: a compiler proves business rules make invalid data structurally impossible (prevention, not detection). The canonical documents SPECIFY intended compile-time/runtime behavior; the mission is to check the compiler against every specified behavior.

DEFINITIONS
- CELL = one specific compile-time/runtime behavior a doc says the compiler should have (e.g. "money / money of a different currency is a type error"; "sqrt of a possibly-negative decimal must be rejected unless proven non-negative"). One doc claim -> one check.
  A cell = { id, desc, exercise, specCite, expected }.
  * exercise = a SHORT DSL fragment that isolates the behavior (e.g. "field Price as money 'USD'; field Qty as number; rule Total: Price / Qty" ) — NOT a full runnable definition. A later phase authors the full def and RUNS it; you do not.
  * expected = the doc-SPECIFIED expectation: "accept" | "reject:<PRE#### or DiagnosticName>" | "reject:any". This is what the DOC says should happen, NOT what the compiler actually does. Do NOT compile, do NOT judge pass/fail — that is a later phase.
  * specCite = "<docPath>:<line>" anchoring the claim.
- BLOCK = a heading section, a markdown table, or a worked-example fenced code block in your region.

YOUR JOB (this pass fixes WHAT gets checked, not verdicts)
Walk your assigned region block-by-block. Every block gets EXACTLY ONE disposition:
  (a) BEHAVIOR — the block states one or more testable compile-time/runtime behaviors -> enumerate the cell(s) for it.
      ENUMERATE THE WHOLE FAMILY, not just the single instance the prose names: every operator x operand-type, every accessor/member-call, every literal-lane x position, every wrong-kind/wrong-arity misuse, qualifier/unit variants, and the boundary/error cases (empty, zero, negative, overflow, unguarded). Under-enumeration is THE primary failure mode of this pass — err toward more cells.
  (b) NO-BEHAVIOR — pure prose, rationale, motivation, cross-reference, or IMPLEMENTATION-internal description (how the compiler is built, not what it guarantees) with no testable behavior -> mark it with a one-line reason.
EVERY block must carry a disposition. An unaccounted block is a stop-and-fix (list it in stopAndFix).

INPUTS
- Your doc region (path + line range) — read it in full. There is no pre-existing cell corpus; you are enumerating from nothing.
- Optional backstop: grep docs/Working/compiler-readiness-plan-2026-06-11-appendices/spec-coverage-audit.md for your section if you suspect a missed behavior. That file is agent-written and sits inside the folder phase 1 disposes of; it is not evidence of anything, and its only use is to prompt you to re-read the canonical text. If it has been deleted, skip this.

OUTPUTS — write TWO files (use absolute repo-relative paths from the repo root), THEN return the schema summary:
1. WRITE ${'`'}${BUNDLE}/coverage/<UNIT>.md${'`'} — a block-inventory markdown. Start with a one-line rollup (blocks total / with-behavior / no-behavior; cells total). Then a table grouped by section: | block (heading text or table/example + line) | kind (section|table|example) | disposition | cell ids / no-behavior reason |. This file is the PROOF the region is exhaustively accounted — every block appears.
2. WRITE ${'`'}${BUNDLE}/assembled/<UNIT>.cells.json${'`'} — a JSON array of ALL cells for this unit: { id, desc, exercise, specCite, expected, block:<blockId> }. Cell ids unique + kebab-case + prefixed "<UNIT>/...".

RULES
- The canonical documents govern. Unbuilt-but-specified behavior is a REAL cell (it will legitimately verdict as a gap later — wanted, not deferred).
- Do NOT author full defs, do NOT run the compiler/MCP, do NOT assert pass/fail.
- Be exhaustive on families; be honest in stopAndFix about anything you could not confidently dispose.
`

const UNITS = [
  { unit: 'spec-preamble-lexer', docPath: 'docs/language/precept-language-spec.md', lineRange: '84-812', model: undefined,
    extra: 'Region: §0 Preamble + §1 Lexer. Behavior here = tokenization, literal lanes (numeric/string/temporal/money/quantity two-door contracts), keyword/identifier rules, comment handling, what is a lex error. Enumerate every literal lane x valid/invalid form.' },
  { unit: 'spec-parser', docPath: 'docs/language/precept-language-spec.md', lineRange: '813-1214', model: undefined,
    extra: 'Region: §2 Parser. Behavior = every construct/grammar production (field, rule, ensure, event, state, transition, guard, action, modifiers, blocks), well-formed vs syntax-error cases, disambiguation. Enumerate each construct x present/missing/duplicated/misordered clause.' },
  { unit: 'spec-typecheck', docPath: 'docs/language/precept-language-spec.md', lineRange: '1215-1846', model: undefined,
    extra: 'Region: §3 Name Binding and Type Checking. Behavior = name resolution, type inference/compatibility, operator type rules, coercion/mix rules, wrong-type/undefined-name errors. Enumerate operator x operand-type matrices and every "is a type error" sentence.' },
  // Line range updated 2026-07-25: this region runs to the end of the file, and the file
  // grew from 2,170 lines to 2,219. The other three spec regions were re-checked and are
  // unchanged (§2 still starts at 813, §3 at 1215, §3A at 1847).
  { unit: 'spec-semantics-proof', docPath: 'docs/language/precept-language-spec.md', lineRange: '1847-2219', model: undefined,
    extra: 'Region: §3A Language Semantics + §4 Graph Analyzer + §5 Proof Engine + Open Questions + Cross-References. Behavior = evaluation semantics, reachability/dead-end/unreachable/sink structural checks, proof-engine prove-or-reject at the spec level. Mark Open Questions / Cross-References as no-behavior unless they state a normative rule. Expect a lot of cells on the structural checks.' },
  { unit: 'primitive-types', docPath: 'docs/language/primitive-types.md', lineRange: 'whole', model: 'sonnet',
    extra: 'Whole doc. Enumerate each primitive x each operator/accessor/literal-form, and every family member the prose names only by example.' },
  { unit: 'temporal', docPath: 'docs/language/temporal-type-system.md', lineRange: 'whole', model: 'sonnet',
    extra: 'Whole doc. Enumerate across date/time/datetime/duration/period types x operators, qualifiers, ordering, arithmetic, literal forms.' },
  { unit: 'business-domain', docPath: 'docs/language/business-domain-types.md', lineRange: 'whole', model: 'sonnet',
    extra: 'Whole doc. Enumerate across money/quantity/percentage/etc. x currency-unit qualifiers, cross-qualifier operations, rounding, mix rules. This doc has locked decisions (e.g. §D6 units) — a cell checks BEHAVIOR, not the decision.' },
  { unit: 'collection', docPath: 'docs/language/collection-types.md', lineRange: 'whole', model: 'sonnet',
    extra: 'Whole doc. Enumerate across list/set/map/stack/queue x element-value modifiers, accessors, mutations, guarded/unguarded access, contains/lookup, scalar-op-on-collection misuse.' },
  // DECIDE BEFORE RUNNING. The last three sentences of this paragraph tell the agent to
  // treat §6 Architecture, §7 Component Mechanics, §8 Dependencies and §9 Failure Modes as
  // out of scope. On 2026-07-14 that instruction wrote off 64 of this unit's 112 blocks —
  // more than half the document, and the largest single write-off in the run. It came from
  // a plan that no longer exists. Whether it still holds is a decision for whoever runs
  // this, not something to inherit silently. Either keep it and say why, or delete it and
  // expect the unit to grow.
  { unit: 'proof-engine-prevention', docPath: 'docs/compiler/proof-engine.md', lineRange: 'prevention-guarantee-only', model: undefined,
    extra: 'SPECIAL UNIT — net-new enumeration. Scope STRICTLY to the PREVENTION GUARANTEE the doc states, NOT the engine implementation. The guarantee: every fault-prone operation is PROVEN safe or REJECTED (prove-or-reject; unprovable = reject, never skip). Enumerate: fault classes { division-by-zero, modulo-by-zero, integer overflow, decimal overflow, sqrt/root of negative, other rejectable math, unguarded collection index/access, unguarded key lookup, null/absent access } x contexts { rule expression, ensure expression, guard/when expression, computed field default, transition action, arg default } x outcome { statically-safe -> accept-with-proof-certificate, statically-violating -> reject-with-counterexample, not-statically-decidable -> reject (unprovable=reject) }. Draw the fault-class list from §5 Inputs/Outputs, the fault taxonomy, and §10 Contracts and Guarantees. Mark §6 Architecture, §7 Component Mechanics (HOW the engine is built), §8 Dependencies, §9 Failure Modes as NO-BEHAVIOR (implementation-internal, out of scope per the plan) UNLESS a subsection states a normative guarantee. specCite to proof-engine.md lines.' },
]

phase('Walk')
const summaries = await parallel(UNITS.map(u => () => {
  const prompt = `${SHARED}

=== YOUR ASSIGNMENT ===
UNIT: ${u.unit}
DOC REGION: ${u.docPath}  lines ${u.lineRange}
UNIT-SPECIFIC GUIDANCE: ${u.extra}

Write:
- ${BUNDLE}/coverage/${u.unit}.md
- ${BUNDLE}/assembled/${u.unit}.cells.json
Then return the summary. In the summary set unit="${u.unit}", docPath="${u.docPath}", lineRange="${u.lineRange}".`
  return agent(prompt, {
    label: u.unit,
    phase: 'Walk',
    schema: SUMMARY_SCHEMA,
    model: u.model,
    effort: 'high',
  })
}))

const ok = summaries.filter(Boolean)
log(`Walk complete: ${ok.length}/${UNITS.length} units returned`)

// The accounting check. This is the part that is lost if the units are run as loose
// parallel agents instead of as this workflow — there is then nothing that adds the
// numbers up, and it falls to whoever reads the results.
const problems = []
let cellsTotal = 0
for (const s of ok) {
  const acct = s.blocksWithBehavior + s.blocksNoBehavior
  if (acct !== s.blocksTotal) problems.push(`${s.unit}: block accounting ${acct} != ${s.blocksTotal}`)
  if (s.stopAndFix && s.stopAndFix.length) problems.push(`${s.unit}: ${s.stopAndFix.length} stop-and-fix`)
  cellsTotal += s.cellsTotal
}

phase('Synthesize')
const synthPrompt = `You are the synthesis step of Precept's exhaustive block inventory. Nine per-doc-region agents have each written:
- ${BUNDLE}/coverage/<unit>.md  (block inventory)
- ${BUNDLE}/assembled/<unit>.cells.json  (that unit's cells)
for units: ${UNITS.map(u => u.unit).join(', ')}.

Do THREE things:
1. Read every ${BUNDLE}/assembled/*.cells.json and MERGE them into ${BUNDLE}/assembled/all-cells.json — a single JSON array of all cells. Verify cell ids are globally unique (report collisions; do not silently drop). Add nothing; just concatenate + validate.
2. Read every ${BUNDLE}/coverage/<unit>.md and write ${BUNDLE}/coverage-report.md. Structure: a top rollup table (one row per unit: doc, line range, blocks total / with-behavior / no-behavior, cells total, links to the per-unit coverage/<unit>.md), then a "Completeness" section stating whether 100% of blocks are accounted (with-behavior + no-behavior == total for every unit) and listing any stop-and-fix items still open, then the honest-limit statement (this is the strongest available check against the docs, not a mathematical proof; a behavior stated only deep in prose and missed by a reader is the named residual).
3. Write ${BUNDLE}/STATUS.md: the merged cell total, the per-unit breakdown, and a one-line pointer to coverage-report.md and assembled/all-cells.json.

Do NOT summarize away a hedge. If a unit's stopAndFix entry says something is flagged rather than clean, the rollup says so in those words. On 2026-07-14 a unit returned "None outright, but flagged for Phase 2: <a real doc-internal contradiction>" and the rollup recorded "Stop-and-fix: None"; the contradiction then sat unruled for eleven days.

Return a short JSON summary: { mergedCellCount, idCollisions:[...], unitsAccountedFully:int, unitsWithStopAndFix:[...], wroteAllCells:bool, wroteCoverageReport:bool, wroteStatus:bool }.`

const synth = await agent(synthPrompt, {
  label: 'synthesize',
  phase: 'Synthesize',
  effort: 'high',
  schema: {
    type: 'object', additionalProperties: false,
    required: ['mergedCellCount', 'idCollisions', 'unitsAccountedFully', 'unitsWithStopAndFix', 'wroteAllCells', 'wroteCoverageReport', 'wroteStatus'],
    properties: {
      mergedCellCount: { type: 'integer' },
      idCollisions: { type: 'array', items: { type: 'string' } },
      unitsAccountedFully: { type: 'integer' },
      unitsWithStopAndFix: { type: 'array', items: { type: 'string' } },
      wroteAllCells: { type: 'boolean' },
      wroteCoverageReport: { type: 'boolean' },
      wroteStatus: { type: 'boolean' },
    },
  },
})

return { units: ok, walkProblems: problems, walkCellsTotal: cellsTotal, synth }
