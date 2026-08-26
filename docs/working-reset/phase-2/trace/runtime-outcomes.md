# Trace — the runtime documents on what an operation returns and what happens when something goes wrong

**Range**: `b93d1db1..HEAD` (`dbc2a3db`)
**Files**: `docs/runtime/fault-system.md` (40 hunks), `docs/runtime/runtime-api.md` (28 hunks),
`docs/runtime/result-types.md` (18 hunks) — 86 hunks in total, counted at **zero context**
(`git diff -U0 b93d1db1..HEAD -- <file>`), grouped below into **41 entries**, one per heading: 17 for
`fault-system.md`, 10 for `result-types.md`, 14 for `runtime-api.md`. (An earlier draft of this header said
44; the entries have always been countable and there are 41 of them.) At three lines of context the same
range separates into 32 regions, which merges edits this walk needed to keep apart.
**Method**: read from `git diff`, hunk by hunk, before any working paper or the register was opened. Line
numbers are HEAD lines in the file named.

## Summary

These three documents were changed for two reasons. The larger one: they described a runtime that
re-evaluated the rules against the finished working copy and carried divide-by-zero style traps at
evaluation time, and the owner's want document says the opposite in his own words — the compiler proves
it and the runtime does not repeat the check. Nearly every hunk follows from that one sentence, and from
the certificate that he says is what lets the runtime be lighter. The smaller one: a question he parked —
whether the evaluator, on finding that its own construction invariants have been broken, reports that as a
structured outcome or lets it stay an unrecoverable exception — had been written up in canon as though it
were answered, and nine hunks change those sentences to say what *would* happen if such an outcome were
ever produced.

**Counting by category, and why the numbers add up to more than 41.** Thirteen of the 41 entries carry more
than one category — most often a deletion the want document warrants, next to a replacement wording that
repairs a separate contradiction — and each of those entries says which part is which. A change is counted
once in every category its warrant names, so the counts total 55 assignments over 41 entries rather than 41:

- **29 trace to the owner's own text** in `what-i-want-2026-07-16.md` (category 1) — 13 in `fault-system.md`,
  5 in `result-types.md`, 11 in `runtime-api.md`.
- **11 trace to a ruling of his**, quoted (category 2) — 6, 3 and 2.
- **8 are internal repairs**, canon disagreeing with itself or a cross-reference pointing at a heading that
  does not exist (category 3) — 4, 4 and none.
- **2 trace to nothing he wrote or ruled** (category 4) — one each in `result-types.md` and `runtime-api.md`.
  They are the **same change**, made in two documents, so the short list at the end has **one item**, not two.
- **5 are mechanical consequences** of another entry in this list and inherit its warrant, so they are counted
  in no category of their own — 1, 1 and 3.

The last section of this file is that single category-4 item, and it is the part worth reading.

---

## `docs/runtime/fault-system.md`

### Status table (`:7`, `:11`)

**What changed**: "evaluator integration sections pending" and "evaluator integration pending" became
"what, if anything, produces a `Fault` is undecided" and "nothing constructs a `Fault`". The Downstream
row dropped the clause "the evaluator produces `Fault` values".

**Warrant**: category 1. want:165 — *"'Lighter' means concretely: **fault-prevention checks are omitted at
evaluation time**. No divide-by-zero tests, no overflow guards, no re-checking of proven bounds on writes —
the evaluator just computes."* There is no evaluator integration left to be pending.

### Table of contents (`:24` removed, `:30` renamed)

**What changed**: the entry for § *Defense-in-depth, not a primary error path* went, and Q3's title changed.

**Warrant**: mechanical consequence of the § Defense-in-depth removal and the Q3 rewrite below; inherits
their warrants (category 1 and category 2).

### Overview (`:39`–`:50`)

**What changed**: "classifies and communicates evaluator failure modes… the runtime mirror of the
compiler's diagnostic system" became "names the classes of failure the compiler is responsible for
preventing, and pairs each one with the diagnostic that prevents it… checked when the project is built".
The comparison table's column headings changed from Compiler/Runtime to Diagnostic system/Fault system.
Added: "Nothing checks the pairing at evaluation time; the check runs when the project is built."

