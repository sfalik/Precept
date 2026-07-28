# Phase 2 — second independent re-check of conditions 3 and 4

**Run 2026-07-27 at HEAD `20021deb`** by someone who wrote none of the procedure, the register, the
four correction slices, the gap-closing slice, or the philosophy rewrite. Nothing canonical was
edited. `docs/Working/bugs.md` and `docs/working-reset/phase-5/` were not opened.

**What was cited**: the canonical documents at HEAD, `docs/Working/what-i-want-2026-07-16.md`, the
phase-1 register and walk, and the phase-2 working documents. No catalog data, no sample, no compile.

The first re-check (`independent-recheck.md`, at `d32d8e26`) is read and its findings are re-tested
below, but this is not a re-test of that list. Every grep pattern in the register was re-run, the
procedure was re-run end to end, all four of its blind spots were worked, and the want document was
read against canon directly.

## Verdict

| Condition | Verdict |
|---|---|
| 3 — every executed row's recorded pattern returns zero | **Fails**, on eight rows. Five of them (`S7`, `N9`, `N40`, `N41`, `N43`) are recorded as executed and their correction is right, but the recorded pattern is a phrase the correction was never going to remove, so it cannot reach zero and proves nothing. Three (`N52`, `N55`, `N70`) are `ADDITION` rows whose inverted test returns zero; two of those had their text written in different words, and the third was never written at all while the row records a commit. |
| 4 — a second person re-running the procedure finds no disagreement that is not already a row | **Fails.** Eight disagreements found that no row covers. Three are in the runtime documents and describe the removed model in the present tense; one is a language document the largest open row's site list omits entirely; one is a substantive gap between canon and the want document created by phase 2's own revision. Two are carried unfixed from the first re-check. |

The corrections themselves are good. Everything the four slices and the gap-closing slice actually
changed reads correctly against the want document, and the specification's §0.7 is now the clearest
statement of the model anywhere in the tree. What fails is coverage of the runtime documents and the
truthfulness of the finish-line test.

---

## 1. The first re-check's findings, re-tested

| Its finding | State at HEAD |
|---|---|
| § 2.1 `N15`, `N19`, `N20` — `*ConstraintsFailed` alive in `runtime-api.md` | **Closed.** All three patterns return zero. |
| § 2.1 `S7` — pattern can never return zero | **Not closed.** Still 15 hits; see § 2 below. |
| § 2.2 `SC-1`/`N65` — the carried-constraint model alive at `compiler-and-runtime-design.md:149`, `:153`, `:169` | **Closed, and well.** `:149` now states discharge by proof over reachable configurations from the four premise classes, and names the pre-state class as the one that is easy to leave out. |
| § 2.2 `N52`, `N53`, `N55` — inverted patterns testing phrases nobody wrote | **Half closed.** `N53` now returns 2. `N52` and `N55` still return zero although their content is written (`precept-language-spec.md:268` and `:280`). |
| § 2.3 the twelve unexecuted rows | **Closed for eleven.** `N8`, `N9`, `N19`, `N31`, `N32`, `N33`, `N40`, `N41`, `N43`, `N59`, `N74`, `N75` all now read correctly at their sites. `N54`'s statement exists at `precept-language-spec.md:270`. |
| § 4.1 `precept-language-spec.md:2085`–`:2094` construction composition | **Closed.** Now two items: ingress evaluated, everything else proven as the base case. |
| § 4.2 `precept-language-spec.md:2184` PRE0115 exemption | **Closed.** Now states the per-field exception and calls the whole-precept early return an implementation gap. |
| § 4.4 `docs/runtime/README.md:23` | **Not closed.** Still reads "evaluator.md — plan execution and constraint evaluation". |
| § 4.5 `graph-analyzer.md:538` `UnhandledEvent` (Warning) | **Not closed**, though `diagnostic-system.md:192` now gives a reason for it (a deliberately-forbidden event is a supported design). `graph-analyzer.md` still calls the same thing a "provably dead declaration", so the two documents give opposite characterisations of one code. |
| § 4.6 `docs/glossary.md:78`, `:113` | **Not closed.** Both still present. They belong to the deferred fault family and the procedure excludes the file, so this is disclosed debt rather than a miss. |

