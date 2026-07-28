# Phase 2 — the fifth correction pass

**Run** 2026-07-28 on top of HEAD `80fc03a0`, branch `spike/Precept-V2-Radical-reset`. Nothing here is
committed. No `.precept` file was compiled and no catalog, sample or compiler behaviour was cited.

**What this pass is.** It executes the four items
[`confirmation-run.md`](confirmation-run.md) § 5 listed as what would close conditions 3 and 4, minus
the nineteen fault-backstop rows, which stay a deliberately deferred group. It also writes down the
structural problem the two broken patterns are an instance of, because that problem outlasts these
corrections.

---

## 1. Two canon sites that were not rows, now rows and executed

### `primitive-types.md:203` — an overflow claim with no disclosure (`N101`)

The `integer + integer` row of the operator table read "Stays in lane. **Checked overflow.**" Its two
siblings both carry the deferral disclosure — `:197` ("that is the end state this section describes …
the compile-time proof that would deliver it is in scope and not yet implemented") and `:236` (the
same, on `decimal + decimal`). `N43`'s own reason column names `:203` as one of the two lines
deliberately left as the fixed-width end state, but `N43`'s site list is `:197`, `:236`, so nothing
reached it.

Two words made it worse than a bare aspiration. "Checked overflow" names a **runtime check**, which is
the mechanism `want:165` removes, so the line asserted the removed model as well as omitting the
disclosure. Under walk item 23 the aspiration stands; the claim that it holds today does not.

It now reads: "Stays in lane. Overflow is an error — the target, with the proof behind it in scope and
not yet implemented (see Open Questions)." That is the wording `:236` already carries, so the three
overflow claims in the file now say the same thing the same way.

### `compiler-and-runtime-design.md:137` — the disputed premise unmarked (`N102`)

Eight sites in canon carry the phrase "and nothing wider". Seven carry a marking saying the scope is
disputed and pointing at the note in spec §0.7 — `precept-language-spec.md:274`/`:276`,
`:1992`/`:1994`, `runtime-api.md:402`/`:404`, `result-types.md:152`/`:154`, `evaluator.md:738`/`:740`,
`:1385`/`:1387`, `compiler-and-runtime-design.md:151`. The eighth did not.

It is the worst one to miss. A reader who meets the architecture overview first meets the narrow scope
with no marking at all and takes it as settled canon, which is exactly what the other seven markings
exist to prevent.

The marking added is the inline form this document already uses at `:151`, so the two sites in the
same file read the same way:

> **"And nothing wider" is disputed and is not settled canon — see the note in spec §0.7,
> *Governance*, and do not build against this scope.**

**The disputed wording itself is untouched and the question is not resolved.** It is `N96`'s, `N96` is
owned by phase 5, and this row says so.

---

## 2. Three patterns rewritten, and one recorded count corrected

Every pattern below was run against the tree its correction landed on top of as well as at HEAD. A
pattern that returns zero at HEAD proves nothing unless it returned more than zero before.

| Row | Defect | Rewritten pattern | Before | After |
|---|---|---|---|---|
| `N74` | The recorded pattern escaped its pipes for the markdown table, so under `-F` the backslashes were literal characters and it matched nothing in any tree — including the one its own cell claimed a hit in | `grep -rnE '\(none\) . Period path . Duration path' $CANON --include=*.md` | **1** at `4ca55318^`, at `temporal-type-system.md:143` | **0** at HEAD |
| `N55` | An addition row that must return more than zero, returning zero because `3dbad40b` reworded "arrived through the contract" to "arrived through a declared operation" | `grep -rniE 'proven.{0,3} for data that arrived.{0,60}assumed.{0,3} for data that did not' $CANON --include=*.md` | **0** at `5e853c0e^`, before the addition | **1** at `5e853c0e`, `3dbad40b^`, `3dbad40b` and HEAD — it now survives the rewording that broke it |
| `N43` | A re-scope row that must return zero, returning one because `fa4891d7` reworded the disclosure it filters on from "deferred past the first release" to "in scope and not yet implemented" | `grep -rnPi 'Overflow is an? (?:type )?error(?!.{0,200}not yet implemented)(?!.{0,200}deferred)' $CANON --include=*.md` | **2** at `4ca55318^` — `:197` and `:236`, both bare | **0** at `84577ee1`, `fa4891d7` and HEAD, with `:203` now inside the set it covers |

