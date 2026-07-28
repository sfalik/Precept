# Third independent re-check of phase 2's finish-line conditions 3 and 4

| Field | Value |
|---|---|
| Run | 2026-07-27 |
| At | HEAD `84577ee1`, branch `spike/Precept-V2-Radical-reset` |
| By | A reader who wrote none of the work being checked |
| Checked | Condition 3 (every recorded pattern returns zero at HEAD) and condition 4 (the search procedure, re-run, produces no disagreement that is not already a row) |
| Plus | A stronger test the conditions do not ask for — see § 1 |
| Evidence | The canonical documents at HEAD, `docs/Working/what-i-want-2026-07-16.md`, the phase-1 register and walk. Not the catalogs, not `samples/`, not compiler behaviour. Nothing was compiled. |
| Edits made | None to any canonical document. This file only. |

**Working tree at the time of the run.** `docs/Working/bugs.md`, `docs/philosophy.md` and
`docs/working-reset/phase-5/recommendation-overflow-and-divisor-nonzero-2026-07-26.md` carry
uncommitted edits by work outside this check, and were left alone. Every measurement below is against
committed HEAD, not the working tree.

---

## Verdict

| Condition | Result |
|---|---|
| **3** — every recorded pattern returns zero at HEAD | **Fails on two patterns**, neither of which hides a missed correction. Separately, nineteen rows in the deliberately-deferred fault-backstop family still return hits and say so in their own commit cells. |
| **4** — re-running the procedure produces no disagreement that is not already a row | **Fails, on two disagreements.** One is a canon site with no row at all; the other is a marking that reaches one of the seven places it needs to reach. |
| **The stronger test** (§ 1) | **Three patterns fail it outright.** One of the three is declared as such by the register. Three more are defective in a way the stronger test does not catch — see § 1.2 and § 1.3. |

**Stated plainly: mostly bookkeeping, but not entirely — there are two new canon sites, and they are
both in `proof-engine.md`.**

The first two re-checks found patterns hiding standing text: two, then nine plus four written in the
wrong grammatical form, and widening those exposed seventeen sites nobody had listed. This pass found
a smaller version of the same thing rather than nothing at all:

- **`proof-engine.md:2237` and `:2249`** describe `ConstraintInfluenceMap` as the "**Runtime
  constraint re-check index**", "Used by the evaluator for **constraint re-check**". That is the
  removed model, it has **no row in the register**, and it survives because it is a *third* spelling —
  neither "constraint evaluation" (the noun the first pass chased) nor "evaluates constraints" (the
  verb the second pass added), but "constraint re-check". § 1.0's family test does not match it and
  neither does anything in the procedure.
- **`proof-engine.md:92`** still reads "a bounded, **six-strategy** set", which row `N57` exists to
  reconcile. `N57`'s pattern returns zero because it quoted one sentence out of a family its own
  reason column enumerates.

Both are small, both sit in a document whose fault-family rows are deferred anyway, and neither
changes the shape of the work. But they are the same defect the two earlier passes found, at a
smaller size, and the honest report is that the pattern class is not yet exhausted.

Everything else — five more defective patterns, four "NOT ACCOUNTED FOR" cells that are in fact
closed, two stale "uncommitted" cells — is bookkeeping over corrections that landed. Verified by
reading the text at HEAD, not by trusting the pattern.

---

## 1. The stronger test: run each pattern against the tree as it stood before its correction

**Why this test exists.** A pattern that returns zero at HEAD proves nothing on its own if it also
returned zero *before* the correction landed. Such a pattern cannot have verified a removal, because
it never matched the text it claims to have removed. Both earlier re-checks failed on exactly this
class and neither ran the test directly.

**Method.** For every row carrying an executing commit hash, the recorded pattern was run against
`git grep <pattern> <commit>^` over the same file set. A pattern returning zero in the pre-correction
tree was never a test of anything.

