# Trace — `docs/runtime/evaluator.md`

**Range**: `b93d1db1..HEAD` **Files**: `docs/runtime/evaluator.md` **Hunks examined**: 47, counted at
three lines of context — plain `git diff b93d1db1..HEAD -- docs/runtime/evaluator.md`. At zero context
(`git diff -U0`) the same range separates into 99 regions. Every entry below is written against the
three-line grouping, so one hunk here often covers several adjacent edits that zero context would split;
that is why 47 hunks carry as much change as they do. (202 lines added, 255 removed, across six commits:
`c3a9be21`, `84577ee1`, `fa4891d7`, `c9193ec8`, `3fb04266`, `39f11e90`.)

## Summary

Almost all of this document's change is one thing done twice. First, the pass of 2026-07-27 removed the
step where the evaluator re-evaluated the definition's rules against the finished working copy and decided
whether to commit — replacing it with a check on values as they arrive from outside. Second, the pass of
2026-07-28 removed the separate machinery that re-tested a fault-prone operation (division by zero and its
family) while the operation ran. Both removals have the owner's own text behind them, one of them
reinforced by a ruling he gave at the phase-1 walk, and everything else in the diff is either downstream of
those two removals or a repair of a place where this document already disagreed with another canonical
document.

Of the 47 hunks: **31 trace to the owner's own text** (category 1), **3 to a ruling he gave** (category 2),
**7 are internal repairs** (category 3), and **6 trace to nothing he wrote or ruled** (category 4) — five
of those six are the new Open Questions index, which asserts nothing and only makes existing questions
findable, and the sixth is the sentence narrowing what an update's ingress check evaluates, which is
written into the document carrying an explicit warning not to build against it.

---

## docs/runtime/evaluator.md

### The evaluator no longer re-checks rules after mutating — `:19`, `:115`, `:141`, `:167`, `:182`, `:333`, `:357`, `:466–512`, `:531`, `:600–651`, `:734–763`, `:1351–1390`, `:1663–1683`, `:1734`, `:1748`, `:2039`, `:2085–2090`, `:2129–2150`

**What changed**: every place that described the evaluator building a working copy, walking the `always` /
`from` / `on` / `to` constraint buckets against it, and committing only if they all passed. The pseudocode
for `Create`, `Fire` and `Update` loses that step; the outcome `EventOutcome.ConstraintsFailed` is deleted
from the outcome union; § 7.6 is renamed from *Constraint Evaluation* to *Ingress Validation* and its
function is rewritten to check only values arriving from outside; the guarantees table swaps *Constraint
completeness* and *Post-mutation evaluation* for *Ingress completeness* and *Checked on entry, not after*;
and the cross-reference matrix at the end now says no operation evaluates a constraint against the finished
working copy.

**Warrant**: **category 1**, and the plainest in the whole diff.
`what-i-want-2026-07-16.md:199` — "Once the compiler has proven every rule, the runtime does **not**
re-evaluate them against the post-mutation configuration — but it does not blindly trust the compiler
either."
`want:165` — "'Lighter' means concretely: **fault-prevention checks are omitted at evaluation time**. No
divide-by-zero tests, no overflow guards, no re-checking of proven bounds on writes — the evaluator just
computes. What runs at runtime are the declared premises (ingress validation of args and editable fields,
guards) that the compiler's proofs depend on, and nothing beyond them."
`want:26` — "The premises are also what the runtime enforces — governance, not deferred proof, because the
compiler has proven those checks *sufficient*."
The replacement's shape comes from `want:141` — "Ingress is exactly two points: **event args** (including
on initial/construction events) and **editable-field writes**."

### The same removal, as the owner ruled it in conversation — supporting warrant for the above

**Warrant**: **category 2**. At the walk of 2026-07-26 the question was put to him as a worked case — an
account with `rule Balance >= -OverdraftLimit`, both values supplied from outside, and an event lowering
the limit — with two options side by side. `the-walk.md` item 16 records them and his choice: the first
option is "the compiler refuses the definition until the author writes a condition that makes it
provable"; the second, which he did not choose, is "the compiler accepts it and the runtime evaluates the
rule on every operation and discards the change if it breaks." The second option is a description of the
code this document used to print. His marked words on the canon sentence that stated the opposite:
*"that was derived, I did not write that exactly."*

### `Create` and the stateless `Create` commit unconditionally — `:600–651`

