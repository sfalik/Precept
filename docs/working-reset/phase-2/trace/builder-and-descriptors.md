# Trace — the Precept Builder, the descriptor types, and the runtime area README

**Range**: `b93d1db1..HEAD` (`dbc2a3db`)
**Files**: `docs/runtime/precept-builder.md`, `docs/runtime/descriptor-types.md`, `docs/runtime/README.md`
**Hunks examined**: 37, counted at three lines of context — plain `git diff b93d1db1..HEAD -- <file>` —
made up of 27 in `precept-builder.md`, 7 in `descriptor-types.md` and 3 in `README.md`. At zero context
(`git diff -U0`) the same range separates into 48 regions (35 / 9 / 4). Every entry below is written
against the three-line grouping, so a hunk here may cover two adjacent edits that zero context would
split.

Four commits touch these three files in the range, and every hunk belongs to one of them:

| Commit | What it did here |
|---|---|
| `84577ee1` | Removed the model in which the runtime re-evaluates the definition's rules against the finished configuration |
| `c9193ec8` | Removed the model in which the compiler plants a runtime check for an obligation it could not prove, and added the certificate gate to the builder's failure modes |
| `3fb04266` | Added an index of open questions to each document and to the area README, and filed the question the owner parked |
| `39f11e90` | Added one sentence saying the builder's account of an engine defect is one of two that disagree |

## Summary

These three documents were changed for two reasons that account for nearly all of it: the owner's own text
says the runtime does not re-evaluate rules after a change and runs no fault-prevention checks, and both
models were still specified here in detail — a whole builder pass, a record type, an opcode field, an
array on the `Precept` class, and the section that presented per-operation constraint evaluation as a
design achievement. The same passes added the load-time certificate gate his text requires and had never
appeared in the builder's document.

Of the 37 hunks: **27 rest on his own text** in the want document; **4 are internal repairs**, where a
document claimed something the code plainly disagrees with; **1 traces to nothing he wrote or ruled**; and
**5 carry two changes each** and split across categories, which the entries below say for each. The two
changes that rest on a **ruling he gave in conversation** — both about the parked question of whether
anything ever constructs a `Fault` — sit inside two of those five mixed hunks. Counted at the level of the
change rather than the hunk, the one thing that traces to nothing is a single editorial decision, to add
an index of open questions to each document; it is described in full in the last section.

---

## `docs/runtime/precept-builder.md`

### Pass 6 renamed, and the planting mechanism removed — HEAD `:34`, `:117`, `:134`, `:155`, `:199`, `:223`, `:283`, `:302`, `:424`, `:457`, `:558`, `:583`, `:602`, `:691`, `:945` (15 hunks)

**What changed**: The builder no longer stamps anything on an opcode for a fault. `FaultSiteAnnotation`
and its planting pseudocode are gone from Pass 4; the `Opcode` base record loses its `FaultSite` property;
`Precept.FaultBackstops` is gone from the executable model; Pass 6 is renamed from *Fault Backstop Pass* to
*Constraint Influence Pass* and now does only the `ConstraintInfluenceMap` work it always also did; and
`FaultSiteLinks` is dropped from every description of what the builder reads out of `ProofLedger`.

**Warrant**: Category 1 — his own text, twice over.

> "But the runtime **can be lighter, because the compiler's work is complete** … 'Lighter' means
> concretely: **fault-prevention checks are omitted at evaluation time**. No divide-by-zero tests, no
> overflow guards, no re-checking of proven bounds on writes — the evaluator just computes."
> — `what-i-want-2026-07-16.md:165`

> "In an ideal world there is **no deferral**. A business rule whose enforcement the compiler cannot prove
> complete is rejected — never handed to the runtime on the compiler's own initiative."
> — `what-i-want-2026-07-16.md:171`

The removed mechanism was defined as existing *only* for obligations the proof engine could not discharge
("Present ONLY for Unresolved proof obligations"), which is exactly the handover his second sentence
refuses. The replacement paragraph states the consequence rather than the mechanism: an obligation that
cannot be discharged refuses the definition, so no `Compilation` reaching the builder carries an unproven
site.