## 2. Judging the patterns

### 2.1 Five patterns that cannot return zero, on rows recorded as executed

Condition 3 says the recorded pattern returns zero. For these five the resolution was a re-scope or
an addition, not a deletion, so the phrase the pattern greps was always going to survive. The row
therefore records a test it fails by construction, and a reader checking condition 3 mechanically
reads five failures where there are none.

| Row | Pattern | Hits | What is actually there |
|---|---|---|---|
| `S7` | `ConstraintsFailed` | 15 | The spec and `runtime-api.md` now say the outcome is structurally impossible for construction; `UpdateOutcome.ConstraintsFailed` survives on purpose, re-scoped to an ingress failure and labelled as one at `result-types.md:152`. Correct, and unprovable by this pattern. |
| `N9` | `Collect-all violation set` | 1 | `result-types.md:375` — the cell survives; the sentence beside it now says collect-all applies at ingress only. |
| `N40` | `the same containment obligation a `set` into a bounded field carries` | 1 | `collection-types.md:627` — the phrase survives; the sentence now calls it rule preservation, not a fault gate. |
| `N41` | `division by zero, overflow, empty collection access, and more` | 1 | `precept-language-spec.md:104` — Principle 7's list survives; the overflow deferral is now disclosed on the same line. |
| `N43` | `Overflow is a type error` | 1 | `primitive-types.md:197` — the sentence survives by design; the cross-reference to the deferral is now attached to it. |

The fix is one column, not one edit: a row whose resolution is a re-scope needs its test written as
*verify by reading*, the way `N3`, `N29`, `N44`, `N47`, `N49` and `N50` already do.

### 2.2 Three `ADDITION` rows whose inverted test fails

| Row | Test | Hits | State |
|---|---|---|---|
| `N52` | `proof by induction` must be > 0 | 0 | The induction **is** written — `precept-language-spec.md:268` states the base case and the inductive step, `proof-engine.md:2696` argues the section that used to deny it. The pattern greps a phrase nobody wrote. |
| `N55` | `assumed for restored` must be > 0 | 0 | The assumption **is** written, at `precept-language-spec.md:280`, in the words "*proven* for data that arrived through the contract and *assumed* for data that did not". Same shape as `N52`. |
| `N70` | `proven from these premises` must be > 0 | 0 | **Different, and this one matters.** The row records commit `c3a9be21`. What that commit actually did was record the gap as open — `result-types.md:250` and `evaluator.md:912` both say what inspection should report is proof provenance and that no canonical document says which type carries it, and leave the three arrays untouched. That is the right call and it is row `N79`'s call, but `N70` is marked executed against it. A row that requires canon to gain a statement is recorded as done by a commit that deliberately did not add the statement. |

### 2.3 The pattern class I judge dishonestly narrow

`R7a`, `N7`, `N14` and `S6` each grep one full clause for the same model — constraint plans evaluated
per operation against the working copy. All four return zero. The model is still specified, in
different words, in two runtime documents:

