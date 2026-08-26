# Trace — the language documents, the glossary, the tooling docs and the contributing anti-patterns

**Range**: `b93d1db1..HEAD` (HEAD `dbc2a3db`)
**Files**: `docs/language/precept-language-spec.md`, `docs/language/temporal-type-system.md`,
`docs/language/catalog-system.md`, `docs/language/primitive-types.md`,
`docs/language/collection-types.md`, `docs/glossary.md`, `docs/tooling/mcp.md`,
`docs/tooling/language-server.md`, `docs/contributing/anti-patterns.md`
**Hunks examined**: 46, counted at three lines of context — plain `git diff b93d1db1..HEAD -- <file>` —
made up of 17 + 8 + 7 + 5 + 3 + 3 + 1 + 1 + 1 in the order the files are listed above, carrying roughly
55 distinct changes. At zero context (`git diff -U0`) the same range separates into 63 regions. Every
entry below is written against the three-line grouping.

Line references are HEAD lines. Quotations from `docs/Working/what-i-want-2026-07-16.md` are cited as
`want:N`; where the HEAD wording of that line comes from the 2026-07-18 Fable edit pass the original
line is given as `(72c2e183:N)`, following the walk's own convention — the provenance map's verdict on
that pass is *"`ed57d7fd` is safe to quote through … No claim in it is absent from `72c2e183`"*
(`phase-1/want-document-provenance.md`, § What this changes).

## Summary

These nine documents were changed for four reasons. The largest by far is that the runtime no longer
re-evaluates rules after mutation and no longer carries fault traps, which the owner's want document
states directly — that one change reaches the specification's principles, its guarantee section, its
outcome taxonomy, its execution order, the collection and catalog documents, the glossary and both
tooling documents. The second is overflow: his walk ruling makes it an authorized exemption, and his
walk item 23 constrains how that may be written, so every prevention list keeps overflow as the target
and gains a note that it is not built. The third is period ordering, carrying his ruling that
same-basis periods order into six sites across two documents. The fourth is a handful of places where
canon disagreed with itself and the fix depends on no position of his.

Of the changes examined: **39 rest on the owner's own text**, **9 on a ruling he gave**, **6 are
internal repairs**, and **1 traces to nothing he wrote or ruled** — the "and nothing wider" narrowing,
which the tree already flags as disputed in its own voice.

**One finding was raised here and has since been withdrawn**, and it is the reason that last count is 1
rather than 2. The non-linear rules paragraph at `precept-language-spec.md:270` was filed as tracing to
nothing he wrote, because the want-document lines it rests on were typed by an agent in the 2026-07-19
amendment rather than by him. On 2026-08-26 he settled it: he reviewed and approved the want document,
that amendment included, so the content carries his authority even though the words are not his. The
paragraph is category 1 and nothing comes out of canon. The finding as it was raised, and what it turned
out to establish, are kept in full in § *Raised and withdrawn* below — the documentary work was right, the
conclusion drawn from it was not.

Four further changes are correctly warranted but have gone stale or are unqualified in a way that
misleads; they are listed after the short list.

---

## docs/contributing/anti-patterns.md

### PROC-3 "Correct alternative" (line 336)
**What changed**: the fallback for an obligation the proof engine cannot discharge stops being a
"runtime fault-site backstop (defense-in-depth)" and becomes refusal of the definition.
**Warrant**: category 1. want:171 (72c2e183:159) — *"In an ideal world there is **no deferral**. A
business rule whose enforcement the compiler cannot prove complete is rejected — never handed to the
runtime on the compiler's own initiative."* And want:165 (72c2e183:153) — *"fault-prevention checks
are omitted at evaluation time. No divide-by-zero tests, no overflow guards, no re-checking of proven
bounds on writes — the evaluator just computes."*

---

## docs/glossary.md

### Descriptor [Runtime] (line 78)
**What changed**: "fault site" removed from the list of program elements a descriptor names.
**Warrant**: category 1, as a consequence of the want:165 change above — a fault site is a runtime
place where a check would run, and there are none. `runtime/descriptor-types.md:8` and `:206` carry
the same reading and note honestly that `FaultSiteDescriptor` is still declared in
`src/Precept/Runtime/Descriptors.cs:60` and has not been removed, so the glossary and that document
agree with each other.

