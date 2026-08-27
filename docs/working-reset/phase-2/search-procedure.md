# The phase-2 search procedure

**What this is.** The written, re-runnable procedure that finds the places where the canonical
documents on this branch and the want document say different things. Phase 2's fourth finish-line
condition is that somebody who did not write this procedure can run it at HEAD and produce no
disagreement that is not already a row in
[`contradiction-register.md`](contradiction-register.md).

**Written and first run** 2026-07-27, at HEAD `b93d1db1` on `spike/Precept-V2-Radical-reset`.

**What it may cite.** The canonical documents at HEAD on this branch, the want document
(`docs/Working/what-i-want-2026-07-16.md`), and the phase-1 register
(`docs/working-reset/phase-1/decision-register.json`). Not the catalogs, not the samples, not
compiler behaviour. Nothing is compiled.

---

## 0. Set up

Run every command from the repository root. Two shell variables carry the file set, and every
command below uses one of them:

```bash
cd "$(git rev-parse --show-toplevel)"

# The canonical set the finish-line grep in the plan uses.
CANON="docs/language docs/compiler docs/runtime docs/philosophy.md"

# The wider set this procedure actually searches. See § 1 for why it is wider.
# docs/tooling was added 2026-07-28, fifth pass, after the folder turned out to be searched by
# nothing at all. See § 1.
CANON_WIDE="docs/language docs/compiler docs/runtime docs/philosophy.md README.md docs/compiler-and-runtime-design.md docs/tooling"

WANT="docs/Working/what-i-want-2026-07-16.md"

git rev-parse HEAD          # record this in the register's header
```

## 1. Which files count as canonical, and one deliberate widening

`CLAUDE.md` names the canonical documents as `docs/language/`, `docs/compiler/`, `docs/runtime/`,
`docs/philosophy.md` and `README.md`. The plan's finish-line grep (condition 3) covers only the
first four — it leaves out `README.md`.

This procedure searches two files and one folder beyond the plan's grep set, and all three are
deliberate:

- **`README.md`** — the documentation-sync table in `CLAUDE.md` makes it a document that must never
  carry a claim as implemented when it is not. The 2026-07-19 sweep checked it and found no
  corrections; this procedure re-checks it rather than inheriting that result.
- **`docs/compiler-and-runtime-design.md`** — `CLAUDE.md` lists it as the architecture-overview entry
  point, `precept-language-spec.md:274` delegates the description of how discharge and governance
  compose to it, and it was **never read** by the 2026-07-19 sweep (recorded as a coverage gap in
  `sweep-correction-list.md` § 3). It is also the file the plan's own record of the 2026-07-26 walk
  says phase 2 must edit (`compiler-and-runtime-design.md:125`). Searching it and then having no way
  to check the row was executed would be worse than not searching it, so:
- **`docs/tooling/`** — added 2026-07-28, fifth pass. Until then this section claimed `$CANON`
  reached `docs/tooling/README.md`, which was false: `$CANON` is four paths and `docs/tooling` is not
  one of them, so nothing in this procedure searched the folder at all. The claim was written by the
  third pass to close an ambiguity two re-checks had raised, and it closed it with a statement that
  was not true. Adding the folder was chosen over declaring it out of scope, because running the
  searches over it found two things and neither is vocabulary reuse: `language-server.md:661` states
  that a runtime overflow check exists and describes it as a fallback the hover must not present as
  proof, which is the mechanism `want:165` removes and the deferral `want:171` refuses; and
  `mcp.md:108` still carries the phrase `N38` re-worded in `diagnostic-system.md`. Register rows
  `N103` and `N104`. A folder that carries the removed model in two files is not a folder that can be
  declared out of scope on the grounds that it carries no normative claim.

**Consequence for the finish-line check.** Rows whose site is in `README.md`,
`docs/compiler-and-runtime-design.md` or `docs/tooling/` must have their recorded grep pattern run
against `$CANON_WIDE`, not `$CANON`. The register marks those rows. This is a widening of the plan's
condition 3, not an exception to it.

