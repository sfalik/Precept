# Phase 2 — the third pass, closing what the second independent re-check found

**Run 2026-07-27 at HEAD `46734adc`.** Input: [`recheck-second-pass.md`](recheck-second-pass.md),
which re-ran phase 2's finish-line conditions 3 and 4 and failed both. Everything in it is worked
here except the one item reserved for the owner.

**Nothing is committed.** Every edit below sits in the working tree.

**What was cited**: the canonical documents at HEAD, `docs/Working/what-i-want-2026-07-16.md`, the
phase-1 register and walk, and phase 2's own working documents. No catalog data as evidence, no
sample, no compile. `docs/language/catalog-system.md` is edited, and it is edited as what it is — a
canonical document *about* the catalogs, whose prose can be wrong. Its contents are never evidence
of anything.

## What was left for the owner

**One thing, deliberately untouched: the scope of ingress on an editable write.** The specification
marks it disputed at `precept-language-spec.md:1978`, the words "and nothing wider" stand at `:274`
and `:1976` exactly as they were, and [`editable-write-cascade.md`](editable-write-cascade.md)
records the reasoning. It is now register row `N96`, open and unexecuted, so the register carries it
rather than leaving it only in `recheck-second-pass.md` § 4.8. Nothing about the marking or the
wording was changed.

The substance, restated so the row is readable on its own: canon at HEAD says an editable write
upholds the constraints declared on the field the value enters and nothing wider. `want:143` says it
also evaluates every rule that mentions the field, and gives a reason nothing has replaced — an edit
is an arbitrary incoming value no static proof covers, so evaluating those rules at ingress is the
premise that closes the editable write sites in the compiler's proof. Without that premise, an
editable write has only field modifiers and pre-state rules available to it, which is not enough for
a relational rule; the consequence is that a field declared `editable` and mentioned in a relational
rule cannot be proven and the definition must be rejected. Canon nowhere says that, and the want
document's own worked example is that exact shape and is presented as accepted.

`docs/philosophy.md`, `docs/Working/bugs.md` and `docs/working-reset/phase-5/` were not opened.
`docs/philosophy.md` also carries uncommitted edits from work outside this pass; the register's
philosophy citations below are against HEAD, not against the tree, and the register says so.

---

## 1. Nine patterns that could not do their job

### 1.1 The new pattern kind, and the family test

Two structural additions to the register, because the same failure had now happened twice.

**`RE-SCOPE` joins `ADDITION` in § How to read a row.** A re-scope resolution keeps the canonical
words and changes what they are scoped to, so the phrase the row quotes was never going to
disappear and a "returns zero" test on that phrase can never pass. Five rows recorded as executed
carried a deletion-shaped pattern for a re-scope resolution, and each read to a mechanical checker
as a failure of condition 3 where the correction was in fact right.

**A family test, recorded once at § 1.0 with its triage written down.** Every pattern in the
post-mutation family had been written as one noun phrase lifted from the sentence being deleted.
All returned zero while three runtime documents went on specifying the same model in the verb form.
The family test carries the noun, verb and passive spellings together; it returns 19 lines at HEAD
plus this pass's corrections, and all 19 are enumerated in the register in five groups with the
reason each is legitimate. A twentieth line is a regression. That enumeration is the thing a second
runner could not reproduce before, and it is why § 2.3's three sites were cheap to miss twice.

### 1.2 The five re-scope rows

| Row | What the old pattern tested | What the new pattern tests | Result |
|---|---|---|---|
| `S7` | `ConstraintsFailed` — 15 hits, and always will be; `UpdateOutcome.ConstraintsFailed` survives on purpose | the identifier still carrying the post-mutation reading, with the denials and the ingress label filtered out | 0 |
| `N9` | `Collect-all violation set` — the OPA comparator cell, which describes what OPA does and is correct | inverted: `Collect-all applies at ingress only` must appear | 1 |
| `N40` | the containment-obligation phrase, which survives because the obligation is real | the same phrase with `rule preservation` filtered out | 0 |
| `N41` | Principle 7's fault list, which survives | the same list with `exempt` / `deferr` / `§0.7` filtered out — it also reaches Principles 10 and 11, which make the same claim | 0 |
| `N43` | "Overflow is a type error", which survives as the fixed-width end state | the same sentence with `end state` / `deferred` / `§0.7` filtered out — reaches `:236` too | 0 |