- `precept-builder.md:837`–`:845`, under the heading **"Dispatch-Optimized Constraint Indexes"** in a
  section called *Innovation*: "Traditional constraint systems evaluate all constraints on every
  operation. Precept pre-sorts constraints into activation-anchor buckets" — then five buckets, each
  described as "evaluated on every operation", "evaluated only when in that state", and so on, with a
  cost estimate ("for a precept with 50 constraints, a typical Fire operation might evaluate only
  5–10").
- `evaluator.md:115`: "The evaluator's inner loop is: look up a prebuilt plan, walk its opcode array,
  **evaluate constraints from prebuilt buckets**, produce a structured outcome."
- `precept-builder.md:143`, the pipeline-responsibility table: "**Operation execution** | The
  evaluator walks execution plans and **evaluates constraints**."

None of the three is cited by any row. All three are in files the runtime slice opened and edited.
The reason they survived is exactly the one the plan warns about: every one of the four patterns
greps a noun phrase (`constraint evaluation`, `constraint plans evaluated`), and all three surviving
sites use the verb (`evaluates constraints`, `evaluate constraints`). The procedure's `A1` grep has
the same blind spot for the same reason.

### 2.4 Patterns that are honest and did their job

For completeness: the sweep family in `precept-language-spec.md`, `runtime-api.md`, `result-types.md`
and `evaluator.md`'s Fire lifecycle, the restore family, the provenance split, the strategy-count
disagreement, the severity flips, the induction denial, the default-fold family and the certificate
additions all return what their rows say they should. `N36` (`defense-in-depth`, wide) returns 13 and
zero of them are in `compiler-and-runtime-design.md`; that edit remains the single strongest piece of
work in the phase.

## 3. Running the procedure at HEAD

It runs. § 0's variables, § 3's sixteen greps, § 4's three absence greps and § 7's resolver all
execute. The resolver reports **0 problems**.

Three things a re-runner cannot resolve from the text:

1. **§ 1's excluded-paths table is still silent on the four per-area READMEs.** `docs/runtime/README.md`
   sits inside `$CANON` and carries a disagreement (§ 4.1 below). The table excludes `docs/README.md`
   by name and says nothing about the four inside the canonical folders. Same ambiguity the first
   re-check raised; unchanged.
2. **§ 3's bucket rule still has no boundary between "contradicts" and "vocabulary reuse", and the
   triage is still unrecorded.** `A1` returns 36 hits at HEAD. Which ones a run discards is not
   written down anywhere, so a second run cannot reproduce a first run's discards. This is what made
   § 2.3's three sites cheap to miss twice.
3. **§ 5a still says "re-verify every line again at the current HEAD" against a `3f69b316` baseline.**
   Seven commits have moved the canonical documents since. The instruction points at a stale baseline;
   the numbers happen to hold because the register was rebuilt at `b93d1db1`.

One procedure defect I would add to § 6: **every grep in § 3 is written in the noun form of its
idea.** `constraint evaluation`, `post-mutation`, `the sweep`. A document that says *evaluates
constraints* is invisible to all sixteen. That is not the blind spot § 6 item 1 describes (prose using
none of the vocabulary) — it is prose using exactly the vocabulary, inflected differently, and it
accounts for three of the eight findings below.

## 4. Disagreements found that are not a row

Eight. Two are carried unfixed from the first re-check; six are new here.

### 4.1 `docs/runtime/README.md:23` — the reading-order map

> 3. [evaluator.md](evaluator.md) — plan execution and **constraint evaluation**

Inside `$CANON`, the first thing a reader of the runtime documents meets, unchanged since
`93934c8b`. Named by the first re-check; still open. **No row.**

### 4.2 `docs/runtime/evaluator.md:115` — the evaluator's own inner loop

> The evaluator's inner loop is: look up a prebuilt plan, walk its opcode array, **evaluate
> constraints from prebuilt buckets**, produce a structured outcome.

Four paragraphs into the document whose Fire lifecycle the runtime slice rewrote correctly at `:533`.
The summary of what the evaluator does was not reached. **New, no row.**

### 4.3 `docs/runtime/precept-builder.md:143` — the pipeline responsibility table

> | **Operation execution** | The evaluator walks execution plans and **evaluates constraints**. The
> builder produces plans, not outcomes. |

The row that tells a reader which component owns which job. **New, no row.**

### 4.4 `docs/runtime/precept-builder.md:837`–`:845` — the constraint-index section

The heading is *Innovation*; the subheading is *Dispatch-Optimized Constraint Indexes*; the content is
the removed model presented as a design achievement, with five per-operation evaluation buckets and a
worked cost estimate. Row `N10` cites `precept-builder.md:115` and `:653` and does not reach this
section, so when `N80` is decided nobody will find it from the register. This is the largest single
piece of removed-model text still standing outside the deliberately-deferred fault family. **New, no
row.**

### 4.5 `docs/language/catalog-system.md:1259`, `:1328`, `:1329`, `:1433`, `:1451` — the same buckets, in a language document

`Constraints.GetMeta(kind)` routing each `ConstraintDescriptor` into an activation bucket
(`always`, `StateResident`, `StateEntry`, `StateExit`, `EventPrecondition`), listed as a live
consumer contract for both the builder and the evaluator, with "**Constraint activation timing**" named
as what the evaluator derives from the catalog. `N10`'s site list names `evaluator.md`,
`precept-builder.md` and `proof-engine.md` — the last of which does not resolve — and omits this file.
The largest open row in the register therefore has an incomplete site list in the one direction that
matters, since `catalog-system.md` is where a builder would look for the contract. **New, no row.**

### 4.6 `docs/language/catalog-system.md:103` — a cite phase 2 broke

The paragraph reads "`precept-language-spec.md:1992` says so in the spec's own voice about the write
list". At HEAD `precept-language-spec.md:1992` is a table row about computed-field recomputation. The
statement it means is now at `:2209`. The spec's phase-2 edits moved it and nothing checked the
downstream cite. (`docs/agent-onboarding.md:29` carries the same broken cite, outside the searched
set, and so does `CLAUDE.md`.) **New, no row.**

### 4.7 `docs/compiler/graph-analyzer.md:538` — `UnhandledEvent`

> **Zero coverage** (no state handles the event anywhere) — provably dead declaration. Emit
> `UnhandledEvent` (Warning).

`diagnostic-system.md:192` now justifies keeping it a report — "a deliberately-forbidden event … [is a]
supported design". That is a reasonable answer and I am not asserting it is wrong. What is wrong is
that the two documents describe the same code in incompatible terms: one calls it a supported design,
the other calls it provably dead. Named by the first re-check; still open. **No row.**

### 4.8 The editable-write premise, and the want document's own example

This is the substantive one, and phase 2 created it.

Canon at HEAD is now explicit and consistent that ingress on an editable-field write upholds "the
constraints declared on the field it enters — nothing wider" (`precept-language-spec.md:274`, `:1976`;
`runtime-api.md:402`; `evaluator.md:738`, `:1383`; `result-types.md:152`;
`compiler-and-runtime-design.md:137`, `:151`). That is the revised `S1`/`R3` direction, and the
register's reasoning for it — that re-evaluating the rules reaching a field through a computed field
is the removed pass under another name — holds.