**Both variables above were checked at `3a561dad` by the condition-3 re-run and both are correct as
written** — `$CANON_WIDE` carries `docs/tooling` and the § 7 resolver carries it too. What was wrong
was the *second copy* of the definition: `contradiction-register.md` § How to read a row spelled
**WIDE** out as `$CANON` plus `README.md docs/compiler-and-runtime-design.md`, with no
`docs/tooling`, and that is the copy a re-runner working row by row reads. The re-run corrected it
there. The lesson is the one this file already applies to the canonical documents: a definition
written down twice drifts, and the copy that drifts is the one nobody re-reads.

Excluded from the search, and each for a stated reason:

| Path | Why excluded |
|---|---|
| `docs/archive/` | Superseded specs; `CLAUDE.md` says reference only, never update. |
| `docs/Working/` | In-flight proposals, not canon. The want document lives here and is the other side of every comparison, not a target of correction. |
| `docs/README.md`, `docs/agent-onboarding.md`, `docs/glossary.md` | Navigation and vocabulary. They carry no normative claim about the proof-versus-governance boundary. **Known blind spot** — see § 6. |
| `src/`, `tools/`, `test/`, `samples/`, the catalogs | Excluded by the plan's "what it may cite" rule. |
| `.squad/`, `CLAUDE.md`, `CONTRIBUTING.md` | Operational and instruction files, corrected by phase 0. |

**The per-area READMEs are searched, and the file set they are searched under is stated exactly.**
`docs/language/README.md`, `docs/compiler/README.md` and `docs/runtime/README.md` sit inside the three
canonical folders, so `$CANON` reaches them; `docs/tooling/README.md` does not, and is reached by
`$CANON_WIDE` only since the fifth pass added the folder. The exclusion row above excludes only
`docs/README.md` at the top level. Both independent re-checks raised this as ambiguous, and both were
right that the table did not say so. It says so now, because `docs/runtime/README.md:23` carried the
removed model through two re-checks with no row against it (register `N88`).

## 2. Build the claim list from the want document

The want document is 221 lines. Read it end to end — not grepped — and write down every sentence that
asserts something about how Precept works, as opposed to describing the ideal-world framing or the
worked example's mechanics. Each becomes a numbered claim with its want line number.

The claim list produced on 2026-07-27 is below. Re-deriving it should reproduce these twenty; if a
re-run produces a claim that is not here, that is a finding about this procedure and it gets added.

| # | Claim | want lines |
|---|---|---|
| W1 | The compiler proves or rejects **all** business rules. | 14, 24 |
| W2 | "Prove" means proof by induction over reachable configurations: a base case and an inductive step. | 16–19 |
| W3 | The inductive step is provable from exactly four premise classes — field modifiers, arg constraints, the handler's guard, and all rules holding in the pre-state. | 19 |
| W4 | A rule's obligations attach to **every** write site of **every** field the rule mentions. | 20 |
| W5 | Modifiers are sugar for rules; there is one constraint mechanism underneath. | 22 |
| W6 | The premises are what the runtime enforces — governance, not deferred proof. | 26 |
| W7 | A non-linear post-state condition is proven only from a guard or arg constraint that makes it directly checkable; otherwise the compiler rejects. Never a solver, never a post-mutation sweep. | 135 |
| W8 | The compiler requires the author to extend constraints to the entry points. | 139 |
| W9 | Ingress is exactly two points: event args and editable-field writes. Restore is **not** ingress — persisted data is trusted and unchecked. | 141 |
| W10 | Structural defects — an unreachable state, an unmarked dead end, a dead row — are **rejected**. | 151–155 |
| W11 | Absence of a transition row is the enforcement; inapplicability is not a refusal. | 153–154 |
| W12 | The runtime is lighter: fault-prevention checks are **omitted** at evaluation time. No divide-by-zero tests, no overflow guards, no re-checking of proven bounds on writes. | 165 |
| W13 | What runs at runtime is the declared premises and **nothing beyond them**. | 165 |
| W14 | No deferral: a rule the compiler cannot prove is rejected, never handed to the runtime. | 171 |
| W15 | The guarantee is uniform — no author-facing distinction between a compile-time-checked rule and a runtime-governed one. | 173 |
| W16 | A rejection suggests the missing premise with a teachable message, and never inserts it. | 179 |
| W17 | Warnings where there should be errors are dilution, not balance. | 189 |
| W18 | The proof boundary is the single precept; cross-precept is out of MVP scope. | 191–195 |
| W19 | The runtime does **not** re-evaluate rules against the post-mutation configuration. The compiler emits a certificate; one checker runs at two call sites, one of them a load-time gate that refuses to govern under a definition whose certificate does not verify. | 199–204 |
| W20 | The certificate's premise list **is** the runtime's validation surface, and inspect can cite provenance. | 210–211 |