**Coverage.** Seventy-nine patterns were runnable this way — the rows carrying both a commit hash and
a recorded pattern. The remainder are `n/a — confirm by reading` rows, cross-reference entries, rows
routed to the owner, or rows deferred with no commit; none of those carries a zero-returning pattern
to test.

### 1.1 Patterns that fail the stronger test outright

| Row | Pattern as recorded | Pre-correction hits | What is wrong with it | Did the correction land? |
|---|---|---|---|---|
| **S14** | `count is 'no result outside a declared bound'` | **0** at `5e853c0e^` | Straight apostrophes. The specification writes the phrase with typographic double quotes: `count is "no result outside a declared bound"`. The pattern could never match the sentence it was written against. | **Yes.** `precept-language-spec.md:258` now reads "§0.7: a `mincount`/`maxcount` bound is rule shorthand, so keeping the collection inside it is rule preservation — discharged by an author guard, never deferred to a runtime check". Verified by reading. |
| **N65** | `carries a sufficient constraint` | **0** at `5e853c0e^` | The specification wrote `*carries* a sufficient constraint`, with markdown emphasis on the verb. The literal string with no asterisks never matched. Changing the pattern to `carries.{0,4} a sufficient constraint` finds the site immediately. | **Yes.** The carried-constraint framing is gone from `precept-language-spec.md:266`, replaced by the four premise classes and the induction at `:268`. Verified by reading. |
| **R8** | `bypasses access-mode checks but enforces constraint checks` | **0** at `c3a9be21^` | Vacuous, **and the register says so.** R8 is a confirm-and-strike row: the text it names was already gone before phase 2 began, and the commit cell records "confirmed discharged and struck, no text changed". A vacuous pattern on a row that corrects nothing is honest, not hidden. | Nothing to land. |

Two of the three are the same defect as the nine the second pass found — a pattern written from
memory of the sentence rather than copied from it. Both are character-level: a quote mark, and a pair
of asterisks. Neither hides a standing site.

### 1.2 Two patterns of the wrong kind, which can never reach zero

These pass the stronger test — they did match before — but they cannot reach zero at HEAD, for the
reason the register's own `RE-SCOPE` note describes. They are the sixth and seventh members of a class
the third pass identified five members of (`S7`, `N9`, `N40`, `N41`, `N43`) and stopped.

**`N42` — `no division by zero, no overflow`, 2 hits at HEAD.**

The two hits are `precept-language-spec.md:266` and `docs/philosophy.md:41`. Neither is a defect:

- The specification's fault list survives on purpose. `:266` states the target, and the deferral is
  disclosed six lines later at `:272` under the heading "**Overflow is where this does not hold today,
  and saying so is the point of this paragraph.**" Principles 7, 10 and 11 at `:104`, `:110` and
  `:112` each carry the exemption sentence inline.
- `docs/philosophy.md:41` is the owner's reserved decision and is out of scope for this check.

`N42`'s resolution is "disclose the deferral on the same three sentences", which is a re-scope.
Recorded as a deletion pattern, it fails condition 3 permanently. **`N41`'s pattern is the correct
one** — it covers the three principle sites, returns zero at HEAD and 2 at `4ca55318^`.

**`N36` — `defense-in-depth` over `$CANON_WIDE`, 13 hits at HEAD.**

None of the 13 is in `compiler-and-runtime-design.md`, which is the file `N36` owns; that file went
from 7 hits at `25894ae3^` to 0. The 13 survivors all sit in files belonging to the
**deliberately-deferred fault-backstop family**: `proof-engine.md` (4), `soundness-and-coverage.md`
(1), `evaluator.md` (2), `fault-system.md` (4), `precept-builder.md` (2). The pattern is scoped across
the whole canonical set while the row corrects one file, so it cannot reach zero until `R9`–`N35`
execute. The correction itself is complete; the pattern needs a path restriction.

### 1.3 One pattern that passes both tests over an incomplete correction

**This is the finding closest to what the two earlier passes found, and the stronger test does not
catch it** — the pattern matched before and returns zero now, and the correction is still unfinished.