**Warrant**: category 1. want:165 again, and want:171 — *"A business rule whose enforcement the compiler
cannot prove complete is rejected — never handed to the runtime on the compiler's own initiative."* If
nothing is handed to the runtime, the pairing cannot be a runtime mirror.

### OWNS / Does NOT OWN (`:56`, `:59`, `:65`; one row deleted)

**What changed**: "the closed registry of all evaluator failure modes" became "the failure classes the
compiler must prevent"; "`Fault` output type — the value produced when the evaluator hits a failure path"
became "the name for a breach of one of the engine's own invariants"; the Does-NOT-OWN row "How the
evaluator produces `Fault` values" was deleted.

**Warrant**: category 1 (want:165). The re-description of `Fault` as an engine-invariant breach is
category 3 in its wording — it copies `compiler-and-runtime-design.md:826`, which had already been
corrected — but the deletion it replaces is want:165's.

### Right-Sizing lead (`:71`, `:73`)

**What changed**: "trimmed for the evaluator's narrower surface" became "trimmed to what a classification
without an evaluation-time path needs"; "every fault is fatal (the transition is aborted…)" became "an
operation that returned `Faulted` did not finish, so its working copy is discarded".

**Warrant**: category 1 (want:165).

### `RecoveryHint` bullet and its new open note (`:77`, `:79`)

**What changed**: the justification "the fault system targets runtime operators who may not be reading
source" was dropped, and a note was added saying the field's only remaining reader is the precept author,
that authoring guidance already lives on the diagnostic, and that keeping / moving / deleting the field are
all open.

**Warrant**: category 3, and it is the editor's judgement rather than his. The two statements that
conflicted: this document said the hint exists for an operator being told at runtime what to do about a
fault, while want:165 leaves nothing to be told at runtime. Nothing he wrote says where the field should
go, and the note says so instead of choosing.

### Inputs and Outputs (`:88`, `:95`, `:96`)

**What changed**: "a `FaultCode` selected by the evaluator at a failure site" became "naming a failure
class"; the format-args line gained "should anything ever construct a `Fault` (Q3)"; the outputs table's
"Evaluator → `EventOutcome.Faulted(Fault)` → caller" lost its first step.

**Warrant**: category 1 for the first two (want:165); category 2 for the Q3 hedge — see § Q3 below.

### `FaultCode` registry lead (`:101`)

**What changed**: "the closed set of all evaluator failure modes… asserting that the named diagnostic, if
emitted, makes the fault impossible at runtime" became "the closed set of failure classes the compiler is
responsible for preventing… naming the diagnostic that prevents that class".

**Warrant**: category 1 (want:165, want:171).

### The `Fault` record's TODO and description (`:211`, `:219`)

