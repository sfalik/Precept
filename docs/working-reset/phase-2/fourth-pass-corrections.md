# Phase 2 — the fourth pass, closing what the third independent re-check found

**Run 2026-07-27 at HEAD `3dbad40b`.** Input: [`recheck-third-pass.md`](recheck-third-pass.md), which
re-ran phase 2's finish-line conditions 3 and 4 and failed both. Everything in it is worked here
except the ingress-scope question itself, which is the owner's.

**Nothing is committed.** Every edit below sits in the working tree.

**What was cited**: the canonical documents at HEAD, `docs/Working/what-i-want-2026-07-16.md`, the
phase-1 register and walk, and phase 2's own working documents. No catalog data as evidence, no
sample, no compile. `docs/philosophy.md`, `docs/Working/bugs.md` and everything under
`docs/working-reset/phase-5/` were not opened.

**The measurement rule this pass worked under.** Every pattern written or rewritten below was run
against the tree the correction landed on top of — `git show <commit>^` over the same files — and had
to return more than zero there. A pattern that finds nothing in the pre-correction tree was never a
test of anything. That rule is now written into the register's § How to read a row so a fifth pass
does not have to rediscover it.

---

## 1. The disputed marking now reaches all seven sites

**The problem.** The block note marking the ingress narrowing as disputed sat at
`precept-language-spec.md:1978`, inside §3A.4 — the atomicity section. The narrowing itself, "and
nothing wider", is stated in seven canonical places, and the one that matters most is §0.7, which is
the section every other document cites for the guarantee contract. `evaluator.md:1383` cites §0.7 *by
name* as its authority for the narrowing, and §0.7 stated it flat and unmarked seventeen hundred
lines away from the note.

**What changed.** The full note moved to §0.7, immediately after the *Governance* paragraph that
states the narrowing. §3A.4 keeps a three-line pointer to it. Each of the five runtime and
architecture sites gained a one-line pointer in the same words.

| Site | Before | After |
|---|---|---|
| `precept-language-spec.md` §0.7 *Governance* | unmarked | the full note, with the list of the other six sites and the reminder that all of them were written in the same pass |
| `precept-language-spec.md` §3A.4 | the full note | a pointer to §0.7, keeping "Do not build against this scope" |
| `runtime/runtime-api.md` § Update | unmarked | pointer |
| `runtime/evaluator.md` § Update ingress (code comment) | unmarked | pointer, in the comment |
| `runtime/evaluator.md` § Update, prose | unmarked | pointer |
| `runtime/result-types.md` § `UpdateOutcome` | unmarked | pointer |
| `compiler-and-runtime-design.md` §1.1 *Governance* | unmarked | pointer, inline |

**Nothing about the question was resolved and no disputed wording changed.** `grep -rn "disputed"`
across the canonical set returned exactly one line before this pass and returns seven now, one per
site. Register row `N96` stays open and stays routed to the owner;
[`editable-write-cascade.md`](editable-write-cascade.md) still records the reasoning.

---

## 2. Two canon sites nothing had a row for

**`docs/compiler/proof-engine.md:2237` and `:2249`** described `ConstraintInfluenceMap` as the
"Runtime constraint re-check index", "Used by the evaluator for constraint re-check". That is the
removed model stated flat: a runtime index whose purpose is deciding which of the definition's rules
to re-check after a change. It contradicted `evaluator.md:1727` (now `:1731`), corrected in this same phase to say
the bucket index survives for two purposes, neither of which is re-checking rules against entity data.

**Corrected.** `:2237` now says what the index answers — which constraints mention a given field or
argument — and who reads it: ingress, to route an entering value to the constraints declared on the
slot it enters, plus agents and diagnostics. A following paragraph says flatly that it is not a
re-check index, gives the reason (every handler was proven to preserve every rule, so there is no
pass after the mutation for such an index to drive), and points at `evaluator.md` § Ingress Validation
Guarantees. The builder-consumption table cell at `:2249` reads "Field-and-arg → constraint index,
read at ingress; not a re-check index".

