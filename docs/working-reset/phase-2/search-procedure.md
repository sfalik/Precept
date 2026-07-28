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
CANON_WIDE="docs/language docs/compiler docs/runtime docs/philosophy.md README.md docs/compiler-and-runtime-design.md"

WANT="docs/Working/what-i-want-2026-07-16.md"

git rev-parse HEAD          # record this in the register's header
```

## 1. Which files count as canonical, and one deliberate widening

`CLAUDE.md` names the canonical documents as `docs/language/`, `docs/compiler/`, `docs/runtime/`,
`docs/philosophy.md` and `README.md`. The plan's finish-line grep (condition 3) covers only the
first four — it leaves out `README.md`.

This procedure searches two files beyond the plan's grep set, and both are deliberate:

- **`README.md`** — the documentation-sync table in `CLAUDE.md` makes it a document that must never
  carry a claim as implemented when it is not. The 2026-07-19 sweep checked it and found no
  corrections; this procedure re-checks it rather than inheriting that result.
- **`docs/compiler-and-runtime-design.md`** — `CLAUDE.md` lists it as the architecture-overview entry
  point, `precept-language-spec.md:274` delegates the description of how discharge and governance
  compose to it, and it was **never read** by the 2026-07-19 sweep (recorded as a coverage gap in
  `sweep-correction-list.md` § 3). It is also the file the plan's own record of the 2026-07-26 walk
  says phase 2 must edit (`compiler-and-runtime-design.md:125`). Searching it and then having no way
  to check the row was executed would be worse than not searching it, so:

**Consequence for the finish-line check.** Rows whose site is in `README.md` or
`docs/compiler-and-runtime-design.md` must have their recorded grep pattern run against `$CANON_WIDE`,
not `$CANON`. The register marks those rows. This is a widening of the plan's condition 3, not an
exception to it.

Excluded from the search, and each for a stated reason:

| Path | Why excluded |
|---|---|
| `docs/archive/` | Superseded specs; `CLAUDE.md` says reference only, never update. |
| `docs/Working/` | In-flight proposals, not canon. The want document lives here and is the other side of every comparison, not a target of correction. |
| `docs/README.md`, `docs/agent-onboarding.md`, `docs/glossary.md` | Navigation and vocabulary. They carry no normative claim about the proof-versus-governance boundary. **Known blind spot** — see § 6. |
| `src/`, `tools/`, `test/`, `samples/`, the catalogs | Excluded by the plan's "what it may cite" rule. |
| `.squad/`, `CLAUDE.md`, `CONTRIBUTING.md` | Operational and instruction files, corrected by phase 0. |

**The four per-area READMEs are inside `$CANON` and are searched.** `docs/language/README.md`,
`docs/compiler/README.md`, `docs/runtime/README.md` and `docs/tooling/README.md` sit inside the
canonical folders, so `$CANON` reaches them and the row above excludes only `docs/README.md` at the
top level. Both independent re-checks raised this as ambiguous, and both were right that the table
did not say so. It says so now, because `docs/runtime/README.md:23` carried the removed model
through two re-checks with no row against it (register `N88`).

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
grep -rnEi 'post-mutation|the sweep|sweep governs|swept|constraint (evaluation|plans? evaluated)|evaluat(e|es|ed|ing) (all |every |the |each )?constraints?|constraints? (are |is )?evaluat(e|es|ed|ing)' $CANON_WIDE --include=*.md

# A2 — W12, W13: runtime fault traps and defence-in-depth
grep -rnEi 'backstop|defense-in-depth|defence-in-depth|defensive redundancy|belt-and-suspenders|fault trap|FaultSite' $CANON_WIDE --include=*.md

# A3 — W19, W20: the certificate and its load-time gate (searched for ABSENCE, see § 4)
grep -rnEi 'certificat' $CANON_WIDE --include=*.md

# A4 — W14: deferral
grep -rnEi 'defer(red|s|ral)? to (a |the )?runtime|handed to the runtime|runtime check|fallback' $CANON_WIDE --include=*.md

# A5 — W8, W9: ingress, and whether canon names more than two ingress points
grep -rnEi 'ingress' $CANON_WIDE --include=*.md

# A6 — W9: restore re-validating
grep -rnEi 'restor(e|ed|ation).{0,80}(valid|constraint|govern|trust)' $CANON_WIDE --include=*.md

# A7 — W10, W17: structural defects carried at Warning severity
grep -rnEi 'severity.{0,30}warning|Warning, not error|stays? .?Warning|advisory|report \(|hygiene' $CANON_WIDE --include=*.md

# A8 — W7: solvers, and non-linear reasoning
grep -rnEi 'SMT|solver|non-linear|nonlinear' $CANON_WIDE --include=*.md

# A9 — W2: induction
grep -rnEi 'inductive|induction|base case|preserv(e|es|ation)|establish(es|ment)?' $CANON_WIDE --include=*.md

# A10 — W4: write sites and symmetric obligations
grep -rnEi 'write site|every write|symmetric' $CANON_WIDE --include=*.md

# A11 — W5: modifiers as rule shorthand
grep -rnEi 'sugar for|rule shorthand|shorthand for a rule|one constraint mechanism' $CANON_WIDE --include=*.md

# A12 — W15: the guarantee split by provenance rather than uniform
grep -rnEi 'provenance|where the value comes from|externally-supplied|governed at runtime' $CANON_WIDE --include=*.md

# A13 — W12: overflow named among the faults a clean compile rules out
grep -rnEi 'overflow' $CANON_WIDE --include=*.md

# A14 — W18: cross-precept scope
grep -rnEi 'cross-precept|saga|message passing|another precept|multi-precept' $CANON_WIDE --include=*.md

# A15 — W19: the outcome taxonomy that presupposes the sweep
grep -rnE 'ConstraintsFailed|EventConstraintsFailed|UpdateConstraintsFailed|RestoreConstraintsFailed' $CANON_WIDE --include=*.md

# A16 — W1, W16: prove-or-reject stated as never rejecting
grep -rnEi 'never rejects|does not reject|unfoldable default|unknown/unfoldable' $CANON_WIDE --include=*.md
```

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