**`N57` — `Five strategies only`, 0 at HEAD, 1 at `d32d8e26^`.**

`N57`'s resolution is "**Reconcile the count**", and its reason column enumerates the problem
precisely: the same document "says **six** at `:106`, `:142`, `:151` and `:2410`, and describes
**eleven** at `:628`", while `compiler-and-runtime-design.md:598` says **four**. The recorded pattern
quotes one of those sentences. `d32d8e26` corrected that sentence and added an honest disclosure note.
Three things it did not do, each of which its own note claims it did or contradicts:

| Site at HEAD | What it says | Why it is a problem |
|---|---|---|
| `proof-engine.md:92` | "discharge statically preventable runtime hazards using a bounded, **six-strategy** set" | A bare numeral, in the document's opening statement of what the proof engine is. `:151`'s note claims "**Every bare numeral in this document has been replaced by a pointer to § Proof Strategies**". This one was not, so the note is false about its own document. |
| `proof-engine.md:151` | "…`compiler-and-runtime-design.md` **says four**." | Stale, and stale by this same phase's own hand. `N58` removed "four strategies only" from that document in `25894ae3`. At HEAD `compiler-and-runtime-design.md` states **no count anywhere** — `:165` and `:655` both say "bounded, non-extensible" and point at `proof-engine.md`. The note describes a document that no longer says what it is quoted as saying. |
| `proof-engine.md:1486` vs `:1489`, `:1535` vs `:1699` | The section headed "#### **Strategy 7**: Compositional Constraint Proof" says in its own body "**Strategy 6** collects all assignments to that field" and carries the code comment "`// Strategy 6: Compositional Constraint Proof`", while `:1699` says "**Strategy 7** is the LAST general-purpose catalog-driven strategy tried — after Strategies 1–6 have all failed." | The enumeration `:151` names as "the only place a count can honestly be read off" cannot agree with itself about which number this strategy carries. That is the closed-list problem `N57` was opened for, one level down. |

The disclosure at `:151` is the right instinct — the register's own reason column says "the count is
not the point — the point is that a closed list nobody can state cannot be closed." But a disclosure
that misstates its own document twice and leaves the enumeration self-contradictory has not reconciled
anything, and `N57`'s pattern reports success.

### 1.4 The other seventy-three patterns

All returned more than zero in the pre-correction tree and zero at HEAD (or, for addition rows, more
than zero at HEAD and zero before). They tested what they claim to have tested.

The five `RE-SCOPE` patterns the third pass rewrote all hold up:

| Row | HEAD | Pre-correction |
|---|---|---|
| `S7` | 0 | 4 at `5e853c0e^` |
| `N9` (inverted — must return more than zero) | 1 | 0 at `4ca55318^` |
| `N40` | 0 | 1 at `4ca55318^` |
| `N41` | 0 | 2 at `4ca55318^` |
| `N43` | 0 | 2 at `4ca55318^` |

So do the two addition-row patterns it rewrote (`N52`, `N55`): zero at `5e853c0e^`, more than zero at
HEAD, which is the correct direction for an addition.

---

## 2. Condition 3 at HEAD

### 2.1 The two failures

`N42` (2 hits) and `N36` (13 hits), both explained in § 1.2. Neither is a missed correction.

### 2.2 The nineteen deferred rows, which are not failures

