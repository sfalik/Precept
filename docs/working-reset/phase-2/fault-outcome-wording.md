# Phase 2 — canon stated the fault outcome shape flatly; it now states it conditionally

**Run** 2026-08-11 on top of HEAD `bc35e479`, branch `spike/Precept-V2-Radical-reset`, which was clean
when this pass started. **Nothing is committed; the tree is left dirty.** No sub-agents were spawned. No
`.precept` file was compiled and no compiler behaviour is cited as evidence. `docs/philosophy.md` was not
edited, and nothing under `docs/archive/`, `docs/Working/`, `src/` or `tools/` was edited.

**What this pass is.** On 2026-08-11 the owner ruled that whether anything ever constructs a `Fault` is
deferred until the runtime is being built. His words: *"do we need to decide this now?  seems like
something we can defer until we start building the runtime"*. That ruling is recorded in
[`compiler-readiness-plan.md`](../compiler-readiness-plan.md) — § 4, exit condition 6, and the named
exceptions under § No phase closes with an open deferral that has no owner. **The lean is exception and
remains a lean; nothing may be built or written as though exception were the answer.**

Six canonical places already record the question as undecided. Elsewhere, canon stated the outcome shape
flatly — "Faults are **never thrown** — they are returned as structured outcome variants" — and a flat
statement reads as though the deferred question had been answered in favour of the structured outcome.
This pass brings those statements into the conditional form. **Nothing was deleted.** The rule applied
throughout: canon may describe what would happen if a `Fault` were ever constructed, and may not assert it
as current or settled behaviour.

**What this pass is not.** It does not answer the question, narrow it, or move it. It does not reopen the
fault-backstop removal. It changes wording, not any design commitment: the shape a `Fault` takes when it
reaches a caller stays settled, and what stays parked is whether one ever does.

---

## 1. What the fault-backstop pass said about the flat wording

The question asked before editing was whether the 2026-07-28 pass had left `evaluator.md`'s flat
statements deliberately conditional-in-substance, or had simply not reached them. **It reached them,
decided to keep them, and decided the question on whether the sentence is true rather than on whether it
presumes an answer.** The conditional form was invented later the same day, at two sites, and never swept.

*(Line numbers are omitted for the two pass records below, because § 4 of this document edits both and
shifts them. Each quotation is exact and greppable.)*

[`fault-backstop-model.md`](fault-backstop-model.md) § Removals, which is where the pass's decisions were
written before it ran:

> **Keep `:1431`** ("Faults are **never thrown** — they are returned as structured outcome variants"). It
> is true under the corrected model and it is the only statement of it in this document. Moving it into
> § Fault Production keeps it anchored; leaving it where it is orphans it under a removed paragraph.

Restated later in the same document, in the review-response section: "**`:1431` is kept** — 'Faults are
never thrown' is true under the corrected model." And
[`fault-backstop-pass.md`](fault-backstop-pass.md)'s `N25` row records the execution:

> `:1431`'s "Faults are **never thrown**" is kept and moved into § Fault Production, where it is anchored
> rather than orphaned.

The test applied in all three places is *is the sentence true*, not *does it presume the answer to the
question this same pass was filing as open*. The one flat-versus-conditional judgement the pass did make
went the other way: the same `N25` row's *Also corrected* cell records replacing a hedge with a flat
statement —

> `:1708` read … "If the compiler emits no errors, the evaluator should never fault." … the second hedged
> with "should never" a guarantee stated absolutely everywhere else. Replaced with a flat statement plus
> the surviving build-time pairing.

That one is correct and is left alone. It is about the compile-time prevention guarantee, which is
settled, not about whether a `Fault` is ever constructed.

**Keep the two things apart.** The 2026-07-28 work hedged *whether anything constructs a `Fault`* in a
good many places — `fault-system.md:219`, `runtime-api.md:171`, `result-types.md:129`,
`precept-language-spec.md:1963`, `glossary.md:113`, `compiler-and-runtime-design.md:826` and
`diagnostic-system.md:102` all say nothing constructs one, or that whether anything ever will is
undecided. What it did not hedge is the separate family of statements about *what happens to a `Fault`*
— returned, not thrown — and those are the ones a reader takes as an answer to the question.

**The conditional form entered canon later the same day, in two sites, as a coherence fix.**
[`fault-backstop-fixes.md`](fault-backstop-fixes.md) § 3, item 10, covering `fault-system.md` Q1 and Q2:

> Q2 is re-authored: it asks what context a `Fault` should carry if anything ever constructs one … Q1's
> resolution now opens "should anything ever construct a `Fault` (Q3)".

Nothing propagated it beyond those two. `fault-backstop-fixes.md` § 4 re-ran the vocabulary sweep with paraphrases, but
every pattern in it hunts the *removed* fault-backstop model — `fault-site`, `runtime mirror`,
`defense-in-depth`, `backstop`, `impossible path`. None of them would match a sentence saying a fault is
returned rather than thrown, so the sweep could not have found this family and did not.

**So the repair belongs in `evaluator.md` and in the other documents that copied the flat form**, not in
the pass records — and the pass records are corrected in § 4 below rather than left saying the sentence
was kept on a test that did not consider the open question.

---

## 2. The sweep

Run over `docs/language docs/compiler docs/runtime docs/tooling docs/philosophy.md
docs/compiler-and-runtime-design.md docs/glossary.md docs/README.md README.md CONTRIBUTING.md`,
case-insensitive, `--include=*.md`, with `docs/archive/`, `docs/Working/` and `docs/working-reset/`
excluded from the results. The exact terms are listed first, then the wordings of the same idea that an
exact-term sweep would miss — which is the failure this topic has already had once.

**The counts below were measured after the edits in § 3, not before**, so they are what a re-runner will
see rather than what the sweep first found. Each is `grep -rn -i` over the set above, counting matching
lines.