Two of these five also had a stale commit cell reading "NOT ACCOUNTED FOR … Flagged for the owner
rather than resolved here" — `N9` and `N41`. Both were executed by `4ca55318` and the register was
never touched by that commit. Corrected, with the reason recorded in the cell. The same stale-cell
defect was fixed on `N33`, `N40`, `N43` and `N75`, whose cells said their edits were still
uncommitted; `4ca55318` had committed all of them.

### 1.3 The three addition rows

- **`N52`** greps `proof by induction`, which nobody wrote. The induction itself is written at
  `precept-language-spec.md:268` — base case and inductive step, in the want document's own terms.
  New pattern greps the inflections that are actually there and returns 12, including
  `proof-engine.md:2696`.
- **`N55`** greps `assumed for restored`, same shape. The assumption is at
  `precept-language-spec.md:280`, in the words "*proven* for data that arrived through the contract
  and *assumed* for data that did not". New pattern returns exactly that line. The cell also still
  cited `:270`; the line has moved.
- **`N70`** recorded commit `c3a9be21` for an addition that commit deliberately did not make. What
  it did was record the gap as open, in canon's own voice, at `result-types.md:250` and
  `evaluator.md:912`. That is the right call and it is `N79`'s decision. The row now says so and is
  marked **not executed**, open behind `N79`.

---

## 2. The four narrow patterns, and the model they were hiding

`R7a`, `N7`, `N14` and `S6` each grep one full clause; all four return zero; and the model was still
specified in three runtime documents plus a language document. Every one of the four patterns uses a
noun phrase. Every surviving site used the verb.

### Sites corrected

| Site | Was | Now |
|---|---|---|
| `runtime/README.md:23` | "plan execution and **constraint evaluation**" | "plan execution and ingress validation" |
| `evaluator.md:115` | inner loop "**evaluate constraints from prebuilt buckets**" | inner loop checks each entering value against the constraints on the slot it enters; states that nothing evaluates the rules against the finished configuration and why |
| `precept-builder.md:143` | "The evaluator walks execution plans and **evaluates constraints**" | states what the evaluator checks, and that the definition's rules are not re-evaluated afterwards |
| `precept-builder.md:837`–`:845` | *Innovation* → *Dispatch-Optimized Constraint Indexes*: the removed model as a design achievement, five per-operation evaluation buckets, a worked cost estimate | *Constraint Indexes, and What They Index Now*: the runtime evaluates none of the definition's rules per operation; the five anchors are declaration forms; the two surviving uses are named; the residual shape question is left open and pointed at `N80` |
| `precept-builder.md:798` | Decision 3's rationale, "Eliminates O(constraints) scan per operation … O(bucket size) evaluation" | the buckets assemble the constraint catalogs and route an entering value to its slot's constraints; they are not walked against a finished working copy |
| `precept-builder.md:394` | computed fields recalculated "before **constraint evaluation**" | "before the working copy is committed" |
| `evaluator.md:1707`, `:2173` | "`Restore` bypasses both access-mode checks and **constraint evaluation**" | "…and ingress validation" |
| `evaluator.md:1800`, `:1808` | "Adding **constraint evaluation** would break this contract" / "No **constraint evaluation** pipeline runs" | "ingress validation" in both |
| `runtime-api.md:734` | "access-mode check + **constraint evaluation**" | "access-mode check + ingress validation" |
| `result-types.md:409` | "otherwise **constraints evaluated against the working copy** will receive wrong values" | "expressions evaluated against the working copy … report spurious results" — the substance, transitive unresolvability, is unchanged |
| `catalog-system.md:1259`, `:1328`, `:1329`, `:1433`, `:1451` | the same five activation buckets as a live consumer contract, with "Constraint activation timing" listed as *Available* | the evaluator's need named as ingress validation; the anchors kept as what `Constraints` declares today; one note stating the two surviving uses and the one open question, cross-linked from all five sites and from `precept-builder.md` |

Only the last six of those were in the re-check's list. The rest came out of running the widened
family test, which is the point of widening it.