### Diagnostic [Pipeline] (line 87)
**What changed**: "The runtime mirror is **Fault**" becomes "Each prevention diagnostic is paired at
build time with the **Fault** class it exists to prevent."
**Warrant**: category 1, want:165 — a "runtime mirror" asserts that faults happen at runtime.

### Fault [Runtime] (line 113)
**What changed**: a fault stops being "emitted when invariants the compiler couldn't prove are
violated at runtime" and becomes a class of failure the compiler prevents; the entry adds that
nothing produces one during evaluation and that whether anything ever constructs one is undecided.
**Warrant**: category 1 for the first half — want:171's *"no deferral"* and want:165's removal of the
runtime checks. Category 2 for the undecided clause: ruled 2026-08-11, his words as recorded in
`compiler-readiness-plan.md` § 4 — *"do we need to decide this now? seems like something we can
defer until we start building the runtime"*.

---

## docs/language/precept-language-spec.md

### Principles 7, 10 and 11 — the overflow note (lines 104, 110, 112)
**What changed**: overflow stays on every prevention list; each principle gains *"**Overflow is on
that list as the target and is not implemented yet**"* pointing at §0.7.
**Warrant**: category 2. Walk item 15, re-asked plainly and marked **mine**: the item records that he
was asked *"with the consequence stated — compiler does not prove it, runtime does not guard it, a
program can overflow and nothing stops it — and he answered 'yes'."* The item's own consequence list
says *"No document may claim overflow is prevented … the aspirational-philosophy ruling at item 23
governs how (the target may stand; a claim of *current* prevention may not)."* Walk item 23, marked
**mine**, his words: *"philosophy.md is a statement and guiding principle for where we are going.
It's aspirational."*

### Principles 10 and 11 — the runtime fault traps (lines 110, 112)
**What changed**: "Runtime fault traps exist only as defensive redundancy" becomes "There are no
runtime fault traps"; Principle 11's equivalent sentence goes the same way and names the load-time
certificate check as what makes it safe.
**Warrant**: category 1. want:165 as quoted above, and want:204 — *"**A load-time gate in the
runtime** — the same checker, run once when a definition is loaded. The runtime refuses to govern
under a definition whose certificate does not verify."*

### Principle 11 — bound containment removed from the fault-class list (line 112)
**What changed**: "constraint range impossibility" leaves the fault list; a sentence says a bound is
rule shorthand, so containment is rule preservation.
**Warrant**: category 1. want:22 — *"**Modifiers are sugar for rules.** `nonnegative` on
`OverdraftLimit` is a compact spelling of `rule OverdraftLimit >= 0` … There is one constraint
mechanism underneath."*

### §0.3 item 4 and §3A.4 — mutation atomicity (lines 149, 1990)
**What changed**: promotion stops being conditional on a post-mutation constraint check and becomes
conditional on the operation running to completion; the discard cases are enumerated (an authored
`reject`, an ingress failure, an access-mode failure).
**Warrant**: category 1. want:199 (72c2e183:192) — *"Once the compiler has proven every rule, the
runtime does **not** re-evaluate them against the post-mutation configuration."* The
no-observable-partial-state clause is kept, which the walk's item 9 identifies as the surviving part.

### §0.4 item 2 — the transition-row sequence (line 162)
**What changed**: "evaluate a guard, execute assignments left-to-right, check rules and ensures"
loses its third step.
**Warrant**: category 1, want:199, as above.

### §0.6 item 11 — an unknown default now rejects (line 213)
**What changed**: "an unknown/unfoldable default never rejects" becomes "So does an unknown or
unfoldable one", with a designed exception for a default the construction event provably overwrites.
**Warrant**: category 1. want:18 — *"**Base case.** The default configuration satisfies every rule"*;
want:124, one of the eleven must-not-compile rows he wrote — *"Change the default to `20000.0` |
Default value violates `max 10000.0` — the base case has a counterexample"*; and want:171's
no-deferral posture for the unprovable case. Note this is a real narrowing of what compiles, executed
inside a documentation pass; the warrant holds but the code change it implies belongs to a later
phase, and `diagnostic-system.md:417` and `proof-engine.md:483` still carry the old posture.