| Pattern | Lines | What they are, now |
|---|---|---|
| `never thrown` | 6 | All six conditional: `runtime-api.md:904`, `result-types.md:51`, `fault-system.md:289` (Q1), `:320` (Q3's parked paragraph, quoting the commitment), `:326`, `evaluator.md:1621`. Five of the six were flat before this pass |
| `not thrown`, `rather than thrown` | 0 | — |
| `FaultException` | 3 | Q1's closing sentence, the options table row, and the Deliberate Exclusions bullet (repaired) |
| `Faulted` | 27 | Type declarations, variant tables, cross-reference rows, MCP serialization, the undecided notes, and the repaired sites |
| `breach of one of the engine` | 14 | The descriptions the 2026-07-28 pass re-scoped; four carried the flat outcome claim and are repaired |
| `invariant breach` | 8 | Category and commit-outcome tables, the recovery row, the runtime README's open-questions row |
| `engine-invariant` | 7 | Same family |
| `unrecoverable` | 8 | Six are the undecided notes describing the exception account; one is `evaluator.md:1638`, the account itself, which now carries a pointer; one is the added sentence at `result-types.md:72` |
| `corrupted model` | 3 | `evaluator.md:1638` and the two notes that cite it |
| `outcome variant` | 8 | In-domain outcomes, which are settled, plus the repaired `evaluator.md:1621` and `result-types.md:129` |
| `reported as` | 9 | Mostly unrelated (diagnostics reported as errors); the fault-side ones are the repaired sites |
| `surfaces as` | 6 | Unrelated uses plus the undecided notes; no flat fault claim left |
| `reaches the caller` | 5 | All in the undecided notes and Q3 |
| `defect in Precept` | 6 | The undecided notes |
| `engine defect` | 6 | The undecided notes and the added sentences |
| `outcome hierarchy` | 3 | Q1, Q3's parked paragraph, `result-types.md:51` — all conditional |
| `structured value inside` | 3 | The same three |
| `raw exception` | 1 | `result-types.md:129`, repaired |
| `hidden control flow` | 2 | Q1's options table, and `result-types.md:72`'s reason for one variant — both about the shape, not the producer |
| `escape path` | 1 | Q1's options table, describing the rejected exception option |
| `no exceptions` | 5 | Two unrelated (a construction guarantee, a numeric-lane rule); `graph-analyzer.md:652` about the analyzer; `evaluator.md:1959` about in-domain outcomes; `compiler-and-runtime-design.md:909`, repaired |
| `structured outcomes only` | 1 | `runtime-api.md:904`, repaired |
| `first-class type` | 5 | Four are temporal and money types; `evaluator.md:1959` closes the in-domain outcome sample |

Nothing outside `docs/runtime/` and `docs/compiler-and-runtime-design.md` carried the flat form.
`docs/glossary.md:113`, `docs/compiler/diagnostic-system.md:102`, `docs/runtime/README.md:36` and
`docs/language/precept-language-spec.md:1963` each already say nothing constructs one, or that whether
anything ever will is undecided.

---

## 3. Site by site — what was changed

Fourteen sites, in six documents. Two of them (3 and 4) are additions beside an unchanged paragraph
rather than re-wordings, and § 6 says why. Each entry gives what the site said and what it says now.

### `docs/runtime/evaluator.md`

**1. § 9 Fault Production, `:1621`** — the sentence the pass moved here.

- Was: "Faults are **never thrown** — they are returned as structured outcome variants. This maintains the
  pattern-matchable, composable outcome model."
- Now: "Should anything ever construct a `Fault` (see the open question below), it is **never thrown** — it
  is returned as a structured outcome variant. This maintains the pattern-matchable, composable outcome
  model. The shape is settled; whether anything produces a value of it is not."
- The open note it now points at is three lines below it and was already there.

**2. § 9 Recovery Paths, the engine-invariant breach row.**

- Was: "Log the fault with `FaultCode` and `CodeName`; investigate the compiler, builder or evaluator
  defect; do not retry".
- Now: "Investigate the compiler, builder or evaluator defect; do not retry. Should the breach ever arrive
  as a `Fault` rather than as a thrown exception — undecided, see § Fault Production — log it with its
  `FaultCode` and `CodeName`".
- The advice a caller can act on today is the half that does not depend on the answer, so it leads. The
  logging half is the half that does, so it is conditional. This row was renamed from "Impossible-path
  fault" by the 2026-07-28 pass, which kept the instruction unchanged.

**3. § 9 No Unclassified Exceptions** — a sentence added after the existing paragraph, which is unchanged.

- Added: "The corrupted-model half of that is one of the two accounts canon carries of what happens when
  Precept itself is broken, and it is written here as this document has always written it. Whether it
  stands, or whether the evaluator instead detects the breach and reports a `Fault`, is undecided — see
  the open note in § Fault Production above, and `fault-system.md` § Q3."
- **This is the same rule applied to the other side, and it goes beyond repairing the flat *Fault* form.**
  "These are unrecoverable and should not be caught" asserts the exception answer as settled, which the
  standing position forbids as squarely as asserting the other one. The account itself is not touched: the
  open note in § Fault Production cites this section by name as one of the two disagreeing accounts, and a
  reader following that citation has to find the account the note describes.

### `docs/runtime/precept-builder.md`

**4. § 9 Builder Invariant Violations** — a sentence added after the existing paragraph, which is
unchanged.

- Added: "This is one of the two accounts canon carries of what happens when Precept itself is broken;
  `compiler-and-runtime-design.md` names the evaluator as where such a breach would surface as a `Fault`
  instead. Which of the two holds is undecided — see `fault-system.md` § Q3, indexed in § 13 Open
  Questions."
- Same reasoning as site 3. The document's § 13 index already said this document is party to the question;
  the section itself did not, and a reader arriving at the section directly read a settled account.

### `docs/runtime/result-types.md`

**5. § Overview, `:51`.**

- Was: "A `Fault` … is returned as the `EventOutcome.Faulted(Fault)` variant, a structured value inside the
  outcome hierarchy, never thrown. See `fault-system.md`."
- Now: "A `Fault` … has its shape settled: should anything ever construct one, it is returned as the
  `EventOutcome.Faulted(Fault)` variant, a structured value inside the outcome hierarchy, never thrown.
  Nothing constructs one today and whether anything ever will is undecided (`fault-system.md` Q3). See
  `fault-system.md`."

**6. § Right-Sizing, `:72`.**

- Was: "…surfaces as the single `EventOutcome.Faulted(Fault)` variant rather than as a thrown exception."
- Now: "…has one place to go in this hierarchy should it ever arrive as a structured value: the single
  `EventOutcome.Faulted(Fault)` variant, rather than a thrown exception." The paragraph's reason for
  confining it to one variant is kept word for word, and a closing sentence records that whether an engine
  defect reaches the caller that way at all is undecided.

**7. § `Faulted` semantics, `:129`** — one of the six places recording the question as undecided. Its
undecided sentence is unchanged; the sentence before it was flat.

- Was: "`Faulted` surfaces it as a structured outcome variant rather than a raw exception at the runtime
  boundary."
- Now: "`Faulted` is where it would surface if it were reported as a structured outcome variant rather than
  as a raw exception at the runtime boundary."

**8. § Cross-References, the `fault-system.md` row.**

- Was: "a breach of one of the engine's own invariants is returned as the `EventOutcome.Faulted(Fault)`
  variant".
- Now: "should a breach of one of the engine's own invariants ever be reported as a structured value, it is
  returned as the `EventOutcome.Faulted(Fault)` variant".

### `docs/runtime/fault-system.md`

**9. § Deliberate Exclusions, the `FaultException` bullet, `:326`.**

- Was: "Faults are returned as the `EventOutcome.Faulted(Fault)` variant, never thrown (Q1, resolved), so
  no fault-carrying exception type exists in the design."
- Now: "Should anything ever construct a `Fault` (Q3), it is returned as the `EventOutcome.Faulted(Fault)`
  variant and never thrown (Q1, resolved), so no fault-carrying exception type exists in the design."

**10. § Q3, the parked-by-the-owner paragraph, `:320`** — this had to move with the others, because it
names the three sites and said they stand **as written**, which stopped being accurate the moment their
wording changed.

- Was: "…and the commitment that a fault is returned as a structured value inside the outcome hierarchy and
  never thrown — Q1 above, `result-types.md` § Overview, `runtime-api.md` § Deliberate Exclusions — stands
  as written."
- Now: "…and the commitment that a fault, if one is ever constructed, is returned as a structured value
  inside the outcome hierarchy and never thrown — Q1 above, `result-types.md` § Overview, `runtime-api.md`
  § Deliberate Exclusions — stands. Those three sites state it conditionally on this question rather than
  flatly, which is the same commitment: the shape a `Fault` takes when it reaches a caller is settled, and
  whether one ever does is what is parked."
- The commitment is not weakened. What changes is that the paragraph now says how the three sites word it,
  so a reader who compares them against this paragraph does not find a discrepancy.

**11. § Cross-References, the `result-types.md` row, `:341`.**

- Was: "a fault is returned as the `EventOutcome.Faulted(Fault)` variant within this hierarchy".
- Now: "should a fault ever be constructed it is returned as the `EventOutcome.Faulted(Fault)` variant
  within this hierarchy".

### `docs/runtime/runtime-api.md`

**12. § Deliberate Exclusions, the no-fault-type-definitions bullet, `:904`.**

- Was: "a breach of one of the engine's own invariants would surface as the `EventOutcome.Faulted(Fault)`
  variant, never thrown."
- Now: "should a breach of one of the engine's own invariants ever be reported as a structured value, it
  surfaces as the `EventOutcome.Faulted(Fault)` variant and is never thrown. Whether it is reported that
  way at all, or stays an unrecoverable exception, is undecided (`fault-system.md` Q3)."
- The "would" here was doing conditional work already, but on the wrong condition — it read as *if a breach
  occurs*, which still asserts that a breach becomes a `Fault`.

### `docs/compiler-and-runtime-design.md`

**13. § Precept Innovations, the structured-outcomes-taxonomy bullet.**

- Was: "Every runtime operation communicates through a structured outcome — success, domain rejection,
  boundary validation, or engine-invariant fault. There are no exceptions, no error codes, no untyped
  failures."
- Now: the list of structured outcomes is success, domain rejection and boundary validation; "There are no
  exceptions, no error codes, no untyped failures **on any of those paths**"; and a closing sentence names
  the engine-defect case as the one outside that, with `Fault` as the declared classification, nothing
  constructing one, and the report shape undecided.
- This bullet contradicted `:826` of its own document, which says the question is undecided.

**14. § Commit outcomes by operation** — a paragraph added under the table; no cell changed.

- The table's fourth column heads "Invariant breach" and gives `Fault` for `Create`/`Fire`, `Update` and
  `Restore`, with no qualification.
- Added: the column says which classification an engine defect would fall under rather than what any
  operation returns today, nothing constructs a `Fault`, and whether an engine defect is reported as one is
  undecided. The paragraph also records the second thing wrong with the column — `result-types.md` and
  `runtime-api.md` give `UpdateOutcome` and `RestoreOutcome` no fault variant — and says that gap closes
  when the producer question does.
- **Why the second half is recorded rather than fixed**: [`fault-backstop-fixes.md`](fault-backstop-fixes.md)
  § 3 Noted dispositions that discrepancy as descriptive and folds it into the open producer question. This
  pass does not reopen that; it makes the disposition visible to a reader of the table, who previously had
  no way to know the column was provisional.

---

## 4. The two pass records, corrected

Neither is canonical. Both said the flat sentence was kept on a test that did not consider the open
question, and a later reader diffing canon against them would find an unexplained difference.

- [`fault-backstop-model.md`](fault-backstop-model.md) — the "Keep `:1431`" decision and its restatement
  now carry a note that the sentence was kept as true under the corrected model, that the test applied did
  not ask whether it presumes the answer to the producer question, and that it was made conditional on
  2026-08-11 by this pass.
- [`fault-backstop-pass.md`](fault-backstop-pass.md) — the `N25` row's "kept and moved into § Fault
  Production" line carries the same note.

Neither correction reverses anything. The move into § Fault Production was right and stands.

---

## 5. What was deliberately left alone

| Site | Why |
|---|---|
| The six canonical places recording the question as undecided — `compiler-and-runtime-design.md:826`, `fault-system.md` Q3, `evaluator.md` § Fault Production's open note, `result-types.md` § `Faulted` semantics, `runtime-api.md:171`, `precept-language-spec.md:1963` | They already say what they should. Q3's parked paragraph and the `Faulted`-semantics lead-in were touched for the reasons at sites 7 and 10; neither changes what the note records |
| `fault-system.md` Q1's options table, and its closing "the prototype used exceptions … the redesign does not" | Both sit inside Q1, whose resolution sentence now opens conditionally, and both are the record of how the *shape* was chosen. Re-hedging the individual rows of a decision table would make it unreadable |
| `evaluator.md:1588`, `:1636`, `:1742`, `:1957` — "all in-domain failures produce structured outcome variants", "the evaluator never throws exceptions for expected business events", "no exceptions, no error codes, no magic strings" | All scoped to in-domain failures, which are settled. An engine defect is not an in-domain failure, and the 2026-07-28 pass deleted the one row that put it in that table |
| `docs/compiler/graph-analyzer.md:72` — "malformed lifecycle structure would surface as runtime faults during evaluation" | A counterfactual about the stage's absence, so not false. [`fault-backstop-fixes.md`](fault-backstop-fixes.md) § 3 Noted already left it and says it is worth a row of its own. Unchanged here for the same reason |
| `docs/tooling/mcp.md:240`, `:242`, `:269` — "Runtime Faults", "Prevents Fault" | Verbatim section headings in the text the MCP tools emit. Renaming them in the document without renaming them in `tools/Precept.Mcp/` creates drift; renaming both changes a tool's output contract. Left twice before, on the same reasoning |
| `docs/compiler/diagnostic-system.md:619`, `:652`, `:664` — the PRECEPT0004 analyzer rule that every `Fault` is constructed via `Faults.Create()`, and `Fault` described as a public output type | The rule constrains *how* a `Fault` would be constructed if one were, which is exactly the conditional form. `:102` in the same document already says nothing constructs one today |
| `docs/runtime/runtime-api.md:148` — a caller sample mapping `EventOutcome.Faulted` to a thrown `InvalidOperationException` | Host code deciding what to do with an outcome, not canon saying what Precept produces |
| Variant declarations and tables — `evaluator.md:337`, `result-types.md:103`, `:115`, `fault-system.md:95` | They say the type and the variant are declared, which is true and is not a claim about a producer |
| `docs/glossary.md:113`, `docs/runtime/README.md:36`, `docs/compiler/diagnostic-system.md:102`, `docs/language/precept-language-spec.md:1963` | Each already states that nothing constructs one, or that the question is parked |
| Everything under `src/` and `tools/` | Not read and not touched. This pass makes no claim about what the code says |

---

## 6. What this pass did not do

- The producer question was not answered, narrowed or moved. It is deferred by the owner's ruling of
  2026-08-11 and recorded as a named exception in the plan.
- No content was deleted. Every change is a re-wording, an added qualifying sentence, or both.
- No exception-side account was rewritten. Sites 3 and 4 add a pointer beside the account; the account
  itself is untouched, so the open note that cites both by name still resolves.
- No code was read or changed, and no `.precept` file was compiled.
- Nothing was committed.
