# Compiler readiness plan

**Branch**: `spike/Precept-V2-Radical-reset` · **Started**: 2026-07-25

This is the plan for finishing the Precept compiler, and it is also the only place the work is
tracked. There is no status page beside it and no second plan.

**How to use it.** Read the tracker below — four short sections that say where the work is, what is
stuck, what has already been settled, and what needs you. That is the page you open each morning;
it runs to about a hundred lines, most of it table rows, because thirteen phases and six pending
questions do not compress further without dropping facts. Everything under **The phases** is
reference: open the entry for the phase you are working in, do what it says, stop at its finish
line. If a thing is not written here it is not being tracked.

**Keeping the tracker current is part of finishing a slice.** A slice whose tracker row was not
updated in the same commit is not finished. That is not bookkeeping etiquette. The last three months
produced a tracking page that was wrong about five of its seven headline items, and work was built on
a picture that was not true.

---

# Tracker

## 1. Where we are

**Phase 1, slice 1.1 — not started. Last touched 2026-07-25.**

**Next:** slice 1.1 — walk `342e66db..327364f8` and build the decision register. Nothing blocks it.

**Landed ahead of phase 1, 2026-07-25.** Three files were pulled out of the cell bundle before slice 1.6
deletes it, and they are now depended on: `block-inventory.workflow.js` with its
`block-inventory-method.md` (phase 3's Size cites it) and `canon-contradictions-found-2026-07-14.md`
(phase 2's second input, and it holds one contradiction that is live at HEAD and never ruled). Slice 1.6
step 1 assumes all three already exist.

## 2. The board

| # | Phase | State | Attempts | Blocked on |
|---|---|---|---|---|
| 0 | Make the instructions true | **Done** 2026-07-25, `355d9b37` `89392acd` | 1 | — |
| 1 | Work out what is actually decided, and by whom | Not started | 0 | — |
| 2 | Settle every place canon and the want document disagree | Not started | 0 | Phase 1 |
| 3 | Corrected statement of the language, and the catalog drift check | Not started | 0 | Phase 2 |
| 4 | Draft the certificate step list and count it | Not started | 0 | Phase 3; ends on **Shane's** recorded word |
| 5 | Settle the obligation families and the discharge procedure | Not started | 0 | Phases 3, 4 |
| 6 | One argument per discharge rule and per step kind | Not started | 0 | Phase 5 |
| 7 | Generate the coverage grid | Not started | 0 | Phases 5, 6 |
| 8 | Confirm, narrow, or revert | Not started | 0 | Phase 7; **Shane's** decision |
| 9 | Fill the catalogs with the settled model | Not started | 0 | Phases 7, 8 |
| 10 | Turn every numbered claim into a criterion and a failing test | Not started | 0 | Phases 3, 5, 7, 9; **Shane's** second stop-or-continue |
| 11 | Build until the frozen suite is green | Not started | 0 | Phase 10 |
| 12 | Confirm it is finished, by people who did not build it | Not started | 0 | Phase 11; **Shane** signs the finish |

**Attempts.** A failed close increments the number in the same commit that records the failure. How the
rule fires: "Failure to converge is an answer", below.

**Elapsed time before the first line of product code.** Phases 1 to 10 sum to 32–50 working days, six
and a half to ten weeks, before the three-attempt allowance at phases 5 and 6. Phase 4, the cheap early
stop, lands two and a half to three and a half weeks in. Recompute whenever a size changes.

### Phase 1 slice board

| Slice | State | Blocked on |
|---|---|---|
| 1.1 Walk the commits and build the register | Not started | — |
| 1.2 Pull out the decisions that live only in a document | Not started | — |
| 1.3 Prepare the conversation | Not started | 1.1, 1.2 |
| 1.4 The walk itself | Not started | 1.3, Shane's time |
| 1.5 Fold the marks back | Not started | 1.4 |
| 1.6 Dispose (does not block the phase) | Not started | 1.5 |

## 3. Ruled — do not reopen

**Anything an agent proposes that this list already answers is not a decision.** Send it back. Five of
five claimed owner decisions checked in the week to 2026-07-25 were already answered somewhere.

| What was settled | Reasoning |
|---|---|
| **A branch, not a copied folder** — promoted or reverted at phase 8 | Phase 8 |
| **The certificate checker is in scope**, including its runtime load-time half | Phase 11 |
| **Coverage rests on the catalogs** — the checker asks the catalog what each thing owes | Phase 9 |
| **A bounded counterexample search is in scope**, numeric scalars first | Phase 6, cond. 6 |
| **Phase 11 carries no size**, given a clear finish line and a stop-or-continue at the test count | Phase 11 |
| **Vocabulary** — `counterexample` not `witness`; `case` not `arm`; `denominator` arithmetic only; `anchor` stays (this project's word for a modifier family) | Phase 0 ran the rename |
| **Thirteen phases, 0 to 12**, as on the board, re-cut only under the rules below | Settled 2026-07-25 |
| **Phase 8 is not a decision to start** — it is confirm, narrow, or revert | Phase 8 |
| **The discharge procedure is a walk over the language's expression forms**, one case each, nothing falling through | Phase 5, cond. 1 |
| **The coverage grid is generated, not written cell by cell** | Phase 7 |
| **Only phase 1 is sliced**; phases 2 to 12 stay as written — slicing phase 10 today plans against a definition that does not exist | Settled 2026-07-25 |
| **Promoting the branch to canon gates nothing**; `main` has had no commits since 2026-04-19 | Settled 2026-07-25 |

**Provenance.** These rows come from an agent-written document dated 2026-07-25 and carry none of the
verbatim quotations phase 1 requires. Phase 1's walk removes any row you did not say.
| The old `docs/Working/` folder stays until phase 8 confirms, then everything moves to `Archive/` or `Superseded/` | Settled 2026-07-26 | Slice 1.6 |
| The `nonnegative` unsoundness is fixed in phase 11 with everything else, not now | Settled 2026-07-26 — 47 tracked defects and probably hundreds untracked; fixing one does not move the needle | "Known risks", 4th |
| The certificate step vocabulary closes at **phase 5**, not phase 4 — phase 4 produces a counted draft, frozen between the count and phase 5 opening | Settled 2026-07-26 | Phase 4; "What closes when" |
| Phase 4 has **no numeric threshold** — it delivers the count plus, per step kind, recovered-or-invented and afternoon-or-fortnight; you record carry on or re-cut | Settled 2026-07-26 | Phase 4, "No numeric threshold" |
| Phase 6 carries **no numeric re-cut threshold** either, and its size is answered by phase 4's afternoon-or-fortnight marks rather than picked now | Settled 2026-07-26 | Phase 6, "Size" |
| The matrix's six ways of slicing do **not** carry as structure — the procedure walks expression forms. Three of the six survive as inputs, one is wrong as written and must be corrected, two are dropped | Settled 2026-07-26 | Phase 5, "What survives of the six" |

## 4. Waiting on Shane

**One item, and it blocks phase 5.**

| What you are asked | Stalled until you answer | Reasoning |
|---|---|---|
| **Where the discharge fragment stops** — which formulas the compiler will decide, and therefore what the language admits. Expect the first honest fragment to be smaller than today's grammar; that gap is the price of the guarantee | **Phase 5** | Phase 5, "Where the fragment stops" |

Every request for your word that is not a scheduled phase sign-off belongs here. The sign-offs are the
four **Shane** rows on the board and each phase's *What you sign off* paragraph.

---

# How this plan is executed

## What a finish line has to be

Every phase's *done when* has to be checkable by somebody who did not do the work. In order of
preference: a command whose output settles it, a count recomputable from a file in the repository, or a
build result. Where a finish line comes down to judgement it says so plainly, names whose judgement it
is, and names what they are looking at. It does not get dressed up in language that sounds checkable.

This is the priority for the whole plan. The difficulty this project has had is work with no way to
know it was finished.

**Some phases write code, deliberately.** Phases 3, 5 and 9 each produce a build check as well as a
document, and phase 6 produces the counterexample search. Building does not start at phase 11. The code
in those phases is what makes their finish lines checkable rather than declared, and their sizes have
to cover writing it.

## What may not be used as evidence

These hold in every phase, and each is the reason an earlier effort produced results that had to be
thrown away.

- **The catalogs, as a record of what is decided.** They are incomplete, worst in proof requirements
  and discharge — of 39 measured places where the catalogs did not reflect a decision, about 30 were
  there. The canonical documents are what completeness is measured against. The catalogs become
  trustworthy at phase 9 and not before.
- **The `samples/` files, as a measure of coverage.** There are 78 of them, confirmed at HEAD. They
  illustrate; they do not cover. No finish line in this plan is a count of sample files.
- **Compiling a `.precept` file and reading current behaviour as the specification.** The compiler is
  unfinished, and its proof results in particular are wrong in at least one confirmed case — the
  `nonnegative` defect described under "Waiting on Shane".
- **The earlier readiness plans, as a guide to approach.** Five exist. Only the first ever shipped code.
  None of the later ones was ever settled. Their structure and framing are off limits.
- **Any "Locked", "ratified" or "owner decision" label.** These are agent-written in this project. Phase
  1 exists precisely because nobody has checked which of them you actually said.

## Two rules with teeth

**A closed list stays closed.** The *what closes when* table says which list stops moving at the end of
which phase. If any later phase adds or removes a member of a closed list, everything downstream halts
until you rule on whether to reopen it. Measured, not theoretical: the fault family went thirteen,
eleven, ten, nine, and every move threw away finished work.

*How it fires.* At the phase that closes a list, the `sha256sum` of that list's data file goes in the
table below, and a build check compares. Without the hash the rule depends on somebody noticing, which
is how it failed to fire before.

**Failure to converge is an answer.** If a phase does not close after **three** attempts, and each
attempt surfaced a different problem rather than the same one narrowing, that is evidence about the
model rather than about effort. Work stops and it comes to you as a revert question. Also measured: the
establishment and preservation design ran three passes across two days — roughly fifty agents and three
and a half hours — with a different objection each round, and each round was treated as needing one more
pass.

*How it fires.* The board's attempts column, and one line per attempt in the phase entry naming the
problem. The rule is applied by reading three rows.

**A phase that cannot be sized is too big and must be re-cut.** One exception, agreed: phase 11 carries
no size. Phases 6 and 10 carry a formula over a count an earlier phase produces rather than a number,
and each names the value above which it is re-cut rather than attempted. That is not an evasion of the
rule — the day the input count arrives, the number is computed and the re-cut decision is taken then.

## What closes when

"Closed" means no further change without your ruling, and reopening halts everything downstream. It
deliberately is not the word "locked", which has meant three different things in this project.

| At the end of | This stops moving | Hash recorded |
|---|---|---|
| Phase 2 | The disagreements between canon and the want document. Nothing after re-litigates them | n/a |
| **Phase 3** | **The language** — what you can write in a `.precept` file, including the list of write sites. The lists are machine-readable and the build checks them against the catalogs. Everything downstream assumes this | one per data file |
| Phase 5 | The discharge procedure; the grid's axes; and **the certificate step vocabulary** — drafted and counted at phase 4, closed here | one per data file |
| Phase 7 | The proof model. The grid is generated and reproducible from the closed lists | grid file |
| Phase 9 | The catalogs | one per catalog data file |
| Phase 10 | The test set, frozen at a named commit | freeze commit |
| Phase 12 | The compiler | — |

**Two departures from the settled input, stated rather than made quietly.**

*The certificate step vocabulary closes at phase 5, not phase 4.* **Ruled 2026-07-26 — this is settled, not a proposal.** The signpost's table puts it at phase
4. But step kinds are what the discharge procedure emits, and the procedure is not written until phase 5
— so phase 5 will very likely need a step kind phase 4 did not draft, and under the closed-list rule
that halts everything downstream on the plan's own schedule. Phase 4 keeps its whole value, which is the
count and the stop; it produces a dated draft rather than a closure. If you want the signpost's version
instead, say so and phase 4 closes it, with the expectation that the rule fires at phase 5.

*Write sites and the grid's axes are added to the closing table.* Neither appears in the signpost. Write
sites are consumed three times downstream — phase 9 catalogues them, phase 5's induction argument
quantifies over them, the cost risk is priced in them — and no phase produced them. They are a language
question (what places in a definition can change a field), so they close at phase 3. The grid's axes are
what makes "no blank cells" mean anything; without them phase 7 chooses for itself what the grid is a
grid of, at generation time, and the unfinishable enumeration returns wearing a generator.

## Where the plan actually stops

If this turns out not to be buildable it will not announce itself at phase 8 as a tidy verdict. It will
look like a phase that keeps not closing. The real stopping points:

- **Phase 4**, deliberately and cheaply. Count the certificate step list. Everything after it assumes the
  remaining judgement is a couple of dozen careful arguments. If it is not, stop there rather than at
  month three.
- **Phases 5 and 6**, under the three-attempt rule. This is where the risk actually lives, because it is
  the same work that has already failed three times under a different arrangement.
- **The second stop-or-continue at the end of phase 10**, when the test count exists. This is the one the
  missing size estimate for phase 11 was agreed against.
- **Phase 8**, as a confirmation rather than a gate.

## Known risks, carried

**A ten-minute check nobody has run.** Editable-field edits are said to discharge at the ingress door, which
the execution order puts at phase 1. Computed fields recompute at phase 7. So a computed field derived from
an edited field is written *after* the check that was meant to cover it. Raised 2026-07-26 by an independent
read that did not probe it and called it worth ten minutes. If it holds, it is a soundness hole in an
argument the matrix treats as settled, and it lands in phase 2 or 5 depending on what it turns out to be.

**The induction's base rests on a stated assumption that is not written down as one.** "Every constraint held
before this operation" is *proven* for data that arrived through the contract, and *assumed* for restored and
host-injected data — the spec says restored state is trusted and not re-validated. The validity argument for
premise class (d) is honest about this and excludes it by citing canon rather than by argument. That makes it
an assumption of the guarantee, and phase 2 or 3 should state it in canon in those words rather than leave it
inside a proof argument where nobody reading the promise will find it.

Four things can still go wrong that no phase's finish line will catch on its own. Each is assigned.

- **The certificate step list may not be short.** Twenty-one step kinds have been designed and none has
  been built — `grep -rn "CertificateStep" src/ tools/ test/` returns nothing. Phase 4 exists to find out
  early.
- **Nobody has argued the induction itself.** The promise is that the initial event establishes every rule
  and every operation preserves it. Over a simple state machine that is obvious. Over this language —
  state residency, computed fields recomputed on every operation, `omit` resets on entry, entry and exit
  chains, first-match rows — it is not, and no argument for it exists anywhere. If it is wrong, phases 5
  through 7 produce nothing of value. **Phase 5 writes it; phase 6 attacks it as an entry like any
  other.**
- **The proof procedure may be too slow to be admissible.** The specification makes not breaking
  single-file recompile a condition of a strategy being allowed. A pass over every rule crossed with every
  write site is a different cost class. **Phase 5 takes the measurement before its strategy set closes.**
- **One live unsound discharge, confirmed by hand 2026-07-25.** `nonnegative` and `positive` contribute
  nothing to a field's declared interval. `field Bal as decimal nonnegative max 100` compiles clean, mints
  a containment obligation, marks it **Proved**, and reports the bound as `[−∞ .. 100]`, while a single
  event drives the value to −10. Not a coverage gap — a proof that says yes about something false.
  **Phase 10 owns the red test; phase 11 owns the fix** — unless the last row of "Waiting on Shane" pulls
  it forward.

  *The argument for pulling it forward*: it is a confirmed defect with a confirmed cause, it needs none of
  phases 2 through 10, and fixing it in week one would break the no-product-code record and prove the route
  from defect to green test works before ten weeks of documents are bet on it. *The argument against*:
  building against a statement of the language that has not been corrected yet is exactly the habit this
  plan exists to break, and a fix now would be written against the current compiler's idea of what an
  interval is. Nothing stalls either way.

---

# The phases

## Phase 0 — Make the instructions true

**Done.** 2026-07-25, commits `355d9b37` ("phase 0: correct the instruction files (first pass)") and
`89392acd` ("phase 0: finish making the instruction files true"). Not planned here.

**What it delivered.** The pull-request process in `CLAUDE.md` replaced with what actually applies on a
spike branch. Three read-first documents that still called the catalogs the language specification
corrected. `witness` renamed to `counterexample` across the canonical documents and one source comment.
The contradictions between files loaded in the same session removed — the reviewer agent enforcing a
pull-request process the plan forbids, the research skill routing into folders deleted three months ago,
the execute skill requiring a green suite while the suite is red. Six generated files that agents had
been told to hand-edit recovered into their sources and regenerated. The MCP guidance kept as it was
with one narrow exception recorded: the tools remain the primary way to answer a language question, but
while the compiler is being aligned to the spec its proof results are not trustworthy.

**What later phases owe it.** Phase 0 recorded every caveat it added, with file, line and quoted text,
in a register. That register has **three** sections, with three different owners:

- **Twenty-one catalog-incompleteness rows.** Phase 9 removes these. Two of them (`CLAUDE.md:100` and
  `docs/language/catalog-system.md:1285`) are marked "re-check, do not delete blind" because they
  describe a real property of the completeness tests rather than a phase-0 hedge — and their exit
  condition is **phase 3's** deliverable, not phase 9's.
- **Six proof-result rows.** These come out when the proof engine is aligned to the spec, not when the
  catalogs are filled. That is **phase 11**, not phase 9. The register says so in its own words.
- **Two rows with a different owner again.** `.claude/skills/execute/SKILL.md:122` ends when the test
  suite goes green — phase 11. `CONTRIBUTING.md:224` ends when somebody builds the test that checks the
  `# EXPECT:` headers in the sample files against emitted diagnostics — **no phase owned that work**, so
  phase 10 now does.

One warning the register carries and the phase that acts on it must heed: the row at
`tools/agent-sources/precept-author/body.md:243` requires editing the source and then running `node
tools/scripts/build-agents.js`. The text is generated into two agent files, and editing either output is
reverted by the next build.

---

## Phase 1 — Work out what is actually decided, and by whom

**Goal.** Establish, for every decision made since the specification was frozen, whether it traces to
something you wrote, is contradicted by something you wrote, or has no trace at all.

**Why it is first.** Every phase after this derives from decisions, and git authorship cannot separate
your rulings from agent output — every commit is under your identity and nearly all carry an agent
co-author line. Until the register exists, every later phase builds on claims nobody has checked.

**What it produces.**

- `docs/working-reset/phase-1/decision-register.json` — one row per decision, fixed columns, one of
  exactly three documentary verdicts each, plus your authority mark where you gave one. **Exists.**
- `docs/working-reset/phase-1/the-walk.md` — the numbered list you mark, and your marks once you have.
  **Exists**, with an answer already derived under every item, so a *not mine* settles it rather than
  opening a follow-up.
- `docs/working-reset/phase-1/want-document-provenance.md` — which lines of the want document are yours
  and which two later commits wrote the rest. **Exists**, and it is what the register's verdicts are
  measured against.
- `docs/working-reset/phase-1/decision-register-FINDINGS.md` — what was corrected in the register and
  why. **Exists.**
- `docs/working-reset/phase-9/phase0-caveats-to-remove.md` — the caveat register, moved. **Exists.**
- `docs/working-reset/phase-2/sweep-correction-list.md` — the 25-row correction list, copied in full and
  re-verified against HEAD. **Exists.**
- The want document itself is **not** copied here. It stays at `docs/Working/what-i-want-2026-07-16.md`
  so its history stays attached to it — the provenance map above is only readable against that history,
  and a copy would strand it.
- Separately, and not blocking the phase: a disposal commit.

**Done when.** All of the following, each recomputable:

1. Every commit in `git rev-list 342e66db..327364f8` has been walked, and the register records for each
   either a decision row or an explicit "carries no decision" row. Check: the count of distinct commit
   hashes named in the register equals the range's count, which is **83** at the time of writing —
   recompute rather than trusting the number.
2. Every row carries exactly one documentary verdict. Check: rows with zero = 0, rows with more than one
   = 0, and no fourth verdict value exists anywhere in the file.
3. Every row verdicted *traced* carries a quotation from something you wrote, with source and line.
   Check: traced rows with an empty quotation column = 0.
4. A second agent, working independently, has re-walked a random ten of the commits, and its rows are
   diffed against the first pass. Check: if the second pass finds a decision the first missed in more
   than one of the ten, slice 1.1 is re-run before anything else proceeds.
5. You have marked every item in the ruling walk. Check: unmarked items = 0.
6. The four moved files are present in this folder. Check: they exist and the correction list has all 25
   rows.

Note what condition 1 does and does not establish. It checks that the register *mentions* every commit.
It says nothing about whether the decisions in them were extracted correctly — that is what condition 4
and the sampling in slice 1.3 are for.

**What it may cite.** The want document; commit messages and diffs on this branch; the canonical
documents at HEAD; your own answers in the walk. Nothing else. In particular, no agent-written document
is evidence that you decided anything — establishing that is the whole point of the phase.

**What you sign off, and what you are looking for.** You do slice 1.4 yourself; there is no way around
it. For each claimed ruling you answer one of *I said that*, *I did not say that*, or *I do not
remember*. You are not being asked to make new decisions and should push back if the list tries to make
you. You sign off the phase by agreeing the register is a fair record of what you answered.

**What blocks it.** Nothing. It can start immediately.

**Size.** Six slices. Roughly three working days of agent time, of which your own time is under two
hours and is confined to slice 1.4.

**Going wrong, as distinct from slow.** Slow looks like the commit walk taking four days instead of two
because the diffs are large. Going wrong looks like either of these. The register keeps growing each
time somebody re-reads the same material, meaning the extraction rule is not written tightly enough and
the inventory has no bottom. Or you answer "I do not remember" to most of the walk, which means the
trace question cannot be answered from memory and those decisions have to be re-made in phase 2 rather
than recovered here. The second is not a failure of this phase — it is a finding, and it gets reported
as one rather than worked around.

### Slice 1.1 — Walk the commits and build the register

Walk `342e66db..327364f8`, the range from the specification freeze to the last commit before phase 0.
Verified at HEAD: **83 commits, 73 touching the old working folder, 13 touching the canonical documents,
and zero touching `src/`.** The "roughly seventy" figure in the signpost is the 73.

For each commit, read the message and the diff and record every decision it asserts. A decision is
anything the commit treats as settled going forward — a ruling, a lock, a supersession, a count that
later work depends on, a rejection of an alternative. Record it as a row:

| Column | Content |
|---|---|
| Commit | The hash |
| What it says | One sentence, plain, in the terms the language uses |
| Where it landed | File and line at HEAD, or "not carried anywhere" |
| Verdict | traced / contradicted / no trace |
| Quotation | The words of yours that support or contradict it, with source and line — empty only when the verdict is *no trace* |
| Your mark | Filled in slice 1.5, not here |
| What depends on it | Which later phase would have to change if this were reversed |

**Two classification axes, kept apart.** The *verdict* is documentary: it is determined by evidence and
needs none of your time. Your *mark* is authority: only you can give it. They are different columns and
they must not be collapsed. A row can be traced and not yours — an agent inferring correctly from
something you wrote is not the same as you having ruled, and treating the two as equivalent is precisely
the assumption this phase was created to test.

The three verdicts are the only ones permitted. "Probably his" is *no trace*. "Consistent with his
intent" is *no trace*. The register file gets a check that fails on any value outside the three.

The only sources that can produce a *traced* verdict are documents you wrote yourself — the want
document is the main one. Agent-written documents on this branch cannot produce a *traced* verdict no
matter what label they carry.

Delegable in batches of ten commits with a single fixed row format. Batches are merged and de-duplicated
by commit hash plus decision text.

**Done when**: every hash present, every row has exactly one verdict, every traced row has a quotation,
and the independent re-walk of ten commits has run. Roughly a day and a half.

### Slice 1.2 — Pull out the decisions that live only in a document

Some decisions were never asserted in a commit message — they were written into a document and left
there. These are the ones a disposal would destroy, which is why this slice runs before any of it.

**Commit the counting commands and their output first, as a file, before reading anything.** Then
"unhandled = 0" is a join against a committed list rather than a claim about a number nobody else has.
The counts below were taken at HEAD on 2026-07-25 and are given so the commands can be checked, not so
the numbers can be trusted:

| Sweep | Command | Count today |
|---|---|---|
| Structured, top-level only | `grep -Ec '^#{2,4} *Decision' docs/Working/*.md` summed | 103 |
| Structured, recursive | `grep -rEc '^#{2,4} *Decision' docs/Working --include=*.md` summed | 303 |
| Of which inside `Archive/` and `Superseded/` | same, those two folders | 200 |
| Prose rulings, top-level | `grep -Ec 'rul(ed\|ing).*20(25\|26)-[0-9]{2}-[0-9]{2}' docs/Working/*.md` summed | 145 |

**Scope: top-level markdown only, `Archive/` and `Superseded/` excluded.** Those hold decisions that
were already disposed of once, and re-adjudicating them is not what this slice is for. The exclusion is
stated here so a reader who runs the recursive count and gets 303 knows why.

An earlier draft of this plan cited 93 prose ruling lines. That figure does not reproduce from any
pattern tried, and the pattern above gives 145. Recount before sizing.

Every match gets read and either added to the register or explicitly dismissed with a reason. Rows from
this slice use the same columns and the same three verdicts as slice 1.1, and carry their source
document.

**Two extractions must not be missed**, because later phases consume them directly:

- The correction list for the post-mutation sweep — 25 rows with per-file line numbers, already verified
  against HEAD, and not one row executed. It is phase 2's starting point and it is copied out in full.
- The phase 0 caveat register, which phases 9, 10 and 11 all work from.

**Done when**: the counting commands and their output are committed; every match is either a register
row or a dismissal with a reason, unhandled = 0; the correction list is present in this folder in full;
the caveat register is present. **Size depends on the recount** — at 103 structured plus 145 prose it is
closer to two days than one, and the slice is re-sized once the commands have run. This is the slice
that cannot be recovered after disposal, so it does not get short-changed.

### Slice 1.3 — Prepare the conversation

This slice protects the one resource the project cannot buy more of. It is judged on how short it makes
the conversation, not on how complete the list is.

**Filter, but sample what you filter.** Rows verdicted *contradicted* are removed — they go straight to
phase 2 as contradictions to resolve, not to you as questions. Rows verdicted *traced* with a verbatim
quotation are removed as already answered, **except for ten of them, chosen at random, which go into the
walk anyway.** The sample is not padding. The traced verdicts are agent work, agents in this project
have a measured record of manufacturing owner rulings, and removing every traced row would hand those
same agents the decision about which claimed rulings you never see, with nothing at all checking the
classification. Ten rows cost a few minutes of the sitting and turn an assumption into a measured error
rate. **If more than one of the ten is wrong, slice 1.1 is redone before the conversation continues.**

Everything verdicted *no trace* goes into the walk in full.

**Then rank.** Order what remains by what depends on it, using the register's dependency column, so that
a sitting truncated at twenty minutes covered what phases 3 through 7 rest on.

**Then compress.** Each item is one line, in a fixed format:

> **N.** *[one sentence, plain English, no project vocabulary]* — first appears `<hash>`, <date>.
> If this is not yours, <the named phase> changes.
> **☐ mine ☐ not mine ☐ don't remember**

No item may run to a second line. A claim that cannot be stated in one sentence is several claims and
gets split before the sitting, not during it.

**Group.** Items that stand or fall together get one heading and one mark, so you answer once for six
related counts rather than six times.

**Size the sitting.** At most 40 items, no sitting longer than 45 minutes, at most two sittings. If the
filtered list comes back above 60, the filter was not aggressive enough and this slice is re-run
**before any of your time is spent at all**. Put the count at the top of the prepared file.

**Nothing in the walk is a new decision.** If working through an item shows it is not a question about
the past but a genuinely open question about what to build, it comes out of the walk and becomes its own
entry in "Waiting on Shane" with what is stalled until you answer. Mixing the two is how this project
has repeatedly spent your attention on things that were already settled.

**Done when**: the prepared file exists, item count is at or below 40, every item is one line, every item
carries the three boxes, ten sampled traced rows are included, ordering matches the dependency column.
All checkable by reading the file. Half a day.

### Slice 1.4 — The walk itself

Plain conversation, one item at a time, no modal prompts and no batching you into a form. You give one of
the three marks. Where you say *not mine*, one follow-up: decide it fresh, or drop it. Where you say
*don't remember*, it is recorded as unresolved and becomes an input to phase 2 — the conversation does
not convert it into a decision.

Record your marks as you give them, in the same sitting. Do not reconstruct them afterwards from notes.

**Done when**: unmarked items = 0. Under 90 minutes of your time, across at most two sittings.

**Going wrong looks like** the sitting running past two hours, or rows arriving that need you to
re-derive an argument before you can mark them. Either means slice 1.3 did not compress enough, and the
right response is to stop the sitting rather than push through.

### Slice 1.5 — Fold the marks back

Every walked item's mark goes into its register row. Rows you marked *not mine* and asked to drop are
struck through rather than deleted, with the mark recorded, so a later reader sees the decision was
considered and removed rather than lost. Rows marked *don't remember* are collected into a short list at
the end of the register — that list is a direct input to phase 2, and phase 2's finish line accounts for
every one of them.

If any of the ten sampled traced rows came back *not mine*, record the count. More than one and slice 1.1
is redone.

**Done when**: every walked item appears in the register with its mark; the unresolved list exists and its
count matches the number of *don't remember* marks; the sample error count is recorded. Two hours.

### Slice 1.6 — Dispose. **This does not block the phase.**

**Phase 1 is finished when 1.5 commits.** Nothing downstream consumes the disposal — everything after
phase 1 reads the register, the want document, the caveat register and the correction list, all of which
arrive in 1.1 and 1.2. The disposal runs whenever convenient after that.

**Ruled 2026-07-26: the folder stays until the reset is on solid footing, then everything moves into
`docs/Working/Archive/` or `docs/Working/Superseded/`.** Not now, and not piecemeal.

**Solid footing means phase 8 confirming rather than reverting.** That is not an arbitrary trigger:
while phase 8 can still return "revert", the old folder is what would be reverted to, so archiving it
before then destroys the fallback the whole branch strategy rests on. When phase 8 confirms, the bulk
move becomes safe and is the first thing to do after it.

Until then this slice does two things only — the deletion in step 1, because that bundle's method and
findings are already extracted and it carries status claims that are simply false, and the marking in
step 4, because a document that presents itself as authoritative is a live hazard to any agent that
wanders in. **Steps 2, 3 and 5 wait for phase 8.**

In this order, and nothing is deleted, moved or retired until what it holds is in the register:

1. **Delete the abandoned cell bundle** — 62 files, 7.9 MB, untouched since 2026-07-16, and
   measurement-incapable: its scoring script points at a runner path that has never existed, so its
   claimed zero-false-positive result was hand-written rather than produced by a run. Before deleting,
   record its tree object `5f709ebada28c0e58302026f80e6685606300c90` in the register. Verified today:
   `git cat-file -t` resolves it, so the deletion is restorable exactly.

   **Two things were extracted out of it first, on 2026-07-25, and they stay.** The method that produced
   its block accounting is recovered as `docs/working-reset/phase-3/block-inventory.workflow.js` — the runnable
   workflow, not a description of one — with `docs/working-reset/phase-3/block-inventory-method.md` alongside it.
   The eleven items its nine agents flagged, five of them contradictions inside the canonical documents,
   are in `docs/working-reset/phase-2/canon-contradictions-found-2026-07-14.md`. One of those five is still live
   at HEAD. Nothing else in the bundle was extracted, and the recorded tree object is the only route back
   to the rest of it — including the 3,446-cell draft corpus at `evidence/recovered-corpus.json`, which
   exists nowhere else.
2. **Freeze the matrix cells** — 107 files, confirmed. Record tree object
   `bd03426822bd29dee62a12ea0b49c3636dc9af23`, also verified to resolve, and write a one-line marker at
   the folder root saying they are frozen, that phase 7 regenerates the grid, and that no cell in them is
   citable. The matrix's six ways of slicing the problem stay worth reading; its contents do not. Freezing
   is not editing.
3. **Retire the old status page into this file.** Anything in it still true and still tracked becomes a
   row on the board or a row under "Ruled". The disposal record lists the status page's headline items,
   each with the tracker row it became or the word "dropped" — otherwise nothing checks that anything was
   carried over. Then delete it. There is no second place status is tracked after this commit.
4. **Mark the misleading documents before moving them.** The decision index is the dangerous one: its own
   header tells an agent it is the one-page lookup for "is this already decided?" and instructs them not
   to re-derive it, it carries no superseded marker, and it still asserts the pre-want-document compiler
   and runtime split as settled. Any agent told to check what is decided lands there first. Put a marker
   in its first three lines before moving it. If step 1 deleted the bundle, its two false status files
   went with it — record which happened.
5. **Move the dead ones** into the archive folder. Dead means superseded by something in the register, or
   unreferenced by anything alive. Bulk move, one commit, no individual review; slices 1.1 and 1.2 have
   already taken everything out of them.

**Done when**: `git cat-file -t` resolves both recorded tree objects after the commit; the marker files
exist or the record says they were deleted instead; the old status page no longer exists at HEAD; the
disposal record accounts for every status-page headline item; and `grep -rn "docs/Working/"
docs/working-reset/` returns hits only in the disposal record, in this file, and in the three extracted
files named in step 1. Half a day.

*Why the last check has three exceptions rather than none.* Slice 1.2's counting-command rows and the
"Waiting on Shane" row about the folder both name `docs/Working/` and always did, so the condition never
held as first written. The three extracted files name it because each has to say where it came from, and
a provenance line that cannot name its source is not provenance. What the check is actually for is
catching a live dependency on a deleted folder, and none of these is one.

**Going wrong looks like** the disposal growing an argument about whether a document is dead. If two
people can disagree about it, mark it and move on — that is what step 4 is for. Folder cleanup is
hygiene, not progress, and it must not eat a morning.

---

## Phase 2 — Settle every place canon and the want document disagree

**Goal.** Find every place where the canonical documents on this branch and the want document say
different things, resolve each in writing with a reason, and write down the search procedure so it can be
run again.

**What it produces.** A contradiction register: one row per disagreement, holding the canonical text with
file and line, the want document's text with line, the resolution, the reason, and the commit that
executed the resolution into canon. Plus a written, re-runnable search procedure — specific enough that a
different person running it reaches the same list.

**Done when.**

1. Every row has a resolution and a reason, both non-empty. Check: rows missing either = 0.
2. Every row names the commit that executed it. Check: rows with no commit = 0; every named commit
   resolves.
3. The superseded wording no longer appears in the canonical tree. Check: for each row, the grep pattern
   recorded in that row returns zero hits under `docs/language`, `docs/compiler`, `docs/runtime` and
   `docs/philosophy.md`. **The pattern is written by the same person doing the correction, so this is
   self-refereed** — it catches a forgotten file, not a resolution done wrong.
4. The search procedure, re-run at HEAD by somebody who did not write it, produces no disagreement that
   is not already a row. Check: run it, diff the output against the register.
5. Every *don't remember* item from phase 1 is disposed of — resolved as a row, or explicitly recorded as
   still open with what is stalled. Check: undisposed = 0.

The known starting point is the 25-row correction list phase 1 moved here, all rows verified against HEAD
and none executed. Its largest single item is the post-mutation sweep: the want document rules that once
the compiler has proven every rule the runtime does not re-evaluate them, and the specification,
philosophy, runtime API document, evaluator document and architecture document all still describe the
sweep. That list is a starting point, not the answer — this phase's own search runs and its output has to
be a superset.

**A second input**: `docs/working-reset/phase-2/canon-contradictions-found-2026-07-14.md`, extracted from the cell
bundle before phase 1 deleted it. Eleven items flagged by nine agents that walked the canonical documents
block by block. Five are places where a canonical document contradicts itself rather than the want
document — a different kind of disagreement from this phase's main one, but the same repair. **Four of the
five were ruled on 2026-07-14 and the documents corrected in `90f0792f`; this phase does not re-litigate
them.** `90f0792f` is inside phase 1's commit walk, so those four arrive here with a documentary verdict
already attached and the "ruled" label does not have to be taken on trust. They are recorded so a reader
who meets the same text knows it was settled. The fifth was never ruled and is still live at HEAD, and
the file records how a summary dropped a hedge and left it unseen for eleven days. The remaining six are
not contradictions and several belong to later phases; each says which.

**Honest note on completeness.** Nobody can prove this list complete and this phase does not claim to.
What is checkable is that the procedure is written down, that it was run, and that re-running it finds
nothing new. That is a weaker claim than completeness and it is stated as the weaker claim.

**What it may cite.** The canonical documents at HEAD on this branch; the want document; the phase 1
register. Not the catalogs, not the samples, not compiler behaviour.

**What you sign off, and what you are looking for.** Two things. The philosophy edits — the sentence about
what the runtime does on an operation, and the composition seam that the want document's mechanism leans
on and which the philosophy does not currently describe. Philosophy edits require your approval by project
rule, and the second is a genuine two-sided choice rather than a consequence of a ruling you already made.
And the register as a whole: you are looking for anything resolved in a direction you did not intend.

**What blocks it.** Phase 1, entirely. Resolving a contradiction before knowing which side you actually
took is how the last two months were spent.

**Size.** Two to three working days. The starting list is 25 rows; the search is expected to find between
10 and 30 more, on no firmer basis than the shape of the correction list. Note that executing a resolution
means editing the specification, not just recording a verdict.

**Going wrong, as distinct from slow.** Slow is a long register. Going wrong is the search procedure
finding a materially different set each time it runs — that means it is not written tightly enough to be a
procedure, and no amount of running it converges. Also going wrong: a resolution that requires a new
language decision. That is a design question, not a contradiction, and it goes to "Waiting on Shane"
rather than being resolved inside this phase. And: a resolution that changes the want document to agree
with canon. The want document is the thing with authorship behind it; if it is wrong, that is yours to
say.

---

## Phase 3 — Write the corrected statement of the language, and build the catalog drift check

**Goal.** State what you can write in a `.precept` file, completely, with every normative statement
numbered and every list in a form a program can read — and make the build fail when a list and its catalog
disagree in either direction.

**What it produces.**

- The corrected canonical language documents on this branch, every normative statement carrying a stable
  number.
- A machine-readable form of every list the language declares — keywords, types, operators, modifiers,
  constructs, expression forms, actions, operations, **and write sites** — as data files the build reads.
- A build check comparing each list against its catalog, with two committed tests demonstrating it fails
  in both directions.
- A join between the numbered statements and the data-file entries.

**A decision this phase has to make and state, because nothing settles it today.** The build can only
compare two machine-readable things, so the check it delivers is *data file versus catalog*. The drift it
does not check is *canonical document versus data file* — and that hand transcription is where a missing
member actually enters. There are three possible arrangements and they cost very different amounts:

- The document is generated from the data files. Then the data file is where the language is written, the
  prose is output, and this phase is a rewrite of how the canonical documents are produced.
- The data files are extracted from the documents by a committed extractor. Then the extractor is a
  produced artifact of this phase, and its failure to parse a normative statement must fail the build
  rather than skip the line.
- The data files are hand-maintained beside the documents. Then nothing checks the documents at all, and
  this phase's closure claim is false — the language would be closed against a hand-copied list, which is
  the arrangement the branch decision explicitly rejected.

**The phase entry must name which, and it becomes a finish-line condition.** Condition 5 below is written
for the second arrangement, which is the only one that both keeps prose canonical and closes the seam; if
a different one is chosen, rewrite the condition to match.

**Done when.**

1. `dotnet build` fails when a member exists in a catalog and not in its list. Check: the committed test
   that adds one, shown red, then removed.
2. `dotnet build` fails when a member exists in a list and not in its catalog. Check: the same, in the
   other direction. **This is the direction nothing in the repository currently checks** —
   `Precept0007GetMetaExhaustiveness` and `Precept0026CatalogDUCompleteness` both quantify over the
   members an enum or a `[CatalogDU]` hierarchy declares, so both are structurally silent about a member
   that was never added. This test is the phase's main deliverable, and it is also the exit condition for
   the two "re-check, do not delete blind" rows on the phase 0 caveat register.
3. Every normative statement in the canonical language documents carries a unique number. Check: a script
   counts normative statements and numbers, and the two counts match with no duplicates. **Honest note:
   what counts as a normative statement is a judgement, and the script applies the same rule the person
   numbering applied, so it confirms consistency with itself and nothing more.** That judgement has to be
   written down before the numbering starts, because phases 6 and 10 are both sized off the resulting
   count.
4. Every list has a machine-readable form, and the enumeration of *which lists* is itself derived rather
   than hand-written. Check: derive from the catalog side, where `CatalogDUAttribute` already exists, so a
   new catalog with no data file fails the build. A hand-maintained list of the lists is the same failure
   one level up.
5. Every data-file entry names the numbered statement it comes from, and every numbered statement that
   declares a member of a list is joined to at least one entry. Check: a script joins the two; statements
   declaring a member with no entry = 0, entries naming no statement = 0.
6. The code hierarchies agree with the declared list of expression forms. There are three candidates in
   the repository — `ExpressionFormKind` (15 members, verified), the `ParsedExpression` hierarchy, and the
   `TypedExpression` hierarchy. Discharge walks typed expressions, so totality over one is not totality
   over the other. Check: a declared form with no typed representation = 0.
7. The claim count is recorded on the board. Phases 6 and 10 are sized off it.

**What it may cite.** The output of phase 2. The catalogs may be *read* here — this phase is where they get
measured — but a catalog is never the reason a list has a member. The list comes from the canonical
documents; the catalog is what gets checked against it.

**What you sign off, and what you are looking for.** The statement of the language itself. You are looking
for anything it now says that you did not intend it to say, and anything it used to say that has quietly
gone. You also sign off the closure, because from the end of this phase the language stops moving.

**Expect one reopening from phase 5, and that is the rule working.** The most likely discovery in phase 5
is a form or a property this statement missed. Two reopenings, on different grounds, is this statement not
being finished.

**What blocks it.** Phase 2.

**Size.** One to two weeks, and it is the largest phase before the build. Part of that number is now
known and the rest is still a guess, so the two are separated here.

*Known.* Numbering the normative statements means first walking the canonical documents and accounting
for every one of them, and there is a recovered method that does exactly that:
`docs/working-reset/phase-3/block-inventory.workflow.js`, with
`docs/working-reset/phase-3/block-inventory-method.md` explaining what it does and does not establish. It ran on
2026-07-14 across nine document regions and accounted for 888 blocks in 22 minutes of wall clock across
ten agents. Two things have to be decided before it is re-run and they are named in the method file. It
is also worth knowing that it reproduces the block accounting but not a cell corpus — it completes one it
is handed rather than producing one — which matters if a later phase wants the cells rather than the
numbering.

**Prefer its successor.** `docs/working-reset/phase-3/enumeration.workflow.js` does the whole job in one run — it derives the block partition in the script rather than asking an agent for a count, collects escalations as typed objects so a summary cannot swallow one, writes its report before it throws, and produces the cells rather than completing a corpus it was handed. It has been fixed against three adversarial reviews and exercised against a stub harness in six failure modes, but **it has never been run for real**, which is why the 2026-07-14 workflow is kept beside it: that one ran. Its head comment names seven places it still needs a person.

*Still a guess.* The weeks are for the other two pieces: rewriting the canonical documents to match the
want document, and building the drift check. For scale: `docs/language/` is 11,248 lines and
`docs/compiler/` is 9,967, and a crude count of lines carrying *must*, *shall*, *never*, *cannot*, *is
required* or *may not* gives 357 and 189 respectively — a lower bound, since one line can carry two
claims and many claims avoid those words. On top of the corrected prose there is the extraction, the
build check across nine lists, and the join. **The cheapest fix available: take the claim count now,
before phase 1 starts.** It is about an hour, and it is the input that makes phases 3, 6 and 10 all
sizeable for the first time.

**Internal checkpoint.** Once the normative statements are numbered and before the machine-readable lists
are built, record the claim count on the board. If the count is **above 800**, stop and re-cut the phase
before the list work starts rather than after. The number is chosen, not measured; it is roughly one and a
half times the crude lower bound above.

**Expect that threshold to fire.** The 2026-07-14 bundle cited **2,205 distinct spec lines** across six
of these documents — 519 in the language spec, 442 in collection types, 440 in business domain types,
377 in temporal, 343 in primitive types, 84 in the proof engine document — measured by deduping the two
path formats its cells used. A cited line is not the same thing as a normative claim: some lines carry
none, some claims span several, and that pass wrote multiple cells against one location. But it is a
count taken by readers working through the documents rather than a grep for *must* and *shall*, so the
real figure is somewhere between the 546 lower bound and 2,205 and is likely much nearer the top.

That has two consequences worth deciding before this phase starts rather than a week into it. The
one-to-two-week size is optimistic by more than the margin it admits. And if the count lands near
2,205, phase 10 is not five hundred tests but something closer to two thousand, which changes that
phase's shape as well. The cheapest way to settle both is the claim count named above.

**Going wrong, as distinct from slow.** Slow is a lot of prose to number. Going wrong is a list that will
not close — members still being added while the check that closes it is being written. If that happens
twice, stop and ask why the list has no natural bottom, because the same property is what made the earlier
definition effort unfinishable. Also going wrong: the numbering turning into rewriting. If a statement has
to be reworded to be numbered, that is a phase 2 contradiction that was missed, and it goes back rather
than being settled here.

---

## Phase 4 — Draft the certificate step list and count it

**Goal.** Find out early whether the remaining judgement is a couple of dozen careful arguments or
something much larger.

**What it produces.** A dated **draft** list of certificate step kinds, each with a one-line description
of what it asserts, and its count — emitted in the same machine-readable form phase 3 established, so
phase 9's drift check compares a file that already exists rather than one it wrote itself.

**This list is not closed here. It closes at phase 5.** Ruled 2026-07-26. Step kinds are what the
discharge procedure emits, and that procedure is not written until phase 5 — so closing the list now
would guarantee the closed-list rule fires the first time phase 5 needs a step nobody thought to draft,
halting everything downstream on a schedule this plan set for itself. Phase 4's value is the count, and
a draft can be counted.

**But the draft is frozen for counting.** Between the moment the count is recorded and the moment phase 5
opens, the list does not change. Otherwise "we will close it at phase 5" becomes how a list stays open
forever, and the count you ruled on stops describing the list you have. An edit in that window is a
process violation and the count has to be retaken.

**Done when.**

1. The list exists, carries a date, and both counts are recorded on the board (see the guard below).
2. **You have recorded one of two words beside them — *carry on* or *re-cut* — with the date. Until that
   word is on the board, phase 5 does not start.** "Then Shane looks at the number" is not a finish line:
   it cannot be checked by someone who did not do the work, and nothing prevents phase 5 starting the next
   morning.

**A guard against a thin draft.** The count is only meaningful if the list is complete, and completeness
of the step vocabulary cannot be verified at phase 4 — the obligation families that would make it
derivable do not close until phase 5. So a short list can be short because the drafting was thin, and the
plan would then read the thin draft as permission to continue. **Two agents draft the list independently
from the same inputs, and both counts go on the board. If they differ by more than a third, the vocabulary
is not stable, the number is not a measurement, and the drafting is re-run before you are asked
anything.** This costs a few hours and it is the only thing found that turns "is the list complete" into
something checkable at phase 4.

**No numeric threshold — ruled 2026-07-26.** A number here would be theatre. Thirty against thirty-one
is not a distinction, and the twenty-one already designed were counted over the *existing* compiler's
discharge paths rather than over the phase 5 walk, so measuring a new list against that number is a coin
toss with a figure written on it. This stop is a judgement call and it is yours by definition — it is not
"is the reasoning sound", it is "is this more work than I am willing to do", which nobody else can answer.

**What makes the judgement real is what you are handed, not a threshold.** A bare count cannot support it:
thirty step kinds of which twenty-five are interval arithmetic, comparison and catalog lookups is an
afternoon each and a different proposition entirely from thirty of which half are novel. So phase 4
delivers the count **and**, per step kind, two marks:

- **Recovered or invented** — was this step kind read off a discharge path the compiler already has, or
  is it new? A recovered step has working code to argue against; an invented one does not.
- **Afternoon or fortnight** — a rough read on what arguing it soundly would take. The measured rate here
  is that arguments which held went about twelve adversarial rounds and ones written in a single pass
  fell within hours, so this mark is the one that decides phase 6's size.

You then record *carry on* or *re-cut* with the date. That is the stop.

The drafters are told to make both marks and are not asked to total them or to recommend anything. Two
independent drafters, so the list is not one person's segmentation.
**What it may cite.** The closed language statement from phase 3; the want document; the canonical
compiler documents on this branch; **and the phase 1 register.** The earlier step-list design is among the
things phase 1 disposed of, but a step-list membership decision is a `## Decision` block, so slice 1.2
swept it into the register with a verdict. It therefore arrives here in exactly the right form — a claim of
unknown authority to be checked, rather than a design to copy. The design document itself stays off
limits.

**What you sign off, and what you are looking for.** The count, and the word. You are looking at one thing:
whether the number is small enough that phases 5 and 6 are a few weeks of careful argument rather than a
different project.

**What blocks it.** Phase 3. The step vocabulary is stated in terms of the language.

**Size.** Half a day to a day, doubled by the two-drafter guard — call it one to two days. Deliberately
cheap, because its whole value is that it is a stop that costs almost nothing to reach.

**Going wrong, as distinct from slow.** This phase cannot really be slow. A large count is not it going
wrong — that is it working, and the correct response is to stop. It is going wrong if it produces an
*argued* list rather than a counted one; arguing belongs in phase 6, and a phase 4 that runs past two days
has quietly become phase 6 with the stop skipped.

---

## Phase 5 — Settle the obligation families and the discharge procedure

**Goal.** Write the discharge procedure as a walk over the expression forms the language allows, so that
every form reaches exactly one case, nothing falls through, and the build proves it.

**Where the fragment stops — the one thing on the "Waiting on Shane" table for this phase.** The procedure
decides sequents drawn from a *subset* of the expression grammar, and where that subset ends is a decision
about what the language admits, not a drafting choice. The grammar has fifteen expression forms, a fixed
precedence table, a closed function catalog with no extension point, and a closed type vocabulary — so the
fragment can be defined precisely and the claim becomes "the procedure decides every sequent in this
fragment; anything outside is rejected, not skipped." That is a structural induction one person can finish.

Expect the first honest fragment to be considerably smaller than today's grammar. Your own worked example
needs linear decimal arithmetic, backward substitution, and one non-linear case closed by a guard that
restates the post-state condition verbatim. That is a small fragment, while `contains`, quantifiers, member
access, lookup by key, string functions, temporal arithmetic and qualifier algebra are all in the grammar
and will all appear in real constraints. The gap between those two is the price of the guarantee, and
`what-i-want-2026-07-16.md` already assigns the call to you — the guarantee is not negotiable, the surface
is.

**What survives of the matrix's six ways of slicing the problem.** Settled 2026-07-26 from an independent
read that was deliberately not shown this plan. They are not the structure — the expression-form walk is.
They are not one kind of thing either: three describe where obligations come from, three describe what the
proof has to work on, and multiplying them together is what produced an enumeration with no bottom. Once
an obligation exists the prover cannot see which write site made it, so most of the product means nothing.

- **Obligation family** — not an axis. Establishment and preservation are the base case and the step of one
  induction, differing only in whether the inductive hypothesis is in the premise set: one flag on the
  obligation. Fault prevention differs in the *goal*, not the premises.
- **Constraint kind** — five catalogue-enumerated members that compute exactly one thing, the set of
  operations a constraint attaches to. A function, not five case shapes. Carries forward one open question:
  whether "the entity is resident in S" is itself usable as a premise.
- **Condition shape** — replaced by the grammar. But the four validity arguments already written under it
  are statements about *formulas* that were mislabelled as statements about constraints; re-home each as a
  named derivation rule keyed on the normalized goal's shape rather than discarding them.
- **Write-site category** — **keep, and correct it.** The matrix lists five writers; `precept-language-spec.md`
  lists eight and names three the matrix misses: the `into` target of `dequeue`/`pop`/`dequeueBy`, the `omit`
  reset on state entry, and the update-patch group. The spec says a writer off that list produces a fact
  surviving a write it should not, and there is a confirmed defect that is exactly that. Dropping this axis
  costs soundness.
- **Read-set shape** — dropped. Whether a pre-state fact is available is the *output* of backward
  substitution, not a five-valued category, and the earlier version of this axis was already refuted by
  counterexample.
- **Type family** — too coarse. What the prover needs is a per-type table of algebraic properties: exact or
  approximate arithmetic, total order, equality, which qualifier axes must agree, cardinality or length.

**And the organising idea none of the six carried: the route.** All six classify declarations statically. An
obligation attaches to a *path through an operation*, not to a piece of text — and the language already
supplies the path, closed and ordered, as the spec's nine-phase execution order. Frame, fact survival and
premise availability all fall out of substituting along that path. This is not abstract: the guard-match
validity argument was found false because state exit actions run before the row's action chain, and its
counterexample compiles clean at HEAD, reports **Proved**, and divides by zero.

**What it produces.**

- The obligation families.
- The discharge procedure as an exhaustive walk, and the build check that proves it total.
- **The closed rule list, emitted in phase 3's machine-readable form**, so phase 9's drift check has a
  file to compare.
- **The definition of what one argument covers, and the resulting item count.** Phase 6 cannot be sized
  without this, and it must not be phase 6 discovering it.
- **The grid's axes** — which lists are crossed, and why those and not others.
- **The specification of the certificate checker**: what it walks, what it asks the catalog for each thing
  it finds, and what it does when the certificate does not cover something.
- The induction argument.
- A cost measurement.

**Done when.**

1. Every expression form declared in phase 3's lists reaches exactly one case. Check: a green build. The
   mechanism already exists and should be reused rather than forked — `HandlesCatalogMemberAttribute` plus
   `Precept0019PipelineCoverageExhaustiveness` already implement "this class claims to handle a catalog
   enum exhaustively, so every member must have a handler or the build fails", and
   `Precept0025CatalogDUWildcard` already forbids wildcard and default cases on `[CatalogDU]` switches.
   Marking the walk's discriminant gets conditions 1 and 2 from analyzers that ship today. Note that
   `TreatWarningsAsErrors` is set in exactly one project file, `src/Precept/Precept.csproj` — name the
   project the walk lives in, or the exhaustiveness warning is only a warning.
2. Nothing falls through. Check: no default case; a default case fails the build.
3. Every case either derives from a named premise or refuses with a stated message. Check: a script over
   the walk reports cases doing neither; count = 0. **What is checkable is the presence of a premise or a
   message, not whether the derivation is valid.**
4. Each case either refuses by construction for everything it does not handle, or names the properties it
   splits on and closes that split the same way, recursively. Check: the same script, one level down.
5. **The induction argument exists and names five things explicitly**: state residency, computed fields
   recomputed on every operation, `omit` resets on entry, entry and exit chains, and first-match rows.
   **This condition is presence-only and says so.** A document that mentions five things and proves
   nothing satisfies it, and this is the finish line where the distance between *checkable* and *true* is
   largest — if the argument is wrong, phases 5 through 7 produce nothing. The gap is closed at no extra
   cost by condition 8 and by entering the argument in phase 6's register as an entry like any other, so
   it carries the same requirement of recorded attacks.
6. A cost measurement exists, taken before the strategy set closes. The specification makes not breaking
   single-file recompile a condition of a strategy being allowed, and a pass over every rule crossed with
   every write site is a different cost class from what the corpus costs today. **The phase must state
   which form the measurement takes**: at phase 5 the procedure is a written specification, so either this
   is a cost model computed from the procedure's shape — rules crossed with write sites, against a
   measured per-operation cost, with the inputs named and the arithmetic in the file — or the phase builds
   a prototype it does not currently say it is building. **Take the before-and-after baseline in the same
   run rather than inheriting a figure**; this project's records carry both about 44ms and about 107ms for
   a full corpus compile, and neither was re-measured for this plan.
7. Not signed off in the session it was written. Check: the sign-off commit's date is later than the
   authoring commit's. This is stricter than the rule as stated — it forces a calendar day, not a session —
   and that is deliberate, so nobody argues the check is wrong when it fires at 11pm.
8. The induction argument records at least **three** attempts to break it, by agents given the goal of
   breaking it rather than reviewing it, each recording a concrete situation it tried — a definition, a
   sequence of operations, and the claimed wrong outcome — and whether it succeeded. Five soundness
   arguments written here with no adversarial rounds all fell in one review pass; the two that held took
   twelve rounds each. Phase 5 otherwise budgets zero.

**What the build result proves, and what it does not.** It proves that no expression form is unhandled. It
proves nothing about whether a case is right, and nothing about combinations of properties inside a nested
expression — a binary operation whose left side is a member access on a computed field inside a state with
an entry chain is one case of the switch and an unbounded family of situations. What killed the previous
attempt was exactly that: the shapes a rule's condition can take are independent properties rather than
alternatives. This phase converts an unbounded enumeration into a bounded dispatch plus fifteen recursive
cases whose correctness is argued by hand in phase 6. That is a genuine improvement and it is claimed as
exactly that. **The build check removes the "have we listed them all" question. It does not remove the hard
part.**

**A trap to avoid when quantifying over write sites.** The compiler already has a notion of write sites —
`src/Precept/Pipeline/GraphAnalyzer.FieldWriteSites.cs` collects them, and a comment near the top of
`GraphAnalyzer.cs` enumerates three for the stateless case. That is working code computing an answer, not a
closed list derived from the language documents, and the evidence rules forbid reading current compiler
behaviour as the specification. The list closes at phase 3; this phase quantifies over it and does not
re-derive it from that file.

**What it may cite.** Phase 3's language statement and lists; phase 4's step list, as a draft; phase 2's
resolutions. Not the frozen matrix cells and not any of the disposed designs.

**What you sign off, and what you are looking for.** The discharge procedure, on a different day from the
one it was written. You are looking for a case you can point at for a form you care about, a refusal
message you would be willing to show a user, and an induction argument that is an argument rather than a
restatement of the promise.

**What blocks it.** Phases 3 and 4.

**Size.** About a week, hard-capped at three attempts. This is where the risk lives — it is the same work
that has already failed three times under a different arrangement, and the three-attempt rule exists
because nobody had it then.

**Going wrong, as distinct from slow.** Slow is a lot of expression forms, and it looks like the same
objection coming back sharper each round. Going wrong is three attempts each failing on a *different*
problem — most reviewers passing the revision and one rejecting it, a different corner each time. That is
not progress narrowing on a problem; it is a model that keeps having somewhere else to be wrong, and it
triggers the rule. The other going-wrong sign is this phase needing to add or remove a member of the
language lists phase 3 closed. One such reopening is expected and is the rule working; two, on different
grounds, means phase 3 was not finished.

---

## Phase 6 — Write one argument per discharge rule and per step kind

**Goal.** Every discharge rule and every certificate step kind has a written argument for why it is
truth-preserving, and something mechanical tries to break each one.

**What it produces.** An argument register — one entry per rule and per step kind, holding the argument,
its author, and the attacks made against it with their authors and dispositions. **And the bounded
counterexample search**, numeric scalars first, running on every commit.

**Done when.**

1. Zero rules without an argument. Check: a script joins phase 5's rule list against the register;
   unmatched rules = 0.
2. Zero arguments naming no rule. Check: the same join, other direction; unmatched arguments = 0.
3. Every argument records attacks by at least two attackers other than the author. **An attacker is a
   separate session with no access to the authoring session's context, given the argument and told to
   break it.** With one owner and everything else agent-written, "two people" is otherwise uncheckable and
   trivially satisfiable by two prompts in the same session. Check: entries with fewer than two distinct
   attacker session identities = 0, and each attack's recorded date is later than the authoring commit's.
4. **Every recorded attack names a concrete attempted counterexample** — a definition, a sequence of
   operations, and the claimed wrong outcome. Attacks recording none = 0. Two agents saying "looks sound"
   otherwise satisfies condition 3.
5. **Every recorded attack has a recorded disposition**: the argument was changed, the attack was
   answered, or the rule was withdrawn. Attacks with no disposition = 0. Without this, an argument
   carrying two live unanswered objections passes.
6. **The counterexample search runs over every numeric discharge rule in the closed list and reports.**
   Rules where it finds a counterexample = 0, or the counterexample is recorded against the argument it
   breaks and that argument is withdrawn. Check: the search's output file joined against the rule list.
   This is the strongest mechanical check anywhere in phases 5 to 7, and it is the reason the search is a
   funded deliverable rather than an idea: the prototype found a live unsound discharge on its first run.
   It does not make soundness countable — nothing does — but it turns "we argued and nobody objected" into
   "a machine tries to break it on every commit". Numeric scalars come first because they are about a fifth
   of the known defects and exactly the area being defined.
7. **Every rule a grid cell will cite has an entry here.** Phase 7 checks that a cited rule exists; this
   checks that it has an argument. The previous attempt's cells cited rules that existed and arguments
   that did not.

**What it may cite.** Phases 3, 4 and 5. Nothing else — in particular, no argument may cite the earlier
validity arguments, five of which were refuted the same day they were written.

**What you sign off, and what you are looking for.** Not every argument. You sign off the counts, and you
read the arguments for whichever rules you think are most likely to be wrong. **The counts establish that
attacks happened and that each named a concrete attempt; whether the attempt was a serious try at breaking
the argument or an agreeable restatement is your judgement and cannot be made checkable.** Conditions 4
and 5 narrow that judgement considerably; they do not remove it.

**What blocks it.** Phase 5 — and note that this phase cannot be sized until phase 5 closes, not phase 4,
because its items are step kinds **plus discharge rules** and the rules come out of phase 5.

**Size — unsized until phase 5 closes, deliberately.** The formula is *(number of discharge rules + number
of step kinds) × (cost per argument)*, and neither factor has a value today. The plan does not give an
illustrative number, because an illustrative number is what people remember. What is known:

- **The item count is undefined until phase 5 produces the definition of what one argument covers.**
  Twenty-one step kinds are already claimed. If a discharge rule is one per expression form (fifteen), the
  count is around thirty-six. If it is one per obligation family crossed with form, it is several times
  that and the phase is not attemptable as written. This is why the definition is phase 5's deliverable.
- **The per-argument cost was disputed, and the dispute is not settleable in the abstract — ruled
  2026-07-26.** The measurement everyone works from is that the two arguments that held took twelve
  adversarial rounds each. One reading makes that one to two days per argument, giving five to ten weeks
  at twenty-five arguments. Another reads twenty-four items as two to three weeks, implying half a day
  each. Twelve rounds in half a day is not consistent with the measurement being cited, so the higher
  figure is the safer assumption — but both readings apply one number to an unknown mix, and that is
  why neither can be right. Forty afternoon-sized arguments and forty fortnight-sized ones are different
  projects.

  **So the size is answered by phase 4's marks rather than picked now.** Phase 4 marks each step kind
  recovered-or-invented and afternoon-or-fortnight; phase 5 carries the same two marks onto every
  discharge rule it defines. The estimate is then arithmetic over a known mix instead of one rate over an
  unknown one. Show it when the count arrives: items at each mark, rounds per argument, cost per round.

- **No numeric re-cut threshold — same ruling as phase 4, and for the same reason.** "Above 40 items"
  was a chosen number, and forty against forty-one is not a distinction. What decides whether this phase
  is attempted or re-cut is the total cost implied by the marks, and that is your judgement on a figure
  you can see rather than a row count. Compute it and record it on the board the day phase 5 closes,
  then record *carry on* or *re-cut* with the date.

**Going wrong, as distinct from slow.** Slow is many arguments. Going wrong is arguments falling to attacks
nobody anticipated, with each repair breaking a neighbouring argument — that means the model underneath
them is wrong rather than the writing. Also going wrong: an argument written and reviewed in the same
session, which has never survived here; and arguments that name no rule, which means somebody wrote the
general case because the specific ones were hard. The three-attempt rule applies here as it does to phase
5.

---

## Phase 7 — Generate the coverage grid

**Goal.** Generate the grid rather than writing it, so that no cell can cite a rule that does not exist.

**What it produces.** A generator, its declared input set, and the generated grid file.

**Done when.**

1. No blank cells. Check: a script counts empty cells; count = 0.
2. Every cell cites a rule on the closed list from phase 5. Check: a join against the rule list; citations
   of unknown rules = 0.
3. Every cited rule has an entry in phase 6's argument register. Check: a second join. Condition 2 only
   establishes that the rule exists; this is the one that would have caught the previous attempt's failure
   directly rather than transitively.
4. **The grid's dimensions match the axes phase 5 closed.** Check: compare the generated file's headers
   against the closed axis list. Without this, "no blank cells" is satisfied by any axis choice at all,
   including one narrow enough to be trivially complete — and the thing that killed the previous attempt
   lives exactly here, because a grid is a cross product of properties and the properties are independent.
5. The generator's input set is exactly phase 3's data files plus phase 4's step list plus phase 5's rule
   list and axes. Any other input fails the run. Check: the generator refuses to start on an undeclared
   input. Without this, a hand-maintained side file can feed it.
6. Re-running on unchanged inputs produces an identical file. Check: `sha256sum` before, re-run,
   `sha256sum` after, compare.

**What it may cite.** The closed lists from phases 3, 4 and 5; the arguments from phase 6. The generator
failing for want of a rule is correct behaviour, not a bug to work around.

**What you sign off, and what you are looking for.** That the grid describes the proof engine you want. You
are looking at the refusals — cells where the procedure declines to prove — because that is what the
language will actually reject, and it is the shape of the product. You are not reading the whole grid.

**What blocks it.** Phases 5 and 6.

**Size.** One to two days. Some generation tooling already exists on this branch — roughly 5,700 lines built
to manage the earlier design work — and some will be reusable, but it was built against a different
definition, so assume adaptation rather than reuse.

**Going wrong, as distinct from slow.** This phase is fast or it is broken. If the generator needs a rule
that does not exist, phase 5 is not actually closed and the fix is upstream. One hand-written cell and the
grid stops being evidence of anything, because the property that makes it worth having is that a generator
could not invent a rule. Patching the generator to emit a cell anyway is the exact failure that killed the
previous attempt.

---

## Phase 8 — Confirm, narrow, or revert

**Goal.** Put three counts in front of you and get one word back.

**What it produces.** Three lists with their counts, partitioning the grid:

1. **Code exists that attempts this.** Renamed deliberately. Assigning a cell here means reading the
   compiler and judging, which is the activity the evidence rules prohibit treating as a statement of what
   is correct — the confirmed `nonnegative` defect is a proof that says yes about something false, and
   every cell in this list is a claim of the same form. **No cell in this list is a correctness claim, only
   a code-inventory claim.**
2. **Needs new code**, with a size per row or per group.
3. **Refused.**

**Done when.**

1. The three counts sum to the grid's cell count. Check: recompute. **The sum is checkable; the assignment
   is not.**
2. Every cell in list one names the code by file and the test that shows it working. Cells in list one with
   no named test = 0. This is where uncertainty will collect, because list one is what makes the project
   look nearly done.
3. Every row or group in list two carries a size.
4. **The first count is taken against a named commit** — what the compiler handled at the freeze commit
   `342e66db` — not what it handles the day this phase runs. Phases 3, 5 and 9 add code, so "what the
   working compiler already handles" is otherwise ambiguous, and it is the input to your decision.
5. You have recorded one of three words: *carry on*, *narrow*, or *revert*.

**What "narrow" sets in motion.** Narrowing means shrinking what the language admits until the refusal list
is acceptable. It removes members from lists that closed at phase 3, which under the closed-list rule would
halt everything downstream until you rule — but you have just ruled, so **narrowing is the one permitted
reopening of a closed list and does not need the halt-and-rule ceremony; it *is* the ruling.** The re-run
set: phase 3's lists are reopened by your word and re-closed with the removals; the affected cases in phase
5's walk and the phase 6 arguments belonging to them are retired rather than rewritten; phase 7
regenerates. Phases 4, 6 and 9 are unaffected except by deletion. Phase 10 has not consumed the claims yet,
so the timing is lucky rather than planned.

**What it may cite.** The generated grid; the current compiler, read as a measurement of what code exists
rather than as a statement of what is correct.

**What you sign off, and what you are looking for.** The decision, and it is entirely your judgement. You
are looking at list three and asking whether you are willing to ship a language that refuses those things,
and at list two's sizes to know what carrying on costs.

**This is not a decision to start.** Forking already committed us to trying. Reverting costs the branch's
work, not the product.

**Why the work is on a branch rather than in a copied folder**, which is what makes this phase a promote
or a revert rather than a merge of two parallel trees: the canonical documents cite each other by path
127 times, and the repository already holds 34 live citations of material a rule says never to cite. Two
copies kept apart by a rule in a prompt is the arrangement that has already failed here.

**What blocks it.** Phase 7.

**Size.** Two days to produce the counts with their tests and sizes; about two hours of yours.

**Going wrong.** An earlier draft said this phase cannot go wrong. It can, in two ways. Three lists that
partition correctly but carry no sizes leave you choosing between narrow and carry on with no idea what
carrying on buys — that is what condition 3 is for. And a list one assembled by reading the compiler with
no named tests makes the project look further along than it is — that is condition 2.

---

## Phase 9 — Fill the catalogs with the settled model, and point agents back at them

**Goal.** The catalogs hold the settled model, and adding a member without teaching its consumers fails the
build.

**What it produces.** Catalog entries for the discharge rules, step kinds, obligation families and write
sites; the drift check extended to cover them, with its committed test; the twenty-one
catalog-incompleteness caveats removed; and the instruction files saying plainly that the catalogs and the
MCP tools are the answer for language questions.

**Done when.**

1. Every discharge rule, step kind, obligation family and write site has a catalog member, and phase 3's
   drift check — extended to these lists — passes in both directions. Check: it compares files that already
   exist, because phase 3 closed the write sites, phase 4 emitted the step list and phase 5 emitted the
   rule list, all in the same machine-readable form. **If any of those three did not emit its file, this
   phase would have to write it by hand from a document, which is exactly the drift the check exists to
   catch.**
2. Adding a member to any of them without teaching its consumers fails the build. Check: a committed test
   doing exactly that, shown red then removed. There is precedent to extend rather than invent —
   `src/Precept.Analyzers/` holds 32 analyzers, including `Precept0007GetMetaExhaustiveness`,
   `Precept0011ModifiersCrossRef`, `Precept0013ActionsCrossRef` and
   `Precept0019PipelineCoverageExhaustiveness`. Name which one is being extended so whoever executes starts
   from the right file.
3. **The certificate checker's questions all have catalog answers.** The checker's specification comes from
   phase 5 and says what it asks the catalog for each thing it finds. Check: the checker's walk runs against
   the filled catalogs and reports; questions with no catalog answer = 0. Without this condition, the
   catalogs get filled to whatever shape seemed natural, the checker's walk cannot call them, and phase 9 is
   done twice.
4. **The twenty-one catalog-incompleteness rows on the phase 0 register are removed.** Check: for each row,
   grep the quoted text; hits = 0. **Only those rows.** The register's other two sections belong to phase 11
   and, in one case, phase 10 — see phase 0 above. Two of the twenty-one (`CLAUDE.md:100` and
   `docs/language/catalog-system.md:1285`) describe a real property of the completeness tests rather than a
   phase-0 hedge, and their exit condition is phase 3's build check; confirm that check is in place rather
   than deleting them blind.

**Phase 9's finish line is the guarantee's finish line.** That is the consequence of coverage resting on the
catalogs: the checker walks the definition and asks the catalog what each thing owes, so a catalog missing a
member is a hole in the promise, not untidiness. The alternative arrangement was a checker that only replays
the steps a certificate records, and that catches a wrong proof and never a missing one. Making the catalog
the thing the checker consults is what turns catalog completeness into part of the guarantee rather than a
tidiness goal.

**What it may cite.** Phases 3, 5, 6 and 7. From the end of this phase, and only from then, the catalogs are
citable as a record of what is decided.

**What you sign off, and what you are looking for.** That the twenty-one rows came out to zero and that the
instruction files now say something you are willing to have every agent read as true. You are looking for
anything restored to a flat statement that is still not quite flat.

**What blocks it.** Phases 7 and 8. Filling the catalogs before the model is generated and confirmed puts
the wrong members in — which is also why phase 8's narrowing sits before this phase rather than after.

**Size.** About a week. Note that extending the drift check is itself work inside that week and is easy to
overlook: `CertificateSteps` has no source file at all (`grep -rn "CertificateStep" src/ tools/ test/`
returns nothing), and `ProofRequirementKind` carries no establishment or preservation member.

**Going wrong, as distinct from slow.** Slow is many members. Going wrong is a decision that will not fit
the catalog's shape — the metadata record structurally cannot carry it. Two such gaps are already reported:
the proof-requirement metadata record cannot carry a three-way verdict, and the operator metadata has no
slot for proof requirements at all. Those two were not verified against the code for this plan. They are
declaration-layer changes and no amount of implementation closes them. Also going wrong: a caveat reworded
rather than removed. A caveat still true at phase 9 means the model is not settled, which is a phase 5
problem surfacing late rather than a wording problem.

---

## Phase 10 — Turn every numbered claim into a criterion and a failing test

**Goal.** Every numbered claim has a criterion and a test, and every test traces to a claim.

**What it produces.** A trace file joining claim numbers to test names; the tests themselves, red; a
disposition record for the tests that already exist; and the freeze commit.

**The claim set this phase works from is not phase 3's claims alone.** Phase 3 closes the *language*, and it
closes before phase 5 exists — so it cannot contain a single claim about the discharge procedure, about
which cases refuse, about certificate steps, or about what the runtime does at load time. If phase 3 were
the whole input, all four counts would come out zero over a claim set that never mentions proof, the suite
would freeze, phase 11 would build to green, and phase 12 would confirm nothing dropped — while a compiler
with no discharge procedure, no certificate checker and the confirmed unsound discharge still in place
passed every finish line. **The input is the union of phase 3's language claims, phase 4's step kinds,
phase 5's discharge claims and checker obligations, and phase 7's coverage claims**, all numbered in the
same scheme and the same machine-readable form phase 3 established. The *language* closes at phase 3; the
*claim set* closes at phase 7.

This is also the mechanism by which the certificate checker and the counterexample search get built at all:
phase 11 builds what phase 10 has a red test for, and neither of those two is a statement about what you
can write in a `.precept` file, so phase 3 alone would never produce a test for either.

**Done when.** Four counts, all zero, each computed by a script over the trace file and a test run:

1. Claims with no test.
2. Tests tracing to no claim.
3. Tests not driving the full compile path. Check: the test must go through `Compiler.Compile` and its
   assertion must read proof-stage output — a grep for the type-checker helper entry points, plus an
   assertion that the compile result carries proof output. This project has already produced false greens
   from test helpers that call the type checker directly and never reach the proof stage.
4. Tests red for a reason other than their stated one. Check: each red test's failure message compared
   against the reason recorded in the trace file; mismatches = 0. **A message can match while the test is
   testing the wrong thing, so this is closer than nothing rather than airtight.**

**Plus a disposition step, before the freeze.** There are **4,253** `[Fact]` and `[Theory]` declarations
under `test/` today, verified at HEAD, and no earlier draft of this plan mentioned them. Read literally,
count 2 requires every one of them to trace to a numbered claim or be removed — a very large job that no
size covered. Read charitably as applying only to new tests, the condition is far weaker than it reads and
most of the running suite stays untraced, so phase 12's re-run confirms nothing about it. **Every existing
test is therefore either traced to a numbered claim, rewritten against the corrected claim, or deleted with
a recorded reason, and the three counts are reported.**

That step also matters for phase 11's protection rule. Phase 2 corrects canon where it contradicts the want
document — the clearest case being the post-mutation sweep, which the specification, philosophy, runtime API
document, evaluator document and architecture document all still describe. Tests asserting the sweep are not
tests that go red to green; they are tests that are now wrong, and under phase 11's no-weakening rule they
could not be touched. Disposing of them here is what makes that rule reachable by honest means.

**One orphaned piece of work lands here.** The phase 0 register's third section names a test nobody owned:
compile each sample, assert every `# EXPECT:` header matches an emitted diagnostic exactly, fail on an extra
diagnostic or a missing header. It is a claim-to-test job, so it is this phase's.

**Then freeze** the test set at a named commit and record the hash on the board.

**What it may cite.** The union claim set above; phase 9's catalogs, now citable.

**What you sign off, and what you are looking for.** The four zeros, the three disposition counts, the freeze
commit — and **the second stop-or-continue.** This is the stop the missing size estimate for phase 11 was
agreed against. The red-test count is the first real measure of how much building is left, and the
disposition counts are the first hard measure of how large the correction actually was. You decide on seeing
them whether to carry on, narrow, or stop.

**What blocks it.** Phases 3, 5, 7 and 9.

**Size — a formula, recomputed the day phase 3 closes and again when phases 5 and 7 close.** Number of claims
in the union set, times the cost of one test, plus the disposition pass over 4,253 existing tests. At a few
hundred claims the new-test half is one to two weeks; the disposition pass is unsized until somebody measures
how many existing tests assert something the corrected language contradicts. **Above six weeks in total this
phase is re-cut**, most likely by splitting the claims by area, since they are already numbered and grouped
by the document they come from.

**Going wrong, as distinct from slow.** Slow is many claims. Going wrong is tests being written that pass —
every test this phase writes is red by construction, and a green one means it is testing what the compiler
does rather than what the claim says, which is the evidence rule this whole plan turns on. Also going wrong:
claims that cannot be turned into a test because they are not testable as stated. A handful is normal and
they go back to phase 3 for rewording. Many of them means the numbering counted prose rather than claims.

---

## Phase 11 — Build until the frozen suite is green

**Goal.** Make the frozen suite green by building the compiler, without touching the suite.

**What it produces.** Working compiler code — including **the certificate checker and the runtime's
load-time call to it**, and **the fix for the confirmed unsound discharge**, each driven by a red test
written in phase 10.

**Why the checker is in scope, including its runtime half.** Without it, certificates get written and
nothing ever replays them. It had been retracted from scope once and priced as a large piece of work; it
is back in. The runtime half is nearly free — a call inside a method that already works, touching no
entity data.

**Done when.**

1. `dotnet test` is green across all test projects.
2. The diff of the test tree against the freeze commit shows no test deleted, renamed, skipped or weakened —
   only tests moving red to green. Check: three of those four verbs are mechanical from the diff. A test name
   present at the freeze must still be present, which covers deleted and renamed; the skip-attribute count
   against the baseline covers skipped. **Weakened is a human read of a diff** — a loosened assertion, a
   widened tolerance, a changed input — and the finish line says so rather than burying it. One partial
   mechanisation worth taking: record the assertion count per test file at the freeze commit and compare
   after. Crude, insufficient, and it catches the most common weakening, which is a deleted assertion.
3. The phase 0 register's six proof-result rows are removed, and its `.claude/skills/execute/SKILL.md` row is
   removed. Check: grep each quoted text; hits = 0. These come out when the proof engine is aligned to the
   spec, which is here, not at phase 9.

**What happens if phase 11 finds a claim needing a test that phase 10 missed.** The test set is on the
closing table, so the closed-list rule applies and everything halts. That will happen, and the plan should
not pretend otherwise. Record the claim, add its test, increment the freeze to a new named commit, and note
it on the board — the rule fires as a recorded reopening rather than as a stoppage. **If it fires more than
three times on different grounds, phase 10's numbering did not cover the claim set, and that is the
three-attempt rule reaching phase 10 late.**

**No size estimate, by agreement**, on condition the goal and the finish line above are clear. The
stop-or-continue point sits at the end of phase 10, when the test count exists.

**What it may cite.** Everything from phases 3 through 10. This is the phase where the catalogs are finally a
legitimate answer to "what does this owe".

**What you sign off, and what you are looking for.** Green, and the test diff. You are looking for a test
that got easier.

**What blocks it.** Phase 10.

**Going wrong, as distinct from slow.** Slow is expected and is not a problem — that is why there is no
estimate. Two things are going wrong. Any test weakened, skipped or deleted to reach green; that is not
slow, it is the finish line being moved, and it is what the diff check exists to catch. And red tests being
reclassified as "not really claims after all" — if a claim is wrong it goes back to phase 3 and reopens a
closed list, which is the correct expensive outcome, and routing around it is the failure.

**One addition beyond the settled input, labelled as one.** If the red count plateaus for two weeks, the
building has hit something the plan did not anticipate and it comes to you rather than continuing. This is
the plan's own rule, not something agreed on 2026-07-25; the agreed stop is the single one at the end of
phase 10.

---

## Phase 12 — Confirm it is finished, by people who did not build it

**Goal.** Somebody who did not build it confirms the compiler is finished.

**What it produces.** A re-run of the claim-to-test trace; a termination result over a named input set; a run
of the counterexample search; a certificate-checker demonstration; and a named list of the places examined
for the compiler continuing without proving.

**Done when.**

1. The claim-to-test trace re-run shows nothing dropped. Check: run the phase 10 script at HEAD, diff against
   the frozen result; differences = 0.
2. Every input in the named set either compiles or produces diagnostics, and terminates. Check: run the set
   under a timeout; timeouts = 0, crashes = 0. **The named set is derived rather than collected**: one input
   per phase 3 numbered statement that a program can be written to exercise, plus at least one per obligation
   family and one per refusal case in the grid. Assembled by the people who built the compiler from their own
   sense of what matters, it measures nothing. It is explicitly not the sample files.
3. **The certificate checker runs at load time over the named input set, and a certificate with a step
   removed is rejected**, demonstrated by a committed test. This is the only check anywhere in the plan that
   the guarantee holds end to end, and it is mechanical.
4. The counterexample search runs and reports; new counterexamples = 0.
5. A named list exists of the places examined for the compiler continuing without proving, each with what was
   found. Check: the list exists and every entry has a result. **Whether the examination went everywhere is
   judgement** — the list exists so that you are judging a named set of places rather than a claim of
   thoroughness.

**What it may cite.** Everything. The catalogs are canonical from phase 9.

**What you sign off, and what you are looking for.** That it is finished, on the reviewers' evidence. You are
looking at condition 5 and judging whether the examination went everywhere it needed to.

**What blocks it.** Phase 11.

**Size.** Two to three days for the runs, plus separately sizing the construction of the named input set —
building a set of `.precept` definitions broad enough to be worth running is its own job and is not covered
by those three days. Plus your reading.

**Going wrong, as distinct from slow.** Going wrong is the reviewers finding that the trace no longer holds,
or finding a place where the compiler continues without proving. Either means phase 11 built past a hole
rather than into it, and the fix is in phase 11, not here. It is also going wrong if the reviewers are the
builders — the whole value of the phase is that they are not.

---

# What this plan does not answer

These are open and visible on purpose. Each would change something in the plan if it were answered, and none
of them can be answered from where we stand today.

**The certificate step list's real length.** Twenty-one step kinds have been designed and none built. But
the twenty-one was derived as the closure of evidence sources consulted by the *existing* discharge paths — a
count over the compiler's current paths, not over the phase 5 walk — and the document that produced it parks
an open question whose one candidate resolution grows the membership past twenty-one. That is why phase 4
carries no numeric threshold: a list drafted by a new method cannot be measured against a number produced
by an old one. Phase 4 delivers the count with each step kind marked recovered-or-invented and
afternoon-or-fortnight, and you rule.

**Whether the induction has ever been argued for a language with these features.** The promise is that the
initial event establishes every rule and every operation preserves it. Over a simple state machine that is
obvious. Over a language with state residency, computed fields recomputed on every operation, `omit` resets
on entry, entry and exit chains and first-match rows, no argument for it exists anywhere, and nobody has
established whether such an argument exists for any comparable language. If it is wrong, phases 5 through 7
produce nothing. Phase 5's finish line for it is presence-only and says so; phase 6 attacks it; neither makes
it true.

**Whether the proof procedure will be fast enough to satisfy the spec's own admissibility rule.** The
specification makes not breaking single-file recompile a condition of a strategy being allowed. A pass over
every rule crossed with every write site is a different cost class from what the corpus costs today, and this
project's records carry two different figures for that cost — about 44ms and about 107ms — neither
re-measured for this plan. Phase 5 takes its own baseline; until it does, the answer is unknown.

**What a discharge rule is a rule *about*.** Nothing in the settled input or in this plan defines it, and
phase 6's whole size depends on it — one rule per expression form gives fifteen, one per obligation family
crossed with form passes the re-cut threshold before the phase begins. Phase 5 owns producing the definition;
until it does, phase 6 is a formula with no value and this plan deliberately does not supply an illustrative
one.

**Where the data files that close the language come from.** Generated-from, extracted-by, or
hand-maintained-beside — three arrangements with very different costs, and only the middle one closes the
seam between prose and data file while keeping prose canonical. Phase 3 must name which and make it a
finish-line condition. Until it does, "the language stops moving" is true one layer down from where it
sounds.

**How large the disposition pass over the existing test suite is.** There are 4,253 tests, and how many
assert behaviour the corrected language contradicts determines whether phase 10's disposition step is a few
dozen tests or a few hundred. That is a measurement phase 10 takes; nothing available today predicts it.

**Whether phase 8's "code exists that attempts this" list can be trustworthy before the phase 10 tests run.**
Assigning a cell to it means reading a compiler whose proof results are known wrong in at least one confirmed
case. If it cannot be made trustworthy, phase 8 may belong after phase 10 rather than before it — but that
re-cuts the settled phase order, so it is flagged as a question rather than proposed as a change.

**Whether the six ways the matrix slices the problem survive into the new discharge procedure.** The signpost
keeps the matrix alive for those six ways and not for its contents, but phase 5 writes the procedure as a
walk over expression forms, which may or may not reuse them. Phase 5's entry neither requires nor forbids the
connection. Worth a sentence from you before phase 5 starts.

**Whether the eleven-week gap before the first line of product code is intended.** It follows from summing
this plan's own sizes and it is stated on the board. The last row of "Waiting on Shane" is the place to
answer it.