But `want:143` is the sentence that revision overrides, and it is not decoration. It says an editable
write evaluates "the field's modifier-rules *and* every rule that mentions the field", and it gives
the reason: "an edit is an arbitrary incoming value that no static proof can cover, so evaluating
those rules at the ingress point is exactly **the premise that closes the editable write sites in the
compiler's proof**."

Take that premise away and the write site has nothing left. An editable write has no guard and no
event argument, so of the four premise classes only (a) field modifiers and (d) pre-state rules are
available to it. For a relational rule that is not enough. The consequence is that a field declared
`editable` and mentioned in a relational rule cannot be proven and the definition must be rejected.

Canon nowhere says this. And the want document's own worked example is exactly that shape:

```
field RepaymentCapPercent as decimal default 0.25 positive max 1.0 editable      (want:36)
rule MonthlyRepayment <= OverdraftLimit * RepaymentCapPercent when MonthlyRepayment is set   (want:40)
```

Editing `RepaymentCapPercent` downward breaks that rule while `MonthlyRepayment` is untouched. Under
`want:143` the edit is refused at ingress. Under canon at HEAD nothing refuses it, and
`precept-language-spec.md:270` says in the same breath that the obligation "holds at every write site
of every field the rule mentions" — which includes this one. So either the example does not compile,
or a write site carries an obligation with no premise that can discharge it. The want document
presents the file as accepted (`want:98`, `want:129` names one deliberate omission and it is a
different field).