### The certificate gate — HEAD `:42` (contents), `:637`–`:645`, `:663`–`:670`, `:681`, `:854` (5 hunks)

**What changed**: A new § The Certificate Gate under § 9 says `Precept.From` also runs the certificate
checker and refuses a definition whose proof record does not verify; the Input Contract gains that
condition; the Recovery Path says its four steps do not fit a definition the gate refuses; and the
always-valid guarantee now names both conditions the factory imposes rather than the `HasErrors` guard
alone. All four say the mechanism is not built and its failure shape is unsettled.

**Warrant**: Category 1 — his own text.

> "**A load-time gate in the runtime** — the same checker, run once when a definition is loaded. The
> runtime refuses to govern under a definition whose certificate does not verify: no unproven, tampered,
> or stale definition can ever govern. The guarantee is rooted in verification the runtime performed
> itself, not in provenance it assumes." — `what-i-want-2026-07-16.md:204`

The one thing the new section states as settled about the failure — that it must explain itself — is also
his:

> "This holds on both sides of the boundary: compile-time rejections and runtime refusals alike must
> explain themselves clearly." — `what-i-want-2026-07-16.md:177`

This is also a mechanical consequence of an earlier phase-2 edit: `runtime-api.md` § Definition Load and
the Certificate Gate was added at `c3a9be21` on the same want-document lines, and `precept-builder.md`
owns `Precept.From` while describing a method with no gate. That earlier edit carries the same
category-1 warrant.

Corroborating, though not itself the warrant: the reset plan's § 3 row *"The certificate checker is in
scope, including its runtime load-time half"* was put to him at the walk and marked **mine** — his words,
*"mostly mine"* (`the-walk.md:100`), against a printed answer reading *"The checker and its runtime
load-time half is want:201–204 almost verbatim."*

### The per-operation constraint pass, and what the buckets are for now — HEAD `:144`, `:392`, `:761`, `:798`–`:812` (4 hunks)

**What changed**: The Out of Scope row for operation execution now says the evaluator checks each entering
value against the constraints declared on the slot it enters and does not evaluate the definition's rules
against the finished configuration. Computed slots are recalculated "before the working copy is committed"
rather than "before constraint evaluation". Decision 3's rationale no longer claims the buckets exist to
make a per-operation scan cheap. § 12's *Dispatch-Optimized Constraint Indexes* is rewritten as
*Constraint Indexes, and What They Index Now*, keeping the five anchors as declaration forms and naming
the two uses that survive.

**Warrant**: Category 1 — his own text.

> "Once the compiler has proven every rule, the runtime does **not** re-evaluate them against the
> post-mutation configuration" — `what-i-want-2026-07-16.md:199`

He confirmed separately at the walk that this line is his and is not in question (`the-walk.md:170–172`,
recorded under item 4).

The blockquote added at the end of § 12 — that whether the index keeps its five state-and-event anchors is
now open — is the editor recording a consequence rather than deciding it: the shape's stated justification
was the pass that his line removes, and the two surviving uses are anchored to a slot and to a catalog
listing. Leaving the question open rather than answering it is the editor's judgement, and the cross-reference
it depends on holds (`evaluator.md:1351`, `:1355`, `:1686`).

### The builder's account of an engine defect is one of two — HEAD `:651` (part of 1 hunk)

**What changed**: One sentence added under § Builder Invariant Violations. The paragraph above it, which
says the builder throws `InvalidOperationException`, is unchanged; the new sentence says
`compiler-and-runtime-design.md` names the evaluator as where such a breach would surface as a `Fault`
instead, and that which of the two holds is undecided.

**Warrant**: Category 2 — his ruling, and Category 3 — an internal contradiction.

The ruling, given in conversation on 2026-08-11 and recorded at `compiler-readiness-plan.md:195`:

> "do we need to decide this now?  seems like something we can defer until we start building the runtime"