Two want items are **open questions the document declares open** (want:219 time-referencing rules,
want:220 the promise across precepts). Canon saying something about either is not a disagreement — it
is canon answering a question the want document left open, and it goes to "Waiting on Shane" rather
than into the register.

## 3. The removal search — canon asserting a mechanism the want document removes

For each claim, grep the canonical set for the vocabulary that the *contradicting* model uses. These
are the exact commands run on 2026-07-27. Each is written to over-return: reading and discarding a
false hit is cheap, and a pattern narrow enough to return only true hits is a pattern that was
written after the answer was known.

```bash
# A1 — W19, W12: the post-mutation constraint sweep
# Corrected 2026-07-27, third pass. Every term below used to be a noun phrase, so a document saying
# "the evaluator evaluates constraints" was invisible to it. Three runtime documents kept specifying
# the removed model in the verb form and survived two full re-checks because of this. Every idea in
# this procedure needs its verb, its noun and its passive spelling.
# Widened again 2026-07-27, fourth pass. Three inflections were still one word short: proof-engine.md
# specified the removed model twice as "constraint re-check", which matches none of them. An idea
# needs its synonyms too, not only its inflections. This is the same test as the register's § 1.0.
grep -rnEi 'post-mutation|the sweep|sweep governs|swept|constraint (evaluation|plans? evaluated)|evaluat(e|es|ed|ing) (all |every |the |each )?constraints?|constraints? (are |is )?evaluat(e|es|ed|ing)|re-?(check|evaluat|validat|verif|govern|prov)[a-z]*[^.]{0,60}(constraint|rule|ensure)|(constraint|rule|ensure)[a-z]*[^.]{0,60}re-?(check|evaluat|validat|verif|govern|prov)' $CANON_WIDE --include=*.md

# A2 — W12, W13: runtime fault traps and defence-in-depth
# Widened 2026-07-28, fifth pass: a backstop is also a trap, a guard and a safety net.
grep -rnEi 'backstop|defense-in-depth|defence-in-depth|defensive redundancy|belt-and-suspenders|belt and braces|fault trap|FaultSite|safety net|last line of defen|redundant check|defensive check|trap the fault|guard against (the )?(overflow|division|fault)' $CANON_WIDE --include=*.md

# A3 — W19, W20: the certificate and its load-time gate (searched for ABSENCE, see § 4)
# Not widened, and it does not need to be: `certificat` is a stem, not an inflection of a phrase, and
# it matches certificate, certificates, certification and certified. The words a writer would reach
# for instead — attestation, proof record, signed proof — return nothing anywhere in the file set.
grep -rnEi 'certificat' $CANON_WIDE --include=*.md

# A4 — W14: deferral
# Widened 2026-07-28, fifth pass: verb and passive spellings of handing work to the runtime.
grep -rnEi 'defer(red|s|ral)? to (a |the )?runtime|handed to the runtime|runtime check|fallback|falls? back|left to the runtime|caught at runtime|catch(es)? (it )?at runtime|the runtime catches|runtime guard|runtime safety|enforced at runtime|checked at runtime|re-?checked at runtime' $CANON_WIDE --include=*.md

# A5 — W8, W9: ingress, and whether canon names more than two ingress points
# Widened 2026-07-28, fifth pass: canon does not always use the word.
grep -rnEi 'ingress|entry point|on the way in|as (it|the value|values) enters?|at the boundary|point of entry' $CANON_WIDE --include=*.md

# A6 — W9: restore re-validating
# Widened 2026-07-28, fifth pass: hydration is the other word for restore, and "check" belongs
# alongside validate/constrain/govern/trust.
grep -rnEi 'restor(e|ed|ation).{0,80}(valid|constraint|govern|trust|check)|(re-?)?hydrat[a-z]*.{0,80}(valid|constraint|govern|trust|check)|load(ed|ing)? (state|configuration).{0,80}(valid|constraint|govern|check)' $CANON_WIDE --include=*.md

# A7 — W10, W17: structural defects carried at Warning severity
# Widened 2026-07-28, fifth pass: the ways a document says "we notice it but do not stop".
grep -rnEi 'severity.{0,30}warning|Warning, not error|stays? .?Warning|advisory|report \(|hygiene|informational|non-fatal|does not fail|lint|suggestion only|reported rather than rejected|flagged rather than' $CANON_WIDE --include=*.md

# A8 — W7: solvers, and non-linear reasoning
# Widened 2026-07-28, fifth pass: named solvers, and the shape described without the word.
grep -rnEi 'SMT|solver|non-linear|nonlinear|satisfiabilit|Z3|constraint solving|decision procedure|quadratic|variable times a variable|two variables multiplied' $CANON_WIDE --include=*.md

# A9 — W2: induction
# Widened 2026-07-28, fifth pass. `invariant` was tried and deliberately left out: it returns 118
# extra lines in this file set, every one of them engine invariants, grammar invariants or slot-index
# invariants, and none of them the induction. A term that produces a hundred discards makes the
# procedure harder to run without making it find more.
grep -rnEi 'inductive|induction|base case|preserv(e|es|ation)|establish(es|ment)?|inductive hypothesis|holds before|holds after|every reachable configuration|initial configuration' $CANON_WIDE --include=*.md

# A10 — W4: write sites and symmetric obligations
# Widened 2026-07-28, fifth pass: assignment and mutation are the other words for a write.
grep -rnEi 'write site|every write|symmetric|assignment site|mutation site|each write|every place .{0,40}(written|writes)|all write' $CANON_WIDE --include=*.md

# A11 — W5: modifiers as rule shorthand
# Widened 2026-07-28, fifth pass: desugar is the word canon actually uses.
grep -rnEi 'sugar for|rule shorthand|shorthand for a rule|one constraint mechanism|desugar|sugar over|equivalent to a rule|single constraint mechanism|same mechanism as a rule' $CANON_WIDE --include=*.md

# A12 — W15: the guarantee split by provenance rather than uniform
# Widened 2026-07-28, fifth pass: including the unhyphenated spelling, which the hyphenated pattern missed.
grep -rnEi 'provenance|where the value comes from|externally-supplied|externally supplied|governed at runtime|source of the value|depends on where the value|from outside the definition|author-supplied' $CANON_WIDE --include=*.md

# A13 — W12: overflow named among the faults a clean compile rules out
# Widened 2026-07-28, fifth pass and it changed nothing: wraparound, saturation and out-of-range
# return zero lines this pattern did not already return. `overflow` is one word for one idea here.
grep -rnEi 'overflow|wraparound|wrap(s|ped)? around|saturat|out of range|exceeds? the (declared )?(range|width)' $CANON_WIDE --include=*.md

# A14 — W18: cross-precept scope
# Widened 2026-07-28, fifth pass.
grep -rnEi 'cross-precept|saga|message passing|another precept|multi-precept|across precepts|other precept|precept-to-precept|between precepts' $CANON_WIDE --include=*.md

# A15 — W19: the outcome taxonomy that presupposes the sweep
# Widened 2026-07-28, fifth pass. These are code identifiers, so inflection does not apply to them;
# what does apply is the neighbouring vocabulary a document uses when it describes the same outcome
# in prose, so `ConstraintViolation` and the two prose spellings of constraint failure are added.
# Note this returns 42 more lines and every one read so far is the structured-violation record, which
# survives — it reports an ingress failure, not a post-mutation one.
grep -rnE 'ConstraintsFailed|ConstraintViolation|constraint failure|constraint-failure' $CANON_WIDE --include=*.md

# A16 — W1, W16: prove-or-reject stated as never rejecting
# Widened 2026-07-28, fifth pass: the ways a document says "it accepts this rather than refusing".
grep -rnEi 'never rejects|does not reject|unfoldable default|unknown/unfoldable|accepts? (it )?anyway|compiles? anyway|permissive|best.effort|skips? the obligation|without rejecting|rather than reject' $CANON_WIDE --include=*.md
```