**What changed**: the TODO dropped its expected additions ("input values that triggered the fault", "linkage
back to the DiagnosticCode that should have prevented it") and now says the shape "waits on what, if
anything, constructs a Fault". The prose below it dropped "the output type the evaluator produces when a
failure path is reached" and gained "Nothing constructs one; whether anything ever will is undecided (Q3)."

**Warrant**: category 1 for the deletion (want:165); category 2 for the undecided statement — see § Q3.

### The `StaticallyPreventable` chain (`:225`, `:228`)

**What changed**: "for every way the evaluator can fail at runtime" became "for every class of failure named
here", and the chain diagram lost its first step, `Evaluator fails →`.

**Warrant**: category 1 (want:165).

### The divide-by-zero example, end to end (`:244`–`:262`)

**What changed**: the five-step walkthrough used to open with "1. Evaluator: a / b where b == 0 →
Fail(FaultCode.DivisionByZero)". That step is gone and the example is re-opened from the compiler's side,
renumbered to four steps, with a closing paragraph: "A definition whose divisor cannot be shown non-zero
never becomes an executable model, and one that does become an executable model carries no divisor test."
The justification sentence — that `FaultCode.DivisionByZero` is defense-in-depth for "data from external
sources that bypassed compile-time checking" — was deleted.

**Warrant**: category 1, and this is the one hunk his own worked example decides directly. want:104, on
`PlanRepayment`: *"At runtime there is no zero-check anywhere — the ingress validation of the arg is what
makes the division safe, and the certificate records that dependency."* The deleted sentence described the
zero-check the want document says does not exist.

The cross-reference in the same hunk was repointed from "§ FaultCode — The Runtime-to-Compiler Chain" to
"§ FaultCode → DiagnosticCode Chain". That is **category 3**: the old heading did not exist. Checked —
`diagnostic-system.md` at `b93d1db1` had `## FaultCode → DiagnosticCode Chain` at its line 552, and has it
at line 555 today. The link was broken before this phase and is now correct.

### § *Defense-in-depth, not a primary error path* deleted, and the prototype paragraph (`:283`)

**What changed**: a whole section went. It said `Fault` classifies failures "reachable only for
**out-of-contract** data — values loaded from external sources that bypassed compile-time checking". The
prototype paragraph's "failure modes are unclassified" became "failure classes are unclassified and
unpaired".

**Warrant**: category 1. want:171 rules out the model the section described, and the spec's own corrected
§0.7 now states the replacement in as many words — data that did not enter through a declared operation is
outside the guarantee, "there are no runtime fault traps waiting to catch such a value at evaluation time."

### Q1 — evaluator result type (`:289`)

**What changed**: "**Resolved:** a fault is returned as the `EventOutcome.Faulted(Fault)` variant" became
"**Resolved:** should anything ever construct a `Fault` (Q3), it is returned as…".

**Warrant**: category 2. His words, 2026-08-11, on whether anything ever constructs a `Fault`: *"do we need
to decide this now?  seems like something we can defer until we start building the runtime"*, and *"yes"*
to writing it up — recorded in `compiler-readiness-plan.md` § 4, second row. And earlier, recorded in
`runtime-open-questions.md`: *"my vote would be exception, but I think we should park this until we get to
the runtime design."* A flat "a fault is returned as…" reads as an answer to the parked question.

### Q2 — Fault context (`:304`–`:310`)

**What changed**: the four bullets asking about expression context, input values, diagnostic linkage and
event context were replaced by three asking where in the plan the breach was detected, what the engine's
own state was, and which operation was running. Closing: "there is nothing to decide until Q3 settles."

**Warrant**: category 3. The old bullets asked what a `Fault` should say about the *author's data*, which
the removal of evaluation-time fault checks makes moot. The replacement bullets — slot index, stack depth,
dispatch entry — are the editor's own suggestions and trace to nothing he wrote. They assert nothing; they
are candidate answers inside a question the same paragraph says cannot be settled yet. Recorded here so
that is visible rather than assumed.

### Q3 — renamed and rewritten (`:312`–`:322`)

**What changed**: Q3 was "Fault vs. structural impossibility" and restated the defense-in-depth paragraph.
It is now "How far the evaluator asserts about its own state", says the data half is closed, names the two
accounts in canon that disagree (`precept-builder.md` § Builder Invariant Violations and `evaluator.md`
§ No Unclassified Exceptions on one side, `compiler-and-runtime-design.md` on the other), and carries a
**Parked by the owner** paragraph.

**Warrant**: two, and they are separable. The deletion of the defense-in-depth restatement is **category
1** (want:165). The parked paragraph is **category 2** — his words as quoted at Q1 above, plus the standing
position the paragraph itself states: the lean is exception, a lean is not a ruling, nothing may be built
or written as though exception were the answer. The naming of the two disagreeing accounts is **category
3** — canon says both things and the paragraph reports the disagreement rather than settling it.

### Deliberate Exclusions (`:326`, `:330`, `:334`)

**What changed**: the `FaultException` bullet gained "Should anything ever construct a `Fault` (Q3)"; "all
faults are currently Fatal" became "all fault classes"; "a fault aborts the current transition immediately"
became "a `Faulted` outcome ends the operation where it stands".

**Warrant**: category 2 for the first (see Q1), category 1 for the second and third (want:165 — nothing
aborts a transition at evaluation time because nothing is checked there).

### Cross-References (`:341`, `:342`)

**What changed**: the `result-types.md` row became conditional; the `evaluator.md` row changed from "the
evaluator is the primary consumer… calls `Fail(FaultCode.X)` at each failure site" to "Declares
`Fail(FaultCode)` and states that a compiled definition has no fault path at evaluation time."

**Warrant**: category 2 for the first (Q1), category 1 for the second (want:165).

---

## `docs/runtime/result-types.md`

### Overview (`:51`) and Right-Sizing (`:72`)

**What changed**: both said a fault "is returned as the `EventOutcome.Faulted(Fault)` variant… never
thrown". Both now say the shape is settled, that this is what happens *should anything ever construct
one*, and that whether anything ever will is undecided.

**Warrant**: category 2 — the same 2026-08-11 deferral quoted at `fault-system.md` Q1 above. The `:72`
paragraph also lost its old subject ("errors the type checker should have prevented, such as referencing an
unknown field") in favour of an engine defect; that half is category 1 (want:165).

### `EventOutcome.ConstraintsFailed` deleted from the type declaration (`:100`)

**What changed**: `public sealed record ConstraintsFailed(ImmutableArray<ConstraintViolation> Violations)`
was removed from the discriminated union.

**Warrant**: category 1. This is the deletion the rest of the phase follows from — want:199: *"Once the
compiler has proven every rule, the runtime does **not** re-evaluate them against the post-mutation
configuration."* A variant whose only meaning is "the post-mutation re-evaluation failed" cannot exist
under that sentence. His ruling at walk item 16 (2026-07-26) is the same answer put as a worked case: shown
`rule Balance >= -OverdraftLimit` with both values supplied from outside and an event lowering the limit, he
chose the compiler refusing the definition over the runtime evaluating the rule on every operation and
discarding the change if it breaks.

### Variant table (`:112`–`:115`)

**What changed**: the `ConstraintsFailed` row was deleted. `Transitioned` and `Applied` changed pipeline
stage from "Commit/discard" to "Commit". `InvalidArgs` was re-described as an ingress failure that includes
"an argument value that violates a constraint declared on it". `Faulted`'s pipeline stage cell changed from
"Backstop" to "—".

**Warrant**: category 1 for the first three (want:199 for the deletion and the stage change; want:143 —
*"For an arg, that's its modifier-rules"* — for the `InvalidArgs` widening). Category 3 for the `Faulted`
cell: every other cell in that column names a phase from spec §3A.4, and no §3A.4 phase produces `Faulted`.

### The replacement note, the ingress sentence and the `InvalidArgs` open note (`:119`–`:123`)

**What changed**: the old scope note ("Covers ALL post-fire constraints") was replaced by "**There is no
`EventOutcome.ConstraintsFailed`**", with the reason; then a sentence saying what does fail at runtime
(ingress, collect-all); then a new open note saying `InvalidArgs(string Reason)` carries one string where
collect-all reporting wants the structured detail `ConstraintViolation` already carries, and that nothing
settles which shape it takes.

**Warrant**: category 1 for the first two (want:199, want:143). The open note is **category 3** and is the
right call — the phase widened what `InvalidArgs` reports without widening what it can carry, and it says
so rather than inventing an API shape.

### `Faulted` semantics (`:129`)

**What changed**: "`Faulted` surfaces this as a structured outcome variant rather than a raw exception"
became "`Faulted` is where it would surface if it were reported as a structured outcome variant…".

**Warrant**: category 2 (the 2026-08-11 deferral).

### `UpdateOutcome` table, its ingress note, and the disputed marking (`:147`–`:154`)

**What changed**: `Updated` and `ConstraintsFailed` were re-described — the check now happens as values
enter and before the patch is applied, at pipeline stage "Ingress governance (§3A.4)" instead of
"Rule/ensure evaluation". Two paragraphs were added: one saying the check upholds "the declared contract of
the slot the value enters and nothing wider", and one flagging that "nothing wider" is disputed.

**Warrant**: split, and the split matters. That the check happens at ingress rather than after the mutation
is **category 1** — want:141: *"Ingress is exactly two points: **event args** (including on
initial/construction events) and **editable-field writes**."* The words **"and nothing wider"** are
**category 4** and are the subject of the last section of this file.

### `EventEnsures` and OQ-4 (`:215`)

**What changed**: OQ-4 — whether `EventEnsures` should move inside `TransitionInspection` and be evaluated
per row against that row's post-mutation state — was closed rather than left open. The text now says it
stays event-level, because an `on <Event> ensure` validates arguments at ingress before any row is matched
(spec §3A.1), so there is no per-row post-mutation state to evaluate it against.

**Warrant**: category 3, and it is the editor closing an open question rather than repairing a
contradiction. The two statements in conflict: OQ-4 offered a per-row post-mutation option, and the same
phase removed the per-row post-mutation state that option needs. Nothing the owner wrote decides where
`EventEnsures` lives; what his want:199 decides is that one of the two options no longer describes anything.

### `HypotheticalResult` (`:231`), `Prospect` (`:246`), and the open note on the `ConstraintResult` arrays (`:252`)

**What changed**: `HypotheticalResult` is now non-null when the patch "passes its ingress check" rather than
when "all constraints pass". `Prospect` is now decided by the guard alone. A note was added saying the three
`ConstraintResult` arrays were shaped around a pipeline that no longer exists, that their descriptions above
still describe the old model, and that deciding what they become is a design question rather than a
correction.

**Warrant**: category 1 for the first two (want:199). The open note is **category 3**, and it quotes his
want:211 for what inspection should report instead — *"Inspect can cite provenance, not just verdicts: 'no
check needed here — proven from these premises'"* — while saying correctly that neither that line nor the
specification says which type carries it.

### Three wording changes that follow (`:357`, `:377`, `:411`)

**What changed**: "access-mode check + constraint evaluation" became "+ ingress validation of the patched
values"; the OPA `deny[msg]` survey row now says collect-all applies at ingress only; "constraints evaluated
against the working copy will produce spurious violations" became "expressions evaluated… will report
spurious results".

**Warrant**: mechanical consequences of the `ConstraintsFailed` deletion above; they inherit its category 1.

### Cross-reference row (`:436`)

**What changed**: "an evaluator-level fault is returned as the `EventOutcome.Faulted(Fault)` variant" became
"should a breach of one of the engine's own invariants ever be reported as a structured value, it is
returned as…".

**Warrant**: category 2 (the 2026-08-11 deferral).

---

## `docs/runtime/runtime-api.md`

### Does NOT OWN (`:83`)

**What changed**: "fault system (`Fault` and fault site taxonomy)" became "(`Fault` and the fault classes)".

**Warrant**: mechanical consequence of removing the fault-site machinery across the runtime documents;
inherits want:165 (category 1). No fault sites are planted, so there is no taxonomy of them.

### The `Create` caller sample (`:148`–`:153`)

**What changed**: the `EventOutcome.ConstraintsFailed f => HandleViolations(f.Violations)` case was deleted
and a comment added to the impossible list: "ConstraintsFailed — structurally impossible (no post-mutation
constraint evaluation exists)".