and *"yes"* to writing it up. The plan records the standing position with it: "the lean is exception, a
lean is not a ruling, and nothing may be built or written as though exception were the answer." The
section stated the exception account flatly and a reader arriving at it directly read a settled account,
which is what the added sentence stops.

The contradiction the repair records is real and both sides are still in the tree: this document and
`evaluator.md` § No Unclassified Exceptions say a corrupted model is an unrecoverable exception, while
`compiler-and-runtime-design.md` names the evaluator as where the breach becomes a `Fault`.

**On the specific worry this trace was asked to check**: no sentence added to this document in this range reads
as though the parked question were settled. The three places that touch it — this sentence, the § 13 index
entry, and the README's open-questions row — each say it is undecided and each say the lean is not a
ruling.

### Two source-file rows made honest about code that still declares the removed types — HEAD `:959`, `:967` (2 hunks)

**What changed**: The `Descriptors.cs` row now says the file also declares `FaultSiteDescriptor`, which no
builder pass produces and which is on its way out. The `ProofLedger.cs` row says the file also declares
`FaultSiteLink` and `FaultSiteAnnotation` and still carries a `FaultSiteLinks` field, that no builder pass
reads any of the three, and that the removal has not been made.

**Warrant**: Category 3 — internal repair. The document's own design no longer describes these types while
the code still declares them: verified at HEAD, `src/Precept/Runtime/Descriptors.cs:60` declares
`FaultSiteDescriptor`, and `src/Precept/Pipeline/ProofLedger.cs:8`, `:117` and `:124` declare the field and
the two records. `CLAUDE.md` § Doc status conventions requires drift to be fixed in the same pass and
reported. This traces to no position of his; it was found by two agent-run reviews of the uncommitted diff
(`fault-backstop-review-deletions.md` finding `B3`, `fault-backstop-review-coherence.md` finding 7).

### § 13 renamed and given an Open Questions index — HEAD `:65` (contents), `:858`–`:871` (1 hunk plus part of another)

**What changed**: § 13 *Implementation Notes* becomes § 13 *Open Questions / Implementation Notes*, with a
new first subsection indexing eight questions the document already left undecided.

**Warrant**: Split. The eighth entry at `:871` — whether a defect in Precept itself reaches the caller as a
structured outcome or a thrown exception — is Category 2 on the 2026-07-28 quotation given in the short
list below. The first three entries (`:864`–`:866`) restate questions raised by changes in this same range
and inherit their Category 1 warrants: the two certificate-gate entries follow the section added above, and
the `ConstraintPlanIndex` entry follows the § 12 rewrite. The remaining four entries, and the decision to
add an index at all, are Category 4 — see the last section.

---

## `docs/runtime/descriptor-types.md`

### `FaultSiteDescriptor` removed from the design — HEAD `:40` (Overview), `:101`–`:105` (Descriptor Shapes), `:136` (evaluator downstream), `:152` (invariants), `:165` (Innovation) (4 hunks and part of a fifth)

**What changed**: The record declaration is gone, along with the invariant tying its `PreventedBy` to the
`[StaticallyPreventable]` attribute, the line saying the evaluator uses it for backstop routing, the
Innovation bullet calling it the proof/runtime link, and "fault sites" in the definition of what a
descriptor represents.

**Warrant**: Category 1 — the same two want-document lines as the builder's removal, `:165` and `:171`. The
removed type existed to carry a runtime check for an obligation the compiler could not discharge, which
his `:171` refuses outright.

### Implementation State and Source Files disclose the record that is still in the code — HEAD `:8`, `:206` (2 hunks)

**What changed**: The status row now says `Descriptors.cs` declares a fifth record this document no longer
describes. The Source Files row, which read "Implemented — all 5 descriptor sealed records", now names the
four the document describes plus the fifth it does not, gives the reason there is no such residue to plant,
and says the removal has not been made.