The residual shape question — whether ingress routing is served by this map or by the evaluator's own
bucket index — is left open and joined to the one the bucket index already carries.

**Recorded as register rows `N97` and `N98`**, in a new § 1.5 for `proof-engine.md`.

---

## 3. The family test now covers synonyms, not only inflections

**Why the two sites above survived three passes.** The first pass chased the noun form
(`constraint evaluation`). The second added the verb and passive forms (`evaluates constraints`,
`constraints are evaluated`). "Constraint re-check" is a *third* spelling and matches none of them.
The lesson the third pass drew — "every idea in this procedure needs its verb, its noun and its
passive spelling" — was one word short. **The axis to widen along is synonyms.**

The register's § 1.0 family test and the procedure's `A1` grep both now carry `re-check`,
`re-evaluate`, `re-validate`, `re-verify`, `re-govern` and `re-prove`, in both word orders relative to
`constraint` / `rule` / `ensure`.

**What the widening turned up, run at `3dbad40b` before this pass edited anything.** The narrow form
returns 19 lines; the widened form returns **33**. Of the 14 extra lines:

| Verdict | Lines |
|---|---|
| Denies the removed model using a re-check or re-validate word — legitimate | `result-types.md:119`, `precept-language-spec.md:1911`, `runtime-api.md:252`, `:277`, `compiler-and-runtime-design.md:125`, `:129`, `:766`, `evaluator.md:1725`, `:1795`, `proof-engine.md:2677`, `philosophy.md:37` |
| Unrelated use of the word — a roadmap note about `deque of T` | `collection-types.md:1505` |
| **The removed model asserted flat** | `proof-engine.md:2237`, `:2249` — rows `N97`, `N98` |

So the widening found exactly the two sites the third re-check found by reading, and nothing else. The
register's § 1.0 triage table now carries all six groups so a fifth runner can reproduce it.

After `N97` and `N98` are corrected the widened test still returns 33, because the corrected text is
itself a denial and joins the first group.

**One synonym gap left un-widened, and named.** `A2` searches for `backstop`, `defense-in-depth`,
`fault trap` and `FaultSite`. A backstop is also a *guard* and a *safety net*, and neither is in the
pattern. The blind-spot note in `search-procedure.md` § 6 now says so. `A3`–`A16` have the same
shape. Widening them is worth doing before the procedure is next re-run; this pass did not do it.

---

## 4. Three patterns that could never have caught what they claim to have removed

Each was run against `git show <commit>^` and returned zero there, which means it never tested the
sentence it was written against. The corrections themselves all landed — verified by reading the text
at HEAD, not by trusting the pattern.

| Row | What was wrong | Rewritten to | Pre-correction | HEAD |
|---|---|---|---|---|
| **`S14`** | Wrapped the quoted phrase in single apostrophes; the specification wraps it in straight double quotes. (The third re-check called them typographic quotes — the character is a plain `"`, 0x22. Same failure, different diagnosis.) | `grep -rnF 'count is "no result outside a declared bound"' $CANON` | **1** at `5e853c0e^` | **0** |
| **`N65`** | The specification wrote the verb with markdown emphasis — `*carries* a sufficient constraint`. The literal string with no asterisks matched nothing. | `grep -rnE 'carries.{0,4} a sufficient constraint' $CANON` | **1** at `5e853c0e^` (`precept-language-spec.md:266`) | **0** |
| **`R8`** | Greps the 2026-07-19 wording, which was already gone before phase 2 opened, so it returned zero in every tree it could run against. Vacuous, and the register said so. | A confirm row asserts the **aligned** text is present: `grep -rn "bypasses both access-mode checks and" $CANON` must return more than zero. | **3** at `c3a9be21^` | **3** |