## 5. Fold in the two committed inputs

Neither of these is produced by the greps above, and both must be merged before the register is
complete.

**5a. `sweep-correction-list.md`** — 25 verified rows plus one clarity-axis row, each with a per-file
line number re-verified at HEAD `3f69b316`. Re-verify every line again at the current HEAD, because
the canonical documents moved between `3f69b316` and now. **That baseline is stale and re-reading it
as current is a trap**: eight commits have moved the canonical documents since `3f69b316`
(`5e853c0e`, `c3a9be21`, `25894ae3`, `d32d8e26`, `4ca55318`, `20021deb`, `5e77865a`, `46734adc`,
plus the third-pass corrections of 2026-07-27, uncommitted at the time of writing). The register's
own numbers happen to hold because it was rebuilt at `b93d1db1`; `sweep-correction-list.md`'s do
not. Resolve every line against `git rev-parse HEAD` at the moment you run, and record which HEAD
that was. Then, **before using any row's direction
column, read that file's final section, "Reconciliation against the 2026-07-26 owner rulings."** Four
rows' stated direction is superseded: S1 and R3 (the wider editable-write ingress check is not the
target), and S11/S12/S13 (removing the trap clause is no longer the whole correction — the overflow
deferral needs a second correction on the same sentences). Executing a superseded direction carries a
model into canon that the owner has since replaced.

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

Nobody can prove this list complete and this procedure does not claim to. Five things it will miss,
each stated so a re-runner knows what the coverage is not:

0. **Measured, not predicted: every grep in § 3 was written in the noun form of its idea.**
   `constraint evaluation`, `post-mutation`, `the sweep`. A document that says *evaluates
   constraints* was invisible to all sixteen. This is not blind spot 1 below — that is prose using
   none of the vocabulary; this is prose using exactly the vocabulary, inflected differently. It
   accounted for three of the eight disagreements the second independent re-check found, and for
   five register rows whose recorded pattern could never return zero. `A1` was widened on 2026-07-27
   to carry the verb and passive spellings alongside the noun. **Every other grep in § 3 still
   carries only one inflection** and should be widened the same way before the procedure is re-run.
   The register's § 1.0 records the triage of `A1`'s hits line by line, so a second run can reproduce
   a first run's discards; no other grep's triage is recorded, which is blind spot 5.

1. **A disagreement carried by prose that uses none of the vocabulary above.** Every grep is keyed to
   a word. A paragraph that describes the post-mutation sweep without using "sweep", "post-mutation",
   or "constraint evaluation" is invisible to this procedure. The only defence is that A1–A16
   over-return and their hits were read in context, which is a weaker defence than exhaustive reading.
2. **The three excluded top-level documents.** `docs/README.md`, `docs/agent-onboarding.md` and
   `docs/glossary.md` were not searched. The glossary in particular could carry a definition that
   restates the sweep; the judgement taken is that a glossary entry is downstream of the spec and gets
   corrected when the spec does. If that turns out to be wrong, the fix is to add them to `CANON_WIDE`
   and re-run § 3, which costs about an hour.
3. **Code blocks inside the canonical documents.** The greps hit them and they were read, but a C#
   signature or a diagram that encodes the removed model — `ConstraintPlanIndex`, a pipeline arrow in
   an ASCII box — is a design statement that a prose-oriented read is likely to skim. Several were
   caught (`evaluator.md:505`, `precept-builder.md:283`); the honest position is that this class is
   under-searched.
4. **Claim W20 is under-tested.** "The premise list is the runtime's validation surface" has no
   canonical counterpart at all, so there is nothing to contradict — it registers as an absence, and
   an absence search cannot tell the difference between "canon is silent" and "canon says it
   somewhere in words this procedure did not guess."
5. **The triage in § 3's four buckets is recorded for `A1` only.** Which hits a run discarded as
   vocabulary reuse, and why, is written down for `A1` in the register's § 1.0 and nowhere else. For
   `A2` through `A16` a second run cannot reproduce a first run's discards, so two runs can disagree
   without either being able to show where. Recording the triage per grep is what would close this.

## 7. Re-running it and diffing

**Bare filenames are ambiguous.** The register cites `README.md` for the repository-root README, and
`docs/README.md` also exists. The resolver below tries the repository root first, then the three
canonical subfolders, then `docs/`. A resolver that tries `docs/` first reports a false out-of-range
on `README.md:110`.

```bash
# 1. Confirm every row's site still resolves at the current HEAD.
python3 - <<'PY'
import re, pathlib
reg = pathlib.Path('docs/working-reset/phase-2/contradiction-register.md').read_text()
PREFIX = ['', 'docs/language/', 'docs/compiler/', 'docs/runtime/', 'docs/']
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