**What the widening found, measured 2026-07-28 on the fifth pass.** Every widened pattern was run and
diffed against its unwidened form, so only the lines the widening newly reached had to be read. The
result is worth stating plainly, because it is not what blind spot 0 predicted: **across `A2`–`A16`,
the synonym-and-inflection widening produced no new disagreement at all.** Every new line was
vocabulary reuse in an unrelated context — `fall back` in a launch-mode selector, `satisfiability` for
the proof engine's own initial-state scan, `desugar` for the language's own rule-shorthand model,
`best-effort` for inspection, `invariant` for engine and grammar invariants, `ConstraintViolation` for
the structured explanation record. The two disagreements this pass found came from widening the
**file set** to `docs/tooling/`, not from widening any pattern. That is a measurement about where the
remaining risk sits: after four passes over these documents the vocabulary axis is close to exhausted,
and the folder axis was not being worked at all.

**How to read a hit.** A hit is a candidate, not a row. Open the line, read the paragraph around it,
and place it in one of four buckets:

1. **Contradicts a want claim** — becomes a row. Record the verbatim canonical sentence, its file and
   line, the want line it contradicts, the resolution, and the reason.
2. **Restates a want claim** — discard. Much of the "no deferral" and "prove-or-reject" language in
   canon agrees with the want document word for word.