`R8`'s rewrite needed a new pattern kind, because a confirm row has no removed phrase to test the
absence of. **`CONFIRM` is now in the register's § How to read a row**, alongside `ADDITION` and
`RE-SCOPE`, and it says what the pattern for such a row must do.

---

## 5. Two patterns of the wrong kind, reclassified rather than forced to zero

**`N42` — `no division by zero, no overflow`, 2 hits at HEAD.** The row's resolution is to *disclose*
the overflow deferral on three sentences, not to delete the fault list, so the quoted phrase was never
going to disappear and a deletion pattern on it fails condition 3 permanently. **Reclassified
`RE-SCOPE`**, with the two surviving lines named: `precept-language-spec.md:266`, whose deferral is
disclosed six lines down at `:272`, and `docs/philosophy.md:45`, which is an `OWNER APPROVAL REQUIRED`
row and not this one's to touch. The test is now that the disclosure exists —
`grep -rn "Overflow is where this does not hold today" $CANON` returns **0** at `5e853c0e^` and **1**
at HEAD — and that the phrase appears nowhere outside those two lines.

**`N36` — `defense-in-depth`, 13 hits at HEAD.** Not a re-scope. The row corrects one document and
the pattern was scoped across the whole canonical set, so it counted text belonging to nineteen other
rows that nobody has executed. All 13 survivors sit in the deliberately-deferred fault-backstop
family — `proof-engine.md` (4), `fault-system.md` (4), `evaluator.md` (2), `precept-builder.md` (2),
`soundness-and-coverage.md` (1) — which `R9`–`N35` own. **Pattern scoped to the file the row owns**:
`grep -rn "defense-in-depth" docs/compiler-and-runtime-design.md` returns **7** at `25894ae3^` and
**0** at HEAD. The register's § How to read a row now states the rule generally: a row that corrects
one document scopes its grep to that document.

---

## 6. Bookkeeping over corrections that already landed

**The five "NOT ACCOUNTED FOR" cells.** Re-checked by reading the canonical text, not by trusting a
pattern. Four were closed and the fifth was mislabelled.

| Row | State | Closed by |
|---|---|---|
| `N19` — `RestoreConstraintsFailed` in `runtime-api.md` | **Closed.** The outcome is gone from the caller example and the "should be removed when the Restore API is finalized" deferral note went with it. 2 hits at `4ca55318^`, 0 at HEAD. | `4ca55318` |
| `N54` — symmetric write-site obligations | **Closed.** `proof-engine.md:2704` and `precept-language-spec.md:270`. 1 hit at `4ca55318^`, 2 at HEAD. | `d32d8e26` + `4ca55318` |
| `N59` — the non-linear rule stated normatively | **Closed.** `precept-language-spec.md:270` is a full normative paragraph with the rejection message shape written out. 0 hits at `4ca55318^`, 1 at HEAD. | `4ca55318` |
| `N67` — `CertificateSteps` at `precept-language-spec.md:225` | **Never a correction.** Its own resolution is "CONFIRM — aligned with the want". Relabelled "confirmed, no edit" and given a `CONFIRM` pattern that returns 7 in both trees. | n/a |
| `N74` — the temporal `(none)` row | **Closed.** `temporal-type-system.md:143` now says compile error in both columns and `:145` adds the reconciliation. 1 hit at `4ca55318^`, 0 at HEAD. | `4ca55318` |

`N59`'s pattern was also replaced. It used to be a bare `non-linear`, which returns more than zero
whether the rule is stated or not — the word occurs at `:1454` about quantity unit conversion. It now
quotes the normative sentence.

**Two stale "uncommitted" cells.** `N31` and `N32` both said "Not executed as a commit, but addressed
in the current working tree" and asked somebody to commit `primitive-types.md` and
`collection-types.md`. `4ca55318` committed them. `defensive redundancy`: 2 at `4ca55318^`, 0 at HEAD.
`Runtime fault — integer overflow`: 2 at `4ca55318^`, 0 at HEAD. Both cells now cite the hash.