**What changed**: both `Create` pseudocode blocks lose their constraint check and their
`ConstraintsFailed` return, with a comment saying the compiler proved the default configuration satisfies
every rule. The CC#26 stateless list changes "`to <State> ensure` — not evaluated" to "structurally absent
… so nothing to prove and nothing to evaluate", and adds "Global rules — proven at compile time, not
evaluated here."

**Warrant**: **category 1**. `want:18` — "**Base case.** The default configuration satisfies every rule,
and every initial event *establishes* every rule, provable from the constraints declared on its args." A
proven base case leaves nothing for the constructor to check.

### A `reject` row returns without executing anything — `:466–480`, `:657–664`

**What changed**: the flow diagram and the `Fire` pseudocode now return `Rejected(reason)` the moment a
reject row wins dispatch, before the working copy is snapshotted, instead of carrying the row through the
pipeline and switching on its outcome at the end.

**Warrant**: **category 1**. `want:102` — "**`Withdraw`** — the guard is the proof … The fallback row
rejects with an explanation and writes nothing."

### No candidate list, no dispatch ambiguity — `:466`, `:651–717`, `:844–872`, `:910`

**What changed**: `Fire` no longer collects passing rows into a candidate list and resolves among them; the
first row whose guard passes is executed and returned. Inspection changes to match: a row after the first
`Certain` row is reported `Impossible` rather than the whole inspection dropping to `Possible` for
"multiple candidates — ambiguous". A new paragraph at `:717` explains why the loop can return early, and
one at `:910` replaces the multiple-candidate paragraph.

**Warrant**: **category 3, an internal repair**, made possible by the category-1 removal above. Two
canonical statements disagreed, and both predate this range. `precept-language-spec.md` § 3A.4 phase 2
said, unchanged at `b93d1db1`: "the transition rows for the current (state, event) pair are evaluated in
declaration order; the first matching guard wins and the remaining rows are not evaluated." This document
said instead: "Multiple-candidate handling: When inspection finds more than one passing row,
`overallProspect` is set to `Possible`." `result-types.md` § *Prospect propagation rules (first-match)*
also predates this range unchanged and says "A `Certain` row makes all subsequent rows `Impossible`" —
which is what this document now says. The only reason the old evaluator had to keep evaluating rows past
the first matching guard was that a later constraint check could still disqualify one; with that check
gone, the contradiction has nothing left holding it up.

### `EventOutcome` loses `ConstraintsFailed`; `UpdateOutcome` keeps it, re-described — `:333`, `:357`, `:1593`, `:1953`

**What changed**: the event outcome union drops the variant; the update outcome union keeps it and its
comment becomes "Ingress failure on a patched value — refused before application". The in-domain failure
table and the caller sample follow.

**Warrant**: **category 1**, inheriting the first entry's warrant. `want:199` removes the post-mutation
re-evaluation that `EventOutcome.ConstraintsFailed` reported; `want:141` and `want:143` keep the
editable-field write as one of the two ingress points, which is what the surviving update variant now
reports. The sibling document `result-types.md` was changed to match in the same phase and carries the
same account at `:129`.

### The fault-backstop machinery is gone — `:19`, `:56`, `:141`, `:315`, `:435`, `:977–985`, `:1412–1429` (deleted), `:1420`, `:1433`, `:1590–1620`, `:1663`, `:2044`, `:2126`, `:2160`

**What changed**: the removal of everything describing a check *for a fault* performed while an operation
runs — the `FaultSiteAnnotation` on opcodes and the `IsFaultCondition` test in the dispatch loop, the
"two-layer defense model", the responsibility row for fault-backstop routing, `Precept.FaultBackstops`,
the § *Impossible-Path Failures (Defense-in-Depth)* section with its nine-row table and `FaultCode`
excerpt, and the summary line at the top of the document. `Fail` survives, marked "Declared; nothing calls
it."

**Warrant**: **category 1**. `want:165` — "**fault-prevention checks are omitted at evaluation time**. No
divide-by-zero tests, no overflow guards, no re-checking of proven bounds on writes — the evaluator just
computes."
`want:104`, on the worked example's division — "At runtime there is no zero-check anywhere — the ingress
validation of the arg is what makes the division safe, and the certificate records that dependency."
`want:171` — "In an ideal world there is **no deferral**. A business rule whose enforcement the compiler
cannot prove complete is rejected — never handed to the runtime on the compiler's own initiative." A
backstop is a check standing behind an unproven site, and under prove-or-reject no such site reaches a
built model.