3. **Neither** — the hit is vocabulary reuse in an unrelated context (`Fault` as an engine-invariant
   variant, `backstop` as a variable name in a code block). Discard.
4. **Canon contradicting canon** — becomes a row, marked as an intra-canon row. Same repair, different
   kind of disagreement; see § 5.

## 4. The absence search — what the want document requires and canon does not say

The removal search cannot find a claim canon never made. Three want claims require canon to say
something new, so each is checked by a grep that is expected to return **nothing**, and a nil return
is the finding:

```bash
# W19 — the runtime load-time certificate gate. Expected: zero hits in docs/runtime/.
grep -rniE 'certificat' docs/runtime --include=*.md

# W2 — the induction, stated as the compiler's proof model.
grep -rniE 'proof by induction|inductive step|base case.{0,40}(rule|configuration)' $CANON_WIDE --include=*.md

# W3 — the four premise classes, named as a closed set.
grep -rniE 'premise class|four premise' $CANON_WIDE --include=*.md
```

A fourth absence is carried from the plan rather than found by grep: the plan's "Known risks" section
records that the induction's base case is *assumed* for restored and host-injected data and that this
assumption is written down nowhere a reader of the guarantee would find it. That is an addition row.

## 4b. The canon-against-canon search

**Added 2026-08-27.** Everything above starts from a claim in the want document, which is what this
phase was scoped to until the owner widened it. The wider scope needs a search that starts nowhere near
the want document: two canonical documents disagreeing with each other is a defect on its own account,
because if canon has no single position then "canon agrees with the want document" is not a claim that
can be checked at all.

**It cannot be written as a grep list.** There is no claim list to start from, and a reader is not a
procedure — one careful reader found twelve of these and would not find the same twelve twice. What
replaces both is a set of **mechanical checks**, each one taking a kind of fact that canon states more
than once and comparing the statements. A check reads the whole canonical set, so no document is
favoured and no reader's attention decides the coverage.