**Warrant**: Category 3 — internal repair. The question asked of this one is answered
plainly: **it traces to nothing he wrote or ruled.** The document claimed "Implemented" for a set of five
records while its own § Descriptor Shapes described four, and the code still declares the fifth
(`src/Precept/Runtime/Descriptors.cs:60`, verified at HEAD). The two statements that contradicted each
other are inside this document and against the tree. The reasoning the disclosure gives for why no such
residue exists does rest on his `:171`, but the disclosure itself is the editor's obligation under
`CLAUDE.md`, not his position. It was prompted by the two agent-run reviews named above, not by him.

The same is true of the two `precept-builder.md` § 16 rows above. Four hunks across the two documents are
this one repair.

### Contents and § Notes restructured, with two indexed questions — HEAD `:31`–`:33`, `:170`–`:178` (parts of 2 hunks)

**What changed**: `## Notes` becomes `## Open Questions / Implementation Notes` with `### Open Questions`
added above the existing note, which is kept unchanged as `### Notes`. Two questions are indexed.

**Warrant**: Category 4 — see the last section. The second of the two entries, on
`FieldDescriptor.AccessModes`, is the weakest thing in this whole range and is called out there.

---

## `docs/runtime/README.md`

### The fault system's one-line description and the cross-cutting bullet — HEAD `:14`, `:67` (2 hunks)

**What changed**: The document table's description of `fault-system.md` changes from "the runtime mirror of
the compiler's diagnostic system" to "each names a class of failure and the compiler diagnostic that
prevents it, paired at build time". The cross-cutting bullet keeps its heading and adds that the mirroring
is a build-time pairing rather than one thing happening at compile time and another at run time.

**Warrant**: Category 1 — `what-i-want-2026-07-16.md:165` again. "Runtime mirror" describes a run-time
counterpart to each diagnostic, which is what his sentence removes. The two lines also disagreed with each
other after the fault-backstop pass corrected only the second one, which makes this an internal repair as
well; the underlying warrant is his.

### Reading order item 3 — HEAD `:23`

**What changed**: `evaluator.md` is described as "plan execution and ingress validation" rather than "plan
execution and constraint evaluation".

**Warrant**: Category 1 — `what-i-want-2026-07-16.md:199`, the same line as the § 12 rewrite above, and
`:143` for what ingress validation is ("the applicable **rules** are evaluated — however they were spelled").

### The § Open questions table — HEAD `:28`–`:56`

**What changed**: A new section with a 21-row table listing every open question across the six runtime
documents, plus a closing paragraph naming three entries that sit under an open-questions heading but are
not open.

**Warrant**: Split. The row for the `Fault`-producer question is Category 2 on the quotation in the short
list. The rows summarising questions raised by changes in this range inherit those changes' Category 1
warrants. The table itself is Category 4.

---

## Traces to nothing he wrote or ruled

> **Dispositioned 2026-08-26 — the item is closed.** The open-question indexes **stay** — *"agree"*. One
> entry comes out, the sub-item this section already singles out: the `FieldDescriptor.AccessModes` entry
> has been deleted from `descriptor-types.md`, because an index that points at questions written in other
> documents becomes a second place to maintain. The finding below is left as it was written and describes
> the file before that deletion. His words and his reasons:
> [`../trace-walk-rulings.md`](../trace-walk-rulings.md).

One item. It is an editorial decision made across all three documents in `3fb04266`, and it changes no
claim: nothing was resolved, narrowed, moved or contradicted, and the working paper for the pass
(`runtime-open-questions.md`) states that plainly.

### Indexing the open questions

**Where at HEAD**: `docs/runtime/README.md:28`–`:56`; `docs/runtime/precept-builder.md:65` and `:858`–`:871`;
`docs/runtime/descriptor-types.md:31`–`:33` and `:170`–`:178`.

**Before**: none of the three sections existed. `precept-builder.md` § 13 was headed
`## 13. Implementation Notes`; `descriptor-types.md` had `## Notes`; the runtime README ran straight from
§ Reading Order to § Relationship to Other Docs.