### The document's opening claim and the guarantees row lose their hedge — `:19`, `:1663`

**What changed**: "No diagnostics means no evaluator faults — compiler proofs are **intended to** eliminate
runtime fault paths, leaving evaluator faults as defense-in-depth only" became "A compiled definition has
no fault path at evaluation time". The guarantees table's "If the compiler emits no errors, the evaluator
should never fault" became a flat statement.

**Warrant**: **category 1**. `want:189` — "The guarantee must be exactly that: a guarantee." "Intended to"
and "should never" state as an aspiration what `want:165` states as a property.

### `Fault` re-described as a breach of the engine's own invariants — `:333`, `:1602–1604`, `:1632`

**What changed**: the `Faulted` variant's comment, the new opening sentence of § Fault Production, and the
recovery-path row now describe a fault as a defect in the compiler, builder or evaluator rather than as an
impossible path the type checker should have prevented.

**Warrant**: **category 3, an internal repair**, and the weaker of the two halves. The old description
named a producing occasion — "impossible-path failure", "only fires when upstream pipeline has a bug" —
which the category-1 removal above eliminates, leaving a type with a description of something that no
longer happens. `compiler-and-runtime-design.md:826` already carried the replacement wording, so this is
this document being brought into line with a sibling rather than a new position. The owner's text does not
mention engine defects at all, which is why this is category 3 and not category 1.

### The fault outcome shape is stated conditionally, and the open question is written down — `:1621`, `:1623–1625`, `:1632`, `:1640`

**What changed**: "Faults are **never thrown** — they are returned as structured outcome variants" became
"Should anything ever construct a `Fault` (see the open question below), it is **never thrown** …". A
blockquote was added recording that whether anything constructs a `Fault` is undecided and parked. A
sentence was added under § No Unclassified Exceptions saying the corrupted-model-is-an-exception account is
one of two accounts canon carries and that which one holds is undecided.

**Warrant**: **category 2**, his words, twice.
Recorded in `runtime-open-questions.md:30–31` (2026-07-28): *"my vote would be exception, but I think we
should park this until we get to the runtime design."*
Recorded in `compiler-readiness-plan.md` § 4 (2026-08-11): *"do we need to decide this now? seems like
something we can defer until we start building the runtime"*, and *"yes"* to writing it up.
The wording "a lean is not a ruling", repeated at every site, is the honest reading of the first quotation:
a vote he explicitly declined to cast yet.

### § 13 gains an Open Questions index — `:88–89`, `:2005–2019`

**What changed**: § 13 was renamed from *Implementation Notes* to *Open Questions / Implementation Notes*
and an eleven-entry index was added as its first subsection, one line per question with a pointer to the
section where the question is actually written. No inline prose moved.

**Warrant**: **mixed.** One entry — the parked fault-producer question — is **category 2** on the
quotations above. The remaining ten index questions that were already written inline in this document
before this range (`LOAD_ARG` on absent input, `BRANCH_FALSE` on numeric zero, the `PreceptValue` layout,
the trace record shape, `stackalloc` versus heap, `Version.Slots` ownership, `FieldDescriptor.AccessModes`
storage, the two state-action questions owned by the spec, and the two below), so the index asserts nothing
new. But the decision to build an index at all, and the section rename, **trace to nothing he wrote or
ruled** — **category 4**. See the short list; the stakes are low, because a navigation aid over unchanged
questions cannot be wrong about the language.

### What inspection reports where there is no constraint verdict — `:846`, `:912–914`, `:917–921`, `:941–948`

**What changed**: the inspection pseudocode no longer evaluates constraints to compute a row's prospect —
the guard alone decides. `BuildConstraintResults` is left unspecified and a paragraph declares that what
should fill `TransitionInspection.Constraints`, `UpdateInspection.Constraints` and
`EventInspection.EventEnsures` is an open question. The old `OQ-4 (pending): whether EventEnsures should
move inside TransitionInspection … Pending Shane's call` was replaced by a pointer to that question.