**Check 1 — types declared in more than one document.** Built and run 2026-08-27.
[`checks/declared-types.py`](checks/declared-types.py), run from the repository root. It walks every
fenced code block in the canonical set, extracts each type declaration and its member names, and prints
every type name whose member sets are not identical across the documents declaring it. The run is
written up in
[`canon-against-canon-check-1-declared-types.md`](canon-against-canon-check-1-declared-types.md): 235
type names declared, 87 in more than one document, 20 with differing members, of which twelve are
genuine and the rest are marked abbreviations. **What the check cannot do is separate those two**, so its
output is a candidate list that has to be read. It flags a declaration carrying an elision marker; nothing
else is decided for you.

**Checks 2, 3 and 4** were built the same day and are written up in
[`canon-against-canon-checks-2-4.md`](canon-against-canon-checks-2-4.md):

- **Check 2 — counts, against the set and against each other.**
  [`checks/stated-counts.py`](checks/stated-counts.py). De-camel-cases each declared enum name into the
  phrase prose uses for it and compares every number canon states for that phrase against the member
  count, then reports countable nouns given more than one number. Three new findings; reproduced the
  147-against-164 and the eight-against-nine variant counts.
- **Check 3 — identifiers used as though declared, that nothing declares.**
  [`checks/undeclared-identifiers.py`](checks/undeclared-identifiers.py). Produces *completeness gaps*
  rather than disagreements, and the write-up says so; kept for whoever writes the corrected statement of
  the runtime. Read `csharp` fences only — its first run read `.precept` examples as C# and returned an
  author's field names.
- **Check 4 — a diagnostic code against the stage document that would emit it.**
  [`checks/code-stage-attribution.py`](checks/code-stage-attribution.py). Prints per-stage coverage,
  because a stage naming every code it emits and missing one is a finding while a stage naming almost none
  never enumerated them. One finding, already known, and nothing new.

**Still not built.** An ordering or numbering stated more than once — `precept-builder.md` numbers its own
passes one way in a list and the other way in a diagram, a dependency table and two section headings.
That one is still a reader's finding.

**Re-running.** Each check is a script and reproduces its own output. A finding it produces goes into the
register's § 12 like any other, and carries no want line, which is the whole point of the shape.

## 5. Fold in the two committed inputs

Neither of these is produced by the greps above, and both must be merged before the register is
complete.

**5a. `sweep-correction-list.md`** — 25 verified rows plus one clarity-axis row, each with a per-file
line number re-verified at HEAD `3f69b316`.

**Its line numbers are historical and must not be re-resolved. Corrected 2026-07-28, fifth pass.**
This section used to say "re-verify every line again at the current HEAD", which cannot be carried
out: the file records where a sentence sat at `3f69b316`, more than a dozen commits have moved the
canonical documents since, and in several cases the sentence the line number pointed at has been
edited or removed by this phase's own corrections. The instruction never said what to do when a line
had moved, so a re-runner following it re-verified nothing. What replaces it:

- **Do not resolve `sweep-correction-list.md`'s line numbers against any HEAD.** Every one of its
  rows was folded into `contradiction-register.md` and re-verified there when the register was built;
  the register's rows are the current record of where each site is and whether it was executed. If a
  correction-list row and a register row disagree about a line, the register is right. The
  correction list is an input already consumed, not a live checklist.
- **One thing in it is still live, and it is not the line numbers.** Read its final section,
  "Reconciliation against the 2026-07-26 owner rulings," before using any row's direction column.
  Four rows' stated direction is superseded: S1 and R3 (the wider editable-write ingress check is not
  the target), and S11/S12/S13 (removing the trap clause is no longer the whole correction — the
  overflow deferral needs a second correction on the same sentences). Executing a superseded
  direction carries a model into canon that the owner has since replaced.

**5b. `canon-contradictions-found-2026-07-14.md`** — eleven items from the block inventory. Four of
its five self-contradictions were ruled on 2026-07-14 and corrected in `90f0792f`; those are **not**
re-litigated and are recorded in the register's excluded list so a reader who meets the same text
knows it was settled. The fifth was never ruled and is live at HEAD — it is a row. The remaining six
are readings the walk could not settle, and each is dispositioned in the register's excluded list with
the phase that owns it.