**`N15`'s pattern never ran.** It used `grep -rnE` with backslash-escaped alternation, under which
`\|` is a literal pipe and the pattern matches nothing in any tree. The backslashes were there because
a bare `|` ends a markdown table cell, so the recording format broke the pattern. Rewritten with
repeated `-e`, which avoids the pipe: 6 at `25894ae3^`, 0 at HEAD. The third re-check reported the
same bug in the procedure's `A15` grep — it is not there; `A15` is unescaped and runs correctly.

**Three more enumeration and header repairs**, all from the third re-check's own list:

- `result-types.md:227` added to the `N79` group in § 1.0. It is a fourth line of the three
  `ConstraintResult` arrays and the enumeration listed three, so a re-runner could not reproduce the
  triage.
- The register's header HEAD line now says it is the build baseline and names the later hashes, so a
  re-runner who reads only the header does not measure against the wrong tree.
- `search-procedure.md` § 6 blind spots **2 and 4 are closed** and kept in the list so nobody
  re-opens them. Blind spot 2: the three excluded top-level documents were searched and returned zero
  hits in all three. Blind spot 4: claim W20 is stated directly at `runtime-api.md:258`. The broken
  `precept-language-spec.md:1992` cite in `docs/agent-onboarding.md:29` and `CLAUDE.md` is recorded
  there so it is not lost; it is outside phase 2's reach and was not fixed.

---

## 7. `N57` finished, and one thing it turned up that is not mechanical

**What `d32d8e26` left undone.** `N57`'s resolution is "reconcile the count", and its pattern quoted
one sentence out of a family its own reason column enumerates, so it reported success over an
unfinished correction.

- **`proof-engine.md:92`** still read "a bounded, **six-strategy** set" — a bare numeral in the
  document's opening statement of what the proof engine is. It now points at § Proof Strategies.