**Warrant**: category 1 (want:199; walk item 16).

### The construction pipeline (`:162`)

**What changed**: "→ execute matched row's action chain → evaluate constraints → return outcome" became
"validate the supplied arguments at ingress → … → execute matched row's action chain → commit → return
outcome", with the reason stated.

**Warrant**: category 1. want:18 — *"The default configuration satisfies every rule, and every initial
event *establishes* every rule, provable from the constraints declared on its args."* If establishment is
proven, there is nothing to evaluate between the action chain and the commit.

### Construction outcome table (`:168`–`:171`) and the impossible list (`:178`)

**What changed**: `Created` is produced when "a mutation row matched and its action chain ran to
completion", not when "all post-mutation constraints passed". The `ConstraintsFailed` row was deleted from
the outcome table and added to the structurally-impossible list. `InvalidArgs` gained the collect-all and
constraint-on-argument framing. `Faulted` was re-described and now says nothing constructs one.

**Warrant**: category 1 (want:18, want:199, want:143) for all but the `Faulted` row, which is category 2
(the 2026-08-11 deferral).

### Why `Create` returns `EventOutcome` (`:180`)

**What changed**: "same guards, same ensures, same constraint checking" became "same ingress validation of
the supplied arguments, same guards, same action chain".

**Warrant**: category 1 (want:199).

