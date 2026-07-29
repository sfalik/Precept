# Stamping the thirty-four unstamped register rows

**Run** 2026-07-28 against HEAD `65b1b11f`, branch `spike/Precept-V2-Radical-reset`. The working tree
was clean at the start. **Nothing here is committed**, and the only file changed is
[`contradiction-register.md`](contradiction-register.md) — 34 lines, one per row, commit column only.

**What this pass is.** Thirty-four rows said they had been executed and named the pass that did it,
but carried no commit hash. Exit condition 2 of the phase is that every row names the commit that
executed it, and it failed on exactly these. This pass writes the hashes in — and verifies each row
against the two trees rather than trusting the label, because the label says which *pass* did the
work and that is not evidence the work is in that *commit*.

**What this pass is not.** No canonical document was read for correctness, no canonical document was
edited, and no row's site, canonical text, resolution, reason or grep pattern was touched. The diff is
34 changed lines and `git diff --stat` shows `34 insertions(+), 34 deletions(-)`.

---

## 1. Method

For each row: read its recorded grep pattern and its cited site, then run the pattern at the commit's
parent and at the commit itself, with `git grep -- <rev>` over the same files the row scopes itself to.

A **deletion row** must return more than zero at the parent and zero at the commit. An **addition row**
is the reverse. Where a row is marked `RE-SCOPE` or `CONFIRM` its pattern is not deletion-shaped and
the counts were read against what that row's own cell says they should be.

Where the counts did not behave that way, the pattern was treated as a question rather than a verdict:
the canonical text was read at both revisions to answer whether the correction was not made or the
sentence was reworded underneath the pattern. Section 4 lists every row where that was necessary. In
every case the correction had been made.

**The pass-to-commit map** used, and confirmed by the verification below:

| Pass named in the cell | Commit |
|---|---|
| third pass | `84577ee1` |
| fourth pass | `fa4891d7` |
| fifth pass | `7265c0d3` |
| the runtime fault-backstop family, plus `N103` and `N104` | `c9193ec8` |

---

## 2. Group one — the fourteen rows that named a pass

Each cell read "Executed &lt;date&gt;, &lt;n&gt;th pass, uncommitted at the time of writing". The
date and the pass attribution were kept; "uncommitted at the time of writing" became the hash.
`$CANON` is `docs/language docs/compiler docs/runtime docs/philosophy.md`.

| Row | Commit | How verified | Parent | Commit |
|---|---|---|---|---|
| `N88` | `84577ee1` | recorded pattern `plan execution and constraint evaluation`, `$CANON` | 1 | 0 |
| `N89` | `84577ee1` | recorded pattern `evaluate constraints from prebuilt buckets`, `$CANON` | 1 | 0 |
| `N90` | `84577ee1` | recorded pattern `walks execution plans and evaluates constraints`, `$CANON` | 1 | 0 |
| `N91` | `84577ee1` | § 1.0 family test over `precept-builder.md`, then read the section — see § 4 | 3 | 1 |
| `N92` | `84577ee1` | § 1.0 family test over the three runtime documents, restricted to this row's six named sites | 6 | 0 |
| `N93` | `84577ee1` | **addition.** Recorded pattern `Available; whether the anchor shape survives is open`, `catalog-system.md` | 0 | 1 |
| `N94` | `84577ee1` | recorded pattern `provably dead declaration`, `$CANON` | 1 | 0 |
| `N95` | `84577ee1` | recorded pattern `precept-language-spec.md:1992`, `$CANON` | 1 | 0 |
| `N57` | `fa4891d7` | recorded strategy-numeral pattern over `proof-engine.md` and `compiler-and-runtime-design.md`, then read both corrected sites — see § 4 | 3 | 3 |
| `N97` | `fa4891d7` | recorded pattern `constraint re-?check`, `$CANON` | 2 | 0 |
| `N98` | `fa4891d7` | same pattern as `N97`; both sites carry the same phrase, and both are among the 2 at the parent | (shared) | (shared) |
| `N99` | `fa4891d7` | recorded pattern `Strategy 6` over `proof-engine.md`, then read the three survivors — see § 4 | 7 | 3 |
| `N101` | `7265c0d3` | recorded pattern `checked overflow` / `overflow is checked`, `$CANON` | 1 | 0 |
| `N102` | `7265c0d3` | **addition.** Recorded pattern for the disputed marking, `compiler-and-runtime-design.md` | 0 | 1 |

`N57` keeps the shape its cell already had — `d32d8e26` did most of the work and the fourth pass
finished it — so its cell now reads "`d32d8e26`, **finished 2026-07-27, fourth pass in `fa4891d7`**".

---

## 3. Group two — the twenty rows that landed in `c9193ec8`