- **`proof-engine.md:151`** claimed "Every bare numeral in this document has been replaced by a
  pointer to § Proof Strategies", which `:92` made false, and said "`compiler-and-runtime-design.md`
  says four", which `N58` had already removed in `25894ae3` — at HEAD that document states no count
  anywhere and points here. The note is rewritten: it says what the enumeration contains (ten
  strategies with headings, an eleventh described without one, two requirement-dispatched paths that
  reuse another strategy's reporting tag) and still chooses no number.
- **The section headed "Strategy 7: Compositional Constraint Proof" called itself Strategy 6** four
  times in its own body, while its two closing notes and the section heading call it Strategy 7 and
  Strategy 6 is Dimensional Product Proof. Renumbered. **Register row `N99`.**

`N57`'s pattern is replaced by one that tests the whole family rather than one sentence: no bare
strategy numeral anywhere in the two documents. Written with repeated `-e` so no pipe breaks the
table cell — **18** at `d32d8e26^`, **3** now, and all three are legitimate (the estimated-lines-of-code
row, the disclosure note describing the enumeration, and one line counting *requirement kinds*).

### The thing that is not mechanical, and is not resolved

**The `ProofStrategy` enum and the `#### Strategy N` headings number the same strategies differently
from 6 onward.** The enum declares `CompositionalConstraint = 6`, `IntervalContainment = 7`,
`LengthContainment = 8`, `CountContainment = 9`, `DimensionalProduct = 10`. The headings place
Dimensional Product at 6, Compositional Constraint at 7, and Interval, Length and Count at 8, 9 and
10. Every strategy from 6 up carries two numbers in the same document. The prose outside the enum
follows the headings.

This is the same contradiction `N99` fixed inside one section, across the whole enumeration — and it
is not mechanical, because reconciling it means changing either a code-facing enum's values or ten
section headings and the prose that cites them, and no document says which numbering is canonical.
It also changes how `:630`'s split into "catalog-driven (1–7)" and "requirement-dispatched (8–10)"
reads: under the enum's numbering Dimensional Product is 10 and sits in the requirement-dispatched
range, which is what `:501` describes it as doing.

**Recorded, not resolved.** The disagreement is written into `proof-engine.md:151` in the document's
own voice, alongside the count disclosure, so a reader meets it where the count is discussed.
**Register row `N100`, routed to the owner alongside `N80`.**

---

## 8. What is left for the owner

Six rows, up from four:

| Row | Question |
|---|---|
| `N79` | What the three `ConstraintResult` arrays carry, now that there is no post-mutation evaluation to collect from |
| `N80` | Whether the `ConstraintPlanIndex` survives in reduced form or is replaced by a certificate-derived premise list — and, added this pass, whether ingress routing is served by that index or by `ConstraintInfluenceMap` |
| `N81`, `N82` | As recorded |
| `N96` | **The ingress scope on an editable write.** Untouched beyond moving the marking. Canon says an editable write upholds the constraints declared on the field the value enters and nothing wider; `want:143` says it also evaluates every rule that mentions the field, and gives a reason nothing has replaced. |
| `N100` | **New this pass.** Which strategy numbering in `proof-engine.md` is canonical — the `ProofStrategy` enum's or the section headings' |

**And one judgement, not a row.** `N54`'s specification sentence sits inside the non-linear paragraph
and reads as a consequence of that paragraph's rule, while `want:20` claims the symmetric write-site
obligation for *every* rule. Only `proof-engine.md:2704` states it generally, and there it is stated
alongside the build gap — the write-site enumeration the compiler works from is asserted by spec
§3A.4 rather than kept closed by the build. Whether the specification should state it generally is
the owner's call. Recorded in `N54`'s commit cell.

---

## 9. Line numbers moved, and by how much

The pointers and the two new paragraphs added lines to five canonical documents, so citations below
the insertion points no longer resolve. A fifth runner needs this table; the register's own citations
were not re-numbered, because re-numbering 141 rows by hand introduces more error than it removes.

| Document | Insertion | Everything below it shifts |
|---|---|---|
| `precept-language-spec.md` | §0.7 gained the moved note after `:275` | **+16** from `:276` to `:1993` |
| `precept-language-spec.md` | §3A.4's nine-line note became three lines | **+10** from `:1994` down (net of the +16 above) |
| `runtime-api.md` | pointer after `:403` | **+2** |
| `evaluator.md` | pointer after `:739` | **+2** to `:1386` |
| `evaluator.md` | pointer after `:1384` | **+4** from `:1387` down. The bucket-index guarantee the register cites as `:1727` is now `:1731`. |
| `result-types.md` | pointer after `:153` | **+2** |
| `compiler-and-runtime-design.md` | inline, one line rewritten | **0** |
| `proof-engine.md` | `:151`'s note became five lines | **+4** from `:152` to `:2240` |
| `proof-engine.md` | the not-a-re-check-index paragraph after `:2237` | **+6** from `:2241` down |

## 10. What this pass did not do

- **Conditions 1, 2 and 5** were not re-run; this pass worked the third re-check's findings on 3 and 4.
- **Whether a correction is *right*.** The sites behind the six defective patterns were read. The rest
  were verified by the pattern pair — pre-correction hit, HEAD zero — which tests that the pattern is
  a test, not that the resolution is correct.
- **The nineteen deferred fault-backstop rows** (`R9`, `N22`–`N30`, `N34`, `N35`, `R10`–`R14`, `C1`,
  `C2`) are still deferred and their patterns still return hits. That is the honest state of the work.
  A reader running condition 3 mechanically still counts about twenty non-zero patterns, of which the
  nineteen are disclosed in their own commit cells and none is a missed correction.
- **`A2`–`A16` were not widened for synonyms.** § 3 above names this as the next thing worth doing.
- **Blind spots 1 and 3** — prose using none of the vocabulary, and diagrams and code blocks — remain
  open by their nature.