### §0.6 — the count-interval parenthetical (line 258)
**What changed**: the reason a `mincount`/`maxcount` obligation is discharged stops being "count is
'no result outside a declared bound'" and becomes "a `mincount`/`maxcount` bound is rule shorthand".
**Warrant**: category 1, want:22 as above.

### §0.7 — the fault-prevention paragraph (line 266)
**What changed**: "no result outside a declared bound" leaves the fault list and is named as rule
preservation instead.
**Warrant**: category 1, want:22.

### §0.7 — proof by induction from four premise classes (line 268)
**What changed**: the "each operand *carries* a sufficient constraint" test is replaced by proof by
induction with a base case and an inductive step, from four named premise classes.
**Warrant**: category 1. want:16–19 — *"proof by induction over reachable configurations"*;
*"**Base case.** The default configuration satisfies every rule, and every initial event *establishes*
every rule"*; *"**Inductive step.** Every handler *preserves* every rule, provable from four premise
classes: (a) field modifiers, (b) arg constraints, (c) the handler's guard, and (d) all rules holding
in the pre-state."*

### §0.7 — the non-linear rules paragraph (line 270)
**What changed**: a new paragraph, not previously in canon at all, stating that a non-linear rule is
proven only from a guard or argument constraint restating the post-state condition, and never from a
solver.
**Warrant**: **category 1**, on want:105, want:135 and the mutation-table rows at want:121–123. The
working paper's own statement of its warrant — *"Every clause is the want document's"* — is accurate, and
the paragraph stands. One qualification, which is a documentary fact and not a doubt about the content:
those want-document lines were typed by an agent in the 2026-07-19 amendment, not by the owner, so they
must not be quoted as his verbatim words. He reviewed and approved them, which is what makes the content
his. This entry was originally filed as category 4 on the opposite reading; see § *Raised and withdrawn*.

### §0.7 — the overflow paragraph (line 272)
**What changed**: a new paragraph recording that overflow is in scope, not implemented, and sequenced
after the MVP.
**Warrant**: category 2 — walk item 15 as above. The gloss defining the MVP as *"the first working
compiler, which is an internal milestone and not something anybody receives"* is category 3: it comes
from `docs/compiler/soundness-and-coverage.md`, which already carried that posture. See the note
about the un-quoted "he does not intend to release the MVP" after the short list.

### §0.7 — the governance paragraph and its disputed note (lines 274–290)
**What changed**: governance is restated as holding a value to the constraints on the slot it enters
"and nothing wider", and a blockquote marks that narrowing as disputed and instructs readers not to
build against it or cite it.
**Warrant**: the "and nothing wider" narrowing is **category 4** (short list). The blockquote flagging
it is category 3 — an internal repair, and an unusually honest one: canon contradicts want:143, and
the note says so in canon's own voice rather than resolving it.

### §0.7 — Composition (line 292)
**What changed**: the proof rests on "a premise the author declared" rather than on the structural
fact that a value carries a constraint.
**Warrant**: category 1, mechanical consequence of the induction change above (want:19).

### §0.7 — the one precondition of the guarantee (lines 294, 296)
**What changed**: the "boundary of the guarantee" passage, which enumerated ways a host could step
outside, is replaced by a single stated precondition — data must not be tampered with between
persisting and restoring — and the assumption paragraph is folded into it.
**Warrant**: category 1 on substance. want:141 (72c2e183:129) — *"Restore/rehydration is *not*
ingress — data coming back from persistence is trusted and unchecked, because it was governed when it
was written"*; with want:165 removing the runtime guards, nothing catches a value that did not arrive
that way, so the trust is a condition. The *framing* — state it once as a precondition rather than
enumerate the holes — is attributed to him in `3dbad40b`'s commit message ("Owner's framing"; "The
word 'host' is gone from both passages at his instruction") with no verbatim quotation recorded. That
half is a weaker warrant than the substance.