### § Definition Load and the Certificate Gate — a new section (`:250`–`:260`)

**What changed**: an eleven-line section was added that did not exist anywhere in runtime canon. It says
the compiler emits a certificate alongside the definition carrying, per obligation, the theorem, the
premises used and the derivation; that `Precept.From(Compilation)` runs the checker over it and refuses to
return a usable `Precept` when it does not verify; that the checker walks the recorded derivation rather
than re-deriving; and that the premise list is the set of runtime validations the proofs depend on. It
closes with a **Not yet built** note.

**Warrant**: category 1, and it is the closest thing in these three documents to his own paragraph
transcribed. want:199 — *"the compiler emits a **certificate**: a checkable record of the proof — per
obligation, the theorem, the premises used (which guard, which arg constraint, which pre-state rule), and
the derivation."* want:204 — *"A load-time gate in the runtime — the same checker, run once when a
definition is loaded. The runtime refuses to govern under a definition whose certificate does not verify:
no unproven, tampered, or stale definition can ever govern. The guarantee is rooted in verification the
runtime performed itself, not in provenance it assumes."* want:206 — *"the checker simply walks those steps
and confirms each one. Every step is re-verified; the figuring-out is never repeated."* want:210 — *"the
premise list **is** the runtime's validation surface — which guards and constraints must run and can never
be skipped — derived from the proof, never maintained in parallel."*