Eighteen are the runtime fault-backstop family; `N103` and `N104` are the two sites that pass picked up
from its own vocabulary sweep. `R13` is in the family and is **not** here: it was ruled against and
never executed, its cell says so, and a row that correctly records that it was not executed already
satisfies the condition. It was left alone.

| Row | How verified (pattern, over `$CANON` unless noted) | Parent | Commit |
|---|---|---|---|
| `R9` | `FaultSiteAnnotation` | 25 | 2 |
| `N22` | `Fault Backstop Pass` | 3 | 0 |
| `N23` | `FaultBackstops` | 9 | 0 |
| `R10` | `Fault backstop routing` | 1 | 0 |
| `N24` | `CC#6` | 15 | 0 |
| `N25` | `Impossible-Path Failures (Defense-in-Depth)`, then read the replacement — see § 4 | 2 | 0 |
| `R14` | `Impossible-Path Failures` | 2 | 0 |
| `N26` | `FaultSiteDescriptor` | 25 | 5 |
| `R11` | `Fail(FaultCode.DivisionByZero)` | 3 | 0 |
| `R12` | `bypassed compile-time checking` | 3 | 0 |
| `N27` | `Defense-in-depth, not a primary error path` | 2 | 0 |
| `N28` | `fault codes exist at evaluation time as a backstop` | 1 | 0 |
| `N29` | `errors the type checker should have prevented`, then read the re-scoped variant — see § 4 | 2 | 0 |
| `C1` | `FaultSiteLink` | 34 | 3 |
| `C2` | `belt-and-suspenders` | 2 | 1 |
| `N30` | `defense-in-depth` over `proof-engine.md` | 4 | 0 |
| `N34` | `evaluator traps are defense-in-depth` | 1 | 0 |
| `N35` | `conservative backstop for proof-only obligation families` | 2 | 0 |
| `N103` | `The runtime check is a fallback, not a guarantee`, over the canonical set **plus `docs/tooling/`** | 1 | 0 |
| `N104` | `declared-bound value-safety codes`, over the canonical set **plus `docs/tooling/`** | 1 | 0 |

`N103` and `N104` both correct files in `docs/tooling/`, which is in neither `$CANON` nor `$CANON_WIDE`
as those are defined at the top of the register — the fifth pass added that folder to the searched set
without adding it to either variable. The patterns were run over the canonical set with `docs/tooling`
appended. The four hits still returned at the commit for `N104`'s pattern are all inside
`docs/working-reset/`, in the register and the pass records, not in any canonical or tooling document.

---

## 4. The rows whose counts did not read as a clean deletion

Every one of these was resolved by reading the text at both revisions. **None of them failed. No row
was stamped that could not be verified.**

### `N91` — 3 to 1, and the survivor is the one the row predicts

The § 12 heading *Dispatch-Optimized Constraint Indexes* became *Constraint Indexes, and What They
Index Now*; `:394`'s "before constraint evaluation" and `:798`'s "Eliminates O(constraints) scan per
operation" are both gone. The remaining hit at `:837` is the sentence the row's own grep column says
will survive and should — it names what traditional constraint systems do in order to say that Precept
does not do it. Denial needs the vocabulary.

One thing worth passing on, though it is not this row's defect and was not changed: the table of
contents at `precept-builder.md:60` still links to the old heading text.

### `N57` — 3 at the parent and 3 at the commit, but not the same 3