**5c. The phase-1 register's `walkUnresolved`** — the two *don't remember* marks from the 2026-07-26
walk. Each is disposed of in the register: either it becomes a row, or it is recorded as still open
with what is stalled.

## 6. Known blind spots

Nobody can prove this list complete and this procedure does not claim to. Six things it will miss (numbered from 0),
each stated so a re-runner knows what the coverage is not. **Two are now closed** — blind spots 2 and
4 — and are kept in the list so nobody re-opens them.

0. **Measured, not predicted: every grep in § 3 was written in the noun form of its idea.**
   `constraint evaluation`, `post-mutation`, `the sweep`. A document that says *evaluates
   constraints* was invisible to all sixteen. This is not blind spot 1 below — that is prose using
   none of the vocabulary; this is prose using exactly the vocabulary, inflected differently. It
   accounted for three of the eight disagreements the second independent re-check found, and for
   five register rows whose recorded pattern could never return zero. `A1` was widened on 2026-07-27
   to carry the verb and passive spellings alongside the noun.
   The register's § 1.0 records the triage of `A1`'s hits line by line, so a second run can reproduce
   a first run's discards; no other grep's triage is recorded, which is blind spot 5.

   **The widening instruction this entry used to carry has now been carried out. 2026-07-28, fifth
   pass.** From 2026-07-27 until then this entry said "every other grep in § 3 still carries only one
   inflection and should be widened the same way before the procedure is re-run", and nothing was
   widened — the fourth pass and the confirmation run both ran `A2`–`A16` as written, which is what a
   re-runner does. `A2`, `A4`–`A12` and `A14`–`A16` now carry their synonyms and inflections, and § 3
   records what each addition was for. Two carry a stated reason for not being widened: `A3`, whose
   pattern is already a stem, and `A13`, which was widened and returned nothing new.

   **And the result was not what this entry predicted.** Across all fifteen widened greps the
   synonym-and-inflection axis produced **no new disagreement** — every newly reached line was
   vocabulary reuse. The two disagreements the fifth pass found came from widening the **file set** to
   `docs/tooling/`, a folder nothing had ever searched. Blind spot 0 is real and it was worth closing,
   but on this file set it is now close to exhausted; the coverage that was actually missing was a
   folder, not a word.

   **Measured again 2026-07-27, fourth pass: three inflections were still one word short.**
   `proof-engine.md` specified the removed model twice as *constraint re-check*, which is neither the
   noun nor the verb nor the passive form of *evaluate* and matched none of the widened terms. The
   axis to widen along is **synonyms**, not only inflections — the other words a writer would reach
   for. `A1` now carries `re-check`, `re-evaluate`, `re-validate`, `re-verify`, `re-govern` and
   `re-prove` in both word orders. The same warning applies to `A2`–`A16`: a backstop is also a
   *trap*, a *guard* and a *safety net*, and none of those synonyms is in `A2`.

1. **A disagreement carried by prose that uses none of the vocabulary above.** Every grep is keyed to
   a word. A paragraph that describes the post-mutation sweep without using "sweep", "post-mutation",
   or "constraint evaluation" is invisible to this procedure. The only defence is that A1–A16
   over-return and their hits were read in context, which is a weaker defence than exhaustive reading.
2. **The three excluded top-level documents. CLOSED 2026-07-27, third re-check.** `docs/README.md`,
   `docs/agent-onboarding.md` and `docs/glossary.md` were searched for the whole removed-model
   vocabulary — the sweep, the backstops, `never rejects`, provenance, re-governance — and returned
   **zero hits in all three**. The judgement the procedure took, that a glossary entry is downstream
   of the specification and gets corrected when the specification does, holds. This is no longer a
   blind spot; it is kept in the list, closed, so a re-runner does not re-open it.

   One thing in those files *is* wrong and is not this procedure's to fix: `docs/agent-onboarding.md:29`
   and `CLAUDE.md` § Catalog System both cite `precept-language-spec.md:1992`, which phase 2's own
   edits blanked. The statement they mean is now at `:2010`. Register row `N95` records it.