**What the `precept-builder.md` and `catalog-system.md` notes do and do not decide.** They do not
decide what becomes of the index — that is `N80`, and it stays with the owner. They record two
things canon already says, at `evaluator.md` § 7.6 and `:1727`: the buckets assemble the
definition-level and state-level constraint catalogs, and they route an entering value to the
constraints declared on the slot it enters. The residual question is narrower than "what are the
buckets for" — it is whether five state-and-event anchors are the right shape for two uses anchored
to a slot and to a catalog listing. Both notes say that and nothing more.

### Patterns rewritten

`R7a`, `N7` and `N14` keep their original clause pattern, which correctly returns zero for the site
each row owns, and each now also cites the § 1.0 family test with a sentence saying what its own
pattern could never have found. `S6` the same. Procedure grep `A1` in
[`search-procedure.md`](search-procedure.md) is widened to carry the verb and passive spellings.

---

## 3. Eight disagreements that were not rows

Nine rows added: `N88`–`N96`.

| Row | Site | Disposition |
|---|---|---|
| `N88` | `runtime/README.md:23` | Executed. Carried unfixed from the first re-check. |
| `N89` | `evaluator.md:115` | Executed. |
| `N90` | `precept-builder.md:143` | Executed. |
| `N91` | `precept-builder.md:837`–`:845`, with `:798` and `:394` | Executed as wording; the index's shape stays with `N80`. |
| `N92` | `evaluator.md:1707`, `:1800`, `:1808`, `:2173`; `runtime-api.md:734`; `result-types.md:409` | Executed. Found only by the widened family test — none of the six asserts the removed pass; each names it as the thing being bypassed or refused. |
| `N93` | `catalog-system.md:1259`, `:1328`, `:1329`, `:1433`, `:1451` | Executed as wording; the shape question stays with `N80`. |
| `N94` | `graph-analyzer.md:538` against `diagnostic-system.md:192` | Executed. Two canonical documents described one diagnostic in opposite terms — a supported design against a provably dead declaration. `graph-analyzer.md` adopts the other document's characterisation and says why the code reports. The severity is not in dispute, so nothing about the ruled severity model moves. Carried unfixed from the first re-check. |
| `N95` | `catalog-system.md:103` | Executed — see § 4. |
| `N96` | the editable-write premise | **Open, and left for the owner.** |

`N10`'s site list is extended to name `precept-builder.md:798`, `:837`–`:845` and the five
`catalog-system.md` sites, with a note that `N80` is decided against the extended list. Before this,
the largest open row in the register had an incomplete site list in the one direction that matters,
since `catalog-system.md` is where a builder would look for the contract.

---

## 4. One stale cross-reference, and two copies of it outside reach

`catalog-system.md:103` cited `precept-language-spec.md:1992` for the statement, in the spec's own
voice, that five of the places data can change have no catalog entry. Phase 2's own edits to the
specification moved that content. **It is at `:2010`, not `:2209`** — `recheck-second-pass.md` § 4.6
gives `:2209`, and `:2209` at HEAD is a paragraph about unprovable obligations. The correct line
reads "the other five writers are not catalogued at all — the `omit` reset in particular has no
catalog member." Corrected to `:2010`.

**Two copies of the same broken cite were not fixed**, because both are outside the searched set and
outside phase 2's scope: `docs/agent-onboarding.md:29` and `CLAUDE.md` § Catalog System. Both need
the same one-token correction.

---

## 5. The philosophy citations

`docs/philosophy.md` was rewritten with the owner's approval at `20021deb` and refined at
`5e77865a`. All six register rows citing it carried line numbers from the version that existed when
the register was built, and none of them resolved any more — so the one document a reader would use
to check whether the philosophy work is done could not be used for that.

**The four `P` rows are satisfied by the rewrite and are now marked executed against those two
commits.** They were never executed *from the register* and that stands: the wording went to the
owner as a diff through `philosophy-changes-for-approval.md`, he approved it, and the two commits
applied it. The rows are marked executed because the change they asked for has landed, not because
the register authorised it.