**`N74`'s defect is a recording format problem, not a search problem.** A bare pipe ends a markdown
table cell, so somebody escaped the pipes to keep the table intact and the pattern stopped being a
pattern. The register already diagnosed and fixed this for `N15`; one row still carried it. The
rewrite replaces each pipe with `.` under `-E`, which keeps the cell intact without escaping anything.
`N43`'s rewrite does the same job with two chained negative lookaheads instead of a second `grep` on
the far side of a pipe.

**`N43`'s filter is now robust to either wording of the disclosure**, using two lookaheads rather than
one alternation. Writing the filter against only the wording of the day is what let it rot the first
time.

**`N15`'s recorded pre-count was wrong and is corrected.** The cell claimed **6** at `25894ae3^`; the
pattern returns **8** — six lines in `compiler-and-runtime-design.md` (`:851`, `:861`, `:881`, `:882`,
`:883`, `:899`) and two in `runtime-api.md` (`:273`, `:282`). Zero at HEAD either way, so the row's
verdict never changed; only its arithmetic was wrong.

**`N96`'s line citations are corrected.** `precept-language-spec.md:1976` and `:1978` were the numbers
before `46734adc` inserted the disputed note; the site and its marking now resolve at `:1992` and
`:1994`.

---

## 3. Four defects in the search procedure

### 3.1 `docs/tooling/` — the coverage claim was false, and the folder was carrying the removed model

§ 1 stated that `docs/language/README.md`, `docs/compiler/README.md`, `docs/runtime/README.md` and
`docs/tooling/README.md` "sit inside the canonical folders, so `$CANON` reaches them". `$CANON` is
four paths and `docs/tooling` is not one of them, nor was it in `$CANON_WIDE`. **Nothing in the
procedure searched the folder at all**, and the sentence that said otherwise was written by the third
pass to close an ambiguity two re-checks had raised.

The choice was to widen the file set or to declare the folder out of scope with a reason. Running the
searches over it settled that, because two things came back and neither is vocabulary reuse:

- **`language-server.md:661`** (row `N103`) — "Even though **a runtime overflow
  check exists**, the hover reflects static proof status. **The runtime check is a fallback, not a
  guarantee**." Two removed models in one sentence: `want:165` removes the runtime overflow guard, and
  `want:171` refuses deferral outright. The hover *routing* it describes is right and gets more right
  once there is no runtime check behind it; the reason given for the routing is the removed model. A
  tooling document reaches an author through hover text every time they open a file, which makes it a
  worse place to leave this than a design document.
- **`mcp.md:108`** (row `N104`) — still carries "the declared-bound value-safety codes", the phrase
  `N38` re-worded where the diagnostic system declares it. The correction did not reach the second
  copy because no grep covered the folder.

`docs/tooling` is therefore added to `$CANON_WIDE`, rows sited there are marked **WIDE**, and the § 7
resolver gained `docs/tooling/` in its prefix list — without it, both new sites report as missing
files.

**Neither row is executed.** Both corrections are wording changes with no design question in them, but
they land in a folder that was out of scope until this pass, and executing an unbriefed third and
fourth canon edit under cover of fixing a procedure defect is how scope quietly grows. They are
recorded so condition 4 can be met honestly, and left for the pass that takes `docs/tooling/` on
deliberately.

### 3.2 § 6 blind spot 0 — the widening instruction, carried out

The entry said "every other grep in § 3 still carries only one inflection and should be widened the
same way before the procedure is re-run", and nothing was widened. The fourth pass and the
confirmation run both ran `A2`–`A16` as written, which is what a re-runner does.

`A2`, `A4`–`A12` and `A14`–`A16` now carry their synonyms and inflections, and each records what the
addition was for. Two carry a stated reason for not needing it: `A3`, whose pattern is already a stem
covering certificate / certificates / certification / certified, with attestation, proof record and
signed proof returning nothing anywhere in the file set; and `A13`, which was widened with wraparound,
saturation and out-of-range and returned **zero** lines the original did not already return.

**Every widened pattern was run and diffed against its unwidened form, and the result is not what the
blind-spot entry predicted.** Across all fifteen, the synonym-and-inflection axis produced **no new
disagreement**. Every newly reached line was vocabulary reuse: `fall back` in a launch-mode selector,
`satisfiability` for the proof engine's own initial-state scan, `desugar` for the language's own
rule-shorthand model, `best-effort` for inspection, `ConstraintViolation` for the structured
explanation record.

One term was tried and deliberately left out. Adding `invariant` to `A9` returns **118** extra lines
in this file set — engine invariants, grammar invariants, slot-index invariants — and not one of them
is the induction. A term that produces a hundred discards makes the procedure harder to run without
making it find more, and that is recorded in `A9`'s comment so nobody adds it back.