3. **Code blocks inside the canonical documents.** The greps hit them and they were read, but a C#
   signature or a diagram that encodes the removed model — `ConstraintPlanIndex`, a pipeline arrow in
   an ASCII box — is a design statement that a prose-oriented read is likely to skim. Several were
   caught (`evaluator.md:505`, `precept-builder.md:283`); the honest position is that this class is
   under-searched.
4. **Claim W20 is under-tested. CLOSED 2026-07-27, third re-check.** "The premise list is the
   runtime's validation surface" no longer registers as an absence: `runtime-api.md:258` states it
   directly — "The premise list *is* the set of runtime validations the proofs depend on — which
   argument constraints and which guards must run — derived from the proof rather than maintained as
   a parallel list alongside it." Kept in the list, closed.
5. **The triage in § 3's four buckets is recorded for `A1` only.** Which hits a run discarded as
   vocabulary reuse, and why, is written down for `A1` in the register's § 1.0 and nowhere else. For
   `A2` through `A16` a second run cannot reproduce a first run's discards, so two runs can disagree
   without either being able to show where. Recording the triage per grep is what would close this.

## 7. Re-running it and diffing

**Bare filenames are ambiguous.** The register cites `README.md` for the repository-root README, and
`docs/README.md` also exists. The resolver below tries the repository root first, then the four
searched subfolders, then `docs/`. A resolver that tries `docs/` first reports a false out-of-range
on `README.md:110`. `docs/tooling/` was added to the list on 2026-07-28 with the folder itself;
without it, `mcp.md:108` and `language-server.md:661` report as missing files.

```bash
# 1. Confirm every row's site still resolves at the current HEAD.
python3 - <<'PY'
import re, pathlib
reg = pathlib.Path('docs/working-reset/phase-2/contradiction-register.md').read_text()
PREFIX = ['', 'docs/language/', 'docs/compiler/', 'docs/runtime/', 'docs/tooling/', 'docs/']
for m in re.finditer(r'`([a-zA-Z0-9._/-]+\.md):(\d+)`', reg):
    fn, n = m.group(1), int(m.group(2))
    hit = next((pathlib.Path(p + fn) for p in PREFIX if pathlib.Path(p + fn).exists()), None)
    if hit is None:
        print('MISSING FILE', m.group(0)); continue
    if n > len(hit.read_text().splitlines()):
        print('OUT OF RANGE', m.group(0), '->', hit)
PY

# 2. Re-run § 3 and § 4 and collect the hits.
# 3. For each hit, find its file:line in the register. A hit with no row is the diff output.
# 4. Per finish-line condition 3, once a row is executed its recorded pattern returns zero:
#    grep -rn "<pattern>" $CANON      # or $CANON_WIDE for the marked rows
```

Condition 3 is self-refereed — the person who writes the correction also writes the pattern that
proves it gone. It catches a forgotten file. It does not catch a correction done wrong, and it is not
represented as doing so.

**And it holds as of a commit, not permanently.** A recorded pattern is a literal string test against
documents that keep being edited, so a later correction that rewords a sentence an earlier pattern
quotes silently invalidates that pattern — the correction is fine, the pattern rots, and nothing
re-runs the register when canon changes. It has now happened twice, to `N55` (broken by `3dbad40b`)
and `N43` (broken by `fa4891d7`), both times to a row a later pass was not touching. Two consequences
for anyone running this procedure. First, re-run **every** row's pattern
after any correction pass, not only the patterns for the rows that pass touched. Second, when
recording a pattern, quote the least changeable words that carry the claim, not the whole sentence —
`N55`'s second rewrite drops the clause that had already been reworded twice and keeps only "proven
for data that arrived … assumed for data that did not". The plan's phase-2 "Done when" states the
same limit on condition 3 itself.
