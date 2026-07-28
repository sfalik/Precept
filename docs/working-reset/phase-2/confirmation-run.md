# Phase 2, conditions 3 and 4 — confirmation run

**Run** 2026-07-28 at HEAD `80fc03a0`, branch `spike/Precept-V2-Radical-reset`, by a reader who did
none of the correction work and did not write the search procedure. Nothing was edited in a canonical
document and nothing was committed. No `.precept` file was compiled.

**What was run.** Every grep pattern recorded in `contradiction-register.md`, at HEAD and again in the
tree the row's correction landed on top of (`git grep <pattern> <commit>^`). Then the search
procedure's §3 and §4 greps as written, triaged against the register. Then the want document read end
to end against the canonical documents.

**Verdict: neither condition holds yet.** Condition 3 fails on three counts — two stale patterns and
the nineteen fault-backstop rows nobody has executed. Condition 4 finds two disagreements that are not
rows, both small. Everything else checks out: 78 executed rows return exactly what their row claims,
at HEAD and in the pre-correction tree, in the file the row names.

---

## 1. The pre-correction test

The test: a pattern that returns zero at HEAD proves nothing unless it returned more than zero in the
tree the correction landed on. 100 patterns were run both ways. **One fails.**

### N74 — the pattern never matched anything, in any tree

Recorded as `grep -rnF '(none) \| Period path \| Duration path' $CANON`. Under `-F` the backslashes
are literal characters, so the pattern cannot match a markdown table row, which has bare pipes. The
row's own cell claims "**1** at `4ca55318^`, **0** at HEAD"; the first half does not reproduce — the
recorded pattern returns **0 at `4ca55318^` as well**.

Dropping the two backslashes gives 1 at `4ca55318^` (`temporal-type-system.md:143`) and 0 at HEAD.

**The correction landed** — verified by reading, not by pattern. `temporal-type-system.md:143` now
reads `| (none) | Compile error — no context, no type | Compile error — no context, no type | TEMP005
compile error | N/A |`, and `:145` carries the reconciliation the row asked for: "**No expected type
is always a compile error.** The `(none)` row above agrees with the type-resolution rules further
down".

This is the same defect the register diagnosed and fixed for `N15` — a bare pipe ends a markdown table
cell, so the pattern got escaped and stopped being a pattern. One row still carries it.

*Related bookkeeping, not a failure:* `N15`'s corrected pattern is claimed to return **6** at
`25894ae3^`. It returns **8**. Zero at HEAD either way.

### Everything else passes the pre-correction test

For every other executed deletion row the pattern returns more than zero before the correction and zero
after, and the pre-correction hits sit in the file and at the line the row cites. Spot-checked
explicitly on twenty rows across all five correction commits (`5e853c0e`, `c3a9be21`, `25894ae3`,
`d32d8e26`, `4ca55318`, plus the third and fourth passes at `84577ee1` and `fa4891d7`): every hit
resolved to the row's own site.

---

## 2. Condition 3 at HEAD

### Two executed rows whose patterns now fail

Both were broken by a commit made *after* the pass that wrote the pattern, and neither pass re-ran the
patterns afterwards. In both cases **the correction is intact and the pattern is stale** — verified by
reading the canonical text.

| Row | Kind | Required | Actual at HEAD | Cause |
|---|---|---|---|---|
| `N55` | ADDITION | more than zero | **0** | `3dbad40b` reworded "arrived through the contract" to "arrived through a declared operation". The pattern `proven.{0,80}through the contract and .{0,12}assumed` no longer matches. |
| `N43` | RE-SCOPE | zero | **1** | `fa4891d7` reworded the disclosure at `primitive-types.md:236` from "deferred past the first release" to "in scope and not yet implemented". The pattern's filter `grep -viE 'end state\|deferred\|§0\.7'` no longer catches it. |

`N55`'s addition is present at `precept-language-spec.md:296`: "Every proof that leans on 'this rule
already held before the operation' is *proven* for data that arrived through a declared operation and
*assumed* for data that did not." `N43`'s disclosure is present at `primitive-types.md:236`: "Overflow
is an error — the target, with the proof behind it in scope and not yet implemented, as for
`integer`."