**After**, in the README:

> ## Open questions
>
> Every runtime document now carries an `Open Questions / Implementation Notes` section with an index of
> what it leaves undecided. The discussion of each question stays in the section that raised it — the
> indexes and this table only make them findable. Ordered by the document that owns the question.

followed by a 21-row table, and closing:

> Three entries that sit in an `Open Questions / Implementation Notes` section are **not** open, and are
> deliberately absent from the table above: `runtime-api.md` § Stateless Precepts — CreateInitialVersion is
> locked; `fault-system.md` § Q1 is resolved; and all three entries in `result-types.md` are requirements
> recorded against the executable model contract …

**After**, in `precept-builder.md`:

> ### Open Questions
>
> An index of what this document leaves undecided. Each entry says what somebody has to decide and points
> at the section where the case for it is written; that discussion stays where it is, because it depends on
> the material around it.

with eight bullets, and the same shape in `descriptor-types.md` with two.

**What was searched, and not found**: the want document was read in full — it has no line about
documentation structure, indexes, findability, or where an undecided question should be recorded. It does
keep its own `## Open questions` section (`:217`) with two items marked "Deliberately left open", which is
the closest thing to a precedent and is a practice in his own document rather than an instruction about
canon. The walk's 41 marked items contain nothing about document organisation; item 29 is the nearest, and
it is about coined vocabulary making a document unreadable, not about where questions live. The plan's § 3
*Ruled* rows and § 4 *Waiting on Shane* rows contain nothing on it. Case-insensitive searches of both the
want document and the walk for *findable*, *index*, *navigat*, *table of*, *can't find* / *cannot find* /
*hard to find* return nothing relevant.

**Two things that do trace, and are not part of this item.** First, the entries about the `Fault`-producer
question are his, and are quoted in full below. Second, the entries summarising the certificate gate, the
`ConstraintPlanIndex` shape and the removed types are summaries of changes made elsewhere in this range on
his text, and inherit those warrants — an index entry that points at a warranted section is a mechanical
consequence of that section existing.

**The narrowest sub-item, which the pass itself flagged.** The `descriptor-types.md` entry on
`FieldDescriptor.AccessModes` indexes a question that is not written in that document at all — the printed
`FieldDescriptor` shape does not carry the property. The entry is at `descriptor-types.md:177`; the question
itself is written in `precept-builder.md` § Pass 1 and `evaluator.md` § 7.5, about a type this document
owns. `runtime-open-questions.md:117–120` names it as a
judgement call and says: "If that reads as over-reach it is one line to remove." It was still there at HEAD
when this was written; it was removed on 2026-08-26.

### The `Fault`-producer entries, which do trace

Quoted here so the short list above is not read as covering them. In `precept-builder.md:871`, in the
README table at `:32`, and in the sentence at `precept-builder.md:651`, the question is recorded as parked
with the note that a lean is not a ruling. His words, recorded in `runtime-open-questions.md:31` as given
on 2026-07-28:

> "my vote would be exception, but I think we should park this until we get to the runtime design."

and again in conversation on 2026-08-11, recorded at `compiler-readiness-plan.md:195`:

> "do we need to decide this now?  seems like something we can defer until we start building the runtime"

with *"yes"* to writing it up. Both are Category 2.

---

## Recorded, not fixed

One observation, offered because the brief asks for changes I think are wrong to be recorded rather than
repaired.

**The Recovery Path's verb.** `precept-builder.md:663` was "If `Precept.From` throws:" and is now "If
`Precept.From` refuses because the `Compilation` carries errors:". The change is warranted — the section
now has to cover two refusal routes and the certificate gate's failure shape is undecided, so a neutral
verb avoids presuming it. The side effect is that a route whose mechanism *is* settled now reads without
it: the code block twelve lines above still shows `throw new InvalidOperationException`, and § No Partial
Builds still says the builder "either produces a complete, valid executable model or throws". Nothing is
false; the settled half is just described more vaguely than the document describes it elsewhere.