### §3.5 — expression scopes (line 1378)
**What changed**: a rule condition is no longer "checked against the complete working copy *after*
all mutations"; it "constrains the complete configuration", and is named as a proof obligation rather
than a runtime check.
**Warrant**: category 1, want:199. The phrasing against *the configuration the operation commits* is
walk item 9, marked **mine** — *"re-asked with a worked handler that breaks a rule mid-way and
restores it by the end … he answered 'yes, end state only'."*

### §2 — the temporal comparison line (line 1449)
**What changed**: `period` is pulled out of the equality-only group; ordering is admitted when both
operands are pinned to the same single unit, with a link to Decision 14 and the note that it is not
built.
**Warrant**: category 2. Walk item 19, marked **mine**, his words: *"mine, I ruled that we should
make same-basis periods orderable, which was against canon."*

### §3A.1 — validation surfaces (line 1927)
**What changed**: collect-all validation is re-scoped from "rules and ensures evaluated
exhaustively" to the values an operation brings in from outside, with an explicit statement that
rules and ensures are not re-evaluated after mutation.
**Warrant**: category 1. want:199 for the removal; want:141 for what ingress covers — *"Ingress is
exactly two points: **event args** (including on initial/construction events) and **editable-field
writes**."*

### §3A.2 — outcome type 4 and its bullet (lines 1941, 1950)
**What changed**: "Constraint failure — mutations would violate a rule or ensure; rolled back"
becomes "Ingress validation failure", refused before any mutation runs; the contrast bullet is
renamed to match.
**Warrant**: category 1, want:199 and want:141. The bullet rename is a mechanical consequence of the
outcome rename.

### §3A.2 — construction outcomes (lines 1958–1965)
**What changed**: `ConstraintsFailed` moves out of the outcome table into the structurally-impossible
list with its reason; `InvalidArgs` gains the collect-all note; `Faulted` is redefined as a breach of
the engine's own invariants that nothing constructs today.
**Warrant**: `ConstraintsFailed` and `InvalidArgs` are category 1 (want:199, want:18). The `Faulted`
redefinition is category 3 — an internal repair against `runtime/fault-system.md` Q3, which records
the question as undecided — written on 2026-07-28 and subsequently consistent with the 2026-08-11
ruling quoted under the glossary above.

### §3A.2 — restoration outcomes (line 1969)
**What changed**: restoration stops evaluating the current definition's rules against persisted data;
the load-time verification named instead is the certificate check on the definition.
**Warrant**: category 1. want:141 verbatim for the trust; want:204 verbatim for the certificate gate.

### §3A.4 — the ingress paragraph and its disputed pointer (lines 1992–1996)
**What changed**: a new paragraph states ingress validation, again with "and nothing wider", followed
by a short blockquote pointing at the §0.7 disputed note.
**Warrant**: the ingress statement is category 1 (want:141). The "and nothing wider" is **category
4**, and the pointer is the category 3 repair, both as at §0.7.

### §3A.4 — operation execution order (lines 2032–2033)
**What changed**: nine phases become eight — "Constraint evaluation" and "Commit or discard" collapse
into "Commit".
**Warrant**: category 1, want:199, mechanical consequence of the atomicity change.

### §3A.5 — the construction pipeline and constraint composition (lines 2103, 2108–2112)
**What changed**: step 2 stops saying "same ensures, same constraint checking"; the five-item
composition list becomes two — evaluated at ingress, everything else proven.
**Warrant**: category 1. want:18 for the base case, want:199 for the removal of the re-evaluation.

### §3A.6 — inspection (line 2180)
**What changed**: constraint evaluation is removed from the phases inspection previews and ingress
validation added.
**Warrant**: category 1, mechanical consequence of the execution-order change above.

### §7 — the PRE0115 construction exemption (lines 2204–2206)
**What changed**: the exemption is narrowed from whole-precept to per-field — only a default the
construction event provably overwrites — and the wider behaviour in the code is named as an
implementation gap.
**Warrant**: category 1, mechanical consequence of the §0.6 item 11 change (want:18, want:124,
want:171). The statement that the engine's current early-return is wider than the exception is
category 3, checkable against the code.

---

## docs/language/temporal-type-system.md