The pattern of failure is worth naming, because it is the third time it has happened in a different
disguise: a correction pass rewords a sentence a *different* row's pattern was written against, and
nothing re-runs the register. The register's own header warns that its build baseline is stale; the
patterns have the same problem and no check catches it.

### Nineteen rows deliberately unexecuted

The fault-backstop family. Each pattern returns non-zero at HEAD, which is what the row says it will.

`R9` 25 · `N22` 3 · `N23` 9 · `R10` 1 · `N24` 15 · `N25` 2 · `R14` 2 · `N26` 25 · `R11` 3 · `R12` 3 ·
`N27` 2 · `R13` 1 · `N28` 1 · `N29` 2 · `C1` 34 · `C2` 2 · `N30` 4 · `N34` 1 · `N35` 2

Condition 3 as written cannot pass while these stand. That is the plan's own board position — the
family is listed as phase 2's open work — so it is a statement of where the phase is, not a surprise.

Open by design and not counted against the condition: `N8`, `N10`, `N66`, `N70` (the addition has not
been made and the row says so), `N79`–`N82`, `N96`, `N100`.

### The rest

78 executed rows pass. That includes every re-scope and confirm row whose pattern could not be
expressed as a simple string and had to be run by hand — `S7`, `N9`, `N15`, `N36`, `N40`, `N41`, `N42`,
`N57`, `N99`, `R8`, `N44`, `N67`. Each returns the count its cell states.

The procedure's §7 resolver script runs clean: zero missing files, zero out-of-range citations across
every `file.md:NNN` in the register. The identifier count is **146**, matching §16's note.

---

## 3. Condition 4 — re-running the search procedure

§1.0's family test returns **33** at HEAD, exactly the number the register accounts for, and every line
falls into one of the groups the register enumerates. `A1`–`A16` and §4's three absence greps produce
no assertion of a removed model that is not already a row, with two exceptions.

### Disagreement 1 — `primitive-types.md:203`, an overflow claim with no disclosure

> `| integer + integer | integer | Stays in lane. Checked overflow. |`