**Warrant**: **category 1**, inherited from the first entry, with `want:211` quoted in the new text
itself — "Inspect can cite provenance, not just verdicts: 'no check needed here — proven from these
premises'". OQ-4's two options both presupposed evaluating event ensures against a post-mutation working
copy, which `want:199` removes; the question was not closed, it was replaced by one that does not
presuppose the removed mechanism. See the note at the end about the *Pending Shane's call* label.

### Decision 5 — restored data cannot be validated after the fact — `:1756–1761`

**What changed**: "If validation is needed: Callers who need to validate restored state should call
`version.InspectFire` or `version.InspectUpdate` after restoration" was replaced by "There is no facility
for validating restored state, and that is deliberate", with the reason and the post-MVP migration path.

**Warrant**: **category 1**, and the document quotes its source in place. `want:141` — "Restore/rehydration
is *not* ingress — data coming back from persistence is trusted and unchecked, because it was governed when
it was written. (Post-MVP, the restore path itself expands to handle schema evolution — data written under
an older definition.)" The old advice also stopped working mechanically once inspection had no constraint
verdict to report, so the same change is a consequence of the first entry.

### The broken cross-reference is repointed — `:2126`

**What changed**: the *Fault–diagnostic correspondence* row pointed at `docs/compiler/proof-engine.md
§Fault Backstops` and now points at `docs/compiler/diagnostic-system.md § FaultCode → DiagnosticCode
Chain`.

**Warrant**: **category 3, an internal repair.** No heading matching "Fault Backstops" exists in
`proof-engine.md` at `b93d1db1` — the reference was already dead before this range began. The new target
exists (`diagnostic-system.md:555`).

### The constraint buckets survive, with a stated reason — `:1353`, `:1683`

**What changed**: two added paragraphs say the `Always`, `FromState` and `ToState` buckets are still built
and still indexed, because they assemble the constraint catalogs and route an incoming value to the
constraints on the slot it enters — they are simply not walked against a finished working copy.

**Warrant**: **category 3, an internal repair.** Without it the removal would have contradicted
`runtime-api.md:81`, which lists `Constraints`, `ApplicableConstraints` and the "constraint exposure model
(all three tiers)" among the public API this engine owes. `precept-builder.md:761` and `:810` carry the
same reconciliation.

### Renames and renumbering carried through the document — `:47`, `:56`, `:63`, `:96`, `:167`, `:182`, `:1677`, `:1734`, `:1748`, `:1772`, `:2085–2090`, `:2129`, `:2039–2050`

**What changed**: contents entries and headings renamed *Constraint Evaluation* → *Ingress Validation*; the
LOC itemization, the bounded-complexity sentence, Decisions 1, 2, 6 and 7, the no-incremental and
no-concurrent exclusions all re-worded from "constraint evaluation" to "ingress validation"; resolved
question 8 marked moot; resolved questions renumbered 8–12 after item 10 (fault backstop threading) was
deleted.

**Warrant**: **mechanical consequences** of the two removals above, inheriting their category-1 warrants.
Nothing here states a position; each is a name or a number following the thing it points at.

### Two notes about `FaultSiteDescriptor` in the source-file lists — `:2027`, `:2160`

**What changed**: `Descriptors.cs` is now annotated as also declaring `FaultSiteDescriptor`, "which the
evaluator does not read and which is on its way out".

**Warrant**: **category 3, an internal repair** — canon disagreed with code. The type is still in
`Descriptors.cs` while the design that used it has been removed, and `descriptor-types.md:8` carries the
same disclosure. Recording the gap rather than claiming the type is gone is the honest form.

---

## Traces to nothing he wrote or ruled

### 1. The sentence narrowing what an update's ingress check evaluates — `:738`, `:1381`, `:1383`, `:2015`

**Before** (nothing — this text is new; the document said only "2. Apply patch to working copy" and § 7.6
carried no scope statement):

**After**, at `:736–740`, inside the `Update` pseudocode:

> `// 2. Ingress governance (§3A.4 phase 1): each incoming value is held to the`
> `//    constraints declared on the field it is entering — and nothing wider —`
> `//    before it is applied and before anything derives from it (spec §0.7).`
> `//    "Nothing wider" is disputed and is not settled canon — see the note in`
> `//    spec §0.7, Governance. Do not build against this scope.`

and at `:1381–1383`:

> `Update` runs the same check over the patched field values, against the constraints declared on each
> field it writes — and nothing wider (spec §0.7, *Governance*).
>
> > **"Nothing wider" is disputed and is not settled canon.** See the note in spec §0.7, *Governance*. Do
> > not build against this scope, and do not cite it as evidence.