The runtime fault-backstop family was deliberately left standing: `R9`, `N22`, `N23`, `R10`, `N24`,
`N25`, `R14`, `N26`, `R11`, `R12`, `N27`, `R13`, `N28`, `N29`, `C1`, `C2`, `N30`, `N34`, `N35`. Each
commit cell records the deferral and the reason (the family crosses six documents, of which only two
were in any single slice's scope). Their patterns return hits at HEAD — `FaultSiteLink` 34,
`FaultSiteAnnotation` 25, `FaultSiteDescriptor` 25, `ConstraintPlanIndex` 17 (`N10`), `CC#6` 15, and
so on. **This is the honest state of the work, not a condition-3 failure**, but a reader running
condition 3 mechanically and counting every non-zero pattern will report about twenty-one.

An independent re-read of that family this pass confirms it is still coherent and still concentrated:
the `FaultSiteLink` → `FaultSiteAnnotation` → `FaultBackstops` chain across `proof-engine.md` § 2,
§ 8 and Decision 2, `precept-builder.md` Pass 4 and Pass 6, `evaluator.md` § 7.3 and § 9,
`descriptor-types.md`, and `fault-system.md` accounts for the great majority of it. `README.md`,
`docs/philosophy.md`, `precept-language-spec.md` and `compiler-and-runtime-design.md` carry none of it.

### 2.3 Two rows the register records as uncommitted that are now committed

`N31` and `N32` both say "Not executed as a commit, but addressed in the current working tree". Both
are committed at HEAD: `defensive redundancy` returns 0, `Runtime fault — integer overflow` returns 0,
and `git status` shows nothing outstanding under `docs/language/`. The cells are stale.

### 2.4 The five rows reading "NOT ACCOUNTED FOR"

The register's § 16 flags seven such cells, notes the third pass corrected `N9` and `N41`, and says
the remaining five have not been re-checked. **They have been re-checked here. Four are closed; the
fifth was never a correction.**

| Row | State at HEAD | Closed by |
|---|---|---|
| **N19** — `RestoreConstraintsFailed` still in `runtime-api.md` | **Closed.** Zero hits in `$CANON`. The outcome and its "should be removed when the Restore API is finalized" deferral note are both gone. Counts across the phase-2 commits: 2 at `d32d8e26`, 0 from `4ca55318` onward. | **`4ca55318`** |
| **N54** — symmetric write-site obligations stated nowhere | **Closed.** `proof-engine.md:2698`: "the induction quantifies over every write site of every field a rule mentions". `precept-language-spec.md:270`: "This holds at every write site of every field the rule mentions, so lowering `OverdraftLimit` inherits the same obligation from the other side." | **`d32d8e26`** (proof engine) and **`4ca55318`** (specification). One caveat below. |
| **N59** — no normative statement of the non-linear rule | **Closed.** `precept-language-spec.md:270` is now a full normative paragraph: proven only from premise class (c) or (b) stating the condition directly, otherwise the compiler "**rejects the handler** and names the guard that would close it", with the message shape written out. Zero hits at `4ca55318^`. | **`4ca55318`** |
| **N67** — `CertificateSteps` at `precept-language-spec.md:225` | **Never a correction.** N67's own resolution is "**CONFIRM — aligned with the want**", and it is confirmable by reading: `:225` is unchanged and agrees with want:199, and `catalog-system.md:103` states in canon's own voice that the catalog does not exist. "NOT ACCOUNTED FOR" is the wrong label for a confirm-only row; the right one is "confirmed, no edit". | n/a |
| **N74** — the temporal `(none)` row | **Closed.** `temporal-type-system.md` now reads `\| (none) \| Compile error — no context, no type \| Compile error — no context, no type \| TEMP005 compile error \| N/A \|`, followed by an added sentence — "**No expected type is always a compile error.** The `(none)` row above agrees with the type-resolution rules further down" — which is the reconciliation the row asked for. 1 hit through `d32d8e26`, 0 from `4ca55318`. | **`4ca55318`** |

**So: four of the five are closed, and `4ca55318` closes all four** (`N19`, `N59`, `N74` outright, and
`N54`'s specification half). The brief's estimate of "at least three" is right; the count is four. The
fifth was mislabelled, not unexecuted.

**One caveat on `N54`.** The specification's sentence sits inside the non-linear paragraph and reads
as a consequence of that paragraph's rule. The general claim want:20 makes — that this holds for
*every* rule, not only non-linear ones — is stated generally only in `proof-engine.md:2698`, and there
it is stated alongside the build gap ("the write-site enumeration the compiler works from is asserted
by spec §3A.4 rather than kept closed by the build"). The pattern is satisfied and the claim is in
canon; whether the specification should state it generally rather than as a rider on the non-linear
paragraph is a judgement, not a defect.

---

## 3. Condition 4 — re-running the search procedure

### 3.1 The procedure is runnable as written

Every command in § 0, § 3, § 4 and § 7 runs unmodified from the repository root. Three notes for a
future re-runner, none of which stopped the run:

- **The header HEAD is stale.** Both the procedure and the register say "written and first run at
  HEAD `b93d1db1`". `b93d1db1` is `review of the overflow and divisor recommendation`; HEAD is now
  `84577ee1`, eight commits later. The register's § 1.0, § 16 and philosophy section each carry a
  different, later HEAD, so the file states three baselines. Not ambiguous once read in full, but a
  re-runner who reads only the header measures against the wrong tree.
- **§ 3's `A15` pattern is broken.** It uses `grep -rnE` with escaped alternation inside a
  double-quoted string (`"EventConstraintsFailed\|Update…"`). Under `-E` the backslash-pipe is a
  literal, so the pattern matches nothing at all. The unescaped form is what `N15` means and what was
  run here. The same mistake sits in `N15`'s own recorded pattern.
- **Fifteen of the sixteen § 3 greps still carry one grammatical inflection each.** § 6 blind spot 0
  says so and says they "should be widened the same way before the procedure is re-run". They were
  not. See § 3.4.

### 3.2 The § 1.0 family test reproduces exactly

The register claims the family test returns 19 lines, each accounted for in one of five groups. Run at
HEAD it returns **19**, and the file-and-line set is identical to the register's table, group for
group: 8 denials, 1 verbatim historical quotation, 2 correctly-scoped ingress lines, 6 riding on
`N79`, 2 compile-time attribution. **This is the first triage in the register a second runner can
reproduce, and it reproduces.**

Widening `A1` to its other three terms (`post-mutation`, `the sweep`, `swept`) adds 18 lines, all of
which read correctly: denials, field names that contain the word (`PostFields`), the compile-time
count-interval walk at `proof-engine.md:1733`/`:1735`, the corrected comparator at
`result-types.md:375`, the corrected classification at `diagnostic-system.md:163`, the
surviving-bucket note at `evaluator.md:1727`, and the "moot" entry at `evaluator.md:2064`. Three that
looked worth opening — `compiler-and-runtime-design.md:151`, `:884`, `result-types.md:213` — all state
the corrected model in full and say why the old one is gone.

### 3.3 The absence searches

| Search | Expected | At HEAD |
|---|---|---|
| certificate in `docs/runtime/` | was zero, must now be more than zero | **4 sites.** `runtime-api.md:250` is a new "Definition Load and the Certificate Gate" section stating the gate and the premise list, with a "**Not yet built.**" disclosure at `:260`. |
| the induction stated as the compiler's proof model | was zero, must now be more than zero | **12 lines**, including `precept-language-spec.md:268`, `proof-engine.md:2696`, `soundness-and-coverage.md:95`, `compiler-and-runtime-design.md:149`. |
| the four premise classes as a closed set | was zero, must now be more than zero | **4 sites**, all enumerating the same four classes in the same order. No document names a fifth. |
| the base case's assumption for restored and host-injected data | was absent, must now be stated | **`precept-language-spec.md:280`**, in the want document's own words. |

### 3.4 The procedure's declared blind spots, worked

- **Blind spot 0 — every grep written in one grammatical form.** `A1` was widened on the third pass;
  the other fifteen were not. This pass compensated by reading every hit in context rather than by
  widening, which is a weaker defence and is stated as such. **It is also not sufficient, and § 4.1
  proves it**: the site found there uses neither the noun nor the verb form, but a third word for the
  same idea, and no widening of `A1` along the axis the third pass widened it would have found it.
- **Blind spot 2 — the three excluded top-level documents.** `docs/README.md`,
  `docs/agent-onboarding.md` and `docs/glossary.md` were searched here for the whole removed-model
  vocabulary — the sweep, the backstops, `never rejects`, provenance, re-governance. **Zero hits in
  all three.** The judgement the procedure took (a glossary entry is downstream of the specification)
  holds. **This blind spot can be closed.**
- **Blind spot 4 — claim W20 under-tested.** No longer absent: `runtime-api.md:258` states it
  directly — "The premise list *is* the set of runtime validations the proofs depend on … derived from
  the proof rather than maintained as a parallel list alongside it."

Blind spots 1 (prose using none of the vocabulary) and 3 (diagrams and code blocks) remain open by
their nature, and this pass does not claim to have closed them.

---

## 4. Disagreements found that are not rows

### 4.1 A canon site with no row: the constraint re-check index

**`docs/compiler/proof-engine.md:2237`**

> Inverted index: given a field or arg that changed, which constraints might be affected? **Used by
> the evaluator for constraint re-check** and by AI agents for causal reasoning.

**`docs/compiler/proof-engine.md:2249`** — the builder-consumption table:

> `| ConstraintInfluence | Inverts to ConstraintInfluenceMap | ` **`Runtime constraint re-check index`** ` |`

This is the removed model: a runtime index whose stated purpose is deciding which of the definition's
rules to re-check after a change. **No row in the register covers it.** `N10` covers
`ConstraintPlanIndex` and lists its sites; `ConstraintInfluenceMap` appears in the register exactly
once, inside `N22`, and only to say the opposite — that its construction is Pass 6's *other*
responsibility and survives the fault-backstop deletion.

It contradicts `evaluator.md:1727` directly, which was corrected in this same phase to say the bucket
index "survives the removal of the post-mutation pass because it still serves two live purposes"
— neither of which is re-checking rules against entity data.

**Why nothing found it.** The phrase is "constraint re-check". § 1.0's family test matches
`constraint evaluation`, `constraint plans evaluated`, `evaluates constraints`, and
`constraints are evaluated`. None of those matches `constraint re-check`. This is a **third** spelling
of the idea, after the noun form the first pass chased and the verb form the second pass added — and
the lesson § 6 blind spot 0 drew ("every idea in this procedure needs its verb, its noun and its
passive spelling") is one inflection short. It needs its synonyms too.

The correction is small and it belongs with `N10` and `N80`, since what the index becomes is part of
the same undecided question. Recorded here as a row the register does not have.

### 4.2 The disputed marking on the ingress scope reaches one site of seven

Row `N96` is open by design and is not reported as a failure. The brief's instruction was to report
anything that makes it inconsistent with the rest of the tree, and this does.

The narrowing — "and nothing wider" — is stated in **seven** canonical places:

| Site | Marked disputed? |
|---|---|
| `precept-language-spec.md:1976` (§3A.4) | **Yes**, by the block note at `:1978` |
| `precept-language-spec.md:274` (§0.7 Governance) | **No** |
| `docs/runtime/runtime-api.md:402` | **No** |
| `docs/runtime/evaluator.md:738` | **No** |
| `docs/runtime/evaluator.md:1383` | **No** — and it cites `spec §0.7` as its authority, which is the unmarked site |
| `docs/runtime/result-types.md:152` | **No** |
| `docs/compiler-and-runtime-design.md:151` | **No** |

`grep -rn "disputed"` across the whole canonical set returns exactly one line:
`precept-language-spec.md:1978`.

The consequence is precise. The note says "**Do not build against this scope, and do not cite it as
evidence**", and it is attached to §3A.4 — the atomicity section. §0.7 is the section every other
document points at for the guarantee contract, and it states the narrowing flatly. A reader who
arrives at §0.7, or at any of the five runtime and architecture sites (one of which cites §0.7 by
name), takes the narrowing as settled canon and never meets the note, which sits seventeen hundred
lines away in a different section.

The note already knows this — it says "every canonical sentence that agrees with it was written in the
same pass" — so it knows the other six exist and marks none of them.

**Not a defect in the ruling, and not a reason to close `N96`.** It is a marking that does not reach
the sites it needs to reach. The fix is mechanical — a one-line pointer at each of the six, or moving
the note to §0.7 and pointing §3A.4 at it — and it is not this pass's to make.

### 4.3 Two enumeration gaps, covered in substance

- **`result-types.md:227`** — "`Constraints` — rules evaluated against the hypothetical post-patch
  state" is a **fourth** site of the three `ConstraintResult` arrays, and § 1.0's `N79` group lists
  only `:173`, `:210` and `:248`. It is covered in substance: `:250`'s open note names
  `UpdateInspection.Constraints` explicitly and says "their descriptions above still describe the old
  model". The enumeration should carry the fourth line so a re-runner can reproduce the triage.
- **The broken specification cite outside `$CANON` is still live.** `N95` corrected
  `catalog-system.md:103` from `precept-language-spec.md:1992` to `:2010` and recorded that two copies
  live outside phase 2's reach. Both are still broken at HEAD: `docs/agent-onboarding.md:29` and
  `CLAUDE.md` § Catalog System both send a reader to `:1992`, which is now blank. Recorded so it is
  not lost when the register closes. Not phase 2's to fix.

### 4.4 Reading the want document directly, against canon

The procedure derives twenty claims and searches for each. Read end to end against canon independently
of that list, the want document yields four assertions the claim list does not carry. **All four are
already satisfied**, so none becomes a row — recorded so a fourth re-runner does not re-derive them:

- **want:106 — the editable-field ingress window closes structurally when the state changes.**
  Covered by `precept-language-spec.md:396`, `:1047`–`:1051`, `:1065` (the three-layer access-mode
  model), `graph-analyzer.md:564`, `runtime-api.md:283`.
- **want:154 — absence expresses inapplicability; `reject` is reserved for resolvable refusals.**
  Covered, and structurally: `precept-language-spec.md:1926`, `:1935` and
  `compiler-and-runtime-design.md:848` distinguish `Unmatched` from `Rejected` as separate outcome
  types rather than as message wording.
- **want:163 — `Version` is an immutable snapshot; every operation returns a new one.** The want
  document cites `runtime-api.md` for it, so it is aligned by construction.
- **want:177 — runtime refusals must explain themselves as clearly as compile-time rejections.**
  Carried by the `reject "…"` message mechanism with interpolation. Covered, though canon states the
  compile-time half far more explicitly than the runtime half.

**One tension worth naming, which is not a contradiction.** want:104 groups "division by zero,
overflow, out-of-range" as "the fault family" getting "the identical premise-and-certificate
treatment". Canon now says the opposite about the classification, emphatically:
`precept-language-spec.md:266` — "A value coming to rest outside a declared bound is deliberately not
on that list … it is not a fault gate and should not be quoted as one." The register's § 4 argues this
follows from want:22's one-mechanism claim and from want:104's own word *identical* — if the treatment
is identical, the label does not matter. That argument holds, and the disposition is the same either
way. Recorded because a reader comparing want:104 with spec:266 side by side sees two sentences that
look like they disagree, and the reason they do not is one inference deep.

---

## 5. What was not checked

- **Conditions 1, 2 and 5** were not re-run; this pass was scoped to 3 and 4.
- **Whether a correction is *right*.** Condition 3 is self-refereed and does not test this, and
  neither does this pass except where reading was needed to judge a pattern. The sites behind the six
  defective patterns were read; the other seventy-three were verified by the pattern pair alone.
- **Blind spots 1 and 3** — prose using none of the vocabulary, and diagrams and code blocks — remain
  open.
- **`docs/philosophy.md`, `docs/Working/bugs.md` and `docs/working-reset/phase-5/`** carry uncommitted
  work and were not read against.
- **The `A1` widening was not extended to the other fifteen greps.** § 4.1 is direct evidence that
  doing so is worth an hour of somebody's time, and that the axis to widen along is synonyms, not only
  inflections.