A bare present-tense claim that integer addition is overflow-checked, with nothing attaching it to the
deferral. Its two siblings both carry the disclosure: `:197` ("that is the end state this section
describes … the compile-time proof that would deliver it is in scope and not yet implemented") and
`:236`. `N43`'s own reason column names this line — "`:197` and `:203` were deliberately left as the
fixed-width end state" — but the row's site list is `:197`, `:236`, so nothing corrected it.

Under walk item 23 the aspiration may stand; what may not stand is a claim that it holds today, and
"Checked overflow" reads as exactly that. It also names a runtime check that `want:165` removes. Small,
mechanical, and one line.

### Disagreement 2 — `compiler-and-runtime-design.md:137`, the disputed premise unmarked

Eight sites in canon carry the exact phrase "and nothing wider". Seven carry the disputed marking
`N96` records — `precept-language-spec.md:274`/`:276`, `:1992`/`:1994`, `runtime-api.md:402`/`:404`,
`result-types.md:152`/`:154`, `evaluator.md:738`, `:1385`/`:1387`, `compiler-and-runtime-design.md:151`.
The eighth does not:

> `compiler-and-runtime-design.md:137` — "It is the field's own declared contract and nothing wider: no
> second pass re-checks the finished configuration after the mutations complete…"

No marking within the paragraph or the four lines after it. A reader who meets the architecture
overview first takes the narrow scope as settled canon, which is precisely what the other seven
markings exist to prevent. This is not a challenge to the deferral — `N96` is correctly recorded and
correctly routed to phase 5 — it is one site the marking pass missed.

Found by reading rather than by grep: `:137` uses none of `A1`–`A16`'s vocabulary. That is blind spot 1
firing, measured rather than predicted.

### Considered and not a disagreement

- **`docs/tooling/`** — one hit (`mcp.md:110`, "defense-in-depth" about MCP tool-wrapper error
  handling). Vocabulary reuse in an unrelated context, not the runtime backstop model.
- **Blind spot 2 re-verified independently.** `docs/README.md`, `docs/agent-onboarding.md` and
  `docs/glossary.md` return zero hits for the whole removed-model vocabulary. The procedure's claim
  that this is closed holds.
- **Ingress point count.** `want:141` says ingress is exactly two points; canon enumerates three —
  event arguments, construction inputs, an update patch. Construction inputs are the arguments of the
  initial event, so the enumerations agree; not a disagreement.
- **`Unmatched` versus `Rejected`.** `want:154` reserves explanation for resolvable refusals. Canon
  agrees (`precept-language-spec.md:1942`, `:1951`); the one departure, construction with no matching
  row producing `Rejected` rather than `Unmatched`, is explained in canon's own voice at
  `runtime-api.md:151` and `:176` and is a consequence of construction having no "not offered" case.
- **Diagnostic code counts.** `runtime-api.md:674` says 132, `diagnostic-system.md:420` says 164. An
  intra-canon numeric disagreement, but a build-status claim of the same class §14.4 excludes for
  `README.md:110`, and the documentation-sync rule already governs it. Recorded here so it is not
  re-found as new.

---

## 4. Defects in the search procedure itself

A procedure a second person cannot follow identically has failed its own purpose. Three problems, in
descending order of consequence.

1. **§1 claims coverage it does not have.** It states that "`docs/language/README.md`,
   `docs/compiler/README.md`, `docs/runtime/README.md` and `docs/tooling/README.md` sit inside the
   canonical folders, so `$CANON` reaches them." `$CANON` is `docs/language docs/compiler docs/runtime
   docs/philosophy.md`. **`docs/tooling/` is in neither `$CANON` nor `$CANON_WIDE`**, and nothing in
   the procedure searches it. The paragraph was added by the third pass to close an ambiguity two
   re-checks had raised, and it closed it with a false statement. I searched the folder by hand and
   found nothing substantive, so the coverage gap costs nothing today — but the sentence should say
   either that `docs/tooling/` is out of scope with a reason, or add it to the file set.

2. **§6 blind spot 0 records an instruction that was never carried out.** "Every other grep in § 3
   still carries only one inflection and should be widened the same way before the procedure is
   re-run." `A2`–`A16` are unchanged. I ran them as written, which is what a re-runner would do, and
   the one disagreement they missed (`compiler-and-runtime-design.md:137`) is a synonym miss of exactly
   the kind that note predicts.

3. **§5a's HEAD instruction is unrunnable as written for `sweep-correction-list.md`.** It says to
   re-verify every line "against `git rev-parse HEAD` at the moment you run". That file's line numbers
   were verified at `3f69b316` and eight canonical-document commits have landed since; a
   re-runner following the instruction re-verifies nothing, because the instruction does not say what
   to do when a line has moved. The register's own numbers were rebuilt and are fine; the correction
   list's are not, and it is still cited as the phase's starting point.

Minor, and worth fixing while the file is open: `N96` cites `precept-language-spec.md:1976` and the
marking at `:1978`. Both moved when `46734adc` inserted the disputed note; they resolve at `:1992` and
`:1994`.

---

## 5. What would close both conditions

1. Execute the nineteen fault-backstop rows, or record the family as deliberately deferred past phase 2
   with the owner's word, and say so in the condition rather than leaving nineteen patterns returning
   non-zero.
2. Re-write `N55`'s and `N43`'s patterns against the text as it now stands, and `N74`'s without the
   backslashes.
3. Correct `primitive-types.md:203` alongside `N43`'s other two sites, and add the disputed marking to
   `compiler-and-runtime-design.md:137`. Two lines.
4. Fix the three procedure defects in §4 above.

The register's substance is in good shape. Every failure found by this run is a pattern, a line number,
or a site the marking pass missed — not a resolution done wrong. That is a different picture from the
first two re-checks, and it is the picture the fourth pass predicted.