The register records the revision in `S1`'s resolution cell and never checks it against the want
document. Whether the right answer is "the example does not compile and the want document's own
sample would be rejected", or "the editable ingress premise comes back in a narrower form", or
something else, is an owner question. **New, no row.**

## 5. The philosophy, checked against the want document

`docs/philosophy.md` was rewritten at `20021deb` with the owner's approval and has not been
independently read against the want document since. Read end to end:

**The four recorded philosophy rows are all satisfied by the rewrite.** `P1`'s Fire sentence, `P2`'s
"the change and the check are the same act", and `P3`'s Update sentence are gone. What replaced them
is aligned: `:29` "Nothing re-evaluates the rules after the change, and nothing needs to"; `:33` the
four premise classes named in the want document's own terms — a constraint on the field, a constraint
on the event argument, the guard, or a rule that already held; `:35` the runtime verifying a
definition's proof before it will govern under it. `P4`'s "enforces … on every operation" survives at
`:45`, which is the disposition `P4` itself predicted ("the one most likely to be judged fine as
written").

**All six philosophy rows now carry dead line numbers.** `P1` cites `:19`, `P2` `:51`, `P3` `:20`,
`P4` `:33`, `P5` `:53`, `P6` `:61`. The file is 100 lines and none of those lines holds the quoted
text any more. The register is the document a reader would use to check whether the philosophy work
is done, and it cannot be used for that.

### The two items the owner is holding

Neither is reported here as a defect. What is reported is what the rest of the tree now does around
them.

**Overflow.** `:33` ("Division by zero, arithmetic overflow, empty collection access: not risks
managed at runtime, but compile-time impossibilities") and `:37` ("no division by zero, **no
overflow**, no empty-collection access") are two unqualified claims — the same count the file carried
before. What changed is everything around it. `precept-language-spec.md` now discloses the deferral in
Principle 7, Principle 10, Principle 11 and a dedicated §0.7 paragraph; `primitive-types.md:81` and
`:197` disclose it; `soundness-and-coverage.md:282` discloses it. **`docs/philosophy.md` is now the
only canonical document that states overflow prevention with no disclosure anywhere near it.** Phase 2
did not make the philosophy worse; it made the philosophy the exception.

**The boundary.** The rewrite added a sentence that was not in the previous version and that bears
directly on this. `:15`: "an entity comes back from storage by **restore, which changes nothing: it
returns a configuration that was already committed**." Two things about it.

First, `precept-language-spec.md:278`–`:280` now says something weaker and says it deliberately:
restored state is *trusted* as valid at the time it was persisted, host-injected values are likewise
outside the envelope, and "every proof that leans on 'this rule already held before the operation' is
*proven* for data that arrived through the contract and *assumed* for data that did not." The
philosophy's sentence asserts as fact the thing the spec now names as an assumption. It does not carry
the boundary; it reads as though there were nothing to carry.

Second, the same paragraph says "The set is closed because the guarantee requires it: an operation
exists in the compiled engine only because the compiler proved every rule survives it" — with restore
inside that closed set. Nothing is proven about restored data; it is trusted. Read strictly, the
closure claim over-reaches for the one operation the spec exempts, and it does so two sentences after
naming it.

Neither observation asks for an edit. Both are the shape of the thing the owner said he would decide,
and the rewrite moved the file slightly further from the specification while he decides.

## 6. Bookkeeping the register can no longer answer

- **The register's own summary contradicts its body.** The header table says "Never executed: all
  123" and § 16 says "Rows naming the commit that executed them: 0". The Commit column now carries
  hashes on roughly ninety rows. Both statements were true when written and neither has been updated.
- **Seven rows still say "NOT ACCOUNTED FOR … Flagged for the owner rather than resolved here"** —
  `N9`, `N19`, `N41`, `N54`, `N59`, `N67`, `N74`. Five of the seven were executed by `4ca55318`; the
  register was not touched by that commit (`git log -- contradiction-register.md` ends at
  `6c3fc90d`). A reader taking the register at face value would take five closed items back to the
  owner.
- **Six philosophy rows carry line numbers that no longer resolve** (§ 5 above).