and at `:2015`, as an index entry: "**Whether an `Update` holds each incoming value to the constraints
declared on the field it enters and nothing wider.** The 'nothing wider' scope is disputed and is not
settled canon".

**What was searched for and not found**: no line of the want document supports the narrow scope, and one
contradicts it. `want:143` says of an editable-field write that ingress validation evaluates "the field's
modifier-rules *and* every rule that mentions the field — an edit is an arbitrary incoming value that no
static proof can cover, so evaluating those rules at the ingress point is exactly the premise that closes
the editable write sites in the compiler's proof." The walk's own answer to item 16 repeats the wide
reading — "evaluating every rule mentioning an editable field at the editable write is a premise you name
(want:143)". Searching the walk for the narrow reading, under "nothing wider", "the slot it enters", "the
field's own constraints" and "declared on the field", turns up no ruling. `compiler-readiness-plan.md:204`
says as much in the plan's own voice: "the evidence cannot decide it — `want:143` and `want:171`/`:199` are
all yours and pull opposite ways, and every canonical sentence on the narrow side was written on 2026-07-27
to implement one reading of walk item 16", and the question is one of the prepared items still waiting on
him.

**In the change's favour**: every one of the three prose sites carries an explicit warning that the scope is
disputed and must not be built against or cited, and the question is indexed as open. So this is a
disclosed disagreement rather than a silent one. It is on this list because the sentence is nevertheless
written in canon's own voice, and because the four sites are new text added in this range — the document
said nothing at all about the scope before.

### 2. The Open Questions index and the § 13 rename — `:88–89`, `:2005`, `:2007`, and ten of the eleven entries at `:2009–2019`

**Before**, at the contents and at § 13:

> `- [13. Implementation Notes](#13-implementation-notes)`
> `## 13. Implementation Notes`

**After**:

> `- [13. Open Questions / Implementation Notes](#13-open-questions-implementation-notes)`
> `  - [Open Questions](#open-questions)`
> `## 13. Open Questions / Implementation Notes`
> `### Open Questions`
> `An index of what this document leaves undecided. Each entry says what somebody has to decide and points
> at the section where the case for it is written; that discussion stays where it is, because it depends on
> the material around it.`

followed by eleven one-line entries.

**What was searched for and not found**: nothing in the want document or in the walk asks for the runtime
documents to carry an index of their open questions, and no ruling in the plan's § 3 or § 4 mentions it.
The reason given is the pass's own, in `3fb04266`'s commit message: "Three of the six runtime documents
carried an Open Questions section; three did not, and all three had open questions anyway … Nobody could
find them." That is an editor's judgement about navigation, and a defensible one, but it is not his and it
is not a repair of a contradiction, so it lands here rather than in category 3. **The stakes are low**: no
entry states a position the document did not already state, ten of the eleven point at prose that was
already there and is unchanged, and the eleventh — the parked fault-producer question — carries his own
quoted words.

---

## Two things the trace noticed that are not warrant questions

**A question labelled *Pending Shane's call* was reshaped without him.** The old text at § 7.2 read
"**OQ-4 (pending):** whether `EventEnsures` should move inside `TransitionInspection` (per-row) or remain
event-level. Pending Shane's call." It is gone, replaced by a pointer to the new open question about what
inspection reports in place of a constraint verdict. The replacement is defensible — both of OQ-4's options
assumed evaluating event ensures against a post-mutation working copy, which `want:199` removes, so the
question as posed could not be answered either way — and the successor question is honestly declared open
rather than resolved. `result-types.md` at HEAD keeps a trace of it ("OQ-4 used to ask …"). Recorded here
because a question explicitly held for him disappeared from this document, and he should see that it did.

**The new `ValidateIngress` pseudocode reads a member the declaring document does not declare.** At
`:1367–1368` the loop is `foreach (var arg in @event.ArgDescriptors) foreach (var constraint in
arg.Constraints)`. `descriptor-types.md:101–106` declares `ArgDescriptor(string Name, TypeKind Type, bool
IsOptional, string? DefaultExpression, int SourceLine)` — there is no `Constraints` member. This is a new
inconsistency between two runtime documents, introduced by this change rather than inherited. Recorded, not
fixed.