### Decision 14, rewritten in place, and its contents entry (lines 68, 1481–1489)
**What changed**: "No ordering on `period` — `==` and `!=` only" becomes "`period` orders only when
both operands are pinned to the same single unit (revised 2026-07-26)", with all four rationale legs
rewritten and a closing "Implementation state: not built" paragraph.
**Warrant**: category 2, walk item 19 as quoted above — a locked four-leg decision overridden on his
authority. The walk records the consequence itself: *"Decision 14 … is overridden by owner ruling. It
is a locked four-leg decision, so the override is recorded as his, not re-argued."* The contents
entry is a mechanical consequence of the heading. The "not built" paragraph is category 3 — canon
would otherwise assert an unbuilt capability, and its three claims were compiled at the time
(`PRE0018`, `PRE0104`, `PRE0020`).

### Alignment directive item 4 (line 244)
**What changed**: a named exception is added to "when NodaTime has no natural ordering on a type,
Precept has no natural ordering on that type."
**Warrant**: category 2, walk item 19, applied. The wording is recorded as his own in
`period-ordering-design.md:194` — *"**The owner amended the directive, in his own shorter wording,
and the amended text is live in canon**"* — but no transcript of him saying it is recorded anywhere,
so the only evidence of his authorship is the pass asserting it. Weaker than the item-19 mark itself.

### The four remaining period-ordering sites (lines 800, 806, 1329, 1346)
**What changed**: a new ordering row in the supported-operations table; the Not-supported row
narrowed; the comparison-rules row changed from ✗ to "Only when pinned"; the cross-type row narrowed.
**Warrant**: category 2, walk item 19, applied. These four and the specification line were outside
the register row's stated two-site scope; the reconciliation paper discloses that openly and offers
to revert them. They are within the ruling itself — he ruled canon wrong, and these are canon saying
the thing he ruled wrong — so the reach past the *row's* scope is not a reach past *his*. Two of them
are unqualified in a way worth correcting; see the notes after the short list.

### The `(none)` row in the typed-constant table (lines 143, 145)
**What changed**: "Period path | Duration path" becomes "Compile error — no context, no type", with a
new paragraph explaining it.
**Warrant**: category 3, internal repair. The row said a temporal quantity with no expected type
resolves to `period` or `duration`; `:1185`, `:1211`, `:1244`, `:1777` and `:1778` in the same
document all say it is a compile error. The new paragraph cites §0.1 principle 7 — the compiler
"does not guess" — which is why the compile-error side is the one kept.