The one thing the section adds that he did not write is the naming of `Precept.From(Compilation)` as the
method that runs the gate. That is the editor mapping his load-time gate onto the existing API, and the
**Not yet built** note discloses that neither the certificate's shape nor the checker's API is settled.

### Restore (`:270`–`:281`)

**What changed**: the `RestoreConstraintsFailed` case was deleted from the caller sample. "Migration on
schema change is the caller's responsibility; a stale value is re-governed by the next operation's
post-mutation constraint sweep" became "Handling data written under an older definition is the post-MVP job
of the restore path itself, through migration. Nothing re-governs a stale value later either." The design
note that `RestoreConstraintsFailed` "should be removed when the Restore API is finalized" became a flat
statement that there is no constraint-failure outcome on restore.

**Warrant**: category 1, and want:141 carries both halves in one sentence: *"Restore/rehydration is *not*
ingress — data coming back from persistence is trusted and unchecked, because it was governed when it was
written. (Post-MVP, the restore path itself expands to handle schema evolution — data written under an
older definition.)"* The parenthetical is the exact correction to "the caller's responsibility". His ruling
at walk item 36 (2026-07-26) confirms the exemption: asked plainly whether Create, Fire and Update each owe
a preservation proof while Restore is exempt because the data was governed when first written, his answer
was **"yes"**.

### Fire (`:371`–`:377`)

**What changed**: the pipeline lost its "→ constraint evaluation (collect-all) → commit or discard" tail and
ends at "→ commit". A paragraph was added naming ingress validation and guard evaluation as the two checks
that run and as exactly the premises the proofs rest on, and naming the two ways a working copy is
discarded. The row dispatch model no longer lists `ConstraintsFailed` as a mutation row's outcome and gained
"the remaining rows are not evaluated (spec §3A.4 phase 2)".

**Warrant**: category 1. want:165 — *"What runs at runtime are the declared premises (ingress validation of
args and editable fields, guards) that the compiler's proofs depend on, and nothing beyond them."* That
sentence is what the added paragraph restates. want:26 gives the same thing from the other side: *"The
premises are also what the runtime enforces — governance, not deferred proof, because the compiler has
proven those checks *sufficient*."*

### Update (`:400`–`:404`)

**What changed**: the pipeline moved ingress validation of each patched value to *before* the patch is
applied and ends at commit. Two paragraphs were added: one saying the check holds the incoming value "to the
constraints declared on the field it enters — nothing wider", and one flagging that "nothing wider" is
disputed.

**Warrant**: split, exactly as at `result-types.md:147`. Ingress-before-application is **category 1**
(want:141, want:143). **"Nothing wider" is category 4** — see the last section.

### `ConstraintViolation` (`:476`)

**What changed**: "Appears in `EventOutcome.ConstraintsFailed.Violations` and
`UpdateOutcome.ConstraintsFailed.Violations`" became `UpdateOutcome` only, with a pointer to the open
question about what event-argument ingress failures carry.

**Warrant**: mechanical consequence of deleting `EventOutcome.ConstraintsFailed`; inherits category 1.

### Correctness Invariant and the enforcement model (`:664`–`:672`)