The count is equal by coincidence. At `fa4891d7^` the three were `:92` ("a bounded, **six-strategy**
set"), `:146` (an estimated-lines-of-code table row) and `:2642` (a count of *requirement kinds*). At
`fa4891d7` they are `:146`, `:153` and `:2648`. The one defect went — `:92` now reads "a bounded,
closed strategy set — enumerated in § Proof Strategies, which is the only place a count can be read
off" — and `:153`, the disclosure note the fourth pass added, is new. The other two are the legitimate
survivors the row already names. `:151`'s stale claim that `compiler-and-runtime-design.md` "says
four" was rewritten in the same commit.

### `N99` — 7 to 3, and all three are Dimensional Product

The four strays inside the Compositional Constraint Proof section (`:1490`, `:1513`, `:1526`, and the
code comment at `:1535`) are gone. The three remaining mentions of "Strategy 6" are `:505`, `:1474` and
`:1481`, all of them about Dimensional Product Proof, which is what the row requires.

### `R9`, `N26`, `C1` — nonzero survivors, all of them the same deliberate disclosure

`FaultSiteAnnotation` 25 to 2, `FaultSiteDescriptor` 25 to 5, `FaultSiteLink` 34 to 3. Every survivor
is a sentence saying the type is **still declared in code and its removal has not been made** —
`proof-engine.md:8` and `:272`, `precept-builder.md:943` and `:951`, `descriptor-types.md:8` and
`:195`, `evaluator.md:2006` and `:2142`. That is exactly what § 3's preamble in the register says the
pass would leave: "What remains is in the code … the documents now say so, and the removal is phase
11's."

### `C2` — 2 to 1, and the survivor is unrelated

The remaining "belt-and-suspenders" is `type-checker.md:1133`, "Forward-reference prohibition
belt-and-suspenders validation" — a type-checker item with nothing to do with fault backstops. The
`proof-engine.md` occurrence the row cites is gone.

### `N29` — the pattern goes to zero on a row that is not a deletion

The row is a re-scope: keep the `EventOutcome.Faulted` variant, change what populates it. Its own grep
column says to verify by reading. The variant is intact at `result-types.md:51`, `:72`, `:103`, `:115`,
`:129`, `:436` and `runtime-api.md:171` and `:904`, and the wording moved from "errors the type checker
should have prevented … a backstop for impossible-path bugs" to "a breach of one of the engine's own
invariants — a defect in the compiler, the builder or the evaluator". That is the row's resolution.

### `N25` and `R14` — the section did not disappear, it was re-scoped

The heading *Impossible-Path Failures (Defense-in-Depth)* is gone, which is what the pattern measures,
but the section survives as `### Fault Production` under `## 9. Failure Modes and Recovery`, scoped to
engine-invariant breaches, with the recovery table row now reading "Engine-invariant breach". `R14`'s
division-by-zero row went with the old table. The row asked for a re-scope, not a deletion, and it got
one.

### `N92` — one extra hit at both revisions, not one of the six sites

The filtered family test returned 7 lines at the parent and 1 at the commit. The extra line at both is
`evaluator.md:1795`, the `FromJson` decision, which uses the words "constraint **validation**" and is
not among this row's six named sites. Of the six the row does name, all six were present at the parent
and none is present at the commit.

---

## 5. Counts after stamping

Measured on the stamped file. The identifier extraction is the register's own, from its § 16 code
block; the classification reads each row's final column.

| Measure | Count |
|---|---|
| Identifiers | **150** |
| Of which are deliberately not rows (`P5`, `P6`, `N76`, `N77`, `N78`) | 5 |
| **Distinct rows** | **145** |
| Rows carrying a commit hash | **133** |
| Rows with no hash that state a reason instead | **12** |
| Rows with neither | **0** |

The twelve rows with no hash and a stated reason:

| Row | Reason recorded |
|---|---|
| `N8` | Not executed — the inspection result types' shape is an open question, held behind `N79` |
| `N10` | Not executed — routed to "Waiting on Shane" as `N80` |
| `N66` | Not executed — routed to phases 4 and 5 |
| `N67` | Confirmed, no edit — the row's own resolution is `CONFIRM`, so there was never an edit to account for |
| `N70` | Not executed — the addition was not made; the gap was recorded in canon's own voice instead, and the row stays open behind `N79` |
| `N79` | Not executed — routed to "Waiting on Shane" by design |
| `N80` | Not executed — routed to "Waiting on Shane" by design |
| `N81` | Not executed — routed to "Waiting on Shane" by design |
| `N82` | Not executed — the document-reconciliation half has not happened; the trait is new language surface and goes through `/design` |
| `N96` | Not executed, and not to be executed — routed to the owner, owned by phase 5 |
| `N100` | Not executed — "Waiting on Shane" alongside `N80` |
| `R13` | Ruled against and not carried out; the three `FaultCode` members stay |

Four of those twelve mention a commit hash inside their prose — `N66` cites `d32d8e26` to say the
passage appears in that diff as unchanged context, `N70` cites `c3a9be21` to say it did *not* add the
statement, `N79` cites `c3a9be21` for the open note it recorded instead, and `N81` cites `d32d8e26`
for stating the induction model without arguing it. None of the four is naming an executing commit,
and they are counted as reason rows, not hash rows.

**Condition 2 passes.** 133 + 12 = 145, and no row is left with neither a hash nor a reason.

---

## 6. Two places the file is now stale, deliberately left alone

Both were outside this pass's scope — it was bounded to the commit column of the thirty-four rows —
and both should be corrected by whoever next edits the register.

- **The index table at § "Where to find the detail behind any edit"** still carries a row whose Commit
  cell reads *uncommitted, 2026-07-28* for the runtime fault-backstop family. That work is
  `c9193ec8`. Twenty rows now say so and the index does not.
- **The § 3 preamble** still says "The rows below ran as one pass on 2026-07-28 and are uncommitted".
  Same fact, same commit.

Neither affects the condition-2 count, which is measured per row.