### "arm" → "case", twice (lines 626, 628)
**What changed**: "the temporal arm" and "closing this arm" become "case".
**Warrant**: category 2, thin. The vocabulary row in `compiler-readiness-plan.md` § 3 (*"`case` not
`arm`"*) was marked **mine** at walk item 1, though his words there were *"mostly mine"* across
twelve rows rather than on this one. `CLAUDE.md` § How to write independently bans the word. The
sweep was incomplete — see the notes after the short list.

---

## docs/language/collection-types.md

### The `period in 'days'` row (line 533)
**What changed**: the bolded "Periods do not order regardless of basis" is removed; the cell mark
becomes "✗ — type error today; open"; the collection question is separated from the scalar rule.
**Warrant**: category 2, walk item 19. The bolded sentence was the flat contradiction of his ruling.
The separation of the collection question was accurate when written and has since gone stale — see
the notes after the short list.

### Element bound write-site (line 627)
**What changed**: the element-bound obligation is named as rule preservation rather than a fault
gate, with a note that `NumericOverflow` here does not mean machine overflow.
**Warrant**: category 1, want:22.

### Emptiness safety philosophy (line 766)
**What changed**: "Runtime fault checks exist only as defensive redundancy" becomes "Nothing stands
behind that at evaluation time."
**Warrant**: category 1, want:165.

---

## docs/language/primitive-types.md

### `integer` backing, `integer +`, `decimal +` (lines 197, 203, 236)
**What changed**: "Overflow is a type error" keeps its claim and gains "in scope and not yet
implemented, its build sequenced after the MVP".
**Warrant**: category 2, walk items 15 and 23, as at the specification's principles.

### The `notempty` applicability row (line 582)
**What changed**: `notempty` stops applying to `set`, `queue`, `stack`, `log`, `bag`, `list` and
`queue of T by P`, and becomes string-only, binding each element in a collection's inner type.
**Warrant**: category 3, internal repair, and a clean one. This row was the sole place in canon
saying `notempty` names collection cardinality; `precept-language-spec.md:459`, `:1147`, `:1174`,
`:1695`, `:1705` and `collection-types.md:354`, `:632`, `:776`, `:794` all say it is string-only and
that cardinality is `mincount`. Walk item 20's `notempty` row was marked **don't remember**, which the
walk defines as carrying the same consequence as *not mine* — so this is the editor's judgement on a
contradiction, not a ruling, which is how it is recorded here.

### The rounding boundary table and its paragraph (lines 654–658)
**What changed**: three "Runtime fault" cells become two "Deferred with the arithmetic overflow
model" cells and one "Cannot arrive — prevented at compile time and at ingress"; the paragraph below
stops calling the table what the runtime does.
**Warrant**: category 1 for removing the runtime trap (want:165); category 2 for the infinity
deferral (walk item 15, whose consequence list says *"Item 14's infinity clause, which rides on this
deferral, stands with it"*). The `NaN` disposition rests on the 2026-07-21 owner ruling already
recorded in the section, which this pass did not change.

### Integer conversion overflow (line 671)
**What changed**: "This is a runtime fault" becomes the conversion case of arithmetic overflow,
carrying the same deferral, with neither end standing behind it.
**Warrant**: category 1 (want:165) and category 2 (walk item 15).

---

## docs/language/catalog-system.md

### The vision note's cross-reference (line 103)
**What changed**: `precept-language-spec.md:1992` becomes `:2010`.
**Warrant**: category 3, mechanical repair after the specification grew. It is wrong again at HEAD —
see the notes after the short list.

### The diagnostic–fault linkage table and its paragraph (lines 701–705)
**What changed**: "runtime fault" becomes "class of failure"; "diagnostics are the compile-time face,
faults are the runtime face" becomes a statement that both halves are checked at build time and
neither during evaluation.
**Warrant**: category 1, want:165.

### The evaluator's constraint row, in three tables and one new note (lines 1259, 1328, 1329, 1433, 1434, 1444, 1454)
**What changed**: "Constraint activation timing" becomes "Ingress validation — the constraints
declared on the slot an entering value occupies"; four places gain a parenthetical saying the
five-anchor shape may not survive; a new blockquote explains what the anchors were for and what is
open.
**Warrant**: category 1 for the substance — want:199 removes the per-operation pass the buckets were
shaped for, and want:141 names ingress as what is left. The "still open" framing is category 3: it is
the editor declining to settle a consumer contract the removal leaves unresolved, which is the honest
disposition but is his judgement, not the owner's.

---

## docs/tooling/language-server.md

### Interval hover routing nuance (line 661)
**What changed**: "Even though a runtime overflow check exists, the hover reflects static proof
status. The runtime check is a fallback" becomes "no check runs at evaluation time".
**Warrant**: category 1. want:165 names overflow guards specifically — *"No divide-by-zero tests, no
overflow guards"*.

---

## docs/tooling/mcp.md

### The per-stage diagnostic-counts note (line 108)
**What changed**: "declared-bound value-safety codes" becomes "declared-bound containment codes", with
"These are rule-preservation obligations, not fault gates — a bound is rule shorthand."
**Warrant**: category 1, want:22.

---

## Traces to nothing he wrote or ruled

One item. The tree already flags it as disputed in canon's own voice, so he is not being shown it for the
first time here. A second item stood in this list until 2026-08-26 and has been withdrawn; it is written up
in full in the section after this one.

### "and nothing wider" — `precept-language-spec.md:274` and `:1992`

**Before** (§0.7 governance, and the §3A.4 twin): governance was stated without any scope limit, and
§3A.4 said *"This post-mutation sweep is one of two enforcement points … Ingress governs *what
enters*; the sweep governs *the result*."*

**After**: *"It is also *only* the field's own declared contract — ingress makes the incoming value
legal for the slot it enters, and **nothing wider**."* And at §3A.4: *"That check upholds the
declared contract of the slot the value enters **and nothing wider**; there is no second pass over
the finished working copy."*

**What was searched for and not found.** The narrowing contradicts want:143 (72c2e183:131), which is
his: *"For an editable-field write, it's the field's modifier-rules *and* every rule that mentions the
field — an edit is an arbitrary incoming value that no static proof can cover, so evaluating those
rules at the ingress point is exactly the premise that closes the editable write sites in the
compiler's proof."* It was written on a reading of walk item 16, and item 16's recorded answer is
about prove-versus-govern for relational rules; it preserves want:143 rather than replacing it.

**This one the tree already declares.** Commit `46734adc` added a blockquote at §0.7 saying exactly
this in canon's own voice — *"**This scope is disputed and is not settled canon** … **Do not build
against this scope, and do not cite it as evidence**"* — naming the six documents that carry the same
narrowing and recording that every one of them was written in the same pass, so none is independent
support. `compiler-readiness-plan.md` § 4 carries it as an open question for him, prepared and ready
to read at `phase-5/what-discharges-an-editable-write.md`. It is listed here because the trace has to
show it, not because it is unhandled.

---

## Raised and withdrawn

### The non-linear rules paragraph — `precept-language-spec.md:270`

**Raised** as the most serious item in this trace. **Withdrawn 2026-08-26 on the owner's own answer.**
Nothing comes out of canon. The finding is kept here in full, because a reader should be able to see what
was raised and how it settled, and because the documentary work underneath it is correct and worth keeping.
The paragraph's entry in the body of this trace is now category 1.

**Before**: nothing. Canon had no statement about non-linear rules at all; the working paper that
added it records that *"the only `non-linear` hit was an unrelated sentence about UCUM units."*

**After** (the whole paragraph, as it stands at HEAD):

> **Non-linear rules are proven from a premise that states the condition directly, or the definition
> is rejected.** Substitution closes a linear rule quietly — a guard such as `Balance - Amount >=
> -OverdraftLimit` is literally the post-state condition, so the proof falls out. A rule whose
> post-state condition is *non-linear* — a product or quotient of two fields, such as
> `MonthlyRepayment <= OverdraftLimit * RepaymentCapPercent` — has no such cancellation available: a
> write that sets one operand to a quotient and is checked against a product leaves four free
> variables and nothing to eliminate. Such a rule is proven only when premise class (c), the
> handler's guard, or class (b), a constraint on an event argument, states the post-state condition
> directly, so the proof closes against what the author wrote. Where no such premise exists the
> compiler **rejects the handler** and names the guard that would close it … It is never governed by
> a pass over the finished configuration, and never deferred to a solver the compiler does not carry.

**What was searched for and not found.** The working paper that added it
(`recheck-gap-corrections-executed.md` § 2.12) states its warrant explicitly: *"**Every clause is the
want document's.** … Nothing here is a new design decision; it is the owner's own statement moved
from the want document into the specification."* It cites want:135 and want:105. Both of those lines,
and the three mutation-table rows it also leans on (want:121, want:122, want:123), are in the
sixteen lines the 2026-07-19 Copilot amendment `73f4db7d` added —
`phase-1/want-document-provenance.md` Table A lists every one of them, and closes with
**"`73f4db7d` is not safe to quote."** The walk states the same rule in its own header: *"**No answer
below rests on the 16 lines the 2026-07-19 Copilot amendment wrote.**"* The walk's rule was a rule about
quoting — do not put words in his mouth — and the working paper did quote those lines as his. That much
was worth catching. Where this entry went wrong was in treating a quoting rule as a rule about whether the
content could be relied on at all.

What that reasoning established, and it still holds: the material specific to the non-linear case — that
the only admissible premise is a guard or argument constraint restating the post-state condition verbatim,
that the compiler will not attempt algebra, that no solver stands behind it, and the worked example the
paragraph is built around — appears in the want document only in the sixteen lines the 2026-07-19 amendment
added. The searches run before concluding that: `non-linear`, `nonlinear`, `product`, `quotient`, `solver`,
`linearize` over `docs/Working/what-i-want-2026-07-16.md`, `phase-1/the-walk.md` and the plan's § 3 and § 4.
Everything else in the paragraph has independent support: prove-or-reject as a general posture is want:171
(72c2e183:159), and "never governed by a pass over the finished configuration" is want:199 (72c2e183:192).

**Why the conclusion drawn from it was wrong.** "`73f4db7d` is not safe to quote" is a finding about who
typed the lines. It means those lines must not be presented as the owner's verbatim words. It does not mean
the content lacks his authority, and this entry read it as though it did. Taken generally that reading would
mean nothing the owner approves could ever be settled, because almost nothing in this repository was typed
by him.

**How it resolved.** On 2026-08-26 he was asked directly whether the repayment cap was his. His words:
*"i don't remember, I didn't write it by hand, but worked with the agent to revise the sample"*, and then
*"lets assume that I didn't write it directly, but did review it."* Asked afterwards why the content was
being disputed at all, he said he had already determined that he reviewed and approved everything in the
want document. The full record is in
[`../want-amendment-marking.md`](../want-amendment-marking.md) § *Answered 2026-08-26*.

**What follows.** The paragraph at `precept-language-spec.md:270` stands, unchanged. The working paper's
claim — *"Every clause is the want document's"* — is accurate. The `.precept` worked example stands. The
one thing genuinely wrong is narrow and lives inside the want document rather than downstream of it:
want:135 is written in the first person, as though the owner wrote those words, and he did not. Where canon
quotes want:135 as his own words it should cite the want document instead of quoting him; the claim is
unaffected. Phase 5 still decides where the discharge fragment stops, which was always its scope.

---

## Warranted, but wrong or misleading as they now stand

Four things that are not warrant failures but that a reader of these documents would be misled by.

**1. `collection-types.md:533` is stale against a later ruling.** The cell says *"deriving
`.min`/`.max` legality from the element's declared qualifier is a separate decision, not yet taken."*
That was true when written on 2026-07-28. On 2026-07-29 he ruled *"build both"* — the plan records
that he was asked the capability question and that *"on a `set of period in 'days'` there is no route
at all … So the collection half is in scope."* On 2026-08-11 he locked the design — *"yes lock the
design and log the bugs"* — and its Decision 4 is exactly *"`.min` / `.max` legality is the
comparison relation applied to the element's declared qualifier against itself."* The decision has
been taken; the cell says it has not.

**2. Two period-ordering sites read as though the operator exists.**
`temporal-type-system.md:1329` (the comparison-rules table) says *"`period` | ✓ | **Only when
pinned** … Every other pairing is a compile error"*, and `:1346` (the cross-type table) says
*"`period < period`, unless both operands are pinned to the same single unit"*. Neither carries the
"not built yet" qualification that `:800`, `precept-language-spec.md:1449` and Decision 14 itself all
carry. Today `rule A <= B` on two `period in 'days'` fields is refused with `PRE0018`, so the
capability those two cells describe does not exist. The alignment directive at `:244` has the same
shape, but it is his own wording and reads as a design principle rather than a behaviour report, so
it is the least of the three.

**3. `catalog-system.md:103` cites a line number that moved again.** It was corrected from
`precept-language-spec.md:1992` to `:2010`; the statement it points at — *"This enumeration is not yet
enforced … the other five writers are not catalogued at all"* — is at `:2020` at HEAD. `CLAUDE.md`
§ Catalog System still cites `:1992` for the same sentence. Both are wrong.

**4. The "arm" → "case" sweep was partial.** `temporal-type-system.md` was corrected in two places,
but `primitive-types.md:663` still reads *"follows the infinity arm, not the `NaN` arm"* and `:701`
*"the temporal arm"* — the same vocabulary, in the document the corrected temporal sentence points at.

One smaller record note, not a document defect: the MVP wording correction across
`precept-language-spec.md:104`, `:110`, `:112`, `:272` and `primitive-types.md:197`, `:236` rests on
the claim *"He does not intend to release the MVP"* (`mvp-wording-corrections.md:12`), which is
asserted without a quotation. Walk item 15's own text says the model is *"deferred past the MVP"* and
he answered yes to it, so "after the MVP" is warranted; what is unquoted is the further claim about
what he intends to do with the MVP, which is what the "internal milestone, not something anybody
receives" gloss rests on.