**What changed**: "same guard evaluation, same action chain, same constraint checking" became "same ingress
validation…, same guard evaluation, same action chain, same computed-field recomputation", and "commit
promotes it on success" became "commit promotes it". A paragraph was added saying inspection has no
constraint-evaluation phase to preview, and that where a rule holds because the compiler proved the handler
preserves it, "inspection reports the proof and the premises it used rather than a verdict it did not
compute." § *Three-Layer Enforcement Model* became § *Two-Layer*, and layer 3 — "15 `[StaticallyPreventable]`
fault codes exist at evaluation time as a backstop" — was deleted whole.

**Warrant**: category 1 throughout. The layer-3 deletion is want:165 word for word. The inspection sentence
is want:211 — *"Inspect can cite provenance, not just verdicts: 'no check needed here — proven from these
premises'."*

*One discrepancy worth naming, not a warrant problem*: this document states that inspection reports the
proof and its premises, while `result-types.md:252` records that nothing decides which type carries that. The
capability is his; where it lives is open. A reader of `runtime-api.md` alone would not know that.

### `InspectFire`/`InspectUpdate` (`:734`)

**What changed**: "access-mode check + constraint evaluation" became "+ ingress validation".

**Warrant**: mechanical; inherits category 1.

### Stateless construction table (`:882`–`:889`)

**What changed**: `to`/`in` ensures changed from "Evaluated against initial state" to "In scope for the
initial state — proven at compile time, not evaluated here"; arg ensures and field constraints on arguments
are now marked evaluated at ingress; a row for global rules was added saying they are proven at compile time
to be established by construction; "Working copy promotion/discard | Standard protocol" became "Working copy
promotion | Unconditional once construction completes".

**Warrant**: category 1 (want:18 for establishment, want:199 for the removal of the evaluation, want:141 for
the ingress split).

### Stateless contract line (`:895`) and Deliberate Exclusions (`:904`, `:908`)

**What changed**: "The `Rejected` and `ConstraintsFailed` outcomes remain possible" became "`Rejected` and
`InvalidArgs`". The fault bullet became conditional and gained the undecided statement. The migration bullet
dropped "migration is the caller's responsibility" and dropped the claim that a stale value "surfaces at the
next operation, whose post-mutation constraint sweep governs the resulting working copy", replacing it with
a statement that nothing inside the engine will notice.

**Warrant**: category 1 for the first and third (want:199, want:141); category 2 for the fault bullet (the
2026-08-11 deferral).

---

## Traces to nothing he wrote or ruled

> **Dispositioned 2026-08-26 — the item is closed.** The narrowing **came out of canon**, in both of these
> documents and at every other site. He did not want the underlying question settled in passing; what a
> direct field edit evaluates is still open and still belongs to phase 5. The finding below is left as it
> was written and describes the files before that surgery. His words and his reasons:
> [`../trace-walk-rulings.md`](../trace-walk-rulings.md).

**One change, made in two of the three documents. It is already labelled inside canon as disputed, and it
is on the plan's § 4 list as a question waiting on you — but it is a sentence that contradicts your own
want document sitting in two canonical documents, so it belongs on this list rather than being counted as an
internal repair.**

### `docs/runtime/runtime-api.md:402` — § Update

Before this phase, the paragraph did not exist; the pipeline line read:

> Update runs the field-write pipeline: access mode check → type validation → patch application to working
> copy → computed field recomputation → constraint evaluation (collect-all) → commit or discard.

It now reads:

> Update runs the field-write pipeline: access mode check → type validation → ingress validation of each
> patched value (collect-all) → patch application to working copy → computed field recomputation → commit.
> Returns one `UpdateOutcome` variant.
>
> Ingress validation happens **before** the value is applied, and it holds the incoming value to the
> constraints declared on the field it enters — **nothing wider** (spec §0.7, *Governance*). It is the same
> operation-blind check every externally-supplied value gets. There is no second pass over the finished
> working copy: an editable field is an ordinary write site, and the compiler discharged its obligations at
> compile time like any other. A patched value that fails its check is refused before any mutation runs, so
> nothing is rolled back.

### `docs/runtime/result-types.md:152` — § `UpdateOutcome`

Before this phase the table row read:

> | `ConstraintsFailed` | Patch applied to working copy but constraints violated | Rule/ensure evaluation |

It now reads:

> | `ConstraintsFailed` | A patched value violates a constraint declared on the field it was entering —
> refused before the patch was applied, collect-all | Ingress governance (§3A.4) |
>
> **`UpdateOutcome.ConstraintsFailed` is an ingress failure, not a post-mutation one.** The constraints it
> reports are the ones declared on the fields the patch writes, checked as the values enter and before
> anything derives from them (spec §0.7, *Governance*). It upholds the declared contract of the slot the
> value enters and **nothing wider** — an editable field is an ordinary write site, and the rules that reach
> it through other fields were discharged by the compiler like any other write site's obligations.

### What was searched for, and what it says instead

The move of the check from after the mutation to the moment of entry is yours — want:141 names editable-field
writes as one of exactly two ingress points, and want:143 says what happens there. **But want:143 says the
opposite of "nothing wider", in your own words:**

> What ingress validation means: the applicable **rules** are evaluated — however they were spelled, as
> modifiers or as rule statements. For an arg, that's its modifier-rules; anything relational an event needs
> lives in its guards. **For an editable-field write, it's the field's modifier-rules *and* every rule that
> mentions the field** — an edit is an arbitrary incoming value that no static proof can cover, so evaluating
> those rules at the ingress point is exactly the premise that closes the editable write sites in the
> compiler's proof.

I searched the want document for every wording of the narrow scope — "declared contract", "the slot it
enters", "nothing wider", "the field's own", "operation-blind" — and none of them is there. I searched
`the-walk.md` for a ruling: the nearest is walk item 16, ruled 2026-07-26, and its recorded answer is about
whether a relational rule over externally-supplied fields is proven or governed. His answer there was that
the compiler refuses the definition until the author writes the premise. That does not narrow what an
editable-field ingress check evaluates; walk item 4 says as much in the same file — the printed answer there
cites want:143 by name and calls the editable-ingress check a premise, and his mark on that item was
*not mine*, so nothing in the walk replaces want:143 either.

The narrowing was written on 2026-07-27 in the same pass that wrote it into the specification, and canon
already says so. The disputed note in `precept-language-spec.md` § 0.7, which both of these sites point at,
puts it plainly:

> The narrowing was written on 2026-07-27 on a reading of the owner's walk item 16, and that item's recorded
> answer preserves want:143 rather than replacing it. No other document supports the narrowing
> independently: every canonical sentence that agrees with it was written in the same pass. **Do not build
> against this scope, and do not cite it as evidence.**

So the two sentences above are labelled honestly and route to you rather than pretending to be settled.
What this trace records is the narrower fact: the two documents in my set now assert a scope your want
document contradicts, and every canonical sentence supporting that scope was written by the same pass on the
same day. It is one question, already on the plan's § 4 as *what discharges a rule's obligation when a
field it mentions is edited*, and answering it removes these two sentences or keeps them.

---

## Three smaller things found while tracing, none of them a warrant question

1. **`runtime-api.md:373` and `fault-system.md:73` disagree about when a working copy is discarded.**
   `runtime-api.md` says "The working copy is discarded **only** when the operation refuses before it
   finishes — arguments that fail ingress, or a `reject` row that wins dispatch." `fault-system.md` says
   "an operation that returned `Faulted` did not finish, so its working copy is discarded". Both lines were
   written by this phase. If anything ever constructs a `Fault`, the word "only" is wrong; while the producer
   question is parked, the two sentences cannot both be relied on.

2. **Three changed lines in `fault-system.md` still speak as though a `Faulted` outcome occurs**, without
   the hedge the 2026-08-11 pass applied elsewhere: `:73` ("an operation that returned `Faulted` did not
   finish"), `:95` (the outputs table row, "`Fault` … → The `EventOutcome.Faulted(Fault)` variant → caller"),
   and `:334` ("a `Faulted` outcome ends the operation where it stands"). None of the three appears in that
   pass's site list or its deliberately-left-alone list, so they were not considered either way. They are
   milder than the sentences that pass fixed — each describes what a `Faulted` outcome *does* rather than
   asserting that one is produced — but the same standard would have reached them.

3. **`runtime-api.md:666` states that inspection reports the proof and its premises**, while
   `result-types.md:252` records that neither the want document nor the specification says which type
   carries that. The capability is yours (want:211); its home is open. Worth one sentence of cross-reference
   if these documents are opened again.