| Row | What it quoted | State at HEAD |
|---|---|---|
| `P1` | the Fire sentence at `:19` — "evaluates all applicable constraints against the resulting configuration, and commits only if every constraint holds" | Gone. `:33` now reads "Nothing re-evaluates the rules after the change, and nothing needs to"; `:41` names the four premise classes in the want document's own terms. |
| `P2` | `:51` — "it evaluates atomically — the change and the check are the same act" | Gone. The surrounding guarantee survives. |
| `P3` | `:20` — "**Update**: edit a field directly. The same constraint enforcement applies." | Gone. `:17` describes an edit as one of the closed set of operations whose entering value is held to the constraints declared on the field it enters. |
| `P4` | `:33` — "the engine enforces it structurally on every operation where it applies" | Survives in reworded form at `:49`, in the governance-not-validation paragraph, where it describes what governance means rather than when a check runs. This is the disposition the row itself predicted. |
| `P5` | `:53` — the overflow sentence | Now `:37`. Still not a row: excluded by owner ruling, walk item 23. Citation updated only. |
| `P6` | `:61` — the second overflow site | Now `:41`. Same. |

**Line numbers above and in the register are against HEAD `46734adc`.** `docs/philosophy.md` carries
further uncommitted edits in the working tree from work outside this pass, which shift every number
after the first by two to four lines. The register's philosophy section says so at the top.

---

## 6. Bookkeeping corrected, and what is still wrong

**Corrected.** The header's "Never executed: all 123" and § 16's "Rows naming the commit that
executed them: 0" both now say they were true as written and are not true now, and point at a
recount. The row count is restated: 142 identifiers, 137 rows, five identifiers deliberately not
rows. Six stale commit cells fixed (`N9`, `N33`, `N40`, `N41`, `N43`, `N75`).

**Still wrong, and not fixed here.** Five rows still open their commit cell with "NOT ACCOUNTED FOR
… Flagged for the owner rather than resolved here": `N19`, `N54`, `N59`, `N67`, `N74`. The
re-check's § 6 says five of the original seven were executed by `4ca55318`; two of those seven were
`N9` and `N41` and both are fixed here, so at least three of the remaining five are closed items
that would go back to the owner if the register were read at face value. They were not re-checked
against `4ca55318` in this pass. `N74` is a different case — the re-check found `temporal-type-system.md`
untouched by any slice, so that one is probably genuinely open.

**Procedure defects recorded rather than fixed.** [`search-procedure.md`](search-procedure.md) now
carries three additions:

- § 1's excluded-paths table says the four per-area READMEs are inside `$CANON` and are searched.
  Both independent re-checks raised this as ambiguous and both were right that the table did not say
  so; `docs/runtime/README.md:23` carried the removed model through two re-checks with no row.
- § 6 gains blind spot 0 — **measured, not predicted** — that every grep in § 3 is written in one
  inflection. `A1` is widened; the other fifteen are not, and the note says so.
- § 6 gains blind spot 5 — the triage of § 3's hits is recorded for `A1` only, in the register's
  § 1.0. For `A2` through `A16` a second run still cannot reproduce a first run's discards.
- § 5a's stale `3f69b316` baseline is named as a trap, with the eight commits that have moved the
  canonical documents since.

---

## 7. Files changed

**Canonical, uncommitted:**

- `docs/runtime/README.md` — 1 line
- `docs/runtime/evaluator.md` — 5 sites
- `docs/runtime/precept-builder.md` — 4 sites, one of them a rewritten section
- `docs/runtime/runtime-api.md` — 1 line
- `docs/runtime/result-types.md` — 1 line
- `docs/compiler/graph-analyzer.md` — 1 line
- `docs/language/catalog-system.md` — 6 sites, one of them a new note

**Working documents, uncommitted:**

- `docs/working-reset/phase-2/contradiction-register.md` — new `RE-SCOPE` pattern kind, new § 1.0
  family test with its triage, nine patterns rewritten, six stale commit cells fixed, nine rows
  added, `N10`'s site list extended, the philosophy section rewritten, counts corrected
- `docs/working-reset/phase-2/search-procedure.md` — `A1` widened, two blind spots added, the
  per-area README ambiguity closed, the stale baseline named
- `docs/working-reset/phase-2/third-pass-corrections.md` — this file