So the two disagreements this pass found came from widening the **file set**, not from widening any
pattern. After four passes over these documents the vocabulary axis is close to exhausted; the
coverage that was actually missing was a folder.

### 3.3 § 5a — the unrunnable re-verify instruction

It said to re-verify every line of `sweep-correction-list.md` "against `git rev-parse HEAD` at the
moment you run". That cannot be carried out. The file records where a sentence sat at
`3f69b316`, more than a dozen commits have moved the canonical documents since, and in
several cases this phase's own corrections edited or removed the sentence the line number pointed at.
The instruction never said what to do when a line had moved, so a re-runner following it re-verified
nothing.

It is replaced with an instruction that can be followed: **do not resolve that file's line numbers
against any HEAD.** Every one of its rows was folded into the register and re-verified there; the
register's rows are the current record of where each site is and whether it was executed, and where
the two disagree the register is right. The correction list is an input already consumed, not a live
checklist. The one part of it that is still live — its "Reconciliation against the 2026-07-26 owner
rulings" section, which supersedes the stated direction of S1, R3, S11, S12 and S13 — is kept, because
executing a superseded direction carries a model into canon that the owner has since replaced.

### 3.4 `N96`'s stale line citations

Corrected. See § 2.

---

## 4. The structural problem, which outlasts this pass

Condition 3 asks that each row's grep pattern returns zero. Two patterns broke on 2026-07-28, and
neither correction was wrong — `3dbad40b` and `fa4891d7` both did exactly what their rows asked, and
in doing so reworded sentences that *other* rows' patterns quoted. The corrections were fine. The
patterns rotted.

That will keep happening. Every future correction can invalidate a pattern written earlier, and
nothing re-runs the register when canon changes. So condition 3 cannot be satisfied permanently by
literal greps against documents that keep being edited. It can only hold **as of a stated commit**.

This is now written into the plan's phase-2 "Done when" as a qualification on condition 3 itself, and
into the search procedure's § 7. The two working defences, both applied here:

1. **Re-run every row's pattern after any correction pass**, not only the patterns belonging to the
   rows that pass touched. Both breakages were in rows nobody was working on.
2. **Quote the least changeable words that carry the claim**, not the whole sentence. `N55`'s rewrite
   drops the clause that had already been reworded twice and keeps only "proven for data that arrived
   … assumed for data that did not", which holds across `5e853c0e`, `3dbad40b` and HEAD. `N43`'s
   rewrite accepts either wording of the disclosure rather than the wording of the day.

Neither defence makes the condition permanent. They make it cheaper to re-establish.

---

## 5. Verification

Every pattern this pass wrote or rewrote was run in the tree its correction landed on top of and in
the tree that has it, and every one behaves as its cell now states:

| Row | Pre-correction tree | Result there | At the working tree |
|---|---|---|---|
| `N74` | `4ca55318^` | 1 | 0 |
| `N15` | `25894ae3^` | 8 | 0 |
| `N55` | `5e853c0e^` | 0 (addition not yet made) | 1 |
| `N43` | `4ca55318^` | 2 | 0 |
| `N101` | `80fc03a0` | 1 | 0 |
| `N102` | `80fc03a0` | 0 (addition not yet made) | 1 |
| `N103` | — not executed | — | 1, as the row states |
| `N104` | — not executed | — | 1 under `$CANON_WIDE`, 0 under `$CANON` |

**The two canon edits were checked against every other recorded pattern.** Each `grep` command in the
register that runs over `$CANON` or `$CANON_WIDE`, plus the two that name an edited file directly, was
run against the HEAD version and the edited version of `primitive-types.md` and
`compiler-and-runtime-design.md` and its hit count compared. **One count changed**, and it is the
intended one: `N43`'s bare pattern goes from 2 to 3 as `:203` joins the disclosed set, and `N43`'s
rewritten filter returns zero over all three.

The § 7 resolver runs clean: zero missing files and zero out-of-range citations across every
`file.md:NNN` in the register, with `docs/tooling/` added to its prefix list. The identifier count is
**150**, the distinct-row count **145**, and both are updated in the register's header and § 16.

**What this pass did not do.** The nineteen fault-backstop rows are still unexecuted and condition 3
still cannot pass while they stand — that is the board position, not a surprise. `N103` and `N104` are
recorded and unexecuted. The full 100-pattern re-run of the register at the working tree was not
repeated end to end; what was repeated is the subset this pass could have disturbed, by the
file-scoped diff described above.
